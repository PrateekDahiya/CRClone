import * as protobuf from 'protobufjs';
import * as path from 'path';
import { logger } from '../utils/logger';

let root: protobuf.Root | null = null;
const protoPath = path.join(__dirname, 'protocol.proto');

export async function loadProtocol(): Promise<protobuf.Root> {
  if (root) return root;

  try {
    root = await protobuf.load(protoPath);
    logger.info('Protocol loaded successfully');
    return root;
  } catch (error) {
    logger.error('Failed to load protocol', { error: error instanceof Error ? error.message : String(error) });
    throw error;
  }
}

export function getProtocolRoot(): protobuf.Root | null {
  return root;
}

// Wire-contract type union. Must match C# MessageTypes constants
// (Assets/Scripts/Network/MessageTypes.cs) exactly.
export const MessageType = {
  // Auth
  Auth: 'auth',
  AuthResponse: 'auth_response',

  // Matchmaking
  Matchmaking: 'matchmaking',
  MatchmakingStarted: 'matchmaking_started',
  BattleFound: 'battle_found',
  BattleStart: 'battle_start',

  // Input
  Input: 'input',
  InputAck: 'input_ack',

  // Game State
  GameState: 'game_state',
  Reconcile: 'reconcile',

  // Battle End
  BattleEnd: 'battle_end',

  // Error
  Error: 'error',

  // Heartbeat
  Heartbeat: 'heartbeat',
  Pong: 'pong',

  // Deck
  SaveDeck: 'save_deck',
  DeckSaved: 'deck_saved',

  // Clan
  Clan: 'clan',
  ClanResponse: 'clan_response',

  // Shop
  Shop: 'shop',
  ShopResponse: 'shop_response',

  // Quest
  Quest: 'quest',
  QuestResponse: 'quest_response',

  // Season
  Season: 'season',
  SeasonResponse: 'season_response',

  // Tournament
  Tournament: 'tournament',
  TournamentResponse: 'tournament_response',

  // Replay
  Replay: 'replay',
  ReplayResponse: 'replay_response',

  // Player
  Player: 'player',
  PlayerResponse: 'player_response',
} as const;

// All message types as a union for type safety
export type MessageTypeValue = typeof MessageType[keyof typeof MessageType];

// Maps wire `type` string -> proto message name.
// Field numbers inside each proto message match the C#
// [ProtoMember(N)] numbers in MessageTypes.cs exactly.
export const TYPE_TO_PROTO: Record<MessageTypeValue, string> = {
  [MessageType.Auth]: 'AuthMessage',
  [MessageType.AuthResponse]: 'AuthResponseMessage',
  [MessageType.Matchmaking]: 'MatchmakingRequest',
  [MessageType.MatchmakingStarted]: 'MatchmakingStartedMessage',
  [MessageType.BattleFound]: 'BattleFoundMessage',
  [MessageType.BattleStart]: 'BattleStartMessage',
  [MessageType.Input]: 'InputMessage',
  [MessageType.InputAck]: 'InputAckMessage',
  [MessageType.GameState]: 'GameStateMessage',
  [MessageType.Reconcile]: 'ReconcileMessage',
  [MessageType.BattleEnd]: 'BattleEndMessage',
  [MessageType.Error]: 'ErrorMessage',
  [MessageType.Heartbeat]: 'HeartbeatMessage',
  [MessageType.Pong]: 'PongMessage',
  [MessageType.SaveDeck]: 'SaveDeckRequest',
  [MessageType.DeckSaved]: 'DeckSavedMessage',
  [MessageType.Clan]: 'ClanMessage',
  [MessageType.ClanResponse]: 'ClanResponseMessage',
  [MessageType.Shop]: 'ShopMessage',
  [MessageType.ShopResponse]: 'ShopResponseMessage',
  [MessageType.Quest]: 'QuestMessage',
  [MessageType.QuestResponse]: 'QuestResponseMessage',
  [MessageType.Season]: 'SeasonMessage',
  [MessageType.SeasonResponse]: 'SeasonResponseMessage',
  [MessageType.Tournament]: 'TournamentMessage',
  [MessageType.TournamentResponse]: 'TournamentResponseMessage',
  [MessageType.Replay]: 'ReplayMessage',
  [MessageType.ReplayResponse]: 'ReplayResponseMessage',
  [MessageType.Player]: 'PlayerMessage',
  [MessageType.PlayerResponse]: 'PlayerResponseMessage',
};

