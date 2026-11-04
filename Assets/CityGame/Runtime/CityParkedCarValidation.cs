#if DEVELOPMENT_BUILD || UNITY_EDITOR
using System;
using System.Collections;
using System.IO;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.UI;

namespace Pozarevac.CityGame
{
    // Runs only with both -vktSmoke and -vktCarFlow in an isolated development profile.
    public sealed class CityParkedCarValidation : MonoBehaviour
    {
        [Serializable]
        private sealed class Report
        {
            public bool passed;
            public bool outdoorEntered, outdoorDrove, outdoorExited;
            public bool spawned, entered, drove, paused, stoppedAtWall, reverseSteering, blockedExits, safeExit, walkingRestored, checkpointExit;
            public bool touchControls;
            public float outdoorDriveMeters, drivenMeters, recordedMeters, wallClearance, reverseMeters, reverseTurnDegrees, walkedMeters;
            public string parkingStreet = "";
            public string error = "";
            public string input = "Synthetic Unity input against controlled geometry. Physical touch review is separate.";
        }

        private readonly Report report = new Report();
        private Keyboard keyboard;
        private Button touchUse;
        private CityTouchHold touchForward, touchReverse, touchRight;
        private PointerEventData pointer;

        public void Initialize(CityGameBootstrap game)
        {
            if (!CityGameBootstrap.IsSmokeValidation || Array.IndexOf(Environment.GetCommandLineArgs(), "-vktCarFlow") < 0) return;
            Application.runInBackground = true;
            StartCoroutine(Verify(game));
        }

