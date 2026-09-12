# Battle Simulation Core - Implementation Plan

## Objective
Complete the entire deterministic battle simulation - the authoritative game logic that runs identically on client and server. This is the foundation everything else builds on.

## Current Implementation Status

### Files Already Exist (Partial Implementation)
| File | Status | Key Gaps |
|------|--------|----------|
| `BattleSimulation.cs` | ~60% | Missing: deterministic float, full win conditions, overtime sudden death, deploy validation, reconciliation, event logging |
| `Entity.cs` | ~80% | Missing: fixed-point math integration, proper death event emission |
| `Unit.cs` | ~50% | Missing: path-distance targeting, charge (multiple units), invisibility, shields, champion abilities, status effect details |
| `Building.cs` | ~60% | Missing: Goblin Cage, Goblin Drill burrowing, Inferno Tower ramping damage, building collision |
| `Projectile.cs` | ~70% | Missing: proper pierce (Magic Archer), chain logic, beam vs projectile distinction |
| `SpellEffect.cs` | ~65% | Missing: Clone, Mirror, Tornado full logic, Graveyard spawn, Royal Delivery, Barbarian Barrel |
| `Tower.cs` | ~70% | Missing: King activation on princess destroyed, Tornado/Fisherman pull, proper targeting priority |
| `Pathfinding.cs` | ~60% | Missing: building collision, unit pushing, flying unit pathing, dynamic obstacle updates |

---

## Assumptions
1. **GameConfig** provides all balance values (elixir rates, tower stats, etc.) - already implemented
2. **DataManager.GetCard()** returns CardData with mechanicsJson for special behaviors - already implemented
3. **EventBus** events are the only way to communicate with presentation layer - frozen contract
4. **Deterministic simulation** required - same seed = identical results (60Hz fixed timestep)
5. **Unity 2022.3+** with Burst/Jobs not available - single-threaded simulation
6. **Mock card data** acceptable until Agent 4 delivers ScriptableObjects

## Constraints
- **DO NOT MODIFY**: `GameTypes.cs`, `Services.cs`, `EventBus.cs` (frozen contracts)
- **Target**: 60Hz simulation < 4ms/tick (16.67ms budget)
- **Entity limits**: ~50 units/side, ~100 projectiles, ~10 buildings/side
- **Network**: Server ports this logic; keep portable (no UnityEngine deps in core logic where possible)

---

## Technical Architecture

### Core Loop Order (Critical for Determinism)
```csharp
void Tick(float dt)
{
    // 1. Process network inputs (validated)
    ProcessInputs();
    
    // 2. Elixir generation
    UpdateElixir(dt);
    
    // 3. Entity updates (ORDER MATTERS!)
    UpdateSpells(dt);        // Instant effects, DoT, spawn over time
    UpdateProjectiles(dt);   // Movement, collision, hits
    UpdateUnits(dt);         // Movement, targeting, attacks
    UpdateBuildings(dt);     // Lifetime, spawning, attacks
    UpdateTowers(dt);        // Targeting, attacks
    
    // 4. Collision & Targeting
    ResolveCollisions();     // Unit pushing, building collision
    UpdateTargeting();       // Retarget on death/loss of LOS
    
    // 5. Death processing (simultaneous)
    ProcessDeaths();         // All deaths, death spawns, effects
    
    // 6. Win condition check
    CheckWinCondition();     // 3 crowns, king tower, overtime, draw
    
    // 7. Event logging for replay
    RecordTickEvents();
}
```

---

## Detailed Implementation Steps

### Phase 1: Deterministic Foundations (Week 1, Days 1-2)

