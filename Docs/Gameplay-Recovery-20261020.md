# Gameplay recovery and readable onboarding - 2026-10-20

Windows remains the main target; Android version code 8 uses the same profile v7 without a reset. This slice reuses CityProgressionController, CityPhonePanel, CityMissionDirector, CityLifestyleRuntime and the existing imported character. No new engine, cloud backend or paid runtime dependency was introduced.

## Before and after

| Reported problem | Implemented behavior |
| --- | --- |
| Phone/map unavailable before the first contract | P/TELEFON and M/MAPA work immediately after character creation; the trade catalog retains its first-contract gate. |
| No explanation of HUD or controls | Four onboarding pages open after creation; persistent TELEFON/MAPA/LIK/POMOC buttons and F1 help remain available for existing saves. Pages describe money, life, HEAT, XP, nutrition, energy, mission acceptance, map, training and audio. Touch help describes actual Android gestures/buttons. |
| Large music modal interrupts gameplay | 320x106 mini player, separate optional settings, F7 play/pause and F8 next; no pause/cursor ownership. Android places it below the top control row so TRCI/SKOK remain clear; its settings drop down. ESC closes the player before the normal pause action. |
| WASD stops after music in touch preview | Desktop keyboard movement is accepted alongside touch preview without requiring a locked cursor. True mobile input remains touch driven. |
| Nearly rigid legs, synchronized arm bend | Distance-driven opposing upper-leg motion and knee flex are applied after imported animation; lower-arm bend follows signed phase. Bone lengths and restored base rotations are retained. |
| Training appears dead | A specific message identifies distance, pursuit, active job, rest, nutrition or energy. Eight complete timed bench repetitions remain required; rewards are unchanged. |
| Default-looking UI text | OFL Noto Sans regular/bold, Serbian glyph coverage, readable hierarchy and highlighted buttons reuse the existing rounded-card skin. |

The early phone journal now explains the current main contract before street jobs unlock. The first-contract message correctly announces the catalog rather than claiming the already available phone was locked.

The same work includes a bounded jaw-relax blendshape on an owned clone of the existing mesh, five expression previews, and a fixed-size 1800-frame metrics window. These are incremental improvements to a stylized model, not a replacement facial rig or photorealism.

## Verification and limits

197 Unity EditMode tests passed, zero failed, one optional ArcGIS skip. Seventeen local studio helper tests and 23 backend/retired-audio tests passed. The backend suite reports an existing Starlette/httpx deprecation warning. A combined invocation initially used the isolated AI environment without NumPy. The two synthetic elevation-audit tests then passed in the existing GIS toolchain environment; this validates the audit helper, not actual terrain heights. Terrain code was unchanged.

The final Windows player exercised character creation, help open/close, immediate phone/map, non-modal music, movement after closing music, both rendered knee poses and completion of all eight bench repetitions in desktop and touch-preview layouts. The touch flow also requires the real music card bounds to clear TRCI/SKOK control bounds. Unity Input System keyboard events and EventSystem submissions are synthetic native-player input, not OS or human input. Rendered knee changes exceeded 32 degrees on both sides in these checks. A separate broad player smoke covers intro/video, city colliders, missions, garden, combat, audio mute/suspension, character, conditioning, boxing, disk job resume and one-time settlement.

Earlier synthetic bench and Escape/conditioning checks failed. The added state report showed that the Escape event did not reach the menu: menu, pause and conditioning remained active. Bench automation now follows its actual phase instead of fixed sleep durations. The synthetic keyboard alone is marked background-capable and waits for Update/end-of-frame processing before release/removal; real device focus rules are unchanged. Unity documents that non-background devices may be reset/disabled on focus loss: https://github.com/Unity-Technologies/InputSystem/blob/develop/Packages/com.unity.inputsystem/Documentation~/background-behavior.md. Final reports retain all assertions. This does not establish that every load condition is resolved; longer keyboard/mouse and traversal sessions remain required.

Both portable builds completed with zero build errors and include the byte-identical original intro and Noto licensing notices. APK signing compatibility is checked before installing as an update. Existing external game data is backed up privately; personal saves and music are not published. The user confirmed physical help/navigation buttons respond normally on the first v8 candidate. Final candidate device details and exact artifact hashes are in Verification-Gameplay-Recovery-20261020.json. Logs, backups, notification screenshots and device identifiers remain private.

Android still logs the existing AssetPackManager ClassNotFoundException at startup. No new natural NPC voice is bundled; diagnostic audio is a tone and conversations use captions. No new terrain height, neighborhood, skateboard, scooter, bicycle or transforming bench is claimed. Remaining UX work includes duplicate legacy touch shortcuts and oversized world labels near some objects; this wave does not redesign every game display. Frame samples exclude paused menus/intro and are not a long traversal, thermal or leak test.

## Files and operation

CityControlsDock owns help and the visible control row. CityMusicPanel owns only music UI. The existing director owns world pause; phone/lifestyle panels keep their existing close/release path. CityActorMotion restores its additive pose before sampling and clears it on disable. CityFaceMorph/CityCharacterGirth own the cloned jaw mesh. CityFrameWindow bounds metric storage.

[Current Serbian TXT guide](VKT-KONTROLE-I-PLAN-20261020.txt) describes the exact controls. [Ordered development plan](Rodja-Development-Plan-20261020.md) covers body, reviewed terrain, mission reactions, audio, micromobility and the rare fictional bench event. [Tool routing](Tool-Routing-20261020.md) separates available/installed capabilities from calls actually made. The portable Konglomerat/Rodja local kit contains three skills, not a new server or a game requirement.

Next: (1) interactive contextual tutorial and unified responsive HUD; (2) grounded locomotion, proper face/eyelid rig and clear bench camera; (3) reviewed central-block elevations with road/building/mission integration; (4) approved Serbian voice bank and environment mix; (5) longer mission/police/performance regression; (6) skate/scooter prototype, then bicycle and the opt-in bench surprise.
