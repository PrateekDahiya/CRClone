jest.mock('../../src/battle/BattleServer', () => ({
  BattleServer: jest.fn().mockImplementation(() => ({}))
}));

import { Matchmaker } from '../../src/matchmaking/Matchmaker';
import { BattleType } from '../../src/types';

const DECK = [26000040, 26000041, 26000042, 26000043, 26000044, 26000045, 26000046, 26000047];

describe('Matchmaking queue -> battle result flow', () => {
  test('4 players produce 2 battles (queue drains)', () => {
    const mm = new Matchmaker(null);
    const players = [
      { playerId: 'p1', username: 'p1', trophies: 4000, deck: DECK, battleType: BattleType.Ladder, joinedAt: Date.now() },
      { playerId: 'p2', username: 'p2', trophies: 4020, deck: DECK, battleType: BattleType.Ladder, joinedAt: Date.now() },
      { playerId: 'p3', username: 'p3', trophies: 4010, deck: DECK, battleType: BattleType.Ladder, joinedAt: Date.now() },
      { playerId: 'p4', username: 'p4', trophies: 3990, deck: DECK, battleType: BattleType.Ladder, joinedAt: Date.now() },
    ];
    for (const p of players) mm.addToQueue(p as any);
    expect(mm.getQueueSize(BattleType.Ladder)).toBe(0);
    mm.shutdown();
  });
});
