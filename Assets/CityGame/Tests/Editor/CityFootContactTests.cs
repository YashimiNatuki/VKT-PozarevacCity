using System;
using NUnit.Framework;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Pozarevac.CityGame.Tests
{
    public sealed class CityFootContactTests
    {
        private sealed class Rig : IDisposable
        {
            public readonly GameObject Owner = new GameObject("GroundContactTest");
            public readonly Transform Facing;
            public readonly GameObject Model;
            private readonly CityCharacterPhysique physique;
            public Rig()
            {
                Owner.transform.position = new Vector3(3000, 100, 3000);
                Owner.transform.rotation = Quaternion.Euler(0, 65, 0);
                Facing = new GameObject("NormalizedCharacter").transform;
                Facing.SetParent(Owner.transform, false);
                Model = Object.Instantiate(CityCharacterModel.LoadSource(), Facing);
                physique = new CityCharacterPhysique(Model);
                var idle = Array.Find(Resources.LoadAll<AnimationClip>("Models/KenneyRetro/idle"), c => !c.name.StartsWith("__preview__"));
                Assert.IsTrue(CityCharacterModel.Normalize(Facing, Model, idle, physique));
            }
            public Transform Bone(string name) => Array.Find(Model.GetComponentsInChildren<Transform>(), t => t.name == name);
            public void Dispose() { physique.Dispose(); Object.DestroyImmediate(Owner); }
        }

        [TestCase("Left")]
        [TestCase("Right")]
        public void RaisedSupportLiftsTheRealFootWithoutMovingTheOwnerOrStretchingBones(string side)
        {
            using (var rig = new Rig())
            {
                var hip = rig.Bone(side + "UpLeg"); var knee = rig.Bone(side + "Leg"); var foot = rig.Bone(side + "Foot");
                var contact = new CityFootContact(rig.Owner.transform, rig.Facing, hip, knee, foot);
                float height = foot.position.y - rig.Owner.transform.position.y;
                float upper = Vector3.Distance(hip.position, knee.position), lower = Vector3.Distance(knee.position, foot.position);
                Quaternion sole = foot.rotation; Vector3 owner = rig.Owner.transform.position;
                CityActorMotion.PoseLeg(hip, knee, foot, rig.Facing, 0, 0);
                var support = new Vector3(foot.position.x, owner.y + .12f, foot.position.z);
                Assert.IsTrue(contact.Apply(support, Vector3.up), "Reachable curb contact was not applied.");
                Assert.That(foot.position.y, Is.EqualTo(support.y + height).Within(.003f));
                Assert.That(Quaternion.Angle(foot.rotation, sole), Is.LessThan(.1f));
                Assert.That(Vector3.Distance(hip.position, knee.position), Is.EqualTo(upper).Within(.0001f));
                Assert.That(Vector3.Distance(knee.position, foot.position), Is.EqualTo(lower).Within(.0001f));
                Assert.That(rig.Owner.transform.position, Is.EqualTo(owner));
                AssertShoeAbovePlane(rig.Model, foot, support, Vector3.up);
            }
        }

        [Test]
        public void SlopedSupportUsesCalibratedSoleAxesInsteadOfLocalFootUp()
        {
            using (var rig = new Rig())
            {
                var hip = rig.Bone("LeftUpLeg"); var knee = rig.Bone("LeftLeg"); var foot = rig.Bone("LeftFoot");
                var contact = new CityFootContact(rig.Owner.transform, rig.Facing, hip, knee, foot);
                Quaternion sole = foot.rotation;
                float height = foot.position.y - rig.Owner.transform.position.y;
                Vector3 normal = Quaternion.AngleAxis(20, rig.Facing.right) * Vector3.up;
                Vector3 support = new Vector3(foot.position.x, rig.Owner.transform.position.y + .10f, foot.position.z);
                Assert.IsTrue(contact.Apply(support, normal));
                Assert.That(Vector3.Dot(foot.position - support, normal), Is.EqualTo(height).Within(.003f));
                Assert.That(Vector3.Angle((foot.rotation * Quaternion.Inverse(sole)) * Vector3.up, normal), Is.LessThan(.1f));
                AssertShoeAbovePlane(rig.Model, foot, support, normal);
            }
        }

        [TestCase(0)]
        [TestCase(1)]
        [TestCase(2)]
        [TestCase(3)]
        public void MissingSelfTriggerAndRigidbodySupportLeaveTheLegUntouched(int kind)
        {
            using (var rig = new Rig())
            {
                var hip = rig.Bone("LeftUpLeg"); var knee = rig.Bone("LeftLeg"); var foot = rig.Bone("LeftFoot");
                var contact = new CityFootContact(rig.Owner.transform, rig.Facing, hip, knee, foot);
                Quaternion upper = hip.rotation, lower = knee.rotation, sole = foot.rotation; Vector3 position = foot.position;
                GameObject surface = null;
                try
                {
                    if (kind != 0)
                    {
                        surface = GameObject.CreatePrimitive(PrimitiveType.Cube);
                        surface.transform.position = new Vector3(foot.position.x, rig.Owner.transform.position.y + .08f, foot.position.z);
                        surface.transform.localScale = new Vector3(.3f, .1f, .3f);
                        if (kind == 1) surface.transform.SetParent(rig.Owner.transform, true);
                        if (kind == 2) surface.GetComponent<Collider>().isTrigger = true;
                        if (kind == 3) surface.AddComponent<Rigidbody>().isKinematic = true;
                    }
                    Physics.SyncTransforms();
                    Assert.IsFalse(contact.Probe());
                    Assert.That(foot.position, Is.EqualTo(position));
                    Assert.That(Quaternion.Angle(upper, hip.rotation), Is.LessThan(.01f));
                    Assert.That(Quaternion.Angle(lower, knee.rotation), Is.LessThan(.01f));
                    Assert.That(Quaternion.Angle(sole, foot.rotation), Is.LessThan(.01f));
                }
                finally { if (surface != null) Object.DestroyImmediate(surface); }
            }
        }

        [Test]
        public void LiftedSwingAndUnsafeContactsNeverPullOrDistortTheLeg()
        {
            using (var rig = new Rig())
            {
                var hip = rig.Bone("LeftUpLeg"); var knee = rig.Bone("LeftLeg"); var foot = rig.Bone("LeftFoot");
                var contact = new CityFootContact(rig.Owner.transform, rig.Facing, hip, knee, foot);
                CityActorMotion.PoseLeg(hip, knee, foot, rig.Facing, 28, 45);
                Quaternion upper = hip.rotation, lower = knee.rotation, sole = foot.rotation; Vector3 position = foot.position;
                Vector3 point = new Vector3(foot.position.x, rig.Owner.transform.position.y - .2f, foot.position.z);
                Assert.IsFalse(contact.Apply(point, Vector3.up));
                Assert.IsFalse(contact.Apply(foot.position + Vector3.up * .5f, Vector3.up));
                Assert.IsFalse(contact.Apply(foot.position + Vector3.right * 3f, Vector3.up));
                Assert.IsFalse(contact.Apply(point, Quaternion.Euler(60, 0, 0) * Vector3.up));
                Assert.IsFalse(contact.Apply(point, Vector3.down));
                Assert.IsFalse(contact.Apply(point, Vector3.zero));
                Assert.IsFalse(contact.Apply(new Vector3(float.NaN, 0, 0), Vector3.up));
                Assert.IsFalse(contact.Apply(point, new Vector3(0, float.PositiveInfinity, 0)));
                Assert.That(foot.position, Is.EqualTo(position));
                Assert.That(Quaternion.Angle(upper, hip.rotation), Is.LessThan(.01f));
                Assert.That(Quaternion.Angle(lower, knee.rotation), Is.LessThan(.01f));
                Assert.That(Quaternion.Angle(sole, foot.rotation), Is.LessThan(.01f));
            }
        }

        [Test]
        public void ProbeFindsAStaticSlopeThroughAnIgnoredTrigger()
        {
            using (var rig = new Rig())
            {
                var hip = rig.Bone("LeftUpLeg"); var knee = rig.Bone("LeftLeg"); var foot = rig.Bone("LeftFoot");
                var contact = new CityFootContact(rig.Owner.transform, rig.Facing, hip, knee, foot);
                Quaternion sole = foot.rotation;
                var surface = GameObject.CreatePrimitive(PrimitiveType.Cube);
                var trigger = GameObject.CreatePrimitive(PrimitiveType.Cube);
                try
                {
                    surface.transform.rotation = Quaternion.AngleAxis(20, rig.Facing.right);
                    Vector3 normal = surface.transform.up;
                    Vector3 point = new Vector3(foot.position.x, rig.Owner.transform.position.y + .10f, foot.position.z);
                    surface.transform.position = point - normal * .05f; surface.transform.localScale = new Vector3(2, .1f, 2);
                    trigger.transform.position = point + Vector3.up * .08f; trigger.transform.localScale = new Vector3(2, .01f, 2);
                    trigger.GetComponent<Collider>().isTrigger = true;
                    Physics.SyncTransforms();
                    Assert.IsTrue(contact.Probe());
                    Assert.That(Vector3.Angle((foot.rotation * Quaternion.Inverse(sole)) * Vector3.up, normal), Is.LessThan(.2f));
                    AssertShoeAbovePlane(rig.Model, foot, point, normal);
                }
                finally { Object.DestroyImmediate(surface); Object.DestroyImmediate(trigger); }
            }
        }

        private static void AssertShoeAbovePlane(GameObject model, Transform foot, Vector3 point, Vector3 normal)
        {
            int count = 0; float lowest = float.PositiveInfinity;
            foreach (var skin in model.GetComponentsInChildren<SkinnedMeshRenderer>())
            {
                var baked = new Mesh();
                try
                {
                    skin.BakeMesh(baked, true);
                    var vertices = baked.vertices; var weights = skin.sharedMesh.boneWeights; var bones = skin.bones;
                    for (int i = 0; i < vertices.Length; i++)
                    {
                        BoneWeight w = weights[i];
                        float selected = FootWeight(w.boneIndex0, w.weight0, bones, foot) + FootWeight(w.boneIndex1, w.weight1, bones, foot) +
                            FootWeight(w.boneIndex2, w.weight2, bones, foot) + FootWeight(w.boneIndex3, w.weight3, bones, foot);
                        if (selected < .75f) continue;
                        count++; lowest = Mathf.Min(lowest, Vector3.Dot(skin.transform.TransformPoint(vertices[i]) - point, normal));
                    }
                }
                finally { Object.DestroyImmediate(baked); }
            }
            Assert.That(count, Is.GreaterThanOrEqualTo(4), "No actual shoe vertices selected.");
            Assert.That(lowest, Is.InRange(-.01f, .02f), "Shoe penetrates or floats above the support plane.");
        }

        private static float FootWeight(int index, float weight, Transform[] bones, Transform foot) =>
            weight > 0 && (bones[index] == foot || bones[index].IsChildOf(foot)) ? weight : 0;

        [Test]
        public void TargetBeyondLegReachIsRejectedBeforeAnyRotationChanges()
        {
            using (var rig = new Rig())
            {
                var hip = rig.Bone("LeftUpLeg"); var knee = rig.Bone("LeftLeg"); var foot = rig.Bone("LeftFoot");
                float clearance = foot.position.y - rig.Owner.transform.position.y;
                var contact = new CityFootContact(rig.Owner.transform, rig.Facing, hip, knee, foot);
                CityActorMotion.PoseLeg(hip, knee, foot, rig.Facing, 0, 0);
                Quaternion upper = hip.rotation, lower = knee.rotation, sole = foot.rotation; Vector3 position = foot.position;
                Vector3 point = position + rig.Facing.right * .14f + Vector3.up * (.001f - clearance);
                Assert.IsFalse(contact.Apply(point, Vector3.up));
                Assert.That(foot.position, Is.EqualTo(position));
                Assert.That(Quaternion.Angle(upper, hip.rotation), Is.LessThan(.01f));
                Assert.That(Quaternion.Angle(lower, knee.rotation), Is.LessThan(.01f));
                Assert.That(Quaternion.Angle(sole, foot.rotation), Is.LessThan(.01f));
            }
        }

        [Test]
        public void SaturatedProbeDoesNotGuessWhichSurfaceIsNearest()
        {
            using (var rig = new Rig())
            {
                var hip = rig.Bone("LeftUpLeg"); var knee = rig.Bone("LeftLeg"); var foot = rig.Bone("LeftFoot");
                var contact = new CityFootContact(rig.Owner.transform, rig.Facing, hip, knee, foot);
                var surfaces = new GameObject[13]; Vector3 position = foot.position;
                try
                {
                    for (int i = 0; i < surfaces.Length; i++)
                    {
                        surfaces[i] = GameObject.CreatePrimitive(PrimitiveType.Cube);
                        surfaces[i].transform.position = new Vector3(position.x, rig.Owner.transform.position.y + i * .008f, position.z);
                        surfaces[i].transform.localScale = new Vector3(.3f, .003f, .3f);
                    }
                    Physics.SyncTransforms();
                    Assert.IsFalse(contact.Probe());
                    Assert.That(foot.position, Is.EqualTo(position));
                }
                finally { foreach (var surface in surfaces) if (surface != null) Object.DestroyImmediate(surface); }
            }
        }

        [Test]
        public void OverflowedBoneLengthIsRejectedBeforeQuaternionMath()
        {
            var owner = new GameObject("MalformedContactRig");
            try
            {
                var hip = new GameObject("Hip").transform; hip.SetParent(owner.transform, false); hip.localPosition = Vector3.up;
                var knee = new GameObject("Knee").transform; knee.SetParent(hip, false); knee.localPosition = new Vector3(1e20f, -.5f, 0);
                var foot = new GameObject("Foot").transform; foot.SetParent(knee, false); foot.localPosition = new Vector3(-1e20f, -.4f, 0);
                var contact = new CityFootContact(owner.transform, owner.transform, hip, knee, foot);
                Assert.IsFalse(contact.Apply(new Vector3(0, .1f, 0), Vector3.up));
                Assert.That(hip.rotation, Is.EqualTo(Quaternion.identity));
                Assert.That(knee.rotation, Is.EqualTo(Quaternion.identity));
            }
            finally { Object.DestroyImmediate(owner); }
        }

        [TestCase(12f)]
        [TestCase(-12f)]
        public void PhysicallyGroundedPlayerHasAReachableFootContact(float slope)
        {
            using (var rig = new Rig())
            {
                var left = new CityFootContact(rig.Owner.transform, rig.Facing, rig.Bone("LeftUpLeg"), rig.Bone("LeftLeg"), rig.Bone("LeftFoot"));
                var right = new CityFootContact(rig.Owner.transform, rig.Facing, rig.Bone("RightUpLeg"), rig.Bone("RightLeg"), rig.Bone("RightFoot"));
                var grounding = new CityGroundingOffset(rig.Owner.transform, rig.Facing);
                var surface = GameObject.CreatePrimitive(PrimitiveType.Cube);
                try
                {
                    surface.transform.position = rig.Owner.transform.position;
                    surface.transform.rotation = Quaternion.Euler(0, 0, slope);
                    surface.transform.localScale = new Vector3(6, .2f, 6);
                    rig.Owner.transform.position += Vector3.up * .5f;
                    var body = rig.Owner.AddComponent<CharacterController>();
                    body.height = 1.8f; body.radius = .32f; body.center = new Vector3(0, .9f, 0);
                    Physics.SyncTransforms();
                    for (int i = 0; i < 50; i++) body.Move(Vector3.down * .04f);
                    Assert.IsTrue(body.isGrounded, "Fixture did not reach the actual collider.");
                    Vector3 owner = rig.Owner.transform.position;
                    for (int i = 0; i < 50; i++) grounding.Step(body.isGrounded, .02f);
                    Assert.That(grounding.Offset, Is.InRange(-.12f, .12f));
                    bool leftContact = left.Probe(), rightContact = right.Probe();
                    Assert.IsTrue(leftContact || rightContact, $"No supported shoe: root={owner:F4}, left={rig.Bone("LeftFoot").position:F4}, right={rig.Bone("RightFoot").position:F4}");
                    Vector3 plane = surface.transform.position + surface.transform.up * .1f;
                    if (leftContact) AssertShoeAbovePlane(rig.Model, rig.Bone("LeftFoot"), plane, surface.transform.up);
                    if (rightContact) AssertShoeAbovePlane(rig.Model, rig.Bone("RightFoot"), plane, surface.transform.up);
                    Assert.That(rig.Owner.transform.position, Is.EqualTo(owner));
                    Vector3 paused = rig.Facing.localPosition;
                    grounding.Step(true, 0);
                    Assert.That(rig.Facing.localPosition, Is.EqualTo(paused));
                    for (int i = 0; i < 50; i++) grounding.Step(false, .02f);
                    Assert.That(Mathf.Abs(grounding.Offset), Is.LessThan(.0001f));
                    grounding.Reset();
                    Assert.That(grounding.Offset, Is.Zero);
                }
                finally { Object.DestroyImmediate(surface); }
            }
        }
    }
}
