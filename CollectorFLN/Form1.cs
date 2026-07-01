using CollectorFLN.Lib;
using CollectorFLN.UI;
using System.Diagnostics;
using static CollectorFLN.UI.InterfaceBuilder;
using static CollectorFLN.UI.Theme;

namespace CollectorFLN
{
    public partial class Form1 : Form
    {
        // Menu bar 
        private MenuStrip menuStrip = null!;

        // Map Info
        private Label lblSelectedMap = null!;
        private Label lblSelectedArtist = null!;
        private Label lblVersion = null!;
        private Button btnConvert = null!;
        private Button btnLinkOsu = null!;

        // Module toggles
        private CheckBox chkEnableFLN = null!;
        private CheckBox chkEnableRemoveSV = null!;
        private CheckBox chkEnableRateChange = null!;
        private CheckBox chkEnableRemoveLN = null!;

        // FLN parameters panel
        private TextBox txtGap = null!;
        private RadioButton rbMsMode = null!;
        private RadioButton rbSnapMode = null!;
        private ComboBox cmbSnapDivisor = null!;
        private Label lblGapLabel = null!;

        // Rate change panel 
        private Label rateChangeHeader = null!;
        private TextBox txtBPM = null!;
        private TextBox txtRate = null!;

        // Difficulty override panel toggles 
        private TextBox txtOD = null!;
        private TextBox txtHP = null!;
        private CheckBox chkOverrideOD = null!;
        private CheckBox chkOverrideHP = null!;

        // Log card 
        private Panel cardLog = null!;
        private TextBox txtLog = null!;

        // Runtime 
        private BeatmapMemorySnapshot currentSnapshot = new BeatmapMemorySnapshot();
        private OsuMemoryReader osuMemoryReader;
        private System.Windows.Forms.Timer memoryTimer;

        // Config
        public string songsPath = "";
        private Config config;

