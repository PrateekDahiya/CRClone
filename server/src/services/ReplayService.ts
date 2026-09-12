import { Database, db } from '../persistence/Database';
import { logger } from '../utils/logger';
import { ReplayRepository, ReplayMetadata, ReplayData } from '../persistence/ReplayRepository';
import { v4 as uuidv4 } from 'uuid';

export interface ReplaySearchFilters {
  playerId?: string;
  playerName?: string;
  deckHash?: string;
  battleType?: string;
  minDuration?: number;
  maxDuration?: number;
  startDate?: Date;
  endDate?: Date;
  winnerTeam?: number;
  limit?: number;
  offset?: number;
}

export class ReplayService {
  private db: Database;
  private replayRepo: ReplayRepository;

  constructor(database: Database = db) {
    this.db = database;
    this.replayRepo = new ReplayRepository(database);
  }

  async storeReplay(data: { battleId: number; replayData: Buffer; replayVersion: string; player1Id: string; player2Id: string | null; player1DeckHash: string; player2DeckHash: string | null; durationSeconds: number; retentionDays?: number; }): Promise<number> {
    const retentionDays = data.retentionDays || 30;
    const expiresAt = new Date();
    expiresAt.setDate(expiresAt.getDate() + retentionDays);
    const replayId = await this.replayRepo.storeReplay({ ...data, expiresAt });
    logger.info('Replay stored', { replayId, battleId: data.battleId, size: data.replayData.length });
    return replayId;
  }

  async getReplayData(replayId: number): Promise<ReplayData | null> { return this.replayRepo.getReplayData(replayId); }
  async getReplayMetadata(replayId: number): Promise<ReplayMetadata | null> { return this.replayRepo.getReplayMetadata(replayId); }
  async getReplayByBattleId(battleId: number): Promise<ReplayMetadata | null> { return this.replayRepo.getReplayMetadata(battleId); }
  async getPlayerReplays(playerId: string, limit: number = 20, offset: number = 0): Promise<ReplayMetadata[]> { return this.replayRepo.getPlayerReplays(playerId, limit, offset); }
  async searchReplays(filters: ReplaySearchFilters): Promise<ReplayMetadata[]> { return this.replayRepo.searchReplays(filters); }
  async getReplayCount(filters: ReplaySearchFilters): Promise<number> { return this.replayRepo.getReplayCount(filters); }
  async deleteReplay(replayId: number): Promise<void> { await this.replayRepo.deleteReplay(replayId); }
  async cleanupExpiredReplays(retentionDays: number = 30): Promise<number> { const deleted = await this.replayRepo.deleteExpiredReplays(retentionDays); logger.info('Expired replays cleaned up', { deleted, retentionDays }); return deleted; }
  async getReplayStats(): Promise<any> { return this.replayRepo.getReplayStats(); }
  static generateDeckHash(cardIds: number[]): string { const sorted = [...cardIds].sort((a, b) => a - b); const str = sorted.join(','); let hash = 0; for (let i = 0; i < str.length; i++) { const char = str.charCodeAt(i); hash = ((hash << 5) - hash) + char; hash = hash & hash; } return Math.abs(hash).toString(16); }
  static compressReplayData(data: any): Buffer { return Buffer.from(JSON.stringify(data), 'utf-8'); }
  static decompressReplayData(buffer: Buffer): any { return JSON.parse(buffer.toString('utf-8')); }
  static isCompatibleVersion(replayVersion: string, currentVersion: string): boolean { const replayMajor = parseInt(replayVersion.split('.')[0]); const currentMajor = parseInt(currentVersion.split('.')[0]); return replayMajor === currentMajor; }
}