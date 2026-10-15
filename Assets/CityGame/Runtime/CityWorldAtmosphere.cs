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
        private bool ownsAmbient;
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
            ownsAmbient = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name == "VKTOffline";
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
            float daylight = Mathf.Clamp01(Mathf.Sin((normalizedTime - 0.25f) * Mathf.PI * 2f));
            if (sun != null)
            {
                sun.transform.rotation = Quaternion.Euler(normalizedTime * 360f - 90f, 25f, 0f);
                sun.intensity = daylight * 1.15f + 0.05f;
            }
            if (ownsAmbient)
            {
                RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Trilight;
                RenderSettings.ambientSkyColor = Color.Lerp(new Color(.035f, .045f, .08f), new Color(.55f, .63f, .74f), daylight);
                RenderSettings.ambientEquatorColor = Color.Lerp(new Color(.025f, .03f, .045f), new Color(.33f, .35f, .38f), daylight);
                RenderSettings.ambientGroundColor = Color.Lerp(new Color(.015f, .018f, .025f), new Color(.18f, .16f, .13f), daylight);
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
