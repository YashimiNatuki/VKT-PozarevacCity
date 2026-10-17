"""Synthetic CPU checks for VKT's isolated asset and terrain tooling."""
import io
import json
import numpy as np
from pyproj import Transformer
from rasterio.io import MemoryFile
from rasterio.transform import from_origin
from shapely.geometry import LineString
import trimesh


def run():
    # Public city origin, not private device GPS. This imports no real terrain.
    origin = (21.184205, 44.620835)
    forward = Transformer.from_crs("EPSG:4326", "EPSG:32634", always_xy=True)
    backward = Transformer.from_crs("EPSG:32634", "EPSG:4326", always_xy=True)
    projected = forward.transform(*origin)
    restored = backward.transform(*projected)
    assert max(abs(a-b) for a, b in zip(origin, restored)) < 1e-8
    height = np.arange(16, dtype=np.float32).reshape(4, 4)
    with MemoryFile() as memory:
        with memory.open(driver="GTiff", height=4, width=4, count=1, dtype="float32",
                         crs="EPSG:32634", transform=from_origin(*projected, 5, 5), nodata=-9999) as raster:
            raster.write(height, 1)
        with memory.open() as raster:
            assert raster.crs.to_epsg() == 32634
            assert np.array_equal(raster.read(1), height)
    corridor = LineString([(0, 0), (10, 0)]).buffer(3, cap_style="flat")
    assert corridor.is_valid and abs(corridor.area-60) < 1e-8
    sphere = trimesh.creation.icosphere(subdivisions=2, radius=1)
    glb = sphere.export(file_type="glb")
    restored_mesh = trimesh.load(io.BytesIO(glb), file_type="glb", force="mesh")
    assert len(restored_mesh.faces) == len(sphere.faces)
    return {"syntheticFixtures": True, "crsRoundtrip": True, "geoTiffRoundtrip": True,
            "corridorGeometry": True, "glbRoundtrip": True,
            "realTerrainImported": False, "gameAssetsModified": False}


if __name__ == "__main__":
    print(json.dumps(run(), indent=2))
