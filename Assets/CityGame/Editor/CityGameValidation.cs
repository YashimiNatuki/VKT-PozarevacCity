using System;
using System.IO;
using System.Globalization;
using CesiumForUnity;
using Pozarevac.CityGame;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;

namespace Pozarevac.CityGame.Editor
{
    [InitializeOnLoad]
    public static class CityGameValidation
    {
        private const string StageKey = "Pozarevac.Validation.Stage";
        private const string DeadlineKey = "Pozarevac.Validation.Deadline";
        private const string ErrorKey = "Pozarevac.Validation.Error";
        private const string ScenarioKey = "Pozarevac.Validation.Scenario";

        static CityGameValidation()
        {
            int stage = SessionState.GetInt(StageKey, 0);
            if (stage >= 100)
                EditorApplication.delayCall += ExitCompletedRun;
            else if (stage > 0)
            {
                EditorApplication.update += Tick;
                Application.logMessageReceived += CaptureError;
            }
        }

        public static void RuntimeSmoke()
        {
            if (EditorSceneManager.GetActiveScene().isDirty)
                throw new InvalidOperationException("Save or discard existing scene changes before running validation.");
            string scenario = Argument("-cityScenario", "fixture");
            if (scenario == "actual")
                EditorSceneManager.OpenScene("Assets/Scenes/SampleScene.unity", OpenSceneMode.Single);
            else
            {
                EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
                new GameObject("ValidationGeoreference").AddComponent<CesiumGeoreference>();
                GameObject floor = GameObject.CreatePrimitive(PrimitiveType.Cube);
                floor.name = "ValidationWalkableFloor";
                floor.transform.position = new Vector3(0f, -0.5f, 20f);
                floor.transform.localScale = new Vector3(100f, 1f, 100f);
                Camera camera = new GameObject("DynamicCamera").AddComponent<Camera>();
                camera.tag = "MainCamera";
                new GameObject("ValidationSun").AddComponent<Light>().type = LightType.Directional;
                new GameObject("CityGame").AddComponent<CityGameBootstrap>();
            }
            SessionState.SetInt(StageKey, 1);
            SessionState.SetString(ScenarioKey, scenario);
            SessionState.SetString(ErrorKey, string.Empty);
            SessionState.SetFloat(DeadlineKey, (float)EditorApplication.timeSinceStartup + 90f);
            EditorApplication.update -= Tick;
            EditorApplication.update += Tick;
            Application.logMessageReceived -= CaptureError;
            Application.logMessageReceived += CaptureError;
            EditorApplication.EnterPlaymode();
        }

