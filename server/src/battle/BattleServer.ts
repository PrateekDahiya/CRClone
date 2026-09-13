import { logger } from '../utils/logger';
import { config } from '../config';
import { DeterministicRNG } from '../utils/rng';
import { BattleType, BattleStatus, PlayerInput, PlayerBattleInfo, EntityState, PlayerState, EntityType, TowerType, CardType } from '../types';
import { PlayerService } from '../services/PlayerService';
import { BattleSimulation, buildDeploySnapshot, isValidDeployPosition } from './BattleSimulation';
import { EntityManager } from './EntityManager';
import { ReplayRecorder, PlayerInputRecord } from './ReplayRecorder';
import { RatingSystem, ratingSystem } from '../matchmaking/RatingSystem';
import { getCardElixirCost, getCardDefinition, isFlyingCard, getFootprintRadius, CHAMPION_ABILITY_COST } from './CardDatabase';

interface BattlePlayer {
  info: PlayerBattleInfo;
  connection: any;
  elixir: number;
  hand: number[];
  deck: number[];
  nextCardIndex: number;
  kingTowerActivated: boolean;
  isBot?: boolean;
  teamPlayers?: any[];
  acknowledgedTick: number;
  pendingInputs: Map<number, PlayerInput>;
}

type WinnerType = 'player1' | 'player2' | 'draw';

function battleStatusToWinner(status: BattleStatus): WinnerType {
  if (status === BattleStatus.Player1Won) return 'player1';
  if (status === BattleStatus.Player2Won) return 'player2';
  return 'draw';
}

export class BattleServer {
  public readonly battleId: string;
  private _simulation: BattleSimulation;
  private _entityManager: EntityManager;
  private _replayRecorder: ReplayRecorder;
  private _tickInterval: NodeJS.Timeout | null = null;
  private _player1: BattlePlayer;
  private _player2: BattlePlayer;
  private _status: BattleStatus = BattleStatus.Waiting;
  private _tick: number = 0;
  private _startTime: number;
  private _playerService: PlayerService;
  private _gameEnded: boolean = false;
  private _connectionManager: any;
  private _isBotMatch: boolean = false;
  private _inputBuffers: Map<string, PlayerInput[]> = new Map();
  private _botRng: DeterministicRNG;

  constructor(
    battleId: string,
    p1Info: PlayerBattleInfo,
    p2Info: PlayerBattleInfo,
    seed: number,
    playerService: PlayerService
  ) {
    this.battleId = battleId;
    this._playerService = playerService;
    this._startTime = Date.now();
    // Bot RNG is seeded from the battle seed so bot matches are reproducible
    // (ISSUE-204). Guard seed 0: Xorshift with a zero state never advances.
    this._botRng = new DeterministicRNG(BigInt(seed) === 0n ? 1n : BigInt(seed));

    // Initialize simulation
    this._simulation = new BattleSimulation();
    this._entityManager = new EntityManager();
    this._replayRecorder = new ReplayRecorder(battleId, BattleType.Ladder, BigInt(seed), p1Info, p2Info);

    // Create players
    this._player1 = this.createPlayer(p1Info, 1);
    this._player2 = this.createPlayer(p2Info, 2);

    // Initialize simulation with game config and decks
    const gameConfig = {
      startingElixir: config.game.startingElixir,
      maxElixir: config.game.maxElixir,
      elixirGenerationRate: config.game.elixirGenerationRate,
      doubleElixirRate: config.game.doubleElixirRate,
      tripleElixirRate: config.game.tripleElixirRate,
      battleDuration: config.game.battleDuration,
      overtimeDuration: config.game.overtimeDuration,
      handSize: config.game.handSize,
      kingTowerHP: 4336, // Level 11 King Tower HP
      princessTowerHP: 2168, // Level 11 Princess Tower HP
      towerDamage: 240,
      towerHitSpeed: 1.1,
      towerRange: 7,
    };

    // Card database would be loaded from data files
    const cardDatabase = new Map<number, any>();
    this._simulation.Initialize(gameConfig, BigInt(seed), p1Info.deck, p2Info.deck, cardDatabase);

    // Apply initial state to entity manager
    this.syncEntityManager();

    logger.info('Battle server created', { battleId, seed });
  }

