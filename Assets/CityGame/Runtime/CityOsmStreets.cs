using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Pozarevac.CityGame
{
    // Flattened OSM road centerlines; road widths are authored, not survey measurements.
    public sealed class CityOsmStreets : MonoBehaviour
    {
        [Serializable] public sealed class Point { public float x,z; }
        [Serializable] public sealed class Road { public string id,name,kind; public Point[] points; }
        [Serializable] public sealed class Building { public string id,heightSource;public float height;public Point[] points; }
        [Serializable] public sealed class Data
        {
            public int schema;public string source,downloadedUtc,bbox,attribution,license,licenseUrl;
            public double anchorLongitude,anchorLatitude;public float radiusMeters;public Road[] roads;public Building[] buildings;
        }
        private GameObject layout;
        private Material roadMaterial,walkMaterial,roofMaterial;
        private readonly List<Material> facadeMaterials=new List<Material>(4);
        private Mesh mesh;
        private readonly List<Mesh> buildingMeshes=new List<Mesh>(40);
        public int SegmentCount {get;private set;}
        public int WallColliderCount {get;private set;}
        public Data Source {get;private set;}
        public static Data Load()
        {
            TextAsset asset=Resources.Load<TextAsset>("Maps/PozarevacStreets");
            if(asset==null)return null;
            Data data=JsonUtility.FromJson<Data>(asset.text);
            if(data==null||data.schema!=1||data.roads==null||data.roads.Length==0||data.radiusMeters<=0||data.radiusMeters>300)throw new InvalidOperationException("Unsupported offline OSM dataset.");
            return data;
        }
        public void Initialize(Transform georoot,Vector3 groundAnchor)
        {
            if(layout!=null)throw new InvalidOperationException("OSM streets already initialized.");
            if(georoot==null)throw new ArgumentNullException(nameof(georoot));
            Source=Load();if(Source==null)throw new InvalidOperationException("Offline OSM street dataset is missing; use the authored fallback or import Maps/PozarevacStreets.");layout=new GameObject("VKT_OSM_RealRoadLayout");layout.transform.SetParent(georoot,false);layout.transform.position=groundAnchor;
            Material template=Resources.Load<Material>("CityMissionMarker");if(template==null)throw new InvalidOperationException("Street material is missing.");
            roadMaterial=new Material(template);roadMaterial.color=new Color(.14f,.17f,.19f);PrepareMaterial(roadMaterial);
            walkMaterial=new Material(template);walkMaterial.color=new Color(.49f,.47f,.41f);PrepareMaterial(walkMaterial);
            Color[] facadeColors={new Color(.67f,.61f,.49f),new Color(.68f,.56f,.47f),new Color(.62f,.64f,.57f),new Color(.76f,.70f,.58f)};
            Texture2D facadeAtlas=Resources.Load<Texture2D>("Art/CityFacades");
            foreach(Color color in facadeColors)
            {
                Material facade=new Material(template);facade.color=facadeAtlas!=null?Color.white:color;
                if(facadeAtlas!=null){facade.mainTexture=facadeAtlas;if(facade.HasProperty("_BaseMap"))facade.SetTexture("_BaseMap",facadeAtlas);}
                PrepareMaterial(facade);if(facade.HasProperty("_Cull"))facade.SetFloat("_Cull",0);facadeMaterials.Add(facade);
            }
            roofMaterial=new Material(template);roofMaterial.color=new Color(.40f,.19f,.12f);PrepareMaterial(roofMaterial);
            List<Vector3> vertices=new List<Vector3>(800);List<int> triangles=new List<int>(1200);HashSet<string> labels=new HashSet<string>();
            foreach(Road road in Source.roads)
            {
                if(road.points==null||road.points.Length<2)continue;
                for(int i=1;i<road.points.Length&&SegmentCount<200;i++)
                {
                    Vector3 a=new Vector3(road.points[i-1].x,.035f,road.points[i-1].z),b=new Vector3(road.points[i].x,.035f,road.points[i].z);
                    if(!Valid(a)||!Valid(b)||(a-b).sqrMagnitude<.01f)continue;
                    float width=road.kind=="footway"?2f:road.kind=="pedestrian"?5f:road.kind=="primary"||road.kind=="secondary"?9f:6f;
                    Vector3 normal=Vector3.Cross((b-a).normalized,Vector3.up)*width*.5f;int at=vertices.Count;
                    vertices.Add(a-normal);vertices.Add(a+normal);vertices.Add(b-normal);vertices.Add(b+normal);
                    triangles.Add(at);triangles.Add(at+1);triangles.Add(at+2);triangles.Add(at+1);triangles.Add(at+3);triangles.Add(at+2);SegmentCount++;
                    if(!string.IsNullOrEmpty(road.name)&&labels.Add(road.name))Label(road.name,(a+b)*.5f+Vector3.up*2);
                }
                if(SegmentCount>=200)break;
            }
            if(SegmentCount==0)throw new InvalidOperationException("No valid OSM segments were available.");
            mesh=new Mesh{name="OSM centerline road ribbons"};mesh.SetVertices(vertices);mesh.SetTriangles(triangles,0);mesh.RecalculateNormals();mesh.RecalculateBounds();
            layout.AddComponent<MeshFilter>().sharedMesh=mesh;layout.AddComponent<MeshRenderer>().sharedMaterial=roadMaterial;
            // A single bounded walkable ground plane; roads retain actual node layout without invented elevation.
            GameObject floor=GameObject.CreatePrimitive(PrimitiveType.Cube);floor.name="OSM_FlatOfflineGround";floor.transform.SetParent(layout.transform,false);
            floor.transform.localPosition=new Vector3(0,-.15f,0);floor.transform.localScale=new Vector3(Source.radiusMeters*2,.3f,Source.radiusMeters*2);floor.GetComponent<Renderer>().sharedMaterial=walkMaterial;
            BuildFootprints();
            GameObject notice=new GameObject("OSM_Attribution",typeof(RectTransform),typeof(Canvas),typeof(CanvasScaler));notice.transform.SetParent(layout.transform,false);
            Canvas canvas=notice.GetComponent<Canvas>();canvas.renderMode=RenderMode.ScreenSpaceOverlay;canvas.sortingOrder=5;
            CanvasScaler scaler=notice.GetComponent<CanvasScaler>();scaler.uiScaleMode=CanvasScaler.ScaleMode.ScaleWithScreenSize;scaler.referenceResolution=new Vector2(1280,720);
            GameObject text=new GameObject("Attribution",typeof(RectTransform),typeof(Text));text.transform.SetParent(notice.transform,false);
            RectTransform rect=text.GetComponent<RectTransform>();rect.anchorMin=new Vector2(0,0);rect.anchorMax=new Vector2(1,0);rect.pivot=new Vector2(.5f,0);rect.sizeDelta=new Vector2(0,42);rect.anchoredPosition=new Vector2(0,3);
            Text caption=text.GetComponent<Text>();caption.font=Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");caption.fontSize=13;caption.alignment=TextAnchor.LowerCenter;caption.color=Color.white;caption.raycastTarget=false;
            caption.text="© OpenStreetMap contributors · ODbL · openstreetmap.org/copyright\nStvarni raspored puteva / ravna podloga i širine prilagođene igri";
        }
        private void BuildFootprints()
        {
            if(Source.buildings==null)return;
            int count=0;
            foreach(Building building in Source.buildings)
            {
                if(count>=40)break;
                if(building.points==null||building.points.Length<4||building.points.Length>128||float.IsNaN(building.height)||float.IsInfinity(building.height))continue;
                List<Vector3> vertices=new List<Vector3>();List<int> walls=new List<int>();List<int> roofs=new List<int>();List<Vector2> uv=new List<Vector2>();
                float height=Mathf.Clamp(building.height,3,40);
                int tile=count%4;float u=(tile%2)*.5f,v=(1-tile/2)*.5f;
                int entrance=EntranceEdge(building);
                for(int i=1;i<building.points.Length;i++)
                {
                    Vector3 a=new Vector3(building.points[i-1].x,0,building.points[i-1].z),b=new Vector3(building.points[i].x,0,building.points[i].z);
                    if(!Valid(a)||!Valid(b)||(b-a).sqrMagnitude<.01f)continue;
                    if(i!=entrance){AppendFacadeSpan(a,b,0,height,height,u,v,vertices,walls,uv);continue;}
                    Vector3 direction=(b-a).normalized,mid=(a+b)*.5f;
                    Vector3 left=mid-direction*1.2f,right=mid+direction*1.2f;
                    AppendFacadeSpan(a,left,0,height,height,u,v,vertices,walls,uv);
                    AppendFacadeSpan(right,b,0,height,height,u,v,vertices,walls,uv);
                    AppendFacadeSpan(left,right,2.5f,height,height,u,v,vertices,walls,uv);
                }                if(vertices.Count==0)continue;
                AddRoof(building.points,height,vertices,roofs);
                while(uv.Count<vertices.Count)uv.Add(Vector2.zero);
                Mesh facade=new Mesh{name="OSM footprint "+building.id};facade.SetVertices(vertices);facade.SetUVs(0,uv);facade.subMeshCount=2;
                facade.SetTriangles(walls,0);facade.SetTriangles(roofs,1);facade.RecalculateNormals();facade.RecalculateBounds();buildingMeshes.Add(facade);
                GameObject block=new GameObject("OSM_Building_"+building.id);block.transform.SetParent(layout.transform,false);
                block.AddComponent<MeshFilter>().sharedMesh=facade;block.AddComponent<MeshRenderer>().sharedMaterials=new[]{facadeMaterials[tile],roofMaterial};
                BuildWallCollision(block.transform,building,height);count++;
            }
        }        private static void AppendFacadeSpan(Vector3 a,Vector3 b,float bottom,float top,float height,float u,float v,List<Vector3> vertices,List<int> triangles,List<Vector2> uv)
        {
            if(top-bottom<.05f||(b-a).sqrMagnitude<.0025f)return;
            int sections=Mathf.CeilToInt(Vector3.Distance(a,b)/8f);
            for(int section=0;section<sections;section++)
            {
                Vector3 from=Vector3.Lerp(a,b,(float)section/sections),to=Vector3.Lerp(a,b,(float)(section+1)/sections);
                int at=vertices.Count;vertices.Add(from+Vector3.up*bottom);vertices.Add(to+Vector3.up*bottom);vertices.Add(from+Vector3.up*top);vertices.Add(to+Vector3.up*top);
                float low=v+.002f+.496f*bottom/height,high=v+.002f+.496f*top/height;
                uv.Add(new Vector2(u+.002f,low));uv.Add(new Vector2(u+.498f,low));uv.Add(new Vector2(u+.002f,high));uv.Add(new Vector2(u+.498f,high));
                triangles.AddRange(new[]{at,at+2,at+1,at+1,at+2,at+3});
            }
        }
        private int EntranceEdge(Building building)
        {
            int entrance=-1;float nearest=float.MaxValue;
            for(int i=1;i<building.points.Length;i++)
            {
                Vector3 a=new Vector3(building.points[i-1].x,0,building.points[i-1].z),b=new Vector3(building.points[i].x,0,building.points[i].z);
                if(!Valid(a)||!Valid(b)||Vector3.Distance(a,b)<3.4f)continue;
                float distance=((a+b)*.5f).sqrMagnitude;if(distance<nearest){nearest=distance;entrance=i;}
            }
            return entrance;
        }        private void BuildWallCollision(Transform buildingRoot,Building building,float height)
        {
            int entrance=EntranceEdge(building);
            for(int i=1;i<building.points.Length&&WallColliderCount<512;i++)
            {
                Vector3 a=new Vector3(building.points[i-1].x,0,building.points[i-1].z),b=new Vector3(building.points[i].x,0,building.points[i].z);
                if(!Valid(a)||!Valid(b)||(b-a).sqrMagnitude<.01f)continue;
                if(i!=entrance){WallSpan(buildingRoot,a,b,0,height);continue;}
                // Authored gameplay doorway, not an OSM assertion: leave an escape opening.
                Vector3 direction=(b-a).normalized,mid=(a+b)*.5f;
                Vector3 left=mid-direction*1.2f,right=mid+direction*1.2f;
                WallSpan(buildingRoot,a,left,0,height);WallSpan(buildingRoot,right,b,0,height);
                WallSpan(buildingRoot,left,right,2.5f,height);
                GameObject doorway=new GameObject("OSM_Doorway");doorway.transform.SetParent(buildingRoot,false);doorway.transform.localPosition=mid;doorway.transform.localRotation=Quaternion.LookRotation(b-a,Vector3.up);
            }
        }
        private void WallSpan(Transform parent,Vector3 a,Vector3 b,float bottom,float top)
        {
            float length=Vector3.Distance(a,b);
            if(WallColliderCount>=512||length<.05f||top-bottom<.05f)return;
            GameObject wall=new GameObject("OSM_WallCollision");wall.transform.SetParent(parent,false);
            wall.transform.localPosition=(a+b)*.5f+Vector3.up*((bottom+top)*.5f);wall.transform.localRotation=Quaternion.LookRotation(b-a,Vector3.up);
            BoxCollider collider=wall.AddComponent<BoxCollider>();collider.size=new Vector3(.24f,top-bottom,length);WallColliderCount++;
        }        private static void PrepareMaterial(Material material)
        {
            material.DisableKeyword("_EMISSION");
            if(material.HasProperty("_EmissionColor"))material.SetColor("_EmissionColor",Color.black);
            if(material.HasProperty("_Smoothness"))material.SetFloat("_Smoothness",.12f);
            if(material.HasProperty("_Metallic"))material.SetFloat("_Metallic",0f);
        }
        private static float Cross(Vector2 a,Vector2 b,Vector2 c)=>(b.x-a.x)*(c.y-a.y)-(b.y-a.y)*(c.x-a.x);
        private static bool InTriangle(Vector2 p,Vector2 a,Vector2 b,Vector2 c)
            =>Cross(a,b,p)>=-.0001f&&Cross(b,c,p)>=-.0001f&&Cross(c,a,p)>=-.0001f;
        private void AddRoof(Point[] points,float height,List<Vector3> vertices,List<int> triangles)
        {
            List<Vector2> polygon=new List<Vector2>(points.Length);
            foreach(Point point in points)
            {
                Vector3 at=new Vector3(point.x,0,point.z);if(!Valid(at))return;
                Vector2 flat=new Vector2(point.x,point.z);
                if(polygon.Count==0||(polygon[polygon.Count-1]-flat).sqrMagnitude>.0001f)polygon.Add(flat);
            }
            if(polygon.Count>1&&(polygon[0]-polygon[polygon.Count-1]).sqrMagnitude<.0001f)polygon.RemoveAt(polygon.Count-1);
            if(polygon.Count<3||polygon.Count>128)return;
            // Reject self-intersections before clipping rather than invent a roof outside the footprint.
            for(int i=0;i<polygon.Count;i++)for(int j=i+1;j<polygon.Count;j++)
            {
                int nextI=(i+1)%polygon.Count,nextJ=(j+1)%polygon.Count;
                if(nextI==j||nextJ==i)continue;
                Vector2 a=polygon[i],b=polygon[nextI],d=polygon[j],e=polygon[nextJ];
                if(Cross(a,b,d)*Cross(a,b,e)<-.000001f&&Cross(d,e,a)*Cross(d,e,b)<-.000001f)return;
            }
            float area=0;
            for(int i=0;i<polygon.Count;i++){Vector2 a=polygon[i],b=polygon[(i+1)%polygon.Count];area+=a.x*b.y-b.x*a.y;}
            if(Mathf.Abs(area)<.001f)return;
            List<int> remaining=new List<int>(polygon.Count);
            for(int i=0;i<polygon.Count;i++)remaining.Add(area>0?i:polygon.Count-1-i);
            List<int> roof=new List<int>((polygon.Count-2)*3);int budget=polygon.Count*polygon.Count;
            while(remaining.Count>3&&budget-->0)
            {
                bool clipped=false;
                for(int i=0;i<remaining.Count;i++)
                {
                    int a=remaining[(i+remaining.Count-1)%remaining.Count],b=remaining[i],d=remaining[(i+1)%remaining.Count];
                    if(Cross(polygon[a],polygon[b],polygon[d])<=.0001f)continue;
                    bool occupied=false;
                    for(int n=0;n<remaining.Count;n++)
                    {
                        int test=remaining[n];if(test==a||test==b||test==d)continue;
                        if(InTriangle(polygon[test],polygon[a],polygon[b],polygon[d])){occupied=true;break;}
                    }
                    if(occupied)continue;
                    roof.Add(a);roof.Add(d);roof.Add(b);remaining.RemoveAt(i);clipped=true;break;
                }
                if(!clipped)return;
            }
            if(remaining.Count!=3||Cross(polygon[remaining[0]],polygon[remaining[1]],polygon[remaining[2]])<=.0001f)return;
            roof.Add(remaining[0]);roof.Add(remaining[2]);roof.Add(remaining[1]);
            int offset=vertices.Count;
            foreach(Vector2 at in polygon)vertices.Add(new Vector3(at.x,height,at.y));
            foreach(int index in roof)triangles.Add(offset+index);
        }
        private bool Valid(Vector3 point)=>!float.IsNaN(point.x)&&!float.IsInfinity(point.x)&&!float.IsNaN(point.z)&&!float.IsInfinity(point.z)&&Mathf.Abs(point.x)<=Source.radiusMeters&&Mathf.Abs(point.z)<=Source.radiusMeters;
        private void Label(string text,Vector3 at)
        {
            GameObject obj=new GameObject("OSM_"+text);obj.transform.SetParent(layout.transform,false);obj.transform.localPosition=at;
            TextMesh label=obj.AddComponent<TextMesh>();label.font=Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");label.text=text;label.fontSize=40;label.characterSize=.18f;label.anchor=TextAnchor.MiddleCenter;label.color=Color.white;obj.GetComponent<Renderer>().sharedMaterial=label.font.material;
        }
        private static void Release(UnityEngine.Object value)
        {
            if(value==null)return;
            if(Application.isPlaying)Destroy(value);else DestroyImmediate(value);
        }
        private void OnDestroy()
        {
            Release(layout);Release(mesh);
            foreach(Mesh facade in buildingMeshes)Release(facade);
            Release(roadMaterial);Release(walkMaterial);Release(roofMaterial);
            foreach(Material material in facadeMaterials)Release(material);
        }
    }
}
