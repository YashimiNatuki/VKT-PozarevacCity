"""Export the reviewed Copernicus DSM as a small map-only, south-to-north grid.

Uses the existing isolated GIS Python. Never changes a scene, road or collider.
The source hash is mandatory; EGM2008/DSM metadata refers to this reviewed tile.
"""
import argparse
import hashlib
import json
import math
from pathlib import Path

import numpy as np
import rasterio
from rasterio.windows import Window

SOURCE = "https://copernicus-dem-30m.s3.amazonaws.com/Copernicus_DSM_COG_10_N44_00_E021_00_DEM/Copernicus_DSM_COG_10_N44_00_E021_00_DEM.tif"
NOTICE = "produced using Copernicus WorldDEM-30 © DLR e.V. 2010-2014 and © Airbus Defence and Space GmbH 2014-2018 provided under COPERNICUS by the European Union and ESA; all rights reserved."
DISCLAIMER = "The organisations in charge of the Copernicus programme by law or by delegation do not incur any liability for any use of the Copernicus WorldDEM-30."


def export(raster, osm, expected_sha, radius=2000, size=129):
    with Path(raster).open("rb") as stream:
        digest = hashlib.file_digest(stream, "sha256").hexdigest()
    if digest != expected_sha:
        raise ValueError("Source raster hash mismatch")
    lat, lon = float(osm["anchorLatitude"]), float(osm["anchorLongitude"])
    if not all(map(math.isfinite, (lat, lon, radius))) or not -85 <= lat <= 85 or not -180 <= lon <= 180:
        raise ValueError("Invalid geographic anchor")
    if not 100 <= radius <= 5000 or not 3 <= size <= 257 or size % 2 == 0:
        raise ValueError("Expected bounded radius and odd grid size")
    axis = np.linspace(-radius, radius, size)
    xs, zs = np.meshgrid(axis, axis)
    longitudes = lon + xs / (111320 * math.cos(math.radians(lat)))
    latitudes = lat + zs / 111320
    with rasterio.open(raster) as dataset:
        if dataset.count != 1 or dataset.crs != rasterio.crs.CRS.from_epsg(4326):
            raise ValueError("Expected single-band WGS84 raster")
        # Raster affine locates cell corners; interpolation uses actual pixel centers.
        inverse = ~dataset.transform
        columns = inverse.a * longitudes + inverse.b * latitudes + inverse.c
        rows = inverse.d * longitudes + inverse.e * latitudes + inverse.f
        columns, rows = columns - .5, rows - .5
        if columns.min() < 0 or rows.min() < 0 or columns.max() >= dataset.width - 1 or rows.max() >= dataset.height - 1:
            raise ValueError("Region outside raster interpolation bounds")
        x0, y0 = int(np.floor(columns.min())), int(np.floor(rows.min()))
        x1, y1 = int(np.floor(columns.max())) + 2, int(np.floor(rows.max())) + 2
        samples = dataset.read(1, window=Window(x0, y0, x1-x0, y1-y0), masked=True).filled(np.nan)
        columns, rows = columns - x0, rows - y0
        ix, iy = np.floor(columns).astype(int), np.floor(rows).astype(int)
        u, v = columns - ix, rows - iy
        values = (samples[iy, ix] * (1-u) + samples[iy, ix+1] * u) * (1-v) + (samples[iy+1, ix] * (1-u) + samples[iy+1, ix+1] * u) * v
        if not np.isfinite(values).all() or values.min() < -500 or values.max() > 9000:
            raise ValueError("Missing or implausible surface heights")
        spacing = [abs(float(value)) * 3600 for value in dataset.res]
    return dict(schema=1, usage="map-only", source=SOURCE, sourceSha256=digest,
                sourceSpacingArcSeconds=spacing, verticalDatum="EGM2008", surfaceModel="DSM",
                streetHeightsVerified=False, anchorLatitude=lat, anchorLongitude=lon,
                radiusMeters=radius, gridSize=size, order="south-to-north-row-major",
                minimumMeters=round(float(values.min()), 2), maximumMeters=round(float(values.max()), 2),
                heightsMeters=np.round(values, 2).flatten().tolist(), attribution=NOTICE, disclaimer=DISCLAIMER,
                license="https://documentation.dataspace.copernicus.eu/APIs/SentinelHub/Data/DEM/resources/license/License-COPDEM-30.pdf")


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--raster", type=Path, required=True)
    parser.add_argument("--osm", type=Path, required=True)
    parser.add_argument("--sha256", required=True)
    parser.add_argument("--output", type=Path, required=True)
    args = parser.parse_args()
    if args.output.exists():
        raise FileExistsError("Preserve the previous export; choose a new output")
    result = export(args.raster, json.loads(args.osm.read_text(encoding="utf-8-sig")), args.sha256)
    args.output.parent.mkdir(parents=True, exist_ok=True)
    args.output.write_text(json.dumps(result, ensure_ascii=False, separators=(",", ":"), allow_nan=False)+"\n", encoding="utf-8")
    print(json.dumps(dict(passed=True, validationExit=0, samples=len(result["heightsMeters"]),
                         minimumMeters=result["minimumMeters"], maximumMeters=result["maximumMeters"], usage=result["usage"])))


if __name__ == "__main__":
    main()
