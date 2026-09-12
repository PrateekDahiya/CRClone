jest.mock('../../src/battle/BattleServer', () => ({
  BattleServer: jest.fn().mockImplementation(() => ({}))
}));

import { BattleSimulation } from '../../src/battle/BattleSimulation';
import { BattleStatus, BattleType } from '../../src/types';
import { Matchmaker } from '../../src/matchmaking/Matchmaker';

function makeConfig() {
  return {
    startingElixir: 5,
    maxElixir: 10,
    elixirGenerationRate: 2.8,
    doubleElixirRate: 1.4,
    tripleElixirRate: 0.93,
    battleDuration: 180,
    overtimeDuration: 60,
    handSize: 4,
    kingTowerHP: 4000,
    princessTowerHP: 2500,
    towerDamage: 150,
    towerHitSpeed: 1.0,
    towerRange: 7
  };
}

const DECK = [26000040, 26000041, 26000042, 26000043, 26000044, 26000045, 26000046, 26000047];

describe('Battle Flow Integration (server simulation)', () => {
  test('Simulation initializes and ticks', () => {
    const sim = new BattleSimulation();
    sim.Initialize(makeConfig(), 12345n, DECK, DECK, new Map());
    expect(sim.Status).toBe(BattleStatus.Playing);
    expect(sim.Player1.Hand).toHaveLength(4);
    expect(sim.Player2.Hand).toHaveLength(4);
    for (let i = 0; i < 120; i++) sim.Tick();
    expect(sim.CurrentTick).toBe(120);
  });

  test('Deterministic: same seed => same event log length', () => {
    const run = () => {
      const sim = new BattleSimulation();
      sim.Initialize(makeConfig(), 777n, DECK, DECK, new Map());
      for (let i = 0; i < 300; i++) sim.Tick();
      return { ticks: sim.CurrentTick, events: sim.ReplayLog.length, status: sim.Status };
    };
    expect(run()).toEqual(run());
  });

  test('Elixir generates over time and caps at max', () => {
    const sim = new BattleSimulation();
    sim.Initialize(makeConfig(), 1n, DECK, DECK, new Map());
    sim.Player1.Elixir = 0;
    for (let i = 0; i < 600; i++) sim.Tick();
    expect(sim.Player1.Elixir).toBeGreaterThan(0);
    expect(sim.Player1.Elixir).toBeLessThanOrEqual(10);
  });
});

