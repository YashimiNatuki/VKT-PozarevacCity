using UnityEngine;

namespace Pozarevac.CityGame
{
    // Shared world-interaction boundary for doors, plots and future vehicle props.
    public abstract class CityInteractable : MonoBehaviour
    {
        public abstract string Prompt { get; }
        public abstract bool TryInteract(CitySession session);
    }
}
