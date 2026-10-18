# Gameplay expressions and audio diagnostics - 2026-10-18

Android version code 7 and Windows development preview extend the same CityGame systems. No external model service, private companion assets, new paid dependency or save-schema change was introduced.

## Changes

- The walking avatar now chooses calm, focus, effort, success and hurt from actual vitals, wanted/exertion state and mission-feedback cues. Hurt has priority, then effort, focus and success. Timers use gameplay time; rebinding/disabling releases event subscriptions and clears transient state. Muting audio does not suppress cosmetic mission feedback.
- Wardrobe: BLOK / LIK -> garderober -> LICE / CEO LIK, then IZRAZ LICA cycles five expressions. Preview and blinking use unscaled time while the wardrobe owns pause. This preview changes neither health nor progression nor the saved outfit.
- MUZIKA / F6 now shows gameplay sound and the actual loaded voice count. PROBA ZVUKA plays a short original generated tone while the menu pauses gameplay. ZVUK IGRE mutes gameplay sound separately from music. Mute, menu close, background and component disable stop the diagnostic source. The launcher hides while the panel is open to avoid covering controls; status updates are bounded to 5 Hz.

## Verification

186 Unity EditMode tests passed, zero failed, one optional ArcGIS test skipped. New tests cover expression priorities, expiry, paused/invalid time, finite poses, vitals subscription ownership and unavailable diagnostic audio. The Windows player exercised all five paused expression previews, captured the portrait/success face, and verified paused probe playback, mute-stop and menu-close cleanup alongside the existing mission, boxing, save-resume, intro and acoustic smoke checks.

Android ARM64 IL2CPP APK: zero build errors, version code 7, min SDK 28 / target SDK 36, compatible signing certificate and byte-identical original intro. Installed as an update on the connected Xiaomi 2409BRN2CY, with an external save backup taken before installation. Phone disconnected from ADB after installation/launch: new v7 UI/audio/FPS checks remain pending. Previous v6 hardware checks are not v7 evidence. Exact artifacts/hashes are in Verification-Expression-Audio-20261018.json. Raw logs and profile backups stay private.

## Limits and next work

The head remains the existing low-poly Kenney mesh and painted atlas. These are shader expressions, not a new realistic facial rig, speech lip sync or a replacement for detailed geometry. Zero NPC voice clips are bundled; the panel states this explicitly. The AssetPackManager message observed on v6 remains an open issue; v7 device logs were unavailable after disconnection. No new subjective voice approval or sustained thermal/stable-60-FPS claim is made. Windows is the primary development target; Android adaptation remains supported in later verification waves.

Next priorities: (1) licensed facial rig with eyelid/jaw blendshapes and mobile LODs; (2) reviewed natural Serbian voice bank and speech-driven mouth animation; (3) responsive mobile menu/safe-area pass; (4) measured mobile quality/thermal profiles; (5) longer city traversal and mission/resume/collision regression; (6) investigate the native AssetPackManager startup message.
