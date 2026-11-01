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
        public float Phase => gait.Phase;

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
            pose = new CityHumanPose(reference, selected); return true;
        }

        public void Step(float deltaSeconds, float speed, bool guard)
        {
            if (!float.IsFinite(deltaSeconds) || deltaSeconds <= 0 || !float.IsFinite(speed)) return;
            float dt = Mathf.Min(deltaSeconds, .05f);
            seconds = Mathf.Repeat(seconds + dt, 1000);
            Restore();
            gait.Step(Vector3.forward * (guard ? 0 : Mathf.Clamp(speed, 0, 8)) * dt, dt);
            CityActorMotion.PoseLeg(bones[9], bones[10], bones[11], frame, gait.LeftStride, gait.LeftKneeFlex);
            CityActorMotion.PoseLeg(bones[12], bones[13], bones[14], frame, gait.RightStride, gait.RightKneeFlex);
            bones[11].rotation = frame.rotation * leftFootFrame;
            bones[14].rotation = frame.rotation * rightFootFrame;
            if (guard)
            {
                CityActorMotion.PoseBoxingArm(bones[3], bones[4], frame, leftSide, 0, null, .7f);
                CityActorMotion.PoseBoxingArm(bones[6], bones[7], frame, rightSide, 0, null, .7f);
            }
            else
            {
                CityActorMotion.RelaxArm(bones[3], bones[4], frame, leftSide, gait.LeftArmSwing);
                CityActorMotion.RelaxArm(bones[6], bones[7], frame, rightSide, gait.RightArmSwing);
            }
            bones[1].localRotation *= Quaternion.Euler(Mathf.Sin(seconds * 1.8f) * .6f, 0, 0);
        }

        private void Restore() { for (int i = 0; i < bones.Length; i++) if (bones[i] != null) bones[i].localRotation = rest[i]; }
        public void Reset() { Restore(); gait.Reset(); seconds = 0; }
    }
}
