using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.IO;
using System.IO.Compression;
using System.Diagnostics;
using System.Net;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using System.Web.Script.Serialization;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Reflection;
using Microsoft.Win32;

namespace BlockInstaller
{
    public class InstallerForm : Form
    {
        private const string InstallerVersion = InstallerBuildVersion.Value;
        private Button btnInstall;
        private Label lblTitle;
        private Label lblStatus;
        private ProgressBar progress;
        private TextBox txtPath;
        private Button btnBrowse;
        
        private CheckedListBox clbLang;
        private RadioButton rbLite;
        private RadioButton rbStandard;
        private RadioButton rbPlus;
        private Label lblVersionDesc;
        
        private Color bgColor = Color.FromArgb(3, 3, 4);
        private Color fgColor = Color.White;
        private Color accentColor = Color.White;
        private Color lineColor = Color.FromArgb(34, 34, 38);
        private Color panelColor = Color.FromArgb(9, 9, 11);

        private static bool IsOwnedProcess(Process process, string installDir)
        {
            try
            {
                string actual = Path.GetFullPath(process.MainModule.FileName);
                string root = Path.GetFullPath(installDir).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar) + Path.DirectorySeparatorChar;
                return actual.StartsWith(root, StringComparison.OrdinalIgnoreCase);
            }
            catch { return false; }
        }

        private static bool ContainsReparsePoint(string path)
        {
            string current;
            try { current = Path.GetFullPath(path); }
            catch { return true; }
            while (!string.IsNullOrEmpty(current))
            {
                try
                {
                    if ((File.GetAttributes(current) & FileAttributes.ReparsePoint) != 0) return true;
                }
                catch (FileNotFoundException) { }
                catch (DirectoryNotFoundException) { }
                catch (UnauthorizedAccessException) { return true; }
                string parent = Path.GetDirectoryName(current);
                if (string.IsNullOrEmpty(parent) || string.Equals(parent, current, StringComparison.OrdinalIgnoreCase)) break;
                current = parent;
            }
            return false;
        }

        private static string NormalizePathEntry(string path)
        {
            if (string.IsNullOrWhiteSpace(path)) return "";
            string value = path.Trim().Trim('"');
            try
            {
                return Path.GetFullPath(value).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
            }
            catch
            {
                return value.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
            }
        }

        private static bool PathContainsEntry(string pathValue, string entry)
        {
            string expected = NormalizePathEntry(entry);
            if (string.IsNullOrEmpty(expected)) return false;
            foreach (string item in (pathValue ?? "").Split(new[] { ';' }, StringSplitOptions.RemoveEmptyEntries))
            {
                if (string.Equals(NormalizePathEntry(item), expected, StringComparison.OrdinalIgnoreCase))
                    return true;
            }
            return false;
        }

        private static string RemovePathEntry(string pathValue, string entry)
        {
            string expected = NormalizePathEntry(entry);
            List<string> kept = new List<string>();
            foreach (string item in (pathValue ?? "").Split(new[] { ';' }, StringSplitOptions.RemoveEmptyEntries))
            {
                if (!string.Equals(NormalizePathEntry(item), expected, StringComparison.OrdinalIgnoreCase))
                    kept.Add(item);
            }
            return string.Join(";", kept);
        }

        private static readonly string[] RuntimeNames = new string[]
        {
            "Python", "NodeJS (JS/TS)", "PHP", "Ruby", "Lua", "SQLite",
            "Go (Block+ Only)", "Rust (Block+ Only)", "Java JDK (Block+ Only)",
            "Dart (Block+ Only)", "Zig (Block+ Only)", "Perl (Block+ Only)", "R (Block+ Only)"
        };

        private Dictionary<string, string[]> runtimeExecutables = new Dictionary<string, string[]>(StringComparer.OrdinalIgnoreCase)
        {
            {"Python", new[] { "python.exe" }},
            {"NodeJS (JS/TS)", new[] { "node.exe" }},
            {"PHP", new[] { "php.exe" }},
            {"Ruby", new[] { "ruby.exe" }},
            {"Lua", new[] { "lua.exe" }},
            {"SQLite", new[] { "sqlite3.exe" }},
            {"Go (Block+ Only)", new[] { "go.exe" }},
            {"Rust (Block+ Only)", new[] { "rustc.exe", "cargo.exe" }},
            {"Java JDK (Block+ Only)", new[] { "java.exe" }},
            {"Dart (Block+ Only)", new[] { "dart.exe" }},
            {"Zig (Block+ Only)", new[] { "zig.exe" }},
            {"Perl (Block+ Only)", new[] { "perl.exe" }},
            {"R (Block+ Only)", new[] { "Rscript.exe", "R.exe" }}
        };

        private const string OfficialRepository = "O-O1112/Block_lang";
        private const string OfficialApiBase = "https://api.github.com/repos/" + OfficialRepository + "/releases/tags/v";
        private const int MaxReleaseMetadataBytes = 4 * 1024 * 1024;
        private const long MaxReleaseAssetBytes = 512L * 1024L * 1024L;
        private static readonly JavaScriptSerializer Json = new JavaScriptSerializer { MaxJsonLength = MaxReleaseMetadataBytes };

        static InstallerForm()
        {
            // GitHub requires TLS 1.2 or newer. Older .NET Framework releases can
            // otherwise default to TLS 1.0 even on a fully updated Windows host.
            // Select only TLS 1.2; never fall back to SSL 3.0 or legacy TLS.
            ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;
            ServicePointManager.Expect100Continue = false;
        }

