import { CardType } from '../types';

// Server-side card catalogue for authoritative input validation.
// Costs match Clash Royale tournament-standard values and the
// in-repo test decks (Assets/Tests/TestFixtures/TestDecks.cs).
// Unknown cardId => undefined (caller must reject the input).

export type ServerCardType = 'troop' | 'spell' | 'building' | 'champion';

export interface CardDefinition {
  cardId: number;
  name: string;
  type: ServerCardType;
  elixirCost: number;
}

const CARDS: Record<number, CardDefinition> = {
  26000040: { cardId: 26000040, name: 'Knight', type: 'troop', elixirCost: 3 },
  26000041: { cardId: 26000041, name: 'Archers', type: 'troop', elixirCost: 3 },
  26000042: { cardId: 26000042, name: 'Giant', type: 'troop', elixirCost: 5 },
  26000043: { cardId: 26000043, name: 'Musketeer', type: 'troop', elixirCost: 4 },
  26000044: { cardId: 26000044, name: 'Fireball', type: 'spell', elixirCost: 4 },
  26000045: { cardId: 26000045, name: 'Cannon', type: 'building', elixirCost: 3 },
  26000046: { cardId: 26000046, name: 'Skeletons', type: 'troop', elixirCost: 1 },
  26000047: { cardId: 26000047, name: 'Minions', type: 'troop', elixirCost: 3 },
  26000048: { cardId: 26000048, name: 'Zap', type: 'spell', elixirCost: 2 },
  26000049: { cardId: 26000049, name: 'The Log', type: 'spell', elixirCost: 2 },
  26000050: { cardId: 26000050, name: 'Poison', type: 'spell', elixirCost: 4 },
  26000051: { cardId: 26000051, name: 'Rocket', type: 'spell', elixirCost: 6 },
  26000052: { cardId: 26000052, name: 'Arrows', type: 'spell', elixirCost: 3 },
  26000053: { cardId: 26000053, name: 'Freeze', type: 'spell', elixirCost: 4 },
  26000054: { cardId: 26000054, name: 'Tornado', type: 'spell', elixirCost: 3 },
  26000055: { cardId: 26000055, name: 'Golem', type: 'troop', elixirCost: 8 },
  26000056: { cardId: 26000056, name: 'Mega Knight', type: 'troop', elixirCost: 7 },
  26000057: { cardId: 26000057, name: 'Lava Hound', type: 'troop', elixirCost: 7 },
  26000058: { cardId: 26000058, name: 'Wizard', type: 'troop', elixirCost: 5 },
  26000059: { cardId: 26000059, name: 'Ice Spirit', type: 'troop', elixirCost: 1 },
  // CycleDeck slot 7 is commented "Cannon" in TestDecks.cs; kept in sync with that comment.
  26000060: { cardId: 26000060, name: 'Cannon', type: 'building', elixirCost: 3 },
  26000061: { cardId: 26000061, name: 'Ice Golem', type: 'troop', elixirCost: 2 },
  26000062: { cardId: 26000062, name: 'Tesla', type: 'building', elixirCost: 4 },
  26000063: { cardId: 26000063, name: 'Inferno Tower', type: 'building', elixirCost: 5 },
  26000064: { cardId: 26000064, name: 'Goblin Hut', type: 'building', elixirCost: 4 },
  26000065: { cardId: 26000065, name: 'Furnace', type: 'building', elixirCost: 4 },
  26000066: { cardId: 26000066, name: 'Bomb Tower', type: 'building', elixirCost: 4 },
  26000067: { cardId: 26000067, name: 'Elixir Collector', type: 'building', elixirCost: 6 },
  27000000: { cardId: 27000000, name: 'Archer Queen', type: 'champion', elixirCost: 5 },
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
