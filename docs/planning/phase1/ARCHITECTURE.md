# Clash Royale Clone - Technical Architecture

## 1. HIGH-LEVEL ARCHITECTURE

```
┌─────────────────────────────────────────────────────────────────────────┐
│                            CLIENT (Unity/Godot)                         │
├─────────────────────────────────────────────────────────────────────────┤
│  ┌──────────┐ ┌──────────┐ ┌──────────┐ ┌──────────┐ ┌──────────┐      │
│  │  Battle  │ │   Lobby  │ │  Deck    │ │  Shop/   │ │  Clan/   │      │
│  │  Scene   │ │  Scene   │ │ Builder  │ │ Profile  │ │ Social   │      │
│  └──────────┘ └──────────┘ └──────────┘ └──────────┘ └──────────┘      │
│         │            │            │            │            │           │
│         └────────────┴────────────┴────────────┴────────────┘           │
│                              │                                           │
│                    ┌─────────▼─────────┐                                │
│                    │   Game Manager    │                                │
│                    │  (Singleton)      │                                │
│                    └─────────┬─────────┘                                │
│                              │                                           │
│         ┌────────────────────┼────────────────────┐                     │
│         ▼                    ▼                    ▼                      │
│  ┌─────────────┐    ┌─────────────┐    ┌─────────────┐                 │
│  │ Network     │    │ Data        │    │ Asset       │                 │
│  │ Layer       │    │ Layer       │    │ Manager     │                 │
│  └─────────────┘    └─────────────┘    └─────────────┘                 │
└─────────────────────────────────────────────────────────────────────────┘
                              │
                    ┌─────────▼─────────┐
                    │   Game Server     │
                    │  (Node.js/Go)     │
                    └─────────┬─────────┘
                              │
         ┌────────────────────┼────────────────────┐
         ▼                    ▼                    ▼
┌─────────────────┐ ┌─────────────────┐ ┌─────────────────┐
│  Matchmaking    │ │  Battle         │ │  Persistence    │
│  Service        │ │  Simulation     │ │  Service        │
└─────────────────┘ └─────────────────┘ └─────────────────┘
         │                    │                    │
         ▼                    ▼                    ▼
┌─────────────────┐ ┌─────────────────┐ ┌─────────────────┐
│  Redis          │ │  Deterministic  │ │  MySQL          │
│  (Queue/State)  │ │  Engine         │ │  (Aiven)        │
└─────────────────┘ └─────────────────┘ └─────────────────┘
```

## 2. CLIENT ARCHITECTURE (Unity)

### 2.1 Project Structure
```
Assets/
├── Scripts/
│   ├── Core/
│   │   ├── GameManager.cs
│   │   ├── ServiceLocator.cs
│   │   ├── EventBus.cs
│   │   └── CoroutineRunner.cs
│   ├── Battle/
│   │   ├── Simulation/
│   │   │   ├── BattleSimulation.cs
│   │   │   ├── Entity.cs
│   │   │   ├── Unit.cs
│   │   │   ├── Building.cs
│   │   │   ├── Spell.cs
│   │   │   ├── Projectile.cs
│   │   │   ├── Tower.cs
│   │   │   ├── Pathfinding.cs
│   │   │   └── Targeting.cs
│   │   ├── Presentation/
│   │   │   ├── BattleView.cs
│   │   │   ├── UnitView.cs
│   │   │   ├── EffectManager.cs
│   │   │   └── CameraController.cs
│   │   ├── Input/
│   │   │   ├── InputManager.cs
│   │   │   ├── CardDragHandler.cs
│   │   │   └── SpellAimHandler.cs
│   │   └── UI/
│   │       ├── HandBar.cs
│   │       ├── ElixirBar.cs
│   │       ├── TowerHealthUI.cs
│   │       └── BattleHUD.cs
│   ├── Network/
│   │   ├── NetworkClient.cs
│   │   ├── MessageTypes.cs
│   │   ├── Serialization.cs
│   │   └── ReconnectionManager.cs
│   ├── Data/
│   │   ├── CardDatabase.cs
│   │   ├── PlayerData.cs
│   │   ├── DeckManager.cs
│   │   └── ConfigManager.cs
│   ├── UI/
│   │   ├── Screens/
│   │   ├── Components/
│   │   └── Animation/
│   └── Systems/
│       ├── AudioManager.cs
│       ├── ParticleManager.cs
│       └── PoolManager.cs
├── Resources/
│   ├── Configs/
│   └── Data/
├── Prefabs/
│   ├── Units/
│   ├── Buildings/
│   ├── Spells/
│   ├── Projectiles/
│   └── UI/
├── Scenes/
│   ├── Battle.unity
│   ├── Lobby.unity
│   ├── DeckBuilder.unity
│   └── MainMenu.unity
└── Shaders/
    ├── UnitShader.shader
    ├── RiverShader.shader
    └── SpellShaders/
```

