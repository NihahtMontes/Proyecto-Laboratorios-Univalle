import { Inject, Injectable, OnApplicationShutdown } from '@nestjs/common';
import { Pool, type PoolConfig } from 'pg';
import { parsePostgresConnectionString } from '../database/connection-config.js';
import type { IPgClient, IPgPool } from '../auth/auth.pg-pool.js';
import type { ResolvedTenantRoute } from './tenant-routing.js';

export const TENANT_CONNECTION_RESOLVER = Symbol('TENANT_CONNECTION_RESOLVER');

export interface TenantConnectionResolver {
  resolve(runtimeSecretReference: string): string | null;
}

/**
 * Local adapter for the deployment secret store. Production deployments should
 * replace this provider with their secret-manager adapter. The reference itself
 * is never accepted from the browser and is only read after TenantRouter has
 * validated it against the control-plane route.
 */
@Injectable()
export class EnvironmentTenantConnectionResolver implements TenantConnectionResolver {
  resolve(runtimeSecretReference: string): string | null {
    const suffix = runtimeSecretReference.replace(/[^A-Za-z0-9]/g, '_').toUpperCase();
    const value = process.env[`TENANT_CONNECTION__${suffix}`];
    return value === undefined || value.trim() === '' ? null : value;
  }
}

export class TenantPoolError extends Error {
  constructor(message: string) {
    super(message);
    this.name = 'TenantPoolError';
  }
}

const FORBIDDEN_COMMAND_RE = new RegExp(
  String.raw`^\s*(?:BEGIN|COMMIT|ROLLBACK|SAVEPOINT|RELEASE|SET|RESET|DISCARD|COPY|DO)\b`,
  'is',
);
const MAX_POOL_SIZE = 10;

function poolMax(): number {
  const raw = process.env['TENANT_DB_POOL_MAX'];
  if (raw === undefined || raw.trim() === '') return MAX_POOL_SIZE;
  const parsed = Number(raw);
  if (!Number.isInteger(parsed) || parsed < 1) {
    throw new TenantPoolError('Tenant pool size must be an integer between 1 and 10.');
  }
  return Math.min(parsed, MAX_POOL_SIZE);
}

function runtimeRole(): string {
  const value = process.env['TENANT_DB_RUNTIME_ROLE']?.trim() || 'lu_auth_runtime';
  if (!/^[a-z_][a-z0-9_]{0,62}$/i.test(value)) {
    throw new TenantPoolError('Tenant runtime role is invalid.');
  }
  return value;
}

function assertSafeSql(sql: string): void {
  const trimmed = sql.trim();
  if (trimmed.length === 0 || trimmed.includes(';') || FORBIDDEN_COMMAND_RE.test(trimmed)) {
    throw new TenantPoolError('Invalid or forbidden tenant SQL statement.');
  }
}

class TenantPool implements IPgPool {
  constructor(private readonly pool: Pool) {}

  async query<T = Record<string, unknown>>(
    sql: string,
    params?: unknown[],
  ): Promise<{ rows: T[]; rowCount: number | null }> {
    assertSafeSql(sql);
    const client = await this.pool.connect();
    let destroyClient = false;
    try {
      await client.query('BEGIN');
      await client.query(`SET LOCAL ROLE ${runtimeRole()}`);
      await client.query('SET LOCAL search_path TO pg_catalog, public');
      const result = await client.query(sql, params);
      await client.query('COMMIT');
      return { rows: result.rows as T[], rowCount: result.rowCount };
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
    const client = await this.pool.connect();
    let destroyClient = false;
    try {
      await client.query('BEGIN');
      await client.query(`SET LOCAL ROLE ${runtimeRole()}`);
      await client.query('SET LOCAL search_path TO pg_catalog, public');
      const wrapped: IPgClient = {
        query: async <R = Record<string, unknown>>(
          sql: string,
          params?: unknown[],
        ): Promise<{ rows: R[]; rowCount: number | null }> => {
          assertSafeSql(sql);
          const result = await client.query(sql, params);
          return { rows: result.rows as R[], rowCount: result.rowCount };
        },
      };
      const value = await fn(wrapped);
      await client.query('COMMIT');
      return value;
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

  close(): Promise<void> {
    return this.pool.end();
  }
}

@Injectable()
export class TenantPgPoolRegistry implements OnApplicationShutdown {
  private readonly pools = new Map<string, TenantPool>();

  constructor(
    @Inject(TENANT_CONNECTION_RESOLVER)
    private readonly resolver: TenantConnectionResolver,
  ) {}

  get(route: ResolvedTenantRoute): IPgPool {
    const existing = this.pools.get(route.runtimeSecretReference);
    if (existing !== undefined) return existing;

    const rawConnection = this.resolver.resolve(route.runtimeSecretReference);
    if (rawConnection === null) {
      throw new TenantPoolError('Tenant database connection is not configured.');
    }
    const config = this.parsePoolConfig(rawConnection);
    const pool = new TenantPool(new Pool(config));
    this.pools.set(route.runtimeSecretReference, pool);
    return pool;
  }

  async onApplicationShutdown(): Promise<void> {
    await Promise.all([...this.pools.values()].map((pool) => pool.close()));
    this.pools.clear();
  }

  private parsePoolConfig(rawConnection: string): PoolConfig {
    let parsed;
    try {
      parsed = parsePostgresConnectionString(rawConnection);
    } catch {
      throw new TenantPoolError('Tenant database connection is invalid.');
    }
    if (parsed.user.toLowerCase() === 'postgres') {
      throw new TenantPoolError('Tenant runtime must not use the postgres superuser.');
    }
    return {
      ...parsed,
      max: poolMax(),
      connectionTimeoutMillis: 5_000,
      idleTimeoutMillis: 30_000,
    };
  }
}
