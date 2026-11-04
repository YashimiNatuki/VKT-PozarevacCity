using System;
using UnityEngine;

namespace Pozarevac.CityGame
{
    // One kinematic car. The player input owner calls Drive while seated.
    public sealed class CityParkedCar : CityInteractable
    {
        private static readonly Vector3 BodyCenter = new Vector3(0, .81f, 0);
        private static readonly Vector3 BodyHalf = new Vector3(.88f, .64f, 1.78f);
        private static readonly Vector3[] ExitOffsets =
        {
            Vector3.left * 1.65f, Vector3.right * 1.65f, Vector3.back * 2.65f
        };
        private readonly Collider[] overlaps = new Collider[24];
        private readonly RaycastHit[] hits = new RaycastHit[24];
        private readonly CityGroundProbe ground = new CityGroundProbe();
        private CityGameBootstrap game;
        private CityHud hud;
        private CityPlayerController driver;
        private CharacterController walker;
        private Func<Vector3, Vector3?> exitGround;
        private Func<Vector3, bool> exitClear;
        private int lastTransition = -1;
        private bool suspended;

        public bool IsOccupied => driver != null;
        public float Speed { get; private set; }
        public string ParkingStreet { get; private set; }
        private bool TouchVisible => game != null && game.Touch != null && game.Touch.IsVisible;
        public override string Prompt => IsOccupied
            ? TouchVisible ? "AUTO / ↑ ↓ gas i rikverc / ← → skretanje / KORISTI: izađi"
                           : "AUTO / W S: gas i rikverc / A D: skretanje / E: izađi"
            : TouchVisible ? "KORISTI / Uđi u parkirani auto" : "E / Uđi u parkirani auto";

        public static CityParkedCar TryCreate(CityGameBootstrap game, CityHud hud, Transform world, Vector3 spawn, CityOsmStreets streets)
        {
            var node = new GameObject("VKT_ParkedCar");
            node.transform.SetParent(world, false);
            var car = node.AddComponent<CityParkedCar>();
            car.game = game;
            car.hud = hud;
            car.exitGround = car.ResolveExitGround;
            car.exitClear = car.IsExitClear;
            bool found = car.TryStreetParking(streets, spawn, out Vector3 position, out Quaternion rotation);
            if (!found)
            {
                rotation = Quaternion.Euler(0, 90, 0);
                foreach (var offset in new[] { new Vector3(8, 0, -4), new Vector3(-8, 0, 5), new Vector3(10, 0, -8) })
                    if (car.TryParkingSpace(spawn + offset, rotation, out position)) { found = true; break; }
            }
            if (!found) { Destroy(node); return null; }
            node.transform.SetPositionAndRotation(position, rotation);
            var collider = node.AddComponent<BoxCollider>();
            collider.center = BodyCenter;
            collider.size = BodyHalf * 2;
            CityParkedCarVisual.Build(node.transform);
            return car;
        }

        private bool TryStreetParking(CityOsmStreets streets, Vector3 spawn, out Vector3 position, out Quaternion rotation)
        {
            position = default;
            rotation = Quaternion.identity;
            if (streets == null || streets.Source == null) return false;
            float best = 80 * 80;
            bool found = false;
            foreach (var road in streets.Source.roads)
            {
                if (road.points == null || road.kind == "footway" || road.kind == "pedestrian") continue;
                float lane = CityRoadGeometry.Width(road.kind) * .5f - 1.15f;
                for (int i = 1; i < road.points.Length; i++)
                {
                    var a = road.points[i - 1];
                    var b = road.points[i];
                    if (!CityRoadGeometry.Valid(a, streets.Source.radiusMeters) || !CityRoadGeometry.Valid(b, streets.Source.radiusMeters)) continue;
                    Vector3 start = streets.RoadPoint(a);
                    Vector3 line = streets.RoadPoint(b) - start;
                    line.y = 0;
                    float length = line.magnitude;
                    if (length < 16) continue;
                    Vector3 forward = line / length;
                    Vector3 center = start + forward * Mathf.Clamp(Vector3.Dot(spawn - start, forward), 8, length - 8);
                    Vector3 across = Vector3.Cross(Vector3.up, forward);
                    for (int side = -1; side <= 1; side += 2)
                    {
                        Vector3 candidate = center + across * (side * lane);
                        float distance = (candidate - spawn).sqrMagnitude;
                        if (distance >= best) continue;
                        Quaternion heading = Quaternion.LookRotation(forward * side, Vector3.up);
                        if (!TryParkingSpace(candidate, heading, out Vector3 parked)) continue;
                        best = distance;
                        position = parked;
                        rotation = heading;
                        ParkingStreet = road.name;
                        found = true;
                    }
                }
            }
            return found;
        }