### 2.2 Core Systems

#### GameManager (Singleton)
```csharp
public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }
    
    // Services
    public NetworkClient Network { get; private set; }
    public DataManager Data { get; private set; }
    public AssetManager Assets { get; private set; }
    public AudioManager Audio { get; private set; }
    public PoolManager Pool { get; private set; }
    public ConfigManager Config { get; private set; }
    
    // State
    public GameState CurrentState { get; private set; }
    public PlayerLocalData LocalPlayer { get; private set; }
    
    // Scene management
    public void LoadScene(GameScene scene, Action onComplete);
    public void ChangeState(GameState newState);
}
```

#### EventBus (Decoupled Communication)
```csharp
public static class EventBus
{
    // Battle events
    public static event Action<CardPlayedEvent> OnCardPlayed;
    public static event Action<UnitSpawnedEvent> OnUnitSpawned;
    public static event Action<UnitDiedEvent> OnUnitDied;
    public static event Action<TowerDamagedEvent> OnTowerDamaged;
    public static event Action<BattleEndedEvent> OnBattleEnded;
    
    // Network events
    public static event Action<NetworkMessage> OnMessageReceived;
    public static event Action OnConnected;
    public static event Action<string> OnDisconnected;
    
    // UI events
    public static event Action<ScreenType> OnScreenChange;
    public static event Action<string, object> OnToast;
}
```

#### ServiceLocator (Dependency Injection Lite)
```csharp
public static class Services
{
    private static readonly Dictionary<Type, object> _services = new();
    
    public static void Register<T>(T service) where T : class 
        => _services[typeof(T)] = service;
    
    public static T Get<T>() where T : class 
        => _services.TryGetValue(typeof(T), out var s) ? s as T : null;
    
    public static void Unregister<T>() where T : class 
        => _services.Remove(typeof(T));
}
```

### 2.3 Battle Simulation (Deterministic)

#### Fixed Timestep Loop
```csharp
public class BattleSimulation : MonoBehaviour
{
    const int TICK_RATE = 60; // 60 Hz
    const float FIXED_DT = 1f / TICK_RATE;
    
    private float _accumulator = 0f;
    private uint _currentTick = 0;
    private uint _serverTick = 0;
    
    // Deterministic RNG
    private DeterministicRNG _rng;
    
    // Entities
    private Dictionary<uint, Entity> _entities = new();
    private List<Entity> _units = new();
    private List<Entity> _buildings = new();
    private List<Projectile> _projectiles = new();
    private List<SpellEffect> _activeSpells = new();
    
    // Players
    private PlayerState _player1;
    private PlayerState _player2;
    
    void FixedUpdate()
    {
        _accumulator += Time.fixedDeltaTime;
        
        while (_accumulator >= FIXED_DT)
        {
            Tick(FIXED_DT);
            _accumulator -= FIXED_DT;
            _currentTick++;
        }
        
        // Interpolation for rendering
        float alpha = _accumulator / FIXED_DT;
        RenderInterpolated(alpha);
    }
    
    void Tick(float dt)
    {
        // 1. Process network inputs (validated)
        ProcessInputs();
        
        // 2. Elixir generation
        UpdateElixir(dt);
        
        // 3. Entity updates (order matters!)
        UpdateSpells(dt);
        UpdateProjectiles(dt);
        UpdateUnits(dt);
        UpdateBuildings(dt);
        UpdateTowers(dt);
        
        // 4. Collision & Targeting
        ResolveCollisions();
        UpdateTargeting();
        
        // 5. Death processing
        ProcessDeaths();
        
        // 6. Win condition check
        CheckWinCondition();
        
        // 7. Generate deterministic events for replay
        RecordTickEvents();
    }
}
```

