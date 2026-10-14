"""Bake reviewed local Piper models into the three existing Unity dialogue slots.

No networking, installation, credentials, or runtime model dependency. Existing
clips are preserved unless --overwrite is supplied. Different characters need
separately selected models/speakers; pitch shifting is not a new voice actor.
"""
import argparse
import importlib.util
import json
from pathlib import Path
import subprocess
import sys
import tempfile
import wave

LINES = {
    "mila_enter": "Dobro došao u štek. Priđi saksiji. Otvori katalog za kupovinu semena i proveri stanje biljaka.",
    "vuk_exit": "Ponovo si na ulici. Drži se, brate. Proveri kome prodaješ i uporedi ponude.",
    "luka_complete": "Posao je završen. Proveri telefon i sledeći ugovor.",
}


def run():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--voice-map", type=Path, required=True,
                        help="JSON keyed by mila_enter/vuk_exit/luka_complete; each value has model, speaker_id, license_reviewed")
    parser.add_argument("--output", type=Path, default=Path(__file__).resolve().parents[2] / "Assets/CityGame/Resources/Audio/Voices")
    parser.add_argument("--overwrite", action="store_true")
    parser.add_argument("--check", action="store_true", help="Validate configuration only; never synthesize")
    args = parser.parse_args()
    mapping = json.loads(args.voice_map.read_text(encoding="utf-8-sig"))
    if set(mapping) != set(LINES):
        raise ValueError("Voice map must contain exactly: " + ", ".join(LINES))
    jobs = []
    for slot, text in LINES.items():
        config = mapping[slot]
        if config.get("license_reviewed") is not True:
            raise ValueError(f"{slot}: review engine, voice, and dataset licenses first")
        model = Path(config["model"]).expanduser().resolve(strict=True)
        details = json.loads(Path(str(model) + ".json").read_text(encoding="utf-8-sig"))
        speaker = config.get("speaker_id", 0)
        if not isinstance(speaker, int) or isinstance(speaker, bool) or not 0 <= speaker < details.get("num_speakers", 1):
            raise ValueError(f"{slot}: speaker_id outside model speaker range")
        destination = args.output.resolve() / (slot + ".wav")
        if destination.exists() and not args.overwrite:
            raise FileExistsError(f"{destination}: use --overwrite after reviewing existing clip")
        jobs.append((model, speaker, text, destination))
    if args.check:
        print(f"Validated {len(jobs)} voice configurations. Piper installed: {importlib.util.find_spec('piper') is not None}")
        return
    if importlib.util.find_spec("piper") is None:
        raise RuntimeError("Local Piper unavailable. Install reviewed piper-tts in a separate audio environment; no download was performed.")
    args.output.mkdir(parents=True, exist_ok=True)
    for model, speaker, text, destination in jobs:
        with tempfile.TemporaryDirectory(prefix="vkt-voice-") as temp:
            clip = Path(temp) / "voice.wav"
            subprocess.run([sys.executable, "-m", "piper", "-m", str(model), "--speaker", str(speaker), "-f", str(clip), "--", text], check=True, timeout=180)
            with wave.open(str(clip), "rb") as audio:
                if audio.getnframes() <= 0 or audio.getframerate() < 8000:
                    raise ValueError(f"Invalid generated WAV for {destination.name}")
            destination.write_bytes(clip.read_bytes())
            print(f"Generated {destination.name}")


if __name__ == "__main__":
    try:
        run()
    except (OSError, ValueError, KeyError, TypeError, RuntimeError, subprocess.SubprocessError, wave.Error) as error:
        print(f"Voice generation failed: {error}", file=sys.stderr)
        sys.exit(1)