        private static void Tick()
        {
            try
            {
                string error = SessionState.GetString(ErrorKey, string.Empty);
                if (error.Length > 0) throw new InvalidOperationException(error);
                if (EditorApplication.timeSinceStartup >= SessionState.GetFloat(DeadlineKey, 0))
                    throw new TimeoutException("Runtime validation exceeded its deadline.");
                if (!EditorApplication.isPlaying || EditorApplication.isCompiling) return;
                CityGameBootstrap game = UnityEngine.Object.FindAnyObjectByType<CityGameBootstrap>();
                if (game == null || !game.WorldReady) return;
                if (SessionState.GetString(ScenarioKey, "") == "video")
                {
                    Ensure(game.Intro != null && game.Intro.IsActive, "Intro did not enter active playback.");
                    if (!game.Intro.HasStarted || game.Intro.DecodedFrame < 3) return;
                    string metadata = $"Intro decoded frame {game.Intro.DecodedFrame}; {game.Intro.Width}x{game.Intro.Height}; length {game.Intro.Length:0.0}s.";
                    Ensure(game.Director.IsPaused, "Gameplay was not paused behind the intro.");
                    game.Intro.Skip();
                    Ensure(!game.Director.IsPaused, "Skipping the intro did not resume gameplay.");
                    Finish(true, metadata + " Skip resumed gameplay.");
                    return;
                }
                CityMissionDirector director = game.Director;
                int stage = SessionState.GetInt(StageKey, 0);
                if (stage == 1)
                {
                    Ensure(director != null && game.Player != null, "Runtime references were not initialized.");
                    int activeListeners = 0;
                    foreach (AudioListener listener in UnityEngine.Object.FindObjectsByType<AudioListener>(FindObjectsInactive.Exclude))
                        if (listener.isActiveAndEnabled) activeListeners++;
                    Ensure(activeListeners == 1 && game.Player.GetComponentInChildren<AudioListener>().isActiveAndEnabled,
                        "The active camera must own the only enabled audio listener.");
                    game.Player.GetComponent<CityPlayerController>().enabled = false;
                    Ensure(!director.TryNextContract(), "Next contract unlocked before completion.");
                    if (Argument("-cityChallenges", "false") == "true")
                    {
                        Ensure(director.TrySelectChallenge(CityMissionChallenge.Express), "Express could not be selected before pickup.");
                        Ensure(director.Run.RemainingSeconds == 72 && director.Run.EffectiveCashReward == 900,
                            "Express conditions were not applied.");
                    }
                    Visit(game, 0);
                    Ensure(!director.TrySelectChallenge(CityMissionChallenge.Ghost), "Active challenge was mutable.");
                    director.SetPaused(true);
                    Ensure(!director.TrySelectChallenge(CityMissionChallenge.Standard), "Paused challenge was mutable.");
                    SessionState.SetString("Pozarevac.Validation.PausedTime", director.Run.RemainingSeconds.ToString("R", CultureInfo.InvariantCulture));
                    SessionState.SetFloat("Pozarevac.Validation.ResumeAt", (float)EditorApplication.timeSinceStartup + 2f);
                    SessionState.SetInt(StageKey, 2);
                    return;
                }
                if (stage == 3)
                {
                    if (EditorApplication.timeSinceStartup < SessionState.GetFloat("Pozarevac.Validation.ResumeAt", 0)) return;
                    CityProgression p = game.Progression;
                    Ensure(p.GrowSeconds == 30 && director.IsPaused, "Phone did not freeze game-time growth.");
                    CityProgressionController controller = game.GetComponent<CityProgressionController>();
                    controller.ClosePanel();
                    Ensure(!director.IsPaused, "Closing the phone did not resume gameplay.");
                    p.Tick(30);
                    Ensure(controller.Perform(p.Harvest) && controller.Perform(p.Sell), "Harvest and trade failed.");
                    Ensure(controller.Perform(p.Recruit) && controller.Perform(p.Recruit), "Crew recruitment failed.");
                    for (int i = 0; i < 3; i++) Ensure(controller.Perform(p.ClaimTerritory), "District claim failed.");
                    int expectedCash = Argument("-cityChallenges", "false") == "true" ? 3465 : 2915;
                    Ensure(p.Session.Cash == expectedCash && p.Territories == 3, "Economy campaign balance mismatch.");
                    CitySaveStore saved = new CitySaveStore(controller.SavePath);
                    Ensure(saved.TryLoad(out CityProfileData profile, out _), "Runtime profile was not durably saved.");
                    CitySession restored = CitySession.Restore(profile);
                    Ensure(profile.completedContracts == 3 && profile.crew == 2 && profile.territories == 3 && restored.Cash == expectedCash,
                        "Runtime save restore lost completed progression.");
                    Ensure(!CityMissionRun.RestoreCompleted(CityMissionDirector.CreateContracts()[2], restored).TryVisit(2) && restored.Cash == expectedCash,
                        "Restored completed route paid twice.");
                    if (Argument("-cityHideout", "false") == "true") VerifyHideout(game);
                    Finish(true, "Three contracts, nine raycast interactions, phone pause, growth/trade, crew, three districts and durable profile restore passed. Challenges=" + Argument("-cityChallenges", "false") + "; hideout=" + Argument("-cityHideout", "false") + "; campaign cash=" + expectedCash);
                    return;
                }
                if (stage != 2 || EditorApplication.timeSinceStartup < SessionState.GetFloat("Pozarevac.Validation.ResumeAt", 0)) return;
                Ensure(director.Run.RemainingSeconds == double.Parse(SessionState.GetString("Pozarevac.Validation.PausedTime", "-1"), CultureInfo.InvariantCulture), "Mission timer advanced while paused.");
                Ensure(!game.Player.GetComponent<CityPlayerInteraction>().TryInteract(), "Interaction succeeded while paused.");
                director.SetPaused(false);
                Ensure(Vector3.Distance(game.Player.GetComponentInChildren<Camera>().transform.localPosition, new Vector3(0, 1.62f, 0)) < 0.01f,
                    "Camera globe components moved the view away from the player during elapsed frames.");
                Visit(game, 1);
                Visit(game, 2);
                Ensure(director.Run.State == CityMissionState.Completed, "First contract did not complete.");
                Ensure(!director.TryInteract(2), "Completed contract paid twice.");
                for (int contract = 1; contract < 3; contract++)
                {
                    Ensure(director.TryNextContract(), "Next contract did not unlock.");
                    Ensure(director.Run.Challenge == CityMissionChallenge.Standard, "New route inherited an old challenge.");
                    if (contract == 1 && Argument("-cityChallenges", "false") == "true")
                        Ensure(director.TrySelectChallenge(CityMissionChallenge.Ghost), "Ghost could not be selected.");
                    Ensure(!director.TryInteract(2), "Out-of-order objective advanced.");
                    Visit(game, 0); Visit(game, 1); Visit(game, 2);
                }
                Ensure(game.Session.Cash == (Argument("-cityChallenges", "false") == "true" ? 6150 : 5600) && game.Session.Reputation == 55, "Campaign rewards did not match the three contracts.");
                Ensure(director.Run.Cargo == 0 && !director.TryNextContract(), "Campaign did not terminate cleanly.");
                Ensure(game.Progression.CompletedContracts == 3 && game.Progression.PhoneUnlocked, "Mission progression did not unlock the phone.");
                CityProgressionController progression = game.GetComponent<CityProgressionController>();
                Ensure(progression.Perform(game.Progression.BuySeed) && progression.Perform(game.Progression.Plant), "Economy did not unlock after the route.");
                progression.OpenPanel();
                Ensure(director.IsPaused, "Opening the phone did not pause gameplay.");
                SessionState.SetFloat("Pozarevac.Validation.ResumeAt", (float)EditorApplication.timeSinceStartup + 1.5f);
                SessionState.SetInt(StageKey, 3);
            }
            catch (Exception exception)
            {
                Finish(false, exception.Message);
            }
        }

