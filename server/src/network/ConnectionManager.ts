import WebSocket from 'ws';
import { v4 as uuidv4 } from 'uuid';
import { logger } from '../utils/logger';
import { NetworkClient } from './NetworkClient';

export class ConnectionManager {
  private connections: Map<string, NetworkClient> = new Map();
  private battleConnections: Map<string, Set<string>> = new Map(); // battleId -> Set of clientIds
  private wsServer: WebSocket.Server;

  constructor(wsServer: WebSocket.Server) {
    this.wsServer = wsServer;
  }

  registerConnection(ws: WebSocket, req: any): NetworkClient {
    const client = new NetworkClient(ws, req, uuidv4());
    this.connections.set(client.id, client);
    return client;
  }

  unregisterConnection(clientId: string): void {
    const client = this.connections.get(clientId);
    if (client && client.battleId) {
      this.removeFromBattle(client.battleId, clientId);
    }
    this.connections.delete(clientId);
  }

  getConnection(clientId: string): NetworkClient | undefined {
    return this.connections.get(clientId);
  }

  getConnectionByPlayerId(playerId: string): NetworkClient | undefined {
    for (const client of this.connections.values()) {
      if (client.player?.id === playerId) {
        return client;
      }
    }
    return undefined;
  }

  getAllConnections(): NetworkClient[] {
    return Array.from(this.connections.values());
  }

  getAuthenticatedConnections(): NetworkClient[] {
    return Array.from(this.connections.values()).filter(c => c.player !== null);
  }

  setClientBattle(clientId: string, battleId: string | null): void {
    const client = this.connections.get(clientId);
    if (!client) return;

    if (client.battleId) {
      this.removeFromBattle(client.battleId, clientId);
    }

    client.battleId = battleId;

    if (battleId) {
      if (!this.battleConnections.has(battleId)) {
        this.battleConnections.set(battleId, new Set());
      }
      this.battleConnections.get(battleId)!.add(clientId);
    }
  }

  private removeFromBattle(battleId: string, clientId: string): void {
    const battleClients = this.battleConnections.get(battleId);
    if (battleClients) {
      battleClients.delete(clientId);
      if (battleClients.size === 0) {
        this.battleConnections.delete(battleId);
      }
    }
  }

  getBattleConnections(battleId: string): NetworkClient[] {
    const clientIds = this.battleConnections.get(battleId);
    if (!clientIds) return [];
    
    return Array.from(clientIds)
      .map(id => this.connections.get(id))
      .filter((c): c is NetworkClient => c !== undefined);
  }

  broadcast(message: any, excludeClientId?: string): void {
    const data = JSON.stringify(message);
    for (const client of this.connections.values()) {
      if (client.id !== excludeClientId && client.ws.readyState === WebSocket.OPEN) {
        client.ws.send(data);
      }
    }
  }

  broadcastToBattle(battleId: string, message: any, excludeClientId?: string): void {
    const clients = this.getBattleConnections(battleId);
    const data = JSON.stringify(message);
    
    for (const client of clients) {
      if (client.id !== excludeClientId && client.ws.readyState === WebSocket.OPEN) {
        client.ws.send(data);
      }
    }
  }

  broadcastBinaryToBattle(battleId: string, data: Buffer, excludeClientId?: string): void {
    const clients = this.getBattleConnections(battleId);
    
    for (const client of clients) {
      if (client.id !== excludeClientId && client.ws.readyState === WebSocket.OPEN) {
        client.sendBinary(data);
      }
    }
  }

  sendToPlayer(playerId: string, message: any): boolean {
    const client = this.getConnectionByPlayerId(playerId);
    if (client && client.ws.readyState === WebSocket.OPEN) {
      client.send(message);
      return true;
    }
    return false;
  }

  getConnectionCount(): number {
    return this.connections.size;
  }

  getAuthenticatedCount(): number {
    return this.getAuthenticatedConnections().length;
  }

  getBattleCount(): number {
    return this.battleConnections.size;
  }

  getClientsInBattle(battleId: string): number {
    const battleClients = this.battleConnections.get(battleId);
    return battleClients?.size || 0;
  }

  cleanupStaleConnections(): number {
    let cleaned = 0;
    const now = Date.now();
    
    for (const [clientId, client] of this.connections) {
      if (!client.isAlive()) {
        logger.info('Cleaning up stale connection', { clientId, playerId: client.player?.id });
        this.unregisterConnection(clientId);
        cleaned++;
      }
    }
    
    return cleaned;
  }
}

export { NetworkClient } from './NetworkClient';