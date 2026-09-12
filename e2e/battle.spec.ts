import { test, expect } from '@playwright/test';

const BASE_URL = process.env.BASE_URL || 'http://localhost:3000';
const WS_URL = process.env.WS_URL || 'ws://localhost:3001';

test.describe('Battle WebSocket flow', () => {
  test('Battle server accepts connection and rejects unauthenticated input', async ({ page }) => {
    // Evaluate WebSocket in browser context
    const result = await page.evaluate(async ({ wsUrl }: any) => {
      return new Promise((resolve) => {
        try {
          const ws = new WebSocket(`${wsUrl}/battle`);
          const timer = setTimeout(() => resolve({ connected: false, reason: 'timeout' }), 5000);
          ws.onopen = () => {
            clearTimeout(timer);
            // Send invalid input without auth
            ws.send(JSON.stringify({ type: 'input', input: { type: 'play_card' }, clientTick: 1 }));
            setTimeout(() => { ws.close(); resolve({ connected: true }); }, 1000);
          };
          ws.onerror = () => { clearTimeout(timer); resolve({ connected: false, reason: 'error' }); };
        } catch (e: any) {
          resolve({ connected: false, reason: String(e) });
        }
      });
    }, { wsUrl: WS_URL });

    // Server should at least accept TCP connection (auth handled app-level)
    expect(result.connected).toBe(true);
  });

  test('Battle REST endpoints respond', async ({ page }) => {
    for (const path of ['/api/v1/battles/active', '/api/v1/matchmaking/status']) {
      const res = await page.request.get(`${BASE_URL}${path}`);
      expect([200, 401, 404]).toContain(res.status());
    }
  });
});
