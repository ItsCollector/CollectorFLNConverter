using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Text.RegularExpressions;

namespace CollectorFLN.Lib
{
    public static class BeatmapWriter
    {
        // Writes a new .osu file with the FLN hit objects and specified metadata, then creates an .osz archive containing the new .osu file and returns the path to the .osz
        public static string WriteNewOsuFile(string songsPath, string folderName, string originalFileName, List<TimingPoint> timingPoints, List<HitObject> flnHitObjects, int keyCount, int gap, bool removeSV, float hp, float od, bool useSnapMode = false, int snapDivisor = 4)
        {
            string originalPath = Path.Combine(songsPath, folderName, originalFileName);
            string newFileName;

            // Build the gap tag for filename and version string
            string gapFileTag;
            string gapVersionTag;

            if (useSnapMode)
            {
                gapFileTag = $"S1-{snapDivisor}";
                gapVersionTag = $"1/{snapDivisor}";
            }
            else
            {
                gapFileTag = $"G{gap}";
                gapVersionTag = $"{gap}ms";
            }

            
            string titlePart = Path.GetFileNameWithoutExtension(originalFileName);
            Console.WriteLine($"[DEBUG] Old: {titlePart}_FLN_{gapFileTag}_OD{od}_HP{hp}_NSV.osu");


            int maxTitleLength = 20;

            if (titlePart.Length > maxTitleLength)
            {
                titlePart = titlePart.Substring(0, 10) + "_" + titlePart.Substring(titlePart.Length - 9);
            }

            Console.WriteLine($"[DEBUG] New: {titlePart}_FLN_{gapFileTag}_OD{od}_HP{hp}_NSV.osu");

            // Create new filename for the FLN .osu file
            if (removeSV)
            {
                newFileName = $"{titlePart}_FLN_{gapFileTag}_OD{od}_HP{hp}_NSV.osu";
            }
            else
            {
                newFileName = $"{titlePart}_FLN_{gapFileTag}_OD{od}_HP{hp}.osu";
            }

            string newPath = Path.Combine(songsPath, folderName, newFileName);

            List<string> outputLines = new List<string>();
            bool inTimingPoints = false;
            bool inHitObjects = false;

            foreach (string line in File.ReadLines(originalPath))
            {
                // Fix outdated file format version
                if (line.StartsWith("osu file format"))
                {
                    outputLines.Add("osu file format v14");
                    continue;
                }

                if (line.StartsWith("Version:"))
                {
                    if (removeSV)
                    {
                        outputLines.Add(line + $" [FLN | {gapVersionTag} | OD {od} | HP {hp} | NSV]");

                    }
                    else
                    {
                        outputLines.Add(line + $" [FLN | {gapVersionTag} | OD {od} | HP {hp}]");
                    }

                    continue;
                }
                if (line.StartsWith("OverallDifficulty:"))
                {
                    outputLines.Add($"OverallDifficulty: {od}");
                    continue;
                }
                if (line.StartsWith("HPDrainRate:"))
                {
                    outputLines.Add($"HPDrainRate: {hp}");
                    continue;
                }
                if (line.StartsWith("Tags:"))
                {
                    var parts = line.Substring(5).Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries).ToList();

                    if (!parts.Contains("CollectorFLN"))
                        parts.Add("CollectorFLN");

                    if (!parts.Contains("FLN"))
                        parts.Add("FLN");

                    string newLine = "Tags: " + string.Join(" ", parts);
                    outputLines.Add(newLine);
                    continue;
                }

                if (line.StartsWith("[TimingPoints]"))
                {
                    outputLines.Add(line);

                    // Write normalized timing points
                    foreach (var timingPoint in timingPoints)
                    {
                        string osuLine =
                            $"{timingPoint.offset}," +
                            $"{timingPoint.beatLength}," +
                            $"{timingPoint.meter},{timingPoint.sampleSet}," +
                            $"{timingPoint.sampleIndex},{timingPoint.volume}," +
                            $"{(timingPoint.isInherited ? 0 : 1)}," +
                            $"{timingPoint.effects}";

                        outputLines.Add(osuLine);
                    }

                    inTimingPoints = true;
                    continue;
                }

                if (line.StartsWith("[HitObjects]"))
                {
                    outputLines.Add(line);

                    // Write FLN objects
                    foreach (var hitObject in flnHitObjects)
                    {
                        string osuLine = ConvertToOsuFormat(hitObject, keyCount);
                        outputLines.Add(osuLine);
                    }

                    inHitObjects = true;
                    continue;
                }

                if (inTimingPoints)
                {
                    if (line.StartsWith("["))
                    {
                        inTimingPoints = false;
                        outputLines.Add(line);
                    }
    
                    continue;
                }


                if (inHitObjects)
                {
                    if (line.StartsWith("["))
                    {
                        inHitObjects = false;
                        outputLines.Add(line);
                    }

                    continue;
                }

                outputLines.Add(line);
            }

            // Write the FLN .osu file
            File.WriteAllLines(newPath, outputLines);

            // Create .osz in the program's working directory
            string workingDir = Environment.CurrentDirectory;
            string zipFileName = Path.GetFileNameWithoutExtension(folderName) + ".osz";
            string zipPath = Path.Combine(workingDir, zipFileName);

            using (var zip = ZipFile.Open(zipPath, ZipArchiveMode.Create))
            {
                zip.CreateEntryFromFile(newPath, newFileName);
            }

            return zipPath;
        }

        // Helper method to convert a HitObject into the string format used in .osu files, using the key count to determine the x position and whether it's a long note or rice note to determine the type and end time
        private static string ConvertToOsuFormat(HitObject hitObject, int keyCount)
        {
            int x = (int)((hitObject.column + 0.5) * 512 / keyCount);
            int y = 192;

            string hitsound = $"{hitObject.SampleSet}:{hitObject.AdditionSet}:{hitObject.CustomIndex}:{hitObject.Volume}:{hitObject.Filename}";

            // Rice note
            if (hitObject.startTime == hitObject.endTime)
            {
                return $"{x},{y},{hitObject.startTime},1,{hitObject.hitsound},{hitsound}";
            }

            // Long note
            return $"{x},{y},{hitObject.startTime},128,{hitObject.hitsound},{hitObject.endTime}:{hitsound}";
        }
    }
}
