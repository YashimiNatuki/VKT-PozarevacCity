using UnityEngine;
namespace Pozarevac.CityGame
{
    public sealed class CityActorMotion : MonoBehaviour
    {
        private Transform actor;private Animation animationPlayer;private Vector3 previous;private bool running;private float phase;
        private Transform leftArm,leftElbow,rightArm,rightElbow;
        private float speed,leftSide=-1,rightSide=1;
        public void Initialize(Transform owner,Animation player)
        {
            actor=owner;animationPlayer=player;previous=owner.position;
            const string upper="HipsCtrl/Hips/Spine/Chest/UpperChest/";
            leftArm=player.transform.Find(upper+"LeftShoulder/LeftArm");leftElbow=leftArm!=null?leftArm.Find("LeftForeArm"):null;
            rightArm=player.transform.Find(upper+"RightShoulder/RightArm");rightElbow=rightArm!=null?rightArm.Find("RightForeArm"):null;
            if(leftArm!=null)leftSide=Mathf.Sign(transform.InverseTransformPoint(leftArm.position).x);
            if(rightArm!=null)rightSide=Mathf.Sign(transform.InverseTransformPoint(rightArm.position).x);
        }
        private void LateUpdate()
        {
            if(actor==null||animationPlayer==null)return;
            Vector3 delta=actor.position-previous;previous=actor.position;
            if(Time.deltaTime>0)
            {
                delta.y=0;
                float measured=delta.magnitude>5?0:delta.magnitude/Mathf.Max(.001f,Time.deltaTime);
                speed=Mathf.Lerp(speed,measured,1-Mathf.Exp(-10*Time.deltaTime));
                bool moving=speed>.18f;
                if(moving!=running){running=moving;animationPlayer.CrossFade(running?"VKT_Run":"VKT_Idle",.18f);}
                if(moving)
                {
                    AnimationState run=animationPlayer["VKT_Run"];if(run!=null)run.speed=Mathf.Clamp(speed/5.5f,.55f,1.45f);
                    phase+=Time.deltaTime*Mathf.Lerp(4,9,Mathf.Clamp01(speed/8));
                    if(delta.sqrMagnitude>.00001f)transform.rotation=Quaternion.Slerp(transform.rotation,Quaternion.LookRotation(delta),1-Mathf.Exp(-8*Time.deltaTime));
                }
            }
            float swing=running?Mathf.Sin(phase)*Mathf.Lerp(.2f,.48f,Mathf.Clamp01(speed/8)):0;
            RelaxArm(leftArm,leftElbow,transform,leftSide,swing);RelaxArm(rightArm,rightElbow,transform,rightSide,-swing);
        }
        public static void RelaxArm(Transform arm,Transform elbow,Transform model,float side,float swing)
        {
            if(arm==null||elbow==null||model==null)return;
            Vector3 direction=elbow.position-arm.position;if(direction.sqrMagnitude<.00001f)return;
            Vector3 target=(-model.up+model.right*side*.12f+model.forward*swing).normalized;
            arm.rotation=Quaternion.FromToRotation(direction,target)*arm.rotation;
            if(elbow.childCount>0)
            {
                Transform hand=elbow.GetChild(0);
                Vector3 forearm=hand.position-elbow.position;
                Vector3 bend=(-model.up*.85f+model.forward*(.15f+Mathf.Abs(swing)*1.5f)).normalized;
                if(forearm.sqrMagnitude>.00001f)elbow.rotation=Quaternion.FromToRotation(forearm,bend)*elbow.rotation;
            }
        }
    }
    public sealed class CityWorldLabel : MonoBehaviour
    {
        private Transform view;
        public void Initialize(Transform cameraView){view=cameraView;}
        private void LateUpdate(){if(view!=null)transform.rotation=view.rotation;}
    }
}
