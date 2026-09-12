import { Database, db } from '../persistence/Database';
import { logger } from '../utils/logger';
import { TournamentRepository, TournamentRow, TournamentEntryRow, TournamentRewardRow } from '../persistence/TournamentRepository';
import { PlayerService } from './PlayerService';

export interface Tournament {
  tournamentId: number;
  name: string;
  description: string;
  tournamentType: 'classic' | 'grand' | 'draft' | '2v2' | 'custom';
  entryCostGems: number;
  entryCostGold: number;
  maxLosses: number;
  maxWins: number;
  cardLevelCap: number;
  allowedCards: number[] | null;
  bannedCards: number[] | null;
  startsAt: Date;
  endsAt: Date;
  rewards: any;
  isEnabled: boolean;
  maxParticipants: number;
}

export interface TournamentEntry {
  entryId: number;
  tournamentId: number;
  playerId: string;
  deckId: string | null;
  wins: number;
  losses: number;
  status: 'active' | 'completed' | 'retired' | 'disqualified';
  startedAt: Date;
  completedAt: Date | null;
  finalRewards: any | null;
  draftPicks: number[] | null;
}

export class TournamentService {
  private db: Database;
  private tournamentRepo: TournamentRepository;
  private playerService: PlayerService;

  constructor(database: Database = db, playerService?: PlayerService) {
    this.db = database;
    this.tournamentRepo = new TournamentRepository(database);
    this.playerService = playerService || new PlayerService(database);
  }

  private mapTournament(row: TournamentRow): Tournament {
    return {
      tournamentId: row.tournament_id,
      name: row.name,
      description: row.description || '',
      tournamentType: row.tournament_type as any,
      entryCostGems: row.entry_cost_gems,
      entryCostGold: row.entry_cost_gold,
      maxLosses: row.max_losses,
      maxWins: row.max_wins,
      cardLevelCap: row.card_level_cap,
      allowedCards: row.allowed_cards,
      bannedCards: row.banned_cards,
      startsAt: row.starts_at,
      endsAt: row.ends_at,
      rewards: row.rewards,
      isEnabled: row.is_enabled,
      maxParticipants: row.max_participants
    };
  }

  async getActiveTournaments(): Promise<Tournament[]> {
    return (await this.tournamentRepo.getActiveTournaments()).map(r => this.mapTournament(r));
  }

  async getUpcomingTournaments(limit: number = 10): Promise<Tournament[]> {
    return (await this.tournamentRepo.getUpcomingTournaments(limit)).map(r => this.mapTournament(r));
  }

  async getTournament(tournamentId: number): Promise<Tournament | null> {
    const t = await this.tournamentRepo.getTournament(tournamentId);
    return t ? this.mapTournament(t) : null;
  }

  async getTournamentsByType(type: string): Promise<Tournament[]> {
    return (await this.tournamentRepo.getTournamentsByType(type)).map(r => this.mapTournament(r));
  }

  async enterTournament(playerId: string, tournamentId: number, deckId?: string): Promise<TournamentEntry> {
    const tournament = await this.getTournament(tournamentId);
    if (!tournament) throw new Error('TOURNAMENT_NOT_FOUND');
    if (!tournament.isEnabled) throw new Error('TOURNAMENT_DISABLED');
    const now = new Date();
    if (now < tournament.startsAt || now > tournament.endsAt) throw new Error('TOURNAMENT_NOT_ACTIVE');
    const existingEntry = await this.tournamentRepo.getPlayerEntry(tournamentId, playerId);
    if (existingEntry) throw new Error('ALREADY_ENTERED');
    if (tournament.maxParticipants > 0) {
      const entries = await this.tournamentRepo.getTournamentEntries(tournamentId, 'active');
      if (entries.length >= tournament.maxParticipants) throw new Error('TOURNAMENT_FULL');
    }
    const player = await this.playerService.getPlayerById(playerId);
    if (!player) throw new Error('PLAYER_NOT_FOUND');
    if (tournament.entryCostGems > 0) {
      if (player.gems < tournament.entryCostGems) throw new Error('INSUFFICIENT_GEMS');
      await this.playerService.addGems(playerId, -tournament.entryCostGems);
    }
    if (tournament.entryCostGold > 0) {
      if (player.gold < tournament.entryCostGold) throw new Error('INSUFFICIENT_GOLD');
      await this.playerService.addGold(playerId, -tournament.entryCostGold);
    }
    const useDeckId = tournament.tournamentType === 'draft' ? null : (deckId || null);
    const entryId = await this.tournamentRepo.createEntry({
      tournament_id: tournamentId,
      player_id: playerId,
      deck_id: useDeckId,
      wins: 0,
      losses: 0,
      status: 'active',
      completed_at: null,
      final_rewards: null,
      draft_picks: null
    });
    return this.getEntry(entryId) as Promise<TournamentEntry>;
  }

  async getEntry(entryId: number): Promise<TournamentEntry | null> {
    const e = await this.tournamentRepo.getEntry(entryId);
    if (!e) return null;
    return {
      entryId: e.entry_id,
      tournamentId: e.tournament_id,
      playerId: e.player_id,
      deckId: e.deck_id,
      wins: e.wins,
      losses: e.losses,
      status: e.status as any,
      startedAt: e.started_at,
      completedAt: e.completed_at,
      finalRewards: e.final_rewards,
      draftPicks: e.draft_picks
    };
  }

