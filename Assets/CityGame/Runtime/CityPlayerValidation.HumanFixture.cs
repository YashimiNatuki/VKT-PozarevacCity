#if DEVELOPMENT_BUILD || UNITY_EDITOR
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.UI;

namespace Pozarevac.CityGame
{
    public sealed partial class CityPlayerValidation
    {
        // Shared lifetime for native human scenarios. Never installed during ordinary gameplay.
        private sealed class HumanValidationScope : IDisposable
        {
            private readonly CityPlayerController mover;
            private readonly bool movementEnabled, cursorVisible;
            private readonly CursorLockMode cursor;
            private readonly HashSet<Camera> cameras;
            private readonly HashSet<AudioListener> listeners;
            private readonly HashSet<CharacterController> controllers;
            private Keyboard keyboard;
            private bool disposed;

            public HumanValidationScope(CityPlayerController mover)
            {
                this.mover = mover != null ? mover : throw new ArgumentNullException(nameof(mover));
                movementEnabled = mover.enabled;
                cursor = Cursor.lockState; cursorVisible = Cursor.visible;
                cameras = new HashSet<Camera>(UnityEngine.Object.FindObjectsByType<Camera>(FindObjectsInactive.Include, FindObjectsSortMode.None));
                listeners = new HashSet<AudioListener>(UnityEngine.Object.FindObjectsByType<AudioListener>(FindObjectsInactive.Include, FindObjectsSortMode.None));
                controllers = new HashSet<CharacterController>(mover.GetComponentsInChildren<CharacterController>(true));
            }

            public bool OwnersUnchanged => !disposed && mover != null &&
                cameras.SetEquals(UnityEngine.Object.FindObjectsByType<Camera>(FindObjectsInactive.Include, FindObjectsSortMode.None)) &&
                listeners.SetEquals(UnityEngine.Object.FindObjectsByType<AudioListener>(FindObjectsInactive.Include, FindObjectsSortMode.None)) &&
                controllers.SetEquals(mover.GetComponentsInChildren<CharacterController>(true));

            public Keyboard CreateKeyboard()
            {
                if (disposed) throw new ObjectDisposedException(nameof(HumanValidationScope));
                return keyboard ??= CityInputFlowValidation.CreateValidationKeyboard();
            }

            public void Dispose()
            {
                if (disposed) return;
                disposed = true;
                try
                {
                    if (keyboard != null)
                    {
                        InputSystem.QueueStateEvent(keyboard, new KeyboardState());
                        InputSystem.RemoveDevice(keyboard);
                        keyboard = null;
                    }
                }
                finally
                {
                    if (mover != null) { mover.ResetView(); mover.enabled = movementEnabled; }
                    Cursor.lockState = cursor; Cursor.visible = cursorVisible;
                }
            }
        }

        private sealed class HumanProfileCheckpoint
        {
            private readonly CityGameBootstrap game;
            private readonly string path, signature, disk;

            public HumanProfileCheckpoint(CityGameBootstrap game)
            {
                this.game = game;
                var progress = game.GetComponent<CityProgressionController>();
                progress.SaveNow();
                path = progress.SavePath;
                signature = PreviewProfileSignature(game.Progression.Snapshot());
                disk = File.ReadAllText(path);
            }

            public bool IsUnchanged => signature == PreviewProfileSignature(game.Progression.Snapshot()) && disk == File.ReadAllText(path);
        }

        private static string PreviewProfileSignature(CityProfileData profile)
        {
            // Garden UTC progress is allowed; comparison must not mutate the caller's snapshot.
            CityProfileData copy = JsonUtility.FromJson<CityProfileData>(JsonUtility.ToJson(profile));
            if (copy.garden != null)
            {
                copy.garden.lastUtcSeconds = 0;
                if (copy.garden.plots != null)
                    foreach (CityGardenPlotData plot in copy.garden.plots)
                        if (plot != null) { plot.grownSeconds = 0; plot.moistureSeconds = 0; }
            }
            return JsonUtility.ToJson(copy);
        }

        private static Button HumanButton(string name) => Array.Find(UnityEngine.Object.FindObjectsByType<Button>(FindObjectsSortMode.None), button => button.name == name);

        private static IEnumerator HumanPress(Keyboard keys, Key key)
        {
            InputSystem.QueueStateEvent(keys, new KeyboardState(key)); yield return null; yield return null;
            InputSystem.QueueStateEvent(keys, new KeyboardState()); yield return null; yield return null;
        }

        private static SkinnedMeshRenderer HumanBodySkin(GameObject root)
        {
            if (root == null) return null;
            SkinnedMeshRenderer body = null;
            foreach (var skin in root.GetComponentsInChildren<SkinnedMeshRenderer>(true))
                if (HumanFaceShape(skin, "eyeBlinkLeft") >= 0 && HumanFaceShape(skin, "jawOpen") >= 0 &&
                    (body == null || skin.sharedMesh.vertexCount > body.sharedMesh.vertexCount)) body = skin;
            return body;
        }

        private static int HumanFaceShape(SkinnedMeshRenderer skin, string suffix)
        {
            if (skin == null || skin.sharedMesh == null) return -1;
            int found = -1;
            for (int i = 0; i < skin.sharedMesh.blendShapeCount; i++)
                if (skin.sharedMesh.GetBlendShapeName(i).EndsWith(suffix, StringComparison.OrdinalIgnoreCase))
                { if (found >= 0) return -1; found = i; }
            return found;
        }
    }
}
#endif
