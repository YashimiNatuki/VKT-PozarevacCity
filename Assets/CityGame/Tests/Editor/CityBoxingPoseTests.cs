using System;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Pozarevac.CityGame.Tests
{
    public sealed class CityBoxingPoseTests
    {
        private const BindingFlags PrivateInstance = BindingFlags.Instance | BindingFlags.NonPublic;

        [Test]
        public void ActualSkinnedHandsFormClosedGuardWithStraightWrists()
        {
            using (var rig = new Rig())
            {
                rig.Pose(0);
                for (int side = 0; side < 2; side++)
                {
                    AssertAlignedAndClosed(rig, side);
                    Assert.That(Vector3.Dot(rig.Hands[side].PalmNormal, -rig.Wrapper.up), Is.LessThan(.8f), "Guard must not retain the fully pronated jab pose.");
                    Assert.That(Vector3.Dot(rig.Hands[side].Hand.position - rig.Elbows[side].position, rig.Wrapper.up), Is.GreaterThan(.1f));
                }
            }
        }

        [Test]
        public void LeftJabExtendsForwardWhileTheRightHandKeepsItsGuard()
        {
            using (var rig = new Rig())
            {
                rig.Pose(0);
                Vector3 leftBefore = rig.Hands[0].Hand.position;
                Vector3 rightBefore = rig.Hands[1].Hand.position;
                Quaternion rightRotation = rig.Hands[1].Hand.rotation;
                rig.Pose(1);
                Assert.That(Vector3.Dot(rig.Hands[0].Hand.position - leftBefore, rig.Wrapper.forward), Is.GreaterThan(.1f));
                Assert.That(Vector3.Dot(rig.Hands[0].LongAxis, rig.Wrapper.forward), Is.GreaterThan(.99f));
                Assert.That(Mathf.Abs(Vector3.Dot(rig.Hands[0].PalmNormal, rig.Wrapper.right)), Is.GreaterThan(.99f), "Full jab keeps a vertical fist.");
                Assert.That(Vector3.Distance(rightBefore, rig.Hands[1].Hand.position), Is.LessThan(.001f));
                Assert.That(Quaternion.Angle(rightRotation, rig.Hands[1].Hand.rotation), Is.LessThan(.1f));
                AssertAlignedAndClosed(rig, 0);
                AssertAlignedAndClosed(rig, 1);
            }
        }

        [TestCase(0)]
        [TestCase(1)]
        public void WristOrientationStaysContinuousAcrossTheWholePunch(int side)
        {
            using (var rig = new Rig())
            {
                float outward = Mathf.Sign(rig.Wrapper.InverseTransformPoint(rig.Arms[side].position).x);
                Quaternion previous = Quaternion.identity;
                for (int sample = 0; sample <= 200; sample++)
                {
                    CityActorMotion.PoseBoxingArm(rig.Arms[side], rig.Elbows[side], rig.Wrapper, outward, sample / 200f, rig.Hands[side]);
                    Quaternion current = rig.Hands[side].Hand.rotation;
                    if (sample > 0) Assert.That(Quaternion.Angle(previous, current), Is.LessThan(3), "Wrist flipped between adjacent punch samples.");
                    AssertAlignedAndClosed(rig, side);
                    previous = current;
                }
            }
        }

        [TestCase(0)]
        [TestCase(5)]
        public void RepeatedGuardAndJabDoNotStretchLimbsOrAccumulateTwist(int strength)
        {
            using (var rig = new Rig())
            {
                rig.Body.Apply(strength);
                rig.Pose(0);
                var positions = new Vector3[2];
                var rotations = new Quaternion[2];
                var lengths = new float[4];
                for (int side = 0; side < 2; side++)
                {
                    positions[side] = rig.Hands[side].Hand.position;
                    rotations[side] = rig.Hands[side].Hand.rotation;
                    lengths[side * 2] = Vector3.Distance(rig.Arms[side].position, rig.Elbows[side].position);
                    lengths[side * 2 + 1] = Vector3.Distance(rig.Elbows[side].position, rig.Hands[side].Hand.position);
                }
                for (int sample = 0; sample < 100; sample++)
                {
                    rig.Pose(Mathf.PingPong(sample * .17f, 1));
                    for (int side = 0; side < 2; side++)
                    {
                        AssertAlignedAndClosed(rig, side);
                        Assert.That(Vector3.Distance(rig.Arms[side].position, rig.Elbows[side].position), Is.EqualTo(lengths[side * 2]).Within(.0001f));
                        Assert.That(Vector3.Distance(rig.Elbows[side].position, rig.Hands[side].Hand.position), Is.EqualTo(lengths[side * 2 + 1]).Within(.0001f));
                    }
                }
                rig.Pose(0);
                for (int side = 0; side < 2; side++)
                {
                    Assert.That(Vector3.Distance(positions[side], rig.Hands[side].Hand.position), Is.LessThan(.001f));
                    Assert.That(Quaternion.Angle(rotations[side], rig.Hands[side].Hand.rotation), Is.LessThan(.1f));
                }
            }
        }

        [Test]
        public void GuardOverridesResampledLegacyFingerPosesOnBothHands()
        {
            using (var rig = new Rig())
            {
                foreach (string name in new[] { "idle", "run" })
                {
                    AnimationClip clip = Array.Find(Resources.LoadAll<AnimationClip>("Models/KenneyRetro/" + name), item => !item.name.StartsWith("__preview__"));
                    Assert.That(clip, Is.Not.Null);
                    for (int sample = 0; sample < 5; sample++)
                    {
                        clip.SampleAnimation(rig.Animation.gameObject, clip.length * sample / 5f);
                        rig.Body.Apply(0);
                        rig.Pose(0);
                        AssertAlignedAndClosed(rig, 0);
                        AssertAlignedAndClosed(rig, 1);
                    }
                }
            }
        }

        [Test]
        public void PausedSnapshotAndDisableRestoreEveryOwnedHandBone()
        {
            float previousScale = Time.timeScale;
            using (var rig = new Rig())
            {
                try
                {
                    CityActorMotion motion = rig.Wrapper.gameObject.AddComponent<CityActorMotion>();
                    motion.Initialize(rig.Owner.transform, rig.Animation);
                    var life = new CityLifestyle(new CitySession(0));
                    var run = new CityBoxingRun();
                    Assert.IsTrue(life.TryStartBoxing());
                    run.Start();
                    motion.ConfigureBoxing(life, run);
                    Assert.IsTrue(motion.BoxingPoseActive);
                    Transform[] owned = Field<Transform[]>(motion, "poseBones");
                    Quaternion[] baseline = Field<Quaternion[]>(motion, "basePose");
                    Quaternion[] rendered = Field<Quaternion[]>(motion, "renderedPose");
                    for (int i = 0; i < owned.Length; i++) if (owned[i] != null) baseline[i] = owned[i].localRotation;
                    // EditMode has no advancing player loop: seed a real rendered pose into its normal snapshots.
                    rig.Pose(.65f);
                    for (int i = 0; i < owned.Length; i++) if (owned[i] != null) rendered[i] = owned[i].localRotation;
                    SetField(motion, "posed", true);
                    SetField(motion, "hasRenderedPose", true);
                    rig.Idle.SampleAnimation(rig.Animation.gameObject, rig.Idle.length * .65f);
                    Time.timeScale = 0;
                    Invoke(motion, "LateUpdate");
                    AssertHandSnapshots(rig, owned, rendered);
                    life.CancelBoxing();
                    Assert.IsFalse(motion.BoxingPoseActive);
                    Invoke(motion, "OnDisable");
                    AssertHandSnapshots(rig, owned, baseline);
                }
                finally { Time.timeScale = previousScale; }
            }
        }

        private static void AssertAlignedAndClosed(Rig rig, int side)
        {
            CityHandPose pose = rig.Hands[side];
            Vector3 forearm = (pose.Hand.position - rig.Elbows[side].position).normalized;
            Assert.That(Vector3.Dot(pose.LongAxis, forearm), Is.GreaterThan(.99f), "Wrist is bent away from its forearm.");
            Transform index = pose.Finger(0), tip = pose.Finger(2).GetChild(0);
            Assert.That(Vector3.Dot(tip.position - index.position, pose.LongAxis), Is.LessThan(-.005f), "Index chain must curl back toward the wrist on both mirrored hands.");
            Quaternion rotation = pose.Hand.rotation;
            Assert.IsTrue(float.IsFinite(rotation.x) && float.IsFinite(rotation.y) && float.IsFinite(rotation.z) && float.IsFinite(rotation.w));
            for (int i = 0; i < 5; i++)
            {
                rotation = pose.Finger(i).localRotation;
                Assert.IsTrue(float.IsFinite(rotation.x) && float.IsFinite(rotation.y) && float.IsFinite(rotation.z) && float.IsFinite(rotation.w));
            }
        }

        private static void AssertHandSnapshots(Rig rig, Transform[] owned, Quaternion[] rotations)
        {
            for (int side = 0; side < 2; side++)
                for (int part = -1; part < 5; part++)
                {
                    Transform bone = part < 0 ? rig.Hands[side].Hand : rig.Hands[side].Finger(part);
                    int slot = Array.IndexOf(owned, bone);
                    Assert.That(slot, Is.GreaterThanOrEqualTo(0), bone.name + " must participate in pose ownership.");
                    Assert.That(Quaternion.Angle(bone.localRotation, rotations[slot]), Is.LessThan(.1f), bone.name + " leaked a training pose.");
                }
        }

        private static T Field<T>(object instance, string name) => (T)typeof(CityActorMotion).GetField(name, PrivateInstance).GetValue(instance);
        private static void SetField(object instance, string name, object value) => typeof(CityActorMotion).GetField(name, PrivateInstance).SetValue(instance, value);
        private static void Invoke(object instance, string name) => typeof(CityActorMotion).GetMethod(name, PrivateInstance).Invoke(instance, null);

        private sealed class Rig : IDisposable
        {
            public readonly GameObject Owner;
            public readonly Transform Wrapper;
            public readonly CityCharacterPhysique Body;
            public readonly Animation Animation;
            public readonly AnimationClip Idle;
            public readonly Transform[] Arms = new Transform[2], Elbows = new Transform[2];
            public readonly CityHandPose[] Hands = new CityHandPose[2];

            public Rig()
            {
                Owner = new GameObject("BoxingPoseRegression");
                Wrapper = new GameObject("NormalizedCharacter").transform;
                Wrapper.SetParent(Owner.transform, false);
                GameObject model = Object.Instantiate(CityCharacterModel.LoadSource(), Wrapper);
                Body = new CityCharacterPhysique(model);
                Idle = Array.Find(Resources.LoadAll<AnimationClip>("Models/KenneyRetro/idle"), item => !item.name.StartsWith("__preview__"));
                Assert.IsTrue(CityCharacterModel.Normalize(Wrapper, model, Idle, Body));
                GameObject animationRoot = CityCharacterModel.AnimationRoot(model);
                Animation = animationRoot.GetComponent<Animation>();
                if (Animation == null) Animation = animationRoot.AddComponent<Animation>();
                Animation.enabled = false;
                Transform[] bones = model.GetComponentsInChildren<Transform>();
                for (int side = 0; side < 2; side++)
                {
                    string prefix = side == 0 ? "Left" : "Right";
                    Arms[side] = Array.Find(bones, bone => bone.name == prefix + "Arm");
                    Elbows[side] = Array.Find(bones, bone => bone.name == prefix + "ForeArm");
                    Hands[side] = new CityHandPose(Array.Find(bones, bone => bone.name == prefix + "Hand"), side == 0);
                    Assert.IsTrue(Hands[side].Ready, "The rendered licensed rig must expose both finger chains.");
                    AssertWeightedFingers(model, Hands[side]);
                }
                Owner.transform.SetPositionAndRotation(new Vector3(4, 2, -3), Quaternion.Euler(0, 71, 0));
            }

            public void Pose(float punch)
            {
                for (int side = 0; side < 2; side++)
                {
                    float outward = Mathf.Sign(Wrapper.InverseTransformPoint(Arms[side].position).x);
                    CityActorMotion.PoseBoxingArm(Arms[side], Elbows[side], Wrapper, outward, side == 0 ? punch : 0, Hands[side]);
                }
            }

            private static void AssertWeightedFingers(GameObject model, CityHandPose hand)
            {
                foreach (SkinnedMeshRenderer renderer in model.GetComponentsInChildren<SkinnedMeshRenderer>())
                {
                    int boneIndex = Array.IndexOf(renderer.bones, hand.Finger(2));
                    if (boneIndex < 0) continue;
                    foreach (BoneWeight weight in renderer.sharedMesh.boneWeights)
                        if ((weight.boneIndex0 == boneIndex && weight.weight0 > .1f) || (weight.boneIndex1 == boneIndex && weight.weight1 > .1f) ||
                            (weight.boneIndex2 == boneIndex && weight.weight2 > .1f) || (weight.boneIndex3 == boneIndex && weight.weight3 > .1f)) return;
                }
                Assert.Fail("Finger pose must deform rendered skin, not only an unweighted helper skeleton.");
            }

            public void Dispose() { Body.Dispose(); Object.DestroyImmediate(Owner); }
        }
    }
}
