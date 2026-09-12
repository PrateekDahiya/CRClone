import { Database } from './Database';
import { logger } from '../utils/logger';
import { Player, DeckData, CardCollectionEntry } from '../services/PlayerService';

export class PlayerRepository {
  constructor(private db: Database) {}

  async findById(id: string): Promise<Player | null> {
    const rows = await this.db.query(
      'SELECT * FROM players WHERE id = ?',
      [id]
    );
    if (rows.length === 0) return null;
    return this.mapRowToPlayer(rows[0]);
  }

  async findByUsername(username: string): Promise<Player | null> {
    const rows = await this.db.query(
      'SELECT * FROM players WHERE username = ?',
      [username]
    );
    if (rows.length === 0) return null;
    return this.mapRowToPlayer(rows[0]);
  }

  async findByEmail(email: string): Promise<Player | null> {
    const rows = await this.db.query(
      'SELECT * FROM players WHERE email = ?',
      [email]
    );
    if (rows.length === 0) return null;
    return this.mapRowToPlayer(rows[0]);
  }

  async create(player: Player): Promise<void> {
    await this.db.execute(
      `INSERT INTO players (id, username, email, password_hash, trophies, best_trophies, level, experience, gold, gems, avatar_id, name_color, created_at, last_login_at)
       VALUES (?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, NOW(), NOW())`,
      [player.id, player.username, player.email, player.passwordHash, player.trophies, player.bestTrophies,
       player.level, player.experience, player.gold, player.gems, player.avatarId, player.nameColor]
    );

    // Initialize empty collection
    // Cards will be added via addCards
  }

  async updateTrophies(playerId: string, change: number): Promise<void> {
    await this.db.execute(
      'UPDATE players SET trophies = trophies + ?, best_trophies = GREATEST(best_trophies, trophies + ?) WHERE id = ?',
      [change, change, playerId]
    );
  }

  async updateLevel(playerId: string, level: number, experience: number): Promise<void> {
    await this.db.execute(
      'UPDATE players SET level = ?, experience = ? WHERE id = ?',
      [level, experience, playerId]
    );
  }

  async updateGold(playerId: string, amount: number): Promise<void> {
    await this.db.execute(
      'UPDATE players SET gold = gold + ? WHERE id = ?',
      [amount, playerId]
    );
  }

  async updateGems(playerId: string, amount: number): Promise<void> {
    await this.db.execute(
      'UPDATE players SET gems = gems + ? WHERE id = ?',
      [amount, playerId]
    );
  }

  async updateLastLogin(playerId: string): Promise<void> {
    await this.db.execute(
      'UPDATE players SET last_login_at = NOW() WHERE id = ?',
      [playerId]
    );
  }

  async updateProfile(playerId: string, data: { avatarId?: number; nameColor?: string }): Promise<void> {
    const updates: string[] = [];
    const params: any[] = [];

    if (data.avatarId !== undefined) {
      updates.push('avatar_id = ?');
      params.push(data.avatarId);
    }
    if (data.nameColor !== undefined) {
      updates.push('name_color = ?');
      params.push(data.nameColor);
    }

    if (updates.length === 0) return;

    params.push(playerId);
    await this.db.execute(
      `UPDATE players SET ${updates.join(', ')} WHERE id = ?`,
      params
    );
  }

  async saveDeck(playerId: string, deckId: string, name: string, cardIds: number[], avgElixir: number, hasChampion: boolean): Promise<void> {
    await this.db.execute(
      `INSERT INTO player_decks (id, player_id, name, card_ids, avg_elixir, has_champion, is_active, updated_at)
       VALUES (?, ?, ?, ?, ?, ?, TRUE, NOW())
       ON DUPLICATE KEY UPDATE name = ?, card_ids = ?, avg_elixir = ?, has_champion = ?, is_active = TRUE, updated_at = NOW()`,
      [deckId, playerId, name, JSON.stringify(cardIds), avgElixir, hasChampion, name, JSON.stringify(cardIds), avgElixir, hasChampion]
    );
  }

