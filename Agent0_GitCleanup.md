# Agent 0: Git History Cleanup & Phased Commit
## Workstream: Repository Organization, Clean History, Phased Commits

---

## 🎯 YOUR MISSION
The 6 development agents + QA agent have been working in the **same folder** without worktrees. The git history is messy. Your job:

1. **Audit** the current state - what files exist, what's changed
2. **Organize** into logical, phased commits with clear messages
3. **Push clean history** to `main` branch
4. **Set up proper worktrees** for future agents

**You run FIRST - before any other agent starts fresh work.**

---

## 🌿 GIT WORKTREE SETUP
```bash
# Run in main repo:
git worktree add ../CRClone-agent0 feature/git-cleanup
cd ../CRClone-agent0
# No .env needed - you only touch git
```

**Branch:** `feature/git-cleanup`

---

## 📋 PHASED COMMIT PLAN

### **Phase 0: Repository Foundation** (Commit 1)
```
chore: initialize repository structure and configuration

- Add .gitignore (Unity, Node, OS, IDE)
- Add .env.example template (no secrets)
- Add docker-compose.yml (MySQL, Redis, Server)
- Add README.md with project overview
- Add LICENSE (MIT)
```
**Files:** `.gitignore`, `.env.example`, `docker-compose.yml`, `README.md`, `LICENSE`

---

### **Phase 1: Planning Documentation** (Commit 2)
```
docs: complete Phase 1 planning documentation

- PROJECT_OVERVIEW.md: scope, tech stack, phases
- CARDS_DATABASE.md: 122+ cards with tournament-standard stats
- MECHANICS.md: all game systems (elixir, targeting, pathfinding, spells, buildings, champions)
- UI_UX_SPEC.md: all screens, interactions, animations, breakpoints
- ASSET_SOURCING.md: asset pipeline, sources, 500+ SFX list
- DATABASE_SCHEMA.md: 20+ MySQL tables, indexes, triggers, migrations
- ARCHITECTURE.md: Unity + Node.js, deterministic networking, 60Hz lockstep
- TEST_SPEC.md: unit/integration/E2E/performance test plans
- PHASE_TRACKER.md: progress tracking, resource estimates
- SUMMARY.md: executive summary
- 100+ individual card files in docs/planning/phase1/cards/{legendary,epic,rare,common,champion}/
```
**Files:** `docs/planning/phase1/**/*`

---

### **Phase 2: Core Engine Types & Contracts** (Commit 3)
```
core: define shared types, service locator, event bus, game config

- GameTypes.cs: enums (CardRarity, CardType, EntityType, BattleStatus, etc.)
- Services.cs: service locator pattern
- EventBus.cs: 30+ events (CardPlayed, UnitSpawned, SpellCast, TowerDamaged, etc.)
- GameConfig.cs: ScriptableObject with balance settings
- GameManager.cs: singleton bootstrap, state machine, scene loading
- GameBootstrap.cs: RuntimeInitializeOnLoadMethod entry point
```
**Files:** `Assets/Scripts/Core/**/*.cs`

---

### **Phase 3: Data & Config Systems** (Commit 4)
```
data: card database, config manager, player data structures

- CardData.cs: ScriptableObject for individual cards
- CardLevelStats.cs: per-level stats with gold/card costs
- GameConfig.cs: balance config (elixir rates, tower stats, etc.)
- DataManager.cs: loads cards, validates decks, calculates avg elixir
- ConfigManager.cs: provides GameConfig instance
- PlayerLocalData.cs: local player state (deck, collection, settings)
- BattleData.cs: battle info (seed, decks, players)
- BattleResult.cs: battle outcome (crowns, trophies, replay)
```
**Files:** `Assets/Scripts/Data/**/*.cs`

---

### **Phase 4: Core Systems** (Commit 5)
```
systems: object pooling, audio, asset management

- PoolManager.cs: generic object pool with IPoolable interface
- AudioManager.cs: AudioMixer groups (Master/Music/SFX/Voice), spatial/2D
- AssetManager.cs: Addressables + Resources, async loading
```
**Files:** `Assets/Scripts/Systems/**/*.cs`

---

### **Phase 5: Battle Simulation - Core Loop** (Commit 6)
```
battle: deterministic simulation core (60Hz lockstep)

- BattleSimulation.cs: main loop, 60Hz tick, entity management
- DeterministicRNG.cs: Xorshift64* for reproducible randomness
- PlayerState.cs: elixir, hand, deck, card draw logic
- BattleEvent.cs: event log for replay
- Pathfinding.cs: A* on half-tile grid, river blocking, bridges
```
**Files:** `Assets/Scripts/Battle/Simulation/BattleSimulation.cs`, `Pathfinding.cs`, `DeterministicRNG.cs`

---

