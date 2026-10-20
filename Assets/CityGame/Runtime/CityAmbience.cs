using System;
using UnityEngine;

namespace Pozarevac.CityGame
{
    // First-party synthetic fallback, not an authentic recording of Pozarevac.
    public sealed class CityAmbience : MonoBehaviour
    {
        private const int SampleRate = 22050, LoopSeconds = 4;
        private const float OutdoorVolume = 0.2f, InteriorAttenuation = 0.12f;
        private CityWorldAtmosphere clock;
        private Func<bool> isInterior, isMuted;
        private AudioSource daySource, nightSource;
        private AudioClip generatedDay, generatedNight;
        private bool initialized;
        private bool suspended;
        private CityGameAudio dialogue;
        private CityMusicPlayer music;
        private CityGameBootstrap game;
        private AudioSource hideoutSource;
        private AudioClip hideoutClip;
        public float HideoutMusicVolume => hideoutSource != null ? hideoutSource.volume : 0f;
        public bool IsUsingSyntheticFallback { get; private set; }

        public void Initialize(CityWorldAtmosphere worldClock, Func<bool> interior, Func<bool> muted)
        {
            if (initialized) throw new InvalidOperationException("City ambience is already initialized.");
            if (worldClock == null) throw new ArgumentNullException(nameof(worldClock));
            clock = worldClock;
            isInterior = interior ?? throw new ArgumentNullException(nameof(interior));
            isMuted = muted ?? throw new ArgumentNullException(nameof(muted));
            dialogue = GetComponent<CityGameAudio>();
            game = GetComponent<CityGameBootstrap>();
            AudioClip day = Resources.Load<AudioClip>("Audio/Ambience/day");
            AudioClip night = Resources.Load<AudioClip>("Audio/Ambience/night");
            if (day == null) day = generatedDay = CreateSyntheticLoop(false);
            if (night == null) night = generatedNight = CreateSyntheticLoop(true);
            IsUsingSyntheticFallback = generatedDay != null || generatedNight != null;
            daySource = CreateSource(day);
            nightSource = CreateSource(night);
            hideoutClip = CreateHideoutLoop();
            hideoutSource = CreateSource(hideoutClip);
            initialized = true;
            if (isActiveAndEnabled) { daySource.Play(); nightSource.Play(); hideoutSource.Play(); }
        }

        private AudioSource CreateSource(AudioClip clip)
        {
            AudioSource source = gameObject.AddComponent<AudioSource>();
            source.playOnAwake = false;
            source.loop = true;
            source.spatialBlend = 0f;
            source.priority = 200;
            source.volume = 0f;
            source.clip = clip;
            return source;
        }

        private void Update()
        {
            if (!initialized || clock == null) return;
            if (music == null) music = GetComponent<CityMusicPlayer>();
            if (suspended || Time.timeScale <= 0f || isMuted() || (game != null && game.Vitals != null && !game.Vitals.IsAlive))
            {
                daySource.volume = 0f;
                nightSource.volume = 0f;
                hideoutSource.volume = 0f;
                return;
            }
            float target = OutdoorVolume * (isInterior() ? InteriorAttenuation : 1f) * (dialogue != null && dialogue.IsDialoguePlaying ? 0.5f : 1f);
            float blend = 1f - Mathf.Exp(-2f * Time.unscaledDeltaTime);
            daySource.volume = Mathf.Lerp(daySource.volume, clock.IsDay ? target : 0f, blend);
            nightSource.volume = Mathf.Lerp(nightSource.volume, clock.IsDay ? 0f : target, blend);
            float roomTarget = isInterior() && (music == null || (!music.HasPlaybackSelection && !music.Muted))
                ? .32f * (music != null ? music.Volume : .35f) * (dialogue != null ? dialogue.DialogueDuck : 1f) : 0f;
            hideoutSource.volume = music != null && (music.HasPlaybackSelection || music.Muted) ? 0f : Mathf.Lerp(hideoutSource.volume, roomTarget, blend);
        }

