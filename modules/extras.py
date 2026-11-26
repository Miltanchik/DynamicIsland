# Language: Python 3.11+ | File: dynamic_island/modules/extras.py | Runtime: Win32 Registry / psutil / PyQt6
"""
Stage 5: Full Windows Dynamic Island Experience Suite (Dropzone, Privacy Dots, Telemetry, Pomodoro, Settings).
"""

from __future__ import annotations

from pathlib import Path
import time
from typing import Optional
import winreg

from PyQt6.QtCore import QMimeData, Qt, QTimer, QUrl, pyqtSignal, QObject
from PyQt6.QtGui import QColor, QDrag, QGuiApplication, QMouseEvent, QPainter, QPixmap
from PyQt6.QtWidgets import (
    QCheckBox,
    QComboBox,
    QDialog,
    QHBoxLayout,
    QLabel,
    QPushButton,
    QSpinBox,
    QVBoxLayout,
    QWidget,
)

from core.config import IslandConfig, set_autostart_enabled

try:
    import psutil
    HAS_PSUTIL = True
except Exception:
    HAS_PSUTIL = False

CONSENT_ROOT = r"Software\Microsoft\Windows\CurrentVersion\CapabilityAccessManager\ConsentStore"


def _check_stop_zero(hive: int, key_path: str) -> bool:
    try:
        with winreg.OpenKey(hive, key_path, 0, winreg.KEY_READ) as k:
            stop_val, _ = winreg.QueryValueEx(k, "LastUsedTimeStop")
            start_val, _ = winreg.QueryValueEx(k, "LastUsedTimeStart")
            return int(start_val) > 0 and int(stop_val) == 0
    except OSError:
        return False


def _is_capability_active_in_hive(hive: int, capability: str) -> bool:
    base_path = f"{CONSENT_ROOT}\\{capability}"
    try:
        with winreg.OpenKey(hive, base_path, 0, winreg.KEY_READ) as root_key:
            i = 0
            while True:
                try:
                    sub_name = winreg.EnumKey(root_key, i)
                    i += 1
                except OSError:
                    break
                sub_path = f"{base_path}\\{sub_name}"
                if sub_name == "NonPackaged":
                    try:
                        with winreg.OpenKey(hive, sub_path, 0, winreg.KEY_READ) as np_key:
                            j = 0
                            while True:
                                try:
                                    app_sub = winreg.EnumKey(np_key, j)
                                    j += 1
                                except OSError:
                                    break
                                if _check_stop_zero(hive, f"{sub_path}\\{app_sub}"):
                                    return True
                    except OSError:
                        pass
                else:
                    if _check_stop_zero(hive, sub_path):
                        return True
    except OSError:
        pass
    return False


class PrivacyDotsSensor(QObject):
    privacy_changed = pyqtSignal(bool, bool)

    def __init__(self, parent: Optional[QObject] = None) -> None:
        super().__init__(parent)
        self.mic_active: bool = False
        self.cam_active: bool = False
        self._timer = QTimer(self)
        self._timer.setInterval(1500)
        self._timer.timeout.connect(self.refresh)
        self._timer.start()
        self.refresh()

    def refresh(self) -> None:
        mic = (
            _is_capability_active_in_hive(winreg.HKEY_CURRENT_USER, "microphone")
            or _is_capability_active_in_hive(winreg.HKEY_LOCAL_MACHINE, "microphone")
        )
        cam = (
            _is_capability_active_in_hive(winreg.HKEY_CURRENT_USER, "webcam")
            or _is_capability_active_in_hive(winreg.HKEY_LOCAL_MACHINE, "webcam")
        )
        if mic != self.mic_active or cam != self.cam_active:
            self.mic_active = mic
            self.cam_active = cam
            self.privacy_changed.emit(mic, cam)


def _fmt_rate(bytes_per_sec: float) -> str:
    if bytes_per_sec < 1024:
        return "0 KB/s"
    kb = bytes_per_sec / 1024.0
    if kb < 1024:
        return f"{kb:.0f} KB/s"
    return f"{kb / 1024.0:.1f} MB/s"


