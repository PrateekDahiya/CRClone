# Clash Royale Clone - Asset Sourcing Plan

## 1. ASSET CATEGORIES & SOURCES

### 1.1 Visual Assets

#### Unit Sprites / Animations
| Source | Method | Notes |
|--------|--------|-------|
| **Official Game** | Extract from APK/IPA | Highest quality, legal risk (local only) |
| **Clash Royale Wiki** | Download rendered images | Good for reference, not animated |
| **Community Rips** | GitHub/Reddit/Discord | Pre-extracted, may have gaps |
| **Custom Creation** | Blender/Spine/Aseprite | Full control, massive effort |
| **Asset Stores** | Unity/Unreal Marketplace | Generic fantasy units, not CR-specific |
| **AI Generation** | Midjourney/Stable Diffusion | Concept art, not game-ready sprites |

**RECOMMENDED APPROACH**: Hybrid
1. Use community rips for base sprites (GitHub: `clash-royale-assets`, `cr-assets`)
2. Supplement with Wiki renders for missing units
3. Create custom for unique mechanics (champions, evolutions)
4. Use Spine/Unity 2D Animation for runtime animation

#### Building Sprites
- **Count**: ~15 unique buildings × 3-4 states = 45-60 sprites
- **Size**: 128×128 to 256×256
- **States**: Idle, Attacking, Damaged, Destroyed

#### Spell Effects
- **Type**: Particle Systems (not sprites)
- **Count**: 20+ spells
- **Tools**: Unity Particle System / Godot Particles2D / Custom

#### Tower Sprites
- **Princess Tower**: 4 states × 2 (player/enemy palette) = 8
- **King Tower**: 4 states × 2 = 8
- **Size**: 256×256

#### Arena/Environment
- **Background**: 1920×1080 (multiple arena themes)
- **River**: Animated shader/material
- **Bridges**: 2-3 variants
- **Grass/Details**: Tileable textures

#### UI Assets
- **Icon Atlas**: 2048×2048 (all UI icons)
- **Card Frames**: 5 rarities × 2 (normal/selected)
- **Buttons**: 9-slice panels (3 states each)
- **Fonts**: Supercell Magic or equivalent
- **Emotes**: 50+ animated emotes

### 1.2 Audio Assets

#### Music
| Track | Duration | Style |
|-------|----------|-------|
| Main Theme | 2:30 | Orchestral, epic |
| Battle Normal | 3:00 | Dynamic, loopable |
| Battle Double Elixir | 1:00 | Intense variant |
| Battle Overtime | 1:30 | Tense, building |
| Victory | 0:10 | Fanfare |
| Defeat | 0:10 | Somber |
| Draw | 0:10 | Neutral |
| Lobby/Menu | 2:00 | Calm, looping |

#### Sound Effects (Estimated 500+)
| Category | Count | Examples |
|----------|-------|----------|
| Unit Voices | 100+ | Deploy, attack, death, hit per unit |
| Spell Cast | 20 | Unique per spell |
| Spell Impact | 30 | Explosion, freeze, zap, poison |
| Tower | 10 | Attack, hit, destroy, activate |
| Building | 15 | Deploy, attack, destroy, spawn |
| UI | 50 | Click, hover, card draw, elixir tick |
| Announcer | 20 | "Clash Royale", "Overtime", etc. |
| Environment | 10 | River, wind, crowd |

#### Audio Specifications
- **Format**: OGG Vorbis (compressed), WAV (source)
- **Sample Rate**: 44.1 kHz
- **Bitrate**: 128-192 kbps (OGG)
- **Channels**: Stereo (music), Mono (SFX)
- **Naming**: `sfx_unit_knight_deploy.ogg`, `music_battle_normal.ogg`

---

## 2. ASSET PIPELINE

