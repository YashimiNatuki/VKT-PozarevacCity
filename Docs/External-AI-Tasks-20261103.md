# Zadaci za Gemini i GitHub Copilot — 3. novembar 2026.

Ovo su spremni promptovi za odvojene predloge i nezavisan pregled. Nisu poslati Gemini-ju, Copilot-u ili drugom eksternom servisu. Njihov odgovor nije potvrđena implementacija dok se ne proveri u kanonskom Unity projektu.

## Provereni polazni okvir

- Unity 6000.6.5f1, URP, namespace `Pozarevac.CityGame`.
- `CityPlayerController` upravlja pešakom; u aktivnom Unity izvoru nema potvrđenog ulaska/izlaska iz automobila ili voznog kontrolera.
- `CityMissionDirector.CreateContracts` definiše tri glavna ugovora. `CityStreetJobs` zatim generiše ponude kroz iste tri stanice.
- `CityLocalStoryPanel` ima Mila/Vuk/Luka kontakte, autorske replike i opcioni lokalni Ollama servis. To je panel kontakata, ne potvrđen razgovor sa hodajućim NPC-jem na ulici.
- `CitySession`, `CityMissionRun`, `CityStreetJobs`, `CityGardenOrder`, `CityProgression` i `CitySaveStore` ostaju vlasnici stanja. Profil je v9.
- Gemini ZIP od 3. novembra je React/Three.js rewrite. Njegov renderer, App, storage i ekonomija ne prepisuju Unity. [Gemini audit](Gemini-Intake-Review-20261103.md).

## Gemini: projektovanje deset početnih misija

Kopirati u odvojeni razgovor/proposal. Ako nema potrebnih izvora, tražiti samo relevantne fajlove.

```text
Act as a senior Unity gameplay designer and reviewer for VKT / Velika Kradja Tockova, set in Pozarevac, Serbia. Produce a REVIEW PROPOSAL ONLY. Do not edit the open AI Studio app, synchronize GitHub, publish, install dependencies, run generators or replace the Unity project.

Canonical architecture:
- Unity 6000.6.5f1 / URP / C# / Pozarevac.CityGame.
- CityPlayerController owns pedestrian movement.
- CityMissionDirector + CityMissionRun own three existing campaign contracts.
- CityStreetJobs owns deterministic repeatable jobs through three authored local stations.
- CityGardenOrder owns the hideout delivery/receipt/payment flow.
- CitySession and CityProgression own money, reputation and profile v9.
- CitySaveStore owns validated atomic persistence and stale-writer protection.
- CityLocalStoryPanel provides authored Mila/Vuk/Luka replies and optional local Ollama flavor text. AI cannot grant rewards, change objectives, modify saves or decide mission completion.
- The Gemini React/Three.js output is a separate prototype, not source-compatible Unity code.

Design a coherent first-ten-mission sequence. Inspect the supplied real C# files and Gemini intake review first. Request missing files instead of inventing APIs. Keep initial missions pedestrian-playable. Mark car driving, physical street NPC conversations and unbuilt neighborhoods as prerequisites, not shipped features.

For every mission give:
1. Stable ID, Serbian title, contact, actual playable location/station and prerequisite.
2. Trigger, objective states, legal transitions and clear Serbian player guidance.
3. Required actions, cargo/receipt ownership and exact completion predicate.
4. Pause, suspend, death, arrest, timeout, cancellation, retry and save/resume behavior.
5. Single-owner reward transaction and duplicate-event/reload/reentrancy protection.
6. Existing C# reuse points, missing systems, minimal patch and acceptance tests.
7. Asset/source/license provenance and unverified assumptions.

Keep the fictional crime tone urban and humorous. Any adult relationships must remain consensual and non-explicit; refusal or coercion must not be a reward condition. Do not provide real-world cultivation or crime instructions.

Return a source-backed current-feature/proposal matrix, ten-mission dependency/pacing table, one exact small unified diff against supplied files only, failing-before/passing-after tests, required Unity/native checks, provenance and five later expansions.

Never claim compilation, passing tests, existing assets or integration unless you ran the named checks. Do not generate a replacement application.
```

## GitHub Copilot: ulazak u vozilo i bezbedan izlazak

Raditi na posebnoj grani/worktree-u tek po proveri da je kopija izolovana. Ne pokretati paralelan Unity Editor nad istim projektom.

