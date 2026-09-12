import { Database } from './Database';
import { logger } from '../utils/logger';
import { BattleType, BattleStatus, BattleResult, PlayerBattleInfo } from '../types';

export interface BattleRecord {
  battleId: string;
  battleType: BattleType;
  seed: bigint;
  player1Id: string;
  player2Id: string;
  player1Deck: number[];
  player2Deck: number[];
  player3Id?: string;
  player4Id?: string;
  player3Deck?: number[];
  player4Deck?: number[];
  winner?: 'player1' | 'player2' | 'draw';
  player1Crowns: number;
  player2Crowns: number;
  player1TrophyChange: number;
  player2TrophyChange: number;
  duration: number;
  wentOvertime: boolean;
  replayId?: string;
}

export class BattleRepository {
  constructor(private db: Database) {}

  async create(battle: BattleRecord): Promise<void> {
    await this.db.execute(
      `INSERT INTO battles (battle_id, battle_type, seed, player1_id, player2_id, player1_deck, player2_deck,
                            player3_id, player4_id, player3_deck, player4_deck,
                            winner, player1_crowns, player2_crowns, player1_trophy_change, player2_trophy_change,
                            duration, went_overtime, replay_id, created_at)
       VALUES (?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, NOW())`,
      [
        battle.battleId,
        battle.battleType,
        battle.seed.toString(),
        battle.player1Id,
        battle.player2Id,
        JSON.stringify(battle.player1Deck),
        JSON.stringify(battle.player2Deck),
        battle.player3Id || null,
        battle.player4Id || null,
        battle.player3Deck ? JSON.stringify(battle.player3Deck) : null,
        battle.player4Deck ? JSON.stringify(battle.player4Deck) : null,
        battle.winner || null,
        battle.player1Crowns,
        battle.player2Crowns,
        battle.player1TrophyChange,
        battle.player2TrophyChange,
        battle.duration,
        battle.wentOvertime,
        battle.replayId || null,
      ]
    );
  }

  async updateResult(battleId: string, result: BattleResult): Promise<void> {
    await this.db.execute(
      `UPDATE battles 
       SET winner = ?, player1_crowns = ?, player2_crowns = ?, 
           player1_trophy_change = ?, player2_trophy_change = ?, 
           duration = ?, went_overtime = ?, replay_id = ?, ended_at = NOW()
       WHERE battle_id = ?`,
      [
        result.winner,
        result.player1Crowns,
        result.player2Crowns,
        result.player1TrophyChange,
        result.player2TrophyChange,
        result.duration,
        result.wentOvertime,
        result.replayId,
        battleId,
      ]
    );
  }

  async getById(battleId: string): Promise<BattleRecord | null> {
    const rows = await this.db.query(
      'SELECT * FROM battles WHERE battle_id = ?',
      [battleId]
    );
    if (rows.length === 0) return null;
    return this.mapRowToBattle(rows[0]);
  }

  async getHistory(playerId: string, limit: number): Promise<BattleRecord[]> {
    const rows = await this.db.query(
      `SELECT * FROM battles 
       WHERE player1_id = ? OR player2_id = ? OR player3_id = ? OR player4_id = ?
       ORDER BY created_at DESC LIMIT ?`,
      [playerId, playerId, playerId, playerId, limit]
    );
    return rows.map(row => this.mapRowToBattle(row));
  }

  async getRecentBattles(limit: number, battleType?: BattleType): Promise<BattleRecord[]> {
    let query = 'SELECT * FROM battles';
    const params: any[] = [];

    if (battleType) {
      query += ' WHERE battle_type = ?';
      params.push(battleType);
    }

    query += ' ORDER BY created_at DESC LIMIT ?';
    params.push(limit);

    const rows = await this.db.query(query, params);
    return rows.map(row => this.mapRowToBattle(row));
  }

