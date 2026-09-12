import { config } from '../config';
import { BattleType, BattleResult } from '../types';

export class RatingSystem {
  private readonly BASE_TROPHY_CHANGE = 30;
  private readonly THREE_CROWN_BONUS = 10;
  private readonly K_FACTOR = 32;

  calculateTrophyChange(
    winnerTrophies: number,
    loserTrophies: number,
    winnerCrowns: number,
    loserCrowns: number
  ): { player1Change: number; player2Change: number } {
    const diff = winnerTrophies - loserTrophies;
    
    let baseChange = this.BASE_TROPHY_CHANGE;
    
    if (diff > 0) {
      baseChange = Math.max(10, this.BASE_TROPHY_CHANGE - Math.floor(diff / 100));
    } else if (diff < 0) {
      baseChange = Math.min(50, this.BASE_TROPHY_CHANGE + Math.floor(Math.abs(diff) / 100));
    }

    const crownDiff = winnerCrowns - loserCrowns;
    let crownMultiplier = 1.0;
    
    if (crownDiff >= 3) {
      crownMultiplier = 1.5;
    } else if (crownDiff === 2) {
      crownMultiplier = 1.2;
    }

    const winnerChange = Math.round(baseChange * crownMultiplier);
    const loserChange = -Math.round(baseChange * 0.8);

    if (winnerCrowns === 3 && loserCrowns === 0) {
      return { 
        player1Change: winnerChange + this.THREE_CROWN_BONUS, 
        player2Change: loserChange - this.THREE_CROWN_BONUS 
      };
    }

    return { player1Change: winnerChange, player2Change: loserChange };
  }

  calculateELOChange(ratingA: number, ratingB: number, scoreA: number): number {
    const expectedA = 1 / (1 + Math.pow(10, (ratingB - ratingA) / 400));
    return Math.round(this.K_FACTOR * (scoreA - expectedA));
  }

  getMaxTrophyDiff(battleType: BattleType, avgTrophies: number): number {
    const baseDiff = config.matchmaking.maxTrophyDiff;
    
    if (battleType === BattleType.Tournament || battleType === BattleType.Challenge) {
      return baseDiff;
    }
    
    if (battleType === BattleType.TwoVTwo) {
      return baseDiff * 1.5;
    }

    if (avgTrophies > 5000) return config.matchmaking.maxTrophyDiffHigh;
    if (avgTrophies > 4000) return baseDiff * 2;
    if (avgTrophies > 3000) return baseDiff * 1.5;
    
    return baseDiff;
  }

  calculateTrophyChangeFromResult(result: BattleResult, player1Trophies: number, player2Trophies: number): {
    player1Change: number;
    player2Change: number;
  } {
    if (result.winner === 'draw') {
      return { player1Change: 0, player2Change: 0 };
    }

    if (result.winner === 'player1') {
      return this.calculateTrophyChange(
        player1Trophies,
        player2Trophies,
        result.player1Crowns,
        result.player2Crowns
      );
    } else {
      const changes = this.calculateTrophyChange(
        player2Trophies,
        player1Trophies,
        result.player2Crowns,
        result.player1Crowns
      );
      return { player1Change: changes.player2Change, player2Change: changes.player1Change };
    }
  }

  getLeagueFromTrophies(trophies: number): string {
    if (trophies >= 7000) return 'Legend League';
    if (trophies >= 6000) return 'Champion League';
    if (trophies >= 5000) return 'Master League';
    if (trophies >= 4000) return 'Diamond League';
    if (trophies >= 3000) return 'Platinum League';
    if (trophies >= 2000) return 'Gold League';
    if (trophies >= 1000) return 'Silver League';
    return 'Bronze League';
  }

  getSeasonReward(trophies: number): { gold: number; gems: number; chests: string[] } {
    if (trophies >= 7000) return { gold: 50000, gems: 500, chests: ['legendary', 'epic', 'epic', 'rare', 'rare', 'common'] };
    if (trophies >= 6000) return { gold: 30000, gems: 300, chests: ['epic', 'epic', 'rare', 'rare', 'common'] };
    if (trophies >= 5000) return { gold: 20000, gems: 200, chests: ['epic', 'rare', 'rare', 'common'] };
    if (trophies >= 4000) return { gold: 15000, gems: 100, chests: ['rare', 'rare', 'common'] };
    if (trophies >= 3000) return { gold: 10000, gems: 50, chests: ['rare', 'common'] };
    if (trophies >= 2000) return { gold: 5000, gems: 25, chests: ['common'] };
    if (trophies >= 1000) return { gold: 2000, gems: 10, chests: [] };
    return { gold: 500, gems: 0, chests: [] };
  }
}

export const ratingSystem = new RatingSystem();