  private createPlayer(info: PlayerBattleInfo, playerId: number): BattlePlayer {
    return {
      info,
      connection: null,
      elixir: config.game.startingElixir,
      hand: info.deck.slice(0, config.game.handSize),
      deck: [...info.deck],
      nextCardIndex: config.game.handSize,
      kingTowerActivated: false,
      acknowledgedTick: 0,
      pendingInputs: new Map(),
    };
  }

  setConnectionManager(connectionManager: any): void {
    this._connectionManager = connectionManager;
  }

  setConnection(playerId: string, connection: any): void {
    if (this._player1.info.playerId === playerId) {
      this._player1.connection = connection;
    } else if (this._player2.info.playerId === playerId) {
      this._player2.connection = connection;
    }
  }

  setTeamData(team1: any[], team2: any[]): void {
    this._player1.teamPlayers = team1;
    this._player2.teamPlayers = team2;
  }

  setBotMode(enabled: boolean): void {
    this._isBotMatch = enabled;
    this._player2.isBot = enabled;
    if (enabled) {
      this._player2.info.playerId = 'bot';
      this._player2.info.username = 'Practice Bot';
    }
  }

  start(): void {
    this._status = BattleStatus.Playing;
    this._tickInterval = setInterval(() => this.tick(), 1000 / 60); // 60Hz
    this.broadcastBattleStart();
  }

  private tick(): void {
    if (this._gameEnded || this._status !== BattleStatus.Playing) {
      if (this._tickInterval) {
        clearInterval(this._tickInterval);
        this._tickInterval = null;
      }
      return;
    }

    this._tick++;

    // 0. Retransmit stale unacknowledged inputs before stepping
    this.processAcknowledgments();

    // 1. Process queued inputs
    this.processInputs();

    // 2. Step simulation
    this._simulation.Tick();

    // 3. Record for replay
    const playerInputs = new Map<string, PlayerInputRecord[]>();
    playerInputs.set(this._player1.info.playerId, this.getPlayerInputsForTick(this._player1.info.playerId));
    playerInputs.set(this._player2.info.playerId, this.getPlayerInputsForTick(this._player2.info.playerId));
    const entities = this.getEntityStates();
    this._replayRecorder.recordFrame(this._tick, playerInputs, entities);

    // 4. Compute delta and broadcast
    const delta = this._entityManager.computeDelta(entities);
    this.broadcastGameState(delta);

    // 5. Check end condition
    if (this._simulation.Status !== BattleStatus.Playing) {
      this.endBattle(battleStatusToWinner(this._simulation.Status));
    }
  }

  private processInputs(): void {
    // Process player 1 inputs
    const p1Inputs = this._inputBuffers.get(this._player1.info.playerId) || [];
    for (const input of p1Inputs) {
      this._simulation.QueueInput(1, input);
    }
    this._inputBuffers.delete(this._player1.info.playerId);

    // Process player 2 inputs
    const p2Inputs = this._inputBuffers.get(this._player2.info.playerId) || [];
    for (const input of p2Inputs) {
      this._simulation.QueueInput(2, input);
    }
    this._inputBuffers.delete(this._player2.info.playerId);

    // Bot AI for practice matches
    if (this._isBotMatch && this._player2.isBot) {
      this.runBotAI();
    }
  }

  private runBotAI(): void {
    // Simple bot AI - play random card if enough elixir. Positions come from
    // the seeded DeterministicRNG so bot matches are reproducible (ISSUE-204).
    if (this._player2.elixir >= 3 && this._player2.hand.length > 0) {
      const cardId = this._player2.hand[0];
      const position = { x: 9 + (this._botRng.nextFloat() - 0.5) * 4, y: 20 + (this._botRng.nextFloat() - 0.5) * 4 };
      this._simulation.QueueInput(2, {
        type: 'play_card',
        cardId,
        position,
        clientTick: this._tick,
      });
    }
  }

  hasPlayer(playerId: string): boolean {
    return this.getPlayerById(playerId) !== null;
  }

