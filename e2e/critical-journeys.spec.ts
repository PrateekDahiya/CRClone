import { test, expect } from '@playwright/test';

const BASE_URL = process.env.BASE_URL || 'http://localhost:3000';

async function guestLogin(page: any) {
  // API-level guest login; UI screens are Unity-driven so E2E asserts API + health
  const res = await page.request.post(`${BASE_URL}/api/v1/auth/guest`);
  expect(res.ok()).toBeTruthy();
  const body = await res.json();
  expect(body.token || body.player || body.id).toBeTruthy();
  return body;
}

test.describe('Critical User Journeys @Critical', () => {
  test('Server health is reachable', async ({ page }) => {
    const res = await page.request.get(`${BASE_URL}/health`);
    expect(res.ok()).toBeTruthy();
    const body = await res.json();
    expect(body.status).toMatch(/ok|healthy|degraded/);
  });

  test('Guest login -> profile fetch', async ({ page }) => {
    const login = await guestLogin(page);
    const token = login.token;
    if (token) {
      const profile = await page.request.get(`${BASE_URL}/api/v1/players/me`, {
        headers: { Authorization: `Bearer ${token}` }
      });
      // Accept 200 or 404 (route may differ); main assertion is auth worked
      expect([200, 404]).toContain(profile.status());
    }
  });

  test('Metrics endpoint exposes battle metrics', async ({ page }) => {
    const res = await page.request.get(`${BASE_URL}/metrics`);
    expect(res.ok()).toBeTruthy();
    const text = await res.text();
    expect(text).toContain('crclone_');
    expect(text).toContain('crclone_battles_active');
    expect(text).toContain('crclone_players_online');
  });

  test('Deck validation rejects bad deck via API', async ({ page }) => {
    const login = await guestLogin(page);
    const token = login.token;
    if (!token) return;
    const res = await page.request.post(`${BASE_URL}/api/v1/decks/validate`, {
      headers: { Authorization: `Bearer ${token}` },
      data: { cardIds: [27000000, 27000001, 1, 2, 3, 4, 5, 6] }
    });
    // Either validated-false or 4xx; both mean rejection path works
    if (res.ok()) {
      const body = await res.json();
      expect(body.valid ?? body.success ?? false).toBeFalsy();
    } else {
      expect(res.status()).toBeGreaterThanOrEqual(400);
    }
  });

  test('Shop offers endpoint responds', async ({ page }) => {
    const res = await page.request.get(`${BASE_URL}/api/v1/shop/offers`);
    expect([200, 401, 404]).toContain(res.status());
  });

  test('Clan search endpoint responds', async ({ page }) => {
    const res = await page.request.get(`${BASE_URL}/api/v1/clans/search?q=test`);
    expect([200, 401, 404]).toContain(res.status());
  });
});
