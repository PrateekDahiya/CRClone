import { ClanService } from '../../src/services/ClanService';

function makeMockDb() {
  return { query: jest.fn().mockResolvedValue([]), execute: jest.fn().mockResolvedValue({}) } as any;
}

describe('ClanService', () => {
  test('Constructs without DB connection', () => {
    expect(() => new ClanService(makeMockDb())).not.toThrow();
  });

  test('searchClans returns array (mocked empty)', async () => {
    const svc = new ClanService(makeMockDb());
    // Mock repo layer
    const { ClanRepository } = require('../../src/persistence/ClanRepository');
    const spy = jest.spyOn(ClanRepository.prototype, 'searchClans').mockResolvedValue([]);
    const res = await svc.searchClans('test');
    expect(Array.isArray(res)).toBe(true);
    spy.mockRestore();
  });

  test('getClan returns null when not found', async () => {
    const svc = new ClanService(makeMockDb());
    const { ClanRepository } = require('../../src/persistence/ClanRepository');
    const spy = jest.spyOn(ClanRepository.prototype, 'getClan').mockResolvedValue(null);
    expect(await svc.getClan(999999)).toBeNull();
    spy.mockRestore();
  });

  test('getTopClans returns array', async () => {
    const svc = new ClanService(makeMockDb());
    const { ClanRepository } = require('../../src/persistence/ClanRepository');
    const spy = jest.spyOn(ClanRepository.prototype, 'getTopClans').mockResolvedValue([]);
    expect(await svc.getTopClans(5)).toEqual([]);
    spy.mockRestore();
  });
});
