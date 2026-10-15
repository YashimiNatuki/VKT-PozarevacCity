using System.Collections;
using System;
using System.IO;
using System.Linq;
using CesiumForUnity;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Pozarevac.CityGame
{
    public sealed class CityGameBootstrap : MonoBehaviour
    {
        [SerializeField, Min(0)] private int startingCash = 2500;
        [SerializeField] private Vector3 localSpawnOffset = new Vector3(0f, 3f, 0f);
        private readonly CityGroundProbe ground = new CityGroundProbe();
        private CityHud hud;
        private Camera playerCamera;
        private CesiumGeoreference georeference;
        private CharacterController character;
        private CityPlayerController movement;
        private bool loadingTerrain;
        private CityProgressionController progressionController;

        public CitySession Session { get; private set; }
        public CityProgression Progression { get; private set; }
        public static bool IsSmokeValidation
        {
            get
            {
#if DEVELOPMENT_BUILD || UNITY_EDITOR
                return Array.Exists(Environment.GetCommandLineArgs(), arg => arg == "-vktSmoke");
#else
                return false;
#endif
            }
        }
        public CityMissionDirector Director { get; private set; }
        public Transform Player { get; private set; }
        public bool WorldReady { get; private set; }
        public CityIntroPlayer Intro { get; private set; }
        public CityHideout Hideout { get; private set; }
        public CityGameAudio Audio { get; private set; }
        public CityPlayerVitals Vitals { get; private set; }
        public CityTouchControls Touch { get; private set; }
        public CityWorldAtmosphere Atmosphere { get; private set; }

        private void Awake()
        {
            Application.targetFrameRate = 60;
            bool isolatedValidation = Application.isBatchMode || IsSmokeValidation;
            string profilePath = isolatedValidation
                ? Path.Combine(Path.GetTempPath(), "VKT-validation", Guid.NewGuid().ToString("N") + ".json")
                : Path.Combine(Application.persistentDataPath, "vkt-profile-v1.json");
            CitySaveStore store = new CitySaveStore(profilePath);
            bool restored = store.TryLoad(out CityProfileData profile, out string saveStatus);
            Session = restored ? CitySession.Restore(profile) : new CitySession(startingCash);
            Progression = new CityProgression(Session, restored ? profile : null);
            if (isolatedValidation) Progression.CreateCharacter("Validation");
            else if (!store.CanWrite) Progression.CreateCharacter("Guest");
            CityWorldAtmosphere atmosphere = new GameObject("CityWorldAtmosphere").AddComponent<CityWorldAtmosphere>();
            Atmosphere = atmosphere;
            Light sun = FindObjectsByType<Light>(FindObjectsInactive.Include).FirstOrDefault(light => light.type == LightType.Directional);
            atmosphere.Configure(sun);
            if (UnityEngine.SceneManagement.SceneManager.GetActiveScene().name == "VKTOffline") atmosphere.ConfigureCycle(1080, 720, 8);
            hud = new GameObject("CityHUD").AddComponent<CityHud>();
            hud.Initialize(Session, atmosphere);
            progressionController = gameObject.AddComponent<CityProgressionController>();
            progressionController.Initialize(this, hud, store, saveStatus);
            CreatePlayer();
            if (Player != null)
            {
                Audio = gameObject.AddComponent<CityGameAudio>();
                Audio.Initialize(Player, progressionController);
            }
            Intro = new GameObject("CityIntro").AddComponent<CityIntroPlayer>();
            Intro.transform.SetParent(transform, false);
            Intro.Begin(active =>
            {
                if (Director != null) Director.SetPaused(active);
            });
        }

        private void Start()
        {
            if (Player != null) StartCoroutine(WaitForTerrain());
        }

        private void Update()
        {
            if (Touch != null && Director != null) Touch.SetOverlayBlocked(Director.IsPaused);
            if (Vitals != null)
            {
                hud.SetHealth(Vitals.Health.Current, Vitals.Health.Maximum);
                if (!Vitals.IsAlive && Director != null && !Director.IsPaused && Keyboard.current != null && Keyboard.current.hKey.wasPressedThisFrame) Vitals.Respawn();
            }
            if (!WorldReady && !loadingTerrain && Player != null && Keyboard.current != null && Keyboard.current.tKey.wasPressedThisFrame)
                StartCoroutine(WaitForTerrain());
        }

        private void CreatePlayer()
        {
            Camera[] cameras = FindObjectsByType<Camera>(FindObjectsInactive.Include);
            playerCamera = cameras.FirstOrDefault(candidate => candidate.name == "DynamicCamera") ?? Camera.main;
            georeference = FindAnyObjectByType<CesiumGeoreference>();
            if (playerCamera == null || georeference == null)
            {
                hud.SetMissionText("SETUP ERROR - a main camera and CesiumGeoreference are required.");
                Debug.LogError("CityGame requires a main camera and CesiumGeoreference.");
                return;
            }

            GameObject player = new GameObject("CityPlayer");
            Player = player.transform;
            Player.SetParent(georeference.transform, false);
            Player.localPosition = localSpawnOffset;
            character = player.AddComponent<CharacterController>();
            character.height = 1.8f;
            character.radius = 0.32f;
            character.center = new Vector3(0f, 0.9f, 0f);
            character.enabled = false;
            movement = player.AddComponent<CityPlayerController>();
            movement.enabled = false;
            Vitals = player.AddComponent<CityPlayerVitals>();
            movement.SetCanAct(() => Vitals.CanAct);
            Vitals.Died += OnPlayerDied;
            Vitals.Respawned += OnPlayerRespawned;

            // The player owns globe positioning. Nested camera anchors and origin shifts
            // can restore a stale globe pose after the parent moves or teleports.
            foreach (Behaviour behaviour in playerCamera.GetComponents<Behaviour>())
                if (behaviour is CesiumGlobeAnchor || behaviour is CesiumOriginShift ||
                    behaviour is CesiumFlyToController || behaviour is CesiumCameraController)
                    behaviour.enabled = false;
            playerCamera.gameObject.SetActive(true);
            playerCamera.enabled = true;
            playerCamera.tag = "MainCamera";
            playerCamera.transform.SetParent(Player, true);
            playerCamera.transform.localPosition = new Vector3(0f, 1.62f, 0f);
            playerCamera.transform.localRotation = Quaternion.identity;
            movement.ConfigureView(playerCamera.transform);
            foreach (Camera other in cameras)
                if (other != playerCamera && other.targetTexture == null) other.enabled = false;
            AudioListener activeListener = playerCamera.GetComponent<AudioListener>();
            if (activeListener == null) activeListener = playerCamera.gameObject.AddComponent<AudioListener>();
            foreach (AudioListener listener in FindObjectsByType<AudioListener>(FindObjectsInactive.Include))
                listener.enabled = listener == activeListener;
            player.AddComponent<CityPlayerInteraction>().Configure(playerCamera, Session, hud);
        }

        private IEnumerator WaitForTerrain()
        {
            loadingTerrain = true;
            float deadline = Time.realtimeSinceStartup + 45f;
            Vector3[] offsets = { new Vector3(2f, 0f, 8f), new Vector3(-5f, 0f, 22f), new Vector3(4f, 0f, 36f) };
            Vector3[] positions = new Vector3[3];
            WaitForSecondsRealtime interval = new WaitForSecondsRealtime(0.25f);
            while (Time.realtimeSinceStartup < deadline)
            {
                hud.SetMissionText("LOADING POZAREVAC\nWaiting for walkable city surfaces at spawn and all three checkpoints.\nMovement stays disabled until the terrain is ready.");
                bool ready = ground.TryFindSurface(georeference.transform.TransformPoint(localSpawnOffset), out Vector3 spawn);
                for (int i = 0; i < offsets.Length; i++)
                    ready &= ground.TryFindSurface(georeference.transform.TransformPoint(localSpawnOffset + offsets[i]), out positions[i]);
                if (ready)
                {
                    Player.position = spawn + Vector3.up * 0.12f;
                    playerCamera.transform.localPosition = new Vector3(0f, 1.62f, 0f);
                    playerCamera.transform.localRotation = Quaternion.identity;
                    Player.gameObject.AddComponent<CesiumGlobeAnchor>();
                    character.enabled = true;
                    movement.enabled = true;
                    Director = gameObject.AddComponent<CityMissionDirector>();
                    Director.Initialize(Session, hud, Player, playerCamera, georeference.transform, positions, Progression.CompletedContracts);
                    Director.BindVitals(Vitals);
                    progressionController.BindDirector();
                    GameObject checkpoint = new GameObject("VKT_Bolnica_Checkpoint");
                    checkpoint.transform.SetParent(georeference.transform, false); checkpoint.transform.position = Player.position;
                    checkpoint.AddComponent<CesiumGlobeAnchor>(); Vitals.Configure(checkpoint.transform);
                    Hideout = gameObject.AddComponent<CityHideout>();
                    Hideout.Initialize(this, progressionController, positions[2]);
                    Touch = gameObject.AddComponent<CityTouchControls>();
                    Touch.Initialize(() => Player.GetComponent<CityPlayerInteraction>().TryInteract(), progressionController.TogglePanel,
                        () => Director.TryNextContract(), () => Director.TryRetry(), () => { if (!Director.IsPaused) Vitals.Respawn(); });
                    movement.ConfigureTouch(Touch);
                    CreateHazard(spawn + new Vector3(18, 0, 8), CityHazardKind.GuardDog);
                    CreateHazard(spawn + new Vector3(-22, 0, 20), CityHazardKind.HostileAdult);
                    CityAmbience ambience = gameObject.AddComponent<CityAmbience>();
                    ambience.Initialize(Atmosphere, () => Hideout != null && Hideout.IsInside, () => Audio.Muted);
                    CityMusicPlayer music = gameObject.AddComponent<CityMusicPlayer>();
                    music.Initialize();
                    gameObject.AddComponent<CityMusicPanel>().Initialize(music, () => Director.IsPaused, open => Director.SetOverlayOpen(open));
                    bool offline = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name == "VKTOffline";
                    bool authored = Array.Exists(Environment.GetCommandLineArgs(), arg => arg == "-vktAuthoredCenter");
                    if (offline && !authored && CityOsmStreets.Load() != null)
                        gameObject.AddComponent<CityOsmStreets>().Initialize(georeference.transform, new Vector3(spawn.x, spawn.y, spawn.z));
                    else if (offline || authored)
                        gameObject.AddComponent<CityCityDetail>().Initialize(georeference.transform, spawn, Atmosphere);
                    if (Intro != null && Intro.IsActive) Director.SetPaused(true);
                    WorldReady = true;
                    Debug.Log("CITY_GAME_READY: grounded player, three mission contracts and one active audio listener.");
                    loadingTerrain = false;
                    hud.ShowNotification("City ready. Follow the cyan pickup marker and press E to begin.", 6f);
                    yield break;
                }
                yield return interval;
            }
            loadingTerrain = false;
            hud.SetMissionText("CITY TERRAIN NOT READY\nCheck Cesium tiles, credentials and physics meshes.\nPress T to retry loading. The player is held safely above the scene.");
            Debug.LogWarning("CityGame could not find walkable surfaces at all route checkpoints within 45 seconds.");
        }

        private void CreateHazard(Vector3 approximate, CityHazardKind kind)
        {
            if (!ground.TryFindSurface(approximate, out Vector3 at)) return;
            GameObject hazard = new GameObject("VKT_" + kind); hazard.transform.SetParent(georeference.transform, true);
            hazard.transform.position = at; hazard.AddComponent<CesiumGlobeAnchor>();
            hazard.AddComponent<CityHazard>().Configure(Vitals, kind);
        }
        private void OnPlayerDied()
        {
            Director?.FailCurrent("Pao si. H / BOLNICA: vrati se na bezbedno mesto; zatim R za novi pokušaj.");
            hud.ShowNotification("Nema života. H: bolnica. Nezavršena misija ne isplaćuje nagradu.", 10);
        }
        private void OnPlayerRespawned() { Hideout?.ResetAfterRespawn(); Director?.TryRetry(); hud.ShowNotification("Oporavljen si. Kratka zaštita od napada je aktivna.", 5); }
    }
}
