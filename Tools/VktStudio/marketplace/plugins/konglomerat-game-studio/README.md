# Konglomerat / Rodja Game Studio 0.2.1

One MIT-licensed local package, three connected skills, no paid runtime or server dependency.

| Skill | Use |
| --- | --- |
| `rodja-game-engineer` | Implement missions, training, police and player systems in the existing Unity project. |
| `rodja-worldcraft` | Review character geometry, rigs, city geography, terrain and audio provenance. |
| `rodja-release-ops` | Verify actual tests, Windows runtime evidence, source scope and package hashes. |

Start with `Use $rodja-game-engineer to improve one connected gameplay slice and verify it in the Windows player.` The game remains VKT / Pozarevac City; the toolkit brand is Konglomerat / Rodja. Existing unrelated Rodja plugins are separate and must not be overwritten.

The `scripts/` tools use Python's standard library and are read-only. Run `python scripts/project_snapshot.py --project ABSOLUTE_UNITY_PROJECT` for orientation. Run `python scripts/verify_release.py --help` for evidence checking. The legacy selected-source fingerprint is explicitly incomplete; it cannot approve a production release.

For Codex local installation, register the containing marketplace with `codex plugin marketplace add ABSOLUTE_MARKETPLACE_PATH`, then `codex plugin add konglomerat-game-studio@konglomerat-local`. The source marketplace is supplied separately in the repository. This archive is the portable plugin directory; it does not include a marketplace registration. Reload the host if new skills are not yet visible.

This is a rename/evolution of the previous game-specific toolkit. Existing plugin caches are not runtime game code. Source, prior releases and test history remain recoverable through Git; avoid running both old and renamed instructions as if they were two independent implementations.
