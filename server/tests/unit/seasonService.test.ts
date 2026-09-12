import { SeasonService } from '../../src/services/SeasonService';

function makeMockDb() {
  return { query: jest.fn().mockResolvedValue([]), execute: jest.fn().mockResolvedValue({}) } as any;
}

describe('SeasonService', () => {
  test('Constructs without DB connection', () => {
    expect(() => new SeasonService(makeMockDb())).not.toThrow();
  });

  test('getCurrentSeason returns null when none active', async () => {
    const svc = new SeasonService(makeMockDb());
    const { SeasonRepository } = require('../../src/persistence/SeasonRepository');
    const spy = jest.spyOn(SeasonRepository.prototype, 'getCurrentSeason').mockResolvedValue(null);
    expect(await svc.getCurrentSeason()).toBeNull();
    spy.mockRestore();
  });

  test('getAllSeasons returns array', async () => {
    const svc = new SeasonService(makeMockDb());
    const { SeasonRepository } = require('../../src/persistence/SeasonRepository');
    const spy = jest.spyOn(SeasonRepository.prototype, 'getAllSeasons').mockResolvedValue([]);
    expect(await svc.getAllSeasons()).toEqual([]);
    spy.mockRestore();
  });
});