#### 1.1 Fixed-Point Math Struct (`Assets/Scripts/Core/FixedMath.cs` - NEW)
```csharp
// 1/1024 precision for deterministic cross-platform math
public struct Fixed
{
    public const int FRACTIONAL_BITS = 10;
    public const int SCALE = 1 << FRACTIONAL_BITS;
    private int _value;
    
    public static Fixed FromFloat(float f) => new Fixed { _value = (int)Math.Round(f * SCALE) };
    public float ToFloat() => _value / (float)SCALE;
    public static Fixed operator +(Fixed a, Fixed b) => new Fixed { _value = a._value + b._value };
    public static Fixed operator -(Fixed a, Fixed b) => new Fixed { _value = a._value - b._value };
    public static Fixed operator *(Fixed a, Fixed b) => new Fixed { _value = (int)(((long)a._value * b._value) >> FRACTIONAL_BITS) };
    public static Fixed operator /(Fixed a, Fixed b) => new Fixed { _value = (int)(((long)a._value << FRACTIONAL_BITS) / b._value) };
    public static bool operator <(Fixed a, Fixed b) => a._value < b._value;
    public static bool operator >(Fixed a, Fixed b) => a._value > b._value;
    public static implicit operator Fixed(int i) => new Fixed { _value = i << FRACTIONAL_BITS };
}
```

#### 1.2 DeterministicRNG Enhancement (`BattleSimulation.cs:656-673`)
- Replace with Xorshift64* (already implemented, verify quality)
- Add `NextFixed()`, `NextVector2()`, `NextInt(min, max)` helpers
- Ensure thread-safety (single-threaded simulation, so fine)

#### 1.3 FixedVector2 Integration (`GameTypes.cs:109-125` - Already exists!)
- Use `FixedVector2` for all entity positions internally
- Convert to `Vector2` only for presentation/EventBus
- Update all distance calculations to use fixed-point

**Validation**: Run same seed twice, compare event logs exactly.

---

### Phase 2: Pathfinding Completion (Week 1, Days 2-3)

#### 2.1 Building Collision Grid (`Pathfinding.cs`)
- Add `bool[,] _buildingGrid` updated each tick
- Mark building footprints as unwalkable (2x2, 3x3, 4x4)
- Clear and rebuild each tick or use dirty flags for performance
- **Critical**: River blocking + bridges + buildings = complete ground navmesh

#### 2.2 Unit Pushing Integration
- In `ResolveCollisions()`: after pathfinding move, apply push forces
- Mass-based: heavier units push lighter ones more
- Prevent stacking: max 2-3 units per tile

#### 2.3 Flying Unit Pathing
- Air units ignore river and buildings (fly over)
- Separate `_walkableAir` grid (all true except map bounds)
- Update `FindPath()` to accept `bool isFlying` parameter

#### 2.4 Dynamic Obstacle Updates
- Rebuild collision grid every N ticks (e.g., every 5 ticks = 12Hz)
- Or use dirty flag when buildings placed/destroyed
- Cache paths and invalidate on grid change

**Validation**: Unit navigates around river, buildings, other units correctly.

---

### Phase 3: Unit.cs - Complete Mechanics (Week 1-2)

#### 3.1 Targeting by Path Distance (`Unit.cs:143-173`)
- Replace Euclidean distance with `Pathfinding.GetPathDistance()`
- Cache path distances per frame
- Only recalculate when target moves significantly

#### 3.2 Charge Mechanics (Prince, Dark Prince, Ram Rider, Battle Ram, Little Prince, Goblin Drill)
- `StartCharge(targetPos, duration)` - already exists
- Double damage on impact
- Invulnerable during charge
- 2x move speed
- **Cancel on**: Stun, Freeze, target death, Zap, Lightning
- **Reset**: Attack cooldown reset on charge end

#### 3.3 Invisibility (Royal Ghost, Ghost, Archer Queen)
- `StatusEffectType.Invisible` - untargetable
- **Reveal on**: Attack, taking damage, splash damage, Tornado pull
- **Visual**: Presentation layer handles shader; simulation just tracks state
- **Targeting**: Invisible units not in `GetPotentialTargets()` unless revealed

#### 3.4 Shields (Guards 120 HP, Dark Prince 300 HP)
- `StatusEffectType.Shield` with `RemainingAmount`
- Absorbs damage before HP
- **Break on**: Damage exceeds remaining
- **Guards**: 3 shields (120 each), lost on any damage
- **Dark Prince**: Single 300 HP shield, regenerates? No, one-time

#### 3.5 Ramping Damage (Inferno Tower, Inferno Dragon)
- Damage doubles every 0.4s: 50→100→200→400→800→1600
- **Reset on**: Target switch, Stun, Freeze, target death
- Track `_rampStage` and `_timeAtStage` per attacker-target pair

