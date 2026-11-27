#!/usr/bin/env python3
# Language: Python 3.10+ (Standard Library Only)
# File: tools/publish_release.py
# Purpose: Automated version bump, x64 C# build, SHA-256 & release/version.json generator, and GitHub publisher

from __future__ import annotations

import argparse
import hashlib
import json
import os
import re
import shutil
import subprocess
import sys
from datetime import datetime, timezone
from pathlib import Path
from typing import List, Optional, Tuple

DEFAULT_REPO = "Miltanchik/DynamicIsland"


def find_git() -> str:
    direct = shutil.which("git")
    if direct:
        return direct
    for cand in [
        r"C:\Program Files\Git\cmd\git.exe",
        r"C:\Program Files\Git\bin\git.exe",
        os.path.expandvars(r"%LOCALAPPDATA%\Programs\Git\cmd\git.exe"),
    ]:
        if os.path.isfile(cand):
            return cand
    raise RuntimeError("git.exe not found in PATH or standard locations.")


def find_csc_and_winrt_refs() -> Tuple[str, str, List[str]]:
    fw_dir = r"C:\Windows\Microsoft.NET\Framework64\v4.0.30319"
    csc = os.path.join(fw_dir, "csc.exe")
    if not os.path.isfile(csc):
        raise RuntimeError(f"csc.exe not found at {csc}")

    winrt_refs: List[str] = [
        os.path.join(fw_dir, "System.Runtime.dll"),
        os.path.join(fw_dir, "System.Runtime.WindowsRuntime.dll"),
        os.path.join(fw_dir, "System.Runtime.InteropServices.WindowsRuntime.dll"),
    ]

    union_root = Path(r"C:\Program Files (x86)\Windows Kits\10\UnionMetadata")
    winmd_found: Optional[str] = None
    if union_root.exists():
        candidates = sorted(union_root.glob("10.*/Windows.winmd"), reverse=True)
        if not candidates:
            candidates = list(union_root.rglob("Windows.winmd"))
        if candidates:
            winmd_found = str(candidates[0])

    if winmd_found:
        winrt_refs.append(winmd_found)
    else:
        sys_winmd = Path(r"C:\Windows\System32\WinMetadata")
        for nm in ("Windows.Foundation.winmd", "Windows.Media.winmd", "Windows.Storage.winmd"):
            p = sys_winmd / nm
            if p.is_file():
                winrt_refs.append(str(p))

    return csc, fw_dir, winrt_refs


def parse_semver(ver: str) -> Tuple[int, int, int]:
    m = re.match(r"^v?(\d+)\.(\d+)(?:\.(\d+))?", ver.strip(), re.IGNORECASE)
    if not m:
        raise ValueError(f"Invalid version string: {ver!r}")
    return int(m.group(1)), int(m.group(2)), int(m.group(3) or 0)


def read_current_version(cs_path: Path) -> str:
    txt = cs_path.read_text(encoding="utf-8")
    m = re.search(r'public\s+const\s+string\s+CurrentVersionStr\s*=\s*"([^"]+)"\s*;', txt)
    if not m:
        raise RuntimeError("CurrentVersionStr not found in NativeDynamicIsland.cs")
    return m.group(1).strip()


def compute_next_version(cur_ver: str, target: str) -> str:
    a, b, c = parse_semver(cur_ver)
    low = target.strip().lower()
    if low in ("sync", "current", "auto-if-changed"):
        return f"{a}.{b}.{c}"
    if low == "patch":
        return f"{a}.{b}.{c + 1}"
    if low == "minor":
        return f"{a}.{b + 1}.0"
    if low == "major":
        return f"{a + 1}.0.0"
    pa, pb, pc = parse_semver(target)
    return f"{pa}.{pb}.{pc}"


def update_source_versions(repo_root: Path, ver3: str) -> bool:
    ver4 = f"{ver3}.0"
    changed = False

    app_cs = repo_root / "NativeDynamicIsland.cs"
    txt = app_cs.read_text(encoding="utf-8")
    new_txt = re.sub(
        r'(public\s+const\s+string\s+CurrentVersionStr\s*=\s*")[^"]+("\s*;)',
        rf'\g<1>{ver3}\2',
        txt,
        count=1,
    )
    new_txt = re.sub(r'(\[assembly:\s*AssemblyVersion\(")[^"]+("\)\])', rf'\g<1>{ver4}\2', new_txt)
    new_txt = re.sub(r'(\[assembly:\s*AssemblyFileVersion\(")[^"]+("\)\])', rf'\g<1>{ver4}\2', new_txt)
    if new_txt != txt:
        app_cs.write_text(new_txt, encoding="utf-8")
        changed = True

    inst_cs = repo_root / "NativeInstaller.cs"
    if inst_cs.is_file():
        itxt = inst_cs.read_text(encoding="utf-8")
        new_itxt = re.sub(r'(\[assembly:\s*AssemblyVersion\(")[^"]+("\)\])', rf'\g<1>{ver4}\2', itxt)
        new_itxt = re.sub(r'(\[assembly:\s*AssemblyFileVersion\(")[^"]+("\)\])', rf'\g<1>{ver4}\2', new_itxt)
        if new_itxt != itxt:
            inst_cs.write_text(new_itxt, encoding="utf-8")
            changed = True

    return changed


