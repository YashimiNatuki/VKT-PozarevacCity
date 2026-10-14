# VKT / Velika Krađa Točkova — Požarevac City

A modular Unity action-game prototype with missions, a fictional ornamental garden economy, health, hazards and a configurable city clock. Source repository: [YashimiNatuki/VKT-PozarevacCity](https://github.com/YashimiNatuki/VKT-PozarevacCity).

This repository is the standalone Unity source milestone. The older [browser/Three.js project](https://github.com/YashimiNatuki/3D-gta-Pozarevac) remains separate. No browser source or history is replaced.

## Current milestone — 2026-10-14

- Three sequential contracts with pickup, delivery and contact confirmation; Standard, Express and Ghost challenges.
- Four persistent garden pots; seed inventory, paid purchases, watering, growth, harvest and harvested-item inventory.
- Eight fictional ornamental varieties. Five are available initially; three unlock at reputation 30, 60 and 100. Seed prices range from 60 to 420 game currency.
- Three buyer profiles and three district price modifiers. Quotes vary by plant, buyer and district; sales consume inventory and pay once. These are counter orders selected through the market UI, not physical delivery routes to spawned customers.
- Health and damage logic, cooldown-protected hazards, session death/respawn and mission retry handling. This is a foundation for encounters, not finished animated dog/gang/armed-NPC AI.
- A configurable 18-minute day and 12-minute night; fog and sun follow the clock. The game clock is separate from real-time garden care.
- Touch movement/look/action controls alongside desktop input. Real-phone usability and performance remain unverified.
- Synthetic city ambience and dialogue playback slots. Three natural Serbian character voice recordings are still absent; subtitles do not constitute delivered voices.

**Verification:** 66/66 Unity tests passed in the development project for this source milestone. Fresh portable Windows and Android builds are pending; neither an APK nor phone certification is claimed here. Existing earlier desktop builds do not verify this revision or the clean exported project.

## Open and generate the clean playground

Use Unity **6000.6.5f1** with Android Build Support, SDK/NDK/OpenJDK for Android work. Dependencies restore from `Packages/manifest.json`; Cesium uses its scoped package registry. The public export excludes original streamed-map scenes, embedded credentials and vendor assets. Fresh public PlayerSettings explicitly select the modern Input System (`activeInputHandler: 1`), VKT product identity and a 1280 × 720 desktop default; private development settings are not copied.

1. Open this folder as a Unity project and wait for package import/compilation.
2. Save any edited scene, then choose **VKT > Create portable offline playground**.
3. Open/generated `Assets/Scenes/VKTOffline.unity` and press Play.
4. Run EditMode tests through Unity Test Runner before building.

The generated fixture is a basic offline practice surface with the gameplay bootstrap, not a complete realistic Požarevac map. Original geospatial scenes stay in the private development workspace. No service token is required by the fixture.

## Standalone builds

The Editor entry points are `Pozarevac.CityGame.Editor.CityPortableBuild.CreatePlayground`, `.BuildWindows` and `.BuildAndroid`. Example batch build:

```powershell
& 'PATH/TO/Unity.exe' -batchmode -quit -projectPath $PWD -executeMethod Pozarevac.CityGame.Editor.CityPortableBuild.BuildWindows -vktOutput 'PATH/TO/Build/VKT.exe' -logFile 'PATH/TO/windows-build.log'
```

Use `.BuildAndroid` with an `.apk` output for Android. That builder selects ARM64, IL2CPP, Android API 28 minimum and landscape orientation, then restores the previous settings. These are development builds, not signed store releases.

Distribute the **whole Windows build folder**, including `VKT_Data`, `UnityPlayer.dll` and generated supporting files. Players launch `VKT.exe` without installing the Unity Editor. Android requires a successful APK build and real-device installation test. iOS requires a separate Mac/Xcode/signing pipeline and is not delivered.

## Source layout

| Path | Purpose |
| --- | --- |
| `Assets/CityGame/Runtime` | Gameplay, save, economy, health, UI, clock and input |
| `Assets/CityGame/Tests/Editor` | Deterministic and integration tests |
| `Assets/CityGame/Editor/CityPortableBuild.cs` | Reproducible offline scene and build entry points |
| `Tools/Audio/generate_voices.py` | Offline local Piper voice-baking tool; no automatic downloads |
| `Tools/Export-Source.ps1` | Allowlisted export; dry run by default |
| `Docs` | Milestone evidence, limitations and platform/audio plan |

## Next work

1. Build and smoke-test the clean exported Windows fixture; build/install Android on a real ARM64 phone.
2. Exercise purchases, harvest/sales, damage/respawn, save reload and touch controls on each target.
3. Add physical customer delivery routes and authored animated encounters with fair reactions and telegraphs.
4. Record or license three distinct natural Serbian actors and authentic city ambience, retaining provenance.
5. Validate geographic accuracy, collisions, street traversal and measured map-streaming performance before claiming realistic Požarevac coverage.

See [platform/audio readiness](Docs/Platform-Audio-Plan-20261014.md), [source publication boundaries](Docs/Source-Publication.md), and [third-party notices](THIRD_PARTY_NOTICES.md). MIT applies only to first-party material covered by [LICENSE](LICENSE); Unity and third-party dependencies retain their own terms.
