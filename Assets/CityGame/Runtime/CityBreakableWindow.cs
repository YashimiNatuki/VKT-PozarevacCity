using UnityEngine;

namespace Pozarevac.CityGame
{
    public sealed class CityBreakableWindow : MonoBehaviour
    {
        private TextMesh caption;
        public bool IsBroken { get; private set; }
        public void Initialize(TextMesh label) => caption = label;
        public bool ReceiveImpact(float damage)
        {
            if (IsBroken || Time.timeScale <= 0 || float.IsNaN(damage) || float.IsInfinity(damage) || damage < 8) return false;
            IsBroken = true; GetComponent<Collider>().enabled = false; GetComponent<Renderer>().enabled = false;
            if (caption != null) caption.text = "RAZBIJENO STAKLO / HEAT";
            return true;
        }
    }
}