def sha256_file(path: Path) -> str:
    h = hashlib.sha256()
    with path.open("rb") as f:
        for chunk in iter(lambda: f.read(65536), b""):
            h.update(chunk)
    return h.hexdigest().lower()


def compile_release_binaries(repo_root: Path) -> Tuple[Path, Path]:
    csc, fw_dir, winrt_refs = find_csc_and_winrt_refs()
    rel_dir = repo_root / "release"
    rel_dir.mkdir(parents=True, exist_ok=True)

    ico_path = repo_root / "DynamicIslandWin.ico"
    out_app = rel_dir / "DynamicIslandWin.exe"
    out_setup = rel_dir / "DynamicIslandWin_Setup.exe"
    out_legacy_setup = rel_dir / "DinamicIsland-Setup.exe"

    app_cmd = [
        csc,
        "/nologo",
        "/optimize+",
        "/platform:x64",
        "/target:winexe",
        f"/win32icon:{ico_path}",
        f"/out:{out_app}",
        *[f"/r:{r}" for r in winrt_refs],
        f"/r:{os.path.join(fw_dir, 'System.Web.Extensions.dll')}",
        f"/r:{os.path.join(fw_dir, 'System.Windows.Forms.dll')}",
        f"/r:{os.path.join(fw_dir, 'System.Drawing.dll')}",
        str(repo_root / "NativeDynamicIsland.cs"),
    ]
    res1 = subprocess.run(app_cmd, cwd=str(repo_root), stdin=subprocess.DEVNULL, capture_output=True, text=True, errors="replace", creationflags=getattr(subprocess, "CREATE_NO_WINDOW", 0))
    if res1.returncode != 0:
        raise RuntimeError(f"DynamicIslandWin.exe build failed:\n{res1.stdout}\n{res1.stderr}")

    setup_cmd = [
        csc,
        "/nologo",
        "/optimize+",
        "/platform:x64",
        "/target:winexe",
        f"/win32icon:{ico_path}",
        f"/out:{out_setup}",
        f"/resource:{out_app},DynamicIslandWin.Payload.exe",
        f"/resource:{ico_path},DynamicIslandWin.Payload.ico",
        f"/r:{os.path.join(fw_dir, 'System.Windows.Forms.dll')}",
        f"/r:{os.path.join(fw_dir, 'System.Drawing.dll')}",
        str(repo_root / "NativeInstaller.cs"),
    ]
    res2 = subprocess.run(setup_cmd, cwd=str(repo_root), stdin=subprocess.DEVNULL, capture_output=True, text=True, errors="replace", creationflags=getattr(subprocess, "CREATE_NO_WINDOW", 0))
    if res2.returncode != 0:
        raise RuntimeError(f"DynamicIslandWin_Setup.exe build failed:\n{res2.stdout}\n{res2.stderr}")

    shutil.copy2(out_setup, out_legacy_setup)
    shutil.copy2(ico_path, rel_dir / "DynamicIslandWin.ico")

    # Best-effort copy to root workspace copies (ignore if currently locked by running exe)
    for src_f, dst_f in ((out_app, repo_root / "DynamicIslandWin.exe"), (out_setup, repo_root / "DynamicIslandWin_Setup.exe")):
        try:
            shutil.copy2(src_f, dst_f)
        except OSError:
            pass

    return out_app, out_setup


