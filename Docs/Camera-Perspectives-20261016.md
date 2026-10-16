# VKT camera, scale and Android access — 2026-10-16

## Playable perspectives

Press **C** on desktop or tap **KAMERA** on Android to cycle first person, shoulder and chase. V remains the existing seed/strain selection key. The imported CC0 character is visible in third person, with movement-driven idle/run animation. First person hides the character and retains the held-equipment view; third person hides the first-person weapon overlay. A full third-person weapon-holding animation is not included.

The character controller stays 1.8 m tall, radius 0.32 m, eye height 1.62 m. The reviewed character visual normalizes to 1.8 m. The OSM world remains in meters: no save coordinates, travel distances, mission anchors or map scale are resized. This wave does not expand geographic coverage.

The shoulder offset is (0.65, 0.25, -2.6) m and chase offset is (0, 1.1, -5) m relative to the camera orientation and eye position. A bounded sphere cast retracts the camera before solid obstacles and ignores the owner's colliders and triggers. A saturated cast retracts to the eye instead of trusting incomplete results. Camera switching is presentation state, not persisted progression.

Interaction and combat originate at the character eye. In third person, the camera center ray determines the aim target, followed by the existing eye-origin range and obstruction checks. Camera distance does not add attack or interaction reach. The camera uses reusable hit buffers rather than per-frame query allocations.

## Visual/UI detail

The hideout entry retains its original collider and interaction component; its box renderer is replaced with rounded door panels, frame, knob, step and canopy. The label is smaller. The 24 existing trees gain three overlapping canopy lobes each, with the same trunk/placement exclusions. Existing city coverage remains 600 selected roads, 100 footprints and 22 green polygons.

Touch controls now sit inside a safe-area wrapper updated when screen size/cutout bounds change. Desktop captures do not establish real-phone ergonomics. No extra service dependency or paid asset is introduced. The game-dev CLI is unavailable on this machine, so no game-dev package, GPU receipt or provider execution is claimed.

## Android installation

Use the GitHub release linked in README, download VKT.apk in the phone browser and open it. If prompted, permit installation from that browser. The development APK requires ARM64 Android 9/API 28 or newer. Keep normal platform verification enabled. This is a sideloaded development preview, not a Play Store release.

The older visual-only release remains available under vkt-visual-20261016. It predates these camera changes. The perspectives release uses a separate tag and artifact receipt.

## Validation boundary

See Verification-Perspectives-20261016.json for final tests, actual-player evidence and artifact hashes. Real-phone gameplay, GPU performance, touch safe-area behavior on hardware and subjective Serbian voice quality require device testing. Flat terrain, incomplete landmark models, traffic/driving and natural recorded dialogue remain unfinished.
