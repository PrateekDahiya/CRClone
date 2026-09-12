# Clash Royale Clone

A complete Clash Royale clone with all 122+ cards, full mechanics, deterministic networking, and authoritative server architecture.

## Project Structure

```
CRclone/
├── Assets/                     # Unity Client
│   ├── Scripts/
│   │   ├── Core/              # GameManager, Services, EventBus
│   │   ├── Battle/
│   │   │   ├── Simulation/    # Deterministic battle simulation
│   │   │   ├── Presentation/  # Visuals, effects, views
│   │   │   ├── Input/         # Touch/mouse/keyboard input
│   │   │   └── UI/            # Battle HUD, hand bar, elixir
│   │   ├── Network/           # WebSocket client, protocol
│   │   ├── Data/              # Card database, configs
│   │   ├── UI/                # Screens (Lobby, DeckBuilder, Shop, etc.)
│   │   └── Systems/           # Audio, Pool, Asset managers
│   ├── Resources/             # Configs, data
│   ├── Prefabs/               # Units, buildings, spells, UI
│   ├── Scenes/                # Battle, Lobby, DeckBuilder, MainMenu
│   └── Shaders/               # Custom shaders
│
├── server/                     # Node.js/TypeScript Server
│   ├── src/
│   │   ├── config/            # Configuration
│   │   ├── network/           # WebSocket handling
│   │   ├── matchmaking/       # Matchmaking queues
│   │   ├── battle/            # Authoritative battle server
│   │   ├── persistence/       # MySQL database
│   │   ├── services/          # Player, Clan, Shop services
│   │   ├── utils/             # Logger, RNG
│   │   └── types/             # Shared TypeScript types
│   ├── sql/                   # Database initialization
│   └── Dockerfile
│
├── docs/planning/              # Phase 1 Planning Documentation
│   ├── phase1/
│   │   ├── PROJECT_OVERVIEW.md
│   │   ├── CARDS_DATABASE.md   # All 122+ cards with stats
│   │   ├── MECHANICS.md        # All game mechanics
│   │   ├── UI_UX_SPEC.md       # Complete UI/UX specification
│   │   ├── ASSET_SOURCING.md   # Asset pipeline & sources
│   │   ├── DATABASE_SCHEMA.md  # MySQL schema (20+ tables)
│   │   ├── ARCHITECTURE.md     # Technical architecture
│   │   ├── TEST_SPEC.md        # Comprehensive test plan
│   │   └── PHASE_TRACKER.md    # Progress tracking
│   ├── phase2/                # Asset Acquisition (next)
│   └── phase3/                # Implementation (future)
│
└── docker-compose.yml          # Local development stack
```

## Features

### Game Content
- **122+ Cards**: All Legendary (18), Epic (28), Rare (35+), Common (30+), Champion (3)
- **Complete Mechanics**: Elixir system, card cycle, targeting AI, pathfinding, spells, buildings, champions
- **All Game Modes**: 1v1 Ladder, 2v2, Tournaments, Challenges, Friendly, Practice
- **Progression**: Cards, gold, chests, quests, seasons, battle pass
- **Social**: Clans, chat, donations, clan wars

### Technical Highlights
- **Deterministic Simulation**: 60Hz lockstep with fixed-point math
- **Authoritative Server**: Server-side simulation with client prediction
- **Reconciliation**: Automatic desync detection and recovery
- **Replay System**: Deterministic replay recording and playback
- **Scalable Architecture**: Stateless battle servers, Redis pub/sub

## Getting Started

### Prerequisites
- Unity 2022.3 LTS or later
- Node.js 20+
- MySQL 8.0 (or Docker)
- Redis 7+ (or Docker)

### Quick Start (Docker)
```bash
# Clone repository
git clone <repo>
cd CRclone

# Configure environment
cp .env.example .env
# Edit .env with your settings

# Start development stack
docker-compose up -d

# Server runs on http://localhost:3000, WS on ws://localhost:3001
```

### Manual Setup

#### Database
```bash
# Using Docker
docker run -d --name mysql \
  -e MYSQL_DATABASE=crclone \
  -e MYSQL_ROOT_PASSWORD=your_password \
  -p 3306:3306 \
  mysql:8.0

# Or use Aiven Cloud (configured in .env)
```

#### Server
```bash
cd server
npm install
npm run dev  # Development with hot reload
# or
npm run build && npm start  # Production
```

#### Client (Unity)
1. Open `Assets/` folder in Unity 2022.3+
2. Open `Scenes/MainMenu.unity`
3. Enter Play Mode

## Documentation

See `docs/planning/phase1/` for complete specifications:
- [Card Database](docs/planning/phase1/CARDS_DATABASE.md) - All card stats
- [Mechanics](docs/planning/phase1/MECHANICS.md) - Game systems
- [UI/UX Spec](docs/planning/phase1/UI_UX_SPEC.md) - Screen designs
- [Architecture](docs/planning/phase1/ARCHITECTURE.md) - Technical design
- [Database Schema](docs/planning/phase1/DATABASE_SCHEMA.md) - MySQL tables
- [Test Plan](docs/planning/phase1/TEST_SPEC.md) - Test specifications

## Development Phases

| Phase | Status | Description |
|-------|--------|-------------|
| **Phase 1: Planning** | ✅ Complete | All documentation, specs, architecture |
| **Phase 2: Assets** | 🔄 Next | Source sprites, animations, audio |
| **Phase 3: Implementation** | ⏳ Future | Unity client + Node.js server |

## Testing

```bash
# Server tests
cd server && npm test

# Unity tests (in Editor)
# Window > General > Test Runner
```

## License

This project is for **local development and learning purposes only**. 
Clash Royale assets and IP belong to Supercell.
Do not distribute, host publicly, or monetize.

## Contributing

This is a personal learning project. For educational reference only.