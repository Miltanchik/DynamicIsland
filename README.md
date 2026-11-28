<div align="center">

<img src="./assets/hero.svg" alt="Dynamic Island for Windows 10 / 11" width="100%" />

<br /><br />

<p>
  <b>Бесплатный нативный 60 FPS оверлей для Windows 10 / 11 x64 от Miltanchik и Kayot.</b><br />
  Мульти-плеер, полка скриншотов, файловый Dropzone и умный игровой режим.
</p>

<br />

<a href="https://github.com/Miltanchik/DynamicIsland/releases/latest/download/DynamicIslandWin_Setup.exe">
  <img src="./assets/download-button.svg" alt="Скачать Dynamic Island" width="340" />
</a>

<br /><br />

<p>
  <a href="https://github.com/Miltanchik/DynamicIsland/releases/latest/download/DynamicIslandWin.exe">Скачать только приложение для Windows x64</a>
  &nbsp;·&nbsp;
  <a href="https://github.com/Miltanchik/DynamicIsland/releases">Что нового</a>
  &nbsp;·&nbsp;
  <a href="#install">Установка</a>
  &nbsp;·&nbsp;
  <a href="#controls">Управление</a>
  &nbsp;·&nbsp;
  <a href="#build">Сборка</a>
</p>

<p>
  <img src="https://img.shields.io/badge/Windows-10%20%2F%2011%20x64-0078D6?style=flat&logo=windows&logoColor=white" alt="Windows 10 / 11 x64" />
  <img src="https://img.shields.io/badge/.NET%20Framework-4.8-512BD4?style=flat" alt=".NET Framework 4.8" />
  <img src="https://img.shields.io/badge/Dependencies-None-2ea44f?style=flat" alt="No Dependencies" />
</p>

<sub>C# Win32 Layered DIB + DWM · исходники доступны · без внешних зависимостей</sub>

</div>

---

## ✨ Основные возможности

- **Мульти-плеер и управление звуком**: интеграция с Яндекс Музыкой, Spotify, AIMP, VLC и любыми браузерами через WinRT SMTC, эквалайзер, синхронизация лайков (`♥`), раздельная регулировка громкости (`Звук ПК` и `Музыка`) и быстрый индикатор/мьют микрофона в один клик.
- **Продуктивность и файловая полка (Dropzone)**: встроенный таймер фокусировки (Помодоро) со стандартными и своими мелодиями, быстрый док приложений (`[ + ]`) и удобный Dropzone для временного хранения файлов.
- **Хранилище скриншотов и Стоп-таймер загрузок**: мгновенный перехват `Win + Shift + S` / `PrtScn` с копированием в буфер и перетаскиванием прямо в чаты, а также автоматический перевод ПК в Сон / Гибернацию / Выключение после завершения крупных закачек.
- **Умный игровой режим и Режим «Призрака»**: автоматическое определение полноэкранных D3D-игр с переключением в ультракомпактную полоску неоновой телеметрии (`CPU • GPU • Temp • Ping`) или авто-скрытие, плюс прозрачный click-through режим «Призрака».
- **Гибкая кастомизация (`Studio Pro`)**: изменение размеров и масштаба текста прямо на лету, точный ввод параметров с клавиатуры и 60 FPS отрисовка без лишней нагрузки на систему.

---

<a id="install"></a>

## 🚀 Установка и запуск

### Вариант 1 — Готовый установщик (рекомендуется)
1. Нажмите большую кнопку **«Скачать DYNAMIC ISLAND»** вверху страницы или загрузите **`DynamicIslandWin_Setup.exe`** из раздела [Releases](https://github.com/Miltanchik/DynamicIsland/releases).
2. Запустите файл и нажмите **«Установить Dynamic Island»** — приложение распакуется в `%LOCALAPPDATA%\Programs\DynamicIslandWin`, создаст ярлыки и сразу запустится.

<a id="build"></a>

### Вариант 2 — Сборка из исходников
Никаких сторонних SDK устанавливать не нужно — используется встроенный компилятор `csc.exe` из Windows 10 / 11:

```powershell
$csc = "C:\Windows\Microsoft.NET\Framework64\v4.0.30319\csc.exe"
$wpf = "C:\Windows\Microsoft.NET\Framework64\v4.0.30319\WPF"
$fw  = "C:\Windows\Microsoft.NET\Framework64\v4.0.30319"
$wm  = "C:\Windows\System32\WinMetadata"

& $csc /nologo /optimize+ /target:winexe /platform:x64 `
  /win32icon:"DynamicIslandWin.ico" `
  /out:"release\DynamicIslandWin.exe" `
  /r:System.dll /r:System.Core.dll /r:System.Drawing.dll /r:System.Windows.Forms.dll `
  /r:System.Web.Extensions.dll /r:System.Management.dll `
  /r:"$wpf\UIAutomationClient.dll" /r:"$wpf\UIAutomationTypes.dll" `
  /r:"$fw\System.Runtime.dll" /r:"$fw\System.Runtime.WindowsRuntime.dll" `
  /r:"$wm\Windows.Foundation.winmd" /r:"$wm\Windows.Media.winmd" /r:"$wm\Windows.Storage.winmd" `
  "NativeDynamicIsland.cs"
```

---

<a id="controls"></a>

## 🎮 Управление и горячие клавиши

| Действие | Как использовать |
| :--- | :--- |
| **Раскрыть / свернуть остров** | Наведите курсор на капсулу у верхнего края экрана или кликните ЛКМ |
| **Глобальная горячая клавиша** | **`Alt + Space`** — мгновенно раскрыть/свернуть остров или выйти из режима «Призрака» |
| **Переключение вкладок** | Нажмите на стрелочки **`‹`** и **`›`** по левому и правому краям раскрытого острова |
| **Открыть настройки (`Studio Pro`)** | Нажмите **ПКМ** по острову, иконку **`⚙`** на главной вкладке или используйте иконку в трее |
| **Изменение размера на лету** | Зажмите **`Левый Ctrl` + ЛКМ** и тяните край; **`Левый Ctrl` + Колёсико** — масштаб текста; **`Левый Ctrl` + СКМ** — сброс |
| **Регулировка громкости** | Крутите **колёсико мыши** над островом (звук ПК) или над плашкой **«Музыка»** (громкость плеера) |
| **Смена активного плеера** | Кликните по бейджу плеера над названием трека: ЛКМ — следующий, ПКМ — список всех плееров, СКМ — развернуть окно |
| **Скриншоты и полка файлов** | Сделайте скриншот (`Win + Shift + S`) — клик во вкладке скриншотов копирует его в буфер, а перетаскивание отправляет файл прямо в Discord / Telegram / браузер |
| **Быстрый мьют микрофона** | Кликните по индикатору микрофона в компактной капсуле или по кнопке **«Микр»** |
| **Точный ввод чисел в настройках** | Кликните по числовому значению справа от любого ползунка, введите число и нажмите `Enter` |

---

<a id="settings"></a>

## 🗂 Где хранятся настройки

Конфигурация сохраняется автоматически в `%APPDATA%\DynamicIslandWin\config.json` и полностью переносится при любых обновлениях приложения.