// Language: C# (.NET 4.8 / Apple macOS Sequoia Single-File Setup with Native Buttons, IShellLinkW & Zone Unblocker)
// File: dynamic_island/NativeInstaller.cs -> Compiles to DynamicIslandWin_Setup.exe

using System;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Text;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.ComTypes;
using System.Text;
using System.Threading;
using System.Windows.Forms;
using Microsoft.Win32;

[assembly: AssemblyTitle("Dynamic Island for Windows — Single-File Setup")]
[assembly: AssemblyDescription("100% Self-Contained Single-File Installer for Dynamic Island Windows 10/11")]
[assembly: AssemblyProduct("Dynamic Island Studio Setup")]
[assembly: AssemblyCompany("DynamicIslandWin")]
[assembly: AssemblyVersion("3.1.2.0")]
[assembly: AssemblyFileVersion("3.1.2.0")]

namespace DynamicIslandSetup
{
    internal static class NativeWin32
    {
        [DllImport("gdi32.dll")]
        public static extern IntPtr CreateRoundRectRgn(int x1, int y1, int x2, int y2, int cx, int cy);

        [DllImport("shell32.dll")] public static extern void SHChangeNotify(int wEventId, uint uFlags, IntPtr dwItem1, IntPtr dwItem2);
        [DllImport("user32.dll")]
        public static extern bool SetProcessDPIAware();

        [DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool DeleteFileW(string lpFileName);

        // Native Shell32 IShellLinkW COM Interface (Works even when WScript.Shell is disabled!)
        [ComImport, Guid("00021401-0000-0000-C000-000000000046")]
        internal class ShellLinkComObject { }

        [ComImport, InterfaceType(ComInterfaceType.InterfaceIsIUnknown), Guid("000214F9-0000-0000-C000-000000000046")]
        internal interface IShellLinkW
        {
            void GetPath([Out, MarshalAs(UnmanagedType.LPWStr)] StringBuilder pszFile, int cchMaxPath, IntPtr pfd, int fFlags);
            void GetIDList(out IntPtr ppidl);
            void SetIDList(IntPtr pidl);
            void GetDescription([Out, MarshalAs(UnmanagedType.LPWStr)] StringBuilder pszName, int cchMaxName);
            void SetDescription([MarshalAs(UnmanagedType.LPWStr)] string pszName);
            void GetWorkingDirectory([Out, MarshalAs(UnmanagedType.LPWStr)] StringBuilder pszDir, int cchMaxPath);
            void SetWorkingDirectory([MarshalAs(UnmanagedType.LPWStr)] string pszDir);
            void GetArguments([Out, MarshalAs(UnmanagedType.LPWStr)] StringBuilder pszArgs, int cchMaxPath);
            void SetArguments([MarshalAs(UnmanagedType.LPWStr)] string pszArgs);
            void GetHotkey(out short pwHotkey);
            void SetHotkey(short wHotkey);
            void GetShowCmd(out int piShowCmd);
            void SetShowCmd(int iShowCmd);
            void GetIconLocation([Out, MarshalAs(UnmanagedType.LPWStr)] StringBuilder pszIconPath, int cchIconPath, out int piIcon);
            void SetIconLocation([MarshalAs(UnmanagedType.LPWStr)] string pszIconPath, int iIcon);
            void SetRelativePath([MarshalAs(UnmanagedType.LPWStr)] string pszPathRel, int dwReserved);
            void Resolve(IntPtr hwnd, int fFlags);
            void SetPath([MarshalAs(UnmanagedType.LPWStr)] string pszFile);
        }

        public static void CreateNativeShortcut(string lnkPath, string targetExe, string workDir, string iconPath, string description)
        {
            try
            {
                IShellLinkW link = (IShellLinkW)(new ShellLinkComObject());
                link.SetPath(targetExe);
                link.SetWorkingDirectory(workDir);
                link.SetIconLocation(iconPath, 0);
                link.SetDescription(description);
                IPersistFile file = (IPersistFile)link;
                file.Save(lnkPath, false);
                return;
            }
            catch { }

            try
            {
                Type shellType = Type.GetTypeFromProgID("WScript.Shell");
                if (shellType == null) return;
                object shell = Activator.CreateInstance(shellType);
                object shortcut = shellType.InvokeMember("CreateShortcut", BindingFlags.InvokeMethod, null, shell, new object[] { lnkPath });
                Type scType = shortcut.GetType();
                scType.InvokeMember("TargetPath", BindingFlags.SetProperty, null, shortcut, new object[] { targetExe });
                scType.InvokeMember("WorkingDirectory", BindingFlags.SetProperty, null, shortcut, new object[] { workDir });
                scType.InvokeMember("IconLocation", BindingFlags.SetProperty, null, shortcut, new object[] { iconPath + ",0" });
                scType.InvokeMember("Description", BindingFlags.SetProperty, null, shortcut, new object[] { description });
                scType.InvokeMember("Save", BindingFlags.InvokeMethod, null, shortcut, null);
            }
            catch { }
        }
    }

