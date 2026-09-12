import { ShopService } from '../../src/services/ShopService';

function makeMockDb() {
  return { query: jest.fn().mockResolvedValue([]), execute: jest.fn().mockResolvedValue({}) } as any;
}

describe('ShopService', () => {
  test('Constructs without DB connection', () => {
    expect(() => new ShopService(makeMockDb())).not.toThrow();
  });

  test('getDailyOffers returns array', async () => {
    const svc = new ShopService(makeMockDb());
    const { ShopRepository } = require('../../src/persistence/ShopRepository');
    const spy = jest.spyOn(ShopRepository.prototype, 'getDailyOffers').mockResolvedValue([]);
    expect(await svc.getDailyOffers()).toEqual([]);
    spy.mockRestore();
  });

  test('getSpecialOffers returns array', async () => {
    const svc = new ShopService(makeMockDb());
    const { ShopRepository } = require('../../src/persistence/ShopRepository');
    const spy = jest.spyOn(ShopRepository.prototype, 'getSpecialOffers').mockResolvedValue([]);
    expect(await svc.getSpecialOffers()).toEqual([]);
    spy.mockRestore();
  });

  test('getOffer returns null when missing', async () => {
    const svc = new ShopService(makeMockDb());
    const { ShopRepository } = require('../../src/persistence/ShopRepository');
    const spy = jest.spyOn(ShopRepository.prototype, 'getOffer').mockResolvedValue(null);
    expect(await svc.getOffer(999999)).toBeNull();
    spy.mockRestore();
  });
});

