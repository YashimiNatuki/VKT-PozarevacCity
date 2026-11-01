using System;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;

namespace Pozarevac.CityGame.Tests.Editor
{
    public sealed class CityHumanFeedbackTests
    {
        private const BindingFlags PrivateInstance = BindingFlags.Instance | BindingFlags.NonPublic;
        private GameObject owner, otherOwner;
        private CityCameraRig rig;
        private CityPlayerVitals vitals;
        private CityProgressionController progress;
        private Transform view;
        private CityGameAudio audioA, audioB;
        private float previousTimeScale;

        [SetUp] public void SetUp()
        {
            previousTimeScale = Time.timeScale; Time.timeScale = 1;
            owner = new GameObject("HumanFeedbackOwner");
            owner.AddComponent<CharacterController>().enabled = false;
            vitals = owner.AddComponent<CityPlayerVitals>();
            progress = owner.AddComponent<CityProgressionController>(); progress.enabled = false;
            view = new GameObject("HumanFeedbackView").transform; view.SetParent(owner.transform, false);
            rig = owner.AddComponent<CityCameraRig>();
            // Isolate the public selection/feedback bridge from unrelated Kenney rendering setup.
            SetField(rig, "owner", owner.transform); SetField(rig, "view", view);
        }

        [TearDown] public void TearDown()
        {
            try
            {
                rig?.HumanActor?.Dispose();
                DestroyAudio(audioA); DestroyAudio(audioB);
                if (owner != null) UnityEngine.Object.DestroyImmediate(owner);
                if (otherOwner != null) UnityEngine.Object.DestroyImmediate(otherOwner);
            }
            finally { Time.timeScale = previousTimeScale; }
        }

        [Test] public void FeedbackConfiguredBeforeLazySelectionReachesTheRealHumanBrow()
        {
            rig.ConfigureFeedback(null);
            CityHumanActor actor = SelectHuman();
            Assert.That(vitals.ApplyDamage(10), Is.True);
            actor.Advance(.02f, false);
            Assert.That(actor.Face.Mood, Is.EqualTo(CityFaceMood.Hurt));
            Assert.That(ActualWeight(actor.Visual, "browDownLeft"), Is.GreaterThan(1));
        }

        [Test] public void ReconfiguringFeedbackUsesTheNewMutedPublisherAndReleasesTheOldOne()
        {
            audioA = CreateMutedAudio(); audioB = CreateMutedAudio();
            rig.ConfigureFeedback(audioA);
            CityHumanActor actor = SelectHuman();
            rig.ConfigureFeedback(audioB);
            Assert.That(audioA.PlayCue(CityAudioCue.Success), Is.False, "Muted feedback must stay inaudible.");
            actor.Advance(.02f, false);
            Assert.That(actor.Face.Mood, Is.EqualTo(CityFaceMood.Calm), "The old audio owner must be disconnected.");
            Assert.That(audioB.PlayCue(CityAudioCue.Success), Is.False);
            actor.Advance(.02f, false);
            Assert.That(actor.Face.Mood, Is.EqualTo(CityFaceMood.Success));
            Assert.That(ActualWeight(actor.Visual, "mouthSmileLeft"), Is.GreaterThan(1));
        }

        [Test] public void PauseKeepsTheDisplayedReactionWhileHiddenWorldTimeExpiresIt()
        {
            rig.ConfigureFeedback(null);
            CityHumanActor actor = SelectHuman();
            Assert.That(vitals.ApplyDamage(10), Is.True);
            actor.Advance(.02f, false);
            Assert.That(actor.Face.Mood, Is.EqualTo(CityFaceMood.Hurt));
            Quaternion head = actor.Face.Head.localRotation;
            float brow = ActualWeight(actor.Visual, "browDownLeft");
            actor.Advance(4, true);
            Assert.That(Quaternion.Angle(head, actor.Face.Head.localRotation), Is.LessThan(.001f));
            Assert.That(ActualWeight(actor.Visual, "browDownLeft"), Is.EqualTo(brow).Within(.001f));
            actor.Advance(.02f, false);
            Assert.That(actor.Face.Mood, Is.EqualTo(CityFaceMood.Hurt), "Paused time must not consume the reaction.");
            actor.SetVisible(false); actor.Advance(2, false); actor.SetVisible(true); actor.Advance(.02f, false);
            Assert.That(actor.Face.Mood, Is.EqualTo(CityFaceMood.Calm), "A hidden avatar must not preserve a stale reaction.");
        }

