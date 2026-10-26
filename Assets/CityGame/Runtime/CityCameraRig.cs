using UnityEngine;
using UnityEngine.InputSystem;

namespace Pozarevac.CityGame
{
    public enum CityCameraMode { FirstPerson, Shoulder, Chase }

    // Presentation only: the controller keeps movement, health and world scale authority.
    [DefaultExecutionOrder(100)]
    public sealed class CityCameraRig : MonoBehaviour
    {
        private Transform owner, view;
        private GameObject avatar;
        private CityCharacterLook look;
        private CityActorMotion actorMotion;
        public CityOutfit CurrentOutfit => look!=null?look.Outfit:CityOutfit.Korzo;
        public void ConfigureFeedback(CityGameAudio audio){if(look!=null)look.Face.BindFeedback(owner.GetComponent<CityPlayerVitals>(),audio);}
        private readonly RaycastHit[] hits = new RaycastHit[24];
        private readonly RaycastHit[] aimHits = new RaycastHit[24];
        private Transform weapon;
        private CityBenchVisual bench;
        private CityLifestyle lifestyle;
        private CityAppearance appearance;
        public void ConfigureAppearance(CityAppearance state){appearance=state;}
        private CityCharacterPhysique physique;
        private bool wasTraining;
        private CursorLockMode previousCursor;
        private bool previousCursorVisible;
        public void ConfigureBench(CityBenchVisual visual,CityLifestyle state){bench=visual;lifestyle=state;}
        public CityCameraMode Mode { get; private set; }
        public bool AvatarAvailable => avatar != null;

        public void Initialize(Transform player, Transform cameraView)
        {
            owner = player; view = cameraView;
            GameObject source = CityCharacterModel.LoadSource();
            Texture2D texture = Resources.Load<Texture2D>("Models/KenneyRetro/humanMaleA");
            if (source != null && texture != null)
            {
                avatar = new GameObject("VKT_PlayerVisual"); avatar.transform.SetParent(owner, false);
                Transform wrapper = new GameObject("NormalizedCharacter").transform; wrapper.SetParent(avatar.transform, false);
                GameObject model = Instantiate(source, wrapper);
                physique = new CityCharacterPhysique(model);
                AnimationClip idle = Clip("idle"), run = Clip("run");
                if (!CityCharacterModel.Normalize(wrapper, model, idle, physique)) { physique.Dispose();physique=null;Destroy(avatar); avatar = null; }
                else
                {
                    look = new CityCharacterLook(model,physique);
                    if (idle != null && run != null)
                    {
                        GameObject root = CityCharacterModel.AnimationRoot(model);
                        Animation motion = root.GetComponent<Animation>(); if (motion == null) motion = root.AddComponent<Animation>();
                        motion.enabled = true; motion.cullingType = AnimationCullingType.AlwaysAnimate;
                        motion.AddClip(idle, "VKT_Idle"); motion.AddClip(run, "VKT_Run"); motion.wrapMode = WrapMode.Loop; motion.Play("VKT_Idle");
                        actorMotion=wrapper.gameObject.AddComponent<CityActorMotion>();actorMotion.Initialize(owner, motion, true);
                    }
                }
            }
            SetMode(CityCameraMode.FirstPerson);
        }

        private static AnimationClip Clip(string name)
        {
            foreach (AnimationClip clip in Resources.LoadAll<AnimationClip>("Models/KenneyRetro/" + name))
                if (!clip.name.StartsWith("__preview__", System.StringComparison.Ordinal)) return clip;
            return null;
        }
        public void Cycle() { if ((lifestyle==null||(!lifestyle.IsTraining&&!lifestyle.IsBoxing)) && Time.timeScale > 0) SetMode((CityCameraMode)(((int)Mode + 1) % 3)); }
        public void SetMode(CityCameraMode mode)
        {
            if ((int)mode < 0 || (int)mode > 2) throw new System.ArgumentOutOfRangeException(nameof(mode));
            Mode = mode;
            if (avatar != null) avatar.SetActive(mode != CityCameraMode.FirstPerson);
        }
        public static Vector3 Offset(CityCameraMode mode) => mode == CityCameraMode.FirstPerson ? Vector3.zero :
            mode == CityCameraMode.Shoulder ? new Vector3(.65f, .25f, -2.6f) : new Vector3(0, 1.1f, -5f);

