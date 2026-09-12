# Clash Royale Clone - Project Overview

## Project Scope
Complete Clash Royale clone with ALL cards, spells, towers, buildings, and mechanics. Local-only implementation.

## Technology Stack
- **Frontend**: Unity (C#) or Godot (GDScript) - Recommend Unity for 2D game development
- **Backend**: Node.js + Express or Python FastAPI
- **Database**: MySQL (Aiven Cloud)
- **Real-time**: Socket.io or SignalR for multiplayer
- **Assets**: Sprite sheets, particle systems, audio

## Database Configuration
```
DB_HOST=${DB_HOST}
DB_NAME=${DB_NAME}
DB_PASSWORD=${DB_PASSWORD}
DB_PORT=${DB_PORT}
DB_SSL=${DB_SSL}
DB_USERNAME=${DB_USERNAME}
```
*Configure via environment variables or .env file. See .env.example for template.*

## Project Phases

### Phase 1: Planning & Documentation (CURRENT)
- Complete card database with all stats
- Mechanics documentation
- UI/UX design specifications
- Asset sourcing plan
- Database schema design
- Technical architecture
- Test case specifications

### Phase 2: Asset Acquisition
- Download all sprites, animations, sounds
- Organize asset pipeline
- Create asset manifests

### Phase 3: Implementation
- Core game engine
- Card system
- Battle system
- Multiplayer networking
- UI implementation
- Testing & Polish

## Clash Royale Card Categories (100+ Cards)

### Troops (50+)
- Common: Knight, Archers, Goblins, Giant, P.E.K.K.A, Minions, Balloon, Witch, Barbarians, Golem, Skeletons, Spear Goblins, Mega Minion, Guards, Dark Prince, Three Musketeers, Lava Hound, Bowler, Lumberjack, Battle Ram, Bandit, Ghost, Bats, Wall Breakers, Royal Hogs, Goblin Gang, Skeleton Barrel, Flying Machine, Cannon Cart, Mega Knight, Goblin Giant, Skeleton King, Archer Queen, Mighty Miner, Monkey, Firecracker, Electro Wizard, Ice Wizard, Mother Witch, Ram Rider, Royal Ghost, Zappies, Rascal, Goblin Drill, Goblin Machine, Phoenix, Little Prince, Mini P.E.K.K.A, Musketeer, Baby Dragon, Prince, Wizard, Minion Horde, Hog Rider, Valkyrie, Goblin Barrel, Goblin Hut, Inferno Dragon, Executioner, Tornado, Giant Skeleton, Royal Giant, Elite Barbarians, Dart Goblin, Goblin Cage, Electro Dragon, Fisherman, Magic Archer, Skeleton Dragons, Royal Delivery, Skeleton King, Archer Queen

### Spells (15+)
- Common: Fireball, Arrows, Zap, Giant Snowball, Royal Delivery, Barbarian Barrel, Log, Earthquake, Lightning, Rocket, Poison, Freeze, Mirror, Clone, Tornado, Graveyard, Rage, Heal Spirit

### Buildings (15+)
- Common: Cannon, Tesla, Bomb Tower, Inferno Tower, Mortar, X-Bow, Goblin Hut, Furnace, Goblin Cage, Elixir Collector, Tesla, Cannon, Tombstone, Goblin Hut, Furnace, Goblin Cage, Elixir Collector

### Champions (3)
- Archer Queen, Skeleton King, Mighty Miner

## Core Mechanics to Implement

1. **Elixir System** - Generation, cap, spending
2. **Card Cycle** - 4-card hand, draw from 8-card deck
3. **Tower Mechanics** - Crown Towers, King Tower, targeting, damage
4. **Troop AI** - Pathfinding, targeting, attack patterns
5. **Spell Mechanics** - Area effects, duration, damage over time
6. **Building Mechanics** - Spawn, lifetime, targeting
7. **Champion Abilities** - Special abilities with cooldowns
8. **Clash Royale TV** - Replay system
9. **Tournament/Challenge System**
10. **Clan Wars** - Clan mechanics
11. **Progression** - Cards, gold, chests, seasons
12. **Shop** - Daily offers, special offers

## Deliverables for Phase 1

1. `MASTER_PLAN.md` - This file
2. `CARDS_DATABASE.md` - Complete card stats
3. Individual card files in `cards/` directory
4. `MECHANICS.md` - All game mechanics
5. `UI_UX_SPEC.md` - UI/UX specifications
6. `ASSET_SOURCING.md` - Where to get assets
7. `DATABASE_SCHEMA.md` - MySQL schema
8. `ARCHITECTURE.md` - Technical architecture
9. `TEST_SPEC.md` - Test cases and scenarios
10. `PHASE_TRACKER.md` - Progress tracking