#### 3.6 Champion Abilities
| Champion | Ability | Cost | CD | Effect |
|----------|---------|------|-----|--------|
| Archer Queen | Royal Cloak | 3 | 20s | Invis 3s, 2.5x dmg, +20% speed, target air |
| Skeleton King | Summon Skeletons | 2 | 15s | Spawn 5 skeletons around |
| Mighty Miner | Super Dash | 2 | 10s | Dash 5 tiles, stun 1s, 220 dmg at end |

- `TryUseAbility(targetPos)` - already stubbed, needs full implementation
- Cooldown tracking per champion instance
- Elixir cost deduction on use

#### 3.7 Status Effect Details
- **Stun**: No move, no attack, channeling broken (Sparky, Inferno)
- **Slow**: -35% move AND attack speed
- **Freeze**: Complete stop (stun + no cooldown progress)
- **Poison**: DoT + slow (reapply slow each tick)
- **Rage**: +50% atk speed, +30% move speed (friendly only)

**Validation**: All unit tests from TEST_SPEC.md Section 2.1.2 pass.

---

### Phase 4: Building.cs - Complete Mechanics (Week 2)

#### 4.1 Defensive Buildings
| Building | Range | Targets | Special |
|----------|-------|---------|---------|
| Cannon | 5.5 | Ground only | - |
| Tesla | 5.5 | Air & Ground | Retracts when no target (invuln) |
| Bomb Tower | 6 | Ground splash | Death damage |
| Inferno Tower | 6 | Air & Ground | Ramping beam damage |

#### 4.2 Spawner Buildings
| Building | Spawn | Interval | Max Waves | Death Spawn |
|----------|-------|----------|-----------|-------------|
| Goblin Hut | 1 Spear Goblin | 4.9s | 6 | - |
| Furnace | 2 Fire Spirits | 10s | 4 | - |
| Tombstone | 1 Skeleton | 2.9s | 6 | 4 Skeletons |
| Goblin Cage | - | - | - | 1 Brawler |
| Goblin Drill | 2 Goblins | 10s | 3 | Burrows underground |

#### 4.3 Siege Buildings
- **Mortar**: Dead zone 0-4 tiles, range 4-11.5, 5s hit speed, high arc, splash
- **X-Bow**: Range 11.5, 0.25s hit speed, ground only, fast projectile

#### 4.4 Economy Building
- **Elixir Collector**: 1 elixir/9.8s, 70s lifetime, 8 elixir total

#### 4.5 Hybrid Building
- **Cannon Cart**: Mobile (ground only, 5.5 range) → Stationary (air & ground, 5.5 range) when wheels destroyed (50% HP)

#### 4.6 Building Placement Validation
- Snap to half-tile grid
- 2x2 default, 3x3 (Elixir Collector), 4x4 (X-Bow, Mortar)
- Cannot place on river (ground buildings)
- Cannot overlap other buildings

**Validation**: All building tests from TEST_SPEC.md Section 2.1.3 pass.

---

### Phase 5: Projectile.cs - Complete Behaviors (Week 2)

#### 5.1 Projectile Types
| Type | Speed | Homing | Trajectory | Examples |
|------|-------|--------|------------|----------|
| Standard | 500-800 | Yes | Direct | Musketeer, Wizard, Archer |
| Fast | 1000+ | Yes | Direct | X-Bow, Tower |
| Mortar | 300 | No | Parabolic arc | Mortar, Goblin Barrel |
| Beam | Instant | N/A | Line | Inferno Tower, Sparky |
| Instant | N/A | N/A | N/A | Zap, Lightning, Arrows |

#### 5.2 Special Behaviors
- **Pierce (Magic Archer)**: Infinite pierce in straight line, damage falloff?
- **Chain (Electro Wizard/Dragon/Spirit)**: Jump to nearest valid target in range (4 tiles), max 2 chains
- **Splash**: Radius damage on impact (50% falloff)
- **Homing**: Track target position each tick
- **Dodgeable**: Fast units can outrun slow projectiles

#### 5.3 Projectile-Target Interaction
- Death mid-flight: Projectile still hits (no cancel)
- Target lost: Continue to last known position or die
- Collision radius: 0.5 tiles typical