class SystemTelemetryWidget(QLabel):
    def __init__(self, parent: Optional[QWidget] = None) -> None:
        super().__init__(parent)
        self.setStyleSheet(
            "color: rgba(255,255,255,185); font-size: 10px; background-color: rgba(255,255,255,12);"
            "border-radius: 9px; padding: 3px 8px;"
        )
        self._last_net = psutil.net_io_counters() if HAS_PSUTIL else None
        self._last_ts = time.monotonic()
        self._timer = QTimer(self)
        self._timer.setInterval(1500)
        self._timer.timeout.connect(self._sample)
        self._timer.start()
        self._sample()

    def _sample(self) -> None:
        if not HAS_PSUTIL:
            self.setText("CPU --%  •  RAM --%  •  ↓ 0 KB/s ↑ 0 KB/s")
            return
        try:
            cpu = int(round(psutil.cpu_percent(interval=None)))
            ram = int(round(psutil.virtual_memory().percent))
            now = time.monotonic()
            net = psutil.net_io_counters()
            dt = max(0.2, now - self._last_ts)
            down = (net.bytes_recv - self._last_net.bytes_recv) / dt if self._last_net else 0.0
            up = (net.bytes_sent - self._last_net.bytes_sent) / dt if self._last_net else 0.0
            self._last_net = net
            self._last_ts = now
            self.setText(f"⚡ CPU {cpu}% • 🧠 RAM {ram}% • ↓ {_fmt_rate(down)} ↑ {_fmt_rate(up)}")
        except Exception:
            pass


class QuickPomodoroController(QObject):
    tick = pyqtSignal(bool, str)
    finished = pyqtSignal()

    def __init__(self, default_minutes: int = 25, parent: Optional[QObject] = None) -> None:
        super().__init__(parent)
        self.default_seconds = max(60, default_minutes * 60)
        self.remaining_sec: int = 0
        self.is_running: bool = False
        self._timer = QTimer(self)
        self._timer.setInterval(1000)
        self._timer.timeout.connect(self._on_second)

    def toggle(self) -> None:
        if self.is_running:
            self.is_running = False
            self._timer.stop()
            self.tick.emit(False, "")
        else:
            if self.remaining_sec <= 0:
                self.remaining_sec = self.default_seconds
            self.is_running = True
            self._timer.start()
            self.tick.emit(True, self.format_remaining())

    def reset(self, minutes: int = 25) -> None:
        self.default_seconds = max(60, minutes * 60)
        self.remaining_sec = 0
        self.is_running = False
        self._timer.stop()
        self.tick.emit(False, "")

    def format_remaining(self) -> str:
        return f"{self.remaining_sec // 60:02d}:{self.remaining_sec % 60:02d}"

    def _on_second(self) -> None:
        if self.remaining_sec > 0:
            self.remaining_sec -= 1
            self.tick.emit(True, self.format_remaining())
        if self.remaining_sec <= 0:
            self.is_running = False
            self._timer.stop()
            self.tick.emit(False, "00:00")
            self.finished.emit()


class FileDropzoneShelf(QLabel):
    files_changed = pyqtSignal(int)

    def __init__(self, parent: Optional[QWidget] = None) -> None:
        super().__init__(parent)
        self.stashed_files: list[str] = []
        self.setAlignment(Qt.AlignmentFlag.AlignCenter)
        self.setCursor(Qt.CursorShape.OpenHandCursor)
        self.setFixedHeight(24)
        self._refresh_label()

    def add_files(self, paths: list[str]) -> None:
        for p in paths:
            if p not in self.stashed_files and Path(p).exists():
                self.stashed_files.append(p)
        self._refresh_label()
        self.files_changed.emit(len(self.stashed_files))

    def clear_files(self) -> None:
        self.stashed_files.clear()
        self._refresh_label()
        self.files_changed.emit(0)

    def _refresh_label(self) -> None:
        if not self.stashed_files:
            self.setText("📥 Dropzone: Drag files here to stash temporarily")
            self.setStyleSheet(
                "background-color: rgba(255,255,255,10); border: 1px dashed rgba(255,255,255,38);"
                "border-radius: 9px; color: rgba(255,255,255,130); font-size: 10px; padding: 2px 8px;"
            )
        else:
            count = len(self.stashed_files)
            first_name = Path(self.stashed_files[0]).name
            extra = f" (+{count - 1})" if count > 1 else ""
            self.setText(f"📦 Shelf: {first_name}{extra} (Drag out / Right-click clear)")
            self.setStyleSheet(
                "background-color: rgba(56,189,248,32); border: 1px solid rgba(56,189,248,130);"
                "border-radius: 9px; color: #E0F2FE; font-size: 10px; font-weight: 600; padding: 2px 8px;"
            )

    def mousePressEvent(self, event: QMouseEvent) -> None:
        if event.button() == Qt.MouseButton.RightButton and self.stashed_files:
            self.clear_files()
            return
        if event.button() == Qt.MouseButton.LeftButton and self.stashed_files:
            drag = QDrag(self)
            mime = QMimeData()
            mime.setUrls([QUrl.fromLocalFile(p) for p in self.stashed_files if Path(p).exists()])
            drag.setMimeData(mime)

            badge = QPixmap(115, 24)
            badge.fill(Qt.GlobalColor.transparent)
            p = QPainter(badge)
            p.setRenderHint(QPainter.RenderHint.Antialiasing, True)
            p.setBrush(QColor(14, 165, 233, 220))
            p.setPen(Qt.PenStyle.NoPen)
            p.drawRoundedRect(0, 0, 115, 24, 7, 7)
            p.setPen(QColor("#FFFFFF"))
            p.drawText(badge.rect(), int(Qt.AlignmentFlag.AlignCenter), f"📦 {len(self.stashed_files)} file(s)")
            p.end()
            drag.setPixmap(badge)
            drag.exec(Qt.DropAction.CopyAction)
        super().mousePressEvent(event)


