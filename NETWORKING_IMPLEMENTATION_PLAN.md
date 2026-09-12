# Networking & Multiplayer Implementation Plan

## Objective
Build the complete networking stack: Unity WebSocket client, authoritative Node.js server, matchmaking, input validation, reconciliation, and replay recording.

## Current Implementation Status

### Already Implemented (Server)
| File | Status | Notes |
|------|--------|-------|
| `server/src/types/index.ts` | ✅ Complete | Frozen - matches Unity MessageTypes.cs |
| `server/src/network/ConnectionManager.ts` | ⚠️ Partial | Basic connection tracking, missing broadcast to battle |
| `server/src/network/NetworkClient.ts` | ⚠️ Partial | Basic client wrapper, missing input queue |
| `server/src/matchmaking/Matchmaker.ts` | ⚠️ Partial | Basic queue, missing 2v2, timeout handling, range expansion |
| `server/src/battle/BattleServer.ts` | ⚠️ Partial | Skeleton only, no simulation loop, no validation |
| `server/src/index.ts` | ⚠️ Partial | Entry point, basic message handling |
| `server/src/config/index.ts` | ✅ Complete | MySQL creds configured |
| `server/src/persistence/Database.ts` | ✅ Complete | MySQL pool |
| `server/src/services/PlayerService.ts` | ✅ Complete | Auth, deck, collection |
| `server/src/utils/rng.ts` | ✅ Complete | Xorshift64* deterministic RNG |
| `server/src/utils/logger.ts` | ✅ Complete | Winston logger |

### Already Implemented (Unity)
| File | Status | Notes |
|------|--------|-------|
| `Assets/Scripts/Network/NetworkClient.cs` | ⚠️ Partial | WebSocket + JSON, missing protobuf, reconnection manager |

### Missing Files (Server)
```
server/src/network/MessageHandler.ts      # Message routing & dispatch
server/src/network/Protocol.ts            # Binary serialization (protobuf)
server/src/matchmaking/Queue.ts           # Priority queue implementation
server/src/matchmaking/RatingSystem.ts    # Trophy/ELO calculations
server/src/battle/BattleSimulation.ts     # Authoritative simulation (port from Unity)
server/src/battle/EntityManager.ts        # Entity state sync + delta compression
server/src/battle/ReplayRecorder.ts       # Deterministic replay recording
server/src/persistence/PlayerRepository.ts
server/src/persistence/BattleRepository.ts
server/src/persistence/ReplayRepository.ts
```

### Missing Files (Unity)
```
Assets/Scripts/Network/MessageTypes.cs    # Protobuf message definitions
Assets/Scripts/Network/Serialization.cs   # Binary serialization helpers
Assets/Scripts/Network/ReconnectionManager.cs  # Auto-reconnect, state recovery
```

---

## Architecture Approach

### Technology Stack
- **Server**: Node.js + TypeScript + ws (WebSocket) + mysql2 + winston
- **Client**: Unity 2022+ + .NET Standard 2.1 + System.Net.WebSockets + protobuf-net
- **Serialization**: Protobuf (binary) for network, JSON for auth/errors
- **Database**: MySQL (Aiven) via connection pool
- **Determinism**: Xorshift64* RNG, fixed timestep (60Hz), same entity update order

### Key Design Principles
1. **Server Authoritative**: All game logic runs on server; client predicts for responsiveness
2. **Deterministic Simulation**: Identical results given same seed on client and server
3. **Delta Compression**: Only send changed entities each tick
4. **Input Validation**: Card ownership, elixir cost, deploy zone, rate limiting
5. **Reconciliation**: Client rolls back and re-simulates when server state differs

---

## Implementation Steps

### Phase 1: Core Network Infrastructure (Week 1)

