// Server configuration
import dotenv from 'dotenv';
dotenv.config();

export const config = {
  // Server
  port: parseInt(process.env.PORT || '3000'),
  wsPort: parseInt(process.env.WS_PORT || '3001'),
  env: process.env.NODE_ENV || 'development',

  // Database (MySQL - Aiven)
  database: {
    host: process.env.DB_HOST!,
    port: parseInt(process.env.DB_PORT!),
    user: process.env.DB_USERNAME!,
    password: process.env.DB_PASSWORD!,
    database: process.env.DB_NAME!,
    ssl: process.env.DB_SSL === 'true',
    connectionLimit: 10,
  },

  // Redis
  redis: {
    host: process.env.REDIS_HOST || 'localhost',
    port: parseInt(process.env.REDIS_PORT || '6379'),
    password: process.env.REDIS_PASSWORD,
    db: 0,
  },

  // JWT
  jwt: {
    secret: process.env.JWT_SECRET!,
    expiresIn: '7d',
    refreshExpiresIn: '30d',
  },

  // Game
  game: {
    tickRate: 60,
    battleDuration: 180, // 3 minutes
    overtimeDuration: 180,
    startingElixir: 5,
    maxElixir: 10,
    elixirGenerationRate: 2.8, // seconds per elixir
    doubleElixirRate: 1.4,
    tripleElixirRate: 0.93,
    maxDeckCards: 8,
    maxChampionsPerDeck: 1,
    handSize: 4,
  },

  // Matchmaking
  matchmaking: {
    maxTrophyDiff: 300,
    maxTrophyDiffHigh: 1000, // For high trophy players
    queueTimeout: 30000, // 30 seconds
    expandRangeInterval: 5000, // Expand search every 5 seconds
  },

  // Battle
  battle: {
    maxConcurrentBattlesPerServer: 100,
    reconciliationThreshold: 0.1, // Desync threshold
    maxInputQueueSize: 10,
    replayRetentionDays: 30,
  },

  // Logging
  logging: {
    level: process.env.LOG_LEVEL || 'info',
    file: process.env.LOG_FILE || 'logs/server.log',
  },
};

export default config;