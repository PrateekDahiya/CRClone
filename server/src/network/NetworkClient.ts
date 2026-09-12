import WebSocket from 'ws';
import { logger } from '../utils/logger';
import { Player } from '@/services/PlayerService';
import { NetworkMessage, PlayerInput } from '../types';

interface QueuedInput {
  input: PlayerInput;
  sentAt: number;
  retries: number;
}

export class NetworkClient {
  public readonly id: string;
  public readonly ws: WebSocket;
  public readonly connectedAt: number;
  public lastHeartbeat: number;
  
  public player: Player | null = null;
  public battleId: string | null = null;
  public authenticated = false;
  public acknowledgedTick = 0;
  public pendingInputs: Map<number, QueuedInput> = new Map();

  private messageQueue: NetworkMessage[] = [];

  constructor(ws: WebSocket, req: any, id: string) {
    this.id = id;
    this.ws = ws;
    this.connectedAt = Date.now();
    this.lastHeartbeat = Date.now();

    ws.on('close', () => this.onClose());
    ws.on('error', (error) => this.onError(error));
  }

  authenticate(player: Player): void {
    this.player = player;
    this.authenticated = true;
  }

  send(message: any): void {
    if (this.ws.readyState === WebSocket.OPEN) {
      const data = JSON.stringify(message);
      this.ws.send(data);
    } else {
      this.messageQueue.push(message);
    }
  }

  sendBinary(data: Buffer): void {
    if (this.ws.readyState === WebSocket.OPEN) {
      this.ws.send(data);
    } else {
      logger.warn('Cannot send binary: WebSocket not open', { clientId: this.id });
    }
  }

  sendError(errorCode: string, details?: any): void {
    this.send({ type: 'error', code: errorCode, details });
  }

  queueInput(input: PlayerInput): void {
    const existing = this.pendingInputs.get(input.clientTick);
    if (existing) {
      existing.input = input;
      existing.sentAt = Date.now();
    } else {
      this.pendingInputs.set(input.clientTick, {
        input,
        sentAt: Date.now(),
        retries: 0,
      });
    }
  }

  acknowledgeInput(ackTick: number): void {
    for (const [tick] of this.pendingInputs) {
      if (tick <= ackTick) {
        this.pendingInputs.delete(tick);
      }
    }
    this.acknowledgedTick = Math.max(this.acknowledgedTick, ackTick);
  }

  getUnacknowledgedInputs(): PlayerInput[] {
    return Array.from(this.pendingInputs.values()).map(q => q.input);
  }

  retryUnacknowledgedInputs(maxAge = 1000): PlayerInput[] {
    const now = Date.now();
    const toRetry: PlayerInput[] = [];
    
    for (const [tick, queued] of this.pendingInputs) {
      if (now - queued.sentAt > maxAge && queued.retries < 3) {
        queued.retries++;
        queued.sentAt = now;
        toRetry.push(queued.input);
      }
    }
    
    return toRetry;
  }

  flushQueue(): void {
    while (this.messageQueue.length > 0) {
      const msg = this.messageQueue.shift();
      if (msg) this.send(msg);
    }
  }

  private onClose(): void {
    this.pendingInputs.clear();
    this.messageQueue = [];
  }

  private onError(error: Error): void {
    logger.error('Client socket error', { clientId: this.id, error: error.message });
  }

  isAlive(): boolean {
    return this.ws.readyState === WebSocket.OPEN && 
           Date.now() - this.lastHeartbeat < 30000;
  }

  getPendingInputCount(): number {
    return this.pendingInputs.size;
  }
}