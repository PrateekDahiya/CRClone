import { ReplayService } from '../../src/services/ReplayService';

function makeMockDb() {
  return { query: jest.fn().mockResolvedValue([]), execute: jest.fn().mockResolvedValue({}) } as any;
}

describe('ReplayService', () => {
  test('Constructs without DB connection', () => {
    expect(() => new ReplayService(makeMockDb())).not.toThrow();
  });

  test('getReplayData returns null when missing', async () => {
    const svc = new ReplayService(makeMockDb());
    const { ReplayRepository } = require('../../src/persistence/ReplayRepository');
    const spy = jest.spyOn(ReplayRepository.prototype, 'getReplayData').mockResolvedValue(null);
    expect(await svc.getReplayData(999999)).toBeNull();
    spy.mockRestore();
  });

  test('getReplayStats delegates to repo', async () => {
    const svc = new ReplayService(makeMockDb());
    const { ReplayRepository } = require('../../src/persistence/ReplayRepository');
    const spy = jest.spyOn(ReplayRepository.prototype, 'getReplayStats').mockResolvedValue({ total: 0 });
    expect(await svc.getReplayStats()).toEqual({ total: 0 });
    spy.mockRestore();
  });

  test('searchReplays returns array', async () => {
    const svc = new ReplayService(makeMockDb());
    const { ReplayRepository } = require('../../src/persistence/ReplayRepository');
    const spy = jest.spyOn(ReplayRepository.prototype, 'searchReplays').mockResolvedValue([]);
    expect(await svc.searchReplays({} as any)).toEqual([]);
    spy.mockRestore();
  });
});
