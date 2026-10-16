using System;
using System.Collections.Generic;
using UnityEngine;

namespace Pozarevac.CityGame
{
    // Owns only its generated material and accessory mesh. Imported meshes/textures stay shared.
    public sealed class CityCharacterLook : IDisposable
    {
        private readonly CityCharacterPhysique physique;
        private readonly Material cloth, hatMaterial;
        private readonly GameObject hat;
        private readonly Mesh cap;
        private int selected=-1;
        public CityOutfit Outfit => (CityOutfit)Mathf.Max(0,selected);
        public void SetStudioFill(float value){cloth.SetFloat("_PreviewFill",Mathf.Clamp01(value));}
        public CityCharacterLook(GameObject model, CityCharacterPhysique body)
        {
            physique=body;
            Shader shader=Resources.Load<Shader>("StreetCharacter");
            if(shader==null)throw new InvalidOperationException("Street character shader missing.");
            cloth=new Material(shader);
            cloth.SetTexture("_BaseMap",Resources.Load<Texture2D>("Models/KenneyRetro/humanMaleA"));
            foreach(Renderer renderer in model.GetComponentsInChildren<Renderer>())
            {
                Material[] slots=new Material[Mathf.Max(1,renderer.sharedMaterials.Length)];
                for(int i=0;i<slots.Length;i++)slots[i]=cloth;
                renderer.sharedMaterials=slots;
            }
            if(body.Head!=null)
            {
                Bounds bounds=CityCharacterModel.Measure(model);
                cap=CreateCap();
                hatMaterial=new Material(Resources.Load<Material>("CityMissionMarker"));
                hat=new GameObject("VKT_StreetCap",typeof(MeshFilter),typeof(MeshRenderer));
                hat.GetComponent<MeshFilter>().sharedMesh=cap;hat.GetComponent<MeshRenderer>().sharedMaterial=hatMaterial;
                hat.transform.position=new Vector3(bounds.center.x,bounds.max.y-.025f,bounds.center.z);
                hat.transform.rotation=model.transform.rotation;
                hat.transform.localScale=new Vector3(.16f,.10f,.18f);
                hat.transform.SetParent(body.Head,true);
            }
            Apply(CityOutfit.Korzo,0);
        }
        public void Apply(CityOutfit outfit,int strength)
        {
            physique.Apply(strength);
            if(selected==(int)outfit)return;
            selected=(int)outfit;
            Color shirt,pants,shoes,accent;
            switch(outfit)
            {
                case CityOutfit.Midnight:
                    shirt=new Color(.085f,.09f,.11f);pants=new Color(.19f,.23f,.3f);shoes=new Color(.65f,.12f,.13f);accent=new Color(.85f,.85f,.87f);break;
                case CityOutfit.Busije:
                    shirt=new Color(.52f,.08f,.14f);pants=new Color(.54f,.48f,.37f);shoes=new Color(.12f,.13f,.15f);accent=new Color(.92f,.77f,.44f);break;
                default:
                    shirt=new Color(.07f,.39f,.42f);pants=new Color(.13f,.15f,.18f);shoes=new Color(.9f,.9f,.84f);accent=new Color(.65f,.91f,.85f);break;
            }
            cloth.SetColor("_Shirt",shirt);cloth.SetColor("_Pants",pants);cloth.SetColor("_Shoes",shoes);cloth.SetColor("_Accent",accent);
            if(hat!=null){hat.SetActive(outfit!=CityOutfit.Midnight);hatMaterial.color=outfit==CityOutfit.Busije?new Color(.13f,.12f,.14f):new Color(.07f,.16f,.18f);}
        }
        private static Mesh CreateCap()
        {
            var vertices=new List<Vector3>();var triangles=new List<int>();
            const int segments=32,rings=10;
            for(int ring=0;ring<=rings;ring++)
            {
                float t=ring*Mathf.PI*.5f/rings;
                for(int j=0;j<=segments;j++){float angle=j*Mathf.PI*2/segments;vertices.Add(new Vector3(Mathf.Cos(angle)*Mathf.Cos(t),Mathf.Sin(t),Mathf.Sin(angle)*Mathf.Cos(t)));}
            }
            for(int ring=0;ring<rings;ring++)for(int j=0;j<segments;j++)
            {int a=ring*(segments+1)+j,b=a+segments+1;triangles.Add(a);triangles.Add(b);triangles.Add(a+1);triangles.Add(a+1);triangles.Add(b);triangles.Add(b+1);}
            int center=vertices.Count;vertices.Add(new Vector3(0,.01f,.55f));
            for(int j=0;j<=segments;j++)
            {float angle=j*Mathf.PI/segments;vertices.Add(new Vector3(Mathf.Cos(angle)*1.05f,.01f-.10f*Mathf.Sin(angle),.55f+Mathf.Sin(angle)*1.05f));}
            for(int j=0;j<segments;j++){triangles.Add(center);triangles.Add(center+j+2);triangles.Add(center+j+1);}
            var mesh=new Mesh{name="VKT_OriginalCurvedStreetCap"};mesh.SetVertices(vertices);mesh.SetTriangles(triangles,0);mesh.RecalculateNormals();mesh.RecalculateBounds();return mesh;
        }
        public void Dispose(){Release(hat);Release(cap);Release(hatMaterial);Release(cloth);physique.Dispose();}
        private static void Release(UnityEngine.Object value){if(value==null)return;if(Application.isPlaying)UnityEngine.Object.Destroy(value);else UnityEngine.Object.DestroyImmediate(value);}
    }
}
