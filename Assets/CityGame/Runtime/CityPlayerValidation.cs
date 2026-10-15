#if DEVELOPMENT_BUILD || UNITY_EDITOR
using System;
using System.Collections;
using System.IO;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace Pozarevac.CityGame
{
    // Explicit development-player verification. Batch profiles are isolated from personal saves.
    public sealed class CityPlayerValidation : MonoBehaviour
    {
        [Serializable] private sealed class Report { public bool passed; public string details; public string product; }
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Install()
        {
            if (!Array.Exists(Environment.GetCommandLineArgs(), arg => arg == "-vktSmoke")) return;
            Application.runInBackground = true;
            Application.targetFrameRate = 30;
            new GameObject("VKTPlayerValidation").AddComponent<CityPlayerValidation>();
        }

        private IEnumerator Start()
        {
            float deadline = Time.realtimeSinceStartup + 90;
            CityGameBootstrap game = null;
            while (Time.realtimeSinceStartup < deadline)
            {
                game = FindAnyObjectByType<CityGameBootstrap>();
                if (game != null && game.WorldReady && (game.Intro == null || !game.Intro.IsActive)) break;
                yield return new WaitForSecondsRealtime(0.25f);
            }
            if (game == null || !game.WorldReady) { Finish(false, "City terrain did not become ready."); yield break; }
            yield return new WaitForSecondsRealtime(1);
            if (!Verify(game, out string details)) { Finish(false, details); yield break; }
            if (Array.Exists(Environment.GetCommandLineArgs(), arg => arg == "-vktEnhanced"))
            {
                if (!VerifyEnhanced(game, out string extra)) { Finish(false, extra); yield break; }
                details += " " + extra;
                CityMusicPlayer music = game.GetComponent<CityMusicPlayer>();
                if (music.LocalCount > 0)
                {
                    music.Next(); float audioDeadline = Time.realtimeSinceStartup + 20;
                    while (!music.IsPlaying && Time.realtimeSinceStartup < audioDeadline) yield return null;
                    if (!music.IsPlaying) { Finish(false, "Local audio did not decode and start within 20 seconds."); yield break; }
                    music.TogglePause(); details += " Local audio decoded and playback started.";
                }
            }
            yield return new WaitForSecondsRealtime(0.5f);
            if (Vector3.Distance(game.Player.GetComponentInChildren<Camera>().transform.localPosition, new Vector3(0, 1.62f, 0)) > 0.01f)
            { Finish(false, "Camera left the player's eye position after validation frames."); yield break; }
            if (Array.Exists(Environment.GetCommandLineArgs(), arg => arg == "-vktCatalogCapture"))
            {
                game.GetComponent<CityProgressionController>().OpenPanel();
                FindAnyObjectByType<CityPhonePanel>(FindObjectsInactive.Include).OpenCatalog();
                yield return null;
            }
            if (Array.Exists(Environment.GetCommandLineArgs(), arg => arg == "-vktCardsCapture"))
            {
                game.GetComponent<CityProgressionController>().OpenPanel();
                CityPhonePanel phone = FindAnyObjectByType<CityPhonePanel>(FindObjectsInactive.Include); phone.OpenCatalog();
                phone.transform.Find("CatalogPanel/Identity").GetComponent<UnityEngine.UI.Button>().onClick.Invoke();
                yield return null;
            }
            if (Array.Exists(Environment.GetCommandLineArgs(), arg => arg == "-vktCityCapture"))
            {
                game.GetComponent<CityProgressionController>().ClosePanel();
                if (game.Hideout.IsInside) game.Hideout.Travel(false);
                CharacterController body = game.Player.GetComponent<CharacterController>(); body.enabled = false;
                game.Player.position = new Vector3(0, 12, -40); body.enabled = true;
                game.Player.GetComponentInChildren<Camera>().transform.LookAt(new Vector3(0, 3, 12));
                yield return null;
            }
            string screenshot = Argument("-vktScreenshot", "");
            if (!string.IsNullOrEmpty(screenshot))
            {
                Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(screenshot)));
                if (!CaptureRenderedFrame(game, screenshot, out string captureError)) { Finish(false, captureError); yield break; }
                if (!File.Exists(screenshot) || !ContainsRenderedPixels(screenshot)) { Finish(false, "Player screenshot is missing or blank; visual validation did not pass."); yield break; }
            }
            Finish(true, details);
        }

        private static bool VerifyEnhanced(CityGameBootstrap game, out string details)
        {
            try
            {
                CityMusicPlayer music = game.GetComponent<CityMusicPlayer>();
                Ensure(music != null && music.Count >= 3, "Music presets were not initialized.");
                CityMusicPanel musicPanel = game.GetComponent<CityMusicPanel>();
                game.GetComponent<CityProgressionController>().ClosePanel();
                musicPanel.Toggle(); Ensure(musicPanel.IsOpen && game.Director.IsPaused, "Music overlay did not pause input.");
                musicPanel.Close(); Ensure(!game.Director.IsPaused, "Music overlay left the game paused.");
                Ensure(Resources.Load<Texture2D>("Art/CityFacades") != null, "City facade atlas is missing.");
                Ensure(CityPlantEncyclopedia.Count == 16 && Resources.Load<Texture2D>("Art/BotanicalAtlas") != null, "Botanical cards or atlas are missing.");
                CityOsmStreets streets = game.GetComponent<CityOsmStreets>();
                Ensure(streets != null && streets.SegmentCount > 0 && streets.Source.buildings.Length > 0, "Actual OSM streets/footprints were not composed.");
                if (game.Hideout.IsInside) Ensure(game.Hideout.Travel(false), "Could not leave hideout for street job.");
                int before = game.Session.Cash;
                Ensure(game.Director.TryAcceptStreetJob(), "Repeatable street job did not unlock.");
                CityStreetJobOffer offer = game.Director.Jobs.Offer;
                for (int i = 0; i < 3; i++) Visit(game, offer.StationAt(i));
                Ensure(!game.Director.Jobs.IsActive && game.Session.Cash == before + offer.CashReward, "Street job payout or route failed.");
                Ensure(!game.Director.TryInteract(offer.StationAt(2)), "Street job paid twice.");
                game.GetComponent<CityProgressionController>().SaveNow();
                InteractWith(game, game.Hideout.Entrance, .9f);
                game.Player.GetComponentInChildren<Camera>().transform.LookAt((game.Hideout.PlotTransform(0).position + game.Hideout.PlotTransform(3).position) / 2 + Vector3.up * .5f);
                details = $"Enhanced: {streets.SegmentCount} OSM segments, {streets.Source.buildings.Length} building footprints, 16 botanical cards, music overlay and physical street-job payout passed.";
                return true;
            }
            catch (Exception error) { details = error.GetType().Name + ": " + error.Message; return false; }
        }

        private static bool Verify(CityGameBootstrap game, out string details)
        {
            try
            {
                string temporaryRoot = Path.GetFullPath(Path.Combine(Path.GetTempPath(), "VKT-validation")) + Path.DirectorySeparatorChar;
                Ensure(CityGameBootstrap.IsSmokeValidation && Path.GetFullPath(game.GetComponent<CityProgressionController>().SavePath).StartsWith(temporaryRoot, StringComparison.OrdinalIgnoreCase),
                    "Player validation requires an isolated temporary profile.");
                Ensure(game.Progression.CompletedContracts == 0, "Validation must start with a fresh isolated profile.");
                game.Player.GetComponent<CityPlayerController>().enabled = false;
                for (int contract = 0; contract < 3; contract++)
                {
                    if (contract > 0) Ensure(game.Director.TryNextContract(), "Next route did not unlock.");
                    for (int station = 0; station < 3; station++) Visit(game, station);
                    Ensure(!game.Director.TryInteract(2), "Completed route paid twice.");
                }
                CityProgression p = game.Progression;
                Ensure(p.CompletedContracts == 3 && p.Session.Cash == 5600 && p.Session.Reputation == 55, "Campaign reward or unlock state mismatch.");
                CityProgressionController controller = game.GetComponent<CityProgressionController>();
                Ensure(controller.Perform(p.BuySeed) && controller.Perform(p.Plant), "Planting failed.");
                p.Tick(30); // Deterministic game-clock advance; no offline growth is inferred.
                Ensure(controller.Perform(p.Harvest) && controller.Perform(p.Sell), "Harvest/trade failed.");
                Ensure(controller.Perform(p.Recruit) && controller.Perform(p.Recruit), "Crew failed.");
                for (int i = 0; i < 3; i++) Ensure(controller.Perform(p.ClaimTerritory), "District claim failed.");
                Ensure(p.Session.Cash == 2915 && p.Crew == 2 && p.Territories == 3, "Final economy balance mismatch.");
                CitySaveStore store = new CitySaveStore(controller.SavePath);
                Ensure(store.TryLoad(out CityProfileData saved, out _) && saved.cash == 2915 && saved.completedContracts == 3 && saved.territories == 3,
                    "Standalone-player save restoration failed.");
                controller.OpenPanel();
                Ensure(game.Director.IsPaused, "Phone did not pause the player.");
                if (Array.Exists(Environment.GetCommandLineArgs(), arg => arg == "-vktHideout"))
                {
                    CityPhonePanel phone = FindAnyObjectByType<CityPhonePanel>(FindObjectsInactive.Include);
                    phone.OpenCatalog();
                    phone.transform.Find("CatalogPanel/Variety_1").GetComponent<UnityEngine.UI.Button>().onClick.Invoke();
                    Ensure(game.Hideout.SelectedVariety == 1, "Catalog row did not select the physical garden variety.");
                    phone.transform.Find("CatalogPanel/Variety_0").GetComponent<UnityEngine.UI.Button>().onClick.Invoke();
                    Ensure(game.Hideout.SelectedVariety == 0, "Catalog row did not reset the physical garden variety.");
                    controller.ClosePanel();
                    InteractWith(game, game.Hideout.Entrance, 0.9f);
                    Ensure(game.Hideout.IsInside, "Player could not enter hideout.");
                    for (int i = 0; i < CityGarden.PlotCount; i++)
                    {
                        Ensure(controller.Perform(() => p.Garden.BuySeed(0)), "Player catalog seed purchase failed.");
                        InteractWith(game, game.Hideout.PlotTransform(i), 0.4f);
                        InteractWith(game, game.Hideout.PlotTransform(i), 0.4f);
                    }
                    Ensure(game.Session.Cash == 2675 && p.Garden.Plot(3).moistureSeconds > 7100, "Player garden care failed.");
                    CitySaveStore gardenStore = new CitySaveStore(controller.SavePath);
                    Ensure(gardenStore.TryLoad(out CityProfileData gardenSaved, out _) && gardenSaved.garden.plots[3].planted,
                        "Player garden save restore failed.");
                    InteractWith(game, game.Hideout.Exit, 1.05f);
                    Ensure(!game.Hideout.IsInside, "Player could not exit hideout.");
                    InteractWith(game, game.Hideout.Entrance, 0.9f);
                    Ensure(game.Hideout.IsInside, "Could not reenter hideout before fatal damage.");
                    Ensure(game.Vitals.ApplyDamage(100) && !game.Vitals.IsAlive, "Player fatal damage did not enter dead state.");
                    Ensure(!game.Player.GetComponent<CityPlayerInteraction>().TryInteract(), "Dead player interacted.");
                    Ensure(game.Vitals.Respawn() && !game.Vitals.ApplyDamage(10), "Player hospital or respawn protection failed.");
                    Ensure(!game.Hideout.IsInside && !game.Hideout.Exit.gameObject.activeInHierarchy, "Hospital did not reset hideout and deactivate interior.");
                    InteractWith(game, game.Hideout.Entrance, 0.9f);
                    Ensure(game.Hideout.IsInside, "Hospital return could not reenter hideout.");
                    InteractWith(game, game.Hideout.Exit, 1.05f);
                    Ensure(!game.Hideout.IsInside, "Hospital return could not exit hideout.");
                    InteractWith(game, game.Hideout.Entrance, 0.9f);
                    Camera roomCamera = game.Player.GetComponentInChildren<Camera>();
                    roomCamera.transform.LookAt((game.Hideout.PlotTransform(0).position + game.Hideout.PlotTransform(3).position) / 2 + Vector3.up * 0.5f);
                }
                details = "Actual Windows player: terrain loaded, nine physics interactions, three contracts, growth/trade, crew, district campaign and durable save restore passed.";
                if (game.Hideout.IsInside) details += " Catalog seed purchases, hideout entry/exit, four physical plants, watering, garden save restoration, death and hospital respawn passed.";
                return true;
            }
            catch (Exception error) { details = error.GetType().Name + ": " + error.Message; return false; }
        }

        private static void Visit(CityGameBootstrap game, int index)
        {
            CityMission station = game.Director.StationAt(index);
            CharacterController body = game.Player.GetComponent<CharacterController>();
            body.enabled = false;
            game.Player.position = station.transform.position + new Vector3(0, -0.73f, -2.5f);
            body.enabled = true;
            Camera camera = game.Player.GetComponentInChildren<Camera>();
            camera.transform.LookAt(station.transform.position);
            Physics.SyncTransforms();
            Ensure(game.Player.GetComponent<CityPlayerInteraction>().TryInteract(), "Physics interaction failed at station " + (index + 1));
        }

        private static void Ensure(bool condition, string message) { if (!condition) throw new InvalidOperationException(message); }
        private static void InteractWith(CityGameBootstrap game, Transform target, float offset)
        {
            CharacterController body = game.Player.GetComponent<CharacterController>(); body.enabled = false;
            game.Player.position = target.position + new Vector3(0, -offset, target == game.Hideout.Exit ? 2.5f : -2.5f); body.enabled = true;
            game.Player.GetComponentInChildren<Camera>().transform.LookAt(target.position);
            Physics.SyncTransforms();
            Ensure(game.Player.GetComponent<CityPlayerInteraction>().TryInteract(), "Player prop interaction failed: " + target.name);
        }
        private static bool ContainsRenderedPixels(string file)
        {
            Texture2D texture = new Texture2D(2, 2);
            try
            {
                if (!ImageConversion.LoadImage(texture, File.ReadAllBytes(file))) return false;
                Color32[] pixels = texture.GetPixels32();
                int visible = 0;
                foreach (Color32 pixel in pixels) if (pixel.r > 8 || pixel.g > 8 || pixel.b > 8) visible++;
                return visible > pixels.Length / 50;
            }
            finally { Destroy(texture); }
        }
        private static bool CaptureRenderedFrame(CityGameBootstrap game, string file, out string error)
        {
            error = "";
            Camera camera = game.Player.GetComponentInChildren<Camera>();
            RenderTexture destination = new RenderTexture(1280, 720, 24, RenderTextureFormat.ARGB32);
            Texture2D pixels = new Texture2D(1280, 720, TextureFormat.RGBA32, false);
            RenderTexture originalTarget = camera.targetTexture, originalActive = RenderTexture.active;
            Canvas[] canvases = FindObjectsByType<Canvas>(FindObjectsInactive.Exclude);
            RenderMode[] modes = new RenderMode[canvases.Length];
            Camera[] cameras = new Camera[canvases.Length];
            float[] distances = new float[canvases.Length];
            try
            {
                destination.Create();
                camera.targetTexture = destination;
                for (int i = 0; i < canvases.Length; i++)
                {
                    modes[i] = canvases[i].renderMode; cameras[i] = canvases[i].worldCamera; distances[i] = canvases[i].planeDistance;
                    canvases[i].renderMode = RenderMode.ScreenSpaceCamera;
                    canvases[i].worldCamera = camera;
                    canvases[i].planeDistance = camera.nearClipPlane + 0.001f;
                }
                Canvas.ForceUpdateCanvases();
                // The installed URP supports this public offscreen rendering request.
                RenderPipeline.SubmitRenderRequest(camera, new UniversalRenderPipeline.SingleCameraRequest { destination = destination });
                RenderTexture.active = destination;
                pixels.ReadPixels(new Rect(0, 0, 1280, 720), 0, 0);
                pixels.Apply();
                File.WriteAllBytes(file, pixels.EncodeToPNG());
                return true;
            }
            catch (Exception exception) { error = "Offscreen player capture failed: " + exception.GetType().Name; return false; }
            finally
            {
                for (int i = 0; i < canvases.Length; i++)
                { canvases[i].renderMode = modes[i]; canvases[i].worldCamera = cameras[i]; canvases[i].planeDistance = distances[i]; }
                camera.targetTexture = originalTarget; RenderTexture.active = originalActive;
                destination.Release(); Destroy(destination); Destroy(pixels);
            }
        }
        private static string Argument(string key, string fallback)
        {
            string[] args = Environment.GetCommandLineArgs();
            for (int i = 0; i + 1 < args.Length; i++) if (args[i] == key) return args[i + 1];
            return fallback;
        }
        private static void Finish(bool passed, string details)
        {
            string output = Path.GetFullPath(Argument("-vktReport", Path.Combine(Path.GetTempPath(), "vkt-player-report.json")));
            Directory.CreateDirectory(Path.GetDirectoryName(output));
            File.WriteAllText(output, JsonUtility.ToJson(new Report { passed = passed, details = details, product = Application.productName }, true));
            Debug.Log("VKT_PLAYER_VALIDATION: " + (passed ? "PASS " : "FAIL ") + details);
            Application.Quit(passed ? 0 : 1);
        }
    }
}
#endif
