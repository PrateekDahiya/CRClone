import { BattleServer } from '../../src/battle/BattleServer';
import { NetworkClient } from '../../src/network/NetworkClient';
import { ratingSystem } from '../../src/matchmaking/RatingSystem';
import { BattleStatus, PlayerBattleInfo, PlayerInput } from '../../src/types';

const DECK = [26000040, 26000041, 26000042, 26000043, 26000044, 26000045, 26000046, 26000047];

function makeInfo(playerId: string, trophies: number): PlayerBattleInfo {
  return {
    playerId,
    username: playerId,
    trophies,
    deck: [...DECK],
    kingTowerLevel: 11,
    princessTowerLevel: 11,
  };
}

function makeBattle(p1Trophies = 3000, p2Trophies = 4000): BattleServer {
  const playerService = { saveBattleResult: jest.fn() } as any;
  return new BattleServer('battle_test', makeInfo('p1', p1Trophies), makeInfo('p2', p2Trophies), 12345, playerService);
}

// Fake server-side connection reusing the REAL NetworkClient ack/retry
// logic bound to a plain object (no WebSocket needed).
function attachFakeConnection(battle: BattleServer, playerId: string): any {
  const fake: any = {
    sent: [] as any[],
    pendingInputs: new Map(),
    acknowledgedTick: 0,
    send(msg: any) {
      this.sent.push(msg);
    },
  };
  fake.queueInput = NetworkClient.prototype.queueInput.bind(fake);
  fake.acknowledgeInput = NetworkClient.prototype.acknowledgeInput.bind(fake);
  fake.retryUnacknowledgedInputs = NetworkClient.prototype.retryUnacknowledgedInputs.bind(fake);
  battle.setConnection(playerId, fake);
  return fake;
}

describe('BattleServer rating payouts (Agent2: RatingSystem, no hardcoded +-30)', () => {
  test('computeRatingChanges matches RatingSystem output exactly', () => {
    const battle = makeBattle(3000, 4000);
    const got = battle.computeRatingChanges('player1', 3, 0);
    const want = ratingSystem.calculateTrophyChangeFromResult(
      {
        battleId: 'battle_test',
        winner: 'player1',
        player1Crowns: 3,
        player2Crowns: 0,
        player1TrophyChange: 0,
        player2TrophyChange: 0,
        duration: 0,
        wentOvertime: false,
        replayId: '',
      },
      3000,
      4000
    );
    expect(got.player1TrophyChange).toBe(want.player1Change);
    expect(got.player2TrophyChange).toBe(want.player2Change);
    expect(got.player1EloChange).toBe(ratingSystem.calculateELOChange(3000, 4000, 1));
    expect(got.player2EloChange).toBe(ratingSystem.calculateELOChange(4000, 3000, 0));
  });

  test('underdog win yields more than 30 via BattleServer', () => {
    const battle = makeBattle(3000, 4000);
    const got = battle.computeRatingChanges('player1', 3, 0);
    expect(got.player1TrophyChange).toBeGreaterThan(30);
  });

  test('favorite blowout yields less than 30 via BattleServer', () => {
    const battle = makeBattle(5000, 3000);
    const got = battle.computeRatingChanges('player1', 3, 0);
    expect(got.player1TrophyChange).toBeLessThan(30);
  });

  test('draw yields zero trophies via BattleServer', () => {
    const battle = makeBattle(4000, 4000);
    const got = battle.computeRatingChanges('draw', 1, 1);
    expect(got.player1TrophyChange).toBe(0);
    expect(got.player2TrophyChange).toBe(0);
  });
});

describe('BattleServer retransmit path (Agent2 deliverable 2.10)', () => {
  test('unacked inputs are resent until acked, then resends stop', () => {
    const battle = makeBattle();
    const fake = attachFakeConnection(battle, 'p1');

    const input: PlayerInput = {
      type: 'play_card',
      cardId: 26000040,
      position: { x: 9, y: 8 },
      clientTick: 5,
    };

    // Simulate the index.ts:147 path: input received + tracked server-side.
    fake.queueInput(input);
    expect(fake.pendingInputs.size).toBe(1);

    // Drop acks for 1500ms: first resend.
    fake.pendingInputs.get(5).sentAt = Date.now() - 1500;
    const first = battle.processAcknowledgments();
    expect(first).toHaveLength(1);
    expect(first[0].clientTick).toBe(5);

    // Drop acks again: second resend (same input, retries < 3).
    fake.pendingInputs.get(5).sentAt = Date.now() - 1500;
    const second = battle.processAcknowledgments();
    expect(second).toHaveLength(1);
    expect(second[0].clientTick).toBe(5);
    expect(fake.pendingInputs.get(5).retries).toBe(2);

    // Ack arrives: tracking clears and resends stop.
    battle.handleInputAck('p1', 5);
    expect(fake.pendingInputs.size).toBe(0);
    expect(battle.processAcknowledgments()).toHaveLength(0);
  });

  test('handleInput marks the tick processed so it is not resent', () => {
    const battle = makeBattle();
    const fake = attachFakeConnection(battle, 'p1');
    // handleInput only processes inputs while Playing (constructor leaves
    // Waiting; start() would also begin the 60Hz interval).
    (battle as any)._status = BattleStatus.Playing;

    // Give player 1 plenty of elixir and a valid Knight play.
    (battle as any)._player1.elixir = 10;
    const input: PlayerInput = {
      type: 'play_card',
      cardId: 26000040,
      position: { x: 9, y: 8 },
      clientTick: 6,
    };
    fake.queueInput(input);

    battle.handleInput('p1', input);

    // Accepted inputs are acknowledged immediately: nothing to resend.
    expect(fake.pendingInputs.size).toBe(0);
    expect((battle as any)._player1.acknowledgedTick).toBe(6);
    expect(battle.processAcknowledgments()).toHaveLength(0);
  });

  test('retries exhaust after 3 attempts', () => {
    const battle = makeBattle();
    const fake = attachFakeConnection(battle, 'p1');

    fake.queueInput({
      type: 'play_card',
      cardId: 26000040,
      position: { x: 9, y: 8 },
      clientTick: 9,
    });

    for (let i = 0; i < 3; i++) {
      fake.pendingInputs.get(9).sentAt = Date.now() - 1500;
      expect(battle.processAcknowledgments()).toHaveLength(1);
    }
    // 4th attempt: retries exhausted, no resend.
    fake.pendingInputs.get(9).sentAt = Date.now() - 1500;
    expect(battle.processAcknowledgments()).toHaveLength(0);
  });
});
