using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace Pozarevac.CityGame
{
    public sealed class CityVisualPolish : MonoBehaviour
    {
        private readonly List<UnityEngine.Object> owned=new List<UnityEngine.Object>();
        private Mesh rounded,sphere,capsule,cylinder,pot,rod;
        private Material cloth,skin,dark,fur,soil;
        private GameObject models;
        private readonly List<Renderer> hidden=new List<Renderer>();
        private readonly List<(MeshFilter filter,Mesh original)> replaced=new List<(MeshFilter,Mesh)>();
        public int RoundedProps {get;private set;}
        public int SculptedActors {get;private set;}
        public int ImportedActors {get;private set;}
        public void Initialize(CityGameBootstrap game)
        {
            if(models!=null)throw new InvalidOperationException("Visual pass already initialized.");
            rounded=Keep(CityVisualMeshes.RoundedBox());
            rod=Keep(CityVisualMeshes.Lathe(new[]{new Vector2(0,-.5f),new Vector2(.42f,-.49f),new Vector2(.5f,-.45f),new Vector2(.5f,.45f),new Vector2(.42f,.49f),new Vector2(0,.5f)}));
            pot=Keep(CityVisualMeshes.Lathe(new[]{new Vector2(.35f,-.5f),new Vector2(.39f,-.47f),new Vector2(.49f,.32f),new Vector2(.54f,.35f),new Vector2(.54f,.48f),new Vector2(.46f,.48f),new Vector2(.44f,.32f),new Vector2(.32f,-.34f),new Vector2(0,-.34f)}));
            sphere=Builtin(PrimitiveType.Sphere);capsule=Builtin(PrimitiveType.Capsule);cylinder=Builtin(PrimitiveType.Cylinder);
            models=new GameObject("VKT_SculptedVisuals");models.transform.SetParent(transform,false);
            Material template=Resources.Load<Material>("CityMissionMarker");
            cloth=Material(template,new Color(.09f,.15f,.18f));skin=Material(template,new Color(.55f,.35f,.23f));dark=Material(template,new Color(.025f,.035f,.04f));fur=Material(template,new Color(.29f,.19f,.10f));soil=Material(template,new Color(.075f,.055f,.035f));
            foreach(MeshFilter filter in FindObjectsByType<MeshFilter>(FindObjectsInactive.Include,FindObjectsSortMode.None))
            {
                string name=filter.name;
                if(name=="HeldStick")
                {replaced.Add((filter,filter.sharedMesh));filter.sharedMesh=rod;RoundedProps++;continue;}
                if(name.StartsWith("Saksija_"))
                {
                    replaced.Add((filter,filter.sharedMesh));filter.sharedMesh=pot;RoundedProps++;
                    Keep(Part("PlanterSoil",filter.transform,cylinder,new Vector3(0,.455f,0),new Vector3(.83f,.025f,.83f),soil).gameObject);
                }
                else if(filter.sharedMesh!=null&&filter.sharedMesh.name=="Cube"&&
                    (name.StartsWith("ParkBench")||name.StartsWith("Held")||name.StartsWith("WindowFrame")||name.StartsWith("StickPickup")||name=="KatalogSemena"||name=="OtkupBuda"))
                {replaced.Add((filter,filter.sharedMesh));filter.sharedMesh=rounded;RoundedProps++;}
            }
            foreach(CityHazard actor in FindObjectsByType<CityHazard>(FindObjectsSortMode.None))Sculpt(actor);
            if(game.Hideout!=null&&game.Hideout.Entrance!=null)DressEntrance(game.Hideout.Entrance);
            foreach(TextMesh label in FindObjectsByType<TextMesh>(FindObjectsInactive.Include,FindObjectsSortMode.None))
            {
                if(label.name!="Upozorenje"&&label.name!="EquipmentSign")continue;
                label.characterSize=label.name=="Upozorenje"?.018f:.025f;
                label.gameObject.AddComponent<CityWorldLabel>().Initialize(game.Player.GetComponentInChildren<Camera>().transform);
            }
            foreach(Light light in FindObjectsByType<Light>(FindObjectsSortMode.None))if(light.type==LightType.Directional){light.shadows=LightShadows.Soft;light.shadowStrength=.72f;light.shadowBias=.04f;}
            Camera camera=game.Player.GetComponentInChildren<Camera>();camera.allowHDR=true;
            UniversalAdditionalCameraData extra=camera.GetUniversalAdditionalCameraData();extra.renderPostProcessing=true;
            Volume volume=gameObject.AddComponent<Volume>();volume.isGlobal=true;volume.priority=5;
            VolumeProfile profile=Keep(ScriptableObject.CreateInstance<VolumeProfile>());volume.sharedProfile=profile;
            Tonemapping tone=Keep(profile.Add<Tonemapping>());tone.mode.Override(TonemappingMode.ACES);
            ColorAdjustments color=Keep(profile.Add<ColorAdjustments>());color.contrast.Override(12);color.saturation.Override(-7);color.postExposure.Override(.2f);
            Bloom bloom=Keep(profile.Add<Bloom>());bloom.intensity.Override(.12f);bloom.threshold.Override(1.15f);
            Vignette vignette=Keep(profile.Add<Vignette>());vignette.intensity.Override(.16f);vignette.smoothness.Override(.4f);
        }
        private T Keep<T>(T value)where T:UnityEngine.Object{owned.Add(value);return value;}
        private void DressEntrance(Transform entrance)
        {
            Renderer original=entrance.GetComponent<Renderer>();if(original!=null){hidden.Add(original);original.enabled=false;}
            Transform root=Keep(new GameObject("VKT_DetailedEntrance")).transform;root.SetParent(entrance,false);
            Vector3 scale=entrance.localScale;root.localScale=new Vector3(1/scale.x,1/scale.y,1/scale.z);
            Material door=Material(Resources.Load<Material>("CityMissionMarker"),new Color(.10f,.23f,.22f));
            Material stone=Material(Resources.Load<Material>("CityMissionMarker"),new Color(.52f,.49f,.42f));
            Part("DoorPanel",root,rounded,new Vector3(0,0,-.20f),new Vector3(1.3f,2.15f,.14f),door);
            for(int side=-1;side<=1;side+=2)Part("DoorFrame",root,rounded,new Vector3(side*.73f,0,-.17f),new Vector3(.14f,2.35f,.25f),stone);
            Part("DoorLintel",root,rounded,new Vector3(0,1.13f,-.17f),new Vector3(1.6f,.17f,.27f),stone);
            Part("DoorCanopy",root,rounded,new Vector3(0,1.35f,-.32f),new Vector3(1.8f,.18f,.75f),dark);
            for(int row=-1;row<=1;row++)Part("DoorInset",root,rounded,new Vector3(0,row*.59f,-.30f),new Vector3(1.02f,.46f,.06f),cloth);
            Part("DoorHandle",root,sphere,new Vector3(.47f,-.08f,-.37f),new Vector3(.09f,.09f,.09f),stone);
            Part("DoorStep",root,rounded,new Vector3(0,-1.1f,-.30f),new Vector3(1.8f,.14f,.8f),stone);
            TextMesh sign=entrance.GetComponentInChildren<TextMesh>();
            if(sign!=null){sign.characterSize=.017f;sign.transform.localPosition=new Vector3(0,.64f,-1.5f);sign.text="ŠTEK / BAŠTA";sign.gameObject.AddComponent<CityWorldLabel>().Initialize(Camera.main.transform);}
            RoundedProps++;
        }
        private Material Material(Material template,Color color)
        {
            Material material=Keep(new Material(template));material.color=color;material.DisableKeyword("_EMISSION");
            if(material.HasProperty("_EmissionColor"))material.SetColor("_EmissionColor",Color.black);
            if(material.HasProperty("_Smoothness"))material.SetFloat("_Smoothness",.25f);return material;
        }
        private static Mesh Builtin(PrimitiveType type)
        {
            GameObject temporary=GameObject.CreatePrimitive(type);temporary.SetActive(false);Mesh mesh=temporary.GetComponent<MeshFilter>().sharedMesh;
            if(Application.isPlaying)Destroy(temporary);else DestroyImmediate(temporary);return mesh;
        }
        private static Transform Part(string name,Transform parent,Mesh mesh,Vector3 at,Vector3 scale,Material material)
        {
            GameObject part=new GameObject(name,typeof(MeshFilter),typeof(MeshRenderer));part.transform.SetParent(parent,false);part.transform.localPosition=at;part.transform.localScale=scale;
            part.GetComponent<MeshFilter>().sharedMesh=mesh;part.GetComponent<MeshRenderer>().sharedMaterial=material;return part.transform;
        }
        private void Sculpt(CityHazard actor)
        {
            MeshRenderer original=actor.GetComponentInChildren<MeshRenderer>();if(original==null)return;
            original.enabled=false;hidden.Add(original);
            Transform root=Keep(new GameObject("SculptedActor")).transform;root.SetParent(actor.transform,false);
            if(actor.Kind==CityHazardKind.HostileAdult)
            {
                if(TryImportedActor(root)){SculptedActors++;return;}
                Part("Hoodie",root,capsule,new Vector3(0,1.13f,0),new Vector3(.49f,.36f,.31f),cloth);
                Part("Head",root,sphere,new Vector3(0,1.65f,.01f),new Vector3(.24f,.29f,.25f),skin);
                Part("Hood",root,sphere,new Vector3(0,1.67f,-.075f),new Vector3(.30f,.32f,.23f),cloth);
                Part("Nose",root,sphere,new Vector3(0,1.64f,.15f),Vector3.one*.055f,skin);
                for(int side=-1;side<=1;side+=2)
                {
                    Part("Sleeve",root,capsule,new Vector3(side*.30f,1.04f,0),new Vector3(.14f,.31f,.14f),cloth).localRotation=Quaternion.Euler(0,0,side*10);
                    Part("Hand",root,sphere,new Vector3(side*.34f,.72f,.01f),new Vector3(.12f,.16f,.12f),skin);
                    Part("Jeans",root,capsule,new Vector3(side*.135f,.43f,0),new Vector3(.18f,.35f,.20f),dark);
                    Part("Shoe",root,rounded,new Vector3(side*.135f,.08f,.065f),new Vector3(.20f,.16f,.36f),dark);
                    Part("Eye",root,sphere,new Vector3(side*.052f,1.69f,.119f),Vector3.one*.025f,dark);
                }
            }
            else
            {
                Part("DogBody",root,capsule,new Vector3(0,.48f,0),new Vector3(.4f,.45f,.42f),fur).localRotation=Quaternion.Euler(90,0,0);
                Part("DogHead",root,sphere,new Vector3(0,.77f,.43f),new Vector3(.34f,.38f,.37f),fur);
                Part("Muzzle",root,capsule,new Vector3(0,.70f,.65f),new Vector3(.24f,.16f,.23f),fur).localRotation=Quaternion.Euler(90,0,0);
                Part("Nose",root,sphere,new Vector3(0,.70f,.79f),Vector3.one*.12f,dark);
                for(int side=-1;side<=1;side+=2)
                {
                    Part("Ear",root,capsule,new Vector3(side*.12f,.98f,.36f),new Vector3(.11f,.14f,.10f),dark);
                    Part("Eye",root,sphere,new Vector3(side*.11f,.83f,.56f),Vector3.one*.045f,dark);
                    for(int leg=-1;leg<=1;leg+=2)Part("DogLeg",root,capsule,new Vector3(side*.14f,.22f,leg*.29f),new Vector3(.12f,.20f,.13f),fur);
                }
                Part("Tail",root,capsule,new Vector3(0,.59f,-.57f),new Vector3(.1f,.24f,.1f),fur).localRotation=Quaternion.Euler(45,0,0);
            }
            SculptedActors++;
        }
        private bool TryImportedActor(Transform parent)
        {
            GameObject asset=CityCharacterModel.LoadSource();
            Texture2D texture=Resources.Load<Texture2D>("Models/KenneyRetro/humanMaleA");
            if(asset==null||texture==null)return false;
            Transform wrapper=new GameObject("NormalizedCharacter").transform;wrapper.SetParent(parent,false);
            GameObject instance=Instantiate(asset,wrapper,false);instance.name="Kenney_CC0_RetroCharacter";
            Renderer[] renderers=instance.GetComponentsInChildren<Renderer>();if(renderers.Length==0){Destroy(instance);return false;}
            AnimationClip idle=Clip("idle"),run=Clip("run");
            if(!CityCharacterModel.Normalize(wrapper,instance,idle)){Destroy(wrapper.gameObject);return false;}
            Material material=Material(Resources.Load<Material>("CityMissionMarker"),Color.white);material.mainTexture=texture;if(material.HasProperty("_BaseMap"))material.SetTexture("_BaseMap",texture);
            foreach(Renderer renderer in renderers){Material[] materials=new Material[Mathf.Max(1,renderer.sharedMaterials.Length)];for(int i=0;i<materials.Length;i++)materials[i]=material;renderer.sharedMaterials=materials;}
            if(idle!=null&&run!=null)
            {
                GameObject rig=CityCharacterModel.AnimationRoot(instance);
                Animation player=rig.GetComponent<Animation>();if(player==null)player=rig.AddComponent<Animation>();player.enabled=true;player.cullingType=AnimationCullingType.AlwaysAnimate;
                player.AddClip(idle,"VKT_Idle");player.AddClip(run,"VKT_Run");player.wrapMode=WrapMode.Loop;player.Play("VKT_Idle");
                wrapper.gameObject.AddComponent<CityActorMotion>().Initialize(parent.parent,player);
            }
            ImportedActors++;return true;
        }
        private static AnimationClip Clip(string name)
        {
            foreach(AnimationClip clip in Resources.LoadAll<AnimationClip>("Models/KenneyRetro/"+name))if(!clip.name.StartsWith("__preview__",StringComparison.Ordinal))return clip;
            return null;
        }
        private void OnDestroy()
        {
            foreach(var item in replaced)if(item.filter!=null)item.filter.sharedMesh=item.original;
            foreach(Renderer renderer in hidden)if(renderer!=null)renderer.enabled=true;
            foreach(UnityEngine.Object item in owned)if(item!=null){if(Application.isPlaying)Destroy(item);else DestroyImmediate(item);}
        }
    }
}