#### Entity Component System (Lightweight)
```csharp
public abstract class Entity
{
    public uint Id { get; protected set; }
    public uint OwnerPlayerId { get; protected set; }
    public Vector2 Position { get; set; }
    public Vector2 Velocity { get; set; }
    public float Rotation { get; set; }
    public int CurrentHP { get; protected set; }
    public int MaxHP { get; protected set; }
    public EntityType Type { get; protected set; }
    public bool IsDead => CurrentHP <= 0;
    
    public abstract void Tick(float dt);
    public virtual void TakeDamage(int amount, DamageType type, uint sourceId);
    public virtual void Die();
}

public class Unit : Entity
{
    public UnitData Data { get; private set; }
    public UnitState State { get; private set; }
    public Entity Target { get; private set; }
    public float AttackCooldown { get; private set; }
    public float MoveSpeed { get; private set; }
    public List<StatusEffect> StatusEffects { get; } = new();
    
    // Pathfinding
    private List<Vector2> _path;
    private int _pathIndex;
    
    public override void Tick(float dt)
    {
        UpdateStatusEffects(dt);
        
        if (IsStunned || IsFrozen) return;
        
        if (Target != null && Target.IsDead)
            Target = null;
            
        if (Target != null && InAttackRange(Target))
        {
            TryAttack();
        }
        else
        {
            UpdateMovement(dt);
        }
    }
    
    private void TryAttack()
    {
        if (AttackCooldown <= 0)
        {
            AttackCooldown = Data.HitSpeed;
            PerformAttack();
        }
        AttackCooldown -= Time.fixedDeltaTime;
    }
}
```

#### Targeting System
```csharp
public static class TargetingSystem
{
    public static Entity FindTarget(Entity attacker, List<Entity> candidates)
    {
        Entity bestTarget = null;
        float bestScore = float.MaxValue;
        
        foreach (var candidate in candidates)
        {
            if (!IsValidTarget(attacker, candidate)) continue;
            
            float distance = Vector2.Distance(attacker.Position, candidate.Position);
            float pathDistance = GetPathDistance(attacker, candidate);
            
            // Score: prioritize path distance, then target priority
            float score = pathDistance * 1000 + GetTargetPriority(attacker, candidate);
            
            if (score < bestScore)
            {
                bestScore = score;
                bestTarget = candidate;
            }
        }
        
        return bestTarget;
    }
    
    private static int GetTargetPriority(Entity attacker, Entity target)
    {
        // Building-targeting troops prioritize buildings
        if (attacker.Data.TargetType == TargetType.Buildings)
        {
            if (target.Type == EntityType.Building) return 0;
            if (target.Type == EntityType.Tower) return 1;
            return 1000;
        }
        
        // Normal troops: troops > buildings > towers
        if (target.Type == EntityType.Unit) return 0;
        if (target.Type == EntityType.Building) return 1;
        if (target.Type == EntityType.Tower) return 2;
        return 100;
    }
}
```

