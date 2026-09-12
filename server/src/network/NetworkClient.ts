import WebSocket from 'ws';
import { Player } from '../../services/PlayerService';
import { NetworkMessage } from '../types';

export class NetworkClient {
  public readonly id: string;
  public readonly ws: WebSocket;
  public readonly connectedAt: number;
  public lastHeartbeat: number;
  
  public player: Player | null = null;
  public battleId: string | null = null;
  public authenticated = false;

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
      this.ws.send(JSON.stringify(message));
    } else {
      this.messageQueue.push(message);
    }
  }

  sendError(errorCode: string, details?: any): void {
    this.send({ type: 'error', code: errorCode, details });
  }

  flushQueue(): void {
    while (this.messageQueue.length > 0) {
      const msg = this.messageQueue.shift();
      if (msg) this.send(msg);
    }
  }

  private onClose(): void {
    // Handled by ConnectionManager
  }

  private onError(error: Error): void {
    logger.error('Client socket error', { clientId: this.id, error: error.message });
  }

  isAlive(): boolean {
    return this.ws.readyState === WebSocket.OPEN && 
           Date.now() - this.lastHeartbeat < 30000; // 30 second timeout
  }
}