        private static void Visit(CityGameBootstrap game, int index)
        {
            CityMission station = game.Director.StationAt(index);
            CharacterController controller = game.Player.GetComponent<CharacterController>();
            controller.enabled = false;
            game.Player.position = station.transform.position + new Vector3(0f, -0.73f, -2.5f);
            controller.enabled = true;
            Camera view = game.Player.GetComponentInChildren<Camera>();
            view.transform.LookAt(station.transform.position);
            Physics.SyncTransforms();
            bool interacted = game.Player.GetComponent<CityPlayerInteraction>().TryInteract();
            if (!interacted)
            {
                bool rayHit = Physics.Raycast(view.transform.position, view.transform.forward, out RaycastHit hit, 4f,
                    Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore);
                string collision = rayHit ? $"{hit.collider.name} at {hit.distance:0.00}m; mission={hit.collider.GetComponentInParent<CityMission>() != null}" : "no collider hit";
                throw new InvalidOperationException($"Station {index + 1} interaction failed: {collision}; " +
                    $"state={game.Director.Run.State}, index={game.Director.Run.ObjectiveIndex}, paused={game.Director.IsPaused}, scale={Time.timeScale}, " +
                    $"camera={view.transform.position}, target={station.transform.position}, direction={view.transform.forward}");
            }
        }

