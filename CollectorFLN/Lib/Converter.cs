using System.IO.Compression;

namespace CollectorFLN
{
    public static class Converter
    {
        // Converts the original hit objects into FLN format, using the specified gap to determine LN lengths
        public static List<HitObject> CreateMsBasedFLN(List<HitObject> originalHitObjects, int gap)
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
                            startTime,
                            currentNote.hitsound,
                            currentNote.SampleSet,
                            currentNote.AdditionSet,
                            currentNote.CustomIndex,
                            currentNote.Volume,
                            currentNote.Filename
                        ));
                    }
                    else
                    {
                        flnHitObjects.Add(new HitObject
                        (
                            columnIndex,
                            startTime,
                            endTime,
                            currentNote.hitsound,
                            currentNote.SampleSet,
                            currentNote.AdditionSet,
                            currentNote.CustomIndex,
                            currentNote.Volume,
                            currentNote.Filename
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

        // Converts the original hit objects into FLN format using snap-based gaps.
        // The gap is computed dynamically from BPM: gap = 60000 / bpm / snapDivisor.
        // Minimum LN length is also snap-aware: minLength = max(60000 / bpm / snapDivisor, 20ms).
        public static List<HitObject> CreateSnappedBasedFLN(List<HitObject> originalHitObjects, List<TimingPoint> timingPoints, int snapDivisor)
        {
            int minimumLnLengthMs = 20;

            // Avoid 1-2ms rounding differences making some LNs rice when they
            // should be LNs, matching the reference converter's approach.
            int minLengthLeniency = 2;

            // Extract red lines (uninherited timing points that define BPM)
            var redLines = timingPoints
                .Where(tp => !tp.isInherited)
                .OrderBy(tp => tp.offset)
                .ToList();

            if (redLines.Count == 0)
            {
                // Fallback: no BPM info, use a default 120 BPM
                redLines.Add(new TimingPoint(0, 500, 4, 0, 0, 100, false, 0));
            }

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

                    // Find the active red line (BPM section) for this note
                    TimingPoint activeRedLine = redLines[0];
                    for (int r = redLines.Count - 1; r >= 0; r--)
                    {
                        if (redLines[r].offset <= startTime)
                        {
                            activeRedLine = redLines[r];
                            break;
                        }
                    }

                    double bpm = 60000.0 / activeRedLine.beatLength;
                    double snapGap = 60000.0 / bpm / snapDivisor;
                    double snapMinLength = Math.Max(60000.0 / bpm / snapDivisor, minimumLnLengthMs);

                    if (noteIndex < columnHitObjects.Count - 1)
                    {
                        HitObject nextNote = columnHitObjects[noteIndex + 1];
                        endTime = nextNote.startTime - (int)Math.Round(snapGap);
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

                    // If LN is shorter than the snap-based minimum (with 2ms leniency for rounding), convert to rice
                    if (noteDuration < (int)Math.Round(snapMinLength) - minLengthLeniency)
                    {
                        flnHitObjects.Add(new HitObject(
                            columnIndex,
                            startTime,
                            startTime,
                            currentNote.hitsound,
                            currentNote.SampleSet,
                            currentNote.AdditionSet,
                            currentNote.CustomIndex,
                            currentNote.Volume,
                            currentNote.Filename
                        ));
                    }
                    else
                    {
                        flnHitObjects.Add(new HitObject(
                            columnIndex,
                            startTime,
                            endTime,
                            currentNote.hitsound,
                            currentNote.SampleSet,
                            currentNote.AdditionSet,
                            currentNote.CustomIndex,
                            currentNote.Volume,
                            currentNote.Filename
                        ));
                    }
                }
            }

            return flnHitObjects
                .OrderBy(hitObject => hitObject.startTime)
                .ThenBy(hitObject => hitObject.column)
                .ToList();
        }

        // Normalizes all timing points so the map scrolls at a constant visual speed.
        // Red lines (uninherited) set BPM. Green lines (inherited) set SV as a negative inverse (-100 / SV).
        // Output: a new list of green lines that cancel BPM changes, with all intentional SVs removed.
        public static List<TimingPoint> NormaliseTimingPoints(List<TimingPoint> timingPoints, double targetBpm)
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
                double normalisedBeatLength = -100.0 / svMultiplier;

                var original = timingPoints.LastOrDefault(tp => tp.offset == offset);

                result.Add(new TimingPoint(
                    offset,
                    normalisedBeatLength,
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

        public static double FindTargetBpm(List<TimingPoint> timingPoints)
        {
            double currentBpm = 0;

            for (int i = 0; i < timingPoints.Count; i++)
            {
                if (!timingPoints[i].isInherited) // record red line BPM
                {
                    currentBpm = 60000 / timingPoints[i].beatLength;
                }
                else // check if the SV is equal to 1.0x - this marks the target BPM
                {
                    double sv = 100 / Math.Abs(timingPoints[i].beatLength);

                    if (Math.Abs(sv - 1.0) < 0.001)
                    {
                        break;
                    }
                }
            }

            Console.WriteLine($"[DEBUG] Target BPM: {currentBpm}");
            return currentBpm;
        }

        public static bool CheckForNormalisation(List<TimingPoint> timingPoints, double targetBpm)
        {
            double currentBpm = 0;
            for (int i = 0; i < timingPoints.Count; ++i)
            {
                if (!timingPoints[i].isInherited)
                {
                    currentBpm = 60000 / timingPoints[i].beatLength;
                }
                else
                {
                    double expectedSv = targetBpm / currentBpm;
                    double actualSv = 100 / Math.Abs(timingPoints[i].beatLength);
                    Console.WriteLine($"[DEBUG] currentBpm={currentBpm}, expectedSV={expectedSv}, actualSV={actualSv}, diff={Math.Abs(expectedSv - actualSv)}");
                    
                    if (Math.Abs(expectedSv - actualSv) > 0.01)
                    {
                        Console.WriteLine("[DEBUG] This map is NOT normalised");
                        return false;
                    }
                }
            }
            Console.WriteLine("[DEBUG] This map is already normalised");
            return true;
        }

        public static bool MultiBpmCheck(List<TimingPoint> timingPoints)
        {
            double firstBpm = timingPoints[0].beatLength;
            bool multiBpmFlag = false;

            for (int i = 1; i < timingPoints.Count; i++)
            {
                // check for red line 
                if (timingPoints[i].isInherited)
                {
                    continue;
                }

                if (firstBpm != timingPoints[i].beatLength)
                {
                    multiBpmFlag = true;
                    break;
                }
            }

            Console.WriteLine($"[DEBUG] Multi-BPM flag: {multiBpmFlag}");
            return multiBpmFlag;
        }
    }
}
