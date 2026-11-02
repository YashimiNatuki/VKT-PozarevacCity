using UnityEngine;

namespace Pozarevac.CityGame
{
    // Shared presentation IK for the existing bench and the human phone pose.
    public static class CityTwoBonePose
    {
        public static void Solve(Transform upper, Transform lower, Transform tip, Vector3 target, Vector3 hint)
        {
            if (upper == null || lower == null || tip == null || !Finite(target) || !Finite(hint)) return;
            float a = Vector3.Distance(upper.position, lower.position), b = Vector3.Distance(lower.position, tip.position);
            Vector3 delta = target - upper.position;
            if (!float.IsFinite(a) || !float.IsFinite(b) || a < .001f || b < .001f || !Finite(delta) ||
                !float.IsFinite(delta.sqrMagnitude) || delta.sqrMagnitude < .000001f) return;
            float reach = Mathf.Clamp(delta.magnitude, Mathf.Abs(a - b) + .001f, a + b - .001f);
            Vector3 direction = delta.normalized, bend = Vector3.ProjectOnPlane(hint, direction).normalized;
            if (bend.sqrMagnitude < .001f)
                bend = Vector3.Cross(direction, Mathf.Abs(direction.y) < .9f ? Vector3.up : Vector3.right).normalized;
            float along = (a * a - b * b + reach * reach) / (2 * reach);
            Vector3 joint = upper.position + direction * along + bend * Mathf.Sqrt(Mathf.Max(0, a * a - along * along));
            Vector3 reachable = upper.position + direction * reach;
            upper.rotation = Quaternion.FromToRotation(lower.position - upper.position, joint - upper.position) * upper.rotation;
            lower.rotation = Quaternion.FromToRotation(tip.position - lower.position, reachable - lower.position) * lower.rotation;
        }
        private static bool Finite(Vector3 value) => float.IsFinite(value.x) && float.IsFinite(value.y) && float.IsFinite(value.z);
    }
}
