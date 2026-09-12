import { BaseRepository } from './BaseRepository';
import { Database, db } from './Database';
import { logger } from '../utils/logger';

export interface TournamentRow {
  tournament_id: number;
  name: string;
  description: string | null;
  tournament_type: string;
  entry_cost_gems: number;
  entry_cost_gold: number;
  max_losses: number;
  max_wins: number;
  card_level_cap: number;
  allowed_cards: number[] | null;
  banned_cards: number[] | null;
  starts_at: Date;
  ends_at: Date;
  rewards: any;
  is_enabled: boolean;
  max_participants: number;
  created_at: Date;
  updated_at: Date;
}

export interface TournamentEntryRow {
  entry_id: number;
  tournament_id: number;
  player_id: string;
  deck_id: string | null;
  wins: number;
  losses: number;
  status: string;
  started_at: Date;
  completed_at: Date | null;
  final_rewards: any | null;
  draft_picks: number[] | null;
}

export interface TournamentRewardRow {
  reward_id: number;
  tournament_id: number;
  min_wins: number;
  max_wins: number;
  rewards: any;
}

export interface TournamentQueueRow {
  queue_id: number;
  tournament_id: number;
  player_id: string;
  entry_id: number;
  trophies_at_entry: number;
  joined_at: Date;
}

export class TournamentRepository extends BaseRepository {
  constructor(database: Database = db) {
    super(database);
  }

  // Tournaments
  async createTournament(tournament: Omit<TournamentRow, 'tournament_id' | 'created_at' | 'updated_at'>): Promise<number> {
    const result = await this.execute(
      `INSERT INTO tournaments (name, description, tournament_type, entry_cost_gems, entry_cost_gold,
        max_losses, max_wins, card_level_cap, allowed_cards, banned_cards,
        starts_at, ends_at, rewards, is_enabled, max_participants)
       VALUES (?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?)`,
      [
        tournament.name,
        tournament.description,
        tournament.tournament_type,
        tournament.entry_cost_gems,
        tournament.entry_cost_gold,
        tournament.max_losses,
        tournament.max_wins,
        tournament.card_level_cap,
        tournament.allowed_cards ? JSON.stringify(tournament.allowed_cards) : null,
        tournament.banned_cards ? JSON.stringify(tournament.banned_cards) : null,
        tournament.starts_at,
        tournament.ends_at,
        JSON.stringify(tournament.rewards),
        tournament.is_enabled,
        tournament.max_participants
      ]
    );
    return result.insertId;
  }

  async getTournament(tournamentId: number): Promise<TournamentRow | null> {
    const rows = await this.query<TournamentRow>('SELECT * FROM tournaments WHERE tournament_id = ?', [tournamentId]);
    if (rows[0]) {
      rows[0].allowed_cards = rows[0].allowed_cards ? JSON.parse(rows[0].allowed_cards as unknown as string) : null;
      rows[0].banned_cards = rows[0].banned_cards ? JSON.parse(rows[0].banned_cards as unknown as string) : null;
      rows[0].rewards = JSON.parse(rows[0].rewards as unknown as string);
    }
    return rows[0] || null;
  }

  async getActiveTournaments(): Promise<TournamentRow[]> {
    const now = new Date();
    const rows = await this.query<TournamentRow>(
      'SELECT * FROM tournaments WHERE is_enabled = TRUE AND starts_at <= ? AND ends_at >= ? ORDER BY starts_at',
      [now, now]
    );
    for (const row of rows) {
      row.allowed_cards = row.allowed_cards ? JSON.parse(row.allowed_cards as unknown as string) : null;
      row.banned_cards = row.banned_cards ? JSON.parse(row.banned_cards as unknown as string) : null;
      row.rewards = JSON.parse(row.rewards as unknown as string);
    }
    return rows;
  }

  async getUpcomingTournaments(limit: number = 10): Promise<TournamentRow[]> {
    const now = new Date();
    const rows = await this.query<TournamentRow>(
      'SELECT * FROM tournaments WHERE is_enabled = TRUE AND starts_at > ? ORDER BY starts_at LIMIT ?',
      [now, limit]
    );
    for (const row of rows) {
      row.allowed_cards = row.allowed_cards ? JSON.parse(row.allowed_cards as unknown as string) : null;
      row.banned_cards = row.banned_cards ? JSON.parse(row.banned_cards as unknown as string) : null;
      row.rewards = JSON.parse(row.rewards as unknown as string);
    }
    return rows;
  }

  async getTournamentsByType(type: string): Promise<TournamentRow[]> {
    const rows = await this.query<TournamentRow>(
      'SELECT * FROM tournaments WHERE tournament_type = ? AND is_enabled = TRUE ORDER BY starts_at',
      [type]
    );
    for (const row of rows) {
      row.allowed_cards = row.allowed_cards ? JSON.parse(row.allowed_cards as unknown as string) : null;
      row.banned_cards = row.banned_cards ? JSON.parse(row.banned_cards as unknown as string) : null;
      row.rewards = JSON.parse(row.rewards as unknown as string);
    }
    return rows;
  }

