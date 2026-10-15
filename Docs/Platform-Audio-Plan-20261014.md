# VKT desktop, mobile, and audio readiness — 2026-10-14

> Earlier city/art/audio baseline published as 9a96f7f. The newer [gameplay milestone](Gameplay-20261015.md) supersedes session-only street progress and adds collision/journal validation.

## Status refresh — 2026-10-15

Final source checks: 75 passed, zero failed, one optional ArcGIS-specific skip (76 total). Final Windows player built with zero errors, 227,705,869 bytes. The exact final Windows player passed local audio decoding/playback, 200 OSM road segments, 40 extruded OSM footprints, 16 encyclopedia cards and side-job payout. Both final screenshots were visually inspected: facade atlas and encyclopedia card aspect are correct. The city remains a prototype. Direct3D 11 is selected after an earlier Direct3D 12 startup stall.

Android progressed through ARM64 IL2CPP and Burst before Gradle failed Java certificate validation. Both official Google Maven AGP 9.1.1 POM endpoints returned HTTP 200 through Windows, while the bundled JDK failed with SSLHandshakeException/ValidatorException. The same JDK returned HTTP 200 using existing Windows trusted roots with `javax.net.ssl.trustStoreType=Windows-ROOT` and `javax.net.ssl.trustStore=NONE`. This preserves TLS/hostname validation, imports no certificates and disables no checks. The subsequent split export/Gradle packaging succeeded; real-device results remain unverified.

The direct Unity packaging retry still failed because Unity's child invocation supplied heap-only daemon JVM arguments, overriding the generated trust configuration; the JAVA_TOOL_OPTIONS path did not reach that child as expected. A direct bundled Gradle `help` probe with explicit launcher and daemon Windows-root flags succeeded (exit zero, 43 seconds). The new `Tools/Build-Android.ps1` uses PowerShell 7, exports with `ExportAndroidProject`, and invokes the bundled Gradle separately with both sets of explicit flags. The actual APK attempt through this route succeeded; this is packaging evidence, not a device pass.

