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
            pose.Reset();previous=owner.position;pendingTeleport=false;
            Visual.transform.rotation=Quaternion.Euler(0,owner.eulerAngles.y,0);
            if(visible)pose.StepObserved(Vector3.zero,.001f,false);
            Visual.SetActive(visible);
        }
        public void Advance(float seconds,bool paused)
        {
            if(owner==null||Visual==null)return;
            Vector3 delta=owner.position-previous;previous=owner.position;
            if(paused&&delta.sqrMagnitude>25)pendingTeleport=true;
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
            pose.StepObserved(delta,seconds,false);
            pose.ApplyGrounding(body!=null&&body.enabled&&body.isGrounded&&!pose.WasTeleported,seconds);
        }
        public void Dispose()
        {
            pose.Reset();
            if(Visual!=null){if(Application.isPlaying)UnityEngine.Object.Destroy(Visual);else UnityEngine.Object.DestroyImmediate(Visual);}
        }
    }
}
