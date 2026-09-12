# Clash Royale - Complete Game Mechanics Documentation

## 1. ELIXIR SYSTEM

### 1.1 Elixir Generation
- **Base Rate**: 1 elixir per 2.8 seconds (0.357 elixir/sec)
- **Double Elixir**: Last 60 seconds of match (1 elixir per 1.4 sec)
- **Triple Elixir**: Overtime (1 elixir per 0.93 sec)
- **Starting Elixir**: 5 elixir
- **Max Cap**: 10 elixir (overflow wasted)
- **Elixir Collector**: Generates 1 elixir per 9.8 sec (8 total over 70s lifetime)

### 1.2 Elixir Mechanics
- Elixir bar: Visual 10-segment bar
- Fractional elixir tracked internally (0.1 precision)
- No elixir gain while at 10 (hard cap)
- Elixir trade: Spending less elixir than opponent for same value

---

## 2. CARD SYSTEM

### 2.1 Deck Construction
- **8 cards per deck**
- **Max 1 Champion** (post-Champions update)
- **Card Levels**: 1-14 (Tournament Standard = 11)
- **Card Rarities**: Common, Rare, Epic, Legendary, Champion

### 2.2 Hand & Cycle
- **Hand Size**: 4 cards visible
- **Initial Draw**: 4 random cards from 8
- **Cycle Order**: Fixed rotation through 8-card deck
- **Next Card Preview**: 5th card shown in UI
- **Card Draw**: After playing, next in cycle draws immediately

### 2.3 Card Deployment
- **Deploy Zone**: Own side, 4 tiles from river (expands when Princess Tower destroyed)
- **Deploy Time**: 1 sec (most cards), 2 sec (heavy: P.E.K.K.A, Golem, X-Bow, Sparky)
- **Placement Validation**: Must be in deploy zone, no collision with river (except flying/spells)
- **Spell Placement**: Anywhere in arena (global range)

---

## 3. BATTLE MECHANICS

### 3.1 Match Structure
- **Duration**: 3 minutes + overtime
- **Overtime**: 3 minutes sudden death (first tower wins)
- **Draw**: If no towers destroyed in overtime
- **Win Condition**: More Crown Towers destroyed, or King Tower destroyed = 3 Crown win

### 3.2 Tower Mechanics

#### Crown Towers (Princess Towers)
- **HP**: 2584 (Level 11)
- **Damage**: 152 per hit
- **Hit Speed**: 1.2 sec
- **Range**: 7 tiles
- **Targeting**: Closest unit in range, prioritizes troops > buildings
- **Target Switch**: Only if current target dies or leaves range
- **Projectile**: Travel time ~0.3 sec at max range

#### King Tower
- **HP**: 4384 (Level 11)
- **Damage**: 152 per hit
- **Hit Speed**: 1.2 sec
- **Range**: 7 tiles
- **Activation**: 
  - Takes any damage
  - Princess Tower destroyed
  - Tornado pulls unit to King Tower
- **Activation Effect**: Deploys 2 Guards (level-based) at King Tower

### 3.3 Targeting Priority (All Units)
1. **Target in range & line of sight**
2. **Closest target** (by path distance, not straight line)
3. **Priority**: Troops > Buildings > Towers (for building-targeters: Buildings > Towers)
4. **Retarget**: Only on death, loss of LOS, or forced (Tornado, Fisherman hook)

---

## 4. TROOP AI & PATHFINDING

### 4.1 Movement
- **Grid**: 0.5 tile resolution pathfinding
- **River**: Impassable for ground (except bridges)
- **Collision**: Units push each other (mass-based)
- **Building Targeting**: Path to nearest building (by path distance)
- **Building Placement**: 2×2 or 3×3 or 4×4 tiles, snaps to grid

### 4.2 Attack Logic
```
IF target in range AND line of sight:
    ATTACK
ELSE IF target exists:
    MOVE toward target (pathfind)
ELSE:
    IDLE / RETARGET
```

