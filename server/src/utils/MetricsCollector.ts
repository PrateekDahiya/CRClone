import { logger } from '../utils/logger';

/**
 * MetricsCollector - Collects and exposes Prometheus metrics
 */
export class MetricsCollector {
  private static instance: MetricsCollector;
  private metrics: Map<string, MetricValue> = new Map();
  private counters: Map<string, number> = new Map();
  private gauges: Map<string, number> = new Map();
  private histograms: Map<string, number[]> = new Map();
  private flushInterval: NodeJS.Timeout | null = null;

  static getInstance(): MetricsCollector {
    if (!MetricsCollector.instance) {
      MetricsCollector.instance = new MetricsCollector();
    }
    return MetricsCollector.instance;
  }

  private constructor() {
    // Start periodic flush
    this.flushInterval = setInterval(() => this.flush(), 10000); // Every 10 seconds
  }

  // Counter - monotonically increasing
  incrementCounter(name: string, labels: Record<string, string> = {}, value: number = 1): void {
    const key = this.getKey(name, labels);
    const current = this.counters.get(key) || 0;
    this.counters.set(key, current + value);
  }

  // Gauge - can go up and down
  setGauge(name: string, value: number, labels: Record<string, string> = {}): void {
    const key = this.getKey(name, labels);
    this.gauges.set(key, value);
  }

  incrementGauge(name: string, labels: Record<string, string> = {}, value: number = 1): void {
    const key = this.getKey(name, labels);
    const current = this.gauges.get(key) || 0;
    this.gauges.set(key, current + value);
  }

  decrementGauge(name: string, labels: Record<string, string> = {}, value: number = 1): void {
    this.incrementGauge(name, labels, -value);
  }

  // Histogram - track distribution of values
  observeHistogram(name: string, value: number, labels: Record<string, string> = {}): void {
    const key = this.getKey(name, labels);
    const values = this.histograms.get(key) || [];
    values.push(value);
    // Keep only last 1000 values per histogram
    if (values.length > 1000) values.shift();
    this.histograms.set(key, values);
  }

  // Timing helper
  startTimer(name: string, labels: Record<string, string> = {}): () => void {
    const start = process.hrtime.bigint();
    return () => {
      const end = process.hrtime.bigint();
      const durationMs = Number(end - start) / 1_000_000;
      this.observeHistogram(name, durationMs, labels);
    };
  }

  private getKey(name: string, labels: Record<string, string>): string {
    const labelStr = Object.entries(labels).sort(([a], [b]) => a.localeCompare(b)).map(([k, v]) => `${k}="${v}"`).join(',');
    return labelStr ? `${name}{${labelStr}}` : name;
  }

  // Get metrics in Prometheus format
  getPrometheusMetrics(): string {
    const lines: string[] = [];

    // Counters
    for (const [key, value] of this.counters) {
      lines.push(`# TYPE ${key.split('{')[0]} counter`);
      lines.push(`${key} ${value}`);
    }

    // Gauges
    for (const [key, value] of this.gauges) {
      lines.push(`# TYPE ${key.split('{')[0]} gauge`);
      lines.push(`${key} ${value}`);
    }

    // Histograms
    for (const [key, values] of this.histograms) {
      if (values.length === 0) continue;
      const baseName = key.split('{')[0];
      lines.push(`# TYPE ${baseName} histogram`);
      
      const sorted = [...values].sort((a, b) => a - b);
      const count = sorted.length;
      const sum = sorted.reduce((a, b) => a + b, 0);
      lines.push(`${key}_count ${count}`);
      lines.push(`${key}_sum ${sum}`);
      
      // Quantiles
      const quantiles = [0.5, 0.9, 0.95, 0.99];
      for (const q of quantiles) {
        const idx = Math.min(Math.floor(q * count), count - 1);
        lines.push(`${key}{quantile="${q}"} ${sorted[idx]}`);
      }
    }

    return lines.join('\n') + '\n';
  }

  // Get JSON format for custom endpoints
  getJsonMetrics(): any {
    const result: any = { counters: {}, gauges: {}, histograms: {} };

    for (const [key, value] of this.counters) {
      result.counters[key] = value;
    }

    for (const [key, value] of this.gauges) {
      result.gauges[key] = value;
    }

    for (const [key, values] of this.histograms) {
      if (values.length === 0) continue;
      const sorted = [...values].sort((a, b) => a - b);
      result.histograms[key] = {
        count: sorted.length,
        sum: sorted.reduce((a, b) => a + b, 0),
        min: sorted[0],
        max: sorted[sorted.length - 1],
        avg: sorted.reduce((a, b) => a + b, 0) / sorted.length,
        p50: sorted[Math.floor(0.5 * sorted.length)],
        p90: sorted[Math.floor(0.9 * sorted.length)],
        p95: sorted[Math.floor(0.95 * sorted.length)],
        p99: sorted[Math.floor(0.99 * sorted.length)]
      };
    }

    return result;
  }

  // Predefined metric helpers
  recordBattleStarted(battleType: string): void {
    this.incrementCounter('battles_started_total', { type: battleType });
  }

  recordBattleEnded(battleType: string, durationMs: number, winner: string): void {
    this.incrementCounter('battles_ended_total', { type: battleType, winner });
    this.observeHistogram('battle_duration_ms', durationMs, { type: battleType });
  }

  recordPlayerConnected(): void {
    this.incrementCounter('players_connected_total');
    this.incrementGauge('players_online');
  }

  recordPlayerDisconnected(): void {
    this.decrementGauge('players_online');
  }

  recordMatchmakingTime(ms: number, battleType: string): void {
    this.observeHistogram('matchmaking_time_ms', ms, { type: battleType });
  }

  recordApiRequest(method: string, endpoint: string, statusCode: number, durationMs: number): void {
    this.incrementCounter('api_requests_total', { method, endpoint, status: statusCode.toString() });
    this.observeHistogram('api_request_duration_ms', durationMs, { method, endpoint });
  }

  recordDatabaseQuery(queryType: string, durationMs: number, success: boolean): void {
    this.incrementCounter('db_queries_total', { type: queryType, success: success.toString() });
    this.observeHistogram('db_query_duration_ms', durationMs, { type: queryType });
  }

  recordWebSocketMessage(type: string, sizeBytes: number): void {
    this.incrementCounter('ws_messages_total', { type });
    this.observeHistogram('ws_message_size_bytes', sizeBytes, { type });
  }

  recordError(type: string, component: string): void {
    this.incrementCounter('errors_total', { type, component });
  }

  setActiveBattles(count: number): void {
    this.setGauge('active_battles', count);
  }

  setQueueSize(battleType: string, size: number): void {
    this.setGauge('matchmaking_queue_size', size, { type: battleType });
  }

  // Flush metrics (for push gateway or logging)
  flush(): void {
    if (process.env.METRICS_LOG === 'true') {
      const json = this.getJsonMetrics();
      logger.debug('Metrics flush', json);
    }
  }

  // Reset all metrics (for testing)
  reset(): void {
    this.counters.clear();
    this.gauges.clear();
    this.histograms.clear();
  }

  shutdown(): void {
    if (this.flushInterval) {
      clearInterval(this.flushInterval);
      this.flushInterval = null;
    }
  }
}

interface MetricValue {
  value: number;
  timestamp: number;
  labels: Record<string, string>;
}

export const metricsCollector = MetricsCollector.getInstance();