using System;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace Pozarevac.CityGame
{
    public sealed class CityLifestyleRuntime : MonoBehaviour
    {
        private CityGameBootstrap game;
        private CityProgressionController progress;
        private CityHud hud;
        private CityPlayerController movement;
        private GameObject world, menu, workout, uiRoot, shade;
        private Text stats, banner, workoutText;
        private Image policeBanner;
        private Material metal, wood, blue, red;
        private Mesh rounded;
        private CityPoliceOfficer[] officers;
        private AudioSource siren;
        private AudioClip sirenClip;
        private AudioLowPassFilter sirenFilter;
        private Transform ears;
        private CitySpatialSoundscape acoustics;
        private float sirenProbe;
        private Vector3 previous;
        private float uiCountdown;
        private bool menuOwnedPause, lastWanted;
        private bool applicationSuspended;
        private CityBenchVisual benchVisual;
        private CityBenchPress bench;
        private readonly CityConditioningRun conditioning=new CityConditioningRun();
        private readonly Vector3[] conditioningPoints=new Vector3[3];
        private readonly GameObject[] conditioningMarkers=new GameObject[3];
        private GameObject repButton;
        private RectTransform cancelTrainingButton;
        public CityConditioningRun Conditioning=>conditioning;
        public Vector3 ConditioningTarget=>conditioningPoints[Math.Min(2,conditioning.Checkpoint)];
        public CityWardrobePanel Wardrobe { get; private set; }
        public CityLocalStoryPanel LocalStory { get; private set; }
        public CityBenchPress Bench => bench;
        public CityBenchVisual BenchVisual => benchVisual;
        private int lastArrests, lastEscapes, lastWorkouts, locationIndex = -1;
        public Transform Gym { get; private set; }
        public Transform Food { get; private set; }
        public CityLifestyle Life => game.Progression.Lifestyle;
        public bool IsMenuOpen => menu != null && menu.activeSelf;
        public bool SirenPlaying => siren != null && siren.isPlaying;
        public bool CanSimulate => game != null && !applicationSuspended && game.WorldReady && game.Progression.IsCreated && game.Vitals.IsAlive && !game.Director.IsPaused && Time.timeScale > 0;

        public void Initialize(CityGameBootstrap owner, CityHud cityHud, Vector3 spawn)
        {
            if(game!=null)throw new InvalidOperationException("Lifestyle is already initialized.");
            game=owner;hud=cityHud;progress=game.GetComponent<CityProgressionController>();movement=game.Player.GetComponent<CityPlayerController>();
            previous=game.Player.position;lastArrests=Life.Arrests;lastEscapes=Life.Escapes;lastWorkouts=Life.Workouts;
            movement.ConfigureLifestyle(Life);
            rounded=CityVisualMeshes.RoundedBox();Material template=Resources.Load<Material>("CityMissionMarker");
            metal=MakeMaterial(template,new Color(.12f,.17f,.2f));wood=MakeMaterial(template,new Color(.55f,.3f,.12f));
            blue=MakeMaterial(template,new Color(.12f,.3f,.8f));red=MakeMaterial(template,new Color(.8f,.12f,.12f));
            world=new GameObject("VKT_BlockLife");world.transform.SetParent(game.Player.parent,false);
            Gym=Station("VKT_StreetGym",spawn+new Vector3(-10,0,12),false);
            Food=Station("VKT_FoodKiosk",spawn+new Vector3(10,0,12),true);
            game.Director.BindLifestyle(Life,game.Audio);
            Vector3[] course={Food.position+new Vector3(-3,0,-3),spawn,Gym.position+Vector3.back*3};
            for(int i=0;i<3;i++)
            {
                Vector3 point=course[i];if(new CityGroundProbe().TryFindSurface(point,out Vector3 surface))point=surface;
                conditioningPoints[i]=point;
                GameObject markerRoot=new GameObject("VKT_Conditioning_"+(i+1));markerRoot.transform.SetParent(world.transform,false);markerRoot.transform.position=point;markerRoot.SetActive(false);conditioningMarkers[i]=markerRoot;
                GameObject marker=GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                Destroy(marker.GetComponent<Collider>());marker.transform.SetParent(markerRoot.transform,false);
                marker.transform.localPosition=Vector3.up*.035f;marker.transform.localScale=new Vector3(2.8f,.03f,2.8f);
                marker.GetComponent<Renderer>().sharedMaterial=blue;
                GameObject label=new GameObject("CircuitLabel",typeof(TextMesh));label.transform.SetParent(world.transform,false);label.transform.position=point+Vector3.up*1.7f;
                TextMesh text=label.GetComponent<TextMesh>();text.text="KONDICIJA / "+(i+1)+"\nSTANI 1.2 s";text.font=Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");text.fontSize=32;text.characterSize=.025f;text.anchor=TextAnchor.MiddleCenter;
                label.GetComponent<Renderer>().sharedMaterial=text.font.material;label.AddComponent<CityWorldLabel>().Initialize(game.Player.GetComponentInChildren<Camera>().transform);
                label.transform.SetParent(markerRoot.transform,true);
            }
            officers=new CityPoliceOfficer[2];
            for(int i=0;i<officers.Length;i++)
            {
                GameObject cop=new GameObject("VKT_Patrol_"+i);cop.transform.SetParent(world.transform,false);cop.transform.position=spawn+new Vector3(i==0?-16:16,0,-18);
                officers[i]=cop.AddComponent<CityPoliceOfficer>();officers[i].Initialize(game,blue,metal);
            }
            BuildUi();Wardrobe=gameObject.AddComponent<CityWardrobePanel>();Wardrobe.Initialize(game,this);
            LocalStory=gameObject.AddComponent<CityLocalStoryPanel>();LocalStory.Initialize(game,this);game.Touch.BindLifestyle(ToggleMenu);
            game.Player.GetComponentInChildren<CityCameraRig>().ConfigureAppearance(game.Progression.Appearance);
            GameObject sirenNode=new GameObject("VKT_PositionalPatrolSiren");sirenNode.transform.SetParent(world.transform,false);
            sirenClip=CreateSiren();siren=sirenNode.AddComponent<AudioSource>();siren.clip=sirenClip;siren.playOnAwake=false;siren.loop=true;siren.volume=.12f;siren.spatialBlend=1;
            siren.minDistance=5;siren.maxDistance=100;siren.rolloffMode=AudioRolloffMode.Logarithmic;siren.dopplerLevel=0;siren.priority=50;
            sirenFilter=sirenNode.AddComponent<AudioLowPassFilter>();sirenFilter.cutoffFrequency=22000;
            ears=game.Player.GetComponentInChildren<Camera>().transform;
            game.Vitals.Died+=OnDeath;
        }
        private static Material MakeMaterial(Material template,Color color){Material m=new Material(template);m.color=color;return m;}
        private Transform Station(string name,Vector3 approximate,bool food)
        {
            CityGroundProbe ground=new CityGroundProbe();if(ground.TryFindSurface(approximate,out Vector3 surface))approximate=surface;
            Transform root=new GameObject(name).transform;root.SetParent(world.transform,false);root.position=approximate;
            if(food)
            {
                Shape("Counter",root,new Vector3(0,.65f,0),new Vector3(2.5f,1.3f,1),wood,true);
                Shape("Awning",root,new Vector3(0,2,0),new Vector3(3,.2f,1.7f),red,false);
                for(int side=-1;side<=1;side+=2)Shape("AwningPost",root,new Vector3(side*1.25f,1.1f,.5f),new Vector3(.1f,2.2f,.1f),metal,false);
                for(int i=0;i<3;i++)Shape("Tray",root,new Vector3((i-1)*.65f,1.35f,0),new Vector3(.5f,.1f,.45f),metal,false);
            }
            else
            {
                benchVisual = root.gameObject.AddComponent<CityBenchVisual>();
                benchVisual.Initialize(rounded, metal, blue);
                game.Player.GetComponentInChildren<CityCameraRig>().ConfigureBench(benchVisual, Life);
            }
            GameObject target=new GameObject("Interaction",typeof(BoxCollider));target.transform.SetParent(root,false);target.transform.localPosition=Vector3.up;
            target.GetComponent<BoxCollider>().size=new Vector3(2.8f,2,.9f);root.gameObject.AddComponent<CityLifestyleStation>().Initialize(this,food);
            GameObject sign=new GameObject("BlockSign",typeof(TextMesh));sign.transform.SetParent(root,false);sign.transform.localPosition=new Vector3(0,2.4f,0);
            TextMesh text=sign.GetComponent<TextMesh>();text.text=food?"KLOPA / CENTAR\n[E] kupi ili rizikuj":"ULIČNI TRENING\n[E] snaga bloka";text.font=Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");text.fontSize=32;text.characterSize=.022f;text.anchor=TextAnchor.MiddleCenter;
            sign.GetComponent<Renderer>().sharedMaterial=text.font.material;sign.AddComponent<CityWorldLabel>().Initialize(game.Player.GetComponentInChildren<Camera>().transform);
            return root;
        }
        private void Shape(string name,Transform parent,Vector3 at,Vector3 scale,Material material,bool collision)
        {
            GameObject shape=new GameObject(name,typeof(MeshFilter),typeof(MeshRenderer));shape.transform.SetParent(parent,false);shape.transform.localPosition=at;shape.transform.localScale=scale;
            shape.GetComponent<MeshFilter>().sharedMesh=rounded;shape.GetComponent<MeshRenderer>().sharedMaterial=material;if(collision)shape.AddComponent<BoxCollider>();
        }
        private void BuildUi()
        {
            GameObject canvas=new GameObject("VKT_LifestyleUI",typeof(RectTransform),typeof(Canvas),typeof(CanvasScaler),typeof(GraphicRaycaster));canvas.transform.SetParent(transform,false);
            uiRoot=canvas;
            canvas.GetComponent<Canvas>().renderMode=RenderMode.ScreenSpaceOverlay;canvas.GetComponent<Canvas>().sortingOrder=80;
            CanvasScaler scaler=canvas.GetComponent<CanvasScaler>();scaler.uiScaleMode=CanvasScaler.ScaleMode.ScaleWithScreenSize;scaler.referenceResolution=new Vector2(1280,720);scaler.matchWidthOrHeight=.5f;
            RectTransform dim=Rect("BlockBackdrop",canvas.transform,Vector2.zero,Vector2.zero,new Vector2(.5f,.5f));dim.anchorMin=Vector2.zero;dim.anchorMax=Vector2.one;dim.offsetMin=dim.offsetMax=Vector2.zero;
            shade=dim.gameObject;shade.AddComponent<Image>().color=new Color(0,0,0,.5f);shade.SetActive(false);
            RectTransform badge=Rect("LifeStatus",canvas.transform,new Vector2(460,62),new Vector2(0,-205),new Vector2(0,1));badge.pivot=new Vector2(0,1);
            badge.gameObject.AddComponent<Image>().color=new Color(.025f,.06f,.08f,.85f);banner=Text(badge,"",17);banner.alignment=TextAnchor.MiddleCenter;
            badge.GetComponent<Image>().raycastTarget=false;
            RectTransform alert=Rect("PoliceAlert",canvas.transform,new Vector2(420,44),new Vector2(0,-100),new Vector2(.5f,1));policeBanner=alert.gameObject.AddComponent<Image>();policeBanner.color=Color.clear;
            Text alertText=Text(alert,"POTERA / prekini kontakt 18 s",17);alertText.name="PoliceText";
            policeBanner.raycastTarget=false;
            menu=Rect("BlockPanel",canvas.transform,new Vector2(620,650),Vector2.zero,new Vector2(.5f,.5f)).gameObject;menu.AddComponent<Image>().color=new Color(.025f,.055f,.075f,.98f);
            RectTransform info=Rect("Info",menu.transform,new Vector2(560,270),new Vector2(0,170),new Vector2(.5f,.5f));stats=Text(info,"",19);stats.alignment=TextAnchor.UpperLeft;stats.supportRichText=false;
            Button(menu.transform,"TRENING / kod klupe",new Vector2(-145,-15),()=>{CloseMenu();if(!StartWorkout())hud.ShowNotification("Priđi klupi. Hrana, 30 energije i odmor su potrebni; potera i aktivan posao blokiraju trening.",5);});
            Button(menu.transform,"KONDICIJA / 3 stanice",new Vector2(145,-15),()=>{CloseMenu();if(!StartConditioning())hud.ShowNotification("Kreni kod klupe: hrana 20%, energija 40%, odmor i bez aktivne misije/potere.",6);});
            Button(menu.transform,"OBROK / 35 DIN",new Vector2(-145,-75),()=>{CloseMenu();Eat(false);});
            Button(menu.transform,"UKRADI / POTERA",new Vector2(145,-75),()=>{CloseMenu();Eat(true);});
            Button(menu.transform,"STREJN KATALOG",new Vector2(-145,-135),()=>{CloseMenu();if(!game.Progression.PhoneUnlocked){hud.ShowNotification("Prvi ugovor otključava telefon i katalog.",5);return;}progress.OpenPanel();FindAnyObjectByType<CityPhonePanel>().OpenCatalog();});
            Button(menu.transform,"DELOVI GRADA",new Vector2(145,-135),()=>{locationIndex=(locationIndex+1)%CityGeoRegistry.Count;RefreshUi();});
            Button(menu.transform,"NAZAD U IGRU",new Vector2(0,-255),CloseMenu);
            Button(menu.transform,"ODEĆA / GARDEROBER",new Vector2(-145,-195),()=>{CloseMenu();Wardrobe.Open();});
            Button(menu.transform,"KONTAKTI / OLLAMA",new Vector2(145,-195),()=>{CloseMenu();LocalStory.Open();});
            workout=Rect("Workout",canvas.transform,new Vector2(600,160),new Vector2(0,125),new Vector2(.5f,0)).gameObject;workout.AddComponent<Image>().color=new Color(.03f,.08f,.11f,.9f);
            workoutText=Text(Rect("WorkoutText",workout.transform,new Vector2(580,48),new Vector2(0,50),new Vector2(.5f,.5f)),"",20);
            repButton=Button(workout.transform,"TAP / F: PONAVLJANJE",new Vector2(0,-2),()=>Rep()).gameObject;
            cancelTrainingButton=(RectTransform)Button(workout.transform,"PREKINI SERIJU",new Vector2(0,-55),()=>{Life.CancelWorkout();CancelConditioning("Kondicija prekinuta / bez XP.");}).transform;
            canvas.AddComponent<CityUiSkin>().ApplyTree();menu.SetActive(false);workout.SetActive(false);alert.gameObject.SetActive(false);
        }
        private static RectTransform Rect(string name,Transform parent,Vector2 size,Vector2 at,Vector2 anchor)
        {RectTransform r=new GameObject(name,typeof(RectTransform)).GetComponent<RectTransform>();r.SetParent(parent,false);r.anchorMin=r.anchorMax=anchor;r.sizeDelta=size;r.anchoredPosition=at;return r;}
        private static Text Text(RectTransform parent,string value,int size)
        {Text t=new GameObject("Text",typeof(RectTransform),typeof(Text)).GetComponent<Text>();t.transform.SetParent(parent,false);RectTransform r=t.rectTransform;r.anchorMin=Vector2.zero;r.anchorMax=Vector2.one;r.offsetMin=new Vector2(10,4);r.offsetMax=new Vector2(-10,-4);t.font=Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");t.fontSize=size;t.color=Color.white;t.alignment=TextAnchor.MiddleCenter;t.text=value;t.raycastTarget=false;return t;}
        private static UnityEngine.UI.Button Button(Transform parent,string title,Vector2 at,Action action)
        {RectTransform r=Rect(title,parent,new Vector2(275,48),at,new Vector2(.5f,.5f));Image image=r.gameObject.AddComponent<Image>();image.color=new Color(.08f,.23f,.27f);UnityEngine.UI.Button b=r.gameObject.AddComponent<UnityEngine.UI.Button>();b.targetGraphic=image;b.onClick.AddListener(()=>action());Text(r,title,17);return b;}
        public void ToggleMenu(){if(IsMenuOpen)CloseMenu();else OpenMenu();}
        public void OpenMenu()
        {
            if(!CanSimulate||Life.IsTraining)return;
            locationIndex=-1;menu.SetActive(true);menuOwnedPause=true;game.Director.SetOverlayOpen(true);RefreshUi();
        }
        public void CloseMenu(){if(menu!=null)menu.SetActive(false);if(menuOwnedPause){menuOwnedPause=false;game.Director.SetOverlayOpen(false);}}
        public bool Near(Transform station)=>station!=null&&Vector3.Distance(game.Player.position,station.position)<3.5f;
        public bool StartWorkout()
        {if(!CanSimulate||!Near(Gym)||game.Director.Jobs.IsActive||game.Director.Run.State==CityMissionState.Active||benchVisual==null||!benchVisual.RigReady||!benchVisual.FeetReady||!Life.TryStartWorkout())return false;bench=new CityBenchPress();progress.SaveNow();hud.ShowNotification("BENČ: 8 celih ponavljanja za 45 s. F/TAP: spusti, pa pritisni kad piše GURAJ. ESC: prekid.",5);return true;}
        public bool StartConditioning()
        {
            if(!CanSimulate||!Near(Gym)||game.Director.Jobs.IsActive||game.Director.Run.State==CityMissionState.Active||!Life.TryStartConditioning())return false;
            conditioning.Start();progress.SaveNow();game.Audio.PlayCue(CityAudioCue.Checkpoint);
            hud.ShowNotification("KONDICIJA: obiđi 3 plava kruga za 75 s. U svakom stani 1.2 s. +20 XP; zajednički odmor 90 s. ESC: prekid.",7);return true;
        }
        public void CancelConditioning(string reason="Kondicija prekinuta / bez XP.")
        {
            bool active=Life.IsConditioning||conditioning.State==CityConditioningState.Active;
            conditioning.Cancel();Life.CancelConditioning();
            if(active){game.Audio.PlayCue(CityAudioCue.Failure);hud.ShowNotification(reason,5);}
        }
        private void StepConditioning()
        {
            if(conditioning.State!=CityConditioningState.Active)return;
            if(!Life.IsConditioning||Life.Wanted||Life.Stamina<5||game.Hideout.IsInside){CancelConditioning("Kondicija prekinuta: potera, umor ili izlazak iz rute / bez XP.");return;}
            if(Keyboard.current!=null&&Keyboard.current.escapeKey.wasPressedThisFrame&&!game.Director.PauseChangedThisFrame){game.Director.SuppressPauseThisFrame();CancelConditioning();return;}
            Vector3 delta=game.Player.position-ConditioningTarget;bool near=Mathf.Abs(delta.y)<2.5f&&new Vector2(delta.x,delta.z).sqrMagnitude<=2.2f*2.2f;
            if(conditioning.Tick(Time.deltaTime,near))
            {
                bool finished=conditioning.State==CityConditioningState.Completed;
                if(finished){if(conditioning.TryClaim(Life)){progress.SaveNow();hud.ShowNotification("KONDICIJA ZAVRŠENA / +20 XP. Obnovi energiju i proveri sledeći ugovor.",6);}}
                else hud.ShowNotification("Stanica potvrđena / prati sledeći plavi krug.",3);
                game.Audio.PlayCue(finished?CityAudioCue.Success:CityAudioCue.Checkpoint);
            }
            if(conditioning.State==CityConditioningState.Expired)CancelConditioning("Kondicija: istekao je rok / bez XP.");
        }
        public bool Rep()=>CanSimulate&&Life.IsTraining&&Near(Gym)&&bench!=null&&bench.Press();
        public bool Eat(bool steal)
        {
            if(!CanSimulate||!Near(Food)||!Life.TryEat(steal)){hud.ShowNotification("Priđi kiosku. Proveri novac, glad, poternicu i pauzu između obroka.",5);return false;}
            progress.SaveNow();hud.ShowNotification(steal?"Prodavac te prijavio! Murija traži lika — prekini vidljiv kontakt.":"Obrok plaćen / glad i energija obnovljeni.",6);return true;
        }
        private void Update()
        {
            if(game==null)return;
            if(Keyboard.current!=null&&Keyboard.current.lKey.wasPressedThisFrame)ToggleMenu();
            if(IsMenuOpen&&Keyboard.current!=null&&Keyboard.current.escapeKey.wasPressedThisFrame)CloseMenu();
            bool simulate=CanSimulate;
            Vector3 delta=game.Player.position-previous;previous=game.Player.position;delta.y=0;
            if(simulate)
            {
                if(Life.IsTraining&&!Near(Gym))Life.CancelWorkout();
                Life.Tick(Time.deltaTime,delta.magnitude/Mathf.Max(.001f,Time.deltaTime)>6.1f);
                StepConditioning();
                if(Life.IsTraining&&Keyboard.current!=null&&Keyboard.current.escapeKey.wasPressedThisFrame){game.Director.SuppressPauseThisFrame();Life.CancelWorkout();}
                if(Life.IsTraining&&Keyboard.current!=null&&Keyboard.current.fKey.wasPressedThisFrame)Rep();
                if(Life.IsTraining&&bench!=null&&bench.Tick(Time.deltaTime))Life.TryRep();
                if(game.Session.Heat>=45&&!Life.Wanted)Life.ReportCrime();
                bool visible=false,close=false;
                foreach(CityPoliceOfficer officer in officers){officer.Step(Time.deltaTime,Life.Wanted);visible|=officer.SeesPlayer;close|=officer.CanCapture;}
                Life.TickPolice(Time.deltaTime,visible,close);
                if(Life.Arrests!=lastArrests){lastArrests=Life.Arrests;game.Director.FailCurrent("Priveden si / kazna do 120 DIN. Nezavršen posao nema isplatu.");game.Vitals.ReturnToCheckpoint();game.Hideout.ResetAfterRespawn();hud.ShowNotification("Privođenje / kazna naplaćena. Vrati se poslu bez nagrade za neuspešnu rutu.",7);progress.SaveNow();}
                if(Life.Escapes!=lastEscapes){lastEscapes=Life.Escapes;hud.ShowNotification("IZGUBIO SI POTERU / +30 XP. Blok pamti kako se vraćaš.",6);progress.SaveNow();}
                if(Life.Workouts!=lastWorkouts){lastWorkouts=Life.Workouts;hud.ShowNotification("TRENING ZAVRŠEN / +40 XP. Svaka 3 treninga podižu snagu.",6);progress.SaveNow();}
            }
            benchVisual.SetAppearance(game.Progression.Appearance.Outfit,Life.Strength);
            for(int i=0;i<3;i++)conditioningMarkers[i].SetActive(simulate&&Life.IsConditioning&&conditioning.Checkpoint==i);
            benchVisual.Pose(Life.IsTraining,bench!=null?bench.Extension:1);
            if(Life.Wanted&&!lastWanted){hud.ShowNotification("POTERA: zaklon prekida vidljivost. Policija mora da priđe za privođenje.",6);progress.SaveNow();}
            lastWanted=Life.Wanted;
            if(simulate&&Life.Wanted&&!game.Audio.Muted)
            {
                CityPoliceOfficer nearest=officers[0];
                foreach(CityPoliceOfficer officer in officers)if((officer.transform.position-game.Player.position).sqrMagnitude<(nearest.transform.position-game.Player.position).sqrMagnitude)nearest=officer;
                siren.transform.position=nearest.transform.position+Vector3.up*1.6f;
                if(Time.unscaledTime>=sirenProbe)
                {
                    sirenProbe=Time.unscaledTime+.25f;if(acoustics==null)acoustics=GetComponent<CitySpatialSoundscape>();
                    sirenFilter.cutoffFrequency=acoustics!=null&&acoustics.IsBlocked(siren.transform.position,ears.position)?1200:22000;
                }
                if(!siren.isPlaying){if(siren.timeSamples>0)siren.UnPause();else siren.Play();}
            }
            else if(siren.isPlaying)siren.Pause();
            uiCountdown-=Time.unscaledDeltaTime;if(uiCountdown<=0){uiCountdown=.15f;RefreshUi();}
        }
        private void RefreshUi()
        {
            shade.SetActive(IsMenuOpen);banner.transform.parent.gameObject.SetActive(!IsMenuOpen);
            banner.text=$"LVL {Life.Level} / XP {Life.Experience} / SNAGA {Life.Strength}\nHRANA {Life.Nutrition:0}% / ENERGIJA {Life.Stamina:0}% / L: BLOK";
            policeBanner.gameObject.SetActive(Life.Wanted&&!IsMenuOpen);policeBanner.color=Mathf.Sin(Time.unscaledTime*5)>0?new Color(.4f,.08f,.08f,.9f):new Color(.05f,.15f,.45f,.9f);
            workout.SetActive(Life.IsTraining||Life.IsConditioning);repButton.SetActive(!Life.IsConditioning);
            RectTransform workoutRect=(RectTransform)workout.transform,captionRect=(RectTransform)workoutText.transform.parent;
            workoutRect.sizeDelta=Life.IsConditioning?new Vector2(560,140):new Vector2(600,160);
            workoutRect.anchoredPosition=new Vector2(0,Life.IsConditioning?90:125);
            captionRect.sizeDelta=Life.IsConditioning?new Vector2(540,70):new Vector2(580,48);
            captionRect.anchoredPosition=new Vector2(0,Life.IsConditioning?30:50);workoutText.fontSize=Life.IsConditioning?18:20;
            cancelTrainingButton.anchoredPosition=new Vector2(0,Life.IsConditioning?-40:-55);
            workoutText.text=Life.IsConditioning?$"KONDICIJA {conditioning.Checkpoint+1}/3 / {conditioning.Remaining:0}s / STANI {conditioning.Hold:0.0}/1.2 s\nCilj {Vector3.Distance(game.Player.position,ConditioningTarget):0} m / +20 XP":$"{Life.Reps}/{CityLifestyle.RequiredReps} / {Life.TrainingRemaining:0.0}s / "+BenchPrompt();
            if(!IsMenuOpen)return;
            if(locationIndex>=0){var p=CityGeoRegistry.At(locationIndex);stats.text=$"DELOVI GRADA / {locationIndex+1}/{CityGeoRegistry.Count}\n{p.Name}\n{p.Latitude:F6}, {p.Longitude:F6}\nKorisnički kandidat, nije izgrađena zona.\n"+(p.ConflictingPin?"Lokacija je sporna: potrebno proveriti.":"Misije trenutno koriste centralnu igrivu zonu.")+"\nDELOVI GRADA: sledeći zapis.";return;}
            stats.text=$"RAZVOJ LIKA / LVL {Life.Level}\n"+(Life.Wanted?"STATUS: POTERA / igra je pauzirana\n":"STATUS: MIRNO\n")+$"Hrana {Life.Nutrition:0}% / energija {Life.Stamina:0}%\nTreninzi {Life.Workouts} / snaga {Life.Strength}/5\nObroci {Life.Meals} / bekstva {Life.Escapes} / privođenja {Life.Arrests}\nIZAZOV BLOKA: trening + obrok + bekstvo\n"+(Life.ChallengeClaimed?"ZAVRŠEN / 250 DIN + 10 REP + 60 XP":"Jednokratna nagrada: 250 DIN / 10 REP / 60 XP")+$"\nKlupa {Vector3.Distance(game.Player.position,Gym.position):0} m / kiosk {Vector3.Distance(game.Player.position,Food.position):0} m\nOdmor posle treninga {Life.WorkoutCooldown:0} s";
        }
        private string BenchPrompt()
        {
            if(bench==null)return "";
            switch(bench.Phase)
            {
                case CityBenchPhase.Lowering:return "SPUŠTAJ / sačekaj";
                case CityBenchPhase.PressWindow:return "GURAJ! F / TAP";
                case CityBenchPhase.Pressing:return "POTISAK / izdah";
                case CityBenchPhase.Recovery:return "ODMOR";
                default:return "F / TAP: SPUSTI";
            }
        }
        private static AudioClip CreateSiren()
        {
            const int rate=12000;float[] samples=new float[rate*2];double phase=0;
            for(int i=0;i<samples.Length;i++){double t=(double)i/rate;phase+=2*Math.PI*(740+150*Math.Sin(t*Math.PI*2))/rate;samples[i]=(float)Math.Sin(phase)*.35f;}
            AudioClip clip=AudioClip.Create("VKT_OriginalPatrolSiren",samples.Length,1,rate,false);clip.SetData(samples,0);return clip;
        }
        private void OnDeath(){Life.OnDeath();conditioning.Cancel();CloseMenu();progress.SaveNow();}
        private void OnApplicationPause(bool paused){applicationSuspended=paused;if(paused&&siren!=null)siren.Pause();}
        private void OnDisable(){if(game!=null){Life.CancelWorkout();Life.CancelConditioning();conditioning.Cancel();CloseMenu();}if(benchVisual!=null)benchVisual.Pose(false,1);if(siren!=null)siren.Stop();}
        private void OnDestroy()
        {
            if(game!=null)game.Vitals.Died-=OnDeath;
            if(siren!=null){siren.Stop();Destroy(siren);}if(sirenClip!=null)Destroy(sirenClip);
            if(world!=null)Destroy(world);if(uiRoot!=null)Destroy(uiRoot);foreach(UnityEngine.Object item in new UnityEngine.Object[]{rounded,metal,wood,blue,red})if(item!=null)Destroy(item);
        }
    }
    public sealed class CityLifestyleStation : CityInteractable
    {
        private CityLifestyleRuntime owner;private bool food;
        public void Initialize(CityLifestyleRuntime runtime,bool isFood){owner=runtime;food=isFood;}
        public override string Prompt=>food?"[E] Kiosk / kupi obrok ili rizikuj poteru":"[E] Ulična teretana / trening";
        public override bool TryInteract(CitySession session){if(owner==null||!owner.CanSimulate)return false;owner.OpenMenu();return owner.IsMenuOpen;}
    }
}
