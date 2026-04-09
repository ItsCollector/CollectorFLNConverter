using CollectorFLN.Lib;
using System.Diagnostics;
using System.Globalization;

namespace CollectorFLN
{
    public partial class Form1 : Form
    {
        private Label lblSelectedMap = null!;
        private Label lblSelectedArtist = null!;
        private Label lblVersion = null!;
        private Button btnConvert = null!;
        private Button btnLinkOsu = null!;
        private TextBox txtLog = null!;

        private TextBox txtGap = null!;
        private TextBox txtOD = null!;
        private TextBox txtHP = null!;

        private CheckBox chkOverrideOD = null!;
        private CheckBox chkOverrideHP = null!;
        private CheckBox chkRemoveSV = null!;

        private BeatmapData currentBeatmapData = new BeatmapData();
        public string songsPath = "";

        private OsuMemoryReader osuMemoryReader;
        private System.Windows.Forms.Timer memoryTimer;
        private Config config;

        private Color _accent = Color.FromArgb(255, 102, 170);   // pink accent
        private Color _bg         = Color.FromArgb(18,  18,  24);    // near-black bg
        private Color _surface    = Color.FromArgb(28,  28,  38);    // card surface
        private Color _border     = Color.FromArgb(55,  55,  75);    // subtle borders
        private Color _textPrim   = Color.FromArgb(240, 240, 248);   // primary text
        private Color _textMuted  = Color.FromArgb(130, 130, 155);   // secondary text

        public Form1()
        {
            this.BackColor = _bg;
            this.ForeColor = _textPrim;
            this.Font = new Font("Segoe UI", 9f, FontStyle.Regular);
            this.ClientSize = new Size(440, 540);
            this.StartPosition = FormStartPosition.CenterScreen;
            this.FormBorderStyle = FormBorderStyle.FixedSingle;
            this.MaximizeBox = false;

            config = Config.Load();
            songsPath = config.SongPath;

            InitializeComponent();
            SetupControls();

            this.Text = "Collector's FLN Converter";
            this.Icon = new Icon("Assets/icon.ico");

            osuMemoryReader = new OsuMemoryReader();

            memoryTimer = new System.Windows.Forms.Timer();
            memoryTimer.Interval = 500;
            memoryTimer.Tick += MemoryTimer_Tick;

            if (!string.IsNullOrEmpty(config.SongPath) && !string.IsNullOrEmpty(config.ExePath))
            {
                memoryTimer.Start();
            }
        }

