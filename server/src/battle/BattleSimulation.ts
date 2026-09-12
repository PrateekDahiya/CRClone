import { logger } from '../utils/logger';
import { config } from '../config';
import { DeterministicRNG } from '../utils/rng';
import { BattleStatus, EntityType, TowerType, Vector2, CardType, CardRarity, PlayerState, EntityState } from '../types';

// Fixed-point math for determinism
export class Fixed {
  public static readonly FRACTIONAL_BITS = 10;
  public static readonly SCALE = 1 << Fixed.FRACTIONAL_BITS;
  public static readonly ONE = new Fixed(Fixed.SCALE);
  public static readonly ZERO = new Fixed(0);

  private _value: number;

  constructor(value: number = 0) {
    this._value = value;
  }

  static FromFloat(f: number): Fixed {
    return new Fixed(Math.round(f * Fixed.SCALE));
  }

  static FromInt(i: number): Fixed {
    return new Fixed(i * Fixed.SCALE);
  }

  toFloat(): number {
    return this._value / Fixed.SCALE;
  }

  toInt(): number {
    return Math.floor(this._value / Fixed.SCALE);
  }

  add(other: Fixed): Fixed {
    return new Fixed(this._value + other._value);
  }

  sub(other: Fixed): Fixed {
    return new Fixed(this._value - other._value);
  }

  mul(other: Fixed): Fixed {
    return new Fixed(Math.floor((this._value * other._value) / Fixed.SCALE));
  }

  div(other: Fixed): Fixed {
    if (other._value === 0) return Fixed.ZERO;
    return new Fixed(Math.floor((this._value * Fixed.SCALE) / other._value));
  }

  eq(other: Fixed): boolean {
    return this._value === other._value;
  }

  lt(other: Fixed): boolean {
    return this._value < other._value;
  }

  lte(other: Fixed): boolean {
    return this._value <= other._value;
  }

  gt(other: Fixed): boolean {
    return this._value > other._value;
  }

  gte(other: Fixed): boolean {
    return this._value >= other._value;
  }

  abs(): Fixed {
    return new Fixed(Math.abs(this._value));
  }

  sqrt(): Fixed {
    return Fixed.FromFloat(Math.sqrt(this.toFloat()));
  }
}

export class FixedVector2 {
  x: Fixed;
  y: Fixed;

  constructor(x: number | Fixed = 0, y: number | Fixed = 0) {
    this.x = x instanceof Fixed ? x : Fixed.FromFloat(x);
    this.y = y instanceof Fixed ? y : Fixed.FromFloat(y);
  }

  static FromFloat(x: number, y: number): FixedVector2 {
    return new FixedVector2(Fixed.FromFloat(x), Fixed.FromFloat(y));
  }

  static FromVector2(v: Vector2): FixedVector2 {
    return new FixedVector2(Fixed.FromFloat(v.x), Fixed.FromFloat(v.y));
  }

  toVector2(): Vector2 {
    return { x: this.x.toFloat(), y: this.y.toFloat() };
  }

  add(other: FixedVector2): FixedVector2 {
    return new FixedVector2(this.x.add(other.x), this.y.add(other.y));
  }

  sub(other: FixedVector2): FixedVector2 {
    return new FixedVector2(this.x.sub(other.x), this.y.sub(other.y));
  }

  mul(scalar: Fixed): FixedVector2 {
    return new FixedVector2(this.x.mul(scalar), this.y.mul(scalar));
  }

  distanceSq(other: FixedVector2): Fixed {
    const dx = this.x.sub(other.x);
    const dy = this.y.sub(other.y);
    return dx.mul(dx).add(dy.mul(dy));
  }

  distance(other: FixedVector2): Fixed {
    return this.distanceSq(other).sqrt();
  }

  normalized(): FixedVector2 {
    const dist = this.distance(FixedVector2.ZERO);
    if (dist.eq(Fixed.ZERO)) return FixedVector2.ZERO;
    return this.mul(Fixed.ONE.div(dist));
  }

  static readonly ZERO = new FixedVector2(Fixed.ZERO, Fixed.ZERO);
}

export interface GameConfig {
  startingElixir: number;
  maxElixir: number;
  elixirGenerationRate: number;
  doubleElixirRate: number;
  tripleElixirRate: number;
  battleDuration: number;
  overtimeDuration: number;
  handSize: number;
  kingTowerHP: number;
  princessTowerHP: number;
  towerDamage: number;
  towerHitSpeed: number;
  towerRange: number;
}

export interface CardData {
  cardId: number;
  name: string;
  rarity: CardRarity;
  type: CardType;
  elixirCost: number;
  hitpoints: number;
  damage: number;
  hitSpeed: number;
  range: number;
  speed: string;
  deployTime: number;
  targetType: string;
  count: number;
  mechanics: Record<string, any>;
  GetStats(level: number): UnitStats;
}

export interface UnitStats {
  level: number;
  hp: number;
  damage: number;
  hitSpeed: number;
  range: number;
  speed: number;
  collisionRadius: number;
  hitboxRadius: number;
}

export interface CardStats {
  level: number;
  hp: number;
  damage: number;
  hitSpeed: number;
  range: number;
  lifetime: number;
  spawnInterval: number;
  spawnCount: number;
}

