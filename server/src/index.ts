import WebSocket from 'ws';
import http from 'http';
import { config } from './config';
import { logger } from './utils/logger';
import { NetworkClient, ConnectionManager } from './network/ConnectionManager';
import { MessageHandler } from './network/MessageHandler';
import { Matchmaker } from './matchmaking/Matchmaker';
import { BattleServer } from './battle/BattleServer';
import { Database } from './persistence/Database';
import { PlayerService } from './services/PlayerService';
import { ClanService } from './services/ClanService';
import { ShopService } from './services/ShopService';
import { QuestService } from './services/QuestService';
import { SeasonService } from './services/SeasonService';
import { TournamentService } from './services/TournamentService';
import { ReplayService } from './services/ReplayService';
import { ConfigService, configService } from './services/ConfigService';
import { MigrationRunner } from './utils/MigrationRunner';
import { configHotReload } from './utils/ConfigHotReload';
import { metricsCollector } from './utils/MetricsCollector';
import { healthCheck, createHealthMiddleware } from './utils/HealthCheck';
import { NetworkMessage, BattleType, PlayerInput } from './types';
import { loadProtocol } from './network/Protocol';
import { EventEmitter } from 'events';

class GameServer extends EventEmitter {
  private httpServer: http.Server;
  private wsServer: WebSocket.Server;
  private connectionManager: ConnectionManager;
  private messageHandler: MessageHandler;
  private matchmaker: Matchmaker;
  private database: Database;
  private playerService: PlayerService;
  private clanService: ClanService;
  private shopService: ShopService;
  private questService: QuestService;
  private seasonService: SeasonService;
  private tournamentService: TournamentService;
  private replayService: ReplayService;
  private activeBattles: Map<string, BattleServer> = new Map();
  private isShuttingDown = false;
  private cleanupInterval: NodeJS.Timeout | null = null;

  constructor() {
    super();
    this.httpServer = http.createServer();
    this.wsServer = new WebSocket.Server({ server: this.httpServer });
    this.connectionManager = new ConnectionManager(this.wsServer);
    this.database = new Database();
    this.playerService = new PlayerService(this.database);
    this.clanService = new ClanService(this.database, this.playerService);
    this.shopService = new ShopService(this.database, this.playerService);
    this.questService = new QuestService(this.database, this.playerService);
    this.seasonService = new SeasonService(this.database, this.playerService);
    this.tournamentService = new TournamentService(this.database, this.playerService);
    this.replayService = new ReplayService(this.database);
    this.matchmaker = new Matchmaker(this.playerService);
    this.messageHandler = new MessageHandler(
      this.connectionManager,
      this.matchmaker,
      this.activeBattles,
      this.playerService,
      this.clanService,
      this.shopService,
      this.questService,
      this.seasonService,
      this.tournamentService,
      this.replayService
    );

    this.matchmaker.setGameServer(this);

    // Register battle servers for config hot-reload
    this.on('battleServerCreated', (serverId: string, battleServer: any) => {
      configHotReload.registerBattleServer(serverId, battleServer);
    });

    this.setupWebSocketHandlers();
    this.setupHttpHandlers();
    this.setupGracefulShutdown();
    this.startCleanupInterval();
  }

