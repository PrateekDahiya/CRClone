import { Database, db } from '../persistence/Database';
import { logger } from '../utils/logger';
import { SeasonRepository, SeasonRow, PlayerSeasonRow, CrownMilestoneRow } from '../persistence/SeasonRepository';
import { PlayerService } from './PlayerService';

export interface Season {
  seasonId: number;
  nameKey: string;
  theme: string;
  startsAt: Date;
  endsAt: Date;
  passTiers: number;
  freeTrackRewards: any;
  paidTrackRewards: any;
  passPriceGems: number;
  isActive: boolean;
}

export interface PlayerSeasonProgress {
  playerId: string;
  seasonId: number;
  tierReached: number;
  isPaidPass: boolean;
  paidClaimedTiers: number[];
  freeClaimedTiers: number[];
  crownsEarned: number;
  lastCrownAt: Date | null;
}

export class SeasonService {
  private db: Database;
  private seasonRepo: SeasonRepository;
  private playerService: PlayerService;

  constructor(database: Database = db, playerService?: PlayerService) {
    this.db = database;
    this.seasonRepo = new SeasonRepository(database);
    this.playerService = playerService || new PlayerService(database);
  }

  private mapSeason(row: SeasonRow): Season {
    return {
      seasonId: row.season_id,
      nameKey: row.name_key || '',
      theme: row.theme || '',
      startsAt: row.starts_at,
      endsAt: row.ends_at,
      passTiers: row.pass_tiers,
      freeTrackRewards: row.free_track_rewards,
      paidTrackRewards: row.paid_track_rewards,
      passPriceGems: row.pass_price_gems,
      isActive: row.is_active
    };
  }

  async getCurrentSeason(): Promise<Season | null> {
    const season = await this.seasonRepo.getCurrentSeason();
    return season ? this.mapSeason(season) : null;
  }

  async getActiveSeason(): Promise<Season | null> {
    const season = await this.seasonRepo.getActiveSeason();
    return season ? this.mapSeason(season) : null;
  }

  async getUpcomingSeason(): Promise<Season | null> {
    const season = await this.seasonRepo.getUpcomingSeason();
    return season ? this.mapSeason(season) : null;
  }

  async getSeason(seasonId: number): Promise<Season | null> {
    const season = await this.seasonRepo.getSeason(seasonId);
    return season ? this.mapSeason(season) : null;
  }

  async getAllSeasons(): Promise<Season[]> {
    return (await this.seasonRepo.getAllSeasons()).map(r => this.mapSeason(r));
  }

  async getPlayerSeason(playerId: string, seasonId?: number): Promise<PlayerSeasonProgress | null> {
    if (!seasonId) {
      const currentSeason = await this.getCurrentSeason();
      if (!currentSeason) return null;
      seasonId = currentSeason.seasonId;
    }
    const ps = await this.seasonRepo.getPlayerSeason(playerId, seasonId);
    if (!ps) return null;
    return {
      playerId: ps.player_id,
      seasonId: ps.season_id,
      tierReached: ps.tier_reached,
      isPaidPass: ps.is_paid_pass,
      paidClaimedTiers: ps.paid_claimed_tiers,
      freeClaimedTiers: ps.free_claimed_tiers,
      crownsEarned: ps.crowns_earned,
      lastCrownAt: ps.last_crown_at
    };
  }

  async getPlayerSeasons(playerId: string): Promise<PlayerSeasonProgress[]> {
    return (await this.seasonRepo.getPlayerSeasons(playerId)).map(ps => ({
      playerId: ps.player_id,
      seasonId: ps.season_id,
      tierReached: ps.tier_reached,
      isPaidPass: ps.is_paid_pass,
      paidClaimedTiers: ps.paid_claimed_tiers,
      freeClaimedTiers: ps.free_claimed_tiers,
      crownsEarned: ps.crowns_earned,
      lastCrownAt: ps.last_crown_at
    }));
  }

  async initializePlayerSeason(playerId: string, seasonId: number): Promise<void> {
    await this.seasonRepo.createPlayerSeason(playerId, seasonId);
  }

  async addCrowns(playerId: string, crowns: number, seasonId?: number): Promise<PlayerSeasonProgress | null> {
    if (!seasonId) {
      const currentSeason = await this.getCurrentSeason();
      if (!currentSeason) return null;
      seasonId = currentSeason.seasonId;
    }
    const result = await this.seasonRepo.addCrowns(playerId, seasonId, crowns);
    if (!result) return null;
    return {
      playerId: result.player_id,
      seasonId: result.season_id,
      tierReached: result.tier_reached,
      isPaidPass: result.is_paid_pass,
      paidClaimedTiers: result.paid_claimed_tiers,
      freeClaimedTiers: result.free_claimed_tiers,
      crownsEarned: result.crowns_earned,
      lastCrownAt: result.last_crown_at
    };
  }

  async claimFreeReward(playerId: string, tier: number, seasonId?: number): Promise<any[] | null> {
    if (!seasonId) {
      const currentSeason = await this.getCurrentSeason();
      if (!currentSeason) return null;
      seasonId = currentSeason.seasonId;
    }
    const success = await this.seasonRepo.claimFreeReward(playerId, seasonId, tier);
    if (!success) return null;
    const season = await this.getSeason(seasonId);
    if (!season) return null;
    const rewards = season.freeTrackRewards[tier] || [];
    await this.grantSeasonRewards(playerId, rewards);
    return rewards;
  }

