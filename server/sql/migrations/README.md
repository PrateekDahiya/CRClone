# SQL Migrations

Up migrations live in this directory as `<version>_<name>.sql` (`001`–`005`).
Each has a matching `<version>_<name>.down.sql` used only by
`MigrationRunner.rollback(version)`.

## Repair procedure: migration recorded but created nothing (ISSUE-501)

DBs touched before the ISSUE-501 fix may hold `001` in `schema_migrations`
while containing zero 001 tables (the old splitter skipped every
`-- ... table`-headed `CREATE TABLE`). `runMigrations()` skips recorded
versions, so re-running alone cannot heal this state. Repair:

1. Confirm the empty-001 state (expect `players`/`battles` missing):
   `SELECT version, name FROM schema_migrations;`
   `SHOW TABLES LIKE 'players';`
   (or call `MigrationRunner.verifyRepair()` — entries with `applied: true`
   and a non-empty `tablesMissing` list need repair; `repairSql` holds the
   exact statement).
2. Delete the bogus record (NEVER delete rows for migrations whose tables
   actually exist):
   `DELETE FROM schema_migrations WHERE version='001';`
   (same string as `MigrationRunner.repairSqlForVersion('001')`).
3. Re-run migrations: `MigrationRunner.runMigrations()` applies 001–005.
   The fixed `applyMigration()` verifies `players`/`battles` exist after 001
   and rolls back instead of recording a silent no-op.
4. Verify: `SHOW TABLES;` must list all domain tables
   (001: players/cards/battles/..., 002: clans/..., 003: tournaments/...,
   004: seasons/quests/..., 005: leaderboards/friends/...),
   and `SELECT version FROM schema_migrations;` must return 001–005.

## Rollback (ISSUE-502)

`MigrationRunner.rollback(version)` executes the matching `.down.sql` inside
a transaction and deletes the `schema_migrations` row. Rules:

- Roll back in reverse version order (005 -> 001) to respect foreign keys
  (each down file also sets `FOREIGN_KEY_CHECKS=0`).
- `001`'s down file drops all 001 domain tables but NOT the
  `schema_migrations` table itself (the runner needs it to delete the row).
- Do NOT run any DOWN migration against the shared live DB without explicit
  approval — statement-level test proof suffices
  (see `server/tests/integration/migrations.test.ts`).
- The old "manual database restore" fallback this replaces: full backup
  restore, now only a last resort if a down file is missing (rollback throws
  in that case instead of silently succeeding).
