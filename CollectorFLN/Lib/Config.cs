using System.Text.Json;

namespace CollectorFLN
{
    public class Config
    {
        // Configuration settings
        public string SongPath { get; set; } = "";
        public float OD { get; set; } = 0;
        public float HP { get; set; } = 6;
        public int Gap { get; set; } = 80;
        public bool OverrideOD { get; set; } = true;
        public bool OverrideHP { get; set; } = true;
        public bool EnableFLN { get; set; } = true;
        public bool EnableRateChange { get; set; } = true;
        public bool EnableRemoveSV { get; set; } = true;
        public bool EnableRemoveLN { get; set; } = true;
        public bool UseSnapMode { get; set; } = false;
        public int SnapDivisor { get; set; } = 4;

        public bool ChangePitchUprate { get; set; } = false;
        public bool ChangePitchDownrate { get; set; } = false;

        public bool ShowLog { get; set; } = false;

        private static readonly string configFile = "config.json";

        public static Config Load()
        {
            try
            {
                if (!File.Exists(configFile))
                {
                    var defaultConfig = new Config();

                    // Try auto-detect ONLY when file doesn't exist
                    defaultConfig.FetchDefaultDirectory();

                    File.WriteAllText(configFile, JsonSerializer.Serialize(defaultConfig, new JsonSerializerOptions { WriteIndented = true }));
                    return defaultConfig;
                }

                string json = File.ReadAllText(configFile);
                var config = JsonSerializer.Deserialize<Config>(json) ?? new Config();

                // If paths are still empty, try detect once
                if (string.IsNullOrEmpty(config.SongPath))
                {
                    config.FetchDefaultDirectory();
                }

                return config;
            }
            catch
            {
                return new Config();
            }
        }

        public void Save()
        {
            try
            {
                string json = JsonSerializer.Serialize(this, new JsonSerializerOptions { WriteIndented = true });
                File.WriteAllText(configFile, json);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to save config: {ex.Message}");
            }
        }

        public void ResetToDefaults()
        {
            OD = 0;
            HP = 6;
            Gap = 80;
            OverrideOD = true;
            OverrideHP = true;
            EnableFLN = false;
            EnableRateChange = false;
            EnableRemoveSV = false;
            EnableRemoveLN = false;
            UseSnapMode = false;
            SnapDivisor = 4;
            ChangePitchUprate = false;
            ChangePitchDownrate = false;
            ShowLog = false;
        }

        // Fetches the osu! Songs directory path and executable path from the user's local application data
        void FetchDefaultDirectory()
        {
            var possiblePaths = new[]
            {
                Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "osu!", "Songs"),
                Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "osu!", "Songs"),
                Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86), "osu!", "Songs"),
                Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "osu!", "Songs")
            };

            foreach (var path in possiblePaths)
            {
                if (Directory.Exists(path))
                {
                    SongPath = path;
                    Save();
                    return;
                }
            }

            Console.WriteLine("Could not auto-detect osu! Songs folder.");
        }
    }
}
