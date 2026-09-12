import { BaseRepository } from './BaseRepository';
import { Database, db } from './Database';
import { logger } from '../utils/logger';

export interface SeasonRow {
  season_id: number;
  name_key: string | null;
  theme: string | null;
  starts_at: Date;
  ends_at: Date;
  pass_tiers: number;
  free_track_rewards: any;
  paid_track_rewards: any;
  pass_price_gems: number;
  is_active: boolean;
  created_at: Date;
  updated_at: Date;
}

export interface PlayerSeasonRow {
  player_id: string;
  season_id: number;
  tier_reached: number;
  is_paid_pass: boolean;
  paid_claimed_tiers: number[];
  free_claimed_tiers: number[];
  crowns_earned: number;
  last_crown_at: Date | null;
  updated_at: Date;
}

export interface CrownMilestoneRow {
  milestone_id: number;
  season_id: number;
  crowns_required: number;
  rewards: any;
  is_paid_only: boolean;
}

export class SeasonRepository extends BaseRepository {
  constructor(database: Database = db) {
    super(database);
  }

  // Seasons
  async createSeason(season: Omit<SeasonRow, 'season_id' | 'created_at' | 'updated_at'> & { season_id: number }): Promise<number> {
    await this.execute(
      `INSERT INTO seasons (season_id, name_key, theme, starts_at, ends_at, pass_tiers, free_track_rewards, paid_track_rewards, pass_price_gems, is_active)
       VALUES (?, ?, ?, ?, ?, ?, ?, ?, ?, ?)`,
      [
        season.season_id,
        season.name_key,
        season.theme,
        season.starts_at,
        season.ends_at,
        season.pass_tiers,
        JSON.stringify(season.free_track_rewards),
        JSON.stringify(season.paid_track_rewards),
        season.pass_price_gems,
        season.is_active
      ]
    );
    return season.season_id;
  }

  async getSeason(seasonId: number): Promise<SeasonRow | null> {
    const rows = await this.query<SeasonRow>('SELECT * FROM seasons WHERE season_id = ?', [seasonId]);
    if (rows[0]) {
      rows[0].free_track_rewards = JSON.parse(rows[0].free_track_rewards);
      rows[0].paid_track_rewards = JSON.parse(rows[0].paid_track_rewards);
    }
    return rows[0] || null;
  }

  async getActiveSeason(): Promise<SeasonRow | null> {
    const rows = await this.query<SeasonRow>(
      'SELECT * FROM seasons WHERE is_active = TRUE AND starts_at <= NOW() AND ends_at >= NOW() LIMIT 1'
    );
    if (rows[0]) {
      rows[0].free_track_rewards = JSON.parse(rows[0].free_track_rewards);
      rows[0].paid_track_rewards = JSON.parse(rows[0].paid_track_rewards);
    }
    return rows[0] || null;
  }

  async getCurrentSeason(): Promise<SeasonRow | null> {
    const rows = await this.query<SeasonRow>(
      'SELECT * FROM seasons WHERE starts_at <= NOW() AND ends_at >= NOW() ORDER BY starts_at DESC LIMIT 1'
    );
    if (rows[0]) {
      rows[0].free_track_rewards = JSON.parse(rows[0].free_track_rewards);
      rows[0].paid_track_rewards = JSON.parse(rows[0].paid_track_rewards);
    }
    return rows[0] || null;
  }

  async getUpcomingSeason(): Promise<SeasonRow | null> {
    const rows = await this.query<SeasonRow>(
      'SELECT * FROM seasons WHERE starts_at > NOW() ORDER BY starts_at ASC LIMIT 1'
    );
    if (rows[0]) {
      rows[0].free_track_rewards = JSON.parse(rows[0].free_track_rewards);
      rows[0].paid_track_rewards = JSON.parse(rows[0].paid_track_rewards);
    }
    return rows[0] || null;
  }

  async getPastSeasons(limit: number = 10): Promise<SeasonRow[]> {
    const rows = await this.query<SeasonRow>(
      'SELECT * FROM seasons WHERE ends_at < NOW() ORDER BY ends_at DESC LIMIT ?',
      [limit]
    );
    for (const row of rows) {
      row.free_track_rewards = JSON.parse(row.free_track_rewards as string);
      row.paid_track_rewards = JSON.parse(row.paid_track_rewards as string);
    }
    return rows;
  }

