#if DEVELOPMENT_BUILD || UNITY_EDITOR
using System;
using System.Collections;
using System.IO;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;

namespace Pozarevac.CityGame
{
    public sealed partial class CityPlayerValidation
    {
        private bool humanFaceVerified;
        [Serializable] private sealed class HumanFaceReport
        {
            public bool passed, authoredChannels, previewBlink, previewJaw, previewSmile,
                previewUnscaled, profileSelectionUnchanged, gazeBounded, worldDamage,
                worldSuccess, pauseStable, hiddenRecovery, ownersUnchanged, controlRestored, eyeGaze, eyeDirectionCorrect;
            public float blinkWeight, jawWeight, smileWeight, blinkVertexMeters,
                jawVertexMeters, smileVertexMeters, hurtVertexMeters, headDegrees,
                neckDegrees, pauseHeadDegrees, pauseMorphDelta, eyeInLeftWeight,
                eyeOutRightWeight, eyeOpposedWeight, eyeVertexMeters, eyeRightMeanMeters;
            public int previewFrames;
        }

        private IEnumerator VerifyHumanFace(CityGameBootstrap game)
        {
            var result = new HumanFaceReport();
            var runtime = game.GetComponent<CityLifestyleRuntime>();
            var progress = game.GetComponent<CityProgressionController>();
            var rig = game.Player.GetComponentInChildren<CityCameraRig>();
            var mover = game.Player.GetComponent<CityPlayerController>();
            Camera view = game.Player.GetComponentInChildren<Camera>();
            string folder = Argument("-vktHumanFaceCaptures", "");
            if (folder.Length == 0) { Finish(false, "Human face capture folder missing."); yield break; }
            Directory.CreateDirectory(folder);
            Keyboard keys = null;
            float healthBefore = game.Vitals.Health.Current;
            var scope = new HumanValidationScope(mover);
            try
            {
                progress.ClosePanel(); game.GetComponent<CityMusicPanel>()?.Close(); runtime.CloseMenu();
                if (game.Hideout.IsInside) game.Hideout.Travel(false);
                mover.enabled = false;
                int cash = game.Session.Cash, xp = runtime.Life.Experience;
                if (!runtime.Wardrobe.Open() || !runtime.Wardrobe.ToggleCandidate())
                { Finish(false, "Human facial wardrobe route unavailable."); yield break; }
                if (!runtime.Wardrobe.Portrait) runtime.Wardrobe.TogglePortrait();
                runtime.Wardrobe.Rotate(15);
                yield return new WaitForSecondsRealtime(.3f); yield return new WaitForEndOfFrame();
                GameObject preview = GameObject.Find("ReviewedHumanPreview");
                SkinnedMeshRenderer previewSkin = HumanBodySkin(preview);
                result.authoredChannels = previewSkin != null && HumanFaceHasChannels(previewSkin);
                if (!result.authoredChannels)
                { Finish(false, "Reviewed human is missing authored eye, jaw, smile or brow channels."); yield break; }
                Vector3 worldPosition = game.Player.position;
                float deadline = Time.realtimeSinceStartup + 10;
                int blink = HumanFaceShape(previewSkin, "eyeBlinkLeft");
                while (Time.realtimeSinceStartup < deadline && !result.previewBlink)
                {
                    yield return new WaitForEndOfFrame(); result.previewFrames++;
                    float weight = previewSkin.GetBlendShapeWeight(blink);
                    if (weight < 55) continue;
                    result.blinkWeight = weight;
                    result.blinkVertexMeters = HumanFaceVertexDelta(previewSkin, blink);
                    result.previewBlink = weight <= 100.01f && result.blinkVertexMeters > .0001f;
                    if (result.previewBlink && !CaptureRenderedFrame(game, Path.Combine(folder, "face-blink.png"), out string blinkError))
                    { Finish(false, blinkError); yield break; }
                }
                result.previewUnscaled = result.previewBlink && result.previewFrames >= 3 && game.Director.IsPaused &&
                    Time.timeScale == 0 && Vector3.Distance(worldPosition, game.Player.position) < .001f;
                if (!result.previewBlink || !result.previewUnscaled)
                { Finish(false, "Authored eyelid did not deform on the unscaled wardrobe clock."); yield break; }
                deadline = Time.realtimeSinceStartup + 2;
                while (Time.realtimeSinceStartup < deadline && previewSkin.GetBlendShapeWeight(blink) > 5) yield return null;
                yield return new WaitForEndOfFrame();
                if (!CaptureRenderedFrame(game, Path.Combine(folder, "face-calm.png"), out string captureError))
                { Finish(false, captureError); yield break; }
                if (!HumanFaceChooseMood(runtime, CityFaceMood.Effort))
                { Finish(false, "Human effort expression button unavailable."); yield break; }
                yield return new WaitForSecondsRealtime(.45f); yield return new WaitForEndOfFrame();
                int jaw = HumanFaceShape(previewSkin, "jawOpen");
                result.jawWeight = previewSkin.GetBlendShapeWeight(jaw);
                result.jawVertexMeters = HumanFaceVertexDelta(previewSkin, jaw);
                result.previewJaw = result.jawWeight > 5 && result.jawWeight <= 100.01f && result.jawVertexMeters > .0001f;
                if (!result.previewJaw || !CaptureRenderedFrame(game, Path.Combine(folder, "face-effort.png"), out captureError))
                { Finish(false, "Human effort did not deform the authored jaw: " + captureError); yield break; }
                if (!HumanFaceChooseMood(runtime, CityFaceMood.Success))
                { Finish(false, "Human success expression button unavailable."); yield break; }
                yield return new WaitForSecondsRealtime(.45f); yield return new WaitForEndOfFrame();
                int smile = HumanFaceShape(previewSkin, "mouthSmileLeft");
                result.smileWeight = previewSkin.GetBlendShapeWeight(smile);
                result.smileVertexMeters = HumanFaceVertexDelta(previewSkin, smile);
                result.previewSmile = result.smileWeight > 5 && result.smileWeight <= 100.01f && result.smileVertexMeters > .0001f;
                if (!result.previewSmile || !CaptureRenderedFrame(game, Path.Combine(folder, "face-success.png"), out captureError))
                { Finish(false, "Human success did not deform the authored smile: " + captureError); yield break; }
                runtime.Wardrobe.Rotate(45);
                yield return new WaitForSecondsRealtime(.7f); yield return new WaitForEndOfFrame();
                if (!CaptureRenderedFrame(game, Path.Combine(folder, "face-gaze-portrait.png"), out captureError))
                { Finish(false, captureError); yield break; }

                var checkpoint = new HumanProfileCheckpoint(game);
                var play = HumanButton("IGRAJ U GRADU");
                if (play == null) { Finish(false, "Human facial preview has no city choice."); yield break; }
                play.onClick.Invoke();
                result.profileSelectionUnchanged = checkpoint.IsUnchanged;
                yield return null; yield return new WaitForEndOfFrame();
                CityHumanActor actor = rig.HumanActor;
                if (!result.profileSelectionUnchanged || !rig.HumanSelected || actor?.Face == null || !actor.IsVisible || game.Director.IsPaused)
                { Finish(false, "Human facial selection changed profile or failed to return to the city."); yield break; }
                Transform[] bones = actor.Visual.GetComponentsInChildren<Transform>(true);
                Transform head = Array.Find(bones, bone => bone.name == "head");
                Transform neck = Array.Find(bones, bone => bone.name == "neck_01");
                SkinnedMeshRenderer skin = HumanBodySkin(actor.Visual);
                if (head == null || neck == null || skin == null)
                { Finish(false, "World human facial geometry missing."); yield break; }
                view.transform.rotation = Quaternion.LookRotation(actor.Visual.transform.forward, Vector3.up);
                yield return new WaitForSecondsRealtime(.6f); yield return new WaitForEndOfFrame();
                Quaternion headNeutral = head.rotation, neckNeutral = neck.rotation;
                view.transform.rotation = Quaternion.LookRotation(actor.Visual.transform.right, Vector3.up);
                yield return new WaitForSecondsRealtime(.7f); yield return new WaitForEndOfFrame();
                result.headDegrees = Quaternion.Angle(headNeutral, head.rotation);
                result.neckDegrees = Quaternion.Angle(neckNeutral, neck.rotation);
                Vector2 gaze = actor.Face.GazeAngles;
                result.gazeBounded = result.headDegrees > 8 && result.headDegrees < 65 && result.neckDegrees > 1 &&
                    float.IsFinite(gaze.x) && float.IsFinite(gaze.y) && Mathf.Abs(gaze.x) <= 55 && Mathf.Abs(gaze.y) <= 35;
                SkinnedMeshRenderer eyeSkin = HumanFaceEyes(actor.Visual);
                if (eyeSkin == null || !actor.Face.HasEyeGaze)
                { Finish(false, "World human is missing the authored eye mesh/gaze channels."); yield break; }
                int eyeOut = HumanFaceShape(eyeSkin, "eyeLookOutRight");
                result.eyeInLeftWeight = eyeSkin.GetBlendShapeWeight(HumanFaceShape(eyeSkin, "eyeLookInLeft"));
                result.eyeOutRightWeight = eyeSkin.GetBlendShapeWeight(eyeOut);
                result.eyeOpposedWeight = Mathf.Max(eyeSkin.GetBlendShapeWeight(HumanFaceShape(eyeSkin, "eyeLookOutLeft")),
                    eyeSkin.GetBlendShapeWeight(HumanFaceShape(eyeSkin, "eyeLookInRight")));
                result.eyeVertexMeters = HumanFaceVertexDelta(eyeSkin, eyeOut, actor.Visual.transform.right, out result.eyeRightMeanMeters);
                result.eyeDirectionCorrect = result.eyeRightMeanMeters > .0001f;
                result.eyeGaze = result.eyeInLeftWeight > 5 && result.eyeInLeftWeight <= 100.01f &&
                    result.eyeOutRightWeight > 5 && result.eyeOutRightWeight <= 100.01f &&
                    result.eyeOpposedWeight < .1f && result.eyeVertexMeters > .0001f;
                if (!result.gazeBounded || !result.eyeGaze || !result.eyeDirectionCorrect || !CaptureRenderedFrame(game, Path.Combine(folder, "face-gaze-world.png"), out captureError))
                { Finish(false, "World head and neck did not track the existing view within safe limits: " + captureError); yield break; }
                view.transform.rotation = Quaternion.LookRotation(actor.Visual.transform.forward, Vector3.up);
                game.Vitals.Health.Restore(Mathf.Max(20, game.Vitals.Health.Current));
                if (!game.Vitals.ApplyDamage(5)) { Finish(false, "Isolated facial damage feedback was rejected."); yield break; }
                yield return new WaitForSecondsRealtime(.2f); yield return new WaitForEndOfFrame();
                int brow = HumanFaceShape(skin, "browDownLeft");
                result.hurtVertexMeters = HumanFaceVertexDelta(skin, brow);
                result.worldDamage = actor.Face.Mood == CityFaceMood.Hurt && skin.GetBlendShapeWeight(brow) > 5 && result.hurtVertexMeters > .0001f;
                if (!result.worldDamage) { Finish(false, "Actual damage event did not deform the human hurt expression."); yield break; }
                keys = scope.CreateKeyboard();
                Cursor.lockState = CursorLockMode.Locked; Cursor.visible = false;
                yield return HumanPress(keys, Key.P);
                if (!game.Director.IsPaused) { Finish(false, "Facial pause test did not open the phone."); yield break; }
                yield return new WaitForEndOfFrame();
                Quaternion pausedHead = head.localRotation;
                float[] pausedWeights = HumanFaceWeights(skin);
                yield return new WaitForSecondsRealtime(.35f); yield return new WaitForEndOfFrame();
                result.pauseHeadDegrees = Quaternion.Angle(pausedHead, head.localRotation);
                for (int i = 0; i < pausedWeights.Length; i++) result.pauseMorphDelta = Mathf.Max(result.pauseMorphDelta, Mathf.Abs(pausedWeights[i] - skin.GetBlendShapeWeight(i)));
                result.pauseStable = game.Director.IsPaused && result.pauseHeadDegrees < .01f && result.pauseMorphDelta < .01f;
                yield return HumanPress(keys, Key.Escape);
                if (!result.pauseStable || game.Director.IsPaused)
                { Finish(false, "Phone pause advanced facial bones/morphs or failed to restore control."); yield break; }
                rig.SetMode(CityCameraMode.FirstPerson); yield return null;
                bool hidden = !actor.IsVisible;
                yield return new WaitForSecondsRealtime(1.7f);
                rig.SetMode(CityCameraMode.Chase); yield return null; yield return new WaitForEndOfFrame();
                result.hiddenRecovery = hidden && actor.IsVisible && actor.Face.Mood != CityFaceMood.Hurt;
                game.Audio.PlayCue(CityAudioCue.Success);
                yield return new WaitForSecondsRealtime(.25f); yield return new WaitForEndOfFrame();
                smile = HumanFaceShape(skin, "mouthSmileLeft");
                result.worldSuccess = actor.Face.Mood == CityFaceMood.Success && skin.GetBlendShapeWeight(smile) > 5 && HumanFaceVertexDelta(skin, smile) > .0001f;
                view.transform.rotation = Quaternion.LookRotation(-actor.Visual.transform.forward, Vector3.up);
                yield return new WaitForSecondsRealtime(.2f); yield return new WaitForEndOfFrame();
                if (!result.hiddenRecovery || !result.worldSuccess || !CaptureRenderedFrame(game, Path.Combine(folder, "face-success-world.png"), out captureError))
                { Finish(false, "Hidden-state decay or real success cue did not restore the authored facial state: " + captureError); yield break; }
                result.ownersUnchanged = scope.OwnersUnchanged;
                result.controlRestored = !game.Director.IsPaused && !runtime.Wardrobe.IsOpen && runtime.CanSimulate;
                if (!result.ownersUnchanged || !result.controlRestored || game.Session.Cash != cash || runtime.Life.Experience != xp)
                { Finish(false, "Facial presentation changed owners, rewards or pause ownership."); yield break; }
                result.passed = true; humanFaceVerified = true;
                Debug.Log("VKT_HUMAN_FACE_VERIFIED " + JsonUtility.ToJson(result));
            }
            finally
            {
                try { runtime.Wardrobe.Close(); progress.ClosePanel(); game.Vitals.Health.Restore(healthBefore); }
                finally { scope.Dispose(); }
                File.WriteAllText(Path.Combine(folder, "human-face.json"), JsonUtility.ToJson(result, true));
            }
        }

