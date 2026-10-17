# Installed free VKT development toolchain

The user explicitly authorized installing the missing local CLI and necessary supporting tools on 2026-10-17. The earlier AI release accurately recorded `game-dev` as absent at that time. It is now installed and verified.

## Installed components

| Tool | Version | Purpose |
| --- | --- | --- |
| Game Development Studio CLI | 1.5.1 | Asset inspection/packaging, capture verification and comparison |
| Dedicated Node.js LTS | 24.22.0 | CLI runtime with built-in SQLite; existing system Node 20 retained |
| Blender LTS | 4.5.9 | Local mesh/rig import and conversion |
| FFmpeg / FFprobe | 9.0.2 essentials | Audio/video processing and metadata checks |
| Hugging Face Hub / CLI | 2.2.0 | Public model discovery and controlled future model downloads |
| trimesh | 5.1.1 | GLB geometry processing |
| Rasterio | 1.5.2 | GeoTIFF and raster processing, bundled GDAL runtime |
| pyproj | 3.8.0 | Coordinate-reference transformations |
| Shapely | 2.2.0 | Road corridors and polygon geometry |
| NumPy / Pillow | 2.5.4 / 13.0.0 | Height arrays, image and texture processing |

Five packaged Game Development Studio skills were installed into the user's local Codex skills directory. CLI execution works immediately through the shell. A client may need a new session to discover new skill entries. No MCP server is claimed registered or hot-loaded by this installation.

## Location and shell use

Tools live outside both Unity checkouts at `%USERPROFILE%\Documents\VKT-Toolchain`. Only its `bin` directory was appended to the user PATH; existing PATH entries and the system Node installation were preserved. The previous user PATH is backed up locally in `user-path-before.json`, not committed. Open a fresh external PowerShell window to inherit the updated PATH. An already running application may retain its old environment; the explicit launcher works without restarting it:

```powershell
& "$env:USERPROFILE\Documents\VKT-Toolchain\bin\game-dev.cmd" --version
game-dev.cmd doctor --workflow blender-normalize --output-dir "$env:USERPROFILE\Documents\VKT-Toolchain\workspace" --json
hf.cmd version
ffmpeg.cmd -version
ffprobe.cmd -version
blender.cmd --background --factory-startup --version
vkt-python.cmd Tools\Toolchain\verify.py
```

Blender is explicitly selected in Game Development Studio's per-user tool configuration, with its executable SHA-256 pinned. That configuration changes tool discovery, not a Unity scene or imported model. No global Python package installation, OS startup service, WSL distribution change, firewall rule or system Node upgrade was needed.

## Verified behavior

- SHA-256 verified the Node ZIP, Blender ZIP and official `game-dev` release tarball. The CLI tarball and FFmpeg ZIP also matched GitHub release asset digest and size metadata.
- `game-dev` doctors passed for generic capture, asset inspection, asset packaging and Blender normalization. Optional paid providers, Basis Universal, CoACD and macOS-only Metal tooling are not requirements of these selected workflows.
- Two synthetic sample capture bundles were sealed and verified. Their decoded comparison was identical. This proves the installed capture pipeline, not VKT FPS or visual quality.
- Blender actually imported the existing CC0 Kenney `characterMedium.fbx`: one mesh, one armature, 1,604 triangles. It exported a separate 95,616-byte GLB which `game-dev asset inspect` successfully read. The original source hash remained unchanged. The exported diagnostic copy was not admitted into Unity.
- FFmpeg generated a one-second 48 kHz mono WAV; FFprobe confirmed the resulting duration, rate and channel count.
- The isolated Python environment passed `pip check`, coordinate round-trip, synthetic GeoTIFF read/write, polygon corridor and GLB round-trip checks. No satellite image, real elevation or new geographic coverage was imported.
- The native command launchers were invoked from PowerShell after installation. Exact versions and reviewed results are in [the verification receipt](Verification-Toolchain-20261017.json).

The first npm attempt encountered a certificate-chain error. The dedicated Node runtime was configured to use Windows system CAs for installation. Blender's initial side-by-side error came from the desktop app's virtual AppData path; relocating the owned tool directory to Documents resolved native startup. TLS verification stayed enabled. Blender also emits a TBB allocator notice on this machine; the import/export process nevertheless exited successfully. This is not a performance benchmark.

## Next VKT use

Use the tools for reviewed model/LOD preparation and asset checks, then admit only verified copies into `Assets/CityGame`. The GIS stack is ready for the [reviewed terrain pipeline](Map-Streets-20261017.md): a licensed real raster, vertical/coordinate reference, and shared height sampler must precede road/building/spawn placement changes. Installing these libraries does not itself improve the rendered game.

Local Ollama and the existing AI companion remain separate from this development environment. No provider credential or sign-in is required for the verified local workflows. Tripo/Leonardo generation was not invoked, and no credits, paid job or model-weight download was started.

## Sources and reinstallation

Use the publisher's [CLI install guide](https://github.com/theisegoria/game-development-studio/blob/main/docs/install.md), [Blender release archive](https://download.blender.org/release/Blender4.5/), [Node.js releases](https://nodejs.org/dist/), [FFmpeg Windows build references](https://ffmpeg.org/download.html#build-windows) and [Hugging Face CLI guide](https://huggingface.co/docs/huggingface_hub/guides/cli). The CLI's public npm registry name is unpublished; install its checksum-verified GitHub release tarball. Keep tools, downloads, sample captures and model caches outside the Git repository and Windows game ZIP.
