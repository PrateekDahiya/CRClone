import { configService } from '../services/ConfigService';
import { logger } from '../utils/logger';

/**
 * ConfigHotReload - Integrates ConfigService with battle servers
 * In production, this would use Redis pub/sub to broadcast changes
 */
export class ConfigHotReload {
  private static instance: ConfigHotReload;
  private battleServers: Map<string, { send: (msg: any) => void }> = new Map();

  static getInstance(): ConfigHotReload {
    if (!ConfigHotReload.instance) {
      ConfigHotReload.instance = new ConfigHotReload();
    }
    return ConfigHotReload.instance;
  }

  private constructor() {
    // Listen for config changes
    configService.on('configChanged', (data) => {
      this.broadcastToBattleServers(data);
    });

    configService.on('broadcast', (data) => {
      this.broadcastToBattleServers(data);
    });
  }

  registerBattleServer(serverId: string, server: { send: (msg: any) => void }): void {
    this.battleServers.set(serverId, server);
    logger.info('Battle server registered for config updates', { serverId, total: this.battleServers.size });
  }

  unregisterBattleServer(serverId: string): void {
    this.battleServers.delete(serverId);
    logger.info('Battle server unregistered', { serverId, total: this.battleServers.size });
  }

  private broadcastToBattleServers(data: { key: string; oldValue: any; newValue: any }): void {
    const message = {
      type: 'config_update',
      key: data.key,
      value: data.newValue,
      timestamp: Date.now()
    };

    for (const [serverId, server] of this.battleServers) {
      try {
        server.send(message);
      } catch (error) {
        const msg = error instanceof Error ? error.message : String(error);
        logger.warn('Failed to send config update to battle server', { serverId, error: msg });
      }
    }

    logger.debug('Config change broadcasted', { key: data.key, servers: this.battleServers.size });
  }

  // Send full config to newly connected battle server
  async sendFullConfig(server: { send: (msg: any) => void }): Promise<void> {
    const configs = configService.getAllConfigs();
    server.send({
      type: 'config_full',
      configs,
      timestamp: Date.now()
    });
  }

  stopHotReload(): void {
    this.battleServers.clear();
  }
}

export const configHotReload = ConfigHotReload.getInstance();