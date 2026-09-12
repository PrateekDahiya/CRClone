# Agent 7: QA Integration & Regression Testing
## Workstream: End-to-End Integration Testing, Bug Detection, Issue Management, Release Validation

---

## 🎯 YOUR MISSION
You are the **Final Gatekeeper**. After all 6 agents declare "done", you run comprehensive integration tests, find every bug, connectivity issue, sync problem, and regression. You create a structured **Issue Sheet** that the other 6 agents will fix. You then re-test. Repeat until **ZERO critical issues**.

**You NEVER edit implementation code.** You only test, document, and orchestrate fixes.

---

## 🌿 GIT WORKTREE SETUP
```bash
# Run ONCE in main repo:
git worktree add ../CRClone-agent7 feature/qa-integration
cd ../CRClone-agent7
cp .env.example .env
# Fill in DB credentials
```

**Branch:** `feature/qa-integration` (auto-set by worktree)

---

## 📋 YOUR WORKFLOW (Repeat Until Clean)

```
┌─────────────────────────────────────────────────────────────────┐
│                    QA CYCLE (Repeat Until Clean)                │
├─────────────────────────────────────────────────────────────────┤
│                                                                 │
│  1. PULL LATEST FROM ALL 6 AGENTS                               │
│     git fetch origin                                            │
│     git merge origin/feature/battle-simulation-core             │
│     git merge origin/feature/networking-multiplayer             │
│     git merge origin/feature/ui-ux-implementation               │
│     git merge origin/feature/asset-pipeline-card-db             │
│     git merge origin/feature/database-backend-services          │
│     git merge origin/feature/testing-ci-cd                      │
│                                                                 │
│  2. BUILD & START FULL STACK                                    │
│     docker-compose -f docker-compose.test.yml up -d             │
│     # Or manual: server + Unity headless test runner            │
│                                                                 │
│  3. RUN FULL INTEGRATION SUITE                                  │
│     ./run-all-tests.sh                                          │
│                                                                 │
│  4. DOCUMENT EVERY FAILURE IN ISSUE SHEET                       │
│     (See format below)                                          │
│                                                                 │
│  5. IF CRITICAL ISSUES > 0:                                     │
│     - Create GitHub Issues for each agent                       │
│     - Tag @agent1 @agent2 etc. with fix prompts                 │
│     - WAIT for agents to fix & push                             │
│     - GOTO STEP 1                                               │
│                                                                 │
│  6. IF ZERO CRITICAL ISSUES:                                    │
│     - Run performance benchmarks                                │
│     - Run security scan                                         │
│     - Sign off: "RELEASE READY"                                 │
│                                                                 │
└─────────────────────────────────────────────────────────────────┘
```

---

## 🔬 TEST MATRIX (What You Test)

### **1. Frontend → Backend Integration (Critical Path)**

| Test | Expected | Fail = Issue |
|------|----------|--------------|
| Guest login → JWT token | 200 + valid token | Auth broken |
| Register → email verification | 201 + email sent | Auth broken |
| Deck save → GET deck | 200 + 8 cards | Deck API broken |
| Matchmake 1v1 → battle_found | WS message + seed | Matchmaking broken |
| Play card → game_state update | WS broadcast + state sync | Network broken |
| Spell cast → damage applied | Server validates + applies | Simulation broken |
| Tower destroyed → crowns++ | Battle end event | Win logic broken |
| Battle end → trophies updated | DB updated + replay saved | Persistence broken |
| Replay ID → retrieve + playback | Deterministic replay | Replay broken |

### **2. Battle Simulation Determinism (P0)**

| Test | Expected | Fail = Issue |
|------|----------|--------------|
| Same seed × 2 runs = identical entity states | Byte-for-byte identical | Non-determinism |
| Same seed client vs server | Entity states match at tick 1000 | Desync |
| Replay playback = original battle | Entity states match every tick | Replay corruption |
| 100 random seeds | All deterministic | RNG bug |

### **3. Network & Reconciliation**

| Test | Expected | Fail = Issue |
|------|----------|--------------|
| 200ms RTT + packet loss | Reconciliation corrects | Desync recovery failed |
| Disconnect mid-battle → reconnect | State restored within 2s | Reconnection broken |
| Invalid input (cheat) | Rejected, no state change | Server validation missing |
| Rate limit (100 inputs/sec) | Connection dropped | Rate limiting missing |
| Heartbeat timeout | Clean disconnect | Zombie connections |

### **4. UI/UX Integration**

