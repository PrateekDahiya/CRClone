import { Database, db } from '../persistence/Database';
import { logger } from '../utils/logger';

export abstract class BaseRepository {
  protected db: Database;

  constructor(database: Database = db) {
    this.db = database;
  }

  protected async query<T>(sql: string, params?: any[]): Promise<T[]> {
    try {
      return await this.db.query(sql, params);
    } catch (error) {
      const msg = error instanceof Error ? error.message : String(error);
      logger.error('Database query error', { sql: sql.substring(0, 200), params, error: msg });
      throw error;
    }
  }

  protected async execute(sql: string, params?: any[]): Promise<any> {
    try {
      return await this.db.execute(sql, params);
    } catch (error) {
      const msg = error instanceof Error ? error.message : String(error);
      logger.error('Database execute error', { sql: sql.substring(0, 200), params, error: msg });
      throw error;
    }
  }

  protected async transaction<T>(callback: (conn: any) => Promise<T>): Promise<T> {
    return await this.db.transaction(callback);
  }

  protected buildWhereClause(conditions: Record<string, any>): { where: string; params: any[] } {
    const clauses: string[] = [];
    const params: any[] = [];

    for (const [key, value] of Object.entries(conditions)) {
      if (value === null || value === undefined) continue;
      
      if (Array.isArray(value)) {
        if (value.length > 0) {
          clauses.push(`\`${key}\` IN (${value.map(() => '?').join(',')})`);
          params.push(...value);
        }
      } else {
        clauses.push(`\`${key}\` = ?`);
        params.push(value);
      }
    }

    return {
      where: clauses.length > 0 ? 'WHERE ' + clauses.join(' AND ') : '',
      params
    };
  }

  protected buildUpdateClause(data: Record<string, any>): { set: string; params: any[] } {
    const clauses: string[] = [];
    const params: any[] = [];

    for (const [key, value] of Object.entries(data)) {
      if (value === undefined) continue;
      clauses.push(`\`${key}\` = ?`);
      params.push(value);
    }

    return {
      set: clauses.join(', '),
      params
    };
  }
}