    public sealed class AppleInstallerForm : Form
    {
        private const string UninstallRegPath = @"Software\Microsoft\Windows\CurrentVersion\Uninstall\DynamicIslandWin";
        private const string RunRegPath = @"Software\Microsoft\Windows\CurrentVersion\Run";
        private const string AppRegName = "DynamicIslandWin";

        private string _installDir;
        private bool _optDesktopShortcut = true;
        private bool _optStartMenuShortcut = true;
        private bool _optAutostart = true;
        private bool _optLaunchNow = true;

        private float _installProgress = 0.0f;
        private bool _isInstalled = false;
        private bool _isInstalling = false;
        private string _statusMessage = "100% автономный .exe — все файлы уже вшиты внутрь установщика";

        private RectangleF _rcCloseTraffic;
        private RectangleF _rcToggle1, _rcToggle2, _rcToggle3, _rcToggle4;
        private Button _btnInstall, _btnUninstall, _btnBrowse;
        private bool _draggingTitle = false;
        private Point _dragOffset;

        private readonly Font _fTitle = new Font("Segoe UI Semibold", 15f, FontStyle.Bold);
        private readonly Font _fSub = new Font("Segoe UI", 9.5f, FontStyle.Regular);
        private readonly Font _fBold = new Font("Segoe UI Semibold", 10f, FontStyle.Bold);
        private readonly Font _fSmall = new Font("Segoe UI", 8.5f, FontStyle.Regular);

        private readonly StringFormat _sfLeft = new StringFormat { Alignment = StringAlignment.Near, LineAlignment = StringAlignment.Center, Trimming = StringTrimming.EllipsisCharacter };
        private readonly StringFormat _sfCenter = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center };

        public AppleInstallerForm()
        {
            string localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            _installDir = Path.Combine(localAppData, @"Programs\DynamicIslandWin");

            Text = "Установка Dynamic Island for Windows";
            FormBorderStyle = FormBorderStyle.None;
            StartPosition = FormStartPosition.CenterScreen;
            TopMost = true;
            DoubleBuffered = true;
            KeyPreview = true;
            ClientSize = new Size(540, 445);
            BackColor = Color.FromArgb(16, 18, 24);
            ForeColor = Color.White;

            try { Icon = Icon.ExtractAssociatedIcon(Application.ExecutablePath); } catch { }
            Region = Region.FromHrgn(NativeWin32.CreateRoundRectRgn(0, 0, Width + 1, Height + 1, 24, 24));

            // Real native WinForms buttons so clicks work 100% reliably on every DPI & monitor
            _btnBrowse = new Button
            {
                Text = "Изменить...",
                Bounds = new Rectangle(540 - 28 - 92, 131, 82, 26),
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.FromArgb(52, 56, 72),
                ForeColor = Color.FromArgb(100, 210, 255),
                Font = _fSmall,
                Cursor = Cursors.Hand,
                TabStop = false
            };
            _btnBrowse.FlatAppearance.BorderSize = 0;
            _btnBrowse.Click += (s, e) =>
            {
                using (var fbd = new FolderBrowserDialog())
                {
                    fbd.Description = "Выберите папку для установки Dynamic Island";
                    fbd.SelectedPath = _installDir;
                    if (fbd.ShowDialog(this) == DialogResult.OK)
                    {
                        _installDir = fbd.SelectedPath;
                        Invalidate();
                    }
                }
            };

            _btnInstall = new Button
            {
                Text = "Установить Dynamic Island",
                Bounds = new Rectangle(28, 382, 540 - 56 - 126, 42),
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.FromArgb(10, 132, 255),
                ForeColor = Color.White,
                Font = _fBold,
                Cursor = Cursors.Hand
            };
            _btnInstall.FlatAppearance.BorderSize = 0;
            _btnInstall.Click += (s, e) =>
            {
                if (_isInstalled) Close();
                else PerformInstall(false);
            };

            _btnUninstall = new Button
            {
                Text = "Удалить",
                Bounds = new Rectangle(540 - 28 - 116, 382, 116, 42),
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.FromArgb(45, 28, 32),
                ForeColor = Color.FromArgb(255, 69, 58),
                Font = _fBold,
                Cursor = Cursors.Hand,
                TabStop = false
            };
            _btnUninstall.FlatAppearance.BorderSize = 1;
            _btnUninstall.FlatAppearance.BorderColor = Color.FromArgb(90, 255, 69, 58);
            _btnUninstall.Click += (s, e) => PerformUninstall(false);

            Controls.Add(_btnBrowse);
            Controls.Add(_btnInstall);
            Controls.Add(_btnUninstall);
        }

