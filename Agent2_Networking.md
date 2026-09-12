# Agent 2: Networking & Multiplayer
## Workstream: Authoritative Server, WebSocket Protocol, Matchmaking, Reconciliation

---

## 🎯 YOUR MISSION
Build the **complete networking stack** - client WebSocket client, authoritative Node.js server, matchmaking, input validation, reconciliation, and replay recording.

---

## 📁 FILES YOU OWN (Exclusive Write Access)

### Unity Client
```
Assets/Scripts/Network/
├── NetworkClient.cs          # WebSocket + Protobuf serialization
├── MessageTypes.cs           # Protobuf message definitions
├── Serialization.cs          # Binary serialization helpers
└── ReconnectionManager.cs    # Auto-reconnect, state recovery
```

### Server (Node.js/TypeScript)
```
server/src/
├── index.ts                  # Entry point (already scaffolded)
├── config/index.ts           # Config with your MySQL creds (done)
├── network/
│   ├── ConnectionManager.ts  # WS connection lifecycle
│   ├── NetworkClient.ts      # Server-side client wrapper
│   ├── MessageHandler.ts     # Message routing
│   └── Protocol.ts           # Protobuf definitions
├── matchmaking/
│   ├── Matchmaker.ts         # Trophy-based queues, 2v2 support
│   ├── Queue.ts              # Priority queue implementation
│   └── RatingSystem.ts       # Trophy/ELO calculations
├── battle/
│   ├── BattleServer.ts       # Authoritative battle simulation
│   ├── BattleSimulation.ts   # Server port of Unity simulation
│   ├── EntityManager.ts      # Entity state sync
│   └── ReplayRecorder.ts     # Deterministic replay recording
├── persistence/
│   ├── Database.ts           # MySQL pool (done)
│   ├── PlayerRepository.ts
│   ├── BattleRepository.ts
│   └── ReplayRepository.ts
└── types/index.ts            # **FROZEN** - shared types (already done)
```

---

## ✅ DELIVERABLES CHECKLIST

### Unity WebSocket Client
- [ ] `NetworkClient.Connect(url, token)` - WSS connection with auth
- [ ] Protobuf serialization for all message types
- [ ] Input queue: `SendInput(PlayerInput)` → queued → sent each tick
- [ ] Input acknowledgment + retransmission (unacked inputs resent after 1s)
- [ ] Game state reconciliation: apply server state, correct local simulation
- [ ] Heartbeat (10s interval) + auto-reconnect (5s delay, exponential backoff)
- [ ] Message types: `auth`, `matchmaking`, `battle_found`, `game_state`, `input_ack`, `reconcile`, `battle_end`, `error`, `heartbeat`

### Server Connection Manager
- [ ] `ConnectionManager` - track all WS connections, authenticated players
- [ ] Per-client: `player` (Player object), `battleId`, `lastHeartbeat`
- [ ] Broadcast to all / exclude one
- [ ] Cleanup on disconnect (remove from matchmaking, handle battle disconnect)

### Matchmaking
- [ ] Per-battle-type queues (Ladder, 2v2, Tournament, Challenge, Friendly, Practice)
- [ ] Trophy-based matching: max diff 300 (expands to 1000 at 5000+ trophies)
- [ ] 2v2: team balancing, combined trophy average
- [ ] Queue timeout (30s) → expand range or create bot match
- [ ] Battle creation: generate deterministic seed, spawn `BattleServer`

### Authoritative Battle Server
- [ ] `BattleServer` - owns simulation, validates all inputs
- [ ] Input validation: card ownership, elixir cost, deploy zone, rate limiting
- [ ] Server tick loop (60Hz): process inputs → step simulation → broadcast state
- [ ] Entity state sync: only send changed entities (delta compression)
- [ ] Desync detection: compare client/server entity states, trigger reconciliation
- [ ] Battle end: calculate crowns, trophy changes, save replay

### Replay System
- [ ] `ReplayRecorder` - records: seed, all inputs with ticks, battle metadata
- [ ] Store in MySQL `replays` table (compressed protobuf/JSON)
- [ ] Replay API: retrieve by ID, search by player/deck/duration

