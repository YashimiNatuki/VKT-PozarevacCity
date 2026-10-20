---
name: rodja-worldcraft
description: Review VKT character rigs, city models, geographic coordinates, terrain and audio assets before Unity admission. Use for provenance and runtime-fit checks, not automatic downloads or paid generation.
---

# Rodja Worldcraft

Keep the user's urban Požarevac identity while judging real geometry and runtime evidence. Separate appearance, topology, rigging, collision and performance: a realistic picture is not a rigged 3D character.

For a head, inspect skeleton, eyelid/jaw/blendshape channels, skin weights, neutral proportions, textures and license. The Kenney source is a low-poly painted atlas without facial bones or imported facial blendshapes. The game may add an owned runtime jaw morph; inspect the actual revision. Shader eyelids and a jaw pose do not provide phoneme lip sync or a realistic new head. Preserve body animation, outfit attachment, 1.8 m normalization and bench IK. Test front/side, walking, wardrobe and training views. Define a nearby/distant face budget and test it in Unity; a Blender/CPU render does not prove engine behavior.

For Požarevac, use one origin and the existing east/up/north convention. Record source, license, acquisition date, CRS, vertical datum, bounds and confidence for every geographic input. Validate contradictory landmarks before admission: the hipodrome and Ljubičevo are distinct places. A DSM may measure roofs/trees; do not move streets or spawns onto raw raster heights. Review road continuity, footpaths, slopes and collider clearance together. Renderer culling must not remove gameplay collisions. Other-city travel stays a separate feature until scene, mission and save transitions exist.

For music/voices, record rights and actual language. A filename resembling Serbian is insufficient: Lower Sorbian is a different language. Existing Piper audio was rejected as robotic; the VITS candidate was unreviewed. Generated audio is not automatically approved or three distinct actors. Keep subtitles and show unavailable status. Private music is not public-release permission. Avoid running untrusted pickled checkpoints or model-provided Python.

If local Game Development Studio is selected, first inspect its version, capabilities and doctor with an explicit workspace. On this machine the launcher may be `C:/Users/jamal/Documents/VKT-Toolchain/bin/game-dev.cmd` even when PATH is stale. The CLI can inspect/package assets, but generation, file mutation and provider spend retain their own current authorization. No credential or paid account is implied by this skill. Use the installed CLI schemas; do not invent API calls. If unavailable, report the blocked operation and continue supported static review.

Before import, identify exact files, units, pivot, material reuse, texture/mesh/LOD budgets, collision proxy and attribution. Preserve source bytes and receipt hashes. Use Unity-native import for the existing FBX rig; do not convert every model to GLB merely because a web pipeline recommends it. Report artistic approval separately from hash, format, render and hardware checks.

Read `../../references/WORKSTYLE.md` for the Konglomerat work contract. For gameplay transitions use `../../references/GAMEPLAY-PROTOCOL.md`; for acceptance evidence use `../../references/QUALITY-GATES.md`.
