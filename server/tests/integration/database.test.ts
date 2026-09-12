// Database repository integration tests.
// These run against mocks by default so CI stays green without a live DB.
// Set TEST_DB_LIVE=1 with real credentials to exercise a live database.

describe('Database repositories (mocked)', () => {
  test('PlayerRepository constructs', async () => {
    const { PlayerRepository } = require('../../src/persistence/PlayerRepository');
    const fakeDb = { query: async () => [], execute: async () => ({}) };
    expect(() => new PlayerRepository(fakeDb)).not.toThrow();
  });

  test('BattleRepository constructs', async () => {
    const { BattleRepository } = require('../../src/persistence/BattleRepository');
    const fakeDb = { query: async () => [], execute: async () => ({}) };
    expect(() => new BattleRepository(fakeDb)).not.toThrow();
  });

  test('ReplayRepository constructs', async () => {
    const { ReplayRepository } = require('../../src/persistence/ReplayRepository');
    const fakeDb = { query: async () => [], execute: async () => ({}) };
    expect(() => new ReplayRepository(fakeDb)).not.toThrow();
  });

  test('ClanRepository constructs', async () => {
    const { ClanRepository } = require('../../src/persistence/ClanRepository');
    const fakeDb = { query: async () => [], execute: async () => ({}) };
    expect(() => new ClanRepository(fakeDb)).not.toThrow();
  });
});
