import { BaseRepository } from './BaseRepository';
import { Database, db } from './Database';
import { logger } from '../utils/logger';

export interface ShopOfferRow {
  offer_id: number;
  offer_type: string;
  cost_gems: number;
  cost_gold: number;
  cost_real_money: number;
  rewards: any;
  purchase_limit: number;
  per_player_limit: number;
  starts_at: Date;
  ends_at: Date;
  display_order: number;
  banner_image: string | null;
  is_enabled: boolean;
  created_at: Date;
  updated_at: Date;
}

export interface PlayerPurchaseRow {
  purchase_id: number;
  player_id: string;
  offer_id: number;
  currency_type: string;
  amount_spent: number;
  transaction_id: string | null;
  platform: string | null;
  receipt_data: string | null;
  is_verified: boolean;
  purchased_at: Date;
}

export interface PlayerPurchaseLimitRow {
  player_id: string;
  offer_id: number;
  purchase_count: number;
  last_purchase_at: Date | null;
  period_start: Date;
}

export class ShopRepository extends BaseRepository {
  constructor(database: Database = db) {
    super(database);
  }

  // Shop Offers
  async createOffer(offer: Omit<ShopOfferRow, 'offer_id' | 'created_at' | 'updated_at'>): Promise<number> {
    const result = await this.execute(
      `INSERT INTO shop_offers (offer_type, cost_gems, cost_gold, cost_real_money, rewards,
        purchase_limit, per_player_limit, starts_at, ends_at, display_order, banner_image, is_enabled)
       VALUES (?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?)`,
      [
        offer.offer_type,
        offer.cost_gems,
        offer.cost_gold,
        offer.cost_real_money,
        JSON.stringify(offer.rewards),
        offer.purchase_limit,
        offer.per_player_limit,
        offer.starts_at,
        offer.ends_at,
        offer.display_order,
        offer.banner_image,
        offer.is_enabled
      ]
    );
    return result.insertId;
  }

  async getOffer(offerId: number): Promise<ShopOfferRow | null> {
    const rows = await this.query<ShopOfferRow>('SELECT * FROM shop_offers WHERE offer_id = ?', [offerId]);
    if (rows[0]) {
      rows[0].rewards = JSON.parse(rows[0].rewards);
    }
    return rows[0] || null;
  }

  async getActiveOffers(offerType?: string): Promise<ShopOfferRow[]> {
    const now = new Date();
    let sql = 'SELECT * FROM shop_offers WHERE is_enabled = TRUE AND starts_at <= ? AND ends_at >= ?';
    const params: any[] = [now, now];

    if (offerType) {
      sql += ' AND offer_type = ?';
      params.push(offerType);
    }

    sql += ' ORDER BY display_order, offer_id';
    const rows = await this.query<ShopOfferRow>(sql, params);

    for (const row of rows) {
      row.rewards = JSON.parse(row.rewards as string);
    }

    return rows;
  }

  async getDailyOffers(): Promise<ShopOfferRow[]> {
    return this.getActiveOffers('daily');
  }

  async getSpecialOffers(): Promise<ShopOfferRow[]> {
    return this.getActiveOffers('special');
  }

  async getAllOffers(includeInactive: boolean = false): Promise<ShopOfferRow[]> {
    let sql = 'SELECT * FROM shop_offers';
    if (!includeInactive) {
      sql += ' WHERE is_enabled = TRUE';
    }
    sql += ' ORDER BY offer_type, display_order, offer_id';

    const rows = await this.query<ShopOfferRow>(sql);

    for (const row of rows) {
      row.rewards = JSON.parse(row.rewards as string);
    }

    return rows;
  }

  async updateOffer(offerId: number, data: Partial<ShopOfferRow>): Promise<void> {
    const { set, params } = this.buildUpdateClause({
      ...data,
      rewards: data.rewards ? JSON.stringify(data.rewards) : undefined
    });
    if (!set) return;
    params.push(offerId);
    await this.execute(`UPDATE shop_offers SET ${set}, updated_at = NOW() WHERE offer_id = ?`, params);
  }

  async deleteOffer(offerId: number): Promise<void> {
    await this.execute('DELETE FROM shop_offers WHERE offer_id = ?', [offerId]);
  }

