# QA Cycle #2 Report - 2026-09-13 (Docker full-stack) - `feature/qa-integration`

Base: `846a4e3` (master incl. PR #10-#13) + 2 QA commits (`5ab5c34`, `9f74d7d`, pushed next).
Stack: `docker-compose.test.yml` + QA-local override (dropped after compose fix) —
mysql:8.0 + redis:7-alpine + server(old `target: test`), all on `crclone-test-network`.

## New issues found & fixed this cycle

| ID | Sev | Finding (evidence) | Fix (commit) | Verified |
|----|-----|--------------------|--------------|----------|
| QA-COMPOSE-001 | P1 | `docker-compose.test.yml` mounted `/var/lib/mysql` twice (named volume + tmpfs) → `up` aborted: `target ... already mounted` | Dropped named-volume mount + decl, kept tmpfs; removed obsolete `version:` key (`5ab5c34`) | `up` proceeds |
| QA-BOOT-001 | P0 | Server exited 1: `Failed to start server {"error":"Database not initialized"}` — `GameServer` connected its own `new Database()` but booted `migrationRunner` singleton wrapping never-connected `db` | `new MigrationRunner(this.database)` (`5ab5c34`) | Migrations 001-005 apply on boot |
| QA-BOOT-002 | P0 | Same class, one layer deeper: `configService` singleton wraps dead `db` (`ConfigService.ts:214`), `index.ts:294` → exit 1 after "Migrations completed" | `await db.connect()` for shared singleton in `start()` (`9f74d7d`, 2-pool accepted) | `Loaded 15 config values`, boot completes |
| QA-INIT-001 | P1 | `server/sql/init.sql:422` FK `player_decks(deck_id)` — column doesn't exist (canonical: `player_decks(id)` per `003:48`); file is stale DDL copy, ZERO seeds | Dropped the init.sql mount (MigrationRunner is single init path); file kept on disk (`9f74d7d` agent work) | Fresh mysql `healthy`, `init done` |
| QA-WSPORT-001 | P0 | `wsServer` shared httpServer → WS on :3000, NOTHING on :3001 (log lied); e2e `WS_URL=ws://server:3001` dead | Standalone `new WebSocket.Server({port: wsPort})` + close both servers on shutdown (`9f74d7d`) | WS:3001 pong (see below) |
| QA-HEALTH-001 | P1 | Healthcheck `curl` missing in node:20-alpine → `(unhealthy)` forever → `depends_on: service_healthy` gates hang | `wget -q -O /dev/null` probe | `Up (healthy)` |
| QA-CFG-001 | P3 | mysql2 warns `acquireTimeout`/`timeout` invalid (future hard error) | Keys removed from DB config (`5ab5c34`) | Warnings gone |

## Live verification (all QA-executed, not trusted)
- Migrations UP on docker MySQL 8.0: 001→005, **43 tables + 2 triggers**, versions recorded.
- Migrations DOWN on scratch DB: 43→33→28→23→15→1 (schema_migrations kept, versions emptied). ISSUE-502 closed with live evidence.
- `docker ps`: server/mysql/redis ALL `(healthy)`.
- `curl :3000/health` → **200** (`degraded` payload only due to 97-98% container memory reading — environmental).
- WS `:3001` (QA's own run): `OPEN → heartbeat → {"type":"pong"}`; garbage → `{"type":"error","code":"INVALID_MESSAGE_FORMAT"}`. `:3000` is HTTP-only now.
- `npm run test:unit` 12/96 · `test:integration` 6/63 · `typecheck` 0 err · `lint` 0 err / 135 warn.

## Still blocked (needs YOU)
1. **UNITY_LICENSE** — `unity-test-runner` cannot start without it (+ ~6GB image + missing `Packages/`). Recommend CI-only Unity validation.
2. **e2e client build** — `./build/` absent; WS-dependent journeys now have a live target (`ws://server:3001` via compose network) but browser/CLI run not attempted.
3. **k6** — not run yet (server only just became bootable); next: `docker run grafana/k6` smoke against live stack.

## Carried notes (non-blocking)
- Dual MySQL pools (GameServer-owned + `db` singleton) — unify later (P3).
- `AnalyticsHooks.BattleType` drift + `InputManager NetworkClient.InputType` + `CardRarity` usings (pre-existing C# flags from ISSUE-103 fixer) — confirm via Unity CI.
- BUG-008 (spell arrival damage) still review-only. Perf thresholds (<4ms tick etc.) still unmeasured (k6 next).
- Aiven shared DB untouched this cycle (all live-DB work on docker test DB).

*QA Agent 7 — I fix nothing directly; fixes above by directed micro-agents, every line re-verified by QA.*