// Game constants matching Unity
export const GameConstants = {
  P1_KING_POS: { x: 9, y: 2 },
  P1_PRINCESS_LEFT_POS: { x: 2, y: 6 },
  P1_PRINCESS_RIGHT_POS: { x: 16, y: 6 },
  P2_KING_POS: { x: 9, y: 30 },
  P2_PRINCESS_LEFT_POS: { x: 2, y: 26 },
  P2_PRINCESS_RIGHT_POS: { x: 16, y: 26 },
  DEPLOY_ZONE_Y_P1_MAX: 16,
  DEPLOY_ZONE_Y_P2_MIN: 16,
  RIVER_Y_MIN: 14,
  RIVER_Y_MAX: 18,
  ARENA_WIDTH: 18,
  ARENA_HEIGHT: 32,
};

export enum EntityTypeInternal {
  Unit = 0,
  Building = 1,
  Projectile = 2,
  SpellEffect = 3,
  Tower = 4,
}

export enum UnitState {
  Idle = 0,
  Moving = 1,
  Attacking = 2,
  Stunned = 3,
  Dead = 4,
}

export enum BuildingState {
  Idle = 0,
  Spawning = 1,
  Retracting = 2,
  Retracted = 3,
  Dead = 4,
}

export enum SpellType {
  Damage = 0,
  Buff = 1,
  Debuff = 2,
  Freeze = 3,
  Poison = 4,
  Heal = 5,
  Push = 6,
  Clone = 7,
  Mirror = 8,
  Rage = 9,
}

export enum DeathCause {
  Damage = 0,
  LifetimeExpired = 1,
  Sacrificed = 2,
  Spell = 3,
}

export interface PlayerInput {
  type: 'play_card' | 'cast_spell' | 'champion_ability' | 'emote';
  cardId?: number;
  spellId?: number;
  position: Vector2;
  targetPosition?: Vector2;
  clientTick: number;
}

export interface BattleEvent {
  tick: number;
  type: EventType;
  playerId: number;
  cardId: number;
  entityId: number;
  position: FixedVector2;
  damage: number;
  hpRemaining: number;
  elixir: number;
}

export enum EventType {
  BattleStart,
  CardPlayed,
  UnitSpawned,
  UnitDied,
  BuildingPlaced,
  BuildingDestroyed,
  SpellCast,
  TowerDamaged,
  TowerDestroyed,
  KingActivated,
  ChampionAbility,
  BattleEnd,
  ElixirChanged,
  EntityStateSync,
}

export interface ReplayEvent {
  tick: number;
  type: ReplayEventType;
  playerId: number;
  cardId: number;
  entityId: number;
  position: FixedVector2;
  damage: number;
  hpRemaining: number;
  elixir: number;
}

export enum ReplayEventType {
  BattleStart,
  CardPlayed,
  UnitSpawned,
  UnitDied,
  BuildingPlaced,
  BuildingDestroyed,
  SpellCast,
  TowerDamaged,
  TowerDestroyed,
  KingActivated,
  ChampionAbility,
  BattleEnd,
  ElixirChanged,
  EntityStateSync,
}

export class BattleSimulation {
  public static readonly TICK_RATE = 60;
  public static readonly FIXED_DT = 1 / BattleSimulation.TICK_RATE;
  public static readonly MAX_ENTITIES = 500;

  private _config!: GameConfig;
  private _seed!: bigint;
  private _rng!: DeterministicRNG;
  private _currentTick: number = 0;
  private _serverTick: number = 0;
  private _status: BattleStatus = BattleStatus.Waiting;

  private _player1!: PlayerStateInternal;
  private _player2!: PlayerStateInternal;

  private _entities: Map<number, Entity> = new Map();
  private _units: Unit[] = [];
  private _buildings: Building[] = [];
  private _projectiles: Projectile[] = [];
  private _activeSpells: SpellEffect[] = [];
  private _towers: Tower[] = [];

  private _nextEntityId: number = 1000;
  private _p1Inputs: PlayerInput[] = [];
  private _p2Inputs: PlayerInput[] = [];
  private _eventLog: BattleEvent[] = [];
  private _replayLog: ReplayEvent[] = [];
  private _pathfinding!: Pathfinding;
  private _collisionGridDirty: boolean = true;

  public get Status(): BattleStatus { return this._status; }
  public get CurrentTick(): number { return this._currentTick; }
  public get Player1(): PlayerStateInternal { return this._player1; }
  public get Player2(): PlayerStateInternal { return this._player2; }
  public get Units(): ReadonlyArray<Unit> { return this._units; }
  public get Buildings(): ReadonlyArray<Building> { return this._buildings; }
  public get Projectiles(): ReadonlyArray<Projectile> { return this._projectiles; }
  public get ActiveSpells(): ReadonlyArray<SpellEffect> { return this._activeSpells; }
  public get Towers(): ReadonlyArray<Tower> { return this._towers; }
  public get EventLog(): ReadonlyArray<BattleEvent> { return this._eventLog; }
  public get ReplayLog(): ReadonlyArray<ReplayEvent> { return this._replayLog; }

