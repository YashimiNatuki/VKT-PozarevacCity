using System;
using UnityEngine;

namespace Pozarevac.CityGame
{
    // Observes collision-resolved movement; never owns input, physics, rewards or saves.
    public sealed class CityHumanActor : IDisposable
    {
        private readonly Transform owner;
        private readonly CharacterController body;
        private readonly CityHumanPose pose;
        private Vector3 previous;
        private bool pendingTeleport;
        private readonly CityFaceState expression=new CityFaceState();
        private CityPlayerVitals vitals;
        private CityGameAudio audio;
        private Transform view;
        private bool alert;
        private CityHumanPhonePose phone;
        private bool phoneUnavailable;
        private CityLifestyle boxingLife;
        private CityBoxingRun boxingRun;
        private float combatPunch;
        public bool BoxingReady => pose.HandsReady;
        public bool IsBoxing => BoxingReady && boxingLife != null && boxingLife.IsBoxing && boxingRun != null;
        public void BindBoxing(CityLifestyle life, CityBoxingRun run) { boxingLife=life;boxingRun=run; }
        public bool PhoneAvailable
        {
            get
            {
                if (owner == null || phoneUnavailable || !pose.HandsReady || pose.GuardWeight >= .01f) return false;
                Vector3 scale = owner.lossyScale;
                return float.IsFinite(scale.x) && float.IsFinite(scale.y) && float.IsFinite(scale.z) &&
                    scale.x > .0001f && scale.y > .0001f && scale.z > .0001f;
            }
        }
        public bool PhoneActive => phone != null && phone.Active;
        public GameObject PhoneVisual => phone?.Visual;
        public float PhoneWeight => phone != null ? phone.Weight : 0;
        public float PhoneGripDistance => phone != null ? phone.GripDistance : float.PositiveInfinity;
        public float PhoneTapDistance => phone != null ? phone.TapDistance : float.PositiveInfinity;
        public bool SetPhoneActive(bool active)
        {
            if (active && (!IsVisible || !PhoneAvailable)) { phone?.SetActive(false); return false; }
            if (active && phone == null) { phone = pose.CreatePhone(owner); phoneUnavailable = phone == null; }
            phone?.SetActive(active); return PhoneActive;
        }
        public void AdvancePhone(float unscaledSeconds) { if (IsVisible) phone?.Step(unscaledSeconds); }
        public void NotifyPhoneTap() => phone?.NotifyTap();
        public CityHumanFace Face=>pose.Face;
        public void BindFeedback(CityPlayerVitals health,CityGameAudio feedback,Transform cameraView)
        {
            Unsubscribe();expression.Clear();vitals=health;audio=feedback;view=cameraView;
            if(vitals!=null){vitals.Damaged+=OnDamage;vitals.Respawned+=ClearExpression;}
            if(audio!=null)audio.CueRequested+=OnCue;
        }
        public void SetAlert(bool value)=>alert=value;
        public void SetCombatPunch(float value)=>combatPunch=float.IsFinite(value)?Mathf.Clamp01(value):0;
        private void OnDamage(float amount)=>expression.Damage(amount);
        private void ClearExpression()=>expression.Clear();
        private void OnCue(CityAudioCue cue){if(cue==CityAudioCue.Success)expression.Celebrate();else expression.Focus();}
        private void Unsubscribe()
        {
            if(vitals!=null){vitals.Damaged-=OnDamage;vitals.Respawned-=ClearExpression;}
            if(audio!=null)audio.CueRequested-=OnCue;
        }
        public GameObject Visual {get;}
        public bool IsVisible=>Visual!=null&&Visual.activeSelf;
        public float ObservedSpeed=>pose.ObservedSpeed;
        public float Phase=>pose.Phase;
        public float Weight=>pose.Weight;
        public int SupportedFeet=>pose.SupportedFeet;
        public float GroundingOffset=>pose.GroundingOffset;
        private CityHumanActor(Transform player,GameObject visual,CityHumanPose adapter)
        {
            owner=player;Visual=visual;pose=adapter;previous=owner.position;body=owner.GetComponent<CharacterController>();
            if(body!=null)pose.ConfigureGrounding(owner);
        }
        public static bool TryCreate(Transform owner,GameObject source,out CityHumanActor actor)
        {
            actor=null;
            if(!CityCharacterModel.TryPrepareHuman(source,owner,0,out GameObject visual))return false;
            if(!CityHumanPose.TryCreate(visual,visual.transform,out CityHumanPose pose))
            {if(Application.isPlaying)UnityEngine.Object.Destroy(visual);else UnityEngine.Object.DestroyImmediate(visual);return false;}
            visual.name="VKT_HumanPlayer";actor=new CityHumanActor(owner,visual,pose);return true;
        }
        public void SetVisible(bool visible)
        {
            if(Visual==null||Visual.activeSelf==visible)return;
            SetPhoneActive(false);
            pose.Reset();previous=owner.position;pendingTeleport=false;
            Visual.transform.rotation=Quaternion.Euler(0,owner.eulerAngles.y,0);
            if(visible)pose.StepObserved(Vector3.zero,.001f,false);
            Visual.SetActive(visible);
        }
        public void Advance(float seconds,bool paused)
        {
            if(owner==null||Visual==null)return;
            if(!paused&&PhoneActive)SetPhoneActive(false);
            Vector3 delta=owner.position-previous;previous=owner.position;
            if(paused&&delta.sqrMagnitude>25)pendingTeleport=true;
            if(!paused&&float.IsFinite(seconds)&&seconds>0)expression.Tick(seconds);
            if(paused||!IsVisible||!float.IsFinite(seconds)||seconds<=0)return;
            if(pendingTeleport)
            {
                pose.Reset();delta=Vector3.zero;pendingTeleport=false;
                Visual.transform.rotation=Quaternion.Euler(0,owner.eulerAngles.y,0);
            }
            Vector3 flat=new Vector3(delta.x,0,delta.z);
            if(delta.sqrMagnitude>25)Visual.transform.rotation=Quaternion.Euler(0,owner.eulerAngles.y,0);
            else if(flat.sqrMagnitude>.00001f)
                Visual.transform.rotation=Quaternion.Slerp(Visual.transform.rotation,Quaternion.LookRotation(flat),1-Mathf.Exp(-8*seconds));
            bool boxing=IsBoxing;
            if(boxing)Visual.transform.rotation=Quaternion.Euler(0,owner.eulerAngles.y,0);
            pose.SetCombatPunch(boxing?boxingRun.Punch:combatPunch);
            pose.StepObserved(delta,seconds,boxing);
            pose.ApplyGrounding(body!=null&&body.enabled&&body.isGrounded&&!pose.WasTeleported,seconds);
            float effort=boxing?.8f:Mathf.Clamp01(pose.ObservedSpeed/8);
            Face?.Step(seconds,view!=null?view.forward:Visual.transform.forward,expression.Resolve(effort,alert),effort);
        }
        public void Dispose()
        {
            Unsubscribe();expression.Clear();
            phone?.Dispose();phone=null;
            pose.Reset();
            if(Visual!=null){if(Application.isPlaying)UnityEngine.Object.Destroy(Visual);else UnityEngine.Object.DestroyImmediate(Visual);}
        }
    }
}
