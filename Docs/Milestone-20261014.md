# Source milestone — 2026-10-14

Status refreshed 2026-10-15: **75 passed, zero failed, one optional ArcGIS test skipped (76 total)**, including geometry-normal regression tests. Final Windows build: **zero errors, 227,705,869 bytes**. The exact final Windows-player runtime passed local MP3 decode/playback, 200 OSM roads, 40 extruded OSM footprints, 16 cards and side-job payout. Final city and encyclopedia captures were visually inspected; facade atlas and card aspect are correct, while the city remains a prototype. Direct3D 11 is selected after an earlier Direct3D 12 startup stall. Android split export/Gradle packaging succeeded after strict Windows-root TLS configuration; real-device installation/runtime remain unverified. Initial source commit `6cc06d2` was pushed; later changes require their own commit.

## Added in this milestone

- Eight fictional ornamental plant definitions, five initially unlocked and three reputation unlocks.
- Paid seed inventory, planting consumption, harvested inventory and variable buyer/district counter-order pricing. Physical delivery to customers is not implemented.
- Health, damage cooldown, session death/respawn and mission retry integration; not finished animated encounter AI.
- Configurable 18-minute day / 12-minute night, separate from real-time plant care.
- Desktop and mobile touch control paths; phone usability/performance pending.
- Synthetic ambience and offline voice-baking tooling. Real natural Serbian voices are absent.
- Portable Editor generator for a clean offline practice scene and Windows/Android development-build entry points.
- Allowlisted public-source export with credentials/scenes/vendor media excluded and first-party MIT license boundary.
- Local mini music player, F6/mobile controls, 16 audio files counted locally through metadata only, and three official SomaFM preset endpoints. No personal music published; live decoding experimental and bounded to ten-minute sessions, not verified 24/7.
- Phone botanical encyclopedia with 16 entries: eight tradeable varieties and eight informational entries. First-party atlas artwork has eight illustrations, not photographs.
- Bounded real OSM road/footprint layout with authored facade textures in the offline scene; no full architectural accuracy claim.
- Repeatable three-chapter side jobs after the campaign; progress is session-only.
- Approximately 200 OpenStreetMap-derived street segments around the verified town center, including Stari Korzo/Tabačka. Dataset attribution: © OpenStreetMap contributors, ODbL; independent first-party code remains within its own MIT scope.

## Delivery boundary

The new repository is `YashimiNatuki/VKT-PozarevacCity`; the coordinator confirmed initial source commit `6cc06d2` was pushed. Preserve the separate browser repository `YashimiNatuki/3D-gta-Pozarevac` unchanged.

The public source scene is generated from C# with the bounded OSM layout and authored facade textures. It is not the original streamed city, not a complete modeled Požarevac and not evidence of geographic accuracy. The full local project remains the working integration source.

## Next acceptance gates

1. Export current source and restore dependencies in the clean project.
2. Generate the practice scene; compile and run all source tests there.
3. Produce a new Windows player and exercise the economy/health/save loop.
4. Build APK; install and verify the loop, touch input, resume/save and performance on a real Android device.
5. Add physical delivery encounters, finished art and reviewed natural Serbian actor recordings.

Update build/push status only from actual logs, artifacts and remote commit evidence.


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
