import { MetricsCollector } from '../../src/utils/MetricsCollector';
import { HealthCheck } from '../../src/utils/HealthCheck';
import { AlertManager } from '../../src/utils/AlertManager';

describe('MetricsCollector', () => {
  test('Singleton returns same instance', () => {
    expect(MetricsCollector.getInstance()).toBe(MetricsCollector.getInstance());
  });

  test('Counters, gauges, histograms work', () => {
    const mc = MetricsCollector.getInstance();
    mc.reset();
    expect(() => {
      mc.incrementCounter('test_counter');
      mc.setGauge('test_gauge', 5);
      mc.incrementGauge('test_gauge');
      mc.decrementGauge('test_gauge');
      mc.observeHistogram('test_hist', 12.5);
      const end = mc.startTimer('test_timer');
      end();
    }).not.toThrow();
    const prom = mc.getPrometheusMetrics();
    expect(typeof prom).toBe('string');
    expect(prom).toContain('test_counter');
    const json = mc.getJsonMetrics();
    expect(json.counters).toBeDefined();
    mc.reset();
    mc.shutdown();
  });

  test('Predefined battle/player/api helpers work', () => {
    const mc = MetricsCollector.getInstance();
    mc.reset();
    expect(() => {
      mc.recordBattleStarted('ladder');
      mc.recordBattleEnded('ladder', 120000, 'player1');
      mc.recordPlayerConnected();
      mc.recordPlayerDisconnected();
      mc.recordMatchmakingTime(1500, 'ladder');
      mc.recordApiRequest('GET', '/health', 200, 3);
      mc.recordDatabaseQuery('select', 2, true);
      mc.recordWebSocketMessage('input', 128);
      mc.recordError('timeout', 'battle');
      mc.setActiveBattles(3);
      mc.setQueueSize('ladder', 7);
    }).not.toThrow();
    mc.reset();
    mc.shutdown();
  });
});

describe('HealthCheck', () => {
  test('Healthy when deps respond', async () => {
    const hc = new HealthCheck(
      { query: async () => [] },
      { ping: async () => 'PONG' },
      { getActiveBattleCount: () => 2 }
    );
    const res = await hc.check();
    expect(res.status).toBe('healthy');
    expect(res.checks.database.status).toBe('healthy');
    expect(res.checks.redis.status).toBe('healthy');
    expect(res.details.activeBattles).toBe(2);
  });

  test('Unhealthy when DB fails', async () => {
    const hc = new HealthCheck(
      { query: async () => { throw new Error('db down'); } },
      { ping: async () => 'PONG' },
      {}
    );
    const res = await hc.check();
    expect(res.status).toBe('unhealthy');
    expect(res.checks.database.status).toBe('unhealthy');
  });

  test('Degraded on high memory', async () => {
    const orig = process.memoryUsage;
    (process as any).memoryUsage = () => ({ heapUsed: 950, heapTotal: 1000, rss: 0, external: 0, arrayBuffers: 0 } as any);
    const hc = new HealthCheck({ query: async () => [] }, { ping: async () => 'PONG' }, {});
    const res = await hc.check();
    expect(res.checks.memory.status).toBe('degraded');
    process.memoryUsage = orig;
  });

  test('quickCheck ok/error', async () => {
    const ok = new HealthCheck({ query: async () => [] }, { ping: async () => 'PONG' }, {});
    expect(await ok.quickCheck()).toEqual({ status: 'ok' });
    const bad = new HealthCheck({ query: async () => { throw new Error('x'); } }, {}, {});
    expect(await bad.quickCheck()).toEqual({ status: 'error' });
  });
});

describe('AlertManager', () => {
  test('Rule management', async () => {
    const am = AlertManager.getInstance();
    const before = am.getRules().length;
    am.addRule({ name: 'test_rule_tmp', condition: async () => false, severity: 'info', message: 'test', cooldownMs: 60000 });
    expect(am.getRules().length).toBe(before + 1);
    am.removeRule('test_rule_tmp');
    expect(am.getRules().length).toBe(before);
  });

  test('Fires alert when condition true', async () => {
    const am = AlertManager.getInstance();
    am.clearAlerts();
    am.addRule({ name: 'always_fire_tmp', condition: async () => true, severity: 'warning', message: 'boom', cooldownMs: 60000 });
    await (am as any).checkRules();
    const alerts = am.getRecentAlerts(10);
    expect(alerts.some(a => a.ruleName === 'always_fire_tmp')).toBe(true);
    am.removeRule('always_fire_tmp');
    am.clearAlerts();
  });

  test('Respects cooldown (fires once)', async () => {
    const am = AlertManager.getInstance();
    am.clearAlerts();
    am.addRule({ name: 'cooldown_tmp', condition: async () => true, severity: 'info', message: 'x', cooldownMs: 60000 });
    await (am as any).checkRules();
    await (am as any).checkRules();
    expect(am.getRecentAlerts(10).filter(a => a.ruleName === 'cooldown_tmp')).toHaveLength(1);
    am.removeRule('cooldown_tmp');
    am.clearAlerts();
  });
});
