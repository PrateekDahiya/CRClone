import { logger } from '../utils/logger';
import { BattleType, BattleStatus, PlayerBattleInfo, PlayerState, EntityState, BattleResult } from '../types';
import { Database } from '../persistence/Database';
import { DeterministicRNG } from '../utils/rng';

export interface ReplayFrame {
  tick: number;
  inputs: PlayerInputRecord[];
  entitiesHash: string;
}

export interface PlayerInputRecord {
  playerId: string;
  input: {
    type: string;
    cardId?: number;
    spellId?: number;
    position: { x: number; y: number };
    targetPosition?: { x: number; y: number };
    clientTick: number;
  };
}

export interface ReplayMetadata {
  battleId: string;
  battleType: BattleType;
  seed: string; // bigint as string
  player1: PlayerBattleInfo;
  player2: PlayerBattleInfo;
  startTime: number;
  duration: number;
  winner: 'player1' | 'player2' | 'draw';
  player1Crowns: number;
  player2Crowns: number;
  player1TrophyChange: number;
  player2TrophyChange: number;
  wentOvertime: boolean;
}

export interface ReplayData {
  metadata: ReplayMetadata;
  frames: ReplayFrame[];
}

export class ReplayRecorder {
  private _battleId: string;
  private _battleType: BattleType;
  private _seed: bigint;
  private _player1: PlayerBattleInfo;
  private _player2: PlayerBattleInfo;
  private _startTime: number;
  private _frames: ReplayFrame[] = [];
  private _inputBuffer: Map<string, PlayerInputRecord[]> = new Map();
  private _rng: DeterministicRNG;

  constructor(battleId: string, battleType: BattleType, seed: bigint, player1: PlayerBattleInfo, player2: PlayerBattleInfo) {
    this._battleId = battleId;
    this._battleType = battleType;
    this._seed = seed;
    this._player1 = player1;
    this._player2 = player2;
    this._startTime = Date.now();
    this._rng = new DeterministicRNG(seed);
  }

  public recordFrame(tick: number, playerInputs: Map<string, PlayerInputRecord[]>, entities: EntityState[]): void {
    // Combine inputs from both players
    const allInputs: PlayerInputRecord[] = [];
    for (const inputs of playerInputs.values()) {
      allInputs.push(...inputs);
    }

    // Compute entities hash for desync detection
    const entitiesHash = this.computeEntitiesHash(entities);

    const frame: ReplayFrame = {
      tick,
      inputs: allInputs,
      entitiesHash,
    };

    this._frames.push(frame);
  }

  public recordInput(playerId: string, input: PlayerInputRecord): void {
    const buffer = this._inputBuffer.get(playerId) || [];
    buffer.push(input);
    this._inputBuffer.set(playerId, buffer);
  }

  public getInputsForTick(tick: number): PlayerInputRecord[] {
    const allInputs: PlayerInputRecord[] = [];
    for (const buffer of this._inputBuffer.values()) {
      for (const input of buffer) {
        if (input.input.clientTick === tick) {
          allInputs.push(input);
        }
      }
    }
    return allInputs;
  }

  public finalize(result: BattleResult): ReplayData {
    const duration = Math.floor((Date.now() - this._startTime) / 1000);
    const wentOvertime = duration > 180; // 3 minutes

    const metadata: ReplayMetadata = {
      battleId: this._battleId,
      battleType: this._battleType,
      seed: this._seed.toString(),
      player1: this._player1,
      player2: this._player2,
      startTime: this._startTime,
      duration,
      winner: result.winner,
      player1Crowns: result.player1Crowns,
      player2Crowns: result.player2Crowns,
      player1TrophyChange: result.player1TrophyChange,
      player2TrophyChange: result.player2TrophyChange,
      wentOvertime,
    };

    return {
      metadata,
      frames: this._frames,
    };
  }

  public async save(database: Database): Promise<string> {
    const replayData = this.finalize({
      battleId: this._battleId,
      winner: 'draw', // Will be updated by BattleServer
      player1Crowns: 0,
      player2Crowns: 0,
      player1TrophyChange: 0,
      player2TrophyChange: 0,
      duration: 0,
      wentOvertime: false,
      replayId: '',
    });

    // Compress replay data
    const compressed = this.compress(JSON.stringify(replayData));
    const replayId = `replay_${Date.now()}_${Math.random().toString(36).substr(2, 9)}`;

    try {
      await database.execute(
        `INSERT INTO replays (replay_id, battle_id, data, metadata, created_at)
         VALUES (?, ?, ?, ?, NOW())`,
        [replayId, this._battleId, compressed, JSON.stringify(replayData.metadata)]
      );

      logger.info('Replay saved', { replayId, battleId: this._battleId, frameCount: this._frames.length });
      return replayId;
    } catch (error) {
      logger.error('Failed to save replay', { error: error instanceof Error ? error.message : String(error) });
      throw error;
    }
  }

