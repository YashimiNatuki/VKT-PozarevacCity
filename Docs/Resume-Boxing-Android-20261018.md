# Mission checkpoints, boxing and Android — 2026-10-18

## Changes from the routine release

Active campaign routes and street jobs previously restarted/vanished after loading. Profile **v7** now captures the contract/challenge, attempt, stage, remaining game time, deterministic offer seed and contact cooldown. Cargo/receipt is derived from the restored phase. Offline wall time does not consume a mission deadline. Player arrival still follows the existing spawn/checkpoint; arbitrary world-position persistence is not added. Successful handovers and contract transitions request an immediate save; periodic saves also capture clocks, suspend and quit.

Balances, terminal mission state and completed counts share the existing checksummed, atomic profile transaction. Restoring a completed contract or settled job never issues a reward. A reward observer that snapshots before the director completion callback still receives the terminal campaign count. Existing stale-writer rejection, backup recovery and preservation of unreadable/newer saves remain in force. Local profiles are not an online authoritative ledger or tamper-proof economy. Restoring a backup rolls back its balances and mission state together.

Versions 1–6 migrate without losing gardens, clothing or lifestyle progress. The Unity JSON inline-null representation is explicitly handled; an all-zero checkpoint means absent, while contradictory non-empty state fails validation. Existing save fields and original scene GUIDs are retained.

## Boxing in the gameplay routine

At the gym use **L → BOKS / reakcija i gard**, or the touch lifestyle menu. Food >=20%, stamina >=35%, shared rest complete, no active delivery/campaign route or pursuit. A capsule bag and stand mark the training area.

Eight timed rounds alternate **J / UDARAC** and **K / GARD**, each with a preparation interval and a 650 ms response window. Each round consumes only one answer; early, wrong and repeated input cannot farm success. At least six correct rounds award **30 XP once**. Every three completed boxing sessions add one endurance level, capped at five. Endurance reduces sprint stamina drain separately from bench strength. Food and stamina costs apply at completion; the durable 90-second shared rest starts at entry. Crime, death, leaving the area, cancellation or failure cannot pay. A workout itself is transient across reload; its earned session count and rest persist.

The standing imported character gains an additive guard/jab pose. A dedicated collision-aware training camera reveals the avatar even when entering from first person, releases the cursor for buttons, and restores the prior camera mode. This is a procedural training pose, not a newly motion-captured boxing animation or opponent sparring system. Combat, movement, contact transactions, wardrobe and local-story entry are blocked during boxing. Pause/application suspension freezes the reaction clock.

## Police, surfaces and city detail

Patrols move between four local stops with dwell time. On a newly reported incident they investigate its fixed location. Line of sight updates the last known position; occlusion stops those updates. At the location they search a bounded four-meter area and reacquisition resumes pursuit. A restored warrant alone does not reveal a hidden player's current position. Contacts still refuse a wanted player. This remains local wall-aware foot steering, not a complete baked city navigation graph, witness network or traffic AI.

Footsteps resolve pavement versus grass from the existing OSM road/park geometry, with roads taking precedence. Authored platforms can override the surface using `CityFootstepSurface`. Original synthesized grass/wood contacts are bounded mono clips. Footsteps and the patrol siren duck during dialogue; global mute/pause/suspension behavior is retained. These are synthesized sounds, not field recordings or a certified surround/HRTF mix.

Up to sixteen nearby building footprints gain original round cornices and arched door trim. The meshes share a material, participate in the existing distance visibility budget, and add no colliders across the authored doorway. OSM footprints/origin and existing wall collision stay authoritative. These decorations improve surface relief; they do not claim a surveyed reconstruction of the municipality or museum.

## Real elevation intake and voice review

