# Bench press development milestone

The static street gym and rapid tap counter now use the player's existing CC0 Kenney skeleton and skin on a rounded bench, with round plate weights, a moving bar and two-bone arm/leg posing.

## Playing

Approach the street gym, press E / KORISTI, then TRENING. Press F or the workout button to lower the bar over 0.9 seconds. When GURAJ appears, press again within 0.75 seconds to perform the 0.8-second press. After 0.25 seconds of recovery the next repetition can begin. Eight full repetitions within 45 active game seconds complete the set.

Early/duplicate presses do nothing. Missing the window returns the bar without awarding a repetition. Only completed repetitions consume 3 stamina. A complete set retains the existing 40 XP, nutrition cost, 90-second rest and strength progression. Every three completed sets add one strength level, up to five. The existing v5 profile remains compatible; partial bench motion is deliberately not saved.

WASD, sprint, jump and attacks are blocked during the set. A side camera frames the bench; the walking avatar and held weapon are hidden to avoid duplicates. The original camera mode returns afterward. ESC / PREKINI SERIJU cancels without rewarding a set. Death, wanted state, timeout and leaving the station retain the existing cancellation rules. Gameplay/app suspension freezes the simulation.

The camera enters smoothly. The same clothed skeleton is posed lying down with bent knees, planted feet and hands following the bar. Bar height is derived from the actual arm reach, with mirrored grip assignment from shoulder coordinates. A maximum-strength pose test checks the entire stroke within 3 cm of hand targets. The shoulder/chest proportions change subtly with the saved strength level: maximum 7% wider and 6% deeper, without changing controller height or city scale.

## Validation

126 EditMode tests: 125 passed, zero failed, one existing optional ArcGIS skip. Tests cover full-cycle rewards, duplicate/early input, missed windows, zero-time pause, frame stalls, invalid time, skeleton availability and full-stroke hand contact. Windows player compilation completed with zero build errors.

An actual Windows player run passed full bench repetitions through the Unity UI, duplicate rejection, attack gating, hand/foot contact checks, food/police/save flows and the existing campaign/garden/combat/camera checks. Its capture was visually inspected. See Verification-Bench-20261016.json for the artifact hash and receipt.

## Boundaries and next work

This is authored procedural animation on the existing stylized character, not motion capture, muscle simulation or ragdoll bar physics. A session presentation instance uses the same player mesh/skin; the authoritative collision controller stays at the station approach. Entry/exit currently use presentation switching, not a complete sitting/lying/getting-up animation. Camera framing uses a fixed station offset; obstacle-aware gym placement and extra camera sweeps remain future work.

General locomotion retains the existing idle/run clips and collision controller. There are no new runtime packages, paid services, external model downloads, API keys or connector requirements. Android source paths share the same logic, but this wave's binary/runtime evidence is Windows only. Physical Android touch ergonomics, hardware performance and gym camera obstruction in all city locations remain unverified.

Next: authored get-on/get-off transitions, broader locomotion blend trees, load selection with balanced progression, measured Android capture, and more indoor gym equipment.