  async getPlayerEntry(tournamentId: number, playerId: string): Promise<TournamentEntry | null> {
    const e = await this.tournamentRepo.getPlayerEntry(tournamentId, playerId);
    if (!e) return null;
    return {
      entryId: e.entry_id,
      tournamentId: e.tournament_id,
      playerId: e.player_id,
      deckId: e.deck_id,
      wins: e.wins,
      losses: e.losses,
      status: e.status as any,
      startedAt: e.started_at,
      completedAt: e.completed_at,
      finalRewards: e.final_rewards,
      draftPicks: e.draft_picks
    };
  }

  async getTournamentEntries(tournamentId: number): Promise<TournamentEntry[]> {
    return (await this.tournamentRepo.getTournamentEntries(tournamentId)).map(e => ({
      entryId: e.entry_id,
      tournamentId: e.tournament_id,
      playerId: e.player_id,
      deckId: e.deck_id,
      wins: e.wins,
      losses: e.losses,
      status: e.status as any,
      startedAt: e.started_at,
      completedAt: e.completed_at,
      finalRewards: e.final_rewards,
      draftPicks: e.draft_picks
    }));
  }

  async recordBattleResult(entryId: number, isWin: boolean): Promise<TournamentEntry | null> {
    const entry = await this.tournamentRepo.getEntry(entryId);
    if (!entry) throw new Error('ENTRY_NOT_FOUND');
    if (entry.status !== 'active') throw new Error('ENTRY_NOT_ACTIVE');
    const tournament = await this.getTournament(entry.tournament_id);
    if (!tournament) throw new Error('TOURNAMENT_NOT_FOUND');
    if (isWin) await this.tournamentRepo.recordWin(entryId);
    else await this.tournamentRepo.recordLoss(entryId);
    const updatedEntry = await this.tournamentRepo.getEntry(entryId);
    if (!updatedEntry) return null;
    if (updatedEntry.wins >= tournament.maxWins || updatedEntry.losses >= tournament.maxLosses) {
      await this.completeEntry(entryId, updatedEntry.wins);
    }
    return this.getEntry(entryId);
  }

  private async completeEntry(entryId: number, wins: number): Promise<void> {
    const tournament = await this.getTournament((await this.tournamentRepo.getEntry(entryId))!.tournament_id);
    if (!tournament) return;
    const rewards = await this.tournamentRepo.getRewardsForWins(tournament.tournamentId, wins);
    await this.tournamentRepo.completeEntry(entryId, rewards);
    if (rewards) await this.grantTournamentRewards((await this.tournamentRepo.getEntry(entryId))!.player_id, rewards);
  }

  async retireFromTournament(entryId: number): Promise<void> { await this.tournamentRepo.retireEntry(entryId); }

  async getDraftPicks(entryId: number): Promise<number[]> { return this.tournamentRepo.getDraftPicks(entryId); }
  async makeDraftPick(entryId: number, cardId: number): Promise<void> { await this.tournamentRepo.saveDraftPicks(entryId, [...(await this.tournamentRepo.getDraftPicks(entryId)), cardId]); }
  async finalizeDraft(entryId: number): Promise<string> {
    const entry = await this.tournamentRepo.getEntry(entryId);
    if (!entry || !entry.draft_picks || entry.draft_picks.length !== 8) throw new Error('DRAFT_INCOMPLETE');
    const deck = await this.playerService.saveDeck(entry.player_id, entry.draft_picks, 'Draft Deck');
    const deckId = deck.deckId;
    await this.tournamentRepo.updateEntry(entryId, { deck_id: deckId });
    return deckId;
  }

  private async grantTournamentRewards(playerId: string, rewards: any[]): Promise<void> {
    for (const reward of rewards) {
      switch (reward.type) {
        case 'gold': if (reward.amount) await this.playerService.addGold(playerId, reward.amount); break;
        case 'gems': if (reward.amount) await this.playerService.addGems(playerId, reward.amount); break;
        case 'xp': if (reward.amount) await this.playerService.addExperience(playerId, reward.amount); break;
        case 'chest': if (reward.chestId) await this.grantChest(playerId, reward.chestId); break;
        case 'wild_card': if (reward.rarity && reward.count) await this.grantWildCard(playerId, reward.rarity, reward.count); break;
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

  async getPlayerTournamentHistory(playerId: string): Promise<any[]> { return this.tournamentRepo.getPlayerTournamentHistory(playerId); }
  async createTournament(tournament: Omit<Tournament, 'tournamentId'>): Promise<number> { return this.tournamentRepo.createTournament({ name: tournament.name, description: tournament.description, tournament_type: tournament.tournamentType, entry_cost_gems: tournament.entryCostGems, entry_cost_gold: tournament.entryCostGold, max_losses: tournament.maxLosses, max_wins: tournament.maxWins, card_level_cap: tournament.cardLevelCap, allowed_cards: tournament.allowedCards, banned_cards: tournament.bannedCards, starts_at: tournament.startsAt, ends_at: tournament.endsAt, rewards: tournament.rewards, is_enabled: tournament.isEnabled, max_participants: tournament.maxParticipants }); }
  async updateTournament(tournamentId: number, data: Partial<Tournament>): Promise<void> { await this.tournamentRepo.updateTournament(tournamentId, { name: data.name, description: data.description, tournament_type: data.tournamentType, entry_cost_gems: data.entryCostGems, entry_cost_gold: data.entryCostGold, max_losses: data.maxLosses, max_wins: data.maxWins, card_level_cap: data.cardLevelCap, allowed_cards: data.allowedCards, banned_cards: data.bannedCards, starts_at: data.startsAt, ends_at: data.endsAt, rewards: data.rewards, is_enabled: data.isEnabled, max_participants: data.maxParticipants }); }
  async getTournamentStats(tournamentId: number): Promise<any> { return this.tournamentRepo.getTournamentStats(tournamentId); }
}