        private IEnumerator Verify(CityGameBootstrap game)
        {
            game.Intro?.Skip();
            yield return new WaitForSecondsRealtime(.25f);
            var car = game.Car;
            var player = game.Player;
            var body = player.GetComponent<CharacterController>();
            var camera = player.GetComponentInChildren<CityCameraRig>();
            var interaction = player.GetComponent<CityPlayerInteraction>();
            var phone = game.GetComponent<CityProgressionController>();
            report.spawned = car != null && car.gameObject.activeInHierarchy && !car.IsOccupied;
            report.parkingStreet = car != null ? car.ParkingStreet : "";
            if (!report.spawned || body == null || camera == null || interaction == null || phone == null || game.Director.IsPaused)
            {
                Finish("World did not expose a playable parked car and active player.");
                yield break;
            }
            if (string.IsNullOrWhiteSpace(report.parkingStreet))
            {
                Finish("The parked car was not placed on a named street from the map.");
                yield break;
            }

            report.touchControls = game.Touch != null && game.Touch.IsVisible;
            if (report.touchControls)
            {
                var touchRoot = game.transform.Find("VKT_Touch");
                touchUse = Array.Find(touchRoot.GetComponentsInChildren<Button>(), button => button.name == "KORISTI");
                var holds = touchRoot.GetComponentsInChildren<CityTouchHold>();
                touchForward = Array.Find(holds, button => button.name == "↑");
                touchReverse = Array.Find(holds, button => button.name == "↓");
                touchRight = Array.Find(holds, button => button.name == "→");
                if (touchUse == null || touchForward == null || touchReverse == null || touchRight == null || EventSystem.current == null)
                {
                    Finish("The touch action and driving controls were not available.");
                    yield break;
                }
                pointer = new PointerEventData(EventSystem.current) { pointerId = 711 };
            }
            else
                keyboard = CityInputFlowValidation.CreateValidationKeyboard();

            PlaceBesideCar(car.transform, player, body, camera);
            Use(interaction);
            yield return null;
            report.outdoorEntered = car.IsOccupied && !body.enabled;
            if (!report.outdoorEntered)
            {
                Finish("The parked car at its authored street location could not be entered.");
                yield break;
            }
            Vector3 outdoorStart = car.transform.position;
            SetForward(true);
            yield return new WaitForSecondsRealtime(.5f);
            report.outdoorDriveMeters = HorizontalDistance(car.transform.position, outdoorStart);
            report.outdoorDrove = report.outdoorDriveMeters > .08f;
            if (report.outdoorDrove) Capture();
            SetForward(false);
            yield return null;
            float stopDeadline = Time.realtimeSinceStartup + 2.5f;
            while (Mathf.Abs(car.Speed) > .08f && Time.realtimeSinceStartup < stopDeadline) yield return null;
            yield return TryOccupiedAction(interaction);
            report.outdoorExited = !car.IsOccupied && body.enabled;
            if (!report.outdoorDrove || !report.outdoorExited)
            {
                Finish("The authored street location did not support a short drive and normal exit.");
                yield break;
            }

            // Exercise the real car, interaction ray and physics on a clean, controlled surface.
            Vector3 fixture = player.position + Vector3.up * 12f;
            Obstacle("VKT_CarTestFloor", fixture + Vector3.down * .25f, new Vector3(36f, .5f, 36f));
            car.transform.SetPositionAndRotation(fixture, Quaternion.identity);
            PlaceBesideCar(car.transform, player, body, camera);
            Use(interaction);
            yield return null;
            report.entered = car.IsOccupied && !body.enabled;
            if (!report.entered)
            {
                Finish("The real interaction ray did not put the player in the car.");
                yield break;
            }

            Vector3 start = car.transform.position;
            Vector3 wallPosition = start + Vector3.forward * 6f;
            Obstacle("VKT_CarTestWall", wallPosition + Vector3.up * 1.25f, new Vector3(5f, 2.5f, .4f));
            Physics.SyncTransforms();
            var stats = game.Progression.Stats;
            float riddenBefore = stats.TotalDistanceRidden;
            float walkedBefore = stats.TotalDistanceWalked;
            float sprintedBefore = stats.TotalDistanceSprinted;
            SetForward(true);
            yield return new WaitForSecondsRealtime(.8f);
            report.drivenMeters = HorizontalDistance(car.transform.position, start);
            report.recordedMeters = stats.TotalDistanceRidden - riddenBefore;
            report.drove = report.drivenMeters > .45f && Mathf.Abs(report.drivenMeters - report.recordedMeters) < .12f
                && Mathf.Abs(stats.TotalDistanceWalked - walkedBefore) < .01f
                && Mathf.Abs(stats.TotalDistanceSprinted - sprintedBefore) < .01f;
            if (!report.drove)
            {
                Finish("Driving did not move the car and record exactly its distance as riding.");
                yield break;
            }
            phone.OpenPanel();
            Vector3 pauseAt = car.transform.position;
            float pausedMeters = stats.TotalDistanceRidden;
            yield return new WaitForSecondsRealtime(.3f);
            report.paused = phone.IsPhoneOpen && game.Director.IsPaused && car.IsOccupied
                && HorizontalDistance(pauseAt, car.transform.position) < .001f && stats.TotalDistanceRidden == pausedMeters;
            SetForward(false);
            phone.ClosePanel();
            yield return null;
            if (!report.paused || game.Director.IsPaused)
            {
                Finish("The real phone pause did not freeze and resume the occupied car.");
                yield break;
            }

            SetForward(true);
            yield return new WaitForSecondsRealtime(2f);
            report.wallClearance = wallPosition.z - car.transform.position.z;
            report.stoppedAtWall = report.wallClearance > 1.9f && report.wallClearance < 2.8f && Mathf.Abs(car.Speed) < .2f;
            SetForward(false);
            yield return null;
            if (!report.stoppedAtWall)
            {
                Finish("The car did not stop at the physical wall.");
                yield break;
            }

            Vector3 reverseStart = car.transform.position;
            Vector3 forwardBeforeReverse = car.transform.forward;
            // First clear the wall before asking the collision solver to reserve turning space.
            SetReverse(true, false);
            yield return new WaitForSecondsRealtime(.4f);
            SetReverse(true, true);
            yield return new WaitForSecondsRealtime(.65f);
            report.reverseMeters = Vector3.Dot(reverseStart - car.transform.position, forwardBeforeReverse);
            report.reverseTurnDegrees = Vector3.SignedAngle(forwardBeforeReverse, car.transform.forward, Vector3.up);
            report.reverseSteering = car.Speed < -.1f && report.reverseMeters > .3f && report.reverseTurnDegrees < -8f;
            SetReverse(false, false);
            yield return new WaitForSecondsRealtime(.55f);
            if (!report.reverseSteering)
            {
                Finish("Reverse input did not move the car backward and steer it away from the wall.");
                yield break;
            }

            Vector3 left = car.transform.TransformPoint(Vector3.left * 1.65f);
            Vector3 right = car.transform.TransformPoint(Vector3.right * 1.65f);
            Vector3 rear = car.transform.TransformPoint(Vector3.back * 2.65f);
            GameObject leftBlock = BlockExit(left);
            GameObject rightBlock = BlockExit(right);
            GameObject rearBlock = BlockExit(rear);
            Physics.SyncTransforms();
            yield return TryOccupiedAction(interaction);
            report.blockedExits = car.IsOccupied && !body.enabled;
            if (!report.blockedExits)
            {
                Finish("A blocked exit released the occupant.");
                yield break;
            }

            Destroy(leftBlock);
            yield return null;
            Physics.SyncTransforms();
            float exitRidden = stats.TotalDistanceRidden;
            float exitWalked = stats.TotalDistanceWalked;
            yield return TryOccupiedAction(interaction);
            report.safeExit = !car.IsOccupied && body.enabled && camera.Mode == CityCameraMode.FirstPerson
                && HorizontalDistance(player.position, left) < .3f
                && stats.TotalDistanceRidden == exitRidden && Mathf.Abs(stats.TotalDistanceWalked - exitWalked) < .01f;
            Destroy(rightBlock);
            Destroy(rearBlock);
            yield return null;
            if (!report.safeExit)
            {
                Finish("Opening the first side did not safely restore the player, camera and movement accounting.");
                yield break;
            }

            player.rotation = Quaternion.LookRotation(-car.transform.forward);
            player.GetComponent<CityPlayerController>().ResetView();
            Vector3 walkingStart = player.position;
            SetForward(true);
            yield return new WaitForSecondsRealtime(.5f);
            SetForward(false);
            yield return null;
            report.walkedMeters = HorizontalDistance(player.position, walkingStart);
            report.walkingRestored = report.walkedMeters > .55f && stats.TotalDistanceWalked > exitWalked + .55f
                && stats.TotalDistanceRidden == exitRidden;
            if (!report.walkingRestored)
            {
                Finish("Walking did not resume after getting out.");
                yield break;
            }

            PlaceBesideCar(car.transform, player, body, camera);
            Use(interaction);
            yield return null;
            Vector3 carBeforeCheckpoint = car.transform.position;
            bool returned = car.IsOccupied && game.Vitals.ReturnToCheckpoint();
            yield return null;
            report.checkpointExit = returned && !car.IsOccupied && body.enabled && camera.Mode == CityCameraMode.FirstPerson
                && Vector3.Distance(player.position, car.transform.position) > 5f
                && Vector3.Distance(carBeforeCheckpoint, car.transform.position) < .01f;
            Finish(report.checkpointExit ? "" : "The occupied car did not release safely at checkpoint return.");
        }

