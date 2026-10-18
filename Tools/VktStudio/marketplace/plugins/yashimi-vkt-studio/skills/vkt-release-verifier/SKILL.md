---
name: vkt-release-verifier
description: Verify VKT Windows test evidence, source fingerprints and distributable hashes, with honest Android follow-up status. Use before VKT release or handoff, not account cleanup or other repositories.
---

# VKT Release Verifier

CODE_IS_EVIDENCE: a passed build does not prove natural speech, full navigation, stable FPS or a clean dependency audit.

Resolve the intended project and baseline first. Use `../../scripts/project_snapshot.py --project ABSOLUTE_PATH` for a read-only source inventory. It must never read saved profiles, credentials, streamed map tokens or arbitrary project scripts. Inspect current git diff and preserve independent changes.

After the stable implementation, run relevant Unity EditMode tests and the actual Windows player through the touched behavior. Capture visible states for character/UI changes. Verify mission settlement/resume, menu pause and lifecycle cleanup when relevant. Keep one bounded repair/review loop; state any unresolved failure. Preserve original intro bytes and explicit input provenance. Check optional local AI offline fallback independently from generation.

Use `../../scripts/verify_release.py --project ABSOLUTE_PATH --tests TEST_XML --receipt RECEIPT_JSON --artifact ZIP_OR_APK` to check XML outcomes, artifact size/hash and the receipt's selected-source fingerprint. This helper reads evidence only; it does not build, install, run the player, sign, publish or certify listening quality. Its legacy fingerprint covers selected extensions under CityGame, excluding meta, prefabs/scenes, Packages and ProjectSettings. Never call that a complete source/build-input match. It validates XML byte identity only when the receipt supplies `testsSha256`; aggregate counters alone do not identify a test run. Inspect `completeBuildInputBinding`, `testXmlIdentityBound` and `releaseApprovalReady`; the legacy helper explicitly does not approve a new release. A new production gate needs a build-produced complete input manifest and test digest, not retrospectively added proof.

Keep development packages separate from final production claims. Windows users extract the whole ZIP and run VKT.exe; Unity Editor is not required. Android needs compatible signing and a save backup before `install -r`; never uninstall/clear user data as an upgrade shortcut. Installation is not a touch/audio/FPS check. USB disconnect means current hardware validation is pending. Earlier v6 metrics do not validate v7 or a later release.

Before publishing, check source/receipt consistency, private-file exclusions, exact archive contents and license notices. Synchronize the original only when every target matches its known baseline. When authorized, commit/push and verify the remote commit. Compare uploaded digest/size and actual download response. Document Dependabot status without suppressing/dismissing alerts to claim success. Never delete similarly named repositories as release cleanup.

Return Windows link, changes, exact test/build/runtime results, skipped work, Android status and at least five next priorities. Keep account inventory and private reports outside the public VKT repository.
