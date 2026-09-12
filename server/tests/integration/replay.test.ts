import { ReplayService } from '../../src/services/ReplayService';

describe('Replay record -> store -> retrieve (mocked)', () => {
  test('store delegates to repository', async () => {
    const fakeDb = { query: async () => [], execute: async () => ({}) };
    const svc = new ReplayService(fakeDb as any);
    const { ReplayRepository } = require('../../src/persistence/ReplayRepository');
    const spy = jest.spyOn(ReplayRepository.prototype, 'storeReplay').mockResolvedValue(42);
    const id = await svc.storeReplay({
      battleId: 1,
      replayData: Buffer.from('test'),
      replayVersion: '1',
      player1Id: 'p1',
      player2Id: 'p2',
      player1DeckHash: 'a',
      player2DeckHash: 'b',
      durationSeconds: 120
    } as any);
    expect(id).toBe(42);
    spy.mockRestore();
  });

  test('retrieve delegates to repository', async () => {
    const fakeDb = { query: async () => [], execute: async () => ({}) };
    const svc = new ReplayService(fakeDb as any);
    const { ReplayRepository } = require('../../src/persistence/ReplayRepository');
    const spy = jest.spyOn(ReplayRepository.prototype, 'getReplayData').mockResolvedValue(null);
    expect(await svc.getReplayData(999)).toBeNull();
    spy.mockRestore();
  });
});
