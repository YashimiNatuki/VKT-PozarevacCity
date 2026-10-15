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
        private float hitStun, suppressed;
        private TextMesh statusLabel;
        public float MaxHitPoints => Kind == CityHazardKind.GuardDog ? 30f : 45f;
        public float CurrentHitPoints { get; private set; } = 30f;
        public bool IsSuppressed => suppressed > 0f;
        public CityHazardKind Kind { get; private set; }
        public bool IsAttacking { get; private set; }
        public void Configure(CityPlayerVitals player, CityHazardKind kind)
        {
            target = player; Kind = kind; home = transform.position;
            CurrentHitPoints = MaxHitPoints; hitStun = suppressed = 0; IsAttacking = false;
            damage = kind == CityHazardKind.GuardDog ? 8f : 15f;
            if (visual != null) { RefreshStatus(); return; }
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
            statusLabel = label; RefreshStatus();
        }
        public bool ReceiveImpact(float amount, Vector3 direction)
        {
            if (Time.timeScale <= 0f || IsSuppressed || CurrentHitPoints <= 0f || float.IsNaN(amount) || float.IsInfinity(amount) || amount <= 0f ||
                float.IsNaN(direction.x) || float.IsInfinity(direction.x) || float.IsNaN(direction.y) || float.IsInfinity(direction.y) || float.IsNaN(direction.z) || float.IsInfinity(direction.z)) return false;
            CurrentHitPoints = Mathf.Max(0f, CurrentHitPoints - Mathf.Min(amount, MaxHitPoints));
            hitStun = .6f; reaction = .6f; IsAttacking = false; cooldown = Mathf.Max(cooldown, 1f);
            if (CurrentHitPoints <= 0f) suppressed = 8f;
            RefreshStatus();
            return true;
        }
        private void RefreshStatus()
        {
            if (statusLabel == null) return;
            string title = Kind == CityHazardKind.GuardDog ? "PAS ČUVAR" : "NEPRIJATELJSKA EKIPA";
            statusLabel.text = title + (IsSuppressed ? "\nUZMIČE / predah" : hitStun > 0f ? "\nODBIJEN NAPAD" : "\nDrži odstojanje");
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
            if (dt <= 0f) return;
            bool wasStunned = hitStun > 0f, wasSuppressed = IsSuppressed;
            hitStun = Mathf.Max(0f, hitStun - dt); suppressed = Mathf.Max(0f, suppressed - dt);
            if (wasSuppressed && !IsSuppressed) { CurrentHitPoints = MaxHitPoints; cooldown = Mathf.Max(cooldown, 1f); }
            if (wasStunned != (hitStun > 0f) || wasSuppressed != IsSuppressed) RefreshStatus();
            cooldown = Mathf.Max(0f, cooldown - dt); reaction = Mathf.Max(0f, reaction - dt);
            if (visual != null) visual.localRotation = Quaternion.Euler(reaction * 25f, 0f, 0f);
            if (IsSuppressed) { IsAttacking = false; MoveToward(home, dt); return; }
            if (hitStun > 0f || target == null) { IsAttacking = false; return; }
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
            MoveToward(destination, dt);
        }
        private void MoveToward(Vector3 destination, float dt)
        {
            // Stay at the authored ground height and inside the local encounter footprint.
            destination.y = home.y;
            Vector3 travel = destination - transform.position;
            float step = Mathf.Min(speed * dt, travel.magnitude);
            if (step > 0f && !Physics.Raycast(transform.position + Vector3.up * 0.6f, travel.normalized, step + 0.4f, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore))
                transform.position = Vector3.MoveTowards(transform.position, destination, step);
        }
    }
}
