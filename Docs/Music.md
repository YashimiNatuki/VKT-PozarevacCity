# Local mini music player

`CityMusicPlayer.Initialize()` creates a separate 2D music AudioSource. `CityMusicPanel.Initialize(player, () => director.IsPaused)` adds a uGUI panel with F6 and a dedicated lower-right mobile button. Escape or gameplay pause closes the panel; it does not consume mission retry R. Music is an independent user-controlled background source.

Controls: previous, play/pause, next, quieter/louder, mute, refresh. Tracks are loaded only after user action; no autoplay on discovery. Existing city effects/dialogue stay separate.

Library discovery is bounded and non-recursive: persistent-data `Music`, the operating system's known Music folder, and an optional explicitly configured absolute folder. Each folder scan examines at most 1,024 top-level files; the combined list has at most 128 items. MP3/OGG/WAV are supported; local files above 128 MiB are skipped. No whole-home search, playback-history inspection, upload or redistribution occurs.

On 2026-10-14 the latest metadata-only known-folder inventory found **16 top-level audio files**. No personal filenames or media are included in source documentation or Git.

Optional configuration: `music.local.json` in `Application.persistentDataPath`:

```json
{
  "localFolder": "D:/MyMusic",
  "stations": [
    { "name": "My reviewed station", "url": "https://my-radio.example/direct-audio.mp3" }
  ]
}
```

The configuration example is a placeholder format, not a working station. Custom HTTPS direct audio endpoints without embedded user/password are accepted. The player also contains three real, optional SomaFM presets verified against the broadcaster's official playlists on 2026-10-14:

| Station | Genre / description | Direct HTTPS endpoint | Official source |
| --- | --- | --- | --- |
| Groove Salad | Ambient/downtempo | `https://ice2.somafm.com/groovesalad-128-mp3` | [Official playlist](https://api.somafm.com/groovesalad.pls) |
| Beat Blender | Deep house/chill | `https://ice2.somafm.com/beatblender-128-mp3` | [Official playlist](https://api.somafm.com/beatblender.pls) |
| Secret Agent | Spy soundtrack | `https://ice2.somafm.com/secretagent-128-mp3` | [Official playlist](https://api.somafm.com/secretagent.pls) |

Endpoints were verified from broadcaster metadata; Unity playback and uninterrupted availability are not thereby verified. No music was downloaded, bundled or republished. Use RADIO to cycle presets; nothing starts automatically.

[Unity's streamAudio documentation](https://docs.unity3d.com/ScriptReference/Networking.DownloadHandlerAudioClip-streamAudio.html) describes playback before a **finite file** completes and requires enough download throughput to finish before playback ends. An endless stream has no known remainder. The experimental radio path waits for 64 KiB and an exposed loaded AudioClip, then attempts early playback. It fails after 20 seconds without a playable clip or new data, and stops at 16 MiB or ten minutes. This is not a verified 24/7 radio backend. A webpage, playlist URL, HLS manifest or authentication flow is not a supported direct-audio input. Local files are the dependable first path.

Loading uses UnityWebRequestMultimedia, streaming clip mode and owned request/clip cleanup on change/destroy. Configuration is limited to 64 KiB. Finite local file requests have a 20-second timeout. The newly integrated music module requires final compilation/build/runtime/device checks before claiming player delivery.

Do not commit `music.local.json`, personal music, downloaded tracks, tokens or login URLs. Copying media to the local persistent-data Music folder is for the user's device only, subject to the user's rights; nothing in this module authorizes public redistribution.

## Optional jazz and hip-hop configuration

`Tools/Music/music.example.json` contains explicit optional external-player URLs. Copy it to the persistent-data directory as `music.local.json`, then press Refresh. These station entries do not download or redistribute recordings with the game.

- SomaFM Sonic Universe (eclectic/avant-garde jazz): exact endpoint from https://api.somafm.com/sonicuniverse.pls ; broadcaster page https://somafm.com/sonicuniverse/ .
- One Station (hip-hop, soul, funk): external-player MP3 link published at https://onestation.fr/ecouter .

These are broadcaster-provided player links, not a grant to redistribute music. They inherit the same experimental Unity decoder/session bounds as other radio entries. Ambient/downtempo presets are not mislabeled as LoFi. No continuous 24/7 or mobile playback result is claimed.
