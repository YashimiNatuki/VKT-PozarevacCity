using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace Pozarevac.CityGame
{
    public sealed class CityWardrobePanel : MonoBehaviour
    {
        private CityGameBootstrap game;
        private CityLifestyleRuntime lifestyle;
        private GameObject canvas,stage,panel,originalPreview,candidatePreview,candidateLights;
        private Transform pivot;
        private Camera preview;
        private RenderTexture image;
        private CityCharacterLook look;
        private CityCharacterPhysique physique;
        private Text title, expressionLabel,info,candidateLabel;
        private Button expressionButton;
        private readonly Button[] outfitButtons=new Button[3];
        public bool CandidatePreview { get; private set; }
        public CityFaceMood PreviewMood=>look!=null?look.Face.PreviewMood:CityFaceMood.Calm;
        private bool ownsPause;
        public bool Portrait { get; private set; }
        public bool IsOpen=>panel!=null&&panel.activeSelf;
        public CityOutfit PreviewOutfit=>look!=null?look.Outfit:CityOutfit.Korzo;
        public void Initialize(CityGameBootstrap owner,CityLifestyleRuntime runtime)
        {
            game=owner;lifestyle=runtime;
            stage=new GameObject("VKT_WardrobeStage");stage.transform.position=new Vector3(10000,-1000,10000);
            pivot=new GameObject("PreviewTurntable").transform;pivot.SetParent(stage.transform,false);
            Transform wrapper=new GameObject("NormalizedCharacter").transform;wrapper.SetParent(pivot,false);
            originalPreview=wrapper.gameObject;
            GameObject model=Instantiate(CityCharacterModel.LoadSource(),wrapper);
            physique=new CityCharacterPhysique(model);AnimationClip pose=null;
            foreach(AnimationClip clip in Resources.LoadAll<AnimationClip>("Models/KenneyRetro/idle"))
                if(!clip.name.StartsWith("__preview__",System.StringComparison.Ordinal)){pose=clip;break;}
            if(!CityCharacterModel.Normalize(wrapper,model,pose,physique))throw new System.InvalidOperationException("Wardrobe character normalization failed.");
            look=new CityCharacterLook(model,physique);
            look.SetStudioFill(.75f);
            look.Face.Preview=true;
            Transform rig=CityCharacterModel.AnimationRoot(model).transform;
            const string chest="HipsCtrl/Hips/Spine/Chest/UpperChest/";
            Transform left=rig.Find(chest+"LeftShoulder/LeftArm"),right=rig.Find(chest+"RightShoulder/RightArm");
            if(left!=null)CityActorMotion.RelaxArm(left,left.Find("LeftForeArm"),pivot,Mathf.Sign(pivot.InverseTransformPoint(left.position).x),0);
            if(right!=null)CityActorMotion.RelaxArm(right,right.Find("RightForeArm"),pivot,Mathf.Sign(pivot.InverseTransformPoint(right.position).x),0);
            foreach(Transform item in stage.GetComponentsInChildren<Transform>(true))item.gameObject.layer=30;
            preview=new GameObject("WardrobeCamera",typeof(Camera)).GetComponent<Camera>();preview.transform.SetParent(stage.transform,false);
            preview.transform.localPosition=new Vector3(0,1.05f,3.25f);preview.transform.LookAt(stage.transform.position+Vector3.up*.96f);
            preview.clearFlags=CameraClearFlags.SolidColor;preview.backgroundColor=new Color(.035f,.055f,.075f);
            preview.cullingMask=1<<30;preview.nearClipPlane=.1f;preview.farClipPlane=6;preview.fieldOfView=36;preview.allowHDR=false;
            preview.gameObject.AddComponent<UnityEngine.Rendering.Universal.UniversalAdditionalCameraData>().renderShadows=false;
            image=new RenderTexture(768,1024,24){name="VKT_WardrobePreview",antiAliasing=2};image.Create();preview.targetTexture=image;
            BuildUi();stage.SetActive(false);panel.SetActive(false);
            game.Vitals.Died+=Close;
        }
        private void BuildUi()
        {
            canvas=new GameObject("VKT_WardrobeUI",typeof(RectTransform),typeof(Canvas),typeof(CanvasScaler),typeof(GraphicRaycaster));
            canvas.transform.SetParent(transform,false);canvas.GetComponent<Canvas>().renderMode=RenderMode.ScreenSpaceOverlay;canvas.GetComponent<Canvas>().sortingOrder=90;
            var scaler=canvas.GetComponent<CanvasScaler>();scaler.uiScaleMode=CanvasScaler.ScaleMode.ScaleWithScreenSize;scaler.referenceResolution=new Vector2(1280,720);scaler.matchWidthOrHeight=.5f;
            panel=new GameObject("WardrobePanel",typeof(RectTransform),typeof(Image));panel.transform.SetParent(canvas.transform,false);
            var full=panel.GetComponent<RectTransform>();full.anchorMin=Vector2.zero;full.anchorMax=Vector2.one;full.offsetMin=full.offsetMax=Vector2.zero;
            panel.GetComponent<Image>().color=new Color(.015f,.03f,.045f,1);
            RectTransform photo=Rect("Preview",panel.transform,new Vector2(330,440),new Vector2(-260,5));
            photo.gameObject.AddComponent<RawImage>().texture=image;
            title=Label(Rect("Title",panel.transform,new Vector2(520,100),new Vector2(200,215)),"",25);
            info=Label(Rect("Info",panel.transform,new Vector2(500,130),new Vector2(220,100)),"",20);
            for(int i=0;i<3;i++){int choice=i;outfitButtons[i]=Button(CityAppearance.Name((CityOutfit)i),new Vector2(210,10-i*65),()=>Select((CityOutfit)choice));}
            Button("← OKRENI",new Vector2(-355,-250),()=>Rotate(-45));
            Button("OKRENI →",new Vector2(-165,-250),()=>Rotate(45));
            Button("LICE / CEO LIK",new Vector2(-260,280),TogglePortrait);
            expressionButton=Button("IZRAZ LICA",new Vector2(-60,280),CycleExpression);
            expressionLabel=expressionButton.GetComponentInChildren<Text>();
            expressionLabel.text="IZRAZ / MIRAN";
            candidateLabel=Button("NOVI 3D LIK / PREGLED",new Vector2(210,-175),()=>ToggleCandidate()).GetComponentInChildren<Text>();
            Button("NAZAD U GRAD / ESC",new Vector2(210,-245),Close);
            canvas.AddComponent<CityUiSkin>().ApplyTree();
        }
        private static RectTransform Rect(string name,Transform parent,Vector2 size,Vector2 at)
        {
            var rect=new GameObject(name,typeof(RectTransform)).GetComponent<RectTransform>();rect.SetParent(parent,false);
            rect.anchorMin=rect.anchorMax=new Vector2(.5f,.5f);rect.sizeDelta=size;rect.anchoredPosition=at;return rect;
        }
        private static Text Label(RectTransform parent,string value,int size)
        {
            Text text=parent.gameObject.AddComponent<Text>();text.text=value;text.font=Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            text.fontSize=size;text.color=Color.white;text.alignment=TextAnchor.MiddleCenter;text.raycastTarget=false;return text;
        }
        private Button Button(string caption,Vector2 at,UnityEngine.Events.UnityAction action)
        {
            var rect=Rect(caption,panel.transform,new Vector2(at.x<0?175:420,48),at);Image background=rect.gameObject.AddComponent<Image>();background.color=new Color(.055f,.2f,.24f);
            var button=rect.gameObject.AddComponent<Button>();button.targetGraphic=background;button.onClick.AddListener(action);
            Label(Rect("Label",rect,new Vector2(at.x<0?170:410,44),Vector2.zero),caption,18);return button;
        }
        public bool Open()
        {
            if(IsOpen||!lifestyle.CanSimulate||lifestyle.Life.IsTraining||lifestyle.Life.IsBoxing)return false;
            ownsPause=true;game.Director.SetOverlayOpen(true);panel.SetActive(true);stage.SetActive(true);
            pivot.localRotation=Quaternion.Euler(0,-15,0);Refresh();return true;
        }
        public void CycleExpression()
        {
            if(!IsOpen||CandidatePreview)return;
            look.Face.PreviewMood=(CityFaceMood)(((int)look.Face.PreviewMood+1)%5);
            expressionLabel.text="IZRAZ / "+CityFaceState.Name(look.Face.PreviewMood);
        }
        public void TogglePortrait()
        {
            if(!IsOpen)return;
            Portrait=!Portrait;
            FramePreview();
        }
        private void FramePreview()
        {
            if(preview==null)return;
            float faceHeight=CandidatePreview?1.66f:1.57f;
            preview.transform.localPosition=Portrait?new Vector3(0,faceHeight+.02f,1.25f):new Vector3(0,1.05f,3.25f);
            preview.transform.LookAt(stage.transform.position+Vector3.up*(Portrait?faceHeight:.96f));
        }
        public bool ToggleCandidate()
        {
            if(!IsOpen)return false;
            if(!CandidatePreview&&candidatePreview==null)
            {
                GameObject source=Resources.Load<GameObject>("Models/RodjaHuman/RodjaHuman");
                if(!TryPrepareCandidate(source,pivot,out candidatePreview))
                {info.text="Novi 3D pregled nije dostupan u ovom paketu.\nTvoj postojeći lik ostaje aktivan.";return false;}
                candidateLights=new GameObject("CandidateStudioLights");candidateLights.transform.SetParent(stage.transform,false);
                StudioLight(new Vector3(1.4f,2.3f,2.1f),new Color(1,.91f,.82f),4);
                StudioLight(new Vector3(-1.5f,1.6f,1.3f),new Color(.72f,.84f,1),2.2f);
            }
            CandidatePreview=!CandidatePreview;
            originalPreview.SetActive(!CandidatePreview);candidatePreview.SetActive(CandidatePreview);
            candidateLights.SetActive(CandidatePreview);FramePreview();Refresh();return true;
        }
        private void StudioLight(Vector3 position,Color color,float intensity)
        {
            Light light=new GameObject("CandidateFill",typeof(Light)).GetComponent<Light>();
            light.transform.SetParent(candidateLights.transform,false);light.transform.localPosition=position;
            light.type=LightType.Point;light.range=6;light.intensity=intensity;light.color=color;
            light.shadows=LightShadows.None;light.cullingMask=1<<30;
        }
        internal static bool TryPrepareCandidate(GameObject source,Transform parent,out GameObject prepared)
        {
            prepared=null;if(source==null||parent==null)return false;
            // Validate the asset before Instantiate can run any imported behaviour callbacks.
            if(!parent.gameObject.activeInHierarchy||source.GetComponentsInChildren<MonoBehaviour>(true).Length>0||
               source.GetComponentsInChildren<Collider>(true).Length>0||source.GetComponentsInChildren<Rigidbody>(true).Length>0||
               source.GetComponentsInChildren<Camera>(true).Length>0||source.GetComponentsInChildren<AudioSource>(true).Length>0||
               source.GetComponentsInChildren<AudioListener>(true).Length>0)return false;
            GameObject wrapper=new GameObject("ReviewedHumanPreview");wrapper.transform.SetParent(parent,false);
            bool ready=false;
            try
            {
                GameObject model=Object.Instantiate(source,wrapper.transform);
                foreach(Animator animator in model.GetComponentsInChildren<Animator>(true))animator.enabled=false;
                if(!CityCharacterModel.Normalize(wrapper.transform,model,null))return false;
                foreach(Transform item in wrapper.GetComponentsInChildren<Transform>(true))item.gameObject.layer=30;
                wrapper.SetActive(false);prepared=wrapper;ready=true;return true;
            }
            finally{if(!ready){if(Application.isPlaying)Object.Destroy(wrapper);else Object.DestroyImmediate(wrapper);}}
        }
        public bool Select(CityOutfit outfit)
        {
            if(!IsOpen||CandidatePreview)return false;
            bool changed=game.Progression.Appearance.Select(outfit);
            Refresh();game.GetComponent<CityProgressionController>().SaveNow();return changed;
        }
        public void Rotate(float degrees){if(IsOpen)pivot.Rotate(Vector3.up,degrees,Space.Self);}
        private void Refresh()
        {
            look.Apply(game.Progression.Appearance.Outfit,game.Progression.Lifestyle.Strength);
            title.text=CandidatePreview?"NOVI 3D LIK\nSAMO PREGLED":"TVOJ LIK / "+CityAppearance.Name(game.Progression.Appearance.Outfit)+"\nSNAGA "+game.Progression.Lifestyle.Strength+"/5";
            info.text=CandidatePreview?"Probni model • materijali • proporcije\nOkreni model i pogledaj lice.\nPrikaz ne menja tvog igrača.\nAnimacije i mimika još nisu povezane.":"GARDEROBER / ORIGINAL VKT\nMajica • pantalone • patike • kačket\nKozmetika je besplatna. Snaga menja građu.\nIzgled se čuva sa likom.";
            candidateLabel.text=CandidatePreview?"NAZAD NA TVOG LIKA":"NOVI 3D LIK / PREGLED";
            foreach(Button button in outfitButtons)button.interactable=!CandidatePreview;
            expressionButton.gameObject.SetActive(!CandidatePreview);
        }
        public void Close()
        {
            CandidatePreview=false;Portrait=false;
            if(candidatePreview!=null)candidatePreview.SetActive(false);if(candidateLights!=null)candidateLights.SetActive(false);
            if(originalPreview!=null)originalPreview.SetActive(true);FramePreview();
            if(panel!=null)panel.SetActive(false);if(stage!=null)stage.SetActive(false);
            if(ownsPause){ownsPause=false;game.Director.SetOverlayOpen(false);}
        }
        private void Update(){if(IsOpen&&Keyboard.current!=null&&Keyboard.current.escapeKey.wasPressedThisFrame)Close();}
        private void OnDisable(){Close();}
        private void OnDestroy()
        {
            if(game!=null&&game.Vitals!=null)game.Vitals.Died-=Close;
            if(preview!=null)preview.targetTexture=null;
            if(look!=null)look.Dispose();else physique?.Dispose();if(stage!=null)Destroy(stage);if(canvas!=null)Destroy(canvas);if(image!=null){image.Release();Destroy(image);}
        }
    }
}