        private static GraphicsPath RoundRect(RectangleF r, float rad)
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

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            Graphics g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.TextRenderingHint = TextRenderingHint.ClearTypeGridFit;

            using (var bgGrad = new LinearGradientBrush(new Point(0, 0), new Point(0, Height),
                Color.FromArgb(22, 24, 32), Color.FromArgb(14, 15, 20)))
            {
                g.FillRectangle(bgGrad, ClientRectangle);
            }

            _rcCloseTraffic = new RectangleF(20, 18, 14, 14);
            using (var bRed = new SolidBrush(Color.FromArgb(255, 95, 86)))
            using (var bYel = new SolidBrush(Color.FromArgb(255, 189, 46)))
            using (var bGrn = new SolidBrush(Color.FromArgb(39, 201, 63)))
            {
                g.FillEllipse(bRed, _rcCloseTraffic);
                g.FillEllipse(bYel, new RectangleF(41, 18, 14, 14));
                g.FillEllipse(bGrn, new RectangleF(62, 18, 14, 14));
            }

            var previewPill = new RectangleF((Width - 224) / 2f, 18f, 224f, 34f);
            using (GraphicsPath pp = RoundRect(previewPill, 17f))
            using (var pb = new SolidBrush(Color.FromArgb(250, 6, 6, 9)))
            using (var pPen = new Pen(Color.FromArgb(140, 10, 132, 255), 1.4f))
            using (var txtBr = new SolidBrush(Color.White))
            {
                g.FillPath(pb, pp);
                g.DrawPath(pPen, pp);
                if (Icon != null)
                {
                    using (Bitmap ib = Icon.ToBitmap())
                        g.DrawImage(ib, new RectangleF(previewPill.X + 7f, previewPill.Y + 4f, 26f, 26f));
                }
                g.DrawString("Dynamic Island  ·  Studio Pro", _fSmall, txtBr, new RectangleF(previewPill.X + 34f, previewPill.Y, previewPill.Width - 40f, previewPill.Height), _sfCenter);
            }

            using (var brWhite = new SolidBrush(Color.FromArgb(245, 245, 250)))
            using (var brMuted = new SolidBrush(Color.FromArgb(155, 158, 170)))
            {
                g.DrawString("Установка Dynamic Island", _fTitle, brWhite, new RectangleF(28, 62, Width - 56, 28), _sfCenter);
                g.DrawString("Вшитый пакет установки для Windows 10 и Windows 11 (64-bit)", _fSub, brMuted, new RectangleF(28, 90, Width - 56, 20), _sfCenter);
            }

            float cardX = 28f;
            float cardW = Width - 56f;
            float curY = 122f;

