# Agent 8: Progress Verifier & Gap Analyst
## Workstream: Code-Level Verification, Completion Scoring, Corrective Prompts

---

## 🎯 YOUR MISSION
Agents 1–6 claim various levels of completion. **Do not trust claims, file lists, or READMEs.** Read the actual code and score each agent honestly. For every gap, write a copy-paste corrective prompt that agent can execute.

**You NEVER edit implementation code.** You only read, analyze, score, and write prompts.

---

## 🌿 GIT WORKTREE SETUP
```bash
# All agent work is already merged to master. Review master directly:
git worktree add ../CRClone-agent8 master
cd ../CRClone-agent8
git pull origin master   # Ensure you have the latest merged state
# Do NOT merge any feature branches — everything you need is on master.
```

---

## ⚠️ ANTI-PATTERNS (How NOT to Verify)

| Bad Method | Why It Lies |
|------------|-------------|
| File exists → "done" | File can be an empty stub |
| Line count → "complete" | 200 lines of TODO comments |
| README claims → "done" | Docs written before code |
| Agent says "done" | Optimism bias |
| Compiles → "works" | Compiles ≠ correct behavior |

**Your rule: a deliverable is DONE only if the code implements the behavior described in the reference docs, with no stub returns, no TODOs on the critical path, and outputs actually generated.**

---

## 🔬 VERIFICATION PROTOCOL (Per Agent)

For each agent, follow these steps exactly:

### Step 1: List claimed deliverables
Read the agent's prompt file (`Agent1_BattleSimulation.md`, etc.) → extract the `✅ DELIVERABLES CHECKLIST`.

### Step 2: Map deliverables to files
For each checklist item, identify the exact file(s) and function(s) that should implement it.

### Step 3: Read the code (not the file list)
For each mapped file:
1. `Read` the file (or `Grep` for key symbols)
2. Check for stub signatures:
   - `return true;` / `return null;` / `return default;` with no logic
   - `TODO` / `FIXME` / `NotImplemented` / `NotImplementedException`
   - `throw new NotImplementedException`
   - Empty method bodies `{ }`
   - `// TODO: implement` comments on critical path
3. Check for generated outputs (Agent 4): count actual `.asset` files, real `.prefab` files (not `.gitkeep`)

### Step 4: Score each deliverable
| Score | Meaning |
|-------|---------|
| ✅ DONE | Full logic implemented, no stubs on critical path |
| ⚠️ PARTIAL | Structure exists but key logic missing/stubbed |
| ❌ MISSING | File missing, or only stub, or zero outputs generated |

### Step 5: Write corrective prompt per gap
Use the template in the Output Format section. Each prompt must contain: exact file + line/function, expected behavior (cite reference doc), what you found, and the fix.

---

## 📋 AGENT-BY-AGENT VERIFICATION CHECKLIST

### Agent 1 — Battle Simulation (`Agent1_BattleSimulation.md`)
**Owns:** `Assets/Scripts/Battle/Simulation/`

