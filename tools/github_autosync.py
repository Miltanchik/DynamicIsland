#!/usr/bin/env python3
# Language: Python 3.10+ (Standard Library Only — Zero External Dependencies)
# File: tools/github_autosync.py
# Purpose: Debounced (20s) background file watcher for automatic Git commit & GitHub push

from __future__ import annotations

import argparse
import fnmatch
import os
import shutil
import signal
import subprocess
import sys
import time
from datetime import datetime
from pathlib import Path
from typing import Dict, List, Optional, Set, Tuple

ALWAYS_IGNORED_DIRS: Set[str] = {
    ".git",
    "node_modules",
    "__pycache__",
    ".autosync",
    ".vs",
    ".vscode",
    "build",
    "dist",
    ".pytest_cache",
    ".mypy_cache",
}

SECRET_PATTERNS: Tuple[str, ...] = (
    ".env",
    ".env.*",
    "*.pem",
    "*.key",
    "*.pfx",
    "*.p12",
    "*secret*",
    "*token*",
    "credentials*.json",
)


def find_git_executable() -> Optional[str]:
    direct = shutil.which("git")
    if direct:
        return direct
    candidates = [
        r"C:\Program Files\Git\cmd\git.exe",
        r"C:\Program Files\Git\bin\git.exe",
        r"C:\Program Files (x86)\Git\cmd\git.exe",
        os.path.expandvars(r"%LOCALAPPDATA%\Programs\Git\cmd\git.exe"),
    ]
    gh_desktop = Path(os.path.expandvars(r"%LOCALAPPDATA%\GitHubDesktop"))
    if gh_desktop.exists():
        for exe in gh_desktop.glob("app-*/resources/app/git/cmd/git.exe"):
            candidates.append(str(exe))
    for path in candidates:
        if path and os.path.isfile(path):
            return path
    return None


