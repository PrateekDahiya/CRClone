"""Offline runner for Assets/Editor/PrefabGenerator.cs :: GenerateAllPrefabs().

Unity is not available in this environment, so this script executes the
(reconciled) generator logic 1:1 and emits real Unity YAML .prefab files
plus .prefab.meta sidecars:
  Troop/Champion cards -> Assets/Prefabs/Units/Unit_<Name>.prefab
  Building cards       -> Assets/Prefabs/Buildings/Building_<Name>.prefab
  Spell cards          -> Assets/Prefabs/Spells/Spell_<Name>.prefab
  Ranged troops        -> Assets/Prefabs/Projectiles/Projectile_<Name>.prefab
  GameConfig towers    -> Assets/Prefabs/Towers/Tower_{King,Princess}.prefab

Fidelity notes (see PrefabGenerator.cs comments):
  - Simulation classes (Unit/Building/SpellEffect/Projectile/Tower) are
    plain C# classes, so no sim components are attached; runtime stats
    resolve from CardData via view Initialize() methods.
  - View cross-references are written (what SerializedObject wiring does
    in-Unity). Animator m_Controller is null offline; the in-Unity run
    creates .controller assets via Create*AnimatorController().
  - No custom materials are assigned (a runtime `new Material()` is not an
    asset and cannot ship in a prefab); renderers use engine defaults.
  - Prefab identity is name-based (PoolManager.Spawn("Unit_Knight")),
    so duplicate card rows (e.g. IDs 2/68 Princess) share one prefab;
    the first card wins (duplicates carry identical resolved stats).
  - Projectile rule mirrors Unit.PerformAttack: baseRange > 1.5.
  - GUID policy: prefab .meta guid = md5(repo-relative prefab path);
    m_Script guid = md5(script repo-relative path). The runner also writes
    the 9 Presentation script .meta sidecars so every script reference in
    the prefabs resolves (ValidatePrefabReferences closure).

Usage (from repo root):
    python Tools/run_prefab_generator.py [--check-only]
"""
import hashlib
import re
import sys
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
CARDS_DIR = ROOT / "Assets/Resources/Data/Cards"
PREFABS_DIR = ROOT / "Assets/Prefabs"
PRESENT = "Assets/Scripts/Battle/Presentation"

LAYERS = {"Unit": 8, "Building": 9, "Spell": 10, "Projectile": 11, "Tower": 12}

SCRIPTS = {
    "UnitView": f"{PRESENT}/UnitView.cs",
    "BuildingView": f"{PRESENT}/BuildingView.cs",
    "SpellEffectView": f"{PRESENT}/SpellEffectView.cs",
    "ProjectileView": f"{PRESENT}/ProjectileView.cs",
    "TowerView": f"{PRESENT}/TowerView.cs",
    "HealthBar": f"{PRESENT}/HealthBar.cs",
    "SelectionRing": f"{PRESENT}/SelectionRing.cs",
    "TeslaRetraction": f"{PRESENT}/TeslaRetraction.cs",
    "UnitPoolable": f"{PRESENT}/PoolableComponents.cs",
    "BuildingPoolable": f"{PRESENT}/PoolableComponents.cs",
    "SpellPoolable": f"{PRESENT}/PoolableComponents.cs",
    "ProjectilePoolable": f"{PRESENT}/PoolableComponents.cs",
}
SCRIPT_GUID = {k: hashlib.md5(v.encode()).hexdigest() for k, v in SCRIPTS.items()}


def guid_of(path_rel):
    return hashlib.md5(path_rel.encode()).hexdigest()


def sanitize_prefab_name(name):
    s = (name or "").replace(" ", "").replace("-", "_").replace(".", "").replace("'", "")
    return re.sub(r"[^A-Za-z0-9_]", "", s)


def fmt(v):
    if isinstance(v, float) and v.is_integer():
        return str(int(v))
    return repr(v)


def col(r, g, b, a=1):
    return f"{{r: {fmt(r)}, g: {fmt(g)}, b: {fmt(b)}, a: {fmt(a)}}}"


WHITE = col(1, 1, 1)
YELLOW = col(1, 0.92156863, 0.01568628)


