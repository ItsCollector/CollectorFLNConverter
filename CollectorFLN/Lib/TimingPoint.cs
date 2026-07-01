namespace CollectorFLN
{
    public class TimingPoint
    {
        public double offset { get; set; }
        public double beatLength { get; set; }
        public int meter { get; set; }
        public int sampleSet { get; set; }
        public int sampleIndex { get; set; }
        public int volume { get; set; }
        public bool isInherited { get; set; }
        public int effects { get; set; }

        public TimingPoint(double offset, double beatLength, int meter, int sampleSet, int sampleIndex, int volume, bool isInherited, int effects)
        {
            this.offset = offset;
            this.beatLength = beatLength;
            this.meter = meter;
            this.sampleSet = sampleSet;
            this.sampleIndex = sampleIndex;
            this.volume = volume;
            this.isInherited = isInherited;
            this.effects = effects;
        }
    }
}