| Test | Expected | Fail = Issue |
|------|----------|--------------|
| DeckBuilder: drag 8 cards → save → use in battle | Cards appear in hand | Deck sync broken |
| Battle HUD: elixir bar updates real-time | Matches server elixir | UI desync |
| Card deploy animation → unit spawns | Visual = simulation | Visual lag |
| Tower HP bar → tower destroyed | Crown updates | UI desync |
| Responsive: mobile/landscape/tablet/desktop | No overflow, touch targets | Responsive broken |
| Accessibility: high contrast, reduce motion | Toggles work | A11y broken |

### **5. Asset Pipeline Validation**

| Test | Expected | Fail = Issue |
|------|----------|--------------|
| All 122+ CardData assets load | No null refs | Missing assets |
| All prefabs instantiate | No errors | Prefab config broken |
| Spine animations: idle→walk→attack→hit→death | Smooth, events fire | Animation broken |
| Texture memory < 512MB | Pass | Memory leak |
| Shader variants compile | No errors | Shader broken |

### **6. Database & Persistence**

| Test | Expected | Fail = Issue |
|------|----------|--------------|
| Migrations up/down | Clean | Migration broken |
| Transaction rollback on error | No partial state | Data corruption |
| Concurrent battles (100) | No deadlocks | Lock contention |
| Replay cleanup job | Old replays deleted | Storage leak |
| Leaderboard refresh | Accurate ranks | Stale data |

### **7. Performance & Regression**

| Test | Threshold | Fail = Issue |
|------|-----------|--------------|
| Simulation tick | < 4ms | Perf regression |
| Pathfinding 10k paths | < 100ms | Perf regression |
| Memory growth 5min battle | < 50MB | Memory leak |
| 1000 concurrent WS | < 100ms p99 | Network bottleneck |
| DB 10k QPS reads | < 50ms p99 | DB bottleneck |

### **8. Security**

| Test | Expected | Fail = Issue |
|------|----------|--------------|
| SQL injection attempts | Blocked | SQLi vulnerability |
| JWT tampering | Rejected | Auth bypass |
| Rate limiting | Enforced | DoS vulnerability |
| IAP receipt validation | Verified | Revenue loss |
| CORS/CSRF headers | Present | Web vulnerability |

---

## 📝 ISSUE SHEET FORMAT (Create This File)

**File:** `QA_ISSUE_SHEET_<YYYYMMDD_HHMM>.md`