        private void SetupControls()
        {
            // ── Palette ─────────────────────────────────────────────
            Color accent = Color.FromArgb(255, 102, 170);
            Color bg = Color.FromArgb(18, 18, 24);
            Color surface = Color.FromArgb(28, 28, 38);
            Color border = Color.FromArgb(55, 55, 75);
            Color textPrim = Color.FromArgb(240, 240, 248);
            Color textMuted = Color.FromArgb(130, 130, 155);

            this.BackColor = bg;
            this.ForeColor = textPrim;
            this.ClientSize = new Size(440, 560);

            // ── Helper: styled label ─────────────────────────────────
            Label MakeLabel(string text, Point loc, float size = 9f,
                            Color? color = null, FontStyle style = FontStyle.Regular)
            {
                return new Label
                {
                    Text = text,
                    Location = loc,
                    AutoSize = true,
                    Font = new Font("Segoe UI", size, style),
                    ForeColor = color ?? textPrim,
                    BackColor = Color.Transparent
                };
            }

            // ── Helper: card panel ──────────────────────────────────
            Panel MakeCard(Point loc, Size size)
            {
                var p = new Panel
                {
                    Location = loc,
                    Size = size,
                    BackColor = surface
                };
                p.Paint += (s, e) =>
                {
                    // rounded-ish border via Graphics
                    using var pen = new Pen(border, 1);
                    e.Graphics.DrawRectangle(pen, 0, 0, p.Width - 1, p.Height - 1);
                };
                return p;
            }

            // ── Helper: styled TextBox ──────────────────────────────
            TextBox MakeTextBox(string defaultVal, Point loc, Size sz)
            {
                return new TextBox
                {
                    Text = defaultVal,
                    Location = loc,
                    Size = sz,
                    BackColor = Color.FromArgb(38, 38, 52),
                    ForeColor = textPrim,
                    BorderStyle = BorderStyle.FixedSingle,
                    Font = new Font("Segoe UI", 10f),
                    TextAlign = HorizontalAlignment.Center
                };
            }

            // ══════════════════════════════════════════════════════════
            // SECTION 1 — Map Info Card (top)
            // ══════════════════════════════════════════════════════════
            var cardInfo = MakeCard(new Point(20, 16), new Size(400, 88));

            // Thin accent bar on the left edge
            var accentBar = new Panel
            {
                Location = new Point(0, 0),
                Size = new Size(4, 88),
                BackColor = accent
            };

            lblSelectedMap = new Label
            {
                Text = "No map selected",
                Location = new Point(18, 12),
                Size = new Size(370, 28),
                Font = new Font("Segoe UI Semibold", 13f, FontStyle.Bold),
                ForeColor = textPrim,
                BackColor = Color.Transparent
            };

            lblSelectedArtist = new Label
            {
                Text = "",
                Location = new Point(18, 40),
                Size = new Size(370, 22),
                Font = new Font("Segoe UI", 10f),
                ForeColor = accent,
                BackColor = Color.Transparent
            };

            lblVersion = new Label
            {
                Text = "",
                Location = new Point(18, 62),
                Size = new Size(370, 18),
                Font = new Font("Segoe UI", 8f),
                ForeColor = textMuted,
                BackColor = Color.Transparent
            };

            cardInfo.Controls.AddRange(new Control[]
                { accentBar, lblSelectedMap, lblSelectedArtist, lblVersion });

            // ══════════════════════════════════════════════════════════
            // SECTION 2 — Parameters Card
            // ══════════════════════════════════════════════════════════
            var cardParams = MakeCard(new Point(20, 120), new Size(400, 170));

            var paramHeader = MakeLabel("PARAMETERS", new Point(16, 12),
                                        7.5f, textMuted, FontStyle.Bold);

            // ── Divider ─────────────────────────────────────────────
            var divider = new Panel
            {
                Location = new Point(14, 30),
                Size = new Size(372, 1),
                BackColor = border
            };

            // ── Gap row ─────────────────────────────────────────────
            var lblGap = MakeLabel("Gap (ms)", new Point(16, 46), 9f, textMuted);
            txtGap = MakeTextBox($"{config.Gap}", new Point(175, 42), new Size(70, 26));

            txtGap.TextChanged += (s, e) =>
            {
                if (Int32.TryParse(txtGap.Text, out int gap))
                {
                    config.Gap = gap;
                    config.Save();
                }
            };

            // ── SV checkbox ─────────────────────────────────────────
            chkRemoveSV = new CheckBox
            {
                Text = "Remove SV",
                Location = new Point(282, 44),
                AutoSize = true,
                Checked = config.RemoveSV,
                ForeColor = textMuted,
                BackColor = Color.Transparent,
                Font = new Font("Segoe UI", 8.5f),
                Cursor = Cursors.Hand
            };

            chkRemoveSV.CheckedChanged += (s, e) =>
            {
                chkRemoveSV.ForeColor = chkRemoveSV.Checked ? accent : textMuted;
                config.RemoveSV = chkRemoveSV.Checked;
                config.Save();
            };

            // ── OD row ──────────────────────────────────────────────
            var lblOD = MakeLabel("Overall Difficulty", new Point(16, 86), 9f, textMuted);
            txtOD = MakeTextBox($"{config.OD}", new Point(175, 84), new Size(70, 26));
            txtOD.Enabled = config.OverrideOD;
            
            txtOD.TextChanged += (s, e) =>
            {
                if (float.TryParse(txtOD.Text, out float od))
                {
                    config.OD = od;
                    config.Save();
                }
            };

            chkOverrideOD = new CheckBox
            {
                Text = "Override",
                Location = new Point(282, 82),
                AutoSize = true,
                Checked = config.OverrideOD,
                ForeColor = textMuted,
                BackColor = Color.Transparent,
                Font = new Font("Segoe UI", 8.5f),
                Cursor = Cursors.Hand
            };

            chkOverrideOD.CheckedChanged += (s, e) =>
            {
                txtOD.Enabled = chkOverrideOD.Checked;
                chkOverrideOD.ForeColor = chkOverrideOD.Checked ? accent : textMuted;
                config.OverrideOD = chkOverrideOD.Checked;
                config.Save();
            };

            // ── HP row ──────────────────────────────────────────────
            var lblHP = MakeLabel("HP Drain", new Point(16, 126), 9f, textMuted);
            txtHP = MakeTextBox($"{config.HP}", new Point(175, 124), new Size(70, 26));
            txtHP.Enabled = config.OverrideHP;

            txtHP.TextChanged += (s, e) =>
            {
                if (float.TryParse(txtHP.Text, out float hp))
                {
                    config.HP = hp;
                    config.Save();
                }
            };

            chkOverrideHP = new CheckBox
            {
                Text = "Override",
                Location = new Point(282, 122),
                AutoSize = true,
                Checked = config.OverrideHP,
                ForeColor = textMuted,
                BackColor = Color.Transparent,
                Font = new Font("Segoe UI", 8.5f),
                Cursor = Cursors.Hand
            };

            chkOverrideHP.CheckedChanged += (s, e) =>
            {
                txtHP.Enabled = chkOverrideHP.Checked;
                chkOverrideHP.ForeColor = chkOverrideHP.Checked ? accent : textMuted;
                config.OverrideHP = chkOverrideHP.Checked;
                config.Save();
            };

            // ── Tooltips ────────────────────────────────────────────
            var tt = new ToolTip { InitialDelay = 300, ReshowDelay = 100 };
            tt.SetToolTip(chkOverrideOD, "Override the map's default Overall Difficulty value.");
            tt.SetToolTip(chkOverrideHP, "Override the map's default HP Drain value.");

            cardParams.Controls.AddRange(new Control[]
            {
                paramHeader, divider,
                lblGap, txtGap, chkRemoveSV,
                lblOD, txtOD, chkOverrideOD,
                lblHP, txtHP, chkOverrideHP
            });

            // ══════════════════════════════════════════════════════════
            // SECTION 3 — Convert Button
            // ══════════════════════════════════════════════════════════
            btnConvert = new Button
            {
                Text = "CONVERT",
                Size = new Size(400, 44),
                Location = new Point(20, 304),
                FlatStyle = FlatStyle.Flat,
                BackColor = accent,
                ForeColor = Color.FromArgb(18, 18, 24),  // dark text on pink
                Font = new Font("Segoe UI Semibold", 10.5f, FontStyle.Bold),
                Cursor = Cursors.Hand
            };

            btnConvert.FlatAppearance.BorderSize = 0;
            btnConvert.FlatAppearance.MouseOverBackColor = Color.FromArgb(255, 130, 190);
            btnConvert.FlatAppearance.MouseDownBackColor = Color.FromArgb(220, 80, 150);
            btnConvert.Click += BtnConvert_Click;

            btnLinkOsu = new Button
            {
                Text = "LINK OSU FOLDER",
                Size = new Size(400, 44),
                Location = new Point(20, 304),
                FlatStyle = FlatStyle.Flat,
                BackColor = accent,
                ForeColor = Color.FromArgb(18, 18, 24),  // dark text on pink
                Font = new Font("Segoe UI Semibold", 10.5f, FontStyle.Bold),
                Cursor = Cursors.Hand
            };

            btnLinkOsu.FlatAppearance.BorderSize = 0;
            btnLinkOsu.FlatAppearance.MouseOverBackColor = Color.FromArgb(255, 130, 190);
            btnLinkOsu.FlatAppearance.MouseDownBackColor = Color.FromArgb(220, 80, 150);
            btnLinkOsu.Click += BtnLinkOsu_Click;

            // ══════════════════════════════════════════════════════════
            // SECTION 4 — Log Card
            // ══════════════════════════════════════════════════════════
            var cardLog = MakeCard(new Point(20, 364), new Size(400, 172));

            var logHeader = MakeLabel("LOG", new Point(16, 10), 7.5f, textMuted, FontStyle.Bold);

            var logDivider = new Panel
            {
                Location = new Point(14, 28),
                Size = new Size(372, 1),
                BackColor = border
            };

            txtLog = new TextBox
            {
                Multiline = true,
                Size = new Size(370, 124),
                Location = new Point(14, 36),
                ScrollBars = ScrollBars.Vertical,
                ReadOnly = true,
                BackColor = Color.FromArgb(22, 22, 32),
                ForeColor = textMuted,
                Font = new Font("Consolas", 8.5f),
                BorderStyle = BorderStyle.None
            };

            cardLog.Controls.AddRange(new Control[] { logHeader, logDivider, txtLog });

            // ══════════════════════════════════════════════════════════
            // ADD ALL TO FORM
            // ══════════════════════════════════════════════════════════
            Controls.AddRange(new Control[] { cardInfo, cardParams, btnConvert, btnLinkOsu, cardLog});

            // Update Check Button forecolour based on config
            chkRemoveSV.ForeColor = chkRemoveSV.Checked ? accent : textMuted;
            chkOverrideOD.ForeColor = chkOverrideOD.Checked ? accent : textMuted;
            chkOverrideHP.ForeColor = chkOverrideHP.Checked ? accent : textMuted;

            // Check if the Osu! folder is linked, if not, it will show a button and ask to link the folder before continuing. 
            if (string.IsNullOrEmpty(config.SongPath) || string.IsNullOrEmpty(config.ExePath))
            {
                txtLog.AppendText("No osu! folder linked. Please link your osu! folder to enable conversion.\r\n");
                btnLinkOsu.Visible = true;
                btnConvert.Visible = false;
            }
            else
            {
                btnLinkOsu.Visible = false;
                btnConvert.Visible = true;
            }
        }

