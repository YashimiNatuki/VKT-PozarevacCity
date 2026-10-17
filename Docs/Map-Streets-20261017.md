# Connected street geometry and bounded map rendering

## Existing data, new composition

The existing OSM snapshot and geographic origin are unchanged. Road widths remain authored game widths, not survey measurements. The existing playable ground remains flat; this update does not invent hills, rescale the player or claim new neighborhood coverage.

Previously each road segment had its own rectangular road/sidewalk edges. Adjacent pieces could leave corner seams, and sidewalks continued through junction approaches. The new composition builds an endpoint incidence graph keyed at centimetre precision. Degree-two nodes share a bounded miter offset in either travel direction. Road and sidewalk ribbons reuse those edge positions across adjacent segments, including separately serialized roads. Miters are capped at 1.25 times half-width to bound acute-turn spikes. Reversals have a finite normal fallback. Triangle winding is explicitly upward; degenerate triangles are discarded.

Nodes with at least three incident segments gain a 24-sided authored asphalt junction surface. Vehicle-road sidewalks pull back at such approaches, with trim bounded to 45 percent of each segment, so short segments retain a finite middle span. These are game street treatments derived from real OSM node connectivity; they are not surveyed intersection curbs, lane counts or traffic rules. Existing source segment limits and footprint collision remain in force.

Street names are now compact double-sided placards with posts, rather than oversized floating labels. Their wording comes from the existing source; physical sign locations, dimensions and appearance are authored artwork, not photographed municipal signage.

## Renderer visibility budget

`CityMapVisibility` registers only OSM building and sign renderers. Building surfaces hide beyond 400 m from their bounds and return within 360 m; signs hide at 90 m and return within 75 m. This hysteresis avoids repeated toggles around a single threshold. Checks run about five times per second, with an immediate check after a camera move of 32 m. An initial disabled renderer is never accidentally enabled. Disabling the component restores initial visibility. Destroyed renderer references are tolerated during teardown.

Colliders, GameObjects, street-job markers, NPCs and save state remain active. This is a bounded presentation control, not scene streaming, terrain chunk unloading or a measured FPS claim. Native Unity already performs frustum culling. Larger maps should graduate to spatial chunks and the [Unity CullingGroup API](https://docs.unity.com/en-us/engine/6000.0/manual/cameras/occlusion-culling/culling-group-api/getstarted) after measurement; the present small map does not need an unverified external streaming framework.

## Terrain and technology research

Super Map World's published catalog covers country/world graphics rather than detailed city streets or local terrain, so it is not used as a Požarevac gameplay source.

The [official Copernicus DEM description](https://dataspace.copernicus.eu/explore-data/data-collections/copernicus-contributing-missions/collections-description/COP-DEM) identifies the product as a surface model, including structures and vegetation, unlike a bare-earth DTM. Its nominal 30 m sampling does not supply street curb or doorway precision. Raw height samples should therefore not be dropped directly under the existing fixed-height mission stations. No Copernicus data is downloaded or imported in this wave, and no satellite texture is claimed.

The practical next terrain pipeline is: obtain a licensed, attributed local height raster; verify CRS, vertical datum, no-data and units; project it into the existing metre origin; remove/regularize urban surface artifacts; explicitly flatten/smooth road corridors; then place building bases, props, spawn and mission anchors against the same height sampler. Compare slopes and collider seams before enabling it. Keep the current flat fixture as a regression route. Physical park/hill landmarks need separately reviewed geometry and source/reference imagery.

Priority after this release: review street-scale captures and walk junctions; add authored crossings/curbs and recognizable landmarks; benchmark draw calls/frame time and memory on target hardware; then expand the OSM/height dataset and chunk it. More data alone does not prove a realistic or navigable city.

## Verification

EditMode adds shared-corner/reversal bounds, upward junction mesh tests, renderer hysteresis with active colliders and bundled-map composition checks. The Windows `-vktMapUpgradeSmoke` path checks generated junctions, hiding/restoring renderers and retaining the actual map collider count, then captures a development aerial view. Prior campaign, garden, combat, character, audio, intro and save checks remain part of the player regression. The final release receipt records actual counts and artifact hashes.

New screenshots are actual development-player renders from a temporary aerial camera; they are not satellite captures, survey reconstructions or proof of every street being traversable. Android hardware performance and a new APK remain separate work.
