using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.EventSystems;

namespace Pozarevac.CityGame
{
    // Fixed-size projectile pool; swept casts prevent tunneling and honor the nearest obstacle.
    public sealed class CityCombatRuntime : MonoBehaviour
    {
        private sealed class Projectile
        {
            public GameObject Visual;
            public Vector3 Position, Velocity;
            public float Distance, Age;
            public CityAttack Attack;
            public bool Active;
        }
        private readonly Projectile[] projectiles = new Projectile[8];
        private readonly RaycastHit[] hits = new RaycastHit[24];
        private CityGameBootstrap game;
        private CityHud hud;
        private Camera view;
        private CityCameraRig cameraRig;
        private Ray Aim => cameraRig != null ? cameraRig.AimRay : new Ray(view.transform.position, view.transform.forward);
        private Transform props, held;
        private GameObject heldStone, heldStick;
        private Material stoneMaterial, woodMaterial, glassMaterial;
        private float swing, hudCountdown;
        public CityCombat Inventory { get; private set; }
        public CityWeaponPickup[] Pickups { get; private set; }
        public CityBreakableWindow PracticeWindow { get; private set; }
        public int ActiveProjectiles { get { int count = 0; foreach (Projectile p in projectiles) if (p != null && p.Active) count++; return count; } }
        public bool CanAct => game != null && game.WorldReady && !game.Progression.Lifestyle.IsTraining && !game.Progression.Lifestyle.IsBoxing && game.Vitals.CanAct && !game.Director.IsPaused && Time.timeScale > 0;

