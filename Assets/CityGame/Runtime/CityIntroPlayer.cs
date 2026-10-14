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

        public bool IsActive { get; private set; }
        public bool HasStarted { get; private set; }
        public long DecodedFrame => video != null ? video.frame : -1;
        public uint Width => video != null ? video.width : 0;
        public uint Height => video != null ? video.height : 0;
        public double Length => video != null ? video.length : 0;

        public void Begin(Action<bool> onPlaybackChanged)
        {
            if (video != null) throw new InvalidOperationException("Intro playback is already initialized.");
            playbackChanged = onPlaybackChanged;
            string path = Path.Combine(Application.streamingAssetsPath, "GameIntro_Pozarevac_ThugLife.mp4");
            if (CityGameBootstrap.IsSmokeValidation || !File.Exists(path) || (Application.isBatchMode && !Array.Exists(Environment.GetCommandLineArgs(), item => item == "-cityValidateIntro")))
                return;
            BuildOverlay();
            video = gameObject.AddComponent<VideoPlayer>();
            video.playOnAwake = false;
            video.source = VideoSource.Url;
            video.url = new Uri(path).AbsoluteUri;
            video.renderMode = VideoRenderMode.RenderTexture;
            video.targetTexture = texture;
            video.audioOutputMode = VideoAudioOutputMode.Direct;
            video.timeUpdateMode = VideoTimeUpdateMode.UnscaledGameTime;
            video.isLooping = false;
            video.waitForFirstFrame = true;
            video.prepareCompleted += Prepared;
            video.loopPointReached += Finished;
            video.errorReceived += Failed;
            IsActive = true;
            prepareDeadline = Time.realtimeSinceStartup + 15f;
            playbackChanged?.Invoke(true);
            video.Prepare();
        }

        private void Update()
        {
            if (!IsActive) return;
            Keyboard keyboard = Keyboard.current;
            if (keyboard != null && (keyboard.spaceKey.wasPressedThisFrame || keyboard.escapeKey.wasPressedThisFrame))
                Skip();
            else if (!HasStarted && Time.realtimeSinceStartup >= prepareDeadline)
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
            if (source.audioTrackCount > 0) source.SetDirectAudioVolume(0, 0.7f);
            caption.text = "POZAREVAC - THUG LIFE\nSPACE / ESC TO SKIP";
            source.Play();
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
            video.Stop();
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
            caption.text = "PREPARING POZAREVAC INTRO\nSPACE / ESC TO SKIP";
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
