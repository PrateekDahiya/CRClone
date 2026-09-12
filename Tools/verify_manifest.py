"""Independent verification for Agent 4 deliverable 4.6 (Asset Manifest).

Checks:
  1. Assets/AssetManifest.csv holds >= 100 data rows.
  2. Every row's path resolves to a file on disk.
  3. Regenerating via Tools/run_manifest_generator.py is byte-stable
     (no churn): second run must produce identical bytes.

Exit 0 on success, 1 with error list otherwise.
"""
import csv
import hashlib
import subprocess
import sys
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
CSV = ROOT / "Assets/AssetManifest.csv"


def main():
    errors = []
    if not CSV.is_file():
        print("FAIL:\n - Assets/AssetManifest.csv missing")
        return 1
    before = CSV.read_bytes()
    with open(CSV, newline="", encoding="utf-8") as fh:
        rows = list(csv.DictReader(fh))
    print(f"rows: {len(rows)}")
    if len(rows) < 100:
        errors.append(f"only {len(rows)} rows, need >= 100")
    for i, r in enumerate(rows, start=2):
        p = (ROOT / r["Path"]) if not Path(r["Path"]).is_absolute() else Path(r["Path"])
        if not p.is_file():
            errors.append(f"line {i}: path does not resolve: {r['Path']}")
        if not r["ID"] or not r["Name"] or not r["Type"]:
            errors.append(f"line {i}: empty ID/Name/Type")

    env = {"PYTHONIOENCODING": "utf-8", "PYTHONDONTWRITEBYTECODE": "1"}
    import os
    env = {**os.environ, **env}
    rc = subprocess.run([sys.executable, "Tools/run_manifest_generator.py"],
                        cwd=ROOT, env=env, capture_output=True, text=True).returncode
    if rc != 0:
        errors.append("regeneration run failed")
    after = CSV.read_bytes()
    if hashlib.md5(before).hexdigest() != hashlib.md5(after).hexdigest():
        errors.append("regeneration churn: bytes differ between runs")
    else:
        print("regen stable: byte-identical")

    if errors:
        print("FAIL:")
        for e in errors[:20]:
            print(f" - {e}")
        return 1
    print("PASS: >=100 rows, all paths resolve, regen stable")
    return 0


if __name__ == "__main__":
    sys.exit(main())
