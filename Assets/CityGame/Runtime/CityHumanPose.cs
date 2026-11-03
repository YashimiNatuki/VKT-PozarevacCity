using UnityEngine;

namespace Pozarevac.CityGame
{
    // MPFB presentation adapter. The existing gait and limb solvers retain pose rules.
    // This class never moves the controller, raycasts, advances training or writes a profile.
    public sealed class CityHumanPose
    {
        private static readonly string[] Required = { "pelvis", "spine_03", "head",
            "upperarm_l", "lowerarm_l", "hand_l", "upperarm_r", "lowerarm_r", "hand_r",
            "thigh_l", "calf_l", "foot_l", "thigh_r", "calf_r", "foot_r" };
        private readonly Transform frame;
        private readonly Transform[] bones;
        private readonly Quaternion[] rest;
        private readonly Quaternion leftFootFrame, rightFootFrame;
        private readonly CityGaitState gait = new CityGaitState();
        private readonly float leftSide, rightSide;
        private float seconds;
        private CityHumanHandPose leftHand, rightHand;
        private float guardWeight, combatPunch;
        public bool HandsReady => leftHand != null && rightHand != null;
        internal CityHumanBenchPose CreateBench(Transform stage,GameObject visual) => !HandsReady?null:
            new CityHumanBenchPose(stage,visual,bones,leftHand,rightHand);
        public float GuardWeight => guardWeight;
        internal CityHumanPhonePose CreatePhone(Transform owner) => !HandsReady ? null : CityHumanPhonePose.Create(owner, frame,
            new[] { bones[3], bones[4], bones[5], bones[6], bones[7], bones[8] }, leftHand, rightHand);
        public void SetCombatPunch(float value) { combatPunch = float.IsFinite(value) ? Mathf.Clamp01(value) : 0; }
        public CityHumanFace Face {get;private set;}
        public float Phase => gait.Phase;
        public float ObservedSpeed => gait.Speed;
        public float Weight => gait.Weight;
        public bool WasTeleported => gait.WasTeleported;
        private CityFootContact leftContact,rightContact;
        private CityGroundingOffset grounding;
        public int SupportedFeet {get;private set;}
        public float GroundingOffset => grounding!=null?grounding.Offset:0;

        public void ConfigureGrounding(Transform owner)
        {
            grounding=new CityGroundingOffset(owner,frame);
            leftContact=new CityFootContact(owner,frame,bones[9],bones[10],bones[11]);
            rightContact=new CityFootContact(owner,frame,bones[12],bones[13],bones[14]);
        }

        public void ApplyGrounding(bool allowed,float deltaSeconds)
        {
            SupportedFeet=0;grounding?.Step(allowed,deltaSeconds);
            if(!allowed)return;
            if(leftContact!=null&&leftContact.Probe())SupportedFeet++;
            if(rightContact!=null&&rightContact.Probe())SupportedFeet++;
        }

        private CityHumanPose(Transform reference, Transform[] selected)
        {
            frame = reference; bones = selected; rest = new Quaternion[bones.Length];
            for (int i = 0; i < bones.Length; i++) rest[i] = bones[i].localRotation;
            leftSide = Mathf.Sign(frame.InverseTransformPoint(bones[3].position).x);
            rightSide = Mathf.Sign(frame.InverseTransformPoint(bones[6].position).x);
            leftFootFrame = Quaternion.Inverse(frame.rotation) * bones[11].rotation;
            rightFootFrame = Quaternion.Inverse(frame.rotation) * bones[14].rotation;
        }

        public static bool TryCreate(GameObject model, Transform reference, out CityHumanPose pose)
        {
            pose = null;
            if (model == null || reference == null || !model.transform.IsChildOf(reference)) return false;
            Transform[] all = model.GetComponentsInChildren<Transform>(true);
            var selected = new Transform[Required.Length];
            for (int i = 0; i < Required.Length; i++)
            {
                foreach (Transform bone in all)
                    if (bone.name == Required[i])
                    {
                        if (selected[i] != null) return false;
                        selected[i] = bone;
                    }
                if (selected[i] == null) return false;
            }
            if (selected[4].parent != selected[3] || selected[5].parent != selected[4] ||
                selected[7].parent != selected[6] || selected[8].parent != selected[7] ||
                selected[10].parent != selected[9] || selected[11].parent != selected[10] ||
                selected[13].parent != selected[12] || selected[14].parent != selected[13]) return false;
            pose = new CityHumanPose(reference, selected);
            CityHumanFace.TryCreate(model,reference,out CityHumanFace face);pose.Face=face;
            if (CityHumanHandPose.TryCreate(all,true,out CityHumanHandPose left) &&
                CityHumanHandPose.TryCreate(all,false,out CityHumanHandPose right))
            { pose.leftHand=left;pose.rightHand=right; }
            return true;
        }