```markdown
# QA Issue Sheet - <DATE> - Cycle #<N>

## Summary
- **Total Issues:** X
- **Critical (P0):** X - Blocks release
- **High (P1):** X - Major functionality broken
- **Medium (P2):** X - Minor functionality / UX
- **Low (P3):** X - Cosmetic / nice-to-have

---

## Issues by Agent

### 🔴 Agent 1 - Battle Simulation (X issues)

#### ISSUE-101 [P0] - Non-deterministic unit targeting
- **Test:** Determinism test (same seed × 2)
- **Expected:** Identical entity states at tick 1000
- **Actual:** Unit A targets different enemy on run 2
- **Root Cause:** `Unit.AcquireTarget()` uses `UnityEngine.Random` for tie-breaking
- **Fix Prompt for Agent 1:**
  > Replace `UnityEngine.Random.Range()` in `Unit.AcquireTarget()` with `DeterministicRNG.NextInt()` from simulation RNG. Ensure tie-breaking is deterministic (e.g., by entity ID).

#### ISSUE-102 [P1] - Elixir generation rate wrong in overtime
- **Test:** Battle duration > 180s
- **Expected:** Triple elixir (0.93s/elixir)
- **Actual:** Still double elixir (1.4s/elixir)
- **Root Cause:** `GetElixirRate()` doesn't check overtime flag
- **Fix Prompt for Agent 1:**
  > In `BattleSimulation.GetElixirRate()`, check `elapsed >= battleDuration + overtimeDuration` for triple elixir, not just `elapsed >= battleDuration`.

### 🔵 Agent 2 - Networking (X issues)

#### ISSUE-201 [P0] - Server doesn't validate deploy position
- **Test:** Cheat client deploys on enemy side
- **Expected:** Input rejected, error returned
- **Actual:** Unit spawns on enemy side
- **Root Cause:** `BattleServer.applyInput()` missing `isInDeployZone()` check
- **Fix Prompt for Agent 2:**
  > In `BattleServer.ts`, add `validateDeployPosition(playerId, position, cardType)` call before spawning. Reject with `INVALID_POSITION` error code.

#### ISSUE-202 [P1] - No input acknowledgment
- **Test:** Client sends input, network lag
- **Expected:** Server ACKs with `input_ack` containing `ackTick`
- **Actual:** Client resends indefinitely
- **Root Cause:** `BattleServer` doesn't send `InputAckMessage`
- **Fix Prompt for Agent 2:**
  > After processing input in `BattleServer.handleInput()`, send `InputAckMessage { ackTick: input.clientTick }` to client. Client should track unacked inputs.

### 🟢 Agent 3 - UI/UX (X issues)

#### ISSUE-301 [P1] - Elixir bar doesn't update in real-time
- **Test:** Play card, watch elixir bar
- **Expected:** Smooth animation to new value within 100ms
- **Actual:** Bar updates only after next server state sync (2-3s delay)
- **Root Cause:** `HandBar` polls `simulation.Player1.Elixir` instead of subscribing to `EventBus.OnElixirChanged`
- **Fix Prompt for Agent 3:**
  > In `HandBar.cs`, subscribe to `EventBus.OnElixirChanged` in `OnEnable()`, update `elixirBarFill.fillAmount` immediately. Unsubscribe in `OnDisable()`.

#### ISSUE-302 [P2] - DeckBuilder validation allows 2 champions
- **Test:** Drag 2 champions to deck → save
- **Expected:** Validation error "Max 1 Champion"
- **Actual:** Saves successfully
- **Root Cause:** `DeckBuilderUI.AddCardToSlot()` doesn't check existing champions in other slots
- **Fix Prompt for Agent 3:**
  > In `DeckBuilderUI.AddCardToSlot()`, iterate all 8 slots, count `CardRarity.Champion`, reject if >= 1 and new card is Champion.

### 🟡 Agent 4 - Assets (X issues)

#### ISSUE-401 [P0] - Knight prefab missing HealthBar component
- **Test:** Spawn Knight in BattleTestRunner
- **Expected:** Health bar visible above unit
- **Actual:** No health bar, null ref in `UnitView.Initialize()`
- **Root Cause:** PrefabGenerator didn't add HealthBar to unit template
- **Fix Prompt for Agent 4:**
  > In `PrefabGenerator.cs`, ensure unit template prefab has `HealthBar` child with `HealthBar` script. Verify `UnitView.Initialize()` finds it via `GetComponentInChildren<HealthBar>()`.

#### ISSUE-402 [P1] - Texture memory 620MB (budget 512MB)
- **Test:** `Profiler.GetRuntimeMemorySizeLong()` for textures
- **Expected:** < 512MB
- **Actual:** 620MB
- **Root Cause:** 40+ textures using RGBA32 instead of ASTC 4x4
- **Fix Prompt for Agent 4:**
  > Run `AssetValidator` to find uncompressed textures. Re-import with ASTC 4x4 (mobile) / BC7 (desktop). Update `AtlasBuilder` to enforce compression.

### 🟣 Agent 5 - Backend (X issues)

#### ISSUE-501 [P0] - Battle result not saving trophies
- **Test:** Complete battle, check player trophies
- **Expected:** Winner +32, Loser -32
- **Actual:** Trophies unchanged
- **Root Cause:** `PlayerService.saveBattleResult()` missing `UPDATE players SET trophies = trophies + ?`
- **Fix Prompt for Agent 5:**
  > In `PlayerService.saveBattleResult()`, add transaction that updates both players' trophies based on `result.player1TrophyChange` and `result.player2TrophyChange`.

#### ISSUE-502 [P1] - Shop purchase doesn't verify IAP receipt
- **Test:** Purchase gem pack with fake receipt
- **Expected:** Rejected, error "INVALID_RECEIPT"
- **Actual:** Grants gems anyway
- **Root Cause:** `ShopService.purchase()` skips `verifyReceipt()` for testing
- **Fix Prompt for Agent 5:**
  > In `ShopService.ts`, implement `verifyAppleReceipt()` and `verifyGoogleReceipt()` using official APIs. Only grant rewards after verification. Add `isTestMode` flag for CI.

### 🟠 Agent 6 - Testing (X issues)

#### ISSUE-601 [P1] - No CI pipeline for Unity tests
- **Test:** Push to feature branch
- **Expected:** GitHub Actions runs EditMode + PlayMode tests
- **Actual:** No workflow file
- **Root Cause:** Missing `.github/workflows/unity-test.yml`
- **Fix Prompt for Agent 6:**
  > Create `.github/workflows/unity-test.yml` with `game-ci/unity-test-runner@v2`. Run EditMode + PlayMode tests. Upload coverage to Codecov.

#### ISSUE-602 [P2] - No desync test in integration suite
- **Test:** Inject 200ms lag, verify reconciliation
- **Expected:** Test passes
- **Actual:** Test doesn't exist
- **Root Cause:** Missing `NetworkDesyncTest.cs` in integration tests
- **Fix Prompt for Agent 6:**
  > Add `NetworkDesyncTest.cs` in `Assets/Tests/Integration/`. Use `NetworkClient` to simulate 200ms RTT, play cards, verify `EventBus.OnReconciliation` fires and entity states match server.

---

## 🔧 FIX PROMPT TEMPLATE (Use for Every Issue)

When creating GitHub Issues for agents, use this format:

```markdown
## 🐛 ISSUE-XXX [P0/P1/P2/P3] - <Short Title>

