# Character body and wardrobe milestone

## Changes

The player now has a smaller head and hands relative to the body, a re-normalized 1.8 m base height, and strength-dependent chest, upper-arm, forearm, thigh and calf shaping. Body girth is applied to owned copies of the existing skin in bind space. Joint lengths remain stable for animation and bench IK. Repeated application uses the original vertex data and cannot accumulate scale drift.

Three free cosmetic outfits share the existing licensed atlas: Korzo (petrol shirt, charcoal trousers, light sneakers, dark cap), Noćni blok (dark shirt, slate trousers, red sneakers, no cap), and Busije (burgundy shirt, stone trousers, dark sneakers/cap). The original face/skin texture remains intact. A new URP shader applies clothing-region colors, small fabric variation and an original geometric print. A curved procedural baseball cap follows the head bone.

Open L / BLOK / LIK, then ODEĆA / 3D GARDEROBER. The wardrobe pauses gameplay, provides a full-body turntable, three outfit buttons and rotation controls. Return with ESC or NAZAD U GRAD. The same selection is used by the world player, wardrobe and bench presentation.

Profile v6 stores appearance. Versions 1–5 migrate to the default outfit while retaining gameplay data. Invalid outfit IDs are rejected; snapshots do not alias live appearance data. Existing backup/stale-writer behavior remains in the save store.

Walk/run presentation now adjusts clip speed and arm swing to measured travel speed, bends the forearms and treats large teleports as discontinuities. It still uses the existing idle/run animation clips.

## Resource ownership

The existing FBX models are explicitly CPU-readable for bind-space skin deformation. Each player presentation owns one bounded mesh copy per skinned renderer; no new mesh is allocated per frame or per repetition. Body deformation is recalculated only when strength changes. The look object owns generated materials/accessory meshes and releases them with its body mesh copies. The wardrobe owns a 384 × 512 preview target and camera, enabled only while open.

## Validation and limits

EditMode coverage includes v1–v5 appearance migration, outfit validation/round-trip snapshots, body normalization/idempotence, shader import and full-stroke bench hand/foot contact. 134 tests passed, zero failed, one optional skip. Windows build completed with zero errors. The actual Windows player passed wardrobe selection/preview, pause, v6 save/load, world/bench appearance matching, bench contacts and prior campaign/economy/audio/combat checks. Both captures were inspected; see Verification-Character-20261016.json.

This is a substantial revision of the existing stylized Kenney character, not a photorealistic human scan or a likeness of the user. Shirt/pants remain the original skinned clothing geometry, not simulated layered garments. Wardrobe masks target this atlas specifically. There is no cloth physics, facial capture, finger-grip animation, complete locomotion blend tree, or new get-on/get-off bench animation. Physical Android rendering/performance and real-phone touch ergonomics remain unverified; no new APK is delivered in this wave.

Next: authored bench transitions, separate walk/strafe/jump clips, finger grips, more garment silhouettes, controlled lighting/skin detail, and a measured Android build.
