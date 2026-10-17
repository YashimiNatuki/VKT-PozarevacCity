using System;
using System.IO;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using UnityEngine.Video;

namespace Pozarevac.CityGame
{
    public sealed class CityIntroPlayer : MonoBehaviour
    {
        private VideoPlayer video;
        private RenderTexture texture;
        private Canvas canvas;
        private AspectRatioFitter aspect;
        private Text caption;
        private Action<bool> playbackChanged;
        private float prepareDeadline;
        private bool initialized, fallback, suspended;
        private float suspendedAt;

        public bool IsActive { get; private set; }
        public bool HasStarted { get; private set; }
        public long DecodedFrame => video != null ? video.frame : -1;
        public uint Width => video != null ? video.width : 0;
        public uint Height => video != null ? video.height : 0;
        public double Length => video != null ? video.length : 0;
        public int AudioTracks => video != null ? video.audioTrackCount : 0;

        public void Begin(Action<bool> onPlaybackChanged)
        {
            if (initialized) throw new InvalidOperationException("Intro playback is already initialized.");
            initialized = true;
            playbackChanged = onPlaybackChanged;
            string path = Path.Combine(Application.streamingAssetsPath, "GameIntro_Pozarevac_ThugLife.mp4");
            bool validate = Array.Exists(Environment.GetCommandLineArgs(), item => item == "-cityValidateIntro" || item == "-vktIntroSmoke" || item == "-vktIntroFallbackSmoke");
            if ((CityGameBootstrap.IsSmokeValidation || Application.isBatchMode) && !validate)
                return;
            BuildOverlay();
            IsActive = true;
            playbackChanged?.Invoke(true);
            if (Application.platform != RuntimePlatform.Android && !File.Exists(path))
            {
                fallback = true; prepareDeadline = Time.realtimeSinceStartup + 5;
                caption.text = "VKT / VELIKA KRAĐA TOČKOVA\nPOŽAREVAC CITY\nOriginalni video nije instaliran. Ulazak u blok…";
                return;
            }
            video = gameObject.AddComponent<VideoPlayer>();
            video.playOnAwake = false;
            video.source = VideoSource.Url;
            video.url = Application.platform == RuntimePlatform.Android ? path : new Uri(path).AbsoluteUri;
            video.renderMode = VideoRenderMode.RenderTexture;
            video.targetTexture = texture;
            AudioSource audio = gameObject.AddComponent<AudioSource>();
            audio.playOnAwake = false; audio.spatialBlend = 0; audio.volume = .65f; audio.priority = 10;
            video.audioOutputMode = VideoAudioOutputMode.AudioSource;
            video.controlledAudioTrackCount = 1; video.EnableAudioTrack(0, true); video.SetTargetAudioSource(0, audio);
            video.timeUpdateMode = VideoTimeUpdateMode.UnscaledGameTime;
            video.isLooping = false;
            video.waitForFirstFrame = true;
            video.prepareCompleted += Prepared;
            video.loopPointReached += Finished;
            video.errorReceived += Failed;
            prepareDeadline = Time.realtimeSinceStartup + 15f;
            video.Prepare();
        }

        private void Update()
        {
            if (!IsActive || suspended) return;
            Keyboard keyboard = Keyboard.current;
            if (keyboard != null && (keyboard.spaceKey.wasPressedThisFrame || keyboard.escapeKey.wasPressedThisFrame))
                Skip();
            else if ((!HasStarted || fallback) && Time.realtimeSinceStartup >= prepareDeadline)
            {
                Debug.LogWarning("City intro preparation timed out. Continuing to gameplay.");
                Skip();
            }
        }

        private void Prepared(VideoPlayer source)
        {
            if (!IsActive) return;
            HasStarted = true;
            if (source.height > 0) aspect.aspectRatio = (float)source.width / source.height;
            caption.text = "VKT / POŽAREVAC CITY\nSPACE / ESC / PRESKOČI UVOD";
            if(!suspended)source.Play();
        }

        private void Finished(VideoPlayer source) => Skip();

        private void Failed(VideoPlayer source, string message)
        {
            Debug.LogWarning($"City intro playback unavailable: {message}");
            Skip();
        }

