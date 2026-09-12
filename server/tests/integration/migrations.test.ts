/**
 * ISSUE-501/502: MigrationRunner statement-level tests (no live DB needed).
 *
 * Covers the splitter fixes (comment stripping, DELIMITER handling) and the
 * down-migration contract by parsing the real files in sql/migrations.
 */
import * as fs from 'fs';
import * as path from 'path';
import { MigrationRunner } from '../../src/utils/MigrationRunner';

const MIGRATIONS_DIR = path.join(__dirname, '../../sql/migrations');

function makeRunner(): MigrationRunner {
  // Statement-level tests never touch the DB; the Database handle is unused.
  return new MigrationRunner({} as any, MIGRATIONS_DIR);
}

function readUp(version: string): string {
  const file = fs.readdirSync(MIGRATIONS_DIR)
    .filter(f => f.endsWith('.sql') && !f.endsWith('.down.sql'))
    .find(f => f.startsWith(version + '_'));
  if (!file) throw new Error(`Up migration ${version} not found`);
  return fs.readFileSync(path.join(MIGRATIONS_DIR, file), 'utf-8');
}

function readDown(version: string): string {
  const file = fs.readdirSync(MIGRATIONS_DIR)
    .filter(f => f.endsWith('.down.sql'))
    .find(f => f.startsWith(version + '_'));
  if (!file) throw new Error(`Down migration ${version} not found`);
  return fs.readFileSync(path.join(MIGRATIONS_DIR, file), 'utf-8');
}

/** Table names from CREATE TABLE [IF NOT EXISTS] `name` occurrences. */
function createdTables(sql: string): string[] {
  const out: string[] = [];
  const re = /CREATE\s+TABLE\s+(?:IF\s+NOT\s+EXISTS\s+)?`?(\w+)`?/gi;
  let m: RegExpExecArray | null;
  while ((m = re.exec(sql)) !== null) out.push(m[1]);
  return out;
}

/** Table/view names dropped by a down file. */
function droppedObjects(sql: string): string[] {
  const out: string[] = [];
  const re = /DROP\s+(?:TABLE|VIEW)\s+(?:IF\s+EXISTS\s+)?`?(\w+)`?/gi;
  let m: RegExpExecArray | null;
  while ((m = re.exec(sql)) !== null) out.push(m[1]);
  return out;
}