  public Initialize(gameConfig: GameConfig, seed: bigint, p1Deck: number[], p2Deck: number[], cardDatabase: Map<number, CardData>): void {
    this._config = gameConfig;
    this._seed = seed;
    this._rng = new DeterministicRNG(seed);
    this._currentTick = 0;
    this._serverTick = 0;
    this._status = BattleStatus.Playing;

    this._player1 = new PlayerStateInternal(1, p1Deck, gameConfig);
    this._player2 = new PlayerStateInternal(2, p2Deck, gameConfig);

    this._pathfinding = new Pathfinding();
    this._pathfinding.Initialize();

    this.CreateTowers(cardDatabase);

    this._player1.Elixir = gameConfig.startingElixir;
    this._player2.Elixir = gameConfig.startingElixir;

    this.LogEvent({ tick: 0, type: EventType.BattleStart, playerId: 0, cardId: 0, entityId: 0, position: FixedVector2.ZERO, damage: 0, hpRemaining: 0, elixir: 0 });
    this.LogReplayEvent({ tick: 0, type: ReplayEventType.BattleStart, playerId: 0, cardId: 0, entityId: 0, position: FixedVector2.ZERO, damage: 0, hpRemaining: 0, elixir: 0 });
  }

  private CreateTowers(cardDatabase: Map<number, CardData>): void {
    const kingCard = this.GetTowerCardData(cardDatabase, TowerType.King);
    const princessCard = this.GetTowerCardData(cardDatabase, TowerType.PrincessLeft);

    const p1King = new Tower(this._nextEntityId++, 1, EntityTypeInternal.Tower, TowerType.King, FixedVector2.FromVector2(GameConstants.P1_KING_POS), this._config.kingTowerHP, this._config.towerDamage, this._config.towerHitSpeed, this._config.towerRange, kingCard);
    const p1PrincessL = new Tower(this._nextEntityId++, 1, EntityTypeInternal.Tower, TowerType.PrincessLeft, FixedVector2.FromVector2(GameConstants.P1_PRINCESS_LEFT_POS), this._config.princessTowerHP, this._config.towerDamage, this._config.towerHitSpeed, this._config.towerRange, princessCard);
    const p1PrincessR = new Tower(this._nextEntityId++, 1, EntityTypeInternal.Tower, TowerType.PrincessRight, FixedVector2.FromVector2(GameConstants.P1_PRINCESS_RIGHT_POS), this._config.princessTowerHP, this._config.towerDamage, this._config.towerHitSpeed, this._config.towerRange, princessCard);

    const p2King = new Tower(this._nextEntityId++, 2, EntityTypeInternal.Tower, TowerType.King, FixedVector2.FromVector2(GameConstants.P2_KING_POS), this._config.kingTowerHP, this._config.towerDamage, this._config.towerHitSpeed, this._config.towerRange, kingCard);
    const p2PrincessL = new Tower(this._nextEntityId++, 2, EntityTypeInternal.Tower, TowerType.PrincessLeft, FixedVector2.FromVector2(GameConstants.P2_PRINCESS_LEFT_POS), this._config.princessTowerHP, this._config.towerDamage, this._config.towerHitSpeed, this._config.towerRange, princessCard);
    const p2PrincessR = new Tower(this._nextEntityId++, 2, EntityTypeInternal.Tower, TowerType.PrincessRight, FixedVector2.FromVector2(GameConstants.P2_PRINCESS_RIGHT_POS), this._config.princessTowerHP, this._config.towerDamage, this._config.towerHitSpeed, this._config.towerRange, princessCard);

    this._towers.push(p1King, p1PrincessL, p1PrincessR, p2King, p2PrincessL, p2PrincessR);
    this._towers.forEach(t => this._entities.set(t.Id, t));
  }

  private GetTowerCardData(cardDatabase: Map<number, CardData>, towerType: TowerType): CardData | undefined {
    for (const card of cardDatabase.values()) {
      if (card.type === CardType.Building && card.mechanics?.towerType === towerType) {
        return card;
      }
    }
    return undefined;
  }

  public Tick(dt: number = BattleSimulation.FIXED_DT): void {
    if (this._status !== BattleStatus.Playing) return;

    this._currentTick++;

    this.ProcessInputs();
    this.UpdateElixir(dt);
    this.UpdateSpells(dt);
    this.UpdateProjectiles(dt);
    this.UpdateUnits(dt);
    this.UpdateBuildings(dt);

    if (this._collisionGridDirty) {
      this._pathfinding.UpdateBuildingCollision(this._buildings);
      this._collisionGridDirty = false;
    }

    this.UpdateTowers(dt);
    this.ResolveCollisions();
    this.ProcessDeaths();
    this.CheckWinCondition();
    this.RecordTickEvents();
  }

  private ProcessInputs(): void {
    while (this._p1Inputs.length > 0) {
      const input = this._p1Inputs.shift()!;
      this.ApplyInput(1, input);
    }

    while (this._p2Inputs.length > 0) {
      const input = this._p2Inputs.shift()!;
      this.ApplyInput(2, input);
    }
  }

  public QueueInput(playerId: number, input: PlayerInput): void {
    if (playerId === 1) this._p1Inputs.push(input);
    else if (playerId === 2) this._p2Inputs.push(input);
  }

  private ApplyInput(playerId: number, input: PlayerInput): void {
    const player = playerId === 1 ? this._player1 : this._player2;
    const opponent = playerId === 1 ? this._player2 : this._player1;

    switch (input.type) {
      case 'play_card':
        this.PlayCard(player, opponent, input.cardId!, input.position);
        break;
      case 'cast_spell':
        this.CastSpell(player, opponent, input.spellId!, input.position);
        break;
      case 'champion_ability':
        this.UseChampionAbility(player, opponent, input.position);
        break;
    }
  }

