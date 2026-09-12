// Deterministic RNG for server-side battle simulation
// Uses Xorshift64* algorithm for fast, high-quality random numbers

export class DeterministicRNG {
  private state: bigint;

  constructor(seed: bigint) {
    this.state = seed;
  }

  // Generate next 64-bit random number
  nextU64(): bigint {
    this.state ^= this.state >> 12n;
    this.state ^= this.state << 25n;
    this.state ^= this.state >> 27n;
    return this.state * 0x2545F4914F6CDD1Dn;
  }

  // Generate 32-bit unsigned integer
  nextUInt(): number {
    return Number(this.nextU64() & 0xFFFFFFFFn);
  }

  // Generate float in [0, 1)
  nextFloat(): number {
    return this.nextUInt() * (1 / 0xFFFFFFFF);
  }

  // Generate integer in [min, max)
  nextInt(min: number, max: number): number {
    return min + Math.floor(this.nextFloat() * (max - min));
  }

  // Generate boolean with probability
  nextBool(probability: number): boolean {
    return this.nextFloat() < probability;
  }

  // Generate float in [min, max)
  nextFloatRange(min: number, max: number): number {
    return min + this.nextFloat() * (max - min);
  }

  // Shuffle array in place (Fisher-Yates)
  shuffle<T>(array: T[]): T[] {
    for (let i = array.length - 1; i > 0; i--) {
      const j = this.nextInt(0, i + 1);
      [array[i], array[j]] = [array[j], array[i]];
    }
    return array;
  }

  // Sample n unique elements from array
  sample<T>(array: T[], count: number): T[] {
    const shuffled = [...array];
    this.shuffle(shuffled);
    return shuffled.slice(0, Math.min(count, array.length));
  }

  // Get current state for serialization
  getState(): bigint {
    return this.state;
  }

  // Set state (for reconciliation)
  setState(state: bigint): void {
    this.state = state;
  }
}

// Generate a deterministic seed from battle parameters
export function generateBattleSeed(
  battleId: string,
  player1Id: string,
  player2Id: string,
  timestamp: number
): bigint {
  // Simple hash combining all parameters
  let hash = 0x9E3779B97F4A7C15n; // Golden ratio
  
  for (const str of [battleId, player1Id, player2Id, timestamp.toString()]) {
    for (let i = 0; i < str.length; i++) {
      hash ^= BigInt(str.charCodeAt(i));
      hash *= 0x100000001B3n;
    }
  }
  
  // Ensure non-zero
  return hash === 0n ? 1n : hash;
}