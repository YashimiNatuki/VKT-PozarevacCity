using System;
using NUnit.Framework;
using UnityEngine;

namespace Pozarevac.CityGame.Tests.Editor
{
    public sealed class CityHumanBenchTests
    {
        [TestCase(0)] [TestCase(65)]
        public void SelectedHumanUsesTheSameBarWithStableHandAndFootContacts(float yaw)
        {
            var root=new GameObject("HumanBenchFixture");
            Mesh mesh=CityVisualMeshes.RoundedBox(.1f,4);
            var material=new Material(Resources.Load<Material>("CityMissionMarker"));
            try
            {
                root.transform.rotation=Quaternion.Euler(0,yaw,0);
                var bench=root.AddComponent<CityBenchVisual>();bench.Initialize(mesh,material,material);
                var select=typeof(CityBenchVisual).GetMethod("SelectHuman");
                Assert.That(select,Is.Not.Null,"Bench must bind the selected real human presentation.");
                Assert.That((bool)select.Invoke(bench,new object[]{true}),Is.True);
                Assert.That(bench.FeetReady,Is.True,"Workout entry checks contact readiness before activating the athlete.");
                GameObject human=(GameObject)typeof(CityBenchVisual).GetProperty("HumanAthlete").GetValue(bench);
                Assert.That(human,Is.Not.Null);
                TestContext.WriteLine("Human bench normalization scale: "+human.transform.localScale.x);
                int colliders=root.GetComponentsInChildren<Collider>(true).Length;
                for(int i=0;i<=80;i++)
                {
                    bench.Pose(true,Mathf.PingPong(i/40f,1));
                    Assert.That(human.activeInHierarchy,Is.True);
                    Assert.That(bench.HandError,Is.LessThan(.006f));
                    Assert.That(bench.FootError,Is.LessThan(.03f));
                    Assert.That(bench.HandOrientationError,Is.LessThan(.1f));
                    Assert.That(bench.WristClearance,Is.GreaterThan(.02f));
                    if(i%20==0)
                    {
                        Transform bar=root.transform.Find("OlympicBar");
                        Transform[] bones=human.GetComponentsInChildren<Transform>(true);
                        foreach(string side in new[]{"l","r"})foreach(string digit in new[]{"index","middle","ring","pinky","thumb"})
                        {
                            Transform middle=Array.Find(bones,b=>b.name==digit+"_02_"+side),distal=Array.Find(bones,b=>b.name==digit+"_03_"+side);
                            Vector3 tip=distal.position+distal.up*Vector3.Distance(middle.position,distal.position)*.75f;
                            float radius=Vector3.ProjectOnPlane(tip-bar.position,root.transform.right).magnitude;
                            Assert.That(radius,Is.InRange(.016f,.035f),"Finger must wrap the real shaft: "+digit+side);
                        }
                        foreach(string side in new[]{"l","r"})
                        {
                            Transform wrist=Array.Find(bones,b=>b.name=="hand_"+side),elbow=Array.Find(bones,b=>b.name=="lowerarm_"+side),knuckle=Array.Find(bones,b=>b.name=="middle_01_"+side);
                            Assert.That(Vector3.Angle(knuckle.position-wrist.position,wrist.position-elbow.position),Is.LessThan(45),"Excessive wrist flex.");
                            Assert.That(Vector3.Dot((knuckle.position-wrist.position).normalized,root.transform.up),Is.GreaterThan(.99f));
                        }
                        var skin=Array.Find(human.GetComponentsInChildren<SkinnedMeshRenderer>(),x=>x.name.IndexOf("shoes02",StringComparison.OrdinalIgnoreCase)>=0);
                        Assert.That(skin,Is.Not.Null);
                        Mesh baked=new Mesh();
                        try
                        {
                            skin.BakeMesh(baked,true);float left=float.PositiveInfinity,right=left;
                            Physics.SyncTransforms();var metal=root.GetComponentsInChildren<BoxCollider>();
                            foreach(Vector3 vertex in baked.vertices)
                            {
                                Vector3 world=skin.transform.TransformPoint(vertex),local=root.transform.InverseTransformPoint(world);
                                if(local.x<0)left=Mathf.Min(left,local.y);else right=Mathf.Min(right,local.y);
                                foreach(var collider in metal)
                                    if(collider.name=="StableFoot"||collider.name=="BenchLeg"||collider.name=="Rack")
                                        Assert.That(Vector3.Distance(world,collider.ClosestPoint(world)),Is.GreaterThan(.002f),"Shoe intersects bench metal: "+collider.name);
                            }
                            Assert.That(Mathf.Abs(left),Is.LessThan(.04f));Assert.That(Mathf.Abs(right),Is.LessThan(.04f));
                        }
                        finally { UnityEngine.Object.DestroyImmediate(baked); }
                    }
                }
                Assert.That(human.GetComponentsInChildren<Collider>(true),Is.Empty);
                Assert.That(root.GetComponentsInChildren<Collider>(true).Length,Is.EqualTo(colliders));
                select.Invoke(bench,new object[]{false});bench.Pose(true,.5f);
                Assert.That(human.activeSelf,Is.False);
                select.Invoke(bench,new object[]{true});bench.Pose(false,1);
                Assert.That(human.activeSelf,Is.False);
                Assert.That(typeof(CityBenchVisual).GetProperty("HumanAthlete").GetValue(bench),Is.SameAs(human));
            }
            finally { UnityEngine.Object.DestroyImmediate(root);UnityEngine.Object.DestroyImmediate(mesh);UnityEngine.Object.DestroyImmediate(material); }
        }
    }
}