  async getActiveDeck(playerId: string): Promise<DeckData | null> {
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

  async getAllDecks(playerId: string): Promise<DeckData[]> {
    const rows = await this.db.query(
      'SELECT * FROM player_decks WHERE player_id = ? ORDER BY updated_at DESC',
      [playerId]
    );
    return rows.map(row => ({
      deckId: row.id,
      name: row.name,
      cardIds: JSON.parse(row.card_ids),
      avgElixir: row.avg_elixir,
      hasChampion: row.has_champion,
    }));
  }

  async setActiveDeck(playerId: string, deckId: string): Promise<void> {
    await this.db.transaction(async (conn) => {
      await conn.execute('UPDATE player_decks SET is_active = FALSE WHERE player_id = ?', [playerId]);
      await conn.execute('UPDATE player_decks SET is_active = TRUE WHERE id = ? AND player_id = ?', [deckId, playerId]);
    });
  }

  async deleteDeck(playerId: string, deckId: string): Promise<void> {
    await this.db.execute(
      'DELETE FROM player_decks WHERE id = ? AND player_id = ?',
      [deckId, playerId]
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
        abilityLevel: row.ability_level
      });
    }
    return collection;
  }

  async addCards(playerId: string, cardId: number, count: number): Promise<void> {
    await this.db.execute(
      `INSERT INTO player_cards (player_id, card_id, count, level, upgrade_progress)
       VALUES (?, ?, ?, 1, 0)
       ON DUPLICATE KEY UPDATE count = count + ?, upgrade_progress = upgrade_progress + ?`,
      [playerId, cardId, count, count, count]
    );
  }

  async upgradeCard(playerId: string, cardId: number, requiredCards: number, goldCost: number, nextLevel: number): Promise<boolean> {
    try {
      await this.db.transaction(async (conn) => {
        // Check if player has enough cards
        const cardRows = await conn.execute(
          'SELECT count FROM player_cards WHERE player_id = ? AND card_id = ?',
          [playerId, cardId]
        );
        const cardData = cardRows[0] as any;
        if (!cardData || cardData.count < requiredCards) {
          throw new Error('NOT_ENOUGH_CARDS');
        }

        // Check gold
        const playerRows = await conn.execute('SELECT gold FROM players WHERE id = ?', [playerId]);
        const playerData = playerRows[0] as any;
        if (!playerData || playerData.gold < goldCost) {
          throw new Error('NOT_ENOUGH_GOLD');
        }

        // Deduct cards and gold, upgrade
        await conn.execute(
          'UPDATE player_cards SET count = count - ?, level = ?, upgrade_progress = 0 WHERE player_id = ? AND card_id = ?',
          [requiredCards, nextLevel, playerId, cardId]
        );
        await conn.execute('UPDATE players SET gold = gold - ? WHERE id = ?', [goldCost, playerId]);
      });
      return true;
    } catch (error) {
      logger.warn('Card upgrade failed', { playerId, cardId, error: error instanceof Error ? error.message : String(error) });
      return false;
    }
  }

  async getLeaderboard(limit: number): Promise<Array<{ playerId: string; username: string; trophies: number; level: number }>> {
    const rows = await this.db.query(
      'SELECT id, username, trophies, level FROM players ORDER BY trophies DESC LIMIT ?',
      [limit]
    );
    return rows.map(row => ({
      playerId: row.id,
      username: row.username,
      trophies: row.trophies,
      level: row.level,
    }));
  }

  async getPlayerRank(playerId: string): Promise<number> {
    const rows = await this.db.query(
      'SELECT COUNT(*) + 1 as rank FROM players WHERE trophies > (SELECT trophies FROM players WHERE id = ?)',
      [playerId]
    );
    return rows[0]?.rank || 1;
  }

  async searchPlayers(query: string, limit: number): Promise<Array<{ playerId: string; username: string; trophies: number; level: number }>> {
    const rows = await this.db.query(
      'SELECT id, username, trophies, level FROM players WHERE username LIKE ? ORDER BY trophies DESC LIMIT ?',
      [`%${query}%`, limit]
    );
    return rows.map(row => ({
      playerId: row.id,
      username: row.username,
      trophies: row.trophies,
      level: row.level,
    }));
  }

  async getBattleHistory(playerId: string, limit: number): Promise<any[]> {
    const rows = await this.db.query(
      `SELECT * FROM battles 
       WHERE player1_id = ? OR player2_id = ? 
       ORDER BY created_at DESC LIMIT ?`,
      [playerId, playerId, limit]
    );
    return rows;
  }

  private mapRowToPlayer(row: any): any {
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
      tag: row.tag,
      clanId: row.clan_id,
      clanRole: row.clan_role,
      activeDeck: null, // Load separately
      collection: new Map(), // Load separately
      createdAt: row.created_at,
      lastLoginAt: row.last_login_at,
    };
  }

  // Aliases for service compatibility
  async getPlayerById(id: string): Promise<any> { return this.findById(id); }
  async getPlayerByUsername(username: string): Promise<any> { return this.findByUsername(username); }
  async getPlayerByTag(tag: string): Promise<any> {
    const rows = await this.db.query('SELECT * FROM players WHERE tag = ?', [tag]);
    if (rows.length === 0) return null;
    return this.mapRowToPlayer(rows[0]);
  }
  async getPlayerCards(playerId: string): Promise<any[]> {
    return this.db.query('SELECT * FROM player_cards WHERE player_id = ?', [playerId]);
  }
  async getPlayerDecks(playerId: string): Promise<any[]> {
    return this.db.query('SELECT * FROM player_decks WHERE player_id = ?', [playerId]);
  }
  async updateExperience(playerId: string, amount: number): Promise<void> {
    await this.db.execute('UPDATE players SET experience = experience + ? WHERE id = ?', [amount, playerId]);
  }
  async getPlayerSettings(playerId: string): Promise<any> {
    return this.db.query('SELECT * FROM player_settings WHERE player_id = ?', [playerId]);
  }
  async updatePlayerSettings(playerId: string, settings: any): Promise<void> {
    await this.db.execute('UPDATE player_settings SET ? WHERE player_id = ?', [settings, playerId]);
  }
  async getTopPlayersByTrophies(limit: number): Promise<any[]> {
    return this.db.query('SELECT * FROM players WHERE is_banned = FALSE ORDER BY trophies DESC LIMIT ?', [limit]);
  }
  async getPlayerRankByTrophies(playerId: string): Promise<number> {
    const rows = await this.db.query('SELECT COUNT(*) + 1 as rank FROM players WHERE trophies > (SELECT trophies FROM players WHERE id = ?) AND is_banned = FALSE', [playerId]);
    return rows[0]?.rank || 0;
  }
  async getPlayersAroundRank(playerId: string, range: number): Promise<any[]> {
    const rank = await this.getPlayerRankByTrophies(playerId);
    const offset = Math.max(0, rank - range - 1);
    return this.db.query('SELECT * FROM players WHERE is_banned = FALSE ORDER BY trophies DESC LIMIT ? OFFSET ?', [range * 2 + 1, offset]);
  }
  async updateLastBattle(playerId: string): Promise<void> {
    await this.db.execute('UPDATE players SET last_battle_at = NOW() WHERE id = ?', [playerId]);
  }
  async updatePlayer(playerId: string, data: any): Promise<void> {
    const { set, params } = this.buildUpdateClause(data);
    if (!set) return;
    params.push(playerId);
    await this.db.execute(`UPDATE players SET ${set} WHERE id = ?`, params);
  }

  private buildUpdateClause(data: Record<string, any>): { set: string; params: any[] } {
    const clauses: string[] = []; const params: any[] = [];
    for (const [key, value] of Object.entries(data)) {
      if (value === undefined) continue;
      clauses.push(`\`${key}\` = ?`); params.push(value);
    }
    return { set: clauses.join(', '), params };
  }
}