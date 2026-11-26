# Language: Python 3.11+ | File: dynamic_island/modules/app_dock.py | Runtime: Win32 Shell32 / GDI32 / PyQt6
"""
Stage 4: Native Windows App Dock & Shell Icon Extractor.
"""

from __future__ import annotations

import ctypes
from ctypes import wintypes
import os
from pathlib import Path
from typing import Optional
import webbrowser

from PyQt6.QtCore import QFileInfo, Qt, pyqtSignal
from PyQt6.QtGui import (
    QAction,
    QColor,
    QIcon,
    QImage,
    QLinearGradient,
    QMouseEvent,
    QPainter,
    QPainterPath,
    QPixmap,
)
from PyQt6.QtWidgets import QFileIconProvider, QHBoxLayout, QLabel, QMenu, QPushButton, QWidget

from core.config import IslandConfig

SHGFI_ICON = 0x000000100
SHGFI_LARGEICON = 0x000000000
FILE_ATTRIBUTE_NORMAL = 0x80


class SHFILEINFOW(ctypes.Structure):
    _fields_ = [
        ("hIcon", wintypes.HICON),
        ("iIcon", ctypes.c_int),
        ("dwAttributes", wintypes.DWORD),
        ("szDisplayName", wintypes.WCHAR * 260),
        ("szTypeName", wintypes.WCHAR * 80),
    ]


class ICONINFO(ctypes.Structure):
    _fields_ = [
        ("fIcon", wintypes.BOOL),
        ("xHotspot", wintypes.DWORD),
        ("yHotspot", wintypes.DWORD),
        ("hbmMask", wintypes.HBITMAP),
        ("hbmColor", wintypes.HBITMAP),
    ]


class BITMAPINFOHEADER(ctypes.Structure):
    _fields_ = [
        ("biSize", wintypes.DWORD),
        ("biWidth", wintypes.LONG),
        ("biHeight", wintypes.LONG),
        ("biPlanes", wintypes.WORD),
        ("biBitCount", wintypes.WORD),
        ("biCompression", wintypes.DWORD),
        ("biSizeImage", wintypes.DWORD),
        ("biXPelsPerMeter", wintypes.LONG),
        ("biYPelsPerMeter", wintypes.LONG),
        ("biClrUsed", wintypes.DWORD),
        ("biClrImportant", wintypes.DWORD),
    ]


shell32 = ctypes.windll.shell32
user32 = ctypes.windll.user32
gdi32 = ctypes.windll.gdi32

_ICON_CACHE: dict[str, QPixmap] = {}
_QT_ICON_PROVIDER = QFileIconProvider()


def _hicon_to_qpixmap(hicon: int, size: int = 32) -> Optional[QPixmap]:
    if not hicon:
        return None
    icon_info = ICONINFO()
    if not user32.GetIconInfo(wintypes.HICON(hicon), ctypes.byref(icon_info)):
        return None
    try:
        hdc = user32.GetDC(0)
        bmi = BITMAPINFOHEADER()
        bmi.biSize = ctypes.sizeof(BITMAPINFOHEADER)
        bmi.biWidth = size
        bmi.biHeight = -size
        bmi.biPlanes = 1
        bmi.biBitCount = 32
        bmi.biCompression = 0

        buf_len = size * size * 4
        pixel_buf = (ctypes.c_ubyte * buf_len)()
        lines = gdi32.GetDIBits(hdc, icon_info.hbmColor, 0, size, pixel_buf, ctypes.byref(bmi), 0)
        user32.ReleaseDC(0, hdc)
        if not lines:
            return None
        img = QImage(bytes(pixel_buf), size, size, size * 4, QImage.Format.Format_ARGB32).copy()
        return QPixmap.fromImage(img)
    finally:
        if icon_info.hbmColor:
            gdi32.DeleteObject(icon_info.hbmColor)
        if icon_info.hbmMask:
            gdi32.DeleteObject(icon_info.hbmMask)


def extract_native_win32_icon(target_path: str, size: int = 24) -> QPixmap:
    cache_key = f"{target_path}::{size}"
    if cache_key in _ICON_CACHE:
        return _ICON_CACHE[cache_key]

    pix: Optional[QPixmap] = None
    if not target_path.startswith(("http://", "https://")) and os.path.exists(target_path):
        qicon = _QT_ICON_PROVIDER.icon(QFileInfo(target_path))
        if not qicon.isNull():
            candidate = qicon.pixmap(size, size)
            if not candidate.isNull():
                pix = candidate

    if (pix is None or pix.isNull()) and not target_path.startswith(("http://", "https://")):
        shfi = SHFILEINFOW()
        res = shell32.SHGetFileInfoW(
            ctypes.c_wchar_p(target_path),
            FILE_ATTRIBUTE_NORMAL,
            ctypes.byref(shfi),
            ctypes.sizeof(shfi),
            SHGFI_ICON | SHGFI_LARGEICON,
        )
        if res and shfi.hIcon:
            raw_pix = _hicon_to_qpixmap(shfi.hIcon, 32)
            user32.DestroyIcon(shfi.hIcon)
            if raw_pix and not raw_pix.isNull():
                pix = raw_pix.scaled(size, size, Qt.AspectRatioMode.KeepAspectRatio, Qt.TransformationMode.SmoothTransformation)

    if pix is None or pix.isNull():
        pix = QPixmap(size, size)
        pix.fill(Qt.GlobalColor.transparent)
        p = QPainter(pix)
        p.setRenderHint(QPainter.RenderHint.Antialiasing, True)
        path = QPainterPath()
        path.addRoundedRect(0, 0, size, size, 7, 7)
        grad = QLinearGradient(0, 0, size, size)
        grad.setColorAt(0.0, QColor("#2563EB"))
        grad.setColorAt(1.0, QColor("#7C3AED"))
        p.fillPath(path, grad)
        p.setPen(QColor("#FFFFFF"))
        p.drawText(pix.rect(), int(Qt.AlignmentFlag.AlignCenter), "🌐" if target_path.startswith(("http://", "https://")) else "⚡")
        p.end()

    _ICON_CACHE[cache_key] = pix
    return pix


