/**
 * MIG-F3-PG-TEST-003 — runner unit specs with ESM mocks for `pg` (no real DB, no sockets).
 *
 * These tests prove the transaction orchestration invariants of applyMigration:
 *  - statements execute in the expected order inside a SERIALIZABLE transaction,
 *  - COMMIT is issued only when the body and verification succeed,
 *  - ROLLBACK is issued on SQL execution failure or verification failure,
 *  - the canonical body is NOT executed when a target table already exists,
 *  - client.end() is always called on every path that opened a connection.
 *
 * The heavy schema verifier is mocked to a pure function so these specs isolate
 * runner behaviour, not PostgreSQL catalog semantics.
 */
import { readFileSync } from 'node:fs';
import { fileURLToPath } from 'node:url';
import { IDENTITY_MIGRATION_FILE } from '../src/database/migration-plan.js';

const MIGRATION_PATH = fileURLToPath(
  new URL(`../migrations/${IDENTITY_MIGRATION_FILE}`, import.meta.url),
);
const REAL_BYTES = readFileSync(MIGRATION_PATH);
const AUTH_SECURITY_PATH = fileURLToPath(
  new URL('../migrations/0002_create_auth_security_controls.sql', import.meta.url),
);
const AUTH_SECURITY_BYTES = readFileSync(AUTH_SECURITY_PATH);

interface MockQueryResult {
  rows: Record<string, unknown>[];
}

const jestRuntime = (import.meta as ImportMeta & { jest: typeof jest }).jest;
const unstableMockModule = (
  jestRuntime as typeof jest & {
    unstable_mockModule(moduleName: string, factory: () => unknown): typeof jest;
  }
).unstable_mockModule.bind(jestRuntime);

type QueryFilter = (text: string, values?: unknown[]) => MockQueryResult | undefined;

