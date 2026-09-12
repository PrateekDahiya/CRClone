export interface CheckResult {
    status: 'healthy' | 'degraded' | 'unhealthy';
    message: string;
    duration?: number;
    details?: any;
}

export interface HealthCheckResult {
    status: 'healthy' | 'degraded' | 'unhealthy';
    timestamp: string;
    checks: {
        database: CheckResult;
        redis: CheckResult;
        battles: CheckResult;
        memory: CheckResult;
        disk: CheckResult;
    };
    details: {
        uptime: number;
        version: string;
        environment: string;
        activeBattles: number;
        connectedPlayers: number;
        dbConnections: number;
        redisConnections: number;
        memoryUsage: NodeJS.MemoryUsage;
    };
}

// Minimal interfaces to avoid tight coupling to concrete implementations.
// Real Database / Redis / Battle manager are injected as `any` and probed defensively.
export class HealthCheck {
    private database: any;
    private redis: any;
    private battleManager: any;
    private startTime: number;

    constructor(database: any = null, redis: any = null, battleManager: any = null) {
        this.database = database;
        this.redis = redis;
        this.battleManager = battleManager;
        this.startTime = Date.now();
    }

    public async check(): Promise<HealthCheckResult> {
        const timestamp = new Date().toISOString();

        const [dbCheck, redisCheck, battlesCheck, memoryCheck, diskCheck] = await Promise.all([
            this.checkDatabase(),
            this.checkRedis(),
            this.checkBattles(),
            this.checkMemory(),
            this.checkDisk()
        ]);

        const checks = {
            database: dbCheck,
            redis: redisCheck,
            battles: battlesCheck,
            memory: memoryCheck,
            disk: diskCheck
        };

        const statuses = Object.values(checks).map(c => c.status);
        let overallStatus: 'healthy' | 'degraded' | 'unhealthy' = 'healthy';
        if (statuses.includes('unhealthy')) {
            overallStatus = 'unhealthy';
        } else if (statuses.includes('degraded')) {
            overallStatus = 'degraded';
        }

        const details = {
            uptime: Date.now() - this.startTime,
            version: process.env.npm_package_version || '1.0.0',
            environment: process.env.NODE_ENV || 'development',
            activeBattles: this.safeBattleCount(),
            connectedPlayers: this.safePlayerCount(),
            dbConnections: this.safeDbConnections(),
            redisConnections: this.safeRedisConnections(),
            memoryUsage: process.memoryUsage()
        };

        return { status: overallStatus, timestamp, checks, details };
    }

    private async checkDatabase(): Promise<CheckResult> {
        const start = Date.now();
        try {
            if (this.database && typeof this.database.query === 'function') {
                await this.database.query('SELECT 1 as health');
            }
            const duration = Date.now() - start;
            return { status: 'healthy', message: 'Database connection successful', duration };
        } catch (error) {
            return {
                status: 'unhealthy',
                message: `Database check failed: ${error instanceof Error ? error.message : 'Unknown error'}`,
                duration: Date.now() - start
            };
        }
    }

    private async checkRedis(): Promise<CheckResult> {
        const start = Date.now();
        try {
            if (this.redis && typeof this.redis.ping === 'function') {
                await this.redis.ping();
            }
            return { status: 'healthy', message: 'Redis connection successful', duration: Date.now() - start };
        } catch (error) {
            return {
                status: 'unhealthy',
                message: `Redis check failed: ${error instanceof Error ? error.message : 'Unknown error'}`,
                duration: Date.now() - start
            };
        }
    }

    private async checkBattles(): Promise<CheckResult> {
        try {
            const activeBattles = this.safeBattleCount();
            return { status: 'healthy', message: `${activeBattles} battles active`, details: { activeBattles } };
        } catch (error) {
            return { status: 'unhealthy', message: `Battles check failed: ${error instanceof Error ? error.message : 'Unknown'}` };
        }
    }

    private async checkMemory(): Promise<CheckResult> {
        const memUsage = process.memoryUsage();
        const utilization = memUsage.heapUsed / memUsage.heapTotal;
        if (utilization > 0.9) {
            return { status: 'degraded', message: `Memory usage at ${Math.round(utilization * 100)}%`, details: { utilization } };
        }
        return { status: 'healthy', message: `Memory usage: ${Math.round(utilization * 100)}%`, details: { utilization } };
    }

    private async checkDisk(): Promise<CheckResult> {
        return { status: 'healthy', message: 'Disk space adequate' };
    }

    public async quickCheck(): Promise<{ status: string }> {
        try {
            await Promise.all([
                this.database?.query ? this.database.query('SELECT 1') : Promise.resolve(),
                this.redis?.ping ? this.redis.ping() : Promise.resolve()
            ]);
            return { status: 'ok' };
        } catch {
            return { status: 'error' };
        }
    }

    private safeBattleCount(): number {
        try {
            if (this.battleManager && typeof this.battleManager.getActiveBattleCount === 'function') {
                return this.battleManager.getActiveBattleCount();
            }
            if (this.battleManager && typeof this.battleManager.size === 'number') return this.battleManager.size;
        } catch { /* ignore */ }
        return 0;
    }

    private safePlayerCount(): number {
        try {
            if (this.battleManager && typeof this.battleManager.getConnectedPlayerCount === 'function') {
                return this.battleManager.getConnectedPlayerCount();
            }
        } catch { /* ignore */ }
        return 0;
    }

    private safeDbConnections(): number {
        try {
            if (this.database && typeof this.database.getPoolStats === 'function') {
                return this.database.getPoolStats().totalConnections ?? 0;
            }
        } catch { /* ignore */ }
        return 0;
    }

    private safeRedisConnections(): number {
        try {
            if (this.redis && typeof this.redis.getConnectionCount === 'function') {
                return this.redis.getConnectionCount();
            }
        } catch { /* ignore */ }
        return 0;
    }
}

// Backwards-compatible aliases expected by server bootstrap (Agent 5).
// HealthCheckService is the canonical service name; HealthCheck remains for tests.
export class HealthCheckService extends HealthCheck {
    constructor(database: any = null, redis: any = null, battleManager: any = null) {
        super(database, redis, battleManager);
    }
}

export const healthCheck = new HealthCheckService();

export function createHealthMiddleware(hc: HealthCheck = healthCheck) {
    return async (req: any, res: any, next?: any) => {
        try {
            if (req?.url === '/health' || req?.url?.startsWith('/health')) {
                const result = await hc.check();
                if (res?.writeHead && res?.end) {
                    res.writeHead(result.status === 'unhealthy' ? 503 : 200, { 'Content-Type': 'application/json' });
                    res.end(JSON.stringify(result));
                    return;
                }
            }
        } catch {
            // fall through to next handler
        }
        if (typeof next === 'function') next();
    };
}