### 4.3 Attack Types
- **Melee**: Range 1.2 tiles, instant hit
- **Ranged**: Projectile travel time, can miss if target moves
- **Splash/Area**: Circular radius from impact point
- **360° Splash**: Valkyrie, Dark Prince (all directions)
- **Piercing**: Magic Archer (infinite pierce in line)
- **Chain**: Electro Wizard (2 targets), Electro Dragon (2 chains), Electro Spirit (2 chains)
- **Charge**: Prince, Dark Prince, Ram Rider, Battle Ram, Little Prince, Goblin Drill

### 4.4 Special States
- **Stun**: Cannot move/attack (Zap, Lightning, Electro Wizard spawn, Electro Spirit, Electro Dragon)
- **Slow**: 35% move/attack speed reduction (Ice Wizard, Ice Spirit, Giant Snowball, Poison, Tornado)
- **Freeze**: Complete stop (Freeze, Ice Spirit death)
- **Invisible**: Untargetable until attack (Royal Ghost, Ghost, Archer Queen ability)
- **Invulnerable**: Cannot take damage (Tesla retracted, Goblin Drill burrowing, Bandit dash, Ram Rider charge)
- **Shield**: Absorbs damage (Guards 120 HP, Dark Prince 300 HP)

---

## 5. SPELL MECHANICS

### 5.1 Spell Categories
| Category | Examples | Mechanics |
|----------|----------|-----------|
| Instant Damage | Zap, Arrows, Giant Snowball | Immediate effect on cast |
| Delayed Damage | Fireball, Rocket, Poison, Earthquake | Travel time or DoT |
| Spawn | Goblin Barrel, Skeleton Barrel, Royal Delivery | Spawns units at location |
| Utility | Freeze, Rage, Clone, Mirror, Tornado | Status effects |
| Global Range | All spells | Can target anywhere |

### 5.2 Spell Details

#### Fireball (4 elixir)
- Travel time: ~1 sec at max range
- Knockback: 0.5 tiles
- Radius: 2.5 tiles
- Damage: 572

#### Rocket (6 elixir)
- Travel time: ~1.5 sec
- Radius: 2 tiles
- Damage: 1080
- Slow travel, high damage

#### Lightning (6 elixir)
- Instant
- 3 strikes, 0.4 sec interval
- Targets 3 highest HP units in 3.5 radius
- Damage per strike: 640

#### Poison (4 elixir)
- Duration: 8 sec
- Radius: 3.5 tiles
- Damage: 65/sec (520 total)
- Slow: 35%
- Ticks: Every 0.5 sec

#### Freeze (4 elixir)
- Duration: 4 sec (Level 11)
- Radius: 3 tiles
- Instant
- Stuns everything (troops, buildings, towers)

#### Tornado (3 elixir)
- Duration: 1.5 sec
- Radius: 5.5 tiles
- Pulls units to center (max 4 tile pull)
- Activates King Tower
- Damage: 0 (pure utility)

#### Graveyard (5 elixir)
- Duration: 3 sec spawn + skeleton lifetime
- Radius: 4 tiles
- Spawns: 15 skeletons randomly over 3 sec
- Skeleton stats: 67 HP, 67 dmg, 1.1 hit speed

#### The Log (2 elixir)
- Range: 11.5 tiles (ground only)
- Width: 11.5 tiles (full arena width)
- Damage: 240
- Knockback: 0.5 tiles
- Ground only, no air effect

#### Zap (2 elixir)
- Instant
- Radius: 2.5 tiles
- Damage: 159
- Stun: 0.5 sec
- Resets charge attacks

#### Rage (2 elixir)
- Duration: 6 sec
- Radius: 3.5 tiles
- +50% attack speed
- +30% move speed

#### Clone (3 elixir)
- Radius: 3 tiles
- Clones all friendly troops in radius
- Clones at -1 level (same HP%)
- Max clones limited by elixir