        [Test] public void RebindAndDisposeReleaseOnlyOwnedEventHandlers()
        {
            CityHumanActor actor = SelectHuman();
            otherOwner = new GameObject("OtherHumanVitals");
            CityPlayerVitals other = otherOwner.AddComponent<CityPlayerVitals>();
            float sentinelDamage = 0; Action<float> sentinel = amount => sentinelDamage += amount;
            vitals.Damaged += sentinel;
            actor.BindFeedback(vitals, null, view); actor.BindFeedback(vitals, null, view);
            Assert.That(OwnedSubscriptions(vitals, nameof(CityPlayerVitals.Damaged), actor), Is.EqualTo(1));
            Assert.That(OwnedSubscriptions(vitals, nameof(CityPlayerVitals.Respawned), actor), Is.EqualTo(1));
            actor.BindFeedback(other, null, view);
            Assert.That(OwnedSubscriptions(vitals, nameof(CityPlayerVitals.Damaged), actor), Is.Zero);
            Assert.That(OwnedSubscriptions(vitals, nameof(CityPlayerVitals.Respawned), actor), Is.Zero);
            Assert.That(OwnedSubscriptions(other, nameof(CityPlayerVitals.Damaged), actor), Is.EqualTo(1));
            actor.Dispose();
            Assert.That(OwnedSubscriptions(other, nameof(CityPlayerVitals.Damaged), actor), Is.Zero);
            Assert.That(OwnedSubscriptions(other, nameof(CityPlayerVitals.Respawned), actor), Is.Zero);
            Assert.That(vitals.ApplyDamage(5), Is.True);
            Assert.That(sentinelDamage, Is.EqualTo(5), "Cleanup must preserve another subscriber.");
        }

        private CityHumanActor SelectHuman()
        {
            Assert.That(rig.SelectHumanAvatar(true), Is.True);
            Assert.That(rig.HumanActor, Is.Not.Null);
            Assert.That(rig.HumanActor.Face, Is.Not.Null, "Reviewed facial asset must be imported.");
            return rig.HumanActor;
        }

        private CityGameAudio CreateMutedAudio()
        {
            CityGameAudio audio = owner.AddComponent<CityGameAudio>();
            audio.Initialize(owner.transform, progress); audio.SetMuted(true); return audio;
        }

        private static float ActualWeight(GameObject visual, string channel)
        {
            foreach (SkinnedMeshRenderer skin in visual.GetComponentsInChildren<SkinnedMeshRenderer>(true))
            {
                Mesh mesh = skin.sharedMesh;
                if (mesh == null) continue;
                for (int i = 0; i < mesh.blendShapeCount; i++)
                {
                    if (!mesh.GetBlendShapeName(i).EndsWith(channel, StringComparison.Ordinal)) continue;
                    var delta = new Vector3[mesh.vertexCount];
                    mesh.GetBlendShapeFrameVertices(i, mesh.GetBlendShapeFrameCount(i) - 1, delta, null, null);
                    foreach (Vector3 vertex in delta)
                        if (skin.transform.TransformVector(vertex).sqrMagnitude > .00000001f) return skin.GetBlendShapeWeight(i);
                }
            }
            Assert.Fail("Nonzero reviewed facial channel missing: " + channel); return 0;
        }

        private static int OwnedSubscriptions(object publisher, string eventName, object owner)
        {
            var handlers = (Delegate)publisher.GetType().GetField(eventName, PrivateInstance).GetValue(publisher);
            if (handlers == null) return 0;
            int count = 0;
            foreach (Delegate handler in handlers.GetInvocationList()) if (ReferenceEquals(handler.Target, owner)) count++;
            return count;
        }

        private static void SetField(object target, string name, object value) => target.GetType().GetField(name, PrivateInstance).SetValue(target, value);

        private static void DestroyAudio(CityGameAudio audio)
        {
            if (audio == null) return;
            // Explicitly release test-created native resources in EditMode. Imported clips remain shared.
            foreach (string name in new[] { "effects", "dialogue", "feedback", "diagnostics" })
            {
                FieldInfo field = typeof(CityGameAudio).GetField(name, PrivateInstance);
                var source = (AudioSource)field.GetValue(audio); field.SetValue(audio, null);
                if (source != null) { source.Stop(); UnityEngine.Object.DestroyImmediate(source); }
            }
            foreach (string name in new[] { "generatedFootstep", "generatedLanding", "grassStep", "woodStep" })
            {
                FieldInfo field = typeof(CityGameAudio).GetField(name, PrivateInstance);
                var clip = (AudioClip)field.GetValue(audio); field.SetValue(audio, null);
                if (clip != null) UnityEngine.Object.DestroyImmediate(clip);
            }
            FieldInfo cuesField = typeof(CityGameAudio).GetField("cues", PrivateInstance);
            var cues = (AudioClip[])cuesField.GetValue(audio); cuesField.SetValue(audio, null);
            if (cues != null) foreach (AudioClip clip in cues) if (clip != null) UnityEngine.Object.DestroyImmediate(clip);
            UnityEngine.Object.DestroyImmediate(audio);
        }
    }
}
