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
        private StructuredOsuMemoryReader _reader;
        #pragma warning restore CS8618 

        public void Initialisation()
        {
            _reader = StructuredOsuMemoryReader.Instance;
            Config config = new Config();
            string songsPath = config.SongPath;
        }

        // Retrieves map data from osu! memory and extracts metadata from the beatmap file
        public (int beatmapId, string folderName, string fileName, string artist, string title, string version, string od, string hp) GetMapData(string songsPath)
        {
            var addresses = _reader.OsuMemoryAddresses;

            _reader.TryRead(addresses.GeneralData);
            
            if (_reader.TryRead(addresses.Beatmap))
            {
                int beatmapId = addresses.Beatmap.Id;
                string folderName = addresses.Beatmap.FolderName;
                string fileName = addresses.Beatmap.OsuFileName;
                
                (string artist, string title, string version, string od, string hp) = GetMetadata(Path.Combine(songsPath, folderName, fileName));

                return (beatmapId, folderName, fileName, artist, title, version, od, hp);
            }
            
            return (-1, string.Empty, string.Empty, string.Empty, string.Empty, string.Empty, string.Empty, string.Empty);
        }

        // Metadata extraction from .osu file, returns artist, title, version, OD and HP as strings
        public (string artist, string title, string version, string od, string hp) GetMetadata(string fullPath)
        {
            string artist = "";
            string title = "";
            string version = "";
            string od = "";
            string hp = "";

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

            return (artist, title, version, od, hp);
        }
    }
}
