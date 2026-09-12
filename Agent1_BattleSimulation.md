# Agent 1: Battle Simulation Core
## Workstream: Deterministic Battle Simulation (P0 Priority)

---

## 🎯 YOUR MISSION
Complete the **entire deterministic battle simulation** - the authoritative game logic that runs identically on client and server. This is the foundation everything else builds on.

---

## 📁 FILES YOU OWN (Exclusive Write Access)
```
Assets/Scripts/Battle/Simulation/
├── BattleSimulation.cs      # Main simulation loop - 60Hz fixed timestep
├── Entity.cs                # Base entity class
├── Unit.cs                  # Troop logic (movement, targeting, attacks)
├── Building.cs              # Building logic (lifetime, spawning, attacks)
├── Projectile.cs            # Projectile logic (homing, splash, chain, pierce)
├── SpellEffect.cs           # Spell logic (instant, DoT, utility, spawn)
├── Tower.cs                 # Tower logic (targeting, king activation)
└── Pathfinding.cs           # A* on half-tile grid with river blocking
```

---

## ✅ DELIVERABLES CHECKLIST

### Core Simulation Loop
- [ ] `BattleSimulation.Tick(FIXED_DT)` - all phases in correct order
- [ ] Elixir generation (normal 2.8s, double 1.4s, triple 0.93s)
- [ ] Card cycle (4-card hand, draw from 8-card deck, next card preview)
- [ ] Deploy validation (deploy zones, river crossing, building placement)
- [ ] Win conditions (3 crowns, king tower = instant win, overtime sudden death, draw)

### Entity Systems
- [ ] **Unit.cs**: Movement, targeting (closest by path), melee/ranged attacks, charge, splash, air/ground, status effects (stun, freeze, slow, invisible, invulnerable, shield)
- [ ] **Building.cs**: Lifetime, retraction (Tesla), spawners (Goblin Hut, Furnace, Tombstone, Goblin Drill), siege (Mortar, X-Bow), Elixir Collector, Cannon Cart transformation
- [ ] **Projectile.cs**: Homing, instant/beam/mortar arc, splash, pierce (Magic Archer), chain (Electro Wizard/Dragon/Spirit)
- [ ] **SpellEffect.cs**: Instant (Zap, Arrows, Log, Freeze, Rage, Tornado, Earthquake), DoT (Poison), spawn (Goblin Barrel, Skeleton Barrel, Graveyard, Royal Delivery, Barbarian Barrel), utility (Clone, Mirror)
- [ ] **Tower.cs**: Targeting priority, king activation (damage, princess destroyed, Tornado/Fisherman pull)

### Advanced Mechanics
- [ ] **Charge**: Prince, Dark Prince, Ram Rider, Battle Ram, Little Prince, Goblin Drill
- [ ] **Invisibility**: Royal Ghost, Ghost, Archer Queen ability
- [ ] **Shields**: Guards (120 HP), Dark Prince (300 HP)
- [ ] **Ramping Damage**: Inferno Tower, Inferno Dragon (reset on stun/target switch)
- [ ] **Champion Abilities**: AQ Royal Cloak (invis + 2.5x dmg), SK Summon Skeletons, MM Super Dash (stun)
- [ ] **Graveyard**: 15 skeletons over 3s in 4-tile radius
- [ ] **Tornado**: Pull to center, activates king tower
- [ ] **The Log**: Ground-only, 11.5 tile width, pushback

### Pathfinding
- [ ] Half-tile grid (36×64)
- [ ] River impassable for ground (bridges at center 2 tiles)
- [ ] A* with binary heap, Manhattan heuristic
- [ ] Building collision, unit pushing

### Determinism & Replay
- [ ] `DeterministicRNG` (Xorshift64*) - same seed = identical results
- [ ] Fixed-point math option (1/1024 precision) or deterministic float
- [ ] Event logging for replay (every card play, spell, death, damage)
- [ ] Reconciliation support (entity state snapshots)

---

