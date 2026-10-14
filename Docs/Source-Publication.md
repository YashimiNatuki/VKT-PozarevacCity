# Public source publication

The development project contains vendor packages, streamed-map credentials in scene serialization, and audio with unconfirmed redistribution rights. Publish a separate allowlisted source export. Keep the original project intact.

`Tools/Export-Source.ps1` defaults to a dry run. It copies nothing unless `-Execute` is explicitly supplied. Execution requires a new destination outside the original project, rejects obvious credential signatures without printing values, and performs no Git operations.

Included: CityGame C# and their metadata, mission marker material, reviewed documentation/tool text, root README/license/notices/gitignore when present, Editor version, and a curated dependency manifest using exact source versions plus the Cesium scoped registry. Unity built-in module dependencies are retained.

Excluded: every original scene, all audio/video, ArcGIS embedded package, Starter Assets, other vendor content, PlayerSettings/UserSettings, Library/Temp/Logs, service settings, save files, and local credentials. The original map scene is deliberately not published. A first-party Editor scene generator must create the reproducible offline fixture after clone; that fixture does not claim photorealistic Požarevac coverage. Test that generated project independently before publishing.

The signature scan is a fail-fast guard, not proof that every possible secret format is absent. Review the staged diff and repository file list before push. Do not add the original project with `git add .`.

## License boundary

No explicit copyright/license header was found in CityGame C# during inspection. A first-party MIT license may cover code the owner has rights to license; it must not imply relicensing Esri, Unity, Cesium service data, third-party recordings, model weights, or generated content with restricted provenance. Keep third-party notices and package licenses in their own scope. The exporter excludes the two existing WAV files because their source rights have not been established.

## GitHub destination and access

**Current publication decision:** the coordinator reports creation of the new repository `YashimiNatuki/VKT-PozarevacCity`. Publish this clean standalone source there after actual access/build checks. Preserve the old browser repository. The candidate audit below explains why replacing its root would have been incorrect; it is not the chosen new destination.

Public metadata identifies [YashimiNatuki/3D-gta-Pozarevac](https://github.com/YashimiNatuki/3D-gta-Pozarevac), branch `main`, description `3d gangser huslin`, as a plausible existing destination. At inspection it had no API-reported license and was approximately 214,935 KiB. Its public root is an existing browser/Three.js game (`src`, `public`, `server.ts`, `package.json`), and its README explicitly states that Unity is a separate local runtime. Preserve that project and its root README. If this repository is selected, publish the export under `unity/VKT-PozarevacCity/` on a feature branch, and add a short root README link/status section. Do not replace the browser project or claim that its runtime is integrated with Unity.

Neither local working directory initially contained Git metadata. Git Credential Manager listed account `AIzooLab`; its write rights to the above repository have not been confirmed. No token or credential file was read. Do not treat the account name as authorization to modify a different owner's repository.

Before push: confirm exact repository, preserve its existing files/history, create a feature branch, export reviewed source, generate/build/test the offline fixture, inspect the staged diff and secret scan, commit, then push normally. Do not force-push. Final handoff must identify the commit and remote branch or state the exact access blocker.
