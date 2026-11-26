# Language: Python 3.11+ | File: dynamic_island/main.py | Runtime: PyQt6 + Win32 + CoreAudio + SMTC
r"""
Complete Windows 10/11 Dynamic Island Desktop Application (Stages 1-5 Integrated).
Modules Integrated:
  - Stage 1: Frameless TopMost Win32 Pill Overlay, dynamic WS_EX_TRANSPARENT click-through,
             OutBack spring physics, multi-monitor positioning, battery & clock HUD.
  - Stage 2: CoreAudioController + VolumeMixerWidget (pycaw IAudioEndpointVolumeCallback,
             hardware keyboard volume Live Activity popup, mouse wheel volume control).
  - Stage 3: SMTCController + SMTCMediaWidget (winsdk GlobalSystemMediaTransportControls,
             async cover art extraction, compact cover + live equalizer, Prev/Play/Next/Seek).
  - Stage 4: AppDockWidget (Drag & Drop .exe/.lnk/folder/URL pinning, Win32 SHGetFileInfoW
             HICON extraction, right-click unpin, %APPDATA%\DynamicIslandWin\config.json).
  - Stage 5: FileDropzoneShelf (temporary file buffer drag-in/drag-out), PrivacyDotsSensor
             (iOS orange mic / green camera dots via ConsentStore registry), SystemTelemetryWidget
             (CPU/RAM/Network speeds), QuickPomodoroController (compact countdown), Settings & Autostart.
r"""

from __future__ import annotations

import ctypes
from ctypes import wintypes
from dataclasses import dataclass
from enum import Enum, auto
import math
from pathlib import Path
import sys
import time
from typing import Final, Optional

from PyQt6.QtCore import (
    QEasingCurve,
    QPoint,
    QPointF,
    QRectF,
    Qt,
    QTimer,
    QVariantAnimation,
    pyqtSignal,
)
from PyQt6.QtGui import (
    QColor,
    QCursor,
    QDragEnterEvent,
    QDropEvent,
    QFont,
    QGuiApplication,
    QLinearGradient,
    QMouseEvent,
    QPainter,
    QPainterPath,
    QPen,
    QPixmap,
    QScreen,
    QWheelEvent,
)
from PyQt6.QtWidgets import (
    QApplication,
    QGraphicsOpacityEffect,
    QHBoxLayout,
    QPushButton,
    QVBoxLayout,
    QWidget,
)

from core.config import IslandConfig
from modules.app_dock import AppDockWidget
from modules.audio_mixer import CoreAudioController, VolumeMixerWidget, format_volume_icon
from modules.extras import (
    FileDropzoneShelf,
    IslandSettingsDialog,
    PrivacyDotsSensor,
    QuickPomodoroController,
    SystemTelemetryWidget,
)
from modules.smtc_player import (
    MediaTrackState,
    SMTCController,
    SMTCMediaWidget,
    make_rounded_cover,
)

# ---------------------------------------------------------------------------
# Win32 API Constants & Extended Window Style Hooks
# ---------------------------------------------------------------------------
GWL_EXSTYLE: Final[int] = -20
WS_EX_LAYERED: Final[int] = 0x00080000
WS_EX_TRANSPARENT: Final[int] = 0x00000020
WS_EX_TOOLWINDOW: Final[int] = 0x00000080
WS_EX_NOACTIVATE: Final[int] = 0x08000000

HWND_TOPMOST: Final[int] = -1
SWP_NOMOVE: Final[int] = 0x0002
SWP_NOSIZE: Final[int] = 0x0001
SWP_NOACTIVATE: Final[int] = 0x0010
SWP_SHOWWINDOW: Final[int] = 0x0040

user32 = ctypes.windll.user32
kernel32 = ctypes.windll.kernel32

SetWindowLongPtrW = getattr(user32, "SetWindowLongPtrW", user32.SetWindowLongW)
SetWindowLongPtrW.argtypes = [wintypes.HWND, ctypes.c_int, ctypes.c_ssize_t]
SetWindowLongPtrW.restype = ctypes.c_ssize_t