            var pathCard = new RectangleF(cardX, curY, cardW, 44f);
            using (GraphicsPath cp = RoundRect(pathCard, 12f))
            using (var cb = new SolidBrush(Color.FromArgb(32, 35, 45)))
            using (var cPen = new Pen(Color.FromArgb(24, 255, 255, 255), 1f))
            {
                g.FillPath(cb, cp);
                g.DrawPath(cPen, cp);
            }
            using (var brTxt = new SolidBrush(Color.FromArgb(215, 218, 228)))
                g.DrawString("Папка: " + _installDir, _fSmall, brTxt, new RectangleF(cardX + 14, curY, cardW - 115, 44), _sfLeft);

            curY += 56f;

            float rowH = 38f;
            var optCard = new RectangleF(cardX, curY, cardW, rowH * 4);
            using (GraphicsPath op = RoundRect(optCard, 12f))
            using (var ob = new SolidBrush(Color.FromArgb(32, 35, 45)))
            using (var oPen = new Pen(Color.FromArgb(24, 255, 255, 255), 1f))
            {
                g.FillPath(ob, op);
                g.DrawPath(oPen, op);
            }
            using (var linePen = new Pen(Color.FromArgb(18, 255, 255, 255), 1f))
            {
                for (int i = 1; i < 4; i++)
                    g.DrawLine(linePen, cardX + 14f, curY + i * rowH, cardX + cardW - 14f, curY + i * rowH);
            }

            _rcToggle1 = DrawToggleRow(g, cardX, curY + 0 * rowH, cardW, rowH, "Создать ярлык «Dynamic Island» на Рабочем столе", _optDesktopShortcut);
            _rcToggle2 = DrawToggleRow(g, cardX, curY + 1 * rowH, cardW, rowH, "Добавить в меню «Пуск» и список программ Windows", _optStartMenuShortcut);
            _rcToggle3 = DrawToggleRow(g, cardX, curY + 2 * rowH, cardW, rowH, "Запускать автоматически при входе в Windows", _optAutostart);
            _rcToggle4 = DrawToggleRow(g, cardX, curY + 3 * rowH, cardW, rowH, "Запустить Dynamic Island сразу после установки", _optLaunchNow);

            curY += rowH * 4 + 16f;

            var progGroove = new RectangleF(cardX, curY, cardW, 6f);
            using (GraphicsPath pg = RoundRect(progGroove, 3f))
            using (var pgb = new SolidBrush(Color.FromArgb(40, 44, 56)))
                g.FillPath(pgb, pg);

            if (_installProgress > 0.01f)
            {
                var progFill = new RectangleF(cardX, curY, Math.Max(8f, cardW * _installProgress), 6f);
                using (GraphicsPath pf = RoundRect(progFill, 3f))
                using (var pfb = new LinearGradientBrush(progFill, Color.FromArgb(10, 132, 255), Color.FromArgb(48, 209, 88), 0f))
                    g.FillPath(pfb, pf);
            }

            using (var stBr = new SolidBrush(_isInstalled ? Color.FromArgb(48, 209, 88) : Color.FromArgb(170, 174, 186)))
                g.DrawString(_statusMessage, _fSmall, stBr, new RectangleF(cardX, curY + 10f, cardW, 18f), _sfCenter);

            using (GraphicsPath wp = RoundRect(new RectangleF(0.5f, 0.5f, Width - 1f, Height - 1f), 24f))
            using (var wPen = new Pen(Color.FromArgb(45, 255, 255, 255), 1f))
                g.DrawPath(wPen, wp);
        }

        private RectangleF DrawToggleRow(Graphics g, float x, float y, float w, float h, string label, bool checkedVal)
        {
            using (var tb = new SolidBrush(Color.FromArgb(235, 235, 242)))
                g.DrawString(label, _fSub, tb, new RectangleF(x + 14f, y, w - 76f, h), _sfLeft);

            var swRc = new RectangleF(x + w - 54f, y + (h - 22f) / 2f, 40f, 22f);
            using (GraphicsPath sp = RoundRect(swRc, 11f))
            using (var sb = new SolidBrush(checkedVal ? Color.FromArgb(48, 209, 88) : Color.FromArgb(62, 64, 78)))
                g.FillPath(sb, sp);

            float knobX = checkedVal ? (swRc.Right - 20f) : (swRc.X + 2f);
            using (var kb = new SolidBrush(Color.White))
                g.FillEllipse(kb, knobX, swRc.Y + 2f, 18f, 18f);

            return new RectangleF(x, y, w, h);
        }

