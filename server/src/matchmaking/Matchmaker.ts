import { logger } from '../utils/logger';
import { config } from '../config';
import { MatchmakingQueueEntry, BattleType, PlayerBattleInfo } from '../types';
import { BattleServer } from '../battle/BattleServer';
import { generateBattleSeed } from '../utils/rng';
import { PriorityQueue } from './Queue';
import { RatingSystem } from './RatingSystem';

interface GameServerLike {
  registerBattle(battleId: string, battle: BattleServer): void;
  getConnectionManager(): any;
}

interface QueuedPlayer {
  entry: MatchmakingQueueEntry;
  expandedRange: number;
  queueStartTime: number;
}

interface TeamQueueEntry {
  players: MatchmakingQueueEntry[];
  combinedTrophies: number;
  queueStartTime: number;
}

export class Matchmaker {
  private queues: Map<BattleType, PriorityQueue<QueuedPlayer>> = new Map();
  private teamQueues: Map<BattleType, TeamQueueEntry[]> = new Map();
  private gameServer: GameServerLike | null = null;
  private ratingSystem: RatingSystem;
  private rangeExpansionIntervals: Map<BattleType, NodeJS.Timeout> = new Map();

  constructor(private playerService: any) {
    this.ratingSystem = new RatingSystem();
    
    Object.values(BattleType).forEach(type => {
      this.queues.set(type, new PriorityQueue<QueuedPlayer>());
      this.teamQueues.set(type, []);
    });

    this.startRangeExpansion();
  }

  setGameServer(server: any): void {
    this.gameServer = server;
  }

  addToQueue(entry: MatchmakingQueueEntry): void {
    const battleType = entry.battleType;
    
    if (battleType === BattleType.TwoVTwo) {
      this.addToTeamQueue(entry);
    } else {
      this.addToSoloQueue(entry);
    }

    this.tryMatch(battleType);
  }

  private addToSoloQueue(entry: MatchmakingQueueEntry): void {
    const queue = this.queues.get(entry.battleType)!;
    const maxDiff = this.ratingSystem.getMaxTrophyDiff(entry.battleType, entry.trophies);
    
    const queuedPlayer: QueuedPlayer = {
      entry,
      expandedRange: maxDiff,
      queueStartTime: Date.now(),
    };

    queue.enqueue(queuedPlayer, -entry.trophies);
    
    logger.info('Player added to matchmaking queue', { 
      playerId: entry.playerId, 
      battleType: entry.battleType,
      trophies: entry.trophies,
      queueSize: queue.size 
    });
  }

  private addToTeamQueue(entry: MatchmakingQueueEntry): void {
    const teamQueue = this.teamQueues.get(entry.battleType)!;
    
    let teamEntry = teamQueue.find(t => t.players.length < 4);
    
    if (!teamEntry) {
      teamEntry = {
        players: [],
        combinedTrophies: 0,
        queueStartTime: Date.now(),
      };
      teamQueue.push(teamEntry);
    }

    teamEntry.players.push(entry);
    teamEntry.combinedTrophies = teamEntry.players.reduce((sum, p) => sum + p.trophies, 0);
    
    logger.info('Player added to 2v2 queue', { 
      playerId: entry.playerId, 
      teamSize: teamEntry.players.length,
      combinedTrophies: teamEntry.combinedTrophies 
    });
  }

  removeFromQueue(playerId: string): void {
    for (const [battleType, queue] of this.queues.entries()) {
      const removed = queue.remove(q => q.entry.playerId === playerId);
      if (removed) {
        logger.info('Player removed from solo matchmaking queue', { 
          playerId, 
          battleType,
          queueSize: queue.size 
        });
      }
    }

    for (const [battleType, teamQueue] of this.teamQueues.entries()) {
      for (let i = teamQueue.length - 1; i >= 0; i--) {
        const team = teamQueue[i];
        const playerIndex = team.players.findIndex(p => p.playerId === playerId);
        if (playerIndex !== -1) {
          team.players.splice(playerIndex, 1);
          team.combinedTrophies = team.players.reduce((sum, p) => sum + p.trophies, 0);
          
          if (team.players.length === 0) {
            teamQueue.splice(i, 1);
          }
          
          logger.info('Player removed from 2v2 queue', { 
            playerId, 
            battleType,
            remainingTeamSize: team.players.length 
          });
        }
      }
    }
  }

