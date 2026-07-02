using OsuMemoryDataProvider;

namespace CollectorFLN.Lib.Memory
{
    /**
     *   Uses StructuredOsuMemoryReader to read the beatmap data from osu! memory and extract the beatmap ID and file name.
     * 
     *   https://github.com/Piotrekol/ProcessMemoryDataFinder
    **/
    public class OsuMemoryReader
    {
        #pragma warning disable CS8618 
        private StructuredOsuMemoryReader reader;
        #pragma warning restore CS8618

        public OsuMemoryReader()
        {
            reader = StructuredOsuMemoryReader.Instance;
            Config config = new Config();
            string songsPath = config.SongPath;
        }

        // Retrieves map data from osu! memory and extracts metadata from the beatmap file
        public BeatmapMemorySnapshot? GetMapData(string songsPath)
        {
            var addresses = reader.OsuMemoryAddresses;

            reader.TryRead(addresses.GeneralData);
            
            if (reader.TryRead(addresses.Beatmap))
            {
                string folderName = addresses.Beatmap.FolderName;
                string fileName = addresses.Beatmap.OsuFileName;

                if (string.IsNullOrEmpty(folderName) || string.IsNullOrEmpty(fileName))
                {
                    return null;
                }

                BeatmapMemorySnapshot beatmapData = new BeatmapMemorySnapshot();
                beatmapData = ExtractMetaData(songsPath, folderName, fileName);

                return beatmapData;
            }
            
            return null;
        }

        // Metadata extraction from .osu file, returns artist, title, version, OD and HP as strings
        public BeatmapMemorySnapshot ExtractMetaData(string songsPath, string folderName, string fileName)
        {
            string artist = "";
            string title = "";
            string version = "";
            string od = "";
            string hp = "";
            string gamemode = "";

            BeatmapMemorySnapshot beatmapData = new BeatmapMemorySnapshot();

            string fullPath = Path.Combine(songsPath, folderName, fileName);

            foreach (string line in File.ReadLines(fullPath))
            {
                if (line.StartsWith("Mode:"))
                    gamemode = line.Split(':')[1].Trim();

                else if (line.StartsWith("Artist:"))
                    artist = line.Split(':')[1].Trim();

                else if (line.StartsWith("Title:"))
                    title = line.Split(':')[1].Trim();

                else if (line.StartsWith("Version:"))
                    version = line.Split(':')[1].Trim();

                else if (line.StartsWith("OverallDifficulty:"))
                    od = line.Split(':')[1].Trim();

                else if (line.StartsWith("HPDrainRate:"))
                    hp = line.Split(':')[1].Trim();

                // find BPM 

                if (!string.IsNullOrEmpty(gamemode) &&
                    !string.IsNullOrEmpty(artist) &&
                    !string.IsNullOrEmpty(title) &&
                    !string.IsNullOrEmpty(version) &&
                    !string.IsNullOrEmpty(od) &&
                    !string.IsNullOrEmpty(hp))
                {
                    break;
                }
            }

            beatmapData.SetBeatmapMemorySnapshot(folderName, fileName, gamemode, artist, title, version, od, hp, 200);

            return beatmapData;
        }
    }
}