  private PlayCard(player: PlayerStateInternal, opponent: PlayerStateInternal, cardId: number, position: Vector2): void {
    // Card validation would use card database
    // Simplified for server - actual validation in BattleServer
    const fixedPos = FixedVector2.FromVector2(position);
    
    // Deduct elixir (cost lookup from card database)
    // player.Elixir -= cost;
    
    // Draw next card
    player.DrawCard();

    this.LogEvent({ 
      tick: this._currentTick, 
      type: EventType.CardPlayed, 
      playerId: player.PlayerId, 
      cardId, 
      entityId: 0, 
      position: fixedPos, 
      damage: 0, 
      hpRemaining: 0, 
      elixir: player.Elixir 
    });

    this.LogReplayEvent({ 
      tick: this._currentTick, 
      type: ReplayEventType.CardPlayed, 
      playerId: player.PlayerId, 
      cardId, 
      entityId: 0, 
      position: fixedPos, 
      damage: 0, 
      hpRemaining: 0, 
      elixir: player.Elixir 
    });
  }

  private CastSpell(player: PlayerStateInternal, opponent: PlayerStateInternal, spellId: number, position: Vector2): void {
    const fixedPos = FixedVector2.FromVector2(position);
    
    // Create spell effect
    // const spellEffect = SpellEffect.CreateFromCard(...)
    
    this.LogEvent({ 
      tick: this._currentTick, 
      type: EventType.SpellCast, 
      playerId: player.PlayerId, 
      cardId: spellId, 
      entityId: 0, 
      position: fixedPos, 
      damage: 0, 
      hpRemaining: 0, 
      elixir: 0 
    });

    this.LogReplayEvent({ 
      tick: this._currentTick, 
      type: ReplayEventType.SpellCast, 
      playerId: player.PlayerId, 
      cardId: spellId, 
      entityId: 0, 
      position: fixedPos, 
      damage: 0, 
      hpRemaining: 0, 
      elixir: 0 
    });
  }

  private UseChampionAbility(player: PlayerStateInternal, opponent: PlayerStateInternal, position: Vector2): void {
    const fixedPos = FixedVector2.FromVector2(position);
    
    this.LogEvent({ 
      tick: this._currentTick, 
      type: EventType.ChampionAbility, 
      playerId: player.PlayerId, 
      cardId: 0, 
      entityId: 0, 
      position: fixedPos, 
      damage: 0, 
      hpRemaining: 0, 
      elixir: 0 
    });

    this.LogReplayEvent({ 
      tick: this._currentTick, 
      type: ReplayEventType.ChampionAbility, 
      playerId: player.PlayerId, 
      cardId: 0, 
      entityId: 0, 
      position: fixedPos, 
      damage: 0, 
      hpRemaining: 0, 
      elixir: 0 
    });
  }

  private UpdateElixir(dt: number): void {
    const rate = this.GetElixirRate();
    const elixirPerTick = (BattleSimulation.FIXED_DT / rate.toFloat());
    
    this._player1.Elixir = Math.min(this._config.maxElixir, this._player1.Elixir + elixirPerTick);
    this._player2.Elixir = Math.min(this._config.maxElixir, this._player2.Elixir + elixirPerTick);
  }

  private GetElixirRate(): Fixed {
    const elapsed = this._currentTick * BattleSimulation.FIXED_DT;
    if (elapsed >= this._config.battleDuration + this._config.overtimeDuration) return Fixed.FromFloat(this._config.tripleElixirRate);
    if (elapsed >= this._config.battleDuration) return Fixed.FromFloat(this._config.doubleElixirRate);
    return Fixed.FromFloat(this._config.elixirGenerationRate);
  }

  private UpdateSpells(dt: number): void {
    for (let i = this._activeSpells.length - 1; i >= 0; i--) {
      const spell = this._activeSpells[i];
      spell.Tick(dt, this);
      if (spell.IsFinished) {
        this._activeSpells.splice(i, 1);
        this._entities.delete(spell.Id);
      }
    }
  }

  private UpdateProjectiles(dt: number): void {
    for (let i = this._projectiles.length - 1; i >= 0; i--) {
      const proj = this._projectiles[i];
      proj.Tick(dt, this);
      if (proj.IsDead) {
        this._projectiles.splice(i, 1);
        this._entities.delete(proj.Id);
      }
    }
  }

  private UpdateUnits(dt: number): void {
    for (const unit of this._units) {
      if (unit.IsDead) continue;
      unit.Tick(dt, this);
    }
  }

  private UpdateBuildings(dt: number): void {
    for (let i = this._buildings.length - 1; i >= 0; i--) {
      const building = this._buildings[i];
      if (building.IsDead) continue;
      building.Tick(dt, this);
      
      if (building.IsExpired) {
        building.Die(DeathCause.LifetimeExpired);
      }
    }
  }

  private UpdateTowers(dt: number): void {
    for (const tower of this._towers) {
      if (tower.IsDead) continue;
      tower.Tick(dt, this);
    }
  }

  private ResolveCollisions(): void {
    for (let i = 0; i < this._units.length; i++) {
      const a = this._units[i];
      if (a.IsDead) continue;

      for (let j = i + 1; j < this._units.length; j++) {
        const b = this._units[j];
        if (b.IsDead) continue;

        const dist = a.Position.distance(b.Position);
        const minDist = Fixed.FromFloat(a.CollisionRadius + b.CollisionRadius);

        if (dist.lt(minDist) && dist.gt(Fixed.FromFloat(0.01))) {
          const pushDir = a.Position.sub(b.Position).normalized();
          const overlap = minDist.sub(dist).mul(Fixed.FromFloat(0.5));
          a.Position = a.Position.add(pushDir.mul(overlap));
          b.Position = b.Position.sub(pushDir.mul(overlap));
        }
      }
    }
  }

