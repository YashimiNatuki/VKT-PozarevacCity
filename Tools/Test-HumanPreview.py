"""Validate the opt-in human art preview in a Windows player, without touching personal saves."""
import argparse
import json
import pathlib
import re
import subprocess
import sys


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('--player', type=pathlib.Path, required=True)
    parser.add_argument('--output', type=pathlib.Path, required=True)
    parser.add_argument('--motion', action='store_true', help='Verify and capture walking, running and guard')
    args = parser.parse_args()
    if not args.player.is_file():
        parser.error('Windows player missing')
    output = args.output.resolve()
    if output.exists():
        parser.error('Use a new evidence folder')
    output.mkdir(parents=True)
    log, report = output / 'player.log', output / 'report.json'
    command = [str(args.player.resolve()), '-force-d3d11', '-screen-width', '1280',
               '-screen-height', '720', '-screen-fullscreen', '0', '-job-worker-count', '4',
               '-vktSmoke', '-vktHumanCandidateSmoke', '-vktLifeSmoke', '-vktCharacterSmoke',
               '-vktHumanCaptures', str(output / 'captures'), '-vktReport', str(report),
               '-logFile', str(log)]
    if args.motion: command.append('-vktHumanMotionSmoke')
    try:
        run = subprocess.run(command, timeout=180, check=False)
        data = json.loads(report.read_text(encoding='utf-8'))
        text = log.read_text(encoding='utf-8', errors='replace')
        errors = re.findall(r'NullReferenceException|MissingReferenceException|IndexOutOfRangeException|Unhandled Exception|Shader error', text)
        markers = 'VKT_HUMAN_PREVIEW_VERIFIED renderedAngles=4 reuse=true profileUnchanged=true controlRestored=true' in text
        captures = all((output / 'captures' / (angle + '.png')).is_file() and (output / 'captures' / (angle + '.png')).stat().st_size > 50000 for angle in ['front', 'side', 'back', 'face'])
        if args.motion:
            markers = markers and 'VKT_HUMAN_MOTION_VERIFIED modes=4 unscaledPreview=true worldPaused=true' in text
            captures = captures and all((output / 'captures' / ('motion-' + str(i) + '.png')).is_file() and (output / 'captures' / ('motion-' + str(i) + '.png')).stat().st_size > 50000 for i in [1, 2, 3])
        passed = run.returncode == 0 and data.get('passed') is True and markers and captures and not errors
        result = dict(passed=passed, processExit=run.returncode, previewChecks=markers,
                      captures=captures, runtimeErrors=errors, report=data,
                      boundary='Windows preview only; not a playable replacement rig or physical mobile test.')
        (output / 'verification.json').write_text(json.dumps(result, indent=2), encoding='utf-8')
        print(json.dumps(result, indent=2))
        return 0 if passed else 1
    except (OSError, ValueError, subprocess.TimeoutExpired) as error:
        print(json.dumps(dict(passed=False, error=str(error))), file=sys.stderr)
        return 1


if __name__ == '__main__':
    raise SystemExit(main())
