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

        private RadioButton rbMsMode = null!;
        private RadioButton rbSnapMode = null!;
        private ComboBox cmbSnapDivisor = null!;
        private Label lblGapLabel = null!;

        private BeatmapMemorySnapshot currentSnapshot = new BeatmapMemorySnapshot();
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

            if (!string.IsNullOrEmpty(config.SongPath))
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
            this.ClientSize = new Size(440, 596);

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
            var cardParams = MakeCard(new Point(20, 120), new Size(400, 206));

            var paramHeader = MakeLabel("PARAMETERS", new Point(16, 12),
                                        7.5f, textMuted, FontStyle.Bold);

            // ── Divider ─────────────────────────────────────────────
            var divider = new Panel
            {
                Location = new Point(14, 30),
                Size = new Size(372, 1),
                BackColor = border
            };

            // ── Gap Mode toggle row ─────────────────────────────────
            var lblGapMode = MakeLabel("Gap Mode", new Point(16, 44), 9f, textMuted);

            rbMsMode = new RadioButton
            {
                Text = "ms",
                Location = new Point(175, 42),
                AutoSize = true,
                Checked = !config.UseSnapMode,
                ForeColor = !config.UseSnapMode ? accent : textMuted,
                BackColor = Color.Transparent,
                Font = new Font("Segoe UI", 9f, FontStyle.Bold),
                Cursor = Cursors.Hand
            };

            rbSnapMode = new RadioButton
            {
                Text = "snap",
                Location = new Point(240, 42),
                AutoSize = true,
                Checked = config.UseSnapMode,
                ForeColor = config.UseSnapMode ? accent : textMuted,
                BackColor = Color.Transparent,
                Font = new Font("Segoe UI", 9f, FontStyle.Bold),
                Cursor = Cursors.Hand
            };

            // ── Gap input row ───────────────────────────────────────
            lblGapLabel = MakeLabel(
                config.UseSnapMode ? "Gap (snap)" : "Gap (ms)",
                new Point(16, 80), 9f, textMuted);

            txtGap = MakeTextBox($"{config.Gap}", new Point(175, 76), new Size(70, 26));
            txtGap.Visible = !config.UseSnapMode;

            txtGap.TextChanged += (s, e) =>
            {
                if (Int32.TryParse(txtGap.Text, out int gap))
                {
                    config.Gap = gap;
                    config.Save();
                }
            };

            // ── Snap divisor ComboBox ────────────────────────────────
            cmbSnapDivisor = new ComboBox
            {
                Location = new Point(175, 76),
                Size = new Size(70, 26),
                DropDownStyle = ComboBoxStyle.DropDownList,
                BackColor = Color.FromArgb(38, 38, 52),
                ForeColor = textPrim,
                Font = new Font("Segoe UI", 10f),
                FlatStyle = FlatStyle.Flat,
                Cursor = Cursors.Hand,
                Visible = config.UseSnapMode
            };

            // Populate snap divisor options
            var snapOptions = new[] { "1/2", "1/3", "1/4", "1/6", "1/8", "1/12", "1/16" };
            cmbSnapDivisor.Items.AddRange(snapOptions);

            // Select the matching item based on saved config
            string savedSnap = $"1/{config.SnapDivisor}";
            int savedIndex = Array.IndexOf(snapOptions, savedSnap);
            cmbSnapDivisor.SelectedIndex = savedIndex >= 0 ? savedIndex : 2; // default 1/4

            cmbSnapDivisor.SelectedIndexChanged += (s, e) =>
            {
                if (cmbSnapDivisor.SelectedItem is string selected)
                {
                    // Parse "1/4" → 4
                    string[] parts = selected.Split('/');
                    if (parts.Length == 2 && int.TryParse(parts[1], out int divisor))
                    {
                        config.SnapDivisor = divisor;
                        config.Save();
                    }
                }
            };

            // ── Gap mode toggle handler ─────────────────────────────
            void UpdateGapModeUI()
            {
                bool isSnap = rbSnapMode.Checked;
                config.UseSnapMode = isSnap;
                config.Save();

                txtGap.Visible = !isSnap;
                cmbSnapDivisor.Visible = isSnap;
                lblGapLabel.Text = isSnap ? "Gap (snap)" : "Gap (ms)";

                rbMsMode.ForeColor = !isSnap ? accent : textMuted;
                rbSnapMode.ForeColor = isSnap ? accent : textMuted;
            }

            rbMsMode.CheckedChanged += (s, e) => { if (rbMsMode.Checked) UpdateGapModeUI(); };
            rbSnapMode.CheckedChanged += (s, e) => { if (rbSnapMode.Checked) UpdateGapModeUI(); };

            // ── SV checkbox ─────────────────────────────────────────
            chkRemoveSV = new CheckBox
            {
                Text = "Remove SV",
                Location = new Point(282, 78),
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
            var lblOD = MakeLabel("Overall Difficulty", new Point(16, 120), 9f, textMuted);
            txtOD = MakeTextBox($"{config.OD}", new Point(175, 118), new Size(70, 26));
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
                Location = new Point(282, 116),
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
            var lblHP = MakeLabel("HP Drain", new Point(16, 160), 9f, textMuted);
            txtHP = MakeTextBox($"{config.HP}", new Point(175, 158), new Size(70, 26));
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
                Location = new Point(282, 156),
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
            tt.SetToolTip(rbMsMode, "Use a fixed millisecond value for LN gaps.");
            tt.SetToolTip(rbSnapMode, "Use beat snap divisor for LN gaps (adapts to BPM).");

            cardParams.Controls.AddRange(new Control[]
            {
                paramHeader, divider,
                lblGapMode, rbMsMode, rbSnapMode,
                lblGapLabel, txtGap, cmbSnapDivisor, chkRemoveSV,
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
                Location = new Point(20, 340),
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
                Text = "LINK OSU SONGS FOLDER",
                Size = new Size(400, 44),
                Location = new Point(20, 340),
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
            var cardLog = MakeCard(new Point(20, 400), new Size(400, 172));

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
            Controls.AddRange(new Control[] { cardInfo, cardParams, btnConvert, btnLinkOsu, cardLog });

            // Update Check Button forecolour based on config
            chkRemoveSV.ForeColor = chkRemoveSV.Checked ? accent : textMuted;
            chkOverrideOD.ForeColor = chkOverrideOD.Checked ? accent : textMuted;
            chkOverrideHP.ForeColor = chkOverrideHP.Checked ? accent : textMuted;

            // Check if the Osu! folder is linked, if not, it will show a button and ask to link the folder before continuing. 
            if (string.IsNullOrEmpty(config.SongPath))
            {
                txtLog.AppendText("No osu! songs folder linked. Please link your osu! songs folder to enable conversion.\r\n");
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
            bool useSnap = config.UseSnapMode;
            int snapDivisor = config.SnapDivisor;
            btnConvert.Enabled = false;

            // Call Conversion Stack
            ConversionStack(gap, removeSV, od, hp, useSnap, snapDivisor);
            btnConvert.Enabled = true;
        }

        private void BtnLinkOsu_Click(object? sender, EventArgs e)
        {
            using (var dialog = new FolderBrowserDialog())
            {
                dialog.Description = "Select your osu! songs folder";
                dialog.UseDescriptionForTitle = true;

                if (dialog.ShowDialog() == DialogResult.OK)
                {
                    string songsPath = dialog.SelectedPath;

                    config.SongPath = songsPath;
                    config.Save();

                    txtLog.AppendText($"Songs folder set to:\r\n{songsPath}\r\n");

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
            BeatmapMemorySnapshot? incomingSnapshot;

            try
            {
                incomingSnapshot = osuMemoryReader.GetMapData(songsPath);
            }
            catch
            {
                return; // silently ignore bad reads
            }

            if (incomingSnapshot == null)
            {
                return;
            }

            btnConvert.Enabled = !string.IsNullOrEmpty(incomingSnapshot.fileName) && incomingSnapshot.gamemode == "3";

            if (incomingSnapshot.fileName != currentSnapshot.fileName && incomingSnapshot.version != currentSnapshot.version)
            {
                currentSnapshot = incomingSnapshot;

                lblSelectedMap.Text = $"{currentSnapshot.title}";
                lblSelectedArtist.Text = $"{currentSnapshot.artist}";
                lblVersion.Text = $"{currentSnapshot.version}";

                if (!chkOverrideOD.Checked)
                {
                    txtOD.Text = $"{currentSnapshot.od}";
                }

                if (!chkOverrideHP.Checked)
                {
                    txtHP.Text = $"{currentSnapshot.hp}";
                }
            }
        }

        // FLN Conversion Stack: Extract → Create FLN → Write new .osu → Open in osu!
        public void ConversionStack(int gap, bool removeSV, float od, float hp, bool useSnapMode = false, int snapDivisor = 4)
        {
            string modeLabel = useSnapMode ? $"1/{snapDivisor} snap" : $"{gap}ms";
            txtLog.AppendText($"Starting conversion ({modeLabel}) for {currentSnapshot.fileName}.\r\n");
            txtLog.AppendText($"Options - Remove SV: {removeSV}, Override OD: {chkOverrideOD.Checked}, Override HP: {chkOverrideHP.Checked}\r\n");
            if (!MapValidation())
            {
                return;
            }

            List<TimingPoint> newTimingPoints = new List<TimingPoint>();

            // Begin Conversion process
            try
            {
                // Extract data from target beatmap
                var (timingPoints, hitObjects, keyCount) = BeatmapParser.ExtractData(
                    songsPath,
                    currentSnapshot.folderName,
                    currentSnapshot.fileName
                );

                // Normalize timing points if SV removal is enabled, otherwise keep original timing points
                if (removeSV)
                {
                    //var redLines = timingPoints.Where(tp => !tp.isInherited).ToList();
                    //double targetBpm = Converter.GetDominantBpm(redLines);
                    newTimingPoints = Converter.NormaliseTimingPoints(timingPoints);
                }
                else
                {
                    newTimingPoints = timingPoints;
                }

                List<HitObject> flnObjects = new List<HitObject>();

                if (useSnapMode)
                {
                    flnObjects = Converter.CreateSnappedBasedFLN(hitObjects, timingPoints, snapDivisor);
                }
                else
                {
                    flnObjects = Converter.CreateMsBasedFLN(hitObjects, gap);
                }


                // Write new .osu file with FLN objects and updated timing points
                string newOsuFile = BeatmapWriter.WriteNewOsuFile(
                    songsPath,
                    currentSnapshot.folderName,
                    currentSnapshot.fileName,
                    newTimingPoints,
                    flnObjects,
                    keyCount,
                    gap,
                    removeSV,
                    hp,
                    od,
                    useSnapMode,
                    snapDivisor
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

        public bool MapValidation()
        {
            txtLog.AppendText($"Running map validation.\r\n");

            // No map detected 
            if (string.IsNullOrEmpty(songsPath) || string.IsNullOrEmpty(currentSnapshot.folderName) || string.IsNullOrEmpty(currentSnapshot.fileName))
            {
                txtLog.AppendText("Error: No map detected.\r\n");
                return false;
            }

            // Prevent duplicate conversion
            if (currentSnapshot.version.Contains("FLN", StringComparison.OrdinalIgnoreCase))
            {
                txtLog.AppendText("Error: This map is already an FLN map.\r\n");
                return false;
            }

            // File exists check 
            if (!File.Exists(Path.Combine(songsPath, currentSnapshot.folderName, currentSnapshot.fileName)))
            {
                txtLog.AppendText("Error: Map file not found.\r\n");
                txtLog.AppendText($"Songs Path: {songsPath}\n, Folder name {currentSnapshot.folderName}\n file name {currentSnapshot.fileName}\r\n");
                return false;
            }

            txtLog.AppendText("Map validation passed.\r\n");
            return true;
        }
    }
}