        private void BtnConvert_Click(object? sender, EventArgs e)
        {
            int gap = int.TryParse(txtGap.Text, out int g) ? g : 80;
            bool removeSV = chkRemoveSV.Checked;
            float od = int.TryParse(txtOD.Text, out int o) ? o : 0;
            float hp = int.TryParse(txtHP.Text, out int h) ? h : 6;
            btnConvert.Enabled = false;

            // Call Conversion Stack
            ConversionStack(gap, removeSV, od, hp);
            btnConvert.Enabled = true;
        }

        private void BtnLinkOsu_Click(object? sender, EventArgs e)
        {
            using (var dialog = new FolderBrowserDialog())
            {
                dialog.Description = "Select your osu! folder";
                dialog.UseDescriptionForTitle = true;

                if (dialog.ShowDialog() == DialogResult.OK)
                {
                    string selectedPath = dialog.SelectedPath;

                    // Save to config
                    string songsPath = Path.Combine(selectedPath, "Songs");
                    string exePath = Path.Combine(selectedPath, "osu!.exe");

                    // Validate folder
                    if (!Directory.Exists(songsPath) || !File.Exists(exePath))
                    {
                        MessageBox.Show(
                            "Invalid osu! folder.\nMake sure it contains 'Songs' and 'osu!.exe'.",
                            "Error",
                            MessageBoxButtons.OK,
                            MessageBoxIcon.Error
                        );
                        return;
                    }

                    config.SongPath = songsPath;
                    config.ExePath = exePath;
                    config.Save();

                    txtLog.AppendText($"Songs folder set to:\r\n{selectedPath}\r\n");

                    btnLinkOsu.Visible = false;
                    btnConvert.Visible = true;

                    this.songsPath = config.SongPath;
                    memoryTimer.Start();
                }
            }
        }

