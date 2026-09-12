"""Offline runner for Assets/Editor/AssetManifestGenerator.cs :: GenerateManifest().

Unity is not available in this environment, so this script mirrors the
generator 1:1 and emits Assets/AssetManifest.csv:
  CardData .assets  -> id=cardId,  type=CardData, rarity, deps=spine/sprite/portrait
  Prefabs           -> id=asset guid, type=Prefab, rarity=folder category,
                       dims="0 tris, 0 textures" (no meshes/textured materials
                       in generated prefabs), deps=renderer shaders + scripts
  Shaders           -> id=asset guid, type=Shader (name parsed from Shader "")
  Textures/Audio/Animations -> none exist yet (art/audio pipeline pending),
                       same as an in-Unity run would emit (zero rows).

GUID policy (documented): prefab/card guids come from their committed
.asset.meta files; shader .metas are created here under the same
md5(repo-relative path) policy used by the card/prefab runners, then used
as IDs - exactly what AssetDatabase.FindAssets/GUIDToAssetPath yields
once Unity imports the project. Sorting (type, name) and CSV quoting
match the generator byte-for-byte in structure. No timestamps are
written, so regeneration is stable.

Usage (from repo root):
    python Tools/run_manifest_generator.py [--check-only]
"""
import hashlib
import re
import sys
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
CARDS_DIR = ROOT / "Assets/Resources/Data/Cards"
PREFABS_DIR = ROOT / "Assets/Prefabs"
SHADERS_DIR = ROOT / "Assets/Shaders"
OUT_CSV = ROOT / "Assets/AssetManifest.csv"

RARITY = {0: "Common", 1: "Rare", 2: "Epic", 3: "Legendary", 4: "Champion"}

# Default-material shaders per renderer class (engine built-ins; assumption
# documented: GetPrefabDependencies reads sharedMaterial.shader.name, and
# generated prefabs use engine-default materials on every renderer).
RENDERER_SHADER = {
    212: "Sprites/Default",   # SpriteRenderer
    110: "Sprites/Default",   # TrailRenderer (Default-Trail)
    109: "Sprites/Default",   # LineRenderer (Default-Line)
    26: "Particles/Standard Unlit",  # ParticleSystemRenderer
}


def guid_of(rel):
    return hashlib.md5(rel.encode()).hexdigest()


def meta_guid(asset_relpath):
    meta = ROOT / (asset_relpath + ".meta")
    if not meta.is_file():
        return None
    m = re.search(r"^guid: ([0-9a-f]{32})$", meta.read_text(encoding="utf-8"), re.M)
    return m.group(1) if m else None


def field(text, name, cast=str):
    m = re.search(rf"^  {name}: (.+)$", text, re.M)
    if not m:
        return None
    v = m.group(1).strip()
    if v.startswith("'") and v.endswith("'"):
        v = v[1:-1].replace("''", "'")
    return cast(v)


def script_classes_for_guid(guid, guid2file):
    rel = guid2file.get(guid)
    if rel is None:
        return []
    src = ROOT / rel.replace(".meta", "")
    return re.findall(r"class (\w+)", src.read_text(encoding="utf-8"))


def collect_cards(rows, guid2file):
    for f in sorted(CARDS_DIR.glob("Card_*.asset")):
        t = f.read_text(encoding="utf-8")
        rel = f"Assets/Resources/Data/Cards/{f.name}"
        rows.append({
            "id": str(field(t, "cardId", int)),
            "name": field(t, "cardName"),
            "type": "CardData",
            "rarity": RARITY[field(t, "rarity", int)],
            "path": rel,
            "dimensions": "N/A",
            "compression": "N/A",
            "mem": 1,
            "deps": f"{field(t, 'spineAssetName')},{field(t, 'spriteId')},{field(t, 'portraitId')}",
        })


