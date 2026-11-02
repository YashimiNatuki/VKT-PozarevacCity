using UnityEngine;

namespace Pozarevac.CityGame
{
    // The MPFB rig has five independent three-joint digits; the legacy hand uses a different topology.
    // Cache authored rest frames once. No mesh mutation, input, physics or reward ownership.
    public sealed class CityHumanHandPose
    {
        private static readonly string[] Digits = { "index", "middle", "ring", "pinky", "thumb" };
        private readonly Transform wrist, elbow;
        private readonly Transform[] joints;
        private readonly Quaternion[] rest;
        private readonly Vector3[] axes;
        private readonly Vector3 along, palm;
        private readonly Quaternion localFrame, pointerFrame;
        private readonly Quaternion[] phoneBase = new Quaternion[15];
        private Vector3 phoneAnchor, pointerOffset;
        private bool phoneCaptured;
        private CityHumanHandPose(Transform hand, Transform forearm, Transform[] bones, Vector3 flex)
        {
            wrist = hand; elbow = forearm; joints = bones;
            rest = new Quaternion[15]; axes = new Vector3[15];
            along = wrist.InverseTransformDirection(joints[3].position - wrist.position).normalized;
            palm = wrist.InverseTransformDirection(Vector3.Cross(flex,wrist.TransformDirection(along))).normalized;
            localFrame = Quaternion.LookRotation(palm,along);
            for (int i = 0; i < joints.Length; i++)
            {
                rest[i] = joints[i].localRotation;
                Vector3 axis = flex;
                if (i >= 12)
                {
                    Vector3 target = joints[0].position - joints[i].position;
                    axis = Vector3.Cross(joints[i].up, target.normalized).normalized;
                    if (axis.sqrMagnitude < .5f) axis = flex;
                }
                axes[i] = joints[i].InverseTransformDirection(axis).normalized;
            }
            ApplyPhonePreset(false);
            Vector3 middle = Vector3.zero;
            for (int digit = 0; digit < 4; digit++) middle += Tip(digit) * .25f;
            phoneAnchor = wrist.InverseTransformPoint(middle + wrist.TransformDirection(along) * .04f + wrist.TransformDirection(palm) * .009f);
            ApplyPhonePreset(true);
            pointerOffset = Quaternion.Inverse(wrist.rotation) * (Tip(0) - wrist.position);
            Vector3 indexDirection = wrist.InverseTransformDirection(joints[2].position - joints[1].position).normalized;
            pointerFrame = Quaternion.LookRotation(Vector3.ProjectOnPlane(palm, indexDirection).normalized, indexDirection);
            Reset();
        }
        internal static bool TryCreate(Transform[] all, bool left, out CityHumanHandPose pose)
        {
            pose = null; string side = left ? "l" : "r";
            Transform hand = Unique(all, "hand_" + side), forearm = Unique(all, "lowerarm_" + side);
            if (hand == null || forearm == null || hand.parent != forearm) return false;
            var selected = new Transform[15]; Vector3 flex = Vector3.zero;
            for (int digit = 0; digit < 5; digit++)
            {
                for (int joint = 0; joint < 3; joint++)
                {
                    int i = digit * 3 + joint;
                    selected[i] = Unique(all, Digits[digit] + "_0" + (joint + 1) + "_" + side);
                    Transform parent = joint == 0 ? hand : selected[i - 1];
                    if (selected[i] == null || selected[i].parent != parent) return false;
                    Vector3 segment = selected[i].position - parent.position;
                    if (!Finite(segment) || segment.sqrMagnitude < 1e-10f) return false;
                }
                if (digit < 4)
                    flex += Vector3.Cross((selected[digit * 3 + 1].position - selected[digit * 3].position).normalized,
                        (selected[digit * 3 + 2].position - selected[digit * 3 + 1].position).normalized);
            }
            // Authored neutral curvature identifies flexion without assuming reflected FBX axes.
            if (!Finite(flex) || flex.sqrMagnitude < .001f) return false;
            pose = new CityHumanHandPose(hand, forearm, selected, flex.normalized); return true;
        }
        private static Transform Unique(Transform[] all, string name)
        {
            Transform result = null;
            foreach (Transform bone in all)
                if (bone != null && bone.name == name) { if (result != null) return null; result = bone; }
            return result;
        }
        public void Apply(float guard, float effort, Vector3 palmDirection)
        {
            guard = float.IsFinite(guard) ? Mathf.Clamp01(guard) : 0;
            effort = float.IsFinite(effort) ? Mathf.Clamp01(effort) : 0;
            if (wrist == null || elbow == null) return;
            Vector3 forearm = wrist.position - elbow.position;
            if (Finite(forearm) && forearm.sqrMagnitude > 1e-10f)
            {
                forearm.Normalize();
                Quaternion aligned=Quaternion.FromToRotation(wrist.TransformDirection(along),forearm)*wrist.rotation;
                Vector3 normal=Finite(palmDirection)?Vector3.ProjectOnPlane(palmDirection,forearm).normalized:Vector3.zero;
                if(normal.sqrMagnitude>.5f)
                {
                    Quaternion target=Quaternion.LookRotation(normal,forearm)*Quaternion.Inverse(localFrame);
                    // Spread axial roll into the forearm instead of twisting only the wrist seam.
                    float roll=Vector3.SignedAngle(aligned*palm,normal,forearm);
                    elbow.rotation=Quaternion.AngleAxis(roll*guard*.8f,forearm)*elbow.rotation;
                    wrist.rotation=Quaternion.Slerp(wrist.rotation,target,guard);
                }
                else wrist.rotation=Quaternion.Slerp(wrist.rotation,aligned,guard);
            }
            for (int i = 0; i < joints.Length; i++)
            {
                if (joints[i] == null) continue;
                int joint = i % 3;
                float relaxed = i < 12 ? (joint == 0 ? 6 : joint == 1 ? 9 : 4) + effort * 3 : 3;
                float closed = i < 12 ? (joint == 0 ? 75 : joint == 1 ? 90 : 50) : (joint == 0 ? 28 : 30);
                joints[i].localRotation = rest[i] * Quaternion.AngleAxis(Mathf.Lerp(relaxed, closed, guard), axes[i]);
            }
        }
        public void Reset()
        {
            for (int i = 0; i < joints.Length; i++) if (joints[i] != null) joints[i].localRotation = rest[i];
            phoneCaptured = false;
        }
        internal Vector3 PhoneAnchor => wrist.TransformPoint(phoneAnchor);
        internal Quaternion PhoneFrame => Quaternion.LookRotation(wrist.TransformDirection(palm), wrist.TransformDirection(along));
        internal Quaternion PhoneOrientation(Vector3 up, Vector3 normal) => Quaternion.LookRotation(normal, up) * Quaternion.Inverse(localFrame);
        internal Quaternion PointerOrientation(Vector3 direction, Vector3 normal) =>
            Quaternion.LookRotation(Vector3.ProjectOnPlane(normal, direction).normalized, direction) * Quaternion.Inverse(pointerFrame);
        internal Vector3 PointerWrist(Vector3 target, Quaternion rotation) => target - rotation * pointerOffset;
        internal Vector3 Tip(int digit)
        {
            int end = digit * 3 + 2;
            return joints[end].position + (joints[end].position - joints[end - 1].position) * .75f;
        }
        internal void BeginPhone()
        {
            if (phoneCaptured) return;
            for (int i = 0; i < 15; i++) phoneBase[i] = joints[i].localRotation;
            phoneCaptured = true;
        }
        internal void RestorePhone()
        {
            if (!phoneCaptured) return;
            for (int i = 0; i < 15; i++) if (joints[i] != null) joints[i].localRotation = phoneBase[i];
        }
        internal void EndPhone() { RestorePhone(); phoneCaptured = false; }
        internal void ApplyPhone(bool pointer, float weight)
        {
            if (!phoneCaptured) return;
            for (int i = 0; i < 15; i++)
                joints[i].localRotation = Quaternion.Slerp(phoneBase[i], rest[i] * Quaternion.AngleAxis(PhoneAngle(i, pointer), axes[i]), weight);
        }
        private void ApplyPhonePreset(bool pointer)
        {
            for (int i = 0; i < 15; i++) joints[i].localRotation = rest[i] * Quaternion.AngleAxis(PhoneAngle(i, pointer), axes[i]);
        }
        private static float PhoneAngle(int index, bool pointer)
        {
            int joint = index % 3;
            if (pointer && index < 3) return joint == 1 ? 2 : 1;
            if (pointer && index < 12) return joint == 0 ? 75 : joint == 1 ? 85 : 50;
            if (pointer) return joint == 0 ? 30 : joint == 1 ? 35 : 20;
            if (index >= 12) return joint == 0 ? 18 : joint == 1 ? 24 : 15;
            return joint == 0 ? 25 : joint == 1 ? 45 : 28;
        }
        private static bool Finite(Vector3 v) => float.IsFinite(v.x) && float.IsFinite(v.y) && float.IsFinite(v.z);
    }
}
