using UnityEngine;

namespace Pozarevac.CityGame
{
    // Atlas expressions for the reviewed Kenney face. No generated dialogue or phoneme claims.
    public sealed class CityFaceExpression : MonoBehaviour
    {
        private static readonly int FaceId = Shader.PropertyToID("_Face");
        private Material surface;
        private float elapsed, effort, targetEffort;
        public bool Preview { get; set; }
        public Vector4 State { get; private set; }
        public void Initialize(Material material) { surface = material; }
        public void SetEffort(float value) { targetEffort = float.IsNaN(value) ? 0 : Mathf.Clamp01(value); }

        public static float Blink(float seconds)
        {
            if (float.IsNaN(seconds) || float.IsInfinity(seconds) || seconds < 0) return 0;
            float phase = Mathf.Repeat(seconds, 4.1f);
            // A fast closure and slower reopening, separated by a long neutral interval.
            return phase < .065f ? phase / .065f : phase < .20f ? 1 - (phase - .065f) / .135f : 0;
        }

        private void LateUpdate()
        {
            float dt = Preview ? Time.unscaledDeltaTime : Time.deltaTime;
            if (surface == null || dt <= 0) return;
            elapsed += Mathf.Min(dt, .1f);
            effort = Mathf.Lerp(effort, targetEffort, 1 - Mathf.Exp(-5 * dt));
            State = new Vector4(Blink(elapsed + 1.7f), effort,
                Preview ? .35f : .08f * (1 - effort), Mathf.Sin(elapsed * .63f) * .22f);
            surface.SetVector(FaceId, State);
        }
        private void OnDisable()
        {
            State = Vector4.zero;
            if (surface != null) surface.SetVector(FaceId, State);
        }
    }
}