export interface ProtoMessage {
  type: MessageTypeValue;
  requestId?: number;
  timestamp?: number;
  [key: string]: any;
}

function lookupProtoType(type: string): protobuf.Type {
  if (!root) throw new Error('Protocol not loaded');
  const protoName = (TYPE_TO_PROTO as Record<string, string>)[type];
  if (!protoName) throw new Error(`Unknown message type: ${type}`);
  return root.lookupType(`crclone.${protoName}`);
}

export function encodeMessage<T extends ProtoMessage>(message: T): Uint8Array {
  const protoType = lookupProtoType(message.type);
  // protobufjs uses camelCase JS property names for snake_case proto fields.
  const payload = protoType.fromObject(message as unknown as Record<string, unknown>);
  const errMsg = protoType.verify(payload);
  if (errMsg) throw new Error(`Message validation failed: ${errMsg}`);

  return protoType.encode(payload).finish();
}

export function peekMessageType(data: Uint8Array): string {
  if (!root) throw new Error('Protocol not loaded');
  const envelopeType = root.lookupType('crclone.NetworkMessageEnvelope');
  const decoded = envelopeType.decode(data) as unknown as { type?: string };
  if (!decoded.type) throw new Error('Message envelope missing type field');
  return decoded.type;
}

export function decodeMessage(data: Uint8Array): ProtoMessage {
  const type = peekMessageType(data);
  return decodeMessageByType(data, type);
}

export function decodeMessageByType(data: Uint8Array, type: string): ProtoMessage {
  const protoType = lookupProtoType(type);
  const decoded = protoType.decode(data);
  const obj = protoType.toObject(decoded, {
    defaults: true,
    arrays: true,
    objects: true,
    oneofs: false,
  }) as unknown as ProtoMessage;
  // Ensure the wire `type` string is present even if sender omitted it.
  if (!obj.type) obj.type = type as MessageTypeValue;
  return obj;
}

// Auth
export function createAuthMessage(token: string): ProtoMessage {
  return {
    type: MessageType.Auth,
    token,
    timestamp: Date.now(),
  };
}

export function createAuthResponseMessage(success: boolean, playerId?: string, error?: string): ProtoMessage {
  return {
    type: MessageType.AuthResponse,
    success,
    playerId,
    error,
    timestamp: Date.now(),
  };
}

// Matchmaking
export function createMatchmakingRequest(battleType: string): ProtoMessage {
  return {
    type: MessageType.Matchmaking,
    battleType,
    timestamp: Date.now(),
  };
}

export function createMatchmakingStartedMessage(battleType: string): ProtoMessage {
  return {
    type: MessageType.MatchmakingStarted,
    battleType,
    timestamp: Date.now(),
  };
}

export function createBattleFoundMessage(
  battleId: string,
  seed: number,
  player1: any,
  player2: any,
  is2v2 = false
): ProtoMessage {
  return {
    type: MessageType.BattleFound,
    battleId,
    seed,
    player1,
    player2,
    is2v2,
    timestamp: Date.now(),
  };
}

export function createBattleStartMessage(
  battleId: string,
  tick: number,
  player1: any,
  player2: any
): ProtoMessage {
  return {
    type: MessageType.BattleStart,
    battleId,
    tick,
    player1,
    player2,
    timestamp: Date.now(),
  };
}

// Input
export function createInputMessage(tick: number, input: any): ProtoMessage {
  return {
    type: MessageType.Input,
    tick,
    input,
    timestamp: Date.now(),
  };
}

export function createInputAckMessage(ackTick: number): ProtoMessage {
  return {
    type: MessageType.InputAck,
    ackTick,
    timestamp: Date.now(),
  };
}

// Game State
export function createGameStateMessage(state: any): ProtoMessage {
  return {
    type: MessageType.GameState,
    tick: state.tick,
    entities: state.entities,
    projectiles: state.projectiles,
    player1: state.player1,
    player2: state.player2,
    status: state.status,
    timestamp: Date.now(),
  };
}

export function createReconcileMessage(tick: number, entities: any[]): ProtoMessage {
  return {
    type: MessageType.Reconcile,
    tick,
    entities,
    timestamp: Date.now(),
  };
}

// Battle End
export function createBattleEndMessage(battleId: string, result: any): ProtoMessage {
  return {
    type: MessageType.BattleEnd,
    battleId,
    result,
    timestamp: Date.now(),
  };
}

