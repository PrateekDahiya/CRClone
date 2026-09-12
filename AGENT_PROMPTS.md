# AGENT PROMPTS FOR PARALLEL DEVELOPMENT
## Clash Royale Clone - 6 Independent Workstreams

---

## 📋 PROJECT CONTEXT (All Agents Read This)

**Project**: Clash Royale Clone (local dev only)  
**Phase**: 2 (Implementation) - Phase 1 Planning **COMPLETE**  
**Tech Stack**: Unity 2022.3+ (C#) + Node.js/TypeScript Server + MySQL (Aiven)  
**Architecture**: Deterministic 60Hz lockstep simulation, authoritative server, client prediction  

**Key Documentation** (reference as needed):
- `docs/planning/phase1/CARDS_DATABASE.md` - All 122+ card stats
- `docs/planning/phase1/MECHANICS.md` - All game mechanics
- `docs/planning/phase1/ARCHITECTURE.md` - Technical architecture
- `docs/planning/phase1/UI_UX_SPEC.md` - UI specifications
- `docs/planning/phase1/DATABASE_SCHEMA.md` - MySQL schema
- `docs/planning/phase1/TEST_SPEC.md` - Test requirements

**Current State**: Core Unity project structure + Server skeleton created. Battle simulation ~60% complete. No assets yet.

---

## ⚠️ PARALLEL WORK RULES (ALL AGENTS)

1. **DO NOT MODIFY**: `Assets/Scripts/Core/GameTypes.cs`, `Services.cs`, `EventBus.cs` - these are shared contracts
2. **DO NOT MODIFY**: `server/src/types/index.ts` - shared TypeScript types
3. **COMMUNICATE VIA**: EventBus (Unity) / NetworkMessage types (Server) - no direct references between workstreams
3. **BRANCH STRATEGY**: Each agent works on own feature branch, PR to main
4. **CONFLICTS**: If you need to touch shared files, STOP and note in PR description
5. **DEPENDENCIES**: Listed in each prompt - wait for dependency PRs if needed

---

## 🤖 AGENT 1: BATTLE SIMULATION CORE
### **Focus**: Complete deterministic battle simulation (P0 priority)

### **Deliverables**
- [ ] Complete `BattleSimulation.cs` - all tick phases working
- [ ] Complete `Entity.cs` + `Unit.cs` + `Building.cs` + `Projectile.cs` + `SpellEffect.cs` + `Tower.cs`
- [ ] Complete `Pathfinding.cs` - A* with half-tile grid, river blocking
- [ ] All card mechanics implemented (charge, splash, chain, pierce, spawn, invisibility, shields)
- [ ] Champion abilities (AQ cloak, SK summon, MM dash)
- [ ] Elixir generation (normal/double/triple), card cycle, deploy validation
- [ ] Win conditions (crowns, king tower, overtime, draw)
- [ ] Replay event logging (deterministic)

### **Files to Work On**
```
Assets/Scripts/Battle/Simulation/
├── BattleSimulation.cs      # Main simulation loop
├── Entity.cs                # Base entity
├── Unit.cs                  # Troop logic
├── Building.cs              # Building logic
├── Projectile.cs            # Projectile logic
├── SpellEffect.cs           # Spell logic
├── Tower.cs                 # Tower logic
└── Pathfinding.cs           # A* pathfinding
```

### **Reference Docs**
- `docs/planning/phase1/MECHANICS.md` - All mechanics specs
- `docs/planning/phase1/CARDS_DATABASE.md` - Card stats for mechanics
- `docs/planning/phase1/TEST_SPEC.md` - Unit test cases (Section 2.1)

### **Dependencies**
- Needs: `GameTypes.cs`, `Services.cs`, `EventBus.cs` (already done)
- Blocks: Agent 2 (networking needs simulation), Agent 3 (UI needs simulation events)

### **Testing**
- Run `BattleTestRunner` in Unity Editor
- All unit tests in `TEST_SPEC.md` Section 2.1 must pass
- Determinism test: same seed = identical event log

### **Branch**: `feature/battle-simulation-core`

---

## 🤖 AGENT 2: NETWORKING & MULTIPLAYER
### **Focus**: Authoritative server, WebSocket protocol, matchmaking, reconciliation

### **Deliverables**
- [ ] Complete `NetworkClient.cs` - WebSocket + Protobuf serialization
- [ ] Complete `ConnectionManager.ts` + `NetworkClient.ts` (server)
- [ ] Complete `Matchmaker.ts` - trophy-based queues, 2v2 support
- [ ] Complete `BattleServer.ts` - authoritative simulation, input validation
- [ ] Input queue + acknowledgment + retransmission
- [ ] Server-side reconciliation (desync detection + correction)
- [ ] Replay recording (seed + inputs → deterministic playback)
- [ ] Heartbeat + reconnection logic

### **Files to Work On**
```
# Unity Client
Assets/Scripts/Network/
├── NetworkClient.cs          # WebSocket client
├── MessageTypes.cs           # Protobuf messages
├── Serialization.cs          # Binary serialization
└── ReconnectionManager.cs    # Reconnect logic

# Server
server/src/network/
├── ConnectionManager.ts      # WS connection handling
├── NetworkClient.ts          # Server-side client wrapper
├── MessageHandler.ts         # Message routing
└── Protocol.ts               # Protobuf definitions

server/src/matchmaking/
├── Matchmaker.ts             # Queue management
├── Queue.ts                  # Priority queue
└── RatingSystem.ts           # Trophy/ELO

server/src/battle/
├── BattleServer.ts           # Authoritative battle
├── BattleSimulation.ts       # Server simulation (port of Unity)
├── EntityManager.ts          # Entity sync
└── ReplayRecorder.ts         # Replay recording
```

### **Reference Docs**
- `docs/planning/phase1/ARCHITECTURE.md` - Networking section (Section 2.4, 3, 4)
- `docs/planning/phase1/MECHANICS.md` - Input validation rules
- `server/src/types/index.ts` - **MUST MATCH** these types exactly

### **Dependencies**
- Needs: Agent 1's `BattleSimulation` logic (port to server)
- Needs: `server/src/types/index.ts` (already done)
- Blocks: Agent 3 (needs network for multiplayer UI)

### **Testing**
- `npm test` in server - integration tests for matchmaking + battle flow
- 2-client local test: connect, matchmake, play cards, verify sync
- Desync test: inject lag, verify reconciliation

### **Branch**: `feature/networking-multiplayer`

---

## 🤖 AGENT 3: UI/UX IMPLEMENTATION
### **Focus**: All game screens, battle HUD, deck builder, animations

### **Deliverables**
- [ ] **MainMenu/Lobby** - Battle button, chest slots, quest bar, navigation
- [ ] **DeckBuilder** - Drag-drop 8 slots, collection grid, filters, validation, avg elixir
- [ ] **Battle HUD** - Hand bar (4 cards), elixir bar (10 segments), next card preview, timer, crowns
- [ ] **BattleResult** - Crowns, trophy change, rewards, replay/share/rematch buttons
- [ ] **Shop** - Daily/Special/Chests tabs, purchase flow, IAP integration points
- [ ] **Clan** - Chat, members, war, capital tabs, donation requests
- [ ] **Profile** - Stats, battle log, settings
- [ ] **Animations** - Card select/deploy, elixir pulse, tower shake, screen transitions
- [ ] **Responsive** - Mobile landscape, tablet, desktop breakpoints
- [ ] **Accessibility** - Color blind, high contrast, reduce motion, screen reader

### **Files to Work On**
```
Assets/Scripts/UI/
├── UIManager.cs              # Screen management
├── Screens/
│   ├── MainMenuScreen.cs
│   ├── LobbyScreen.cs
│   ├── DeckBuilderScreen.cs
│   ├── BattleScreen.cs
│   ├── BattleResultScreen.cs
│   ├── ShopScreen.cs
│   ├── ClanScreen.cs
│   ├── ProfileScreen.cs
│   └── SettingsScreen.cs
├── Components/
│   ├── HandBar.cs            # 4-card hand + elixir
│   ├── ElixirBar.cs          # 10-segment bar
│   ├── TowerHealthUI.cs      # Tower HP bars
│   ├── BattleHUD.cs          # Timer, crowns, pause
│   ├── DeckSlotUI.cs         # Drag-drop deck slot
│   ├── CollectionCardUI.cs   # Draggable card
│   ├── ChestSlotUI.cs        # Chest with timer
│   └── ChatMessageUI.cs      # Clan chat
└── Animation/
    ├── ScreenTransition.cs
    ├── CardDeployAnimation.cs
    └── UITweens.cs

Assets/Scripts/Battle/UI/     # Already partially done
├── HandBar.cs
├── ElixirBar.cs
├── TowerHealthUI.cs
└── BattleHUD.cs
```

### **Reference Docs**
- `docs/planning/phase1/UI_UX_SPEC.md` - **FULL SPEC** (all screens, interactions, animations)
- `docs/planning/phase1/MECHANICS.md` - For battle HUD data binding

### **Dependencies**
- Needs: Agent 1's `EventBus` events (OnCardPlayed, OnUnitSpawned, OnElixirChanged, etc.)
- Needs: Agent 2's `NetworkClient` for multiplayer actions
- Can work in parallel with mock data initially

### **Testing**
- Playmode tests for each screen
- UI automation: deck building flow, battle deploy, shop purchase
- Responsive testing at all breakpoints

### **Branch**: `feature/ui-ux-implementation`

---

## 🤖 AGENT 4: ASSET PIPELINE & CARD DATABASE
### **Focus**: ScriptableObject card database, prefab templates, asset automation, Spine setup

### **Deliverables**
- [ ] **CardDatabase** - ScriptableObject for all 122+ cards with level stats (1-14)
- [ ] **Prefab Templates** - Unit, Building, Spell, Projectile, Tower prefabs with components
- [ ] **Asset Automation** - TexturePacker atlas builder, animation clip generator, prefab generator
- [ ] **Spine Integration** - SkeletonAnimation setup, animation state machine, skin system
- [ ] **Asset Manifest** - CSV with all assets: name, type, rarity, paths, specs
- [ ] **Addressables Setup** - Groups, labels, build pipeline for asset bundles
- [ ] **Shader System** - Unit shader (outline, team color), river shader, spell shaders
- [ ] **Audio System** - AudioMixer groups, clip registration, spatial/2D setup

### **Files to Work On**
```
Assets/Scripts/Data/
├── CardDatabase.cs           # ScriptableObject registry
├── CardData.cs               # Individual card SO (already exists)
└── GameConfig.cs             # Balance config (already exists)

Assets/Scripts/Systems/
├── AssetManager.cs           # Addressables + Resources
├── PoolManager.cs            # Object pooling (already exists)
└── AudioManager.cs           # AudioMixer (already exists)

Assets/Editor/                # Editor-only tools
├── AtlasBuilder.cs           # TexturePacker integration
├── AnimationClipGenerator.cs # Frame → AnimationClip
├── PrefabGenerator.cs        # Data → Prefab
├── AssetValidator.cs         # Naming, dimensions, format checks
└── CardDatabaseBuilder.cs    # CSV/JSON → ScriptableObjects

Assets/Shaders/
├── UnitShader.shader         # Team color, outline, dissolve
├── RiverShader.shader        # Flowing water
└── SpellShaders/             # Spell-specific

Assets/Spine/                 # Spine project structure
├── Units/
├── Buildings/
└── Spells/
```

### **Reference Docs**
- `docs/planning/phase1/ASSET_SOURCING.md` - Asset pipeline, directory structure, specs
- `docs/planning/phase1/CARDS_DATABASE.md` - All card data for ScriptableObjects
- `docs/planning/phase1/UI_UX_SPEC.md` - UI asset requirements (Section 10)

### **Dependencies**
- Can start immediately (no code dependencies)
- Blocks: Agent 1 (needs prefabs for visual testing), Agent 3 (needs UI assets)

### **Asset Sourcing** (from ASSET_SOURCING.md)
- GitHub: `clash-royale-assets`, `cr-assets`, `clash-royale-sprites`
- Reddit: r/ClashRoyaleModding, r/gamedev
- Discord: CR modding servers
- Wiki: clashroyale.fandom.com (renders for reference)

### **Testing**
- Automated asset validation (naming, dimensions, compression)
- Prefab instantiation test (all 122+ cards)
- Spine animation playback test
- Memory profiling (texture memory < 512MB)

### **Branch**: `feature/asset-pipeline-card-db`

---

## 🤖 AGENT 5: DATABASE & BACKEND SERVICES
### **Focus**: MySQL migrations, PlayerService, ClanService, ShopService, QuestService, Replay API

### **Deliverables**
- [ ] **Migration System** - Versioned SQL migrations (V1-V5+)
- [ ] **PlayerService** - Auth (JWT), registration, login, decks, collection, progression
- [ ] **ClanService** - CRUD, chat, donations, war opt-in, roles
- [ ] **ShopService** - Daily/special offers, purchase validation, IAP receipt verification
- [ ] **QuestService** - Daily/weekly/seasonal, progress tracking, rewards
- [ ] **SeasonService** - Battle pass tiers, free/paid tracks, crowns
- [ ] **TournamentService** - Classic/grand/draft, entry, rewards
- [ ] **Replay API** - Store/retrieve, metadata search, expiration cleanup
- [ ] **Leaderboards** - Global/country/clan/friends, periodic refresh
- [ ] **Admin Tools** - Player lookup, ban, config hot-reload

### **Files to Work On**
```
server/src/persistence/
├── Database.ts               # MySQL pool (already done)
├── migrations/
│   ├── 001_initial_schema.sql
│   ├── 002_clan_system.sql
│   ├── 003_tournaments.sql
│   ├── 004_season_pass.sql
│   └── 005_replay_system.sql
├── PlayerRepository.ts
├── BattleRepository.ts
├── ClanRepository.ts
├── ReplayRepository.ts
└── LeaderboardRepository.ts

server/src/services/
├── PlayerService.ts          # Auth, decks, progression (partially done)
├── ClanService.ts            # Clan CRUD, chat, donations
├── ShopService.ts            # Offers, purchases, IAP
├── QuestService.ts           # Daily/weekly/seasonal
├── SeasonService.ts          # Battle pass
├── TournamentService.ts      # Challenges/tournaments
└── ReplayService.ts          # Replay CRUD + search

server/src/utils/
├── MigrationRunner.ts        # Apply migrations on startup
└── ConfigHotReload.ts        # game_config table watcher
```

### **Reference Docs**
- `docs/planning/phase1/DATABASE_SCHEMA.md` - **FULL SCHEMA** (all tables, indexes, triggers, views)
- `docs/planning/phase1/MECHANICS.md` - Progression mechanics (chests, quests, seasons)
- `docs/planning/phase1/ARCHITECTURE.md` - Server architecture (Section 2.5)

### **Dependencies**
- Needs: `server/sql/init.sql` (already done - run on DB init)
- Needs: `server/src/types/index.ts` (already done)
- Independent - can run in parallel

### **Testing**
- `npm test` - Repository tests with test DB
- Integration: full player lifecycle (register → battle → progress → clan)
- Load test: 1000 concurrent players, 100 battles/sec

### **Branch**: `feature/database-backend-services`

---

## 🤖 AGENT 6: TESTING, CI/CD & DEVOPS
### **Focus**: Test infrastructure, GitHub Actions, performance benchmarks, monitoring

### **Deliverables**
- [ ] **Unity Test Framework** - EditMode/PlayMode tests, TestRunner setup
- [ ] **Unit Tests** - All `TEST_SPEC.md` Section 2.1 cases (elixir, card cycle, combat, buildings, spells, champions)
- [ ] **Integration Tests** - Battle flow, networking, database (Section 3)
- [ ] **E2E Tests** - Playwright: onboarding, deck building, chest unlock (Section 4)
- [ ] **Performance Tests** - 60FPS benchmark, memory stability, load test (Section 5)
- [ ] **Regression Tests** - Balance validation, determinism (Section 6)
- [ ] **GitHub Actions** - Build, test, deploy workflows
- [ ] **Docker Compose** - Dev stack (MySQL, Redis, Server, Unity headless)
- [ ] **Monitoring** - Structured logging, metrics (battle duration, desync rate, errors)
- [ ] **Code Quality** - ESLint, TypeScript strict, C# analyzers, coverage gates

### **Files to Work On**
```
# Unity Tests
Assets/Tests/
├── Unit/
│   ├── BattleSimulationTests.cs
│   ├── ElixirSystemTests.cs
│   ├── CardCycleTests.cs
│   ├── UnitCombatTests.cs
│   ├── BuildingTests.cs
│   ├── SpellTests.cs
│   ├── ChampionTests.cs
│   └── TargetingTests.cs
├── Integration/
│   ├── BattleFlowTests.cs
│   ├── NetworkTests.cs
│   └── DatabaseTests.cs
└── Performance/
    ├── SimulationBenchmark.cs
    ├── MemoryStabilityTest.cs
    └── PathfindingBenchmark.cs

# Server Tests
server/tests/
├── unit/
│   ├── rng.test.ts
│   ├── matchmaking.test.ts
│   └── playerService.test.ts
├── integration/
│   ├── battleFlow.test.ts
│   ├── matchmaking.test.ts
│   └── database.test.ts
└── load/
    └── k6-load-test.js

# CI/CD
.github/workflows/
├── unity-test.yml            # Unity EditMode/PlayMode tests
├── server-test.yml           # Server unit/integration tests
├── e2e-test.yml              # Playwright E2E
├── performance.yml           # Weekly benchmarks
├── deploy.yml                # Staging deploy
└── dependency-check.yml      # Security scan

# Docker
docker-compose.yml            # Already exists
docker-compose.test.yml       # Test environment
docker-compose.prod.yml       # Production

# Monitoring
server/src/utils/
├── MetricsCollector.ts       # Prometheus metrics
└── HealthCheck.ts            # /health endpoint
```

### **Reference Docs**
- `docs/planning/phase1/TEST_SPEC.md` - **COMPLETE TEST PLAN** (all sections)
- `docs/planning/phase1/ARCHITECTURE.md` - Performance targets (Section 5)

### **Dependencies**
- Needs: Agent 1 (simulation for unit tests), Agent 2 (network for integration), Agent 5 (DB for integration)
- Can write test scaffolding immediately, implement tests as features land

### **Quality Gates** (from TEST_SPEC.md Section 10.2)
| Gate | Threshold |
|------|-----------|
| Unit Test Pass Rate | 100% |
| Integration Test Pass Rate | 100% |
| Code Coverage | > 80% |
| No Critical Bugs | 0 |
| Performance Regression | < 5% slower |
| Memory Leak | 0 bytes/frame growth |

### **Branch**: `feature/testing-ci-cd`

---

## 🌿 GIT WORKTREE SETUP (Run All 6 Agents SIMULTANEOUSLY)

**All 6 agents run in parallel - no waiting, no sequential dependency.**

```bash
# Run ONCE in the main repo to create 6 isolated worktrees:
git worktree add ../CRClone-agent1 feature/battle-simulation-core
git worktree add ../CRClone-agent2 feature/networking-multiplayer
git worktree add ../CRClone-agent3 feature/ui-ux-implementation
git worktree add ../CRClone-agent4 feature/asset-pipeline-card-db
git worktree add ../CRClone-agent5 feature/database-backend-services
git worktree add ../CRClone-agent6 feature/testing-ci-cd

# Then EACH AGENT runs in their own directory:
# Agent 1:
cd ../CRClone-agent1 && cp .env.example .env

# Agent 2:
cd ../CRClone-agent2 && cp .env.example .env

# Agent 3:
cd ../CRClone-agent3 && cp .env.example .env

# Agent 4:
cd ../CRClone-agent4 && cp .env.example .env

# Agent 5:
cd ../CRClone-agent5 && cp .env.example .env

# Agent 6:
cd ../CRClone-agent6 && cp .env.example .env
```

**Each worktree is a complete, independent repo copy** - build, test, commit, push independently. Zero conflicts.

### Per-Agent Branch (Auto-Set by Worktree)
```bash
# Inside each worktree, the branch is already set:
# Agent 1: feature/battle-simulation-core
# Agent 2: feature/networking-multiplayer
# Agent 3: feature/ui-ux-implementation
# Agent 4: feature/asset-pipeline-card-db
# Agent 5: feature/database-backend-services
# Agent 6: feature/testing-ci-cd

# Work, commit, push:
git push origin feature/battle-simulation-core  # etc.
# Create PR when deliverables done
```

---

## 📦 HOW TO RUN AGENTS (Parallel - Start All Now)

```bash
# In 6 separate terminals, each agent runs in their worktree:

# Terminal 1 - Agent 1 (Battle Simulation)
cd ../CRClone-agent1
# Start Unity, open BattleTestRunner, begin implementing

# Terminal 2 - Agent 2 (Networking)
cd ../CRClone-agent2
cd server && npm install && npm run dev

# Terminal 3 - Agent 3 (UI/UX)
cd ../CRClone-agent3
# Start Unity, build screens with mock data

# Terminal 4 - Agent 4 (Assets)
cd ../CRClone-agent4
# Start sourcing assets, building tools

# Terminal 5 - Agent 5 (Backend)
cd ../CRClone-agent5
cd server && npm install && npm run migrate

# Terminal 6 - Agent 6 (Testing)
cd ../CRClone-agent6
# Write test scaffolding, setup CI
```

**All 6 start TODAY - no sequential dependency.**

---

## 🔄 INTEGRATION POINTS (Weekly Sync via PRs)

| Week | Integration Target |
|------|-------------------|
| 1 | Agent 1 + Agent 4: Simulation runs with real prefabs |
| 2 | Agent 1 + Agent 2: Server simulation matches client |
| 3 | Agent 1 + Agent 3: Battle HUD binds to simulation events |
| 4 | Agent 2 + Agent 3: Full 1v1 matchmaking → battle → result |
| 5 | Agent 5 + All: Persistence, progression, clans |
| 6 | Agent 6 + All: Full test suite passing, CI green |

---

## ⚠️ PARALLEL EXECUTION RULES

1. **DO NOT WAIT** for another agent - work on mock data / non-blocked tasks
2. **COMMUNICATE VIA PRs/ISSUES** - tag `@agent-name` in GitHub
3. **MOCK FIRST** - Agent 3 uses fake cards until Agent 4 delivers; Agent 1 uses hardcoded cards until Agent 4 delivers ScriptableObjects
4. **BLOCKED?** Create GitHub Issue with `blocked` label, tag relevant agent, pivot to next task
5. **SYNC WEEKLY** - 15 min standup, review PRs, plan next week

---

## 📞 ESCALATION

If blocked by another agent's work:
1. Create GitHub Issue with `blocked` label
2. Tag relevant agent
3. Work on non-blocked tasks in parallel
4. Don't wait - pivot to next deliverable

---

*Generated: 2026-09-12*  
*All agents start from `main` branch with current project state*