  // Returns null when the input is accepted, otherwise a typed rejection
  // code (INVALID_POSITION et al.) that MessageHandler reports to the sender.
  handleInput(playerId: string, input: PlayerInput): string | null {
    if (this._status !== BattleStatus.Playing) return 'BATTLE_NOT_ACTIVE';

    const player = this.getPlayerById(playerId);
    if (!player) return 'NOT_IN_BATTLE';

    // Validate input
    const rejection = this.validateInput(player, input);
    if (rejection !== null) {
      logger.warn('Invalid input rejected', { playerId, input, reason: rejection });
      return rejection;
    }

    // Queue for next tick
    const buffer = this._inputBuffers.get(playerId) || [];
    buffer.push(input);
    this._inputBuffers.set(playerId, buffer);

    // Mark this tick as processed so the per-tick retransmit pass
    // (processAcknowledgments) does not resend it.
    player.acknowledgedTick = Math.max(player.acknowledgedTick, input.clientTick);
    if (player.connection && typeof player.connection.acknowledgeInput === 'function') {
      player.connection.acknowledgeInput(input.clientTick);
    }

    // Send immediate acknowledgment
    this.sendInputAck(playerId, input.clientTick);
    return null;
  }

  handleInputAck(playerId: string, ackTick: number): void {
    const player = this.getPlayerById(playerId);
    if (!player) return;
    if (typeof ackTick !== 'number' || !Number.isFinite(ackTick)) return;

    player.acknowledgedTick = Math.max(player.acknowledgedTick, ackTick);
    if (player.connection && typeof player.connection.acknowledgeInput === 'function') {
      player.connection.acknowledgeInput(ackTick);
    }
  }

  // Runs every tick: clears connection-level tracking up to the last
  // processed tick, then resends stale unacknowledged inputs back into
  // the simulation queue until they are acked or retries exhaust.
  processAcknowledgments(): PlayerInput[] {
    const resent: PlayerInput[] = [];
    for (const player of [this._player1, this._player2]) {
      const conn = player.connection;
      if (!conn) continue;
      if (typeof conn.acknowledgeInput === 'function') {
        conn.acknowledgeInput(player.acknowledgedTick);
      }
      if (typeof conn.retryUnacknowledgedInputs === 'function') {
        const stale: PlayerInput[] = conn.retryUnacknowledgedInputs(1000);
        for (const input of stale) {
          const buffer = this._inputBuffers.get(player.info.playerId) || [];
          buffer.push(input);
          this._inputBuffers.set(player.info.playerId, buffer);
          resent.push(input);
        }
        if (stale.length > 0) {
          logger.debug('Resent unacknowledged inputs', {
            battleId: this.battleId,
            playerId: player.info.playerId,
            count: stale.length,
          });
        }
      }
    }
    return resent;
  }

  // Null = valid; otherwise a typed rejection code for the sender.
  private validateInput(player: BattlePlayer, input: PlayerInput): string | null {
    // Check card ownership
    if (input.type === 'play_card' && input.cardId !== undefined) {
      if (!player.hand.includes(input.cardId)) {
        return 'CARD_NOT_IN_HAND';
      }
    }

    // Real elixir cost looked up from the card database.
    // Unknown cardId => reject. Champion abilities use the fixed
    // activation cost; emotes are free.
    const cardId = input.cardId ?? input.spellId;
    let cost: number;
    if (cardId !== undefined) {
      const realCost = getCardElixirCost(cardId);
      if (realCost === undefined) {
        return 'UNKNOWN_CARD';
      }
      cost = realCost;
    } else if (input.type === 'champion_ability') {
      cost = CHAMPION_ABILITY_COST;
    } else {
      cost = 0;
    }
    if (player.elixir < cost) {
      return 'INSUFFICIENT_ELIXIR';
    }

    // Check position validity
    if (!this.isValidPosition(player, input)) {
      return 'INVALID_POSITION';
    }

    // Rate limiting
    if (player.pendingInputs.size > 20) {
      return 'RATE_LIMITED';
    }

    return null;
  }

