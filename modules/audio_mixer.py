# Language: Python 3.11+ | File: dynamic_island/modules/audio_mixer.py | Runtime: pycaw / CoreAudio COM
"""
Stage 2: Windows Core Audio Endpoint Mixer & Real-time Hardware Volume Hook.
"""

from __future__ import annotations

import ctypes
from typing import Optional

from PyQt6.QtCore import Qt, QTimer, pyqtSignal, QObject
from PyQt6.QtGui import QWheelEvent
from PyQt6.QtWidgets import QHBoxLayout, QLabel, QPushButton, QSlider, QWidget

try:
    import comtypes
    from comtypes import CLSCTX_ALL, COMObject
    from pycaw.pycaw import AudioUtilities, IAudioEndpointVolume, IAudioEndpointVolumeCallback
    HAS_PYCAW = True
except Exception:
    HAS_PYCAW = False


if HAS_PYCAW:
    class _EndpointVolumeCallback(COMObject):
        _com_interfaces_ = [IAudioEndpointVolumeCallback]

        def __init__(self, bridge: "CoreAudioController") -> None:
            super().__init__()
            self._bridge = bridge

        def OnNotify(self, pNotify) -> int:
            try:
                data = ctypes.cast(
                    pNotify,
                    ctypes.POINTER(comtypes.gen.mmdeviceapi.AUDIO_VOLUME_NOTIFICATION_DATA),
                ).contents
                vol_pct = int(round(float(data.fMasterVolume) * 100.0))
                muted = bool(data.bMuted)
                self._bridge.notify_from_com(vol_pct, muted)
            except Exception:
                self._bridge.request_resync()
            return 0


class CoreAudioController(QObject):
    volume_changed = pyqtSignal(int, bool, bool)

    def __init__(self, parent: Optional[QObject] = None) -> None:
        super().__init__(parent)
        self._endpoint = None
        self._callback_ref = None
        self._last_vol: int = 50
        self._last_mute: bool = False
        self._internal_change_guard: bool = False

        self._init_endpoint()

        self._sync_timer = QTimer(self)
        self._sync_timer.setInterval(300)
        self._sync_timer.timeout.connect(self._poll_sync)
        self._sync_timer.start()

    def _init_endpoint(self) -> None:
        if not HAS_PYCAW:
            return
        try:
            comtypes.CoInitialize()
        except Exception:
            pass
        try:
            devices = AudioUtilities.GetSpeakers()
            interface = devices.Activate(IAudioEndpointVolume._iid_, CLSCTX_ALL, None)
            self._endpoint = ctypes.cast(interface, ctypes.POINTER(IAudioEndpointVolume))
            self._last_vol = self.get_volume()
            self._last_mute = self.is_muted()
            try:
                self._callback_ref = _EndpointVolumeCallback(self)
                self._endpoint.RegisterControlChangeNotify(self._callback_ref)
            except Exception:
                self._callback_ref = None
        except Exception:
            self._endpoint = None

    def get_volume(self) -> int:
        if not self._endpoint:
            return self._last_vol
        try:
            scalar = float(self._endpoint.GetMasterVolumeLevelScalar())
            return max(0, min(100, int(round(scalar * 100.0))))
        except Exception:
            self._init_endpoint()
            return self._last_vol

    def is_muted(self) -> bool:
        if not self._endpoint:
            return self._last_mute
        try:
            return bool(self._endpoint.GetMute())
        except Exception:
            return self._last_mute

    def set_volume(self, percent: int, external_trigger: bool = False) -> None:
        clamped = max(0, min(100, int(percent)))
        self._internal_change_guard = not external_trigger
        self._last_vol = clamped
        if self._endpoint:
            try:
                self._endpoint.SetMasterVolumeLevelScalar(clamped / 100.0, None)
                if clamped > 0 and self._last_mute:
                    self._endpoint.SetMute(0, None)
                    self._last_mute = False
            except Exception:
                self._init_endpoint()
        self.volume_changed.emit(self._last_vol, self._last_mute, external_trigger)
        self._internal_change_guard = False

    def toggle_mute(self) -> bool:
        new_mute = not self.is_muted()
        self._internal_change_guard = True
        self._last_mute = new_mute
        if self._endpoint:
            try:
                self._endpoint.SetMute(1 if new_mute else 0, None)
            except Exception:
                self._init_endpoint()
        self.volume_changed.emit(self._last_vol, self._last_mute, False)
        self._internal_change_guard = False
        return self._last_mute

    def step_volume(self, delta_steps: int, external_trigger: bool = False) -> None:
        self.set_volume(self._last_vol + delta_steps, external_trigger=external_trigger)

    def notify_from_com(self, vol_pct: int, muted: bool) -> None:
        if vol_pct != self._last_vol or muted != self._last_mute:
            self._last_vol = vol_pct
            self._last_mute = muted
            if not self._internal_change_guard:
                self.volume_changed.emit(vol_pct, muted, True)

    def request_resync(self) -> None:
        QTimer.singleShot(0, self._poll_sync)

    def _poll_sync(self) -> None:
        if not self._endpoint:
            self._init_endpoint()
            return
        cur_vol = self.get_volume()
        cur_mute = self.is_muted()
        if cur_vol != self._last_vol or cur_mute != self._last_mute:
            self._last_vol = cur_vol
            self._last_mute = cur_mute
            if not self._internal_change_guard:
                self.volume_changed.emit(cur_vol, cur_mute, True)


