# Build and validation notes — 2026-10-15

## Verified state

- Tests: **75 passed / zero failed / one optional ArcGIS-specific skip**, 76 total, including geometry-normal regression coverage.
- Final verified Windows build: **zero errors, 227,705,869 bytes**. Direct3D 11 is selected; earlier Direct3D 12 startup stalled.
- The exact final Windows player passed runtime checks for local audio decoding/playback, 200 OSM road segments, 40 extruded OSM footprints, 16 encyclopedia cards, physical street-job payout, campaign, garden, save/restore and damage/respawn.
- Both final city and encyclopedia captures were visually inspected: facade atlas and card aspect are correct. The city remains a prototype, not a finished architectural reconstruction.
- Android APK packaging succeeded through the separate export/Gradle path. Installation, real-phone performance and store readiness remain unverified.

## Android Gradle trust diagnosis

AGP 9.1.1 and its plugin-marker POM exist: official Google Maven HEAD requests returned HTTP 200. Generated plugin repositories include Google. [Official AGP compatibility](https://developer.android.com/build/releases/agp-9-1-0-release-notes) lists Gradle 9.3.1 and JDK 17, matching the installed build stack. Do not randomly downgrade AGP to mask a trust failure.

The bundled JDK initially failed with SSLHandshakeException caused by ValidatorException. Repeating the same request using the existing Windows trusted-root store returned HTTP 200:

```text
-Djavax.net.ssl.trustStoreType=Windows-ROOT
-Djavax.net.ssl.trustStore=NONE
```

This is validated TLS against OS-trusted roots, not a TLS bypass. No certificates were imported, no hostname checks were disabled and no persistent JDK trust configuration changed.

The Unity build entry point supports opt-in `-vktWindowsRootTrust` and restores environment state in finally, but the direct Unity packaging retry still failed: its child Gradle invocation explicitly supplied heap-only `org.gradle.jvmargs`, overriding the daemon JVM settings. The JAVA_TOOL_OPTIONS path did not reach that child as expected. Thus generated properties or a successful standalone Java HEAD probe alone did not prove Unity packaging was fixed.

A direct **bundled Gradle `help` probe succeeded**, exit zero, `BUILD SUCCESSFUL in 43s`, using explicit Windows-root flags on both the Java launcher and the single grouped daemon argument:

```text
-Dorg.gradle.jvmargs=-Xmx2048m -Djavax.net.ssl.trustStoreType=Windows-ROOT -Djavax.net.ssl.trustStore=NONE
```

`Tools/Build-Android.ps1` implements the practical separate path: run with **PowerShell 7 (`pwsh`)**, export via `CityPortableBuild.ExportAndroidProject`, then invoke the bundled Java/Gradle directly with explicit launcher and daemon trust flags when `-UseWindowsRootTrust` is selected. The script uses `ProcessStartInfo.ArgumentList`, unavailable in Windows PowerShell 5.1, and preserves the whole daemon JVM value as one argument. It copies an APK only after successful assembly and emits its SHA256.

The actual APK attempt through this new path **succeeded**: Unity export completed with zero errors and direct Gradle reported BUILD SUCCESSFUL in 9m54s. Packaging success does not prove installation or device compatibility. This is a Windows-specific trust opt-in; other hosts should use their correctly configured native trusted certificate chain.

## Bounded build concurrency

The builder supplies `BEE_BUILD_THREADS=4` only when the caller has not already set a value, then restores prior state. Unity documents the control in its [6000.0 release notes](https://release-notes.ds.unity3d.com/search?from_release=6000.0.32f1&to_release=6000.0.71f1). The bound reduces concurrent native build pressure; it is not proof of improved gameplay performance.

## Remaining acceptance

Finish Android packaging and install on a real ARM64 device. Re-run save/resume, economy, damage/respawn, touch controls and measured frame time/memory/thermal behavior. Final Windows visual corrections have been inspected. Source three natural Serbian character voices with clear rights, test uninterrupted radio using a proper long-running backend, and persist story/side-job chapter progress. Commit/push status must be updated only from actual remote commit evidence.

## Final PC evidence refresh — 2026-10-15

`vkt-verified-tests-20261015.xml`: **75 passed, zero failed, one optional ArcGIS-specific skip / 76 total**. Final Windows build: **zero errors, 227,705,869 bytes**. Both `vkt-verified-city-player` and `vkt-verified-cards-player` runtime reports returned true for the checked paths: contracts, physics, garden/save/respawn, street-job payout, 200 OSM roads, 40 building footprints, 16 cards, music controls and local audio decoding. Final city/cards screenshots were visually inspected; facade atlas and card aspect are correct. This verifies the prototype checks, not full-city completeness or photorealism.

PC ZIP: **105,841,466 bytes**. SHA256: `8CAEB69EAE49443C0C6CBCFE88809BE4A5D68B5DFB99667694CE2C11B02DD251`.

Android split export/Gradle APK packaging succeeded. No ADB device was attached, so installation and real-device verification remain pending. Natural Serbian voices, verified uninterrupted radio, measured phone performance and persisted chapter progress remain next delivery gates.

## Verified Android APK — 2026-10-15

- Unity native/Gradle export: **Succeeded, zero errors**.
- Direct bundled Gradle assembly: **BUILD SUCCESSFUL in 9m54s**.
- `VKT.apk`: **51,984,055 bytes**; SHA256 `0C8C43BC45C968CC94C81D2364BA6C147FDF40DCF34FEE5021FE64923D496BFA`.
- Package `com.yashimi.vkt`; version code **1**, version name **1.0**; minimum API **28**, target API **36**.
- ABI **arm64-v8a**, IL2CPP; actual APK contains `libunity` and `libil2cpp` native libraries.
- `apksigner verify`: v2 signature **true**, **one signer**. This is a **debuggable development APK**, not a signed store release.
- No ADB device attached: **no installation, phone runtime/performance pass or store-delivery proof**.

Historical failed Unity-child trust attempts remain documented above. The successful artifact used the separate native export/direct Gradle path with strict Windows-root TLS validation. Final source commit/publication remains the coordinator's pending step; no later commit is asserted here.
