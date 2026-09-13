import { CardType } from '../types';

// Server-side card catalogue for authoritative input validation.
// Costs match Clash Royale tournament-standard values and the
// in-repo test decks (Assets/Tests/TestFixtures/TestDecks.cs).
// Unknown cardId => undefined (caller must reject the input).

export type ServerCardType = 'troop' | 'spell' | 'building' | 'champion';

export type ServerTargetType = 'ground' | 'air' | 'both' | 'building';

export interface CardCombatStats {
  hp: number;
  damage: number;
  hitSpeed: number;
  range: number;
  /** Movement speed in arena tiles per second (C# SpeedType mapped: 1->0.75, 2->1, 3->1.5, 4->2). */
  speed: number;
  /** Units spawned per play; buildings always 1. */
  count: number;
  targetType: ServerTargetType;
  isFlying: boolean;
  /** Seconds before an unattended building expires (CR-standard values). */
  lifetime?: number;
  /** Spell radius in tiles. */
  spellRadius?: number;
  /** Duration in seconds for damage-over-time / control spells; 0/omitted = instant. */
  spellDuration?: number;
  /** Damage per second for duration spells. */
  spellDps?: number;
  /** True when the spell applies its damage immediately on cast (not via _activeSpells). */
  spellInstant?: boolean;
}

export interface CardDefinition {
  cardId: number;
  name: string;
  type: ServerCardType;
  elixirCost: number;
  rarity: 'common' | 'rare' | 'epic' | 'legendary' | 'champion';
  stats: CardCombatStats;
}

