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
        private RectTransform healthFill;
        private GameObject promptCard,notificationCard;
        private CityUiSkin skin;
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
            {
                notification.text = string.Empty;
                if(notificationCard!=null)notificationCard.SetActive(false);
            }
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
            skin=gameObject.AddComponent<CityUiSkin>();
            Card("ProfileCard",new Vector2(0,1),new Vector2(20,-20),new Vector2(610,83));
            Card("MissionCard",new Vector2(0,1),new Vector2(20,-116),new Vector2(680,155));
            Card("VitalsCard",Vector2.one,new Vector2(-20,-20),new Vector2(390,230));
            promptCard=Card("PromptCard",new Vector2(.5f,0),new Vector2(0,70),new Vector2(800,50));
            notificationCard=Card("NotificationCard",new Vector2(.5f,0),new Vector2(0,132),new Vector2(1100,84));
            promptCard.SetActive(false);notificationCard.SetActive(false);

            status = CreateLabel("CityStatus", new Vector2(38f, -33f), new Vector2(560f, 30f), 20, TextAnchor.UpperLeft);
            identity = CreateLabel("VKTIdentity", new Vector2(38f, -70f), new Vector2(560f, 24f), 15, TextAnchor.UpperLeft);
            identity.text = "VKT / Velika Kradja Točkova / Po-Blok-C / what bike since 1995";
            health = CreateLabel("Health", new Vector2(-36f, -120f), new Vector2(400f, 42f), 20, TextAnchor.UpperRight);
            equipment = CreateLabel("Equipment", new Vector2(-36f, -175f), new Vector2(350f, 64f), 15, TextAnchor.UpperRight);
            cash = CreateLabel("Cash", new Vector2(-36f, -34f), new Vector2(360f, 48f), 24, TextAnchor.UpperRight);
            clock = CreateLabel("Clock", new Vector2(-36f, -78f), new Vector2(360f, 38f), 16, TextAnchor.UpperRight);
            prompt = CreateLabel("Prompt", new Vector2(0f, 72f), new Vector2(900f, 48f), 20, TextAnchor.LowerCenter);
            mission = CreateLabel("MissionJournal", new Vector2(38f, -132f), new Vector2(638f, 125f), 18, TextAnchor.UpperLeft);
            notification = CreateLabel("Notification", new Vector2(0f, 132f), new Vector2(1200f, 70f), 20, TextAnchor.LowerCenter);
            notification.color = new Color(1f, 0.85f, 0.45f);
            Text crosshair = CreateLabel("Crosshair", Vector2.zero, new Vector2(24f, 24f), 18, TextAnchor.MiddleCenter);
            crosshair.text = "+";
            GameObject bar=new GameObject("HealthTrack",typeof(RectTransform),typeof(Image));bar.transform.SetParent(transform,false);
            RectTransform track=bar.GetComponent<RectTransform>();track.anchorMin=track.anchorMax=Vector2.one;track.pivot=Vector2.one;track.anchoredPosition=new Vector2(-36,-159);track.sizeDelta=new Vector2(350,7);
            bar.GetComponent<Image>().color=new Color(.18f,.25f,.28f);bar.GetComponent<Image>().raycastTarget=false;
            GameObject fill=new GameObject("HealthFill",typeof(RectTransform),typeof(Image));fill.transform.SetParent(track,false);healthFill=fill.GetComponent<RectTransform>();healthFill.anchorMin=Vector2.zero;healthFill.anchorMax=Vector2.one;healthFill.offsetMin=healthFill.offsetMax=Vector2.zero;fill.GetComponent<Image>().color=new Color(.25f,.84f,.68f);fill.GetComponent<Image>().raycastTarget=false;
        }

        private GameObject Card(string name,Vector2 anchor,Vector2 at,Vector2 size)
        {
            GameObject card=new GameObject(name,typeof(RectTransform),typeof(Image));card.transform.SetParent(transform,false);card.transform.SetAsFirstSibling();
            RectTransform rect=card.GetComponent<RectTransform>();rect.anchorMin=rect.anchorMax=anchor;rect.pivot=anchor;rect.anchoredPosition=at;rect.sizeDelta=size;
            Image image=card.GetComponent<Image>();image.sprite=skin.Rounded;image.type=Image.Type.Sliced;image.color=new Color(.025f,.055f,.075f,.82f);image.raycastTarget=false;return card;
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
            if(promptCard!=null)promptCard.SetActive(!string.IsNullOrEmpty(value));
        }

        public void SetIdentity(string callsign, bool phoneUnlocked)
        {
            if (identity != null) identity.text = "VKT  /  " + callsign +
                (phoneUnlocked ? "  •  P: TELEFON" : "  •  PRVI UGOVOR OTKLJUČAVA TELEFON");
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
            if(healthFill!=null){healthFill.anchorMax=new Vector2(maximum>0?Mathf.Clamp01(current/maximum):0,1);healthFill.GetComponent<Image>().color=current>maximum*.3f?new Color(.25f,.84f,.68f):new Color(1,.35f,.3f);}
        }
        public void SetEquipment(string value) { if (equipment != null) equipment.text = value ?? string.Empty; }

        public void ShowNotification(string value, float seconds)
        {
            if (notification == null) return;
            notification.text = value ?? string.Empty;
            notificationUntil = Time.unscaledTime + Mathf.Max(0f, seconds);
            if(notificationCard!=null)notificationCard.SetActive(!string.IsNullOrEmpty(value));
        }

        private void Refresh()
        {
            if (session == null)
                return;

            status.text = $"PO-BLOK-C   /   REP {session.Reputation:000}   /   HEAT {session.Heat:00}%";
            cash.text = $"{session.Cash:N0} DIN";
        }
    }
}