class AutoSyncDaemon:
    def __init__(self, repo_root: Path, debounce_sec: float = 20.0, poll_sec: float = 2.0) -> None:
        self.repo_root = repo_root.resolve()
        self.debounce_sec = max(5.0, float(debounce_sec))
        self.poll_sec = max(1.0, float(poll_sec))
        self.state_dir = self.repo_root / ".autosync"
        self.state_dir.mkdir(parents=True, exist_ok=True)
        self.log_file = self.state_dir / "autosync.log"
        self.pid_file = self.state_dir / "watcher.pid"
        self._running = True
        self._gitignore_patterns: List[str] = []
        self._gitignore_mtime: float = -1.0

    def log(self, level: str, message: str) -> None:
        stamp = datetime.now().strftime("%Y-%m-%d %H:%M:%S")
        line = f"[{stamp}] [{level}] {message}\n"
        try:
            if self.log_file.exists() and self.log_file.stat().st_size > 2 * 1024 * 1024:
                bak = self.state_dir / "autosync.old.log"
                if bak.exists():
                    bak.unlink()
                self.log_file.rename(bak)
            with self.log_file.open("a", encoding="utf-8") as fh:
                fh.write(line)
        except OSError:
            pass

    def _reload_gitignore_if_needed(self) -> None:
        gi_path = self.repo_root / ".gitignore"
        if not gi_path.is_file():
            self._gitignore_patterns = []
            self._gitignore_mtime = -1.0
            return
        try:
            mt = gi_path.stat().st_mtime
            if mt == self._gitignore_mtime:
                return
            self._gitignore_mtime = mt
            patterns: List[str] = []
            for raw in gi_path.read_text(encoding="utf-8", errors="ignore").splitlines():
                line = raw.strip()
                if not line or line.startswith("#") or line.startswith("!"):
                    continue
                patterns.append(line.replace("\\", "/"))
            self._gitignore_patterns = patterns
        except OSError:
            pass

    @staticmethod
    def is_secret_path(rel_posix: str) -> bool:
        name = rel_posix.rsplit("/", 1)[-1].lower()
        if name == ".env.example":
            return False
        for pat in SECRET_PATTERNS:
            if fnmatch.fnmatch(name, pat.lower()) or fnmatch.fnmatch(rel_posix.lower(), pat.lower()):
                return True
        return False

    def is_ignored_path(self, rel_posix: str, is_dir: bool = False) -> bool:
        parts = rel_posix.split("/")
        for part in parts[:-1] if not is_dir else parts:
            if part in ALWAYS_IGNORED_DIRS:
                return True
        if not is_dir and self.is_secret_path(rel_posix):
            return True

        self._reload_gitignore_if_needed()
        name = parts[-1]
        for pat in self._gitignore_patterns:
            clean = pat.rstrip("/")
            if pat.endswith("/"):
                if is_dir and (name == clean or fnmatch.fnmatch(rel_posix, clean)):
                    return True
                if any(p == clean for p in parts[:-1]):
                    return True
            else:
                if fnmatch.fnmatch(rel_posix, clean) or fnmatch.fnmatch(name, clean):
                    return True
        return False

    def snapshot_workspace(self) -> Dict[str, Tuple[int, int]]:
        snap: Dict[str, Tuple[int, int]] = {}
        for cur_root, dirs, files in os.walk(self.repo_root):
            rel_root = os.path.relpath(cur_root, self.repo_root).replace("\\", "/")
            if rel_root == ".":
                rel_root = ""

            kept_dirs: List[str] = []
            for d in dirs:
                d_rel = f"{rel_root}/{d}" if rel_root else d
                if not self.is_ignored_path(d_rel, is_dir=True):
                    kept_dirs.append(d)
            dirs[:] = kept_dirs

            for f in files:
                f_rel = f"{rel_root}/{f}" if rel_root else f
                if self.is_ignored_path(f_rel, is_dir=False):
                    continue
                full_path = os.path.join(cur_root, f)
                try:
                    st = os.stat(full_path)
                    snap[f_rel] = (st.st_mtime_ns, st.st_size)
                except OSError:
                    continue
        return snap

    def run_git(self, git_exe: str, args: List[str], timeout: int = 45) -> subprocess.CompletedProcess:
        creationflags = getattr(subprocess, "CREATE_NO_WINDOW", 0)
        env = os.environ.copy()
        env["GIT_TERMINAL_PROMPT"] = "0"
        return subprocess.run(
            [git_exe, *args],
            cwd=str(self.repo_root),
            capture_output=True,
            text=True,
            encoding="utf-8",
            errors="replace",
            timeout=timeout,
            creationflags=creationflags,
            env=env,
        )

    def perform_sync(self) -> bool:
        git_exe = find_git_executable()
        if not git_exe:
            self.log("WARN", "Git (git.exe) не найден в системе. Установите Git (`winget install Git.Git`) — повтор при следующем сохранении.")
            return False

        if not (self.repo_root / ".git").exists():
            self.log("WARN", "Репозиторий .git ещё не инициализирован. Выполните `autosync.cmd init <URL>` — повтор при следующем сохранении.")
            return False

        st = self.run_git(git_exe, ["status", "--porcelain", "-uall"])
        if st.returncode != 0:
            self.log("ERROR", f"Ошибка git status: {st.stderr.strip()}")
            return False
        if not st.stdout.strip():
            return True

        # Auto-sync release/version.json & binaries if CurrentVersionStr or Setup changed
        pub_script = self.repo_root / "tools" / "publish_release.py"
        if pub_script.is_file():
            try:
                subprocess.run(
                    [sys.executable, str(pub_script), "auto-if-changed", "--no-push"],
                    cwd=str(self.repo_root),
                    capture_output=True,
                    timeout=90,
                    creationflags=getattr(subprocess, "CREATE_NO_WINDOW", 0),
                )
            except Exception as exc:
                self.log("WARN", f"publish_release auto-if-changed skipped: {exc}")

        add_res = self.run_git(git_exe, ["add", "-A"])
        if add_res.returncode != 0:
            self.log("ERROR", f"Ошибка git add -A: {add_res.stderr.strip()}")
            return False

        staged_res = self.run_git(git_exe, ["diff", "--cached", "--name-only"])
        staged_files = [ln.strip() for ln in staged_res.stdout.splitlines() if ln.strip()]
        if not staged_files:
            return True

        # Unstage any secret file that could have slipped past .gitignore
        blocked: List[str] = []
        safe_files: List[str] = []
        for rel in staged_files:
            if self.is_secret_path(rel.replace("\\", "/")):
                blocked.append(rel)
                self.run_git(git_exe, ["reset", "HEAD", "--", rel])
            else:
                safe_files.append(rel)

        if blocked:
            self.log("WARN", f"Заблокирована отправка секретных файлов: {', '.join(blocked)}")
        if not safe_files:
            return True

        stamp = datetime.now().strftime("%Y-%m-%d %H:%M:%S")
        short_list = ", ".join(safe_files[:5])
        if len(safe_files) > 5:
            short_list += f" (+{len(safe_files) - 5} more)"
        commit_msg = f"chore(autosync): {stamp} [{short_list}]"

        cm_res = self.run_git(git_exe, ["commit", "-m", commit_msg])
        if cm_res.returncode != 0:
            self.log("ERROR", f"Ошибка git commit: {(cm_res.stderr or cm_res.stdout).strip()}")
            return False

        rev_res = self.run_git(git_exe, ["rev-parse", "--short", "HEAD"])
        short_sha = rev_res.stdout.strip() or "HEAD"
        self.log("INFO", f"Создан коммит [{short_sha}] {commit_msg}")

        br_res = self.run_git(git_exe, ["rev-parse", "--abbrev-ref", "HEAD"])
        branch = br_res.stdout.strip() or "main"

        rem_res = self.run_git(git_exe, ["remote"])
        if "origin" not in rem_res.stdout.split():
            self.log("WARN", f"Удалённый репозиторий 'origin' не настроен. Коммит [{short_sha}] сохранён локально.")
            return False

        push_res = self.run_git(git_exe, ["push", "-u", "origin", branch], timeout=60)
        if push_res.returncode == 0:
            self.log("INFO", f"Успешно отправлено в GitHub (origin/{branch}): [{short_sha}]")
            return True

        err_txt = (push_res.stderr or push_res.stdout).strip()
        if "fetch first" in err_txt.lower() or "non-fast-forward" in err_txt.lower() or "rejected" in err_txt.lower():
            self.log("WARN", "Удалённая ветка ушла вперёд — выполняю git pull --rebase...")
            pull_res = self.run_git(git_exe, ["pull", "--rebase", "origin", branch], timeout=60)
            if pull_res.returncode != 0:
                self.run_git(git_exe, ["rebase", "--abort"])
                self.log("ERROR", f"Конфликт слияния при rebase (отменён безопасно, без --force): {(pull_res.stderr or pull_res.stdout).strip()}")
                return False
            retry_push = self.run_git(git_exe, ["push", "-u", "origin", branch], timeout=60)
            if retry_push.returncode == 0:
                self.log("INFO", f"Успешно отправлено после rebase (origin/{branch}): [{short_sha}]")
                return True
            err_txt = (retry_push.stderr or retry_push.stdout).strip()

        self.log("ERROR", f"Не удалось выполнить git push (повтор при следующем сохранении): {err_txt}")
        return False

    def run_forever(self) -> None:
        self.pid_file.write_text(str(os.getpid()), encoding="utf-8")
        self.log("INFO", f"Фоновый наблюдатель GitHub AutoSync запущен (PID={os.getpid()}, debounce={self.debounce_sec:.0f}с)")

        def _handle_stop(signum, frame):
            self._running = False

        signal.signal(signal.SIGINT, _handle_stop)
        signal.signal(signal.SIGTERM, _handle_stop)

        prev_snap = self.snapshot_workspace()
        dirty_since_last_change: Optional[float] = None
        pending_retry = False

        # Sync any pre-existing uncommitted changes shortly after startup
        self.perform_sync()
        prev_snap = self.snapshot_workspace()

        try:
            while self._running:
                time.sleep(self.poll_sec)
                try:
                    cur_snap = self.snapshot_workspace()
                    if cur_snap != prev_snap:
                        prev_snap = cur_snap
                        dirty_since_last_change = time.monotonic()
                        continue

                    if dirty_since_last_change is not None:
                        elapsed = time.monotonic() - dirty_since_last_change
                        if elapsed >= self.debounce_sec:
                            dirty_since_last_change = None
                            ok = self.perform_sync()
                            pending_retry = not ok
                            prev_snap = self.snapshot_workspace()
                    elif pending_retry:
                        # Retry failed push every 60s if network was down
                        if int(time.monotonic()) % 60 < int(self.poll_sec) + 1:
                            if self.perform_sync():
                                pending_retry = False
                except Exception as exc:
                    self.log("ERROR", f"Исключение в цикле наблюдателя (продолжаю работу): {exc}")
        finally:
            self.log("INFO", f"Фоновый наблюдатель GitHub AutoSync остановлен (PID={os.getpid()})")
            try:
                if self.pid_file.exists() and self.pid_file.read_text(encoding="utf-8").strip() == str(os.getpid()):
                    self.pid_file.unlink()
            except OSError:
                pass


def main() -> int:
    parser = argparse.ArgumentParser(description="Debounced GitHub Auto-Sync File Watcher")
    parser.add_argument("--root", default=str(Path(__file__).resolve().parent.parent), help="Project root directory")
    parser.add_argument("--debounce", type=float, default=20.0, help="Debounce delay in seconds (default: 20)")
    parser.add_argument("--once", action="store_true", help="Run a single sync pass and exit")
    args = parser.parse_args()

    daemon = AutoSyncDaemon(Path(args.root), debounce_sec=args.debounce)
    if args.once:
        return 0 if daemon.perform_sync() else 1
    daemon.run_forever()
    return 0


if __name__ == "__main__":
    sys.exit(main())