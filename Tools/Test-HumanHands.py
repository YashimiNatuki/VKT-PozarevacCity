"""Validate human finger deformation, real unarmed attacks, pause and recovery."""
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
    parser.add_argument('--touch-preview', action='store_true')
    args = parser.parse_args()
    if not args.player.is_file():
        parser.error('Windows development player missing')
    output = args.output.resolve()
    if output.exists():
        parser.error('Use a new output folder')
    captures = output / 'captures'
    captures.mkdir(parents=True)
    command = [str(args.player.resolve()), '-force-d3d11', '-screen-width', '1280', '-screen-height', '720',
               '-screen-fullscreen', '0', '-job-worker-count', '2', '-vktSmoke', '-vktHumanHandsSmoke',
               '-vktHumanHandsCaptures', str(captures), '-vktReport', str(output / 'report.json'),
               '-logFile', str(output / 'player.log')]
    if args.touch_preview:
        command.append('-vktTouchPreview')
    try:
        run = subprocess.run(command, timeout=180, check=False)
        report = json.loads((output / 'report.json').read_text(encoding='utf-8'))
        metrics = json.loads((captures / 'human-hands.json').read_text(encoding='utf-8'))
        log = (output / 'player.log').read_text(encoding='utf-8', errors='replace')
        required = ['passed','previewFists','previewDeformation','worldAttack','cooldown','pauseStable',
                    'pausedAttackBlocked','recovery','profileUnchanged','ownersUnchanged','controlRestored']
        failed = [name for name in required if metrics.get(name) is not True]
        errors = re.findall(r'NullReferenceException|MissingReferenceException|IndexOutOfRangeException|Unhandled Exception|Shader error', log)
        pictures = ['hands-guard-front.png','hands-left.png','hands-right.png','hands-world-attack.png']
        captures_ok = all((captures / name).is_file() and (captures / name).stat().st_size > 50000 for name in pictures)
        passed = run.returncode == 0 and report.get('passed') is True and not failed and not errors and captures_ok and 'VKT_HUMAN_HANDS_VERIFIED ' in log
        result = dict(passed=passed, processExit=run.returncode, failedChecks=failed, runtimeErrors=errors,
                      captures=captures_ok, metrics=metrics, report=report,
                      boundary='Actual Windows combat endpoint and baked mesh checks; synthetic input, no physical phone, new bench grip or human aesthetic approval.')
        (output / 'verification.json').write_text(json.dumps(result, indent=2), encoding='utf-8')
        print(json.dumps(result, indent=2))
        return 0 if passed else 1
    except (OSError, ValueError, subprocess.TimeoutExpired) as error:
        print(json.dumps(dict(passed=False, error=str(error))), file=sys.stderr)
        return 1


if __name__ == '__main__':
    raise SystemExit(main())
