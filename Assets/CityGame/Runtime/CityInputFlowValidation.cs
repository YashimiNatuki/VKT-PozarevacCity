#if DEVELOPMENT_BUILD || UNITY_EDITOR
using System;
using System.Collections;
using System.IO;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.UI;

namespace Pozarevac.CityGame
{
    // Synthetic input in an isolated native-player profile; never injects OS input or changes personal saves.
    public sealed class CityInputFlowValidation : MonoBehaviour
    {
        [Serializable] private sealed class Report
        {
            public bool passed,phoneBeforeContract,mapBeforeContract,musicNonModal,movementAfterMusic,legsAnimated,benchStarted,benchCompleted,dockVisible,helpReturnsControl,onboardingCreated;
            public float distanceAfterMusic,leftLegDegrees,rightLegDegrees;
            public float peakObservedSpeed;
            public bool gaitReady;
            public bool musicControlsClear;
            public bool signalBlockedByMenu,signalBlockedByWall,signalReachesOfficer,signalCooldown,signalDoesNotPauseOrAddHeat;
            public bool hideoutScoreFades,hideoutScoreMutes;
            public bool helpAudioTextFits,hideoutMusicPreferenceMutes,disabledSignalBlocked;
            public bool mapPreservesAudio,muteShortcutWorks;
            public bool gardenPacked,gardenDelivery,gardenPause,gardenDiskCargo,gardenDiskReceipt,gardenSinglePayout,gardenTrainingGate,gardenPhoneCancellation;
            public string error="",input="Synthetic Unity Input System keyboard and EventSystem submit; not human input.";
        }
        private readonly Report report=new Report();
        private Keyboard keyboard;
        private static bool keyboardLayoutRegistered;
        internal static Keyboard CreateValidationKeyboard()
        {
            // Only the synthetic device accepts queued events while the native validation window lacks focus.
            // Never change background behavior for real keyboards, mice or touchscreens.
            const string layout="VKTValidationKeyboard";
            if(!keyboardLayoutRegistered)
            {
                InputSystem.RegisterLayout("{\"name\":\"VKTValidationKeyboard\",\"extend\":\"Keyboard\",\"runInBackground\":\"enabled\"}");
                keyboardLayoutRegistered=true;
            }
            var device=(Keyboard)InputSystem.AddDevice(layout);
            if(!device.enabled)InputSystem.EnableDevice(device);
            Debug.Log($"VKT_SYNTHETIC_KEYBOARD enabled={device.enabled} background={device.canRunInBackground} focused={Application.isFocused}");
            return device;
        }
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Install()
        {
            if(Array.IndexOf(Environment.GetCommandLineArgs(),"-vktInputFlow")<0)return;
            if(Array.IndexOf(Environment.GetCommandLineArgs(),"-vktSmoke")<0)return;
            Application.runInBackground=true;
            new GameObject("VKT_InputFlowValidation").AddComponent<CityInputFlowValidation>();
        }
        private IEnumerator Start()
        {
            keyboard=CreateValidationKeyboard();
            CityGameBootstrap game=null;float deadline=Time.realtimeSinceStartup+90;
            while(Time.realtimeSinceStartup<deadline)
            {
                game=FindAnyObjectByType<CityGameBootstrap>();
                if(game!=null&&game.WorldReady)break;
                yield return null;
            }
            if(game==null||!game.WorldReady){Finish("World not ready.");yield break;}
            game.Intro?.Skip();yield return new WaitForSecondsRealtime(.3f);
            var progress=game.GetComponent<CityProgressionController>();var phone=game.GetComponentInChildren<CityPhonePanel>();
            if(Array.IndexOf(Environment.GetCommandLineArgs(),"-vktOnboardingFlow")>=0)
            {
                yield return new WaitForSecondsRealtime(.2f);
                if(!phone.IsOpen||game.Progression.IsCreated){Finish("Fresh-profile character entry was not shown.");yield break;}
                var field=phone.GetComponentInChildren<InputField>();field.text="Flow Player";Submit("Create");yield return null;
                report.onboardingCreated=game.Progression.IsCreated&&game.GetComponent<CityControlsDock>().HelpOpen;
                if(!report.onboardingCreated){Finish("Character UI did not hand off to controls help.");yield break;}
                yield return Press(Key.Escape);
            }
            else report.onboardingCreated=true;
            var dock=game.GetComponent<CityControlsDock>();
            report.dockVisible=dock!=null&&dock.transform.Find("VKT_Controls/QuickControls").gameObject.activeInHierarchy;
            yield return Press(Key.F1);bool helpShown=dock!=null&&dock.HelpOpen&&game.Director.IsPaused;
            report.signalBlockedByMenu=helpShown&&!game.GetComponent<CityStreetSignals>().TryWhistle();
            if(helpShown)Capture(game,"help");
            if(helpShown)
            {
                for(int page=0;page<3;page++){Submit("DALJE");yield return null;}
                Canvas.ForceUpdateCanvases();
                var helpText=dock.transform.Find("VKT_Controls/HelpBackdrop/HelpCard/Label").GetComponent<Text>();
                report.helpAudioTextFits=helpText.preferredHeight<=helpText.rectTransform.rect.height;
                Capture(game,"help-audio");
            }
            yield return Press(Key.Escape);report.helpReturnsControl=helpShown&&!dock.HelpOpen&&!game.Director.IsPaused;
            yield return Press(Key.P);
            report.phoneBeforeContract=phone.IsOpen&&game.Progression.CompletedContracts==0;
            if(phone.IsOpen)Capture(game,"phone");
            if(phone.IsOpen)yield return Press(Key.Escape);
            game.Audio.SetMuted(false);yield return Press(Key.M);
            report.mapPreservesAudio=!game.Audio.Muted;
            var map=phone.transform.Find("MapPanel");report.mapBeforeContract=phone.IsOpen&&map!=null&&map.gameObject.activeSelf;
            if(report.mapBeforeContract)Capture(game,"map");
            if(phone.IsOpen)yield return Press(Key.Escape);
            yield return Press(Key.F10);bool muted=game.Audio.Muted;
            yield return Press(Key.F10);report.muteShortcutWorks=muted&&!game.Audio.Muted;
            yield return Press(Key.F6);
            var music=game.GetComponent<CityMusicPanel>();report.musicNonModal=music.IsOpen&&!game.Director.IsPaused&&Time.timeScale>0;
            Canvas.ForceUpdateCanvases();report.musicControlsClear=MusicClearsTouchControls(game);
            if(music.IsOpen)Capture(game,"music");
            yield return Press(Key.Escape);
            // Original reproduction: closing music then pressing W must return control to the player.
            var camera=game.Player.GetComponentInChildren<CityCameraRig>();camera.SetMode(CityCameraMode.Chase);
            yield return new WaitForSecondsRealtime(.3f);
            Transform left=null,right=null;
            var actorMotion=game.Player.GetComponentInChildren<CityActorMotion>();report.gaitReady=actorMotion!=null&&actorMotion.GaitReady;
            foreach(Transform bone in game.Player.GetComponentsInChildren<Transform>())
            {if(bone.name=="LeftLeg")left=bone;if(bone.name=="RightLeg")right=bone;}
            Vector3 start=game.Player.position;Quaternion l=left!=null?left.localRotation:Quaternion.identity,r=right!=null?right.localRotation:Quaternion.identity;
            InputSystem.QueueStateEvent(keyboard,new KeyboardState(Key.W));
            float walkUntil=Time.realtimeSinceStartup+1.2f;
            int walkingFrames=0;
            while(Time.realtimeSinceStartup<walkUntil)
            {
                // Cosmetic gait is applied in LateUpdate. Inspect the rendered pose, not the restored pre-animation pose.
                yield return new WaitForEndOfFrame();
                if(left!=null)report.leftLegDegrees=Mathf.Max(report.leftLegDegrees,Quaternion.Angle(l,left.localRotation));
                if(right!=null)report.rightLegDegrees=Mathf.Max(report.rightLegDegrees,Quaternion.Angle(r,right.localRotation));
                if(actorMotion!=null)report.peakObservedSpeed=Mathf.Max(report.peakObservedSpeed,actorMotion.ObservedSpeed);
                walkingFrames++;if(walkingFrames==20)Capture(game,"walking-stride");
            }
            InputSystem.QueueStateEvent(keyboard,new KeyboardState());yield return null;
            report.distanceAfterMusic=Vector3.Distance(start,game.Player.position);
            report.movementAfterMusic=report.distanceAfterMusic>1&&!game.Director.IsPaused&&!music.IsOpen;
            report.legsAnimated=report.leftLegDegrees>8&&report.rightLegDegrees>8;
            Capture(game,"walking");
            var life=game.GetComponent<CityLifestyleRuntime>();
            if(game.Director.IsPaused)game.Director.SetPaused(false);
            var body=game.Player.GetComponent<CharacterController>();body.enabled=false;game.Player.position=life.Gym.position+Vector3.back*2;body.enabled=true;Physics.SyncTransforms();
            yield return Press(Key.L);Submit("TRENING / kod klupe");yield return new WaitForSecondsRealtime(.3f);
            report.benchStarted=life.Life.IsTraining;
            if(report.benchStarted)
            {
                int before=life.Life.Workouts;
                for(int i=0;i<CityLifestyle.RequiredReps&&life.Life.IsTraining;i++)
                {
                    float phaseDeadline=Time.realtimeSinceStartup+5;
                    while(life.Life.IsTraining&&life.Bench.Phase!=CityBenchPhase.Ready&&Time.realtimeSinceStartup<phaseDeadline)yield return null;
                    if(!life.Life.IsTraining||life.Bench.Phase!=CityBenchPhase.Ready)break;
                    yield return Press(Key.F);
                    phaseDeadline=Time.realtimeSinceStartup+5;
                    while(life.Life.IsTraining&&life.Bench.Phase!=CityBenchPhase.PressWindow&&Time.realtimeSinceStartup<phaseDeadline)yield return null;
                    if(!life.Life.IsTraining||life.Bench.Phase!=CityBenchPhase.PressWindow)break;
                    yield return Press(Key.F);
                    phaseDeadline=Time.realtimeSinceStartup+5;
                    while(life.Life.IsTraining&&life.Bench.Phase!=CityBenchPhase.Ready&&Time.realtimeSinceStartup<phaseDeadline)yield return null;
                    if(i==1)Capture(game,"bench");
                }
                report.benchCompleted=life.Life.Workouts==before+1;
            }
            yield return ValidateStreetSound(game);
            yield return ValidateGardenOrder(game);
            Finish("");
        }
        private IEnumerator ValidateGardenOrder(CityGameBootstrap game)
        {
            var director=game.Director;var progress=game.GetComponent<CityProgressionController>();
            int attempts=0;
            while(!director.StreetJobsUnlocked&&attempts++<12)
            {
                if(director.Run.State==CityMissionState.Completed)director.TryNextContract();
                else if(!director.TryInteract(director.Run.ObjectiveIndex)){Finish("Garden fixture campaign progression failed.");yield break;}
            }
            if(!director.StreetJobsUnlocked){Finish("Garden fixture did not unlock campaign.");yield break;}
            var garden=game.Progression.Garden;var order=game.Progression.GardenOrder;
            GrowValidationHarvest(garden);int before=garden.Stock(0);
            game.Hideout.SelectVariety(0);game.Hideout.Travel(true);
            yield return null;
            report.gardenPacked=InteractAt(game,game.Hideout.OrderDesk,.7f)&&order.HasCargo&&garden.Stock(0)==before-2;
            if(!report.gardenPacked){Finish("Garden order did not pack real inventory at its physical desk.");yield break;}
            if(new CitySaveStore(progress.SavePath).TryLoad(out var cargo,out _))
            {
                var resumed=new CityProgression(CitySession.Restore(cargo),cargo);
                report.gardenDiskCargo=resumed.GardenOrder.HasCargo&&resumed.Garden.Stock(0)==before-2&&resumed.GardenOrder.CashReward==order.CashReward;
            }
            yield return Press(Key.P);game.GetComponentInChildren<CityPhonePanel>().OpenJobs();
            double time=order.RemainingSeconds;yield return new WaitForSecondsRealtime(.25f);
            report.gardenPause=order.RemainingSeconds==time&&director.IsPaused;
            Capture(game,"garden-order-journal");yield return Press(Key.Escape);
            game.Hideout.Travel(false);
            var body=game.Player.GetComponent<CharacterController>();var life=game.GetComponent<CityLifestyleRuntime>();
            life.Life.Tick(91,false);if(life.Life.Nutrition<35)life.Life.TryEat(false);
            body.enabled=false;game.Player.position=life.Gym.position+Vector3.back*2;body.enabled=true;Physics.SyncTransforms();
            report.gardenTrainingGate=!life.StartWorkout()&&!life.StartBoxing()&&!life.StartConditioning()&&!director.TryAcceptStreetJob();
            report.gardenDelivery=InteractAt(game,director.StationAt(CityGardenOrder.DeliveryStation).transform,.73f)&&order.Phase==CityGardenOrderPhase.Receipt;
            if(new CitySaveStore(progress.SavePath).TryLoad(out var receipt,out _))
            {
                var resumed=new CityProgression(CitySession.Restore(receipt),receipt);
                report.gardenDiskReceipt=resumed.GardenOrder.Phase==CityGardenOrderPhase.Receipt;
            }
            game.Hideout.Travel(true);int cash=game.Session.Cash,reward=order.CashReward,count=order.Completed;
            bool settled=InteractAt(game,game.Hideout.OrderDesk,.7f);
            report.gardenSinglePayout=settled&&!order.TrySettle()&&game.Session.Cash==cash+reward&&order.Completed==count+1&&garden.Stock(0)==before-2;
            Capture(game,"garden-order-settled");
            order.Tick(31);GrowValidationHarvest(garden);int cancelStock=garden.Stock(0),cancelCash=game.Session.Cash;
            bool packed=InteractAt(game,game.Hideout.OrderDesk,.7f);
            yield return Press(Key.P);game.GetComponentInChildren<CityPhonePanel>().OpenJobs();
            Submit("CancelJob");yield return null;
            report.gardenPhoneCancellation=packed&&!order.IsActive&&garden.Stock(0)==cancelStock-2&&game.Session.Cash==cancelCash;
            yield return Press(Key.Escape);game.Hideout.Travel(false);
        }

