using System;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;

namespace Pozarevac.CityGame.Tests.Editor
{
    public sealed class CityHumanMotionTests
    {
        private GameObject root;
        private object pose;
        private Type poseType;
        private GameObject model;

        [SetUp]
        public void SetUp()
        {
            poseType = typeof(CityWardrobePanel).Assembly.GetType("Pozarevac.CityGame.CityHumanPose");
            Assert.That(poseType, Is.Not.Null, "The reviewed human needs a working pose adapter before motion preview ships.");
            root = new GameObject("HumanMotionFixture");
            root.transform.position = new Vector3(10000, -1000, 10000);
            model = UnityEngine.Object.Instantiate(Resources.Load<GameObject>("Models/RodjaHuman/RodjaHuman"), root.transform);
            Assert.That(CityCharacterModel.Normalize(root.transform, model, null), Is.True);
            object[] args = { model, root.transform, null };
            Assert.That((bool)poseType.GetMethod("TryCreate").Invoke(null, args), Is.True);
            pose = args[2];
        }

        [TearDown] public void TearDown() { if (root != null) UnityEngine.Object.DestroyImmediate(root); }

        [Test]
        public void WalkingBendsBothKneesAndSwingsOppositeArmsWithoutMovingTheStage()
        {
            Vector3 position = root.transform.position;
            var leftKnee = Bone("calf_l"); var rightKnee = Bone("calf_r");
            Quaternion left = leftKnee.localRotation, right = rightKnee.localRotation;
            float maxLeft = 0, maxRight = 0; bool opposite = false;
            for (int i = 0; i < 180; i++)
            {
                Step(1f / 60, 2, false);
                maxLeft = Mathf.Max(maxLeft, Quaternion.Angle(left, leftKnee.localRotation));
                maxRight = Mathf.Max(maxRight, Quaternion.Angle(right, rightKnee.localRotation));
                float leg = Vector3.Dot((Bone("calf_l").position - Bone("thigh_l").position).normalized, root.transform.forward);
                float arm = Vector3.Dot((Bone("lowerarm_l").position - Bone("upperarm_l").position).normalized, root.transform.forward);
                opposite |= Mathf.Abs(leg) > .15f && leg * arm < -.015f;
            }
            Assert.That(maxLeft, Is.GreaterThan(15)); Assert.That(maxRight, Is.GreaterThan(15));
            Assert.That(opposite, Is.True, "Arm and same-side leg must counter-swing.");
            Assert.That(root.transform.position, Is.EqualTo(position));
        }

        [Test]
        public void FrozenOrInvalidTimeCannotChangeTheDisplayedPose()
        {
            Step(.2f, 5.5f, false);
            Quaternion before = Bone("calf_l").localRotation;
            Step(0, 2, false); Step(float.NaN, 2, false); Step(float.PositiveInfinity, 2, false);
            Assert.That(Quaternion.Angle(before, Bone("calf_l").localRotation), Is.LessThan(.01f));
        }

        [Test]
        public void ResetRestoresTheImportedPoseAfterWalkingAndBoxing()
        {
            Transform[] bones = model.GetComponentsInChildren<Transform>(true);
            Quaternion[] rest = Array.ConvertAll(bones, b => b.localRotation);
            for (int i = 0; i < 300; i++) Step(1f / 60, i < 150 ? 5.5f : 0, i >= 150);
            poseType.GetMethod("Reset").Invoke(pose, null);
            for (int i = 0; i < bones.Length; i++) Assert.That(Quaternion.Angle(rest[i], bones[i].localRotation), Is.LessThan(.02f), bones[i].name);
        }

        [Test]
        public void GuardKeepsWristsNearTheFaceRatherThanAboveTheHead()
        {
            Step(.02f, 0, true);
            foreach (string side in new[] { "l", "r" })
            {
                float wrist = Bone("hand_" + side).position.y;
                Assert.That(wrist, Is.GreaterThan(Bone("pelvis").position.y + .15f));
                Assert.That(wrist, Is.LessThanOrEqualTo(Bone("head").position.y + .04f));
                Assert.That(Bone("index_03_" + side).position.y, Is.LessThanOrEqualTo(Bone("head").position.y + .10f));
            }
        }

        [Test]
        public void IncompleteRigIsRejectedBeforeAnyBoneIsChanged()
        {
            var broken = new GameObject("MissingRig");
            try
            {
                object[] args = { broken, root.transform, null };
                Assert.That((bool)poseType.GetMethod("TryCreate").Invoke(null, args), Is.False);
                Assert.That(args[2], Is.Null);
                Assert.That(broken.transform.childCount, Is.Zero);
            }
            finally { UnityEngine.Object.DestroyImmediate(broken); }
        }

        private Transform Bone(string name) => Array.Find(model.GetComponentsInChildren<Transform>(true), b => b.name == name);
        private void Step(float dt, float speed, bool boxing) => poseType.GetMethod("Step").Invoke(pose, new object[] { dt, speed, boxing });
    }
}