        private static void VerifyHideout(CityGameBootstrap game)
        {
            Ensure(game.Hideout != null && !game.Hideout.IsInside, "Hideout was not composed.");
            InteractWith(game, game.Hideout.Entrance, 0.9f);
            Ensure(game.Hideout.IsInside, "Door did not enter the hideout.");
            int cashBefore = game.Session.Cash;
            for (int i = 0; i < CityGarden.PlotCount; i++)
            {
                Ensure(game.GetComponent<CityProgressionController>().Perform(() => game.Progression.Garden.BuySeed(0)), "Catalog seed purchase failed.");
                InteractWith(game, game.Hideout.PlotTransform(i), 0.4f);
                Ensure(game.Progression.Garden.Plot(i).planted, "Physical plot did not plant.");
                InteractWith(game, game.Hideout.PlotTransform(i), 0.4f);
                Ensure(game.Progression.Garden.Plot(i).moistureSeconds > 7100, "Physical plot did not water.");
            }
            Ensure(game.Session.Cash == cashBefore - 240, "Garden seed costs were incorrect.");
            CityProgressionController controller = game.GetComponent<CityProgressionController>();
            CitySaveStore saved = new CitySaveStore(controller.SavePath);
            Ensure(saved.TryLoad(out CityProfileData profile, out _), "Garden profile was not saved.");
            Ensure(profile.garden.plots[3].planted && profile.garden.plots[3].moistureSeconds > 7100, "Garden restore lost care state.");
            InteractWith(game, game.Hideout.Exit, 1.05f);
            Ensure(!game.Hideout.IsInside && game.Player.position.y < game.Hideout.Exit.position.y - 900, "Exit failed to return to the city.");
            Ensure(game.Vitals.ApplyDamage(100) && !game.Vitals.IsAlive, "Fatal damage did not enter dead state.");
            Ensure(!game.Player.GetComponent<CityPlayerInteraction>().TryInteract(), "Dead player could interact.");
            Ensure(game.Vitals.Respawn() && game.Vitals.Health.Current == 100, "Hospital respawn failed.");
            Ensure(!game.Vitals.ApplyDamage(10), "Respawn protection failed.");
        }

        private static void InteractWith(CityGameBootstrap game, Transform target, float footOffset)
        {
            CharacterController body = game.Player.GetComponent<CharacterController>(); body.enabled = false;
            game.Player.position = target.position + new Vector3(0, -footOffset, target == game.Hideout.Exit ? 2.5f : -2.5f); body.enabled = true;
            game.Player.GetComponentInChildren<Camera>().transform.LookAt(target.position);
            Physics.SyncTransforms();
            Ensure(game.Player.GetComponent<CityPlayerInteraction>().TryInteract(), "Hideout raycast failed for " + target.name);
        }

        private static void CaptureError(string condition, string stackTrace, LogType type)
        {
            if ((type == LogType.Error || type == LogType.Exception || type == LogType.Assert) &&
                (stackTrace.Contains("Pozarevac.CityGame") || condition.StartsWith("CityGame", StringComparison.Ordinal)))
                SessionState.SetString(ErrorKey, "A CityGame runtime error was logged; inspect the Unity log.");
        }

