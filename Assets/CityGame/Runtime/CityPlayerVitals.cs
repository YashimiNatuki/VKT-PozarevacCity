using System;
using UnityEngine;

namespace Pozarevac.CityGame
{
    [DisallowMultipleComponent]
    public sealed class CityPlayerVitals : MonoBehaviour
    {
        public CityHealth Health { get; } = new CityHealth();
        public bool IsAlive => Health.IsAlive;
        public bool CanAct => IsAlive;
        public event Action Died;
        public event Action Respawned;
        public event Action<float> Damaged;
        private Transform checkpoint;
        private CharacterController character;
        private CityPlayerController movement;
        private void Awake() { character = GetComponent<CharacterController>(); movement = GetComponent<CityPlayerController>(); }
        public void Configure(Transform safeCheckpoint) { checkpoint = safeCheckpoint; }
        private void Update() { Health.Tick(Time.deltaTime); }
        public bool ApplyDamage(float amount)
        {
            if (!Health.TryDamage(amount)) return false;
            Damaged?.Invoke(amount);
            if (!IsAlive) Died?.Invoke();
            return true;
        }
        public bool Respawn()
        {
            if (IsAlive || checkpoint == null) return false;
            bool controllerEnabled = character != null && character.enabled;
            if (character != null) character.enabled = false;
            transform.SetPositionAndRotation(checkpoint.position, checkpoint.rotation);
            if (character != null) character.enabled = controllerEnabled;
            if (movement != null) movement.ResetView();
            Health.Respawn();
            Respawned?.Invoke();
            return true;
        }
    }
}
