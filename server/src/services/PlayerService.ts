import { Database, db } from '../persistence/Database';
import { config } from '../config';
import { logger } from '../utils/logger';
import * as bcrypt from 'bcryptjs';
import * as jwt from 'jsonwebtoken';
import { v4 as uuidv4 } from 'uuid';
import { PlayerRepository } from '../persistence/PlayerRepository';
import { BattleRepository } from '../persistence/BattleRepository';

export interface Player {
  id: string;
  username: string;
  email: string;
  passwordHash: string;
  trophies: number;
  bestTrophies: number;
  level: number;
  experience: number;
  gold: number;
  gems: number;
  avatarId: number;
  nameColor: string;
  tag: string | null;
  activeDeck: DeckData | null;
  collection: Map<number, CardCollectionEntry>;
  createdAt: Date;
  lastLoginAt: Date;
  clanId: number | null;
  clanRole: string | null;
}

export interface DeckData {
  deckId: string;
  name: string;
  cardIds: number[];
  avgElixir: number;
  hasChampion: boolean;
}

export interface CardCollectionEntry {
  cardId: number;
  count: number;
  level: number;
  upgradeProgress: number;
  abilityLevel: number;
}

export interface BattleResultData {
  battleId: string;
  battleType: string;
  player1Id: string;
  player2Id: string;
  winner: 'player1' | 'player2' | 'draw';
  player1Crowns: number;
  player2Crowns: number;
  player1TrophyChange: number;
  player2TrophyChange: number;
  duration: number;
  wentOvertime: boolean;
  replayId: number;
  replaySeed: number;
}

export class PlayerService {
  private db: Database;
  private playerRepo: PlayerRepository;
  private battleRepo: BattleRepository;

  constructor(database: Database = db) {
    this.db = database;
    this.playerRepo = new PlayerRepository(database);
    this.battleRepo = new BattleRepository(database);
  }

  async authenticate(token: string): Promise<Player | null> {
    try {
      const decoded = jwt.verify(token, config.jwt.secret) as { playerId: string };
      return await this.getPlayerById(decoded.playerId);
    } catch (error) {
      return null;
    }
  }

  async register(username: string, email: string, password: string): Promise<{ player: Player; token: string; refreshToken: string }> {
    const existing = await this.db.query('SELECT id FROM players WHERE username = ? OR email = ?', [username, email]);
    if (existing.length > 0) throw new Error('USERNAME_OR_EMAIL_EXISTS');

    const passwordHash = await bcrypt.hash(password, 12);
    const playerId = uuidv4();

    await this.db.execute(
      `INSERT INTO players (id, username, email, password_hash, trophies, level, gold, gems, created_at) VALUES (?, ?, ?, ?, 0, 1, 1000, 100, NOW())`,
      [playerId, username, email, passwordHash]
    );

    await this.giveStarterDeck(playerId);

    const player = await this.getPlayerById(playerId);
    const token = this.generateToken(player!);
    const refreshToken = this.generateRefreshToken(player!);
    return { player: player!, token, refreshToken };
  }

  async login(username: string, password: string): Promise<{ player: Player; token: string; refreshToken: string }> {
    const rows = await this.db.query('SELECT * FROM players WHERE username = ?', [username]);
    if (rows.length === 0) throw new Error('INVALID_CREDENTIALS');

    const playerRow = rows[0];
    const valid = await bcrypt.compare(password, playerRow.password_hash);
    if (!valid) throw new Error('INVALID_CREDENTIALS');

    if (playerRow.is_banned) {
      if (playerRow.ban_expires_at && new Date(playerRow.ban_expires_at) > new Date()) throw new Error('ACCOUNT_BANNED');
      else await this.db.execute('UPDATE players SET is_banned = FALSE, ban_reason = NULL, ban_expires_at = NULL WHERE id = ?', [playerRow.id]);
    }

    await this.db.execute('UPDATE players SET last_login_at = NOW() WHERE id = ?', [playerRow.id]);

    const player = await this.getPlayerById(playerRow.id);
    const token = this.generateToken(player!);
    const refreshToken = this.generateRefreshToken(player!);
    return { player: player!, token, refreshToken };
  }