#### Pathfinding (A* on Grid)
```csharp
public class Pathfinding
{
    private const int GRID_WIDTH = 36;  // 18 tiles * 2 (half-tile precision)
    private const int GRID_HEIGHT = 64; // 32 tiles * 2
    private bool[,] _walkableGrid;
    private Node[,] _nodes;
    
    public List<Vector2> FindPath(Vector2 start, Vector2 end, EntityType entityType)
    {
        var startNode = WorldToGrid(start);
        var endNode = WorldToGrid(end);
        
        if (!IsWalkable(endNode, entityType))
            endNode = FindNearestWalkable(endNode, entityType);
        
        return AStar(startNode, endNode, entityType);
    }
    
    private List<Vector2> AStar(Node start, Node end, EntityType type)
    {
        // Standard A* with binary heap
        // Heuristic: Manhattan distance * 10
        // Cost: 10 for orthogonal, 14 for diagonal
        // Impassable: River (for ground), map bounds
    }
}
```

### 2.4 Networking

#### Message Protocol (Protocol Buffers)
```protobuf
// messages.proto
syntax = "proto3";

package crclone;

message Vector2 {
    float x = 1;
    float y = 2;
}

message CardPlayed {
    int32 card_id = 1;
    Vector2 position = 2;
    uint32 tick = 3;
}

message SpellCast {
    int32 spell_id = 1;
    Vector2 position = 2;
    uint32 tick = 3;
}

message PlayerInput {
    oneof input {
        CardPlayed card_played = 1;
        SpellCast spell_cast = 2;
        EmoteUsed emote = 3;
    }
    uint32 client_tick = 100;
}

message GameState {
    uint32 tick = 1;
    repeated EntityState entities = 2;
    repeated ProjectileState projectiles = 3;
    PlayerState player1 = 4;
    PlayerState player2 = 5;
    BattleStatus status = 6;
}

message EntityState {
    uint32 id = 1;
    int32 type = 2; // EntityType enum
    uint32 owner = 3;
    Vector2 position = 4;
    Vector2 velocity = 5;
    int32 hp = 6;
    uint32 target_id = 7;
    repeated StatusEffectState effects = 8;
}
```

#### Network Client
```csharp
public class NetworkClient : MonoBehaviour
{
    private WebSocket _ws;
    private uint _lastAckedTick = 0;
    private Queue<PlayerInput> _pendingInputs = new();
    private Dictionary<uint, PlayerInput> _sentInputs = new();
    
    public void Connect(string serverUrl, string authToken)
    {
        _ws = new WebSocket(serverUrl);
        _ws.OnMessage += OnMessage;
        _ws.OnOpen += OnOpen;
        _ws.OnClose += OnClose;
        _ws.OnError += OnError;
    }
    
    public void SendInput(PlayerInput input)
    {
        _pendingInputs.Enqueue(input);
    }
    
    private void Update()
    {
        // Send pending inputs
        while (_pendingInputs.Count > 0)
        {
            var input = _pendingInputs.Dequeue();
            _sentInputs[input.ClientTick] = input;
            _ws.Send(Serialize(input));
        }
        
        // Handle timeouts/retransmission
        CheckRetransmission();
    }
    
    private void OnMessage(byte[] data)
    {
        var msg = Deserialize<GameMessage>(data);
        
        switch (msg.Type)
        {
            case MessageType.GameState:
                OnGameState(msg.State);
                break;
            case MessageType.InputAck:
                OnInputAck(msg.AckTick);
                break;
            case MessageType.Reconcile:
                OnReconcile(msg.State);
                break;
        }
    }
    
    private void OnGameState(GameState state)
    {
        _serverTick = state.Tick;
        _lastAckedTick = state.Tick;
        
        // Reconcile local simulation
        Simulation.Reconcile(state);
    }
}
```

### 2.5 Server Architecture (Node.js/TypeScript)