**Validation**: Projectile tests from TEST_SPEC.md pass.

---

### Phase 6: SpellEffect.cs - Complete All Spells (Week 2-3)

#### 6.1 Instant Damage Spells
| Spell | Elixir | Radius | Damage | Knockback | Special |
|-------|--------|--------|--------|-----------|---------|
| Zap | 2 | 2.5 | 159 | - | Stun 0.5s, resets charge/Inferno |
| Arrows | 3 | 4 | 223 | - | - |
| Giant Snowball | 2 | 2.5 | 159 | 0.5 | Slow 35% |
| Fireball | 4 | 2.5 | 572 | 0.5 | Travel time ~1s |
| Rocket | 6 | 2 | 1080 | - | Travel time ~1.5s |
| Lightning | 6 | 3.5 | 640/strike | - | 3 strikes, 0.4s interval, top 3 HP |
| The Log | 2 | 11.5 wide | 240 | 0.5 | Ground only, line across arena |
| Earthquake | 3 | 3.5 | 206 | - | 2x dmg to buildings, stun 1s |

#### 6.2 Damage Over Time
| Spell | Elixir | Duration | Radius | DPS | Total | Slow |
|-------|--------|----------|--------|-----|-------|------|
| Poison | 4 | 8s | 3.5 | 65 | 520 | 35% |

#### 6.3 Utility Spells
| Spell | Elixir | Duration | Radius | Effect |
|-------|--------|----------|--------|--------|
| Freeze | 4 | 4s | 3 | Stun all (troops, buildings, towers) |
| Rage | 2 | 6s | 3.5 | Friendly: +50% atk speed, +30% move |
| Clone | 3 | - | 3 | Clone friendly troops at -1 level (same HP%) |
| Mirror | Last+1 | - | - | Play last card at +1 level |
| Tornado | 3 | 1.5s | 5.5 | Pull to center (max 4 tiles), activates King |

#### 6.4 Spawn Spells
| Spell | Elixir | Spawn | Duration | Radius | Notes |
|-------|--------|-------|----------|--------|-------|
| Goblin Barrel | 3 | 3 Goblins | 1s travel | - | Lands at target |
| Skeleton Barrel | 3 | 6 Skeletons | 3s | - | Death spawn |
| Graveyard | 5 | 15 Skeletons | 3s spawn | 4 | Random positions |
| Royal Delivery | 3 | 1 Recruit | 1.5s | 2.5 | Damage on landing |
| Barbarian Barrel | 2 | 1 Barbarian | 1s | 2.5 | Rolls, damage on path |

#### 6.5 Special Implementations
- **Tornado**: Pulls units toward center each tick, activates King Tower if pulled to it
- **Graveyard**: 15 skeletons over 3s in 4-tile radius (random positions)
- **The Log**: Ground-only, 11.5 tile width (full arena), pushback 0.5 tiles
- **Clone**: Clones all friendly troops in radius at -1 level (max HP%), cannot clone Champions
- **Mirror**: Cost = last card played +1 elixir, plays at +1 level, cannot mirror Mirror

**Validation**: All spell tests from TEST_SPEC.md Section 2.1.4 pass.

---

### Phase 7: Tower.cs - Complete Logic (Week 3, Day 1)

#### 7.1 Princess Tower Targeting
- Range: 7 tiles
- Damage: 152 (Level 11)
- Hit Speed: 1.2s
- Projectile speed: ~600 (travel time ~0.3s at max range)
- **Priority**: Closest by path distance, Troops > Buildings > Towers
- **Retarget**: Only on death, loss of LOS, or forced (Tornado, Fisherman)

#### 7.2 King Tower Activation
Triggered by:
1. **Takes any damage** (direct hit)
2. **Princess Tower destroyed** (either one)
3. **Tornado pulls unit** to King Tower position
4. **Fisherman hook** pulls unit to King Tower

Effect: Deploys 2 Guards (level-based) at King Tower, King Tower attacks.

#### 7.3 Tower States
- Princess Towers: Independent, can be destroyed separately
- King Tower: Invulnerable until activated, then targetable
- **3 Crown Win**: King Tower destroyed = instant win

