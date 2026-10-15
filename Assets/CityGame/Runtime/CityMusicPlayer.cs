using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.Networking;

namespace Pozarevac.CityGame
{
    public sealed class CityMusicPlayer : MonoBehaviour
    {
        [Serializable] public sealed class Station { public string name; public string url; }
        [Serializable] private sealed class Settings { public string localFolder; public Station[] stations; }
        private sealed class Track { public string Name, Uri; public AudioType Type; public bool Radio; }
        private readonly List<Track> tracks = new List<Track>();
        private AudioSource source;
        private AudioClip ownedClip;
        private UnityWebRequest pending;
        private Coroutine loading;
        private int current = -1;
        private bool paused, started;
        public string Status { get; private set; } = "Dodaj MP3, OGG ili WAV u Music folder.";
        public string Folder => Path.Combine(Application.persistentDataPath, "Music");
        public int Count => tracks.Count;
        public bool IsPlaying => source != null && source.isPlaying;
        public int LocalCount { get { int count = 0; foreach (Track track in tracks) if (!track.Radio) count++; return count; } }
        public float Volume { get => source != null ? source.volume : 0.35f; set { if (source != null) source.volume = Mathf.Clamp01(value); } }
        public bool Muted { get => source != null && source.mute; set { if (source != null) source.mute = value; } }
        public void Initialize()
        {
            if (source != null) return;
            source = gameObject.AddComponent<AudioSource>(); source.playOnAwake = false; source.spatialBlend = 0; source.volume = 0.35f;
            Refresh();
        }
        public void Refresh()
        {
            StopLoading(); if (source != null) source.Stop(); ReleaseClip(); tracks.Clear(); current = -1; started = paused = false;
            try
            {
                Directory.CreateDirectory(Folder); AddFolder(Folder);
                string knownMusic = Environment.GetFolderPath(Environment.SpecialFolder.MyMusic);
                if (!string.IsNullOrWhiteSpace(knownMusic) && !string.Equals(Path.GetFullPath(knownMusic), Path.GetFullPath(Folder), StringComparison.OrdinalIgnoreCase)) AddFolder(knownMusic);
                AddRadio("SomaFM Groove Salad • ambient/downtempo", "https://ice2.somafm.com/groovesalad-128-mp3");
                AddRadio("SomaFM Beat Blender • deep house/chill", "https://ice2.somafm.com/beatblender-128-mp3");
                AddRadio("SomaFM Secret Agent • spy soundtrack", "https://ice2.somafm.com/secretagent-128-mp3");
                string configPath = Path.Combine(Application.persistentDataPath, "music.local.json");
                if (File.Exists(configPath))
                {
                    if (new FileInfo(configPath).Length > 65536) throw new IOException("Music konfiguracija je prevelika.");
                    Settings config = JsonUtility.FromJson<Settings>(File.ReadAllText(configPath));
                    if (config != null)
                    {
                        if (!string.IsNullOrWhiteSpace(config.localFolder) && Path.IsPathRooted(config.localFolder)) AddFolder(config.localFolder);
                        if (config.stations != null) foreach (Station station in config.stations)
                        {
                            if (tracks.Count >= 128) break;
                            if (station == null || !Uri.TryCreate(station.url, UriKind.Absolute, out Uri uri) || uri.Scheme != "https" || !string.IsNullOrEmpty(uri.UserInfo)) continue;
                            AddRadio(string.IsNullOrWhiteSpace(station.name) ? "Radio" : station.name.Substring(0, Math.Min(80, station.name.Length)), uri.AbsoluteUri);
                        }
                    }
                }
                Status = tracks.Count == 0 ? "Nema muzike. Dodaj fajlove u Music pa OSVEŽI." : $"Pronađeno: {tracks.Count}. Pritisni PUSTI.";
            }
            catch (Exception error) when (error is IOException || error is UnauthorizedAccessException || error is ArgumentException)
            { Status = "Muzička biblioteka nije dostupna. Proveri lokalnu konfiguraciju."; }
        }
        private void AddRadio(string name, string uri)
        {
            if (tracks.Count < 128) tracks.Add(new Track { Name = name, Uri = uri, Type = AudioType.MPEG, Radio = true });
        }
        private void AddFolder(string folder)
        {
            if (!Directory.Exists(folder)) return;
            int examined = 0;
            foreach (string path in Directory.EnumerateFiles(folder, "*", SearchOption.TopDirectoryOnly))
            {
                if (++examined > 1024 || tracks.Count >= 128) break;
                string extension = Path.GetExtension(path).ToLowerInvariant();
                AudioType type = extension == ".mp3" ? AudioType.MPEG : extension == ".ogg" ? AudioType.OGGVORBIS : extension == ".wav" ? AudioType.WAV : AudioType.UNKNOWN;
                if (type == AudioType.UNKNOWN || new FileInfo(path).Length > 128L * 1024 * 1024) continue;
                tracks.Add(new Track { Name = Path.GetFileNameWithoutExtension(path), Uri = new Uri(Path.GetFullPath(path)).AbsoluteUri, Type = type });
            }
        }
        public void Next() { if (tracks.Count > 0) Play((current + 1) % tracks.Count); }
        public void Previous() { if (tracks.Count > 0) Play((current - 1 + tracks.Count) % tracks.Count); }
        public void Radio()
        {
            for (int offset = 1; offset <= tracks.Count; offset++)
            { int index = (current + offset) % tracks.Count; if (tracks[index].Radio) { Play(index); return; } }
        }
        public void TogglePause()
        {
            if (source == null) Initialize();
            if (current < 0) { Next(); return; }
            if (loading != null && !started) return;
            paused = !paused; if (paused) source.Pause(); else source.UnPause();
            Status = (paused ? "Pauza: " : "Svira: ") + tracks[current].Name;
        }
        private void Play(int index)
        {
            StopLoading(); source.Stop(); ReleaseClip(); current = index; paused = started = false;
            loading = StartCoroutine(Load(tracks[index]));
        }
        private IEnumerator Load(Track track)
        {
            Status = "Učitavanje: " + track.Name;
            pending = UnityWebRequestMultimedia.GetAudioClip(track.Uri, track.Type); pending.timeout = track.Radio ? 0 : 20;
            DownloadHandlerAudioClip handler = (DownloadHandlerAudioClip)pending.downloadHandler;
            handler.streamAudio = true;
            UnityWebRequestAsyncOperation operation = pending.SendWebRequest();
            float began = Time.realtimeSinceStartup, lastData = began;
            ulong lastBytes = 0;
            while (!operation.isDone)
            {
                if (pending.downloadedBytes != lastBytes) { lastBytes = pending.downloadedBytes; lastData = Time.realtimeSinceStartup; }
                if (track.Radio)
                {
                    // Infinite streams have no known remainder; this is a bounded experimental
                    // decoder path, not Unity's documented finite-file completion guarantee.
                    if (!started && pending.downloadedBytes >= 65536)
                    {
                        AudioClip available = null;
                        try { available = handler.audioClip; } catch (InvalidOperationException) { }
                        if (available != null && available.loadState == AudioDataLoadState.Loaded)
                        { ownedClip = available; source.clip = ownedClip; source.Play(); started = true; Status = "Radio • eksperimentalno: " + track.Name; }
                    }
                    if ((!started && Time.realtimeSinceStartup - began > 20) || Time.realtimeSinceStartup - lastData > 20 || pending.downloadedBytes > 16UL * 1024 * 1024 || Time.realtimeSinceStartup - began > 600)
                    {
                        pending.Abort(); source.Stop(); started = false;
                        Status = "Radio nije podržan ili je dostignut limit sesije. Izaberi sledeću stanicu/pesmu.";
                        pending.Dispose(); pending = null; loading = null; yield break;
                    }
                }
                yield return null;
            }
            if (pending.result == UnityWebRequest.Result.Success)
            {
                if (ownedClip == null) ownedClip = DownloadHandlerAudioClip.GetContent(pending);
                if (ownedClip != null) { source.clip = ownedClip; source.Play(); started = true; Status = "Svira: " + track.Name; }
                else Status = "Format zvuka nije podržan.";
            }
            else Status = "Zvuk nije dostupan. Izaberi drugu pesmu ili proveri vezu.";
            pending.Dispose(); pending = null; loading = null;
        }
        private void Update()
        {
            if (started && !paused && source != null && !source.isPlaying && ownedClip != null && source.time >= Mathf.Max(0, ownedClip.length - 0.25f)) { started = false; Next(); }
        }
        private void StopLoading() { if (pending != null) { pending.Abort(); pending.Dispose(); pending = null; } if (loading != null) { StopCoroutine(loading); loading = null; } }
        private void ReleaseClip() { if (source != null) source.clip = null; if (ownedClip != null) Destroy(ownedClip); ownedClip = null; }
        private void OnDestroy() { StopLoading(); ReleaseClip(); if (source != null) Destroy(source); }
    }
}
