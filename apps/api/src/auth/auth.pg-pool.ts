import { Inject, Injectable, OnApplicationShutdown } from '@nestjs/common';
import { Pool, type PoolConfig } from 'pg';
import { parsePostgresConnectionString } from '../database/connection-config.js';
import { AUTH_CONFIG } from './auth.constants.js';
import { AuthConfig, AuthConfigError } from './auth.config.js';

export interface IPgClient {
  query<T = Record<string, unknown>>(
    sql: string,
    params?: unknown[],
  ): Promise<{ rows: T[]; rowCount: number | null }>;
}

export interface IPgPool {
  query<T = Record<string, unknown>>(
    sql: string,
    params?: unknown[],
  ): Promise<{ rows: T[]; rowCount: number | null }>;
  transaction<T>(fn: (client: IPgClient) => Promise<T>): Promise<T>;
  close?(): Promise<void>;
}

export class AuthPoolError extends Error {
  constructor(message: string) {
    super(message);
    this.name = 'AuthPoolError';
  }
}

/**
 * Forbidden command tokens that must never be accepted from repository SQL.
 *
 * The pool enforces a strict, non-permissive whitelist-like policy: one statement
 * per call, no semicolons, and no transaction-control or privileged commands.
 */
const FORBIDDEN_COMMAND_RE = new RegExp(
  String.raw`^\s*(?:BEGIN|COMMIT|ROLLBACK|SAVEPOINT|RELEASE|SET|RESET|DISCARD|COPY|DO)\b`,
  'is',
);

const EXPECTED_LOGIN_USER = 'lu_auth_login';
const EXPECTED_RUNTIME_ROLE = 'lu_auth_runtime';
const EXPECTED_SEARCH_PATH = 'pg_catalog, public';
const DEFAULT_POOL_MAX = 10;

function resolvePoolMax(raw: string | undefined): number {
  if (raw === undefined || raw.trim() === '') {
    return DEFAULT_POOL_MAX;
  }
  const value = raw.trim();
  if (!/^\d+$/.test(value)) {
    throw new AuthPoolError('AUTH_DB_POOL_MAX must be an integer between 1 and 10.');
  }
  const parsed = Number(value);
  if (!Number.isSafeInteger(parsed) || parsed < 1 || parsed > 10) {
    throw new AuthPoolError('AUTH_DB_POOL_MAX must be an integer between 1 and 10.');
  }
  return parsed;
}

@Injectable()
export class AuthPgPool implements IPgPool, OnApplicationShutdown {
  protected pool: Pool | null = null;

  constructor(@Inject(AUTH_CONFIG) private readonly config: AuthConfig) {}

  async query<T = Record<string, unknown>>(
    sql: string,
    params?: unknown[],
  ): Promise<{ rows: T[]; rowCount: number | null }> {
    this.assertSafeSql(sql);

    const pool = this.getPool();
    const client = await pool.connect();
    let destroyClient = false;

    try {
      await client.query('BEGIN');
      await client.query(`SET LOCAL ROLE ${EXPECTED_RUNTIME_ROLE}`);
      await client.query(`SET LOCAL search_path TO ${EXPECTED_SEARCH_PATH}`);

      const result = await client.query(sql, params);

      await client.query('COMMIT');
      return { rows: result.rows as unknown as T[], rowCount: result.rowCount };
    } catch (error) {
      try {
        await client.query('ROLLBACK');
      } catch {
        destroyClient = true;
      }
      throw error;
    } finally {
      client.release(destroyClient);
    }
  }

  async transaction<T>(fn: (client: IPgClient) => Promise<T>): Promise<T> {
    const pool = this.getPool();
    const client = await pool.connect();
    let destroyClient = false;

    try {
      await client.query('BEGIN');
      await client.query(`SET LOCAL ROLE ${EXPECTED_RUNTIME_ROLE}`);
      await client.query(`SET LOCAL search_path TO ${EXPECTED_SEARCH_PATH}`);

      const wrappedClient: IPgClient = {
        query: async <R = Record<string, unknown>>(
          sql: string,
          params?: unknown[],
        ): Promise<{ rows: R[]; rowCount: number | null }> => {
          this.assertSafeSql(sql);
          const result = await client.query(sql, params);
          return { rows: result.rows as unknown as R[], rowCount: result.rowCount };
        },
      };

      const result = await fn(wrappedClient);

      await client.query('COMMIT');
      return result;
    } catch (error) {
      try {
        await client.query('ROLLBACK');
      } catch {
        destroyClient = true;
      }
      throw error;
    } finally {
      client.release(destroyClient);
    }
  }

  async close(): Promise<void> {
    if (this.pool !== null) {
      await this.pool.end();
      this.pool = null;
    }
  }

  onApplicationShutdown(): Promise<void> {
    return this.close();
  }

  private getPool(): Pool {
    if (this.pool === null) {
      this.pool = this.createPool();
    }
    return this.pool;
  }

  /**
   * Rejects any SQL that could be used to escape the one-statement, restricted
   * transaction sandbox. Error messages are generic and never echo the SQL.
   */
  private assertSafeSql(sql: string): void {
    const trimmed = sql.trim();
    if (trimmed.length === 0 || trimmed.includes(';') || FORBIDDEN_COMMAND_RE.test(trimmed)) {
      throw new AuthPoolError('Invalid or forbidden SQL statement.');
    }
  }

  protected buildPoolConfig(): PoolConfig {
    const raw = process.env['ConnectionStrings__ControlPlaneRuntime'];
    if (raw === undefined || raw.trim() === '') {
      throw new AuthPoolError(
        'Missing required environment variable "ConnectionStrings__ControlPlaneRuntime".',
      );
    }
    let parsed;
    try {
      parsed = parsePostgresConnectionString(raw);
    } catch (error) {
      if (error instanceof AuthConfigError) {
        throw new AuthPoolError(error.message);
      }
      throw new AuthPoolError('Invalid Control Plane connection string.');
    }

    if (parsed.user !== EXPECTED_LOGIN_USER) {
      throw new AuthPoolError(
        `Control Plane database user must be exactly "${EXPECTED_LOGIN_USER}".`,
      );
    }

    return {
      host: parsed.host,
      port: parsed.port,
      database: parsed.database,
      user: parsed.user,
      password: parsed.password,
      ssl: parsed.ssl,
      max: resolvePoolMax(process.env['AUTH_DB_POOL_MAX']),
      connectionTimeoutMillis: 5_000,
      idleTimeoutMillis: 30_000,
    };
  }

  protected createPool(): Pool {
    const poolConfig = this.buildPoolConfig();
    const pool = new Pool(poolConfig);
    pool.on('error', () => {
      // Sanitized log without error details or secrets to avoid leaking
      // sensitive information through unhandled pool error emissions.
      console.error('AuthPgPool unexpected pool error.');
    });
    return pool;
  }
}