#### Step 1.1: Create MessageHandler.ts
**File**: `server/src/network/MessageHandler.ts`
**Purpose**: Route messages to appropriate handlers, validate message structure
```typescript
export class MessageHandler {
  constructor(
    private connectionManager: ConnectionManager,
    private matchmaker: Matchmaker,
    private battleServers: Map<string, BattleServer>,
    private playerService: PlayerService
  ) {}

  handle(client: NetworkClient, message: NetworkMessage): void {
    // Validate message has required fields
    // Route to handler based on message.type
    // Handle errors gracefully
  }
}
```
**Reuses**: `ConnectionManager`, `NetworkClient`, `Matchmaker`, `BattleServer` types

#### Step 1.2: Create Protocol.ts (Protobuf Serialization)
**File**: `server/src/network/Protocol.ts`
**Purpose**: Binary serialization using protobuf for high-performance network messages
```typescript
// Use protobufjs or @protobufjs/aspromise
// Define schemas matching types/index.ts exactly
// Provide encode/decode methods for each message type
```
**Dependencies**: `protobufjs` npm package

#### Step 1.3: Update NetworkClient.ts - Add Input Queue
**File**: `server/src/network/NetworkClient.ts` (modify)
**Changes**:
- Add `inputQueue: PlayerInput[]` for pending inputs
- Add `battleId: string | null` tracking
- Add `sendBinary(data: Buffer)` for protobuf messages
- Add `acknowledgedTick: number` for input ack tracking

#### Step 1.4: Update ConnectionManager.ts - Battle Broadcast
**File**: `server/src/network/ConnectionManager.ts` (modify)
**Changes**:
- Add `broadcastToBattle(battleId: string, message: any, excludeClientId?: string)`
- Add `getBattleConnections(battleId: string): NetworkClient[]`
- Add cleanup of battle references on disconnect

#### Step 1.5: Create Queue.ts - Priority Queue
**File**: `server/src/matchmaking/Queue.ts`
**Purpose**: Generic priority queue for matchmaking
```typescript
export class PriorityQueue<T> {
  private items: { item: T; priority: number }[] = [];
  
  enqueue(item: T, priority: number): void
  dequeue(): T | undefined
  peek(): T | undefined
  peekAt(index: number): T | undefined
  get size(): number
  clear(): void
}
```

#### Step 1.6: Create RatingSystem.ts - Trophy/ELO Calculations
**File**: `server/src/matchmaking/RatingSystem.ts`
**Purpose**: Trophy change calculations, ELO-style rating
```typescript
export class RatingSystem {
  calculateTrophyChange(winnerTrophies: number, loserTrophies: number, crownsDiff: number): number
  calculateELOChange(ratingA: number, ratingB: number, scoreA: number): number
  getMaxTrophyDiff(battleType: BattleType, avgTrophies: number): number
}
```
**Formula**: Based on Clash Royale - trophy diff determines gain/loss, 3-crown wins give bonus

#### Step 1.7: Enhance Matchmaker.ts
**File**: `server/src/matchmaking/Matchmaker.ts` (modify)
**Changes**:
- Use `PriorityQueue` from Queue.ts
- Add 2v2 support (team queue, combined trophy average)
- Add queue timeout (30s) → expand range or create bot match
- Add `expandSearchRange()` called every 5s via setInterval
- Add `createBotMatch()` for practice/timeout
- Integrate `RatingSystem` for trophy calculations

---

### Phase 2: Authoritative Battle Simulation (Week 2)

#### Step 2.1: Create BattleSimulation.ts (Port from Unity)
**File**: `server/src/battle/BattleSimulation.ts`
**Purpose**: Authoritative server-side simulation - MUST match Unity exactly

**Key Requirements**:
- Same `DeterministicRNG` (Xorshift64*) - port from `server/src/utils/rng.ts`
- Same fixed timestep: 60Hz, FIXED_DT = 1/60
- Same entity update order:
  1. Process Inputs
  2. Elixir Generation
  3. Update Spells
  4. Update Projectiles
  5. Update Units
  6. Update Buildings
  7. Update Towers
  8. Resolve Collisions
  9. Process Deaths
  10. Check Win Condition
  11. Record Events for Replay

