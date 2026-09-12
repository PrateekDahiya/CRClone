// Test setup
beforeAll(async () => {
  // Set test environment
  process.env.NODE_ENV = 'test';
  process.env.DB_HOST = 'localhost';
  process.env.DB_PORT = '3306';
  process.env.DB_NAME = 'crclone_test';
  process.env.DB_USERNAME = 'root';
  process.env.DB_PASSWORD = 'test';
  process.env.DB_SSL = 'false';
  process.env.REDIS_HOST = 'localhost';
  process.env.REDIS_PORT = '6379';
  process.env.JWT_SECRET = 'test-secret';
  process.env.LOG_LEVEL = 'silent';
});

// Global test utilities
export function createTestConfig() {
  return {
    elixirGenerationRate: 2.8,
    doubleElixirRate: 1.4,
    tripleElixirRate: 0.93,
    startingElixir: 5,
    maxElixir: 10,
    battleDuration: 180,
    overtimeDuration: 180,
    maxDeckCards: 8,
    maxChampionsPerDeck: 1,
    handSize: 4,
    princessTowerHP: 2584,
    kingTowerHP: 4384,
    towerDamage: 152,
    towerHitSpeed: 1.2,
    towerRange: 7,
    deployZoneDepth: 4,
    deployZoneDepthExpanded: 8,
    simulationTickRate: 60,
    maxDesyncThreshold: 0.1,
    maxInputQueueSize: 10,
    maxCardLevel: 14,
    tournamentStandardLevel: 11,
    levelStatMultiplier: 1.1
  };
}

export function createMockDeck() {
  return [26000040, 26000041, 26000042, 26000043, 26000044, 26000045, 26000046, 26000047];
}

export function createMockPlayer(id: string, trophies: number = 4000) {
  return {
    id,
    username: `test_${id}`,
    trophies,
    deck: createMockDeck(),
    gold: 100000,
    gems: 1000
  };
}

// Mock database for unit tests
export class MockDatabase {
  private data: Map<string, any[]> = new Map();

  async query(sql: string, params?: any[]): Promise<any[]> {
    // Simple mock implementation
    return [];
  }

  async execute(sql: string, params?: any[]): Promise<any> {
    return { affectedRows: 1, insertId: 1 };
  }

  async beginTransaction(): Promise<void> {}
  async commit(): Promise<void> {}
  async rollback(): Promise<void> {}
  async close(): Promise<void> {}
}

// Test data cleanup
afterAll(async () => {
  // Cleanup any test resources
});