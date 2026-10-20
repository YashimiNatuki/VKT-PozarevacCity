using System;

namespace Pozarevac.CityGame
{
    // O(1) ingestion; sorting/copying occurs only when a diagnostic snapshot is requested.
    public sealed class CityFrameWindow
    {
        public readonly struct Summary
        {
            public readonly int Frames;
            public readonly double Seconds, Fps, MedianMilliseconds, P95Milliseconds;
            public Summary(int frames,double seconds,double median,double p95)
            {Frames=frames;Seconds=seconds;Fps=seconds>0?frames/seconds:0;MedianMilliseconds=median*1000;P95Milliseconds=p95*1000;}
        }
        private readonly float[] frames;
        private int index,count;
        public long TotalFrames { get; private set; }
        public double EligibleSeconds { get; private set; }
        public CityFrameWindow(int capacity=1800)
        {
            if(capacity<2||capacity>36000)throw new ArgumentOutOfRangeException(nameof(capacity));
            frames=new float[capacity];
        }
        public bool Add(float seconds)
        {
            if(float.IsNaN(seconds)||float.IsInfinity(seconds)||seconds<=0)return false;
            frames[index]=seconds;index=(index+1)%frames.Length;count=Math.Min(count+1,frames.Length);
            TotalFrames++;EligibleSeconds+=seconds;return true;
        }
        public Summary Read()
        {
            if(count==0)return default;
            var sorted=new float[count];Array.Copy(frames,sorted,count);Array.Sort(sorted);
            double total=0;foreach(float seconds in sorted)total+=seconds;
            double median=count%2==1?sorted[count/2]:((double)sorted[count/2-1]+sorted[count/2])*.5;
            return new Summary(count,total,median,sorted[Math.Max(0,(int)Math.Ceiling(count*.95)-1)]);
        }
    }
}
