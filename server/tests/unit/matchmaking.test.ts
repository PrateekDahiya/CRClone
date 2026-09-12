import { PriorityQueue, Queue } from '../../src/matchmaking/Queue';
import { BattleType, MatchmakingQueueEntry } from '../../src/types';

// Mock BattleServer: real createBattle hits BigInt(Number(hugeBigInt)) => Infinity crash
// (filed as bug for Agent 2/5). Mock keeps matchmaking queue logic testable.
jest.mock('../../src/battle/BattleServer', () => ({
  BattleServer: jest.fn().mockImplementation(() => ({}))
}));

import { Matchmaker } from '../../src/matchmaking/Matchmaker';

function makeEntry(playerId: string, trophies: number, battleType: BattleType = BattleType.Ladder): MatchmakingQueueEntry {
  return {
    playerId,
    username: `user_${playerId}`,
    trophies,
    deck: [26000040, 26000041, 26000042, 26000043, 26000044, 26000045, 26000046, 26000047],
    battleType,
    joinedAt: Date.now()
  };
}

describe('Matchmaking', () => {
  let matchmaker: Matchmaker;

  beforeEach(() => {
    jest.clearAllMocks();
    matchmaker = new Matchmaker(null);
  });

  afterEach(() => {
    matchmaker.shutdown();
  });

  describe('Queue Operations', () => {
    test('Player added to queue increases size', () => {
      matchmaker.addToQueue(makeEntry('p1', 4000));
      expect(matchmaker.getQueueSize(BattleType.Ladder)).toBe(1);
    });

    test('Player removed from queue', () => {
      matchmaker.addToQueue(makeEntry('p1', 4000));
      matchmaker.removeFromQueue('p1');
      expect(matchmaker.getQueueSize(BattleType.Ladder)).toBe(0);
    });

    test('Separate queues per battle type', () => {
      matchmaker.addToQueue(makeEntry('p1', 4000, BattleType.Ladder));
      matchmaker.addToQueue(makeEntry('p2', 4000, BattleType.Friendly));
      expect(matchmaker.getQueueSize(BattleType.Ladder)).toBe(1);
      expect(matchmaker.getQueueSize(BattleType.Friendly)).toBe(1);
    });
  });

  describe('Matching Logic', () => {
    test('Matches two players within trophy range and clears queue', () => {
      matchmaker.addToQueue(makeEntry('p1', 4000));
      expect(matchmaker.getQueueSize(BattleType.Ladder)).toBe(1);
      matchmaker.addToQueue(makeEntry('p2', 4050));
      expect(matchmaker.getQueueSize(BattleType.Ladder)).toBe(0);
    });

    test('Does not match players far outside trophy range', () => {
      matchmaker.addToQueue(makeEntry('low', 1000));
      matchmaker.addToQueue(makeEntry('high', 6000));
      expect(matchmaker.getQueueSize(BattleType.Ladder)).toBe(2);
    });

    test('2v2 team queue accumulates players', () => {
      for (let i = 0; i < 3; i++) {
        matchmaker.addToQueue(makeEntry(`t${i}`, 4000, BattleType.TwoVTwo));
      }
      const sizes = matchmaker.getTeamQueueSizes(BattleType.TwoVTwo);
      expect(sizes.totalPlayers).toBe(3);
    });
  });
});

describe('PriorityQueue', () => {
  test('Dequeues highest priority first', () => {
    const q = new PriorityQueue<{ id: string }>();
    q.enqueue({ id: 'a' }, 1);
    q.enqueue({ id: 'b' }, 10);
    q.enqueue({ id: 'c' }, 5);
    expect(q.dequeue()!.id).toBe('b');
    expect(q.dequeue()!.id).toBe('c');
    expect(q.dequeue()!.id).toBe('a');
  });

  test('Peek, remove, size, isEmpty', () => {
    const q = new PriorityQueue<{ id: string }>();
    expect(q.isEmpty()).toBe(true);
    expect(q.dequeue()).toBeUndefined();
    q.enqueue({ id: 'a' }, 1);
    q.enqueue({ id: 'b' }, 2);
    expect(q.size).toBe(2);
    expect(q.peek()!.id).toBe('b');
    expect(q.peekAt(1)!.id).toBe('a');
    q.remove(x => x.id === 'b');
    expect(q.size).toBe(1);
    expect(q.peek()!.id).toBe('a');
  });
});

describe('Queue', () => {
  test('FIFO order', () => {
    const q = new Queue<string>();
    q.enqueue('a'); q.enqueue('b');
    expect(q.dequeue()).toBe('a');
    expect(q.peek()).toBe('b');
    expect(q.size).toBe(1);
  });
});
