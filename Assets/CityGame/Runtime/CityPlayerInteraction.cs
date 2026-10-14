using UnityEngine;
using UnityEngine.InputSystem;

namespace Pozarevac.CityGame
{
    public sealed class CityPlayerInteraction : MonoBehaviour
    {
        [SerializeField, Min(0.5f)] private float range = 4f;

        private Camera viewCamera;
        private CitySession session;
        private CityHud hud;
        private CityMission focusedMission;
        private CityInteractable focusedObject;
        private Collider focusedCollider;
        private readonly RaycastHit[] interactionHits = new RaycastHit[16];
        private CityPlayerVitals vitals;

        public void Configure(Camera camera, CitySession citySession, CityHud cityHud)
        {
            viewCamera = camera;
            session = citySession;
            hud = cityHud;
            vitals = GetComponent<CityPlayerVitals>();
        }

        private void Update()
        {
            if (Time.deltaTime <= 0f)
            {
                if (hud != null) hud.SetPrompt("Paused - Esc to resume");
                return;
            }
            UpdateFocus();
            if (Keyboard.current != null && Keyboard.current.eKey.wasPressedThisFrame) TryInteract();
        }

        public bool TryInteract()
        {
            if (Time.timeScale <= 0f || session == null || (vitals != null && !vitals.CanAct)) return false;
            UpdateFocus();
            return focusedObject != null ? focusedObject.TryInteract(session) : focusedMission != null && focusedMission.TryComplete(session);
        }

        private void UpdateFocus()
        {
            Collider nextCollider = null;
            if (viewCamera != null)
            {
                int count = Physics.RaycastNonAlloc(viewCamera.transform.position, viewCamera.transform.forward,
                    interactionHits, range, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore);
                float nearest = float.PositiveInfinity;
                // A saturated buffer is ambiguous; do not interact through an unseen obstruction.
                if (count < interactionHits.Length)
                    for (int i = 0; i < count; i++)
                    {
                        RaycastHit hit = interactionHits[i];
                        if (hit.collider == null || hit.collider.transform.IsChildOf(transform) || hit.distance >= nearest)
                            continue;
                        nearest = hit.distance;
                        nextCollider = hit.collider;
                    }
            }
            if (nextCollider != focusedCollider)
            {
                focusedCollider = nextCollider;
                focusedMission = nextCollider != null ? nextCollider.GetComponentInParent<CityMission>() : null;
                focusedObject = nextCollider != null ? nextCollider.GetComponentInParent<CityInteractable>() : null;
            }
            if (hud != null) hud.SetPrompt(focusedObject != null ? focusedObject.Prompt : focusedMission == null ? string.Empty : focusedMission.Prompt);
        }
    }
}
