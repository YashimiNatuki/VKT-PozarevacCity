"""Inspect local VKT prerequisites. Exit 0 means audit completed, never that an iOS build exists."""
import argparse
import json
import pathlib
import platform
import shutil


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('--project', type=pathlib.Path, default=pathlib.Path(__file__).resolve().parents[1])
    parser.add_argument('--editor', type=pathlib.Path, required=True)
    parser.add_argument('--output', type=pathlib.Path, required=True)
    args = parser.parse_args()
    if not (args.project / 'ProjectSettings/ProjectVersion.txt').is_file() or not args.editor.is_file():
        parser.error('Unity project or editor missing')
    if args.output.exists():
        parser.error('Use a new report path')
    engines = args.editor.parent / 'Data/PlaybackEngines'
    module = (engines / 'iOSSupport').is_dir()
    cesium = list((args.project / 'Library/PackageCache').glob('com.cesium.unity@*/Plugins/iOS/libCesiumForUnityNative.a'))
    mac = platform.system() == 'Darwin'
    xcode = shutil.which('xcodebuild') is not None
    report = dict(auditCompleted=True, validationExit=0, target='iPhone 13 and newer',
                  host=platform.system(), iosBuildSupportInstalled=module,
                  cesiumIosNativeLibraryPresent=bool(cesium), macHost=mac, xcodeCommandAvailable=xcode,
                  localBuildPrerequisitesPresent=module and mac and xcode,
                  signingConfigured='not inspected', iosBuildVerified=False, physicalIPhoneTested=False,
                  remaining=['Install matching iOS Build Support', 'Use macOS/Xcode or a configured cloud Mac',
                             'Configure signing on the build host', 'Export, compile and test on iPhone 13+'])
    args.output.parent.mkdir(parents=True, exist_ok=True)
    args.output.write_text(json.dumps(report, indent=2) + '\n', encoding='utf-8')
    print(json.dumps(report, indent=2))
    return 0


if __name__ == '__main__':
    raise SystemExit(main())