```text
Work as a Unity gameplay engineer on an ISOLATED proposal branch/worktree for YashimiNatuki/VKT-PozarevacCity. Do not touch the active checkout or unrelated work. First report branch, HEAD, dirty state, applicable AGENTS.md and source ownership. Do not commit, merge, push, publish or synchronize automatically.

Read these files under Assets/CityGame/Runtime:
CityPlayerController.cs, CityPlayerInteraction.cs, CityInteractable.cs,
CityCameraRig.cs, CityHumanActor.cs, CityActorMotion.cs,
CityMissionDirector.cs, CityStreetJobs.cs, CityGardenOrder.cs,
CityProgression.cs, CityPlayerStats.cs, CitySaveStore.cs,
CityPlayerVitals.cs, CityLifestyleRuntime.cs, CityHideout.cs.
Read relevant Editor tests and Docs/Gemini-Intake-Review-20261103.md.

Task: propose and implement the smallest reversible parked-vehicle enter/exit slice using an existing suitable licensed asset if actually present. Do not assume a drivable vehicle system or asset exists. If absent, report the blocker and use a clearly labeled disposable test fixture, not a production-art claim.

Required invariants:
- Explicit occupancy and exactly one movement owner: pedestrian, AI or player vehicle.
- Reuse E/touch interaction, with no competing global input handler.
- Enter only within range/line of sight, alive, unpaused, outside the hideout and incompatible training/carry states.
- Exit requires capsule/ground/obstacle clearance. A blocked exit preserves occupancy and explains the next action in Serbian.
- Pause/suspend freeze movement and simulation clocks.
- Death/arrest/disable/destruction restore a safe controllable state once.
- One physical collision owner; no duplicate capsules or hidden movement.
- Restore camera/avatar visibility in first/chase/shoulder modes.
- Record vehicle distance once as ridden, never also walked.
- Keep one wallet, mission state, heat simulation and profile v9. Session-only selection is acceptable initially.
- Do not import Gemini App/storage/traffic loops. The inspected prototype moved the driven car twice; test against this ownership bug.
- Add no package without a demonstrated compatibility need.

Use test-first development with real state/physics fixtures. Cover blocked exit, repeated entry/exit, pause, death, owner switching, stale callbacks and distance accounting. Run focused tests then the project gate in the isolated checkout. No Unity Editor may use the active project directory.

Return exact changed files/unified diff, actual command exits/test counts, source/asset provenance, rollback steps and unverified physical-device/art/performance boundaries. Do not claim full driving or traffic if you delivered occupancy only.
```

## Copilot ili Gemini: ulični NPC i razgovori

```text
Prepare an isolated Unity proposal for visible street contacts using existing VKT owners. Inspect CityLocalStoryPanel, CityContactStory, CityMissionDirector, CityPlayerInteraction, CityInteractable, CityGroundProbe, CityPoliceInvestigation and current tests. Request missing source files.

Build one contact before a city-wide crowd system. Reuse existing contacts and authored offline replies. Route world interaction through the existing line-of-sight/range boundary. Keep authored objective facts independent of optional AI flavor text.

Specify approach, wait, talk, leave and interrupted states. Do not teleport NPCs to cafe/bench targets. Respect actual collision/ground/navigation data and pause/suspend. Handle death, wanted state, training, active delivery and UI close/resume without stuck input locks.

Only existing mission owners may validate objectives or pay. Dialogue must never execute model-generated action names, code, tools or wallet edits. External text is bounded plain text. Conversation stays usable offline and explains AI unavailability.

Return a source-backed design, one small exact unified diff, independent behavior tests for pause/close/interruption/duplicate completion, native checks, asset license provenance and unknowns. Do not rewrite the app, install dependencies, synchronize, publish, merge or push.
```

## Štek i bašta: pregled veze sa kampanjom

```text
Review Unity CityGardenOrder / CityHideout / CityProgression / CityStreetJobs / CityMissionRun. Use supplied source only; design documents are not proof of shipped behavior.

Produce a state/ownership table for unlock, buy fictional seed, plant, care, harvest, reserve cargo, deliver to contact, return receipt and claim payment. This is a fictional game economy: keep timing/yields abstract.

Find duplicate claims, lost cargo, stuck UI, save/resume inconsistencies, death/arrest/cancel cases and concurrent acceptance. Every fix must name the owner, precondition, mutation and independent test. Keep profile v9, atomic saves and one authoritative economy. Recommend one minimal patch, not a new backend or cloud dependency.

Explain which of the first ten missions can use this flow today and which need missing assets/mechanics. Return exact source references, diff/tests, command evidence if run and unverified boundaries. No active-checkout edits, GitHub synchronization or publication.
```

## Prihvatanje odgovora

1. Sačuvati odgovor kao zaseban predlog sa alatom, datumom i izvornim commitom.
2. Uporediti konkretan diff sa trenutnim Unity kodom i sačuvati lokalne izmene.
3. Proveriti ponovnu upotrebu postojećih vlasnika kretanja, misija, sejva i novca.
4. Izvršiti najmanji relevantan test, projektni gate i odgovarajući native tok.
5. Uzeti odabranu doradu; ne kopirati ceo Gemini/Copilot projekat preko kanonskog izvora.
