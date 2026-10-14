using UnityEngine;

namespace Pozarevac.CityGame
{
    public sealed class CityGroundProbe
    {
        private readonly RaycastHit[] hits = new RaycastHit[32];

        public bool TryFindSurface(Vector3 approximatePosition, out Vector3 point)
        {
            int count = Physics.RaycastNonAlloc(approximatePosition + Vector3.up * 300f,
                Vector3.down, hits, 800f, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore);
            float nearest = float.PositiveInfinity;
            point = default;
            for (int i = 0; i < count; i++)
            {
                RaycastHit hit = hits[i];
                if (hit.collider == null || hit.normal.y < 0.65f || hit.distance >= nearest ||
                    hit.collider is CharacterController || hit.collider.GetComponentInParent<CityMission>() != null)
                    continue;
                nearest = hit.distance;
                point = hit.point;
            }
            return !float.IsPositiveInfinity(nearest);
        }
    }
}
