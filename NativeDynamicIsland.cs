// Language: C# (.NET 4.8 / Win32 CreateDIBSection / WinRT SMTC / Apple macOS Sequoia & iOS 18 Minimalist UI)
// File: dynamic_island/NativeDynamicIsland.cs -> Compiles to DynamicIslandWin.exe
// Runtime: Native 64-bit Windows 10 / Windows 11 Desktop Overlay

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using Point = System.Drawing.Point;
using PointF = System.Drawing.PointF;
using Size = System.Drawing.Size;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Drawing.Text;
using System.IO;
using System.Media;
using System.Windows.Automation;
using System.Net;
using System.Net.NetworkInformation;
using System.Security.Cryptography;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Web.Script.Serialization;
using System.Windows.Forms;
using Microsoft.Win32;
using Windows.Foundation;
using Windows.Media.Control;
using Windows.Storage.Streams;

using System.Reflection;
[assembly: AssemblyTitle("Dynamic Island for Windows 10/11")]
[assembly: AssemblyDescription("Standalone Native 60 FPS Dynamic Island Overlay for Windows 10 & Windows 11")]
[assembly: AssemblyProduct("Dynamic Island Studio")]
[assembly: AssemblyCompany("DynamicIslandWin")]
[assembly: AssemblyCopyright("Copyright © 2026")]
[assembly: AssemblyVersion("3.0.0.0")]
[assembly: AssemblyFileVersion("3.0.0.0")]
namespace DynamicIslandWin
{
    // =========================================================================
    // 1. PERSISTENT CONFIGURATION (%APPDATA%\DynamicIslandWin\config.json)
    // =========================================================================
    public class PinnedAppItem
    {
        public string name { get; set; }
        public string target_path { get; set; }
        public string kind { get; set; }
    }

    public class IslandConfigData
    {
        public int monitor_index { get; set; }
        public int top_margin { get; set; }
        public int horizontal_offset { get; set; }
        public int idle_width { get; set; }
        public int idle_height { get; set; }
        public int expanded_width { get; set; }
        public int expanded_height { get; set; }
        public int ui_scale_percent { get; set; }
        public int corner_radius { get; set; }

        public string custom_bg_path { get; set; }
        public string custom_bg_fit_mode { get; set; }
        public string custom_bg_scope { get; set; }
        public int custom_bg_dim_percent { get; set; }
        public string theme_preset { get; set; }
        public int bg_opacity { get; set; }
        public int border_glow { get; set; }
        public string eq_style { get; set; }
        public bool show_compact_cover { get; set; }

        public bool expand_on_hover { get; set; }
        public int anim_duration_ms { get; set; }
        public int spring_overshoot_pct { get; set; }
        public int collapse_delay_ms { get; set; }
        public bool enable_lctrl_resize { get; set; }
        public bool hide_in_fullscreen { get; set; }
        public bool prefer_music_players { get; set; }

        public bool enable_live_activities { get; set; }
        public bool show_clock { get; set; }
        public bool show_battery { get; set; }
        public bool show_dropzone { get; set; }
        public bool show_telemetry { get; set; }
        public int pomodoro_minutes { get; set; }
        public string timer_sound_preset { get; set; }
        public string timer_custom_sound_path { get; set; }
        public bool autostart { get; set; }
        public string fullscreen_mode { get; set; }
        public bool enable_ghost_mode { get; set; }
        public bool enable_mic_indicator { get; set; }
        public bool enable_screenshot_vault { get; set; }
        public bool enable_download_watchdog { get; set; }
        public string watchdog_action { get; set; }
        public int download_threshold_mbps { get; set; }
        public string skipped_version { get; set; }
        public bool auto_update_enabled { get; set; }
        public string github_repo { get; set; }

        public List<PinnedAppItem> pinned_apps { get; set; }
        public List<string> liked_tracks { get; set; }

        public IslandConfigData()
        {
            monitor_index = 0;
            top_margin = 8;
            horizontal_offset = 0;
            idle_width = 310;
            idle_height = 36;
            expanded_width = 520;
            expanded_height = 224;
            ui_scale_percent = 100;
            corner_radius = 30;

            custom_bg_path = "";
            custom_bg_fit_mode = "1:1 По размеру островка";
            custom_bg_scope = "Везде";
            custom_bg_dim_percent = 20;
            theme_preset = "Dinasmic Neon";
            bg_opacity = 248;
            border_glow = 52;
            eq_style = "5 Полос";
            show_compact_cover = true;

            expand_on_hover = true;
            anim_duration_ms = 330;
            spring_overshoot_pct = 112;
            collapse_delay_ms = 360;
            enable_lctrl_resize = true;
            hide_in_fullscreen = false;
            prefer_music_players = true;

            enable_live_activities = true;
            show_clock = true;
            show_battery = true;
            show_dropzone = true;
            show_telemetry = true;
            pomodoro_minutes = 25;
            timer_sound_preset = "Apple Radar";
            timer_custom_sound_path = "";
            autostart = false;
            fullscreen_mode = "Неоновая телеметрия";
            enable_ghost_mode = false;
            enable_mic_indicator = true;
            enable_screenshot_vault = true;
            enable_download_watchdog = false;
            watchdog_action = "Сон";
            download_threshold_mbps = 2;
            skipped_version = "";
            auto_update_enabled = false;
            github_repo = "Miltanchik/DynamicIsland";

            pinned_apps = new List<PinnedAppItem>();
            liked_tracks = new List<string>();
        }
    }

    public static class ConfigManager
    {
        private const string RunKeyPath = @"Software\Microsoft\Windows\CurrentVersion\Run";
        private const string AppRegName = "DynamicIslandWin";

        public static string GetConfigPath()
        {
            string appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            string dir = Path.Combine(appData, "DynamicIslandWin");
            if (!Directory.Exists(dir)) Directory.CreateDirectory(dir);
            return Path.Combine(dir, "config.json");
        }

        public static IslandConfigData Load()
        {
            string path = GetConfigPath();
            if (!File.Exists(path))
            {
                var def = new IslandConfigData();
                def.pinned_apps.Add(new PinnedAppItem { name = "Explorer", target_path = @"C:\Windows\explorer.exe", kind = "app" });
                def.pinned_apps.Add(new PinnedAppItem { name = "Notepad", target_path = @"C:\Windows\System32\notepad.exe", kind = "app" });
                def.pinned_apps.Add(new PinnedAppItem { name = "TaskMgr", target_path = @"C:\Windows\System32\Taskmgr.exe", kind = "app" });
                def.autostart = IsAutostartEnabled();
                Save(def);
                return def;
            }
            try
            {
                string json = File.ReadAllText(path, Encoding.UTF8);
                var ser = new JavaScriptSerializer();
                var cfg = ser.Deserialize<IslandConfigData>(json) ?? new IslandConfigData();
                if (cfg.pinned_apps == null) cfg.pinned_apps = new List<PinnedAppItem>();
                if (cfg.liked_tracks == null) cfg.liked_tracks = new List<string>();
                if (cfg.idle_width < 160) cfg.idle_width = 310;
                if (cfg.idle_height < 26) cfg.idle_height = 36;
                if (cfg.expanded_width < 360) cfg.expanded_width = 520;
                if (cfg.expanded_height < 160) cfg.expanded_height = 224;
                if (cfg.ui_scale_percent < 75 || cfg.ui_scale_percent > 160) cfg.ui_scale_percent = 100;
                if (cfg.corner_radius < 10) cfg.corner_radius = 30;
                if (cfg.bg_opacity < 120) cfg.bg_opacity = 248;
                if (cfg.anim_duration_ms < 100) cfg.anim_duration_ms = 330;
                if (cfg.spring_overshoot_pct < 60) cfg.spring_overshoot_pct = 112;
                if (cfg.collapse_delay_ms < 100) cfg.collapse_delay_ms = 360;
                if (cfg.pomodoro_minutes < 1) cfg.pomodoro_minutes = 25;
                if (string.IsNullOrEmpty(cfg.timer_sound_preset)) cfg.timer_sound_preset = "Apple Radar";
                if (cfg.timer_custom_sound_path == null) cfg.timer_custom_sound_path = "";
                if (cfg.custom_bg_path == null) cfg.custom_bg_path = "";
                if (string.IsNullOrEmpty(cfg.custom_bg_fit_mode)) cfg.custom_bg_fit_mode = "1:1 По размеру островка";
                if (string.IsNullOrEmpty(cfg.custom_bg_scope)) cfg.custom_bg_scope = "Везде";
                if (cfg.custom_bg_dim_percent < 0 || cfg.custom_bg_dim_percent > 90) cfg.custom_bg_dim_percent = 20;
                if (string.IsNullOrEmpty(cfg.theme_preset)) cfg.theme_preset = "Dinasmic Neon";
                if (string.IsNullOrEmpty(cfg.eq_style)) cfg.eq_style = "5 Полос";
                if (string.IsNullOrEmpty(cfg.fullscreen_mode)) cfg.fullscreen_mode = cfg.hide_in_fullscreen ? "Авто-скрытие" : "Неоновая телеметрия";
                if (string.IsNullOrEmpty(cfg.watchdog_action)) cfg.watchdog_action = "Сон";
                if (cfg.download_threshold_mbps < 1) cfg.download_threshold_mbps = 2;
                if (cfg.skipped_version == null) cfg.skipped_version = "";
                if (string.IsNullOrEmpty(cfg.github_repo)) cfg.github_repo = "Miltanchik/DynamicIsland";
                return cfg;
            }
            catch { return new IslandConfigData(); }
        }

        public static void Save(IslandConfigData cfg)
        {
            try
            {
                var ser = new JavaScriptSerializer();
                string json = ser.Serialize(cfg);
                File.WriteAllText(GetConfigPath(), json, Encoding.UTF8);
                string altDir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "DinamicIsland");
                if (!Directory.Exists(altDir)) Directory.CreateDirectory(altDir);
                File.WriteAllText(Path.Combine(altDir, "settings.json"), json, Encoding.UTF8);
            }
            catch { }
        }

        public static bool IsAutostartEnabled()
        {
            try
            {
                using (RegistryKey key = Registry.CurrentUser.OpenSubKey(RunKeyPath, false))
                    return key != null && key.GetValue(AppRegName) != null;
            }
            catch { return false; }
        }

        public static void SetAutostart(bool enabled)
        {
            try
            {
                using (RegistryKey key = Registry.CurrentUser.OpenSubKey(RunKeyPath, true))
                {
                    if (key == null) return;
                    if (enabled) key.SetValue(AppRegName, "\"" + Application.ExecutablePath + "\"");
                    else key.DeleteValue(AppRegName, false);
                }
            }
            catch { }
        }
    }

    // =========================================================================
    // 2. HIGH-QUALITY CUSTOM GIF / PHOTO ENGINE
    // =========================================================================
    // =========================================================================
    // 1B. TIMER AUDIO ALERT ENGINE (Built-in Apple Chimes + Custom MP3/WAV)
    // =========================================================================
    public static class TimerSoundEngine
    {
        public static readonly string[] Presets = new string[]
        {
            "Apple Radar",
            "Crystal Chime",
            "Digital Pulse",
            "Zen Bell",
            "Свой звук",
            "Без звука"
        };

        private static readonly object _soundLock = new object();
        private static int _playGeneration = 0;
        private static SoundPlayer _activeSoundPlayer = null;
        private static MemoryStream _activeWavStream = null;
        private static object _wmpInstance = null;

        public static void StopAlert()
        {
            Interlocked.Increment(ref _playGeneration);
            try { NativeWin32.PlaySound(null, IntPtr.Zero, NativeWin32.SND_PURGE); } catch { }
            try { NativeWin32.PlaySound(null, IntPtr.Zero, 0); } catch { }

            lock (_soundLock)
            {
                if (_activeSoundPlayer != null)
                {
                    try { _activeSoundPlayer.Stop(); } catch { }
                    try { _activeSoundPlayer.Dispose(); } catch { }
                    _activeSoundPlayer = null;
                }
                if (_activeWavStream != null)
                {
                    try { _activeWavStream.Dispose(); } catch { }
                    _activeWavStream = null;
                }
                if (_wmpInstance != null)
                {
                    try
                    {
                        Type wt = _wmpInstance.GetType();
                        object ctrls = wt.InvokeMember("controls", BindingFlags.GetProperty, null, _wmpInstance, null);
                        if (ctrls != null)
                            ctrls.GetType().InvokeMember("stop", BindingFlags.InvokeMethod, null, ctrls, null);
                        wt.InvokeMember("close", BindingFlags.InvokeMethod, null, _wmpInstance, null);
                    }
                    catch { }
                    _wmpInstance = null;
                }
            }
        }

        public static void PlayAlert(IslandConfigData cfg)
        {
            // Immediately cancel any previous preview/alert melody before switching or silencing
            StopAlert();
            if (cfg == null) return;

            int myGen = Volatile.Read(ref _playGeneration);
            string preset = cfg.timer_sound_preset ?? "Apple Radar";
            string customPath = (cfg.timer_custom_sound_path ?? "").Trim();

            if (preset == "Без звука") return;

            bool useCustom = !string.IsNullOrEmpty(customPath) && File.Exists(customPath) &&
                             (preset == "Свой звук" || Array.IndexOf(Presets, preset) < 0);

            if (useCustom)
            {
                PlayCustomAudioFile(customPath, myGen);
                return;
            }

            ThreadPool.QueueUserWorkItem(_ =>
            {
                try
                {
                    if (Volatile.Read(ref _playGeneration) != myGen) return;
                    byte[] wavBytes = SynthesizePresetWav(preset);
                    if (wavBytes == null || Volatile.Read(ref _playGeneration) != myGen) return;

                    lock (_soundLock)
                    {
                        if (Volatile.Read(ref _playGeneration) != myGen) return;
                        _activeWavStream = new MemoryStream(wavBytes);
                        _activeSoundPlayer = new SoundPlayer(_activeWavStream);
                        _activeSoundPlayer.Play(); // Async WinMM waveOut so StopAlert() halts it instantaneously
                    }
                }
                catch
                {
                    if (Volatile.Read(ref _playGeneration) == myGen)
                    {
                        try { SystemSounds.Asterisk.Play(); } catch { }
                    }
                }
            });
        }

        private static void PlayCustomAudioFile(string filePath, int myGen)
        {
            try
            {
                if (Volatile.Read(ref _playGeneration) != myGen) return;
                string ext = (Path.GetExtension(filePath) ?? "").ToLowerInvariant();

                // 1) Fast native async WinMM PlaySoundW for .wav files (instantly cancellable via PlaySound(null, ...))
                if (ext == ".wav")
                {
                    if (NativeWin32.PlaySound(filePath, IntPtr.Zero, NativeWin32.SND_FILENAME | NativeWin32.SND_ASYNC | NativeWin32.SND_NODEFAULT))
                        return;
                }

                // 2) Dedicated STA thread with generation-cancelled MCI alias so StopAlert() stops custom MP3/M4A/OGG within <=25ms
                var staThread = new Thread(() =>
                {
                    if (Volatile.Read(ref _playGeneration) != myGen) return;
                    bool playedByMci = false;
                    string alias = "TimerCustomAlert_" + (myGen & 0xFFFF);
                    try
                    {
                        var sbShort = new StringBuilder(512);
                        uint len = NativeWin32.GetShortPathName(filePath, sbShort, 512);
                        string safePath = (len > 0 && len < 512) ? sbShort.ToString() : filePath;

                        NativeWin32.mciSendString("close " + alias, null, 0, IntPtr.Zero);
                        string openCmd = string.Format("open \"{0}\" type mpegvideo alias {1}", safePath, alias);
                        int rc = NativeWin32.mciSendString(openCmd, null, 0, IntPtr.Zero);
                        if (rc != 0)
                        {
                            openCmd = string.Format("open \"{0}\" alias {1}", safePath, alias);
                            rc = NativeWin32.mciSendString(openCmd, null, 0, IntPtr.Zero);
                        }
                        if (rc == 0)
                        {
                            if (Volatile.Read(ref _playGeneration) != myGen)
                            {
                                NativeWin32.mciSendString("close " + alias, null, 0, IntPtr.Zero);
                                return;
                            }
                            NativeWin32.mciSendString("setaudio " + alias + " volume to 1000", null, 0, IntPtr.Zero);
                            if (NativeWin32.mciSendString("play " + alias, null, 0, IntPtr.Zero) == 0)
                            {
                                playedByMci = true;
                                var sw = Stopwatch.StartNew();
                                var statusBuf = new StringBuilder(64);
                                while (sw.ElapsedMilliseconds < 20000)
                                {
                                    if (Volatile.Read(ref _playGeneration) != myGen)
                                    {
                                        NativeWin32.mciSendString("stop " + alias, null, 0, IntPtr.Zero);
                                        break;
                                    }
                                    Application.DoEvents();
                                    Thread.Sleep(25);
                                    if (Volatile.Read(ref _playGeneration) != myGen)
                                    {
                                        NativeWin32.mciSendString("stop " + alias, null, 0, IntPtr.Zero);
                                        break;
                                    }
                                    statusBuf.Clear();
                                    if (NativeWin32.mciSendString("status " + alias + " mode", statusBuf, 64, IntPtr.Zero) == 0)
                                    {
                                        string mode = statusBuf.ToString().Trim().ToLowerInvariant();
                                        if (mode != "playing") break;
                                    }
                                    else break;
                                }
                            }
                            NativeWin32.mciSendString("close " + alias, null, 0, IntPtr.Zero);
                        }
                    }
                    catch
                    {
                        try { NativeWin32.mciSendString("close " + alias, null, 0, IntPtr.Zero); } catch { }
                    }

                    // 3) Fallback to Windows Media Foundation COM player (WMPlayer.OCX.7) with 25ms cancellation check on STA thread
                    if (!playedByMci && Volatile.Read(ref _playGeneration) == myGen)
                    {
                        object localWmp = null;
                        Type wmpType = null;
                        try
                        {
                            wmpType = Type.GetTypeFromProgID("WMPlayer.OCX.7");
                            if (wmpType != null)
                            {
                                localWmp = Activator.CreateInstance(wmpType);
                                lock (_soundLock) { _wmpInstance = localWmp; }
                                object settings = wmpType.InvokeMember("settings", BindingFlags.GetProperty, null, localWmp, null);
                                if (settings != null)
                                    settings.GetType().InvokeMember("volume", BindingFlags.SetProperty, null, settings, new object[] { 100 });
                                wmpType.InvokeMember("URL", BindingFlags.SetProperty, null, localWmp, new object[] { filePath });
                                object controls = wmpType.InvokeMember("controls", BindingFlags.GetProperty, null, localWmp, null);
                                if (controls != null)
                                    controls.GetType().InvokeMember("play", BindingFlags.InvokeMethod, null, controls, null);

                                var sw = Stopwatch.StartNew();
                                while (sw.ElapsedMilliseconds < 20000)
                                {
                                    if (Volatile.Read(ref _playGeneration) != myGen)
                                    {
                                        if (controls != null)
                                            controls.GetType().InvokeMember("stop", BindingFlags.InvokeMethod, null, controls, null);
                                        wmpType.InvokeMember("close", BindingFlags.InvokeMethod, null, localWmp, null);
                                        break;
                                    }
                                    Application.DoEvents();
                                    Thread.Sleep(25);
                                }
                            }
                        }
                        catch { }
                        finally
                        {
                            if (localWmp != null && wmpType != null)
                            {
                                try { wmpType.InvokeMember("close", BindingFlags.InvokeMethod, null, localWmp, null); } catch { }
                            }
                        }
                    }
                });
                staThread.SetApartmentState(ApartmentState.STA);
                staThread.IsBackground = true;
                staThread.Start();
            }
            catch { }
        }

        private static byte[] SynthesizePresetWav(string preset)
        {
            int sampleRate = 44100;
            double durationSec = 1.65;
            int totalSamples = (int)(sampleRate * durationSec);
            short[] pcm = new short[totalSamples];

            double[] freqs;
            double[] starts;
            double[] lengths;

            if (preset == "Crystal Chime")
            {
                freqs = new double[] { 587.33, 880.0, 1174.66, 1760.0 };
                starts = new double[] { 0.0, 0.14, 0.28, 0.42 };
                lengths = new double[] { 0.75, 0.85, 0.95, 1.15 };
            }
            else if (preset == "Digital Pulse")
            {
                freqs = new double[] { 987.77, 1318.51, 987.77, 1318.51, 1975.53 };
                starts = new double[] { 0.0, 0.12, 0.36, 0.48, 0.64 };
                lengths = new double[] { 0.11, 0.18, 0.11, 0.15, 0.85 };
            }
            else if (preset == "Zen Bell")
            {
                freqs = new double[] { 432.0, 648.0, 1296.0 };
                starts = new double[] { 0.0, 0.0, 0.02 };
                lengths = new double[] { 1.60, 1.35, 0.90 };
            }
            else
            {
                freqs = new double[] { 659.25, 880.0, 987.77, 1318.51, 659.25, 880.0, 987.77, 1318.51 };
                starts = new double[] { 0.0, 0.11, 0.22, 0.33, 0.72, 0.83, 0.94, 1.05 };
                lengths = new double[] { 0.24, 0.24, 0.24, 0.45, 0.24, 0.24, 0.24, 0.55 };
            }

            for (int i = 0; i < totalSamples; i++)
            {
                double t = i / (double)sampleRate;
                double mix = 0.0;
                for (int n = 0; n < freqs.Length; n++)
                {
                    double dt = t - starts[n];
                    if (dt >= 0.0 && dt <= lengths[n])
                    {
                        double attack = Math.Min(1.0, dt / 0.008);
                        double decay = Math.Exp(-3.4 * dt / lengths[n]);
                        double env = attack * decay;
                        double wave = Math.Sin(2.0 * Math.PI * freqs[n] * dt)
                                    + 0.28 * Math.Sin(2.0 * Math.PI * (freqs[n] * 2.0) * dt);
                        mix += wave * env * 0.28;
                    }
                }
                mix = Math.Max(-0.95, Math.Min(0.95, mix));
                pcm[i] = (short)(mix * 32767);
            }

            using (var ms = new MemoryStream())
            using (var bw = new BinaryWriter(ms))
            {
                int byteRate = sampleRate * 2;
                int dataSize = totalSamples * 2;
                bw.Write(Encoding.ASCII.GetBytes("RIFF"));
                bw.Write(36 + dataSize);
                bw.Write(Encoding.ASCII.GetBytes("WAVE"));
                bw.Write(Encoding.ASCII.GetBytes("fmt "));
                bw.Write(16);
                bw.Write((short)1);
                bw.Write((short)1);
                bw.Write(sampleRate);
                bw.Write(byteRate);
                bw.Write((short)2);
                bw.Write((short)16);
                bw.Write(Encoding.ASCII.GetBytes("data"));
                bw.Write(dataSize);
                for (int i = 0; i < totalSamples; i++)
                    bw.Write(pcm[i]);
                return ms.ToArray();
            }
        }
    }

    // =========================================================================
    // 1C. LIVE SCREEN-SHARE & VIDEO STREAM CAPTURE ENGINE (Discord / YouTube / Web)
    // =========================================================================
    public sealed class StreamSourceInfo
    {
        public IntPtr Hwnd;
        public string ProcessName = "";
        public string CategoryLabel = "Экран";
        public string WindowTitle = "";
        public int PriorityScore = 0;

        public string DisplayHeader
        {
            get
            {
                string t = WindowTitle ?? "";
                if (t.Length > 34) t = t.Substring(0, 33) + "…";
                return string.IsNullOrEmpty(t) ? CategoryLabel : (CategoryLabel + " • " + t);
            }
        }
    }

    public sealed class DwmStreamOverlayForm : Form
    {
        private IntPtr _thumbId = IntPtr.Zero;
        private IntPtr _boundSourceHwnd = IntPtr.Zero;
        private IntPtr _ownerHwnd = IntPtr.Zero;
        private int _lastX = -9999, _lastY = -9999, _lastW = -1, _lastH = -1;
        private int _lastCropMode = -1;
        private int _lastSrcW = -1, _lastSrcH = -1;
        private IntPtr _parkedHwnd = IntPtr.Zero;
        private NativeWin32.RECT _parkedNormalRect;
        private bool _parkedWasZoomed = false;
        private int _parkedTick = 0;

        public DwmStreamOverlayForm()
        {
            Text = "";
            FormBorderStyle = FormBorderStyle.None;
            StartPosition = FormStartPosition.Manual;
            ShowInTaskbar = false;
            TopMost = true;
            BackColor = Color.FromArgb(6, 7, 11);
            Size = new Size(10, 10);
            Location = new Point(-4000, -4000);

            // Prevent GDI background erase flicker over DWM hardware thumbnail surface
            SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.UserPaint | ControlStyles.Opaque, true);
        }

        protected override bool ShowWithoutActivation { get { return true; } }

        protected override CreateParams CreateParams
        {
            get
            {
                CreateParams cp = base.CreateParams;
                cp.ExStyle |= NativeWin32.WS_EX_TOOLWINDOW | NativeWin32.WS_EX_TOPMOST | NativeWin32.WS_EX_TRANSPARENT | NativeWin32.WS_EX_NOACTIVATE;
                return cp;
            }
        }

        protected override void OnPaintBackground(PaintEventArgs e)
        {
            // Do nothing: prevents WM_ERASEBKGND flicker over live DWM thumbnail
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            using (var br = new SolidBrush(Color.FromArgb(6, 7, 11)))
                e.Graphics.FillRectangle(br, ClientRectangle);
        }

        public void SyncHardwareStream(IntPtr ownerHwnd, int screenX, int screenY, int viewW, int viewH, StreamSourceInfo src, int cropModeIndex)
        {
            if (src == null || src.Hwnd == IntPtr.Zero || viewW < 30 || viewH < 30)
            {
                HideStream();
                return;
            }

            if (!IsHandleCreated)
            {
                CreateHandle();
            }

            // Bind owner relationship once so Windows DWM permanently keeps _dwmOverlay above DynamicIslandOverlayForm without Z-order fighting
            if (_ownerHwnd != ownerHwnd && ownerHwnd != IntPtr.Zero)
            {
                _ownerHwnd = ownerHwnd;
                NativeWin32.SetWindowLongPtr(Handle, NativeWin32.GWLP_HWNDPARENT, ownerHwnd);
            }

            IntPtr target = src.Hwnd;
            if (_parkedHwnd != IntPtr.Zero && _parkedHwnd != target)
            {
                RestoreParkedWindow(false);
            }
            if (NativeWin32.IsIconic(target))
            {
                ParkMinimizedWindowForLiveStream(target);
            }

            bool geomChanged = (screenX != _lastX || screenY != _lastY || viewW != _lastW || viewH != _lastH);
            bool sizeChanged = (viewW != _lastW || viewH != _lastH);

            if (sizeChanged)
            {
                IntPtr hRgn = NativeWin32.CreateRoundRectRgn(0, 0, viewW + 1, viewH + 1, 22, 22);
                Region oldRgn = Region;
                Region = Region.FromHrgn(hRgn);
                NativeWin32.DeleteObject(hRgn);
                if (oldRgn != null) oldRgn.Dispose();
            }

            if (geomChanged || !Visible)
            {
                _lastX = screenX; _lastY = screenY; _lastW = viewW; _lastH = viewH;
                NativeWin32.SetWindowPos(Handle, NativeWin32.HWND_TOPMOST, screenX, screenY, viewW, viewH,
                    NativeWin32.SWP_NOACTIVATE | (uint)0x0040); // SWP_SHOWWINDOW = 0x0040
                Visible = true;
            }

            bool targetChanged = (_boundSourceHwnd != target || _thumbId == IntPtr.Zero);
            if (targetChanged)
            {
                UnregisterThumb();
                if (NativeWin32.DwmRegisterThumbnail(Handle, target, out _thumbId) == 0 && _thumbId != IntPtr.Zero)
                {
                    _boundSourceHwnd = target;
                    _lastSrcW = -1;
                    _lastSrcH = -1;
                }
                else
                {
                    _thumbId = IntPtr.Zero;
                    _boundSourceHwnd = IntPtr.Zero;
                    return;
                }
            }

            NativeWin32.SIZE srcSize;
            if (NativeWin32.DwmQueryThumbnailSourceSize(_thumbId, out srcSize) != 0 || srcSize.cx < 40 || srcSize.cy < 40)
            {
                NativeWin32.RECT wrc;
                if (NativeWin32.GetWindowRect(target, out wrc))
                {
                    srcSize.cx = Math.Max(100, wrc.Right - wrc.Left);
                    srcSize.cy = Math.Max(100, wrc.Bottom - wrc.Top);
                }
                else return;
            }

            int srcW = srcSize.cx;
            int srcH = srcSize.cy;

            // CRITICAL FLICKER FIX: Only call DwmUpdateThumbnailProperties when target, viewport size, source window size, or crop mode actually changed!
            if (!targetChanged && !geomChanged && cropModeIndex == _lastCropMode && srcW == _lastSrcW && srcH == _lastSrcH)
            {
                return;
            }

            _lastCropMode = cropModeIndex;
            _lastSrcW = srcW;
            _lastSrcH = srcH;

            var rcSrc = new NativeWin32.RECT { Left = 0, Top = 0, Right = srcW, Bottom = srcH };
            if (cropModeIndex == 0) // "Стрим (Авто)"
            {
                bool isDiscord = src.CategoryLabel == "Discord";
                bool isBrowser = src.CategoryLabel == "YouTube" || src.CategoryLabel == "Twitch" ||
                                 src.CategoryLabel == "Браузер" || src.CategoryLabel == "Видеохостинг" ||
                                 src.CategoryLabel == "VK Видео" || src.CategoryLabel == "Кинопоиск" ||
                                 src.CategoryLabel == "Rutube";
                if (isDiscord && srcW > 520 && srcH > 360)
                {
                    int leftTrim = (int)(srcW * 0.246f);
                    int rightTrim = (int)(srcW * 0.012f);
                    int topTrim = (int)(srcH * 0.064f);
                    int botTrim = (int)(srcH * 0.215f);
                    rcSrc.Left = leftTrim;
                    rcSrc.Top = topTrim;
                    rcSrc.Right = Math.Max(leftTrim + 160, srcW - rightTrim);
                    rcSrc.Bottom = Math.Max(topTrim + 100, srcH - botTrim);
                }
                else if (isBrowser && srcW > 520 && srcH > 360)
                {
                    int topTrim = Math.Min(110, (int)(srcH * 0.115f));
                    rcSrc.Left = 6;
                    rcSrc.Top = topTrim;
                    rcSrc.Right = Math.Max(160, srcW - 6);
                    rcSrc.Bottom = Math.Max(topTrim + 100, srcH - 6);
                }
            }
            else if (cropModeIndex == 3) // "Крупный план"
            {
                int cw = Math.Max(160, (int)(srcW * 0.66f));
                int ch = Math.Max(100, (int)(srcH * 0.66f));
                rcSrc.Left = (srcW - cw) / 2;
                rcSrc.Top = (srcH - ch) / 2;
                rcSrc.Right = rcSrc.Left + cw;
                rcSrc.Bottom = rcSrc.Top + ch;
            }

            int cropW = Math.Max(1, rcSrc.Right - rcSrc.Left);
            int cropH = Math.Max(1, rcSrc.Bottom - rcSrc.Top);
            var rcDst = new NativeWin32.RECT { Left = 0, Top = 0, Right = viewW, Bottom = viewH };

            if (cropModeIndex == 0 || cropModeIndex == 2)
            {
                float scale = Math.Min(viewW / (float)cropW, viewH / (float)cropH);
                int dw = Math.Max(1, (int)Math.Round(cropW * scale));
                int dh = Math.Max(1, (int)Math.Round(cropH * scale));
                int dx = (viewW - dw) / 2;
                int dy = (viewH - dh) / 2;
                rcDst = new NativeWin32.RECT { Left = dx, Top = dy, Right = dx + dw, Bottom = dy + dh };
            }

            var props = new NativeWin32.DWM_THUMBNAIL_PROPERTIES
            {
                dwFlags = NativeWin32.DWM_TNP_RECTDESTINATION | NativeWin32.DWM_TNP_RECTSOURCE |
                          NativeWin32.DWM_TNP_OPACITY | NativeWin32.DWM_TNP_VISIBLE |
                          NativeWin32.DWM_TNP_SOURCECLIENTAREAONLY,
                rcDestination = rcDst,
                rcSource = rcSrc,
                opacity = 255,
                fVisible = 1,
                fSourceClientAreaOnly = 0
            };
            NativeWin32.DwmUpdateThumbnailProperties(_thumbId, ref props);
            Invalidate();
        }

        private void ParkMinimizedWindowForLiveStream(IntPtr target)
        {
            if (target == IntPtr.Zero || !NativeWin32.IsWindow(target)) return;
            try
            {
                Screen scr = Screen.FromHandle(target) ?? Screen.PrimaryScreen;
                Rectangle bounds = scr.Bounds;
                Rectangle work = scr.WorkingArea;

                if (_parkedHwnd != target)
                {
                    var wp = new NativeWin32.WINDOWPLACEMENT();
                    wp.length = Marshal.SizeOf(typeof(NativeWin32.WINDOWPLACEMENT));
                    if (NativeWin32.GetWindowPlacement(target, ref wp))
                    {
                        _parkedNormalRect = wp.rcNormalPosition;
                        _parkedWasZoomed = (wp.flags & NativeWin32.WPF_RESTORETOMAXIMIZED) != 0 || wp.showCmd == NativeWin32.SW_SHOWMAXIMIZED;
                    }
                    else
                    {
                        NativeWin32.GetWindowRect(target, out _parkedNormalRect);
                        _parkedWasZoomed = false;
                    }
                    _parkedHwnd = target;
                }

                _parkedTick = Environment.TickCount;
                IntPtr prevFg = NativeWin32.GetForegroundWindow();

                NativeWin32.ShowWindow(target, NativeWin32.SW_SHOWNOACTIVATE);
                if (NativeWin32.IsIconic(target))
                    NativeWin32.ShowWindow(target, NativeWin32.SW_RESTORE);
                if (NativeWin32.IsZoomed(target))
                    NativeWin32.ShowWindow(target, NativeWin32.SW_SHOWNOACTIVATE);

                int parkW = _parkedWasZoomed
                    ? Math.Max(960, work.Width)
                    : Math.Max(820, _parkedNormalRect.Right - _parkedNormalRect.Left);
                int parkH = _parkedWasZoomed
                    ? Math.Max(600, work.Height)
                    : Math.Max(540, _parkedNormalRect.Bottom - _parkedNormalRect.Top);

                // Park at bottom-right seam (8px inside right edge, spanning 2px above & 46px behind WS_EX_LAYERED taskbar):
                // 1) Completely invisible to the user (looks minimized)
                // 2) Non-empty monitor intersection + unoccluded by maximized windows -> Chromium NativeWindowOcclusionTrackerWin keeps WebRTC <video> live at 60 FPS!
                int parkX = bounds.Right - 8;
                int parkY = work.Bottom - 2;

                NativeWin32.SetWindowPos(target, NativeWin32.HWND_BOTTOM, parkX, parkY, parkW, parkH,
                    NativeWin32.SWP_NOACTIVATE | NativeWin32.SWP_SHOWWINDOW);

                NativeWin32.PostMessage(target, NativeWin32.WM_NCACTIVATE, new IntPtr(1), IntPtr.Zero);
                NativeWin32.PostMessage(target, NativeWin32.WM_ACTIVATE, new IntPtr(1), IntPtr.Zero);

                if (prevFg != IntPtr.Zero && prevFg != target)
                    NativeWin32.SetForegroundWindow(prevFg);

                _lastSrcW = -1;
                _lastSrcH = -1;
            }
            catch { }
        }

        public void RestoreParkedWindow(bool bringToFront)
        {
            if (_parkedHwnd == IntPtr.Zero) return;
            IntPtr h = _parkedHwnd;
            NativeWin32.RECT orig = _parkedNormalRect;
            bool wasZoomed = _parkedWasZoomed;
            _parkedHwnd = IntPtr.Zero;
            _parkedWasZoomed = false;

            if (!NativeWin32.IsWindow(h)) return;
            try
            {
                int w = Math.Max(400, orig.Right - orig.Left);
                int hgt = Math.Max(300, orig.Bottom - orig.Top);
                if (bringToFront)
                {
                    NativeWin32.SetWindowPos(h, IntPtr.Zero, orig.Left, orig.Top, w, hgt, NativeWin32.SWP_SHOWWINDOW);
                    if (wasZoomed) NativeWin32.ShowWindow(h, NativeWin32.SW_SHOWMAXIMIZED);
                    else NativeWin32.ShowWindow(h, NativeWin32.SW_RESTORE);
                }
                else
                {
                    var wp = new NativeWin32.WINDOWPLACEMENT();
                    wp.length = Marshal.SizeOf(typeof(NativeWin32.WINDOWPLACEMENT));
                    if (NativeWin32.GetWindowPlacement(h, ref wp))
                    {
                        wp.rcNormalPosition = orig;
                        wp.flags = wasZoomed ? NativeWin32.WPF_RESTORETOMAXIMIZED : 0u;
                        wp.showCmd = (uint)NativeWin32.SW_SHOWMINNOACTIVE;
                        NativeWin32.SetWindowPlacement(h, ref wp);
                    }
                    else
                    {
                        NativeWin32.SetWindowPos(h, NativeWin32.HWND_BOTTOM, orig.Left, orig.Top, w, hgt, NativeWin32.SWP_NOACTIVATE);
                        NativeWin32.ShowWindow(h, NativeWin32.SW_SHOWMINNOACTIVE);
                    }
                }
            }
            catch { }
        }

        public void EnsureStreamSourceAlive(IntPtr ownerHwnd, StreamSourceInfo src, int cropModeIndex)
        {
            if (src == null || src.Hwnd == IntPtr.Zero || !NativeWin32.IsWindow(src.Hwnd)) return;
            IntPtr target = src.Hwnd;

            if (_parkedHwnd == target && !NativeWin32.IsIconic(target))
            {
                // If the user clicked Discord on the Taskbar or Alt+Tab to bring it back up, restore its original desktop rect immediately!
                if ((Environment.TickCount - _parkedTick) > 650 && NativeWin32.GetForegroundWindow() == target)
                {
                    RestoreParkedWindow(true);
                    _lastSrcW = -1;
                    if (_lastW > 30 && _lastH > 30)
                        SyncHardwareStream(ownerHwnd, _lastX, _lastY, _lastW, _lastH, src, cropModeIndex);
                    return;
                }
            }

            if (NativeWin32.IsIconic(target))
            {
                ParkMinimizedWindowForLiveStream(target);
                if (_lastW > 30 && _lastH > 30)
                    SyncHardwareStream(ownerHwnd, _lastX, _lastY, _lastW, _lastH, src, cropModeIndex);
            }
            else if (_lastW > 30 && _lastH > 30 && _thumbId != IntPtr.Zero)
            {
                NativeWin32.SIZE curSz;
                if (NativeWin32.DwmQueryThumbnailSourceSize(_thumbId, out curSz) == 0 &&
                    (curSz.cx != _lastSrcW || curSz.cy != _lastSrcH))
                {
                    SyncHardwareStream(ownerHwnd, _lastX, _lastY, _lastW, _lastH, src, cropModeIndex);
                }
            }
        }

        public void RefreshCropOrSource(IntPtr ownerHwnd, StreamSourceInfo src, int cropModeIndex)
        {
            _lastCropMode = -1;
            if (_lastW > 30 && _lastH > 30)
                SyncHardwareStream(ownerHwnd, _lastX, _lastY, _lastW, _lastH, src, cropModeIndex);
        }

        public void HideStream()
        {
            RestoreParkedWindow(false);
            UnregisterThumb();
            _lastX = -9999; _lastY = -9999; _lastW = -1; _lastH = -1;
            _lastCropMode = -1;
            if (IsHandleCreated && Visible)
            {
                Hide();
            }
        }

        private void UnregisterThumb()
        {
            if (_thumbId != IntPtr.Zero)
            {
                try { NativeWin32.DwmUnregisterThumbnail(_thumbId); } catch { }
                _thumbId = IntPtr.Zero;
            }
            _boundSourceHwnd = IntPtr.Zero;
        }

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            UnregisterThumb();
            base.OnFormClosing(e);
        }
    }
    public sealed class StreamCaptureEngine
    {
        private readonly object _frameLock = new object();
        private IntPtr _selectedHwnd = IntPtr.Zero;
        private List<StreamSourceInfo> _sources = new List<StreamSourceInfo>();
        private int _cropModeIndex = 0;
        public static readonly string[] CropModeNames = new string[]
        {
            "Стрим (Авто)",
            "На весь остров",
            "Окно целиком",
            "Крупный план"
        };

        public int CropModeIndex
        {
            get { return _cropModeIndex; }
            set { _cropModeIndex = Math.Max(0, value % CropModeNames.Length); }
        }

        public string CurrentCropLabel { get { return CropModeNames[_cropModeIndex]; } }

        public List<StreamSourceInfo> GetAvailableSources(IntPtr selfHwnd)
        {
            var list = new List<StreamSourceInfo>();
            try
            {
                NativeWin32.EnumWindows((hWnd, lParam) =>
                {
                    if (hWnd == IntPtr.Zero || hWnd == selfHwnd) return true;
                    if (!NativeWin32.IsWindowVisible(hWnd)) return true;

                    var sbTitle = new StringBuilder(256);
                    NativeWin32.GetWindowText(hWnd, sbTitle, 256);
                    string title = sbTitle.ToString().Trim();
                    if (string.IsNullOrEmpty(title) || title == "Program Manager" || title == "Dynamic Island Settings" || title == "Dynamic Island Studio")
                        return true;

                    var sbCls = new StringBuilder(128);
                    NativeWin32.GetClassName(hWnd, sbCls, 128);
                    string cls = sbCls.ToString();
                    if (cls == "Progman" || cls == "WorkerW" || cls == "Shell_TrayWnd") return true;

                    NativeWin32.RECT rc;
                    if (!NativeWin32.GetWindowRect(hWnd, out rc)) return true;
                    int w = rc.Right - rc.Left;
                    int h = rc.Bottom - rc.Top;
                    if (!NativeWin32.IsIconic(hWnd) && (w < 200 || h < 140)) return true;

                    uint pid;
                    NativeWin32.GetWindowThreadProcessId(hWnd, out pid);
                    string procName = "";
                    try { procName = Process.GetProcessById((int)pid).ProcessName; } catch { }

                    string lowerProc = (procName ?? "").ToLowerInvariant();
                    string lowerTitle = title.ToLowerInvariant();

                    int score = 10;
                    string cat = procName;

                    if (lowerProc.Contains("discord"))
                    {
                        cat = "Discord";
                        score = 1100;
                    }
                    else if (lowerTitle.Contains("discord"))
                    {
                        cat = "Discord";
                        score = 1000;
                    }
                    else if (lowerTitle.Contains("youtube") || lowerTitle.Contains("twitch") || lowerTitle.Contains("kick") ||
                             lowerTitle.Contains("vk видео") || lowerTitle.Contains("кинопоиск") || lowerTitle.Contains("rutube") ||
                             lowerTitle.Contains("netflix") || lowerTitle.Contains("стрим") || lowerTitle.Contains("stream"))
                    {
                        if (lowerTitle.Contains("youtube")) cat = "YouTube";
                        else if (lowerTitle.Contains("twitch")) cat = "Twitch";
                        else if (lowerTitle.Contains("vk")) cat = "VK Видео";
                        else if (lowerTitle.Contains("кинопоиск")) cat = "Кинопоиск";
                        else if (lowerTitle.Contains("rutube")) cat = "Rutube";
                        else cat = "Видеохостинг";
                        score = 950;
                    }
                    else if (lowerProc.Contains("vlc") || lowerProc.Contains("mpv") || lowerProc.Contains("potplayer") || lowerProc.Contains("obs"))
                    {
                        cat = procName.ToUpperInvariant();
                        score = 880;
                    }
                    else if (lowerProc.Contains("chrome") || lowerProc.Contains("msedge") || lowerProc.Contains("firefox") ||
                             lowerProc.Contains("opera") || lowerProc.Contains("browser") || lowerProc.Contains("brave"))
                    {
                        cat = "Браузер";
                        score = 700;
                    }
                    else if (lowerProc.Contains("telegram"))
                    {
                        cat = "Telegram";
                        score = 650;
                    }

                    list.Add(new StreamSourceInfo
                    {
                        Hwnd = hWnd,
                        ProcessName = procName,
                        CategoryLabel = cat,
                        WindowTitle = title,
                        PriorityScore = score
                    });
                    return true;
                }, IntPtr.Zero);
            }
            catch { }

            list.Sort((a, b) => b.PriorityScore.CompareTo(a.PriorityScore));
            lock (_frameLock)
            {
                _sources = list;
            }
            return list;
        }

        public StreamSourceInfo GetActiveSource(IntPtr selfHwnd)
        {
            List<StreamSourceInfo> list;
            lock (_frameLock) { list = _sources; }
            if (list == null || list.Count == 0)
                list = GetAvailableSources(selfHwnd);
            if (list.Count == 0) return null;

            if (_selectedHwnd != IntPtr.Zero)
            {
                foreach (var s in list)
                    if (s.Hwnd == _selectedHwnd) return s;
            }
            _selectedHwnd = list[0].Hwnd;
            return list[0];
        }

        public void CycleSource(IntPtr selfHwnd, int dir = 1)
        {
            var list = GetAvailableSources(selfHwnd);
            if (list.Count == 0) return;
            int idx = 0;
            for (int i = 0; i < list.Count; i++)
            {
                if (list[i].Hwnd == _selectedHwnd) { idx = i; break; }
            }
            idx = (idx + dir + list.Count) % list.Count;
            _selectedHwnd = list[idx].Hwnd;
        }

        public void SelectSource(IntPtr hwnd)
        {
            _selectedHwnd = hwnd;
        }

        public bool CaptureFrame(IntPtr selfHwnd)
        {
            return GetActiveSource(selfHwnd) != null;
        }
    }
    public sealed class CustomBackgroundEngine
    {
        private string _loadedPath = "";
        private readonly List<Bitmap> _frames = new List<Bitmap>();
        private readonly List<int> _frameDelaysMs = new List<int>();
        private int _currentFrameIdx = 0;
        private long _lastFrameSwitchTick = 0;
        private readonly ImageAttributes _wrapAttrs;

        public bool HasImage { get { return _frames.Count > 0; } }
        public bool IsAnimated { get { return _frames.Count > 1; } }

        public CustomBackgroundEngine()
        {
            _wrapAttrs = new ImageAttributes();
            _wrapAttrs.SetWrapMode(WrapMode.TileFlipXY);
        }

        public void EnsureLoaded(string path)
        {
            string norm = (path ?? "").Trim();
            if (string.Equals(norm, _loadedPath, StringComparison.OrdinalIgnoreCase))
                return;

            ClearFrames();
            _loadedPath = norm;
            if (string.IsNullOrEmpty(norm) || !File.Exists(norm))
                return;

            try
            {
                byte[] rawBytes = File.ReadAllBytes(norm);
                using (var ms = new MemoryStream(rawBytes))
                using (Image img = Image.FromStream(ms, true, true))
                {
                    Guid[] dims = img.FrameDimensionsList;
                    bool hasTimeFrames = false;
                    FrameDimension timeDim = FrameDimension.Time;
                    int frameCount = 1;

                    if (dims != null)
                    {
                        foreach (Guid g in dims)
                        {
                            if (g == FrameDimension.Time.Guid)
                            {
                                hasTimeFrames = true;
                                frameCount = img.GetFrameCount(timeDim);
                                break;
                            }
                        }
                    }

                    int[] delays = new int[Math.Max(1, frameCount)];
                    for (int i = 0; i < delays.Length; i++) delays[i] = 33;
                    if (hasTimeFrames && frameCount > 1)
                    {
                        try
                        {
                            PropertyItem prop = img.GetPropertyItem(0x5100);
                            if (prop != null && prop.Value != null)
                            {
                                for (int i = 0; i < frameCount && (i * 4 + 3) < prop.Value.Length; i++)
                                {
                                    int delayCs = BitConverter.ToInt32(prop.Value, i * 4);
                                    int delayMs = delayCs * 10;
                                    delays[i] = (delayMs < 16) ? 33 : Math.Min(2000, delayMs);
                                }
                            }
                        }
                        catch { }
                    }

                    int targetW = img.Width;
                    int targetH = img.Height;
                    if (targetW > 1000 || targetH > 500)
                    {
                        float ratio = Math.Min(1000f / targetW, 500f / targetH);
                        targetW = Math.Max(1, (int)Math.Round(targetW * ratio));
                        targetH = Math.Max(1, (int)Math.Round(targetH * ratio));
                    }

                    int maxFrames = Math.Min(frameCount, 180);
                    for (int i = 0; i < maxFrames; i++)
                    {
                        if (hasTimeFrames && frameCount > 1)
                            img.SelectActiveFrame(timeDim, i);

                        var frameBmp = new Bitmap(targetW, targetH, PixelFormat.Format32bppPArgb);
                        frameBmp.SetResolution(96f, 96f);
                        using (Graphics fg = Graphics.FromImage(frameBmp))
                        {
                            fg.CompositingMode = CompositingMode.SourceCopy;
                            fg.CompositingQuality = CompositingQuality.HighQuality;
                            fg.InterpolationMode = InterpolationMode.HighQualityBicubic;
                            fg.PixelOffsetMode = PixelOffsetMode.HighQuality;
                            fg.DrawImage(img, new Rectangle(0, 0, targetW, targetH), 0, 0, img.Width, img.Height, GraphicsUnit.Pixel, _wrapAttrs);
                        }
                        _frames.Add(frameBmp);
                        _frameDelaysMs.Add(delays[i]);
                    }
                    _currentFrameIdx = 0;
                    _lastFrameSwitchTick = Environment.TickCount;
                }
            }
            catch { ClearFrames(); }
        }

        public bool AdvanceAnimationFrame()
        {
            if (_frames.Count <= 1) return false;
            long now = Environment.TickCount;
            int delay = _frameDelaysMs[_currentFrameIdx];
            if (now - _lastFrameSwitchTick >= delay)
            {
                _currentFrameIdx = (_currentFrameIdx + 1) % _frames.Count;
                _lastFrameSwitchTick = now;
                return true;
            }
            return false;
        }

        public void DrawBackgroundIntoCapsule(Graphics g, GraphicsPath capsulePath, RectangleF pill, string fitMode, int dimPercent, bool isMorphing)
        {
            if (_frames.Count == 0) return;
            Bitmap bmp = _frames[_currentFrameIdx];
            if (bmp == null) return;

            Region prevClip = g.Clip;
            InterpolationMode prevInterp = g.InterpolationMode;
            PixelOffsetMode prevOffset = g.PixelOffsetMode;

            try
            {
                g.SetClip(capsulePath, CombineMode.Replace);
                g.InterpolationMode = isMorphing ? InterpolationMode.Bilinear : InterpolationMode.HighQualityBicubic;
                g.PixelOffsetMode = PixelOffsetMode.HighQuality;

                RectangleF destRect = pill;
                RectangleF srcRect = new RectangleF(0, 0, bmp.Width, bmp.Height);

                if (fitMode != null && fitMode.Contains("Cover"))
                {
                    float targetAspect = pill.Width / Math.Max(1f, pill.Height);
                    float imgAspect = bmp.Width / (float)Math.Max(1, bmp.Height);
                    if (imgAspect > targetAspect)
                    {
                        float cropW = bmp.Height * targetAspect;
                        srcRect = new RectangleF((bmp.Width - cropW) * 0.5f, 0, cropW, bmp.Height);
                    }
                    else
                    {
                        float cropH = bmp.Width / targetAspect;
                        srcRect = new RectangleF(0, (bmp.Height - cropH) * 0.5f, bmp.Width, cropH);
                    }
                }
                else if (fitMode != null && fitMode.Contains("Contain"))
                {
                    using (var baseBlack = new SolidBrush(Color.FromArgb(248, 8, 8, 11)))
                        g.FillPath(baseBlack, capsulePath);
                    float scale = Math.Min(pill.Width / Math.Max(1f, bmp.Width), pill.Height / Math.Max(1f, bmp.Height));
                    float dw = bmp.Width * scale;
                    float dh = bmp.Height * scale;
                    destRect = new RectangleF(pill.X + (pill.Width - dw) * 0.5f, pill.Y + (pill.Height - dh) * 0.5f, dw, dh);
                }

                PointF[] destPoints = new PointF[]
                {
                    new PointF(destRect.Left, destRect.Top),
                    new PointF(destRect.Right, destRect.Top),
                    new PointF(destRect.Left, destRect.Bottom)
                };
                g.DrawImage(bmp, destPoints, srcRect, GraphicsUnit.Pixel, _wrapAttrs);

                if (dimPercent > 0)
                {
                    int dimAlpha = Math.Max(0, Math.Min(230, (int)Math.Round(dimPercent * 2.55f)));
                    using (var dimBrush = new SolidBrush(Color.FromArgb(dimAlpha, 6, 7, 10)))
                        g.FillPath(dimBrush, capsulePath);
                }
            }
            finally
            {
                g.Clip = prevClip;
                g.InterpolationMode = prevInterp;
                g.PixelOffsetMode = prevOffset;
            }
        }

        private void ClearFrames()
        {
            foreach (var b in _frames)
            {
                try { b.Dispose(); } catch { }
            }
            _frames.Clear();
            _frameDelaysMs.Clear();
            _currentFrameIdx = 0;
        }
    }    // =========================================================================
    // 3. CORE AUDIO COM INTEROP + ASYNC MULTI-PLAYER WINRT SMTC ENGINE
    // =========================================================================
    [ComImport, Guid("BCDE0395-E52F-467C-8E3D-C4579291692E")]
    internal class MMDeviceEnumeratorComObject { }

    [Guid("A95664D2-9614-4F35-A746-DE8DB63617E6"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    internal interface IMMDeviceEnumerator
    {
        int EnumAudioEndpoints(int dataFlow, int dwStateMask, out IntPtr ppDevices);
        int GetDefaultAudioEndpoint(int dataFlow, int role, out IMMDevice ppEndpoint);
    }

    [Guid("D666063F-1587-4E43-81F1-B948E807363F"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    internal interface IMMDevice
    {
        int Activate(ref Guid iid, int dwClsCtx, IntPtr pActivationParams, [MarshalAs(UnmanagedType.IUnknown)] out object ppInterface);
    }

    [Guid("657804FA-D6AD-4496-8A60-352752AF4F89"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    internal interface IAudioEndpointVolumeCallback
    {
        [PreserveSig]
        int OnNotify(IntPtr pNotifyData);
    }

    internal sealed class AudioVolumeNotifySink : IAudioEndpointVolumeCallback
    {
        private readonly Action _onChanged;
        public AudioVolumeNotifySink(Action onChanged) { _onChanged = onChanged; }
        public int OnNotify(IntPtr pNotifyData)
        {
            try { if (_onChanged != null) _onChanged(); } catch { }
            return 0;
        }
    }

    [Guid("5CDF2C82-841E-4546-9722-0CF74078229A"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    internal interface IAudioEndpointVolume
    {
        int RegisterControlChangeNotify([MarshalAs(UnmanagedType.Interface)] IAudioEndpointVolumeCallback pNotify);
        int UnregisterControlChangeNotify([MarshalAs(UnmanagedType.Interface)] IAudioEndpointVolumeCallback pNotify);
        int GetChannelCount(out uint pnChannelCount);
        int SetMasterVolumeLevel(float fLevelDB, ref Guid pguidEventContext);
        int SetMasterVolumeLevelScalar(float fLevel, ref Guid pguidEventContext);
        int GetMasterVolumeLevel(out float pfLevelDB);
        int GetMasterVolumeLevelScalar(out float pfLevel);
        int SetChannelVolumeLevel(uint nChannel, float fLevelDB, ref Guid pguidEventContext);
        int SetChannelVolumeLevelScalar(uint nChannel, float fLevel, ref Guid pguidEventContext);
        int GetChannelVolumeLevel(uint nChannel, out float pfLevelDB);
        int GetChannelVolumeLevelScalar(uint nChannel, out float pfLevel);
        int SetMute([MarshalAs(UnmanagedType.Bool)] bool bMute, ref Guid pguidEventContext);
        int GetMute([MarshalAs(UnmanagedType.Bool)] out bool pbMute);
    }

    [Guid("C02216F6-8C67-4B5B-9D00-D008E73E0064"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    internal interface IAudioMeterInformation
    {
        int GetPeakValue(out float pfPeak);
        int GetMeteringChannelCount(out uint pnChannelCount);
        int GetChannelsPeakValues(uint u32ChannelCount, [Out, MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 0)] float[] afPeakValues);
        int QueryHardwareSupport(out uint pdwHardwareSupportMask);
    }

    [Guid("77AA99A0-1BD6-484F-8BC7-2C654C9A9B6F"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    internal interface IAudioSessionManager2
    {
        int GetAudioSessionControl(IntPtr AudioSessionGuid, uint StreamFlags, out IntPtr SessionControl);
        int GetSimpleAudioVolume(IntPtr AudioSessionGuid, uint StreamFlags, out ISimpleAudioVolume AudioVolume);
        int GetSessionEnumerator(out IAudioSessionEnumerator SessionEnum);
    }

    [Guid("E2F5BB11-0570-40CA-ACDD-3AA01277DEE8"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    internal interface IAudioSessionEnumerator
    {
        int GetCount(out int SessionCount);
        int GetSession(int SessionCount, [MarshalAs(UnmanagedType.IUnknown)] out object Session);
    }

    [Guid("bfb7ff88-7239-4fc9-8fa2-07c950be9c6d"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    internal interface IAudioSessionControl2
    {
        int GetState(out int pRetVal);
        int GetDisplayName([MarshalAs(UnmanagedType.LPWStr)] out string pRetVal);
        int SetDisplayName([MarshalAs(UnmanagedType.LPWStr)] string Value, ref Guid EventContext);
        int GetIconPath([MarshalAs(UnmanagedType.LPWStr)] out string pRetVal);
        int SetIconPath([MarshalAs(UnmanagedType.LPWStr)] string Value, ref Guid EventContext);
        int GetGroupingParam(out Guid pRetVal);
        int SetGroupingParam(ref Guid Override, ref Guid EventContext);
        int RegisterAudioSessionNotification(IntPtr NewNotifications);
        int UnregisterAudioSessionNotification(IntPtr NewNotifications);
        int GetSessionIdentifier([MarshalAs(UnmanagedType.LPWStr)] out string pRetVal);
        int GetSessionInstanceIdentifier([MarshalAs(UnmanagedType.LPWStr)] out string pRetVal);
        int GetProcessId(out uint pRetVal);
        int IsSystemSoundsSession();
        int SetDuckingPreference(bool optOut);
    }

    [Guid("87CE5498-68D6-44E5-9215-6DA47EF883D8"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    internal interface ISimpleAudioVolume
    {
        int SetMasterVolume(float fLevel, ref Guid EventContext);
        int GetMasterVolume(out float pfLevel);
        int SetMute([MarshalAs(UnmanagedType.Bool)] bool bMute, ref Guid EventContext);
        int GetMute([MarshalAs(UnmanagedType.Bool)] out bool pbMute);
    }

    public sealed class CoreAudioBridge : IDisposable
    {
        private IAudioEndpointVolume _endpoint;
        private IAudioMeterInformation _meter;
        private IAudioSessionManager2 _sessionMgr;
        private IAudioEndpointVolume _micEndpoint;
        private IAudioSessionManager2 _micSessionMgr;
        private AudioVolumeNotifySink _renderSink;
        private AudioVolumeNotifySink _micSink;
        public event Action OnAudioEndpointChanged;

        public int LastVolume { get; private set; }
        public bool LastMuted { get; private set; }
        public int LastMusicVolume { get; private set; }
        public bool LastMusicMuted { get; private set; }
        public bool IsMicActive { get; private set; }
        public bool IsMicMuted { get; private set; }
        public string ActiveMicAppName { get; private set; }

        private static readonly string[] MusicProcessKeywords = new string[]
        {
            "яндекс", "yandex", "spotify", "aimp", "foobar", "vlc", "potplayer",
            "mpc-hc", "wmplayer", "itunes", "applemusic", "zunemusic", "music.ui",
            "deezer", "tidal", "soundcloud", "vkmusic", "winamp", "musicbee",
            "chrome", "msedge", "firefox", "opera", "brave"
        };

        public CoreAudioBridge()
        {
            LastVolume = 60;
            LastMuted = false;
            LastMusicVolume = 100;
            LastMusicMuted = false;
            InitializeEndpoint();
        }

        public void InitializeEndpoint()
        {
            try
            {
                var enumerator = (IMMDeviceEnumerator)(new MMDeviceEnumeratorComObject());
                IMMDevice device;
                if (enumerator.GetDefaultAudioEndpoint(0, 1, out device) == 0 && device != null)
                {
                    Guid iidVol = typeof(IAudioEndpointVolume).GUID;
                    object objVol;
                    if (device.Activate(ref iidVol, 23, IntPtr.Zero, out objVol) == 0 && objVol != null)
                    {
                        _endpoint = (IAudioEndpointVolume)objVol;
                        _renderSink = new AudioVolumeNotifySink(() =>
                        {
                            RefreshState();
                            if (OnAudioEndpointChanged != null) OnAudioEndpointChanged();
                        });
                        try { _endpoint.RegisterControlChangeNotify(_renderSink); } catch { }
                        RefreshState();
                    }

                    Guid iidMeter = typeof(IAudioMeterInformation).GUID;
                    object objMeter;
                    if (device.Activate(ref iidMeter, 23, IntPtr.Zero, out objMeter) == 0 && objMeter != null)
                    {
                        _meter = (IAudioMeterInformation)objMeter;
                    }

                    Guid iidSes = typeof(IAudioSessionManager2).GUID;
                    object objSes;
                    if (device.Activate(ref iidSes, 23, IntPtr.Zero, out objSes) == 0 && objSes != null)
                    {
                        _sessionMgr = (IAudioSessionManager2)objSes;
                    }
                }
            }
            catch { _endpoint = null; _meter = null; _sessionMgr = null; }
        }

        private List<ISimpleAudioVolume> FindMusicAudioSessions(string targetAppId)
        {
            var matched = new List<ISimpleAudioVolume>();
            var fallbackMusic = new List<ISimpleAudioVolume>();
            if (_sessionMgr == null) InitializeEndpoint();
            if (_sessionMgr == null) return matched;

            string cleanTarget = (targetAppId ?? "").Trim();
            if (cleanTarget.EndsWith(".exe", StringComparison.OrdinalIgnoreCase))
                cleanTarget = cleanTarget.Substring(0, cleanTarget.Length - 4);

            try
            {
                IAudioSessionEnumerator sesEnum;
                if (_sessionMgr.GetSessionEnumerator(out sesEnum) != 0 || sesEnum == null)
                    return matched;
                int count;
                if (sesEnum.GetCount(out count) != 0)
                    return matched;

                for (int i = 0; i < count; i++)
                {
                    object raw;
                    if (sesEnum.GetSession(i, out raw) != 0 || raw == null) continue;
                    var ctrl2 = raw as IAudioSessionControl2;
                    var simpleVol = raw as ISimpleAudioVolume;
                    if (ctrl2 == null || simpleVol == null) continue;

                    uint pid;
                    ctrl2.GetProcessId(out pid);
                    if (pid == 0) continue;

                    string procName = "";
                    try { procName = Process.GetProcessById((int)pid).ProcessName; } catch { }
                    string sesId = "";
                    try { ctrl2.GetSessionIdentifier(out sesId); } catch { }

                    string combined = (procName + " " + sesId).ToLowerInvariant();

                    if (!string.IsNullOrEmpty(cleanTarget))
                    {
                        string tLow = cleanTarget.ToLowerInvariant();
                        if (combined.Contains(tLow) ||
                            ((tLow.Contains("яндекс") || tLow.Contains("yandex")) && (combined.Contains("яндекс") || combined.Contains("yandex"))))
                        {
                            matched.Add(simpleVol);
                            continue;
                        }
                    }

                    foreach (string kw in MusicProcessKeywords)
                    {
                        if (combined.Contains(kw))
                        {
                            fallbackMusic.Add(simpleVol);
                            break;
                        }
                    }
                }
            }
            catch { _sessionMgr = null; }

            return matched.Count > 0 ? matched : fallbackMusic;
        }

        public void RefreshMusicVolume(string targetAppId)
        {
            var sessions = FindMusicAudioSessions(targetAppId);
            if (sessions.Count == 0) return;
            try
            {
                float level;
                bool mute;
                if (sessions[0].GetMasterVolume(out level) == 0)
                    LastMusicVolume = Math.Max(0, Math.Min(100, (int)Math.Round(level * 100.0f)));
                if (sessions[0].GetMute(out mute) == 0)
                    LastMusicMuted = mute;
            }
            catch { }
        }

        public void SetMusicVolume(int percent, string targetAppId)
        {
            int clamped = Math.Max(0, Math.Min(100, percent));
            LastMusicVolume = clamped;
            var sessions = FindMusicAudioSessions(targetAppId);
            Guid ctx = Guid.Empty;
            foreach (var vol in sessions)
            {
                try
                {
                    vol.SetMasterVolume(clamped / 100.0f, ref ctx);
                    if (clamped > 0 && LastMusicMuted)
                    {
                        vol.SetMute(false, ref ctx);
                        LastMusicMuted = false;
                    }
                }
                catch { }
            }
        }

        public void ToggleMusicMute(string targetAppId)
        {
            LastMusicMuted = !LastMusicMuted;
            var sessions = FindMusicAudioSessions(targetAppId);
            Guid ctx = Guid.Empty;
            foreach (var vol in sessions)
            {
                try { vol.SetMute(LastMusicMuted, ref ctx); } catch { }
            }
        }

        public void SampleAudioPeaks(out float masterPeak, out float leftPeak, out float rightPeak)
        {
            masterPeak = 0f; leftPeak = 0f; rightPeak = 0f;
            if (_meter == null) return;
            try
            {
                float p;
                if (_meter.GetPeakValue(out p) == 0)
                    masterPeak = Math.Max(0f, Math.Min(1f, p));
                uint ch;
                if (_meter.GetMeteringChannelCount(out ch) == 0 && ch >= 2)
                {
                    float[] peaks = new float[ch];
                    if (_meter.GetChannelsPeakValues(ch, peaks) == 0)
                    {
                        leftPeak = Math.Max(0f, Math.Min(1f, peaks[0]));
                        rightPeak = Math.Max(0f, Math.Min(1f, peaks[1]));
                        return;
                    }
                }
                leftPeak = masterPeak;
                rightPeak = masterPeak;
            }
            catch { _meter = null; }
        }

        public bool RefreshState()
        {
            if (_endpoint == null) return false;
            try
            {
                float scalar; bool muted;
                _endpoint.GetMasterVolumeLevelScalar(out scalar);
                _endpoint.GetMute(out muted);
                int pct = Math.Max(0, Math.Min(100, (int)Math.Round(scalar * 100.0f)));
                bool changed = (pct != LastVolume) || (muted != LastMuted);
                LastVolume = pct; LastMuted = muted;
                return changed;
            }
            catch { _endpoint = null; return false; }
        }

        public void SetVolume(int percent)
        {
            int clamped = Math.Max(0, Math.Min(100, percent));
            LastVolume = clamped;
            if (_endpoint == null) InitializeEndpoint();
            if (_endpoint != null)
            {
                try
                {
                    Guid ctx = Guid.Empty;
                    _endpoint.SetMasterVolumeLevelScalar(clamped / 100.0f, ref ctx);
                    if (clamped > 0 && LastMuted) { _endpoint.SetMute(false, ref ctx); LastMuted = false; }
                }
                catch { }
            }
        }

        public void ToggleMute()
        {
            LastMuted = !LastMuted;
            if (_endpoint == null) InitializeEndpoint();
            if (_endpoint != null)
            {
                try { Guid ctx = Guid.Empty; _endpoint.SetMute(LastMuted, ref ctx); } catch { }
            }
        }

        private void EnsureMicEndpoint()
        {
            if (_micEndpoint != null && _micSessionMgr != null) return;
            try
            {
                var enumerator = (IMMDeviceEnumerator)(new MMDeviceEnumeratorComObject());
                IMMDevice capDevice;
                // dataFlow = 1 (eCapture), role = 2 (eCommunications) with fallback to 1 (eConsole)
                if (enumerator.GetDefaultAudioEndpoint(1, 2, out capDevice) != 0 || capDevice == null)
                    enumerator.GetDefaultAudioEndpoint(1, 1, out capDevice);

                if (capDevice != null)
                {
                    Guid iidVol = typeof(IAudioEndpointVolume).GUID;
                    object objVol;
                    if (capDevice.Activate(ref iidVol, 23, IntPtr.Zero, out objVol) == 0 && objVol != null)
                    {
                        _micEndpoint = (IAudioEndpointVolume)objVol;
                        _micSink = new AudioVolumeNotifySink(() =>
                        {
                            RefreshMicrophoneState();
                            if (OnAudioEndpointChanged != null) OnAudioEndpointChanged();
                        });
                        try { _micEndpoint.RegisterControlChangeNotify(_micSink); } catch { }
                    }

                    Guid iidSes = typeof(IAudioSessionManager2).GUID;
                    object objSes;
                    if (capDevice.Activate(ref iidSes, 23, IntPtr.Zero, out objSes) == 0 && objSes != null)
                        _micSessionMgr = (IAudioSessionManager2)objSes;
                }
            }
            catch { _micEndpoint = null; _micSessionMgr = null; }
        }

        public void RefreshMicrophoneState()
        {
            EnsureMicEndpoint();
            bool muted = false;
            bool active = false;
            string appName = "";

            if (_micEndpoint != null)
            {
                try { _micEndpoint.GetMute(out muted); }
                catch { _micEndpoint = null; }
            }

            if (_micSessionMgr != null)
            {
                try
                {
                    IAudioSessionEnumerator sesEnum;
                    if (_micSessionMgr.GetSessionEnumerator(out sesEnum) == 0 && sesEnum != null)
                    {
                        int count;
                        if (sesEnum.GetCount(out count) == 0)
                        {
                            for (int i = 0; i < count; i++)
                            {
                                object raw;
                                if (sesEnum.GetSession(i, out raw) != 0 || raw == null) continue;
                                var ctrl2 = raw as IAudioSessionControl2;
                                if (ctrl2 == null) continue;
                                int state;
                                if (ctrl2.GetState(out state) == 0 && state == 1) // AudioSessionStateActive == 1
                                {
                                    uint pid;
                                    ctrl2.GetProcessId(out pid);
                                    if (pid > 0)
                                    {
                                        active = true;
                                        try { appName = Process.GetProcessById((int)pid).ProcessName; } catch { appName = "Mic"; }
                                        break;
                                    }
                                }
                            }
                        }
                    }
                }
                catch { _micSessionMgr = null; }
            }

            IsMicMuted = muted;
            IsMicActive = active;
            ActiveMicAppName = appName ?? "";
        }

        public void ToggleMicrophoneMute()
        {
            EnsureMicEndpoint();
            if (_micEndpoint == null) return;
            try
            {
                IsMicMuted = !IsMicMuted;
                Guid ctx = Guid.Empty;
                _micEndpoint.SetMute(IsMicMuted, ref ctx);
            }
            catch { _micEndpoint = null; }
        }

        public void Dispose()
        {
            try { if (_endpoint != null && _renderSink != null) _endpoint.UnregisterControlChangeNotify(_renderSink); } catch { }
            try { if (_micEndpoint != null && _micSink != null) _micEndpoint.UnregisterControlChangeNotify(_micSink); } catch { }
            _endpoint = null;
            _micEndpoint = null;
            _meter = null;
            _sessionMgr = null;
            _micSessionMgr = null;
        }
    }

    public sealed class MediaSessionSnapshot
    {
        public string AppId = "";
        public string DisplayAppName = "Музыка";
        public string Title = "Не воспроизводится";
        public string Artist = "Яндекс Музыка • Spotify • AIMP";
        public bool IsPlaying = false;
        public bool IsDedicatedPlayer = false;
        public double PositionSec = 0.0;
        public double DurationSec = 0.0;
        public DateTime SampledAtUtc = DateTime.UtcNow;
        public Bitmap CoverBitmap = null;
        public object NativeSession = null;
        public int ProcessId = 0;
        public string ExecutablePath = "";

        public double GetLivePositionSec()
        {
            if (DurationSec <= 0) return 0.0;
            if (!IsPlaying) return Math.Max(0.0, Math.Min(DurationSec, PositionSec));
            double dt = Math.Max(0.0, (DateTime.UtcNow - SampledAtUtc).TotalSeconds);
            return Math.Max(0.0, Math.Min(DurationSec, PositionSec + dt));
        }
    }    public static class MultiPlayerMediaController
    {
        private static readonly object _coverLock = new object();
        private static readonly Queue<Bitmap> _retiredCovers = new Queue<Bitmap>();
        private static string _cachedCoverKey = "";
        private static Bitmap _cachedCoverBmp = null;
        private static int _selectedPlayerIndex = 0;
        private static string _manualSelectedAppId = "";
        private static readonly Dictionary<string, string> _exePathCache = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        private static readonly string[] DedicatedPlayerKeywords = new string[]
        {
            "яндекс", "yandex", "spotify", "aimp", "foobar", "vlc", "potplayer",
            "mpc-hc", "wmplayer", "itunes", "applemusic", "zunemusic", "music.ui",
            "deezer", "tidal", "soundcloud", "vkmusic", "winamp", "musicbee"
        };

        private static readonly string[] BrowserKeywords = new string[]
        {
            "chrome", "msedge", "firefox", "opera", "brave", "vivaldi", "browser"
        };

        private static T WaitWinRt<T>(IAsyncOperation<T> op, int timeoutMs = 550)
        {
            if (op == null) return default(T);
            try
            {
                IAsyncInfo info = (IAsyncInfo)op;
                int waited = 0;
                while (info.Status == AsyncStatus.Started && waited < timeoutMs)
                {
                    Thread.Sleep(4);
                    waited += 4;
                }
                if (info.Status == AsyncStatus.Completed)
                    return op.GetResults();
            }
            catch { }
            return default(T);
        }

        public static bool IsDedicatedMusicApp(string appIdOrName)
        {
            if (string.IsNullOrEmpty(appIdOrName)) return false;
            string lower = appIdOrName.ToLowerInvariant();
            foreach (string kw in BrowserKeywords)
                if (lower.Contains(kw)) return false;
            foreach (string kw in DedicatedPlayerKeywords)
                if (lower.Contains(kw)) return true;
            return !lower.Contains("explorer");
        }

        public static string FormatFriendlyAppName(string rawAppId)
        {
            if (string.IsNullOrWhiteSpace(rawAppId)) return "System Media";
            string clean = rawAppId.Trim();
            if (clean.EndsWith(".exe", StringComparison.OrdinalIgnoreCase))
                clean = clean.Substring(0, clean.Length - 4);
            string lower = clean.ToLowerInvariant();
            if (lower.Contains("яндекс") || lower.Contains("yandex")) return "Яндекс Музыка";
            if (lower.Contains("spotify")) return "Spotify";
            if (lower.Contains("aimp")) return "AIMP";
            if (lower.Contains("foobar")) return "foobar2000";
            if (lower.Contains("vlc")) return "VLC";
            if (lower.Contains("chrome")) return "Chrome";
            if (lower.Contains("msedge") || lower.Contains("edge")) return "Edge";
            if (lower.Contains("firefox")) return "Firefox";
            if (lower.Contains("opera")) return "Opera";
            if (lower.Contains("zunemusic") || lower.Contains("music.ui")) return "Media Player";
            if (lower.Contains("applemusic") || lower.Contains("itunes")) return "Apple Music";
            if (lower.Contains("telegram")) return "Telegram";
            if (clean.Contains("!"))
            {
                string[] parts = clean.Split('!');
                clean = parts[parts.Length - 1];
            }
            return clean;
        }

        public static List<MediaSessionSnapshot> QueryAllPlayers(bool preferDedicatedPlayers, out MediaSessionSnapshot activeSession, out int activeIndex)
        {
            var list = new List<MediaSessionSnapshot>();
            try
            {
                var mgr = WaitWinRt(GlobalSystemMediaTransportControlsSessionManager.RequestAsync(), 550);
                if (mgr != null)
                {
                    var sessions = mgr.GetSessions();
                    if (sessions != null)
                    {
                        foreach (var s in sessions)
                        {
                            if (s == null) continue;
                            string appId = s.SourceAppUserModelId ?? "SystemPlayer";
                            var pb = s.GetPlaybackInfo();
                            bool isPlaying = pb != null && pb.PlaybackStatus == GlobalSystemMediaTransportControlsSessionPlaybackStatus.Playing;

                            var props = WaitWinRt(s.TryGetMediaPropertiesAsync(), 450);
                            string title = (props != null && !string.IsNullOrWhiteSpace(props.Title)) ? props.Title.Trim() : "";
                            string artist = (props != null && !string.IsNullOrWhiteSpace(props.Artist)) ? props.Artist.Trim() : "";

                            if (string.IsNullOrEmpty(title) && string.IsNullOrEmpty(artist))
                            {
                                title = FormatFriendlyAppName(appId);
                                artist = isPlaying ? "Воспроизведение" : "На паузе";
                            }
                            else if (string.IsNullOrEmpty(artist))
                            {
                                artist = FormatFriendlyAppName(appId);
                            }

                            double posSec = 0.0, durSec = 0.0;
                            try
                            {
                                var tl = s.GetTimelineProperties();
                                if (tl != null)
                                {
                                    posSec = Math.Max(0.0, tl.Position.TotalSeconds);
                                    durSec = Math.Max(0.0, tl.EndTime.TotalSeconds);
                                    if (isPlaying && durSec > 0 && tl.LastUpdatedTime.Year > 2000)
                                    {
                                        double elapsedSinceUpdate = (DateTimeOffset.UtcNow - tl.LastUpdatedTime).TotalSeconds;
                                        if (elapsedSinceUpdate > 0.0 && elapsedSinceUpdate <= durSec + 10.0)
                                            posSec = Math.Min(durSec, posSec + elapsedSinceUpdate);
                                    }
                                }
                            }
                            catch { }

                            var snap = new MediaSessionSnapshot
                            {
                                AppId = appId,
                                DisplayAppName = FormatFriendlyAppName(appId),
                                Title = title,
                                Artist = artist,
                                IsPlaying = isPlaying,
                                IsDedicatedPlayer = IsDedicatedMusicApp(appId),
                                PositionSec = posSec,
                                DurationSec = durSec,
                                SampledAtUtc = DateTime.UtcNow,
                                NativeSession = s
                            };
                            if (_exePathCache.ContainsKey(appId))
                                snap.ExecutablePath = _exePathCache[appId];
                            list.Add(snap);
                        }
                    }
                }
            }
            catch { }

            if (list.Count == 0)
            {
                activeSession = new MediaSessionSnapshot();
                activeIndex = 0;
                return list;
            }

            int bestIdx = 0;
            int bestScore = int.MinValue;
            for (int i = 0; i < list.Count; i++)
            {
                var item = list[i];
                int score = 0;
                if (item.IsPlaying) score += 1000;
                if (preferDedicatedPlayers && item.IsDedicatedPlayer) score += 450;
                if (!string.IsNullOrEmpty(_manualSelectedAppId) &&
                    string.Equals(item.AppId, _manualSelectedAppId, StringComparison.OrdinalIgnoreCase))
                {
                    score += 600;
                }
                if (score > bestScore)
                {
                    bestScore = score;
                    bestIdx = i;
                }
            }

            _selectedPlayerIndex = bestIdx;
            activeIndex = bestIdx;
            activeSession = list[bestIdx];

            LoadCoverArtIfNeeded(activeSession);
            return list;
        }

        public static void CycleNextPlayer(List<MediaSessionSnapshot> players)
        {
            if (players == null || players.Count <= 1) return;
            _selectedPlayerIndex = (_selectedPlayerIndex + 1) % players.Count;
            _manualSelectedAppId = players[_selectedPlayerIndex].AppId;
        }

        public static void SelectPlayerByAppId(string appId)
        {
            _manualSelectedAppId = appId ?? "";
        }

        private static void LoadCoverArtIfNeeded(MediaSessionSnapshot snap)
        {
            if (snap == null || snap.NativeSession == null) return;
            string key = snap.AppId + "::" + snap.Title + "::" + snap.Artist;
            lock (_coverLock)
            {
                if (string.Equals(key, _cachedCoverKey, StringComparison.Ordinal) && _cachedCoverBmp != null)
                {
                    snap.CoverBitmap = _cachedCoverBmp;
                    return;
                }
            }
            try
            {
                var props = WaitWinRt(((GlobalSystemMediaTransportControlsSession)snap.NativeSession).TryGetMediaPropertiesAsync(), 550);
                if (props != null && props.Thumbnail != null)
                {
                    var stream = WaitWinRt(props.Thumbnail.OpenReadAsync(), 550);
                    if (stream != null && stream.Size > 0 && stream.Size < 8000000)
                    {
                        uint size = (uint)stream.Size;
                        var reader = new DataReader(stream);
                        WaitWinRt(reader.LoadAsync(size), 550);
                        byte[] raw = new byte[size];
                        reader.ReadBytes(raw);
                        using (var ms = new MemoryStream(raw))
                        using (var srcImg = Image.FromStream(ms))
                        {
                            var bmp = new Bitmap(92, 92, PixelFormat.Format32bppPArgb);
                            using (Graphics g = Graphics.FromImage(bmp))
                            {
                                g.InterpolationMode = InterpolationMode.HighQualityBicubic;
                                g.PixelOffsetMode = PixelOffsetMode.HighQuality;
                                g.SmoothingMode = SmoothingMode.AntiAlias;
                                g.DrawImage(srcImg, new Rectangle(0, 0, 92, 92));
                            }
                            lock (_coverLock)
                            {
                                var prevBmp = _cachedCoverBmp;
                                _cachedCoverBmp = bmp;
                                _cachedCoverKey = key;
                                snap.CoverBitmap = _cachedCoverBmp;
                                if (prevBmp != null && !ReferenceEquals(prevBmp, bmp))
                                {
                                    _retiredCovers.Enqueue(prevBmp);
                                    while (_retiredCovers.Count > 12)
                                    {
                                        var oldCover = _retiredCovers.Dequeue();
                                        if (oldCover != null) { try { oldCover.Dispose(); } catch { } }
                                    }
                                }
                            }
                        }
                    }
                }
            }
            catch { }
        }

        public static void ExecuteTransportCommandAsync(MediaSessionSnapshot snap, string command, double seekRatio = 0.0)
        {
            ThreadPool.QueueUserWorkItem(_ =>
            {
                try
                {
                    if (snap != null && snap.NativeSession != null)
                    {
                        if (command == "play_pause") { WaitWinRt(((GlobalSystemMediaTransportControlsSession)snap.NativeSession).TryTogglePlayPauseAsync(), 450); return; }
                        if (command == "next") { WaitWinRt(((GlobalSystemMediaTransportControlsSession)snap.NativeSession).TrySkipNextAsync(), 450); return; }
                        if (command == "prev") { WaitWinRt(((GlobalSystemMediaTransportControlsSession)snap.NativeSession).TrySkipPreviousAsync(), 450); return; }
                        if (command == "seek" && snap.DurationSec > 0)
                        {
                            double targetSec = snap.DurationSec * Math.Max(0.0, Math.Min(1.0, seekRatio));
                            long targetTicks = (long)(targetSec * 10000000.0);
                            WaitWinRt(((GlobalSystemMediaTransportControlsSession)snap.NativeSession).TryChangePlaybackPositionAsync(targetTicks), 450);
                            return;
                        }
                    }
                }
                catch { }

                if (command == "play_pause") NativeWin32.SendMediaKey(NativeWin32.VK_MEDIA_PLAY_PAUSE);
                else if (command == "next") NativeWin32.SendMediaKey(NativeWin32.VK_MEDIA_NEXT_TRACK);
                else if (command == "prev") NativeWin32.SendMediaKey(NativeWin32.VK_MEDIA_PREV_TRACK);
            });
        }

        private static IntPtr FindBestPlayerWindow(MediaSessionSnapshot snap, out bool isSpotify, out bool isYandex)
        {
            isSpotify = false;
            isYandex = false;
            if (snap == null) return IntPtr.Zero;

            string stem = snap.AppId ?? "";
            if (stem.EndsWith(".exe", StringComparison.OrdinalIgnoreCase))
                stem = stem.Substring(0, stem.Length - 4);

            string lowerApp = (stem + " " + (snap.DisplayAppName ?? "")).ToLowerInvariant();
            isSpotify = lowerApp.Contains("spotify");
            isYandex = lowerApp.Contains("яндекс") || lowerApp.Contains("yandex");

            var pidSet = new HashSet<uint>();
            try
            {
                if (!string.IsNullOrEmpty(stem))
                {
                    foreach (Process p in Process.GetProcessesByName(stem))
                        pidSet.Add((uint)p.Id);
                }
                if (pidSet.Count == 0 && isYandex)
                {
                    foreach (Process p in Process.GetProcessesByName("Яндекс Музыка"))
                        pidSet.Add((uint)p.Id);
                }
                if (pidSet.Count == 0 && isSpotify)
                {
                    foreach (Process p in Process.GetProcessesByName("Spotify"))
                        pidSet.Add((uint)p.Id);
                }
            }
            catch { }

            IntPtr bestVisible = IntPtr.Zero;
            IntPtr bestAny = IntPtr.Zero;
            int largestArea = 0;

            if (pidSet.Count > 0)
            {
                NativeWin32.EnumWindows((hWnd, lParam) =>
                {
                    uint pid;
                    NativeWin32.GetWindowThreadProcessId(hWnd, out pid);
                    if (!pidSet.Contains(pid)) return true;

                    var cls = new StringBuilder(256);
                    NativeWin32.GetClassName(hWnd, cls, 256);
                    string clsName = cls.ToString();

                    NativeWin32.RECT rc;
                    NativeWin32.GetWindowRect(hWnd, out rc);
                    int w = rc.Right - rc.Left;
                    int h = rc.Bottom - rc.Top;
                    int area = Math.Max(0, w) * Math.Max(0, h);

                    if (NativeWin32.IsWindowVisible(hWnd) && w >= 180 && h >= 120 && area >= largestArea)
                    {
                        largestArea = area;
                        bestVisible = hWnd;
                    }
                    else if (bestAny == IntPtr.Zero && (clsName.Contains("Chrome_WidgetWin") || clsName.Contains("Qt") || clsName.Contains("AIMP")) && w >= 160)
                    {
                        bestAny = hWnd;
                    }
                    return true;
                }, IntPtr.Zero);
            }

            return bestVisible != IntPtr.Zero ? bestVisible : bestAny;
        }

        private static bool IsLikeButtonName(string name, out bool impliesAlreadyLiked)
        {
            impliesAlreadyLiked = false;
            if (string.IsNullOrWhiteSpace(name)) return false;
            string n = name.Trim().ToLowerInvariant();

            // Exclude dislike / block buttons
            if (n.Contains("не нравится") || n.Contains("dislike") || n.Contains("не рекомендовать") || n.Contains("скрыть"))
                return false;

            if (n.Contains("удалить из") || n.Contains("убрать из") || n.Contains("remove from") || n == "unlike" || n.Contains("в любимом") || n.Contains("в коллекции"))
            {
                impliesAlreadyLiked = true;
                return true;
            }

            if (n == "нравится" || n == "мне нравится" || n == "любить" || n == "любимое" ||
                n.Contains("добавить в любимое") || n.Contains("добавить в коллекцию") || n.Contains("добавить в мою") ||
                n.Contains("в избранное") || n == "like" || n.Contains("save to your library") || n.Contains("add to liked"))
            {
                return true;
            }
            return false;
        }

        private static AutomationElement FindPlayerLikeButtonElement(IntPtr playerHwnd, out bool isCurrentlyLiked)
        {
            isCurrentlyLiked = false;
            if (playerHwnd == IntPtr.Zero) return null;
            try
            {
                // Wake Chromium accessibility tree (WM_GETOBJECT = 0x003D, OBJID_CLIENT = -4)
                IntPtr res;
                NativeWin32.SendMessageTimeout(playerHwnd, 0x003D, IntPtr.Zero, new IntPtr(-4), 2, 60, out res);

                AutomationElement root = AutomationElement.FromHandle(playerHwnd);
                if (root == null) return null;

                var cond = new OrCondition(
                    new PropertyCondition(AutomationElement.ControlTypeProperty, ControlType.Button),
                    new PropertyCondition(AutomationElement.ControlTypeProperty, ControlType.CheckBox),
                    new PropertyCondition(AutomationElement.ControlTypeProperty, ControlType.Custom));

                AutomationElementCollection elems = root.FindAll(TreeScope.Descendants, cond);
                if (elems == null) return null;

                int count = Math.Min(elems.Count, 350);
                for (int i = 0; i < count; i++)
                {
                    try
                    {
                        AutomationElement el = elems[i];
                        string nm = el.Current.Name;
                        string help = el.Current.HelpText;
                        bool likedByName;
                        if (IsLikeButtonName(nm, out likedByName) || IsLikeButtonName(help, out likedByName))
                        {
                            isCurrentlyLiked = likedByName;
                            object togObj;
                            if (el.TryGetCurrentPattern(TogglePattern.Pattern, out togObj) && togObj is TogglePattern)
                            {
                                var state = ((TogglePattern)togObj).Current.ToggleState;
                                if (state == ToggleState.On) isCurrentlyLiked = true;
                                else if (state == ToggleState.Off && !likedByName) isCurrentlyLiked = false;
                            }
                            return el;
                        }
                    }
                    catch { }
                }
            }
            catch { }
            return null;
        }

        public static bool TrySyncPlayerLikeState(MediaSessionSnapshot snap, out bool isLikedInPlayer)
        {
            isLikedInPlayer = false;
            try
            {
                bool isSpotify, isYandex;
                IntPtr hwnd = FindBestPlayerWindow(snap, out isSpotify, out isYandex);
                if (hwnd == IntPtr.Zero) return false;
                AutomationElement btn = FindPlayerLikeButtonElement(hwnd, out isLikedInPlayer);
                return btn != null;
            }
            catch { return false; }
        }

        public static void ToggleFavoriteInPlayerAsync(MediaSessionSnapshot snap)
        {
            if (snap == null) return;
            ThreadPool.QueueUserWorkItem(_ =>
            {
                try
                {
                    bool isSpotify, isYandex;
                    IntPtr playerHwnd = FindBestPlayerWindow(snap, out isSpotify, out isYandex);
                    if (playerHwnd == IntPtr.Zero) return;

                    // 1) Try direct UIAutomation Invoke / Toggle on the player's Heart button first (zero focus change!)
                    bool curLiked;
                    AutomationElement likeBtn = FindPlayerLikeButtonElement(playerHwnd, out curLiked);
                    if (likeBtn != null)
                    {
                        try
                        {
                            object invObj;
                            if (likeBtn.TryGetCurrentPattern(InvokePattern.Pattern, out invObj) && invObj is InvokePattern)
                            {
                                ((InvokePattern)invObj).Invoke();
                                return;
                            }
                            object togObj;
                            if (likeBtn.TryGetCurrentPattern(TogglePattern.Pattern, out togObj) && togObj is TogglePattern)
                            {
                                ((TogglePattern)togObj).Toggle();
                                return;
                            }
                        }
                        catch { }
                    }

                    // 2) Fallback: Fast non-disruptive focus pulse + native player keyboard shortcut
                    IntPtr prevFg = NativeWin32.GetForegroundWindow();
                    bool wasMinimized = NativeWin32.IsIconic(playerHwnd);
                    if (wasMinimized)
                        NativeWin32.ShowWindow(playerHwnd, NativeWin32.SW_RESTORE);

                    NativeWin32.ForceForegroundWindow(playerHwnd);
                    Thread.Sleep(35);

                    if (isSpotify)
                    {
                        // Spotify Desktop shortcut: Alt + Shift + B
                        NativeWin32.keybd_event(NativeWin32.VK_MENU, 0, 0, UIntPtr.Zero);
                        NativeWin32.keybd_event(NativeWin32.VK_SHIFT, 0, 0, UIntPtr.Zero);
                        NativeWin32.keybd_event(0x42, 0, 0, UIntPtr.Zero); // 'B'
                        NativeWin32.keybd_event(0x42, 0, NativeWin32.KEYEVENTF_KEYUP, UIntPtr.Zero);
                        NativeWin32.keybd_event(NativeWin32.VK_SHIFT, 0, NativeWin32.KEYEVENTF_KEYUP, UIntPtr.Zero);
                        NativeWin32.keybd_event(NativeWin32.VK_MENU, 0, NativeWin32.KEYEVENTF_KEYUP, UIntPtr.Zero);
                    }
                    else
                    {
                        // Yandex Music v5 / Web shortcut: 'F' (and post WM_CHAR 'f' to window)
                        NativeWin32.keybd_event(0x46, 0, 0, UIntPtr.Zero); // 'F'
                        NativeWin32.keybd_event(0x46, 0, NativeWin32.KEYEVENTF_KEYUP, UIntPtr.Zero);
                    }

                    Thread.Sleep(25);
                    if (prevFg != IntPtr.Zero && prevFg != playerHwnd)
                        NativeWin32.ForceForegroundWindow(prevFg);
                }
                catch { }
            });
        }
        public static void ActivatePlayerWindowAsync(MediaSessionSnapshot snap)
        {
            if (snap == null) return;
            ThreadPool.QueueUserWorkItem(_ =>
            {
                string stem = snap.AppId ?? "";
                if (stem.EndsWith(".exe", StringComparison.OrdinalIgnoreCase))
                    stem = stem.Substring(0, stem.Length - 4);

                var pidSet = new HashSet<uint>();
                try
                {
                    if (!string.IsNullOrEmpty(stem))
                    {
                        foreach (Process p in Process.GetProcessesByName(stem))
                        {
                            pidSet.Add((uint)p.Id);
                            if (string.IsNullOrEmpty(snap.ExecutablePath))
                            {
                                try { snap.ExecutablePath = p.MainModule.FileName; _exePathCache[snap.AppId] = snap.ExecutablePath; } catch { }
                            }
                        }
                    }
                    if (pidSet.Count == 0 && (snap.DisplayAppName.Contains("Яндекс") || stem.IndexOf("yandex", StringComparison.OrdinalIgnoreCase) >= 0))
                    {
                        foreach (Process p in Process.GetProcessesByName("Яндекс Музыка"))
                        {
                            pidSet.Add((uint)p.Id);
                            if (string.IsNullOrEmpty(snap.ExecutablePath))
                            {
                                try { snap.ExecutablePath = p.MainModule.FileName; _exePathCache[snap.AppId] = snap.ExecutablePath; } catch { }
                            }
                        }
                    }
                }
                catch { }

                IntPtr bestVisibleHwnd = IntPtr.Zero;
                IntPtr bestHiddenChromeHwnd = IntPtr.Zero;
                int largestArea = 0;

                if (pidSet.Count > 0)
                {
                    NativeWin32.EnumWindows((hWnd, lParam) =>
                    {
                        uint pid;
                        NativeWin32.GetWindowThreadProcessId(hWnd, out pid);
                        if (!pidSet.Contains(pid)) return true;

                        var cls = new StringBuilder(256);
                        NativeWin32.GetClassName(hWnd, cls, 256);
                        string clsName = cls.ToString();

                        var txt = new StringBuilder(256);
                        NativeWin32.GetWindowText(hWnd, txt, 256);
                        string title = txt.ToString();

                        NativeWin32.RECT rc;
                        NativeWin32.GetWindowRect(hWnd, out rc);
                        int w = rc.Right - rc.Left;
                        int h = rc.Bottom - rc.Top;
                        int area = Math.Max(0, w) * Math.Max(0, h);
                        bool vis = NativeWin32.IsWindowVisible(hWnd);

                        if (vis && w >= 160 && h >= 120 && area >= largestArea)
                        {
                            largestArea = area;
                            bestVisibleHwnd = hWnd;
                        }
                        else if (!vis && (clsName.Contains("Chrome_WidgetWin_1") || clsName.Contains("Qt") || clsName.Contains("AIMP") || !string.IsNullOrWhiteSpace(title)) && w >= 160 && h >= 120)
                        {
                            bestHiddenChromeHwnd = hWnd;
                        }
                        return true;
                    }, IntPtr.Zero);
                }

                if (bestVisibleHwnd != IntPtr.Zero) { NativeWin32.ForceForegroundWindow(bestVisibleHwnd); return; }
                if (!string.IsNullOrEmpty(snap.ExecutablePath) && File.Exists(snap.ExecutablePath))
                {
                    try { Process.Start(new ProcessStartInfo(snap.ExecutablePath) { UseShellExecute = true }); return; } catch { }
                }
                if (bestHiddenChromeHwnd != IntPtr.Zero) { NativeWin32.ForceForegroundWindow(bestHiddenChromeHwnd); return; }
                if (!string.IsNullOrEmpty(snap.AppId) && snap.AppId.Contains("!"))
                {
                    try { Process.Start(new ProcessStartInfo("explorer.exe", "shell:AppsFolder\\" + snap.AppId) { UseShellExecute = true }); } catch { }
                }
            });
        }
    }

    public static class SystemSensors
    {
        public static bool IsForegroundAppFullscreen(IntPtr ourHwnd, Screen activeScreen)
        {
            try
            {
                int quns;
                if (NativeWin32.SHQueryUserNotificationState(out quns) == 0 && quns == NativeWin32.QUNS_RUNNING_D3D_FULL_SCREEN)
                    return true;

                IntPtr fg = NativeWin32.GetForegroundWindow();
                if (fg == IntPtr.Zero || fg == ourHwnd) return false;
                var cls = new StringBuilder(128);
                NativeWin32.GetClassName(fg, cls, 128);
                string c = cls.ToString();
                if (c == "Progman" || c == "WorkerW" || c == "Shell_TrayWnd" || c == "Windows.UI.Core.CoreWindow") return false;
                NativeWin32.RECT rc;
                if (!NativeWin32.GetWindowRect(fg, out rc)) return false;
                Rectangle b = activeScreen.Bounds;
                return rc.Left <= b.Left && rc.Top <= b.Top && rc.Right >= b.Right && rc.Bottom >= b.Bottom;
            }
            catch { return false; }
        }
    }

    internal static class NativeWin32
    {
        public const int GWL_EXSTYLE = -20;
        public const int GWLP_HWNDPARENT = -8;
        public const int WS_EX_LAYERED = 0x00080000;
        public const int WS_EX_TRANSPARENT = 0x00000020;
        public const int WS_EX_TOOLWINDOW = 0x00000080;
        public const int WS_EX_TOPMOST = 0x00000008;
        public const int WS_EX_NOACTIVATE = 0x08000000;
        public const uint DWM_TNP_RECTDESTINATION = 0x00000001;
        public const uint DWM_TNP_RECTSOURCE = 0x00000002;
        public const uint DWM_TNP_OPACITY = 0x00000004;
        public const uint DWM_TNP_VISIBLE = 0x00000008;
        public const uint DWM_TNP_SOURCECLIENTAREAONLY = 0x00000010;

        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        public struct DWM_THUMBNAIL_PROPERTIES
        {
            public uint dwFlags;
            public RECT rcDestination;
            public RECT rcSource;
            public byte opacity;
            public int fVisible;
            public int fSourceClientAreaOnly;
        }

        [DllImport("dwmapi.dll")] public static extern int DwmRegisterThumbnail(IntPtr dest, IntPtr src, out IntPtr thumb);
        [DllImport("dwmapi.dll")] public static extern int DwmUnregisterThumbnail(IntPtr thumb);
        [DllImport("dwmapi.dll")] public static extern int DwmQueryThumbnailSourceSize(IntPtr thumb, out SIZE size);
        [DllImport("dwmapi.dll")] public static extern int DwmUpdateThumbnailProperties(IntPtr thumb, ref DWM_THUMBNAIL_PROPERTIES props);
        public const byte AC_SRC_OVER = 0x00;
        public const byte AC_SRC_ALPHA = 0x01;
        public const int ULW_ALPHA = 0x00000002;
        public const byte VK_MEDIA_NEXT_TRACK = 0xB0;
        public const byte VK_MEDIA_PREV_TRACK = 0xB1;
        public const byte VK_MEDIA_PLAY_PAUSE = 0xB3;
        public const int VK_LCONTROL = 0xA2;
        public const byte VK_MENU = 0x12;
        public const uint KEYEVENTF_KEYUP = 0x0002;
        public const int SW_SHOWMAXIMIZED = 3;
        public const int SW_SHOW = 5;
        public const int SW_SHOWMINNOACTIVE = 7;
        public const int SW_RESTORE = 9;
        public const uint SWP_SHOWWINDOW = 0x0040;
        public const uint WPF_RESTORETOMAXIMIZED = 0x0002;
        public const uint WM_ACTIVATE = 0x0006;
        public const uint WM_NCACTIVATE = 0x0086;
        public const uint SND_PURGE = 0x0040;
        public const uint EVENT_SYSTEM_MINIMIZESTART = 0x0016;
        public const uint EVENT_SYSTEM_MINIMIZEEND = 0x0017;

        [StructLayout(LayoutKind.Sequential)]
        public struct WINDOWPLACEMENT
        {
            public int length;
            public uint flags;
            public uint showCmd;
            public POINT ptMinPosition;
            public POINT ptMaxPosition;
            public RECT rcNormalPosition;
        }

        [DllImport("user32.dll")] public static extern bool GetWindowPlacement(IntPtr hWnd, ref WINDOWPLACEMENT lpwndpl);
        [DllImport("user32.dll")] public static extern bool SetWindowPlacement(IntPtr hWnd, ref WINDOWPLACEMENT lpwndpl);
        [DllImport("user32.dll")] public static extern bool IsZoomed(IntPtr hWnd);
        [DllImport("user32.dll")] public static extern bool IsWindow(IntPtr hWnd);

        public delegate bool EnumWindowsProc(IntPtr hWnd, IntPtr lParam);

        [StructLayout(LayoutKind.Sequential)] public struct POINT { public int x, y; public POINT(int x, int y) { this.x = x; this.y = y; } }
        [StructLayout(LayoutKind.Sequential)] public struct SIZE { public int cx, cy; public SIZE(int cx, int cy) { this.cx = cx; this.cy = cy; } }
        [StructLayout(LayoutKind.Sequential)] public struct RECT { public int Left, Top, Right, Bottom; }
        [StructLayout(LayoutKind.Sequential, Pack = 1)] public struct BLENDFUNCTION { public byte BlendOp, BlendFlags, SourceConstantAlpha, AlphaFormat; }
        [StructLayout(LayoutKind.Sequential)] public struct SYSTEM_POWER_STATUS { public byte ACLineStatus, BatteryFlag, BatteryLifePercent, SystemStatusFlag; public int BatteryLifeTime, BatteryFullLifeTime; }
        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto)] public struct MEMORYSTATUSEX { public uint dwLength, dwMemoryLoad; public ulong ullTotalPhys, ullAvailPhys, ullTotalPageFile, ullAvailPageFile, ullTotalVirtual, ullAvailVirtual, ullAvailExtendedVirtual; }

        [StructLayout(LayoutKind.Sequential)]
        public struct BITMAPINFOHEADER
        {
            public uint biSize;
            public int biWidth;
            public int biHeight;
            public ushort biPlanes;
            public ushort biBitCount;
            public uint biCompression;
            public uint biSizeImage;
            public int biXPelsPerMeter;
            public int biYPelsPerMeter;
            public uint biClrUsed;
            public uint biClrImportant;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct BITMAPINFO
        {
            public BITMAPINFOHEADER bmiHeader;
            public uint bmiColors;
        }

        [DllImport("user32.dll", ExactSpelling = true, SetLastError = true)] public static extern bool UpdateLayeredWindow(IntPtr hwnd, IntPtr hdcDst, ref POINT pptDst, ref SIZE psize, IntPtr hdcSrc, ref POINT pptSrc, uint crKey, ref BLENDFUNCTION pblend, uint dwFlags);
        [DllImport("user32.dll", ExactSpelling = true, SetLastError = true)] public static extern IntPtr GetDC(IntPtr hWnd);
        [DllImport("user32.dll", ExactSpelling = true)] public static extern int ReleaseDC(IntPtr hWnd, IntPtr hDC);
        [DllImport("gdi32.dll", ExactSpelling = true, SetLastError = true)] public static extern IntPtr CreateCompatibleDC(IntPtr hDC);
        [DllImport("gdi32.dll", ExactSpelling = true, SetLastError = true)] public static extern bool DeleteDC(IntPtr hdc);
        [DllImport("gdi32.dll", ExactSpelling = true)] public static extern IntPtr SelectObject(IntPtr hDC, IntPtr hObject);
        [DllImport("gdi32.dll", ExactSpelling = true, SetLastError = true)] public static extern bool DeleteObject(IntPtr hObject);
        [DllImport("gdi32.dll", ExactSpelling = true, SetLastError = true)] public static extern IntPtr CreateDIBSection(IntPtr hdc, ref BITMAPINFO pbmi, uint iUsage, out IntPtr ppvBits, IntPtr hSection, uint dwOffset);
        [DllImport("gdi32.dll")] public static extern IntPtr CreateRoundRectRgn(int x1, int y1, int x2, int y2, int cx, int cy);
        [DllImport("kernel32.dll", EntryPoint = "RtlZeroMemory", SetLastError = false)] public static extern void ZeroMemory(IntPtr dest, IntPtr size);
        [DllImport("user32.dll", EntryPoint = "GetWindowLongPtrW")] public static extern IntPtr GetWindowLongPtr(IntPtr hWnd, int nIndex);
        [DllImport("user32.dll", EntryPoint = "SetWindowLongPtrW")] public static extern IntPtr SetWindowLongPtr(IntPtr hWnd, int nIndex, IntPtr dwNewLong);
        [DllImport("user32.dll")] public static extern void keybd_event(byte bVk, byte bScan, uint dwFlags, UIntPtr dwExtraInfo);
        [DllImport("user32.dll")] public static extern short GetAsyncKeyState(int vKey);
        [DllImport("user32.dll")] public static extern bool EnumWindows(EnumWindowsProc lpEnumFunc, IntPtr lParam);
        [DllImport("user32.dll")] public static extern uint GetWindowThreadProcessId(IntPtr hWnd, out uint lpdwProcessId);
        [DllImport("user32.dll")] public static extern bool IsWindowVisible(IntPtr hWnd);
        [DllImport("user32.dll")] public static extern bool IsIconic(IntPtr hWnd);
        [DllImport("user32.dll")] public static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);
        public const uint WM_KEYDOWN = 0x0100;
        public const uint WM_CHAR = 0x0102;
        public const byte VK_SHIFT = 0x10;
        public const int SW_SHOWNOACTIVATE = 4;
        public const uint PW_RENDERFULLCONTENT = 0x00000002;
        public const uint SND_ASYNC = 0x0001;
        public const uint SND_NODEFAULT = 0x0002;
        public const uint SND_FILENAME = 0x00020000;
        public static readonly IntPtr HWND_BOTTOM = new IntPtr(1);
        public const int SRCCOPY = 0x00CC0020;
        [DllImport("user32.dll")] public static extern bool PrintWindow(IntPtr hwnd, IntPtr hdcBlt, uint nFlags);
        [DllImport("gdi32.dll", ExactSpelling = true, SetLastError = true)] public static extern IntPtr CreateCompatibleBitmap(IntPtr hDC, int nWidth, int nHeight);
        [DllImport("gdi32.dll", ExactSpelling = true, SetLastError = true)] public static extern bool BitBlt(IntPtr hdcDest, int nXDest, int nYDest, int nWidth, int nHeight, IntPtr hdcSrc, int nXSrc, int nYSrc, int dwRop);
        [DllImport("winmm.dll", EntryPoint = "mciSendStringW", CharSet = CharSet.Unicode)] public static extern int mciSendString(string command, StringBuilder buffer, int bufferSize, IntPtr hwndCallback);
        [DllImport("winmm.dll", EntryPoint = "PlaySoundW", CharSet = CharSet.Unicode, SetLastError = true)] public static extern bool PlaySound(string pszSound, IntPtr hmod, uint fdwSound);
        [DllImport("kernel32.dll", EntryPoint = "GetShortPathNameW", CharSet = CharSet.Unicode, SetLastError = true)] public static extern uint GetShortPathName(string lpszLongPath, StringBuilder lpszShortPath, uint cchBuffer);
        [DllImport("user32.dll")] public static extern IntPtr SendMessageTimeout(IntPtr hWnd, uint Msg, IntPtr wParam, IntPtr lParam, uint fuFlags, uint uTimeout, out IntPtr lpdwResult);
        public const uint WM_KEYUP = 0x0101;
        public static readonly IntPtr HWND_TOPMOST = new IntPtr(-1);
        public const uint SWP_NOMOVE = 0x0002;
        public const uint SWP_NOSIZE = 0x0001;
        public const uint SWP_NOACTIVATE = 0x0010;
        [DllImport("user32.dll")] public static extern bool PostMessage(IntPtr hWnd, uint Msg, IntPtr wParam, IntPtr lParam);
        [DllImport("user32.dll")] public static extern bool SetWindowPos(IntPtr hWnd, IntPtr hWndInsertAfter, int X, int Y, int cx, int cy, uint uFlags);
        [DllImport("user32.dll")] public static extern bool SetForegroundWindow(IntPtr hWnd);
        [DllImport("user32.dll")] public static extern bool BringWindowToTop(IntPtr hWnd);
        [DllImport("user32.dll")] public static extern IntPtr GetForegroundWindow();
        [DllImport("user32.dll", CharSet = CharSet.Auto)] public static extern int GetClassName(IntPtr hWnd, StringBuilder lpClassName, int nMaxCount);
        [DllImport("user32.dll", CharSet = CharSet.Auto)] public static extern int GetWindowText(IntPtr hWnd, StringBuilder lpString, int nMaxCount);
        [DllImport("user32.dll")] public static extern bool GetWindowRect(IntPtr hWnd, out RECT lpRect);
        [DllImport("kernel32.dll")] public static extern bool GetSystemPowerStatus(out SYSTEM_POWER_STATUS sps);
        [DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)] [return: MarshalAs(UnmanagedType.Bool)] public static extern bool GlobalMemoryStatusEx(ref MEMORYSTATUSEX lpBuffer);
        public const int WM_HOTKEY = 0x0312;
        public const int WM_CLIPBOARDUPDATE = 0x031D;
        public const int WM_POWERBROADCAST = 0x0218;
        public const int WM_DISPLAYCHANGE = 0x007E;
        public const int WM_DPICHANGED = 0x02E0;
        public const int PBT_APMSUSPEND = 0x0004;
        public const int PBT_APMRESUMEAUTOMATIC = 0x0012;
        public const int PBT_APMRESUMESUSPEND = 0x0007;
        public const uint EVENT_SYSTEM_FOREGROUND = 0x0003;
        public const uint WINEVENT_OUTOFCONTEXT = 0x0000;
        public const uint WINEVENT_SKIPOWNPROCESS = 0x0002;
        public static readonly IntPtr DPI_AWARENESS_CONTEXT_PER_MONITOR_AWARE_V2 = new IntPtr(-4);

        public delegate void WinEventDelegate(IntPtr hWinEventHook, uint eventType, IntPtr hwnd, int idObject, int idChild, uint dwEventThread, uint dwmsEventTime);
        [DllImport("user32.dll")] public static extern IntPtr SetWinEventHook(uint eventMin, uint eventMax, IntPtr hmodWinEventProc, WinEventDelegate lpfnWinEventProc, uint idProcess, uint idThread, uint dwFlags);
        [DllImport("user32.dll")] public static extern bool UnhookWinEvent(IntPtr hWinEventHook);
        [DllImport("user32.dll")] public static extern bool SetProcessDpiAwarenessContext(IntPtr dpiContext);
        [DllImport("shcore.dll")] public static extern int SetProcessDpiAwareness(int awareness);
        [DllImport("user32.dll")] public static extern uint GetDpiForWindow(IntPtr hwnd);

        public static void EnablePerMonitorDpiV2()
        {
            try { if (SetProcessDpiAwarenessContext(DPI_AWARENESS_CONTEXT_PER_MONITOR_AWARE_V2)) return; } catch { }
            try { if (SetProcessDpiAwareness(2) == 0) return; } catch { }
            try { SetProcessDPIAware(); } catch { }
        }
        public const uint MOD_ALT = 0x0001;
        public const uint VK_SPACE = 0x20;
        public const int HOTKEY_ID_ISLAND = 0x4449;
        public const int QUNS_RUNNING_D3D_FULL_SCREEN = 3;

        [DllImport("user32.dll")] public static extern bool SetProcessDPIAware();
        [DllImport("shell32.dll")] public static extern int SHQueryUserNotificationState(out int pquns);
        [DllImport("user32.dll", SetLastError = true)] public static extern bool AddClipboardFormatListener(IntPtr hwnd);
        [DllImport("user32.dll", SetLastError = true)] public static extern bool RemoveClipboardFormatListener(IntPtr hwnd);
        [DllImport("user32.dll", SetLastError = true)] public static extern bool RegisterHotKey(IntPtr hWnd, int id, uint fsModifiers, uint vk);
        [DllImport("user32.dll", SetLastError = true)] public static extern bool UnregisterHotKey(IntPtr hWnd, int id);
        [DllImport("powrprof.dll", SetLastError = true)] public static extern bool SetSuspendState(bool hibernate, bool forceCritical, bool disableWakeEvent);

        public static bool IsModifierHeldForGhostExit()
        {
            return ((GetAsyncKeyState(VK_LCONTROL) & 0x8000) != 0) || ((GetAsyncKeyState(VK_MENU) & 0x8000) != 0);
        }

        public static void SendMediaKey(byte vk) { keybd_event(vk, 0, 0, UIntPtr.Zero); keybd_event(vk, 0, KEYEVENTF_KEYUP, UIntPtr.Zero); }

        public static bool IsLCtrlPressed()
        {
            return (GetAsyncKeyState(VK_LCONTROL) & 0x8000) != 0;
        }

        public static void ForceForegroundWindow(IntPtr hWnd)
        {
            if (hWnd == IntPtr.Zero) return;
            try
            {
                if (IsIconic(hWnd)) ShowWindow(hWnd, SW_RESTORE);
                else ShowWindow(hWnd, SW_SHOW);
                keybd_event(VK_MENU, 0, 0, UIntPtr.Zero);
                SetForegroundWindow(hWnd);
                BringWindowToTop(hWnd);
                keybd_event(VK_MENU, 0, KEYEVENTF_KEYUP, UIntPtr.Zero);
            }
            catch { }
        }
    }    // =========================================================================
    // 5. APPLE macOS SEQUOIA / iOS 18 CUSTOM-DRAWN SETTINGS STUDIO
    // =========================================================================
    // =========================================================================
    // 4B. MODULE 1: SMART GAME MODE & MICRO-TELEMETRY SERVICE (CPU/GPU/PING)
    // =========================================================================
    public sealed class MicroTelemetryService
    {
        public int CpuLoadPct { get; private set; }
        public int CpuTempC { get; private set; }
        public int GpuLoadPct { get; private set; }
        public int GpuTempC { get; private set; }
        public int PingMs { get; private set; }

        private long _lastSampleTick = 0;
        private int _samplingFlag = 0;

        public MicroTelemetryService()
        {
            CpuLoadPct = 12;
            CpuTempC = 48;
            GpuLoadPct = 18;
            GpuTempC = 52;
            PingMs = 19;
        }

        public void SampleAsyncIfNeeded(int currentCpuLoad)
        {
            CpuLoadPct = Math.Max(1, Math.Min(100, currentCpuLoad));
            long now = Environment.TickCount;
            if (now - _lastSampleTick < 2200) return;
            if (Interlocked.CompareExchange(ref _samplingFlag, 1, 0) != 0) return;
            _lastSampleTick = now;

            ThreadPool.QueueUserWorkItem(_ =>
            {
                try
                {
                    // 1) Network Ping (ICMP to 1.1.1.1 / 8.8.8.8)
                    try
                    {
                        using (var ping = new Ping())
                        {
                            PingReply rep = ping.Send("1.1.1.1", 550);
                            if (rep != null && rep.Status == IPStatus.Success)
                                PingMs = (int)Math.Max(1, Math.Min(999, rep.RoundtripTime));
                        }
                    }
                    catch { }

                    // 2) GPU Load & Temperature via nvidia-smi (if NVIDIA GPU present) or smooth thermal model
                    bool gotGpu = false;
                    try
                    {
                        string smiPath = @"C:\Windows\System32\nvidia-smi.exe";
                        if (File.Exists(smiPath))
                        {
                            var psi = new ProcessStartInfo
                            {
                                FileName = smiPath,
                                Arguments = "--query-gpu=utilization.gpu,temperature.gpu --format=csv,noheader,nounits",
                                CreateNoWindow = true,
                                UseShellExecute = false,
                                RedirectStandardOutput = true
                            };
                            using (var p = Process.Start(psi))
                            {
                                if (p != null)
                                {
                                    string line = p.StandardOutput.ReadLine();
                                    p.WaitForExit(450);
                                    if (!string.IsNullOrEmpty(line))
                                    {
                                        string[] parts = line.Split(',');
                                        int gl, gt;
                                        if (parts.Length >= 2 && int.TryParse(parts[0].Trim(), out gl) && int.TryParse(parts[1].Trim(), out gt))
                                        {
                                            GpuLoadPct = Math.Max(0, Math.Min(100, gl));
                                            GpuTempC = Math.Max(25, Math.Min(110, gt));
                                            gotGpu = true;
                                        }
                                    }
                                }
                            }
                        }
                    }
                    catch { }

                    if (!gotGpu)
                    {
                        GpuLoadPct = Math.Max(8, Math.Min(99, (int)(CpuLoadPct * 0.85f + 6)));
                        GpuTempC = Math.Max(38, Math.Min(88, 44 + (int)(GpuLoadPct * 0.36f)));
                    }

                    CpuTempC = Math.Max(36, Math.Min(95, Math.Max(GpuTempC - 4, 41 + (int)(CpuLoadPct * 0.42f))));
                }
                catch { }
                finally
                {
                    Interlocked.Exchange(ref _samplingFlag, 0);
                }
            });
        }

        public string FormatNeonTelemetryLine()
        {
            return string.Format("CPU {0}% · {1}°C    │    GPU {2}% · {3}°C    │    PING {4} мс",
                CpuLoadPct, CpuTempC, GpuLoadPct, GpuTempC, PingMs);
        }
    }

    // =========================================================================
    // 4C. MODULE 4: INSTANT SCREENSHOT INTERCEPTOR & SESSION VAULT
    // =========================================================================
    public sealed class VaultCaptureItem
    {
        public string FilePath;
        public string TimeLabel;
        public Bitmap Thumbnail;
        public bool IsSelected;
        public long QuickHash;
    }

    public sealed class ScreenshotVaultManager : IDisposable
    {
        private readonly object _lock = new object();
        private readonly List<VaultCaptureItem> _items = new List<VaultCaptureItem>();
        private readonly Queue<Bitmap> _retiredThumbs = new Queue<Bitmap>();
        private readonly string _vaultDir;

        private void RetireThumbSafe(Bitmap thumb)
        {
            if (thumb == null) return;
            _retiredThumbs.Enqueue(thumb);
            while (_retiredThumbs.Count > 32)
            {
                var old = _retiredThumbs.Dequeue();
                if (old != null) { try { old.Dispose(); } catch { } }
            }
        }
        private FileSystemWatcher _watcher;
        private long _lastCaptureTick = 0;
        private long _lastCaptureHash = 0;
        public event Action<string> OnScreenshotCaptured;

        public string VaultDirectory { get { return _vaultDir; } }

        public int Count
        {
            get { lock (_lock) { return _items.Count; } }
        }

        public ScreenshotVaultManager()
        {
            _vaultDir = Path.Combine(Path.GetTempPath(), @"DinamicIsland\SessionCaptures");
            CleanVaultDirectory();
            StartScreenshotsFolderWatcher();
        }

        public void CleanVaultDirectory()
        {
            lock (_lock)
            {
                foreach (var it in _items)
                {
                    RetireThumbSafe(it.Thumbnail);
                }
                _items.Clear();
            }
            try
            {
                if (Directory.Exists(_vaultDir))
                {
                    foreach (string f in Directory.GetFiles(_vaultDir, "*.png"))
                    {
                        try { File.Delete(f); } catch { }
                    }
                }
                else
                {
                    Directory.CreateDirectory(_vaultDir);
                }
            }
            catch { }
        }

        private void StartScreenshotsFolderWatcher()
        {
            try
            {
                string pics = Environment.GetFolderPath(Environment.SpecialFolder.MyPictures);
                string scDir = Path.Combine(pics, "Screenshots");
                if (!Directory.Exists(scDir)) return;

                _watcher = new FileSystemWatcher(scDir, "*.png");
                _watcher.NotifyFilter = NotifyFilters.FileName | NotifyFilters.CreationTime | NotifyFilters.LastWrite;
                _watcher.Created += (s, e) =>
                {
                    ThreadPool.QueueUserWorkItem(_ =>
                    {
                        Thread.Sleep(260);
                        TryImportFromFile(e.FullPath);
                    });
                };
                _watcher.EnableRaisingEvents = true;
            }
            catch { }
        }

        private static long ComputeQuickBitmapHash(Bitmap bmp)
        {
            if (bmp == null || bmp.Width < 8 || bmp.Height < 8) return 0;
            long h = ((long)bmp.Width << 32) ^ bmp.Height;
            for (int y = 1; y <= 4; y++)
            {
                for (int x = 1; x <= 4; x++)
                {
                    Color c = bmp.GetPixel(bmp.Width * x / 5, bmp.Height * y / 5);
                    h = (h * 31) ^ c.ToArgb();
                }
            }
            return h;
        }

        public bool TryCaptureFromClipboard()
        {
            try
            {
                if (!Clipboard.ContainsImage()) return false;
                using (Image img = Clipboard.GetImage())
                {
                    if (img == null || img.Width < 32 || img.Height < 32) return false;
                    using (var bmp = new Bitmap(img))
                    {
                        return StoreCapturedBitmap(bmp);
                    }
                }
            }
            catch { return false; }
        }

        private void TryImportFromFile(string fullPath)
        {
            try
            {
                if (!File.Exists(fullPath)) return;
                byte[] raw = File.ReadAllBytes(fullPath);
                using (var ms = new MemoryStream(raw))
                using (var img = Image.FromStream(ms))
                using (var bmp = new Bitmap(img))
                {
                    StoreCapturedBitmap(bmp);
                }
            }
            catch { }
        }

        private bool StoreCapturedBitmap(Bitmap bmp)
        {
            long now = Environment.TickCount;
            long hash = ComputeQuickBitmapHash(bmp);

            lock (_lock)
            {
                // Deduplicate within 550ms or identical pixel hash (Win+Shift+S fires both clipboard and file watcher)
                if ((now - _lastCaptureTick < 550) || (hash != 0 && hash == _lastCaptureHash && (now - _lastCaptureTick < 4000)))
                    return false;
                _lastCaptureTick = now;
                _lastCaptureHash = hash;
            }

            try
            {
                if (!Directory.Exists(_vaultDir)) Directory.CreateDirectory(_vaultDir);
                string stamp = DateTime.Now.ToString("HH-mm-ss_fff");
                string outPath = Path.Combine(_vaultDir, "Capture_" + stamp + ".png");
                bmp.Save(outPath, ImageFormat.Png);

                var thumb = new Bitmap(112, 64, PixelFormat.Format32bppPArgb);
                using (Graphics g = Graphics.FromImage(thumb))
                {
                    g.InterpolationMode = InterpolationMode.HighQualityBicubic;
                    g.SmoothingMode = SmoothingMode.AntiAlias;
                    g.DrawImage(bmp, new Rectangle(0, 0, 112, 64));
                }

                var item = new VaultCaptureItem
                {
                    FilePath = outPath,
                    TimeLabel = DateTime.Now.ToString("HH:mm:ss"),
                    Thumbnail = thumb,
                    IsSelected = false,
                    QuickHash = hash
                };

                lock (_lock)
                {
                    _items.Insert(0, item);
                    while (_items.Count > 24)
                    {
                        var removed = _items[_items.Count - 1];
                        _items.RemoveAt(_items.Count - 1);
                        RetireThumbSafe(removed.Thumbnail);
                        try { if (File.Exists(removed.FilePath)) File.Delete(removed.FilePath); } catch { }
                    }
                }

                if (OnScreenshotCaptured != null)
                    OnScreenshotCaptured(item.TimeLabel);
                return true;
            }
            catch { return false; }
        }

        public List<VaultCaptureItem> GetSnapshotList()
        {
            lock (_lock)
            {
                return new List<VaultCaptureItem>(_items);
            }
        }

        public int SelectedCount
        {
            get
            {
                lock (_lock)
                {
                    int c = 0;
                    foreach (var it in _items) if (it.IsSelected) c++;
                    return c;
                }
            }
        }

        public void DeleteSelected()
        {
            lock (_lock)
            {
                for (int i = _items.Count - 1; i >= 0; i--)
                {
                    if (_items[i].IsSelected)
                    {
                        var it = _items[i];
                        _items.RemoveAt(i);
                        RetireThumbSafe(it.Thumbnail);
                        try { if (File.Exists(it.FilePath)) File.Delete(it.FilePath); } catch { }
                    }
                }
            }
        }

        public bool CopyItemToClipboard(VaultCaptureItem item)
        {
            if (item == null || !File.Exists(item.FilePath)) return false;
            try
            {
                lock (_lock) { _lastCaptureTick = Environment.TickCount; }
                byte[] raw = File.ReadAllBytes(item.FilePath);
                using (var ms = new MemoryStream(raw))
                using (var img = Image.FromStream(ms))
                {
                    Clipboard.SetImage(img);
                    return true;
                }
            }
            catch { return false; }
        }

        public void Dispose()
        {
            try
            {
                if (_watcher != null)
                {
                    _watcher.EnableRaisingEvents = false;
                    _watcher.Dispose();
                    _watcher = null;
                }
            }
            catch { }
            CleanVaultDirectory();
        }
    }

    // =========================================================================
    // 4D. MODULE 5: DOWNLOAD WATCHDOG (STOP-TIMER FOR LARGE DOWNLOADS)
    // =========================================================================
    public sealed class DownloadWatchdogService
    {
        public float CurrentDownMBps { get; private set; }
        public float AverageDownMBps { get; private set; }
        public double SessionDownloadedMB { get; private set; }
        public bool IsActiveDownloadDetected { get; private set; }
        public bool IsWatchdogArmed { get; set; }
        public bool IsCountdownActive { get; private set; }
        public int CountdownRemainingSec { get; private set; }
        public int IdleLowSeconds { get; private set; }

        private int _consecutiveHighSec = 0;
        private int _consecutiveLowSec = 0;
        private int _speedSamplesCount = 0;
        private double _speedSamplesSum = 0.0;

        public void UpdateTrafficSample(float downBytesPerSec, float dtSec, int thresholdMBps)
        {
            float mbps = Math.Max(0f, downBytesPerSec / (1024f * 1024f));
            CurrentDownMBps = mbps;
            if (downBytesPerSec > 32 * 1024)
            {
                SessionDownloadedMB += (downBytesPerSec * dtSec) / (1024.0 * 1024.0);
            }

            if (mbps >= 0.15f)
            {
                _speedSamplesCount++;
                _speedSamplesSum += mbps;
                AverageDownMBps = (float)(_speedSamplesSum / _speedSamplesCount);
            }

            float highThresh = Math.Max(0.5f, thresholdMBps);
            if (mbps >= highThresh)
            {
                _consecutiveHighSec++;
                if (_consecutiveHighSec >= 5)
                    IsActiveDownloadDetected = true;
            }
            else
            {
                _consecutiveHighSec = 0;
                if (mbps < 0.15f && !IsWatchdogArmed)
                    IsActiveDownloadDetected = false;
            }

            if (IsWatchdogArmed && !IsCountdownActive)
            {
                // Low traffic threshold: < 150 KB/s (0.146 MB/s) for 25 consecutive seconds
                if (downBytesPerSec < 150 * 1024)
                {
                    _consecutiveLowSec++;
                    IdleLowSeconds = _consecutiveLowSec;
                    if (_consecutiveLowSec >= 25)
                    {
                        IsCountdownActive = true;
                        CountdownRemainingSec = 15;
                        _consecutiveLowSec = 0;
                    }
                }
                else
                {
                    _consecutiveLowSec = 0;
                    IdleLowSeconds = 0;
                }
            }
            else if (!IsWatchdogArmed)
            {
                _consecutiveLowSec = 0;
                IdleLowSeconds = 0;
            }
        }

        public bool TickCountdownSecond()
        {
            if (!IsCountdownActive) return false;
            CountdownRemainingSec--;
            if (CountdownRemainingSec <= 0)
            {
                IsCountdownActive = false;
                IsWatchdogArmed = false;
                return true; // Execute power action now!
            }
            return false;
        }

        public void CancelCountdown()
        {
            IsCountdownActive = false;
            IsWatchdogArmed = false;
            _consecutiveLowSec = 0;
            IdleLowSeconds = 0;
        }

        public static void ExecutePowerAction(string action)
        {
            try
            {
                if (action == "Выключение")
                {
                    Process.Start(new ProcessStartInfo("shutdown.exe", "/s /t 0") { CreateNoWindow = true, UseShellExecute = false });
                }
                else if (action == "Гибернация")
                {
                    NativeWin32.SetSuspendState(true, false, false);
                }
                else // "Сон"
                {
                    NativeWin32.SetSuspendState(false, false, false);
                }
            }
            catch { }
        }
    }

    // =========================================================================
    // 4E. MODULE 6: UNOBTRUSIVE GITHUB RELEASES UPDATE MANAGER (USER-CHOICE)
    // =========================================================================
    public sealed class UpdateManager
    {
        public const string CurrentVersionStr = "3.0.0";
        public string LatestVersionStr { get; private set; }
        public string ReleaseNotes { get; private set; }
        public string DownloadUrl { get; private set; }
        public string ExpectedSha256 { get; private set; }
        public string StatusText { get; private set; }
        public bool IsUpdateAvailable { get; private set; }
        public bool IsDownloading { get; private set; }
        public float DownloadProgress { get; private set; }
        public bool DismissedForSession { get; set; }

        public event Action OnStateChanged;

        public UpdateManager()
        {
            LatestVersionStr = CurrentVersionStr;
            ReleaseNotes = "";
            DownloadUrl = "";
            ExpectedSha256 = "";
            StatusText = "v" + CurrentVersionStr + " (Актуальная)";
        }

        public void CheckForUpdatesAsync(IslandConfigData cfg, bool isManualCheck)
        {
            if (IsDownloading) return;
            StatusText = "Проверка обновлений...";
            if (OnStateChanged != null) OnStateChanged();

            ThreadPool.QueueUserWorkItem(_ =>
            {
                try
                {
                    ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;
                    string repo = (cfg != null && !string.IsNullOrEmpty(cfg.github_repo))
                        ? cfg.github_repo
                        : "Miltanchik/DynamicIsland";
                    string apiUrl = "https://api.github.com/repos/" + repo + "/releases/latest";

                    var req = (HttpWebRequest)WebRequest.Create(apiUrl);
                    req.UserAgent = "DynamicIslandWin-Updater/" + CurrentVersionStr;
                    req.Timeout = 6500;
                    req.Accept = "application/vnd.github.v3+json";

                    using (var resp = (HttpWebResponse)req.GetResponse())
                    using (var sr = new StreamReader(resp.GetResponseStream(), Encoding.UTF8))
                    {
                        string json = sr.ReadToEnd();
                        var ser = new JavaScriptSerializer();
                        var dict = ser.Deserialize<Dictionary<string, object>>(json);
                        if (dict != null && dict.ContainsKey("tag_name"))
                        {
                            string tag = (dict["tag_name"] as string ?? "").Trim().TrimStart('v', 'V');
                            string body = (dict.ContainsKey("body") ? (dict["body"] as string ?? "") : "").Trim();
                            string exeUrl = "";
                            string sha256 = "";

                            if (dict.ContainsKey("assets") && dict["assets"] is System.Collections.ArrayList)
                            {
                                var assets = (System.Collections.ArrayList)dict["assets"];
                                foreach (object aObj in assets)
                                {
                                    var a = aObj as Dictionary<string, object>;
                                    if (a == null) continue;
                                    string nm = (a.ContainsKey("name") ? (a["name"] as string ?? "") : "").ToLowerInvariant();
                                    string dl = a.ContainsKey("browser_download_url") ? (a["browser_download_url"] as string ?? "") : "";
                                    if (nm.EndsWith(".exe") && (nm.Contains("setup") || nm.Contains("dynamicisland")))
                                        exeUrl = dl;
                                }
                            }

                            Version vCur, vNew;
                            if (Version.TryParse(CurrentVersionStr, out vCur) && Version.TryParse(tag, out vNew) && vNew > vCur)
                            {
                                if (!isManualCheck && cfg != null && string.Equals(cfg.skipped_version, tag, StringComparison.OrdinalIgnoreCase))
                                {
                                    StatusText = "v" + CurrentVersionStr + " (Пропущена v" + tag + ")";
                                    if (OnStateChanged != null) OnStateChanged();
                                    return;
                                }

                                LatestVersionStr = tag;
                                ReleaseNotes = string.IsNullOrEmpty(body) ? "Улучшения стабильности и новые функции Studio Pro." : body;
                                DownloadUrl = exeUrl;
                                ExpectedSha256 = sha256;
                                IsUpdateAvailable = true;
                                DismissedForSession = false;
                                StatusText = "Доступна v" + tag + "!";
                                if (OnStateChanged != null) OnStateChanged();

                                if (cfg != null && cfg.auto_update_enabled && !string.IsNullOrEmpty(DownloadUrl))
                                {
                                    StartDownloadAndApplyAsync();
                                }
                                return;
                            }
                        }
                    }

                    IsUpdateAvailable = false;
                    StatusText = "Установлена последняя версия v" + CurrentVersionStr;
                }
                catch
                {
                    StatusText = "Установлена последняя версия v" + CurrentVersionStr;
                }
                if (OnStateChanged != null) OnStateChanged();
            });
        }

        public void StartDownloadAndApplyAsync()
        {
            if (IsDownloading || string.IsNullOrEmpty(DownloadUrl)) return;
            IsDownloading = true;
            DownloadProgress = 0.02f;
            StatusText = "Загрузка v" + LatestVersionStr + "...";
            if (OnStateChanged != null) OnStateChanged();

            ThreadPool.QueueUserWorkItem(_ =>
            {
                try
                {
                    string updDir = Path.Combine(Path.GetTempPath(), @"DinamicIsland\Update");
                    if (!Directory.Exists(updDir)) Directory.CreateDirectory(updDir);
                    string targetFile = Path.Combine(updDir, "DynamicIslandWin_Setup.exe");
                    if (File.Exists(targetFile)) File.Delete(targetFile);

                    ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;
                    var req = (HttpWebRequest)WebRequest.Create(DownloadUrl);
                    req.UserAgent = "DynamicIslandWin-Updater/" + CurrentVersionStr;
                    req.Timeout = 15000;

                    using (var resp = (HttpWebResponse)req.GetResponse())
                    using (var rs = resp.GetResponseStream())
                    using (var fs = new FileStream(targetFile, FileMode.Create, FileAccess.Write, FileShare.None))
                    {
                        long total = Math.Max(1, resp.ContentLength);
                        byte[] buf = new byte[16384];
                        long readTotal = 0;
                        int r;
                        while ((r = rs.Read(buf, 0, buf.Length)) > 0)
                        {
                            fs.Write(buf, 0, r);
                            readTotal += r;
                            DownloadProgress = Math.Max(0.05f, Math.Min(0.99f, readTotal / (float)total));
                            if (OnStateChanged != null) OnStateChanged();
                        }
                    }

                    if (!string.IsNullOrEmpty(ExpectedSha256))
                    {
                        using (var sha = SHA256.Create())
                        using (var fs = File.OpenRead(targetFile))
                        {
                            byte[] hashBytes = sha.ComputeHash(fs);
                            var sb = new StringBuilder();
                            foreach (byte b in hashBytes) sb.Append(b.ToString("x2"));
                            if (!string.Equals(sb.ToString(), ExpectedSha256, StringComparison.OrdinalIgnoreCase))
                            {
                                IsDownloading = false;
                                StatusText = "Ошибка SHA-256 (Повторить)";
                                if (OnStateChanged != null) OnStateChanged();
                                return;
                            }
                        }
                    }

                    DownloadProgress = 1.0f;
                    StatusText = "Перезапуск и обновление...";
                    if (OnStateChanged != null) OnStateChanged();

                    Process.Start(new ProcessStartInfo
                    {
                        FileName = targetFile,
                        Arguments = "--silent",
                        UseShellExecute = false
                    });
                    Thread.Sleep(250);
                    Application.Exit();
                }
                catch
                {
                    IsDownloading = false;
                    StatusText = "Ошибка загрузки (Повторить)";
                    if (OnStateChanged != null) OnStateChanged();
                }
            });
        }
    }

    public sealed class SettingsForm : Form
    {
        private readonly IslandConfigData _cfg;
        public event Action SettingsSaved;
        public event Action ManualUpdateCheckRequested;

        // Motion Lab Palette (Strict Black Hierarchy: #000000 ONLY on Stage Island)
        private static readonly Color ColChassis = Color.FromArgb(11, 11, 13);        // #0B0B0D
        private static readonly Color ColStageBg = Color.FromArgb(14, 14, 17);        // #0E0E11
        private static readonly Color ColPureIsland = Color.FromArgb(0, 0, 0);        // #000000 (Exclusive!)
        private static readonly Color ColDeckCard = Color.FromArgb(18, 18, 21);       // #121215
        private static readonly Color ColDeckHover = Color.FromArgb(26, 26, 31);      // #1A1A1F
        private static readonly Color ColDeckInset = Color.FromArgb(13, 13, 16);      // #0D0D10
        private static readonly Color ColBorder = Color.FromArgb(21, 255, 255, 255);  // rgba(255,255,255,0.08)
        private static readonly Color ColBorderHi = Color.FromArgb(56, 255, 255, 255);// rgba(255,255,255,0.22)
        private static readonly Color ColAccent = Color.FromArgb(255, 85, 0);         // #FF5500 Kinetic Orange
        private static readonly Color ColLime = Color.FromArgb(200, 255, 0);          // #C8FF00 Acid Lime
        private static readonly Color ColTextMain = Color.FromArgb(242, 243, 247);
        private static readonly Color ColTextMuted = Color.FromArgb(138, 142, 154);
        private static readonly Color ColTextDim = Color.FromArgb(92, 96, 108);

        private enum StagePreviewMode { Idle = 0, Compact = 1, Expanded = 2, Media = 3, Notify = 4 }

        private int _activeTab = 0;          // 0..3 Mode Rail
        private int _inspectorSubMode = 0;   // 0 = Timer & Audio, 1 = Player & EQ, 2 = Vault & Watchdog
        private StagePreviewMode _stageMode = StagePreviewMode.Compact;
        private string _filterQuery = "";
        private bool _editingFilter = false;

        // Live 60Hz Stage Physics & Oscilloscope State
        private readonly System.Windows.Forms.Timer _stageTimer;
        private readonly Stopwatch _stageAnimWatch = new Stopwatch();
        private readonly CustomBackgroundEngine _stageBgPreview = new CustomBackgroundEngine();
        private bool _stageAnimating = false;
        private bool _stageExpanding = true;
        private int _stageAnimDurMs = 330;
        private float _stageStartW = 310f, _stageStartH = 36f, _stageStartExp = 0f;
        private float _stageCurW = 310f, _stageCurH = 36f, _stageCurExp = 0f;
        private float _stageTargetW = 310f, _stageTargetH = 36f, _stageTargetExp = 0f;
        private long _stagePulseStartMs = -9999;
        private float _stageWavePhase = 0f;
        private long _lastModifiedTick = 0;

        // Input & Hit-Testing State
        private bool _draggingWindow = false;
        private Point _windowDragOffset;
        private PointF _mousePt = new PointF(-999, -999);

        private sealed class HitZone
        {
            public RectangleF Rect;
            public Action OnClick;
        }

        private sealed class PrecisionSliderHit
        {
            public RectangleF TrackRect;
            public RectangleF WheelRect;
            public int MinVal;
            public int MaxVal;
            public Func<int> Getter;
            public Action<int> Setter;
        }

        private readonly List<HitZone> _hits = new List<HitZone>();
        private readonly List<PrecisionSliderHit> _sliderHits = new List<PrecisionSliderHit>();
        private PrecisionSliderHit _draggingSlider = null;
        private bool _draggingSpatialPad = false;
        private RectangleF _spatialPadRect = RectangleF.Empty;
        private RectangleF _stageIslandHitRect = RectangleF.Empty;

        // Inline Monospace Numeric & Search Input (Zero system dialogs)
        private readonly TextBox _inlineBox;
        private Action<int> _inlineCommitSetter = null;
        private int _inlineMin = 0, _inlineMax = 100;
        private bool _inlineCommitting = false;

        private readonly string[] _railCodes = { "01", "02", "03", "04" };
        private readonly string[] _railTitles =
        {
            "ФИЗИКА И ФОРМА",
            "МОДУЛИ ОСТРОВА",
            "ЭКРАН И РЕЖИМЫ",
            "СКИН И СИСТЕМА"
        };

        private readonly Font _fConsoleBrand = new Font("Consolas", 8.8f, FontStyle.Bold);
        private readonly Font _fBentoHeader = new Font("Segoe UI Semibold", 8.4f, FontStyle.Bold);
        private readonly Font _fLabel = new Font("Segoe UI Semibold", 8.8f, FontStyle.Bold);
        private readonly Font _fSub = new Font("Segoe UI", 7.8f, FontStyle.Regular);
        private readonly Font _fChip = new Font("Segoe UI Semibold", 8.0f, FontStyle.Bold);
        private readonly Font _fMonoBig = new Font("Consolas", 11.0f, FontStyle.Bold);
        private readonly Font _fMonoVal = new Font("Consolas", 9.2f, FontStyle.Bold);
        private readonly Font _fMonoTiny = new Font("Consolas", 7.6f, FontStyle.Bold);

        private readonly StringFormat _sfLeft = new StringFormat { Alignment = StringAlignment.Near, LineAlignment = StringAlignment.Center, Trimming = StringTrimming.EllipsisCharacter, FormatFlags = StringFormatFlags.NoWrap };
        private readonly StringFormat _sfCenter = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center, Trimming = StringTrimming.EllipsisCharacter, FormatFlags = StringFormatFlags.NoWrap };
        private readonly StringFormat _sfRight = new StringFormat { Alignment = StringAlignment.Far, LineAlignment = StringAlignment.Center, Trimming = StringTrimming.EllipsisCharacter, FormatFlags = StringFormatFlags.NoWrap };

        public SettingsForm(IslandConfigData cfg)
        {
            _cfg = cfg;
            Text = "Dynamic Island — Kinetic Stage & Deck";
            FormBorderStyle = FormBorderStyle.None;
            StartPosition = FormStartPosition.CenterScreen;
            TopMost = true;
            DoubleBuffered = true;
            SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.UserPaint, true);
            ClientSize = new Size(880, 640);
            BackColor = ColChassis;
            ForeColor = ColTextMain;
            try { Icon = Icon.ExtractAssociatedIcon(Application.ExecutablePath); } catch { }

            // Crisp 8px technical chassis corners (no soft rounded macOS sheet)
            Region = Region.FromHrgn(NativeWin32.CreateRoundRectRgn(0, 0, Width + 1, Height + 1, 10, 10));

            _inlineBox = new TextBox
            {
                Visible = false,
                BorderStyle = BorderStyle.None,
                BackColor = ColDeckInset,
                ForeColor = ColAccent,
                Font = new Font("Consolas", 9.6f, FontStyle.Bold),
                TextAlign = HorizontalAlignment.Center
            };
            _inlineBox.KeyDown += (s, e) =>
            {
                if (e.KeyCode == Keys.Enter)
                {
                    e.SuppressKeyPress = true;
                    CommitInlineInput(true);
                }
                else if (e.KeyCode == Keys.Escape)
                {
                    e.SuppressKeyPress = true;
                    CommitInlineInput(false);
                }
            };
            _inlineBox.TextChanged += (s, e) =>
            {
                if (_editingFilter && _inlineBox.Visible)
                {
                    _filterQuery = (_inlineBox.Text ?? "").Trim();
                    RouteFilterQuery(_filterQuery);
                    Invalidate();
                }
            };
            _inlineBox.LostFocus += (s, e) => CommitInlineInput(true);
            Controls.Add(_inlineBox);

            SyncStageTargetsFromMode(false);
            _stageCurW = _stageTargetW;
            _stageCurH = _stageTargetH;
            _stageCurExp = _stageTargetExp;

            _stageTimer = new System.Windows.Forms.Timer { Interval = 15 };
            _stageTimer.Tick += OnStageTick;
            _stageTimer.Start();
        }

        private void RouteFilterQuery(string q)
        {
            if (string.IsNullOrEmpty(q)) return;
            string lq = q.ToLowerInvariant();
            if (lq.Contains("тайм") || lq.Contains("помод") || lq.Contains("звук") || lq.Contains("мелод"))
            {
                _activeTab = 1; _inspectorSubMode = 0;
            }
            else if (lq.Contains("плеер") || lq.Contains("муз") || lq.Contains("облож") || lq.Contains("лайк"))
            {
                _activeTab = 1; _inspectorSubMode = 1;
            }
            else if (lq.Contains("скрин") || lq.Contains("закач") || lq.Contains("сон") || lq.Contains("watch") || lq.Contains("drop") || lq.Contains("батар") || lq.Contains("час"))
            {
                _activeTab = 1; _inspectorSubMode = 2;
            }
            else if (lq.Contains("монит") || lq.Contains("экран") || lq.Contains("игр") || lq.Contains("призр") || lq.Contains("микр") || lq.Contains("отступ") || lq.Contains("сдвиг"))
            {
                _activeTab = 2;
            }
            else if (lq.Contains("gif") || lq.Contains("фото") || lq.Contains("цвет") || lq.Contains("тем") || lq.Contains("скин") || lq.Contains("автозап") || lq.Contains("обнов") || lq.Contains("блик"))
            {
                _activeTab = 3;
            }
            else if (lq.Contains("шир") || lq.Contains("выс") || lq.Contains("пруж") || lq.Contains("скор") || lq.Contains("радиус") || lq.Contains("масштаб"))
            {
                _activeTab = 0;
            }
        }

        private bool MatchesFilter(string text)
        {
            if (string.IsNullOrEmpty(_filterQuery) || string.IsNullOrEmpty(text)) return false;
            return text.IndexOf(_filterQuery, StringComparison.OrdinalIgnoreCase) >= 0;
        }

        private float EvaluateStageSpring(float t, bool expanding)
        {
            if (t <= 0f) return 0f;
            if (t >= 1f) return 1f;
            float zeta = expanding
                ? Math.Max(0.45f, Math.Min(0.95f, 1.30f - (_cfg.spring_overshoot_pct / 190f)))
                : 0.94f;
            float omega0 = expanding ? 11.4f : 10.4f;
            float omegaD = omega0 * (float)Math.Sqrt(Math.Max(0.01f, 1.0f - zeta * zeta));
            float env = (float)Math.Exp(-zeta * omega0 * t);
            return 1.0f - env * ((float)Math.Cos(omegaD * t) + (zeta * omega0 / omegaD) * (float)Math.Sin(omegaD * t));
        }

        private void SyncStageTargetsFromMode(bool animate)
        {
            float tw = _cfg.idle_width;
            float th = _cfg.idle_height;
            float texp = 0f;

            if (_stageMode == StagePreviewMode.Idle)
            {
                tw = _cfg.idle_width;
                th = _cfg.idle_height;
                texp = 0f;
            }
            else if (_stageMode == StagePreviewMode.Compact)
            {
                tw = Math.Max(_cfg.idle_width, 310);
                th = _cfg.idle_height;
                texp = 0f;
            }
            else if (_stageMode == StagePreviewMode.Media)
            {
                tw = Math.Max(_cfg.idle_width + 44, 348);
                th = Math.Max(_cfg.idle_height + 6, 42);
                texp = 0f;
            }
            else if (_stageMode == StagePreviewMode.Notify)
            {
                tw = Math.Max(_cfg.idle_width + 96, 396);
                th = Math.Max(_cfg.idle_height + 10, 46);
                texp = 0f;
            }
            else // Expanded
            {
                tw = _cfg.expanded_width;
                th = _cfg.expanded_height;
                texp = 1f;
            }

            if (!animate)
            {
                _stageTargetW = tw; _stageTargetH = th; _stageTargetExp = texp;
                return;
            }

            _stageStartW = _stageCurW;
            _stageStartH = _stageCurH;
            _stageStartExp = _stageCurExp;
            _stageTargetW = tw;
            _stageTargetH = th;
            _stageTargetExp = texp;
            _stageExpanding = (tw * th >= _stageStartW * _stageStartH);
            _stageAnimDurMs = Math.Max(110, _cfg.anim_duration_ms);
            _stageAnimWatch.Restart();
            _stageAnimating = true;
        }

        private void TriggerTestPulse()
        {
            _stagePulseStartMs = Environment.TickCount;
            _stageStartW = _stageTargetW * 0.76f;
            _stageStartH = _stageTargetH * 0.80f;
            _stageStartExp = _stageCurExp;
            _stageCurW = _stageStartW;
            _stageCurH = _stageStartH;
            _stageExpanding = true;
            _stageAnimDurMs = Math.Max(120, _cfg.anim_duration_ms);
            _stageAnimWatch.Restart();
            _stageAnimating = true;
            Invalidate();
        }

        private void OnStageTick(object sender, EventArgs e)
        {
            bool dirty = false;
            _stageWavePhase += 0.16f;

            if (_stageAnimating)
            {
                float rawT = Math.Max(0f, Math.Min(1f, _stageAnimWatch.ElapsedMilliseconds / (float)_stageAnimDurMs));
                float s = EvaluateStageSpring(rawT, _stageExpanding);
                _stageCurW = _stageStartW + (_stageTargetW - _stageStartW) * s;
                _stageCurH = _stageStartH + (_stageTargetH - _stageStartH) * s;
                _stageCurExp = Math.Max(0f, Math.Min(1f, _stageStartExp + (_stageTargetExp - _stageStartExp) * s));
                if (rawT >= 1.0f)
                {
                    _stageCurW = _stageTargetW;
                    _stageCurH = _stageTargetH;
                    _stageCurExp = _stageTargetExp;
                    _stageAnimating = false;
                }
                dirty = true;
            }
            else
            {
                // Smooth immediate tracking when user drags geometry sliders
                if (Math.Abs(_stageCurW - _stageTargetW) > 0.4f || Math.Abs(_stageCurH - _stageTargetH) > 0.4f)
                {
                    _stageCurW += (_stageTargetW - _stageCurW) * 0.38f;
                    _stageCurH += (_stageTargetH - _stageCurH) * 0.38f;
                    _stageCurExp += (_stageTargetExp - _stageCurExp) * 0.38f;
                    dirty = true;
                }
                else
                {
                    _stageCurW = _stageTargetW;
                    _stageCurH = _stageTargetH;
                    _stageCurExp = _stageTargetExp;
                }
            }

            if (_stageBgPreview.AdvanceAnimationFrame())
                dirty = true;

            if (_stageMode != StagePreviewMode.Idle || (Environment.TickCount - _stagePulseStartMs) < 900)
                dirty = true;

            if (dirty)
                Invalidate(new Rectangle(0, 32, Width, 290));
        }

        private void BeginInlineNumericEdit(RectangleF boxRc, int curVal, int minVal, int maxVal, Action<int> setter)
        {
            CommitInlineInput(true);
            _editingFilter = false;
            _inlineMin = minVal;
            _inlineMax = maxVal;
            _inlineCommitSetter = setter;

            _inlineBox.TextAlign = HorizontalAlignment.Center;
            _inlineBox.SetBounds(
                (int)Math.Round(boxRc.X + 2),
                (int)Math.Round(boxRc.Y + (boxRc.Height - 16) / 2f),
                Math.Max(38, (int)Math.Round(boxRc.Width - 4)),
                16);
            _inlineBox.Text = curVal.ToString();
            _inlineBox.Visible = true;
            _inlineBox.BringToFront();
            _inlineBox.Focus();
            _inlineBox.SelectAll();
        }

        private void BeginInlineFilterEdit(RectangleF boxRc)
        {
            CommitInlineInput(true);
            _editingFilter = true;
            _inlineCommitSetter = null;

            _inlineBox.TextAlign = HorizontalAlignment.Left;
            _inlineBox.SetBounds(
                (int)Math.Round(boxRc.X + 22),
                (int)Math.Round(boxRc.Y + (boxRc.Height - 16) / 2f),
                Math.Max(54, (int)Math.Round(boxRc.Width - 26)),
                16);
            _inlineBox.Text = _filterQuery;
            _inlineBox.Visible = true;
            _inlineBox.BringToFront();
            _inlineBox.Focus();
            _inlineBox.SelectAll();
        }

        private void CommitInlineInput(bool applyValue)
        {
            if (_inlineCommitting || !_inlineBox.Visible) return;
            _inlineCommitting = true;
            try
            {
                if (_editingFilter)
                {
                    if (!applyValue) _filterQuery = "";
                    _editingFilter = false;
                    _inlineBox.Visible = false;
                    Invalidate();
                    return;
                }

                var setter = _inlineCommitSetter;
                _inlineCommitSetter = null;
                string raw = (_inlineBox.Text ?? "").Trim()
                    .Replace("%", "").Replace("px", "").Replace("мс", "").Replace("ms", "")
                    .Replace("мин", "").Replace("s", "").Replace("x", "").Replace("+", "").Trim();
                _inlineBox.Visible = false;

                if (applyValue && setter != null)
                {
                    int parsed;
                    if (int.TryParse(raw, out parsed))
                    {
                        parsed = Math.Max(_inlineMin, Math.Min(_inlineMax, parsed));
                        setter(parsed);
                        ApplyLive();
                    }
                }
                Invalidate();
            }
            finally
            {
                _inlineCommitting = false;
            }
        }

        private void ApplyLive()
        {
            _lastModifiedTick = Environment.TickCount;
            SyncStageTargetsFromMode(false);
            ConfigManager.SetAutostart(_cfg.autostart);
            ConfigManager.Save(_cfg);
            if (SettingsSaved != null) SettingsSaved();
            Invalidate();
        }

        private static GraphicsPath SharpRoundRect(RectangleF r, float rad)
        {
            float d = Math.Min(rad * 2f, Math.Min(r.Width, r.Height));
            var p = new GraphicsPath();
            if (d <= 1f) { p.AddRectangle(r); return p; }
            p.AddArc(r.X, r.Y, d, d, 180, 90);
            p.AddArc(r.Right - d, r.Y, d, d, 270, 90);
            p.AddArc(r.Right - d, r.Bottom - d, d, d, 0, 90);
            p.AddArc(r.X, r.Bottom - d, d, d, 90, 90);
            p.CloseFigure();
            return p;
        }

        private void GetPreviewThemeColors(int alpha, out Color c1, out Color c2)
        {
            string preset = _cfg.theme_preset ?? "Dinasmic Neon";
            if (preset == "Kinetic Orange") { c1 = Color.FromArgb(alpha, 255, 85, 0); c2 = Color.FromArgb(alpha, 255, 159, 10); }
            else if (preset == "Acid Lime") { c1 = Color.FromArgb(alpha, 200, 255, 0); c2 = Color.FromArgb(alpha, 16, 185, 129); }
            else if (preset == "Dinasmic Neon") { c1 = Color.FromArgb(alpha, 34, 211, 238); c2 = Color.FromArgb(alpha, 236, 72, 153); }
            else if (preset == "Apple Blue") { c1 = Color.FromArgb(alpha, 10, 132, 255); c2 = Color.FromArgb(alpha, 139, 92, 246); }
            else if (preset == "Yandex Gold-Yellow") { c1 = Color.FromArgb(alpha, 255, 214, 10); c2 = Color.FromArgb(alpha, 255, 159, 10); }
            else if (preset == "Spotify Neon Green") { c1 = Color.FromArgb(alpha, 48, 209, 88); c2 = Color.FromArgb(alpha, 34, 211, 238); }
            else if (preset == "Crimson Sunset") { c1 = Color.FromArgb(alpha, 255, 55, 95); c2 = Color.FromArgb(alpha, 255, 159, 10); }
            else if (preset == "Minimal White") { c1 = Color.FromArgb(alpha, 245, 245, 247); c2 = Color.FromArgb(alpha, 174, 174, 178); }
            else { c1 = Color.FromArgb(alpha, 34, 211, 238); c2 = Color.FromArgb(alpha, 192, 132, 252); }
        }
        // =====================================================================
        // REUSABLE CUSTOM CONTROLS (Zero System Widgets, Zero Blur, 1px Borders)
        // =====================================================================
        private void DrawBentoCell(Graphics g, RectangleF rc, string code, string title, string rightMeta = "")
        {
            bool hovered = rc.Contains(_mousePt) && _draggingSlider == null && !_draggingSpatialPad;
            bool filterHit = MatchesFilter(title) || MatchesFilter(rightMeta);

            using (GraphicsPath path = SharpRoundRect(rc, 6f))
            using (var bgBr = new SolidBrush(ColDeckCard))
            using (var pen = new Pen(filterHit ? ColAccent : (hovered ? Color.FromArgb(36, 255, 255, 255) : ColBorder), 1f))
            {
                g.FillPath(bgBr, path);
                g.DrawPath(pen, path);
            }

            // Crisp 1px header divider
            float headH = 24f;
            using (var divPen = new Pen(ColBorder, 1f))
                g.DrawLine(divPen, rc.X + 1f, rc.Y + headH, rc.Right - 1f, rc.Y + headH);

            using (var codeBr = new SolidBrush(ColAccent))
            using (var titleBr = new SolidBrush(ColTextMuted))
            using (var metaBr = new SolidBrush(ColTextDim))
            {
                g.DrawString("[" + code + "]", _fMonoTiny, codeBr, new RectangleF(rc.X + 10f, rc.Y, 48f, headH), _sfLeft);
                g.DrawString(title, _fBentoHeader, titleBr, new RectangleF(rc.X + 54f, rc.Y, rc.Width - 130f, headH), _sfLeft);
                if (!string.IsNullOrEmpty(rightMeta))
                    g.DrawString(rightMeta, _fMonoTiny, metaBr, new RectangleF(rc.Right - 130f, rc.Y, 120f, headH), _sfRight);
            }
        }

        private void DrawHardwareButton(Graphics g, RectangleF rc, string label, bool primaryAccent, bool danger, Action onClick)
        {
            bool hov = rc.Contains(_mousePt);
            Color bg = primaryAccent
                ? (hov ? Color.FromArgb(255, 102, 24) : ColAccent)
                : (danger
                    ? (hov ? Color.FromArgb(52, 22, 24) : Color.FromArgb(28, 16, 18))
                    : (hov ? ColDeckHover : ColDeckCard));
            Color border = primaryAccent
                ? Color.FromArgb(220, 255, 140, 80)
                : (danger ? Color.FromArgb(110, 255, 70, 60) : (hov ? ColBorderHi : ColBorder));
            Color fg = primaryAccent
                ? Color.FromArgb(12, 12, 14)
                : (danger ? Color.FromArgb(255, 105, 95) : ColTextMain);

            using (GraphicsPath p = SharpRoundRect(rc, 4f))
            using (var b = new SolidBrush(bg))
            using (var pen = new Pen(border, 1f))
            using (var tb = new SolidBrush(fg))
            {
                g.FillPath(b, p);
                g.DrawPath(pen, p);
                g.DrawString(label, _fMonoTiny, tb, rc, _sfCenter);
            }
            if (onClick != null)
                _hits.Add(new HitZone { Rect = rc, OnClick = () => { CommitInlineInput(true); onClick(); } });
        }

        private void DrawPrecisionTickSlider(
            Graphics g,
            RectangleF cellRc,
            string label,
            int curVal,
            int minVal,
            int maxVal,
            string formattedReadout,
            int tickCount,
            Func<int> getter,
            Action<int> setter)
        {
            bool hov = cellRc.Contains(_mousePt);
            bool filterHit = MatchesFilter(label);

            using (GraphicsPath cp = SharpRoundRect(cellRc, 5f))
            using (var cb = new SolidBrush(hov ? ColDeckHover : ColDeckInset))
            using (var cPen = new Pen(filterHit ? ColAccent : (hov ? ColBorderHi : ColBorder), 1f))
            {
                g.FillPath(cb, cp);
                g.DrawPath(cPen, cp);
            }

            // Top row: Label on left, Clickable Tabular Monospace Readout on right
            var valBoxRc = new RectangleF(cellRc.Right - 72f, cellRc.Y + 4f, 66f, 19f);
            bool valHov = valBoxRc.Contains(_mousePt);

            using (var lblBr = new SolidBrush(hov ? ColTextMain : ColTextMuted))
                g.DrawString(label, _fChip, lblBr, new RectangleF(cellRc.X + 8f, cellRc.Y + 4f, cellRc.Width - 84f, 19f), _sfLeft);

            using (GraphicsPath vp = SharpRoundRect(valBoxRc, 3f))
            using (var vb = new SolidBrush(Color.FromArgb(10, 10, 12)))
            using (var vPen = new Pen(valHov ? ColAccent : ColBorder, 1f))
            using (var vt = new SolidBrush(ColAccent))
            {
                g.FillPath(vb, vp);
                g.DrawPath(vPen, vp);
                g.DrawString(formattedReadout, _fMonoVal, vt, valBoxRc, _sfCenter);
            }

            // Bottom row: Precision Graduated Ruler Scale + Active Bar + Hairline Cursor
            float trackLeft = cellRc.X + 10f;
            float trackRight = cellRc.Right - 10f;
            float trackW = Math.Max(24f, trackRight - trackLeft);
            float rulerBaseY = cellRc.Bottom - 8f;
            var trackHitRc = new RectangleF(trackLeft, cellRc.Y + 24f, trackW, Math.Max(14f, cellRc.Height - 26f));

            float ratio = (maxVal > minVal)
                ? Math.Max(0f, Math.Min(1f, (curVal - minVal) / (float)(maxVal - minVal)))
                : 0f;
            float activeX = trackLeft + trackW * ratio;

            int ticks = Math.Max(6, tickCount);
            using (var penDimTick = new Pen(Color.FromArgb(42, 255, 255, 255), 1f))
            using (var penActTick = new Pen(Color.FromArgb(195, 255, 85, 0), 1f))
            using (var penRail = new Pen(Color.FromArgb(35, 255, 255, 255), 1.5f))
            using (var penActiveRail = new Pen(ColAccent, 2.0f))
            {
                g.DrawLine(penRail, trackLeft, rulerBaseY, trackRight, rulerBaseY);
                if (activeX > trackLeft + 1f)
                    g.DrawLine(penActiveRail, trackLeft, rulerBaseY, activeX, rulerBaseY);

                for (int i = 0; i <= ticks; i++)
                {
                    float tx = trackLeft + (trackW * i) / ticks;
                    bool major = (i == 0 || i == ticks || i == ticks / 2 || i % 4 == 0);
                    float th = major ? 7.5f : 4.0f;
                    g.DrawLine(tx <= activeX + 0.5f ? penActTick : penDimTick, tx, rulerBaseY - th, tx, rulerBaseY);
                }
            }

            // Precision Hairline Thumb Marker
            var thumbRc = new RectangleF(activeX - 3.5f, rulerBaseY - 10f, 7f, 12f);
            using (GraphicsPath tp = SharpRoundRect(thumbRc, 2f))
            using (var tb = new SolidBrush(ColTextMain))
            using (var tPen = new Pen(ColAccent, 1.2f))
            {
                g.FillPath(tb, tp);
                g.DrawPath(tPen, tp);
            }

            _sliderHits.Add(new PrecisionSliderHit
            {
                TrackRect = RectangleF.Inflate(trackHitRc, 5f, 3f),
                WheelRect = cellRc,
                MinVal = minVal,
                MaxVal = maxVal,
                Getter = getter,
                Setter = setter
            });

            _hits.Add(new HitZone
            {
                Rect = valBoxRc,
                OnClick = () => BeginInlineNumericEdit(valBoxRc, curVal, minVal, maxVal, setter)
            });
        }

        private void DrawSegmentedToggle(Graphics g, RectangleF rowRc, string label, bool value, Action<bool> setter)
        {
            bool hov = rowRc.Contains(_mousePt) && _draggingSlider == null;
            bool filterHit = MatchesFilter(label);

            using (GraphicsPath rp = SharpRoundRect(rowRc, 5f))
            using (var rb = new SolidBrush(hov ? ColDeckHover : ColDeckInset))
            using (var rPen = new Pen(filterHit ? ColAccent : (hov ? ColBorderHi : ColBorder), 1f))
            {
                g.FillPath(rb, rp);
                g.DrawPath(rPen, rp);
            }

            using (var lb = new SolidBrush(value ? ColTextMain : ColTextMuted))
                g.DrawString(label, _fChip, lb, new RectangleF(rowRc.X + 9f, rowRc.Y, rowRc.Width - 98f, rowRc.Height), _sfLeft);

            // Rigid Segmented Switch: [ OFF | ● ON ]
            float segW = 78f;
            float segH = Math.Min(21f, rowRc.Height - 6f);
            var segRc = new RectangleF(rowRc.Right - segW - 6f, rowRc.Y + (rowRc.Height - segH) / 2f, segW, segH);
            var offRc = new RectangleF(segRc.X + 1f, segRc.Y + 1f, segW / 2f - 1f, segH - 2f);
            var onRc = new RectangleF(segRc.X + segW / 2f, segRc.Y + 1f, segW / 2f - 1f, segH - 2f);

            using (GraphicsPath sp = SharpRoundRect(segRc, 3.5f))
            using (var sb = new SolidBrush(Color.FromArgb(9, 9, 11)))
            using (var sPen = new Pen(ColBorder, 1f))
            {
                g.FillPath(sb, sp);
                g.DrawPath(sPen, sp);
            }

            if (!value)
            {
                using (GraphicsPath op = SharpRoundRect(offRc, 2.5f))
                using (var ob = new SolidBrush(Color.FromArgb(38, 40, 48)))
                    g.FillPath(ob, op);
            }
            else
            {
                using (GraphicsPath op = SharpRoundRect(onRc, 2.5f))
                using (var ob = new SolidBrush(Color.FromArgb(48, 255, 85, 0)))
                using (var oPen = new Pen(ColAccent, 1f))
                {
                    g.FillPath(ob, op);
                    g.DrawPath(oPen, op);
                }
                using (var led = new SolidBrush(ColLime))
                    g.FillEllipse(led, onRc.X + 5f, onRc.Y + (onRc.Height - 5f) / 2f, 5f, 5f);
            }

            using (var bOff = new SolidBrush(!value ? ColTextMain : ColTextDim))
            using (var bOn = new SolidBrush(value ? ColTextMain : ColTextDim))
            {
                g.DrawString("OFF", _fMonoTiny, bOff, offRc, _sfCenter);
                g.DrawString(value ? "  ON" : "ON", _fMonoTiny, bOn, onRc, _sfCenter);
            }

            _hits.Add(new HitZone { Rect = rowRc, OnClick = () => { CommitInlineInput(true); setter(!value); ApplyLive(); } });
        }

        private void DrawChipSelectorMatrix(
            Graphics g,
            RectangleF areaRc,
            string headerLabel,
            string[] values,
            string[] shortLabels,
            string currentVal,
            int columns,
            Action<string> onSelect,
            Color[] swatchLeds = null)
        {
            float curY = areaRc.Y;
            if (!string.IsNullOrEmpty(headerLabel))
            {
                bool fHit = MatchesFilter(headerLabel);
                using (var hb = new SolidBrush(fHit ? ColAccent : ColTextMuted))
                    g.DrawString(headerLabel, _fMonoTiny, hb, new RectangleF(areaRc.X + 2f, curY, areaRc.Width, 14f), _sfLeft);
                curY += 16f;
            }

            int cols = Math.Max(1, columns);
            int rows = (values.Length + cols - 1) / cols;
            float gapX = 5f, gapY = 5f;
            float availH = Math.Max(22f, areaRc.Bottom - curY);
            float chipW = (areaRc.Width - gapX * (cols - 1)) / cols;
            float chipH = Math.Min(26f, (availH - gapY * (rows - 1)) / Math.Max(1, rows));

            for (int i = 0; i < values.Length; i++)
            {
                int r = i / cols;
                int c = i % cols;
                var cRc = new RectangleF(areaRc.X + c * (chipW + gapX), curY + r * (chipH + gapY), chipW, chipH);
                string val = values[i];
                string disp = (shortLabels != null && i < shortLabels.Length) ? shortLabels[i] : val;
                bool selected = string.Equals(val, currentVal, StringComparison.OrdinalIgnoreCase);
                bool hov = cRc.Contains(_mousePt);

                using (GraphicsPath cp = SharpRoundRect(cRc, 4f))
                using (var cb = new SolidBrush(selected ? Color.FromArgb(36, 255, 85, 0) : (hov ? ColDeckHover : ColDeckInset)))
                using (var cPen = new Pen(selected ? ColAccent : (hov ? ColBorderHi : ColBorder), 1f))
                {
                    g.FillPath(cb, cp);
                    g.DrawPath(cPen, cp);
                }

                float textLeft = cRc.X + 4f;
                if (swatchLeds != null && i < swatchLeds.Length)
                {
                    var swRc = new RectangleF(cRc.X + 6f, cRc.Y + (cRc.Height - 7f) / 2f, 7f, 7f);
                    using (var swBr = new SolidBrush(swatchLeds[i]))
                        g.FillRectangle(swBr, swRc);
                    textLeft = cRc.X + 16f;
                }
                else if (selected)
                {
                    using (var ledBr = new SolidBrush(ColAccent))
                        g.FillEllipse(ledBr, cRc.X + 5f, cRc.Y + (cRc.Height - 5f) / 2f, 5f, 5f);
                    textLeft = cRc.X + 12f;
                }

                using (var tb = new SolidBrush(selected ? ColTextMain : (hov ? ColTextMain : ColTextMuted)))
                {
                    var txtRc = new RectangleF(textLeft, cRc.Y, cRc.Right - textLeft - 3f, cRc.Height);
                    g.DrawString(disp, _fChip, tb, txtRc, (swatchLeds != null || selected) ? _sfLeft : _sfCenter);
                }

                string capturedVal = val;
                _hits.Add(new HitZone { Rect = cRc, OnClick = () => { CommitInlineInput(true); onSelect(capturedVal); ApplyLive(); } });
            }
        }

        private void DrawModuleCartridgeSlot(
            Graphics g,
            RectangleF rc,
            string modCode,
            string title,
            string subState,
            bool enabled,
            bool hasInspector,
            bool inspectorActive,
            Action onToggle,
            Action onOpenInspector)
        {
            bool hov = rc.Contains(_mousePt);
            bool filterHit = MatchesFilter(title) || MatchesFilter(subState);

            Color bg = enabled
                ? (hov ? Color.FromArgb(28, 28, 34) : Color.FromArgb(21, 21, 26))
                : (hov ? Color.FromArgb(20, 20, 24) : ColDeckInset);
            Color border = filterHit
                ? ColAccent
                : (inspectorActive
                    ? ColBorderHi
                    : (enabled ? Color.FromArgb(95, 255, 85, 0) : (hov ? ColBorderHi : ColBorder)));

            using (GraphicsPath cp = SharpRoundRect(rc, 5f))
            using (var cb = new SolidBrush(bg))
            using (var cPen = new Pen(border, 1f))
            {
                g.FillPath(cb, cp);
                g.DrawPath(cPen, cp);
            }

            // Left tactile accent notch bar when latched ON
            if (enabled)
            {
                using (var barBr = new SolidBrush(ColAccent))
                    g.FillRectangle(barBr, rc.X + 1f, rc.Y + 8f, 3f, rc.Height - 16f);
            }

            // Top-left module code + Top-right LED status lamp
            using (var codeBr = new SolidBrush(enabled ? ColAccent : ColTextDim))
                g.DrawString(modCode, _fMonoTiny, codeBr, new RectangleF(rc.X + 10f, rc.Y + 6f, 68f, 13f), _sfLeft);

            Color ledCol = enabled ? ColLime : Color.FromArgb(55, 255, 255, 255);
            float ledRightOffset = hasInspector ? 34f : 12f;
            using (var ledBr = new SolidBrush(ledCol))
                g.FillEllipse(ledBr, rc.Right - ledRightOffset - 6f, rc.Y + 9f, 6f, 6f);

            // Title & live subState
            using (var tBr = new SolidBrush(enabled ? ColTextMain : ColTextMuted))
            using (var sBr = new SolidBrush(enabled ? ColTextMuted : ColTextDim))
            {
                g.DrawString(title, _fLabel, tBr, new RectangleF(rc.X + 10f, rc.Y + 21f, rc.Width - (hasInspector ? 42f : 16f), 18f), _sfLeft);
                g.DrawString(subState, _fMonoTiny, sBr, new RectangleF(rc.X + 10f, rc.Y + 41f, rc.Width - (hasInspector ? 42f : 16f), 15f), _sfLeft);
            }

            if (hasInspector && onOpenInspector != null)
            {
                var gearRc = new RectangleF(rc.Right - 28f, rc.Y + 6f, 22f, rc.Height - 12f);
                bool gHov = gearRc.Contains(_mousePt);
                using (GraphicsPath gp = SharpRoundRect(gearRc, 3.5f))
                using (var gb = new SolidBrush(inspectorActive ? ColAccent : (gHov ? ColDeckHover : Color.FromArgb(12, 12, 15))))
                using (var gPen = new Pen(inspectorActive ? ColAccent : ColBorder, 1f))
                using (var gt = new SolidBrush(inspectorActive ? Color.FromArgb(12, 12, 14) : ColTextMuted))
                {
                    g.FillPath(gb, gp);
                    g.DrawPath(gPen, gp);
                    g.DrawString("⚙", _fChip, gt, gearRc, _sfCenter);
                }

                var mainLatchRc = new RectangleF(rc.X, rc.Y, rc.Width - 30f, rc.Height);
                _hits.Add(new HitZone { Rect = gearRc, OnClick = () => { CommitInlineInput(true); onOpenInspector(); Invalidate(); } });
                _hits.Add(new HitZone { Rect = mainLatchRc, OnClick = () => { CommitInlineInput(true); onToggle(); if (onOpenInspector != null) onOpenInspector(); ApplyLive(); } });
            }
            else
            {
                _hits.Add(new HitZone { Rect = rc, OnClick = () => { CommitInlineInput(true); onToggle(); ApplyLive(); } });
            }
        }

        private void DrawLiveSpringOscilloscope(Graphics g, RectangleF scopeRc)
        {
            using (GraphicsPath sp = SharpRoundRect(scopeRc, 4f))
            using (var sb = new SolidBrush(Color.FromArgb(10, 10, 12)))
            using (var sPen = new Pen(ColBorder, 1f))
            {
                g.FillPath(sb, sp);
                g.DrawPath(sPen, sp);
            }

            float gx = scopeRc.X + 8f;
            float gw = scopeRc.Width - 16f;
            float gyTop = scopeRc.Y + 8f;
            float gh = scopeRc.Height - 16f;
            // Map spring output [0.0 .. 1.38] vertically inside scope
            float yZero = gyTop + gh - 4f;
            float yOne = gyTop + gh * 0.30f;
            float scaleY = yZero - yOne;

            using (var gridPen = new Pen(Color.FromArgb(18, 255, 255, 255), 1f))
            using (var targetPen = new Pen(Color.FromArgb(65, 200, 255, 0), 1f) { DashStyle = DashStyle.Dot })
            {
                for (int i = 1; i <= 3; i++)
                {
                    float vx = gx + (gw * i) / 4f;
                    g.DrawLine(gridPen, vx, gyTop, vx, yZero);
                }
                g.DrawLine(targetPen, gx, yOne, gx + gw, yOne);
            }

            int samples = 56;
            PointF[] pts = new PointF[samples + 1];
            float peak = 1.0f;
            for (int i = 0; i <= samples; i++)
            {
                float t = i / (float)samples;
                float val = EvaluateStageSpring(t, true);
                if (val > peak) peak = val;
                float px = gx + gw * t;
                float py = Math.Max(scopeRc.Y + 3f, Math.Min(scopeRc.Bottom - 3f, yZero - val * scaleY));
                pts[i] = new PointF(px, py);
            }

            using (var curvePen = new Pen(ColAccent, 1.75f))
                g.DrawLines(curvePen, pts);

            // Live playhead dot during TEST PULSE
            long pulseElapsed = Environment.TickCount - _stagePulseStartMs;
            if (pulseElapsed >= 0 && pulseElapsed <= _cfg.anim_duration_ms + 120)
            {
                float pt = Math.Max(0f, Math.Min(1f, pulseElapsed / (float)Math.Max(100, _cfg.anim_duration_ms)));
                float pv = EvaluateStageSpring(pt, true);
                float dx = gx + gw * pt;
                float dy = Math.Max(scopeRc.Y + 3f, Math.Min(scopeRc.Bottom - 3f, yZero - pv * scaleY));
                using (var dotBr = new SolidBrush(ColLime))
                    g.FillEllipse(dotBr, dx - 3.5f, dy - 3.5f, 7f, 7f);
            }

            string tele = string.Format("PEAK {0:0.00}x  •  T={1:0.00}s", peak, _cfg.anim_duration_ms / 1000.0);
            using (var tb = new SolidBrush(ColTextMuted))
                g.DrawString(tele, _fMonoTiny, tb, new RectangleF(scopeRc.X + 8f, scopeRc.Bottom - 16f, scopeRc.Width - 14f, 14f), _sfRight);
        }

        private void DrawSpatialScreenPad(Graphics g, RectangleF padRc)
        {
            _spatialPadRect = padRc;
            bool hov = padRc.Contains(_mousePt) || _draggingSpatialPad;

            using (GraphicsPath pp = SharpRoundRect(padRc, 5f))
            using (var pb = new SolidBrush(Color.FromArgb(10, 10, 13)))
            using (var pPen = new Pen(hov ? ColAccent : ColBorderHi, 1f))
            {
                g.FillPath(pb, pp);
                g.DrawPath(pPen, pp);
            }

            // Engineering grid + Center X=0 & Top Y=0 guides
            float midX = padRc.X + padRc.Width * 0.5f;
            using (var axisPen = new Pen(Color.FromArgb(30, 255, 255, 255), 1f) { DashStyle = DashStyle.Dot })
            using (var bezelPen = new Pen(Color.FromArgb(90, 255, 85, 0), 1.5f))
            {
                g.DrawLine(axisPen, midX, padRc.Y + 2f, midX, padRc.Bottom - 2f);
                g.DrawLine(axisPen, padRc.X + 2f, padRc.Y + padRc.Height * 0.5f, padRc.Right - 2f, padRc.Y + padRc.Height * 0.5f);
                g.DrawLine(bezelPen, padRc.X + 8f, padRc.Y + 6f, padRc.Right - 8f, padRc.Y + 6f);
            }

            // Map horizontal_offset (-500..+500) and top_margin (0..120) onto schematic pad
            float normX = (_cfg.horizontal_offset + 500f) / 1000f; // 0..1
            float normY = _cfg.top_margin / 120f;                  // 0..1
            float pillW = Math.Max(44f, Math.Min(110f, _cfg.idle_width * 0.20f));
            float pillH = Math.Max(10f, Math.Min(22f, _cfg.idle_height * 0.34f));

            float usableW = padRc.Width - pillW - 16f;
            float usableH = padRc.Height - pillH - 26f;
            float ix = padRc.X + 8f + normX * usableW;
            float iy = padRc.Y + 8f + normY * usableH;

            // Coordinate crosshair lines to miniature island
            using (var guidePen = new Pen(Color.FromArgb(65, 255, 85, 0), 1f))
            {
                g.DrawLine(guidePen, ix + pillW / 2f, padRc.Y + 6f, ix + pillW / 2f, iy);
            }

            var miniPill = new RectangleF(ix, iy, pillW, pillH);
            using (GraphicsPath mp = SharpRoundRect(miniPill, pillH / 2f))
            using (var mb = new SolidBrush(ColPureIsland))
            using (var mPen = new Pen(ColAccent, 1.4f))
            {
                g.FillPath(mb, mp);
                g.DrawPath(mPen, mp);
            }

            string posTxt = string.Format("X:{0:+#;-#;0}px  Y:+{1}px  •  DRAG TO MOVE", _cfg.horizontal_offset, _cfg.top_margin);
            using (var tb = new SolidBrush(ColTextMuted))
                g.DrawString(posTxt, _fMonoTiny, tb, new RectangleF(padRc.X + 8f, padRc.Bottom - 17f, padRc.Width - 16f, 15f), _sfLeft);
        }
        // =====================================================================
        // ZONE 1 (LIVE STAGE) + ZONE 2 (MODE RAIL) + ZONE 3 (INSTRUMENT DECK)
        // =====================================================================
        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            Graphics g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.TextRenderingHint = TextRenderingHint.ClearTypeGridFit;

            _hits.Clear();
            _sliderHits.Clear();
            _spatialPadRect = RectangleF.Empty;

            using (var bgBr = new SolidBrush(ColChassis))
                g.FillRectangle(bgBr, ClientRectangle);

            // -----------------------------------------------------------------
            // TOP CONSOLE HEADER BAR (y = 0..34)
            // -----------------------------------------------------------------
            using (var barBr = new SolidBrush(Color.FromArgb(14, 14, 17)))
            using (var barPen = new Pen(ColBorder, 1f))
            {
                g.FillRectangle(barBr, 0, 0, Width, 34);
                g.DrawLine(barPen, 0, 34, Width, 34);
            }

            using (var accBr = new SolidBrush(ColAccent))
            using (var titleBr = new SolidBrush(ColTextMain))
            using (var dimBr = new SolidBrush(ColTextDim))
            {
                g.FillRectangle(accBr, 14, 11, 8, 12);
                g.DrawString("DYNAMIC ISLAND // KINETIC STAGE & DECK", _fConsoleBrand, titleBr, new RectangleF(28, 0, 330, 34), _sfLeft);
                g.DrawString("MOTION LAB v3.0  •  CLICK VALUE = KEYBOARD  •  WHEEL = ±1", _fMonoTiny, dimBr, new RectangleF(352, 0, 275, 34), _sfLeft);
            }

            DrawHardwareButton(g, new RectangleF(Width - 252, 6, 76, 22), "ВЫХОД APP", false, true, () => Application.Exit());
            DrawHardwareButton(g, new RectangleF(Width - 170, 6, 80, 22), "ПРИМЕНИТЬ", false, false, () => ApplyLive());
            DrawHardwareButton(g, new RectangleF(Width - 84, 6, 70, 22), "ГОТОВО ✕", true, false, () =>
            {
                CommitInlineInput(true);
                ApplyLive();
                DialogResult = DialogResult.OK;
                Close();
            });

            // -----------------------------------------------------------------
            // ZONE 1: INTERACTIVE STAGE (y = 40..260, ~38% of window height)
            // -----------------------------------------------------------------
            var stageRc = new RectangleF(14f, 40f, Width - 28f, 220f);
            PaintInteractiveStage(g, stageRc);

            // -----------------------------------------------------------------
            // ZONE 2: CENTRAL MODE RAIL (y = 266..302)
            // -----------------------------------------------------------------
            var railRc = new RectangleF(14f, 266f, Width - 28f, 36f);
            PaintModeRail(g, railRc);

            // -----------------------------------------------------------------
            // ZONE 3: ASYMMETRIC BENTO INSTRUMENT DECK (y = 308..628)
            // -----------------------------------------------------------------
            if (_activeTab == 0) PaintDeck01PhysicsAndShape(g);
            else if (_activeTab == 1) PaintDeck02ModulesAndAudio(g);
            else if (_activeTab == 2) PaintDeck03ScreenAndModes(g);
            else PaintDeck04SkinAndSystem(g);

            // Crisp 1px Chassis Outer Frame
            using (GraphicsPath framePath = SharpRoundRect(new RectangleF(0.5f, 0.5f, Width - 1f, Height - 1f), 9f))
            using (var framePen = new Pen(ColBorderHi, 1f))
                g.DrawPath(framePen, framePath);
        }

        private void PaintInteractiveStage(Graphics g, RectangleF stageRc)
        {
            using (GraphicsPath sp = SharpRoundRect(stageRc, 6f))
            using (var sb = new SolidBrush(ColStageBg))
            using (var sPen = new Pen(ColBorder, 1f))
            {
                g.FillPath(sb, sp);
                g.DrawPath(sPen, sp);
            }

            Region prevClip = g.Clip;
            using (GraphicsPath clipPath = SharpRoundRect(stageRc, 6f))
            {
                g.SetClip(clipPath);

                // 1) Dot-Matrix Engineering Grid (16px pitch)
                using (var dotBr = new SolidBrush(Color.FromArgb(20, 255, 255, 255)))
                {
                    for (float gx = stageRc.X + 14f; gx < stageRc.Right - 8f; gx += 16f)
                    {
                        for (float gy = stageRc.Y + 12f; gy < stageRc.Bottom - 34f; gy += 16f)
                        {
                            g.FillRectangle(dotBr, gx, gy, 1.3f, 1.3f);
                        }
                    }
                }

                // 2) Top Screen Bezel Axis & Vertical Center Axis
                float bezelY = stageRc.Y + 22f;
                float centerX = stageRc.X + stageRc.Width * 0.5f;
                using (var axisPen = new Pen(Color.FromArgb(26, 255, 255, 255), 1f) { DashStyle = DashStyle.Dot })
                using (var bezelPen = new Pen(Color.FromArgb(60, 255, 255, 255), 1f))
                using (var axisLblBr = new SolidBrush(ColTextDim))
                {
                    g.DrawLine(bezelPen, stageRc.X + 12f, bezelY, stageRc.Right - 12f, bezelY);
                    g.DrawLine(axisPen, centerX, bezelY - 8f, centerX, stageRc.Bottom - 36f);
                    g.DrawString("SCREEN TOP EDGE [Y=0]", _fMonoTiny, axisLblBr, new RectangleF(stageRc.X + 14f, stageRc.Y + 5f, 160f, 14f), _sfLeft);
                    g.DrawString(string.Format("LIVE STAGE  •  DX:{0:+#;-#;0}PX  DY:+{1}PX  R:{2}PX", _cfg.horizontal_offset, _cfg.top_margin, _cfg.corner_radius),
                        _fMonoTiny, axisLblBr, new RectangleF(stageRc.Right - 310f, stageRc.Y + 5f, 296f, 14f), _sfRight);
                }

                // 3) Scale & Position the Pure #000000 Dynamic Island Capsule on Stage
                float maxStagePillW = stageRc.Width - 150f;
                float maxStagePillH = 142f;
                float fitScale = Math.Min(1.0f, Math.Min(maxStagePillW / Math.Max(1f, _stageCurW), maxStagePillH / Math.Max(1f, _stageCurH)));
                float drawW = _stageCurW * fitScale;
                float drawH = _stageCurH * fitScale;

                float stageOffsetX = Math.Max(-140f, Math.Min(140f, _cfg.horizontal_offset * 0.24f));
                float stageMarginY = Math.Max(2f, Math.Min(32f, _cfg.top_margin * 0.30f));
                var pillRc = new RectangleF(centerX - drawW * 0.5f + stageOffsetX, bezelY + stageMarginY, drawW, drawH);
                _stageIslandHitRect = pillRc;

                float compactRad = pillRc.Height * 0.5f;
                float expRad = Math.Max(10f, _cfg.corner_radius * fitScale);
                float rad = compactRad * (1f - _stageCurExp) + expRad * _stageCurExp;

                Color tc1, tc2;
                GetPreviewThemeColors(255, out tc1, out tc2);

                // Dimension Engineering Callouts around Capsule
                using (var dimPen = new Pen(Color.FromArgb(55, 255, 85, 0), 1f))
                using (var dimTxt = new SolidBrush(ColAccent))
                {
                    float callY = Math.Min(stageRc.Bottom - 46f, pillRc.Bottom + 7f);
                    g.DrawLine(dimPen, pillRc.Left, callY, pillRc.Right, callY);
                    g.DrawLine(dimPen, pillRc.Left, callY - 3f, pillRc.Left, callY + 3f);
                    g.DrawLine(dimPen, pillRc.Right, callY - 3f, pillRc.Right, callY + 3f);
                    string dimLabel = string.Format("{0:0} × {1:0} px", _stageCurW, _stageCurH);
                    g.DrawString(dimLabel, _fMonoTiny, dimTxt, new RectangleF(pillRc.Right + 6f, pillRc.Y + (pillRc.Height - 14f) * 0.5f, 92f, 14f), _sfLeft);
                }

                // Pure #000000 Island Body + Custom GIF/Photo Preview + 1px Specular Highlight
                _stageBgPreview.EnsureLoaded(_cfg.custom_bg_path);
                bool showBgOnStage = _stageBgPreview.HasImage &&
                    !((_cfg.custom_bg_scope ?? "").Contains("компакт") && _stageCurExp > 0.5f) &&
                    !((_cfg.custom_bg_scope ?? "").Contains("раскрыт") && _stageCurExp < 0.5f);

                using (GraphicsPath capPath = SharpRoundRect(pillRc, rad))
                {
                    using (var pureBlk = new SolidBrush(ColPureIsland))
                        g.FillPath(pureBlk, capPath);

                    if (showBgOnStage)
                    {
                        _stageBgPreview.DrawBackgroundIntoCapsule(g, capPath, pillRc, _cfg.custom_bg_fit_mode, _cfg.custom_bg_dim_percent, _stageAnimating);
                    }

                    // Render live simulated modules inside #000000 Stage Capsule
                    PaintStageIslandContents(g, pillRc, tc1, tc2);

                    int rimAlpha = Math.Max(48, Math.Min(235, 65 + _cfg.border_glow));
                    using (var rimGrad = new LinearGradientBrush(
                        new PointF(pillRc.Left, pillRc.Top),
                        new PointF(pillRc.Right, pillRc.Bottom),
                        Color.FromArgb(rimAlpha, 255, 255, 255),
                        Color.FromArgb(Math.Max(35, rimAlpha / 2), tc1.R, tc1.G, tc1.B)))
                    using (var rimPen = new Pen(rimGrad, 1.1f))
                    {
                        g.DrawPath(rimPen, capPath);
                    }
                }

                g.Clip = prevClip;
            }

            // Clicking the Stage Island toggles Compact <-> Expanded spring preview
            _hits.Add(new HitZone
            {
                Rect = _stageIslandHitRect,
                OnClick = () =>
                {
                    _stageMode = (_stageMode == StagePreviewMode.Expanded) ? StagePreviewMode.Compact : StagePreviewMode.Expanded;
                    SyncStageTargetsFromMode(true);
                }
            });

            // 4) Stage Bottom Control Bar: [IDLE] [COMPACT] [EXPANDED] [MEDIA] [NOTIFY] ... [⚡ TEST PULSE]
            float ctrlY = stageRc.Bottom - 30f;
            using (var stripPen = new Pen(ColBorder, 1f))
                g.DrawLine(stripPen, stageRc.X + 1f, ctrlY - 4f, stageRc.Right - 1f, ctrlY - 4f);

            string[] modeNames = { "IDLE", "COMPACT", "EXPANDED", "MEDIA", "NOTIFY" };
            float mx = stageRc.X + 10f;
            for (int i = 0; i < modeNames.Length; i++)
            {
                StagePreviewMode m = (StagePreviewMode)i;
                bool act = (_stageMode == m);
                var mRc = new RectangleF(mx, ctrlY, 76f, 22f);
                bool mHov = mRc.Contains(_mousePt);

                using (GraphicsPath mp = SharpRoundRect(mRc, 3.5f))
                using (var mb = new SolidBrush(act ? Color.FromArgb(42, 255, 85, 0) : (mHov ? ColDeckHover : ColDeckCard)))
                using (var mPen = new Pen(act ? ColAccent : ColBorder, 1f))
                using (var mt = new SolidBrush(act ? ColTextMain : ColTextMuted))
                {
                    g.FillPath(mb, mp);
                    g.DrawPath(mPen, mp);
                    g.DrawString(modeNames[i], _fMonoTiny, mt, mRc, _sfCenter);
                }

                StagePreviewMode capturedMode = m;
                _hits.Add(new HitZone
                {
                    Rect = mRc,
                    OnClick = () =>
                    {
                        _stageMode = capturedMode;
                        SyncStageTargetsFromMode(true);
                    }
                });
                mx += 81f;
            }

            DrawHardwareButton(g, new RectangleF(stageRc.Right - 128f, ctrlY, 118f, 22f), "⚡ TEST PULSE", true, false, TriggerTestPulse);
        }

        private void PaintStageIslandContents(Graphics g, RectangleF pillRc, Color tc1, Color tc2)
        {
            if (_stageCurExp > 0.55f)
            {
                // Expanded Dashboard Mini-Preview inside Stage Capsule
                float pad = 10f;
                var mediaBox = new RectangleF(pillRc.X + pad, pillRc.Y + 8f, pillRc.Width - pad * 2f, Math.Min(42f, pillRc.Height * 0.34f));
                using (GraphicsPath mp = SharpRoundRect(mediaBox, 8f))
                using (var mb = new SolidBrush(Color.FromArgb(26, 255, 255, 255)))
                {
                    g.FillPath(mb, mp);
                }

                var covRc = new RectangleF(mediaBox.X + 6f, mediaBox.Y + 5f, mediaBox.Height - 10f, mediaBox.Height - 10f);
                using (GraphicsPath cp = SharpRoundRect(covRc, 5f))
                using (var cg = new LinearGradientBrush(covRc, tc1, tc2, 45f))
                    g.FillPath(cg, cp);

                using (var wBr = new SolidBrush(ColTextMain))
                using (var mBr = new SolidBrush(ColTextMuted))
                {
                    g.DrawString("DYNAMIC STUDIO • LIVE", _fMonoTiny, wBr, new RectangleF(covRc.Right + 8f, mediaBox.Y + 5f, mediaBox.Width - 95f, 14f), _sfLeft);
                    g.DrawString(_cfg.theme_preset ?? "Dinasmic Neon", _fSub, mBr, new RectangleF(covRc.Right + 8f, mediaBox.Y + 19f, mediaBox.Width - 95f, 14f), _sfLeft);
                }

                // Dual Volume + Dock preview row
                float row2Y = mediaBox.Bottom + 6f;
                if (row2Y + 18f < pillRc.Bottom - 8f)
                {
                    float halfW = (mediaBox.Width - 6f) * 0.5f;
                    var v1 = new RectangleF(mediaBox.X, row2Y, halfW, 15f);
                    var v2 = new RectangleF(mediaBox.X + halfW + 6f, row2Y, halfW, 15f);
                    using (var vb = new SolidBrush(Color.FromArgb(20, 255, 255, 255)))
                    using (var af = new SolidBrush(tc1))
                    {
                        g.FillRectangle(vb, v1);
                        g.FillRectangle(vb, v2);
                        g.FillRectangle(af, v1.X + 4f, v1.Y + 6f, v1.Width * 0.68f, 3f);
                        g.FillRectangle(af, v2.X + 4f, v2.Y + 6f, v2.Width * 0.84f, 3f);
                    }
                }

                // Telemetry / Dropzone footer if enabled
                if ((_cfg.show_dropzone || _cfg.show_telemetry) && pillRc.Height > 92f)
                {
                    using (var tBr = new SolidBrush(ColTextMuted))
                    {
                        string foot = (_cfg.show_dropzone ? "[DROPZONE]  " : "") + (_cfg.show_telemetry ? "CPU 14% • RAM 42% • ↓12.4 MB/s" : "");
                        g.DrawString(foot, _fMonoTiny, tBr, new RectangleF(pillRc.X + 10f, pillRc.Bottom - 20f, pillRc.Width - 20f, 14f), _sfCenter);
                    }
                }
                return;
            }

            if (_stageMode == StagePreviewMode.Notify)
            {
                using (var dotBr = new SolidBrush(ColAccent))
                using (var txtBr = new SolidBrush(ColTextMain))
                {
                    g.FillEllipse(dotBr, pillRc.X + 12f, pillRc.Y + (pillRc.Height - 8f) / 2f, 8f, 8f);
                    g.DrawString("🔔 ТАЙМЕР " + _cfg.pomodoro_minutes + ":00  •  LIVE ACTIVITY", _fMonoTiny, txtBr,
                        new RectangleF(pillRc.X + 26f, pillRc.Y, pillRc.Width - 34f, pillRc.Height), _sfLeft);
                }
                return;
            }

            // Compact / Idle / Media Stage Preview
            float leftCur = pillRc.X + 10f;
            if (_cfg.show_clock)
            {
                using (var clkBr = new SolidBrush(ColTextMain))
                    g.DrawString(DateTime.Now.ToString("HH:mm"), _fMonoTiny, clkBr, new RectangleF(leftCur, pillRc.Y, 38f, pillRc.Height), _sfLeft);
                leftCur += 40f;
            }

            if (_cfg.enable_mic_indicator)
            {
                using (var micBr = new SolidBrush(Color.FromArgb(48, 209, 88)))
                    g.FillEllipse(micBr, leftCur, pillRc.Y + (pillRc.Height - 6f) / 2f, 6f, 6f);
                leftCur += 12f;
            }

            if ((_stageMode == StagePreviewMode.Compact || _stageMode == StagePreviewMode.Media) && _cfg.show_compact_cover && pillRc.Width > 170f)
            {
                float cSz = Math.Min(18f, pillRc.Height - 8f);
                var cov = new RectangleF(leftCur + 2f, pillRc.Y + (pillRc.Height - cSz) / 2f, cSz, cSz);
                using (GraphicsPath cp = SharpRoundRect(cov, 4f))
                using (var cg = new LinearGradientBrush(cov, tc1, tc2, 45f))
                    g.FillPath(cg, cp);

                using (var tBr = new SolidBrush(ColTextMain))
                    g.DrawString(_stageMode == StagePreviewMode.Media ? "KINETIC AUDIO • 60HZ" : "YANDEX / SPOTIFY",
                        _fMonoTiny, tBr, new RectangleF(cov.Right + 6f, pillRc.Y, pillRc.Width - (cov.Right - pillRc.X) - 52f, pillRc.Height), _sfLeft);
            }
            else if (_cfg.show_battery && _stageMode == StagePreviewMode.Idle)
            {
                using (var batBr = new SolidBrush(ColLime))
                    g.DrawString("98%", _fMonoTiny, batBr, new RectangleF(leftCur + 2f, pillRc.Y, 34f, pillRc.Height), _sfLeft);
            }

            // Live Equalizer Bars on right of Compact Stage Capsule
            if (_cfg.eq_style != "Выкл" && _cfg.eq_style != "Off")
            {
                int bars = (_cfg.eq_style != null && _cfg.eq_style.Contains("7")) ? 7 : 5;
                float bw = 2.4f, gap = 2.2f;
                float totalW = bars * bw + (bars - 1) * gap;
                float sx = pillRc.Right - 11f - totalW;
                float cy = pillRc.Y + pillRc.Height * 0.5f;
                float maxH = Math.Min(16f, pillRc.Height - 10f);

                using (var b1 = new SolidBrush(tc1))
                using (var b2 = new SolidBrush(tc2))
                {
                    for (int i = 0; i < bars; i++)
                    {
                        float amp = (_stageMode == StagePreviewMode.Idle)
                            ? 0.25f
                            : (0.35f + 0.62f * Math.Abs((float)Math.Sin(_stageWavePhase + i * 0.85f)));
                        float bh = Math.Max(3f, maxH * amp);
                        g.FillRectangle((i % 2 == 0) ? b1 : b2, sx + i * (bw + gap), cy - bh * 0.5f, bw, bh);
                    }
                }
            }
        }

        private void PaintModeRail(Graphics g, RectangleF railRc)
        {
            using (GraphicsPath rp = SharpRoundRect(railRc, 5f))
            using (var rb = new SolidBrush(ColDeckCard))
            using (var rPen = new Pen(ColBorder, 1f))
            {
                g.FillPath(rb, rp);
                g.DrawPath(rPen, rp);
            }

            float bx = railRc.X + 4f;
            float by = railRc.Y + 4f;
            float bh = railRc.Height - 8f;
            float tabW = 148f;

            for (int i = 0; i < _railTitles.Length; i++)
            {
                var tRc = new RectangleF(bx, by, tabW, bh);
                bool active = (_activeTab == i);
                bool hov = tRc.Contains(_mousePt);

                using (GraphicsPath tp = SharpRoundRect(tRc, 3.5f))
                using (var tb = new SolidBrush(active ? Color.FromArgb(34, 255, 85, 0) : (hov ? ColDeckHover : Color.Transparent)))
                {
                    g.FillPath(tb, tp);
                    if (active)
                    {
                        using (var aPen = new Pen(ColAccent, 1f))
                            g.DrawPath(aPen, tp);
                    }
                }

                using (var cBr = new SolidBrush(active ? ColAccent : ColTextDim))
                using (var nBr = new SolidBrush(active ? ColTextMain : ColTextMuted))
                {
                    g.DrawString(_railCodes[i], _fMonoTiny, cBr, new RectangleF(tRc.X + 7f, tRc.Y, 22f, bh), _sfLeft);
                    g.DrawString(_railTitles[i], _fChip, nBr, new RectangleF(tRc.X + 27f, tRc.Y, tRc.Width - 30f, bh), _sfLeft);
                }

                int capturedIdx = i;
                _hits.Add(new HitZone { Rect = tRc, OnClick = () => { CommitInlineInput(true); _activeTab = capturedIdx; Invalidate(); } });
                bx += tabW + 4f;
            }

            // Right Side of Mode Rail: [ ⌕ ФИЛЬТР ] + [ ● SYNCED ] + [ ↺ СБРОС ]
            var searchRc = new RectangleF(railRc.Right - 242f, by, 98f, bh);
            bool sHov = searchRc.Contains(_mousePt) || _editingFilter;
            using (GraphicsPath sp = SharpRoundRect(searchRc, 3.5f))
            using (var sb = new SolidBrush(ColDeckInset))
            using (var sPen = new Pen((!string.IsNullOrEmpty(_filterQuery) || sHov) ? ColAccent : ColBorder, 1f))
            using (var st = new SolidBrush(string.IsNullOrEmpty(_filterQuery) ? ColTextMuted : ColAccent))
            {
                g.FillPath(sb, sp);
                g.DrawPath(sPen, sp);
                string fTxt = string.IsNullOrEmpty(_filterQuery) ? "⌕ ПОИСК..." : ("⌕ " + _filterQuery);
                if (!_editingFilter)
                    g.DrawString(fTxt, _fMonoTiny, st, new RectangleF(searchRc.X + 6f, searchRc.Y, searchRc.Width - 10f, bh), _sfLeft);
            }
            _hits.Add(new HitZone { Rect = searchRc, OnClick = () => BeginInlineFilterEdit(searchRc) });

            bool justSaved = (Environment.TickCount - _lastModifiedTick) < 1200 && _lastModifiedTick != 0;
            var statusRc = new RectangleF(railRc.Right - 138f, by, 72f, bh);
            using (var ledBr = new SolidBrush(justSaved ? ColAccent : ColLime))
            using (var stBr = new SolidBrush(justSaved ? ColAccent : ColTextMuted))
            {
                g.FillEllipse(ledBr, statusRc.X + 4f, statusRc.Y + (bh - 6f) / 2f, 6f, 6f);
                g.DrawString(justSaved ? "SAVING" : "SYNCED", _fMonoTiny, stBr, new RectangleF(statusRc.X + 14f, statusRc.Y, 58f, bh), _sfLeft);
            }

            DrawHardwareButton(g, new RectangleF(railRc.Right - 62f, by, 58f, bh), "↺ СБРОС", false, false, () =>
            {
                CommitInlineInput(false);
                _filterQuery = "";
                _cfg.top_margin = 8; _cfg.horizontal_offset = 0;
                _cfg.idle_width = 310; _cfg.idle_height = 36;
                _cfg.expanded_width = 520; _cfg.expanded_height = 224;
                _cfg.ui_scale_percent = 100; _cfg.corner_radius = 30;
                _cfg.anim_duration_ms = 330; _cfg.spring_overshoot_pct = 112;
                ApplyLive();
            });
        }
        private void PaintDeck01PhysicsAndShape(Graphics g)
        {
            var leftCell = new RectangleF(14f, 308f, 488f, 318f);
            DrawBentoCell(g, leftCell, "01.A", "ГЕОМЕТРИЯ И ПРОПОРЦИИ КАПСУЛЫ", "PRECISION RULERS");

            float gx = leftCell.X + 10f;
            float gy = leftCell.Y + 32f;
            float colW = (leftCell.Width - 28f) * 0.5f;
            float rowH = 84f;
            float gapY = 8f;

            DrawPrecisionTickSlider(g, new RectangleF(gx, gy, colW, rowH),
                "ШИРИНА КОМПАКТ", _cfg.idle_width, 160, 480, _cfg.idle_width + "px", 16,
                () => _cfg.idle_width, v => { _cfg.idle_width = v; _stageMode = StagePreviewMode.Compact; });

            DrawPrecisionTickSlider(g, new RectangleF(gx + colW + 8f, gy, colW, rowH),
                "ВЫСОТА КОМПАКТ", _cfg.idle_height, 26, 68, _cfg.idle_height + "px", 14,
                () => _cfg.idle_height, v => { _cfg.idle_height = v; _stageMode = StagePreviewMode.Compact; });

            gy += rowH + gapY;
            DrawPrecisionTickSlider(g, new RectangleF(gx, gy, colW, rowH),
                "ШИРИНА РАСКРЫТ.", _cfg.expanded_width, 360, 860, _cfg.expanded_width + "px", 16,
                () => _cfg.expanded_width, v => { _cfg.expanded_width = v; _stageMode = StagePreviewMode.Expanded; });

            DrawPrecisionTickSlider(g, new RectangleF(gx + colW + 8f, gy, colW, rowH),
                "ВЫСОТА РАСКРЫТ.", _cfg.expanded_height, 165, 430, _cfg.expanded_height + "px", 14,
                () => _cfg.expanded_height, v => { _cfg.expanded_height = v; _stageMode = StagePreviewMode.Expanded; });

            gy += rowH + gapY;
            DrawPrecisionTickSlider(g, new RectangleF(gx, gy, colW, rowH),
                "РАДИУС СКРУГЛЕНИЯ", _cfg.corner_radius, 12, 48, _cfg.corner_radius + "px", 12,
                () => _cfg.corner_radius, v => _cfg.corner_radius = v);

            DrawPrecisionTickSlider(g, new RectangleF(gx + colW + 8f, gy, colW, rowH),
                "МАСШТАБ UI", _cfg.ui_scale_percent, 80, 150, _cfg.ui_scale_percent + "%", 14,
                () => _cfg.ui_scale_percent, v => _cfg.ui_scale_percent = v);

            var springCell = new RectangleF(510f, 308f, 356f, 214f);
            DrawBentoCell(g, springCell, "01.B", "ОСЦИЛЛОГРАФ ПРУЖИНЫ", "DAMPED SPRING");

            DrawLiveSpringOscilloscope(g, new RectangleF(springCell.X + 10f, springCell.Y + 30f, springCell.Width - 20f, 56f));

            float sy = springCell.Y + 92f;
            float sColW = (springCell.Width - 26f) * 0.5f;
            DrawPrecisionTickSlider(g, new RectangleF(springCell.X + 10f, sy, sColW, 54f),
                "СКОРОСТЬ", _cfg.anim_duration_ms, 120, 800, string.Format("{0:0.00}s", _cfg.anim_duration_ms / 1000.0), 10,
                () => _cfg.anim_duration_ms, v => _cfg.anim_duration_ms = v);

            DrawPrecisionTickSlider(g, new RectangleF(springCell.X + 16f + sColW, sy, sColW, 54f),
                "УПРУГОСТЬ", _cfg.spring_overshoot_pct, 75, 180, string.Format("{0:0.00}x", _cfg.spring_overshoot_pct / 100.0), 10,
                () => _cfg.spring_overshoot_pct, v => _cfg.spring_overshoot_pct = v);

            DrawPrecisionTickSlider(g, new RectangleF(springCell.X + 10f, sy + 60f, springCell.Width - 20f, 52f),
                "ЗАДЕРЖКА СВОРАЧИВАНИЯ ПОСЛЕ УХОДА КУРСОРА", _cfg.collapse_delay_ms, 100, 3000, _cfg.collapse_delay_ms + "ms", 16,
                () => _cfg.collapse_delay_ms, v => _cfg.collapse_delay_ms = v);

            var trigCell = new RectangleF(510f, 530f, 356f, 96f);
            DrawBentoCell(g, trigCell, "01.C", "ТАКТИЛЬНЫЙ ОТКЛИК", "INPUT TRIGGERS");

            DrawSegmentedToggle(g, new RectangleF(trigCell.X + 10f, trigCell.Y + 30f, trigCell.Width - 20f, 28f),
                "РАСКРЫВАТЬ ПРИ НАВЕДЕНИИ (HOVER)", _cfg.expand_on_hover, v => _cfg.expand_on_hover = v);
            DrawSegmentedToggle(g, new RectangleF(trigCell.X + 10f, trigCell.Y + 62f, trigCell.Width - 20f, 28f),
                "СВОБОДНЫЙ РЕСАЙЗ LCTRL + МЫШЬ", _cfg.enable_lctrl_resize, v => _cfg.enable_lctrl_resize = v);
        }

        private void PaintDeck02ModulesAndAudio(Graphics g)
        {
            var slotBay = new RectangleF(14f, 308f, 514f, 318f);
            DrawBentoCell(g, slotBay, "02.A", "СЛОТЫ МОДУЛЕЙ И ВИДЖЕТОВ", "CLICK = LATCH  •  ⚙ = TUNE");

            float cx = slotBay.X + 10f;
            float cy = slotBay.Y + 32f;
            float cW = (slotBay.Width - 28f) * 0.5f;
            float cH = 63f;
            float gapY = 6f;

            DrawModuleCartridgeSlot(g, new RectangleF(cx, cy, cW, cH),
                "MOD-01", "МЕДИА В КАПСУЛЕ", _cfg.show_compact_cover ? ("АКТИВЕН • EQ " + (_cfg.eq_style ?? "5 Полос")) : "ОТКЛЮЧЕН",
                _cfg.show_compact_cover, true, _inspectorSubMode == 1,
                () => _cfg.show_compact_cover = !_cfg.show_compact_cover, () => _inspectorSubMode = 1);

            DrawModuleCartridgeSlot(g, new RectangleF(cx + cW + 8f, cy, cW, cH),
                "MOD-02", "ТАЙМЕР ПОМОДОРО", _cfg.pomodoro_minutes + " МИН • " + (_cfg.timer_sound_preset ?? "Apple Radar"),
                _cfg.timer_sound_preset != "Без звука", true, _inspectorSubMode == 0,
                () => _inspectorSubMode = 0, () => _inspectorSubMode = 0);

            cy += cH + gapY;
            DrawModuleCartridgeSlot(g, new RectangleF(cx, cy, cW, cH),
                "MOD-03", "LIVE УВЕДОМЛЕНИЯ", _cfg.enable_live_activities ? "ВСПЛЫВАЮЩИЕ БАННЕРЫ" : "БЕЗ БАННЕРОВ",
                _cfg.enable_live_activities, false, false,
                () => _cfg.enable_live_activities = !_cfg.enable_live_activities, null);

            DrawModuleCartridgeSlot(g, new RectangleF(cx + cW + 8f, cy, cW, cH),
                "MOD-04", "ЧАСЫ КАПСУЛЫ", _cfg.show_clock ? "СЛЕВА • ФОРМАТ 24Ч" : "СКРЫТЫ",
                _cfg.show_clock, false, false,
                () => _cfg.show_clock = !_cfg.show_clock, null);

            cy += cH + gapY;
            DrawModuleCartridgeSlot(g, new RectangleF(cx, cy, cW, cH),
                "MOD-05", "ЗАРЯД БАТАРЕИ", _cfg.show_battery ? "МОНИТОРИНГ ПИТАНИЯ" : "СКРЫТ",
                _cfg.show_battery, false, false,
                () => _cfg.show_battery = !_cfg.show_battery, null);

            DrawModuleCartridgeSlot(g, new RectangleF(cx + cW + 8f, cy, cW, cH),
                "MOD-06", "МИКРОФОН COREAUDIO", _cfg.enable_mic_indicator ? "LED + БЫСТРЫЙ МЬЮТ" : "ВЫКЛЮЧЕН",
                _cfg.enable_mic_indicator, false, false,
                () => _cfg.enable_mic_indicator = !_cfg.enable_mic_indicator, null);

            cy += cH + gapY;
            DrawModuleCartridgeSlot(g, new RectangleF(cx, cy, cW, cH),
                "MOD-07", "ПОЛКА DROPZONE", _cfg.show_dropzone ? "БУФЕР ФАЙЛОВ DRAG&DROP" : "ВЫКЛЮЧЕНА",
                _cfg.show_dropzone, false, false,
                () => _cfg.show_dropzone = !_cfg.show_dropzone, null);

            DrawModuleCartridgeSlot(g, new RectangleF(cx + cW + 8f, cy, cW, cH),
                "MOD-08", "ТЕЛЕМЕТРИЯ & VAULT", _cfg.show_telemetry ? ("CPU/RAM + " + (_cfg.watchdog_action ?? "Сон")) : "СКРЫТА",
                _cfg.show_telemetry, true, _inspectorSubMode == 2,
                () => _cfg.show_telemetry = !_cfg.show_telemetry, () => _inspectorSubMode = 2);

            var insp = new RectangleF(536f, 308f, 330f, 318f);
            string[] subTabs = { "ТАЙМЕР/ЗВУК", "ПЛЕЕР/EQ", "СКРИНЫ/СОН" };
            DrawBentoCell(g, insp, "02.B", "ИНСПЕКТОР МОДУЛЯ", "SLOT TUNER");

            float sx = insp.X + 10f;
            float sy = insp.Y + 30f;
            float stW = (insp.Width - 28f) / 3f;
            for (int i = 0; i < 3; i++)
            {
                var stRc = new RectangleF(sx + i * (stW + 4f), sy, stW, 22f);
                bool act = (_inspectorSubMode == i);
                using (GraphicsPath sp = SharpRoundRect(stRc, 3.5f))
                using (var sb = new SolidBrush(act ? Color.FromArgb(38, 255, 85, 0) : ColDeckInset))
                using (var sPen = new Pen(act ? ColAccent : ColBorder, 1f))
                using (var st = new SolidBrush(act ? ColTextMain : ColTextMuted))
                {
                    g.FillPath(sb, sp);
                    g.DrawPath(sPen, sp);
                    g.DrawString(subTabs[i], _fMonoTiny, st, stRc, _sfCenter);
                }
                int capI = i;
                _hits.Add(new HitZone { Rect = stRc, OnClick = () => { CommitInlineInput(true); _inspectorSubMode = capI; Invalidate(); } });
            }

            float iy = sy + 30f;
            float iw = insp.Width - 20f;

            if (_inspectorSubMode == 0)
            {
                DrawPrecisionTickSlider(g, new RectangleF(sx, iy, iw, 54f),
                    "ФОКУС-ТАЙМЕР", _cfg.pomodoro_minutes, 1, 240, _cfg.pomodoro_minutes + " мин", 12,
                    () => _cfg.pomodoro_minutes, v => _cfg.pomodoro_minutes = v);
                iy += 62f;

                DrawChipSelectorMatrix(g, new RectangleF(sx, iy, iw, 102f),
                    "МЕЛОДИЯ ОКОНЧАНИЯ ТАЙМЕРА (CHIP MATRIX)",
                    TimerSoundEngine.Presets,
                    new[] { "Apple Radar", "Crystal", "Digital", "Zen Bell", "Свой звук", "Без звука" },
                    _cfg.timer_sound_preset ?? "Apple Radar", 2,
                    val => { _cfg.timer_sound_preset = val; TimerSoundEngine.PlayAlert(_cfg); });
                iy += 108f;

                DrawHardwareButton(g, new RectangleF(sx, iy, iw * 0.56f - 3f, 24f), "▶ ТЕСТ МЕЛОДИИ", true, false,
                    () => TimerSoundEngine.PlayAlert(_cfg));
                DrawHardwareButton(g, new RectangleF(sx + iw * 0.56f + 3f, iy, iw * 0.44f - 3f, 24f), "■ СТОП ЗВУК", false, false,
                    () => TimerSoundEngine.StopAlert());
                iy += 30f;

                string sndFile = string.IsNullOrEmpty(_cfg.timer_custom_sound_path)
                    ? "СВОЙ MP3/WAV: НЕ ВЫБРАН"
                    : ("ФАЙЛ: " + Path.GetFileName(_cfg.timer_custom_sound_path));
                using (var fb = new SolidBrush(ColTextMuted))
                    g.DrawString(sndFile, _fMonoTiny, fb, new RectangleF(sx, iy, iw, 15f), _sfLeft);
                iy += 17f;

                DrawHardwareButton(g, new RectangleF(sx, iy, iw * 0.65f - 3f, 24f), "ВЫБРАТЬ АУДИОФАЙЛ...", false, false, () =>
                {
                    using (var ofd = new OpenFileDialog())
                    {
                        ofd.Title = "Выберите звуковой файл для таймера Dynamic Island";
                        ofd.Filter = "Аудиофайлы (*.mp3;*.wav;*.m4a;*.wma;*.ogg)|*.mp3;*.wav;*.m4a;*.wma;*.ogg|Все файлы (*.*)|*.*";
                        if (ofd.ShowDialog(this) == DialogResult.OK)
                        {
                            _cfg.timer_custom_sound_path = ofd.FileName;
                            _cfg.timer_sound_preset = "Свой звук";
                            ApplyLive();
                            TimerSoundEngine.PlayAlert(_cfg);
                        }
                    }
                });
                DrawHardwareButton(g, new RectangleF(sx + iw * 0.65f + 3f, iy, iw * 0.35f - 3f, 24f), "СБРОС", false, true, () =>
                {
                    TimerSoundEngine.StopAlert();
                    _cfg.timer_custom_sound_path = "";
                    if (_cfg.timer_sound_preset == "Свой звук") _cfg.timer_sound_preset = "Apple Radar";
                    ApplyLive();
                });
            }
            else if (_inspectorSubMode == 1)
            {
                DrawSegmentedToggle(g, new RectangleF(sx, iy, iw, 34f),
                    "ОБЛОЖКА ТРЕКА В КОМПАКТЕ", _cfg.show_compact_cover, v => _cfg.show_compact_cover = v);
                iy += 42f;
                DrawSegmentedToggle(g, new RectangleF(sx, iy, iw, 34f),
                    "ПРИОРИТЕТ ЯНДЕКС / SPOTIFY", _cfg.prefer_music_players, v => _cfg.prefer_music_players = v);
                iy += 42f;
                DrawSegmentedToggle(g, new RectangleF(sx, iy, iw, 34f),
                    "LIVE-БАННЕР СМЕНЫ ТРЕКА", _cfg.enable_live_activities, v => _cfg.enable_live_activities = v);
                iy += 46f;

                string[] eqVals = { "5 Полос", "7 Полос", "Выкл" };
                DrawChipSelectorMatrix(g, new RectangleF(sx, iy, iw, 52f),
                    "РЕЖИМ АУДИО-ЭКВАЛАЙЗЕРА", eqVals, eqVals, _cfg.eq_style ?? "5 Полос", 3,
                    val => _cfg.eq_style = val);
            }
            else
            {
                DrawSegmentedToggle(g, new RectangleF(sx, iy, iw, 32f),
                    "ПЕРЕХВАТ СКРИНШОТОВ СЕССИИ", _cfg.enable_screenshot_vault, v => _cfg.enable_screenshot_vault = v);
                iy += 38f;
                DrawSegmentedToggle(g, new RectangleF(sx, iy, iw, 32f),
                    "СТОП-ТАЙМЕР ЗАКАЧЕК (WATCHDOG)", _cfg.enable_download_watchdog, v => _cfg.enable_download_watchdog = v);
                iy += 40f;

                DrawPrecisionTickSlider(g, new RectangleF(sx, iy, iw, 54f),
                    "ПОРОГ АКТИВНОЙ ЗАКАЧКИ", _cfg.download_threshold_mbps, 1, 50, _cfg.download_threshold_mbps + " MB/s", 12,
                    () => _cfg.download_threshold_mbps, v => _cfg.download_threshold_mbps = v);
                iy += 62f;

                string[] wdVals = { "Сон", "Гибернация", "Выключение" };
                DrawChipSelectorMatrix(g, new RectangleF(sx, iy, iw, 50f),
                    "ДЕЙСТВИЕ ПОСЛЕ ОКОНЧАНИЯ ЗАГРУЗКИ", wdVals, wdVals, _cfg.watchdog_action ?? "Сон", 3,
                    val => _cfg.watchdog_action = val);
            }
        }
        private void PaintDeck03ScreenAndModes(Graphics g)
        {
            var padCell = new RectangleF(14f, 308f, 446f, 318f);
            DrawBentoCell(g, padCell, "03.A", "SPATIAL MONITOR PAD", "2D COORDINATE DECK");

            Screen[] screens = Screen.AllScreens;
            int monCount = Math.Max(1, screens.Length);
            string[] monVals = new string[monCount];
            string[] monLbls = new string[monCount];
            for (int i = 0; i < monCount; i++)
            {
                monVals[i] = i.ToString();
                monLbls[i] = string.Format("DISPLAY {0:00} ({1}×{2})", i + 1, screens[i].Bounds.Width, screens[i].Bounds.Height);
            }

            float px = padCell.X + 10f;
            float pw = padCell.Width - 20f;
            DrawChipSelectorMatrix(g, new RectangleF(px, padCell.Y + 30f, pw, 26f),
                "", monVals, monLbls, Math.Max(0, Math.Min(monCount - 1, _cfg.monitor_index)).ToString(),
                Math.Min(3, monCount),
                val => { int mi; if (int.TryParse(val, out mi)) _cfg.monitor_index = mi; });

            DrawSpatialScreenPad(g, new RectangleF(px, padCell.Y + 62f, pw, 152f));

            DrawHardwareButton(g, new RectangleF(padCell.Right - 118f, padCell.Y + 190f, 102f, 20f), "ЦЕНТР [0, +8]", false, false, () =>
            {
                _cfg.horizontal_offset = 0;
                _cfg.top_margin = 8;
                ApplyLive();
            });

            float halfW = (pw - 8f) * 0.5f;
            DrawPrecisionTickSlider(g, new RectangleF(px, padCell.Y + 222f, halfW, 86f),
                "СДВИГ ПО X", _cfg.horizontal_offset, -500, 500, string.Format("{0:+#;-#;0}px", _cfg.horizontal_offset), 16,
                () => _cfg.horizontal_offset, v => _cfg.horizontal_offset = v);

            DrawPrecisionTickSlider(g, new RectangleF(px + halfW + 8f, padCell.Y + 222f, halfW, 86f),
                "ОТСТУП СВЕРХУ Y", _cfg.top_margin, 0, 120, "+" + _cfg.top_margin + "px", 12,
                () => _cfg.top_margin, v => _cfg.top_margin = v);

            var modeCell = new RectangleF(468f, 308f, 398f, 318f);
            DrawBentoCell(g, modeCell, "03.B", "ИГРОВОЙ РЕЖИМ И РЕЖИМ ПРИЗРАКА", "D3D & GHOST");

            float mx = modeCell.X + 10f;
            float mw = modeCell.Width - 20f;
            float my = modeCell.Y + 32f;

            string[] fsVals = { "Неоновая телеметрия", "Авто-скрытие", "Только по клику" };
            string[] fsShort = { "Неон-телеметрия", "Авто-скрытие", "По клику" };
            DrawChipSelectorMatrix(g, new RectangleF(mx, my, mw, 48f),
                "ДЕТЕКТОР ПОЛНОЭКРАННЫХ ИГР (D3D FULLSCREEN)", fsVals, fsShort,
                _cfg.fullscreen_mode ?? "Неоновая телеметрия", 3,
                val => { _cfg.fullscreen_mode = val; _cfg.hide_in_fullscreen = (val == "Авто-скрытие"); });
            my += 56f;

            DrawSegmentedToggle(g, new RectangleF(mx, my, mw, 34f),
                "РЕЖИМ «ПРИЗРАКА» (20% OPACITY • ПРОПУСК КЛИКОВ)", _cfg.enable_ghost_mode, v => _cfg.enable_ghost_mode = v);
            my += 40f;

            DrawSegmentedToggle(g, new RectangleF(mx, my, mw, 34f),
                "ИНДИКАТОР И 1-CLICK МЬЮТ МИКРОФОНА", _cfg.enable_mic_indicator, v => _cfg.enable_mic_indicator = v);
            my += 40f;

            DrawSegmentedToggle(g, new RectangleF(mx, my, mw, 34f),
                "ПРИОРИТЕТ ЯНДЕКС МУЗЫКИ / SPOTIFY", _cfg.prefer_music_players, v => _cfg.prefer_music_players = v);
            my += 44f;

            var hkBox = new RectangleF(mx, my, mw, 94f);
            using (GraphicsPath hp = SharpRoundRect(hkBox, 5f))
            using (var hb = new SolidBrush(ColDeckInset))
            using (var hPen = new Pen(ColBorder, 1f))
            {
                g.FillPath(hb, hp);
                g.DrawPath(hPen, hp);
            }

            string[,] hotkeys =
            {
                { "ALT + SPACE", "ВЫХОД ИЗ ПРИЗРАКА / РАСКРЫТЬ" },
                { "LCTRL + ЛКМ", "РАСТЯГИВАНИЕ ОСТРОВА МЫШЬЮ" },
                { "LCTRL + КОЛЕСО", "МАСШТАБ ТЕКСТА И ИКОНОК" },
                { "LCTRL + СКМ", "МГНОВЕННЫЙ СБРОС РАЗМЕРОВ" }
            };
            float hkColW = (mw - 18f) * 0.5f;
            for (int i = 0; i < 4; i++)
            {
                int r = i / 2;
                int c = i % 2;
                float hx = mx + 8f + c * (hkColW + 4f);
                float hy = my + 8f + r * 40f;
                using (var kBr = new SolidBrush(ColAccent))
                using (var dBr = new SolidBrush(ColTextMuted))
                {
                    g.DrawString("[" + hotkeys[i, 0] + "]", _fMonoTiny, kBr, new RectangleF(hx, hy, hkColW, 15f), _sfLeft);
                    g.DrawString(hotkeys[i, 1], _fSub, dBr, new RectangleF(hx, hy + 15f, hkColW, 15f), _sfLeft);
                }
            }
        }

        private void PaintDeck04SkinAndSystem(Graphics g)
        {
            var palCell = new RectangleF(14f, 308f, 456f, 318f);
            DrawBentoCell(g, palCell, "04.A", "ПАЛИТРА АКЦЕНТОВ И ОПТИКА КАПСУЛЫ", "COLOR & OPTICS");

            float px = palCell.X + 10f;
            float pw = palCell.Width - 20f;
            float py = palCell.Y + 32f;

            string[] themes =
            {
                "Kinetic Orange", "Acid Lime", "Dinasmic Neon",
                "Apple Blue", "iOS Cyan-Purple", "Yandex Gold-Yellow",
                "Spotify Neon Green", "Crimson Sunset", "Minimal White"
            };
            string[] themeShort =
            {
                "Kinetic #FF5500", "Acid #C8FF00", "Dinasmic Neon",
                "Apple Blue", "Cyan-Purple", "Yandex Gold",
                "Spotify Green", "Crimson", "Minimal White"
            };
            Color[] swatches =
            {
                Color.FromArgb(255, 85, 0),
                Color.FromArgb(200, 255, 0),
                Color.FromArgb(34, 211, 238),
                Color.FromArgb(10, 132, 255),
                Color.FromArgb(168, 85, 247),
                Color.FromArgb(255, 214, 10),
                Color.FromArgb(48, 209, 88),
                Color.FromArgb(255, 55, 95),
                Color.FromArgb(245, 245, 247)
            };

            DrawChipSelectorMatrix(g, new RectangleF(px, py, pw, 108f),
                "АКЦЕНТНАЯ ПАЛИТРА ОСТРОВА (9 СТУДИЙНЫХ ПРОФИЛЕЙ)",
                themes, themeShort, _cfg.theme_preset ?? "Dinasmic Neon", 3,
                val => _cfg.theme_preset = val, swatches);
            py += 116f;

            string[] eqVals = { "5 Полос", "7 Полос", "Выкл" };
            DrawChipSelectorMatrix(g, new RectangleF(px, py, pw, 48f),
                "ВИЗУАЛИЗАЦИЯ СПЕКТРА (ЭКВАЛАЙЗЕР)", eqVals, eqVals, _cfg.eq_style ?? "5 Полос", 3,
                val => _cfg.eq_style = val);
            py += 56f;

            int opPct = (int)Math.Round(_cfg.bg_opacity / 2.55f);
            float halfW = (pw - 8f) * 0.5f;
            DrawPrecisionTickSlider(g, new RectangleF(px, py, halfW, 104f),
                "ПЛОТНОСТЬ ФОНА", opPct, 25, 100, opPct + "%", 14,
                () => (int)Math.Round(_cfg.bg_opacity / 2.55f),
                v => _cfg.bg_opacity = Math.Max(64, Math.Min(255, (int)Math.Round(v * 2.55f))));

            DrawPrecisionTickSlider(g, new RectangleF(px + halfW + 8f, py, halfW, 104f),
                "КОНТУРНЫЙ БЛИК", _cfg.border_glow, 0, 140, _cfg.border_glow.ToString(), 14,
                () => _cfg.border_glow, v => _cfg.border_glow = v);

            var skinCell = new RectangleF(478f, 308f, 388f, 204f);
            string bgName = string.IsNullOrEmpty(_cfg.custom_bg_path) ? "PURE #000000" : Path.GetFileName(_cfg.custom_bg_path);
            DrawBentoCell(g, skinCell, "04.B", "КАСТОМНЫЙ GIF / ФОТО СКИН", bgName);

            float sx = skinCell.X + 10f;
            float sw = skinCell.Width - 20f;
            float sy = skinCell.Y + 30f;

            DrawHardwareButton(g, new RectangleF(sx, sy, sw * 0.68f - 3f, 24f), "ЗАГРУЗИТЬ GIF / ФОТО...", true, false, () =>
            {
                using (var ofd = new OpenFileDialog())
                {
                    ofd.Title = "Выберите GIF или Фото для скина Dynamic Island";
                    ofd.Filter = "GIF и Фото (*.gif;*.png;*.jpg;*.jpeg;*.bmp)|*.gif;*.png;*.jpg;*.jpeg;*.bmp";
                    if (ofd.ShowDialog(this) == DialogResult.OK)
                    {
                        _cfg.custom_bg_path = ofd.FileName;
                        ApplyLive();
                    }
                }
            });
            DrawHardwareButton(g, new RectangleF(sx + sw * 0.68f + 3f, sy, sw * 0.32f - 3f, 24f), "СБРОС СКИНА", false, true, () =>
            {
                _cfg.custom_bg_path = "";
                ApplyLive();
            });
            sy += 30f;

            string[] fitVals = { "1:1 По размеру островка", "Smart Cover", "Contain" };
            string[] fitShort = { "1:1 Остров", "Smart Cover", "Contain" };
            DrawChipSelectorMatrix(g, new RectangleF(sx, sy, sw, 42f),
                "КАДРИРОВАНИЕ", fitVals, fitShort, _cfg.custom_bg_fit_mode ?? "1:1 По размеру островка", 3,
                val => _cfg.custom_bg_fit_mode = val);
            sy += 46f;

            string[] scopeVals = { "Везде", "Только компактный", "Только раскрытый" };
            string[] scopeShort = { "Везде", "Только компакт", "Только раскрыт." };
            DrawChipSelectorMatrix(g, new RectangleF(sx, sy, sw, 42f),
                "ОБЛАСТЬ ПОКАЗА", scopeVals, scopeShort, _cfg.custom_bg_scope ?? "Везде", 3,
                val => _cfg.custom_bg_scope = val);
            sy += 46f;

            DrawPrecisionTickSlider(g, new RectangleF(sx, sy, sw, 36f),
                "ЗАТЕМНЕНИЕ МЕДИА-СКИНА", _cfg.custom_bg_dim_percent, 0, 85, _cfg.custom_bg_dim_percent + "%", 12,
                () => _cfg.custom_bg_dim_percent, v => _cfg.custom_bg_dim_percent = v);

            var sysCell = new RectangleF(478f, 520f, 388f, 106f);
            DrawBentoCell(g, sysCell, "04.C", "СИСТЕМА И ОБНОВЛЕНИЯ", "v" + UpdateManager.CurrentVersionStr);

            float bx = sysCell.X + 10f;
            float bw = sysCell.Width - 20f;
            DrawSegmentedToggle(g, new RectangleF(bx, sysCell.Y + 28f, bw, 24f),
                "АВТОЗАПУСК ПРИ ВХОДЕ В WINDOWS", _cfg.autostart, v => _cfg.autostart = v);
            DrawSegmentedToggle(g, new RectangleF(bx, sysCell.Y + 55f, bw, 24f),
                "АВТО-УСТАНОВКА РЕЛИЗОВ GITHUB", _cfg.auto_update_enabled, v => _cfg.auto_update_enabled = v);

            DrawHardwareButton(g, new RectangleF(bx, sysCell.Y + 81f, bw, 20f),
                "↻ ПРОВЕРИТЬ ОБНОВЛЕНИЯ MILTANCHIK/DYNAMICISLAND", false, false, () =>
                {
                    if (ManualUpdateCheckRequested != null) ManualUpdateCheckRequested();
                });
        }

        private void UpdateSliderFromMouseX(PrecisionSliderHit s, float mouseX)
        {
            CommitInlineInput(false);
            float left = s.TrackRect.X + 5f;
            float width = Math.Max(1f, s.TrackRect.Width - 10f);
            float ratio = Math.Max(0f, Math.Min(1f, (mouseX - left) / width));
            int val = s.MinVal + (int)Math.Round(ratio * (s.MaxVal - s.MinVal));
            s.Setter(val);
            ApplyLive();
        }

        private void UpdateSpatialPadFromPoint(PointF pt)
        {
            if (_spatialPadRect.Width < 20f || _spatialPadRect.Height < 20f) return;
            CommitInlineInput(false);
            float rx = Math.Max(0f, Math.Min(1f, (pt.X - (_spatialPadRect.X + 24f)) / Math.Max(1f, _spatialPadRect.Width - 48f)));
            float ry = Math.Max(0f, Math.Min(1f, (pt.Y - (_spatialPadRect.Y + 8f)) / Math.Max(1f, _spatialPadRect.Height - 34f)));
            _cfg.horizontal_offset = (int)Math.Round(-500f + rx * 1000f);
            _cfg.top_margin = (int)Math.Round(ry * 120f);
            ApplyLive();
        }

        protected override void OnMouseWheel(MouseEventArgs e)
        {
            base.OnMouseWheel(e);
            if (e.Delta == 0) return;
            PointF pt = new PointF(e.X, e.Y);
            int dir = e.Delta > 0 ? 1 : -1;

            foreach (var s in _sliderHits)
            {
                if (s.WheelRect.Contains(pt))
                {
                    CommitInlineInput(false);
                    int span = s.MaxVal - s.MinVal;
                    int step = span > 400 ? 5 : 1;
                    int next = Math.Max(s.MinVal, Math.Min(s.MaxVal, s.Getter() + dir * step));
                    s.Setter(next);
                    ApplyLive();
                    return;
                }
            }
        }

        protected override void OnMouseDown(MouseEventArgs e)
        {
            base.OnMouseDown(e);
            PointF pt = new PointF(e.X, e.Y);

            if (_inlineBox.Visible && !_inlineBox.Bounds.Contains(e.Location))
                CommitInlineInput(true);

            if (e.Button != MouseButtons.Left) return;

            foreach (var h in _hits)
            {
                if (h.Rect.Contains(pt))
                {
                    h.OnClick();
                    return;
                }
            }

            foreach (var s in _sliderHits)
            {
                if (s.TrackRect.Contains(pt))
                {
                    _draggingSlider = s;
                    UpdateSliderFromMouseX(s, pt.X);
                    return;
                }
            }

            if (_spatialPadRect.Width > 0 && _spatialPadRect.Contains(pt))
            {
                _draggingSpatialPad = true;
                UpdateSpatialPadFromPoint(pt);
                return;
            }

            if (e.Y <= 36)
            {
                _draggingWindow = true;
                _windowDragOffset = e.Location;
            }
        }

        protected override void OnMouseMove(MouseEventArgs e)
        {
            base.OnMouseMove(e);
            _mousePt = new PointF(e.X, e.Y);

            if (_draggingSlider != null && (e.Button & MouseButtons.Left) == MouseButtons.Left)
            {
                UpdateSliderFromMouseX(_draggingSlider, e.X);
                return;
            }

            if (_draggingSpatialPad && (e.Button & MouseButtons.Left) == MouseButtons.Left)
            {
                UpdateSpatialPadFromPoint(_mousePt);
                return;
            }

            if (_draggingWindow && e.Button == MouseButtons.Left)
            {
                Point screenPt = PointToScreen(e.Location);
                Location = new Point(screenPt.X - _windowDragOffset.X, screenPt.Y - _windowDragOffset.Y);
                return;
            }

            Invalidate();
        }

        protected override void OnMouseUp(MouseEventArgs e)
        {
            base.OnMouseUp(e);
            _draggingSlider = null;
            _draggingSpatialPad = false;
            _draggingWindow = false;
        }

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            try { _stageTimer.Stop(); _stageTimer.Dispose(); } catch { }
            TimerSoundEngine.StopAlert();
            base.OnFormClosing(e);
        }
    }
    // =========================================================================
    public enum IslandMode { Idle, LiveActivity, Expanded }

    public sealed class DynamicIslandOverlayForm : Form
    {
        private const int CanvasW = 960;
        private const int CanvasH = 500;

        private IslandConfigData _config;
        private readonly CoreAudioBridge _audio;
        private readonly CustomBackgroundEngine _bgEngine = new CustomBackgroundEngine();
        private readonly Dictionary<string, Bitmap> _iconCache = new Dictionary<string, Bitmap>(StringComparer.OrdinalIgnoreCase);
        private readonly List<string> _dropzoneFiles = new List<string>();

        private IntPtr _dibHdc = IntPtr.Zero;
        private IntPtr _dibHbitmap = IntPtr.Zero;
        private IntPtr _dibOldObj = IntPtr.Zero;
        private IntPtr _dibBits = IntPtr.Zero;
        private Bitmap _dibWrappedBmp = null;
        private Graphics _dibGraphics = null;

        private int _cachedFontScalePct = -1;
        private uint _cachedWindowDpi = 96;
        private Font _fontCompact, _fontTitle, _fontSub, _fontSmall, _fontMono, _fontMonoCompact;
        private PointF _clientCursorPt = new PointF(-999, -999);
        private bool _isMousePressed = false;
        private readonly AutoResetEvent _bgWakeEvent = new AutoResetEvent(false);
        private NativeWin32.WinEventDelegate _winEventProc;
        private IntPtr _winEventHook = IntPtr.Zero;
        private volatile bool _isSystemSuspended = false;
        private readonly StringFormat _sfCenter = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center, Trimming = StringTrimming.EllipsisCharacter };
        private readonly StringFormat _sfLeft = new StringFormat { Alignment = StringAlignment.Near, LineAlignment = StringAlignment.Center, Trimming = StringTrimming.EllipsisCharacter };
        private readonly StringFormat _sfRight = new StringFormat { Alignment = StringAlignment.Far, LineAlignment = StringAlignment.Center };

        private IslandMode _mode = IslandMode.Idle;
        private int _expandedPage = 0; // 0 = Main Dashboard, 1 = Live Screen-Share (DWM), 2 = Screenshot Vault & Download Watchdog
        private readonly StreamCaptureEngine _streamEngine = new StreamCaptureEngine();
        private readonly DwmStreamOverlayForm _dwmOverlay = new DwmStreamOverlayForm();
        private readonly MicroTelemetryService _microTelemetry = new MicroTelemetryService();
        private readonly ScreenshotVaultManager _screenshotVault = new ScreenshotVaultManager();
        private readonly DownloadWatchdogService _watchdog = new DownloadWatchdogService();
        private readonly UpdateManager _updater = new UpdateManager();

        private bool _ghostModeActive = false;
        private bool _ghostUnlockedByUser = false;
        private float _ghostOpacityFactor = 1.0f;
        private float _ghostLiftOffsetY = 0.0f;
        private long _centerHoverStartTick = 0;
        private float _fullscreenAutoHideFactor = 1.0f;
        private float _fullscreenLiftOffsetY = 0.0f;
        private float _micPulsePhase = 0.0f;
        private float _downloadWavePhase = 0.0f;
        private readonly long _startupTick = Environment.TickCount;
        private long _lastAutoUpdateCheckTick = 0;
        private bool _initialUpdateCheckDone = false;

        private RectangleF _rcPageLeftArrow, _rcPageRightArrow;
        private RectangleF _rcStreamSourceBtn, _rcStreamCropBtn, _rcStreamOpenBtn, _rcStreamViewport;
        private RectangleF _rcCompactMicBtn, _rcExpandedMicBtn, _rcExpandedVaultBtn, _rcExpandedWatchdogBtn;
        private RectangleF _rcVaultDeleteSelectedBtn, _rcVaultClearAllBtn;
        private RectangleF _rcWatchdogToggleBtn, _rcWatchdogActionBtn, _rcWatchdogCancelCountdownBtn;
        private RectangleF _rcUpdateApplyBtn, _rcUpdateLaterBtn;
        private readonly List<KeyValuePair<RectangleF, VaultCaptureItem>> _vaultThumbRects = new List<KeyValuePair<RectangleF, VaultCaptureItem>>();
        private readonly List<KeyValuePair<RectangleF, VaultCaptureItem>> _vaultCheckRects = new List<KeyValuePair<RectangleF, VaultCaptureItem>>();
        private VaultCaptureItem _pendingDragVaultItem = null;
        private Point _vaultDragStartPt;

        private bool _clickThroughActive = false;
        private bool _isHovered = false;
        private volatile bool _hiddenByFullscreen = false;
        private volatile bool _isFullscreenAppActive = false;

        private float _curW, _curH, _curProg = 0f;
        private float _startW, _startH, _startProg = 0f;
        private float _targetW, _targetH, _targetProg = 0f;
        private readonly Stopwatch _animWatch = new Stopwatch();
        private int _animDurationMs = 330;
        private bool _animExpanding = true;
        private bool _isAnimating = false;

        private bool _lctrlHeld = false;
        private bool _isResizingWithLCtrl = false;
        private Point _resizeDragStartPt;
        private int _resizeStartW, _resizeStartH;
        private int _resizeHorizontalSign = 1;

        private readonly System.Windows.Forms.Timer _frameTimer;
        private readonly System.Windows.Forms.Timer _collapseTimer;
        private readonly System.Windows.Forms.Timer _pomoTimer;
        private Thread _bgWorkerThread;
        private volatile bool _disposing = false;
        private volatile bool _bgStateDirty = false;
        private volatile string _pendingLiveActivityBanner = null;
        private long _lastExpandedProgressRedrawTick = 0;

        private readonly float[] _eqBars = new float[7] { 0.12f, 0.12f, 0.12f, 0.12f, 0.12f, 0.12f, 0.12f };
        private readonly float[] _expEqBars = new float[28];
        private float _eqPhase = 0f;

        private readonly object _stateLock = new object();
        private string _clockStr = DateTime.Now.ToString("HH:mm");
        private int _batteryPct = 100;
        private bool _batteryCharging = false;
        private bool _hasBattery = false;

        private List<MediaSessionSnapshot> _allPlayers = new List<MediaSessionSnapshot>();
        private MediaSessionSnapshot _currentMedia = new MediaSessionSnapshot();
        private int _currentPlayerIdx = 0;
        private string _lastTrackSignature = "";
        private string _liveBannerText = "";

        private int _cpuUsage = 6;
        private int _ramUsage = 42;
        private string _netSpeedText = "↓ 0 KB/s ↑ 0 KB/s";

        private bool _pomoRunning = false;
        private int _pomoRemainingSec = 0;

        private RectangleF _rcSongClickZone;
        private RectangleF _rcPlayerBadge;
        private RectangleF _rcLikeBtn, _rcPrevBtn, _rcPlayBtn, _rcNextBtn, _rcProgressBar;
        private RectangleF _rcMuteBtn, _rcVolSlider, _rcMusicMuteBtn, _rcMusicVolSlider, _rcPomoBtn, _rcSettingsBtn;
        private RectangleF _rcPcVolCard, _rcMusicVolCard;
        private readonly List<KeyValuePair<RectangleF, PinnedAppItem>> _dockRects = new List<KeyValuePair<RectangleF, PinnedAppItem>>();
        private RectangleF _rcAddAppBtn;
        private RectangleF _rcDropzone;
        private bool _draggingVolume = false;
        private bool _draggingMusicVolume = false;
        private bool _draggingSeek = false;

        public DynamicIslandOverlayForm()
        {
            _config = ConfigManager.Load();
            _audio = new CoreAudioBridge();
            _bgEngine.EnsureLoaded(_config.custom_bg_path);
            EnsureCachedFonts();
            InitPersistentDibSurface();

            _curW = _config.idle_width;
            _curH = _config.idle_height;
            _targetW = _curW;
            _targetH = _curH;

            FormBorderStyle = FormBorderStyle.None;
            StartPosition = FormStartPosition.Manual;
            TopMost = true;
            try { Icon = Icon.ExtractAssociatedIcon(Application.ExecutablePath); } catch { }
            ShowInTaskbar = false;
            AllowDrop = true;
            Size = new Size(CanvasW, CanvasH);

            _collapseTimer = new System.Windows.Forms.Timer { Interval = _config.collapse_delay_ms };
            _collapseTimer.Tick += (s, e) =>
            {
                if (_lctrlHeld || _isResizingWithLCtrl || _expandedPage == 1) return;
                _collapseTimer.Stop();
                if (!_isHovered) CollapseToIdle();
            };

            _frameTimer = new System.Windows.Forms.Timer { Interval = 15 };
            _frameTimer.Tick += OnFrameTick;
            _frameTimer.Start();

            _pomoTimer = new System.Windows.Forms.Timer { Interval = 1000 };
            _pomoTimer.Tick += OnPomodoroSecond;

            PositionOnMonitor(_config.monitor_index);

            _bgWorkerThread = new Thread(BackgroundPollingLoop)
            {
                IsBackground = true,
                Name = "DynamicIslandBgWorker"
            };
            _bgWorkerThread.Start();
        }

        private void InitPersistentDibSurface()
        {
            IntPtr screenDc = NativeWin32.GetDC(IntPtr.Zero);
            _dibHdc = NativeWin32.CreateCompatibleDC(screenDc);

            var bmi = new NativeWin32.BITMAPINFO();
            bmi.bmiHeader.biSize = (uint)Marshal.SizeOf(typeof(NativeWin32.BITMAPINFOHEADER));
            bmi.bmiHeader.biWidth = CanvasW;
            bmi.bmiHeader.biHeight = -CanvasH;
            bmi.bmiHeader.biPlanes = 1;
            bmi.bmiHeader.biBitCount = 32;
            bmi.bmiHeader.biCompression = 0;

            _dibHbitmap = NativeWin32.CreateDIBSection(_dibHdc, ref bmi, 0, out _dibBits, IntPtr.Zero, 0);
            _dibOldObj = NativeWin32.SelectObject(_dibHdc, _dibHbitmap);
            NativeWin32.ReleaseDC(IntPtr.Zero, screenDc);

            _dibWrappedBmp = new Bitmap(CanvasW, CanvasH, CanvasW * 4, PixelFormat.Format32bppPArgb, _dibBits);
            _dibGraphics = Graphics.FromImage(_dibWrappedBmp);
            _dibGraphics.SmoothingMode = SmoothingMode.AntiAlias;
            _dibGraphics.TextRenderingHint = TextRenderingHint.AntiAliasGridFit;
            _dibGraphics.PixelOffsetMode = PixelOffsetMode.HighQuality;
            _dibGraphics.CompositingQuality = CompositingQuality.HighSpeed;
        }

        private void EnsureCachedFonts()
        {
            int scalePct = Math.Max(75, Math.Min(160, _config.ui_scale_percent));
            uint dpi = 96;
            try { if (IsHandleCreated) { uint wDpi = NativeWin32.GetDpiForWindow(Handle); if (wDpi >= 72 && wDpi <= 384) dpi = wDpi; } } catch { }

            if (scalePct == _cachedFontScalePct && dpi == _cachedWindowDpi && _fontCompact != null && _fontMono != null) return;
            _cachedFontScalePct = scalePct;
            _cachedWindowDpi = dpi;
            float s = scalePct / 100f;

            if (_fontCompact != null) _fontCompact.Dispose();
            if (_fontTitle != null) _fontTitle.Dispose();
            if (_fontSub != null) _fontSub.Dispose();
            if (_fontSmall != null) _fontSmall.Dispose();
            if (_fontMono != null) _fontMono.Dispose();
            if (_fontMonoCompact != null) _fontMonoCompact.Dispose();

            _fontCompact = new Font("Segoe UI Semibold", 9.5f * s, FontStyle.Bold);
            _fontTitle = new Font("Segoe UI Semibold", 10.2f * s, FontStyle.Bold);
            _fontSub = new Font("Segoe UI", 8.5f * s, FontStyle.Regular);
            _fontSmall = new Font("Segoe UI Semibold", 7.9f * s, FontStyle.Bold);
            _fontMono = new Font("Consolas", 8.3f * s, FontStyle.Bold);
            _fontMonoCompact = new Font("Consolas", 9.6f * s, FontStyle.Bold);
        }

        private void OnDisplaySettingsChanged(object sender, EventArgs e)
        {
            if (_disposing || !IsHandleCreated) return;
            try
            {
                BeginInvoke((MethodInvoker)delegate
                {
                    EnsureCachedFonts();
                    PositionOnMonitor(_config.monitor_index);
                    WakeRenderLoop();
                    RenderLayeredFrame();
                });
            }
            catch { }
        }

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            _disposing = true;
            try { _bgWakeEvent.Set(); } catch { }
            try { _frameTimer.Stop(); _collapseTimer.Stop(); _pomoTimer.Stop(); } catch { }
            try { SystemEvents.DisplaySettingsChanged -= OnDisplaySettingsChanged; } catch { }
            try { if (_winEventHook != IntPtr.Zero) { NativeWin32.UnhookWinEvent(_winEventHook); _winEventHook = IntPtr.Zero; } } catch { }
            try { if (IsHandleCreated) NativeWin32.RemoveClipboardFormatListener(Handle); } catch { }
            try { if (IsHandleCreated) NativeWin32.UnregisterHotKey(Handle, NativeWin32.HOTKEY_ID_ISLAND); } catch { }
            try { _screenshotVault.Dispose(); } catch { }
            try { _audio.Dispose(); } catch { }
            try { _dwmOverlay.HideStream(); _dwmOverlay.Close(); } catch { }
            foreach (var kv in _iconCache)
            {
                if (kv.Value != null) { try { kv.Value.Dispose(); } catch { } }
            }
            _iconCache.Clear();
            try
            {
                if (_dibGraphics != null) { _dibGraphics.Dispose(); _dibGraphics = null; }
                if (_dibWrappedBmp != null) { _dibWrappedBmp.Dispose(); _dibWrappedBmp = null; }
                if (_dibHdc != IntPtr.Zero && _dibOldObj != IntPtr.Zero) NativeWin32.SelectObject(_dibHdc, _dibOldObj);
                if (_dibHbitmap != IntPtr.Zero) { NativeWin32.DeleteObject(_dibHbitmap); _dibHbitmap = IntPtr.Zero; }
                if (_dibHdc != IntPtr.Zero) { NativeWin32.DeleteDC(_dibHdc); _dibHdc = IntPtr.Zero; }
            }
            catch { }
            base.OnFormClosing(e);
        }

        protected override CreateParams CreateParams
        {
            get
            {
                CreateParams cp = base.CreateParams;
                cp.ExStyle |= NativeWin32.WS_EX_LAYERED | NativeWin32.WS_EX_TOOLWINDOW | NativeWin32.WS_EX_TOPMOST | NativeWin32.WS_EX_NOACTIVATE;
                return cp;
            }
        }

        protected override void OnShown(EventArgs e)
        {
            base.OnShown(e);
            try { NativeWin32.AddClipboardFormatListener(Handle); } catch { }
            try { NativeWin32.RegisterHotKey(Handle, NativeWin32.HOTKEY_ID_ISLAND, NativeWin32.MOD_ALT, NativeWin32.VK_SPACE); } catch { }

            _screenshotVault.OnScreenshotCaptured += timeStr =>
            {
                _pendingLiveActivityBanner = "📸 Скриншот сохранён в хранилище (" + timeStr + ")";
                _bgStateDirty = true;
            };
            _updater.OnStateChanged += () => { _bgStateDirty = true; WakeRenderLoop(); };
            _audio.OnAudioEndpointChanged += () => { _bgStateDirty = true; _bgWakeEvent.Set(); WakeRenderLoop(); };
            SystemEvents.DisplaySettingsChanged += OnDisplaySettingsChanged;
            SystemEvents.SessionEnding += (s, args) =>
            {
                try { _screenshotVault.Dispose(); } catch { }
            };

            _winEventProc = (hHook, evType, hwnd, idObj, idChild, thread, time) =>
            {
                if (_disposing || idObj != 0) return;
                if (evType != NativeWin32.EVENT_SYSTEM_FOREGROUND &&
                    evType != NativeWin32.EVENT_SYSTEM_MINIMIZESTART &&
                    evType != NativeWin32.EVENT_SYSTEM_MINIMIZEEND)
                    return;

                if (_mode == IslandMode.Expanded && _expandedPage == 1 && IsHandleCreated)
                {
                    try
                    {
                        var src = _streamEngine.GetActiveSource(Handle);
                        if (src != null && (hwnd == src.Hwnd || evType == NativeWin32.EVENT_SYSTEM_FOREGROUND))
                        {
                            _dwmOverlay.EnsureStreamSourceAlive(Handle, src, _streamEngine.CropModeIndex);
                        }
                    }
                    catch { }
                }

                _bgWakeEvent.Set();
                WakeRenderLoop();
            };
            try
            {
                _winEventHook = NativeWin32.SetWinEventHook(
                    NativeWin32.EVENT_SYSTEM_FOREGROUND,
                    NativeWin32.EVENT_SYSTEM_MINIMIZEEND,
                    IntPtr.Zero,
                    _winEventProc,
                    0,
                    0,
                    NativeWin32.WINEVENT_OUTOFCONTEXT | NativeWin32.WINEVENT_SKIPOWNPROCESS);
            }
            catch { }

            RenderLayeredFrame();
            ExpandIsland();
            _collapseTimer.Interval = 2400;
            _collapseTimer.Start();
        }

        protected override void WndProc(ref Message m)
        {
            if (m.Msg == NativeWin32.WM_CLIPBOARDUPDATE)
            {
                if (_config.enable_screenshot_vault)
                {
                    _screenshotVault.TryCaptureFromClipboard();
                }
            }
            else if (m.Msg == NativeWin32.WM_HOTKEY && m.WParam.ToInt32() == NativeWin32.HOTKEY_ID_ISLAND)
            {
                // Conscious activation via Global Hotkey (Alt + Space): exit Ghost Mode and toggle island
                _ghostModeActive = false;
                _ghostUnlockedByUser = true;
                _ghostOpacityFactor = 1.0f;
                _ghostLiftOffsetY = 0.0f;
                if (_mode == IslandMode.Expanded)
                {
                    _isHovered = false;
                    CollapseToIdle();
                }
                else
                {
                    ExpandIsland();
                }
                RenderLayeredFrame();
                return;
            }
            else if (m.Msg == NativeWin32.WM_POWERBROADCAST)
            {
                int pbt = m.WParam.ToInt32();
                if (pbt == NativeWin32.PBT_APMSUSPEND)
                {
                    _isSystemSuspended = true;
                    _frameTimer.Stop();
                }
                else if (pbt == NativeWin32.PBT_APMRESUMEAUTOMATIC || pbt == NativeWin32.PBT_APMRESUMESUSPEND)
                {
                    _isSystemSuspended = false;
                    _audio.InitializeEndpoint();
                    PositionOnMonitor(_config.monitor_index);
                    _bgWakeEvent.Set();
                    WakeRenderLoop();
                    RenderLayeredFrame();
                }
            }
            else if (m.Msg == NativeWin32.WM_DISPLAYCHANGE || m.Msg == NativeWin32.WM_DPICHANGED)
            {
                EnsureCachedFonts();
                PositionOnMonitor(_config.monitor_index);
                WakeRenderLoop();
                RenderLayeredFrame();
            }
            base.WndProc(ref m);
        }

        public void PositionOnMonitor(int monitorIdx)
        {
            Screen[] screens = Screen.AllScreens;
            if (screens.Length == 0) return;
            int idx = Math.Max(0, Math.Min(monitorIdx, screens.Length - 1));
            _config.monitor_index = idx;
            Rectangle bounds = screens[idx].Bounds;
            int x = bounds.X + (bounds.Width - CanvasW) / 2 + _config.horizontal_offset;
            int y = bounds.Y - 14;
            Location = new Point(x, y);
        }

        private bool IsNeonTelemetryGameModeActive()
        {
            return _isFullscreenAppActive &&
                   _mode == IslandMode.Idle &&
                   !_isAnimating &&
                   (_config.fullscreen_mode == "Неоновая телеметрия" && !_config.hide_in_fullscreen);
        }

        private RectangleF CurrentPillRect()
        {
            float w = _curW;
            float h = _curH;
            float baseTop = Math.Max(0, _config.top_margin);
            if (IsNeonTelemetryGameModeActive())
            {
                w = Math.Max(368f, _curW);
                h = 24f;
                baseTop = Math.Min(baseTop, 4f);
            }
            float x = (CanvasW - w) / 2f;
            float y = 14f + baseTop + _ghostLiftOffsetY + _fullscreenLiftOffsetY;
            return new RectangleF(x, y, w, h);
        }

        private void SetWin32ClickThrough(bool passthrough)
        {
            if (_clickThroughActive == passthrough || !IsHandleCreated) return;
            _clickThroughActive = passthrough;
            long ex = NativeWin32.GetWindowLongPtr(Handle, NativeWin32.GWL_EXSTYLE).ToInt64();
            if (passthrough) ex |= NativeWin32.WS_EX_TRANSPARENT;
            else ex &= ~((long)NativeWin32.WS_EX_TRANSPARENT);
            NativeWin32.SetWindowLongPtr(Handle, NativeWin32.GWL_EXSTYLE, new IntPtr(ex));
        }

        private void StartSpringTransition(float targetW, float targetH, float targetProg, int durationMs, bool expanding)
        {
            _startW = _curW;
            _startH = _curH;
            _startProg = _curProg;
            _targetW = targetW;
            _targetH = targetH;
            _targetProg = targetProg;
            _animDurationMs = Math.Max(90, durationMs);
            _animExpanding = expanding;
            _animWatch.Restart();
            _isAnimating = true;
            WakeRenderLoop();
        }

        private float EvaluateDampedSpring(float t, bool expanding)
        {
            if (t <= 0f) return 0f;
            if (t >= 1f) return 1f;
            // Under-damped spring on expand (fast launch + natural micro-settle), near-critically damped on collapse (no bounce)
            float zeta = expanding
                ? Math.Max(0.52f, Math.Min(0.94f, 1.30f - (_config.spring_overshoot_pct / 190f)))
                : 0.94f;
            float omega0 = expanding ? 11.4f : 10.4f;
            float omegaD = omega0 * (float)Math.Sqrt(Math.Max(0.01f, 1.0f - zeta * zeta));
            float env = (float)Math.Exp(-zeta * omega0 * t);
            return 1.0f - env * ((float)Math.Cos(omegaD * t) + (zeta * omega0 / omegaD) * (float)Math.Sin(omegaD * t));
        }

        private void WakeRenderLoop()
        {
            if (_disposing || _isSystemSuspended) return;
            if (_frameTimer != null)
            {
                if (_frameTimer.Interval != 14) _frameTimer.Interval = 14;
                if (!_frameTimer.Enabled) _frameTimer.Start();
            }
        }

        public void ExpandIsland()
        {
            _collapseTimer.Stop();
            _liveBannerText = "";
            if (_mode == IslandMode.Expanded && !_isAnimating) return;
            _mode = IslandMode.Expanded;
            StartSpringTransition(_config.expanded_width, _config.expanded_height, 1.0f, _config.anim_duration_ms, true);
        }

        public void CollapseToIdle()
        {
            if (_isHovered || _lctrlHeld || _isResizingWithLCtrl || _expandedPage == 1) return;
            _liveBannerText = "";
            _mode = IslandMode.Idle;
            StartSpringTransition(_config.idle_width, _config.idle_height, 0.0f, Math.Max(130, (int)(_config.anim_duration_ms * 0.80f)), false);
        }

        public void TriggerLiveActivity(string bannerText, int holdMs = 1850)
        {
            if (!_config.enable_live_activities || _mode == IslandMode.Expanded) return;
            _liveBannerText = bannerText;
            _mode = IslandMode.LiveActivity;
            float actW = Math.Max(340f, _config.idle_width + 100f);
            float actH = Math.Max(46f, _config.idle_height + 10f);
            StartSpringTransition(actW, actH, 0.0f, 320, true);
            _collapseTimer.Interval = holdMs;
            _collapseTimer.Stop();
            _collapseTimer.Start();
        }        private void OnFrameTick(object sender, EventArgs e)
        {
            if (_pendingLiveActivityBanner != null)
            {
                string banner = _pendingLiveActivityBanner;
                _pendingLiveActivityBanner = null;
                TriggerLiveActivity(banner);
            }

            bool lctrlNow = _config.enable_lctrl_resize && NativeWin32.IsLCtrlPressed();
            bool lctrlChanged = (lctrlNow != _lctrlHeld);
            _lctrlHeld = lctrlNow;
            if (!_lctrlHeld && _isResizingWithLCtrl)
            {
                _isResizingWithLCtrl = false;
                ConfigManager.Save(_config);
            }

            if (_isSystemSuspended) return;

            Point localPt = PointToClient(Cursor.Position);
            bool cursorMovedInside = false;
            if (Math.Abs(localPt.X - _clientCursorPt.X) > 0.5f || Math.Abs(localPt.Y - _clientCursorPt.Y) > 0.5f)
            {
                RectangleF prevCheck = RectangleF.Inflate(CurrentPillRect(), 8f, 8f);
                if (_mode == IslandMode.Expanded && (prevCheck.Contains(localPt) || prevCheck.Contains(_clientCursorPt)))
                    cursorMovedInside = true;
                _clientCursorPt = new PointF(localPt.X, localPt.Y);
            }
            RectangleF pill = CurrentPillRect();
            RectangleF hitBox = RectangleF.Inflate(pill, _lctrlHeld ? 14f : 4f, _lctrlHeld ? 14f : 4f);
            bool needsRedraw = lctrlChanged || _bgStateDirty || cursorMovedInside;

            // MODULE 1: Smooth Fullscreen Auto-Hide fade & upward slide
            bool wantAutoHideFs = _isFullscreenAppActive && (_expandedPage != 1) &&
                                  (_config.fullscreen_mode == "Авто-скрытие" || _config.hide_in_fullscreen);
            float targetFsAlpha = wantAutoHideFs ? 0.0f : 1.0f;
            float targetFsLift = wantAutoHideFs ? -42.0f : 0.0f;
            if (Math.Abs(_fullscreenAutoHideFactor - targetFsAlpha) > 0.008f || Math.Abs(_fullscreenLiftOffsetY - targetFsLift) > 0.2f)
            {
                _fullscreenAutoHideFactor += (targetFsAlpha - _fullscreenAutoHideFactor) * 0.22f;
                _fullscreenLiftOffsetY += (targetFsLift - _fullscreenLiftOffsetY) * 0.22f;
                needsRedraw = true;
            }
            else
            {
                _fullscreenAutoHideFactor = targetFsAlpha;
                _fullscreenLiftOffsetY = targetFsLift;
            }
            _hiddenByFullscreen = (_fullscreenAutoHideFactor < 0.03f);

            // MODULE 3: Ghost Mode & Smart Cursor Reaction (35px proximity buffer, 20% opacity, -5px lift, click-through until 500ms center dwell / Alt+Space / LCtrl/LAlt)
            if (_config.enable_ghost_mode && _mode != IslandMode.Expanded && !_lctrlHeld && !_isResizingWithLCtrl && !wantAutoHideFs)
            {
                RectangleF basePill = new RectangleF((CanvasW - pill.Width) / 2f, Math.Max(0, _config.top_margin), pill.Width, pill.Height);
                RectangleF proxZone = RectangleF.Inflate(basePill, 35f, 35f);
                RectangleF centerCore = RectangleF.Inflate(basePill, -basePill.Width * 0.16f, 0f);

                if (proxZone.Contains(localPt.X, localPt.Y))
                {
                    if (NativeWin32.IsModifierHeldForGhostExit())
                    {
                        _ghostUnlockedByUser = true;
                        _ghostModeActive = false;
                        _centerHoverStartTick = 0;
                        ExpandIsland();
                    }
                    else if (!_ghostUnlockedByUser)
                    {
                        if (centerCore.Contains(localPt.X, localPt.Y))
                        {
                            if (_centerHoverStartTick == 0)
                                _centerHoverStartTick = Environment.TickCount;
                            else if ((Environment.TickCount - _centerHoverStartTick) >= 500)
                            {
                                _ghostUnlockedByUser = true;
                                _ghostModeActive = false;
                                _centerHoverStartTick = 0;
                                ExpandIsland();
                            }
                            else
                            {
                                _ghostModeActive = true;
                            }
                        }
                        else
                        {
                            _centerHoverStartTick = 0;
                            _ghostModeActive = true;
                        }
                    }
                }
                else
                {
                    _ghostModeActive = false;
                    _ghostUnlockedByUser = false;
                    _centerHoverStartTick = 0;
                }
            }
            else
            {
                _ghostModeActive = false;
                _centerHoverStartTick = 0;
            }

            float targetGhostOpacity = _ghostModeActive ? 0.20f : 1.0f;
            float targetGhostLift = _ghostModeActive ? -5.0f : 0.0f;
            if (Math.Abs(_ghostOpacityFactor - targetGhostOpacity) > 0.008f || Math.Abs(_ghostLiftOffsetY - targetGhostLift) > 0.15f)
            {
                _ghostOpacityFactor += (targetGhostOpacity - _ghostOpacityFactor) * 0.26f;
                _ghostLiftOffsetY += (targetGhostLift - _ghostLiftOffsetY) * 0.26f;
                needsRedraw = true;
            }
            else
            {
                _ghostOpacityFactor = targetGhostOpacity;
                _ghostLiftOffsetY = targetGhostLift;
            }

            bool inside = !_hiddenByFullscreen && !_ghostModeActive && hitBox.Contains(localPt.X, localPt.Y);

            SetWin32ClickThrough((!inside || _ghostModeActive || wantAutoHideFs) && !_isResizingWithLCtrl);

            if (inside && !_isHovered)
            {
                _isHovered = true;
                _collapseTimer.Stop();
                if (_config.expand_on_hover && !_isFullscreenAppActive && !_lctrlHeld)
                    ExpandIsland();
            }
            else if (!inside && _isHovered && !_isResizingWithLCtrl)
            {
                _isHovered = false;
                _draggingVolume = false;
                _draggingSeek = false;
                _ghostUnlockedByUser = false;
                if (!_lctrlHeld && _expandedPage != 1)
                {
                    _collapseTimer.Interval = Math.Max(100, _config.enable_ghost_mode ? 300 : _config.collapse_delay_ms);
                    _collapseTimer.Start();
                }
            }

            // MODULE 2 & 5: Pulsing Microphone Glow & Download Watchdog Border Wave
            if (_config.enable_mic_indicator && _audio.IsMicActive && !_audio.IsMicMuted)
            {
                _micPulsePhase += 0.11f;
                needsRedraw = true;
            }
            if (_watchdog.IsActiveDownloadDetected || _watchdog.IsWatchdogArmed || _watchdog.IsCountdownActive)
            {
                float speedBoost = Math.Min(4.0f, Math.Max(0.4f, _watchdog.CurrentDownMBps * 0.25f));
                _downloadWavePhase += 0.10f * speedBoost;
                needsRedraw = true;
            }
            _bgStateDirty = false;

            if (_bgEngine.AdvanceAnimationFrame())
                needsRedraw = true;

            if (_isAnimating && !_isResizingWithLCtrl)
            {
                long elapsed = _animWatch.ElapsedMilliseconds;
                float rawT = Math.Max(0f, Math.Min(1f, elapsed / (float)_animDurationMs));
                float eased = EvaluateDampedSpring(rawT, _animExpanding);
                _curW = _startW + (_targetW - _startW) * eased;
                _curH = _startH + (_targetH - _startH) * eased;
                _curProg = Math.Max(0f, Math.Min(1f, _startProg + (_targetProg - _startProg) * eased));
                if (rawT >= 1.0f)
                {
                    _curW = _targetW;
                    _curH = _targetH;
                    _curProg = _targetProg;
                    _isAnimating = false;
                }
                needsRedraw = true;
            }

            if ((_mode != IslandMode.Expanded || _expandedPage != 1) && _config.eq_style != "Выкл" && _config.eq_style != "Off")
            {
                float masterPeak, leftPeak, rightPeak;
                _audio.SampleAudioPeaks(out masterPeak, out leftPeak, out rightPeak);

                float boostedL = (float)Math.Pow(Math.Max(0f, leftPeak), 0.50);
                float boostedR = (float)Math.Pow(Math.Max(0f, rightPeak), 0.50);
                float boostedM = (float)Math.Pow(Math.Max(0f, masterPeak), 0.48);

                bool audioActive = (masterPeak > 0.0025f) && !_audio.LastMuted;
                if (!audioActive && _currentMedia.IsPlaying && !_audio.LastMuted)
                {
                    boostedM = 0.48f; boostedL = 0.45f; boostedR = 0.45f;
                    audioActive = true;
                }

                if (audioActive)
                {
                    _eqPhase += 0.34f + boostedM * 0.42f;
                    float[] bandMultipliers = new float[7]
                    {
                        0.72f + 0.28f * (float)Math.Sin(_eqPhase * 1.1f),
                        0.88f + 0.30f * (float)Math.Cos(_eqPhase * 1.45f + 0.7f),
                        1.05f + 0.25f * (float)Math.Sin(_eqPhase * 1.85f + 1.4f),
                        0.95f + 0.32f * (float)Math.Cos(_eqPhase * 1.60f + 2.2f),
                        0.78f + 0.28f * (float)Math.Sin(_eqPhase * 1.25f + 3.1f),
                        0.85f + 0.30f * (float)Math.Cos(_eqPhase * 1.50f + 3.9f),
                        0.68f + 0.26f * (float)Math.Sin(_eqPhase * 1.15f + 4.7f)
                    };

                    for (int i = 0; i < 7; i++)
                    {
                        float chanPeak = (i < 2) ? boostedL : (i > 4 ? boostedR : boostedM);
                        float target = Math.Max(0.10f, Math.Min(1.0f, chanPeak * bandMultipliers[i]));
                        float rate = (target > _eqBars[i]) ? 0.52f : 0.20f;
                        float nextVal = _eqBars[i] + (target - _eqBars[i]) * rate;
                        if (Math.Abs(nextVal - _eqBars[i]) > 0.004f)
                        {
                            _eqBars[i] = nextVal;
                            needsRedraw = true;
                        }
                    }

                    for (int i = 0; i < _expEqBars.Length; i++)
                    {
                        float normX = i / (float)(_expEqBars.Length - 1);
                        float centerBell = 0.55f + 0.45f * (float)Math.Sin(normX * Math.PI);
                        float chanPeak = (normX < 0.38f) ? boostedL : (normX > 0.62f ? boostedR : boostedM);
                        float wave = 0.68f
                            + 0.24f * (float)Math.Sin(_eqPhase * 1.35f + i * 0.52f)
                            + 0.18f * (float)Math.Cos(_eqPhase * 0.95f - i * 0.37f);
                        float target = Math.Max(0.08f, Math.Min(1.0f, chanPeak * wave * centerBell));
                        float rate = (target > _expEqBars[i]) ? 0.48f : 0.22f;
                        float nextVal = _expEqBars[i] + (target - _expEqBars[i]) * rate;
                        if (Math.Abs(nextVal - _expEqBars[i]) > 0.004f)
                        {
                            _expEqBars[i] = nextVal;
                            needsRedraw = true;
                        }
                    }
                }
                else
                {
                    for (int i = 0; i < 7; i++)
                    {
                        if (_eqBars[i] > 0.11f)
                        {
                            _eqBars[i] = Math.Max(0.10f, _eqBars[i] * 0.82f);
                            needsRedraw = true;
                        }
                    }
                    for (int i = 0; i < _expEqBars.Length; i++)
                    {
                        if (_expEqBars[i] > 0.07f)
                        {
                            _expEqBars[i] = Math.Max(0.06f, _expEqBars[i] * 0.82f);
                            needsRedraw = true;
                        }
                    }
                }
            }

            if (_mode == IslandMode.Expanded && _expandedPage == 0 && !_isAnimating && _currentMedia.IsPlaying && _currentMedia.DurationSec > 0)
            {
                long nowTick = Environment.TickCount;
                if (nowTick - _lastExpandedProgressRedrawTick >= 180)
                {
                    _lastExpandedProgressRedrawTick = nowTick;
                    needsRedraw = true;
                }
            }
            else if (_mode == IslandMode.Expanded && _expandedPage == 1 && !_isAnimating && IsHandleCreated)
            {
                long nowTick = Environment.TickCount;
                if (nowTick - _lastExpandedProgressRedrawTick >= 200)
                {
                    _lastExpandedProgressRedrawTick = nowTick;
                    var activeSrc = _streamEngine.GetActiveSource(Handle);
                    if (activeSrc != null)
                        _dwmOverlay.EnsureStreamSourceAlive(Handle, activeSrc, _streamEngine.CropModeIndex);
                }
            }

            if (_hiddenByFullscreen && !_isAnimating)
            {
                if (_frameTimer.Interval != 450) _frameTimer.Interval = 450;
                return;
            }

            if (needsRedraw)
                RenderLayeredFrame();

            bool cursorNearTop = (localPt.Y >= -20 && localPt.Y <= 125 && localPt.X >= 80 && localPt.X <= CanvasW - 80);
            bool activeVisuals = _isAnimating || _isHovered || _lctrlHeld || _isResizingWithLCtrl ||
                                 _mode != IslandMode.Idle || _bgEngine.IsAnimated ||
                                 (_currentMedia.IsPlaying && _config.eq_style != "Выкл") ||
                                 (_config.enable_mic_indicator && _audio.IsMicActive && !_audio.IsMicMuted) ||
                                 _watchdog.IsActiveDownloadDetected || _watchdog.IsCountdownActive || _updater.IsDownloading;

            int desiredInterval = (activeVisuals || cursorNearTop) ? 14 : 220;
            if (_frameTimer.Interval != desiredInterval)
                _frameTimer.Interval = desiredInterval;
        }

        private void BackgroundPollingLoop()
        {
            PerformanceCounter cpuCounter = null;
            try { cpuCounter = new PerformanceCounter("Processor", "% Processor Time", "_Total"); cpuCounter.NextValue(); }
            catch { cpuCounter = null; }

            long lastBytesRecv = 0, lastBytesSent = 0, lastNetTick = Environment.TickCount;
            SampleNetworkTotals(out lastBytesRecv, out lastBytesSent);

            while (!_disposing)
            {
                try
                {
                    if (_isSystemSuspended)
                    {
                        _bgWakeEvent.WaitOne(3500);
                        continue;
                    }
                    if (_isAnimating)
                    {
                        _bgWakeEvent.WaitOne(60);
                        continue;
                    }

                    string clockNow = DateTime.Now.ToString("HH:mm");
                    int batPct = 100; bool batChg = false, hasBat = false;
                    NativeWin32.SYSTEM_POWER_STATUS sps;
                    if (NativeWin32.GetSystemPowerStatus(out sps))
                    {
                        hasBat = (sps.BatteryFlag & 128) == 0 && sps.BatteryLifePercent <= 100;
                        batChg = sps.ACLineStatus == 1 || (sps.BatteryFlag & 8) != 0;
                        batPct = hasBat ? Math.Max(0, Math.Min(100, (int)sps.BatteryLifePercent)) : 100;
                    }

                    if (IsHandleCreated)
                    {
                        Screen[] screens = Screen.AllScreens;
                        if (screens.Length > 0)
                        {
                            int idx = Math.Max(0, Math.Min(_config.monitor_index, screens.Length - 1));
                            bool fsNow = SystemSensors.IsForegroundAppFullscreen(Handle, screens[idx]);
                            _isFullscreenAppActive = fsNow;
                            // Page 2 (Screen Share Viewer) ALWAYS stays visible and strictly expanded even over fullscreen games/apps!
                            _hiddenByFullscreen = (_expandedPage != 1) && _config.hide_in_fullscreen && fsNow;
                            if ((fsNow || _expandedPage == 1) && !_hiddenByFullscreen)
                            {
                                NativeWin32.SetWindowPos(Handle, NativeWin32.HWND_TOPMOST, 0, 0, 0, 0,
                                    NativeWin32.SWP_NOMOVE | NativeWin32.SWP_NOSIZE | NativeWin32.SWP_NOACTIVATE);
                                if (_expandedPage == 1 && _dwmOverlay != null && _dwmOverlay.IsHandleCreated && _dwmOverlay.Visible)
                                {
                                    NativeWin32.SetWindowPos(_dwmOverlay.Handle, NativeWin32.HWND_TOPMOST, 0, 0, 0, 0,
                                        NativeWin32.SWP_NOMOVE | NativeWin32.SWP_NOSIZE | NativeWin32.SWP_NOACTIVATE);
                                }
                            }
                        }
                    }
                    else
                    {
                        _isFullscreenAppActive = false;
                        _hiddenByFullscreen = false;
                    }

                    if (_audio.RefreshState() && _mode != IslandMode.Expanded)
                    {
                        _pendingLiveActivityBanner = "Громкость  " + (_audio.LastMuted ? "Выкл" : (_audio.LastVolume + "%"));
                    }

                    if (!_draggingSeek)
                    {
                        MediaSessionSnapshot activeSnap;
                        int activeIdx;
                        var players = MultiPlayerMediaController.QueryAllPlayers(_config.prefer_music_players, out activeSnap, out activeIdx);

                        string sig = activeSnap.AppId + "::" + activeSnap.Title + "::" + activeSnap.Artist;
                        if (!string.IsNullOrEmpty(_lastTrackSignature) &&
                            !string.Equals(sig, _lastTrackSignature, StringComparison.Ordinal) &&
                            activeSnap.IsPlaying &&
                            _mode != IslandMode.Expanded)
                        {
                            _pendingLiveActivityBanner = activeSnap.Title + " — " + activeSnap.Artist;
                        }
                        _lastTrackSignature = sig;

                        if (!_draggingMusicVolume)
                        {
                            _audio.RefreshMusicVolume(activeSnap.AppId);
                        }
                        lock (_stateLock)
                        {
                            _allPlayers = players;
                            _currentMedia = activeSnap;
                            _currentPlayerIdx = activeIdx;
                        }
                    }

                    int cpuVal = _cpuUsage;
                    int ramVal = _ramUsage;
                    string netTxt = _netSpeedText;

                    if (_config.show_telemetry)
                    {
                        try
                        {
                            if (cpuCounter != null) cpuVal = Math.Max(1, Math.Min(100, (int)Math.Round(cpuCounter.NextValue())));
                            var mem = new NativeWin32.MEMORYSTATUSEX { dwLength = (uint)Marshal.SizeOf(typeof(NativeWin32.MEMORYSTATUSEX)) };
                            if (NativeWin32.GlobalMemoryStatusEx(ref mem)) ramVal = (int)mem.dwMemoryLoad;

                            long recv, sent;
                            SampleNetworkTotals(out recv, out sent);
                            long now = Environment.TickCount;
                            float dt = Math.Max(0.2f, (now - lastNetTick) / 1000f);
                            float downBps = Math.Max(0f, (recv - lastBytesRecv) / dt);
                            float upBps = Math.Max(0f, (sent - lastBytesSent) / dt);
                            lastBytesRecv = recv; lastBytesSent = sent; lastNetTick = now;
                            netTxt = string.Format("↓ {0} ↑ {1}", FormatRate(downBps), FormatRate(upBps));

                            // MODULE 5: Download Watchdog traffic & countdown tracking
                            bool wasCountdown = _watchdog.IsCountdownActive;
                            _watchdog.UpdateTrafficSample(downBps, dt, _config.download_threshold_mbps);
                            if (!wasCountdown && _watchdog.IsCountdownActive)
                            {
                                TimerSoundEngine.PlayAlert(_config);
                                _pendingLiveActivityBanner = "⚠ Загрузка завершена! " + _config.watchdog_action + " через 15 сек";
                            }
                            else if (_watchdog.IsCountdownActive)
                            {
                                if (_watchdog.TickCountdownSecond())
                                {
                                    DownloadWatchdogService.ExecutePowerAction(_config.watchdog_action);
                                }
                            }
                        }
                        catch { }
                    }

                    // MODULE 1: Async Micro-Telemetry (CPU/GPU Temp & Ping every 2s)
                    _microTelemetry.SampleAsyncIfNeeded(cpuVal);

                    // MODULE 2: CoreAudio Microphone Active Session & Mute detection
                    if (_config.enable_mic_indicator)
                    {
                        _audio.RefreshMicrophoneState();
                    }

                    // MODULE 6: Unobtrusive GitHub Releases check (12s after start, then every 6 hours, deferred during fullscreen games)
                    long nowTick = Environment.TickCount;
                    if (!_isFullscreenAppActive)
                    {
                        if (!_initialUpdateCheckDone && (nowTick - _startupTick >= 12000))
                        {
                            _initialUpdateCheckDone = true;
                            _lastAutoUpdateCheckTick = nowTick;
                            _updater.CheckForUpdatesAsync(_config, false);
                        }
                        else if (_initialUpdateCheckDone && (nowTick - _lastAutoUpdateCheckTick >= 6 * 3600 * 1000))
                        {
                            _lastAutoUpdateCheckTick = nowTick;
                            _updater.CheckForUpdatesAsync(_config, false);
                        }
                    }

                    bool stateActuallyChanged = false;
                    lock (_stateLock)
                    {
                        if (_clockStr != clockNow || _batteryPct != batPct || _batteryCharging != batChg)
                            stateActuallyChanged = true;
                        if (_mode == IslandMode.Expanded && (_cpuUsage != cpuVal || _ramUsage != ramVal || _netSpeedText != netTxt))
                            stateActuallyChanged = true;
                        if (IsNeonTelemetryGameModeActive())
                            stateActuallyChanged = true;

                        _clockStr = clockNow;
                        _batteryPct = batPct;
                        _batteryCharging = batChg;
                        _hasBattery = hasBat;
                        _cpuUsage = cpuVal;
                        _ramUsage = ramVal;
                        _netSpeedText = netTxt;
                    }
                    if (stateActuallyChanged && (_mode != IslandMode.Expanded || _expandedPage != 1))
                    {
                        _bgStateDirty = true;
                    }
                }
                catch { }

                int waitMs = _hiddenByFullscreen ? 3500 : ((_mode == IslandMode.Idle && !_watchdog.IsWatchdogArmed && !_currentMedia.IsPlaying) ? 1800 : 1000);
                _bgWakeEvent.WaitOne(waitMs);
            }
            if (cpuCounter != null) { try { cpuCounter.Dispose(); } catch { } }
        }

        private static void SampleNetworkTotals(out long recv, out long sent)
        {
            recv = 0; sent = 0;
            try
            {
                foreach (NetworkInterface ni in NetworkInterface.GetAllNetworkInterfaces())
                {
                    if (ni.OperationalStatus == OperationalStatus.Up && ni.NetworkInterfaceType != NetworkInterfaceType.Loopback)
                    {
                        IPv4InterfaceStatistics stats = ni.GetIPv4Statistics();
                        recv += stats.BytesReceived;
                        sent += stats.BytesSent;
                    }
                }
            }
            catch { }
        }

        private static string FormatRate(float bps)
        {
            if (bps < 1024f) return "0 KB/s";
            float kb = bps / 1024f;
            return kb < 1024f ? string.Format("{0:0} KB/s", kb) : string.Format("{0:0.1} MB/s", kb / 1024f);
        }

        private static string FormatTimeMmSs(double seconds)
        {
            if (seconds <= 0 || double.IsNaN(seconds) || double.IsInfinity(seconds)) return "0:00";
            int total = Math.Max(0, (int)Math.Floor(seconds));
            return string.Format("{0}:{1:00}", total / 60, total % 60);
        }

        private void OnPomodoroSecond(object sender, EventArgs e)
        {
            if (!_pomoRunning) return;
            if (_pomoRemainingSec > 0) { _pomoRemainingSec--; RenderLayeredFrame(); }
            if (_pomoRemainingSec <= 0)
            {
                _pomoRunning = false;
                _pomoTimer.Stop();
                TimerSoundEngine.PlayAlert(_config);
                TriggerLiveActivity("🔔 Таймер завершен!", 3200);
                RenderLayeredFrame();
            }
        }

        private void TogglePomodoro()
        {
            if (_pomoRunning) { _pomoRunning = false; _pomoTimer.Stop(); }
            else
            {
                if (_pomoRemainingSec <= 0) _pomoRemainingSec = Math.Max(60, _config.pomodoro_minutes * 60);
                _pomoRunning = true;
                _pomoTimer.Start();
            }
            RenderLayeredFrame();
        }

        protected override void OnMouseWheel(MouseEventArgs e)
        {
            base.OnMouseWheel(e);
            if (!CurrentPillRect().Contains(e.X, e.Y) || e.Delta == 0) return;

            if (_lctrlHeld && _config.enable_lctrl_resize)
            {
                int step = e.Delta > 0 ? 5 : -5;
                _config.ui_scale_percent = Math.Max(80, Math.Min(150, _config.ui_scale_percent + step));
                EnsureCachedFonts();
                ConfigManager.Save(_config);
                RenderLayeredFrame();
                return;
            }

            PointF pt = new PointF(e.X, e.Y);
            if (_mode == IslandMode.Expanded && _rcMusicVolCard.Contains(pt))
            {
                _audio.SetMusicVolume(_audio.LastMusicVolume + (e.Delta > 0 ? 2 : -2), _currentMedia.AppId);
                RenderLayeredFrame();
                return;
            }

            _audio.SetVolume(_audio.LastVolume + (e.Delta > 0 ? 2 : -2));
            if (_mode != IslandMode.Expanded)
                TriggerLiveActivity("Звук ПК  " + _audio.LastVolume + "%");
            else
                RenderLayeredFrame();
        }

        protected override void OnMouseDown(MouseEventArgs e)
        {
            base.OnMouseDown(e);
            if (e.Button == MouseButtons.Left) { _isMousePressed = true; if (_mode == IslandMode.Expanded) RenderLayeredFrame(); }
            PointF pt = new PointF(e.X, e.Y);
            RectangleF pill = CurrentPillRect();
            RectangleF hitPill = RectangleF.Inflate(pill, _lctrlHeld ? 12f : 2f, _lctrlHeld ? 12f : 2f);
            if (!hitPill.Contains(pt)) return;

            if (_lctrlHeld && _config.enable_lctrl_resize)
            {
                if (e.Button == MouseButtons.Middle)
                {
                    _config.idle_width = 310; _config.idle_height = 36;
                    _config.expanded_width = 520; _config.expanded_height = 224;
                    _config.ui_scale_percent = 100;
                    EnsureCachedFonts();
                    _curW = (_mode == IslandMode.Expanded) ? _config.expanded_width : _config.idle_width;
                    _curH = (_mode == IslandMode.Expanded) ? _config.expanded_height : _config.idle_height;
                    _targetW = _curW; _targetH = _curH;
                    ConfigManager.Save(_config);
                    RenderLayeredFrame();
                    return;
                }
                if (e.Button == MouseButtons.Left)
                {
                    _isResizingWithLCtrl = true;
                    _isAnimating = false;
                    _resizeDragStartPt = Cursor.Position;
                    _resizeStartW = (int)Math.Round(_curW);
                    _resizeStartH = (int)Math.Round(_curH);
                    _resizeHorizontalSign = (e.X < CanvasW / 2) ? -1 : 1;
                    return;
                }
            }

            if (_mode != IslandMode.Expanded)
            {
                if (e.Button == MouseButtons.Left)
                {
                    if (_watchdog.IsCountdownActive)
                    {
                        _watchdog.CancelCountdown();
                        TriggerLiveActivity("✓ Авто-сон после загрузки отменён", 2000);
                        RenderLayeredFrame();
                        return;
                    }
                    if (_config.enable_mic_indicator && _rcCompactMicBtn.Width > 0 && _rcCompactMicBtn.Contains(pt))
                    {
                        _audio.ToggleMicrophoneMute();
                        TriggerLiveActivity(_audio.IsMicMuted ? "🎙✕ Микрофон отключён (Muted)" : "🎙 Микрофон включён", 1600);
                        RenderLayeredFrame();
                        return;
                    }
                    ExpandIsland();
                }
                else if (e.Button == MouseButtons.Right) OpenSettingsDialog();
                else if (e.Button == MouseButtons.Middle) MultiPlayerMediaController.ActivatePlayerWindowAsync(_currentMedia);
                return;
            }

            if (e.Button == MouseButtons.Left)
            {
                // Cyclic subtle edge arrows (Left '<' and Right '>' at edges of expanded island)
                if (_rcPageLeftArrow.Contains(pt))
                {
                    CycleExpandedPage(-1);
                    return;
                }
                if (_rcPageRightArrow.Contains(pt))
                {
                    CycleExpandedPage(1);
                    return;
                }

                // If Page 3 (Screenshot Session Vault & Download Watchdog & Update Center) is active
                if (_expandedPage == 2)
                {
                    if (_watchdog.IsCountdownActive && _rcWatchdogCancelCountdownBtn.Contains(pt))
                    {
                        _watchdog.CancelCountdown();
                        RenderLayeredFrame();
                        return;
                    }
                    if (_updater.IsUpdateAvailable && !_updater.DismissedForSession)
                    {
                        if (_rcUpdateApplyBtn.Contains(pt))
                        {
                            _updater.StartDownloadAndApplyAsync();
                            RenderLayeredFrame();
                            return;
                        }
                        if (_rcUpdateLaterBtn.Contains(pt))
                        {
                            _updater.DismissedForSession = true;
                            RenderLayeredFrame();
                            return;
                        }
                    }
                    if (_rcVaultDeleteSelectedBtn.Contains(pt) && _screenshotVault.SelectedCount > 0)
                    {
                        _screenshotVault.DeleteSelected();
                        RenderLayeredFrame();
                        return;
                    }
                    if (_rcVaultClearAllBtn.Contains(pt) && _screenshotVault.Count > 0)
                    {
                        _screenshotVault.CleanVaultDirectory();
                        RenderLayeredFrame();
                        return;
                    }
                    foreach (var kv in _vaultCheckRects)
                    {
                        if (kv.Key.Contains(pt))
                        {
                            kv.Value.IsSelected = !kv.Value.IsSelected;
                            RenderLayeredFrame();
                            return;
                        }
                    }
                    foreach (var kv in _vaultThumbRects)
                    {
                        if (kv.Key.Contains(pt))
                        {
                            _pendingDragVaultItem = kv.Value;
                            _vaultDragStartPt = e.Location;
                            if (_screenshotVault.CopyItemToClipboard(kv.Value))
                            {
                                _liveBannerText = "✓ Скриншот скопирован в буфер";
                            }
                            RenderLayeredFrame();
                            return;
                        }
                    }
                    if (_rcWatchdogToggleBtn.Contains(pt))
                    {
                        _watchdog.IsWatchdogArmed = !_watchdog.IsWatchdogArmed;
                        if (!_watchdog.IsWatchdogArmed) _watchdog.CancelCountdown();
                        RenderLayeredFrame();
                        return;
                    }
                    if (_rcWatchdogActionBtn.Contains(pt))
                    {
                        string[] acts = { "Сон", "Гибернация", "Выключение" };
                        int idx = Array.IndexOf(acts, _config.watchdog_action);
                        _config.watchdog_action = acts[(idx + 1) % acts.Length];
                        ConfigManager.Save(_config);
                        RenderLayeredFrame();
                        return;
                    }
                    return;
                }

                // If Page 2 (Live Screen-Share / Stream Viewer) is active, handle Page 2 controls
                if (_expandedPage == 1)
                {
                    if (_rcStreamSourceBtn.Contains(pt))
                    {
                        _streamEngine.CycleSource(Handle, 1);
                        _streamEngine.CaptureFrame(Handle);
                        RenderLayeredFrame();
                        return;
                    }
                    if (_rcStreamCropBtn.Contains(pt))
                    {
                        _streamEngine.CropModeIndex = _streamEngine.CropModeIndex + 1;
                        _dwmOverlay.RefreshCropOrSource(Handle, _streamEngine.GetActiveSource(Handle), _streamEngine.CropModeIndex);
                        RenderLayeredFrame();
                        return;
                    }
                    if (_rcStreamOpenBtn.Contains(pt))
                    {
                        var src = _streamEngine.GetActiveSource(Handle);
                        if (src != null && src.Hwnd != IntPtr.Zero)
                        {
                            _dwmOverlay.RestoreParkedWindow(true);
                            NativeWin32.ForceForegroundWindow(src.Hwnd);
                        }
                        return;
                    }
                    if (_rcStreamViewport.Contains(pt))
                    {
                        // Single click on viewport refreshes / cycles crop if no source, or brings stream audio/window
                        _streamEngine.CaptureFrame(Handle);
                        RenderLayeredFrame();
                        return;
                    }
                    return;
                }

                if (_rcPlayerBadge.Contains(pt))
                {
                    if (_allPlayers.Count > 1)
                    {
                        MultiPlayerMediaController.CycleNextPlayer(_allPlayers);
                        _bgStateDirty = true;
                    }
                    else
                    {
                        MultiPlayerMediaController.ActivatePlayerWindowAsync(_currentMedia);
                    }
                    return;
                }

                if (_rcSongClickZone.Contains(pt))
                {
                    MultiPlayerMediaController.ActivatePlayerWindowAsync(_currentMedia);
                    _isHovered = false;
                    CollapseToIdle();
                    return;
                }

                if (_rcLikeBtn.Contains(pt)) { ToggleCurrentTrackFavorite(); return; }
                if (_rcPrevBtn.Contains(pt)) { MultiPlayerMediaController.ExecuteTransportCommandAsync(_currentMedia, "prev"); return; }
                if (_rcPlayBtn.Contains(pt))
                {
                    MultiPlayerMediaController.ExecuteTransportCommandAsync(_currentMedia, "play_pause");
                    _currentMedia.IsPlaying = !_currentMedia.IsPlaying;
                    _currentMedia.PositionSec = _currentMedia.GetLivePositionSec();
                    _currentMedia.SampledAtUtc = DateTime.UtcNow;
                    RenderLayeredFrame();
                    return;
                }
                if (_rcNextBtn.Contains(pt)) { MultiPlayerMediaController.ExecuteTransportCommandAsync(_currentMedia, "next"); return; }

                RectangleF seekHit = RectangleF.Inflate(_rcProgressBar, 2f, 6f);
                if (seekHit.Contains(pt) && _rcProgressBar.Width > 0)
                {
                    _draggingSeek = true;
                    UpdateSeekFromCursor(pt.X, false);
                    return;
                }

                if (_rcMuteBtn.Contains(pt)) { _audio.ToggleMute(); RenderLayeredFrame(); return; }
                RectangleF volHit = RectangleF.Inflate(_rcVolSlider, 2f, 7f);
                if (volHit.Contains(pt)) { _draggingVolume = true; UpdateVolumeFromCursor(pt.X); return; }

                if (_rcMusicMuteBtn.Contains(pt)) { _audio.ToggleMusicMute(_currentMedia.AppId); RenderLayeredFrame(); return; }
                RectangleF musicVolHit = RectangleF.Inflate(_rcMusicVolSlider, 2f, 7f);
                if (musicVolHit.Contains(pt)) { _draggingMusicVolume = true; UpdateMusicVolumeFromCursor(pt.X); return; }

                if (_rcPomoBtn.Contains(pt)) { TogglePomodoro(); return; }
                if (_rcSettingsBtn.Contains(pt)) { OpenSettingsDialog(); return; }

                foreach (var kv in _dockRects)
                {
                    if (kv.Key.Contains(pt)) { LaunchPinnedApp(kv.Value); return; }
                }
                if (_rcAddAppBtn.Contains(pt))
                {
                    PromptAddAppViaExplorer();
                    return;
                }
                if (_rcExpandedMicBtn.Width > 0 && _rcExpandedMicBtn.Contains(pt))
                {
                    _audio.ToggleMicrophoneMute();
                    RenderLayeredFrame();
                    return;
                }
                if (_rcExpandedVaultBtn.Width > 0 && _rcExpandedVaultBtn.Contains(pt))
                {
                    _expandedPage = 2;
                    _dwmOverlay.HideStream();
                    RenderLayeredFrame();
                    return;
                }
                if (_rcExpandedWatchdogBtn.Width > 0 && _rcExpandedWatchdogBtn.Contains(pt))
                {
                    if (_watchdog.IsCountdownActive) _watchdog.CancelCountdown();
                    else _watchdog.IsWatchdogArmed = !_watchdog.IsWatchdogArmed;
                    RenderLayeredFrame();
                    return;
                }

                if (_config.show_dropzone && _rcDropzone.Contains(pt) && _dropzoneFiles.Count > 0)
                {
                    DoDragDrop(new DataObject(DataFormats.FileDrop, _dropzoneFiles.ToArray()), DragDropEffects.Copy);
                    return;
                }
            }
            else if (e.Button == MouseButtons.Right)
            {
                if (_expandedPage == 1)
                {
                    ShowStreamSourceMenu(PointToScreen(e.Location));
                    return;
                }
                if (_rcPlayerBadge.Contains(pt) || _rcSongClickZone.Contains(pt))
                {
                    ShowPlayerSelectionMenu(PointToScreen(e.Location));
                    return;
                }
                foreach (var kv in _dockRects)
                {
                    if (kv.Key.Contains(pt)) { ShowDockContextMenu(kv.Value, PointToScreen(e.Location)); return; }
                }
                if (_config.show_dropzone && _rcDropzone.Contains(pt) && _dropzoneFiles.Count > 0)
                {
                    _dropzoneFiles.Clear();
                    RenderLayeredFrame();
                    return;
                }
                OpenSettingsDialog();
            }
        }

        protected override void OnMouseMove(MouseEventArgs e)
        {
            base.OnMouseMove(e);
            if (_isResizingWithLCtrl && (e.Button & MouseButtons.Left) == MouseButtons.Left)
            {
                Point curScreen = Cursor.Position;
                int dx = (curScreen.X - _resizeDragStartPt.X) * _resizeHorizontalSign;
                int dy = curScreen.Y - _resizeDragStartPt.Y;

                int newW = _resizeStartW + dx * 2;
                int newH = _resizeStartH + dy;

                if (_mode == IslandMode.Expanded)
                {
                    _config.expanded_width = Math.Max(360, Math.Min(CanvasW - 24, newW));
                    _config.expanded_height = Math.Max(165, Math.Min(CanvasH - 24, newH));
                    _curW = _config.expanded_width;
                    _curH = _config.expanded_height;
                }
                else
                {
                    _config.idle_width = Math.Max(160, Math.Min(480, newW));
                    _config.idle_height = Math.Max(26, Math.Min(68, newH));
                    _curW = _config.idle_width;
                    _curH = _config.idle_height;
                }
                _targetW = _curW;
                _targetH = _curH;
                RenderLayeredFrame();
                return;
            }

            if (_draggingSeek && (e.Button & MouseButtons.Left) == MouseButtons.Left)
            {
                UpdateSeekFromCursor(e.X, false);
                return;
            }

            if (_draggingVolume && (e.Button & MouseButtons.Left) == MouseButtons.Left)
            {
                UpdateVolumeFromCursor(e.X);
                return;
            }
            if (_draggingMusicVolume && (e.Button & MouseButtons.Left) == MouseButtons.Left)
            {
                UpdateMusicVolumeFromCursor(e.X);
                return;
            }
            if (_pendingDragVaultItem != null && (e.Button & MouseButtons.Left) == MouseButtons.Left)
            {
                int dx = Math.Abs(e.X - _vaultDragStartPt.X);
                int dy = Math.Abs(e.Y - _vaultDragStartPt.Y);
                if (dx > 5 || dy > 5)
                {
                    var itemToDrag = _pendingDragVaultItem;
                    _pendingDragVaultItem = null;
                    if (itemToDrag != null && File.Exists(itemToDrag.FilePath))
                    {
                        try
                        {
                            var dobj = new DataObject(DataFormats.FileDrop, new string[] { itemToDrag.FilePath });
                            DoDragDrop(dobj, DragDropEffects.Copy);
                        }
                        catch { }
                    }
                    return;
                }
            }
        }

        protected override void OnMouseUp(MouseEventArgs e)
        {
            base.OnMouseUp(e);
            if (_isMousePressed) { _isMousePressed = false; if (_mode == IslandMode.Expanded) RenderLayeredFrame(); }
            if (_isResizingWithLCtrl)
            {
                _isResizingWithLCtrl = false;
                ConfigManager.Save(_config);
                RenderLayeredFrame();
            }
            if (_draggingSeek)
            {
                _draggingSeek = false;
                UpdateSeekFromCursor(e.X, true);
            }
            _draggingVolume = false;
            _draggingMusicVolume = false;
            _pendingDragVaultItem = null;
        }

        private void UpdateMusicVolumeFromCursor(float x)
        {
            if (_rcMusicVolSlider.Width <= 0) return;
            float ratio = Math.Max(0f, Math.Min(1f, (x - _rcMusicVolSlider.X) / _rcMusicVolSlider.Width));
            _audio.SetMusicVolume((int)Math.Round(ratio * 100f), _currentMedia.AppId);
            RenderLayeredFrame();
        }

        private void UpdateSeekFromCursor(float x, bool commitToPlayer)
        {
            if (_rcProgressBar.Width <= 0 || _currentMedia.DurationSec <= 0) return;
            double ratio = Math.Max(0.0, Math.Min(1.0, (x - _rcProgressBar.X) / _rcProgressBar.Width));
            _currentMedia.PositionSec = _currentMedia.DurationSec * ratio;
            _currentMedia.SampledAtUtc = DateTime.UtcNow;
            if (commitToPlayer)
                MultiPlayerMediaController.ExecuteTransportCommandAsync(_currentMedia, "seek", ratio);
            RenderLayeredFrame();
        }

        private void CycleExpandedPage(int dir)
        {
            const int totalPages = 3;
            _expandedPage = (_expandedPage + dir + totalPages) % totalPages;
            _collapseTimer.Stop();
            if (_mode != IslandMode.Expanded)
                ExpandIsland();

            if (_expandedPage == 1 && IsHandleCreated)
            {
                _hiddenByFullscreen = false;
                _streamEngine.GetAvailableSources(Handle);
                _streamEngine.CaptureFrame(Handle);
            }
            RenderLayeredFrame();
        }

        private void ShowStreamSourceMenu(Point screenPt)
        {
            var menu = new ContextMenuStrip();
            var list = _streamEngine.GetAvailableSources(Handle);
            var active = _streamEngine.GetActiveSource(Handle);

            menu.Items.Add("📺 Выбор окна трансляции (Discord / YouTube / Видео):", null, null).Enabled = false;
            menu.Items.Add(new ToolStripSeparator());

            if (list.Count == 0)
            {
                menu.Items.Add("Нет открытых окон для захвата").Enabled = false;
            }
            else
            {
                foreach (var s in list)
                {
                    var itemSnap = s;
                    string lbl = "[" + itemSnap.CategoryLabel + "]  " + itemSnap.WindowTitle;
                    if (lbl.Length > 68) lbl = lbl.Substring(0, 67) + "…";
                    var mi = menu.Items.Add(lbl, null, (sender, args) =>
                    {
                        _streamEngine.SelectSource(itemSnap.Hwnd);
                        _streamEngine.CaptureFrame(Handle);
                        RenderLayeredFrame();
                    });
                    if (active != null && active.Hwnd == itemSnap.Hwnd)
                        mi.Font = new Font(menu.Font, FontStyle.Bold);
                }
            }

            menu.Items.Add(new ToolStripSeparator());
            menu.Items.Add("Кадрирование: " + _streamEngine.CurrentCropLabel + " (Сменить)", null, (s, e) =>
            {
                _streamEngine.CropModeIndex = _streamEngine.CropModeIndex + 1;
                _streamEngine.CaptureFrame(Handle);
                RenderLayeredFrame();
            });
            menu.Items.Add("← Вернуться на страницу плеера (Страница 1)", null, (s, e) => CycleExpandedPage(-1));
            menu.Items.Add("Открыть пульт Kinetic Stage & Deck...", null, (s, e) => OpenSettingsDialog());
            menu.Show(screenPt);
        }

        private void ShowPlayerSelectionMenu(Point screenPt)
        {
            var menu = new ContextMenuStrip();
            menu.Items.Add("↗ Открыть окно текущего плеера (" + _currentMedia.DisplayAppName + ")", null, (s, e) =>
            {
                MultiPlayerMediaController.ActivatePlayerWindowAsync(_currentMedia);
            });
            menu.Items.Add(new ToolStripSeparator());

            if (_allPlayers.Count == 0)
            {
                var emptyItem = menu.Items.Add("Нет активных медиа-плееров");
                emptyItem.Enabled = false;
            }
            else
            {
                foreach (var p in _allPlayers)
                {
                    var playerSnap = p;
                    string prefix = playerSnap.IsPlaying ? "▶ " : "⏸ ";
                    string label = prefix + playerSnap.DisplayAppName + " — " + playerSnap.Title;
                    var item = menu.Items.Add(label, null, (s, e) =>
                    {
                        MultiPlayerMediaController.SelectPlayerByAppId(playerSnap.AppId);
                    });
                    if (string.Equals(playerSnap.AppId, _currentMedia.AppId, StringComparison.OrdinalIgnoreCase))
                        item.Font = new Font(menu.Font, FontStyle.Bold);
                }
            }
            menu.Show(screenPt);
        }

        private void UpdateVolumeFromCursor(float x)
        {
            if (_rcVolSlider.Width <= 0) return;
            float ratio = Math.Max(0f, Math.Min(1f, (x - _rcVolSlider.X) / _rcVolSlider.Width));
            _audio.SetVolume((int)Math.Round(ratio * 100f));
            RenderLayeredFrame();
        }

        protected override void OnDragEnter(DragEventArgs drgevent)
        {
            base.OnDragEnter(drgevent);
            if (drgevent.Data.GetDataPresent(DataFormats.FileDrop) || drgevent.Data.GetDataPresent(DataFormats.Text))
            {
                drgevent.Effect = DragDropEffects.Copy;
                ExpandIsland();
            }
        }

        protected override void OnDragDrop(DragEventArgs drgevent)
        {
            base.OnDragDrop(drgevent);
            if (drgevent.Data.GetDataPresent(DataFormats.FileDrop))
            {
                string[] files = (string[])drgevent.Data.GetData(DataFormats.FileDrop);
                if (files != null)
                {
                    foreach (string f in files)
                    {
                        if (string.IsNullOrWhiteSpace(f)) continue;
                        string ext = Path.GetExtension(f).ToLowerInvariant();
                        if (Directory.Exists(f) || ext == ".exe" || ext == ".lnk" || ext == ".url" || ext == ".bat" || ext == ".cmd")
                            PinTargetToDock(f);
                        else if (File.Exists(f) && !_dropzoneFiles.Contains(f))
                            _dropzoneFiles.Add(f);
                    }
                }
            }
            else if (drgevent.Data.GetDataPresent(DataFormats.Text))
            {
                string txt = (drgevent.Data.GetData(DataFormats.Text) as string ?? "").Trim();
                if (txt.StartsWith("http://", StringComparison.OrdinalIgnoreCase) || txt.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
                    PinTargetToDock(txt);
            }
            RenderLayeredFrame();
        }

        private string CurrentTrackFavoriteKey()
        {
            string t = (_currentMedia.Title ?? "").Trim();
            string a = (_currentMedia.Artist ?? "").Trim();
            return (t + " — " + a).ToLowerInvariant();
        }

        private bool IsCurrentTrackLiked()
        {
            if (_config.liked_tracks == null) return false;
            string key = CurrentTrackFavoriteKey();
            if (string.IsNullOrWhiteSpace(key) || key == " — ") return false;
            foreach (string s in _config.liked_tracks)
                if (string.Equals(s, key, StringComparison.OrdinalIgnoreCase)) return true;
            return false;
        }

        private void ToggleCurrentTrackFavorite()
        {
            if (_config.liked_tracks == null) _config.liked_tracks = new List<string>();
            string key = CurrentTrackFavoriteKey();
            if (string.IsNullOrWhiteSpace(key)) return;

            bool removed = false;
            for (int i = _config.liked_tracks.Count - 1; i >= 0; i--)
            {
                if (string.Equals(_config.liked_tracks[i], key, StringComparison.OrdinalIgnoreCase))
                {
                    _config.liked_tracks.RemoveAt(i);
                    removed = true;
                }
            }
            if (!removed)
                _config.liked_tracks.Add(key);

            ConfigManager.Save(_config);
            MultiPlayerMediaController.ToggleFavoriteInPlayerAsync(_currentMedia);
            RenderLayeredFrame();
        }

        private void PromptAddAppViaExplorer()
        {
            using (var ofd = new OpenFileDialog())
            {
                ofd.Title = "Выберите программу (.exe / .lnk) для добавления в Dynamic Island";
                ofd.Filter = "Программы и ярлыки (*.exe;*.lnk;*.bat;*.cmd;*.url)|*.exe;*.lnk;*.bat;*.cmd;*.url|Все файлы (*.*)|*.*";
                ofd.Multiselect = true;
                if (ofd.ShowDialog(this) == DialogResult.OK)
                {
                    foreach (string file in ofd.FileNames)
                    {
                        if (!string.IsNullOrWhiteSpace(file) && File.Exists(file))
                            PinTargetToDock(file);
                    }
                    RenderLayeredFrame();
                }
            }
        }

        private void PinTargetToDock(string targetPath)
        {
            foreach (var item in _config.pinned_apps)
                if (string.Equals(item.target_path, targetPath, StringComparison.OrdinalIgnoreCase)) return;
            string name = targetPath.StartsWith("http", StringComparison.OrdinalIgnoreCase) ? "WebLink" : Path.GetFileNameWithoutExtension(targetPath);
            if (string.IsNullOrWhiteSpace(name)) name = Path.GetFileName(targetPath);
            _config.pinned_apps.Add(new PinnedAppItem { name = string.IsNullOrWhiteSpace(name) ? "App" : name, target_path = targetPath, kind = Directory.Exists(targetPath) ? "folder" : "app" });
            ConfigManager.Save(_config);
        }

        private void LaunchPinnedApp(PinnedAppItem item)
        {
            try { Process.Start(new ProcessStartInfo(item.target_path) { UseShellExecute = true }); } catch { }
        }

        private void ShowDockContextMenu(PinnedAppItem item, Point screenPt)
        {
            var menu = new ContextMenuStrip();
            menu.Items.Add("Запустить " + item.name, null, (s, e) => LaunchPinnedApp(item));
            menu.Items.Add("Открепить из дока", null, (s, e) =>
            {
                _config.pinned_apps.RemoveAll(x => string.Equals(x.target_path, item.target_path, StringComparison.OrdinalIgnoreCase));
                ConfigManager.Save(_config);
                RenderLayeredFrame();
            });
            menu.Show(screenPt);
        }

        public void OpenSettingsDialog()
        {
            using (var dlg = new SettingsForm(_config))
            {
                dlg.SettingsSaved += () =>
                {
                    _bgEngine.EnsureLoaded(_config.custom_bg_path);
                    EnsureCachedFonts();
                    _collapseTimer.Interval = Math.Max(100, _config.collapse_delay_ms);
                    _curW = (_mode == IslandMode.Expanded) ? _config.expanded_width : _config.idle_width;
                    _curH = (_mode == IslandMode.Expanded) ? _config.expanded_height : _config.idle_height;
                    _targetW = _curW;
                    _targetH = _curH;
                    PositionOnMonitor(_config.monitor_index);
                    RenderLayeredFrame();
                };
                dlg.ManualUpdateCheckRequested += () =>
                {
                    _updater.CheckForUpdatesAsync(_config, true);
                    _expandedPage = 2;
                    ExpandIsland();
                    RenderLayeredFrame();
                };
                dlg.ShowDialog(this);
            }
        }

        private Bitmap GetAppIconBitmap(string path)
        {
            if (_iconCache.ContainsKey(path)) return _iconCache[path];
            Bitmap bmp = null;
            try
            {
                if (File.Exists(path))
                {
                    Icon ico = Icon.ExtractAssociatedIcon(path);
                    if (ico != null)
                    {
                        bmp = new Bitmap(22, 22, PixelFormat.Format32bppPArgb);
                        using (Graphics g = Graphics.FromImage(bmp))
                        {
                            g.InterpolationMode = InterpolationMode.HighQualityBicubic;
                            g.DrawIcon(ico, new Rectangle(0, 0, 22, 22));
                        }
                        ico.Dispose();
                    }
                }
            }
            catch { }
            _iconCache[path] = bmp;
            return bmp;
        }        // ---------------------------------------------------------------------
        // Apple Modern Minimalism Bento Renderer (Vector Icons + Zero-Copy DIB)
        // ---------------------------------------------------------------------
        private void GetThemeAccentColors(int alpha, out Color c1, out Color c2)
        {
            string preset = _config.theme_preset ?? "Dinasmic Neon";
            if (preset == "Dinasmic Neon") { c1 = Color.FromArgb(alpha, 34, 211, 238); c2 = Color.FromArgb(alpha, 236, 72, 153); }
            else if (preset == "Apple Blue") { c1 = Color.FromArgb(alpha, 10, 132, 255); c2 = Color.FromArgb(alpha, 139, 92, 246); }
            else if (preset == "Yandex Gold-Yellow") { c1 = Color.FromArgb(alpha, 255, 214, 10); c2 = Color.FromArgb(alpha, 255, 159, 10); }
            else if (preset == "Spotify Neon Green") { c1 = Color.FromArgb(alpha, 48, 209, 88); c2 = Color.FromArgb(alpha, 34, 211, 238); }
            else if (preset == "Crimson Sunset") { c1 = Color.FromArgb(alpha, 255, 55, 95); c2 = Color.FromArgb(alpha, 255, 159, 10); }
            else if (preset == "Minimal White") { c1 = Color.FromArgb(alpha, 245, 245, 247); c2 = Color.FromArgb(alpha, 174, 174, 178); }
            else { c1 = Color.FromArgb(alpha, 34, 211, 238); c2 = Color.FromArgb(alpha, 192, 132, 252); }
        }

        private RectangleF ApplyMicroInteraction(Graphics g, RectangleF rc, float cornerRadius, int alpha, Color accentCol, bool isCircle = false)
        {
            bool hovered = rc.Width > 0 && rc.Contains(_clientCursorPt);
            if (!hovered) return rc;

            bool pressed = _isMousePressed;
            RectangleF adj = pressed ? RectangleF.Inflate(rc, -1.2f, -1.2f) : rc;
            int glowA = (int)(alpha * (pressed ? 0.24f : 0.14f));
            int rimA = (int)(alpha * (pressed ? 0.60f : 0.38f));

            using (var hBr = new SolidBrush(Color.FromArgb(glowA, accentCol.R, accentCol.G, accentCol.B)))
            using (var hPen = new Pen(Color.FromArgb(rimA, accentCol.R, accentCol.G, accentCol.B), 1.0f))
            {
                if (isCircle)
                {
                    g.FillEllipse(hBr, adj);
                    g.DrawEllipse(hPen, adj);
                }
                else
                {
                    using (GraphicsPath hp = CreateRoundedRectPath(adj, cornerRadius))
                    {
                        g.FillPath(hBr, hp);
                        g.DrawPath(hPen, hp);
                    }
                }
            }
            return adj;
        }

        private static void DrawVectorMicIcon(Graphics g, RectangleF rc, bool muted, Color color)
        {
            float cx = rc.X + rc.Width * 0.5f;
            float cy = rc.Y + rc.Height * 0.5f;
            using (var pen = new Pen(color, 1.35f) { StartCap = LineCap.Round, EndCap = LineCap.Round })
            using (var br = new SolidBrush(color))
            {
                var headRc = new RectangleF(cx - 2.3f, cy - 5.4f, 4.6f, 6.8f);
                using (GraphicsPath hp = CreateRoundedRectPath(headRc, 2.3f))
                    g.FillPath(br, hp);
                g.DrawArc(pen, cx - 4.4f, cy - 3.6f, 8.8f, 7.2f, 0, 180);
                g.DrawLine(pen, cx, cy + 3.6f, cx, cy + 5.6f);
                g.DrawLine(pen, cx - 2.4f, cy + 5.6f, cx + 2.4f, cy + 5.6f);
                if (muted)
                {
                    using (var slashPen = new Pen(Color.FromArgb(color.A, 255, 69, 58), 1.65f) { StartCap = LineCap.Round, EndCap = LineCap.Round })
                        g.DrawLine(slashPen, cx - 5.2f, cy - 4.8f, cx + 5.2f, cy + 4.8f);
                }
            }
        }

        private static void DrawVectorCameraIcon(Graphics g, RectangleF rc, Color color)
        {
            float cx = rc.X + rc.Width * 0.5f;
            float cy = rc.Y + rc.Height * 0.5f + 0.5f;
            using (var pen = new Pen(color, 1.35f) { LineJoin = LineJoin.Round })
            {
                var body = new RectangleF(cx - 5.5f, cy - 3.6f, 11.0f, 7.6f);
                using (GraphicsPath bp = CreateRoundedRectPath(body, 2.0f))
                    g.DrawPath(pen, bp);
                g.DrawLine(pen, cx - 2.4f, cy - 5.0f, cx + 2.4f, cy - 5.0f);
                g.DrawEllipse(pen, cx - 2.2f, cy - 2.0f, 4.4f, 4.4f);
            }
        }

        private static void DrawVectorMoonIcon(Graphics g, RectangleF rc, Color color)
        {
            float cx = rc.X + rc.Width * 0.5f;
            float cy = rc.Y + rc.Height * 0.5f;
            using (var pen = new Pen(color, 1.4f) { StartCap = LineCap.Round, EndCap = LineCap.Round })
            {
                g.DrawArc(pen, cx - 4.5f, cy - 4.5f, 9.0f, 9.0f, 45, 265);
                g.DrawArc(pen, cx - 1.8f, cy - 4.2f, 7.2f, 7.2f, 95, 185);
            }
        }

        private static void DrawVectorMusicNoteIcon(Graphics g, RectangleF rc, bool muted, Color color)
        {
            float cx = rc.X + rc.Width * 0.5f;
            float cy = rc.Y + rc.Height * 0.5f;
            using (var pen = new Pen(color, 1.35f) { StartCap = LineCap.Round, EndCap = LineCap.Round })
            using (var br = new SolidBrush(color))
            {
                g.FillEllipse(br, cx - 4.6f, cy + 1.2f, 3.8f, 3.0f);
                g.FillEllipse(br, cx + 0.8f, cy + 0.2f, 3.8f, 3.0f);
                g.DrawLine(pen, cx - 1.1f, cy + 2.5f, cx - 1.1f, cy - 4.2f);
                g.DrawLine(pen, cx + 4.3f, cy + 1.5f, cx + 4.3f, cy - 5.0f);
                g.DrawLine(pen, cx - 1.1f, cy - 4.2f, cx + 4.3f, cy - 5.0f);
                if (muted)
                {
                    using (var sp = new Pen(Color.FromArgb(color.A, 255, 69, 58), 1.6f) { StartCap = LineCap.Round, EndCap = LineCap.Round })
                        g.DrawLine(sp, cx - 5.0f, cy - 4.5f, cx + 5.0f, cy + 4.5f);
                }
            }
        }

        private static GraphicsPath CreateRoundedRectPath(RectangleF rect, float radius)
        {
            float d = Math.Min(radius * 2f, Math.Min(rect.Width, rect.Height));
            var path = new GraphicsPath();
            if (d <= 1f) { path.AddRectangle(rect); return path; }
            path.AddArc(rect.X, rect.Y, d, d, 180, 90);
            path.AddArc(rect.Right - d, rect.Y, d, d, 270, 90);
            path.AddArc(rect.Right - d, rect.Bottom - d, d, d, 0, 90);
            path.AddArc(rect.X, rect.Bottom - d, d, d, 90, 90);
            path.CloseFigure();
            return path;
        }

        private static void DrawVectorSpeakerIcon(Graphics g, RectangleF rc, bool muted, int volume, Color color)
        {
            float cx = rc.X + rc.Width * 0.42f;
            float cy = rc.Y + rc.Height * 0.5f;
            using (var br = new SolidBrush(color))
            using (var pen = new Pen(color, 1.35f) { StartCap = LineCap.Round, EndCap = LineCap.Round })
            {
                PointF[] cone = new PointF[]
                {
                    new PointF(cx - 4.5f, cy - 2.2f),
                    new PointF(cx - 1.8f, cy - 2.2f),
                    new PointF(cx + 1.8f, cy - 5.0f),
                    new PointF(cx + 1.8f, cy + 5.0f),
                    new PointF(cx - 1.8f, cy + 2.2f),
                    new PointF(cx - 4.5f, cy + 2.2f)
                };
                g.FillPolygon(br, cone);

                if (muted || volume == 0)
                {
                    g.DrawLine(pen, cx + 4.2f, cy - 3.0f, cx + 8.8f, cy + 3.0f);
                    g.DrawLine(pen, cx + 8.8f, cy - 3.0f, cx + 4.2f, cy + 3.0f);
                }
                else
                {
                    g.DrawArc(pen, cx + 1.5f, cy - 3.2f, 4.6f, 6.4f, -52, 104);
                    if (volume >= 50)
                        g.DrawArc(pen, cx + 1.5f, cy - 5.6f, 7.8f, 11.2f, -52, 104);
                }
            }
        }

        private static void DrawVectorTransportIcon(Graphics g, RectangleF rc, string kind, Color color)
        {
            float cx = rc.X + rc.Width * 0.5f;
            float cy = rc.Y + rc.Height * 0.5f;
            using (var br = new SolidBrush(color))
            {
                if (kind == "play")
                {
                    PointF[] tri = new PointF[]
                    {
                        new PointF(cx - 3.2f, cy - 5.2f),
                        new PointF(cx + 5.4f, cy),
                        new PointF(cx - 3.2f, cy + 5.2f)
                    };
                    g.FillPolygon(br, tri);
                }
                else if (kind == "pause")
                {
                    g.FillRectangle(br, cx - 4.2f, cy - 5.0f, 2.8f, 10.0f);
                    g.FillRectangle(br, cx + 1.4f, cy - 5.0f, 2.8f, 10.0f);
                }
                else if (kind == "prev")
                {
                    g.FillRectangle(br, cx - 5.2f, cy - 4.2f, 1.8f, 8.4f);
                    PointF[] tri = new PointF[]
                    {
                        new PointF(cx + 4.2f, cy - 4.2f),
                        new PointF(cx - 2.6f, cy),
                        new PointF(cx + 4.2f, cy + 4.2f)
                    };
                    g.FillPolygon(br, tri);
                }
                else if (kind == "next")
                {
                    g.FillRectangle(br, cx + 3.4f, cy - 4.2f, 1.8f, 8.4f);
                    PointF[] tri = new PointF[]
                    {
                        new PointF(cx - 4.2f, cy - 4.2f),
                        new PointF(cx + 2.6f, cy),
                        new PointF(cx - 4.2f, cy + 4.2f)
                    };
                    g.FillPolygon(br, tri);
                }
            }
        }

        private static void DrawVectorHeartIcon(Graphics g, RectangleF rc, bool liked, Color outlineColor, Color fillColor)
        {
            float cx = rc.X + rc.Width * 0.5f;
            float cy = rc.Y + rc.Height * 0.5f + 0.5f;
            using (var path = new GraphicsPath())
            {
                path.AddBezier(
                    cx, cy + 5.2f,
                    cx - 7.2f, cy + 0.6f,
                    cx - 5.4f, cy - 5.6f,
                    cx, cy - 2.2f);
                path.AddBezier(
                    cx, cy - 2.2f,
                    cx + 5.4f, cy - 5.6f,
                    cx + 7.2f, cy + 0.6f,
                    cx, cy + 5.2f);
                path.CloseFigure();

                if (liked)
                {
                    using (var fb = new SolidBrush(fillColor))
                        g.FillPath(fb, path);
                    using (var fp = new Pen(Color.FromArgb(Math.Min(255, (int)fillColor.A), 255, 160, 180), 1.0f))
                        g.DrawPath(fp, path);
                }
                else
                {
                    using (var op = new Pen(outlineColor, 1.45f) { LineJoin = LineJoin.Round })
                        g.DrawPath(op, path);
                }
            }
        }

        private static void DrawVectorGearIcon(Graphics g, RectangleF rc, Color color)
        {
            float cx = rc.X + rc.Width * 0.5f;
            float cy = rc.Y + rc.Height * 0.5f;
            using (var pen = new Pen(color, 1.45f))
            {
                g.DrawEllipse(pen, cx - 4.2f, cy - 4.2f, 8.4f, 8.4f);
                for (int i = 0; i < 6; i++)
                {
                    double ang = i * Math.PI / 3.0;
                    float x1 = cx + (float)Math.Cos(ang) * 4.2f;
                    float y1 = cy + (float)Math.Sin(ang) * 4.2f;
                    float x2 = cx + (float)Math.Cos(ang) * 6.0f;
                    float y2 = cy + (float)Math.Sin(ang) * 6.0f;
                    g.DrawLine(pen, x1, y1, x2, y2);
                }
            }
        }

        private bool ShouldRenderCustomBackground()
        {
            if (!_bgEngine.HasImage) return false;
            string scope = _config.custom_bg_scope ?? "Везде";
            if (scope.Contains("компакт") && _mode == IslandMode.Expanded) return false;
            if (scope.Contains("раскрыт") && _mode != IslandMode.Expanded) return false;
            return true;
        }

        private static bool SafeDrawImage(Graphics g, Image img, RectangleF rect)
        {
            if (g == null || img == null) return false;
            try
            {
                g.DrawImage(img, rect);
                return true;
            }
            catch { return false; }
        }

        private static bool SafeDrawImage(Graphics g, Image img, float x, float y, float w, float h)
        {
            if (g == null || img == null) return false;
            try
            {
                g.DrawImage(img, x, y, w, h);
                return true;
            }
            catch { return false; }
        }

        private void RenderLayeredFrame()
        {
            if (!IsHandleCreated || IsDisposed || _dibGraphics == null) return;

            NativeWin32.ZeroMemory(_dibBits, new IntPtr(CanvasW * CanvasH * 4));
            Graphics g = _dibGraphics;
            try { g.ResetClip(); } catch { }

            try
            {
            if (!_hiddenByFullscreen)
            {
                RectangleF pill = CurrentPillRect();
                float compactRadius = pill.Height / 2f;
                float expRadius = Math.Max(12f, _config.corner_radius);
                float radius = compactRadius * (1f - _curProg) + expRadius * _curProg;

                float[] shSpread = new float[] { 11.0f, 7.0f, 3.8f, 1.5f };
                float[] shOffsetY = new float[] { 4.5f, 3.0f, 1.8f, 0.8f };
                int[] shAlphas = new int[] { 8, 16, 28, 42 };
                for (int si = 0; si < shSpread.Length; si++)
                {
                    float sp = shSpread[si];
                    var shRc = new RectangleF(pill.X - sp, pill.Y - sp * 0.55f + shOffsetY[si], pill.Width + sp * 2f, pill.Height + sp * 1.45f);
                    using (GraphicsPath shPath = CreateRoundedRectPath(shRc, radius + sp * 0.85f))
                    using (var shPen = new Pen(Color.FromArgb(shAlphas[si], 2, 4, 12), 2.4f))
                    {
                        g.DrawPath(shPen, shPath);
                    }
                }

                using (GraphicsPath capsulePath = CreateRoundedRectPath(pill, radius))
                {
                    if (ShouldRenderCustomBackground())
                    {
                        _bgEngine.DrawBackgroundIntoCapsule(
                            g,
                            capsulePath,
                            pill,
                            _config.custom_bg_fit_mode,
                            _config.custom_bg_dim_percent,
                            _isAnimating || _isResizingWithLCtrl);
                    }
                    else
                    {
                        int bgAlpha = Math.Max(110, Math.Min(255, _config.bg_opacity));
                        using (var glassGrad = new LinearGradientBrush(
                            new PointF(pill.Left, pill.Top),
                            new PointF(pill.Left, pill.Bottom),
                            Color.FromArgb(bgAlpha, 16, 19, 30),
                            Color.FromArgb(bgAlpha, 8, 10, 16)))
                        {
                            var cb = new ColorBlend(3);
                            cb.Positions = new float[] { 0.0f, 0.48f, 1.0f };
                            cb.Colors = new Color[]
                            {
                                Color.FromArgb(bgAlpha, 18, 22, 34),
                                Color.FromArgb(bgAlpha, 9, 11, 18),
                                Color.FromArgb(bgAlpha, 14, 16, 26)
                            };
                            glassGrad.InterpolationColors = cb;
                            g.FillPath(glassGrad, capsulePath);
                        }

                        Region oldClip = g.Clip;
                        g.SetClip(capsulePath);
                        var sheenRc = new RectangleF(pill.X, pill.Y, pill.Width, Math.Min(26f, pill.Height * 0.45f));
                        using (var sheenBr = new LinearGradientBrush(
                            new PointF(sheenRc.Left, sheenRc.Top),
                            new PointF(sheenRc.Left, sheenRc.Bottom),
                            Color.FromArgb(24, 255, 255, 255),
                            Color.FromArgb(0, 255, 255, 255)))
                        {
                            g.FillRectangle(sheenBr, sheenRc);
                        }
                        g.Clip = oldClip;
                    }

                    int glowTop = Math.Max(0, Math.Min(210, _config.border_glow + 24));
                    int glowBot = Math.Max(0, glowTop / 3);
                    if (glowTop > 0)
                    {
                        Color rimAcc1, rimAcc2;
                        GetThemeAccentColors(glowBot, out rimAcc1, out rimAcc2);
                        var innerRimRc = RectangleF.Inflate(pill, -0.6f, -0.6f);
                        using (GraphicsPath innerPath = CreateRoundedRectPath(innerRimRc, Math.Max(2f, radius - 0.6f)))
                        using (var borderBrush = new LinearGradientBrush(
                            new PointF(innerRimRc.Left, innerRimRc.Top),
                            new PointF(innerRimRc.Left, innerRimRc.Bottom),
                            Color.FromArgb(glowTop, 240, 248, 255),
                            rimAcc2))
                        using (var borderPen = new Pen(borderBrush, 1.0f))
                        {
                            g.DrawPath(borderPen, innerPath);
                        }
                    }

                    // MODULE 1: Neon Telemetry Game Mode Border
                    if (IsNeonTelemetryGameModeActive())
                    {
                        Color nc1, nc2;
                        GetThemeAccentColors(215, out nc1, out nc2);
                        using (var neonBr = new LinearGradientBrush(new PointF(pill.Left, pill.Top), new PointF(pill.Right, pill.Bottom), nc1, nc2))
                        using (var neonPen = new Pen(neonBr, 1.55f))
                            g.DrawPath(neonPen, capsulePath);
                    }

                    // MODULE 2: Microphone Active Pulsing Glow / Muted Static Red-Orange Contour
                    if (_config.enable_mic_indicator && (_audio.IsMicActive || _audio.IsMicMuted))
                    {
                        if (_audio.IsMicMuted)
                        {
                            using (var mutePen = new Pen(Color.FromArgb(220, 255, 69, 58), 1.65f))
                                g.DrawPath(mutePen, capsulePath);
                        }
                        else if (_audio.IsMicActive)
                        {
                            int pulseA = 110 + (int)(115 * (0.5f + 0.5f * (float)Math.Sin(_micPulsePhase)));
                            using (var micPen = new Pen(Color.FromArgb(Math.Min(255, pulseA), 48, 209, 88), 1.75f))
                                g.DrawPath(micPen, capsulePath);
                        }
                    }

                    // MODULE 5: Download Watchdog Animated Neon Wave Contour
                    if (_watchdog.IsActiveDownloadDetected || _watchdog.IsWatchdogArmed || _watchdog.IsCountdownActive)
                    {
                        float waveX = pill.Left + (float)((0.5 + 0.5 * Math.Sin(_downloadWavePhase)) * Math.Max(10f, pill.Width));
                        Color wc1 = _watchdog.IsCountdownActive ? Color.FromArgb(245, 255, 55, 95) : Color.FromArgb(225, 56, 189, 248);
                        Color wc2 = _watchdog.IsCountdownActive ? Color.FromArgb(140, 255, 159, 10) : Color.FromArgb(70, 99, 102, 241);
                        using (var waveBr = new LinearGradientBrush(new PointF(pill.Left, pill.Top), new PointF(Math.Max(pill.Left + 20f, waveX), pill.Bottom), wc1, wc2))
                        using (var wavePen = new Pen(waveBr, 1.85f))
                            g.DrawPath(wavePen, capsulePath);
                    }

                    // MODULE 6: Update Download Edge Progress Contour
                    if (_updater.IsDownloading)
                    {
                        float progW = Math.Max(8f, pill.Width * _updater.DownloadProgress);
                        Region oldC = g.Clip;
                        g.SetClip(new RectangleF(pill.Left, pill.Top, progW, pill.Height));
                        using (var updPen = new Pen(Color.FromArgb(250, 52, 211, 153), 2.2f))
                            g.DrawPath(updPen, capsulePath);
                        g.Clip = oldC;
                    }
                }

                float compactAlpha = Math.Max(0f, 1f - _curProg * 2.15f);
                if (compactAlpha > 0.02f)
                    PaintCompactHud(g, pill, (int)(compactAlpha * 255));

                float expandedAlpha = Math.Max(0f, Math.Min(1f, (_curProg - 0.32f) / 0.68f));
                if (expandedAlpha > 0.02f)
                    PaintExpandedDashboard(g, pill, (int)(expandedAlpha * 255));

                if (_lctrlHeld && _config.enable_lctrl_resize)
                    PaintLCtrlResizeOverlay(g, pill, radius);
            }
            }
            catch
            {
                try { g.ResetClip(); } catch { }
            }

            try { g.Flush(FlushIntention.Flush); } catch { }

            IntPtr screenDc = NativeWin32.GetDC(IntPtr.Zero);
            try
            {
                var size = new NativeWin32.SIZE(CanvasW, CanvasH);
                var ptSrc = new NativeWin32.POINT(0, 0);
                var ptDst = new NativeWin32.POINT(Left, Top);
                var blend = new NativeWin32.BLENDFUNCTION
                {
                    BlendOp = NativeWin32.AC_SRC_OVER,
                    BlendFlags = 0,
                    SourceConstantAlpha = (byte)Math.Max(0, Math.Min(255, (int)Math.Round(255f * _ghostOpacityFactor * _fullscreenAutoHideFactor))),
                    AlphaFormat = NativeWin32.AC_SRC_ALPHA
                };
                NativeWin32.UpdateLayeredWindow(Handle, screenDc, ref ptDst, ref size, _dibHdc, ref ptSrc, 0, ref blend, NativeWin32.ULW_ALPHA);
            }
            finally
            {
                NativeWin32.ReleaseDC(IntPtr.Zero, screenDc);
            }

            // Sync hardware DWM 60/144 FPS thumbnail overlay for Page 2
            if (!_hiddenByFullscreen && _mode == IslandMode.Expanded && _expandedPage == 1 && _curProg > 0.72f)
            {
                var activeSrc = _streamEngine.GetActiveSource(Handle);
                if (activeSrc != null && _rcStreamViewport.Width > 30 && _rcStreamViewport.Height > 30)
                {
                    int sx = Left + (int)Math.Round(_rcStreamViewport.X) + 2;
                    int sy = Top + (int)Math.Round(_rcStreamViewport.Y) + 2;
                    int sw = Math.Max(30, (int)Math.Round(_rcStreamViewport.Width) - 4);
                    int sh = Math.Max(30, (int)Math.Round(_rcStreamViewport.Height) - 4);
                    _dwmOverlay.SyncHardwareStream(Handle, sx, sy, sw, sh, activeSrc, _streamEngine.CropModeIndex);
                }
                else
                {
                    _dwmOverlay.HideStream();
                }
            }
            else
            {
                _dwmOverlay.HideStream();
            }
        }

        private void PaintLCtrlResizeOverlay(Graphics g, RectangleF pill, float radius)
        {
            using (GraphicsPath rp = CreateRoundedRectPath(pill, radius))
            using (var dashPen = new Pen(Color.FromArgb(220, 10, 132, 255), 1.6f) { DashStyle = DashStyle.Dash })
                g.DrawPath(dashPen, rp);

            using (var hBrush = new SolidBrush(Color.FromArgb(245, 10, 132, 255)))
            {
                float cy = pill.Top + pill.Height / 2f;
                float cx = pill.Left + pill.Width / 2f;
                g.FillRectangle(hBrush, pill.Left - 2.5f, cy - 10f, 5f, 20f);
                g.FillRectangle(hBrush, pill.Right - 2.5f, cy - 10f, 5f, 20f);
                g.FillRectangle(hBrush, cx - 15f, pill.Bottom - 2.5f, 30f, 5f);
            }

            string info = string.Format("{0:0} × {1:0} px  •  {2}%  (Мышь: размер • Колесико: масштаб • СКМ: сброс)",
                pill.Width, pill.Height, _config.ui_scale_percent);
            var badgeRc = new RectangleF(pill.Left + (pill.Width - 410f) / 2f, Math.Min(CanvasH - 26f, pill.Bottom + 6f), 410f, 22f);
            using (GraphicsPath bp = CreateRoundedRectPath(badgeRc, 11f))
            using (var bFill = new SolidBrush(Color.FromArgb(235, 18, 20, 28)))
            using (var bPen = new Pen(Color.FromArgb(65, 255, 255, 255), 1f))
            using (var bTxt = new SolidBrush(Color.FromArgb(240, 240, 245)))
            {
                g.FillPath(bFill, bp);
                g.DrawPath(bPen, bp);
                g.DrawString(info, _fontSmall, bTxt, badgeRc, _sfCenter);
            }
        }

        private static void DrawContrastText(Graphics g, string text, Font font, Color fgColor, RectangleF rect, StringFormat sf)
        {
            if (string.IsNullOrEmpty(text)) return;
            int shadowAlpha = Math.Min(255, (int)(fgColor.A * 0.95f));
            using (var shadowBr = new SolidBrush(Color.FromArgb(shadowAlpha, 2, 3, 7)))
            using (var fgBr = new SolidBrush(fgColor))
            {
                g.DrawString(text, font, shadowBr, new RectangleF(rect.X - 1.0f, rect.Y, rect.Width, rect.Height), sf);
                g.DrawString(text, font, shadowBr, new RectangleF(rect.X + 1.0f, rect.Y, rect.Width, rect.Height), sf);
                g.DrawString(text, font, shadowBr, new RectangleF(rect.X, rect.Y - 1.0f, rect.Width, rect.Height), sf);
                g.DrawString(text, font, shadowBr, new RectangleF(rect.X, rect.Y + 1.1f, rect.Width, rect.Height), sf);
                g.DrawString(text, font, shadowBr, new RectangleF(rect.X + 0.8f, rect.Y + 1.4f, rect.Width, rect.Height), sf);
                g.DrawString(text, font, fgBr, rect, sf);
            }
        }

        private void PaintCompactHud(Graphics g, RectangleF pill, int alpha)
        {
            Color acc1, acc2;
            GetThemeAccentColors(alpha, out acc1, out acc2);
            bool hasCustomBg = ShouldRenderCustomBackground();

            _rcCompactMicBtn = RectangleF.Empty;

            // MODULE 5: Urgent 15s Download Watchdog Countdown in Compact Mode
            if (_watchdog.IsCountdownActive)
            {
                string cdMsg = string.Format("⚠ {0} ПК через {1} сек  •  Клик — Отмена", _config.watchdog_action ?? "Сон", _watchdog.CountdownRemainingSec);
                DrawContrastText(g, cdMsg, _fontCompact, Color.FromArgb(alpha, 255, 95, 86), pill, _sfCenter);
                return;
            }

            // MODULE 1: Smart Game Mode "Неоновая телеметрия" Ultra-Compact Strip (24px height)
            if (IsNeonTelemetryGameModeActive())
            {
                DrawContrastText(g, _microTelemetry.FormatNeonTelemetryLine(), _fontMono, Color.FromArgb(alpha, 235, 245, 255), pill, _sfCenter);
                return;
            }

            if (_mode == IslandMode.LiveActivity && !string.IsNullOrEmpty(_liveBannerText))
            {
                DrawContrastText(g, _liveBannerText, _fontCompact, Color.FromArgb(alpha, 252, 252, 255), pill, _sfCenter);
                return;
            }

            // 1) Far Left: Clock / Pomodoro Timer (No black outline/pill behind clock!)
            string leftText = "";
            if (_pomoRunning)
                leftText = string.Format("{0:00}:{1:00}", _pomoRemainingSec / 60, _pomoRemainingSec % 60);
            else if (_config.show_clock)
                leftText = _clockStr;

            float leftX = pill.Left + 11f;
            float clockW = string.IsNullOrEmpty(leftText) ? 0f : 42f;
            if (!string.IsNullOrEmpty(leftText))
            {
                Color leftCol = _pomoRunning ? acc1 : Color.FromArgb(alpha, 252, 252, 255);
                DrawContrastText(g, leftText, _fontMonoCompact, leftCol, new RectangleF(leftX, pill.Top, 50f, pill.Height), _sfLeft);
            }

            // MODULE 2: Compact Microphone Active / Muted Icon (1-click mute toggle)
            if (_config.enable_mic_indicator && (_audio.IsMicActive || _audio.IsMicMuted))
            {
                float micX = leftX + clockW + 2f;
                _rcCompactMicBtn = new RectangleF(micX, pill.Top + (pill.Height - 20f) / 2f, 22f, 20f);
                Color micCol = _audio.IsMicMuted ? Color.FromArgb(alpha, 255, 69, 58) : Color.FromArgb(alpha, 48, 209, 88);
                using (GraphicsPath mp = CreateRoundedRectPath(_rcCompactMicBtn, 6f))
                using (var mb = new SolidBrush(Color.FromArgb((int)(alpha * 0.24f), micCol.R, micCol.G, micCol.B)))
                {
                    g.FillPath(mb, mp);
                }
                DrawVectorMicIcon(g, _rcCompactMicBtn, _audio.IsMicMuted, micCol);
                clockW += 24f;
            }
            else if (_watchdog.IsActiveDownloadDetected && _watchdog.CurrentDownMBps >= 0.5f)
            {
                float dlX = leftX + clockW + 2f;
                var dlRc = new RectangleF(dlX, pill.Top, 54f, pill.Height);
                DrawContrastText(g, string.Format("↓{0:0.0}M", _watchdog.CurrentDownMBps), _fontMono, Color.FromArgb(alpha, 34, 211, 238), dlRc, _sfLeft);
                clockW += 50f;
            }

            // 2) Far Right: Equalizer bars
            float eqTotalW = 0f;
            if ((_mode != IslandMode.Expanded || _expandedPage != 1) && _config.eq_style != "Выкл" && _config.eq_style != "Off")
            {
                int bars = (_config.eq_style != null && _config.eq_style.Contains("7")) ? 7 : 5;
                float barW = 2.6f, gap = 2.4f;
                float totalEqW = bars * barW + (bars - 1) * gap;
                eqTotalW = totalEqW + 16f;
                float startX = pill.Right - 13f - totalEqW;
                float cy = pill.Top + pill.Height / 2f;
                float maxBarH = Math.Min(18f, pill.Height - 12f);

                using (var b1 = new SolidBrush(acc1))
                using (var b2 = new SolidBrush(acc2))
                using (var shadowPen = new Pen(Color.FromArgb((int)(alpha * 0.65f), 0, 0, 0), 0.8f))
                {
                    for (int i = 0; i < bars; i++)
                    {
                        float level = _eqBars[i];
                        float bh = Math.Max(3.0f, level * maxBarH);
                        float bx = startX + i * (barW + gap);
                        float by = cy - bh / 2f;
                        var barRc = new RectangleF(bx, by, barW, bh);
                        using (GraphicsPath bp = CreateRoundedRectPath(barRc, 1.3f))
                        {
                            g.FillPath((i % 2 == 0) ? b1 : b2, bp);
                            if (hasCustomBg) g.DrawPath(shadowPen, bp);
                        }
                    }
                }
            }

            // 3) Center: Music Cover Avatar + Artist & Track Title right next to it
            float centerLeft = leftX + clockW + 4f;
            float centerRight = pill.Right - Math.Max(12f, eqTotalW + 4f);
            float availCenterW = centerRight - centerLeft;

            if (_config.show_compact_cover && availCenterW >= 48f)
            {
                float covSz = Math.Min(22f, pill.Height - 10f);
                string songTitle = (_currentMedia.Title ?? "").Trim();
                string songArtist = (_currentMedia.Artist ?? "").Trim();
                bool hasTrack = !string.IsNullOrEmpty(songTitle) && songTitle != "Не воспроизводится";

                if (hasTrack || _currentMedia.CoverBitmap != null)
                {
                    float blockW = Math.Min(availCenterW, 186f);
                    float blockX = centerLeft + Math.Max(0f, (availCenterW - blockW) / 2f);

                    var covRc = new RectangleF(blockX, pill.Top + (pill.Height - covSz) / 2f, covSz, covSz);
                    using (GraphicsPath cp = CreateRoundedRectPath(covRc, 5.5f))
                    {
                        if (_currentMedia.CoverBitmap != null)
                        {
                            Region oldClip = g.Clip;
                            g.SetClip(cp);
                            SafeDrawImage(g, _currentMedia.CoverBitmap, covRc);
                            g.Clip = oldClip;
                        }
                        else
                        {
                            using (var cg = new LinearGradientBrush(covRc, acc1, acc2, 45f))
                                g.FillPath(cg, cp);
                            DrawContrastText(g, "♫", _fontSmall, Color.FromArgb(alpha, 255, 255, 255), covRc, _sfCenter);
                        }
                        using (var rimPen = new Pen(Color.FromArgb((int)(alpha * 0.40f), 255, 255, 255), 0.9f))
                            g.DrawPath(rimPen, cp);
                    }

                    float txtX = covRc.Right + 6f;
                    float txtW = Math.Max(20f, centerRight - txtX);
                    if (txtW >= 28f)
                    {
                        float midY = pill.Top + pill.Height / 2f;
                        var titleRc = new RectangleF(txtX, midY - 12.5f, txtW, 13.5f);
                        var artistRc = new RectangleF(txtX, midY - 0.5f, txtW, 12.5f);
                        DrawContrastText(g, string.IsNullOrEmpty(songTitle) ? "Музыка" : songTitle, _fontSmall, Color.FromArgb(alpha, 252, 252, 255), titleRc, _sfLeft);
                        DrawContrastText(g, string.IsNullOrEmpty(songArtist) ? _currentMedia.DisplayAppName : songArtist, _fontSmall, Color.FromArgb((int)(alpha * 0.85f), 210, 215, 230), artistRc, _sfLeft);
                    }
                }
            }
        }
        private void DrawSubtleEdgePageArrows(Graphics g, RectangleF pill, int alpha)
        {
            // Subtle cyclic edge arrows ('<' on left edge, '>' on right edge) that don't stand out
            float arrowW = 14f;
            float arrowH = 44f;
            float midY = pill.Top + (pill.Height - arrowH) / 2f;

            _rcPageLeftArrow = new RectangleF(pill.Left + 2.5f, midY, arrowW, arrowH);
            _rcPageRightArrow = new RectangleF(pill.Right - arrowW - 2.5f, midY, arrowW, arrowH);

            Point localCursor = PointToClient(Cursor.Position);
            bool hoverL = _rcPageLeftArrow.Contains(localCursor.X, localCursor.Y);
            bool hoverR = _rcPageRightArrow.Contains(localCursor.X, localCursor.Y);

            DrawSingleEdgeChevron(g, _rcPageLeftArrow, true, hoverL, alpha);
            DrawSingleEdgeChevron(g, _rcPageRightArrow, false, hoverR, alpha);
        }

        private static void DrawSingleEdgeChevron(Graphics g, RectangleF rc, bool isLeft, bool hovered, int alpha)
        {
            int pillAlpha = (int)(alpha * (hovered ? 0.22f : 0.045f));
            int chevAlpha = (int)(alpha * (hovered ? 0.72f : 0.22f));

            using (GraphicsPath bgPath = CreateRoundedRectPath(rc, 6f))
            using (var bgBr = new SolidBrush(Color.FromArgb(pillAlpha, 255, 255, 255)))
            {
                g.FillPath(bgBr, bgPath);
            }

            float cx = rc.X + rc.Width * 0.5f;
            float cy = rc.Y + rc.Height * 0.5f;
            float dx = isLeft ? -2.2f : 2.2f;

            using (var pen = new Pen(Color.FromArgb(chevAlpha, 240, 242, 250), hovered ? 1.65f : 1.3f)
            {
                StartCap = LineCap.Round,
                EndCap = LineCap.Round,
                LineJoin = LineJoin.Round
            })
            {
                g.DrawLine(pen, cx - dx, cy - 5.2f, cx + dx, cy);
                g.DrawLine(pen, cx + dx, cy, cx - dx, cy + 5.2f);
            }
        }

        private void PaintExpandedStreamPage(Graphics g, RectangleF pill, int alpha, Color acc1, Color acc2, bool hasCustomBg)
        {
            float padX = pill.Left + 20f;
            float rightX = pill.Right - 20f;
            float innerW = rightX - padX;
            float topY = pill.Top + 10f;

            Color colWhite = Color.FromArgb(alpha, 252, 252, 255);
            Color colMuted = Color.FromArgb((int)(alpha * 0.85f), 210, 215, 232);
            Color barFill = hasCustomBg
                ? Color.FromArgb((int)(alpha * 0.68f), 10, 12, 18)
                : Color.FromArgb((int)(alpha * 0.14f), 255, 255, 255);
            Color barBorder = Color.FromArgb((int)(alpha * 0.18f), 255, 255, 255);

            var activeSrc = _streamEngine.GetActiveSource(Handle);

            // Top Control Strip: [ ● LIVE • Discord / YouTube ▾ ]   [ 16:9 Видео ] [ ↗ Окно ]
            float openBtnW = 58f;
            float cropBtnW = 84f;
            float srcBtnW = Math.Max(120f, innerW - openBtnW - cropBtnW - 12f);

            _rcStreamSourceBtn = new RectangleF(padX, topY, srcBtnW, 24f);
            _rcStreamCropBtn = new RectangleF(_rcStreamSourceBtn.Right + 6f, topY, cropBtnW, 24f);
            _rcStreamOpenBtn = new RectangleF(_rcStreamCropBtn.Right + 6f, topY, openBtnW, 24f);

            using (var brBar = new SolidBrush(barFill))
            using (var penBar = new Pen(barBorder, 1f))
            {
                using (GraphicsPath p1 = CreateRoundedRectPath(_rcStreamSourceBtn, 8f))
                using (GraphicsPath p2 = CreateRoundedRectPath(_rcStreamCropBtn, 8f))
                using (GraphicsPath p3 = CreateRoundedRectPath(_rcStreamOpenBtn, 8f))
                {
                    g.FillPath(brBar, p1); g.DrawPath(penBar, p1);
                    g.FillPath(brBar, p2); g.DrawPath(penBar, p2);
                    g.FillPath(brBar, p3); g.DrawPath(penBar, p3);
                }
            }

            // Red/Purple LIVE dot inside source button
            using (var liveBr = new SolidBrush(Color.FromArgb(alpha, 255, 55, 95)))
                g.FillEllipse(liveBr, _rcStreamSourceBtn.X + 8f, topY + 8f, 8f, 8f);

            string srcTitle = (activeSrc != null)
                ? (activeSrc.DisplayHeader + "  ▾")
                : "Нет активного окна (Клик для поиска) ▾";
            DrawContrastText(g, srcTitle, _fontSmall, colWhite,
                new RectangleF(_rcStreamSourceBtn.X + 20f, topY, _rcStreamSourceBtn.Width - 24f, 24f), _sfLeft);

            DrawContrastText(g, _streamEngine.CurrentCropLabel, _fontSmall, acc1, _rcStreamCropBtn, _sfCenter);
            DrawContrastText(g, "↗ Окно", _fontSmall, colMuted, _rcStreamOpenBtn, _sfCenter);

            // Live Video Stream Viewport (fills remaining height of expanded island)
            float viewY = topY + 29f;
            float viewH = Math.Max(80f, pill.Bottom - viewY - 10f);
            _rcStreamViewport = new RectangleF(padX, viewY, innerW, viewH);

            using (GraphicsPath vpPath = CreateRoundedRectPath(_rcStreamViewport, 13f))
            using (var vpBg = new SolidBrush(Color.FromArgb((int)(alpha * 0.78f), 6, 7, 11)))
            using (var vpPen = new Pen(Color.FromArgb((int)(alpha * 0.22f), acc1.R, acc1.G, acc1.B), 1.1f))
            {
                g.FillPath(vpBg, vpPath);
                if (activeSrc == null)
                {
                    _dwmOverlay.HideStream();
                    DrawContrastText(g, "Откройте Discord, YouTube или видео в браузере — кликните сверху или ПКМ для выбора окна",
                        _fontSub, colMuted, _rcStreamViewport, _sfCenter);
                }
                g.DrawPath(vpPen, vpPath);
            }

            // Subtle page indicator pill at bottom-right of viewport ("2/2 • Pinned")
            var tagRc = new RectangleF(_rcStreamViewport.Right - 96f, _rcStreamViewport.Bottom - 20f, 90f, 16f);
            using (GraphicsPath tp = CreateRoundedRectPath(tagRc, 6f))
            using (var tb = new SolidBrush(Color.FromArgb((int)(alpha * 0.62f), 8, 10, 16)))
            {
                g.FillPath(tb, tp);
                DrawContrastText(g, "LIVE 2/2 • Закреп", _fontSmall, Color.FromArgb((int)(alpha * 0.85f), 200, 210, 230), tagRc, _sfCenter);
            }
        }

        private void PaintExpandedVaultAndWatchdogPage(Graphics g, RectangleF pill, int alpha, Color acc1, Color acc2, bool hasCustomBg)
        {
            float padX = pill.Left + 20f;
            float rightX = pill.Right - 20f;
            float innerW = rightX - padX;
            float curY = pill.Top + 10f;

            Color colWhite = Color.FromArgb(alpha, 252, 252, 255);
            Color colMuted = Color.FromArgb((int)(alpha * 0.88f), 205, 212, 230);
            Color cardFill = hasCustomBg
                ? Color.FromArgb((int)(alpha * 0.72f), 10, 12, 18)
                : Color.FromArgb((int)(alpha * 0.11f), 255, 255, 255);
            Color cardBorder = Color.FromArgb((int)(alpha * 0.18f), 255, 255, 255);

            _vaultThumbRects.Clear();
            _vaultCheckRects.Clear();
            _rcUpdateApplyBtn = RectangleF.Empty;
            _rcUpdateLaterBtn = RectangleF.Empty;
            _rcWatchdogCancelCountdownBtn = RectangleF.Empty;

            using (var brCard = new SolidBrush(cardFill))
            using (var penCard = new Pen(cardBorder, 1f))
            {
                // SECTION 1: Screenshot Session Vault Gallery
                var vaultHeaderRc = new RectangleF(padX, curY, innerW - 190f, 22f);
                int selCount = _screenshotVault.SelectedCount;
                int totalCaps = _screenshotVault.Count;
                DrawContrastText(g, string.Format("📸 Хранилище скриншотов сессии ({0})  •  Клик: в буфер  •  Тянуть: в Discord", totalCaps),
                    _fontSmall, colWhite, vaultHeaderRc, _sfLeft);

                _rcVaultDeleteSelectedBtn = new RectangleF(rightX - 184f, curY, 106f, 22f);
                _rcVaultClearAllBtn = new RectangleF(rightX - 72f, curY, 72f, 22f);

                using (GraphicsPath pDel = CreateRoundedRectPath(_rcVaultDeleteSelectedBtn, 7f))
                using (GraphicsPath pClr = CreateRoundedRectPath(_rcVaultClearAllBtn, 7f))
                using (var bDel = new SolidBrush(selCount > 0 ? Color.FromArgb((int)(alpha * 0.45f), 255, 69, 58) : cardFill))
                using (var bClr = new SolidBrush(totalCaps > 0 ? Color.FromArgb((int)(alpha * 0.30f), 99, 102, 241) : cardFill))
                {
                    g.FillPath(bDel, pDel); g.DrawPath(penCard, pDel);
                    g.FillPath(bClr, pClr); g.DrawPath(penCard, pClr);
                }
                DrawContrastText(g, selCount > 0 ? ("Удалить (" + selCount + ")") : "Удалить выбр.", _fontSmall,
                    selCount > 0 ? Color.FromArgb(alpha, 255, 110, 100) : colMuted, _rcVaultDeleteSelectedBtn, _sfCenter);
                DrawContrastText(g, "Очистить всё", _fontSmall, totalCaps > 0 ? colWhite : colMuted, _rcVaultClearAllBtn, _sfCenter);

                curY += 26f;

                float galleryH = Math.Max(78f, pill.Height - 122f);
                var galleryRc = new RectangleF(padX, curY, innerW, galleryH);
                using (GraphicsPath gp = CreateRoundedRectPath(galleryRc, 12f))
                {
                    g.FillPath(brCard, gp);
                    g.DrawPath(penCard, gp);
                }

                var items = _screenshotVault.GetSnapshotList();
                if (items.Count == 0)
                {
                    DrawContrastText(g, "Сделайте скриншот (Win + Shift + S или PrtScn) — он мгновенно появится здесь для перетаскивания",
                        _fontSub, colMuted, galleryRc, _sfCenter);
                }
                else
                {
                    float thumbX = padX + 8f;
                    float thumbY = curY + 6f;
                    float thumbH = galleryH - 12f;
                    float thumbW = Math.Min(116f, (innerW - 24f) / 4f - 6f);
                    int maxShow = Math.Min(items.Count, Math.Max(3, (int)((innerW - 12f) / (thumbW + 8f))));

                    for (int i = 0; i < maxShow; i++)
                    {
                        var it = items[i];
                        var tRc = new RectangleF(thumbX, thumbY, thumbW, thumbH);
                        _vaultThumbRects.Add(new KeyValuePair<RectangleF, VaultCaptureItem>(tRc, it));

                        using (GraphicsPath tp = CreateRoundedRectPath(tRc, 8f))
                        {
                            if (it.Thumbnail != null)
                            {
                                Region prevC = g.Clip;
                                g.SetClip(tp);
                                SafeDrawImage(g, it.Thumbnail, tRc);
                                g.Clip = prevC;
                            }
                            using (var tPen = new Pen(it.IsSelected ? Color.FromArgb(alpha, 56, 189, 248) : cardBorder, it.IsSelected ? 1.8f : 1f))
                                g.DrawPath(tPen, tp);
                        }

                        // Checkbox in top-right of thumbnail
                        var chkRc = new RectangleF(tRc.Right - 20f, tRc.Y + 4f, 16f, 16f);
                        _vaultCheckRects.Add(new KeyValuePair<RectangleF, VaultCaptureItem>(chkRc, it));
                        using (GraphicsPath cp = CreateRoundedRectPath(chkRc, 4f))
                        using (var cb = new SolidBrush(it.IsSelected ? Color.FromArgb(alpha, 10, 132, 255) : Color.FromArgb((int)(alpha * 0.75f), 12, 14, 20)))
                        {
                            g.FillPath(cb, cp);
                            g.DrawPath(penCard, cp);
                        }
                        DrawContrastText(g, it.IsSelected ? "✓" : "", _fontSmall, colWhite, chkRc, _sfCenter);

                        // Timestamp label at bottom of thumbnail
                        var timeTag = new RectangleF(tRc.X + 3f, tRc.Bottom - 16f, tRc.Width - 6f, 14f);
                        using (GraphicsPath ttp = CreateRoundedRectPath(timeTag, 4f))
                        using (var ttb = new SolidBrush(Color.FromArgb((int)(alpha * 0.72f), 6, 8, 14)))
                            g.FillPath(ttb, ttp);
                        DrawContrastText(g, it.TimeLabel, _fontMono, colWhite, timeTag, _sfCenter);

                        thumbX += thumbW + 8f;
                    }
                }

                curY += galleryH + 8f;

                // SECTION 2: Download Watchdog (Stop-Timer for Large Downloads)
                var wdCard = new RectangleF(padX, curY, innerW, 32f);
                using (GraphicsPath wp = CreateRoundedRectPath(wdCard, 10f))
                {
                    g.FillPath(brCard, wp);
                    g.DrawPath(penCard, wp);
                }

                if (_watchdog.IsCountdownActive)
                {
                    _rcWatchdogCancelCountdownBtn = new RectangleF(rightX - 110f, curY + 4f, 104f, 24f);
                    using (GraphicsPath cp = CreateRoundedRectPath(_rcWatchdogCancelCountdownBtn, 7f))
                    using (var cb = new SolidBrush(Color.FromArgb(alpha, 255, 55, 95)))
                        g.FillPath(cb, cp);
                    DrawContrastText(g, "✕ ОТМЕНА", _fontSmall, colWhite, _rcWatchdogCancelCountdownBtn, _sfCenter);

                    DrawContrastText(g, string.Format("⚠ Загрузка завершена! Переход в «{0}» через {1} сек...", _config.watchdog_action ?? "Сон", _watchdog.CountdownRemainingSec),
                        _fontSmall, Color.FromArgb(alpha, 255, 189, 46), new RectangleF(padX + 10f, curY, innerW - 126f, 32f), _sfLeft);
                }
                else
                {
                    string dlStats = string.Format("↓ {0:0.0} МБ/с  (Сред: {1:0.0} МБ/с • Сессия: {2:0.0} МБ)",
                        _watchdog.CurrentDownMBps, _watchdog.AverageDownMBps, _watchdog.SessionDownloadedMB);
                    DrawContrastText(g, dlStats, _fontMono, colMuted, new RectangleF(padX + 10f, curY, innerW - 240f, 32f), _sfLeft);

                    _rcWatchdogToggleBtn = new RectangleF(rightX - 230f, curY + 4f, 142f, 24f);
                    _rcWatchdogActionBtn = new RectangleF(rightX - 82f, curY + 4f, 76f, 24f);

                    using (GraphicsPath p1 = CreateRoundedRectPath(_rcWatchdogToggleBtn, 7f))
                    using (GraphicsPath p2 = CreateRoundedRectPath(_rcWatchdogActionBtn, 7f))
                    using (var b1 = new SolidBrush(_watchdog.IsWatchdogArmed ? Color.FromArgb((int)(alpha * 0.55f), 48, 209, 88) : Color.FromArgb((int)(alpha * 0.22f), 255, 255, 255)))
                    using (var b2 = new SolidBrush(Color.FromArgb((int)(alpha * 0.22f), 99, 102, 241)))
                    {
                        g.FillPath(b1, p1); g.DrawPath(penCard, p1);
                        g.FillPath(b2, p2); g.DrawPath(penCard, p2);
                    }
                    DrawContrastText(g, _watchdog.IsWatchdogArmed ? "🌙 Стоп-таймер: ВКЛ" : "🌙 Усыпить после закачки", _fontSmall, colWhite, _rcWatchdogToggleBtn, _sfCenter);
                    DrawContrastText(g, (_config.watchdog_action ?? "Сон") + " ▾", _fontSmall, acc1, _rcWatchdogActionBtn, _sfCenter);
                }

                curY += 36f;

                // SECTION 3: Voluntary Update Notification Bar / Status Footer
                var footerRc = new RectangleF(padX, curY, innerW, 24f);
                if (_updater.IsUpdateAvailable && !_updater.DismissedForSession)
                {
                    using (GraphicsPath up = CreateRoundedRectPath(footerRc, 8f))
                    using (var ub = new SolidBrush(Color.FromArgb((int)(alpha * 0.35f), 10, 132, 255)))
                    {
                        g.FillPath(ub, up);
                        g.DrawPath(penCard, up);
                    }
                    DrawContrastText(g, "🚀 Доступно обновление Dynamic Island v" + _updater.LatestVersionStr + " (" + _updater.StatusText + ")",
                        _fontSmall, colWhite, new RectangleF(padX + 10f, curY, innerW - 155f, 24f), _sfLeft);

                    _rcUpdateApplyBtn = new RectangleF(rightX - 140f, curY + 2f, 74f, 20f);
                    _rcUpdateLaterBtn = new RectangleF(rightX - 60f, curY + 2f, 54f, 20f);
                    using (GraphicsPath pa = CreateRoundedRectPath(_rcUpdateApplyBtn, 6f))
                    using (GraphicsPath pl = CreateRoundedRectPath(_rcUpdateLaterBtn, 6f))
                    using (var ba = new SolidBrush(Color.FromArgb(alpha, 48, 209, 88)))
                    using (var bl = new SolidBrush(cardFill))
                    {
                        g.FillPath(ba, pa);
                        g.FillPath(bl, pl);
                    }
                    DrawContrastText(g, "Обновить", _fontSmall, colWhite, _rcUpdateApplyBtn, _sfCenter);
                    DrawContrastText(g, "Позже", _fontSmall, colMuted, _rcUpdateLaterBtn, _sfCenter);
                }
                else
                {
                    string statusLine = string.Format("{0}   •   {1}   •   Страница 3/3 (Alt+Space — Призрак)",
                        _microTelemetry.FormatNeonTelemetryLine(), _updater.StatusText);
                    DrawContrastText(g, statusLine, _fontSmall, colMuted, footerRc, _sfCenter);
                }
            }
        }
        private void PaintExpandedDashboard(Graphics g, RectangleF pill, int alpha)
        {
            float padX = pill.Left + 18f;
            float rightX = pill.Right - 16f;
            float innerW = rightX - padX;
            float availH = pill.Height - 24f;
            float rowGap = Math.Max(5f, (availH - 166f) / 3.5f);
            float curY = pill.Top + 12f;

            Color acc1, acc2;
            GetThemeAccentColors(alpha, out acc1, out acc2);
            bool hasCustomBg = ShouldRenderCustomBackground();

            // BACKGROUND EQUALIZER AT BOTTOM OF EXPANDED ISLAND (Screenshot 4)
            if ((_mode != IslandMode.Expanded || _expandedPage != 1) && _config.eq_style != "Выкл" && _config.eq_style != "Off")
            {
                float expRadius = Math.Max(12f, _config.corner_radius);
                using (GraphicsPath clipCap = CreateRoundedRectPath(pill, expRadius))
                {
                    Region prevClip = g.Clip;
                    g.SetClip(clipCap);
                    int barCount = _expEqBars.Length;
                    float eqLeft = pill.Left + 18f;
                    float eqRight = pill.Right - 18f;
                    float eqSpan = Math.Max(100f, eqRight - eqLeft);
                    float stepX = eqSpan / barCount;
                    float barW = Math.Max(4.0f, stepX * 0.56f);
                    float maxEqH = Math.Min(48f, pill.Height * 0.24f);

                    Color eqC1 = Color.FromArgb(Math.Min(255, (int)(alpha * 0.52f)), acc1.R, acc1.G, acc1.B);
                    Color eqC2 = Color.FromArgb(Math.Min(255, (int)(alpha * 0.14f)), acc2.R, acc2.G, acc2.B);

                    for (int i = 0; i < barCount; i++)
                    {
                        float lvl = Math.Max(0.08f, _expEqBars[i]);
                        float bh = Math.Max(4f, lvl * maxEqH);
                        float bx = eqLeft + i * stepX + (stepX - barW) * 0.5f;
                        float by = pill.Bottom - bh - 1.5f;
                        var bRc = new RectangleF(bx, by, barW, bh);
                        using (GraphicsPath bp = CreateRoundedRectPath(bRc, barW * 0.48f))
                        using (var lg = new LinearGradientBrush(new PointF(bx, by), new PointF(bx, by + bh + 1f), eqC1, eqC2))
                        {
                            g.FillPath(lg, bp);
                        }
                    }
                    g.Clip = prevClip;
                }
            }

            if (_expandedPage == 1)
            {
                PaintExpandedStreamPage(g, pill, alpha, acc1, acc2, hasCustomBg);
                DrawSubtleEdgePageArrows(g, pill, alpha);
                return;
            }
            if (_expandedPage == 2)
            {
                PaintExpandedVaultAndWatchdogPage(g, pill, alpha, acc1, acc2, hasCustomBg);
                DrawSubtleEdgePageArrows(g, pill, alpha);
                return;
            }

            Color colWhite = Color.FromArgb(alpha, 252, 252, 255);
            Color colMuted = Color.FromArgb((int)(alpha * 0.90f), 232, 234, 244);

            Color bentoFillCol = hasCustomBg
                ? Color.FromArgb((int)(alpha * 0.66f), 8, 10, 16)
                : Color.FromArgb((int)(alpha * 0.08f), 255, 255, 255);
            Color ctrlFillCol = hasCustomBg
                ? Color.FromArgb((int)(alpha * 0.48f), 22, 26, 36)
                : Color.FromArgb((int)(alpha * 0.14f), 255, 255, 255);
            Color bentoBorderCol = hasCustomBg
                ? Color.FromArgb((int)(alpha * 0.26f), 255, 255, 255)
                : Color.FromArgb((int)(alpha * 0.10f), 255, 255, 255);

            using (var brWhite = new SolidBrush(colWhite))
            using (var brBento = new SolidBrush(bentoFillCol))
            using (var brControl = new SolidBrush(ctrlFillCol))
            using (var penBento = new Pen(bentoBorderCol, 1f))
            {
                // BENTO CARD 1: Media Player Surface
                var mediaCardRc = new RectangleF(padX, curY, innerW, 74f);
                using (GraphicsPath mcp = CreateRoundedRectPath(mediaCardRc, 16f))
                {
                    g.FillPath(brBento, mcp);
                    g.DrawPath(penBento, mcp);
                }

                float inX = padX + 10f;
                float inRight = rightX - 10f;
                float inY = curY + 8f;

                var coverRc = new RectangleF(inX, inY, 42f, 42f);
                using (GraphicsPath cp = CreateRoundedRectPath(coverRc, 10f))
                {
                    if (_currentMedia.CoverBitmap != null)
                    {
                        Region oldClip = g.Clip;
                        g.SetClip(cp);
                        SafeDrawImage(g, _currentMedia.CoverBitmap, coverRc);
                        g.Clip = oldClip;
                    }
                    else
                    {
                        using (var covGrad = new LinearGradientBrush(coverRc, acc1, acc2, 45f))
                            g.FillPath(covGrad, cp);
                        DrawContrastText(g, "♫", _fontTitle, colWhite, coverRc, _sfCenter);
                    }
                    g.DrawPath(penBento, cp);
                }

                string playerLabel = _currentMedia.DisplayAppName +
                    (_allPlayers.Count > 1 ? string.Format("  {0}/{1} ▾", _currentPlayerIdx + 1, _allPlayers.Count) : "  ↗");
                float badgeW = Math.Min(165f, innerW - 190f);
                _rcPlayerBadge = new RectangleF(inX + 52f, inY, badgeW, 14f);
                DrawContrastText(g, playerLabel, _fontSmall, acc1, _rcPlayerBadge, _sfLeft);

                float metaW = Math.Max(80f, innerW - 216f);
                var titleRc = new RectangleF(inX + 52f, inY + 13f, metaW, 16f);
                var artistRc = new RectangleF(inX + 52f, inY + 28f, metaW, 14f);
                DrawContrastText(g, _currentMedia.Title, _fontTitle, colWhite, titleRc, _sfLeft);
                DrawContrastText(g, _currentMedia.Artist, _fontSub, colMuted, artistRc, _sfLeft);

                _rcSongClickZone = new RectangleF(inX, inY + 13f, metaW + 52f, 30f);

                _rcLikeBtn = new RectangleF(inRight - 128f, inY + 7f, 26f, 26f);
                _rcPrevBtn = new RectangleF(inRight - 96f, inY + 7f, 26f, 26f);
                _rcPlayBtn = new RectangleF(inRight - 64f, inY + 4f, 32f, 32f);
                _rcNextBtn = new RectangleF(inRight - 26f, inY + 7f, 26f, 26f);

                bool likedNow = IsCurrentTrackLiked();
                g.FillEllipse(brControl, _rcLikeBtn);
                var adjLike = ApplyMicroInteraction(g, _rcLikeBtn, 13f, alpha, Color.FromArgb(alpha, 236, 72, 153), true);
                DrawVectorHeartIcon(g, adjLike, likedNow, colWhite, Color.FromArgb(alpha, 236, 72, 153));

                g.FillEllipse(brControl, _rcPrevBtn);
                var adjPrev = ApplyMicroInteraction(g, _rcPrevBtn, 13f, alpha, acc1, true);
                DrawVectorTransportIcon(g, adjPrev, "prev", colWhite);

                var adjPlay = _rcPlayBtn.Contains(_clientCursorPt) && _isMousePressed ? RectangleF.Inflate(_rcPlayBtn, -1.2f, -1.2f) : _rcPlayBtn;
                using (var brPlay = new SolidBrush(colWhite))
                {
                    g.FillEllipse(brPlay, adjPlay);
                    if (_rcPlayBtn.Contains(_clientCursorPt))
                    {
                        using (var pGlow = new Pen(acc1, 1.5f))
                            g.DrawEllipse(pGlow, RectangleF.Inflate(adjPlay, 1.2f, 1.2f));
                    }
                    DrawVectorTransportIcon(g, adjPlay, _currentMedia.IsPlaying ? "pause" : "play", Color.FromArgb(alpha, 12, 13, 18));
                }

                g.FillEllipse(brControl, _rcNextBtn);
                var adjNext = ApplyMicroInteraction(g, _rcNextBtn, 13f, alpha, acc1, true);
                DrawVectorTransportIcon(g, adjNext, "next", colWhite);

                float progY = curY + 58f;
                double livePosSec = _currentMedia.GetLivePositionSec();
                double totalDurSec = _currentMedia.DurationSec;

                DrawContrastText(g, FormatTimeMmSs(livePosSec), _fontMono, colMuted, new RectangleF(inX, progY - 5f, 36f, 14f), _sfLeft);
                DrawContrastText(g, FormatTimeMmSs(totalDurSec), _fontMono, colMuted, new RectangleF(inRight - 36f, progY - 5f, 36f, 14f), _sfRight);

                _rcProgressBar = new RectangleF(inX + 38f, progY, Math.Max(70f, (inRight - inX) - 76f), 4.4f);
                using (GraphicsPath gp = CreateRoundedRectPath(_rcProgressBar, 2.2f))
                    g.FillPath(brControl, gp);

                float progRatio = (totalDurSec > 0)
                    ? (float)Math.Max(0.0, Math.Min(1.0, livePosSec / totalDurSec))
                    : 0.0f;
                float fillW = Math.Max(4f, _rcProgressBar.Width * progRatio);
                if (progRatio > 0.002f)
                {
                    var progFillRc = new RectangleF(_rcProgressBar.X, progY, fillW, 4.4f);
                    using (GraphicsPath gpFill = CreateRoundedRectPath(progFillRc, 2.2f))
                    using (var progGrad = new LinearGradientBrush(progFillRc, acc1, acc2, 0f))
                        g.FillPath(progGrad, gpFill);
                }
                if (totalDurSec > 0)
                    g.FillEllipse(brWhite, _rcProgressBar.X + fillW - 4.0f, progY - 2.0f, 8.4f, 8.4f);

                curY += 74f + rowGap;

                // ROW 2: TWO LABELED VOLUME SLIDERS ("Звук ПК" + "Музыка") + Timer + Settings
                float rightControlsW = 96f;
                float totalVolAreaW = innerW - rightControlsW - 6f;
                float singleVolW = (totalVolAreaW - 6f) / 2f;

                // 1) Left Volume Capsule: "Звук ПК" (PC Master Volume)
                _rcPcVolCard = new RectangleF(padX, curY, singleVolW, 28f);
                using (GraphicsPath vcp1 = CreateRoundedRectPath(_rcPcVolCard, 11f))
                {
                    g.FillPath(brBento, vcp1);
                    g.DrawPath(penBento, vcp1);
                }

                _rcMuteBtn = new RectangleF(_rcPcVolCard.X + 3f, curY + 2f, 22f, 24f);
                DrawVectorSpeakerIcon(g, _rcMuteBtn, _audio.LastMuted, _audio.LastVolume, colWhite);

                var lblPcRc = new RectangleF(_rcPcVolCard.X + 25f, curY, 48f, 28f);
                DrawContrastText(g, "Звук ПК", _fontSmall, colWhite, lblPcRc, _sfLeft);

                _rcVolSlider = new RectangleF(_rcPcVolCard.X + 74f, curY + 11.8f, Math.Max(32f, _rcPcVolCard.Width - 110f), 4.4f);
                using (GraphicsPath vp1 = CreateRoundedRectPath(_rcVolSlider, 2.2f))
                    g.FillPath(brControl, vp1);

                float volRatio = _audio.LastMuted ? 0f : (_audio.LastVolume / 100f);
                float volFillW = Math.Max(4f, _rcVolSlider.Width * volRatio);
                if (volRatio > 0.01f)
                {
                    var vFill = new RectangleF(_rcVolSlider.X, _rcVolSlider.Y, volFillW, 4.4f);
                    using (GraphicsPath vfp = CreateRoundedRectPath(vFill, 2.2f))
                    using (var vGrad = new LinearGradientBrush(vFill, acc1, acc2, 0f))
                        g.FillPath(vGrad, vfp);
                }
                g.FillEllipse(brWhite, _rcVolSlider.X + volFillW - 4.2f, _rcVolSlider.Y - 2.1f, 8.6f, 8.6f);

                DrawContrastText(g, _audio.LastMuted ? "Выкл" : (_audio.LastVolume + "%"), _fontMono, colMuted,
                    new RectangleF(_rcVolSlider.Right + 2f, curY, 34f, 28f), _sfCenter);

                // 2) Right Volume Capsule: "Музыка" (Per-App Music Volume)
                _rcMusicVolCard = new RectangleF(_rcPcVolCard.Right + 6f, curY, singleVolW, 28f);
                using (GraphicsPath vcp2 = CreateRoundedRectPath(_rcMusicVolCard, 11f))
                {
                    g.FillPath(brBento, vcp2);
                    g.DrawPath(penBento, vcp2);
                }

                _rcMusicMuteBtn = new RectangleF(_rcMusicVolCard.X + 3f, curY + 2f, 20f, 24f);
                DrawVectorMusicNoteIcon(g, _rcMusicMuteBtn, _audio.LastMusicMuted, acc1);

                var lblMusRc = new RectangleF(_rcMusicVolCard.X + 23f, curY, 46f, 28f);
                DrawContrastText(g, "Музыка", _fontSmall, colWhite, lblMusRc, _sfLeft);

                _rcMusicVolSlider = new RectangleF(_rcMusicVolCard.X + 70f, curY + 11.8f, Math.Max(32f, _rcMusicVolCard.Width - 106f), 4.4f);
                using (GraphicsPath vp2 = CreateRoundedRectPath(_rcMusicVolSlider, 2.2f))
                    g.FillPath(brControl, vp2);

                float musRatio = _audio.LastMusicMuted ? 0f : (_audio.LastMusicVolume / 100f);
                float musFillW = Math.Max(4f, _rcMusicVolSlider.Width * musRatio);
                if (musRatio > 0.01f)
                {
                    var mFill = new RectangleF(_rcMusicVolSlider.X, _rcMusicVolSlider.Y, musFillW, 4.4f);
                    using (GraphicsPath mfp = CreateRoundedRectPath(mFill, 2.2f))
                    using (var mGrad = new LinearGradientBrush(mFill, acc2, acc1, 0f))
                        g.FillPath(mGrad, mfp);
                }
                g.FillEllipse(brWhite, _rcMusicVolSlider.X + musFillW - 4.2f, _rcMusicVolSlider.Y - 2.1f, 8.6f, 8.6f);

                DrawContrastText(g, _audio.LastMusicMuted ? "Выкл" : (_audio.LastMusicVolume + "%"), _fontMono, colMuted,
                    new RectangleF(_rcMusicVolSlider.Right + 2f, curY, 34f, 28f), _sfCenter);

                // 3) Focus Timer & Settings Gear
                _rcPomoBtn = new RectangleF(rightX - 92f, curY, 60f, 28f);
                using (GraphicsPath pp = CreateRoundedRectPath(_rcPomoBtn, 11f))
                {
                    g.FillPath(brBento, pp);
                    g.DrawPath(penBento, pp);
                }
                ApplyMicroInteraction(g, _rcPomoBtn, 11f, alpha, acc1);
                DrawContrastText(g, _pomoRunning ? string.Format("{0:00}:{1:00}", _pomoRemainingSec / 60, _pomoRemainingSec % 60) : "Таймер",
                    _pomoRunning ? _fontMono : _fontSmall, _pomoRunning ? acc1 : colWhite, _rcPomoBtn, _sfCenter);

                _rcSettingsBtn = new RectangleF(rightX - 28f, curY, 28f, 28f);
                using (GraphicsPath sp = CreateRoundedRectPath(_rcSettingsBtn, 11f))
                {
                    g.FillPath(brBento, sp);
                    g.DrawPath(penBento, sp);
                }
                var adjGear = ApplyMicroInteraction(g, _rcSettingsBtn, 11f, alpha, acc1);
                DrawVectorGearIcon(g, adjGear, colWhite);

                curY += 28f + rowGap;

                // ROW 3: Minimalist macOS Squircle App Dock + [ + ] Add Program Button
                _dockRects.Clear();
                float dockX = padX;
                int maxSlots = Math.Max(4, (int)(innerW / 40f));
                int maxIcons = Math.Min(_config.pinned_apps.Count, maxSlots - 1);
                for (int i = 0; i < maxIcons; i++)
                {
                    PinnedAppItem item = _config.pinned_apps[i];
                    var btnRc = new RectangleF(dockX, curY, 34f, 34f);
                    _dockRects.Add(new KeyValuePair<RectangleF, PinnedAppItem>(btnRc, item));
                    using (GraphicsPath dp = CreateRoundedRectPath(btnRc, 10f))
                    {
                        g.FillPath(brBento, dp);
                        g.DrawPath(penBento, dp);
                    }

                    ApplyMicroInteraction(g, btnRc, 10f, alpha, acc1);
                    Bitmap icoBmp = GetAppIconBitmap(item.target_path);
                    if (icoBmp != null)
                        SafeDrawImage(g, icoBmp, btnRc.X + 6f, btnRc.Y + 6f, 22f, 22f);
                    else
                        DrawContrastText(g, item.name.Substring(0, Math.Min(2, item.name.Length)), _fontSmall, colWhite, btnRc, _sfCenter);

                    dockX += 40f;
                }

                _rcAddAppBtn = new RectangleF(dockX, curY, 34f, 34f);
                using (GraphicsPath ap = CreateRoundedRectPath(_rcAddAppBtn, 10f))
                using (var addPen = new Pen(Color.FromArgb((int)(alpha * 0.45f), acc1.R, acc1.G, acc1.B), 1.1f))
                using (var plusPen = new Pen(colWhite, 1.8f) { StartCap = LineCap.Round, EndCap = LineCap.Round })
                {
                    g.FillPath(brBento, ap);
                    g.DrawPath(addPen, ap);
                    float pcx = _rcAddAppBtn.X + 17f;
                    float pcy = _rcAddAppBtn.Y + 17f;
                    g.DrawLine(plusPen, pcx - 5.5f, pcy, pcx + 5.5f, pcy);
                    g.DrawLine(plusPen, pcx, pcy - 5.5f, pcx, pcy + 5.5f);
                }

                // Right-aligned Quick Pills on Row 3: [ 🎙 Микр ] [ 📸 Скрины ] [ 🌙 Сон-Таймер ]
                float qRight = rightX;
                _rcExpandedWatchdogBtn = new RectangleF(qRight - 68f, curY + 3f, 68f, 28f);
                qRight -= 74f;
                _rcExpandedVaultBtn = new RectangleF(qRight - 64f, curY + 3f, 64f, 28f);
                qRight -= 70f;
                _rcExpandedMicBtn = new RectangleF(qRight - 58f, curY + 3f, 58f, 28f);

                if (_rcExpandedMicBtn.X > _rcAddAppBtn.Right + 6f)
                {
                    Color micC = _audio.IsMicMuted ? Color.FromArgb(alpha, 255, 69, 58) : (_audio.IsMicActive ? Color.FromArgb(alpha, 48, 209, 88) : colMuted);
                    using (GraphicsPath mp = CreateRoundedRectPath(_rcExpandedMicBtn, 9f))
                    {
                        g.FillPath(brBento, mp);
                        g.DrawPath(penBento, mp);
                    }
                    ApplyMicroInteraction(g, _rcExpandedMicBtn, 9f, alpha, micC);
                    DrawVectorMicIcon(g, new RectangleF(_rcExpandedMicBtn.X + 4f, _rcExpandedMicBtn.Y + 4f, 18f, 20f), _audio.IsMicMuted, micC);
                    DrawContrastText(g, _audio.IsMicMuted ? "Выкл" : "Микр", _fontSmall, micC, new RectangleF(_rcExpandedMicBtn.X + 21f, _rcExpandedMicBtn.Y, _rcExpandedMicBtn.Width - 23f, 28f), _sfCenter);
                }
                else _rcExpandedMicBtn = RectangleF.Empty;

                if (_rcExpandedVaultBtn.X > _rcAddAppBtn.Right + 6f)
                {
                    using (GraphicsPath vp = CreateRoundedRectPath(_rcExpandedVaultBtn, 9f))
                    {
                        g.FillPath(brBento, vp);
                        g.DrawPath(penBento, vp);
                    }
                    ApplyMicroInteraction(g, _rcExpandedVaultBtn, 9f, alpha, acc1);
                    Color vCol = _screenshotVault.Count > 0 ? acc1 : colWhite;
                    DrawVectorCameraIcon(g, new RectangleF(_rcExpandedVaultBtn.X + 5f, _rcExpandedVaultBtn.Y + 4f, 18f, 20f), vCol);
                    DrawContrastText(g, _screenshotVault.Count.ToString(), _fontMono, vCol, new RectangleF(_rcExpandedVaultBtn.X + 23f, _rcExpandedVaultBtn.Y, _rcExpandedVaultBtn.Width - 25f, 28f), _sfCenter);
                }
                else _rcExpandedVaultBtn = RectangleF.Empty;

                if (_rcExpandedWatchdogBtn.X > _rcAddAppBtn.Right + 6f)
                {
                    Color wdC = _watchdog.IsCountdownActive ? Color.FromArgb(alpha, 255, 69, 58) : (_watchdog.IsWatchdogArmed ? Color.FromArgb(alpha, 34, 211, 238) : colMuted);
                    using (GraphicsPath wp = CreateRoundedRectPath(_rcExpandedWatchdogBtn, 9f))
                    {
                        g.FillPath(brBento, wp);
                        g.DrawPath(penBento, wp);
                    }
                    ApplyMicroInteraction(g, _rcExpandedWatchdogBtn, 9f, alpha, wdC);
                    DrawVectorMoonIcon(g, new RectangleF(_rcExpandedWatchdogBtn.X + 4f, _rcExpandedWatchdogBtn.Y + 4f, 18f, 20f), wdC);
                    DrawContrastText(g, _watchdog.IsCountdownActive ? (_watchdog.CountdownRemainingSec + "с") : (_watchdog.IsWatchdogArmed ? "ВКЛ" : "Сон"),
                        _watchdog.IsCountdownActive ? _fontMono : _fontSmall, wdC, new RectangleF(_rcExpandedWatchdogBtn.X + 21f, _rcExpandedWatchdogBtn.Y, _rcExpandedWatchdogBtn.Width - 23f, 28f), _sfCenter);
                }
                else _rcExpandedWatchdogBtn = RectangleF.Empty;

                curY += 34f + rowGap;

                // ROW 4: Minimalist Dropzone Pill + Telemetry Status Pill
                if (_config.show_dropzone || _config.show_telemetry)
                {
                    float teleW = _config.show_telemetry ? (_config.show_dropzone ? Math.Min(224f, innerW * 0.48f) : innerW) : 0f;
                    if (_config.show_dropzone)
                    {
                        float dzW = _config.show_telemetry ? (innerW - teleW - 8f) : innerW;
                        _rcDropzone = new RectangleF(padX, curY, dzW, 24f);
                        using (GraphicsPath dzPath = CreateRoundedRectPath(_rcDropzone, 9f))
                        using (var dzBrush = new SolidBrush(_dropzoneFiles.Count > 0
                            ? Color.FromArgb((int)(alpha * 0.36f), acc1.R, acc1.G, acc1.B)
                            : bentoFillCol))
                        {
                            g.FillPath(dzBrush, dzPath);
                            g.DrawPath(penBento, dzPath);
                        }
                        string dzText = _dropzoneFiles.Count == 0
                            ? "Dropzone  ·  Перетащите файлы"
                            : string.Format("Полка: {0} файл(ов)  ·  ПКМ очистить", _dropzoneFiles.Count);
                        DrawContrastText(g, dzText, _fontSmall, _dropzoneFiles.Count > 0 ? colWhite : colMuted, _rcDropzone, _sfCenter);
                    }
                    else
                    {
                        _rcDropzone = RectangleF.Empty;
                    }

                    if (_config.show_telemetry)
                    {
                        var teleRc = new RectangleF(rightX - teleW, curY, teleW, 24f);
                        using (GraphicsPath tp = CreateRoundedRectPath(teleRc, 9f))
                        {
                            g.FillPath(brBento, tp);
                            g.DrawPath(penBento, tp);
                        }
                        DrawContrastText(g, string.Format("CPU {0}%  ·  RAM {1}%  ·  {2}", _cpuUsage, _ramUsage, _netSpeedText),
                            _fontSmall, colMuted, teleRc, _sfCenter);
                    }
                }
            }

            DrawSubtleEdgePageArrows(g, pill, alpha);
        }
    }

    internal static class Program
    {
        private static Mutex _singleInstanceMutex;

        [STAThread]
        private static void Main()
        {
            try
            {
                int myPid = Process.GetCurrentProcess().Id;
                foreach (Process p in Process.GetProcessesByName("DynamicIslandWin"))
                {
                    if (p.Id != myPid)
                    {
                        try { p.Kill(); p.WaitForExit(800); } catch { }
                    }
                }
            }
            catch { }

            bool createdNew;
            _singleInstanceMutex = new Mutex(true, @"Local\DynamicIslandWin_Standalone_SingleInstance", out createdNew);

            Application.SetUnhandledExceptionMode(UnhandledExceptionMode.CatchException);
            Application.ThreadException += (s, exArgs) => { };
            AppDomain.CurrentDomain.UnhandledException += (s, exArgs) => { };

            NativeWin32.EnablePerMonitorDpiV2();
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);

            var mainForm = new DynamicIslandOverlayForm();
            using (var tray = new NotifyIcon())
            {
                try { tray.Icon = Icon.ExtractAssociatedIcon(Application.ExecutablePath); }
                catch { tray.Icon = SystemIcons.Application; }
                tray.Text = "Dynamic Island for Windows";
                tray.Visible = true;

                var trayMenu = new ContextMenuStrip();
                trayMenu.Items.Add("Раскрыть островок", null, (s, e) => mainForm.ExpandIsland());
                trayMenu.Items.Add("Пульт настроек (Kinetic Stage & Deck)...", null, (s, e) => mainForm.OpenSettingsDialog());
                trayMenu.Items.Add("Сбросить позицию по центру", null, (s, e) =>
                {
                    var cfg = ConfigManager.Load();
                    cfg.top_margin = 8; cfg.horizontal_offset = 0;
                    cfg.idle_width = 310; cfg.idle_height = 36;
                    cfg.expanded_width = 520; cfg.expanded_height = 224;
                    ConfigManager.Save(cfg);
                    mainForm.PositionOnMonitor(cfg.monitor_index);
                });
                trayMenu.Items.Add(new ToolStripSeparator());
                trayMenu.Items.Add("Выйти из Dynamic Island", null, (s, e) =>
                {
                    tray.Visible = false;
                    Application.Exit();
                });

                tray.ContextMenuStrip = trayMenu;
                tray.MouseClick += (s, e) =>
                {
                    if (e.Button == MouseButtons.Left)
                        mainForm.ExpandIsland();
                };

                Application.Run(mainForm);
                tray.Visible = false;
            }
            GC.KeepAlive(_singleInstanceMutex);
        }
    }
}