# Agent 4: Asset Pipeline & Card Database
## Workstream: ScriptableObject Cards, Prefab Templates, Spine Setup, Automation, Shaders

---

## 🎯 YOUR MISSION
Build the **entire asset pipeline** - from raw assets to game-ready prefabs. Create all 122+ cards as ScriptableObjects, prefab templates for every entity type, and automation tools so content creation is fast and consistent.

---

## 📁 FILES YOU OWN (Exclusive Write Access)

### Data & Config
```
Assets/Scripts/Data/
├── CardDatabase.cs           # Registry: GetCard(id), GetByRarity(), GetByType()
├── CardData.cs               # ScriptableObject (already exists - enhance)
├── GameConfig.cs             # Balance config (already exists - enhance)
```

### Systems
```
Assets/Scripts/Systems/
├── AssetManager.cs           # Addressables + Resources loading
├── PoolManager.cs            # Object pooling (exists - enhance)
├── AudioManager.cs           # AudioMixer groups (exists - enhance)
```

### Editor Tools (Assets/Editor/)
```
Assets/Editor/
├── AtlasBuilder.cs           # TexturePacker integration -> Sprite Atlases
├── AnimationClipGenerator.cs # Frame sequences -> AnimationClips
├── PrefabGenerator.cs        # CardData -> Unit/Building/Spell prefabs
├── AssetValidator.cs         # Naming, dimensions, compression checks
├── CardDatabaseBuilder.cs    # CSV/JSON -> 122+ CardData ScriptableObjects
├── SpineExporter.cs          # Spine project -> Unity animation setup
└── AudioImporter.cs          # Batch normalize, register clips
```

### Shaders
```
Assets/Shaders/
├── UnitShader.shader         # Team color tint, outline, dissolve, hover highlight
├── RiverShader.shader        # Flowing water with foam, refraction
├── SpellShaders/
    ├── FireShader.shader
    ├── IceShader.shader
    ├── LightningShader.shader
    └── PoisonShader.shader
```

### Spine Structure (Assets/Spine/)
```
Assets/Spine/
├── Units/
    ├── Common/               # Knight, Archers, Goblins, Skeletons, Minions, Bomber, Spear Goblins, Mega Minion, Guards, Goblin Gang, Bats, Wall Breakers, Battle Ram, Royal Hogs
    ├── Rare/                 # Giant, Musketeer, Mini PEKKA, Princess, Ice Wizard, Miner, Inferno Dragon, Mega Minion, Hog Rider, Valkyrie, etc.
    ├── Epic/                 # Baby Dragon, Prince, Wizard, Witch, Giant Skeleton, Balloon, PEKKA, Minion Horde, etc.
    ├── Legendary/            # Log, Princess, Ice Wizard, Miner, Sparky, Inferno Dragon, Lava Hound, Mega Knight, Electro Wizard, etc.
    └── Champion/             # Archer Queen, Skeleton King, Mighty Miner
├── Buildings/
    ├── Cannon, Tesla, Bomb Tower, Inferno Tower, Mortar, X-Bow, Elixir Collector
    ├── Goblin Hut, Furnace, Tombstone, Goblin Cage, Goblin Drill
    └── Cannon Cart
├── Spells/
    ├── Fireball, Rocket, Lightning, Poison, Freeze, Tornado, Graveyard
    ├── Log, Zap, Arrows, Rage, Clone, Mirror
    ├── Earthquake, Giant Snowball, Royal Delivery, Barbarian Barrel
    └── Goblin Barrel, Skeleton Barrel
└── Towers/
    ├── King Tower
    └── Princess Tower
```

---

## ✅ DELIVERABLES CHECKLIST

