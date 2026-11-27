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
using Windows.Media;
using Windows.Media.Control;
using Windows.Storage.Streams;

using System.Reflection;
[assembly: AssemblyTitle("Dynamic Island for Windows 10/11")]
[assembly: AssemblyDescription("Standalone Native 60 FPS Dynamic Island Overlay for Windows 10 & Windows 11")]
[assembly: AssemblyProduct("Dynamic Island Studio")]
[assembly: AssemblyCompany("DynamicIslandWin")]
[assembly: AssemblyCopyright("Copyright © 2026")]
[assembly: AssemblyVersion("3.2.1.0")]
[assembly: AssemblyFileVersion("3.2.1.0")]
namespace DynamicIslandWin
{
    public static class ColorThemeUtils
    {
        public static string ToHex(Color c)
        {
            return string.Format("#{0:X2}{1:X2}{2:X2}", c.R, c.G, c.B);
        }

        public static Color ParseHex(string hex, Color fallback)
        {
            if (string.IsNullOrWhiteSpace(hex)) return fallback;
            string s = hex.Trim().TrimStart('#');
            if (s.Length == 6)
            {
                int val;
                if (int.TryParse(s, System.Globalization.NumberStyles.HexNumber, System.Globalization.CultureInfo.InvariantCulture, out val))
                    return Color.FromArgb(255, (val >> 16) & 0xFF, (val >> 8) & 0xFF, val & 0xFF);
            }
            return fallback;
        }

        public static double GetLuminance(Color c)
        {
            return 0.2126 * c.R + 0.7152 * c.G + 0.0722 * c.B;
        }

        public static Color Blend(Color a, Color b, float t)
        {
            t = Math.Max(0f, Math.Min(1f, t));
            return Color.FromArgb(
                (int)Math.Round(a.A + (b.A - a.A) * t),
                (int)Math.Round(a.R + (b.R - a.R) * t),
                (int)Math.Round(a.G + (b.G - a.G) * t),
                (int)Math.Round(a.B + (b.B - a.B) * t));
        }

        public static Color BoostVibrance(Color c, float minBright = 0.52f)
        {
            int max = Math.Max(c.R, Math.Max(c.G, c.B));
            int min = Math.Min(c.R, Math.Min(c.G, c.B));
            float r = c.R, g = c.G, b = c.B;
            if (max < (int)(255 * minBright))
            {
                float scale = (max > 8) ? ((255f * minBright) / max) : 1f;
                r = Math.Min(255f, r * scale + 18f);
                g = Math.Min(255f, g * scale + 18f);
                b = Math.Min(255f, b * scale + 18f);
            }
            float avg = (r + g + b) / 3f;
            if (max - min < 55)
            {
                r = Math.Max(0f, Math.Min(255f, avg + (r - avg) * 1.65f));
                g = Math.Max(0f, Math.Min(255f, avg + (g - avg) * 1.65f));
                b = Math.Max(0f, Math.Min(255f, avg + (b - avg) * 1.65f));
            }
            return Color.FromArgb(255, (int)Math.Round(r), (int)Math.Round(g), (int)Math.Round(b));
        }

        public static void ExtractDominantPairFromBitmap(Bitmap bmp, out Color primary, out Color secondary)
        {
            primary = Color.FromArgb(34, 211, 238);
            secondary = Color.FromArgb(236, 72, 153);
            if (bmp == null || bmp.Width < 4 || bmp.Height < 4) return;
            try
            {
                long r1 = 0, g1 = 0, b1 = 0, w1 = 0;
                long r2 = 0, g2 = 0, b2 = 0, w2 = 0;
                int stepX = Math.Max(1, bmp.Width / 12);
                int stepY = Math.Max(1, bmp.Height / 12);
                for (int y = stepY / 2; y < bmp.Height; y += stepY)
                {
                    for (int x = stepX / 2; x < bmp.Width; x += stepX)
                    {
                        Color px = bmp.GetPixel(x, y);
                        if (px.A < 80) continue;
                        int mx = Math.Max(px.R, Math.Max(px.G, px.B));
                        int mn = Math.Min(px.R, Math.Min(px.G, px.B));
                        int sat = mx - mn;
                        if (mx < 28) continue;
                        int weight = 1 + (sat / 8);
                        if (x + y < (bmp.Width + bmp.Height) / 2)
                        {
                            r1 += px.R * weight; g1 += px.G * weight; b1 += px.B * weight; w1 += weight;
                        }
                        else
                        {
                            r2 += px.R * weight; g2 += px.G * weight; b2 += px.B * weight; w2 += weight;
                        }
                    }
                }
                if (w1 > 0) primary = BoostVibrance(Color.FromArgb(255, (int)(r1 / w1), (int)(g1 / w1), (int)(b1 / w1)), 0.58f);
                if (w2 > 0) secondary = BoostVibrance(Color.FromArgb(255, (int)(r2 / w2), (int)(g2 / w2), (int)(b2 / w2)), 0.52f);
                else secondary = Blend(primary, Color.White, 0.28f);

                if (Math.Abs(primary.R - secondary.R) + Math.Abs(primary.G - secondary.G) + Math.Abs(primary.B - secondary.B) < 45)
                {
                    secondary = Color.FromArgb(255, Math.Min(255, primary.B + 45), Math.Max(40, primary.R), Math.Min(255, primary.G + 35));
                }
            }
            catch { }
        }

        public static void ResolveIslandThemeAccents(string preset, int alpha, out Color c1, out Color c2)
        {
            string p = preset ?? "Dinasmic Neon";
            if (p == "Kinetic Orange") { c1 = Color.FromArgb(alpha, 255, 85, 0); c2 = Color.FromArgb(alpha, 255, 159, 10); }
            else if (p == "Acid Lime") { c1 = Color.FromArgb(alpha, 200, 255, 0); c2 = Color.FromArgb(alpha, 16, 185, 129); }
            else if (p == "Dinasmic Neon") { c1 = Color.FromArgb(alpha, 34, 211, 238); c2 = Color.FromArgb(alpha, 236, 72, 153); }
            else if (p == "Apple Blue") { c1 = Color.FromArgb(alpha, 10, 132, 255); c2 = Color.FromArgb(alpha, 139, 92, 246); }
            else if (p == "iOS Cyan-Purple") { c1 = Color.FromArgb(alpha, 34, 211, 238); c2 = Color.FromArgb(alpha, 168, 85, 247); }
            else if (p == "Yandex Gold-Yellow") { c1 = Color.FromArgb(alpha, 255, 214, 10); c2 = Color.FromArgb(alpha, 255, 159, 10); }
            else if (p == "Spotify Neon Green") { c1 = Color.FromArgb(alpha, 48, 209, 88); c2 = Color.FromArgb(alpha, 34, 211, 238); }
            else if (p == "Crimson Sunset") { c1 = Color.FromArgb(alpha, 255, 55, 95); c2 = Color.FromArgb(alpha, 255, 159, 10); }
            else if (p == "Minimal White") { c1 = Color.FromArgb(alpha, 245, 245, 247); c2 = Color.FromArgb(alpha, 174, 174, 178); }
            else { c1 = Color.FromArgb(alpha, 34, 211, 238); c2 = Color.FromArgb(alpha, 192, 132, 252); }
        }
    }


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
        public string settings_theme_preset { get; set; }
        public bool settings_sync_island_accent { get; set; }
        public string settings_custom_accent_hex { get; set; }
        public string settings_custom_bg_hex { get; set; }

        public string eq_style { get; set; }
        public string eq_color_mode { get; set; }
        public string eq_custom_color_hex { get; set; }
        public int eq_bar_count { get; set; }
        public int eq_sensitivity_pct { get; set; }
        public int eq_smoothing_pct { get; set; }
        public bool show_compact_cover { get; set; }

        public bool expand_on_hover { get; set; }
        public int anim_duration_ms { get; set; }
        public int spring_overshoot_pct { get; set; }
        public int collapse_delay_ms { get; set; }
        public bool enable_lctrl_resize { get; set; }
        public bool hide_in_fullscreen { get; set; }
        public bool hide_on_video_playback { get; set; }
        public bool prefer_music_players { get; set; }
        public string default_music_player_id { get; set; }
        public string last_used_music_player_id { get; set; }
        public string last_used_music_player_name { get; set; }
        public string last_used_music_player_path { get; set; }

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
            settings_theme_preset = "Kinetic Dark";
            settings_sync_island_accent = false;
            settings_custom_accent_hex = "#FF5500";
            settings_custom_bg_hex = "";

            eq_style = "Столбики";
            eq_color_mode = "Градиент темы";
            eq_custom_color_hex = "#22D3EE";
            eq_bar_count = 6;
            eq_sensitivity_pct = 100;
            eq_smoothing_pct = 55;
            show_compact_cover = true;

            expand_on_hover = true;
            anim_duration_ms = 330;
            spring_overshoot_pct = 112;
            collapse_delay_ms = 360;
            enable_lctrl_resize = true;
            hide_in_fullscreen = false;
            hide_on_video_playback = false;
            prefer_music_players = true;
            default_music_player_id = "";
            last_used_music_player_id = "";
            last_used_music_player_name = "";
            last_used_music_player_path = "";

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
                if (string.IsNullOrEmpty(cfg.settings_theme_preset)) cfg.settings_theme_preset = "Kinetic Dark";
                if (string.IsNullOrEmpty(cfg.settings_custom_accent_hex)) cfg.settings_custom_accent_hex = "#FF5500";
                if (cfg.settings_custom_bg_hex == null) cfg.settings_custom_bg_hex = "";