  async updateTournament(tournamentId: number, data: Partial<TournamentRow>): Promise<void> {
    const { set, params } = this.buildUpdateClause({
      ...data,
      allowed_cards: data.allowed_cards ? JSON.stringify(data.allowed_cards) : undefined,
      banned_cards: data.banned_cards ? JSON.stringify(data.banned_cards) : undefined,
      rewards: data.rewards ? JSON.stringify(data.rewards) : undefined
    });
    if (!set) return;
    params.push(tournamentId);
    await this.execute(`UPDATE tournaments SET ${set}, updated_at = NOW() WHERE tournament_id = ?`, params);
  }

  // Tournament Entries
  async createEntry(entry: Omit<TournamentEntryRow, 'entry_id' | 'started_at'>): Promise<number> {
    const result = await this.execute(
      `INSERT INTO tournament_entries (tournament_id, player_id, deck_id, wins, losses, status, draft_picks)
       VALUES (?, ?, ?, ?, ?, ?, ?)`,
      [
        entry.tournament_id,
        entry.player_id,
        entry.deck_id,
        entry.wins || 0,
        entry.losses || 0,
        entry.status || 'active',
        entry.draft_picks ? JSON.stringify(entry.draft_picks) : null
      ]
    );
    return result.insertId;
  }

  async getEntry(entryId: number): Promise<TournamentEntryRow | null> {
    const rows = await this.query<TournamentEntryRow>('SELECT * FROM tournament_entries WHERE entry_id = ?', [entryId]);
    if (rows[0]) {
      rows[0].final_rewards = rows[0].final_rewards ? JSON.parse(rows[0].final_rewards as unknown as string) : null;
      rows[0].draft_picks = rows[0].draft_picks ? JSON.parse(rows[0].draft_picks as unknown as string) : null;
    }
    return rows[0] || null;
  }

  async getPlayerEntry(tournamentId: number, playerId: string): Promise<TournamentEntryRow | null> {
    const rows = await this.query<TournamentEntryRow>(
      'SELECT * FROM tournament_entries WHERE tournament_id = ? AND player_id = ?',
      [tournamentId, playerId]
    );
    if (rows[0]) {
      rows[0].final_rewards = rows[0].final_rewards ? JSON.parse(rows[0].final_rewards as unknown as string) : null;
      rows[0].draft_picks = rows[0].draft_picks ? JSON.parse(rows[0].draft_picks as unknown as string) : null;
    }
    return rows[0] || null;
  }

  async getTournamentEntries(tournamentId: number, status?: string): Promise<TournamentEntryRow[]> {
    let sql = 'SELECT * FROM tournament_entries WHERE tournament_id = ?';
    const params: any[] = [tournamentId];

    if (status) {
      sql += ' AND status = ?';
      params.push(status);
    }

    sql += ' ORDER BY wins DESC, losses ASC, started_at';
    const rows = await this.query<TournamentEntryRow>(sql, params);

    for (const row of rows) {
      row.final_rewards = row.final_rewards ? JSON.parse(row.final_rewards as unknown as string) : null;
      row.draft_picks = row.draft_picks ? JSON.parse(row.draft_picks as unknown as string) : null;
    }

    return rows;
  }

  async updateEntry(entryId: number, data: Partial<TournamentEntryRow>): Promise<void> {
    const { set, params } = this.buildUpdateClause({
      ...data,
      final_rewards: data.final_rewards ? JSON.stringify(data.final_rewards) : undefined,
      draft_picks: data.draft_picks ? JSON.stringify(data.draft_picks) : undefined
    });
    if (!set) return;
    params.push(entryId);
    await this.execute(`UPDATE tournament_entries SET ${set} WHERE entry_id = ?`, params);
  }

  async recordWin(entryId: number): Promise<TournamentEntryRow | null> {
    await this.execute(
      'UPDATE tournament_entries SET wins = wins + 1 WHERE entry_id = ?',
      [entryId]
    );
    return this.getEntry(entryId);
  }

  async recordLoss(entryId: number): Promise<TournamentEntryRow | null> {
    await this.execute(
      'UPDATE tournament_entries SET losses = losses + 1 WHERE entry_id = ?',
      [entryId]
    );
    return this.getEntry(entryId);
  }

  async completeEntry(entryId: number, finalRewards: any): Promise<void> {
    await this.execute(
      'UPDATE tournament_entries SET status = "completed", completed_at = NOW(), final_rewards = ? WHERE entry_id = ?',
      [JSON.stringify(finalRewards), entryId]
    );
  }

  async retireEntry(entryId: number): Promise<void> {
    await this.execute(
      'UPDATE tournament_entries SET status = "retired", completed_at = NOW() WHERE entry_id = ?',
      [entryId]
    );
  }

