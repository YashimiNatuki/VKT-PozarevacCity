# Character and HUD refinement — 2026-10-18

The existing Kenney low-poly avatar had a large head, a long neck, oversized extremities and a static painted face. This slice changes the shared physique used by walking, bench and wardrobe: reduced head, shortened neck with head-axis compensation, smaller hands/feet and a lower fitted cap. The model remains normalized to 1.8 m and limb lengths, saved outfits and strength progression remain authoritative.

The owned character material now supports bounded atlas expressions: asymmetric-timing blink, subtle gaze, brow effort and a small mouth-corner change. Running, boxing and bench effort feed presentation only. There is no facial skeleton, phoneme lip sync, realistic eyeball geometry or photoreal skin. The original 256-pixel atlas and low-poly mesh remain the limiting assets; expressions are a bridge to a licensed facial rig, not a claim of realism.

Wardrobe has a FACE / FULL BODY button and a 768x1024 render target instead of 384x512. Its expression clock advances while its own menu pauses gameplay; gameplay faces use scaled time. Rotate buttons, outfit saving and close/pause ownership remain intact.

HUD profile, mission and vitals cards now use a common 1280x720 reference, smaller cards and clearer spacing. Training hides the mission card/text, interaction prompt, crosshair, notifications and lifestyle badge, restoring valid state on exit. Music sits in the lower-right corner instead of covering the player. Training input/buttons are unchanged.

Validation: 180 Unity EditMode tests passed, zero failed, one optional ArcGIS skip. Actual Windows D3D11 player regression passed existing missions, disk-resume/single payout, combat, intro, spatial audio, wardrobe/outfits, bench, conditioning and boxing. New runtime validation entered portrait view and observed a blink while wardrobe owned gameplay pause. Screenshots were inspected; face remains visibly stylized. No physical Android, listening review or mobile touch/FPS proof is claimed. This milestone produces Windows only; the prior Android APK does not contain these character/HUD changes.

Next priorities:
1. Replace the painted low-poly face with a free licensed rigged head: eyelids, eye tracking, jaw and expression blendshapes, with a mobile LOD.
2. Retarget walking, turns and training to the upgraded skeleton; verify foot planting and hand contact.
3. Bind reviewed Serbian dialogue to phonemes or recorded amplitude, with a silent captions fallback.
4. Build the updated Android APK and test portrait/full-body UI, touch, safe areas, audio and sustained FPS on hardware.
5. Consolidate phone, journal and lifestyle menus into a consistent responsive UI with gamepad focus and font scaling.
6. Rebuild recognizable central Požarevac landmarks with measured scale and collision/terrain verification.
