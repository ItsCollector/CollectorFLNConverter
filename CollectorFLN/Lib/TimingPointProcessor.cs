namespace CollectorFLN.Lib
{
    public static class TimingPointProcessor
    {
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

        // Helper to find target BPM for green line editing 
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

        // Checks if timing points are already normalised by the mapper
        public static bool CheckForNormalisation(List<TimingPoint> timingPoints, double targetBpm)
        {
            double currentBpm = 0;
            bool foundGreenLine = false;

            var redLines = timingPoints.Where(tp => !tp.isInherited).ToList();
            var greenOffsets = timingPoints.Where(tp => tp.isInherited).Select(tp => tp.offset).ToHashSet();

            // Every red line must have a corresponding green line
            foreach (var red in redLines)
            {
                if (!greenOffsets.Contains(red.offset))
                {
                    Console.WriteLine($"[DEBUG] Red line at {red.offset} has no green line - NOT normalised");
                    return false;
                }
            }

            for (int i = 0; i < timingPoints.Count; ++i)
            {
                if (!timingPoints[i].isInherited)
                {
                    currentBpm = 60000 / timingPoints[i].beatLength;
                }
                else
                {
                    foundGreenLine = true;
                    double expectedSv = targetBpm / currentBpm;
                    double actualSv = 100 / Math.Abs(timingPoints[i].beatLength);

                    if (Math.Abs(expectedSv - actualSv) > 0.01)
                    {
                        Console.WriteLine("[DEBUG] This map is NOT normalised");
                        return false;
                    }
                }
            }

            if (!foundGreenLine)
            {
                return false;
            }

            Console.WriteLine("[DEBUG] This map is already normalised");
            return true;
        }

        // Identifies if multiple BPMs changes exist 
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
