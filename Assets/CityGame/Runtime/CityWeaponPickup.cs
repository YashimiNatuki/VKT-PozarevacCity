using UnityEngine;

namespace Pozarevac.CityGame
{
    public sealed class CityWeaponPickup : CityInteractable
    {
        private CityCombatRuntime owner;
        private CityWeapon weapon;
        public override string Prompt => weapon == CityWeapon.Stone ? "[E] Pokupi kamen" : "[E] Pokupi motku";
        public void Initialize(CityCombatRuntime combat, CityWeapon kind) { owner = combat; weapon = kind; }
        public override bool TryInteract(CitySession session)
        {
            if (owner == null || !gameObject.activeInHierarchy || !owner.TryCollect(weapon)) return false;
            gameObject.SetActive(false); return true;
        }
    }
}