        // Timer tick event to continuously read osu! memory and update map info on the UI
        private void MemoryTimer_Tick(object? sender, EventArgs e)
        {
            BeatmapData incomingBeatmapData = osuMemoryReader.GetMapData(songsPath);
            
            btnConvert.Enabled = !string.IsNullOrEmpty(currentBeatmapData.fileName);

            if (incomingBeatmapData.fileName != currentBeatmapData.fileName && incomingBeatmapData.version != currentBeatmapData.version)
            {
                currentBeatmapData = incomingBeatmapData;

                lblSelectedMap.Text = $"{currentBeatmapData.title}";
                lblSelectedArtist.Text = $"{currentBeatmapData.artist}";
                lblVersion.Text = $"{currentBeatmapData.version}";

                if (!chkOverrideOD.Checked)
                {
                    txtOD.Text = $"{currentBeatmapData.od}";
                }

                if (!chkOverrideHP.Checked)
                {
                    txtHP.Text = $"{currentBeatmapData.hp}";
                }
            }
        }

        // FLN Conversion Stack: Extract → Create FLN → Write new .osu → Open in osu!
        public void ConversionStack(int gap, bool removeSV, float od, float hp)
        {
            txtLog.AppendText($"Starting conversion for {currentBeatmapData.fileName}\r\n");

            if (string.IsNullOrEmpty(currentBeatmapData.folderName) || string.IsNullOrEmpty(currentBeatmapData.fileName))
            {
                txtLog.AppendText("Error: No map detected.\r\n");
                return;
            }

            // Prevent duplicate conversion
            if (currentBeatmapData.version.Contains("FLN", StringComparison.OrdinalIgnoreCase))
            {
                txtLog.AppendText("This map is already an FLN map.\r\n");
                return;
            }

            // File exists check
            string fullPath = Path.Combine(songsPath, currentBeatmapData.folderName, currentBeatmapData.fileName);

            if (!File.Exists(fullPath))
            {
                txtLog.AppendText("Error: Map file not found.\r\n");
                return;
            }

            Converter converter = new Converter();
            List<TimingPoint> newTimingPoints = new List<TimingPoint>();

            if (Converter.GetMapGamemode(songsPath, currentBeatmapData.folderName, currentBeatmapData.fileName) != 3)
            {
                txtLog.AppendText("Error: Only osu!mania maps are supported.\r\n");
                return;
            }

            // Begin Conversion process
            try
            {
                // Extract data from target beatmap
                var (timingPoints, hitObjects, keyCount) = converter.ExtractData(
                    songsPath,
                    currentBeatmapData.folderName,
                    currentBeatmapData.fileName
                );

                // Normalize timing points if SV removal is enabled, otherwise keep original timing points
                if (removeSV)
                {
                    double targetBpm = Converter.GetDominantBpm(timingPoints);
                    newTimingPoints = Converter.NormalizeTimingPoints(timingPoints, targetBpm);
                }
                else
                {
                    newTimingPoints = timingPoints;
                }

                // Create FLN hit objects based on extracted data and user-defined gap
                var flnObjects = Converter.CreateFLN(hitObjects, gap);

                // Write new .osu file with FLN objects and updated timing points
                string newOsuFile = converter.WriteNewOsuFile(
                    songsPath,
                    currentBeatmapData.folderName,
                    currentBeatmapData.fileName,
                    newTimingPoints,
                    flnObjects,
                    keyCount,
                    gap,
                    removeSV,
                    hp,
                    od
                );

                txtLog.AppendText("Conversion complete!\r\n");

                // Open the new FLN map in osu!
                Process.Start(new ProcessStartInfo
                {
                    FileName = newOsuFile,
                    UseShellExecute = true
                });

                txtLog.AppendText("FLN map opened in osu!\r\n");
            }
            catch (Exception ex)
            {
                txtLog.AppendText($"Error: {ex.Message}\r\n");
            }
        }
    }
}