def spell_color(name):
    n = name or ""
    if "Fire" in n:
        return (1, 0, 0, 1)
    if "Ice" in n or "Freeze" in n:
        return (0, 1, 1, 1)
    if "Lightning" in n or "Zap" in n:
        return (1, 0.92156863, 0.01568628, 1)
    if "Poison" in n:
        return (0, 1, 0, 1)
    if "Tornado" in n:
        return (0.5, 0.5, 0.5, 1)
    if "Log" in n:
        return (0.6, 0.4, 0.2, 1)
    return (1, 1, 1, 1)


def spell_gradient(name):
    n = (name or "").lower()
    if "fire" in n:
        return ([(1, 0, 0, 0.0), (1, 0.92156863, 0.01568628, 0.5), (1, 1, 1, 1.0)],
                [(1, 0.0), (0, 1.0)])
    if "ice" in n or "freeze" in n:
        return ([(0, 1, 1, 0.0), (0, 0, 1, 1.0)], [(1, 0.0), (0, 1.0)])
    if "lightning" in n or "zap" in n:
        return ([(1, 0.92156863, 0.01568628, 0.0), (1, 1, 1, 1.0)], [(1, 0.0), (0, 1.0)])
    if "poison" in n:
        return ([(0, 1, 0, 0.0), (0.5, 0.8, 0.2, 1.0)], [(0.5, 0.0), (0, 1.0)])
    return None


# ---- card loading (mirrors Resources.LoadAll<CardData>) --------------------

def _field(text, name, cast=str):
    m = re.search(rf"^  {name}: (.+)$", text, re.M)
    if not m:
        return None
    v = m.group(1).strip()
    if v.startswith("'") and v.endswith("'"):
        v = v[1:-1].replace("''", "'")
    return cast(v)


def load_cards():
    cards = []
    for f in sorted(CARDS_DIR.glob("Card_*.asset")):
        t = f.read_text(encoding="utf-8")
        cards.append({
            "file": f.name,
            "cardId": _field(t, "cardId", int),
            "cardName": _field(t, "cardName"),
            "rarity": _field(t, "rarity", int),
            "type": _field(t, "type", int),
            "elixirCost": _field(t, "elixirCost", int),
            "baseHitpoints": _field(t, "baseHitpoints", int),
            "baseDamage": _field(t, "baseDamage", int),
            "baseHitSpeed": _field(t, "baseHitSpeed", float),
            "baseRange": _field(t, "baseRange", float),
            "speed": _field(t, "speed", int),
            "deployTime": _field(t, "deployTime", int),
            "targetType": _field(t, "targetType", int),
            "count": _field(t, "count", int),
            "mechanicsJson": _field(t, "mechanicsJson"),
        })
    return cards


TYPE_NAME = {0: "Troop", 1: "Spell", 2: "Building", 3: "Champion"}


# ---- prefab object model ----------------------------------------------------

class Prefab:
    def __init__(self, name, tag, layer):
        self.name = name
        self.tag = tag
        self.layer = layer
        self.objs = []  # (class_id, kind, lines|callable)
        self._fids = {}
        self._used = set()

    def fid(self, role):
        base = int(hashlib.md5(f"{self.name}|{role}".encode()).hexdigest()[:15], 16)
        fid = base or 1
        while fid in self._used:
            fid += 1
        self._used.add(fid)
        return fid

    def add(self, class_id, kind, body):
        fid = self.fid(f"{kind}:{len(self.objs)}")
        self.objs.append((fid, class_id, kind, body))
        return fid

    def finalize_transforms(self, specs):
        for i, (fid, class_id, kind, body) in enumerate(self.objs):
            if class_id == 4 and fid in specs:
                s = specs[fid]
                self.objs[i] = (fid, class_id, kind,
                                tr_block(self, fid, s["go"], s["pos"], s["scale"],
                                         s["kids"], s["father"]))

    def text(self):
        out = ["%YAML 1.1", "%TAG !u! tag:unity3d.com,2011:"]
        for fid, class_id, kind, body in self.objs:
            out.append(f"--- !u!{class_id} &{fid}")
            out.append(body() if callable(body) else body)
        out.append("")
        return "\n".join(out)


def ref(fid):
    return "{fileID: 0}" if not fid else f"{{fileID: {fid}}}"


