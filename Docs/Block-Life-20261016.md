# VKT Block Life — character development and consequences

This milestone connects a playable street gym, hunger/stamina, paid or stolen meals, a foot patrol response and a one-time neighborhood challenge. It extends the existing single-player Unity game and retains the three camera modes, campaign, garden, trading, music and street-job progression.

## Play

Open **L** on desktop or **BLOK / LIK** on touch. The panel shows level, XP, strength, nutrition, stamina, workouts, meals, escapes, arrests and distances to the gym/kiosk. Approach the physical bench or kiosk and use **E / KORISTI** to open the same panel. The new fixtures are authored near the existing central spawn, not surveyed businesses.

- Gym: approach within 3.5 m, choose training, then press **F** or tap the repetition button 24 times within 15 game seconds. Repetitions are rate-limited to one per 0.18 seconds. Walking away, timeout or death cancels without XP. Pausing or suspending the app freezes the timer. Successful training gives 40 XP and requires 90 game seconds of recovery before the next session.
- Every three completed workouts adds a strength point, capped at five. Each point improves sprint stamina efficiency, adds 2.5% to the well-fed sprint multiplier and adds 8% to melee impact against the existing hostile hazard. Level increases every 100 XP, capped at level 20; level itself does not unlock an unimplemented skill tree.
- A meal costs 35 game DIN, restores 35 nutrition and 20 stamina, and is consumed immediately. Meals have a 20-second cooldown and cannot be used above 90 nutrition or during training/pursuit. There is no carried-food inventory in this slice.
- Stealing the meal skips payment, records the theft, adds 30 HEAT and immediately dispatches the pursuit. This is a scripted seller report, not a city-wide civilian witness simulation.
- Nutrition falls by one point per 20 active game seconds. Sprint drains stamina; walking/standing restores it. Low nutrition slows sprint and very low nutrition prevents sprinting. Exhaustion requires recovery before sprint resumes. This version does not apply starvation health damage or offline hunger.
- The optional **IZAZOV BLOKA** tracks at least one workout, meal and escape. It gives 250 DIN, 10 REP and 60 XP once. It is independent of the existing three campaign contracts.

## Police and effects

Two bounded foot patrol actors use the reviewed existing Kenney character resources and authored police caps. Existing crimes accumulate HEAT; 45 HEAT also begins a pursuit. Hitting an officer causes a short nonlethal stun, adds HEAT and reports the attack.

Patrols check actual physics line of sight within 32 m. They pursue the last observed position when sight is lost, with local obstacle steering and CharacterController collision. Eighteen unseen game seconds ends pursuit and gives 30 XP; a visible officer within 2.2 m must maintain contact for 2.5 seconds to arrest. Arrest removes at most 120 DIN, clears HEAT and returns the living player to the existing checkpoint. Unfinished work fails without payout. Restarting does not clear a saved wanted flag; transient search/contact timers restart conservatively.

The response includes an authored procedural siren and a small red/blue status banner. The siren follows master mute, gameplay pause and application suspension. No external recordings or natural Serbian voice performances were added.

This is **foot pursuit with local steering**, not a completed road navigation graph, police cars, traffic system or squad tactics. Complex building corners still require navigation work and wider playtesting.

## Catalog and city names: what is actually used

The existing phone contains **16 strain identity cards**, with history/provenance and primary breeder links; **eight** have stable trade/inventory IDs, purchase prices and unlock rules. The other eight are informational. The eight atlas illustrations are representative AI artwork, not authenticated cultivar photographs. The new character panel links back to this catalog after the first contract unlocks the phone. See Strain-Catalog-20261015.md and Art-Provenance.md.

The city registry retains **18 user-supplied candidate locations**. BLOK / LIK → DELOVI GRADA cycles their names, coordinates and uncertainty notice; the conflicting Ljubičevo/Hipodrom record remains flagged. Street-job briefings now identify contacts from Centar, Burjan, Radna mala, Tulba, Busije and Meminac. Their physical routes still use the three central stations. The full districts have not been built by adding their names. Offline map coverage remains 600 selected road segments, 100 footprints and 22 green polygons.

## Persistence and verification

Profile version **5** adds bounded character statistics, cooldowns and the wanted/challenge flags. Versions 1–4 migrate with fresh lifestyle defaults while retaining cash, inventory, garden, crew and completed jobs. Workout repetitions are session state; reloading an unfinished workout never grants its completion reward. The existing checksum, backup recovery and stale-writer protection remain in CitySaveStore. Older APKs cannot interpret the new profile version; keep the previous backup rather than forcing a downgrade.

Final tests: **121 passed, 0 failed, 1 optional ArcGIS skip (122 total)**. Final Windows development build: zero errors, 228,347,133 build-report bytes. Two actual final Windows player runs verified the existing game plus physical gym interaction, UI repetitions/anti-spam, menu and application-pause behavior, meal transactions, wanted save restore, police visibility/occlusion/capture range, accelerated game-time escape/arrest, the one-time challenge and v5 save restoration. Both resulting captures were inspected.

The police transition checks advance model game time in the smoke test; they do not establish a full open-world chase playthrough. Android build/signature evidence and artifact hashes are recorded in Verification-BlockLife-20261016.json. Physical-phone installation, performance, touch ergonomics and subjective listening remain unverified.

Next: measure Android performance and touch behavior; bake/validate navigation on actual streets; extend authored central landmarks and grounded district geometry; add more varied mission routes and natural Serbian dialogue.
