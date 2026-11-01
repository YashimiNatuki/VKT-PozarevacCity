using System;
using System.Reflection;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

namespace Pozarevac.CityGame.Tests.Editor
{
    public sealed class CityHumanFaceTests
    {
        private GameObject owner, visual;
        private object face;
        private Type type;
        private static readonly string[] Channels = { "eyeBlinkLeft", "eyeBlinkRight", "jawOpen", "mouthSmileLeft", "mouthSmileRight", "browDownLeft", "browDownRight" };

        [SetUp] public void SetUp()
        {
            owner = new GameObject("HumanFaceFixture");
            Assert.That(CityCharacterModel.TryPrepareHuman(Resources.Load<GameObject>("Models/RodjaHuman/RodjaHuman"), owner.transform, 0, out visual), Is.True);
        }
        [TearDown] public void TearDown()
        {
            if (face != null) type.GetMethod("Reset").Invoke(face, null);
            UnityEngine.Object.DestroyImmediate(owner);
        }
        [Test] public void ReviewedModelCarriesRealNonzeroEyelidAndMouthTargets()
        {
            foreach (string channel in Channels)
            {
                bool found = false;float maxRaw=0,maxWorld=0;
                foreach (var skin in visual.GetComponentsInChildren<SkinnedMeshRenderer>(true))
                {
                    Mesh mesh = skin.sharedMesh;
                    for (int i = 0; i < mesh.blendShapeCount; i++)
                    {
                        if (!mesh.GetBlendShapeName(i).EndsWith(channel, StringComparison.Ordinal)) continue;
                        var delta = new Vector3[mesh.vertexCount];
                        mesh.GetBlendShapeFrameVertices(i, mesh.GetBlendShapeFrameCount(i) - 1, delta, null, null);
                        foreach (var vertex in delta)
                        {
                            maxRaw=Mathf.Max(maxRaw,vertex.magnitude);
                            maxWorld=Mathf.Max(maxWorld,skin.transform.TransformVector(vertex).magnitude);
                        }
                        found|=maxWorld>.0001f;
                    }
                }
                Assert.That(found, Is.True, "Reviewed facial target must move at least 0.1mm: " + channel+" raw="+maxRaw+" worldMeters="+maxWorld);
            }
        }
        [Test] public void HeadGazeIsBoundedAndCannotAccumulateAcrossFrames()
        {
            CreateFace();
            Transform head = FindBone("head");
            Vector3 forwardAxis = head.InverseTransformDirection(visual.transform.forward);
            for (int i = 0; i < 150; i++) Step(.02f, new Vector3(10, 8, 1), CityFaceMood.Calm, 0);
            Quaternion settled = head.rotation;
            Vector3 direction = visual.transform.InverseTransformDirection(head.TransformDirection(forwardAxis));
            Assert.That(Mathf.Atan2(direction.x, direction.z) * Mathf.Rad2Deg, Is.InRange(10f, 36f));
            Assert.That(Mathf.Abs(Mathf.Asin(Mathf.Clamp(direction.y, -1, 1)) * Mathf.Rad2Deg), Is.LessThan(21));
            for (int i = 0; i < 150; i++) Step(.02f, new Vector3(10, 8, 1), CityFaceMood.Calm, 0);
            Assert.That(Quaternion.Angle(settled, head.rotation), Is.LessThan(.1f));
        }
        [Test] public void BlinkClosesBothRealEyelidChannelsAndReopensThem()
        {
            CreateFace(); float peak = 0; bool reopened = false;
            for (int i = 0; i < 300; i++)
            {
                Step(.02f, Vector3.forward, CityFaceMood.Calm, 0);
                float left = Weight("eyeBlinkLeft"), right = Weight("eyeBlinkRight");
                Assert.That(left, Is.EqualTo(right).Within(.01f));
                peak = Mathf.Max(peak, left);
                if (peak > 85 && left < 1) reopened = true;
            }
            Assert.That(peak, Is.GreaterThan(85)); Assert.That(reopened, Is.True);
        }
        [Test] public void DistinctMoodsDriveActualBrowJawAndSmileWeights()
        {
            CreateFace();
            for (int i = 0; i < 50; i++) Step(.02f, Vector3.forward, CityFaceMood.Hurt, 0);
            Assert.That(Weight("browDownLeft"), Is.GreaterThan(15));
            for (int i = 0; i < 50; i++) Step(.02f, Vector3.forward, CityFaceMood.Success, 0);
            Assert.That(Weight("mouthSmileLeft"), Is.GreaterThan(15));
            Assert.That(Weight("browDownLeft"), Is.LessThan(5));
            for (int i = 0; i < 50; i++) Step(.02f, Vector3.forward, CityFaceMood.Effort, 1);
            Assert.That(Weight("jawOpen"), Is.GreaterThan(10));
        }
        [Test] public void InvalidTimeFreezesThePoseAndResetRestoresBonesAndChannels()
        {
            CreateFace(); Transform head = FindBone("head"), neck = FindBone("neck_01");
            Quaternion headRest = head.localRotation, neckRest = neck.localRotation;
            Step(.1f, Vector3.right, CityFaceMood.Success, 1);
            Quaternion posed = head.localRotation; float smile = Weight("mouthSmileLeft");
            foreach (float dt in new[] { 0f, -1, float.NaN, float.PositiveInfinity }) Step(dt, Vector3.left, CityFaceMood.Hurt, 0);
            Assert.That(Quaternion.Angle(posed, head.localRotation), Is.LessThan(.01f));
            Assert.That(Weight("mouthSmileLeft"), Is.EqualTo(smile));
            type.GetMethod("Reset").Invoke(face, null);
            Assert.That(Quaternion.Angle(headRest, head.localRotation), Is.LessThan(.01f));
            Assert.That(Quaternion.Angle(neckRest, neck.localRotation), Is.LessThan(.01f));
            foreach (string channel in Channels) Assert.That(Weight(channel), Is.Zero);
        }
        [Test] public void GazeUsesAuthoredEyeTargetsOnBothSidesWithoutOpposingWeights()
        {
            CreateFace();
            for (int i = 0; i < 80; i++) Step(.02f, new Vector3(1, 1, 1), CityFaceMood.Calm, 0);
            // Reviewed Unity FBX geometry reflects X relative to the source GLB.
            Assert.That(Weight("eyeLookInLeft"), Is.GreaterThan(5));
            Assert.That(Weight("eyeLookOutRight"), Is.GreaterThan(5));
            Assert.That(Weight("eyeLookOutLeft"), Is.Zero);
            Assert.That(Weight("eyeLookInRight"), Is.Zero);
            Assert.That(Weight("eyeLookUpLeft"), Is.GreaterThan(5));
            Assert.That(Weight("eyeLookDownLeft"), Is.Zero);
            type.GetMethod("Reset").Invoke(face,null);
            foreach(string key in new[]{"eyeLookInLeft","eyeLookOutRight","eyeLookUpLeft"})Assert.That(Weight(key),Is.Zero);
        }
        [Test] public void IncompleteEyeTargetsCannotMoveOnlyOneEye()
        {
            var originals=new Dictionary<SkinnedMeshRenderer,Mesh>();
            var copies=new List<Mesh>();
            try
            {
                foreach(var skin in visual.GetComponentsInChildren<SkinnedMeshRenderer>(true))
                {
                    Mesh source=skin.sharedMesh,copy=UnityEngine.Object.Instantiate(source);originals[skin]=source;copies.Add(copy);copy.ClearBlendShapes();
                    for(int i=0;i<source.blendShapeCount;i++)
                    {
                        string name=source.GetBlendShapeName(i);
                        if(Array.IndexOf(Channels,name)<0&&name!="eyeLookOutLeft")continue;
                        var delta=new Vector3[source.vertexCount];source.GetBlendShapeFrameVertices(i,source.GetBlendShapeFrameCount(i)-1,delta,null,null);
                        copy.AddBlendShapeFrame(name,100,delta,null,null);
                    }
                    skin.sharedMesh=copy;
                }
                CreateFace();
                Assert.That((bool)type.GetProperty("HasEyeGaze").GetValue(face),Is.False);
                for(int i=0;i<60;i++)Step(.02f,Vector3.right,CityFaceMood.Calm,0);
                Assert.That(Weight("eyeLookOutLeft"),Is.Zero);
            }
            finally
            {
                if(face!=null){type.GetMethod("Reset").Invoke(face,null);face=null;}
                foreach(var entry in originals)entry.Key.sharedMesh=entry.Value;
                foreach(Mesh copy in copies)UnityEngine.Object.DestroyImmediate(copy);
            }
        }
        [Test] public void HurtKeepsProtectiveEyelidClosureBetweenNaturalBlinks()
        {
            CreateFace();
            for(int i=0;i<50;i++)Step(.02f,Vector3.forward,CityFaceMood.Hurt,0);
            Assert.That(Weight("eyeBlinkLeft"),Is.GreaterThanOrEqualTo(44.9f));
            Assert.That(Weight("eyeBlinkRight"),Is.GreaterThanOrEqualTo(44.9f));
        }
        [Test] public void AmbiguousFacialChannelsAreRejectedWithoutChangingTheSourceMesh()
        {
            var skin=Array.Find(visual.GetComponentsInChildren<SkinnedMeshRenderer>(true),s=>s.sharedMesh.GetBlendShapeIndex("eyeBlinkLeft")>=0);
            Assert.That(skin,Is.Not.Null);
            Mesh original=skin.sharedMesh,owned=UnityEngine.Object.Instantiate(original);
            try
            {
                owned.AddBlendShapeFrame("Duplicate.eyeBlinkLeft",100,new Vector3[owned.vertexCount],null,null);skin.sharedMesh=owned;
                Type adapter=typeof(CityHumanPose).Assembly.GetType("Pozarevac.CityGame.CityHumanFace");
                object[] args={visual,visual.transform,null};
                Assert.That((bool)adapter.GetMethod("TryCreate").Invoke(null,args),Is.False);
            }
            finally{skin.sharedMesh=original;UnityEngine.Object.DestroyImmediate(owned);}
        }
        private void CreateFace()
        {
            type = typeof(CityHumanPose).Assembly.GetType("Pozarevac.CityGame.CityHumanFace");
            Assert.That(type, Is.Not.Null, "The reviewed human needs a facial presentation adapter.");
            object[] args = { visual, visual.transform, null };
            Assert.That((bool)type.GetMethod("TryCreate").Invoke(null, args), Is.True);
            face = args[2];
        }
        private void Step(float dt, Vector3 direction, CityFaceMood mood, float effort) => type.GetMethod("Step").Invoke(face, new object[] { dt, direction, mood, effort });
        private Transform FindBone(string name) => Array.Find(visual.GetComponentsInChildren<Transform>(true), bone => bone.name == name);
        private float Weight(string channel)
        {
            foreach (var skin in visual.GetComponentsInChildren<SkinnedMeshRenderer>(true))
                for (int i = 0; i < skin.sharedMesh.blendShapeCount; i++)
                    if (skin.sharedMesh.GetBlendShapeName(i).EndsWith(channel, StringComparison.Ordinal)) return skin.GetBlendShapeWeight(i);
            Assert.Fail("Facial channel missing: " + channel); return 0;
        }
    }
}