        private static void PlaceBesideCar(Transform car, Transform player, CharacterController body, CityCameraRig camera)
        {
            camera.SetMode(CityCameraMode.FirstPerson);
            Vector3 position = car.position + car.transform.right * 2.65f + Vector3.up * .02f;
            if (new CityGroundProbe().TryFindSurface(position, out Vector3 ground) && Mathf.Abs(position.y - ground.y) < .6f)
                position = ground + Vector3.up * .02f;
            Quaternion facing = Quaternion.LookRotation(-car.transform.right);
            body.enabled = false;
            player.SetPositionAndRotation(position, facing);
            body.enabled = true;
            player.GetComponent<CityPlayerController>().ResetView();
            camera.transform.rotation = Quaternion.LookRotation(car.position + Vector3.up * .75f - (position + Vector3.up * 1.62f));
            Physics.SyncTransforms();
        }

        private void Use(CityPlayerInteraction interaction)
        {
            if (touchUse != null)
                ExecuteEvents.Execute(touchUse.gameObject, new BaseEventData(EventSystem.current), ExecuteEvents.submitHandler);
            else
                interaction.TryInteract();
        }

        private IEnumerator TryOccupiedAction(CityPlayerInteraction interaction)
        {
            if (keyboard == null)
                Use(interaction);
            else
                InputSystem.QueueStateEvent(keyboard, new KeyboardState(Key.E));
            yield return null;
            yield return null;
            if (keyboard != null) InputSystem.QueueStateEvent(keyboard, new KeyboardState());
            yield return null;
        }

