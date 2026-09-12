import { Database, db } from '../persistence/Database';
import { logger } from '../utils/logger';
import * as fs from 'fs';
import * as path from 'path';
import crypto from 'crypto';

export class MigrationRunner {
  private db: Database;
  private migrationsDir: string;

  constructor(database: Database, migrationsDir?: string) {
    this.db = database;
    this.migrationsDir = migrationsDir || path.join(__dirname, '../../sql/migrations');
  }

  /**
   * Canonical tables each migration must leave behind. Used by
   * applyMigration() post-apply verification and verifyRepair().
   */
  private static readonly EXPECTED_TABLES: Record<string, string[]> = {
    '001': ['players', 'battles'],
    '002': ['clans', 'clan_members'],
    '003': ['tournaments', 'tournament_entries'],
    '004': ['seasons', 'player_quests'],
    '005': ['leaderboards', 'friends'],
  };

  /**
   * Returns the exact repair statement for a migration that was recorded in
   * schema_migrations but created nothing (e.g. 001 on DBs touched before
   * the ISSUE-501 fix). Run it, then re-run runMigrations().
   * See server/sql/migrations/README.md for the full procedure.
   */
  static repairSqlForVersion(version: string): string {
    return `DELETE FROM schema_migrations WHERE version='${version}'`;
  }

  async runMigrations(): Promise<void> {
    logger.info('Starting database migrations...');

    // Ensure schema_migrations table exists
    await this.ensureMigrationsTable();

    // Get applied migrations
    const appliedMigrations = await this.getAppliedMigrations();
    const appliedVersions = new Set(appliedMigrations.map(m => m.version));

    // Get migration files
    const migrationFiles = this.getMigrationFiles();

    for (const file of migrationFiles) {
      const version = this.extractVersion(file);
      
      if (appliedVersions.has(version)) {
        // Verify checksum
        const applied = appliedMigrations.find(m => m.version === version);
        const checksum = this.calculateChecksum(file);
        if (applied && applied.checksum !== checksum) {
          logger.warn(`Migration ${version} checksum mismatch!`, {
            expected: applied.checksum,
            actual: checksum
          });
          // In production, you might want to halt or require manual intervention
        }
        continue;
      }

      logger.info(`Applying migration: ${file}`);
      await this.applyMigration(file, version);
    }

    logger.info('All migrations applied successfully');
  }

  private async ensureMigrationsTable(): Promise<void> {
    await this.db.execute(`
      CREATE TABLE IF NOT EXISTS \`schema_migrations\` (
        \`version\` VARCHAR(32) PRIMARY KEY,
        \`name\` VARCHAR(255) NOT NULL,
        \`applied_at\` TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
        \`checksum\` VARCHAR(64)
      ) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci
    `);
  }

  private async getAppliedMigrations(): Promise<Array<{version: string, name: string, checksum: string}>> {
    return await this.db.query('SELECT version, name, checksum FROM schema_migrations ORDER BY applied_at');
  }

  private getMigrationFiles(): string[] {
    if (!fs.existsSync(this.migrationsDir)) {
      logger.warn(`Migrations directory not found: ${this.migrationsDir}`);
      return [];
    }

    return fs.readdirSync(this.migrationsDir)
      .filter(f => f.endsWith('.sql') && !f.endsWith('.down.sql'))
      .sort(); // Assumes numbered prefixes like 001_, 002_, etc.
  }

  private extractVersion(filename: string): string {
    const match = filename.match(/^(\d+)_/);
    return match ? match[1] : filename.replace('.sql', '');
  }

  private calculateChecksum(filename: string): string {
    const filepath = path.join(this.migrationsDir, filename);
    const content = fs.readFileSync(filepath, 'utf-8');
    return crypto.createHash('sha256').update(content).digest('hex').substring(0, 64);
  }