#### Mirror (varies)
- Cost: Last card played +1 elixir
- Plays last card at +1 level
- Cannot mirror Mirror

---

## 6. BUILDING MECHANICS

### 6.1 Building Types

#### Defensive (Target Troops)
- **Cannon**: Ground only, 5.5 range, 30s lifetime
- **Tesla**: Air & Ground, 5.5 range, retracts, 25s lifetime
- **Bomb Tower**: Ground splash, 6 range, 30s lifetime, death damage
- **Inferno Tower**: Ramping damage, 6 range, 25s lifetime

#### Spawners
- **Goblin Hut**: Spear Goblins every 4.9s, 30s, max 6
- **Furnace**: 2 Fire Spirits every 10s, 40s, max 4 waves
- **Tombstone**: Skeleton every 2.9s, 20s, max 6, death spawn 4
- **Goblin Cage**: Brawler on death, 30s or until destroyed
- **Goblin Drill**: Burrows, 3 waves of 2 goblins, 30s

#### Siege
- **Mortar**: Dead zone 0-4 tiles, range 4-11.5, 5s hit speed, 30s
- **X-Bow**: Range 11.5, 0.25s hit speed, ground only, 30s

#### Economy
- **Elixir Collector**: 1 elixir/9.8s, 70s lifetime, 8 elixir total

#### Hybrid
- **Cannon Cart**: Mobile (ground only) → Stationary (air & ground) when wheels destroyed

### 6.2 Building Mechanics
- **Lifetime**: Counts down from deployment
- **Targeting**: Same as troops (closest by path)
- **Placement**: 2×2 (most), 3×3 (Elixir Collector), 4×4 (X-Bow, Mortar)
- **Hitbox**: Center of building tile
- **Destruction**: Removed immediately, death effects trigger

---

## 7. CHAMPION MECHANICS

### 7.1 Champion Rules
- **Max 1 per deck**
- **Elixir Cost**: 4 (all champions)
- **Ability Cost**: Additional elixir (2-3)
- **Ability Cooldown**: 10-20 seconds
- **Level Scaling**: HP/Damage scale, ability effects scale

### 7.2 Champion Abilities

#### Archer Queen - Royal Cloak (3 elixir, 20s CD)
- Invisibility: 3 sec
- Damage Boost: 2.5x
- Move Speed: +20%
- Can target air & ground while invisible

#### Skeleton King - Summon Skeletons (2 elixir, 15s CD)
- Spawns 5 Skeletons around him
- Skeleton level = Champion level
- Skeletons behave normally

#### Mighty Miner - Super Dash (2 elixir, 10s CD)
- Dashes 5 tiles in target direction
- Stuns enemies in path 1 sec
- Damage: 220 at end
- Can deploy anywhere (like Miner)

---

## 8. PROJECTILE & COLLISION SYSTEM

### 8.1 Projectile Properties
- **Speed**: Tiles per second
- **Homing**: Tracks target (most ranged) or fixed trajectory (Fireball, Rocket)
- **Pierce**: Pass through (Magic Archer)
- **Chain**: Jump to nearby targets (Electro Wizard/Dragon/Spirit)
- **Splash**: Area on impact
- **Collision Radius**: 0.5 tiles typical

### 8.2 Projectile Interactions
- **Can be dodged**: Fast units can outrun slow projectiles
- **Splash hits all**: In radius at impact time
- **Chain priority**: Nearest valid target
- **Death mid-flight**: Projectile still hits (no cancel)

---

## 9. STATUS EFFECTS