        public Form1()
        {
            InitializeComponent();

            this.Text = "Collector's FLN Converter";
            this.Icon = new Icon("Assets/icon.ico");
            this.Font = new Font("Segoe UI", 9f, FontStyle.Regular);
            this.ClientSize = new Size(480, 780);
            this.StartPosition = FormStartPosition.CenterScreen;
            this.BackColor = bg;
            this.FormBorderStyle = FormBorderStyle.FixedSingle;
            this.MaximizeBox = false;

            config = Config.Load();
            songsPath = config.SongPath;

            SetupControls();
            WireEvents();
            FillConfigSettings();

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
            // Log Card
            cardLog = MakeCard(new Point(480, 36), new Size(440, 662));
            var logHeader = MakeLabel("LOG", new Point(16, 10), 7.5f, textMuted, FontStyle.Bold);
            var logDivider = new Panel { Location = new Point(14, 28), Size = new Size(412, 1), BackColor = border };

            txtLog = new TextBox
            {
                Multiline = true,
                Location = new Point(14, 36),
                Size = new Size(412, 620),
                ScrollBars = ScrollBars.Vertical,
                ReadOnly = true,
                BackColor = Color.FromArgb(22, 22, 32),
                ForeColor = textMuted,
                Font = new Font("Consolas", 8.5f),
                BorderStyle = BorderStyle.None
            };

            cardLog.Controls.AddRange(new Control[] { logHeader, logDivider, txtLog });

            // Toolbar 
            Toolbar toolbar = new Toolbar
            (
                message => Log(message),
                visible =>
                {
                    Console.WriteLine($"[DEBUG] logs visible {visible}");
                    cardLog.Visible = visible;
                    config.ShowLog = visible;
                    config.Save();

                    this.Size = (visible) ? new Size(960, 780) : new Size(500, 780); 
                },
                isOn =>
                {
                    Console.WriteLine($"[DEBUG] change pitch uprate {isOn}");
                    config.ChangePitchUprate = isOn;
                    config.Save();
                },
                isOn =>
                {
                    Console.WriteLine($"[DEBUG] change pitch downrate {isOn}");
                    config.ChangePitchDownrate = isOn;
                    config.Save();
                },
                config.ShowLog,
                config.ChangePitchUprate,    
                config.ChangePitchDownrate  
            );

            menuStrip = toolbar.GetMenuStrip();  
            this.MainMenuStrip = menuStrip;

            // SECTION 1 — Map Info Card
            var mapInfoCard = MakeCard(new Point(20, 36), new Size(440, 88));
            var accentBar = new Panel { Location = new Point(0, 0), Size = new Size(4, 88), BackColor = accent };

            lblSelectedMap = new Label { Text = "No map selected", Location = new Point(18, 12), Size = new Size(410, 28), Font = new Font("Segoe UI Semibold", 13f, FontStyle.Bold), ForeColor = textPrim, BackColor = Color.Transparent };
            lblSelectedArtist = new Label { Text = "", Location = new Point(18, 40), Size = new Size(410, 22), Font = new Font("Segoe UI", 10f), ForeColor = accent, BackColor = Color.Transparent };
            lblVersion = new Label { Text = "", Location = new Point(18, 62), Size = new Size(410, 18), Font = new Font("Segoe UI", 8f), ForeColor = textMuted, BackColor = Color.Transparent };
            
            mapInfoCard.Controls.AddRange(new Control[] { accentBar, lblSelectedMap, lblSelectedArtist, lblVersion });

            // SECTION 2 — Module Toggles Card
            var moduleCard = MakeCard(new Point(20, 134), new Size(440, 120), surface);
            var moduleHeader = MakeLabel("ACTIVE MODULES", new Point(16, 10), 7.5f, textMuted, FontStyle.Bold);
            var moduleDivider = new Panel { Location = new Point(14, 30), Size = new Size(412, 1), BackColor = border };

            chkEnableFLN = MakeModuleToggle("FLN Conversion", new Point(16, 44));
            chkEnableRemoveLN = MakeModuleToggle("Remove LNs", new Point(195, 44));
            chkEnableRemoveSV = MakeModuleToggle("Remove SV", new Point(195, 80));
            chkEnableRateChange = MakeModuleToggle("Rate Change", new Point(16, 80));
            
            moduleCard.Controls.AddRange(new Control[] { moduleHeader, moduleDivider, chkEnableFLN, chkEnableRemoveLN, chkEnableRemoveSV, chkEnableRateChange});
            // SECTION 3 — FLN Parameters Card 
            var flnCard = MakeCard(new Point(20, 264), new Size(440, 120));
            var flnHeader = MakeLabel("FLN PARAMETERS", new Point(16, 12), 7.5f, textMuted, FontStyle.Bold);
            var flnDivider = new Panel { Location = new Point(14, 30), Size = new Size(412, 1), BackColor = border };
            var lblGapMode = MakeLabel("Gap Mode", new Point(16, 44), 9f, textMuted);

            rbMsMode = new RadioButton { Text = "Ms", Location = new Point(195, 42), AutoSize = true, BackColor = Color.Transparent, Font = new Font("Segoe UI", 9f, FontStyle.Bold), Cursor = Cursors.Hand };
            rbSnapMode = new RadioButton { Text = "Snap", Location = new Point(260, 42), AutoSize = true, BackColor = Color.Transparent, Font = new Font("Segoe UI", 9f, FontStyle.Bold), Cursor = Cursors.Hand };

            lblGapLabel = MakeLabel(new Point(16, 80), 9f, textMuted);
            txtGap = MakeTextBox(new Point(195, 76), new Size(70, 26));

            cmbSnapDivisor = new ComboBox
            {
                Location = new Point(195, 76),
                Size = new Size(70, 26),
                DropDownStyle = ComboBoxStyle.DropDownList,
                BackColor = Color.FromArgb(38, 38, 52),
                ForeColor = textPrim,
                Font = new Font("Segoe UI", 10f),
                FlatStyle = FlatStyle.Flat,
                Cursor = Cursors.Hand,
                Visible = config.UseSnapMode
            };

            var snapOptions = new[] { "1/2", "1/3", "1/4", "1/6", "1/8", "1/12", "1/16" };
            cmbSnapDivisor.Items.AddRange(snapOptions);

            var tt = new ToolTip { InitialDelay = 300, ReshowDelay = 100 };
            tt.SetToolTip(rbMsMode, "Use a fixed millisecond value for LN gaps.");
            tt.SetToolTip(rbSnapMode, "Use beat snap divisor for LN gaps (adapts to BPM).");
            tt.SetToolTip(chkEnableFLN, "Convert the chart's rice notes into long notes.");
            tt.SetToolTip(chkEnableRemoveSV, "Strip SVs and normalise BPM changes to a constant scroll speed.");
            tt.SetToolTip(chkEnableRateChange, "Change the chart's BPM/rate, with optional pitch preservation.");
            tt.SetToolTip(chkEnableRemoveLN, "Remove all long notes from the chart. If combined with FLN, it will remove the original LNs then convert to FLN.");

            flnCard.Controls.AddRange(new Control[] { flnHeader, flnDivider, lblGapMode, rbMsMode, rbSnapMode, lblGapLabel, txtGap, cmbSnapDivisor });

            // SECTION 4 — Rate Change Card
            var rateChangeCard = MakeCard(new Point(20, 394), new Size(440, 120));
            rateChangeHeader = MakeLabel("RATE CHANGE", new Point(16, 12), 7.5f, textMuted, FontStyle.Bold); // field, not shadowed local
            var rateDivider = new Panel { Location = new Point(14, 30), Size = new Size(412, 1), BackColor = border };

            var lblBPM = MakeLabel("Target BPM", new Point(16, 46), 9f, textMuted);
            txtBPM = MakeTextBox("", new Point(195, 42), new Size(70, 26));

            var lblRate = MakeLabel("Rate (x)", new Point(16, 74), 9f, textMuted);
            txtRate = MakeTextBox("1.00", new Point(195, 70), new Size(70, 26));

            tt.SetToolTip(txtBPM, "Set a target BPM — rate is calculated automatically.");
            tt.SetToolTip(txtRate, "Set a playback rate — BPM is calculated automatically.");

            rateChangeCard.Controls.AddRange(new Control[] { rateChangeHeader, rateDivider, lblBPM, txtBPM, lblRate, txtRate});

            // SECTION 5 — OD / HP Override 
            var difficultyCard = MakeCard(new Point(20, 524), new Size(440, 120));
            var difficultyHeader = MakeLabel("DIFFICULTY OVERRIDES", new Point(16, 12), 7.5f, textMuted, FontStyle.Bold);
            var difficultyDivider = new Panel { Location = new Point(14, 30), Size = new Size(412, 1), BackColor = border };

            var lblOD = MakeLabel("Overall Difficulty", new Point(16, 46), 9f, textMuted);
            txtOD = MakeTextBox(new Point(195, 42), new Size(70, 26));
            chkOverrideOD = new CheckBox { Text = "Override", Location = new Point(302, 44), AutoSize = true, ForeColor = textMuted, BackColor = Color.Transparent, Font = new Font("Segoe UI", 8.5f), Cursor = Cursors.Hand };

            var lblHP = MakeLabel("HP Drain", new Point(16, 74), 9f, textMuted);
            txtHP = MakeTextBox(new Point(195, 70), new Size(70, 26));
            chkOverrideHP = new CheckBox { Text = "Override", Location = new Point(302, 72), AutoSize = true, ForeColor = textMuted, BackColor = Color.Transparent, Font = new Font("Segoe UI", 8.5f), Cursor = Cursors.Hand };

            tt.SetToolTip(chkOverrideOD, "Override the map's default Overall Difficulty value.");
            tt.SetToolTip(chkOverrideHP, "Override the map's default HP Drain value.");

            difficultyCard.Controls.AddRange(new Control[] { difficultyHeader, difficultyDivider, lblOD, txtOD, chkOverrideOD, lblHP, txtHP, chkOverrideHP });

            // SECTION 6 — Convert / Link Buttons
            btnConvert = new Button { Text = "CONVERT", Size = new Size(440, 44), Location = new Point(20, 654), FlatStyle = FlatStyle.Flat, BackColor = accent, ForeColor = Color.FromArgb(18, 18, 24), Font = new Font("Segoe UI Semibold", 10.5f, FontStyle.Bold), Cursor = Cursors.Hand };
            btnConvert.FlatAppearance.BorderSize = 0;
            btnConvert.FlatAppearance.MouseOverBackColor = Color.FromArgb(255, 130, 190);
            btnConvert.FlatAppearance.MouseDownBackColor = Color.FromArgb(220, 80, 150);

            btnLinkOsu = new Button { Text = "LINK OSU SONGS FOLDER", Size = new Size(440, 44), Location = new Point(20, 662), FlatStyle = FlatStyle.Flat, BackColor = accent, ForeColor = Color.FromArgb(18, 18, 24), Font = new Font("Segoe UI Semibold", 10.5f, FontStyle.Bold), Cursor = Cursors.Hand };
            btnLinkOsu.FlatAppearance.BorderSize = 0;
            btnLinkOsu.FlatAppearance.MouseOverBackColor = Color.FromArgb(255, 130, 190);
            btnLinkOsu.FlatAppearance.MouseDownBackColor = Color.FromArgb(220, 80, 150);

            // ADD ALL TO FORM 
            Controls.AddRange(new Control[]
            {
                mapInfoCard, moduleCard, flnCard, rateChangeCard, difficultyCard,
                btnConvert, btnLinkOsu, cardLog, menuStrip
            });
            Controls.SetChildIndex(menuStrip, 0);
        }