        // Original ten-second instrumental loop, with no external recording or impersonated voice.
        public static AudioClip CreateHideoutLoop()
        {
            const int rate = 22050, count = rate * 10;
            var data = new float[count];
            float[] notes = { 130.8128f, 155.5635f, 174.6141f, 116.5409f };
            for (int i = 0; i < count; i++)
            {
                float t = (float)i / rate, beat = t * 1.6f, within = beat - Mathf.Floor(beat);
                int chord = Mathf.Min(3, (int)(beat / 4)); float root = notes[chord];
                float attack = Mathf.Min(1f, within / .025f);
                float bass = Mathf.Sin(2 * Mathf.PI * root * .5f * t) * Mathf.Exp(-within * 7) * .16f * attack;
                float offbeat = Mathf.Max(0f, within - .5f);
                float keys = within >= .5f ? (Mathf.Sin(2 * Mathf.PI * root * t) + Mathf.Sin(2 * Mathf.PI * root * 1.5f * t)) * Mathf.Exp(-offbeat * 20) * .055f * Mathf.Min(1f, offbeat / .025f) : 0f;
                float kick = Mathf.Sin(2 * Mathf.PI * 55 * t) * Mathf.Exp(-within * 35) * .12f * attack;
                float edge = Mathf.Min(1f, Mathf.Min(i, count - 1 - i) / (rate * .025f));
                data[i] = (bass + keys + kick) * edge;
            }
            var clip = AudioClip.Create("VKT_OriginalHideoutDowntempo", count, 1, rate, false);
            clip.SetData(data, 0); return clip;
        }

        private static AudioClip CreateSyntheticLoop(bool night)
        {
            int count = SampleRate * LoopSeconds;
            float[] samples = new float[count];
            uint noiseState = night ? 0x34a7b9c1u : 0x729ecf13u;
            float filteredNoise = 0f;
            for (int i = 0; i < count; i++)
            {
                noiseState ^= noiseState << 13;
                noiseState ^= noiseState >> 17;
                noiseState ^= noiseState << 5;
                float whiteNoise = (noiseState & 0xffffu) / 32767.5f - 1f;
                filteredNoise += 0.025f * (whiteNoise - filteredNoise);
                float seconds = (float)i / SampleRate;
                float noiseFade = Mathf.Min(1f, Mathf.Min(i, count - 1 - i) / (SampleRate * 0.08f));
                float sound;
                if (night)
                {
                    float chirp = Mathf.Pow(0.5f + 0.5f * Mathf.Sin(2f * Mathf.PI * 2f * seconds), 4f);
                    sound = 0.06f * chirp * Mathf.Sin(2f * Mathf.PI * 1750f * seconds)
                        + 0.025f * Mathf.Sin(2f * Mathf.PI * 2011.25f * seconds)
                        + 0.06f * filteredNoise * noiseFade;
                }
                else
                {
                    float traffic = 0.65f + 0.35f * Mathf.Sin(2f * Mathf.PI * 0.25f * seconds);
                    sound = traffic * (0.1f * Mathf.Sin(2f * Mathf.PI * 39f * seconds)
                        + 0.045f * Mathf.Sin(2f * Mathf.PI * 61.25f * seconds))
                        + 0.22f * filteredNoise * noiseFade;
                }
                samples[i] = Mathf.Clamp(sound, -0.3f, 0.3f);
            }
            AudioClip clip = AudioClip.Create(night ? "VKT_Synthetic_Night" : "VKT_Synthetic_Day", count, 1, SampleRate, false);
            clip.SetData(samples, 0);
            return clip;
        }

        private void OnEnable()
        {
            if (!initialized) return;
            daySource.UnPause(); nightSource.UnPause(); hideoutSource.UnPause();
        }
        private void OnApplicationPause(bool value)
        {
            suspended=value;
            if(value&&initialized){daySource.volume=nightSource.volume=hideoutSource.volume=0;daySource.Pause();nightSource.Pause();hideoutSource.Pause();}
            else if(initialized){daySource.UnPause();nightSource.UnPause();hideoutSource.UnPause();}
        }
        private void OnDisable()
        {
            if (!initialized) return;
            daySource.volume = nightSource.volume = 0f;
            hideoutSource.volume = 0f;
            daySource.Pause(); nightSource.Pause();
            hideoutSource.Pause();
        }
        private void OnDestroy()
        {
            if (daySource != null) { daySource.Stop(); Destroy(daySource); }
            if (nightSource != null) { nightSource.Stop(); Destroy(nightSource); }
            if (hideoutSource != null) { hideoutSource.Stop(); Destroy(hideoutSource); }
            if (hideoutClip != null) Destroy(hideoutClip);
            if (generatedDay != null) Destroy(generatedDay);
            if (generatedNight != null) Destroy(generatedNight);
        }
    }
}
