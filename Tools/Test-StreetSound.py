"""Run the isolated native Unity flow; a build alone is not gameplay evidence."""
import argparse
import json
import pathlib
import re
import subprocess
import sys


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--player", required=True, type=pathlib.Path)
    parser.add_argument("--output", required=True, type=pathlib.Path)
    parser.add_argument("--touch-preview", action="store_true")
    parser.add_argument("--timeout", type=int, default=180)
    args = parser.parse_args()
    if not args.player.is_file():
        parser.error("The Windows development player does not exist.")
    if not 30 <= args.timeout <= 600:
        parser.error("Timeout must be between 30 and 600 seconds.")
    output = args.output.resolve()
    output.mkdir(parents=True, exist_ok=True)
    report_path, log_path = output / "flow.json", output / "player.log"
    if report_path.exists() or log_path.exists():
        parser.error("Use a new output folder to prevent stale evidence or overwrites.")
    command = [str(args.player.resolve()), "-force-d3d11", "-screen-width", "1280",
               "-screen-height", "720", "-screen-fullscreen", "0", "-job-worker-count", "4",
               "-vktSmoke", "-vktInputFlow", "-vktOnboardingFlow", "-vktFlowReport",
               str(report_path), "-vktFlowCaptures", str(output / "captures"),
               "-logFile", str(log_path)]
    if args.touch_preview:
        command.append("-vktTouchPreview")
    try:
        result = subprocess.run(command, timeout=args.timeout, check=False)
        report = json.loads(report_path.read_text(encoding="utf-8"))
        log = log_path.read_text(encoding="utf-8", errors="replace")
        required = ["passed", "onboardingCreated", "phoneBeforeContract", "mapBeforeContract",
                    "musicNonModal", "movementAfterMusic", "benchCompleted", "legsAnimated",
                    "musicControlsClear", "helpReturnsControl", "dockVisible", "benchStarted",
                    "signalBlockedByMenu", "signalBlockedByWall", "signalReachesOfficer",
                    "signalCooldown", "signalDoesNotPauseOrAddHeat", "hideoutScoreFades",
                    "hideoutScoreMutes", "helpAudioTextFits", "hideoutMusicPreferenceMutes",
                    "disabledSignalBlocked", "mapPreservesAudio", "muteShortcutWorks"]
        failed = [name for name in required if report.get(name) is not True]
        errors = re.findall(r"NullReferenceException|MissingReferenceException|IndexOutOfRangeException|Unhandled Exception|Shader error", log)
        passed = result.returncode == 0 and not failed and not errors
        print(json.dumps({"passed": passed, "playerExit": result.returncode,
                          "failedChecks": failed, "runtimeErrors": errors,
                          "report": str(report_path),
                          "input": "Synthetic Unity input; no physical Android or human audio test."}, indent=2))
        return 0 if passed else 1
    except (OSError, ValueError, subprocess.TimeoutExpired) as error:
        print(json.dumps({"passed": False, "error": str(error)}, indent=2), file=sys.stderr)
        return 1


if __name__ == "__main__":
    raise SystemExit(main())