| Effect | Sources | Duration | Mechanics |
|--------|---------|----------|-----------|
| Stun | Zap, Lightning, E-Wiz spawn, E-Spirit, E-Dragon | 0.5-1s | No move, no attack, channeling broken |
| Slow | Ice Wizard, Ice Spirit, Giant Snowball, Poison | 1.5-8s | -35% move/attack speed |
| Freeze | Freeze, Ice Spirit death | 1.5-4s | Complete stop |
| Invisible | Royal Ghost, Ghost, AQ ability | 2-3s | Untargetable, revealed on attack |
| Invulnerable | Tesla retract, Goblin Drill, Bandit dash | Varies | No damage taken |
| Shield | Guards, Dark Prince | Until broken | Absorbs X damage |
| Charge | Prince, Dark Prince, Ram, Battle Ram, Little Prince | Until hit | 2x damage, fast move, invuln |
| Pull | Tornado, Fisherman | Instant | Moves unit to point |

---

## 10. RENDERING & VISUAL MECHANICS

### 10.1 Visual Layers (Bottom to Top)
1. Arena background
2. River water animation
3. Grass/ground decals
4. Building placements (footprints)
5. Building structures
6. Projectiles (flying)
7. Ground troops
8. Air troops (height offset)
9. Spell effects (particles)
10. Health bars / selection rings
11. UI overlays

### 10.2 Animations Required per Unit
- **Idle**: Breathing, subtle movement
- **Walk/Run**: Movement cycle
- **Attack**: Windup, strike, recovery
- **Hit/Stun**: Flinch, stagger
- **Death**: Collapse, fade, particle burst
- **Spawn**: Appear, rise from ground
- **Special**: Charge windup, ability cast, transform

### 10.3 Particle Systems
- **Hit effects**: Blood, sparks, magic
- **Spell effects**: Fire, ice, lightning, poison cloud
- **Death effects**: Explosion, bones, gibs
- **Environmental**: River flow, torches, flags
- **UI**: Elixir pulse, card draw, tower target laser

---

## 11. AUDIO SYSTEM

### 11.1 Audio Categories
- **Music**: Battle theme (3 min + overtime variant)
- **SFX**: Unit voices, attacks, deaths, spells
- **UI**: Button clicks, card draw, elixir tick, tower hit
- **Announcer**: "Clash Royale!", "Overtime!", "Victory/Defeat"

### 11.2 Key Audio Cues
- Elixir generation tick (subtle)
- Card play sound (varies by rarity)
- Tower targeting laser sound
- Spell cast voice line
- Unit deploy voice
- Tower destruction fanfare
- King Tower activation warning

---

## 12. NETWORKING (Multiplayer)

### 12.1 Architecture
- **Authoritative Server**: Game state on server
- **Client Prediction**: Local simulation with reconciliation
- **Lockstep/Deterministic**: Fixed timestep (60 Hz)
- **Rollback**: For latency compensation

### 12.2 Message Types
- **Game State**: Full sync (periodic)
- **Input**: Card play, spell target
- **Ack**: Server confirmation
- **Replay**: Deterministic seed + inputs

### 12.3 Latency Handling
- **RTT < 100ms**: Smooth
- **RTT 100-200ms**: Minor prediction errors
- **RTT > 200ms**: Visible rollback/correction

---

## 13. PROGRESSION SYSTEM

### 13.1 Card Collection
- **Card Levels**: 1-14
- **Upgrade Cost**: Gold + Cards
- **Card Sources**: Chests, Shop, Trade, Quests
- **Wild Cards**: Universal (any rarity)

### 13.2 Chests
- **Free**: Every 4 hours
- **Crown**: Every 24 hours (10 crowns)
- **Season**: Battle Pass rewards
- **Battle**: Win rewards (Silver, Gold, Giant, Magical, Legendary)
- **Clan**: Clan war rewards

### 13.3 Gold Economy
- **Upgrade Costs**: Exponential scaling
- **Donate Rewards**: Gold + XP
- **Shop Offers**: Daily, Special

---

## 14. CLAN SYSTEM

