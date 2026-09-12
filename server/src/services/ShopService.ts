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

// Minimal fetch shape so verification stays mockable in tests without new deps
// (Node 18+ provides global fetch; resolved at call time via globalThis).
type FetchFn = (input: any, init?: any) => Promise<{ ok: boolean; status: number; json: () => Promise<any> }>;

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
    if (!transactionId || !receiptData) {
      logger.warn('IAP verification failed: missing transactionId or receiptData', { platform });
      return false;
    }

    try {
      if (platform === 'ios') return await this.verifyAppleReceipt(transactionId, receiptData);
      if (platform === 'android') return await this.verifyGooglePlayPurchase(transactionId, receiptData, expectedPrice);
      logger.warn('IAP verification failed: unsupported platform', { platform, transactionId });
      return false;
    } catch (error) {
      // Fail closed: any verification error (network, parse, store outage) denies the grant.
      logger.warn('IAP verification error', { platform, transactionId, error: error instanceof Error ? error.message : String(error) });
      return false;
    }
  }

  /**
   * Fail-closed stub fallback for environments without live store credentials.
   * NEVER returns true in production. In non-production it only accepts
   * explicitly-marked sandbox-test fixtures (see isSandboxTestReceipt), so
   * forged receipts are rejected in every environment.
   */
  private stubVerifyReceipt(platform: string, transactionId: string, receiptData: string): boolean {
    if (process.env.NODE_ENV === 'production') return false;
    logger.warn('IAP verification stubbed — non-production only', { platform, transactionId });
    return this.isSandboxTestReceipt(transactionId, receiptData);
  }

  /**
   * Dev/test-only sandbox-test fixture check. Accepts receipts explicitly
   * marked as test fixtures bound to the transaction id; everything else
   * (including forged receipts) is rejected.
   */
  private isSandboxTestReceipt(transactionId: string, receiptData: string): boolean {
    if (!transactionId || typeof receiptData !== 'string') return false;
    if (receiptData === `SANDBOX_TEST_RECEIPT:${transactionId}`) return true;
    try {
      const parsed = JSON.parse(receiptData);
      return !!parsed && parsed.__sandboxTest === true && parsed.transactionId === transactionId;
    } catch {
      return false;
    }
  }

  private async verifyAppleReceipt(transactionId: string, receiptData: string): Promise<boolean> {
    const sharedSecret = process.env.APPLE_IAP_SHARED_SECRET || '';
    const fetchFn = (globalThis as any).fetch as FetchFn | undefined;

    // Without the shared secret (or fetch) we cannot verify against Apple —
    // fall back to the stub path, which is fail-closed in production.
    if (!sharedSecret || typeof fetchFn !== 'function') {
      return this.stubVerifyReceipt('ios', transactionId, receiptData);
    }

    const sandboxUrl = 'https://sandbox.itunes.apple.com/verifyReceipt';
    const productionUrl = 'https://buy.itunes.apple.com/verifyReceipt';
    const forceSandbox = process.env.APPLE_IAP_SANDBOX === 'true';
    const primaryUrl = process.env.NODE_ENV === 'production' && !forceSandbox ? productionUrl : sandboxUrl;
    const payload = JSON.stringify({ 'receipt-data': receiptData, password: sharedSecret });

    let result: any = await this.postAppleVerifyReceipt(fetchFn, primaryUrl, payload);
    // Apple's documented environment-mismatch codes: retry once on the other endpoint.
    if (result && result.status === 21007 && primaryUrl !== sandboxUrl) {
      logger.info('Apple sandbox receipt sent to production endpoint, retrying sandbox', { transactionId });
      result = await this.postAppleVerifyReceipt(fetchFn, sandboxUrl, payload);
    } else if (result && result.status === 21008 && primaryUrl !== productionUrl) {
      result = await this.postAppleVerifyReceipt(fetchFn, productionUrl, payload);
    }

    if (!result || result.status !== 0) {
      logger.warn('Apple receipt verification failed', { transactionId, status: result?.status });
      return false;
    }

    const receiptInApps = result.receipt && Array.isArray(result.receipt.in_app) ? result.receipt.in_app : [];
    const latestInfo = Array.isArray(result.latest_receipt_info) ? result.latest_receipt_info : [];
    const matched = [...receiptInApps, ...latestInfo].some(
      (entry: any) => entry && (entry.transaction_id === transactionId || entry.original_transaction_id === transactionId)
    );
    if (!matched) {
      logger.warn('Apple receipt verification failed: transaction not found in receipt', { transactionId });
      return false;
    }

    logger.info('Apple receipt verified', { transactionId });
    return true;
  }

  private async postAppleVerifyReceipt(fetchFn: FetchFn, url: string, payload: string): Promise<any> {
    const response = await fetchFn(url, { method: 'POST', headers: { 'Content-Type': 'application/json' }, body: payload });
    if (!response.ok) throw new Error(`Apple verifyReceipt HTTP ${response.status}`);
    return response.json();
  }

  private async verifyGooglePlayPurchase(transactionId: string, receiptData: string, expectedPrice: number): Promise<boolean> {
    const packageName = process.env.GOOGLE_PLAY_PACKAGE_NAME || process.env.ANDROID_PACKAGE_NAME || '';
    const accessToken = process.env.GOOGLE_PLAY_ACCESS_TOKEN || '';
    const fetchFn = (globalThis as any).fetch as FetchFn | undefined;

    let productId = process.env.GOOGLE_PLAY_PRODUCT_ID || '';
    let purchaseToken = '';
    let claimedOrderId: string | null = null;
    try {
      const parsed = JSON.parse(receiptData);
      if (parsed && typeof parsed === 'object') {
        purchaseToken = parsed.purchaseToken || parsed.token || parsed.purchase_token || '';
        productId = parsed.productId || parsed.product_id || productId;
        claimedOrderId = parsed.orderId || parsed.order_id || null;
      }
    } catch {
      purchaseToken = receiptData; // raw purchase token
    }
    if (!purchaseToken) purchaseToken = transactionId;

    // Without package/token credentials we cannot validate against Google —
    // fall back to the stub path, which is fail-closed in production.
    if (!packageName || !accessToken || !productId || !purchaseToken || typeof fetchFn !== 'function') {
      return this.stubVerifyReceipt('android', transactionId, receiptData);
    }

    const url =
      `https://androidpublisher.googleapis.com/androidpublisher/v3/applications/${encodeURIComponent(packageName)}` +
      `/purchases/products/${encodeURIComponent(productId)}/tokens/${encodeURIComponent(purchaseToken)}` +
      `?access_token=${encodeURIComponent(accessToken)}`;
    const response = await fetchFn(url, { method: 'GET' });
    if (!response.ok) throw new Error(`Google Play purchases.get HTTP ${response.status}`);
    const purchase: any = await response.json();

    // purchaseState: 0 = purchased, 1 = canceled, 2 = pending
    if (purchase.purchaseState !== 0) {
      logger.warn('Google Play purchase not completed', { transactionId, purchaseState: purchase.purchaseState });
      return false;
    }

    const orderId: string | undefined = purchase.orderId || purchase.order_id;
    const looksLikeOrderId = (id: string) => typeof id === 'string' && id.startsWith('GPA.');
    if (claimedOrderId) {
      if (!orderId || orderId !== claimedOrderId) {
        logger.warn('Google Play orderId mismatch', { transactionId, orderId });
        return false;
      }
    } else if (looksLikeOrderId(transactionId) && orderId !== transactionId) {
      logger.warn('Google Play orderId mismatch', { transactionId, orderId });
      return false;
    }

    const micros = purchase.priceAmountMicros ?? purchase.price_amount_micros;
    if (micros !== undefined && micros !== null && Number.isFinite(expectedPrice) && expectedPrice > 0) {
      const actualPrice = Number(micros) / 1e6;
      if (Math.abs(actualPrice - expectedPrice) > 0.005) {
        logger.warn('Google Play price mismatch', { transactionId, expectedPrice, actualPrice });
        return false;
      }
    }

    logger.info('Google Play purchase verified', { transactionId, orderId });
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