  private ProcessDeaths(): void {
    for (let i = this._units.length - 1; i >= 0; i--) {
      if (this._units[i].IsDead) {
        const unit = this._units[i];
        unit.OnDeath(this);
        this._entities.delete(unit.Id);
        this._units.splice(i, 1);

        this.LogReplayEvent({
          tick: this._currentTick,
          type: ReplayEventType.UnitDied,
          playerId: unit.OwnerPlayerId,
          cardId: unit.CardData.cardId,
          entityId: unit.Id,
          position: unit.Position,
          damage: 0,
          hpRemaining: 0,
          elixir: 0,
        });
      }
    }

    for (let i = this._buildings.length - 1; i >= 0; i--) {
      if (this._buildings[i].IsDead) {
        const building = this._buildings[i];
        building.OnDeath(this);
        this._entities.delete(building.Id);
        this._buildings.splice(i, 1);
        this._collisionGridDirty = true;

        this.LogReplayEvent({
          tick: this._currentTick,
          type: ReplayEventType.BuildingDestroyed,
          playerId: building.OwnerPlayerId,
          cardId: building.CardData.cardId,
          entityId: building.Id,
          position: building.Position,
          damage: 0,
          hpRemaining: 0,
          elixir: 0,
        });
      }
    }
  }

  private CheckWinCondition(): void {
    let p1KingDead = false, p2KingDead = false;
    let p1Crowns = 0, p2Crowns = 0;

    for (const tower of this._towers) {
      if (tower.OwnerPlayerId === 1) {
        if (tower.TowerType === TowerType.King && tower.IsDead) p1KingDead = true;
        if (tower.TowerType !== TowerType.King && tower.IsDead) p1Crowns++;
      } else {
        if (tower.TowerType === TowerType.King && tower.IsDead) p2KingDead = true;
        if (tower.TowerType !== TowerType.King && tower.IsDead) p2Crowns++;
      }
    }

    let newStatus = this._status;

    if (p1KingDead || p2KingDead) {
      newStatus = p1KingDead ? BattleStatus.Player2Won : BattleStatus.Player1Won;
      p1Crowns = p1KingDead ? 0 : 3;
      p2Crowns = p2KingDead ? 0 : 3;
    } else if (this._currentTick >= (this._config.battleDuration + this._config.overtimeDuration) * BattleSimulation.TICK_RATE) {
      newStatus = BattleStatus.Draw;
    }

    if (newStatus !== this._status && newStatus !== BattleStatus.Playing) {
      this._status = newStatus;
      this.LogReplayEvent({
        tick: this._currentTick,
        type: ReplayEventType.BattleEnd,
        playerId: this._status === BattleStatus.Player1Won ? 1 : 
                 this._status === BattleStatus.Player2Won ? 2 : 0,
        cardId: 0,
        entityId: 0,
        position: FixedVector2.ZERO,
        damage: 0,
        hpRemaining: 0,
        elixir: 0,
      });
    }
  }

  private RecordTickEvents(): void {
    this.LogReplayEvent({
      tick: this._currentTick,
      type: ReplayEventType.ElixirChanged,
      playerId: 1,
      cardId: 0,
      entityId: 0,
      position: FixedVector2.ZERO,
      damage: 0,
      hpRemaining: 0,
      elixir: this._player1.Elixir,
    });
    this.LogReplayEvent({
      tick: this._currentTick,
      type: ReplayEventType.ElixirChanged,
      playerId: 2,
      cardId: 0,
      entityId: 0,
      position: FixedVector2.ZERO,
      damage: 0,
      hpRemaining: 0,
      elixir: this._player2.Elixir,
    });
  }

  private LogEvent(evt: BattleEvent): void {
    this._eventLog.push(evt);
  }

  private LogReplayEvent(evt: ReplayEvent): void {
    this._replayLog.push(evt);
  }

  public GetPotentialTargets(attacker: Entity): Entity[] {
    const targets: Entity[] = [];
    const enemyPlayerId = attacker.OwnerPlayerId === 1 ? 2 : 1;

    for (const u of this._units) {
      if (u.OwnerPlayerId === enemyPlayerId && !u.IsDead) targets.push(u);
    }
    for (const b of this._buildings) {
      if (b.OwnerPlayerId === enemyPlayerId && !b.IsDead) targets.push(b);
    }
    for (const t of this._towers) {
      if (t.OwnerPlayerId === enemyPlayerId && !t.IsDead) targets.push(t);
    }

    return targets;
  }

  public GetAllEntitiesInRadius(center: FixedVector2, radius: Fixed): Entity[] {
    const entities: Entity[] = [];
    const radiusSq = radius.mul(radius);

    for (const entity of this._entities.values()) {
      if (entity.IsDead) continue;
      if (entity.Position.distanceSq(center).lte(radiusSq)) {
        entities.push(entity);
      }
    }

    return entities;
  }

  public GetEntity(id: number): Entity | undefined {
    return this._entities.get(id);
  }

  public AddProjectile(projectile: Projectile): void {
    this._projectiles.push(projectile);
    this._entities.set(projectile.Id, projectile);
  }

  public Dispose(): void {
    this._entities.clear();
    this._units = [];
    this._buildings = [];
    this._projectiles = [];
    this._activeSpells = [];
    this._towers = [];
    this._eventLog = [];
    this._replayLog = [];
  }
}

