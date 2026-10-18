using UnityEngine;

namespace Pozarevac.CityGame
{
    public enum CityFootstepKind { Pavement, Grass, Wood }
    // Optional explicit override for authored indoor props/platforms.
    public sealed class CityFootstepSurface : MonoBehaviour
    {
        public CityFootstepKind kind;
    }
}
