# VKT desktop, mobile, and audio readiness — 2026-10-14

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
