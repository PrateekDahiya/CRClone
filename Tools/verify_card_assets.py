"""Independent verification for Agent 4 deliverable 4.2 (Card Database).

Checks, per the task spec:
  1. Assets/Resources/Data/Cards/ holds >= 122 .asset files (no .gitkeep).
  2. Every asset has non-default Type/Rarity/Elixir/HP/Damage
     (valid enum ints; elixir/HP/damage > 0; at least one parsed or
     doc-merged field, i.e. not pure ApplyDefaults output).
  3. AssetValidator naming rules pass on the folder:
     no spaces, no [^A-Za-z0-9_-] in basenames.

Exit 0 on success, 1 with error list otherwise.
"""
import json
import re
import sys
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
CARDS = ROOT / "Assets/Resources/Data/Cards"


def field(text, name):
    m = re.search(rf"^  {name}: (.+)$", text, re.M)
    return m.group(1).strip() if m else None


def unq(v):
    if v.startswith("'") and v.endswith("'"):
        return v[1:-1].replace("''", "'")
    return v


def main():
    errors = []
    files = sorted(CARDS.glob("Card_*.asset"))
    print(f"asset files: {len(files)}")
    if len(files) < 122:
        errors.append(f"only {len(files)} assets, need >= 122")
    if (CARDS / ".gitkeep").exists():
        errors.append(".gitkeep still present")

    seen = set()
    for f in files:
        t = f.read_text(encoding="utf-8")
        if not f.with_suffix(".asset.meta").is_file():
            errors.append(f"{f.name}: missing .asset.meta")
        if re.search(r"[^A-Za-z0-9_\-]", f.stem):
            errors.append(f"{f.name}: naming violation (spaces/special chars)")
        try:
            cid = int(field(t, "cardId"))
            typ = int(field(t, "type"))
            rar = int(field(t, "rarity"))
            elix = int(field(t, "elixirCost"))
            hp = int(field(t, "baseHitpoints"))
            dmg = int(field(t, "baseDamage"))
        except (TypeError, ValueError):
            errors.append(f"{f.name}: unreadable core fields")
            continue
        seen.add(cid)
        if typ not in range(4):
            errors.append(f"{f.name}: bad type {typ}")
        if rar not in range(5):
            errors.append(f"{f.name}: bad rarity {rar}")
        if elix <= 0:
            errors.append(f"{f.name}: elixir <= 0")
        if hp <= 0:
            errors.append(f"{f.name}: HP <= 0")
        if dmg <= 0:
            errors.append(f"{f.name}: damage <= 0")
        try:
            json.loads(unq(field(t, "mechanicsJson")))
        except (TypeError, json.JSONDecodeError):
            errors.append(f"{f.name}: mechanicsJson invalid")
        for req in ("cardName", "spriteId", "portraitId", "spineAssetName",
                    "deploySound", "attackSound", "hitSound", "deathSound"):
            if not unq(field(t, req) or ""):
                errors.append(f"{f.name}: empty {req}")

    missing = sorted(set(range(1, 123)) - seen)
    if missing:
        errors.append(f"missing IDs: {missing}")

    # placeholders must carry real stats, not ApplyDefaults fallbacks
    for n, want_hp, want_dmg in ((68, 216, 140), (102, 1296, None),
                                 (120, 1280, 170), (121, 1728, 200),
                                 (122, 1664, 220), (116, 132, 180)):
        f = next((x for x in files if x.name.startswith(f"Card_{n:03d}_")), None)
        if f is None:
            errors.append(f"Card_{n:03d}_* absent")
            continue
        t = f.read_text(encoding="utf-8")
        hp, dmg = int(field(t, "baseHitpoints")), int(field(t, "baseDamage"))
        if hp != want_hp or (want_dmg is not None and dmg != want_dmg):
            errors.append(f"{f.name}: placeholder stats wrong (hp={hp}, dmg={dmg})")

    if errors:
        print("FAIL:")
        for e in errors:
            print(f" - {e}")
        return 1
    print("PASS: >=122 assets, fields populated, AssetValidator naming clean")
    return 0


if __name__ == "__main__":
    sys.exit(main())