export class PlayerStateInternal {
  public readonly PlayerId: number;
  public Elixir: number = 0;
  public readonly Deck: number[];
  public Hand: number[] = [];
  public NextCardIndex: number = 0;
  public KingTowerActivated: boolean = false;

  private readonly _config: GameConfig;

  constructor(playerId: number, deck: number[], config: GameConfig) {
    this.PlayerId = playerId;
    this.Deck = deck;
    this._config = config;
    this.Hand = new Array(config.handSize);
    this.NextCardIndex = config.handSize;
    this.DrawInitialHand();
  }

  private DrawInitialHand(): void {
    for (let i = 0; i < this._config.handSize; i++) {
      this.Hand[i] = this.Deck[i];
    }
    this.NextCardIndex = this._config.handSize;
  }

  public DrawCard(): void {
    if (this.NextCardIndex >= this.Deck.length) this.NextCardIndex = 0;
    
    for (let i = 0; i < this._config.handSize - 1; i++) {
      this.Hand[i] = this.Hand[i + 1];
    }
    this.Hand[this._config.handSize - 1] = this.Deck[this.NextCardIndex];
    this.NextCardIndex++;
  }
}

export abstract class Entity {
  public readonly Id: number;
  public readonly OwnerPlayerId: number;
  public Position: FixedVector2;
  public Velocity: FixedVector2 = FixedVector2.ZERO;
  public CurrentHP: number;
  public readonly MaxHP: number;
  public readonly Type: EntityTypeInternal;
  public TargetId: number = 0;
  private _isDead: boolean = false;
  public CollisionRadius: number = 0.5;

  constructor(id: number, owner: number, type: EntityTypeInternal, position: FixedVector2, maxHP: number) {
    this.Id = id;
    this.OwnerPlayerId = owner;
    this.Position = position;
    this.MaxHP = maxHP;
    this.CurrentHP = maxHP;
    this.Type = type;
  }

  public get IsDead(): boolean {
    return this._isDead;
  }

  public set IsDead(value: boolean) {
    this._isDead = value;
  }

  abstract Tick(dt: number, sim: BattleSimulation): void;

  public TakeDamage(amount: number, sourceId: number): void {
    this.CurrentHP = Math.max(0, this.CurrentHP - amount);
    if (this.CurrentHP <= 0) {
      this.IsDead = true;
    }
  }

  public Die(cause: DeathCause): void {
    this.IsDead = true;
    this.CurrentHP = 0;
  }
}

export class Unit extends Entity {
  public readonly CardData: CardData;
  public readonly Stats: UnitStats;
  public readonly Level: number;
  public State: UnitState = UnitState.Idle;
  public Target: Entity | null = null;
  public AttackCooldown: number = 0;
  public MoveSpeed: number;

  constructor(id: number, owner: number, cardData: CardData, stats: UnitStats, position: FixedVector2, level: number) {
    super(id, owner, EntityTypeInternal.Unit, position, stats.hp);
    this.CardData = cardData;
    this.Stats = stats;
    this.Level = level;
    this.MoveSpeed = stats.speed;
    this.CollisionRadius = stats.collisionRadius;
  }

  public Tick(dt: number, sim: BattleSimulation): void {
    if (this.IsDead) return;

    // Update attack cooldown
    if (this.AttackCooldown > 0) {
      this.AttackCooldown -= dt;
    }

    // Check if target is dead
    if (this.Target && this.Target.IsDead) {
      this.Target = null;
    }

    // Try to attack
    if (this.Target && this.InAttackRange(this.Target)) {
      this.TryAttack(sim);
    } else {
      this.UpdateMovement(dt, sim);
    }
  }

  private InAttackRange(target: Entity): boolean {
    const dist = this.Position.distance(target.Position);
    return dist.lte(Fixed.FromFloat(this.Stats.range + target.CollisionRadius));
  }

  private TryAttack(sim: BattleSimulation): void {
    if (this.AttackCooldown <= 0) {
      this.AttackCooldown = this.Stats.hitSpeed;
      this.PerformAttack(sim);
    }
  }

  private PerformAttack(sim: BattleSimulation): void {
    if (!this.Target) return;
    
    // Create projectile or direct damage
    if (this.Stats.range > 1.5) {
      // Ranged attack - create projectile
      const projectile = new Projectile(
        sim['_nextEntityId']++, 
        this.OwnerPlayerId, 
        this.Id, 
        this.Target.Id, 
        this.Position, 
        this.Target.Position, 
        this.Stats.damage, 
        this.Stats.hitSpeed,
        false
      );
      sim.AddProjectile(projectile);
    } else {
      // Melee attack - direct damage
      this.Target.TakeDamage(this.Stats.damage, this.Id);
    }
  }

  private UpdateMovement(dt: number, sim: BattleSimulation): void {
    if (!this.Target) {
      this.Target = this.AcquireTarget(sim.GetPotentialTargets(this));
      if (!this.Target) return;
    }

    const direction = this.Target.Position.sub(this.Position).normalized();
    const moveDist = Fixed.FromFloat(this.MoveSpeed * dt);
    this.Position = this.Position.add(direction.mul(moveDist));
  }

