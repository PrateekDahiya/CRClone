// Core type definitions for CRClone Server

export enum BattleType {
  Ladder = 'ladder',
  TwoVTwo = '2v2',
  Tournament = 'tournament',
  Challenge = 'challenge',
  Friendly = 'friendly',
  Practice = 'practice',
  ClanWar = 'clan_war'
}

export enum BattleStatus {
  Waiting = 'waiting',
  Playing = 'playing',
  Paused = 'paused',
  Player1Won = 'player1_won',
  Player2Won = 'player2_won',
  Draw = 'draw'
}

export enum CardRarity {
  Common = 'common',
  Rare = 'rare',
  Epic = 'epic',
  Legendary = 'legendary',
  Champion = 'champion'
}

export enum CardType {
  Troop = 'troop',
  Spell = 'spell',
  Building = 'building',
  Champion = 'champion'
}

export enum EntityType {
  Unit = 'unit',
  Building = 'building',
  Projectile = 'projectile',
  SpellEffect = 'spell_effect',
  Tower = 'tower'
}

export enum TowerType {
  King = 'king',
  PrincessLeft = 'princess_left',
  PrincessRight = 'princess_right'
}

export interface Vector2 {
  x: number;
  y: number;
}

export interface CardData {
  cardId: number;
  name: string;
  rarity: CardRarity;
  type: CardType;
  elixirCost: number;
  hitpoints: number;
  damage: number;
  hitSpeed: number;
  range: number;
  speed: string;
  deployTime: number;
  targetType: string;
  count: number;
  mechanics: Record<string, any>;
}

export interface PlayerState {
  playerId: number;
  elixir: number;
  hand: number[];
  deck: number[];
  nextCardIndex: number;
  kingTowerActivated: boolean;
}

export interface EntityState {
  id: number;
  type: EntityType;
  owner: number;
  position: Vector2;
  velocity: Vector2;
  hp: number;
  maxHp: number;
  targetId?: number;
  isDead: boolean;
  // Unit specific
  state?: string;
  attackCooldown?: number;
  // Building specific
  lifetime?: number;
  isRetracted?: boolean;
  spawnTimer?: number;
  // Projectile specific
  sourceId?: number;
  isBeam?: boolean;
  // Spell specific
  spellType?: string;
  radius?: number;
  remainingTime?: number;
}

export interface PlayerInput {
  type: 'play_card' | 'cast_spell' | 'champion_ability' | 'emote';
  cardId?: number;
  spellId?: number;
  position: Vector2;
  targetPosition?: Vector2;
  clientTick: number;
}

export interface GameState {
  tick: number;
  entities: EntityState[];
  projectiles: EntityState[];
  player1: PlayerState;
  player2: PlayerState;
  status: BattleStatus;
}

export interface BattleData {
  battleId: string;
  type: BattleType;
  seed: number;
  player1: PlayerBattleInfo;
  player2: PlayerBattleInfo;
}

export interface PlayerBattleInfo {
  playerId: string;
  username: string;
  trophies: number;
  deck: number[];
  kingTowerLevel: number;
  princessTowerLevel: number;
}

export interface BattleResult {
  battleId: string;
  winner: 'player1' | 'player2' | 'draw';
  player1Crowns: number;
  player2Crowns: number;
  player1TrophyChange: number;
  player2TrophyChange: number;
  duration: number;
  wentOvertime: boolean;
  replayId: string;
}

export interface NetworkMessage {
  type: string;
  requestId?: number;
  timestamp?: number;
}

export interface AuthMessage extends NetworkMessage {
  type: 'auth';
  token: string;
}

export interface AuthResponseMessage extends NetworkMessage {
  type: 'auth_response';
  success: boolean;
  playerId?: string;
  error?: string;
}

export interface MatchmakingRequest extends NetworkMessage {
  type: 'matchmaking';
  battleType: BattleType;
}

export interface BattleFoundMessage extends NetworkMessage {
  type: 'battle_found';
  battleId: string;
  seed: number;
  player1: PlayerBattleInfo;
  player2: PlayerBattleInfo;
}

export interface InputMessage extends NetworkMessage {
  type: 'input';
  tick: number;
  input: PlayerInput;
}

export interface GameStateMessage extends NetworkMessage {
  type: 'game_state';
  tick: number;
  entities: EntityState[];
  projectiles: EntityState[];
  player1: PlayerState;
  player2: PlayerState;
  status: BattleStatus;
}

export interface InputAckMessage extends NetworkMessage {
  type: 'input_ack';
  ackTick: number;
}

export interface ReconcileMessage extends NetworkMessage {
  type: 'reconcile';
  tick: number;
  entities: EntityState[];
}

export interface BattleEndMessage extends NetworkMessage {
  type: 'battle_end';
  battleId: string;
  result: BattleResult;
}

export interface ErrorMessage extends NetworkMessage {
  type: 'error';
  message: string;
}

export interface HeartbeatMessage extends NetworkMessage {
  type: 'heartbeat';
}

export interface SaveDeckRequest extends NetworkMessage {
  type: 'save_deck';
  cardIds: number[];
}

export interface MatchmakingQueueEntry {
  playerId: string;
  username: string;
  trophies: number;
  deck: number[];
  battleType: BattleType;
  joinedAt: number;
}