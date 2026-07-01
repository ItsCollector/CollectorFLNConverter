using System.Diagnostics;

namespace CollectorFLN.Lib.Converters
{
    public static class LNRemover
    {
        public static List<HitObject> RemoveLN(List<HitObject> originalHitObjects)
        {
            Debug.Assert(originalHitObjects != null, "LNRemover.RemoveLN(): originalHitObjects cannot be null");
            Debug.Assert(originalHitObjects.Count > 0, "LNRemover.RemoveLN(): originalHitObjects cannot be empty");

            List<HitObject> noLnHitObjects = new List<HitObject>();

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

                    noLnHitObjects.Add(new HitObject
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

                HitObject lastNote = columnHitObjects.Last();

                /* Adds an additional object for the end of a long note if its the last object in the map.
                 * This is to preserve the average BPM of the map as the map duration is taken into consideration */
                if (lastNote.startTime != lastNote.endTime) // scope issue?
                {
                    noLnHitObjects.Add(new HitObject
                    (
                        columnIndex,
                        lastNote.endTime,
                        lastNote.endTime,
                        lastNote.hitsound,
                        lastNote.SampleSet,
                        lastNote.AdditionSet,
                        lastNote.CustomIndex,
                        lastNote.Volume,
                        lastNote.Filename
                    ));
                }
            }

            return noLnHitObjects;
        }
    }
}
