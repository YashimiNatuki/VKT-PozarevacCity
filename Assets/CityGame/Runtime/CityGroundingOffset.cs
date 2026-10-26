using UnityEngine;

namespace Pozarevac.CityGame
{
    // Compensates the controller's contact margin in presentation space only.
    public sealed class CityGroundingOffset
    {
        private readonly Transform owner, visual;
        private readonly Vector3 standingPosition;
        private readonly RaycastHit[] hits = new RaycastHit[12];
        public float Offset { get; private set; }

        public CityGroundingOffset(Transform owner, Transform visual)
        {
            this.owner = owner; this.visual = visual;
            if (visual != null) standingPosition = visual.localPosition;
        }

        public void Step(bool grounded, float seconds)
        {
            if (owner == null || visual == null || !float.IsFinite(seconds) || seconds <= 0) return;
            float target = 0;
            if (grounded)
            {
                int count = Physics.RaycastNonAlloc(owner.position + Vector3.up * .25f, Vector3.down,
                    hits, .5f, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore);
                int nearest = -1; float distance = float.PositiveInfinity;
                if (count < hits.Length)
                    for (int i = 0; i < count; i++)
                    {
                        var collider = hits[i].collider;
                        if (collider == null || collider.transform.IsChildOf(owner) || collider.attachedRigidbody != null) continue;
                        if (hits[i].distance < distance) { distance = hits[i].distance; nearest = i; }
                    }
                if (nearest >= 0 && hits[nearest].normal.y >= .7660444f)
                {
                    float difference = hits[nearest].point.y - owner.position.y;
                    if (float.IsFinite(difference) && Mathf.Abs(difference) <= .12f) target = difference;
                }
            }
            Offset = Mathf.Lerp(Offset, target, 1f - Mathf.Exp(-18f * seconds));
            Vector3 displacement = Vector3.up * Offset;
            visual.localPosition = standingPosition + (visual.parent != null ? visual.parent.InverseTransformVector(displacement) : displacement);
        }

        public void Reset()
        {
            Offset = 0;
            if (visual != null) visual.localPosition = standingPosition;
        }
    }
}
