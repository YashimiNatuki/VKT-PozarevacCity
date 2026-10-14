# City mission system

The mission loop is implemented in the existing `Assets/CityGame` system. It uses the already installed Unity UI, Input System, Cesium and URP packages.

## Contracts

| Contract | Time after pickup | Cash | Reputation | Heat gain |
|---|---:|---:|---:|---:|
| First Night Courier | 120 seconds | 600 | 12 | 4 |
| Street Supply | 90 seconds | 1000 | 18 | 7 |
| Night Express | 60 seconds | 1500 | 25 | 10 |

Every contract requires pickup, delivery and contact confirmation. The player holds one cargo item between pickup and delivery. No partial route pays a reward. A completed contract cannot pay again; N unlocks the next contract.

## State and failure behavior

`Ready -> Active -> Completed` is the successful route. The timer starts at pickup. Incorrect stations and repeated interactions do not advance the objective.

An expired deadline or excessive session heat moves an active route to `Failed`, clears cargo and pays nothing. R creates a clean attempt at the pickup, once heat is below the contract limit. Heat cools down while waiting or recovering.

Esc pauses movement, interactions and game-time deadlines. The pause owner restores time scale and cursor state when disabled. Rewards update cash, reputation and heat together, after committing completion, so event observers cannot claim the same reward again.

## World and presentation

The bootstrap waits for upward-facing, non-trigger collision surfaces at the player spawn and three checkpoints. It excludes player and mission colliders from ground selection. Ground probes use a reusable raycast buffer. After 45 seconds without surfaces, movement remains disabled and the HUD offers T to retry.

Grounded objects use Cesium globe anchors. Checkpoints have shared URP materials: cyan for the current objective, green for visited objectives and muted colors for unavailable stations. The HUD shows mission progress, deadline, cargo, reward, horizontal distance and a relative direction hint. Presentation updates at 10 Hz; the deadline uses elapsed game time rather than frame count.

Collision placement does not prove street accessibility. The terrain provider and a real movement run through the chosen positions require their own validation.

## Ownership

- `CityMissionRun`: deterministic progression, timer, cargo, failure and reward rules.
- `CityMissionDirector`: contract sequence, input commands, checkpoint visuals and journal.
- `CityMission`: raycast interaction adapter, preserving the earlier standalone terminal API.
- `CityGroundProbe`: bounded collision-surface search.
- `CityGameBootstrap`: scene composition, loading gate, player/camera setup.
- `CityHud`: presentation and notifications.
- `CitySession`: cash, reputation and heat.

Completed contract counts and balances are now persisted by `CityProgressionController` and `CitySaveStore`; see `Progression-System.md`. An unfinished route restarts at pickup on reload. Challenge selection is session-only and resets to Standard on reload.

Before pickup, number keys 1/2/3 select Standard, Express or Ghost. Express uses 60% of the route deadline and pays +50% cash. Ghost requires heat below 20 (or the original cap if smaller) and pays +25%. Selection is locked during active/failed/completed states and blocked by pause. Retry retains the challenge and returns to Ready. See `Mission-Challenges-20261014.md` for exact rules and evidence.

## Controls

WASD: move. Mouse: look. Shift: sprint. Space: jump. E: interact. N: next completed contract. R: retry failed contract. Esc: pause/resume. T: retry terrain loading after timeout.

## Validation

The EditMode suite covers ordered objectives, cargo, time boundaries, failed attempts, cooldown, duplicate rewards, reward-event reentrancy, balance overflow, invalid configuration, frame-rate-independent deadlines and collision selection.

`CityGameValidation.RuntimeSmoke` runs a physics interaction scenario in PlayMode. The default `fixture` uses a temporary flat test surface; `-cityScenario actual` opens the real SampleScene. These are separate evidence levels. Neither silently saves scene changes.

`CityGameValidation.BuildWindows` builds SampleScene into an explicitly supplied output outside Assets. No publishing or deployment is performed.
