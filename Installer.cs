using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.IO;
using System.IO.Compression;
using System.Diagnostics;
using System.Net;
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

        private Dictionary<string, string> langWingetIds = new Dictionary<string, string>()
        {
            {"Python", "Python.Python.3.10"},
            {"NodeJS (JS/TS)", "OpenJS.NodeJS"},
            {"PHP", "PHP.PHP"},
            {"Ruby", "RubyInstallerTeam.Ruby"},
            {"Lua", "Lua.Lua"},
            {"SQLite", "SQLite.SQLite"},
            {"Go (Block+ Only)", "GoLang.Go"},
            {"Rust (Block+ Only)", "Rustlang.Rustup"},
            {"Java JDK (Block+ Only)", "Oracle.JDK.21"},
            {"Dart (Block+ Only)", "Dart.Dart"},
            {"Zig (Block+ Only)", "zig.zig"},
            {"Perl (Block+ Only)", "StrawberryPerl.StrawberryPerl"},
            {"R (Block+ Only)", "RProject.R"}
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

        private Dictionary<string, string> runtimeChocolateyIds = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            {"PHP", "php"},
            {"Ruby", "ruby"},
            {"Lua", "lua"},
            {"SQLite", "sqlite"},
            {"Go (Block+ Only)", "golang"},
            {"Rust (Block+ Only)", "rust"},
            {"Java JDK (Block+ Only)", "temurin"},
            {"Dart (Block+ Only)", "dart-sdk"},
            {"Zig (Block+ Only)", "zig"},
            {"Perl (Block+ Only)", "strawberryperl"},
            {"R (Block+ Only)", "r.project"}
        };

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
                    lblVersionDesc.Text = "Flagship edition. Unlocks 15+ advanced runtimes, Zero-IO, and Winget installer.";
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
            foreach (var key in langWingetIds.Keys)
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

            try
            {
                lblStatus.Text = "Deploying Block Engine Core and Registry entries...";
                progress.Style = ProgressBarStyle.Marquee;
                await Task.Run(() => DeployCoreEngine(targetDir, selectedVersion));
                
                await Task.Delay(800);
                progress.Style = ProgressBarStyle.Continuous;

                List<string> runtimeWarnings = new List<string>();
                for (int i = 1; i < clbLang.Items.Count; i++)
                {
                    if (clbLang.GetItemChecked(i))
                    {
                        string itemText = clbLang.Items[i].ToString();
                        string langName = itemText.Replace(" Runtime", "");
                        
                        if (langWingetIds.ContainsKey(langName))
                        {
                            if (IsRuntimeAvailable(langName))
                            {
                                lblStatus.Text = string.Format("{0} is already available; skipping install.", langName);
                                continue;
                            }

                            string wingetId = langWingetIds[langName];
                            lblStatus.Text = string.Format("Installing {0} via Winget...", langName);
                            progress.Style = ProgressBarStyle.Marquee;
                            try
                            {
                                await RunRuntimeInstall(langName, wingetId);
                            }
                            catch (Exception runtimeError)
                            {
                                // Optional runtimes must not roll back the core
                                // engine or leave the installer in a failed state.
                                runtimeWarnings.Add(string.Format("{0}: {1}", langName, runtimeError.Message));
                            }
                            progress.Style = ProgressBarStyle.Continuous;
                        }
                    }
                }

                if (runtimeWarnings.Count > 0)
                {
                    lblStatus.Text = string.Format("Core installed; {0} optional runtime(s) skipped.", runtimeWarnings.Count);
                    MessageBox.Show(
                        "Block Engine core installation completed. The following optional runtimes were not installed:\n\n" +
                        string.Join("\n", runtimeWarnings.ToArray()) +
                        "\n\nYou can install them later and run the installer again. Open a new terminal after setup; use 'block workspace set <folder>' to discover projects without changing directories.",
                        "Optional runtime warning", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                }
                else
                {
                    lblStatus.Text = "Installed. Open a new terminal; project paths are resolved automatically.";
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
        }

        private void DeployCoreEngine(string installDir, string selectedVersion)
        {
            if (!Directory.Exists(installDir))
                Directory.CreateDirectory(installDir);

            string exeFileName = "block.exe";
            string resourceName = "block.zip";
            string progId = "block_script";
            string progDesc = "Block Script";
            string mainExt = ".blk";
            string altExt = ".block";

            if (selectedVersion == "lite")
            {
                exeFileName = "block-lite.exe";
                resourceName = "block-lite.zip";
                progId = "blocklite_script";
                progDesc = "Block Lite Script";
                mainExt = ".blkl";
                altExt = ".blocklite";
            }
            else if (selectedVersion == "plus")
            {
                exeFileName = "block-plus.exe";
                resourceName = "block-plus.zip";
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
            
            // Extract verified embedded bundle (never fall back to untrusted current directory)
            using (Stream stream = Assembly.GetExecutingAssembly().GetManifestResourceStream(resourceName))
            {
                if (stream != null)
                {
                    string tempZip = Path.Combine(Path.GetTempPath(), "BlockEngineInstaller_" + Guid.NewGuid().ToString("N") + ".zip");
                    string tempExtract = Path.Combine(Path.GetTempPath(), "BlockEngineInstaller_" + Guid.NewGuid().ToString("N"));
                    try
                    {
                        using (FileStream fs = new FileStream(tempZip, FileMode.CreateNew, FileAccess.Write, FileShare.None))
                        {
                            stream.CopyTo(fs);
                        }

                        Directory.CreateDirectory(tempExtract);
                        System.IO.Compression.ZipFile.ExtractToDirectory(tempZip, tempExtract);

                        if (File.Exists(Path.Combine(tempExtract, exeFileName)))
                            InstallExecutableAtomically(Path.Combine(tempExtract, exeFileName), exePath);
                        else if (File.Exists(Path.Combine(tempExtract, "block.exe")))
                            InstallExecutableAtomically(Path.Combine(tempExtract, "block.exe"), exePath);
                        else
                            throw new Exception(exeFileName + " not found in embedded archive");
                    }
                    finally
                    {
                        try { if (File.Exists(tempZip)) File.Delete(tempZip); } catch { }
                        try { if (Directory.Exists(tempExtract)) Directory.Delete(tempExtract, true); } catch { }
                    }
                }
                else
                {
                    throw new Exception("Embedded resource missing: " + resourceName);
                }
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
        
        private async Task RunWingetInstall(string packageId)
        {
            await Task.Run(() => {
                try
                {
                    string extraArgs = packageId == "Rustlang.Rustup" ? "--override \"-y\"" : "";
                    ProcessStartInfo psi = new ProcessStartInfo();
                    psi.FileName = "winget";
                    psi.Arguments = string.Format("install -e --id {0} --accept-package-agreements --accept-source-agreements --silent {1}", packageId, extraArgs);
                    psi.UseShellExecute = false;
                    psi.CreateNoWindow = true;
                    
                    using (Process p = Process.Start(psi))
                    {
                        p.WaitForExit();
                        if (p.ExitCode != 0)
                            throw new InvalidOperationException(string.Format("Winget exited with code {0} for {1}.", p.ExitCode, packageId));
                    }
                }
                catch (Exception)
                {
                    throw;
                }
            });
        }

        private async Task RunRuntimeInstall(string runtimeName, string wingetId)
        {
            Exception wingetError = null;
            try
            {
                await RunWingetInstall(wingetId);
                return;
            }
            catch (Exception ex)
            {
                wingetError = ex;
            }

            string chocolateyId;
            if (!runtimeChocolateyIds.TryGetValue(runtimeName, out chocolateyId) || !IsCommandAvailable("choco.exe"))
                throw new InvalidOperationException(string.Format("Winget failed ({0}). Install App Installer or Chocolatey, then retry.", wingetError.Message));

            try
            {
                await RunChocolateyInstall(chocolateyId);
            }
            catch (Exception chocolateyError)
            {
                throw new InvalidOperationException(string.Format("Winget failed ({0}); Chocolatey fallback also failed ({1}).", wingetError.Message, chocolateyError.Message));
            }
        }

        private async Task RunChocolateyInstall(string packageId)
        {
            await Task.Run(() => {
                ProcessStartInfo psi = new ProcessStartInfo();
                psi.FileName = "choco.exe";
                psi.Arguments = string.Format("install {0} -y --no-progress", packageId);
                psi.UseShellExecute = false;
                psi.CreateNoWindow = true;

                using (Process p = Process.Start(psi))
                {
                    if (p == null) throw new InvalidOperationException("Chocolatey process could not be started.");
                    p.WaitForExit();
                    if (p.ExitCode != 0)
                        throw new InvalidOperationException(string.Format("Chocolatey exited with code {0}.", p.ExitCode));
                }
            });
        }

        private bool IsCommandAvailable(string executableName)
        {
            string processPath = Environment.GetEnvironmentVariable("PATH", EnvironmentVariableTarget.Process);
            string userPath = Environment.GetEnvironmentVariable("PATH", EnvironmentVariableTarget.User);
            foreach (string value in new[] { processPath, userPath })
            {
                if (string.IsNullOrWhiteSpace(value)) continue;
                foreach (string directory in value.Split(new[] { ';' }, StringSplitOptions.RemoveEmptyEntries))
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
