using UnityEngine;
namespace Pozarevac.CityGame
{
    public sealed class CityActorMotion : MonoBehaviour
    {
        private Transform actor;private Animation animationPlayer;private Vector3 previous;private bool running;private float phase;
        private Transform leftArm,leftElbow,rightArm,rightElbow;
        private Transform leftHip,leftKnee,leftFoot,rightHip,rightKnee,rightFoot;
        private readonly Transform[] gaitBones=new Transform[4];
        private readonly Quaternion[] gaitBase=new Quaternion[4];
        private float speed,leftSide=-1,rightSide=1;
        private Transform chest,head,view;
        private Quaternion chestBase,headBase;
        private bool posed;
        private float breath,turn,previousYaw;
        private CityLifestyle boxingLife;
        private CityBoxingRun boxing;
        public bool BoxingPoseActive=>boxingLife!=null&&boxingLife.IsBoxing&&leftArm!=null&&leftElbow!=null&&rightArm!=null&&rightElbow!=null;
        public void ConfigureBoxing(CityLifestyle life,CityBoxingRun run){boxingLife=life;boxing=run;}
        public float ObservedSpeed=>speed;
        public bool GaitReady=>leftHip!=null&&leftKnee!=null&&leftFoot!=null&&rightHip!=null&&rightKnee!=null&&rightFoot!=null;
        public float BreathingPhase=>breath;
        public void Initialize(Transform owner,Animation player)
        {
            actor=owner;animationPlayer=player;previous=owner.position;
            const string upper="HipsCtrl/Hips/Spine/Chest/UpperChest/";
            leftArm=player.transform.Find(upper+"LeftShoulder/LeftArm");leftElbow=leftArm!=null?leftArm.Find("LeftForeArm"):null;
            rightArm=player.transform.Find(upper+"RightShoulder/RightArm");rightElbow=rightArm!=null?rightArm.Find("RightForeArm"):null;
            if(leftArm!=null)leftSide=Mathf.Sign(transform.InverseTransformPoint(leftArm.position).x);
            if(rightArm!=null)rightSide=Mathf.Sign(transform.InverseTransformPoint(rightArm.position).x);
            chest=player.transform.Find("HipsCtrl/Hips/Spine/Chest/UpperChest");
            if(chest!=null)head=chest.Find("Neck/Head");
            foreach(Transform bone in player.GetComponentsInChildren<Transform>())
                switch(bone.name)
                {
                    case "LeftUpLeg":leftHip=bone;break;case "LeftLeg":leftKnee=bone;break;case "LeftFoot":leftFoot=bone;break;
                    case "RightUpLeg":rightHip=bone;break;case "RightLeg":rightKnee=bone;break;case "RightFoot":rightFoot=bone;break;
                }
            gaitBones[0]=leftHip;gaitBones[1]=leftKnee;gaitBones[2]=rightHip;gaitBones[3]=rightKnee;
            view=Camera.main!=null?Camera.main.transform:null;previousYaw=transform.eulerAngles.y;
        }
        private void RestorePose()
        {
            // Remove only last frame's additive pose before the legacy animation samples again.
            if(!posed)return;if(chest!=null)chest.localRotation=chestBase;if(head!=null)head.localRotation=headBase;
            for(int i=0;i<gaitBones.Length;i++)if(gaitBones[i]!=null)gaitBones[i].localRotation=gaitBase[i];
            posed=false;
        }
        private void Update()=>RestorePose();
        private void OnEnable(){if(actor!=null){previous=actor.position;speed=0;running=false;if(animationPlayer!=null)animationPlayer.Play("VKT_Idle");}}
        private void OnDisable(){RestorePose();speed=0;running=false;}
        private void LateUpdate()
        {
            if(actor==null||animationPlayer==null)return;
            Vector3 delta=actor.position-previous;previous=actor.position;
            if(Time.deltaTime<=0)return;
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
                    phase=Mathf.Repeat(phase+delta.magnitude*(Mathf.PI*2/2.4f),Mathf.PI*2);
                    if(delta.sqrMagnitude>.00001f)transform.rotation=Quaternion.Slerp(transform.rotation,Quaternion.LookRotation(delta),1-Mathf.Exp(-8*Time.deltaTime));
                }
            }
            float swing=running?Mathf.Sin(phase)*Mathf.Lerp(.2f,.48f,Mathf.Clamp01(speed/8)):0;
            RelaxArm(leftArm,leftElbow,transform,leftSide,swing);RelaxArm(rightArm,rightElbow,transform,rightSide,-swing);
            for(int i=0;i<gaitBones.Length;i++)if(gaitBones[i]!=null)gaitBase[i]=gaitBones[i].localRotation;
            if(running)
            {
                float amplitude=Mathf.Lerp(18,32,Mathf.Clamp01(speed/8));
                PoseLeg(leftHip,leftKnee,leftFoot,transform,Mathf.Sin(phase)*amplitude,6+Mathf.Max(0,-Mathf.Cos(phase))*42);
                PoseLeg(rightHip,rightKnee,rightFoot,transform,-Mathf.Sin(phase)*amplitude,6+Mathf.Max(0,Mathf.Cos(phase))*42);
            }
            if(boxingLife!=null&&boxingLife.IsBoxing)
            {
                BoxingArm(leftArm,leftElbow,transform,leftSide,boxing!=null?boxing.Punch:0);
                BoxingArm(rightArm,rightElbow,transform,rightSide,0);
            }
            breath+=Time.deltaTime*Mathf.Lerp(1.8f,3.8f,Mathf.Clamp01(speed/8));
            float yaw=transform.eulerAngles.y;
            float yawRate=delta.magnitude>5?0:Mathf.DeltaAngle(previousYaw,yaw)/Mathf.Max(.001f,Time.deltaTime);previousYaw=yaw;
            turn=Mathf.Lerp(turn,Mathf.Clamp(yawRate*.035f,-7,7),1-Mathf.Exp(-7*Time.deltaTime));
            if(chest!=null){chestBase=chest.localRotation;chest.localRotation*=Quaternion.Euler(Mathf.Sin(breath)*.8f+Mathf.Clamp01(speed/8)*6,0,-turn);}
            if(head!=null)
            {
                headBase=head.localRotation;
                Vector3 gaze=view!=null?transform.InverseTransformDirection(view.forward):Vector3.forward;
                float glance=Mathf.Clamp(Mathf.Atan2(gaze.x,gaze.z)*Mathf.Rad2Deg,-28,28);
                head.localRotation*=Quaternion.Euler(Mathf.Sin(breath*.5f)*1.4f,glance*.45f,turn*.35f);
            }
            posed=true;
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
                Vector3 bend=(-model.up*.85f+model.forward*(.15f+Mathf.Max(0,swing)*1.5f)).normalized;
                if(forearm.sqrMagnitude>.00001f)elbow.rotation=Quaternion.FromToRotation(forearm,bend)*elbow.rotation;
            }
        }
        public static void PoseLeg(Transform hip,Transform knee,Transform foot,Transform model,float stride,float flex)
        {
            if(hip==null||knee==null||foot==null||model==null)return;
            Vector3 upper=knee.position-hip.position;
            if(upper.sqrMagnitude<.00001f)return;
            stride=float.IsFinite(stride)?Mathf.Clamp(stride,-40,40):0;
            flex=float.IsFinite(flex)?Mathf.Clamp(flex,0,60):0;
            Vector3 thigh=Quaternion.AngleAxis(-stride,model.right)*(-model.up);
            hip.rotation=Quaternion.FromToRotation(upper,thigh)*hip.rotation;
            Vector3 lower=foot.position-knee.position;
            Vector3 shin=Quaternion.AngleAxis(-stride+flex,model.right)*(-model.up);
            if(lower.sqrMagnitude>.00001f)knee.rotation=Quaternion.FromToRotation(lower,shin)*knee.rotation;
        }
        private static void BoxingArm(Transform arm,Transform elbow,Transform model,float side,float punch)
        {
            if(arm==null||elbow==null)return;
            Vector3 upper=elbow.position-arm.position;
            Vector3 target=(model.forward*.55f+model.right*side*.24f-model.up*.12f).normalized;
            if(upper.sqrMagnitude>.00001f)arm.rotation=Quaternion.FromToRotation(upper,target)*arm.rotation;
            if(elbow.childCount==0)return;
            Vector3 lower=elbow.GetChild(0).position-elbow.position;
            Vector3 guard=Vector3.Lerp(model.up*.85f+model.forward*.3f,model.forward,punch).normalized;
            if(lower.sqrMagnitude>.00001f)elbow.rotation=Quaternion.FromToRotation(lower,guard)*elbow.rotation;
        }
    }
    public sealed class CityWorldLabel : MonoBehaviour
    {
        private Transform view;
        public void Initialize(Transform cameraView){view=cameraView;}
        private void LateUpdate(){if(view!=null)transform.rotation=view.rotation;}
    }
}