### **Phase 6: Battle Simulation - Entities** (Commit 7)
```
battle: entity hierarchy (Entity, Unit, Building, Projectile, SpellEffect, Tower)

- Entity.cs: base class, HP, status effects (stun, freeze, slow, invisible, shield, charge)
- Unit.cs: movement, targeting, melee/ranged attacks, charge, splash, chain, pierce
- Building.cs: lifetime, spawners, siege, retraction, Elixir Collector, Cannon Cart
- Projectile.cs: homing, mortar arc, splash, pierce (Magic Archer), chain (Electro)
- SpellEffect.cs: instant (Zap, Log, Freeze), DoT (Poison), spawn (Goblin Barrel, Graveyard), utility (Clone, Rage, Tornado)
- Tower.cs: targeting priority, king activation (damage, princess destroyed, Tornado pull)
```
**Files:** `Assets/Scripts/Battle/Simulation/{Entity,Unit,Building,Projectile,SpellEffect,Tower}.cs`

---

### **Phase 7: Battle Presentation & UI** (Commit 8)
```
battle: visual layer, views, HUD

- BattleView.cs: syncs simulation to visuals, camera shake, event subscriptions
- UnitView.cs: Spine animation, health bar, selection ring, interpolation
- BuildingView.cs: retraction visual, health bar
- ProjectileView.cs: trail renderer, impact particles
- SpellEffectView.cs: particle systems, tornado line renderer
- TowerView.cs: activation effect, destruction animation
- HealthBar.cs: dynamic color (green/yellow/red), world-space canvas
- RiverRenderer.cs: animated UV water shader
- HandBar.cs: 4-card hand, elixir cost, selection glow, deploy animation
- ElixirBar.cs: 10-segment bar, pulse animation
- TowerHealthUI.cs: HP fill, crown icons
- BattleHUD.cs: timer, crowns, pause menu
```
**Files:** `Assets/Scripts/Battle/Presentation/**/*.cs`, `Assets/Scripts/Battle/UI/**/*.cs`

---

### **Phase 8: Input & Networking (Client)** (Commit 9)
```
input: touch/mouse/keyboard handling, card drag-deploy

- InputManager.cs: pointer down/drag/up, card selection, spell radius preview, deploy validation
- NetworkClient.cs: WebSocket + Protobuf, input queue, reconciliation, reconnection
- MessageTypes.cs: Protobuf definitions (auth, matchmaking, input, game_state, battle_end)
- Serialization.cs: binary helpers
- ReconnectionManager.cs: auto-reconnect with exponential backoff
```
**Files:** `Assets/Scripts/Battle/Input/**/*.cs`, `Assets/Scripts/Network/**/*.cs`

---

### **Phase 9: UI Screens & Deck Builder** (Commit 10)
```
ui: all game screens, deck builder, navigation

- UIManager.cs: screen management, toasts, loading overlay
- MainMenuScreen.cs, LobbyScreen.cs, DeckBuilderScreen.cs
- BattleScreen.cs, BattleResultScreen.cs, ShopScreen.cs
- ClanScreen.cs, ProfileScreen.cs, SettingsScreen.cs
- DeckBuilderUI.cs: drag-drop 8 slots, collection grid, filters, validation
- CollectionCardUI.cs: draggable cards, owned badge
- DeckSlotUI.cs: drop target, remove button
- ToastUI.cs, LoadingUI.cs
```
**Files:** `Assets/Scripts/UI/**/*.cs`

---

### **Phase 10: Server - Types & Config** (Commit 11)
```
server: TypeScript types, configuration, logging

- types/index.ts: all shared types (CardRarity, CardType, EntityState, PlayerInput, GameState, etc.)
- config/index.ts: env-based config (DB, Redis, JWT, game, matchmaking, battle)
- utils/logger.ts: Winston with file/console transports
- utils/rng.ts: DeterministicRNG (Xorshift64*), battle seed generation
```
**Files:** `server/src/types/**/*.ts`, `server/src/config/**/*.ts`, `server/src/utils/logger.ts`, `server/src/utils/rng.ts`

---

### **Phase 11: Server - Network & Matchmaking** (Commit 12)
```
server: WebSocket server, connection manager, matchmaking

- network/ConnectionManager.ts: WS lifecycle, broadcast, authenticated connections
- network/NetworkClient.ts: server-side client wrapper, player attachment
- network/MessageHandler.ts: message routing (auth, matchmaking, input, save_deck)
- network/Protocol.ts: Protobuf definitions
- matchmaking/Matchmaker.ts: trophy-based queues, 2v2, rating system
- matchmaking/Queue.ts: priority queue
- matchmaking/RatingSystem.ts: ELO/trophy calculations
```
**Files:** `server/src/network/**/*.ts`, `server/src/matchmaking/**/*.ts`

---

### **Phase 13: Server - Persistence & Database** (Commit 13)
```
server: MySQL persistence, repositories, migrations

- persistence/Database.ts: MySQL pool with connection limits
- persistence/PlayerRepository.ts, BattleRepository.ts, ClanRepository.ts
- persistence/ReplayRepository.ts, QuestRepository.ts, SeasonRepository.ts
- persistence/TournamentRepository.ts, ShopRepository.ts, LeaderboardRepository.ts
- sql/init.sql: complete schema (20+ tables, indexes, triggers, views)
- sql/migrations/001-005: versioned migrations
- utils/MigrationRunner.ts: applies migrations on startup
```
**Files:** `server/src/persistence/**/*.ts`, `server/sql/**/*.sql`

