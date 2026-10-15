# Offline OpenStreetMap road layout

The bundled `Assets/CityGame/Resources/Maps/PozarevacStreets.json` is a derived OpenStreetMap database under **ODbL 1.0**, separately from the game's original source code. Attribution: **© OpenStreetMap contributors**. License and contributors: https://www.openstreetmap.org/copyright . Full ODbL: https://opendatacommons.org/licenses/odbl/1-0/ . Distribute this derived JSON and this notice with builds/repository; preserve source, attribution and license metadata. Reusers of the derived database must comply with ODbL share-alike obligations.

## Provenance and scope

Public read-only API snapshot obtained 2026-10-14. Exact download time, endpoint and bbox appear inside the JSON. Source bbox: `21.1818,44.6174926,21.1890,44.6224926`. Anchor `21.1854,44.6199926` comes from one public Nominatim query for `Pozarevac,Serbia`, returning OSM relation **6308411**. This is the town feature reference point, not a surveyed civic landmark. Query provenance: https://nominatim.openstreetmap.org/search?q=Pozarevac%2CSerbia&format=jsonv2&limit=1 . The refreshed dataset includes actual named road segments for **Tabačka čaršija, Stari korzo, Trg oslobođenja and Lenjinova**, plus other surrounding roads. The original gameplay georeference is not changed by the importer.
Importer retains at most 200 actual node-to-node highway segments with both endpoints within a 285m east/north square. Segments with endpoints outside are omitted, never bridged. IDs are OSM *way feature IDs* for provenance; all contributor usernames, contributor IDs, changeset identity and unrelated tags are discarded. Full XML remains temporary outside the repository. The output is a deliberately bounded partial road dataset, not every street in town.

`CityOsmStreets.Initialize(Transform georoot, Vector3 groundAnchor)` loads the bundled JSON without runtime requests. East maps to local +X; north to +Z. It renders the actual recorded centerline layout as road ribbon meshes over one flat walkable square. Road widths and elevation are authored defaults. Up to 40 actual OSM building footprints are also retained and extruded as facade perimeters. OSM height is used when numeric; otherwise levels times 3 meters, otherwise an explicitly authored 9m default. The JSON records heightSource. Flat roofs use bounded ear clipping for simple concave footprints, with degenerate/self-intersecting polygons skipped. No building interiors or facade photographs are supplied. An original four-panel AI-illustrated facade atlas provides authored shopfront textures; it does not reproduce particular buildings. Curbs, junction polygons, window positions and architectural accuracy are not inferred from centerlines. This is a real data improvement over the optional authored center fixture, with a distinct visual/fidelity limitation.

Choose either this real-road-layout view or the authored `CityCityDetail` fixture; do not superimpose them or claim authored landmark offsets are surveyed. Road meshes have no colliders; the shared flat ground supplies walking collision. `SegmentCount`, `Source`, and static `Load()` support verification. Attribution is visible on-screen.

## Refresh

Run `Tools/Maps/Import-OsmCenter.ps1` for a single bounded public download; or supply `-InputXml <local-file>` to avoid another download. The script does not persist the source XML it downloads. Do not poll the editing API. For ongoing bulk ingestion, use appropriate OSM extracts/Overpass services and respect their policies.

API source documentation: https://wiki.openstreetmap.org/wiki/API_v0.6#Retrieving_map_data_by_bounding_box:_GET_/api/0.6/map . The wiki itself recommends extracts/Overpass for nonediting bulk use. API implementation: https://github.com/openstreetmap/openstreetmap-website/tree/master/app/controllers/api .

## Verification

Importer executed against a successful public API response; produced 200 road segments, 21 distinct named streets and 40 bounded building footprints. Source identities were omitted. Runtime compilation, road mesh orientation, geographic alignment and visual review remain required in Unity. No claim of a full geographically accurate Požarevac is made.
