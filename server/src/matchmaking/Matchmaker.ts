import { logger } from '../utils/logger';
import { config } from '../config';
import { MatchmakingQueueEntry, BattleType } from '../types';
import { BattleServer } from '../battle/BattleServer';
import { generateBattleSeed } from '../utils/rng';
import { GameServer } from '../index';

export class Matchmaker {
  private queues: Map<BattleType, MatchmakingQueueEntry[]> = new Map();
  private gameServer: GameServer;

  constructor(private playerService: any) {
    // Initialize queues for each battle type
    Object.values(BattleType).forEach(type => {
      this.queues.set(type, []);
    });
  }

  setGameServer(server: any): void {
    this.gameServer = server;
  }

  addToQueue(entry: MatchmakingQueueEntry): void {
    const queue = this.queues.get(entry.battleType) || [];
    queue.push(entry);
    this.queues.set(entry.battleType, queue);
    
    logger.info('Player added to matchmaking queue', { 
      playerId: entry.playerId, 
      battleType: entry.battleType,
      queueSize: queue.length 
    });

    this.tryMatch(queue, entry.battleType);
  }

  removeFromQueue(clientId: string): void {
    for (const [battleType, queue] of this.queues.entries()) {
      const index = queue.findIndex(e => e.playerId === clientId);
      if (index !== -1) {
        queue.splice(index, 1);
        logger.info('Player removed from matchmaking queue', { 
          playerId: clientId, 
          battleType,
          queueSize: queue.length 
        });
      }
    }
  }

  private tryMatch(queue: MatchmakingQueueEntry[], battleType: BattleType): void {
    while (queue.length >= 2) {
      const p1 = queue[0];
      const p2 = queue[1];

      // Check trophy difference
      const diff = Math.abs(p1.trophies - p2.trophies);
      const maxDiff = this.getMaxTrophyDiff(battleType, Math.max(p1.trophies, p2.trophies));

      if (diff <= maxDiff) {
        // Match found!
        queue.shift();
        queue.shift();
        this.createBattle(p1, p2, battleType);
      } else {
        // Can't match these two, wait for more players or expand range
        break;
      }
    }
  }

  private getMaxTrophyDiff(battleType: BattleType, avgTrophies: number): number {
    const baseDiff = config.matchmaking.maxTrophyDiff;
    
    // Higher trophies = wider search
    if (avgTrophies > 5000) return config.matchmaking.maxTrophyDiffHigh;
    if (avgTrophies > 4000) return baseDiff * 2;
    if (avgTrophies > 3000) return baseDiff * 1.5;
    
    return baseDiff;
  }

  private async createBattle(p1: MatchmakingQueueEntry, p2: MatchmakingQueueEntry, battleType: BattleType): Promise<void> {
    const battleId = `battle_${Date.now()}_${Math.random().toString(36).substr(2, 9)}`;
    const seed = generateBattleSeed(battleId, p1.playerId, p2.playerId, Date.now());

    // Create battle server
    const battle = new BattleServer(battleId, p1, p2, seed, this.playerService);
    
    // Register with game server
    if (this.gameServer) {
      this.gameServer.registerBattle(battleId, battle);
    }

    // Notify both players
    this.sendBattleFound(p1, p2, battleId, seed);
    this.sendBattleFound(p2, p1, battleId, seed);

    logger.info('Battle created', { battleId, player1: p1.playerId, player2: p2.playerId, battleType });
  }

  private sendBattleFound(player: MatchmakingQueueEntry, opponent: MatchmakingQueueEntry, battleId: string, seed: number): void {
    // Get player's ws connection and send battle_found message
    // This would be called through the connection manager
    logger.debug('Sending battle_found', { 
      toPlayer: player.playerId, 
      opponent: opponent.username,
      battleId 
    });
  }
}