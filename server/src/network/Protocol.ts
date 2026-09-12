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

export const MessageType = {
  Auth: 'auth',
  AuthResponse: 'auth_response',
  Matchmaking: 'matchmaking',
  MatchmakingStarted: 'matchmaking_started',
  BattleFound: 'battle_found',
  Input: 'input',
  InputAck: 'input_ack',
  GameState: 'game_state',
  Reconcile: 'reconcile',
  BattleEnd: 'battle_end',
  Error: 'error',
  Heartbeat: 'heartbeat',
  Pong: 'pong',
  SaveDeck: 'save_deck',
  DeckSaved: 'deck_saved',
} as const;

export interface ProtoMessage {
  type: string;
  requestId?: number;
  timestamp?: number;
  [key: string]: any;
}

export function encodeMessage<T extends ProtoMessage>(message: T): Uint8Array {
  if (!root) throw new Error('Protocol not loaded');
  
  const MessageType = root.lookupType('crclone.Message');
  const errMsg = MessageType.verify(message);
  if (errMsg) throw new Error(`Message validation failed: ${errMsg}`);
  
  return MessageType.encode(message).finish();
}

export function decodeMessage(data: Uint8Array): ProtoMessage {
  if (!root) throw new Error('Protocol not loaded');
  
  const MessageType = root.lookupType('crclone.Message');
  return MessageType.decode(data) as unknown as ProtoMessage;
}

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
  player2: any
): ProtoMessage {
  return {
    type: MessageType.BattleFound,
    battleId,
    seed,
    player1,
    player2,
    timestamp: Date.now(),
  };
}

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

export function createBattleEndMessage(battleId: string, result: any): ProtoMessage {
  return {
    type: MessageType.BattleEnd,
    battleId,
    result,
    timestamp: Date.now(),
  };
}

export function createErrorMessage(message: string, code?: string): ProtoMessage {
  return {
    type: MessageType.Error,
    message,
    code,
    timestamp: Date.now(),
  };
}

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

export function serializeToJson(message: ProtoMessage): string {
  return JSON.stringify(message);
}

export function parseFromJson(json: string): ProtoMessage {
  return JSON.parse(json);
}