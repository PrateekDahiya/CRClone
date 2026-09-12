# Agent 5: Database & Backend Services
## Workstream: MySQL Migrations, Player/Clan/Shop/Quest/Season/Tournament Services, Replay API, Leaderboards

---

## 🎯 YOUR MISSION
Build the **complete backend services** - database migrations, all game services (player, clan, shop, quests, seasons, tournaments), replay system, and leaderboards. This is the persistent layer that makes progression work.

---

## 📁 FILES YOU OWN (Exclusive Write Access)

### Migrations (server/sql/migrations/)
```
server/sql/migrations/
├── 001_initial_schema.sql      # Players, cards, decks, battles, replays
├── 002_clan_system.sql         # Clans, members, chat, donations
├── 003_tournaments.sql         # Tournaments, entries, challenges
├── 004_season_pass.sql         # Seasons, battle pass, player seasons
└── 005_replay_system.sql       # Replays, battle events, leaderboards
```

### Persistence Layer (server/src/persistence/)
```
server/src/persistence/
├── Database.ts                 # MySQL pool (done)
├── MigrationRunner.ts          # Apply migrations on startup
├── PlayerRepository.ts         # Player CRUD, decks, collection, progression
├── BattleRepository.ts         # Battle CRUD, events, results
├── ClanRepository.ts           # Clan CRUD, members, chat, donations
├── ReplayRepository.ts         # Replay CRUD, search, expiration
├── QuestRepository.ts          # Quest definitions, player progress
├── SeasonRepository.ts         # Seasons, battle pass tiers
├── TournamentRepository.ts     # Tournaments, entries
├── ShopRepository.ts           # Offers, purchases
└── LeaderboardRepository.ts    # Global/country/clan/friends leaderboards
```

### Services (server/src/services/)
```
server/src/services/
├── PlayerService.ts            # Auth, registration, login, decks, collection, progression (partially done)
├── ClanService.ts              # Clan CRUD, chat, donations, war opt-in, roles
├── ShopService.ts              # Daily/special offers, purchase validation, IAP receipt verification
├── QuestService.ts             # Daily/weekly/seasonal/achievement, progress tracking, rewards
├── SeasonService.ts            # Battle pass tiers, free/paid tracks, crowns, rewards
├── TournamentService.ts        # Classic/grand/draft/2v2, entry, bracket, rewards
├── ReplayService.ts            # Store/retrieve, metadata search, expiration cleanup
└── ConfigService.ts            # game_config table hot-reload, balance updates
```

### Utils (server/src/utils/)
```
server/src/utils/
├── MigrationRunner.ts          # Apply migrations on startup with version tracking
├── ConfigHotReload.ts          # Watch game_config table, broadcast changes
├── MetricsCollector.ts         # Prometheus metrics (battles, players, latency)
└── HealthCheck.ts              # /health endpoint for load balancer
```

---

## ✅ DELIVERABLES CHECKLIST

### Database Migrations (Versioned, Idempotent)
- [ ] **001_initial_schema**: Players, cards, card_level_stats, player_cards, player_decks, battles, battle_events, replays, game_config
- [ ] **002_clan_system**: Clans, clan_members, clan_chat, clan_donations, clan_donation_fills
- [ ] **003_tournaments**: Tournaments, tournament_entries, tournament_rewards
- [ ] **004_season_pass**: Seasons, player_seasons, season_rewards
- [ ] **005_replay_system**: Replays (with compression), battle_events (partitioned), leaderboards
- [ ] **MigrationRunner**: Runs on startup, tracks applied versions in `schema_migrations`, rolls back on failure

### PlayerService (Complete)
- [ ] **Auth**: JWT register/login, token refresh, password reset, guest accounts
- [ ] **Decks**: Save/load/validate (8 cards, max 1 champion, elixir avg), copy deck link
- [ ] **Collection**: Get all cards, add cards (chest rewards), upgrade (gold + cards), level stats
- [ ] **Progression**: XP/level, trophies/best, gold/gems, chests (4 slots + queue), quests
- [ ] **Battle History**: Last 20 battles with replay links

### ClanService (Complete)
- [ ] **Clan CRUD**: Create, join (open/invite/closed), leave, disband, settings (type, trophies req, war freq)
- [ ] **Members**: List with roles (leader/co-leader/elder/member), promote/demote/kick, war opt-in
- [ ] **Chat**: Messages (text, replay link, donation request, system), pagination
- [ ] **Donations**: Request (max 10 common/1 rare/0 epic/0 legendary per request), fulfill, weekly reset, gold/XP rewards
- [ ] **War/Capital**: Opt-in, attacks used, best attack (placeholder for Phase 3)

### ShopService (Complete)
- [ ] **Offers**: Daily (4 slots), Special (limited time), Chests, Gems, Wild Cards
- [ ] **Pricing**: Gold, Gems, Real Money (IAP)
- [ ] **Purchase Flow**: Validate offer exists/active, check currency, deduct, grant rewards, log purchase
- [ ] **IAP Verification**: Apple App Store / Google Play receipt validation (server-side)
- [ ] **Limits**: Per-player daily/weekly, global offer limits

