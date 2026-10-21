using System;
using NUnit.Framework;
using UnityEngine;

namespace Pozarevac.CityGame.Tests
{
    public sealed class CityBenchPressTests
    {
        [Test]
        public void BenchSolesKeepTheirStandingOrientationThroughTheWholeStroke()
        {
            var standing=new GameObject("StandingReference");
            var bench=new GameObject("RotatedBench");
            var mesh=CityVisualMeshes.RoundedBox();
            var material=new Material(Resources.Load<Material>("CityMissionMarker"));
            CityCharacterPhysique referenceBody=null;
            try
            {
                var model=UnityEngine.Object.Instantiate(CityCharacterModel.LoadSource(),standing.transform);
                referenceBody=new CityCharacterPhysique(model);
                var idle=Array.Find(Resources.LoadAll<AnimationClip>("Models/KenneyRetro/idle"),c=>!c.name.StartsWith("__preview__"));
                Assert.IsTrue(CityCharacterModel.Normalize(standing.transform,model,idle,referenceBody));
                var reference=model.GetComponentsInChildren<Transform>();
                Quaternion left=Array.Find(reference,b=>b.name=="LeftFoot").rotation;
                Quaternion right=Array.Find(reference,b=>b.name=="RightFoot").rotation;
                float leftHeight=Array.Find(reference,b=>b.name=="LeftFoot").position.y;
                float rightHeight=Array.Find(reference,b=>b.name=="RightFoot").position.y;
                bench.transform.rotation=Quaternion.Euler(0,65,0);
                var visual=bench.AddComponent<CityBenchVisual>();visual.Initialize(mesh,material,material);
                var posed=bench.GetComponentsInChildren<Transform>(true);
                Transform leftFoot=Array.Find(posed,b=>b.name=="LeftFoot"),rightFoot=Array.Find(posed,b=>b.name=="RightFoot");
                var supports=bench.GetComponentsInChildren<BoxCollider>();
                for(int i=0;i<40;i++)
                {
                    visual.Pose(true,Mathf.PingPong(i*.13f,1));
                    Assert.That(Quaternion.Angle(leftFoot.rotation,bench.transform.rotation*left),Is.LessThan(.1f),"Left sole tilted.");
                    Assert.That(Quaternion.Angle(rightFoot.rotation,bench.transform.rotation*right),Is.LessThan(.1f),"Right sole tilted.");
                    Assert.That(bench.transform.InverseTransformPoint(leftFoot.position).y,Is.EqualTo(leftHeight).Within(.03f));
                    Assert.That(bench.transform.InverseTransformPoint(rightFoot.position).y,Is.EqualTo(rightHeight).Within(.03f));
                    Assert.That(visual.FootError,Is.LessThan(.03f));
                    Assert.That(visual.HandError,Is.LessThan(.03f));
                    foreach(var support in supports)
                    {
                        Assert.That(Vector3.Distance(support.ClosestPoint(leftFoot.position),leftFoot.position),Is.GreaterThan(.04f),"Left ankle intersects "+support.name);
                        Assert.That(Vector3.Distance(support.ClosestPoint(rightFoot.position),rightFoot.position),Is.GreaterThan(.04f),"Right ankle intersects "+support.name);
                    }
                }
            }
            finally
            {
                referenceBody?.Dispose();UnityEngine.Object.DestroyImmediate(standing);UnityEngine.Object.DestroyImmediate(bench);
                UnityEngine.Object.DestroyImmediate(mesh);UnityEngine.Object.DestroyImmediate(material);
            }
        }
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
