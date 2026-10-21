using UnityEngine;

namespace Pozarevac.CityGame
{
    // Presentation observer only. Movement, collisions, stamina and saves keep their existing owners.
    public sealed class CityGaitState
    {
        public const float StrideMeters = 2.4f;
        public const float DiscontinuityMeters = 5f;
        public float Speed { get; private set; }
        public float Phase { get; private set; }
        public float Weight { get; private set; }
        public bool WasTeleported { get; private set; }
        private float Effort => Mathf.Clamp01(Speed / 8f);
        public float LeftStride => Mathf.Sin(Phase) * Mathf.Lerp(18f, 32f, Effort) * Weight;
        public float RightStride => -LeftStride;
        public float LeftKneeFlex => (6f + Mathf.Max(0f, -Mathf.Cos(Phase)) * 42f) * Weight;
        public float RightKneeFlex => (6f + Mathf.Max(0f, Mathf.Cos(Phase)) * 42f) * Weight;
        public float LeftArmSwing => -Mathf.Sin(Phase) * Mathf.Lerp(.2f, .48f, Effort) * Weight;
        public float RightArmSwing => -LeftArmSwing;

        public bool Step(Vector3 displacement, float seconds)
        {
            if (!float.IsFinite(seconds) || seconds <= 0f) return false;
            if (!float.IsFinite(displacement.x) || !float.IsFinite(displacement.y) || !float.IsFinite(displacement.z))
            { Reset(); return false; }
            if (displacement.sqrMagnitude > DiscontinuityMeters * DiscontinuityMeters)
            { Reset(); WasTeleported = true; return true; }
            WasTeleported = false;
            displacement.y = 0f;
            float distance = displacement.magnitude;
            Speed = Mathf.Lerp(Speed, distance / Mathf.Max(.001f, seconds), 1f - Mathf.Exp(-10f * seconds));
            float targetWeight = distance > .00001f && Speed > .18f ? Mathf.Clamp01(Speed / .8f) : 0f;
            Weight = Mathf.Lerp(Weight, targetWeight, 1f - Mathf.Exp(-12f * seconds));
            Phase = Mathf.Repeat(Phase + distance * (Mathf.PI * 2f / StrideMeters), Mathf.PI * 2f);
            return true;
        }

        public void Reset()
        {
            Speed = Phase = Weight = 0f;
            WasTeleported = false;
        }
    }
}