// Base combat stats mirror Assets/Resources/Data/Cards/*.asset base values
// (baseHitpoints/baseDamage/baseHitSpeed/baseRange/speed/targetType/count)
// at tournament-standard level 1; Golem / Ice Golem use CR-standard
// approximations (no .asset with those exact names exists).
const CARDS: Record<number, CardDefinition> = {
  26000040: { cardId: 26000040, name: 'Knight', type: 'troop', elixirCost: 3, rarity: 'common',
    stats: { hp: 1520, damage: 160, hitSpeed: 1.1, range: 1.2, speed: 1.0, count: 1, targetType: 'ground', isFlying: false } },
  26000041: { cardId: 26000041, name: 'Archers', type: 'troop', elixirCost: 3, rarity: 'common',
    stats: { hp: 256, damage: 82, hitSpeed: 1.2, range: 5, speed: 1.0, count: 2, targetType: 'both', isFlying: false } },
  26000042: { cardId: 26000042, name: 'Giant', type: 'troop', elixirCost: 5, rarity: 'rare',
    stats: { hp: 3392, damage: 210, hitSpeed: 1.5, range: 1.2, speed: 0.75, count: 1, targetType: 'building', isFlying: false } },
  26000043: { cardId: 26000043, name: 'Musketeer', type: 'troop', elixirCost: 4, rarity: 'rare',
    stats: { hp: 592, damage: 164, hitSpeed: 1.1, range: 6, speed: 1.0, count: 1, targetType: 'both', isFlying: false } },
  26000044: { cardId: 26000044, name: 'Fireball', type: 'spell', elixirCost: 4, rarity: 'rare',
    stats: { hp: 100, damage: 572, hitSpeed: 1, range: 1.2, speed: 1.0, count: 1, targetType: 'ground', isFlying: false, spellRadius: 2.5, spellInstant: true } },
  26000045: { cardId: 26000045, name: 'Cannon', type: 'building', elixirCost: 3, rarity: 'common',
    stats: { hp: 1168, damage: 132, hitSpeed: 0.8, range: 5.5, speed: 1.0, count: 1, targetType: 'ground', isFlying: false, lifetime: 30 } },
  26000046: { cardId: 26000046, name: 'Skeletons', type: 'troop', elixirCost: 1, rarity: 'common',
    stats: { hp: 67, damage: 67, hitSpeed: 1.1, range: 1.2, speed: 1.5, count: 4, targetType: 'ground', isFlying: false } },
  26000047: { cardId: 26000047, name: 'Minions', type: 'troop', elixirCost: 3, rarity: 'common',
    stats: { hp: 140, damage: 70, hitSpeed: 1.0, range: 2, speed: 1.5, count: 3, targetType: 'both', isFlying: true } },
  26000048: { cardId: 26000048, name: 'Zap', type: 'spell', elixirCost: 2, rarity: 'common',
    stats: { hp: 100, damage: 159, hitSpeed: 1, range: 1.2, speed: 1.0, count: 1, targetType: 'ground', isFlying: false, spellRadius: 2.5, spellInstant: true } },
  26000049: { cardId: 26000049, name: 'The Log', type: 'spell', elixirCost: 2, rarity: 'legendary',
    stats: { hp: 100, damage: 240, hitSpeed: 1, range: 11.5, speed: 1.0, count: 1, targetType: 'ground', isFlying: false, spellRadius: 2.5, spellInstant: true } },
  26000050: { cardId: 26000050, name: 'Poison', type: 'spell', elixirCost: 4, rarity: 'epic',
    stats: { hp: 100, damage: 65, hitSpeed: 1, range: 1.2, speed: 1.0, count: 1, targetType: 'ground', isFlying: false, spellRadius: 3.5, spellDuration: 8, spellDps: 130 } },
  26000051: { cardId: 26000051, name: 'Rocket', type: 'spell', elixirCost: 6, rarity: 'rare',
    stats: { hp: 100, damage: 1080, hitSpeed: 1, range: 1.2, speed: 1.0, count: 1, targetType: 'ground', isFlying: false, spellRadius: 2, spellInstant: true } },
  26000052: { cardId: 26000052, name: 'Arrows', type: 'spell', elixirCost: 3, rarity: 'common',
    stats: { hp: 100, damage: 168, hitSpeed: 1, range: 1.2, speed: 1.0, count: 1, targetType: 'ground', isFlying: false, spellRadius: 4, spellInstant: true } },
  26000053: { cardId: 26000053, name: 'Freeze', type: 'spell', elixirCost: 4, rarity: 'epic',
    stats: { hp: 100, damage: 0, hitSpeed: 1, range: 1.2, speed: 1.0, count: 1, targetType: 'ground', isFlying: false, spellRadius: 3, spellDuration: 4, spellDps: 0 } },
  26000054: { cardId: 26000054, name: 'Tornado', type: 'spell', elixirCost: 3, rarity: 'epic',
    stats: { hp: 100, damage: 0, hitSpeed: 1, range: 1.2, speed: 1.0, count: 1, targetType: 'ground', isFlying: false, spellRadius: 5.5, spellDuration: 1.5, spellDps: 0 } },
  26000055: { cardId: 26000055, name: 'Golem', type: 'troop', elixirCost: 8, rarity: 'epic',
    stats: { hp: 4500, damage: 300, hitSpeed: 2.5, range: 1.2, speed: 0.75, count: 1, targetType: 'building', isFlying: false } },
  26000056: { cardId: 26000056, name: 'Mega Knight', type: 'troop', elixirCost: 7, rarity: 'legendary',
    stats: { hp: 3344, damage: 264, hitSpeed: 1.7, range: 1.2, speed: 1.0, count: 1, targetType: 'ground', isFlying: false } },
  26000057: { cardId: 26000057, name: 'Lava Hound', type: 'troop', elixirCost: 7, rarity: 'legendary',
    stats: { hp: 3248, damage: 44, hitSpeed: 1.8, range: 2.5, speed: 0.75, count: 1, targetType: 'building', isFlying: true } },
  26000058: { cardId: 26000058, name: 'Wizard', type: 'troop', elixirCost: 5, rarity: 'rare',
    stats: { hp: 592, damage: 324, hitSpeed: 1.4, range: 5.5, speed: 1.0, count: 1, targetType: 'both', isFlying: false } },
  26000059: { cardId: 26000059, name: 'Ice Spirit', type: 'troop', elixirCost: 1, rarity: 'common',
    stats: { hp: 132, damage: 66, hitSpeed: 1.1, range: 2.5, speed: 2.0, count: 1, targetType: 'ground', isFlying: false } },
  // CycleDeck slot 7 is commented "Cannon" in TestDecks.cs; kept in sync with that comment.
  26000060: { cardId: 26000060, name: 'Cannon', type: 'building', elixirCost: 3, rarity: 'common',
    stats: { hp: 1168, damage: 132, hitSpeed: 0.8, range: 5.5, speed: 1.0, count: 1, targetType: 'ground', isFlying: false, lifetime: 30 } },
  26000061: { cardId: 26000061, name: 'Ice Golem', type: 'troop', elixirCost: 2, rarity: 'rare',
    stats: { hp: 1000, damage: 100, hitSpeed: 1.1, range: 1.2, speed: 1.0, count: 1, targetType: 'building', isFlying: false } },
  26000062: { cardId: 26000062, name: 'Tesla', type: 'building', elixirCost: 4, rarity: 'common',
    stats: { hp: 840, damage: 160, hitSpeed: 0.8, range: 5.5, speed: 1.0, count: 1, targetType: 'both', isFlying: false, lifetime: 40 } },
  26000063: { cardId: 26000063, name: 'Inferno Tower', type: 'building', elixirCost: 5, rarity: 'rare',
    stats: { hp: 1840, damage: 50, hitSpeed: 0.4, range: 6, speed: 1.0, count: 1, targetType: 'both', isFlying: false, lifetime: 40 } },
  26000064: { cardId: 26000064, name: 'Goblin Hut', type: 'building', elixirCost: 4, rarity: 'rare',
    stats: { hp: 1296, damage: 10, hitSpeed: 1, range: 1.2, speed: 1.0, count: 1, targetType: 'ground', isFlying: false, lifetime: 60 } },
  26000065: { cardId: 26000065, name: 'Furnace', type: 'building', elixirCost: 4, rarity: 'rare',
    stats: { hp: 1200, damage: 10, hitSpeed: 1, range: 1.2, speed: 1.0, count: 1, targetType: 'ground', isFlying: false, lifetime: 50 } },
  26000066: { cardId: 26000066, name: 'Bomb Tower', type: 'building', elixirCost: 4, rarity: 'rare',
    stats: { hp: 1440, damage: 224, hitSpeed: 1.7, range: 6, speed: 1.0, count: 1, targetType: 'ground', isFlying: false, lifetime: 40 } },
  26000067: { cardId: 26000067, name: 'Elixir Collector', type: 'building', elixirCost: 6, rarity: 'rare',
    stats: { hp: 1440, damage: 10, hitSpeed: 1, range: 1.2, speed: 1.0, count: 1, targetType: 'ground', isFlying: false, lifetime: 70 } },
  27000000: { cardId: 27000000, name: 'Archer Queen', type: 'champion', elixirCost: 5, rarity: 'champion',
    stats: { hp: 1280, damage: 170, hitSpeed: 1.2, range: 7, speed: 1.0, count: 1, targetType: 'both', isFlying: false } },
};