// Error
export function createErrorMessage(message: string, code?: string): ProtoMessage {
  return {
    type: MessageType.Error,
    message,
    code,
    timestamp: Date.now(),
  };
}

// Heartbeat
export function createHeartbeatMessage(): ProtoMessage {
  return {
    type: MessageType.Heartbeat,
    timestamp: Date.now(),
  };
}

export function createPongMessage(): ProtoMessage {
  return {
    type: MessageType.Pong,
    timestamp: Date.now(),
  };
}

// Deck
export function createSaveDeckRequest(cardIds: number[]): ProtoMessage {
  return {
    type: MessageType.SaveDeck,
    cardIds,
    timestamp: Date.now(),
  };
}

export function createDeckSavedMessage(success: boolean): ProtoMessage {
  return {
    type: MessageType.DeckSaved,
    success,
    timestamp: Date.now(),
  };
}

// Clan
export function createClanMessage(action: string, data: Uint8Array): ProtoMessage {
  return {
    type: MessageType.Clan,
    action,
    data,
    timestamp: Date.now(),
  };
}

export function createClanResponseMessage(action: string, success: boolean, data: Uint8Array): ProtoMessage {
  return {
    type: MessageType.ClanResponse,
    action,
    success,
    data,
    timestamp: Date.now(),
  };
}

// Shop
export function createShopMessage(action: string, data: Uint8Array): ProtoMessage {
  return {
    type: MessageType.Shop,
    action,
    data,
    timestamp: Date.now(),
  };
}

export function createShopResponseMessage(action: string, data: Uint8Array): ProtoMessage {
  return {
    type: MessageType.ShopResponse,
    action,
    data,
    timestamp: Date.now(),
  };
}

// Quest
export function createQuestMessage(action: string, data: Uint8Array): ProtoMessage {
  return {
    type: MessageType.Quest,
    action,
    data,
    timestamp: Date.now(),
  };
}

export function createQuestResponseMessage(action: string, data: Uint8Array): ProtoMessage {
  return {
    type: MessageType.QuestResponse,
    action,
    data,
    timestamp: Date.now(),
  };
}

// Season
export function createSeasonMessage(action: string, data: Uint8Array): ProtoMessage {
  return {
    type: MessageType.Season,
    action,
    data,
    timestamp: Date.now(),
  };
}

export function createSeasonResponseMessage(action: string, data: Uint8Array): ProtoMessage {
  return {
    type: MessageType.SeasonResponse,
    action,
    data,
    timestamp: Date.now(),
  };
}

// Tournament
export function createTournamentMessage(action: string, data: Uint8Array): ProtoMessage {
  return {
    type: MessageType.Tournament,
    action,
    data,
    timestamp: Date.now(),
  };
}

export function createTournamentResponseMessage(action: string, data: Uint8Array): ProtoMessage {
  return {
    type: MessageType.TournamentResponse,
    action,
    data,
    timestamp: Date.now(),
  };
}

// Replay
export function createReplayMessage(action: string, data: Uint8Array): ProtoMessage {
  return {
    type: MessageType.Replay,
    action,
    data,
    timestamp: Date.now(),
  };
}

export function createReplayResponseMessage(action: string, data: Uint8Array): ProtoMessage {
  return {
    type: MessageType.ReplayResponse,
    action,
    data,
    timestamp: Date.now(),
  };
}

// Player
export function createPlayerMessage(action: string, data: Uint8Array): ProtoMessage {
  return {
    type: MessageType.Player,
    action,
    data,
    timestamp: Date.now(),
  };
}

export function createPlayerResponseMessage(action: string, data: Uint8Array): ProtoMessage {
  return {
    type: MessageType.PlayerResponse,
    action,
    data,
    timestamp: Date.now(),
  };
}

// Utility
export function serializeToJson(message: ProtoMessage): string {
  return JSON.stringify(message);
}

export function parseFromJson(json: string): ProtoMessage {
  return JSON.parse(json);
}

// Contract test helper - returns all expected message types
export function getAllMessageTypes(): MessageTypeValue[] {
  return Object.values(MessageType);
}

// Verify a message type has a matching proto definition
export function verifyMessageType(type: string): boolean {
  if (!root) return false;
  try {
    const protoName = (TYPE_TO_PROTO as Record<string, string>)[type];
    if (!protoName) return false;
    root.lookupType(`crclone.${protoName}`);
    return true;
  } catch {
    return false;
  }
}
