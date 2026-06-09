namespace CollectorFLN
{
    public class HitObject
    {
        public int column { get; set; }
        public int startTime { get; set; }
        public int endTime { get; set; }
        public int hitsound { get; set; }
        public string SampleSet { get; set; }
        public string AdditionSet { get; set; }
        public string CustomIndex { get; set; }
        public string Volume { get; set; }
        public string Filename { get; set; }

        public HitObject(int column, int startTime, int endTime, int hitsound, string sampleSet, string additionSet, string customIndex, string volume, string filename)
        {
            this.column = column;
            this.startTime = startTime;
            this.endTime = endTime;
            this.hitsound = hitsound;
            this.SampleSet = sampleSet;
            this.AdditionSet = additionSet;
            this.CustomIndex = customIndex;
            this.Volume = volume;
            this.Filename = filename;
        }
    }
}
