using System;
using UnityEngine;

namespace Pozarevac.CityGame
{
    // Authored props inside OSM green outlines; these are not surveyed tree/bench positions.
    public sealed class CityParkDetails : MonoBehaviour
    {
        private GameObject root;
        private Material wood, green, metal;
        public int PlacedBenches { get; private set; }
        public int PlacedTrees { get; private set; }
        public void Initialize(Transform parent, Vector3 anchor, CityOsmStreets.Data data)
        {
            if(root!=null)throw new InvalidOperationException("Park details already initialized.");
            if(parent==null||data==null)throw new ArgumentNullException();
            root=new GameObject("VKT_AuthoredParkProps");root.transform.SetParent(parent,false);root.transform.position=anchor;
            Material template=Resources.Load<Material>("CityMissionMarker");
            if(template==null)throw new InvalidOperationException("Park material template missing.");
            wood=CreateMaterial(template,new Color(.29f,.18f,.09f));green=CreateMaterial(template,new Color(.13f,.28f,.10f));metal=CreateMaterial(template,new Color(.11f,.14f,.15f));
            if(data.parks==null)return;
            foreach(var park in data.parks)
            {
                if(park.points==null||park.points.Length<4)continue;
                Vector2 low=new Vector2(float.MaxValue,float.MaxValue),high=new Vector2(float.MinValue,float.MinValue);
                foreach(var point in park.points){low=Vector2.Min(low,new Vector2(point.x,point.z));high=Vector2.Max(high,new Vector2(point.x,point.z));}
                int candidates=0;
                for(float z=low.y+3;z<high.y&&candidates<128;z+=9)
                for(float x=low.x+3;x<high.x&&candidates++<128;x+=9)
                {
                    Vector2 at=new Vector2(x,z);
                    if(!IsInside(at,park.points)||!IsClear(at,data))continue;
                    if(PlacedBenches<12){Bench(new Vector3(x,0,z));PlacedBenches++;}
                    else if(PlacedTrees<24){Tree(new Vector3(x,0,z));PlacedTrees++;}
                    if(PlacedBenches>=12&&PlacedTrees>=24)return;
                }
            }
        }
        public static bool IsInside(Vector2 point, CityOsmStreets.Point[] polygon)
        {
            if(polygon==null||polygon.Length<3)return false;
            bool inside=false;
            for(int i=0,j=polygon.Length-1;i<polygon.Length;j=i++)
            {
                var a=polygon[i];var b=polygon[j];
                if((a.z>point.y)!=(b.z>point.y)&&point.x<(b.x-a.x)*(point.y-a.z)/(b.z-a.z)+a.x)inside=!inside;
            }
            return inside;
        }
        public static bool IsClear(Vector2 point, CityOsmStreets.Data data)
        {
            if(data.buildings!=null)foreach(var building in data.buildings)if(IsInside(point,building.points))return false;
            if(data.roads!=null)foreach(var road in data.roads)
            {
                if(road.points==null)continue;
                float clearance=road.kind=="footway"?2.5f:road.kind=="pedestrian"?4:road.kind=="primary"||road.kind=="secondary"?7:5.5f;
                for(int i=1;i<road.points.Length;i++)
                {
                    Vector2 a=new Vector2(road.points[i-1].x,road.points[i-1].z),b=new Vector2(road.points[i].x,road.points[i].z),delta=b-a;
                    float t=delta.sqrMagnitude>.0001f?Mathf.Clamp01(Vector2.Dot(point-a,delta)/delta.sqrMagnitude):0;
                    if((point-a-delta*t).sqrMagnitude<clearance*clearance)return false;
                }
            }
            return true;
        }
        private static Material CreateMaterial(Material template,Color color)
        {
            Material result=new Material(template);result.color=color;result.DisableKeyword("_EMISSION");
            if(result.HasProperty("_EmissionColor"))result.SetColor("_EmissionColor",Color.black);
            return result;
        }
        private void Shape(string name,Vector3 position,Vector3 scale,Material material,PrimitiveType type=PrimitiveType.Cube)
        {
            GameObject item=GameObject.CreatePrimitive(type);item.name=name;item.transform.SetParent(root.transform,false);item.transform.localPosition=position;item.transform.localScale=scale;item.GetComponent<Renderer>().sharedMaterial=material;
            if(name=="ParkCanopy")item.GetComponent<Collider>().enabled=false;
        }
        private void Bench(Vector3 at)
        {
            Shape("ParkBenchSeat",at+Vector3.up*.48f,new Vector3(1.8f,.12f,.55f),wood);
            Shape("ParkBenchBack",at+new Vector3(0,.8f,.23f),new Vector3(1.8f,.55f,.09f),wood);
            for(int side=-1;side<=1;side+=2)Shape("ParkBenchLeg",at+new Vector3(side*.65f,.22f,0),new Vector3(.12f,.44f,.5f),metal);
        }
        private void Tree(Vector3 at)
        {
            Shape("ParkTrunk",at+Vector3.up*1.6f,new Vector3(.35f,1.6f,.35f),wood,PrimitiveType.Cylinder);
            Shape("ParkCanopy",at+Vector3.up*4,new Vector3(2.8f,3.2f,2.8f),green,PrimitiveType.Sphere);
            Shape("ParkCanopy",at+new Vector3(-1.05f,3.5f,.3f),new Vector3(2.4f,2.6f,2.3f),green,PrimitiveType.Sphere);
            Shape("ParkCanopy",at+new Vector3(.95f,3.8f,-.3f),new Vector3(2.5f,2.8f,2.4f),green,PrimitiveType.Sphere);
        }
        private void OnDestroy()
        {
            Release(root);Release(wood);Release(green);Release(metal);
        }
        private static void Release(UnityEngine.Object item){if(item==null)return;if(Application.isPlaying)Destroy(item);else DestroyImmediate(item);}
    }
}
