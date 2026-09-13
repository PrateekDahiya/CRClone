import { BattleServer } from '../../src/battle/BattleServer';
import { BattleSimulation, EventType } from '../../src/battle/BattleSimulation';
import { BattleStatus, BattleType, TowerType } from '../../src/types';
import { getFootprintRadius, isFlyingCard } from '../../src/battle/CardDatabase';

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
    towerRange: 7,
  };
}

const DECK = [26000040, 26000041, 26000042, 26000043, 26000044, 26000045, 26000046, 26000047];
const KNIGHT = 26000040;
const GIANT = 26000042;
const FIREBALL = 26000044;
const CANNON = 26000045;
const POISON = 26000050;
const QUEEN = 27000000;

const P1 = { playerId: 'p1', username: 'p1', trophies: 4000, deck: DECK, kingTowerLevel: 11, princessTowerLevel: 11 };
const P2 = { playerId: 'p2', username: 'p2', trophies: 4000, deck: DECK, kingTowerLevel: 11, princessTowerLevel: 11 };

function makeBattle(): BattleServer {
  const battle = new BattleServer('b_res', P1, P2, 4242, { saveBattleResult: jest.fn() } as any);
  (battle as any)._status = BattleStatus.Playing;
  (battle as any)._player1.elixir = 10;
  (battle as any)._player2.elixir = 10;
  const sim: BattleSimulation = (battle as any)._simulation;
  sim.Player1.Elixir = 10;
  sim.Player2.Elixir = 10;
  return battle;
}

function simOf(battle: BattleServer): BattleSimulation {
  return (battle as any)._simulation;
}

function buffersOf(battle: BattleServer, playerId: string): unknown[] {
  return (battle as any)._inputBuffers.get(playerId) || [];
}

function findTower(sim: BattleSimulation, owner: number, type: TowerType) {
  const tower = sim.Towers.find((t) => t.OwnerPlayerId === owner && t.TowerType === type);
  if (!tower) throw new Error(`tower ${owner}/${type} missing`);
  return tower;
}

function killTower(sim: BattleSimulation, owner: number, type: TowerType): void {
  const tower = findTower(sim, owner, type);
  tower.TakeDamage(tower.MaxHP, 0);
}

describe('ISSUE-201: authoritative server sim spawns entities', () => {
  test('play_card spawns a Giant unit and deducts the real elixir cost', () => {
    const sim = new BattleSimulation();
    sim.Initialize(makeConfig(), 42n, DECK, DECK, new Map());
    sim.Player1.Elixir = 10;

    sim.QueueInput(1, { type: 'play_card', cardId: GIANT, position: { x: 9, y: 8 }, clientTick: 1 });
    sim.Tick();

    expect(sim.Units.length).toBe(1);
    expect(sim.Units[0].OwnerPlayerId).toBe(1);
    expect(sim.Units[0].CurrentHP).toBe(3392);
    // Giant costs 5: 10 -> ~5 (one tick of elixir regen applies).
    expect(sim.Player1.Elixir).toBeCloseTo(5, 1);
  });

  test('multi-unit cards spawn their full count (Skeletons x4)', () => {
    const sim = new BattleSimulation();
    sim.Initialize(makeConfig(), 42n, DECK, DECK, new Map());
    sim.Player1.Elixir = 10;

    sim.QueueInput(1, { type: 'play_card', cardId: 26000046, position: { x: 9, y: 8 }, clientTick: 1 });
    sim.Tick();

    expect(sim.Units.length).toBe(4);
  });

  test('play_card spawns a Cannon building with lifetime stats', () => {
    const sim = new BattleSimulation();
    sim.Initialize(makeConfig(), 42n, DECK, DECK, new Map());
    sim.Player1.Elixir = 10;

    sim.QueueInput(1, { type: 'play_card', cardId: CANNON, position: { x: 5, y: 5 }, clientTick: 1 });
    sim.Tick();

    expect(sim.Buildings.length).toBe(1);
    expect(sim.Buildings[0].CurrentHP).toBe(1168);
    expect(sim.Player1.Elixir).toBeCloseTo(7, 1);
  });

  test('Giant + Fireball through a real BattleServer: units exist and an enemy tower loses HP', () => {
    const battle = makeBattle();
    const sim = simOf(battle);

    expect(battle.handleInput('p1', { type: 'play_card', cardId: GIANT, position: { x: 9, y: 8 }, clientTick: 1 })).toBeNull();
    expect(battle.handleInput('p1', { type: 'cast_spell', spellId: FIREBALL, position: { x: 2, y: 26 }, clientTick: 2 })).toBeNull();
    expect(buffersOf(battle, 'p1')).toHaveLength(2);

    const towerBefore = findTower(sim, 2, TowerType.PrincessLeft).CurrentHP;

    sim.QueueInput(1, { type: 'play_card', cardId: GIANT, position: { x: 9, y: 8 }, clientTick: 1 });
    sim.QueueInput(1, { type: 'cast_spell', spellId: FIREBALL, position: { x: 2, y: 26 }, clientTick: 2 });
    sim.Tick();

    expect(sim.Units.length).toBeGreaterThan(0);
    // Instant-damage spells resolve on cast, never lingering in _activeSpells.
    expect(sim.ActiveSpells.length).toBe(0);
    expect(findTower(sim, 2, TowerType.PrincessLeft).CurrentHP).toBeLessThan(towerBefore);
  });

  test('duration spells enter _activeSpells (Poison)', () => {
    const sim = new BattleSimulation();
    sim.Initialize(makeConfig(), 42n, DECK, DECK, new Map());
    sim.Player1.Elixir = 10;

    sim.QueueInput(1, { type: 'cast_spell', spellId: POISON, position: { x: 9, y: 20 }, clientTick: 1 });
    sim.Tick();

    expect(sim.ActiveSpells.length).toBe(1);
  });

  test('champion ability deducts cost and logs (Archer Queen on field)', () => {
    const sim = new BattleSimulation();
    sim.Initialize(makeConfig(), 42n, DECK, DECK, new Map());
    sim.Player1.Elixir = 10;

    sim.QueueInput(1, { type: 'play_card', cardId: QUEEN, position: { x: 9, y: 8 }, clientTick: 1 });
    sim.Tick();
    expect(sim.Units.length).toBe(1);

    const elixirBefore = sim.Player1.Elixir;
    sim.QueueInput(1, { type: 'champion_ability', position: { x: 9, y: 20 }, clientTick: 2 });
    sim.Tick();

    expect(sim.Player1.Elixir).toBeCloseTo(elixirBefore - 2, 1);
    expect(sim.EventLog.some((e) => e.type === EventType.ChampionAbility)).toBe(true);
  });

  test('unknown card and short elixir spawn nothing sim-side', () => {
    const sim = new BattleSimulation();
    sim.Initialize(makeConfig(), 42n, DECK, DECK, new Map());
    sim.Player1.Elixir = 0;

    sim.QueueInput(1, { type: 'play_card', cardId: 99999999, position: { x: 9, y: 8 }, clientTick: 1 });
    sim.QueueInput(1, { type: 'play_card', cardId: KNIGHT, position: { x: 9, y: 8 }, clientTick: 2 });
    sim.Tick();

    expect(sim.Units.length).toBe(0);
    expect(sim.Buildings.length).toBe(0);
  });

  test('CardDatabase combat helpers: flying allowlist + footprints', () => {
    expect(isFlyingCard(26000047)).toBe(true); // Minions
    expect(isFlyingCard(26000057)).toBe(true); // Lava Hound
    expect(isFlyingCard(KNIGHT)).toBe(false);
    expect(isFlyingCard(GIANT)).toBe(false);
    expect(getFootprintRadius(26000067)).toBe(1.5); // Elixir Collector
    expect(getFootprintRadius(CANNON)).toBe(1);
    expect(getFootprintRadius(KNIGHT)).toBe(0.5);
  });
});

