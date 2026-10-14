using System;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Pozarevac.CityGame
{
    [RequireComponent(typeof(CharacterController))]
    public sealed class CityPlayerController : MonoBehaviour
    {
        [SerializeField] private Transform view;
        [SerializeField] private float walkSpeed = 5f;
        [SerializeField] private float sprintSpeed = 8f;
        [SerializeField] private float jumpHeight = 1.25f;
        [SerializeField] private float gravity = -24f;
        [SerializeField] private float mouseSensitivity = 0.11f;

        private CharacterController controller;
        private float verticalVelocity;
        private float pitch;
        private CityTouchControls touch;
        private CityPlayerVitals vitals;
        private Func<bool> canAct;
        public void ConfigureTouch(CityTouchControls controls) { touch = controls; }
        public void SetCanAct(Func<bool> predicate) { canAct = predicate; }

        public void ConfigureView(Transform cameraTransform)
        {
            view = cameraTransform;
        }

        public void ResetView()
        {
            pitch = 0; verticalVelocity = 0;
            if (view != null) view.localRotation = Quaternion.identity;
        }

        private void Awake()
        {
            controller = GetComponent<CharacterController>();
            vitals = GetComponent<CityPlayerVitals>();
            if (view == null)
                view = Camera.main != null ? Camera.main.transform : null;
        }

        private void Update()
        {
            bool touchActive = touch != null && touch.IsVisible;
            if (view == null || Time.deltaTime <= 0f || (canAct != null && !canAct()) || (vitals != null && !vitals.IsAlive))
            {
                if (touch != null) touch.ResetInput();
                return;
            }
            bool desktopInput = Cursor.lockState == CursorLockMode.Locked && Keyboard.current != null;
            if (!desktopInput && !touchActive) return;
            Keyboard keyboard = Keyboard.current;
            Vector2 moveInput = desktopInput ? new Vector2(
                (keyboard.dKey.isPressed ? 1f : 0f) - (keyboard.aKey.isPressed ? 1f : 0f),
                (keyboard.wKey.isPressed ? 1f : 0f) - (keyboard.sKey.isPressed ? 1f : 0f)) : Vector2.zero;
            if (touchActive) moveInput += touch.MovementVector;
            moveInput = Vector2.ClampMagnitude(moveInput, 1f);
            Vector2 look = touchActive ? touch.ConsumeLook() * 0.15f : Vector2.zero;
            if (desktopInput && Mouse.current != null) look += Mouse.current.delta.ReadValue() * mouseSensitivity;
            pitch = Mathf.Clamp(pitch - look.y, -82f, 82f);
            view.localRotation = Quaternion.Euler(pitch, 0f, 0f);
            transform.Rotate(Vector3.up * look.x);
            bool wantsJump = (desktopInput && keyboard.spaceKey.wasPressedThisFrame) | (touchActive && touch.ConsumeJump());
            bool grounded = controller.isGrounded;
            if (grounded && verticalVelocity < 0f)
                verticalVelocity = -2f;
            if (grounded && wantsJump)
                verticalVelocity = Mathf.Sqrt(jumpHeight * -2f * gravity);

            verticalVelocity += gravity * Time.deltaTime;
            float speed = ((desktopInput && keyboard.leftShiftKey.isPressed) || (touchActive && touch.Sprint)) ? sprintSpeed : walkSpeed;
            Vector3 direction = transform.right * moveInput.x + transform.forward * moveInput.y;
            controller.Move((direction * speed + Vector3.up * verticalVelocity) * Time.deltaTime);
        }
    }
}


