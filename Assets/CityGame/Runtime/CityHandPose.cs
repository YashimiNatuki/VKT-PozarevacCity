using UnityEngine;

namespace Pozarevac.CityGame
{
    // Calibrated once from the licensed rig. Presentation only: never owns training or rewards.
    public sealed class CityHandPose
    {
        private readonly Transform hand;
        private readonly Transform[] fingers = new Transform[5];
        private readonly Transform[] tips = new Transform[5];
        private readonly Vector3 along, across, palm;
        private readonly Vector3 grip;
        private readonly Quaternion localFrame;
        public bool Ready { get; }
        public Vector3 GripPoint => hand.TransformPoint(grip);
        public Vector3 LongAxis => hand.TransformDirection(along);
        public Vector3 PalmNormal => hand.TransformDirection(palm);
        public Transform Hand => hand;
        public Transform Finger(int index) => fingers[index];

        public CityHandPose(Transform wrist, bool left)
        {
            hand = wrist;
            if (hand == null) return;
            string side = left ? "Left" : "Right";
            fingers[0] = hand.Find(side + "HandIndex1");
            fingers[1] = fingers[0] != null ? fingers[0].Find(side + "HandIndex2") : null;
            fingers[2] = fingers[1] != null ? fingers[1].Find(side + "HandIndex3") : null;
            fingers[3] = hand.Find(side + "HandThumb1");
            fingers[4] = fingers[3] != null ? fingers[3].Find(side + "HandThumb2") : null;
            for (int i = 0; i < fingers.Length; i++)
            {
                if (fingers[i] == null || fingers[i].childCount == 0) return;
                tips[i] = fingers[i].GetChild(0);
            }
            Vector3 knuckle = hand.InverseTransformPoint(fingers[0].position);
            along = knuckle.normalized;
            across = Vector3.ProjectOnPlane(hand.InverseTransformPoint(fingers[3].position), along).normalized;
            palm = Vector3.Cross(across, along).normalized * (left ? 1 : -1);
            if (knuckle.sqrMagnitude < 1e-10f || palm.sqrMagnitude < .9f) return;
            localFrame = Quaternion.LookRotation(palm, along);
            // Palm pad behind the index knuckle, with room for the 32 mm shaft.
            grip = knuckle * .72f + palm * knuckle.magnitude * .72f;
            Ready = true;
        }

        public Quaternion Orientation(Vector3 fingerDirection, Vector3 palmDirection)
        {
            Vector3 normal = Vector3.ProjectOnPlane(palmDirection, fingerDirection).normalized;
            return Quaternion.LookRotation(normal, fingerDirection) * Quaternion.Inverse(localFrame);
        }

        public Vector3 WristTarget(Vector3 contact, Quaternion rotation)
        {
            // Includes the normalized wrapper and current physique scale without baking a mesh.
            Vector3 scaled = Quaternion.Inverse(hand.rotation) * hand.TransformVector(grip);
            return contact - rotation * scaled;
        }

        public void Close(Quaternion rotation)
        {
            if (!Ready) return;
            hand.rotation = rotation;
            Curl(0, 55); Curl(1, 130); Curl(2, 185);
            Aim(3, along * .35f - across * .8f + palm * .5f);
            Aim(4, -along * .2f - across * .8f + palm * .2f);
        }

        private void Curl(int index, float degrees)
        {
            float radians = degrees * Mathf.Deg2Rad;
            Aim(index, along * Mathf.Cos(radians) + palm * Mathf.Sin(radians));
        }

        private void Aim(int index, Vector3 direction)
        {
            Transform bone = fingers[index];
            Vector3 current = tips[index].position - bone.position;
            if (current.sqrMagnitude > 1e-10f)
                bone.rotation = Quaternion.FromToRotation(current, hand.TransformDirection(direction)) * bone.rotation;
        }
    }
}
