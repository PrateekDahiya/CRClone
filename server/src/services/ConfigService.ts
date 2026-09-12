import { Database, db } from '../persistence/Database';
import { logger } from '../utils/logger';
import { EventEmitter } from 'events';

export interface GameConfig {
  config_key: string;
  config_value: any;
  description: string;
  updated_at: Date;
  updated_by: string;
}

export class ConfigService extends EventEmitter {
  private db: Database;
  private configCache: Map<string, any> = new Map();
  private watchInterval: NodeJS.Timeout | null = null;
  private lastCheck: Date = new Date();

  constructor(database: Database = db) {
    super();
    this.db = database;
  }

  async initialize(): Promise<void> {
    await this.loadAllConfigs();
    this.startHotReload();
    logger.info('ConfigService initialized with hot-reload');
  }

  async loadAllConfigs(): Promise<void> {
    const rows = await this.db.query<GameConfig>('SELECT * FROM game_config');
    for (const row of rows) {
      this.configCache.set(row.config_key, row.config_value);
    }
    logger.info(`Loaded ${this.configCache.size} config values`);
  }

  getConfig<T>(key: string, defaultValue?: T): T {
    const value = this.configCache.get(key);
    return (value !== undefined ? value : defaultValue) as T;
  }

  getAllConfigs(): Record<string, any> {
    const result: Record<string, any> = {};
    for (const [key, value] of this.configCache) {
      result[key] = value;
    }
    return result;
  }

  async updateConfig(key: string, value: any, updatedBy: string = 'system'): Promise<void> {
    await this.db.execute(
      `INSERT INTO game_config (config_key, config_value, description, updated_by)
       VALUES (?, ?, ?, ?)
       ON DUPLICATE KEY UPDATE config_value = VALUES(config_value), updated_by = VALUES(updated_by), updated_at = NOW()`,
      [key, JSON.stringify(value), '', updatedBy]
    );

    const oldValue = this.configCache.get(key);
    this.configCache.set(key, value);

    // Emit change event
    this.emit('configChanged', { key, oldValue, newValue: value, updatedBy });
    logger.info('Config updated', { key, oldValue, newValue: value });
  }

  async deleteConfig(key: string): Promise<void> {
    await this.db.execute('DELETE FROM game_config WHERE config_key = ?', [key]);
    this.configCache.delete(key);
    this.emit('configDeleted', { key });
    logger.info('Config deleted', { key });
  }

  // Hot reload - periodically check for external changes
  private startHotReload(intervalMs: number = 30000): void {
    this.watchInterval = setInterval(async () => {
      try {
        await this.checkForChanges();
      } catch (error) {
        const msg = error instanceof Error ? error.message : String(error);
        logger.error('Config hot-reload check failed', { error: msg });
      }
    }, intervalMs);
  }

  private async checkForChanges(): Promise<void> {
    const rows = await this.db.query<GameConfig>(
      'SELECT * FROM game_config WHERE updated_at > ?',
      [this.lastCheck]
    );

    if (rows.length > 0) {
      for (const row of rows) {
        const oldValue = this.configCache.get(row.config_key);
        const newValue = row.config_value;

        if (JSON.stringify(oldValue) !== JSON.stringify(newValue)) {
          this.configCache.set(row.config_key, newValue);
          this.emit('configChanged', { key: row.config_key, oldValue, newValue, updatedBy: row.updated_by });
          logger.info('Config hot-reloaded', { key: row.config_key, oldValue, newValue });
        }
      }
      this.lastCheck = new Date();
    }
  }

  stopHotReload(): void {
    if (this.watchInterval) {
      clearInterval(this.watchInterval);
      this.watchInterval = null;
    }
  }

  // Convenience getters for common configs
  getElixirRates(): { normal: number; double: number; triple: number } {
    return this.getConfig('elixir_generation_rate', { normal: 2.8, double: 1.4, triple: 0.93 });
  }

  getMaxElixir(): number {
    return this.getConfig('max_elixir', 10);
  }

  getStartingElixir(): number {
    return this.getConfig('starting_elixir', 5);
  }

  getBattleDuration(): number {
    return this.getConfig('battle_duration_seconds', 180);
  }

  getOvertimeDuration(): number {
    return this.getConfig('overtime_duration_seconds', 180);
  }

  getChestSlots(): number {
    return this.getConfig('chest_slots', 4);
  }

  getMaxDeckCards(): number {
    return this.getConfig('max_deck_cards', 8);
  }

  getMaxChampionsPerDeck(): number {
    return this.getConfig('max_champions_per_deck', 1);
  }

  getHandSize(): number {
    return this.getConfig('hand_size', 4);
  }

  getTournamentStandardLevel(): number {
    return this.getConfig('tournament_standard_level', 11);
  }

  getChestCycle(): any[] {
    return this.getConfig('chest_cycle', []);
  }

  getDonationLimits(): any {
    return this.getConfig('donation_limits', { common: 10, rare: 1, epic: 0, legendary: 0, champion: 0 });
  }

  getDonationRewards(): any {
    return this.getConfig('donation_rewards', {});
  }

  getTrophyFormula(): any {
    return this.getConfig('trophy_formula', { base: 30, diff_factor: 100, min: 15, max: 45, overtime_bonus: 5 });
  }

  getXpPerLevel(): number[] {
    return this.getConfig('xp_per_level', [0, 100, 200, 400, 800, 1600, 3200, 6400, 12800, 25600, 51200, 102400, 204800, 409600, 819200]);
  }

  // Calculate trophy change based on formula
  calculateTrophyChange(winnerTrophies: number, loserTrophies: number, isOvertime: boolean): number {
    const formula = this.getTrophyFormula();
    const diff = winnerTrophies - loserTrophies;
    let change = formula.base - Math.floor(diff / formula.diff_factor);

    if (change > formula.max) change = formula.max;
    if (change < formula.min) change = formula.min;

    if (isOvertime) {
      change += formula.overtime_bonus;
    }

    return change;
  }

  // Get XP required for level
  getXpForLevel(level: number): number {
    const xpTable = this.getXpPerLevel();
    return xpTable[level] || xpTable[xpTable.length - 1];
  }

  // Calculate level from XP
  getLevelFromXp(xp: number): number {
    const xpTable = this.getXpPerLevel();
    for (let i = xpTable.length - 1; i >= 0; i--) {
      if (xp >= xpTable[i]) return i;
    }
    return 1;
  }

  // Broadcast config to battle servers (would use Redis pub/sub in production)
  broadcastConfigChange(key: string, value: any): void {
    // In production: publish to Redis channel for battle servers
    this.emit('broadcast', { key, value });
  }
}

// Singleton instance
export const configService = new ConfigService(db);