**Classes to Port** (from Unity `BattleSimulation.cs`):
- `Entity` base class
- `Unit` - movement, targeting, attacks
- `Building` - lifetime, spawning
- `Projectile` - homing, collision
- `SpellEffect` - area effects, damage over time
- `Tower` - targeting, attacking
- `PlayerState` - elixir, hand, deck cycling
- `Pathfinding` - A* on half-tile grid
- `TargetingSystem` - priority-based targeting
- `BattleEvent` - for replay recording

**Validation**: Run determinism test - same seed on client/server must produce identical entity states at tick 1000

#### Step 2.2: Create EntityManager.ts
**File**: `server/src/battle/EntityManager.ts`
**Purpose**: Entity state synchronization with delta compression

```typescript
export class EntityManager {
  private previousState: Map<number, EntityState> = new Map();
  
  computeDelta(currentEntities: EntityState[]): EntityState[] {
    // Only return entities that changed since last broadcast
    // Include: position, velocity, hp, targetId, state-specific fields
    // Mark deleted entities with isDead = true
  }
  
  applyFullState(entities: EntityState[]): void {
    this.previousState.clear();
    for (const e of entities) this.previousState.set(e.id, e);
  }
}
```

#### Step 2.3: Create ReplayRecorder.ts
**File**: `server/src/battle/ReplayRecorder.ts`
**Purpose**: Record deterministic replays for storage and playback

```typescript
export class ReplayRecorder {
  private frames: ReplayFrame[] = [];
  private metadata: ReplayMetadata;
  
  constructor(battleId: string, seed: bigint, player1: PlayerBattleInfo, player2: PlayerBattleInfo) {
    this.metadata = { battleId, seed, player1, player2, startTime: Date.now() };
  }
  
  recordFrame(tick: number, inputs: PlayerInput[], state: GameState): void {
    // Store inputs + minimal state for verification
    this.frames.push({ tick, inputs, entitiesHash: hashEntities(state.entities) });
  }
  
  finalize(result: BattleResult): ReplayData {
    return { metadata: this.metadata, frames: this.frames, result };
  }
  
  async save(repository: ReplayRepository): Promise<string> {
    // Compress and store in MySQL
  }
}

interface ReplayFrame {
  tick: number;
  inputs: PlayerInput[];  // All inputs for this tick
  entitiesHash: string;   // For desync detection
}

interface ReplayMetadata {
  battleId: string;
  seed: bigint;
  player1: PlayerBattleInfo;
  player2: PlayerBattleInfo;
  startTime: number;
  duration: number;
}
```

#### Step 2.4: Complete BattleServer.ts
**File**: `server/src/battle/BattleServer.ts` (major rewrite)