        public Ray AimRay
        {
            get
            {
                Vector3 eye = owner.TransformPoint(new Vector3(0, 1.62f, 0));
                if (Mode == CityCameraMode.FirstPerson) return new Ray(eye, view.forward);
                Vector3 target = view.position + view.forward * 50f;
                int count = Physics.RaycastNonAlloc(view.position, view.forward, aimHits, 50f, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore);
                float nearest = 50f;
                if (count < aimHits.Length)
                    for (int i = 0; i < count; i++)
                        if (aimHits[i].collider != null && !aimHits[i].collider.transform.IsChildOf(owner) && aimHits[i].distance < nearest)
                        { nearest = aimHits[i].distance; target = aimHits[i].point; }
                Vector3 direction = target - eye;
                return new Ray(eye, Vector3.Dot(direction, view.forward) > 0 ? direction.normalized : view.forward);
            }
        }
        private void Update()
        {
            if (Time.timeScale > 0 && Cursor.lockState == CursorLockMode.Locked && Keyboard.current != null && Keyboard.current.cKey.wasPressedThisFrame) Cycle();
        }
        private void LateUpdate()
        {
            if (owner == null || view == null) return;
            if(weapon==null)weapon=view.Find("VKT_HeldWeapon");
            bool training=lifestyle!=null&&lifestyle.IsTraining&&bench!=null;
            bool boxing=lifestyle!=null&&lifestyle.IsBoxing;
            if(look!=null)look.Apply(appearance!=null?appearance.Outfit:CityOutfit.Korzo,lifestyle!=null?lifestyle.Strength:0);
            if(look!=null)look.Face.SetEffort(boxing?.8f:actorMotion!=null?Mathf.Clamp01(actorMotion.ObservedSpeed/8):0);
            if(look!=null)look.Face.SetAlert(lifestyle!=null&&lifestyle.Wanted);
            if(avatar!=null)avatar.SetActive(!training&&(boxing||Mode!=CityCameraMode.FirstPerson));
            if(training||boxing)
            {
                if(!wasTraining){previousCursor=Cursor.lockState;previousCursorVisible=Cursor.visible;}
                Cursor.lockState=CursorLockMode.None;Cursor.visible=!Application.isMobilePlatform;
                float blend=1-Mathf.Exp(-8*Time.deltaTime);
                Vector3 target=boxing?owner.position+Vector3.up*1.1f+owner.forward*.4f:bench.CameraTarget;
                Vector3 position=boxing?owner.TransformPoint(new Vector3(3.2f,2.05f,-1.4f)):bench.CameraPosition;
                if(boxing)
                {
                    Vector3 path=position-target;float length=path.magnitude;
                    int count=Physics.SphereCastNonAlloc(target,.18f,path.normalized,hits,length,Physics.DefaultRaycastLayers,QueryTriggerInteraction.Ignore);
                    float allowed=count==hits.Length?0:length;
                    for(int i=0;i<count;i++)if(hits[i].collider!=null&&!hits[i].transform.IsChildOf(owner))allowed=Mathf.Min(allowed,Mathf.Max(0,hits[i].distance-.08f));
                    if(length>.001f)position=target+path*(allowed/length);
                }
                view.position=Vector3.Lerp(view.position,position,blend);
                if((target-view.position).sqrMagnitude>.001f)view.rotation=Quaternion.Slerp(view.rotation,Quaternion.LookRotation(target-view.position,Vector3.up),blend);
                if(weapon!=null)weapon.gameObject.SetActive(false);
                wasTraining=true;
                return;
            }
            if(wasTraining){wasTraining=false;owner.GetComponent<CityPlayerController>().ResetView();Cursor.lockState=previousCursor;Cursor.visible=previousCursorVisible;}
            Vector3 eye = owner.TransformPoint(new Vector3(0, 1.62f, 0));
            Vector3 offset = view.rotation * Offset(Mode);
            float distance = offset.magnitude;
            if (distance > .001f)
            {
                int count = Physics.SphereCastNonAlloc(eye, .18f, offset / distance, hits, distance, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore);
                float allowed = count == hits.Length ? 0 : distance;
                for (int i = 0; i < count; i++)
                    if (hits[i].collider != null && !hits[i].collider.transform.IsChildOf(owner)) allowed = Mathf.Min(allowed, Mathf.Max(0, hits[i].distance - .08f));
                offset *= allowed / distance;
            }
            view.position = eye + offset;
            if (weapon == null) weapon = view.Find("VKT_HeldWeapon");
            if (weapon != null) weapon.gameObject.SetActive(Mode == CityCameraMode.FirstPerson);
        }
        private void OnDestroy() { if (avatar != null) Destroy(avatar); if(look!=null)look.Dispose();else physique?.Dispose(); }
    }
}