#### Project Structure
```
server/
├── src/
│   ├── index.ts                 # Entry point
│   ├── config/
│   │   └── index.ts
│   ├── network/
│   │   ├── WebSocketServer.ts
│   │   ├── ConnectionManager.ts
│   │   ├── MessageHandler.ts
│   │   └── Protocol.ts
│   ├── matchmaking/
│   │   ├── Matchmaker.ts
│   │   ├── Queue.ts
│   │   └── RatingSystem.ts
│   ├── battle/
│   │   ├── BattleServer.ts
│   │   ├── BattleSimulation.ts  # Authoritative simulation
│   │   ├── EntityManager.ts
│   │   └── ReplayRecorder.ts
│   ├── persistence/
│   │   ├── Database.ts
│   │   ├── PlayerRepository.ts
│   │   ├── BattleRepository.ts
│   │   └── ReplayRepository.ts
│   ├── services/
│   │   ├── PlayerService.ts
│   │   ├── ClanService.ts
│   │   ├── ShopService.ts
│   │   └── QuestService.ts
│   ├── utils/
│   │   ├── Logger.ts
│   │   ├── DeterministicRNG.ts
│   │   └── Metrics.ts
│   └── types/
│       └── index.ts
├── package.json
├── tsconfig.json
└── Dockerfile
```

#### Battle Server (Authoritative)
```typescript
class BattleServer {
    private simulation: BattleSimulation;
    private players: Map<string, PlayerConnection> = new Map();
    private replayRecorder: ReplayRecorder;
    private tickRate = 60;
    private tickInterval: NodeJS.Timeout;
    
    constructor(battleId: string, player1: PlayerConnection, player2: PlayerConnection, seed: number) {
        this.simulation = new BattleSimulation(seed);
        this.replayRecorder = new ReplayRecorder(battleId, seed);
        
        this.players.set(player1.id, player1);
        this.players.set(player2.id, player2);
        
        // Initialize simulation with decks
        this.simulation.initialize(player1.deck, player2.deck);
        
        // Start tick loop
        this.tickInterval = setInterval(() => this.tick(), 1000 / this.tickRate);
    }
    
    private tick() {
        // Process queued inputs
        for (const [playerId, conn] of this.players) {
            while (conn.inputQueue.length > 0) {
                const input = conn.inputQueue.shift();
                this.simulation.applyInput(playerId, input);
            }
        }
        
        // Step simulation
        const state = this.simulation.step(1/60);
        
        // Record for replay
        this.replayRecorder.record(state);
        
        // Broadcast to clients
        this.broadcastState(state);
        
        // Check end condition
        if (state.status !== BattleStatus.Playing) {
            this.endBattle(state);
        }
    }
    
    private broadcastState(state: GameState) {
        const data = serialize(state);
        for (const conn of this.players.values()) {
            if (conn.ws.readyState === WebSocket.OPEN) {
                conn.ws.send(data);
            }
        }
    }
    
    handleInput(playerId: string, input: PlayerInput) {
        const conn = this.players.get(playerId);
        if (conn) {
            conn.inputQueue.push(input);
        }
    }
}
```

#### Matchmaking
```typescript
class Matchmaker {
    private queues: Map<BattleType, PriorityQueue<PlayerEntry>> = new Map();
    private ratingSystem: RatingSystem;
    
    async addToQueue(player: PlayerEntry) {
        const queue = this.queues.get(player.battleType);
        queue.enqueue(player, -player.trophies); // Higher trophies = higher priority
        
        this.tryMatch(queue, player.battleType);
    }
    
    private tryMatch(queue: PriorityQueue<PlayerEntry>, type: BattleType) {
        while (queue.size >= 2) {
            const p1 = queue.peek();
            const p2 = queue.peekAt(1);
            
            // Check trophy difference (configurable)
            const diff = Math.abs(p1.trophies - p2.trophies);
            const maxDiff = this.getMaxTrophyDiff(type, p1.trophies);
            
            if (diff <= maxDiff) {
                queue.dequeue();
                queue.dequeue();
                this.createBattle(p1, p2, type);
            } else {
                // Expand search or wait
                break;
            }
        }
    }
    
    private createBattle(p1: PlayerEntry, p2: PlayerEntry, type: BattleType) {
        const battleId = generateId();
        const seed = DeterministicRNG.generateSeed();
        
        const battle = new BattleServer(battleId, p1, p2, seed);
        
        // Notify both players
        p1.ws.send({ type: 'battle_found', battleId, seed, opponent: p2.info });
        p2.ws.send({ type: 'battle_found', battleId, seed, opponent: p1.info });
    }
}
```

