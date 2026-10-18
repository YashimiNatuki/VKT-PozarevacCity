# Physical Android validation and audio — 2026-10-18

## Result

Android **version code 6**, package `com.yashimi.vkt`, ARM64 IL2CPP, Android 9+; target SDK 36. APK build: zero errors. Signature matches the previous preview, seven ARM64 libraries including Cesium are present, and the original intro MP4 is byte-identical to the explicit input. Installed on Xiaomi **2409BRN2CY**, Android API **36**, Mali-G52 MC2, **1640x720**. Compatible game data survived upgrade; a verified backup of the external Unity profile directory remains local.

Actual phone checks: city startup, wardrobe full-body/portrait view, music panel open/close, camera switch, held movement and background/resume. The user separately confirmed manual movement/wardrobe and audible intro. ADB input initially required the phone's security debugging option; after user enablement, 250ms held touchscreen presses reliably drove the Unity UI. No other app was captured or controlled.

The new source has **181 Unity tests passed, 0 failed, 1 optional ArcGIS skip**. A layout regression verifies that the touch music launcher clears jump/action rows and vitals. The final APK's measured active-gameplay window contains **1800 frames**, average **36.74 FPS**, p95 **33.36 ms**, audio configuration **24000 Hz**. This is a short USB-connected development-build sample containing standing and brief movement, excluding menus/background/intro. It does not prove sustained thermals or stable 60 FPS. Raw metrics keep their human-review flags false; separate review evidence records the user's actual feedback.

## Bugs found and fixed

The first hardware run (private v4) played the intro but failed city startup: `CreatePrimitive` could not construct `SphereCollider`/`CapsuleCollider`, followed by a `CityCombatRuntime.Primitive` null reference. A targeted `Assets/CityGame/link.xml` preserves those two types without globally disabling stripping. v5 and v6 reach `CITY_GAME_READY` without that failure. The exporter now includes the exact linker file and its meta so a later clean source export retains the fix. [Unity primitive reference](https://docs.unity3d.com/ja/6000.0/ScriptReference/GameObject.CreatePrimitive.html) and [link.xml format](https://docs.unity3d.com/6000.0/Documentation/Manual/managed-code-stripping-xml-formatting.html).

The phone screenshot also revealed that the old music launcher overlapped SKOK. v6 uses a separate touch position and the mobile label MUZIKA; the corrected layout and music overlay were exercised on the phone. Desktop retains its existing F6 location.

One native startup E/Unity line reports a missing `com.google.android.play.core.assetpacks.AssetPackManager` class. The installed base APK continues into the tested city/UI flows. This remains an observed non-blocking message with unproven cause; Play Asset Delivery/AAB behavior has not been validated. It is recorded rather than hidden or described as zero runtime errors.

## Sound and voice

The opening movie has audio; the user heard it on this phone. Raise **media volume**, check Bluetooth/headphone routing, and leave the intro playing to check its track. Gameplay has generated environmental/effect audio. Tap **MUZIKA** for the separate music controls; desktop M toggles gameplay sound and F6 opens music.

There are **zero spoken NPC recordings in this APK**. Mila/Vuk/Luka events currently show Serbian captions. Their expected Resources clips (`Audio/Voices/mila_enter`, `vuk_exit`, `luka_complete`) are absent. The rejected robotic Piper candidate and the unapproved VITS listening candidate remain outside the game. No TTS engine, model weights or API key is bundled.

## Next priorities

1. Tune an explicit mobile quality profile and measure 10–15 minute travel/thermal sessions.
2. Record/review a natural Serbian voice bank and expose voice availability in audio settings.
3. Apply expression-state logic to a rigged 3D head; see Character-Expression-Roadmap.md.
4. Consolidate all mobile menus, safe areas and large-text behavior; the lifestyle panel still needs a responsive pass.
5. Investigate the AssetPackManager startup message and expand real-device mission/traffic/collision traversal tests.