  public static async load(database: Database, replayId: string): Promise<ReplayData | null> {
    try {
      const rows = await database.query(
        'SELECT data, metadata FROM replays WHERE replay_id = ?',
        [replayId]
      );

      if (rows.length === 0) return null;

      const row = rows[0];
      const decompressed = this.decompress(row.data);
      const replayData = JSON.parse(decompressed);
      
      return replayData;
    } catch (error) {
      logger.error('Failed to load replay', { replayId, error: error instanceof Error ? error.message : String(error) });
      return null;
    }
  }

  private computeEntitiesHash(entities: EntityState[]): string {
    // Simple hash for desync detection
    let hash = 0;
    for (const entity of entities) {
      hash = ((hash << 5) - hash) + entity.id;
      hash = ((hash << 5) - hash) + Math.round(entity.position.x * 1000);
      hash = ((hash << 5) - hash) + Math.round(entity.position.y * 1000);
      hash = ((hash << 5) - hash) + entity.hp;
      hash |= 0; // Convert to 32bit integer
    }
    return hash.toString(16);
  }

  private compress(data: string): Buffer {
    // Simple compression - in production use zlib or lz4
    return Buffer.from(data);
  }

  private static decompress(data: Buffer): string {
    return data.toString('utf-8');
  }

  public getFrameCount(): number {
    return this._frames.length;
  }

  public getFrames(): ReadonlyArray<ReplayFrame> {
    return this._frames;
  }
}

export class ReplayPlayer {
  private _replayData: ReplayData;
  private _currentFrameIndex: number = 0;
  private _simulation: any; // BattleSimulation instance

  constructor(replayData: ReplayData) {
    this._replayData = replayData;
  }

  public setSimulation(simulation: any): void {
    this._simulation = simulation;
  }

  public step(): boolean {
    if (this._currentFrameIndex >= this._replayData.frames.length) {
      return false;
    }

    const frame = this._replayData.frames[this._currentFrameIndex];
    
    // Apply inputs to simulation
    for (const inputRecord of frame.inputs) {
      const playerId = inputRecord.playerId === this._replayData.metadata.player1.playerId ? 1 : 2;
      this._simulation.QueueInput(playerId, {
        type: inputRecord.input.type as any,
        cardId: inputRecord.input.cardId,
        spellId: inputRecord.input.spellId,
        position: inputRecord.input.position,
        targetPosition: inputRecord.input.targetPosition,
        clientTick: inputRecord.input.clientTick,
      });
    }

    // Step simulation
    this._simulation.Tick();

    // Verify hash
    const currentEntities = this._simulation.Units.map((u: any) => u)
      .concat(this._simulation.Buildings.map((b: any) => b))
      .concat(this._simulation.Projectiles.map((p: any) => p))
      .concat(this._simulation.Towers.map((t: any) => t));
    
    const currentHash = this.computeEntitiesHash(currentEntities);
    if (currentHash !== frame.entitiesHash) {
      logger.warn('Replay desync detected', { 
        tick: frame.tick, 
        expectedHash: frame.entitiesHash, 
        actualHash: currentHash 
      });
    }

    this._currentFrameIndex++;
    return true;
  }

  public play(): void {
    while (this.step()) {
      // Continue until end
    }
  }

  public getProgress(): number {
    return this._currentFrameIndex / this._replayData.frames.length;
  }

  private computeEntitiesHash(entities: any[]): string {
    let hash = 0;
    for (const entity of entities) {
      hash = ((hash << 5) - hash) + entity.Id;
      hash = ((hash << 5) - hash) + Math.round(entity.Position.x.toFloat() * 1000);
      hash = ((hash << 5) - hash) + Math.round(entity.Position.y.toFloat() * 1000);
      hash = ((hash << 5) - hash) + entity.CurrentHP;
      hash |= 0;
    }
    return hash.toString(16);
  }
}