        private static void Finish(bool passed, string details)
        {
            EditorApplication.update -= Tick;
            Application.logMessageReceived -= CaptureError;
            string path = Argument("-cityReport", Path.Combine(Path.GetTempPath(), "pozarevac-runtime-report.json"));
            Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(path)));
            File.WriteAllText(path, JsonUtility.ToJson(new RuntimeReport
            {
                passed = passed,
                scenario = SessionState.GetString(ScenarioKey, "unknown"),
                details = details
            }, true));
            Debug.Log(passed ? "CITY_GAME_VALIDATION_PASS" : "CITY_GAME_VALIDATION_FAIL");
            SessionState.SetInt(StageKey, passed ? 100 : 101);
            if (EditorApplication.isPlaying) EditorApplication.ExitPlaymode();
            EditorApplication.delayCall += ExitCompletedRun;
        }

        private static void ExitCompletedRun()
        {
            int code = SessionState.GetInt(StageKey, 101) == 100 ? 0 : 1;
            SessionState.EraseInt(StageKey);
            EditorApplication.Exit(code);
        }

        public static void BuildWindows()
        {
            PlayerSettings.productName = "VKT - VelikaKradjaTockova";
            // ArcGIS's installed occlusion renderer reads defaultRenderPipeline directly.
            // Keep that setting aligned with the already selected Quality pipeline.
            if (GraphicsSettings.defaultRenderPipeline == null && QualitySettings.renderPipeline != null)
            {
                GraphicsSettings.defaultRenderPipeline = QualitySettings.renderPipeline;
                AssetDatabase.SaveAssets();
                Debug.Log("VKT_RENDER_PIPELINE: default pipeline aligned with the active Quality pipeline.");
            }
            EnsureMarkerMaterial();
            string output = Path.GetFullPath(Argument("-cityBuild", Path.Combine(Path.GetTempPath(), "PozarevacBuild", "Pozarevac.exe")));
            if (output.StartsWith(Path.GetFullPath(Application.dataPath) + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase))
                throw new InvalidOperationException("Build output must be outside Assets.");
            Directory.CreateDirectory(Path.GetDirectoryName(output));
            BuildReport report = BuildPipeline.BuildPlayer(new BuildPlayerOptions
            {
                scenes = new[] { "Assets/Scenes/SampleScene.unity" },
                locationPathName = output,
                target = BuildTarget.StandaloneWindows64,
                options = BuildOptions.Development | (Array.Exists(Environment.GetCommandLineArgs(), item => item == "-cityScriptsOnly")
                    ? BuildOptions.BuildScriptsOnly : BuildOptions.None)
            });
            Debug.Log($"CITY_BUILD_RESULT {report.summary.result}; errors={report.summary.totalErrors}; size={report.summary.totalSize}");
            if (report.summary.result != BuildResult.Succeeded)
                throw new InvalidOperationException("Windows build failed. Inspect the build report log.");
        }

        public static void EnsureMarkerMaterial()
        {
            const string resourceFolder = "Assets/CityGame/Resources";
            const string assetPath = resourceFolder + "/CityMissionMarker.mat";
            if (!AssetDatabase.IsValidFolder(resourceFolder)) AssetDatabase.CreateFolder("Assets/CityGame", "Resources");
            Material material = AssetDatabase.LoadAssetAtPath<Material>(assetPath);
            if (material == null)
            {
                Shader shader = Shader.Find("Universal Render Pipeline/Lit");
                if (shader == null) throw new InvalidOperationException("URP Lit shader was not found in the project.");
                material = new Material(shader);
                material.color = Color.cyan;
                material.EnableKeyword("_EMISSION");
                material.SetColor("_EmissionColor", Color.cyan * 1.8f);
                AssetDatabase.CreateAsset(material, assetPath);
                AssetDatabase.SaveAssets();
            }
        }

        private static string Argument(string name, string fallback)
        {
            string[] arguments = Environment.GetCommandLineArgs();
            for (int i = 0; i + 1 < arguments.Length; i++)
                if (arguments[i] == name) return arguments[i + 1];
            return fallback;
        }

        private static void Ensure(bool condition, string message)
        {
            if (!condition) throw new InvalidOperationException(message);
        }

        [Serializable]
        private sealed class RuntimeReport
        {
            public bool passed;
            public string scenario;
            public string details;
        }
    }
}
