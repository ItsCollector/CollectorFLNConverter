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
                        if (currentNote.startTime < currentNote.endTime)
                        {
                            endTime = currentNote.endTime;
                        }
                        else
                        {
                            endTime = startTime + 150;
                        }
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
                .ThenByDescending(hitObject => hitObject.column)
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
                        if (currentNote.startTime < currentNote.endTime)
                        {
                            endTime = currentNote.endTime;
                        }
                        else
                        {
                            endTime = startTime + (int)Math.Round(snapGap);
                        }
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
                .ThenByDescending(hitObject => hitObject.column)
                .ToList();
        }
    }
}
