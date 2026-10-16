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
        [Serializable] private sealed class Report { public bool passed; public string details; public string product; }
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Install()
        {
            if (!Array.Exists(Environment.GetCommandLineArgs(), arg => arg == "-vktSmoke")) return;
            Application.runInBackground = true;
            Application.targetFrameRate = 30;
            new GameObject("VKTPlayerValidation").AddComponent<CityPlayerValidation>();
        }

        private IEnumerator Start()
        {
            float deadline = Time.realtimeSinceStartup + 90;
            CityGameBootstrap game = null;
            while (Time.realtimeSinceStartup < deadline)
            {
                game = FindAnyObjectByType<CityGameBootstrap>();
                if (game != null && game.WorldReady && (game.Intro == null || !game.Intro.IsActive)) break;
                yield return new WaitForSecondsRealtime(0.25f);
            }
            if (game == null || !game.WorldReady) { Finish(false, "City terrain did not become ready."); yield break; }
            yield return new WaitForSecondsRealtime(1);
            if (!Verify(game, out string details)) { Finish(false, details); yield break; }
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
            Finish(true, details);
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
            lifestyleVerified=true;
        }

        private static bool VerifyEnhanced(CityGameBootstrap game, out string details)
        {
            try
            {
                CityMusicPlayer music = game.GetComponent<CityMusicPlayer>();
                Ensure(music != null && music.Count >= 3, "Music presets were not initialized.");
                CityMusicPanel musicPanel = game.GetComponent<CityMusicPanel>();
                game.GetComponent<CityProgressionController>().ClosePanel();
                musicPanel.Toggle(); Ensure(musicPanel.IsOpen && game.Director.IsPaused, "Music overlay did not pause input.");
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