**Required Implementation**:
```typescript
export class BattleServer {
  private simulation: BattleSimulation;
  private entityManager: EntityManager;
  private replayRecorder: ReplayRecorder;
  private tickInterval: NodeJS.Timeout;
  private inputBuffers: Map<string, PlayerInput[]> = new Map(); // Per-player input queue
  private lastBroadcastTick = 0;
  
  constructor(battleId: string, p1: PlayerBattleInfo, p2: PlayerBattleInfo, seed: number, playerService: PlayerService) {
    this.simulation = new BattleSimulation(seed);
    this.entityManager = new EntityManager();
    this.replayRecorder = new ReplayRecorder(battleId, seed, p1, p2);
    
    // Initialize with decks
    this.simulation.initialize(p1.deck, p2.deck);
    
    // Start 60Hz tick loop
    this.tickInterval = setInterval(() => this.tick(), 1000 / 60);
  }
  
  private tick(): void {
    // 1. Apply queued inputs to simulation
    for (const [playerId, inputs] of this.inputBuffers) {
      for (const input of inputs) {
        this.simulation.applyInput(playerId, input);
      }
      inputs.length = 0; // Clear processed inputs
    }
    
    // 2. Step simulation
    const state = this.simulation.step(1/60);
    
    // 3. Record for replay
    this.replayRecorder.recordFrame(this.simulation.currentTick, 
      this.getInputsForTick(), state);
    
    // 4. Compute delta and broadcast
    const delta = this.entityManager.computeDelta(state.entities);
    this.broadcastGameState(delta, state);
    
    // 5. Check end condition
    if (state.status !== BattleStatus.Playing) {
      this.endBattle(state);
    }
  }
  
  handleInput(playerId: string, input: PlayerInput): void {
    // Validate input (elixir, card ownership, position, rate limit)
    if (!this.validateInput(playerId, input)) return;
    
    // Queue for next tick
    const buffer = this.inputBuffers.get(playerId) || [];
    buffer.push(input);
    this.inputBuffers.set(playerId, buffer);
    
    // Acknowledge immediately
    this.sendInputAck(playerId, input.clientTick);
  }
  
  validateInput(playerId: string, input: PlayerInput): boolean {
    // 1. Check card ownership
    // 2. Check elixir cost
    // 3. Check deploy zone validity
    // 4. Rate limiting (max 10 inputs/sec)
    // 5. Check battle status = Playing
    return true;
  }
  
  private broadcastGameState(delta: EntityState[], state: GameState): void {
    const message: GameStateMessage = {
      type: 'game_state',
      tick: state.tick,
      entities: delta,  // Delta compressed
      projectiles: state.projectiles, // Always send projectiles (few)
      player1: state.player1,
      player2: state.player2,
      status: state.status
    };
    this.connectionManager.broadcastToBattle(this.battleId, message);
  }
}
```

#### Step 2.5: Update index.ts - Integrate BattleServer
**File**: `server/src/index.ts` (modify)
**Changes**:
- Import `BattleSimulation`, `EntityManager`, `ReplayRecorder`
- Update `handleInput` to call `battle.handleInput()`
- Add battle cleanup on end

---

### Phase 3: Persistence Layer (Week 2-3)

#### Step 3.1: Create PlayerRepository.ts
**File**: `server/src/persistence/PlayerRepository.ts`
```typescript
export class PlayerRepository {
  constructor(private db: Database) {}
  
  async findById(id: string): Promise<Player | null>
  async findByUsername(username: string): Promise<Player | null>
  async create(player: Player): Promise<void>
  async updateTrophies(playerId: string, change: number): Promise<void>
  async updateDeck(playerId: string, deckId: string, cardIds: number[]): Promise<void>
  async getCollection(playerId: string): Promise<Map<number, CardCollectionEntry>>
  async addCards(playerId: string, cardId: number, count: number): Promise<void>
}
```

#### Step 3.2: Create BattleRepository.ts
**File**: `server/src/persistence/BattleRepository.ts`
```typescript
export class BattleRepository {
  constructor(private db: Database) {}
  
  async create(battle: BattleRecord): Promise<void>
  async updateResult(battleId: string, result: BattleResult): Promise<void>
  async getHistory(playerId: string, limit: number): Promise<BattleRecord[]>
  async getById(battleId: string): Promise<BattleRecord | null>
}
```

#### Step 3.3: Create ReplayRepository.ts
**File**: `server/src/persistence/ReplayRepository.ts`
```typescript
export class ReplayRepository {
  constructor(private db: Database) {}
  
  async save(replay: ReplayData): Promise<string> // Returns replayId
  async findById(replayId: string): Promise<ReplayData | null>
  async search(query: ReplaySearchQuery): Promise<ReplayData[]>
  async deleteOld(retentionDays: number): Promise<number>
}

interface ReplaySearchQuery {
  playerId?: string;
  deckHash?: string;
  minDuration?: number;
  maxDuration?: number;
  battleType?: BattleType;
  dateFrom?: Date;
  dateTo?: Date;
}
```