GetWindowLongPtrW = getattr(user32, "GetWindowLongPtrW", user32.GetWindowLongW)
GetWindowLongPtrW.argtypes = [wintypes.HWND, ctypes.c_int]
GetWindowLongPtrW.restype = ctypes.c_ssize_t


class SYSTEM_POWER_STATUS(ctypes.Structure):
    _fields_ = [
        ("ACLineStatus", wintypes.BYTE),
        ("BatteryFlag", wintypes.BYTE),
        ("BatteryLifePercent", wintypes.BYTE),
        ("SystemStatusFlag", wintypes.BYTE),
        ("BatteryLifeTime", wintypes.DWORD),
        ("BatteryFullLifeTime", wintypes.DWORD),
    ]


def get_battery_status() -> tuple[int, bool, bool]:
    status = SYSTEM_POWER_STATUS()
    if not kernel32.GetSystemPowerStatus(ctypes.byref(status)):
        return (100, False, False)
    has_battery = (status.BatteryFlag & 128) == 0 and status.BatteryLifePercent <= 100
    is_charging = status.ACLineStatus == 1 or (status.BatteryFlag & 8) != 0
    percent = min(max(int(status.BatteryLifePercent), 0), 100) if has_battery else 100
    return (percent, is_charging, has_battery)


class IslandState(Enum):
    IDLE = auto()
    LIVE_ACTIVITY = auto()
    EXPANDED = auto()


@dataclass
class IslandMetrics:
    canvas_width: int = 580
    canvas_height: int = 260
    top_margin: int = 8

    idle_width: float = 236.0
    idle_height: float = 36.0

    activity_width: float = 336.0
    activity_height: float = 46.0

    expanded_width: float = 516.0
    expanded_height: float = 218.0

    collapse_delay_ms: int = 400


class SpringAnimator:
    def __init__(self, on_tick_callback) -> None:
        self._on_tick = on_tick_callback
        self.current_w: float = IslandMetrics.idle_width
        self.current_h: float = IslandMetrics.idle_height
        self.expansion_progress: float = 0.0

        self._anim = QVariantAnimation()
        self._anim.setStartValue(0.0)
        self._anim.setEndValue(1.0)
        self._anim.valueChanged.connect(self._handle_value_changed)

        self._start_w: float = self.current_w
        self._start_h: float = self.current_h
        self._target_w: float = self.current_w
        self._target_h: float = self.current_h
        self._start_prog: float = 0.0
        self._target_prog: float = 0.0

    def transition_to(
        self,
        target_w: float,
        target_h: float,
        target_prog: float,
        duration_ms: int = 380,
        expanding: bool = True,
    ) -> None:
        self._anim.stop()
        self._start_w = self.current_w
        self._start_h = self.current_h
        self._start_prog = self.expansion_progress
        self._target_w = target_w
        self._target_h = target_h
        self._target_prog = target_prog

        curve = QEasingCurve(
            QEasingCurve.Type.OutBack if expanding else QEasingCurve.Type.OutCubic
        )
        if expanding:
            curve.setOvershoot(1.18)

        self._anim.setDuration(duration_ms)
        self._anim.setEasingCurve(curve)
        self._anim.start()

    def _handle_value_changed(self, value: float) -> None:
        t = float(value)
        self.current_w = self._start_w + (self._target_w - self._start_w) * t
        self.current_h = self._start_h + (self._target_h - self._start_h) * t
        raw_prog = self._start_prog + (self._target_prog - self._start_prog) * t
        self.expansion_progress = max(0.0, min(1.0, raw_prog))
        self._on_tick()


