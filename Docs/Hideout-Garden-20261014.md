# VKT: štek, bašta, interakcije i audio

## Scope and entry

Source: `C:/Users/jamal/Pozarevac Lejt Najt 3D Gta-ThugLife`.
The hideout door is four metres beside the third mission checkpoint. It unlocks
after the first contract. Look at the door and press E. Inside, use WASD/mouse,
E on a pot and V to cycle the selected ornamental plant. Use the exit door to
return to the city. P retains its phone role. M toggles gameplay sound.

The room is composed at runtime using collidable Unity geometry, lighting, four
pots, growth representations, labels and entry/exit interactables. It is a
functional blockout, not a finished environment-art asset. Its isolated location
avoids placing the room under streamed terrain. Mission deadlines continue while
inside; entering is not a way to pause a contract. The phone/Esc still pauses
gameplay. Garden care uses real UTC and can advance while gameplay is paused.

## Fictional plant loop

| Variety | Wet growth required | Seed cost | Harvest value |
| --- | --- | --- | --- |
| Noćni cvet | 6 real hours | 60 | 180 |
| Plava iskra | 7 real hours | 60 | 225 |
| Zlatna magla | 8 real hours | 60 | 270 |

These are invented ornamental plants and game economy values. Planting starts
dry. Watering supplies two hours of moisture. Only elapsed wet time contributes
to growth. Dry plants pause and do not die. A long absence grants at most the
remaining moisture time; returning after a week does not create a week's crop.
E plants an empty pot, waters a dry pot, or harvests a mature pot. A watered pot
shows remaining moisture and completion percentage. Repeated watering is blocked
until dry. Harvest commits the empty state before granting cash and cannot pay
twice. Backward clock movement does not subtract time or generate growth.

The original 30-game-second phone test plant remains a distinct legacy loop.
It does not claim to implement the new real-hour garden. No real cultivation,
strain or trafficking instructions are part of this feature.