  // Server mirror of C# IsValidDeployPosition (BattleSimulation.cs:299-369):
  // canonical 13/19 deploy zones with princess-death expansion, river rules
  // for ground troops/buildings (flying allowlist via CardDatabase), and the
  // footprint-overlap loop over live sim state. Spells, champion abilities
  // and emotes target anywhere (no deploy semantics).
  private isValidPosition(player: BattlePlayer, input: PlayerInput): boolean {
    if (input.type === 'cast_spell' || input.type === 'champion_ability' || input.type === 'emote') return true;

    const cardId = input.cardId ?? input.spellId;
    if (cardId === undefined) return true;
    const def = getCardDefinition(cardId);
    if (!def) return false;
    if (def.type === 'spell') return true;

    const toCardType = (): CardType => {
      switch (def.type) {
        case 'troop': return CardType.Troop;
        case 'building': return CardType.Building;
        case 'champion': return CardType.Champion;
        default: return CardType.Spell;
      }
    };

    const playerId = player === this._player1 ? 1 : 2;
    return isValidDeployPosition(
      playerId,
      input.position,
      {
        type: toCardType(),
        name: def.name,
        isFlying: isFlyingCard(cardId),
        footprintRadius: getFootprintRadius(cardId),
      },
      buildDeploySnapshot(this._simulation),
    );
  }

  private getPlayerById(playerId: string): BattlePlayer | null {
    if (this._player1.info.playerId === playerId) return this._player1;
    if (this._player2.info.playerId === playerId) return this._player2;
    return null;
  }

  private getPlayerInputsForTick(playerId: string): PlayerInputRecord[] {
    const buffer = this._inputBuffers.get(playerId) || [];
    return buffer.map(input => ({
      playerId,
      input: {
        type: input.type,
        cardId: input.cardId,
        spellId: input.spellId,
        position: input.position,
        targetPosition: input.targetPosition,
        clientTick: input.clientTick,
      },
    }));
  }

  private getEntityStates(): EntityState[] {
    const entities: EntityState[] = [];

    // Units
    for (const unit of this._simulation.Units) {
      entities.push(this.unitToEntityState(unit));
    }

    // Buildings
    for (const building of this._simulation.Buildings) {
      entities.push(this.buildingToEntityState(building));
    }

    // Projectiles
    for (const projectile of this._simulation.Projectiles) {
      entities.push(this.projectileToEntityState(projectile));
    }

    // Towers
    for (const tower of this._simulation.Towers) {
      entities.push(this.towerToEntityState(tower));
    }

    // Active spells
    for (const spell of this._simulation.ActiveSpells) {
      entities.push(this.spellToEntityState(spell));
    }

    return entities;
  }

  private unitToEntityState(unit: any): EntityState {
    return {
      id: unit.Id,
      type: EntityType.Unit,
      owner: unit.OwnerPlayerId,
      position: { x: unit.Position.x.toFloat(), y: unit.Position.y.toFloat() },
      velocity: { x: unit.Velocity.x.toFloat(), y: unit.Velocity.y.toFloat() },
      hp: unit.CurrentHP,
      maxHp: unit.MaxHP,
      targetId: unit.TargetId,
      isDead: unit.IsDead,
      state: unit.State.toString(),
      attackCooldown: unit.AttackCooldown,
    };
  }

  private buildingToEntityState(building: any): EntityState {
    return {
      id: building.Id,
      type: EntityType.Building,
      owner: building.OwnerPlayerId,
      position: { x: building.Position.x.toFloat(), y: building.Position.y.toFloat() },
      velocity: { x: 0, y: 0 },
      hp: building.CurrentHP,
      maxHp: building.MaxHP,
      targetId: 0,
      isDead: building.IsDead,
      lifetime: building.Lifetime,
      isRetracted: building.IsRetracted,
      spawnTimer: building.SpawnTimer,
    };
  }

  private projectileToEntityState(projectile: any): EntityState {
    return {
      id: projectile.Id,
      type: EntityType.Projectile,
      owner: projectile.OwnerPlayerId,
      position: { x: projectile.Position.x.toFloat(), y: projectile.Position.y.toFloat() },
      velocity: { x: projectile.Velocity.x.toFloat(), y: projectile.Velocity.y.toFloat() },
      hp: 1,
      maxHp: 1,
      targetId: projectile.TargetId,
      isDead: projectile.IsDead,
      sourceId: projectile.SourceId,
      isBeam: projectile.IsBeam,
    };
  }

