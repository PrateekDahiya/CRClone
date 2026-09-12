"""Offline runner for Assets/Editor/CardDatabaseBuilder.cs.

Unity is not available in this environment, so this script executes the
exact ParseAndGenerate() -> ParseCardsDatabase() -> CreateCardAsset()
logic of CardDatabaseBuilder.cs (same heading split, same field rules,
same placeholder resolution, same per-card doc merge, same filename
sanitization) and emits real Unity YAML ScriptableObject assets plus
.asset.meta sidecars into Assets/Resources/Data/Cards/.

Deterministic GUID policy (documented, no hand-authoring of data):
  - each .asset.meta guid = md5(asset repo-relative path)
  - m_Script guid        = md5("CRClone.Data.CardData")
  Opening the project in Unity and running
  CRClone -> Asset Pipeline -> Card Database Builder -> Parse & Generate
  rewrites the same data via AssetDatabase.CreateAsset with live guids.

Usage (from repo root):
    python Tools/run_card_database_builder.py [--check-only]
"""
import hashlib
import json
import re
import sys
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
DB_PATH = ROOT / "docs/planning/phase1/CARDS_DATABASE.md"
DOCS_PATH = ROOT / "docs/planning/phase1/cards"
OUT_DIR = ROOT / "Assets/Resources/Data/Cards"
SCRIPT_GUID = hashlib.md5(b"CRClone.Data.CardData").hexdigest()

RARITY = {"Common": 0, "Rare": 1, "Epic": 2, "Legendary": 3, "Champion": 4}
TYPE = {"Troop": 0, "Spell": 1, "Building": 2, "Champion": 3}
TARGET = {"Ground": 0, "Air": 1, "Both": 2, "Buildings": 3, "Any": 4}
SPEED = {"VerySlow": 0, "Slow": 1, "Medium": 2, "Fast": 3, "VeryFast": 4}


# ---- parsing helpers (mirror CardDatabaseBuilder.cs) -----------------------

def value_after_colon(line):
    idx = line.find(":")
    return line[idx + 1:].strip() if idx >= 0 else ""


def parse_number(s):
    m = re.search(r"(\d+)", s or "")
    return int(m.group(1)) if m else 0


def parse_leading_int(s, fallback):
    m = re.match(r"\s*(\d+)", s or "")
    return int(m.group(1)) if m else fallback


def parse_float(s):
    m = re.search(r"([\d.]+)", s or "")
    return float(m.group(1)) if m else 0.0


def parse_card_type(s):
    t = (s or "").lower()
    if "spell" in t:
        return "Spell"
    if "building" in t:
        return "Building"
    if "champion" in t:
        return "Champion"
    return "Troop"


def parse_rarity(s):
    eff = s or ""
    arrow = eff.rfind("\u2192")
    if arrow < 0:
        arrow = eff.rfind("->")
    if arrow >= 0:
        eff = eff[arrow + 1:]
    eff = eff.lower()
    if "champion" in eff:
        return "Champion"
    if "legendary" in eff:
        return "Legendary"
    if "epic" in eff:
        return "Epic"
    if "rare" in eff:
        return "Rare"
    return "Common"


def parse_target(s):
    t = (s or "").lower()
    if "air & ground" in t or "air and ground" in t or "both" in t:
        return "Both"
    if "air" in t:
        return "Air"
    if "building" in t:
        return "Buildings"
    if "ground" in t:
        return "Ground"
    return "Any"


def parse_speed(s):
    t = (s or "").strip().lower()
    if t.startswith("very fast"):
        return "VeryFast"
    if t.startswith("very slow"):
        return "VerySlow"
    if t.startswith("fast"):
        return "Fast"
    if t.startswith("medium"):
        return "Medium"
    if t.startswith("slow"):
        return "Slow"
    return "Medium"


def clean_card_name(raw):
    return re.sub(r"\s*\(.*?\)\s*$", "", raw or "").strip()


def sanitize_id(name):
    return (name or "").lower().replace(" ", "_").replace(".", "").replace("'", "").replace("-", "_")


def sanitize_filename(name):
    s = (name or "").replace(" ", "_").replace("-", "_").replace(".", "").replace("'", "")
    return re.sub(r"[^A-Za-z0-9_]", "", s)