### Card Database (122+ ScriptableObjects)
- [ ] **CardDatabaseBuilder** reads `docs/planning/phase1/CARDS_DATABASE.md` (or CSV export) -> creates `Assets/Resources/Data/Cards/` with 122+ `CardData` assets
- [ ] Each `CardData` has: ID, name, rarity, type, elixir, base stats (HP, damage, hit speed, range, speed, deploy time, target, count), mechanics JSON, sprite/portrait IDs, spine asset name, audio clips
- [ ] **Level stats auto-generated** (1-14) using 1.1x multiplier per level
- [ ] `CardDatabase` singleton: `GetCard(id)`, `GetByRarity()`, `GetByType()`, `ValidateDeck()`

### Prefab Templates (One per entity type, instantiated via PoolManager)
- [ ] **Unit Prefab**: `UnitView` + `Animator` (Spine) + `HealthBar` + `SelectionRing` + collision + particle points
- [ ] **Building Prefab**: `BuildingView` + `Animator` + `HealthBar` + retraction (Tesla) + spawn points
- [ ] **Spell Prefab**: `SpellEffectView` + `ParticleSystem` (configured per spell)
- [ ] **Projectile Prefab**: `ProjectileView` + `SpriteRenderer` + `TrailRenderer` + impact particles
- [ ] **Tower Prefab**: `TowerView` + `Animator` + `HealthBar` + activation effect
- [ ] All prefabs in `Assets/Prefabs/{Units,Buildings,Spells,Projectiles,UI}/`

### Asset Pipeline Automation
- [ ] **AtlasBuilder**: TexturePacker CLI -> `Assets/Art/UI/UIAtlas.png` + `.meta` + `Assets/Art/Units/UnitAtlases/`
- [ ] **AnimationClipGenerator**: Folder of PNG frames -> `AnimationClip` with correct wrap mode, events
- [ ] **PrefabGenerator**: `CardData` + template -> fully configured prefab (assigns stats, spine asset, audio clips)
- [ ] **AssetValidator**: Runs on import - checks naming (`unit_knight_idle_01.png`), dimensions (power of 2), compression (ASTC 4x4), no missing references
- [ ] **SpineExporter**: `.skel.bytes` + `.atlas.txt` + PNG -> Unity `SkeletonDataAsset` + `SkeletonAnimation` setup

### Spine Animation Setup
- [ ] **Animation State Machine** per unit: Idle -> Walk -> Attack -> Hit -> Death -> Spawn
- [ ] **Skin System**: Team color tint (blue/red) via shader, not duplicate spines
- [ ] **Events**: Attack frame event -> spawn projectile; Hit frame -> play hit particles
- [ ] **Mix Durations**: Smooth transitions (0.1s idle<->walk, 0.05s attack->idle)

### Shaders
- [ ] **UnitShader**: Vertex color tint (team), outline (selection), dissolve (death), hover highlight
- [ ] **RiverShader**: UV scroll + noise for flow, foam at edges, refraction
- [ ] **Spell Shaders**: Additive fire, refractive ice, animated lightning, dissolving poison

### Audio System
- [ ] **AudioMixer Groups**: Master, Music, SFX, Voice (with ducking)
- [ ] **Clip Registration**: `AudioManager.RegisterClips()` from `Resources/Audio/`
- [ ] **Spatial/2D**: SFX spatial (3D), Voice/UI 2D
- [ ] **Batch Normalize**: All clips -18dB LUFS, peak -1dB

### Asset Manifest
- [ ] **CSV Export**: `Assets/AssetManifest.csv` - every asset with: ID, name, type, rarity, path, dimensions, compression, memory estimate, spine/prefab dependencies

---

## 📚 REFERENCE DOCS
| Doc | Purpose |
|-----|---------|
| `docs/planning/phase1/ASSET_SOURCING.md` | **Primary** - Pipeline, directory structure, asset counts, sourcing strategy |
| `docs/planning/phase1/CARDS_DATABASE.md` | All 122+ card stats for ScriptableObject generation |
| `docs/planning/phase1/UI_UX_SPEC.md` Section 10 | UI asset specs (atlases, icons, fonts) |
| `docs/planning/phase1/ARCHITECTURE.md` Section 5.2 | Rendering optimization, texture compression |

