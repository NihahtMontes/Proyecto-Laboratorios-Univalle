import 'reflect-metadata';
import { Test } from '@nestjs/testing';
import type { PoolConfig } from 'pg';
import { AUTH_CONFIG } from '../src/auth/auth.constants.js';
import { AuthConfig } from '../src/auth/auth.config.js';
import { AuthPgPool, AuthPoolError } from '../src/auth/auth.pg-pool.js';

const VALID_CONNECTION_STRING =
  'Host=localhost;Port=5432;Database=lu;Username=lu_auth_login;Password=secret';

class FakePoolClient {
  readonly queries: { sql: string; params: unknown[] }[] = [];
  releasedWith: boolean | undefined;

  constructor(
    private readonly options: {
      readonly rowSource?: (sql: string) => unknown[] | undefined;
      readonly throwOn?: readonly string[];
    } = {},
  ) {}

  async query<T = Record<string, unknown>>(
    sql: string,
    params?: unknown[],
  ): Promise<{ rows: T[]; rowCount: number }> {
    this.queries.push({ sql, params: params ?? [] });

    if (this.options.throwOn?.includes(sql)) {
      throw new Error(`forced failure: ${sql}`);
    }

    const rows = this.options.rowSource?.(sql) ?? [];
    return { rows: rows as T[], rowCount: rows.length };
  }

  release(destroy?: boolean): void {
    this.releasedWith = destroy;
  }
}

class FakePool {
  readonly config: PoolConfig;
  readonly clients: FakePoolClient[] = [];
  private readonly eventListeners = new Map<string, Array<(...args: unknown[]) => void>>();
  private nextClientOptions?: ConstructorParameters<typeof FakePoolClient>[0];

  constructor(config: PoolConfig) {
    this.config = config;
  }

  setNextClientOptions(options: ConstructorParameters<typeof FakePoolClient>[0]): void {
    this.nextClientOptions = options;
  }

  on(event: string, handler: (...args: unknown[]) => void): this {
    if (!this.eventListeners.has(event)) {
      this.eventListeners.set(event, []);
    }
    this.eventListeners.get(event)!.push(handler);
    return this;
  }

  listeners(event: string): Array<(...args: unknown[]) => void> {
    return this.eventListeners.get(event) ?? [];
  }

  async connect(): Promise<FakePoolClient> {
    const client = new FakePoolClient(this.nextClientOptions);
    this.nextClientOptions = undefined;
    this.clients.push(client);
    return client;
  }

  async end(): Promise<void> {
    // no-op for tests
  }
}

class TestableAuthPgPool extends AuthPgPool {
  createdPools: FakePool[] = [];
  nextClientOptions?: ConstructorParameters<typeof FakePoolClient>[0];

  protected override createPool(): import('pg').Pool {
    const config = this.buildPoolConfig();
    const pool = new FakePool(config);
    if (this.nextClientOptions !== undefined) {
      pool.setNextClientOptions(this.nextClientOptions);
    }
    this.nextClientOptions = undefined;
    pool.on('error', () => {
      console.error('AuthPgPool unexpected pool error.');
    });
    this.createdPools.push(pool);
    return pool as unknown as import('pg').Pool;
  }

  usePool(pool: FakePool): void {
    this.pool = pool as unknown as import('pg').Pool;
  }

  get createdPool(): FakePool | undefined {
    return this.createdPools[0];
  }
}