  // Tournament Rewards
  async createRewardTier(reward: Omit<TournamentRewardRow, 'reward_id'>): Promise<number> {
    const result = await this.execute(
      'INSERT INTO tournament_rewards (tournament_id, min_wins, max_wins, rewards) VALUES (?, ?, ?, ?)',
      [reward.tournament_id, reward.min_wins, reward.max_wins, JSON.stringify(reward.rewards)]
    );
    return result.insertId;
  }

  async getRewardTiers(tournamentId: number): Promise<TournamentRewardRow[]> {
    const rows = await this.query<TournamentRewardRow>(
      'SELECT * FROM tournament_rewards WHERE tournament_id = ? ORDER BY min_wins',
      [tournamentId]
    );
    for (const row of rows) {
      row.rewards = JSON.parse(row.rewards as unknown as string);
    }
    return rows;
  }

  async getRewardsForWins(tournamentId: number, wins: number): Promise<any | null> {
    const rows = await this.query<TournamentRewardRow>(
      'SELECT * FROM tournament_rewards WHERE tournament_id = ? AND min_wins <= ? AND max_wins >= ? ORDER BY min_wins DESC LIMIT 1',
      [tournamentId, wins, wins]
    );
    return rows[0] ? JSON.parse(rows[0].rewards as unknown as string) : null;
  }

  // Tournament Queue (for matchmaking)
  async joinQueue(queue: Omit<TournamentQueueRow, 'queue_id' | 'joined_at'>): Promise<number> {
    const result = await this.execute(
      'INSERT INTO tournament_queue (tournament_id, player_id, entry_id, trophies_at_entry) VALUES (?, ?, ?, ?)',
      [queue.tournament_id, queue.player_id, queue.entry_id, queue.trophies_at_entry]
    );
    return result.insertId;
  }

  async leaveQueue(tournamentId: number, playerId: string): Promise<void> {
    await this.execute('DELETE FROM tournament_queue WHERE tournament_id = ? AND player_id = ?', [tournamentId, playerId]);
  }

  async getQueue(tournamentId: number): Promise<TournamentQueueRow[]> {
    return this.query<TournamentQueueRow>(
      'SELECT * FROM tournament_queue WHERE tournament_id = ? ORDER BY trophies_at_entry DESC, joined_at',
      [tournamentId]
    );
  }

  async findMatch(tournamentId: number, playerId: string, trophyRange: number): Promise<TournamentQueueRow | null> {
    const playerEntry = await this.getPlayerEntry(tournamentId, playerId);
    if (!playerEntry) return null;

    const rows = await this.query<TournamentQueueRow>(
      `SELECT * FROM tournament_queue 
       WHERE tournament_id = ? AND player_id != ? 
       AND trophies_at_entry BETWEEN ? AND ?
       ORDER BY ABS(trophies_at_entry - ?) ASC, joined_at
       LIMIT 1`,
      [tournamentId, playerId, playerEntry.wins, playerEntry.losses, playerEntry.wins]
    );
    return rows[0] || null;
  }

  async clearQueue(tournamentId: number): Promise<void> {
    await this.execute('DELETE FROM tournament_queue WHERE tournament_id = ?', [tournamentId]);
  }

  // Draft Mode
  async saveDraftPicks(entryId: number, picks: number[]): Promise<void> {
    await this.execute(
      'UPDATE tournament_entries SET draft_picks = ? WHERE entry_id = ?',
      [JSON.stringify(picks), entryId]
    );
  }

  async getDraftPicks(entryId: number): Promise<number[]> {
    const entry = await this.getEntry(entryId);
    return entry?.draft_picks || [];
  }

  // Statistics
  async getTournamentStats(tournamentId: number): Promise<{
    totalEntries: number;
    activeEntries: number;
    completedEntries: number;
    avgWins: number;
    maxWins: number;
  }> {
    const rows = await this.query<any>(
      `SELECT 
        COUNT(*) as totalEntries,
        SUM(CASE WHEN status = 'active' THEN 1 ELSE 0 END) as activeEntries,
        SUM(CASE WHEN status = 'completed' THEN 1 ELSE 0 END) as completedEntries,
        AVG(wins) as avgWins,
        MAX(wins) as maxWins
       FROM tournament_entries WHERE tournament_id = ?`,
      [tournamentId]
    );
    return rows[0] || { totalEntries: 0, activeEntries: 0, completedEntries: 0, avgWins: 0, maxWins: 0 };
  }

  async getPlayerTournamentHistory(playerId: string): Promise<Array<TournamentEntryRow & {tournament: TournamentRow}>> {
    const rows = await this.query<any>(
      `SELECT te.*, t.name, t.tournament_type, t.starts_at, t.ends_at
       FROM tournament_entries te
       JOIN tournaments t ON te.tournament_id = t.tournament_id
       WHERE te.player_id = ?
       ORDER BY te.started_at DESC`,
      [playerId]
    );
    return rows;
  }
}