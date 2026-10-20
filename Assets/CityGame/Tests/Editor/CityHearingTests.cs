using NUnit.Framework;
using UnityEngine;

namespace Pozarevac.CityGame.Tests
{
    public sealed class CityHearingTests
    {
        [Test]
        public void WallsAndRangePreventAcquisitionWithoutOverwritingLastSound()
        {
            var memory = new CityHearingMemory();
            Assert.IsFalse(memory.Hear(Vector3.right * 8, Vector3.zero, false, 24));
            Assert.IsFalse(memory.Active);
            Assert.IsTrue(memory.Hear(Vector3.right * 8, Vector3.zero, true, 24));
            Assert.IsFalse(memory.Hear(Vector3.right * 25, Vector3.zero, true, 24));
            Assert.AreEqual(Vector3.right * 8, memory.Position);
        }

        [Test]
        public void MemoryExpiresAndDoesNotTrackHiddenEmitter()
        {
            var memory = new CityHearingMemory();
            memory.Hear(Vector3.forward * 4, Vector3.zero, true, 24);
            memory.Tick(0); Assert.AreEqual(CityHearingMemory.Duration, memory.Remaining);
            memory.Tick(5); Assert.IsTrue(memory.Active);
            Assert.AreEqual(Vector3.forward * 4, memory.Position);
            memory.Tick(2); Assert.IsFalse(memory.Active);
            memory.Hear(Vector3.zero, Vector3.zero, true, 24);
            memory.Clear(); Assert.IsFalse(memory.Active);
        }

        [Test]
        public void NonFiniteOrNegativeInputsAreRejected()
        {
            var memory = new CityHearingMemory();
            Assert.Throws<System.ArgumentException>(() => memory.Hear(new Vector3(float.NaN, 0, 0), Vector3.zero, true, 24));
            Assert.Throws<System.ArgumentException>(() => memory.Hear(Vector3.zero, Vector3.zero, true, float.PositiveInfinity));
            Assert.Throws<System.ArgumentOutOfRangeException>(() => memory.Tick(-1));
            Assert.Throws<System.ArgumentOutOfRangeException>(() => memory.Tick(float.NaN));
        }

        [Test]
        public void AuthoredClipsHaveFiniteBoundedSamplesAndSilentSeams()
        {
            foreach (var clip in new[] { CityStreetSignals.CreateWhistle(), CityAmbience.CreateHideoutLoop() })
            {
                try
                {
                    Assert.AreEqual(1, clip.channels);
                    var data = new float[clip.samples]; Assert.IsTrue(clip.GetData(data, 0));
                    float peak = 0, energy = 0;
                    foreach (float sample in data)
                    {
                        Assert.IsTrue(float.IsFinite(sample));
                        peak = Mathf.Max(peak, Mathf.Abs(sample)); energy += sample * sample;
                    }
                    Assert.Greater(peak, .05f); Assert.LessOrEqual(peak, .5f);
                    Assert.Greater(energy, 1f); Assert.AreEqual(0f, data[0], .00001f); Assert.AreEqual(0f, data[data.Length - 1], .00001f);
                }
                finally { Object.DestroyImmediate(clip); }
            }
        }
    }
}