describe('AuthPgPool', () => {
  const originalEnv = process.env;

  afterEach(() => {
    process.env = originalEnv;
  });

  it('rejects missing connection string', async () => {
    process.env = { ...originalEnv };
    delete process.env['ConnectionStrings__ControlPlaneRuntime'];

    const moduleRef = await Test.createTestingModule({
      providers: [AuthPgPool, { provide: AUTH_CONFIG, useValue: new AuthConfig() }],
    }).compile();

    const pool = moduleRef.get(AuthPgPool);
    await expect(pool.query('SELECT 1')).rejects.toThrow(AuthPoolError);
    await expect(pool.query('SELECT 1')).rejects.toThrow('Missing required environment variable');
  });

  it('rejects empty username', async () => {
    process.env = {
      ...originalEnv,
      ConnectionStrings__ControlPlaneRuntime:
        'Host=localhost;Port=5432;Database=lu;Username=;Password=secret',
    };

    const moduleRef = await Test.createTestingModule({
      providers: [AuthPgPool, { provide: AUTH_CONFIG, useValue: new AuthConfig() }],
    }).compile();

    const pool = moduleRef.get(AuthPgPool);
    await expect(pool.query('SELECT 1')).rejects.toThrow(AuthPoolError);
  });

  it('rejects the reserved postgres user', async () => {
    process.env = {
      ...originalEnv,
      ConnectionStrings__ControlPlaneRuntime:
        'Host=localhost;Port=5432;Database=lu;Username=postgres;Password=secret',
    };

    const moduleRef = await Test.createTestingModule({
      providers: [AuthPgPool, { provide: AUTH_CONFIG, useValue: new AuthConfig() }],
    }).compile();

    const pool = moduleRef.get(AuthPgPool);
    await expect(pool.query('SELECT 1')).rejects.toThrow(AuthPoolError);
    await expect(pool.query('SELECT 1')).rejects.toThrow('must be exactly "lu_auth_login"');
  });

  it('rejects app_user', async () => {
    process.env = {
      ...originalEnv,
      ConnectionStrings__ControlPlaneRuntime:
        'Host=localhost;Port=5432;Database=lu;Username=app_user;Password=secret',
    };

    const moduleRef = await Test.createTestingModule({
      providers: [AuthPgPool, { provide: AUTH_CONFIG, useValue: new AuthConfig() }],
    }).compile();

    const pool = moduleRef.get(AuthPgPool);
    await expect(pool.query('SELECT 1')).rejects.toThrow(AuthPoolError);
    await expect(pool.query('SELECT 1')).rejects.toThrow('must be exactly "lu_auth_login"');
  });

  it('rejects case variants of the login user', async () => {
    process.env = {
      ...originalEnv,
      ConnectionStrings__ControlPlaneRuntime:
        'Host=localhost;Port=5432;Database=lu;Username=Lu_Auth_Login;Password=secret',
    };

    const moduleRef = await Test.createTestingModule({
      providers: [AuthPgPool, { provide: AUTH_CONFIG, useValue: new AuthConfig() }],
    }).compile();

    const pool = moduleRef.get(AuthPgPool);
    await expect(pool.query('SELECT 1')).rejects.toThrow(AuthPoolError);
    await expect(pool.query('SELECT 1')).rejects.toThrow('must be exactly "lu_auth_login"');
  });

  it('accepts the exact login user after normal parser trimming', async () => {
    process.env = {
      ...originalEnv,
      ConnectionStrings__ControlPlaneRuntime:
        'Host=localhost;Port=5432;Database=lu;Username=  lu_auth_login  ;Password=secret',
    };

    const pool = new TestableAuthPgPool(new AuthConfig());
    await pool.query('SELECT 1').catch(() => {
      // The fake pool does not implement query, so this is expected.
    });

    expect(pool.createdPool).toBeDefined();
    expect(pool.createdPool!.config.user).toBe('lu_auth_login');
  });

  it('configures max connections and TLS', async () => {
    process.env = {
      ...originalEnv,
      ConnectionStrings__ControlPlaneRuntime:
        'Host=db.example.com;Port=5432;Database=lu;Username=lu_auth_login;Password=secret;SSL Mode=Prefer',
    };

    const pool = new TestableAuthPgPool(new AuthConfig());
    await pool.query('SELECT 1').catch(() => {
      // Expected: the fake pool does not implement query.
    });

    const createdPool = pool.createdPool;
    expect(createdPool).toBeDefined();
    expect(createdPool!.config.max).toBe(10);
    expect(createdPool!.config.ssl).not.toBe(false);
    expect(createdPool!.config.connectionTimeoutMillis).toBe(5_000);
    expect(createdPool!.config.idleTimeoutMillis).toBe(30_000);
  });

  it('uses a bounded pool size suitable for serverless runtimes', async () => {
    process.env = {
      ...originalEnv,
      AUTH_DB_POOL_MAX: '2',
      ConnectionStrings__ControlPlaneRuntime: VALID_CONNECTION_STRING,
    };

    const pool = new TestableAuthPgPool(new AuthConfig());
    await pool.query('SELECT 1');

    expect(pool.createdPool!.config.max).toBe(2);
  });

  it.each(['0', '11', '2.5', 'many'])('rejects invalid pool maximum %s', async (poolMax) => {
    process.env = {
      ...originalEnv,
      AUTH_DB_POOL_MAX: poolMax,
      ConnectionStrings__ControlPlaneRuntime: VALID_CONNECTION_STRING,
    };

    const pool = new TestableAuthPgPool(new AuthConfig());

    await expect(pool.query('SELECT 1')).rejects.toThrow(
      'AUTH_DB_POOL_MAX must be an integer between 1 and 10.',
    );
  });

  it('does not create a pool eagerly', async () => {
    process.env = {
      ...originalEnv,
      ConnectionStrings__ControlPlaneRuntime: VALID_CONNECTION_STRING,
    };

    const pool = new TestableAuthPgPool(new AuthConfig());
    expect(pool.createdPool).toBeUndefined();
  });

  it('registers a sanitized error listener without exposing secrets', async () => {
    process.env = {
      ...originalEnv,
      ConnectionStrings__ControlPlaneRuntime: VALID_CONNECTION_STRING,
    };

    const pool = new TestableAuthPgPool(new AuthConfig());
    await pool.query('SELECT 1').catch(() => {
      // Expected.
    });

    const createdPool = pool.createdPool;
    expect(createdPool).toBeDefined();
    const errorListeners = createdPool!.listeners('error');
    expect(errorListeners).toHaveLength(1);

    const originalConsoleError = console.error;
    const calls: unknown[] = [];
    console.error = (...args: unknown[]): void => {
      calls.push(args);
    };
    try {
      errorListeners[0]!(new Error('secret password leak'));
      expect(calls).toEqual([['AuthPgPool unexpected pool error.']]);
    } finally {
      console.error = originalConsoleError;
    }
  });

  it('closes the pool on application shutdown if it was created', async () => {
    process.env = {
      ...originalEnv,
      ConnectionStrings__ControlPlaneRuntime: VALID_CONNECTION_STRING,
    };

    const pool = new TestableAuthPgPool(new AuthConfig());
    await pool.query('SELECT 1').catch(() => {
      // Expected.
    });

    const createdPool = pool.createdPool;
    expect(createdPool).toBeDefined();
    expect(createdPool!.clients).toHaveLength(1);

    await pool.onApplicationShutdown();

    expect(pool.createdPool).toBeDefined();
  });

  it('close is safe when pool was never created', async () => {
    process.env = {
      ...originalEnv,
      ConnectionStrings__ControlPlaneRuntime: VALID_CONNECTION_STRING,
    };

    const pool = new TestableAuthPgPool(new AuthConfig());
    await expect(pool.close()).resolves.toBeUndefined();
    expect(pool.createdPool).toBeUndefined();
  });

  it('one-shot query acquires a client and runs exactly one statement inside a prepared transaction', async () => {
    process.env = {
      ...originalEnv,
      ConnectionStrings__ControlPlaneRuntime: VALID_CONNECTION_STRING,
    };

    const pool = new TestableAuthPgPool(new AuthConfig());
    pool.nextClientOptions = {
      rowSource: (sql) => (sql === 'SELECT 1' ? [{ one: 1 }] : []),
    };

    const result = await pool.query<{ one: number }>('SELECT 1');

    const fakePool = pool.createdPool!;
    expect(fakePool.clients).toHaveLength(1);
    const client = fakePool.clients[0]!;
    expect(client.releasedWith).toBe(false);
    expect(client.queries.map((q) => q.sql)).toEqual([
      'BEGIN',
      'SET LOCAL ROLE lu_auth_runtime',
      'SET LOCAL search_path TO pg_catalog, public',
      'SELECT 1',
      'COMMIT',
    ]);
    expect(result.rows).toEqual([{ one: 1 }]);
  });

  it('transaction prepares role and search_path before the callback and commits', async () => {
    process.env = {
      ...originalEnv,
      ConnectionStrings__ControlPlaneRuntime: VALID_CONNECTION_STRING,
    };

    const pool = new TestableAuthPgPool(new AuthConfig());
    pool.nextClientOptions = {
      rowSource: (sql) => (sql === 'SELECT 2' ? [{ two: 2 }] : []),
    };

    const value = await pool.transaction(async (client) => {
      const res = await client.query<{ two: number }>('SELECT 2');
      return res.rows[0]!.two;
    });

    expect(value).toBe(2);
    const fakePool = pool.createdPool!;
    const client = fakePool.clients[0]!;
    expect(client.releasedWith).toBe(false);
    expect(client.queries.map((q) => q.sql)).toEqual([
      'BEGIN',
      'SET LOCAL ROLE lu_auth_runtime',
      'SET LOCAL search_path TO pg_catalog, public',
      'SELECT 2',
      'COMMIT',
    ]);
  });

  it('validates identity indirectly through the exact connection user, not an extra session_user query', async () => {
    process.env = {
      ...originalEnv,
      ConnectionStrings__ControlPlaneRuntime: VALID_CONNECTION_STRING,
    };

    const pool = new TestableAuthPgPool(new AuthConfig());

    await pool.query('SELECT 1');

    const fakePool = pool.createdPool!;
    const client = fakePool.clients[0]!;
    const hasSessionUserQuery = client.queries.some((q) =>
      q.sql.toLowerCase().includes('session_user'),
    );
    expect(hasSessionUserQuery).toBe(false);
  });

  it('rolls back and releases the client on one-shot error', async () => {
    process.env = {
      ...originalEnv,
      ConnectionStrings__ControlPlaneRuntime: VALID_CONNECTION_STRING,
    };

    const pool = new TestableAuthPgPool(new AuthConfig());
    pool.nextClientOptions = { throwOn: ['SELECT 1'] };

    await expect(pool.query('SELECT 1')).rejects.toThrow('forced failure: SELECT 1');

    const fakePool = pool.createdPool!;
    const client = fakePool.clients[0]!;
    expect(client.releasedWith).toBe(false);
    expect(client.queries[client.queries.length - 1]!.sql).toBe('ROLLBACK');
  });

  it('destroys the client and preserves the original error when rollback fails', async () => {
    process.env = {
      ...originalEnv,
      ConnectionStrings__ControlPlaneRuntime: VALID_CONNECTION_STRING,
    };

    const pool = new TestableAuthPgPool(new AuthConfig());
    pool.nextClientOptions = { throwOn: ['ROLLBACK'] };

    const originalError = new Error('business failure');
    await expect(
      pool.transaction(async () => {
        throw originalError;
      }),
    ).rejects.toBe(originalError);

    const fakePool = pool.createdPool!;
    const client = fakePool.clients[0]!;
    expect(client.releasedWith).toBe(true);
    expect(client.queries.some((q) => q.sql === 'ROLLBACK')).toBe(true);
  });

  it('rejects multi-statement SQL containing a semicolon', async () => {
    process.env = {
      ...originalEnv,
      ConnectionStrings__ControlPlaneRuntime: VALID_CONNECTION_STRING,
    };

    const pool = new TestableAuthPgPool(new AuthConfig());

    await expect(pool.query('SELECT 1; SELECT 2')).rejects.toThrow(AuthPoolError);
    await expect(pool.query('SELECT 1; SELECT 2')).rejects.toThrow(
      'Invalid or forbidden SQL statement.',
    );
    expect(pool.createdPool).toBeUndefined();
  });

  it('rejects transactional control commands in repository SQL', async () => {
    process.env = {
      ...originalEnv,
      ConnectionStrings__ControlPlaneRuntime: VALID_CONNECTION_STRING,
    };

    const pool = new TestableAuthPgPool(new AuthConfig());

    await expect(pool.query('BEGIN')).rejects.toThrow(AuthPoolError);
    await expect(pool.query('COMMIT')).rejects.toThrow(AuthPoolError);
    await expect(pool.query('ROLLBACK')).rejects.toThrow(AuthPoolError);
    await expect(pool.query('SAVEPOINT x')).rejects.toThrow(AuthPoolError);
  });

  it('rejects privileged commands in repository SQL', async () => {
    process.env = {
      ...originalEnv,
      ConnectionStrings__ControlPlaneRuntime: VALID_CONNECTION_STRING,
    };

    const pool = new TestableAuthPgPool(new AuthConfig());

    await expect(pool.query('SET ROLE postgres')).rejects.toThrow(AuthPoolError);
    await expect(pool.query('SET SESSION AUTHORIZATION postgres')).rejects.toThrow(AuthPoolError);
    await expect(pool.query('RESET ROLE')).rejects.toThrow(AuthPoolError);
    await expect(pool.query('SET search_path TO public')).rejects.toThrow(AuthPoolError);
    await expect(pool.query('DISCARD ALL')).rejects.toThrow(AuthPoolError);
    await expect(pool.query('COPY lu_user TO STDOUT')).rejects.toThrow(AuthPoolError);
    await expect(pool.query('DO $$ BEGIN END $$')).rejects.toThrow(AuthPoolError);
  });

  it('does not confuse an UPDATE SET clause with a leading SET command', async () => {
    process.env = {
      ...originalEnv,
      ConnectionStrings__ControlPlaneRuntime: VALID_CONNECTION_STRING,
    };

    const pool = new TestableAuthPgPool(new AuthConfig());
    await expect(
      pool.query('UPDATE public.lu_session SET last_seen_at = $1 WHERE id = $2', [
        new Date(),
        '00000000-0000-4000-8000-000000000000',
      ]),
    ).resolves.toEqual({ rows: [], rowCount: 0 });
  });

  it('transaction wrapper rejects forbidden SQL from the callback', async () => {
    process.env = {
      ...originalEnv,
      ConnectionStrings__ControlPlaneRuntime: VALID_CONNECTION_STRING,
    };

    const pool = new TestableAuthPgPool(new AuthConfig());

    await expect(
      pool.transaction(async (client) => {
        await client.query('ROLLBACK');
      }),
    ).rejects.toThrow(AuthPoolError);

    const client = pool.createdPool!.clients[0]!;
    expect(client.queries[0]!.sql).toBe('BEGIN');
    expect(client.queries[client.queries.length - 1]!.sql).toBe('ROLLBACK');
  });

  it('does not execute any query after COMMIT in a one-shot query', async () => {
    process.env = {
      ...originalEnv,
      ConnectionStrings__ControlPlaneRuntime: VALID_CONNECTION_STRING,
    };

    const pool = new TestableAuthPgPool(new AuthConfig());
    pool.nextClientOptions = {
      rowSource: (sql) => (sql === 'SELECT 1' ? [{ one: 1 }] : []),
    };

    await pool.query('SELECT 1');

    const fakePool = pool.createdPool!;
    const client = fakePool.clients[0]!;
    expect(client.queries).toHaveLength(5);
    expect(client.queries[4]!.sql).toBe('COMMIT');
  });
});