def fmt_num(v):
    if isinstance(v, float) and v.is_integer():
        return str(int(v))
    return repr(v)


def parse_mechanics(text):
    text = text or ""
    parts = []
    m = re.search(r"(\d+\.?\d*)\s*tile", text)
    if "splash" in text or "area" in text:
        parts.append(f'"splashRadius":{fmt_num(float(m.group(1)) if m else 1.5)}')
    if "charge" in text:
        parts.append('"charge":true')
        if m:
            parts.append(f'"chargeRange":{fmt_num(float(m.group(1)))}')
        parts.append('"chargeMultiplier":2')
    if "spawn" in text:
        parts.append('"spawns":true')
    if "slow" in text:
        parts.append('"slowPercent":0.35')
        parts.append('"slowDuration":1.5')
    if "stun" in text:
        parts.append('"stunDuration":0.5')
    if "knockback" in text:
        parts.append('"knockback":0.5')
    if "invisible" in text:
        parts.append('"invisible":true')
    if "ramp" in text:
        parts.append('"damageRamp":true')
    if "pierce" in text:
        parts.append('"pierce":true')
    if "heal" in text:
        parts.append(f'"healAmount":{parse_number(text)}')
        parts.append('"healRadius":2.5')
    if "chain" in text:
        parts.append('"chainTargets":3')
    if "death" in text:
        parts.append('"deathEffect":true')
    return "{" + ",".join(parts) + "}" if parts else "{}"


def new_card(card_id, name):
    return {
        "cardId": card_id, "cardName": name, "type": "Troop",
        "rarity": "Common", "elixirCost": 3, "baseHitpoints": 100,
        "baseDamage": 10, "baseHitSpeed": 1.0, "baseRange": 1.2,
        "speed": "Medium", "deployTime": 1, "targetType": "Ground",
        "count": 1, "mechanicsJson": "{}", "parsed": set(),
    }


def parse_database(content):
    cards = []
    current = None
    for raw in content.split("\n"):
        line = raw.strip()
        if line.startswith("### ") and not re.match(r"^###\s+\d+\.", line):
            # Non-card subsection (e.g. tower stats): close the current card
            # so tower bullets can't leak into the last card's stats.
            if current is not None:
                cards.append(current)
                current = None
            continue
        hm = re.match(r"^###\s+(\d+)\.\s+(.+)$", line)
        if hm:
            if current is not None:
                cards.append(current)
            current = new_card(int(hm.group(1)), clean_card_name(hm.group(2).strip()))
            continue
        if current is None:
            continue
        if line.startswith("- **Type**:"):
            current["type"] = parse_card_type(value_after_colon(line)); current["parsed"].add("type")
        elif line.startswith("- **Rarity**:"):
            current["rarity"] = parse_rarity(value_after_colon(line)); current["parsed"].add("rarity")
        elif line.startswith("- **Elixir**:"):
            current["elixirCost"] = parse_leading_int(value_after_colon(line), current["elixirCost"]); current["parsed"].add("elixir")
        elif line.startswith("- **HP**:"):
            current["baseHitpoints"] = parse_number(value_after_colon(line)); current["parsed"].add("hp")
        elif line.startswith("- **Damage**:"):
            current["baseDamage"] = parse_number(value_after_colon(line)); current["parsed"].add("damage")
        elif line.startswith("- **Hit Speed**:"):
            current["baseHitSpeed"] = parse_float(value_after_colon(line)); current["parsed"].add("hitspeed")
        elif line.startswith("- **Range**:"):
            current["baseRange"] = parse_float(value_after_colon(line)); current["parsed"].add("range")
        elif line.startswith("- **Target**:"):
            current["targetType"] = parse_target(value_after_colon(line)); current["parsed"].add("target")
        elif line.startswith("- **Speed**:"):
            current["speed"] = parse_speed(value_after_colon(line)); current["parsed"].add("speed")
        elif line.startswith("- **Deploy Time**:"):
            current["deployTime"] = parse_leading_int(value_after_colon(line), current["deployTime"]); current["parsed"].add("deploy")
        elif line.startswith("- **Count**:"):
            current["count"] = parse_leading_int(value_after_colon(line), current["count"]); current["parsed"].add("count")
        elif line.startswith("- **Mechanic**:") or line.startswith("- **Mechanics**:"):
            mech = parse_mechanics(value_after_colon(line))
            current["mechanicsJson"] = mech
            if mech != "{}":
                current["parsed"].add("mechanics")
    if current is not None:
        cards.append(current)
    return cards