        private bool TryParkingSpace(Vector3 candidate, Quaternion heading, out Vector3 parked)
        {
            if (!TryGround(candidate, out parked) || !HasSupport(parked, heading)) return false;
            // Reserve the first five metres in front of the parked car as one clear corridor.
            Vector3 forward = heading * Vector3.forward;
            return IsBodyClear(parked + forward * 2.5f, heading, BodyHalf + Vector3.forward * 2.5f);
        }

        public string LocationFrom(Vector3 origin)
        {
            Vector3 offset = transform.position - origin;
            string direction = Mathf.Abs(offset.x) > Mathf.Abs(offset.z) ? (offset.x > 0 ? "istočno" : "zapadno")
                                                                        : (offset.z > 0 ? "severno" : "južno");
            string street = string.IsNullOrWhiteSpace(ParkingStreet) ? "" : ParkingStreet + ", ";
            return $"{street}{Mathf.RoundToInt(new Vector2(offset.x, offset.z).magnitude)} m {direction}";
        }

        public override bool TryInteract(CitySession session)
        {
            if (game == null || suspended || lastTransition == Time.frameCount ||
                !ReferenceEquals(session, game.Session) || !game.Progression.IsCreated ||
                !game.Vitals.CanAct || game.Director.IsPaused || Time.timeScale <= 0) return false;
            if (IsOccupied) return TryExit();
            CityLifestyle life = game.Progression.Lifestyle;
            if (game.Hideout.IsInside || life.IsTraining || life.IsBoxing || life.IsConditioning ||
                (game.Player.position - transform.position).sqrMagnitude > 4f * 4f) return false;
            var movement = game.Player.GetComponent<CityPlayerController>();
            var body = game.Player.GetComponent<CharacterController>();
            if (movement == null || !movement.enabled || movement.Vehicle != null || body == null || !body.enabled) return false;
            driver = movement;
            walker = body;
            walker.enabled = false;
            Speed = 0;
            lastTransition = Time.frameCount;
            driver.BindVehicle(this);
            PlaceDriver();
            hud.ShowNotification("Ušao si u auto. Telefon pauzira vožnju.", 3);
            return true;
        }

        internal void Drive(Vector2 input, float delta)
        {
            if (!IsOccupied || suspended || !float.IsFinite(delta) || delta <= 0 || Time.timeScale <= 0 ||
                game.Director.IsPaused || !game.Vitals.CanAct || !float.IsFinite(input.x) || !float.IsFinite(input.y)) return;
            float time = Mathf.Min(delta, .1f);
            int steps = Mathf.CeilToInt(time / .03334f);
            float dt = time / steps;
            float meters = 0;
            for (int i = 0; i < steps; i++)
            {
                float throttle = Mathf.Clamp(input.y, -1, 1);
                float target = throttle * (throttle < 0 ? 3 : 7.5f);
                Speed = Mathf.MoveTowards(Speed, target, (Mathf.Abs(target) < Mathf.Abs(Speed) ? 9 : 5) * dt);
                if (Mathf.Abs(Speed) < .001f) break;
                if (!TryMove(Mathf.Clamp(input.x, -1, 1), dt, out float moved))
                {
                    Speed = 0;
                    break;
                }
                meters += moved;
            }
            if (meters <= 0) return;
            PlaceDriver();
            game.Progression.Stats.RecordMovement(meters, false, true);
        }

        private bool TryMove(float steer, float dt, out float meters)
        {
            meters = 0;
            Vector3 start = transform.position;
            Quaternion current = transform.rotation;
            float yaw = steer * Mathf.Clamp(Speed / 2, -1, 1) * 62 * dt;
            Quaternion rotation = current * Quaternion.Euler(0, yaw, 0);
            Vector3 forward = rotation * Vector3.forward;
            Vector3 approximate = start + forward * (Speed * dt);
            if (!TryGround(approximate, out Vector3 end) || !HasSupport(end, rotation)) return false;
            Vector3 displacement = end - start;
            float turnMargin = Mathf.Abs(yaw) * Mathf.Deg2Rad * 2f;
            if (!IsBodyClear(end, rotation, BodyHalf + new Vector3(turnMargin, 0, turnMargin))) return false;
            float distance = displacement.magnitude;
            if (distance <= .0001f) return false;
            int count = Physics.BoxCastNonAlloc(start + BodyCenter, BodyHalf, displacement / distance,
                hits, current, distance, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore);
            if (count == hits.Length) return false;
            for (int i = 0; i < count; i++)
                if (IsObstacle(hits[i].collider)) return false;
            transform.SetPositionAndRotation(end, rotation);
            meters = new Vector2(displacement.x, displacement.z).magnitude;
            return true;
        }