  async getPlayerStats(playerId: string): Promise<{
    totalBattles: number;
    wins: number;
    losses: number;
    draws: number;
    winRate: number;
    totalCrowns: number;
    avgDuration: number;
  }> {
    const rows = await this.db.query(
      `SELECT 
         COUNT(*) as total,
         SUM(CASE WHEN (player1_id = ? AND winner = 'player1') OR (player2_id = ? AND winner = 'player2') THEN 1 ELSE 0 END) as wins,
         SUM(CASE WHEN (player1_id = ? AND winner = 'player2') OR (player2_id = ? AND winner = 'player1') THEN 1 ELSE 0 END) as losses,
         SUM(CASE WHEN winner = 'draw' THEN 1 ELSE 0 END) as draws,
         SUM(CASE WHEN player1_id = ? THEN player1_crowns ELSE player2_crowns END) as total_crowns,
         AVG(duration) as avg_duration
       FROM battles 
       WHERE player1_id = ? OR player2_id = ?`,
      [playerId, playerId, playerId, playerId, playerId, playerId, playerId]
    );

    const row = rows[0];
    const total = row.total || 0;
    return {
      totalBattles: total,
      wins: row.wins || 0,
      losses: row.losses || 0,
      draws: row.draws || 0,
      winRate: total > 0 ? (row.wins || 0) / total : 0,
      totalCrowns: row.total_crowns || 0,
      avgDuration: row.avg_duration || 0,
    };
  }

  private mapRowToBattle(row: any): BattleRecord {
    return {
      battleId: row.battle_id,
      battleType: row.battle_type,
      seed: BigInt(row.seed),
      player1Id: row.player1_id,
      player2Id: row.player2_id,
      player1Deck: JSON.parse(row.player1_deck),
      player2Deck: JSON.parse(row.player2_deck),
      player3Id: row.player3_id,
      player4Id: row.player4_id,
      player3Deck: row.player3_deck ? JSON.parse(row.player3_deck) : undefined,
      player4Deck: row.player4_deck ? JSON.parse(row.player4_deck) : undefined,
      winner: row.winner,
      player1Crowns: row.player1_crowns,
      player2Crowns: row.player2_crowns,
      player1TrophyChange: row.player1_trophy_change,
      player2TrophyChange: row.player2_trophy_change,
      duration: row.duration,
      wentOvertime: row.went_overtime,
      replayId: row.replay_id,
    };
  }

  // Alias for service compatibility
  async createBattle(data: any): Promise<void> {
    await this.db.execute(
      `INSERT INTO battles (battle_type, player_1_id, player_2_id, player_3_id, player_4_id,
        player_1_deck_id, player_2_deck_id, player_3_deck_id, player_4_deck_id,
        winner_team, player_1_crowns, player_2_crowns, player_3_crowns, player_4_crowns,
        player_1_king_hp, player_2_king_hp, player_3_king_hp, player_4_king_hp,
        duration_seconds, went_overtime,
        player_1_trophy_change, player_2_trophy_change, player_3_trophy_change, player_4_trophy_change,
        replay_id, replay_seed,
        player_1_trophies_at_start, player_2_trophies_at_start, player_3_trophies_at_start, player_4_trophies_at_start)
       VALUES (?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?)`,
      [
        data.battle_type, data.player_1_id, data.player_2_id, data.player_3_id, data.player_4_id,
        data.player_1_deck_id, data.player_2_deck_id, data.player_3_deck_id, data.player_4_deck_id,
        data.winner_team, data.player_1_crowns, data.player_2_crowns, data.player_3_crowns, data.player_4_crowns,
        data.player_1_king_hp, data.player_2_king_hp, data.player_3_king_hp, data.player_4_king_hp,
        data.duration_seconds, data.went_overtime,
        data.player_1_trophy_change, data.player_2_trophy_change, data.player_3_trophy_change, data.player_4_trophy_change,
        data.replay_id, data.replay_seed,
        data.player_1_trophies_at_start, data.player_2_trophies_at_start, data.player_3_trophies_at_start, data.player_4_trophies_at_start
      ]
    );
  }
}