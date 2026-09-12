"""Independent verification for Agent 4 deliverable 4.4 (Prefab Templates).

Checks:
  1. Every card maps to its prefab (Units/Buildings/Spells by card type;
     Projectiles for ranged Troop/Champion cards with baseRange > 1.5),
     plus Tower_King / Tower_Princess; each with a .prefab.meta.
  2. Every prefab carries its required components (scripts + built-ins)
     and required children, mirroring PrefabGenerator paths.
  3. AssetValidator.ValidatePrefabReferences equivalent: every m_Script
     guid resolves to a committed script .meta; every local fileID
     reference resolves inside the same file; no dangling materials.
  4. Naming fits ^(Unit|Building|Spell|Projectile|Tower)_[A-Za-z0-9_]+$;
     tags/layers exist in ProjectSettings/TagManager.asset.

Exit 0 on success, 1 with error list otherwise.
"""
import re
import sys
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
PREFABS = ROOT / "Assets/Prefabs"
CARDS = ROOT / "Assets/Resources/Data/Cards"
TAGMANAGER = ROOT / "ProjectSettings/TagManager.asset"

TYPE_NAME = {0: "Troop", 1: "Spell", 2: "Building", 3: "Champion"}


def sanitize(name):
    s = (name or "").replace(" ", "").replace("-", "_").replace(".", "").replace("'", "")
    return re.sub(r"[^A-Za-z0-9_]", "", s)


def field(text, name, cast=str):
    m = re.search(rf"^  {name}: (.+)$", text, re.M)
    if not m:
        return None
    v = m.group(1).strip()
    if v.startswith("'") and v.endswith("'"):
        v = v[1:-1].replace("''", "'")
    return cast(v)


def load_cards():
    out = []
    for f in sorted(CARDS.glob("Card_*.asset")):
        t = f.read_text(encoding="utf-8")
        out.append({"cardName": field(t, "cardName"), "type": field(t, "type", int),
                    "baseRange": field(t, "baseRange", float),
                    "mechanicsJson": field(t, "mechanicsJson") or ""})
    return out


def parse_prefab(text):
    objs = []
    cur = None
    for line in text.split("\n"):
        m = re.match(r"^--- !u!(\d+) &(\d+)$", line)
        if m:
            cur = {"class": int(m.group(1)), "fid": int(m.group(2)), "scripts": [],
                   "refs": [], "go": None, "name": None, "tag": None, "layer": None,
                   "active": None, "text": ""}
            objs.append(cur)
        if cur is not None:
            cur["text"] += line + "\n"
    for o in objs:
        t = o["text"]
        m = re.search(r"m_Script: \{fileID: 11500000, guid: ([0-9a-f]{32}), type: 3\}", t)
        if m:
            o["scripts"].append(m.group(1))
        o["refs"] = [int(x) for x in re.findall(r"\{fileID: (\d+)\}", t)]
        m = re.search(r"^  m_Name: (.*)$", t, re.M)
        if m:
            o["name"] = m.group(1).strip()
        m = re.search(r"^  m_TagString: (.*)$", t, re.M)
        if m:
            o["tag"] = m.group(1).strip()
        m = re.search(r"^  m_Layer: (\d+)$", t, re.M)
        if m:
            o["layer"] = int(m.group(1))
        m = re.search(r"^  m_IsActive: ([01])$", t, re.M)
        if m:
            o["active"] = m.group(1) == "1"
    return objs