def go_block(p, go_fid, name, tag, layer, active):
    return (f"GameObject:\n  m_ObjectHideFlags: 0\n"
            f"  m_CorrespondingSourceObject: {{fileID: 0}}\n  m_PrefabInstance: {{fileID: 0}}\n"
            f"  m_PrefabAsset: {{fileID: 0}}\n  m_GameObject: {{fileID: 0}}\n  m_Enabled: 1\n"
            f"  m_EditorHideFlags: 0\n  m_Script: {{fileID: 0}}\n  m_Name: {name}\n"
            f"  m_TagString: {tag}\n  m_Icon: {{fileID: 0}}\n  m_NavMeshLayer: 0\n"
            f"  m_StaticEditorFlags: 0\n  m_Layer: {layer}\n  m_IsActive: {1 if active else 0}")


def tr_block(p, tr_fid, go_fid, pos, scale, children, father):
    kids = "".join(f"\n  - {{fileID: {c}}}" for c in children) if children else " []"
    return (f"Transform:\n  m_ObjectHideFlags: 0\n"
            f"  m_CorrespondingSourceObject: {{fileID: 0}}\n  m_PrefabInstance: {{fileID: 0}}\n"
            f"  m_PrefabAsset: {{fileID: 0}}\n  m_GameObject: {{fileID: {go_fid}}}\n"
            f"  m_LocalRotation: {{x: 0, y: 0, z: 0, w: 1}}\n"
            f"  m_LocalPosition: {{x: {fmt(pos[0])}, y: {fmt(pos[1])}, z: {fmt(pos[2])}}}\n"
            f"  m_LocalScale: {{x: {fmt(scale[0])}, y: {fmt(scale[1])}, z: {fmt(scale[2])}}}\n"
            f"  m_ConstrainProportionsScale: 0\n  m_Children:{kids}\n"
            f"  m_Father: {ref(father)}\n  m_LocalEulerAnglesHint: {{x: 0, y: 0, z: 0}}")


def mb_block(p, go_fid, script, fields):
    lines = [f"MonoBehaviour:", f"  m_ObjectHideFlags: 0",
             f"  m_CorrespondingSourceObject: {{fileID: 0}}", f"  m_PrefabInstance: {{fileID: 0}}",
             f"  m_PrefabAsset: {{fileID: 0}}", f"  m_GameObject: {{fileID: {go_fid}}}",
             f"  m_Enabled: 1", f"  m_EditorHideFlags: 0",
             f"  m_Script: {{fileID: 11500000, guid: {SCRIPT_GUID[script]}, type: 3}}",
             f"  m_Name: ", f"  m_EditorClassIdentifier: "]
    lines += [f"  {k}: {v}" for k, v in fields]
    return "\n".join(lines)


def renderer_base(go_fid, sorting):
    return (f"  m_CastShadows: 0\n  m_ReceiveShadows: 0\n  m_DynamicOccludee: 1\n"
            f"  m_StaticShadowCaster: 0\n  m_MotionVectors: 1\n  m_LightProbeUsage: 0\n"
            f"  m_ReflectionProbeUsage: 0\n  m_RayTracingMode: 0\n  m_RayTraceProcedural: 0\n"
            f"  m_RenderingLayerMask: 1\n  m_Materials:\n  - {{fileID: 0}}\n"
            f"  m_StaticBatchInfo:\n    firstSubMesh: 0\n    subMeshCount: 0\n"
            f"  m_StaticBatchRoot: {{fileID: 0}}\n  m_ProbeAnchor: {{fileID: 0}}\n"
            f"  m_LightProbeVolumeOverride: {{fileID: 0}}\n  m_ScaleInLightmap: 1\n"
            f"  m_ReceiveGI: 1\n  m_PreserveUVs: 0\n  m_IgnoreNormalsForChartDetection: 0\n"
            f"  m_SortingLayerID: 0\n  m_SortingLayer: 0\n  m_SortingOrder: {sorting}")


def sprite_block(go_fid, sorting):
    return ("SpriteRenderer:\n  m_ObjectHideFlags: 0\n  m_CorrespondingSourceObject: {fileID: 0}\n"
            "  m_PrefabInstance: {fileID: 0}\n  m_PrefabAsset: {fileID: 0}\n"
            f"  m_GameObject: {{fileID: {go_fid}}}\n  m_Enabled: 1\n  m_EditorHideFlags: 0\n"
            + renderer_base(go_fid, sorting) +
            "\n  m_Sprite: {fileID: 0}\n  m_Color: " + WHITE +
            "\n  m_FlipX: 0\n  m_FlipY: 0\n  m_DrawMode: 0\n  m_Size: {x: 1, y: 1}\n"
            "  m_AdaptiveModeThreshold: 0.5\n  m_SpriteTileMode: 0\n  m_WasSpriteAssigned: 0\n"
            "  m_MaskInteraction: 0\n  m_SpriteSortPoint: 0")


