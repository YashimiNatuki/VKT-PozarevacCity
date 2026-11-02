using System;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;

namespace Pozarevac.CityGame.Tests.Editor
{
    public sealed class CityTwoBonePoseTests
    {
        [TestCase(false)]
        [TestCase(true)]
        public void ReachesARealTargetWithoutStretchingEitherSegment(bool degenerateHint)
        {
            var root = new GameObject("TwoBoneFixture");
            try
            {
                Transform upper = root.transform, lower = new GameObject("Elbow").transform, tip = new GameObject("Wrist").transform;
                lower.SetParent(upper, false); tip.SetParent(lower, false);
                lower.localPosition = tip.localPosition = Vector3.down;
                Vector3 target = new Vector3(.4f, -1.3f, .2f);
                Solve(upper, lower, tip, target, degenerateHint ? target.normalized : Vector3.back);
                Assert.That(Vector3.Distance(tip.position, target), Is.LessThan(.001f));
                Assert.That(Vector3.Distance(upper.position, lower.position), Is.EqualTo(1).Within(.0001f));
                Assert.That(Vector3.Distance(lower.position, tip.position), Is.EqualTo(1).Within(.0001f));
            }
            finally { UnityEngine.Object.DestroyImmediate(root); }
        }

        [TestCase(false)]
        [TestCase(true)]
        public void NonFiniteOrOverflowingTargetLeavesTheChainUntouched(bool overflowing)
        {
            var root = new GameObject("TwoBoneInvalid");
            try
            {
                Transform lower = new GameObject("Elbow").transform, tip = new GameObject("Wrist").transform;
                lower.SetParent(root.transform, false); tip.SetParent(lower, false);
                lower.localPosition = tip.localPosition = Vector3.down;
                Solve(root.transform, lower, tip, new Vector3(overflowing ? 3e38f : float.NaN, 0, 0), Vector3.back);
                Assert.That(root.transform.localRotation, Is.EqualTo(Quaternion.identity));
                Assert.That(lower.localRotation, Is.EqualTo(Quaternion.identity));
                Assert.That(tip.position, Is.EqualTo(Vector3.down * 2));
            }
            finally { UnityEngine.Object.DestroyImmediate(root); }
        }

        private static void Solve(Transform upper, Transform lower, Transform tip, Vector3 target, Vector3 hint)
        {
            Type type = typeof(CityBenchVisual).Assembly.GetType("Pozarevac.CityGame.CityTwoBonePose");
            Assert.That(type, Is.Not.Null, "The bench and phone need one real two-bone solver.");
            type.GetMethod("Solve").Invoke(null, new object[] { upper, lower, tip, target, hint });
        }
    }
}
