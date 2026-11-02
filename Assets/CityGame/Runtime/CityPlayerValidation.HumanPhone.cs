#if DEVELOPMENT_BUILD || UNITY_EDITOR
using System;
using System.Collections;
using System.IO;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace Pozarevac.CityGame
{
    public sealed partial class CityPlayerValidation
    {
        private bool humanPhoneVerified;
        [Serializable] private sealed class HumanPhoneReport
        {
            public bool passed, raised, grip, pausedWorld, buttonTap, cameraModes, closeRestores,
                ownersUnchanged, controlRestored, reuse, noPhysics, profileUnchanged, uiFits,
                narrowFallback, occlusionFallback, regionalRelief;
            public float gripMeters, tapMeters = 100, maxPausedMovement, maxPausedLegDegrees, maxViewRestoreDegrees, maxTapWristDegrees;
            public int modes;
        }
        private IEnumerator VerifyHumanPhone(CityGameBootstrap game)
        {
            var result = new HumanPhoneReport();
            var life = game.GetComponent<CityLifestyleRuntime>();
            var progress = game.GetComponent<CityProgressionController>();
            var phone = game.GetComponentInChildren<CityPhonePanel>();
            var rig = game.Player.GetComponentInChildren<CityCameraRig>();
            var mover = game.Player.GetComponent<CityPlayerController>();
            var body = game.Player.GetComponent<CharacterController>();
            Camera view = game.Player.GetComponentInChildren<Camera>();
            string folder = Argument("-vktHumanPhoneCaptures", "");
            if (folder.Length == 0) { Finish(false, "Human phone captures missing."); yield break; }
            Directory.CreateDirectory(folder);
            var scope = new HumanValidationScope(mover);
            Vector3 positionBefore = game.Player.position;
            CityCameraMode modeBefore = rig.Mode; bool selectedBefore = rig.HumanSelected;
            GameObject support = null, blocker = null;
            try
            {
                progress.ClosePanel(); life.CloseMenu(); game.GetComponent<CityMusicPanel>()?.Close();
                if (game.Hideout.IsInside) game.Hideout.Travel(false);
                if (!rig.HumanSelected)
                {
                    if (!life.Wardrobe.Open() || !life.Wardrobe.ToggleCandidate())
                    { Finish(false, "Human phone wardrobe unavailable."); yield break; }
                    HumanButton("IGRAJ U GRADU").onClick.Invoke(); yield return null; yield return null;
                }
                CityHumanActor actor = rig.HumanActor;
                if (actor == null || !actor.PhoneAvailable) { Finish(false, "Phone hand rig unavailable."); yield break; }
                Transform[] phoneBones = actor.Visual.GetComponentsInChildren<Transform>(true);
                Transform handR = Array.Find(phoneBones, bone => bone.name == "hand_r"), forearmR = Array.Find(phoneBones, bone => bone.name == "lowerarm_r");
                Transform indexR2 = Array.Find(phoneBones, bone => bone.name == "index_02_r"), indexR3 = Array.Find(phoneBones, bone => bone.name == "index_03_r");
                // A real controller on isolated static support removes unrelated street camera obstructions.
                support = GameObject.CreatePrimitive(PrimitiveType.Cube); support.name = "VKT_PhoneSupportFixture";
                support.transform.position = game.Player.position + Vector3.up * 40;
                support.transform.localScale = new Vector3(12, .2f, 12); support.GetComponent<Renderer>().enabled = false;
                mover.enabled = false; body.enabled = false; game.Player.position = support.transform.position + Vector3.up * .14f;
                body.enabled = true; mover.enabled = true; mover.ResetView(); Physics.SyncTransforms();
                game.Atmosphere.ConfigureCycle(12, 12, 15);
                yield return new WaitForSecondsRealtime(.7f);
                Keyboard keys = scope.CreateKeyboard();
                bool portrait = Screen.width / (float)Screen.height < 1.5f;
                result.profileUnchanged = result.uiFits = true;
                if (portrait)
                {
                    var opening = new HumanProfileCheckpoint(game);
                    progress.OpenPanel(); result.profileUnchanged &= opening.IsUnchanged;
                    yield return new WaitForSecondsRealtime(.4f); yield return new WaitForEndOfFrame();
                    result.narrowFallback = game.Director.IsPaused && !rig.PhonePresentationActive && !actor.PhoneActive;
                    result.uiFits = PhoneRectFits(phone.transform.Find("PhonePanel").GetComponent<RectTransform>());
                    if (!CaptureRenderedFrame(game, Path.Combine(folder, "phone-portrait.png"), out string portraitError))
                    { Finish(false, portraitError); yield break; }
                    var closing = new HumanProfileCheckpoint(game);
                    progress.ClosePanel(); result.profileUnchanged &= closing.IsUnchanged;
                    yield return null; yield return null;
                    result.controlRestored = !game.Director.IsPaused && !phone.IsOpen;
                }
                else
                {
                    result.raised = result.grip = result.pausedWorld = result.cameraModes = result.closeRestores = result.reuse = result.noPhysics = result.buttonTap = true;
                    GameObject identity = null;
                    for (int mode = 0; mode < 3; mode++)
                    {
                        rig.SetMode((CityCameraMode)mode); yield return null; yield return null;
                        Quaternion rotation = view.transform.localRotation;
                        yield return HumanPress(keys, Key.P);
                        float deadline = Time.realtimeSinceStartup + 3;
                        while (Time.realtimeSinceStartup < deadline && (!actor.PhoneActive || actor.PhoneWeight < .999f)) yield return null;
                        yield return new WaitForEndOfFrame();
                        result.raised &= rig.PhonePresentationActive && actor.PhoneActive && actor.PhoneVisual != null && actor.PhoneVisual.activeSelf && actor.IsVisible;
                        if (!result.raised) { Finish(false, "Phone did not raise through actual P input."); yield break; }
                        if (identity == null) identity = actor.PhoneVisual; else result.reuse &= ReferenceEquals(identity, actor.PhoneVisual);
                        result.gripMeters = Mathf.Max(result.gripMeters, actor.PhoneGripDistance);
                        result.grip &= actor.PhoneGripDistance < .014f;
                        result.noPhysics &= actor.PhoneVisual.GetComponentsInChildren<Collider>(true).Length == 0 &&
                            actor.PhoneVisual.GetComponentsInChildren<Rigidbody>(true).Length == 0;
                        result.cameraModes &= rig.Mode == (CityCameraMode)mode;
                        result.uiFits &= PhoneRectFits(phone.transform.Find("PhonePanel").GetComponent<RectTransform>());
                        Transform calf = Array.Find(actor.Visual.GetComponentsInChildren<Transform>(), bone => bone.name == "calf_l");
                        Vector3 frozen = game.Player.position; Quaternion leg = calf.localRotation; float phase = actor.Phase;
                        yield return new WaitForSecondsRealtime(.3f); yield return new WaitForEndOfFrame();
                        result.maxPausedMovement = Mathf.Max(result.maxPausedMovement, Vector3.Distance(frozen, game.Player.position));
                        result.maxPausedLegDegrees = Mathf.Max(result.maxPausedLegDegrees, Quaternion.Angle(leg, calf.localRotation));
                        result.pausedWorld &= game.Director.IsPaused && Time.timeScale == 0 && actor.Phase == phase &&
                            result.maxPausedMovement < .001f && result.maxPausedLegDegrees < .01f && !game.Combat.TryAttack();
                        if (!CaptureRenderedFrame(game, Path.Combine(folder, "phone-mode-" + mode + ".png"), out string captureError))
                        { Finish(false, captureError); yield break; }
                        var checkpoint = new HumanProfileCheckpoint(game);
                        phone.transform.Find("PhonePanel/Map").GetComponent<Button>().onClick.Invoke();
                        result.profileUnchanged &= checkpoint.IsUnchanged;
                        float until = Time.realtimeSinceStartup + .8f; bool captured = false;
                        while (Time.realtimeSinceStartup < until)
                        {
                            yield return new WaitForEndOfFrame();
                            result.tapMeters = Mathf.Min(result.tapMeters, actor.PhoneTapDistance);
                            if (actor.PhoneTapDistance < .012f)
                                result.maxTapWristDegrees = Mathf.Max(result.maxTapWristDegrees, Vector3.Angle(handR.position - forearmR.position, indexR3.position - indexR2.position));
                            if (!captured && actor.PhoneTapDistance < .012f)
                            {
                                if (!CaptureRenderedFrame(game, Path.Combine(folder, "phone-tap.png"), out captureError))
                                { Finish(false, captureError); yield break; }
                                captured = true;
                            }
                        }
                        result.buttonTap &= captured && phone.transform.Find("MapPanel").gameObject.activeSelf;
                        result.buttonTap &= result.maxTapWristDegrees < 65;
                        if(mode==0)
                        {
                            CityMapView map=phone.transform.Find("MapPanel").GetComponent<CityMapView>();
                            Button toggle=map.transform.Find("ReliefToggle").GetComponent<Button>();
                            int interaction=phone.InteractionVersion;
                            var reliefOpen=new HumanProfileCheckpoint(game);
                            toggle.onClick.Invoke();result.profileUnchanged&=reliefOpen.IsUnchanged;
                            yield return new WaitForSecondsRealtime(.6f);yield return new WaitForEndOfFrame();
                            result.regionalRelief=map.ReliefActive&&phone.InteractionVersion==unchecked(interaction+1)&&game.Director.IsPaused;
                            if(!CaptureRenderedFrame(game,Path.Combine(folder,"regional-relief.png"),out captureError)){Finish(false,captureError);yield break;}
                            var reliefClose=new HumanProfileCheckpoint(game);
                            toggle.onClick.Invoke();result.profileUnchanged&=reliefClose.IsUnchanged;result.regionalRelief&=!map.ReliefActive;
                        }
                        yield return HumanPress(keys, Key.Escape); yield return null;
                        result.maxViewRestoreDegrees = Mathf.Max(result.maxViewRestoreDegrees, Quaternion.Angle(rotation, view.transform.localRotation));
                        result.closeRestores &= !rig.PhonePresentationActive && !actor.PhoneActive && !identity.activeSelf &&
                            !game.Director.IsPaused && !phone.IsOpen && rig.Mode == (CityCameraMode)mode && result.maxViewRestoreDegrees < .5f;
                        result.modes++;
                    }
                    Transform human = actor.Visual.transform;
                    Vector3 target = human.position + Vector3.up * 1.4f + human.forward * .05f;
                    blocker = GameObject.CreatePrimitive(PrimitiveType.Cube); blocker.name = "VKT_PhoneCameraBlocker";
                    blocker.transform.position = target + human.forward * .45f + human.right * .33f + Vector3.up * .08f;
                    blocker.transform.localScale = Vector3.one * .3f; blocker.GetComponent<Renderer>().enabled = false;
                    Physics.SyncTransforms(); progress.OpenPanel();
                    yield return new WaitForSecondsRealtime(.25f); yield return new WaitForEndOfFrame();
                    result.occlusionFallback = game.Director.IsPaused && !rig.PhonePresentationActive && !actor.PhoneActive;
                    progress.ClosePanel(); Destroy(blocker); blocker = null; yield return null; yield return null;
                    result.controlRestored = !game.Director.IsPaused && !phone.IsOpen && mover.enabled;
                }
                result.ownersUnchanged = scope.OwnersUnchanged;
                result.passed = result.ownersUnchanged && result.controlRestored && result.profileUnchanged && result.uiFits &&
                    (portrait ? result.narrowFallback : result.raised && result.grip && result.pausedWorld && result.buttonTap && result.cameraModes &&
                        result.closeRestores && result.reuse && result.noPhysics && result.occlusionFallback && result.regionalRelief && result.modes == 3);
                if (!result.passed) { Finish(false, "Human phone invariants failed: " + JsonUtility.ToJson(result)); yield break; }
                humanPhoneVerified = true; Debug.Log("VKT_HUMAN_PHONE_VERIFIED " + JsonUtility.ToJson(result));
            }
            finally
            {
                try
                {
                    progress.ClosePanel(); life.Wardrobe.Close(); rig.HumanActor?.SetPhoneActive(false);
                    rig.SelectHumanAvatar(selectedBefore); rig.SetMode(modeBefore);
                    if (blocker != null) Destroy(blocker); if (support != null) Destroy(support);
                    mover.enabled = false; body.enabled = false; game.Player.position = positionBefore; body.enabled = true; Physics.SyncTransforms();
                }
                finally { scope.Dispose(); }
                File.WriteAllText(Path.Combine(folder, "human-phone.json"), JsonUtility.ToJson(result, true));
            }
        }
        private static bool PhoneRectFits(RectTransform rect)
        {
            var corners = new Vector3[4]; rect.GetWorldCorners(corners);
            Rect safe = Screen.safeArea.width > 0 ? Screen.safeArea : new Rect(0, 0, Screen.width, Screen.height);
            return corners[0].x >= safe.xMin - 2 && corners[0].y >= safe.yMin - 2 && corners[2].x <= safe.xMax + 2 && corners[2].y <= safe.yMax + 2;
        }
    }
}
#endif
