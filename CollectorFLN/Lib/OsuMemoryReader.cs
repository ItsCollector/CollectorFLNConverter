using CollectorFLN.Lib;
using OsuMemoryDataProvider;

namespace CollectorFLN
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
        public BeatmapData GetMapData(string songsPath)
        {
            var addresses = reader.OsuMemoryAddresses;

            reader.TryRead(addresses.GeneralData);
            
            if (reader.TryRead(addresses.Beatmap))
            {
                string folderName = addresses.Beatmap.FolderName;
                string fileName = addresses.Beatmap.OsuFileName;

                if (string.IsNullOrEmpty(folderName) || string.IsNullOrEmpty(fileName))
                {
                    return (new BeatmapData());
                }

                BeatmapData beatmapData = new BeatmapData();
                beatmapData = GetMetadata(songsPath, folderName, fileName);

                return (beatmapData);
            }
            
            return (new BeatmapData());
        }

        // Metadata extraction from .osu file, returns artist, title, version, OD and HP as strings
        public BeatmapData GetMetadata(string songsPath, string folderName, string fileName)
        {
            string artist = "";
            string title = "";
            string version = "";
            string od = "";
            string hp = "";

            BeatmapData beatmapData = new BeatmapData();

            string fullPath = Path.Combine(songsPath, folderName, fileName);

            foreach (string line in File.ReadLines(fullPath))
            {
                if (line.StartsWith("Artist:"))
                    artist = line.Split(':')[1].Trim();

                else if (line.StartsWith("Title:"))
                    title = line.Split(':')[1].Trim();

                else if (line.StartsWith("Version:"))
                    version = line.Split(':')[1].Trim();

                else if (line.StartsWith("OverallDifficulty:"))
                    od = line.Split(':')[1].Trim();

                else if (line.StartsWith("HPDrainRate:"))
                    hp = line.Split(':')[1].Trim();

                if (!string.IsNullOrEmpty(artist) &&
                    !string.IsNullOrEmpty(title) &&
                    !string.IsNullOrEmpty(version) &&
                    !string.IsNullOrEmpty(od) &&
                    !string.IsNullOrEmpty(hp))
                {
                    break;
                }
            }

            beatmapData.SetBeatmapData(folderName, fileName, artist, title, version, od, hp);

            return (beatmapData);
        }
    }
}
