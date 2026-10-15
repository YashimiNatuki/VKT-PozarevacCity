# VKT / Velika Krađa Točkova — Požarevac City

A modular Unity action-game prototype with missions, a fictional ornamental garden economy, health, hazards and a configurable city clock. Source repository: [YashimiNatuki/VKT-PozarevacCity](https://github.com/YashimiNatuki/VKT-PozarevacCity).

This repository is the standalone Unity source milestone. The older [browser/Three.js project](https://github.com/YashimiNatuki/3D-gta-Pozarevac) remains separate. No browser source or history is replaced.

## Current milestone — 2026-10-15

- Three sequential contracts with pickup, delivery and contact confirmation; Standard, Express and Ghost challenges.
- Four persistent garden pots; seed inventory, paid purchases, watering, growth, harvest and harvested-item inventory.
- Eight fictional ornamental varieties. Five are available initially; three unlock at reputation 30, 60 and 100. Seed prices range from 60 to 420 game currency.
- Three buyer profiles and three district price modifiers. Quotes vary by plant, buyer and district; sales consume inventory and pay once. These are counter orders selected through the market UI, not physical delivery routes to spawned customers.
- Health and damage logic, cooldown-protected hazards, session death/respawn and mission retry handling. This is a foundation for encounters, not finished animated dog/gang/armed-NPC AI.
- A configurable 18-minute day and 12-minute night; fog and sun follow the clock. The game clock is separate from real-time garden care.
- Touch movement/look/action controls alongside desktop input. Real-phone usability and performance remain unverified.
- Synthetic city ambience and dialogue playback slots. Three natural Serbian character voice recordings are still absent; subtitles do not constitute delivered voices.
- A local mini music player with F6/mobile controls, local MP3/OGG/WAV and three optional official SomaFM presets. Radio decoding is experimental and bounded to ten-minute sessions; continuous 24/7 playback is not verified. Sixteen local audio files were counted through metadata only; personal music is not published.
- A 16-entry phone botanical encyclopedia: eight tradeable fictional varieties plus eight informational entries. The original illustrated atlas contains eight illustrations, not photographs.
- A bounded offline Požarevac center using 200 real OSM road segments and 40 building footprints, with original facade textures. Flat terrain, default widths and authored textures limit fidelity; this is not full-city photogrammetry.
- Repeatable three-chapter side jobs available after the campaign; their progress is session-only.

**Verification (2026-10-15):** 75 tests passed, zero failed and one optional ArcGIS-specific test skipped (76 total). The latest Windows development build succeeded with zero build errors (227,705,869 bytes). That exact player passed mission, physics interaction, garden, save/restore, damage/respawn, side-job payout, 200 OSM road segments, 40 footprints, 16 cards, music overlay and local audio decoding/playback checks. City facades and the corrected botanical card framing were captured and visually inspected. The portable builder uses Direct3D 11 after an earlier Direct3D 12 startup stall. Android ARM64/IL2CPP APK assembly succeeded: 51,984,055 bytes, API 28 minimum / target 36, APK v2 signature verified. No ADB device was attached, so installation, touch usability and phone runtime/performance are unverified.

## Actual Windows player captures

![Partial OSM center with authored facade textures](Docs/Screenshots/city-windows-20261015.png)

![Botanical phone card](Docs/Screenshots/botanical-card-windows-20261015.png)

## Map and art

The bundled map uses 200 OSM road segments and up to 40 real building footprints near 21.1854, 44.6199926, including Stari Korzo and Tabačka. **© OpenStreetMap contributors**, [ODbL 1.0](https://www.openstreetmap.org/copyright), applies to the derived JSON database. Heights use source tags/levels where available and explicitly marked defaults otherwise. Flat terrain, road widths, window placement and facade artwork are authored. This is a partial center, not a complete surveyed or photorealistic city.

Botanical art and facade textures are original AI illustrations. The botanical cards identify real analogues of fictional game plants and cite NC State Extension sources. Eight informational cards currently have no art. See [art provenance](Docs/Art-Provenance.md) and [map provenance](Docs/OpenStreetMap-Data.md).

## Open and play

Use Unity **6000.6.5f1**. Open this folder, allow package restore, save any edited scene, then choose **VKT > Create portable offline playground**. Open the generated `Assets/Scenes/VKTOffline.unity` and press Play. The generator creates its own URP settings and offline scene. Public source excludes private streamed-map scenes, credentials, vendor media and personal music.

Desktop: WASD/mouse, Shift sprint, Space jump, E interact, P phone/catalog/jobs, N next contract, R retry, H hospital respawn, F6 music. `-vktTouchPreview` enables the touch controls on desktop.

## Build standalone players

Windows batch entry point:

```powershell
& 'PATH/TO/Unity.exe' -batchmode -quit -projectPath $PWD -executeMethod Pozarevac.CityGame.Editor.CityPortableBuild.BuildWindows -vktOutput 'PATH/TO/Build/VKT.exe' -logFile 'PATH/TO/windows-build.log'
```

Distribute the whole Windows folder, including `VKT_Data` and supporting DLLs. Players launch `VKT.exe` without Unity Editor. Development diagnostic backup folders are excluded from the distribution ZIP.

Android needs Unity Android Build Support, SDK/NDK/OpenJDK. `.BuildAndroid` builds an APK with ARM64, IL2CPP, API 28 minimum and landscape orientation, then restores prior settings. For Windows installations requiring the existing OS trusted-root store, the split export/assemble script uses PowerShell 7:

```powershell
pwsh -File Tools/Build-Android.ps1 -UnityPath 'PATH/TO/Unity.exe' -OutputDirectory 'PATH/TO/AndroidBuild' -UseWindowsRootTrust
```

It exports through `.ExportAndroidProject` then invokes bundled Gradle with explicit launcher and daemon JVM flags. TLS verification remains enabled; no certificates are imported. See [build notes](Docs/Build-Notes-20261015.md). These are development builds, not signed store releases. iOS needs a separate Mac/Xcode/signing pipeline and is not delivered.

## Source layout

| Path | Purpose |
| --- | --- |
| `Assets/CityGame/Runtime` | Gameplay, UI, economy, save, map, music and input |
| `Assets/CityGame/Tests/Editor` | Deterministic/integration/geometry tests |
| `Assets/CityGame/Editor/CityPortableBuild.cs` | Reproducible offline scene and build entry points |
| `Tools/Build-Android.ps1` | Split Android export and Gradle assembly |
| `Tools/Audio/generate_voices.py` | Local Piper voice baking; no models or automatic downloads |
| `Tools/Export-Source.ps1` | Allowlisted export, dry run by default |
| `Docs` | Evidence, provenance, limitations and next work |

## Next work

1. Install/test the verified Android development APK on a real ARM64 phone: touch, save/resume, economy, damage, frame time, memory and thermals.
2. Add colliders/interiors, sidewalks/junctions and recognizable authored landmarks; current footprint shells are visual geometry over flat walking ground.
3. Add physical customer delivery routes and animated encounters with readable reactions.
4. Obtain three natural Serbian character voices with clear rights and authentic city ambience. Validate long-running radio; endpoint discovery does not prove 24/7 playback.
5. Persist street-job story/chapter progress, which currently resets each session, while cash and reputation persist.

See [milestone](Docs/Milestone-20261014.md), [platform/audio readiness](Docs/Platform-Audio-Plan-20261014.md), [music](Docs/Music.md), [source boundaries](Docs/Source-Publication.md) and [third-party notices](THIRD_PARTY_NOTICES.md). MIT covers first-party code under [LICENSE](LICENSE); OSM, Unity and dependencies retain their own terms.

Verification hashes and package metadata: [2026-10-15 receipt](Docs/Verification-20261015.json). Binary packages were produced locally; this repository publishes source and captures, not a binary release.
