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
        public static List<TimingPoint> NormaliseTimingPoints(List<TimingPoint> timingPoints, double targetBpm = -1)
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
    }
}
