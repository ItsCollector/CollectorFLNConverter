using System.IO.Compression;

namespace CollectorFLN
{
    public class Converter
    {
        // Extracts hit objects and key count from the .osu file of the beatmap
        public (List<HitObject>, int keyCount) ExtractHitObjects(string songsPath, string folderName, string fileName)
        {
            int mapGamemode = GetMapGamemode(songsPath, folderName, fileName);
            List<HitObject> hitObjects = new List<HitObject>();
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

                if (line.StartsWith("[HitObjects]"))
                {
                    inHitObjects = true;
                    continue;
                }

                if (inHitObjects)
                {
                    if (string.IsNullOrWhiteSpace(line) || line.StartsWith("["))
                    {
                        continue;
                    }
                    else
                    {
                        HitObject hitObject = CreateHitObject(line, keyCount);
                        hitObjects.Add(hitObject);
                    }
                }
            }

            return (hitObjects, keyCount);
        }

        // Extracts the game mode of the beatmap from the .osu file, returns -1 if not found or error occurs
        public int GetMapGamemode(string songsPath, string folderName, string fileName)
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

        // Converts a line from the .osu file into a HitObject, using the key count to determine the column
        HitObject CreateHitObject(string line, int keyCount)
        {
            var parts = line.Split(',');

            int x = int.Parse(parts[0]);
            int time = int.Parse(parts[2]);
            int type = int.Parse(parts[3]);

            // Convert x → column
            int column = (int)(x * keyCount / 512);

            int endTime = time;

            // Check if LN 
            bool isLN = (type & 128) > 0;

            if (isLN)
            {
                var lnParts = parts[5].Split(':');
                endTime = int.Parse(lnParts[0]);
            }

            HitObject hitObject = new HitObject
            {
                column = column,
                startTime = time,
                endTime = endTime
            };

            return hitObject;
        }

        // Converts the original hit objects into FLN format, using the specified gap to determine LN lengths
        public List<HitObject> CreateFLN(List<HitObject> originalHitObjects, int gap)
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
                        {
                            column = columnIndex,
                            startTime = startTime,
                            endTime = startTime
                        });
                    }
                    else
                    {
                        flnHitObjects.Add(new HitObject
                        {
                            column = columnIndex,
                            startTime = startTime,
                            endTime = endTime
                        });
                    }
                }
            }

            // Sort final result (important for writing back to file)
            return flnHitObjects
                .OrderBy(hitObject => hitObject.startTime)
                .ThenBy(hitObject => hitObject.column)
                .ToList();
        }

        // Writes a new .osu file with the FLN hit objects and specified metadata, then creates an .osz archive containing the new .osu file and returns the path to the .osz
        public string WriteNewOsuFile(string songsPath, string folderName, string originalFileName, List<HitObject> flnHitObjects, int keyCount, int gap, float hp, float od)
        {
            string originalPath = Path.Combine(songsPath, folderName, originalFileName);

            // Create new filename for the FLN .osu file
            string newFileName = $"{Path.GetFileNameWithoutExtension(originalFileName)}_FLN_G{gap}_OD{od}_HP{hp}.osu";
            string newPath = Path.Combine(songsPath, folderName, newFileName);

            List<string> outputLines = new List<string>();
            bool inHitObjects = false;

            foreach (string line in File.ReadLines(originalPath))
            {
                if (line.StartsWith("Version:"))
                {
                    outputLines.Add(line + $" [FLN | {gap}ms | OD {od} | HP {hp}]");
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
        private string ConvertToOsuFormat(HitObject hitObject, int keyCount)
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
