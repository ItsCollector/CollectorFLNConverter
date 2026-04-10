using System.IO.Compression;

namespace CollectorFLN
{
    public class Converter
    {
        // Converts the original hit objects into FLN format, using the specified gap to determine LN lengths
        public static List<HitObject> CreateFLN(List<HitObject> originalHitObjects, int gap)
        {       
            int minimumLnLengthMs = 20;

            List<HitObject> flnHitObjects = new List<HitObject>();

            // Group hit objects by column and sort by start time
            var hitObjectsByColumn = originalHitObjects
                .GroupBy(hitObject => hitObject.column)
                .ToDictionary(
                    columnGroup => columnGroup.Key,
                    columnGroup => columnGroup
                        .OrderBy(hitObject => hitObject.startTime)
                        .ToList()
                );

            foreach (var columnEntry in hitObjectsByColumn)
            {
                int columnIndex = columnEntry.Key;
                List<HitObject> columnHitObjects = columnEntry.Value;

                for (int noteIndex = 0; noteIndex < columnHitObjects.Count; noteIndex++)
                {
                    HitObject currentNote = columnHitObjects[noteIndex];

                    int startTime = currentNote.startTime;
                    int endTime;

                    // If there is a next note, end before it (with gap)
                    if (noteIndex < columnHitObjects.Count - 1)
                    {
                        HitObject nextNote = columnHitObjects[noteIndex + 1];
                        endTime = nextNote.startTime - gap;
                    }
                    else
                    {
                        // Last note in column → give default length
                        endTime = startTime + 150;
                    }

                    // Prevent invalid durations
                    if (endTime <= startTime)
                    {
                        endTime = startTime;
                    }

                    int noteDuration = endTime - startTime;

                    // Convert very short LN → rice
                    if (noteDuration < minimumLnLengthMs)
                    {
                        flnHitObjects.Add(new HitObject
                        (
                            columnIndex,
                            startTime,
                            startTime
                        ));
                    }
                    else
                    {
                        flnHitObjects.Add(new HitObject
                        (
                            columnIndex,
                            startTime,
                            endTime
                        ));
                    }
                }
            }

            // Sort final result (important for writing back to file)
            return flnHitObjects
                .OrderBy(hitObject => hitObject.startTime)
                .ThenBy(hitObject => hitObject.column)
                .ToList();
        }

        // Normalizes all timing points so the map scrolls at a constant visual speed.
        // Red lines (uninherited) set BPM. Green lines (inherited) set SV as a negative inverse (-100 / SV).
        // Output: a new list of green lines that cancel BPM changes, with all intentional SVs removed.
        public static List<TimingPoint> NormalizeTimingPoints(List<TimingPoint> timingPoints, double targetBpm = -1)
        {
            if (timingPoints == null || timingPoints.Count == 0)
            {
                return new List<TimingPoint>();
            }

            var redLines = timingPoints.Where(tp => !tp.isInherited).ToList();

            if (redLines.Count == 0)
            {
                return timingPoints.ToList();
            }

            if (targetBpm <= 0)
            {
                targetBpm = GetDominantBpm(redLines);
            }

            var result = new List<TimingPoint>();

            foreach (var red in redLines)
            {
                result.Add(red);
            }

            var allOffsets = timingPoints
                .Select(tp => tp.offset)
                .Distinct()
                .OrderBy(o => o)
                .ToList();

            TimingPoint currentRed = redLines[0];

            foreach (var offset in allOffsets)
            {
                var redAtOffset = redLines.LastOrDefault(r => r.offset <= offset);

                if (redAtOffset != null)
                {
                    currentRed = redAtOffset;
                }

                double currentBpm = 60000.0 / currentRed.beatLength;
                double svMultiplier = targetBpm / currentBpm;
                double normalizedBeatLength = -100.0 / svMultiplier;

                var original = timingPoints.LastOrDefault(tp => tp.offset == offset);

                result.Add(new TimingPoint(
                    offset,
                    normalizedBeatLength,
                    original?.meter ?? 4,
                    original?.sampleSet ?? 0,
                    original?.sampleIndex ?? 0,
                    original?.volume ?? 100,
                    true,   // green line
                    original?.effects ?? 0
                ));
            }

            return result.OrderBy(tp => tp.offset).ThenBy(tp => tp.isInherited ? 1 : 0).ToList();
        }

        /// Returns the BPM that covers the most time in the map.
        public static double GetDominantBpm(List<TimingPoint> redLines)
        {
            // Pair each red line with the offset of the next one to measure its duration
            var durations = new Dictionary<double, double>();

            for (int i = 0; i < redLines.Count; i++)
            {
                double bpm = 60000.0 / redLines[i].beatLength;
                double start = redLines[i].offset;
                double end = i + 1 < redLines.Count ? redLines[i + 1].offset : start + 9999999;
                double span = end - start;

                if (!durations.ContainsKey(bpm))
                {
                    durations[bpm] = 0;
                }

                durations[bpm] += span;
            }

            return durations.OrderByDescending(kv => kv.Value).First().Key;
        }

        // Writes a new .osu file with the FLN hit objects and specified metadata, then creates an .osz archive containing the new .osu file and returns the path to the .osz
        public string WriteNewOsuFile(string songsPath, string folderName, string originalFileName, List<TimingPoint> timingPoints, List<HitObject> flnHitObjects, int keyCount, int gap, bool removeSV, float hp, float od)
        {
            string originalPath = Path.Combine(songsPath, folderName, originalFileName);
            string newFileName;

            // Create new filename for the FLN .osu file
            if (removeSV)
            {
                newFileName = $"{Path.GetFileNameWithoutExtension(originalFileName)}_FLN_G{gap}_OD{od}_HP{hp}_NSV.osu";
            }
            else
            {
                newFileName = $"{Path.GetFileNameWithoutExtension(originalFileName)}_FLN_G{gap}_OD{od}_HP{hp}.osu";
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
                        outputLines.Add(line + $" [FLN | {gap}ms | OD {od} | HP {hp} | NSV]");

                    }
                    else
                    {
                        outputLines.Add(line + $" [FLN | {gap}ms | OD {od} | HP {hp}]");
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
                    inTimingPoints = true;

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
                    continue;
                }

                if (line.StartsWith("[HitObjects]"))
                {
                    outputLines.Add(line);
                    inHitObjects = true;

                    // Write FLN objects
                    foreach (var hitObject in flnHitObjects)
                    {
                        string osuLine = ConvertToOsuFormat(hitObject, keyCount);
                        outputLines.Add(osuLine);
                    }

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

            // Rice note
            if (hitObject.startTime == hitObject.endTime)
            {
                return $"{x},{y},{hitObject.startTime},1,0,0:0:0:0:";
            }

            // Long note
            return $"{x},{y},{hitObject.startTime},128,0,{hitObject.endTime}:0:0:0:0:";
        }
    }
}
