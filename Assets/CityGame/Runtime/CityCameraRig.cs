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
        public CityHumanActor HumanActor {get;private set;}
        public bool HumanSelected {get;private set;}
        public CityOutfit CurrentOutfit => look!=null?look.Outfit:CityOutfit.Korzo;
        private CityGameAudio feedbackAudio;
        private CityCombatRuntime combat;
        private CityProgressionController phone;
        private CityPlayerController mover;
        private bool phoneView, phoneBlocked;
        private Quaternion phoneRotation;
        private int phoneInteraction;
        public bool PhonePresentationActive => phoneView;
        public void ConfigurePhone(CityProgressionController controller) { phone = controller; }
        public void ConfigureCombat(CityCombatRuntime runtime){combat=runtime;}
        public void ConfigureFeedback(CityGameAudio audio)
        {
            feedbackAudio=audio;
            if(look!=null)look.Face.BindFeedback(owner.GetComponent<CityPlayerVitals>(),audio);
            HumanActor?.BindFeedback(owner.GetComponent<CityPlayerVitals>(),audio,view);
        }
        private readonly RaycastHit[] hits = new RaycastHit[24];
        private readonly RaycastHit[] aimHits = new RaycastHit[24];
        private Transform weapon;
        private CityBenchVisual bench;
        private CityLifestyle lifestyle;
        private CityBoxingRun boxingRun;
        private CityAppearance appearance;
        public void ConfigureAppearance(CityAppearance state){appearance=state;}
        private CityCharacterPhysique physique;
        private bool wasTraining;
        private CursorLockMode previousCursor;
        private bool previousCursorVisible;
        public void ConfigureBench(CityBenchVisual visual,CityLifestyle state,CityBoxingRun run=null)
        {bench=visual;lifestyle=state;boxingRun=run;HumanActor?.BindBoxing(state,run);}
        public CityCameraMode Mode { get; private set; }
        public bool AvatarAvailable => avatar != null;

        public void Initialize(Transform player, Transform cameraView)
        {
            owner = player; view = cameraView;
            mover = owner.GetComponent<CityPlayerController>();
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
            ApplyAvatarVisibility();
        }
        public bool SelectHumanAvatar(bool selected)
        {
            if(owner==null||(lifestyle!=null&&(lifestyle.IsTraining||lifestyle.IsBoxing)))return false;
            if(selected&&HumanActor==null)
            {
                if(!CityHumanActor.TryCreate(owner,Resources.Load<GameObject>("Models/RodjaHuman/RodjaHuman"),out CityHumanActor actor))return false;
                HumanActor=actor;
                HumanActor.BindBoxing(lifestyle,boxingRun);
                HumanActor.BindFeedback(owner.GetComponent<CityPlayerVitals>(),feedbackAudio,view);
            }
            HumanSelected=selected;
            if(selected)Mode=CityCameraMode.Chase;
            ApplyAvatarVisibility();return true;
        }
        private void ApplyAvatarVisibility()
        {
            bool training=lifestyle!=null&&lifestyle.IsTraining&&bench!=null;
            bool boxing=lifestyle!=null&&lifestyle.IsBoxing;
            bool humanBoxing=boxing&&HumanSelected&&HumanActor!=null&&HumanActor.IsBoxing;
            if(avatar!=null)avatar.SetActive(!training&&(boxing||Mode!=CityCameraMode.FirstPerson)&&(!HumanSelected||(boxing&&!humanBoxing)));
            HumanActor?.SetVisible(HumanSelected&&!training&&(humanBoxing||(!boxing&&(phoneView||Mode!=CityCameraMode.FirstPerson))));
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
            if (phone == null || !phone.IsPhoneOpen) phoneBlocked = false;
            bool requested = phone != null && phone.IsPhoneOpen && phone.PhoneWorldPreviewSupported && Time.timeScale <= 0 &&
                HumanSelected && HumanActor != null && HumanActor.PhoneAvailable && !training && !boxing &&
                !phoneBlocked && (mover == null || mover.enabled) && (combat == null || combat.UnarmedPunch <= .001f);
            if (requested && !phoneView) { phoneRotation = view.localRotation; phoneInteraction = phone.PhoneInteractionVersion; }
            if (!requested && phoneView) { HumanActor?.SetPhoneActive(false); view.localRotation = phoneRotation; }
            phoneView = requested;
            if(look!=null)look.Apply(appearance!=null?appearance.Outfit:CityOutfit.Korzo,lifestyle!=null?lifestyle.Strength:0);
            if(look!=null)look.Face.SetEffort(boxing?.8f:actorMotion!=null?Mathf.Clamp01(actorMotion.ObservedSpeed/8):0);
            if(look!=null)look.Face.SetAlert(lifestyle!=null&&lifestyle.Wanted);
            ApplyAvatarVisibility();
            HumanActor?.SetAlert(lifestyle!=null&&lifestyle.Wanted);
            HumanActor?.SetCombatPunch(combat!=null&&combat.CanAct?combat.UnarmedPunch:0);
            HumanActor?.Advance(Time.deltaTime,Time.timeScale<=0||Time.deltaTime<=0);
            if (phoneView)
            {
                if (HumanActor.SetPhoneActive(true))
                {
                    Transform human = HumanActor.Visual.transform;
                    Vector3 target = human.position + Vector3.up * 1.4f + human.forward * .05f;
                    Vector3 position = RetractCamera(target, target + human.forward * 1.1f + human.right * .8f + Vector3.up * .2f);
                    if ((position - target).sqrMagnitude >= .16f)
                    {
                        phone.SetPhoneWorldPreview(true);
                        if (phoneInteraction != phone.PhoneInteractionVersion) { phoneInteraction = phone.PhoneInteractionVersion; HumanActor.NotifyPhoneTap(); }
                        HumanActor.AdvancePhone(Time.unscaledDeltaTime);
                        Vector3 right = Vector3.Cross(Vector3.up, (target - position).normalized).normalized;
                        Vector3 lookAt = target + right * .4f;
                        float blend = 1 - Mathf.Exp(-12 * Mathf.Min(Time.unscaledDeltaTime, .05f));
                        view.position = Vector3.Lerp(view.position, position, blend);
                        if ((lookAt - view.position).sqrMagnitude > .0001f)
                            view.rotation = Quaternion.Slerp(view.rotation, Quaternion.LookRotation(lookAt - view.position, Vector3.up), blend);
                        if (weapon != null) weapon.gameObject.SetActive(false);
                        return;
                    }
                    phoneBlocked = true;
                }
                HumanActor.SetPhoneActive(false); phoneView = false; view.localRotation = phoneRotation; ApplyAvatarVisibility();
            }
            if (phone != null) phone.SetPhoneWorldPreview(false);
            if(training||boxing)
            {
                if(!wasTraining){previousCursor=Cursor.lockState;previousCursorVisible=Cursor.visible;}
                Cursor.lockState=CursorLockMode.None;Cursor.visible=!Application.isMobilePlatform;
                float blend=1-Mathf.Exp(-8*Time.deltaTime);
                Vector3 target=boxing?owner.position+Vector3.up*1.1f+owner.forward*.4f:bench.CameraTarget;
                Vector3 position=boxing?owner.TransformPoint(new Vector3(3.2f,2.05f,-1.4f)):bench.CameraPosition;
                if(boxing)
                {
                    position=RetractCamera(target,position);
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
                offset=RetractCamera(eye,eye+offset)-eye;
            }
            view.position = eye + offset;
            if (weapon == null) weapon = view.Find("VKT_HeldWeapon");
            if (weapon != null) weapon.gameObject.SetActive(Mode == CityCameraMode.FirstPerson);
        }
        private Vector3 RetractCamera(Vector3 target,Vector3 position)
        {
            Vector3 path=position-target;float length=path.magnitude;
            if(length<=.001f)return target;
            int count=Physics.SphereCastNonAlloc(target,.18f,path/length,hits,length,Physics.DefaultRaycastLayers,QueryTriggerInteraction.Ignore);
            float allowed=count==hits.Length?0:length;
            for(int i=0;i<count;i++)if(hits[i].collider!=null&&!hits[i].transform.IsChildOf(owner))allowed=Mathf.Min(allowed,Mathf.Max(0,hits[i].distance-.08f));
            return target+path*(allowed/length);
        }
        private void OnDestroy() { if(phone!=null)phone.SetPhoneWorldPreview(false);HumanActor?.Dispose();if (avatar != null) Destroy(avatar); if(look!=null)look.Dispose();else physique?.Dispose(); }
    }
}