def collect_auto_changelog(repo_root: Path, version_str: str, existing_changelog: str, version_changed: bool) -> str:
    if not version_changed and existing_changelog.strip():
        return existing_changelog.strip()
    try:
        git = find_git()
        res = subprocess.run(
            [git, "log", "-n", "25", "--pretty=%s"],
            stdin=subprocess.DEVNULL,
            creationflags=getattr(subprocess, "CREATE_NO_WINDOW", 0),
            cwd=str(repo_root),
            capture_output=True,
            text=True,
            encoding="utf-8",
            errors="replace",
            timeout=15,
        )
        bullets: List[str] = []
        for raw in res.stdout.splitlines():
            s = raw.strip()
            if not s or s.startswith("chore(autosync):") or s.startswith("chore(release):"):
                continue
            bullets.append(f"- {s}")
            if len(bullets) >= 8:
                break
        if bullets:
            return f"Dynamic Island v{version_str}:\n" + "\n".join(bullets)
    except Exception:
        pass
    return existing_changelog.strip() or f"Dynamic Island v{version_str}: обновления стабильности и функционала."


def write_manifest_and_sums(
    repo_root: Path,
    repo_slug: str,
    version_str: str,
    changelog: str,
    out_app: Path,
    out_setup: Path,
) -> dict:
    rel_dir = repo_root / "release"
    sha_app = sha256_file(out_app)
    sha_setup = sha256_file(out_setup)

    sums_text = (
        f"{sha_app}  DynamicIslandWin.exe\n"
        f"{sha_setup}  DynamicIslandWin_Setup.exe\n"
        f"{sha_setup}  DinamicIsland-Setup.exe\n"
    )
    (rel_dir / "SHA256SUMS.txt").write_text(sums_text, encoding="utf-8")

    manifest = {
        "version": version_str,
        "download_url": f"https://raw.githubusercontent.com/{repo_slug}/main/release/DynamicIslandWin_Setup.exe",
        "sha256": sha_setup,
        "release_url": f"https://github.com/{repo_slug}/tree/main/release",
        "published_at": datetime.now(timezone.utc).astimezone().isoformat(timespec="seconds"),
        "changelog": changelog,
    }
    (rel_dir / "version.json").write_text(json.dumps(manifest, ensure_ascii=False, indent=2) + "\n", encoding="utf-8")
    return manifest


def git_commit_and_push(repo_root: Path, version_str: str, create_tag: bool) -> str:
    git = find_git()
    env = os.environ.copy()
    env["GIT_TERMINAL_PROMPT"] = "0"

    def run_g(args: List[str], check: bool = True) -> subprocess.CompletedProcess:
        r = subprocess.run([git, *args], cwd=str(repo_root), capture_output=True, text=True, encoding="utf-8", errors="replace", env=env)
        if check and r.returncode != 0:
            raise RuntimeError(f"git {' '.join(args)} failed:\n{r.stdout}\n{r.stderr}")
        return r

    st = run_g(["status", "--porcelain"])
    if st.stdout.strip():
        run_g(["add", "NativeDynamicIsland.cs", "NativeInstaller.cs", "release/"], check=False)
        run_g(["add", "-u"], check=False)
        staged = run_g(["diff", "--cached", "--name-only"])
        if staged.stdout.strip():
            run_g(["commit", "-m", f"chore(release): publish Dynamic Island v{version_str}"])

    if create_tag:
        run_g(["tag", "-f", f"v{version_str}"])

    br = run_g(["rev-parse", "--abbrev-ref", "HEAD"]).stdout.strip() or "main"
    if br == "HEAD":
        br = "main"

    push_r = run_g(["push", "origin", f"HEAD:{br}"], check=False)
    if push_r.returncode != 0:
        run_g(["pull", "--rebase", "origin", br])
        run_g(["push", "origin", f"HEAD:{br}"])

    if create_tag:
        run_g(["push", "origin", f"v{version_str}", "--force"], check=False)

    return run_g(["rev-parse", "--short", "HEAD"]).stdout.strip()


def print_status(repo_root: Path) -> int:
    cs_ver = read_current_version(repo_root / "NativeDynamicIsland.cs")
    ver_json_path = repo_root / "release" / "version.json"
    setup_path = repo_root / "release" / "DynamicIslandWin_Setup.exe"
    print(f"[Release] CurrentVersionStr in C#: v{cs_ver}")
    if ver_json_path.is_file():
        data = json.loads(ver_json_path.read_text(encoding="utf-8"))
        print(f"[Release] release/version.json:    v{data.get('version')} | sha256={str(data.get('sha256', ''))[:16]}...")
        print(f"[Release] download_url:            {data.get('download_url')}")
    else:
        print("[Release] release/version.json:    MISSING")
    if setup_path.is_file():
        print(f"[Release] Setup actual sha256:     {sha256_file(setup_path)[:16]}... ({setup_path.stat().st_size} bytes)")
    return 0


