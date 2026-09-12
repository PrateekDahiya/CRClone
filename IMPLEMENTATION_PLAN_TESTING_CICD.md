# Implementation Plan: Testing, CI/CD & DevOps Infrastructure

## Objective
Build comprehensive test infrastructure and CI/CD pipelines for the Clash Royale Clone - covering Unity client tests (unit/integration/performance), Server tests (unit/integration/load), GitHub Actions workflows, Docker environments, and monitoring/alerting.

## Current Implementation Status

### Existing Assets (From Repository Analysis)
| Component | Status | Location |
|-----------|--------|----------|
| Unity BattleTestRunner | Basic MonoBehaviour test runner | `Assets/Scripts/Testing/BattleTestRunner.cs:1` |
| Server package.json | Jest, ESLint, TypeScript configured | `server/package.json:1` |
| Docker Compose (dev) | MySQL, Redis, Server | `docker-compose.yml:1` |
| Server Structured Logger | Winston logger with correlation IDs | `server/src/utils/logger.ts:1` |
| Deterministic RNG | Xorshift implementation | `server/src/utils/rng.ts:1` |
| TEST_SPEC.md | Complete test specification | `docs/planning/phase1/TEST_SPEC.md:1` |
| ARCHITECTURE.md | Architecture with testing section | `docs/planning/phase1/ARCHITECTURE.md:958` |

### Missing (To Be Created)
- Unity Assembly Definitions (.asmdef) for test assemblies
- Complete Unity Unit Test Suite (NUnit)
- Complete Unity Integration Test Suite
- Complete Unity Performance Benchmarks
- Server Unit Tests (Jest)
- Server Integration Tests (Jest + Test DB)
- Server Load Tests (k6)
- GitHub Actions Workflows (7 workflows)
- Docker Compose for Test Environment
- Docker Compose for Production
- Prometheus Metrics Collector
- Health Check Endpoints
- Alert Manager
- Enhanced Structured Logger
- E2E Tests (Playwright)
- Code Quality configs (.eslintrc.js, .editorconfig)

---

## Implementation Plan

### Phase 1: Foundation & Test Infrastructure Setup

#### Step 1.1: Unity Assembly Definitions
**Files to Create:**
- `Assets/Tests/AssemblyDefinition.asmdef` - Test assembly definition
- `Assets/Scripts/AssemblyDefinition.asmdef` - Main code assembly definition (if missing)

**Rationale:** Unity requires Assembly Definitions for test isolation and proper compilation. The test assembly must reference the main assembly and NUnit.

#### Step 1.2: Unity Test Fixtures & Base Classes
**Files to Create:**
- `Assets/Tests/TestFixtures/TestDecks.cs` - Balanced, spell-heavy, tank, cycle decks
- `Assets/Tests/TestFixtures/TestScenarios.cs` - Pre-built battle scenarios
- `Assets/Tests/TestFixtures/BattleTestBase.cs` - Base class with simulation factory
- `Assets/Tests/TestFixtures/BattleTestRunner.cs` - Enhanced test runner (extends existing)

**Rationale:** Shared test infrastructure reduces duplication and ensures consistent test setup.

#### Step 1.3: Unity Unit Test Suite
**Files to Create (following TEST_SPEC.md specifications):**
- `Assets/Tests/Unit/ElixirSystemTests.cs` - Elixir generation, capping, double elixir
- `Assets/Tests/Unit/CardCycleTests.cs` - Initial hand, card draw, cycle wrapping
- `Assets/Tests/Unit/UnitCombatTests.cs` - Knight vs Goblin, 3 Goblins vs Knight, ranged attacks, splash
- `Assets/Tests/Unit/BuildingTests.cs` - Cannon targeting, Tesla retract, spawner buildings, lifetime
- `Assets/Tests/Unit/SpellTests.cs` - Fireball, Zap, Poison, Freeze, Log, Tornado, Graveyard
- `Assets/Tests/Unit/ChampionTests.cs` - Archer Queen, Skeleton King, Mighty Miner, cooldowns
- `Assets/Tests/Unit/TargetingTests.cs` - Closest target, building targeters, retarget on death

**Total: ~70 unit tests covering all simulation mechanics**

#### Step 1.4: Unity Integration Test Suite
**Files to Create:**
- `Assets/Tests/Integration/BattleFlowTests.cs` - Full 1v1, overtime, draw, reconnection
- `Assets/Tests/Integration/NetworkTests.cs` - Input validation, rate limiting, desync recovery

**Rationale:** Integration tests verify full battle flow with simulated network conditions.

#### Step 1.5: Unity Performance Benchmarks
**Files to Create:**
- `Assets/Tests/Performance/SimulationBenchmark.cs` - 60 FPS target, 5min battle < 50MB growth
- `Assets/Tests/Performance/MemoryStabilityTest.cs` - Long-running memory stability
- `Assets/Tests/Performance/PathfindingBenchmark.cs` - 10k paths < 100ms

