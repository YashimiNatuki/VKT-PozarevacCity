using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;

namespace Pozarevac.CityGame
{
    public sealed class CityTouchControls : MonoBehaviour
    {
        private CityTouchHold up, down, left, right, sprint;
        private CityTouchLook look;
        private bool jump;
        private bool overlayBlocked;
        private GameObject overlay;
        private GameObject ownedEvents;
        public bool IsVisible => overlay != null && overlay.activeInHierarchy;
        public Vector2 MovementVector => !IsVisible ? Vector2.zero : Vector2.ClampMagnitude(new Vector2((right.Held ? 1 : 0) - (left.Held ? 1 : 0), (up.Held ? 1 : 0) - (down.Held ? 1 : 0)), 1f);
        public bool Sprint => IsVisible && sprint.Held;
        public void Initialize(Action interact, Action phone, Action next, Action retry, Action respawn)
        {
            if (overlay != null) return;
            bool requested = Application.isMobilePlatform || Array.IndexOf(Environment.GetCommandLineArgs(), "-vktTouchPreview") >= 0;
            if (!requested) return;
            overlay = new GameObject("VKT_Touch", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            overlay.transform.SetParent(transform, false);
            Canvas canvas = overlay.GetComponent<Canvas>(); canvas.renderMode = RenderMode.ScreenSpaceOverlay; canvas.sortingOrder = 15;
            CanvasScaler scaler = overlay.GetComponent<CanvasScaler>(); scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1280, 720); scaler.matchWidthOrHeight = 0.5f;
            if (EventSystem.current == null) ownedEvents = new GameObject("VKT_TouchEvents", typeof(EventSystem), typeof(InputSystemUIInputModule));
            // This transparent panel receives look gestures only on the right side.
            GameObject drag = new GameObject("Look", typeof(RectTransform), typeof(Image), typeof(CityTouchLook)); drag.transform.SetParent(overlay.transform, false);
            RectTransform area = drag.GetComponent<RectTransform>(); area.anchorMin = new Vector2(0.48f, 0.23f); area.anchorMax = new Vector2(0.98f, 0.90f); area.offsetMin = area.offsetMax = Vector2.zero;
            drag.GetComponent<Image>().color = new Color(0, 0, 0, 0.005f); look = drag.GetComponent<CityTouchLook>();
            up = Hold("NAPRED", new Vector2(0, 0), new Vector2(145, 230));
            down = Hold("NAZAD", new Vector2(0, 0), new Vector2(145, 70));
            left = Hold("LEVO", new Vector2(0, 0), new Vector2(60, 150));
            right = Hold("DESNO", new Vector2(0, 0), new Vector2(230, 150));
            sprint = Hold("TRČI", new Vector2(1, 0), new Vector2(-250, 70));
            Click("SKOK", new Vector2(1, 0), new Vector2(-145, 70), () => jump = true);
            Click("KORISTI", new Vector2(1, 0), new Vector2(-60, 160), interact);
            Click("TELEFON", new Vector2(1, 1), new Vector2(-70, -170), phone);
            Click("SLEDEĆE", new Vector2(1, 1), new Vector2(-180, -170), next);
            Click("PONOVI", new Vector2(1, 1), new Vector2(-290, -170), retry);
            Click("BOLNICA", new Vector2(1, 1), new Vector2(-400, -170), respawn);
        }
        public void BindCombat(Action attack, Action cycle)
        {
            if (overlay == null) return;
            Click("NAPAD", new Vector2(1, 0), new Vector2(-170, 160), attack);
            Click("OPREMA", new Vector2(1, 0), new Vector2(-280, 160), cycle);
        }
        private GameObject MakeButton(string title, Vector2 anchor, Vector2 position)
        {
            GameObject obj = new GameObject(title, typeof(RectTransform), typeof(Image)); obj.transform.SetParent(overlay.transform, false);
            RectTransform rect = obj.GetComponent<RectTransform>(); rect.anchorMin = rect.anchorMax = anchor; rect.sizeDelta = new Vector2(100, 72); rect.anchoredPosition = position;
            obj.GetComponent<Image>().color = new Color(0.05f, 0.12f, 0.17f, 0.78f);
            GameObject caption = new GameObject("Label", typeof(RectTransform), typeof(Text)); caption.transform.SetParent(obj.transform, false);
            RectTransform labelRect = caption.GetComponent<RectTransform>(); labelRect.anchorMin = Vector2.zero; labelRect.anchorMax = Vector2.one; labelRect.offsetMin = labelRect.offsetMax = Vector2.zero;
            Text text = caption.GetComponent<Text>(); text.text = title; text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf"); text.fontSize = 17; text.color = Color.white; text.alignment = TextAnchor.MiddleCenter; text.raycastTarget = false;
            return obj;
        }
        private CityTouchHold Hold(string title, Vector2 anchor, Vector2 at) => MakeButton(title, anchor, at).AddComponent<CityTouchHold>();
        private void Click(string title, Vector2 anchor, Vector2 at, Action action)
        {
            Button button = MakeButton(title, anchor, at).AddComponent<Button>(); button.onClick.AddListener(() => action?.Invoke());
        }
        public Vector2 ConsumeLook() => IsVisible && look != null ? look.Consume() : Vector2.zero;
        public bool ConsumeJump() { bool value = IsVisible && jump; jump = false; return value; }
        public void ResetInput()
        {
            jump = false;
            if (up != null) up.ResetHeld(); if (down != null) down.ResetHeld(); if (left != null) left.ResetHeld(); if (right != null) right.ResetHeld(); if (sprint != null) sprint.ResetHeld();
            if (look != null) look.ResetLook();
        }
        public void SetOverlayBlocked(bool blocked)
        {
            if (overlay == null || overlayBlocked == blocked) return;
            overlayBlocked = blocked; ResetInput(); overlay.SetActive(!blocked && isActiveAndEnabled);
        }
        private void LateUpdate()
        {
            // The mission director restores desktop cursor lock when closing overlays.
            // Touch preview must keep pointer coordinates available to the EventSystem.
            if (!IsVisible) return;
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = !Application.isMobilePlatform;
        }
        private void OnDisable() { ResetInput(); if (overlay != null) overlay.SetActive(false); }
        private void OnEnable() { if (overlay != null) overlay.SetActive(!overlayBlocked); }
        private void OnApplicationFocus(bool focused) { if (!focused) ResetInput(); }
        private void OnDestroy() { if (ownedEvents != null) Destroy(ownedEvents); }
    }
    public sealed class CityTouchHold : MonoBehaviour, IPointerDownHandler, IPointerUpHandler
    {
        private readonly HashSet<int> pointers = new HashSet<int>();
        public bool Held => pointers.Count > 0;
        public void OnPointerDown(PointerEventData data) { pointers.Add(data.pointerId); }
        public void OnPointerUp(PointerEventData data) { pointers.Remove(data.pointerId); }
        public void ResetHeld() { pointers.Clear(); }
        private void OnDisable() { ResetHeld(); }
    }
    public sealed class CityTouchLook : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IDragHandler
    {
        private int? pointer;
        private Vector2 accumulated;
        public void OnPointerDown(PointerEventData data) { if (!pointer.HasValue) pointer = data.pointerId; }
        public void OnPointerUp(PointerEventData data) { if (pointer == data.pointerId) pointer = null; }
        public void OnDrag(PointerEventData data) { if (pointer == data.pointerId) accumulated += data.delta * (720f / Mathf.Max(1, Screen.height)); }
        public Vector2 Consume() { Vector2 result = accumulated; accumulated = Vector2.zero; return result; }
        public void ResetLook() { pointer = null; accumulated = Vector2.zero; }
        private void OnDisable() { ResetLook(); }
    }
}
