using System;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Pozarevac.CityGame.Tests.Editor
{
    public sealed class CityNativeValidationTests
    {
        private GameObject player, extra;
        private IDisposable scope;
        private Keyboard externalKeyboard;
        private CursorLockMode cursor;
        private bool cursorVisible;

        [SetUp] public void SetUp()
        {
            scope = null; externalKeyboard = null; extra = null;
            cursor = Cursor.lockState; cursorVisible = Cursor.visible;
            player = new GameObject("NativeValidationFixture");
            player.SetActive(false);
        }

        [TearDown] public void TearDown()
        {
            scope?.Dispose();
            if (externalKeyboard != null) InputSystem.RemoveDevice(externalKeyboard);
            if (extra != null) UnityEngine.Object.DestroyImmediate(extra);
            if (player != null) UnityEngine.Object.DestroyImmediate(player);
            Cursor.lockState = cursor; Cursor.visible = cursorVisible;
        }

        [Test] public void ProfileComparisonDoesNotRewriteItsInputSnapshot()
        {
            CityProfileData profile = Profile();
            string before = JsonUtility.ToJson(profile);
            Signature(profile);
            Assert.That(JsonUtility.ToJson(profile), Is.EqualTo(before), "A validation comparison rewrote the supplied profile.");
        }

        [Test] public void ProfileComparisonIgnoresOnlyGardenClocksAndRejectsEconomyChanges()
        {
            string baseline = Signature(Profile());
            CityProfileData later = Profile();
            later.garden.lastUtcSeconds = 200;
            later.garden.plots[0].grownSeconds = 40;
            later.garden.plots[0].moistureSeconds = 60;
            Assert.That(Signature(later), Is.EqualTo(baseline));
            later.cash++;
            Assert.That(Signature(later), Is.Not.EqualTo(baseline), "Garden clock tolerance hid a cash change.");
            later.cash--;
            later.garden.plots[0].variety++;
            Assert.That(Signature(later), Is.Not.EqualTo(baseline), "Garden clock tolerance hid a plant identity change.");
        }

        [TestCase(true)]
        [TestCase(false)]
        public void EarlyExitRestoresMovementCursorNeutralizesViewAndOnlyRemovesOwnedKeyboard(bool enabled)
        {
            CityPlayerController mover = player.AddComponent<CityPlayerController>();
            Camera view = View(); mover.ConfigureView(view.transform); mover.enabled = enabled;
            Quaternion before = Quaternion.Euler(12, 8, 0); view.transform.localRotation = before;
            externalKeyboard = InputSystem.AddDevice<Keyboard>();
            scope = Begin(mover);
            Keyboard owned = (Keyboard)Call("CreateKeyboard");
            Assert.That(Call("CreateKeyboard"), Is.SameAs(owned), "A scenario allocated a second keyboard.");
            try
            {
                mover.enabled = !enabled; view.transform.localRotation = Quaternion.Euler(-25, 0, 0);
                Cursor.visible = !cursorVisible;
                throw new InvalidOperationException("Controlled scenario exit");
            }
            catch (InvalidOperationException) { }
            finally { scope.Dispose(); }
            scope.Dispose();
            Assert.That(mover.enabled, Is.EqualTo(enabled));
            Assert.That(Quaternion.Angle(view.transform.localRotation, Quaternion.identity), Is.LessThan(.001f),
                "Cleanup restored a stale camera rotation after resetting the controller's pitch.");
            Assert.That(Cursor.lockState, Is.EqualTo(cursor));
            Assert.That(Cursor.visible, Is.EqualTo(cursorVisible));
            Assert.That(HasDevice(owned), Is.False);
            Assert.That(HasDevice(externalKeyboard), Is.True);
            TargetInvocationException error = Assert.Throws<TargetInvocationException>(() => Call("CreateKeyboard"));
            Assert.That(error.InnerException, Is.TypeOf<ObjectDisposedException>());
        }

        [TestCase("Camera")]
        [TestCase("AudioListener")]
        [TestCase("CharacterController")]
        public void OwnershipCheckIncludesInactiveOwnersAndRecoversAfterRemoval(string owner)
        {
            CityPlayerController mover = player.AddComponent<CityPlayerController>();
            View(); scope = Begin(mover);
            Assert.That(OwnersUnchanged(), Is.True);
            extra = new GameObject("UnexpectedInactiveOwner"); extra.SetActive(false);
            if (owner == "CharacterController") extra.transform.SetParent(player.transform, false);
            extra.AddComponent(owner == "Camera" ? typeof(Camera) : owner == "AudioListener" ? typeof(AudioListener) : typeof(CharacterController));
            Assert.That(OwnersUnchanged(), Is.False);
            UnityEngine.Object.DestroyImmediate(extra); extra = null;
            Assert.That(OwnersUnchanged(), Is.True);
        }

        [Test] public void ReplacingCameraWithAnotherCameraCannotPassByKeepingTheSameCount()
        {
            CityPlayerController mover = player.AddComponent<CityPlayerController>();
            Camera view = View(); scope = Begin(mover);
            int before = UnityEngine.Object.FindObjectsByType<Camera>(FindObjectsInactive.Include, FindObjectsSortMode.None).Length;
            UnityEngine.Object.DestroyImmediate(view.gameObject);
            View();
            Assert.That(UnityEngine.Object.FindObjectsByType<Camera>(FindObjectsInactive.Include, FindObjectsSortMode.None).Length, Is.EqualTo(before));
            Assert.That(OwnersUnchanged(), Is.False, "Owner identity changed while its count stayed constant.");
        }

        private Camera View()
        {
            var child = new GameObject("ValidationView"); child.transform.SetParent(player.transform, false);
            return child.AddComponent<Camera>();
        }

        private static CityProfileData Profile() => new CityProfileData
        {
            version = 9, cash = 1000,
            garden = new CityGardenData { lastUtcSeconds = 100, plots = new[]
            {
                new CityGardenPlotData { variety = 1, planted = true, grownSeconds = 20, moistureSeconds = 30 }
            } }
        };

        private static string Signature(CityProfileData profile) => (string)typeof(CityPlayerValidation)
            .GetMethod("PreviewProfileSignature", BindingFlags.Static | BindingFlags.NonPublic).Invoke(null, new object[] { profile });

        private static IDisposable Begin(CityPlayerController mover)
        {
            Type type = typeof(CityPlayerValidation).GetNestedType("HumanValidationScope", BindingFlags.NonPublic);
            Assert.That(type, Is.Not.Null, "Native scenarios need one shared lifetime scope.");
            return (IDisposable)Activator.CreateInstance(type, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic,
                null, new object[] { mover }, null);
        }

        private object Call(string name) => scope.GetType().GetMethod(name).Invoke(scope, null);
        private bool OwnersUnchanged() => (bool)scope.GetType().GetProperty("OwnersUnchanged").GetValue(scope);
        private static bool HasDevice(Keyboard keyboard)
        {
            foreach (var device in InputSystem.devices) if (ReferenceEquals(device, keyboard)) return true;
            return false;
        }
    }
}
