using CollectorFLN.Lib;
using CollectorFLN.Lib.Converters;
using CollectorFLN.Lib.Memory;
using CollectorFLN.UI.Elements;
using System.Diagnostics;
using static CollectorFLN.UI.Theme;

namespace CollectorFLN
{
    public partial class Form1 : Form
    {
        // Menu bar 
        private AppLogCard logCard = null!;

        // Map Info
        private AppMapInfoCard mapInfoCard = null!;
        private AppModuleToggleCard moduleCard;
        private AppFlnParametersCard flnCard;
        private AppRateChangeCard rateChangeCard;
        private AppDifficultyOverrideCard difficultyOverrideCard = null!;
        private Button btnConvert = null!;
        private Button btnLinkOsu = null!;

        // Runtime 
        private BeatmapMemorySnapshot currentSnapshot = new BeatmapMemorySnapshot();
        private OsuMemoryReader osuMemoryReader;
        private bool osuOpen = false;
        private System.Windows.Forms.Timer memoryTimer;

        // Config
        private Config config;
        private double currentRate;

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

            SetupControls();

            SetClientSize(config.ShowLog);

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
            logCard = new AppLogCard();
            Controls.Add(logCard.logCard);

            // Toolbar 
            AppToolbar toolbar = new AppToolbar(config);
            toolbar.LogVisibilityChanged += (s, visible) =>
            {
                logCard.logCard.Visible = visible;
                SetClientSize(visible);
            };

            toolbar.LogMessage += (s, msg) =>
            {
                Log(msg);
            };

            //toolbar.ResetDefaultsRequested += (s, e) => ResetConfigToDefaults();

            Controls.Add(toolbar.menuStrip);
            Controls.SetChildIndex(toolbar.menuStrip, 0);
            this.MainMenuStrip = toolbar.menuStrip;

            // SECTION 1 — Map Info Card
            mapInfoCard = new AppMapInfoCard();
            Controls.Add(mapInfoCard.mapInfoCard);

            // SECTION 2 — Module Toggles Card
            moduleCard = new AppModuleToggleCard(new ModuleToggleConfig(config.EnableFLN, config.EnableRemoveLN, config.EnableRemoveSV, config.EnableRateChange));
            Controls.Add(moduleCard.moduleCard);

            moduleCard.ChkEnableRemoveLNChanged += (s, isEnabled) =>
            {
                config.EnableRemoveLN = isEnabled;
                config.Save();
            };

            moduleCard.ChkEnableRemoveSVChanged += (s, isEnabled) =>
            {
                config.EnableRemoveSV = isEnabled;
                config.Save();
            };

            // SECTION 3 — FLN Parameters Card 
            flnCard = new AppFlnParametersCard(config);
            Controls.Add(flnCard.flnParametersCard);
            flnCard.ToggleEnabled(config.EnableFLN);

            flnCard.GapModeChanged += (s, isSnap) =>
            {
                config.UseSnapMode = isSnap;
                config.Save();
            };

            flnCard.GapMsChanged += (s, gapValue) =>
            {
                if (Int32.TryParse(gapValue, out int gap))
                {
                    config.Gap = gap;
                    config.Save();
                }
            };

            flnCard.GapSnapChanged += (s, snapValue) =>
            {
                string[] parts = snapValue.Split('/');

                if (parts.Length == 2 && int.TryParse(parts[1], out int divisor))
                {
                    config.SnapDivisor = divisor;
                    config.Save();
                }
            };

            moduleCard.ChkEnableFLNChanged += (s, isEnabled) =>
            {
                flnCard.ToggleEnabled(isEnabled);

                config.EnableFLN = isEnabled;
                config.Save();
            };

            // SECTION 4 — Rate Change Card
            rateChangeCard = new AppRateChangeCard(new RateChangeConfig(200, 1.00, config.IncreasePitch, config.DecreasePitch));
            Controls.Add(rateChangeCard.rateChangeCard);
            rateChangeCard.ToggleEnabled(config.EnableRateChange);

            rateChangeCard.RateChanged += (s, rate) =>
            {
                currentRate = rate; 
                logCard.AppendLine($"Rate set to {rate:0.00}x");
            };