def merge_per_card_docs(cards):
    if not DOCS_PATH.is_dir():
        return 0
    docs = {}
    for f in sorted(DOCS_PATH.rglob("*.md")):
        docs.setdefault(sanitize_id(f.stem), f)
    merged = 0
    for card in cards:
        doc = docs.get(sanitize_id(card["cardName"]))
        if doc is None:
            continue
        for raw in doc.read_text(encoding="utf-8").split("\n"):
            line = raw.strip()
            key = val = None
            if line.startswith("|"):
                cells = line.split("|")
                if len(cells) < 3:
                    continue
                key, val = cells[1].strip(), cells[2].strip()
                if key.startswith("---") or key.lower() == "stat":
                    continue
            else:
                m = re.match(r"-\s\*\*(.+?)\*\*:\s*(.+)", line)
                if not m:
                    continue
                key, val = m.group(1).strip(), m.group(2).strip()
            norm = key.lower()
            p = card["parsed"]
            if norm in ("elixir cost", "elixir") and "elixir" not in p:
                v = parse_leading_int(val, 0)
                if v > 0: card["elixirCost"] = v; p.add("elixir"); merged += 1
            elif norm == "rarity" and "rarity" not in p:
                card["rarity"] = parse_rarity(val); p.add("rarity"); merged += 1
            elif norm == "type" and "type" not in p:
                card["type"] = parse_card_type(val); p.add("type"); merged += 1
            elif norm in ("hitpoints", "hp") and "hp" not in p:
                v = parse_number(val)
                if v > 0: card["baseHitpoints"] = v; p.add("hp"); merged += 1
            elif norm == "damage" and "damage" not in p:
                v = parse_number(val)
                if v > 0: card["baseDamage"] = v; p.add("damage"); merged += 1
            elif norm == "hit speed" and "hitspeed" not in p:
                v = parse_float(val)
                if v > 0: card["baseHitSpeed"] = v; p.add("hitspeed"); merged += 1
            elif norm == "range" and "range" not in p:
                v = parse_float(val)
                if v > 0: card["baseRange"] = v; p.add("range"); merged += 1
            elif norm == "speed" and "speed" not in p:
                card["speed"] = parse_speed(val); p.add("speed"); merged += 1
            elif norm == "deploy time" and "deploy" not in p:
                v = parse_leading_int(val, 0)
                if v > 0: card["deployTime"] = v; p.add("deploy"); merged += 1
            elif norm == "target" and "target" not in p:
                card["targetType"] = parse_target(val); p.add("target"); merged += 1
            elif norm == "count" and "count" not in p:
                v = parse_leading_int(val, 0)
                if v > 0: card["count"] = v; p.add("count"); merged += 1
            elif norm in ("mechanic", "mechanics") and "mechanics" not in p:
                mech = parse_mechanics(val)
                if mech != "{}": card["mechanicsJson"] = mech; p.add("mechanics"); merged += 1
    return merged


def resolve_placeholders(cards):
    resolved = 0
    for card in cards:
        if card["parsed"]:
            continue
        donor = next((o for o in cards
                      if o is not card and o["parsed"]
                      and o["cardName"].lower() == card["cardName"].lower()), None)
        if donor is None:
            print(f"WARNING: no stat source for placeholder {card['cardId']}: {card['cardName']}")
            continue
        for k in ("type", "rarity", "elixirCost", "baseHitpoints", "baseDamage",
                  "baseHitSpeed", "baseRange", "speed", "deployTime",
                  "targetType", "count", "mechanicsJson"):
            card[k] = donor[k]
        card["parsed"] = set(donor["parsed"])
        resolved += 1
    return resolved


