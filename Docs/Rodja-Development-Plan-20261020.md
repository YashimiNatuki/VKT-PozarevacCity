# Rođa: ordered game development plan

Windows remains primary. Android follows the same input/state contracts and receives grouped builds plus physical-device verification. VKT / Požarevac City remains the game; Konglomerat / Rođa names the development toolkit.

## Current recovery gate

First restore a usable loop: create character -> controls/HUD explanation -> walk -> phone -> map -> return -> compact music -> return -> gym -> complete a set -> save/reload. Verify real input routing, not only direct model calls. Keep money/mission settlement authoritative and preserve existing saves. The recovery report records which checks passed; this plan is not evidence that later features exist.

## Ordered slices and acceptance gates

| Order | Player outcome | Work and acceptance |
| --- | --- | --- |
| 1. Face/head | Readable, expressive face | Replace the small painted atlas with a reviewed licensed model/texture when available. Review neutral front/side proportions, eyelids, jaw and expression channels. Preserve 1.8 m normalization and attachments. Current owned jaw morph is cosmetic, not a realistic new face or speech lip sync. |
| 2. Body/motion | Legs, arms and training visibly coordinate | Verify rendered stride phase, opposing limbs, stop/start, walls, stairs, pause and camera changes. Preserve joint lengths. Follow with ground-aware foot placement and separate bench/boxing pose review. A mathematical pose test does not prove natural motion. |
| 3. Terrain/center | Recognizable center with coherent slopes | Use one origin and meters; review existing OSM roads and footprints. Add Tulba, Kruška and Čačalica height data only with CRS, vertical datum, source/license and ground checks. Keep collision, road width, steps and spawn clearance consistent. Do not invent elevation or use roofs/trees as walkable street heights. |
| 4. Skate/scooter | Mount, ride, dismount without trapping controls | First a skateboard and an ordinary kick scooter as bounded prototypes. Shared rider input and ground probing, distinct steering/braking and stance. Test slope limits, curb collisions, jump/landing, death, menus and dismount clearance. No vehicle may bypass mission, pause or health rules. Purchase/save ownership is a separate durable feature. |
| 5. Bicycle | Pedal, steer and brake coherently | Reuse only verified rider/ground contracts. Add wheel contact, low-speed behavior, safe dismount and foot/pedal alignment. Confirm physics and road traversal before pursuit missions. |
| 6. Story/reactions | Consequences follow actions | Persist active objectives and settlement once. Connect contacts, witnesses and police evidence with location/visibility rather than omniscience. Add optional urban encounters after durable mission transitions pass. |
| 7. Audio | Readable mix and natural Serbian dialogue | Preserve original intro. Steps follow surface; ambience follows interior/time; speech ducks music. New voices need language/license/listening approval. Local AI remains optional with authored captions and unavailable status. Never reactivate the retired incompatible VITS path. |
| 8. Optimistic bench | Rare authored surprise without lost progress | A clearly fictional original transforming bench encounter: eligible idle player -> short warning/voice/caption -> bounded transformation -> retreat/return -> cooldown. It cannot interrupt active reps, settlement, loading or a modal menu. Keep a safe dismount zone, cancel path and deterministic test seed. No automatic reward for repeatedly reloading the event. Not implemented by this recovery wave. |

## Related AiBrat reference

Read the explicitly referenced Codex conversation `01a14435-31ee-7dd1-9a0f-dbd14614c177` (Y!Appi i AiRodja). Its recent records describe a WPF desktop companion with skull artwork/cloud and a web UI. That is a useful tone/presentation reference, not evidence of a reusable rigged realistic 3D human. Inspect an actual licensed mesh before any transfer. Keep that project's source, backend, account data and execution permissions separate.

## Geographic research checked 2026-10-19

Unity supports RAW heightmap import; resolution, byte order, terrain size and vertical flip need explicit settings. Copernicus GLO-30 is a surface model at approximately 30 m sampling, suitable as a candidate broad-relief reference, not fine curb/street geometry. The current Copernicus 30 m view/API service also has access-category and registration requirements; free dataset licensing does not imply every endpoint is anonymously accessible. No dataset was downloaded or license accepted in this review.

- https://docs.unity.com/en-us/engine/6000.6/manual/creating-environments/script-terrain/terrain-heightmaps
- https://dataspace.copernicus.eu/explore-data/data-collections/copernicus-contributing-missions/collections-description/COP-DEM
- https://documentation.dataspace.copernicus.eu/APIs/SentinelHub/Data/DEM.html

## Delivery discipline

One coherent slice -> relevant tests -> rendered native-player flow -> screenshots -> package/hash -> safe original-source comparison/sync -> authorized commit/push -> verified remote artifact. Keep physical Android touch/audio/FPS, artistic approval and extended traversal as separate evidence. Never label a connector installed/enabled as already integrated into gameplay.
