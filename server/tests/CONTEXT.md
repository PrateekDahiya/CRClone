# CONTEXT.md — server/tests (Agent 6)

## Purpose
Jest unit + integration + k6 load tests for the Node/TypeScript game server.

## Layout
- `setup.ts` — env defaults only (no DB connection; keeps CI green without live services).
- `unit/` — rng, matchmaking (+PriorityQueue/Queue), playerService, clan/shop/quest/season/tournament/replay (mocked repos), metrics (MetricsCollector/HealthCheck/AlertManager).
- `integration/` — battleFlow (server BattleSimulation determinism + matchmaking drain),
  matchmaking (4-player drain), database (repo construction), replay (store/retrieve delegation).
- `load/k6-load-test.js` — ramp 100→1000→5000 VUs; thresholds p95 connect <500ms, p99 msg <100ms.

## Conventions
- Real APIs as of 2026-09-12: `DeterministicRNG(bigint)`, `Matchmaker(playerService).addToQueue(entry)`,
  `PlayerService.saveDeck` throws `DECK_MUST_HAVE_8_CARDS` / `MAX_ONE_CHAMPION_PER_DECK`.
- `BattleServer` is **mocked** in matchmaking/battle tests (see BUG_REPORTS.md BUG-004: real
  `createBattle` crashes on `BigInt(Infinity)` seed round-trip).
- Repository tests mock at the repository-prototype level; no live MySQL/Redis required.
- `MetricsCollector` is Agent 5's in-memory implementation (no prom-client). `HealthCheck` accepts
  nullable deps; `healthCheck` singleton + `HealthCheckService` alias + `createHealthMiddleware`
  are provided for `src/index.ts` compatibility.

## Commands (server/)
- `npm run test:unit` / `npm run test:integration` / `npm run test` (all, 71 tests)
- `npm run test:coverage`, `npm run lint` (warnings only; 0 errors), `npm run typecheck`
- Load: `k6 run tests/load/k6-load-test.js` (needs running server + k6 binary)
