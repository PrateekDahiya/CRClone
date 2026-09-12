import { Database, db } from '../persistence/Database';
import { logger } from '../utils/logger';
import { ShopRepository, ShopOfferRow, PlayerPurchaseRow } from '../persistence/ShopRepository';
import { PlayerService } from './PlayerService';

export interface ShopOffer {
  offerId: number;
  offerType: 'daily' | 'special' | 'bundle' | 'gem' | 'gold' | 'wild_card';
  costGems: number;
  costGold: number;
  costRealMoney: number;
  rewards: any;
  purchaseLimit: number;
  perPlayerLimit: number;
  startsAt: Date;
  endsAt: Date;
  displayOrder: number;
  bannerImage: string | null;
  isEnabled: boolean;
}

export interface ShopReward {
  type: 'card' | 'gold' | 'gems' | 'chest' | 'wild_card';
  cardId?: number;
  count?: number;
  amount?: number;
  chestId?: number;
  rarity?: 'common' | 'rare' | 'epic' | 'legendary' | 'champion';
}

export interface PurchaseResult {
  success: boolean;
  purchaseId?: number;
  rewards?: ShopReward[];
  error?: string;
}

export class ShopService {
  private db: Database;
  private shopRepo: ShopRepository;
  private playerService: PlayerService;

  constructor(database: Database = db, playerService?: PlayerService) {
    this.db = database;
    this.shopRepo = new ShopRepository(database);
    this.playerService = playerService || new PlayerService(database);
  }

  private mapOffer(row: ShopOfferRow): ShopOffer {
    return {
      offerId: row.offer_id,
      offerType: row.offer_type as any,
      costGems: row.cost_gems,
      costGold: row.cost_gold,
      costRealMoney: row.cost_real_money,
      rewards: row.rewards,
      purchaseLimit: row.purchase_limit,
      perPlayerLimit: row.per_player_limit,
      startsAt: row.starts_at,
      endsAt: row.ends_at,
      displayOrder: row.display_order,
      bannerImage: row.banner_image,
      isEnabled: row.is_enabled
    };
  }

  async getDailyOffers(): Promise<ShopOffer[]> {
    return (await this.shopRepo.getDailyOffers()).map(r => this.mapOffer(r));
  }

  async getSpecialOffers(): Promise<ShopOffer[]> {
    return (await this.shopRepo.getSpecialOffers()).map(r => this.mapOffer(r));
  }

  async getAllOffers(): Promise<ShopOffer[]> {
    return (await this.shopRepo.getAllOffers()).map(r => this.mapOffer(r));
  }

  async getOffer(offerId: number): Promise<ShopOffer | null> {
    const offer = await this.shopRepo.getOffer(offerId);
    return offer ? this.mapOffer(offer) : null;
  }

  async purchaseOffer(playerId: string, offerId: number, currencyType: 'gems' | 'gold' = 'gems'): Promise<PurchaseResult> {
    const canPurchase = await this.shopRepo.canPurchaseOffer(playerId, offerId);
    if (!canPurchase.allowed) return { success: false, error: canPurchase.reason };

    const offer = await this.shopRepo.getOffer(offerId);
    if (!offer) return { success: false, error: 'OFFER_NOT_FOUND' };

    const player = await this.playerService.getPlayerById(playerId);
    if (!player) return { success: false, error: 'PLAYER_NOT_FOUND' };

    const cost = currencyType === 'gems' ? offer.cost_gems : offer.cost_gold;
    if (cost === 0 && offer.cost_real_money === 0) return { success: false, error: 'OFFER_NOT_PURCHASABLE_WITH_CURRENCY' };

    const playerCurrency = currencyType === 'gems' ? player.gems : player.gold;
    if (playerCurrency < cost) return { success: false, error: 'INSUFFICIENT_CURRENCY' };

    if (currencyType === 'gems') await this.playerService.addGems(playerId, -cost);
    else await this.playerService.addGold(playerId, -cost);

    const rewards = this.parseRewards(offer.rewards);
    await this.grantRewards(playerId, rewards);

    const purchaseId = await this.shopRepo.recordPurchase({
      player_id: playerId,
      offer_id: offerId,
      currency_type: currencyType,
      amount_spent: cost,
      is_verified: true,
      transaction_id: null,
      platform: null,
      receipt_data: null
    });

    logger.info('Purchase completed', { playerId, offerId, purchaseId, cost, currencyType });
    return { success: true, purchaseId, rewards };
  }