  async claimPaidReward(playerId: string, tier: number, seasonId?: number): Promise<any[] | null> {
    if (!seasonId) {
      const currentSeason = await this.getCurrentSeason();
      if (!currentSeason) return null;
      seasonId = currentSeason.seasonId;
    }
    const success = await this.seasonRepo.claimPaidReward(playerId, seasonId, tier);
    if (!success) return null;
    const season = await this.getSeason(seasonId);
    if (!season) return null;
    const rewards = season.paidTrackRewards[tier] || [];
    await this.grantSeasonRewards(playerId, rewards);
    return rewards;
  }

  async claimCrownMilestone(playerId: string, milestoneId: number, seasonId?: number): Promise<any[] | null> {
    if (!seasonId) {
      const currentSeason = await this.getCurrentSeason();
      if (!currentSeason) return null;
      seasonId = currentSeason.seasonId;
    }
    const ps = await this.seasonRepo.getPlayerSeason(playerId, seasonId);
    if (!ps) return null;
    const milestones = await this.seasonRepo.getCrownMilestones(seasonId);
    const milestone = milestones.find(m => m.milestone_id === milestoneId);
    if (!milestone) return null;
    if (ps.crowns_earned < milestone.crowns_required) return null;
    if (milestone.is_paid_only && !ps.is_paid_pass) return null;
    await this.grantSeasonRewards(playerId, milestone.rewards);
    return milestone.rewards;
  }

  async purchaseBattlePass(playerId: string, seasonId?: number): Promise<boolean> {
    if (!seasonId) {
      const currentSeason = await this.getCurrentSeason();
      if (!currentSeason) return false;
      seasonId = currentSeason.seasonId;
    }
    const season = await this.getSeason(seasonId);
    if (!season) return false;
    const player = await this.playerService.getPlayerById(playerId);
    if (!player || player.gems < season.passPriceGems) return false;
    await this.playerService.addGems(playerId, -season.passPriceGems);
    return this.seasonRepo.purchasePass(playerId, seasonId);
  }

  private async grantSeasonRewards(playerId: string, rewards: any[]): Promise<void> {
    for (const reward of rewards) {
      switch (reward.type) {
        case 'gold': if (reward.amount) await this.playerService.addGold(playerId, reward.amount); break;
        case 'gems': if (reward.amount) await this.playerService.addGems(playerId, reward.amount); break;
        case 'xp': if (reward.amount) await this.playerService.addExperience(playerId, reward.amount); break;
        case 'chest': if (reward.chestId) await this.grantChest(playerId, reward.chestId); break;
        case 'wild_card': if (reward.rarity && reward.count) await this.grantWildCard(playerId, reward.rarity, reward.count); break;
        case 'emote': logger.info('Emote granted', { playerId, emoteId: reward.emoteId }); break;
        case 'cosmetic': logger.info('Cosmetic granted', { playerId, cosmeticId: reward.cosmeticId }); break;
      }
    }
  }

  private async grantChest(playerId: string, chestTypeId: number): Promise<void> {
    const slots = await this.db.query('SELECT * FROM player_chests WHERE player_id = ? ORDER BY slot_index LIMIT 5', [playerId]);
    let targetSlot = -1;
    for (let i = 0; i < 4; i++) if (!slots.find(s => s.slot_index === i)) { targetSlot = i; break; }
    if (targetSlot === -1) targetSlot = 4;
    await this.db.execute(`INSERT INTO player_chests (player_id, chest_type_id, slot_index, status) VALUES (?, ?, ?, ?)`, [playerId, chestTypeId, targetSlot, targetSlot < 4 ? 'locked' : 'empty']);
  }

  private async grantWildCard(playerId: string, rarity: string, count: number): Promise<void> {
    const wildCardIds = { common: 99000001, rare: 99000002, epic: 99000003, legendary: 99000004, champion: 99000005 };
    const cardId = wildCardIds[rarity as keyof typeof wildCardIds] || 99000001;
    await this.db.execute(`INSERT INTO player_cards (player_id, card_id, count, level) VALUES (?, ?, ?, 1) ON DUPLICATE KEY UPDATE count = count + ?`, [playerId, cardId, count, count]);
  }

  async getSeasonLeaderboard(seasonId: number, limit: number = 100): Promise<any[]> { return this.seasonRepo.getSeasonLeaderboard(seasonId, limit); }
  async getPlayerSeasonRank(playerId: string, seasonId: number): Promise<number> { return this.seasonRepo.getPlayerSeasonRank(playerId, seasonId); }
  async createSeason(season: Omit<Season, 'seasonId'>): Promise<number> { return this.seasonRepo.createSeason({ season_id: Date.now(), name_key: season.nameKey, theme: season.theme, starts_at: season.startsAt, ends_at: season.endsAt, pass_tiers: season.passTiers, free_track_rewards: season.freeTrackRewards, paid_track_rewards: season.paidTrackRewards, pass_price_gems: season.passPriceGems, is_active: season.isActive }); }
  async activateSeason(seasonId: number): Promise<void> { await this.seasonRepo.activateSeason(seasonId); }
  async addCrownMilestone(seasonId: number, crownsRequired: number, rewards: any[], isPaidOnly: boolean = false): Promise<number> { return this.seasonRepo.createCrownMilestone({ season_id: seasonId, crowns_required: crownsRequired, rewards, is_paid_only: isPaidOnly }); }
}