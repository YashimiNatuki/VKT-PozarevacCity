import importlib.util
from pathlib import Path
import tempfile
import unittest

import numpy as np
import rasterio
from rasterio.transform import from_origin

spec = importlib.util.spec_from_file_location("audit", Path(__file__).with_name("audit_elevation.py"))
module = importlib.util.module_from_spec(spec)
spec.loader.exec_module(module)


class ElevationAuditTests(unittest.TestCase):
    def test_valid_sample_and_provenance_do_not_enable_terrain(self):
        with tempfile.TemporaryDirectory() as temp:
            path = Path(temp)/"fixture.tif"
            with rasterio.open(path, "w", driver="GTiff", width=4, height=4, count=1,
                               dtype="float32", crs="EPSG:4326", transform=from_origin(21,45,.01,.01), nodata=-9999) as output:
                output.write(np.full((4,4), 80, dtype="float32"), 1)
            point = dict(name="synthetic", latitude=44.985, longitude=21.015)
            report = module.audit(path, [point], "https://example.org/synthetic", "synthetic datum")
            self.assertEqual(report["points"][0]["surfaceHeightMeters"], 80)
            self.assertFalse(report["runtimeEnabled"])
            self.assertFalse(report["points"][0]["streetHeightVerified"])
            self.assertEqual(len(report["sha256"]), 64)
            with self.assertRaises(ValueError):
                module.audit(path, [dict(point, latitude=50)], "synthetic", "synthetic")
            with self.assertRaises(ValueError):
                module.audit(path, [dict(point, longitude=float("nan"))], "synthetic", "synthetic")

    def test_nodata_is_rejected(self):
        with tempfile.TemporaryDirectory() as temp:
            path = Path(temp)/"nodata.tif"
            with rasterio.open(path, "w", driver="GTiff", width=4, height=4, count=1,
                               dtype="float32", crs="EPSG:4326", transform=from_origin(21,45,.01,.01), nodata=-9999) as output:
                output.write(np.full((4,4), -9999, dtype="float32"), 1)
            with self.assertRaises(ValueError):
                module.audit(path, [dict(name="missing", latitude=44.985, longitude=21.015)], "synthetic", "synthetic")


if __name__ == "__main__":
    unittest.main()
