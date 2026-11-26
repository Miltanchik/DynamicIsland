# Language: Python 3.11+ | File: dynamic_island/modules/smtc_player.py | Runtime: winsdk / WinRT SMTC
"""
Stage 3: Windows System Media Transport Controls (SMTC) Integration.
"""

from __future__ import annotations

import asyncio
from dataclasses import dataclass
import threading
from typing import Optional

from PyQt6.QtCore import Qt, pyqtSignal, QObject, QRectF
from PyQt6.QtGui import (
    QColor,
    QImage,
    QLinearGradient,
    QMouseEvent,
    QPainter,
    QPainterPath,
    QPen,
    QPixmap,
)
from PyQt6.QtWidgets import QHBoxLayout, QLabel, QPushButton, QVBoxLayout, QWidget

try:
    from winsdk.windows.media.control import (
        GlobalSystemMediaTransportControlsSessionManager as MediaManager,
        GlobalSystemMediaTransportControlsSessionPlaybackStatus as PlaybackStatus,
    )
    from winsdk.windows.storage.streams import Buffer, DataReader, InputStreamOptions
    HAS_WINSDK = True
except Exception:
    try:
        from winrt.windows.media.control import (
            GlobalSystemMediaTransportControlsSessionManager as MediaManager,
            GlobalSystemMediaTransportControlsSessionPlaybackStatus as PlaybackStatus,
        )
        from winrt.windows.storage.streams import Buffer, DataReader, InputStreamOptions
        HAS_WINSDK = True
    except Exception:
        HAS_WINSDK = False


@dataclass
class MediaTrackState:
    title: str = "No Media Playing"
    artist: str = "Spotify • Yandex Music • Browser"
    app_id: str = ""
    is_playing: bool = False
    position_sec: float = 0.0
    duration_sec: float = 0.0
    cover_bytes: bytes = b""


class ClickableProgressBar(QWidget):
    seek_requested = pyqtSignal(float)

    def __init__(self, parent: Optional[QWidget] = None) -> None:
        super().__init__(parent)
        self.setFixedHeight(12)
        self.setCursor(Qt.CursorShape.PointingHandCursor)
        self._progress: float = 0.0

    def set_progress(self, ratio: float) -> None:
        clamped = max(0.0, min(1.0, float(ratio)))
        if abs(clamped - self._progress) > 0.002:
            self._progress = clamped
            self.update()

    def mousePressEvent(self, event: QMouseEvent) -> None:
        if event.button() == Qt.MouseButton.LeftButton and self.width() > 0:
            ratio = max(0.0, min(1.0, event.position().x() / float(self.width())))
            self.set_progress(ratio)
            self.seek_requested.emit(ratio)
        super().mousePressEvent(event)

    def paintEvent(self, event) -> None:
        painter = QPainter(self)
        painter.setRenderHint(QPainter.RenderHint.Antialiasing, True)
        groove = QRectF(0.0, 3.5, float(self.width()), 5.0)
        painter.setPen(Qt.PenStyle.NoPen)
        painter.setBrush(QColor(255, 255, 255, 32))
        painter.drawRoundedRect(groove, 2.5, 2.5)

        fill_w = max(5.0, groove.width() * self._progress) if self._progress > 0 else 0.0
        if fill_w > 0:
            grad = QLinearGradient(groove.topLeft(), groove.topRight())
            grad.setColorAt(0.0, QColor("#38BDF8"))
            grad.setColorAt(1.0, QColor("#A855F7"))
            painter.setBrush(grad)
            painter.drawRoundedRect(QRectF(0.0, 3.5, fill_w, 5.0), 2.5, 2.5)