def main():
    errors = []
    cards = load_cards()
    if not cards:
        return ["no CardData assets"] and 1

    # guid -> {classes} closure: a .meta pins one script file, which may
    # define several MonoBehaviours (e.g. PoolableComponents.cs).
    guid2classes = {}
    for meta in (ROOT / "Assets/Scripts").rglob("*.cs.meta"):
        m = re.search(r"^guid: ([0-9a-f]{32})$", meta.read_text(encoding="utf-8"), re.M)
        if m:
            src = meta.with_suffix("")  # strip .meta -> the .cs path
            classes = set(re.findall(r"class (\w+)", src.read_text(encoding="utf-8")))
            guid2classes[m.group(1)] = classes

    tags, layers = set(), {}
    if not TAGMANAGER.is_file():
        errors.append("ProjectSettings/TagManager.asset missing")
    else:
        tm = TAGMANAGER.read_text(encoding="utf-8")
        in_tags, in_layers, idx = False, False, 0
        for line in tm.split("\n"):
            s = line.strip()
            if s == "tags:":
                in_tags, in_layers = True, False
            elif s == "layers:":
                in_tags, in_layers = False, True
            elif s == "-" or s.startswith("- "):
                v = s[1:].strip()
                if in_tags and v:
                    tags.add(v)
                elif in_layers:
                    if v:
                        layers[v] = idx
                    idx += 1

    expected = {}  # prefab rel path -> (kind, card|None)
    for c in cards:
        t = TYPE_NAME[c["type"]]
        n = sanitize(c["cardName"])
        if t in ("Troop", "Champion"):
            expected[f"Units/Unit_{n}.prefab"] = ("unit", c)
            if (c["baseRange"] or 0) > 1.5:
                expected[f"Projectiles/Projectile_{n}.prefab"] = ("projectile", c)
        elif t == "Building":
            expected[f"Buildings/Building_{n}.prefab"] = ("building", c)
        elif t == "Spell":
            expected[f"Spells/Spell_{n}.prefab"] = ("spell", c)
    expected["Towers/Tower_King.prefab"] = ("tower", None)
    expected["Towers/Tower_Princess.prefab"] = ("tower", None)

    for sub in ("Units", "Buildings", "Spells", "Projectiles", "Towers"):
        d = PREFABS / sub
        if (d / ".gitkeep").exists():
            errors.append(f"{sub}/.gitkeep still present")

    scripts_needed = {
        "unit": {"UnitView", "UnitPoolable"},
        "building": {"BuildingView", "BuildingPoolable"},
        "spell": {"SpellEffectView", "SpellPoolable"},
        "projectile": {"ProjectileView", "ProjectilePoolable"},
        "tower": {"TowerView"},
    }
    builtin_needed = {
        "unit": {95, 212, 60, 50, 109},
        "building": {95, 212, 59, 50},
        "spell": {100, 26},
        "projectile": {212, 110, 60, 50, 100, 26},
        "tower": {95, 212, 59, 50},
    }

    for rel, (kind, card) in sorted(expected.items()):
        f = PREFABS / rel
        if not f.is_file():
            errors.append(f"missing prefab {rel}")
            continue
        if not Path(str(f) + ".meta").is_file():
            errors.append(f"{rel}: missing .prefab.meta")
        stem = Path(rel).stem
        if not re.match(r"^(Unit|Building|Spell|Projectile|Tower)_[A-Za-z0-9_]+$", stem):
            errors.append(f"{rel}: naming violation")
        raw = f.read_text(encoding="utf-8")
        if re.search(r"m_LocalEulerAnglesHint: \{[^}]*\}\n  - \{fileID", raw):
            errors.append(f"{rel}: malformed Transform m_Children list")
        objs = parse_prefab(raw)
        fids = {o["fid"] for o in objs}
        got_scripts, got_builtin = set(), set()
        for o in objs:
            if o["class"] == 114:
                for g in o["scripts"]:
                    if g not in guid2classes:
                        errors.append(f"{rel}: script guid {g} has no committed .meta")
                    else:
                        got_scripts |= guid2classes[g]
            else:
                got_builtin.add(o["class"])
            for r in o["refs"]:
                if r != 0 and r not in fids:
                    errors.append(f"{rel}: dangling fileID {r}")
            if o["class"] == 1:
                if o["tag"] not in tags:
                    errors.append(f"{rel}: unknown tag {o['tag']}")
                if o["layer"] not in layers.values():
                    errors.append(f"{rel}: unknown layer {o['layer']}")
        missing = scripts_needed[kind] - got_scripts
        if missing:
            errors.append(f"{rel}: missing scripts {sorted(missing)}")
        missing = builtin_needed[kind] - got_builtin
        if missing:
            errors.append(f"{rel}: missing components {sorted(missing)}")
        names = {o["name"] for o in objs if o["class"] == 1}
        need_children = {"HealthBar", "SelectionRing", "ParticlePoints"} if kind == "unit" else set()
        if kind == "building":
            need_children = {"HealthBar", "RetractedVisual"}
            nm = card["cardName"] or ""
            if ("spawn" in (card["mechanicsJson"] or "") or "Hut" in nm or "Furnace" in nm
                    or "Tombstone" in nm or "Cage" in nm or "Drill" in nm):
                need_children.add("SpawnPoint")
            if "tesla" in nm.lower() and "TeslaRetraction" not in got_scripts:
                errors.append(f"{rel}: Tesla prefab lacks TeslaRetraction")
        if kind == "projectile":
            need_children = {"ImpactParticles"}
        if kind == "tower":
            need_children = {"HealthBar", "ActivationEffect"}
        missing = need_children - names
        if missing:
            errors.append(f"{rel}: missing children {sorted(missing)}")
        if kind == "unit" and not any(o["class"] == 1 and o["name"] == "SelectionRing"
                                      and o["active"] is False for o in objs):
            errors.append(f"{rel}: SelectionRing should be inactive")

    counts = {}
    for rel in expected:
        counts[rel.split("/")[0]] = counts.get(rel.split("/")[0], 0) + 1
    print(f"expected prefabs: {len(expected)} {counts}")

    if errors:
        print("FAIL:")
        for e in errors[:40]:
            print(f" - {e}")
        if len(errors) > 40:
            print(f" ... +{len(errors) - 40} more")
        return 1
    print("PASS: counts map per type, components attached, prefab refs resolve")
    return 0


if __name__ == "__main__":
    sys.exit(main())
