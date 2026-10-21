# Master Blueprint: integration review, 2026-10-21

## Provenance and delivery boundary

The original [Master Blueprint](VKT-Master-Upgrade-Blueprint.md) was read in full and imported unchanged from commit `8a4be21ab663a8bfa463376f4031944849744955` in `C:\Users\jamal\Documents\antigravity\radiant-kepler\VKT-PozarevacCity`.

That checkout contains an additional runtime commit, `f0b4291`, based on `75928ba`. It is a parallel development candidate, not the tested Garden Orders Windows release. The active release baseline is `75401b3e2e661ddc28b8950dc0d3f072076da8cf`; its courier inventory, profile v8, checkpoints and single settlement must be retained. Do not push the older checkout over main, reset main to it, or import its bootstrap wholesale. Both original checkouts are preserved.

The blueprint's "finished" labels for pedestrians and skateboard refer to that parallel candidate. They do not establish working features in the current Windows player. Its numerical claims about immersion and other indie projects are aspirations, not measured results.

## Source findings before any runtime integration

| Candidate | Observed source behavior | Required correction and acceptance |
|---|---|---|
| CityVehicle / CitySkateboard | ApplyPhysics changes a speed scalar, but does not move the rider or steer. Skateboard Update follows the rider; verticalVelocity is not applied to displacement, and StepPhysics always receives grounded=true. | One movement owner using existing player collision/input gates; real ground probe, acceleration, braking, steering, mount/dismount and jump displacement. Verify walls, slopes, death, menus, training and safe exit in a native Windows player. |
| CityPedestrianSpawner | SpawnPedestrian creates state only; VisualObject is never created here. Position uses MoveTowards without a collision/navigation query; danger is passed as false. | Visible bounded pooled actors on walkable routes; no passage through walls, correct pool lifecycle and existing whistle/police reaction integration. Validate rendered actors and route traversal, not only count tests. |
| CityMinimap | Separate Canvas, built-in LegacyRuntime font, catches all target exceptions and assumes old station objectives. | Reuse current UI typography/safe areas and mission ownership, include profile-v8 garden delivery/return targets, verify heading orientation and no touch/HUD occlusion. |
| Player stats / screen feedback | Added by parallel commit; not imported or validated in this review. | Audit against existing vitals/lifestyle/progression and avoid a second authority for health, XP, cash or saves. |

These are source findings, not a claim that the parallel candidate has been run or that its complete test suite fails. Its tests and native behavior still need a separate intake pass. No candidate runtime file or dependency is activated by this documentation change.

## Technology decisions

- Installed baseline: Unity 6000.6.5f1, URP 17.6.0, Input System 1.20.1, Cesium 1.26.0 and Test Framework 1.8.0. AI Navigation, Cinemachine, Splines and DOTween are not listed in the current manifest. Add a package only for a tested consumer, pin the resolved compatible version, and retain the existing camera/input behavior during migration.
- AI Navigation provides character navigation meshes, links and dynamic obstacles. Road lanes, right-of-way, traffic lights and vehicle steering need their own gameplay model; installing NavMesh does not implement traffic. Documentation reviewed: AI Navigation 2.0.16, 2026-10-21: https://docs.unity3d.com/Packages/com.unity.ai.navigation@2.0/manual/index.html
- Decals are supported by URP but require the renderer feature and decal materials. Start with an atlas/shared materials and measure render cost. They are not new building geometry. Documentation reviewed 2026-10-21 (Unity 6000.0 reference; validate against installed URP 17.6 before changing renderer assets): https://docs.unity3d.com/6000.0/Documentation/Manual/urp/renderer-feature-decal.html
- Copernicus GLO-30 is a surface model containing buildings and vegetation, not bare-earth terrain. Its 30 m grid can guide broad relief but cannot resolve individual curbs or street-level grades. Check tile coverage, missing values, horizontal/vertical reference, attribution and known control points; filter artifacts before aligning roads and colliders. RAW export must specify dimensions, byte order, scale and origin. No Požarevac elevation tile was downloaded or validated in this review. Official collection description reviewed 2026-10-21: https://dataspace.copernicus.eu/explore-data/data-collections/copernicus-contributing-missions/collections-description/COP-DEM
- Reuse the existing music player, hideout priority/mute logic and audio ownership for fictional stations; station names/frequencies are design labels, not verified licensed live streams. Use original or licensed recordings. Natural Serbian speech remains a listening/approval task; retired VITS tooling stays disabled.
- Blender, recorded voices and reviewed assets fit the free/local preference. Check each model/tool license independently; "free to download" does not imply open source or redistribution permission. Existing Unity UI transitions are the first reuse choice before another tween dependency.

## Next ordered slices

1. **Movement and body contact:** repair limb phase, feet/ground contact, bench hand/bar contact and boxing guard with current controls. Check walk/run/turn/training/menu return, rendered pose and collision before adding transport.
2. **One complete skateboard encounter:** intake the parallel candidate, fix movement ownership and jump grounding, place a visible board with E mount/exit, then verify a short delivery with current save/mission gates. Keep it unavailable until the complete flow passes.
3. **Visible pedestrians and minimap:** integrate corrected pooling/navigation and current mission targets; test wall avoidance, whistle reaction, marker direction and UI layout. Preserve phone/map/music behavior.
4. **One recognizable central block:** verified references for Načelstvo/park/Korzo, measured scale, hero facade geometry, LOD and collisions. Import a small verified relief patch separately before expanding Čačalica or other districts.
5. **Connected contacts and sound:** Rođa → Kure objective with an authored distraction encounter, subtitles, reviewed short Serbian recordings, footsteps/traffic mix and one-time rewards after reload. Reuse the delivered garden order authority.
6. **Then vehicles and wheel-recovery mission:** one vehicle, clear interaction prompts, abstract game minigame, inventory/vehicle state and police consequences. Validate persistence and collision before additional fleet models or campaign branches.

Each runtime slice requires its own focused tests, Windows build, native flow and documented unverified boundaries. The existing 209 passing EditMode tests, one skipped test and Garden Orders native checks describe the baseline only; they do not certify these future slices. Android and iOS are not tested by this review.