  async refreshToken(refreshToken: string): Promise<{ token: string; refreshToken: string } | null> {
    try {
      const decoded = jwt.verify(refreshToken, config.jwt.secret) as { playerId: string; type: string };
      if (decoded.type !== 'refresh') return null;
      const player = await this.getPlayerById(decoded.playerId);
      if (!player) return null;
      return { token: this.generateToken(player), refreshToken: this.generateRefreshToken(player) };
    } catch { return null; }
  }

  async guestLogin(): Promise<{ player: Player; token: string; refreshToken: string }> {
    const guestName = `Guest_${uuidv4().slice(0, 8)}`;
    const playerId = uuidv4();
    await this.db.execute(`INSERT INTO players (id, username, email, password_hash, auth_provider, trophies, level, gold, gems, created_at) VALUES (?, ?, ?, ?, 'guest', 0, 1, 1000, 100, NOW())`, [playerId, guestName, `${guestName}@guest.local`, await bcrypt.hash(uuidv4(), 12)]);
    await this.giveStarterDeck(playerId);
    const player = await this.getPlayerById(playerId);
    const token = this.generateToken(player!);
    const refreshToken = this.generateRefreshToken(player!);
    return { player: player!, token, refreshToken };
  }

  async getPlayerById(playerId: string): Promise<Player | null> {
    const row = await this.playerRepo.getPlayerById(playerId);
    if (!row) return null;

    const [deck, collection] = await Promise.all([
      this.playerRepo.getActiveDeck(playerId),
      this.playerRepo.getPlayerCards(playerId)
    ]);

    const collectionMap = new Map<number, CardCollectionEntry>();
    for (const card of collection) {
      collectionMap.set(card.card_id, {
        cardId: card.card_id,
        count: card.count,
        level: card.level,
        upgradeProgress: card.upgrade_progress,
        abilityLevel: card.ability_level
      });
    }

    return {
      id: row.id, username: row.username, email: row.email, passwordHash: row.password_hash,
      trophies: row.trophies, bestTrophies: row.best_trophies, level: row.level, experience: row.experience,
      gold: row.gold, gems: row.gems, avatarId: row.avatar_id, nameColor: row.name_color, tag: row.tag,
      activeDeck: deck ? this.mapDeckRowToDeckData(deck) : null,
      collection: collectionMap,
      createdAt: row.created_at, lastLoginAt: row.last_login_at,
      clanId: row.clan_id, clanRole: row.clan_role
    };
  }

  async getPlayerByUsername(username: string): Promise<Player | null> {
    const row = await this.playerRepo.getPlayerByUsername(username);
    if (!row) return null;
    return this.getPlayerById(row.id);
  }

  async getPlayerByTag(tag: string): Promise<Player | null> {
    const row = await this.playerRepo.getPlayerByTag(tag);
    if (!row) return null;
    return this.getPlayerById(row.id);
  }

  async saveDeck(playerId: string, cardIds: number[], deckName?: string): Promise<DeckData> {
    if (cardIds.length !== 8) throw new Error('DECK_MUST_HAVE_8_CARDS');
    const collection = await this.playerRepo.getPlayerCards(playerId);
    const collectionMap = new Map(collection.map(c => [c.card_id, c]));
    for (const cardId of cardIds) { if (!collectionMap.get(cardId) || collectionMap.get(cardId)!.count === 0) throw new Error(`PLAYER_DOES_NOT_OWN_CARD_${cardId}`); }
    const championCount = cardIds.filter(id => this.isChampionCard(id)).length;
    if (championCount > 1) throw new Error('MAX_ONE_CHAMPION_PER_DECK');
    const avgElixir = cardIds.reduce((sum, id) => sum + this.getCardElixirCost(id), 0) / 8;
    const hasChampion = championCount > 0;
    const deckId = uuidv4();
    await this.playerRepo.saveDeck(deckId, playerId, deckName || 'Main Deck', cardIds, avgElixir, hasChampion);
    return { deckId, name: deckName || 'Main Deck', cardIds, avgElixir, hasChampion };
  }