  async getAllSeasons(): Promise<SeasonRow[]> {
    const rows = await this.query<SeasonRow>('SELECT * FROM seasons ORDER BY season_id');
    for (const row of rows) {
      row.free_track_rewards = JSON.parse(row.free_track_rewards as string);
      row.paid_track_rewards = JSON.parse(row.paid_track_rewards as string);
    }
    return rows;
  }

  async updateSeason(seasonId: number, data: Partial<SeasonRow>): Promise<void> {
    const { set, params } = this.buildUpdateClause({
      ...data,
      free_track_rewards: data.free_track_rewards ? JSON.stringify(data.free_track_rewards) : undefined,
      paid_track_rewards: data.paid_track_rewards ? JSON.stringify(data.paid_track_rewards) : undefined
    });
    if (!set) return;
    params.push(seasonId);
    await this.execute(`UPDATE seasons SET ${set}, updated_at = NOW() WHERE season_id = ?`, params);
  }

  async activateSeason(seasonId: number): Promise<void> {
    await this.transaction(async (conn) => {
      await conn.execute('UPDATE seasons SET is_active = FALSE WHERE is_active = TRUE');
      await conn.execute('UPDATE seasons SET is_active = TRUE WHERE season_id = ?', [seasonId]);
    });
  }

  // Player Season Progress
  async getPlayerSeason(playerId: string, seasonId: number): Promise<PlayerSeasonRow | null> {
    const rows = await this.query<PlayerSeasonRow>(
      'SELECT * FROM player_seasons WHERE player_id = ? AND season_id = ?',
      [playerId, seasonId]
    );
    if (rows[0]) {
      rows[0].paid_claimed_tiers = JSON.parse((rows[0].paid_claimed_tiers as unknown as string) || '[]');
      rows[0].free_claimed_tiers = JSON.parse((rows[0].free_claimed_tiers as unknown as string) || '[]');
    }
    return rows[0] || null;
  }

  async getPlayerSeasons(playerId: string): Promise<PlayerSeasonRow[]> {
    const rows = await this.query<PlayerSeasonRow>(
      'SELECT * FROM player_seasons WHERE player_id = ? ORDER BY season_id DESC',
      [playerId]
    );
    for (const row of rows) {
      row.paid_claimed_tiers = JSON.parse((row.paid_claimed_tiers as unknown as string) || '[]');
      row.free_claimed_tiers = JSON.parse((row.free_claimed_tiers as unknown as string) || '[]');
    }
    return rows;
  }

  async createPlayerSeason(playerId: string, seasonId: number): Promise<void> {
    await this.execute(
      `INSERT IGNORE INTO player_seasons (player_id, season_id, tier_reached, is_paid_pass, paid_claimed_tiers, free_claimed_tiers, crowns_earned)
       VALUES (?, ?, 0, FALSE, '[]', '[]', 0)`,
      [playerId, seasonId]
    );
  }

  async updatePlayerSeason(playerId: string, seasonId: number, data: Partial<PlayerSeasonRow>): Promise<void> {
    const { set, params } = this.buildUpdateClause({
      ...data,
      paid_claimed_tiers: data.paid_claimed_tiers ? JSON.stringify(data.paid_claimed_tiers) : undefined,
      free_claimed_tiers: data.free_claimed_tiers ? JSON.stringify(data.free_claimed_tiers) : undefined
    });
    if (!set) return;
    params.push(playerId, seasonId);
    await this.execute(`UPDATE player_seasons SET ${set}, updated_at = NOW() WHERE player_id = ? AND season_id = ?`, params);
  }

  async addCrowns(playerId: string, seasonId: number, crowns: number): Promise<PlayerSeasonRow | null> {
    const playerSeason = await this.getPlayerSeason(playerId, seasonId);
    if (!playerSeason) return null;

    const newCrowns = playerSeason.crowns_earned + crowns;
    const newTier = this.calculateTierFromCrowns(newCrowns);

    await this.updatePlayerSeason(playerId, seasonId, {
      crowns_earned: newCrowns,
      tier_reached: Math.max(playerSeason.tier_reached, newTier),
      last_crown_at: new Date()
    });

    return this.getPlayerSeason(playerId, seasonId);
  }