describe('ShopService IAP verification (Agent 5.4, fail-closed)', () => {
  const IAP_ENV_KEYS = [
    'APPLE_IAP_SHARED_SECRET',
    'APPLE_IAP_SANDBOX',
    'GOOGLE_PLAY_PACKAGE_NAME',
    'GOOGLE_PLAY_PRODUCT_ID',
    'GOOGLE_PLAY_ACCESS_TOKEN',
  ] as const;

  let svc: ShopService;
  let prevNodeEnv: string | undefined;
  let prevIapEnv: Record<string, string | undefined>;
  let originalFetch: any;

  const verify = (platform: 'ios' | 'android', transactionId: string, receiptData: string, expectedPrice = 4.99) =>
    (svc as any).verifyReceipt(platform, transactionId, receiptData, expectedPrice) as Promise<boolean>;

  beforeEach(() => {
    svc = new ShopService(makeMockDb());
    prevNodeEnv = process.env.NODE_ENV;
    prevIapEnv = {};
    for (const k of IAP_ENV_KEYS) {
      prevIapEnv[k] = process.env[k];
      delete process.env[k];
    }
    originalFetch = (global as any).fetch;
    delete (global as any).fetch;
  });

  afterEach(() => {
    if (prevNodeEnv === undefined) delete process.env.NODE_ENV;
    else process.env.NODE_ENV = prevNodeEnv;
    for (const k of IAP_ENV_KEYS) {
      if (prevIapEnv[k] === undefined) delete process.env[k];
      else process.env[k] = prevIapEnv[k];
    }
    (global as any).fetch = originalFetch;
    jest.restoreAllMocks();
  });

  test('forged receipt rejected in non-production', async () => {
    process.env.NODE_ENV = 'test';
    await expect(verify('ios', 'txn-forged', 'bogus-forged-receipt')).resolves.toBe(false);
    await expect(verify('android', 'txn-forged', 'bogus-forged-receipt')).resolves.toBe(false);
  });

  test('forged receipt rejected in production', async () => {
    process.env.NODE_ENV = 'production';
    await expect(verify('ios', 'txn-forged', 'bogus-forged-receipt')).resolves.toBe(false);
    await expect(verify('android', 'txn-forged', 'bogus-forged-receipt')).resolves.toBe(false);
  });

  test('valid sandbox-test receipt accepted in dev', async () => {
    process.env.NODE_ENV = 'test';
    await expect(verify('ios', 'txn-valid', 'SANDBOX_TEST_RECEIPT:txn-valid')).resolves.toBe(true);
    await expect(
      verify('android', 'txn-json', JSON.stringify({ __sandboxTest: true, transactionId: 'txn-json' }))
    ).resolves.toBe(true);
    // Fixture is bound to its transaction id — a different id must fail.
    await expect(verify('ios', 'txn-other', 'SANDBOX_TEST_RECEIPT:txn-valid')).resolves.toBe(false);
  });

  test('production stub path never returns true', async () => {
    process.env.NODE_ENV = 'production';
    // No credentials configured -> stub path must fail closed, even for
    // dev-marked fixtures and forged receipts.
    await expect(verify('ios', 'txn-valid', 'SANDBOX_TEST_RECEIPT:txn-valid')).resolves.toBe(false);
    await expect(
      verify('android', 'txn-json', JSON.stringify({ __sandboxTest: true, transactionId: 'txn-json' }))
    ).resolves.toBe(false);
    await expect(verify('ios', 'txn-forged', 'bogus-forged-receipt')).resolves.toBe(false);
    await expect(verify('android', 'txn-forged', 'bogus-forged-receipt')).resolves.toBe(false);
  });

  test('real Apple verification accepts status 0 + transaction match (mocked fetch)', async () => {
    process.env.NODE_ENV = 'test';
    process.env.APPLE_IAP_SHARED_SECRET = 'test-secret';
    (global as any).fetch = jest.fn().mockResolvedValue({
      ok: true,
      status: 200,
      json: async () => ({ status: 0, receipt: { in_app: [{ transaction_id: 'txn-123' }] } }),
    });
    await expect(verify('ios', 'txn-123', 'base64receiptdata')).resolves.toBe(true);
    await expect(verify('ios', 'txn-other', 'base64receiptdata')).resolves.toBe(false);
  });

  test('real Apple verification rejects non-zero status', async () => {
    process.env.NODE_ENV = 'test';
    process.env.APPLE_IAP_SHARED_SECRET = 'test-secret';
    (global as any).fetch = jest.fn().mockResolvedValue({
      ok: true,
      status: 200,
      json: async () => ({ status: 21002 }),
    });
    await expect(verify('ios', 'txn-123', 'base64receiptdata')).resolves.toBe(false);
  });

  test('production retries Apple sandbox on status 21007', async () => {
    process.env.NODE_ENV = 'production';
    process.env.APPLE_IAP_SHARED_SECRET = 'prod-secret';
    const fetchMock = jest
      .fn()
      .mockResolvedValueOnce({ ok: true, status: 200, json: async () => ({ status: 21007 }) })
      .mockResolvedValueOnce({
        ok: true,
        status: 200,
        json: async () => ({ status: 0, receipt: { in_app: [{ transaction_id: 'txn-1' }] } }),
      });
    (global as any).fetch = fetchMock;
    await expect(verify('ios', 'txn-1', 'base64receiptdata')).resolves.toBe(true);
    expect(fetchMock).toHaveBeenCalledTimes(2);
    expect(fetchMock.mock.calls[0][0]).toContain('buy.itunes.apple.com');
    expect(fetchMock.mock.calls[1][0]).toContain('sandbox.itunes.apple.com');
  });

  test('real Google verification requires purchaseState 0 + order/price match (mocked fetch)', async () => {
    process.env.NODE_ENV = 'test';
    process.env.GOOGLE_PLAY_PACKAGE_NAME = 'com.example.game';
    process.env.GOOGLE_PLAY_ACCESS_TOKEN = 'ya29.test';
    const receipt = JSON.stringify({ productId: 'gems_500', orderId: 'GPA.123' });
    (global as any).fetch = jest.fn().mockResolvedValue({
      ok: true,
      status: 200,
      json: async () => ({ purchaseState: 0, orderId: 'GPA.123', priceAmountMicros: '4990000' }),
    });
    await expect(verify('android', 'tok-abc', receipt, 4.99)).resolves.toBe(true);

    (global as any).fetch = jest.fn().mockResolvedValue({
      ok: true,
      status: 200,
      json: async () => ({ purchaseState: 1, orderId: 'GPA.123', priceAmountMicros: '4990000' }),
    });
    await expect(verify('android', 'tok-abc', receipt, 4.99)).resolves.toBe(false);

    (global as any).fetch = jest.fn().mockResolvedValue({
      ok: true,
      status: 200,
      json: async () => ({ purchaseState: 0, orderId: 'GPA.123', priceAmountMicros: '9990000' }),
    });
    await expect(verify('android', 'tok-abc', receipt, 4.99)).resolves.toBe(false);
  });

  test('purchaseWithIAP fails closed on forged receipt', async () => {
    process.env.NODE_ENV = 'test';
    const { ShopRepository } = require('../../src/persistence/ShopRepository');
    const getOffer = jest
      .spyOn(ShopRepository.prototype, 'getOffer')
      .mockResolvedValue({ offer_id: 7, cost_real_money: 4.99, rewards: [] });
    const store = jest.spyOn(ShopRepository.prototype, 'storeIAPReceipt').mockResolvedValue(42);
    const verifyPurchase = jest.spyOn(ShopRepository.prototype, 'verifyPurchase').mockResolvedValue(undefined);

    const result = await svc.purchaseWithIAP('player-1', 7, 'ios', 'txn-forged', 'bogus-forged-receipt');
    expect(result).toMatchObject({ success: false, error: 'RECEIPT_VERIFICATION_FAILED', purchaseId: 42 });
    expect(verifyPurchase).not.toHaveBeenCalled();

    getOffer.mockRestore();
    store.mockRestore();
    verifyPurchase.mockRestore();
  });
});