        private void WireEvents()
        {
            chkEnableFLN.CheckedChanged += (s, e) =>
            {
                rbMsMode.Enabled = chkEnableFLN.Checked;
                rbSnapMode.Enabled = chkEnableFLN.Checked;
                txtGap.Enabled = chkEnableFLN.Checked;
                cmbSnapDivisor.Enabled = chkEnableFLN.Checked;
                chkEnableFLN.ForeColor = (chkEnableFLN.Checked) ? accent : textMuted;

                config.EnableFLN = chkEnableFLN.Checked;
                config.Save();
            };

            chkEnableRemoveSV.CheckedChanged += (s, e) =>
            {
                chkEnableRemoveSV.ForeColor = (chkEnableRemoveSV.Checked) ? accent : textMuted;

                config.EnableRemoveSV = chkEnableRemoveSV.Checked;
                config.Save();
            };

            chkEnableRateChange.CheckedChanged += (s, e) =>
            {
                chkEnableRateChange.ForeColor = (chkEnableRateChange.Checked) ? accent : textMuted;
                txtBPM.Enabled = chkEnableRateChange.Checked;
                txtRate.Enabled = chkEnableRateChange.Checked;

                config.EnableRateChange = chkEnableRateChange.Checked;
                config.Save();
            };

            chkEnableRemoveLN.CheckedChanged += (s, e) =>
            {
                chkEnableRemoveLN.ForeColor = (chkEnableRemoveLN.Checked) ? accent : textMuted;
                config.EnableRemoveLN = chkEnableRemoveLN.Checked;
                config.Save();
            };

            txtGap.TextChanged += (s, e) =>
            {
                if (Int32.TryParse(txtGap.Text, out int gap))
                {
                    config.Gap = gap;
                    config.Save();
                }
            };

            cmbSnapDivisor.SelectedIndexChanged += (s, e) =>
            {
                if (cmbSnapDivisor.SelectedItem is string selected)
                {
                    string[] parts = selected.Split('/');
                    if (parts.Length == 2 && int.TryParse(parts[1], out int divisor))
                    {
                        config.SnapDivisor = divisor;
                        config.Save();
                    }
                }
            };

            rbMsMode.CheckedChanged += (s, e) =>
            {
                if (rbMsMode.Checked)
                {
                    UpdateGapModeUI();
                }
            };

            rbSnapMode.CheckedChanged += (s, e) => 
            {
                if (rbSnapMode.Checked)
                {
                    UpdateGapModeUI();
                }
            };

            txtOD.TextChanged += (s, e) =>
            {
                if (float.TryParse(txtOD.Text, out float od))
                {
                    config.OD = od;
                    config.Save();
                }
            };

            txtHP.TextChanged += (s, e) =>
            {
                if (float.TryParse(txtHP.Text, out float hp))
                {
                    config.HP = hp;
                    config.Save();
                }
            };

            chkOverrideOD.CheckedChanged += (s, e) =>
            {
                txtOD.Enabled = chkOverrideOD.Checked;
                chkOverrideOD.ForeColor = chkOverrideOD.Checked ? accent : textMuted;
                config.OverrideOD = chkOverrideOD.Checked;
                config.Save();
            };

            chkOverrideHP.CheckedChanged += (s, e) =>
            {
                txtHP.Enabled = chkOverrideHP.Checked;
                chkOverrideHP.ForeColor = chkOverrideHP.Checked ? accent : textMuted;
                config.OverrideHP = chkOverrideHP.Checked;
                config.Save();
            };

            btnConvert.Click += BtnConvert_Click;
            btnLinkOsu.Click += BtnLinkOsu_Click;
        }

