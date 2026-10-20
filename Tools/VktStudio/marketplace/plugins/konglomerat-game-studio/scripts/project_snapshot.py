"""Read-only VKT orientation. Never executes project scripts or opens saves/secrets."""
import argparse
import hashlib
import json
from pathlib import Path
import subprocess

SOURCE_EXTENSIONS = {".cs", ".shader", ".xml", ".fbx", ".png", ".json", ".mat"}


def digest(path):
    result = hashlib.sha256()
    with path.open("rb") as stream:
        for chunk in iter(lambda: stream.read(1024 * 1024), b""):
            result.update(chunk)
    return result.hexdigest()


def project_root(value):
    path = Path(value)
    if not path.is_absolute():
        raise ValueError("Pass an explicit absolute project path")
    root = path.resolve(strict=True)
    if not (root / "Assets/CityGame").is_dir() or not (root / "ProjectSettings/ProjectVersion.txt").is_file():
        raise ValueError("Not an existing VKT Unity source project")
    return root


def source_fingerprint(root):
    entries = []
    for path in sorted((root / "Assets/CityGame").rglob("*")):
        if path.suffix.lower() not in SOURCE_EXTENSIONS or not path.is_file():
            continue
        if path.is_symlink() or not path.resolve().is_relative_to(root / "Assets/CityGame"):
            raise ValueError("Source entry resolves outside the CityGame boundary")
        entries.append(path.relative_to(root).as_posix() + ":" + digest(path))
    if not entries:
        raise ValueError("No CityGame source assets found")
    return hashlib.sha256("\n".join(entries).encode()).hexdigest(), len(entries)


def git(root, *arguments):
    result = subprocess.run(["git", "-C", str(root), *arguments], capture_output=True, text=True, timeout=20)
    if result.returncode:
        raise ValueError("Git metadata unavailable for requested project")
    return result.stdout.strip()


def snapshot(root):
    fingerprint, count = source_fingerprint(root)
    voices = root / "Assets/CityGame/Resources/Audio/Voices"
    voice_count = sum(1 for path in voices.glob("*") if path.suffix.lower() in {".wav", ".ogg", ".mp3"} and path.is_file())
    return {
        "schema": "vkt.project-snapshot.v1", "project": str(root),
        "revision": git(root, "rev-parse", "HEAD"),
        "branch": git(root, "branch", "--show-current"),
        "workingChanges": git(root, "status", "--porcelain", "--untracked-files=normal").splitlines(),
        "sourceFingerprint": fingerprint, "sourceFiles": count,
        "voiceClipFiles": voice_count,
        "engine": (root / "ProjectSettings/ProjectVersion.txt").read_text(encoding="utf-8-sig").splitlines()[0],
        "evidenceLimit": "Static source inventory only; no gameplay, FPS, voice listening, build or security certification."
    }


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--project", required=True)
    args = parser.parse_args()
    try:
        print(json.dumps(snapshot(project_root(args.project)), indent=2, ensure_ascii=True))
    except (OSError, ValueError, subprocess.SubprocessError) as error:
        parser.exit(2, "VKT snapshot refused: " + str(error) + "\n")


if __name__ == "__main__":
    main()