class DockAppButton(QPushButton):
    remove_requested = pyqtSignal(str)

    def __init__(self, name: str, target_path: str, parent: Optional[QWidget] = None) -> None:
        super().__init__(parent)
        self.app_name = name
        self.target_path = target_path
        self.setFixedSize(34, 34)
        self.setCursor(Qt.CursorShape.PointingHandCursor)
        self.setToolTip(f"{name}\n{target_path}\n(Left-click: Launch | Right-click: Remove)")
        self.setStyleSheet(
            "QPushButton { background-color: rgba(255,255,255,16); border: 1px solid rgba(255,255,255,26); border-radius: 10px; }"
            "QPushButton:hover { background-color: rgba(255,255,255,42); border: 1px solid rgba(56,189,248,160); }"
        )
        icon_pix = extract_native_win32_icon(target_path, 22)
        self.setIcon(QIcon(icon_pix))
        self.setIconSize(icon_pix.size())
        self.clicked.connect(self.launch_target)

    def launch_target(self) -> None:
        try:
            if self.target_path.startswith(("http://", "https://")):
                webbrowser.open(self.target_path)
            else:
                os.startfile(self.target_path)
        except Exception:
            pass

    def mousePressEvent(self, event: QMouseEvent) -> None:
        if event.button() == Qt.MouseButton.RightButton:
            menu = QMenu(self)
            menu.setStyleSheet(
                "QMenu { background-color: #111827; color: #F9FAFB; border: 1px solid #374151; border-radius: 8px; padding: 4px; }"
                "QMenu::item { padding: 5px 14px; border-radius: 5px; font-size: 11px; }"
                "QMenu::item:selected { background-color: #EF4444; color: #FFFFFF; }"
            )
            act_open = QAction(f"🚀 Launch {self.app_name}", self)
            act_open.triggered.connect(self.launch_target)
            act_del = QAction("🗑 Remove from Island Dock", self)
            act_del.triggered.connect(lambda: self.remove_requested.emit(self.target_path))
            menu.addAction(act_open)
            menu.addAction(act_del)
            menu.exec(event.globalPosition().toPoint())
            return
        super().mousePressEvent(event)


class AppDockWidget(QWidget):
    def __init__(self, config: IslandConfig, parent: Optional[QWidget] = None) -> None:
        super().__init__(parent)
        self.config = config
        self._layout = QHBoxLayout(self)
        self._layout.setContentsMargins(4, 2, 4, 2)
        self._layout.setSpacing(6)
        self.lbl_hint = QLabel("")
        self.lbl_hint.setStyleSheet("color: rgba(255,255,255,115); font-size: 10px;")
        self.rebuild_dock()

    def rebuild_dock(self) -> None:
        while self._layout.count():
            item = self._layout.takeAt(0)
            if item.widget():
                item.widget().deleteLater()

        for entry in self.config.pinned_apps[:9]:
            btn = DockAppButton(
                name=str(entry.get("name", "App")),
                target_path=str(entry.get("target_path", "")),
                parent=self,
            )
            btn.remove_requested.connect(self.remove_pinned_app)
            self._layout.addWidget(btn)

        self.lbl_hint = QLabel("")
        self.lbl_hint.setStyleSheet("color: rgba(255,255,255,115); font-size: 10px;")
        self._layout.addWidget(self.lbl_hint, stretch=1)

    def add_pinned_target(self, target_path: str) -> bool:
        norm = target_path.strip()
        if not norm:
            return False
        for existing in self.config.pinned_apps:
            if existing.get("target_path", "").lower() == norm.lower():
                return False

        if norm.startswith(("http://", "https://")):
            name = norm.split("//", 1)[-1].split("/", 1)[0]
            kind = "url"
        else:
            p = Path(norm)
            name = p.stem if p.suffix.lower() in (".exe", ".lnk", ".url") else p.name
            kind = "folder" if p.is_dir() else "app"

        self.config.pinned_apps.append({"name": name or "Pinned", "target_path": norm, "kind": kind})
        self.config.save()
        self.rebuild_dock()
        return True

    def remove_pinned_app(self, target_path: str) -> None:
        self.config.pinned_apps = [
            e for e in self.config.pinned_apps
            if e.get("target_path", "").lower() != target_path.lower()
        ]
        self.config.save()
        self.rebuild_dock()
