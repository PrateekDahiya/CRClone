# Agent 6: Testing, CI/CD & DevOps
## Workstream: Test Infrastructure, GitHub Actions, Performance Benchmarks, Monitoring

---

## 🎯 YOUR MISSION
Build **comprehensive test infrastructure** and **CI/CD pipelines** - unit/integration/E2E/performance tests, automated builds, deployments, monitoring, and quality gates. You ensure the game is reliable and deployable.

---

## 📁 FILES YOU OWN (Exclusive Write Access)

### Unity Tests (Assets/Tests/)
```
Assets/Tests/
├── Unit/
│   ├── BattleSimulationTests.cs      # Elixir, card cycle, combat, buildings, spells, champions, targeting
│   ├── ElixirSystemTests.cs
│   ├── CardCycleTests.cs
│   ├── UnitCombatTests.cs
│   ├── BuildingTests.cs
│   ├── SpellTests.cs
│   ├── ChampionTests.cs
│   └── TargetingTests.cs
├── Integration/
│   ├── BattleFlowTests.cs            # Full 1v1, overtime, draw, reconnection
│   ├── NetworkTests.cs               # Input validation, rate limiting, desync recovery
│   └── DatabaseTests.cs              # Player lifecycle, clan, shop, quests
├── Performance/
│   ├── SimulationBenchmark.cs        # 60 FPS target, memory stability
│   ├── MemoryStabilityTest.cs        # 5min battle, <50MB growth
│   ├── PathfindingBenchmark.cs       # 10k paths < 100ms
│   └── NetworkLatencyTest.cs         # RTT handling, reconciliation
└── TestFixtures/
    ├── TestDecks.cs                  # Balanced, spell-heavy, tank, cycle decks
    ├── TestScenarios.cs              # Tank vs swarm, air vs ground, etc.
    └── BattleTestRunner.cs           # Already exists - enhance
```

### Server Tests (server/tests/)
```
server/tests/
├── unit/
│   ├── rng.test.ts                   # DeterministicRNG
│   ├── matchmaking.test.ts           # Queue, trophy diff, 2v2
│   ├── playerService.test.ts         # Auth, decks, progression
│   ├── clanService.test.ts           # CRUD, chat, donations
│   ├── shopService.test.ts           # Offers, purchases, IAP
│   ├── questService.test.ts          # Progress, rewards, scheduling
│   ├── seasonService.test.ts         # Battle pass, tiers
│   ├── tournamentService.test.ts     # Classic/grand/draft
│   └── replayService.test.ts         # Store, retrieve, search
├── integration/
│   ├── battleFlow.test.ts            # Server battle server + 2 clients
│   ├── matchmaking.test.ts           # Full queue -> battle -> result
│   ├── database.test.ts              # Repository CRUD with test DB
│   └── replay.test.ts                # Record -> store -> retrieve
└── load/
    └── k6-load-test.js               # 1000 concurrent, 100 battles/sec
```

### CI/CD (`.github/workflows/`)
```
.github/workflows/
├── unity-test.yml              # Unity EditMode/PlayMode tests
├── server-test.yml             # Server unit/integration tests
├── e2e-test.yml                # Playwright E2E tests
├── performance.yml             # Weekly benchmarks (scheduled)
├── deploy.yml                  # Staging deploy (on main merge)
├── dependency-check.yml        # npm audit, Unity package audit
└── pr-checks.yml               # Lint, typecheck, test on every PR
```

### Docker
```
docker-compose.yml              # Already exists - enhance
docker-compose.test.yml         # Test environment (MySQL, Redis, Server, Unity headless)
docker-compose.prod.yml         # Production (multi-replica, load balancer)
```

### Monitoring (server/src/utils/)
```
server/src/utils/
├── MetricsCollector.ts         # Prometheus: battles_active, battles_total, desync_rate, player_online, battle_duration
├── HealthCheck.ts              # /health (DB, Redis, active battles)
├── AlertManager.ts             # Desync spike, battle server crash, DB connection pool exhausted
└── StructuredLogger.ts         # Already done - enhance with correlation IDs
```

### Code Quality
```
# Root
.eslintrc.js                    # Server TypeScript linting
stylelint.config.js             # CSS/SCSS (if any)
.editorconfig                   # Consistent formatting
# Unity
Assets/Tests/AssemblyDefinition.asmdef
Assets/Scripts/AssemblyDefinition.asmdef
```

---

## ✅ DELIVERABLES CHECKLIST

### Unit Tests (100% Pass Required)
- [ ] **Simulation**: Elixir rates, card cycle, unit combat, building behavior, spell effects, champion abilities, targeting/pathfinding
- [ ] **Server**: RNG determinism, matchmaking logic, player/clan/shop/quest/season/tournament/replay services
- [ ] **Coverage**: >80% on simulation + services code

### Integration Tests (100% Pass Required)
- [ ] **Battle Flow**: Matchmake -> battle -> play cards -> win/lose/draw -> result screen -> replay saved
- [ ] **Overtime**: 1-1 crowns -> sudden death -> first tower wins
- [ ] **Draw**: No towers destroyed in overtime -> draw
- [ ] **Reconnection**: Disconnect mid-battle -> reconnect -> receive current state
- [ ] **Network**: Invalid input rejected, rate limited, desync detected+corrected
- [ ] **Database**: Register -> battle -> progress -> clan -> shop -> tournament