def animator_block(go_fid):
    return ("Animator:\n  m_ObjectHideFlags: 0\n  m_CorrespondingSourceObject: {fileID: 0}\n"
            "  m_PrefabInstance: {fileID: 0}\n  m_PrefabAsset: {fileID: 0}\n"
            f"  m_GameObject: {{fileID: {go_fid}}}\n  m_Enabled: 1\n  m_EditorHideFlags: 0\n"
            "  m_Avatar: {fileID: 0}\n  m_Controller: {fileID: 0}\n  m_CullingMode: 0\n"
            "  m_UpdateMode: 0\n  m_ApplyRootMotion: 0\n  m_LinearVelocityBlending: 0\n"
            "  m_WarningMessage: \n  m_HasTransformHierarchy: 1\n"
            "  m_AllowConstantClipSamplingOptimization: 1\n  m_KeepAnimatorControllerStateOnDisable: 0")


def circle_collider(go_fid, radius):
    return ("CircleCollider2D:\n  m_ObjectHideFlags: 0\n  m_CorrespondingSourceObject: {fileID: 0}\n"
            "  m_PrefabInstance: {fileID: 0}\n  m_PrefabAsset: {fileID: 0}\n"
            f"  m_GameObject: {{fileID: {go_fid}}}\n  m_Enabled: 1\n  m_EditorHideFlags: 0\n"
            "  m_Density: 1\n  m_Material: {fileID: 0}\n  m_IsTrigger: 1\n"
            "  m_UsedByEffector: 0\n  m_UsedByComposite: 0\n  m_Offset: {x: 0, y: 0}\n"
            f"  m_Radius: {fmt(radius)}")


def box_collider(go_fid, sx, sy):
    return ("BoxCollider2D:\n  m_ObjectHideFlags: 0\n  m_CorrespondingSourceObject: {fileID: 0}\n"
            "  m_PrefabInstance: {fileID: 0}\n  m_PrefabAsset: {fileID: 0}\n"
            f"  m_GameObject: {{fileID: {go_fid}}}\n  m_Enabled: 1\n  m_EditorHideFlags: 0\n"
            "  m_Density: 1\n  m_Material: {fileID: 0}\n  m_IsTrigger: 1\n"
            "  m_UsedByEffector: 0\n  m_UsedByComposite: 0\n  m_Offset: {x: 0, y: 0}\n"
            f"  m_Size: {{x: {fmt(sx)}, y: {fmt(sy)}}}\n  m_EdgeRadius: 0\n  m_AutoTiling: 0")


def rigidbody2d(go_fid):
    return ("Rigidbody2D:\n  m_ObjectHideFlags: 0\n  m_CorrespondingSourceObject: {fileID: 0}\n"
            "  m_PrefabInstance: {fileID: 0}\n  m_PrefabAsset: {fileID: 0}\n"
            f"  m_GameObject: {{fileID: {go_fid}}}\n  m_Enabled: 1\n  m_EditorHideFlags: 0\n"
            "  m_BodyType: 1\n  m_Simulated: 1\n  m_UseFullKinematicContacts: 0\n"
            "  m_UseAutoMass: 0\n  m_Mass: 1\n  m_LinearDrag: 0\n  m_AngularDrag: 0.05\n"
            "  m_GravityScale: 0\n  m_Material: {fileID: 0}\n  m_Constraints: 0")


def gradient_block(color_keys, alpha_keys):
    out = ["    m_Gradient:", "      serializedVersion: 2", "      m_Mode: 0",
           "      m_ColorSpace: 0", f"      m_NumColorKeys: {len(color_keys)}",
           f"      m_NumAlphaKeys: {len(alpha_keys)}", "      m_ColorKeys:"]
    for r, g, b, t in color_keys:
        out += [f"      - r: {fmt(r)}", f"        g: {fmt(g)}", f"        b: {fmt(b)}",
                f"        a: 1", f"        t: {fmt(t)}"]
    out.append("      m_AlphaKeys:")
    for a, t in alpha_keys:
        out += [f"      - a: {fmt(a)}", f"        t: {fmt(t)}"]
    return "\n".join(out)