---

## 🔗 DEPENDENCIES

| Dependency | Status | Notes |
|------------|--------|-------|
| `CardData`, `GameConfig` | Exists | Enhance, don't rewrite |
| `PoolManager`, `AudioManager`, `AssetManager` | Exists | Enhance with new features |
| Agent 1 Simulation | Parallel | Provides entity types; you build prefabs for them |
| Agent 3 UI | Parallel | Needs UI atlas, fonts, card portraits |
| Agent 2 Server | Independent | Server doesn't need Unity assets |

---

## 🎨 ASSET SOURCING (From ASSET_SOURCING.md)
**Primary Sources (in order):**
1. **GitHub**: `clash-royale-assets`, `cr-assets`, `clash-royale-sprites` repos
2. **Reddit**: r/ClashRoyaleModding, r/gamedev
3. **Discord**: CR modding servers
3. **Wiki**: clashroyale.fandom.com (high-res renders for reference)
4. **YouTube**: "Clash Royale sprite sheet extraction" tutorials

**If gaps remain**: Custom creation in Spine/Blender/Aseprite (budget ~60h for 122 units)

---

## 🧪 TESTING REQUIREMENTS
- **AssetValidator**: Runs on every import - zero warnings in console
- **Prefab Instantiation**: All 122+ unit/building/spell prefabs instantiate without errors
- **Spine Playback**: Every animation state plays, transitions smooth, events fire
- **Memory**: Texture memory < 512MB (ASTC 4x4 mobile, BC7 desktop)
- **Pool Stress**: Spawn/despawn 1000 entities/sec - no GC spikes
- **Shader Compile**: All variants compile on target platforms

---

## 🚫 DO NOT TOUCH
- `Assets/Scripts/Core/` - Frozen
- `Assets/Scripts/Battle/Simulation/` - Agent 1
- `Assets/Scripts/Network/` - Agent 2
- `Assets/Scripts/UI/` - Agent 3
- `server/` - Agents 2 & 5

---

## 🌿 GIT WORKTREE SETUP (Run All 6 Agents Simultaneously)

**Each agent works in their own isolated worktree - no conflicts, no waiting.**

```bash
# Run ONCE per agent (each agent runs their own setup):

# Agent 4 - Assets
git worktree add ../CRClone-agent4 feature/asset-pipeline-card-db
cd ../CRClone-agent4
cp .env.example .env   # Fill in your DB credentials
# Start working...
```

**Each worktree is a complete, independent copy of the repo** - you can build, run tests, and commit independently. No stepping on each other's toes.

### Branch & Workflow (Per Worktree)
```bash
# Inside your worktree directory:
git checkout -b feature/asset-pipeline-card-db  # Already set by worktree add
# Phase 1: Build tools (AtlasBuilder, PrefabGenerator, CardDatabaseBuilder)
# Phase 2: Source assets -> process -> generate ScriptableObjects
# Phase 3: Build prefabs -> test in BattleTestRunner
git push origin feature/asset-pipeline-card-db
```

**Integration Points (Cross-Agent Sync via PRs):**
- Week 1: Tools ready → Agent 1 tests with real prefabs in `BattleTestRunner`
- Week 2: CardDatabase complete → Agent 3 DeckBuilder uses real data
- Week 3: All prefabs ready → Agent 3 replaces placeholders in Battle HUD
- Continuous: Agent 6 writes asset validation tests

---

## 📋 QUICK START
```csharp
// 1. Create CardDatabaseBuilder - parse CARDS_DATABASE.md -> CardData assets
// 2. Build PrefabGenerator - CardData + template -> Prefab
// 3. Test: BattleTestRunner with real UnitView prefab
var unitView = PoolManager.Spawn("Unit_Knight", pos, rot);
unitView.Initialize(unit); // Should work with Spine animations
```

**Good luck! You're building the content foundation.**