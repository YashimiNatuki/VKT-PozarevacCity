import json
from pathlib import Path
import sys
import tempfile
import unittest

TOOLS = Path(__file__).parent / "marketplace/plugins/yashimi-vkt-studio/scripts"
sys.path.insert(0, str(TOOLS))
from project_snapshot import digest, project_root, source_fingerprint
from verify_release import verify


class VktToolsTests(unittest.TestCase):
    def setUp(self):
        self.temp = tempfile.TemporaryDirectory()
        self.root = Path(self.temp.name)
        (self.root / "Assets/CityGame").mkdir(parents=True)
        (self.root / "Assets/CityGame/model.cs").write_text("class Model {}")
        (self.root / "ProjectSettings").mkdir()
        (self.root / "ProjectSettings/ProjectVersion.txt").write_text("m_EditorVersion: 6000.6.5f1")
        self.xml = self.root / "test.xml"
        self.xml.write_text('<test-run total="2" passed="1" failed="0" skipped="1"/>')
        self.artifact = self.root / "VKT.zip"
        self.artifact.write_bytes(b"known artifact")
        self.receipt = self.root / "receipt.json"
        self.data = dict(buildErrors=0, windowsPlayerPassed=True, physicalDeviceTested=False,
                         testsSha256=digest(self.xml),
                         sourceFingerprint=source_fingerprint(self.root)[0],
                         tests=dict(total=2, passed=1, failed=0, skipped=1),
                         windows=dict(name="VKT.zip", size=self.artifact.stat().st_size, sha256=digest(self.artifact)))
        self.write()

    def tearDown(self):
        self.temp.cleanup()

    def write(self):
        self.receipt.write_text(json.dumps(self.data))

    def run_verify(self):
        return verify(self.root, self.xml, self.receipt, self.artifact)

    def test_binds_current_source_and_artifact_without_claiming_android(self):
        result = self.run_verify()
        self.assertTrue(result["passed"])
        self.assertFalse(result["androidHardwareTested"])
        self.assertTrue(result["testXmlIdentityBound"])
        self.assertFalse(result["completeBuildInputBinding"])
        self.assertFalse(result["releaseApprovalReady"])

    def test_same_test_counts_with_different_xml_are_rejected_when_bound(self):
        self.xml.write_text('<test-run total="2" passed="1" failed="0" skipped="1"><test-case name="other"/></test-run>')
        with self.assertRaises(ValueError): self.run_verify()

    def test_legacy_receipt_is_explicitly_not_xml_identity_bound(self):
        del self.data["testsSha256"];self.write()
        self.assertFalse(self.run_verify()["testXmlIdentityBound"])

    def test_changed_source_is_rejected(self):
        (self.root / "Assets/CityGame/model.cs").write_text("class Changed {}")
        with self.assertRaises(ValueError): self.run_verify()

    def test_same_size_corrupt_artifact_is_rejected(self):
        self.artifact.write_bytes(b"other artifact")
        with self.assertRaises(ValueError): self.run_verify()

    def test_different_test_receipt_is_rejected(self):
        self.data["tests"]["passed"] = 2; self.write()
        with self.assertRaises(ValueError): self.run_verify()

    def test_failed_or_empty_test_run_is_rejected(self):
        for xml in ['<test-run total="2" passed="1" failed="1" skipped="0"/>', '<test-run total="0" passed="0" failed="0" skipped="0"/>']:
            self.xml.write_text(xml)
            with self.assertRaises(ValueError): self.run_verify()

    def test_missing_game_source_is_rejected(self):
        with self.assertRaises(ValueError): project_root(self.root / "ProjectSettings")

    def test_no_runtime_proof_is_rejected(self):
        self.data["windowsPlayerPassed"] = False; self.write()
        with self.assertRaises(ValueError): self.run_verify()

    def test_private_unrelated_files_do_not_change_public_source_fingerprint(self):
        before = source_fingerprint(self.root)
        (self.root / "private-save.json").write_text('{"cash":99}')
        (self.root / "Assets/StreamingAssets").mkdir()
        (self.root / "Assets/StreamingAssets/private-config.json").write_text('{"private":true}')
        self.assertEqual(before, source_fingerprint(self.root))


if __name__ == "__main__":
    unittest.main()
