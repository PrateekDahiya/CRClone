import WebSocket from 'ws';
import http from 'http';
import { config } from './config';
import { logger } from './utils/logger';
import { NetworkClient, ConnectionManager } from './network/ConnectionManager';
import { Matchmaker } from './matchmaking/Matchmaker';
import { BattleServer } from './battle/BattleServer';
import { Database } from './persistence/Database';
import { PlayerService } from './services/PlayerService';
import { NetworkMessage, BattleType, PlayerInput, GameStateMessage } from './types';

class GameServer {
  private httpServer: http.Server;
  private wsServer: WebSocket.Server;
  private connectionManager: ConnectionManager;
  private matchmaker: Matchmaker;
  private database: Database;
  private playerService: PlayerService;
  private activeBattles: Map<string, BattleServer> = new Map();
  private isShuttingDown = false;

  constructor() {
    this.httpServer = http.createServer();
    this.wsServer = new WebSocket.Server({ server: this.httpServer });
    this.connectionManager = new ConnectionManager(this.wsServer);
    this.database = new Database();
    this.playerService = new PlayerService(this.database);
    this.matchmaker = new Matchmaker(this.playerService);

    this.setupWebSocketHandlers();
    this.setupGracefulShutdown();
  }

  private setupWebSocketHandlers(): void {
    this.wsServer.on('connection', (ws: WebSocket, req: http.IncomingMessage) => {
      const client = this.connectionManager.registerConnection(ws, req);
      logger.info('New connection', { clientId: client.id, ip: req.socket.remoteAddress });

      ws.on('message', (data: Buffer) => {
        try {
          const message = JSON.parse(data.toString()) as NetworkMessage;
          this.handleMessage(client, message);
        } catch (error) {
          logger.warn('Invalid message format', { clientId: client.id, error: error.message });
          client.sendError('INVALID_MESSAGE_FORMAT');
        }
      });

      ws.on('close', () => {
        this.handleDisconnect(client);
      });

      ws.on('error', (error) => {
        logger.error('WebSocket error', { clientId: client.id, error: error.message });
      });
    });
  }

  private handleMessage(client: NetworkClient, message: NetworkMessage): void {
    switch (message.type) {
      case 'auth':
        this.handleAuth(client, message);
        break;
      case 'matchmaking':
        this.handleMatchmaking(client, message);
        break;
      case 'input':
        this.handleInput(client, message);
        break;
      case 'save_deck':
        this.handleSaveDeck(client, message);
        break;
      case 'heartbeat':
        client.lastHeartbeat = Date.now();
        break;
      default:
        logger.warn('Unknown message type', { type: message.type, clientId: client.id });
    }
  }

  private async handleAuth(client: NetworkClient, message: NetworkMessage): Promise<void> {
    const authMsg = message as any; // AuthMessage
    try {
      const player = await this.playerService.authenticate(authMsg.token);
      if (player) {
        client.authenticate(player);
        client.send({ type: 'auth_response', success: true, playerId: player.id });
        logger.info('Player authenticated', { playerId: player.id, username: player.username });
      } else {
        client.send({ type: 'auth_response', success: false, error: 'INVALID_TOKEN' });
      }
    } catch (error) {
      logger.error('Auth error', { error: error.message });
      client.send({ type: 'auth_response', success: false, error: 'AUTH_FAILED' });
    }
  }

  private handleMatchmaking(client: NetworkClient, message: NetworkMessage): void {
    const mmMsg = message as any; // MatchmakingRequest
    if (!client.player) {
      client.sendError('NOT_AUTHENTICATED');
      return;
    }

    const battleType = mmMsg.battleType || BattleType.Ladder;
    this.matchmaker.addToQueue({
      playerId: client.player.id,
      username: client.player.username,
      trophies: client.player.trophies,
      deck: client.player.activeDeck?.cardIds || [],
      battleType,
      joinedAt: Date.now(),
    });

    client.send({ type: 'matchmaking_started', battleType });
  }

  private handleInput(client: NetworkClient, message: NetworkMessage): void {
    const inputMsg = message as any; // InputMessage
    if (!client.battleId) return;

    const battle = this.activeBattles.get(client.battleId);
    if (!battle) return;

    const input: PlayerInput = {
      type: inputMsg.input.type,
      cardId: inputMsg.input.cardId,
      spellId: inputMsg.input.spellId,
      position: inputMsg.input.position,
      targetPosition: inputMsg.input.targetPosition,
      clientTick: inputMsg.tick,
    };

    battle.handleInput(client.player!.id, input);
  }

  private async handleSaveDeck(client: NetworkClient, message: NetworkMessage): Promise<void> {
    const saveMsg = message as any; // SaveDeckRequest
    if (!client.player) return;

    await this.playerService.saveDeck(client.player.id, saveMsg.cardIds);
    client.send({ type: 'deck_saved', success: true });
  }

  private handleDisconnect(client: NetworkClient): void {
    logger.info('Client disconnected', { clientId: client.id, playerId: client.player?.id });

    // Remove from matchmaking queue
    this.matchmaker.removeFromQueue(client.id);

    // Handle battle disconnect
    if (client.battleId) {
      const battle = this.activeBattles.get(client.battleId);
      if (battle) {
        battle.handleDisconnect(client.player!.id);
      }
    }

    this.connectionManager.unregisterConnection(client.id);
  }

  private setupGracefulShutdown(): void {
    const shutdown = async (signal: string) => {
      logger.info(`${signal} received, shutting down gracefully...`);
      this.isShuttingDown = true;

      // Stop accepting new connections
      this.wsServer.close(() => {
        logger.info('WebSocket server closed');
      });

      // Give active battles time to finish (max 30 seconds)
      const shutdownTimeout = setTimeout(() => {
        logger.warn('Force shutting down active battles');
        this.activeBattles.forEach((battle) => battle.forceEnd());
      }, 30000);

      // Wait for battles to finish naturally
      while (this.activeBattles.size > 0) {
        await new Promise(resolve => setTimeout(resolve, 1000));
      }

      clearTimeout(shutdownTimeout);

      // Close database
      await this.database.close();

      logger.info('Shutdown complete');
      process.exit(0);
    };

    process.on('SIGTERM', () => shutdown('SIGTERM'));
    process.on('SIGINT', () => shutdown('SIGINT'));
  }

  public async start(): Promise<void> {
    // Initialize database
    await this.database.connect();
    logger.info('Database connected');

    // Start HTTP server
    this.httpServer.listen(config.port, () => {
      logger.info(`HTTP server listening on port ${config.port}`);
    });

    // Start WebSocket server
    this.wsServer.on('listening', () => {
      logger.info(`WebSocket server listening on port ${config.wsPort}`);
    });

    logger.info('Game server started');
  }

  public registerBattle(battleId: string, battle: BattleServer): void {
    this.activeBattles.set(battleId, battle);
  }

  public unregisterBattle(battleId: string): void {
    this.activeBattles.delete(battleId);
  }
}

// Start server
const server = new GameServer();
server.start().catch((error) => {
  logger.error('Failed to start server', { error: error.message });
  process.exit(1);
});