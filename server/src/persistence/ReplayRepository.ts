import { Database } from './Database';
import { logger } from '../utils/logger';

export interface ReplayMetadata {
  replayId: number;
  battleId: number;
  replayVersion: string;
  player1Id: string | null;
  player2Id: string | null;
  player1DeckHash: string | null;
  player2DeckHash: string | null;
  durationSeconds: number;
  createdAt: Date;
  expiresAt: Date | null;
  battleType: string;
  winnerTeam: number | null;
  player1Crowns: number;
  player2Crowns: number;
}

export interface ReplayData {
  replayId: number;
  replayData: Buffer;
  replayVersion: string;
}

export interface ReplaySearchQuery {
  playerId?: string;
  deckHash?: string;
  minDuration?: number;
  maxDuration?: number;
  battleType?: string;
  dateFrom?: Date;
  dateTo?: Date;
  limit?: number;
  offset?: number;
}

export class ReplayRepository {
  constructor(private db: Database) {}

  async storeReplay(data: { battleId: number; replayData: Buffer; replayVersion: string; player1Id: string; player2Id: string | null; player1DeckHash: string; player2DeckHash: string | null; durationSeconds: number; expiresAt: Date }): Promise<number> {
    const result = await this.db.execute(
      `INSERT INTO replays (battle_id, replay_data, replay_version, player_1_id, player_2_id, player_1_deck_hash, player_2_deck_hash, duration_seconds, expires_at) VALUES (?, ?, ?, ?, ?, ?, ?, ?, ?)`,
      [data.battleId, data.replayData, data.replayVersion, data.player1Id, data.player2Id, data.player1DeckHash, data.player2DeckHash, data.durationSeconds, data.expiresAt]
    );
    return result.insertId;
  }

  async getReplayData(replayId: number): Promise<ReplayData | null> {
    const rows = await this.db.query('SELECT replay_id, replay_data, replay_version FROM replays WHERE replay_id = ?', [replayId]);
    return rows[0] || null;
  }

  async getReplayMetadata(replayId: number): Promise<ReplayMetadata | null> {
    const rows = await this.db.query(
      `SELECT r.*, b.battle_type, b.winner_team, b.player_1_crowns, b.player_2_crowns FROM replays r JOIN battles b ON r.battle_id = b.battle_id WHERE r.replay_id = ?`,
      [replayId]
    );
    return rows[0] || null;
  }

  async getPlayerReplays(playerId: string, limit: number = 20, offset: number = 0): Promise<ReplayMetadata[]> {
    return this.db.query(
      `SELECT r.*, b.battle_type, b.winner_team, b.player_1_crowns, b.player_2_crowns FROM replays r JOIN battles b ON r.battle_id = b.battle_id WHERE b.player_1_id = ? OR b.player_2_id = ? ORDER BY r.created_at DESC LIMIT ? OFFSET ?`,
      [playerId, playerId, limit, offset]
    );
  }

  async searchReplays(filters: any): Promise<ReplayMetadata[]> {
    let sql = `SELECT r.*, b.battle_type, b.winner_team, b.player_1_crowns, b.player_2_crowns FROM replays r JOIN battles b ON r.battle_id = b.battle_id WHERE 1=1`;
    const params: any[] = [];
    if (filters.playerId) { sql += ` AND (b.player_1_id = ? OR b.player_2_id = ?)`; params.push(filters.playerId, filters.playerId); }
    if (filters.battleType) { sql += ` AND b.battle_type = ?`; params.push(filters.battleType); }
    sql += ` ORDER BY r.created_at DESC LIMIT ? OFFSET ?`;
    params.push(filters.limit || 50, filters.offset || 0);
    return this.db.query(sql, params);
  }

  async deleteReplay(replayId: number): Promise<void> { await this.db.execute('DELETE FROM replays WHERE replay_id = ?', [replayId]); }
  async deleteExpiredReplays(retentionDays: number = 30): Promise<number> {
    const expiresBefore = new Date(); expiresBefore.setDate(expiresBefore.getDate() - retentionDays);
    const result = await this.db.execute('DELETE FROM replays WHERE expires_at IS NOT NULL AND expires_at < ?', [expiresBefore]);
    return result.affectedRows;
  }

  async getReplayStats(): Promise<any> {
    const [totalResult] = await this.db.query('SELECT COUNT(*) as total, COALESCE(SUM(LENGTH(replay_data)), 0) as size FROM replays');
    const [dateResult] = await this.db.query('SELECT MIN(created_at) as oldest, MAX(created_at) as newest FROM replays');
    const typeRows = await this.db.query(`SELECT b.battle_type, COUNT(*) as count FROM replays r JOIN battles b ON r.battle_id = b.battle_id GROUP BY b.battle_type`);
    const byBattleType: Record<string, number> = {}; for (const row of typeRows) byBattleType[row.battle_type] = row.count;
    return { totalReplays: totalResult.total, totalSizeBytes: totalResult.size, oldestReplay: dateResult.oldest, newestReplay: dateResult.newest, byBattleType };
  }

  async getReplayCount(filters: any): Promise<number> {
    let sql = `SELECT COUNT(*) as count FROM replays r JOIN battles b ON r.battle_id = b.battle_id WHERE 1=1`;
    const params: any[] = [];
    if (filters.playerId) { sql += ` AND (b.player_1_id = ? OR b.player_2_id = ?)`; params.push(filters.playerId, filters.playerId); }
    if (filters.battleType) { sql += ` AND b.battle_type = ?`; params.push(filters.battleType); }
    if (filters.startDate) { sql += ` AND r.created_at >= ?`; params.push(filters.startDate); }
    if (filters.endDate) { sql += ` AND r.created_at <= ?`; params.push(filters.endDate); }
    const rows = await this.db.query(sql, params);
    return rows[0]?.count || 0;
  }
}