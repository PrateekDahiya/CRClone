import { BaseRepository } from './BaseRepository';
import { Database, db } from './Database';
import { logger } from '../utils/logger';

export interface LeaderboardRow {
  leaderboard_id: number;
  leaderboard_type: string;
  scope: string;
  scope_id: number | null;
  player_id: string;
  rank: number;
  score: number;
  snapshot_at: Date;
}

export interface LeaderboardSnapshotRow {
  snapshot_id: number;
  leaderboard_type: string;
  scope: string;
  scope_id: number | null;
  data: any;
  created_at: Date;
}

export interface PlayerRankRow {
  player_id: string;
  username: string;
  rank: number;
  score: number;
}

export class LeaderboardRepository extends BaseRepository {
  constructor(database: Database = db) {
    super(database);
  }

  // Leaderboard CRUD
  async refreshLeaderboard(
    leaderboardType: string,
    scope: string,
    scopeId: number | null,
    entries: Array<{playerId: string, score: number}>
  ): Promise<void> {
    // Sort by score descending
    entries.sort((a, b) => b.score - a.score);

    await this.transaction(async (conn) => {
      // Delete existing entries for this leaderboard
      await conn.execute(
        'DELETE FROM leaderboards WHERE leaderboard_type = ? AND scope = ? AND scope_id <=> ?',
        [leaderboardType, scope, scopeId]
      );

      // Insert new entries with ranks
      for (let i = 0; i < entries.length; i++) {
        const entry = entries[i];
        await conn.execute(
          `INSERT INTO leaderboards (leaderboard_type, scope, scope_id, player_id, rank, score)
           VALUES (?, ?, ?, ?, ?, ?)`,
          [leaderboardType, scope, scopeId, entry.playerId, i + 1, entry.score]
        );
      }
    });
  }

  async getLeaderboard(
    leaderboardType: string,
    scope: string,
    scopeId: number | null,
    limit: number = 100,
    offset: number = 0
  ): Promise<PlayerRankRow[]> {
    const rows = await this.query<any>(
      `SELECT l.player_id, l.rank, l.score, p.username
       FROM leaderboards l
       JOIN players p ON l.player_id = p.id
       WHERE l.leaderboard_type = ? AND l.scope = ? AND l.scope_id <=> ?
       ORDER BY l.rank LIMIT ? OFFSET ?`,
      [leaderboardType, scope, scopeId, limit, offset]
    );
    return rows;
  }

  async getPlayerRank(
    leaderboardType: string,
    scope: string,
    scopeId: number | null,
    playerId: string
  ): Promise<{rank: number, score: number} | null> {
    const rows = await this.query<LeaderboardRow>(
      'SELECT * FROM leaderboards WHERE leaderboard_type = ? AND scope = ? AND scope_id <=> ? AND player_id = ?',
      [leaderboardType, scope, scopeId, playerId]
    );
    if (rows[0]) {
      return { rank: rows[0].rank, score: rows[0].score };
    }
    return null;
  }

  async getPlayerRankWithContext(
    leaderboardType: string,
    scope: string,
    scopeId: number | null,
    playerId: string,
    contextSize: number = 10
  ): Promise<{
    player: PlayerRankRow | null;
    above: PlayerRankRow[];
    below: PlayerRankRow[];
  }> {
    const playerRank = await this.getPlayerRank(leaderboardType, scope, scopeId, playerId);
    if (!playerRank) return { player: null, above: [], below: [] };

    const playerInfo = await this.query<any>('SELECT username FROM players WHERE id = ?', [playerId]);
    const playerUsername = playerInfo[0]?.username || 'Unknown';

    const player: PlayerRankRow = {
      player_id: playerId,
      username: playerUsername,
      rank: playerRank.rank,
      score: playerRank.score
    };

    const offset = Math.max(0, playerRank.rank - contextSize - 1);
    const limit = contextSize * 2 + 1;

    const entries = await this.getLeaderboard(leaderboardType, scope, scopeId, limit, offset);

    const above = entries.filter(e => e.rank < playerRank.rank);
    const below = entries.filter(e => e.rank > playerRank.rank);

    return { player, above, below };
  }

  async getTopPlayers(leaderboardType: string, scope: string, scopeId: number | null, limit: number = 10): Promise<PlayerRankRow[]> {
    return this.getLeaderboard(leaderboardType, scope, scopeId, limit, 0);
  }

  // Snapshot management
  async createSnapshot(
    leaderboardType: string,
    scope: string,
    scopeId: number | null,
    data: any
  ): Promise<number> {
    const result = await this.execute(
      'INSERT INTO leaderboard_snapshots (leaderboard_type, scope, scope_id, data) VALUES (?, ?, ?, ?)',
      [leaderboardType, scope, scopeId, JSON.stringify(data)]
    );
    return result.insertId;
  }

