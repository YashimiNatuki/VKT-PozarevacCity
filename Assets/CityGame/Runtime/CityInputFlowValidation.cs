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
            if(helpShown)Capture(game,"help");
            yield return Press(Key.Escape);report.helpReturnsControl=helpShown&&!dock.HelpOpen&&!game.Director.IsPaused;
            yield return Press(Key.P);
            report.phoneBeforeContract=phone.IsOpen&&game.Progression.CompletedContracts==0;
            if(phone.IsOpen)Capture(game,"phone");
            if(phone.IsOpen)yield return Press(Key.Escape);
            yield return Press(Key.M);
            var map=phone.transform.Find("MapPanel");report.mapBeforeContract=phone.IsOpen&&map!=null&&map.gameObject.activeSelf;
            if(report.mapBeforeContract)Capture(game,"map");
            if(phone.IsOpen)yield return Press(Key.Escape);
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
            Finish("");
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
            string path=Arg("-vktFlowReport",Path.Combine(Path.GetTempPath(),"vkt-input-flow.json"));
            File.WriteAllText(path,JsonUtility.ToJson(report,true));Debug.Log("VKT_INPUT_FLOW "+(report.passed?"PASS":"FAIL"));
            if(keyboard!=null)InputSystem.RemoveDevice(keyboard);Application.Quit(report.passed?0:1);
        }
    }
}
#endif
