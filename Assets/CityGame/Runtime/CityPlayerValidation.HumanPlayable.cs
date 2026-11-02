#if DEVELOPMENT_BUILD || UNITY_EDITOR
using System;
using System.Collections;
using System.IO;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.UI;

namespace Pozarevac.CityGame
{
    public sealed partial class CityPlayerValidation
    {
        private bool humanPlayableVerified;
        [Serializable] private sealed class HumanPlayableReport
        {
            public bool passed, uiSelection, profileSelectionUnchanged, walking, sprinting, movementWithShift, bothKnees,
                contralateral, pauseStable, teleportReset, stopSettles, firstPersonHidden, shoulderVisible,
                cachedSelection, ownersUnchanged, flatGroundContact, benchFallback, boxingHuman, boxingGuard, boxingPunch, boxingPause, boxingCompletedOnce, fallbackRestored,
                originalHiddenWalking, originalHiddenFirstPerson;
            public float walkMeters, sprintMeters, leftKneeDegrees, rightKneeDegrees, peakSpeed,
                pausedLeftDegrees, pausedRightDegrees, leftSoleGap, rightSoleGap,
                flatWalkObservedSpeed, flatSprintObservedSpeed, flatWalkPhysicalSpeed, flatSprintPhysicalSpeed;
            public int sampledFrames, oppositeSamples, incorrectPairs, supportedFeet;
        }

