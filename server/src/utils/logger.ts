import winston from 'winston';
import { AsyncLocalStorage } from 'async_hooks';
import { v4 as uuidv4 } from 'uuid';
import config from '../config';

interface LogContext {
  correlationId: string;
  playerId?: string;
  battleId?: string;
  requestId?: string;
}

const contextStorage = new AsyncLocalStorage<LogContext>();

export function getCorrelationId(): string {
  return contextStorage.getStore()?.correlationId ?? 'no-correlation';
}

export function runWithCorrelation<T>(fn: () => T, ctx?: Partial<LogContext>): T {
  const parent = contextStorage.getStore();
  const context: LogContext = {
    correlationId: ctx?.correlationId ?? parent?.correlationId ?? uuidv4(),
    playerId: ctx?.playerId ?? parent?.playerId,
    battleId: ctx?.battleId ?? parent?.battleId,
    requestId: ctx?.requestId ?? parent?.requestId
  };
  return contextStorage.run(context, fn);
}

export async function runWithCorrelationAsync<T>(fn: () => Promise<T>, ctx?: Partial<LogContext>): Promise<T> {
  const parent = contextStorage.getStore();
  const context: LogContext = {
    correlationId: ctx?.correlationId ?? parent?.correlationId ?? uuidv4(),
    playerId: ctx?.playerId ?? parent?.playerId,
    battleId: ctx?.battleId ?? parent?.battleId,
    requestId: ctx?.requestId ?? parent?.requestId
  };
  return contextStorage.run(context, fn);
}

const correlationFormat = winston.format((info: any) => {
  const store = contextStorage.getStore();
  info.correlationId = (info as any).correlationId ?? store?.correlationId ?? 'no-correlation';
  if (store?.playerId && !(info as any).playerId) (info as any).playerId = store.playerId;
  if (store?.battleId && !(info as any).battleId) (info as any).battleId = store.battleId;
  if (store?.requestId && !(info as any).requestId) (info as any).requestId = store.requestId;
  return info;
});

const logFormat = winston.format.combine(
  correlationFormat(),
  winston.format.timestamp({ format: 'YYYY-MM-DD HH:mm:ss' }),
  winston.format.errors({ stack: true }),
  winston.format.json()
);

const consoleFormat = winston.format.combine(
  correlationFormat(),
  winston.format.colorize(),
  winston.format.timestamp({ format: 'HH:mm:ss' }),
  winston.format.printf(({ timestamp, level, message, correlationId, ...meta }: any) => {
    let metaStr = '';
    const cleanMeta = { ...meta };
    // Remove winston internals from display
    delete (cleanMeta as any)[Symbol.for('level')];
    delete (cleanMeta as any)[Symbol.for('message')];
    if (Object.keys(cleanMeta).length > 0) {
      try { metaStr = ` ${JSON.stringify(cleanMeta)}`; } catch { metaStr = ''; }
    }
    return `${timestamp} ${level}: [${correlationId}] ${message}${metaStr}`;
  })
);

function buildTransports(): winston.transport[] {
  const transports: winston.transport[] = [
    new winston.transports.Console({ format: consoleFormat })
  ];
  try {
    const logFile = (config as any)?.logging?.file ?? 'logs/app.log';
    transports.push(new winston.transports.File({
      filename: logFile,
      maxsize: 10 * 1024 * 1024,
      maxFiles: 5
    }));
    transports.push(new winston.transports.File({
      filename: 'logs/error.log',
      level: 'error',
      maxsize: 10 * 1024 * 1024,
      maxFiles: 5
    }));
  } catch {
    // File transports optional (e.g. read-only CI); console is enough
  }
  return transports;
}

export const logger = winston.createLogger({
  level: (config as any)?.logging?.level ?? 'info',
  format: logFormat,
  transports: buildTransports()
});

export function createChildLogger(meta: Record<string, any>) {
  return logger.child(meta);
}

export function createBattleLogger(battleId: string, playerId?: string) {
  return logger.child({ battleId, playerId, correlationId: getCorrelationId() });
}

export function createRequestLogger(requestId: string, playerId?: string) {
  return logger.child({ requestId, playerId, correlationId: getCorrelationId() });
}
