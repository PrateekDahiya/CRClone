import { BaseRepository } from './BaseRepository';
import { Database, db } from './Database';
import { logger } from '../utils/logger';

export interface QuestRow {
  quest_id: number;
  name_key: string | null;
  description_key: string | null;
  quest_type: string;
  requirements: any;
  rewards: any;
  starts_at: Date | null;
  ends_at: Date | null;
  reset_cron: string | null;
  xp_reward: number;
  is_enabled: boolean;
  is_repeatable: boolean;
  sort_order: number;
}

export interface PlayerQuestRow {
  player_id: string;
  quest_id: number;
  progress: any;
  status: string;
  started_at: Date;
  completed_at: Date | null;
  claimed_at: Date | null;
}

export class QuestRepository extends BaseRepository {
  constructor(database: Database = db) {
    super(database);
  }

  // Quest Definitions
  async createQuest(quest: Omit<QuestRow, 'quest_id'> & { quest_id: number }): Promise<number> {
    const result = await this.execute(
      `INSERT INTO quests (quest_id, name_key, description_key, quest_type, requirements, rewards, starts_at, ends_at, reset_cron, xp_reward, is_enabled, is_repeatable, sort_order)
       VALUES (?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?)`,
      [
        quest.quest_id,
        quest.name_key,
        quest.description_key,
        quest.quest_type,
        JSON.stringify(quest.requirements),
        JSON.stringify(quest.rewards),
        quest.starts_at,
        quest.ends_at,
        quest.reset_cron,
        quest.xp_reward,
        quest.is_enabled,
        quest.is_repeatable,
        quest.sort_order
      ]
    );
    return quest.quest_id;
  }

  async getQuest(questId: number): Promise<QuestRow | null> {
    const rows = await this.query<QuestRow>('SELECT * FROM quests WHERE quest_id = ?', [questId]);
    if (rows[0]) {
      rows[0].requirements = JSON.parse(rows[0].requirements);
      rows[0].rewards = JSON.parse(rows[0].rewards);
    }
    return rows[0] || null;
  }

  async getQuestsByType(questType: string, includeInactive: boolean = false): Promise<QuestRow[]> {
    let sql = 'SELECT * FROM quests WHERE quest_type = ?';
    const params: any[] = [questType];

    if (!includeInactive) {
      sql += ' AND is_enabled = TRUE';
      const now = new Date();
      sql += ' AND (starts_at IS NULL OR starts_at <= ?)';
      sql += ' AND (ends_at IS NULL OR ends_at >= ?)';
      params.push(now, now);
    }

    sql += ' ORDER BY sort_order, quest_id';
    const rows = await this.query<QuestRow>(sql, params);

    for (const row of rows) {
      row.requirements = JSON.parse(row.requirements as string);
      row.rewards = JSON.parse(row.rewards as string);
    }

    return rows;
  }

  async getActiveQuests(questType?: string): Promise<QuestRow[]> {
    let sql = 'SELECT * FROM quests WHERE is_enabled = TRUE';
    const params: any[] = [];

    const now = new Date();
    sql += ' AND (starts_at IS NULL OR starts_at <= ?)';
    sql += ' AND (ends_at IS NULL OR ends_at >= ?)';
    params.push(now, now);

    if (questType) {
      sql += ' AND quest_type = ?';
      params.push(questType);
    }

    sql += ' ORDER BY sort_order, quest_id';
    const rows = await this.query<QuestRow>(sql, params);

    for (const row of rows) {
      row.requirements = JSON.parse(row.requirements as string);
      row.rewards = JSON.parse(row.rewards as string);
    }

    return rows;
  }

  async updateQuest(questId: number, data: Partial<QuestRow>): Promise<void> {
    const { set, params } = this.buildUpdateClause({
      ...data,
      requirements: data.requirements ? JSON.stringify(data.requirements) : undefined,
      rewards: data.rewards ? JSON.stringify(data.rewards) : undefined
    });
    if (!set) return;
    params.push(questId);
    await this.execute(`UPDATE quests SET ${set} WHERE quest_id = ?`, params);
  }

  async deleteQuest(questId: number): Promise<void> {
    await this.execute('DELETE FROM quests WHERE quest_id = ?', [questId]);
  }

  // Player Quest Progress
  async getPlayerQuests(playerId: string, status?: string): Promise<PlayerQuestRow[]> {
    let sql = 'SELECT * FROM player_quests WHERE player_id = ?';
    const params: any[] = [playerId];

    if (status) {
      sql += ' AND status = ?';
      params.push(status);
    }

    sql += ' ORDER BY started_at DESC';
    const rows = await this.query<PlayerQuestRow>(sql, params);

    for (const row of rows) {
      row.progress = JSON.parse(row.progress as string);
    }

    return rows;
  }