describe('BattleServer input validation hardening (Agent2 deliverable 2.9)', () => {
  // NOTE: this file mocks BattleServer at the top, so pull the real
  // implementations here for validation tests.
  const { BattleServer: RealBattleServer } = jest.requireActual('../../src/battle/BattleServer');
  const { MessageHandler: RealMessageHandler } = jest.requireActual('../../src/network/MessageHandler');

  const P1 = { playerId: 'p1', username: 'p1', trophies: 4000, deck: DECK, kingTowerLevel: 11, princessTowerLevel: 11 };
  const P2 = { playerId: 'p2', username: 'p2', trophies: 4000, deck: DECK, kingTowerLevel: 11, princessTowerLevel: 11 };

  function makeRealBattle() {
    const battle = new RealBattleServer('b_val', P1, P2, 999, { saveBattleResult: jest.fn() });
    (battle as any)._player1.elixir = 10;
    (battle as any)._player2.elixir = 10;
    // handleInput only processes inputs while the battle is Playing
    // (constructor leaves status at Waiting; start() would also begin the
    // 60Hz interval, which tests must not do).
    (battle as any)._status = BattleStatus.Playing;
    return battle;
  }

  function buffersOf(battle: any, playerId: string): any[] {
    return battle._inputBuffers.get(playerId) || [];
  }

  test('Unowned card rejected (not in hand)', () => {
    const battle = makeRealBattle();
    // Fireball (26000044) is real but not in player 1's opening hand [40,41,42,43].
    battle.handleInput('p1', { type: 'play_card', cardId: 26000044, position: { x: 9, y: 8 }, clientTick: 1 });
    expect(buffersOf(battle, 'p1')).toHaveLength(0);
  });

  test('Unknown cardId rejected', () => {
    const battle = makeRealBattle();
    battle.handleInput('p1', { type: 'play_card', cardId: 99999999, position: { x: 9, y: 8 }, clientTick: 1 });
    expect(buffersOf(battle, 'p1')).toHaveLength(0);
  });

  test('Insufficient real-cost elixir rejected (Giant costs 5, have 4)', () => {
    const battle = makeRealBattle();
    (battle as any)._player1.elixir = 4;
    // Giant IS in hand but costs 5. The old estimatedCost=3 guess would accept this.
    battle.handleInput('p1', { type: 'play_card', cardId: 26000042, position: { x: 9, y: 8 }, clientTick: 1 });
    expect(buffersOf(battle, 'p1')).toHaveLength(0);
  });

  test('Sufficient real-cost elixir accepted (Giant costs 5, have 5)', () => {
    const battle = makeRealBattle();
    (battle as any)._player1.elixir = 5;
    battle.handleInput('p1', { type: 'play_card', cardId: 26000042, position: { x: 9, y: 8 }, clientTick: 1 });
    expect(buffersOf(battle, 'p1')).toHaveLength(1);
  });

  test('More than 20 pending inputs rejected (rate limit)', () => {
    const battle = makeRealBattle();
    for (let i = 0; i < 21; i++) {
      (battle as any)._player1.pendingInputs.set(100 + i, { input: {}, sentAt: Date.now(), retries: 0 });
    }
    battle.handleInput('p1', { type: 'play_card', cardId: 26000040, position: { x: 9, y: 8 }, clientTick: 1 });
    expect(buffersOf(battle, 'p1')).toHaveLength(0);
  });

  function makeHandler(battles: Map<string, any>) {
    return new RealMessageHandler(null, null, battles, null, null, null, null, null, null, null);
  }

  function makeClient(overrides: any = {}) {
    return {
      id: 'c1',
      authenticated: true,
      player: { id: 'p1' },
      battleId: 'b1',
      sendError: jest.fn(),
      queueInput: jest.fn(),
      ...overrides,
    };
  }

  test('Forged battleId rejected (battle does not exist)', () => {
    const handler = makeHandler(new Map());
    const client = makeClient({ battleId: 'forged-id' });
    handler.handle(client as any, {
      type: 'input',
      input: { type: 'play_card', cardId: 26000040, position: { x: 9, y: 8 }, clientTick: 1 },
    } as any);
    expect(client.sendError).toHaveBeenCalledWith('BATTLE_NOT_FOUND');
    expect(client.queueInput).not.toHaveBeenCalled();
  });

  test('Non-participant rejected (battle does not contain sender)', () => {
    const fakeBattle = { hasPlayer: jest.fn().mockReturnValue(false), handleInput: jest.fn() };
    const handler = makeHandler(new Map([['b1', fakeBattle]]));
    const client = makeClient({});
    handler.handle(client as any, {
      type: 'input',
      input: { type: 'play_card', cardId: 26000040, position: { x: 9, y: 8 }, clientTick: 1 },
    } as any);
    expect(fakeBattle.hasPlayer).toHaveBeenCalledWith('p1');
    expect(client.sendError).toHaveBeenCalledWith('NOT_IN_BATTLE');
    expect(fakeBattle.handleInput).not.toHaveBeenCalled();
  });

  test('Unauthenticated sender rejected before touching battle state', () => {
    const fakeBattle = { hasPlayer: jest.fn().mockReturnValue(true), handleInput: jest.fn() };
    const handler = makeHandler(new Map([['b1', fakeBattle]]));
    const client = makeClient({ authenticated: false, player: null });
    handler.handle(client as any, {
      type: 'input',
      input: { type: 'play_card', cardId: 26000040, position: { x: 9, y: 8 }, clientTick: 1 },
    } as any);
    expect(client.sendError).toHaveBeenCalledWith('NOT_AUTHENTICATED');
    expect(fakeBattle.handleInput).not.toHaveBeenCalled();
  });

  test('Valid participant input is tracked and forwarded', () => {
    const fakeBattle = { hasPlayer: jest.fn().mockReturnValue(true), handleInput: jest.fn() };
    const handler = makeHandler(new Map([['b1', fakeBattle]]));
    const client = makeClient({});
    handler.handle(client as any, {
      type: 'input',
      input: { type: 'play_card', cardId: 26000040, position: { x: 9, y: 8 }, clientTick: 1 },
    } as any);
    expect(client.queueInput).toHaveBeenCalled();
    expect(fakeBattle.handleInput).toHaveBeenCalledWith('p1', expect.objectContaining({ cardId: 26000040 }));
  });
});

describe('Matchmaking Integration (queue -> battle creation)', () => {
  test('Two close players auto-match and drain queue', () => {
    const mm = new Matchmaker(null);
    mm.addToQueue({ playerId: 'p1', username: 'p1', trophies: 4000, deck: DECK, battleType: BattleType.Ladder, joinedAt: Date.now() });
    expect(mm.getQueueSize(BattleType.Ladder)).toBe(1);
    mm.addToQueue({ playerId: 'p2', username: 'p2', trophies: 4050, deck: DECK, battleType: BattleType.Ladder, joinedAt: Date.now() });
    expect(mm.getQueueSize(BattleType.Ladder)).toBe(0);
    mm.shutdown();
  });

  test('Far-apart players stay queued', () => {
    const mm = new Matchmaker(null);
    mm.addToQueue({ playerId: 'low', username: 'low', trophies: 500, deck: DECK, battleType: BattleType.Ladder, joinedAt: Date.now() });
    mm.addToQueue({ playerId: 'high', username: 'high', trophies: 6500, deck: DECK, battleType: BattleType.Ladder, joinedAt: Date.now() });
    expect(mm.getQueueSize(BattleType.Ladder)).toBe(2);
    mm.shutdown();
  });
});
