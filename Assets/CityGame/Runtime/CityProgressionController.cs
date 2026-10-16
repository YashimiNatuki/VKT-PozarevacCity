using System;
using System.IO;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Pozarevac.CityGame
{
    public sealed class CityProgressionController : MonoBehaviour
    {
        private CityGameBootstrap game;
        private CityHud hud;
        private CitySaveStore store;
        private CityMissionDirector director;
        private CityPhonePanel panel;
        private bool dirty;
        private float saveCountdown = 5;
        private float gardenCountdown;
        public string SaveStatus { get; private set; }
        public string SavePath => store.PathName;
        public CityProgression Progression => game.Progression;

        public void Initialize(CityGameBootstrap owner, CityHud cityHud, CitySaveStore saveStore, string status)
        {
            game = owner; hud = cityHud; store = saveStore; SaveStatus = status;
            game.Session.Changed += MarkDirty;
            Progression.Lifestyle.Changed += MarkDirty;
            Progression.Appearance.Changed += MarkDirty;
            panel = new GameObject("VKTPhone").AddComponent<CityPhonePanel>();
            panel.transform.SetParent(transform, false);
            panel.Initialize(this);
            RefreshIdentity();
            if (!store.CanWrite) hud.ShowNotification(status, 12);
        }

        public void BindDirector()
        {
            director = game.Director;
            director.ContractCompleted += OnContractCompleted;
            Progression.BindStreetJobs(director.Jobs);
            director.Jobs.Completed += OnStreetJobCompleted;
        }

        private void OnContractCompleted(int count)
        {
            if (!Progression.CompleteContract(count)) return;
            dirty = true;
            SaveNow(); // Reward and completed count are now committed together.
            RefreshIdentity();
            game.Audio.Speak(CityVoice.Luka);
            if (count == 1) hud.ShowNotification("Telefon otključan: P. Strejn katalog, seme, štek i trgovina.", 7);
            else if (count == 2) hud.ShowNotification("Crew unlocked at REP 30. Open P for recruitment.", 7);
            else hud.ShowNotification("City route complete. Recruit two crew members to claim three districts.", 7);
        }

        private void OnStreetJobCompleted(int completedCount)
        {
            dirty = true;
            SaveNow(); // Snapshot includes the committed count and its cash/reputation reward together.
        }

        private void MarkDirty() => dirty = true;

        private void Update()
        {
            if (game == null || !game.WorldReady || (game.Intro != null && game.Intro.IsActive)) return;
            gardenCountdown -= Time.unscaledDeltaTime;
            if (gardenCountdown <= 0)
            {
                if (Progression.Garden.Advance(DateTimeOffset.UtcNow.ToUnixTimeSeconds())) dirty = true;
                gardenCountdown = 1f;
            }
            if (!Progression.IsCreated && !Application.isBatchMode && !panel.IsOpen) OpenPanel();
            Keyboard keyboard = Keyboard.current;
            if (keyboard != null && keyboard.pKey.wasPressedThisFrame && Progression.PhoneUnlocked)
            {
                if (panel.IsOpen) ClosePanel(); else OpenPanel();
            }
            if (panel.IsOpen && keyboard != null && keyboard.escapeKey.wasPressedThisFrame) ClosePanel();
            if (director != null && !director.IsPaused)
            {
                if (Progression.IsGrowing) dirty = true;
                if (Progression.Tick(Time.deltaTime)) hud.ShowNotification("Probna saksija je spremna. P: berba.", 5);
            }
            saveCountdown -= Time.unscaledDeltaTime;
            if (saveCountdown <= 0) { SaveNow(); saveCountdown = 5; }
        }

        public bool CreateCharacter(string name)
        {
            if (!store.CanWrite || !Progression.CreateCharacter(name)) return false;
            dirty = true;
            SaveNow();
            RefreshIdentity();
            ClosePanel();
            hud.ShowNotification("Follow the cyan marker. E accepts pickup; finish delivery and contact to unlock P.", 8);
            return true;
        }

        public bool Perform(Func<bool> action)
        {
            if (!store.CanWrite || (game.Vitals != null && !game.Vitals.CanAct) || !action()) return false;
            dirty = true;
            SaveNow();
            return true;
        }

        public void Notify(string message) => hud.ShowNotification(message, 5);

        public void OpenPanel()
        {
            if (director == null) return;
            game.GetComponent<CityLifestyleRuntime>()?.Wardrobe?.Close();
            game.GetComponent<CityLifestyleRuntime>()?.CloseMenu();
            game.GetComponent<CityMusicPanel>()?.Close();
            director.SetOverlayOpen(true);
            panel.Open();
        }

        public void TogglePanel()
        {
            if (!Progression.PhoneUnlocked || (game.Vitals != null && !game.Vitals.CanAct)) return;
            if (panel.IsOpen) ClosePanel(); else OpenPanel();
        }

        public void ClosePanel()
        {
            if (!Progression.IsCreated) return;
            panel.Close();
            director?.SetOverlayOpen(false);
        }

        public void SaveNow()
        {
            if (!dirty || !store.CanWrite || !Progression.IsCreated) return;
            try { store.Save(Progression.Snapshot()); dirty = false; SaveStatus = "Saved locally"; }
            catch (Exception error) when (error is IOException || error is UnauthorizedAccessException || error is ArgumentException || error is InvalidOperationException)
            { SaveStatus = "Save failed; progress remains in this session. Existing files preserved."; Debug.LogWarning("VKT profile save failed: " + error.GetType().Name); }
        }

        private void RefreshIdentity() => hud.SetIdentity(Progression.Callsign, Progression.PhoneUnlocked);
        private void OnApplicationPause(bool paused) { if (paused) SaveNow(); }
        private void OnApplicationQuit() => SaveNow();
        private void OnEnable()
        {
            if (game == null) return;
            game.Session.Changed += MarkDirty;
            Progression.Lifestyle.Changed += MarkDirty;
            Progression.Appearance.Changed += MarkDirty;
            if (director != null) { director.ContractCompleted += OnContractCompleted; director.Jobs.Completed += OnStreetJobCompleted; }
        }
        private void OnDisable()
        {
            if (game == null) return;
            SaveNow();
            game.Session.Changed -= MarkDirty;
            Progression.Lifestyle.Changed -= MarkDirty;
            Progression.Appearance.Changed -= MarkDirty;
            if (director != null) { director.ContractCompleted -= OnContractCompleted; director.Jobs.Completed -= OnStreetJobCompleted; director.SetOverlayOpen(false); }
            if (panel != null) panel.Close();
        }
        private void OnDestroy()
        {
            SaveNow();
            if (game != null && game.Session != null) game.Session.Changed -= MarkDirty;
            if (game != null && game.Progression != null) Progression.Lifestyle.Changed -= MarkDirty;
            Progression.Appearance.Changed -= MarkDirty;
            if (director != null) { director.ContractCompleted -= OnContractCompleted; director.Jobs.Completed -= OnStreetJobCompleted; }
            if (panel != null) Destroy(panel.gameObject);
        }
    }
}