  private tryMatch(battleType: BattleType): void {
    if (battleType === BattleType.TwoVTwo) {
      this.tryMatch2v2(battleType);
    } else {
      this.tryMatchSolo(battleType);
    }
  }

  private tryMatchSolo(battleType: BattleType): void {
    const queue = this.queues.get(battleType)!;
    
    while (queue.size >= 2) {
      const p1 = queue.peek();
      const p2 = queue.peekAt(1);
      
      if (!p1 || !p2) break;

      const diff = Math.abs(p1.entry.trophies - p2.entry.trophies);
      const maxDiff = Math.max(p1.expandedRange, p2.expandedRange);

      if (diff <= maxDiff) {
        queue.dequeue();
        queue.dequeue();
        this.createBattle(p1.entry, p2.entry, battleType);
      } else {
        break;
      }
    }
  }

  private tryMatch2v2(battleType: BattleType): void {
    const teamQueue = this.teamQueues.get(battleType)!;
    
    for (let i = 0; i < teamQueue.length - 1; i++) {
      const team1 = teamQueue[i];
      if (team1.players.length !== 4) continue;

      for (let j = i + 1; j < teamQueue.length; j++) {
        const team2 = teamQueue[j];
        if (team2.players.length !== 4) continue;

        const avgTrophies1 = team1.combinedTrophies / 4;
        const avgTrophies2 = team2.combinedTrophies / 4;
        const diff = Math.abs(avgTrophies1 - avgTrophies2);
        const maxDiff = this.ratingSystem.getMaxTrophyDiff(battleType, Math.max(avgTrophies1, avgTrophies2)) * 1.5;

        if (diff <= maxDiff) {
          teamQueue.splice(j, 1);
          teamQueue.splice(i, 1);
          
          const p1 = team1.players[0];
          const p2 = team2.players[0];
          
          this.createBattle2v2(team1.players, team2.players, battleType);
          return;
        }
      }
    }
  }

  private toPlayerBattleInfo(entry: MatchmakingQueueEntry): PlayerBattleInfo {
    return {
      playerId: entry.playerId,
      username: entry.username,
      trophies: entry.trophies,
      deck: entry.deck,
      kingTowerLevel: 1,
      princessTowerLevel: 1,
    };
  }

  private async createBattle(p1: MatchmakingQueueEntry, p2: MatchmakingQueueEntry, battleType: BattleType): Promise<void> {
    const battleId = `battle_${Date.now()}_${Math.random().toString(36).substr(2, 9)}`;
    const seed = generateBattleSeed(battleId, p1.playerId, p2.playerId, Date.now());

    const player1 = this.toPlayerBattleInfo(p1);
    const player2 = this.toPlayerBattleInfo(p2);

    const battle = new BattleServer(battleId, player1, player2, Number(seed), this.playerService);
    
    if (this.gameServer) {
      this.gameServer.registerBattle(battleId, battle);
    }

    this.sendBattleFound(player1, player2, battleId, Number(seed));
    this.sendBattleFound(player2, player1, battleId, Number(seed));

    logger.info('Battle created', { battleId, player1: p1.playerId, player2: p2.playerId, battleType });
  }

  private async createBattle2v2(
    team1: MatchmakingQueueEntry[], 
    team2: MatchmakingQueueEntry[], 
    battleType: BattleType
  ): Promise<void> {
    const battleId = `battle_2v2_${Date.now()}_${Math.random().toString(36).substr(2, 9)}`;
    
    const p1 = team1[0];
    const p2 = team2[0];
    const seed = generateBattleSeed(battleId, p1.playerId, p2.playerId, Date.now());

    const player1 = this.toPlayerBattleInfo(p1);
    const player2 = this.toPlayerBattleInfo(p2);

    const battle = new BattleServer(battleId, player1, player2, Number(seed), this.playerService);
    battle.setTeamData(team1, team2);
    
    if (this.gameServer) {
      this.gameServer.registerBattle(battleId, battle);
    }

    for (const player of team1) {
      this.sendBattleFound(this.toPlayerBattleInfo(player), player2, battleId, Number(seed), true);
    }
    for (const player of team2) {
      this.sendBattleFound(this.toPlayerBattleInfo(player), player1, battleId, Number(seed), true);
    }

    logger.info('2v2 Battle created', { 
      battleId, 
      team1: team1.map(p => p.playerId), 
      team2: team2.map(p => p.playerId) 
    });
  }

