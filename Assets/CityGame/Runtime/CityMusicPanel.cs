using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;

namespace Pozarevac.CityGame
{
    // Non-modal music never owns world pause or cursor lock.
    [DefaultExecutionOrder(-150)]
    public sealed class CityMusicPanel : MonoBehaviour
    {
        private CityMusicPlayer player;
        private GameObject overlay,panel,settings,ownedEvents,launcher;
        private Text status,audioStatus;
        private CityGameAudio gameAudio;
        private Func<bool> gamePaused;
        private Action consumeBack;
        private float nextStatus;
        private bool touchLayout;
        private string diagnosticMessage="PROBA: kratak ton, nije govor.";
        public bool IsOpen=>panel!=null&&panel.activeSelf;
        public void Initialize(CityMusicPlayer music,Func<bool> isPaused=null,Action backConsumed=null,CityGameAudio gameplayAudio=null)
        {
            if(overlay!=null)return;
            bool touch=Application.isMobilePlatform||Array.IndexOf(Environment.GetCommandLineArgs(),"-vktTouchPreview")>=0;
            touchLayout=touch;
            player=music;gameAudio=gameplayAudio;gamePaused=isPaused;consumeBack=backConsumed;player.Initialize();
            overlay=new GameObject("VKT_Music_UI",typeof(RectTransform),typeof(Canvas),typeof(CanvasScaler),typeof(GraphicRaycaster));overlay.transform.SetParent(transform,false);
            var canvas=overlay.GetComponent<Canvas>();canvas.renderMode=RenderMode.ScreenSpaceOverlay;canvas.sortingOrder=25;
            var scaler=overlay.GetComponent<CanvasScaler>();scaler.uiScaleMode=CanvasScaler.ScaleMode.ScaleWithScreenSize;scaler.referenceResolution=new Vector2(1280,720);scaler.matchWidthOrHeight=.5f;
            if(EventSystem.current==null)ownedEvents=new GameObject("VKT_Music_Events",typeof(EventSystem),typeof(InputSystemUIInputModule));
            panel=Box("MusicPanel",overlay.transform,new Vector2(320,106));
            var rect=(RectTransform)panel.transform;rect.anchorMin=rect.anchorMax=new Vector2(1,0);rect.pivot=new Vector2(1,0);rect.anchoredPosition=new Vector2(-16,16);
            status=Label(panel.transform,"MUZIKA / F7: pusti-pauza",new Vector2(12,-8),new Vector2(296,38));
            Button(panel.transform,"<",new Vector2(12,-56),40,()=>player.Previous());
            Button(panel.transform,touch?"PUSTI":"PUSTI / F7",new Vector2(58,-56),90,()=>player.TogglePause());
            Button(panel.transform,touch?">":"> / F8",new Vector2(154,-56),52,()=>player.Next());
            Button(panel.transform,"ZVUK",new Vector2(212,-56),54,()=>settings.SetActive(!settings.activeSelf));
            Button(panel.transform,"X",new Vector2(272,-56),36,Close);
            settings=Box("MusicSettings",panel.transform,new Vector2(320,228));
            var options=(RectTransform)settings.transform;options.anchorMin=options.anchorMax=new Vector2(0,1);options.pivot=new Vector2(0,0);options.anchoredPosition=new Vector2(0,8);
            Button(settings.transform,"TIŠE",new Vector2(12,-12),92,()=>player.Volume-=.1f);
            Button(settings.transform,"GLASNIJE",new Vector2(114,-12),92,()=>player.Volume+=.1f);
            Button(settings.transform,"MUTE",new Vector2(216,-12),92,()=>player.Muted=!player.Muted);
            Button(settings.transform,"OSVEŽI",new Vector2(12,-58),92,()=>player.Refresh());
            Button(settings.transform,"RADIO",new Vector2(114,-58),92,()=>player.Radio());
            Button(settings.transform,"ZATVORI",new Vector2(216,-58),92,Close);
            audioStatus=Label(settings.transform,"",new Vector2(12,-104),new Vector2(296,68));
            Button(settings.transform,"PROBA ZVUKA",new Vector2(12,-184),142,TestAudio);
            Button(settings.transform,"ZVUK IGRE",new Vector2(166,-184),142,()=>{if(gameAudio!=null)gameAudio.SetMuted(!gameAudio.Muted);nextStatus=0;});
            if(touch)
            {
                // Keep movement, jump and training controls clear; extra settings drop below this compact card.
                rect.anchorMin=rect.anchorMax=rect.pivot=new Vector2(.5f,1);
                rect.anchoredPosition=new Vector2(0,-118);
                options.anchorMin=options.anchorMax=new Vector2(0,0);options.pivot=new Vector2(0,1);options.anchoredPosition=new Vector2(0,-8);
            }
            launcher=Button(overlay.transform,touch?"MUZIKA":"MUZIKA / F6",LauncherPosition(touch),120,Toggle).gameObject;
            var launch=(RectTransform)launcher.transform;launch.anchorMin=launch.anchorMax=new Vector2(1,0);
            overlay.AddComponent<CityUiSkin>().ApplyTree();settings.SetActive(false);panel.SetActive(false);
        }
        public static Vector2 LauncherPosition(bool touchLayout)=>new Vector2(-144,touchLayout?420:68);
        public void Toggle()
        {
            if(IsOpen){Close();return;}
            if(panel==null||(gamePaused!=null&&gamePaused()))return;
            panel.SetActive(true);launcher.SetActive(false);nextStatus=0;
        }
        public void Close()
        {
            if(!IsOpen)return;
            gameAudio?.StopDiagnostic();settings.SetActive(false);panel.SetActive(false);launcher.SetActive(true);consumeBack?.Invoke();
        }
        public void TestAudio(){diagnosticMessage=gameAudio!=null&&gameAudio.TryPlayDiagnostic()?"PROBA: kratak ton, nije govor.":"PROBA: nedostupno ili isključeno.";nextStatus=0;}
        private void Update()
        {
            var keys=Keyboard.current;
            if(keys!=null&&keys.f6Key.wasPressedThisFrame)Toggle();
            if(keys!=null&&keys.f7Key.wasPressedThisFrame&&(gamePaused==null||!gamePaused()))player?.TogglePause();
            if(keys!=null&&keys.f8Key.wasPressedThisFrame&&(gamePaused==null||!gamePaused()))player?.Next();
            if(IsOpen&&keys!=null&&keys.escapeKey.wasPressedThisFrame){Close();return;}
            if(!IsOpen||Time.unscaledTime<nextStatus)return;
            nextStatus=Time.unscaledTime+.2f;
            string title=player.Status.Replace('\n',' ');if(title.Length>47)title=title.Substring(0,44)+"…";
            status.text=title+"\n"+Mathf.RoundToInt(player.Volume*100)+(touchLayout?"% / X: zatvori":"% / F6 zatvori / F7 pusti-pauza");
            audioStatus.text=gameAudio==null?"ZVUK IGRE: nedostupan":"ZVUK IGRE: "+(gameAudio.Muted?"isključen":"uključen")+$"\nGLASOVI: {gameAudio.LoadedVoiceClips}/3 / "+diagnosticMessage;
        }
        private static GameObject Box(string name,Transform parent,Vector2 size)
        {
            var item=new GameObject(name,typeof(RectTransform),typeof(Image));item.transform.SetParent(parent,false);
            ((RectTransform)item.transform).sizeDelta=size;item.GetComponent<Image>().color=new Color(.025f,.055f,.075f,.94f);return item;
        }
        private static Text Label(Transform parent,string title,Vector2 position,Vector2 size)
        {
            var item=new GameObject("Label",typeof(RectTransform),typeof(Text));item.transform.SetParent(parent,false);
            var rect=(RectTransform)item.transform;rect.anchorMin=rect.anchorMax=rect.pivot=new Vector2(0,1);rect.anchoredPosition=position;rect.sizeDelta=size;
            var text=item.GetComponent<Text>();text.text=title;text.font=Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");text.fontSize=13;text.color=Color.white;text.raycastTarget=false;return text;
        }
        private static Button Button(Transform parent,string title,Vector2 position,float width,UnityEngine.Events.UnityAction action)
        {
            var item=Box(title,parent,new Vector2(width,36));var rect=(RectTransform)item.transform;rect.anchorMin=rect.anchorMax=rect.pivot=new Vector2(0,1);rect.anchoredPosition=position;
            item.GetComponent<Image>().color=new Color(.08f,.23f,.27f);var button=item.AddComponent<Button>();button.onClick.AddListener(action);
            var label=Label(item.transform,title,new Vector2(4,-8),new Vector2(width-8,24));label.alignment=TextAnchor.MiddleCenter;return button;
        }
        private void OnDisable()=>Close();
        private void OnDestroy(){Close();if(overlay!=null)Destroy(overlay);if(ownedEvents!=null)Destroy(ownedEvents);}
    }
}