#### Step 3.4: Update PlayerService.ts - Use Repositories
**File**: `server/src/services/PlayerService.ts` (modify)
**Changes**: Refactor to use `PlayerRepository`, `BattleRepository`, `ReplayRepository`

---

### Phase 4: Unity Client - Complete Network Stack (Week 3)

#### Step 4.1: Create MessageTypes.cs (Protobuf Definitions)
**File**: `Assets/Scripts/Network/MessageTypes.cs`
**Purpose**: Protobuf message definitions matching server `types/index.ts` exactly

```csharp
[ProtoContract]
public class NetworkMessage {
    [ProtoMember(1)] public string type;
    [ProtoMember(2)] public uint requestId;
    [ProtoMember(3)] public ulong timestamp;
}

[ProtoContract]
public class AuthMessage : NetworkMessage {
    [ProtoMember(10)] public string token;
}

[ProtoContract]
public class PlayerInput {
    [ProtoMember(1)] public InputType type;
    [ProtoMember(2)] public int cardId;
    [ProtoMember(3)] public int spellId;
    [ProtoMember(4)] public Vector2 position;
    [ProtoMember(5)] public Vector2 targetPosition;
    [ProtoMember(6)] public uint clientTick;
}

[ProtoContract]
public class GameStateMessage : NetworkMessage {
    [ProtoMember(10)] public uint tick;
    [ProtoMember(11)] public EntityState[] entities;
    [ProtoMember(12)] public EntityState[] projectiles;
    [ProtoMember(13)] public PlayerState player1;
    [ProtoMember(14)] public PlayerState player2;
    [ProtoMember(15)] public BattleStatus status;
}

// ... all other message types matching server/types/index.ts
```
**Dependencies**: `protobuf-net` NuGet package

#### Step 4.2: Create Serialization.cs
**File**: `Assets/Scripts/Network/Serialization.cs`
**Purpose**: Binary serialization helpers for protobuf

```csharp
public static class Serialization {
    public static byte[] Serialize<T>(T obj) where T : class;
    public static T Deserialize<T>(byte[] data) where T : class;
    public static byte[] SerializeLengthPrefixed<T>(T obj) where T : class;
    public static T DeserializeLengthPrefixed<T>(byte[] data, int offset, int count) where T : class;
}
```

#### Step 4.3: Create ReconnectionManager.cs
**File**: `Assets/Scripts/Network/ReconnectionManager.cs`
**Purpose**: Auto-reconnect with exponential backoff, state recovery

```csharp
public class ReconnectionManager : MonoBehaviour {
    private NetworkClient _networkClient;
    private int _reconnectAttempts = 0;
    private const int MAX_RECONNECT_ATTEMPTS = 10;
    private const float BASE_DELAY = 5f;
    private const float MAX_DELAY = 60f;
    
    public void HandleDisconnect(string reason) {
        if (_reconnectAttempts < MAX_RECONNECT_ATTEMPTS) {
            float delay = Mathf.Min(BASE_DELAY * Mathf.Pow(2, _reconnectAttempts), MAX_DELAY);
            StartCoroutine(ReconnectAfterDelay(delay));
        } else {
            // Show "Connection lost" UI, return to main menu
        }
    }
    
    private IEnumerator ReconnectAfterDelay(float delay) {
        yield return new WaitForSeconds(delay);
        _networkClient.Reconnect();
        _reconnectAttempts++;
    }
    
    public void OnReconnected() {
        _reconnectAttempts = 0;
        // Request state resync from server
        _networkClient.Send(new ReconcileRequest { lastKnownTick = _lastServerTick });
    }
}
```

#### Step 4.4: Enhance NetworkClient.cs - Full Implementation
**File**: `Assets/Scripts/Network/NetworkClient.cs` (major rewrite)

