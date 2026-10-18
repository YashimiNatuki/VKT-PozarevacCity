"""Artifact-boundary regression tests; never imports Torch/Coqui or synthesizes audio."""
import hashlib
import importlib.util
import io
from pathlib import Path
import sys
import tempfile
import unittest
from unittest.mock import patch

spec = importlib.util.spec_from_file_location("vkt_vits_candidate", Path(__file__).with_name("generate_vits_candidate.py"))
candidate = importlib.util.module_from_spec(spec)
spec.loader.exec_module(candidate)


class VitsArtifactTests(unittest.TestCase):
    def setUp(self):
        self.temp = tempfile.TemporaryDirectory()
        self.root = Path(self.temp.name)
        self.manifest = {}
        for name in candidate.MODEL_FILES:
            data = ("reviewed " + name).encode()
            (self.root / name).write_bytes(data)
            self.manifest[name] = (len(data), hashlib.sha256(data).hexdigest())
        self.patch = patch.object(candidate, "MODEL_FILES", self.manifest)
        self.patch.start()

    def tearDown(self):
        self.patch.stop()
        self.temp.cleanup()

    def test_accepts_the_complete_reviewed_set(self):
        candidate.validate_model_directory(self.root)

    def test_same_size_changed_configuration_is_rejected(self):
        path = self.root / "config.json"
        path.write_bytes(b"x" * path.stat().st_size)
        with self.assertRaises(ValueError): candidate.validate_model_directory(self.root)

    def test_same_size_changed_checkpoint_and_ids_are_rejected(self):
        for name in ["model.pth", "speaker_ids.json", "language_ids.json"]:
            path = self.root / name; original = path.read_bytes()
            path.write_bytes(b"x" * len(original))
            with self.assertRaises(ValueError): candidate.validate_model_directory(self.root)
            path.write_bytes(original)

    def test_legacy_generation_refuses_before_creating_output(self):
        output = self.root / "never-created.wav"
        with patch.object(sys, "argv", ["vits", "--model-dir", str(self.root), "--output", str(output)]), patch("sys.stderr", new_callable=io.StringIO):
            with self.assertRaises(SystemExit) as error: candidate.main()
        self.assertEqual(error.exception.code, 2)
        self.assertFalse(output.exists())

    def test_check_mode_never_imports_model_runtime_or_changes_artifacts(self):
        before = {p.name: p.read_bytes() for p in self.root.iterdir()}
        with patch.object(sys, "argv", ["vits", "--model-dir", str(self.root), "--check-model"]), patch("sys.stdout", new_callable=io.StringIO) as output:
            candidate.main()
        self.assertIn('"dependenciesImported": false', output.getvalue())
        self.assertEqual(before, {p.name: p.read_bytes() for p in self.root.iterdir()})
        self.assertNotIn("torch", sys.modules)
        self.assertNotIn("TTS", sys.modules)

    def test_all_sidecars_and_checkpoint_are_required(self):
        for name in self.manifest:
            path = self.root / name
            original = path.read_bytes(); path.unlink()
            with self.assertRaises(ValueError): candidate.validate_model_directory(self.root)
            path.write_bytes(original)

    def test_oversized_artifact_is_rejected_before_hash_read(self):
        (self.root / "config.json").write_bytes(b"oversized" * 100)
        with patch.object(Path, "open", side_effect=AssertionError("Do not read oversized artifact")):
            # Checkpoint is first and valid; limit the case to the sidecar gate.
            with patch.object(candidate, "MODEL_FILES", {"config.json": self.manifest["config.json"]}):
                with self.assertRaises(ValueError): candidate.validate_model_directory(self.root)


if __name__ == "__main__":
    unittest.main()
