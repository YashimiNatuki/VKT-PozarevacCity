# Street signals and hideout score — 2026-10-20

Windows-first incremental gameplay slice. G or ZVIŽDI emits an original positional whistle with a 24 m hearing radius and four-second cooldown. A clear physics path is required; walls and saturated queries fail closed. Existing patrols without an active pursuit investigate the sound location for up to six seconds. Hidden movement is not tracked, no heat is awarded, and active pursuit cannot be cancelled by whistling. Existing local hazards can investigate within their authored leash; ordinary detection/attack rules still apply. No new civilian/gang simulation or distraction mission objective is claimed. A pre-existing M key conflict between map and audio mute is removed: M opens the map; F10 toggles game sound. Native flows assert that opening the map preserves audio and that F10 toggles it both ways.

Input is rejected during menus, death, training, hideout entry and suspension. One owned source and fixed query buffer use startup-cached listeners; future spawned contacts will need explicit receiver registration. Disabling the component prevents external button calls. Muting suppresses playback, not the action's gameplay consequence.

The hideout plays an original ten-second downtempo instrumental. Location transitions fade; global audio mute and music preferences apply. Any selected/paused personal music takes priority. The score is synthesized, not a licensed popular song or authentic city recording. No new natural Serbian voice bank is bundled. The existing video is preserved byte-for-byte; a new video remains a separate roadmap task.

## Evidence

201 Unity EditMode tests passed, zero failed, one optional skip. Final native desktop and touch-preview flows check creation/help/phone/map/music, movement, rendered legs, all eight bench repetitions, wall-blocked and audible whistles, the visible whistle button, cooldown, no extra heat/pause, disabled-component rejection, room score, global mute and music preference mute. The audio help page must fit its rendered text rectangle. A broader Windows smoke checks intro, existing spatial acoustics, colliders, combat, garden, missions/resume, boxing and conditioning. All checked flow fields passed with zero runtime exceptions in the checked error classes.

The first touch-preview candidate failed the new help-text-height assertion: the last instruction was clipped. The text was shortened without reducing the assertion; final desktop and touch-preview checks were repeated against the rebuilt player. The signal perception fixture temporarily isolates the actual player and an actual patrol above the map; it proves the hearing/input/physics integration, not city-wide navigation or pathfinding. The accompanying screenshot is captured after restoring their positions. Personal save data is not used. Audio samples are checked for finite amplitude and quiet seams; these checks do not establish human listening quality or a perceptually balanced mix. Personal selected-track precedence was reviewed in code; runtime checks cover the room's global and music mute behavior, not a live radio stream.

No phone was connected or controlled, no new Android package was built, and no iOS artifact was produced. Existing Android v8 remains the earlier build. The rendered city and face remain stylized; this iteration does not solve terrain elevation or photorealism. No long-duration performance guarantee is made.

## Files and next work

CityHearingMemory owns finite-lived sound memory. CityStreetSignals owns input, bounded occlusion queries, cooldown and playback. CityPoliceOfficer and CityHazard extend their existing movement decisions. CityAmbience owns the room score; CityMusicPlayer exposes user playback selection. CityControlsDock documents and exposes the action. CityInputFlowValidation and Tools/Test-StreetSound.py provide repeatable native checks.

Run the Windows development build with the Python helper, using a fresh output directory:

```text
python Tools/Test-StreetSound.py --player <full-path-to-VKT.exe> --output <new-evidence-folder>
```

Add --touch-preview for the second layout. These tests require the new development build, a Windows desktop graphics session and its complete data folder.

The ordered next steps and acceptance criteria are in [Rođa roadmap](Rodja-Roadmap-20261020.md): authoritative garden mission, body/bench contacts and face rig, reviewed central block, contacts/story/police consequences, natural speech, then intro/micromobility/rare bench event. Dependencies follow a concrete need; no new paid/cloud runtime dependency was introduced.