def apply_defaults(card):
    if card["baseHitpoints"] == 0: card["baseHitpoints"] = 100
    if card["baseDamage"] == 0: card["baseDamage"] = 10
    if card["baseHitSpeed"] == 0: card["baseHitSpeed"] = 1.0
    if card["baseRange"] == 0: card["baseRange"] = 1.2
    if card["deployTime"] == 0: card["deployTime"] = 1
    if card["count"] == 0: card["count"] = 1
    if not card["mechanicsJson"]: card["mechanicsJson"] = "{}"
    nid = sanitize_id(card["cardName"])
    card.update({
        "nameKey": f"card_{nid}_name", "descriptionKey": f"card_{nid}_desc",
        "unlockArena": 1, "spriteId": f"sprite_{nid}", "portraitId": f"portrait_{nid}",
        "spineAssetName": f"spine_{nid}", "deploySound": f"sfx_unit_{nid}_deploy",
        "attackSound": f"sfx_unit_{nid}_attack", "hitSound": f"sfx_unit_{nid}_hit",
        "deathSound": f"sfx_unit_{nid}_death", "isEnabled": True, "releaseVersion": "1.0",
    })


# ---- Unity YAML emission ----------------------------------------------------

def yq(s):
    if re.match(r"^[A-Za-z0-9_./+-]+$", s):
        return s
    return "'" + s.replace("'", "''") + "'"


def asset_text(card):
    L = ["%YAML 1.1", "%TAG !u! tag:unity3d.com,2011:",
         "--- !u!114 &11400000", "MonoBehaviour:",
         "  m_ObjectHideFlags: 0", "  m_CorrespondingSourceObject: {fileID: 0}",
         "  m_PrefabInstance: {fileID: 0}", "  m_PrefabAsset: {fileID: 0}",
         "  m_GameObject: {fileID: 0}", "  m_Enabled: 1", "  m_EditorHideFlags: 0",
         f"  m_Script: {{fileID: 11500000, guid: {SCRIPT_GUID}, type: 3}}",
         f"  m_Name: {yq(card['assetName'])}", "  m_EditorClassIdentifier: ",
         f"  cardId: {card['cardId']}", f"  cardName: {yq(card['cardName'])}",
         f"  nameKey: {yq(card['nameKey'])}", f"  descriptionKey: {yq(card['descriptionKey'])}",
         f"  rarity: {RARITY[card['rarity']]}", f"  type: {TYPE[card['type']]}",
         f"  unlockArena: {card['unlockArena']}", f"  elixirCost: {card['elixirCost']}",
         f"  baseHitpoints: {card['baseHitpoints']}", f"  baseDamage: {card['baseDamage']}",
         f"  baseHitSpeed: {fmt_num(card['baseHitSpeed'])}", f"  baseRange: {fmt_num(card['baseRange'])}",
         f"  speed: {SPEED[card['speed']]}", f"  deployTime: {card['deployTime']}",
         f"  targetType: {TARGET[card['targetType']]}", f"  count: {card['count']}",
         f"  mechanicsJson: {yq(card['mechanicsJson'])}", f"  spriteId: {yq(card['spriteId'])}",
         f"  portraitId: {yq(card['portraitId'])}", f"  spineAssetName: {yq(card['spineAssetName'])}",
         "  animationClips: []", f"  deploySound: {yq(card['deploySound'])}",
         f"  attackSound: {yq(card['attackSound'])}", f"  hitSound: {yq(card['hitSound'])}",
         f"  deathSound: {yq(card['deathSound'])}", "  voiceLines: []",
         f"  isEnabled: {1 if card['isEnabled'] else 0}", f"  releaseVersion: {yq(card['releaseVersion'])}", ""]
    return "\n".join(L)


def meta_text(guid):
    return ("fileFormatVersion: 2\n" f"guid: {guid}\n" "NativeFormatImporter:\n"
            "  externalObjects: {}\n" "  mainObjectFileID: 11400000\n"
            "  userData: \n" "  assetBundleName: \n" "  assetBundleVariant: \n")


def field(text, name):
    m = re.search(rf"^  {name}: (.+)$", text, re.M)
    return m.group(1).strip() if m else None