def particle_system(go_fid, duration=1.0, loop=0, lifetime=1.0, speed=5.0,
                    size=0.5, color=(1, 1, 1, 1), gravity=0.0, rate=50.0,
                    gradient=None):
    r, g, b, a = color
    lines = ["ParticleSystem:", "  m_ObjectHideFlags: 0",
             "  m_CorrespondingSourceObject: {fileID: 0}", "  m_PrefabInstance: {fileID: 0}",
             "  m_PrefabAsset: {fileID: 0}", f"  m_GameObject: {{fileID: {go_fid}}}",
             "  m_Enabled: 1", "  m_EditorHideFlags: 0", "  m_LengthInSec: 5",
             "  m_SimulationSpeed: 1", "  m_PlayOnAwake: 0",
             "  m_MainModule:", "    serializedVersion: 2",
             f"    m_Duration: {fmt(duration)}", f"    m_Loop: {loop}",
             "    m_PreInfinity: 1", "    m_PostInfinity: 1",
             "    m_StartDelay: 0", "    m_StartDelayMultiplier: 1",
             f"    m_StartLifetime: {fmt(lifetime)}", "    m_StartLifetimeMultiplier: 1",
             f"    m_StartSpeed: {fmt(speed)}", "    m_StartSpeedMultiplier: 1",
             f"    m_StartSize: {fmt(size)}", "    m_StartSizeMultiplier: 1",
             f"    m_StartSizeY: {fmt(size)}", "    m_StartSizeYMultiplier: 1",
             f"    m_StartSizeZ: {fmt(size)}", "    m_StartSizeZMultiplier: 1",
             "    m_StartRotation: 0", "    m_StartRotationMultiplier: 1",
             f"    m_StartColor: {col(r, g, b, a)}",
             f"    m_GravityModifier: {fmt(gravity)}",
             "    m_SimulationSpeed: 1", "    m_ScaleMode: 0",
             "    m_PlayOnAwake: 0", "    m_MaxParticles: 1000",
             "  m_EmissionModule:", "    serializedVersion: 3", "    m_Enabled: 1",
             f"    m_RateOverTime: {fmt(rate)}", "    m_RateOverTimeMultiplier: 1",
             "    m_BurstCount: 0",
             "  m_ShapeModule:", "    serializedVersion: 4", "    m_Enabled: 1",
             "    m_ShapeType: 0", "    m_Radius: 0.5"]
    if gradient is not None:
        ck, ak = gradient
        lines += ["  m_ColorOverLifetimeModule:", "    serializedVersion: 2",
                  "    m_Enabled: 1", "    m_Color:", "      serializedVersion: 2",
                  gradient_block(ck, ak)]
    return "\n".join(lines)


def particle_renderer(go_fid, sorting=0):
    return ("ParticleSystemRenderer:\n  m_ObjectHideFlags: 0\n"
            "  m_CorrespondingSourceObject: {fileID: 0}\n  m_PrefabInstance: {fileID: 0}\n"
            "  m_PrefabAsset: {fileID: 0}\n"
            f"  m_GameObject: {{fileID: {go_fid}}}\n  m_Enabled: 1\n  m_EditorHideFlags: 0\n"
            + renderer_base(go_fid, sorting) +
            "\n  m_RenderMode: 0\n  m_SortMode: 0\n  m_SortingFudge: 0\n"
            "  m_MinParticleSize: 0\n  m_MaxParticleSize: 0.5\n  m_Alignment: 0\n"
            "  m_Flip: {x: 0, y: 0, z: 0}\n  m_AllowRoll: 1\n  m_Mesh: {fileID: 0}")


def curve2(v0, v1):
    def key(t, v):
        slope = v1 - v0
        return (f"    - serializedVersion: 3\n      time: {fmt(t)}\n      value: {fmt(v)}\n"
                f"      inSlope: {fmt(slope)}\n      outSlope: {fmt(slope)}\n"
                f"      tangentMode: 0\n      weightedMode: 0\n"
                f"      inWeight: 0.33333334\n      outWeight: 0.33333334")
    return (f"    serializedVersion: 2\n    m_Curve:\n{key(0, v0)}\n{key(1, v1)}\n"
            f"    m_PreInfinity: 2\n    m_PostInfinity: 2\n    m_RotationOrder: 4")