  public AcquireTarget(candidates: Entity[]): Entity | null {
    let bestTarget: Entity | null = null;
    let bestScore = Number.MAX_VALUE;

    for (const candidate of candidates) {
      if (!this.IsValidTarget(candidate)) continue;

      const dist = this.Position.distance(candidate.Position);
      const pathDist = this.GetPathDistance(candidate);

      const score = pathDist.toFloat() * 1000 + this.GetTargetPriority(candidate);
      
      if (score < bestScore) {
        bestScore = score;
        bestTarget = candidate;
      }
    }

    if (bestTarget) {
      this.Target = bestTarget;
      this.TargetId = bestTarget.Id;
    }
    return bestTarget;
  }

  private IsValidTarget(candidate: Entity): boolean {
    // Buildings target buildings
    if (this.CardData.targetType === 'building') {
      return candidate.Type === EntityTypeInternal.Building || candidate.Type === EntityTypeInternal.Tower;
    }
    return true;
  }

  private GetPathDistance(target: Entity): Fixed {
    // Simplified - use direct distance
    return this.Position.distance(target.Position);
  }

  private GetTargetPriority(target: Entity): number {
    if (target.Type === EntityTypeInternal.Unit) return 0;
    if (target.Type === EntityTypeInternal.Building) return 1;
    if (target.Type === EntityTypeInternal.Tower) return 2;
    return 100;
  }

  public OnDeath(sim: BattleSimulation): void {
    // Death effects
  }
}

export class Building extends Entity {
  public readonly CardData: CardData;
  public readonly Stats: CardStats;
  public readonly Level: number;
  public State: BuildingState = BuildingState.Idle;
  public Lifetime: number = 0;
  public SpawnTimer: number = 0;
  public IsRetracted: boolean = false;
  public IsExpired: boolean = false;

  constructor(id: number, owner: number, cardData: CardData, stats: CardStats, position: FixedVector2, level: number) {
    super(id, owner, EntityTypeInternal.Building, position, stats.hp);
    this.CardData = cardData;
    this.Stats = stats;
    this.Level = level;
    this.Lifetime = stats.lifetime;
    this.CollisionRadius = 1;
  }

  public Tick(dt: number, sim: BattleSimulation): void {
    if (this.IsDead) return;

    this.Lifetime -= dt;
    if (this.Lifetime <= 0) {
      this.IsExpired = true;
      return;
    }

    if (this.Stats.spawnInterval > 0) {
      this.SpawnTimer -= dt;
      if (this.SpawnTimer <= 0) {
        this.SpawnUnits(sim);
        this.SpawnTimer = this.Stats.spawnInterval;
      }
    }
  }

  private SpawnUnits(sim: BattleSimulation): void {
    // Spawn logic for buildings like Goblin Hut, Barbarian Hut, etc.
  }

  public Die(cause: DeathCause): void {
    super.Die(cause);
    this.IsExpired = true;
  }

  public OnDeath(sim: BattleSimulation): void {
    // Death effects for buildings
  }
}

export class Projectile extends Entity {
  public readonly SourceId: number;
  public readonly TargetId: number;
  public readonly Damage: number;
  public readonly Speed: number;
  public readonly IsBeam: boolean;
  public Hit: boolean = false;

  constructor(id: number, owner: number, sourceId: number, targetId: number, 
              startPos: FixedVector2, targetPos: FixedVector2, damage: number, speed: number, isBeam: boolean) {
    super(id, owner, EntityTypeInternal.Projectile, startPos, 1);
    this.SourceId = sourceId;
    this.TargetId = targetId;
    this.Damage = damage;
    this.Speed = speed;
    this.IsBeam = isBeam;
  }

  public Tick(dt: number, sim: BattleSimulation): void {
    if (this.Hit) return;

    const target = sim.GetEntity(this.TargetId);
    if (!target || target.IsDead) {
      this.Hit = true;
      return;
    }

    const direction = target.Position.sub(this.Position).normalized();
    const moveDist = Fixed.FromFloat(this.Speed * dt);
    this.Position = this.Position.add(direction.mul(moveDist));

    const dist = this.Position.distance(target.Position);
    if (dist.lte(Fixed.FromFloat(target.CollisionRadius + 0.5))) {
      target.TakeDamage(this.Damage, this.SourceId);
      this.Hit = true;
    }
  }
}

export class SpellEffect extends Entity {
  public readonly SpellType: SpellType;
  public readonly Radius: Fixed;
  public readonly DamagePerSecond: number;
  public readonly Duration: number;
  public RemainingTime: number;
  public IsFinished: boolean = false;

  constructor(id: number, owner: number, spellType: SpellType, position: FixedVector2, radius: Fixed, damagePerSecond: number, duration: number) {
    super(id, owner, EntityTypeInternal.SpellEffect, position, 1);
    this.SpellType = spellType;
    this.Radius = radius;
    this.DamagePerSecond = damagePerSecond;
    this.Duration = duration;
    this.RemainingTime = duration;
  }

  public static CreateFromCard(id: number, owner: number, cardData: CardData, position: Vector2, level: number): SpellEffect | null {
    // Factory method - would parse cardData.mechanics
    return null;
  }

  public Tick(dt: number, sim: BattleSimulation): void {
    this.RemainingTime -= dt;
    if (this.RemainingTime <= 0) {
      this.IsFinished = true;
      return;
    }

    // Apply spell effects based on type
    const entities = sim.GetAllEntitiesInRadius(this.Position, this.Radius);
    for (const entity of entities) {
      if (entity.OwnerPlayerId !== this.OwnerPlayerId && !entity.IsDead) {
        this.ApplyEffect(entity, dt, sim);
      }
    }
  }

