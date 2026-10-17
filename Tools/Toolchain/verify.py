"""Verify the user-installed Windows VKT toolchain without changing game assets."""
import argparse
import json
from pathlib import Path
import subprocess


def run(root):
    root = root.resolve(strict=True)
    state = json.loads((root/"installation.json").read_text(encoding="utf-8"))
    for key in ("nodePath", "cliEntry"):
        if not Path(state[key]).resolve(strict=True).is_relative_to(root):
            raise ValueError("Tool executable is outside the selected installation.")
    cli = [state["nodePath"], state["cliEntry"]]

    def invoke(arguments, timeout=45):
        result = subprocess.run(arguments, capture_output=True, text=True, encoding="utf-8",
                                timeout=timeout, creationflags=getattr(subprocess, "CREATE_NO_WINDOW", 0))
        if result.returncode:
            raise RuntimeError("Tool check failed: " + Path(arguments[0]).name)
        return result.stdout

    result = {"gameDevVersion": invoke(cli+["--version"]).strip(), "workflows": {}}
    for workflow in ("generic-capture", "asset-inspect", "asset-package", "blender-normalize"):
        value = json.loads(invoke(cli+["doctor", "--workflow", workflow, "--expected-version", "1.5.1",
                                      "--output-dir", str(root/"workspace"), "--json"]))
        if not value.get("ok") or not value["data"]["healthy"]:
            raise RuntimeError("Unhealthy tool workflow: " + workflow)
        result["workflows"][workflow] = True
    python = root/"python/Scripts/python.exe"
    result["pythonDependencies"] = invoke([str(python), "-m", "pip", "check"]).strip()
    result["geoAndMeshSmoke"] = json.loads(invoke([str(python), "-X", "utf8",
                                                 str(Path(__file__).with_name("smoke_geo.py"))]))
    result["gameAssetsModified"] = False
    result["paidProviderInvoked"] = False
    return result


if __name__ == "__main__":
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--root", type=Path, default=Path.home()/"Documents/VKT-Toolchain")
    options = parser.parse_args()
    print(json.dumps(run(options.root), indent=2))
