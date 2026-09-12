import { Database, db } from '../persistence/Database';
import { logger } from '../utils/logger';
import { QuestRepository, QuestRow, PlayerQuestRow } from '../persistence/QuestRepository';
import { PlayerService } from './PlayerService';

export interface Quest {
  questId: number;
  nameKey: string;
  descriptionKey: string;
  questType: 'daily' | 'weekly' | 'seasonal' | 'achievement' | 'tutorial';
  requirements: any;
  rewards: any;
  startsAt: Date | null;
  endsAt: Date | null;
  resetCron: string | null;
  xpReward: number;
  isEnabled: boolean;
}

export interface PlayerQuestProgress {
  playerId: string;
  questId: number;
  progress: { current: number; target: number };
  status: 'active' | 'completed' | 'claimed' | 'expired';
  startedAt: Date;
  completedAt: Date | null;
  claimedAt: Date | null;
}

export class QuestService {
  private db: Database;
  private questRepo: QuestRepository;
  private playerService: PlayerService;

  constructor(database: Database = db, playerService?: PlayerService) {
    this.db = database;
    this.questRepo = new QuestRepository(database);
    this.playerService = playerService || new PlayerService(database);
  }

  private mapQuest(row: QuestRow): Quest {
    return {
      questId: row.quest_id,
      nameKey: row.name_key || '',
      descriptionKey: row.description_key || '',
      questType: row.quest_type as any,
      requirements: row.requirements,
      rewards: row.rewards,
      startsAt: row.starts_at,
      endsAt: row.ends_at,
      resetCron: row.reset_cron,
      xpReward: row.xp_reward,
      isEnabled: row.is_enabled
    };
  }

  async getActiveQuests(questType?: string): Promise<Quest[]> {
    return (await this.questRepo.getActiveQuests(questType as any)).map(r => this.mapQuest(r));
  }

  async getQuestsByType(questType: string): Promise<Quest[]> {
    return (await this.questRepo.getQuestsByType(questType)).map(r => this.mapQuest(r));
  }

  async getQuest(questId: number): Promise<Quest | null> {
    const quest = await this.questRepo.getQuest(questId);
    return quest ? this.mapQuest(quest) : null;
  }

  async getPlayerQuests(playerId: string, status?: string): Promise<PlayerQuestProgress[]> {
    const quests = await this.questRepo.getPlayerQuests(playerId, status);
    return quests.map(q => ({
      playerId: q.player_id,
      questId: q.quest_id,
      progress: q.progress || { current: 0, target: 0 },
      status: q.status as any,
      startedAt: q.started_at,
      completedAt: q.completed_at,
      claimedAt: q.claimed_at
    }));
  }

  async getPlayerQuest(playerId: string, questId: number): Promise<PlayerQuestProgress | null> {
    const quest = await this.questRepo.getPlayerQuest(playerId, questId);
    if (!quest) return null;
    return {
      playerId: quest.player_id,
      questId: quest.quest_id,
      progress: quest.progress || { current: 0, target: 0 },
      status: quest.status as any,
      startedAt: quest.started_at,
      completedAt: quest.completed_at,
      claimedAt: quest.claimed_at
    };
  }

  async onBattleEnd(playerId: string, battleData: { battleType: string; isWin: boolean; cardsPlayed: number[]; towersDestroyed: number; trophyChange: number }): Promise<void> {
    const activeQuests = await this.getPlayerQuests(playerId, 'active');
    for (const pq of activeQuests) {
      const quest = await this.getQuest(pq.questId);
      if (!quest) continue;
      let progressAmount = 0;
      switch (quest.requirements.type) {
        case 'win_battles':
          if (battleData.isWin && (!quest.requirements.mode || quest.requirements.mode === battleData.battleType)) progressAmount = 1;
          break;
        case 'play_cards':
          progressAmount = battleData.cardsPlayed.length;
          break;
        case 'destroy_towers':
          progressAmount = battleData.towersDestroyed;
          break;
        case 'earn_trophies':
          if (battleData.trophyChange > 0) progressAmount = battleData.trophyChange;
          break;
      }
      if (progressAmount > 0) await this.questRepo.incrementQuestProgress(playerId, pq.questId, progressAmount);
    }
  }

  async onDonation(playerId: string, cardId: number, count: number): Promise<void> {
    const activeQuests = await this.getPlayerQuests(playerId, 'active');
    for (const pq of activeQuests) {
      const quest = await this.getQuest(pq.questId);
      if (!quest) continue;
      if (quest.requirements.type === 'donate_cards') await this.questRepo.incrementQuestProgress(playerId, pq.questId, count);
    }
  }

  async onChestOpened(playerId: string): Promise<void> {
    const activeQuests = await this.getPlayerQuests(playerId, 'active');
    for (const pq of activeQuests) {
      const quest = await this.getQuest(pq.questId);
      if (!quest) continue;
      if (quest.requirements.type === 'open_chests') await this.questRepo.incrementQuestProgress(playerId, pq.questId, 1);
    }
  }

  async onCardUpgrade(playerId: string): Promise<void> {
    const activeQuests = await this.getPlayerQuests(playerId, 'active');
    for (const pq of activeQuests) {
      const quest = await this.getQuest(pq.questId);
      if (!quest) continue;
      if (quest.requirements.type === 'upgrade_cards') await this.questRepo.incrementQuestProgress(playerId, pq.questId, 1);
    }
  }

  async onTrophyChange(playerId: string, newTrophies: number): Promise<void> {
    const activeQuests = await this.getPlayerQuests(playerId, 'active');
    for (const pq of activeQuests) {
      const quest = await this.getQuest(pq.questId);
      if (!quest) continue;
      if (quest.requirements.type === 'earn_trophies' && quest.requirements.count <= newTrophies) {
        await this.questRepo.setQuestProgress(playerId, pq.questId, newTrophies);
      }
    }
  }

  async claimQuestReward(playerId: string, questId: number): Promise<any[] | null> {
    const pq = await this.questRepo.getPlayerQuest(playerId, questId);
    if (!pq || pq.status !== 'completed') return null;
    const quest = await this.getQuest(questId);
    if (!quest) return null;
    await this.grantQuestRewards(playerId, quest.rewards);
    await this.questRepo.claimQuest(playerId, questId);
    return quest.rewards;
  }

  private async grantQuestRewards(playerId: string, rewards: any[]): Promise<void> {
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

  async initializePlayerQuests(playerId: string): Promise<void> {
    const activeQuests = await this.getActiveQuests();
    for (const quest of activeQuests) {
      if (quest.questType === 'tutorial' || quest.questType === 'daily') {
        await this.questRepo.startQuest(playerId, quest.questId, { current: 0, target: quest.requirements.count });
      }
    }
  }

  async resetDailyQuests(): Promise<number> { return this.questRepo.resetDailyQuests(); }
  async resetWeeklyQuests(): Promise<number> { return this.questRepo.resetWeeklyQuests(); }
}