  private calculateTierFromCrowns(crowns: number): number {
    // 10 crowns per tier (standard Clash Royale battle pass)
    return Math.floor(crowns / 10);
  }

  async claimFreeReward(playerId: string, seasonId: number, tier: number): Promise<boolean> {
    const playerSeason = await this.getPlayerSeason(playerId, seasonId);
    if (!playerSeason) return false;
    if (playerSeason.free_claimed_tiers.includes(tier)) return false;
    if (playerSeason.tier_reached < tier) return false;

    playerSeason.free_claimed_tiers.push(tier);
    await this.updatePlayerSeason(playerId, seasonId, {
      free_claimed_tiers: playerSeason.free_claimed_tiers
    });
    return true;
  }

  async claimPaidReward(playerId: string, seasonId: number, tier: number): Promise<boolean> {
    const playerSeason = await this.getPlayerSeason(playerId, seasonId);
    if (!playerSeason) return false;
    if (!playerSeason.is_paid_pass) return false;
    if (playerSeason.paid_claimed_tiers.includes(tier)) return false;
    if (playerSeason.tier_reached < tier) return false;

    playerSeason.paid_claimed_tiers.push(tier);
    await this.updatePlayerSeason(playerId, seasonId, {
      paid_claimed_tiers: playerSeason.paid_claimed_tiers
    });
    return true;
  }

  async purchasePass(playerId: string, seasonId: number): Promise<boolean> {
    const playerSeason = await this.getPlayerSeason(playerId, seasonId);
    if (!playerSeason) return false;
    if (playerSeason.is_paid_pass) return false;

    await this.updatePlayerSeason(playerId, seasonId, {
      is_paid_pass: true
    });
    return true;
  }

  // Crown Milestones
  async createCrownMilestone(milestone: Omit<CrownMilestoneRow, 'milestone_id'>): Promise<number> {
    const result = await this.execute(
      `INSERT INTO season_crown_milestones (season_id, crowns_required, rewards, is_paid_only)
       VALUES (?, ?, ?, ?)`,
      [milestone.season_id, milestone.crowns_required, JSON.stringify(milestone.rewards), milestone.is_paid_only]
    );
    return result.insertId;
  }

  async getCrownMilestones(seasonId: number): Promise<CrownMilestoneRow[]> {
    const rows = await this.query<CrownMilestoneRow>(
      'SELECT * FROM season_crown_milestones WHERE season_id = ? ORDER BY crowns_required',
      [seasonId]
    );
    for (const row of rows) {
      row.rewards = JSON.parse(row.rewards as string);
    }
    return rows;
  }

  async getPlayerClaimedMilestones(playerId: string, seasonId: number): Promise<number[]> {
    const playerSeason = await this.getPlayerSeason(playerId, seasonId);
    if (!playerSeason) return [];

    const milestones = await this.getCrownMilestones(seasonId);
    const claimed: number[] = [];

    for (const milestone of milestones) {
      if (playerSeason.crowns_earned >= milestone.crowns_required) {
        claimed.push(milestone.milestone_id);
      }
    }

    return claimed;
  }

  // Statistics
  async getSeasonLeaderboard(seasonId: number, limit: number = 100): Promise<Array<{playerId: string, crowns: number, tier: number, username: string}>> {
    const rows = await this.query<any>(
      `SELECT ps.player_id, ps.crowns_earned, ps.tier_reached, p.username
       FROM player_seasons ps
       JOIN players p ON ps.player_id = p.id
       WHERE ps.season_id = ?
       ORDER BY ps.crowns_earned DESC, ps.tier_reached DESC
       LIMIT ?`,
      [seasonId, limit]
    );
    return rows.map(r => ({
      playerId: r.player_id,
      crowns: r.crowns_earned,
      tier: r.tier_reached,
      username: r.username
    }));
  }

  async getPlayerSeasonRank(playerId: string, seasonId: number): Promise<number> {
    const rows = await this.query<{rank: number}>(
      `SELECT COUNT(*) + 1 as rank FROM player_seasons 
       WHERE season_id = ? AND crowns_earned > (SELECT crowns_earned FROM player_seasons WHERE player_id = ? AND season_id = ?)`,
      [seasonId, playerId, seasonId]
    );
    return rows[0]?.rank || 0;
  }
}