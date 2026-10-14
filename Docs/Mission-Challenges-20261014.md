# VKT mission challenges

## Recovered project state

The working source is `C:/Users/jamal/Pozarevac Lejt Najt 3D Gta-ThugLife`, Unity 6000.6.5f1.
The source already contains three ordered contracts, grounded raycast interactions,
timers/retries, phone onboarding, inventory/trading, crew, district management and
versioned profile storage. See `Delivery-20261014.md` for the earlier milestone's
historical evidence. That report is not a fresh verification of these changes.

## New behavior

Before pickup, choose a challenge using the number row:

| Key | Challenge | Deadline | Maximum starting/active heat | Cash |
| --- | --- | --- | --- | --- |
| 1 | Standard | Original | Original contract limit | Original |
| 2 | Express | 60% of original | Original contract limit | +50% |
| 3 | Ghost | Original | Below the smaller of 20 and the original limit | +25% |

These are variations of the existing three routes, not additional locations or
new NPC mission types. Express deadlines are 72, 54 and 36 seconds. Ghost is a
heat constraint, not simulated police perception or stealth AI.

The choice is locked after pickup. Failure clears cargo, pays nothing and permits
retry only below the selected heat limit. Retry retains the choice; while back at
Ready, the player may change it again. Pause and phone overlays block selection.
The HUD displays the actual reward, heat cap, challenge and deadline.

Only final confirmation pays. Completion is committed before reward observers
run. Integer bonuses round down and balances saturate rather than overflow.
Reputation and completion heat remain the original values. The final heat gain
is applied after successful completion; it does not retroactively fail Ghost.

## Save compatibility

No profile fields or schema version changed. Completed route counts and actual
awarded balances use the existing durable save path. An unfinished route resumes
at pickup in Standard after restart, matching existing restart semantics; its
challenge choice is session-only. Completed contracts do not become replayable.
New contracts start in Standard.

## Focused next milestones

1. Validate a continuous walkable route through the real city without checkpoint teleportation.
2. Introduce a bicycle interaction and ride/dismount loop with collision and save tests.
3. Add mission types around that vehicle: retrieval, checkpoint race and delivery.
4. Add spatial NPC/pursuit behavior and crew activity after those movement boundaries work.

These are pending product work, not implemented features.

## Changed surfaces

- Runtime/CityMissionRun.cs: challenge rules and reward calculation.
- Runtime/CityMissionDirector.cs: keyboard selection, pause gate and HUD.
- Tests/Editor/CityMissionChallengeTests.cs: deterministic boundary and reward coverage.
- Editor/CityGameValidation.cs: optional `-cityChallenges true` engine scenario,
  verifying Express/Ghost selection, locking, premium rewards and save restoration.

Original runtime files were copied before editing to
`C:/Users/jamal/Documents/CodexBackups/VKT-Challenges-20261014-181201`.
No vendor package, scene YAML, credential, cloud account or unrelated application is modified.

## Fresh evidence

- `C:/Users/jamal/Documents/vkt-challenges-tests-20261014.xml`: 45 EditMode tests passed, zero failed; includes six new challenge tests.
- `C:/Users/jamal/Documents/vkt-challenges-runtime-20261014.json`: actual SampleScene Play Mode scenario passed with challenges enabled, nine physics raycast interactions, phone pause, economy, crew, districts and durable save restoration. Final cash is 3465 (550 above the Standard scenario).
- `C:/Users/jamal/Documents/vkt-challenges-build-20261014.log`: Windows development build succeeded, zero build errors, reported output size 608114404 bytes.
- Runnable build: `C:/Users/jamal/Documents/CodexBuilds/VKT-Challenges-20261014/VKT.exe`.
- `C:/Users/jamal/Documents/vkt-challenges-player-20261014.json`: fresh Windows executable passed terrain readiness, all nine Standard-route physics interactions, three contracts, economy, crew, districts and durable save restoration. This player scenario uses Standard; the premium modes were exercised in the actual-scene Editor scenario above.
- `C:/Users/jamal/Documents/vkt-challenges-player-20261014.png`: actual offscreen render inspected. Management UI is readable, but the surrounding world shows overhead/overlapping geometry from the automated final checkpoint viewpoint. This is a visual/world-placement issue to investigate before treating the city as polished or traversable.
- Runtime command: Unity `-batchmode -executeMethod Pozarevac.CityGame.Editor.CityGameValidation.RuntimeSmoke -cityScenario actual -cityChallenges true -cityReport <report path>`.

The engine scenario moves the player directly between checkpoints. It does not
prove continuous street traversal, physical keyboard input, readable rendering
at every resolution, NPC stealth or long-session performance.

The actual-scene log also records an ArcGIS SDK Editor `MissingReferenceException`
in `ArcGISMapComponent.OnDisableEditor` when entering Play Mode. The gameplay
scenario passed despite that separate error. Package/license, DOTween importer
and SDK cleanup messages are retained in the raw log. This is not a claim of a
globally clean Console or production release readiness.

The packaged player also logs the existing ComputeBuffer cleanup warning on
shutdown. No zero-leak or whole-renderer correctness claim is made.

## Before and after

Before this increment, each of the three contracts had one fixed deadline, heat
cap and cash payout. After it, each has three selectable risk/reward conditions,
with accurate journal values and state-locked selection. Existing Standard
campaign totals remain 2915 final cash in the complete economy scenario;
Express then Ghost then Standard ends at 3465. The original three routes and
their objective locations remain the campaign content. Vehicle missions,
spatial NPC simulation and a continuous street traversal pass are still pending.
