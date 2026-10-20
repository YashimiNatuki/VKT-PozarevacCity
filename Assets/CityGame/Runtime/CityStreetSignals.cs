using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace Pozarevac.CityGame
{
    // One owned source and cached listeners. No per-frame scene searches or spawning.
    public sealed class CityStreetSignals : MonoBehaviour
    {
        public const float Radius = 24f, Cooldown = 4f;
        private CityGameBootstrap game;
        private CityHud hud;
        private CityPoliceOfficer[] officers;
        private CityHazard[] hazards;
        private readonly RaycastHit[] hits = new RaycastHit[16];
        private AudioSource source;
        private AudioClip whistle;
        private float remaining;
        private bool suspended;
        public int Emitted { get; private set; }
        public int LastListeners { get; private set; }
        public float CooldownRemaining => remaining;
        public bool Playing => source != null && source.isPlaying;
        public bool CanSignal => isActiveAndEnabled && game != null && !suspended && game.WorldReady && game.Progression.IsCreated &&
            game.Vitals.CanAct && !game.Director.IsPaused && Time.timeScale > 0f &&
            !game.Hideout.IsInside && !game.Progression.Lifestyle.IsTraining &&
            !game.Progression.Lifestyle.IsBoxing && !game.Progression.Lifestyle.IsConditioning;

        public void Initialize(CityGameBootstrap owner, CityHud cityHud)
        {
            if (game != null) throw new InvalidOperationException("Street signals already initialized.");
            game = owner ?? throw new ArgumentNullException(nameof(owner));
            hud = cityHud ?? throw new ArgumentNullException(nameof(cityHud));
            officers = FindObjectsByType<CityPoliceOfficer>(FindObjectsSortMode.None);
            hazards = FindObjectsByType<CityHazard>(FindObjectsSortMode.None);
            var node = new GameObject("VKT_WhistleSource"); node.transform.SetParent(transform, false);
            source = node.AddComponent<AudioSource>(); source.playOnAwake = false;
            source.spatialBlend = 1f; source.minDistance = 3f; source.maxDistance = Radius;
            source.rolloffMode = AudioRolloffMode.Logarithmic; source.dopplerLevel = 0; source.priority = 90;
            whistle = CreateWhistle(); source.clip = whistle;
        }

        public bool TryWhistle()
        {
            if (!CanSignal || remaining > 0f) return false;
            remaining = Cooldown; Emitted++; LastListeners = 0;
            Vector3 origin = game.Player.position + Vector3.up * 1.5f;
            source.transform.position = origin;
            source.mute = game.Audio.Muted; source.volume = .35f * game.Audio.DialogueDuck;
            if (!source.mute) source.Play();
            foreach (var officer in officers)
                if (officer != null && officer.isActiveAndEnabled && officer.HearSignal(origin, ClearPath(origin, officer.transform))) LastListeners++;
            foreach (var hazard in hazards)
                if (hazard != null && hazard.isActiveAndEnabled && hazard.HearSignal(origin, ClearPath(origin, hazard.transform))) LastListeners++;
            hud.ShowNotification(LastListeners > 0 ? $"ZVIŽDUK / reaguje {LastListeners}. Pomeri se: pamte mesto zvuka." : "ZVIŽDUK / nema reakcije u dometu. G: ponovo za 4 s.", 4f);
            return true;
        }

        private bool ClearPath(Vector3 origin, Transform listener)
        {
            Vector3 delta = listener.position + Vector3.up * 1.3f - origin;
            if (delta.sqrMagnitude > Radius * Radius) return false;
            int count = Physics.RaycastNonAlloc(origin, delta.normalized, hits, delta.magnitude,
                Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore);
            if (count == hits.Length) return false;
            for (int i = 0; i < count; i++)
                if (hits[i].collider != null && !hits[i].transform.IsChildOf(game.Player) && !hits[i].transform.IsChildOf(listener)) return false;
            return true;
        }

        private void Update()
        {
            if (game == null) return;
            bool quiet = suspended || !game.Vitals.IsAlive || game.Director.IsPaused || Time.timeScale <= 0f || game.Hideout.IsInside;
            source.mute = quiet || game.Audio.Muted;
            if (quiet) source.Stop();
            if (!quiet) remaining = Mathf.Max(0f, remaining - Time.deltaTime);
            bool typing = EventSystem.current != null && EventSystem.current.currentSelectedGameObject != null &&
                EventSystem.current.currentSelectedGameObject.GetComponent<InputField>() != null;
            if (!typing && Keyboard.current != null && Keyboard.current.gKey.wasPressedThisFrame) TryWhistle();
        }

        public static AudioClip CreateWhistle()
        {
            const int rate = 22050, count = 12128;
            var samples = new float[count]; double phase = 0;
            for (int i = 0; i < count; i++)
            {
                float t = (float)i / rate, p = (float)i / (count - 1);
                phase += 2 * Math.PI * (1550 + 220 * Mathf.Sin(p * Mathf.PI) + 22 * Mathf.Sin(t * 40)) / rate;
                samples[i] = .35f * Mathf.Pow(Mathf.Sin(p * Mathf.PI), 2) * (float)Math.Sin(phase);
            }
            var clip = AudioClip.Create("VKT_OriginalWhistle", count, 1, rate, false);
            clip.SetData(samples, 0); return clip;
        }
        private void OnApplicationPause(bool value) { suspended = value; if (value && source != null) source.Stop(); }
        private void OnDisable() { if (source != null) source.Stop(); }
        private void OnDestroy() { if (source != null) Destroy(source.gameObject); if (whistle != null) Destroy(whistle); }
    }
}