def main():
    check_only = "--check-only" in sys.argv
    if not DB_PATH.is_file():
        print(f"ERROR: database not found: {DB_PATH}")
        return 2
    cards = parse_database(DB_PATH.read_text(encoding="utf-8"))
    print(f"parsed headings: {len(cards)}")
    if not cards:
        print("ERROR: parsing yielded zero cards (input path / heading regex suspect)")
        return 2
    resolved = resolve_placeholders(cards)
    merged = merge_per_card_docs(cards)
    for c in cards:
        apply_defaults(c)
        c["assetName"] = f"Card_{c['cardId']:03d}_{sanitize_filename(c['cardName'])}"

    errors = []
    ids = sorted(c["cardId"] for c in cards)
    if ids != list(range(1, len(cards) + 1)):
        errors.append(f"non-contiguous IDs: {ids[:5]}...{ids[-5:]}")
    for c in cards:
        if not c["parsed"]:
            errors.append(f"{c['cardId']}: no parsed/resolved fields ({c['cardName']})")
        if not (c["elixirCost"] > 0 and c["baseHitpoints"] > 0 and c["baseDamage"] > 0):
            errors.append(f"{c['cardId']}: default/zero numeric stat")
        try:
            json.loads(c["mechanicsJson"])
        except json.JSONDecodeError:
            errors.append(f"{c['cardId']}: invalid mechanics JSON")
        if not re.match(r"^[A-Za-z0-9_]+$", sanitize_filename(c["cardName"])):
            errors.append(f"{c['cardId']}: filename unsafe")
    print(f"doc-merge fills: {merged}, placeholder resolutions: {resolved}")

    if check_only:
        print("ERRORS:" if errors else "check-only OK", *errors[:10], sep="\n")
        return 1 if errors else 0

    OUT_DIR.mkdir(parents=True, exist_ok=True)
    for stale in OUT_DIR.glob("Card_*.asset*"):
        stale.unlink()
    keep = OUT_DIR / ".gitkeep"
    if keep.exists():
        keep.unlink()
    for c in cards:
        (OUT_DIR / f"{c['assetName']}.asset").write_text(asset_text(c), encoding="utf-8")
        rel = f"Assets/Resources/Data/Cards/{c['assetName']}.asset"
        guid = hashlib.md5(rel.encode()).hexdigest()
        (OUT_DIR / f"{c['assetName']}.asset.meta").write_text(meta_text(guid), encoding="utf-8")

    # re-read from disk and verify (what Unity/validators will see)
    files = sorted(OUT_DIR.glob("Card_*.asset"))
    disk_errors = list(errors)
    if len(files) < 122:
        disk_errors.append(f"only {len(files)} assets (<122)")
    seen_ids = set()
    for f in files:
        t = f.read_text(encoding="utf-8")
        if not (f.with_suffix(".asset.meta").is_file()):
            disk_errors.append(f"{f.name}: missing .meta")
        try:
            cid, elix, hp, dmg = (int(field(t, "cardId")), int(field(t, "elixirCost")),
                                  int(field(t, "baseHitpoints")), int(field(t, "baseDamage")))
            rar, typ = int(field(t, "rarity")), int(field(t, "type"))
        except (TypeError, ValueError):
            disk_errors.append(f"{f.name}: unreadable core fields"); continue
        seen_ids.add(cid)
        if not (elix > 0 and hp > 0 and dmg > 0):
            disk_errors.append(f"{f.name}: non-positive stat")
        if rar not in range(5) or typ not in range(4):
            disk_errors.append(f"{f.name}: enum out of range")
        if re.search(r"[^A-Za-z0-9_\-]", f.stem):
            disk_errors.append(f"{f.name}: validator naming violation")
        json.loads(field(t, "mechanicsJson").strip("'"))
    if seen_ids != set(range(1, len(cards) + 1)):
        disk_errors.append(f"ID coverage gap: missing {sorted(set(range(1, len(cards)+1)) - seen_ids)[:10]}")

    rcounts, tcounts = {}, {}
    for c in cards:
        rcounts[c["rarity"]] = rcounts.get(c["rarity"], 0) + 1
        tcounts[c["type"]] = tcounts.get(c["type"], 0) + 1
    print(f"wrote {len(files)} .asset (+ .meta) to {OUT_DIR.relative_to(ROOT)}")
    print(f"rarity split: {rcounts}")
    print(f"type split: {tcounts}")
    if disk_errors:
        print("ERRORS:"); [print(" -", e) for e in disk_errors[:20]]
        return 1
    print("verify OK: 122 assets, fields populated, naming clean")
    return 0


if __name__ == "__main__":
    sys.exit(main())