        private void FillConfigSettings()
        {
            txtOD.Text = config.OD.ToString();
            txtHP.Text = config.HP.ToString();

            lblGapLabel.Text = config.UseSnapMode ? "Gap (snap):" : "Gap (ms):";
            txtGap.Text = config.Gap.ToString();
            txtGap.Visible = !config.UseSnapMode;

            chkOverrideOD.Checked = config.OverrideOD;
            chkOverrideHP.Checked = config.OverrideHP;

            var snapOptions = new[] { "1/2", "1/3", "1/4", "1/6", "1/8", "1/12", "1/16" };
            string savedSnap = $"1/{config.SnapDivisor}";
            int savedIndex = Array.IndexOf(snapOptions, savedSnap);
            cmbSnapDivisor.SelectedIndex = savedIndex >= 0 ? savedIndex : 2;

            chkEnableFLN.Checked = config.EnableFLN;
            chkEnableFLN.ForeColor = chkEnableFLN.Checked ? accent : textMuted;

            chkEnableRateChange.Checked = config.EnableRateChange;
            chkEnableRateChange.ForeColor = chkEnableRateChange.Checked ? accent : textMuted;

            chkEnableRemoveSV.Checked = config.EnableRemoveSV;
            chkEnableRemoveSV.ForeColor = chkEnableRemoveSV.Checked ? accent : textMuted;

            chkEnableRemoveLN.Checked = config.EnableRemoveLN;
            chkEnableRemoveLN.ForeColor = chkEnableRemoveLN.Checked ? accent : textMuted;

            rbSnapMode.Checked = config.UseSnapMode;
            rbSnapMode.ForeColor = rbSnapMode.Checked ? accent : textMuted;

            rbMsMode.Checked = !config.UseSnapMode;
            rbMsMode.ForeColor = rbMsMode.Checked ? accent : textMuted;

            if (config.ShowLog)
            {
                this.Size = new Size(960, 780);
                cardLog.Visible = true;
            }
            else
            {
                this.Size = new Size(500, 780);
                cardLog.Visible = false;
            }

            if (string.IsNullOrEmpty(config.SongPath))
            {
                Log("No osu! songs folder linked. Please link your osu! songs folder to enable conversion.");
                btnLinkOsu.Visible = true;
                btnConvert.Visible = false;
            }
            else
            {
                btnLinkOsu.Visible = false;
                btnConvert.Visible = true;
            }
        }

