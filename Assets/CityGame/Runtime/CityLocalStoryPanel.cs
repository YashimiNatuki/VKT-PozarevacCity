using System;
using System.Collections;
using System.Text;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Networking;
using UnityEngine.UI;

namespace Pozarevac.CityGame
{
    public sealed class CityLocalStoryPanel : MonoBehaviour
    {
        public const string Endpoint="http://127.0.0.1:18741/v1/contact";
        public const string HealthEndpoint="http://127.0.0.1:18741/health";
        private CityGameBootstrap game;
        private CityLifestyleRuntime lifestyle;
        private GameObject canvas,panel;
        private Text heading,body,state,goal;
        private Button refresh;
        private UnityWebRequest request;
        private Coroutine pending;
        private int selected,serial;
        private bool ownsPause;
        private string intent="briefing";
        private CityContactReply reply;
        public bool IsOpen=>panel!=null&&panel.activeSelf;
        public bool IsLoading=>pending!=null;
        public string Source=>reply!=null?reply.source:"";
        public string CurrentLine=>reply!=null?reply.line:"";
        public string StatusText=>state!=null?state.text:"";

        public void Initialize(CityGameBootstrap owner,CityLifestyleRuntime runtime)
        {
            game=owner;lifestyle=runtime;
            canvas=new GameObject("VKT_LocalContacts",typeof(RectTransform),typeof(Canvas),typeof(CanvasScaler),typeof(GraphicRaycaster));
            canvas.transform.SetParent(transform,false);canvas.GetComponent<Canvas>().renderMode=RenderMode.ScreenSpaceOverlay;canvas.GetComponent<Canvas>().sortingOrder=100;
            var scaler=canvas.GetComponent<CanvasScaler>();scaler.uiScaleMode=CanvasScaler.ScaleMode.ScaleWithScreenSize;scaler.referenceResolution=new Vector2(1280,720);scaler.matchWidthOrHeight=.5f;
            panel=Rect("ContactsPanel",canvas.transform,new Vector2(1060,610),Vector2.zero).gameObject;panel.AddComponent<Image>().color=new Color(.02f,.045f,.065f,1);
            heading=Label("Heading",new Vector2(0,245),new Vector2(960,70),29);
            Label("Subheading",new Vector2(0,190),new Vector2(940,38),18).text="PO-BLOK-C / LOKALNA VEZA / TRI KONTAKTA";
            for(int i=0;i<3;i++){int contact=i;Button(new[]{"MILA / dogovor","VUK / ekipa","LUKA / mreža"}[i],new Vector2(-310+i*310,130),()=>Select(contact));}
            body=Label("Story",new Vector2(0,20),new Vector2(940,150),25);
            goal=Label("NextGoal",new Vector2(0,-90),new Vector2(940,60),20);
            state=Label("Source",new Vector2(0,-150),new Vector2(940,64),16);
            refresh=Button("OSVEŽI / OLLAMA",new Vector2(-310,-210),()=>RefreshRemote());
            Button("SAVET ZA LIKA",new Vector2(0,-210),()=>{if(IsLoading)return;intent="advice";RefreshRemote();});
            Button("PRATI CILJ",new Vector2(310,-210),Follow);
            Button("NAZAD U IGRU / ESC",new Vector2(0,-270),Close);
            canvas.AddComponent<CityUiSkin>().ApplyTree();panel.SetActive(false);game.Vitals.Died+=Close;
        }
        private static RectTransform Rect(string name,Transform parent,Vector2 size,Vector2 at)
        {
            var r=new GameObject(name,typeof(RectTransform)).GetComponent<RectTransform>();r.SetParent(parent,false);
            r.anchorMin=r.anchorMax=new Vector2(.5f,.5f);r.sizeDelta=size;r.anchoredPosition=at;return r;
        }
        private Text Label(string name,Vector2 at,Vector2 size,int fontSize)
        {
            Text text=Rect(name,panel.transform,size,at).gameObject.AddComponent<Text>();
            text.font=Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");text.fontSize=fontSize;text.alignment=TextAnchor.MiddleCenter;
            text.color=Color.white;text.supportRichText=false;text.raycastTarget=false;return text;
        }
        private Button Button(string caption,Vector2 at,UnityEngine.Events.UnityAction action)
        {
            var rect=Rect(caption,panel.transform,new Vector2(290,46),at);var image=rect.gameObject.AddComponent<Image>();image.color=new Color(.055f,.22f,.27f);
            var button=rect.gameObject.AddComponent<Button>();button.targetGraphic=image;button.onClick.AddListener(action);
            Text text=new GameObject("Text",typeof(RectTransform),typeof(Text)).GetComponent<Text>();text.transform.SetParent(rect,false);
            var r=text.rectTransform;r.anchorMin=Vector2.zero;r.anchorMax=Vector2.one;r.offsetMin=r.offsetMax=Vector2.zero;
            text.font=Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");text.fontSize=17;text.color=Color.white;text.alignment=TextAnchor.MiddleCenter;text.raycastTarget=false;text.text=caption;return button;
        }
        private CityContactFacts Facts()=>new CityContactFacts{
            contact=selected==0?"mila":selected==1?"vuk":"luka",intent=intent,
            chapter=game.Director.Jobs.StoryChapter,campaign=game.Progression.CompletedContracts,
            heat=Mathf.Clamp(Mathf.RoundToInt(game.Session.Heat),0,100),
            nutrition=Mathf.Clamp(Mathf.RoundToInt(lifestyle.Life.Nutrition),0,100),
            stamina=Mathf.Clamp(Mathf.RoundToInt(lifestyle.Life.Stamina),0,100),job_active=game.Director.Jobs.IsActive,wanted=lifestyle.Life.Wanted};
        public bool Open()
        {
            if(IsOpen||!lifestyle.CanSimulate||lifestyle.Life.IsTraining)return false;
            ownsPause=true;game.Director.SetOverlayOpen(true);panel.SetActive(true);intent="briefing";
            Present(CityContactStory.Offline(Facts(),"not_requested"));return true;
        }
        public void Select(int index)
        {
            if(!IsOpen||index<0||index>2)return;
            CancelPending();selected=index;intent="briefing";Present(CityContactStory.Offline(Facts(),"not_requested"));
        }
        public bool RefreshRemote()
        {
            if(!IsOpen||IsLoading)return false;
#if UNITY_ANDROID && !UNITY_EDITOR
            Present(CityContactStory.Offline(Facts(),"phone_offline"));return false;
#else
            pending=StartCoroutine(Fetch(Facts(),++serial));return true;
#endif
        }
        private IEnumerator Fetch(CityContactFacts facts,int generation)
        {
            state.text="PROVERA AI VEZE… možeš da zatvoriš ekran.";refresh.interactable=false;
            request=UnityWebRequest.Get(HealthEndpoint);request.timeout=4;
            yield return request.SendWebRequest();
            CityStoryHealth health=null;
            bool connected=request.result==UnityWebRequest.Result.Success&&request.downloadedBytes<=32768;
            if(connected)
            {
                try{health=JsonUtility.FromJson<CityStoryHealth>(request.downloadHandler.text);}catch(ArgumentException){ }
            }
            request.Dispose();request=null;
            if(!CityContactStory.ValidHealth(health))
            {
                if(generation==serial&&IsOpen)Present(CityContactStory.Offline(facts,connected?"wrong_service":"backend_unavailable"));
                refresh.interactable=true;pending=null;yield break;
            }
            // Even an unavailable model may have a matching cached conversation.
            state.text=health.available?"LOKALNI AI PIŠE… možeš da zatvoriš ekran.":CityContactStory.Status(health.reason);
            request=new UnityWebRequest(Endpoint,"POST"){uploadHandler=new UploadHandlerRaw(Encoding.UTF8.GetBytes(JsonUtility.ToJson(facts))),downloadHandler=new DownloadHandlerBuffer(),timeout=40};
            request.SetRequestHeader("Content-Type","application/json");
            yield return request.SendWebRequest();
            CityContactReply result=null;
            if(generation==serial&&request.result==UnityWebRequest.Result.Success&&request.downloadedBytes<=32768)
            {
                try{result=JsonUtility.FromJson<CityContactReply>(request.downloadHandler.text);}catch(ArgumentException){ }
            }
            request.Dispose();request=null;
            if(generation==serial&&IsOpen)
                Present(CityContactStory.Valid(result,CityContactStory.Action(facts))?result:CityContactStory.Offline(facts,"backend_unavailable"));
            refresh.interactable=true;pending=null;
        }
        private void Present(CityContactReply value)
        {
            reply=value;heading.text=value.title;body.text=value.line;
            // The actionable objective always comes from current, authoritative game facts.
            goal.text="SLEDEĆI KORAK / "+CityContactStory.Tip(CityContactStory.Action(Facts()));
            state.text=value.source=="ollama"?"OLLAMA / lokalno generisana priča":value.source=="cache"?"KEŠ / prethodno generisana lokalna priča":
                "AUTORSKA REPLIKA / "+CityContactStory.Status(value.reason);
            if(value.source=="offline"&&value.retry_after>0&&value.retry_after<=60)state.text+="\nPokušaj ponovo za oko "+value.retry_after+" s.";
        }
        private void Follow()
        {
            string action=CityContactStory.Action(Facts());Close();
            if(action=="jobs"||action=="delivery")
            {
                game.GetComponent<CityProgressionController>().OpenPanel();FindAnyObjectByType<CityPhonePanel>().OpenJobs();
            }
            else if(action=="meal")lifestyle.OpenMenu();
        }
        private void CancelPending()
        {
            serial++;if(request!=null){request.Abort();request.Dispose();request=null;}
            if(pending!=null){StopCoroutine(pending);pending=null;}if(refresh!=null)refresh.interactable=true;
        }
        public void Close(){CancelPending();if(panel!=null)panel.SetActive(false);if(ownsPause){ownsPause=false;game.Director.SetOverlayOpen(false);}}
        private void Update(){if(IsOpen&&Keyboard.current!=null&&Keyboard.current.escapeKey.wasPressedThisFrame)Close();}
        private void OnApplicationPause(bool paused){if(paused&&IsOpen){CancelPending();Present(CityContactStory.Offline(Facts(),"suspended"));}}
        private void OnDisable(){Close();}
        private void OnDestroy(){if(game!=null&&game.Vitals!=null)game.Vitals.Died-=Close;CancelPending();if(canvas!=null)Destroy(canvas);}
    }
}