class SMTCController(QObject):
    track_updated = pyqtSignal(object)
    track_switched = pyqtSignal(object)

    def __init__(self, parent: Optional[QObject] = None) -> None:
        super().__init__(parent)
        self._state = MediaTrackState()
        self._last_track_key: str = ""
        self._cached_cover: bytes = b""
        self._loop: Optional[asyncio.AbstractEventLoop] = None
        self._cmd_queue: Optional[asyncio.Queue[tuple[str, float]]] = None
        self._running = True

        self._thread = threading.Thread(target=self._run_async_loop, daemon=True)
        self._thread.start()

    def send_command(self, action: str, value: float = 0.0) -> None:
        if self._loop and self._cmd_queue:
            self._loop.call_soon_threadsafe(self._cmd_queue.put_nowait, (action, value))

    def _run_async_loop(self) -> None:
        self._loop = asyncio.new_event_loop()
        asyncio.set_event_loop(self._loop)
        self._cmd_queue = asyncio.Queue()
        self._loop.run_until_complete(self._worker())

    async def _worker(self) -> None:
        while self._running:
            try:
                while self._cmd_queue and not self._cmd_queue.empty():
                    action, val = await self._cmd_queue.get()
                    await self._execute_transport(action, val)
                if HAS_WINSDK:
                    await self._poll_smtc_once()
            except Exception:
                pass
            await asyncio.sleep(0.75)

    async def _execute_transport(self, action: str, val: float) -> None:
        if not HAS_WINSDK:
            if action == "play_pause":
                self._state.is_playing = not self._state.is_playing
                self.track_updated.emit(self._state)
            return
        try:
            manager = await MediaManager.request_async()
            session = manager.get_current_session()
            if not session:
                return
            if action == "play_pause":
                await session.try_toggle_play_pause_async()
            elif action == "next":
                await session.try_skip_next_async()
            elif action == "prev":
                await session.try_skip_previous_async()
            elif action == "seek_ratio":
                timeline = session.get_timeline_properties()
                if timeline and timeline.end_time:
                    dur_sec = timeline.end_time.total_seconds()
                    target_ticks = int(dur_sec * val * 10_000_000)
                    await session.try_change_playback_position_async(target_ticks)
        except Exception:
            pass

    async def _poll_smtc_once(self) -> None:
        manager = await MediaManager.request_async()
        session = manager.get_current_session()
        if not session:
            empty = MediaTrackState()
            if self._state.title != empty.title or self._state.is_playing:
                self._state = empty
                self.track_updated.emit(empty)
            return

        playback_info = session.get_playback_info()
        is_playing = bool(playback_info and playback_info.playback_status == PlaybackStatus.PLAYING)

        props = await session.try_get_media_properties_async()
        title = (props.title if props and props.title else "Unknown Track").strip()
        artist = (props.artist if props and props.artist else (session.source_app_user_model_id or "System Audio")).strip()

        pos_sec = 0.0
        dur_sec = 0.0
        try:
            timeline = session.get_timeline_properties()
            if timeline:
                if timeline.position:
                    pos_sec = max(0.0, float(timeline.position.total_seconds()))
                if timeline.end_time:
                    dur_sec = max(0.0, float(timeline.end_time.total_seconds()))
                if is_playing and dur_sec > 0 and getattr(timeline, "last_updated_time", None):
                    from datetime import datetime, timezone
                    elapsed = (datetime.now(timezone.utc) - timeline.last_updated_time).total_seconds()
                    if 0.0 < elapsed <= dur_sec + 10.0:
                        pos_sec = min(dur_sec, pos_sec + elapsed)
        except Exception:
            pass

        track_key = f"{title}::{artist}"
        if track_key != self._last_track_key:
            self._cached_cover = await self._read_thumbnail_bytes(props.thumbnail if props else None)

        new_state = MediaTrackState(
            title=title,
            artist=artist,
            app_id=session.source_app_user_model_id or "",
            is_playing=is_playing,
            position_sec=pos_sec,
            duration_sec=dur_sec,
            cover_bytes=self._cached_cover,
        )

        switched = track_key != self._last_track_key and bool(title) and title != "No Media Playing"
        self._last_track_key = track_key
        self._state = new_state
        self.track_updated.emit(new_state)
        if switched:
            self.track_switched.emit(new_state)

    async def _read_thumbnail_bytes(self, thumb_ref) -> bytes:
        if not thumb_ref:
            return b""
        try:
            stream = await thumb_ref.open_read_async()
            size = int(stream.size)
            if size <= 0 or size > 8_000_000:
                return b""
            buf = Buffer(size)
            await stream.read_async(buf, size, InputStreamOptions.READ_AHEAD)
            reader = DataReader.from_buffer(buf)
            raw = bytearray(size)
            reader.read_bytes(raw)
            return bytes(raw)
        except Exception:
            return b""