---

### **Phase 14: Server - Game Services** (Commit 14)
```
server: player, clan, shop, quest, season, tournament, replay services

- services/PlayerService.ts: JWT auth, decks, collection, progression
- services/ClanService.ts: CRUD, chat, donations, war opt-in
- services/ShopService.ts: daily/special offers, IAP verification
- services/QuestService.ts: daily/weekly/seasonal, progress tracking
- services/SeasonService.ts: battle pass tiers, free/paid tracks
- services/TournamentService.ts: classic/grand/draft, entry, rewards
- services/ReplayService.ts: store/retrieve, metadata search, cleanup
- services/ConfigService.ts: game_config hot-reload
```
**Files:** `server/src/services/**/*.ts`

---

### **Phase 15: Server - Battle Server** (Commit 15)
```
server: authoritative battle server, reconciliation, replay recording

- battle/BattleServer.ts: input validation, 60Hz tick, entity sync, desync detection
- battle/BattleSimulation.ts: server port of Unity simulation
- battle/EntityManager.ts: entity state delta compression
- battle/ReplayRecorder.ts: deterministic replay (seed + inputs)
- persistence/BattleRepository.ts, ReplayRepository.ts
```
**Files:** `server/src/battle/**/*.ts`

---

### **Phase 16: Testing Infrastructure** (Commit 16)
```
test: Unity test framework, server tests, CI/CD, monitoring

- Assets/Tests/Unit/*.cs: elixir, card cycle, combat, buildings, spells, champions, targeting
- Assets/Tests/Integration/*.cs: battle flow, network, database
- Assets/Tests/Performance/*.cs: simulation benchmark, memory, pathfinding
- server/tests/unit/*.test.ts: rng, matchmaking, player/clan/shop/quest/season/tournament/replay
- server/tests/integration/*.test.ts: battle flow, matchmaking, database, replay
- server/tests/load/k6-load-test.js: 1000 concurrent, 100 battles/sec
- .github/workflows/*.yml: unity-test, server-test, e2e-test, performance, deploy, dependency-check
- docker-compose.test.yml, docker-compose.prod.yml
- server/src/utils/MetricsCollector.ts, HealthCheck.ts
```
**Files:** `Assets/Tests/**/*.cs`, `server/tests/**/*.ts`, `.github/workflows/**/*.yml`, `docker-compose*.yml`

---

### **Phase 17: Agent Prompts & QA** (Commit 17)
```
docs: agent workstream prompts, QA integration testing

- Agent1_BattleSimulation.md through Agent7_QA_Integration.md
- AGENT_PROMPTS.md: master orchestration doc
- Each agent: files owned, deliverables, dependencies, test requirements, worktree setup
```
**Files:** `Agent*.md`, `AGENT_PROMPTS.md`

---

### **Phase 18: Worktree Setup & Final Polish** (Commit 18)
```
chore: git worktree setup for parallel development, clean .gitignore

- .gitignore: ensure all build artifacts ignored
- .env.example: verified no secrets
- Worktree setup commands documented in each agent prompt
- AGENT_PROMPTS.md updated with worktree commands
```
**Files:** `.gitignore`, `.env.example`, `Agent*.md`, `AGENT_PROMPTS.md`

---

## 🛠️ YOUR EXECUTION COMMANDS

```bash
cd ../CRClone-agent0

# 1. Check current state
git status
git log --oneline -10

# 2. For each phase:
#    - Stage files: git add <files>
#    - Commit: git commit -m "<message>"
#    - Verify: git log --oneline -1

# 3. Push to main (force if needed - this is history rewrite)
git push -f origin feature/git-cleanup
# Then merge to main:
git checkout master
git merge feature/git-cleanup
git push origin master
```

---

## ⚠️ RULES

1. **NO CODE CHANGES** - Only git operations (add, commit, reorder)
2. **NO SECRETS** - Verify `.env` not committed, `.env.example` exists
3. **ATOMIC COMMITS** - Each phase = one commit, one logical theme
4. **CLEAR MESSAGES** - Format: `<type>: <subject>\n\n<body>`
5. **VERIFY EACH COMMIT** - `git show --stat HEAD` after each

---

## 🔍 VERIFICATION CHECKLIST (After All Commits)

```bash
# 1. Clean history
git log --oneline --graph -20

# 2. No large files (>100MB)
git ls-files -z | xargs -0 ls -la | awk '$5 > 100000000'

# 3. No secrets
git log --all --full-history --oneline -- "*password*" "*secret*" "*token*"

# 4. Build works
cd ../CRClone-agent0
# Unity: /Applications/Unity/Hub/Editor/2022.3.x/Unity -batchmode -quit -projectPath .
# Server: cd server && npm ci && npm run build

# 5. Tests pass
cd server && npm test
# Unity: /Applications/Unity/.../Unity -runTests -projectPath .
```

---

## 📋 DELIVERABLE

**A clean, linear git history on `main` with 18 atomic commits, each with clear message explaining what and why.**

The 7 agents (1-7) will then use `git worktree add` from this clean base.

---

**Run this agent FIRST. No other agent should touch code until you're done.** 🧹