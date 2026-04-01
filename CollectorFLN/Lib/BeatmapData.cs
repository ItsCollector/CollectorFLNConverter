using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CollectorFLN.Lib
{
    public class BeatmapData
    {
        public string folderName = string.Empty;
        public string fileName = string.Empty;
        public string artist = string.Empty;
        public string title = string.Empty;
        public string version = string.Empty;
        public string od = string.Empty;
        public string hp = string.Empty;

        /* BeatmapData will be an object with empty data initially, 
         * and will be populated with the actual data when the metadata is extracted from the .osu file */
        public BeatmapData() { }

        public void SetBeatmapData(string folderName, string fileName, string artist, string title, string version, string od, string hp)
        {
            this.folderName = folderName;
            this.fileName = fileName;
            this.artist = artist;
            this.title = title;
            this.version = version;
            this.od = od;
            this.hp = hp;
        }
    }
}