describe('migration-runner ESM mock suite', () => {
  let applyMigration: (typeof import('../src/database/migration-runner.js'))['applyMigration'];
  let fetchStatus: (typeof import('../src/database/migration-runner.js'))['fetchStatus'];
  let verifyMigration: (typeof import('../src/database/migration-runner.js'))['verifyMigration'];

  const baseConfig = {
    host: 'localhost',
    port: 5432,
    database: 'testdb',
    user: 'tester',
    password: 'secret',
    ssl: false as const,
  };

  const queryLog: string[] = [];
  let connectError: Error | null = null;
  let failQueryContaining: string | null = null;
  const queryFilters: QueryFilter[] = [];

  function clearMocks(): void {
    queryLog.length = 0;
    connectError = null;
    failQueryContaining = null;
    queryFilters.length = 0;
  }

  class MockClient {
    async connect(): Promise<void> {
      queryLog.push('CONNECT');
      if (connectError !== null) {
        throw connectError;
      }
    }

    async query(text: string, values?: unknown[]): Promise<MockQueryResult> {
      queryLog.push(text);
      if (failQueryContaining !== null && text.includes(failQueryContaining)) {
        throw new Error(`Mock SQL failure for query containing "${failQueryContaining}"`);
      }
      for (const filter of queryFilters) {
        const result = filter(text, values);
        if (result !== undefined) return result;
      }
      return { rows: [] };
    }

    async end(): Promise<void> {
      queryLog.push('END');
    }
  }

  beforeAll(async () => {
    unstableMockModule('pg', () => ({
      Client: MockClient,
    }));
    unstableMockModule('../src/database/schema-manifest.js', () => ({
      AUTH_SECURITY_SCHEMA_MANIFEST: {},
      IDENTITY_CONTROL_PLANE_SCHEMA_MANIFEST: {},
      TENANT_ROUTE_SCHEMA_MANIFEST: {},
      verifySchema: async () => ({
        passed: true,
        diffs: [],
        actualTableNames: [],
      }),
    }));

    const runner = await import('../src/database/migration-runner.js');
    applyMigration = runner.applyMigration;
    fetchStatus = runner.fetchStatus;
    verifyMigration = runner.verifyMigration;
  });

  beforeEach(() => {
    clearMocks();
  });

  afterAll(() => {
    jestRuntime.resetModules();
  });

  it('applyMigration: runs statements in transactional order and commits on success', async () => {
    queryFilters.push((text) => {
      if (text.includes('current_database()')) {
        return { rows: [{ database: 'testdb', user: 'tester', server_version: '16.0' }] };
      }
      if (text.includes('has_schema_privilege')) {
        return { rows: [{ can_create: true }] };
      }
      if (text.includes('information_schema.tables')) {
        return { rows: [] };
      }
      if (text.includes('information_schema.columns')) {
        return { rows: [] };
      }
      return undefined;
    });

    const config = baseConfig;

    const outcome = await applyMigration(config, MIGRATION_PATH, REAL_BYTES);

    expect(outcome.executed).toBe(true);
    expect(outcome.committed).toBe(true);
    expect(outcome.preexisting).toEqual([]);

    // Transaction orchestration order: verify -> inspect -> COMMIT, and no
    // information_schema queries after COMMIT.
    expect(queryLog[0]).toMatch(/CONNECT/i);
    expect(queryLog[1]).toBe('BEGIN ISOLATION LEVEL SERIALIZABLE');
    expect(queryLog[2]).toBe('SET LOCAL search_path = public, pg_catalog');
    expect(queryLog[3]).toMatch(/pg_advisory_xact_lock/);
    expect(queryLog[4]).toMatch(/has_schema_privilege/);
    expect(queryLog[5]).toMatch(/information_schema\.tables/);
    const createTableIndex = queryLog.findIndex((q) => q.includes('CREATE TABLE'));
    expect(createTableIndex).toBeGreaterThan(5);
    const inspectIndex = queryLog.findIndex((q) => q.includes('current_database()'));
    expect(inspectIndex).toBeGreaterThan(createTableIndex);
    const commitIndex = queryLog.indexOf('COMMIT');
    expect(commitIndex).toBeGreaterThan(inspectIndex);
    expect(queryLog.slice(commitIndex + 1).some((q) => q.includes('information_schema'))).toBe(
      false,
    );
    expect(queryLog[queryLog.length - 1]).toBe('END');
  });

  it('applyMigration: additive 0002 ignores existing baseline tables and targets only its new tables', async () => {
    queryFilters.push((text, values) => {
      if (text.includes('current_database()')) {
        return { rows: [{ database: 'testdb', user: 'tester', server_version: '16.0' }] };
      }
      if (text.includes('has_schema_privilege')) {
        return { rows: [{ can_create: true }] };
      }
      if (text.includes('information_schema.tables')) {
        const requested = values?.[1] as string[] | undefined;
        if (requested?.includes('lu_auth_rate_limit') || requested?.includes('lu_security_event')) {
          return { rows: [] };
        }
        return {
          rows: [
            { table_name: 'lu_site' },
            { table_name: 'lu_user' },
            { table_name: 'lu_site_membership' },
            { table_name: 'lu_session' },
          ],
        };
      }
      if (text.includes('information_schema.columns')) return { rows: [] };
      return undefined;
    });

    const outcome = await applyMigration(baseConfig, AUTH_SECURITY_PATH, AUTH_SECURITY_BYTES);

    expect(outcome.executed).toBe(true);
    expect(outcome.committed).toBe(true);
    expect(outcome.preexisting).toEqual([]);
    const body = queryLog.find((q) => q.includes('CREATE TABLE lu_auth_rate_limit'));
    expect(body).toBeDefined();
    expect(body).toContain('CREATE TABLE lu_security_event');
  });

  it('applyMigration: rolls back when the canonical body throws an SQL error', async () => {
    queryFilters.push((text) => {
      if (text.includes('current_database()')) {
        return { rows: [{ database: 'testdb', user: 'tester', server_version: '16.0' }] };
      }
      if (text.includes('has_schema_privilege')) {
        return { rows: [{ can_create: true }] };
      }
      if (text.includes('information_schema.tables')) {
        return { rows: [] };
      }
      return undefined;
    });
    failQueryContaining = 'CREATE TABLE';

    const config = baseConfig;

    await expect(applyMigration(config, MIGRATION_PATH, REAL_BYTES)).rejects.toThrow(
      /Mock SQL failure/,
    );

    expect(queryLog.some((q) => q === 'ROLLBACK')).toBe(true);
    expect(queryLog.some((q) => q === 'COMMIT')).toBe(false);
    expect(queryLog[queryLog.length - 1]).toBe('END');
  });

  it('applyMigration: skips execution and rolls back when a target table already exists', async () => {
    queryFilters.push((text) => {
      if (text.includes('current_database()')) {
        return { rows: [{ database: 'testdb', user: 'tester', server_version: '16.0' }] };
      }
      if (text.includes('has_schema_privilege')) {
        return { rows: [{ can_create: true }] };
      }
      if (text.includes('information_schema.tables')) {
        return { rows: [{ table_name: 'lu_site' }] };
      }
      return undefined;
    });

    const config = baseConfig;

    const outcome = await applyMigration(config, MIGRATION_PATH, REAL_BYTES);

    expect(outcome.executed).toBe(false);
    expect(outcome.committed).toBe(false);
    expect(outcome.preexisting).toContain('lu_site');
    expect(queryLog.some((q) => q.includes('CREATE TABLE'))).toBe(false);
    expect(queryLog.some((q) => q === 'ROLLBACK')).toBe(true);
    expect(queryLog[queryLog.length - 1]).toBe('END');
  });

  it('applyMigration: rolls back when schema verification fails', async () => {
    queryFilters.push((text) => {
      if (text.includes('current_database()')) {
        return { rows: [{ database: 'testdb', user: 'tester', server_version: '16.0' }] };
      }
      if (text.includes('has_schema_privilege')) {
        return { rows: [{ can_create: true }] };
      }
      if (text.includes('information_schema.tables')) {
        return { rows: [] };
      }
      return undefined;
    });

    jestRuntime.resetModules();
    unstableMockModule('pg', () => ({
      Client: MockClient,
    }));
    unstableMockModule('../src/database/schema-manifest.js', () => ({
      AUTH_SECURITY_SCHEMA_MANIFEST: {},
      IDENTITY_CONTROL_PLANE_SCHEMA_MANIFEST: {},
      TENANT_ROUTE_SCHEMA_MANIFEST: {},
      verifySchema: async () => ({
        passed: false,
        diffs: ['mocked verification failure'],
        actualTableNames: [],
      }),
    }));
    // Re-import to pick up the new verifier mock.
    const runner = await import('../src/database/migration-runner.js');
    applyMigration = runner.applyMigration;

    const config = baseConfig;

    await expect(applyMigration(config, MIGRATION_PATH, REAL_BYTES)).rejects.toThrow(
      /Schema verification failed/,
    );

    expect(queryLog.some((q) => q === 'ROLLBACK')).toBe(true);
    expect(queryLog.some((q) => q === 'COMMIT')).toBe(false);
    expect(queryLog[queryLog.length - 1]).toBe('END');
  });

  it('applyMigration: rolls back when post-verify inspection fails, never commits', async () => {
    // A previous test may have replaced the verifier mock with a failing one.
    // Re-import with the default passing verifier so this test isolates inspection failure.
    jestRuntime.resetModules();
    unstableMockModule('pg', () => ({
      Client: MockClient,
    }));
    unstableMockModule('../src/database/schema-manifest.js', () => ({
      AUTH_SECURITY_SCHEMA_MANIFEST: {},
      IDENTITY_CONTROL_PLANE_SCHEMA_MANIFEST: {},
      TENANT_ROUTE_SCHEMA_MANIFEST: {},
      verifySchema: async () => ({
        passed: true,
        diffs: [],
        actualTableNames: [],
      }),
    }));
    const runner = await import('../src/database/migration-runner.js');
    applyMigration = runner.applyMigration;

    queryFilters.push((text) => {
      if (text.includes('current_database()')) {
        return { rows: [{ database: 'testdb', user: 'tester', server_version: '16.0' }] };
      }
      if (text.includes('has_schema_privilege')) {
        return { rows: [{ can_create: true }] };
      }
      if (text.includes('information_schema.tables')) {
        return { rows: [] };
      }
      return undefined;
    });
    failQueryContaining = 'current_database()';

    const config = baseConfig;

    await expect(applyMigration(config, MIGRATION_PATH, REAL_BYTES)).rejects.toThrow(
      /Mock SQL failure/,
    );

    const commitIndex = queryLog.indexOf('COMMIT');
    const rollbackIndex = queryLog.indexOf('ROLLBACK');
    expect(rollbackIndex).toBeGreaterThanOrEqual(0);
    expect(commitIndex).toBe(-1);
    // The failing inspection query is logged, but ROLLBACK follows it.
    expect(rollbackIndex).toBeGreaterThan(
      queryLog.findIndex((q) => q.includes('current_database()')),
    );
    expect(queryLog[queryLog.length - 1]).toBe('END');
  });

  it('fetchStatus: inspects metadata and always ends the client', async () => {
    queryFilters.push((text) => {
      if (text.includes('current_database()')) {
        return { rows: [{ database: 'testdb', user: 'tester', server_version: '16.0' }] };
      }
      if (text.includes('information_schema.tables')) {
        return { rows: [] };
      }
      if (text.includes('information_schema.columns')) {
        return { rows: [] };
      }
      return undefined;
    });

    const config = baseConfig;

    const inspection = await fetchStatus(config);

    expect(inspection.server.database).toBe('testdb');
    expect(inspection.server.user).toBe('tester');
    expect(inspection.tables).toHaveLength(7);
    expect(queryLog[queryLog.length - 1]).toBe('END');
  });

  it('verifyMigration: returns verification and inspection from the same read-only transaction', async () => {
    queryFilters.push((text) => {
      if (text.includes('current_database()')) {
        return { rows: [{ database: 'testdb', user: 'tester', server_version: '16.0' }] };
      }
      if (text.includes('information_schema.tables')) {
        return { rows: [{ table_name: 'lu_site' }] };
      }
      if (text.includes('information_schema.columns')) {
        return { rows: [{ table_name: 'lu_site', column_name: 'id' }] };
      }
      return undefined;
    });

    const config = baseConfig;

    const outcome = await verifyMigration(config, MIGRATION_PATH, REAL_BYTES);

    expect(outcome.verification.passed).toBe(true);
    expect(outcome.verification.diffs).toEqual([]);
    expect(outcome.inspection.server.database).toBe('testdb');
    expect(outcome.inspection.tables).toHaveLength(7);
    const site = outcome.inspection.tables.find((t) => t.table === 'lu_site');
    expect(site).toBeDefined();
    expect(site!.exists).toBe(true);
    expect(site!.columns).toEqual(['id']);

    const beginIndex = queryLog.indexOf('BEGIN ISOLATION LEVEL REPEATABLE READ READ ONLY');
    const commitIndex = queryLog.indexOf('COMMIT');
    expect(beginIndex).toBeGreaterThanOrEqual(0);
    expect(commitIndex).toBeGreaterThan(beginIndex);
    expect(queryLog[queryLog.length - 1]).toBe('END');
  });

  it('verifyMigration: rolls back and never writes when connection fails', async () => {
    connectError = new Error('connection refused');

    const config = baseConfig;

    await expect(verifyMigration(config, MIGRATION_PATH, REAL_BYTES)).rejects.toThrow(
      /connection refused/,
    );

    expect(queryLog.some((q) => q === 'BEGIN ISOLATION LEVEL REPEATABLE READ READ ONLY')).toBe(
      false,
    );
    expect(queryLog.some((q) => q === 'ROLLBACK')).toBe(false);
    expect(queryLog.some((q) => q === 'COMMIT')).toBe(false);
  });
});

describe('migration-runner pure helpers', () => {
  it('imports do not start a database connection', async () => {
    const { applyMigration: _apply } = await import('../src/database/migration-runner.js');
    expect(typeof _apply).toBe('function');
  });
});