class IslandSettingsDialog(QDialog):
    settings_applied = pyqtSignal()

    def __init__(self, config: IslandConfig, parent: Optional[QWidget] = None) -> None:
        super().__init__(parent)
        self.config = config
        self.setWindowTitle("Dynamic Island Settings")
        self.setWindowFlags(Qt.WindowType.Dialog | Qt.WindowType.WindowStaysOnTopHint)
        self.setFixedSize(320, 230)
        self.setStyleSheet(
            "QDialog { background-color: #0B0F19; color: #F9FAFB; font-family: 'Segoe UI'; }"
            "QLabel, QCheckBox { color: #E5E7EB; font-size: 12px; }"
            "QComboBox, QSpinBox { background-color: #1F2937; color: #FFF; border: 1px solid #374151; border-radius: 6px; padding: 4px 8px; }"
            "QPushButton { background-color: #2563EB; color: #FFF; border: none; border-radius: 8px; padding: 6px 14px; font-weight: 600; }"
            "QPushButton:hover { background-color: #3B82F6; }"
        )

        layout = QVBoxLayout(self)
        layout.setContentsMargins(18, 16, 18, 16)
        layout.setSpacing(12)

        row_mon = QHBoxLayout()
        row_mon.addWidget(QLabel("Active Monitor:"))
        self.cmb_monitor = QComboBox()
        for idx, scr in enumerate(QGuiApplication.screens()):
            g = scr.geometry()
            self.cmb_monitor.addItem(f"Monitor {idx + 1} ({g.width()}x{g.height()})", idx)
        self.cmb_monitor.setCurrentIndex(max(0, min(config.monitor_index, self.cmb_monitor.count() - 1)))
        row_mon.addWidget(self.cmb_monitor, stretch=1)
        layout.addLayout(row_mon)

        row_top = QHBoxLayout()
        row_top.addWidget(QLabel("Top Edge Offset (px):"))
        self.spn_top = QSpinBox()
        self.spn_top.setRange(0, 64)
        self.spn_top.setValue(config.top_margin)
        row_top.addWidget(self.spn_top)
        layout.addLayout(row_top)

        row_pomo = QHBoxLayout()
        row_pomo.addWidget(QLabel("Pomodoro Timer (min):"))
        self.spn_pomo = QSpinBox()
        self.spn_pomo.setRange(1, 120)
        self.spn_pomo.setValue(config.pomodoro_minutes)
        row_pomo.addWidget(self.spn_pomo)
        layout.addLayout(row_pomo)

        self.chk_autostart = QCheckBox("Launch automatically on Windows startup")
        self.chk_autostart.setChecked(config.autostart)
        layout.addWidget(self.chk_autostart)

        btn_row = QHBoxLayout()
        btn_save = QPushButton("Save & Apply")
        btn_save.clicked.connect(self._save)
        btn_quit = QPushButton("Quit Island")
        btn_quit.setStyleSheet("background-color: #DC2626;")
        btn_quit.clicked.connect(lambda: QGuiApplication.instance().quit())
        btn_row.addWidget(btn_save, stretch=1)
        btn_row.addWidget(btn_quit)
        layout.addLayout(btn_row)

    def _save(self) -> None:
        self.config.monitor_index = int(self.cmb_monitor.currentData())
        self.config.top_margin = int(self.spn_top.value())
        self.config.pomodoro_minutes = int(self.spn_pomo.value())
        self.config.autostart = bool(self.chk_autostart.isChecked())
        set_autostart_enabled(self.config.autostart)
        self.config.save()
        self.settings_applied.emit()
        self.accept()