        public InstallerForm()
        {
            this.Text = "Block Installer";
            this.Size = new Size(520, 650);
            this.FormBorderStyle = FormBorderStyle.None;
            this.StartPosition = FormStartPosition.CenterScreen;
            this.BackColor = bgColor;
            this.ForeColor = fgColor;
            this.DoubleBuffered = true;
            this.SetStyle(ControlStyles.OptimizedDoubleBuffer | ControlStyles.AllPaintingInWmPaint | ControlStyles.ResizeRedraw, true);
            try { this.Icon = Icon.ExtractAssociatedIcon(Application.ExecutablePath); } catch { }
            
            this.MouseDown += (s, e) => {
                if (e.Button == MouseButtons.Left)
                {
                    NativeMethods.ReleaseCapture();
                    NativeMethods.SendMessage(this.Handle, NativeMethods.WM_NCLBUTTONDOWN, NativeMethods.HT_CAPTION, 0);
                }
            };

            lblTitle = new Label
            {
                Text = "Block Engine " + InstallerVersion,
                Font = new Font("Segoe UI", 16, FontStyle.Bold),
                ForeColor = fgColor,
                AutoSize = true,
                Location = new Point(78, 28),
                BackColor = Color.Transparent
            };

            lblStatus = new Label
            {
                Text = "Ready to install.",
                Font = new Font("Consolas", 8, FontStyle.Regular),
                ForeColor = Color.FromArgb(126, 126, 132),
                AutoSize = true,
                Location = new Point(80, 57),
                BackColor = Color.Transparent
            };

            Label lblPath = new Label
            {
                Text = "01 / INSTALL PATH",
                Font = new Font("Consolas", 8, FontStyle.Regular),
                ForeColor = Color.FromArgb(150, 150, 156),
                AutoSize = true,
                Location = new Point(32, 122),
                BackColor = Color.Transparent
            };

            string defaultPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".blocklang", "bin");
            
            txtPath = new TextBox
            {
                Text = defaultPath,
                Location = new Point(32, 149),
                Size = new Size(388, 30),
                Font = new Font("Consolas", 8),
                BackColor = Color.FromArgb(11, 11, 13),
                ForeColor = Color.White,
                BorderStyle = BorderStyle.FixedSingle
            };

            btnBrowse = new Button
            {
                Text = "...",
                Location = new Point(428, 149),
                Size = new Size(60, 30),
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Consolas", 9, FontStyle.Bold),
                ForeColor = Color.White,
                BackColor = panelColor,
                Cursor = Cursors.Hand
            };
            btnBrowse.FlatAppearance.BorderSize = 0;
            btnBrowse.Click += (s, e) => {
                using (FolderBrowserDialog fbd = new FolderBrowserDialog())
                {
                    fbd.Description = "Select Installation Folder";
                    fbd.SelectedPath = txtPath.Text;
                    if (fbd.ShowDialog() == DialogResult.OK)
                    {
                        txtPath.Text = fbd.SelectedPath;
                    }
                }
            };

            Label lblVersion = new Label
            {
                Text = "02 / SELECT ENGINE EDITION",
                Font = new Font("Consolas", 8, FontStyle.Regular),
                ForeColor = Color.FromArgb(150, 150, 156),
                AutoSize = true,
                Location = new Point(32, 207),
                BackColor = Color.Transparent
            };

            rbLite = new RadioButton
            {
                Text = "Block Lite — Lightweight (.blkl)",
                Font = new Font("Segoe UI", 9),
                ForeColor = Color.White,
                BackColor = Color.Transparent,
                Location = new Point(32, 234),
                AutoSize = true,
                 FlatStyle = FlatStyle.Standard,
                 UseVisualStyleBackColor = true,
                Padding = new Padding(2, 0, 0, 0)
            };

            rbStandard = new RadioButton
            {
                Text = "Block — Standard, Recommended (.blk) ★",
                Font = new Font("Segoe UI", 9),
                ForeColor = Color.White,
                BackColor = Color.Transparent,
                Location = new Point(32, 260),
                AutoSize = true,
                Checked = true,
                 FlatStyle = FlatStyle.Standard,
                 UseVisualStyleBackColor = true,
                Padding = new Padding(2, 0, 0, 0)
            };

            rbPlus = new RadioButton
            {
                Text = "Block+ — Flagship (.blkp)",
                Font = new Font("Segoe UI", 9),
                ForeColor = Color.White,
                BackColor = Color.Transparent,
                Location = new Point(32, 286),
                AutoSize = true,
                 FlatStyle = FlatStyle.Standard,
                 UseVisualStyleBackColor = true,
                Padding = new Padding(2, 0, 0, 0)
            };

            lblVersionDesc = new Label
            {
                Text = "Standard edition. Includes HTTP server, API mode, and core runtimes.",
                Font = new Font("Segoe UI", 8, FontStyle.Regular),
                ForeColor = Color.FromArgb(116, 116, 122),
                AutoSize = false,
                Size = new Size(450, 30),
                Location = new Point(32, 314),
                BackColor = Color.Transparent
            };

            rbLite.CheckedChanged += (s, e) => {
                if (rbLite.Checked)
                    lblVersionDesc.Text = "Lightweight edition. Built-in Python, JS & HTML without HTTP server.";
            };
            rbStandard.CheckedChanged += (s, e) => {
                if (rbStandard.Checked)
                    lblVersionDesc.Text = "Standard edition. Includes HTTP server, API mode, and core runtimes.";
            };
            rbPlus.CheckedChanged += (s, e) => {
                if (rbPlus.Checked)
                    lblVersionDesc.Text = "Flagship edition. Unlocks 15+ advanced runtimes and extended tooling.";
            };

            Label lblLang = new Label
            {
                Text = "03 / RUNTIME COMPONENTS",
                Font = new Font("Consolas", 8, FontStyle.Regular),
                ForeColor = Color.FromArgb(150, 150, 156),
                AutoSize = true,
                Location = new Point(32, 360),
                BackColor = Color.Transparent
            };

            clbLang = new CheckedListBox
            {
                Location = new Point(32, 385),
                Size = new Size(456, 112),
                Font = new Font("Segoe UI", 8),
                BackColor = Color.FromArgb(9, 9, 11),
                ForeColor = Color.White,
                BorderStyle = BorderStyle.None,
                CheckOnClick = true,
                IntegralHeight = false,
                ItemHeight = 22,
                HorizontalScrollbar = false
            };
            
            clbLang.Items.Add("Block Engine Core (Required)", CheckState.Checked);
            foreach (string key in RuntimeNames)
            {
                bool defaultCheck = key.StartsWith("Python") || key.StartsWith("NodeJS");
                clbLang.Items.Add(key + " Runtime", defaultCheck ? CheckState.Checked : CheckState.Unchecked);
            }
            clbLang.ItemCheck += (s, e) => {
                if (e.Index == 0 && e.NewValue == CheckState.Unchecked)
                    e.NewValue = CheckState.Checked;
            };

