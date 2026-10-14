# VKT progression and local profile

Identity: **VKT / VelikaKradjaTočk🛞va — Po-Blok-C**, with **what bike 🚲 since 1995**.
The runtime uses Latin text compatible with its existing Unity font; emoji artwork is not a generated logo asset.

## Player flow

1. After intro and terrain readiness, choose a callsign (2–24 letters/numbers, spaces, hyphen or underscore). Existing profiles resume without this creation step.
2. Complete First Night Courier: pickup → delivery → contact. This unlocks the phone on P.
3. The phone manages one fictional Night Bloom plot: buy a seed, plant, return to the streets for 30 game seconds, harvest three inventory units and trade.
4. Street Supply brings mission reputation to 30 and unlocks crew recruitment.
5. After Night Express, recruit at least two crew members and claim three blocks to finish this management campaign.

Existing mission navigation, deadline failures, retries, heat, pause and intro continue to work. The phone is a pause-owning overlay: mission time, movement and growth stop while it is open. P/Esc closes it after character creation. A previous pause state is restored instead of always forcing gameplay to resume.

## Rules

| Action | Cost/result | Gate |
|---|---:|---|
| Buy seed | 40 cash | First contract; max 30 seeds |
| Plant | One seed; one plot; 30 game seconds | Plot empty |
| Harvest | 3 stock | Mature plot; inventory capacity 30 |
| Trade | 35 cash per stock | Non-empty inventory |
| Recruit | 250 cash | Reputation 30; max 3 crew |
| Claim block | 750 cash | Three contracts + two crew; max 3 blocks |

The nominal complete test route starts at 2500 cash, earns 3100 from contracts, pays 40 for a seed, earns 105 from harvest, pays 500 for two crew and 2250 for three blocks. Final balance: **2915 cash**, reputation **55**, crew **2**, blocks **3**.

These are fictional gameplay resources. Crew and blocks are management progression values; NPC teammates, combat AI and spatial territory simulation are not implemented by these counters. Character creation chooses a persistent callsign, not a full 3D appearance editor. These limits must remain explicit when describing the game.

## Ownership

- CitySession: authoritative cash, reputation and heat.
- CityProgression: callsign, unlocks, seeds/stock, plot timer, crew and blocks.
- CityMissionDirector: mission progression and overlay pause ownership.
- CityProgressionController: composition, game-clock advancement, save checkpoints and UI.
- CityPhonePanel: existing uGUI/Input System presentation.
- CitySaveStore: versioned local file boundary.
- CityPlayerValidation: development-only, explicitly requested Windows-player smoke automation.

There are no new packages, scenes or vendor changes. The existing Cesium scene and assets remain intact. The source folder stays unchanged; renaming an entire project directory is not required for a product identity change.

## Saves and recovery

Normal play saves vkt-profile-v1.json under Application.persistentDataPath. A valid profile restores balances, callsign, contract completion, plot remaining time, stock, crew and block counts. Completed contracts advance to the next ready route; after all three, completion is restored without a new payment.

Mid-contract pickup/cargo/deadlines are deliberately not checkpointed. Reload restarts that unfinished contract at pickup and pays nothing for an incomplete attempt. Growth resumes at its saved remaining game time; elapsed real-world offline time is not converted into production.

Saves use versioned JSON, a SHA-256 corruption checksum, same-directory temporary writes, disk flush and atomic replacement with a .bak file. Recovery from a damaged primary preserves damaged and backup copies before replacing it. Unknown newer formats or two unreadable copies disable automatic writes. The game can continue as a non-persistent Guest without overwriting those files.

An exclusive file gate and content fingerprint reject a stale writer from another game instance. Checksums are not encryption or protection against an owner intentionally modifying a save. Small bounded saves are written at completed contracts and economy actions, at most every five seconds for ordinary changed session state, and on shutdown/pause.

Batch validation always creates separate temporary profiles, preventing test runs from changing a personal playthrough.

## Verification scope

- EditMode domain and file tests verify balances, unlocks, invalid data, actual save/reload, backup recovery, future-version refusal, stale-write rejection and observer reentrancy.
- Editor runtime validation exercises three contracts through nine physics interactions and verifies phone pause, the economic loop and file restoration.
- The development-player smoke repeats that route in the Windows executable and can capture its rendered phone/terrain view.

Automation moves the player to stations. It does not establish human walking accessibility, controller support, every screen size, NPC behavior or final city rendering quality.

## Build

CityGameValidation.BuildWindows sets the product name to VKT - VelikaKradjaTockova and writes a separately named Windows build. The original source, previous build, scene and intro video remain preserved.

The scoped pre-change source and ProjectSettings backup is under the workspace .yappi-staging/vkt-citygame-baseline-20261013 directory.

The rendered proof uses the installed public URP SingleCameraRequest API in a development player. UI is temporarily camera-bound at the near plane for offscreen capture and restored afterward. Phone opening hides gameplay HUD/world labels and adds a dim backdrop. A pixel gate rejects blank frames; rendered images were also visually inspected.
