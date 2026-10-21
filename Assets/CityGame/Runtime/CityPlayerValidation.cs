#if DEVELOPMENT_BUILD || UNITY_EDITOR
using System;
using System.Collections;
using System.IO;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace Pozarevac.CityGame
{
    // Explicit development-player verification. Batch profiles are isolated from personal saves.
    public sealed class CityPlayerValidation : MonoBehaviour
    {
        private bool lifestyleVerified;
        private bool routineVerified;
        private bool resumeBoxingVerified;
        [Serializable] private sealed class Report { public bool passed; public string details; public string product; }
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Install()
        {
            if (Array.IndexOf(Environment.GetCommandLineArgs(), "-vktInputFlow") >= 0) return;
            if (!Array.Exists(Environment.GetCommandLineArgs(), arg => arg == "-vktSmoke")) return;
            Application.runInBackground = true;
            Application.targetFrameRate = 30;
            new GameObject("VKTPlayerValidation").AddComponent<CityPlayerValidation>();
        }

        private IEnumerator Start()
        {
            float deadline = Time.realtimeSinceStartup + 90;
            CityGameBootstrap game = null;
            bool introFallback=Array.IndexOf(Environment.GetCommandLineArgs(),"-vktIntroFallbackSmoke")>=0;
            if(Array.IndexOf(Environment.GetCommandLineArgs(),"-vktIntroSmoke")>=0||introFallback)
            {
                float introDeadline=Time.realtimeSinceStartup+30;
                while(Time.realtimeSinceStartup<introDeadline)
                {
                    game=FindAnyObjectByType<CityGameBootstrap>();
                    if(game!=null&&game.WorldReady&&game.Intro!=null&&game.Intro.IsActive&&(introFallback||game.Intro.HasStarted&&game.Intro.DecodedFrame>=30))break;
                    yield return null;
                }
                if(game==null||!game.WorldReady||game.Intro==null||!game.Intro.IsActive||(!introFallback&&game.Intro.DecodedFrame<30)||!game.Director.IsPaused)
                {Finish(false,"Original intro did not decode frames with gameplay paused.");yield break;}
                string introCapture=Argument("-vktIntroScreenshot","");
                if(!string.IsNullOrEmpty(introCapture)&&!CaptureRenderedFrame(game,introCapture,out string introError))
                {Finish(false,introError);yield break;}
                Debug.Log("VKT_INTRO_VERIFIED frame="+game.Intro.DecodedFrame+" size="+game.Intro.Width+"x"+game.Intro.Height+" seconds="+game.Intro.Length+" audioTracks="+game.Intro.AudioTracks);
                bool natural=Array.IndexOf(Environment.GetCommandLineArgs(),"-vktIntroNaturalSmoke")>=0;
                if(natural)
                {
                    float end=Time.realtimeSinceStartup+65;
                    while(game.Intro!=null&&game.Intro.IsActive&&Time.realtimeSinceStartup<end)yield return null;
                    if(game.Intro!=null&&game.Intro.IsActive){Finish(false,"Original intro did not complete naturally.");yield break;}
                }
                else
                {
                    var skip=Array.Find(FindObjectsByType<UnityEngine.UI.Button>(FindObjectsSortMode.None),b=>b.name=="PRESKOČI UVOD");
                    if(skip==null){Finish(false,"Intro touch skip button absent.");yield break;}
                    skip.onClick.Invoke();yield return null;
                }
                if(game.Director.IsPaused){Finish(false,"Intro skip did not restore gameplay.");yield break;}
                if(natural||introFallback){Finish(true,natural?"Original intro decoded, played to natural completion and resumed gameplay.":"Missing-video title card and touch skip restored gameplay.");yield break;}
            }
            while (Time.realtimeSinceStartup < deadline)
            {
                game = FindAnyObjectByType<CityGameBootstrap>();
                if (game != null && game.WorldReady && (game.Intro == null || !game.Intro.IsActive)) break;
                yield return new WaitForSecondsRealtime(0.25f);
            }
            if (game == null || !game.WorldReady) { Finish(false, "City terrain did not become ready."); yield break; }
            yield return new WaitForSecondsRealtime(1);
            if (!Verify(game, out string details)) { Finish(false, details); yield break; }
            if(Array.IndexOf(Environment.GetCommandLineArgs(),"-vktAcousticsSmoke")>=0)
            {
                game.GetComponent<CityProgressionController>().ClosePanel();
                if(game.Hideout.IsInside)game.Hideout.Travel(false);
                CitySpatialSoundscape sound=game.GetComponent<CitySpatialSoundscape>();
                int listeners=0;foreach(AudioListener ear in FindObjectsByType<AudioListener>(FindObjectsSortMode.None))if(ear.enabled)listeners++;
                if(sound==null||sound.EmitterCount!=6||listeners!=1||!game.Audio.HasMovementClips)
                {Finish(false,"Spatial source composition, single listener or movement clips missing.");yield break;}
                yield return new WaitForSecondsRealtime(.3f);
                if(sound.PlayingSources==0){Finish(false,"Outdoor positional sources did not play.");yield break;}
                Vector3 source=game.Player.position+Vector3.up*100,earPoint=source+Vector3.right*8;
                if(sound.IsBlocked(source,earPoint)){Finish(false,"Clear acoustic ray was incorrectly blocked.");yield break;}
                GameObject barrier=GameObject.CreatePrimitive(PrimitiveType.Cube);barrier.transform.position=source+Vector3.right*4;barrier.transform.localScale=new Vector3(.3f,3,3);Physics.SyncTransforms();
                bool blocked=sound.IsBlocked(source,earPoint);Destroy(barrier);yield return null;
                if(!blocked||sound.IsBlocked(source,earPoint)){Finish(false,"Acoustic wall occlusion or removal failed.");yield break;}
                game.Audio.SetMuted(true);yield return null;
                if(sound.PlayingSources!=0){Finish(false,"Spatial ambience ignored global mute.");yield break;}
                game.Audio.SetMuted(false);yield return null;
                sound.SendMessage("OnApplicationPause",true);
                if(sound.PlayingSources!=0){Finish(false,"Spatial ambience played during suspension.");yield break;}
                sound.SendMessage("OnApplicationPause",false);yield return null;
                if(!game.Hideout.Travel(true)){Finish(false,"Could not enter furnished hideout.");yield break;}
                yield return new WaitForSecondsRealtime(.2f);
                if(sound.PlayingSources!=1||game.Hideout.WorkDesk==null||!game.Hideout.WorkDesk.GetComponent<CityHideoutDesk>().TryInteract(game.Session)||!game.Director.IsPaused)
                {Finish(false,"Interior fan/desk journal/pause integration failed.");yield break;}
                yield return null;
                if(sound.PlayingSources!=0){Finish(false,"Interior sound ignored paused journal.");yield break;}
                game.GetComponent<CityProgressionController>().ClosePanel();
                string hideoutCapture=Argument("-vktHideoutScreenshot","");
                if(!string.IsNullOrEmpty(hideoutCapture)&&!CaptureRenderedFrame(game,hideoutCapture,out string hideoutError))
                {Finish(false,hideoutError);yield break;}
                if(!game.Hideout.Travel(false)){Finish(false,"Furnished hideout exit failed.");yield break;}
                yield return null;
                CityCameraRig poseCamera=game.Player.GetComponentInChildren<CityCameraRig>();CityCameraMode priorMode=poseCamera.Mode;
                poseCamera.SetMode(CityCameraMode.Chase);yield return null;
                var motion=game.Player.GetComponentInChildren<CityActorMotion>();
                if(motion==null){Finish(false,"Animated player motion absent.");yield break;}
                float phase=motion.BreathingPhase;yield return new WaitForSeconds(.25f);
                if(motion.BreathingPhase<=phase){Finish(false,"Player breathing pose did not advance.");yield break;}
                poseCamera.SetMode(priorMode);
                details+=" Spatial audio: six mono 3D emitters, one listener, clear/blocked/removed-wall rays, mute, suspension, interior fan and desk journal pause passed. Additive player breathing advanced.";
                Debug.Log("VKT_ACOUSTICS_VERIFIED emitters=6 listeners=1 probes="+sound.ProbeCount);
            }
            if (Array.Exists(Environment.GetCommandLineArgs(), arg => arg == "-vktEnhanced"))
            {
                if (!VerifyEnhanced(game, out string extra)) { Finish(false, extra); yield break; }
                details += " " + extra;
                CityMusicPlayer music = game.GetComponent<CityMusicPlayer>();
                if (music.LocalCount > 0)
                {
                    music.SendMessage("OnApplicationPause",true);
                    music.Next(); float audioDeadline = Time.realtimeSinceStartup + 20;
                    while (!music.IsReady && Time.realtimeSinceStartup < audioDeadline) yield return null;
                    if (!music.IsReady || music.IsPlaying) { Finish(false, "Local audio did not finish loading silently during application suspension."); yield break; }
                    music.SendMessage("OnApplicationPause",false);
                    yield return new WaitForSecondsRealtime(.5f);
                    if (!music.IsPlaying) { Finish(false, "Deferred audio did not start on application resume."); yield break; }
                    music.TogglePause(); details += " Local audio decoded while suspended, stayed silent and started on application resume.";
                }
            }
            if(Array.IndexOf(Environment.GetCommandLineArgs(),"-vktMapUpgradeSmoke")>=0)
            {
                game.GetComponent<CityProgressionController>().ClosePanel();
                if(game.Hideout.IsInside)game.Hideout.Travel(false);
                yield return null;
                var streets=game.GetComponent<CityOsmStreets>();
                if(streets==null||streets.JunctionCount<10||streets.Visibility==null||streets.Visibility.Registered<100)
                {Finish(false,"Connected road junctions and map visibility were not composed.");yield break;}
                int colliders=0;foreach(Collider c in streets.Visibility.GetComponentsInChildren<Collider>())if(c.enabled)colliders++;
                streets.Visibility.ApplyAt(streets.GroundAnchor+Vector3.up*1000);
                int after=0;foreach(Collider c in streets.Visibility.GetComponentsInChildren<Collider>())if(c.enabled)after++;
                if(streets.Visibility.VisibleCount!=0||colliders!=after||after<streets.WallColliderCount)
                {Finish(false,"Distant map visibility disabled physics or retained distant renderers.");yield break;}
                Camera mapCamera=game.Player.GetComponentInChildren<Camera>();var mapRig=mapCamera.GetComponent<CityCameraRig>();
                Vector3 savedPosition=mapCamera.transform.position;Quaternion savedRotation=mapCamera.transform.rotation;
                mapRig.enabled=false;mapCamera.transform.position=streets.GroundAnchor+new Vector3(0,100,-95);mapCamera.transform.LookAt(streets.GroundAnchor+Vector3.forward*20);
                streets.Visibility.ApplyAt(mapCamera.transform.position);yield return new WaitForEndOfFrame();
                string roadCapture=Argument("-vktRoadScreenshot","");
                if(!string.IsNullOrEmpty(roadCapture)&&!CaptureRenderedFrame(game,roadCapture,out string roadError))
                {Finish(false,roadError);yield break;}
                mapCamera.transform.SetPositionAndRotation(savedPosition,savedRotation);mapRig.enabled=true;streets.Visibility.ApplyAt(savedPosition);
                if(streets.Visibility.VisibleCount==0){Finish(false,"Map rendering did not return after camera restoration.");yield break;}
                details+=" Map upgrade: connected road/sidewalk topology, authored junction surfaces and street placards composed; renderer-distance budget retained all colliders and restored the camera view.";
                Debug.Log("VKT_STREETS_VERIFIED segments="+streets.SegmentCount+" junctions="+streets.JunctionCount+" budgetEntries="+streets.Visibility.Registered+" activeColliders="+after);
            }
            if (Array.Exists(Environment.GetCommandLineArgs(), arg => arg == "-vktCombatSmoke"))
            {
                if (!BeginCombatCheck(game, out string combatError)) { Finish(false, combatError); yield break; }
                float impactDeadline = Time.realtimeSinceStartup + 3;
                while (!game.Combat.PracticeWindow.IsBroken && Time.realtimeSinceStartup < impactDeadline) yield return null;
                if (!game.Combat.PracticeWindow.IsBroken) { Finish(false, "Thrown stone did not hit and break the physical window."); yield break; }
                if (!FinishCombatCheck(game, out combatError)) { Finish(false, combatError); yield break; }
                if (!PrepareProjectileCheck(game, out combatError)) { Finish(false, combatError); yield break; }
                GameObject tracked = GameObject.Find("StoneProjectile_0");
                if (tracked == null) { Finish(false, "Pooled projectile visual missing."); yield break; }
                Vector3 frozen = tracked.transform.position;
                CityProgressionController combatController = game.GetComponent<CityProgressionController>();
                combatController.OpenPanel();
                yield return new WaitForSecondsRealtime(.2f);
                bool pausedProjectile = tracked != null && Vector3.Distance(frozen, tracked.transform.position) < .001f && game.Combat.ActiveProjectiles == 8;
                combatController.ClosePanel();
                if (!pausedProjectile) { Finish(false, "Phone pause did not freeze pooled projectiles."); yield break; }
                float expiryDeadline = Time.realtimeSinceStartup + 4;
                while (game.Combat.ActiveProjectiles > 0 && Time.realtimeSinceStartup < expiryDeadline) yield return null;
                if (game.Combat.ActiveProjectiles != 0 || !game.Combat.TryAttack()) { Finish(false, "Projectile pool did not expire and become reusable."); yield break; }
                if (!game.Vitals.ApplyDamage(100) || game.Combat.TryAttack()) { Finish(false, "Dead player attack gating failed."); yield break; }
                yield return null;
                if (game.Combat.ActiveProjectiles != 0 || !game.Vitals.Respawn()) { Finish(false, "Death did not clear projectiles and restore the player."); yield break; }
                game.Combat.Inventory.SelectWeapon(CityWeapon.Stick);
                game.Player.GetComponentInChildren<Camera>().transform.LookAt(game.Combat.PracticeWindow.transform.position);
                details += " Physical stone/stick pickups, paused attack gating, swept stone/window impact, single glass break, stick hit/NPC reaction, wall occlusion, pool exhaustion/reuse, projectile pause and death cleanup passed.";
            }
            yield return new WaitForSecondsRealtime(0.5f);
            if (Vector3.Distance(game.Player.GetComponentInChildren<Camera>().transform.localPosition, new Vector3(0, 1.62f, 0)) > 0.01f)
            { Finish(false, "Camera left the player's eye position after validation frames."); yield break; }
            CityCameraRig cameraRig = game.Player.GetComponentInChildren<CityCameraRig>();
            if (cameraRig == null || !cameraRig.AvatarAvailable) { Finish(false, "Player camera rig/avatar missing."); yield break; }
            Camera perspectiveCamera = game.Player.GetComponentInChildren<Camera>();
            Vector3 cameraEye = game.Player.TransformPoint(new Vector3(0, 1.62f, 0));
            Vector3 savedPlayerPosition = game.Player.position;
            CharacterController cameraBody = game.Player.GetComponent<CharacterController>();
            cameraBody.enabled = false; game.Player.position += Vector3.up * 40; cameraBody.enabled = true;
            foreach (CityCameraMode mode in new[] { CityCameraMode.Shoulder, CityCameraMode.Chase })
            {
                cameraRig.SetMode(mode); yield return new WaitForEndOfFrame();
                cameraEye = game.Player.TransformPoint(new Vector3(0, 1.62f, 0));
                float actualDistance = Vector3.Distance(perspectiveCamera.transform.position, cameraEye);
                if (actualDistance < 1 || actualDistance > CityCameraRig.Offset(mode).magnitude + .02f)
                { Finish(false, "Third-person camera did not maintain its bounded orbit."); yield break; }
            }
            Vector3 obstructionOffset = perspectiveCamera.transform.rotation * CityCameraRig.Offset(CityCameraMode.Chase);
            GameObject cameraWall = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            cameraWall.name = "CameraOcclusionValidation"; cameraWall.transform.position = cameraEye + obstructionOffset * .5f; cameraWall.transform.localScale = Vector3.one;
            Physics.SyncTransforms(); yield return new WaitForEndOfFrame();
            float blockedDistance = Vector3.Distance(perspectiveCamera.transform.position, game.Player.TransformPoint(new Vector3(0,1.62f,0)));
            Destroy(cameraWall);
            if (blockedDistance >= obstructionOffset.magnitude * .65f)
            { Finish(false, "Camera did not retract before an obstruction."); yield break; }
            cameraRig.SetMode(CityCameraMode.FirstPerson);
            cameraBody.enabled = false; game.Player.position = savedPlayerPosition; cameraBody.enabled = true;
            yield return new WaitForEndOfFrame();
            details += " Three camera modes, imported player avatar, bounded orbit, obstruction retraction and first-person restoration passed.";
            if (Array.Exists(Environment.GetCommandLineArgs(), arg => arg == "-vktLifeSmoke"))
            {
                yield return VerifyLifestyle(game);
                if (!lifestyleVerified) yield break;
                details += " Block life: physical gym and kiosk interaction, UI repetitions and anti-spam, pause ownership, paid/stolen food, wanted save restoration, police line-of-sight/occlusion and capture range, accelerated escape/arrest, one-time challenge and durable v5 character stats passed.";
                if(Array.IndexOf(Environment.GetCommandLineArgs(),"-vktRoutineSmoke")>=0)
                {
                    yield return VerifyRoutine(game);
                    if(!routineVerified)yield break;
                    details+=" Routine: authoritative wanted/training contact gates, staged cargo/receipt settlement, mission pause/suspension, scrollable journal, conditioning course/XP/save/cancellation, and bounded feedback audio pause/mute/suspension passed.";
                }
            }
            if(Array.IndexOf(Environment.GetCommandLineArgs(),"-vktResumeBoxingSmoke")>=0)
            {
                yield return VerifyResumeBoxing(game);
                if(!resumeBoxingVerified)yield break;
                details+=" Active street-job disk resume, single settlement, boxing UI/reaction/guard/reward/pause and durable rest passed.";
            }
            if (Array.Exists(Environment.GetCommandLineArgs(), arg => arg == "-vktPerspectiveCapture"))
            {
                game.GetComponent<CityProgressionController>().ClosePanel();
                if (game.Hideout.IsInside) game.Hideout.Travel(false);
                game.Atmosphere.ConfigureCycle(18,12,15);
                cameraBody.enabled = false; game.Player.position = game.Hideout.Entrance.position + new Vector3(-6,-.9f,-7); cameraBody.enabled = true;
                game.Player.rotation = Quaternion.Euler(0,145,0);
                perspectiveCamera.transform.localRotation = Quaternion.Euler(12,0,0);
                string requestedMode = Argument("-vktCameraMode", "Chase");
                cameraRig.SetMode(requestedMode == "Shoulder" ? CityCameraMode.Shoulder : requestedMode == "FirstPerson" ? CityCameraMode.FirstPerson : CityCameraMode.Chase);
                yield return new WaitForEndOfFrame();
            }
            if (Array.Exists(Environment.GetCommandLineArgs(), arg => arg == "-vktCatalogCapture"))
            {
                game.GetComponent<CityProgressionController>().OpenPanel();
                FindAnyObjectByType<CityPhonePanel>(FindObjectsInactive.Include).OpenCatalog();
                yield return null;
            }
            if (Array.Exists(Environment.GetCommandLineArgs(), arg => arg == "-vktCardsCapture"))
            {
                game.GetComponent<CityProgressionController>().OpenPanel();
                CityPhonePanel phone = FindAnyObjectByType<CityPhonePanel>(FindObjectsInactive.Include); phone.OpenCatalog();
                phone.transform.Find("CatalogPanel/Identity").GetComponent<UnityEngine.UI.Button>().onClick.Invoke();
                yield return null;
            }
            if (Array.Exists(Environment.GetCommandLineArgs(), arg => arg == "-vktCityCapture"))
            {
                game.GetComponent<CityProgressionController>().ClosePanel();
                if (game.Hideout.IsInside) game.Hideout.Travel(false);
                CharacterController body = game.Player.GetComponent<CharacterController>(); body.enabled = false;
                game.Player.position = new Vector3(0, 12, -40); body.enabled = true;
                game.Player.GetComponentInChildren<Camera>().transform.LookAt(new Vector3(0, 3, 12));
                yield return null;
            }
            if (Array.Exists(Environment.GetCommandLineArgs(), arg => arg == "-vktJobsCapture"))
            {
                game.GetComponent<CityProgressionController>().OpenPanel();
                FindAnyObjectByType<CityPhonePanel>(FindObjectsInactive.Include).OpenJobs();
                yield return null;
            }
            if (Array.Exists(Environment.GetCommandLineArgs(), arg => arg == "-vktMapCapture"))
            {
                game.GetComponent<CityProgressionController>().OpenPanel();
                FindAnyObjectByType<CityPhonePanel>(FindObjectsInactive.Include).OpenMap();
                yield return null;
            }
            if (Array.Exists(Environment.GetCommandLineArgs(), arg => arg == "-vktActorCapture"))
            {
                game.Atmosphere.ConfigureCycle(1080,720,15);
                game.GetComponent<CityProgressionController>().ClosePanel();if(game.Hideout.IsInside)game.Hideout.Travel(false);
                CityHazard actor=Array.Find(FindObjectsByType<CityHazard>(FindObjectsSortMode.None),item=>item.Kind==CityHazardKind.HostileAdult);actor.enabled=false;
                CharacterController body=game.Player.GetComponent<CharacterController>();body.enabled=false;game.Player.position=actor.transform.position+new Vector3(1.8f,.05f,2.8f);body.enabled=true;
                game.Player.GetComponentInChildren<Camera>().transform.LookAt(actor.transform.position+Vector3.up*1.1f);
                yield return null;
            }
            if (Array.Exists(Environment.GetCommandLineArgs(), arg => arg == "-vktLifeCapture"))
            { game.GetComponent<CityProgressionController>().ClosePanel(); game.GetComponent<CityLifestyleRuntime>().OpenMenu(); yield return null; }
            string screenshot = Argument("-vktScreenshot", "");
            if (!string.IsNullOrEmpty(screenshot))
            {
                Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(screenshot)));
                if (!CaptureRenderedFrame(game, screenshot, out string captureError)) { Finish(false, captureError); yield break; }
                if (!File.Exists(screenshot) || !ContainsRenderedPixels(screenshot)) { Finish(false, "Player screenshot is missing or blank; visual validation did not pass."); yield break; }
            }
            if(float.TryParse(Argument("-vktSoakSeconds","0"),System.Globalization.NumberStyles.Float,System.Globalization.CultureInfo.InvariantCulture,out float soak)&&float.IsFinite(soak)&&soak>0)
            {
                soak=Mathf.Clamp(soak,10,600);
                game.GetComponent<CityProgressionController>().ClosePanel();
                if(game.Hideout.IsInside)game.Hideout.Travel(false);
                float started=Time.realtimeSinceStartup;int frames=0;
                while(Time.realtimeSinceStartup-started<soak)
                {
                    if(!game.WorldReady||game.Director.IsPaused||!game.Vitals.IsAlive)
                    {Finish(false,"Extended city observation lost ready/alive/unpaused state.");yield break;}
                    game.Player.rotation=Quaternion.Euler(0,145+(Time.realtimeSinceStartup-started)*3,0);
                    frames++;yield return null;
                }
                Debug.Log("VKT_SOAK_VERIFIED seconds="+soak+" frames="+frames+" stationaryPlayer=true cameraOrbit=true");
                details+=" Extended stationary city observation with camera orbit passed; not a full-city traversal or thermal benchmark.";
            }
            Finish(true, details);
        }

        private IEnumerator VerifyResumeBoxing(CityGameBootstrap game)
        {
            var runtime=game.GetComponent<CityLifestyleRuntime>();var life=runtime.Life;
            var controller=game.GetComponent<CityProgressionController>();controller.ClosePanel();runtime.CloseMenu();
            if(game.Hideout.IsInside)game.Hideout.Travel(false);
            life.TickPolice(19,false,false);life.Tick(91,false);game.Session.CoolDown(game.Session.Heat);
            var body=game.Player.GetComponent<CharacterController>();body.enabled=false;game.Player.position=runtime.Gym.position+Vector3.back*2;body.enabled=true;Physics.SyncTransforms();
            if(life.Nutrition<30)life.TryEat(false);
            var boxingCamera=game.Player.GetComponentInChildren<CityCameraRig>();boxingCamera.SetMode(CityCameraMode.FirstPerson);
            runtime.OpenMenu();yield return null;
            var start=Array.Find(FindObjectsByType<UnityEngine.UI.Button>(FindObjectsSortMode.None),b=>b.name=="BOKS / reakcija i gard");
            if(start==null){Finish(false,"Boxing menu button missing.");yield break;}
            int xp=life.Experience,count=life.BoxingSessions,strength=life.Strength;start.onClick.Invoke();yield return null;
            if(!life.IsBoxing||game.Combat.CanAct||game.Director.TryAcceptStreetJob()){Finish(false,"Boxing entry/contact/combat exclusivity failed.");yield break;}
            yield return new WaitForSeconds(.2f);
            Transform visual=game.Player.Find("VKT_PlayerVisual");
            if(visual==null||!visual.gameObject.activeSelf||Cursor.lockState!=CursorLockMode.None){Finish(false,"First-person boxing did not reveal its avatar or release cursor for UI.");yield break;}
            var boxer=visual.GetComponentInChildren<CityActorMotion>();
            if(boxer==null||!boxer.BoxingPoseActive){Finish(false,"First-person boxing did not bind its previously hidden rig.");yield break;}
            double clock=runtime.Boxing.RoundTime;game.Director.SetPaused(true);yield return new WaitForSecondsRealtime(.2f);
            if(runtime.Boxing.RoundTime!=clock){Finish(false,"Pause advanced boxing clock.");yield break;}game.Director.SetPaused(false);
            for(int round=0;round<8;round++)
            {
                float deadline=Time.realtimeSinceStartup+4;
                while(runtime.Boxing.Round==round&&runtime.Boxing.RoundTime<.8&&Time.realtimeSinceStartup<deadline)yield return null;
                if(runtime.Boxing.Round!=round||!runtime.Boxing.WindowOpen){Finish(false,"Boxing reaction window missed.");yield break;}
                string name=runtime.Boxing.Expected==CityBoxingAction.Jab?"UDARAC / J":"GARD / K";
                var response=Array.Find(FindObjectsByType<UnityEngine.UI.Button>(FindObjectsSortMode.None),b=>b.name==name);
                if(response==null){Finish(false,"Boxing response button missing.");yield break;}response.onClick.Invoke();
                if(round==2)
                {
                    yield return new WaitForSeconds(.15f);string capture=Argument("-vktBoxingScreenshot","");
                    if(!string.IsNullOrEmpty(capture)&&!CaptureRenderedFrame(game,capture,out string error)){Finish(false,error);yield break;}
                }
                while(runtime.Boxing.Round==round&&Time.realtimeSinceStartup<deadline)yield return null;
            }
            if(life.IsBoxing||life.BoxingSessions!=count+1||life.Experience!=xp+30||life.Strength!=strength||runtime.Boxing.TryClaim(life)||runtime.StartBoxing())
            {Finish(false,"Boxing payout, endurance/strength separation or rest failed.");yield break;}
            yield return null;
            if(boxingCamera.Mode!=CityCameraMode.FirstPerson||visual.gameObject.activeSelf){Finish(false,"Boxing did not restore the prior first-person presentation.");yield break;}
            controller.SaveNow();
            if(!new CitySaveStore(controller.SavePath).TryLoad(out CityProfileData boxed,out _)||boxed.lifestyle.boxingSessions!=count+1)
            {Finish(false,"Boxing progress did not survive disk reload.");yield break;}
            var jobs=game.Director.Jobs;if(jobs.IsActive)jobs.Cancel();jobs.Tick(31);
            if(!game.Director.TryAcceptStreetJob()||!game.Director.TryInteract(jobs.Offer.StationAt(0))){Finish(false,"Checkpoint fixture could not pick up cargo.");yield break;}
            // No manual SaveNow: pickup must synchronously request its own checkpoint.
            if(!new CitySaveStore(controller.SavePath).TryLoad(out CityProfileData saved,out _)||saved.streetJob==null||!saved.streetJob.active||saved.streetJob.objectiveIndex!=1)
            {Finish(false,"Active cargo checkpoint did not reach disk.");yield break;}
            var restored=new CityProgression(CitySession.Restore(saved),saved);var resumed=new CityStreetJobs(restored.Session);restored.BindStreetJobs(resumed);
            if(!resumed.HasCargo||resumed.Offer.Title!=jobs.Offer.Title||resumed.RemainingSeconds!=saved.streetJob.remainingSeconds)
            {Finish(false,"Street checkpoint restored different cargo/offer/time.");yield break;}
            int cash=restored.Session.Cash,reward=resumed.Offer.CashReward;
            resumed.TryVisit(resumed.Offer.StationAt(1));resumed.TryVisit(resumed.Offer.StationAt(2));
            if(restored.Session.Cash!=cash+reward||resumed.TryVisit(0)){Finish(false,"Restored mission replayed or missed its reward.");yield break;}
            jobs.Cancel();controller.SaveNow();resumeBoxingVerified=true;
            Debug.Log("VKT_RESUME_BOXING_VERIFIED profileVersion="+CityProgression.CurrentProfileVersion+" diskCargoResume=true payoutOnce=true boxingRounds=8 separateEndurance=true pause=true");
        }

        private IEnumerator VerifyLifestyle(CityGameBootstrap game)
        {
            CityLifestyleRuntime runtime=game.GetComponent<CityLifestyleRuntime>();
            CityProgressionController controller=game.GetComponent<CityProgressionController>();
            controller.ClosePanel();game.GetComponent<CityMusicPanel>()?.Close();
            if(runtime==null){Finish(false,"Block-life runtime absent.");yield break;}
            if(game.Hideout.IsInside)game.Hideout.Travel(false);
            CityLifestyle life=runtime.Life;
            CharacterController body=game.Player.GetComponent<CharacterController>();
            body.enabled=false;game.Player.position=runtime.Gym.position+Vector3.back*2;body.enabled=true;
            Camera view=game.Player.GetComponentInChildren<Camera>();view.transform.LookAt(runtime.Gym.position+Vector3.up);
            Physics.SyncTransforms();
            if(!game.Player.GetComponent<CityPlayerInteraction>().TryInteract()||!runtime.IsMenuOpen||!game.Director.IsPaused)
            {Finish(false,"Physical gym interaction did not open its paused menu.");yield break;}
            float nutrition=life.Nutrition;yield return new WaitForSecondsRealtime(.2f);
            if(Mathf.Abs(life.Nutrition-nutrition)>.001f){Finish(false,"Nutrition changed while menu paused gameplay.");yield break;}
            runtime.CloseMenu();
            if(Array.IndexOf(Environment.GetCommandLineArgs(),"-vktCharacterSmoke")>=0)
            {
                if(!runtime.Wardrobe.Open()||!game.Director.IsPaused){Finish(false,"Wardrobe did not own gameplay pause.");yield break;}
                for(int style=0;style<3;style++)
                {
                    string caption=CityAppearance.Name((CityOutfit)style);
                    var choice=Array.Find(FindObjectsByType<UnityEngine.UI.Button>(FindObjectsSortMode.None),b=>b.name==caption);
                    if(choice==null){Finish(false,"Wardrobe outfit button missing.");yield break;}
                    choice.onClick.Invoke();yield return new WaitForSecondsRealtime(.2f);
                    if(runtime.Wardrobe.PreviewOutfit!=(CityOutfit)style||game.Progression.Appearance.Outfit!=(CityOutfit)style)
                    {Finish(false,"Wardrobe choice and preview diverged.");yield break;}
                }
                string wardrobeCapture=Argument("-vktWardrobeScreenshot","");
                if(!string.IsNullOrEmpty(wardrobeCapture)&&!CaptureRenderedFrame(game,wardrobeCapture,out string wardrobeError))
                {Finish(false,wardrobeError);yield break;}
                runtime.Wardrobe.Rotate(45);yield return new WaitForSecondsRealtime(.1f);
                string portraitCapture=Argument("-vktPortraitScreenshot","");
                if(!string.IsNullOrEmpty(portraitCapture))
                {
                    runtime.Wardrobe.Rotate(-45);runtime.Wardrobe.TogglePortrait();
                    yield return new WaitForSecondsRealtime(.25f);
                    if(!runtime.Wardrobe.Portrait||!CaptureRenderedFrame(game,portraitCapture,out string portraitError))
                    {Finish(false,"Portrait capture failed.");yield break;}
                    CityFaceExpression previewFace=null;
                    foreach(var face in FindObjectsByType<CityFaceExpression>(FindObjectsSortMode.None))if(face.Preview){previewFace=face;break;}
                    if(previewFace==null){Finish(false,"Preview expression driver absent.");yield break;}
                    float timeout=Time.realtimeSinceStartup+5;bool blinked=false;
                    while(Time.realtimeSinceStartup<timeout)
                    {
                        if(previewFace.State.x>.85f){blinked=true;break;}yield return null;
                    }
                    if(!blinked){Finish(false,"Wardrobe face did not blink during its owned pause.");yield break;}
                    for(int mood=1;mood<=5;mood++)
                    {
                        runtime.Wardrobe.CycleExpression();yield return new WaitForSecondsRealtime(.35f);
                        if(previewFace.Mood!=(CityFaceMood)(mood%5)||!game.Director.IsPaused)
                        {Finish(false,"Expression preview changed pause ownership or selected the wrong mood.");yield break;}
                        if(mood==2)
                        {
                            var faceSkin=previewFace.GetComponentInChildren<SkinnedMeshRenderer>();
                            int shape=faceSkin==null?-1:faceSkin.sharedMesh.GetBlendShapeIndex(CityFaceMorph.JawShape);
                            if(shape<0||faceSkin.GetBlendShapeWeight(shape)<10)
                            {Finish(false,"Effort preview did not drive the owned jaw morph during pause.");yield break;}
                            if(!CaptureRenderedFrame(game,portraitCapture.Replace(".png","-effort.png"),out _))
                            {Finish(false,"Effort jaw capture failed.");yield break;}
                            Debug.Log("VKT_JAW_VERIFIED ownedBlendShape=true pausedEffort=true");
                        }
                        if(mood==3&&!CaptureRenderedFrame(game,portraitCapture.Replace(".png","-success.png"),out _))
                        {Finish(false,"Success expression capture failed.");yield break;}
                    }
                    Debug.Log("VKT_FACE_STATES_VERIFIED count=5 previewWhilePaused=true");
                    Debug.Log("VKT_CHARACTER_UI_VERIFIED portrait=true previewBlinkDuringPause=true");
                    runtime.Wardrobe.TogglePortrait();
                }
                runtime.Wardrobe.Close();yield return null;
                var wardrobeStore=new CitySaveStore(controller.SavePath);
                if(!wardrobeStore.TryLoad(out CityProfileData wardrobeSaved,out _)||wardrobeSaved.appearance==null||wardrobeSaved.appearance.outfit!=2||
                    new CityProgression(CitySession.Restore(wardrobeSaved),wardrobeSaved).Appearance.Outfit!=CityOutfit.Busije)
                {Finish(false,"Wardrobe profile round-trip failed.");yield break;}
                if(game.Player.GetComponentInChildren<CityCameraRig>().CurrentOutfit!=CityOutfit.Busije||runtime.BenchVisual.CurrentOutfit!=CityOutfit.Busije)
                {Finish(false,"Walking and bench outfits diverged.");yield break;}
            }
            if(!runtime.StartWorkout()){Finish(false,"Physical gym workout could not start.");yield break;}
            if(game.Combat.CanAct){Finish(false,"Combat remained enabled during bench training.");yield break;}
            yield return new WaitForSeconds(.2f);
            UnityEngine.UI.Button tap=Array.Find(FindObjectsByType<UnityEngine.UI.Button>(FindObjectsSortMode.None),b=>b.name=="TAP / F: PONAVLJANJE");
            if(tap==null){Finish(false,"Workout touch button absent.");yield break;}
            for(int i=0;i<CityLifestyle.RequiredReps;i++)
            {
                tap.onClick.Invoke();tap.onClick.Invoke();
                if(life.Reps!=i){Finish(false,"Bench awarded an incomplete repetition.");yield break;}
                yield return new WaitForSeconds(1.0f);
                if(i==2)
                {
                    string capture=Argument("-vktBenchScreenshot","");
                    if(!string.IsNullOrEmpty(capture)&&!CaptureRenderedFrame(game,capture,out string captureError))
                    {Finish(false,captureError);yield break;}
                }
                tap.onClick.Invoke();
                yield return new WaitForSeconds(1.15f);
                if(life.Reps!=i+1){Finish(false,"Full bench repetition failed.");yield break;}
                if(!runtime.BenchVisual.RigReady||runtime.BenchVisual.HandError>.03f||runtime.BenchVisual.FootError>.03f)
                {Finish(false,"Bench rig or hand contact failed: "+runtime.BenchVisual.HandError);yield break;}
            }
            if(life.Workouts!=1||life.IsTraining||life.Experience!=40){Finish(false,"Workout reward/state failed.");yield break;}
            body.enabled=false;game.Player.position=runtime.Food.position+Vector3.back*2;body.enabled=true;Physics.SyncTransforms();
            int cash=game.Session.Cash;
            if(!runtime.Eat(false)||game.Session.Cash!=cash-CityLifestyle.MealPrice){Finish(false,"Paid kiosk meal failed.");yield break;}
            life.Tick(210,false);
            if(!runtime.Eat(true)||!life.Wanted){Finish(false,"Stolen kiosk meal did not trigger wanted state.");yield break;}
            CitySaveStore reader=new CitySaveStore(controller.SavePath);
            if(!reader.TryLoad(out CityProfileData wanted,out _)||wanted.lifestyle==null||!wanted.lifestyle.wanted)
            {Finish(false,"Wanted state was not durable.");yield break;}
            yield return new WaitForSeconds(.2f);
            runtime.OpenMenu();yield return new WaitForSecondsRealtime(.2f);
            if(runtime.SirenPlaying){Finish(false,"Patrol siren played while menu paused gameplay.");yield break;}
            runtime.CloseMenu();
            runtime.SendMessage("OnApplicationPause",true);nutrition=life.Nutrition;yield return new WaitForSecondsRealtime(.2f);
            if(runtime.SirenPlaying||Mathf.Abs(life.Nutrition-nutrition)>.001f){Finish(false,"Block-life audio or nutrition continued during app suspension.");yield break;}
            runtime.SendMessage("OnApplicationPause",false);
            CityPoliceOfficer[] patrol=FindObjectsByType<CityPoliceOfficer>(FindObjectsSortMode.None);
            CityPoliceOfficer visible=null;
            foreach(CityPoliceOfficer officer in patrol){officer.Step(.01f,true);if(officer.SeesPlayer)visible=officer;}
            if(visible==null){Finish(false,"Dispatched patrol did not see the visible kiosk player.");yield break;}
            GameObject wall=GameObject.CreatePrimitive(PrimitiveType.Cube);wall.name="PatrolOcclusionValidation";
            Vector3 between=game.Player.position-visible.transform.position;between.y=0;
            wall.transform.position=(game.Player.position+visible.transform.position)*.5f+Vector3.up*1.5f;
            wall.transform.rotation=Quaternion.LookRotation(between);wall.transform.localScale=new Vector3(12,4,.4f);Physics.SyncTransforms();visible.Step(.01f,true);
            bool blocked=!visible.SeesPlayer&&!visible.CanCapture;Destroy(wall);yield return null;
            if(!blocked){Finish(false,"Patrol saw or captured through a wall.");yield break;}
            life.TickPolice(CityLifestyle.EscapeDuration+.1,false,false);yield return null;
            if(life.Wanted||life.Escapes!=1||!life.ChallengeClaimed){Finish(false,"Escape/challenge terminal state failed.");yield break;}
            cash=game.Session.Cash;
            body.enabled=false;game.Player.position=visible.transform.position+visible.transform.forward*1.2f;body.enabled=true;Physics.SyncTransforms();
            life.ReportCrime();visible.Step(.01f,true);
            if(!visible.CanCapture){Finish(false,"Patrol close-contact capture range failed.");yield break;}
            life.TickPolice(CityLifestyle.ArrestDuration+.1,true,true);yield return new WaitForSeconds(.2f);
            if(life.Wanted||life.Arrests!=1||game.Session.Cash!=Math.Max(0,cash-120)){Finish(false,"Arrest fine or release failed.");yield break;}
            controller.SaveNow();
            if(!new CitySaveStore(controller.SavePath).TryLoad(out CityProfileData saved,out _)||saved.version!=CityProgression.CurrentProfileVersion||saved.lifestyle.workouts!=1||saved.lifestyle.meals!=2||saved.lifestyle.escapes!=1||saved.lifestyle.arrests!=1||!saved.lifestyle.challengeClaimed)
            {Finish(false,"Development stats/challenge did not survive save restore.");yield break;}
            if(Array.IndexOf(Environment.GetCommandLineArgs(),"-vktLocalAiSmoke")>=0)
            {
                int cashBefore=game.Session.Cash, contractsBefore=game.Progression.CompletedContracts;
                CityLocalStoryPanel contacts=runtime.LocalStory;
                if(contacts==null||!contacts.Open()||!game.Director.IsPaused||contacts.Source!="offline")
                {Finish(false,"Local contacts failed to open with authored offline text and pause ownership.");yield break;}
                contacts.RefreshRemote();contacts.Select(1);yield return null;
                if(contacts.IsLoading||contacts.Source!="offline")
                {Finish(false,"Contact switch did not cancel the previous request.");yield break;}
                var refreshButton=Array.Find(FindObjectsByType<UnityEngine.UI.Button>(FindObjectsSortMode.None),b=>b.name=="OSVEŽI / OLLAMA");
                if(refreshButton==null){Finish(false,"Local contacts refresh button absent.");yield break;}
                refreshButton.onClick.Invoke();
                float deadline=Time.realtimeSinceStartup+48;
                while(contacts.IsLoading&&Time.realtimeSinceStartup<deadline)yield return null;
                if(contacts.IsLoading||string.IsNullOrWhiteSpace(contacts.CurrentLine)||!game.Director.IsPaused)
                {Finish(false,"Local contact response timed out or lost its paused panel.");yield break;}
                bool required=Array.IndexOf(Environment.GetCommandLineArgs(),"-vktLocalAiRequired")>=0;
                if(required&&contacts.Source!="ollama"&&contacts.Source!="cache")
                {Finish(false,"Live local AI was required but the panel returned authored offline text.");yield break;}
                bool offlineRequired=Array.IndexOf(Environment.GetCommandLineArgs(),"-vktLocalAiOfflineRequired")>=0;
                if(offlineRequired&&(contacts.Source!="offline"||!contacts.StatusText.Contains("Start-LocalAI.ps1")))
                {Finish(false,"Unavailable companion did not explain how to restore AI while preserving authored dialogue.");yield break;}
                if(string.IsNullOrWhiteSpace(contacts.StatusText))
                {Finish(false,"Local contact response did not expose its source or availability.");yield break;}
                string aiCapture=Argument("-vktLocalAiScreenshot","");
                if(!string.IsNullOrEmpty(aiCapture)&&!CaptureRenderedFrame(game,aiCapture,out string aiError))
                {Finish(false,aiError);yield break;}
                if(game.Session.Cash!=cashBefore||game.Progression.CompletedContracts!=contractsBefore)
                {Finish(false,"Narrative contacts modified authoritative economy or mission progress.");yield break;}
                Debug.Log("VKT_LOCAL_AI_VERIFIED source="+contacts.Source+" line="+contacts.CurrentLine);
                Debug.Log("VKT_AI_STATUS_VERIFIED "+contacts.StatusText);
                contacts.Close();yield return null;
                if(game.Director.IsPaused||contacts.IsOpen||contacts.IsLoading)
                {Finish(false,"Local contacts left gameplay paused or a request alive after close.");yield break;}
            }
            lifestyleVerified=true;
        }

        private IEnumerator VerifyRoutine(CityGameBootstrap game)
        {
            CityLifestyleRuntime runtime=game.GetComponent<CityLifestyleRuntime>();CityLifestyle life=runtime.Life;
            var controller=game.GetComponent<CityProgressionController>();var body=game.Player.GetComponent<CharacterController>();
            var jobs=game.Director.Jobs;
            if(jobs.IsActive)jobs.Cancel();jobs.Tick(31);
            life.ReportCrime();game.Session.CoolDown(game.Session.Heat);
            if(game.Director.TryAcceptStreetJob()||jobs.IsActive){Finish(false,"Zero-heat wanted state allowed job acceptance.");yield break;}
            life.TickPolice(CityLifestyle.EscapeDuration+.1,false,false);
            if(!game.Director.TryAcceptStreetJob()){Finish(false,"Calm contact did not accept a job.");yield break;}
            int first=jobs.Offer.StationAt(0),second=jobs.Offer.StationAt(1),third=jobs.Offer.StationAt(2),reward=jobs.Offer.CashReward,cash=game.Session.Cash,completed=jobs.CompletedJobs;
            if(!game.Director.TryInteract(first)||!jobs.HasCargo){Finish(false,"Pickup did not establish carried cargo.");yield break;}
            double remaining=jobs.RemainingSeconds;game.Director.SetPaused(true);yield return new WaitForSecondsRealtime(.2f);
            if(jobs.RemainingSeconds!=remaining){Finish(false,"Menu pause advanced job time.");yield break;}
            game.Director.SetPaused(false);game.Director.SendMessage("OnApplicationPause",true);remaining=jobs.RemainingSeconds;yield return new WaitForSecondsRealtime(.2f);
            if(jobs.RemainingSeconds!=remaining){Finish(false,"Application suspension advanced job time.");yield break;}
            game.Director.SendMessage("OnApplicationPause",false);life.ReportCrime();game.Session.CoolDown(game.Session.Heat);
            if(game.Director.TryInteract(second)||jobs.ObjectiveIndex!=1||game.Session.Cash!=cash){Finish(false,"Wanted handover advanced or paid a job.");yield break;}
            remaining=jobs.RemainingSeconds;yield return new WaitForSeconds(.2f);
            if(jobs.RemainingSeconds>=remaining){Finish(false,"Wanted state incorrectly froze active job time.");yield break;}
            life.TickPolice(CityLifestyle.EscapeDuration+.1,false,false);
            if(!game.Director.TryInteract(second)||!jobs.HasReceipt||!game.Director.TryInteract(third)||jobs.CompletedJobs!=completed+1||game.Session.Cash!=cash+reward)
            {Finish(false,"Delivery/receipt did not settle exactly one completed job.");yield break;}
            controller.OpenPanel();var phone=FindAnyObjectByType<CityPhonePanel>();phone.OpenJobs();Canvas.ForceUpdateCanvases();
            var scroll=Array.Find(FindObjectsByType<UnityEngine.UI.ScrollRect>(FindObjectsSortMode.None),s=>s.name=="JobDetails");
            if(scroll==null||scroll.horizontal||!scroll.vertical){Finish(false,"Scrollable job briefing is absent.");yield break;}
            if(!TextJournalFixture(scroll)){Finish(false,"Job journal could not scroll to its end.");yield break;}
            string journalCapture=Argument("-vktRoutineJournalScreenshot","");
            if(!string.IsNullOrEmpty(journalCapture)&&!CaptureRenderedFrame(game,journalCapture,out string journalError)){Finish(false,journalError);yield break;}
            controller.ClosePanel();life.Tick(91,false);
            body.enabled=false;game.Player.position=runtime.Gym.position+Vector3.back*3;body.enabled=true;Physics.SyncTransforms();
            int xp=life.Experience,workouts=life.Workouts;
            runtime.OpenMenu();var cardio=Array.Find(FindObjectsByType<UnityEngine.UI.Button>(FindObjectsSortMode.None),b=>b.name=="KONDICIJA / 3 stanice");
            string menuCapture=Argument("-vktRoutineMenuScreenshot","");
            if(!string.IsNullOrEmpty(menuCapture)&&!CaptureRenderedFrame(game,menuCapture,out string menuError)){Finish(false,menuError);yield break;}
            if(cardio==null){Finish(false,"Conditioning touch button absent.");yield break;}cardio.onClick.Invoke();
            if(!life.IsConditioning||game.Director.IsPaused||life.IsTraining||game.Director.TryAcceptStreetJob())
            {Finish(false,"Conditioning failed to start or allowed a job/bench overlap.");yield break;}
            remaining=runtime.Conditioning.Remaining;float nutrition=life.Nutrition;runtime.OpenMenu();yield return new WaitForSecondsRealtime(.2f);
            if(runtime.Conditioning.Remaining!=remaining||life.Nutrition!=nutrition){Finish(false,"Paused conditioning advanced course or nutrition.");yield break;}
            yield return SendRoutineEscape();
            if(runtime.IsMenuOpen||game.Director.IsPaused||!life.IsConditioning){Finish(false,$"Closing conditioning menu: menu={runtime.IsMenuOpen}, paused={game.Director.IsPaused}, conditioning={life.IsConditioning}, state={runtime.Conditioning.State}, pauseChanged={game.Director.PauseChangedThisFrame}.");yield break;}
            game.Player.GetComponentInChildren<CityCameraRig>().SetMode(CityCameraMode.Chase);
            for(int stop=0;stop<3;stop++)
            {
                body.enabled=false;game.Player.position=runtime.ConditioningTarget+Vector3.up*.1f;body.enabled=true;Physics.SyncTransforms();
                yield return new WaitForSeconds(.45f);
                if(stop==1)
                {
                    string capture=Argument("-vktRoutineScreenshot","");
                    if(!string.IsNullOrEmpty(capture)&&!CaptureRenderedFrame(game,capture,out string captureError)){Finish(false,captureError);yield break;}
                }
                float end=Time.realtimeSinceStartup+4;
                while(runtime.Conditioning.Checkpoint<=stop&&Time.realtimeSinceStartup<end)yield return null;
                if(runtime.Conditioning.Checkpoint!=stop+1){Finish(false,"Conditioning checkpoint did not register continuous physical dwell.");yield break;}
            }
            if(life.IsConditioning||life.Experience!=xp+20||life.Workouts!=workouts||runtime.Conditioning.TryClaim(life)||runtime.StartConditioning())
            {Finish(false,"Conditioning reward, bench separation or rest gating failed.");yield break;}
            controller.SaveNow();
            if(!new CitySaveStore(controller.SavePath).TryLoad(out CityProfileData saved,out _)||saved.lifestyle.experience!=life.Experience||saved.lifestyle.workoutCooldown<=0)
            {Finish(false,"Conditioning XP/rest did not survive save.");yield break;}
            life.Tick(91,false);if(!runtime.StartConditioning()){Finish(false,"Rested conditioning restart failed.");yield break;}
            xp=life.Experience;life.ReportCrime();yield return new WaitForSeconds(.1f);
            if(life.IsConditioning||runtime.Conditioning.State!=CityConditioningState.Cancelled||life.Experience!=xp){Finish(false,"Crime did not cancel conditioning without rewards.");yield break;}
            life.TickPolice(CityLifestyle.EscapeDuration+.1,false,false);
            life.Tick(91,false);xp=life.Experience;
            if(!runtime.StartConditioning()){Finish(false,"Escape-cancel exercise could not start.");yield break;}
            yield return SendRoutineEscape();
            if(life.IsConditioning||game.Director.IsPaused||life.Experience!=xp){Finish(false,"Exercise ESC did not cancel without toggling pause or awarding XP.");yield break;}
            game.Audio.SetMuted(false);yield return new WaitForSecondsRealtime(.2f);
            if(!game.Audio.PlayCue(CityAudioCue.Checkpoint)){Finish(false,"Feedback cue did not start.");yield break;}
            yield return new WaitForSecondsRealtime(.03f);if(!game.Audio.CuePlaying){Finish(false,"Feedback source did not play.");yield break;}
            game.Director.SetPaused(true);yield return new WaitForSecondsRealtime(.05f);
            if(game.Audio.CuePlaying||game.Audio.PlayCue(CityAudioCue.Success)){Finish(false,"Feedback cue survived pause.");yield break;}
            game.Director.SetPaused(false);game.Audio.SetMuted(true);yield return new WaitForSecondsRealtime(.2f);
            if(game.Audio.PlayCue(CityAudioCue.Success)){Finish(false,"Muted feedback accepted a cue.");yield break;}
            game.Audio.SetMuted(false);game.Audio.PlayCue(CityAudioCue.Success);game.Audio.SendMessage("OnApplicationPause",true);yield return new WaitForSecondsRealtime(.03f);
            if(game.Audio.CuePlaying||game.Audio.PlayCue(CityAudioCue.Checkpoint)){Finish(false,"Suspended feedback accepted a cue.");yield break;}
            game.Audio.SendMessage("OnApplicationPause",false);controller.SaveNow();
            var audioPanel=game.GetComponent<CityMusicPanel>();audioPanel.Toggle();yield return null;
            if(!audioPanel.IsOpen||game.Director.IsPaused||!game.Audio.TryPlayDiagnostic())
            {Finish(false,"Non-modal music audio probe was unavailable or paused the world.");yield break;}
            yield return new WaitForSecondsRealtime(.03f);
            if(!game.Audio.DiagnosticPlaying){Finish(false,"Menu audio probe did not play.");yield break;}
            game.Audio.SetMuted(true);
            if(game.Audio.DiagnosticPlaying||game.Audio.TryPlayDiagnostic())
            {Finish(false,"Muted menu audio probe kept playing.");yield break;}
            game.Audio.SetMuted(false);game.Audio.TryPlayDiagnostic();audioPanel.Close();
            if(game.Audio.DiagnosticPlaying||game.Director.IsPaused)
            {Finish(false,"Closing audio menu leaked probe playback or pause.");yield break;}
            Debug.Log("VKT_AUDIO_PROBE_VERIFIED nonModalMusic=true muteStops=true closeStops=true voices="+game.Audio.LoadedVoiceClips);
            Debug.Log("VKT_ROUTINE_VERIFIED checkpoints=3 rewardXP=20 originalProfileVersion="+CityProgression.CurrentProfileVersion+" wantedContactGate=true audioPauseMuteSuspend=true");
            routineVerified=true;
        }
        private static IEnumerator SendRoutineEscape()
        {
            var device=CityInputFlowValidation.CreateValidationKeyboard();
            UnityEngine.InputSystem.InputSystem.QueueStateEvent(device,new UnityEngine.InputSystem.LowLevel.KeyboardState(UnityEngine.InputSystem.Key.Escape));
            yield return null;
            yield return new WaitForEndOfFrame();
            UnityEngine.InputSystem.InputSystem.QueueStateEvent(device,new UnityEngine.InputSystem.LowLevel.KeyboardState());
            yield return null;
            yield return new WaitForEndOfFrame();
            UnityEngine.InputSystem.InputSystem.RemoveDevice(device);
        }
        private static bool TextJournalFixture(UnityEngine.UI.ScrollRect scroll)
        {
            var text=scroll.content.GetComponent<UnityEngine.UI.Text>();string original=text.text;
            text.text=original+new string('\n',50)+"KRAJ PROVERE";Canvas.ForceUpdateCanvases();scroll.verticalNormalizedPosition=0;
            bool correct=scroll.content.rect.height>scroll.viewport.rect.height&&Mathf.Abs(scroll.verticalNormalizedPosition)<=.01f;
            text.text=original;Canvas.ForceUpdateCanvases();scroll.verticalNormalizedPosition=1;
            return correct;
        }

        private static bool VerifyEnhanced(CityGameBootstrap game, out string details)
        {
            try
            {
                CityMusicPlayer music = game.GetComponent<CityMusicPlayer>();
                Ensure(music != null && music.Count >= 3, "Music presets were not initialized.");
                CityMusicPanel musicPanel = game.GetComponent<CityMusicPanel>();
                game.GetComponent<CityProgressionController>().ClosePanel();
                musicPanel.Toggle(); Ensure(musicPanel.IsOpen && !game.Director.IsPaused && Time.timeScale > 0, "Music mini player interrupted gameplay.");
                musicPanel.Close(); Ensure(!game.Director.IsPaused, "Music overlay left the game paused.");
                Ensure(Resources.Load<Texture2D>("Art/CityFacades") != null, "City facade atlas is missing.");
                Ensure(CityPlantEncyclopedia.Count == 16 && Resources.Load<Texture2D>("Art/BotanicalAtlas") != null, "Botanical cards or atlas are missing.");
                CityOsmStreets streets = game.GetComponent<CityOsmStreets>();
                Ensure(streets != null && streets.SegmentCount > 0 && streets.Source.buildings.Length > 0, "Actual OSM streets/footprints were not composed.");
                Ensure(streets.WallColliderCount > 0, "Building collision was not composed.");
                Ensure(streets.SegmentCount > 200 && streets.Source.buildings.Length > 40 && streets.ParkCount > 0, "Expanded city roads, buildings or parks were not composed.");
                CityVisualPolish polish=game.GetComponent<CityVisualPolish>();
                Ensure(polish!=null&&polish.RoundedProps>=4&&polish.SculptedActors==2&&polish.ImportedActors==1,"Rounded props or reviewed character model were not installed.");
                if (game.Hideout.IsInside) Ensure(game.Hideout.Travel(false), "Could not leave hideout for street job.");
                int before = game.Session.Cash;
                CityProgressionController controller = game.GetComponent<CityProgressionController>();
                controller.OpenPanel();
                CityPhonePanel phone = FindAnyObjectByType<CityPhonePanel>(FindObjectsInactive.Include);
                phone.OpenMap();
                Ensure(phone.IsOpen && game.Director.IsPaused && phone.transform.Find("MapPanel").gameObject.activeSelf, "City map did not own the paused phone state.");
                phone.OpenJobs();
                Ensure(phone.transform.Find("JobsPanel").gameObject.activeSelf && game.Director.IsPaused, "Job journal did not open and pause the player.");
                phone.transform.Find("JobsPanel/AcceptJob").GetComponent<UnityEngine.UI.Button>().onClick.Invoke();
                Ensure(game.Director.Jobs.IsActive && !phone.IsOpen && !game.Director.IsPaused, "Journal did not accept the job and resume the player.");
                CityStreetJobOffer offer = game.Director.Jobs.Offer;
                for (int i = 0; i < 3; i++) Visit(game, offer.StationAt(i));
                Ensure(!game.Director.Jobs.IsActive && game.Session.Cash == before + offer.CashReward, "Street job payout or route failed.");
                Ensure(!game.Director.TryInteract(offer.StationAt(2)), "Street job paid twice.");
                game.GetComponent<CityProgressionController>().SaveNow();
                CitySaveStore jobStore = new CitySaveStore(controller.SavePath);
                Ensure(jobStore.TryLoad(out CityProfileData jobSaved, out _) && jobSaved.completedStreetJobs == game.Director.Jobs.CompletedJobs && jobSaved.completedStreetJobs == 1,
                    "Completed street-job progress did not survive save/reload.");
                InteractWith(game, game.Hideout.Entrance, .9f);
                game.Player.GetComponentInChildren<Camera>().transform.LookAt((game.Hideout.PlotTransform(0).position + game.Hideout.PlotTransform(3).position) / 2 + Vector3.up * .5f);
                CityParkDetails parkDetails=game.GetComponent<CityParkDetails>();
                Ensure(parkDetails!=null && parkDetails.PlacedBenches>0 && parkDetails.PlacedTrees>0,"Park furniture was not composed.");
                details = $"Enhanced: {streets.SegmentCount} OSM segments, {streets.Source.buildings.Length} building footprints, {streets.ParkCount} green polygons, {streets.WallColliderCount} wall colliders, {parkDetails.PlacedBenches} benches/{parkDetails.PlacedTrees} trees, city map, 16 strain cards, music overlay, job journal, physical street-job payout and durable story progress passed.";
                return true;
            }
            catch (Exception error) { details = error.GetType().Name + ": " + error.Message; return false; }
        }
        private static bool BeginCombatCheck(CityGameBootstrap game, out string error)
        {
            try
            {
                game.GetComponent<CityProgressionController>().ClosePanel();
                if (game.Hideout.IsInside) Ensure(game.Hideout.Travel(false), "Could not leave hideout for combat.");
                CityCombatRuntime combat = game.Combat;
                Ensure(combat != null && combat.Pickups.Length == 4, "Combat pickups missing.");
                InteractWith(game, combat.Pickups[0].transform, .3f);
                InteractWith(game, combat.Pickups[3].transform, .3f);
                Ensure(combat.Inventory.StoneCount == 1 && combat.Inventory.HasStick, "Physical pickup inventory mismatch.");
                CityProgressionController controller = game.GetComponent<CityProgressionController>();
                controller.OpenPanel();
                Ensure(!combat.TryAttack() && combat.Inventory.CooldownRemaining == 0, "Paused UI allowed an attack.");
                controller.ClosePanel();
                Ensure(combat.Inventory.SelectWeapon(CityWeapon.Stone), "Could not select collected stone.");
                CharacterController body = game.Player.GetComponent<CharacterController>(); body.enabled = false;
                game.Player.position = combat.PracticeWindow.transform.position + new Vector3(0, -1.3f, -4); body.enabled = true;
                game.Player.GetComponentInChildren<Camera>().transform.LookAt(combat.PracticeWindow.transform.position);
                Physics.SyncTransforms();
                Ensure(combat.TryAttack() && combat.Inventory.StoneCount == 0 && combat.ActiveProjectiles == 1, "Stone did not launch or consume one inventory item.");
                Ensure(!combat.TryAttack(), "Cooldown allowed a second projectile.");
                error = ""; return true;
            }
            catch (Exception failure) { error = "Combat setup: " + failure.Message; return false; }
        }
        private static bool FinishCombatCheck(CityGameBootstrap game, out string error)
        {
            CityHazard opponent = null; bool wasEnabled = false; GameObject blocker = null;
            try
            {
                CityCombatRuntime combat = game.Combat;
                Ensure(combat.ActiveProjectiles == 0 && !combat.PracticeWindow.ReceiveImpact(12), "Glass repeated its break or projectile remained active.");
                foreach (CityHazard actor in FindObjectsByType<CityHazard>(FindObjectsSortMode.None))
                    if (actor.Kind == CityHazardKind.HostileAdult) { opponent = actor; break; }
                Ensure(opponent != null, "Adult encounter missing.");
                wasEnabled = opponent.enabled; opponent.enabled = false;
                combat.Inventory.Tick(2);
                Ensure(combat.Inventory.SelectWeapon(CityWeapon.Stick), "Stick selection failed.");
                CharacterController body = game.Player.GetComponent<CharacterController>(); body.enabled = false;
                game.Player.position = opponent.transform.position + new Vector3(0, 0, -1.5f); body.enabled = true;
                game.Player.GetComponentInChildren<Camera>().transform.LookAt(opponent.transform.position + Vector3.up * .9f);
                Physics.SyncTransforms(); float health = opponent.CurrentHitPoints;
                Ensure(combat.TryAttack() && opponent.CurrentHitPoints == health - 15 && !opponent.IsAttacking, "Physical stick hit did not interrupt the opponent.");
                blocker = GameObject.CreatePrimitive(PrimitiveType.Cube);
                blocker.name = "SmokeOnlyCombatOccluder"; blocker.transform.position = opponent.transform.position + new Vector3(0, 1, -.75f);
                blocker.transform.localScale = new Vector3(.8f, 2, .2f); Physics.SyncTransforms();
                combat.Inventory.Tick(2); health = opponent.CurrentHitPoints; float heat = game.Session.Heat;
                Ensure(combat.TryAttack() && opponent.CurrentHitPoints == health && game.Session.Heat == heat, "Melee passed through an intervening wall.");
                game.GetComponent<CityProgressionController>().SaveNow();
                error = ""; return true;
            }
            catch (Exception failure) { error = "Combat result: " + failure.Message; return false; }
            finally
            {
                if (blocker != null) { blocker.GetComponent<Collider>().enabled = false; Destroy(blocker); }
                if (opponent != null) opponent.enabled = wasEnabled;
            }
        }
        private static bool PrepareProjectileCheck(CityGameBootstrap game, out string error)
        {
            try
            {
                CityCombatRuntime combat = game.Combat;
                CharacterController body = game.Player.GetComponent<CharacterController>(); body.enabled = false;
                game.Player.position = combat.PracticeWindow.transform.position + new Vector3(-7, -1.3f, -7); body.enabled = true;
                game.Player.GetComponentInChildren<Camera>().transform.rotation = Quaternion.LookRotation(Vector3.up, Vector3.forward);
                for (int i = combat.Inventory.StoneCount; i < CityCombat.MaxStones; i++) Ensure(combat.Inventory.TryCollectStone(), "Could not prepare bounded pool inventory.");
                Ensure(combat.Inventory.SelectWeapon(CityWeapon.Stone), "Pool stone selection failed.");
                for (int i = 0; i < 8; i++) { combat.Inventory.Tick(2); Ensure(combat.TryAttack(), "Projectile pool filled before its configured capacity."); }
                combat.Inventory.Tick(2); int remaining = combat.Inventory.StoneCount;
                Ensure(combat.ActiveProjectiles == 8 && !combat.TryAttack() && combat.Inventory.StoneCount == remaining, "Full projectile pool consumed ammunition or exceeded its bound.");
                error = ""; return true;
            }
            catch (Exception failure) { error = "Projectile check: " + failure.Message; return false; }
        }

        private static bool Verify(CityGameBootstrap game, out string details)
        {
            try
            {
                string temporaryRoot = Path.GetFullPath(Path.Combine(Path.GetTempPath(), "VKT-validation")) + Path.DirectorySeparatorChar;
                Ensure(CityGameBootstrap.IsSmokeValidation && Path.GetFullPath(game.GetComponent<CityProgressionController>().SavePath).StartsWith(temporaryRoot, StringComparison.OrdinalIgnoreCase),
                    "Player validation requires an isolated temporary profile.");
                Ensure(game.Progression.CompletedContracts == 0, "Validation must start with a fresh isolated profile.");
                game.Player.GetComponent<CityPlayerController>().enabled = false;
                for (int contract = 0; contract < 3; contract++)
                {
                    if (contract > 0) Ensure(game.Director.TryNextContract(), "Next route did not unlock.");
                    for (int station = 0; station < 3; station++) Visit(game, station);
                    Ensure(!game.Director.TryInteract(2), "Completed route paid twice.");
                }
                CityProgression p = game.Progression;
                Ensure(p.CompletedContracts == 3 && p.Session.Cash == 5600 && p.Session.Reputation == 55, "Campaign reward or unlock state mismatch.");
                CityProgressionController controller = game.GetComponent<CityProgressionController>();
                Ensure(controller.Perform(p.BuySeed) && controller.Perform(p.Plant), "Planting failed.");
                p.Tick(30); // Deterministic game-clock advance; no offline growth is inferred.
                Ensure(controller.Perform(p.Harvest) && controller.Perform(p.Sell), "Harvest/trade failed.");
                Ensure(controller.Perform(p.Recruit) && controller.Perform(p.Recruit), "Crew failed.");
                for (int i = 0; i < 3; i++) Ensure(controller.Perform(p.ClaimTerritory), "District claim failed.");
                Ensure(p.Session.Cash == 2915 && p.Crew == 2 && p.Territories == 3, "Final economy balance mismatch.");
                CitySaveStore store = new CitySaveStore(controller.SavePath);
                Ensure(store.TryLoad(out CityProfileData saved, out _) && saved.cash == 2915 && saved.completedContracts == 3 && saved.territories == 3,
                    "Standalone-player save restoration failed.");
                controller.OpenPanel();
                Ensure(game.Director.IsPaused, "Phone did not pause the player.");
                if (Array.Exists(Environment.GetCommandLineArgs(), arg => arg == "-vktHideout"))
                {
                    CityPhonePanel phone = FindAnyObjectByType<CityPhonePanel>(FindObjectsInactive.Include);
                    phone.OpenCatalog();
                    phone.transform.Find("CatalogPanel/Variety_1").GetComponent<UnityEngine.UI.Button>().onClick.Invoke();
                    Ensure(game.Hideout.SelectedVariety == 1, "Catalog row did not select the physical garden variety.");
                    phone.transform.Find("CatalogPanel/Variety_0").GetComponent<UnityEngine.UI.Button>().onClick.Invoke();
                    Ensure(game.Hideout.SelectedVariety == 0, "Catalog row did not reset the physical garden variety.");
                    controller.ClosePanel();
                    InteractWith(game, game.Hideout.Entrance, 0.9f);
                    Ensure(game.Hideout.IsInside, "Player could not enter hideout.");
                    for (int i = 0; i < CityGarden.PlotCount; i++)
                    {
                        Ensure(controller.Perform(() => p.Garden.BuySeed(0)), "Player catalog seed purchase failed.");
                        InteractWith(game, game.Hideout.PlotTransform(i), 0.4f);
                        InteractWith(game, game.Hideout.PlotTransform(i), 0.4f);
                    }
                    Ensure(game.Session.Cash == 2675 && p.Garden.Plot(3).moistureSeconds > 7100, "Player garden care failed.");
                    CitySaveStore gardenStore = new CitySaveStore(controller.SavePath);
                    Ensure(gardenStore.TryLoad(out CityProfileData gardenSaved, out _) && gardenSaved.garden.plots[3].planted,
                        "Player garden save restore failed.");
                    InteractWith(game, game.Hideout.Exit, 1.05f);
                    Ensure(!game.Hideout.IsInside, "Player could not exit hideout.");
                    InteractWith(game, game.Hideout.Entrance, 0.9f);
                    Ensure(game.Hideout.IsInside, "Could not reenter hideout before fatal damage.");
                    Ensure(game.Vitals.ApplyDamage(100) && !game.Vitals.IsAlive, "Player fatal damage did not enter dead state.");
                    Ensure(!game.Player.GetComponent<CityPlayerInteraction>().TryInteract(), "Dead player interacted.");
                    Ensure(game.Vitals.Respawn() && !game.Vitals.ApplyDamage(10), "Player hospital or respawn protection failed.");
                    Ensure(!game.Hideout.IsInside && !game.Hideout.Exit.gameObject.activeInHierarchy, "Hospital did not reset hideout and deactivate interior.");
                    InteractWith(game, game.Hideout.Entrance, 0.9f);
                    Ensure(game.Hideout.IsInside, "Hospital return could not reenter hideout.");
                    InteractWith(game, game.Hideout.Exit, 1.05f);
                    Ensure(!game.Hideout.IsInside, "Hospital return could not exit hideout.");
                    InteractWith(game, game.Hideout.Entrance, 0.9f);
                    Camera roomCamera = game.Player.GetComponentInChildren<Camera>();
                    roomCamera.transform.LookAt((game.Hideout.PlotTransform(0).position + game.Hideout.PlotTransform(3).position) / 2 + Vector3.up * 0.5f);
                }
                details = "Actual Windows player: terrain loaded, nine physics interactions, three contracts, growth/trade, crew, district campaign and durable save restore passed.";
                if (game.Hideout.IsInside) details += " Catalog seed purchases, hideout entry/exit, four physical plants, watering, garden save restoration, death and hospital respawn passed.";
                return true;
            }
            catch (Exception error) { details = error.GetType().Name + ": " + error.Message; return false; }
        }

        private static void Visit(CityGameBootstrap game, int index)
        {
            CityMission station = game.Director.StationAt(index);
            CharacterController body = game.Player.GetComponent<CharacterController>();
            body.enabled = false;
            game.Player.position = station.transform.position + new Vector3(0, -0.73f, -2.5f);
            body.enabled = true;
            Camera camera = game.Player.GetComponentInChildren<Camera>();
            camera.transform.LookAt(station.transform.position);
            Physics.SyncTransforms();
            Ensure(game.Player.GetComponent<CityPlayerInteraction>().TryInteract(), "Physics interaction failed at station " + (index + 1));
        }

        private static void Ensure(bool condition, string message) { if (!condition) throw new InvalidOperationException(message); }
        private static void InteractWith(CityGameBootstrap game, Transform target, float offset)
        {
            CharacterController body = game.Player.GetComponent<CharacterController>(); body.enabled = false;
            game.Player.position = target.position + new Vector3(0, -offset, target == game.Hideout.Exit ? 2.5f : -2.5f); body.enabled = true;
            game.Player.GetComponentInChildren<Camera>().transform.LookAt(target.position);
            Physics.SyncTransforms();
            Ensure(game.Player.GetComponent<CityPlayerInteraction>().TryInteract(), "Player prop interaction failed: " + target.name);
        }
        private static bool ContainsRenderedPixels(string file)
        {
            Texture2D texture = new Texture2D(2, 2);
            try
            {
                if (!ImageConversion.LoadImage(texture, File.ReadAllBytes(file))) return false;
                Color32[] pixels = texture.GetPixels32();
                int visible = 0;
                foreach (Color32 pixel in pixels) if (pixel.r > 8 || pixel.g > 8 || pixel.b > 8) visible++;
                return visible > pixels.Length / 50;
            }
            finally { Destroy(texture); }
        }
        private static bool CaptureRenderedFrame(CityGameBootstrap game, string file, out string error)
        {
            error = "";
            Camera camera = game.Player.GetComponentInChildren<Camera>();
            RenderTexture destination = new RenderTexture(1280, 720, 24, RenderTextureFormat.ARGB32);
            Texture2D pixels = new Texture2D(1280, 720, TextureFormat.RGBA32, false);
            RenderTexture originalTarget = camera.targetTexture, originalActive = RenderTexture.active;
            Canvas[] canvases = FindObjectsByType<Canvas>(FindObjectsInactive.Exclude);
            RenderMode[] modes = new RenderMode[canvases.Length];
            Camera[] cameras = new Camera[canvases.Length];
            float[] distances = new float[canvases.Length];
            try
            {
                destination.Create();
                camera.targetTexture = destination;
                for (int i = 0; i < canvases.Length; i++)
                {
                    modes[i] = canvases[i].renderMode; cameras[i] = canvases[i].worldCamera; distances[i] = canvases[i].planeDistance;
                    canvases[i].renderMode = RenderMode.ScreenSpaceCamera;
                    canvases[i].worldCamera = camera;
                    canvases[i].planeDistance = camera.nearClipPlane + 0.001f;
                }
                Canvas.ForceUpdateCanvases();
                // The installed URP supports this public offscreen rendering request.
                RenderPipeline.SubmitRenderRequest(camera, new UniversalRenderPipeline.SingleCameraRequest { destination = destination });
                RenderTexture.active = destination;
                pixels.ReadPixels(new Rect(0, 0, 1280, 720), 0, 0);
                pixels.Apply();
                File.WriteAllBytes(file, pixels.EncodeToPNG());
                return true;
            }
            catch (Exception exception) { error = "Offscreen player capture failed: " + exception.GetType().Name; return false; }
            finally
            {
                for (int i = 0; i < canvases.Length; i++)
                { canvases[i].renderMode = modes[i]; canvases[i].worldCamera = cameras[i]; canvases[i].planeDistance = distances[i]; }
                camera.targetTexture = originalTarget; RenderTexture.active = originalActive;
                destination.Release(); Destroy(destination); Destroy(pixels);
            }
        }
        private static string Argument(string key, string fallback)
        {
            string[] args = Environment.GetCommandLineArgs();
            for (int i = 0; i + 1 < args.Length; i++) if (args[i] == key) return args[i + 1];
            return fallback;
        }
        private static void Finish(bool passed, string details)
        {
            string output = Path.GetFullPath(Argument("-vktReport", Path.Combine(Path.GetTempPath(), "vkt-player-report.json")));
            Directory.CreateDirectory(Path.GetDirectoryName(output));
            File.WriteAllText(output, JsonUtility.ToJson(new Report { passed = passed, details = details, product = Application.productName }, true));
            Debug.Log("VKT_PLAYER_VALIDATION: " + (passed ? "PASS " : "FAIL ") + details);
            Application.Quit(passed ? 0 : 1);
        }
    }
}
#endif
