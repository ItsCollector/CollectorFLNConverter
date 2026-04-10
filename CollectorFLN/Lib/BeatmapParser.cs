using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CollectorFLN.Lib
{
    internal class BeatmapParser
    {
        // Extracts hit objects and key count from the .osu file of the beatmap
        public (List<TimingPoint>, List<HitObject>, int keyCount) ExtractData(string songsPath, string folderName, string fileName)
        {
            List<HitObject> hitObjects = new List<HitObject>();
            List<TimingPoint> timingPoints = new List<TimingPoint>();

            bool inTimingPoints = false;
            bool inHitObjects = false;
            int keyCount = 4;

            // Set full string path to the beatmap
            string fullPath = Path.Combine(songsPath, folderName, fileName);

            // Parse beatmap file to extract hit objects
            foreach (string line in File.ReadLines(fullPath))
            {
                if (line.StartsWith("CircleSize:"))
                {
                    keyCount = Int32.Parse(line.Split(':')[1].Trim());
                }

                if (line.StartsWith("[TimingPoints]"))
                {
                    inTimingPoints = true;
                    continue;
                }

                if (line.StartsWith("[HitObjects]"))
                {
                    inHitObjects = true;
                    continue;
                }

                if (inTimingPoints)
                {
                    if (string.IsNullOrWhiteSpace(line) || line.StartsWith("["))
                    {
                        inTimingPoints = false;
                    }
                    else
                    {
                        TimingPoint timingPoint = ParseTimingPoint(line);
                        timingPoints.Add(timingPoint);
                        continue;
                    }
                }

                if (inHitObjects)
                {
                    if (string.IsNullOrWhiteSpace(line) || line.StartsWith("["))
                    {
                        inHitObjects = false;
                    }
                    else
                    {
                        hitObjects.Add(ParseHitObject(line, keyCount));
                        continue;
                    }
                }
            }

            return (timingPoints, hitObjects, keyCount);
        }

        // Extracts the game mode of the beatmap from the .osu file, returns -1 if not found or error occurs
        public static int GetMapGamemode(string songsPath, string folderName, string fileName)
        {
            string fullPath = Path.Combine(songsPath, folderName, fileName);

            if (!File.Exists(fullPath))
            {
                Console.WriteLine($"\nError: File not found at {fullPath}");
                return -1;
            }

            // Check mode 
            foreach (string line in File.ReadLines(fullPath))
            {
                if (line.StartsWith("Mode:"))
                {
                    int mapGamemode = Int32.Parse(line.Split(':')[1].Trim());

                    return mapGamemode;
                }
            }

            return -1;
        }

        // Helper method to parse TimingPoint lines from .osu files
        public static TimingPoint ParseTimingPoint(string line)
        {
            var parts = line.Split(',');

            double offset = double.Parse(parts[0].Trim());
            double beatLength = double.Parse(parts[1].Trim());
            int meter = int.Parse(parts[2].Trim());
            int sampleSet = int.Parse(parts[3].Trim());
            int sampleIndex = int.Parse(parts[4].Trim());
            int volume = int.Parse(parts[5].Trim());
            bool isInherited = parts[6].Trim() == "0";
            int effects = int.Parse(parts[7].Trim());

            return new TimingPoint(offset, beatLength, meter, sampleSet, sampleIndex, volume, isInherited, effects);

        }

        // Helper method to parse HitObject lines from .osu files
        public static HitObject ParseHitObject(string line, int keyCount)
        {
            var parts = line.Split(',');

            int x = int.Parse(parts[0]);
            int time = int.Parse(parts[2]);
            int type = int.Parse(parts[3]);

            int column = (int)(x * keyCount / 512);

            int endTime = time;

            bool isLN = (type & 128) > 0;

            if (isLN)
            {
                var lnParts = parts[5].Split(':');
                endTime = int.Parse(lnParts[0]);
            }

            return new HitObject(column, time, endTime);
        }
    }
}
