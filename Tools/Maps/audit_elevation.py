"""Audit a local licensed height raster; never changes Unity terrain or scene files.

Run with the isolated VKT toolchain Python (rasterio, pyproj, numpy).
Input points: JSON array of {name, latitude, longitude}. Heights are surface
samples, not surveyed street elevations. Out-of-raster/no-data fail closed.
"""
import argparse
import hashlib
import json
import math
from pathlib import Path

import numpy as np
import rasterio
from pyproj import Transformer


def audit(raster, points, source_url, vertical_datum):
    digest = hashlib.sha256()
    with Path(raster).open("rb") as stream:
        for block in iter(lambda: stream.read(1024 * 1024), b""):
            digest.update(block)
    if not points or len(points) > 128:
        raise ValueError("Expected 1-128 audit points")
    result = []
    with rasterio.open(raster) as dataset:
        if dataset.crs is None or dataset.count != 1:
            raise ValueError("A single height band and an explicit CRS are required")
        project = Transformer.from_crs("EPSG:4326", dataset.crs, always_xy=True)
        for point in points:
            lat, lon = float(point["latitude"]), float(point["longitude"])
            if not math.isfinite(lat) or not math.isfinite(lon) or not -90 <= lat <= 90 or not -180 <= lon <= 180:
                raise ValueError("Invalid geographic coordinate")
            x, y = project.transform(lon, lat)
            row, col = dataset.index(x, y)
            if not 0 <= row < dataset.height or not 0 <= col < dataset.width:
                raise ValueError(f"Point outside raster: {point['name']}")
            window = rasterio.windows.Window(max(0, col-1), max(0, row-1), min(3, dataset.width-max(0, col-1)), min(3, dataset.height-max(0, row-1)))
            neighborhood = dataset.read(1, window=window, masked=True)
            height = next(dataset.sample([(x, y)], masked=True))[0]
            if np.ma.is_masked(height) or not np.isfinite(height) or neighborhood.count() == 0:
                raise ValueError(f"Missing elevation: {point['name']}")
            result.append(dict(name=point["name"], latitude=lat, longitude=lon,
                               surfaceHeightMeters=round(float(height), 3),
                               neighborhoodRangeMeters=round(float(neighborhood.max()-neighborhood.min()), 3),
                               locationVerified=False, streetHeightVerified=False))
        metadata = dict(crs=str(dataset.crs), width=dataset.width, height=dataset.height,
                        spacing=list(dataset.res), nodata=dataset.nodata)
    return dict(schema=1, source=source_url, sha256=digest.hexdigest(), raster=metadata,
                verticalDatum=vertical_datum, runtimeEnabled=False,
                warning="DSM includes buildings and vegetation. Candidate locations and street heights require review. Existing flat gameplay remains authoritative.",
                points=result)


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--raster", type=Path, required=True)
    parser.add_argument("--points", type=Path, required=True)
    parser.add_argument("--source-url", required=True)
    parser.add_argument("--vertical-datum", required=True)
    parser.add_argument("--output", type=Path, required=True)
    args = parser.parse_args()
    if args.output.exists():
        raise FileExistsError("Audit output already exists; choose a new filename")
    report = audit(args.raster, json.loads(args.points.read_text(encoding="utf-8-sig")), args.source_url, args.vertical_datum)
    args.output.parent.mkdir(parents=True, exist_ok=True)
    args.output.write_text(json.dumps(report, ensure_ascii=False, indent=2, allow_nan=False)+"\n", encoding="utf-8")
    print(f"Audited {len(report['points'])} surface samples; runtime terrain unchanged")


if __name__ == "__main__":
    main()