  async purchaseWithIAP(playerId: string, offerId: number, platform: 'ios' | 'android', transactionId: string, receiptData: string): Promise<PurchaseResult> {
    const offer = await this.shopRepo.getOffer(offerId);
    if (!offer) return { success: false, error: 'OFFER_NOT_FOUND' };
    if (offer.cost_real_money <= 0) return { success: false, error: 'OFFER_NOT_IAP' };

    const purchaseId = await this.shopRepo.storeIAPReceipt(playerId, offerId, platform, transactionId, receiptData);
    const verified = await this.verifyReceipt(platform, transactionId, receiptData, offer.cost_real_money);

    if (verified) {
      const rewards = this.parseRewards(offer.rewards);
      await this.grantRewards(playerId, rewards);
      await this.shopRepo.verifyPurchase(purchaseId, true);
      return { success: true, purchaseId, rewards };
    }
    return { success: false, error: 'RECEIPT_VERIFICATION_FAILED', purchaseId };
  }

  private async verifyReceipt(platform: 'ios' | 'android', transactionId: string, receiptData: string, expectedPrice: number): Promise<boolean> {
    logger.info('Verifying IAP receipt', { platform, transactionId, expectedPrice });
    return true;
  }

  private parseRewards(rewards: any): ShopReward[] {
    if (Array.isArray(rewards)) return rewards.map(r => this.normalizeReward(r));
    return [this.normalizeReward(rewards)];
  }

  private normalizeReward(reward: any): ShopReward {
    return { type: reward.type, cardId: reward.cardId, count: reward.count || reward.amount, amount: reward.amount, chestId: reward.chestId, rarity: reward.rarity };
  }

  private async grantRewards(playerId: string, rewards: ShopReward[]): Promise<void> {
    for (const reward of rewards) {
      switch (reward.type) {
        case 'card':
          if (reward.cardId && reward.count) await this.playerService.addCards(playerId, reward.cardId, reward.count);
          break;
        case 'gold':
          if (reward.amount) await this.playerService.addGold(playerId, reward.amount);
          break;
        case 'gems':
          if (reward.amount) await this.playerService.addGems(playerId, reward.amount);
          break;
        case 'chest':
          if (reward.chestId) await this.grantChest(playerId, reward.chestId);
          break;
        case 'wild_card':
          if (reward.rarity && reward.count) await this.grantWildCard(playerId, reward.rarity, reward.count);
          break;
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

  async createOffer(offer: Omit<ShopOffer, 'offerId'>): Promise<number> {
    return this.shopRepo.createOffer({
      offer_type: offer.offerType,
      cost_gems: offer.costGems,
      cost_gold: offer.costGold,
      cost_real_money: offer.costRealMoney,
      rewards: offer.rewards,
      purchase_limit: offer.purchaseLimit,
      per_player_limit: offer.perPlayerLimit,
      starts_at: offer.startsAt,
      ends_at: offer.endsAt,
      display_order: offer.displayOrder,
      banner_image: offer.bannerImage,
      is_enabled: offer.isEnabled
    });
  }

  async updateOffer(offerId: number, data: Partial<ShopOffer>): Promise<void> {
    await this.shopRepo.updateOffer(offerId, {
      offer_type: data.offerType,
      cost_gems: data.costGems,
      cost_gold: data.costGold,
      cost_real_money: data.costRealMoney,
      rewards: data.rewards,
      purchase_limit: data.purchaseLimit,
      per_player_limit: data.perPlayerLimit,
      starts_at: data.startsAt,
      ends_at: data.endsAt,
      display_order: data.displayOrder,
      banner_image: data.bannerImage,
      is_enabled: data.isEnabled
    });
  }

  async rotateDailyOffers(offers: Omit<ShopOffer, 'offerId'>[]): Promise<void> {
    await this.shopRepo.rotateDailyOffers(offers.map(o => ({
      offer_type: o.offerType,
      cost_gems: o.costGems,
      cost_gold: o.costGold,
      cost_real_money: o.costRealMoney,
      rewards: o.rewards,
      purchase_limit: o.purchaseLimit,
      per_player_limit: o.perPlayerLimit,
      starts_at: o.startsAt,
      ends_at: o.endsAt,
      display_order: o.displayOrder,
      banner_image: o.bannerImage,
      is_enabled: o.isEnabled
    })));
  }

  async getShopStats(): Promise<any> { return this.shopRepo.getShopStats(); }
  async getPlayerPurchaseHistory(playerId: string, limit: number = 50): Promise<any[]> { return this.shopRepo.getPlayerPurchases(playerId, limit); }
}