        public void Initialize(CityGameBootstrap owner, CityHud cityHud, Transform worldRoot, Vector3 spawn)
        {
            if (game != null) throw new System.InvalidOperationException("Combat already initialized.");
            game = owner; hud = cityHud; Inventory = new CityCombat();
            view = game.Player.GetComponentInChildren<Camera>();
            cameraRig = view.GetComponent<CityCameraRig>();
            props = new GameObject("VKT_CombatProps").transform; props.SetParent(worldRoot, false);
            Material template = Resources.Load<Material>("CityMissionMarker");
            stoneMaterial = MakeMaterial(template, new Color(.35f, .36f, .38f));
            woodMaterial = MakeMaterial(template, new Color(.32f, .18f, .08f));
            glassMaterial = MakeMaterial(template, new Color(.34f, .65f, .76f));
            for (int i = 0; i < projectiles.Length; i++)
            {
                GameObject visual = Primitive("StoneProjectile_" + i, PrimitiveType.Sphere, props, Vector3.one * .18f, stoneMaterial, false);
                visual.SetActive(false); projectiles[i] = new Projectile { Visual = visual };
            }
            held = new GameObject("VKT_HeldWeapon").transform; held.SetParent(view.transform, false);
            held.localPosition = new Vector3(.28f, -.24f, .55f);
            heldStone = Primitive("HeldStone", PrimitiveType.Sphere, held, Vector3.one * .12f, stoneMaterial, false);
            heldStick = Primitive("HeldStick", PrimitiveType.Cube, held, new Vector3(.05f, .55f, .05f), woodMaterial, false);
            Pickups = new CityWeaponPickup[4];
            for (int i = 0; i < 4; i++)
            {
                CityWeapon kind = i == 3 ? CityWeapon.Stick : CityWeapon.Stone;
                GameObject prop = Primitive(kind + "Pickup_" + i, i == 3 ? PrimitiveType.Cube : PrimitiveType.Sphere,
                    props, i == 3 ? new Vector3(.12f, .12f, .85f) : Vector3.one * .35f, i == 3 ? woodMaterial : stoneMaterial, true);
                prop.transform.position = spawn + new Vector3(i - 2, .3f, 3);
                Pickups[i] = prop.AddComponent<CityWeaponPickup>(); Pickups[i].Initialize(this, kind);
            }
            CreateWindow(spawn + new Vector3(7, 0, 7));
            TextMesh tutorial = new GameObject("EquipmentSign").AddComponent<TextMesh>(); tutorial.transform.SetParent(props, false);
            tutorial.transform.position = spawn + new Vector3(0, 1.1f, 3.5f); tutorial.text = "KAMENJE + MOTKA / E\nQ: OPREMA / LMB: NAPAD";
            tutorial.anchor = TextAnchor.MiddleCenter; tutorial.fontSize = 40; tutorial.characterSize = .065f;
            tutorial.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf"); tutorial.GetComponent<Renderer>().sharedMaterial = tutorial.font.material;
            game.Touch.BindCombat(() => TryAttack(), CycleWeapon);
            RefreshHeld();
        }
        private static Material MakeMaterial(Material template, Color color)
        {
            Material result = new Material(template); result.color = color; result.DisableKeyword("_EMISSION");
            if (result.HasProperty("_EmissionColor")) result.SetColor("_EmissionColor", Color.black);
            return result;
        }
        private static GameObject Primitive(string title, PrimitiveType type, Transform parent, Vector3 size, Material material, bool collide)
        {
            GameObject obj = GameObject.CreatePrimitive(type); obj.name = title; obj.transform.SetParent(parent, false);
            obj.transform.localScale = size; obj.GetComponent<Renderer>().sharedMaterial = material;
            obj.GetComponent<Collider>().enabled = collide; if (!collide) obj.layer = 2;
            return obj;
        }
        private void CreateWindow(Vector3 position)
        {
            Transform root = new GameObject("VKT_RivalWindow").transform; root.SetParent(props, false); root.position = position;
            for (int i = 0; i < 4; i++)
            {
                bool vertical = i < 2;
                GameObject frame = Primitive("WindowFrame", PrimitiveType.Cube, root,
                    vertical ? new Vector3(.12f, 2f, .2f) : new Vector3(2.8f, .12f, .2f), woodMaterial, true);
                frame.transform.localPosition = vertical ? new Vector3(i == 0 ? -1.4f : 1.4f, 1.3f, 0) : new Vector3(0, i == 2 ? .3f : 2.3f, 0);
            }
            GameObject pane = Primitive("RivalGlass", PrimitiveType.Cube, root, new Vector3(2.65f, 1.85f, .08f), glassMaterial, true);
            pane.transform.localPosition = Vector3.up * 1.3f;
            TextMesh label = new GameObject("WindowLabel").AddComponent<TextMesh>(); label.transform.SetParent(root, false);
            label.transform.localPosition = new Vector3(0, 2.7f, 0); label.text = "SUPARNIČKI IZLOG / STAKLO";
            label.anchor = TextAnchor.MiddleCenter; label.fontSize = 40; label.characterSize = .09f;
            label.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf"); label.GetComponent<Renderer>().sharedMaterial = label.font.material;
            PracticeWindow = pane.AddComponent<CityBreakableWindow>(); PracticeWindow.Initialize(label);
        }
        public bool TryCollect(CityWeapon weapon)
        {
            if (!CanAct) return false;
            bool success = weapon == CityWeapon.Stone ? Inventory.TryCollectStone() : weapon == CityWeapon.Stick && Inventory.CollectStick();
            if (!success) return false;
            Inventory.SelectWeapon(weapon); RefreshHeld(); hud.ShowNotification(weapon == CityWeapon.Stone ? "Kamen pokupljen. LMB: baci / Q: promeni opremu." : "Motka pokupljena. LMB: udarac / Q: promeni opremu.", 4);
            return true;
        }
        public void CycleWeapon() { if (CanAct) { Inventory.CycleWeapon(); RefreshHeld(); } }
        public bool TryAttack()
        {
            if (!CanAct) return false;
            if (Inventory.SelectedWeapon == CityWeapon.Stone && Inventory.StoneCount == 0)
            { hud.ShowNotification("Nema kamena. Pokupi novi ili pritisni Q za drugu opremu.", 2); return false; }
            Projectile available = null;
            if (Inventory.SelectedWeapon == CityWeapon.Stone)
            {
                foreach (Projectile p in projectiles) if (!p.Active) { available = p; break; }
                if (available == null) return false;
            }
            if (!Inventory.TryAttack(true, out CityAttack attack)) return false;
            swing = .2f;
            if (attack.Weapon == CityWeapon.Stone)
            {
                available.Attack = attack; available.Position = Aim.origin;
                available.Velocity = Aim.direction * attack.ProjectileSpeed; available.Age = available.Distance = 0;
                available.Active = true; available.Visual.transform.position = available.Position; available.Visual.SetActive(true);
            }
            else if (NearestHit(Aim.origin, Aim.direction, attack.Range, false, out RaycastHit hit))
                ResolveImpact(hit.collider, Aim.direction, attack);
            RefreshHeld(); return true;
        }
        private bool NearestHit(Vector3 origin, Vector3 direction, float range, bool sphere, out RaycastHit nearest)
        {
            nearest = default;
            int count = sphere ? Physics.SphereCastNonAlloc(origin, .09f, direction, hits, range, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore)
                : Physics.RaycastNonAlloc(origin, direction, hits, range, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore);
            // Saturation fails closed: the caller consumes the projectile rather than hitting through an unknown blocker.
            if (count == hits.Length) return true;
            float distance = float.PositiveInfinity;
            for (int i = 0; i < count; i++)
                if (hits[i].collider != null && !hits[i].transform.IsChildOf(game.Player) && hits[i].distance < distance)
                { distance = hits[i].distance; nearest = hits[i]; }
            return distance < float.PositiveInfinity;
        }
        private void ResolveImpact(Collider target, Vector3 direction, CityAttack attack)
        {
            if (target == null) return;
            CityPoliceOfficer patrol = target.GetComponentInParent<CityPoliceOfficer>();
            if (patrol != null && patrol.ReceiveImpact(direction)) { hud.ShowNotification("Policajac uzmiče / potera pojačana.", 3); return; }
            CityHazard hazard = target.GetComponentInParent<CityHazard>();
            if (hazard != null && hazard.ReceiveImpact(attack.Damage * (1 + game.Progression.Lifestyle.Strength * .08f), direction))
            { game.Session.AddHeat(4); hud.ShowNotification("Protivnik uzmiče / HEAT +4", 2); return; }
            CityBreakableWindow window = target.GetComponent<CityBreakableWindow>();
            if (window != null && window.ReceiveImpact(attack.Damage))
            { game.Session.AddHeat(8); hud.ShowNotification("Izlog razbijen / HEAT +8.", 4); }
        }
        private void Update()
        {
            if (game == null) return;
            if (!game.Vitals.IsAlive) ClearProjectiles();
            if (!CanAct) return;
            Inventory.Tick(Time.deltaTime);
            if (Keyboard.current != null && Keyboard.current.qKey.wasPressedThisFrame) CycleWeapon();
            bool desktopAim = Cursor.lockState == CursorLockMode.Locked && (game.Touch == null || !game.Touch.IsVisible)
                && (EventSystem.current == null || !EventSystem.current.IsPointerOverGameObject());
            if (desktopAim && Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame) TryAttack();
            swing = Mathf.Max(0, swing - Time.deltaTime);
            held.localRotation = Quaternion.Euler(-Mathf.Sin(swing / .2f * Mathf.PI) * 40, 0, 0);
            hudCountdown -= Time.deltaTime;
            if (hudCountdown <= 0) { RefreshHeld(); hudCountdown = .2f; }
        }
        private void FixedUpdate()
        {
            if (!CanAct) return;
            float dt = Time.fixedDeltaTime;
            foreach (Projectile p in projectiles)
            {
                if (!p.Active) continue;
                Vector3 nextVelocity = p.Velocity + Physics.gravity * dt;
                Vector3 step = (p.Velocity + nextVelocity) * (.5f * dt);
                float distance = Mathf.Min(step.magnitude, Mathf.Max(0, p.Attack.Range - p.Distance));
                if (distance <= .0001f || p.Age >= 3) { Deactivate(p); continue; }
                if (NearestHit(p.Position, step.normalized, distance, true, out RaycastHit hit))
                { ResolveImpact(hit.collider, step.normalized, p.Attack); Deactivate(p); continue; }
                p.Position += step.normalized * distance; p.Velocity = nextVelocity; p.Distance += distance; p.Age += dt;
                p.Visual.transform.position = p.Position;
            }
        }
        private void RefreshHeld()
        {
            heldStone.SetActive(Inventory.SelectedWeapon == CityWeapon.Stone && Inventory.StoneCount > 0);
            heldStick.SetActive(Inventory.SelectedWeapon == CityWeapon.Stick);
            string title = Inventory.SelectedWeapon == CityWeapon.Stone ? "KAMEN" : Inventory.SelectedWeapon == CityWeapon.Stick ? "MOTKA" : "PESNICE";
            hud.SetEquipment($"{title} / kamen {Inventory.StoneCount}/{CityCombat.MaxStones}\nLMB: napad / Q: oprema / E: pokupi");
        }
        private static void Deactivate(Projectile p) { p.Active = false; if (p.Visual != null) p.Visual.SetActive(false); }
        private void ClearProjectiles() { foreach (Projectile p in projectiles) if (p != null) Deactivate(p); }
        private void OnDisable() => ClearProjectiles();
        private void OnDestroy()
        {
            if (props != null) Destroy(props.gameObject); if (held != null) Destroy(held.gameObject);
            if (stoneMaterial != null) Destroy(stoneMaterial); if (woodMaterial != null) Destroy(woodMaterial); if (glassMaterial != null) Destroy(glassMaterial);
        }
    }
}