        public void Skip()
        {
            if (!IsActive) return;
            IsActive = false;
            if (video != null) video.Stop();
            canvas.enabled = false;
            playbackChanged?.Invoke(false);
            Destroy(gameObject);
        }

        private void BuildOverlay()
        {
            canvas = gameObject.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 100;
            CanvasScaler scaler = gameObject.AddComponent<CanvasScaler>();
            gameObject.AddComponent<GraphicRaycaster>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            GameObject backdrop = new GameObject("IntroBackdrop", typeof(RectTransform), typeof(Image));
            Stretch(backdrop, transform);
            backdrop.GetComponent<Image>().color = Color.black;
            backdrop.GetComponent<Image>().raycastTarget = false;
            GameObject screen = new GameObject("IntroScreen", typeof(RectTransform), typeof(RawImage), typeof(AspectRatioFitter));
            Stretch(screen, transform);
            aspect = screen.GetComponent<AspectRatioFitter>();
            aspect.aspectMode = AspectRatioFitter.AspectMode.FitInParent;
            aspect.aspectRatio = 16f / 9f;
            texture = new RenderTexture(1920, 1080, 0, RenderTextureFormat.ARGB32);
            texture.Create();
            screen.GetComponent<RawImage>().texture = texture;
            screen.GetComponent<RawImage>().raycastTarget = false;
            GameObject label = new GameObject("IntroCaption", typeof(RectTransform), typeof(Text));
            label.transform.SetParent(transform, false);
            RectTransform rect = label.GetComponent<RectTransform>();
            rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0f);
            rect.pivot = new Vector2(0.5f, 0f);
            rect.anchoredPosition = new Vector2(0f, 32f);
            rect.sizeDelta = new Vector2(1000f, 90f);
            caption = label.GetComponent<Text>();
            caption.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            caption.fontSize = 24;
            caption.alignment = TextAnchor.MiddleCenter;
            caption.color = Color.white;
            caption.raycastTarget = false;
            caption.text = "PRIPREMA UVODA / POŽAREVAC CITY\nSPACE / ESC / PRESKOČI UVOD";
            GameObject skip = new GameObject("PRESKOČI UVOD", typeof(RectTransform), typeof(Image), typeof(Button));
            skip.transform.SetParent(transform, false);
            RectTransform buttonRect = skip.GetComponent<RectTransform>();
            buttonRect.anchorMin = buttonRect.anchorMax = new Vector2(1, 0); buttonRect.pivot = new Vector2(1, 0);
            buttonRect.anchoredPosition = new Vector2(-40, 40); buttonRect.sizeDelta = new Vector2(300, 70);
            skip.GetComponent<Image>().color = new Color(.025f,.18f,.22f,.96f);
            skip.GetComponent<Button>().onClick.AddListener(Skip);
            GameObject text = new GameObject("Text", typeof(RectTransform), typeof(Text)); Stretch(text, skip.transform);
            Text labelText = text.GetComponent<Text>(); labelText.font = caption.font; labelText.fontSize = 24;
            labelText.color = Color.white; labelText.alignment = TextAnchor.MiddleCenter; labelText.text = "PRESKOČI UVOD"; labelText.raycastTarget = false;
        }

        private void OnApplicationPause(bool paused)
        {
            if (!IsActive || paused == suspended) return;
            suspended = paused;
            if (paused) { suspendedAt = Time.realtimeSinceStartup; if (video != null) video.Pause(); }
            else { prepareDeadline += Time.realtimeSinceStartup - suspendedAt; if (video != null && HasStarted) video.Play(); }
        }

        private static void Stretch(GameObject item, Transform parent)
        {
            item.transform.SetParent(parent, false);
            RectTransform rect = item.GetComponent<RectTransform>();
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = rect.offsetMax = Vector2.zero;
        }

        private void OnDestroy()
        {
            if (IsActive)
            {
                IsActive = false;
                playbackChanged?.Invoke(false);
            }
            if (video != null)
            {
                video.prepareCompleted -= Prepared;
                video.loopPointReached -= Finished;
                video.errorReceived -= Failed;
                video.Stop();
                video.targetTexture = null;
            }
            if (texture != null)
            {
                texture.Release();
                Destroy(texture);
            }
        }
    }
}