## 3. DATA FLOW

### 3.1 Battle Flow
```
Client A                    Server                      Client B
   │                          │                           │
   ├── Matchmaking Request ──▶│                           │
   │◀─ Battle Found (seed) ───┤                           │
   │                          ├── Battle Found (seed) ───▶│
   │                          │                           │
   │    (Load Scene, Init)    │                           │
   │                          │                           │
   ├──── Card Played ───────▶│                           │
   │                          ├── Validate & Queue ──────▶│
   │                          │                           │
   │◀──── Game State ────────┤                           │
   │                          ├── Game State ────────────▶│
   │                          │                           │
   │    (Reconcile & Render)  │                           │
   │                          │                           │
   │                     [60 Hz Tick Loop]                │
   │                          │                           │
   │◀──── Battle Ended ──────┤                           │
   │                          ├── Battle Ended ─────────▶│
   │                          │                           │
```

### 3.2 Reconciliation
```
Client Simulation          Server Simulation
     │                          │
     ├── Tick 100 ─────────────▶│
     │                          ├── Process Input
     │                          ├── Simulate
     │◀── State @ Tick 100 ─────┤
     │                          │
     │  Compare local vs server │
     │  If diff > threshold:    │
     │    Rollback to tick 100  │
     │    Re-simulate with      │
     │    server inputs         │
     │                          │
```

## 4. DETERMINISTIC REQUIREMENTS

### 4.1 Sources of Non-Determinism
| Source | Solution |
|--------|----------|
| Float math | Fixed-point (int32, 1/1024 precision) or deterministic float |
| RNG | DeterministicRNG (seed-based, Xorshift/Xoshiro) |
| Physics | Custom deterministic physics, no Unity Physics |
| Dictionary iteration | Sorted containers or arrays |
| Multithreading | Single-threaded simulation |
| Time | Fixed timestep, no deltaTime variance |

### 4.2 Deterministic RNG
```csharp
public struct DeterministicRNG
{
    private ulong _state;
    
    public DeterministicRNG(ulong seed) => _state = seed;
    
    public uint NextUInt()
    {
        _state ^= _state >> 12;
        _state ^= _state << 25;
        _state ^= _state >> 27;
        return (uint)(_state * 0x2545F4914F6CDD1D);
    }
    
    public float NextFloat() => NextUInt() * (1f / 0xFFFFFFFF);
    public int NextInt(int min, int max) => min + (int)(NextFloat() * (max - min));
    public bool NextBool(float probability) => NextFloat() < probability;
}
```

### 4.3 Fixed-Point Math (Optional)
```csharp
public struct Fixed
{
    public const int FRACTIONAL_BITS = 10; // 1/1024 precision
    public const int SCALE = 1 << FRACTIONAL_BITS;
    private int _value;
    
    public static Fixed FromFloat(float f) => new Fixed { _value = (int)(f * SCALE) };
    public float ToFloat() => _value / (float)SCALE;
    
    public static Fixed operator +(Fixed a, Fixed b) => new Fixed { _value = a._value + b._value };
    public static Fixed operator -(Fixed a, Fixed b) => new Fixed { _value = a._value - b._value };
    public static Fixed operator *(Fixed a, Fixed b) => new Fixed { _value = (int)(((long)a._value * b._value) >> FRACTIONAL_BITS) };
    public static Fixed operator /(Fixed a, Fixed b) => new Fixed { _value = (int)(((long)a._value << FRACTIONAL_BITS) / b._value) };
}
```