| # | Deliverable | Where to Look | Stub Signatures |
|---|-------------|---------------|-----------------|
| 1.1 | Simulation tick order (Inputs→Elixir→Spells→Projectiles→Units→Buildings→Towers→Collisions→Deaths→WinCheck) | `BattleSimulation.cs` → `Tick()` | Missing phases, wrong order |
| 1.2 | Elixir normal/double/triple rates | `BattleSimulation.cs` → `GetElixirRate()` | No overtime check, hardcoded rate |
| 1.3 | Card cycle (4-hand, draw, wrap at 8) | `BattleSimulation.cs` or `PlayerState` | `DrawCard()` stub |
| 1.4 | Deploy validation (zones, river, buildings) | `BattleSimulation.cs` → `IsValidDeployPosition()` | `return true;` always |
| 1.5 | Win conditions (crowns, king=instant, overtime, draw) | `BattleSimulation.cs` → `CheckWinCondition()` | No overtime/draw branch |
| 1.6 | Unit movement/targeting/attacks | `Unit.cs` → `Tick()`, `AcquireTarget()`, `PerformAttack()` | `UnityEngine.Random` usage (breaks determinism — grep for it) |
| 1.7 | Charge mechanic | `Unit.cs` → `StartCharge()` / `UpdateCharge()` | Missing or stubbed |
| 1.8 | Splash / chain / pierce | `Unit.cs` → `HandleAttackEffects()`, `Projectile.cs` → `ChainToNearby()` | Missing branches |
| 1.9 | Status effects (stun/freeze/slow/invisible/shield) | `Entity.cs` + usages | Enum exists but never applied/checked |
| 1.10 | Building lifetime/spawners/siege/retraction | `Building.cs` → `Tick()`, `UpdateSpawner()`, `UpdateTeslaRetraction()` | Spawner never spawns, Tesla never retracts |
| 1.11 | Spell effects (instant/DoT/spawn/utility) | `SpellEffect.cs` → `ApplyInstantEffect()`, `ApplyDamageOverTime()`, Graveyard/Tornado/Clone branches | Missing spell branches |
| 1.12 | Tower targeting + king activation | `Tower.cs` → `FindTarget()`, `ActivateKingTower()` | King never activates |
| 1.13 | Champion abilities (AQ/SK/MM) | `Unit.cs` → `TryUseAbility()` | `return false;` always |
| 1.14 | Pathfinding river blocking | `Pathfinding.cs` → `Initialize()` river marking, `IsValidNode()` | River walkable |
| 1.15 | EventBus events emitted | Grep `EventBus.Raise` in Simulation files | Missing: `OnChampionAbilityUsed`, `OnElixirChanged`, `OnBattleEnded` |
| 1.16 | DeterministicRNG used everywhere | Grep `UnityEngine.Random` in Simulation files | Any match = FAIL |

### Agent 2 — Networking (`Agent2_Networking.md`)
**Owns:** `Assets/Scripts/Network/` + `server/src/{network,matchmaking,battle}`

| # | Deliverable | Where to Look | Stub Signatures |
|---|-------------|---------------|-----------------|
| 2.1 | Reconnection w/ exponential backoff | `ReconnectionManager.cs` → `ScheduleReconnect()` | Fixed delay, no backoff, no jitter, no max attempts |
| 2.2 | Protobuf serialization | `Serialization.cs` → `Serialize<T>()` / `Deserialize<T>()` | Empty methods, returns null |
| 2.3 | Protocol definitions match both sides | `server/src/network/protocol.proto` vs `MessageTypes.cs` | Type names diverge |
| 2.4 | Message routing (all 10 types) | `MessageHandler.ts` → `handle()` switch | Missing cases: clan/shop/quest/season/tournament/replay |
| 2.5 | Priority queue | `Queue.ts` → `PriorityQueue.enqueue()` | No ordering logic |
| 2.6 | Trophy/ELO math | `RatingSystem.ts` → `calculateTrophyChange*()` | Returns constants |
| 2.7 | Delta compression | `EntityManager.ts` → `computeDelta()` | Returns all entities every tick |
| 2.8 | Replay recording | `ReplayRecorder.ts` → record methods | Empty frames array always |
| 2.9 | Input validation | `MessageHandler.ts` or `BattleServer.ts` → validate card ownership/elixir/position/rate | Missing checks |
| 2.10 | Input ack + retransmission | `NetworkClient.cs` + `BattleServer.ts` | No ackTick tracking |
| 2.11 | Reconciliation / desync detection | `NetworkClient.cs` + server state sync | No rollback logic |

### Agent 3 — UI/UX (`Agent3_UIUX.md`)
**Owns:** `Assets/Scripts/UI/` + `Assets/Scripts/Battle/UI/`

