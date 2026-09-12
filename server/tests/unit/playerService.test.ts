import { PlayerService } from '../../src/services/PlayerService';

function makeMockDb(overrides: any = {}) {
  return {
    query: jest.fn().mockResolvedValue([]),
    execute: jest.fn().mockResolvedValue({ affectedRows: 1 }),
    ...overrides
  } as any;
}

describe('PlayerService', () => {
  describe('authenticate', () => {
    test('Returns null for invalid token without DB access', async () => {
      const svc = new PlayerService(makeMockDb());
      const result = await svc.authenticate('invalid.token.here');
      expect(result).toBeNull();
    });

    test('Returns null for malformed token', async () => {
      const svc = new PlayerService(makeMockDb());
      expect(await svc.authenticate('')).toBeNull();
      expect(await svc.authenticate('abc')).toBeNull();
    });
  });

  describe('saveDeck validation', () => {
    test('Rejects deck with wrong size', async () => {
      const svc = new PlayerService(makeMockDb());
      await expect(svc.saveDeck('p1', [1, 2, 3])).rejects.toThrow('DECK_MUST_HAVE_8_CARDS');
    });

    test('Rejects deck with too many champions', async () => {
      // Mock collection owning all cards
      const mockDb = makeMockDb();
      const svc = new PlayerService(mockDb);
      // Patch getPlayerCards via playerRepo mock: easiest is to mock PlayerRepository prototype
      const { PlayerRepository } = require('../../src/persistence/PlayerRepository');
      const spy = jest.spyOn(PlayerRepository.prototype, 'getPlayerCards').mockResolvedValue(
        [27000000, 27000001, 26000042, 26000043, 26000044, 26000045, 26000046, 26000047].map(id => ({ card_id: id, count: 10 }))
      );
      await expect(svc.saveDeck('p1', [27000000, 27000001, 26000042, 26000043, 26000044, 26000045, 26000046, 26000047])).rejects.toThrow('MAX_ONE_CHAMPION_PER_DECK');
      spy.mockRestore();
    });

    test('Rejects deck with unowned card', async () => {
      const { PlayerRepository } = require('../../src/persistence/PlayerRepository');
      const spy = jest.spyOn(PlayerRepository.prototype, 'getPlayerCards').mockResolvedValue([]);
      const svc = new PlayerService(makeMockDb());
      await expect(svc.saveDeck('p1', [1, 2, 3, 4, 5, 6, 7, 8])).rejects.toThrow();
      spy.mockRestore();
    });
  });

  describe('upgradeCard', () => {
    test('Returns false when player does not own card', async () => {
      const { PlayerRepository } = require('../../src/persistence/PlayerRepository');
      const spy = jest.spyOn(PlayerRepository.prototype, 'getPlayerCards').mockResolvedValue([]);
      const svc = new PlayerService(makeMockDb());
      expect(await svc.upgradeCard('p1', 26000040)).toBe(false);
      spy.mockRestore();
    });
  });

  describe('refreshToken', () => {
    test('Returns null for invalid refresh token', async () => {
      const svc = new PlayerService(makeMockDb());
      expect(await svc.refreshToken('bad')).toBeNull();
    });
  });
});