Design references: the official [Farming Simulator Academy](https://www.farming-simulator.com/newsArticle.php?news_id=297)
explains readable growth stages and field information; the
[Stardew Valley watering guide](https://wiki.stardewvalley.net/Watering_Cans)
describes growth pausing when crops are not watered. VKT's durations, prices,
four-pot limit and fictional varieties are original gameplay decisions.

## Data and compatibility

`CityGarden` owns deterministic care, cost and payout rules. `CityProgression`
owns the garden and copies it into profile snapshots. Profile payload version 2
adds the garden; valid version 1 profiles load with an empty garden and save as
version 2. The existing envelope and atomic backup/recovery protocol are reused.
The filename stays `vkt-profile-v1.json` to find existing players' profiles.
Older VKT binaries refuse version 2 instead of silently overwriting garden data.
The original save is retained by the existing replacement backup mechanism.

Care is sampled once per second, labels four times per second, and arrays are
bounded. Varieties use direct lookup; growth is O(number of pots). No new
multiplayer, ECS conversion or unmeasured performance/scalability claim is made.
System-clock tampering is not prevented in this local single-player design.

`CityInteractable` extends existing nearest-hit physics selection for doors and
garden pots. Mission interaction stays supported. This gives future functional
props a shared boundary without adding another input or event framework.

## UI and sound

Phone commands, garden labels and interaction prompts use Serbian Latin script.
The entire old mission journal and all failure strings are not yet localized.
Existing Starter Assets footstep/landing clips are connected to player motion.
SFX and dialogue have separate AudioSources; M mutes both. Existing intro-video
direct audio is separate from this gameplay mute.

Three dialogue roles are wired: Mila (entry), Vuk (exit), Luka (mission finish).
Their Serbian captions work without voice files. Optional natural recordings go
under Resources/Audio/Voices as `mila_enter`, `vuk_exit`, `luka_complete`.
The loader resolves each AudioClip once. No Serbian voice recordings existed in
the inspected Assets; no speech-generation connector was callable. Therefore
natural multi-voice Serbian dubbing is **not delivered** by these subtitles.
The exact caption is the recording script. Mute is session-only, not a saved
audio-settings screen.

## Požarevac and remaining world work

SampleScene serializes latitude 44.6193 and longitude 21.1764, Cesium OSM Buildings
asset 96188 and Google Photorealistic asset 2275207, plus an ArcGIS map. This
establishes the configured geographic origin and providers, not complete local
street authenticity, current coverage or walkable routes. Earlier captured
frames showed overlapping/overhead geometry; the SDK Editor exception and
ComputeBuffer cleanup warning remain separate issues to investigate.
No claim that all Unity/map/audio work is finished is justified.

Baseline backup:
`C:/Users/jamal/Documents/CodexBackups/VKT-Stek-20261014-183411/CityGame`.

## Verification

`C:/Users/jamal/Documents/vkt-stek-tests-20261014.xml`: 50 EditMode tests passed,
zero failed, including five new garden tests. Domain coverage includes dry growth,
offline bounds, independent pots, duplicate payment, clock rollback, invalid
state, insufficient cash and real version-1-to-version-2 file restoration.
The optional engine scenario `-cityHideout true` uses actual physics rays to
enter, plant and water all four pots, restore their saved state and exit.

`C:/Users/jamal/Documents/vkt-stek-runtime-20261014.json`: actual SampleScene
scenario passed with `hideout=true`. It passed the original campaign, phone
pause, nine mission raycasts and economic restore, then physical door entry,
eight pot interactions (plant and water), garden file restoration and door exit.
The known ArcGIS Editor exception is still present in the raw log.
The test positions the player near objects; it is not a continuous walking run.

`C:/Users/jamal/Documents/vkt-stek-build-20261014.log`: Windows development build
succeeded with zero build errors; reported output size 608144692 bytes.
Runnable output: `C:/Users/jamal/Documents/CodexBuilds/VKT-Stek-20261014/VKT.exe`.

`C:/Users/jamal/Documents/vkt-stek-player-20261014.json`: the first Windows
hideout executable passed the campaign, physical door entry/exit, four plants,
watering and garden file restoration. Its rendered frame was inspected and
revealed a camera/world placement issue, so verification continued.

## Camera ownership follow-up

The scene's DynamicCamera had its own enabled Cesium globe anchor, origin shift,
fly-to controller and camera controller while the player also owned globe
positioning. Runtime bootstrap now disables those camera globe/motion owners
before parenting the camera under the player. The player's globe anchor remains
authoritative. This does not rewrite the SDK or serialized scene.

`C:/Users/jamal/Documents/vkt-stek-camera-runtime-20261014.json`: actual scene
passed again with hideout enabled and an added check that camera local position
remains at (0, 1.62, 0) after elapsed engine frames. The development-player smoke
also rejects a camera that leaves eye position after its validation sequence.
This check addresses camera ownership; it does not establish every street or
every renderer provider as correct.

The new entrance and room are parented under the same CesiumGeoreference as the
player, satisfying the anchor hierarchy requirement. Their owner destroys both
detached objects and its materials when unloaded.

Final distributable: `C:/Users/jamal/Documents/CodexBuilds/VKT-Stek-Camera-20261014/VKT.exe`.
It was fully built in `vkt-stek-camera-build-20261014.log`, then its code was
rebuilt with the anchor-parent fix and the inspected seedling/label refinements
using Unity's scripts-only build against that same complete output.
`vkt-stek-delivery-build-20261014.log` reports success, zero build errors and
608145288 bytes. The first VKT-Stek output is retained as
an intermediate artifact; use the Camera output above for the delivered version.

## Final executable evidence

- `C:/Users/jamal/Documents/vkt-stek-delivery-player-20261014.json`: final Windows player passed the campaign, nine mission physics interactions, growth/trade, crew, districts, durable profile restore, hideout entry/exit, four physical plantings, watering and garden restore. It also passed the camera eye-position check after elapsed frames.
- `C:/Users/jamal/Documents/vkt-stek-delivery-player-20261014.png`: final actual offscreen render inspected. Room floor/walls are coherent; the terrain no longer intrudes through the floor in this viewpoint. Seedlings sit on pots and front/back labels are separated. Geometry remains a functional blockout rather than final art.
- `C:/Users/jamal/Documents/vkt-stek-delivery-player-20261014.log`: no new missing-georeference anchor warning or logged Exception in the final player; the existing SDK ComputeBuffer cleanup warning remains.
- `C:/Users/jamal/Documents/vkt-stek-source-20261014.csv`: SHA-256 manifest of first-party CityGame C# sources.

No continuous city walk, complete Serbian mission localization, recorded natural
Serbian dialogue, vehicle simulation or NPC behavior is claimed. The new care
logic is validated with deterministic time jumps; a human multi-hour playthrough
was not performed. Ear-level listening/subjective voice quality was not verified.
