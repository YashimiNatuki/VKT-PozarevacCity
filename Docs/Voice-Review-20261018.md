# Serbian voice intake — 2026-10-18

The running game retains authored Serbian captions and its existing optional dialogue slots. No newly synthesized voice is certified or shipped as several natural actors in this milestone.

## Human listening result

The user listened to the generated candidate and reported: **understandable, but robotic**. It is rejected for admission to the game. The local synthesis runtime remains useful for prototyping; no naturalness claim or actor-bank substitution follows from successful inference.

## Rejected shortcut

The `rhasspy/piper-voices` path `sr/sr_RS/serbski_institut/medium` appears Serbian by its folder label, but its model card points to [Lower Sorbian data](https://github.com/marytts/serbski-institut-dsb-data), with a CC-BY-NC-SA dataset licence. Lower Sorbian is not Serbian. Do not install it as the game's Serbian voice merely because the code looks like `sr_RS`.

## Actual free local candidate

[phantom9623/piper-serbian-tts](https://huggingface.co/phantom9623/piper-serbian-tts), revision `a71694f9ec3480f132dffaf7eb422d43aebd69e0`, provides `sr_Marko_medium.onnx` under the publisher-declared MIT licence. Weight SHA256: `2d009fe6d43e25b9516b23f5e64f65af202e76f0e6940e452dfb45852a412a68`. The publisher links its [Slovenian base](https://huggingface.co/ppisljar/piper_si_artur), whose card identifies a CC-BY-4.0 dataset. Preserve both provenance links when admitting outputs. The candidate card itself inconsistently labels the language description as Slovenian; that discrepancy requires listening review, not blind trust in a filename.

Piper 1.4.1 and pathvalidate 3.3.1 were installed in a separate local virtual environment. The CLI initially lacked pathvalidate; the dependency was added and synthesis then succeeded. A 22,050 Hz mono 3.85-second WAV was generated for: **„Posao je završen. Proveri telefon i sledeći ugovor.“** It is a candidate outside Unity, not a bundled voice bank. One speaker is one actor; pitch shifting does not establish three independent voices. Engine GPL licensing remains separate from the model's publisher licence and first-party game code; no Piper runtime or weights are linked into the APK.

[daremc86/sr-cv-vits](https://huggingface.co/daremc86/sr-cv-vits) is another publisher-declared CC-BY-4.0 Serbian candidate, single female speaker, Cyrillic input, with a documented numbers limitation. It was downloaded at revision `83dc1e1b95d85b9f5602dc94909706fc83dfbc6c` and successfully synthesized a separate 5.24-second, 22,050 Hz mono candidate for Mila. Its checkpoint SHA256 `7b43231864dbac69901155ed397c1a30d0d06b066b03f0348fd39eed2ea1d4b0` matches the provider LFS digest. The user could not listen at this time, so naturalness remains unreviewed and the candidate is not admitted to Unity. Model-card quality claims are not our listening evidence.

The isolated working combination is Coqui TTS 0.27.5, PyTorch/torchaudio 2.8.0 and Transformers 4.57.6. The initially resolved Transformers 5.20.1 removed an API required by Coqui; pinning the compatible version fixed the import. Running relative to the model directory resolves its speaker/language ID files. Checkpoints load with `TORCH_FORCE_WEIGHTS_ONLY_LOAD=1`, without disabling safe loading. `pip check` passed, and the public `Tools/Audio/generate_vits_candidate.py` was actually run successfully from outside the model folder. It validates the pinned checkpoint digest, refuses to overwrite outputs, caps CPU threads and generates only a local review WAV. `requirements-vits.txt` records the working direct dependencies.

## Admission criteria

Review Serbian pronunciation, accent, pacing and slang using several short gameplay lines. Match each recorded caption to the spoken meaning. Confirm the selected model/speaker and redistribution terms; use consenting recordings for custom voice cloning. Normalize speech loudness/true peak, preserve a quiet tail, avoid clipping, import at suitable platform compression and test on Android speakers/headphones. Ship subtitles even when audio exists. Do not substitute a wrong-language voice when the reviewed Serbian bank is absent.

The existing `Tools/Audio/generate_voices.py` bakes explicitly selected reviewed local models into the three named Unity slots. This intake demonstrates a working free local synthesis path; a better natural-sounding candidate and distinct voice selection remain open work.
