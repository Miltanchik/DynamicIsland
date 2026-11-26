# Language: Python 3.11+ | File: dynamic_island/core/config.py | Runtime: Win32 Registry & JSON
r"""
Persistent Configuration & Windows Startup Manager.
Purpose: Loads/saves pinned App Dock items, monitor selection, top margin, and autostart state
         in %APPDATA%\DynamicIslandWin\config.json and syncs HKCU\...\Run registry key.
r"""

from __future__ import annotations

from dataclasses import asdict, dataclass, field
import json
import os
from pathlib import Path
import sys
from typing import Any
import winreg

RUN_KEY_PATH = r"Software\Microsoft\Windows\CurrentVersion\Run"
APP_REG_NAME = "DynamicIslandWin"


def get_config_dir() -> Path:
    appdata = os.environ.get("APPDATA") or str(Path.home() / "AppData" / "Roaming")
    cfg_dir = Path(appdata) / "DynamicIslandWin"
    cfg_dir.mkdir(parents=True, exist_ok=True)
    return cfg_dir


def get_config_path() -> Path:
    return get_config_dir() / "config.json"


@dataclass
class IslandConfig:
    monitor_index: int = 0
    top_margin: int = 8
    autostart: bool = False
    pomodoro_minutes: int = 25
    pinned_apps: list[dict[str, Any]] = field(default_factory=list)

    @classmethod
    def load(cls) -> IslandConfig:
        path = get_config_path()
        if not path.exists():
            cfg = cls(
                pinned_apps=[
                    {"name": "Explorer", "target_path": r"C:\Windows\explorer.exe", "kind": "app"},
                    {"name": "Notepad", "target_path": r"C:\Windows\System32\notepad.exe", "kind": "app"},
                    {"name": "TaskMgr", "target_path": r"C:\Windows\System32\Taskmgr.exe", "kind": "app"},
                ]
            )
            cfg.autostart = is_autostart_enabled()
            cfg.save()
            return cfg
        try:
            raw = json.loads(path.read_text(encoding="utf-8"))
            return cls(
                monitor_index=int(raw.get("monitor_index", 0)),
                top_margin=int(raw.get("top_margin", 8)),
                autostart=bool(raw.get("autostart", is_autostart_enabled())),
                pomodoro_minutes=int(raw.get("pomodoro_minutes", 25)),
                pinned_apps=list(raw.get("pinned_apps", [])),
            )
        except Exception:
            return cls()

    def save(self) -> None:
        path = get_config_path()
        temp_path = path.with_suffix(".tmp")
        temp_path.write_text(json.dumps(asdict(self), indent=2, ensure_ascii=False), encoding="utf-8")
        temp_path.replace(path)


def is_autostart_enabled() -> bool:
    try:
        with winreg.OpenKey(winreg.HKEY_CURRENT_USER, RUN_KEY_PATH, 0, winreg.KEY_READ) as key:
            val, _ = winreg.QueryValueEx(key, APP_REG_NAME)
            return bool(val)
    except OSError:
        return False


def set_autostart_enabled(enabled: bool, script_path: Path | None = None) -> bool:
    try:
        with winreg.OpenKey(winreg.HKEY_CURRENT_USER, RUN_KEY_PATH, 0, winreg.KEY_SET_VALUE) as key:
            if enabled:
                exe = Path(sys.executable)
                pythonw = exe.with_name("pythonw.exe")
                runner = str(pythonw if pythonw.exists() else exe)
                entry_script = str((script_path or (Path(__file__).resolve().parent.parent / "main.py")).resolve())
                winreg.SetValueEx(key, APP_REG_NAME, 0, winreg.REG_SZ, f'"{runner}" "{entry_script}"')
            else:
                try:
                    winreg.DeleteValue(key, APP_REG_NAME)
                except FileNotFoundError:
                    pass
        return True
    except OSError:
        return False