// Default champion-ability activation cost (matches Unity BattleSimulation default).
export const CHAMPION_ABILITY_COST = 2;

export function getCardDefinition(cardId: number): CardDefinition | undefined {
  return CARDS[cardId];
}

export function getCardElixirCost(cardId: number): number | undefined {
  return CARDS[cardId]?.elixirCost;
}

export function isKnownCard(cardId: number): boolean {
  return CARDS[cardId] !== undefined;
}

export function getCardStats(cardId: number): CardCombatStats | undefined {
  return CARDS[cardId]?.stats;
}

// Flying allowlist mirroring C# IsValidDeployPosition()
// (BattleSimulation.cs:335-339): mechanics "flying" flag plus the same
// name fragments, so future flying cards stay exempt without a DB change.
const FLYING_NAME_HINTS = [
  'minion', 'bat', 'dragon', 'balloon', 'phoenix', 'lava hound', 'skeleton dragon', 'mega minion',
];

export function isFlyingCard(cardId: number): boolean {
  const def = CARDS[cardId];
  if (!def) return false;
  if (def.stats.isFlying) return true;
  const name = def.name.toLowerCase();
  return FLYING_NAME_HINTS.some((hint) => name.includes(hint));
}

// Building footprint radii mirroring C# GetBuildingFootprintRadius()
// (X-Bow/Mortar 2, Elixir Collector 1.5, default building 1, others 0.5).
export function getFootprintRadius(cardId: number): number {
  const def = CARDS[cardId];
  if (!def) return 0.5;
  if (def.type !== 'building') return 0.5;
  if (def.name === 'X-Bow' || def.name === 'Mortar') return 2;
  if (def.name === 'Elixir Collector') return 1.5;
  return 1;
}

export function toServerCardType(t: CardType | string): ServerCardType | undefined {
  switch (t) {
    case 'troop':
    case 'spell':
    case 'building':
    case 'champion':
      return t;
    default:
      return undefined;
  }
}
