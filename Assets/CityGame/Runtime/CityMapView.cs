using System;
using UnityEngine;
using UnityEngine.UI;

namespace Pozarevac.CityGame
{
    public sealed class CityMapView : MonoBehaviour
    {
        private const int Size=512;
        private Texture2D texture;
        private Color32[] pixels;
        private CityGameBootstrap game;
        private CityOsmStreets streets;
        private CityOsmStreets.Data data;
        private RectTransform marker;
        private Text caption;
        private Text toggleLabel;
        private CityReliefMap relief;
        private float radius;
        private float timer;
        private bool initialized;
        public bool HasMap => data!=null;
        public bool ReliefActive { get; private set; }
        public void Initialize()
        {
            if(initialized)return;initialized=true;
            game=FindAnyObjectByType<CityGameBootstrap>();
            streets=game!=null?game.GetComponent<CityOsmStreets>():null;
            caption=Label("MapStatus",new Vector2(0,-310),new Vector2(650,100),16);
            try{data=CityOsmStreets.Load();}catch(Exception){data=null;}
            if(data==null){caption.text="Mapa nije dostupna. Uvezi offline OSM podatke pa ponovo pokreni igru.";return;}
            radius=data.radiusMeters;relief=CityReliefMap.Load(data);
            GameObject image=new GameObject("StreetMap",typeof(RectTransform),typeof(RawImage));image.transform.SetParent(transform,false);
            RectTransform rect=image.GetComponent<RectTransform>();rect.sizeDelta=new Vector2(630,630);rect.anchoredPosition=new Vector2(0,45);
            texture=new Texture2D(Size,Size,TextureFormat.RGBA32,false);texture.name="VKT owned offline map";texture.filterMode=FilterMode.Bilinear;texture.wrapMode=TextureWrapMode.Clamp;
            pixels=new Color32[Size*Size];Redraw();
            RawImage raw=image.GetComponent<RawImage>();raw.texture=texture;raw.raycastTarget=false;
            GameObject point=new GameObject("PlayerMarker",typeof(RectTransform),typeof(Image));point.transform.SetParent(image.transform,false);
            marker=point.GetComponent<RectTransform>();marker.sizeDelta=new Vector2(14,14);point.GetComponent<Image>().color=new Color(1,.35f,.12f);point.GetComponent<Image>().raycastTarget=false;
            Label("North",new Vector2(280,375),new Vector2(80,30),20).text="N ↑";
            if(relief!=null)
            {
                var button=new GameObject("ReliefToggle",typeof(RectTransform),typeof(Image),typeof(Button));button.transform.SetParent(transform,false);
                RectTransform area=button.GetComponent<RectTransform>();area.sizeDelta=new Vector2(280,34);area.anchoredPosition=new Vector2(-175,380);
                button.GetComponent<Image>().color=new Color(.12f,.24f,.27f);
                toggleLabel=Label("ReliefToggleLabel",Vector2.zero,new Vector2(270,30),18);toggleLabel.transform.SetParent(button.transform,false);
                toggleLabel.text="RELJEF OKOLINE / 4 km";
                button.GetComponent<Button>().onClick.AddListener(()=>
                {
                    CityPhonePanel panel=GetComponentInParent<CityPhonePanel>();if(panel!=null)panel.NotifyInteraction();
                    ShowRelief(!ReliefActive);
                });
            }
            RefreshPosition();
        }
        public void ShowRelief(bool active)
        {
            if(data==null||texture==null)return;
            active&=relief!=null;if(active==ReliefActive)return;
            ReliefActive=active;radius=active?relief.Radius:data.radiusMeters;
            if(toggleLabel!=null)toggleLabel.text=active?"VRATI ULICE / 1,3 km":"RELJEF OKOLINE / 4 km";
            Redraw();RefreshPosition();
        }
        private void Redraw()
        {
            for(int y=0;y<Size;y++)for(int x=0;x<Size;x++)
                pixels[y*Size+x]=ReliefActive?relief.Shade((x/(float)(Size-1)*2-1)*radius,(y/(float)(Size-1)*2-1)*radius):new Color32(23,33,36,255);
            if(data.parks!=null)foreach(CityOsmStreets.Park park in data.parks)Draw(park.points,new Color32(66,142,86,255));
            if(data.buildings!=null)foreach(CityOsmStreets.Building building in data.buildings)Draw(building.points,new Color32(157,134,105,255));
            foreach(CityOsmStreets.Road road in data.roads)Draw(road.points,new Color32(222,221,198,255));
            if(ReliefActive)
            {
                int low=Pixel(-data.radiusMeters),high=Pixel(data.radiusMeters);
                Color32 border=new Color32(244,179,68,255);
                for(int p=low;p<=high;p++)if((p/6)%2==0)
                {pixels[low*Size+p]=border;pixels[high*Size+p]=border;pixels[p*Size+low]=border;pixels[p*Size+high]=border;}
            }
            texture.SetPixels32(pixels);texture.Apply(false,false);
        }
        private Text Label(string name,Vector2 at,Vector2 size,int fontSize)
        {
            GameObject obj=new GameObject(name,typeof(RectTransform),typeof(Text));obj.transform.SetParent(transform,false);
            RectTransform rect=obj.GetComponent<RectTransform>();rect.sizeDelta=size;rect.anchoredPosition=at;
            Text text=obj.GetComponent<Text>();text.font=Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");text.fontSize=fontSize;text.alignment=TextAnchor.MiddleCenter;text.color=Color.white;text.raycastTarget=false;return text;
        }
        private void Draw(CityOsmStreets.Point[] points,Color32 color)
        {
            if(points==null)return;
            int limit=Mathf.Min(points.Length,1024);
            for(int i=1;i<limit;i++)
            {
                CityOsmStreets.Point a=points[i-1],b=points[i];
                if(!Finite(a.x)||!Finite(a.z)||!Finite(b.x)||!Finite(b.z))continue;
                int x0=Pixel(a.x),y0=Pixel(a.z),x1=Pixel(b.x),y1=Pixel(b.z);
                int dx=Mathf.Abs(x1-x0),sx=x0<x1?1:-1,dy=-Mathf.Abs(y1-y0),sy=y0<y1?1:-1,error=dx+dy;
                for(int budget=0;budget<1024;budget++)
                {
                    pixels[y0*Size+x0]=color;if(x0==x1&&y0==y1)break;
                    int twice=2*error;if(twice>=dy){error+=dy;x0+=sx;}if(twice<=dx){error+=dx;y0+=sy;}
                }
            }
        }
        private int Pixel(float meters)=>Mathf.Clamp(Mathf.RoundToInt((meters/radius*.5f+.5f)*(Size-1)),0,Size-1);
        private static bool Finite(float value)=>!float.IsNaN(value)&&!float.IsInfinity(value);
        public void RefreshPosition()
        {
            if(data==null||caption==null)return;
            string legend=(ReliefActive?$"\nDSM ~30 m / {relief.Minimum:0}–{relief.Maximum:0} m EGM2008 / konture 10 m\nŽUTA GRANICA: igrivi centar / visine ulica nisu proverene\nCopernicus GLO-30 / zgrade i vegetacija":"\nRaspored OSM / širine i visine prilagođene igri")+
                "\n© OpenStreetMap contributors / ODbL / openstreetmap.org/copyright";
            if(game==null||game.Player==null||streets==null){if(marker!=null)marker.gameObject.SetActive(false);caption.text="Offline mapa / položaj igrača nije dostupan."+legend;return;}
            bool hideout=game.Hideout!=null&&game.Hideout.IsInside;
            Vector3 worldPosition=hideout&&game.Hideout.Entrance!=null?game.Hideout.Entrance.position:game.Player.position;
            Vector3 displacement=worldPosition-streets.GroundAnchor;
            Vector3 local=game.Player.parent!=null?game.Player.parent.InverseTransformVector(displacement):displacement;
            bool outside=Mathf.Abs(local.x)>radius||Mathf.Abs(local.z)>radius||hideout;
            marker.anchoredPosition=new Vector2(Mathf.Clamp(local.x/radius,-1,1)*315,Mathf.Clamp(local.z/radius,-1,1)*315);
            caption.text=(hideout?"TI: ŠTEK / marker označava projekciju ulaza":outside?"TI: izvan prikaza / marker na ivici":"NARANDŽASTO: TI / sever gore")+
                $" / ±{radius:0} m"+legend;
        }
        private void Update(){timer-=Time.unscaledDeltaTime;if(timer<=0){timer=.2f;RefreshPosition();}}
        private void OnDestroy(){if(texture!=null){if(Application.isPlaying)Destroy(texture);else DestroyImmediate(texture);}}
    }
}