**Required Changes**:
1. **Switch to Protobuf**: Use `MessageTypes.cs` + `Serialization.cs`
2. **Input Queue with Retransmission**:
   - Track send time for each input
   - Retransmit unacknowledged inputs after 1 second
   - Max queue size: 10 inputs
3. **Heartbeat**: Send ping every 10s, expect pong within 5s
4. **Reconciliation Handler**:
   - On `game_state`: Compare local simulation with server state
   - If desync > threshold: Request full resync (`reconcile` message)
   - On `reconcile`: Rollback local simulation to server tick, replay inputs
5. **Reconnection Integration**: Use `ReconnectionManager` for auto-reconnect

---

### Phase 5: Integration & Testing (Week 3-4)

#### Step 5.1: Server Unit Tests
**File**: `server/tests/`
- `rng.test.ts` - DeterministicRNG produces same sequence
- `matchmaking.test.ts` - Queue ordering, trophy matching, 2v2 logic
- `rating.test.ts` - Trophy/ELO calculations
- `validation.test.ts` - Input validation rules
- `simulation.test.ts` - BattleSimulation determinism (same seed = same result)

#### Step 5.2: Integration Test - Full 1v1 Flow
**File**: `server/tests/integration/battle.test.ts`
```typescript
test('full 1v1 battle completes', async () => {
  const server = await startTestServer();
  const client1 = await connectClient(server, 'player1');
  const client2 = await connectClient(server, 'player2');
  
  await matchmake(client1, client2);
  
  // Play cards programmatically
  await playCard(client1, CardId.Knight, new Vector2(9, 8));
  await playCard(client2, CardId.Arrows, new Vector2(9, 8));
  
  const result = await waitForBattleEnd(client1, 30000);
  expect(result.winner).toBeDefined();
});
```

#### Step 5.3: Desync Test
- Inject 200ms lag on one client
- Verify reconciliation corrects state within 2 ticks

#### Step 5.4: Reconnection Test
- Disconnect mid-battle
- Reconnect within 10s grace period
- Verify receives current game state

#### Step 5.5: Load Test (k6)
- 1000 concurrent connections
- 100 battles/sec
- Monitor CPU, memory, tick time

---

## Database Schema Additions

Run these migrations after `server/sql/init.sql`:

```sql
-- Battle records
CREATE TABLE IF NOT EXISTS battles (
    battle_id VARCHAR(64) PRIMARY KEY,
    battle_type ENUM('ladder','2v2','tournament','challenge','friendly','practice','clan_war') NOT NULL,
    seed BIGINT UNSIGNED NOT NULL,
    player1_id CHAR(36) NOT NULL,
    player2_id CHAR(36) NOT NULL,
    player1_deck JSON NOT NULL,
    player2_deck JSON NOT NULL,
    winner ENUM('player1','player2','draw'),
    player1_crowns TINYINT UNSIGNED DEFAULT 0,
    player2_crowns TINYINT UNSIGNED DEFAULT 0,
    player1_trophy_change SMALLINT DEFAULT 0,
    player2_trophy_change SMALLINT DEFAULT 0,
    duration_seconds INT UNSIGNED,
    went_overtime BOOLEAN DEFAULT FALSE,
    replay_id VARCHAR(64),
    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    ended_at TIMESTAMP NULL,
    INDEX idx_player1 (player1_id),
    INDEX idx_player2 (player2_id),
    INDEX idx_created (created_at)
);

-- Replays (compressed protobuf/JSON)
CREATE TABLE IF NOT EXISTS replays (
    replay_id VARCHAR(64) PRIMARY KEY,
    battle_id VARCHAR(64) NOT NULL,
    data LONGBLOB NOT NULL,  -- Compressed replay data
    metadata JSON NOT NULL,  -- battleId, seed, players, duration
    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    INDEX idx_battle (battle_id),
    INDEX idx_created (created_at)
);

-- Matchmaking queue (for persistence across restarts)
CREATE TABLE IF NOT EXISTS matchmaking_queue (
    id BIGINT UNSIGNED AUTO_INCREMENT PRIMARY KEY,
    player_id CHAR(36) NOT NULL,
    battle_type ENUM('ladder','2v2','tournament','challenge','friendly','practice','clan_war') NOT NULL,
    trophies INT NOT NULL,
    deck JSON NOT NULL,
    joined_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    UNIQUE KEY unique_player_type (player_id, battle_type)
);
```