  private async applyMigration(filename: string, version: string): Promise<void> {
    const filepath = path.join(this.migrationsDir, filename);
    const sql = fs.readFileSync(filepath, 'utf-8');
    const checksum = this.calculateChecksum(filename);
    const name = filename.replace('.sql', '');

    // Split by delimiter and execute each statement.
    // Comment-only chunks are already removed by splitStatements(); anything
    // left here must be real SQL, so an empty remainder means "skip", and a
    // migration that yields zero executable statements is a hard error —
    // recording it as applied would repeat the ISSUE-501 silent no-op.
    const statements = this.splitStatements(sql).filter(s => s.trim().length > 0);

    if (statements.length === 0) {
      throw new Error(
        `Migration ${version} (${filename}) produced zero executable statements - refusing to record as applied`
      );
    }

    await this.db.transaction(async (conn) => {
      for (const stmt of statements) {
        // Text protocol (query), NOT prepared statements (execute): MySQL
        // rejects DDL such as CREATE TRIGGER under the prepared protocol
        // (ER_UNSUPPORTED_PS). Migration statements never carry parameters.
        await conn.query(stmt);
      }

      // Post-apply verification: a migration that executes without creating
      // its tables must roll back, never be recorded as applied (ISSUE-501).
      const expected = MigrationRunner.EXPECTED_TABLES[version] || [];
      for (const table of expected) {
        // Table names come from the static EXPECTED_TABLES map above, never
        // from user input, so string interpolation is safe here.
        const rows = await conn.query(`SHOW TABLES LIKE '${table}'`) as unknown[];
        if (rows.length === 0) {
          throw new Error(
            `Post-apply verification failed for migration ${version}: expected table '${table}' does not exist - rolling back`
          );
        }
      }

      // Record migration
      await conn.execute(
        'INSERT INTO schema_migrations (version, name, checksum) VALUES (?, ?, ?)',
        [version, name, checksum]
      );
    });

    logger.info(`Migration ${version} applied successfully`);
  }

  /**
   * Splits a migration file into executable statements. Public so tests can
   * assert splitter output without a live DB.
   *
   * - Full-line `--` and `#` comments are stripped BEFORE accumulating, so a
   *   `-- ... table` header can never cause its CREATE TABLE to be skipped
   *   or misclassified (ISSUE-501 bug 1).
   * - The trailing delimiter is stripped from the TRIMMED buffer (not via a
   *   raw slice), so `END//` followed by a newline cannot leave a dangling
   *   `END/` (ISSUE-501 bug 2).
   * - `DELIMITER //` ... `DELIMITER ;` blocks (002 triggers) are honoured:
   *   while a custom delimiter is active, `;`-terminated lines inside
   *   BEGIN...END do not split.
   */
  public splitStatements(sql: string): string[] {
    const statements: string[] = [];
    let current = '';
    let delimiter = ';';

    const lines = sql.split('\n');

    for (const line of lines) {
      const trimmed = line.trim();

      // Handle DELIMITER changes
      if (trimmed.startsWith('DELIMITER')) {
        const parts = trimmed.split(/\s+/);
        delimiter = parts.length > 1 ? parts[1] : ';';
        continue;
      }

      // Strip full-line comments and blank lines before accumulating.
      if (trimmed === '' || trimmed.startsWith('--') || trimmed.startsWith('#')) {
        continue;
      }

      current += line + '\n';

      if (current.trimEnd().endsWith(delimiter)) {
        const withoutDelimiter = current.trim();
        statements.push(withoutDelimiter.slice(0, withoutDelimiter.length - delimiter.length).trim());
        current = '';
      }
    }

    // Add any remaining
    if (current.trim()) {
      statements.push(current.trim());
    }

    return statements.filter(s => s.length > 0);
  }