`BEE_BUILD_THREADS` defaults to four only when unset in the Editor build path and is restored afterward. See [Unity release notes documenting the build thread control](https://release-notes.ds.unity3d.com/search?from_release=6000.0.32f1&to_release=6000.0.71f1) and [build notes](Build-Notes-20261015.md).

Next delivery gates remain natural Serbian voices, actually verified continuous radio, Android device usability/performance and persisted story/side-job chapter progress. Existing synthetic ambience, finite local MP3 playback and experimental bounded radio are distinct capabilities.

## Verified local baseline

- Unity 6000.6.5f1; Cesium 1.26.0; embedded ArcGIS Maps SDK 2.4.0.
- Windows, Android, WebGL, Linux and UWP playback modules exist. Android SDK, NDK and OpenJDK directories exist. This is installation evidence, not an Android build or device test.
- Android currently uses IL2CPP, ARM64 and minimum API 26. ArcGIS 2.4 documentation lists minimum API 27; raise the minimum in an explicit mobile build profile and verify on a device. The documentation labels that minimum as Android 9/API 27, so confirm the effective SDK requirement during target build rather than silently assuming the label is consistent.
- ArcGIS 2.4 certifies Unity 6.0/6.3/6.5; the current 6.6 editor is outside that published matrix. Existing desktop build evidence does not certify mobile SDK compatibility.
- Current audio assets contain Footstep.wav and Land.wav. The three configured NPC voice files are absent. No natural voices have been delivered by this research pass.
- Bundled Python works; Python on PATH is a Windows Store stub. Piper is absent from bundled Python. No models were installed or downloaded.
- Neither this project nor the initial working directory had Git metadata at inspection; no remote destination could be inferred. Git is available; gh is absent from PATH. Do not publish to an invented repository or bundle geospatial credentials.

## Practical delivery route

Keep Unity and the existing C# gameplay. Ship a complete Windows player folder: executable, VKT_Data, UnityPlayer.dll and generated supporting files. Players launch the executable without installing the Unity Editor. Streamed map access can still require internet and service authorization.

Android is the next practical native target: ARM64 + IL2CPP, Internet Require, URP mobile quality, touch move/look/action controls, safe-area HUD, pause/resume save, explicit lower map cache/LOD budgets. Prefer one active geospatial renderer per profile. Do not remove ArcGIS solely to force a build; isolate optional SDK integration with tested assembly/platform boundaries if it is not required for gameplay.

Acceptance: APK build, install on a real ARM64 phone, complete one contract and seed purchase/plant/harvest/sale loop, test damage/respawn, pause/resume/save, ten-minute frame-time/memory/thermal observation, and map attribution/network failure checks. No Android readiness claim until this passes. iOS requires a separate Mac/Xcode signing pipeline; this Windows installation has no iOS module. A Web build adds native threading/header requirements and ArcGIS compatibility work, so native Android precedes a browser port.

## Offline Serbian dialogue baking

`Tools/Audio/generate_voices.py` is a local-only authoring module. It accepts an explicit three-slot JSON model map, validates local model/config/speaker indices and license acknowledgment, invokes Piper with a bounded subprocess, validates WAV output, and preserves existing clips unless `--overwrite` is selected. It never installs software, accesses an API, downloads models, or runs TTS on the player's phone.

Example map entry (all three slots required):

```json
{
  "mila_enter": {"model": "D:/ReviewedVoices/mila.onnx", "speaker_id": 0, "license_reviewed": true},
  "vuk_exit": {"model": "D:/ReviewedVoices/vuk.onnx", "speaker_id": 0, "license_reviewed": true},
  "luka_complete": {"model": "D:/ReviewedVoices/luka.onnx", "speaker_id": 0, "license_reviewed": true}
}
```

Use `python generate_voices.py --voice-map voices.json --check` to validate configuration, then omit `--check` in a reviewed Piper environment. Import generated WAVs through Unity; keep spoken subtitles synchronized with the final text. Listen to every line for Serbian pronunciation and distinct speakers before delivery.

The available Serbian Piper `serbski_institut` model has **two speakers**, not three. Its own model card lists a **CC BY-NC-SA dataset**, despite the umbrella repository MIT label; it is therefore not a clean unrestricted commercial-content choice. Piper's current engine is GPL-3.0. Do not claim three distinct natural actors by changing pitch. For the full three-character cast, source three consented actor recordings or separately licensed Serbian models; retain a per-clip provenance/license ledger. Voice generation quality and rights remain unresolved until that is done.

City ambience should use separately licensed day/night city beds and spatial one-shots, with mixer buses for ambience/effects/dialogue, dialogue ducking, pooled capped emitters and indoor attenuation. Procedural ambience can supply a placeholder without download rights issues, but is not an authentic recorded Požarevac soundscape. Record clean original city beds or acquire CC0 recordings and attach provenance before replacing placeholders.

## Primary sources checked

- [Cesium 1.26 supported platforms](https://cesium.com/learn/cesium-unity/ref-doc/supported-platforms.html): Android IL2CPP ARM64/x86-64, no ARMv7; native Windows and iOS players; Web threading/server headers.
- [ArcGIS 2.4 system requirements](https://developers.arcgis.com/unity/system-requirements/system-requirements-2-4-0/): exact embedded SDK version, certified editors and mobile requirements.
- [Piper engine and GPL license](https://github.com/OHF-Voice/piper1-gpl), [Piper CLI](https://github.com/OHF-Voice/piper1-gpl/blob/main/docs/CLI.md).
- [Serbian model card](https://huggingface.co/rhasspy/piper-voices/blob/main/sr/sr_RS/serbski_institut/medium/MODEL_CARD): two speakers, 22,050 Hz, dataset license.

Research/tool authoring only: this document does not assert an Android build, natural voice delivery, or GitHub push.

## Final PC evidence refresh — 2026-10-15

`vkt-verified-tests-20261015.xml`: **75 passed, zero failed, one optional ArcGIS-specific skip / 76 total**. Final Windows build: **zero errors, 227,705,869 bytes**. Both `vkt-verified-city-player` and `vkt-verified-cards-player` runtime reports returned true for the checked paths: contracts, physics, garden/save/respawn, street-job payout, 200 OSM roads, 40 building footprints, 16 cards, music controls and local audio decoding. Final city/cards screenshots were visually inspected; facade atlas and card aspect are correct. This verifies the prototype checks, not full-city completeness or photorealism.

PC ZIP: **105,841,466 bytes**. SHA256: `8CAEB69EAE49443C0C6CBCFE88809BE4A5D68B5DFB99667694CE2C11B02DD251`.

Android split export/Gradle APK packaging succeeded. No ADB device was attached, so installation and real-device verification remain pending. Natural Serbian voices, verified uninterrupted radio, measured phone performance and persisted chapter progress remain next delivery gates.

## Verified Android APK — 2026-10-15

- Unity native/Gradle export: **Succeeded, zero errors**.
- Direct bundled Gradle assembly: **BUILD SUCCESSFUL in 9m54s**.
- `VKT.apk`: **51,984,055 bytes**; SHA256 `0C8C43BC45C968CC94C81D2364BA6C147FDF40DCF34FEE5021FE64923D496BFA`.
- Package `com.yashimi.vkt`; version code **1**, version name **1.0**; minimum API **28**, target API **36**.
- ABI **arm64-v8a**, IL2CPP; actual APK contains `libunity` and `libil2cpp` native libraries.
- `apksigner verify`: v2 signature **true**, **one signer**. This is a **debuggable development APK**, not a signed store release.
- No ADB device attached: **no installation, phone runtime/performance pass or store-delivery proof**.

Historical failed Unity-child trust attempts remain documented above. The successful artifact used the separate native export/direct Gradle path with strict Windows-root TLS validation. Final source commit/publication remains the coordinator's pending step; no later commit is asserted here.
