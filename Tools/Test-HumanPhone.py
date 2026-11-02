"""Validate the real phone UI, held human phone, tap/contact and camera restoration."""
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
    parser.add_argument('--portrait', action='store_true')
    args = parser.parse_args()
    if not args.player.is_file():
        parser.error('Windows development player missing')
    output = args.output.resolve()
    if output.exists():
        parser.error('Use a new output folder')
    captures = output / 'captures'
    captures.mkdir(parents=True)
    width, height = (720, 1280) if args.portrait else (1280, 720)
    command = [str(args.player.resolve()), '-force-d3d11', '-screen-width', str(width), '-screen-height', str(height),
               '-screen-fullscreen', '0', '-job-worker-count', '2', '-vktSmoke', '-vktHumanPhoneSmoke',
               '-vktHumanPhoneCaptures', str(captures), '-vktReport', str(output / 'report.json'),
               '-logFile', str(output / 'player.log')]
    if args.touch_preview:
        command.append('-vktTouchPreview')
    process_exit = None
    try:
        run = subprocess.run(command, timeout=210, check=False)
        process_exit = run.returncode
        report = json.loads((output / 'report.json').read_text(encoding='utf-8'))
        metrics = json.loads((captures / 'human-phone.json').read_text(encoding='utf-8'))
        log = (output / 'player.log').read_text(encoding='utf-8', errors='replace')
        required = ['passed', 'ownersUnchanged', 'controlRestored', 'profileUnchanged', 'uiFits']
        pictures = ['phone-portrait.png'] if args.portrait else ['phone-mode-0.png', 'phone-mode-1.png', 'phone-mode-2.png', 'phone-tap.png', 'regional-relief.png']
        required += ['narrowFallback'] if args.portrait else ['raised', 'pausedWorld', 'buttonTap', 'grip', 'cameraModes', 'closeRestores', 'reuse', 'noPhysics', 'occlusionFallback', 'regionalRelief']
        failed = [name for name in required if metrics.get(name) is not True]
        errors = re.findall(r'NullReferenceException|MissingReferenceException|IndexOutOfRangeException|Unhandled Exception|Shader error', log)
        captures_ok = all((captures / name).is_file() and (captures / name).stat().st_size > 50000 for name in pictures)
        passed = process_exit == 0 and report.get('passed') is True and not failed and not errors and captures_ok and 'VKT_HUMAN_PHONE_VERIFIED ' in log
        result = dict(passed=passed, processExit=process_exit, failedChecks=failed, runtimeErrors=errors,
                      captures=captures_ok, metrics=metrics, report=report,
                      boundary='Actual Windows uGUI and normalized human in a controlled physical fixture; finger endpoint proxies, no physical Android/iPhone or human aesthetic approval.')
    except (OSError, ValueError, subprocess.TimeoutExpired) as error:
        result = dict(passed=False, processExit=process_exit, error=str(error))
    (output / 'verification.json').write_text(json.dumps(result, indent=2), encoding='utf-8')
    print(json.dumps(result, indent=2))
    return 0 if result['passed'] else 1


if __name__ == '__main__':
    raise SystemExit(main())
