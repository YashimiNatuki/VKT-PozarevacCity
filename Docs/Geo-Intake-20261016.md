# Geographic document intake and implementation — 2026-10-16

The user supplied two local design documents and 18 candidate coordinates. They are design/reference inputs, not evidence that their proposed systems are installed. Original documents remain in Downloads and are not republished.

## Applied now

- Expanded the offline center from ±285 m to ±650 m: 600 OSM road segments, 100 selected footprints, 22 park/garden/grass outlines. Road widths, flat ground, 1.5 m visual sidewalks, facade materials and park furniture are authored. This is a bounded 1.3 km square, not all neighborhoods or a scanned city.
- Added a phone map, stable candidate-location IDs and an explicitly bounded local projection. Candidate coordinates retain user-supplied provenance; none becomes an authoritative district boundary, terrain elevation or travel spawn automatically.
- Kept the existing geographic anchor (44.6199926, 21.1854) to preserve current map/save alignment. The suggested Opština origin is retained as a candidate; changing the origin needs explicit position/save migration.
- Coordinates map longitude/easting to local X, latitude/northing to local Z and height to Y. The current approximation uses double arithmetic before conversion to nearby float meters, bounded to 20 km. Larger cities require a separate origin and Cesium/ECEF conversion, not absolute degrees in Unity physics.
- Existing player capsule is 1.8 m high with a 1.62 m camera. Walking 5 m/s and sprinting 8 m/s are arcade tuning, not realistic walking speed. A 1.3 km traversal therefore has actual distance; device testing is required before claiming smooth frame times.

## Coordinate review

All 18 pins in CityGeoRegistry are user-supplied candidates. The Ljubičevo/Hipodrom pin lies about 30 m from the supplied Opština point, so it is explicitly flagged as conflicting and must not spawn an equestrian complex there. Monument height does not establish terrain elevation. The remaining pins still need map/field confirmation. Exact district extents are not derivable from a single point.

## Corrections to proposed stack

- Nanite, Lumen and Unreal RVT/WPO terminology in the master document is not evidence of Unity features. Keep this project on its existing Unity URP pipeline. No engine migration was made.
- MapMagic/Mapbox/FMOD/BASS and online imagery are not automatically free, installed or suitable for redistribution. Review the actual version, license and target-device support before adoption. No paid dependency was added.
- Cesium cartographic clipping is a geographic 2.5D mask, not a general 3D hole/window cutter. Its visual mask does not prove correct playable collision. Authored replacement landmarks need their own colliders and traversal tests. [Official Cesium guide](https://cesium.com/learn/unity/unity-clipping/).
- Google 3D coverage for Požarevac is unverified. Streaming tiles does not grant permission to extract/repackage a redistributable offline city. [Official tile policies](https://developers.google.com/maps/documentation/tile/policies).
- The documents' statements that natural local voices, driving, traffic AI, detailed cultivation simulation or production stability already exist are not repository/runtime evidence. Audio denoising is also distinct from reverb/occlusion. Current gaps remain explicit.

## Next city content and user captures

Prioritize a reviewed center route, then one district pack (Tulba, Busije or Čačalica). Use actual terrain data and user-owned reference/scans before describing a hill as reconstructed. Preserve provenance and collect a measured doorway/curb reference for scale. See [source options and capture instructions](Geo-Expansion-20261015.md).

Multi-city selection, async bus/train transitions, transactional fares and per-city arrival saves are planned, not implemented by this wave. Keep one persistent player/economy and load bounded city content additively. The current generic importer accepts another center/bounds, but a downloaded map alone is not a finished playable city.

## Supplied Python projection example

The later example uses origin latitude44.6200/longitude21.1894 and candidate hipodrom latitude44.6400/longitude21.1650. Its stated radii produce approximately X=-1933.34m,Z=2222.66m. This validates the sign/scale of that example, not the pin's real identity. That example origin is about317m east of the current project anchor; replacing it silently would shift existing geometry/save alignment. The C# registry keeps the current importer-compatible local approximation; higher-accuracy cross-city placement should use Cesium's WGS84/ECEF pipeline. Do not combine the city's hipodrom and Ergela Ljubičevo as one location without verification.