def trail_block(go_fid):
    return ("TrailRenderer:\n  m_ObjectHideFlags: 0\n"
            "  m_CorrespondingSourceObject: {fileID: 0}\n  m_PrefabInstance: {fileID: 0}\n"
            "  m_PrefabAsset: {fileID: 0}\n"
            f"  m_GameObject: {{fileID: {go_fid}}}\n  m_Enabled: 1\n  m_EditorHideFlags: 0\n"
            + renderer_base(go_fid, 0) +
            "\n  m_Time: 0.3\n  m_MinVertexDistance: 0.1\n  m_WidthMultiplier: 1\n"
            f"  m_WidthCurve:\n{curve2(0.2, 0.0)}\n  m_Autodestruct: 0")


def line_block(go_fid):
    return ("LineRenderer:\n  m_ObjectHideFlags: 0\n"
            "  m_CorrespondingSourceObject: {fileID: 0}\n  m_PrefabInstance: {fileID: 0}\n"
            "  m_PrefabAsset: {fileID: 0}\n"
            f"  m_GameObject: {{fileID: {go_fid}}}\n  m_Enabled: 1\n  m_EditorHideFlags: 0\n"
            + renderer_base(go_fid, 0) +
            "\n  m_WidthMultiplier: 0.05\n  m_Loop: 1\n  m_UseWorldSpace: 1\n  m_Positions: []")


# ---- prefab builders (mirror fixed PrefabGenerator.cs) ----------------------

class Ctx:
    def __init__(self, prefab):
        self.p = prefab
        self.children_tr = []
        self.tr_specs = {}  # tr fid -> dict(go, pos, scale, father, kids)

    def go(self, name, tag, layer, active=True, pos=(0, 0, 0), scale=(1, 1, 1), father=None):
        go = self.p.add(1, f"go:{name}", None)
        tr = self.p.add(4, f"tr:{name}", None)
        self.p.objs[-2] = (go, 1, f"go:{name}",
                           go_block(self.p, go, name, tag, layer, active))
        self.p.objs[-1] = (tr, 4, f"tr:{name}",
                           tr_block(self.p, tr, go, pos, scale, [], father))
        self.tr_specs[tr] = {"go": go, "pos": pos, "scale": scale,
                             "father": father, "kids": []}
        if father is None:
            self.root_tr = tr
        else:
            self.tr_specs[father]["kids"].append(tr)
        self.children_tr.append((name, tr))
        self.p.finalize_transforms(self.tr_specs)
        return go, tr

    def child(self, parent_tr, name, pos=(0, 0, 0), scale=(1, 1, 1), active=True, tag="Untagged", layer=0):
        return self.go(name, tag, layer, active, pos, scale, father=parent_tr)

    def mb(self, go, script, fields):
        return self.p.add(114, f"mb:{script}", mb_block(self.p, go, script, fields))


def add_healthbar(ctx, parent_tr):
    go, _tr = ctx.child(parent_tr, "HealthBar", pos=(0, 2.5, 0), scale=(0.5, 0.5, 0.5))
    hb = ctx.mb(go, "HealthBar", [("_fillImage", "{fileID: 0}"), ("_backgroundImage", "{fileID: 0}"),
                                  ("_width", "2"), ("_height", "0.2"), ("_yOffset", "1.5")])
    return go, hb


def build_unit(card):
    p = Prefab(f"Unit_{sanitize_prefab_name(card['cardName'])}", "Unit", LAYERS["Unit"])
    ctx = Ctx(p)
    go, tr = ctx.go(p.name, "Unit", LAYERS["Unit"])
    uv = ctx.mb(go, "UnitView", [])
    an = p.add(95, "animator", animator_block(go))
    sr = p.add(212, "sprite", sprite_block(go, 10))
    p.add(60, "collider", circle_collider(go, 0.5))
    p.add(50, "rb", rigidbody2d(go))
    hb_go, _hb = add_healthbar(ctx, tr)
    ring_go, ring_tr = ctx.child(tr, "SelectionRing", active=False)
    lr = p.add(109, "line", line_block(ring_go))
    ctx.mb(ring_go, "SelectionRing", [("_lineRenderer", ref(lr)), ("_radius", "0.6"),
                                      ("_segments", "32"), ("_color", YELLOW),
                                      ("_width", "0.05"), ("_pulseSpeed", "3"), ("_pulseAmount", "0.2")])
    _pp, pp_tr = ctx.child(tr, "ParticlePoints")
    for n, pos in (("SpawnPoint", (0, 0, 0)), ("AttackPoint", (0.8, 0, 0)),
                   ("HitPoint", (0, 0, 0)), ("DeathPoint", (0, 0, 0))):
        ctx.child(pp_tr, n, pos=pos)
    ctx.mb(go, "UnitPoolable", [])
    # wire UnitView (SerializedObject equivalent)
    for i, (f, c, k, b) in enumerate(p.objs):
        if k == "mb:UnitView":
            p.objs[i] = (f, c, k, mb_block(p, go, "UnitView",
                                           [("_spriteRenderer", ref(sr)), ("_animator", ref(an)),
                                            ("_healthBar", ref(_hb)), ("_selectionRing", ref(ring_go)),
                                            ("_hitParticles", "{fileID: 0}"), ("_deathParticles", "{fileID: 0}")]))
    return p