        private void StepProgress(float target, string msg, int sleepMs = 100)
        {
            _statusMessage = msg;
            _installProgress = target;
            Invalidate();
            Update();
            Application.DoEvents();
            if (sleepMs > 0) Thread.Sleep(sleepMs);
        }

        public void PerformInstall(bool silent = false)
        {
            if (_isInstalling) return;

            // Check if an existing version is already installed in %LocalAppData%\Programs\DynamicIslandWin
            string existingExe = Path.Combine(_installDir, "DynamicIslandWin.exe");
            if (!silent && File.Exists(existingExe))
            {
                string oldVer = "2.0.0";
                try
                {
                    var fvi = FileVersionInfo.GetVersionInfo(existingExe);
                    if (fvi != null && !string.IsNullOrEmpty(fvi.FileVersion))
                        oldVer = fvi.FileVersion;
                }
                catch { }

                if (oldVer != "3.1.2.0")
                {
                    var ans = MessageBox.Show(
                        this,
                        string.Format("На вашем ПК уже установлена версия Dynamic Island v{0}.\n\nОбновить Dynamic Island с v{0} до v3.1.2 Studio Pro (с сохранением всех ваших настроек)?", oldVer),
                        "Обновление Dynamic Island",
                        MessageBoxButtons.YesNo,
                        MessageBoxIcon.Question);
                    if (ans != DialogResult.Yes) return;
                }
            }

            _isInstalling = true;
            _btnInstall.Enabled = false;
            try
            {
                StepProgress(0.20f, "Остановка запущенных экземпляров...", silent ? 5 : 90);

                int myPid = Process.GetCurrentProcess().Id;
                foreach (Process p in Process.GetProcessesByName("DynamicIslandWin"))
                {
                    if (p.Id != myPid)
                    {
                        try { p.Kill(); p.WaitForExit(1200); } catch { }
                    }
                }

                if (!Directory.Exists(_installDir))
                    Directory.CreateDirectory(_installDir);

                foreach (string oldFile in Directory.GetFiles(_installDir, "*.old"))
                {
                    try { File.Delete(oldFile); } catch { }
                }

                StepProgress(0.55f, "Распаковка встроенного приложения DynamicIslandWin.exe...", silent ? 5 : 120);

                string targetExe = Path.Combine(_installDir, "DynamicIslandWin.exe");
                string targetIco = Path.Combine(_installDir, "DynamicIslandWin.ico");
                string targetUninst = Path.Combine(_installDir, "Uninstall.exe");

                SafeExtractEmbeddedResource("DynamicIslandWin.Payload.exe", targetExe);
                SafeExtractEmbeddedResource("DynamicIslandWin.Payload.ico", targetIco);

                // Remove SmartScreen Zone.Identifier stream so Windows never blocks the installed app
                try { NativeWin32.DeleteFileW(targetExe + ":Zone.Identifier"); } catch { }
                try { NativeWin32.DeleteFileW(targetIco + ":Zone.Identifier"); } catch { }

                if (!string.Equals(Application.ExecutablePath, targetUninst, StringComparison.OrdinalIgnoreCase))
                {
                    try
                    {
                        if (File.Exists(targetUninst))
                        {
                            try { File.Delete(targetUninst); }
                            catch { File.Move(targetUninst, targetUninst + "." + Environment.TickCount + ".old"); }
                        }
                        File.Copy(Application.ExecutablePath, targetUninst, true);
                        try { NativeWin32.DeleteFileW(targetUninst + ":Zone.Identifier"); } catch { }
                    }
                    catch { }
                }

                StepProgress(0.82f, "Создание ярлыков на Рабочем столе и в меню Пуск...", silent ? 5 : 120);

                if (_optDesktopShortcut)
                {
                    string desktopDir = Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory);
                    string dLnk = Path.Combine(desktopDir, "Dynamic Island.lnk");
                    if (File.Exists(dLnk)) { try { File.Delete(dLnk); } catch { } }
                    NativeWin32.CreateNativeShortcut(dLnk, targetExe, _installDir, targetIco, "Dynamic Island for Windows 10/11");
                }

                if (_optStartMenuShortcut)
                {
                    string startMenuDir = Environment.GetFolderPath(Environment.SpecialFolder.Programs);
                    string sLnk = Path.Combine(startMenuDir, "Dynamic Island.lnk");
                    if (File.Exists(sLnk)) { try { File.Delete(sLnk); } catch { } }
                    NativeWin32.CreateNativeShortcut(sLnk, targetExe, _installDir, targetIco, "Dynamic Island for Windows 10/11");
                }

                try { NativeWin32.SHChangeNotify(0x08000000, 0x0000, IntPtr.Zero, IntPtr.Zero); } catch { }

                try
                {
                    using (RegistryKey k = Registry.CurrentUser.CreateSubKey(UninstallRegPath))
                    {
                        if (k != null)
                        {
                            k.SetValue("DisplayName", "Dynamic Island for Windows 10/11");
                            k.SetValue("DisplayVersion", "3.1.2");
                            k.SetValue("Publisher", "DynamicIslandWin Studio");
                            k.SetValue("DisplayIcon", targetExe);
                            k.SetValue("InstallLocation", _installDir);
                            k.SetValue("UninstallString", "\"" + targetUninst + "\" --uninstall");
                            k.SetValue("NoModify", 1, RegistryValueKind.DWord);
                            k.SetValue("NoRepair", 1, RegistryValueKind.DWord);
                            k.SetValue("EstimatedSize", 120, RegistryValueKind.DWord);
                        }
                    }
                }
                catch { }

                try
                {
                    using (RegistryKey rk = Registry.CurrentUser.OpenSubKey(RunRegPath, true))
                    {
                        if (rk != null)
                        {
                            if (_optAutostart) rk.SetValue(AppRegName, "\"" + targetExe + "\"");
                            else rk.DeleteValue(AppRegName, false);
                        }
                    }
                }
                catch { }

                StepProgress(1.0f, "✓ Установлено! Dynamic Island запущен вверху экрана.", silent ? 5 : 100);
                _isInstalled = true;
                _btnInstall.BackColor = Color.FromArgb(48, 209, 88);
                _btnInstall.Text = "✓ Установлено (Закрыть)";
                _btnInstall.Enabled = true;

                if (_optLaunchNow && File.Exists(targetExe))
                {
                    // UseShellExecute = false uses CreateProcessW directly, bypassing any SmartScreen/Shell prompt!
                    Process.Start(new ProcessStartInfo
                    {
                        FileName = targetExe,
                        WorkingDirectory = _installDir,
                        UseShellExecute = false
                    });
                }

                if (!silent)
                {
                    TopMost = false;
                    MessageBox.Show(
                        this,
                        "Dynamic Island v3.1.2 Studio Pro успешно установлен!\n\n" +
                        "• Папка установки: " + _installDir + "\n" +
                        "• Ярлык «Dynamic Island» создан на Рабочем столе\n" +
                        "• Островок уже запущен в верхней части вашего экрана!",
                        "Установка завершена",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Information);
                    Close();
                }
            }
            catch (Exception ex)
            {
                _statusMessage = "Ошибка установки: " + ex.Message;
                _btnInstall.Enabled = true;
                Invalidate();
                if (!silent)
                    MessageBox.Show(this, "Ошибка при установке:\n" + ex.Message, "Dynamic Island Setup", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                _isInstalling = false;
            }
        }