            moduleCard.ChkEnableRateChangeChanged += (s, isEnabled) =>
            {
                rateChangeCard.ToggleEnabled(isEnabled);
                config.EnableRateChange = isEnabled;
                config.Save();
            };

            // SECTION 5 — OD / HP Override 
            difficultyOverrideCard = new AppDifficultyOverrideCard(new DifficultyOverrideConfig(config.OverrideOD, config.OD, config.OverrideHP, config.HP));
            Controls.Add(difficultyOverrideCard.difficultyOverrideCard);

            difficultyOverrideCard.OverrideODChanged += (s, isEnabled) =>
            {
                if (!isEnabled)
                {
                    difficultyOverrideCard.SetOD(float.Parse(currentSnapshot.od));
                }

                config.OverrideOD = isEnabled;
                config.Save();
            };

            difficultyOverrideCard.OverrideHPChanged += (s, isEnabled) =>
            {
                if (!isEnabled)
                {
                    difficultyOverrideCard.SetHP(float.Parse(currentSnapshot.hp));
                }

                config.OverrideHP = isEnabled;
                config.Save();
            };

            difficultyOverrideCard.ODTxtChanged += (s, odValue) =>
            {
                config.OD = odValue;
                config.Save();
            };

            difficultyOverrideCard.HPTxtChanged += (s, hpValue) =>
            {
                config.HP = hpValue;
                config.Save();
            };

            // SECTION 6 — Convert / Link Buttons
            btnConvert = new Button { Text = "CONVERT", Size = new Size(440, 44), Location = new Point(20, 690), FlatStyle = FlatStyle.Flat, BackColor = accent, ForeColor = Color.FromArgb(18, 18, 24), Font = new Font("Segoe UI Semibold", 10.5f, FontStyle.Bold), Cursor = Cursors.Hand };
            btnConvert.FlatAppearance.BorderSize = 0;
            btnConvert.FlatAppearance.MouseOverBackColor = Color.FromArgb(255, 130, 190);
            btnConvert.FlatAppearance.MouseDownBackColor = Color.FromArgb(220, 80, 150);
            Controls.Add(btnConvert);

            btnLinkOsu = new Button { Text = "LINK OSU SONGS FOLDER", Size = new Size(440, 44), Location = new Point(20, 690), FlatStyle = FlatStyle.Flat, BackColor = accent, ForeColor = Color.FromArgb(18, 18, 24), Font = new Font("Segoe UI Semibold", 10.5f, FontStyle.Bold), Cursor = Cursors.Hand };
            btnLinkOsu.FlatAppearance.BorderSize = 0;
            btnLinkOsu.FlatAppearance.MouseOverBackColor = Color.FromArgb(255, 130, 190);
            btnLinkOsu.FlatAppearance.MouseDownBackColor = Color.FromArgb(220, 80, 150);
            Controls.Add(btnLinkOsu);

            btnConvert.Click += BtnConvert_Click;
            btnLinkOsu.Click += BtnLinkOsu_Click;

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

        private void BtnConvert_Click(object? sender, EventArgs e)
        {
            int gap = config.Gap;
            bool removeSV = config.EnableRemoveSV;
            float od = config.OD;
            float hp = config.HP;
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
                    //songsPath = dialog.SelectedPath;

                    config.SongPath = dialog.SelectedPath;
                    config.Save();

                    Log($"Songs folder set to:\r\n{config.SongPath}");

                    btnLinkOsu.Visible = false;
                    btnConvert.Visible = true;

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
                incomingSnapshot = osuMemoryReader.GetMapData(config.SongPath);
            }
            catch (Exception ex)
            {
                Log($"Error: {ex.Message}");
                return; // silently ignore bad reads
            }

            if (incomingSnapshot == null)
            {
                osuOpen = false;
                ToggleEnabled(false);
                return;
            }
            else
            {
                osuOpen = true;
                ToggleEnabled(true);
            }

            btnConvert.Enabled = !string.IsNullOrEmpty(incomingSnapshot.fileName) && incomingSnapshot.gamemode == "3";