            progress = new ProgressBar
            {
                Size = new Size(456, 5),
                Location = new Point(32, 524),
                Style = ProgressBarStyle.Continuous,
                Visible = false,
                ForeColor = Color.White,
                BackColor = Color.FromArgb(28, 28, 31)
            };

            btnInstall = new Button
            {
                Text = "Install",
                Size = new Size(142, 40),
                Location = new Point(32, 552),
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 9, FontStyle.Bold),
                ForeColor = bgColor,
                BackColor = accentColor,
                Cursor = Cursors.Hand
            };
            btnInstall.FlatAppearance.BorderSize = 0;
            btnInstall.Click += BtnInstall_Click;
            btnInstall.MouseEnter += (s, e) => btnInstall.BackColor = Color.LightGray;
            btnInstall.MouseLeave += (s, e) => btnInstall.BackColor = accentColor;

            Button btnClose = new Button
            {
                Text = "X",
                Size = new Size(30, 30),
                Location = new Point(this.Width - 42, 12),
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 12, FontStyle.Regular),
                ForeColor = Color.FromArgb(112, 112, 118),
                BackColor = Color.Transparent,
                Cursor = Cursors.Hand
            };
            btnClose.FlatAppearance.BorderSize = 0;
            btnClose.Click += (s, e) => this.Close();
            btnClose.MouseEnter += (s, e) => btnClose.ForeColor = Color.White;
            btnClose.MouseLeave += (s, e) => btnClose.ForeColor = Color.DarkGray;

            this.Controls.Add(lblTitle);
            this.Controls.Add(lblStatus);
            this.Controls.Add(lblPath);
            this.Controls.Add(txtPath);
            this.Controls.Add(btnBrowse);
            this.Controls.Add(lblVersion);
            this.Controls.Add(rbLite);
            this.Controls.Add(rbStandard);
            this.Controls.Add(rbPlus);
            this.Controls.Add(lblVersionDesc);
            this.Controls.Add(lblLang);
            this.Controls.Add(clbLang);
            this.Controls.Add(progress);
            this.Controls.Add(btnInstall);
            this.Controls.Add(btnClose);

            // Always start a new install/update on the recommended Standard
            // edition. Setting this after all three controls are attached makes
            // the WinForms radio group deterministic on every launch.
            rbLite.Checked = false;
            rbPlus.Checked = false;
            rbStandard.Checked = true;

            bool isInstalled = Registry.CurrentUser.OpenSubKey(@"Software\Classes\block_engine_install") != null || Registry.CurrentUser.OpenSubKey(@"Software\Classes\block_script") != null;
            if (isInstalled)
            {
                lblVersion.Visible = false;
                rbLite.Visible = false;
                rbStandard.Visible = false;
                rbPlus.Visible = false;
                lblVersionDesc.Visible = false;
                lblLang.Visible = false;
                clbLang.Visible = false;
                lblPath.Visible = false;
                txtPath.Visible = false;
                btnBrowse.Visible = false;
                
                lblStatus.Text = "Block Engine is already installed.";
                lblStatus.Font = new Font("Segoe UI", 11, FontStyle.Bold);
                lblStatus.ForeColor = Color.White;
                lblStatus.Location = new Point(32, 165);
                
                btnInstall.Text = "OK";
                btnInstall.Click -= BtnInstall_Click;
                btnInstall.Click += BtnClose_Click;
                
                Button btnUpdate = new Button
                {
                    Text = "Update / Modify",
                    Size = new Size(142, 40),
                    Location = new Point(190, 552),
                    FlatStyle = FlatStyle.Flat,
                    Font = new Font("Segoe UI", 9, FontStyle.Bold),
                    ForeColor = Color.White,
                    BackColor = panelColor,
                    Cursor = Cursors.Hand
                };
                btnUpdate.FlatAppearance.BorderSize = 1;
                btnUpdate.FlatAppearance.BorderColor = Color.FromArgb(80, 80, 86);
                this.Controls.Add(btnUpdate);
                
                Button btnUninstall = new Button
                {
                    Text = "Uninstall",
                    Size = new Size(142, 40),
                    Location = new Point(346, 552),
                    FlatStyle = FlatStyle.Flat,
                    Font = new Font("Segoe UI", 9, FontStyle.Bold),
                    ForeColor = Color.FromArgb(226, 106, 106),
                    BackColor = panelColor,
                    Cursor = Cursors.Hand
                };
                btnUninstall.FlatAppearance.BorderSize = 1;
                btnUninstall.FlatAppearance.BorderColor = Color.FromArgb(132, 54, 54);
                btnUninstall.Click += BtnUninstall_Click;
                this.Controls.Add(btnUninstall);
                
                btnUpdate.Click += (s, e) => {
                    lblStatus.Text = "Ready to update.";
                    lblStatus.Font = new Font("Consolas", 8, FontStyle.Regular);
                    lblStatus.ForeColor = Color.FromArgb(126, 126, 132);
                    lblStatus.Location = new Point(80, 57);
                    
                    lblVersion.Visible = true;
                    rbLite.Visible = true;
                    rbStandard.Visible = true;
                    rbPlus.Visible = true;
                    lblVersionDesc.Visible = true;
                    lblLang.Visible = true;
                    clbLang.Visible = true;
                    lblPath.Visible = true;
                    txtPath.Visible = true;
                    btnBrowse.Visible = true;
                    
                    btnUninstall.Visible = false;
                    btnUpdate.Visible = false;
                    
                     btnInstall.Text = "Update";
                     btnInstall.Click -= BtnClose_Click;
                     btnInstall.Click += BtnInstall_Click;

                     rbLite.Checked = false;
                     rbPlus.Checked = false;
                     rbStandard.Checked = true;
                 };
            }
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            Graphics g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;

            g.Clear(bgColor);
            using (Pen gridPen = new Pen(Color.FromArgb(12, 12, 15), 1))
            {
                for (int x = 16; x < this.Width; x += 32) g.DrawLine(gridPen, x, 0, x, this.Height);
                for (int y = 16; y < this.Height; y += 32) g.DrawLine(gridPen, 0, y, this.Width, y);
            }

            using (Pen sectionPen = new Pen(lineColor, 1))
            {
                g.DrawLine(sectionPen, 24, 104, this.Width - 24, 104);
                g.DrawLine(sectionPen, 24, 194, this.Width - 24, 194);
                g.DrawLine(sectionPen, 24, 347, this.Width - 24, 347);
                g.DrawLine(sectionPen, 24, 510, this.Width - 24, 510);
            }

            Pen borderPen = new Pen(Color.FromArgb(45, 45, 48), 1);
            g.DrawRectangle(borderPen, 0, 0, this.Width - 1, this.Height - 1);

            Brush b = Brushes.White;
            float startX = 32f;
            float startY = 28f;
            float size = 36f;
            
            float scale = size / 100f;
            
            g.FillRectangle(b, startX + 25 * scale, startY + 20 * scale, 15 * scale, 60 * scale);
            
            PointF[] p1 = new PointF[] {
                new PointF(startX + 45 * scale, startY + 20 * scale),
                new PointF(startX + 70 * scale, startY + 20 * scale),
                new PointF(startX + 75 * scale, startY + 32.5f * scale),
                new PointF(startX + 70 * scale, startY + 45 * scale),
                new PointF(startX + 45 * scale, startY + 45 * scale)
            };
            g.FillPolygon(b, p1);

            PointF[] p2 = new PointF[] {
                new PointF(startX + 45 * scale, startY + 55 * scale),
                new PointF(startX + 75 * scale, startY + 55 * scale),
                new PointF(startX + 80 * scale, startY + 67.5f * scale),
                new PointF(startX + 75 * scale, startY + 80 * scale),
                new PointF(startX + 45 * scale, startY + 80 * scale)
            };
            g.FillPolygon(b, p2);
        }

        private void BtnClose_Click(object sender, EventArgs e) { this.Close(); }

        private async void BtnUninstall_Click(object sender, EventArgs e)
        {
            if (MessageBox.Show("Are you sure you want to uninstall Block Engine and all related components?", "Uninstall", MessageBoxButtons.YesNo, MessageBoxIcon.Warning) == DialogResult.Yes)
            {
                Button btnUninstall = (Button)sender;
                btnUninstall.Enabled = false;
                btnInstall.Enabled = false;
                
                lblStatus.Text = "Uninstalling...";
                progress.Visible = true;
                progress.Style = ProgressBarStyle.Marquee;
                
                string installDir = txtPath.Text;
                using (RegistryKey key = Registry.CurrentUser.OpenSubKey(@"Software\Classes\block_engine_install"))
                {
                    string registeredDir = key == null ? null : key.GetValue("InstallDir") as string;
                    if (!string.IsNullOrWhiteSpace(registeredDir)) installDir = registeredDir;
                }
                
                Exception uninstallError = null;
                await Task.Run(() => {
                    try {
                        foreach (string pName in new string[] { "block", "block-lite", "block-plus" })
                        {
                            foreach (var process in System.Diagnostics.Process.GetProcessesByName(pName))
                            {
                                try { if (IsOwnedProcess(process, installDir)) { process.Kill(); process.WaitForExit(1000); } } catch { }
                            }
                        }
                        
                        // Only remove associations that still point to this installer.
                        // Users may have reassigned these extensions after installation.
                        UnregisterExtension(".blk", "block_script");
                        UnregisterExtension(".block", "block_script");
                        UnregisterExtension(".blkl", "blocklite_script");
                        UnregisterExtension(".blocklite", "blocklite_script");
                        UnregisterExtension(".blkp", "blockplus_script");
                        UnregisterExtension(".blockplus", "blockplus_script");
                        Registry.CurrentUser.DeleteSubKeyTree(@"Software\Classes\block_engine_install", false);
                        Registry.CurrentUser.DeleteSubKeyTree(@"Software\Microsoft\Windows\CurrentVersion\Uninstall\BlockEngine", false);
                        NativeMethods.SHChangeNotify(0x08000000, 0x0000, IntPtr.Zero, IntPtr.Zero);
                        
                        // Delete only Block Engine created files to avoid deleting user documents if installed in shared folders
                        if (Directory.Exists(installDir))
                        {
                            string[] filesToDelete = new string[] {
                                "block.exe", "block-lite.exe", "block-plus.exe", "icon.ico",
                                "block.zip", "block-lite.zip", "block-plus.zip"
                            };
                            foreach (string file in filesToDelete)
                            {
                                string target = Path.Combine(installDir, file);
                                if (File.Exists(target))
                                {
                                    try { File.Delete(target); } catch { }
                                }
                            }

                            // If installDir is the default .blocklang/bin and is now empty, clean up the empty directory
                            try
                            {
                                if (Directory.GetFiles(installDir).Length == 0 && Directory.GetDirectories(installDir).Length == 0)
                                {
                                    Directory.Delete(installDir, false);
                                }
                            }
                            catch { }
                        }
                        
                        string currentPath = Environment.GetEnvironmentVariable("PATH", EnvironmentVariableTarget.User) ?? "";
                        if (PathContainsEntry(currentPath, installDir))
                        {
                            currentPath = RemovePathEntry(currentPath, installDir);
                            Environment.SetEnvironmentVariable("PATH", currentPath, EnvironmentVariableTarget.User);
                        }
                    } catch (Exception ex) {
                        uninstallError = ex;
                    }
                });

                if (uninstallError != null)
                {
                    lblStatus.Text = "Uninstall failed: " + uninstallError.Message;
                    lblStatus.ForeColor = Color.IndianRed;
                    btnUninstall.Enabled = true;
                    btnInstall.Enabled = true;
                    return;
                }
                
                lblStatus.Text = "Uninstallation complete!";
                progress.Style = ProgressBarStyle.Continuous;
                progress.Value = 100;
                btnUninstall.Visible = false;
                btnInstall.Enabled = true;
            }
        }

        private async void BtnInstall_Click(object sender, EventArgs e)
        {
            btnInstall.Enabled = false;
            btnBrowse.Enabled = false;
            txtPath.Enabled = false;
            clbLang.Enabled = false;
            btnInstall.BackColor = Color.Gray;
            btnInstall.ForeColor = Color.White;
            progress.Visible = true;
            progress.Value = 0;

            string targetDir = txtPath.Text;
            string selectedVersion = rbLite.Checked ? "lite" : rbPlus.Checked ? "plus" : "standard";
            string downloadedAsset = null;
            try
            {
                lblStatus.Text = "Fetching official GitHub release metadata...";
                progress.Style = ProgressBarStyle.Marquee;
                downloadedAsset = await Task.Run(() => DownloadVerifiedAsset(selectedVersion));
                lblStatus.Text = "Verified SHA-256; deploying the selected edition...";
                await Task.Run(() => DeployCoreEngine(targetDir, selectedVersion, downloadedAsset));
                progress.Style = ProgressBarStyle.Continuous;

                List<string> runtimeWarnings = new List<string>();
                for (int i = 1; i < clbLang.Items.Count; i++)
                {
                    if (!clbLang.GetItemChecked(i)) continue;
                    string langName = clbLang.Items[i].ToString().Replace(" Runtime", "");
                    if (!IsRuntimeAvailable(langName))
                        runtimeWarnings.Add(langName + ": not found on PATH (not installed by Block Setup)");
                }

                if (runtimeWarnings.Count > 0)
                {
                    lblStatus.Text = string.Format("Core installed; {0} optional runtime(s) need manual setup.", runtimeWarnings.Count);
                    MessageBox.Show(
                        "Block Engine core installation completed. Optional runtimes are detected only; this secure installer never runs package managers or arbitrary commands.\n\n" +
                        string.Join("\n", runtimeWarnings.ToArray()) +
                        "\n\nInstall runtimes from their official sources, then open a new terminal.",
                        "Optional runtime notice", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
                else
                {
                    lblStatus.Text = "Installed from the official release; SHA-256 verified.";
                }
                progress.Value = 100;
                btnInstall.Text = "Finish";
                btnInstall.BackColor = accentColor;
                btnInstall.ForeColor = bgColor;
                btnInstall.Enabled = true;
                btnInstall.Click -= BtnInstall_Click;
                btnInstall.Click += (s, ev) => this.Close();
            }
            catch (Exception ex)
            {
                lblStatus.Text = "Error: " + ex.Message;
                lblStatus.ForeColor = Color.IndianRed;
                btnInstall.Enabled = true;
                btnBrowse.Enabled = true;
                txtPath.Enabled = true;
                clbLang.Enabled = true;
                btnInstall.BackColor = accentColor;
                btnInstall.ForeColor = bgColor;
            }
            finally
            {
                try { if (!string.IsNullOrEmpty(downloadedAsset) && File.Exists(downloadedAsset)) File.Delete(downloadedAsset); } catch { }
            }
        }

        private void DeployCoreEngine(string installDir, string selectedVersion, string packagePath)
        {
            if (ContainsReparsePoint(installDir))
                throw new UnauthorizedAccessException("The selected installation path contains a reparse point and cannot be trusted.");
            if (!Directory.Exists(installDir))
                Directory.CreateDirectory(installDir);

            string exeFileName = "block.exe";
            string progId = "block_script";
            string progDesc = "Block Script";
            string mainExt = ".blk";
            string altExt = ".block";

            if (selectedVersion == "lite")
            {
                exeFileName = "block-lite.exe";
                progId = "blocklite_script";
                progDesc = "Block Lite Script";
                mainExt = ".blkl";
                altExt = ".blocklite";
            }
            else if (selectedVersion == "plus")
            {
                exeFileName = "block-plus.exe";
                progId = "blockplus_script";
                progDesc = "Block Plus Script";
                mainExt = ".blkp";
                altExt = ".blockplus";
            }

            foreach (var process in System.Diagnostics.Process.GetProcessesByName(Path.GetFileNameWithoutExtension(exeFileName)))
            {
                try { if (IsOwnedProcess(process, installDir)) { process.Kill(); process.WaitForExit(1000); } } catch { }
            }

            string exePath = Path.Combine(installDir, exeFileName);
            string iconPath = Path.Combine(installDir, "icon.ico");
            if (ContainsReparsePoint(exePath) || ContainsReparsePoint(iconPath))
                throw new UnauthorizedAccessException("The existing installation target contains a reparse point.");
            
            // Extract icon from embedded resource
            using (Stream iconStream = Assembly.GetExecutingAssembly().GetManifestResourceStream("icon.ico"))
            {
                if (iconStream != null)
                {
                    using (FileStream fs = new FileStream(iconPath, FileMode.Create))
                    {
                        iconStream.CopyTo(fs);
                    }
                }
            }
            
            if (string.IsNullOrWhiteSpace(packagePath) || !File.Exists(packagePath))
                throw new FileNotFoundException("Verified release package was not downloaded.", packagePath);
            string tempExtract = Path.Combine(Path.GetTempPath(), "BlockEngineInstaller_" + Guid.NewGuid().ToString("N"));
            try
            {
                Directory.CreateDirectory(tempExtract);
                ExtractVerifiedArchive(packagePath, tempExtract, exeFileName);
                InstallExecutableAtomically(Path.Combine(tempExtract, exeFileName), exePath);
            }
            finally
            {
                try { if (Directory.Exists(tempExtract)) Directory.Delete(tempExtract, true); } catch { }
            }

            // Keep other Block editions in a shared directory. Installing one edition
            // must not destroy an existing executable belonging to another edition.

            string currentPath = Environment.GetEnvironmentVariable("PATH", EnvironmentVariableTarget.User) ?? "";
            if (!PathContainsEntry(currentPath, installDir))
            {
                string updatedPath = string.IsNullOrWhiteSpace(currentPath) ? installDir : currentPath + ";" + installDir;
                Environment.SetEnvironmentVariable("PATH", updatedPath, EnvironmentVariableTarget.User);
            }

            // Unregister unselected version extensions to prevent conflicts
            if (selectedVersion == "lite")
            {
                UnregisterExtension(".blk", "block_script");
                UnregisterExtension(".block", "block_script");
                UnregisterExtension(".blkp", "blockplus_script");
                UnregisterExtension(".blockplus", "blockplus_script");
            }
            else if (selectedVersion == "standard")
            {
                UnregisterExtension(".blkl", "blocklite_script");
                UnregisterExtension(".blocklite", "blocklite_script");
                UnregisterExtension(".blkp", "blockplus_script");
                UnregisterExtension(".blockplus", "blockplus_script");
            }
            else if (selectedVersion == "plus")
            {
                UnregisterExtension(".blkl", "blocklite_script");
                UnregisterExtension(".blocklite", "blocklite_script");
                UnregisterExtension(".blk", "block_script");
                UnregisterExtension(".block", "block_script");
            }

            RegisterExtension(mainExt, progId, progDesc);
            RegisterExtension(altExt, progId, progDesc);

            using (RegistryKey key = Registry.CurrentUser.CreateSubKey(string.Format(@"Software\Classes\{0}\shell\open\command", progId)))
            {
                key.SetValue("", string.Format("\"{0}\" \"%1\"", exePath));
            }
            using (RegistryKey key = Registry.CurrentUser.CreateSubKey(string.Format(@"Software\Classes\{0}\DefaultIcon", progId)))
            {
                key.SetValue("", string.Format("\"{0}\",0", iconPath));
            }

            using (RegistryKey key = Registry.CurrentUser.CreateSubKey(@"Software\Classes\block_engine_install"))
            {
                key.SetValue("Version", selectedVersion);
                key.SetValue("InstallDir", installDir);
                key.SetValue("ExeName", exeFileName);
            }

            // Register Windows Add/Remove Programs (Control Panel & Apps Settings)
            try
            {
                using (RegistryKey unkey = Registry.CurrentUser.CreateSubKey(@"Software\Microsoft\Windows\CurrentVersion\Uninstall\BlockEngine"))
                {
                    unkey.SetValue("DisplayName", "Block Engine (" + selectedVersion.ToUpper() + ")");
                    unkey.SetValue("DisplayVersion", InstallerVersion);
                    unkey.SetValue("Publisher", "Block Language Team");
                    unkey.SetValue("InstallLocation", installDir);
                    unkey.SetValue("DisplayIcon", iconPath);
                    unkey.SetValue("UninstallString", string.Format("\"{0}\"", Application.ExecutablePath));
                    unkey.SetValue("NoModify", 1);
                }
            }
            catch { }
            
            NativeMethods.SHChangeNotify(0x08000000, 0x0000, IntPtr.Zero, IntPtr.Zero);
        }

        private sealed class ReleaseInfo
        {
            public string tag_name { get; set; }
            public List<ReleaseAssetInfo> assets { get; set; }
        }

        private sealed class ReleaseAssetInfo
        {
            public string name { get; set; }
            public string browser_download_url { get; set; }
        }

        private string DownloadVerifiedAsset(string selectedVersion)
        {
            string assetName = selectedVersion == "lite" ? "block-lite.zip" : selectedVersion == "plus" ? "block-plus.zip" : "block.zip";
            string apiUrl = OfficialApiBase + InstallerVersion;
            ReleaseInfo release = Json.Deserialize<ReleaseInfo>(Encoding.UTF8.GetString(DownloadRemoteBytes(apiUrl, MaxReleaseMetadataBytes)));
            if (release == null || !string.Equals(release.tag_name, "v" + InstallerVersion, StringComparison.OrdinalIgnoreCase))
                throw new InvalidDataException("Official release tag does not match installer version v" + InstallerVersion + ".");
            ReleaseAssetInfo package = FindAsset(release, assetName);
            ReleaseAssetInfo sums = FindAsset(release, "SHA256SUMS.txt");
            if (package == null || sums == null) throw new InvalidDataException("Official release is missing the selected package or SHA256SUMS.txt.");
            ValidateReleaseAssetUri(package.browser_download_url, assetName);
            ValidateReleaseAssetUri(sums.browser_download_url, "SHA256SUMS.txt");

            string sumsText = Encoding.UTF8.GetString(DownloadRemoteBytes(sums.browser_download_url, MaxReleaseMetadataBytes));
            string expectedHash = FindHash(sumsText, assetName);
            if (string.IsNullOrEmpty(expectedHash)) throw new InvalidDataException("SHA256SUMS.txt has no digest for " + assetName + ".");

            string outputPath = Path.Combine(Path.GetTempPath(), "BlockEngineInstaller-" + Guid.NewGuid().ToString("N") + ".zip");
            try
            {
                string actualHash = DownloadRemoteFile(package.browser_download_url, outputPath, MaxReleaseAssetBytes);
                if (!string.Equals(actualHash, expectedHash, StringComparison.OrdinalIgnoreCase))
                    throw new InvalidDataException("SHA-256 verification failed for " + assetName + ".");
                return outputPath;
            }
            catch
            {
                try { if (File.Exists(outputPath)) File.Delete(outputPath); } catch { }
                throw;
            }
        }

        private static ReleaseAssetInfo FindAsset(ReleaseInfo release, string name)
        {
            if (release == null || release.assets == null) return null;
            foreach (ReleaseAssetInfo asset in release.assets)
                if (asset != null && string.Equals(asset.name, name, StringComparison.Ordinal)) return asset;
            return null;
        }

        private static string FindHash(string sumsText, string assetName)
        {
            foreach (string line in (sumsText ?? "").Replace("\r", "").Split('\n'))
            {
                Match match = Regex.Match(line.Trim(), "^([0-9a-fA-F]{64})\\s+(.+)$");
                if (match.Success && string.Equals(match.Groups[2].Value.Trim(), assetName, StringComparison.Ordinal))
                    return match.Groups[1].Value.ToLowerInvariant();
            }
            return null;
        }

        private static byte[] DownloadRemoteBytes(string url, int maxBytes)
        {
            Uri uri = ValidateRemoteUri(url);
            HttpWebRequest request = CreateGitHubRequest(uri, 20000, "application/json, text/plain, */*");
            try
            {
                using (HttpWebResponse response = (HttpWebResponse)request.GetResponse())
                {
                    ValidateResponseUri(response.ResponseUri);
                    if (response.StatusCode != HttpStatusCode.OK) throw new InvalidDataException("GitHub returned HTTP " + (int)response.StatusCode + ".");
                    using (Stream input = response.GetResponseStream())
                    using (MemoryStream output = new MemoryStream())
                    {
                        byte[] buffer = new byte[8192];
                        int read;
                        while ((read = input.Read(buffer, 0, buffer.Length)) > 0)
                        {
                            if (output.Length + read > maxBytes) throw new InvalidDataException("GitHub response exceeds the safe limit.");
                            output.Write(buffer, 0, read);
                        }
                        return output.ToArray();
                    }
                }
            }
            catch (WebException ex)
            {
                throw CreateNetworkFailure(ex, uri);
            }
        }

        private static void ValidateReleaseAssetUri(string url, string assetName)
        {
            Uri uri = ValidateRemoteUri(url);
            string expectedPrefix = "/" + OfficialRepository + "/releases/download/v" + InstallerVersion + "/";
            if (!string.Equals(uri.Host, "github.com", StringComparison.OrdinalIgnoreCase) ||
                !uri.AbsolutePath.StartsWith(expectedPrefix, StringComparison.OrdinalIgnoreCase) ||
                !string.Equals(uri.AbsolutePath.Substring(expectedPrefix.Length), assetName, StringComparison.Ordinal))
                throw new InvalidDataException("GitHub release asset is not an official v" + InstallerVersion + " asset: " + assetName + ".");
        }

        private static string DownloadRemoteFile(string url, string outputPath, long maxBytes)
        {
            Uri uri = ValidateRemoteUri(url);
            HttpWebRequest request = CreateGitHubRequest(uri, 30000, "application/octet-stream, */*");
            try
            {
                using (HttpWebResponse response = (HttpWebResponse)request.GetResponse())
                {
                    ValidateResponseUri(response.ResponseUri);
                    if (response.StatusCode != HttpStatusCode.OK) throw new InvalidDataException("GitHub asset returned HTTP " + (int)response.StatusCode + ".");
                    using (Stream input = response.GetResponseStream())
                    using (FileStream output = new FileStream(outputPath, FileMode.CreateNew, FileAccess.Write, FileShare.None))
                    using (SHA256 sha = SHA256.Create())
                    {
                        byte[] buffer = new byte[1024 * 64];
                        long total = 0;
                        int read;
                        while ((read = input.Read(buffer, 0, buffer.Length)) > 0)
                        {
                            total += read;
                            if (total > maxBytes) throw new InvalidDataException("Release asset exceeds the safe size limit.");
                            sha.TransformBlock(buffer, 0, read, buffer, 0);
                            output.Write(buffer, 0, read);
                        }
                        sha.TransformFinalBlock(new byte[0], 0, 0);
                        return BitConverter.ToString(sha.Hash).Replace("-", "").ToLowerInvariant();
                    }
                }
            }
            catch (WebException ex)
            {
                throw CreateNetworkFailure(ex, uri);
            }
        }

        private static HttpWebRequest CreateGitHubRequest(Uri uri, int timeout, string accept)
        {
            HttpWebRequest request = (HttpWebRequest)WebRequest.Create(uri);
            request.Method = "GET";
            request.UserAgent = "BlockSetup/" + InstallerVersion;
            request.Accept = accept;
            request.Timeout = timeout;
            request.ReadWriteTimeout = timeout;
            request.AllowAutoRedirect = true;
            request.MaximumAutomaticRedirections = 5;
            request.AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate;
            request.ProtocolVersion = HttpVersion.Version11;
            return request;
        }

        private static Exception CreateNetworkFailure(WebException error, Uri endpoint)
        {
            string host = endpoint == null ? "GitHub" : endpoint.Host;
            HttpWebResponse response = error.Response as HttpWebResponse;
            if (response != null)
            {
                return new InvalidDataException(
                    host + " returned HTTP " + (int)response.StatusCode + " (" + response.StatusCode + ").",
                    error);
            }
            switch (error.Status)
            {
                case WebExceptionStatus.SecureChannelFailure:
                case WebExceptionStatus.TrustFailure:
                    return new InvalidOperationException(
                        "Secure TLS 1.2 connection to " + host + " failed. Check the Windows date/time, install current root certificates, and ensure HTTPS inspection or a proxy is not replacing GitHub's certificate.",
                        error);
                case WebExceptionStatus.NameResolutionFailure:
                case WebExceptionStatus.ProxyNameResolutionFailure:
                    return new InvalidOperationException(
                        "Could not resolve " + host + ". Check DNS, proxy, and internet access, then retry.",
                        error);
                case WebExceptionStatus.Timeout:
                    return new TimeoutException(
                        "The secure connection to " + host + " timed out. Check the firewall or proxy, then retry.",
                        error);
                case WebExceptionStatus.ConnectFailure:
                    return new InvalidOperationException(
                        "Could not connect to " + host + " over HTTPS. Check the firewall, proxy, and internet access, then retry.",
                        error);
                default:
                    return new InvalidOperationException(
                        "The verified download from " + host + " failed (" + error.Status + "). Retry after checking network and proxy settings.",
                        error);
            }
        }

        private static Uri ValidateRemoteUri(string url)
        {
            Uri uri;
            if (!Uri.TryCreate(url, UriKind.Absolute, out uri) || uri.Scheme != Uri.UriSchemeHttps)
                throw new InvalidDataException("Installer accepts HTTPS GitHub URLs only.");
            if (!string.Equals(uri.Host, "api.github.com", StringComparison.OrdinalIgnoreCase) &&
                !string.Equals(uri.Host, "github.com", StringComparison.OrdinalIgnoreCase) &&
                !string.Equals(uri.Host, "objects.githubusercontent.com", StringComparison.OrdinalIgnoreCase) &&
                !string.Equals(uri.Host, "release-assets.githubusercontent.com", StringComparison.OrdinalIgnoreCase))
                throw new InvalidDataException("Installer rejected a non-GitHub download host.");
            return uri;
        }

        private static void ValidateResponseUri(Uri uri)
        {
            if (uri == null) throw new InvalidDataException("GitHub response has no final URI.");
            ValidateRemoteUri(uri.AbsoluteUri);
        }

        private static void ExtractVerifiedArchive(string archivePath, string destination, string executableName)
        {
            using (ZipArchive archive = ZipFile.OpenRead(archivePath))
            {
                ZipArchiveEntry executable = null;
                int fileCount = 0;
                foreach (ZipArchiveEntry entry in archive.Entries)
                {
                    string normalized = (entry.FullName ?? "").Replace('\\', '/');
                    if (normalized.StartsWith("/", StringComparison.Ordinal) || normalized.IndexOf("../", StringComparison.Ordinal) >= 0 ||
                        normalized.IndexOf("/..", StringComparison.Ordinal) >= 0)
                        throw new InvalidDataException("Release archive contains an unsafe path.");
                    if (string.IsNullOrEmpty(entry.Name)) continue;
                    fileCount++;
                    if (!string.Equals(normalized, executableName, StringComparison.Ordinal))
                        throw new InvalidDataException("Release archive contains an unexpected file: " + entry.FullName);
                    if (entry.Length <= 0 || entry.Length > MaxReleaseAssetBytes) throw new InvalidDataException("Release executable has an invalid size.");
                    executable = entry;
                }
                if (fileCount != 1 || executable == null) throw new InvalidDataException("Release archive must contain exactly one executable.");
                string target = Path.GetFullPath(Path.Combine(destination, executable.Name));
                string root = Path.GetFullPath(destination).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar) + Path.DirectorySeparatorChar;
                if (!target.StartsWith(root, StringComparison.OrdinalIgnoreCase)) throw new InvalidDataException("Release archive path escaped staging directory.");
                using (Stream input = executable.Open())
                using (FileStream output = new FileStream(target, FileMode.CreateNew, FileAccess.Write, FileShare.None))
                {
                    byte[] buffer = new byte[64 * 1024];
                    long total = 0;
                    int read;
                    while ((read = input.Read(buffer, 0, buffer.Length)) > 0)
                    {
                        total += read;
                        if (total > MaxReleaseAssetBytes) throw new InvalidDataException("Extracted release executable exceeds the safe size limit.");
                        output.Write(buffer, 0, read);
                    }
                }
            }
        }

        private void InstallExecutableAtomically(string sourcePath, string targetPath)
        {
            string stagedPath = targetPath + ".staging-" + Guid.NewGuid().ToString("N");
            string backupPath = targetPath + ".backup-" + Guid.NewGuid().ToString("N");
            try
            {
                File.Copy(sourcePath, stagedPath, false);
                if (File.Exists(targetPath))
                {
                    try
                    {
                        File.Replace(stagedPath, targetPath, null);
                    }
                    catch (Exception ex)
                    {
                        if (!(ex is PlatformNotSupportedException) && !(ex is IOException)) throw;
                        // Preserve the existing executable until the staged file is
                        // in place. If the second move fails, restore the backup.
                        File.Move(targetPath, backupPath);
                        try
                        {
                            File.Move(stagedPath, targetPath);
                        }
                        catch
                        {
                            if (File.Exists(backupPath) && !File.Exists(targetPath))
                                File.Move(backupPath, targetPath);
                            throw;
                        }
                        if (File.Exists(backupPath)) File.Delete(backupPath);
                    }
                }
                else
                {
                    File.Move(stagedPath, targetPath);
                }
            }
            finally
            {
                try { if (File.Exists(stagedPath)) File.Delete(stagedPath); } catch { }
                try { if (File.Exists(backupPath)) File.Delete(backupPath); } catch { }
            }
        }

        private void UnregisterExtension(string ext, string progId)
        {
            bool ownsAssociation = false;
            try
            {
                using (RegistryKey extKey = Registry.CurrentUser.OpenSubKey(@"Software\Classes\" + ext))
                {
                    string currentProgId = extKey == null ? null : extKey.GetValue("") as string;
                    ownsAssociation = string.Equals(currentProgId, progId, StringComparison.OrdinalIgnoreCase);
                }
                if (ownsAssociation)
                    Registry.CurrentUser.DeleteSubKeyTree(@"Software\Classes\" + ext, false);
            }
            catch { }

            if (ownsAssociation)
            {
                try { Registry.CurrentUser.DeleteSubKeyTree(@"Software\Classes\" + progId, false); } catch { }
            }
        }
        
        private bool IsRuntimeAvailable(string runtimeName)
        {
            string[] executableNames;
            if (!runtimeExecutables.TryGetValue(runtimeName, out executableNames)) return false;

            List<string> pathEntries = new List<string>();
            string processPath = Environment.GetEnvironmentVariable("PATH", EnvironmentVariableTarget.Process);
            string userPath = Environment.GetEnvironmentVariable("PATH", EnvironmentVariableTarget.User);
            foreach (string value in new[] { processPath, userPath })
            {
                if (string.IsNullOrWhiteSpace(value)) continue;
                pathEntries.AddRange(value.Split(new[] { ';' }, StringSplitOptions.RemoveEmptyEntries));
            }

            foreach (string directory in pathEntries)
            {
                foreach (string executableName in executableNames)
                {
                    try
                    {
                        if (File.Exists(Path.Combine(directory.Trim().Trim('"'), executableName))) return true;
                    }
                    catch { }
                }
            }
            return false;
        }

        private void RegisterExtension(string ext, string progId, string description)
        {
            using (RegistryKey key = Registry.CurrentUser.CreateSubKey(@"Software\Classes\" + ext))
            {
                key.SetValue("", progId);
            }
            using (RegistryKey key = Registry.CurrentUser.CreateSubKey(@"Software\Classes\" + progId))
            {
                key.SetValue("", description);
            }
        }
    }

    static class NativeMethods
    {
        public const int WM_NCLBUTTONDOWN = 0xA1;
        public const int HT_CAPTION = 0x2;
        [System.Runtime.InteropServices.DllImport("user32.dll")]
        public static extern int SendMessage(IntPtr hWnd, int Msg, int wParam, int lParam);
        [System.Runtime.InteropServices.DllImport("user32.dll")]
        public static extern bool ReleaseCapture();
        [System.Runtime.InteropServices.DllImport("shell32.dll")]
        public static extern void SHChangeNotify(uint wEventId, uint uFlags, IntPtr dwItem1, IntPtr dwItem2);
    }

    static class Program
    {
        [STAThread]
        static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new InstallerForm());
        }
    }
}