---

## Validation Commands

### Server
```bash
cd server
npm install
npm run build          # TypeScript compilation
npm run test           # Unit tests
npm run test:integration  # Integration tests
npm run dev            # Start dev server (port 3000 HTTP, 3001 WS)
```

### Unity
```bash
# Open in Unity Editor
# Run Play Mode tests: Window > General > Test Runner
# Run Edit Mode tests for serialization
```

### Determinism Verification
```bash
# Run same seed on client and server, compare entity states at tick 1000
npm run test:determinism
```

---

## Edge Cases & Error Handling

| Scenario | Handling |
|----------|----------|
| Client sends invalid input | Reject silently, log warning, send error if repeated |
| Client disconnects mid-battle | 10s grace period, then forfeit |
| Server tick takes >16ms | Log warning, skip broadcast for this tick |
| Desync detected | Send full `reconcile` message, client rolls back |
| Matchmaking timeout (30s) | Expand trophy range to 1000, or create bot match |
| Replay storage full | Delete oldest replays beyond retention (30 days) |
| Database connection lost | Queue writes, retry with exponential backoff |

---

## Risks & Mitigations

| Risk | Probability | Impact | Mitigation |
|------|-------------|--------|------------|
| Determinism mismatch client/server | High | Critical | Extensive testing, shared RNG, fixed math |
| WebSocket connection drops | Medium | High | Auto-reconnect, input buffering, grace period |
| Matchmaking queue starvation | Low | Medium | Bot matches, range expansion |
| Replay storage growth | Medium | Low | Compression, 30-day retention, cleanup job |
| Input validation bypass | Low | Critical | Server-only validation, rate limiting |

---

## Files That Should NOT Change

| File | Reason |
|------|--------|
| `server/src/types/index.ts` | Frozen - shared contract |
| `Assets/Scripts/Core/` | Frozen contracts (Agent 1 owns) |
| `Assets/Scripts/Battle/Simulation/` | Agent 1 owns Unity version (port logic only) |
| `Assets/Scripts/UI/` | Agent 3 owns |
| `server/src/services/` | Agent 5 owns |
| `server/src/persistence/*.ts` repositories | Agent 5 owns (you use them) |

---

## Dependencies

| Dependency | Owner | Integration Point |
|------------|-------|-------------------|
| Agent 1 BattleSimulation | Agent 1 | Port logic to `BattleSimulation.ts` - verify identical results |
| Agent 5 Database/Repositories | Agent 5 | Use `PlayerRepository`, `BattleRepository`, `ReplayRepository` |
| Agent 3 UI | Agent 3 | Needs `NetworkClient` for matchmaking, battle actions |
| Agent 6 Tests | Agent 6 | Writes tests for your code |

---

## Success Criteria

- [ ] Unity client connects via WSS, authenticates, enters matchmaking
- [ ] 1v1 matchmaking finds match within trophy range
- [ ] Battle starts, both clients receive same seed
- [ ] Client plays card → server validates → simulation steps → both clients receive state
- [ ] Reconciliation corrects desync within 2 ticks
- [ ] Battle ends, crowns/trophies calculated, replay saved
- [ ] Reconnection mid-battle restores state
- [ ] 1000 concurrent connections, 100 battles/sec on load test
- [ ] Determinism test passes: client/server entity states match at tick 1000