def make_rounded_cover(raw_bytes: bytes, size: int = 42, radius: int = 10) -> QPixmap:
    target = QPixmap(size, size)
    target.fill(Qt.GlobalColor.transparent)

    painter = QPainter(target)
    painter.setRenderHint(QPainter.RenderHint.Antialiasing, True)
    path = QPainterPath()
    path.addRoundedRect(QRectF(0, 0, size, size), radius, radius)
    painter.setClipPath(path)

    img = QImage.fromData(raw_bytes) if raw_bytes else QImage()
    if not img.isNull():
        scaled = QPixmap.fromImage(img).scaled(
            size,
            size,
            Qt.AspectRatioMode.KeepAspectRatioByExpanding,
            Qt.TransformationMode.SmoothTransformation,
        )
        painter.drawPixmap(0, 0, scaled)
    else:
        grad = QLinearGradient(0, 0, size, size)
        grad.setColorAt(0.0, QColor("#1E1B4B"))
        grad.setColorAt(0.5, QColor("#4338CA"))
        grad.setColorAt(1.0, QColor("#0EA5E9"))
        painter.fillPath(path, grad)
        painter.setPen(QColor(255, 255, 255, 220))
        font = painter.font()
        font.setPointSize(max(7, size // 3))
        painter.setFont(font)
        painter.drawText(target.rect(), int(Qt.AlignmentFlag.AlignCenter), "♫")

    painter.setClipping(False)
    painter.setPen(QPen(QColor(255, 255, 255, 45), 1.0))
    painter.drawPath(path)
    painter.end()
    return target


def _fmt_mmss(sec: float) -> str:
    if sec <= 0:
        return "0:00"
    return f"{int(sec) // 60}:{int(sec) % 60:02d}"


class SMTCMediaWidget(QWidget):
    def __init__(self, controller: SMTCController, parent: Optional[QWidget] = None) -> None:
        super().__init__(parent)
        self.controller = controller

        root = QVBoxLayout(self)
        root.setContentsMargins(6, 2, 6, 2)
        root.setSpacing(4)

        top = QHBoxLayout()
        top.setSpacing(10)

        self.lbl_cover = QLabel()
        self.lbl_cover.setFixedSize(42, 42)
        self.lbl_cover.setPixmap(make_rounded_cover(b"", 42, 10))
        top.addWidget(self.lbl_cover)

        meta_col = QVBoxLayout()
        meta_col.setSpacing(1)
        self.lbl_title = QLabel("No Media Playing")
        self.lbl_title.setStyleSheet("color: #FFFFFF; font-size: 12px; font-weight: 600;")
        self.lbl_artist = QLabel("Spotify • Yandex Music • Browser")
        self.lbl_artist.setStyleSheet("color: rgba(255, 255, 255, 155); font-size: 11px;")
        meta_col.addWidget(self.lbl_title)
        meta_col.addWidget(self.lbl_artist)
        top.addLayout(meta_col, stretch=1)

        btn_css = (
            "QPushButton { background-color: rgba(255,255,255,20); color: #FFF; border: none; border-radius: 13px; font-size: 12px; font-weight: 600; }"
            "QPushButton:hover { background-color: rgba(255,255,255,45); }"
        )
        self.btn_prev = QPushButton("⏮")
        self.btn_prev.setFixedSize(26, 26)
        self.btn_prev.setCursor(Qt.CursorShape.PointingHandCursor)
        self.btn_prev.setStyleSheet(btn_css)
        self.btn_prev.clicked.connect(lambda: self.controller.send_command("prev"))

        self.btn_play = QPushButton("▶")
        self.btn_play.setFixedSize(30, 30)
        self.btn_play.setCursor(Qt.CursorShape.PointingHandCursor)
        self.btn_play.setStyleSheet(
            "QPushButton { background-color: #FFFFFF; color: #09090B; border: none; border-radius: 15px; font-size: 13px; font-weight: 700; }"
            "QPushButton:hover { background-color: #E5E7EB; }"
        )
        self.btn_play.clicked.connect(lambda: self.controller.send_command("play_pause"))

        self.btn_next = QPushButton("⏭")
        self.btn_next.setFixedSize(26, 26)
        self.btn_next.setCursor(Qt.CursorShape.PointingHandCursor)
        self.btn_next.setStyleSheet(btn_css)
        self.btn_next.clicked.connect(lambda: self.controller.send_command("next"))

        top.addWidget(self.btn_prev)
        top.addWidget(self.btn_play)
        top.addWidget(self.btn_next)
        root.addLayout(top)

        prog_row = QHBoxLayout()
        prog_row.setSpacing(6)
        self.lbl_pos = QLabel("0:00")
        self.lbl_pos.setStyleSheet("color: rgba(255,255,255,140); font-size: 10px;")
        self.progress_bar = ClickableProgressBar()
        self.progress_bar.seek_requested.connect(lambda r: self.controller.send_command("seek_ratio", r))
        self.lbl_dur = QLabel("0:00")
        self.lbl_dur.setStyleSheet("color: rgba(255,255,255,140); font-size: 10px;")
        prog_row.addWidget(self.lbl_pos)
        prog_row.addWidget(self.progress_bar, stretch=1)
        prog_row.addWidget(self.lbl_dur)
        root.addLayout(prog_row)

        self.controller.track_updated.connect(self.update_state)

    def update_state(self, state: MediaTrackState) -> None:
        fm = self.lbl_title.fontMetrics()
        self.lbl_title.setText(fm.elidedText(state.title, Qt.TextElideMode.ElideRight, 220))
        fm_sub = self.lbl_artist.fontMetrics()
        self.lbl_artist.setText(fm_sub.elidedText(state.artist, Qt.TextElideMode.ElideRight, 220))
        self.btn_play.setText("⏸" if state.is_playing else "▶")
        self.lbl_cover.setPixmap(make_rounded_cover(state.cover_bytes, 42, 10))
        self.lbl_pos.setText(_fmt_mmss(state.position_sec))
        self.lbl_dur.setText(_fmt_mmss(state.duration_sec))
        ratio = (state.position_sec / state.duration_sec) if state.duration_sec > 0 else 0.0
        self.progress_bar.set_progress(ratio)
