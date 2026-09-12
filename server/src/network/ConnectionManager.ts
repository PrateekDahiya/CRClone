import WebSocket from 'ws';
import { v4 as uuidv4 } from 'uuid';
import { logger } from '../utils/logger';
import { NetworkClient } from './NetworkClient';

export class ConnectionManager {
  private connections: Map<string, NetworkClient> = new Map();
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
    this.connections.delete(clientId);
  }

  getConnection(clientId: string): NetworkClient | undefined {
    return this.connections.get(clientId);
  }

  getAllConnections(): NetworkClient[] {
    return Array.from(this.connections.values());
  }

  getAuthenticatedConnections(): NetworkClient[] {
    return Array.from(this.connections.values()).filter(c => c.player !== null);
  }

  broadcast(message: any, excludeClientId?: string): void {
    const data = JSON.stringify(message);
    for (const client of this.connections.values()) {
      if (client.id !== excludeClientId && client.ws.readyState === WebSocket.OPEN) {
        client.ws.send(data);
      }
    }
  }

  getConnectionCount(): number {
    return this.connections.size;
  }

  getAuthenticatedCount(): number {
    return this.getAuthenticatedConnections().length;
  }
}

export { NetworkClient } from './NetworkClient';