"""Generate one local Serbian VITS candidate; no download or Unity admission.

Use an isolated environment with requirements-vits.txt and the reviewed pinned
daremc86/sr-cv-vits model files. Run from any directory. Human pronunciation and
naturalness review remains required; successful synthesis is not that review.
"""
import argparse
import hashlib
import json
import os
from pathlib import Path
import tempfile
import wave

MODEL_SHA256 = "7b43231864dbac69901155ed397c1a30d0d06b066b03f0348fd39eed2ea1d4b0"


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--model-dir", type=Path, required=True)
    parser.add_argument("--output", type=Path, required=True)
    parser.add_argument("--text", default="Добро дошао у штек. Провери телефон и следећи уговор.")
    args = parser.parse_args()
    model = args.model_dir.resolve(strict=True)
    output = args.output.resolve()
    if output.exists():
        raise FileExistsError("Candidate output already exists; choose a new filename")
    if not args.text.strip() or len(args.text) > 500:
        raise ValueError("Use a non-empty reviewed line of at most 500 characters")
    digest = hashlib.sha256()
    with (model/"model.pth").open("rb") as stream:
        for block in iter(lambda: stream.read(1024*1024), b""):
            digest.update(block)
    if digest.hexdigest() != MODEL_SHA256:
        raise ValueError("Checkpoint does not match the reviewed pinned model")
    config = json.loads((model/"config.json").read_text(encoding="utf-8"))
    if config.get("model") != "vits" or config.get("phoneme_language") != "sr" or config.get("characters", {}).get("characters_class") != "TTS.tts.models.vits.VitsCharacters":
        raise ValueError("Expected the reviewed Serbian VITS configuration")
    for name in ["speaker_ids.json", "language_ids.json"]:
        json.loads((model/name).read_text(encoding="utf-8"))
    os.environ["TORCH_FORCE_WEIGHTS_ONLY_LOAD"] = "1"
    os.environ["OMP_NUM_THREADS"] = "2"
    import torch
    from TTS.api import TTS
    torch.set_num_threads(2)
    previous = Path.cwd()
    output.parent.mkdir(parents=True, exist_ok=True)
    try:
        os.chdir(model)  # The publisher configuration references relative ID files.
        engine = TTS(model_path=str(model/"model.pth"), config_path=str(model/"config.json"), progress_bar=False).to("cpu")
        with tempfile.TemporaryDirectory(prefix="vkt-vits-") as directory:
            candidate = Path(directory)/"candidate.wav"
            engine.tts_to_file(text=args.text, file_path=str(candidate))
            with wave.open(str(candidate), "rb") as audio:
                if audio.getnchannels() != 1 or audio.getframerate() != 22050 or not 0 < audio.getnframes()/22050 <= 60:
                    raise ValueError("Generated candidate has invalid format or duration")
            # Exclusive creation also preserves an output created by another process.
            with output.open("xb") as destination:
                destination.write(candidate.read_bytes())
    finally:
        os.chdir(previous)
    print("Generated local candidate; no Unity assets or voices were replaced")


if __name__ == "__main__":
    main()
