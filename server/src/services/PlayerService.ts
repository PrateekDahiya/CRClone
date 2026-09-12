import { Database, db } from '../persistence/Database';
import { config } from '../config';
import { logger } from '../utils/logger';
import * as bcrypt from 'bcryptjs';
import * as jwt from 'jsonwebtoken';
import { v4 as uuidv4 } from 'uuid';

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
  activeDeck: DeckData | null;
  collection: Map<number, CardCollectionEntry>;
  createdAt: Date;
  lastLoginAt: Date;
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
}

export class PlayerService {
  private db: Database;

  constructor(database: Database) {
    this.db = database;
  }

  async authenticate(token: string): Promise<Player | null> {
    try {
      const decoded = jwt.verify(token, config.jwt.secret) as { playerId: string };
      return await this.getPlayerById(decoded.playerId);
    } catch (error) {
      return null;
    }
  }

  async register(username: string, email: string, password: string): Promise<{ player: Player; token: string }> {
    // Check if username/email exists
    const existing = await this.db.query(
      'SELECT id FROM players WHERE username = ? OR email = ?',
      [username, email]
    );
    if (existing.length > 0) {
      throw new Error('USERNAME_OR_EMAIL_EXISTS');
    }

    const passwordHash = await bcrypt.hash(password, 12);
    const playerId = uuidv4();

    await this.db.execute(
      `INSERT INTO players (id, username, email, password_hash, trophies, level, gold, gems, created_at)
       VALUES (?, ?, ?, ?, 0, 1, 1000, 100, NOW())`,
      [playerId, username, email, passwordHash]
    );

    // Give starter deck
    await this.giveStarterDeck(playerId);

    const player = await this.getPlayerById(playerId);
    const token = this.generateToken(player);
    return { player, token };
  }

  async login(username: string, password: string): Promise<{ player: Player; token: string }> {
    const rows = await this.db.query(
      'SELECT * FROM players WHERE username = ?',
      [username]
    );

    if (rows.length === 0) {
      throw new Error('INVALID_CREDENTIALS');
    }

    const player = rows[0];
    const valid = await bcrypt.compare(password, player.password_hash);
    if (!valid) {
      throw new Error('INVALID_CREDENTIALS');
    }

    // Update last login
    await this.db.execute(
      'UPDATE players SET last_login_at = NOW() WHERE id = ?',
      [player.id]
    );

    const token = this.generateToken(player);
    return { player: this.mapRowToPlayer(player), token };
  }

  async getPlayerById(playerId: string): Promise<Player | null> {
    const rows = await this.db.query('SELECT * FROM players WHERE id = ?', [playerId]);
    if (rows.length === 0) return null;
    return this.mapRowToPlayer(rows[0]);
  }

  async saveDeck(playerId: string, cardIds: number[]): Promise<void> {
    const deckId = uuidv4();
    const avgElixir = cardIds.reduce((sum, id) => sum + this.getCardElixirCost(id), 0) / cardIds.length;
    const hasChampion = cardIds.some(id => this.getCardRarity(id) === 'legendary'); // Simplified

    await this.db.execute(
      `INSERT INTO player_decks (id, player_id, name, card_ids, avg_elixir, has_champion, is_active, updated_at)
       VALUES (?, ?, 'Main Deck', ?, ?, ?, TRUE, NOW())
       ON DUPLICATE KEY UPDATE card_ids = ?, avg_elixir = ?, has_champion = ?, is_active = TRUE, updated_at = NOW()`,
      [deckId, playerId, JSON.stringify(cardIds), avgElixir, hasChampion, JSON.stringify(cardIds), avgElixir, hasChampion]
    );
  }

  async getDeck(playerId: string): Promise<DeckData | null> {
    const rows = await this.db.query(
      'SELECT * FROM player_decks WHERE player_id = ? AND is_active = TRUE',
      [playerId]
    );
    if (rows.length === 0) return null;
    return {
      deckId: rows[0].id,
      name: rows[0].name,
      cardIds: JSON.parse(rows[0].card_ids),
      avgElixir: rows[0].avg_elixir,
      hasChampion: rows[0].has_champion,
    };
  }

  async addCards(playerId: string, cardId: number, count: number): Promise<void> {
    await this.db.execute(
      `INSERT INTO player_cards (player_id, card_id, count, level, upgrade_progress)
       VALUES (?, ?, ?, 1, 0)
       ON DUPLICATE KEY UPDATE count = count + ?, upgrade_progress = upgrade_progress + ?`,
      [playerId, cardId, count, count, count]
    );
  }

