using UnityEngine;

namespace Pozarevac.CityGame
{
    public sealed class CityWorldAtmosphere : MonoBehaviour
    {
        [SerializeField, Min(30f)] private float dayDurationSeconds = 1080f;
        [SerializeField, Min(30f)] private float nightDurationSeconds = 720f;
        [SerializeField, Range(0f, 24f)] private float startingHour = 20f;
        [SerializeField] private Light sun;
        [SerializeField] private Gradient fogColor;
        [SerializeField] private AnimationCurve fogDensity = AnimationCurve.Linear(0f, 0.012f, 1f, 0.003f);

        private CityWorldClock clock;
        public float Hour => clock != null ? clock.Hour : startingHour;
        public bool IsDay => Hour >= 6f && Hour < 18f;
        public float DayDurationSeconds => dayDurationSeconds;
        public float NightDurationSeconds => nightDurationSeconds;
        public void ConfigureCycle(float daySeconds, float nightSeconds, float hour)
        {
            clock = new CityWorldClock(daySeconds, nightSeconds, hour);
            dayDurationSeconds = daySeconds; nightDurationSeconds = nightSeconds;
        }

        public void Configure(Light directionalLight)
        {
            sun = directionalLight;
        }

        private void Awake()
        {
            clock = new CityWorldClock(Mathf.Max(30f, dayDurationSeconds), Mathf.Max(30f, nightDurationSeconds), startingHour);
            if (fogColor == null || fogColor.colorKeys.Length == 0)
                fogColor = CreateDefaultSkyGradient();
        }

        private void Update()
        {
            clock.Tick(Time.deltaTime);
            ApplyAtmosphere(Hour / 24f);
        }

        private void ApplyAtmosphere(float normalizedTime)
        {
            if (sun != null)
            {
                sun.transform.rotation = Quaternion.Euler(normalizedTime * 360f - 90f, 25f, 0f);
                sun.intensity = Mathf.Clamp01(Mathf.Sin((normalizedTime - 0.25f) * Mathf.PI * 2f)) * 1.15f + 0.05f;
            }

            RenderSettings.fog = true;
            RenderSettings.fogColor = fogColor.Evaluate(normalizedTime);
            RenderSettings.fogDensity = Mathf.Max(0f, fogDensity.Evaluate(normalizedTime));
        }

        private static Gradient CreateDefaultSkyGradient()
        {
            Gradient gradient = new Gradient();
            gradient.SetKeys(
                new[]
                {
                    new GradientColorKey(new Color(0.025f, 0.035f, 0.09f), 0f),
                    new GradientColorKey(new Color(0.16f, 0.22f, 0.35f), 0.25f),
                    new GradientColorKey(new Color(0.55f, 0.68f, 0.82f), 0.5f),
                    new GradientColorKey(new Color(0.18f, 0.1f, 0.24f), 0.77f),
                    new GradientColorKey(new Color(0.025f, 0.035f, 0.09f), 1f)
                },
                new[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(1f, 1f) });
            return gradient;
        }
    }
}