        public void Step(float deltaSeconds, float speed, bool guard)
        {
            if (!float.IsFinite(deltaSeconds) || deltaSeconds <= 0 || !float.IsFinite(speed)) return;
            float dt = Mathf.Min(deltaSeconds, .05f);
            StepObserved(Vector3.forward*(guard?0:Mathf.Clamp(speed,0,8))*dt,dt,guard);
        }

        public void StepObserved(Vector3 displacement,float deltaSeconds,bool guard)
        {
            if(!float.IsFinite(deltaSeconds)||deltaSeconds<=0)return;
            Restore();
            if(!gait.Step(displacement,deltaSeconds)){Reset();return;}
            if(gait.WasTeleported){grounding?.Reset();seconds=guardWeight=combatPunch=0;}
            seconds=Mathf.Repeat(seconds+Mathf.Min(deltaSeconds,.05f),1000);
            // Slight standing flex leaves room for the shared two-bone support solver.
            CityActorMotion.PoseLeg(bones[9], bones[10], bones[11], frame, gait.LeftStride, Mathf.Max(4,gait.LeftKneeFlex));
            CityActorMotion.PoseLeg(bones[12], bones[13], bones[14], frame, gait.RightStride, Mathf.Max(4,gait.RightKneeFlex));
            bones[11].rotation = frame.rotation * leftFootFrame;
            bones[14].rotation = frame.rotation * rightFootFrame;
            // Explicit wardrobe poses are immediate; live combat blends in and out.
            guardWeight=guard?1:Mathf.MoveTowards(guardWeight,combatPunch>0?1:0,Mathf.Min(deltaSeconds,.05f)*(combatPunch>0?18:7));
            CityActorMotion.RelaxArm(bones[3], bones[4], frame, leftSide, gait.LeftArmSwing, gait.RunningBlend);
            CityActorMotion.RelaxArm(bones[6], bones[7], frame, rightSide, gait.RightArmSwing, gait.RunningBlend);
            if (guardWeight > 0)
            {
                Quaternion leftArm=bones[3].localRotation,leftElbow=bones[4].localRotation;
                Quaternion rightArm=bones[6].localRotation,rightElbow=bones[7].localRotation;
                CityActorMotion.PoseBoxingArm(bones[3], bones[4], frame, leftSide, 0, null, .7f);
                CityActorMotion.PoseBoxingArm(bones[6], bones[7], frame, rightSide, combatPunch, null, .7f);
                bones[3].localRotation=Quaternion.Slerp(leftArm,bones[3].localRotation,guardWeight);
                bones[4].localRotation=Quaternion.Slerp(leftElbow,bones[4].localRotation,guardWeight);
                bones[6].localRotation=Quaternion.Slerp(rightArm,bones[6].localRotation,guardWeight);
                bones[7].localRotation=Quaternion.Slerp(rightElbow,bones[7].localRotation,guardWeight);
            }
            ApplyHand(leftHand,bones[4],bones[5],leftSide,0);
            ApplyHand(rightHand,bones[7],bones[8],rightSide,combatPunch);
            bones[1].localRotation *= Quaternion.Euler(Mathf.Sin(seconds * 1.8f) * .6f, 0, 0);
        }

        private void ApplyHand(CityHumanHandPose hand,Transform elbow,Transform wrist,float side,float punch)
        {
            if(hand==null)return;
            Vector3 direction=(wrist.position-elbow.position).normalized;
            Vector3 palm=Quaternion.AngleAxis(90*side*punch,direction)*Vector3.Cross(direction,frame.right);
            hand.Apply(guardWeight,gait.Speed/8,palm);
        }

        private void Restore() { for (int i = 0; i < bones.Length; i++) if (bones[i] != null) bones[i].localRotation = rest[i]; }
        public void Reset() { Restore(); leftHand?.Reset();rightHand?.Reset();Face?.Reset();gait.Reset(); seconds = guardWeight = combatPunch = 0;grounding?.Reset();SupportedFeet=0; }
    }
}
