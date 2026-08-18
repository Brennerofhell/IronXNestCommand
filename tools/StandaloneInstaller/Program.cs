using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.IO;
using System.IO.Compression;
using System.Reflection;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using Microsoft.Win32;

namespace IronXNestCommand.Installer
{
    static class Program
    {
        [DllImport("user32.dll")]
        private static extern bool SetProcessDPIAware();

        [STAThread]
        static void Main()
        {
            try { SetProcessDPIAware(); } catch { }
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new MainForm());
        }
    }

    public class MainForm : Form
    {
        // Design Farbtöne (Anthropic Dieselpunk Design)
        public static readonly Color cBg = Color.FromArgb(24, 24, 27);         // #18181B Dark Graphite
        public static readonly Color cCard = Color.FromArgb(37, 37, 41);       // #252529 Card Surface
        public static readonly Color cCardLight = Color.FromArgb(48, 48, 54);  // #303036
        public static readonly Color cTerracotta = Color.FromArgb(217, 119, 87);// #D97757
        public static readonly Color cText = Color.FromArgb(250, 249, 245);     // #FAF9F5
        public static readonly Color cMuted = Color.FromArgb(161, 161, 170);    // #A1A1AA
        public static readonly Color cEmerald = Color.FromArgb(16, 185, 129);   // #10B981
        public static readonly Color cRed = Color.FromArgb(239, 68, 68);        // #EF4444
        public static readonly Color cBorder = Color.FromArgb(63, 63, 70);      // #3F3F46
        public static readonly Color cBorderLight = Color.FromArgb(82, 82, 91); // #52525B

        public const string GameExeName = "Iron Nest Heavy Turret Simulator.exe";
        public const string ModVersion = "0.1.5";

        // Navigation
        private Panel pnlHeader;
        private Panel pnlTabBar;
        private Button btnTabInstall;
        private Button btnTabUninstall;
        private Button btnTabInfo;
        private Panel pnlContainer;

        // Tabs
        private InstallTab tabInstall;
        private UninstallTab tabUninstall;
        private InfoTab tabInfo;

        public string CurrentGamePath { get; set; }

        public MainForm()
        {
            this.Text = "IronXNestCommand // Mod Setup & Manager v" + ModVersion;
            this.Size = new Size(640, 620);
            this.MinimumSize = new Size(640, 620);
            this.StartPosition = FormStartPosition.CenterScreen;
            this.FormBorderStyle = FormBorderStyle.FixedSingle;
            this.MaximizeBox = false;
            this.BackColor = cBg;
            this.ForeColor = cText;
            this.Font = new Font("Segoe UI", 9F, FontStyle.Regular);

            InitializeUI();
            DetectGame();
            SwitchTab(0);
        }

        private void InitializeUI()
        {
            // 1. Header
            pnlHeader = new Panel { Dock = DockStyle.Top, Height = 75, BackColor = cCard };
            pnlHeader.Paint += (s, e) =>
            {
                using (var pen = new Pen(cTerracotta, 3))
                    e.Graphics.DrawLine(pen, 0, 0, pnlHeader.Width, 0);
                using (var pen = new Pen(cBorder))
                    e.Graphics.DrawLine(pen, 0, pnlHeader.Height - 1, pnlHeader.Width, pnlHeader.Height - 1);
            };

            Label lblTitle = new Label
            {
                Text = "✦ IRON X NEST COMMAND",
                Font = new Font("Segoe UI", 13F, FontStyle.Bold),
                ForeColor = cTerracotta,
                Location = new Point(24, 14),
                AutoSize = true
            };

            Label lblSub = new Label
            {
                Text = "Co-op Lobby Overlay, Feind-Schutz & Lochkarten-Sync · Setup Suite",
                Font = new Font("Segoe UI", 9F, FontStyle.Regular),
                ForeColor = cMuted,
                Location = new Point(25, 42),
                AutoSize = true
            };

            Label lblVer = new Label
            {
                Text = "v" + ModVersion,
                Font = new Font("Consolas", 9F, FontStyle.Bold),
                ForeColor = cTerracotta,
                Location = new Point(560, 18),
                AutoSize = true
            };

            pnlHeader.Controls.Add(lblTitle);
            pnlHeader.Controls.Add(lblSub);
            pnlHeader.Controls.Add(lblVer);
            this.Controls.Add(pnlHeader);

            // 2. Tab-Bar
            pnlTabBar = new Panel { Dock = DockStyle.Top, Height = 42, BackColor = cBg };
            pnlTabBar.Paint += (s, e) =>
            {
                using (var pen = new Pen(cBorder))
                    e.Graphics.DrawLine(pen, 0, pnlTabBar.Height - 1, pnlTabBar.Width, pnlTabBar.Height - 1);
            };

            btnTabInstall = CreateTabButton("📥 Installation & Update", 20, 0);
            btnTabUninstall = CreateTabButton("🗑️ Deinstallation & Cleanup", 220, 0);
            btnTabInfo = CreateTabButton("ℹ️ Info & Verzeichnisse", 440, 0);

            btnTabInstall.Click += (s, e) => SwitchTab(0);
            btnTabUninstall.Click += (s, e) => SwitchTab(1);
            btnTabInfo.Click += (s, e) => SwitchTab(2);

            pnlTabBar.Controls.Add(btnTabInstall);
            pnlTabBar.Controls.Add(btnTabUninstall);
            pnlTabBar.Controls.Add(btnTabInfo);
            this.Controls.Add(pnlTabBar);

            // 3. Tab Container
            pnlContainer = new Panel { Dock = DockStyle.Fill, BackColor = cBg, Padding = new Padding(20) };
            this.Controls.Add(pnlContainer);

            // Tab Views initialisieren
            tabInstall = new InstallTab(this) { Dock = DockStyle.Fill };
            tabUninstall = new UninstallTab(this) { Dock = DockStyle.Fill };
            tabInfo = new InfoTab(this) { Dock = DockStyle.Fill };
        }

        private Button CreateTabButton(string text, int x, int y)
        {
            Button btn = new Button
            {
                Text = text,
                Location = new Point(x, y),
                Size = new Size(195, 41),
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 9F, FontStyle.Bold),
                ForeColor = cMuted,
                BackColor = cBg,
                Cursor = Cursors.Hand
            };
            btn.FlatAppearance.BorderSize = 0;
            return btn;
        }

        public void SwitchTab(int index)
        {
            btnTabInstall.ForeColor = (index == 0) ? cTerracotta : cMuted;
            btnTabUninstall.ForeColor = (index == 1) ? cTerracotta : cMuted;
            btnTabInfo.ForeColor = (index == 2) ? cTerracotta : cMuted;

            pnlContainer.Controls.Clear();
            if (index == 0) { pnlContainer.Controls.Add(tabInstall); tabInstall.RefreshState(); }
            else if (index == 1) { pnlContainer.Controls.Add(tabUninstall); tabUninstall.RefreshState(); }
            else if (index == 2) { pnlContainer.Controls.Add(tabInfo); tabInfo.RefreshState(); }
        }

        public void DetectGame()
        {
            string[] candidates = new string[]
            {
                @"C:\Program Files (x86)\Steam\steamapps\common\Iron Nest Heavy Turret Simulator",
                @"C:\Program Files\Steam\steamapps\common\Iron Nest Heavy Turret Simulator",
                @"D:\SteamLibrary\steamapps\common\Iron Nest Heavy Turret Simulator",
                @"E:\SteamLibrary\steamapps\common\Iron Nest Heavy Turret Simulator",
                @"F:\SteamLibrary\steamapps\common\Iron Nest Heavy Turret Simulator",
                @"G:\SteamLibrary\steamapps\common\Iron Nest Heavy Turret Simulator"
            };

            foreach (var path in candidates)
            {
                if (File.Exists(Path.Combine(path, GameExeName)))
                {
                    CurrentGamePath = path;
                    return;
                }
            }

            try
            {
                using (var key = Registry.CurrentUser.OpenSubKey(@"Software\Valve\Steam"))
                {
                    if (key != null)
                    {
                        var steamPath = key.GetValue("SteamPath") as string;
                        if (!string.IsNullOrEmpty(steamPath))
                        {
                            var regGame = Path.Combine(steamPath, "steamapps", "common", "Iron Nest Heavy Turret Simulator");
                            if (File.Exists(Path.Combine(regGame, GameExeName)))
                            {
                                CurrentGamePath = regGame;
                                return;
                            }
                        }
                    }
                }
            }
            catch { }

            CurrentGamePath = @"C:\Program Files (x86)\Steam\steamapps\common\Iron Nest Heavy Turret Simulator";
        }
    }

    // =========================================================================
    // TAB 1: INSTALLATION & UPDATE
    // =========================================================================
    public class InstallTab : UserControl
    {
        private MainForm parent;
        private TextBox txtPath;
        private Button btnBrowse;
        private Label lblGameStatus;
        private Label lblModStatus;
        private CheckBox chkBep;
        private CheckBox chkMelon;
        private Label lblLoaderInfo;
        private Button btnInstall;
        private Button btnLaunch;
        private ProgressBar progressBar;
        private Label lblLog;

        public InstallTab(MainForm form)
        {
            this.parent = form;
            this.BackColor = MainForm.cBg;
            InitializeComponent();
        }

        private void InitializeComponent()
        {
            // Pfad-Label
            Label lblPathHead = new Label
            {
                Text = "SPIELVERZEICHNIS (AUTOMATISCH ERKANNT):",
                Font = new Font("Segoe UI", 8F, FontStyle.Bold),
                ForeColor = MainForm.cMuted,
                Location = new Point(4, 8),
                AutoSize = true
            };

            txtPath = new TextBox
            {
                Location = new Point(4, 30),
                Size = new Size(465, 26),
                BackColor = MainForm.cCard,
                ForeColor = MainForm.cText,
                BorderStyle = BorderStyle.FixedSingle,
                Font = new Font("Segoe UI", 9F)
            };
            txtPath.TextChanged += (s, e) =>
            {
                parent.CurrentGamePath = txtPath.Text.Trim();
                RefreshState();
            };

            btnBrowse = CreateStyledBtn("Durchsuchen...", 478, 28, 105, 29, MainForm.cCardLight, MainForm.cText);
            btnBrowse.Click += (s, e) =>
            {
                using (FolderBrowserDialog fbd = new FolderBrowserDialog())
                {
                    fbd.Description = "Wähle das Hauptverzeichnis von 'Iron Nest Heavy Turret Simulator':";
                    if (Directory.Exists(txtPath.Text)) fbd.SelectedPath = txtPath.Text;
                    if (fbd.ShowDialog() == DialogResult.OK) txtPath.Text = fbd.SelectedPath;
                }
            };

            // Status Card
            Panel pnlCard = new Panel
            {
                Location = new Point(4, 72),
                Size = new Size(580, 172),
                BackColor = MainForm.cCard
            };
            pnlCard.Paint += (s, e) =>
            {
                using (var pen = new Pen(MainForm.cBorder))
                    e.Graphics.DrawRectangle(pen, 0, 0, pnlCard.Width - 1, pnlCard.Height - 1);
            };

            lblGameStatus = new Label
            {
                Text = "● Spiel: Prüfe...",
                Location = new Point(16, 16),
                AutoSize = true,
                Font = new Font("Segoe UI", 9.5F, FontStyle.Bold),
                ForeColor = MainForm.cMuted
            };

            lblModStatus = new Label
            {
                Text = "● Mod-Status: Unbekannt",
                Location = new Point(16, 48),
                AutoSize = true,
                Font = new Font("Segoe UI", 9F, FontStyle.Bold),
                ForeColor = MainForm.cMuted
            };

            Label lblLoaderTitle = new Label
            {
                Text = "Ziel-ModLoader (Automatisch):",
                Location = new Point(16, 85),
                AutoSize = true,
                ForeColor = MainForm.cMuted,
                Font = new Font("Segoe UI", 8.5F, FontStyle.Bold)
            };

            chkBep = new CheckBox
            {
                Text = "BepInEx 6 IL2CPP (plugins/)",
                Checked = true,
                Location = new Point(200, 82),
                AutoSize = true,
                ForeColor = MainForm.cText,
                Font = new Font("Segoe UI", 9F, FontStyle.Bold)
            };
            chkBep.CheckedChanged += (s, e) => RefreshState();

            chkMelon = new CheckBox
            {
                Text = "MelonLoader 0.7.3+ (Mods/)",
                Checked = true,
                Location = new Point(200, 108),
                AutoSize = true,
                ForeColor = MainForm.cText,
                Font = new Font("Segoe UI", 9F, FontStyle.Bold)
            };
            chkMelon.CheckedChanged += (s, e) => RefreshState();

            lblLoaderInfo = new Label
            {
                Text = "",
                Location = new Point(16, 134),
                Size = new Size(548, 32),
                ForeColor = MainForm.cMuted,
                Font = new Font("Segoe UI", 8.25F)
            };

            pnlCard.Controls.Add(lblGameStatus);
            pnlCard.Controls.Add(lblModStatus);
            pnlCard.Controls.Add(lblLoaderTitle);
            pnlCard.Controls.Add(chkBep);
            pnlCard.Controls.Add(chkMelon);
            pnlCard.Controls.Add(lblLoaderInfo);

            // Install Button
            btnInstall = CreateStyledBtn("✔ JETZT INSTALLIEREN / AKTUALISIEREN", 4, 257, 580, 46, MainForm.cTerracotta, Color.White);
            btnInstall.Font = new Font("Segoe UI", 10.5F, FontStyle.Bold);
            btnInstall.Click += BtnInstall_Click;

            // Start Game Button
            btnLaunch = CreateStyledBtn("🎮 Spiel über Steam starten", 4, 312, 580, 36, MainForm.cCardLight, MainForm.cEmerald);
            btnLaunch.Click += (s, e) =>
            {
                try
                {
                    string exe = Path.Combine(txtPath.Text.Trim(), MainForm.GameExeName);
                    if (File.Exists(exe))
                        Process.Start(new ProcessStartInfo { FileName = exe, WorkingDirectory = txtPath.Text.Trim() });
                    else
                        MessageBox.Show("Spieldatei nicht gefunden!", "Fehler", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
                catch (Exception ex) { MessageBox.Show("Fehler beim Starten: " + ex.Message); }
            };

            // Progress & Log
            progressBar = new ProgressBar
            {
                Location = new Point(4, 362),
                Size = new Size(580, 8),
                Visible = false
            };

            lblLog = new Label
            {
                Text = "Tipp: Im Spiel öffnet die Taste [F8] das Co-op Lobby Menü.",
                Location = new Point(4, 377),
                Size = new Size(580, 45),
                ForeColor = MainForm.cMuted,
                Font = new Font("Segoe UI", 8.5F)
            };

            this.Controls.Add(lblPathHead);
            this.Controls.Add(txtPath);
            this.Controls.Add(btnBrowse);
            this.Controls.Add(pnlCard);
            this.Controls.Add(btnInstall);
            this.Controls.Add(btnLaunch);
            this.Controls.Add(progressBar);
            this.Controls.Add(lblLog);
        }

        public void RefreshState()
        {
            if (txtPath.Text != parent.CurrentGamePath)
                txtPath.Text = parent.CurrentGamePath;

            string gPath = txtPath.Text.Trim();
            bool hasGame = Directory.Exists(gPath) && File.Exists(Path.Combine(gPath, MainForm.GameExeName));

            if (hasGame)
            {
                lblGameStatus.Text = "● Spiel erkannt: Iron Nest: Heavy Turret Simulator";
                lblGameStatus.ForeColor = MainForm.cEmerald;
                btnLaunch.Enabled = true;
                btnInstall.Enabled = true;
            }
            else
            {
                lblGameStatus.Text = "● Spiel nicht gefunden (Bitte Pfad prüfen)";
                lblGameStatus.ForeColor = MainForm.cRed;
                btnLaunch.Enabled = false;
                btnInstall.Enabled = false;
                lblModStatus.Text = "● Mod-Status: Pfad ungültig";
                lblModStatus.ForeColor = MainForm.cMuted;
                return;
            }

            string bepMod = Path.Combine(gPath, "BepInEx", "plugins", "IronXNestCommand.dll");
            string melonMod = Path.Combine(gPath, "Mods", "IronXNestCommand.dll");

            bool hasBep = File.Exists(bepMod);
            bool hasMelon = File.Exists(melonMod);

            if (hasBep && hasMelon)
            {
                lblModStatus.Text = "● Installiert in BepInEx & MelonLoader (Dual-Loader)";
                lblModStatus.ForeColor = MainForm.cEmerald;
            }
            else if (hasBep)
            {
                var fi = new FileInfo(bepMod);
                lblModStatus.Text = string.Format("● Installiert in BepInEx/plugins ({0} KB)", fi.Length / 1024);
                lblModStatus.ForeColor = MainForm.cEmerald;
            }
            else if (hasMelon)
            {
                var fi = new FileInfo(melonMod);
                lblModStatus.Text = string.Format("● Installiert in Mods/ ({0} KB)", fi.Length / 1024);
                lblModStatus.ForeColor = MainForm.cEmerald;
            }
            else
            {
                lblModStatus.Text = "● Noch nicht installiert";
                lblModStatus.ForeColor = MainForm.cMuted;
            }

            bool bepLoaderPresent = IsBepInExInstalled(gPath);
            bool melonLoaderPresent = IsMelonLoaderInstalled(gPath);
            var infoParts = new System.Collections.Generic.List<string>();
            if (chkBep.Checked)
            {
                infoParts.Add(bepLoaderPresent
                    ? "BepInEx bereits vorhanden — wird nicht überschrieben."
                    : "BepInEx 6 IL2CPP wird mitinstalliert (~33 MB, im Paket enthalten).");
            }
            if (chkMelon.Checked)
            {
                infoParts.Add(melonLoaderPresent
                    ? "MelonLoader bereits vorhanden — wird nicht überschrieben."
                    : "MelonLoader wird mitinstalliert (~19 MB, im Paket enthalten).");
            }
            lblLoaderInfo.Text = string.Join("  ·  ", infoParts.ToArray());
        }

        // Best-Effort-Erkennung: reicht, um "Loader fehlt komplett" zuverlässig zu erkennen,
        // ohne jede BepInEx-Version pixelgenau zu validieren.
        private static bool IsBepInExInstalled(string gamePath)
        {
            if (File.Exists(Path.Combine(gamePath, "winhttp.dll"))) return true;
            if (File.Exists(Path.Combine(gamePath, "doorstop_config.ini"))) return true;
            string core = Path.Combine(gamePath, "BepInEx", "core");
            return Directory.Exists(core) && Directory.GetFiles(core, "*.dll").Length > 0;
        }

        private static bool IsMelonLoaderInstalled(string gamePath)
        {
            string mlDir = Path.Combine(gamePath, "MelonLoader");
            return Directory.Exists(mlDir) && Directory.GetFileSystemEntries(mlDir).Length > 0;
        }

        private void BtnInstall_Click(object sender, EventArgs e)
        {
            string gPath = txtPath.Text.Trim();
            try
            {
                btnInstall.Enabled = false;
                progressBar.Visible = true;
                progressBar.Value = 20;
                lblLog.Text = "Kopiere Dateien in Zielordner...";
                lblLog.ForeColor = MainForm.cText;

                var asm = Assembly.GetExecutingAssembly();
                int installedCount = 0;

                if (chkBep.Checked)
                {
                    if (!IsBepInExInstalled(gPath))
                    {
                        lblLog.Text = "Installiere BepInEx 6 IL2CPP Runtime...";
                        Application.DoEvents();
                        ExtractEmbeddedZip(asm, "BepInExRuntime.zip", gPath);
                    }
                    progressBar.Value = 55;

                    string target = Path.Combine(gPath, "BepInEx", "plugins");
                    Directory.CreateDirectory(target);

                    ExtractResource(asm, "IronXNestCommand.dll", Path.Combine(target, "IronXNestCommand.dll"));
                    ExtractResource(asm, "IronXNestCommand.Core.dll", Path.Combine(target, "IronXNestCommand.Core.dll"));
                    try { ExtractResource(asm, "IronNestCoop.Core.dll", Path.Combine(target, "IronNestCoop.Core.dll")); } catch { }
                    installedCount++;
                }

                if (chkMelon.Checked)
                {
                    if (!IsMelonLoaderInstalled(gPath))
                    {
                        lblLog.Text = "Installiere MelonLoader Runtime...";
                        Application.DoEvents();
                        ExtractEmbeddedZip(asm, "MelonLoaderRuntime.zip", gPath);
                    }
                    progressBar.Value = 85;

                    string target = Path.Combine(gPath, "Mods");
                    Directory.CreateDirectory(target);

                    ExtractResource(asm, "IronXNestCommand_Melon.dll", Path.Combine(target, "IronXNestCommand.dll"));
                    installedCount++;
                }

                progressBar.Value = 100;
                lblLog.Text = "✔ Installation erfolgreich! Starte das Spiel über Steam und drücke [F8].";
                lblLog.ForeColor = MainForm.cEmerald;

                MessageBox.Show("IronXNestCommand wurde erfolgreich installiert — inklusive benötigter Modloader-Runtime(s)!\n\nStarte das Spiel und drücke [F8] für das Co-op Menü.\n\nHinweis: Beim allerersten Start nach der Installation kann MelonLoader etwas länger brauchen (einmalige Interop-Generierung).", "Installation erfolgreich", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                lblLog.Text = "Fehler: " + ex.Message;
                lblLog.ForeColor = MainForm.cRed;
                MessageBox.Show("Fehler beim Installieren:\n" + ex.Message, "Fehler", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                progressBar.Visible = false;
                btnInstall.Enabled = true;
                RefreshState();
            }
        }

        private Button CreateStyledBtn(string text, int x, int y, int w, int h, Color bg, Color fg)
        {
            Button btn = new Button
            {
                Text = text,
                Location = new Point(x, y),
                Size = new Size(w, h),
                BackColor = bg,
                ForeColor = fg,
                FlatStyle = FlatStyle.Flat,
                Cursor = Cursors.Hand,
                Font = new Font("Segoe UI", 9F, FontStyle.Bold)
            };
            btn.FlatAppearance.BorderColor = MainForm.cBorder;
            btn.FlatAppearance.BorderSize = 1;
            btn.MouseEnter += (s, e) => { if (btn.Enabled) btn.BackColor = ControlPaint.Light(bg, 0.15f); };
            btn.MouseLeave += (s, e) => { if (btn.Enabled) btn.BackColor = bg; };
            return btn;
        }

        private void ExtractResource(Assembly assembly, string resourceName, string targetFilePath)
        {
            string match = null;
            foreach (var name in assembly.GetManifestResourceNames())
            {
                if (name.EndsWith(resourceName, StringComparison.OrdinalIgnoreCase))
                {
                    match = name;
                    break;
                }
            }

            if (match == null) throw new FileNotFoundException("Eingebettete Ressource nicht gefunden: " + resourceName);

            using (Stream s = assembly.GetManifestResourceStream(match))
            {
                if (s == null) throw new Exception("Ressource konnte nicht geöffnet werden.");
                using (FileStream fs = new FileStream(targetFilePath, FileMode.Create, FileAccess.Write, FileShare.None))
                {
                    s.CopyTo(fs);
                }
            }
        }

        // Entpackt eine als Ressource eingebettete Runtime-ZIP (BepInEx/MelonLoader) direkt
        // in den Spielordner. Wird nur aufgerufen, wenn der jeweilige Loader noch fehlt.
        private void ExtractEmbeddedZip(Assembly assembly, string resourceName, string destDir)
        {
            string match = null;
            foreach (var name in assembly.GetManifestResourceNames())
            {
                if (name.EndsWith(resourceName, StringComparison.OrdinalIgnoreCase))
                {
                    match = name;
                    break;
                }
            }

            if (match == null) throw new FileNotFoundException("Eingebettete Runtime-Ressource nicht gefunden: " + resourceName);

            string tempZip = Path.Combine(Path.GetTempPath(), "ixnc_" + Guid.NewGuid().ToString("N") + ".zip");
            try
            {
                using (Stream s = assembly.GetManifestResourceStream(match))
                {
                    if (s == null) throw new Exception("Runtime-Ressource konnte nicht geöffnet werden.");
                    using (FileStream fs = new FileStream(tempZip, FileMode.Create, FileAccess.Write, FileShare.None))
                    {
                        s.CopyTo(fs);
                    }
                }
                ZipFile.ExtractToDirectory(tempZip, destDir);
            }
            finally
            {
                try { File.Delete(tempZip); } catch { }
            }
        }
    }

    // =========================================================================
    // TAB 2: ERWEITERTE DEINSTALLATION & WARTUNG
    // =========================================================================
    public class UninstallTab : UserControl
    {
        private MainForm parent;
        private CheckBox chkBepPlugins;
        private CheckBox chkMelonMods;
        private CheckBox chkUserData;
        private CheckBox chkLogs;
        private Label lblDetectedSummary;
        private Button btnUninstallSelected;
        private Button btnWipeAll;
        private Label lblResult;

        public UninstallTab(MainForm form)
        {
            this.parent = form;
            this.BackColor = MainForm.cBg;
            InitializeComponent();
        }

        private void InitializeComponent()
        {
            Label lblHeader = new Label
            {
                Text = "ERWEITERTE DEINSTALLATION & DATEIBEREINIGUNG",
                Font = new Font("Segoe UI", 8F, FontStyle.Bold),
                ForeColor = MainForm.cMuted,
                Location = new Point(4, 8),
                AutoSize = true
            };

            Panel pnlCard = new Panel
            {
                Location = new Point(4, 30),
                Size = new Size(580, 200),
                BackColor = MainForm.cCard
            };
            pnlCard.Paint += (s, e) =>
            {
                using (var pen = new Pen(MainForm.cBorder))
                    e.Graphics.DrawRectangle(pen, 0, 0, pnlCard.Width - 1, pnlCard.Height - 1);
            };

            lblDetectedSummary = new Label
            {
                Text = "Gefundene Komponenten:",
                Location = new Point(16, 14),
                AutoSize = true,
                Font = new Font("Segoe UI", 9F, FontStyle.Bold),
                ForeColor = MainForm.cText
            };

            chkBepPlugins = new CheckBox
            {
                Text = "BepInEx Plugin-Dateien entfernen (IronXNestCommand.dll & Core.dll)",
                Checked = true,
                Location = new Point(16, 45),
                AutoSize = true,
                ForeColor = MainForm.cText
            };

            chkMelonMods = new CheckBox
            {
                Text = "MelonLoader Mod-Dateien entfernen (Mods/IronXNestCommand.dll)",
                Checked = true,
                Location = new Point(16, 75),
                AutoSize = true,
                ForeColor = MainForm.cText
            };

            chkUserData = new CheckBox
            {
                Text = "Einstellungs- & Speicherdaten löschen (UserData/IronXNestCommand/)",
                Checked = false,
                Location = new Point(16, 105),
                AutoSize = true,
                ForeColor = Color.Orange
            };

            chkLogs = new CheckBox
            {
                Text = "Temporäre Setup-Logs & Skripte bereinigen",
                Checked = false,
                Location = new Point(16, 135),
                AutoSize = true,
                ForeColor = MainForm.cMuted
            };

            pnlCard.Controls.Add(lblDetectedSummary);
            pnlCard.Controls.Add(chkBepPlugins);
            pnlCard.Controls.Add(chkMelonMods);
            pnlCard.Controls.Add(chkUserData);
            pnlCard.Controls.Add(chkLogs);

            btnUninstallSelected = CreateStyledBtn("🗑️ AUSGEWÄHLTE DATEIEN ENTFERNEN", 4, 245, 580, 42, MainForm.cCardLight, MainForm.cRed);
            btnUninstallSelected.Font = new Font("Segoe UI", 9.5F, FontStyle.Bold);
            btnUninstallSelected.Click += BtnUninstallSelected_Click;

            btnWipeAll = CreateStyledBtn("⚠️ Vollständiger Reset (Alles restlos löschen)", 4, 295, 580, 36, MainForm.cCard, Color.IndianRed);
            btnWipeAll.Click += BtnWipeAll_Click;

            lblResult = new Label
            {
                Text = "Wähle die Komponenten aus, die du entfernen möchtest.",
                Location = new Point(4, 345),
                Size = new Size(580, 40),
                ForeColor = MainForm.cMuted
            };

            this.Controls.Add(lblHeader);
            this.Controls.Add(pnlCard);
            this.Controls.Add(btnUninstallSelected);
            this.Controls.Add(btnWipeAll);
            this.Controls.Add(lblResult);
        }

        public void RefreshState()
        {
            string gPath = parent.CurrentGamePath;
            if (!Directory.Exists(gPath))
            {
                lblDetectedSummary.Text = "Kein gültiges Spielverzeichnis gefunden.";
                lblDetectedSummary.ForeColor = MainForm.cRed;
                return;
            }

            int count = 0;
            string bepMod = Path.Combine(gPath, "BepInEx", "plugins", "IronXNestCommand.dll");
            string melonMod = Path.Combine(gPath, "Mods", "IronXNestCommand.dll");
            string userData = Path.Combine(gPath, "UserData", "IronXNestCommand");

            if (File.Exists(bepMod)) count++;
            if (File.Exists(melonMod)) count++;
            if (Directory.Exists(userData)) count++;

            lblDetectedSummary.Text = string.Format("Status: {0} Komponenten von IronXNestCommand auf dem PC gefunden.", count);
            lblDetectedSummary.ForeColor = count > 0 ? MainForm.cEmerald : MainForm.cMuted;
        }

        private void BtnUninstallSelected_Click(object sender, EventArgs e)
        {
            string gPath = parent.CurrentGamePath;
            if (!Directory.Exists(gPath)) return;

            if (MessageBox.Show("Möchtest du die ausgewählten Komponenten wirklich entfernen?", "Bestätigung", MessageBoxButtons.YesNo, MessageBoxIcon.Question) != DialogResult.Yes)
                return;

            int deleted = 0;
            try
            {
                if (chkBepPlugins.Checked)
                {
                    string f1 = Path.Combine(gPath, "BepInEx", "plugins", "IronXNestCommand.dll");
                    string f2 = Path.Combine(gPath, "BepInEx", "plugins", "IronXNestCommand.Core.dll");
                    if (File.Exists(f1)) { File.Delete(f1); deleted++; }
                    if (File.Exists(f2)) { File.Delete(f2); deleted++; }
                }

                if (chkMelonMods.Checked)
                {
                    string f3 = Path.Combine(gPath, "Mods", "IronXNestCommand.dll");
                    if (File.Exists(f3)) { File.Delete(f3); deleted++; }
                }

                if (chkUserData.Checked)
                {
                    string uDir = Path.Combine(gPath, "UserData", "IronXNestCommand");
                    if (Directory.Exists(uDir)) { Directory.Delete(uDir, true); deleted++; }
                }

                lblResult.Text = string.Format("✔ Erfolgreich {0} Elemente entfernt.", deleted);
                lblResult.ForeColor = MainForm.cEmerald;
                MessageBox.Show("Deinstallation abgeschlossen!\n" + deleted + " Komponenten wurden bereinigt.", "Erfolg", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                lblResult.Text = "Fehler beim Löschen: " + ex.Message;
                lblResult.ForeColor = MainForm.cRed;
                MessageBox.Show("Fehler beim Löschen:\n" + ex.Message, "Fehler", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                RefreshState();
            }
        }

        private void BtnWipeAll_Click(object sender, EventArgs e)
        {
            if (MessageBox.Show("ACHTUNG: Dies löscht ALLE Mod-Dateien UND deine gespeicherten Einstellungen in UserData!\n\nFortfahren?", "Vollständiger Reset", MessageBoxButtons.YesNo, MessageBoxIcon.Warning) != DialogResult.Yes)
                return;

            chkBepPlugins.Checked = true;
            chkMelonMods.Checked = true;
            chkUserData.Checked = true;
            BtnUninstallSelected_Click(sender, e);
        }

        private Button CreateStyledBtn(string text, int x, int y, int w, int h, Color bg, Color fg)
        {
            Button btn = new Button
            {
                Text = text,
                Location = new Point(x, y),
                Size = new Size(w, h),
                BackColor = bg,
                ForeColor = fg,
                FlatStyle = FlatStyle.Flat,
                Cursor = Cursors.Hand,
                Font = new Font("Segoe UI", 9F, FontStyle.Bold)
            };
            btn.FlatAppearance.BorderColor = MainForm.cBorder;
            btn.FlatAppearance.BorderSize = 1;
            btn.MouseEnter += (s, e) => { if (btn.Enabled) btn.BackColor = ControlPaint.Light(bg, 0.15f); };
            btn.MouseLeave += (s, e) => { if (btn.Enabled) btn.BackColor = bg; };
            return btn;
        }
    }

    // =========================================================================
    // TAB 3: INFO & VERZEICHNISSE
    // =========================================================================
    public class InfoTab : UserControl
    {
        private MainForm parent;

        public InfoTab(MainForm form)
        {
            this.parent = form;
            this.BackColor = MainForm.cBg;
            InitializeComponent();
        }

        private void InitializeComponent()
        {
            Panel pnlCard = new Panel
            {
                Location = new Point(4, 10),
                Size = new Size(580, 230),
                BackColor = MainForm.cCard
            };
            pnlCard.Paint += (s, e) =>
            {
                using (var pen = new Pen(MainForm.cBorder))
                    e.Graphics.DrawRectangle(pen, 0, 0, pnlCard.Width - 1, pnlCard.Height - 1);
            };

            Label lblF8 = new Label
            {
                Text = "⌨️ Tastenbelegung & Co-op Nutzung",
                Font = new Font("Segoe UI", 10F, FontStyle.Bold),
                ForeColor = MainForm.cTerracotta,
                Location = new Point(16, 12),
                AutoSize = true
            };

            Label lblInfoText = new Label
            {
                Text = "• [F8]: Öffnet das In-Game Overlay (Lobby-Hexcode, Besatzungs-Roster, Resync)\n" +
                       "• Hex-Codes (z. B. '4A2F-9C1B') können direkt mit 1 Klick kopiert & geteilt werden.\n" +
                       "• Lochkarten-Sync & Gegner-Schutz laufen automatisch im Hintergrund aktiv mit.\n" +
                       "• Die Taste [F8] kann in den In-Game Einstellungen auf F7-F12 geändert werden.",
                Font = new Font("Segoe UI", 9F),
                ForeColor = MainForm.cText,
                Location = new Point(16, 40),
                Size = new Size(550, 80)
            };

            Label lblPathsTitle = new Label
            {
                Text = "📂 Schnellzugriff auf Verzeichnisse:",
                Font = new Font("Segoe UI", 9F, FontStyle.Bold),
                ForeColor = MainForm.cMuted,
                Location = new Point(16, 125),
                AutoSize = true
            };

            Button btnOpenGame = CreateStyledBtn("📁 Spielordner öffnen", 16, 155, 175, 32, MainForm.cCardLight, MainForm.cText);
            btnOpenGame.Click += (s, e) => OpenFolder(parent.CurrentGamePath);

            Button btnOpenUserData = CreateStyledBtn("📁 UserData / Config", 200, 155, 175, 32, MainForm.cCardLight, MainForm.cText);
            btnOpenUserData.Click += (s, e) => OpenFolder(Path.Combine(parent.CurrentGamePath, "UserData", "IronXNestCommand"));

            Button btnOpenPlugins = CreateStyledBtn("📁 BepInEx/plugins", 385, 155, 175, 32, MainForm.cCardLight, MainForm.cText);
            btnOpenPlugins.Click += (s, e) => OpenFolder(Path.Combine(parent.CurrentGamePath, "BepInEx", "plugins"));

            pnlCard.Controls.Add(lblF8);
            pnlCard.Controls.Add(lblInfoText);
            pnlCard.Controls.Add(lblPathsTitle);
            pnlCard.Controls.Add(btnOpenGame);
            pnlCard.Controls.Add(btnOpenUserData);
            pnlCard.Controls.Add(btnOpenPlugins);

            this.Controls.Add(pnlCard);
        }

        public void RefreshState() { }

        private void OpenFolder(string path)
        {
            try
            {
                if (!Directory.Exists(path)) Directory.CreateDirectory(path);
                Process.Start(new ProcessStartInfo { FileName = "explorer.exe", Arguments = "\"" + path + "\"" });
            }
            catch (Exception ex) { MessageBox.Show("Konnte Ordner nicht öffnen: " + ex.Message); }
        }

        private Button CreateStyledBtn(string text, int x, int y, int w, int h, Color bg, Color fg)
        {
            Button btn = new Button
            {
                Text = text,
                Location = new Point(x, y),
                Size = new Size(w, h),
                BackColor = bg,
                ForeColor = fg,
                FlatStyle = FlatStyle.Flat,
                Cursor = Cursors.Hand,
                Font = new Font("Segoe UI", 9F, FontStyle.Bold)
            };
            btn.FlatAppearance.BorderColor = MainForm.cBorder;
            btn.FlatAppearance.BorderSize = 1;
            btn.MouseEnter += (s, e) => { if (btn.Enabled) btn.BackColor = ControlPaint.Light(bg, 0.15f); };
            btn.MouseLeave += (s, e) => { if (btn.Enabled) btn.BackColor = bg; };
            return btn;
        }
    }
}