## 5. PERFORMANCE OPTIMIZATION

### 5.1 Object Pooling
```csharp
public class PoolManager : MonoBehaviour
{
    private Dictionary<string, Queue<GameObject>> _pools = new();
    private Dictionary<string, GameObject> _prefabs = new();
    
    public GameObject Spawn(string key, Vector3 position, Quaternion rotation)
    {
        if (!_pools.ContainsKey(key)) _pools[key] = new Queue<GameObject>();
        
        GameObject obj;
        if (_pools[key].Count > 0)
        {
            obj = _pools[key].Dequeue();
            obj.transform.SetPositionAndRotation(position, rotation);
            obj.SetActive(true);
        }
        else
        {
            obj = Instantiate(_prefabs[key], position, rotation);
            obj.name = key;
        }
        
        return obj;
    }
    
    public void Despawn(string key, GameObject obj)
    {
        obj.SetActive(false);
        obj.transform.SetParent(transform);
        _pools[key].Enqueue(obj);
    }
}
```

### 5.2 Rendering Optimization
- **GPU Instancing**: For units of same type
- **Sprite Atlas**: All UI and unit sprites in atlases
- **LOD**: Simplified meshes for distant units
- **Culling**: Frustum + distance culling
- **Batching**: Dynamic batching for UI, GPU instancing for units

### 5.3 Memory Management
- **Addressables**: For asset loading/unloading
- **Texture Compression**: ASTC 4x4 mobile, BC7 desktop
- **Audio**: Compressed OGG, streaming for music
- **GC**: Minimize allocations in simulation tick

## 6. DEPLOYMENT ARCHITECTURE

### 6.1 Development
```
Local Machine
├── Unity Editor (Client)
├── Node.js Server (Local)
├── MySQL (Docker Local)
└── Redis (Docker Local)
```

### 6.2 Production (Conceptual)
```
┌─────────────┐     ┌─────────────┐     ┌─────────────┐
│   CDN       │     │  Load       │     │  Game       │
│  (Assets)   │────▶│  Balancer   │────▶│  Servers    │
└─────────────┘     └─────────────┘     └──────┬──────┘
                                                │
                    ┌─────────────┐             │
                    │  MySQL      │◀────────────┤
                    │  (Aiven)    │             │
                    └─────────────┘             │
                                                │
                    ┌─────────────┐             │
                    │  Redis      │◀────────────┤
                    │  Cluster    │             │
                    └─────────────┘             │
```

### 6.3 Docker Compose (Local Dev)
```yaml
version: '3.8'
services:
  mysql:
    image: mysql:8.0
    environment:
      MYSQL_DATABASE: crclone
      MYSQL_ROOT_PASSWORD: ${DB_PASSWORD}
    ports: ["3306:3306"]
    volumes: [mysql_data:/var/lib/mysql]
    
  redis:
    image: redis:7-alpine
    ports: ["6379:6379"]
    
  server:
    build: ./server
    ports: ["3000:3000", "3001:3001"] # HTTP + WS
    environment:
      DB_HOST: mysql
      REDIS_HOST: redis
      JWT_SECRET: ${JWT_SECRET}
    depends_on: [mysql, redis]
    
volumes:
  mysql_data:
```

## 7. SECURITY

### 7.1 Client-Server Trust Model
- **Server Authoritative**: All game logic on server
- **Client Prediction**: Local simulation for responsiveness
- **Anti-Cheat**: Input validation, rate limiting, replay verification
- **Encryption**: WSS for WebSocket, HTTPS for REST

### 7.2 Input Validation
```typescript
function validateInput(input: PlayerInput, playerState: PlayerState): boolean {
    // Check card ownership
    if (input.cardPlayed) {
        const card = input.cardPlayed.cardId;
        if (!playerState.deck.includes(card)) return false;
        if (playerState.elixir < getCardCost(card)) return false;
        if (!isValidPosition(input.cardPlayed.position, playerState)) return false;
    }
    
    // Rate limiting
    if (playerState.inputsThisSecond > MAX_INPUTS_PER_SEC) return false;
    
    return true;
}
```

