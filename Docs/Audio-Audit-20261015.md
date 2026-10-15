# Audio audit and bounded fixes — 2026-10-15

## Findings and source changes

- `CityGameAudio` could be initialized repeatedly, leaking extra player AudioSources; it now validates dependencies and rejects duplicate initialization. Uninitialized dialogue fails explicitly. Effects/dialogue stop on disable, and owned sources are destroyed on teardown; shared Resources clips are retained.
- Gameplay pause previously left dialogue/effects playback advancing independently of the paused game. These sources now pause/unpause with gameplay. Subtitles remain gameplay UI and do not depend on voice availability.
- Music's volume property previously reflected the rendered AudioSource level, making future ducking overwrite user intent. User volume/mute are now separate stored preferences with bounded/non-NaN volume. M master mute also mutes the music source; its separate mute remains respected.
- Dialogue has higher voice priority. Music ducks to 25% of the chosen user level and ambience to 50% during spoken playback, then recovers without changing user settings. No new mixer dependency or repeated clip loading was added.
- Completed-track detection depended on `AudioSource.time` staying at clip end, but stopped sources may reset time. It now records actual observed playback and advances only after that playback ends, excluding user pause, application suspension and in-flight streaming.
- Music now cancels/disposes pending requests and releases owned clips on disable as well as destruction. Application suspension pauses music without clearing the user's pause choice; pending loading does not start playback while suspended. Music intentionally remains available while its own phone-like control overlay pauses gameplay.
- Ambience resumes paused loops on re-enable rather than restarting both loops. Synthetic loops remain bounded at two four-second mono 22,050 Hz clips; this is an allocation bound, not measured performance evidence.

## Coverage and remaining gaps

Two Editor tests were added for pre-initialization music preferences/NaN bounds and explicit dialogue initialization errors. **They have not run in this audit subtask**; no Unity process, build or listening session was launched. The coordinator must compile/run the suite and exercise pause/resume, master and independent mute, dialogue ducking, next-track behavior, disable-during-load and app background/resume before claiming verification.

Natural Serbian Mila/Vuk/Luka recordings are still absent. Subtitle slots are not delivered natural voices. Existing local Footstep/Land recordings remain outside public export because redistribution provenance is unresolved. Synthetic ambience is first-party placeholder audio, not an authentic Požarevac recording. Personal music is read locally, never published. Optional radio remains bounded/experimental, not proven uninterrupted 24/7 streaming.

No subjective sound-quality, loudness normalization, device battery/thermal, mixer profiling or phone audio-route proof is claimed. Voice/SFX settings persistence and robust indefinite-radio playback remain future work. Real character voices and licensed/original city recordings require provenance plus actual audition; no paid generation API, model download or external media redistribution occurred here.

## Coordinator verification — 2026-10-16

Final suite: 100 passed, 0 failed, 1 optional ArcGIS skip (101 total). Final Windows build completed with 0 errors (227,927,900bytes). Actual Windows and PC touch-preview runs decoded local audio while a simulated application-pause callback was active, confirmed silence, then confirmed playback after resume. This verifies the deferred-start regression in a player; it is not a physical phone background/resume or subjective listening test. An additional guard prevents M master-mute while the paused phone input is being edited. Master mute/ducking code was reviewed; full voice/ambience listening and continuous-radio tests remain outstanding.
