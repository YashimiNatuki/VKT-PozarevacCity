using System;
using NUnit.Framework;
using UnityEngine;

namespace Pozarevac.CityGame.Tests
{
    public sealed class CityHandContactTests
    {
        [TestCase(0)]
        [TestCase(5)]
        public void BenchKeepsTheWristOutsideTheShaftThroughoutTheStroke(int strength)
        {
            var root = new GameObject("GripTest");
            var mesh = CityVisualMeshes.RoundedBox();
            var material = new Material(Resources.Load<Material>("CityMissionMarker"));
            try
            {
                var visual = root.AddComponent<CityBenchVisual>(); visual.Initialize(mesh, material, material);
                var bones = root.GetComponentsInChildren<Transform>(true);
                var bar = Array.Find(bones, b => b.name == "OlympicBar");
                Assert.IsTrue(visual.HandsReady);
                root.transform.rotation=Quaternion.Euler(0,65,0);
                visual.SetStrength(strength);
                for(int i=0;i<=100;i++)
                {
                    visual.Pose(true,Mathf.PingPong(i/10f,1));
                    Assert.That(visual.HandError,Is.LessThan(.005f));
                    Assert.That(visual.HandOrientationError,Is.LessThan(.1f));
                    Assert.That(visual.WristClearance,Is.GreaterThan(.02f));
                    foreach(string name in new[]{"LeftHand","RightHand"})
                    {
                        var hand=Array.Find(bones,b=>b.name==name);
                        var wrist=bar.InverseTransformPoint(hand.position);wrist.x=0;
                        Assert.That(wrist.magnitude,Is.GreaterThan(.025f),"The shaft intersects the wrist joint: "+name);
                        Transform knuckle=hand.Find(name+"Index1"),tip=hand.Find(name+"Index1/"+name+"Index2/"+name+"Index3/"+name+"Index3_end");
                        Assert.That(Vector3.Dot(tip.position-knuckle.position,root.transform.up),Is.LessThan(.01f),"The fingers stay open: "+name);
                        Vector3 tipOffset=bar.InverseTransformPoint(tip.position);tipOffset.x=0;
                        Assert.That(tipOffset.magnitude,Is.LessThan(.07f),"Fingertips miss the shaft: "+name);
                    }
                }
                visual.Pose(true,float.NaN);
                Assert.IsTrue(float.IsFinite(bar.position.y));
                Assert.That(visual.HandError,Is.LessThan(.005f));
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(root);UnityEngine.Object.DestroyImmediate(mesh);UnityEngine.Object.DestroyImmediate(material);
            }
        }
    }
}