### Protocol (server/src/types/index.ts) - **MATCH EXACTLY**
```typescript
// Already defined - do not modify without cross-agent approval
export interface PlayerInput {
  type: 'play_card' | 'cast_spell' | 'champion_ability' | 'emote';
  cardId?: number; spellId?: number;
  position: Vector2; targetPosition?: Vector2;
  clientTick: number;
}
export interface GameState {
  tick: number;
  entities: EntityState[];
  projectiles: EntityState[];
  player1: PlayerState; player2: PlayerState;
  status: BattleStatus;
}
```

---

## 📚 REFERENCE DOCS
| Doc | Purpose |
|-----|---------|
| `docs/planning/phase1/ARCHITECTURE.md` Sections 2.4, 3, 4 | Networking architecture, data flow, deterministic requirements |
| `docs/planning/phase1/MECHANICS.md` | Input validation rules (deploy zones, elixir, card ownership) |
| `server/src/types/index.ts` | **FROZEN** - all message types, entity states |
| `docs/planning/phase1/TEST_SPEC.md` Section 3 | Integration test cases |

---

## 🔗 DEPENDENCIES

| Dependency | Status | Notes |
|------------|--------|-------|
| `server/src/types/index.ts` | ✅ Done | **Frozen** - matches Unity `MessageTypes.cs` |
| Agent 1 BattleSimulation | ⏳ Parallel | **Port logic to server** - deterministic RNG, fixed timestep, same entity behavior |
| Agent 5 Database | ⏳ Parallel | `PlayerRepository`, `BattleRepository`, `ReplayRepository` |
| Agent 3 UI | ⏳ Parallel | Needs `NetworkClient` for matchmaking, battle actions |

---

## 🧪 TESTING REQUIREMENTS
- **Unit**: `npm test` - RNG determinism, matchmaking logic, input validation
- **Integration**: 2 clients connect → matchmake → play cards → verify sync
- **Desync Test**: Inject 200ms lag on one client → verify reconciliation corrects
- **Reconnection**: Disconnect mid-battle → reconnect → receive current state
- **Load**: k6 script - 1000 concurrent connections, 100 battles/sec

---

## 🚫 DO NOT TOUCH
- `Assets/Scripts/Core/` - Frozen contracts
- `Assets/Scripts/Battle/Simulation/` - Agent 1 owns (port logic, don't modify Unity version)
- `Assets/Scripts/UI/` - Agent 3 owns
- `server/src/services/` - Agent 5 owns
- `server/src/persistence/*.ts` repositories - Agent 5 owns (you use them)

---

## 🌿 GIT WORKTREE SETUP (Run All 6 Agents Simultaneously)

**Each agent works in their own isolated worktree - no conflicts, no waiting.**

```bash
# Run ONCE per agent (each agent runs their own setup):

# Agent 2 - Networking
git worktree add ../CRClone-agent2 feature/networking-multiplayer
cd ../CRClone-agent2
cp .env.example .env   # Fill in your DB credentials
# Start working...
```

**Each worktree is a complete, independent copy of the repo** - you can build, run tests, and commit independently. No stepping on each other's toes.

### Branch & Workflow (Per Worktree)
```bash
# Inside your worktree directory:
git checkout -b feature/networking-multiplayer  # Already set by worktree add
# Work, commit frequently
git push origin feature/networking-multiplayer
# Create PR when deliverables done
```

**Integration Points (Cross-Agent Sync via PRs):**
- Week 1: Port Agent 1's `BattleSimulation` to server `BattleSimulation.ts` - verify identical results with same seed
- Week 2: Unity `NetworkClient` ↔ Server `BattleServer` - full 1v1 flow
- Week 3: Agent 3 integrates `NetworkClient` into Lobby/DeckBuilder/Battle screens
- Continuous: Agent 6 writes tests for your code

---

## ⚠️ CRITICAL: DETERMINISM REQUIREMENTS
Your server simulation **MUST** produce identical results to Unity client given same seed:
- Same `DeterministicRNG` (Xorshift64*)
- Same fixed timestep (60Hz, 1/60s)
- Same entity update order: Inputs → Elixir → Spells → Projectiles → Units → Buildings → Towers → Collisions → Deaths → WinCheck
- Same float math (or fixed-point)
- **Test**: Run same seed on client and server, compare entity states at tick 1000

---

## 📋 QUICK START
```bash
# Server
cd server
npm install
npm run dev  # Runs on :3000 (HTTP) + :3001 (WS)

# Test: Open 2 browser tabs to test client (once Agent 3 provides basic UI)
# Or use test client script
```

**Good luck! Networking is the backbone of multiplayer.** 🌐