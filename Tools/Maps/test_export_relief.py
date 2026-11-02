import hashlib
import importlib.util
from pathlib import Path
import tempfile
import unittest

import numpy as np
import rasterio
from rasterio.transform import from_origin

spec = importlib.util.spec_from_file_location("relief", Path(__file__).with_name("export_relief.py"))
module = importlib.util.module_from_spec(spec)
spec.loader.exec_module(module)


class ReliefExportTests(unittest.TestCase):
    def fixture(self, directory, nodata=False):
        path = Path(directory) / "synthetic.tif"
        values = np.repeat(np.arange(10, dtype="float32")[:, None], 10, axis=1) + 80
        if nodata:
            values[:] = -9999
        with rasterio.open(path, "w", driver="GTiff", width=10, height=10, count=1,
                           dtype="float32", crs="EPSG:4326", transform=from_origin(21, 45, .01, .01), nodata=-9999) as output:
            output.write(values, 1)
        return path, hashlib.sha256(path.read_bytes()).hexdigest()

    def test_georeferenced_grid_is_south_to_north_and_retains_provenance(self):
        with tempfile.TemporaryDirectory() as directory:
            path, digest = self.fixture(directory)
            data = module.export(path, dict(anchorLatitude=44.95, anchorLongitude=21.05), digest, radius=500, size=5)
            self.assertEqual(data["sourceSha256"], digest)
            self.assertEqual(data["verticalDatum"], "EGM2008")
            self.assertFalse(data["streetHeightsVerified"])
            self.assertEqual(data["usage"], "map-only")
            self.assertAlmostEqual(data["heightsMeters"][12], 84.5, places=2)
            self.assertGreater(data["heightsMeters"][2], data["heightsMeters"][22])

    def test_no_data_and_wrong_source_hash_are_rejected(self):
        with tempfile.TemporaryDirectory() as directory:
            path, digest = self.fixture(directory, nodata=True)
            for expected in [digest, "0" * 64]:
                with self.assertRaises(ValueError):
                    module.export(path, dict(anchorLatitude=44.95, anchorLongitude=21.05), expected, radius=500, size=5)

    def test_outside_raster_and_nonfinite_anchor_are_rejected(self):
        with tempfile.TemporaryDirectory() as directory:
            path, digest = self.fixture(directory)
            for latitude in [50, float("nan")]:
                with self.assertRaises(ValueError):
                    module.export(path, dict(anchorLatitude=latitude, anchorLongitude=21.05), digest, radius=500, size=5)


if __name__ == "__main__":
    unittest.main()