### 2.1 Directory Structure
```
Assets/
├── Art/
│   ├── Units/
│   │   ├── Common/
│   │   ├── Rare/
│   │   ├── Epic/
│   │   ├── Legendary/
│   │   └── Champion/
│   ├── Buildings/
│   ├── Spells/
│   ├── Towers/
│   ├── Arena/
│   └── UI/
│       ├── Icons/
│       ├── Frames/
│       ├── Buttons/
│       └── Fonts/
├── Audio/
│   ├── Music/
│   ├── SFX/
│   │   ├── Units/
│   │   ├── Spells/
│   │   ├── Buildings/
│   │   ├── Towers/
│   │   ├── UI/
│   │   └── Announcer/
│   └── Voice/
├── Animations/
│   ├── Units/
│   ├── Buildings/
│   └── Spells/
├── Prefabs/
│   ├── Units/
│   ├── Buildings/
│   ├── Spells/
│   └── Projectiles/
├── ScriptableObjects/
│   ├── CardData/
│   ├── SpellData/
│   ├── BuildingData/
│   └── TowerData/
└── Scenes/
    ├── Battle/
    ├── Lobby/
    ├── DeckBuilder/
    └── Menus/
```

### 2.2 Import Settings (Unity)

#### Sprites
```
Texture Type: Sprite (2D and UI)
Sprite Mode: Multiple (for atlases) / Single
Pixels Per Unit: 100
Filter Mode: Bilinear
Compression: ASTC 4x4 (mobile), BC7 (PC)
Max Size: 2048
Generate Mip Maps: Off (UI), On (3D/World)
```

#### Audio
```
Force To Mono: On (SFX), Off (Music)
Load Type: Compressed In Memory (SFX), Streaming (Music)
Compression Format: Vorbis
Quality: 0.7 (SFX), 0.9 (Music)
```

#### Animations
```
Rig: Generic / Humanoid (for Spine)
Animation Type: Legacy / Generic
Loop Time: On (idle, walk), Off (attack, death)
```

### 2.3 Animation Pipeline (Spine → Unity)
1. **Spine Project**: `Assets/Spine/Units/`
2. **Export**: `.skel.bytes` + `.atlas.txt` + PNG atlas
3. **Unity**: Spine-Unity runtime
4. **Prefab**: SkeletonAnimation component + scripts

### 2.4 Particle Systems
- **Authoring**: Unity Particle System / Godot GPUParticles2D
- **Prefabs**: One per spell/effect
- **Pooling**: Object pool for runtime instantiation

---

## 3. SPECIFIC ASSET LISTS

### 3.1 Units Requiring Sprites (122+ cards)
**Priority Order**:
1. **Core Meta Cards** (20): Knight, Archers, Giant, Musketeer, Mini P.E.K.K.A, Fireball, Wizard, Witch, Baby Dragon, Prince, Hog Rider, Valkyrie, Goblin Barrel, Goblin Gang, Mega Minion, Ice Wizard, Miner, Princess, Log, Electro Wizard
2. **Common Staples** (30): Skeletons, Goblins, Spear Goblins, Minions, Bats, Bomber, Cannon, Tesla, Tombstone, Furnace, Arrows, Zap, Giant Snowball, Barbarian Barrel, Royal Delivery, etc.
3. **Remaining** (70+): All other cards

### 3.2 Animation Count per Unit
| Unit Type | Animations Needed |
|-----------|-------------------|
| Ground Melee | Idle, Walk, Attack, Hit, Death, Spawn (6) |
| Ground Ranged | Idle, Walk, Attack, Hit, Death, Spawn (6) |
| Flying | Idle, Fly, Attack, Hit, Death, Spawn (6) |
| Building | Idle, Attack, Damaged, Destroyed, Spawn (5) |
| Spell | Cast, Travel, Impact, Area (particle) |
| Champion | All above + Ability Cast, Ability Active |

**Total Animations**: ~122 × 6 = 732 + buildings/spells ≈ 900+

### 3.3 Particle Effects (Spells)
| Spell | Particle Type | Complexity |
|-------|---------------|------------|
| Fireball | Fire/explosion | Medium |
| Rocket | Trail + explosion | Medium |
| Lightning | Bolt + strikes | High |
| Poison | Gas cloud + ticks | Medium |
| Freeze | Ice crystals + shatter | Medium |
| Tornado | Wind swirl + debris | High |
| Graveyard | Skeleton spawn + rise | High |
| The Log | Log roll + dust | Low |
| Zap | Electric arc | Low |
| Arrows | Arrow rain | Low |
| Rage | Red aura + burst | Low |
| Clone | Purple shimmer | Medium |
| Mirror | Gold shimmer | Medium |
| Earthquake | Ground crack | Medium |
| Giant Snowball | Snow roll + freeze | Medium |
| Royal Delivery | Crate drop + recruit | Medium |
| Barbarian Barrel | Barrel roll + spawn | Medium |