def collect_prefabs(rows, guid2file):
    for f in sorted(PREFABS_DIR.rglob("*.prefab")):
        rel = f.relative_to(ROOT).as_posix()
        t = f.read_text(encoding="utf-8")
        gid = meta_guid(rel)
        if gid is None:
            print(f"WARNING: no .meta for {rel}")
            continue
        if "/Units/" in rel:
            category = "Unit"
        elif "/Buildings/" in rel:
            category = "Building"
        elif "/Spells/" in rel:
            category = "Spell"
        elif "/Projectiles/" in rel:
            category = "Projectile"
        elif "/UI/" in rel:
            category = "UI"
        else:
            category = "Unknown"
        # Mirror GetPrefabDependencies: default materials carry no textures,
        # no SkinnedMeshRenderer/MeshRenderer exists -> 0 tris, 0 textures.
        shaders = sorted({RENDERER_SHADER[c] for c in
                          (int(x) for x in re.findall(r"^--- !u!(\d+) &\d+$", t, re.M))
                          if c in RENDERER_SHADER})
        scripts = []
        for g in re.findall(r"m_Script: \{fileID: 11500000, guid: ([0-9a-f]{32}), type: 3\}", t):
            scripts += script_classes_for_guid(g, guid2file)
        deps = ";".join(shaders + sorted(set(scripts)))
        m = re.search(r"^  m_Name: (.*)$", t, re.M)
        rows.append({
            "id": gid,
            "name": m.group(1).strip() if m else f.stem,
            "type": "Prefab",
            "rarity": category,
            "path": rel,
            "dimensions": "0 tris, 0 textures",
            "compression": "N/A",
            "mem": 0,
            "dependencies": deps,
            "deps": deps,
        })


def collect_shaders(rows):
    for f in sorted(SHADERS_DIR.rglob("*.shader")):
        rel = f.relative_to(ROOT).as_posix()
        meta = Path(str(ROOT / rel) + ".meta")
        if not meta.is_file():
            meta.write_text(
                "fileFormatVersion: 2\nguid: " + guid_of(rel) + "\n"
                "ShaderImporter:\n  externalObjects: {}\n  serializedVersion: 2\n"
                "  userData: \n  assetBundleName: \n  assetBundleVariant: \n",
                encoding="utf-8")
        gid = meta_guid(rel)
        head = f.read_text(encoding="utf-8").split("\n")[0]
        m = re.search(r'Shader\s+"([^"]+)"', head)
        rows.append({
            "id": gid,
            "name": m.group(1) if m else f.stem,
            "type": "Shader",
            "rarity": "N/A",
            "path": rel,
            "dimensions": "N/A",
            "compression": "N/A",
            "mem": 0,
            "deps": "N/A",
        })


def to_csv(rows):
    out = ["ID,Name,Type,Rarity,Path,Dimensions,Compression,MemoryEstimateKB,Dependencies"]
    for e in sorted(rows, key=lambda e: (e["type"], e["name"])):
        out.append(f"\"{e['id']}\",\"{e['name']}\",\"{e['type']}\",\"{e['rarity']}\","
                   f"\"{e['path']}\",\"{e['dimensions']}\",\"{e['compression']}\","
                   f"{e['mem']},\"{e['deps']}\"")
    return "\n".join(out) + "\n"


def main():
    check_only = "--check-only" in sys.argv
    guid2file = {}
    for meta in (ROOT / "Assets/Scripts").rglob("*.cs.meta"):
        m = re.search(r"^guid: ([0-9a-f]{32})$", meta.read_text(encoding="utf-8"), re.M)
        if m:
            guid2file[m.group(1)] = str(meta.with_suffix("").relative_to(ROOT)).replace("\\", "/")
    rows = []
    collect_cards(rows, guid2file)
    collect_prefabs(rows, guid2file)
    collect_shaders(rows)
    kinds = {}
    for r in rows:
        kinds[r["type"]] = kinds.get(r["type"], 0) + 1
    print(f"rows: {len(rows)} {kinds}")
    if check_only:
        return 0
    OUT_CSV.write_text(to_csv(rows), encoding="utf-8", newline="")
    print(f"wrote {OUT_CSV.relative_to(ROOT)}")
    return 0


if __name__ == "__main__":
    sys.exit(main())