  private sendBattleFound(
    player: PlayerBattleInfo, 
    opponent: PlayerBattleInfo, 
    battleId: string, 
    seed: number,
    is2v2 = false
  ): void {
    if (this.gameServer) {
      const connectionManager = this.gameServer.getConnectionManager?.();
      if (connectionManager) {
        const connection = connectionManager.getConnection(player.playerId);
        if (connection) {
          connection.send({
            type: 'battle_found',
            battleId,
            seed,
            player1: {
              playerId: player.playerId,
              username: player.username,
              trophies: player.trophies,
              deck: player.deck,
              kingTowerLevel: player.kingTowerLevel,
              princessTowerLevel: player.princessTowerLevel,
            },
            player2: {
              playerId: opponent.playerId,
              username: opponent.username,
              trophies: opponent.trophies,
              deck: opponent.deck,
              kingTowerLevel: opponent.kingTowerLevel,
              princessTowerLevel: opponent.princessTowerLevel,
            },
            is2v2,
          });
        }
      }
    }
  }

  private startRangeExpansion(): void {
    for (const battleType of Object.values(BattleType)) {
      const interval = setInterval(() => {
        this.expandSearchRange(battleType);
      }, config.matchmaking.expandRangeInterval);
      
      this.rangeExpansionIntervals.set(battleType, interval);
    }
  }

  private expandSearchRange(battleType: BattleType): void {
    const queue = this.queues.get(battleType);
    if (!queue || queue.isEmpty()) return;

    const items = queue.toArray();
    let expanded = false;
    
    for (const queuedPlayer of items) {
      const waitTime = Date.now() - queuedPlayer.queueStartTime;
      if (waitTime > config.matchmaking.queueTimeout) {
        const expansion = Math.floor(waitTime / config.matchmaking.queueTimeout) * 100;
        queuedPlayer.expandedRange += expansion;
        expanded = true;
      }
    }

    if (expanded) {
      logger.debug('Expanded search range', { battleType, queueSize: queue.size });
      this.tryMatch(battleType);
    }

    this.checkQueueTimeouts(battleType);
  }

  private checkQueueTimeouts(battleType: BattleType): void {
    const queue = this.queues.get(battleType);
    if (!queue) return;

    const items = queue.toArray();
    for (const queuedPlayer of items) {
      const waitTime = Date.now() - queuedPlayer.queueStartTime;
      if (waitTime > config.matchmaking.queueTimeout * 2) {
        queue.remove(q => q.entry.playerId === queuedPlayer.entry.playerId);
        this.createBotMatch(queuedPlayer.entry, battleType);
      }
    }
  }

  private async createBotMatch(entry: MatchmakingQueueEntry, battleType: BattleType): Promise<void> {
    const battleId = `battle_bot_${Date.now()}_${Math.random().toString(36).substr(2, 9)}`;
    const seed = generateBattleSeed(battleId, entry.playerId, 'bot', Date.now());

    const botEntry: PlayerBattleInfo = {
      playerId: 'bot',
      username: 'Practice Bot',
      trophies: entry.trophies,
      deck: this.getBotDeck(entry.trophies),
      kingTowerLevel: 1,
      princessTowerLevel: 1,
    };

    const playerEntry = this.toPlayerBattleInfo(entry);

    const battle = new BattleServer(battleId, playerEntry, botEntry, Number(seed), this.playerService);
    battle.setBotMode(true);
    
    if (this.gameServer) {
      this.gameServer.registerBattle(battleId, battle);
    }

    this.sendBattleFound(playerEntry, botEntry, battleId, Number(seed));

    logger.info('Bot match created', { battleId, playerId: entry.playerId, battleType });
  }

  private getBotDeck(playerTrophies: number): number[] {
    const starterDeck = [26000040, 26000041, 26000042, 26000043, 26000044, 26000045, 26000046, 26000047];
    return starterDeck;
  }

  getQueueSize(battleType: BattleType): number {
    const queue = this.queues.get(battleType);
    return queue?.size || 0;
  }

  getTeamQueueSizes(battleType: BattleType): { teamCount: number; totalPlayers: number } {
    const teamQueue = this.teamQueues.get(battleType) || [];
    const totalPlayers = teamQueue.reduce((sum, t) => sum + t.players.length, 0);
    return { teamCount: teamQueue.length, totalPlayers };
  }

  shutdown(): void {
    for (const interval of this.rangeExpansionIntervals.values()) {
      clearInterval(interval);
    }
    this.rangeExpansionIntervals.clear();
  }
}