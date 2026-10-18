# Character expression roadmap

The user's local companion prototype demonstrates a useful lightweight pattern: a four-cell expression atlas (calm, wink, smile, focus), activity-driven states, breathing, bounded visual effects and reduced animation work when hidden or backgrounded. Its implementation is a 2D Canvas sprite with depth effects. Inspection did not turn it into a rigged mesh; no private source, portrait atlas or voice files were copied into this repository.

For VKT, transfer the design in two stages:

1. Conversation/phone portraits: an independently produced, licensed expression atlas, explicit neutral fallback, lazy loading and a fixed memory budget. Choose expressions from authored mission/dialogue events; AI-generated text does not control rewards, combat or progression.
2. The world avatar: a licensed humanoid head with eyelid, jaw, gaze and expression blendshapes. Drive the same neutral/focus/success/tension/exertion/hurt states through clamped blends, with hurt/exertion overriding the idle face. Preserve body animation, bench IK, outfits and saves.

Keep blink and gaze independent of expression transitions; use phoneme or reviewed speech-envelope animation only after the voice bank is approved. Update nearby facial animation at a bounded rate, reduce work at distance and stop hidden/background presentation. Provide a static neutral pose for reduced motion. Validate the same face from front, side, wardrobe and training cameras on the actual Android device.

Current status: existing VKT atlas blink, subtle gaze and training effort remain implemented. The new expression atlas, emotional state controller and facial blendshape rig are planned, not bundled in the Android v6 build.