  private ApplyEffect(entity: Entity, dt: number, sim: BattleSimulation): void {
    switch (this.SpellType) {
      case SpellType.Damage:
        entity.TakeDamage(Math.round(this.DamagePerSecond * dt), this.Id);
        break;
      case SpellType.Freeze:
        // Apply freeze effect
        break;
      case SpellType.Poison:
        entity.TakeDamage(Math.round(this.DamagePerSecond * dt), this.Id);
        break;
    }
  }
}

export class Tower extends Entity {
  public readonly TowerType: TowerType;
  public readonly Damage: number;
  public readonly HitSpeed: number;
  public readonly Range: number;
  public AttackCooldown: number = 0;

  constructor(id: number, owner: number, entityType: EntityTypeInternal, type: TowerType, position: FixedVector2, hp: number, damage: number, hitSpeed: number, range: number, cardData?: CardData) {
    super(id, owner, entityType, position, hp);
    this.TowerType = type;
    this.Damage = damage;
    this.HitSpeed = hitSpeed;
    this.Range = range;
    this.CollisionRadius = 1.5;
  }

  public Tick(dt: number, sim: BattleSimulation): void {
    if (this.IsDead) return;

    if (this.AttackCooldown > 0) {
      this.AttackCooldown -= dt;
    }

    if (this.AttackCooldown <= 0) {
      const target = this.AcquireTarget(sim.GetPotentialTargets(this));
      if (target) {
        this.Attack(target, sim);
        this.AttackCooldown = this.HitSpeed;
      }
    }
  }

  private AcquireTarget(candidates: Entity[]): Entity | null {
    let bestTarget: Entity | null = null;
    let bestDist = Number.MAX_VALUE;

    for (const candidate of candidates) {
      const dist = this.Position.distance(candidate.Position).toFloat();
      if (dist <= this.Range && dist < bestDist) {
        bestDist = dist;
        bestTarget = candidate;
      }
    }

    return bestTarget;
  }

  private Attack(target: Entity, sim: BattleSimulation): void {
    // Create projectile for tower attack
    const projectile = new Projectile(
      sim['_nextEntityId']++, 
      this.OwnerPlayerId, 
      this.Id, 
      target.Id, 
      this.Position, 
      target.Position, 
      this.Damage, 
      800, // projectile speed
      false
    );
    sim.AddProjectile(projectile);
  }
}

export class Pathfinding {
  private _grid: number[][] = [];
  private _width: number = 36;
  private _height: number = 64;
  private _tileSize: number = 0.5;

  Initialize(): void {
    this._grid = Array(this._height).fill(null).map(() => Array(this._width).fill(0));
    this.BuildBaseGrid();
  }

  private BuildBaseGrid(): void {
    // River is unwalkable for ground units (y = 14-18 in tile coords: 28-36)
    for (let y = 28; y <= 36; y++) {
      for (let x = 0; x < this._width; x++) {
        this._grid[y][x] = 1; // Unwalkable
      }
    }
  }

  UpdateBuildingCollision(buildings: Building[]): void {
    // Reset river
    for (let y = 28; y <= 36; y++) {
      for (let x = 0; x < this._width; x++) {
        this._grid[y][x] = 1;
      }
    }

    // Add building collisions
    for (const building of buildings) {
      if (building.IsDead) continue;
      const gridPos = this.WorldToGrid(building.Position.toVector2());
      const radius = Math.ceil(building.CollisionRadius / this._tileSize);
      
      for (let dy = -radius; dy <= radius; dy++) {
        for (let dx = -radius; dx <= radius; dx++) {
          const x = gridPos.x + dx;
          const y = gridPos.y + dy;
          if (x >= 0 && x < this._width && y >= 0 && y < this._height) {
            this._grid[y][x] = 1;
          }
        }
      }
    }
  }

  FindPath(start: Vector2, end: Vector2, isFlying: boolean): Vector2[] {
    if (isFlying) {
      return [start, end];
    }

    const startNode = this.WorldToGrid(start);
    const endNode = this.WorldToGrid(end);

    if (!this.IsWalkable(endNode.x, endNode.y)) {
      const nearest = this.FindNearestWalkable(endNode.x, endNode.y);
      if (nearest) {
        return this.AStar(startNode, nearest);
      }
      return [];
    }

    return this.AStar(startNode, endNode);
  }

  private AStar(start: { x: number; y: number }, end: { x: number; y: number }): Vector2[] {
    // Simplified A* - returns direct path for now
    return [start, end].map(n => this.GridToWorld(n));
  }

  private WorldToGrid(pos: Vector2): { x: number; y: number } {
    return {
      x: Math.floor(pos.x / this._tileSize),
      y: Math.floor(pos.y / this._tileSize),
    };
  }

  private GridToWorld(grid: { x: number; y: number }): Vector2 {
    return {
      x: (grid.x + 0.5) * this._tileSize,
      y: (grid.y + 0.5) * this._tileSize,
    };
  }

  private IsWalkable(x: number, y: number): boolean {
    return x >= 0 && x < this._width && y >= 0 && y < this._height && this._grid[y][x] === 0;
  }

  private FindNearestWalkable(x: number, y: number): { x: number; y: number } | null {
    for (let radius = 1; radius < 10; radius++) {
      for (let dx = -radius; dx <= radius; dx++) {
        for (let dy = -radius; dy <= radius; dy++) {
          const nx = x + dx;
          const ny = y + dy;
          if (this.IsWalkable(nx, ny)) {
            return { x: nx, y: ny };
          }
        }
      }
    }
    return null;
  }
}