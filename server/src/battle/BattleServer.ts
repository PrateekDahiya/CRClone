import { logger } from '../utils/logger';
import { config } from '../config';
import { DeterministicRNG } from '../utils/rng';
import { BattleType, BattleStatus, PlayerInput, PlayerBattleInfo, EntityState, PlayerState } from '../types';
import { PlayerService } from '../services/PlayerService';

interface BattlePlayer {
  info: PlayerBattleInfo;
  connection: any; // NetworkClient
  elixir: number;
  hand: number[];
  deck: number[];
  nextCardIndex: number;
  kingTowerActivated: boolean;
}

export class BattleServer {
  public readonly battleId: string;
  private rng: DeterministicRNG;
  private player1: BattlePlayer;
  private player2: BattlePlayer;
  private status: BattleStatus = BattleStatus.Waiting;
  private tick = 0;
  private startTime: number;
  private entities: Map<number, EntityState> = new Map();
  private nextEntityId = 1000;
  private eventLog: any[] = [];
  private playerService: PlayerService;
  private gameEnded = false;

  constructor(
    battleId: string,
    p1Info: PlayerBattleInfo,
    p2Info: PlayerBattleInfo,
    seed: number,
    playerService: PlayerService
  ) {
    this.battleId = battleId;
    this.rng = new DeterministicRNG(BigInt(seed));
    this.playerService = playerService;

    this.player1 = this.createPlayer(p1Info, 1);
    this.player2 = this.createPlayer(p2Info, 2);
    this.startTime = Date.now();

    // Initialize towers
    this.initializeTowers();

    logger.info('Battle server created', { battleId, seed });
  }

  private createPlayer(info: PlayerBattleInfo, playerId: number): BattlePlayer {
    return {
      info,
      connection: null,
      elixir: config.game.startingElixir,
      hand: info.deck.slice(0, config.game.handSize),
      deck: [...info.deck],
      nextCardIndex: config.game.handSize,
      kingTowerActivated: false,
    };
  }

  private initializeTowers(): void {
    // Create tower entities for both players
    // Tower IDs: 1-3 for P1, 4-6 for P2
    // This is simplified - real implementation would use the simulation
  }

  setConnection(playerId: string, connection: any): void {
    if (this.player1.info.playerId === playerId) {
      this.player1.connection = connection;
    } else if (this.player2.info.playerId === playerId) {
      this.player2.connection = connection;
    }
  }

  handleInput(playerId: string, input: PlayerInput): void {
    if (this.status !== BattleStatus.Playing) return;

    const player = this.getPlayerById(playerId);
    if (!player) return;

    // Validate input
    if (!this.validateInput(player, input)) {
      logger.warn('Invalid input rejected', { playerId, input });
      return;
    }

    // Apply input immediately (server-authoritative)
    this.applyInput(player, input);
  }

  private validateInput(player: BattlePlayer, input: PlayerInput): boolean {
    // Check elixir cost
    // Check card ownership
    // Check position validity
    return true; // Simplified
  }

  private applyInput(player: BattlePlayer, input: PlayerInput): void {
    const opponent = player === this.player1 ? this.player2 : this.player1;

    switch (input.type) {
      case 'play_card':
        this.playCard(player, opponent, input);
        break;
      case 'cast_spell':
        this.castSpell(player, opponent, input);
        break;
      case 'champion_ability':
        this.useChampionAbility(player, opponent, input);
        break;
    }
  }

  private playCard(player: BattlePlayer, opponent: BattlePlayer, input: PlayerInput): void {
    // Find card in hand
    const handIndex = player.hand.indexOf(input.cardId!);
    if (handIndex === -1) return;

    // Deduct elixir (simplified)
    // Spawn entity
    // Draw next card
    player.hand.splice(handIndex, 1);
    player.drawCard();
  }

  private castSpell(player: BattlePlayer, opponent: BattlePlayer, input: PlayerInput): void {
    // Similar to playCard but for spells
  }

  private useChampionAbility(player: BattlePlayer, opponent: BattlePlayer, input: PlayerInput): void {
    // Champion ability logic
  }

  private getPlayerById(playerId: string): BattlePlayer | null {
    if (this.player1.info.playerId === playerId) return this.player1;
    if (this.player2.info.playerId === playerId) return this.player2;
    return null;
  }

  private drawCard(player: BattlePlayer): void {
    if (player.nextCardIndex >= player.deck.length) {
      player.nextCardIndex = 0;
    }
    player.hand[config.game.handSize - 1] = player.deck[player.nextCardIndex];
    player.nextCardIndex++;
  }

  handleDisconnect(playerId: string): void {
    if (this.gameEnded) return;

    const player = this.getPlayerById(playerId);
    if (!player) return;

    logger.info('Player disconnected during battle', { battleId: this.battleId, playerId });

    // Give grace period for reconnection
    setTimeout(() => {
      if (!player.connection && !this.gameEnded) {
        // Force forfeit
        this.endBattle(player === this.player1 ? 'player2' : 'player1', true);
      }
    }, 10000);
  }

  forceEnd(): void {
    if (this.gameEnded) return;
    this.endBattle('draw', true);
  }

  private endBattle(winner: 'player1' | 'player2' | 'draw', forfeit = false): void {
    if (this.gameEnded) return;
    this.gameEnded = true;
    this.status = winner === 'player1' ? BattleStatus.Player1Won : 
                  winner === 'player2' ? BattleStatus.Player2Won : BattleStatus.Draw;

    const duration = (Date.now() - this.startTime) / 1000;

    // Calculate results
    const result = this.calculateResult(winner, duration, forfeit);

    // Notify players
    this.sendBattleEnd(result);

    // Save to database
    this.saveBattleResult(result);

    // Cleanup
    this.cleanup();
  }

  private calculateResult(winner: string, duration: number, forfeit: boolean): any {
    // Calculate crowns, trophy changes, etc.
    return {
      battleId: this.battleId,
      result: winner,
      duration,
      forfeit,
      // ... more fields
    };
  }

  private sendBattleEnd(result: any): void {
    // Send to both players
  }

  private async saveBattleResult(result: any): Promise<void> {
    try {
      await this.playerService.saveBattleResult(result);
    } catch (error) {
      logger.error('Failed to save battle result', { error: error.message });
    }
  }

  private cleanup(): void {
    // Unregister from game server
    // this.gameServer?.unregisterBattle(this.battleId);
  }
}