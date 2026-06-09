using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CollectorFLN.Lib
{
    internal static class BeatmapParser
    {
        // Extracts hit objects and key count from the .osu file of the beatmap
        public static (List<TimingPoint>, List<HitObject>, int keyCount) ExtractData(string songsPath, string folderName, string fileName)
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
                    inTimingPoints = false;
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
                        try
                        {
                            TimingPoint timingPoint = ParseTimingPoint(line);
                            timingPoints.Add(timingPoint);
                        }
                        catch (Exception ex)
                        {
                            //Console.WriteLine($"[FATAL] Timing point parse failed in file: '{fullPath}'");
                            //Console.WriteLine($"[FATAL] Offending line: '{line}'");
                            //Console.WriteLine($"[FATAL] {ex.Message}");
                            throw;
                        }
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

        // Helper method to parse TimingPoint lines from .osu files
        public static TimingPoint ParseTimingPoint(string line)
        {
            var parts = line.Split(',');

            try
            {
                double offset = double.Parse(parts[0].Trim());
                double beatLength = double.Parse(parts[1].Trim());
                int meter = parts.Length > 2 ? int.Parse(parts[2].Trim()) : 4;
                int sampleSet = parts.Length > 3 ? int.Parse(parts[3].Trim()) : 0;
                int sampleIndex = parts.Length > 4 ? int.Parse(parts[4].Trim()) : 0;
                int volume = parts.Length > 5 ? int.Parse(parts[5].Trim()) : 100;
                bool isInherited = parts.Length > 6 && parts[6].Trim() == "0";
                int effects = parts.Length > 7 ? int.Parse(parts[7].Trim()) : 0;

                return new TimingPoint(offset, beatLength, meter, sampleSet, sampleIndex, volume, isInherited, effects);
            }
            catch (Exception ex)
            {
                //Console.WriteLine($"[ERROR] Failed to parse timing point.");
                //Console.WriteLine($"[ERROR] Line: '{line}'");
                //Console.WriteLine($"[ERROR] Exception: {ex.Message}");
                throw;
            }
        }

        // Helper method to parse HitObject lines from .osu files
        public static HitObject ParseHitObject(string line, int keyCount)
        {
            //Console.WriteLine($"[DEBUG] Parsing hit object: '{line}'");

            try
            {
                var parts = line.Split(',');

                int x = int.Parse(parts[0]);
                int time = int.Parse(parts[2]);
                int type = int.Parse(parts[3]);
                int hitsound = int.Parse(parts[4]);
                var extras = parts[5].Split(':');

                string sampleSet = extras[0];
                string additionSet = extras[1];
                string customIndex = extras[2];
                string volume = extras[3];
                string filename = extras.Length > 4 ? extras[4] : "";

                int column = (int)(x * keyCount / 512);

                return new HitObject(column, time, type, hitsound, sampleSet, additionSet, customIndex, volume, filename);
            }
            catch (Exception ex)
            {
                //Console.WriteLine($"[ERROR] Failed to parse hit object.");
                //Console.WriteLine($"[ERROR] Line: '{line}'");
                //Console.WriteLine($"[ERROR] Exception: {ex.Message}");
                throw;
            }
        }
    }
}
