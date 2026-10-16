using UnityEngine;
namespace Pozarevac.CityGame
{
    public sealed class CityActorMotion : MonoBehaviour
    {
        private Transform actor;private Animation animationPlayer;private Vector3 previous;private bool running;private float phase;
        private Transform leftArm,leftElbow,rightArm,rightElbow;
        public void Initialize(Transform owner,Animation player)
        {
            actor=owner;animationPlayer=player;previous=owner.position;
            const string upper="HipsCtrl/Hips/Spine/Chest/UpperChest/";
            leftArm=player.transform.Find(upper+"LeftShoulder/LeftArm");leftElbow=leftArm!=null?leftArm.Find("LeftForeArm"):null;
            rightArm=player.transform.Find(upper+"RightShoulder/RightArm");rightElbow=rightArm!=null?rightArm.Find("RightForeArm"):null;
        }
        private void LateUpdate()
        {
            if(actor==null||animationPlayer==null)return;
            Vector3 delta=actor.position-previous;previous=actor.position;
            if(Time.deltaTime>0)
            {
                delta.y=0;bool moving=delta.sqrMagnitude>Time.deltaTime*Time.deltaTime*.03f;
                if(moving!=running){running=moving;animationPlayer.CrossFade(running?"VKT_Run":"VKT_Idle",.18f);}
                if(moving){phase+=Time.deltaTime*7;transform.rotation=Quaternion.Slerp(transform.rotation,Quaternion.LookRotation(delta),1-Mathf.Exp(-8*Time.deltaTime));}
            }
            float swing=running?Mathf.Sin(phase)*.4f:0;
            RelaxArm(leftArm,leftElbow,transform,-1,swing);RelaxArm(rightArm,rightElbow,transform,1,-swing);
        }
        public static void RelaxArm(Transform arm,Transform elbow,Transform model,float side,float swing)
        {
            if(arm==null||elbow==null||model==null)return;
            Vector3 direction=elbow.position-arm.position;if(direction.sqrMagnitude<.00001f)return;
            Vector3 target=(-model.up+model.right*side*.12f+model.forward*swing).normalized;
            arm.rotation=Quaternion.FromToRotation(direction,target)*arm.rotation;
        }
    }
    public sealed class CityWorldLabel : MonoBehaviour
    {
        private Transform view;
        public void Initialize(Transform cameraView){view=cameraView;}
        private void LateUpdate(){if(view!=null)transform.rotation=view.rotation;}
    }
}
