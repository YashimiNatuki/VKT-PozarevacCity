# Konglomerat / Rodja Local Kit

This is the complete free local kit: one plugin, three connected skills, read-only Python evidence tools and shared gameplay/quality protocols. Brand: Konglomerat / Rodja. The game it currently supports remains VKT / Pozarevac City.

Extract this whole directory to a location you intend to keep. In a terminal with Codex CLI installed:

```text
codex plugin marketplace add ABSOLUTE_PATH_TO_THIS_DIRECTORY
codex plugin add konglomerat-game-studio@konglomerat-local
```

Reload the Codex host if needed. Try `Use $rodja-game-engineer to continue the Unity game from its verified baseline.` The actual installed CLI is the authority for installation commands; `codex plugin --help` lists supported operations.

The plugin needs no paid API key, remote server, account scraping or background telemetry. Python helpers need an available Python 3.10+ and Git for source snapshots. These tools verify supplied local evidence; they do not produce a game build or certify complete production readiness. See the plugin README and `references/QUALITY-GATES.md` for exact limits.

Migration: the source replaces `yashimi-vkt-studio@yashimi-vkt-local`. The previous source/release remains in Git history. Only uninstall that exact obsolete plugin if installed; keep the independently maintained `yashimi-rodja-studio@yashimi-rodja-local` separate.