---

### Phase 2: Server Test Infrastructure

#### Step 2.1: Server Test Configuration
**Files to Modify:**
- `server/package.json` - Add test scripts, install additional deps (supertest, testcontainers)

**Files to Create:**
- `server/jest.config.js` - Jest configuration with TypeScript support
- `server/tests/setup.ts` - Test setup/teardown (DB, Redis)
- `server/tests/helpers/testDatabase.ts` - Test database helpers

#### Step 2.2: Server Unit Tests
**Files to Create (following TEST_SPEC.md):**
- `server/tests/unit/rng.test.ts` - DeterministicRNG determinism, distribution
- `server/tests/unit/matchmaking.test.ts` - Queue, trophy diff, 2v2 matching
- `server/tests/unit/playerService.test.ts` - Auth, decks, progression
- `server/tests/unit/clanService.test.ts` - CRUD, chat, donations
- `server/tests/unit/shopService.test.ts` - Offers, purchases, IAP validation
- `server/tests/unit/questService.test.ts` - Progress, rewards, scheduling
- `server/tests/unit/seasonService.test.ts` - Battle pass, tiers
- `server/tests/unit/tournamentService.test.ts` - Classic/grand/draft tournaments
- `server/tests/unit/replayService.test.ts` - Store, retrieve, search

#### Step 2.3: Server Integration Tests
**Files to Create:**
- `server/tests/integration/battleFlow.test.ts` - Server battle server + 2 clients
- `server/tests/integration/matchmaking.test.ts` - Full queue -> battle -> result
- `server/tests/integration/database.test.ts` - Repository CRUD with test DB
- `server/tests/integration/replay.test.ts` - Record -> store -> retrieve

#### Step 2.4: Server Load Tests (k6)
**Files to Create:**
- `server/tests/load/k6-load-test.js` - 1000 concurrent, 100 battles/sec, <100ms p99

---

### Phase 3: CI/CD Pipeline (GitHub Actions)

#### Step 3.1: PR Checks Workflow
**File to Create:** `.github/workflows/pr-checks.yml`
- Runs on every PR
- Unity: `dotnet build` (or Unity CLI), lint
- Server: `npm run lint` + `npm run typecheck` + `npm run test` (unit only)
- Code quality gates

#### Step 3.2: Unity Tests Workflow
**File to Create:** `.github/workflows/unity-test.yml`
- Runs on push to any branch
- Ubuntu runner + Unity license
- EditMode + PlayMode tests
- Upload test results as artifact
- Code coverage report

#### Step 3.3: Server Tests Workflow
**File to Create:** `.github/workflows/server-test.yml`
- Runs on push to any branch
- MySQL + Redis services
- Unit + Integration tests
- Coverage report

#### Step 3.4: E2E Tests Workflow
**File to Create:** `.github/workflows/e2e-test.yml`
- Runs on merge to main (after staging deploy)
- Playwright tests against staging
- Critical user journeys

#### Step 3.5: Performance Benchmarks Workflow
**File to Create:** `.github/workflows/performance.yml`
- Scheduled weekly + manual dispatch
- Runs Unity performance benchmarks
- Runs k6 load test
- Compares to baseline, fails on >5% regression

#### Step 3.6: Deploy Workflow
**File to Create:** `.github/workflows/deploy.yml`
- Triggered on merge to main
- Docker build -> push to registry
- Rolling update on staging
- Manual approval for production

#### Step 3.7: Security/Dependency Check Workflow
**File to Create:** `.github/workflows/dependency-check.yml`
- Weekly scheduled
- `npm audit` for server
- Unity package vulnerability scan
- Fail on critical/high vulnerabilities

---

### Phase 4: Docker Environments

#### Step 4.1: Test Environment Docker Compose
**File to Create:** `docker-compose.test.yml`
- MySQL (test database)
- Redis
- Server (test mode)
- Unity headless test runner
- Health checks for all services

#### Step 4.2: Production Docker Compose
**File to Enhance:** `docker-compose.prod.yml`
- 3x Server replicas
- nginx load balancer
- MySQL primary/replica
- Redis cluster
- Health checks on all services

---

### Phase 5: Monitoring & Observability

#### Step 5.1: Prometheus Metrics Collector
**File to Create:** `server/src/utils/MetricsCollector.ts`
- Metrics: `battles_active`, `battles_total`, `desync_rate`, `player_online`, `battle_duration_seconds`, `matchmaking_queue_size`, `db_connections_active`
- `/metrics` endpoint for Prometheus scraping

#### Step 5.2: Health Check Endpoints
**File to Create:** `server/src/utils/HealthCheck.ts`
- `/health` endpoint
- Checks: DB connectivity, Redis connectivity, active battles count
- Returns JSON with status and details

