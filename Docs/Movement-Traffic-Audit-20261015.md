# Movement, collision and traffic audit — 2026-10-15

## Current evidence

Reviewed CityPlayerController, CityGroundProbe, CityCityDetail, CityOsmStreets and first-party CityGame runtime searches. No Unity process was launched during this audit. Tests added below require the coordinator's actual Unity run.

The player uses a CharacterController, 1.8m capsule and a camera 1.62m above the player origin. Current walking/sprinting speeds are 5/8 meters per second, jump height 1.25m and gravity -24m/s². Units are coherent with normal-size doors/benches, but walking is deliberately fast and needs subjective tuning after a real walkthrough. Combined directional input is clamped, avoiding diagonal speed gain. Pause/death clear touch input. Teleport reset clears vertical velocity.

OSM roads preserve imported centerline layout, with authored widths and flat elevation. Actual footprint facades have bounded static wall spans and authored 2.4m-wide, 2.5m-high visible doorway gaps. Roofs have no collision; they are visual shells, not a parkour surface. These approximate doors/heights are not geographic source facts. CityCityDetail is a separate primitive approximation and must not be advertised as surveyed geometry or automatically overlaid on real street data.

## Implemented fix

CityGroundProbe previously chose whichever upward-facing surface was highest under a ray started 300m overhead. Adding roofs or props could therefore move spawn/mission anchors onto a roof even when the authored approximate position was at street level. It now chooses a valid surface closest to the approximate elevation, retains CharacterController/mission-marker exclusions, rejects nonfinite positions and fails closed if the fixed hit buffer saturates. Roof placement remains possible when the authored approximate height intentionally targets that roof.

Added two EditMode regressions: stacked ground/roof selection at two authored elevations, invalid input rejection, and excluding mission-marker tops. No unrelated controller rewrite was necessary.

## Traffic gap

No vehicle controller, WheelCollider-based vehicle, moving traffic agent, drivable bicycle or traffic routing implementation was found in Assets/CityGame C# sources. CityAmbience contains procedural traffic sound; bicycle service signs and bike-part jobs are props/story text. They do not constitute road traffic. Vehicle/NPC collision claims therefore remain unverified and should not be presented as implemented.

A practical next traffic slice is a small fixed pool of slow agents following reviewed road routes, with spawn exclusion around the player, crossing reservations, obstacle stopping and a separate vehicle-player collision test. Keep it distinct from ambient audio. Android frame time and pedestrian safety distances need device measurements before increasing density.

## Character and story direction

Use only the user's explicitly shared urban programmer/terminal-lover and street style: a fictional local programmer-courier who repairs equipment, earns neighborhood trust and navigates Požarevac errands. Clothing direction can use a practical hoodie/jacket, backpack and inexpensive workwear. Dialogue can be relaxed Serbian with clear goals and consequences. Do not claim facial likeness, personal biography, ethnicity, health, criminal history or other sensitive traits.

A grounded opening arc: retrieve a bicycle part on Tabačka čaršija, deliver it along Stari korzo, meet a contact near Trg oslobođenja, then unlock workshop/plant catalog upgrades. NPCs need readable motives and reactions; character visual polish and natural Serbian voice casting remain separate production work.

## Remaining validation

Coordinator must run the new tests, verify generated city spawn positions after the ground-selection change, walk through visible doorway gaps at player scale, and test collision at sprint/jump speeds. No physical traffic or mobile performance validation was performed by this audit.

## Coordinator evidence — 2026-10-16

Final suite: 100 passed, 0 failed, 1 optional ArcGIS skip. New ground-selection cases passed. Actual portable PC and PC touch-preview player runs passed grounded startup, mission/garden/save/respawn, wall occlusion, swept projectiles and doorway geometry tests. Continuous manual sprint/jump traversal, drivable vehicles and moving traffic remain unverified/unimplemented as described above.