        private void UpdateGapModeUI()
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

        private void BtnOpenConfig_Click(object? sender, EventArgs e)
        {
            try
            {
                string configPath = Path.Combine(AppContext.BaseDirectory, "config.json");
                Process.Start(new ProcessStartInfo
                {
                    FileName = configPath,
                    UseShellExecute = true
                });
            }
            catch (Exception ex)
            {
                Log($"Could not open config.json: {ex.Message}");
            }
        }

        private void ResetDefaultsMenuItem_Click(object? sender, EventArgs e)
        {
            // TODO: implement actual reset-to-default logic against Config
            Log("Reset to defaults requested (not yet implemented).");
        }

        public void Log(string message)
        {
            txtLog.AppendText(message + "\r\n");
        }

        private void BtnConvert_Click(object? sender, EventArgs e)
        {
            int gap = int.TryParse(txtGap.Text, out int g) ? g : 80;
            bool removeSV = chkEnableRemoveSV.Checked;
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

                    Log($"Songs folder set to:\r\n{songsPath}");

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

                // TODO: populate lblOriginalBPM.Text from snapshot once BPM data is available
            }
        }

        // FLN Conversion Stack: Extract → Create FLN → Write new .osu → Open in osu!
        public void ConversionStack(int gap, bool removeSV, float od, float hp, bool useSnapMode = false, int snapDivisor = 4)
        {
            string modeLabel = useSnapMode ? $"1/{snapDivisor} snap" : $"{gap}ms";
            Log($"Starting conversion ({modeLabel}) for {currentSnapshot.fileName}.");
            Log($"Options - Remove SV: {removeSV}, Override OD: {chkOverrideOD.Checked}, Override HP: {chkOverrideHP.Checked}");
            
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

                bool multiBpmFlag = TimingPointProcessor.MultiBpmCheck(timingPoints);

                if (removeSV && multiBpmFlag == true)  // Normalise timing points if multiple BPMs found
                {
                    double targetBpm = TimingPointProcessor.FindTargetBpm(timingPoints);

                    if (TimingPointProcessor.CheckForNormalisation(timingPoints, targetBpm) == true) // Cancel Normalisation if already normalised
                    {
                        newTimingPoints = timingPoints;
                        removeSV = false;
                    }
                    else
                    {
                        newTimingPoints = TimingPointProcessor.NormaliseTimingPoints(timingPoints, targetBpm);
                    }
                }
                else if (removeSV && multiBpmFlag == false) // Cancel Normalisation if only 1 BPM found
                {
                    double firstRedOffset = timingPoints.First(tp => !tp.isInherited).offset;
                    var firstRed = timingPoints.First(tp => !tp.isInherited);

                    newTimingPoints = new List<TimingPoint> // strips away any extra green lines and keeps 1 at 1.0x
                    {
                        firstRed,
                        new TimingPoint(
                            firstRedOffset,
                            -100,
                            firstRed.meter,
                            firstRed.sampleSet,
                            firstRed.sampleIndex,
                            firstRed.volume,
                            true,  // green line
                            firstRed.effects
                        )
                    };
                }
                else // NSV is disabled
                {
                    Console.WriteLine("This map has NSV disabled");
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

                Log("Conversion complete!");

                // Open the new FLN map in osu!
                Process.Start(new ProcessStartInfo
                {
                    FileName = newOsuFile,
                    UseShellExecute = true
                });

                Log("FLN map opened in osu!");
            }
            catch (Exception ex)
            {
                Log($"Error: {ex.Message}");
            }
        }

        public bool MapValidation()
        {
            Log($"Running map validation.");

            // No map detected 
            if (string.IsNullOrEmpty(songsPath) || string.IsNullOrEmpty(currentSnapshot.folderName) || string.IsNullOrEmpty(currentSnapshot.fileName))
            {
                Log("Error: No map detected.");
                return false;
            }

            // Prevent duplicate conversion
            if (currentSnapshot.version.Contains("FLN", StringComparison.OrdinalIgnoreCase))
            {
                Log("Error: This map is already an FLN map.");
                return false;
            }

            // File exists check 
            if (!File.Exists(Path.Combine(songsPath, currentSnapshot.folderName, currentSnapshot.fileName)))
            {
                Log("Error: Map file not found.");
                Log($"Songs Path: {songsPath}\n, Folder name {currentSnapshot.folderName}\n file name {currentSnapshot.fileName}");
                return false;
            }

            Log("Map validation passed.");
            return true;
        }
    }
}