def build_building(card):
    p = Prefab(f"Building_{sanitize_prefab_name(card['cardName'])}", "Building", LAYERS["Building"])
    ctx = Ctx(p)
    go, tr = ctx.go(p.name, "Building", LAYERS["Building"])
    an = p.add(95, "animator", animator_block(go))
    sr = p.add(212, "sprite", sprite_block(go, 5))
    p.add(59, "collider", box_collider(go, 2, 2))
    p.add(50, "rb", rigidbody2d(go))
    hb_go, _hb = add_healthbar(ctx, tr)
    ret_go, _ret_tr = ctx.child(tr, "RetractedVisual", active=False)
    if "tesla" in (card["cardName"] or "").lower():
        ctx.mb(go, "TeslaRetraction", [("_visualRoot", "{fileID: 0}"),
                                       ("_retractedPosition", "{x: 0, y: -2, z: 0}"),
                                       ("_retractionSpeed", "10"),
                                       ("_retractParticles", "{fileID: 0}"),
                                       ("_extendParticles", "{fileID: 0}")])
    mech = card["mechanicsJson"] or ""
    nm = card["cardName"] or ""
    if ("spawn" in mech or "Hut" in nm or "Furnace" in nm or "Tombstone" in nm
            or "Cage" in nm or "Drill" in nm):
        ctx.child(tr, "SpawnPoint", pos=(0, 1, 0))
    ctx.mb(go, "BuildingView", [("_spriteRenderer", ref(sr)), ("_animator", ref(an)),
                                   ("_healthBar", ref(_hb)), ("_retractedVisual", ref(ret_go))])
    ctx.mb(go, "BuildingPoolable", [])
    return p


def build_spell(card):
    p = Prefab(f"Spell_{sanitize_prefab_name(card['cardName'])}", "Spell", LAYERS["Spell"])
    ctx = Ctx(p)
    go, tr = ctx.go(p.name, "Spell", LAYERS["Spell"])
    ps = p.add(100, "ps", particle_system(go, color=spell_color(card["cardName"]),
                                          gradient=spell_gradient(card["cardName"])))
    p.add(26, "psr", particle_renderer(go))
    ctx.mb(go, "SpellPoolable", [])
    ctx.mb(go, "SpellEffectView", [("_particleSystem", ref(ps)),
                                   ("_areaIndicator", "{fileID: 0}"),
                                   ("_tornadoLine", "{fileID: 0}")])
    return p


def build_projectile(card):
    p = Prefab(f"Projectile_{sanitize_prefab_name(card['cardName'])}", "Projectile", LAYERS["Projectile"])
    ctx = Ctx(p)
    go, tr = ctx.go(p.name, "Projectile", LAYERS["Projectile"])
    sr = p.add(212, "sprite", sprite_block(go, 15))
    trl = p.add(110, "trail", trail_block(go))
    p.add(60, "collider", circle_collider(go, 0.2))
    p.add(50, "rb", rigidbody2d(go))
    imp_go, _imp_tr = ctx.child(tr, "ImpactParticles")
    ips = p.add(100, "impact", particle_system(imp_go))
    p.add(26, "impactr", particle_renderer(imp_go))
    ctx.mb(go, "ProjectilePoolable", [])
    ctx.mb(go, "ProjectileView", [("_spriteRenderer", ref(sr)), ("_trailRenderer", ref(trl)),
                                  ("_impactParticles", ref(ips))])
    return p