        private bool TryGround(Vector3 approximate, out Vector3 point) =>
            ground.TryFindSurface(approximate, out point) && Mathf.Abs(point.y - approximate.y) <= .25f;

        private bool HasSupport(Vector3 center, Quaternion rotation)
        {
            Vector3 axle = rotation * Vector3.forward * 1.14f;
            return TryGround(center + axle, out _) && TryGround(center - axle, out _);
        }

        private bool IsBodyClear(Vector3 position, Quaternion rotation, Vector3 half)
        {
            int count = Physics.OverlapBoxNonAlloc(position + BodyCenter, half, overlaps, rotation,
                Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore);
            if (count == overlaps.Length) return false;
            for (int i = 0; i < count; i++)
                if (IsObstacle(overlaps[i])) return false;
            return true;
        }

        private bool IsObstacle(Collider candidate) => candidate != null && !candidate.transform.IsChildOf(transform);

        private void PlaceDriver() => driver.transform.SetPositionAndRotation(
            transform.TransformPoint(new Vector3(-.32f, .34f, .10f)), transform.rotation);

        private bool TryExit()
        {
            Speed = 0;
            Physics.SyncTransforms();
            if (!TryFindExit(transform.position, transform.rotation, exitGround, exitClear, out Vector3 exit))
            {
                hud.ShowNotification("Izlaz je blokiran. Pomeri auto pa probaj ponovo.", 3);
                return false;
            }
            lastTransition = Time.frameCount;
            driver.transform.SetPositionAndRotation(exit, transform.rotation);
            ReleaseDriver();
            return true;
        }

        private Vector3? ResolveExitGround(Vector3 point) =>
            TryGround(point, out Vector3 resolved) ? resolved + Vector3.up * .06f : (Vector3?)null;

        private bool IsExitClear(Vector3 position)
        {
            float radius = walker.radius + walker.skinWidth;
            Vector3 center = position + transform.rotation * walker.center;
            Vector3 up = Vector3.up * Mathf.Max(0, walker.height * .5f - radius);
            if (Physics.CheckCapsule(center - up, center + up, radius,
                Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore)) return false;
            Vector3 local = transform.InverseTransformPoint(position);
            Vector3 edge = Mathf.Abs(local.x) > 1
                ? new Vector3(Mathf.Sign(local.x) * .92f, local.y, 0)
                : new Vector3(0, local.y, -1.84f);
            return !Physics.Linecast(transform.TransformPoint(edge) + Vector3.up * .9f, center,
                Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore);
        }

        private void OnApplicationPause(bool paused)
        {
            suspended = paused;
            if (paused) Speed = 0;
        }

        internal void Brake() => Speed = 0;

        internal void ReleaseDriver()
        {
            if (!IsOccupied) return;
            Speed = 0;
            driver.BindVehicle(null);
            walker.enabled = true;
            driver = null;
            walker = null;
        }

        public static bool TryFindExit(Vector3 origin, Quaternion rotation, Func<Vector3, Vector3?> resolveGround,
            Func<Vector3, bool> isClear, out Vector3 exit)
        {
            exit = default;
            if (!Finite(origin) || resolveGround == null || isClear == null ||
                !float.IsFinite(Quaternion.Dot(rotation, rotation)) || Quaternion.Dot(rotation, rotation) < .0001f) return false;
            rotation = Quaternion.Normalize(rotation);
            foreach (Vector3 offset in ExitOffsets)
            {
                Vector3 candidate = origin + rotation * offset;
                if (!Finite(candidate)) continue;
                Vector3? resolved = resolveGround(candidate);
                if (!resolved.HasValue || !Finite(resolved.Value) || !isClear(resolved.Value)) continue;
                exit = resolved.Value;
                return true;
            }
            return false;
        }

        private static bool Finite(Vector3 value) =>
            float.IsFinite(value.x) && float.IsFinite(value.y) && float.IsFinite(value.z);
    }
}