  async getLatestSnapshot(
    leaderboardType: string,
    scope: string,
    scopeId: number | null
  ): Promise<LeaderboardSnapshotRow | null> {
    const rows = await this.query<LeaderboardSnapshotRow>(
      'SELECT * FROM leaderboard_snapshots WHERE leaderboard_type = ? AND scope = ? AND scope_id <=> ? ORDER BY created_at DESC LIMIT 1',
      [leaderboardType, scope, scopeId]
    );
    if (rows[0]) {
      rows[0].data = JSON.parse(rows[0].data);
    }
    return rows[0] || null;
  }

  async getSnapshotHistory(
    leaderboardType: string,
    scope: string,
    scopeId: number | null,
    limit: number = 10
  ): Promise<LeaderboardSnapshotRow[]> {
    const rows = await this.query<LeaderboardSnapshotRow>(
      'SELECT * FROM leaderboard_snapshots WHERE leaderboard_type = ? AND scope = ? AND scope_id <=> ? ORDER BY created_at DESC LIMIT ?',
      [leaderboardType, scope, scopeId, limit]
    );
    for (const row of rows) {
      row.data = JSON.parse(row.data as string);
    }
    return rows;
  }

  // Batch refresh operations
  async refreshGlobalLeaderboards(): Promise<void> {
    // Trophies leaderboard
    const trophyRows = await this.query<any>(
      'SELECT id as player_id, trophies as score FROM players WHERE is_banned = FALSE ORDER BY trophies DESC LIMIT 10000'
    );
    await this.refreshLeaderboard('trophies', 'global', null, trophyRows);

    // Wins leaderboard (would need to compute from battles)
    const winRows = await this.query<any>(
      `SELECT p.id as player_id, COUNT(b.battle_id) as score
       FROM players p
       LEFT JOIN battles b ON (b.player_1_id = p.id OR b.player_2_id = p.id) AND b.winner_team = 1
       WHERE p.is_banned = FALSE
       GROUP BY p.id
       ORDER BY score DESC
       LIMIT 10000`
    );
    await this.refreshLeaderboard('wins', 'global', null, winRows);

    // Donations leaderboard
    const donationRows = await this.query<any>(
      `SELECT player_id, donations as score FROM clan_members ORDER BY donations DESC LIMIT 10000`
    );
    await this.refreshLeaderboard('donations', 'global', null, donationRows);
  }

  async refreshCountryLeaderboards(): Promise<void> {
    // Would need country data on players
    // For now, skip - requires player country field
    logger.info('Country leaderboards refresh skipped - country data not available');
  }

  async refreshClanLeaderboards(): Promise<void> {
    // Clan trophies
    const clanRows = await this.query<any>(
      'SELECT clan_id as scope_id, trophies as score FROM clans ORDER BY trophies DESC LIMIT 10000'
    );
    await this.refreshLeaderboard('trophies', 'clan', null, clanRows.map(r => ({ playerId: r.scope_id.toString(), score: r.score })));

    // Clan war wins
    const warRows = await this.query<any>(
      'SELECT clan_id as scope_id, war_wins as score FROM clans ORDER BY war_wins DESC LIMIT 10000'
    );
    await this.refreshLeaderboard('war_wins', 'clan', null, warRows.map(r => ({ playerId: r.scope_id.toString(), score: r.score })));
  }

  async refreshFriendsLeaderboards(playerId: string, friendIds: string[]): Promise<void> {
    if (friendIds.length === 0) return;

    const placeholders = friendIds.map(() => '?').join(',');
    const playerAndFriends = [playerId, ...friendIds];

    // Trophies
    const trophyRows = await this.query<any>(
      `SELECT id as player_id, trophies as score FROM players WHERE id IN (${placeholders}) ORDER BY trophies DESC`,
      playerAndFriends
    );
    await this.refreshLeaderboard('trophies', 'friends', parseInt(playerId.slice(0, 8), 16), trophyRows); // Use playerId hash as scope_id

    // Wins
    const winRows = await this.query<any>(
      `SELECT p.id as player_id, COUNT(b.battle_id) as score
       FROM players p
       LEFT JOIN battles b ON (b.player_1_id = p.id OR b.player_2_id = p.id) AND b.winner_team = 1
       WHERE p.id IN (${placeholders})
       GROUP BY p.id
       ORDER BY score DESC`,
      playerAndFriends
    );
    await this.refreshLeaderboard('wins', 'friends', parseInt(playerId.slice(0, 8), 16), winRows);
  }

  // Utility: Get leaderboard config
  getLeaderboardTypes(): string[] {
    return ['trophies', 'wins', 'win_streak', 'donations', 'war_wins'];
  }

  getScopes(): string[] {
    return ['global', 'country', 'clan', 'friends'];
  }
}