**Validation**: Tower tests pass, King activation on all triggers works.

---

### Phase 8: BattleSimulation Core Loop (Week 3, Days 1-3)

#### 8.1 Elixir Generation (`BattleSimulation.cs:344-361`)
- Normal: 1 per 2.8s (0.357/s) - First 180s
- Double: 1 per 1.4s (0.714/s) - Last 60s (180-240s)
- Triple: 1 per 0.93s (1.075/s) - Overtime (240-420s)
- **Cap**: 10 elixir (fractional tracked internally)
- **Elixir Collector**: Handled in Building update

#### 8.2 Card Cycle (`PlayerState.cs:598-628`)
- Initial hand: 4 random from 8 (fixed rotation, not random - Clash Royale uses fixed cycle)
- After play: Shift left, draw next from deck
- **Wrap**: After 8th card, cycle back to 1st
- **Next card preview**: `Deck[NextCardIndex % 8]`

#### 8.3 Deploy Validation (`BattleSimulation.cs:228-256`)
- **Deploy Zone**: Own side, 4 tiles from river (y ≤ 13 for P1, y ≥ 19 for P2)
- **Expanded**: When Princess Tower destroyed, expands to 8 tiles (y ≤ 9 for P1, y ≥ 23 for P2)
- **Spells**: Global range (anywhere)
- **Buildings**: Own side of river only
- **Ground units**: Cannot place across river
- **Flying units**: Can place anywhere in deploy zone
- **Collision**: Check building/unit overlap at deploy position

#### 8.4 Win Conditions (`BattleSimulation.cs:478-515`)
1. **3 Crowns**: King Tower destroyed = instant win (3-0)
2. **Crown Count**: More Princess Towers destroyed after 3 min
3. **Overtime**: 3 min sudden death (first tower destroyed wins)
4. **Draw**: No towers destroyed in overtime
5. **Tie**: Equal crowns at end of overtime = draw

#### 8.5 Event Logging for Replay (`BattleSimulation.cs:517-525`)
```csharp
public struct ReplayEvent
{
    public uint tick;
    public ReplayEventType type;
    public int playerId;
    public int cardId;
    public FixedVector2 position;
    public uint entityId;
    public int damage;
    public int hpRemaining;
}
```
Log: Card plays, spells, unit spawns, deaths, tower damage, king activation, champion abilities.

#### 8.6 Reconciliation Support
- `GetEntitySnapshot(uint entityId)` - returns position, HP, target, state
- `Reconcile(ServerStateMessage)` - correct local state to server
- Track `_serverTick` vs `_currentTick` for latency compensation

**Validation**: Determinism test (TEST_SPEC.md Section 2.1.6) passes - same seed = identical event log.

---

### Phase 9: Integration & Testing (Week 3, Days 4-5)

#### 9.1 BattleTestRunner Enhancement
- Run all test scenarios from TEST_SPEC.md Section 2.1
- Add deterministic replay test
- Add performance benchmark (60Hz < 4ms/tick)

#### 9.2 Test Coverage Checklist
- [ ] Elixir generation rates (normal/double/triple)
- [ ] Card cycle (initial hand, draw next, wrap at 8)
- [ ] Unit combat (Knight vs Goblin, 3 Goblins vs Knight, ranged distance)
- [ ] Buildings (Cannon ground-only, Tesla retract/pop, spawner waves, lifetime)
- [ ] Spells (Fireball radius/knockback, Zap stun/reset, Poison DoT/slow, Freeze, Log ground-only, Tornado pull, Graveyard spawn)
- [ ] Champions (AQ invis+damage, SK skeletons, MM dash+stun, cooldowns)
- [ ] Targeting (closest by path, building-targeters ignore troops, retarget on death)
- [ ] **Determinism**: Same seed → identical event log

#### 9.3 Performance Validation
- 50 units/side + 100 projectiles + 10 buildings = < 4ms/tick
- Pathfinding: 10k paths < 100ms
- Memory stable over 5 min battle (< 50MB growth)

---

## Files to Modify (Priority Order)

