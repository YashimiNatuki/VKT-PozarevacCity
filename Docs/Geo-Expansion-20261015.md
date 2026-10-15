# VKT geographic expansion — 2026-10-15

This is researched guidance, not a claim that additional cities, surveyed terrain, scans or bus travel have been delivered. The coordinator is expanding a bounded OSM-based Požarevac center. Authored textures/heights and flat ground must remain distinguished from photogrammetry.

## Local names verified with primary sources

| Name | Supported fact | Uncertainty |
| --- | --- | --- |
| Čačalica | The [city government](https://pozarevac.rs/cacalica-pozarevac/?lang=sr&lng=cir) identifies the hill above Požarevac and its memorial park. | No exact terrain mesh, hill boundary or measured playable route obtained. |
| Tulba | The [National Museum](https://muzejpozarevac.rs/) describes the ethnopark on Tulba hill, established in 1972. The [city museum page](https://pozarevac.rs/narodni-muzej-pozarevac/) confirms the open-air exhibition there. | Descriptions do not grant reuse rights to museum images/models. No scan acquired. |
| Busije | A [2023 city planning document](https://urbanizam.pozarevac.rs/download/lokacijski-uslovi-juzni-sliv-izmena-pozarevac/?refresh=68077cc3c0e631745321155&wpdmdl=688) identifies the southern drainage area as Busije. A [city-hosted transport study](https://pozarevac.rs/wp-content/uploads/2019/10/Studija-Pozarevac-2019-Konacno-Ispravka-maj-2019_compressed.pdf) lists a Busije–Požarevac route. | Historical documents do not establish current bus timetables/fares or district boundaries. Avoid confusion with other places named Busije. |
| Kruška / “brdo Kruška” | A [city environmental noise report](https://zzs.pozarevac.rs/wp-content/uploads/2025/08/buka-jul-2025.pdf) names “PP odreda – Kruška 1 (kod Ljubićice)”. | This supports local use of Kruška, **not** a verified official hill called “brdo Kruška”, summit coordinates or elevation. User field references or authoritative topographic evidence are still needed. |

Keep original place names as display labels and stable ASCII IDs for saved references, with source/date fields. Do not invent coordinates, terrain or boundaries from a name alone.

## Data choices and constraints

| Source | VKT use | Access, rights and coverage |
| --- | --- | --- |
| [OpenStreetMap](https://www.openstreetmap.org/copyright) | Offline road/building layout and available height tags. | ODbL and attribution apply. Footprints do not provide facade photos, interiors or verified complete heights. Do not assume arbitrary public tile servers allow bulk downloads. |
| [RGZ GeoSrbija](https://www.rgz.gov.rs/geo-srbija) | Identify authoritative national layers and request appropriate exports. | [RGZ service terms](https://www.rgz.gov.rs/uslovi-kori%C5%A1%C4%87enja-elektronskih-servisa) are not a blanket open redistribution license. Verify each layer's owner, permission, fee and credentials. No dataset licensed/downloaded here. |
| [Copernicus DEM](https://dataspace.copernicus.eu/explore-data/data-collections/copernicus-contributing-missions/collections-description/COP-DEM) | Broad regional relief: GLO-30 roughly 30 m sampling, GLO-90 roughly 90 m; inadequate for curbs and steps. | Collection documentation discusses global GLO-30/90 availability. A [July 2026 notice](https://dataspace.copernicus.eu/news/2026-7-17-copernicus-dem-30m-view-service-license-acceptance) restricts the **30 m view service** to authorized categories from 28 July. Check the exact instance/download route, accepted license and account eligibility; a dataset license does not guarantee every service endpoint is unrestricted. |
| [Mapillary licensing](https://help.mapillary.com/hc/en-us/articles/115001770409-CC-BY-SA-license-for-open-data) | Street-level reference where actual local sequences exist. | Official imagery licensing identifies CC BY-SA. Preserve asset-specific attribution/share-alike requirements. Review [API access](https://www.mapillary.com/developer/api-documentation/) separately; no token provisioned. Požarevac coverage not verified. |
| [KartaView terms](https://kartaview.org/terms) | Alternative street-level references. | Terms distinguish MIT code from CC BY-SA 4.0 imagery/spatial data. Verify asset attribution, current endpoint/authentication requirements and local coverage. None tested here. |
| [Google Photorealistic 3D Tiles](https://developers.google.com/maps/documentation/tile/3d-tiles) | Optional online visualization after budget/product-use checks. | Billing, API enablement and key required. [Policies](https://developers.google.com/maps/documentation/tile/policies) restrict caching, extraction and offline uses. It is not a source for ripping an offline redistributable city mesh. Exact Požarevac 3D coverage is unverified; EEA billing accounts have different terms/functionality. |
| [Cesium ion content](https://cesium.com/platform/cesium-ion/content/) | Globe positioning and separately licensed streamed content behind an optional adapter. | Check token scopes, plan quotas, provider terms and actual coverage. Platform/code availability does not relicense third-party assets. No new paid service or coverage verification performed. |
| User-owned captures | Recognizable local facades and authentic ambience. | Retain photographer/recorder permission and review publication rights. Public online images are not automatically free assets. Keep raw captures outside Git. |

Budget recommendation: OSM layout → licensed regional relief → authored landmarks from reviewed references → selected user-owned scans. Keep online services optional and preserve an offline fallback.

## Multi-city implementation plan — not delivered here

1. Keep a persistent bootstrap/player/session layer and load city chunks additively. A manifest should specify city ID, content version, geographic anchor, coordinate system, chunk IDs, safe arrival anchors and provenance. Choose chunk/texture budgets from real phone measurements.
2. Use stable player/profile IDs independent of callsign and city. Save city/chunk/arrival IDs plus coordinates with their origin/version. Cash, reputation, inventory, completed street-job count and story unlocks stay with the existing player; do not clone the economy on city entry.
3. Migrate saves explicitly. Removed chunks or invalid positions fall back to a validated safe arrival. Travel charges commit once per transaction and roll back when loading fails.
4. A bus interaction validates destination/unlock, saves a safe checkpoint and starts [LoadSceneAsync](https://docs.unity3d.com/6000.0/Documentation/ScriptReference/SceneManagement.SceneManager.LoadSceneAsync.html). Keep the old city until destination assets, ground colliders and arrival anchor are ready. Activate the destination, move the same player, switch origin/ambience, then unload the old chunk.
5. The requested **3–5 second minimum** belongs to a labeled travel presentation, not fake loading progress. If assets finish early, show “Spremno — polazak” while the travel shot finishes. If loading is slower, show the actual phase and a recoverable error/return action. Never claim a made-up download percentage or release the player before ground readiness.
6. Test missing bundles, offline startup, interrupted travel, duplicate input/charges, save during transition, death before boarding, phone background/resume, arrival collision and memory pressure. A second city is delivered only after a playable route and save reload on actual target platforms.

Chapter unlocks may use persisted job counts. Use fictional contacts and authored mission locations instead of associating real residents/private addresses with criminal behavior.

## User capture kit

Suggested folder, not created here:

`C:\Users\jamal\Documents\VKT-Captures\pozarevac\2026-10-15\`

Subfolders: `facades\tulba`, `facades\busije`, `facades\cacalica`, `facades\kruska-unverified`, `landmarks`, `audio`, `measurements`, `rights`, `reviewed-export`. Keep originals separate; import only reviewed reduced assets. Review EXIF/location metadata before publication.

- Photograph from permitted public pedestrian positions. Avoid identifiable people and number plates; remove/redact accidental captures before export. Do not enter private courtyards, restricted sites or traffic lanes to complete a shot.
- The [Meshroom capture guide](https://meshroom-manual.readthedocs.io/en/latest/capturing/capturing.html) recommends at least **60% side overlap and 80% frontal overlap**, diffuse lighting, fixed focal length and avoidance of moving/reflective/transparent surfaces. This 60–80% overlap pattern is a starting capture target, not a reconstruction guarantee.
- Include whole facade, corners, entrance, roofline and non-sensitive architectural details. Note approximate dimensions from accessible known references and label estimates. Record orientation and visible building side; do not imply survey precision.
- Record several 30–60 second ambient clips at safe public positions for morning/day/evening. Keep private conversations, intelligible incidental speech and copyrighted background music out of usable recordings. Prefer original WAV and retain sample rate, broad location, time-of-day and recorder permission.
- Maintain a manifest: author, capture date, place label, source files, release scope, edits, dimension confidence and excluded sensitive content. Synthetic sound must remain labeled synthetic.

## Free reconstruction tools

[Meshroom/AliceVision](https://github.com/alicevision/Meshroom) supplies an open-source node pipeline with MPL-2.0 license material; check the chosen release's components and hardware requirements. [COLMAP's tutorial](https://colmap.github.io/tutorial.html) describes its reconstruction workflow; its [license](https://colmap.github.io/license.html) is BSD for COLMAP itself, with separate dependency terms. Neither creates evidence of unseen surfaces. Clean/decimate reviewed meshes, build LODs, bake textures and author simple collision before Unity import. Measure phone texture memory and reconstruction cost.

## Character direction

An optional protagonist can reflect the user's explicitly supplied urban programmer persona: street-programmer clothing and a portable-computer/phone motif with fictional courier contacts. This is a symbolic design, not a claimed facial likeness, verified biography or personal history. A closer likeness needs explicit user-provided reference and choice. Keep personal email, home location and family material out of exported story assets unless separately requested.