  async getDeck(playerId: string): Promise<DeckData | null> {
    const deck = await this.playerRepo.getActiveDeck(playerId);
    if (!deck) return null;
    return this.mapDeckRowToDeckData(deck);
  }

  async getAllDecks(playerId: string): Promise<DeckData[]> {
    const decks = await this.playerRepo.getPlayerDecks(playerId);
    return decks.map(d => this.mapDeckRowToDeckData(d));
  }

  private mapDeckRowToDeckData(deck: any): DeckData {
    return {
      deckId: deck.id,
      name: deck.name,
      cardIds: [deck.slot_1_card, deck.slot_2_card, deck.slot_3_card, deck.slot_4_card, deck.slot_5_card, deck.slot_6_card, deck.slot_7_card, deck.slot_8_card].filter(c => c !== null) as number[],
      avgElixir: deck.avg_elixir,
      hasChampion: deck.has_champion
    };
  }

  async deleteDeck(playerId: string, deckId: string): Promise<void> { await this.playerRepo.deleteDeck(deckId, playerId); }
  async setActiveDeck(playerId: string, deckId: string): Promise<void> { await this.playerRepo.setActiveDeck(playerId, deckId); }

  async getCollection(playerId: string): Promise<Map<number, CardCollectionEntry>> {
    const collection = await this.playerRepo.getPlayerCards(playerId);
    const map = new Map<number, CardCollectionEntry>();
    for (const card of collection) { map.set(card.card_id, { cardId: card.card_id, count: card.count, level: card.level, upgradeProgress: card.upgrade_progress, abilityLevel: card.ability_level }); }
    return map;
  }

  async addCards(playerId: string, cardId: number, count: number): Promise<void> { await this.playerRepo.addCards(playerId, cardId, count); }

  async upgradeCard(playerId: string, cardId: number): Promise<boolean> {
    const collection = await this.getCollection(playerId);
    const entry = collection.get(cardId);
    if (!entry) return false;
    const nextLevel = entry.level + 1;
    const requiredCards = this.getCardsRequiredForLevel(nextLevel, entry.cardId);
    const goldCost = this.getGoldCostForLevel(nextLevel, entry.cardId);
    if (entry.count < requiredCards) return false;
    const player = await this.getPlayerById(playerId);
    if (!player || player.gold < goldCost) return false;
    return await this.playerRepo.upgradeCard(playerId, cardId, nextLevel, requiredCards, goldCost);
  }

  async addGold(playerId: string, amount: number): Promise<void> { await this.playerRepo.updateGold(playerId, amount); }
  async addGems(playerId: string, amount: number): Promise<void> { await this.playerRepo.updateGems(playerId, amount); }
  async addExperience(playerId: string, amount: number): Promise<void> { await this.playerRepo.updateExperience(playerId, amount); }

  async saveBattleResult(result: BattleResultData): Promise<void> {
    await this.battleRepo.createBattle({
      battle_type: result.battleType, player_1_id: result.player1Id, player_2_id: result.player2Id,
      player_3_id: null, player_4_id: null, player_1_deck_id: result.battleId, player_2_deck_id: result.battleId,
      player_3_deck_id: null, player_4_deck_id: null, winner_team: result.winner === 'player1' ? 1 : result.winner === 'player2' ? 2 : null,
      player_1_crowns: result.player1Crowns, player_2_crowns: result.player2Crowns, player_3_crowns: 0, player_4_crowns: 0,
      player_1_king_hp: null, player_2_king_hp: null, player_3_king_hp: null, player_4_king_hp: null,
      duration_seconds: result.duration, went_overtime: result.wentOvertime,
      player_1_trophy_change: result.player1TrophyChange, player_2_trophy_change: result.player2TrophyChange,
      player_3_trophy_change: 0, player_4_trophy_change: 0,
      replay_id: result.replayId, replay_seed: result.replaySeed,
      player_1_trophies_at_start: 0, player_2_trophies_at_start: 0, player_3_trophies_at_start: null, player_4_trophies_at_start: null
    });
    if (result.player1TrophyChange !== 0) await this.playerRepo.updateTrophies(result.player1Id, result.player1TrophyChange);
    if (result.player2TrophyChange !== 0) await this.playerRepo.updateTrophies(result.player2Id, result.player2TrophyChange);
    await this.playerRepo.updateLastBattle(result.player1Id);
    await this.playerRepo.updateLastBattle(result.player2Id);
  }

