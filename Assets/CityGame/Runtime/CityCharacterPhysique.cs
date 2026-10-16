using UnityEngine;

namespace Pozarevac.CityGame
{
    // Small, reversible proportions on the existing skeleton; no mesh copies or save schema changes.
    public sealed class CityCharacterPhysique
    {
        private readonly Transform chest;
        private readonly Vector3 original;
        public CityCharacterPhysique(GameObject model)
        {
            chest = CityCharacterModel.AnimationRoot(model).transform.Find("HipsCtrl/Hips/Spine/Chest/UpperChest");
            if (chest != null) original = chest.localScale;
        }
        public void Apply(int strength)
        {
            if (chest == null) return;
            float level = Mathf.Clamp(strength, 0, 5);
            chest.localScale = Vector3.Scale(original, new Vector3(1 + level * .014f, 1, 1 + level * .012f));
        }
    }
}