  async getCollection(playerId: string): Promise<Map<number, CardCollectionEntry>> {
    const rows = await this.db.query(
      'SELECT * FROM player_cards WHERE player_id = ?',
      [playerId]
    );
    const collection = new Map<number, CardCollectionEntry>();
    for (const row of rows) {
      collection.set(row.card_id, {
        cardId: row.card_id,
        count: row.count,
        level: row.level,
        upgradeProgress: row.upgrade_progress,
      });
    }
    return collection;
  }

  async upgradeCard(playerId: string, cardId: number): Promise<boolean> {
    // Check if player has enough cards and gold
    const collection = await this.getCollection(playerId);
    const entry = collection.get(cardId);
    if (!entry) return false;

    const nextLevel = entry.level + 1;
    const requiredCards = this.getCardsRequiredForLevel(nextLevel);
    const goldCost = this.getGoldCostForLevel(nextLevel);

    if (entry.count < requiredCards) return false;

    // Check gold
    const player = await this.getPlayerById(playerId);
    if (!player || player.gold < goldCost) return false;

    // Deduct resources
    await this.db.transaction(async (conn) => {
      await conn.execute(
        'UPDATE player_cards SET count = count - ?, level = ?, upgrade_progress = 0 WHERE player_id = ? AND card_id = ?',
        [requiredCards, nextLevel, playerId, cardId]
      );
      await conn.execute(
        'UPDATE players SET gold = gold - ? WHERE id = ?',
        [goldCost, playerId]
      );
    });

    return true;
  }

  async addGold(playerId: string, amount: number): Promise<void> {
    await this.db.execute('UPDATE players SET gold = gold + ? WHERE id = ?', [amount, playerId]);
  }

  async addGems(playerId: string, amount: number): Promise<void> {
    await this.db.execute('UPDATE players SET gems = gems + ? WHERE id = ?', [amount, playerId]);
  }

  async saveBattleResult(result: any): Promise<void> {
    // Save battle to database
    await this.db.execute(
      `INSERT INTO battles (battle_id, battle_type, player1_id, player2_id, winner, 
       player1_crowns, player2_crowns, duration, went_overtime, replay_id, created_at)
       VALUES (?, ?, ?, ?, ?, ?, ?, ?, ?, ?, NOW())`,
      [result.battleId, 'ladder', result.player1Id, result.player2Id, result.winner,
       result.player1Crowns, result.player2Crowns, result.duration, result.wentOvertime, result.replayId]
    );

    // Update player trophies
    if (result.player1TrophyChange !== 0) {
      await this.db.execute('UPDATE players SET trophies = trophies + ? WHERE id = ?', 
        [result.player1TrophyChange, result.player1Id]);
    }
    if (result.player2TrophyChange !== 0) {
      await this.db.execute('UPDATE players SET trophies = trophies + ? WHERE id = ?', 
        [result.player2TrophyChange, result.player2Id]);
    }
  }

  private generateToken(player: Player): string {
    return jwt.sign({ playerId: player.id }, config.jwt.secret, { expiresIn: config.jwt.expiresIn });
  }

  private mapRowToPlayer(row: any): Player {
    return {
      id: row.id,
      username: row.username,
      email: row.email,
      passwordHash: row.password_hash,
      trophies: row.trophies,
      bestTrophies: row.best_trophies,
      level: row.level,
      experience: row.experience,
      gold: row.gold,
      gems: row.gems,
      avatarId: row.avatar_id,
      nameColor: row.name_color,
      activeDeck: null, // Load separately
      collection: new Map(), // Load separately
      createdAt: row.created_at,
      lastLoginAt: row.last_login_at,
    };
  }

  private async giveStarterDeck(playerId: string): Promise<void> {
    // Give starter cards
    const starterCards = [26000040, 26000041, 26000042, 26000043, 26000044, 26000045, 26000046, 26000047];
    for (const cardId of starterCards) {
      await this.addCards(playerId, cardId, 10);
    }

    // Create initial deck
    await this.saveDeck(playerId, starterCards);
  }

  private getCardElixirCost(cardId: number): number {
    // In real implementation, load from card database
    return 3;
  }

  private getCardRarity(cardId: number): string {
    return 'common';
  }

  private getCardsRequiredForLevel(level: number): number {
    return Math.floor(2 * Math.pow(1.8, level - 1));
  }

  private getGoldCostForLevel(level: number): number {
    return Math.floor(50 * Math.pow(1.5, level - 1));
  }
}