        private static SkinnedMeshRenderer HumanFaceEyes(GameObject root)
        {
            foreach (var skin in root.GetComponentsInChildren<SkinnedMeshRenderer>(true))
                if (skin.name.IndexOf("low-poly", StringComparison.OrdinalIgnoreCase) >= 0 &&
                    HumanFaceShape(skin, "eyeLookOutLeft") >= 0 && HumanFaceShape(skin, "eyeLookInRight") >= 0 &&
                    HumanFaceShape(skin, "eyeLookInLeft") >= 0 && HumanFaceShape(skin, "eyeLookOutRight") >= 0) return skin;
            return null;
        }
        private static bool HumanFaceHasChannels(SkinnedMeshRenderer skin)
        {
            foreach (string name in new[] { "eyeBlinkLeft", "eyeBlinkRight", "jawOpen", "mouthSmileLeft", "mouthSmileRight", "browDownLeft", "browDownRight" })
                if (HumanFaceShape(skin, name) < 0) return false;
            return true;
        }
        private static bool HumanFaceChooseMood(CityLifestyleRuntime runtime, CityFaceMood mood)
        {
            var button = HumanButton("IZRAZ LICA");
            if (button == null || !button.interactable) return false;
            for (int i = 0; i < 5 && runtime.Wardrobe.PreviewMood != mood; i++) button.onClick.Invoke();
            return runtime.Wardrobe.PreviewMood == mood;
        }
        private static float[] HumanFaceWeights(SkinnedMeshRenderer skin)
        {
            var result = new float[skin.sharedMesh.blendShapeCount];
            for (int i = 0; i < result.Length; i++) result[i] = skin.GetBlendShapeWeight(i);
            return result;
        }
        // Same-frame counterfactual: bone transforms and all other channels remain identical.
        private static float HumanFaceVertexDelta(SkinnedMeshRenderer skin, int channel)
            => HumanFaceVertexDelta(skin, channel, Vector3.zero, out _);

