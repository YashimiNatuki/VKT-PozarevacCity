using System;
using System.Collections.Generic;
using UnityEngine;

namespace Pozarevac.CityGame
{
    // Optional offline streetscape. Never overlays or replaces streamed city geometry implicitly.
    public sealed class CityCityDetail : MonoBehaviour
    {
        private Transform root;
        private CityWorldAtmosphere atmosphere;
        private readonly List<Light> lamps = new List<Light>(8);
        private readonly List<Material> materials = new List<Material>(9);
        private Material road, stone, cream, roof, glass, wood, green, metal, white;
        private bool night;
        public bool IsInitialized => root != null;
        public Transform PlaceMarker(int index) => root.Find("Place_" + CityPlaceCatalog.At(index).Id);
        public void Initialize(Transform georoot, Vector3 nearspawn, CityWorldAtmosphere worldAtmosphere)
        {
            if (root != null) throw new InvalidOperationException("City detail may only be initialized once.");
            if (georoot == null) throw new ArgumentNullException(nameof(georoot));
            atmosphere = worldAtmosphere;
            root = new GameObject("VKT_Autorski_Centar").transform; root.SetParent(georoot, false); root.position = nearspawn;
            road = Material(new Color(.12f,.14f,.16f)); stone = Material(new Color(.57f,.55f,.49f));
            cream = Material(new Color(.82f,.72f,.52f)); roof = Material(new Color(.36f,.13f,.08f));
            glass = Material(new Color(.12f,.28f,.34f)); wood = Material(new Color(.29f,.14f,.06f));
            green = Material(new Color(.14f,.34f,.12f)); metal = Material(new Color(.08f,.10f,.10f)); white = Material(new Color(.89f,.87f,.74f));
            Box("Kolovoz", new Vector3(0,-.06f,0), new Vector3(106,.12f,12), road);
            Box("Poprecna_ulica", new Vector3(0,-.06f,18), new Vector3(12,.12f,48), road);
            for (int side = -1; side <= 1; side += 2)
            {
                Box("Trotoar", new Vector3(0,.02f,side*8), new Vector3(106,.16f,4), stone);
                for (int n=0;n<6;n++)
                {
                    float x=-45+n*18;
                    Building(new Vector3(x,0,side*16), n, side);
                    Bench(new Vector3(x+5,.15f,side*8));
                    if (n%2==0) Lamp(new Vector3(x,.15f,side*9));
                }
            }
            for(int i=0;i<7;i++) Box("Zebra",new Vector3(-5+i*1.6f,.025f,0),new Vector3(.8f,.03f,10),white);
            Box("Trg",new Vector3(26,.01f,33),new Vector3(38,.16f,28),stone);
            Box("Park",new Vector3(32,.13f,36),new Vector3(21,.12f,16),green);
            Box("Spomenik_postament",new Vector3(30,.8f,34),new Vector3(2,1.6f,2),stone);
            Box("Stilizovana_figura",new Vector3(30,2.4f,34),new Vector3(.7f,1.7f,.6f),metal);
            Sphere("Glava",new Vector3(30,3.45f,34),new Vector3(.5f,.5f,.5f),metal);
            Label("KNJAZ MILOŠ\nStilizovani orijentir",new Vector3(30,1.1f,32.95f),.12f);
            for(int i=0;i<6;i++) Tree(new Vector3(20+(i%3)*11,.2f,27+(i/3)*17));
            for(int i=0;i<CityPlaceCatalog.Count;i++)
            {
                CityPlaceCatalog.Place place=CityPlaceCatalog.At(i);
                Transform marker=new GameObject("Place_"+place.Id).transform; marker.SetParent(root,false); marker.localPosition=place.Offset;
                Box("Tabla_"+place.Id,place.Offset+Vector3.up*2.6f,new Vector3(7,.8f,.15f),metal);
                Label(place.Name,place.Offset+new Vector3(0,2.6f,-.09f),.14f);
            }
            Label(CityPlaceCatalog.Attribution+"\n"+CityPlaceCatalog.GeometryNotice,new Vector3(-15,3,10),.13f);
            UpdateLighting(true);
        }
        private Material Material(Color color)
        {
            Material template=Resources.Load<Material>("CityMissionMarker");
            if(template==null) throw new InvalidOperationException("City details require CityMissionMarker material.");
            Material mat=new Material(template); mat.color=color; materials.Add(mat); return mat;
        }
        private GameObject Shape(string name,Vector3 position,Vector3 size,Material material,PrimitiveType type)
        {
            GameObject obj=GameObject.CreatePrimitive(type); obj.name=name; obj.transform.SetParent(root,false); obj.transform.localPosition=position; obj.transform.localScale=size;
            obj.GetComponent<Renderer>().sharedMaterial=material; return obj;
        }
        private GameObject Box(string name,Vector3 position,Vector3 size,Material material)=>Shape(name,position,size,material,PrimitiveType.Cube);
        private void Sphere(string name,Vector3 position,Vector3 size,Material material)=>Shape(name,position,size,material,PrimitiveType.Sphere);
        private void Building(Vector3 at,int index,int side)
        {
            float height=5+(index%3)*1.2f;
            Box("Radnja",at+Vector3.up*height/2,new Vector3(12,height,10),cream);
            Box("Krov",at+Vector3.up*(height+.25f),new Vector3(12.5f,.5f,10.5f),roof);
            float front=at.z-side*5.06f;
            for(int window=0;window<3;window++) Box("Izlog",new Vector3(at.x-4+window*4,2,front),new Vector3(2.2f,2,.08f),glass);
            Box("Nadstresnica",new Vector3(at.x,3.5f,front-side*.5f),new Vector3(11,.2f,1.2f),roof);
            if(side==1) Label(index%2==0?"BICIKL SERVIS":"ZANATSKA RADNJA",new Vector3(at.x,4.25f,front-.08f),.13f);
        }
        private void Bench(Vector3 at)
        {
            Box("Klupa_sediste",at+Vector3.up*.5f,new Vector3(2,.16f,.65f),wood);
            Box("Klupa_naslon",at+new Vector3(0,.95f,.28f),new Vector3(2,.7f,.12f),wood);
            for(int side=-1;side<=1;side+=2) Box("Klupa_noga",at+new Vector3(side*.7f,.25f,0),new Vector3(.15f,.5f,.5f),metal);
        }
        private void Tree(Vector3 at)
        {
            Box("Stablo",at+Vector3.up*1.5f,new Vector3(.35f,3,.35f),wood);
            Sphere("Krosnja",at+Vector3.up*3.4f,new Vector3(3,3,3),green);
        }
        private void Lamp(Vector3 at)
        {
            Box("Kandelaber",at+Vector3.up*2.5f,new Vector3(.15f,5,.15f),metal);
            Sphere("Fenjer",at+Vector3.up*5,new Vector3(.6f,.5f,.6f),white);
            GameObject obj=new GameObject("Toplo_ulicno_svetlo");obj.transform.SetParent(root,false);obj.transform.localPosition=at+Vector3.up*4.7f;
            Light lamp=obj.AddComponent<Light>();lamp.type=LightType.Point;lamp.color=new Color(1,.72f,.38f);lamp.range=14;lamp.intensity=2;lamp.shadows=LightShadows.None;lamps.Add(lamp);
        }
        private void Label(string text,Vector3 at,float size)
        {
            GameObject obj=new GameObject("Natpis");obj.transform.SetParent(root,false);obj.transform.localPosition=at;
            TextMesh mesh=obj.AddComponent<TextMesh>();mesh.text=text;mesh.font=Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");mesh.fontSize=36;mesh.characterSize=size;mesh.anchor=TextAnchor.MiddleCenter;mesh.color=Color.white;
            obj.GetComponent<Renderer>().sharedMaterial=mesh.font.material;
        }
        private void Update(){if(root!=null)UpdateLighting(false);}
        private void UpdateLighting(bool force)
        {
            bool next=atmosphere!=null&&!atmosphere.IsDay;if(!force&&next==night)return;night=next;
            foreach(Light lamp in lamps)lamp.enabled=night;
        }
        private void OnDestroy(){if(root!=null)Destroy(root.gameObject);foreach(Material material in materials)if(material!=null)Destroy(material);}
    }
}