## 📚 REFERENCE DOCS (Read These First)
| Doc | Purpose |
|-----|---------|
| `docs/planning/phase1/MECHANICS.md` | **Primary** - All mechanics, interactions, edge cases |
| `docs/planning/phase1/CARDS_DATABASE.md` | Card stats for mechanic implementation |
| `docs/planning/phase1/TEST_SPEC.md` Section 2.1 | Unit test cases you must pass |
| `docs/planning/phase1/ARCHITECTURE.md` Section 2.3 | Simulation architecture |

---

## 🔗 DEPENDENCIES & INTEGRATION

| Dependency | Status | Notes |
|------------|--------|-------|
| `GameTypes.cs`, `Services.cs`, `EventBus.cs` | ✅ Done | Frozen contracts - do not modify |
| `DataManager.GetCard()` | ✅ Done | Use for card stats |
| `EventBus` events | ✅ Done | Emit: `OnCardPlayed`, `OnUnitSpawned`, `OnUnitDied`, `OnSpellCast`, `OnTowerDamaged`, `OnTowerDestroyed`, `OnKingTowerActivated`, `OnBattleEnded`, `OnElixirChanged`, `OnChampionAbilityUsed` |
| Agent 4 (Assets) | ⏳ Parallel | You need prefab templates for visual testing; Agent 4 provides `UnitView`, `BuildingView`, etc. |
| Agent 2 (Network) | ⏳ Parallel | Server ports your simulation logic; keep it portable |

---

## 🧪 TESTING REQUIREMENTS (From TEST_SPEC.md)

Run `BattleTestRunner` in Unity Editor. All must pass:
- Elixir generation rates (normal/double/triple)
- Card cycle (initial hand, draw next, wrap at 8)
- Unit combat (Knight vs Goblin, 3 Goblins vs Knight, ranged distance)
- Buildings (Cannon ground-only, Tesla retract/pop, spawner waves, lifetime)
- Spells (Fireball radius/knockback, Zap stun/reset, Poison DoT/slow, Freeze, Log ground-only, Tornado pull, Graveyard spawn)
- Champions (AQ invis+damage, SK skeletons, MM dash+stun, cooldowns)
- Targeting (closest by path, building-targeters ignore troops, retarget on death)
- **Determinism**: Same seed → identical event log (run simulation twice, compare)

---

## 🚫 DO NOT TOUCH
- `Assets/Scripts/Core/GameTypes.cs` - Frozen
- `Assets/Scripts/Core/Services.cs` - Frozen  
- `Assets/Scripts/Core/EventBus.cs` - Frozen (only emit events)
- `Assets/Scripts/Network/` - Agent 2 owns this
- `Assets/Scripts/UI/` - Agent 3 owns this
- `server/` - Agent 2 & 5 own this

---

## 🌿 BRANCH & WORKFLOW
```bash
git checkout -b feature/battle-simulation-core
# Work, commit frequently
git push origin feature/battle-simulation-core
# Create PR when all deliverables done
```

**Integration Points:**
- Week 1: Agent 4 provides prefab templates → you test with real views
- Week 2: Agent 2 ports your simulation to server → verify identical results
- Week 3: Agent 3 binds HUD to your EventBus events

---

## 📝 NOTES FOR YOU
- **Other agents are working simultaneously**: Agent 2 (Network), Agent 3 (UI), Agent 4 (Assets), Agent 5 (Backend), Agent 6 (Tests)
- **Communicate via PRs/Issues only** - don't edit other agents' files
- **If blocked**: Create GitHub Issue with `blocked` label, tag relevant agent, work on non-blocked deliverables
- **Mock data is fine** - use `BattleTestRunner` with hardcoded cards until Agent 4 delivers ScriptableObjects
- **Target**: 60Hz simulation < 4ms/tick (16.67ms budget)

---

## 📋 QUICK START
```csharp
// In BattleTestRunner.cs - already created
// Run in Unity Editor: creates simulation, spawns Knight vs Knight, runs 10s
// Verify: no errors, units fight, HP decreases, events logged
```

**Good luck! This is the engine of the entire game.** 🚀