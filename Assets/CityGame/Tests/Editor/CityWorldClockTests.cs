using NUnit.Framework;
using System;
namespace Pozarevac.CityGame.Tests.Editor
{
    public sealed class CityWorldClockTests
    {
        [Test] public void SeparateDayAndNightDurationsReachExactPhaseBoundaries()
        {
            CityWorldClock clock = new CityWorldClock(1080f, 720f, 6f);
            clock.Tick(540f); Assert.That(clock.Hour, Is.EqualTo(12f).Within(0.001f));
            clock.Tick(540f); Assert.That(clock.Hour, Is.EqualTo(18f).Within(0.001f)); Assert.That(clock.IsDay, Is.False);
            clock.Tick(360f); Assert.That(clock.Hour, Is.EqualTo(0f).Within(0.001f));
            clock.Tick(360f); Assert.That(clock.Hour, Is.EqualTo(6f).Within(0.001f)); Assert.That(clock.IsDay, Is.True);
        }
        [Test] public void PauseAndLargeStepsPreserveDeterministicClock()
        {
            CityWorldClock clock = new CityWorldClock(100f, 50f, 20f);
            clock.Tick(0f); clock.Tick(-1f); clock.Tick(float.NaN);
            Assert.That(clock.Hour, Is.EqualTo(20f).Within(0.001f));
            clock.Tick(15000f); Assert.That(clock.Hour, Is.EqualTo(20f).Within(0.001f));
        }
        [Test] public void InvalidCycleDurationIsRejected()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => new CityWorldClock(0f));
            Assert.Throws<ArgumentOutOfRangeException>(() => new CityWorldClock(10f, float.NaN));
        }
    }
}
