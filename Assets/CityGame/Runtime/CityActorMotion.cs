using UnityEngine;
namespace Pozarevac.CityGame
{
    public sealed class CityActorMotion : MonoBehaviour
    {
        private Transform actor;private Animation animationPlayer;private Vector3 previous;private bool running;
        private readonly CityGaitState gait=new CityGaitState();
        private Transform leftArm,leftElbow,rightArm,rightElbow;
        private Transform leftHip,leftKnee,leftFoot,rightHip,rightKnee,rightFoot;
        private readonly Transform[] poseBones=new Transform[10];
        private readonly Quaternion[] basePose=new Quaternion[10],renderedPose=new Quaternion[10];
        private float leftSide=-1,rightSide=1;
        private Transform chest,head,view;
        private bool posed,hasRenderedPose;
        private float breath,turn,previousYaw;
        private CityLifestyle boxingLife;
        private CityBoxingRun boxing;
        public bool BoxingPoseActive=>boxingLife!=null&&boxingLife.IsBoxing&&leftArm!=null&&leftElbow!=null&&rightArm!=null&&rightElbow!=null;
        public void ConfigureBoxing(CityLifestyle life,CityBoxingRun run){boxingLife=life;boxing=run;}
        public float ObservedSpeed=>gait.Speed;
        public float LocomotionWeight=>gait.Weight;
        public bool GaitReady=>leftHip!=null&&leftKnee!=null&&leftFoot!=null&&rightHip!=null&&rightKnee!=null&&rightFoot!=null;
        public float BreathingPhase=>breath;
        public void Initialize(Transform owner,Animation player)
        {
            RestorePose();gait.Reset();hasRenderedPose=false;
            actor=owner;animationPlayer=player;previous=owner.position;
            const string upper="HipsCtrl/Hips/Spine/Chest/UpperChest/";
            leftArm=player.transform.Find(upper+"LeftShoulder/LeftArm");leftElbow=leftArm!=null?leftArm.Find("LeftForeArm"):null;
            rightArm=player.transform.Find(upper+"RightShoulder/RightArm");rightElbow=rightArm!=null?rightArm.Find("RightForeArm"):null;
            if(leftArm!=null)leftSide=Mathf.Sign(transform.InverseTransformPoint(leftArm.position).x);
            if(rightArm!=null)rightSide=Mathf.Sign(transform.InverseTransformPoint(rightArm.position).x);
            chest=player.transform.Find("HipsCtrl/Hips/Spine/Chest/UpperChest");
            head=chest!=null?chest.Find("Neck/Head"):null;
            foreach(Transform bone in player.GetComponentsInChildren<Transform>())
                switch(bone.name)
                {
                    case "LeftUpLeg":leftHip=bone;break;case "LeftLeg":leftKnee=bone;break;case "LeftFoot":leftFoot=bone;break;
                    case "RightUpLeg":rightHip=bone;break;case "RightLeg":rightKnee=bone;break;case "RightFoot":rightFoot=bone;break;
                }
            poseBones[0]=leftHip;poseBones[1]=leftKnee;poseBones[2]=rightHip;poseBones[3]=rightKnee;
            poseBones[4]=leftArm;poseBones[5]=leftElbow;poseBones[6]=rightArm;poseBones[7]=rightElbow;
            poseBones[8]=chest;poseBones[9]=head;
            view=Camera.main!=null?Camera.main.transform:null;previousYaw=transform.eulerAngles.y;
        }
        private void RestorePose()
        {
            // Remove only last frame's additive pose before the legacy animation samples again.
            if(!posed)return;
            for(int i=0;i<poseBones.Length;i++)if(poseBones[i]!=null)poseBones[i].localRotation=basePose[i];
            posed=false;
        }
        private void Update(){if(Time.timeScale>0&&Time.deltaTime>0)RestorePose();}
        private void OnEnable()
        {
            if(actor==null)return;
            previous=actor.position;previousYaw=transform.eulerAngles.y;turn=0;gait.Reset();running=false;hasRenderedPose=false;
            if(animationPlayer!=null&&animationPlayer["VKT_Idle"]!=null)animationPlayer.Play("VKT_Idle");
        }
        private void OnDisable(){RestorePose();gait.Reset();running=false;hasRenderedPose=false;}
        private void LateUpdate()
        {
            if(actor==null||animationPlayer==null)return;
            Vector3 delta=actor.position-previous;previous=actor.position;
            if(Time.timeScale<=0||Time.deltaTime<=0)
            {
                // Legacy clips can sample while the clock is paused. Preserve the final displayed pose.
                if(hasRenderedPose)
                {
                    for(int i=0;i<poseBones.Length;i++)if(poseBones[i]!=null)poseBones[i].localRotation=renderedPose[i];
                    posed=true;
                }
                return;
            }
            gait.Step(delta,Time.deltaTime);
            if(gait.WasTeleported)
            {
                running=false;turn=0;transform.rotation=Quaternion.Euler(0,actor.eulerAngles.y,0);previousYaw=transform.eulerAngles.y;
                if(animationPlayer["VKT_Idle"]!=null){animationPlayer.Play("VKT_Idle");animationPlayer.Sample();}
            }
            float speed=gait.Speed;bool moving=speed>.18f;
            if(moving!=running)
            {running=moving;string clip=running?"VKT_Run":"VKT_Idle";if(animationPlayer[clip]!=null)animationPlayer.CrossFade(clip,.18f);}
            if(moving)
            {
                AnimationState run=animationPlayer["VKT_Run"];if(run!=null)run.speed=Mathf.Clamp(speed/5.5f,.55f,1.45f);
                delta.y=0;
                if(delta.sqrMagnitude>.00001f)transform.rotation=Quaternion.Slerp(transform.rotation,Quaternion.LookRotation(delta),1-Mathf.Exp(-8*Time.deltaTime));
            }
            for(int i=0;i<poseBones.Length;i++)if(poseBones[i]!=null)basePose[i]=poseBones[i].localRotation;
            RelaxArm(leftArm,leftElbow,transform,leftSide,gait.LeftArmSwing);
            RelaxArm(rightArm,rightElbow,transform,rightSide,gait.RightArmSwing);
            if(gait.Weight>.005f)
            {
                PoseLeg(leftHip,leftKnee,leftFoot,transform,gait.LeftStride,gait.LeftKneeFlex);
                PoseLeg(rightHip,rightKnee,rightFoot,transform,gait.RightStride,gait.RightKneeFlex);
            }
            if(boxingLife!=null&&boxingLife.IsBoxing)
            {
                BoxingArm(leftArm,leftElbow,transform,leftSide,boxing!=null?boxing.Punch:0);
                BoxingArm(rightArm,rightElbow,transform,rightSide,0);
            }
            breath+=Time.deltaTime*Mathf.Lerp(1.8f,3.8f,Mathf.Clamp01(speed/8));
            float yaw=transform.eulerAngles.y;
            float yawRate=Mathf.DeltaAngle(previousYaw,yaw)/Mathf.Max(.001f,Time.deltaTime);previousYaw=yaw;
            turn=Mathf.Lerp(turn,Mathf.Clamp(yawRate*.035f,-7,7),1-Mathf.Exp(-7*Time.deltaTime));
            if(chest!=null)chest.localRotation*=Quaternion.Euler(Mathf.Sin(breath)*.8f+Mathf.Clamp01(speed/8)*6,0,-turn);
            if(head!=null)
            {
                Vector3 gaze=view!=null?transform.InverseTransformDirection(view.forward):Vector3.forward;
                float glance=Mathf.Clamp(Mathf.Atan2(gaze.x,gaze.z)*Mathf.Rad2Deg,-28,28);
                head.localRotation*=Quaternion.Euler(Mathf.Sin(breath*.5f)*1.4f,glance*.45f,turn*.35f);
            }
            for(int i=0;i<poseBones.Length;i++)if(poseBones[i]!=null)renderedPose[i]=poseBones[i].localRotation;
            posed=true;hasRenderedPose=true;
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