### 14.1 Clan Features
- **Capacity**: 50 members
- **Roles**: Leader, Co-Leader, Elder, Member
- **Clan Chat**: Text + Replay sharing
- **Donations**: Cards (Common 10, Rare 1, Epic 0, Legendary 0 per request)
- **Clan Wars**: River Race / Classic War
- **Clan Capital**: Separate mode

---

## 15. TOURNAMENT / CHALLENGE SYSTEM

### 15.1 Challenge Types
- **Classic**: 3 losses = out, 12 wins = max
- **Grand**: 3 losses, 12 wins, better rewards
- **Draft**: Pick from pairs
- **2v2**: Team battles
- **Special Events**: Unique rules

### 15.2 Tournament Standard
- **Card Level**: 11 (all cards)
- **Tower Level**: 11
- **Fair Play**: No level advantage

---

## 16. REPLAY SYSTEM

### 16.1 Replay Data
- **Seed**: Deterministic RNG seed
- **Inputs**: All player actions with timestamps
- **Version**: Game version for compatibility
- **Metadata**: Players, decks, result, duration

### 16.2 Playback
- **Speed**: 0.5x, 1x, 2x, 4x, 8x
- **Camera**: Follow, free, player perspective
- **Timeline**: Jump to timestamp
- **Share**: Export/replay code

---

## 17. AI / SINGLE PLAYER

### 17.1 AI Difficulty
- **Easy**: Random plays, suboptimal targeting
- **Medium**: Basic counter-play, elixir management
- **Hard**: Optimal cycle, prediction, spell value

### 17.2 AI Behaviors
- **Defensive**: React to threats
- **Offensive**: Build pushes
- **Spell Usage**: Value targeting (hit 3+ units)
- **Building Placement**: Optimal defensive positions

---

## 18. EDGE CASES & BUG FIXES

### 18.1 Known Interactions
- **Tornado + King Tower**: Always activates King Tower
- **Fisherman Hook + King Tower**: Activates if pulled to King
- **Graveyard + Tornado**: Skeletons pulled to center
- **Clone + Champion**: Cannot clone Champions
- **Mirror + Champion**: Cannot mirror Champion
- **Sparky + Zap**: Charge reset
- **Inferno + Zap**: Ramp reset
- **Prince Charge + Zap**: Charge cancel
- **Ram Rider Snare + Zap**: Snare canceled

### 18.2 Simultaneous Events
- **Resolution Order**: Spells → Projectiles → Melee → Movement → Spawns
- **Death Processing**: All deaths processed simultaneously
- **Spawn on Death**: Death spawns happen after damage resolution

---

## 19. PERFORMANCE TARGETS

### 19.1 Frame Rate
- **Target**: 60 FPS (mobile), 60+ FPS (PC)
- **Simulation**: 60 Hz fixed timestep
- **Render**: Variable, vsync

### 19.2 Entity Limits
- **Max Troops**: ~50 per side (practical limit)
- **Max Projectiles**: ~100
- **Max Particles**: ~500
- **Buildings**: 5-10 per side

### 19.3 Network
- **Bandwidth**: < 50 KB/s per client
- **Packet Rate**: 20-30 Hz input, 10 Hz state sync

---

## 20. TESTING SCENARIOS (Mechanics)

### 20.1 Unit Tests
- Elixir generation rates
- Card cycle order
- Damage calculations (with level scaling)
- Spell radius/knockback
- Projectile travel time
- Building lifetime
- Champion ability cooldowns

### 20.2 Integration Tests
- Full 1v1 match simulation
- Spell-troop interactions
- Building-targeting logic
- Pathfinding around river
- Overtime sudden death
- Replay determinism

### 20.3 Edge Case Tests
- Simultaneous tower destruction
- King Tower activation mid-combat
- Maximum unit collision
- Spell on deploying unit
- Charge cancel mid-dash
- Invisible unit revealed by splash

---

*This mechanics document covers all core game systems. Each subsystem should have detailed implementation specs in separate files.*