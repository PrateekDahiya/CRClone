import { PriorityQueue, Queue } from '../../src/matchmaking/Queue';
import { RatingSystem } from '../../src/matchmaking/RatingSystem';
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

describe('RatingSystem trophy scaling (Agent2 deliverable: BattleServer payouts)', () => {
  const rs = new RatingSystem();

  function resultFor(winner: 'player1' | 'player2' | 'draw', p1Crowns: number, p2Crowns: number) {
    return {
      battleId: 'test',
      winner,
      player1Crowns: p1Crowns,
      player2Crowns: p2Crowns,
      player1TrophyChange: 0,
      player2TrophyChange: 0,
      duration: 180,
      wentOvertime: false,
      replayId: '',
    };
  }

  test('Underdog 3-0 win yields more than the old flat 30', () => {
    // 3000-trophy underdog beats a 4000-trophy favorite 3-0:
    // diff -1000 => base 40, 1.5x crown multiplier => 60, +10 shutout => 70.
    const changes = rs.calculateTrophyChangeFromResult(resultFor('player1', 3, 0), 3000, 4000);
    expect(changes.player1Change).toBeGreaterThan(30);
    expect(changes.player1Change).toBe(70);
    expect(changes.player2Change).toBeLessThan(0);
  });

  test('Favorite 3-0 blowout yields less than the old flat 30', () => {
    // 5000-trophy favorite beats a 3000-trophy underdog 3-0:
    // diff +2000 => base 10, 1.5x => 15, +10 shutout => 25.
    const changes = rs.calculateTrophyChangeFromResult(resultFor('player1', 3, 0), 5000, 3000);
    expect(changes.player1Change).toBeLessThan(30);
    expect(changes.player1Change).toBe(25);
  });

  test('Close-match 1-0 win stays near base with no crown multiplier', () => {
    const changes = rs.calculateTrophyChangeFromResult(resultFor('player1', 1, 0), 4000, 4050);
    // diff -50 => base 30, crownDiff 1 => 1.0x => 30.
    expect(changes.player1Change).toBe(30);
  });

  test('2-0 win applies the 1.2x crown multiplier', () => {
    const changes = rs.calculateTrophyChangeFromResult(resultFor('player1', 2, 0), 4000, 4000);
    // diff 0 => base 30, crownDiff 2 => 1.2x => 36.
    expect(changes.player1Change).toBe(36);
  });

  test('Draw yields zero for both sides', () => {
    const changes = rs.calculateTrophyChangeFromResult(resultFor('draw', 1, 1), 4000, 4000);
    expect(changes).toEqual({ player1Change: 0, player2Change: 0 });
  });

  test('ELO: winner gains, loser loses symmetrically-ish', () => {
    const gain = rs.calculateELOChange(3000, 4000, 1);
    const loss = rs.calculateELOChange(4000, 3000, 0);
    expect(gain).toBeGreaterThan(0);
    expect(loss).toBeLessThan(0);
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