  private towerToEntityState(tower: any): EntityState {
    return {
      id: tower.Id,
      type: EntityType.Tower,
      owner: tower.OwnerPlayerId,
      position: { x: tower.Position.x.toFloat(), y: tower.Position.y.toFloat() },
      velocity: { x: 0, y: 0 },
      hp: tower.CurrentHP,
      maxHp: tower.MaxHP,
      targetId: tower.TargetId || 0,
      isDead: tower.IsDead,
    };
  }

  private spellToEntityState(spell: any): EntityState {
    return {
      id: spell.Id,
      type: EntityType.SpellEffect,
      owner: spell.OwnerPlayerId,
      position: { x: spell.Position.x.toFloat(), y: spell.Position.y.toFloat() },
      velocity: { x: 0, y: 0 },
      hp: 1,
      maxHp: 1,
      targetId: 0,
      isDead: spell.IsFinished,
      spellType: spell.SpellType.toString(),
      radius: spell.Radius.toFloat(),
      remainingTime: spell.RemainingTime,
    };
  }

  private syncEntityManager(): void {
    const entities = this.getEntityStates();
    this._entityManager.applyFullState(entities);
  }

  private broadcastGameState(delta: EntityState[]): void {
    if (!this._connectionManager) return;

    const playerStates = this.getPlayerStates();
    
    const message = {
      type: 'game_state',
      tick: this._tick,
      entities: delta,
      projectiles: delta.filter(e => e.type === 'projectile'),
      player1: playerStates[0],
      player2: playerStates[1],
      status: this._simulation.Status,
    };

    this._connectionManager.broadcastToBattle(this.battleId, message);
  }

  private getPlayerStates(): [PlayerState, PlayerState] {
    return [
      {
        playerId: 1,
        elixir: Math.floor(this._simulation.Player1.Elixir),
        hand: this._simulation.Player1.Hand,
        deck: this._simulation.Player1.Deck,
        nextCardIndex: this._simulation.Player1.NextCardIndex,
        kingTowerActivated: this._simulation.Player1.KingTowerActivated,
      },
      {
        playerId: 2,
        elixir: Math.floor(this._simulation.Player2.Elixir),
        hand: this._simulation.Player2.Hand,
        deck: this._simulation.Player2.Deck,
        nextCardIndex: this._simulation.Player2.NextCardIndex,
        kingTowerActivated: this._simulation.Player2.KingTowerActivated,
      },
    ];
  }

  private broadcastBattleStart(): void {
    if (!this._connectionManager) return;

    const message = {
      type: 'battle_start',
      battleId: this.battleId,
      tick: 0,
      player1: this.getPlayerStates()[0],
      player2: this.getPlayerStates()[1],
    };

    this._connectionManager.broadcastToBattle(this.battleId, message);
  }

  private sendInputAck(playerId: string, ackTick: number): void {
    if (!this._connectionManager) return;

    const message = {
      type: 'input_ack',
      ackTick,
    };

    const player = this.getPlayerById(playerId);
    if (player?.connection) {
      player.connection.send(message);
    }
  }

  handleDisconnect(playerId: string): void {
    if (this._gameEnded) return;

    const player = this.getPlayerById(playerId);
    if (!player) return;

    logger.info('Player disconnected during battle', { battleId: this.battleId, playerId });

    // Give grace period for reconnection
    setTimeout(() => {
      if (!player.connection && !this._gameEnded) {
        this.endBattle(player === this._player1 ? 'player2' : 'player1');
      }
    }, 10000);
  }

  forceEnd(): void {
    if (this._gameEnded) return;
    this.endBattle('draw');
  }