### E2E Tests (Playwright - Critical Paths)
- [ ] **Onboarding**: Guest login -> tutorial -> first battle -> deck builder
- [ ] **Deck Building**: Drag 8 cards -> save -> use in battle
- [ ] **Chest Unlock**: Win battle -> chest appears -> unlock -> rewards received
- [ ] **Shop Purchase**: Buy gold offer -> verify gold added
- [ ] **Clan Flow**: Create clan -> invite -> donate -> request

### Performance Tests (Benchmarks + Regression Detection)
- [ ] **Simulation**: 60Hz tick < 4ms (16.67ms budget), 5min battle < 50MB memory growth
- [ ] **Pathfinding**: 10k A* paths < 100ms
- [ ] **Network**: 1000 concurrent WS, 100 battles/sec, <100ms p99 latency
- [ ] **Database**: 10k QPS reads, 1k QPS writes, <50ms p99
- [ ] **Regression Gate**: Fail if >5% slower than baseline

### CI/CD Pipelines
- [ ] **PR Checks**: `dotnet build` (Unity), `npm run lint` + `npm run test` (server), typecheck
- [ ] **Unity Tests**: EditMode + PlayMode on every push (Ubuntu runner + Unity license)
- [ ] **Server Tests**: Unit + integration on every push
- [ ] **E2E**: Playwright on merge to main (staging deploy first)
- [ ] **Performance**: Weekly scheduled run, compare to baseline, alert on regression
- [ ] **Deploy**: Docker build -> push to registry -> rolling update on staging
- [ ] **Security**: `npm audit`, Unity package vulnerability scan, dependency check

### Docker Environments
- [ ] **docker-compose.test.yml**: MySQL, Redis, Server, Unity headless test runner
- [ ] **docker-compose.prod.yml**: 3x Server replicas, nginx LB, MySQL primary/replica, Redis cluster
- [ ] **Health Checks**: All services have `/health` endpoints

### Monitoring & Alerting
- [ ] **Prometheus Metrics**: `battles_active`, `battles_total`, `desync_rate`, `player_online`, `battle_duration_seconds`, `matchmaking_queue_size`, `db_connections_active`
- [ ] **Grafana Dashboards**: Battle throughput, desync rate, player retention, server health
- [ ] **Alerts**: Desync rate > 1%, battle server down > 30s, DB pool > 80%, memory > 90%
- [ ] **Structured Logging**: Correlation IDs across client->server->DB for debugging

---

## 📚 REFERENCE DOCS
| Doc | Purpose |
|-----|---------|
| `docs/planning/phase1/TEST_SPEC.md` | **COMPLETE TEST PLAN** - All sections (unit, integration, E2E, performance, regression) |
| `docs/planning/phase1/ARCHITECTURE.md` Section 5.1, 10 | Performance targets, testing architecture |
| `docs/planning/phase1/MECHANICS.md` | Expected behaviors for test assertions |

---

## 🔗 DEPENDENCIES

| Dependency | Status | Notes |
|------------|--------|-------|
| Agent 1 Simulation | Parallel | Write tests for their code; they fix bugs you find |
| Agent 2 Network | Parallel | Integration tests need their client/server |
| Agent 3 UI | Parallel | E2E tests use their screens |
| Agent 4 Assets | Parallel | Tests need prefabs for simulation tests |
| Agent 5 Backend | Parallel | Integration tests need real DB |

---

## 🧪 QUALITY GATES (From TEST_SPEC.md - MUST ENFORCE)

| Gate | Threshold | Blocking |
|------|-----------|----------|
| Unit Test Pass Rate | 100% | Yes |
| Integration Test Pass Rate | 100% | Yes |
| Code Coverage | >80% | Yes |
| Critical Bugs | 0 | Yes |
| Performance Regression | <5% slower | Yes |
| Memory Leak | 0 bytes/frame | Yes |
| Security Vulnerabilities | 0 critical/high | Yes |

---

## 🚫 DO NOT TOUCH
- Implementation code - you only write **tests**, **CI configs**, **monitoring**, **Docker files**
- If you find bugs, create GitHub Issues with `bug` label, tag relevant agent

---

## 🌿 GIT WORKTREE SETUP (Run All 6 Agents Simultaneously)

**Each agent works in their own isolated worktree - no conflicts, no waiting.**

```bash
# Run ONCE per agent (each agent runs their own setup):

# Agent 6 - Testing
git worktree add ../CRClone-agent6 feature/testing-ci-cd
cd ../CRClone-agent6
cp .env.example .env   # Fill in your DB credentials
# Start working...
```

**Each worktree is a complete, independent copy of the repo** - you can build, run tests, and commit independently. No stepping on each other's toes.

### Branch & Workflow (Per Worktree)
```bash
# Inside your worktree directory:
git checkout -b feature/testing-ci-cd  # Already set by worktree add
# Phase 1: Test scaffolding + CI pipelines
# Phase 2: Unit tests as features land
# Phase 3: Integration + E2E + Performance
# Phase 4: Monitoring + Dashboards + Alerts
git push origin feature/testing-ci-cd
```

**Integration Points (Cross-Agent Sync via PRs):**
- Continuous: PR checks run on every push
- Weekly: Performance benchmarks + security scan
- On Merge: Full test suite + staging deploy
- Release: Production deploy (manual approval)
- Continuous: Writes tests for all 5 other agents' code

---

## 📋 QUICK START
```bash
# 1. Add test assembly definitions in Unity
# 2. Write first unit test (ElixirSystemTests)
# 3. Setup GitHub Actions unity-test.yml
# 4. Run locally: ./run-tests.sh

# Server
cd server
npm test  # Runs Jest

# Load test
cd server/tests/load
k6 run k6-load-test.js
```

**Good luck! You're the quality gatekeeper.** 🛡️