  // Player Purchases
  async recordPurchase(purchase: Omit<PlayerPurchaseRow, 'purchase_id' | 'purchased_at'>): Promise<number> {
    const result = await this.execute(
      `INSERT INTO player_purchases (player_id, offer_id, currency_type, amount_spent, transaction_id, platform, receipt_data, is_verified)
       VALUES (?, ?, ?, ?, ?, ?, ?, ?)`,
      [
        purchase.player_id,
        purchase.offer_id,
        purchase.currency_type,
        purchase.amount_spent,
        purchase.transaction_id,
        purchase.platform,
        purchase.receipt_data,
        purchase.is_verified
      ]
    );

    // Update purchase limit tracking
    await this.incrementPurchaseLimit(purchase.player_id, purchase.offer_id);

    return result.insertId;
  }

  async getPurchase(purchaseId: number): Promise<PlayerPurchaseRow | null> {
    const rows = await this.query<PlayerPurchaseRow>('SELECT * FROM player_purchases WHERE purchase_id = ?', [purchaseId]);
    return rows[0] || null;
  }

  async getPlayerPurchases(playerId: string, limit: number = 50): Promise<PlayerPurchaseRow[]> {
    return this.query<PlayerPurchaseRow>(
      'SELECT * FROM player_purchases WHERE player_id = ? ORDER BY purchased_at DESC LIMIT ?',
      [playerId, limit]
    );
  }

  async getPlayerPurchasesByOffer(playerId: string, offerId: number): Promise<PlayerPurchaseRow[]> {
    return this.query<PlayerPurchaseRow>(
      'SELECT * FROM player_purchases WHERE player_id = ? AND offer_id = ? ORDER BY purchased_at DESC',
      [playerId, offerId]
    );
  }

  async verifyPurchase(purchaseId: number, verified: boolean = true): Promise<void> {
    await this.execute(
      'UPDATE player_purchases SET is_verified = ? WHERE purchase_id = ?',
      [verified, purchaseId]
    );
  }

  async getPurchaseByTransaction(transactionId: string): Promise<PlayerPurchaseRow | null> {
    const rows = await this.query<PlayerPurchaseRow>(
      'SELECT * FROM player_purchases WHERE transaction_id = ?',
      [transactionId]
    );
    return rows[0] || null;
  }

  // Purchase Limits
  async getPurchaseLimit(playerId: string, offerId: number): Promise<PlayerPurchaseLimitRow | null> {
    const rows = await this.query<PlayerPurchaseLimitRow>(
      'SELECT * FROM player_purchase_limits WHERE player_id = ? AND offer_id = ?',
      [playerId, offerId]
    );
    return rows[0] || null;
  }

  async incrementPurchaseLimit(playerId: string, offerId: number): Promise<void> {
    await this.execute(
      `INSERT INTO player_purchase_limits (player_id, offer_id, purchase_count, last_purchase_at, period_start)
       VALUES (?, ?, 1, NOW(), NOW())
       ON DUPLICATE KEY UPDATE purchase_count = purchase_count + 1, last_purchase_at = NOW()`,
      [playerId, offerId]
    );
  }

  async resetPurchaseLimits(offerId?: number): Promise<void> {
    let sql = 'UPDATE player_purchase_limits SET purchase_count = 0, period_start = NOW()';
    const params: any[] = [];

    if (offerId) {
      sql += ' WHERE offer_id = ?';
      params.push(offerId);
    }

    await this.execute(sql, params);
  }

  async canPurchaseOffer(playerId: string, offerId: number): Promise<{allowed: boolean, reason?: string}> {
    const offer = await this.getOffer(offerId);
    if (!offer) return { allowed: false, reason: 'OFFER_NOT_FOUND' };
    if (!offer.is_enabled) return { allowed: false, reason: 'OFFER_DISABLED' };

    const now = new Date();
    if (offer.starts_at > now || offer.ends_at < now) {
      return { allowed: false, reason: 'OFFER_EXPIRED' };
    }

    // Check global purchase limit
    if (offer.purchase_limit > 0) {
      const totalPurchases = await this.getTotalPurchases(offerId);
      if (totalPurchases >= offer.purchase_limit) {
        return { allowed: false, reason: 'GLOBAL_LIMIT_REACHED' };
      }
    }

    // Check per-player limit
    if (offer.per_player_limit > 0) {
      const limit = await this.getPurchaseLimit(playerId, offerId);
      if (limit && limit.purchase_count >= offer.per_player_limit) {
        return { allowed: false, reason: 'PLAYER_LIMIT_REACHED' };
      }
    }

    return { allowed: true };
  }