def format_volume_icon(percent: int, muted: bool) -> str:
    if muted or percent == 0:
        return "🔇"
    if percent < 35:
        return "🔈"
    if percent < 70:
        return "🔉"
    return "🔊"


class VolumeMixerWidget(QWidget):
    def __init__(self, controller: CoreAudioController, parent: Optional[QWidget] = None) -> None:
        super().__init__(parent)
        self.controller = controller
        self._updating_ui = False

        layout = QHBoxLayout(self)
        layout.setContentsMargins(8, 2, 8, 2)
        layout.setSpacing(8)

        self.btn_mute = QPushButton("🔊")
        self.btn_mute.setFixedSize(26, 24)
        self.btn_mute.setCursor(Qt.CursorShape.PointingHandCursor)
        self.btn_mute.setStyleSheet(
            "QPushButton { background-color: rgba(255,255,255,18); color: #FFF; border-radius: 12px; font-size: 12px; border: none; }"
            "QPushButton:hover { background-color: rgba(255,255,255,38); }"
        )
        self.btn_mute.clicked.connect(self.controller.toggle_mute)

        self.slider = QSlider(Qt.Orientation.Horizontal)
        self.slider.setRange(0, 100)
        self.slider.setCursor(Qt.CursorShape.PointingHandCursor)
        self.slider.setStyleSheet(
            "QSlider::groove:horizontal { height: 5px; background: rgba(255,255,255,28); border-radius: 2px; }"
            "QSlider::sub-page:horizontal { background: qlineargradient(x1:0,y1:0,x2:1,y2:0,stop:0 #38BDF8,stop:1 #818CF8); border-radius: 2px; }"
            "QSlider::handle:horizontal { background: #FFF; width: 13px; height: 13px; margin: -4px 0; border-radius: 6px; }"
        )
        self.slider.valueChanged.connect(self._on_slider_moved)

        self.lbl_pct = QLabel("50%")
        self.lbl_pct.setFixedWidth(36)
        self.lbl_pct.setAlignment(Qt.AlignmentFlag.AlignRight | Qt.AlignmentFlag.AlignVCenter)
        self.lbl_pct.setStyleSheet("color: #E5E7EB; font-size: 11px; font-weight: 600;")

        layout.addWidget(self.btn_mute)
        layout.addWidget(self.slider, stretch=1)
        layout.addWidget(self.lbl_pct)

        self.controller.volume_changed.connect(self._sync_from_controller)
        self._sync_from_controller(self.controller.get_volume(), self.controller.is_muted(), False)

    def _on_slider_moved(self, value: int) -> None:
        if not self._updating_ui:
            self.controller.set_volume(value, external_trigger=False)

    def _sync_from_controller(self, percent: int, muted: bool, _external: bool) -> None:
        self._updating_ui = True
        self.slider.setValue(percent)
        self.btn_mute.setText(format_volume_icon(percent, muted))
        self.lbl_pct.setText("MUTE" if muted else f"{percent}%")
        self._updating_ui = False

    def wheelEvent(self, event: QWheelEvent) -> None:
        delta = event.angleDelta().y()
        if delta != 0:
            self.controller.step_volume(2 if delta > 0 else -2, external_trigger=False)
            event.accept()