def build_tower(name, hp):
    p = Prefab(f"Tower_{name}", "Tower", LAYERS["Tower"])
    ctx = Ctx(p)
    go, tr = ctx.go(p.name, "Tower", LAYERS["Tower"])
    an = p.add(95, "animator", animator_block(go))
    sr = p.add(212, "sprite", sprite_block(go, 5))
    p.add(59, "collider", box_collider(go, 3, 4))
    p.add(50, "rb", rigidbody2d(go))
    hb_go, _hb = add_healthbar(ctx, tr)
    act_go, _act_tr = ctx.child(tr, "ActivationEffect", active=False)
    ctx.mb(go, "TowerView", [("_spriteRenderer", ref(sr)), ("_animator", ref(an)),
                             ("_healthBar", ref(_hb)), ("_activationEffect", ref(act_go)),
                             ("_hitParticles", "{fileID: 0}"), ("_destroyParticles", "{fileID: 0}")])
    return p


META_PREFAB = ("fileFormatVersion: 2\nguid: {guid}\nPrefabImporter:\n"
               "  externalObjects: {{}}\n  userData: \n"
               "  assetBundleName: \n  assetBundleVariant: \n")
META_SCRIPT = ("fileFormatVersion: 2\nguid: {guid}\nMonoImporter:\n"
               "  externalObjects: {{}}\n  serializedVersion: 2\n  userData: \n"
               "  assetBundleName: \n  assetBundleVariant: \n  defaultReferences: []\n"
               "  executionOrder: 0\n  icon: {{instanceID: 0}}\n"
               "  isExplicitlyReferenced: 0\n  needToUpdateDependencies: 0\n")


def write_script_metas():
    for cls, rel in SCRIPTS.items():
        meta = ROOT / (rel + ".meta")
        if not meta.is_file():
            meta.write_text(META_SCRIPT.format(guid=SCRIPT_GUID[cls]), encoding="utf-8")


def emit(prefab, subfolder):
    d = PREFABS_DIR / subfolder
    d.mkdir(parents=True, exist_ok=True)
    (d / f"{prefab.name}.prefab").write_text(prefab.text(), encoding="utf-8")
    rel = f"Assets/Prefabs/{subfolder}/{prefab.name}.prefab"
    (d / f"{prefab.name}.prefab.meta").write_text(
        META_PREFAB.format(guid=guid_of(rel)), encoding="utf-8")


def main():
    check_only = "--check-only" in sys.argv
    cards = load_cards()
    if not cards:
        print("ERROR: no CardData assets found (GAP-4.2 prerequisite missing)")
        return 2

    plan = []  # (prefab, subfolder, source_card_or_None)
    seen = set()
    counts = {"Units": 0, "Buildings": 0, "Spells": 0, "Projectiles": 0, "Towers": 0}
    for c in cards:
        t = TYPE_NAME[c["type"]]
        if t in ("Troop", "Champion"):
            p = build_unit(c)
            sub = "Units"
        elif t == "Building":
            p = build_building(c)
            sub = "Buildings"
        elif t == "Spell":
            p = build_spell(c)
            sub = "Spells"
        else:
            continue
        if p.name not in seen:
            seen.add(p.name)
            plan.append((p, sub, c))
            counts[sub] += 1
        if t in ("Troop", "Champion") and (c["baseRange"] or 0) > 1.5:
            pp = build_projectile(c)
            if pp.name not in seen:
                seen.add(pp.name)
                plan.append((pp, "Projectiles", c))
                counts["Projectiles"] += 1
    for tname in ("King", "Princess"):
        tp = build_tower(tname, 4384 if tname == "King" else 2584)
        plan.append((tp, "Towers", None))
        counts["Towers"] += 1

    exp_units = sum(1 for c in cards if TYPE_NAME[c["type"]] in ("Troop", "Champion"))
    exp_b = sum(1 for c in cards if TYPE_NAME[c["type"]] == "Building")
    exp_s = sum(1 for c in cards if TYPE_NAME[c["type"]] == "Spell")
    print(f"cards: {len(cards)} (units={exp_units} buildings={exp_b} spells={exp_s})")
    print(f"plan: {counts} (deduped by prefab name)")

    if check_only:
        return 0
    write_script_metas()
    for sub in ("Units", "Buildings", "Spells", "Projectiles", "Towers"):
        d = PREFABS_DIR / sub
        if d.is_dir():
            for stale in list(d.glob("*.prefab")) + list(d.glob("*.prefab.meta")):
                stale.unlink()
            gk = d / ".gitkeep"
            if gk.exists():
                gk.unlink()
    for prefab, sub, _src in plan:
        emit(prefab, sub)
    print(f"wrote {len(plan)} prefabs (+ .meta)")
    return 0


if __name__ == "__main__":
    sys.exit(main())
