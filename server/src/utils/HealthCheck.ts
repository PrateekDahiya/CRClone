import { Database, db } from '../persistence/Database';
import { logger } from '../utils/logger';
import { config } from '../config';

export interface HealthCheckResult {
  status: 'healthy' | 'degraded' | 'unhealthy';
  timestamp: string;
  checks: HealthCheck[];
  uptime: number;
  version: string;
}

export interface HealthCheck {
  name: string;
  status: 'healthy' | 'degraded' | 'unhealthy';
  latencyMs?: number;
  message?: string;
  details?: any;
}

export class HealthCheckService {
  private db: Database;
  private startTime: number;
  private version: string;

  constructor(database: Database = db, version: string = '1.0.0') {
    this.db = database;
    this.startTime = Date.now();
    this.version = version;
  }

  async check(): Promise<HealthCheckResult> {
    const checks: HealthCheck[] = [];

    // Database check
    checks.push(await this.checkDatabase());

    // Memory check
    checks.push(this.checkMemory());

    // Event loop lag check
    checks.push(this.checkEventLoopLag());

    // Determine overall status
    const unhealthy = checks.filter(c => c.status === 'unhealthy').length;
    const degraded = checks.filter(c => c.status === 'degraded').length;

    let overallStatus: 'healthy' | 'degraded' | 'unhealthy' = 'healthy';
    if (unhealthy > 0) overallStatus = 'unhealthy';
    else if (degraded > 0) overallStatus = 'degraded';

    return {
      status: overallStatus,
      timestamp: new Date().toISOString(),
      checks,
      uptime: Date.now() - this.startTime,
      version: this.version
    };
  }

  private async checkDatabase(): Promise<HealthCheck> {
    const start = Date.now();
    try {
      const result = await this.db.query('SELECT 1 as health');
      const latency = Date.now() - start;

      if (result[0]?.health === 1) {
        return {
          name: 'database',
          status: latency > 100 ? 'degraded' : 'healthy',
          latencyMs: latency,
          message: `Database responsive (${latency}ms)`
        };
      } else {
        return {
          name: 'database',
          status: 'unhealthy',
          latencyMs: latency,
          message: 'Database query returned unexpected result'
        };
      }
    } catch (error) {
      const msg = error instanceof Error ? error.message : String(error);
      return {
        name: 'database',
        status: 'unhealthy',
        latencyMs: Date.now() - start,
        message: `Database connection failed: ${msg}`
      };
    }
  }

  private checkMemory(): HealthCheck {
    const used = process.memoryUsage();
    const heapUsedMb = Math.round(used.heapUsed / 1024 / 1024);
    const heapTotalMb = Math.round(used.heapTotal / 1024 / 1024);
    const usagePercent = (used.heapUsed / used.heapTotal) * 100;

    let status: 'healthy' | 'degraded' | 'unhealthy' = 'healthy';
    if (usagePercent > 90) status = 'unhealthy';
    else if (usagePercent > 75) status = 'degraded';

    return {
      name: 'memory',
      status,
      message: `Heap: ${heapUsedMb}MB / ${heapTotalMb}MB (${usagePercent.toFixed(1)}%)`,
      details: {
        heapUsedMb,
        heapTotalMb,
        externalMb: Math.round(used.external / 1024 / 1024),
        rssMb: Math.round(used.rss / 1024 / 1024)
      }
    };
  }

  private checkEventLoopLag(): HealthCheck {
    const start = process.hrtime.bigint();
    // Schedule immediate to measure event loop lag
    return new Promise<HealthCheck>((resolve) => {
      setImmediate(() => {
        const lagNs = Number(process.hrtime.bigint() - start);
        const lagMs = lagNs / 1_000_000;

        let status: 'healthy' | 'degraded' | 'unhealthy' = 'healthy';
        if (lagMs > 100) status = 'unhealthy';
        else if (lagMs > 50) status = 'degraded';

        resolve({
          name: 'event_loop',
          status,
          latencyMs: lagMs,
          message: `Event loop lag: ${lagMs.toFixed(2)}ms`
        });
      });
    }) as any; // Simplified for sync return
  }

  // Quick health check (for load balancer)
  async quickCheck(): Promise<{ status: string; timestamp: string }> {
    try {
      await this.db.query('SELECT 1');
      return { status: 'ok', timestamp: new Date().toISOString() };
    } catch {
      return { status: 'error', timestamp: new Date().toISOString() };
    }
  }

  // Readiness check (for Kubernetes)
  async readinessCheck(): Promise<{ ready: boolean; checks: HealthCheck[] }> {
    const checks: HealthCheck[] = [];

    // Database
    try {
      await this.db.query('SELECT 1');
      checks.push({ name: 'database', status: 'healthy', message: 'Database connected' });
    } catch {
      checks.push({ name: 'database', status: 'unhealthy', message: 'Database unavailable' });
    }

    // Config loaded
    checks.push({ name: 'config', status: 'healthy', message: 'Configuration loaded' });

    const ready = checks.every(c => c.status === 'healthy');
    return { ready, checks };
  }

  // Liveness check (for Kubernetes)
  livenessCheck(): { alive: boolean; uptime: number } {
    return {
      alive: true,
      uptime: Date.now() - this.startTime
    };
  }
}

// Express-style middleware for health endpoint
export function createHealthMiddleware(healthCheck: HealthCheckService) {
  return async (req: any, res: any) => {
    const type = req.query.type || 'full';

    try {
      let result: any;

      switch (type) {
        case 'quick':
          result = await healthCheck.quickCheck();
          break;
        case 'readiness':
          result = await healthCheck.readinessCheck();
          break;
        case 'liveness':
          result = healthCheck.livenessCheck();
          break;
        default:
          result = await healthCheck.check();
      }

      const statusCode = result.status === 'healthy' || result.ready === true || result.alive === true || result.status === 'ok' ? 200 : 503;
      res.status(statusCode).json(result);
    } catch (error) {
      const msg = error instanceof Error ? error.message : String(error);
      res.status(500).json({
        status: 'error',
        message: msg,
        timestamp: new Date().toISOString()
      });
    }
  };
}

export const healthCheck = new HealthCheckService(db);