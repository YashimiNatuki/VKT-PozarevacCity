using System;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;

namespace Pozarevac.CityGame.Tests.Editor
{
    public sealed class CityHumanPhoneTests
    {
        private GameObject player;
        private CityHumanActor actor;
        [SetUp] public void SetUp()
        {
            player = new GameObject("PhoneActorFixture");
            player.AddComponent<CharacterController>().enabled = false;
            Assert.That(CityHumanActor.TryCreate(player.transform, Resources.Load<GameObject>("Models/RodjaHuman/RodjaHuman"), out actor), Is.True);
            actor.SetVisible(true);
        }
        [TearDown] public void TearDown()
        {
            actor?.Dispose();
            if (player != null) UnityEngine.Object.DestroyImmediate(player);
        }

        [Test] public void PhoneRaisesOnPresentationClockWithoutMovingPlayerLegsOrGaitClock()
        {
            player.transform.position += Vector3.forward * .1f; actor.Advance(.02f, false);
            Transform calf = Bone("calf_l"), wrist = Bone("hand_l");
            Quaternion leg = calf.localRotation, hand = wrist.localRotation;
            float phase = actor.Phase; Vector3 position = player.transform.position;
            Assert.That((bool)Call("SetPhoneActive", true), Is.True);
            for (int i = 0; i < 60; i++) { actor.Advance(0, true); Call("AdvancePhone", .02f); }
            Assert.That(Value<bool>("PhoneActive"), Is.True);
            Assert.That(Value<GameObject>("PhoneVisual").activeSelf, Is.True);
            Assert.That(Value<float>("PhoneWeight"), Is.EqualTo(1).Within(.001f));
            Assert.That(actor.Phase, Is.EqualTo(phase));
            Assert.That(player.transform.position, Is.EqualTo(position));
            Assert.That(Quaternion.Angle(leg, calf.localRotation), Is.LessThan(.001f));
            Assert.That(Quaternion.Angle(hand, wrist.localRotation), Is.GreaterThan(10));
            Assert.That(Value<float>("PhoneGripDistance"), Is.LessThan(.014f));
            Assert.That(player.GetComponentsInChildren<Camera>(true), Is.Empty);
            Assert.That(player.GetComponentsInChildren<AudioListener>(true), Is.Empty);
            Assert.That(player.GetComponentsInChildren<Collider>(true).Length, Is.EqualTo(1));
        }

        [TestCase(float.NaN)]
        [TestCase(float.PositiveInfinity)]
        [TestCase(-1f)]
        [TestCase(0f)]
        public void InvalidPresentationDeltaDoesNotAdvanceThePhone(float seconds)
        {
            Call("SetPhoneActive", true); Quaternion before = Bone("hand_l").localRotation;
            Call("AdvancePhone", seconds);
            Assert.That(Value<float>("PhoneWeight"), Is.Zero);
            Assert.That(Quaternion.Angle(before, Bone("hand_l").localRotation), Is.LessThan(.001f));
        }

        [Test] public void ClosingRestoresArmsAndReopeningReusesTheSamePhone()
        {
            Quaternion arm = Bone("upperarm_l").localRotation, finger = Bone("index_02_l").localRotation;
            Call("SetPhoneActive", true); for (int i = 0; i < 30; i++) Call("AdvancePhone", .02f);
            GameObject phone = Value<GameObject>("PhoneVisual");
            Call("SetPhoneActive", false);
            Assert.That(phone.activeSelf, Is.False);
            Assert.That(Quaternion.Angle(arm, Bone("upperarm_l").localRotation), Is.LessThan(.001f));
            Assert.That(Quaternion.Angle(finger, Bone("index_02_l").localRotation), Is.LessThan(.001f));
            Call("SetPhoneActive", true); Call("AdvancePhone", .02f);
            Assert.That(Value<GameObject>("PhoneVisual"), Is.SameAs(phone));
            actor.Advance(.02f, false);
            Assert.That(Value<bool>("PhoneActive"), Is.False, "Resumed gameplay retained the phone pose.");
        }

        [Test] public void TapReachesTheScreenAndRecoversWithoutAnAutomaticIdleTap()
        {
            Call("SetPhoneActive", true); for (int i = 0; i < 40; i++) Call("AdvancePhone", .02f);
            Quaternion idle = Bone("upperarm_r").localRotation;
            for (int i = 0; i < 20; i++) Call("AdvancePhone", .02f);
            Assert.That(Quaternion.Angle(idle, Bone("upperarm_r").localRotation), Is.LessThan(.001f));
            Call("NotifyPhoneTap"); float contact = float.PositiveInfinity, angle = 0, wristBend = 0;
            for (int i = 0; i < 45; i++)
            {
                Call("AdvancePhone", .02f);
                contact = Mathf.Min(contact, Value<float>("PhoneTapDistance"));
                if (Value<float>("PhoneTapDistance") < .012f)
                    wristBend = Mathf.Max(wristBend, Vector3.Angle(Bone("hand_r").position - Bone("lowerarm_r").position,
                        Bone("index_03_r").position - Bone("index_02_r").position));
                angle = Mathf.Max(angle, Quaternion.Angle(idle, Bone("upperarm_r").localRotation));
            }
            Assert.That(contact, Is.LessThan(.012f));
            Assert.That(angle, Is.GreaterThan(10));
            Assert.That(wristBend, Is.LessThan(65), "The finger reached the screen with an excessive wrist bend.");
            Assert.That(Quaternion.Angle(idle, Bone("upperarm_r").localRotation), Is.LessThan(.01f));
        }

        [Test] public void PhoneHasPhysicalSizeAndReleasesItsOwnedRenderResources()
        {
            Call("SetPhoneActive", true); for (int i = 0; i < 30; i++) Call("AdvancePhone", .02f);
            GameObject phone = Value<GameObject>("PhoneVisual");
            Transform body = phone.transform.Find("Body");
            Assert.That(Vector3.Distance(body.TransformPoint(Vector3.up * .5f), body.TransformPoint(Vector3.down * .5f)), Is.EqualTo(.151f).Within(.002f));
            Mesh mesh = body.GetComponent<MeshFilter>().sharedMesh;
            Material material = body.GetComponent<Renderer>().sharedMaterial;
            actor.Dispose(); actor = null;
            Assert.That(phone == null && mesh == null && material == null, Is.True, "Phone geometry/material outlived its actor.");
        }

        [Test] public void SingularOwnerScaleRefusesThePhoneWithoutCreatingRenderResources()
        {
            player.transform.localScale = new Vector3(0, 1, 1);
            Assert.That(Value<bool>("PhoneAvailable"), Is.False);
            Assert.That((bool)Call("SetPhoneActive", true), Is.False);
            Assert.That(Value<GameObject>("PhoneVisual"), Is.Null);
        }

        private Transform Bone(string name) => Array.Find(actor.Visual.GetComponentsInChildren<Transform>(true), item => item.name == name);
        private object Call(string name, params object[] args)
        {
            MethodInfo method = typeof(CityHumanActor).GetMethod(name);
            Assert.That(method, Is.Not.Null, "Missing real actor phone presentation: " + name);
            return method.Invoke(actor, args);
        }
        private T Value<T>(string name)
        {
            PropertyInfo property = typeof(CityHumanActor).GetProperty(name);
            Assert.That(property, Is.Not.Null, name);
            return (T)property.GetValue(actor);
        }
    }
}