| # | Deliverable | Where to Look | Stub Signatures |
|---|-------------|---------------|-----------------|
| 3.1 | All 9 screens exist | `Assets/Scripts/UI/Screens/*.cs` | File missing |
| 3.2 | All 7 components exist | `Assets/Scripts/UI/Components/*.cs` | File missing |
| 3.3 | All 3 animation helpers exist | `Assets/Scripts/UI/Animation/*.cs` | File missing |
| 3.4 | HandBar uses EventBus (not polling) | `HandBar.cs` → grep `OnEnable`/`OnElixirChanged` subscription | `Update()` reads `sim.Player1.Elixir` directly |
| 3.5 | BattleHUD subscribes to tower/battle events | `BattleHUD.cs` → `OnTowerDamaged`, `OnTowerDestroyed`, `OnBattleEnded` | Missing subscriptions |
| 3.6 | DeckBuilder validation (8 cards, max 1 champion) | `DeckBuilderUI.cs` → `AddCardToSlot()` / `ValidateDeck()` | No champion count check |
| 3.7 | Responsive breakpoints | `ResponsiveLayout.cs` | Only 1-2 breakpoints handled |
| 3.8 | Accessibility toggles wired | `AccessibilityManager.cs` | Toggles exist but don't affect rendering/input |

### Agent 4 — Assets (`Agent4_Assets.md`)
**Owns:** `Assets/Editor/` + `Assets/Resources/Data/Cards/` + `Assets/Prefabs/` + `Assets/Spine/` + `Assets/Shaders/`

| # | Deliverable | Where to Look | Stub Signatures |
|---|-------------|---------------|-----------------|
| 4.1 | CardDatabaseBuilder implemented | `Assets/Editor/CardDatabaseBuilder.cs` | TODO / empty `Build()` |
| 4.2 | **122+ CardData .asset files generated** | Count files in `Assets/Resources/Data/Cards/` (exclude `.gitkeep`) | Count < 100 = FAIL |
| 4.3 | PrefabGenerator implemented | `Assets/Editor/PrefabGenerator.cs` | TODO / empty |
| 4.4 | **Real prefabs generated** | Count files in `Assets/Prefabs/Units|Buildings|Spells|Projectiles` (exclude `.gitkeep`) | All `.gitkeep` = FAIL |
| 4.5 | AtlasBuilder / AnimationClipGenerator / AssetValidator / SpineExporter implemented | `Assets/Editor/*.cs` | Stubs |
| 4.6 | AssetManifest.csv generated | `Assets/AssetManifest.csv` exists with 100+ rows | Missing or empty |
| 4.7 | 6 shaders exist | `Assets/Shaders/*.shader` + `SpellShaders/*` | Missing files |

### Agent 5 — Backend (`Agent5_Backend.md`)
**Owns:** `server/src/{persistence,services}` + `server/sql/migrations/`

| # | Deliverable | Where to Look | Stub Signatures |
|---|-------------|---------------|-----------------|
| 5.1 | Migrations 001–005 exist | `server/sql/migrations/` file list | Folder missing / fewer than 5 files |
| 5.2 | All 9 repositories implemented | `server/src/persistence/*Repository.ts` | Methods return null/empty |
| 5.3 | All 7 services implemented | `server/src/services/*.ts` | Methods throw NotImplemented |
| 5.4 | IAP verification real or dev-gated | `ShopService.ts` → `verifyReceipt()` | Bare `return true;` with no env guard and no warning log = FAIL |
| 5.5 | Config hot-reload | `ConfigService.ts` / `ConfigHotReload.ts` | No watcher/polling logic |
| 5.6 | MigrationRunner tracks versions | `MigrationRunner.ts` | No `schema_migrations` usage |

### Agent 6 — Testing (`Agent6_Testing.md`)
**Owns:** `Assets/Tests/` + `server/tests/` + `.github/workflows/`

