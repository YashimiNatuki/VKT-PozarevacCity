# Connected missions, conditioning and feedback audio

## Weak link addressed

Street jobs previously checked HEAT but did not know the character's actual wanted or training state. A low-heat wanted character could therefore accept or advance a delivery. The mission director now binds the existing lifestyle authority to the job model. Primary mission interactions also respect the same wanted/training gate. No duplicate police state or parallel mission framework was created.

| Before | Result |
| --- | --- |
| Low HEAT could bypass a real wanted state | Acceptance and handover reject while wanted, with a reason in prompts, HUD and journal |
| Job progress exposed only a checkpoint index | Derived cargo and delivery-receipt phases describe pickup, delivery and final confirmation |
| Delivery timeout always claimed a package return | Delivered-but-unconfirmed expiry closes the job without inventing a return or paying |
| App suspension was not part of the director's timer gate | Mission timers stop on application suspension and menu pause; wanted status alone does not freeze time |
| Long job briefings had a fixed text rectangle | A masked vertical scroll area supports mouse wheel and touch dragging |
| Only bench training | An alternative three-stop conditioning course uses normal walking/sprinting controls |
| No consistent objective feedback cue | Short original checkpoint/success/failure signals share bounded volume, mute and pause rules |

## Player routine

Before accepting a mission, use the gym or kiosk, check food/energy/rest, then read the job conditions in the phone. Training cannot start during an active main delivery or street job. Contacts reject interactions while training or wanted. After pickup the package is carried; after delivery a receipt awaits final confirmation. Only that final step pays. Losing police restores contact availability, but an active job deadline continues during pursuit. A phone/menu pauses simulation rather than granting offline progress.

The main campaign still contains three authored contracts. Street jobs still use six offer types and three existing central stations. Neighborhood names remain narrative references; this wave does not build those districts or introduce an NPC traffic simulation. Active jobs remain intentionally abandoned on reload, while completed counts persist. Durable active-route recovery is prioritized below.

## Conditioning course

At the street gym, open L / BLOK and select **KONDICIJA / 3 stanice**. Requirements: no active delivery/pursuit/other exercise, at least 20 nutrition and 40 energy, and no shared workout rest remaining. Follow three blue floor markers in the existing authored central area and remain within 2.2 horizontal metres and 2.5 vertical metres of each target for 1.2 continuous simulation seconds. Leaving resets that stop's hold. The time limit is 75 game seconds; a stalled frame cannot instantly register a stop.

The shared 90-second training reservation begins at course start and is saved immediately. Reload/cancel cannot remove that reservation. Normal movement and sprint fatigue remain active; checkpoints, pauses and captions work with the existing mobile controls. A complete course grants **20 XP exactly once**, consumes four nutrition and ten energy, and saves XP/rest. It does not increment bench workouts or silently grant bench strength. Menu/app pause stops course time. Crime, death, exhaustion, entering the hideout, expiry or cancellation gives no conditioning XP. ESC cancels an active exercise without also toggling global pause in the same frame; the cancel button remains available.

No save-schema bump was needed: XP, nutrition, energy and the shared rest already belong to profile v6. The active course is transient and does not resume on load. Rewards are committed through the existing lifestyle owner; the completed course seals its claim before notifying observers.

## Audio change and evidence boundary

Three original mono 22.05 kHz PCM cues have a smooth zero-edge envelope and a peak bound of 0.2 before the feedback source's 0.24 volume. One owned source replaces its current cue; a 150 ms gate prevents interaction spam from accumulating sources. Notifications remain the readable equivalent. Dialogue retains higher source priority. Cues stop on pause/app suspension and reject playback while muted, suspended or paused.

Movement sounds also now reject playback during application suspension even when a transform changes while game time remains positive. Existing six-source spatial ambience, wall occlusion, indoor/outdoor selection and patrol siren remain in place and are part of the development-player regression checks.

PCM tests verify mono format, finite values, peak/RMS bounds and quiet edges. Player checks verify source playback and pause/mute/suspension behavior. These checks do not prove subjective loudness, natural Serbian voice acting, HRTF surround or recorded street ambience. No recording, external music or paid sound generation was imported.

## Next improvements, prioritized after reviewing larger options

| Priority | Improvement | Why / acceptance criterion |
| --- | --- | --- |
| 1 | Durable active missions with a settlement ID | Persist route, stage, remaining game time and claim identity; recover after crash/load without duplicated payout. This is the next mission-system reliability gap. |
| 2 | Physical contact NPCs and investigation states | Build patrol/search/contact schedules on verified navigation routes; no contact through walls, no unexplained teleport capture. Start in the existing centre before scaling. |
| 3 | Recognizable central landmark assets with LOD | Use reviewed reference material and the installed Blender/asset pipeline; validate scale, collision, mobile draw/triangle budgets and actual player captures. Higher payoff than simply adding more generic buildings. |
| 4 | Reviewed real height raster and shared height sampler | Resolve CRS, vertical datum, raster license/nodata and DSM artefacts; place roads, buildings, spawn and objectives from the same surface. Keep the flat regression fixture. Do not invent hills with random noise. |
| 5 | Recorded Serbian voice bank and surface-aware audio mix | Three consistent voices, captions, proper licenses, surface footsteps and dialogue ducking. Evaluate HRTF only against measured Windows/Android CPU and an actual listening comparison. |
| 6 | Technique-based boxing and separate endurance progression | Add timing/guard/reaction rather than tap spam; persist clear endurance versus strength gains and verify touch fairness, fatigue and save migration. |
| 7 | City-cell streaming when coverage grows | Scene/asset budgets and asynchronous loading can help a larger city, but require seams, navigation, save and mission-anchor checks. Current bounded central coverage does not justify pretending whole-city streaming is already implemented. |
| 8 | Object personalities such as Optimistična klupa | Keep an authored performance and interruption-safe return animation available offline; optional AI can vary flavor, while gameplay and collisions remain deterministic. |

These are proposed follow-ups, not delivered features. The installed tools enable the work; they do not substitute for reviewed assets, real data or device measurements. See the accompanying verification receipt for the exact test/build/player results of this wave.

Primary engineering references reviewed for the follow-ups: [Unity Audio Mixer routing, snapshots and ducking](https://docs.unity3d.com/6000.0/Documentation/Manual/AudioMixer.html), [Unity Audio Profiler](https://docs.unity3d.com/6000.0/Documentation/Manual/ProfilerAudio.html), [AI Navigation runtime/edit-time navigation](https://docs.unity3d.com/Packages/com.unity.ai.navigation@2.0/manual/index.html). These capabilities inform the proposed architecture; this wave does not claim a new mixer graph or NavMesh installation.

## Reporting convention requested by the user

Future VKT milestone reports should end with at least five concrete, prioritized possible next improvements, selected after checking the current project weaknesses. Keep completed behavior, evidence limits and proposed work distinct. An updated Android APK and physical-phone validation should accompany a future mobile delivery; this Windows milestone does not substitute for that device evidence.
