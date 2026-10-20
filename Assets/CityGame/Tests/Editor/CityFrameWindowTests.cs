using NUnit.Framework;

namespace Pozarevac.CityGame.Tests.Editor
{
    public sealed class CityFrameWindowTests
    {
        [Test] public void EmptyAndInvalidSamplesNeverProduceFalsePerformanceEvidence()
        {
            var samples=new CityFrameWindow(4);
            foreach(float value in new[]{0f,-1,float.NaN,float.PositiveInfinity})Assert.IsFalse(samples.Add(value));
            Assert.That(samples.TotalFrames,Is.Zero);Assert.That(samples.EligibleSeconds,Is.Zero);
            Assert.That(samples.Read().Frames,Is.Zero);Assert.That(samples.Read().Fps,Is.Zero);
        }
        [Test] public void RolloverKeepsLifetimeSeparateFromLatestWindowAndReadsDoNotMutateIt()
        {
            var samples=new CityFrameWindow(4);
            foreach(float value in new[]{.1f,.2f,.3f,.4f,.5f})samples.Add(value);
            var window=samples.Read();var repeated=samples.Read();
            Assert.That(samples.TotalFrames,Is.EqualTo(5));Assert.That(samples.EligibleSeconds,Is.EqualTo(1.5).Within(.00001));
            Assert.That(window.Frames,Is.EqualTo(4));Assert.That(window.Seconds,Is.EqualTo(1.4).Within(.00001));
            Assert.That(window.MedianMilliseconds,Is.EqualTo(350).Within(.001));Assert.That(window.P95Milliseconds,Is.EqualTo(500).Within(.001));
            Assert.That(repeated.Fps,Is.EqualTo(window.Fps));
            samples.Add(.6f);Assert.That(samples.Read().Seconds,Is.EqualTo(1.8).Within(.00001));
        }
        [Test] public void PercentileIncludesAnActualSlowFrameRatherThanAnAverage()
        {
            var samples=new CityFrameWindow(20);
            for(int i=0;i<18;i++)samples.Add(.01f);samples.Add(.1f);samples.Add(.2f);
            Assert.That(samples.Read().MedianMilliseconds,Is.EqualTo(10).Within(.001));
            Assert.That(samples.Read().P95Milliseconds,Is.EqualTo(100).Within(.001));
        }
    }
}
