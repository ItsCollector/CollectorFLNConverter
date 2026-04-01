namespace CollectorFLN
{
    public class HitObject
    {
        public int column { get; set; }
        public int startTime { get; set; }
        public int endTime { get; set; }

        public HitObject(int column, int startTime, int endTime)
        {
            this.column = column;
            this.startTime = startTime;
            this.endTime = endTime;
        }
    }
}
