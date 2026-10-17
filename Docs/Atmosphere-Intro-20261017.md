# Original intro, spatial sound, player motion and the hideout

## Original opening restored

The original project contains `Assets/StreamingAssets/GameIntro_Pozarevac_ThugLife.mp4` (124,295,536 bytes, MP4 header duration 43.5 seconds). Earlier portable source exports excluded all audio/video. Consequently, those builds had no clip and the old intro player silently returned. The original was preserved; this release explicitly copies that exact MP4 into the Windows player's `VKT_Data/StreamingAssets` after the build. No new substitute movie is generated.

The opening now has a visible **PRESKOČI UVOD** touch/mouse button plus Space/Esc. Video audio uses its own 2D AudioSource. Application suspension pauses playback. Missing desktop media produces a short, explicitly labeled title card instead of silently omitting the opening. Android URL handling no longer assumes StreamingAssets is an ordinary filesystem directory; Android packaging/device decoding remains unverified in this wave.

Public source intentionally keeps the original movie separate from its MIT code. To build with a locally held copy, add `-vktIntroSource 'ABSOLUTE/PATH/GameIntro_Pozarevac_ThugLife.mp4'` to the documented Windows batch build command. Only that explicitly selected MP4 is copied. Without it, the build uses the title card. The release receipt records the original and packaged hashes. The movie is user-supplied project media; the code license is not a license for its soundtrack or underlying footage.

## Spatial audio

`CitySpatialSoundscape` composes six bounded mono point emitters: street air/ventilation, two park locations, a night park layer, distant city and the hideout fan. Sources use Unity 3D distance attenuation and directional speaker panning. A reusable 16-hit non-allocating physics query detects walls, ignores the player hierarchy and conservatively treats saturated queries as blocked. One emitter is probed roughly every 34 ms, about five checks per second per emitter. Obstruction reduces level and smooths the low-pass cutoff toward 900 Hz. This is a bounded presentation budget, not a measured performance improvement over a previous implementation.

Outdoor sources pause inside; only the fan plays in the hideout, with Unity's Room reverb enabled there. Dialogue ducks the spatial layer. Menu pause, mute, application suspension and death silence it. Existing day/night beds remain separate and gain application-suspension handling. The patrol siren now belongs to a world-space node following the closest officer, with distance falloff and a separate four-checks-per-second obstruction filter. Missing footstep/landing clips receive owned mono synthesis instead of silence; supplied Resources clips still take priority.

These new loops and contact impulses are first-party synthetic effects. They are not authentic field recordings of Požarevac. No natural Serbian voices, imported ambience library, binaural HRTF, Ambisonics or calibrated 5.1/7.1 mix is claimed. Automated playback/physics checks do not prove subjective loudness or directional quality on headphones.

### Researched free library route

[Steam Audio](https://github.com/ValveSoftware/steam-audio) is Apache-2.0, supports Unity and Windows/Android, and is the next candidate for a separate native-plugin integration trial. Its [Unity guide](https://valvesoftware.github.io/steam-audio/doc/unity/guide.html) covers binaural HRTF, runtime listener notification and occlusion. It is **not installed or activated by this change**. First validate a pinned package on Unity 6000.6.5f1, the dynamically created camera listener, domain reload, Windows/ARM64 native loading and DSP cost; then compare controlled headphone captures before enabling it by default.

[Kenney Impact Sounds](https://kenney.nl/assets/impact-sounds) is a verified CC0 candidate for recorded footsteps/foley. It is researched, not imported in this wave. Existing public asset/provenance boundaries remain in place. Original city recordings would improve authenticity further: short traffic, park, stairwell and room-tone clips, without music playing over them, can replace the synthetic layer after review and loop editing.

## Player and hideout

The existing legacy idle/run rig remains authoritative for presentation. Additive chest breathing, speed-dependent torso lean, bounded turn banking and head glance add secondary motion. Previous additive rotations are restored before the next animation sample, avoiding accumulation; paused animation does not continue advancing the pose. Gameplay collision/root movement remains controlled by the existing player controller. A small wristwatch adds owned accessory geometry and follows the forearm; wardrobe colors affect its material.

The hideout retains its four plots and original care/economy rules. Rounded pot meshes preserve interaction colliders. New furniture includes a planning board, stacked storage crates, animated fan and a physical **PLAN BLOKA / E** work desk. The desk opens the existing job journal and owns its pause through the existing phone controller. No real-world cultivation instructions or new save format are added.

## Missions

Street job generation now selects six types instead of four. **Tihi dogovor** requires HEAT at most 30; **Inventar ekipe** requires at most 45. A contact rejects acceptance or handover above its threshold; objective index, cash and payout remain unchanged. The active deadline continues during gameplay, so cooling off has a cost. The two types have their own objective labels and retain the existing three local stations, ordered visits, one-time payout, cancellation and persisted completion count. They do not claim new physical neighborhood coverage or simulated NPC travel.

## Verification / remaining work

See `Verification-Atmosphere-20261017.json` for the final build, test/player results and archive hash. Development flags include `-vktIntroSmoke`, `-vktIntroNaturalSmoke`, `-vktIntroFallbackSmoke` and `-vktAcousticsSmoke`. Smoke profiles use isolated temporary saves. Intro tests require actual decoded frames; natural completion and skip are distinct runs. Acoustic checks cover source/listener composition, actual wall insertion/removal, mute/suspend, indoor source selection, desk journal/pause and breathing progression, alongside existing gameplay regression.

Next priorities: headphone/listening review, licensed recorded ambience and Foley intake, optional Steam Audio integration benchmark, fuller walk/turn/jump animation assets and foot placement, followed by a new Android build and physical phone validation. Character proportions are still stylized; this wave does not replace the model with a photorealistic human.