describe('MigrationRunner.splitStatements (ISSUE-501)', () => {
  const runner = makeRunner();

  test('001: every CREATE TABLE survives; no statement starts with --', () => {
    const sql = readUp('001');
    const statements = runner.splitStatements(sql);

    expect(statements.length).toBeGreaterThan(0);
    for (const s of statements) {
      expect(s.startsWith('--')).toBe(false);
      expect(s.startsWith('#')).toBe(false);
    }

    // Statement count sanity: ≥ tables + SET lines + final INSERT.
    const tables = createdTables(sql);
    expect(tables.length).toBeGreaterThan(0);
    expect(statements.length).toBeGreaterThanOrEqual(tables.length);

    // Every CREATE TABLE in the file must appear intact in exactly one statement.
    for (const table of tables) {
      const holders = statements.filter(s =>
        new RegExp(`CREATE\\s+TABLE\\s+(?:IF\\s+NOT\\s+EXISTS\\s+)?\`?${table}\`?`, 'i').test(s)
      );
      expect(holders).toHaveLength(1);
    }

    // The seed INSERT must survive as a single statement (multi-line VALUES).
    const inserts = statements.filter(s => /INSERT\s+(?:IGNORE\s+)?INTO\s+`?game_config`?/i.test(s));
    expect(inserts).toHaveLength(1);
  });

  test('002: both triggers intact, no dangling delimiter', () => {
    const sql = readUp('002');
    const statements = runner.splitStatements(sql);

    // No statement may start with a comment header (bug 1 swallowed the
    // first trigger chunk via its `-- Triggers...` header).
    for (const s of statements) {
      expect(s.startsWith('--')).toBe(false);
    }

    const insertTrigger = statements.filter(s => s.includes('trg_clan_member_count_insert'));
    const deleteTrigger = statements.filter(s => s.includes('trg_clan_member_count_delete'));
    expect(insertTrigger).toHaveLength(1);
    expect(deleteTrigger).toHaveLength(1);

    for (const trg of [...insertTrigger, ...deleteTrigger]) {
      expect(trg).toMatch(/CREATE\s+TRIGGER/i);
      expect(trg).toMatch(/BEGIN/i);
      // Proper delimiter strip: ends with END, not END/ or END//.
      expect(trg.trimEnd().endsWith('END')).toBe(true);
    }

    // No statement anywhere may contain a leftover custom delimiter or a
    // dangling slash (bug 2 corrupted `END//\n` into `END/`).
    for (const s of statements) {
      expect(s).not.toContain('//');
      expect(s.trimEnd().endsWith('/')).toBe(false);
    }

    // All 8 clan tables present.
    for (const table of createdTables(sql)) {
      expect(statements.some(s => s.includes(table))).toBe(true);
    }
  });

  test('003-005: splitter yields executable statements, comment-free', () => {
    for (const version of ['003', '004', '005']) {
      const sql = readUp(version);
      const statements = runner.splitStatements(sql);
      expect(statements.length).toBeGreaterThan(0);
      for (const s of statements) {
        expect(s.startsWith('--')).toBe(false);
        expect(s).not.toContain('//');
      }
      for (const table of createdTables(sql)) {
        expect(statements.some(s => s.includes(table))).toBe(true);
      }
    }
  });

  test('005: views survive the splitter', () => {
    const statements = runner.splitStatements(readUp('005'));
    expect(statements.some(s => /CREATE.*VIEW.*v_replay_search/is.test(s))).toBe(true);
    expect(statements.some(s => /CREATE.*VIEW.*v_battle_events_detailed/is.test(s))).toBe(true);
  });

  test('getMigrationFiles excludes down files', () => {
    const files = (runner as any).getMigrationFiles() as string[];
    expect(files.length).toBeGreaterThan(0);
    for (const f of files) {
      expect(f.endsWith('.down.sql')).toBe(false);
    }
    expect(files).toHaveLength(5);
  });

  test('repairSqlForVersion returns the documented DELETE statement', () => {
    expect(MigrationRunner.repairSqlForVersion('001'))
      .toBe("DELETE FROM schema_migrations WHERE version='001'");
  });
});

describe('Down migrations (ISSUE-502)', () => {
  const runner = makeRunner();

  test.each(['001', '002', '003', '004', '005'])(
    '%s down file parses to non-empty statements',
    (version) => {
      const statements = runner.splitStatements(readDown(version))
        .filter(s => s.trim().length > 0);
      expect(statements.length).toBeGreaterThan(0);
      for (const s of statements) {
        expect(s.startsWith('--')).toBe(false);
      }
    }
  );

  test.each(['001', '002', '003', '004', '005'])(
    '%s down file drops only objects created by its up file',
    (version) => {
      const up = readUp(version);
      const down = readDown(version);
      const created = new Set(createdTables(up));
      // 005 views are CREATE OR REPLACE VIEW, not CREATE TABLE — allow them.
      if (version === '005') {
        created.add('v_replay_search');
        created.add('v_battle_events_detailed');
      }
      // 002 triggers are CREATE TRIGGER — allow them.
      if (version === '002') {
        created.add('trg_clan_member_count_insert');
        created.add('trg_clan_member_count_delete');
      }
      const dropped = droppedObjects(down);
      expect(dropped.length).toBeGreaterThan(0);
      for (const obj of dropped) {
        expect(created.has(obj)).toBe(true);
      }
      // Every table the up file creates must be dropped (except the
      // schema_migrations bookkeeping table, kept deliberately in 001 down).
      for (const table of createdTables(up)) {
        if (version === '001' && table === 'schema_migrations') continue;
        expect(dropped).toContain(table);
      }
    }
  );

  test('rollback throws when no down file exists (no live DB touched)', async () => {
    await expect(runner.rollback('999')).rejects.toThrow(/no down migration file/i);
  });

  test('rollback throws when migration is not recorded as applied', async () => {
    const dbStub = {
      query: jest.fn().mockResolvedValue([]),
      execute: jest.fn(),
      transaction: jest.fn(),
    };
    const r = new MigrationRunner(dbStub as any, MIGRATIONS_DIR);
    await expect(r.rollback('005')).rejects.toThrow(/not recorded as applied/i);
    expect(dbStub.transaction).not.toHaveBeenCalled();
  });
});