  // Trophy + ELO deltas for a finished battle, delegated to the shared
  // RatingSystem so payouts match matchmaking expectations exactly.
  public computeRatingChanges(
    winner: WinnerType,
    p1Crowns: number,
    p2Crowns: number
  ): {
    player1TrophyChange: number;
    player2TrophyChange: number;
    player1EloChange: number;
    player2EloChange: number;
  } {
    const p1Trophies = this._player1.info.trophies;
    const p2Trophies = this._player2.info.trophies;

    const trophy = ratingSystem.calculateTrophyChangeFromResult(
      {
        battleId: this.battleId,
        winner,
        player1Crowns: p1Crowns,
        player2Crowns: p2Crowns,
        player1TrophyChange: 0,
        player2TrophyChange: 0,
        duration: 0,
        wentOvertime: false,
        replayId: '',
      },
      p1Trophies,
      p2Trophies
    );

    // ELO uses trophies as the rating proxy; score follows the winner.
    const p1Score = winner === 'player1' ? 1 : winner === 'draw' ? 0.5 : 0;
    const player1EloChange = ratingSystem.calculateELOChange(p1Trophies, p2Trophies, p1Score);
    const player2EloChange = ratingSystem.calculateELOChange(p2Trophies, p1Trophies, 1 - p1Score);

    logger.info('Battle rating changes', {
      battleId: this.battleId,
      winner,
      p1Crowns,
      p2Crowns,
      player1TrophyChange: trophy.player1Change,
      player2TrophyChange: trophy.player2Change,
      player1EloChange,
      player2EloChange,
    });

    return {
      player1TrophyChange: trophy.player1Change,
      player2TrophyChange: trophy.player2Change,
      player1EloChange,
      player2EloChange,
    };
  }

  private endBattle(winner: WinnerType): void {
    if (this._gameEnded) return;
    this._gameEnded = true;

    if (this._tickInterval) {
      clearInterval(this._tickInterval);
      this._tickInterval = null;
    }

    this._status = winner === 'player1' ? BattleStatus.Player1Won : 
                  winner === 'player2' ? BattleStatus.Player2Won : BattleStatus.Draw;

    const duration = Math.floor((Date.now() - this._startTime) / 1000);

    // Calculate crowns. Crowns are EARNED by destroying ENEMY towers
    // (mirrors the sim CheckWinCondition semantics): P1 crowns count dead
    // P2 princess towers and vice versa. A king kill ends 3-0.
    let p1Crowns = 0, p2Crowns = 0;
    for (const tower of this._simulation.Towers) {
      if (tower.TowerType !== TowerType.King && tower.IsDead) {
        if (tower.OwnerPlayerId === 1) p2Crowns++;
        else p1Crowns++;
      }
    }
    if (winner === 'player1') p1Crowns = 3;
    if (winner === 'player2') p2Crowns = 3;

    // Rating changes from the shared RatingSystem (diff-scaled base,
    // 1.5x/1.2x crown multipliers, 3-crown bonus). A forfeit is settled
    // as a normal rated result for the recorded winner; a forced draw
    // (e.g. server shutdown) yields 0/0 from the RatingSystem.
    const rating = this.computeRatingChanges(winner, p1Crowns, p2Crowns);

    const result = {
      battleId: this.battleId,
      winner,
      player1Crowns: p1Crowns,
      player2Crowns: p2Crowns,
      player1TrophyChange: rating.player1TrophyChange,
      player2TrophyChange: rating.player2TrophyChange,
      duration,
      wentOvertime: duration > 180,
      replayId: '',
    };

    // Save replay synchronously
    const replayData = this._replayRecorder.finalize(result);
    
    // Save replay and battle result asynchronously
    this.saveReplayAndResult(replayData, result);
  }

  private async saveReplayAndResult(replayData: any, result: any): Promise<void> {
    try {
      const { Database } = require('../persistence/Database');
      const db = new Database();
      await db.connect();
      result.replayId = await this._replayRecorder.save(db);
      await this.saveBattleResult(result);
      await db.close();
      this.sendBattleEnd(result);
    } catch (error) {
      logger.error('Failed to finalize replay', { error: error instanceof Error ? error.message : String(error) });
    }
  }

  private sendBattleEnd(result: any): void {
    if (!this._connectionManager) return;

    const message = {
      type: 'battle_end',
      battleId: this.battleId,
      result: {
        winner: result.winner,
        player1Crowns: result.player1Crowns,
        player2Crowns: result.player2Crowns,
        player1TrophyChange: result.player1TrophyChange,
        player2TrophyChange: result.player2TrophyChange,
        duration: result.duration,
        wentOvertime: result.wentOvertime,
        replayId: result.replayId,
      },
    };

    this._connectionManager.broadcastToBattle(this.battleId, message);
  }

  private async saveBattleResult(result: any): Promise<void> {
    try {
      await this._playerService.saveBattleResult(result);
    } catch (error) {
      logger.error('Failed to save battle result', { error: error instanceof Error ? error.message : String(error) });
    }
  }
}