  async getBattleHistory(playerId: string, limit: number = 20): Promise<any[]> { return this.playerRepo.getBattleHistory(playerId, limit); }
  async updateProfile(playerId: string, data: { avatarId?: number; nameColor?: string }): Promise<void> { await this.playerRepo.updatePlayer(playerId, data as any); }
  async getSettings(playerId: string): Promise<any> { return this.playerRepo.getPlayerSettings(playerId); }
  async updateSettings(playerId: string, settings: any): Promise<void> { await this.playerRepo.updatePlayerSettings(playerId, settings); }
  async getLeaderboard(limit: number = 100): Promise<Player[]> { const rows = await this.playerRepo.getTopPlayersByTrophies(limit); const players: Player[] = []; for (const row of rows) { const player = await this.getPlayerById(row.id); if (player) players.push(player); } return players; }
  async getPlayerRank(playerId: string): Promise<number> { return this.playerRepo.getPlayerRankByTrophies(playerId); }
  async getPlayersAroundPlayer(playerId: string, range: number = 10): Promise<Player[]> { const rows = await this.playerRepo.getPlayersAroundRank(playerId, range); const players: Player[] = []; for (const row of rows) { const player = await this.getPlayerById(row.id); if (player) players.push(player); } return players; }
  async searchPlayers(query: string, limit: number = 20): Promise<Player[]> { const rows = await this.playerRepo.searchPlayers(query, limit); const players: Player[] = []; for (const row of rows) { const player = await this.getPlayerById(row.playerId); if (player) players.push(player); } return players; }

  private generateToken(player: Player): string { return jwt.sign({ playerId: player.id }, config.jwt.secret, { expiresIn: config.jwt.expiresIn as any }); }
  private generateRefreshToken(player: Player): string { return jwt.sign({ playerId: player.id, type: 'refresh' }, config.jwt.secret, { expiresIn: config.jwt.refreshExpiresIn as any }); }

  private async giveStarterDeck(playerId: string): Promise<void> {
    const starterCards = [{ id: 26000000, count: 10 }, { id: 26000001, count: 10 }, { id: 26000002, count: 10 }, { id: 26000003, count: 10 }, { id: 26000004, count: 10 }, { id: 26000005, count: 10 }, { id: 26000006, count: 10 }, { id: 26000007, count: 10 }];
    for (const card of starterCards) await this.addCards(playerId, card.id, card.count);
    await this.saveDeck(playerId, starterCards.map(c => c.id), 'Starter Deck');
  }

  private isChampionCard(cardId: number): boolean { return cardId >= 27000000 && cardId < 28000000; }
  private getCardElixirCost(cardId: number): number { const costs: Record<number, number> = { 26000000: 3, 26000001: 3, 26000002: 2, 26000003: 5, 26000004: 4, 26000005: 4, 26000006: 4, 26000007: 3 }; return costs[cardId] || 3; }
  private getCardsRequiredForLevel(level: number, cardId: number): number { const rarity = this.getCardRarity(cardId); const baseCards = { common: 2, rare: 2, epic: 2, legendary: 1, champion: 1 }[rarity] || 2; return Math.floor(baseCards * Math.pow(1.8, level - 1)); }
  private getGoldCostForLevel(level: number, cardId: number): number { const rarity = this.getCardRarity(cardId); const baseGold = { common: 50, rare: 150, epic: 400, legendary: 1000, champion: 2000 }[rarity] || 50; return Math.floor(baseGold * Math.pow(1.5, level - 1)); }
  private getCardRarity(cardId: number): string { if (cardId >= 27000000) return 'champion'; if (cardId >= 26000100) return 'legendary'; if (cardId >= 26000050) return 'epic'; if (cardId >= 26000020) return 'rare'; return 'common'; }
}