        private IEnumerator VerifyHumanPlayable(CityGameBootstrap game)
        {
            var result = new HumanPlayableReport();
            var runtime = game.GetComponent<CityLifestyleRuntime>();
            var progress = game.GetComponent<CityProgressionController>();
            var rig = game.Player.GetComponentInChildren<CityCameraRig>();
            var body = game.Player.GetComponent<CharacterController>();
            var mover = game.Player.GetComponent<CityPlayerController>();
            Keyboard keys = null;
            GameObject support = null;
            Mesh shoes = null;
            Vector3 beforeGround = Vector3.zero;
            bool groundFixtureActive = false, moverWasEnabled = mover.enabled;
            string folder = Argument("-vktHumanPlayableCaptures", "");
            if (folder.Length == 0) { Finish(false, "Human playable capture folder missing."); yield break; }
            Directory.CreateDirectory(folder);
            var scope = new HumanValidationScope(mover);
            try
            {
                progress.ClosePanel(); game.GetComponent<CityMusicPanel>()?.Close(); runtime.CloseMenu();
                if (game.Hideout.IsInside) game.Hideout.Travel(false);
                mover.enabled = true; mover.ResetView();
                if (!runtime.Wardrobe.Open() || !runtime.Wardrobe.ToggleCandidate())
                { Finish(false, "Could not open human selection in the wardrobe."); yield break; }
                var checkpoint = new HumanProfileCheckpoint(game);
                Button play = HumanButton("IGRAJ U GRADU");
                if (play == null || !play.interactable) { Finish(false, "Playable human UI choice missing."); yield break; }
                play.onClick.Invoke();
                result.profileSelectionUnchanged = checkpoint.IsUnchanged;
                yield return null; yield return null;
                var actor = rig.HumanActor;
                result.uiSelection = rig.HumanSelected && actor != null && actor.IsVisible && !runtime.Wardrobe.IsOpen && !game.Director.IsPaused && rig.Mode == CityCameraMode.Chase;
                if (!result.uiSelection || !result.profileSelectionUnchanged)
                { Finish(false, "Human selection changed progression, failed to close the wardrobe or left controls paused."); yield break; }
                GameObject identity = actor.Visual;
                Transform originalVisual = game.Player.Find("VKT_PlayerVisual");
                Renderer[] originalRenderers = originalVisual != null ? originalVisual.GetComponentsInChildren<Renderer>(true) : Array.Empty<Renderer>();
                result.originalHiddenWalking = originalRenderers.Length > 0 && HumanRenderersHidden(originalRenderers);
                if (!result.originalHiddenWalking) { Finish(false, "Original avatar renderer missing or still active beside the human."); yield break; }
                Transform[] bones = identity.GetComponentsInChildren<Transform>(true);
                Transform left = Array.Find(bones, b => b.name == "calf_l"), right = Array.Find(bones, b => b.name == "calf_r");
                Transform leftHip = Array.Find(bones, b => b.name == "thigh_l"), rightHip = Array.Find(bones, b => b.name == "thigh_r");
                Transform leftArm = Array.Find(bones, b => b.name == "upperarm_l"), rightArm = Array.Find(bones, b => b.name == "upperarm_r");
                Transform leftElbow = Array.Find(bones, b => b.name == "lowerarm_l"), rightElbow = Array.Find(bones, b => b.name == "lowerarm_r");
                if (left == null || right == null || leftHip == null || rightHip == null || leftArm == null || rightArm == null || leftElbow == null || rightElbow == null)
                { Finish(false, "Playable human limb geometry missing."); yield break; }
                keys = scope.CreateKeyboard();
                Cursor.lockState = CursorLockMode.Locked; Cursor.visible = false;
                Key walkKey = Key.W;
                for (int attempt = 0; attempt < 2 && !result.walking; attempt++)
                {
                    walkKey = attempt == 0 ? Key.W : Key.S;
                    Vector3 start = game.Player.position;
                    Quaternion initialLeft = left.localRotation, initialRight = right.localRotation;
                    InputSystem.QueueStateEvent(keys, new KeyboardState(walkKey));
                    float until = Time.realtimeSinceStartup + 1.3f;
                    while (Time.realtimeSinceStartup < until)
                    {
                        yield return new WaitForEndOfFrame(); result.sampledFrames++;
                        result.originalHiddenWalking &= HumanRenderersHidden(originalRenderers);
                        result.leftKneeDegrees = Mathf.Max(result.leftKneeDegrees, Quaternion.Angle(initialLeft, left.localRotation));
                        result.rightKneeDegrees = Mathf.Max(result.rightKneeDegrees, Quaternion.Angle(initialRight, right.localRotation));
                        result.peakSpeed = Mathf.Max(result.peakSpeed, actor.ObservedSpeed);
                        Vector3 forward = actor.Visual.transform.forward;
                        float arms = Vector3.Dot((leftElbow.position - leftArm.position).normalized - (rightElbow.position - rightArm.position).normalized, forward);
                        float legs = Vector3.Dot((left.position - leftHip.position).normalized - (right.position - rightHip.position).normalized, forward);
                        if (Mathf.Abs(arms) > .12f && Mathf.Abs(legs) > .12f)
                        { result.oppositeSamples++; if (arms * legs >= 0) result.incorrectPairs++; }
                        result.walkMeters = Mathf.Max(result.walkMeters, HumanFlatDistance(start, game.Player.position));
                    }
                    InputSystem.QueueStateEvent(keys, new KeyboardState()); yield return null; yield return null;
                    result.walking = result.walkMeters > 1 && actor.IsVisible && !game.Director.IsPaused;
                }
                result.bothKnees = result.leftKneeDegrees > 8 && result.rightKneeDegrees > 8;
                result.contralateral = result.oppositeSamples >= 5 && result.incorrectPairs == 0;
                if (!result.walking || !result.bothKnees || !result.contralateral || !result.originalHiddenWalking)
                { Finish(false, "Human physical walk, two-knee motion or opposite arm/leg pairing failed: " + JsonUtility.ToJson(result)); yield break; }
                if (!CaptureRenderedFrame(game, Path.Combine(folder, "human-city-walk.png"), out string captureError))
                { Finish(false, captureError); yield break; }
                Vector3 sprintStart = game.Player.position;
                InputSystem.QueueStateEvent(keys, new KeyboardState(walkKey == Key.W ? Key.S : Key.W, Key.LeftShift));
                yield return new WaitForSecondsRealtime(1f);
                InputSystem.QueueStateEvent(keys, new KeyboardState()); yield return null; yield return new WaitForEndOfFrame();
                result.sprintMeters = HumanFlatDistance(sprintStart, game.Player.position);
                // The city route can end at an obstacle; compare actual sprint speeds on the clear fixture below.
                result.movementWithShift = result.sprintMeters > 1 && actor.ObservedSpeed > .2f;
                if (!result.movementWithShift) { Finish(false, "Shift movement did not move the physical player along the city route."); yield break; }
                yield return HumanPress(keys, Key.P);
                if (!game.Director.IsPaused) { Finish(false, "Phone input did not acquire pause ownership."); yield break; }
                yield return new WaitForEndOfFrame();
                Quaternion pausedLeft = left.localRotation, pausedRight = right.localRotation;
                Vector3 pausedPosition = game.Player.position;
                yield return new WaitForSecondsRealtime(.3f); yield return new WaitForEndOfFrame();
                result.pausedLeftDegrees = Quaternion.Angle(pausedLeft, left.localRotation);
                result.pausedRightDegrees = Quaternion.Angle(pausedRight, right.localRotation);
                result.pauseStable = game.Director.IsPaused && result.pausedLeftDegrees < .5f && result.pausedRightDegrees < .5f && HumanFlatDistance(pausedPosition, game.Player.position) < .001f;
                yield return HumanPress(keys, Key.Escape);
                if (!result.pauseStable || game.Director.IsPaused) { Finish(false, "Phone pause changed human pose or failed to restore control."); yield break; }
                Vector3 beforeTeleport = game.Player.position;
                body.enabled = false; game.Player.position += Vector3.right * 40; body.enabled = true; Physics.SyncTransforms();
                yield return new WaitForEndOfFrame();
                result.teleportReset = actor.ObservedSpeed < .001f && actor.Weight < .001f;
                body.enabled = false; game.Player.position = beforeTeleport; body.enabled = true; Physics.SyncTransforms();
                yield return null; yield return null;
                InputSystem.QueueStateEvent(keys, new KeyboardState(Key.W)); yield return new WaitForSecondsRealtime(.25f);
                InputSystem.QueueStateEvent(keys, new KeyboardState()); yield return new WaitForSecondsRealtime(.8f);
                result.stopSettles = actor.ObservedSpeed < .1f && actor.Weight < .01f;
                if (!result.teleportReset || !result.stopSettles) { Finish(false, "Human motion did not reset after teleport or settle after stopping."); yield break; }
                rig.SetMode(CityCameraMode.FirstPerson); yield return null; yield return null;
                result.originalHiddenFirstPerson = HumanRenderersHidden(originalRenderers);
                result.firstPersonHidden = rig.HumanSelected && !actor.IsVisible && result.originalHiddenFirstPerson;
                rig.SetMode(CityCameraMode.Shoulder); yield return null; yield return null;
                result.shoulderVisible = actor.IsVisible;
                rig.SetMode(CityCameraMode.Chase); yield return null; yield return null;
                if (!result.firstPersonHidden || !result.shoulderVisible || !actor.IsVisible) { Finish(false, "Human camera visibility failed."); yield break; }

                // Controlled flat support verifies actual baked shoe vertices, separate from street movement.
                beforeGround = game.Player.position; moverWasEnabled = mover.enabled; groundFixtureActive = true;
                support = GameObject.CreatePrimitive(PrimitiveType.Cube); support.name = "VKT_HumanFlatValidation";
                support.transform.position = beforeGround + Vector3.up * 40; support.transform.localScale = new Vector3(20, .2f, 20);
                mover.enabled = false; body.enabled = false; game.Player.position = support.transform.position + Vector3.up * .3f; body.enabled = true; Physics.SyncTransforms();
                for (int i = 0; i < 50; i++) { body.Move(Vector3.down * .04f); yield return null; yield return new WaitForEndOfFrame(); }
                var shoeSkin = Array.Find(identity.GetComponentsInChildren<SkinnedMeshRenderer>(true), skin => skin.name.IndexOf("shoes02", StringComparison.OrdinalIgnoreCase) >= 0);
                if (shoeSkin == null) { Finish(false, "Human shoe geometry missing for flat-ground measurement."); yield break; }
                shoes = new Mesh(); shoeSkin.BakeMesh(shoes, true);
                float leftSole = float.PositiveInfinity, rightSole = float.PositiveInfinity;
                foreach (Vector3 vertex in shoes.vertices)
                {
                    Vector3 world = shoeSkin.transform.TransformPoint(vertex);
                    if (identity.transform.InverseTransformPoint(world).x < 0) leftSole = Mathf.Min(leftSole, world.y);
                    else rightSole = Mathf.Min(rightSole, world.y);
                }
                float floor = support.transform.position.y + .1f;
                result.leftSoleGap = leftSole - floor; result.rightSoleGap = rightSole - floor; result.supportedFeet = actor.SupportedFeet;
                result.flatGroundContact = body.isGrounded && actor.SupportedFeet == 2 && float.IsFinite(leftSole) && float.IsFinite(rightSole) && Mathf.Abs(result.leftSoleGap) < .07f && Mathf.Abs(result.rightSoleGap) < .07f;
                if (!CaptureRenderedFrame(game, Path.Combine(folder, "human-flat-ground.png"), out captureError)) { Finish(false, captureError); yield break; }
                if (!result.flatGroundContact) { Finish(false, "Human flat-ground shoe measurement failed: " + JsonUtility.ToJson(result)); yield break; }

                // The raised fixture gives both measurements an unobstructed flat route and the same real controller.
                mover.enabled = true; mover.ResetView(); Cursor.lockState = CursorLockMode.Locked; Cursor.visible = false;
                for (int mode = 0; mode < 2; mode++)
                {
                    Vector3 start = game.Player.position;
                    float started = Time.realtimeSinceStartup, observedTotal = 0;
                    int speedSamples = 0;
                    InputSystem.QueueStateEvent(keys, mode == 0 ? new KeyboardState(Key.W) : new KeyboardState(Key.S, Key.LeftShift));
                    while (Time.realtimeSinceStartup - started < .8f)
                    {
                        yield return new WaitForEndOfFrame();
                        if (Time.realtimeSinceStartup - started >= .2f) { observedTotal += actor.ObservedSpeed; speedSamples++; }
                    }
                    InputSystem.QueueStateEvent(keys, new KeyboardState());
                    float elapsed = Time.realtimeSinceStartup - started;
                    float physicalSpeed = HumanFlatDistance(start, game.Player.position) / Mathf.Max(.001f, elapsed);
                    float observedSpeed = speedSamples > 0 ? observedTotal / speedSamples : 0;
                    if (mode == 0) { result.flatWalkPhysicalSpeed = physicalSpeed; result.flatWalkObservedSpeed = observedSpeed; }
                    else { result.flatSprintPhysicalSpeed = physicalSpeed; result.flatSprintObservedSpeed = observedSpeed; }
                    if (speedSamples < 3) { Finish(false, "Too few rendered samples to verify flat walk/sprint speed."); yield break; }
                    yield return new WaitForSecondsRealtime(.4f); yield return new WaitForEndOfFrame();
                }
                result.sprinting = result.flatWalkPhysicalSpeed > 3 && result.flatWalkObservedSpeed > 3 &&
                    result.flatSprintPhysicalSpeed > result.flatWalkPhysicalSpeed * 1.2f &&
                    result.flatSprintObservedSpeed > result.flatWalkObservedSpeed * 1.2f;
                if (!result.sprinting) { Finish(false, "Shift did not increase physical and observed speed on the clear flat route: " + JsonUtility.ToJson(result)); yield break; }
                body.enabled = false; game.Player.position = beforeGround; body.enabled = true; mover.enabled = moverWasEnabled; mover.ResetView(); Destroy(support); support = null; groundFixtureActive = false; Physics.SyncTransforms();
                yield return null; yield return null;

                // Exercise the authored wardrobe route in both directions; do not allocate a second player.
                if (!runtime.Wardrobe.Open()) { Finish(false, "Wardrobe did not reopen after human movement."); yield break; }
                Button original = HumanButton("VRATI ORIGINALNI LIK");
                if (original == null) { Finish(false, "Original-avatar UI choice missing."); yield break; }
                original.onClick.Invoke(); yield return null; yield return null;
                bool originalRestored = !rig.HumanSelected && !actor.IsVisible;
                if (!runtime.Wardrobe.Open() || !runtime.Wardrobe.ToggleCandidate()) { Finish(false, "Human choice did not reopen."); yield break; }
                play = HumanButton("IGRAJ U GRADU");
                if (play == null) { Finish(false, "Human UI choice disappeared after toggling."); yield break; }
                play.onClick.Invoke(); yield return null; yield return null;
                result.cachedSelection = originalRestored && rig.HumanSelected && ReferenceEquals(actor, rig.HumanActor) && actor.Visual == identity && actor.IsVisible;
                if (!result.cachedSelection) { Finish(false, "Avatar toggles lost selection or allocated another human model."); yield break; }
                int cash = game.Session.Cash, xp = runtime.Life.Experience;
                runtime.Life.Tick(91, false); // Isolated smoke profile: clear the shared workout rest.
                body.enabled = false; game.Player.position = runtime.Gym.position + Vector3.back * 2; body.enabled = true; Physics.SyncTransforms(); yield return null; yield return null;
                if (!runtime.StartWorkout()) { Finish(false, "Could not start bench fallback."); yield break; }
                yield return null; yield return new WaitForEndOfFrame();
                Transform trainee = runtime.BenchVisual.transform.Find("BenchAthlete");
                result.benchFallback = rig.HumanSelected && !actor.IsVisible && trainee != null && trainee.gameObject.activeInHierarchy && runtime.BenchVisual.RigReady && runtime.BenchVisual.FeetReady;
                if (!CaptureRenderedFrame(game, Path.Combine(folder, "human-bench-fallback.png"), out captureError)) { Finish(false, captureError); yield break; }
                runtime.Life.CancelWorkout(); yield return null; yield return null;
                result.fallbackRestored = actor.IsVisible && rig.HumanSelected;
                runtime.Life.Tick(91, false); rig.SetMode(CityCameraMode.FirstPerson);
                Transform finger=Array.Find(bones,bone=>bone.name=="index_02_r"), wrist=Array.Find(bones,bone=>bone.name=="hand_r");
                Quaternion resting=finger.localRotation;
                int sessions=runtime.Life.BoxingSessions;
                if (!runtime.StartBoxing()) { Finish(false, "Could not start human boxing."); yield break; }
                yield return null; yield return new WaitForEndOfFrame();
                result.boxingHuman=rig.HumanSelected&&actor.IsVisible&&HumanRenderersHidden(originalRenderers)&&actor.IsBoxing;
                result.boxingGuard=Quaternion.Angle(resting,finger.localRotation)>45;
                Quaternion guard=finger.localRotation;Vector3 handStart=wrist.position;
                double roundClock=runtime.Boxing.RoundTime;
                game.Director.SetPaused(true);yield return new WaitForSecondsRealtime(.2f);yield return new WaitForEndOfFrame();
                result.boxingPause=runtime.Boxing.RoundTime==roundClock&&Quaternion.Angle(guard,finger.localRotation)<.01f;
                game.Director.SetPaused(false);
                float deadline=Time.realtimeSinceStartup+25;
                bool captured=false;
                while(runtime.Life.IsBoxing&&Time.realtimeSinceStartup<deadline)
                {
                    if(runtime.Boxing.WindowOpen)yield return HumanPress(keys,runtime.Boxing.Expected==CityBoxingAction.Jab?Key.J:Key.K);
                    yield return new WaitForEndOfFrame();
                    if(runtime.Life.IsBoxing)
                    {
                        result.boxingPunch|=Vector3.Dot(wrist.position-handStart,game.Player.forward)>.08f;
                        result.boxingHuman&=actor.IsVisible&&HumanRenderersHidden(originalRenderers);
                    }
                    if(!captured&&runtime.Boxing.Punch>.7f)
                    {
                        if(!CaptureRenderedFrame(game,Path.Combine(folder,"human-boxing-training.png"),out captureError)){Finish(false,captureError);yield break;}
                        captured=true;
                    }
                }
                result.boxingPunch&=captured;
                result.boxingCompletedOnce=runtime.Life.BoxingSessions==sessions+1&&runtime.Life.Experience==xp+30&&!runtime.Boxing.TryClaim(runtime.Life);
                if(runtime.Life.IsBoxing)runtime.CancelBoxing();
                yield return null;yield return null;
                result.fallbackRestored &= rig.Mode == CityCameraMode.FirstPerson && !actor.IsVisible;
                rig.SetMode(CityCameraMode.Chase); yield return null; yield return null;
                result.fallbackRestored &= actor.IsVisible && rig.HumanSelected;
                result.ownersUnchanged = scope.OwnersUnchanged;
                if (!result.benchFallback || !result.boxingHuman || !result.boxingGuard || !result.boxingPunch || !result.boxingPause || !result.boxingCompletedOnce || !result.fallbackRestored || !result.ownersUnchanged || game.Session.Cash != cash || runtime.Life.Experience != xp+30)
                { Finish(false, "Human training fallback, state restoration, owners or rewards failed: " + JsonUtility.ToJson(result)); yield break; }
                result.passed = true; humanPlayableVerified = true;
                Debug.Log("VKT_HUMAN_PLAYABLE_VERIFIED " + JsonUtility.ToJson(result));
            }
            finally
            {
                try
                {
                    if (groundFixtureActive) { body.enabled = false; game.Player.position = beforeGround; body.enabled = true; mover.enabled = moverWasEnabled; Physics.SyncTransforms(); }
                    if (support != null) Destroy(support); if (shoes != null) Destroy(shoes);
                    runtime.Wardrobe.Close(); progress.ClosePanel();
                }
                finally { scope.Dispose(); }
                File.WriteAllText(Path.Combine(folder, "human-playable.json"), JsonUtility.ToJson(result, true));
            }
        }

        private static bool HumanRenderersHidden(Renderer[] renderers)
        {
            foreach (Renderer renderer in renderers)
                if (renderer != null && renderer.enabled && renderer.gameObject.activeInHierarchy) return false;
            return true;
        }
        private static float HumanFlatDistance(Vector3 a, Vector3 b) => new Vector2(a.x - b.x, a.z - b.z).magnitude;
    }
}
#endif