  async getPlayerQuest(playerId: string, questId: number): Promise<PlayerQuestRow | null> {
    const rows = await this.query<PlayerQuestRow>(
      'SELECT * FROM player_quests WHERE player_id = ? AND quest_id = ?',
      [playerId, questId]
    );
    if (rows[0]) {
      rows[0].progress = JSON.parse(rows[0].progress as string);
    }
    return rows[0] || null;
  }

  async startQuest(playerId: string, questId: number, initialProgress: any = {}): Promise<void> {
    await this.execute(
      `INSERT INTO player_quests (player_id, quest_id, progress, status)
       VALUES (?, ?, ?, 'active')
       ON DUPLICATE KEY UPDATE progress = VALUES(progress), status = 'active', started_at = NOW()`,
      [playerId, questId, JSON.stringify(initialProgress)]
    );
  }

  async updateQuestProgress(playerId: string, questId: number, progress: any): Promise<void> {
    await this.execute(
      'UPDATE player_quests SET progress = ? WHERE player_id = ? AND quest_id = ?',
      [JSON.stringify(progress), playerId, questId]
    );
  }

  async completeQuest(playerId: string, questId: number): Promise<void> {
    await this.execute(
      'UPDATE player_quests SET status = "completed", completed_at = NOW() WHERE player_id = ? AND quest_id = ?',
      [playerId, questId]
    );
  }

  async claimQuest(playerId: string, questId: number): Promise<void> {
    await this.execute(
      'UPDATE player_quests SET status = "claimed", claimed_at = NOW() WHERE player_id = ? AND quest_id = ?',
      [playerId, questId]
    );
  }

  async expireQuest(playerId: string, questId: number): Promise<void> {
    await this.execute(
      'UPDATE player_quests SET status = "expired" WHERE player_id = ? AND quest_id = ?',
      [playerId, questId]
    );
  }

  async resetDailyQuests(): Promise<number> {
    const result = await this.execute(
      `UPDATE player_quests pq
       JOIN quests q ON pq.quest_id = q.quest_id
       SET pq.status = 'active', pq.progress = '{}', pq.started_at = NOW(), pq.completed_at = NULL, pq.claimed_at = NULL
       WHERE q.quest_type = 'daily' AND q.reset_cron IS NOT NULL`
    );
    return result.affectedRows;
  }

  async resetWeeklyQuests(): Promise<number> {
    const result = await this.execute(
      `UPDATE player_quests pq
       JOIN quests q ON pq.quest_id = q.quest_id
       SET pq.status = 'active', pq.progress = '{}', pq.started_at = NOW(), pq.completed_at = NULL, pq.claimed_at = NULL
       WHERE q.quest_type = 'weekly' AND q.reset_cron IS NOT NULL`
    );
    return result.affectedRows;
  }

  async getQuestsNeedingReset(questType: string): Promise<QuestRow[]> {
    // This would be called by a cron job
    return this.getQuestsByType(questType, true);
  }

  // Progress tracking helpers
  async incrementQuestProgress(playerId: string, questId: number, amount: number = 1): Promise<PlayerQuestRow | null> {
    const playerQuest = await this.getPlayerQuest(playerId, questId);
    if (!playerQuest || playerQuest.status !== 'active') return null;

    const quest = await this.getQuest(questId);
    if (!quest) return null;

    const currentProgress = playerQuest.progress?.current || 0;
    const target = quest.requirements?.count || 1;
    const newProgress = Math.min(currentProgress + amount, target);

    await this.updateQuestProgress(playerId, questId, {...playerQuest.progress, current: newProgress});

    if (newProgress >= target) {
      await this.completeQuest(playerId, questId);
    }

    return this.getPlayerQuest(playerId, questId);
  }

  async setQuestProgress(playerId: string, questId: number, value: number): Promise<PlayerQuestRow | null> {
    const playerQuest = await this.getPlayerQuest(playerId, questId);
    if (!playerQuest || playerQuest.status !== 'active') return null;

    const quest = await this.getQuest(questId);
    if (!quest) return null;

    const target = quest.requirements?.count || 1;
    const newProgress = Math.min(Math.max(value, 0), target);

    await this.updateQuestProgress(playerId, questId, {...playerQuest.progress, current: newProgress});

    if (newProgress >= target) {
      await this.completeQuest(playerId, questId);
    }

    return this.getPlayerQuest(playerId, questId);
  }
}