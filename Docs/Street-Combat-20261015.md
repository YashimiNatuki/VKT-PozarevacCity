# Street action and combat scope — 2026-10-15

VKT's intended direction is a cannabis-themed street-action game in an authored Požarevac prototype. Earlier ornamental-plant descriptions reflect a prior interpretation, not the current product direction. This document records the verified PC combat wave. Prior published artifacts remain evidence of their own revisions; refreshed Android packaging is still running.

## First playable combat slice

The source implementation now includes punch, stone and stick equipment, three stone pickups plus one stick pickup, an eight-projectile pool with swept collision, and line-of-sight checks for melee. Domain inventory is capped at 12 items with attack cooldowns. Desktop actions use Q/left mouse button, with corresponding touch controls. Equipment is session-only; durable equipment save is not implemented.

Implemented reactions include nonlethal stun/suppression, hazard hits adding 4 heat and breakable windows adding 8 heat once per window. The PC runtime checks below verify these reactions and hit paths. They do not establish finished encounter AI or balanced difficulty.

Targets need explicit hostility/reaction states and readable warning/recovery behavior. Attack resolution must respect walls, distance and the intended target; allies, bystanders and inactive/dead targets require deliberate rules. Desktop and touch controls must expose the same actions. Save only owned/unlocked equipment and durable progression; transient hits/projectiles should not reapply after reload.

The punch/stone/stick slice passed the PC runtime and touch-preview checks listed below. Actual Android phone behavior remains unverified.

## Later progression — unimplemented

Slingshot → water pistol → fictional firecracker → tank is the requested later arcade progression. These items are **unimplemented planned content** until source, assets and tests demonstrate otherwise. Their unlocks, prices, ammunition/resource limits, presentation, target reactions and mission consequences must be designed before adding each stage. Tank gameplay also needs vehicle physics, traversal restrictions, camera, collision and mobile performance work.

All descriptions here concern fictional in-game mechanics. Real weapon construction, real explosive recipes and real-world operation instructions are outside the implementation requirement.

## Theme and economy connection

The strain catalog, customer types, district pricing and reputation progression should share stable IDs and authoritative inventory/payment operations. Combat consequences can alter heat/reputation and interrupt active jobs, but cannot silently duplicate or lose inventory. Cannabis-themed growth remains an abstract timed game loop; no real cultivation procedure is required.

## Verification boundary

Before calling the wave complete: compile; run deterministic combat/inventory/save tests; exercise real desktop and touch actions; inspect readable reactions/audio/UI; rebuild Windows and Android; verify final artifacts and distinguish phone packaging from an actual device runtime pass. Natural Serbian voices, continuous radio validation and broader city traversal remain separate outstanding gates.

## Verified street-wave evidence — 2026-10-15

- Tests: **93 passed, zero failed, one optional ArcGIS-specific skip / 94 total**, recorded in `vkt-street-tests-20261015.xml`.
- Final Windows build: **zero errors, 227,743,368 bytes**, recorded in `vkt-street-windows-checked-build-20261015.log`.
- Actual PC smoke and PC touch-preview runtime reports both passed: `vkt-street-checked-player-20261015.json` and `vkt-street-touch-player-20261015.json`.
- Exercised paths include E stone/stick pickup, actual swept stone/window collision, melee actor damage and heat, walls blocking attacks, cooldown, eight-projectile pool exhaustion without ammunition loss and subsequent reuse, phone-pause freeze, death cleanup and respawn.
- Final strain-card and touch-combat screenshots were visually inspected. Atlas imagery is representative AI cannabis illustration, while physical 3D plants remain stylized.
- Android native export succeeded with **zero errors, 1,178,179,142 bytes**. Its final APK is still building; no current APK success or actual-phone runtime pass is asserted.

Desktop touch preview verifies that input path on PC; it does not replace Android installation, touch ergonomics, performance or thermal testing. Later slingshot/water-pistol/fictional-firecracker/tank progression remains unimplemented.

Final Android assembly succeeded: ARM64/IL2CPP development APK, 52,119,391 bytes, minimum API 28/target 36; v2 signature verified. No device was attached. See [current receipt](Verification-Street-20261015.json) for distribution hashes. Windows and Android ZIPs include map/art/license notices and exclude development backup folders.