        private static void GrowValidationHarvest(CityGarden garden)
        {
            // Isolated test profile only: advance the existing fictional garden clock, never change production timers.
            long now=Math.Max(DateTimeOffset.UtcNow.ToUnixTimeSeconds(),garden.Snapshot().lastUtcSeconds+1);
            if(!garden.BuySeed(0)||!garden.PlantFromInventory(0,0,now))throw new InvalidOperationException("Garden validation seed fixture failed.");
            int steps=0;
            while(!garden.IsReady(0)&&steps++<12)
            {garden.Water(0,now);now+=(long)CityGarden.WaterDuration;garden.Advance(now);}
            if(!garden.Harvest(0,now))throw new InvalidOperationException("Garden validation harvest failed.");
        }

        private static bool InteractAt(CityGameBootstrap game,Transform target,float height)
        {
            var body=game.Player.GetComponent<CharacterController>();body.enabled=false;
            game.Player.position=target.position+new Vector3(0,-height,-2.2f);body.enabled=true;
            var rig=game.Player.GetComponentInChildren<CityCameraRig>();rig.SetMode(CityCameraMode.FirstPerson);
            game.Player.GetComponentInChildren<Camera>().transform.LookAt(target.position);Physics.SyncTransforms();
            return game.Player.GetComponent<CityPlayerInteraction>().TryInteract();
        }
        private IEnumerator ValidateStreetSound(CityGameBootstrap game)
        {
            var signals=game.GetComponent<CityStreetSignals>();
            var officer=FindAnyObjectByType<CityPoliceOfficer>();
            var body=game.Player.GetComponent<CharacterController>();
            var movement=game.Player.GetComponent<CityPlayerController>();
            var policeBody=officer.GetComponent<CharacterController>();
            Vector3 playerAt=game.Player.position,officerAt=officer.transform.position;
            // A temporary isolated physics fixture above the city avoids dependence on changing street geometry.
            movement.enabled=false;body.enabled=false;policeBody.enabled=false;
            game.Player.position=playerAt+Vector3.up*50;
            officer.transform.position=game.Player.position+Vector3.forward*6;
            var wall=GameObject.CreatePrimitive(PrimitiveType.Cube);wall.name="ValidationSoundWall";
            wall.transform.position=game.Player.position+Vector3.up*1.4f+Vector3.forward*3;
            wall.transform.localScale=new Vector3(5,4,.4f);Physics.SyncTransforms();
            float heat=game.Session.Heat;
            yield return Press(Key.G);
            report.signalBlockedByWall=signals.Emitted==1&&!officer.InvestigatingSound&&signals.LastListeners==0;
            report.signalCooldown=!signals.TryWhistle();
            Destroy(wall);yield return new WaitForSecondsRealtime(CityStreetSignals.Cooldown+.2f);
            Physics.SyncTransforms();
            Submit(game.Touch.IsVisible?"ZVIŽDI":"ZVIŽDI / G");yield return null;yield return null;
            report.signalReachesOfficer=signals.Emitted==2&&officer.InvestigatingSound&&signals.LastListeners==1&&signals.Playing;
            report.signalDoesNotPauseOrAddHeat=!game.Director.IsPaused&&Time.timeScale>0&&game.Session.Heat<=heat&&!game.Progression.Lifestyle.Wanted;
            signals.enabled=false;report.disabledSignalBlocked=!signals.TryWhistle();signals.enabled=true;
            game.Player.position=playerAt;officer.transform.position=officerAt;
            body.enabled=true;policeBody.enabled=true;movement.enabled=true;Physics.SyncTransforms();
            Capture(game,"street-signal");
            // Use the real authored room entry and existing save progression gate.
            game.Progression.CompleteContract(1);
            bool entered=game.Hideout.Travel(true);
            yield return new WaitForSecondsRealtime(1.2f);
            var ambience=game.GetComponent<CityAmbience>();
            report.hideoutScoreFades=entered&&ambience.HideoutMusicVolume>.03f;
            game.Audio.SetMuted(true);yield return null;yield return null;
            report.hideoutScoreMutes=ambience.HideoutMusicVolume==0;
            game.Audio.SetMuted(false);
            var music=game.GetComponent<CityMusicPlayer>();music.Muted=true;yield return null;yield return null;
            report.hideoutMusicPreferenceMutes=ambience.HideoutMusicVolume==0;
            music.Muted=false;game.Hideout.Travel(false);
        }
        private IEnumerator Press(Key key)
        {
            InputSystem.QueueStateEvent(keyboard,new KeyboardState(key));yield return null;yield return null;
            InputSystem.QueueStateEvent(keyboard,new KeyboardState());yield return null;yield return null;
        }
        private static bool MusicClearsTouchControls(CityGameBootstrap game)
        {
            if(game.Touch==null||!game.Touch.IsVisible)return true;
            var card=(RectTransform)game.transform.Find("VKT_Music_UI/MusicPanel");
            Rect bounds=ScreenBounds(card);int found=0;
            foreach(var control in game.transform.Find("VKT_Touch").GetComponentsInChildren<RectTransform>())
            {
                if(control.name!="SKOK"&&control.name!="TRČI")continue;
                found++;if(bounds.Overlaps(ScreenBounds(control)))return false;
            }
            return found==2;
        }
        private static Rect ScreenBounds(RectTransform rect)
        {
            var corners=new Vector3[4];rect.GetWorldCorners(corners);
            Vector2 lower=RectTransformUtility.WorldToScreenPoint(null,corners[0]);
            Vector2 upper=RectTransformUtility.WorldToScreenPoint(null,corners[2]);
            return Rect.MinMaxRect(lower.x,lower.y,upper.x,upper.y);
        }
        private static void Submit(string name)
        {
            foreach(Button button in FindObjectsByType<Button>(FindObjectsSortMode.None))
                if(button.name==name){ExecuteEvents.Execute(button.gameObject,new BaseEventData(EventSystem.current),ExecuteEvents.submitHandler);return;}
        }
        private static string Arg(string key,string fallback)
        {
            string[] args=Environment.GetCommandLineArgs();for(int i=0;i+1<args.Length;i++)if(args[i]==key)return args[i+1];return fallback;
        }
        private static void Capture(CityGameBootstrap game,string name)
        {
            string folder=Arg("-vktFlowCaptures","");if(folder.Length==0)return;Directory.CreateDirectory(folder);
            ScreenCapture.CaptureScreenshot(Path.Combine(folder,name+".png"));
        }
        private void Finish(string error)
        {
            report.error=error;
            report.passed=error.Length==0&&report.phoneBeforeContract&&report.mapBeforeContract&&report.musicNonModal&&report.musicControlsClear&&report.movementAfterMusic&&report.legsAnimated&&report.benchStarted&&report.benchCompleted&&report.dockVisible&&report.helpReturnsControl&&report.onboardingCreated;
            report.passed=report.passed&&report.signalBlockedByMenu&&report.signalBlockedByWall&&report.signalReachesOfficer&&report.signalCooldown&&report.signalDoesNotPauseOrAddHeat&&report.hideoutScoreFades&&report.hideoutScoreMutes;
            report.passed=report.passed&&report.helpAudioTextFits&&report.hideoutMusicPreferenceMutes&&report.disabledSignalBlocked;
            report.passed=report.passed&&report.mapPreservesAudio&&report.muteShortcutWorks;
            report.passed=report.passed&&report.gardenPacked&&report.gardenDelivery&&report.gardenPause&&report.gardenDiskCargo&&report.gardenDiskReceipt&&report.gardenSinglePayout&&report.gardenTrainingGate&&report.gardenPhoneCancellation;
            string path=Arg("-vktFlowReport",Path.Combine(Path.GetTempPath(),"vkt-input-flow.json"));
            File.WriteAllText(path,JsonUtility.ToJson(report,true));Debug.Log("VKT_INPUT_FLOW "+(report.passed?"PASS":"FAIL"));
            if(keyboard!=null)InputSystem.RemoveDevice(keyboard);Application.Quit(report.passed?0:1);
        }
    }
}
#endif