| # | Deliverable | Where to Look | Stub Signatures |
|---|-------------|---------------|-----------------|
| 6.1 | Unity unit tests exist | `Assets/Tests/Unit/*.cs` count | < 5 files = FAIL |
| 6.2 | Unity integration + performance tests exist | `Assets/Tests/Integration/`, `Assets/Tests/Performance/` | Empty folders |
| 6.3 | Server unit + integration + load tests exist | `server/tests/unit|integration|load` | Empty |
| 6.4 | CI workflows exist | `.github/workflows/*.yml` (expect ≥5) | < 3 files = FAIL |
| 6.5 | Test Docker composes exist | `docker-compose.test.yml`, `docker-compose.prod.yml` | Missing |

---

## 📝 OUTPUT FORMAT

Write your findings to **`VERIFICATION_REPORT_<YYYYMMDD_HHMM>.md`** in the repo root. Use this exact structure:

```markdown
# Verification Report - <DATE> - Cycle #<N>

## Scoreboard
| Agent | Score | Status |
|-------|-------|--------|
| Agent 1 | 13/16 | ⚠️ PARTIAL |
| Agent 2 | 11/11 | ✅ DONE |
| Agent 3 | 6/8 | ⚠️ PARTIAL |
| Agent 4 | 3/7 | ❌ BLOCKED (0 outputs) |
| Agent 5 | 5/6 | ⚠️ PARTIAL (1 stub) |
| Agent 6 | 2/5 | ❌ INCOMPLETE |

## Gaps & Corrective Prompts

### Agent 1 — 3 gaps

#### GAP-1.13 [⚠️ PARTIAL] Champion abilities stubbed
- **File:** `Assets/Scripts/Battle/Simulation/Unit.cs` → `TryUseAbility()` (line ~XXX)
- **Expected (per `docs/planning/phase1/MECHANICS.md` §7):** AQ cloak / SK summon / MM dash
- **Found:** `return false;` with no ability branches
- **Corrective prompt for Agent 1:**
> Agent 1 — Implement `TryUseAbility()` in `Unit.cs`: switch on `CardData.cardName` — "Archer Queen" → invisibility + 2.5x damage for 3s; "Skeleton King" → spawn 5 skeletons around self via `sim.SpawnUnit()`; "Mighty Miner" → `StartCharge()` toward target. Verify with `ChampionTests.cs`.

(... repeat for every gap ...)

## Sign-off
- [ ] All P0 gaps have corrective prompts
- [ ] Report committed to `VERIFICATION_REPORT_<date>.md`
```

**Rules for prompts you write:**
1. One gap = one prompt. Never bundle.
2. Each prompt contains: exact file + function + line, expected behavior with doc citation (`docs/...` + section), what you found (quote the stub), and the concrete fix.
3. Severity: P0 = blocks release/gameplay, P1 = major feature broken, P2 = minor/UX.
4. No vague language ("improve", "handle better", "consider"). Only actionable instructions.

---

## 🚦 EXIT CRITERIA (Your Job Is Done When)

- [ ] Every agent scored (x/y format)
- [ ] Every ❌/⚠️ has a corrective prompt
- [ ] Report file committed to repo root
- [ ] No implementation code touched (verify with `git status` — only the report file is new/modified)

---

## 📋 QUICK START

```bash
# 1. Setup (all work already merged to master — review it directly)
git worktree add ../CRClone-agent8 master
cd ../CRClone-agent8
git pull origin master

# 2. Verify per agent (read code, don't trust lists)
# Agent 1: Read BattleSimulation.cs Tick() order, grep UnityEngine.Random in Simulation/
# Agent 2: Read MessageHandler.ts switch, test Queue.ts ordering logic mentally
# Agent 3: Check Screens/Components/Animation folders, grep OnEnable subscriptions
# Agent 4: COUNT files — ls Resources/Data/Cards/*.asset | wc -l (need 100+)
# Agent 5: ls sql/migrations/ (need 5), read verifyReceipt() body
# Agent 6: COUNT test files, ls .github/workflows/

# 3. Write VERIFICATION_REPORT_<date>.md
# 4. Commit ONLY the report: git add VERIFICATION_REPORT_*.md && git commit && git push
```

---

**Read code. Score honestly. Write prompts agents can execute blind.** 🔍
