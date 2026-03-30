namespace CollectorFLN
{
    public class HitObject
    {
        public int column { get; set; }
        public int startTime { get; set; }
        public int endTime { get; set; }
        public bool isLongNote => endTime > startTime;
    }
}
