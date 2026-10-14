# Source milestone — 2026-10-14

The implementation team reports **66/66 Unity tests passed** in the development project. This is source-level milestone evidence; fresh Windows and Android portable builds and exported-project verification remain pending at the time this document was written.

## Added in this milestone

- Eight fictional ornamental plant definitions, five initially unlocked and three reputation unlocks.
- Paid seed inventory, planting consumption, harvested inventory and variable buyer/district counter-order pricing. Physical delivery to customers is not implemented.
- Health, damage cooldown, session death/respawn and mission retry integration; not finished animated encounter AI.
- Configurable 18-minute day / 12-minute night, separate from real-time plant care.
- Desktop and mobile touch control paths; phone usability/performance pending.
- Synthetic ambience and offline voice-baking tooling. Real natural Serbian voices are absent.
- Portable Editor generator for a clean offline practice scene and Windows/Android development-build entry points.
- Allowlisted public-source export with credentials/scenes/vendor media excluded and first-party MIT license boundary.

## Delivery boundary

The intended new repository is `YashimiNatuki/VKT-PozarevacCity`; creation was reported by the coordinator, but this document does not claim a successful source push. Preserve the separate browser repository `YashimiNatuki/3D-gta-Pozarevac` unchanged.

The public source scene is generated from C# and uses a practice floor. It is not the original streamed city, not a complete modeled Požarevac and not evidence of geographic accuracy. The full local project remains the working integration source.

## Next acceptance gates

1. Export current source and restore dependencies in the clean project.
2. Generate the practice scene; compile and run all source tests there.
3. Produce a new Windows player and exercise the economy/health/save loop.
4. Build APK; install and verify the loop, touch input, resume/save and performance on a real Android device.
5. Add physical delivery encounters, finished art and reviewed natural Serbian actor recordings.

Update build/push status only from actual logs, artifacts and remote commit evidence.
