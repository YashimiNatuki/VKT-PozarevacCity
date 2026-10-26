using UnityEngine;

namespace Pozarevac.CityGame
{
    // Contact adapter for the reviewed standing rig; gameplay movement remains authoritative.
    public sealed class CityFootContact
    {
        private readonly Transform owner, facing, hip, knee, foot;
        private readonly Quaternion standingSole;
        private readonly float soleHeight;
        private readonly bool ready;
        private readonly RaycastHit[] hits = new RaycastHit[12];

        // Capture once from the normalized standing pose, before gait modifies the bones.
        public CityFootContact(Transform owner, Transform facing, Transform hip, Transform knee, Transform foot)
        {
            this.owner = owner; this.facing = facing; this.hip = hip; this.knee = knee; this.foot = foot;
            if (owner == null || facing == null || hip == null || knee == null || foot == null) return;
            soleHeight = foot.position.y - owner.position.y;
            if (!float.IsFinite(soleHeight) || soleHeight < 0 || soleHeight > .3f) return;
            standingSole = Quaternion.Inverse(facing.rotation) * foot.rotation;
            ready = true;
        }

        public bool Probe()
        {
            if (!ready || foot == null) return false;
            int count = Physics.RaycastNonAlloc(foot.position + Vector3.up * .25f, Vector3.down,
                hits, .65f, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore);
            if (count == hits.Length) return false; // An incomplete set cannot establish the nearest support.
            int nearest = -1; float distance = float.PositiveInfinity;
            for (int i = 0; i < count; i++)
            {
                var collider = hits[i].collider;
                if (collider == null || collider.transform.IsChildOf(owner) || collider.attachedRigidbody != null) continue;
                if (hits[i].distance < distance) { distance = hits[i].distance; nearest = i; }
            }
            return nearest >= 0 && Apply(hits[nearest].point, hits[nearest].normal);
        }

        public bool Apply(Vector3 point, Vector3 normal)
        {
            if (!ready || hip == null || knee == null || foot == null || facing == null ||
                !Finite(point) || !Finite(normal) || !float.IsFinite(normal.sqrMagnitude) || normal.sqrMagnitude < .001f) return false;
            normal.Normalize();
            if (normal.y < .7660444f) return false; // Static supports up to 40 degrees.
            Vector3 target = point + normal * soleHeight;
            Vector3 correction = target - foot.position;
            if (correction.y > .2f || correction.y < -.035f || new Vector2(correction.x, correction.z).sqrMagnitude > .0225f) return false;
            float contactWeight = 1f - Mathf.Clamp01(-correction.y / .035f);
            // Preserve the swing trajectory: never pull a lifted foot down to the surface.
            target.y = Mathf.Max(target.y, foot.position.y);
            float upper = Vector3.Distance(hip.position, knee.position), lower = Vector3.Distance(knee.position, foot.position);
            Vector3 delta = target - hip.position;
            float reach = delta.magnitude;
            if (!float.IsFinite(reach) || !float.IsFinite(upper) || !float.IsFinite(lower) || upper < .001f || lower < .001f ||
                reach >= upper + lower - .0001f || reach <= Mathf.Abs(upper - lower) + .0001f) return false;
            Vector3 direction = delta / reach;
            Vector3 bend = Vector3.ProjectOnPlane(facing.forward, direction).normalized;
            if (bend.sqrMagnitude < .001f) return false;
            float along = (upper * upper - lower * lower + reach * reach) / (2f * reach);
            Vector3 joint = hip.position + direction * along + bend * Mathf.Sqrt(Mathf.Max(0, upper * upper - along * along));
            if (!Finite(joint)) return false;
            hip.rotation = Quaternion.FromToRotation(knee.position - hip.position, joint - hip.position) * hip.rotation;
            knee.rotation = Quaternion.FromToRotation(foot.position - knee.position, target - knee.position) * knee.rotation;
            Quaternion orientation = Quaternion.FromToRotation(Vector3.up, normal) * facing.rotation * standingSole;
            foot.rotation = Quaternion.Slerp(foot.rotation, orientation, contactWeight);
            return true;
        }

        private static bool Finite(Vector3 value) => float.IsFinite(value.x) && float.IsFinite(value.y) && float.IsFinite(value.z);
    }
}