**Agent:** @agent1 / @agent2 / @agent3 / @agent4 / @agent5 / @agent6
**Cycle:** #<N>
**Test:** <Test name from matrix>
**Severity:** Critical / High / Medium / Low

### Expected
<What should happen>

### Actual
<What happened>

### Root Cause
<Technical explanation>

### Reproduction
1. Step 1
2. Step 2
3. Step 3

### Fix Prompt for Agent X
> <Copy-paste the fix prompt from above>

### Files Likely Affected
- `Assets/Scripts/Battle/Simulation/Unit.cs`
- `server/src/battle/BattleServer.ts`

### Verification
After fix, QA will re-run: `<specific test command>`
```

---

## 📋 QA COMMANDS (Your Daily Toolkit)

```bash
# 1. Pull all latest
cd ../CRClone-agent7
git fetch origin
git merge origin/feature/battle-simulation-core
git merge origin/feature/networking-multiplayer
git merge origin/feature/ui-ux-implementation
git merge origin/feature/asset-pipeline-card-db
git merge origin/feature/database-backend-services
git merge origin/feature/testing-ci-cd

# 2. Start full stack
docker-compose -f docker-compose.test.yml up -d
# Wait for health checks
curl -f http://localhost:3000/health

# 3. Run integration tests
cd server && npm run test:integration
cd ../Assets/Tests && ./run-integration-tests.sh

# 4. Run determinism test
cd ../Assets/Tests && ./run-determinism-test.sh 100

# 5. Run performance benchmarks
cd ../Assets/Tests && ./run-performance-benchmarks.sh

# 6. Generate issue sheet
./generate-issue-sheet.sh > QA_ISSUE_SHEET_$(date +%Y%m%d_%H%M).md

# 7. Create GitHub issues (if any)
./create-github-issues.sh QA_ISSUE_SHEET_*.md

# 8. Wait for agents to fix, then repeat
```

---

## 🚦 EXIT CRITERIA (Release Ready)

**ALL must be GREEN:**

| Category | Criteria |
|----------|----------|
| **Critical Issues** | 0 |
| **High Issues** | 0 |
| **Determinism** | 100/100 seeds pass |
| **Integration Tests** | 100% pass |
| **E2E Tests** | 100% pass |
| **Performance** | All thresholds met |
| **Security Scan** | 0 critical/high |
| **Code Coverage** | > 80% |
| **Memory Leaks** | 0 bytes/frame growth |

---

## 📋 QUICK START (When You Start)

```bash
# 1. Setup worktree
git worktree add ../CRClone-agent7 feature/qa-integration
cd ../CRClone-agent7
cp .env.example .env
# Fill DB creds

# 2. Verify all 6 agents have pushed
git fetch origin
git log origin/feature/battle-simulation-core --oneline -1
git log origin/feature/networking-multiplayer --oneline -1
# ... check all 6

# 3. Run first cycle
./run-qa-cycle.sh

# 4. If issues found → create GitHub issues → wait for fixes → repeat
# 5. If clean → run final benchmarks → sign off
```

---

## 📞 ESCALATION

| Situation | Action |
|-----------|--------|
| Agent unresponsive > 48h | Escalate to project lead, reassign issue |
| Recurring same issue (>2 cycles) | Root cause analysis meeting |
| Blocker affects multiple agents | Cross-agent sync meeting |
| Release deadline at risk | Triage: P0 only, defer P1+ |

---

## 📌 KEY PRINCIPLES

1. **YOU DON'T FIX** - You find, document, orchestrate
2. **EVERY ISSUE GETS A FIX PROMPT** - Agents copy-paste and implement
3. **CYCLE REPEATS** - Until ZERO critical issues
3. **DOCUMENT EVERYTHING** - Issue sheet is the source of truth
4. **VERIFY FIXES** - Don't trust "fixed" - re-run the exact test
5. **NO SCOPE CREEP** - Only test what's in the matrix; new features = new cycle

---

**You are the quality gate. No code ships without your sign-off.** 🛡️

*Generated: 2026-09-12*  
*Run this agent ONLY after all 6 agents declare "feature complete"*