                if (string.IsNullOrEmpty(cfg.eq_style)) cfg.eq_style = "Столбики";
                if (cfg.eq_style == "5 Полос") { cfg.eq_style = "Столбики"; if (cfg.eq_bar_count <= 0) cfg.eq_bar_count = 5; }
                else if (cfg.eq_style == "7 Полос") { cfg.eq_style = "Столбики"; if (cfg.eq_bar_count <= 0) cfg.eq_bar_count = 7; }
                else if (cfg.eq_style == "Off") { cfg.eq_style = "Выкл"; }
                if (string.IsNullOrEmpty(cfg.eq_color_mode)) cfg.eq_color_mode = "Градиент темы";
                if (string.IsNullOrEmpty(cfg.eq_custom_color_hex)) cfg.eq_custom_color_hex = "#22D3EE";
                if (cfg.eq_bar_count < 4 || cfg.eq_bar_count > 12) cfg.eq_bar_count = 6;
                if (cfg.eq_sensitivity_pct < 40 || cfg.eq_sensitivity_pct > 220) cfg.eq_sensitivity_pct = 100;
                if (cfg.eq_smoothing_pct < 10 || cfg.eq_smoothing_pct > 95) cfg.eq_smoothing_pct = 55;
                if (string.IsNullOrEmpty(cfg.fullscreen_mode)) cfg.fullscreen_mode = cfg.hide_in_fullscreen ? "Авто-скрытие" : "Неоновая телеметрия";
                if (string.IsNullOrEmpty(cfg.watchdog_action)) cfg.watchdog_action = "Сон";
                if (cfg.download_threshold_mbps < 1) cfg.download_threshold_mbps = 2;
                if (cfg.skipped_version == null) cfg.skipped_version = "";
                if (cfg.default_music_player_id == null) cfg.default_music_player_id = "";
                if (cfg.last_used_music_player_id == null) cfg.last_used_music_player_id = "";
                if (cfg.last_used_music_player_name == null) cfg.last_used_music_player_name = "";
                if (cfg.last_used_music_player_path == null) cfg.last_used_music_player_path = "";
                if (string.IsNullOrWhiteSpace(cfg.github_repo) || cfg.github_repo.IndexOf("DynamicIslandWin/DynamicIslandWin", StringComparison.OrdinalIgnoreCase) >= 0 || cfg.github_repo.IndexOf("/") < 1)
                    cfg.github_repo = "Miltanchik/DynamicIsland";
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
                    if (_parkedNormalRect.Right - _parkedNormalRect.Left < 400 || _parkedNormalRect.Bottom - _parkedNormalRect.Top < 300)
                    {
                        _parkedNormalRect = new NativeWin32.RECT
                        {
                            Left = work.Left + Math.Max(40, (work.Width - 1180) / 2),
                            Top = work.Top + Math.Max(30, (work.Height - 720) / 2),
                            Right = work.Left + Math.Max(40, (work.Width - 1180) / 2) + 1180,
                            Bottom = work.Top + Math.Max(30, (work.Height - 720) / 2) + 720
                        };
                    }
                    _parkedHwnd = target;
                }

                _parkedTick = Environment.TickCount;
                IntPtr prevFg = NativeWin32.GetForegroundWindow();

                // Exit SW_MINIMIZE without stealing focus so Windows & Chromium resume WebRTC <video> swapchain rendering
                if (NativeWin32.IsIconic(target))
                    NativeWin32.ShowWindow(target, NativeWin32.SW_SHOWNOACTIVATE);
                if (NativeWin32.IsIconic(target))
                    NativeWin32.ShowWindow(target, NativeWin32.SW_RESTORE);
                if (NativeWin32.IsZoomed(target))
                    NativeWin32.ShowWindow(target, NativeWin32.SW_SHOWNOACTIVATE);

                int maxParkW = Math.Max(640, bounds.Width - 32);
                int maxParkH = Math.Max(480, bounds.Height - 32);
                int desiredW = _parkedWasZoomed
                    ? Math.Min(maxParkW, Math.Max(960, work.Width - 40))
                    : Math.Min(maxParkW, Math.Max(860, _parkedNormalRect.Right - _parkedNormalRect.Left));
                int desiredH = _parkedWasZoomed
                    ? Math.Min(maxParkH, Math.Max(600, work.Height - 20))
                    : Math.Min(maxParkH, Math.Max(560, _parkedNormalRect.Bottom - _parkedNormalRect.Top));

                // CRITICAL DComp + Chromium Occlusion Fix:
                // 1) Keep 100% of parkW x parkH INSIDE scr.Bounds so DirectComposition NEVER clips the hardware YUV <video> surface to black!
                // 2) Align bottom edge with bounds.Bottom (extending behind the WS_EX_LAYERED Windows Taskbar) so even when a browser or
                //    borderless game is maximized over 100% of WorkingArea, Chromium's NativeWindowOcclusionTrackerWin sees the taskbar-underlap
                //    strip as unoccluded and keeps WebRTC decoding active at 60 FPS!
                // 3) Place at HWND_BOTTOM with SWP_NOACTIVATE so it stays behind all user windows without stealing focus.
                int parkX = bounds.Left + Math.Max(16, (bounds.Width - desiredW) / 2);
                int parkY = Math.Max(bounds.Top + 16, bounds.Bottom - desiredH);

                NativeWin32.SetWindowPos(target, NativeWin32.HWND_BOTTOM, parkX, parkY, desiredW, desiredH,
                    NativeWin32.SWP_NOACTIVATE | NativeWin32.SWP_SHOWWINDOW);

                NativeWin32.PostMessage(target, NativeWin32.WM_NCACTIVATE, new IntPtr(1), IntPtr.Zero);
                NativeWin32.PostMessage(target, NativeWin32.WM_ACTIVATE, new IntPtr(1), IntPtr.Zero);

                if (prevFg != IntPtr.Zero && prevFg != target && NativeWin32.IsWindow(prevFg))
                    NativeWin32.SetForegroundWindow(prevFg);

                // Force DWM thumbnail re-registration because DWM invalidates/freezes the surface handle across iconic transitions!
                UnregisterThumb();
                _lastSrcW = -1;
                _lastSrcH = -1;
                _lastCropMode = -1;
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
                // If user clicked Discord on the Taskbar or Alt+Tab to bring it to front, restore its original normal/maximized placement!
                if (unchecked(Environment.TickCount - _parkedTick) > 650 && NativeWin32.GetForegroundWindow() == target)
                {
                    RestoreParkedWindow(true);
                    UnregisterThumb();
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
            else if (_lastW > 30 && _lastH > 30)
            {
                if (_thumbId == IntPtr.Zero || _boundSourceHwnd != target)
                {
                    SyncHardwareStream(ownerHwnd, _lastX, _lastY, _lastW, _lastH, src, cropModeIndex);
                }
                else
                {
                    NativeWin32.SIZE curSz;
                    if (NativeWin32.DwmQueryThumbnailSourceSize(_thumbId, out curSz) != 0 ||
                        curSz.cx < 40 || curSz.cy < 40 ||
                        curSz.cx != _lastSrcW || curSz.cy != _lastSrcH)
                    {
                        if (curSz.cx < 40 || curSz.cy < 40) UnregisterThumb();
                        SyncHardwareStream(ownerHwnd, _lastX, _lastY, _lastW, _lastH, src, cropModeIndex);
                    }
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

        private static readonly HashSet<string> VideoPlayerProcessStems = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "vlc", "mpv", "mpc-hc", "mpc-hc64", "mpc-be", "mpc-be64",
            "potplayer", "potplayermini", "potplayermini64", "kmplayer",
            "gom", "gom64", "video.ui", "zunevideo", "smplayer", "ACGPlayer"
        };

        private static readonly HashSet<string> BrowserProcessStems = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "chrome", "msedge", "firefox", "opera", "opera_gx", "yandex",
            "browser", "brave", "vivaldi", "arc", "zen", "waterfox", "thorium", "librewolf", "floorp"
        };

        public void CheckActiveVideoAndBrowserAudio(out bool localVideoAudioActive, out bool browserAudioActive)
        {
            localVideoAudioActive = false;
            browserAudioActive = false;
            if (_sessionMgr == null) InitializeEndpoint();
            if (_sessionMgr == null) return;
            try
            {
                IAudioSessionEnumerator sesEnum;
                if (_sessionMgr.GetSessionEnumerator(out sesEnum) != 0 || sesEnum == null) return;
                int count;
                if (sesEnum.GetCount(out count) != 0) return;

                for (int i = 0; i < count; i++)
                {
                    object raw;
                    if (sesEnum.GetSession(i, out raw) != 0 || raw == null) continue;
                    var ctrl2 = raw as IAudioSessionControl2;
                    if (ctrl2 == null) continue;

                    int state;
                    if (ctrl2.GetState(out state) != 0 || state != 1) continue; // 1 == AudioSessionStateActive

                    uint pid;
                    ctrl2.GetProcessId(out pid);
                    if (pid == 0) continue;

                    string pName = "";
                    try { pName = Process.GetProcessById((int)pid).ProcessName ?? ""; } catch { }
                    if (string.IsNullOrEmpty(pName)) continue;

                    if (VideoPlayerProcessStems.Contains(pName))
                        localVideoAudioActive = true;
                    else if (BrowserProcessStems.Contains(pName))
                        browserAudioActive = true;

                    if (localVideoAudioActive && browserAudioActive) break;
                }
            }
            catch { _sessionMgr = null; }
        }

        public bool IsAnyAudioSessionActiveForKeywords(string[] keywords)
        {
            if (keywords == null || keywords.Length == 0) return false;
            if (_sessionMgr == null) InitializeEndpoint();
            if (_sessionMgr == null) return false;
            try
            {
                IAudioSessionEnumerator sesEnum;
                if (_sessionMgr.GetSessionEnumerator(out sesEnum) != 0 || sesEnum == null) return false;
                int count;
                if (sesEnum.GetCount(out count) != 0) return false;
                for (int i = 0; i < count; i++)
                {
                    object raw;
                    if (sesEnum.GetSession(i, out raw) != 0 || raw == null) continue;
                    var ctrl2 = raw as IAudioSessionControl2;
                    if (ctrl2 == null) continue;
                    int state;
                    if (ctrl2.GetState(out state) != 0 || state != 1) continue;
                    uint pid;
                    ctrl2.GetProcessId(out pid);
                    if (pid == 0) continue;
                    string pName = "";
                    try { pName = Process.GetProcessById((int)pid).ProcessName ?? ""; } catch { }
                    string sesId = "";
                    try { ctrl2.GetSessionIdentifier(out sesId); } catch { }
                    string comb = (pName + " " + sesId).ToLowerInvariant();
                    foreach (string kw in keywords)
                    {
                        if (!string.IsNullOrEmpty(kw) && comb.Contains(kw.ToLowerInvariant()))
                            return true;
                    }
                }
            }
            catch { }
            return false;
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
        public bool IsVideoPlayback = false;
        public double PositionSec = 0.0;
        public double DurationSec = 0.0;
        public DateTime SampledAtUtc = DateTime.UtcNow;
        public Bitmap CoverBitmap = null;
        public Color CoverColor1 = Color.FromArgb(34, 211, 238);
        public Color CoverColor2 = Color.FromArgb(236, 72, 153);
        public bool HasCoverColors = false;
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
        private static Color _cachedCovC1 = Color.FromArgb(34, 211, 238);
        private static Color _cachedCovC2 = Color.FromArgb(236, 72, 153);
        private static int _selectedPlayerIndex = 0;
        private static string _manualSelectedAppId = "";
        public static volatile bool SmtcVideoPlayingNow = false;
        public static volatile bool SmtcBrowserPlayingNow = false;

        private static readonly string[] PureMusicOnlyKeywords = new string[]
        {
            "яндекс", "yandexmusic", "yandex.music", "spotify", "aimp", "foobar",
            "itunes", "applemusic", "zunemusic", "music.ui", "deezer", "tidal",
            "soundcloud", "vkmusic", "vk music", "winamp", "musicbee"
        };

        private static readonly string[] DedicatedVideoAppKeywords = new string[]
        {
            "vlc", "mpv", "mpc-hc", "mpc-be", "potplayer", "kmplayer",
            "gom", "zunevideo", "video.ui", "smplayer"
        };

        public static bool IsPureMusicOnlyApp(string appIdOrName)
        {
            if (string.IsNullOrEmpty(appIdOrName)) return false;
            string low = appIdOrName.ToLowerInvariant();
            foreach (string b in BrowserKeywords)
                if (low.Contains(b)) return false;
            foreach (string m in PureMusicOnlyKeywords)
                if (low.Contains(m)) return true;
            return false;
        }

        public static bool IsDedicatedVideoApp(string appIdOrName)
        {
            if (string.IsNullOrEmpty(appIdOrName)) return false;
            string low = appIdOrName.ToLowerInvariant();
            foreach (string v in DedicatedVideoAppKeywords)
                if (low.Contains(v)) return true;
            return false;
        }
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
            bool foundSmtcVideoPlaying = false;
            bool foundBrowserSmtcPlaying = false;
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

                            bool isPureMusic = IsPureMusicOnlyApp(appId);
                            bool isDedVideo = IsDedicatedVideoApp(appId);
                            bool smtcTypeVideo = false;
                            bool smtcTypeMusic = false;
                            try
                            {
                                if (pb != null && pb.PlaybackType.HasValue)
                                {
                                    if (pb.PlaybackType.Value == MediaPlaybackType.Video) smtcTypeVideo = true;
                                    else if (pb.PlaybackType.Value == MediaPlaybackType.Music) smtcTypeMusic = true;
                                }
                                if (props != null && props.PlaybackType.HasValue)
                                {
                                    if (props.PlaybackType.Value == MediaPlaybackType.Video) smtcTypeVideo = true;
                                    else if (props.PlaybackType.Value == MediaPlaybackType.Music) smtcTypeMusic = true;
                                }
                            }
                            catch { }

                            string appLower = appId.ToLowerInvariant();
                            bool isBrowserSession = false;
                            foreach (string bkw in BrowserKeywords)
                                if (appLower.Contains(bkw)) { isBrowserSession = true; break; }

                            bool titleLooksVideoExt = false;
                            if (!smtcTypeMusic && !isPureMusic && !string.IsNullOrEmpty(title))
                            {
                                string tlLow = title.ToLowerInvariant();
                                if (tlLow.EndsWith(".mp4") || tlLow.EndsWith(".mkv") || tlLow.EndsWith(".avi") ||
                                    tlLow.EndsWith(".webm") || tlLow.EndsWith(".mov") || tlLow.EndsWith(".wmv") || tlLow.EndsWith(".m4v"))
                                    titleLooksVideoExt = true;
                            }

                            bool isVideoSession = !isPureMusic && (smtcTypeVideo || isDedVideo || titleLooksVideoExt);
                            if (isPlaying && isVideoSession)
                                foundSmtcVideoPlaying = true;
                            if (isPlaying && isBrowserSession && !smtcTypeMusic)
                                foundBrowserSmtcPlaying = true;

                            var snap = new MediaSessionSnapshot
                            {
                                AppId = appId,
                                DisplayAppName = FormatFriendlyAppName(appId),
                                Title = title,
                                Artist = artist,
                                IsPlaying = isPlaying,
                                IsDedicatedPlayer = IsDedicatedMusicApp(appId),
                                IsVideoPlayback = isVideoSession,
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

            SmtcVideoPlayingNow = foundSmtcVideoPlaying;
            SmtcBrowserPlayingNow = foundBrowserSmtcPlaying;

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
                    snap.CoverColor1 = _cachedCovC1;
                    snap.CoverColor2 = _cachedCovC2;
                    snap.HasCoverColors = true;
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
                                Color ext1, ext2;
                                ColorThemeUtils.ExtractDominantPairFromBitmap(bmp, out ext1, out ext2);
                                _cachedCoverBmp = bmp;
                                _cachedCovC1 = ext1;
                                _cachedCovC2 = ext2;
                                _cachedCoverKey = key;
                                snap.CoverBitmap = _cachedCoverBmp;
                                snap.CoverColor1 = ext1;
                                snap.CoverColor2 = ext2;
                                snap.HasCoverColors = true;
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

        public static IntPtr FindRenderWidgetChild(IntPtr topHwnd)
        {
            if (topHwnd == IntPtr.Zero || !NativeWin32.IsWindow(topHwnd)) return IntPtr.Zero;
            IntPtr foundRender = IntPtr.Zero;
            int bestArea = -1;
            try
            {
                NativeWin32.EnumChildWindows(topHwnd, (ch, lParam) =>
                {
                    var cls = new StringBuilder(128);
                    NativeWin32.GetClassName(ch, cls, 128);
                    if (cls.ToString().IndexOf("Chrome_RenderWidgetHostHWND", StringComparison.Ordinal) >= 0)
                    {
                        NativeWin32.RECT r;
                        int area = 0;
                        if (NativeWin32.GetWindowRect(ch, out r))
                            area = Math.Max(0, r.Right - r.Left) * Math.Max(0, r.Bottom - r.Top);
                        if (area > bestArea) { bestArea = area; foundRender = ch; }
                    }
                    return true;
                }, IntPtr.Zero);
            }
            catch { }
            return foundRender;
        }

        public static IntPtr FindPlayerWindowSafe(MediaSessionSnapshot snap, bool isSpotify, bool isYandex, out string bestWindowTitle, out IntPtr renderWidgetHwnd)
        {
            bestWindowTitle = "";
            renderWidgetHwnd = IntPtr.Zero;
            if (snap == null) return IntPtr.Zero;

            string stem = snap.AppId ?? "";
            if (stem.EndsWith(".exe", StringComparison.OrdinalIgnoreCase)) stem = stem.Substring(0, stem.Length - 4);
            if (stem.Contains("!")) stem = stem.Split('!')[0];

            var pidSet = new HashSet<uint>();
            try
            {
                if (!string.IsNullOrEmpty(stem))
                    foreach (Process p in Process.GetProcessesByName(stem)) pidSet.Add((uint)p.Id);
                if (isYandex)
                {
                    foreach (Process p in Process.GetProcessesByName("Яндекс Музыка")) pidSet.Add((uint)p.Id);
                    foreach (Process p in Process.GetProcessesByName("YandexMusic")) pidSet.Add((uint)p.Id);
                }
                if (isSpotify)
                    foreach (Process p in Process.GetProcessesByName("Spotify")) pidSet.Add((uint)p.Id);
            }
            catch { }

            IntPtr bestHwnd = IntPtr.Zero, bestRender = IntPtr.Zero;
            string bestTitle = "";
            int bestScore = int.MinValue;

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
                    if (clsName == "Chrome_WidgetWin_0" || clsName == "IME" || clsName == "MSCTFIME UI") return true;

                    var sbTitle = new StringBuilder(256);
                    NativeWin32.GetWindowText(hWnd, sbTitle, 256);
                    string wTitle = sbTitle.ToString();

                    NativeWin32.RECT rc;
                    NativeWin32.GetWindowRect(hWnd, out rc);
                    int w = rc.Right - rc.Left, h = rc.Bottom - rc.Top;
                    bool vis = NativeWin32.IsWindowVisible(hWnd);
                    bool iconic = NativeWin32.IsIconic(hWnd);
                    IntPtr childRender = FindRenderWidgetChild(hWnd);

                    int sc = 0;
                    if (childRender != IntPtr.Zero) sc += 5000;
                    if (clsName == "Chrome_WidgetWin_1" || clsName.Contains("Qt") || clsName.Contains("AIMP")) sc += 2000;
                    else if (clsName.Contains("Chrome_WidgetWin")) sc += 600;
                    if (!string.IsNullOrWhiteSpace(wTitle)) sc += 1200;
                    if (vis && !iconic && w >= 180 && h >= 120) sc += 1500 + Math.Min(900, (w * h) / 2500);
                    else if (iconic) sc += 800;
                    else if (!vis && (w >= 160 || childRender != IntPtr.Zero)) sc += 500;

                    if (sc > bestScore && sc >= 1200)
                    {
                        bestScore = sc; bestHwnd = hWnd; bestRender = childRender; bestTitle = wTitle;
                    }
                    return true;
                }, IntPtr.Zero);
            }

            bestWindowTitle = bestTitle;
            renderWidgetHwnd = bestRender;
            return bestHwnd;
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

    // =========================================================================
    // 3B. NATIVE COM CUIAutomation8 (UIA v3) STA FAVORITE SYNC & SAFE DISPATCH
    //     Replaces legacy .NET 3.0 System.Windows.Automation (which crashes Electron/CEF)
    // =========================================================================
    [ComImport, Guid("6D5140C1-7436-11CE-8034-00AA006009FA"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    internal interface IComServiceProvider
    {
        [PreserveSig] int QueryService(ref Guid guidService, ref Guid riid, out IntPtr ppvObject);
    }

    [ComImport, Guid("E22AD333-B25F-460C-83D0-0581107395C9")]
    public sealed class DiscoveredMusicPlayer
    {
        public string Id = "";
        public string DisplayName = "";
        public string LaunchTarget = "";
        public string LaunchArgs = "";
        public string IconSourcePath = "";
        public bool IsWebFallback = false;
        public bool IsLastUsed = false;
        public string[] MatchKeywords = new string[0];
    }

    public static class InstalledMusicPlayerDiscovery
    {
        private static readonly object _discLock = new object();
        private static List<DiscoveredMusicPlayer> _cachedPlayers = null;
        private static int _lastScanTick = 0;

        private sealed class PlayerCatalogSpec
        {
            public string Id;
            public string Name;
            public string[] ExeCandidates;
            public string[] ShortcutNames;
            public string[] UwpPrefixes;
            public string UwpEntryPoint;
            public string WebUrl;
            public string[] Keywords;
        }

        private static readonly PlayerCatalogSpec[] Catalog = new PlayerCatalogSpec[]
        {
            new PlayerCatalogSpec
            {
                Id = "yandex_music", Name = "Яндекс Музыка",
                ExeCandidates = new[] { @"%LOCALAPPDATA%\Programs\YandexMusic\Яндекс Музыка.exe", @"%LOCALAPPDATA%\Programs\YandexMusic\YandexMusic.exe" },
                ShortcutNames = new[] { "яндекс музыка", "yandex music" },
                UwpPrefixes = new[] { "Yandex.Music_" }, UwpEntryPoint = "App",
                WebUrl = "https://music.yandex.ru",
                Keywords = new[] { "яндекс", "yandexmusic", "yandex.music", "music.yandex" }
            },
            new PlayerCatalogSpec
            {
                Id = "spotify", Name = "Spotify",
                ExeCandidates = new[] { @"%APPDATA%\Spotify\Spotify.exe", @"%LOCALAPPDATA%\Microsoft\WindowsApps\Spotify.exe" },
                ShortcutNames = new[] { "spotify" },
                UwpPrefixes = new[] { "SpotifyAB.SpotifyMusic_" }, UwpEntryPoint = "Spotify",
                WebUrl = "https://open.spotify.com",
                Keywords = new[] { "spotify" }
            },
            new PlayerCatalogSpec
            {
                Id = "vk_music", Name = "VK Музыка",
                ExeCandidates = new[] { @"%LOCALAPPDATA%\Programs\vk-music\VK Музыка.exe", @"%LOCALAPPDATA%\Programs\VK Music\VK Music.exe", @"%LOCALAPPDATA%\Programs\vkmusic\VKMusic.exe" },
                ShortcutNames = new[] { "vk музыка", "vk music", "вк музыка" },
                UwpPrefixes = new string[0], UwpEntryPoint = "App",
                WebUrl = "https://vkmusic.vk.com",
                Keywords = new[] { "vkmusic", "vk музыка", "vk music", "vk.com" }
            },
            new PlayerCatalogSpec
            {
                Id = "youtube_music", Name = "YouTube Music",
                ExeCandidates = new[] { @"%LOCALAPPDATA%\Programs\youtube-music\YouTube Music.exe", @"%LOCALAPPDATA%\Programs\YouTube Music Desktop App\YouTube Music Desktop App.exe" },
                ShortcutNames = new[] { "youtube music", "yt music" },
                UwpPrefixes = new string[0], UwpEntryPoint = "App",
                WebUrl = "https://music.youtube.com",
                Keywords = new[] { "youtube music", "youtube-music", "music.youtube" }
            },
            new PlayerCatalogSpec
            {
                Id = "soundcloud", Name = "SoundCloud",
                ExeCandidates = new[] { @"%LOCALAPPDATA%\Programs\SoundCloud\SoundCloud.exe" },
                ShortcutNames = new[] { "soundcloud" },
                UwpPrefixes = new[] { "SoundCloud." }, UwpEntryPoint = "App",
                WebUrl = "https://soundcloud.com/discover",
                Keywords = new[] { "soundcloud" }
            },
            new PlayerCatalogSpec
            {
                Id = "apple_music", Name = "Apple Music",
                ExeCandidates = new[] { @"%LOCALAPPDATA%\Microsoft\WindowsApps\AppleMusic.exe", @"%ProgramFiles%\iTunes\iTunes.exe", @"%ProgramFiles(x86)%\iTunes\iTunes.exe" },
                ShortcutNames = new[] { "apple music", "itunes" },
                UwpPrefixes = new[] { "AppleInc.AppleMusicWin_", "AppleInc.iTunes_" }, UwpEntryPoint = "App",
                WebUrl = "https://music.apple.com",
                Keywords = new[] { "applemusic", "itunes", "apple music" }
            },
            new PlayerCatalogSpec
            {
                Id = "aimp", Name = "AIMP",
                ExeCandidates = new[] { @"%ProgramFiles%\AIMP\AIMP.exe", @"%ProgramFiles(x86)%\AIMP\AIMP.exe", @"%LOCALAPPDATA%\Programs\AIMP\AIMP.exe" },
                ShortcutNames = new[] { "aimp" },
                UwpPrefixes = new string[0], UwpEntryPoint = "",
                WebUrl = "",
                Keywords = new[] { "aimp" }
            },
            new PlayerCatalogSpec
            {
                Id = "foobar2000", Name = "foobar2000",
                ExeCandidates = new[] { @"%ProgramFiles%\foobar2000\foobar2000.exe", @"%ProgramFiles(x86)%\foobar2000\foobar2000.exe" },
                ShortcutNames = new[] { "foobar2000" },
                UwpPrefixes = new string[0], UwpEntryPoint = "",
                WebUrl = "",
                Keywords = new[] { "foobar" }
            },
            new PlayerCatalogSpec
            {
                Id = "musicbee", Name = "MusicBee",
                ExeCandidates = new[] { @"%ProgramFiles%\MusicBee\MusicBee.exe", @"%ProgramFiles(x86)%\MusicBee\MusicBee.exe" },
                ShortcutNames = new[] { "musicbee" },
                UwpPrefixes = new string[0], UwpEntryPoint = "",
                WebUrl = "",
                Keywords = new[] { "musicbee" }
            },
            new PlayerCatalogSpec
            {
                Id = "wmp", Name = "Медиаплеер Windows",
                ExeCandidates = new[] { @"%LOCALAPPDATA%\Microsoft\WindowsApps\Microsoft.Media.Player.exe" },
                ShortcutNames = new[] { "медиаплеер", "media player" },
                UwpPrefixes = new[] { "Microsoft.ZuneMusic_" }, UwpEntryPoint = "Microsoft.ZuneMusic",
                WebUrl = "",
                Keywords = new[] { "zunemusic", "music.ui", "microsoft.media.player", "wmplayer" }
            }
        };

        public static void RememberFromActiveSession(IslandConfigData cfg, MediaSessionSnapshot snap)
        {
            if (cfg == null || snap == null || string.IsNullOrEmpty(snap.AppId) || snap.NativeSession == null) return;
            if (snap.IsVideoPlayback || !snap.IsDedicatedPlayer) return;

            string appLow = (snap.AppId + " " + snap.DisplayAppName).ToLowerInvariant();
            string matchedId = "";
            string matchedName = snap.DisplayAppName;
            foreach (var spec in Catalog)
            {
                foreach (string kw in spec.Keywords)
                {
                    if (appLow.Contains(kw))
                    {
                        matchedId = spec.Id;
                        matchedName = spec.Name;
                        break;
                    }
                }
                if (!string.IsNullOrEmpty(matchedId)) break;
            }
            if (string.IsNullOrEmpty(matchedId)) matchedId = snap.AppId;

            string exe = snap.ExecutablePath ?? "";
            if (string.IsNullOrEmpty(exe))
            {
                try
                {
                    string stem = snap.AppId;
                    if (stem.EndsWith(".exe", StringComparison.OrdinalIgnoreCase)) stem = stem.Substring(0, stem.Length - 4);
                    if (!stem.Contains("!"))
                    {
                        Process[] ps = Process.GetProcessesByName(stem);
                        if (ps.Length == 0 && matchedId == "yandex_music") ps = Process.GetProcessesByName("Яндекс Музыка");
                        if (ps.Length > 0) { try { exe = ps[0].MainModule.FileName; } catch { } }
                    }
                }
                catch { }
            }

            bool changed = !string.Equals(cfg.last_used_music_player_id, matchedId, StringComparison.OrdinalIgnoreCase) ||
                           (!string.IsNullOrEmpty(exe) && !string.Equals(cfg.last_used_music_player_path, exe, StringComparison.OrdinalIgnoreCase));
            if (changed)
            {
                cfg.last_used_music_player_id = matchedId;
                cfg.last_used_music_player_name = matchedName;
                if (!string.IsNullOrEmpty(exe)) cfg.last_used_music_player_path = exe;
                ConfigManager.Save(cfg);
            }
        }

        private static Dictionary<string, string> ScanStartMenuShortcuts()
        {
            var found = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            string[] roots = new string[]
            {
                Environment.GetFolderPath(Environment.SpecialFolder.Programs),
                Environment.GetFolderPath(Environment.SpecialFolder.CommonPrograms),
                Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory)
            };
            foreach (string root in roots)
            {
                if (string.IsNullOrEmpty(root) || !Directory.Exists(root)) continue;
                try
                {
                    foreach (string f in Directory.GetFiles(root, "*.lnk", SearchOption.TopDirectoryOnly))
                        RegisterShortcutCandidate(found, f);
                    foreach (string sub in Directory.GetDirectories(root))
                    {
                        try
                        {
                            foreach (string f in Directory.GetFiles(sub, "*.lnk", SearchOption.TopDirectoryOnly))
                                RegisterShortcutCandidate(found, f);
                        }
                        catch { }
                    }
                }
                catch { }
            }
            return found;
        }

        private static void RegisterShortcutCandidate(Dictionary<string, string> map, string lnkPath)
        {
            string nm = (Path.GetFileNameWithoutExtension(lnkPath) ?? "").ToLowerInvariant();
            if (nm.Contains("uninstall") || nm.Contains("удалить") || nm.Contains("деинстал")) return;
            foreach (var spec in Catalog)
            {
                if (map.ContainsKey(spec.Id)) continue;
                foreach (string sn in spec.ShortcutNames)
                {
                    if (nm.Equals(sn, StringComparison.OrdinalIgnoreCase) || nm.Contains(sn))
                    {
                        map[spec.Id] = lnkPath;
                        break;
                    }
                }
            }
        }

        private static Dictionary<string, string> ScanRegistryPlayers()
        {
            var found = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            string[] uninstPaths = new string[]
            {
                @"Software\Microsoft\Windows\CurrentVersion\Uninstall",
                @"SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall",
                @"SOFTWARE\WOW6432Node\Microsoft\Windows\CurrentVersion\Uninstall"
            };
            RegistryKey[] hives = new RegistryKey[] { Registry.CurrentUser, Registry.LocalMachine, Registry.LocalMachine };
            for (int r = 0; r < uninstPaths.Length; r++)
            {
                try
                {
                    using (RegistryKey baseKey = hives[r].OpenSubKey(uninstPaths[r], false))
                    {
                        if (baseKey == null) continue;
                        foreach (string subName in baseKey.GetSubKeyNames())
                        {
                            try
                            {
                                using (RegistryKey sk = baseKey.OpenSubKey(subName, false))
                                {
                                    if (sk == null) continue;
                                    string disp = Convert.ToString(sk.GetValue("DisplayName") ?? "");
                                    if (string.IsNullOrEmpty(disp)) continue;
                                    string dLow = disp.ToLowerInvariant();
                                    foreach (var spec in Catalog)
                                    {
                                        if (found.ContainsKey(spec.Id)) continue;
                                        bool match = false;
                                        foreach (string sn in spec.ShortcutNames)
                                            if (dLow.Contains(sn)) { match = true; break; }
                                        if (!match) continue;

                                        string iconRaw = Convert.ToString(sk.GetValue("DisplayIcon") ?? "").Trim('"', ' ');
                                        int comma = iconRaw.IndexOf(',');
                                        if (comma > 0) iconRaw = iconRaw.Substring(0, comma).Trim('"', ' ');
                                        if (iconRaw.EndsWith(".exe", StringComparison.OrdinalIgnoreCase) &&
                                            iconRaw.IndexOf("uninst", StringComparison.OrdinalIgnoreCase) < 0 &&
                                            File.Exists(iconRaw))
                                        {
                                            found[spec.Id] = iconRaw;
                                            continue;
                                        }
                                        string loc = Convert.ToString(sk.GetValue("InstallLocation") ?? "").Trim('"', ' ');
                                        if (!string.IsNullOrEmpty(loc) && Directory.Exists(loc))
                                        {
                                            foreach (string exe in Directory.GetFiles(loc, "*.exe", SearchOption.TopDirectoryOnly))
                                            {
                                                string fn = Path.GetFileName(exe).ToLowerInvariant();
                                                if (!fn.Contains("unins") && !fn.Contains("update") && !fn.Contains("crash"))
                                                {
                                                    found[spec.Id] = exe;
                                                    break;
                                                }
                                            }
                                        }
                                    }
                                }
                            }
                            catch { }
                        }
                    }
                }
                catch { }
            }
            return found;
        }

        private static string TryFindUwpAppUri(string[] prefixes, string entryPoint)
        {
            if (prefixes == null || prefixes.Length == 0) return "";
            try
            {
                string pkgRoot = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Packages");
                if (!Directory.Exists(pkgRoot)) return "";
                foreach (string dir in Directory.GetDirectories(pkgRoot))
                {
                    string folder = Path.GetFileName(dir) ?? "";
                    foreach (string pref in prefixes)
                    {
                        if (folder.StartsWith(pref, StringComparison.OrdinalIgnoreCase))
                        {
                            // Convert PackageFamilyName folder (Name_8wekyb3d8bbwe) to shell:AppsFolder URI
                            return @"shell:AppsFolder\" + folder + "!" + (string.IsNullOrEmpty(entryPoint) ? "App" : entryPoint);
                        }
                    }
                }
            }
            catch { }
            return "";
        }

        public static List<DiscoveredMusicPlayer> DiscoverPlayers(IslandConfigData cfg, bool forceRefresh = false)
        {
            int now = Environment.TickCount;
            lock (_discLock)
            {
                if (!forceRefresh && _cachedPlayers != null && unchecked(now - _lastScanTick) < 30000)
                    return OrderWithConfig(_cachedPlayers, cfg);
            }

            var localList = new List<DiscoveredMusicPlayer>();
            var webFallbacks = new List<DiscoveredMusicPlayer>();
            var shortcuts = ScanStartMenuShortcuts();
            var regExes = ScanRegistryPlayers();

            foreach (var spec in Catalog)
            {
                string target = "";
                string args = "";
                string iconSrc = "";

                // 1) Direct known paths
                foreach (string raw in spec.ExeCandidates)
                {
                    string exp = Environment.ExpandEnvironmentVariables(raw);
                    if (File.Exists(exp)) { target = exp; iconSrc = exp; break; }
                }
                // 2) Registry Uninstall / App Paths
                if (string.IsNullOrEmpty(target) && regExes.ContainsKey(spec.Id))
                {
                    target = regExes[spec.Id];
                    iconSrc = target;
                }
                // 3) Start Menu / Desktop / Browser PWA .lnk
                if (string.IsNullOrEmpty(target) && shortcuts.ContainsKey(spec.Id))
                {
                    target = shortcuts[spec.Id];
                    iconSrc = target;
                }
                // 4) UWP Store package
                if (string.IsNullOrEmpty(target))
                {
                    string uwpUri = TryFindUwpAppUri(spec.UwpPrefixes, spec.UwpEntryPoint);
                    if (!string.IsNullOrEmpty(uwpUri))
                    {
                        target = "explorer.exe";
                        args = uwpUri;
                    }
                }

                if (!string.IsNullOrEmpty(target))
                {
                    localList.Add(new DiscoveredMusicPlayer
                    {
                        Id = spec.Id,
                        DisplayName = spec.Name,
                        LaunchTarget = target,
                        LaunchArgs = args,
                        IconSourcePath = iconSrc,
                        IsWebFallback = false,
                        MatchKeywords = spec.Keywords
                    });
                }
                else if (!string.IsNullOrEmpty(spec.WebUrl))
                {
                    webFallbacks.Add(new DiscoveredMusicPlayer
                    {
                        Id = "web_" + spec.Id,
                        DisplayName = spec.Name + " (Веб)",
                        LaunchTarget = spec.WebUrl,
                        LaunchArgs = "",
                        IconSourcePath = "",
                        IsWebFallback = true,
                        MatchKeywords = spec.Keywords
                    });
                }
            }

            // Include custom last-used executable if it wasn't part of standard catalog
            if (cfg != null && !string.IsNullOrEmpty(cfg.last_used_music_player_path) && File.Exists(cfg.last_used_music_player_path))
            {
                bool already = false;
                foreach (var p in localList)
                    if (string.Equals(p.LaunchTarget, cfg.last_used_music_player_path, StringComparison.OrdinalIgnoreCase) ||
                        string.Equals(p.Id, cfg.last_used_music_player_id, StringComparison.OrdinalIgnoreCase))
                    { already = true; break; }
                if (!already)
                {
                    string nm = string.IsNullOrEmpty(cfg.last_used_music_player_name) ? Path.GetFileNameWithoutExtension(cfg.last_used_music_player_path) : cfg.last_used_music_player_name;
                    localList.Insert(0, new DiscoveredMusicPlayer
                    {
                        Id = string.IsNullOrEmpty(cfg.last_used_music_player_id) ? nm : cfg.last_used_music_player_id,
                        DisplayName = nm,
                        LaunchTarget = cfg.last_used_music_player_path,
                        IconSourcePath = cfg.last_used_music_player_path,
                        IsWebFallback = false,
                        MatchKeywords = new[] { nm.ToLowerInvariant() }
                    });
                }
            }

            var combined = new List<DiscoveredMusicPlayer>(localList.Count + webFallbacks.Count);
            combined.AddRange(localList);
            combined.AddRange(webFallbacks);

            lock (_discLock)
            {
                _cachedPlayers = combined;
                _lastScanTick = now;
            }
            return OrderWithConfig(combined, cfg);
        }

        private static List<DiscoveredMusicPlayer> OrderWithConfig(List<DiscoveredMusicPlayer> src, IslandConfigData cfg)
        {
            var res = new List<DiscoveredMusicPlayer>(src.Count);
            string lastId = cfg != null ? (cfg.last_used_music_player_id ?? "") : "";
            DiscoveredMusicPlayer topLast = null;
            foreach (var p in src)
            {
                bool isLast = !string.IsNullOrEmpty(lastId) && string.Equals(p.Id, lastId, StringComparison.OrdinalIgnoreCase);
                var copy = new DiscoveredMusicPlayer
                {
                    Id = p.Id, DisplayName = p.DisplayName, LaunchTarget = p.LaunchTarget,
                    LaunchArgs = p.LaunchArgs, IconSourcePath = p.IconSourcePath,
                    IsWebFallback = p.IsWebFallback, IsLastUsed = isLast, MatchKeywords = p.MatchKeywords
                };
                if (isLast && topLast == null) topLast = copy;
                else res.Add(copy);
            }
            if (topLast != null) res.Insert(0, topLast);
            return res;
        }

        public static DiscoveredMusicPlayer ResolveDefaultPlayer(IslandConfigData cfg)
        {
            if (cfg == null || string.IsNullOrWhiteSpace(cfg.default_music_player_id)) return null;
            var all = DiscoverPlayers(cfg, false);
            foreach (var p in all)
                if (string.Equals(p.Id, cfg.default_music_player_id, StringComparison.OrdinalIgnoreCase))
                    return p;
            return null;
        }

        private static IntPtr FindPlayerMainWindowByKeywords(string[] keywords)
        {
            if (keywords == null || keywords.Length == 0) return IntPtr.Zero;
            IntPtr found = IntPtr.Zero;
            int bestArea = 0;
            try
            {
                NativeWin32.EnumWindows((hWnd, lParam) =>
                {
                    if (!NativeWin32.IsWindowVisible(hWnd) && !NativeWin32.IsIconic(hWnd)) return true;
                    uint pid;
                    NativeWin32.GetWindowThreadProcessId(hWnd, out pid);
                    if (pid == 0) return true;
                    string pName = "";
                    try { pName = Process.GetProcessById((int)pid).ProcessName ?? ""; } catch { }
                    var sbTitle = new StringBuilder(256);
                    NativeWin32.GetWindowText(hWnd, sbTitle, 256);
                    string comb = (pName + " " + sbTitle.ToString()).ToLowerInvariant();

                    bool matched = false;
                    foreach (string kw in keywords)
                        if (!string.IsNullOrEmpty(kw) && comb.Contains(kw.ToLowerInvariant())) { matched = true; break; }
                    if (!matched) return true;

                    NativeWin32.RECT rc;
                    int area = 100;
                    if (NativeWin32.GetWindowRect(hWnd, out rc))
                        area = Math.Max(1, (rc.Right - rc.Left) * (rc.Bottom - rc.Top));
                    if (area > bestArea) { bestArea = area; found = hWnd; }
                    return true;
                }, IntPtr.Zero);
            }
            catch { }
            return found;
        }

        private static void SendAppCommandPlayToWindow(IntPtr hwnd, string command)
        {
            if (hwnd == IntPtr.Zero || !NativeWin32.IsWindow(hwnd)) return;
            int appCmd = command == "next" ? NativeWin32.APPCOMMAND_MEDIA_NEXTTRACK
                       : (command == "prev" ? NativeWin32.APPCOMMAND_MEDIA_PREVIOUSTRACK : NativeWin32.APPCOMMAND_MEDIA_PLAY);
            IntPtr lParam = new IntPtr(appCmd << 16);
            NativeWin32.PostMessage(hwnd, NativeWin32.WM_APPCOMMAND, hwnd, lParam);
            IntPtr child = MultiPlayerMediaController.FindRenderWidgetChild(hwnd);
            if (child != IntPtr.Zero)
                NativeWin32.PostMessage(child, NativeWin32.WM_APPCOMMAND, child, lParam);
        }

        public static void LaunchPlayerAndAutoPlayAsync(DiscoveredMusicPlayer player, IslandConfigData cfg, CoreAudioBridge audio, string command, Action onReadyWake)
        {
            if (player == null || string.IsNullOrEmpty(player.LaunchTarget)) return;
            if (cfg != null)
            {
                cfg.last_used_music_player_id = player.Id;
                cfg.last_used_music_player_name = player.DisplayName;
                if (!player.IsWebFallback && File.Exists(player.LaunchTarget) && player.LaunchTarget.EndsWith(".exe", StringComparison.OrdinalIgnoreCase))
                    cfg.last_used_music_player_path = player.LaunchTarget;
                ConfigManager.Save(cfg);
            }

            ThreadPool.QueueUserWorkItem(_ =>
            {
                try
                {
                    var psi = new ProcessStartInfo
                    {
                        FileName = player.LaunchTarget,
                        Arguments = player.LaunchArgs ?? "",
                        UseShellExecute = true
                    };
                    Process.Start(psi);
                }
                catch { return; }

                int startTick = Environment.TickCount;
                int windowFirstSeenTick = 0;
                bool appCmdSentOnce = false;
                bool appCmdToggleSent = false;

                while (unchecked(Environment.TickCount - startTick) < 13000)
                {
                    Thread.Sleep(420);
                    try
                    {
                        MediaSessionSnapshot activeSnap;
                        int activeIdx;
                        var sessions = MultiPlayerMediaController.QueryAllPlayers(true, out activeSnap, out activeIdx);
                        MediaSessionSnapshot matchedSnap = null;
                        foreach (var s in sessions)
                        {
                            string comb = (s.AppId + " " + s.DisplayAppName + " " + s.Title).ToLowerInvariant();
                            foreach (string kw in player.MatchKeywords)
                                if (!string.IsNullOrEmpty(kw) && comb.Contains(kw.ToLowerInvariant()))
                                { matchedSnap = s; break; }
                            if (matchedSnap != null) break;
                        }
                        if (matchedSnap == null && sessions.Count == 1)
                            matchedSnap = sessions[0];

                        if (matchedSnap != null && matchedSnap.NativeSession != null)
                        {
                            MultiPlayerMediaController.SelectPlayerByAppId(matchedSnap.AppId);
                            var nativeSes = (GlobalSystemMediaTransportControlsSession)matchedSnap.NativeSession;
                            if (!matchedSnap.IsPlaying)
                            {
                                try { var op = nativeSes.TryPlayAsync(); Thread.Sleep(350); } catch { }
                                var pbAfter = nativeSes.GetPlaybackInfo();
                                if (pbAfter == null || pbAfter.PlaybackStatus != GlobalSystemMediaTransportControlsSessionPlaybackStatus.Playing)
                                {
                                    try { var op2 = nativeSes.TryTogglePlayPauseAsync(); Thread.Sleep(250); } catch { }
                                }
                            }
                            if (command == "next")
                            {
                                try { var opn = nativeSes.TrySkipNextAsync(); } catch { }
                            }
                            if (onReadyWake != null) onReadyWake();
                            return;
                        }

                        // Cold-start Electron/Desktop fallback (before SMTC session registers)
                        if (!player.IsWebFallback)
                        {
                            if (audio != null && audio.IsAnyAudioSessionActiveForKeywords(player.MatchKeywords))
                            {
                                if (onReadyWake != null) onReadyWake();
                                return;
                            }

                            IntPtr pHwnd = FindPlayerMainWindowByKeywords(player.MatchKeywords);
                            if (pHwnd != IntPtr.Zero)
                            {
                                if (windowFirstSeenTick == 0) windowFirstSeenTick = Environment.TickCount;
                                int hydratedMs = unchecked(Environment.TickCount - windowFirstSeenTick);
                                if (!appCmdSentOnce && hydratedMs >= 2100)
                                {
                                    appCmdSentOnce = true;
                                    SendAppCommandPlayToWindow(pHwnd, "play");
                                }
                                else if (appCmdSentOnce && !appCmdToggleSent && hydratedMs >= 3400)
                                {
                                    appCmdToggleSent = true;
                                    IntPtr lp = new IntPtr(NativeWin32.APPCOMMAND_MEDIA_PLAY_PAUSE << 16);
                                    NativeWin32.PostMessage(pHwnd, NativeWin32.WM_APPCOMMAND, pHwnd, lp);
                                    if (sessions.Count == 0)
                                        NativeWin32.SendMediaKey(NativeWin32.VK_MEDIA_PLAY_PAUSE);
                                }
                            }
                        }
                    }
                    catch { }
                }
                if (onReadyWake != null) onReadyWake();
            });
        }
    }

    internal class CUIAutomation8ComObject { }

    [ComImport, Guid("352FFBA8-0973-437C-A61F-F64CAFD81DF9"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    internal interface IUIAutomationCondition { }

    [ComImport, Guid("14314595-B4BC-4055-95F2-58F2E42C9855"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    internal interface IUIAutomationElementArray
    {
        [PreserveSig] int get_Length(out int length);
        [PreserveSig] int GetElement(int index, out IUIAutomationElement element);
    }

    [ComImport, Guid("94CF8058-9B8D-4AB9-8BFD-4CD0A33C8C70"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    internal interface IUIAutomationTogglePattern
    {
        [PreserveSig] int Toggle();
        [PreserveSig] int get_CurrentToggleState(out int retVal); // 0=Off, 1=On, 2=Indeterminate
        [PreserveSig] int get_CachedToggleState(out int retVal);
    }

    [ComImport, Guid("FB377FBE-8EA6-46D5-9C73-6499642D3059"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    internal interface IUIAutomationInvokePattern
    {
        [PreserveSig] int Invoke();
    }

    [ComImport, Guid("D22108AA-8AC5-49A5-837B-37BBB3D7591E"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    internal interface IUIAutomationElement
    {
        void _Slot3_SetFocus();
        void _Slot4_GetRuntimeId();
        [PreserveSig] int FindFirst(int scope, IUIAutomationCondition condition, out IUIAutomationElement found);
        [PreserveSig] int FindAll(int scope, IUIAutomationCondition condition, out IUIAutomationElementArray found);
        void _Slot7_FindFirstBuildCache();
        void _Slot8_FindAllBuildCache();
        void _Slot9_BuildUpdatedCache();
        void _Slot10_GetCurrentPropertyValue();
        void _Slot11_GetCurrentPropertyValueEx();
        void _Slot12_GetCachedPropertyValue();
        void _Slot13_GetCachedPropertyValueEx();
        void _Slot14_GetCurrentPatternAs();
        void _Slot15_GetCachedPatternAs();
        [PreserveSig] int GetCurrentPattern(int patternId, [MarshalAs(UnmanagedType.IUnknown)] out object patternObject);
        void _Slot17_GetCachedPattern();
        void _Slot18_GetCachedParent();
        void _Slot19_GetCachedChildren();
        [PreserveSig] int get_CurrentProcessId(out int retVal);
        [PreserveSig] int get_CurrentControlType(out int retVal);
        void _Slot22_LocalizedControlType();
        [PreserveSig] int get_CurrentName([MarshalAs(UnmanagedType.BStr)] out string retVal);
        void _Slot24_AcceleratorKey();
        void _Slot25_AccessKey();
        void _Slot26_HasKeyboardFocus();
        void _Slot27_IsKeyboardFocusable();
        [PreserveSig] int get_CurrentIsEnabled(out int retVal);
        [PreserveSig] int get_CurrentAutomationId([MarshalAs(UnmanagedType.BStr)] out string retVal);
        [PreserveSig] int get_CurrentClassName([MarshalAs(UnmanagedType.BStr)] out string retVal);
        [PreserveSig] int get_CurrentHelpText([MarshalAs(UnmanagedType.BStr)] out string retVal);
        void _Slot32_Culture();
        void _Slot33_IsControlElement();
        void _Slot34_IsContentElement();
        void _Slot35_IsPassword();
        void _Slot36_NativeWindowHandle();
        void _Slot37_ItemType();
        [PreserveSig] int get_CurrentIsOffscreen(out int retVal);
        void _Slot39_Orientation();
        void _Slot40_FrameworkId();
        void _Slot41_IsRequiredForForm();
        void _Slot42_ItemStatus();
        [PreserveSig] int get_CurrentBoundingRectangle(out NativeWin32.RECT retVal);
        void _Slot44_LabeledBy();
        [PreserveSig] int get_CurrentAriaRole([MarshalAs(UnmanagedType.BStr)] out string retVal);
        [PreserveSig] int get_CurrentAriaProperties([MarshalAs(UnmanagedType.BStr)] out string retVal);
    }

    [ComImport, Guid("34723AFF-0C9D-49D0-9896-7AB52DF8CD8A"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    internal interface IUIAutomation2
    {
        void _Slot3_CompareElements();
        void _Slot4_CompareRuntimeIds();
        void _Slot5_GetRootElement();
        [PreserveSig] int ElementFromHandle(IntPtr hwnd, out IUIAutomationElement element);
        [PreserveSig] int ElementFromPoint(NativeWin32.POINT pt, out IUIAutomationElement element);
        void _Slot8_GetFocusedElement();
        void _Slot9_GetRootElementBuildCache();
        void _Slot10_ElementFromHandleBuildCache();
        void _Slot11_ElementFromPointBuildCache();
        void _Slot12_GetFocusedElementBuildCache();
        void _Slot13_CreateTreeWalker();
        void _Slot14_ControlViewWalker();
        void _Slot15_ContentViewWalker();
        void _Slot16_RawViewWalker();
        void _Slot17_RawViewCondition();
        void _Slot18_ControlViewCondition();
        void _Slot19_ContentViewCondition();
        void _Slot20_CreateCacheRequest();
        [PreserveSig] int CreateTrueCondition(out IUIAutomationCondition newCondition);
        void _Slot22_CreateFalseCondition();
        [PreserveSig] int CreatePropertyCondition(int propertyId, [In, MarshalAs(UnmanagedType.Struct)] object value, out IUIAutomationCondition newCondition);
        void _Slot24_CreatePropertyConditionEx();
        [PreserveSig] int CreateAndCondition(IUIAutomationCondition condition1, IUIAutomationCondition condition2, out IUIAutomationCondition newCondition);
        void _Slot26_CreateAndConditionFromArray();
        void _Slot27_CreateAndConditionFromNativeArray();
        [PreserveSig] int CreateOrCondition(IUIAutomationCondition condition1, IUIAutomationCondition condition2, out IUIAutomationCondition newCondition);
        void _Slot29_CreateOrConditionFromArray();
        void _Slot30_CreateOrConditionFromNativeArray();
        void _Slot31_CreateNotCondition();
        void _Slot32_AddAutomationEventHandler();
        void _Slot33_RemoveAutomationEventHandler();
        void _Slot34_AddPropChangedNative();
        void _Slot35_AddPropChanged();
        void _Slot36_RemovePropChanged();
        void _Slot37_AddStructChanged();
        void _Slot38_RemoveStructChanged();
        void _Slot39_AddFocusChanged();
        void _Slot40_RemoveFocusChanged();
        void _Slot41_RemoveAllEvents();
        void _Slot42_IntNativeToSafe();
        void _Slot43_IntSafeToNative();
        void _Slot44_RectToVariant();
        void _Slot45_VariantToRect();
        void _Slot46_SafeToRectNative();
        void _Slot47_CreateProxyEntry();
        void _Slot48_ProxyFactoryMapping();
        void _Slot49_GetPropProgName();
        void _Slot50_GetPatProgName();
        void _Slot51_PollPatterns();
        void _Slot52_PollProps();
        void _Slot53_CheckNotSupported();
        void _Slot54_ReservedNotSupported();
        void _Slot55_ReservedMixed();
        void _Slot56_ElemFromIAcc();
        void _Slot57_ElemFromIAccCache();
        [PreserveSig] int get_AutoSetFocus(out int autoSetFocus);
        [PreserveSig] int put_AutoSetFocus(int autoSetFocus);
        [PreserveSig] int get_ConnectionTimeout(out uint timeout);
        [PreserveSig] int put_ConnectionTimeout(uint timeout);
        [PreserveSig] int get_TransactionTimeout(out uint timeout);
        [PreserveSig] int put_TransactionTimeout(uint timeout);
    }

    public enum PlayerFavoriteSupport { Unsupported = 0, SupportedUiaAndBgKey = 1, SupportedUiaOnly = 2 }

    public sealed class PlayerFavoriteSyncResult
    {
        public string TrackKey = "";
        public int Generation = 0;
        public bool SyncedFromPlayer = false;
        public bool IsLiked = false;
        public bool PlayerSupported = true;
        public string StatusMessage = "";
    }

    public static class PlayerFavoriteEngine
    {
        private const int UIA_TreeScope_Descendants = 4;
        private const int UIA_ControlTypePropertyId = 30003;
        private const int UIA_ButtonControlTypeId = 50000;
        private const int UIA_CheckBoxControlTypeId = 50002;
        private const int UIA_TogglePatternId = 10015;
        private const int UIA_InvokePatternId = 10000;

        private static readonly object _queueLock = new object();
        private static readonly AutoResetEvent _wakeEvt = new AutoResetEvent(false);
        private static Thread _staThread = null;

        private sealed class FavWorkItem
        {
            public bool IsToggleAction;
            public int Generation;
            public MediaSessionSnapshot Snapshot;
            public string TrackKey;
            public bool ExpectedTargetLiked;
            public Action<PlayerFavoriteSyncResult> Callback;
        }

        private sealed class CandidateMeta
        {
            public object ComNode;
            public bool IsAccNode;
            public string NameLower;
            public string HelpLower;
            public string AriaProps;
            public int ControlType;
            public bool HasToggle;
            public int ToggleVal;
            public int Enabled;
            public NativeWin32.RECT Bounds;
            public bool HasBounds;
        }

        [DllImport("oleacc.dll")]
        private static extern int AccessibleChildren(
            [MarshalAs(UnmanagedType.Interface)] object paccContainer,
            int iChildStart,
            int cChildren,
            [Out, MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 2)] object[] rgvarChildren,
            out int pcObtained);

        private static FavWorkItem _pendingToggle = null;
        private static FavWorkItem _pendingSync = null;
        private static int _lastManualToggleTick = 0;
        private static string _lastLoggedPassiveSync = "";

        private static void LogDiagnostic(string level, string msg)
        {
            try
            {
                string dir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "DynamicIslandWin");
                if (!Directory.Exists(dir)) Directory.CreateDirectory(dir);
                string logPath = Path.Combine(dir, "player_sync.log");
                FileInfo fi = new FileInfo(logPath);
                if (fi.Exists && fi.Length > 512 * 1024)
                {
                    string old = logPath + ".old";
                    if (File.Exists(old)) File.Delete(old);
                    File.Move(logPath, old);
                }
                File.AppendAllText(logPath, string.Format("[{0:yyyy-MM-dd HH:mm:ss.fff}] [{1}] {2}{3}", DateTime.Now, level, msg, Environment.NewLine), Encoding.UTF8);
            }
            catch { }
        }

        private static void EnsureStaWorkerStarted()
        {
            if (_staThread != null) return;
            lock (_queueLock)
            {
                if (_staThread != null) return;
                _staThread = new Thread(StaWorkerLoop) { IsBackground = true, Name = "DynamicIslandPlayerFavSTA" };
                _staThread.SetApartmentState(ApartmentState.STA);
                _staThread.Start();
            }
        }

        public static string BuildTrackKey(MediaSessionSnapshot snap)
        {
            if (snap == null) return "";
            string t = (snap.Title ?? "").Trim();
            string a = (snap.Artist ?? "").Trim();
            if (string.IsNullOrEmpty(t) || t == "Не воспроизводится") return "";
            return (t + " — " + a).ToLowerInvariant();
        }

        public static PlayerFavoriteSupport ClassifyPlayerSupport(MediaSessionSnapshot snap, out bool isSpotify, out bool isYandex, out bool isBrowser)
        {
            isSpotify = false; isYandex = false; isBrowser = false;
            if (snap == null) return PlayerFavoriteSupport.Unsupported;
            string combined = ((snap.AppId ?? "") + " " + (snap.DisplayAppName ?? "")).ToLowerInvariant();
            isSpotify = combined.Contains("spotify");
            isYandex = combined.Contains("яндекс") || combined.Contains("yandex");
            isBrowser = combined.Contains("chrome") || combined.Contains("msedge") || combined.Contains("opera") ||
                        combined.Contains("brave") || combined.Contains("firefox") || combined.Contains("vivaldi");
            if (isYandex) return PlayerFavoriteSupport.SupportedUiaAndBgKey;
            if (isSpotify || combined.Contains("aimp") || combined.Contains("itunes") || combined.Contains("applemusic") ||
                combined.Contains("deezer") || combined.Contains("tidal") || combined.Contains("musicbee") || combined.Contains("foobar") || isBrowser)
                return PlayerFavoriteSupport.SupportedUiaOnly;
            return PlayerFavoriteSupport.Unsupported;
        }

        public static void RequestSyncStateAsync(MediaSessionSnapshot snap, int generation, Action<PlayerFavoriteSyncResult> onCompleted, bool forceImmediate = false)
        {
            if (snap == null || string.IsNullOrEmpty(BuildTrackKey(snap))) return;
            if (!forceImmediate && _lastManualToggleTick != 0 && unchecked(Environment.TickCount - _lastManualToggleTick) < 2200) return;
            EnsureStaWorkerStarted();
            lock (_queueLock)
            {
                if (_pendingToggle != null) return;
                _pendingSync = new FavWorkItem { IsToggleAction = false, Generation = generation, Snapshot = snap, TrackKey = BuildTrackKey(snap), Callback = onCompleted };
            }
            _wakeEvt.Set();
        }

        public static void RequestToggleFavoriteAsync(MediaSessionSnapshot snap, bool targetLiked, int generation, Action<PlayerFavoriteSyncResult> onCompleted)
        {
            if (snap == null) return;
            _lastManualToggleTick = Environment.TickCount;
            EnsureStaWorkerStarted();
            lock (_queueLock)
            {
                _pendingToggle = new FavWorkItem { IsToggleAction = true, Generation = generation, Snapshot = snap, TrackKey = BuildTrackKey(snap), ExpectedTargetLiked = targetLiked, Callback = onCompleted };
                _pendingSync = null;
            }
            _wakeEvt.Set();
        }

        private static void ReleaseCom(object comObj)
        {
            if (comObj != null && Marshal.IsComObject(comObj)) { try { Marshal.ReleaseComObject(comObj); } catch { } }
        }

        private static bool IsWindowResponsive(IntPtr hwnd)
        {
            if (hwnd == IntPtr.Zero || !NativeWin32.IsWindow(hwnd)) return false;
            IntPtr res;
            return NativeWin32.SendMessageTimeout(hwnd, 0x0000, IntPtr.Zero, IntPtr.Zero, 0x0002, 160, out res) != IntPtr.Zero;
        }

        private static string Norm(string s)
        {
            return string.IsNullOrEmpty(s) ? "" : s.Trim().ToLowerInvariant().Replace('\u2019', '\'').Replace('ё', 'е');
        }

        private static bool IsStrongAnchor(string n)
        {
            if (string.IsNullOrEmpty(n)) return false;
            return n.Contains("следующ") || n.Contains("предыдущ") || n.Contains("next song") || n.Contains("previous song") ||
                   n.Contains("next track") || n.Contains("previous track") || n == "next" || n == "previous" ||
                   n.Contains("не нравится") || n.Contains("don't like") || n.Contains("dont like") ||
                   n.Contains("ұнамайды") || n.Contains("yoqmaydi") || n.Contains("келесі ән") || n.Contains("алдыңғы ән") ||
                   n.Contains("keyingi tarona") || n.Contains("avvalgi tarona");
        }

        private static bool IsPlayAnchor(string n)
        {
            return n == "пауза" || n == "воспроизведение" || n == "pause" || n == "play" || n == "playback";
        }
        private static bool EvaluateSemantics(CandidateMeta m, bool nearStrong, out bool resolvedLiked)
        {
            resolvedLiked = false;
            string nm = m.NameLower;
            string n = string.IsNullOrEmpty(m.HelpLower) ? nm : (nm + " | " + m.HelpLower);
            if (n.Length < 2) return false;

            if (n.Contains("не нравится") || n.Contains("не рекомендовать") || n.Contains("дизлайк") ||
                n.Contains("dislike") || n.Contains("thumbs down") || n.Contains("don't like") || n.Contains("dont like") ||
                n.Contains("do not like") || n.Contains("ұнамайды") || n.Contains("yoqmaydi") ||
                n.Contains("скрыть") || n.Contains("закрепить") || n.Contains("открепить") || n.Contains("pin") ||
                n.Contains("повтор") || n.Contains("случайн") || n.Contains("перемеш") || n.Contains("очеред") ||
                n.Contains("контекстное меню") || n.Contains("поделиться") || n.Contains("share"))
                return false;

            bool explicitRemove =
                n.Contains("удалить из") || n.Contains("убрать из") || n.Contains("убрать отметку") ||
                n.Contains("снять лайк") || n.Contains("убрать лайк") || n.Contains("remove from") || n.Contains("unlike");

            bool spotifyCheckedBox = nearStrong && m.ControlType == UIA_CheckBoxControlTypeId && m.HasToggle && m.ToggleVal == 1 &&
                                     (n.Contains("добавить в плейлист") || n.Contains("add to playlist"));

            bool isLikeCtrl =
                explicitRemove || spotifyCheckedBox ||
                nm == "нравится" || nm == "like" || nm == "ұнайды" || nm == "yoqadi" ||
                n.Contains("нравится") || n.Contains("лайк") || n.Contains("любить") || n.Contains("любимое") || n.Contains("любимые") ||
                n.Contains("в коллекцию") || n.Contains("в мою коллекцию") || n.Contains("добавить в коллекцию") ||
                n.Contains("в медиатеку") || n.Contains("добавить в медиатеку") || n.Contains("в избранное") ||
                n == "like" || n.StartsWith("like |") || n.EndsWith("| like") ||
                n.Contains("save to your library") || n.Contains("add to liked") || n.Contains("add to favorites") || n.Contains("favorite");

            if (!isLikeCtrl) return false;

            if (m.HasToggle && (m.ToggleVal == 0 || m.ToggleVal == 1))
            {
                resolvedLiked = (m.ToggleVal == 1) ? true : explicitRemove;
                return true;
            }
            if (!string.IsNullOrEmpty(m.AriaProps))
            {
                string ap = m.AriaProps.ToLowerInvariant();
                if (ap.Contains("pressed=true") || ap.Contains("checked=true")) { resolvedLiked = true; return true; }
                if (ap.Contains("pressed=false") || ap.Contains("checked=false")) { resolvedLiked = explicitRemove; return true; }
            }
            resolvedLiked = explicitRemove;
            return true;
        }

        private static CandidateMeta SelectBestCandidate(List<CandidateMeta> items, List<int> strongAnchors, List<int> playAnchors, NativeWin32.RECT topWinRc, bool topIconic, out bool resolvedLiked, out string dbg)
        {
            resolvedLiked = false; dbg = "";
            if (items == null || items.Count == 0) return null;

            int winW = topWinRc.Right - topWinRc.Left, winH = topWinRc.Bottom - topWinRc.Top;
            bool validWin = !topIconic && winW >= 200 && winH >= 120;
            int bestIdx = -1, bestScore = int.MinValue;
            bool bestLiked = false;

            for (int k = 0; k < items.Count; k++)
            {
                var m = items[k];
                if (m.Enabled == 0) continue;

                int minStrong = int.MaxValue, minPlay = int.MaxValue;
                for (int s = 0; s < strongAnchors.Count; s++) { int d = Math.Abs(k - strongAnchors[s]); if (d < minStrong) minStrong = d; }
                for (int p = 0; p < playAnchors.Count; p++) { int d = Math.Abs(k - playAnchors[p]); if (d < minPlay) minPlay = d; }

                bool nearStrong = minStrong <= 14;
                bool nearPlay = minPlay <= 10;
                bool candLiked;
                if (!EvaluateSemantics(m, nearStrong, out candLiked)) continue;

                int score = 0;
                if (nearStrong) score += 3600 - minStrong * 60;
                else if (nearPlay) score += 1200 - minPlay * 40;

                int bw = m.Bounds.Right - m.Bounds.Left, bh = m.Bounds.Bottom - m.Bounds.Top;
                bool validBtn = m.HasBounds && bw >= 8 && bw <= 180 && bh >= 8 && bh <= 110;

                if (validWin && validBtn)
                {
                    float cy = (m.Bounds.Top + m.Bounds.Bottom) * 0.5f;
                    float relY = (cy - topWinRc.Top) / Math.Max(1f, (float)winH);
                    if (cy >= topWinRc.Bottom - Math.Max(148, (int)(winH * 0.25f)) && cy <= topWinRc.Bottom + 16)
                        score += 1400 + (int)(relY * 200f);
                    else if (relY >= 0.70f) score += 750 + (int)(relY * 100f);
                    else if (relY <= 0.15f) score += 480;
                    else if (!nearStrong) continue;
                }
                else
                {
                    if (!nearStrong && !nearPlay && k < items.Count - 24) continue;
                    if (k >= items.Count - 24) score += 400 + (k - (items.Count - 24)) * 10;
                }

                if (m.HasToggle) score += 120;
                if (score >= bestScore) { bestScore = score; bestIdx = k; bestLiked = candLiked; }
            }

            CandidateMeta winner = null;
            for (int k = 0; k < items.Count; k++)
            {
                if (k == bestIdx)
                {
                    winner = items[k];
                    dbg = string.Format("mode={0}, name='{1}', tog={2}:{3}, score={4}, btns={5}",
                        winner.IsAccNode ? "IA2" : "UIA", winner.NameLower, winner.HasToggle ? 1 : 0, winner.ToggleVal, bestScore, items.Count);
                }
                else { ReleaseCom(items[k].ComNode); items[k].ComNode = null; }
            }
            resolvedLiked = bestLiked;
            return winner;
        }

        private static void CollectAccButtonsRecursive(
            object acc, int depth, ref int visitedNodes,
            List<CandidateMeta> items, List<int> strongAnchors, List<int> playAnchors)
        {
            if (acc == null || depth > 24 || visitedNodes > 1800) return;
            visitedNodes++;

            object vSelf = 0;
            int role = 0, state = 0, childCount = 0;
            try
            {
                if (depth <= 8)
                {
                    var sp = acc as IComServiceProvider;
                    if (sp != null)
                    {
                        Guid gAcc = new Guid("618736E0-3C3D-11CF-810C-00AA00389B71");
                        Guid gIA2 = new Guid("E89F726E-C4F4-4C19-BB19-B647D7FA8478");
                        IntPtr p2;
                        if (sp.QueryService(ref gAcc, ref gIA2, out p2) == 0 && p2 != IntPtr.Zero)
                            Marshal.Release(p2);
                    }
                }

                Type t = acc.GetType();
                object rObj = t.InvokeMember("accRole", System.Reflection.BindingFlags.GetProperty, null, acc, new object[] { vSelf });
                if (rObj is int) role = (int)rObj;
                object sObj = t.InvokeMember("accState", System.Reflection.BindingFlags.GetProperty, null, acc, new object[] { vSelf });
                if (sObj is int) state = (int)sObj;
                object cObj = t.InvokeMember("accChildCount", System.Reflection.BindingFlags.GetProperty, null, acc, null);
                if (cObj is int) childCount = (int)cObj;

                if (role == 43 || role == 44 || role == 62)
                {
                    string rawNm = t.InvokeMember("accName", System.Reflection.BindingFlags.GetProperty, null, acc, new object[] { vSelf }) as string;
                    string nmNorm = Norm(rawNm);
                    if (IsStrongAnchor(nmNorm)) strongAnchors.Add(items.Count);
                    else if (IsPlayAnchor(nmNorm)) playAnchors.Add(items.Count);

                    int enabled = ((state & 0x01) == 0) ? 1 : 0;
                    int pressed = (((state & 0x08) != 0) || ((state & 0x10) != 0)) ? 1 : 0;

                    items.Add(new CandidateMeta
                    {
                        ComNode = acc, IsAccNode = true, NameLower = nmNorm, HelpLower = "", AriaProps = null,
                        ControlType = (role == 44) ? UIA_CheckBoxControlTypeId : UIA_ButtonControlTypeId,
                        HasToggle = true, ToggleVal = pressed, Enabled = enabled, HasBounds = false
                    });
                    return;
                }
            }
            catch { return; }

            if (childCount <= 0 || childCount > 450) return;
            object[] kids = new object[childCount];
            int got;
            if (AccessibleChildren(acc, 0, childCount, kids, out got) == 0 && got > 0)
            {
                for (int i = 0; i < got; i++)
                {
                    object k = kids[i];
                    if (k != null && !(k is int))
                    {
                        int beforeCount = items.Count;
                        CollectAccButtonsRecursive(k, depth + 1, ref visitedNodes, items, strongAnchors, playAnchors);
                        bool retained = (items.Count > beforeCount && ReferenceEquals(items[items.Count - 1].ComNode, k));
                        if (!retained) ReleaseCom(k);
                    }
                }
            }
        }
        private static CandidateMeta ScanAccWindow(IntPtr hwnd, NativeWin32.RECT topWinRc, bool topIconic, out bool resolvedLiked, out int totalBtns, out string dbg)
        {
            resolvedLiked = false; totalBtns = 0; dbg = "";
            if (hwnd == IntPtr.Zero || !IsWindowResponsive(hwnd)) return null;

            Guid iidAcc = new Guid("618736E0-3C3D-11CF-810C-00AA00389B71");
            object accRoot = null;
            var items = new List<CandidateMeta>();
            var strongAnchors = new List<int>();
            var playAnchors = new List<int>();
            try
            {
                if (NativeWin32.AccessibleObjectFromWindow(hwnd, 0xFFFFFFFCu, ref iidAcc, out accRoot) != 0 || accRoot == null)
                    return null;

                int visited = 0;
                CollectAccButtonsRecursive(accRoot, 0, ref visited, items, strongAnchors, playAnchors);
                bool rootRetained = (items.Count > 0 && ReferenceEquals(items[0].ComNode, accRoot));
                if (!rootRetained) { ReleaseCom(accRoot); accRoot = null; }

                totalBtns = items.Count;
                return SelectBestCandidate(items, strongAnchors, playAnchors, topWinRc, topIconic, out resolvedLiked, out dbg);
            }
            catch (Exception ex)
            {
                for (int i = 0; i < items.Count; i++) { ReleaseCom(items[i].ComNode); items[i].ComNode = null; }
                ReleaseCom(accRoot);
                LogDiagnostic("WARN", "IA2 scan skipped safely: " + ex.Message);
                return null;
            }
        }

        private static CandidateMeta ScanUiaWindow(
            IUIAutomation2 uia, IntPtr hwnd, NativeWin32.RECT topWinRc, bool topIconic,
            IUIAutomationCondition condInteractive, out bool resolvedLiked, out int totalBtns, out string dbg)
        {
            resolvedLiked = false; totalBtns = 0; dbg = "";
            if (uia == null || hwnd == IntPtr.Zero || !IsWindowResponsive(hwnd)) return null;

            IUIAutomationElement root = null;
            IUIAutomationElementArray arr = null;
            var items = new List<CandidateMeta>();
            var strongAnchors = new List<int>();
            var playAnchors = new List<int>();
            try
            {
                if (uia.ElementFromHandle(hwnd, out root) != 0 || root == null) return null;
                if (root.FindAll(UIA_TreeScope_Descendants, condInteractive, out arr) != 0 || arr == null) return null;
                int len;
                if (arr.get_Length(out len) != 0 || len <= 0) return null;
                totalBtns = len;

                int limit = Math.Min(len, 500);
                for (int i = Math.Max(0, len - limit); i < len; i++)
                {
                    IUIAutomationElement el = null;
                    if (arr.GetElement(i, out el) != 0 || el == null) continue;
                    string rawNm = null, rawHelp = null, rawAria = null;
                    int en = 1, ct = UIA_ButtonControlTypeId;
                    el.get_CurrentIsEnabled(out en);
                    el.get_CurrentControlType(out ct);
                    el.get_CurrentName(out rawNm);
                    el.get_CurrentHelpText(out rawHelp);
                    el.get_CurrentAriaProperties(out rawAria);

                    string nmNorm = Norm(rawNm), helpNorm = Norm(rawHelp);
                    if (IsStrongAnchor(nmNorm) || IsStrongAnchor(helpNorm)) strongAnchors.Add(items.Count);
                    else if (IsPlayAnchor(nmNorm) || IsPlayAnchor(helpNorm)) playAnchors.Add(items.Count);

                    bool hasTog = false; int togVal = -1; object patObj = null;
                    if (el.GetCurrentPattern(UIA_TogglePatternId, out patObj) == 0 && patObj != null)
                    {
                        var tp = patObj as IUIAutomationTogglePattern;
                        if (tp != null && tp.get_CurrentToggleState(out togVal) == 0) hasTog = true;
                        ReleaseCom(patObj);
                    }
                    NativeWin32.RECT brc;
                    bool hasBounds = (el.get_CurrentBoundingRectangle(out brc) == 0);
                    items.Add(new CandidateMeta { ComNode = el, IsAccNode = false, NameLower = nmNorm, HelpLower = helpNorm, AriaProps = rawAria, ControlType = ct, HasToggle = hasTog, ToggleVal = togVal, Enabled = en, Bounds = brc, HasBounds = hasBounds });
                }

                return SelectBestCandidate(items, strongAnchors, playAnchors, topWinRc, topIconic, out resolvedLiked, out dbg);
            }
            catch
            {
                for (int i = 0; i < items.Count; i++) { ReleaseCom(items[i].ComNode); items[i].ComNode = null; }
                return null;
            }
            finally { ReleaseCom(arr); ReleaseCom(root); }
        }

        private static CandidateMeta FindPlayerBarLikeControl(
            IUIAutomation2 uia, IntPtr topHwnd, IntPtr renderHwnd, bool isBrowser, string windowTitle,
            bool allowColdWakeRetry, out bool isCurrentlyLiked, out string debugSummary)
        {
            isCurrentlyLiked = false; debugSummary = "";
            if (topHwnd == IntPtr.Zero) return null;

            if (isBrowser)
            {
                string tl = (windowTitle ?? "").ToLowerInvariant();
                if (!(tl.Contains("музык") || tl.Contains("music") || tl.Contains("spotify") || tl.Contains("яндекс") || tl.Contains("deezer") || tl.Contains("soundcloud") || tl.Contains("vk")))
                    return null;
            }

            NativeWin32.RECT winRc;
            if (!NativeWin32.GetWindowRect(topHwnd, out winRc)) winRc = new NativeWin32.RECT();
            bool isIconic = NativeWin32.IsIconic(topHwnd);

            int maxAttempts = allowColdWakeRetry ? 3 : 1;
            for (int attempt = 0; attempt < maxAttempts; attempt++)
            {
                int accBtns = 0;
                if (renderHwnd != IntPtr.Zero)
                {
                    var hitAccR = ScanAccWindow(renderHwnd, winRc, isIconic, out isCurrentlyLiked, out accBtns, out debugSummary);
                    if (hitAccR != null) return hitAccR;
                }
                var hitAccTop = ScanAccWindow(topHwnd, winRc, isIconic, out isCurrentlyLiked, out accBtns, out debugSummary);
                if (hitAccTop != null) return hitAccTop;

                int uiaBtns = 0;
                if (uia != null)
                {
                    IUIAutomationCondition cBtn = null, cChk = null, cInter = null;
                    try
                    {
                        uia.CreatePropertyCondition(UIA_ControlTypePropertyId, UIA_ButtonControlTypeId, out cBtn);
                        uia.CreatePropertyCondition(UIA_ControlTypePropertyId, UIA_CheckBoxControlTypeId, out cChk);
                        uia.CreateOrCondition(cBtn, cChk, out cInter);
                        if (cInter != null)
                        {
                            var hitUia = ScanUiaWindow(uia, topHwnd, winRc, isIconic, cInter, out isCurrentlyLiked, out uiaBtns, out debugSummary);
                            if (hitUia != null) return hitUia;
                        }
                    }
                    finally { ReleaseCom(cInter); ReleaseCom(cChk); ReleaseCom(cBtn); }
                }

                if (attempt + 1 < maxAttempts && Math.Max(accBtns, uiaBtns) <= 5)
                {
                    Thread.Sleep(220);
                    continue;
                }
                break;
            }
            return null;
        }
        private static bool InvokeLikeControlSafe(CandidateMeta ctrl)
        {
            if (ctrl == null || ctrl.ComNode == null) return false;
            if (ctrl.IsAccNode)
            {
                try
                {
                    ctrl.ComNode.GetType().InvokeMember(
                        "accDoDefaultAction",
                        System.Reflection.BindingFlags.InvokeMethod,
                        null,
                        ctrl.ComNode,
                        new object[] { 0 });
                    return true;
                }
                catch (Exception ex)
                {
                    LogDiagnostic("WARN", "IA2 accDoDefaultAction failed safely: " + ex.Message);
                    return false;
                }
            }

            var btn = ctrl.ComNode as IUIAutomationElement;
            if (btn == null) return false;
            object patObj = null;
            try
            {
                if (btn.GetCurrentPattern(UIA_TogglePatternId, out patObj) == 0 && patObj is IUIAutomationTogglePattern)
                {
                    int hr = ((IUIAutomationTogglePattern)patObj).Toggle();
                    ReleaseCom(patObj); patObj = null;
                    if (hr == 0) return true;
                }
                ReleaseCom(patObj); patObj = null;
                if (btn.GetCurrentPattern(UIA_InvokePatternId, out patObj) == 0 && patObj is IUIAutomationInvokePattern)
                {
                    int hr = ((IUIAutomationInvokePattern)patObj).Invoke();
                    ReleaseCom(patObj); patObj = null;
                    if (hr == 0) return true;
                }
            }
            catch (Exception ex) { LogDiagnostic("WARN", "UIA invoke failed safely: " + ex.Message); }
            finally { ReleaseCom(patObj); }
            return false;
        }

        private static bool SendSilentBackgroundKeyYandex(IntPtr topHwnd, IntPtr renderHwnd)
        {
            IntPtr target = (renderHwnd != IntPtr.Zero && NativeWin32.IsWindow(renderHwnd)) ? renderHwnd : topHwnd;
            if (target == IntPtr.Zero || !NativeWin32.IsWindow(target)) return false;
            try
            {
                uint scanF = 0x21;
                IntPtr lDown = new IntPtr((int)((scanF << 16) | 1));
                IntPtr lUp = new IntPtr(unchecked((int)(0xC0000001u | (scanF << 16))));
                if (topHwnd != IntPtr.Zero && NativeWin32.GetForegroundWindow() != topHwnd)
                {
                    NativeWin32.PostMessage(topHwnd, NativeWin32.WM_ACTIVATE, new IntPtr(1), IntPtr.Zero);
                    Thread.Sleep(12);
                }
                NativeWin32.PostMessage(target, NativeWin32.WM_KEYDOWN, new IntPtr(0x46), lDown);
                Thread.Sleep(20);
                NativeWin32.PostMessage(target, NativeWin32.WM_KEYUP, new IntPtr(0x46), lUp);
                return true;
            }
            catch (Exception ex) { LogDiagnostic("WARN", "Silent background key failed: " + ex.Message); return false; }
        }

        public static string RunDiagnosticProbe()
        {
            var sb = new StringBuilder();
            try
            {
                IntPtr fg0 = NativeWin32.GetForegroundWindow();
                var uia = (IUIAutomation2)(new CUIAutomation8ComObject());
                uia.put_AutoSetFocus(0);
                var dummyYm = new MediaSessionSnapshot { AppId = "YandexMusic", DisplayAppName = "Яндекс Музыка", Title = "Probe", Artist = "Probe" };
                string wt; IntPtr rw;
                IntPtr top = MultiPlayerMediaController.FindPlayerWindowSafe(dummyYm, false, true, out wt, out rw);
                if (top != IntPtr.Zero)
                {
                    bool liked; string dbg;
                    var ctrl = FindPlayerBarLikeControl(uia, top, rw, false, wt, true, out liked, out dbg);
                    sb.AppendLine(string.Format("Before: found={0} isLiked={1} [{2}]", ctrl != null, liked, dbg));
                    if (ctrl != null) ReleaseCom(ctrl.ComNode);
                }
                sb.AppendLine("FG_UNCHANGED=" + (fg0 == NativeWin32.GetForegroundWindow()));
                ReleaseCom(uia);
            }
            catch (Exception ex) { sb.AppendLine("ERR: " + ex.Message); }
            return sb.ToString();
        }

        private static void StaWorkerLoop()
        {
            IUIAutomation2 uia = null;
            try
            {
                uia = (IUIAutomation2)(new CUIAutomation8ComObject());
                uia.put_AutoSetFocus(0);
                uia.put_ConnectionTimeout(750);
                uia.put_TransactionTimeout(1100);
            }
            catch (Exception ex) { LogDiagnostic("ERROR", "CUIAutomation8 init failed: " + ex.Message); uia = null; }

            while (true)
            {
                _wakeEvt.WaitOne(2500);
                FavWorkItem job = null;
                lock (_queueLock)
                {
                    if (_pendingToggle != null) { job = _pendingToggle; _pendingToggle = null; }
                    else if (_pendingSync != null) { job = _pendingSync; _pendingSync = null; }
                }
                if (job == null) continue;
                try { ExecuteJobOnSta(uia, job); }
                catch (Exception ex) { LogDiagnostic("ERROR", "Unhandled STA job error: " + ex.Message); }
            }
        }

        private static void ExecuteJobOnSta(IUIAutomation2 uia, FavWorkItem job)
        {
            var res = new PlayerFavoriteSyncResult { TrackKey = job.TrackKey, Generation = job.Generation };
            bool isSpotify, isYandex, isBrowser;
            PlayerFavoriteSupport sup = ClassifyPlayerSupport(job.Snapshot, out isSpotify, out isYandex, out isBrowser);

            if (sup == PlayerFavoriteSupport.Unsupported)
            {
                res.PlayerSupported = false; res.SyncedFromPlayer = false; res.IsLiked = job.ExpectedTargetLiked;
                if (job.IsToggleAction) LogDiagnostic("INFO", "Unsupported player '" + (job.Snapshot.DisplayAppName ?? job.Snapshot.AppId) + "' — stored locally.");
                if (job.Callback != null) job.Callback(res);
                return;
            }

            string winTitle; IntPtr renderHwnd;
            IntPtr topHwnd = MultiPlayerMediaController.FindPlayerWindowSafe(job.Snapshot, isSpotify, isYandex, out winTitle, out renderHwnd);

            if (!job.IsToggleAction)
            {
                if (topHwnd == IntPtr.Zero) return;
                bool playerLiked; string dbg;
                var ctrl = FindPlayerBarLikeControl(uia, topHwnd, renderHwnd, isBrowser, winTitle, true, out playerLiked, out dbg);
                if (ctrl != null)
                {
                    ReleaseCom(ctrl.ComNode);
                    res.SyncedFromPlayer = true;
                    res.IsLiked = playerLiked;
                    string sigLog = job.TrackKey + "|" + playerLiked;
                    if (!string.Equals(_lastLoggedPassiveSync, sigLog, StringComparison.Ordinal))
                    {
                        _lastLoggedPassiveSync = sigLog;
                        LogDiagnostic("SYNC", "Synced player favorite state for '" + job.TrackKey + "' -> IsLiked=" + playerLiked + " [" + dbg + "]");
                    }
                    if (job.Callback != null) job.Callback(res);
                }
                return;
            }

            if (topHwnd == IntPtr.Zero)
            {
                res.PlayerSupported = false; res.SyncedFromPlayer = false; res.IsLiked = job.ExpectedTargetLiked;
                LogDiagnostic("INFO", "Player window unavailable for '" + job.Snapshot.DisplayAppName + "' — kept local state.");
                if (job.Callback != null) job.Callback(res);
                return;
            }

            bool curLikedBefore; string beforeDbg;
            var likeCtrl = FindPlayerBarLikeControl(uia, topHwnd, renderHwnd, isBrowser, winTitle, true, out curLikedBefore, out beforeDbg);
            if (likeCtrl != null)
            {
                bool alreadyMatched = (curLikedBefore == job.ExpectedTargetLiked);
                bool invoked = false;
                try { invoked = alreadyMatched ? true : InvokeLikeControlSafe(likeCtrl); }
                finally { ReleaseCom(likeCtrl.ComNode); }

                if (invoked)
                {
                    if (alreadyMatched)
                    {
                        res.SyncedFromPlayer = true;
                        res.IsLiked = curLikedBefore;
                        LogDiagnostic("INFO", "Aligned phase without redundant toggle for '" + job.TrackKey + "' -> IsLiked=" + res.IsLiked + " [" + beforeDbg + "]");
                        if (job.Callback != null) job.Callback(res);
                        return;
                    }

                    bool verifiedLiked = job.ExpectedTargetLiked;
                    bool settled = false;
                    int[] waits = new int[] { 180, 260, 360, 450 };
                    for (int w = 0; w < waits.Length; w++)
                    {
                        Thread.Sleep(waits[w]);
                        bool vLiked; string vDbg;
                        var checkCtrl = FindPlayerBarLikeControl(uia, topHwnd, renderHwnd, isBrowser, winTitle, false, out vLiked, out vDbg);
                        if (checkCtrl != null)
                        {
                            ReleaseCom(checkCtrl.ComNode);
                            verifiedLiked = vLiked;
                            if (vLiked == job.ExpectedTargetLiked) { settled = true; break; }
                        }
                    }

                    res.SyncedFromPlayer = true;
                    res.IsLiked = settled ? verifiedLiked : job.ExpectedTargetLiked;
                    _lastManualToggleTick = Environment.TickCount;
                    LogDiagnostic("INFO", "Toggled favorite silently in background for '" + job.TrackKey + "' (" + curLikedBefore + " -> " + res.IsLiked + ", settled=" + settled + ") [" + beforeDbg + "]");
                    if (job.Callback != null) job.Callback(res);
                    return;
                }
            }

            if (sup == PlayerFavoriteSupport.SupportedUiaAndBgKey && SendSilentBackgroundKeyYandex(topHwnd, renderHwnd))
            {
                Thread.Sleep(450);
                bool afterLiked; string afterDbg;
                var verifyCtrl = FindPlayerBarLikeControl(uia, topHwnd, renderHwnd, isBrowser, winTitle, false, out afterLiked, out afterDbg);
                if (verifyCtrl != null) { ReleaseCom(verifyCtrl.ComNode); res.SyncedFromPlayer = true; res.IsLiked = afterLiked; }
                else { res.SyncedFromPlayer = false; res.IsLiked = job.ExpectedTargetLiked; }
                LogDiagnostic("INFO", "Sent silent background KeyF for hidden '" + job.Snapshot.DisplayAppName + "' -> IsLiked=" + res.IsLiked);
                if (job.Callback != null) job.Callback(res);
                return;
            }

            res.PlayerSupported = false; res.SyncedFromPlayer = false; res.IsLiked = job.ExpectedTargetLiked;
            LogDiagnostic("INFO", "Like button unreachable in background for '" + job.Snapshot.DisplayAppName + "' — kept local state without window activation.");
            if (job.Callback != null) job.Callback(res);
        }
    }
    public static class SystemSensors
    {
        private const uint PROCESS_QUERY_LIMITED_INFORMATION = 0x1000;

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern IntPtr OpenProcess(uint dwDesiredAccess, bool bInheritHandle, uint dwProcessId);

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern bool CloseHandle(IntPtr hObject);

        [DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
        private static extern bool QueryFullProcessImageNameW(IntPtr hProcess, uint dwFlags, StringBuilder lpExeName, ref uint lpdwSize);

        private static readonly string[] TransientShellClasses = new string[]
        {
            "MultitaskingViewFrame", "XamlExplorerHostIslandWindow", "TaskSwitcherWnd", "TaskSwitcherOverlayWnd",
            "ForegroundStaging", "TaskListThumbnailWnd", "TaskListOverlayWnd", "Shell_InputSwitchTopLevelWindow"
        };

        private static readonly HashSet<string> NonGameProcessBlacklist = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            // Browsers
            "chrome", "msedge", "opera", "opera_gx", "firefox", "yandex", "browser", "brave", "vivaldi",
            "waterfox", "tor", "arc", "zen", "maxthon", "iexplore", "whale", "thorium", "librewolf", "floorp",
            // Messengers, Voice & Capture Apps
            "discord", "discordptb", "discordcanary", "telegram", "ayugram", "kotatogram", "unigram",
            "whatsapp", "viber", "skype", "slack", "teams", "ms-teams", "zoom", "obs64", "obs32", "streamlabs", "sharex",
            // Video & Audio Players
            "vlc", "mpv", "mpc-hc", "mpc-hc64", "mpc-be", "mpc-be64", "potplayer", "potplayermini", "potplayermini64",
            "kmplayer", "gom", "gom64", "wmplayer", "video.ui", "music.ui", "zunemusic", "microsoft.media.player",
            "yandexmusic", "яндекс музыка", "spotify", "aimp", "foobar2000", "itunes", "applemusic", "winamp", "musicbee",
            // Windows Shell, Terminals, IDEs, Editors, Office & Viewers
            "explorer", "searchhost", "searchapp", "shellexperiencehost", "startmenuexperiencehost", "lockapp",
            "applicationframehost", "systemsettings", "taskmgr", "mmc", "regedit", "cmd", "powershell", "pwsh", "windowsterminal",
            "conhost", "code", "cursor", "windsurf", "antigravity", "devenv", "rider64", "idea64", "pycharm64", "webstorm64",
            "clion64", "goland64", "sublime_text", "notepad", "notepad++", "winword", "excel", "powerpnt", "onenote", "outlook",
            "acrobat", "acrord32", "foxitreader", "sumatrapdf", "mspaint", "paintdotnet", "photoshop", "illustrator", "figma",
            "canva", "blender", "obs", "nvidia share", "nvcontainer", "nvidia overlay", "amdow", "radeonsoftware",
            // Game Launchers / Storefronts
            "steam", "steamwebhelper", "epicgameslauncher", "epicwebhelper", "riotclientux", "riotclientuxrender",
            "riotclientservices", "battle.net", "agent", "galaxyclient", "eadesktop", "eabackgroundservice",
            "ubisoftconnect", "upc", "playnite.desktopapp", "playnite.fullscreenapp",
            // Editors & Self
            "unrealeditor", "ue4editor", "unity", "unityhub", "godot", "dynamicislandwin", "dynamicislandwin_setup", "dinamicisland-setup"
        };

        private static readonly string[] GameInstallPathMarkers = new string[]
        {
            @"\steamapps\common\", @"\steamlibrary\", @"\epic games\", @"\riot games\",
            @"\xboxgames\", @"\windowsapps\", @"\gog galaxy\games\", @"\gog games\",
            @"\ubisoft game launcher\games\", @"\ea games\", @"\origin games\",
            @"\battle.net\", @"\battlenet\", @"\games\", @"\игры\", @"\genshin impact\", @"\honkai", @"\wuthering waves\"
        };

        private static readonly string[] GameEngineWindowClasses = new string[]
        {
            "UnrealWindow", "UnityWndClass", "SDL_app", "GLFW30", "Valve001", "Source2",
            "CryENGINE", "grcWindow", "RiotWindowClass", "LWJGL", "TankWindowClass",
            "Respawn001", "ApexLegends", "CreationEngine", "SkyrimSpecialEdition", "prism", "D3D Window"
        };

        private static readonly string[] GameSiblingSignatures = new string[]
        {
            "UnityPlayer.dll", "GameAssembly.dll", "steam_api64.dll", "steam_api.dll",
            "EOSSDK-Win64-Shipping.dll", "EOSSDK-Win32-Shipping.dll", "nvngx_dlss.dll",
            "amd_fidelityfx_dx12.dll", "amd_fidelityfx_vk.dll", "libxess.dll",
            "bink2w64.dll", "fmodstudio.dll", "AkSoundEngine.dll", "PhysX3_x64.dll",
            "tier0.dll", "engine2.dll", "EasyAntiCheat", "BattlEye"
        };

        private struct CachedPidClassification
        {
            public bool IsKnownBlacklisted;
            public bool HasPositiveGameTraits;
            public string ExePathLower;
            public int CachedAtTick;
        }

        private static readonly object _cacheLock = new object();
        private static readonly Dictionary<uint, CachedPidClassification> _pidCache = new Dictionary<uint, CachedPidClassification>();

        private static string TryQueryProcessPathFast(uint pid, out string procNameNoExt)
        {
            procNameNoExt = "";
            string fullPath = "";
            IntPtr hProc = IntPtr.Zero;
            try
            {
                hProc = OpenProcess(PROCESS_QUERY_LIMITED_INFORMATION, false, pid);
                if (hProc != IntPtr.Zero)
                {
                    var sb = new StringBuilder(1024);
                    uint sz = (uint)sb.Capacity;
                    if (QueryFullProcessImageNameW(hProc, 0, sb, ref sz) && sz > 0)
                        fullPath = sb.ToString();
                }
            }
            catch { }
            finally
            {
                if (hProc != IntPtr.Zero) CloseHandle(hProc);
            }

            if (!string.IsNullOrEmpty(fullPath))
            {
                try { procNameNoExt = Path.GetFileNameWithoutExtension(fullPath) ?? ""; } catch { }
                return fullPath;
            }

            try { procNameNoExt = Process.GetProcessById((int)pid).ProcessName ?? ""; } catch { }
            return "";
        }

        private static bool HasGameFilesInDirectory(string exePath)
        {
            if (string.IsNullOrEmpty(exePath)) return false;
            try
            {
                string fileName = Path.GetFileName(exePath) ?? "";
                if (fileName.IndexOf("-Win64-Shipping", StringComparison.OrdinalIgnoreCase) >= 0 ||
                    fileName.IndexOf("-WinGDK-Shipping", StringComparison.OrdinalIgnoreCase) >= 0)
                    return true;

                string dir = Path.GetDirectoryName(exePath);
                if (string.IsNullOrEmpty(dir) || !Directory.Exists(dir)) return false;

                for (int i = 0; i < GameSiblingSignatures.Length; i++)
                {
                    string sig = GameSiblingSignatures[i];
                    string p1 = Path.Combine(dir, sig);
                    if (File.Exists(p1) || Directory.Exists(p1)) return true;
                }

                string stem = Path.GetFileNameWithoutExtension(exePath) ?? "";
                if (!string.IsNullOrEmpty(stem) && Directory.Exists(Path.Combine(dir, stem + "_Data")))
                    return true;
                if (dir.IndexOf(@"\binaries\win64", StringComparison.OrdinalIgnoreCase) >= 0 ||
                    dir.IndexOf(@"\binaries\wingdk", StringComparison.OrdinalIgnoreCase) >= 0)
                    return true;
            }
            catch { }
            return false;
        }

        private static CachedPidClassification ClassifyProcessCached(uint pid)
        {
            int now = Environment.TickCount;
            lock (_cacheLock)
            {
                CachedPidClassification existing;
                if (_pidCache.TryGetValue(pid, out existing) && unchecked(now - existing.CachedAtTick) < 45000)
                    return existing;
            }

            string procName;
            string exePath = TryQueryProcessPathFast(pid, out procName);
            string pathLower = (exePath ?? "").ToLowerInvariant();
            string nameTrim = (procName ?? "").Trim();

            bool blacklisted = string.IsNullOrEmpty(nameTrim) || NonGameProcessBlacklist.Contains(nameTrim);
            if (!blacklisted && !string.IsNullOrEmpty(pathLower))
            {
                if (pathLower.Contains(@"\windows\system32\") ||
                    pathLower.Contains(@"\windows\explorer.exe") ||
                    pathLower.Contains(@"\windows\immersivecontrolpanel\") ||
                    pathLower.Contains(@"\program files\google\chrome\") ||
                    pathLower.Contains(@"\program files (x86)\microsoft\edge\") ||
                    pathLower.Contains(@"\appdata\local\discord\") ||
                    pathLower.Contains(@"\appdata\local\programs\yandexmusic\") ||
                    pathLower.Contains(@"\appdata\roaming\spotify\") ||
                    pathLower.Contains(@"\appdata\roaming\telegram desktop\"))
                {
                    blacklisted = true;
                }
            }

            bool positiveGame = false;
            if (!blacklisted)
            {
                for (int i = 0; i < GameInstallPathMarkers.Length; i++)
                {
                    if (pathLower.Contains(GameInstallPathMarkers[i]))
                    {
                        positiveGame = true;
                        break;
                    }
                }
                if (!positiveGame && (nameTrim.Equals("javaw", StringComparison.OrdinalIgnoreCase) || nameTrim.Equals("java", StringComparison.OrdinalIgnoreCase)))
                {
                    if (pathLower.Contains("minecraft") || pathLower.Contains("curseforge") || pathLower.Contains("prismlauncher") || pathLower.Contains("modrinth") || pathLower.Contains("tlauncher") || pathLower.Contains("azul") || pathLower.Contains("adoptium") || pathLower.Contains("runtime"))
                        positiveGame = true;
                }
                if (!positiveGame)
                {
                    positiveGame = HasGameFilesInDirectory(exePath);
                }
            }

            var entry = new CachedPidClassification
            {
                IsKnownBlacklisted = blacklisted,
                HasPositiveGameTraits = positiveGame,
                ExePathLower = pathLower,
                CachedAtTick = now
            };

            lock (_cacheLock)
            {
                if (_pidCache.Count > 256) _pidCache.Clear();
                _pidCache[pid] = entry;
            }
            return entry;
        }

        private static bool IsTransientShellClass(string cls, IntPtr hwnd)
        {
            foreach (string t in TransientShellClasses)
                if (string.Equals(cls, t, StringComparison.Ordinal)) return true;
            if (cls == "ApplicationFrameWindow" || cls == "Windows.UI.Core.CoreWindow")
            {
                var title = new StringBuilder(96);
                NativeWin32.GetWindowText(hwnd, title, 96);
                string tl = title.ToString().ToLowerInvariant();
                if (tl.Contains("task switching") || tl.Contains("task view") || tl.Contains("переключение задач") || tl.Contains("представление задач"))
                    return true;
            }
            return false;
        }

        public static bool IsTransientShellForeground()
        {
            try
            {
                IntPtr fg = NativeWin32.GetForegroundWindow();
                if (fg == IntPtr.Zero) return true;
                var cls = new StringBuilder(128);
                NativeWin32.GetClassName(fg, cls, 128);
                return IsTransientShellClass(cls.ToString(), fg);
            }
            catch { return false; }
        }

        public static bool IsForegroundFullscreenGame(IntPtr ourHwnd, Screen activeScreen, out bool definiteNonGameForeground)
        {
            definiteNonGameForeground = false;
            try
            {
                IntPtr fg = NativeWin32.GetForegroundWindow();
                if (fg == IntPtr.Zero || fg == ourHwnd) return false;

                if (NativeWin32.IsIconic(fg))
                {
                    definiteNonGameForeground = true;
                    return false;
                }

                var clsSb = new StringBuilder(128);
                NativeWin32.GetClassName(fg, clsSb, 128);
                string cls = clsSb.ToString();

                if (IsTransientShellClass(cls, fg)) return false;

                if (cls == "Progman" || cls == "WorkerW" || cls == "Shell_TrayWnd" || cls == "Shell_SecondaryTrayWnd")
                {
                    definiteNonGameForeground = true;
                    return false;
                }

                uint pid;
                NativeWin32.GetWindowThreadProcessId(fg, out pid);
                if (pid == 0) return false;

                CachedPidClassification info = ClassifyProcessCached(pid);
                if (info.IsKnownBlacklisted)
                {
                    definiteNonGameForeground = true;
                    return false;
                }

                if ((cls.StartsWith("Chrome_WidgetWin", StringComparison.Ordinal) || cls == "MozillaWindowClass") && !info.HasPositiveGameTraits)
                {
                    definiteNonGameForeground = true;
                    return false;
                }

                int quns = 0;
                bool isExclusiveD3d = (NativeWin32.SHQueryUserNotificationState(out quns) == 0 && quns == NativeWin32.QUNS_RUNNING_D3D_FULL_SCREEN);

                NativeWin32.RECT rc;
                bool coversMonitor = false;
                if (NativeWin32.GetWindowRect(fg, out rc) && activeScreen != null)
                {
                    Rectangle b = activeScreen.Bounds;
                    coversMonitor = (rc.Left <= b.Left + 2 && rc.Top <= b.Top + 2 && rc.Right >= b.Right - 2 && rc.Bottom >= b.Bottom - 2);
                }

                if (!coversMonitor && !isExclusiveD3d)
                {
                    definiteNonGameForeground = true;
                    return false;
                }

                for (int i = 0; i < GameEngineWindowClasses.Length; i++)
                {
                    if (cls.IndexOf(GameEngineWindowClasses[i], StringComparison.OrdinalIgnoreCase) >= 0)
                        return true;
                }

                if (info.HasPositiveGameTraits || isExclusiveD3d)
                    return true;

                definiteNonGameForeground = true;
                return false;
            }
            catch { return false; }
        }

        private static readonly HashSet<string> FullscreenVideoCapableStems = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "chrome", "msedge", "firefox", "opera", "opera_gx", "yandex", "browser", "brave", "vivaldi",
            "arc", "zen", "waterfox", "thorium", "librewolf", "floorp",
            "vlc", "mpv", "mpc-hc", "mpc-hc64", "mpc-be", "mpc-be64", "potplayer", "potplayermini",
            "potplayermini64", "kmplayer", "gom", "gom64", "wmplayer", "video.ui", "zunevideo",
            "microsoft.media.player", "applicationframehost", "telegram", "ayugram", "discord"
        };

        public static bool IsUniversalVideoPlaying(IntPtr ourHwnd, Screen activeScreen, CoreAudioBridge audio)
        {
            try
            {
                // 1) SMTC explicitly reports active video playback (YouTube, Twitch, Kinopoisk, VK, Windows Media Player, Movies & TV, VLC, etc.)
                if (MultiPlayerMediaController.SmtcVideoPlayingNow)
                    return true;

                bool localVideoAudio = false;
                bool browserAudio = false;
                if (audio != null)
                    audio.CheckActiveVideoAndBrowserAudio(out localVideoAudio, out browserAudio);

                // 2) Dedicated local video player (VLC, MPV, PotPlayer, MPC-HC/BE, Movies & TV) has an active CoreAudio stream
                if (localVideoAudio)
                    return true;

                // 3) Fullscreen video fallback (when browser/player is fullscreen & outputting audio or playing SMTC without PlaybackType.Video)
                IntPtr fg = NativeWin32.GetForegroundWindow();
                if (fg != IntPtr.Zero && fg != ourHwnd && !NativeWin32.IsIconic(fg) && activeScreen != null)
                {
                    NativeWin32.RECT rc;
                    if (NativeWin32.GetWindowRect(fg, out rc))
                    {
                        Rectangle b = activeScreen.Bounds;
                        bool covers = (rc.Left <= b.Left + 2 && rc.Top <= b.Top + 2 && rc.Right >= b.Right - 2 && rc.Bottom >= b.Bottom - 2);
                        if (covers)
                        {
                            uint pid;
                            NativeWin32.GetWindowThreadProcessId(fg, out pid);
                            if (pid != 0)
                            {
                                string pName;
                                TryQueryProcessPathFast(pid, out pName);
                                if (!string.IsNullOrEmpty(pName) && FullscreenVideoCapableStems.Contains(pName))
                                {
                                    if (browserAudio || MultiPlayerMediaController.SmtcBrowserPlayingNow || localVideoAudio)
                                        return true;
                                }
                            }
                        }
                    }
                }
            }
            catch { }
            return false;
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
        [DllImport("oleacc.dll")] public static extern int AccessibleObjectFromWindow(IntPtr hwnd, uint dwId, ref Guid riid, [MarshalAs(UnmanagedType.IUnknown)] out object ppvObject);

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
        [DllImport("user32.dll")] public static extern bool EnumChildWindows(IntPtr hWndParent, EnumWindowsProc lpEnumFunc, IntPtr lParam);
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
        public static readonly IntPtr HWND_NOTOPMOST = new IntPtr(-2);
        public const uint SWP_NOZORDER = 0x0004;
        public const uint SWP_NOOWNERZORDER = 0x0200;
        public const int WM_WINDOWPOSCHANGING = 0x0046;
        public const uint WM_APPCOMMAND = 0x0319;
        public const int APPCOMMAND_MEDIA_PLAY_PAUSE = 14;
        public const int APPCOMMAND_MEDIA_PLAY = 46;
        public const int APPCOMMAND_MEDIA_NEXTTRACK = 11;
        public const int APPCOMMAND_MEDIA_PREVIOUSTRACK = 12;
        public const uint GW_HWNDPREV = 3;
        [DllImport("user32.dll")] public static extern IntPtr GetWindow(IntPtr hWnd, uint uCmd);
        [DllImport("kernel32.dll")] public static extern uint GetCurrentThreadId();
        [DllImport("user32.dll")] public static extern IntPtr GetThreadDesktop(uint dwThreadId);
        [DllImport("user32.dll", CharSet = CharSet.Unicode)]
        public static extern bool GetUserObjectInformationW(IntPtr hObj, int nIndex, StringBuilder pvInfo, int nLength, out int lpnLengthNeeded);

        public static string GetCurrentDesktopNameSafe()
        {
            try
            {
                IntPtr hDesk = GetThreadDesktop(GetCurrentThreadId());
                if (hDesk != IntPtr.Zero)
                {
                    var sb = new StringBuilder(128);
                    int needed;
                    if (GetUserObjectInformationW(hDesk, 2 /* UOI_NAME */, sb, sb.Capacity * 2, out needed) && sb.Length > 0)
                    {
                        var clean = new StringBuilder();
                        foreach (char ch in sb.ToString())
                            clean.Append(char.IsLetterOrDigit(ch) || ch == '_' || ch == '-' ? ch : '_');
                        return clean.ToString();
                    }
                }
            }
            catch { }
            return "Default";
        }
        [StructLayout(LayoutKind.Sequential)]
        public struct WINDOWPOS
        {
            public IntPtr hwnd;
            public IntPtr hwndInsertAfter;
            public int x;
            public int y;
            public int cx;
            public int cy;
            public uint flags;
        }
        public static readonly IntPtr HWND_BROADCAST = new IntPtr(0xFFFF);
        [DllImport("user32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
        public static extern uint RegisterWindowMessageW(string lpString);
        public static readonly uint WM_DI_SHUTDOWN = RegisterWindowMessageW("DynamicIslandWin_GracefulShutdown_v3");
        public static readonly uint WM_DI_ACTIVATE = RegisterWindowMessageW("DynamicIslandWin_ActivateExpand_v3");
        public const uint SWP_NOMOVE = 0x0002;
        public const uint SWP_NOSIZE = 0x0001;
        public const uint SWP_NOACTIVATE = 0x0010;
        public const uint SWP_ASYNCWINDOWPOS = 0x4000;
        public const int WM_MOUSEACTIVATE = 0x0021;
        public const int MA_NOACTIVATE = 3;
        public const int VK_TAB = 0x09;
        public static bool IsAltTabHeld()
        {
            return ((GetAsyncKeyState(VK_MENU) & 0x8000) != 0) || ((GetAsyncKeyState(VK_TAB) & 0x8000) != 0);
        }
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
        public static void SendHardwareKey(byte vk, byte scanCode, bool keyUp)
        {
            keybd_event(vk, scanCode, keyUp ? KEYEVENTF_KEYUP : 0u, UIntPtr.Zero);
        }

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

        private int _lastSampleTick = Environment.TickCount - 100000;
        private int _samplingFlag = 0;
        private int _samplingStartTick = 0;

        // ---- NVML (in-process, ~0.05 ms per query, zero child processes, zero blocking I/O) ----
        [DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true)] private static extern IntPtr LoadLibraryW(string lpFileName);
        [DllImport("kernel32.dll", CharSet = CharSet.Ansi, SetLastError = true)] private static extern IntPtr GetProcAddress(IntPtr hModule, string procName);
        [StructLayout(LayoutKind.Sequential)] private struct NvmlUtilization { public uint gpu; public uint memory; }
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)] private delegate int NvmlInitFn();
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)] private delegate int NvmlGetHandleFn(uint index, out IntPtr device);
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)] private delegate int NvmlGetUtilFn(IntPtr device, out NvmlUtilization util);
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)] private delegate int NvmlGetTempFn(IntPtr device, int sensorType, out uint temp);

        private bool _nvmlTried = false;
        private bool _nvmlReady = false;
        private IntPtr _nvmlDevice = IntPtr.Zero;
        private NvmlGetUtilFn _nvmlGetUtil;
        private NvmlGetTempFn _nvmlGetTemp;

        private void TryInitNvml()
        {
            if (_nvmlTried) return;
            _nvmlTried = true;
            try
            {
                string[] candidates = new string[]
                {
                    "nvml.dll",
                    Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), @"NVIDIA Corporation\NVSMI\nvml.dll"),
                    @"C:\Windows\System32\nvml.dll"
                };
                IntPtr lib = IntPtr.Zero;
                foreach (string c in candidates)
                {
                    lib = LoadLibraryW(c);
                    if (lib != IntPtr.Zero) break;
                }
                if (lib == IntPtr.Zero) return;

                IntPtr pInit = GetProcAddress(lib, "nvmlInit_v2");
                IntPtr pHandle = GetProcAddress(lib, "nvmlDeviceGetHandleByIndex_v2");
                IntPtr pUtil = GetProcAddress(lib, "nvmlDeviceGetUtilizationRates");
                IntPtr pTemp = GetProcAddress(lib, "nvmlDeviceGetTemperature");
                if (pInit == IntPtr.Zero || pHandle == IntPtr.Zero || pUtil == IntPtr.Zero || pTemp == IntPtr.Zero) return;

                var init = (NvmlInitFn)Marshal.GetDelegateForFunctionPointer(pInit, typeof(NvmlInitFn));
                var getHandle = (NvmlGetHandleFn)Marshal.GetDelegateForFunctionPointer(pHandle, typeof(NvmlGetHandleFn));
                if (init() != 0) return;
                IntPtr dev;
                if (getHandle(0, out dev) != 0 || dev == IntPtr.Zero) return;

                _nvmlDevice = dev;
                _nvmlGetUtil = (NvmlGetUtilFn)Marshal.GetDelegateForFunctionPointer(pUtil, typeof(NvmlGetUtilFn));
                _nvmlGetTemp = (NvmlGetTempFn)Marshal.GetDelegateForFunctionPointer(pTemp, typeof(NvmlGetTempFn));
                _nvmlReady = true;
            }
            catch { _nvmlReady = false; }
        }

        public MicroTelemetryService()
        {
            CpuLoadPct = 12;
            CpuTempC = 48;
            GpuLoadPct = 18;
            GpuTempC = 52;
            PingMs = 19;
        }

        public void SampleAsyncIfNeeded(int currentCpuLoad, bool gameMode = false)
        {
            CpuLoadPct = Math.Max(1, Math.Min(100, currentCpuLoad));
            int now = Environment.TickCount;
            int interval = gameMode ? 5000 : 2200;
            if (unchecked(now - _lastSampleTick) < interval) return;

            if (Interlocked.CompareExchange(ref _samplingFlag, 1, 0) != 0)
            {
                // Watchdog: a sampler stuck for >10s must never freeze telemetry forever
                if (unchecked(now - _samplingStartTick) > 10000)
                    Interlocked.Exchange(ref _samplingFlag, 0);
                return;
            }
            _lastSampleTick = now;
            _samplingStartTick = now;

            ThreadPool.QueueUserWorkItem(_ =>
            {
                try
                {
                    // 1) Network ping (bounded 450ms, own pooled thread)
                    try
                    {
                        using (var ping = new Ping())
                        {
                            PingReply rep = ping.Send("1.1.1.1", 450);
                            if (rep != null && rep.Status == IPStatus.Success)
                                PingMs = (int)Math.Max(1, Math.Min(999, rep.RoundtripTime));
                        }
                    }
                    catch { }

                    // 2) GPU load & temperature via in-process NVML (NVIDIA) or smooth thermal model
                    bool gotGpu = false;
                    TryInitNvml();
                    if (_nvmlReady)
                    {
                        try
                        {
                            NvmlUtilization util;
                            uint temp;
                            if (_nvmlGetUtil(_nvmlDevice, out util) == 0 && _nvmlGetTemp(_nvmlDevice, 0, out temp) == 0)
                            {
                                GpuLoadPct = (int)Math.Max(0, Math.Min(100, util.gpu));
                                GpuTempC = (int)Math.Max(25, Math.Min(110, temp));
                                gotGpu = true;
                            }
                        }
                        catch { _nvmlReady = false; }
                    }

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
        public int OrigWidth;
        public int OrigHeight;
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

                // High-DPI crisp lossless thumbnail (up to 512px long edge, exact original aspect ratio, WrapMode.TileFlipXY)
                int srcW = Math.Max(1, bmp.Width);
                int srcH = Math.Max(1, bmp.Height);
                const int maxThumbEdge = 512;
                int tw = srcW;
                int th = srcH;
                if (srcW > maxThumbEdge || srcH > maxThumbEdge)
                {
                    double scale = Math.Min((double)maxThumbEdge / srcW, (double)maxThumbEdge / srcH);
                    tw = Math.Max(1, (int)Math.Round(srcW * scale));
                    th = Math.Max(1, (int)Math.Round(srcH * scale));
                }

                var thumb = new Bitmap(tw, th, PixelFormat.Format32bppPArgb);
                thumb.SetResolution(96f, 96f);
                using (Graphics g = Graphics.FromImage(thumb))
                using (var wrap = new ImageAttributes())
                {
                    wrap.SetWrapMode(WrapMode.TileFlipXY);
                    g.CompositingMode = CompositingMode.SourceCopy;
                    g.CompositingQuality = CompositingQuality.HighQuality;
                    g.InterpolationMode = InterpolationMode.HighQualityBicubic;
                    g.SmoothingMode = SmoothingMode.HighQuality;
                    g.PixelOffsetMode = PixelOffsetMode.HighQuality;
                    g.DrawImage(bmp, new Rectangle(0, 0, tw, th), 0, 0, srcW, srcH, GraphicsUnit.Pixel, wrap);
                }

                var item = new VaultCaptureItem
                {
                    FilePath = outPath,
                    TimeLabel = DateTime.Now.ToString("HH:mm:ss"),
                    Thumbnail = thumb,
                    OrigWidth = srcW,
                    OrigHeight = srcH,
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
    public enum UpdateCheckState { Idle, Checking, UpToDate, Available, NoReleases, Error, Downloading, Installing }

    public sealed class UpdateManager
    {
        public const string CurrentVersionStr = "3.2.1";
        public const string DefaultRepo = "Miltanchik/DynamicIsland";

        public string LatestVersionStr { get; private set; }
        public string ReleaseNotes { get; private set; }
        public string DownloadUrl { get; private set; }
        public string ExpectedSha256 { get; private set; }
        public string ReleasePageUrl { get; private set; }
        public string RepoSlug { get; private set; }
        public string StatusText { get; private set; }
        public string ErrorText { get; private set; }
        public float DownloadProgress { get; private set; }
        public bool DismissedForSession { get; set; }

        private int _state;
        private volatile bool _hasUpdate;
        private int _checkingFlag;

        public UpdateCheckState State
        {
            get { return (UpdateCheckState)Volatile.Read(ref _state); }
            private set { Volatile.Write(ref _state, (int)value); }
        }
        public bool IsUpdateAvailable { get { return _hasUpdate; } }
        public bool IsDownloading
        {
            get { UpdateCheckState s = State; return s == UpdateCheckState.Downloading || s == UpdateCheckState.Installing; }
        }
        public bool IsChecking { get { return State == UpdateCheckState.Checking; } }

        public event Action OnStateChanged;
        public event Action UpdateAvailableDetected;
        public event Action RestartRequested;

        public UpdateManager()
        {
            LatestVersionStr = CurrentVersionStr;
            ReleaseNotes = "";
            DownloadUrl = "";
            ExpectedSha256 = "";
            ReleasePageUrl = "https://github.com/" + DefaultRepo + "/tree/main/release";
            RepoSlug = DefaultRepo;
            ErrorText = "";
            State = UpdateCheckState.Idle;
            StatusText = "v" + CurrentVersionStr + " (Актуальная)";
        }

        private void Raise()
        {
            Action h = OnStateChanged;
            if (h != null) { try { h(); } catch { } }
        }

        private void SetError(string message)
        {
            ErrorText = message;
            StatusText = message;
            State = UpdateCheckState.Error;
            Raise();
        }

        private static void EnsureTls()
        {
            try { ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12 | SecurityProtocolType.Tls11; } catch { }
        }

        private static string NormalizeRepo(string cfgRepo)
        {
            string r = (cfgRepo ?? "").Trim().Trim('/');
            if (r.StartsWith("https://github.com/", StringComparison.OrdinalIgnoreCase)) r = r.Substring(19).Trim('/');
            if (r.EndsWith(".git", StringComparison.OrdinalIgnoreCase)) r = r.Substring(0, r.Length - 4);
            if (r.Length == 0 || r.IndexOf('/') < 1 || r.IndexOf("DynamicIslandWin/DynamicIslandWin", StringComparison.OrdinalIgnoreCase) >= 0)
                return DefaultRepo;
            return r;
        }

        public static Version ParseVersion(string s)
        {
            var m = System.Text.RegularExpressions.Regex.Match(s ?? "", @"(\d+)\.(\d+)(?:\.(\d+))?(?:\.(\d+))?");
            if (!m.Success) return null;
            try
            {
                int a = int.Parse(m.Groups[1].Value);
                int b = int.Parse(m.Groups[2].Value);
                int c = m.Groups[3].Success ? int.Parse(m.Groups[3].Value) : 0;
                int d = m.Groups[4].Success ? int.Parse(m.Groups[4].Value) : 0;
                return new Version(a, b, c, d);
            }
            catch { return null; }
        }

        private static string HttpGetString(string url, int timeoutMs)
        {
            var req = (HttpWebRequest)WebRequest.Create(url);
            req.UserAgent = "DynamicIslandWin-Updater/" + CurrentVersionStr;
            req.Accept = "application/vnd.github+json, text/plain, */*";
            req.Timeout = timeoutMs;
            req.ReadWriteTimeout = timeoutMs;
            req.AllowAutoRedirect = true;
            using (var resp = (HttpWebResponse)req.GetResponse())
            using (var sr = new StreamReader(resp.GetResponseStream(), Encoding.UTF8))
                return sr.ReadToEnd();
        }

        private static string ExtractSha(string sums, string fileName)
        {
            if (string.IsNullOrEmpty(sums) || string.IsNullOrEmpty(fileName)) return "";
            foreach (string raw in sums.Replace("\r", "").Split('\n'))
            {
                string line = raw.Trim().TrimStart('\uFEFF');
                if (line.Length < 66 || line.IndexOf(fileName, StringComparison.OrdinalIgnoreCase) < 0) continue;
                string[] parts = line.Split(new char[] { ' ', '\t', '*' }, StringSplitOptions.RemoveEmptyEntries);
                if (parts.Length >= 2 && parts[0].Length == 64) return parts[0].ToLowerInvariant();
            }
            return "";
        }

        public void CheckForUpdatesAsync(IslandConfigData cfg, bool isManualCheck)
        {
            if (IsDownloading) return;
            if (Interlocked.CompareExchange(ref _checkingFlag, 1, 0) != 0) return;

            State = UpdateCheckState.Checking;
            StatusText = "Проверка обновлений...";
            ErrorText = "";
            Raise();

            string repoCfg = cfg != null ? cfg.github_repo : "";
            string skipped = cfg != null ? (cfg.skipped_version ?? "") : "";
            bool autoInstall = cfg != null && cfg.auto_update_enabled;

            ThreadPool.QueueUserWorkItem(_ =>
            {
                try { CheckCore(repoCfg, skipped, isManualCheck, autoInstall); }
                catch (Exception ex) { SetError("Не удалось проверить обновления: " + ex.Message); }
                finally { Interlocked.Exchange(ref _checkingFlag, 0); Raise(); }
            });
        }

        private static string ExtractChangelogValue(Dictionary<string, object> dict)
        {
            if (dict == null) return "";
            string[] keys = new string[] { "changelog", "notes", "release_notes", "body" };
            foreach (string k in keys)
            {
                if (!dict.ContainsKey(k) || dict[k] == null) continue;
                var arr = dict[k] as System.Collections.IEnumerable;
                if (arr != null && !(dict[k] is string))
                {
                    var sb = new StringBuilder();
                    foreach (object item in arr)
                    {
                        string ln = Convert.ToString(item) ?? "";
                        if (ln.Length > 0) sb.AppendLine("• " + ln);
                    }
                    return sb.ToString().Trim();
                }
                string val = Convert.ToString(dict[k]) ?? "";
                if (!string.IsNullOrWhiteSpace(val)) return val.Trim();
            }
            return "";
        }

        private void CheckCore(string repoCfg, string skipped, bool manual, bool autoInstall)
        {
            EnsureTls();
            string repo = NormalizeRepo(repoCfg);
            RepoSlug = repo;
            ReleasePageUrl = "https://github.com/" + repo + "/tree/main/release";

            var ser = new JavaScriptSerializer();
            ser.MaxJsonLength = int.MaxValue;

            string manifestJson = null;
            string fallbackSetupUrl = "https://raw.githubusercontent.com/" + repo + "/main/release/DynamicIslandWin_Setup.exe";

            // 1) Primary source: raw.githubusercontent.com/<repo>/main/release/version.json (cache-busted)
            try
            {
                string rawManifestUrl = "https://raw.githubusercontent.com/" + repo + "/main/release/version.json?cb=" + unchecked((uint)Environment.TickCount);
                manifestJson = HttpGetString(rawManifestUrl, 8500);
            }
            catch (WebException)
            {
                manifestJson = null;
            }

            // 2) Fallback source: GitHub Contents API https://api.github.com/repos/<repo>/contents/release?ref=main
            if (string.IsNullOrWhiteSpace(manifestJson))
            {
                try
                {
                    string contentsJson = HttpGetString("https://api.github.com/repos/" + repo + "/contents/release?ref=main", 9000);
                    var entries = ser.DeserializeObject(contentsJson) as object[];
                    if (entries != null)
                    {
                        string manifestDlUrl = "";
                        int bestPri = 0;
                        foreach (object entryObj in entries)
                        {
                            var eDict = entryObj as Dictionary<string, object>;
                            if (eDict == null) continue;
                            string nm = Convert.ToString(eDict.ContainsKey("name") ? eDict["name"] : "") ?? "";
                            string dl = Convert.ToString(eDict.ContainsKey("download_url") ? eDict["download_url"] : "") ?? "";
                            string low = nm.ToLowerInvariant();
                            if (low == "version.json" && !string.IsNullOrEmpty(dl))
                            {
                                manifestDlUrl = dl;
                            }
                            else if (low.EndsWith(".exe") && !string.IsNullOrEmpty(dl))
                            {
                                int pri = low == "dynamicislandwin_setup.exe" ? 3 : (low == "dinamicisland-setup.exe" ? 2 : (low.Contains("setup") ? 1 : 0));
                                if (pri > bestPri) { bestPri = pri; fallbackSetupUrl = dl; }
                            }
                        }
                        if (!string.IsNullOrEmpty(manifestDlUrl))
                        {
                            manifestJson = HttpGetString(manifestDlUrl + (manifestDlUrl.Contains("?") ? "&cb=" : "?cb=") + unchecked((uint)Environment.TickCount), 8500);
                        }
                    }
                }
                catch (WebException wex)
                {
                    HttpWebResponse r = wex.Response as HttpWebResponse;
                    if (r != null && r.StatusCode == HttpStatusCode.NotFound)
                    {
                        _hasUpdate = false;
                        ErrorText = "В ветке main репозитория " + repo + " не найдена папка release/version.json.";
                        StatusText = "Манифест release/version.json не найден";
                        State = UpdateCheckState.NoReleases;
                        return;
                    }
                    if (r != null && (int)r.StatusCode == 403)
                    {
                        SetError("Достигнут лимит запросов GitHub API. Повторите проверку через несколько минут.");
                        return;
                    }
                    SetError(r != null
                        ? ("GitHub вернул ошибку " + (int)r.StatusCode + " " + r.StatusDescription)
                        : "Нет соединения с GitHub. Проверьте подключение к интернету.");
                    return;
                }
            }

            if (string.IsNullOrWhiteSpace(manifestJson))
            {
                _hasUpdate = false;
                ErrorText = "Файл release/version.json пока отсутствует в ветке main репозитория " + repo + ".";
                StatusText = "Манифест version.json ещё не опубликован";
                State = UpdateCheckState.NoReleases;
                return;
            }

            string cleanJson = manifestJson.Trim().TrimStart('\uFEFF');
            var dict = ser.Deserialize<Dictionary<string, object>>(cleanJson);
            if (dict == null || (!dict.ContainsKey("version") && !dict.ContainsKey("tag_name")))
            {
                SetError("Некорректный формат release/version.json (отсутствует поле version).");
                return;
            }

            string tagRaw = Convert.ToString(dict.ContainsKey("version") ? dict["version"] : dict["tag_name"]) ?? "";
            string tag = tagRaw.Trim().TrimStart('v', 'V');
            Version vNew = ParseVersion(tagRaw);
            Version vCur = ParseVersion(CurrentVersionStr);
            if (vNew == null || vCur == null)
            {
                SetError("Не удалось разобрать номер версии в version.json: " + tagRaw);
                return;
            }

            string body = ExtractChangelogValue(dict);
            string htmlUrl = dict.ContainsKey("release_url") ? (Convert.ToString(dict["release_url"]) ?? "") : "";
            if (string.IsNullOrWhiteSpace(htmlUrl)) htmlUrl = ReleasePageUrl;

            string setupUrl = dict.ContainsKey("download_url") ? (Convert.ToString(dict["download_url"]) ?? "").Trim() : "";
            if (string.IsNullOrWhiteSpace(setupUrl))
            {
                setupUrl = fallbackSetupUrl;
            }
            else if (!setupUrl.StartsWith("http://", StringComparison.OrdinalIgnoreCase) &&
                     !setupUrl.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
            {
                string relPath = setupUrl.TrimStart('/');
                if (!relPath.StartsWith("release/", StringComparison.OrdinalIgnoreCase))
                    relPath = "release/" + relPath;
                setupUrl = "https://raw.githubusercontent.com/" + repo + "/main/" + relPath;
            }

            string sha = "";
            if (dict.ContainsKey("sha256") && dict["sha256"] != null)
            {
                string rawSha = (Convert.ToString(dict["sha256"]) ?? "").Trim().ToLowerInvariant();
                if (rawSha.Length == 64) sha = rawSha;
            }

            if (vNew > vCur)
            {
                if (!manual && string.Equals(skipped, tag, StringComparison.OrdinalIgnoreCase))
                {
                    _hasUpdate = false;
                    StatusText = "v" + CurrentVersionStr + " (Пропущена v" + tag + ")";
                    State = UpdateCheckState.UpToDate;
                    return;
                }

                string previous = LatestVersionStr;
                LatestVersionStr = tag;
                ReleaseNotes = body;
                DownloadUrl = setupUrl;
                ExpectedSha256 = sha;
                ReleasePageUrl = htmlUrl;
                _hasUpdate = true;
                if (manual || !string.Equals(previous, tag, StringComparison.OrdinalIgnoreCase)) DismissedForSession = false;
                StatusText = "Доступна v" + tag + "!";
                State = UpdateCheckState.Available;
                Raise();

                Action ev = UpdateAvailableDetected;
                if (ev != null) { try { ev(); } catch { } }

                if (autoInstall && !string.IsNullOrEmpty(setupUrl))
                    StartDownloadAndApplyAsync();
                return;
            }

            _hasUpdate = false;
            LatestVersionStr = tag;
            StatusText = "Установлена актуальная v" + CurrentVersionStr;
            State = UpdateCheckState.UpToDate;
        }

        public void StartDownloadAndApplyAsync()
        {
            if (IsDownloading) return;
            if (string.IsNullOrEmpty(DownloadUrl))
            {
                try { Process.Start(new ProcessStartInfo(string.IsNullOrEmpty(ReleasePageUrl) ? ("https://github.com/" + DefaultRepo + "/tree/main/release") : ReleasePageUrl) { UseShellExecute = true }); } catch { }
                return;
            }

            State = UpdateCheckState.Downloading;
            DownloadProgress = 0.02f;
            StatusText = "Загрузка v" + LatestVersionStr + "...";
            ErrorText = "";
            Raise();

            string url = DownloadUrl;
            string expectedSha = ExpectedSha256;
            string versionLabel = LatestVersionStr;

            ThreadPool.QueueUserWorkItem(_ =>
            {
                try
                {
                    EnsureTls();
                    string updDir = Path.Combine(Path.GetTempPath(), @"DinamicIsland\Update");
                    if (!Directory.Exists(updDir)) Directory.CreateDirectory(updDir);
                    string finalFile = Path.Combine(updDir, "DynamicIslandWin_Setup.exe");
                    string partFile = finalFile + ".part";
                    try { if (File.Exists(partFile)) File.Delete(partFile); } catch { }

                    var req = (HttpWebRequest)WebRequest.Create(url);
                    req.UserAgent = "DynamicIslandWin-Updater/" + CurrentVersionStr;
                    req.Accept = "application/octet-stream";
                    req.Timeout = 30000;
                    req.ReadWriteTimeout = 30000;
                    req.AllowAutoRedirect = true;

                    using (var resp = (HttpWebResponse)req.GetResponse())
                    using (var rs = resp.GetResponseStream())
                    using (var fs = new FileStream(partFile, FileMode.Create, FileAccess.Write, FileShare.None))
                    {
                        long total = resp.ContentLength;
                        byte[] buf = new byte[32768];
                        long readTotal = 0;
                        int lastRaise = Environment.TickCount;
                        int n;
                        while ((n = rs.Read(buf, 0, buf.Length)) > 0)
                        {
                            fs.Write(buf, 0, n);
                            readTotal += n;
                            if (total > 0) DownloadProgress = Math.Max(0.03f, Math.Min(0.97f, readTotal / (float)total));
                            if (unchecked(Environment.TickCount - lastRaise) > 80) { lastRaise = Environment.TickCount; Raise(); }
                        }
                    }

                    var fi = new FileInfo(partFile);
                    if (fi.Length < 200 * 1024) throw new IOException("Загруженный файл повреждён (слишком маленький размер).");
                    using (var chk = File.OpenRead(partFile))
                    {
                        int b0 = chk.ReadByte(), b1 = chk.ReadByte();
                        if (b0 != 'M' || b1 != 'Z') throw new IOException("Загруженный файл не является установщиком Windows.");
                    }

                    if (!string.IsNullOrEmpty(expectedSha))
                    {
                        string actual;
                        using (var sha = SHA256.Create())
                        using (var fsr = File.OpenRead(partFile))
                        {
                            var sb = new StringBuilder();
                            foreach (byte b in sha.ComputeHash(fsr)) sb.Append(b.ToString("x2"));
                            actual = sb.ToString();
                        }
                        if (!string.Equals(actual, expectedSha, StringComparison.OrdinalIgnoreCase))
                        {
                            try { File.Delete(partFile); } catch { }
                            throw new IOException("Контрольная сумма SHA-256 не совпала — обновление отменено ради безопасности.");
                        }
                    }

                    if (File.Exists(finalFile)) File.Delete(finalFile);
                    File.Move(partFile, finalFile);

                    DownloadProgress = 1.0f;
                    StatusText = "Запуск установщика v" + versionLabel + "...";
                    State = UpdateCheckState.Installing;
                    Raise();

                    Process.Start(new ProcessStartInfo { FileName = finalFile, Arguments = "--silent", UseShellExecute = false });
                    Thread.Sleep(400);
                    Action rh = RestartRequested;
                    if (rh != null) rh();
                }
                catch (Exception ex)
                {
                    SetError("Ошибка загрузки обновления: " + ex.Message);
                }
            });
        }
    }

    public sealed class UpdateDialogForm : Form
    {
        private static readonly Color ColChassis = Color.FromArgb(11, 11, 13);
        private static readonly Color ColCard = Color.FromArgb(18, 18, 21);
        private static readonly Color ColInset = Color.FromArgb(13, 13, 16);
        private static readonly Color ColHover = Color.FromArgb(26, 26, 31);
        private static readonly Color ColBorder = Color.FromArgb(21, 255, 255, 255);
        private static readonly Color ColBorderHi = Color.FromArgb(56, 255, 255, 255);
        private static readonly Color ColAccent = Color.FromArgb(255, 85, 0);
        private static readonly Color ColLime = Color.FromArgb(200, 255, 0);
        private static readonly Color ColText = Color.FromArgb(242, 243, 247);
        private static readonly Color ColMuted = Color.FromArgb(138, 142, 154);
        private static readonly Color ColDim = Color.FromArgb(92, 96, 108);

        private readonly UpdateManager _um;
        private readonly IslandConfigData _cfg;
        private readonly bool _noActivate;
        private readonly System.Windows.Forms.Timer _animTimer;
        private readonly Action _stateHandler;

        private sealed class Hit { public RectangleF Rect; public Action Click; }
        private readonly List<Hit> _hits = new List<Hit>();
        private PointF _mouse = new PointF(-99, -99);
        private float _scroll = 0f;
        private float _maxScroll = 0f;
        private bool _dragging = false;
        private Point _dragOff;

        private readonly Font _fTitle = new Font("Consolas", 8.8f, FontStyle.Bold);
        private readonly Font _fHuge = new Font("Consolas", 19f, FontStyle.Bold);
        private readonly Font _fMono = new Font("Consolas", 9.4f, FontStyle.Bold);
        private readonly Font _fTiny = new Font("Consolas", 7.8f, FontStyle.Bold);
        private readonly Font _fBody = new Font("Segoe UI", 9.0f, FontStyle.Regular);
        private readonly Font _fBtn = new Font("Consolas", 8.2f, FontStyle.Bold);
        private readonly Font _fHead = new Font("Consolas", 14f, FontStyle.Bold);
        private readonly StringFormat _sfC = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center, Trimming = StringTrimming.EllipsisCharacter, FormatFlags = StringFormatFlags.NoWrap };
        private readonly StringFormat _sfL = new StringFormat { Alignment = StringAlignment.Near, LineAlignment = StringAlignment.Center, Trimming = StringTrimming.EllipsisCharacter, FormatFlags = StringFormatFlags.NoWrap };

        public UpdateDialogForm(UpdateManager um, IslandConfigData cfg, bool noActivate)
        {
            _um = um;
            _cfg = cfg;
            _noActivate = noActivate;
            Text = "Dynamic Island — Обновление";
            FormBorderStyle = FormBorderStyle.None;
            StartPosition = FormStartPosition.CenterScreen;
            TopMost = true;
            ShowInTaskbar = false;
            DoubleBuffered = true;
            SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.UserPaint, true);
            ClientSize = new Size(560, 392);
            BackColor = ColChassis;
            try { Icon = Icon.ExtractAssociatedIcon(Application.ExecutablePath); } catch { }
            Region = Region.FromHrgn(NativeWin32.CreateRoundRectRgn(0, 0, Width + 1, Height + 1, 10, 10));

            _stateHandler = delegate
            {
                try
                {
                    if (IsDisposed || !IsHandleCreated) return;
                    BeginInvoke((MethodInvoker)delegate { if (!IsDisposed) Invalidate(); });
                }
                catch { }
            };
            _um.OnStateChanged += _stateHandler;

            _animTimer = new System.Windows.Forms.Timer { Interval = 40 };
            _animTimer.Tick += delegate
            {
                UpdateCheckState s = _um.State;
                if (s == UpdateCheckState.Checking || s == UpdateCheckState.Downloading || s == UpdateCheckState.Installing)
                    Invalidate();
            };
            _animTimer.Start();
        }

        protected override bool ShowWithoutActivation { get { return _noActivate; } }

        protected override CreateParams CreateParams
        {
            get
            {
                CreateParams cp = base.CreateParams;
                cp.ExStyle |= NativeWin32.WS_EX_TOOLWINDOW; // never listed in Alt+Tab
                return cp;
            }
        }

        protected override void OnFormClosed(FormClosedEventArgs e)
        {
            try { _um.OnStateChanged -= _stateHandler; } catch { }
            try { _animTimer.Stop(); _animTimer.Dispose(); } catch { }
            base.OnFormClosed(e);
        }

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            if (_um.State == UpdateCheckState.Available) _um.DismissedForSession = true;
            base.OnFormClosing(e);
        }

        private static GraphicsPath RR(RectangleF r, float rad)
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

        private void Btn(Graphics g, RectangleF rc, string label, int kind, Action click)
        {
            bool hov = rc.Contains(_mouse);
            Color bg = kind == 1 ? (hov ? Color.FromArgb(255, 102, 24) : ColAccent)
                     : (kind == 2 ? (hov ? Color.FromArgb(52, 22, 24) : Color.FromArgb(28, 16, 18))
                     : (hov ? ColHover : ColCard));
            Color bd = kind == 1 ? Color.FromArgb(220, 255, 140, 80)
                     : (kind == 2 ? Color.FromArgb(110, 255, 70, 60) : (hov ? ColBorderHi : ColBorder));
            Color fg = kind == 1 ? Color.FromArgb(12, 12, 14) : (kind == 2 ? Color.FromArgb(255, 105, 95) : ColText);
            using (GraphicsPath p = RR(rc, 4f))
            using (var b = new SolidBrush(bg))
            using (var pen = new Pen(bd, 1f))
            using (var tb = new SolidBrush(fg))
            {
                g.FillPath(b, p);
                g.DrawPath(pen, p);
                g.DrawString(label, _fBtn, tb, rc, _sfC);
            }
            if (click != null) _hits.Add(new Hit { Rect = rc, Click = click });
        }

        private static string PlainNotes(string md)
        {
            string t = (md ?? "").Replace("\r\n", "\n").Replace("\r", "\n");
            t = System.Text.RegularExpressions.Regex.Replace(t, @"\[([^\]]+)\]\([^\)]+\)", "$1");
            t = System.Text.RegularExpressions.Regex.Replace(t, @"(?m)^\s{0,3}#{1,6}\s*", "");
            t = System.Text.RegularExpressions.Regex.Replace(t, @"(?m)^\s*[\*\-]\s+", "•  ");
            t = t.Replace("**", "").Replace("__", "").Replace("`", "");
            t = System.Text.RegularExpressions.Regex.Replace(t, @"\n{3,}", "\n\n").Trim();
            if (t.Length == 0) t = "Улучшения стабильности и новые функции Studio Pro.";
            return t;
        }

        private void DoCheck() { _scroll = 0f; _um.CheckForUpdatesAsync(_cfg, true); Invalidate(); }

        private void DoOpenReleases()
        {
            try { Process.Start(new ProcessStartInfo(_um.ReleasePageUrl) { UseShellExecute = true }); } catch { }
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            Graphics g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.TextRenderingHint = TextRenderingHint.ClearTypeGridFit;
            _hits.Clear();

            using (var bg = new SolidBrush(ColChassis)) g.FillRectangle(bg, ClientRectangle);

            using (var bar = new SolidBrush(Color.FromArgb(14, 14, 17)))
            using (var pen = new Pen(ColBorder, 1f))
            using (var ac = new SolidBrush(ColAccent))
            using (var tb = new SolidBrush(ColText))
            using (var db = new SolidBrush(ColDim))
            {
                g.FillRectangle(bar, 0, 0, Width, 34);
                g.DrawLine(pen, 0, 34, Width, 34);
                g.FillRectangle(ac, 14, 11, 8, 12);
                g.DrawString("UPDATE CENTER // DYNAMIC ISLAND", _fTitle, tb, new RectangleF(28, 0, 330, 34), _sfL);
                g.DrawString("ТЕКУЩАЯ v" + UpdateManager.CurrentVersionStr, _fTiny, db, new RectangleF(Width - 230, 0, 150, 34), _sfL);
            }
            Btn(g, new RectangleF(Width - 70, 6, 56, 22), "ЗАКРЫТЬ", 0, delegate { Close(); });

            UpdateCheckState st = _um.State;
            float W = Width;
            float H = Height;
            var body = new RectangleF(16f, 46f, W - 32f, H - 62f);
            float btnY = H - 52f;

            if (st == UpdateCheckState.Available)
            {
                using (var latest = new SolidBrush(ColAccent))
                using (var cur = new SolidBrush(ColMuted))
                using (var mut = new SolidBrush(ColMuted))
                {
                    g.DrawString("v" + UpdateManager.CurrentVersionStr, _fHuge, cur, new RectangleF(body.X, body.Y + 2, 120, 38), _sfL);
                    g.DrawString("→", _fHuge, mut, new RectangleF(body.X + 110, body.Y + 2, 40, 38), _sfC);
                    g.DrawString("v" + _um.LatestVersionStr, _fHuge, latest, new RectangleF(body.X + 150, body.Y + 2, 200, 38), _sfL);
                    g.DrawString("ДОСТУПНО ОБНОВЛЕНИЕ  •  " + _um.RepoSlug, _fTiny, mut, new RectangleF(body.X, body.Y + 42, body.Width, 16), _sfL);
                }
                using (var led = new SolidBrush(ColLime)) g.FillEllipse(led, body.Right - 14, body.Y + 14, 8, 8);

                var box = new RectangleF(body.X, body.Y + 66, body.Width, btnY - body.Y - 78f);
                using (GraphicsPath bp = RR(box, 5f))
                using (var bb = new SolidBrush(ColInset))
                using (var bpn = new Pen(ColBorder, 1f))
                {
                    g.FillPath(bb, bp);
                    g.DrawPath(bpn, bp);
                }
                using (var hb = new SolidBrush(ColAccent))
                    g.DrawString("[CHANGELOG]", _fTiny, hb, new RectangleF(box.X + 10, box.Y + 4, 140, 16), _sfL);

                var view = new RectangleF(box.X + 8, box.Y + 24, box.Width - 22, box.Height - 30);
                string notes = PlainNotes(_um.ReleaseNotes);
                SizeF sz = g.MeasureString(notes, _fBody, (int)view.Width);
                _maxScroll = Math.Max(0f, sz.Height - view.Height + 6f);
                _scroll = Math.Max(0f, Math.Min(_maxScroll, _scroll));

                Region prev = g.Clip;
                g.SetClip(view);
                using (var nb = new SolidBrush(Color.FromArgb(226, 228, 236)))
                    g.DrawString(notes, _fBody, nb, new RectangleF(view.X, view.Y - _scroll, view.Width, sz.Height + 4f));
                g.Clip = prev;

                if (_maxScroll > 1f)
                {
                    var track = new RectangleF(box.Right - 9, view.Y, 3, view.Height);
                    float th = Math.Max(18f, view.Height * (view.Height / (view.Height + _maxScroll)));
                    float ty = track.Y + (track.Height - th) * (_scroll / _maxScroll);
                    using (var tr = new SolidBrush(Color.FromArgb(24, 255, 255, 255)))
                    using (var th2 = new SolidBrush(ColAccent))
                    {
                        g.FillRectangle(tr, track);
                        g.FillRectangle(th2, track.X, ty, track.Width, th);
                    }
                }

                string sha = _um.ExpectedSha256;
                using (var sb = new SolidBrush(ColDim))
                    g.DrawString(string.IsNullOrEmpty(sha) ? ("ИСТОЧНИК: " + _um.RepoSlug + "/main/release  •  PE-ПРОВЕРКА ВКЛЮЧЕНА") : ("SHA-256: " + sha.Substring(0, 16) + "…  ПРОВЕРКА ВКЛЮЧЕНА"),
                        _fTiny, sb, new RectangleF(body.X, btnY - 14, body.Width, 12), _sfL);

                Btn(g, new RectangleF(body.X, btnY + 2, 214, 30), "ОБНОВИТЬ СЕЙЧАС", 1, delegate { _um.StartDownloadAndApplyAsync(); Invalidate(); });
                Btn(g, new RectangleF(body.X + 222, btnY + 2, 110, 30), "ПОЗЖЕ", 0, delegate { _um.DismissedForSession = true; Close(); });
                Btn(g, new RectangleF(body.X + 340, btnY + 2, body.Width - 340, 30), "ПРОПУСТИТЬ ЭТУ ВЕРСИЮ", 2, delegate
                {
                    _cfg.skipped_version = _um.LatestVersionStr;
                    ConfigManager.Save(_cfg);
                    _um.DismissedForSession = true;
                    Close();
                });
                return;
            }

            if (st == UpdateCheckState.Downloading || st == UpdateCheckState.Installing)
            {
                using (var tb = new SolidBrush(ColText))
                using (var mb = new SolidBrush(ColMuted))
                {
                    g.DrawString(st == UpdateCheckState.Installing ? "УСТАНОВКА ОБНОВЛЕНИЯ" : "ЗАГРУЗКА ОБНОВЛЕНИЯ", _fMono, tb, new RectangleF(body.X, body.Y + 60, body.Width, 24), _sfC);
                    g.DrawString("v" + UpdateManager.CurrentVersionStr + "  →  v" + _um.LatestVersionStr, _fHuge, new SolidBrush(ColAccent), new RectangleF(body.X, body.Y + 90, body.Width, 40), _sfC);
                    g.DrawString(_um.StatusText, _fTiny, mb, new RectangleF(body.X, body.Y + 138, body.Width, 18), _sfC);
                }
                var track = new RectangleF(body.X + 20, body.Y + 168, body.Width - 40, 10);
                using (GraphicsPath tp = RR(track, 3f))
                using (var tbk = new SolidBrush(Color.FromArgb(30, 255, 255, 255)))
                    g.FillPath(tbk, tp);
                float prog = Math.Max(0.02f, Math.Min(1f, _um.DownloadProgress));
                var fill = new RectangleF(track.X, track.Y, track.Width * prog, track.Height);
                using (GraphicsPath fp = RR(fill, 3f))
                using (var fb = new SolidBrush(ColAccent))
                    g.FillPath(fb, fp);
                using (var pb = new SolidBrush(ColText))
                    g.DrawString(((int)Math.Round(prog * 100f)) + "%", _fMono, pb, new RectangleF(body.X, body.Y + 184, body.Width, 22), _sfC);
                using (var nb = new SolidBrush(ColDim))
                    g.DrawString("После загрузки Dynamic Island перезапустится автоматически.", _fTiny, nb, new RectangleF(body.X, body.Y + 214, body.Width, 18), _sfC);
                return;
            }

            string head = "";
            string sub = "";
            Color headCol = ColText;
            if (st == UpdateCheckState.Checking) { head = "ПРОВЕРКА ОБНОВЛЕНИЙ…"; sub = "Запрос к main/release/version.json: " + _um.RepoSlug; }
            else if (st == UpdateCheckState.UpToDate) { head = "✓ ВЫ НА АКТУАЛЬНОЙ ВЕРСИИ"; sub = "Установлена v" + UpdateManager.CurrentVersionStr + "  •  " + _um.RepoSlug; headCol = ColLime; }
            else if (st == UpdateCheckState.NoReleases) { head = "РЕЛИЗОВ ПОКА НЕТ"; sub = _um.ErrorText; headCol = Color.FromArgb(255, 189, 46); }
            else if (st == UpdateCheckState.Error) { head = "ОШИБКА ПРОВЕРКИ"; sub = _um.ErrorText; headCol = Color.FromArgb(255, 105, 95); }
            else { head = "ЦЕНТР ОБНОВЛЕНИЙ"; sub = "Нажмите «ПРОВЕРИТЬ» для проверки main/release/version.json."; }

            using (var hb = new SolidBrush(headCol))
                g.DrawString(head, _fHead, hb, new RectangleF(body.X, body.Y + 58, body.Width, 34), _sfC);
            using (var sb = new SolidBrush(ColMuted))
            {
                var sf = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Near };
                g.DrawString(sub, _fBody, sb, new RectangleF(body.X + 24, body.Y + 104, body.Width - 48, 90), sf);
            }
            if (st == UpdateCheckState.Checking)
            {
                var track = new RectangleF(body.X + 60, body.Y + 190, body.Width - 120, 6);
                using (var tbk = new SolidBrush(Color.FromArgb(30, 255, 255, 255))) g.FillRectangle(tbk, track);
                float ph = (Environment.TickCount % 1100) / 1100f;
                float w = track.Width * 0.30f;
                float x = track.X + (track.Width + w) * ph - w;
                Region prev = g.Clip;
                g.SetClip(track);
                using (var ab = new SolidBrush(ColAccent)) g.FillRectangle(ab, x, track.Y, w, track.Height);
                g.Clip = prev;
                return;
            }

            if (st == UpdateCheckState.NoReleases)
            {
                Btn(g, new RectangleF(body.X, btnY, 250, 32), "ОТКРЫТЬ ПАПКУ RELEASE", 1, delegate { DoOpenReleases(); });
                Btn(g, new RectangleF(body.X + 258, btnY, body.Width - 258, 32), "ПРОВЕРИТЬ СНОВА", 0, delegate { DoCheck(); });
            }
            else if (st == UpdateCheckState.Error)
            {
                Btn(g, new RectangleF(body.X, btnY, 250, 32), "ПОВТОРИТЬ", 1, delegate
                {
                    if (_um.IsUpdateAvailable && !string.IsNullOrEmpty(_um.DownloadUrl)) _um.StartDownloadAndApplyAsync(); else DoCheck();
                    Invalidate();
                });
                Btn(g, new RectangleF(body.X + 258, btnY, body.Width - 258, 32), "ОТКРЫТЬ ПАПКУ RELEASE", 0, delegate { DoOpenReleases(); });
            }
            else
            {
                Btn(g, new RectangleF(body.X, btnY, 250, 32), "ПРОВЕРИТЬ СНОВА", 1, delegate { DoCheck(); });
                Btn(g, new RectangleF(body.X + 258, btnY, body.Width - 258, 32), "ЗАКРЫТЬ", 0, delegate { Close(); });
            }
        }

        protected override void OnMouseWheel(MouseEventArgs e)
        {
            base.OnMouseWheel(e);
            if (_maxScroll <= 0f) return;
            _scroll = Math.Max(0f, Math.Min(_maxScroll, _scroll - Math.Sign(e.Delta) * 36f));
            Invalidate();
        }

        protected override void OnMouseDown(MouseEventArgs e)
        {
            base.OnMouseDown(e);
            if (e.Button != MouseButtons.Left) return;
            var pt = new PointF(e.X, e.Y);
            foreach (Hit h in _hits)
            {
                if (h.Rect.Contains(pt)) { h.Click(); return; }
            }
            if (e.Y <= 34) { _dragging = true; _dragOff = e.Location; }
        }

        protected override void OnMouseMove(MouseEventArgs e)
        {
            base.OnMouseMove(e);
            _mouse = new PointF(e.X, e.Y);
            if (_dragging && e.Button == MouseButtons.Left)
            {
                Point sp = PointToScreen(e.Location);
                Location = new Point(sp.X - _dragOff.X, sp.Y - _dragOff.Y);
                return;
            }
            Invalidate();
        }

        protected override void OnMouseUp(MouseEventArgs e)
        {
            base.OnMouseUp(e);
            _dragging = false;
        }
    }
    public sealed class SettingsForm : Form
    {
        private readonly IslandConfigData _cfg;
        public event Action SettingsSaved;
        public event Action ManualUpdateCheckRequested;
        public UpdateManager Updater { get; set; }
        private UpdateCheckState _lastShownUpdState = UpdateCheckState.Idle;

        // Dynamic Studio Palette (Live-Switchable: Kinetic Dark, Daylight Light, Cyber Neon, Monochrome, Custom + Island Sync)
        private Color ColChassis = Color.FromArgb(11, 11, 13);
        private Color ColHeaderBar = Color.FromArgb(14, 14, 17);
        private Color ColStageBg = Color.FromArgb(14, 14, 17);
        private readonly Color ColPureIsland = Color.FromArgb(0, 0, 0);
        private Color ColDeckCard = Color.FromArgb(18, 18, 21);
        private Color ColDeckHover = Color.FromArgb(26, 26, 31);
        private Color ColDeckInset = Color.FromArgb(13, 13, 16);
        private Color ColWellBg = Color.FromArgb(9, 9, 11);
        private Color ColBorder = Color.FromArgb(21, 255, 255, 255);
        private Color ColBorderHi = Color.FromArgb(56, 255, 255, 255);
        private Color ColAccent = Color.FromArgb(255, 85, 0);
        private Color ColAccentHover = Color.FromArgb(255, 106, 28);
        private Color ColLime = Color.FromArgb(200, 255, 0);
        private Color ColTextMain = Color.FromArgb(242, 243, 247);
        private Color ColTextMuted = Color.FromArgb(138, 142, 154);
        private Color ColTextDim = Color.FromArgb(92, 96, 108);
        private Color ColOnAccentText = Color.FromArgb(12, 12, 14);
        private bool _isLightTheme = false;

        private Color ColAccentAlpha(int a)
        {
            return Color.FromArgb(Math.Max(0, Math.Min(255, a)), ColAccent.R, ColAccent.G, ColAccent.B);
        }

        private Color ColInkAlpha(int a)
        {
            int ca = Math.Max(0, Math.Min(255, a));
            return _isLightTheme ? Color.FromArgb(ca, 16, 22, 38) : Color.FromArgb(ca, 255, 255, 255);
        }

        private void RefreshSettingsPalette()
        {
            string preset = _cfg.settings_theme_preset ?? "Kinetic Dark";
            Color defAcc = Color.FromArgb(255, 85, 0);
            Color defSec = Color.FromArgb(200, 255, 0);

            if (preset == "Daylight Light")
            {
                _isLightTheme = true;
                ColChassis = Color.FromArgb(239, 241, 246);
                ColHeaderBar = Color.FromArgb(229, 233, 241);
                ColStageBg = Color.FromArgb(231, 235, 243);
                ColDeckCard = Color.FromArgb(255, 255, 255);
                ColDeckHover = Color.FromArgb(243, 246, 252);
                ColDeckInset = Color.FromArgb(234, 238, 246);
                ColWellBg = Color.FromArgb(223, 228, 239);
                defAcc = Color.FromArgb(10, 132, 255);
                defSec = Color.FromArgb(16, 185, 129);
            }
            else if (preset == "Cyber Neon")
            {
                _isLightTheme = false;
                ColChassis = Color.FromArgb(7, 9, 19);
                ColHeaderBar = Color.FromArgb(10, 14, 28);
                ColStageBg = Color.FromArgb(10, 13, 26);
                ColDeckCard = Color.FromArgb(16, 21, 40);
                ColDeckHover = Color.FromArgb(24, 32, 58);
                ColDeckInset = Color.FromArgb(11, 15, 30);
                ColWellBg = Color.FromArgb(6, 8, 17);
                defAcc = Color.FromArgb(34, 211, 238);
                defSec = Color.FromArgb(236, 72, 153);
            }
            else if (preset == "Monochrome")
            {
                _isLightTheme = false;
                ColChassis = Color.FromArgb(14, 14, 16);
                ColHeaderBar = Color.FromArgb(18, 18, 21);
                ColStageBg = Color.FromArgb(17, 17, 20);
                ColDeckCard = Color.FromArgb(24, 24, 28);
                ColDeckHover = Color.FromArgb(35, 35, 41);
                ColDeckInset = Color.FromArgb(18, 18, 21);
                ColWellBg = Color.FromArgb(10, 10, 12);
                defAcc = Color.FromArgb(228, 228, 234);
                defSec = Color.FromArgb(161, 161, 170);
            }
            else
            {
                _isLightTheme = false;
                ColChassis = Color.FromArgb(11, 11, 13);
                ColHeaderBar = Color.FromArgb(14, 14, 17);
                ColStageBg = Color.FromArgb(14, 14, 17);
                ColDeckCard = Color.FromArgb(18, 18, 21);
                ColDeckHover = Color.FromArgb(26, 26, 31);
                ColDeckInset = Color.FromArgb(13, 13, 16);
                ColWellBg = Color.FromArgb(9, 9, 11);
                defAcc = Color.FromArgb(255, 85, 0);
                defSec = Color.FromArgb(200, 255, 0);
            }

            if (preset == "Custom" && !string.IsNullOrWhiteSpace(_cfg.settings_custom_bg_hex))
            {
                Color baseBg = ColorThemeUtils.ParseHex(_cfg.settings_custom_bg_hex, Color.FromArgb(14, 16, 22));
                _isLightTheme = ColorThemeUtils.GetLuminance(baseBg) >= 140.0;
                if (_isLightTheme)
                {
                    ColChassis = baseBg;
                    ColHeaderBar = ColorThemeUtils.Blend(baseBg, Color.Black, 0.05f);
                    ColStageBg = ColorThemeUtils.Blend(baseBg, Color.Black, 0.04f);
                    ColDeckCard = ColorThemeUtils.Blend(baseBg, Color.White, 0.65f);
                    ColDeckHover = ColorThemeUtils.Blend(baseBg, Color.White, 0.35f);
                    ColDeckInset = ColorThemeUtils.Blend(baseBg, Color.Black, 0.04f);
                    ColWellBg = ColorThemeUtils.Blend(baseBg, Color.Black, 0.09f);
                }
                else
                {
                    ColChassis = baseBg;
                    ColHeaderBar = ColorThemeUtils.Blend(baseBg, Color.White, 0.03f);
                    ColStageBg = ColorThemeUtils.Blend(baseBg, Color.White, 0.025f);
                    ColDeckCard = ColorThemeUtils.Blend(baseBg, Color.White, 0.055f);
                    ColDeckHover = ColorThemeUtils.Blend(baseBg, Color.White, 0.105f);
                    ColDeckInset = ColorThemeUtils.Blend(baseBg, Color.Black, 0.18f);
                    ColWellBg = ColorThemeUtils.Blend(baseBg, Color.Black, 0.35f);
                }
            }

            if (_isLightTheme)
            {
                ColTextMain = Color.FromArgb(18, 22, 33);
                ColTextMuted = Color.FromArgb(78, 86, 106);
                ColTextDim = Color.FromArgb(118, 126, 146);
                ColBorder = Color.FromArgb(32, 15, 22, 40);
                ColBorderHi = Color.FromArgb(76, 15, 22, 40);
            }
            else
            {
                ColTextMain = Color.FromArgb(242, 243, 247);
                ColTextMuted = Color.FromArgb(138, 142, 154);
                ColTextDim = Color.FromArgb(92, 96, 108);
                ColBorder = Color.FromArgb(22, 255, 255, 255);
                ColBorderHi = Color.FromArgb(58, 255, 255, 255);
            }

            Color chosenAcc = defAcc;
            Color chosenSec = defSec;
            if (_cfg.settings_sync_island_accent)
            {
                ColorThemeUtils.ResolveIslandThemeAccents(_cfg.theme_preset, 255, out chosenAcc, out chosenSec);
            }
            else if (preset == "Custom")
            {
                chosenAcc = ColorThemeUtils.ParseHex(_cfg.settings_custom_accent_hex, defAcc);
                chosenSec = ColorThemeUtils.Blend(chosenAcc, _isLightTheme ? Color.FromArgb(16, 185, 129) : Color.White, 0.28f);
            }

            // Guarantee readable contrast on light cards
            if (_isLightTheme && ColorThemeUtils.GetLuminance(chosenAcc) > 195.0)
                chosenAcc = ColorThemeUtils.Blend(chosenAcc, Color.FromArgb(20, 30, 55), 0.38f);
            if (_isLightTheme && ColorThemeUtils.GetLuminance(chosenSec) > 195.0)
                chosenSec = ColorThemeUtils.Blend(chosenSec, Color.FromArgb(16, 120, 85), 0.38f);

            ColAccent = chosenAcc;
            ColLime = chosenSec;
            ColAccentHover = _isLightTheme
                ? ColorThemeUtils.Blend(ColAccent, Color.Black, 0.14f)
                : ColorThemeUtils.Blend(ColAccent, Color.White, 0.16f);
            ColOnAccentText = ColorThemeUtils.GetLuminance(ColAccent) > 142.0
                ? Color.FromArgb(12, 13, 16)
                : Color.FromArgb(250, 251, 255);

            BackColor = ColChassis;
            ForeColor = ColTextMain;
            if (_inlineBox != null)
            {
                _inlineBox.BackColor = ColDeckInset;
                _inlineBox.ForeColor = ColAccent;
            }
        }

        private void PickColorInteractive(Color current, Action<Color> onChosen)
        {
            using (var cd = new ColorDialog())
            {
                cd.Color = current;
                cd.FullOpen = true;
                cd.AnyColor = true;
                if (cd.ShowDialog(this) == DialogResult.OK && onChosen != null)
                {
                    onChosen(cd.Color);
                    RefreshSettingsPalette();
                    ApplyLive();
                }
            }
        }

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
            RefreshSettingsPalette();

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
            else if (lq.Contains("эква") || lq.Contains("спектр") || lq.Contains("волн") || lq.Contains("зерк") || lq.Contains("визуал"))
            {
                _activeTab = 1; _inspectorSubMode = 2;
            }
            else if (lq.Contains("плеер") || lq.Contains("муз") || lq.Contains("облож") || lq.Contains("лайк"))
            {
                _activeTab = 1; _inspectorSubMode = 1;
            }
            else if (lq.Contains("скрин") || lq.Contains("закач") || lq.Contains("сон") || lq.Contains("watch") || lq.Contains("drop") || lq.Contains("батар") || lq.Contains("час"))
            {
                _activeTab = 1; _inspectorSubMode = 2;
            }
            else if (lq.Contains("видео") || lq.Contains("кино") || lq.Contains("youtube") || lq.Contains("vlc") || lq.Contains("монит") || lq.Contains("экран") || lq.Contains("игр") || lq.Contains("призр") || lq.Contains("микр") || lq.Contains("отступ") || lq.Contains("сдвиг"))
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
            if (Updater != null && Updater.State != _lastShownUpdState) { _lastShownUpdState = Updater.State; dirty = true; }

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
                Invalidate(new Rectangle(0, 0, Width, 322));
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
            RefreshSettingsPalette();
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
            ColorThemeUtils.ResolveIslandThemeAccents(_cfg.theme_preset, alpha, out c1, out c2);
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
            using (var pen = new Pen(filterHit ? ColAccent : (hovered ? ColBorderHi : ColBorder), 1f))
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
                ? (hov ? ColAccentHover : ColAccent)
                : (danger
                    ? (_isLightTheme ? (hov ? Color.FromArgb(254, 215, 215) : Color.FromArgb(254, 232, 232)) : (hov ? Color.FromArgb(52, 22, 24) : Color.FromArgb(28, 16, 18)))
                    : (hov ? ColDeckHover : ColDeckCard));
            Color border = primaryAccent
                ? ColAccentHover
                : (danger ? Color.FromArgb(145, 239, 68, 68) : (hov ? ColBorderHi : ColBorder));
            Color fg = primaryAccent
                ? ColOnAccentText
                : (danger ? (_isLightTheme ? Color.FromArgb(185, 28, 28) : Color.FromArgb(255, 105, 95)) : ColTextMain);

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
            using (var vb = new SolidBrush(ColWellBg))
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
            using (var penDimTick = new Pen(ColInkAlpha(42), 1f))
            using (var penActTick = new Pen(ColAccentAlpha(195), 1f))
            using (var penRail = new Pen(ColInkAlpha(35), 1.5f))
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
            using (var sb = new SolidBrush(ColWellBg))
            using (var sPen = new Pen(ColBorder, 1f))
            {
                g.FillPath(sb, sp);
                g.DrawPath(sPen, sp);
            }

            if (!value)
            {
                using (GraphicsPath op = SharpRoundRect(offRc, 2.5f))
                using (var ob = new SolidBrush(_isLightTheme ? Color.FromArgb(203, 210, 224) : Color.FromArgb(38, 40, 48)))
                    g.FillPath(ob, op);
            }
            else
            {
                using (GraphicsPath op = SharpRoundRect(onRc, 2.5f))
                using (var ob = new SolidBrush(ColAccentAlpha(48)))
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
                using (var cb = new SolidBrush(selected ? ColAccentAlpha(38) : (hov ? ColDeckHover : ColDeckInset)))
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
                ? (hov ? ColDeckHover : ColDeckCard)
                : (hov ? ColDeckHover : ColDeckInset);
            Color border = filterHit
                ? ColAccent
                : (inspectorActive
                    ? ColBorderHi
                    : (enabled ? ColAccentAlpha(115) : (hov ? ColBorderHi : ColBorder)));

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

            Color ledCol = enabled ? ColLime : ColInkAlpha(55);
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
                using (var gb = new SolidBrush(inspectorActive ? ColAccent : (gHov ? ColDeckHover : ColWellBg)))
                using (var gPen = new Pen(inspectorActive ? ColAccent : ColBorder, 1f))
                using (var gt = new SolidBrush(inspectorActive ? ColOnAccentText : ColTextMuted))
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
            using (var sb = new SolidBrush(ColWellBg))
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

            using (var gridPen = new Pen(ColInkAlpha(22), 1f))
            using (var targetPen = new Pen(Color.FromArgb(85, ColLime.R, ColLime.G, ColLime.B), 1f) { DashStyle = DashStyle.Dot })
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
            using (var pb = new SolidBrush(ColWellBg))
            using (var pPen = new Pen(hov ? ColAccent : ColBorderHi, 1f))
            {
                g.FillPath(pb, pp);
                g.DrawPath(pPen, pp);
            }

            // Engineering grid + Center X=0 & Top Y=0 guides
            float midX = padRc.X + padRc.Width * 0.5f;
            using (var axisPen = new Pen(ColInkAlpha(32), 1f) { DashStyle = DashStyle.Dot })
            using (var bezelPen = new Pen(ColAccentAlpha(110), 1.5f))
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
            using (var guidePen = new Pen(ColAccentAlpha(80), 1f))
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
            using (var barBr = new SolidBrush(ColHeaderBar))
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
                g.DrawString("DYNAMIC ISLAND // KINETIC STAGE & DECK", _fConsoleBrand, titleBr, new RectangleF(28, 0, 290, 34), _sfLeft);
                g.DrawString("WHEEL ±1 • CLICK = INPUT", _fMonoTiny, dimBr, new RectangleF(318, 0, 158, 34), _sfLeft);
            }

            bool updAvail = (Updater != null && Updater.IsUpdateAvailable);
            bool updBusy = (Updater != null && (Updater.IsChecking || Updater.IsDownloading));
            string updLabel = updAvail ? ("● ДОСТУПНО v" + Updater.LatestVersionStr) : (updBusy ? "⟳ ПРОВЕРКА..." : "↻ ОБНОВЛЕНИЯ");
            DrawHardwareButton(g, new RectangleF(Width - 400, 6, 140, 22), updLabel, updAvail, false, () =>
            {
                if (ManualUpdateCheckRequested != null) ManualUpdateCheckRequested();
            });
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
                using (var dotBr = new SolidBrush(ColInkAlpha(26)))
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
                using (var axisPen = new Pen(ColInkAlpha(28), 1f) { DashStyle = DashStyle.Dot })
                using (var bezelPen = new Pen(ColInkAlpha(64), 1f))
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
                using (var dimPen = new Pen(ColAccentAlpha(85), 1f))
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
                using (var mb = new SolidBrush(act ? ColAccentAlpha(42) : (mHov ? ColDeckHover : ColDeckCard)))
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

                using (var wBr = new SolidBrush(Color.FromArgb(245, 246, 250)))
                using (var mBr = new SolidBrush(Color.FromArgb(165, 170, 185)))
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
                using (var txtBr = new SolidBrush(Color.FromArgb(245, 246, 250)))
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
                using (var clkBr = new SolidBrush(Color.FromArgb(245, 246, 250)))
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

                using (var tBr = new SolidBrush(Color.FromArgb(245, 246, 250)))
                    g.DrawString(_stageMode == StagePreviewMode.Media ? "KINETIC AUDIO • 60HZ" : "YANDEX / SPOTIFY",
                        _fMonoTiny, tBr, new RectangleF(cov.Right + 6f, pillRc.Y, pillRc.Width - (cov.Right - pillRc.X) - 52f, pillRc.Height), _sfLeft);
            }
            else if (_cfg.show_battery && _stageMode == StagePreviewMode.Idle)
            {
                using (var batBr = new SolidBrush(ColLime))
                    g.DrawString("98%", _fMonoTiny, batBr, new RectangleF(leftCur + 2f, pillRc.Y, 34f, pillRc.Height), _sfLeft);
            }

            // Live Equalizer Multi-Style Preview on right of Compact Stage Capsule
            if (_cfg.eq_style != "Выкл" && _cfg.eq_style != "Off")
            {
                Color eq1 = tc1, eq2 = tc2;
                if (_cfg.eq_color_mode == "Один цвет")
                {
                    eq1 = ColorThemeUtils.ParseHex(_cfg.eq_custom_color_hex, tc1);
                    eq2 = eq1;
                }
                else if (_cfg.eq_color_mode == "Обложка трека")
                {
                    eq1 = Color.FromArgb(255, 244, 114, 182);
                    eq2 = Color.FromArgb(255, 56, 189, 248);
                }

                int bars = Math.Max(4, Math.Min(12, _cfg.eq_bar_count));
                float sens = Math.Max(0.4f, Math.Min(2.2f, _cfg.eq_sensitivity_pct / 100f));
                float bw = bars > 8 ? 2.0f : 2.5f;
                float gap = bars > 8 ? 1.7f : 2.2f;
                float totalW = bars * bw + (bars - 1) * gap;
                float sx = pillRc.Right - 11f - totalW;
                float cy = pillRc.Y + pillRc.Height * 0.5f;
                float maxH = Math.Min(18f, pillRc.Height - 9f);
                string style = _cfg.eq_style ?? "Столбики";

                if (style == "Волна")
                {
                    PointF[] wPts = new PointF[bars + 2];
                    for (int i = 0; i < wPts.Length; i++)
                    {
                        float t = i / (float)(wPts.Length - 1);
                        float env = (float)Math.Sin(t * Math.PI);
                        float v = (_stageMode == StagePreviewMode.Idle ? 0.12f : 0.45f * sens) * env * (float)Math.Sin(_stageWavePhase * 1.4f + i * 0.95f);
                        wPts[i] = new PointF(sx + totalW * t, cy + Math.Max(-maxH * 0.5f, Math.Min(maxH * 0.5f, v * maxH)));
                    }
                    using (var wPen2 = new Pen(Color.FromArgb(115, eq2.R, eq2.G, eq2.B), 2.4f) { StartCap = LineCap.Round, EndCap = LineCap.Round })
                    using (var wPen1 = new Pen(eq1, 1.5f) { StartCap = LineCap.Round, EndCap = LineCap.Round })
                    {
                        g.DrawCurve(wPen2, wPts, 0.45f);
                        g.DrawCurve(wPen1, wPts, 0.45f);
                    }
                }
                else
                {
                    for (int i = 0; i < bars; i++)
                    {
                        float t = bars > 1 ? (i / (float)(bars - 1)) : 0f;
                        Color bc = ColorThemeUtils.Blend(eq1, eq2, t);
                        float raw = (_stageMode == StagePreviewMode.Idle)
                            ? 0.22f
                            : Math.Min(1f, (0.22f + 0.62f * Math.Abs((float)Math.Sin(_stageWavePhase + i * 0.82f))) * sens);
                        float bx = sx + i * (bw + gap);
                        using (var br = new SolidBrush(bc))
                        {
                            if (style == "Точки")
                            {
                                float dSz = bw + 1.0f;
                                float bounce = (raw - 0.35f) * (maxH * 0.42f);
                                g.FillEllipse(br, bx - 0.5f, cy - bounce - dSz * 0.5f, dSz, dSz);
                                using (var dimBr = new SolidBrush(Color.FromArgb(80, bc.R, bc.G, bc.B)))
                                    g.FillEllipse(dimBr, bx - 0.2f, cy + maxH * 0.28f, bw, bw);
                            }
                            else if (style == "Зеркальный")
                            {
                                float sym = 1f - Math.Abs(t * 2f - 1f) * 0.45f;
                                float halfH = Math.Max(1.6f, maxH * 0.5f * raw * sym);
                                g.FillRectangle(br, bx, cy - halfH - 0.6f, bw, halfH);
                                using (var refl = new SolidBrush(Color.FromArgb(165, bc.R, bc.G, bc.B)))
                                    g.FillRectangle(refl, bx, cy + 0.6f, bw, halfH);
                            }
                            else // Столбики (Bars — bottom-anchored classic spectrum)
                            {
                                float bh = Math.Max(2.8f, maxH * raw);
                                g.FillRectangle(br, bx, cy + maxH * 0.48f - bh, bw, bh);
                            }
                        }
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
                using (var tb = new SolidBrush(active ? ColAccentAlpha(36) : (hov ? ColDeckHover : Color.Transparent)))
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
                using (var sb = new SolidBrush(act ? ColAccentAlpha(40) : ColDeckInset))
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
                DrawSegmentedToggle(g, new RectangleF(sx, iy, iw, 28f),
                    "ОБЛОЖКА ТРЕКА В КОМПАКТЕ", _cfg.show_compact_cover, v => _cfg.show_compact_cover = v);
                iy += 33f;
                DrawSegmentedToggle(g, new RectangleF(sx, iy, iw, 28f),
                    "ПРИОРИТЕТ ЯНДЕКС / SPOTIFY", _cfg.prefer_music_players, v => _cfg.prefer_music_players = v);
                iy += 33f;
                DrawSegmentedToggle(g, new RectangleF(sx, iy, iw, 28f),
                    "СКРЫВАТЬ ОСТРОВ ПРИ ПРОСМОТРЕ ВИДЕО", _cfg.hide_on_video_playback, v => _cfg.hide_on_video_playback = v);
                iy += 33f;
                DrawSegmentedToggle(g, new RectangleF(sx, iy, iw, 28f),
                    "LIVE-БАННЕР СМЕНЫ ТРЕКА", _cfg.enable_live_activities, v => _cfg.enable_live_activities = v);
                iy += 35f;

                string[] eqVals = { "5 Полос", "7 Полос", "Выкл" };
                DrawChipSelectorMatrix(g, new RectangleF(sx, iy, iw, 46f),
                    "РЕЖИМ АУДИО-ЭКВАЛАЙЗЕРА", eqVals, eqVals, _cfg.eq_style ?? "5 Полос", 3,
                    val => _cfg.eq_style = val);
                iy += 52f;

                var defP = InstalledMusicPlayerDiscovery.ResolveDefaultPlayer(_cfg);
                string defLbl = defP != null ? ("♫ АВТОЗАПУСК PLAY: " + defP.DisplayName + " ▾") : "♫ АВТОЗАПУСК PLAY: СПРАШИВАТЬ (МЕНЮ) ▾";
                var defBtnRc = new RectangleF(sx, iy, iw, 26f);
                DrawHardwareButton(g, defBtnRc, defLbl, defP != null, false, () =>
                {
                    var menu = new ContextMenuStrip();
                    var askItem = menu.Items.Add("Спрашивать при нажатии Play / Skip (показывать список)", null, (ms, me) =>
                    {
                        _cfg.default_music_player_id = "";
                        ApplyLive();
                    });
                    if (string.IsNullOrEmpty(_cfg.default_music_player_id)) askItem.Font = new Font(menu.Font, FontStyle.Bold);
                    menu.Items.Add(new ToolStripSeparator());
                    var found = InstalledMusicPlayerDiscovery.DiscoverPlayers(_cfg, true);
                    foreach (var fp in found)
                    {
                        var capP = fp;
                        string pTitle = (capP.IsLastUsed ? "★ " : "") + capP.DisplayName;
                        var mi = menu.Items.Add(pTitle, null, (ms, me) =>
                        {
                            _cfg.default_music_player_id = capP.Id;
                            ApplyLive();
                        });
                        if (string.Equals(_cfg.default_music_player_id, capP.Id, StringComparison.OrdinalIgnoreCase))
                            mi.Font = new Font(menu.Font, FontStyle.Bold);
                    }
                    menu.Show(PointToScreen(new Point((int)defBtnRc.X, (int)defBtnRc.Bottom + 2)));
                });
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
            DrawBentoCell(g, modeCell, "03.B", "ИГРОВОЙ РЕЖИМ, ВИДЕО И ПРИЗРАК", "D3D • VIDEO • TOP");

            float mx = modeCell.X + 10f;
            float mw = modeCell.Width - 20f;
            float my = modeCell.Y + 30f;

            string[] fsVals = { "Неоновая телеметрия", "Авто-скрытие", "Только по клику" };
            string[] fsShort = { "Неон-телеметрия", "Авто-скрытие", "По клику" };
            DrawChipSelectorMatrix(g, new RectangleF(mx, my, mw, 46f),
                "ДЕТЕКТОР ПОЛНОЭКРАННЫХ ИГР (D3D FULLSCREEN)", fsVals, fsShort,
                _cfg.fullscreen_mode ?? "Неоновая телеметрия", 3,
                val => { _cfg.fullscreen_mode = val; _cfg.hide_in_fullscreen = (val == "Авто-скрытие"); });
            my += 52f;

            DrawSegmentedToggle(g, new RectangleF(mx, my, mw, 28f),
                "СКРЫВАТЬ ОСТРОВ ПРИ ВОСПРОИЗВЕДЕНИИ ВИДЕО", _cfg.hide_on_video_playback, v => _cfg.hide_on_video_playback = v);
            my += 33f;

            DrawSegmentedToggle(g, new RectangleF(mx, my, mw, 28f),
                "РЕЖИМ «ПРИЗРАКА» (20% OPACITY • ПРОПУСК КЛИКОВ)", _cfg.enable_ghost_mode, v => _cfg.enable_ghost_mode = v);
            my += 33f;

            DrawSegmentedToggle(g, new RectangleF(mx, my, mw, 28f),
                "ИНДИКАТОР И 1-CLICK МЬЮТ МИКРОФОНА", _cfg.enable_mic_indicator, v => _cfg.enable_mic_indicator = v);
            my += 33f;

            DrawSegmentedToggle(g, new RectangleF(mx, my, mw, 28f),
                "ПРИОРИТЕТ ЯНДЕКС МУЗЫКИ / SPOTIFY", _cfg.prefer_music_players, v => _cfg.prefer_music_players = v);
            my += 34f;

            var hkBox = new RectangleF(mx, my, mw, 90f);
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
                "АВТО-УСТАНОВКА ОБНОВЛЕНИЙ (MAIN/RELEASE)", _cfg.auto_update_enabled, v => _cfg.auto_update_enabled = v);

            DrawHardwareButton(g, new RectangleF(bx, sysCell.Y + 81f, bw, 20f),
                "↻ ОБНОВИТЬ ПРОГРАММУ — ПРОВЕРИТЬ СЕЙЧАС", true, false, () =>
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
        private int _lastGameRenderTick = 0;
        private bool _gameRenderPending = false;
        private int _fsLastTrueTick = 0;
        private int _lastTopmostAssertTick = 0;
        private volatile bool _pendingUpdatePopup = false;
        private UpdateDialogForm _updateDialog = null;
        private System.Windows.Forms.Timer _relayoutTimer = null;
        private string _lastLikeSyncSig = "";
        private int _lastLikeSyncTick = 0;
        private int _likeActionGeneration = 0;
        private int _initialLikeSyncAttempts = 0;
        private volatile bool _livePlayerLikedKnown = false;
        private string _livePlayerLikedTrackKey = "";
        private volatile bool _livePlayerLikedValue = false;

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

        private readonly uint _ourProcessId = (uint)Process.GetCurrentProcess().Id;
        private int _ownDialogOrMenuDepth = 0;
        private int _lastZOrderCheckTick = 0;
        private bool _wasSuppressedTopMost = false;
        private volatile bool _isVideoPlayingActive = false;
        private volatile bool _hiddenByVideo = false;
        private int _pressedDockIndex = -1;
        private PinnedAppItem _pressedDockItem = null;
        private Point _dockMouseDownPt;
        private float _dockGrabOffsetX = 17f;
        private float _dockDragCurrentX = 0f;
        private float _dockRowStartX = 0f;
        private int _dockVisibleCount = 0;
        private bool _isDraggingDockApp = false;
        private bool _dockSettleAnimating = false;
        private readonly Dictionary<string, float> _dockVisualX = new Dictionary<string, float>(StringComparer.OrdinalIgnoreCase);

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
                if (_lctrlHeld || _isResizingWithLCtrl || _expandedPage == 1 || _isDraggingDockApp || Volatile.Read(ref _ownDialogOrMenuDepth) > 0) return;
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
            try { BeginInvoke((MethodInvoker)delegate { ScheduleDisplayRelayout(); }); }
            catch { }
        }

        // Games switch display modes in bursts (WM_DISPLAYCHANGE storm): coalesce into ONE relayout after 450ms of silence
        private void ScheduleDisplayRelayout()
        {
            if (_disposing || !IsHandleCreated) return;
            if (_relayoutTimer == null)
            {
                _relayoutTimer = new System.Windows.Forms.Timer { Interval = 450 };
                _relayoutTimer.Tick += delegate
                {
                    _relayoutTimer.Stop();
                    try
                    {
                        EnsureCachedFonts();
                        PositionOnMonitor(_config.monitor_index);
                        WakeRenderLoop();
                        RenderLayeredFrame();
                    }
                    catch { }
                };
            }
            _relayoutTimer.Stop();
            _relayoutTimer.Start();
        }

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            _disposing = true;
            Program.CleanupTrayIconSafe();
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

        // The overlay must NEVER take focus (games, Alt+Tab, startup over a fullscreen app)
        protected override bool ShowWithoutActivation { get { return true; } }

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
            _updater.UpdateAvailableDetected += () => { _pendingUpdatePopup = true; WakeRenderLoop(); };
            _updater.RestartRequested += () =>
            {
                try { BeginInvoke((MethodInvoker)delegate { Application.Exit(); }); }
                catch { Application.Exit(); }
            };
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

                if (_mode == IslandMode.Expanded && _expandedPage == 1 && IsHandleCreated && !_isFullscreenAppActive)
                {
                    try
                    {
                        var src = _streamEngine.GetActiveSource(Handle);
                        if (src != null && hwnd == src.Hwnd)
                        {
                            _dwmOverlay.EnsureStreamSourceAlive(Handle, src, _streamEngine.CropModeIndex);
                            if (evType == NativeWin32.EVENT_SYSTEM_MINIMIZESTART)
                            {
                                ThreadPool.QueueUserWorkItem(_ =>
                                {
                                    Thread.Sleep(115);
                                    if (!_disposing && IsHandleCreated)
                                    {
                                        try { BeginInvoke((MethodInvoker)delegate {
                                            if (_mode == IslandMode.Expanded && _expandedPage == 1)
                                                _dwmOverlay.EnsureStreamSourceAlive(Handle, _streamEngine.GetActiveSource(Handle), _streamEngine.CropModeIndex);
                                        }); } catch { }
                                    }
                                });
                            }
                        }
                    }
                    catch { }
                }

                _bgWakeEvent.Set();
                WakeRenderLoop();
                if ((evType == NativeWin32.EVENT_SYSTEM_FOREGROUND || evType == NativeWin32.EVENT_SYSTEM_MINIMIZEEND) &&
                    hwnd != Handle && !_disposing && IsHandleCreated)
                {
                    AssertIslandTopMostSafe(false, true);
                    ThreadPool.QueueUserWorkItem(_ =>
                    {
                        Thread.Sleep(95);
                        if (!_disposing && IsHandleCreated)
                        {
                            try { BeginInvoke((MethodInvoker)delegate { AssertIslandTopMostSafe(false, false); }); } catch { }
                        }
                    });
                }
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
            if (NativeWin32.WM_DI_SHUTDOWN != 0 && (uint)m.Msg == NativeWin32.WM_DI_SHUTDOWN)
            {
                m.Result = new IntPtr(1);
                Program.CleanupTrayIconSafe();
                try { Close(); } catch { }
                try { Application.Exit(); } catch { }
                return;
            }
            if (NativeWin32.WM_DI_ACTIVATE != 0 && (uint)m.Msg == NativeWin32.WM_DI_ACTIVATE)
            {
                m.Result = new IntPtr(0x4449);
                _ghostModeActive = false;
                _ghostUnlockedByUser = true;
                _ghostOpacityFactor = 1.0f;
                _ghostLiftOffsetY = 0.0f;
                _hiddenByFullscreen = false;
                _hiddenByVideo = false;
                _fullscreenAutoHideFactor = 1.0f;
                _fullscreenLiftOffsetY = 0.0f;
                ExpandIsland();
                AssertIslandTopMostSafe(true, false);
                _collapseTimer.Interval = 3200;
                _collapseTimer.Start();
                RenderLayeredFrame();
                return;
            }
            if (m.Msg == NativeWin32.WM_MOUSEACTIVATE)
            {
                m.Result = (IntPtr)NativeWin32.MA_NOACTIVATE;
                return;
            }
            if (m.Msg == NativeWin32.WM_WINDOWPOSCHANGING && m.LParam != IntPtr.Zero)
            {
                try
                {
                    var wp = (NativeWin32.WINDOWPOS)Marshal.PtrToStructure(m.LParam, typeof(NativeWin32.WINDOWPOS));
                    wp.flags |= NativeWin32.SWP_NOACTIVATE;
                    if (!ShouldSuppressTopMostAssertion() && (wp.flags & NativeWin32.SWP_NOZORDER) == 0)
                    {
                        bool allowAboveStream = (_expandedPage == 1 && _dwmOverlay != null && _dwmOverlay.IsHandleCreated && wp.hwndInsertAfter == _dwmOverlay.Handle);
                        if (!allowAboveStream && wp.hwndInsertAfter != NativeWin32.HWND_TOPMOST)
                        {
                            wp.hwndInsertAfter = NativeWin32.HWND_TOPMOST;
                        }
                    }
                    Marshal.StructureToPtr(wp, m.LParam, false);
                }
                catch { }
            }
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
                ScheduleDisplayRelayout();
            }
            base.WndProc(ref m);
        }

        private bool ShouldSuppressTopMostAssertion()
        {
            if (_disposing || _isSystemSuspended || !IsHandleCreated) return true;
            if (Volatile.Read(ref _ownDialogOrMenuDepth) > 0) return true;
            if (_updateDialog != null && !_updateDialog.IsDisposed && _updateDialog.Visible) return true;
            if (NativeWin32.IsAltTabHeld() || SystemSensors.IsTransientShellForeground()) return true;
            if (_hiddenByFullscreen || _hiddenByVideo) return true;
            if (_config.hide_on_video_playback && _isVideoPlayingActive && _expandedPage != 1 && !_ghostUnlockedByUser) return true;
            if (_isFullscreenAppActive && _expandedPage != 1)
            {
                // Respect fullscreen game exceptions: never pop above game in Auto-Hide or Click-Only modes
                if (_config.hide_in_fullscreen || _config.fullscreen_mode == "Авто-скрытие" || _config.fullscreen_mode == "Только по клику")
                    return true;
            }
            return false;
        }

        private bool IsIslandBelowAnyExternalWindow()
        {
            if (!IsHandleCreated) return false;
            try
            {
                IntPtr cur = NativeWin32.GetWindow(Handle, NativeWin32.GW_HWNDPREV);
                int steps = 0;
                while (cur != IntPtr.Zero && steps < 45)
                {
                    steps++;
                    if (NativeWin32.IsWindowVisible(cur) && !NativeWin32.IsIconic(cur))
                    {
                        uint pid;
                        NativeWin32.GetWindowThreadProcessId(cur, out pid);
                        if (pid != 0 && pid != _ourProcessId)
                        {
                            NativeWin32.RECT r;
                            if (NativeWin32.GetWindowRect(cur, out r) && (r.Right - r.Left) > 4 && (r.Bottom - r.Top) > 4)
                                return true;
                        }
                    }
                    cur = NativeWin32.GetWindow(cur, NativeWin32.GW_HWNDPREV);
                }
            }
            catch { }
            return false;
        }

        private void AssertIslandTopMostSafe(bool force, bool asyncCrossThread = false)
        {
            if (ShouldSuppressTopMostAssertion()) return;
            if (!force && !IsIslandBelowAnyExternalWindow()) return;

            uint flags = NativeWin32.SWP_NOMOVE | NativeWin32.SWP_NOSIZE | NativeWin32.SWP_NOACTIVATE | NativeWin32.SWP_NOOWNERZORDER;
            if (asyncCrossThread) flags |= NativeWin32.SWP_ASYNCWINDOWPOS;

            NativeWin32.SetWindowPos(Handle, NativeWin32.HWND_TOPMOST, 0, 0, 0, 0, flags);
            if (_expandedPage == 1 && _dwmOverlay != null && _dwmOverlay.IsHandleCreated && _dwmOverlay.Visible)
            {
                NativeWin32.SetWindowPos(_dwmOverlay.Handle, NativeWin32.HWND_TOPMOST, 0, 0, 0, 0, flags);
            }
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
            if (InvokeRequired)
            {
                try { BeginInvoke((MethodInvoker)delegate { WakeRenderLoop(); }); } catch { }
                return;
            }
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
            if (_expandedPage == 0 && !_isFullscreenAppActive)
            {
                MediaSessionSnapshot snapNow;
                lock (_stateLock) { snapNow = _currentMedia; }
                PlayerFavoriteEngine.RequestSyncStateAsync(snapNow, Volatile.Read(ref _likeActionGeneration), ApplySyncedFavoriteResult);
            }
            if (_mode == IslandMode.Expanded && !_isAnimating) return;
            _mode = IslandMode.Expanded;
            StartSpringTransition(_config.expanded_width, _config.expanded_height, 1.0f, _config.anim_duration_ms, true);
        }

        public void CollapseToIdle()
        {
            if (_isHovered || _lctrlHeld || _isResizingWithLCtrl || _expandedPage == 1 || _isDraggingDockApp || Volatile.Read(ref _ownDialogOrMenuDepth) > 0) return;
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
            // GAME THROTTLE: while a fullscreen game runs and the island is idle, repaint <= 4 Hz (keeps DWM Independent Flip + FPS intact)
            bool gameThrottle = _isFullscreenAppActive && _mode == IslandMode.Idle && !_isAnimating && !_isHovered &&
                                _expandedPage != 1 && !_isResizingWithLCtrl && !_lctrlHeld;

            // MODULE 1 & VIDEO HIDE: Smooth Fullscreen / Video Playback Auto-Hide fade & upward slide
            bool wantAutoHideFs = _isFullscreenAppActive && (_expandedPage != 1) &&
                                  (_config.fullscreen_mode == "Авто-скрытие" || _config.hide_in_fullscreen);
            bool wantAutoHideVideo = _config.hide_on_video_playback && _isVideoPlayingActive &&
                                     (_expandedPage != 1) && !_ghostUnlockedByUser;
            if (wantAutoHideVideo && _mode == IslandMode.Expanded && !_isHovered && !_isDraggingDockApp && Volatile.Read(ref _ownDialogOrMenuDepth) == 0)
            {
                CollapseToIdle();
            }
            bool wantAutoHideAny = wantAutoHideFs || wantAutoHideVideo;
            float targetFsAlpha = wantAutoHideAny ? 0.0f : 1.0f;
            float targetFsLift = wantAutoHideAny ? -42.0f : 0.0f;
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
            bool fullyHiddenFade = (_fullscreenAutoHideFactor < 0.03f);
            _hiddenByFullscreen = wantAutoHideFs && fullyHiddenFade;
            _hiddenByVideo = wantAutoHideVideo && fullyHiddenFade;

            // STRICT Z-ORDER GUARD: Keep Island strictly HWND_TOPMOST above normal & topmost windows, respecting exceptions & zero focus steal
            bool nowSuppressedZ = ShouldSuppressTopMostAssertion();
            int nowZTick = Environment.TickCount;
            if (_wasSuppressedTopMost && !nowSuppressedZ)
            {
                _lastZOrderCheckTick = nowZTick;
                AssertIslandTopMostSafe(true, false);
            }
            else if (!nowSuppressedZ && unchecked(nowZTick - _lastZOrderCheckTick) >= 380)
            {
                _lastZOrderCheckTick = nowZTick;
                AssertIslandTopMostSafe(false, false);
            }
            _wasSuppressedTopMost = nowSuppressedZ;

            // MODULE 3: Ghost Mode & Smart Cursor Reaction (35px proximity buffer, 20% opacity, -5px lift, click-through until 500ms center dwell / Alt+Space / LCtrl/LAlt)
            if (_config.enable_ghost_mode && _mode != IslandMode.Expanded && !_lctrlHeld && !_isResizingWithLCtrl && !wantAutoHideAny)
            {
                RectangleF basePill = new RectangleF((CanvasW - pill.Width) / 2f, Math.Max(0, _config.top_margin), pill.Width, pill.Height);
                RectangleF proxZone = RectangleF.Inflate(basePill, 35f, 35f);
                RectangleF centerCore = RectangleF.Inflate(basePill, -basePill.Width * 0.16f, 0f);

                if (proxZone.Contains(localPt.X, localPt.Y))
                {
                    if (NativeWin32.IsModifierHeldForGhostExit() && !_isFullscreenAppActive)
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
                                if (!_isFullscreenAppActive) ExpandIsland();
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

            bool inside = !fullyHiddenFade && !wantAutoHideAny && !_ghostModeActive && hitBox.Contains(localPt.X, localPt.Y);

            SetWin32ClickThrough((!inside || _ghostModeActive || wantAutoHideAny) && !_isResizingWithLCtrl);

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

            if (!gameThrottle && _bgEngine.AdvanceAnimationFrame())
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

            if (!gameThrottle && (_mode != IslandMode.Expanded || _expandedPage != 1) && _config.eq_style != "Выкл" && _config.eq_style != "Off")
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

            if (_pendingUpdatePopup)
            {
                if (!_isFullscreenAppActive && _updateDialog == null && !_updater.DismissedForSession && _updater.State == UpdateCheckState.Available)
                {
                    _pendingUpdatePopup = false;
                    try { ShowUpdateDialog(false, null); } catch { }
                }
                else if (_updater.DismissedForSession || (_updater.State != UpdateCheckState.Available && _updater.State != UpdateCheckState.Checking))
                {
                    _pendingUpdatePopup = false;
                }
            }

            if (gameThrottle)
            {
                int tNow = Environment.TickCount;
                if (needsRedraw) _gameRenderPending = true;
                if (_gameRenderPending && unchecked(tNow - _lastGameRenderTick) >= 250)
                {
                    needsRedraw = true;
                    _gameRenderPending = false;
                    _lastGameRenderTick = tNow;
                }
                else
                {
                    needsRedraw = false;
                }
            }

            if (_dockSettleAnimating)
                needsRedraw = true;

            if ((_hiddenByFullscreen || _hiddenByVideo) && !_isAnimating)
            {
                if (_frameTimer.Interval != 380) _frameTimer.Interval = 380;
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
            if (gameThrottle) desiredInterval = 120;
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
                            bool definiteNonGameFg;
                            bool fsRaw = SystemSensors.IsForegroundFullscreenGame(Handle, screens[idx], out definiteNonGameFg);
                            int nowFs = Environment.TickCount;
                            bool fsNow;
                            if (fsRaw)
                            {
                                _fsLastTrueTick = nowFs;
                                fsNow = true;
                            }
                            else if (_isFullscreenAppActive &&
                                     (NativeWin32.IsAltTabHeld() || SystemSensors.IsTransientShellForeground() ||
                                      (!definiteNonGameFg && unchecked(nowFs - _fsLastTrueTick) < 450)))
                            {
                                // Keep game state across Alt+Tab switcher overlay & brief D3D mode switches,
                                // but drop IMMEDIATELY once user lands on desktop, browser, video player, or minimizes the game!
                                fsNow = true;
                            }
                            else
                            {
                                fsNow = false;
                            }
                            bool fsChanged = (fsNow != _isFullscreenAppActive);
                            _isFullscreenAppActive = fsNow;
                            // Page 2 (Screen Share Viewer) ALWAYS stays visible and strictly expanded even over fullscreen games/apps!
                            _hiddenByFullscreen = (_expandedPage != 1) && (_config.hide_in_fullscreen || _config.fullscreen_mode == "Авто-скрытие") && fsNow;
                            if (!ShouldSuppressTopMostAssertion() && (fsChanged || unchecked(nowFs - _lastTopmostAssertTick) > 2500))
                            {
                                _lastTopmostAssertTick = nowFs;
                                AssertIslandTopMostSafe(fsChanged, true);
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
                        InstalledMusicPlayerDiscovery.RememberFromActiveSession(_config, activeSnap);

                        // 2-Way Favorite Sync (Single Source of Truth: immediate on startup/track change + up to 2 cold retries + every 2.0s while Expanded)
                        int nowSync = Environment.TickCount;
                        string activeTrackKey = PlayerFavoriteEngine.BuildTrackKey(activeSnap);
                        bool trackChangedForLike = !string.Equals(sig, _lastLikeSyncSig, StringComparison.Ordinal);
                        if (trackChangedForLike) _initialLikeSyncAttempts = 0;
                        bool needsInitialSync = (!_livePlayerLikedKnown || !string.Equals(_livePlayerLikedTrackKey, activeTrackKey, StringComparison.OrdinalIgnoreCase)) && _initialLikeSyncAttempts < 2;
                        bool periodicSync = !_isFullscreenAppActive && (
                            (_mode == IslandMode.Expanded && _expandedPage == 0 && unchecked(nowSync - _lastLikeSyncTick) >= 2000) ||
                            (needsInitialSync && unchecked(nowSync - _lastLikeSyncTick) >= 2200));
                        if ((trackChangedForLike || periodicSync) && !string.IsNullOrEmpty(activeTrackKey))
                        {
                            _lastLikeSyncSig = sig;
                            _lastLikeSyncTick = nowSync;
                            _initialLikeSyncAttempts++;
                            PlayerFavoriteEngine.RequestSyncStateAsync(activeSnap, Volatile.Read(ref _likeActionGeneration), ApplySyncedFavoriteResult, trackChangedForLike);
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
                    _microTelemetry.SampleAsyncIfNeeded(cpuVal, _isFullscreenAppActive);

                    // MODULE 2: CoreAudio Microphone Active Session & Mute detection
                    if (_config.enable_mic_indicator)
                    {
                        _audio.RefreshMicrophoneState();
                    }

                    // Universal Video Playback Auto-Hide detection (SMTC MediaPlaybackType.Video + Local Video Players + Fullscreen Browser Video)
                    bool videoPlayingNow = false;
                    if (_config.hide_on_video_playback && IsHandleCreated)
                    {
                        Screen[] vScreens = Screen.AllScreens;
                        Screen monScr = vScreens.Length > 0 ? vScreens[Math.Max(0, Math.Min(_config.monitor_index, vScreens.Length - 1))] : Screen.PrimaryScreen;
                        videoPlayingNow = SystemSensors.IsUniversalVideoPlaying(Handle, monScr, _audio);
                    }
                    else
                    {
                        videoPlayingNow = false;
                    }
                    if (videoPlayingNow != _isVideoPlayingActive)
                    {
                        _isVideoPlayingActive = videoPlayingNow;
                        if (!videoPlayingNow) _ghostUnlockedByUser = false;
                        _bgStateDirty = true;
                        WakeRenderLoop();
                    }

                    // MODULE 6: Unobtrusive main/release/version.json check (4.5s after start, then every 6 hours, deferred during fullscreen games)
                    long nowTick = Environment.TickCount;
                    if (!_isFullscreenAppActive)
                    {
                        if (!_initialUpdateCheckDone && (nowTick - _startupTick >= 4500))
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

                int waitMs = (_config.hide_on_video_playback && (_isVideoPlayingActive || _hiddenByVideo)) ? 600 : (_hiddenByFullscreen ? 3500 : ((_isFullscreenAppActive && _mode == IslandMode.Idle) ? 1500 : ((_mode == IslandMode.Idle && !_watchdog.IsWatchdogArmed && !_currentMedia.IsPlaying && !_config.hide_on_video_playback) ? 1800 : 900)));
                _bgWakeEvent.WaitOne(waitMs);
            }
            if (cpuCounter != null) { try { cpuCounter.Dispose(); } catch { } }
        }

        private static NetworkInterface[] _cachedNics = null;
        private static int _cachedNicsTick = 0;

        private static void SampleNetworkTotals(out long recv, out long sent)
        {
            recv = 0; sent = 0;
            try
            {
                int now = Environment.TickCount;
                if (_cachedNics == null || unchecked(now - _cachedNicsTick) > 30000)
                {
                    var list = new List<NetworkInterface>();
                    foreach (NetworkInterface ni in NetworkInterface.GetAllNetworkInterfaces())
                    {
                        if (ni.OperationalStatus == OperationalStatus.Up && ni.NetworkInterfaceType != NetworkInterfaceType.Loopback)
                            list.Add(ni);
                    }
                    _cachedNics = list.ToArray();
                    _cachedNicsTick = now;
                }
                foreach (NetworkInterface ni in _cachedNics)
                {
                    try
                    {
                        IPv4InterfaceStatistics stats = ni.GetIPv4Statistics();
                        recv += stats.BytesReceived;
                        sent += stats.BytesSent;
                    }
                    catch { }
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

                bool hasLiveSession = (_allPlayers != null && _allPlayers.Count > 0 && _currentMedia != null && _currentMedia.NativeSession != null);

                if (_rcPlayerBadge.Contains(pt))
                {
                    if (!hasLiveSession)
                    {
                        HandleInactiveMediaButtonOrPrompt("play", PointToScreen(new Point((int)_rcPlayerBadge.X, (int)_rcPlayerBadge.Bottom + 4)), true);
                    }
                    else if (_allPlayers.Count > 1)
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
                    if (!hasLiveSession)
                    {
                        HandleInactiveMediaButtonOrPrompt("play", PointToScreen(new Point((int)_rcSongClickZone.X + 20, (int)_rcSongClickZone.Bottom + 4)), true);
                        return;
                    }
                    MultiPlayerMediaController.ActivatePlayerWindowAsync(_currentMedia);
                    _isHovered = false;
                    CollapseToIdle();
                    return;
                }

                if (_rcLikeBtn.Contains(pt)) { ToggleCurrentTrackFavorite(); return; }
                if (_rcPrevBtn.Contains(pt))
                {
                    if (!hasLiveSession)
                    {
                        HandleInactiveMediaButtonOrPrompt("prev", PointToScreen(new Point((int)_rcPrevBtn.X, (int)_rcPrevBtn.Bottom + 6)), false);
                        return;
                    }
                    MultiPlayerMediaController.ExecuteTransportCommandAsync(_currentMedia, "prev");
                    return;
                }
                if (_rcPlayBtn.Contains(pt))
                {
                    if (!hasLiveSession)
                    {
                        HandleInactiveMediaButtonOrPrompt("play", PointToScreen(new Point((int)_rcPlayBtn.X - 40, (int)_rcPlayBtn.Bottom + 6)), false);
                        return;
                    }
                    MultiPlayerMediaController.ExecuteTransportCommandAsync(_currentMedia, "play_pause");
                    _currentMedia.IsPlaying = !_currentMedia.IsPlaying;
                    _currentMedia.PositionSec = _currentMedia.GetLivePositionSec();
                    _currentMedia.SampledAtUtc = DateTime.UtcNow;
                    RenderLayeredFrame();
                    return;
                }
                if (_rcNextBtn.Contains(pt))
                {
                    if (!hasLiveSession)
                    {
                        HandleInactiveMediaButtonOrPrompt("next", PointToScreen(new Point((int)_rcNextBtn.X - 80, (int)_rcNextBtn.Bottom + 6)), false);
                        return;
                    }
                    MultiPlayerMediaController.ExecuteTransportCommandAsync(_currentMedia, "next");
                    return;
                }

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

                for (int di = 0; di < _dockRects.Count; di++)
                {
                    if (_dockRects[di].Key.Contains(pt))
                    {
                        _pressedDockIndex = di;
                        _pressedDockItem = _dockRects[di].Value;
                        _dockMouseDownPt = e.Location;
                        _dockGrabOffsetX = pt.X - _dockRects[di].Key.X;
                        _dockDragCurrentX = _dockRects[di].Key.X;
                        _isDraggingDockApp = false;
                        _collapseTimer.Stop();
                        RenderLayeredFrame();
                        return;
                    }
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
            if (_pressedDockIndex >= 0 && (e.Button & MouseButtons.Left) == MouseButtons.Left)
            {
                int dx = Math.Abs(e.X - _dockMouseDownPt.X);
                int dy = Math.Abs(e.Y - _dockMouseDownPt.Y);
                if (!_isDraggingDockApp && (dx >= 6 || dy >= 6))
                {
                    _isDraggingDockApp = true;
                }
                if (_isDraggingDockApp)
                {
                    _dockDragCurrentX = e.X - _dockGrabOffsetX;
                    int visCount = Math.Min(_dockVisibleCount, _config.pinned_apps.Count);
                    if (visCount > 1 && _pressedDockIndex >= 0 && _pressedDockIndex < visCount)
                    {
                        int targetSlot = Math.Max(0, Math.Min(visCount - 1, (int)Math.Round((_dockDragCurrentX - _dockRowStartX) / 40f)));
                        if (targetSlot != _pressedDockIndex)
                        {
                            PinnedAppItem moving = _config.pinned_apps[_pressedDockIndex];
                            _config.pinned_apps.RemoveAt(_pressedDockIndex);
                            _config.pinned_apps.Insert(targetSlot, moving);
                            _pressedDockIndex = targetSlot;
                        }
                    }
                    WakeRenderLoop();
                    RenderLayeredFrame();
                    return;
                }
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

            if (_pressedDockIndex >= 0)
            {
                bool wasDraggingDock = _isDraggingDockApp;
                PinnedAppItem clickedApp = _pressedDockItem;
                _isDraggingDockApp = false;
                _pressedDockIndex = -1;
                _pressedDockItem = null;

                if (wasDraggingDock)
                {
                    ConfigManager.Save(_config);
                    WakeRenderLoop();
                    RenderLayeredFrame();
                    return;
                }
                else if (e.Button == MouseButtons.Left && clickedApp != null)
                {
                    if (Math.Abs(e.X - _dockMouseDownPt.X) < 6 && Math.Abs(e.Y - _dockMouseDownPt.Y) < 6)
                    {
                        LaunchPinnedApp(clickedApp);
                    }
                    RenderLayeredFrame();
                    return;
                }
            }
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
            ShowTrackedContextMenu(menu, screenPt);
        }

        private void ShowTrackedContextMenu(ContextMenuStrip menu, Point screenPt)
        {
            if (menu == null) return;
            Interlocked.Increment(ref _ownDialogOrMenuDepth);
            _collapseTimer.Stop();
            menu.Closed += (s, e) =>
            {
                if (Interlocked.Decrement(ref _ownDialogOrMenuDepth) < 0)
                    Interlocked.Exchange(ref _ownDialogOrMenuDepth, 0);
                if (!_isHovered && _mode == IslandMode.Expanded && _expandedPage != 1)
                {
                    _collapseTimer.Interval = Math.Max(180, _config.collapse_delay_ms);
                    _collapseTimer.Start();
                }
                AssertIslandTopMostSafe(false, false);
            };
            menu.Show(screenPt);
        }

        private void LaunchDiscoveredPlayerWithFeedback(DiscoveredMusicPlayer p, string cmd)
        {
            if (p == null) return;
            _currentMedia.DisplayAppName = p.DisplayName;
            _currentMedia.Title = "Запуск " + p.DisplayName + "...";
            _currentMedia.Artist = "Автостарт воспроизведения";
            RenderLayeredFrame();
            InstalledMusicPlayerDiscovery.LaunchPlayerAndAutoPlayAsync(p, _config, _audio, cmd, () =>
            {
                _bgStateDirty = true;
                _bgWakeEvent.Set();
                WakeRenderLoop();
            });
        }

        private void HandleInactiveMediaButtonOrPrompt(string cmd, Point screenPt, bool forceShowMenu)
        {
            if (!forceShowMenu)
            {
                var defP = InstalledMusicPlayerDiscovery.ResolveDefaultPlayer(_config);
                if (defP != null)
                {
                    LaunchDiscoveredPlayerWithFeedback(defP, cmd);
                    return;
                }
            }

            var discovered = InstalledMusicPlayerDiscovery.DiscoverPlayers(_config, false);
            var menu = new ContextMenuStrip
            {
                BackColor = Color.FromArgb(19, 21, 28),
                ForeColor = Color.FromArgb(244, 245, 250),
                ShowImageMargin = true
            };

            var hdr = menu.Items.Add("♫ Нет активной сессии — открыть плеер:");
            hdr.Enabled = false;
            menu.Items.Add(new ToolStripSeparator());

            bool addedLocal = false;
            bool addedWebSep = false;
            foreach (var dp in discovered)
            {
                var cap = dp;
                if (cap.IsWebFallback && addedLocal && !addedWebSep)
                {
                    addedWebSep = true;
                    menu.Items.Add(new ToolStripSeparator());
                }
                if (!cap.IsWebFallback) addedLocal = true;

                string title = (cap.IsLastUsed ? "★ " : "▶ ") + cap.DisplayName + (cap.IsLastUsed ? " (Недавний)" : "");
                Bitmap ico = !string.IsNullOrEmpty(cap.IconSourcePath) ? GetAppIconBitmap(cap.IconSourcePath) : null;
                var mi = menu.Items.Add(title, ico, (s, e) => LaunchDiscoveredPlayerWithFeedback(cap, cmd));
                if (cap.IsLastUsed) mi.Font = new Font(menu.Font, FontStyle.Bold);
            }

            menu.Items.Add(new ToolStripSeparator());
            var defSub = new ToolStripMenuItem("⚙ Запускать по умолчанию без вопроса");
            defSub.DropDown.BackColor = Color.FromArgb(19, 21, 28);
            defSub.DropDown.ForeColor = Color.FromArgb(244, 245, 250);
            var askOpt = defSub.DropDownItems.Add("Всегда показывать это меню", null, (s, e) =>
            {
                _config.default_music_player_id = "";
                ConfigManager.Save(_config);
            });
            if (string.IsNullOrEmpty(_config.default_music_player_id))
                askOpt.Font = new Font(menu.Font, FontStyle.Bold);
            defSub.DropDownItems.Add(new ToolStripSeparator());

            foreach (var dp in discovered)
            {
                var cap = dp;
                var dmi = defSub.DropDownItems.Add(cap.DisplayName + " (запустить сейчас)", null, (s, e) =>
                {
                    _config.default_music_player_id = cap.Id;
                    ConfigManager.Save(_config);
                    LaunchDiscoveredPlayerWithFeedback(cap, cmd);
                });
                if (string.Equals(_config.default_music_player_id, cap.Id, StringComparison.OrdinalIgnoreCase))
                    dmi.Font = new Font(menu.Font, FontStyle.Bold);
            }
            menu.Items.Add(defSub);

            ShowTrackedContextMenu(menu, screenPt);
        }

        private void ShowPlayerSelectionMenu(Point screenPt)
        {
            if (_allPlayers == null || _allPlayers.Count == 0 || _currentMedia == null || _currentMedia.NativeSession == null)
            {
                HandleInactiveMediaButtonOrPrompt("play", screenPt, true);
                return;
            }

            var menu = new ContextMenuStrip();
            menu.Items.Add("↗ Открыть окно текущего плеера (" + _currentMedia.DisplayAppName + ")", null, (s, e) =>
            {
                MultiPlayerMediaController.ActivatePlayerWindowAsync(_currentMedia);
            });
            menu.Items.Add(new ToolStripSeparator());

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
            menu.Items.Add(new ToolStripSeparator());
            menu.Items.Add("♫ Запустить другой плеер...", null, (s, e) => HandleInactiveMediaButtonOrPrompt("play", screenPt, true));
            ShowTrackedContextMenu(menu, screenPt);
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
            string key = CurrentTrackFavoriteKey();
            if (string.IsNullOrWhiteSpace(key) || key == " — " || (_currentMedia.Title ?? "") == "Не воспроизводится")
                return false;

            if (_livePlayerLikedKnown && string.Equals(_livePlayerLikedTrackKey, key, StringComparison.OrdinalIgnoreCase))
                return _livePlayerLikedValue;

            bool isSpotify, isYandex, isBrowser;
            PlayerFavoriteSupport sup = PlayerFavoriteEngine.ClassifyPlayerSupport(_currentMedia, out isSpotify, out isYandex, out isBrowser);
            if (sup != PlayerFavoriteSupport.Unsupported)
                return false;

            if (_config.liked_tracks == null) return false;
            foreach (string s in _config.liked_tracks)
                if (string.Equals(s, key, StringComparison.OrdinalIgnoreCase)) return true;
            return false;
        }

        private void ApplySyncedFavoriteResult(PlayerFavoriteSyncResult res)
        {
            if (res == null || string.IsNullOrWhiteSpace(res.TrackKey) || _disposing) return;
            if (res.Generation < Volatile.Read(ref _likeActionGeneration)) return;

            if (InvokeRequired)
            {
                try { BeginInvoke((MethodInvoker)delegate { ApplySyncedFavoriteResult(res); }); } catch { }
                return;
            }
            if (res.Generation < Volatile.Read(ref _likeActionGeneration)) return;

            bool uiChanged = false;
            if (res.SyncedFromPlayer || !res.PlayerSupported)
            {
                if (!_livePlayerLikedKnown ||
                    !string.Equals(_livePlayerLikedTrackKey, res.TrackKey, StringComparison.OrdinalIgnoreCase) ||
                    _livePlayerLikedValue != res.IsLiked)
                {
                    uiChanged = true;
                }
                _livePlayerLikedTrackKey = res.TrackKey;
                _livePlayerLikedValue = res.IsLiked;
                _livePlayerLikedKnown = true;
            }

            if (_config.liked_tracks == null) _config.liked_tracks = new List<string>();
            bool cfgChanged = false;
            if (res.IsLiked)
            {
                bool exists = false;
                foreach (string s in _config.liked_tracks)
                    if (string.Equals(s, res.TrackKey, StringComparison.OrdinalIgnoreCase)) { exists = true; break; }
                if (!exists) { _config.liked_tracks.Add(res.TrackKey); cfgChanged = true; }
            }
            else
            {
                for (int i = _config.liked_tracks.Count - 1; i >= 0; i--)
                    if (string.Equals(_config.liked_tracks[i], res.TrackKey, StringComparison.OrdinalIgnoreCase))
                    { _config.liked_tracks.RemoveAt(i); cfgChanged = true; }
            }

            if (cfgChanged) ConfigManager.Save(_config);
            if (uiChanged || cfgChanged) RenderLayeredFrame();
        }

        private void ToggleCurrentTrackFavorite()
        {
            if (_config.liked_tracks == null) _config.liked_tracks = new List<string>();
            string key = CurrentTrackFavoriteKey();
            if (string.IsNullOrWhiteSpace(key) || key == " — " || (_currentMedia.Title ?? "") == "Не воспроизводится") return;

            bool wasLiked = IsCurrentTrackLiked();
            bool targetLiked = !wasLiked;
            int gen = Interlocked.Increment(ref _likeActionGeneration);

            _livePlayerLikedTrackKey = key;
            _livePlayerLikedValue = targetLiked;
            _livePlayerLikedKnown = true;

            if (!targetLiked)
            {
                for (int i = _config.liked_tracks.Count - 1; i >= 0; i--)
                    if (string.Equals(_config.liked_tracks[i], key, StringComparison.OrdinalIgnoreCase))
                        _config.liked_tracks.RemoveAt(i);
            }
            else
            {
                bool exists = false;
                foreach (string s in _config.liked_tracks)
                    if (string.Equals(s, key, StringComparison.OrdinalIgnoreCase)) { exists = true; break; }
                if (!exists) _config.liked_tracks.Add(key);
            }

            ConfigManager.Save(_config);
            RenderLayeredFrame();
            PlayerFavoriteEngine.RequestToggleFavoriteAsync(_currentMedia, targetLiked, gen, ApplySyncedFavoriteResult);
        }

        private void PromptAddAppViaExplorer()
        {
            Interlocked.Increment(ref _ownDialogOrMenuDepth);
            try
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
            finally
            {
                if (Interlocked.Decrement(ref _ownDialogOrMenuDepth) < 0)
                    Interlocked.Exchange(ref _ownDialogOrMenuDepth, 0);
                AssertIslandTopMostSafe(true, false);
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
            int idx = _config.pinned_apps.FindIndex(x => string.Equals(x.target_path, item.target_path, StringComparison.OrdinalIgnoreCase));
            if (idx > 0)
            {
                menu.Items.Add("← Переместить левее", null, (s, e) =>
                {
                    int cur = _config.pinned_apps.FindIndex(x => string.Equals(x.target_path, item.target_path, StringComparison.OrdinalIgnoreCase));
                    if (cur > 0)
                    {
                        var mv = _config.pinned_apps[cur];
                        _config.pinned_apps.RemoveAt(cur);
                        _config.pinned_apps.Insert(cur - 1, mv);
                        ConfigManager.Save(_config);
                        RenderLayeredFrame();
                    }
                });
            }
            if (idx >= 0 && idx < _config.pinned_apps.Count - 1)
            {
                menu.Items.Add("→ Переместить правее", null, (s, e) =>
                {
                    int cur = _config.pinned_apps.FindIndex(x => string.Equals(x.target_path, item.target_path, StringComparison.OrdinalIgnoreCase));
                    if (cur >= 0 && cur < _config.pinned_apps.Count - 1)
                    {
                        var mv = _config.pinned_apps[cur];
                        _config.pinned_apps.RemoveAt(cur);
                        _config.pinned_apps.Insert(cur + 1, mv);
                        ConfigManager.Save(_config);
                        RenderLayeredFrame();
                    }
                });
            }
            menu.Items.Add(new ToolStripSeparator());
            menu.Items.Add("Открепить из дока", null, (s, e) =>
            {
                _config.pinned_apps.RemoveAll(x => string.Equals(x.target_path, item.target_path, StringComparison.OrdinalIgnoreCase));
                ConfigManager.Save(_config);
                RenderLayeredFrame();
            });
            ShowTrackedContextMenu(menu, screenPt);
        }

        public void ShowUpdateDialog(bool manual, IWin32Window owner)
        {
            if (_updateDialog != null && !_updateDialog.IsDisposed)
            {
                if (manual && !_updater.IsDownloading && !_updater.IsChecking)
                    _updater.CheckForUpdatesAsync(_config, true);
                try { _updateDialog.Activate(); } catch { }
                return;
            }
            if (manual && !_updater.IsDownloading && !_updater.IsChecking)
                _updater.CheckForUpdatesAsync(_config, true);

            var dlg = new UpdateDialogForm(_updater, _config, !manual);
            _updateDialog = dlg;
            dlg.FormClosed += delegate
            {
                _updateDialog = null;
                AssertIslandTopMostSafe(true, false);
            };
            if (manual)
            {
                Interlocked.Increment(ref _ownDialogOrMenuDepth);
                try { dlg.ShowDialog(owner ?? this); }
                finally
                {
                    if (Interlocked.Decrement(ref _ownDialogOrMenuDepth) < 0)
                        Interlocked.Exchange(ref _ownDialogOrMenuDepth, 0);
                    AssertIslandTopMostSafe(true, false);
                }
            }
            else
            {
                dlg.Show();
            }
        }

        public void OpenSettingsDialog()
        {
            Interlocked.Increment(ref _ownDialogOrMenuDepth);
            try
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
                        _bgWakeEvent.Set();
                        WakeRenderLoop();
                        RenderLayeredFrame();
                    };
                    dlg.Updater = _updater;
                    dlg.ManualUpdateCheckRequested += () => ShowUpdateDialog(true, dlg);
                    dlg.ShowDialog(this);
                }
            }
            finally
            {
                if (Interlocked.Decrement(ref _ownDialogOrMenuDepth) < 0)
                    Interlocked.Exchange(ref _ownDialogOrMenuDepth, 0);
                AssertIslandTopMostSafe(true, false);
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
            if (!_hiddenByFullscreen && !_hiddenByVideo)
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

            IntPtr screenDc = IntPtr.Zero; // hdcDst = NULL: no GetDC(NULL) on the desktop DC every frame (stalls under D3D fullscreen)
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
                if (screenDc != IntPtr.Zero) NativeWin32.ReleaseDC(IntPtr.Zero, screenDc);
            }

            // Sync hardware DWM 60/144 FPS thumbnail overlay for Page 2
            if (!_hiddenByFullscreen && !_hiddenByVideo && _mode == IslandMode.Expanded && _expandedPage == 1 && _curProg > 0.72f)
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
                    float thumbH = Math.Max(64f, galleryH - 12f);
                    float idealCardW = Math.Min(162f, Math.Max(110f, thumbH * 1.38f));
                    float slot4W = (innerW - 16f) / 4f - 6f;
                    float slot3W = (innerW - 16f) / 3f - 6f;
                    float thumbW = (thumbH > 105f && slot3W <= 172f) ? Math.Min(idealCardW, slot3W) : Math.Min(idealCardW, Math.Max(96f, slot4W));
                    int maxShow = Math.Min(items.Count, Math.Max(2, (int)((innerW - 10f) / (thumbW + 8f))));

                    InterpolationMode prevInterp = g.InterpolationMode;
                    PixelOffsetMode prevPixOff = g.PixelOffsetMode;
                    CompositingQuality prevCompQ = g.CompositingQuality;
                    g.InterpolationMode = InterpolationMode.HighQualityBicubic;
                    g.PixelOffsetMode = PixelOffsetMode.HighQuality;
                    g.CompositingQuality = CompositingQuality.HighQuality;

                    using (var thumbWrap = new ImageAttributes())
                    using (var thumbMatte = new SolidBrush(Color.FromArgb((int)(alpha * 0.88f), 7, 9, 14)))
                    {
                        thumbWrap.SetWrapMode(WrapMode.TileFlipXY);
                        for (int i = 0; i < maxShow; i++)
                        {
                            var it = items[i];
                            var tRc = new RectangleF(thumbX, thumbY, thumbW, thumbH);
                            _vaultThumbRects.Add(new KeyValuePair<RectangleF, VaultCaptureItem>(tRc, it));

                            using (GraphicsPath tp = CreateRoundedRectPath(tRc, 8f))
                            {
                                g.FillPath(thumbMatte, tp);
                                if (it.Thumbnail != null && it.Thumbnail.Width > 0 && it.Thumbnail.Height > 0)
                                {
                                    Region prevC = g.Clip;
                                    g.SetClip(tp);
                                    try
                                    {
                                        float bw = it.Thumbnail.Width;
                                        float bh = it.Thumbnail.Height;
                                        float imgAspect = bw / Math.Max(1f, bh);
                                        float cardAspect = tRc.Width / Math.Max(1f, tRc.Height);
                                        float ratioDiff = imgAspect / Math.Max(0.05f, cardAspect);

                                        // Aspect-preserving render: NEVER stretch!
                                        // Close aspect ratios (0.68x .. 1.48x) use crisp center-crop (aspect-fill);
                                        // wide/tall snippets use centered aspect-fit so 100% of text/UI stays sharp & readable.
                                        RectangleF dstRect = tRc;
                                        RectangleF srcRect = new RectangleF(0, 0, bw, bh);
                                        if (ratioDiff >= 0.68f && ratioDiff <= 1.48f)
                                        {
                                            if (imgAspect > cardAspect)
                                            {
                                                float cropW = bh * cardAspect;
                                                srcRect = new RectangleF((bw - cropW) * 0.5f, 0f, cropW, bh);
                                            }
                                            else
                                            {
                                                float cropH = bw / cardAspect;
                                                srcRect = new RectangleF(0f, (bh - cropH) * 0.5f, bw, cropH);
                                            }
                                        }
                                        else
                                        {
                                            float fitScale = Math.Min(tRc.Width / bw, tRc.Height / bh);
                                            float dw = Math.Max(1f, bw * fitScale);
                                            float dh = Math.Max(1f, bh * fitScale);
                                            dstRect = new RectangleF(tRc.X + (tRc.Width - dw) * 0.5f, tRc.Y + (tRc.Height - dh) * 0.5f, dw, dh);
                                        }

                                        PointF[] dstPts = new PointF[]
                                        {
                                            new PointF(dstRect.Left, dstRect.Top),
                                            new PointF(dstRect.Right, dstRect.Top),
                                            new PointF(dstRect.Left, dstRect.Bottom)
                                        };
                                        g.DrawImage(it.Thumbnail, dstPts, srcRect, GraphicsUnit.Pixel, thumbWrap);
                                    }
                                    catch { }
                                    finally { g.Clip = prevC; }
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
                    g.InterpolationMode = prevInterp;
                    g.PixelOffsetMode = prevPixOff;
                    g.CompositingQuality = prevCompQ;
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

                // ROW 3: Minimalist macOS Squircle App Dock (Drag-and-Drop Reorderable) + [ + ] Add Program Button Locked at End
                _dockRects.Clear();
                float dockX = padX;
                _dockRowStartX = padX;
                int maxSlots = Math.Max(4, (int)(innerW / 40f));
                int maxIcons = Math.Min(_config.pinned_apps.Count, maxSlots - 1);
                _dockVisibleCount = maxIcons;
                bool anyDockSettling = false;
                PinnedAppItem draggedDockObj = null;

                for (int i = 0; i < maxIcons; i++)
                {
                    PinnedAppItem item = _config.pinned_apps[i];
                    float slotX = padX + i * 40f;
                    var slotHitRc = new RectangleF(slotX, curY, 34f, 34f);
                    _dockRects.Add(new KeyValuePair<RectangleF, PinnedAppItem>(slotHitRc, item));

                    if (_isDraggingDockApp && i == _pressedDockIndex)
                    {
                        draggedDockObj = item;
                        _dockVisualX[item.target_path ?? i.ToString()] = slotX;
                        using (GraphicsPath phPath = CreateRoundedRectPath(slotHitRc, 10f))
                        using (var phBr = new SolidBrush(Color.FromArgb((int)(alpha * 0.14f), acc1.R, acc1.G, acc1.B)))
                        using (var phPen = new Pen(Color.FromArgb((int)(alpha * 0.55f), acc1.R, acc1.G, acc1.B), 1.15f) { DashStyle = DashStyle.Dash })
                        {
                            g.FillPath(phBr, phPath);
                            g.DrawPath(phPen, phPath);
                        }
                        dockX += 40f;
                        continue;
                    }

                    string vKey = item.target_path ?? i.ToString();
                    float drawX = slotX;
                    float prevX;
                    if (_dockVisualX.TryGetValue(vKey, out prevX))
                    {
                        if (Math.Abs(prevX - slotX) > 0.45f)
                        {
                            drawX = prevX + (slotX - prevX) * 0.38f;
                            anyDockSettling = true;
                        }
                        else
                        {
                            drawX = slotX;
                        }
                    }
                    _dockVisualX[vKey] = drawX;

                    var btnRc = new RectangleF(drawX, curY, 34f, 34f);
                    using (GraphicsPath dp = CreateRoundedRectPath(btnRc, 10f))
                    {
                        g.FillPath(brBento, dp);
                        g.DrawPath(penBento, dp);
                    }

                    if (!_isDraggingDockApp)
                        ApplyMicroInteraction(g, btnRc, 10f, alpha, acc1);
                    Bitmap icoBmp = GetAppIconBitmap(item.target_path);
                    if (icoBmp != null)
                        SafeDrawImage(g, icoBmp, btnRc.X + 6f, btnRc.Y + 6f, 22f, 22f);
                    else
                        DrawContrastText(g, item.name.Substring(0, Math.Min(2, item.name.Length)), _fontSmall, colWhite, btnRc, _sfCenter);

                    dockX += 40f;
                }
                _dockSettleAnimating = anyDockSettling;

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

                if (_isDraggingDockApp && draggedDockObj != null)
                {
                    float minDragX = padX - 4f;
                    float maxDragX = padX + Math.Max(0, maxIcons - 1) * 40f + 4f;
                    float floatX = Math.Max(minDragX, Math.Min(maxDragX, _dockDragCurrentX));
                    var dragRc = new RectangleF(floatX - 1f, curY - 3.5f, 36f, 36f);
                    var dragShadowRc = new RectangleF(dragRc.X + 1.5f, dragRc.Y + 3f, 36f, 36f);
                    using (GraphicsPath dsp = CreateRoundedRectPath(dragShadowRc, 11f))
                    using (var dsb = new SolidBrush(Color.FromArgb((int)(alpha * 0.55f), 2, 4, 10)))
                        g.FillPath(dsb, dsp);

                    using (GraphicsPath dgp = CreateRoundedRectPath(dragRc, 11f))
                    using (var dgb = new SolidBrush(Color.FromArgb(Math.Min(255, (int)(alpha * 0.94f)), 24, 28, 40)))
                    using (var dgPen = new Pen(acc1, 1.5f))
                    {
                        g.FillPath(dgb, dgp);
                        g.DrawPath(dgPen, dgp);
                    }
                    Bitmap dragIco = GetAppIconBitmap(draggedDockObj.target_path);
                    if (dragIco != null)
                        SafeDrawImage(g, dragIco, dragRc.X + 7f, dragRc.Y + 7f, 22f, 22f);
                    else
                        DrawContrastText(g, draggedDockObj.name.Substring(0, Math.Min(2, draggedDockObj.name.Length)), _fontSmall, colWhite, dragRc, _sfCenter);
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
        private const string SingleInstanceMutexName = @"Local\DynamicIslandWin_Standalone_SingleInstance";
        private static Mutex _singleInstanceMutex;
        private static NotifyIcon _trayIcon;
        private static Icon _extractedTrayIcon;
        private static int _trayCleanedUp = 0;

        public static void CleanupTrayIconSafe()
        {
            if (Interlocked.Exchange(ref _trayCleanedUp, 1) != 0) return;
            var tray = _trayIcon;
            _trayIcon = null;
            if (tray != null)
            {
                try { tray.Visible = false; } catch { }
                try { if (tray.ContextMenuStrip != null) tray.ContextMenuStrip.Dispose(); } catch { }
                try { tray.Dispose(); } catch { }
            }
            var ico = _extractedTrayIcon;
            _extractedTrayIcon = null;
            if (ico != null)
            {
                try { ico.Dispose(); } catch { }
            }
        }

        private static void RequestGracefulShutdownOfOtherInstances(int waitBudgetMs)
        {
            int myPid = Process.GetCurrentProcess().Id;
            Process[] others = null;
            try { others = Process.GetProcessesByName("DynamicIslandWin"); } catch { }
            if (others == null || others.Length == 0) return;

            bool anyOther = false;
            foreach (Process p in others)
            {
                if (p.Id != myPid) { anyOther = true; break; }
            }
            if (!anyOther) return;

            // 1) Broadcast WM_DI_SHUTDOWN so running instance executes CleanupTrayIconSafe() -> Shell_NotifyIcon(NIM_DELETE)!
            if (NativeWin32.WM_DI_SHUTDOWN != 0)
            {
                IntPtr res;
                NativeWin32.SendMessageTimeout(NativeWin32.HWND_BROADCAST, NativeWin32.WM_DI_SHUTDOWN, IntPtr.Zero, IntPtr.Zero, 0x0002 /* SMTO_ABORTIFHUNG */, 450, out res);

                var targetPids = new HashSet<uint>();
                foreach (Process p in others) if (p.Id != myPid) targetPids.Add((uint)p.Id);
                NativeWin32.EnumWindows((hWnd, lParam) =>
                {
                    uint wPid;
                    NativeWin32.GetWindowThreadProcessId(hWnd, out wPid);
                    if (targetPids.Contains(wPid))
                        NativeWin32.PostMessage(hWnd, NativeWin32.WM_DI_SHUTDOWN, IntPtr.Zero, IntPtr.Zero);
                    return true;
                }, IntPtr.Zero);
            }

            // 2) Wait for graceful exit; only terminate if completely hung past deadline
            int elapsed = 0;
            while (elapsed < waitBudgetMs)
            {
                bool stillAlive = false;
                foreach (Process p in others)
                {
                    if (p.Id == myPid) continue;
                    try { if (!p.HasExited) { stillAlive = true; break; } } catch { }
                }
                if (!stillAlive) return;
                Thread.Sleep(60);
                elapsed += 60;
            }

            foreach (Process p in others)
            {
                if (p.Id == myPid) continue;
                try
                {
                    if (!p.HasExited)
                    {
                        p.CloseMainWindow();
                        if (!p.WaitForExit(300)) { p.Kill(); p.WaitForExit(500); }
                    }
                }
                catch { }
            }
        }

        [STAThread]
        private static void Main(string[] args)
        {
            bool hasProbe = false;
            bool hasShutdownOnly = false;
            bool hasReplace = false;
            if (args != null)
            {
                foreach (string a in args)
                {
                    string low = (a ?? "").Trim().ToLowerInvariant();
                    if (low == "--probe-players") hasProbe = true;
                    else if (low == "--shutdown" || low == "--quit") hasShutdownOnly = true;
                    else if (low == "--replace" || low == "--restart") hasReplace = true;
                }
            }

            if (hasProbe)
            {
                string report = PlayerFavoriteEngine.RunDiagnosticProbe();
                try { File.WriteAllText(Path.Combine(Path.GetTempPath(), "di_player_probe.txt"), report, Encoding.UTF8); } catch { }
                return;
            }

            if (hasShutdownOnly)
            {
                RequestGracefulShutdownOfOtherInstances(1800);
                return;
            }

            if (hasReplace)
            {
                RequestGracefulShutdownOfOtherInstances(1800);
            }

            // Strict Desktop-Scoped Single-Instance Enforcement with Live Window Handshake (self-heals headless/other-desktop ghosts!)
            string desktopMutexName = SingleInstanceMutexName + "_" + NativeWin32.GetCurrentDesktopNameSafe();
            bool ownsMutex = false;
            try
            {
                _singleInstanceMutex = new Mutex(false, desktopMutexName);
                try
                {
                    ownsMutex = _singleInstanceMutex.WaitOne(hasReplace ? 1500 : 0, false);
                }
                catch (AbandonedMutexException)
                {
                    ownsMutex = true;
                }
            }
            catch { ownsMutex = true; }

            if (!ownsMutex)
            {
                bool activatedLiveWindow = false;
                if (NativeWin32.WM_DI_ACTIVATE != 0)
                {
                    int selfPid = Process.GetCurrentProcess().Id;
                    var otherPids = new HashSet<uint>();
                    try
                    {
                        foreach (Process p in Process.GetProcessesByName("DynamicIslandWin"))
                            if (p.Id != selfPid) otherPids.Add((uint)p.Id);
                    }
                    catch { }

                    if (otherPids.Count > 0)
                    {
                        NativeWin32.EnumWindows((hWnd, lParam) =>
                        {
                            uint wPid;
                            NativeWin32.GetWindowThreadProcessId(hWnd, out wPid);
                            if (otherPids.Contains(wPid) && NativeWin32.IsWindowVisible(hWnd))
                            {
                                IntPtr ack;
                                if (NativeWin32.SendMessageTimeout(hWnd, NativeWin32.WM_DI_ACTIVATE, IntPtr.Zero, IntPtr.Zero, 0x0002, 450, out ack) != IntPtr.Zero &&
                                    ack.ToInt64() == 0x4449)
                                {
                                    activatedLiveWindow = true;
                                    return false;
                                }
                            }
                            return true;
                        }, IntPtr.Zero);
                    }
                }

                if (activatedLiveWindow)
                    return;

                // Stale/zombie/other-desktop process held the mutex without a live window on this desktop -> self-heal!
                RequestGracefulShutdownOfOtherInstances(1500);
                try
                {
                    ownsMutex = _singleInstanceMutex.WaitOne(1200, false);
                }
                catch (AbandonedMutexException) { ownsMutex = true; }
                catch { ownsMutex = true; }
                if (!ownsMutex) return;
            }

            Application.SetUnhandledExceptionMode(UnhandledExceptionMode.CatchException);
            Application.ThreadException += (s, exArgs) => { };
            AppDomain.CurrentDomain.UnhandledException += (s, exArgs) => { CleanupTrayIconSafe(); };
            AppDomain.CurrentDomain.ProcessExit += (s, e) => { CleanupTrayIconSafe(); };
            Application.ApplicationExit += (s, e) => { CleanupTrayIconSafe(); };
            SystemEvents.SessionEnding += (s, e) => { CleanupTrayIconSafe(); };

            NativeWin32.EnablePerMonitorDpiV2();
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);

            try
            {
                var mainForm = new DynamicIslandOverlayForm();
                _trayIcon = new NotifyIcon();
                try
                {
                    _extractedTrayIcon = Icon.ExtractAssociatedIcon(Application.ExecutablePath);
                    _trayIcon.Icon = _extractedTrayIcon ?? SystemIcons.Application;
                }
                catch
                {
                    _trayIcon.Icon = SystemIcons.Application;
                }
                _trayIcon.Text = "Dynamic Island for Windows";

                var trayMenu = new ContextMenuStrip();
                trayMenu.Items.Add("Раскрыть островок", null, (s, e) => mainForm.ExpandIsland());
                trayMenu.Items.Add("Пульт настроек (Kinetic Stage & Deck)...", null, (s, e) => mainForm.OpenSettingsDialog());
                trayMenu.Items.Add("Обновить программу (проверить обновления)...", null, (s, e) => mainForm.ShowUpdateDialog(true, null));
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
                    CleanupTrayIconSafe();
                    Application.Exit();
                });

                _trayIcon.ContextMenuStrip = trayMenu;
                _trayIcon.MouseClick += (s, e) =>
                {
                    if (e.Button == MouseButtons.Left)
                        mainForm.ExpandIsland();
                };
                _trayIcon.Visible = true;

                Application.Run(mainForm);
            }
            finally
            {
                CleanupTrayIconSafe();
                if (ownsMutex && _singleInstanceMutex != null)
                {
                    try { _singleInstanceMutex.ReleaseMutex(); } catch { }
                    try { _singleInstanceMutex.Dispose(); } catch { }
                }
            }
        }
    }
}