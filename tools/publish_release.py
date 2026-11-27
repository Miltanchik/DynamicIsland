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
    res1 = subprocess.run(app_cmd, cwd=str(repo_root), capture_output=True, text=True, errors="replace")
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
    res2 = subprocess.run(setup_cmd, cwd=str(repo_root), capture_output=True, text=True, errors="replace")
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
