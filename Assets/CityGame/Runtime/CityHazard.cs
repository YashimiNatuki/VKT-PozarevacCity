using UnityEngine;

namespace Pozarevac.CityGame
{
    public enum CityHazardKind { GuardDog, HostileAdult }

    // Bounded local encounters: no scene scans and no per-frame object spawning.
    public sealed class CityHazard : MonoBehaviour
    {
        [SerializeField] private float detectionRange = 9f;
        [SerializeField] private float attackRange = 1.8f;
        [SerializeField] private float damage = 12f;
        [SerializeField] private float attackCooldown = 2f;
        [SerializeField] private float speed = 2f;
        [SerializeField] private float leashRadius = 12f;
        private CityPlayerVitals target;
        private Vector3 home;
        private float cooldown;
        private Transform visual;
        private float reaction;
        public CityHazardKind Kind { get; private set; }
        public bool IsAttacking { get; private set; }
        public void Configure(CityPlayerVitals player, CityHazardKind kind)
        {
            target = player; Kind = kind; home = transform.position;
            damage = kind == CityHazardKind.GuardDog ? 8f : 15f;
            if (visual != null) return;
            GameObject body = GameObject.CreatePrimitive(kind == CityHazardKind.GuardDog ? PrimitiveType.Cube : PrimitiveType.Capsule);
            body.name = kind == CityHazardKind.GuardDog ? "Pas cuvar" : "Neprijateljski odrasli";
            body.transform.SetParent(transform, false);
            body.transform.localPosition = Vector3.up * (kind == CityHazardKind.GuardDog ? 0.4f : 0.9f);
            body.transform.localScale = kind == CityHazardKind.GuardDog ? new Vector3(0.6f, 0.6f, 1f) : new Vector3(0.7f, 0.9f, 0.7f);
            visual = body.transform;
            Material template = Resources.Load<Material>("CityMissionMarker");
            if (template != null) body.GetComponent<Renderer>().sharedMaterial = template;
            TextMesh label = new GameObject("Upozorenje").AddComponent<TextMesh>();
            label.transform.SetParent(transform, false); label.transform.localPosition = Vector3.up * 2.2f;
            label.text = kind == CityHazardKind.GuardDog ? "PAS ČUVAR\nZaobiđi dvorište" : "NEPRIJATELJSKA EKIPA\nDrži odstojanje";
            label.characterSize = 0.1f; label.fontSize = 40; label.anchor = TextAnchor.MiddleCenter;
        }
        private bool HasClearPath()
        {
            Vector3 origin = transform.position + Vector3.up * 0.8f;
            Vector3 direction = target.transform.position + Vector3.up * 0.8f - origin;
            return !Physics.Raycast(origin, direction.normalized, out RaycastHit hit, direction.magnitude, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore) || hit.transform.IsChildOf(target.transform);
        }
        private void Update()
        {
            float dt = Time.deltaTime;
            if (dt <= 0f || target == null) return;
            cooldown = Mathf.Max(0f, cooldown - dt); reaction = Mathf.Max(0f, reaction - dt);
            if (visual != null) visual.localRotation = Quaternion.Euler(reaction * 25f, 0f, 0f);
            Vector3 delta = target.transform.position - transform.position;
            IsAttacking = target.IsAlive && Mathf.Abs(delta.y) < 3f && delta.sqrMagnitude <= detectionRange * detectionRange;
            Vector3 destination = home;
            if (IsAttacking && Vector3.Distance(target.transform.position, home) <= leashRadius)
            {
                if (delta.sqrMagnitude <= attackRange * attackRange)
                {
                    if (cooldown <= 0f && HasClearPath() && target.ApplyDamage(damage)) { cooldown = attackCooldown; reaction = 0.4f; }
                    return;
                }
                destination = target.transform.position;
            }
            // Stay at the authored ground height and inside the local encounter footprint.
            destination.y = home.y;
            Vector3 travel = destination - transform.position;
            float step = Mathf.Min(speed * dt, travel.magnitude);
            if (step > 0f && !Physics.Raycast(transform.position + Vector3.up * 0.6f, travel.normalized, step + 0.4f, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore))
                transform.position = Vector3.MoveTowards(transform.position, destination, step);
        }
    }
}