            if (incomingSnapshot.fileName != currentSnapshot.fileName && incomingSnapshot.version != currentSnapshot.version)
            {
                currentSnapshot = incomingSnapshot;

                mapInfoCard.SetMapInfo
                (
                    currentSnapshot.title,
                    currentSnapshot.artist,
                    currentSnapshot.version,
                    currentSnapshot.bpm
                );

                if (!config.OverrideOD)
                {
                    difficultyOverrideCard.SetOD(float.Parse(currentSnapshot.od));
                }

                if (!config.OverrideHP)
                {
                    difficultyOverrideCard.SetHP(float.Parse(currentSnapshot.hp));
                }

                // TODO: populate lblOriginalBPM.Text from snapshot once BPM data is available
            }
        }

        public void SetClientSize(bool logOpen)
        {
            if (logOpen)
            {
                this.ClientSize = new Size(940, 754);
            }
            else
            {
                this.ClientSize = new Size(480, 754);
            }
        }

        // FLN Conversion Stack: Extract → Create FLN → Write new .osu → Open in osu!
        public void ConversionStack(int gap, bool removeSV, float od, float hp, bool useSnapMode = false, int snapDivisor = 4)
        {
            string modeLabel = useSnapMode ? $"1/{snapDivisor} snap" : $"{gap}ms";
            Log($"Starting conversion ({modeLabel}) for {currentSnapshot.fileName}.");
            Log($"Options - Remove SV: {removeSV}, Override OD: {config.OverrideOD}, Override HP: {config.OverrideHP}");
            
            if (!MapValidation())
            {
                return;
            }

            List<TimingPoint> newTimingPoints = new List<TimingPoint>();

            // Begin Conversion process
            try
            {
                // Extract data from target beatmap
                var (timingPoints, hitObjects, keyCount) = BeatmapParser.ExtractData( // added try catch behaviour to internal BeatmapParser.ExtractData to handle exceptions 
                    config.SongPath,
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
                    flnObjects = FLNConverter.CreateSnappedBasedFLN(hitObjects, timingPoints, snapDivisor);
                }
                else
                {
                    flnObjects = FLNConverter.CreateMsBasedFLN(hitObjects, gap);
                }

                // Write new .osu file with FLN objects and updated timing points
                string newOsuFile = BeatmapWriter.WriteNewOsuFile(
                    config.SongPath,
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
                Log($"{ex.Message}");
            }
        }

        public bool MapValidation()
        {
            Log($"Running map validation.");

            // No map detected 
            if (string.IsNullOrEmpty(config.SongPath) || string.IsNullOrEmpty(currentSnapshot.folderName) || string.IsNullOrEmpty(currentSnapshot.fileName))
            {
                Log("Conversion Failed: No map detected.");
                return false;
            }

            // Prevent duplicate conversion
            if (currentSnapshot.version.Contains("FLN", StringComparison.OrdinalIgnoreCase))
            {
                Log("Conversion Failed: This map is already an FLN map.");
                Log("If this is a mistake, remove \"FLN\" from the map's difficuty name and try again");
                return false;
            }

            // File exists check 
            if (!File.Exists(Path.Combine(config.SongPath, currentSnapshot.folderName, currentSnapshot.fileName)))
            {
                Log("Error: Map file not found.");
                Log($"Songs Path: {config.SongPath}\n, Folder name {currentSnapshot.folderName}\n file name {currentSnapshot.fileName}");
                return false;
            }

            Log("Map validation passed.");
            return true;
        }

        // Toggle for all fields depending on whether Osu! is open or not
        public void ToggleEnabled(bool osuOpen)
        {
            mapInfoCard.ToggleEnabled(osuOpen);
            moduleCard.ToggleEnabled(osuOpen);
            difficultyOverrideCard.ToggleEnabled(osuOpen);

            if (osuOpen)
            {
                flnCard.ToggleEnabled(config.EnableFLN);
                rateChangeCard.ToggleEnabled(config.EnableRateChange);
            }
            else
            {
                flnCard.ToggleEnabled(false);
                rateChangeCard.ToggleEnabled(false);
            }
        }

        public void Log(string message)
        {
            logCard.AppendLine(message + "\r\n");
        }
    }
}