The actual Copernicus GLO-30 tile covering the area was downloaded to the private toolchain cache and eight candidate points sampled. [Audit receipt](Elevation-Audit-20261018.json) contains the source URL, SHA256, CRS, spacing and sample neighborhood ranges. Vertical reference is provider-documented EGM2008 orthometric meters. Point names/coordinates remain user candidates, not independently surveyed locations. The runtime heightfield stays disabled: roofs/trees in a DSM cannot safely replace street ground or be mixed with Cesium ellipsoid heights without datum conversion and common placement/collision sampling.

Sources: [Copernicus product and vertical reference](https://dataspace.copernicus.eu/explore-data/data-collections/copernicus-contributing-missions/collections-description/COP-DEM), [AWS public-data registry and licence](https://registry.opendata.aws/copernicus-dem/), [COG tile structure](https://copernicus-dem-30m.s3.amazonaws.com/readme.html). Copernicus DEM GLO-30 was accessed on 2026-10-18; preserve provider licence/attribution when distributing derived terrain. The source raster is not committed or packaged.

A free local Piper runtime and a pinned Marko candidate model were installed outside the source tree and a short Serbian dialogue candidate generated. Model/engine metadata and hashes remain in the private voice intake receipt. The publisher's model card has an inconsistent language description, so synthesis success is **not** a Serbian pronunciation/naturalness certificate. No unreviewed voice or shifted-pitch imitation of several actors is silently added to this release. The user reviewed the Piper candidate as understandable but robotic, so it is rejected for game admission. A second Serbian VITS candidate was generated successfully in an isolated environment; the user could not listen yet, so it also remains outside the game. See [voice review](Voice-Review-20261018.md).

## Android and verification boundaries

The Android build uses ARM64/IL2CPP, minimum Android 9, landscape and package `com.yashimi.vkt`; version code increases to 3. The original MP4 is an explicit local build input, staged without overwriting existing media, packaged inside the APK and removed from the public working tree after the build. The same movie bytes are retained in the Windows player. No Unity Editor is needed to play either build. Development APK signing is for testing, not a Play Store production submission.

Development Android players collect a local rolling window of up to 1,800 gameplay frame intervals, observed FPS, p95 frame time, touch press count and audio output sample rate. Intro, menu pause and suspension are excluded, as is the first resume frame. Long gameplay frames remain in the measurement. Data is written to `vkt-device-metrics.json` under the app's persistent data directory every 30 gameplay seconds and on suspension/quit. No network telemetry or device identifiers are collected. FPS includes the frame-rate cap; it is not GPU-only timing. A touch counter and audio configuration do not certify correct controls or audible output.

For a connected development device: install the APK, allow the intro to play/skip, walk/sprint, complete the three mission stages, pause/resume, start boxing and use both touch buttons. Compare a completed save/reload and repeat the final interaction. Listen to footsteps, music/dialogue ducking and siren through headphones and speaker; suspend/resume and toggle mute. Traverse the center for at least ten minutes and repeat after thermal warmup. Pull local metrics using `adb shell run-as com.yashimi.vkt cat files/vkt-device-metrics.json`. A real device test is pending until ADB sees the user's phone.

The final machine-readable test/build/player/artifact results are recorded in [Verification-Resume-20261018.json](Verification-Resume-20261018.json). Earlier receipts are historical evidence, not proof of the new binaries.

## Prioritized next improvements

1. Physical Android touch, audible-output and thermal/FPS traversal; adjust render budgets from measured p95 rather than assumptions.
2. Review Serbian voice candidates with a native listener; admit distinct licensed/consented actors and normalize each dialogue bank.
3. Verify center landmark positions and references; model the municipality, museum and central square at measured scale with LOD/collision.
4. Build one shared, datum-aware terrain sampler; road-corridor correction and building/objective placement, with seam/slope regression before enabling relief.
5. Replace local patrol steering with a validated pedestrian navigation graph and witness/contact investigation events.
6. Add authored boxing animation transitions, bag impact/recoil and graded reaction scoring; retain independent endurance and single-claim rewards.
7. Add atomic district arrival checkpoints and travel transitions once the central route passes physical device traversal.