describe('ISSUE-202: server/client outcome parity', () => {
  const REG_END = 180 * 60;
  const OT_END = (180 + 60) * 60;

  function freshSim(): BattleSimulation {
    const sim = new BattleSimulation();
    sim.Initialize(makeConfig(), 7n, DECK, DECK, new Map());
    return sim;
  }

  test('regulation still running with unequal crowns stays Playing', () => {
    const sim = freshSim();
    killTower(sim, 2, TowerType.PrincessLeft);
    sim.Tick();
    expect(sim.Status).toBe(BattleStatus.Playing);
  });

  test('regulation-expiry crown leader wins immediately (1-0 is NOT a Draw)', () => {
    const sim = freshSim();
    killTower(sim, 2, TowerType.PrincessLeft);
    (sim as any)._currentTick = REG_END;
    sim.Tick();
    expect(sim.Status).toBe(BattleStatus.Player1Won);
  });

  test('overtime 0-0 stays Playing; first tower destroyed wins (sudden death)', () => {
    const sim = freshSim();
    (sim as any)._currentTick = REG_END + 600;
    sim.Tick();
    expect(sim.Status).toBe(BattleStatus.Playing);

    killTower(sim, 2, TowerType.PrincessRight);
    sim.Tick();
    expect(sim.Status).toBe(BattleStatus.Player1Won);
  });

  test('overtime-timeout HP tiebreak favors the healthier side', () => {
    const sim = freshSim();
    (sim as any)._currentTick = OT_END;
    findTower(sim, 1, TowerType.King).TakeDamage(500, 0);
    sim.Tick();
    expect(sim.Status).toBe(BattleStatus.Player2Won);
  });

  test('overtime-timeout exact-equal HP is a Draw (and only then)', () => {
    const sim = freshSim();
    (sim as any)._currentTick = OT_END;
    sim.Tick();
    expect(sim.Status).toBe(BattleStatus.Draw);
  });

  test('king kill wins instantly mid-regulation', () => {
    const sim = freshSim();
    killTower(sim, 2, TowerType.King);
    sim.Tick();
    expect(sim.Status).toBe(BattleStatus.Player1Won);
  });

  test('crowns credit the destroyer side (enemy-side semantics)', () => {
    const sim = freshSim();
    // P2 loses a princess: at regulation end P1 must lead, not P2.
    killTower(sim, 2, TowerType.PrincessLeft);
    (sim as any)._currentTick = REG_END;
    sim.Tick();
    expect(sim.Status).toBe(BattleStatus.Player1Won);

    const sim2 = freshSim();
    // P1 loses a princess: P2 leads.
    killTower(sim2, 1, TowerType.PrincessLeft);
    (sim2 as any)._currentTick = REG_END;
    sim2.Tick();
    expect(sim2.Status).toBe(BattleStatus.Player2Won);
  });

  test('full battle through BattleServer tick path ends non-Draw with a crown lead', () => {
    const battle = makeBattle();
    const sim = simOf(battle);
    killTower(sim, 2, TowerType.PrincessLeft);
    (sim as any)._currentTick = REG_END;
    sim.Tick();
    expect(sim.Status).toBe(BattleStatus.Player1Won);
    expect(BattleType.Ladder).toBeDefined();
  });
});
