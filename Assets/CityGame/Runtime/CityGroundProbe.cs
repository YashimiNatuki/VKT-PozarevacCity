using UnityEngine;

namespace Pozarevac.CityGame
{
    public sealed class CityGroundProbe
    {
        private readonly RaycastHit[] hits = new RaycastHit[32];

        private static bool Finite(float value) => !float.IsNaN(value) && !float.IsInfinity(value);
        public bool TryFindSurface(Vector3 approximatePosition, out Vector3 point)
        {
            point = default;
            if (!Finite(approximatePosition.x) || !Finite(approximatePosition.y) || !Finite(approximatePosition.z)) return false;
            int count = Physics.RaycastNonAlloc(approximatePosition + Vector3.up * 300f,
                Vector3.down, hits, 800f, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore);
            if (count == hits.Length) return false;
            float nearest = float.PositiveInfinity;
            point = default;
            for (int i = 0; i < count; i++)
            {
                RaycastHit hit = hits[i];
                float separation = Mathf.Abs(hit.point.y - approximatePosition.y);
                if (hit.collider == null || hit.normal.y < 0.65f || separation >= nearest ||
                    hit.collider is CharacterController || hit.collider.GetComponentInParent<CityMission>() != null)
                    continue;
                // Prefer the authored elevation instead of the highest roof below the probe origin.
                nearest = separation;
                point = hit.point;
            }
            return !float.IsPositiveInfinity(nearest);
        }
    }
}
