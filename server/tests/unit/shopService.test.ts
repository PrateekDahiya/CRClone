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
