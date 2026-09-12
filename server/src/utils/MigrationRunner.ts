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
      .filter(f => f.endsWith('.sql'))
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

    // Split by semicolon and execute each statement
    const statements = this.splitStatements(sql);

    await this.db.transaction(async (conn) => {
      for (const stmt of statements) {
        const trimmed = stmt.trim();
        if (trimmed && !trimmed.startsWith('--')) {
          await conn.execute(trimmed);
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

  private splitStatements(sql: string): string[] {
    // Simple statement splitter - handles basic cases
    // For production, consider using a proper SQL parser
    const statements: string[] = [];
    let current = '';
    let inDelimiter = false;
    let delimiter = ';';

    const lines = sql.split('\n');
    
    for (const line of lines) {
      const trimmed = line.trim();
      
      // Handle DELIMITER changes
      if (trimmed.startsWith('DELIMITER')) {
        if (inDelimiter) {
          delimiter = ';';
          inDelimiter = false;
        } else {
          const parts = trimmed.split(' ');
          if (parts.length > 1) {
            delimiter = parts[1];
            inDelimiter = true;
          }
        }
        continue;
      }

      current += line + '\n';

      if (trimmed.endsWith(delimiter)) {
        statements.push(current.slice(0, -delimiter.length).trim());
        current = '';
      }
    }

    // Add any remaining
    if (current.trim()) {
      statements.push(current.trim());
    }

    return statements.filter(s => s.length > 0);
  }

  async rollback(version: string): Promise<void> {
    logger.warn(`Rollback requested for version ${version} - manual intervention required`);
    // Rollback would require down migrations which we don't have
    // In production, maintain down migration files
    throw new Error('Rollback not implemented - use manual database restore');
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