---

## 4. SOURCING STRATEGY BY PHASE

### Phase 2A: Core Assets (Weeks 1-2)
**Goal**: Minimum playable battle
- 20 core units (sprites + 6 anims each)
- 5 buildings
- 5 spells (Fireball, Zap, Arrows, Log, Rocket)
- 2 towers (Princess, King)
- 1 arena background
- Basic UI (hand bar, elixir, cards)
- 20 essential SFX
- 1 battle music track

### Phase 2B: Full Roster (Weeks 3-6)
- All 122+ units
- All 15+ buildings
- All 20+ spells
- All tower states
- 3 arena variants
- Full UI set
- All SFX (~500)
- All music tracks

### Phase 2C: Polish (Weeks 7-8)
- Champion animations
- Evolution variants
- Emotes (50+)
- Particle polish
- Shader effects (river, spells)
- LODs for performance
- Asset bundles for streaming

---

## 5. TOOLS & WORKFLOWS

### 5.1 Required Tools
| Task | Tool | License |
|------|------|---------|
| Sprite Editing | Aseprite | $20 |
| Animation | Spine 2D | $69-$299 |
| Particle Design | Unity/Godot Built-in | Free |
| Audio Editing | Audacity / Reaper | Free / $60 |
| Texture Compression | TexturePacker | $35 |
| Atlas Packing | TexturePacker / Unity Sprite Atlas | - |
| Version Control | Git LFS | Free |

### 5.2 Automation Scripts Needed
1. **Sprite Sheet Splitter**: Auto-slice exported atlases
2. **Animation Clip Creator**: Generate .anim from frame sequences
3. **Prefab Generator**: Auto-create unit prefabs from data
4. **Audio Normalizer**: Batch normalize volume levels
5. **Asset Validator**: Check naming, dimensions, formats

---

## 6. LEGAL NOTICE

**⚠️ IMPORTANT**: 
- Clash Royale assets are copyright Supercell
- This project is for LOCAL DEVELOPMENT ONLY
- NO distribution, NO public hosting, NO monetization
- Use only for learning/personal project
- Delete assets if project becomes public
- Consider creating original assets for any public release

---

## 7. ASSET TRACKING SPREADSHEET

Create `AssetTracker.xlsx` with columns:
| Asset ID | Name | Category | Rarity | Source | Status | Artist | Files | Notes |
|----------|------|----------|--------|--------|--------|--------|-------|-------|
| UNIT_001 | Knight | Troop | Common | Community Rip | Done | - | knight_idle.png, knight_walk.png... | 6 anims |
| SPELL_001 | Fireball | Spell | Rare | Custom Particles | In Progress | - | fireball.prefab | 3 variants |

---

## 8. DOWNLOAD CHECKLIST (Phase 2)

### Community Resources to Check
- [ ] GitHub: `clash-royale-assets` / `cr-assets` / `clash-royale-sprites`
- [ ] Reddit: r/ClashRoyaleModding / r/gamedev
- [ ] Discord: Clash Royale modding servers
- [ ] Wiki: clashroyale.fandom.com (renders)
- [ ] YouTube: "Clash Royale sprite sheet" tutorials

### Files to Download/Organize
- [ ] Unit sprite sheets (all 122+)
- [ ] Building sprites
- [ ] Tower sprites
- [ ] Arena backgrounds
- [ ] UI atlas source files
- [ ] Font files (Supercell Magic or substitute)
- [ ] Sound effect library
- [ ] Music tracks
- [ ] Particle effect references (videos/gifs)

---

*This asset plan provides a roadmap for acquiring all necessary game assets. Phase 2 execution will follow this structure.*