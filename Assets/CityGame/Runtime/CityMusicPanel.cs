using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;

namespace Pozarevac.CityGame
{
    public sealed class CityMusicPanel : MonoBehaviour
    {
        private CityMusicPlayer player;
        private GameObject overlay, panel, ownedEvents;
        private Text status;
        private Func<bool> gamePaused;
        private Action<bool> setOverlay;
        public bool IsOpen => panel != null && panel.activeSelf;
        public void Initialize(CityMusicPlayer music, Func<bool> isPaused = null, Action<bool> overlayChanged = null)
        {
            if (overlay != null) return;
            player = music; gamePaused = isPaused; setOverlay = overlayChanged; player.Initialize();
            overlay = new GameObject("VKT_Music_UI", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster)); overlay.transform.SetParent(transform, false);
            Canvas canvas = overlay.GetComponent<Canvas>(); canvas.renderMode = RenderMode.ScreenSpaceOverlay; canvas.sortingOrder = 25;
            CanvasScaler scaler = overlay.GetComponent<CanvasScaler>(); scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize; scaler.referenceResolution = new Vector2(1280, 720); scaler.matchWidthOrHeight = 0.5f;
            if (EventSystem.current == null) ownedEvents = new GameObject("VKT_Music_Events", typeof(EventSystem), typeof(InputSystemUIInputModule));
            panel = new GameObject("MusicPanel", typeof(RectTransform), typeof(Image)); panel.transform.SetParent(overlay.transform, false);
            RectTransform box = panel.GetComponent<RectTransform>(); box.anchorMin = box.anchorMax = new Vector2(1, 0); box.pivot = new Vector2(1, 0); box.anchoredPosition = new Vector2(-20, 315); box.sizeDelta = new Vector2(420, 270);
            panel.GetComponent<Image>().color = new Color(0.03f, 0.04f, 0.07f, 0.97f);
            status = Label(panel.transform, "VKT muzika", new Vector2(15, -15), new Vector2(390, 72));
            Button(panel.transform, "PRETHODNA", new Vector2(15, -100), () => player.Previous());
            Button(panel.transform, "PUSTI/PAUZA", new Vector2(150, -100), () => player.TogglePause());
            Button(panel.transform, "SLEDEĆA", new Vector2(285, -100), () => player.Next());
            Button(panel.transform, "TIŠE", new Vector2(15, -155), () => player.Volume -= 0.1f);
            Button(panel.transform, "GLASNIJE", new Vector2(150, -155), () => player.Volume += 0.1f);
            Button(panel.transform, "MUTE", new Vector2(285, -155), () => player.Muted = !player.Muted);
            Button(overlay.transform, "MUZIKA / F6", new Vector2(-420, 290), Toggle, true);
            Button(panel.transform, "OSVEŽI", new Vector2(15, -210), () => player.Refresh());
            Button(panel.transform, "RADIO", new Vector2(150, -210), () => player.Radio());
            Button(panel.transform, "ZATVORI", new Vector2(285, -210), Close);
            panel.SetActive(false);
        }
        public void Toggle()
        {
            if (IsOpen) { Close(); return; }
            if (panel == null || (gamePaused != null && gamePaused())) return;
            panel.SetActive(true); setOverlay?.Invoke(true);
        }
        public void Close()
        {
            if (!IsOpen) return;
            panel.SetActive(false); setOverlay?.Invoke(false);
        }
        private void Update()
        {
            if (Keyboard.current != null && Keyboard.current.f6Key.wasPressedThisFrame) Toggle();
            if (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame) Close();
            if (panel != null && panel.activeSelf) status.text = player.Status + $"\nJačina: {Mathf.RoundToInt(player.Volume * 100)}%" + (player.Muted ? " • bez zvuka" : "");
        }
        private static Text Label(Transform parent, string text, Vector2 position, Vector2 size)
        {
            GameObject item = new GameObject("Label", typeof(RectTransform), typeof(Text)); item.transform.SetParent(parent, false);
            RectTransform rect = item.GetComponent<RectTransform>(); rect.anchorMin = rect.anchorMax = new Vector2(0, 1); rect.pivot = new Vector2(0, 1); rect.anchoredPosition = position; rect.sizeDelta = size;
            Text label = item.GetComponent<Text>(); label.text = text; label.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf"); label.fontSize = 16; label.color = Color.white; label.raycastTarget = false; return label;
        }
        private static void Button(Transform parent, string text, Vector2 position, UnityEngine.Events.UnityAction action, bool corner = false)
        {
            GameObject item = new GameObject(text, typeof(RectTransform), typeof(Image), typeof(Button)); item.transform.SetParent(parent, false);
            RectTransform rect = item.GetComponent<RectTransform>(); rect.anchorMin = rect.anchorMax = corner ? new Vector2(1, 0) : new Vector2(0, 1); rect.pivot = new Vector2(0, 1); rect.anchoredPosition = position; rect.sizeDelta = new Vector2(120, 42);
            item.GetComponent<Image>().color = new Color(0.12f, 0.22f, 0.3f, 1); item.GetComponent<Button>().onClick.AddListener(action);
            Label(item.transform, text, new Vector2(5, -8), new Vector2(110, 30));
        }
        private void OnDestroy() { Close(); if (overlay != null) Destroy(overlay); if (ownedEvents != null) Destroy(ownedEvents); }
    }
}