## 8. MONITORING & OBSERVABILITY

### 8.1 Metrics
- **Battle Metrics**: Duration, actions/min, desync rate
- **Server Metrics**: CPU, memory, connections, tick time
- **Business**: DAU, retention, purchase conversion
- **Errors**: Exception tracking, failed matches

### 8.2 Logging
```typescript
// Structured logging
logger.info('battle_started', {
    battleId,
    player1: p1.id,
    player2: p2.id,
    type: 'ladder',
    seed
});

logger.warn('desync_detected', {
    battleId,
    tick: serverTick,
    clientTick: input.clientTick,
    entityDiffs: diffCount
});
```

## 9. SCALABILITY CONSIDERATIONS

### 9.1 Horizontal Scaling
- **Stateless Battle Servers**: Can run multiple instances
- **Redis Pub/Sub**: For cross-server communication
- **Matchmaking**: Single service, can shard by battle type
- **Database**: Read replicas for queries, primary for writes

### 9.2 Battle Server Sharding
```typescript
// Each battle server handles N concurrent battles
const MAX_BATTLES_PER_SERVER = 100;

// Battle assignment
function assignBattleServer(): BattleServer {
    return battleServers
        .filter(s => s.currentBattles < MAX_BATTLES_PER_SERVER)
        .sort((a, b) => a.currentBattles - b.currentBattles)[0];
}
```

## 10. TESTING ARCHITECTURE

### 10.1 Unit Tests (Simulation)
```csharp
[Test]
public void TestKnightVsGoblinInteraction()
{
    var sim = new BattleSimulation(testSeed);
    var knight = sim.SpawnUnit(CardId.Knight, Player.P1, new Vector2(9, 8));
    var goblin = sim.SpawnUnit(CardId.Goblin, Player.P2, new Vector2(9, 10));
    
    sim.Step(2.0f); // 2 seconds
    
    Assert.IsTrue(knight.CurrentHP < knight.MaxHP);
    Assert.IsTrue(goblin.IsDead);
}
```

### 10.2 Integration Tests (Network)
```typescript
test('full 1v1 battle completes', async () => {
    const server = await startTestServer();
    const client1 = await connectClient(server, 'player1');
    const client2 = await connectClient(server, 'player2');
    
    await matchmake(client1, client2);
    
    // Play cards programmatically
    await playCard(client1, CardId.Knight, new Vector2(9, 8));
    await playCard(client2, CardId.Arrows, new Vector2(9, 8));
    
    // Wait for battle end
    const result = await waitForBattleEnd(client1, 30000);
    
    expect(result.winner).toBeDefined();
});
```

### 10.3 Replay Determinism Test
```csharp
[Test]
public void TestReplayDeterminism()
{
    var seed = 12345;
    
    // Run battle once
    var sim1 = new BattleSimulation(seed);
    sim1.Initialize(deck1, deck2);
    while (!sim1.IsEnded) sim1.Tick(FIXED_DT);
    var events1 = sim1.GetEventLog();
    
    // Run again with same seed
    var sim2 = new BattleSimulation(seed);
    sim2.Initialize(deck1, deck2);
    while (!sim2.IsEnded) sim2.Tick(FIXED_DT);
    var events2 = sim2.GetEventLog();
    
    // Events must match exactly
    Assert.AreEqual(events1.Count, events2.Count);
    for (int i = 0; i < events1.Count; i++)
    {
        Assert.AreEqual(events1[i], events2[i]);
    }
}
```

---

*This architecture provides a scalable, deterministic foundation for a Clash Royale clone. The key is the separation of simulation (authoritative, deterministic) from presentation (client-side, interpolated).*