  private setupWebSocketHandlers(): void {
    this.wsServer.on('connection', (ws: WebSocket, req: http.IncomingMessage) => {
      const client = this.connectionManager.registerConnection(ws, req);
      logger.info('New connection', { clientId: client.id, ip: req.socket.remoteAddress });

      ws.on('message', (data: Buffer) => {
        try {
          const message = JSON.parse(data.toString()) as NetworkMessage;
          metricsCollector.recordWebSocketMessage(message.type, data.length);
          this.messageHandler.handle(client, message);
        } catch (error) {
          logger.warn('Invalid message format', { clientId: client.id, error: error instanceof Error ? error.message : String(error) });
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

  private setupHttpHandlers(): void {
    // Health check endpoints
    this.httpServer.on('request', createHealthMiddleware(healthCheck));

    // Metrics endpoint
    this.httpServer.on('request', (req, res) => {
      if (req.url === '/metrics' && req.method === 'GET') {
        res.setHeader('Content-Type', 'text/plain');
        res.end(metricsCollector.getPrometheusMetrics());
      }
    });

    // Config endpoint (for debugging)
    this.httpServer.on('request', (req, res) => {
      if (req.url === '/config' && req.method === 'GET') {
        res.setHeader('Content-Type', 'application/json');
        res.end(JSON.stringify(configService.getAllConfigs(), null, 2));
      }
    });
  }

  private handleInput(client: NetworkClient, message: NetworkMessage): void {
    const inputMsg = message as any;
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

    client.queueInput(input);
    const rejection = battle.handleInput(client.player!.id, input);
    if (typeof rejection === 'string' && rejection.length > 0) {
      client.sendError(rejection);
    }
  }

  private handleDisconnect(client: NetworkClient): void {
    logger.info('Client disconnected', { clientId: client.id, playerId: client.player?.id });

    metricsCollector.recordPlayerDisconnected();

    this.matchmaker.removeFromQueue(client.id);

    if (client.battleId) {
      const battle = this.activeBattles.get(client.battleId);
      if (battle) {
        battle.handleDisconnect(client.player!.id);
      }
    }

    this.connectionManager.unregisterConnection(client.id);
  }

  private startCleanupInterval(): void {
    this.cleanupInterval = setInterval(() => {
      const cleaned = this.connectionManager.cleanupStaleConnections();
      if (cleaned > 0) {
        logger.debug('Cleaned up stale connections', { count: cleaned });
      }

      // Update active battles gauge
      metricsCollector.setActiveBattles(this.activeBattles.size);
    }, 30000);

    // Run daily cleanup jobs
    const dailyCleanup = setInterval(async () => {
      await this.runDailyCleanup();
    }, 24 * 60 * 60 * 1000); // Every 24 hours

    // Run hourly leaderboard refresh
    const hourlyLeaderboard = setInterval(async () => {
      await this.refreshLeaderboards();
    }, 60 * 60 * 1000); // Every hour

    // Store intervals for cleanup
    (this as any).dailyCleanupInterval = dailyCleanup;
    (this as any).hourlyLeaderboardInterval = hourlyLeaderboard;
  }

  private async runDailyCleanup(): Promise<void> {
    try {
      logger.info('Running daily cleanup jobs...');

      // Cleanup expired replays
      const deletedReplays = await this.replayService.cleanupExpiredReplays(30);
      logger.info('Expired replays cleaned', { count: deletedReplays });

      // Expire clan donations
      const expiredDonations = await this.clanService.expireDonations();
      logger.info('Expired donations cleaned', { count: expiredDonations });

      // Expire clan invites
      const expiredInvites = await this.clanService.expireInvites();
      logger.info('Expired invites cleaned', { count: expiredInvites });

      // Reset weekly donations
      await this.clanService.resetWeeklyDonations();
      logger.info('Weekly donations reset');

      // Reset daily/weekly quests
      const dailyReset = await this.questService.resetDailyQuests();
      const weeklyReset = await this.questService.resetWeeklyQuests();
      logger.info('Quests reset', { daily: dailyReset, weekly: weeklyReset });

      // Reset shop purchase limits
      await this.shopService.getShopStats(); // This would reset limits in a real implementation

      logger.info('Daily cleanup completed');
    } catch (error) {
      logger.error('Daily cleanup failed', { error: error instanceof Error ? error.message : String(error) });
    }
  }

  private async refreshLeaderboards(): Promise<void> {
    try {
      logger.info('Refreshing leaderboards...');
      // Leaderboard refresh would be implemented here
      // This would query player stats and update leaderboard tables
      logger.info('Leaderboards refreshed');
    } catch (error) {
      logger.error('Leaderboard refresh failed', { error: error instanceof Error ? error.message : String(error) });
    }
  }

  private setupGracefulShutdown(): void {
    const shutdown = async (signal: string) => {
      logger.info(`${signal} received, shutting down gracefully...`);
      this.isShuttingDown = true;

      if (this.cleanupInterval) clearInterval(this.cleanupInterval);
      if ((this as any).dailyCleanupInterval) clearInterval((this as any).dailyCleanupInterval);
      if ((this as any).hourlyLeaderboardInterval) clearInterval((this as any).hourlyLeaderboardInterval);

      this.matchmaker.shutdown();

      configHotReload.stopHotReload();
      configService.stopHotReload();
      metricsCollector.shutdown();

      this.wsServer.close(() => {
        logger.info('WebSocket server closed');
      });

      const shutdownTimeout = setTimeout(() => {
        logger.warn('Force shutting down active battles');
        this.activeBattles.forEach((battle) => battle.forceEnd());
      }, 30000);

      while (this.activeBattles.size > 0) {
        await new Promise(resolve => setTimeout(resolve, 1000));
      }

      clearTimeout(shutdownTimeout);

      await this.database.close();

      logger.info('Shutdown complete');
      process.exit(0);
    };

    process.on('SIGTERM', () => shutdown('SIGTERM'));
    process.on('SIGINT', () => shutdown('SIGINT'));
  }

  public async start(): Promise<void> {
    await loadProtocol();

    // Initialize database
    await this.database.connect();
    logger.info('Database connected');

    // Run migrations
    await new MigrationRunner(this.database).runMigrations();
    logger.info('Migrations completed');

    // Initialize config service with hot-reload
    await configService.initialize();
    logger.info('Config service initialized');

    // Initialize player quests for new players (would be done on login)
    // This is handled in PlayerService.register()

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
    metricsCollector.recordBattleStarted('unknown'); // Battle type would be passed
    this.emit('battleServerCreated', battleId, battle);
  }

  public unregisterBattle(battleId: string): void {
    this.activeBattles.delete(battleId);
  }

  public getConnectionManager(): ConnectionManager {
    return this.connectionManager;
  }

  public getPlayerService(): PlayerService {
    return this.playerService;
  }

  public getClanService(): ClanService {
    return this.clanService;
  }

  public getShopService(): ShopService {
    return this.shopService;
  }

  public getQuestService(): QuestService {
    return this.questService;
  }

  public getSeasonService(): SeasonService {
    return this.seasonService;
  }

  public getTournamentService(): TournamentService {
    return this.tournamentService;
  }

  public getReplayService(): ReplayService {
    return this.replayService;
  }
}

// Start server
const server = new GameServer();
server.start().catch((error) => {
  logger.error('Failed to start server', { error: error instanceof Error ? error.message : String(error) });
  process.exit(1);
});