def main() -> int:
    parser = argparse.ArgumentParser(description="Automated Dynamic Island Release Builder & version.json Publisher")
    parser.add_argument(
        "target",
        nargs="?",
        default="status",
        help="Version target: status | sync | patch | minor | major | X.Y.Z | auto-if-changed",
    )
    parser.add_argument("-m", "--changelog", default="", help="Changelog text for version.json (use \\n for new lines)")
    parser.add_argument("--changelog-file", default="", help="Read changelog text from UTF-8 file")
    parser.add_argument("--repo", default=DEFAULT_REPO, help="GitHub owner/repo slug")
    parser.add_argument("--no-build", action="store_true", help="Skip csc.exe build and use existing binaries in release/")
    parser.add_argument("--no-push", action="store_true", help="Do not run git commit / git push")
    parser.add_argument("--install", action="store_true", help="Silently install built Setup locally after build")
    parser.add_argument("--tag", action="store_true", help="Also create & push git tag vX.Y.Z")
    args = parser.parse_args()

    repo_root = Path(__file__).resolve().parent.parent
    if args.target.strip().lower() == "status":
        return print_status(repo_root)

    cur_ver = read_current_version(repo_root / "NativeDynamicIsland.cs")
    ver_json_path = repo_root / "release" / "version.json"
    old_manifest = {}
    if ver_json_path.is_file():
        try:
            old_manifest = json.loads(ver_json_path.read_text(encoding="utf-8"))
        except Exception:
            old_manifest = {}

    old_json_ver = str(old_manifest.get("version", "")).strip()
    old_json_sha = str(old_manifest.get("sha256", "")).strip().lower()

    if args.target.strip().lower() == "auto-if-changed":
        setup_p = repo_root / "release" / "DynamicIslandWin_Setup.exe"
        actual_sha = sha256_file(setup_p) if setup_p.is_file() else ""
        if old_json_ver == cur_ver and actual_sha and old_json_sha == actual_sha:
            return 0
        # Bidirectional version pick: whether user bumped CurrentVersionStr in C# OR "version" in release/version.json, take the higher version!
        try:
            ver_cs_tuple = parse_semver(cur_ver)
            ver_js_tuple = parse_semver(old_json_ver) if old_json_ver else (0, 0, 0)
            chosen_tuple = max(ver_cs_tuple, ver_js_tuple)
            next_ver = f"{chosen_tuple[0]}.{chosen_tuple[1]}.{chosen_tuple[2]}"
        except Exception:
            next_ver = compute_next_version(cur_ver, args.target)
    else:
        next_ver = compute_next_version(cur_ver, args.target)
    version_changed = (next_ver != old_json_ver) or (next_ver != cur_ver)

    update_source_versions(repo_root, next_ver)

    if args.no_build:
        out_app = repo_root / "release" / "DynamicIslandWin.exe"
        out_setup = repo_root / "release" / "DynamicIslandWin_Setup.exe"
        if not out_setup.is_file():
            raise RuntimeError("release/DynamicIslandWin_Setup.exe does not exist; remove --no-build.")
    else:
        print(f"[Release] Compiling x64 binaries for v{next_ver}...")
        out_app, out_setup = compile_release_binaries(repo_root)

    manual_notes = args.changelog.replace("\\n", "\n").strip()
    if args.changelog_file and os.path.isfile(args.changelog_file):
        manual_notes = Path(args.changelog_file).read_text(encoding="utf-8").strip()

    if manual_notes:
        if not manual_notes.lower().startswith("dynamic island") and not manual_notes.lower().startswith("v"):
            final_notes = f"Dynamic Island v{next_ver}:\n{manual_notes}"
        else:
            final_notes = manual_notes
    else:
        json_bumped_directly = (old_json_ver == next_ver and cur_ver != next_ver and bool(str(old_manifest.get("changelog", "")).strip()))
        final_notes = str(old_manifest.get("changelog", "")).strip() if json_bumped_directly else collect_auto_changelog(repo_root, next_ver, str(old_manifest.get("changelog", "")), version_changed)

    manifest = write_manifest_and_sums(repo_root, args.repo, next_ver, final_notes, out_app, out_setup)
    print(f"[Release] Updated release/version.json -> v{manifest['version']} (sha256={manifest['sha256'][:16]}...)")

    if args.install:
        print("[Release] Installing updated DynamicIslandWin_Setup.exe (--silent)...")
        subprocess.run([str(out_setup), "--silent"], cwd=str(repo_root), check=False)

    if not args.no_push:
        sha = git_commit_and_push(repo_root, next_ver, args.tag)
        print(f"[Release] Published to GitHub main: [{sha}] v{next_ver}")

    return 0


if __name__ == "__main__":
    sys.exit(main())