  private async getTotalPurchases(offerId: number): Promise<number> {
    const rows = await this.query<{count: number}>(
      'SELECT COUNT(*) as count FROM player_purchases WHERE offer_id = ?',
      [offerId]
    );
    return rows[0]?.count || 0;
  }

  // IAP Receipt Verification (store receipts for server-side validation)
  async storeIAPReceipt(playerId: string, offerId: number, platform: string, transactionId: string, receiptData: string): Promise<number> {
    const result = await this.execute(
      `INSERT INTO player_purchases (player_id, offer_id, currency_type, amount_spent, transaction_id, platform, receipt_data, is_verified)
       VALUES (?, ?, 'real_money', 0, ?, ?, ?, FALSE)`,
      [playerId, offerId, transactionId, platform, receiptData]
    );
    return result.insertId;
  }

  async getUnverifiedIAPPurchases(): Promise<PlayerPurchaseRow[]> {
    return this.query<PlayerPurchaseRow>(
      'SELECT * FROM player_purchases WHERE currency_type = "real_money" AND is_verified = FALSE ORDER BY purchased_at'
    );
  }

  // Offer Rotation (for daily offers)
  async rotateDailyOffers(newOffers: Omit<ShopOfferRow, 'offer_id' | 'created_at' | 'updated_at'>[]): Promise<void> {
    await this.transaction(async (conn) => {
      // Disable current daily offers
      await conn.execute('UPDATE shop_offers SET is_enabled = FALSE WHERE offer_type = "daily"');

      // Insert new daily offers
      for (const offer of newOffers) {
        await conn.execute(
          `INSERT INTO shop_offers (offer_type, cost_gems, cost_gold, cost_real_money, rewards,
            purchase_limit, per_player_limit, starts_at, ends_at, display_order, banner_image, is_enabled)
           VALUES (?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?)`,
          [
            offer.offer_type,
            offer.cost_gems,
            offer.cost_gold,
            offer.cost_real_money,
            JSON.stringify(offer.rewards),
            offer.purchase_limit,
            offer.per_player_limit,
            offer.starts_at,
            offer.ends_at,
            offer.display_order,
            offer.banner_image,
            offer.is_enabled
          ]
        );
      }
    });
  }

  // Statistics
  async getShopStats(): Promise<{
    totalOffers: number;
    activeOffers: number;
    totalRevenueGems: number;
    totalRevenueGold: number;
    totalRevenueRealMoney: number;
    purchasesToday: number;
  }> {
    const today = new Date();
    today.setHours(0, 0, 0, 0);

    const [offerStats] = await this.query<any>(
      `SELECT COUNT(*) as totalOffers, SUM(CASE WHEN is_enabled = TRUE THEN 1 ELSE 0 END) as activeOffers FROM shop_offers`
    );

    const [revenueStats] = await this.query<any>(
      `SELECT 
        SUM(CASE WHEN currency_type = 'gems' THEN amount_spent ELSE 0 END) as totalRevenueGems,
        SUM(CASE WHEN currency_type = 'gold' THEN amount_spent ELSE 0 END) as totalRevenueGold,
        SUM(CASE WHEN currency_type = 'real_money' THEN amount_spent ELSE 0 END) as totalRevenueRealMoney
       FROM player_purchases`
    );

    const [todayStats] = await this.query<any>(
      `SELECT COUNT(*) as purchasesToday FROM player_purchases WHERE purchased_at >= ?`,
      [today]
    );

    return {
      totalOffers: offerStats.totalOffers,
      activeOffers: offerStats.activeOffers,
      totalRevenueGems: revenueStats.totalRevenueGems || 0,
      totalRevenueGold: revenueStats.totalRevenueGold || 0,
      totalRevenueRealMoney: revenueStats.totalRevenueRealMoney || 0,
      purchasesToday: todayStats.purchasesToday || 0
    };
  }
}