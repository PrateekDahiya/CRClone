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
