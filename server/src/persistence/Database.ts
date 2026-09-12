import mysql from 'mysql2/promise';
import { config } from '../config';
import { logger } from '../utils/logger';

export class Database {
  private pool: mysql.Pool | null = null;

  async connect(): Promise<void> {
    const dbConfig: any = { ...config.database };
    if (dbConfig.ssl === true) {
      dbConfig.ssl = { rejectUnauthorized: false };
    }
    this.pool = mysql.createPool(dbConfig);
    
    // Test connection
    const conn = await this.pool.getConnection();
    conn.release();
    logger.info('Database pool created');
  }

  getPool(): mysql.Pool {
    if (!this.pool) throw new Error('Database not initialized');
    return this.pool;
  }

  async query<T = any>(sql: string, params?: any[]): Promise<T[]> {
    const [rows] = await this.getPool().execute(sql, params);
    return rows as T[];
  }

  async execute(sql: string, params?: any[]): Promise<any> {
    const [result] = await this.getPool().execute(sql, params);
    return result;
  }

  async transaction<T>(callback: (conn: mysql.PoolConnection) => Promise<T>): Promise<T> {
    const conn = await this.getPool().getConnection();
    await conn.beginTransaction();
    try {
      const result = await callback(conn);
      await conn.commit();
      return result;
    } catch (error) {
      await conn.rollback();
      throw error;
    } finally {
      conn.release();
    }
  }

  async close(): Promise<void> {
    if (this.pool) {
      await this.pool.end();
      this.pool = null;
      logger.info('Database pool closed');
    }
  }
}

export const db = new Database();