class DynamicIslandWindow(QWidget):
    state_changed = pyqtSignal(IslandState)

    def __init__(self) -> None:
        super().__init__(None)
        self.config = IslandConfig.load()
        self.metrics = IslandMetrics(top_margin=self.config.top_margin)
        self.state = IslandState.IDLE

        self._is_click_through_active = False
        self._is_hovered = False

        # Compact & Live Activity state
        self._eq_phase: float = 0.0
        self._clock_text: str = time.strftime("%H:%M")
        self._battery_pct, self._battery_charging, self._has_battery = get_battery_status()
        self._media_state = MediaTrackState()
        self._compact_cover: QPixmap = make_rounded_cover(b"", 24, 6)
        self._live_banner_text: str = ""
        self._pomodoro_compact_text: str = ""
        self._mic_active: bool = False
        self._cam_active: bool = False

        # Core Controllers (Stages 2, 3, 5)
        self.audio_ctrl = CoreAudioController(self)
        self.smtc_ctrl = SMTCController(self)
        self.privacy_sensor = PrivacyDotsSensor(self)
        self.pomodoro_ctrl = QuickPomodoroController(self.config.pomodoro_minutes, self)

        self._configure_window_flags()
        self._build_expanded_ui()
        self._wire_signals()

        self.animator = SpringAnimator(self._on_animation_frame)

        self._collapse_timer = QTimer(self)
        self._collapse_timer.setSingleShot(True)
        self._collapse_timer.setInterval(self.metrics.collapse_delay_ms)
        self._collapse_timer.timeout.connect(self._collapse_to_idle)

        self._tick_timer = QTimer(self)
        self._tick_timer.setInterval(16)
        self._tick_timer.timeout.connect(self._on_frame_tick)
        self._tick_timer.start()

        self._status_timer = QTimer(self)
        self._status_timer.setInterval(2000)
        self._status_timer.timeout.connect(self._refresh_system_status)
        self._status_timer.start()

        self.position_on_screen(self.config.monitor_index)

    def _configure_window_flags(self) -> None:
        flags = (
            Qt.WindowType.FramelessWindowHint
            | Qt.WindowType.WindowStaysOnTopHint
            | Qt.WindowType.Tool
            | Qt.WindowType.WindowDoesNotAcceptFocus
        )
        self.setWindowFlags(flags)
        self.setAttribute(Qt.WidgetAttribute.WA_TranslucentBackground, True)
        self.setAttribute(Qt.WidgetAttribute.WA_NoSystemBackground, True)
        self.setAttribute(Qt.WidgetAttribute.WA_ShowWithoutActivating, True)
        self.setMouseTracking(True)
        self.setAcceptDrops(True)
        self.setFixedSize(self.metrics.canvas_width, self.metrics.canvas_height)

    def showEvent(self, event) -> None:
        super().showEvent(event)
        self._apply_win32_extended_styles()

    def _apply_win32_extended_styles(self) -> None:
        hwnd = int(self.winId())
        ex_style = GetWindowLongPtrW(hwnd, GWL_EXSTYLE)
        ex_style |= WS_EX_TOOLWINDOW | WS_EX_LAYERED | WS_EX_NOACTIVATE
        SetWindowLongPtrW(hwnd, GWL_EXSTYLE, ex_style)
        user32.SetWindowPos(
            hwnd,
            HWND_TOPMOST,
            0,
            0,
            0,
            0,
            SWP_NOMOVE | SWP_NOSIZE | SWP_NOACTIVATE | SWP_SHOWWINDOW,
        )

    def _set_win32_click_through(self, enabled: bool) -> None:
        if self._is_click_through_active == enabled:
            return
        self._is_click_through_active = enabled
        hwnd = int(self.winId())
        ex_style = GetWindowLongPtrW(hwnd, GWL_EXSTYLE)
        if enabled:
            ex_style |= WS_EX_TRANSPARENT
        else:
            ex_style &= ~WS_EX_TRANSPARENT
        SetWindowLongPtrW(hwnd, GWL_EXSTYLE, ex_style)

    def position_on_screen(self, screen_index: int = 0) -> None:
        screens: list[QScreen] = QGuiApplication.screens()
        if not screens:
            return
        idx = max(0, min(screen_index, len(screens) - 1))
        self.config.monitor_index = idx
        self.metrics.top_margin = self.config.top_margin
        screen_geom = screens[idx].geometry()
        x = screen_geom.x() + (screen_geom.width() - self.metrics.canvas_width) // 2
        y = screen_geom.y()
        self.move(x, y)
        self.update()

    def current_pill_rect(self) -> QRectF:
        w = self.animator.current_w
        h = self.animator.current_h
        x = (self.metrics.canvas_width - w) / 2.0
        y = float(self.metrics.top_margin)
        return QRectF(x, y, w, h)

    # -----------------------------------------------------------------------
    # Expanded Hub UI (Stages 2, 3, 4, 5 Integrated)
    # -----------------------------------------------------------------------
    def _build_expanded_ui(self) -> None:
        self.expanded_container = QWidget(self)
        self.expanded_container.setAttribute(Qt.WidgetAttribute.WA_TranslucentBackground, True)
        self._opacity_effect = QGraphicsOpacityEffect(self.expanded_container)
        self._opacity_effect.setOpacity(0.0)
        self.expanded_container.setGraphicsEffect(self._opacity_effect)
        self.expanded_container.hide()

        root = QVBoxLayout(self.expanded_container)
        root.setContentsMargins(18, 12, 18, 12)
        root.setSpacing(6)

        # Row 1: Stage 3 Windows SMTC Media Player
        self.media_widget = SMTCMediaWidget(self.smtc_ctrl, self.expanded_container)
        root.addWidget(self.media_widget)

        # Row 2: Stage 2 CoreAudio Volume Mixer + Quick Pomodoro + Settings Button
        mid_row = QHBoxLayout()
        mid_row.setSpacing(6)

        self.volume_widget = VolumeMixerWidget(self.audio_ctrl, self.expanded_container)
        mid_row.addWidget(self.volume_widget, stretch=1)

        pill_btn_css = (
            "QPushButton {"
            "  background-color: rgba(255,255,255,18); color: #F9FAFB;"
            "  border: 1px solid rgba(255,255,255,30); border-radius: 11px;"
            "  padding: 3px 9px; font-size: 11px; font-weight: 600;"
            "}"
            "QPushButton:hover { background-color: rgba(255,255,255,38); }"
        )
        self.btn_pomodoro = QPushButton("⏱ Timer")
        self.btn_pomodoro.setCursor(Qt.CursorShape.PointingHandCursor)
        self.btn_pomodoro.setStyleSheet(pill_btn_css)
        self.btn_pomodoro.clicked.connect(self.pomodoro_ctrl.toggle)

        self.btn_settings = QPushButton("⚙")
        self.btn_settings.setFixedSize(26, 24)
        self.btn_settings.setCursor(Qt.CursorShape.PointingHandCursor)
        self.btn_settings.setToolTip("Dynamic Island Settings & Autostart")
        self.btn_settings.setStyleSheet(pill_btn_css)
        self.btn_settings.clicked.connect(self._open_settings_dialog)

        mid_row.addWidget(self.btn_pomodoro)
        mid_row.addWidget(self.btn_settings)
        root.addLayout(mid_row)

        # Row 3: Stage 4 Native App Dock (Drag & Drop .exe / .lnk / Folder / URL)
        self.app_dock = AppDockWidget(self.config, self.expanded_container)
        root.addWidget(self.app_dock)

        # Row 4: Stage 5 File Dropzone Shelf + Live CPU/RAM/Net Telemetry
        bottom_row = QHBoxLayout()
        bottom_row.setSpacing(8)
        self.dropzone_shelf = FileDropzoneShelf(self.expanded_container)
        self.telemetry_widget = SystemTelemetryWidget(self.expanded_container)
        bottom_row.addWidget(self.dropzone_shelf, stretch=1)
        bottom_row.addWidget(self.telemetry_widget)
        root.addLayout(bottom_row)

    def _wire_signals(self) -> None:
        self.audio_ctrl.volume_changed.connect(self._on_volume_changed)
        self.smtc_ctrl.track_updated.connect(self._on_media_updated)
        self.smtc_ctrl.track_switched.connect(self._on_media_switched)
        self.privacy_sensor.privacy_changed.connect(self._on_privacy_changed)
        self.pomodoro_ctrl.tick.connect(self._on_pomodoro_tick)
        self.pomodoro_ctrl.finished.connect(
            lambda: self.trigger_live_activity("⏰ Timer Finished!")
        )

    # -----------------------------------------------------------------------
    # Signal Handlers & Live Activities
    # -----------------------------------------------------------------------
    def _on_volume_changed(self, percent: int, muted: bool, external: bool) -> None:
        if external and self.state != IslandState.EXPANDED:
            icon = format_volume_icon(percent, muted)
            label = "Muted" if muted else f"Volume {percent}%"
            self.trigger_live_activity(f"{icon}  {label}")

    def _on_media_updated(self, state: MediaTrackState) -> None:
        self._media_state = state
        self._compact_cover = make_rounded_cover(state.cover_bytes, 24, 6)
        self.update()

    def _on_media_switched(self, state: MediaTrackState) -> None:
        if self.state != IslandState.EXPANDED:
            self.trigger_live_activity(f"🎵 {state.title} — {state.artist}")

    def _on_privacy_changed(self, mic: bool, cam: bool) -> None:
        self._mic_active = mic
        self._cam_active = cam
        self.update()

    def _on_pomodoro_tick(self, running: bool, text: str) -> None:
        self._pomodoro_compact_text = text if running else ""
        self.btn_pomodoro.setText(f"⏱ {text}" if running else "⏱ Timer")
        self.update()

    def trigger_live_activity(self, banner_text: str) -> None:
        self._live_banner_text = banner_text
        self.state = IslandState.LIVE_ACTIVITY
        self.animator.transition_to(
            self.metrics.activity_width,
            self.metrics.activity_height,
            target_prog=0.0,
            duration_ms=340,
            expanding=True,
        )
        self._collapse_timer.start(1750)
        self.state_changed.emit(self.state)

    def expand(self) -> None:
        self._collapse_timer.stop()
        self._live_banner_text = ""
        if self.state == IslandState.EXPANDED:
            return
        self.state = IslandState.EXPANDED
        self.expanded_container.show()
        self.animator.transition_to(
            self.metrics.expanded_width,
            self.metrics.expanded_height,
            target_prog=1.0,
            duration_ms=410,
            expanding=True,
        )
        self.state_changed.emit(self.state)

    def _collapse_to_idle(self) -> None:
        if self._is_hovered:
            return
        self._live_banner_text = ""
        self.state = IslandState.IDLE
        self.animator.transition_to(
            self.metrics.idle_width,
            self.metrics.idle_height,
            target_prog=0.0,
            duration_ms=320,
            expanding=False,
        )
        self.state_changed.emit(self.state)

    def _open_settings_dialog(self) -> None:
        dlg = IslandSettingsDialog(self.config, self)
        dlg.settings_applied.connect(self._apply_new_config)
        dlg.exec()

    def _apply_new_config(self) -> None:
        self.pomodoro_ctrl.reset(self.config.pomodoro_minutes)
        self.position_on_screen(self.config.monitor_index)

    # -----------------------------------------------------------------------
    # Mouse, Wheel & Drag-and-Drop (App Dock Pinning vs Dropzone Buffer)
    # -----------------------------------------------------------------------
    def wheelEvent(self, event: QWheelEvent) -> None:
        r"""Changes Windows Master Volume when scrolling mouse wheel directly over the Island.r"""
        pill = self.current_pill_rect()
        if pill.contains(QPointF(event.position())):
            delta = event.angleDelta().y()
            if delta != 0:
                step = 2 if delta > 0 else -2
                self.audio_ctrl.step_volume(step, external_trigger=(self.state != IslandState.EXPANDED))
                event.accept()
                return
        super().wheelEvent(event)

    def mousePressEvent(self, event: QMouseEvent) -> None:
        pill = self.current_pill_rect()
        if pill.contains(QPointF(event.position())):
            if event.button() == Qt.MouseButton.LeftButton and self.state != IslandState.EXPANDED:
                self.expand()
            elif event.button() == Qt.MouseButton.RightButton and self.state != IslandState.EXPANDED:
                self._open_settings_dialog()
        super().mousePressEvent(event)

    def dragEnterEvent(self, event: QDragEnterEvent) -> None:
        if event.mimeData().hasUrls() or event.mimeData().hasText():
            event.acceptProposedAction()
            self.expand()

    def dropEvent(self, event: QDropEvent) -> None:
        mime = event.mimeData()
        stash_files: list[str] = []
        if mime.hasUrls():
            for url in mime.urls():
                if url.isLocalFile():
                    local_path = url.toLocalFile()
                    p = Path(local_path)
                    # Pin .exe, .lnk, .url or folders to App Dock; stash documents/images in Dropzone Shelf
                    if p.is_dir() or p.suffix.lower() in (".exe", ".lnk", ".url", ".bat", ".cmd"):
                        self.app_dock.add_pinned_target(local_path)
                    else:
                        stash_files.append(local_path)
                else:
                    self.app_dock.add_pinned_target(url.toString())
        elif mime.hasText():
            txt = mime.text().strip()
            if txt.startswith(("http://", "https://")):
                self.app_dock.add_pinned_target(txt)

        if stash_files:
            self.dropzone_shelf.add_files(stash_files)
        event.acceptProposedAction()

    # -----------------------------------------------------------------------
    # Frame Tick & Hardware-Accelerated Capsule Painting
    # -----------------------------------------------------------------------
    def _on_frame_tick(self) -> None:
        global_pos: QPoint = QCursor.pos()
        local_pos = QPointF(self.mapFromGlobal(global_pos))
        pill = self.current_pill_rect().adjusted(-4.0, -4.0, 4.0, 4.0)
        inside_pill = pill.contains(local_pos)

        self._set_win32_click_through(not inside_pill)

        if inside_pill and not self._is_hovered:
            self._is_hovered = True
            self._collapse_timer.stop()
            self.expand()
        elif not inside_pill and self._is_hovered:
            self._is_hovered = False
            self._collapse_timer.start(self.metrics.collapse_delay_ms)

        if self.animator.expansion_progress < 0.85:
            step = 0.25 if self._media_state.is_playing else 0.06
            self._eq_phase += step
            self.update()

    def _on_animation_frame(self) -> None:
        pill = self.current_pill_rect()
        self.expanded_container.setGeometry(pill.toRect())
        prog = self.animator.expansion_progress
        ui_opacity = max(0.0, min(1.0, (prog - 0.35) / 0.65))
        self._opacity_effect.setOpacity(ui_opacity)
        if prog <= 0.05 and self.state != IslandState.EXPANDED:
            self.expanded_container.hide()
        self.update()

    def _refresh_system_status(self) -> None:
        self._clock_text = time.strftime("%H:%M")
        self._battery_pct, self._battery_charging, self._has_battery = get_battery_status()
        self.update()

    def paintEvent(self, event) -> None:
        painter = QPainter(self)
        painter.setRenderHint(QPainter.RenderHint.Antialiasing, True)
        painter.setRenderHint(QPainter.RenderHint.TextAntialiasing, True)

        pill = self.current_pill_rect()
        compact_radius = pill.height() / 2.0
        expanded_radius = 34.0
        prog = self.animator.expansion_progress
        radius = compact_radius * (1.0 - prog) + expanded_radius * prog

        path = QPainterPath()
        path.addRoundedRect(pill, radius, radius)

        painter.fillPath(path, QColor(7, 7, 9, 250))

        border_grad = QLinearGradient(pill.topLeft(), pill.bottomLeft())
        border_grad.setColorAt(0.0, QColor(255, 255, 255, 52))
        border_grad.setColorAt(1.0, QColor(255, 255, 255, 16))
        painter.setPen(QPen(border_grad, 1.1))
        painter.drawPath(path)

        # Draw iOS-style Privacy Dots (Orange = Mic, Green = Webcam) in top-right corner of Island
        self._draw_privacy_dots(painter, pill)

        compact_opacity = max(0.0, 1.0 - prog * 2.2)
        if compact_opacity > 0.01:
            painter.save()
            painter.setOpacity(compact_opacity)
            self._draw_compact_hud(painter, pill)
            painter.restore()

    def _draw_privacy_dots(self, painter: QPainter, pill: QRectF) -> None:
        dot_x = pill.right() - 34.0
        dot_y = pill.top() + 8.0
        painter.setPen(Qt.PenStyle.NoPen)
        if self._cam_active:
            painter.setBrush(QColor("#22C55E"))  # iOS Camera Green
            painter.drawEllipse(QPointF(dot_x, dot_y), 3.5, 3.5)
            dot_x -= 9.0
        if self._mic_active:
            painter.setBrush(QColor("#F97316"))  # iOS Microphone Orange
            painter.drawEllipse(QPointF(dot_x, dot_y), 3.5, 3.5)

    def _draw_compact_hud(self, painter: QPainter, pill: QRectF) -> None:
        font = QFont("Segoe UI Variable Display", 10)
        font.setWeight(QFont.Weight.DemiBold)
        painter.setFont(font)
        painter.setPen(QColor(245, 246, 250))

        # Live Activity transient popup mode
        if self.state == IslandState.LIVE_ACTIVITY and self._live_banner_text:
            fm = painter.fontMetrics()
            elided = fm.elidedText(
                self._live_banner_text, Qt.TextElideMode.ElideRight, int(pill.width() - 32)
            )
            painter.drawText(pill, int(Qt.AlignmentFlag.AlignCenter), elided)
            return

        # Left side: Cover Art (when playing) + Time / Pomodoro / Battery
        left_offset = pill.left() + 12.0
        if self._media_state.is_playing or self._media_state.cover_bytes:
            cy = int(pill.center().y() - 12)
            painter.drawPixmap(int(left_offset), cy, self._compact_cover)
            left_offset += 30.0

        if self._pomodoro_compact_text:
            label_text = f"⏱ {self._pomodoro_compact_text}"
            painter.setPen(QColor("#38BDF8"))
        else:
            label_text = self._clock_text
            if self._has_battery:
                bolt = "⚡" if self._battery_charging else ""
                label_text = f"{self._clock_text} • {bolt}{self._battery_pct}%"

        left_rect = QRectF(left_offset, pill.top(), 110.0, pill.height())
        painter.drawText(
            left_rect,
            int(Qt.AlignmentFlag.AlignVCenter | Qt.AlignmentFlag.AlignLeft),
            label_text,
        )

        # Center camera lens aesthetic detail
        lens_center = QPointF(pill.center().x() + 14.0, pill.center().y())
        painter.setBrush(QColor(18, 20, 28, 255))
        painter.setPen(QPen(QColor(45, 52, 70, 140), 0.8))
        painter.drawEllipse(lens_center, 4.8, 4.8)

        # Right side: 5-bar live equalizer
        bar_count = 5
        bar_w = 2.8
        bar_gap = 2.5
        total_w = bar_count * bar_w + (bar_count - 1) * bar_gap
        start_x = pill.right() - 15.0 - total_w
        center_y = pill.center().y()

        painter.setPen(Qt.PenStyle.NoPen)
        amp = 11.5 if self._media_state.is_playing else 3.5
        for i in range(bar_count):
            wave = math.sin(self._eq_phase + i * 0.85) * 0.5 + 0.5
            bar_h = 3.5 + wave * amp
            bx = start_x + i * (bar_w + bar_gap)
            by = center_y - bar_h / 2.0
            color = QColor.fromHsvF((0.55 + i * 0.06) % 1.0, 0.75, 0.98)
            painter.setBrush(color)
            painter.drawRoundedRect(QRectF(bx, by, bar_w, bar_h), 1.4, 1.4)


def main() -> int:
    QGuiApplication.setHighDpiScaleFactorRoundingPolicy(
        Qt.HighDpiScaleFactorRoundingPolicy.PassThrough
    )
    app = QApplication(sys.argv)
    app.setQuitOnLastWindowClosed(False)

    island = DynamicIslandWindow()
    island.show()
    return app.exec()


if __name__ == "__main__":
    sys.exit(main())