### QuestService (Complete)
- [ ] **Quest Types**: Daily (win 3 battles), Weekly (donate 50 cards), Seasonal (reach 4000 trophies), Achievement (unlock all legendaries), Tutorial
- [ ] **Requirements**: JSON-based (type, count, mode, card filters)
- [ ] **Rewards**: Gold, gems, chests, wild cards, XP
- [ ] **Progress Tracking**: Auto-update on battle end, donation, trophy change
- [ ] **Scheduling**: Cron-based reset (daily 00:00 UTC, weekly Monday)

### SeasonService (Complete)
- [ ] **Season Config**: 35 tiers, free/paid track rewards, 500 gems pass price
- [ ] **Battle Pass**: Crowns earned -> tier progress, free/paid claim tracking
- [ ] **Rewards**: Per-tier (gold, gems, chests, wild cards, emotes, cosmetics)
- [ ] **Season Schedule**: Start/end dates, auto-activate next season

### TournamentService (Complete)
- [ ] **Types**: Classic (3 losses/12 wins), Grand (better rewards), Draft (pick from pairs), 2v2, Custom
- [ ] **Entry**: Gem/gold cost, card level cap (tournament standard = 11), banned cards
- [ ] **Progression**: Track wins/losses, max 3 losses / 12 wins
- [ ] **Rewards**: Tiered by wins (12 wins = max rewards)
- [ ] **Draft Mode**: Server sends pairs, player picks, builds deck

### ReplayService (Complete)
- [ ] **Store**: Compressed protobuf (seed + inputs + metadata), expires in 30 days
- [ ] **Retrieve**: By replay ID, by player, by deck hash, by date range
- [ ] **Metadata Search**: Player names, decks, duration, result
- [ ] **Cleanup Job**: Daily cron - delete expired replays

### Leaderboards (Periodic Refresh)
- [ ] **Scopes**: Global, Country (ISO code), Clan, Friends
- [ ] **Types**: Trophies, Wins, Win Streak, Donations, War Wins
- [ ] **Refresh**: Hourly cron, materialized views for performance
- [ ] **API**: Top 100, player rank, around player (±10)

### ConfigService
- [ ] **Hot Reload**: Watch `game_config` table, broadcast changes to battle servers
- [ ] **Balance Updates**: Elixir rates, card stats, chest timers without restart

---

## 📚 REFERENCE DOCS
| Doc | Purpose |
|-----|---------|
| `docs/planning/phase1/DATABASE_SCHEMA.md` | **FULL SCHEMA** - All 20+ tables, indexes, triggers, views, stored procedures |
| `docs/planning/phase1/MECHANICS.md` | Progression mechanics (chests, quests, seasons, elixir) |
| `docs/planning/phase1/ARCHITECTURE.md` Section 2.5 | Server architecture, persistence service |
| `server/sql/init.sql` | **Reference** - Complete schema in one file |

---

## 🔗 DEPENDENCIES

| Dependency | Status | Notes |
|------------|--------|-------|
| `server/src/types/index.ts` | Done | Shared types |
| `server/src/config/index.ts` | Done | Your MySQL creds configured |
| Agent 2 Network | Parallel | Uses `PlayerRepository`, `BattleRepository`, `ReplayRepository` |
| Agent 6 Tests | Parallel | Writes integration tests for your repositories |

---

## 🧪 TESTING REQUIREMENTS
- **Unit**: `npm test` - Repository CRUD, service logic (mock DB)
- **Integration**: Test DB (Docker MySQL) - full player lifecycle: register -> battle -> progress -> clan -> shop -> tournament
- **Migration**: Up/down migrations, rollback on failure
- **Load**: 1000 concurrent players, 100 battles/sec, 10k clan chat msgs/sec
- **Data Integrity**: Transactions for multi-table ops (battle result + trophy update + chest reward)

---

## 🚫 DO NOT TOUCH
- `server/src/types/index.ts` - Frozen
- `server/src/network/` - Agent 2
- `server/src/battle/` - Agent 2
- `Assets/` - Agents 1,3,4

---

## 🌿 BRANCH & WORKFLOW
```bash
git checkout -b feature/database-backend-services
# Phase 1: Migrations 001-005 + MigrationRunner
# Phase 2: PlayerService + ClanService (core progression)
# Phase 3: Shop + Quest + Season + Tournament
# Phase 4: Replay + Leaderboards + ConfigService
git push origin feature/database-backend-services
```

**Integration Points:**
- Week 1: Migrations + PlayerService -> Agent 2 uses for auth, matchmaking
- Week 2: ClanService -> Agent 3 Clan screen
- Week 3: Shop + Quest + Season -> Agent 3 Shop/Profile screens
- Week 4: ReplayService -> Agent 2 BattleServer records replays

---

## 📋 QUICK START
```bash
# 1. Run migrations
cd server
npm run migrate  # Runs MigrationRunner on sql/migrations/

# 2. Test PlayerService
npm test -- PlayerService.test.ts

# 3. Verify schema
mysql -h $DB_HOST -u $DB_USER -p$DB_PASSWORD $DB_NAME < server/sql/init.sql
```

**Good luck! You're building the persistent heart of the game.** 💾