export interface AlertRule {
    name: string;
    condition: () => Promise<boolean>;
    severity: 'critical' | 'warning' | 'info';
    message: string;
    cooldownMs: number;
    lastFired?: number;
}

export interface Alert {
    ruleName: string;
    severity: 'critical' | 'warning' | 'info';
    message: string;
    timestamp: string;
    details?: any;
}

export class AlertManager {
    private static instance: AlertManager;
    private rules: AlertRule[] = [];
    private alerts: Alert[] = [];
    private maxAlerts = 1000;
    private checkInterval: NodeJS.Timeout | null = null;
    private webhookUrls: Map<string, string> = new Map(); // severity -> webhook URL

    private constructor() {
        this.initializeDefaultRules();
    }

    public static getInstance(): AlertManager {
        if (!AlertManager.instance) {
            AlertManager.instance = new AlertManager();
        }
        return AlertManager.instance;
    }

    private initializeDefaultRules(): void {
        // Desync rate alert (wired to metrics JSON in production; defaults to false here)
        this.addRule({
            name: 'high_desync_rate',
            condition: async () => {
                return false; // Placeholder - production wires real desync gauge
            },
            severity: 'critical',
            message: 'Desync rate exceeded 1% threshold',
            cooldownMs: 5 * 60 * 1000 // 5 minutes
        });

        // Battle server down
        this.addRule({
            name: 'battle_server_down',
            condition: async () => {
                // Check if any battle servers are responsive
                // This would integrate with BattleServerManager
                return false; // Placeholder
            },
            severity: 'critical',
            message: 'Battle server unresponsive for > 30 seconds',
            cooldownMs: 10 * 60 * 1000 // 10 minutes
        });

        // Database connection pool exhausted
        this.addRule({
            name: 'db_pool_exhausted',
            condition: async () => {
                return false; // Placeholder - production wires real pool stats
            },
            severity: 'critical',
            message: 'Database connection pool > 80% utilized',
            cooldownMs: 5 * 60 * 1000
        });

        // High memory usage
        this.addRule({
            name: 'high_memory_usage',
            condition: async () => {
                const memUsage = process.memoryUsage();
                const utilization = memUsage.heapUsed / memUsage.heapTotal;
                return utilization > 0.9; // > 90%
            },
            severity: 'warning',
            message: 'Memory usage exceeded 90%',
            cooldownMs: 10 * 60 * 1000
        });

        // High CPU usage
        this.addRule({
            name: 'high_cpu_usage',
            condition: async () => {
                // Would track CPU over time
                return false; // Placeholder
            },
            severity: 'warning',
            message: 'CPU usage exceeded 80%',
            cooldownMs: 10 * 60 * 1000
        });

        // Matchmaking queue backup
        this.addRule({
            name: 'matchmaking_queue_backup',
            condition: async () => {
                return false; // Placeholder - production wires real queue size
            },
            severity: 'warning',
            message: 'Matchmaking queue backup - players waiting > 60 seconds',
            cooldownMs: 15 * 60 * 1000
        });

        // Battle duration anomaly
        this.addRule({
            name: 'battle_duration_anomaly',
            condition: async () => {
                // Check for battles lasting too long or too short
                return false; // Placeholder
            },
            severity: 'info',
            message: 'Unusual battle duration pattern detected',
            cooldownMs: 30 * 60 * 1000
        });
    }

    public addRule(rule: AlertRule): void {
        this.rules.push(rule);
    }

    public removeRule(name: string): void {
        this.rules = this.rules.filter(r => r.name !== name);
    }

    public setWebhookUrl(severity: 'critical' | 'warning' | 'info', url: string): void {
        this.webhookUrls.set(severity, url);
    }

    public startChecking(intervalMs: number = 30000): void {
        if (this.checkInterval) {
            clearInterval(this.checkInterval);
        }
        
        this.checkInterval = setInterval(() => {
            this.checkRules();
        }, intervalMs);
        
        // Run immediately
        this.checkRules();
    }

    public stopChecking(): void {
        if (this.checkInterval) {
            clearInterval(this.checkInterval);
            this.checkInterval = null;
        }
    }

    private async checkRules(): Promise<void> {
        const now = Date.now();
        
        for (const rule of this.rules) {
            // Check cooldown
            if (rule.lastFired && (now - rule.lastFired) < rule.cooldownMs) {
                continue;
            }

            try {
                const shouldFire = await rule.condition();
                
                if (shouldFire) {
                    await this.fireAlert(rule);
                    rule.lastFired = now;
                }
            } catch (error) {
                console.error(`Error checking alert rule ${rule.name}:`, error);
            }
        }
    }

    private async fireAlert(rule: AlertRule): Promise<void> {
        const alert: Alert = {
            ruleName: rule.name,
            severity: rule.severity,
            message: rule.message,
            timestamp: new Date().toISOString()
        };

        this.alerts.unshift(alert);
        if (this.alerts.length > this.maxAlerts) {
            this.alerts = this.alerts.slice(0, this.maxAlerts);
        }

        // Log alert
        console.warn(`[ALERT] ${rule.severity.toUpperCase()}: ${rule.message}`);

        // Send webhook notification
        await this.sendWebhook(alert);
    }

    private async sendWebhook(alert: Alert): Promise<void> {
        const webhookUrl = this.webhookUrls.get(alert.severity);
        if (!webhookUrl) return;

        try {
            const payload = {
                text: `[${alert.severity.toUpperCase()}] ${alert.message}`,
                blocks: [
                    {
                        type: 'section',
                        text: {
                            type: 'mrkdwn',
                            text: `*${alert.severity.toUpperCase()} ALERT*\n${alert.message}`
                        }
                    },
                    {
                        type: 'context',
                        elements: [
                            {
                                type: 'mrkdwn',
                                text: `Rule: ${alert.ruleName} | Time: ${alert.timestamp}`
                            }
                        ]
                    }
                ]
            };

            await fetch(webhookUrl, {
                method: 'POST',
                headers: { 'Content-Type': 'application/json' },
                body: JSON.stringify(payload)
            });
        } catch (error) {
            console.error('Failed to send alert webhook:', error);
        }
    }

    public getRecentAlerts(limit: number = 100): Alert[] {
        return this.alerts.slice(0, limit);
    }

    public getAlertsBySeverity(severity: 'critical' | 'warning' | 'info'): Alert[] {
        return this.alerts.filter(a => a.severity === severity);
    }

    public clearAlerts(): void {
        this.alerts = [];
    }

    public getRules(): AlertRule[] {
        return [...this.rules];
    }
}

export const alertManager = AlertManager.getInstance();