import { TournamentService } from '../../src/services/TournamentService';

function makeMockDb() {
  return { query: jest.fn().mockResolvedValue([]), execute: jest.fn().mockResolvedValue({}) } as any;
}

describe('TournamentService', () => {
  test('Constructs without DB connection', () => {
    expect(() => new TournamentService(makeMockDb())).not.toThrow();
  });

  test('getActiveTournaments returns array', async () => {
    const svc = new TournamentService(makeMockDb());
    const { TournamentRepository } = require('../../src/persistence/TournamentRepository');
    const spy = jest.spyOn(TournamentRepository.prototype, 'getActiveTournaments').mockResolvedValue([]);
    expect(await svc.getActiveTournaments()).toEqual([]);
    spy.mockRestore();
  });

  test('getTournament returns null when missing', async () => {
    const svc = new TournamentService(makeMockDb());
    const { TournamentRepository } = require('../../src/persistence/TournamentRepository');
    const spy = jest.spyOn(TournamentRepository.prototype, 'getTournament').mockResolvedValue(null);
    expect(await svc.getTournament(999999)).toBeNull();
    spy.mockRestore();
  });
});
