using UnityEngine;
using UnityEngine.UI;

namespace Pozarevac.CityGame
{
    public sealed class CityHud : MonoBehaviour
    {
        private Text status;
        private Text cash;
        private Text clock;
        private Text prompt;
        private Text mission;
        private Text notification;
        private Text identity;
        private Text health;
        private Text equipment;
        private float notificationUntil;
        private int displayedMinute = -1;
        private CitySession session;
        private CityWorldAtmosphere atmosphere;

        public void Initialize(CitySession citySession, CityWorldAtmosphere cityAtmosphere)
        {
            if (session != null || citySession == null)
                throw new System.InvalidOperationException("HUD initialization requires one valid session.");
            session = citySession;
            atmosphere = cityAtmosphere;
            BuildCanvas();
            if (isActiveAndEnabled) session.Changed += Refresh;
            Refresh();
        }

        private void Update()
        {
            if (clock != null && atmosphere != null)
            {
                int minute = Mathf.FloorToInt(atmosphere.Hour * 60f);
                if (minute != displayedMinute)
                {
                    displayedMinute = minute;
                    clock.text = $"POZAREVAC  |  {minute / 60:00}:{minute % 60:00}";
                }
            }
            if (notification != null && Time.unscaledTime >= notificationUntil && notification.text.Length > 0)
                notification.text = string.Empty;
        }

        private void OnEnable()
        {
            if (session != null)
            {
                session.Changed += Refresh;
                Refresh();
            }
        }

        private void OnDisable()
        {
            if (session != null) session.Changed -= Refresh;
        }

        private void OnDestroy()
        {
            if (session != null)
                session.Changed -= Refresh;
        }

        private void BuildCanvas()
        {
            Canvas canvas = gameObject.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            gameObject.AddComponent<CanvasScaler>().uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            gameObject.GetComponent<CanvasScaler>().referenceResolution = new Vector2(1920f, 1080f);
            gameObject.AddComponent<GraphicRaycaster>();

            status = CreateLabel("CityStatus", new Vector2(32f, -34f), new Vector2(700f, 48f), 20, TextAnchor.UpperLeft);
            identity = CreateLabel("VKTIdentity", new Vector2(32f, -78f), new Vector2(1100f, 26f), 16, TextAnchor.UpperLeft);
            identity.text = "VKT / Velika Kradja Točkova / Po-Blok-C / what bike since 1995";
            health = CreateLabel("Health", new Vector2(-36f, -120f), new Vector2(400f, 42f), 20, TextAnchor.UpperRight);
            equipment = CreateLabel("Equipment", new Vector2(-36f, -175f), new Vector2(440f, 80f), 17, TextAnchor.UpperRight);
            cash = CreateLabel("Cash", new Vector2(-36f, -34f), new Vector2(360f, 48f), 24, TextAnchor.UpperRight);
            clock = CreateLabel("Clock", new Vector2(-36f, -78f), new Vector2(360f, 38f), 16, TextAnchor.UpperRight);
            prompt = CreateLabel("Prompt", new Vector2(0f, 72f), new Vector2(900f, 48f), 20, TextAnchor.LowerCenter);
            mission = CreateLabel("MissionJournal", new Vector2(32f, -105f), new Vector2(1080f, 210f), 19, TextAnchor.UpperLeft);
            notification = CreateLabel("Notification", new Vector2(0f, 132f), new Vector2(1200f, 70f), 20, TextAnchor.LowerCenter);
            notification.color = new Color(1f, 0.85f, 0.45f);
            Text crosshair = CreateLabel("Crosshair", Vector2.zero, new Vector2(24f, 24f), 18, TextAnchor.MiddleCenter);
            crosshair.text = "+";
        }

        private Text CreateLabel(string labelName, Vector2 anchoredPosition, Vector2 size, int fontSize, TextAnchor alignment)
        {
            GameObject item = new GameObject(labelName, typeof(RectTransform), typeof(Text), typeof(Outline));
            item.transform.SetParent(transform, false);
            RectTransform rect = item.GetComponent<RectTransform>();
            Vector2 anchor = alignment switch
            {
                TextAnchor.UpperRight => Vector2.one,
                TextAnchor.LowerCenter => new Vector2(0.5f, 0f),
                TextAnchor.MiddleCenter => new Vector2(0.5f, 0.5f),
                _ => new Vector2(0f, 1f)
            };
            rect.anchorMin = anchor;
            rect.anchorMax = anchor;
            rect.pivot = anchor;
            rect.anchoredPosition = anchoredPosition;
            rect.sizeDelta = size;
            Text text = item.GetComponent<Text>();
            text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            text.raycastTarget = false;
            text.fontSize = fontSize;
            text.alignment = alignment;
            text.color = new Color(0.94f, 0.98f, 1f, 1f);
            item.GetComponent<Outline>().effectColor = new Color(0f, 0f, 0f, 0.8f);
            return text;
        }

        public void SetPrompt(string value)
        {
            if (prompt != null && prompt.text != (value ?? string.Empty))
                prompt.text = value ?? string.Empty;
        }

        public void SetIdentity(string callsign, bool phoneUnlocked)
        {
            if (identity != null) identity.text = "VKT / Po-Blok-C / what bike since 1995 / " + callsign +
                (phoneUnlocked ? " / P: phone" : " / Finish first contract to unlock phone");
        }

        public void SetOverlayVisible(bool overlayOpen)
        {
            Canvas canvas = GetComponent<Canvas>();
            if (canvas != null) canvas.enabled = !overlayOpen;
        }

        public void SetMissionText(string value)
        {
            if (mission != null && mission.text != value) mission.text = value ?? string.Empty;
        }

        public void SetHealth(float current, float maximum)
        {
            if (health == null) return;
            health.text = current > 0 ? $"ŽIVOT {current:0}/{maximum:0}" : "PAO SI / H: povratak";
            health.color = current > maximum * 0.3f ? Color.white : new Color(1, 0.35f, 0.3f);
        }
        public void SetEquipment(string value) { if (equipment != null) equipment.text = value ?? string.Empty; }

        public void ShowNotification(string value, float seconds)
        {
            if (notification == null) return;
            notification.text = value ?? string.Empty;
            notificationUntil = Time.unscaledTime + Mathf.Max(0f, seconds);
        }

        private void Refresh()
        {
            if (session == null)
                return;

            status.text = $"CITY STATUS  •  REP {session.Reputation:000}  •  HEAT {session.Heat:00}%";
            cash.text = $"₽ {session.Cash:N0}";
        }
    }
}
