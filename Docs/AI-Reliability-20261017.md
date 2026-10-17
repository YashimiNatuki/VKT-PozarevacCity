# Local AI reliability and next world interactions

## Delivered

The existing Unity contacts panel now checks the companion identity before posting game facts. A four-second health request precedes the existing bounded conversation request. Closing, switching contact, application suspension and destruction cancel either stage. Gameplay goals, money and save data remain authoritative in Unity.

`GET /health` distinguishes a running VKT companion from usable generation: `status=ready` means the HTTP service is alive; `available`, `reason` and `retry_after` describe the model. Ollama model availability is sampled at most once per five seconds, serialized separately from generation, without downloading or loading a model. Warmup, busy generation, missing model, disconnected Ollama and an open circuit are distinct states.

The panel explains these states in Serbian. A failed preflight uses authored dialogue immediately after the bounded connection attempt. When the companion is reachable but its model is unavailable, the request still checks the persistent conversation cache. A cache hit remains usable without the model. Model failure never prevents playing the game. Retry is explicit; no automatic generation loop is added.

## Local setup

No new account or API key is required for the installed local path. Run Ollama on the PC, then use `Tools/LocalAI/Start-LocalAI.ps1` with the prepared runtime described in [Local AI setup](Local-AI-20261017.md). The selected default is `llama3.2:3b`; selecting another already installed model uses `VKT_OLLAMA_MODEL` when launching the companion. No models are downloaded automatically.

Run `python Tools/LocalAI/doctor.py` for read-only diagnostics. It uses only Python's standard library and prints fixed status fields, not keys, paths or arbitrary upstream text. Exit code zero means generation is currently available; one means read the status and continue with authored dialogue. Availability is a sampled capability check, not proof that a future generated sentence will be valid or natural Serbian.

## Android and BYOK boundary

Android continues to use authored contact dialogue in this release. A phone's `127.0.0.1` is the phone itself, not this PC. The companion remains bound to PC loopback. No firewall change, public Ollama port or embedded provider key is introduced.

The next remote-AI slice should add an authenticated HTTPS companion with explicit device pairing, revocation, request budgets and provider selection. BYOK belongs in a user-controlled server secret store, with a test-connection result and removable credential. Game builds must never include a shared API secret. An Android local-model experiment needs measured RAM, battery, thermal and generation latency on the actual phone before choosing a runtime. None of these remote/provider capabilities is claimed delivered here.

## Saved design idea: Optimistična klupa

The user's proposed character is an original humorous bench that says “Ja sam Optimistična klupa”, briefly unfolds or runs, and returns to its bench pose. This is a future game feature, not an implemented animation or generated asset.

Recommended contract: opt-in interaction; authored Serbian lines available immediately; optional validated AI flavor; `Idle -> Speaking -> Performing -> Returning -> Idle`; one performance at a time; cooldown and interruption handling. Preserve its authoritative interaction anchor and avoid moving solid geometry into the player. Keep the character distinct from licensed franchise characters. Ship a reviewed rig, animation clips, pooled effects and caption/audio fallback together, then test repeat interaction, save reload, collision and mobile budgets. A scripted performance should work without any model or internet.

## Tool routing and evidence limits

Unity Essentials is used for feature ownership, lifecycle cancellation and tests. The requested Game Development Studio route is blocked because `game-dev` is absent; it was not installed. HF CLI is absent, and no model was downloaded. Tripo/Topview asset production is a separate workflow requiring a concrete asset plan, provider availability, license review and any necessary credit approval. No generation job was submitted.

Super Map World targets game-map catalog assets rather than city street/elevation surveying. Map posters are visual references, not geospatial terrain inputs. Browser game stacks, quantum workspaces and web UI services are not dependencies of this Unity AI slice. No plugin is claimed connected merely because it was mentioned.

For the city, retain the current OSM provenance and [terrain pipeline](Map-Streets-20261017.md). A licensed height raster, correct coordinate/vertical reference and one shared height sampler must precede terrain, road, building, spawn and mission placement changes. This wave adds no satellite data, elevation or new neighborhoods.

Primary API references: [Ollama installed models](https://docs.ollama.com/api/tags), [Ollama chat and structured response format](https://docs.ollama.com/api/chat).
