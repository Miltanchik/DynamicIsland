# Language: Python 3.11+ | File: dynamic_island/stage1_island_window.py | Runtime: PyQt6 >= 6.6.0 on Windows 10/11
r"""
Stage 1 Core Foundation: Native Win32 Frameless Overlay, Dynamic Click-Through & Spring Physics.
Purpose:
  Implements the primary TopMost Dynamic Island capsule overlay for Windows 10/11 with:
  - Per-pixel alpha compositing (WA_TranslucentBackground + WS_EX_LAYERED)
  - Zero taskbar / Alt+Tab footprint (Qt.WindowType.Tool + WS_EX_TOOLWINDOW | WS_EX_NOACTIVATE)
  - Dynamic Win32 hit-test passthrough (WS_EX_TRANSPARENT toggled in O(1) on 60 FPS cursor check)
  - OutBack spring physics morphing between IDLE, LIVE_ACTIVITY, and EXPANDED states
  - Multi-monitor centering, native Kernel32 GetSystemPowerStatus battery telemetry, and live equalizer HUD
"""

from __future__ import annotations

import ctypes
from ctypes import wintypes
from dataclasses import dataclass
from enum import Enum, auto
import math
import sys
import time
from typing import Callable, Final

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
    QFont,
    QGuiApplication,
    QLinearGradient,
    QMouseEvent,
    QPainter,
    QPainterPath,
    QPen,
    QScreen,
)
from PyQt6.QtWidgets import (
    QApplication,
    QGraphicsOpacityEffect,
    QHBoxLayout,
    QLabel,
    QPushButton,
    QVBoxLayout,
    QWidget,
)

GWL_EXSTYLE: Final[int] = -20
WS_EX_LAYERED: Final[int] = 0x00080000
WS_EX_TRANSPARENT: Final[int] = 0x00000020
WS_EX_TOOLWINDOW: Final[int] = 0x00000080
WS_EX_NOACTIVATE: Final[int] = 0x08000000

HWND_TOPMOST: Final[int] = -1
SWP_NOSIZE: Final[int] = 0x0001
SWP_NOMOVE: Final[int] = 0x0002
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


def query_system_battery() -> tuple[int, bool, bool]:
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


@dataclass(slots=True)
class IslandGeometrySpec:
    canvas_width: int = 580
    canvas_height: int = 260
    top_margin: int = 8

    idle_width: float = 224.0
    idle_height: float = 36.0

    activity_width: float = 330.0
    activity_height: float = 46.0

    expanded_width: float = 490.0
    expanded_height: float = 196.0

    collapse_delay_ms: int = 400