        private void SetForward(bool held)
        {
            if (touchForward != null)
                SetTouchHold(touchForward, held);
            else if (keyboard != null)
                InputSystem.QueueStateEvent(keyboard, held ? new KeyboardState(Key.W) : new KeyboardState());
        }

        private void SetReverse(bool held, bool steerRight)
        {
            if (touchReverse != null)
            {
                SetTouchHold(touchReverse, held);
                SetTouchHold(touchRight, held && steerRight);
            }
            else if (keyboard != null)
                InputSystem.QueueStateEvent(keyboard, held
                    ? (steerRight ? new KeyboardState(Key.S, Key.D) : new KeyboardState(Key.S))
                    : new KeyboardState());
        }

        private void SetTouchHold(CityTouchHold control, bool held)
        {
            if (held) ExecuteEvents.Execute(control.gameObject, pointer, ExecuteEvents.pointerDownHandler);
            else ExecuteEvents.Execute(control.gameObject, pointer, ExecuteEvents.pointerUpHandler);
        }

        private static GameObject BlockExit(Vector3 position) =>
            Obstacle("VKT_CarTestExitBlock", position + Vector3.up * 1.35f, new Vector3(.9f, 2.4f, .9f));

        private static GameObject Obstacle(string name, Vector3 position, Vector3 size)
        {
            var obstacle = GameObject.CreatePrimitive(PrimitiveType.Cube);
            obstacle.name = name;
            obstacle.transform.position = position;
            obstacle.transform.localScale = size;
            return obstacle;
        }

        private static float HorizontalDistance(Vector3 a, Vector3 b) => new Vector2(a.x - b.x, a.z - b.z).magnitude;

        private static string Argument(string key, string fallback)
        {
            string[] arguments = Environment.GetCommandLineArgs();
            int index = Array.IndexOf(arguments, key);
            return index >= 0 && index + 1 < arguments.Length ? arguments[index + 1] : fallback;
        }

        private static void Capture()
        {
            string path = Argument("-vktCarCapture", "");
            if (path.Length == 0) return;
            Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(path)));
            ScreenCapture.CaptureScreenshot(path);
        }

        private void Finish(string error)
        {
            report.error = error;
            report.passed = error.Length == 0;
            string path = Argument("-vktCarReport", Path.Combine(Application.persistentDataPath, "vkt-car-flow.json"));
            Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(path)));
            File.WriteAllText(path, JsonUtility.ToJson(report, true));
            Debug.Log("VKT_CAR_FLOW " + (report.passed ? "PASS" : "FAIL: " + error));
            if (keyboard != null) InputSystem.RemoveDevice(keyboard);
            Application.Quit(report.passed ? 0 : 1);
        }
    }
}
#endif
