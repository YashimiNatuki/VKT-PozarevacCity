using System;
using NUnit.Framework;
using UnityEngine;

namespace Pozarevac.CityGame.Tests
{
    public sealed class CityBenchPressTests
    {
        [Test] public void CompleteCycleCountsOnceAndRejectsSpam()
        {
            var bench = new CityBenchPress();
            Assert.That(bench.Press(), Is.True);
            Assert.That(bench.Press(), Is.False);
            Assert.That(bench.Tick(.45), Is.False);
            Assert.That(bench.Extension, Is.EqualTo(.5f).Within(.001));
            Assert.That(bench.Press(), Is.False);
            bench.Tick(.45); Assert.That(bench.Phase, Is.EqualTo(CityBenchPhase.PressWindow));
            Assert.That(bench.Press(), Is.True);
            Assert.That(bench.Tick(.8), Is.True);
            Assert.That(bench.Completed, Is.EqualTo(1));
            Assert.That(bench.Tick(.25), Is.False);
            Assert.That(bench.Phase, Is.EqualTo(CityBenchPhase.Ready));
            Assert.That(bench.Tick(10), Is.False);
            Assert.That(bench.Completed, Is.EqualTo(1));
        }
        [Test] public void MissedWindowReturnsBarWithoutRewardAndPauseIsFrozen()
        {
            var bench = new CityBenchPress(); bench.Press(); bench.Tick(.9);
            bench.Tick(0); Assert.That(bench.Phase, Is.EqualTo(CityBenchPhase.PressWindow));
            bench.Tick(.75); Assert.That(bench.Misses, Is.EqualTo(1));
            Assert.That(bench.Tick(.8), Is.False);
            Assert.That(bench.Completed, Is.Zero);
            bench.Tick(.25); Assert.That(bench.Press(), Is.True);
            Assert.Throws<ArgumentOutOfRangeException>(() => bench.Tick(double.NaN));
            Assert.Throws<ArgumentOutOfRangeException>(() => bench.Tick(-1));
        }
        [Test] public void FrameStallCannotSkipThePressWindow()
        {
            var bench = new CityBenchPress(); bench.Press(); bench.Tick(10);
            Assert.That(bench.Phase, Is.EqualTo(CityBenchPhase.PressWindow));
            Assert.That(bench.Completed, Is.Zero);
            Assert.That(bench.Press(), Is.True);
        }
        [Test] public void LicensedPlayerRigReachesBothEndsOfTheBenchStroke()
        {
            var root = new GameObject("BenchTest");
            var mesh = CityVisualMeshes.RoundedBox();
            var material = new Material(Resources.Load<Material>("CityMissionMarker"));
            try
            {
                var visual = root.AddComponent<CityBenchVisual>(); visual.Initialize(mesh, material, material);
                Assert.That(visual.RigReady, Is.True, "The player's arm skeleton must be available.");
                Assert.That(visual.FeetReady, Is.True, "Both leg chains must be available.");
                Assert.That(visual.FootError, Is.LessThan(.03f), "Feet must reach the authored ground contacts.");
                visual.SetStrength(5);
                for (int i = 0; i <= 10; i++)
                {
                    visual.Pose(true, i / 10f);
                    Assert.That(visual.HandError, Is.LessThan(.03f), "Grip lost at extension " + i + ": " + visual.HandError);
                }
                visual.Pose(false, 1);
            }
            finally { UnityEngine.Object.DestroyImmediate(root); UnityEngine.Object.DestroyImmediate(mesh); UnityEngine.Object.DestroyImmediate(material); }
        }
    }
}