class IslandSpringAnimator:
    def __init__(self, spec: IslandGeometrySpec, on_frame: Callable[[], None]) -> None:
        self._on_frame = on_frame
        self.current_w: float = spec.idle_width
        self.current_h: float = spec.idle_height
        self.progress: float = 0.0

        self._start_w: float = self.current_w
        self._start_h: float = self.current_h
        self._target_w: float = self.current_w
        self._target_h: float = self.current_h
        self._start_prog: float = 0.0
        self._target_prog: float = 0.0

        self._anim = QVariantAnimation()
        self._anim.setStartValue(0.0)
        self._anim.setEndValue(1.0)
        self._anim.valueChanged.connect(self._on_value_changed)

    def morph_to(
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
        self._start_prog = self.progress
        self._target_w = target_w
        self._target_h = target_h
        self._target_prog = target_prog

        curve = QEasingCurve(
            QEasingCurve.Type.OutBack if expanding else QEasingCurve.Type.OutCubic
        )
        if expanding:
            curve.setOvershoot(1.22)

        self._anim.setDuration(duration_ms)
        self._anim.setEasingCurve(curve)
        self._anim.start()

    def _on_value_changed(self, value: float) -> None:
        t = float(value)
        self.current_w = self._start_w + (self._target_w - self._start_w) * t
        self.current_h = self._start_h + (self._target_h - self._start_h) * t
        raw_prog = self._start_prog + (self._target_prog - self._start_prog) * t
        self.progress = max(0.0, min(1.0, raw_prog))
        self._on_frame()


class Stage1DynamicIslandWindow(QWidget):
    state_changed = pyqtSignal(IslandState)

    def __init__(self, monitor_index: int = 0) -> None:
        super().__init__(None)
        self.spec = IslandGeometrySpec()
        self.state = IslandState.IDLE
        self._monitor_index = monitor_index

        self._click_through_enabled: bool = False
        self._cursor_inside_pill: bool = False
        self._eq_phase: float = 0.0
        self._live_activity_banner: str = ""

        self._clock_str: str = time.strftime("%H:%M")
        self._bat_pct, self._bat_charging, self._has_bat = query_system_battery()

        self._init_window_attributes()
        self._init_expanded_placeholder_ui()

        self.animator = IslandSpringAnimator(self.spec, self._on_spring_frame)

        self._collapse_timer = QTimer(self)
        self._collapse_timer.setSingleShot(True)
        self._collapse_timer.setInterval(self.spec.collapse_delay_ms)
        self._collapse_timer.timeout.connect(self.collapse_to_idle)

        self._frame_timer = QTimer(self)
        self._frame_timer.setInterval(16)
        self._frame_timer.timeout.connect(self._on_frame_tick)
        self._frame_timer.start()

        self._telemetry_timer = QTimer(self)
        self._telemetry_timer.setInterval(2000)
        self._telemetry_timer.timeout.connect(self._refresh_clock_and_battery)
        self._telemetry_timer.start()

        self.move_to_monitor(self._monitor_index)

    def _init_window_attributes(self) -> None:
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
        self.setFixedSize(self.spec.canvas_width, self.spec.canvas_height)

    def showEvent(self, event) -> None:
        super().showEvent(event)
        self._enforce_win32_overlay_styles()

    def _enforce_win32_overlay_styles(self) -> None:
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

    def _set_win32_click_through(self, passthrough: bool) -> None:
        if self._click_through_enabled == passthrough:
            return
        self._click_through_enabled = passthrough
        hwnd = int(self.winId())
        ex_style = GetWindowLongPtrW(hwnd, GWL_EXSTYLE)
        if passthrough:
            ex_style |= WS_EX_TRANSPARENT
        else:
            ex_style &= ~WS_EX_TRANSPARENT
        SetWindowLongPtrW(hwnd, GWL_EXSTYLE, ex_style)

    def move_to_monitor(self, monitor_index: int = 0) -> None:
        screens: list[QScreen] = QGuiApplication.screens()
        if not screens:
            return
        idx = max(0, min(monitor_index, len(screens) - 1))
        self._monitor_index = idx
        geom = screens[idx].geometry()
        x = geom.x() + (geom.width() - self.spec.canvas_width) // 2
        y = geom.y()
        self.move(x, y)

    def current_capsule_rect(self) -> QRectF:
        w = self.animator.current_w
        h = self.animator.current_h
        x = (self.spec.canvas_width - w) / 2.0
        y = float(self.spec.top_margin)
        return QRectF(x, y, w, h)

    def _init_expanded_placeholder_ui(self) -> None:
        self.expanded_panel = QWidget(self)
        self.expanded_panel.setAttribute(Qt.WidgetAttribute.WA_TranslucentBackground, True)
        self._panel_opacity = QGraphicsOpacityEffect(self.expanded_panel)
        self._panel_opacity.setOpacity(0.0)
        self.expanded_panel.setGraphicsEffect(self._panel_opacity)
        self.expanded_panel.hide()

        layout = QVBoxLayout(self.expanded_panel)
        layout.setContentsMargins(22, 16, 22, 16)
        layout.setSpacing(10)

        header_row = QHBoxLayout()
        title_lbl = QLabel("Dynamic Island • Windows 10/11 Stage 1")
        title_lbl.setStyleSheet("color: #F9FAFB; font-size: 13px; font-weight: 700;")
        status_lbl = QLabel("60 FPS Spring • Click-Through Ready")
        status_lbl.setStyleSheet("color: #38BDF8; font-size: 11px; font-weight: 600;")
        header_row.addWidget(title_lbl)
        header_row.addStretch()
        header_row.addWidget(status_lbl)
        layout.addLayout(header_row)

        desc_lbl = QLabel(
            "Stage 1 Foundation verified: OutBack spring physics, WS_EX_TRANSPARENT hit-test "
            "passthrough around the capsule, 400ms collapse debounce, and Live Activity morphing."
        )
        desc_lbl.setWordWrap(True)
        desc_lbl.setStyleSheet("color: #9CA3AF; font-size: 11px;")
        layout.addWidget(desc_lbl)

        btn_row = QHBoxLayout()
        btn_style = (
            "QPushButton {"
            "  background-color: rgba(255, 255, 255, 20); color: #F3F4F6;"
            "  border: 1px solid rgba(255, 255, 255, 35); border-radius: 10px;"
            "  padding: 5px 12px; font-size: 11px; font-weight: 600;"
            "}"
            "QPushButton:hover { background-color: rgba(255, 255, 255, 42); }"
        )

        btn_demo_activity = QPushButton("⚡ Test Live Activity")
        btn_demo_activity.setCursor(Qt.CursorShape.PointingHandCursor)
        btn_demo_activity.setStyleSheet(btn_style)
        btn_demo_activity.clicked.connect(
            lambda: self.trigger_live_activity("🔊 Volume 78%  •  Bluetooth Connected")
        )

        btn_switch_mon = QPushButton("🖥 Next Monitor")
        btn_switch_mon.setCursor(Qt.CursorShape.PointingHandCursor)
        btn_switch_mon.setStyleSheet(btn_style)
        btn_switch_mon.clicked.connect(self._cycle_monitor)

        btn_quit = QPushButton("✕ Quit")
        btn_quit.setCursor(Qt.CursorShape.PointingHandCursor)
        btn_quit.setStyleSheet(btn_style)
        btn_quit.clicked.connect(QApplication.instance().quit)

        btn_row.addWidget(btn_demo_activity)
        btn_row.addWidget(btn_switch_mon)
        btn_row.addStretch()
        btn_row.addWidget(btn_quit)
        layout.addLayout(btn_row)

    def _cycle_monitor(self) -> None:
        screens = QGuiApplication.screens()
        if len(screens) > 1:
            self.move_to_monitor((self._monitor_index + 1) % len(screens))

    def expand(self) -> None:
        self._collapse_timer.stop()
        self._live_activity_banner = ""
        if self.state == IslandState.EXPANDED:
            return
        self.state = IslandState.EXPANDED
        self.expanded_panel.show()
        self.animator.morph_to(
            self.spec.expanded_width,
            self.spec.expanded_height,
            target_prog=1.0,
            duration_ms=400,
            expanding=True,
        )
        self.state_changed.emit(self.state)

    def collapse_to_idle(self) -> None:
        if self._cursor_inside_pill:
            return
        self._live_activity_banner = ""
        self.state = IslandState.IDLE
        self.animator.morph_to(
            self.spec.idle_width,
            self.spec.idle_height,
            target_prog=0.0,
            duration_ms=320,
            expanding=False,
        )
        self.state_changed.emit(self.state)

    def trigger_live_activity(self, banner_text: str, duration_ms: int = 1800) -> None:
        self._cursor_inside_pill = False
        self._live_activity_banner = banner_text
        self.state = IslandState.LIVE_ACTIVITY
        self.animator.morph_to(
            self.spec.activity_width,
            self.spec.activity_height,
            target_prog=0.0,
            duration_ms=340,
            expanding=True,
        )
        self._collapse_timer.start(duration_ms)
        self.state_changed.emit(self.state)

    def mousePressEvent(self, event: QMouseEvent) -> None:
        if self.current_capsule_rect().contains(QPointF(event.position())):
            if event.button() == Qt.MouseButton.LeftButton and self.state != IslandState.EXPANDED:
                self.expand()
        super().mousePressEvent(event)

    def _on_frame_tick(self) -> None:
        global_cursor: QPoint = QCursor.pos()
        local_cursor = QPointF(self.mapFromGlobal(global_cursor))
        hit_rect = self.current_capsule_rect().adjusted(-4.0, -4.0, 4.0, 4.0)
        inside = hit_rect.contains(local_cursor)

        self._set_win32_click_through(not inside)

        if inside and not self._cursor_inside_pill:
            self._cursor_inside_pill = True
            self._collapse_timer.stop()
            self.expand()
        elif not inside and self._cursor_inside_pill:
            self._cursor_inside_pill = False
            self._collapse_timer.start(self.spec.collapse_delay_ms)

        if self.animator.progress < 0.85:
            self._eq_phase += 0.18
            self.update()

    def _on_spring_frame(self) -> None:
        pill = self.current_capsule_rect()
        self.expanded_panel.setGeometry(pill.toRect())
        prog = self.animator.progress
        panel_alpha = max(0.0, min(1.0, (prog - 0.35) / 0.65))
        self._panel_opacity.setOpacity(panel_alpha)
        if prog <= 0.05 and self.state != IslandState.EXPANDED:
            self.expanded_panel.hide()
        self.update()

    def _refresh_clock_and_battery(self) -> None:
        self._clock_str = time.strftime("%H:%M")
        self._bat_pct, self._bat_charging, self._has_bat = query_system_battery()
        self.update()

    def paintEvent(self, event) -> None:
        painter = QPainter(self)
        painter.setRenderHint(QPainter.RenderHint.Antialiasing, True)
        painter.setRenderHint(QPainter.RenderHint.TextAntialiasing, True)

        pill = self.current_capsule_rect()
        prog = self.animator.progress
        radius = (pill.height() / 2.0) * (1.0 - prog) + 32.0 * prog

        path = QPainterPath()
        path.addRoundedRect(pill, radius, radius)

        painter.fillPath(path, QColor(7, 7, 9, 250))

        rim_grad = QLinearGradient(pill.topLeft(), pill.bottomLeft())
        rim_grad.setColorAt(0.0, QColor(255, 255, 255, 55))
        rim_grad.setColorAt(1.0, QColor(255, 255, 255, 15))
        painter.setPen(QPen(rim_grad, 1.1))
        painter.drawPath(path)

        compact_alpha = max(0.0, 1.0 - prog * 2.2)
        if compact_alpha > 0.01:
            painter.save()
            painter.setOpacity(compact_alpha)
            self._paint_compact_capsule(painter, pill)
            painter.restore()

    def _paint_compact_capsule(self, painter: QPainter, pill: QRectF) -> None:
        font = QFont("Segoe UI Variable Display", 10)
        font.setWeight(QFont.Weight.DemiBold)
        painter.setFont(font)
        painter.setPen(QColor(245, 246, 250))

        if self.state == IslandState.LIVE_ACTIVITY and self._live_activity_banner:
            painter.drawText(pill, int(Qt.AlignmentFlag.AlignCenter), self._live_activity_banner)
            return

        left_label = self._clock_str
        if self._has_bat:
            bolt = "⚡" if self._bat_charging else ""
            left_label = f"{self._clock_str} • {bolt}{self._bat_pct}%"

        left_rect = QRectF(pill.left() + 16.0, pill.top(), 115.0, pill.height())
        painter.drawText(
            left_rect,
            int(Qt.AlignmentFlag.AlignVCenter | Qt.AlignmentFlag.AlignLeft),
            left_label,
        )

        lens_pt = QPointF(pill.center().x() + 12.0, pill.center().y())
        painter.setBrush(QColor(18, 20, 28, 255))
        painter.setPen(QPen(QColor(48, 56, 78, 150), 0.9))
        painter.drawEllipse(lens_pt, 4.8, 4.8)

        bar_count = 5
        bar_w = 2.8
        bar_gap = 2.5
        total_w = bar_count * bar_w + (bar_count - 1) * bar_gap
        start_x = pill.right() - 16.0 - total_w
        cy = pill.center().y()

        painter.setPen(Qt.PenStyle.NoPen)
        for i in range(bar_count):
            wave = math.sin(self._eq_phase + i * 0.85) * 0.5 + 0.5
            bar_h = 4.0 + wave * 10.5
            bx = start_x + i * (bar_w + bar_gap)
            by = cy - bar_h / 2.0
            painter.setBrush(QColor.fromHsvF((0.55 + i * 0.06) % 1.0, 0.75, 0.98))
            painter.drawRoundedRect(QRectF(bx, by, bar_w, bar_h), 1.4, 1.4)


def main() -> int:
    QGuiApplication.setHighDpiScaleFactorRoundingPolicy(
        Qt.HighDpiScaleFactorRoundingPolicy.PassThrough
    )
    app = QApplication(sys.argv)
    window = Stage1DynamicIslandWindow(monitor_index=0)
    window.show()
    return app.exec()


if __name__ == "__main__":
    sys.exit(main())