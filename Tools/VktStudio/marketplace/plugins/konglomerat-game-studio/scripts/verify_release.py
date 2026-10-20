"""Read-only binding of Unity test XML, release receipt, source and artifact bytes."""
import argparse
import json
from pathlib import Path
import xml.etree.ElementTree as ET
from project_snapshot import digest, project_root, source_fingerprint


def verify(root, tests, receipt_path, artifact):
    xml = ET.parse(tests).getroot()
    counts = {name: int(xml.attrib[name]) for name in ("total", "passed", "failed", "skipped")}
    if counts["failed"] or counts["passed"] <= 0 or any(n < 0 for n in counts.values()):
        raise ValueError("Unity tests did not pass")
    if counts["passed"] + counts["skipped"] != counts["total"]:
        raise ValueError("Unity test totals are inconsistent")
    receipt = json.loads(receipt_path.read_text(encoding="utf-8-sig"))
    if receipt.get("buildErrors") != 0:
        raise ValueError("Receipt does not prove zero build errors")
    if artifact.suffix.lower() not in {".zip", ".apk"}:
        raise ValueError("Expected a Windows ZIP or Android APK")
    if artifact.suffix.lower() == ".zip" and receipt.get("windowsPlayerPassed") is not True:
        raise ValueError("Receipt does not record a passed Windows player run")
    item = receipt if receipt.get("name") == artifact.name else receipt.get("windows", {})
    if item.get("name") != artifact.name or item.get("size") != artifact.stat().st_size or item.get("sha256") != digest(artifact):
        raise ValueError("Artifact name, size or SHA256 differs from receipt")
    fingerprint, count = source_fingerprint(root)
    if receipt.get("sourceFingerprint") != fingerprint:
        raise ValueError("Current source differs from the built receipt")
    recorded = receipt.get("tests", {})
    if any(recorded.get(name) != number for name, number in counts.items()):
        raise ValueError("Receipt and test XML are from different results")
    xml_digest = digest(tests)
    if receipt.get("testsSha256") is not None and receipt["testsSha256"] != xml_digest:
        raise ValueError("Test XML bytes differ from the receipt")
    return {"schema": "vkt.release-verification.v1", "passed": True,
            "artifact": artifact.name, "sha256": item["sha256"], "tests": counts,
            "sourceFingerprint": fingerprint, "sourceFiles": count,
            "fingerprintScope": "Legacy selected extensions under Assets/CityGame only; excludes scenes, prefabs, meta, Packages and ProjectSettings",
            "completeBuildInputBinding": False,
            "testXmlSha256": xml_digest,
            "testXmlIdentityBound": receipt.get("testsSha256") == xml_digest,
            "releaseApprovalReady": False,
            "androidHardwareTested": receipt.get("physicalDeviceTested") is True,
            "evidenceLimit": "Historical artifact/subset consistency only. Not a complete build-input or release-approval proof; does not rerun tests, gameplay, signing or listening."}


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    for name in ("project", "tests", "receipt", "artifact"):
        parser.add_argument("--" + name, required=True)
    args = parser.parse_args()
    try:
        print(json.dumps(verify(project_root(args.project), Path(args.tests), Path(args.receipt), Path(args.artifact)), indent=2))
    except (OSError, ValueError, KeyError, ET.ParseError) as error:
        parser.exit(2, "VKT release verification refused: " + str(error) + "\n")


if __name__ == "__main__":
    main()
