"""Retired VITS generation entrypoint; read-only reviewed-artifact checks remain.

The historical Coqui stack has eight dependency advisories and no verified
compatible patched replacement. --check-model hashes local files without
importing a model runtime. Existing models and WAVs are never changed.
"""
import argparse
import hashlib
import json
from pathlib import Path

MODEL_FILES = {
    "model.pth": (346592643, "7b43231864dbac69901155ed397c1a30d0d06b066b03f0348fd39eed2ea1d4b0"),
    "config.json": (8412, "a0a14385a21854b970cee364a853950aa6168e690beb8c7c35d1673ea042d5c8"),
    "speaker_ids.json": (20, "4d877c09f8dca306307c51a1d0070d5fc493615eed11fdadc7d55d2976f685c8"),
    "language_ids.json": (15, "d249880c338370db1ee4df26207f541512cd05c275f2c0aafedf27e20abeccf4"),
}


def validate_model_directory(model):
    """Check the reviewed artifacts without parsing or loading the checkpoint.

    These are previously reviewed public intake hashes, not a configurable trust
    list. No later inference reopens these paths; generation is disabled.
    """
    for name, (size, expected) in MODEL_FILES.items():
        path = model / name
        if path.is_symlink() or not path.is_file() or path.stat().st_size != size:
            raise ValueError("Reviewed model artifact is missing or has an unexpected size: " + name)
        digest = hashlib.sha256()
        with path.open("rb") as stream:
            for block in iter(lambda: stream.read(1024 * 1024), b""):
                digest.update(block)
        if digest.hexdigest() != expected:
            raise ValueError("Artifact does not match the reviewed pinned model: " + name)


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--model-dir", type=Path, required=True)
    parser.add_argument("--check-model", action="store_true", help="Read-only size/SHA256 check; no synthesis, imports or writes")
    parser.add_argument("--output", type=Path, help="Retired generation option; always refused")
    parser.add_argument("--text", help=argparse.SUPPRESS)
    args = parser.parse_args()
    if not args.check_model or args.output is not None or args.text is not None:
        parser.error("VITS synthesis is disabled pending a compatible maintained runtime. Existing audio is preserved. Use --check-model without --output/--text for a read-only check; see Docs/Voice-Review-20261018.md.")
    model = args.model_dir.resolve(strict=True)
    validate_model_directory(model)
    print(json.dumps({"verifiedArtifacts": len(MODEL_FILES), "synthesisEnabled": False,
                      "dependenciesImported": False, "writes": False,
                      "evidenceLimit": "Artifact identity only; not model safety, inference or naturalness approval."}))


if __name__ == "__main__":
    main()