        private static float HumanFaceVertexDelta(SkinnedMeshRenderer skin, int channel, Vector3 worldAxis, out float signedMean)
        {
            signedMean = 0;
            if (channel < 0) return 0;
            Mesh source = skin.sharedMesh;
            var delta = new Vector3[source.vertexCount];
            source.GetBlendShapeFrameVertices(channel, source.GetBlendShapeFrameCount(channel) - 1, delta, null, null);
            var posed = new Mesh(); var neutral = new Mesh();
            float weight = skin.GetBlendShapeWeight(channel);
            try
            {
                skin.BakeMesh(posed, true);
                skin.SetBlendShapeWeight(channel, 0); skin.BakeMesh(neutral, true);
                Vector3[] a = posed.vertices, b = neutral.vertices;
                float maximum = 0, projectedTotal = 0;
                int affected = 0;
                Vector3 axis = worldAxis.normalized;
                for (int i = 0; i < delta.Length; i++)
                    if (delta[i].sqrMagnitude > 1e-12f)
                    {
                        Vector3 worldDelta = skin.transform.TransformVector(a[i] - b[i]);
                        maximum = Mathf.Max(maximum, worldDelta.magnitude);
                        projectedTotal += Vector3.Dot(worldDelta, axis); affected++;
                    }
                signedMean = affected > 0 ? projectedTotal / affected : 0;
                return maximum;
            }
            finally
            {
                skin.SetBlendShapeWeight(channel, weight); Destroy(posed); Destroy(neutral);
            }
        }
    }
}
#endif
