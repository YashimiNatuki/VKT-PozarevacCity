# Požarevac places and authored offline center

Verified 2026-10-14. The optional `CityCityDetail` is an authored, bounded gameplay streetscape built from original geometric primitives. It is not a measured street network, a photogrammetry model, or proof of the accuracy of Cesium streaming. Distances, building designs, street orientation and place offsets are invented for gameplay. No imagery, map tiles or building meshes were copied.

## Primary provenance

- City of Požarevac, pedestrian-zone notice: confirms the street/place names Tabačka čaršija, Trg oslobođenja and Lenjinova. https://pozarevac.rs/resenje-o-odredjivanju-privremene-pesacke-zone-u-ulicama-tabacka-carsija-trg-oslobodjenja-i-lenjinovoj-u-pozarevcu-2/?lang=sr&lng=lat
- City of Požarevac, Stari korzo map page: confirms Stari korzo as a named city location. https://pozarevac.rs/prijavni-list-za-ucesce-na-novogodisnjem-i-bozicnom-vasaru-u-pozarevcu/stari-korzo-mapa/?lang=en&lng=lat
- Official Tourism Organization, monument article: places the Knjaz Miloš monument in the central city park before the Okružno zdanje. The game uses only an abstract pedestal and figure; it does not reproduce the original sculpture. https://togp.rs/spomenik-knjazu-milosu?pismo=lat
- City reconstruction information confirms Tabačka čaršija as a city street. https://pozarevac.rs/konferencija-za-medije-tabacka-carsija/?lang=sr&lng=lat

The above official pages do not establish surveyed longitude/latitude for every route point. Therefore this catalog intentionally contains **no claimed geographic coordinates**. `Place.Offset` is an authored local Unity offset, never WGS84. A future accurate import must retain original coordinates, feature IDs, source timestamp and license in a separate geographic dataset.

## Runtime behavior

`CityCityDetail.Initialize(Transform georoot, Vector3 nearspawn, CityWorldAtmosphere atmosphere)` creates 106 meters of authored streetscape, sidewalks, a crosswalk, 12 shop buildings, benches, trees, a small plaza and six warm street lamps. Lamps follow the configured day/night phase; they cast no realtime shadows. Materials are shared per category and destroyed with the owning component. No runtime network or paid service is involved.

Call only in the offline fixture or with an explicit optional feature flag. Pass a ground-level `nearspawn`, not the player eye height. Keep disabled over streamed geometry unless a designer explicitly places and validates it. `PlaceMarker(index)` supplies local route anchors; `CityPlaceCatalog.At(index)` supplies truthful real place names with fictional mission objectives. Existing mission positions are not modified automatically.

## OpenStreetMap boundary

No OSM geometry is bundled by this addition. If OSM roads/buildings are imported later, preserve attribution and ODbL obligations, with a visible `© OpenStreetMap contributors` attribution and license link. The official copyright page describes these requirements and cautions that OSM does not provide unrestricted third-party tile/API services: https://www.openstreetmap.org/copyright

## Validation

Source implementation and primary-source name verification completed. Unity compilation, collision walkthrough, night-light visual proof and mobile performance validation are required before a playable fidelity claim.
