using System;
using NUnit.Framework;
using UnityEngine;

namespace Pozarevac.CityGame.Tests.Editor
{
    public sealed class CityHumanHandsTests
    {
        private GameObject root, model;
        private CityHumanPose pose;
        private Transform[] all;
        private static readonly string[] Digits = { "index", "middle", "ring", "pinky", "thumb" };
        [SetUp] public void SetUp()
        {
            root = new GameObject("HumanHandsFixture");
            model = UnityEngine.Object.Instantiate(Resources.Load<GameObject>("Models/RodjaHuman/RodjaHuman"), root.transform);
            Assert.That(CityCharacterModel.Normalize(root.transform, model, null), Is.True);
            all = model.GetComponentsInChildren<Transform>(true);
            Assert.That(CityHumanPose.TryCreate(model, root.transform, out pose), Is.True);
        }
        [TearDown] public void TearDown() { if (root != null) UnityEngine.Object.DestroyImmediate(root); }
        [TestCase(0)]
        [TestCase(65)]
        public void GuardClosesAllFiveDigitsOnBothHandsAndKeepsWristsStraight(float yaw)
        {
            root.transform.rotation = Quaternion.Euler(0, yaw, 0);
            Quaternion[] rest = Array.ConvertAll(all, b => b.localRotation);
            Vector3 leftAlong = Bone("hand_l").InverseTransformPoint(Bone("middle_01_l").position).normalized;
            Vector3 rightAlong = Bone("hand_r").InverseTransformPoint(Bone("middle_01_r").position).normalized;
            for (int i = 0; i < 90; i++) pose.Step(.02f, 0, true);
            foreach (string side in new[] { "l", "r" })
            {
                Transform wrist = Bone("hand_" + side);
                Vector3 along = wrist.TransformDirection(side == "l" ? leftAlong : rightAlong);
                Assert.That(Vector3.Angle(along, wrist.position - Bone("lowerarm_" + side).position), Is.LessThan(3), "Bent wrist " + side);
                foreach (string digit in Digits)
                    for (int joint = 1; joint <= 3; joint++)
                    {
                        Transform bone = Bone(digit + "_0" + joint + "_" + side);
                        float angle = Quaternion.Angle(rest[Array.IndexOf(all, bone)], bone.localRotation);
                        Assert.That(angle, Is.GreaterThan(digit == "thumb" ? 5 : 15), "Open finger: " + bone.name);
                        Assert.That(angle, Is.LessThan(100), "Overextended joint: " + bone.name);
                    }
                foreach (string digit in new[] { "index", "middle", "ring", "pinky" })
                    Assert.That(Vector3.Dot(Bone(digit + "_03_" + side).up, along), Is.LessThan(0), "Fingertip must fold back into the fist: " + digit + side);
            }
        }
        [Test] public void LeavingGuardSettlesBothHandsAndResetRestoresEverySourceBone()
        {
            Quaternion[] original = Array.ConvertAll(all, b => b.localRotation);
            for (int i = 0; i < 90; i++) pose.Step(.02f, 0, true);
            Quaternion closed = Bone("index_02_l").localRotation;
            for (int i = 0; i < 120; i++) pose.Step(.02f, 0, false);
            Quaternion relaxed = Bone("index_02_l").localRotation;
            Assert.That(Quaternion.Angle(closed, relaxed), Is.GreaterThan(30));
            for (int i = 0; i < 600; i++) pose.Step(.02f, 0, false);
            Assert.That(Quaternion.Angle(relaxed, Bone("index_02_l").localRotation), Is.LessThan(.02f), "Finger pose accumulated.");
            pose.Reset();
            for (int i = 0; i < all.Length; i++)
                Assert.That(Quaternion.Angle(original[i], all[i].localRotation), Is.LessThan(.02f), all[i].name);
        }
        [Test] public void GuardPalmsFaceTheBodyInsteadOfPresentingOpenPalmsToTheOpponent()
        {
            Vector3[] normals=new Vector3[2];int i=0;
            foreach(string side in new[]{"l","r"})
            {
                Transform a=Bone("index_01_"+side),b=Bone("index_02_"+side),c=Bone("index_03_"+side),wrist=Bone("hand_"+side);
                Vector3 flex=Vector3.Cross((b.position-a.position).normalized,(c.position-b.position).normalized).normalized;
                Vector3 along=(Bone("middle_01_"+side).position-wrist.position).normalized;
                normals[i++]=wrist.InverseTransformDirection(Vector3.Cross(flex,along).normalized);
            }
            for(i=0;i<60;i++)pose.Step(.02f,0,true);
            i=0;foreach(string side in new[]{"l","r"})
                Assert.That(Vector3.Dot(Bone("hand_"+side).TransformDirection(normals[i++]),root.transform.forward),Is.LessThan(-.5f),"Guard palm points away from body: "+side);
        }
        [Test] public void ClosedFingertipsStayInsideThePalmEnvelopeInsteadOfFloatingAsAClaw()
        {
            Vector3[] normals=new Vector3[2];int i=0;
            foreach(string side in new[]{"l","r"})
            {
                Transform a=Bone("index_01_"+side),b=Bone("index_02_"+side),c=Bone("index_03_"+side),wrist=Bone("hand_"+side);
                Vector3 flex=Vector3.Cross((b.position-a.position).normalized,(c.position-b.position).normalized).normalized;
                normals[i++]=wrist.InverseTransformDirection(Vector3.Cross(flex,(Bone("middle_01_"+side).position-wrist.position).normalized));
            }
            for(i=0;i<60;i++)pose.Step(.02f,0,true);
            i=0;foreach(string side in new[]{"l","r"})
            {
                Vector3 normal=Bone("hand_"+side).TransformDirection(normals[i++]);
                foreach(string digit in new[]{"index","middle","ring","pinky"})
                {
                    Transform a=Bone(digit+"_01_"+side),b=Bone(digit+"_02_"+side),c=Bone(digit+"_03_"+side);
                    Vector3 tip=c.position+c.up*Vector3.Distance(b.position,c.position)*.65f;
                    float gap=Vector3.Dot(tip-a.position,normal);
                    Assert.That(gap,Is.InRange(-.005f,.028f),digit+side+" fingertip misses the bounded palm envelope.");
                }
            }
        }
        [Test] public void InvalidTimeDoesNotChangeAnyFingerOrWrist()
        {
            for (int i = 0; i < 90; i++) pose.Step(.02f, 0, true);
            Quaternion[] frozen = Array.ConvertAll(all, b => b.localRotation);
            foreach (float seconds in new[] { 0, -1, float.NaN, float.PositiveInfinity }) pose.Step(seconds, 5, false);
            for (int i = 0; i < all.Length; i++)
                Assert.That(Quaternion.Angle(frozen[i], all[i].localRotation), Is.LessThan(.02f), all[i].name);
        }
        [Test] public void CombatPoseIsBoundedAndTeleportReleasesTheHands()
        {
            pose.SetCombatPunch(float.PositiveInfinity);pose.StepObserved(Vector3.zero,.02f,false);
            Assert.That(pose.GuardWeight,Is.Zero);
            for(int i=0;i<60;i++){pose.SetCombatPunch(2);pose.StepObserved(Vector3.zero,.02f,false);}
            Assert.That(pose.GuardWeight,Is.EqualTo(1).Within(.001f));
            pose.StepObserved(Vector3.right*40,.02f,false);
            Assert.That(pose.GuardWeight,Is.Zero,"Teleport retained a stale combat pose.");
        }
        [Test] public void IncompleteFingerRigDisablesHandAdaptationWithoutBreakingLocomotion()
        {
            UnityEngine.Object.DestroyImmediate(Bone("pinky_03_r").gameObject);
            Assert.That(CityHumanPose.TryCreate(model, root.transform, out pose), Is.True);
            var ready = typeof(CityHumanPose).GetProperty("HandsReady");
            Assert.That(ready, Is.Not.Null, "The optional full-hand adapter needs explicit readiness.");
            Assert.That((bool)ready.GetValue(pose), Is.False);
            Quaternion finger = Bone("index_02_l").localRotation;
            for (int i = 0; i < 60; i++) pose.Step(.02f, 2, false);
            Assert.That(pose.ObservedSpeed, Is.GreaterThan(1));
            Assert.That(Quaternion.Angle(finger, Bone("index_02_l").localRotation), Is.LessThan(.02f), "Partial hand was mutated.");
        }
        private Transform Bone(string name) => Array.Find(all, b => b != null && b.name == name);
    }
}
