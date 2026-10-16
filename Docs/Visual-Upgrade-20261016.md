# VKT visual and UI upgrade — 2026-10-16

The previous milestone expanded data coverage but retained placeholder geometry. This wave changes the visible presentation in the existing portable Unity game; it does not claim a finished photoreal city.

## Geometry and materials

- Shared smoothly rounded box mesh replaces selected bench, counter, held-equipment and window-frame visuals. Existing interaction/collision ownership remains intact.
- Four garden boxes become revolved ceramic planters with a lip, inner wall and soil surface. Mesh generation is bounded and happens once; instances share meshes/materials. The garden's IDs, timers, payments and saves are unchanged.
- Convex building footprints gain authored pitched roofs; concave/invalid cases retain the existing bounded triangulation behavior. Roof shapes are artwork, not surveyed architectural evidence. They do not introduce walkable roof collision.
- Three original procedural 256×256 mipmapped surfaces provide asphalt, paving and grass variation with meter-based repeating UVs. This is authored texture detail, not real photographs or scanned PBR materials.
- Native render scale 1.0 replaces 0.85, MSAA 4 replaces 2. A procedural sky, soft directional shadows and modest URP ACES/color/bloom/vignette grading provide a consistent look. Extra GPU cost must be measured on target phones; no frame-time improvement is claimed.

## Character asset and licensing

The reviewed **Kenney Animated Characters Retro 1.1** subset contains characterMedium.fbx, idle.fbx, run.fbx and humanMaleA.png plus the original License.txt. Primary source: [Kenney](https://kenney.nl/assets/animated-characters-retro). License: **CC0**, as stated on the creator's page and inside the downloaded archive.

Archive SHA256: `1D03F1FB001F3CF629425B69C898DF58261A316616220EA25B58BD35298E6882`. The raw archive is retained outside Git; only the reviewed subset and generated Unity metadata are published. Models/texture/license are not relicensed as first-party MIT code.

A narrowly scoped importer configures only these reviewed files for legacy animation and disables material import. The runtime assigns its own URP material with the supplied skin, normalizes the visual to 1.8 m, and selects idle/run motion from observed actor movement. No root-motion authority, new networking dependency or player-save migration is introduced. A procedural human remains the missing-asset fallback; the guard dog uses an authored rounded multi-part visual. Neither is a likeness of the user. Actual player assertions confirmed the imported actor loads. Animation binds to the reviewed rig Root. Scoped removal of empty-path transform curves, a normalization wrapper and scale-aware BakeMesh measurement prevent deformation. Relaxed arm posing removes the stock A-pose while preserving bone lengths.

## UI and readability

HUD uses separate rounded profile, mission and vitals cards, a health bar, shorter identity text and DIN currency labeling. Prompt/notification backgrounds disappear with their text. Touch movement uses arrows; phone/retry/next/hospital controls move to a compact top-center row instead of covering equipment/vitals. Phone/buttons share owned nine-slice UI styling. Garden signage is condensed to the current selection; full details remain in the phone. Encounter/equipment labels are reduced and face the active camera.

The map, pause ownership, mission/garden/combat/save rules remain the existing systems. UI layout is verified in PC captures; physical-phone safe areas, finger ergonomics and performance remain unverified.

## Verification

Final verification: 105 tests passed, zero failed, one optional ArcGIS test skipped (106 total). Windows build: zero errors, 228,300,069 build-report bytes. Three actual Windows player runs passed gameplay, saves, audio and visual assertions; actor/city runs also passed physical combat. All three captures were inspected. Android ARM64 IL2CPP development APK built successfully; v2 signature verified. No Android device was attached. Phone runtime/performance remain unverified. See Verification-Visual-20261016.json for hashes. Natural Serbian voices, driving/traffic and scanned terrain remain outside this visual wave.