  /**
   * Rolls back one applied migration using its `<version>_*.down.sql` file.
   * Statements run inside a transaction; the schema_migrations row is deleted
   * only if every down statement succeeds. Roll back in reverse version order
   * (005 -> 001) to respect foreign-key dependencies. Never run against the
   * shared live DB without explicit approval.
   */
  async rollback(version: string): Promise<void> {
    const downFile = fs.existsSync(this.migrationsDir)
      ? fs.readdirSync(this.migrationsDir)
          .filter(f => f.endsWith('.down.sql'))
          .sort()
          .find(f => this.extractVersion(f) === version)
      : undefined;

    if (!downFile) {
      throw new Error(
        `Rollback not possible for version ${version} - no down migration file found (expected <version>_*.down.sql)`
      );
    }

    const applied = await this.getAppliedMigrations();
    if (!applied.some(m => m.version === version)) {
      throw new Error(
        `Rollback not possible for version ${version} - migration is not recorded as applied`
      );
    }

    const filepath = path.join(this.migrationsDir, downFile);
    const sql = fs.readFileSync(filepath, 'utf-8');
    const statements = this.splitStatements(sql).filter(s => s.trim().length > 0);

    if (statements.length === 0) {
      throw new Error(
        `Rollback not possible for version ${version} - down file ${downFile} produced zero executable statements`
      );
    }

    logger.warn(`Rolling back migration ${version} via ${downFile}`);

    await this.db.transaction(async (conn) => {
      for (const stmt of statements) {
        // Text protocol (see applyMigration): down files contain DROP
        // TRIGGER, which the prepared-statement protocol rejects.
        await conn.query(stmt);
      }
      await conn.execute('DELETE FROM schema_migrations WHERE version = ?', [version]);
    });

    logger.info(`Migration ${version} rolled back successfully`);
  }

  /**
   * Detects migrations recorded in schema_migrations whose canonical tables
   * are missing (the ISSUE-501 "recorded-but-empty" state). Returns one entry
   * per known version; entries with applied=true and a non-empty
   * tablesMissing list need the README repair procedure
   * (DELETE + re-run) before runMigrations() can help, since runMigrations()
   * skips already-recorded versions.
   */
  async verifyRepair(): Promise<Array<{
    version: string;
    applied: boolean;
    tablesPresent: string[];
    tablesMissing: string[];
    repairSql: string | null;
  }>> {
    const applied = await this.getAppliedMigrations();
    const appliedVersions = new Set(applied.map(m => m.version));

    const report: Array<{
      version: string;
      applied: boolean;
      tablesPresent: string[];
      tablesMissing: string[];
      repairSql: string | null;
    }> = [];

    for (const [version, tables] of Object.entries(MigrationRunner.EXPECTED_TABLES)) {
      if (!appliedVersions.has(version)) {
        report.push({ version, applied: false, tablesPresent: [], tablesMissing: [...tables], repairSql: null });
        continue;
      }
      const present: string[] = [];
      const missing: string[] = [];
      for (const table of tables) {
        const rows = await this.db.query(`SHOW TABLES LIKE '${table}'`) as unknown[];
        if (rows.length === 0) {
          missing.push(table);
        } else {
          present.push(table);
        }
      }
      report.push({
        version,
        applied: true,
        tablesPresent: present,
        tablesMissing: missing,
        repairSql: missing.length > 0 ? MigrationRunner.repairSqlForVersion(version) : null,
      });
    }

    return report;
  }

  async getMigrationStatus(): Promise<Array<{version: string, name: string, applied: boolean, checksum?: string}>> {
    const appliedMigrations = await this.getAppliedMigrations();
    const appliedMap = new Map(appliedMigrations.map(m => [m.version, m]));
    const migrationFiles = this.getMigrationFiles();

    return migrationFiles.map(file => {
      const version = this.extractVersion(file);
      const applied = appliedMap.get(version);
      return {
        version,
        name: file.replace('.sql', ''),
        applied: !!applied,
        checksum: applied?.checksum
      };
    });
  }
}

export const migrationRunner = new MigrationRunner(db);