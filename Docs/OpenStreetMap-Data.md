# Offline OpenStreetMap city layout — 2026-10-16

The bundled Assets/CityGame/Resources/Maps/PozarevacStreets.json is a derived OpenStreetMap database under **ODbL 1.0**, separately from first-party MIT code. **© OpenStreetMap contributors**. Preserve this notice, source JSON and metadata with distribution. [Copyright and contributors](https://www.openstreetmap.org/copyright), [ODbL](https://opendatacommons.org/licenses/odbl/1-0/).

## Actual scope

The refreshed public API snapshot contains **600 selected road segments, 100 selected building footprints and 22 park/garden/grass outlines**, bounded to ±650 m east/north around longitude 21.1854, latitude 44.6199926. Exact bbox, endpoint and UTC download timestamp are retained in JSON. This is a 1.3 km square, not complete town coverage; budget selection omits some geometry. It does not include reconstructed Tulba/Čačalica relief or all requested neighborhoods.

The anchor originally came from an OSM town-feature Nominatim lookup (relation 6308411), not a surveyed civic landmark. User-supplied alternative origins remain candidate records; no silent save-origin migration was performed.

Only feature IDs, selected geometry/names and height provenance survive conversion. Contributor identity, changesets and unrelated tags are discarded; XML downloads remain temporary outside Git. Roads keep real recorded node-to-node segments. Edges outside the bounded square are omitted rather than bridging gaps. Existing reviewed center way IDs receive import priority; additional buildings intersecting the authored mission clearing are omitted to preserve the playable route. This is a gameplay selection, not a full reconstruction.

## Authored presentation

X=east, Z=north, Y=height. Conversion uses a local double-precision approximation before nearby float meters. Ground remains flat. Road widths are game defaults; 1.5 m visual sidewalk ribbons are authored and may overlap at junctions. No surveyed curb/junction geometry is claimed. Park outlines use bounded ear clipping; invalid simple polygons are skipped. Some green polygons may be recreation grounds/grass, not named municipal parks.

OSM height/levels are used when available (levels ×3m), otherwise an explicit 9 m default. Facades use original AI artwork and have empty interiors. The player scene reports **878 static wall colliders**, within a 2048 cap. Eligible footprints have an authored 2.4 m-wide/2.5m-high visible doorway; roofs have no walking collision. These are gameplay passages, not real entrances.

Park furniture has a fixed cap of 12 benches / 24 trees. Positions are authored inside imported polygons after road/building exclusion checks; they do not identify actual surveyed trees or furniture. Three shared owned materials are cleaned up with the owner. Geometry is static; Android frame times/thermal behavior have not been measured.

The phone map uses a bounded 512×512 owned texture drawn once from the data, with a cached player marker refreshed at 5 Hz. It displays the offline layout north-up, with scale/attribution and explicit out-of-map/hideout text. It is not turn-by-turn navigation or a city travel system.

## Refresh and extension

Tools/Maps/Import-OsmCenter.ps1 accepts center longitude/latitude, RadiusMeters (285–900), RoadBudget (200–900), BuildingBudget (40–160), and optional local InputXml/Output. Default budgets are 650 m/600/100. The runtime has independent caps. Downloading another center is only data intake; a playable city still needs safe arrival, collision, story and save-transition validation. Do not put raw lat/lon degrees into Unity physics/culling boxes.

Current automated geometry tests and actual Windows player checks passed; source/render fidelity remains bounded as above. See Verification-Geo-Audio-20261016.json and Geo-Intake-20261016.md. Earlier 200/40/285m measurements belong to the retained historical receipts.