### High Priority - Core Mechanics
1. `Assets/Scripts/Battle/Simulation/Pathfinding.cs` - Building collision, flying units, unit pushing
2. `Assets/Scripts/Battle/Simulation/Unit.cs` - Targeting, charge, invisibility, shields, champions
3. `Assets/Scripts/Battle/Simulation/Building.cs` - All building types, spawners, siege, economy
3. `Assets/Scripts/Battle/Simulation/SpellEffect.cs` - All spell implementations
4. `Assets/Scripts/Battle/Simulation/Projectile.cs` - Pierce, chain, mortar, beam
5. `Assets/Scripts/Battle/Simulation/Tower.cs` - King activation, targeting priority

### High Priority - Simulation Loop
6. `Assets/Scripts/Battle/Simulation/BattleSimulation.cs` - Elixir, card cycle, deploy validation, win conditions, event logging, reconciliation

### New Files
7. `Assets/Scripts/Core/FixedMath.cs` - Fixed-point math (if not using existing FixedVector2)
8. `Assets/Scripts/Battle/Simulation/DeterministicMath.cs` - Deterministic helpers (sin, cos, sqrt)

---

## Files That Should NOT Change
- `Assets/Scripts/Core/GameTypes.cs` - Frozen
- `Assets/Scripts/Core/Services.cs` - Frozen
- `Assets/Scripts/Core/EventBus.cs` - Frozen (only emit events)
- `Assets/Scripts/Network/` - Agent 2 owns this
- `Assets/Scripts/UI/` - Agent 3 owns this
- `server/` - Agent 2 & 5 own this

---

## Validation Commands

```bash
# Run unit tests in Unity Editor
# Open Unity, run BattleTestRunner scene, check console output

# Or via command line (if Unity CLI configured)
/Applications/Unity/Hub/Editor/2022.3.20f1/Unity.app/Contents/MacOS/Unity \
  -batchmode \
  -projectPath D:\Documents\Projects\CRclone \
  -runTests \
  -testResults results.xml \
  -logFile -

# Performance test
# In BattleTestRunner, set _testDurationSeconds = 300 (5 min)
# Monitor ms/tick in profiler

# Determinism test
# Run simulation twice with same seed, compare EventLog byte-for-byte
```

---

## Risk Assessment

| Risk | Likelihood | Impact | Mitigation |
|------|------------|--------|------------|
| Determinism bugs (float variance) | High | Critical | Use FixedVector2 everywhere, test replay determinism daily |
| Performance > 4ms/tick | Medium | High | Profile early, optimize pathfinding cache, object pooling |
| Missing edge cases (simultaneous events) | Medium | Medium | Implement resolution order: Spells → Projectiles → Melee → Movement → Spawns |
| Champion ability complexity | Medium | Medium | Implement one at a time, test thoroughly |
| Pathfinding with dynamic obstacles | Medium | High | Rebuild grid every 5 ticks, cache paths, invalidate on change |

---

## Rollback Considerations
- Each phase can be reverted independently (git commits per phase)
- BattleSimulation.cs is the main integration point - keep it working
- Feature flags for complex mechanics (e.g., `ENABLE_CHAMPIONS`)

---

## Dependencies
- **Agent 4 (Assets)**: Prefab templates for visual testing (UnitView, BuildingView, etc.)
- **Agent 2 (Network)**: Server ports simulation logic; verify identical results
- **Agent 6 (Tests)**: Writes tests for all agents' code; run BattleTestRunner continuously

---

## Next Steps
1. **Immediate**: Start Phase 1 (FixedMath, DeterministicRNG verification)
2. **Day 2**: Phase 2 (Pathfinding completion)
3. **Day 3-5**: Phase 3 (Unit mechanics)
4. **Week 2**: Phases 4-6 (Buildings, Projectiles, Spells)
5. **Week 3**: Phases 7-9 (Towers, Core Loop, Integration)

---

*Plan created based on analysis of existing codebase (GameTypes.cs, Services.cs, EventBus.cs, DataManager.cs, BattleSimulation.cs, Entity.cs, Unit.cs, Building.cs, Projectile.cs, SpellEffect.cs, Tower.cs, Pathfinding.cs) and reference documents (MECHANICS.md, ARCHITECTURE.md, TEST_SPEC.md).*