#### Step 5.3: Alert Manager
**File to Create:** `server/src/utils/AlertManager.ts`
- Alert rules: Desync rate > 1%, battle server down > 30s, DB pool > 80%, memory > 90%
- Webhook/email/Slack integration ready

#### Step 5.4: Enhanced Structured Logger
**File to Modify:** `server/src/utils/logger.ts`
- Add correlation ID propagation
- Structured JSON output
- Battle context enrichment

---

### Phase 6: Code Quality & E2E

#### Step 6.1: Code Quality Configs
**Files to Create:**
- `.eslintrc.js` - Server TypeScript linting
- `.editorconfig` - Consistent formatting across editors
- `stylelint.config.js` - CSS/SCSS (if any)

#### Step 6.2: E2E Tests (Playwright)
**Files to Create:**
- `e2e/critical-journeys.spec.ts` - Onboarding, deck building, chest unlock, shop, clan
- `e2e/battle.spec.ts` - Battle gameplay E2E
- `playwright.config.ts` - Playwright configuration

---

## Affected Components Summary

| Layer | Components | Files |
|-------|------------|-------|
| Unity Tests | 7 unit + 2 integration + 3 perf test files | 12 new files |
| Server Tests | 9 unit + 4 integration + 1 load test files | 14 new files |
| CI/CD | 7 GitHub Actions workflows | 7 new files |
| Docker | 2 compose files (1 new, 1 enhanced) | 2 files |
| Monitoring | 3 new utils + 1 enhanced | 4 files |
| Code Quality | 3 config files | 3 new files |
| E2E | 3 test files + config | 4 new files |
| **Total** | | **~50 new/modified files** |

---

## Reusable Existing Functionality

| Existing Code | Reuse For |
|---------------|-----------|
| `BattleTestRunner.cs` | Base for all Unity test runners |
| `DeterministicRNG` | Server unit test for RNG determinism |
| `StructuredLogger` | Base for enhanced logging with correlation IDs |
| `docker-compose.yml` | Base for test/prod compose files |
| `TEST_SPEC.md` | All test specifications and assertions |
| `server/src/types/index.ts` | Type definitions for test helpers |

---

## Validation Commands

### Local Development
```bash
# Unity Tests (requires Unity Editor)
cd /path/to/unity/project
/Applications/Unity/Hub/Editor/2022.3.20f1/Unity.app/Contents/MacOS/Unity -batchmode -runTests -projectPath . -testResults results.xml -logFile -

# Server Tests
cd server
npm run test           # Unit tests
npm run test:integration  # Integration tests (requires DB)
npm run test:coverage  # With coverage report
npm run lint           # ESLint
npm run typecheck      # TypeScript check

# Load Test
cd server/tests/load
k6 run k6-load-test.js

# Docker Test Environment
docker-compose -f docker-compose.test.yml up --build

# E2E Tests
npx playwright test
```

### CI Validation
```bash
# All workflows trigger on push/PR
# Check GitHub Actions UI for results
# Performance workflow: workflow_dispatch or schedule
```

---

## Risk Assessment

| Risk | Likelihood | Impact | Mitigation |
|------|------------|--------|------------|
| Unity headless licensing in CI | Medium | High | Use game-ci/unity-installer with personal license |
| Test database flakiness | Medium | Medium | Use testcontainers, proper cleanup |
| k6 load test resource limits | Medium | Medium | Configure GitHub runners with adequate resources |
| Playwright browser install time | Low | Low | Cache browser binaries |
| Memory benchmark variance | High | Medium | Run multiple iterations, use median |
| Desync test reliability | Medium | High | Deterministic seeds, controlled conditions |

---

## Rollback Considerations

- All workflows are additive (new files only)
- Docker compose files are versioned
- No database migrations required
- Can disable individual workflows via GitHub UI
- Monitoring endpoints are read-only additions

---

## Dependencies

| Dependency | Required By | Status |
|------------|-------------|--------|
| Agent 1 (Simulation) | Unity unit/integration tests | Parallel |
| Agent 2 (Network) | Integration tests, E2E | Parallel |
| Agent 3 (UI) | E2E tests, deck building tests | Parallel |
| Agent 4 (Assets) | Test prefabs, card data | Parallel |
| Agent 5 (Backend) | Server integration tests, DB | Parallel |

---

## Approval Checklist

- [ ] Plan reviewed and understood
- [ ] Technology choices confirmed (NUnit, Jest, k6, Playwright, Prometheus)
- [ ] Resource allocation for CI runners confirmed
- [ ] Unity license for CI confirmed
- [ ] Test database strategy confirmed (testcontainers vs dedicated)
- [ ] Monitoring stack confirmed (Prometheus + Grafana or cloud)
- [ ] Alert notification channels identified

---

**Implementation has not started. Approve this plan to begin execution.**