        public void PerformUninstall(bool silent = false)
        {
            try
            {
                foreach (Process p in Process.GetProcessesByName("DynamicIslandWin"))
                {
                    try { p.Kill(); p.WaitForExit(1000); } catch { }
                }

                string desktopLnk = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory), "Dynamic Island.lnk");
                string startLnk = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Programs), "Dynamic Island.lnk");
                if (File.Exists(desktopLnk)) File.Delete(desktopLnk);
                if (File.Exists(startLnk)) File.Delete(startLnk);

                try { Registry.CurrentUser.DeleteSubKeyTree(UninstallRegPath, false); } catch { }
                try
                {
                    using (RegistryKey rk = Registry.CurrentUser.OpenSubKey(RunRegPath, true))
                    {
                        if (rk != null) rk.DeleteValue(AppRegName, false);
                    }
                }
                catch { }

                string targetExe = Path.Combine(_installDir, "DynamicIslandWin.exe");
                if (File.Exists(targetExe))
                {
                    try { File.Delete(targetExe); } catch { }
                }

                _installProgress = 0.0f;
                _isInstalled = false;
                _btnInstall.BackColor = Color.FromArgb(10, 132, 255);
                _btnInstall.Text = "Установить Dynamic Island";
                _statusMessage = "Dynamic Island удален из системы и автозагрузки.";
                Invalidate();
                if (!silent)
                    MessageBox.Show(this, "Dynamic Island полностью удален из системы.", "Удаление завершено", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                _statusMessage = "Ошибка удаления: " + ex.Message;
                Invalidate();
            }
        }

        private static void SafeExtractEmbeddedResource(string resourceName, string outPath)
        {
            using (Stream s = Assembly.GetExecutingAssembly().GetManifestResourceStream(resourceName))
            {
                if (s == null) throw new InvalidOperationException("Встроенный ресурс не найден: " + resourceName);

                for (int attempt = 0; attempt < 6; attempt++)
                {
                    try
                    {
                        if (File.Exists(outPath))
                        {
                            try { File.Delete(outPath); }
                            catch
                            {
                                string bak = outPath + "." + Environment.TickCount + ".old";
                                File.Move(outPath, bak);
                            }
                        }
                        using (FileStream fs = new FileStream(outPath, FileMode.Create, FileAccess.Write, FileShare.Read))
                        {
                            s.Position = 0;
                            s.CopyTo(fs);
                        }
                        return;
                    }
                    catch
                    {
                        if (attempt == 5) throw;
                        Thread.Sleep(150);
                    }
                }
            }
        }

        protected override void OnMouseDown(MouseEventArgs e)
        {
            base.OnMouseDown(e);
            PointF pt = new PointF(e.X, e.Y);
            if (e.Button != MouseButtons.Left) return;

            if (_rcCloseTraffic.Contains(pt)) { Close(); return; }
            if (_rcToggle1.Contains(pt)) { _optDesktopShortcut = !_optDesktopShortcut; Invalidate(); return; }
            if (_rcToggle2.Contains(pt)) { _optStartMenuShortcut = !_optStartMenuShortcut; Invalidate(); return; }
            if (_rcToggle3.Contains(pt)) { _optAutostart = !_optAutostart; Invalidate(); return; }
            if (_rcToggle4.Contains(pt)) { _optLaunchNow = !_optLaunchNow; Invalidate(); return; }

            if (e.Y < 56)
            {
                _draggingTitle = true;
                _dragOffset = e.Location;
            }
        }

        protected override void OnMouseMove(MouseEventArgs e)
        {
            base.OnMouseMove(e);
            if (_draggingTitle && e.Button == MouseButtons.Left)
            {
                Point screenPt = PointToScreen(e.Location);
                Location = new Point(screenPt.X - _dragOffset.X, screenPt.Y - _dragOffset.Y);
            }
        }

        protected override void OnMouseUp(MouseEventArgs e)
        {
            base.OnMouseUp(e);
            _draggingTitle = false;
        }
    }

    internal static class Program
    {
        [STAThread]
        private static void Main(string[] args)
        {
            try { NativeWin32.SetProcessDPIAware(); } catch { }
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);

            var form = new AppleInstallerForm();
            if (args != null && args.Length > 0)
            {
                foreach (string a in args)
                {
                    string low = (a ?? "").ToLowerInvariant();
                    if (low == "--silent" || low == "/verysilent" || low == "/silent")
                    {
                        form.PerformInstall(true);
                        return;
                    }
                    if (low == "--uninstall" || low == "/uninstall")
                    {
                        form.PerformUninstall(false);
                        return;
                    }
                }
            }
            Application.Run(form);
        }
    }
}