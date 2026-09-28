/**
 * MIG-F3-PG-TEST-003 — runner unit specs with ESM mocks for `pg` (no real DB, no sockets).
 *
 * These tests prove the transaction orchestration invariants of applyMigration:
 *  - statements execute in the expected order inside a SERIALIZABLE transaction,
 *  - COMMIT is issued only when the body and verification succeed,
 *  - ROLLBACK is issued on SQL execution failure or verification failure,
 *  - the canonical body is NOT executed when a target table already exists
 *    AND a history table is present AND no matching pin row exists,
 *  - client.end() is always called on every path that opened a connection.
 *
 * The heavy schema verifier is mocked to a pure function so these specs isolate
 * runner behaviour, not PostgreSQL catalog semantics.
 *
 * MIG-001-F2-W11/W13A note: the W10 functional block below was repaired after
 * the W10 mechanical split (the split referenced out-of-scope state such as
 * `freshRunner`/`MockClient`/`baseConfig` from the first describe block). The
 * block now owns its capture state and re-imports the runner per test. The
 * W13A block adds the accepted review-fix regressions: ledger contiguity,
 * cumulative CP manifest ladder, supplemental merge last-ordinal-wins,
 * migrator preflight, and verifier exactness.
 */
import { readFileSync } from 'node:fs';
import { fileURLToPath } from 'node:url';
import { IDENTITY_MIGRATION_FILE } from '../src/database/migration-plan.js';
import {
  CONTROL_PLANE_COMMON_ADVISORY_LOCK_KEY,
  CONTROL_PLANE_REGISTRY,
  TENANT_COMMON_ADVISORY_LOCK_PREFIX,
  findRegistryEntryByRelativePath,
} from '../src/database/migration-registry.js';

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

/**
 * Shared W13A preflight filters — covers every catalog query the migrator
 * preflight issues after BEGIN + SET LOCAL search_path and BEFORE the
 * advisory locks. Tests that exercise the APPLY path push this filter; the
 * resulting queries never echo credentials and never select from business
 * tables (pg_roles / pg_class / pg_proc / pg_namespace only).
 */
function migratorPreflightFilters(
  overrides: Partial<{
    sessionRole: Record<string, unknown> | null;
    migratorRole: Record<string, unknown> | null;
    runtimeRole: Record<string, unknown> | null;
    membership: boolean;
    currentActor: string;
    migratorCanCreate: boolean;
    runtimeCanCreate: boolean;
    runtimeOwnsTables: boolean;
    runtimeOwnsSequences: boolean;
    runtimeOwnsFunctions: boolean;
  }> = {},
): QueryFilter[] {
  const sessionRole =
    overrides.sessionRole === null
      ? null
      : (overrides.sessionRole ?? {
          rolname: 'lu_migrator_login',
          rolcanlogin: true,
          rolsuper: false,
          rolcreaterole: false,
          rolcreatedb: false,
          rolreplication: false,
          rolbypassrls: false,
        });
  const migratorRole =
    overrides.migratorRole === null
      ? null
      : (overrides.migratorRole ?? {
          rolname: 'lu_auth_migrator',
          rolcanlogin: false,
        });
  const runtimeRole =
    overrides.runtimeRole === null
      ? null
      : (overrides.runtimeRole ?? {
          rolname: 'lu_auth_runtime',
          rolcanlogin: false,
        });
  const filters: QueryFilter[] = [];
  filters.push((text, values) => {
    // Q1: session role properties
    if (text.includes('pg_roles') && text.includes('rolname = current_user')) {
      if (sessionRole === null) return { rows: [] };
      return { rows: [sessionRole] };
    }
    // Q2 / Q4: role presence (parameterized $1 = 'lu_auth_migrator' OR
    // 'lu_auth_runtime'). Both share the exact same SQL shape so the
    // filter dispatches on the bound parameter rather than the text.
    if (
      text.includes('pg_roles') &&
      text.includes('rolname = $1') &&
      text.includes('rolcanlogin') &&
      !text.includes('LEFT JOIN')
    ) {
      const param = String((values ?? [])[0]);
      if (param === 'lu_auth_migrator') {
        if (migratorRole === null) return { rows: [] };
        return { rows: [migratorRole] };
      }
      if (param === 'lu_auth_runtime') {
        if (runtimeRole === null) return { rows: [] };
        return { rows: [runtimeRole] };
      }
      return undefined;
    }
    // Q3: session membership
    if (text.includes("pg_has_role(current_user, $1, 'MEMBER')")) {
      return { rows: [{ is_member: overrides.membership ?? true }] };
    }
    // Q6: current_user verification after SET LOCAL ROLE
    if (text.includes('current_user AS current_actor')) {
      return { rows: [{ current_actor: overrides.currentActor ?? 'lu_auth_migrator' }] };
    }
    // Q7: migrator CREATE on target schema (parameterized $1, $2 = 'lu_auth_migrator', schema)
    if (text.includes('has_schema_privilege($1, $2, $3)') && !text.includes('runtime_can_create')) {
      return { rows: [{ can_create: overrides.migratorCanCreate ?? true }] };
    }
    // Q8: lu_auth_runtime CREATE on public
    if (text.includes('runtime_can_create')) {
      return { rows: [{ runtime_can_create: overrides.runtimeCanCreate ?? false }] };
    }
    // Q9: lu_auth_runtime ownership of migrable public objects
    if (text.includes('runtime_owns_tables')) {
      return {
        rows: [
          {
            runtime_owns_tables: overrides.runtimeOwnsTables ?? false,
            runtime_owns_sequences: overrides.runtimeOwnsSequences ?? false,
            runtime_owns_functions: overrides.runtimeOwnsFunctions ?? false,
          },
        ],
      };
    }
    return undefined;
  });
  return filters;
}

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
      AUTH_SECURITY_SCHEMA_MANIFEST: { tables: { lu_tenant_route: {} } },
      IDENTITY_CONTROL_PLANE_SCHEMA_MANIFEST: {},
      TENANT_ROUTE_SCHEMA_MANIFEST: {
        tables: {
          lu_tenant_route: {
            columns: [],
            primaryKey: { columns: ['site_id'] },
            uniques: [],
            foreignKeys: [],
            checks: [],
            indexes: [],
          },
        },
      },
      normalizeCheckDefinition: (s: string) => s,
      normalizeDefault: (s: string | null) => s,
      normalizeIndexDefinition: (s: string) => s,
      verifySchema: async () => ({
        passed: true,
        diffs: [],
        actualTableNames: [],
      }),
    }));
    unstableMockModule('../src/database/schema-manifest-f2.js', () => ({
      POST_F2_CONTROL_PLANE_SCHEMA_MANIFEST: {},
      POST_F2_CONTROL_PLANE_TABLES: [],
      ACADEMIC_CATALOG_SCHEMA_MANIFEST: {},
      TENANT_FINAL_TABLES: [],
      TENANT_FINAL_SCHEMA_MANIFEST: {},
      TENANT_MIGRATION_MANIFESTS: {},
      TENANT_SUPPLEMENTAL_EXPECTATIONS: {},
      CONTROL_PLANE_SUPPLEMENTAL_EXPECTATIONS: {},
    }));
    unstableMockModule('../src/database/schema-manifest-tenant.js', () => ({
      TENANT_FINAL_TABLES: [],
      TENANT_FINAL_SCHEMA_MANIFEST: {},
      TENANT_MIGRATION_MANIFESTS: {},
      TENANT_SUPPLEMENTAL_EXPECTATIONS: {},
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
    queryFilters.push(...migratorPreflightFilters());
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

    const outcome = await applyMigration(config, MIGRATION_PATH, REAL_BYTES);

    expect(outcome.executed).toBe(true);
    expect(outcome.committed).toBe(true);
    expect(outcome.preexisting).toEqual([]);

    // Transaction orchestration order (W13A):
    //   CONNECT, BEGIN, SET LOCAL search_path,
    //   preflight: pg_roles(session), pg_roles(migrator),
    //              pg_has_role, SET LOCAL ROLE, current_user, has_schema_privilege,
    //              runtime_can_create, runtime_owns_*,
    //   stream-common lock, per-entry lock,
    //   has_schema_privilege (CREATE on schema as migrator),
    //   history probe, ledger check (skipped when no history),
    //   findPreexistingTables, body, verifySchema, supplemental verify,
    //   history probe (no-op when no history),
    //   inspect, COMMIT, END.
    // No information_schema queries after COMMIT.
    expect(queryLog[0]).toMatch(/CONNECT/i);
    expect(queryLog[1]).toBe('BEGIN ISOLATION LEVEL SERIALIZABLE');
    expect(queryLog[2]).toBe('SET LOCAL search_path = public, pg_catalog');
    const lockQueries = queryLog.filter((q) => q.includes('pg_advisory_xact_lock'));
    expect(lockQueries.length).toBeGreaterThanOrEqual(2);
    const lockIndex = queryLog.findIndex((q) => q.includes('pg_advisory_xact_lock'));
    expect(lockIndex).toBeGreaterThan(2);
    expect(queryLog[lockIndex + 1]).toMatch(/pg_advisory_xact_lock/);
    const hasPrivilege = queryLog.findIndex((q) => q.includes('has_schema_privilege($1, $2, $3)'));
    expect(hasPrivilege).toBeGreaterThan(2);
    // CREATE-on-schema check runs as part of the preflight and the
    // post-preflight canCreate check; the latter lives AFTER the locks.
    const preexist = queryLog.findIndex((q) => q.includes('information_schema.tables'));
    expect(preexist).toBeGreaterThan(lockIndex);
    const createTableIndex = queryLog.findIndex((q) => q.includes('CREATE TABLE'));
    expect(createTableIndex).toBeGreaterThan(preexist);
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
    queryFilters.push(...migratorPreflightFilters());
    queryFilters.push((text, values) => {
      if (text.includes('current_database()')) {
        return { rows: [{ database: 'testdb', user: 'tester', server_version: '16.0' }] };
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
    queryFilters.push(...migratorPreflightFilters());
    queryFilters.push((text) => {
      if (text.includes('current_database()')) {
        return { rows: [{ database: 'testdb', user: 'tester', server_version: '16.0' }] };
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
    queryFilters.push(...migratorPreflightFilters());
    queryFilters.push((text) => {
      if (text.includes('current_database()')) {
        return { rows: [{ database: 'testdb', user: 'tester', server_version: '16.0' }] };
      }
      if (text.includes('has_schema_privilege')) {
        return { rows: [{ can_create: true }] };
      }
      // History table probe says it exists, the pin row query returns empty,
      // and findPreexistingTables finds lu_site.
      if (text.startsWith('SELECT EXISTS (')) {
        return { rows: [{ exists: true }] };
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
    jestRuntime.resetModules();
    unstableMockModule('pg', () => ({
      Client: MockClient,
    }));
    unstableMockModule('../src/database/schema-manifest.js', () => ({
      AUTH_SECURITY_SCHEMA_MANIFEST: {},
      IDENTITY_CONTROL_PLANE_SCHEMA_MANIFEST: {},
      TENANT_ROUTE_SCHEMA_MANIFEST: {},
      normalizeCheckDefinition: (s: string) => s,
      normalizeDefault: (s: string | null) => s,
      normalizeIndexDefinition: (s: string) => s,
      verifySchema: async () => ({
        passed: false,
        diffs: ['mocked verification failure'],
        actualTableNames: [],
      }),
    }));
    // Re-import to pick up the new verifier mock.
    const runner = await import('../src/database/migration-runner.js');
    applyMigration = runner.applyMigration;

    queryFilters.push(...migratorPreflightFilters());
    queryFilters.push((text) => {
      if (text.includes('current_database()')) {
        return { rows: [{ database: 'testdb', user: 'tester', server_version: '16.0' }] };
      }
      if (text.includes('information_schema.tables')) {
        return { rows: [] };
      }
      return undefined;
    });

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
      normalizeCheckDefinition: (s: string) => s,
      normalizeDefault: (s: string | null) => s,
      normalizeIndexDefinition: (s: string) => s,
      verifySchema: async () => ({
        passed: true,
        diffs: [],
        actualTableNames: [],
      }),
    }));
    // Re-import to pick up the new verifier mock.
    const runner = await import('../src/database/migration-runner.js');
    applyMigration = runner.applyMigration;

    queryFilters.push(...migratorPreflightFilters());
    queryFilters.push((text) => {
      if (text.includes('current_database()')) {
        return { rows: [{ database: 'testdb', user: 'tester', server_version: '16.0' }] };
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
    expect(rollbackIndex).toBeGreaterThan(
      queryLog.findIndex((q) => q.includes('current_database()')),
    );
    expect(queryLog[queryLog.length - 1]).toBe('END');
  });

  it('fetchStatus: inspects post-F2 metadata for the control-plane stream and always ends the client', async () => {
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
    // Control-plane status inspects the post-F2 table set (15 tables);
    // pre-F2 inspection is no longer reachable from the public API.
    expect(inspection.tables).toHaveLength(15);
    expect(queryLog[queryLog.length - 1]).toBe('END');
  });

  it('verifyMigration: returns verification and inspection from the same read-only transaction', async () => {
    // verify must run the migrator preflight before the history probe
    // (the executor session is NOINHERIT so the history table is only
    // visible after `SET LOCAL ROLE lu_auth_migrator`). Without these
    // mocks, the preflight rejects with an empty pg_roles row.
    queryFilters.push(...migratorPreflightFilters());
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
    // The cumulative manifest on a DB without history pins is the entry's
    // own per-ordinal manifest (0001 touches the 4 baseline tables); the
    // runner then inspects the SAME set so the operator sees consistent
    // drift between manifest expectations and live catalog.
    expect(outcome.inspection.tables).toHaveLength(4);
    const site = outcome.inspection.tables.find((t) => t.table === 'lu_site');
    expect(site).toBeDefined();
    expect(site!.exists).toBe(true);
    expect(site!.columns).toEqual(['id']);

    const beginIndex = queryLog.indexOf('BEGIN ISOLATION LEVEL REPEATABLE READ READ ONLY');
    const commitIndex = queryLog.indexOf('COMMIT');
    expect(beginIndex).toBeGreaterThanOrEqual(0);
    expect(commitIndex).toBeGreaterThan(beginIndex);
    // The preflight runs before any ledger query and emits SET LOCAL
    // ROLE so the migrator role can see lu_migration_history.
    const setLocalRoleIdx = queryLog.indexOf('SET LOCAL ROLE lu_auth_migrator');
    expect(setLocalRoleIdx).toBeGreaterThan(beginIndex);
    const historyProbeIdx = queryLog.findIndex((q) =>
      q.includes("table_name = 'lu_migration_history'"),
    );
    expect(historyProbeIdx).toBeGreaterThan(setLocalRoleIdx);
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

// ---------------------------------------------------------------------------
// MIG-001-F2-W10/W11 — functional contract suite: history ledger stamping,
// lock order, ACL-only pairing and cumulative replay verification.
//
// Repaired after the W10 mechanical split: this block carries its OWN capture
// state (log/lockKeys/queryValues/filters) and imports the runner fresh per
// test; the split had left it referencing `freshRunner`, `MockClient`,
// `baseConfig`, `applyMigration` and `verifyMigration` from the first
// describe block's private closure (test-only compile break). The production
// behaviour is now contract-correct for every invariant asserted below.
// ---------------------------------------------------------------------------
describe('migration-runner W10/W11: ledger, locks and ACL-only pairing', () => {
  type RunnerModule = typeof import('../src/database/migration-runner.js');
  type PinRow = Parameters<RunnerModule['__testing']['comparePinRow']>[0];
  interface MockManifest {
    tables?: Record<string, unknown>;
  }
  interface MockVerification {
    passed: boolean;
    diffs: string[];
    actualTableNames: string[];
  }

  const tenant0013Path = fileURLToPath(
    new URL('../migrations/tenant/0013_migration_history.sql', import.meta.url),
  );
  const tenant0013Bytes = readFileSync(tenant0013Path);
  const cp0005Path = fileURLToPath(
    new URL('../migrations/control-plane/0005_user_management.sql', import.meta.url),
  );
  const cp0005Bytes = readFileSync(cp0005Path);
  const cp0006Path = fileURLToPath(
    new URL('../migrations/control-plane/0006_mig001_users_identity.sql', import.meta.url),
  );
  const cp0006Bytes = readFileSync(cp0006Path);
  const tenant0006Path = fileURLToPath(
    new URL('../migrations/tenant/0006_management_operations.sql', import.meta.url),
  );
  const tenant0006Bytes = readFileSync(tenant0006Path);

  const baseConfig = {
    host: 'localhost',
    port: 5432,
    database: 'testdb',
    user: 'tester',
    password: 'secret',
    ssl: false as const,
  };

  const HISTORY_SELECT_PREFIX =
    'SELECT stream, ordinal, relative_path, sha256, bytes, work_id FROM public.lu_migration_history';
  const LEDGER_SELECT_PREFIX =
    'SELECT stream, ordinal, relative_path, sha256, bytes, work_id FROM public.lu_migration_history WHERE stream = $1 ORDER BY ordinal ASC';
  const PIN_SELECT_PREFIX =
    'SELECT stream, ordinal, relative_path, sha256, bytes, work_id FROM public.lu_migration_history WHERE stream = $1 AND ordinal = $2 LIMIT 1';
  const MAX_ORDINAL_SELECT =
    'SELECT COALESCE(MAX( ordinal), 0) AS max_ordinal FROM public.lu_migration_history WHERE stream = $1';
  const SESSION_ROLE_SELECT =
    'SELECT rolname, rolcanlogin, rolsuper, rolcreaterole, rolcreatedb, rolreplication, rolbypassrls FROM pg_roles WHERE rolname = current_user';
  const MIGRATOR_ROLE_SELECT =
    'SELECT rolname, rolcanlogin, rolsuper, rolcreaterole, rolcreatedb,\n       rolreplication, rolbypassrls\n       FROM pg_roles WHERE rolname = $1';
  const MEMBERSHIP_SELECT = "SELECT pg_has_role(current_user, $1, 'MEMBER') AS is_member";
  const RUNTIME_ROLE_SELECT =
    'SELECT rolname, rolcanlogin, rolsuper, rolcreaterole, rolcreatedb,\n       rolreplication, rolbypassrls\n       FROM pg_roles WHERE rolname = $1';
  const CURRENT_ACTOR_SELECT = 'SELECT current_user AS current_actor';
  const RUNTIME_CREATE_SELECT =
    "SELECT has_schema_privilege($1, $2, 'CREATE') AS runtime_can_create";
  const RUNTIME_OWNS_SELECT = 'SELECT\n       EXISTS(';

  const log: string[] = [];
  const lockKeys: string[] = [];
  const queryValues: Array<{ text: string; values: unknown[] }> = [];
  const filters: QueryFilter[] = [];

  // The W10 capture client. LAST-registered filter wins so a test can push
  // its own overrides after standardFilters() without re-declaring defaults.
  class W10Client {
    async connect(): Promise<void> {
      log.push('CONNECT');
    }
    async query(text: string, values?: unknown[]): Promise<MockQueryResult> {
      log.push(text);
      if (text.includes('pg_advisory_xact_lock')) {
        lockKeys.push(String(values?.[0] ?? ''));
      }
      queryValues.push({ text, values: values ?? [] });
      for (let i = filters.length - 1; i >= 0; i--) {
        const filter = filters[i] as QueryFilter;
        const result = filter(text, values);
        if (result !== undefined) return result;
      }
      return { rows: [] };
    }
    async end(): Promise<void> {
      log.push('END');
    }
  }

  async function importRunner(
    opts: {
      verifySchema?: (manifest: MockManifest) => MockVerification;
      schemaManifest?: Record<string, unknown>;
      f2?: Record<string, unknown>;
      tenant?: Record<string, unknown>;
    } = {},
  ): Promise<RunnerModule> {
    jestRuntime.resetModules();
    unstableMockModule('pg', () => ({ Client: W10Client }));
    unstableMockModule('../src/database/schema-manifest.js', () => ({
      IDENTITY_CONTROL_PLANE_SCHEMA_MANIFEST: { schema: 'public', tables: {} },
      AUTH_SECURITY_SCHEMA_MANIFEST: { schema: 'public', tables: {} },
      TENANT_ROUTE_SCHEMA_MANIFEST: { schema: 'public', tables: {} },
      normalizeCheckDefinition: (s: string) => s,
      normalizeDefault: (s: string | null) => s,
      normalizeIndexDefinition: (s: string) => s,
      verifySchema: async (_client: unknown, manifest: MockManifest) =>
        (opts.verifySchema ?? (() => ({ passed: true, diffs: [], actualTableNames: [] })))(
          manifest,
        ),
      ...opts.schemaManifest,
    }));
    unstableMockModule('../src/database/schema-manifest-f2.js', () => ({
      POST_F2_CONTROL_PLANE_SCHEMA_MANIFEST: {},
      POST_F2_CONTROL_PLANE_TABLES: [],
      ACADEMIC_CATALOG_SCHEMA_MANIFEST: {},
      CONTROL_PLANE_SUPPLEMENTAL_EXPECTATIONS: {},
      ...opts.f2,
    }));
    unstableMockModule('../src/database/schema-manifest-tenant.js', () => ({
      TENANT_FINAL_TABLES: [],
      TENANT_FINAL_SCHEMA_MANIFEST: {},
      TENANT_MIGRATION_MANIFESTS: {},
      TENANT_SUPPLEMENTAL_EXPECTATIONS: {},
      ...opts.tenant,
    }));
    return import('../src/database/migration-runner.js');
  }

  function standardFilters(): void {
    filters.push(...migratorPreflightFilters());
    filters.push((text) => {
      if (text.includes('current_database()')) {
        return { rows: [{ database: 'testdb', user: 'tester', server_version: '16.0' }] };
      }
      if (text.includes('has_schema_privilege($1, $2, $3)')) {
        return { rows: [{ can_create: true }] };
      }
      if (text.startsWith('SELECT EXISTS (')) {
        return { rows: [{ exists: false }] };
      }
      if (text.startsWith(HISTORY_SELECT_PREFIX)) {
        return { rows: [] };
      }
      if (text.startsWith(MAX_ORDINAL_SELECT)) {
        return { rows: [{ max_ordinal: 0 }] };
      }
      if (
        text.includes('information_schema.tables') ||
        text.includes('information_schema.columns')
      ) {
        return { rows: [] };
      }
      return undefined;
    });
  }

  function pinRowFor(relativePath: string): PinRow {
    const entry = findRegistryEntryByRelativePath(relativePath);
    if (entry === undefined) {
      throw new Error(`test setup: ${relativePath} is not registered`);
    }
    return {
      stream: entry.stream,
      ordinal: entry.ordinal,
      relative_path: entry.relativePath,
      sha256: entry.sha256,
      bytes: entry.byteLength,
      work_id: entry.workId,
    };
  }

  // Stateful in-memory `lu_migration_history`: INSERT ... ON CONFLICT
  // (stream, ordinal) DO NOTHING keeps the first row; every SELECT re-reads
  // from the map, exactly like the immutable-ledger contract requires.
  //
  // `probeAnswers` is an explicit ordered array: each `SELECT EXISTS (`
  // call consumes the next entry. Once exhausted the filter defers so the
  // caller's own `standardFilters` answer can win. This is the
  // fresh-apply shape for the entries that CREATE `lu_migration_history`
  // (CP 0006, tenant 0013): probeAnswers `[false, true]` models the
  // pre-body probe (table does NOT exist yet, body about to create it)
  // followed by the post-body re-probe (table now exists). A single
  // `[true]` is the replay scenario where the relation is already on disk.
  function ledgerFilter(
    ledger: Map<string, Record<string, unknown>>,
    opts: { probeAnswers?: readonly boolean[] } = {},
  ): QueryFilter {
    const probeAnswers = opts.probeAnswers ?? [];
    let probeIdx = 0;
    return (text, values) => {
      if (text.startsWith(LEDGER_SELECT_PREFIX)) {
        const stream = String(values?.[0]);
        const rows: Record<string, unknown>[] = [];
        for (const [key, value] of ledger.entries()) {
          if (key.startsWith(`${stream}:`)) rows.push(value);
        }
        return { rows };
      }
      if (text.startsWith(PIN_SELECT_PREFIX)) {
        const key = `${String(values?.[0])}:${String(values?.[1])}`;
        const row = ledger.get(key);
        return { rows: row === undefined ? [] : [row] };
      }
      if (text.startsWith('INSERT INTO public.lu_migration_history')) {
        const v = (values ?? []) as unknown[];
        const key = `${String(v[0])}:${String(v[1])}`;
        if (!ledger.has(key)) {
          ledger.set(key, {
            stream: v[0],
            ordinal: v[1],
            relative_path: v[2],
            sha256: v[3],
            bytes: v[4],
            work_id: v[5],
          });
        }
        return { rows: [] };
      }
      if (text.startsWith('SELECT EXISTS (')) {
        // Explicit probe-answer array: each call consumes the next
        // entry. Once exhausted the filter defers so the caller's
        // own `standardFilters`/`ledger` answer can win. This
        // replaces the W10/W13A implicit `preBodyProbe` deferral
        // (W16A equivalent: `ledgerWithProbes(ledger, answers)`).
        if (probeIdx < probeAnswers.length) {
          const answer = probeAnswers[probeIdx] === true;
          probeIdx += 1;
          return { rows: [{ exists: answer }] };
        }
        return undefined;
      }
      if (text.startsWith(MAX_ORDINAL_SELECT)) {
        let max = 0;
        for (const [key, value] of ledger.entries()) {
          if (!key.startsWith(`${String(values?.[0])}:`)) continue;
          const ord = Number(value.ordinal);
          if (Number.isFinite(ord) && ord > max) max = ord;
        }
        return { rows: [{ max_ordinal: max }] };
      }
      return undefined;
    };
  }

  beforeEach(() => {
    log.length = 0;
    lockKeys.length = 0;
    queryValues.length = 0;
    filters.length = 0;
  });

  afterAll(() => {
    jestRuntime.resetModules();
  });

  it('applyMigration: tenant stream fails closed without a site UUID and never opens a connection', async () => {
    const runner = await importRunner();
    await expect(
      runner.applyMigration(baseConfig, tenant0013Path, tenant0013Bytes, {
        stream: 'tenant',
      }),
    ).rejects.toThrow(/requires an explicit --site UUID/i);
    // Fail-closed BEFORE the stream-common lock and before `new Client()`:
    // not a single statement is issued.
    expect(log).toEqual([]);
  });

  it('applyMigration: tenant stream also fails closed when the site id is empty', async () => {
    const runner = await importRunner();
    await expect(
      runner.applyMigration(baseConfig, tenant0013Path, tenant0013Bytes, {
        stream: 'tenant',
        site: { siteId: '', migrationSecretReference: null },
      }),
    ).rejects.toThrow(/refuses to invent a site|requires an explicit --site UUID/i);
    expect(log).toEqual([]);
  });

  it('applyMigration: the stream-common lock is taken AFTER the migrator preflight with the pinned keys, and nothing queries the catalog after COMMIT', async () => {
    const runner = await importRunner();
    standardFilters();
    filters.push((text) => {
      if (text.startsWith(HISTORY_SELECT_PREFIX)) {
        // Pre-history DB: the ledger table does not exist yet.
        throw new Error('relation "public.lu_migration_history" does not exist');
      }
      return undefined;
    });

    const outcome = await runner.applyMigration(baseConfig, MIGRATION_PATH, REAL_BYTES);
    expect(outcome.executed).toBe(true);
    expect(outcome.committed).toBe(true);
    expect(outcome.historyKey).toBeUndefined();

    expect(log[0]).toBe('CONNECT');
    expect(log[1]).toBe('BEGIN ISOLATION LEVEL SERIALIZABLE');
    expect(log[2]).toBe('SET LOCAL search_path = public, pg_catalog');
    // The preflight runs first: session role / migrator role / membership /
    // runtime role / SET LOCAL ROLE / current_user / has_schema_privilege /
    // runtime CREATE / runtime ownership all land BEFORE the advisory
    // locks. The runtime role properties (Q4) are fetched BEFORE
    // `SET LOCAL ROLE lu_auth_migrator` so the session login can still see
    // the runtime principal through pg_roles.
    expect(log[3]).toBe(SESSION_ROLE_SELECT);
    expect(log[4]?.replace(/\s+/g, ' ').trim()).toBe(
      MIGRATOR_ROLE_SELECT.replace(/\s+/g, ' ').trim(),
    );
    expect(log[5]).toBe(MEMBERSHIP_SELECT);
    expect(log[6]?.replace(/\s+/g, ' ').trim()).toBe(
      RUNTIME_ROLE_SELECT.replace(/\s+/g, ' ').trim(),
    );
    expect(log[7]).toBe('SET LOCAL ROLE lu_auth_migrator');
    expect(log[8]).toBe(CURRENT_ACTOR_SELECT);
    expect(log[9]).toMatch(/has_schema_privilege\(\$1, \$2, \$3\)/);
    expect(log[10]).toBe(RUNTIME_CREATE_SELECT);
    expect(log[11]?.startsWith(RUNTIME_OWNS_SELECT)).toBe(true);
    const lockStart = log.findIndex((q) => q.includes('pg_advisory_xact_lock'));
    expect(lockStart).toBe(12);
    expect(lockKeys[0]).toBe(CONTROL_PLANE_COMMON_ADVISORY_LOCK_KEY);
    expect(lockKeys[1]).toBe('lu:identity-control-plane:0001');

    // No catalog query after COMMIT: only client.end() follows.
    const commitIdx = log.indexOf('COMMIT');
    expect(commitIdx).toBeGreaterThan(0);
    const after = log.slice(commitIdx + 1);
    expect(after.some((q) => q.includes('information_schema'))).toBe(false);
    expect(after.some((q) => q.includes('current_database'))).toBe(false);
    expect(after.some((q) => q.includes('lu_migration_history'))).toBe(false);
    expect(after).toEqual(['END']);
  });

  it('applyMigration: pre-history 0001 apply probes the ledger once, never INSERTs and reports no historyKey', async () => {
    const runner = await importRunner();
    standardFilters(); // SELECT EXISTS -> {exists:false}; history SELECT -> no row
    const outcome = await runner.applyMigration(baseConfig, MIGRATION_PATH, REAL_BYTES);
    expect(outcome.executed).toBe(true);
    expect(outcome.committed).toBe(true);
    expect(outcome.historyKey).toBeUndefined();
    expect(log.filter((q) => q.startsWith('SELECT EXISTS ('))).toHaveLength(1);
    expect(log.some((q) => q.startsWith('INSERT INTO public.lu_migration_history'))).toBe(false);
    // The body DID run: an early migration applied by a pre-ledger build is
    // re-run (idempotent canonical body) instead of being refused.
    expect(log.some((q) => q.includes('CREATE TABLE'))).toBe(true);
  });

  it('applyMigration: 0006 stamps the full CP ledger (1..6) with the pinned bytes and re-verifies every row BEFORE COMMIT', async () => {
    const runner = await importRunner();
    standardFilters();
    const ledger = new Map<string, Record<string, unknown>>();
    // W16A / proven B1 fix: fresh apply of CP 0006 — the pre-body
    // probe answers FALSE (the relation does not exist yet because 0006
    // is what creates it) and the post-body re-probe answers TRUE
    // (the body just created the relation). The OLD mock that falsely
    // returned true on the pre-body probe is corrected here.
    filters.push(ledgerFilter(ledger, { probeAnswers: [false, true] }));

    const outcome = await runner.applyMigration(baseConfig, cp0006Path, cp0006Bytes);
    expect(outcome.executed).toBe(true);
    expect(outcome.committed).toBe(true);
    expect(outcome.historyKey).toEqual({
      stream: 'control-plane',
      relativePath: 'control-plane/0006_mig001_users_identity.sql',
    });

    const commitIdx = log.indexOf('COMMIT');
    expect(commitIdx).toBeGreaterThan(0);
    const inserts = queryValues.filter((c) =>
      c.text.startsWith('INSERT INTO public.lu_migration_history'),
    );
    expect(inserts).toHaveLength(6);
    expect(inserts.map((c) => Number(c.values[1])).sort((a, b) => a - b)).toEqual([
      1, 2, 3, 4, 5, 6,
    ]);
    for (const c of inserts) {
      const entry = findRegistryEntryByRelativePath(String(c.values[2]));
      expect(entry).toBeDefined();
      expect(c.values[0]).toBe(entry?.stream);
      expect(c.values[3]).toBe(entry?.sha256);
      expect(c.values[4]).toBe(entry?.byteLength);
      expect(c.values[5]).toBe(entry?.workId);
    }
    const insertIdxs = log
      .map((q, i) => (q.startsWith('INSERT INTO public.lu_migration_history') ? i : -1))
      .filter((i) => i >= 0);
    expect(insertIdxs.every((i) => i < commitIdx)).toBe(true);
    // With probeAnswers=[false,true] the pre-body ledger check is skipped
    // (hasHistoryTable=false); the only ledger reads are the
    // per-ordinal post-INSERT re-reads inside backfill (PIN_SELECT,
    // not LEDGER_SELECT).
    expect(queryValues.filter((c) => c.text.startsWith(LEDGER_SELECT_PREFIX))).toHaveLength(0);
    // Two history probes total: the pre-body probe (false) and the
    // post-body re-probe (true) — both BEFORE COMMIT, none after.
    const probes = log.filter((q) => q.startsWith('SELECT EXISTS ('));
    expect(probes).toHaveLength(2);
    const firstProbeIdx = log.findIndex((q) => q.startsWith('SELECT EXISTS ('));
    const secondProbeIdx = log.findIndex(
      (q, i) => i > firstProbeIdx && q.startsWith('SELECT EXISTS ('),
    );
    expect(firstProbeIdx).toBeGreaterThan(0);
    expect(secondProbeIdx).toBeGreaterThan(firstProbeIdx);
    // The re-probe happens AFTER the body (so we know the body created
    // the relation) and BEFORE COMMIT, satisfying the W16A "no query
    // after COMMIT" invariant.
    const bodyIdx = log.findIndex((q) => q.includes('CREATE TABLE'));
    expect(secondProbeIdx).toBeGreaterThan(bodyIdx);
    expect(secondProbeIdx).toBeLessThan(commitIdx);
    // No catalog query after COMMIT.
    expect(log.slice(commitIdx + 1)).toEqual(['END']);
  });

  it('applyMigration: backfill rejects a recorded pin that diverges from the registry (immutable ledger) and rolls back', async () => {
    const runner = await importRunner();
    standardFilters();
    const ledger = new Map<string, Record<string, unknown>>();
    const diverged: Record<string, unknown> = {
      ...pinRowFor('0002_create_auth_security_controls.sql'),
      sha256: 'f'.repeat(64),
    };
    // Ledger has a contiguous 1..2 run so the runner reaches the
    // pin-match step at the LEDGER CHECK; ordinal 2 is the tampered row.
    // The pre-body probe answers TRUE because a prior runner already
    // stamped the relation — the divergence MUST be caught by the
    // ledger contiguity/pin check BEFORE any body execution.
    ledger.set('control-plane:1', pinRowFor('0001_create_identity_control_plane.sql'));
    ledger.set('control-plane:2', diverged);
    filters.push(ledgerFilter(ledger, { probeAnswers: [true] }));

    await expect(runner.applyMigration(baseConfig, cp0006Path, cp0006Bytes)).rejects.toThrow(
      /lu_migration_history row for 0002_create_auth_security_controls\.sql is immutable and conflicts with the registered pin/,
    );
    expect(log.some((q) => q === 'ROLLBACK')).toBe(true);
    expect(log.some((q) => q === 'COMMIT')).toBe(false);
    expect(log[log.length - 1]).toBe('END');
    // ON CONFLICT DO NOTHING never overwrote the foreign row.
    expect(ledger.get('control-plane:2')).toBe(diverged);
  });

  it('applyMigration: replay with a matching history pin skips the body but still verifies and commits', async () => {
    const runner = await importRunner();
    standardFilters();
    const ledger = new Map<string, Record<string, unknown>>([
      [
        'control-plane:1',
        pinRowFor('0001_create_identity_control_plane.sql') as unknown as Record<string, unknown>,
      ],
    ]);
    // Replay scenario: the relation ALREADY exists on disk; the
    // pre-body probe answers TRUE so the ledger check runs and the
    // pin-matches-history shortcut is taken BEFORE the body.
    filters.push(ledgerFilter(ledger, { probeAnswers: [true] }));
    filters.push((text) => {
      if (text.startsWith(MAX_ORDINAL_SELECT)) {
        return { rows: [{ max_ordinal: 6 }] };
      }
      return undefined;
    });

    const outcome = await runner.applyMigration(baseConfig, MIGRATION_PATH, REAL_BYTES);
    expect(outcome.executed).toBe(false);
    expect(outcome.committed).toBe(true);
    expect(outcome.skipped).toEqual({ reason: 'pin-matches-history' });
    expect(outcome.verification).toEqual({ passed: true, diffs: [], actualTableNames: [] });
    expect(outcome.preexisting).toEqual([]);
    expect(log.some((q) => q.includes('CREATE TABLE'))).toBe(false);
    expect(log.some((q) => q.startsWith('INSERT INTO public.lu_migration_history'))).toBe(false);
    const commitIdx = log.indexOf('COMMIT');
    expect(commitIdx).toBeGreaterThan(0);
    expect(log.findIndex((q) => q.includes('current_database()'))).toBeLessThan(commitIdx);
    expect(log.slice(commitIdx + 1)).toEqual(['END']);
  });

  it('applyMigration: a recorded pin with a different work_id fails closed and the message names work_id', async () => {
    const runner = await importRunner();
    standardFilters();
    filters.push((text) => {
      if (text.startsWith('SELECT EXISTS (')) {
        return { rows: [{ exists: true }] };
      }
      if (text.startsWith(MAX_ORDINAL_SELECT)) {
        return { rows: [{ max_ordinal: 1 }] };
      }
      if (text.startsWith(HISTORY_SELECT_PREFIX)) {
        return {
          rows: [
            {
              ...pinRowFor('0001_create_identity_control_plane.sql'),
              work_id: 'MIG-OTHER-BUILD',
            },
          ],
        };
      }
      return undefined;
    });

    await expect(runner.applyMigration(baseConfig, MIGRATION_PATH, REAL_BYTES)).rejects.toThrow(
      /work_id "MIG-OTHER-BUILD" ≠ "MIG-F3-PG-IDENTITY-001"/,
    );
    expect(log.some((q) => q === 'ROLLBACK')).toBe(true);
    expect(log.some((q) => q === 'COMMIT')).toBe(false);
    expect(log.some((q) => q.includes('CREATE TABLE'))).toBe(false);
  });

  it('verifyMigration: a DB with no recorded history validates the per-entry 0001 manifest inside a READ ONLY transaction', async () => {
    const seen: string[][] = [];
    const runner = await importRunner({
      verifySchema: (manifest) => {
        seen.push(Object.keys(manifest.tables ?? {}).sort());
        return { passed: true, diffs: [], actualTableNames: [] };
      },
      schemaManifest: {
        IDENTITY_CONTROL_PLANE_SCHEMA_MANIFEST: {
          schema: 'public',
          tables: { lu_site: {}, lu_user: {}, lu_site_membership: {}, lu_session: {} },
        },
      },
    });
    standardFilters();

    const outcome = await runner.verifyMigration(baseConfig, MIGRATION_PATH, REAL_BYTES);
    expect(outcome.verification.passed).toBe(true);
    expect(seen).toEqual([['lu_session', 'lu_site', 'lu_site_membership', 'lu_user']]);
    expect(log[0]).toBe('CONNECT');
    expect(log[1]).toBe('BEGIN ISOLATION LEVEL REPEATABLE READ READ ONLY');
    const commitIdx = log.indexOf('COMMIT');
    expect(commitIdx).toBeGreaterThan(0);
    expect(log.slice(commitIdx + 1)).toEqual(['END']);
  });

  it('verifyMigration: 0006 on a DB recorded at max ordinal 6 validates the POST_F2 final manifest', async () => {
    const seen: string[][] = [];
    const runner = await importRunner({
      verifySchema: (manifest) => {
        seen.push(Object.keys(manifest.tables ?? {}));
        return { passed: true, diffs: [], actualTableNames: [] };
      },
      f2: {
        POST_F2_CONTROL_PLANE_SCHEMA_MANIFEST: {
          schema: 'public',
          tables: {
            lu_site: {},
            lu_user: {},
            lu_session: {},
            lu_site_membership: {},
            lu_migration_history: {},
          },
        },
      },
    });
    standardFilters();
    filters.push((text) => {
      if (text.startsWith('SELECT EXISTS (')) {
        return { rows: [{ exists: true }] };
      }
      if (text.startsWith(MAX_ORDINAL_SELECT)) {
        return { rows: [{ max_ordinal: 6 }] };
      }
      return undefined;
    });

    const outcome = await runner.verifyMigration(baseConfig, cp0006Path, cp0006Bytes);
    expect(outcome.verification.passed).toBe(true);
    expect(seen.some((tables) => tables.includes('lu_migration_history'))).toBe(true);
  });

  it('cumulative replay: verify of an old entry (0001) on a DB recorded at max ordinal 6 validates the POST_F2 final schema', async () => {
    const seen: string[][] = [];
    const runner = await importRunner({
      verifySchema: (manifest) => {
        seen.push(Object.keys(manifest.tables ?? {}));
        return { passed: true, diffs: [], actualTableNames: [] };
      },
      schemaManifest: {
        IDENTITY_CONTROL_PLANE_SCHEMA_MANIFEST: {
          schema: 'public',
          tables: { lu_site: {}, lu_user: {}, lu_site_membership: {}, lu_session: {} },
        },
      },
      f2: {
        POST_F2_CONTROL_PLANE_SCHEMA_MANIFEST: {
          schema: 'public',
          tables: {
            lu_site: {},
            lu_user: {},
            lu_session: {},
            lu_site_membership: {},
            lu_migration_history: {},
          },
        },
      },
    });
    standardFilters();
    const ledger = new Map<string, Record<string, unknown>>();
    for (let i = 1; i <= 6; i += 1) {
      const e = CONTROL_PLANE_REGISTRY[i - 1];
      if (e !== undefined) {
        ledger.set(`control-plane:${i}`, pinRowFor(e.relativePath));
      }
    }
    // The pre-body probe answers TRUE: the ledger relation exists.
    filters.push(ledgerFilter(ledger, { probeAnswers: [true] }));

    await runner.verifyMigration(baseConfig, MIGRATION_PATH, REAL_BYTES);
    expect(seen.some((tables) => tables.includes('lu_migration_history'))).toBe(true);
  });

  // -------------------------------------------------------------------------
  // W16C: verify runs the same migrator preflight as up, so the executor
  // session (NOINHERIT migrator login) sees lu_migration_history through
  // `SET LOCAL ROLE lu_auth_migrator`. Without this preflight the history
  // probe silently defaults maxOrdinal=0 and the runner picks the wrong
  // (per-entry 0001) manifest on a DB already at ordinal 6.
  // -------------------------------------------------------------------------
  it('verifyMigration: runs the migrator preflight (SET LOCAL ROLE) before the history probe', async () => {
    const queryOrder: string[] = [];
    const runner = await importRunner();
    standardFilters();
    filters.push((text) => {
      queryOrder.push(text);
      return undefined;
    });

    await runner.verifyMigration(baseConfig, MIGRATION_PATH, REAL_BYTES);

    // The preflight is the same as up: 9 SELECTs against pg_roles /
    // pg_class / pg_proc + `SET LOCAL ROLE lu_auth_migrator`.
    const beginIdx = queryOrder.indexOf('BEGIN ISOLATION LEVEL REPEATABLE READ READ ONLY');
    const setLocalRoleIdx = queryOrder.indexOf('SET LOCAL ROLE lu_auth_migrator');
    const historyProbeIdx = queryOrder.findIndex((q) =>
      q.includes("table_name = 'lu_migration_history'"),
    );
    expect(beginIdx).toBeGreaterThanOrEqual(0);
    expect(setLocalRoleIdx).toBeGreaterThan(beginIdx);
    // S5 (SET LOCAL ROLE) MUST precede the history probe; without that
    // ordering the migrator role is not active and information_schema
    // would not see lu_migration_history (its owner).
    expect(historyProbeIdx).toBeGreaterThan(setLocalRoleIdx);
    // The preflight is read-only against pg_*: only SELECTs + `SET LOCAL
    // ROLE` (which is itself a session-local setting, compatible with
    // READ ONLY transactions). No INSERT/UPDATE/DELETE/DDL on business
    // tables is issued in the verify path.
    expect(
      queryOrder.some((q) =>
        /^\s*(INSERT INTO|UPDATE\s+\w+\s+SET|DELETE FROM|DROP\s+\w+|ALTER\s+\w+)/i.test(q),
      ),
    ).toBe(false);
  });

  it('verifyMigration: sees lu_migration_history under migrator role and uses max ordinal for an old pin', async () => {
    const seen: string[][] = [];
    const runner = await importRunner({
      verifySchema: (manifest) => {
        seen.push(Object.keys(manifest.tables ?? {}));
        return { passed: true, diffs: [], actualTableNames: [] };
      },
      schemaManifest: {
        IDENTITY_CONTROL_PLANE_SCHEMA_MANIFEST: {
          schema: 'public',
          tables: { lu_site: {}, lu_user: {}, lu_site_membership: {}, lu_session: {} },
        },
      },
      f2: {
        POST_F2_CONTROL_PLANE_SCHEMA_MANIFEST: {
          schema: 'public',
          tables: {
            lu_site: {},
            lu_user: {},
            lu_session: {},
            lu_site_membership: {},
            lu_migration_history: {},
          },
        },
      },
    });
    standardFilters();
    const ledger = new Map<string, Record<string, unknown>>();
    for (let i = 1; i <= 6; i += 1) {
      const e = CONTROL_PLANE_REGISTRY[i - 1];
      if (e !== undefined) {
        ledger.set(`control-plane:${i}`, pinRowFor(e.relativePath));
      }
    }
    filters.push(ledgerFilter(ledger, { probeAnswers: [true] }));

    await runner.verifyMigration(baseConfig, MIGRATION_PATH, REAL_BYTES);
    // The verified entry is 0001 (ordinal=1) but the DB is recorded at
    // max ordinal 6; the runner MUST resolve to the POST_F2 cumulative
    // manifest (which includes lu_migration_history) — NOT the 0001
    // per-entry manifest (which would lose lu_migration_history).
    expect(seen.some((tables) => tables.includes('lu_migration_history'))).toBe(true);
  });

  it('verifyMigration: fails closed when the executor session is NOT a member of lu_auth_migrator', async () => {
    const runner = await importRunner();
    filters.push(...migratorPreflightFilters({ membership: false }));

    await expect(runner.verifyMigration(baseConfig, MIGRATION_PATH, REAL_BYTES)).rejects.toThrow(
      /is not a member of role "lu_auth_migrator"/,
    );
    expect(log.some((q) => q === 'COMMIT')).toBe(false);
    expect(log.some((q) => q === 'ROLLBACK')).toBe(true);
  });

  it('verifyMigration: fails closed when the executor session has SUPERUSER', async () => {
    const runner = await importRunner();
    filters.push(
      ...migratorPreflightFilters({
        sessionRole: {
          rolname: 'lu_migrator_login',
          rolcanlogin: true,
          rolsuper: true,
          rolcreaterole: false,
          rolcreatedb: false,
          rolreplication: false,
          rolbypassrls: false,
        },
      }),
    );

    await expect(runner.verifyMigration(baseConfig, MIGRATION_PATH, REAL_BYTES)).rejects.toThrow(
      /session role is a PostgreSQL superuser/,
    );
    expect(log.some((q) => q === 'COMMIT')).toBe(false);
  });

  it('verifyMigration: fails closed when the executor session has CREATEDB', async () => {
    const runner = await importRunner();
    filters.push(
      ...migratorPreflightFilters({
        sessionRole: {
          rolname: 'lu_migrator_login',
          rolcanlogin: true,
          rolsuper: false,
          rolcreaterole: false,
          rolcreatedb: true,
          rolreplication: false,
          rolbypassrls: false,
        },
      }),
    );

    await expect(runner.verifyMigration(baseConfig, MIGRATION_PATH, REAL_BYTES)).rejects.toThrow(
      /session role has CREATEDB/,
    );
    expect(log.some((q) => q === 'COMMIT')).toBe(false);
  });

  it('cumulative replay: applyMigration replay-skip of 0001 on a DB recorded at max ordinal 6 verifies the POST_F2 final schema', async () => {
    const seen: string[][] = [];
    const runner = await importRunner({
      verifySchema: (manifest) => {
        seen.push(Object.keys(manifest.tables ?? {}));
        return { passed: true, diffs: [], actualTableNames: [] };
      },
      schemaManifest: {
        IDENTITY_CONTROL_PLANE_SCHEMA_MANIFEST: {
          schema: 'public',
          tables: { lu_site: {}, lu_user: {}, lu_site_membership: {}, lu_session: {} },
        },
      },
      f2: {
        POST_F2_CONTROL_PLANE_SCHEMA_MANIFEST: {
          schema: 'public',
          tables: {
            lu_site: {},
            lu_user: {},
            lu_session: {},
            lu_site_membership: {},
            lu_migration_history: {},
          },
        },
      },
    });
    standardFilters();
    const ledger = new Map<string, Record<string, unknown>>();
    for (let i = 1; i <= 6; i += 1) {
      const e = CONTROL_PLANE_REGISTRY[i - 1];
      if (e !== undefined) {
        ledger.set(`control-plane:${i}`, pinRowFor(e.relativePath));
      }
    }
    // The pre-body probe answers TRUE: the ledger relation exists.
    filters.push(ledgerFilter(ledger, { probeAnswers: [true] }));

    const outcome = await runner.applyMigration(baseConfig, MIGRATION_PATH, REAL_BYTES);
    expect(outcome.skipped).toEqual({ reason: 'pin-matches-history' });
    expect(seen.some((tables) => tables.includes('lu_migration_history'))).toBe(true);
  });

  it('comparePinRow (__testing): passes on an identical row and reports a null work_id with the "(null)" sentinel', async () => {
    const runner = await importRunner();
    const entry = findRegistryEntryByRelativePath('0003_create_tenant_route_catalog.sql');
    expect(entry).toBeDefined();
    expect(() =>
      runner.__testing.comparePinRow(pinRowFor('0003_create_tenant_route_catalog.sql'), entry!),
    ).not.toThrow();
    expect(() =>
      runner.__testing.comparePinRow(
        { ...pinRowFor('0003_create_tenant_route_catalog.sql'), work_id: null } as PinRow,
        entry!,
      ),
    ).toThrow(/work_id "\(null\)" ≠ "MIG-F4-ROUTE-CATALOG-008"/);
  });

  it('applyMigration: ACL-only tenant 0006 commits on supplemental PASS and locks <prefix>:<site-uuid> before its per-entry key', async () => {
    const SITE = 'A1B2C3D4-0000-4000-8000-00000000000F';
    const aclQueries: unknown[][] = [];
    const runner = await importRunner({
      tenant: {
        TENANT_SUPPLEMENTAL_EXPECTATIONS: {
          6: {
            stream: 'tenant',
            relativePath: 'tenant/0006_management_operations.sql',
            sequences: [],
            triggers: [],
            tableAcls: [
              {
                role: 'lu_tenant_runtime',
                table: 'lu_management',
                grants: [{ privilege: 'SELECT', columns: null, granted: true }],
              },
            ],
            sequenceAcls: [],
            functionAcls: [],
            schemaAcls: [],
            notes: [],
          },
        },
      },
    });
    standardFilters();
    filters.push((text, values) => {
      // The new pg_class.relacl + aclexplode table-level verifier reads
      // aclitem[] from the catalog directly (no information_schema). The
      // mock matches the new query shape so the runtime never issues an
      // information_schema.role_*_grants query.
      if (
        text.includes('pg_class') &&
        text.includes('relacl') &&
        text.includes('aclexplode') &&
        !text.includes('pg_attribute')
      ) {
        aclQueries.push(values ?? []);
        return {
          rows: [
            {
              grantee: 'lu_tenant_runtime',
              privilege_type: 'SELECT',
              is_grantable: false,
            },
          ],
        };
      }
      if (text.startsWith(HISTORY_SELECT_PREFIX)) {
        throw new Error('relation "public.lu_migration_history" does not exist');
      }
      return undefined;
    });

    const outcome = await runner.applyMigration(baseConfig, tenant0006Path, tenant0006Bytes, {
      stream: 'tenant',
      site: { siteId: SITE, migrationSecretReference: null },
    });
    expect(outcome.executed).toBe(true);
    expect(outcome.committed).toBe(true);
    expect(outcome.skipped).toBeUndefined();
    expect(outcome.historyKey).toBeUndefined();

    expect(lockKeys[0]).toBe(`${TENANT_COMMON_ADVISORY_LOCK_PREFIX}:${SITE.toLowerCase()}`);
    expect(lockKeys[1]).toBe('lu:tenant:0006-management');
    // New pg_class.relacl verifier uses ['public', table, privilege, role]
    // — the table is at index 1 and the role at index 3.
    expect(
      aclQueries.some(
        (v) => v[1] === 'lu_management' && v[2] === 'SELECT' && v[3] === 'lu_tenant_runtime',
      ),
    ).toBe(true);
    expect(
      lockKeys.some((k) => /migration_secret|TENANT_MIGRATION_CONNECTION|dsn|password/i.test(k)),
    ).toBe(false);
  });

  it('applyMigration: tenant 0006 supplemental FAIL rolls back and never commits', async () => {
    const SITE = 'aaaaaaaa-aaaa-4aaa-8aaa-aaaaaaaaaaaa';
    const runner = await importRunner({
      tenant: {
        TENANT_SUPPLEMENTAL_EXPECTATIONS: {
          6: {
            stream: 'tenant',
            relativePath: 'tenant/0006_management_operations.sql',
            sequences: [],
            triggers: [],
            tableAcls: [
              {
                role: 'lu_tenant_runtime',
                table: 'lu_management',
                grants: [{ privilege: 'SELECT', columns: null, granted: true }],
              },
            ],
            sequenceAcls: [],
            functionAcls: [],
            schemaAcls: [],
            notes: [],
          },
        },
      },
    });
    standardFilters();
    filters.push((text) => {
      // New pg_class.relacl + aclexplode verifier — empty rows mean the
      // grant is absent in the catalog, which the runner must reject.
      if (
        text.includes('pg_class') &&
        text.includes('relacl') &&
        text.includes('aclexplode') &&
        !text.includes('pg_attribute')
      ) {
        return { rows: [] };
      }
      if (text.startsWith(HISTORY_SELECT_PREFIX)) {
        throw new Error('relation "public.lu_migration_history" does not exist');
      }
      return undefined;
    });

    await expect(
      runner.applyMigration(baseConfig, tenant0006Path, tenant0006Bytes, {
        stream: 'tenant',
        site: { siteId: SITE, migrationSecretReference: null },
      }),
    ).rejects.toThrow(
      /Supplemental verification failed after executing migration tenant\/0006_management_operations\.sql: missing grant SELECT ON lu_management TO lu_tenant_runtime/,
    );
    expect(log.some((q) => q === 'ROLLBACK')).toBe(true);
    expect(log.some((q) => q === 'COMMIT')).toBe(false);
    expect(log[log.length - 1]).toBe('END');
  });

  it('applyMigration: CP 0005 commits on supplemental PASS; verifySchema validates the cumulative compose(route-final + academic) manifest at max=4', async () => {
    let verifySchemaCalls = 0;
    let verifiedManifest: unknown = null;
    const runner = await importRunner({
      verifySchema: (manifest) => {
        verifySchemaCalls += 1;
        verifiedManifest = manifest;
        return { passed: true, diffs: [], actualTableNames: [] };
      },
      f2: {
        CONTROL_PLANE_SUPPLEMENTAL_EXPECTATIONS: {
          'control-plane/0005_user_management.sql': {
            stream: 'control-plane',
            relativePath: 'control-plane/0005_user_management.sql',
            sequences: [],
            triggers: [],
            tableAcls: [
              {
                role: 'lu_user_runtime',
                table: 'lu_user',
                grants: [{ privilege: 'SELECT', columns: null, granted: true }],
              },
            ],
            sequenceAcls: [],
            functionAcls: [],
            schemaAcls: [],
            notes: [],
          },
        },
      },
    });
    standardFilters();
    filters.push((text) => {
      // New pg_class.relacl + aclexplode verifier returns is_grantable as
      // a boolean (false) instead of the information_schema 'NO' string.
      if (
        text.includes('pg_class') &&
        text.includes('relacl') &&
        text.includes('aclexplode') &&
        !text.includes('pg_attribute')
      ) {
        return {
          rows: [
            {
              grantee: 'lu_user_runtime',
              privilege_type: 'SELECT',
              is_grantable: false,
            },
          ],
        };
      }
      return undefined;
    });

    const outcome = await runner.applyMigration(baseConfig, cp0005Path, cp0005Bytes);
    expect(outcome.executed).toBe(true);
    expect(outcome.committed).toBe(true);
    // Per W13A fix #3 the fresh-apply cumulative cap at entry.ordinal=5 is
    // compose(route-final + academic); verifySchema validates that surface.
    expect(verifySchemaCalls).toBe(1);
    expect((verifiedManifest as { tables?: Record<string, unknown> } | null)?.tables).toBeDefined();
    expect(outcome.historyKey).toBeUndefined();
    expect(log.some((q) => q.startsWith('INSERT INTO public.lu_migration_history'))).toBe(false);
    expect(log[log.length - 1]).toBe('END');
  });

  it('applyMigration: CP 0005 supplemental FAIL rolls back with the ACL-only failure message', async () => {
    const runner = await importRunner({
      f2: {
        CONTROL_PLANE_SUPPLEMENTAL_EXPECTATIONS: {
          'control-plane/0005_user_management.sql': {
            stream: 'control-plane',
            relativePath: 'control-plane/0005_user_management.sql',
            sequences: [],
            triggers: [],
            tableAcls: [
              {
                role: 'lu_user_runtime',
                table: 'lu_user',
                grants: [{ privilege: 'SELECT', columns: null, granted: true }],
              },
            ],
            sequenceAcls: [],
            functionAcls: [],
            schemaAcls: [],
            notes: [],
          },
        },
      },
    });
    standardFilters();
    filters.push((text) => {
      if (
        text.includes('pg_class') &&
        text.includes('relacl') &&
        text.includes('aclexplode') &&
        !text.includes('pg_attribute')
      ) {
        return { rows: [] };
      }
      return undefined;
    });

    await expect(runner.applyMigration(baseConfig, cp0005Path, cp0005Bytes)).rejects.toThrow(
      /Supplemental verification failed after executing migration control-plane\/0005_user_management\.sql: missing grant SELECT ON lu_user TO lu_user_runtime/,
    );
    expect(log.some((q) => q === 'ROLLBACK')).toBe(true);
    expect(log.some((q) => q === 'COMMIT')).toBe(false);
    expect(log[log.length - 1]).toBe('END');
  });
});

// ---------------------------------------------------------------------------
// MIG-001-F2-W16A — proven B1 fix regressions:
//   * 0006 / 0013 re-probe INSIDE the transaction AFTER the body /
//     verification; a missing lu_migration_history at the post-body probe
//     fails the apply closed BEFORE any INSERT.
//   * 0006 / 0013 fresh apply (probe false-before / true-after) backfill
//     exact pins 1..entry.ordinal inside the same transaction; the
//     ledger is NEVER stamped against a missing relation; no catalog
//     query runs after COMMIT.
//   * clean-state-gated bootstrap 0001: if 0001's baseline tables
//     preexist AND no matching history can exist (history table
//     absent), the runner refuses to silently run the idempotent
//     body — fail closed with executed=false, committed=false, no
//     relation query.
// ---------------------------------------------------------------------------
describe('migration-runner W16A: B1 clean-state gate + post-body re-probe for history creators', () => {
  type RunnerModule = typeof import('../src/database/migration-runner.js');

  const baseConfig = {
    host: 'localhost',
    port: 5432,
    database: 'testdb',
    user: 'tester',
    password: 'secret',
    ssl: false as const,
  };

  const cp0006Path = fileURLToPath(
    new URL('../migrations/control-plane/0006_mig001_users_identity.sql', import.meta.url),
  );
  const cp0006Bytes = readFileSync(cp0006Path);
  const tenant0013Path = fileURLToPath(
    new URL('../migrations/tenant/0013_migration_history.sql', import.meta.url),
  );
  const tenant0013Bytes = readFileSync(tenant0013Path);

  const LEDGER_SELECT_PREFIX =
    'SELECT stream, ordinal, relative_path, sha256, bytes, work_id FROM public.lu_migration_history WHERE stream = $1 ORDER BY ordinal ASC';
  const PIN_SELECT_PREFIX =
    'SELECT stream, ordinal, relative_path, sha256, bytes, work_id FROM public.lu_migration_history WHERE stream = $1 AND ordinal = $2 LIMIT 1';

  const log: string[] = [];
  const queryValues: Array<{ text: string; values: unknown[] }> = [];
  const filters: QueryFilter[] = [];

  class W16AClient {
    async connect(): Promise<void> {
      log.push('CONNECT');
    }
    async query(text: string, values?: unknown[]): Promise<MockQueryResult> {
      log.push(text);
      queryValues.push({ text, values: values ?? [] });
      for (let i = filters.length - 1; i >= 0; i--) {
        const filter = filters[i] as QueryFilter;
        const result = filter(text, values);
        if (result !== undefined) return result;
      }
      return { rows: [] };
    }
    async end(): Promise<void> {
      log.push('END');
    }
  }

  async function importRunner(): Promise<RunnerModule> {
    jestRuntime.resetModules();
    unstableMockModule('pg', () => ({ Client: W16AClient }));
    unstableMockModule('../src/database/schema-manifest.js', () => ({
      IDENTITY_CONTROL_PLANE_SCHEMA_MANIFEST: { schema: 'public', tables: {} },
      AUTH_SECURITY_SCHEMA_MANIFEST: { schema: 'public', tables: {} },
      TENANT_ROUTE_SCHEMA_MANIFEST: { schema: 'public', tables: {} },
      normalizeCheckDefinition: (s: string) => s,
      normalizeDefault: (s: string | null) => s,
      normalizeIndexDefinition: (s: string) => s,
      verifySchema: async () => ({ passed: true, diffs: [], actualTableNames: [] }),
    }));
    unstableMockModule('../src/database/schema-manifest-f2.js', () => ({
      POST_F2_CONTROL_PLANE_SCHEMA_MANIFEST: {},
      POST_F2_CONTROL_PLANE_TABLES: [],
      ACADEMIC_CATALOG_SCHEMA_MANIFEST: {},
      CONTROL_PLANE_SUPPLEMENTAL_EXPECTATIONS: {},
      TENANT_FINAL_TABLES: [],
      TENANT_FINAL_SCHEMA_MANIFEST: {},
      TENANT_MIGRATION_MANIFESTS: {},
      TENANT_SUPPLEMENTAL_EXPECTATIONS: {},
    }));
    unstableMockModule('../src/database/schema-manifest-tenant.js', () => ({
      TENANT_FINAL_TABLES: [],
      TENANT_FINAL_SCHEMA_MANIFEST: {},
      TENANT_MIGRATION_MANIFESTS: {},
      TENANT_SUPPLEMENTAL_EXPECTATIONS: {},
    }));
    return import('../src/database/migration-runner.js');
  }

  /**
   * Stateful in-memory ledger filter for the W16A scenarios. The
   * `probeAnswers` array is consumed in order: every time the runner
   * issues `SELECT EXISTS (`, the next value is returned. After the
   * array is exhausted the filter defers (`undefined`) so the caller
   * can compose a final answer through a separate filter. This lets
   * each test spell out its exact pre-body / post-body expectations
   * without an implicit fallback.
   */
  function ledgerWithProbes(
    ledger: Map<string, Record<string, unknown>>,
    probeAnswers: readonly boolean[],
  ): QueryFilter {
    let probeIdx = 0;
    return (text, values) => {
      if (text.startsWith(LEDGER_SELECT_PREFIX)) {
        const stream = String(values?.[0]);
        const rows: Record<string, unknown>[] = [];
        for (const [key, value] of ledger.entries()) {
          if (key.startsWith(`${stream}:`)) rows.push(value);
        }
        return { rows };
      }
      if (text.startsWith(PIN_SELECT_PREFIX)) {
        const key = `${String(values?.[0])}:${String(values?.[1])}`;
        const row = ledger.get(key);
        return { rows: row === undefined ? [] : [row] };
      }
      if (text.startsWith('INSERT INTO public.lu_migration_history')) {
        const v = (values ?? []) as unknown[];
        const key = `${String(v[0])}:${String(v[1])}`;
        if (!ledger.has(key)) {
          ledger.set(key, {
            stream: v[0],
            ordinal: v[1],
            relative_path: v[2],
            sha256: v[3],
            bytes: v[4],
            work_id: v[5],
          });
        }
        return { rows: [] };
      }
      if (text.startsWith('SELECT EXISTS (')) {
        if (probeIdx < probeAnswers.length) {
          const answer = probeAnswers[probeIdx] === true;
          probeIdx += 1;
          return { rows: [{ exists: answer }] };
        }
        return undefined;
      }
      return undefined;
    };
  }

  function standardFilters(): void {
    filters.push(...migratorPreflightFilters());
    filters.push((text) => {
      if (text.includes('current_database()')) {
        return { rows: [{ database: 'testdb', user: 'tester', server_version: '16.0' }] };
      }
      if (
        text.includes('has_schema_privilege($1, $2, $3)') &&
        !text.includes('runtime_can_create')
      ) {
        return { rows: [{ can_create: true }] };
      }
      if (
        text.includes('information_schema.tables') ||
        text.includes('information_schema.columns')
      ) {
        return { rows: [] };
      }
      return undefined;
    });
  }

  beforeEach(() => {
    log.length = 0;
    queryValues.length = 0;
    filters.length = 0;
  });

  afterAll(() => {
    jestRuntime.resetModules();
  });

  it('applyMigration: 0006 fresh apply (probe false-before/true-after) inserts ordinals 1..6 BEFORE COMMIT and exposes historyKey (B1 fix)', async () => {
    const runner = await importRunner();
    standardFilters();
    const ledger = new Map<string, Record<string, unknown>>();
    // Probe 1 (pre-body): FALSE — 0006 is what creates the relation.
    // Probe 2 (post-body re-probe): TRUE — the body just created the
    // relation so the runner can safely backfill.
    filters.push(ledgerWithProbes(ledger, [false, true]));

    const outcome = await runner.applyMigration(baseConfig, cp0006Path, cp0006Bytes);
    expect(outcome.executed).toBe(true);
    expect(outcome.committed).toBe(true);
    expect(outcome.historyKey).toEqual({
      stream: 'control-plane',
      relativePath: 'control-plane/0006_mig001_users_identity.sql',
    });

    const commitIdx = log.indexOf('COMMIT');
    expect(commitIdx).toBeGreaterThan(0);

    // Exactly two history probes: pre-body (false) + post-body re-probe
    // (true). Both BEFORE COMMIT.
    const probeIdxs = log
      .map((q, i) => (q.startsWith('SELECT EXISTS (') ? i : -1))
      .filter((i) => i >= 0);
    expect(probeIdxs).toHaveLength(2);
    expect(probeIdxs.every((i) => i < commitIdx)).toBe(true);
    const bodyIdx = log.findIndex((q) => q.includes('CREATE TABLE'));
    expect(bodyIdx).toBeGreaterThan(probeIdxs[0]!);
    expect(probeIdxs[1]!).toBeGreaterThan(bodyIdx);

    // Exactly six ledger inserts, all BEFORE COMMIT, every pin uses
    // the registered stream/ordinal/sha256/bytes/work_id.
    const inserts = queryValues.filter((c) =>
      c.text.startsWith('INSERT INTO public.lu_migration_history'),
    );
    expect(inserts).toHaveLength(6);
    expect(inserts.map((c) => Number(c.values[1])).sort((a, b) => a - b)).toEqual([
      1, 2, 3, 4, 5, 6,
    ]);
    const insertIdxs = log
      .map((q, i) => (q.startsWith('INSERT INTO public.lu_migration_history') ? i : -1))
      .filter((i) => i >= 0);
    expect(insertIdxs.every((i) => i < commitIdx)).toBe(true);
    for (const c of inserts) {
      const entry = findRegistryEntryByRelativePath(String(c.values[2]));
      expect(entry).toBeDefined();
      expect(c.values[0]).toBe(entry?.stream);
      expect(c.values[3]).toBe(entry?.sha256);
      expect(c.values[4]).toBe(entry?.byteLength);
      expect(c.values[5]).toBe(entry?.workId);
    }

    // No catalog query after COMMIT (only client.end()).
    expect(log.slice(commitIdx + 1)).toEqual(['END']);
  });

  it('applyMigration: tenant 0013 fresh apply (probe false-before/true-after) inserts ordinals 1..13 BEFORE COMMIT and exposes historyKey (B1 fix)', async () => {
    const SITE = 'aaaaaaaa-1111-4111-8111-aaaaaaaaaaaa';
    const runner = await importRunner();
    standardFilters();
    const ledger = new Map<string, Record<string, unknown>>();
    filters.push(ledgerWithProbes(ledger, [false, true]));

    const outcome = await runner.applyMigration(baseConfig, tenant0013Path, tenant0013Bytes, {
      stream: 'tenant',
      site: { siteId: SITE, migrationSecretReference: null },
    });
    expect(outcome.executed).toBe(true);
    expect(outcome.committed).toBe(true);
    expect(outcome.historyKey).toEqual({
      stream: 'tenant',
      relativePath: 'tenant/0013_migration_history.sql',
    });

    const commitIdx = log.indexOf('COMMIT');
    expect(commitIdx).toBeGreaterThan(0);

    const probeIdxs = log
      .map((q, i) => (q.startsWith('SELECT EXISTS (') ? i : -1))
      .filter((i) => i >= 0);
    expect(probeIdxs).toHaveLength(2);
    expect(probeIdxs.every((i) => i < commitIdx)).toBe(true);

    const inserts = queryValues.filter((c) =>
      c.text.startsWith('INSERT INTO public.lu_migration_history'),
    );
    expect(inserts).toHaveLength(13);
    expect(inserts.map((c) => Number(c.values[1])).sort((a, b) => a - b)).toEqual([
      1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13,
    ]);
    const insertIdxs = log
      .map((q, i) => (q.startsWith('INSERT INTO public.lu_migration_history') ? i : -1))
      .filter((i) => i >= 0);
    expect(insertIdxs.every((i) => i < commitIdx)).toBe(true);
    for (const c of inserts) {
      const entry = findRegistryEntryByRelativePath(String(c.values[2]));
      expect(entry).toBeDefined();
      expect(c.values[0]).toBe('tenant');
    }

    expect(log.slice(commitIdx + 1)).toEqual(['END']);
  });

  it('applyMigration: post-body re-probe FALSE (history table NOT created) fails closed BEFORE any INSERT and ROLLBACKs (B1 fix)', async () => {
    const runner = await importRunner();
    standardFilters();
    const ledger = new Map<string, Record<string, unknown>>();
    // Both probes return FALSE: the pre-body probe says the relation
    // is absent, and the post-body re-probe STILL says the relation
    // is absent — meaning the body silently failed to create it.
    // The runner must refuse to backfill and must ROLLBACK.
    filters.push(ledgerWithProbes(ledger, [false, false]));

    await expect(runner.applyMigration(baseConfig, cp0006Path, cp0006Bytes)).rejects.toThrow(
      /did not create lu_migration_history in its body/,
    );

    // The body ran (the runner is allowed to apply the canonical
    // body before re-probing), but no INSERT must have happened and
    // the runner must have ROLLBACK'd, never COMMIT.
    expect(log.some((q) => q.includes('CREATE TABLE'))).toBe(true);
    expect(
      queryValues.some((c) => c.text.startsWith('INSERT INTO public.lu_migration_history')),
    ).toBe(false);
    expect(log.some((q) => q === 'ROLLBACK')).toBe(true);
    expect(log.some((q) => q === 'COMMIT')).toBe(false);
    // Two probes total — pre-body (false) and the post-body re-probe
    // (false) that triggered the ROLLBACK. No third probe (no post-
    // COMMIT query).
    const probeIdxs = log
      .map((q, i) => (q.startsWith('SELECT EXISTS (') ? i : -1))
      .filter((i) => i >= 0);
    expect(probeIdxs).toHaveLength(2);
    const rollbackIdx = log.indexOf('ROLLBACK');
    expect(rollbackIdx).toBeGreaterThan(probeIdxs[1]!);
    expect(log[log.length - 1]).toBe('END');
  });

  it('applyMigration: bootstrap 0001 preexisting tables / no history -> no body, no relation query, ROLLBACK, not committed (B1 fix)', async () => {
    const runner = await importRunner();
    standardFilters();
    // 0001's baseline tables preexist on a pre-history DB: the
    // bootstrap clean-state gate MUST fail closed. We register a
    // thrower for any direct relation query so the test asserts the
    // runner never SELECTs against the (missing) lu_migration_history
    // table once the gate fires.
    filters.push((text) => {
      if (text.includes('lu_migration_history') && !text.startsWith('SELECT EXISTS (')) {
        throw new Error(
          'runner must NOT query lu_migration_history after the bootstrap clean-state gate fires',
        );
      }
      return undefined;
    });
    // Mock findPreexistingTables: 0001's BASELINE_TARGET_TABLES are
    // lu_site, lu_user, lu_site_membership, lu_session. We make the
    // catalog report all four as pre-existing.
    filters.push((text, values) => {
      if (text.startsWith('SELECT EXISTS (')) {
        return { rows: [{ exists: false }] };
      }
      if (text.includes('information_schema.tables')) {
        const requested = (values?.[1] as string[] | undefined) ?? [];
        const rows = requested.map((table_name) => ({ table_name }));
        return { rows };
      }
      if (text.includes('information_schema.columns')) {
        return { rows: [] };
      }
      return undefined;
    });

    const outcome = await runner.applyMigration(baseConfig, MIGRATION_PATH, REAL_BYTES);
    expect(outcome.executed).toBe(false);
    expect(outcome.committed).toBe(false);
    // Preexisting list is reported so the operator can decide.
    expect(outcome.preexisting).toEqual(
      expect.arrayContaining(['lu_site', 'lu_user', 'lu_site_membership', 'lu_session']),
    );

    // Body MUST NOT run — bootstrap 0001 must not silently run the
    // idempotent body when its target tables preexist on a pre-
    // history DB.
    expect(log.some((q) => q.includes('CREATE TABLE lu_site'))).toBe(false);
    expect(log.some((q) => q.includes('CREATE TABLE lu_user'))).toBe(false);
    expect(log.some((q) => q.includes('CREATE TABLE lu_session'))).toBe(false);
    expect(log.some((q) => q.includes('CREATE TABLE lu_site_membership'))).toBe(false);
    expect(log.some((q) => q.startsWith('CREATE TABLE'))).toBe(false);

    // Exactly one history probe (the pre-body probe; no re-probe
    // because 0001 is not a strict history creator).
    expect(log.filter((q) => q.startsWith('SELECT EXISTS ('))).toHaveLength(1);
    // ROLLBACK was issued, COMMIT was NOT.
    expect(log.some((q) => q === 'ROLLBACK')).toBe(true);
    expect(log.some((q) => q === 'COMMIT')).toBe(false);
    // No catalog query after the rollback (only client.end()).
    expect(log[log.length - 1]).toBe('END');
  });

  it('applyMigration: CP 0006 preexisting tables + history present + missing pin row fails closed without running body (requiresCleanState + createsHistory)', async () => {
    const runner = await importRunner();
    standardFilters();
    const ledger = new Map<string, Record<string, unknown>>();
    // CP 0006 target tables are lu_login_identifier, lu_legacy_user_xref,
    // lu_identity_audit_event, lu_identity_migration_state, lu_migration_history
    // (IDENTITY_CONTRACT_TABLES). The catalog reports every requested
    // target as present so findPreexistingTables returns a non-empty
    // preexisting list. Pre-body probe answers TRUE (history exists),
    // but the ledger has NO pin row at ordinal 6. The
    // `requiresCleanState` partial-state gate fires and the runner
    // must fail closed (no body, no backfill, ROLLBACK).
    filters.push(ledgerWithProbes(ledger, [true]));
    filters.push((text, values) => {
      // The narrow `SELECT table_name FROM information_schema.tables WHERE
      // table_schema = $1 AND table_name = ANY($2::text[])` query is the
      // one findPreexistingTables (and inspectTarget) issues; the
      // history-probe query `SELECT EXISTS (... information_schema.tables
      // ...)` shares the substring `information_schema.tables` but is a
      // completely different shape, so the substring check ALONE is too
      // broad. Anchor on `SELECT table_name` so the mock only answers
      // for the preexisting-tables lookup.
      if (
        text.startsWith('SELECT table_name FROM information_schema.tables') &&
        text.includes('table_name = ANY($2::text[])')
      ) {
        const requested = (values?.[1] as string[] | undefined) ?? [];
        const rows = requested.map((table_name) => ({ table_name }));
        return { rows };
      }
      return undefined;
    });

    const outcome = await runner.applyMigration(baseConfig, cp0006Path, cp0006Bytes);
    expect(outcome.executed).toBe(false);
    expect(outcome.committed).toBe(false);
    expect(outcome.preexisting.length).toBeGreaterThan(0);
    expect(outcome.preexisting).toEqual(
      expect.arrayContaining([
        'lu_login_identifier',
        'lu_legacy_user_xref',
        'lu_identity_audit_event',
        'lu_identity_migration_state',
        'lu_migration_history',
      ]),
    );
    expect(outcome.verification).toBeNull();
    // Body MUST NOT run — the partial-state gate rejects BEFORE
    // the canonical body executes.
    expect(log.some((q) => q.startsWith('CREATE TABLE'))).toBe(false);
    expect(
      queryValues.some((c) => c.text.startsWith('INSERT INTO public.lu_migration_history')),
    ).toBe(false);
    // ROLLBACK was issued, COMMIT was NOT.
    expect(log.some((q) => q === 'ROLLBACK')).toBe(true);
    expect(log.some((q) => q === 'COMMIT')).toBe(false);
    // No catalog query after the rollback (only client.end()).
    expect(log[log.length - 1]).toBe('END');
  });

  it('applyMigration: tenant 0013 preexisting table + history present + missing pin row fails closed without running body (requiresCleanState + createsHistory)', async () => {
    const SITE = 'bbbbbbbb-2222-4222-8222-bbbbbbbbbbbb';
    const runner = await importRunner();
    standardFilters();
    const ledger = new Map<string, Record<string, unknown>>();
    // Tenant 0013 target tables is just lu_migration_history. The
    // catalog reports it as present so findPreexistingTables returns
    // ['lu_migration_history']. Pre-body probe answers TRUE (history
    // exists), but the ledger has NO pin row at ordinal 13. The
    // `requiresCleanState` partial-state gate fires and the runner
    // must fail closed (no body, no backfill, ROLLBACK).
    filters.push(ledgerWithProbes(ledger, [true]));
    filters.push((text, values) => {
      if (
        text.startsWith('SELECT table_name FROM information_schema.tables') &&
        text.includes('table_name = ANY($2::text[])')
      ) {
        const requested = (values?.[1] as string[] | undefined) ?? [];
        const rows = requested.map((table_name) => ({ table_name }));
        return { rows };
      }
      return undefined;
    });

    const outcome = await runner.applyMigration(baseConfig, tenant0013Path, tenant0013Bytes, {
      stream: 'tenant',
      site: { siteId: SITE, migrationSecretReference: null },
    });
    expect(outcome.executed).toBe(false);
    expect(outcome.committed).toBe(false);
    expect(outcome.preexisting).toEqual(['lu_migration_history']);
    expect(outcome.verification).toBeNull();
    // Body MUST NOT run — the partial-state gate rejects BEFORE
    // the canonical body executes.
    expect(log.some((q) => q.startsWith('CREATE TABLE'))).toBe(false);
    expect(
      queryValues.some((c) => c.text.startsWith('INSERT INTO public.lu_migration_history')),
    ).toBe(false);
    // ROLLBACK was issued, COMMIT was NOT.
    expect(log.some((q) => q === 'ROLLBACK')).toBe(true);
    expect(log.some((q) => q === 'COMMIT')).toBe(false);
    // No catalog query after the rollback (only client.end()).
    expect(log[log.length - 1]).toBe('END');
  });

  it('applyMigration: CP 0006 fresh apply (false-before / true-after) enforces query order: 2 probes before COMMIT, body between, no catalog query after COMMIT (B1 fix)', async () => {
    const runner = await importRunner();
    standardFilters();
    const ledger = new Map<string, Record<string, unknown>>();
    filters.push(ledgerWithProbes(ledger, [false, true]));

    const outcome = await runner.applyMigration(baseConfig, cp0006Path, cp0006Bytes);
    expect(outcome.executed).toBe(true);
    expect(outcome.committed).toBe(true);

    // Query order contract: pre-body probe (false), body, post-body
    // re-probe (true), backfill inserts (1..6), inspect, COMMIT.
    // Exactly two SELECT EXISTS probes, both BEFORE COMMIT, with the
    // body running BETWEEN them. No catalog query after COMMIT.
    const probeIdxs = log
      .map((q, i) => (q.startsWith('SELECT EXISTS (') ? i : -1))
      .filter((i) => i >= 0);
    expect(probeIdxs).toHaveLength(2);
    const commitIdx = log.indexOf('COMMIT');
    expect(commitIdx).toBeGreaterThan(0);
    expect(probeIdxs.every((i) => i < commitIdx)).toBe(true);

    const bodyIdx = log.findIndex((q) => q.includes('CREATE TABLE'));
    expect(bodyIdx).toBeGreaterThan(0);
    expect(probeIdxs[0]!).toBeLessThan(bodyIdx);
    expect(probeIdxs[1]!).toBeGreaterThan(bodyIdx);

    // INSERTs happen after the second probe (re-probe must confirm
    // the relation before any ledger row is written) and BEFORE COMMIT.
    const insertIdxs = log
      .map((q, i) => (q.startsWith('INSERT INTO public.lu_migration_history') ? i : -1))
      .filter((i) => i >= 0);
    expect(insertIdxs.length).toBe(6);
    expect(insertIdxs.every((i) => i > probeIdxs[1]!)).toBe(true);
    expect(insertIdxs.every((i) => i < commitIdx)).toBe(true);

    // Inspection runs AFTER the backfill but BEFORE COMMIT.
    const inspectIdx = log.findIndex((q) => q.includes('current_database()'));
    expect(inspectIdx).toBeGreaterThan(insertIdxs[insertIdxs.length - 1]!);
    expect(inspectIdx).toBeLessThan(commitIdx);

    // Only client.end() follows COMMIT — no information_schema query,
    // no SELECT EXISTS, no INSERT, no SELECT against the ledger.
    expect(log.slice(commitIdx + 1)).toEqual(['END']);
  });
});

// ---------------------------------------------------------------------------
// MIG-001-F2-W13A — accepted review regressions:
//
//   1. History probe + live transaction safety.
//   3. Cumulative CP manifests (max1 identity; max2 auth final;
//      max3 tenant-route final; max4/5 compose route-final + academic;
//      max>=6 POST_F2). Tenant remains merged/final.
//   4. Supplemental merge uses chronological entries through cap and
//      LAST-ORDINAL WINS for identical expectations.
//   5. Migrator preflight on APPLY only.
//   6. Supplemental verifier exactness using existing W8 expectation fields.
//   7. Replay/verify no-pin path semantics.
// ---------------------------------------------------------------------------
describe('migration-runner W13A: ledger contiguity, supplemental merge, migrator preflight, verifier exactness', () => {
  type RunnerModule = typeof import('../src/database/migration-runner.js');

  const baseConfig = {
    host: 'localhost',
    port: 5432,
    database: 'testdb',
    user: 'tester',
    password: 'secret',
    ssl: false as const,
  };
  const cp0005Path = fileURLToPath(
    new URL('../migrations/control-plane/0005_user_management.sql', import.meta.url),
  );
  const cp0005Bytes = readFileSync(cp0005Path);
  const cp0006Path = fileURLToPath(
    new URL('../migrations/control-plane/0006_mig001_users_identity.sql', import.meta.url),
  );
  const cp0006Bytes = readFileSync(cp0006Path);

  const LEDGER_SELECT_PREFIX =
    'SELECT stream, ordinal, relative_path, sha256, bytes, work_id FROM public.lu_migration_history WHERE stream = $1 ORDER BY ordinal ASC';
  const PIN_SELECT_PREFIX =
    'SELECT stream, ordinal, relative_path, sha256, bytes, work_id FROM public.lu_migration_history WHERE stream = $1 AND ordinal = $2 LIMIT 1';
  const MAX_ORDINAL_SELECT =
    'SELECT COALESCE(MAX(ordinal), 0) AS max_ordinal FROM public.lu_migration_history WHERE stream = $1';
  const SESSION_ROLE_SELECT =
    'SELECT rolname, rolcanlogin, rolsuper, rolcreaterole, rolcreatedb, rolreplication, rolbypassrls FROM pg_roles WHERE rolname = current_user';
  const MIGRATOR_ROLE_SELECT =
    'SELECT rolname, rolcanlogin, rolsuper, rolcreaterole, rolcreatedb,\n       rolreplication, rolbypassrls\n       FROM pg_roles WHERE rolname = $1';
  const MEMBERSHIP_SELECT = "SELECT pg_has_role(current_user, $1, 'MEMBER') AS is_member";
  const RUNTIME_ROLE_SELECT =
    'SELECT rolname, rolcanlogin, rolsuper, rolcreaterole, rolcreatedb,\n       rolreplication, rolbypassrls\n       FROM pg_roles WHERE rolname = $1';
  const CURRENT_ACTOR_SELECT = 'SELECT current_user AS current_actor';
  const RUNTIME_CREATE_SELECT =
    "SELECT has_schema_privilege($1, $2, 'CREATE') AS runtime_can_create";
  const RUNTIME_OWNS_SELECT = 'SELECT\n       EXISTS(';

  const log: string[] = [];
  const lockKeys: string[] = [];
  const filters: QueryFilter[] = [];

  class W13AClient {
    async connect(): Promise<void> {
      log.push('CONNECT');
    }
    async query(text: string, values?: unknown[]): Promise<MockQueryResult> {
      log.push(text);
      if (text.includes('pg_advisory_xact_lock')) {
        lockKeys.push(String(values?.[0] ?? ''));
      }
      for (let i = filters.length - 1; i >= 0; i--) {
        const filter = filters[i] as QueryFilter;
        const result = filter(text, values);
        if (result !== undefined) return result;
      }
      return { rows: [] };
    }
    async end(): Promise<void> {
      log.push('END');
    }
  }

  function pinRowFor(relativePath: string): Record<string, unknown> {
    const entry = findRegistryEntryByRelativePath(relativePath);
    if (entry === undefined) {
      throw new Error(`test setup: ${relativePath} is not registered`);
    }
    return {
      stream: entry.stream,
      ordinal: entry.ordinal,
      relative_path: entry.relativePath,
      sha256: entry.sha256,
      bytes: entry.byteLength,
      work_id: entry.workId,
    };
  }

  async function importRunner(
    opts: {
      verifySchema?: (manifest: { tables?: Record<string, unknown> }) => {
        passed: boolean;
        diffs: string[];
        actualTableNames: string[];
      };
      schemaManifest?: Record<string, unknown>;
      f2?: Record<string, unknown>;
      tenant?: Record<string, unknown>;
    } = {},
  ): Promise<RunnerModule> {
    jestRuntime.resetModules();
    unstableMockModule('pg', () => ({ Client: W13AClient }));
    unstableMockModule('../src/database/schema-manifest.js', () => ({
      IDENTITY_CONTROL_PLANE_SCHEMA_MANIFEST: { schema: 'public', tables: {} },
      AUTH_SECURITY_SCHEMA_MANIFEST: { schema: 'public', tables: {} },
      TENANT_ROUTE_SCHEMA_MANIFEST: { schema: 'public', tables: {} },
      normalizeCheckDefinition: (s: string) => s,
      normalizeDefault: (s: string | null) => s,
      normalizeIndexDefinition: (s: string) => s,
      verifySchema: async (_client: unknown, manifest: { tables?: Record<string, unknown> }) =>
        (opts.verifySchema ?? (() => ({ passed: true, diffs: [], actualTableNames: [] })))(
          manifest,
        ),
      ...opts.schemaManifest,
    }));
    unstableMockModule('../src/database/schema-manifest-f2.js', () => ({
      POST_F2_CONTROL_PLANE_SCHEMA_MANIFEST: {},
      POST_F2_CONTROL_PLANE_TABLES: [],
      ACADEMIC_CATALOG_SCHEMA_MANIFEST: {},
      CONTROL_PLANE_SUPPLEMENTAL_EXPECTATIONS: {},
      ...opts.f2,
    }));
    unstableMockModule('../src/database/schema-manifest-tenant.js', () => ({
      TENANT_FINAL_TABLES: [],
      TENANT_FINAL_SCHEMA_MANIFEST: {},
      TENANT_MIGRATION_MANIFESTS: {},
      TENANT_SUPPLEMENTAL_EXPECTATIONS: {},
      ...opts.tenant,
    }));
    return import('../src/database/migration-runner.js');
  }

  function ledgerFilter(
    ledger: Map<string, Record<string, unknown>>,
    opts: { probeAnswers?: readonly boolean[] } = {},
  ): QueryFilter {
    const probeAnswers = opts.probeAnswers ?? [];
    let probeIdx = 0;
    return (text, values) => {
      if (text.startsWith(LEDGER_SELECT_PREFIX)) {
        const stream = String(values?.[0]);
        const rows: Record<string, unknown>[] = [];
        for (const [key, value] of ledger.entries()) {
          if (key.startsWith(`${stream}:`)) rows.push(value);
        }
        return { rows };
      }
      if (text.startsWith(PIN_SELECT_PREFIX)) {
        const key = `${String(values?.[0])}:${String(values?.[1])}`;
        const row = ledger.get(key);
        return { rows: row === undefined ? [] : [row] };
      }
      if (text.startsWith('INSERT INTO public.lu_migration_history')) {
        const v = (values ?? []) as unknown[];
        const key = `${String(v[0])}:${String(v[1])}`;
        if (!ledger.has(key)) {
          ledger.set(key, {
            stream: v[0],
            ordinal: v[1],
            relative_path: v[2],
            sha256: v[3],
            bytes: v[4],
            work_id: v[5],
          });
        }
        return { rows: [] };
      }
      if (text.startsWith('SELECT EXISTS (')) {
        // Explicit probe-answer array — same W16A semantic as
        // the W10 ledgerFilter and `ledgerWithProbes`. Each
        // call consumes the next entry; once exhausted the
        // filter defers so the caller's `standardFilters` answer
        // can win. This replaces the W10/W13A implicit
        // `preBodyProbe` deferral.
        if (probeIdx < probeAnswers.length) {
          const answer = probeAnswers[probeIdx] === true;
          probeIdx += 1;
          return { rows: [{ exists: answer }] };
        }
        return undefined;
      }
      if (text.startsWith(MAX_ORDINAL_SELECT)) {
        let max = 0;
        for (const [key, value] of ledger.entries()) {
          if (!key.startsWith(`${String(values?.[0])}:`)) continue;
          const ord = Number(value.ordinal);
          if (Number.isFinite(ord) && ord > max) max = ord;
        }
        return { rows: [{ max_ordinal: max }] };
      }
      return undefined;
    };
  }

  beforeEach(() => {
    log.length = 0;
    lockKeys.length = 0;
    filters.length = 0;
  });

  afterAll(() => {
    jestRuntime.resetModules();
  });

  // -------------------------------------------------------------------------
  // Fix 1 — history probe + live transaction safety.
  // -------------------------------------------------------------------------
  it('fix-1: absent-history probe leaves the transaction usable so a fresh 0001 can still proceed', async () => {
    const runner = await importRunner();
    filters.push(...migratorPreflightFilters());
    filters.push((text) => {
      if (text.includes('current_database()')) {
        return { rows: [{ database: 'testdb', user: 'tester', server_version: '16.0' }] };
      }
      if (text.includes('has_schema_privilege($1, $2, $3)')) {
        return { rows: [{ can_create: true }] };
      }
      if (text.startsWith('SELECT EXISTS (')) {
        return { rows: [{ exists: false }] };
      }
      if (
        text.includes('information_schema.tables') ||
        text.includes('information_schema.columns')
      ) {
        return { rows: [] };
      }
      return undefined;
    });

    const outcome = await runner.applyMigration(baseConfig, MIGRATION_PATH, REAL_BYTES);
    expect(outcome.executed).toBe(true);
    expect(outcome.committed).toBe(true);
    expect(outcome.historyKey).toBeUndefined();
    // Exactly one history probe; no swallowed 42P01 error.
    expect(log.filter((q) => q.startsWith('SELECT EXISTS ('))).toHaveLength(1);
    // The 0001 body DID run — the absent-history probe did not poison the
    // live SERIALIZABLE transaction.
    expect(log.some((q) => q.includes('CREATE TABLE lu_site'))).toBe(true);
    expect(log[log.length - 1]).toBe('END');
  });

  // -------------------------------------------------------------------------
  // Fix 2 — ledger contiguous + pin-correct check.
  // -------------------------------------------------------------------------
  it('fix-2: ledger gap (missing old row with later max) rejects the apply before any body execution', async () => {
    const runner = await importRunner();
    filters.push(...migratorPreflightFilters());
    filters.push((text) => {
      if (text.includes('current_database()')) {
        return { rows: [{ database: 'testdb', user: 'tester', server_version: '16.0' }] };
      }
      if (text.includes('has_schema_privilege($1, $2, $3)')) {
        return { rows: [{ can_create: true }] };
      }
      if (text.startsWith('SELECT EXISTS (')) {
        return { rows: [{ exists: true }] };
      }
      if (
        text.includes('information_schema.tables') ||
        text.includes('information_schema.columns')
      ) {
        return { rows: [] };
      }
      return undefined;
    });
    // Ledger has row at ordinal 1 and 3, but NOT at ordinal 2 — a gap.
    const ledger = new Map<string, Record<string, unknown>>([
      ['control-plane:1', pinRowFor('0001_create_identity_control_plane.sql')],
      ['control-plane:3', pinRowFor('0003_create_tenant_route_catalog.sql')],
    ]);
    // Pre-body probe answers TRUE; the runner's contiguous-pin
    // check runs first and rejects the gap BEFORE the body.
    filters.push(ledgerFilter(ledger, { probeAnswers: [true] }));

    await expect(runner.applyMigration(baseConfig, cp0006Path, cp0006Bytes)).rejects.toThrow(
      /lu_migration_history stream "control-plane" has a gap at ordinal 2 \(recorded max=3\)/,
    );
    // No body, no advisory locks taken after BEGIN — preflight + locks DID
    // run, but the gap-rejecting ledger check fired BEFORE the body.
    expect(log.some((q) => q.includes('CREATE TABLE'))).toBe(false);
    expect(log.some((q) => q === 'ROLLBACK')).toBe(true);
    expect(log[log.length - 1]).toBe('END');
  });

  it('fix-2: ledger tamper (mismatched bytes on an old row) rejects the apply before any body execution', async () => {
    const runner = await importRunner();
    filters.push(...migratorPreflightFilters());
    filters.push((text) => {
      if (text.includes('current_database()')) {
        return { rows: [{ database: 'testdb', user: 'tester', server_version: '16.0' }] };
      }
      if (text.includes('has_schema_privilege($1, $2, $3)')) {
        return { rows: [{ can_create: true }] };
      }
      if (text.startsWith('SELECT EXISTS (')) {
        return { rows: [{ exists: true }] };
      }
      if (
        text.includes('information_schema.tables') ||
        text.includes('information_schema.columns')
      ) {
        return { rows: [] };
      }
      return undefined;
    });
    // Ledger is contiguous 1..3 but ordinal 2 has a tampered sha256.
    const ledger = new Map<string, Record<string, unknown>>([
      ['control-plane:1', pinRowFor('0001_create_identity_control_plane.sql')],
      [
        'control-plane:2',
        { ...pinRowFor('0002_create_auth_security_controls.sql'), sha256: 'a'.repeat(64) },
      ],
      ['control-plane:3', pinRowFor('0003_create_tenant_route_catalog.sql')],
    ]);
    // Pre-body probe answers TRUE; the runner's contiguous-pin
    // check runs first and rejects the tamper BEFORE the body.
    filters.push(ledgerFilter(ledger, { probeAnswers: [true] }));

    await expect(runner.applyMigration(baseConfig, cp0006Path, cp0006Bytes)).rejects.toThrow(
      /lu_migration_history row for 0002_create_auth_security_controls\.sql is immutable and conflicts with the registered pin/,
    );
    expect(log.some((q) => q.includes('CREATE TABLE'))).toBe(false);
    expect(log.some((q) => q === 'ROLLBACK')).toBe(true);
    expect(log[log.length - 1]).toBe('END');
  });

  // -------------------------------------------------------------------------
  // Fix 3 — cumulative CP manifest ladder.
  // -------------------------------------------------------------------------
  it('fix-3: cumulativeManifestFor resolves max=1..max=5 and max>=6 to the documented ladder', async () => {
    const runner = await importRunner({
      schemaManifest: {
        IDENTITY_CONTROL_PLANE_SCHEMA_MANIFEST: {
          schema: 'public',
          tables: { lu_identity_only: {} },
        },
        AUTH_SECURITY_SCHEMA_MANIFEST: {
          schema: 'public',
          tables: { lu_auth_final: {} },
        },
        TENANT_ROUTE_SCHEMA_MANIFEST: {
          schema: 'public',
          tables: { lu_route_final: {} },
        },
      },
      f2: {
        POST_F2_CONTROL_PLANE_SCHEMA_MANIFEST: {
          schema: 'public',
          tables: { lu_post_f2: {} },
        },
        ACADEMIC_CATALOG_SCHEMA_MANIFEST: {
          schema: 'public',
          tables: { lu_academic: {} },
        },
      },
    });
    const entry = findRegistryEntryByRelativePath('0001_create_identity_control_plane.sql')!;
    expect(
      Object.keys(
        (runner.__testing.cumulativeManifestFor(entry, 1) as { tables: Record<string, unknown> })
          .tables,
      ),
    ).toEqual(['lu_identity_only']);
    expect(
      Object.keys(
        (runner.__testing.cumulativeManifestFor(entry, 2) as { tables: Record<string, unknown> })
          .tables,
      ),
    ).toEqual(['lu_auth_final']);
    expect(
      Object.keys(
        (runner.__testing.cumulativeManifestFor(entry, 3) as { tables: Record<string, unknown> })
          .tables,
      ),
    ).toEqual(['lu_route_final']);
    const composed45 = runner.__testing.cumulativeManifestFor(entry, 4) as {
      tables: Record<string, unknown>;
    };
    expect(Object.keys(composed45.tables).sort()).toEqual(['lu_academic', 'lu_route_final']);
    const composed55 = runner.__testing.cumulativeManifestFor(entry, 5) as {
      tables: Record<string, unknown>;
    };
    expect(Object.keys(composed55.tables).sort()).toEqual(['lu_academic', 'lu_route_final']);
    expect(
      Object.keys(
        (runner.__testing.cumulativeManifestFor(entry, 6) as { tables: Record<string, unknown> })
          .tables,
      ),
    ).toEqual(['lu_post_f2']);
  });

  it('fix-3: tenant cumulativeManifestFor remains merged/final (cap<13 merges per-ordinal)', async () => {
    const runner = await importRunner({
      tenant: {
        TENANT_MIGRATION_MANIFESTS: {
          1: {
            ordinal: 1,
            relativePath: 'tenant/0001_dashboard_foundation.sql',
            schemaManifest: { schema: 'public', tables: { t1: {} } },
            supplemental: {
              stream: 'tenant',
              relativePath: 'tenant/0001_dashboard_foundation.sql',
              sequences: [],
              triggers: [],
              tableAcls: [],
              sequenceAcls: [],
              functionAcls: [],
              schemaAcls: [],
              notes: [],
            },
          },
          2: {
            ordinal: 2,
            relativePath: 'tenant/0002_catalogs.sql',
            schemaManifest: { schema: 'public', tables: { t2: {} } },
            supplemental: {
              stream: 'tenant',
              relativePath: 'tenant/0002_catalogs.sql',
              sequences: [],
              triggers: [],
              tableAcls: [],
              sequenceAcls: [],
              functionAcls: [],
              schemaAcls: [],
              notes: [],
            },
          },
        } as never,
        TENANT_FINAL_SCHEMA_MANIFEST: {
          schema: 'public',
          tables: { t_final: {} },
        } as never,
      },
    });
    const entry = findRegistryEntryByRelativePath('tenant/0001_dashboard_foundation.sql')!;
    const merged = runner.__testing.cumulativeManifestFor(entry, 2) as {
      tables: Record<string, unknown>;
    };
    expect(Object.keys(merged.tables).sort()).toEqual(['t1', 't2']);
    const final13 = runner.__testing.cumulativeManifestFor(entry, 13) as {
      tables: Record<string, unknown>;
    };
    expect(Object.keys(final13.tables)).toEqual(['t_final']);
  });

  it('fix-3: inspectionTablesFor covers the FINAL current state when a recorded max is present (CP POST_F2, tenant TENANT_FINAL)', async () => {
    const runner = await importRunner({
      tenant: {
        TENANT_FINAL_TABLES: ['t1', 't2', 't3'],
      },
    });
    const entry = findRegistryEntryByRelativePath('tenant/0001_dashboard_foundation.sql')!;
    // Tenant stream always inspects the final table list (regardless of
    // recorded max); this is the parity contract for verify/replay/final.
    expect(runner.__testing.inspectionTablesFor(entry, { stream: 'tenant' }, 0)).toEqual([
      't1',
      't2',
      't3',
    ]);
    expect(runner.__testing.inspectionTablesFor(entry, { stream: 'tenant' }, 6)).toEqual([
      't1',
      't2',
      't3',
    ]);
    // CP replay: maxOrdinal >= 6 → POST_F2 table set.
    const cpEntry = findRegistryEntryByRelativePath('0001_create_identity_control_plane.sql')!;
    expect(runner.__testing.inspectionTablesFor(cpEntry, {}, 6).length).toBeGreaterThan(0);
    // Fresh CP DB (maxOrdinal=0, ordinal < 6): per-entry touched tables.
    expect(runner.__testing.inspectionTablesFor(cpEntry, {}, 0).length).toBeGreaterThan(0);
  });

  // -------------------------------------------------------------------------
  // Fix 4 — supplemental merge with LAST-ORDINAL-WINS dedup.
  // -------------------------------------------------------------------------
  it('fix-4: CP0005 table-level INSERT/UPDATE true is overridden by CP0006 table-level false; CP0006 column-level grants remain true', async () => {
    const runner = await importRunner();
    const cp0005: LocalSupplemental = {
      stream: 'control-plane',
      relativePath: 'control-plane/0005_user_management.sql',
      sequences: [],
      triggers: [],
      tableAcls: [
        {
          role: 'lu_auth_runtime',
          table: 'lu_user',
          grants: [
            { privilege: 'SELECT', columns: null, granted: true },
            { privilege: 'INSERT', columns: null, granted: true },
            { privilege: 'UPDATE', columns: null, granted: true },
          ],
        },
      ],
      sequenceAcls: [],
      functionAcls: [],
      schemaAcls: [],
      notes: [],
    };
    const cp0006: LocalSupplemental = {
      stream: 'control-plane',
      relativePath: 'control-plane/0006_mig001_users_identity.sql',
      sequences: [],
      triggers: [],
      tableAcls: [
        {
          role: 'lu_auth_runtime',
          table: 'lu_user',
          grants: [
            { privilege: 'INSERT', columns: null, granted: false },
            { privilege: 'UPDATE', columns: null, granted: false },
            { privilege: 'DELETE', columns: null, granted: false },
            { privilege: 'TRUNCATE', columns: null, granted: false },
            { privilege: 'SELECT', columns: null, granted: true },
            {
              privilege: 'INSERT',
              columns: ['id', 'email', 'full_name'],
              granted: true,
            },
            {
              privilege: 'UPDATE',
              columns: ['email', 'full_name', 'security_version'],
              granted: true,
            },
          ],
        },
      ],
      sequenceAcls: [],
      functionAcls: [],
      schemaAcls: [],
      notes: [],
    };
    const merged = runner.__testing.dedupTableAcls([
      asSupplemental(cp0005) as never,
      asSupplemental(cp0006) as never,
    ]);
    const group = merged[0];
    expect(group).toBeDefined();
    const tableLevel = group!.grants.filter((g) => g.columns === null);
    const columnLevel = group!.grants.filter((g) => g.columns !== null);
    // Table-level: CP0006 wins.
    const tableInsert = tableLevel.find((g) => g.privilege === 'INSERT');
    const tableUpdate = tableLevel.find((g) => g.privilege === 'UPDATE');
    const tableSelect = tableLevel.find((g) => g.privilege === 'SELECT');
    const tableDelete = tableLevel.find((g) => g.privilege === 'DELETE');
    const tableTruncate = tableLevel.find((g) => g.privilege === 'TRUNCATE');
    expect(tableInsert?.granted).toBe(false);
    expect(tableUpdate?.granted).toBe(false);
    expect(tableSelect?.granted).toBe(true);
    expect(tableDelete?.granted).toBe(false);
    expect(tableTruncate?.granted).toBe(false);
    // Column-level: CP0006 column grants survive (different dedup key).
    expect(columnLevel.length).toBe(2);
    expect(columnLevel.find((g) => g.privilege === 'INSERT')?.granted).toBe(true);
    expect(columnLevel.find((g) => g.privilege === 'UPDATE')?.granted).toBe(true);
  });

  it('fix-4: no contradictory final tuple — table-level INSERT false + column-level INSERT true are preserved as separate rows', async () => {
    const runner = await importRunner();
    const expectations: LocalSupplemental[] = [
      {
        stream: 'control-plane',
        relativePath: 'control-plane/0005_user_management.sql',
        sequences: [],
        triggers: [],
        tableAcls: [
          {
            role: 'lu_auth_runtime',
            table: 'lu_user',
            grants: [{ privilege: 'INSERT', columns: null, granted: true }],
          },
        ],
        sequenceAcls: [],
        functionAcls: [],
        schemaAcls: [],
        notes: [],
      },
      {
        stream: 'control-plane',
        relativePath: 'control-plane/0006_mig001_users_identity.sql',
        sequences: [],
        triggers: [],
        tableAcls: [
          {
            role: 'lu_auth_runtime',
            table: 'lu_user',
            grants: [
              { privilege: 'INSERT', columns: null, granted: false },
              { privilege: 'INSERT', columns: ['id', 'email'], granted: true },
            ],
          },
        ],
        sequenceAcls: [],
        functionAcls: [],
        schemaAcls: [],
        notes: [],
      },
    ];
    const merged = runner.__testing.dedupTableAcls(
      expectations.map((e) => asSupplemental(e)) as never,
    );
    const group = merged[0]!;
    const tableLevel = group.grants.filter((g) => g.columns === null);
    const columnLevel = group.grants.filter((g) => g.columns !== null);
    expect(tableLevel.length).toBe(1);
    expect(tableLevel[0]!.granted).toBe(false);
    expect(columnLevel.length).toBe(1);
    expect(columnLevel[0]!.granted).toBe(true);
    expect(columnLevel[0]!.columns).toEqual(['id', 'email']);
  });

  // -------------------------------------------------------------------------
  // Fix 5 — migrator preflight.
  // -------------------------------------------------------------------------
  it('fix-5: preflight rejects superuser session role without taking any lock or body', async () => {
    const runner = await importRunner();
    filters.push(
      ...migratorPreflightFilters({
        sessionRole: {
          rolname: 'lu_migrator_login',
          rolcanlogin: true,
          rolsuper: true,
          rolcreaterole: false,
          rolcreatedb: false,
          rolreplication: false,
          rolbypassrls: false,
        },
      }),
    );

    await expect(runner.applyMigration(baseConfig, MIGRATION_PATH, REAL_BYTES)).rejects.toThrow(
      /session role is a PostgreSQL superuser/,
    );
    expect(log.some((q) => q === 'BEGIN ISOLATION LEVEL SERIALIZABLE')).toBe(true);
    expect(log.some((q) => q.includes('pg_advisory_xact_lock'))).toBe(false);
    expect(log.some((q) => q.includes('CREATE TABLE'))).toBe(false);
    expect(log.some((q) => q === 'COMMIT')).toBe(false);
    expect(log.some((q) => q === 'ROLLBACK')).toBe(true);
    expect(log[log.length - 1]).toBe('END');
  });

  it('fix-5: preflight rejects runtime-role session without taking any lock or body', async () => {
    const runner = await importRunner();
    filters.push(
      ...migratorPreflightFilters({
        sessionRole: {
          rolname: 'lu_auth_runtime',
          rolcanlogin: true,
          rolsuper: false,
          rolcreaterole: false,
          rolcreatedb: false,
          rolreplication: false,
          rolbypassrls: false,
        },
      }),
    );
    await expect(runner.applyMigration(baseConfig, MIGRATION_PATH, REAL_BYTES)).rejects.toThrow(
      /session role "lu_auth_runtime" is a managed identity/,
    );
    expect(log.some((q) => q.includes('pg_advisory_xact_lock'))).toBe(false);
    expect(log.some((q) => q.includes('CREATE TABLE'))).toBe(false);
  });

  it('fix-5: preflight rejects missing migrator role without taking any lock or body', async () => {
    const runner = await importRunner();
    filters.push(...migratorPreflightFilters({ migratorRole: null }));
    await expect(runner.applyMigration(baseConfig, MIGRATION_PATH, REAL_BYTES)).rejects.toThrow(
      /role "lu_auth_migrator" is absent/,
    );
    expect(log.some((q) => q.includes('pg_advisory_xact_lock'))).toBe(false);
    expect(log.some((q) => q.includes('CREATE TABLE'))).toBe(false);
  });

  it('fix-5: preflight rejects migrator-with-LOGIN without taking any lock or body', async () => {
    const runner = await importRunner();
    filters.push(
      ...migratorPreflightFilters({
        migratorRole: { rolname: 'lu_auth_migrator', rolcanlogin: true },
      }),
    );
    await expect(runner.applyMigration(baseConfig, MIGRATION_PATH, REAL_BYTES)).rejects.toThrow(
      /must be NOLOGIN/,
    );
    expect(log.some((q) => q.includes('pg_advisory_xact_lock'))).toBe(false);
    expect(log.some((q) => q.includes('CREATE TABLE'))).toBe(false);
  });

  it('fix-5: preflight rejects non-member session without taking any lock or body', async () => {
    const runner = await importRunner();
    filters.push(...migratorPreflightFilters({ membership: false }));
    await expect(runner.applyMigration(baseConfig, MIGRATION_PATH, REAL_BYTES)).rejects.toThrow(
      /is not a member of role "lu_auth_migrator"/,
    );
    expect(log.some((q) => q.includes('pg_advisory_xact_lock'))).toBe(false);
    expect(log.some((q) => q.includes('CREATE TABLE'))).toBe(false);
  });

  it('fix-5: preflight rejects lu_auth_runtime CREATE on public without taking any lock or body', async () => {
    const runner = await importRunner();
    filters.push(...migratorPreflightFilters({ runtimeCanCreate: true }));
    await expect(runner.applyMigration(baseConfig, MIGRATION_PATH, REAL_BYTES)).rejects.toThrow(
      /must NOT have CREATE on schema "public"/,
    );
    expect(log.some((q) => q.includes('pg_advisory_xact_lock'))).toBe(false);
    expect(log.some((q) => q.includes('CREATE TABLE'))).toBe(false);
  });

  it('fix-5: preflight rejects lu_auth_runtime owning migrable public tables without taking any lock or body', async () => {
    const runner = await importRunner();
    filters.push(...migratorPreflightFilters({ runtimeOwnsTables: true }));
    await expect(runner.applyMigration(baseConfig, MIGRATION_PATH, REAL_BYTES)).rejects.toThrow(
      /owns migrable public objects/,
    );
    expect(log.some((q) => q.includes('pg_advisory_xact_lock'))).toBe(false);
    expect(log.some((q) => q.includes('CREATE TABLE'))).toBe(false);
  });

  it('fix-5: preflight rejects lu_auth_runtime owning migrable public functions without taking any lock or body', async () => {
    const runner = await importRunner();
    filters.push(...migratorPreflightFilters({ runtimeOwnsFunctions: true }));
    await expect(runner.applyMigration(baseConfig, MIGRATION_PATH, REAL_BYTES)).rejects.toThrow(
      /owns migrable public objects/,
    );
    expect(log.some((q) => q.includes('pg_advisory_xact_lock'))).toBe(false);
  });

  it('fix-5: accepted order is BEGIN -> SET search_path -> identity query -> SET LOCAL ROLE -> privilege checks -> common lock -> file lock', async () => {
    const runner = await importRunner();
    filters.push(...migratorPreflightFilters());
    filters.push((text) => {
      if (text.includes('current_database()')) {
        return { rows: [{ database: 'testdb', user: 'tester', server_version: '16.0' }] };
      }
      if (text.includes('has_schema_privilege($1, $2, $3)')) {
        return { rows: [{ can_create: true }] };
      }
      if (text.startsWith('SELECT EXISTS (')) {
        return { rows: [{ exists: false }] };
      }
      if (
        text.includes('information_schema.tables') ||
        text.includes('information_schema.columns')
      ) {
        return { rows: [] };
      }
      return undefined;
    });

    await runner.applyMigration(baseConfig, MIGRATION_PATH, REAL_BYTES);
    expect(log[0]).toBe('CONNECT');
    expect(log[1]).toBe('BEGIN ISOLATION LEVEL SERIALIZABLE');
    expect(log[2]).toBe('SET LOCAL search_path = public, pg_catalog');
    expect(log[3]).toBe(SESSION_ROLE_SELECT);
    expect(log[4]?.replace(/\s+/g, ' ').trim()).toBe(
      MIGRATOR_ROLE_SELECT.replace(/\s+/g, ' ').trim(),
    );
    expect(log[5]).toBe(MEMBERSHIP_SELECT);
    // Q4 fetches lu_auth_runtime properties BEFORE the SET LOCAL ROLE
    // statement so the session login can still see the role through pg_roles.
    expect(log[6]?.replace(/\s+/g, ' ').trim()).toBe(
      RUNTIME_ROLE_SELECT.replace(/\s+/g, ' ').trim(),
    );
    expect(log[7]).toBe('SET LOCAL ROLE lu_auth_migrator');
    expect(log[8]).toBe(CURRENT_ACTOR_SELECT);
    expect(log[9]).toMatch(/has_schema_privilege\(\$1, \$2, \$3\)/);
    expect(log[10]).toBe(RUNTIME_CREATE_SELECT);
    expect(log[11]?.startsWith(RUNTIME_OWNS_SELECT)).toBe(true);
    expect(log[12]).toMatch(/pg_advisory_xact_lock/);
    expect(log[13]).toMatch(/pg_advisory_xact_lock/);
    expect(lockKeys[0]).toBe(CONTROL_PLANE_COMMON_ADVISORY_LOCK_KEY);
    expect(lockKeys[1]).toBe('lu:identity-control-plane:0001');
  });

  // -------------------------------------------------------------------------
  // Fix 6 — verifier exactness.
  // -------------------------------------------------------------------------
  it('fix-6: sequence ownership table+column mismatch is rejected', async () => {
    const runner = await importRunner({
      f2: {
        CONTROL_PLANE_SUPPLEMENTAL_EXPECTATIONS: {
          'control-plane/0004_academic_catalogs.sql': {
            stream: 'control-plane',
            relativePath: 'control-plane/0004_academic_catalogs.sql',
            sequences: [
              { name: 'lu_faculty_id_seq', ownedByTable: 'lu_faculty', ownedByColumn: 'id' },
            ],
            triggers: [],
            tableAcls: [],
            sequenceAcls: [],
            functionAcls: [],
            schemaAcls: [],
            notes: [],
          },
        },
      },
    });
    filters.push(...migratorPreflightFilters());
    filters.push((text) => {
      if (text.includes('current_database()')) {
        return { rows: [{ database: 'testdb', user: 'tester', server_version: '16.0' }] };
      }
      if (text.includes('has_schema_privilege($1, $2, $3)')) {
        return { rows: [{ can_create: true }] };
      }
      if (text.startsWith('SELECT EXISTS (')) {
        return { rows: [{ exists: false }] };
      }
      if (
        text.includes('pg_class') &&
        text.includes("relkind = 'S'") &&
        text.includes('s.relname = $2')
      ) {
        return {
          rows: [
            {
              sequence_relname: 'lu_faculty_id_seq',
              owner_table: 'lu_wrong_table',
              owner_column: 'id',
            },
          ],
        };
      }
      if (
        text.includes('information_schema.tables') ||
        text.includes('information_schema.columns')
      ) {
        return { rows: [] };
      }
      return undefined;
    });

    await expect(runner.applyMigration(baseConfig, cp0005Path, cp0005Bytes)).rejects.toThrow(
      /sequence lu_faculty_id_seq owned by lu_wrong_table \(expected lu_faculty\)/,
    );
  });

  it('fix-6: identity sequence accepts deptype=i ownership link AND verifies attidentity=d', async () => {
    const runner = await importRunner({
      f2: {
        CONTROL_PLANE_SUPPLEMENTAL_EXPECTATIONS: {
          'control-plane/0004_academic_catalogs.sql': {
            stream: 'control-plane',
            relativePath: 'control-plane/0004_academic_catalogs.sql',
            sequences: [
              {
                name: 'lu_faculty_id_seq',
                ownedByTable: 'lu_faculty',
                ownedByColumn: 'id',
                identity: 'BY DEFAULT',
              },
            ],
            triggers: [],
            tableAcls: [],
            sequenceAcls: [],
            functionAcls: [],
            schemaAcls: [],
            notes: [],
          },
        },
      },
    });
    let identityQuerySeen = false;
    filters.push(...migratorPreflightFilters());
    filters.push((text) => {
      if (text.includes('current_database()')) {
        return { rows: [{ database: 'testdb', user: 'tester', server_version: '16.0' }] };
      }
      if (text.includes('has_schema_privilege($1, $2, $3)')) {
        return { rows: [{ can_create: true }] };
      }
      if (text.startsWith('SELECT EXISTS (')) {
        return { rows: [{ exists: false }] };
      }
      // Ownership query (now allows deptype IN ('a', 'i')) — emit an
      // identity-dep row (deptype='i') to prove the verifier accepts it.
      if (
        text.includes('pg_class') &&
        text.includes("relkind = 'S'") &&
        text.includes('s.relname = $2')
      ) {
        return {
          rows: [
            {
              sequence_relname: 'lu_faculty_id_seq',
              owner_table: 'lu_faculty',
              owner_column: 'id',
            },
          ],
        };
      }
      // Identity-column probe (attidentity). Confirm the runner actually
      // emits this query when seq.identity === 'BY DEFAULT'.
      if (
        text.includes('pg_attribute') &&
        text.includes('attidentity') &&
        text.includes('tbl.relname = $2')
      ) {
        identityQuerySeen = true;
        return { rows: [{ attidentity: 'd' }] };
      }
      if (
        text.includes('information_schema.tables') ||
        text.includes('information_schema.columns')
      ) {
        return { rows: [] };
      }
      return undefined;
    });

    await expect(runner.applyMigration(baseConfig, cp0005Path, cp0005Bytes)).resolves.toEqual(
      expect.objectContaining({ executed: true, committed: true }),
    );
    expect(identityQuerySeen).toBe(true);
  });

  it('fix-6: identity sequence rejects column whose attidentity is empty (e.g. serial link)', async () => {
    const runner = await importRunner({
      f2: {
        CONTROL_PLANE_SUPPLEMENTAL_EXPECTATIONS: {
          'control-plane/0004_academic_catalogs.sql': {
            stream: 'control-plane',
            relativePath: 'control-plane/0004_academic_catalogs.sql',
            sequences: [
              {
                name: 'lu_faculty_id_seq',
                ownedByTable: 'lu_faculty',
                ownedByColumn: 'id',
                identity: 'BY DEFAULT',
              },
            ],
            triggers: [],
            tableAcls: [],
            sequenceAcls: [],
            functionAcls: [],
            schemaAcls: [],
            notes: [],
          },
        },
      },
    });
    filters.push(...migratorPreflightFilters());
    filters.push((text) => {
      if (text.includes('current_database()')) {
        return { rows: [{ database: 'testdb', user: 'tester', server_version: '16.0' }] };
      }
      if (text.includes('has_schema_privilege($1, $2, $3)')) {
        return { rows: [{ can_create: true }] };
      }
      if (text.startsWith('SELECT EXISTS (')) {
        return { rows: [{ exists: false }] };
      }
      if (
        text.includes('pg_class') &&
        text.includes("relkind = 'S'") &&
        text.includes('s.relname = $2')
      ) {
        return {
          rows: [
            {
              sequence_relname: 'lu_faculty_id_seq',
              owner_table: 'lu_faculty',
              owner_column: 'id',
            },
          ],
        };
      }
      // attidentity probe returns '' to simulate a serial-style column
      // that happens to be linked via deptype='a' but is NOT identity.
      if (
        text.includes('pg_attribute') &&
        text.includes('attidentity') &&
        text.includes('tbl.relname = $2')
      ) {
        return { rows: [{ attidentity: '' }] };
      }
      if (
        text.includes('information_schema.tables') ||
        text.includes('information_schema.columns')
      ) {
        return { rows: [] };
      }
      return undefined;
    });

    await expect(runner.applyMigration(baseConfig, cp0005Path, cp0005Bytes)).rejects.toThrow(
      /identity link expected attidentity='d' on lu_faculty\.id, got ''/,
    );
  });

  it('fix-6: trigger event set mismatch is rejected', async () => {
    const runner = await importRunner({
      f2: {
        CONTROL_PLANE_SUPPLEMENTAL_EXPECTATIONS: {
          'control-plane/0006_mig001_users_identity.sql': {
            stream: 'control-plane',
            relativePath: 'control-plane/0006_mig001_users_identity.sql',
            sequences: [],
            triggers: [
              {
                table: 'lu_user',
                name: 'trg_lu_user_full_name_sync',
                timing: 'BEFORE',
                events: ['INSERT', 'UPDATE'],
                updateColumns: null,
                isConstraintTrigger: false,
                deferrable: false,
                initiallyDeferred: false,
                functionName: 'lu_user_full_name_sync()',
              },
            ],
            tableAcls: [],
            sequenceAcls: [],
            functionAcls: [],
            schemaAcls: [],
            notes: [],
          },
        },
      },
    });
    filters.push(...migratorPreflightFilters());
    filters.push((text) => {
      if (text.includes('current_database()')) {
        return { rows: [{ database: 'testdb', user: 'tester', server_version: '16.0' }] };
      }
      if (text.includes('has_schema_privilege($1, $2, $3)')) {
        return { rows: [{ can_create: true }] };
      }
      if (text.startsWith('SELECT EXISTS (')) {
        return { rows: [{ exists: false }] };
      }
      if (text.includes('pg_trigger')) {
        return {
          rows: [
            {
              trigger_name: 'trg_lu_user_full_name_sync',
              event_object_table: 'lu_user',
              action_timing: 'BEFORE',
              event_manipulation: 'UPDATE',
              action_orientation: 'ROW',
            },
          ],
        };
      }
      if (
        text.includes('information_schema.tables') ||
        text.includes('information_schema.columns')
      ) {
        return { rows: [] };
      }
      return undefined;
    });

    await expect(runner.applyMigration(baseConfig, cp0006Path, cp0006Bytes)).rejects.toThrow(
      /events are UPDATE \(expected INSERT,UPDATE\)/,
    );
  });

  it('fix-6: positive ACL row with GRANT OPTION (is_grantable=YES) is rejected', async () => {
    const runner = await importRunner({
      f2: {
        CONTROL_PLANE_SUPPLEMENTAL_EXPECTATIONS: {
          'control-plane/0005_user_management.sql': {
            stream: 'control-plane',
            relativePath: 'control-plane/0005_user_management.sql',
            sequences: [],
            triggers: [],
            tableAcls: [
              {
                role: 'lu_user_runtime',
                table: 'lu_user',
                grants: [{ privilege: 'SELECT', columns: null, granted: true }],
              },
            ],
            sequenceAcls: [],
            functionAcls: [],
            schemaAcls: [],
            notes: [],
          },
        },
      },
    });
    filters.push(...migratorPreflightFilters());
    filters.push((text) => {
      if (text.includes('current_database()')) {
        return { rows: [{ database: 'testdb', user: 'tester', server_version: '16.0' }] };
      }
      if (text.includes('has_schema_privilege($1, $2, $3)')) {
        return { rows: [{ can_create: true }] };
      }
      if (text.startsWith('SELECT EXISTS (')) {
        return { rows: [{ exists: false }] };
      }
      // New pg_class.relacl + aclexplode verifier — is_grantable is a
      // boolean. The catalog reports the GRANT OPTION flag (true) and the
      // runner must reject the positive expectation.
      if (
        text.includes('pg_class') &&
        text.includes('relacl') &&
        text.includes('aclexplode') &&
        !text.includes('pg_attribute')
      ) {
        return {
          rows: [
            {
              grantee: 'lu_user_runtime',
              privilege_type: 'SELECT',
              is_grantable: true,
            },
          ],
        };
      }
      if (
        text.includes('information_schema.tables') ||
        text.includes('information_schema.columns')
      ) {
        return { rows: [] };
      }
      return undefined;
    });

    await expect(runner.applyMigration(baseConfig, cp0005Path, cp0005Bytes)).rejects.toThrow(
      /carries GRANT OPTION/,
    );
  });

  // -------------------------------------------------------------------------
  // Fix 7 — replay/verify no-pin path semantics.
  // -------------------------------------------------------------------------
  it('fix-7: verify path rejects when recorded max >= requested ordinal but the pin row is absent (contiguity)', async () => {
    const runner = await importRunner();
    // verify runs the migrator preflight before the history probe; the
    // session role must be a valid migrator login under the contract.
    filters.push(...migratorPreflightFilters());
    filters.push((text) => {
      if (text.includes('current_database()')) {
        return { rows: [{ database: 'testdb', user: 'tester', server_version: '16.0' }] };
      }
      if (text.startsWith('SELECT EXISTS (')) {
        return { rows: [{ exists: true }] };
      }
      if (text.startsWith(LEDGER_SELECT_PREFIX)) {
        // Contiguous 1..3 but missing the requested ordinal (2) — not
        // possible in a real contiguous ledger; here we craft a ledger with
        // ordinals 1 and 3 only to force the gap-reject.
        return {
          rows: [
            pinRowFor('0001_create_identity_control_plane.sql'),
            pinRowFor('0003_create_tenant_route_catalog.sql'),
          ],
        };
      }
      if (
        text.includes('information_schema.tables') ||
        text.includes('information_schema.columns')
      ) {
        return { rows: [] };
      }
      return undefined;
    });

    await expect(
      runner.verifyMigration(baseConfig, AUTH_SECURITY_PATH, AUTH_SECURITY_BYTES),
    ).rejects.toThrow(/has a gap at ordinal 2/);
  });

  it('fix-7: fresh-apply verifies only through requested ordinal when max < requested', async () => {
    const seen: Array<{ tables: string[] }> = [];
    const runner = await importRunner({
      verifySchema: (manifest) => {
        seen.push({ tables: Object.keys(manifest.tables ?? {}) });
        return { passed: true, diffs: [], actualTableNames: [] };
      },
      schemaManifest: {
        IDENTITY_CONTROL_PLANE_SCHEMA_MANIFEST: {
          schema: 'public',
          tables: { lu_only_0001: {} },
        },
      },
    });
    // verify runs the migrator preflight before the history probe.
    filters.push(...migratorPreflightFilters());
    filters.push((text) => {
      if (text.includes('current_database()')) {
        return { rows: [{ database: 'testdb', user: 'tester', server_version: '16.0' }] };
      }
      if (text.startsWith('SELECT EXISTS (')) {
        return { rows: [{ exists: false }] };
      }
      if (
        text.includes('information_schema.tables') ||
        text.includes('information_schema.columns')
      ) {
        return { rows: [] };
      }
      return undefined;
    });

    const outcome = await runner.verifyMigration(baseConfig, MIGRATION_PATH, REAL_BYTES);
    expect(outcome.verification.passed).toBe(true);
    // Fresh DB (no history): the manifest is the per-entry 0001 manifest
    // (entry.ordinal=1) — never POST_F2 or any other cumulative rung.
    expect(seen.some((s) => s.tables.length === 1 && s.tables[0] === 'lu_only_0001')).toBe(true);
    expect(seen.some((s) => s.tables.length > 1)).toBe(false);
  });
});

// ---------------------------------------------------------------------------
// Helpers local to the W13A block.
// ---------------------------------------------------------------------------

type LocalGrant = {
  privilege: 'SELECT' | 'INSERT' | 'UPDATE' | 'DELETE' | 'TRUNCATE';
  columns: readonly string[] | null;
  granted: boolean;
};

type LocalTableAcl = {
  role: string;
  table: string;
  grants: readonly LocalGrant[];
};

interface LocalSupplemental {
  readonly stream: 'control-plane' | 'tenant';
  readonly relativePath: string;
  readonly sequences: readonly unknown[];
  readonly triggers: readonly unknown[];
  readonly tableAcls: readonly LocalTableAcl[];
  readonly sequenceAcls: readonly unknown[];
  readonly functionAcls: readonly unknown[];
  readonly schemaAcls: readonly unknown[];
  readonly notes: readonly string[];
}

// The dedup helpers are typed as `readonly (MigrationSupplementalExpectation | undefined)[]`
// but the only field they consult is `tableAcls` (and a couple of others
// for parallel dedups). We cast through `unknown` for these focused unit
// tests so we don't have to fabricate full supplemental objects.
const asSupplemental = (s: LocalSupplemental): unknown => s;

// ---------------------------------------------------------------------------
// MIG-001-F2-W18 — focused regression suite for R7 demonstrated findings.
//
// 1) Trigger verifier must compare ALL existing expectation fields.
// 2) Tenant supplemental merge must iterate ordinal 1..cap exactly once
//    in ascending chronology so later ordinal wins (no `own` prepending).
// 3) Migrator preflight covers every elevated-attribute flag.
// ---------------------------------------------------------------------------

type RunnerModuleW18 = typeof import('../src/database/migration-runner.js');
type QueryFilterW18 = (text: string, values?: unknown[]) => MockQueryResult | undefined;

function makeW18Filters(push: (f: QueryFilterW18) => void): void {
  push((text) => {
    if (text.includes('current_database()')) {
      return { rows: [{ database: 'testdb', user: 'tester', server_version: '16.0' }] };
    }
    if (text.includes('has_schema_privilege($1, $2, $3)')) {
      return { rows: [{ can_create: true }] };
    }
    if (text.startsWith('SELECT EXISTS (')) {
      return { rows: [{ exists: false }] };
    }
    if (text.includes('information_schema.tables') || text.includes('information_schema.columns')) {
      return { rows: [] };
    }
    return undefined;
  });
}

async function importW18Runner(f2?: Record<string, unknown>): Promise<RunnerModuleW18> {
  jestRuntime.resetModules();
  unstableMockModule('pg', () => ({
    Client: class {
      async connect(): Promise<void> {
        /* no-op */
      }
      async query(_text: string, _values?: unknown[]): Promise<MockQueryResult> {
        void _text;
        void _values;
        return { rows: [] };
      }
      async end(): Promise<void> {
        /* no-op */
      }
    },
  }));
  unstableMockModule('../src/database/schema-manifest.js', () => ({
    IDENTITY_CONTROL_PLANE_SCHEMA_MANIFEST: { schema: 'public', tables: {} },
    AUTH_SECURITY_SCHEMA_MANIFEST: { schema: 'public', tables: {} },
    TENANT_ROUTE_SCHEMA_MANIFEST: { schema: 'public', tables: {} },
    normalizeCheckDefinition: (s: string) => s,
    normalizeDefault: (s: string | null) => s,
    normalizeIndexDefinition: (s: string) => s,
    verifySchema: async () => ({ passed: true, diffs: [], actualTableNames: [] }),
  }));
  unstableMockModule('../src/database/schema-manifest-f2.js', () => ({
    POST_F2_CONTROL_PLANE_SCHEMA_MANIFEST: {},
    POST_F2_CONTROL_PLANE_TABLES: [],
    ACADEMIC_CATALOG_SCHEMA_MANIFEST: {},
    CONTROL_PLANE_SUPPLEMENTAL_EXPECTATIONS: {},
    TENANT_FINAL_TABLES: [],
    TENANT_FINAL_SCHEMA_MANIFEST: {},
    TENANT_MIGRATION_MANIFESTS: {},
    TENANT_SUPPLEMENTAL_EXPECTATIONS: {},
    ...f2,
  }));
  unstableMockModule('../src/database/schema-manifest-tenant.js', () => ({
    TENANT_FINAL_TABLES: [],
    TENANT_FINAL_SCHEMA_MANIFEST: {},
    TENANT_MIGRATION_MANIFESTS: {},
    TENANT_SUPPLEMENTAL_EXPECTATIONS: {},
  }));
  return import('../src/database/migration-runner.js');
}

async function importW18RunnerWithFilters(
  overrides: Parameters<typeof migratorPreflightFilters>[0],
): Promise<RunnerModuleW18> {
  const filters: QueryFilter[] = [];
  filters.push(...migratorPreflightFilters(overrides));
  makeW18Filters((f) => filters.push(f));
  jestRuntime.resetModules();
  unstableMockModule('pg', () => ({
    Client: class {
      async connect(): Promise<void> {
        /* no-op */
      }
      async query(text: string, values?: unknown[]): Promise<MockQueryResult> {
        for (let i = filters.length - 1; i >= 0; i--) {
          const filter = filters[i] as QueryFilter;
          const result = filter(text, values);
          if (result !== undefined) return result;
        }
        return { rows: [] };
      }
      async end(): Promise<void> {
        /* no-op */
      }
    },
  }));
  unstableMockModule('../src/database/schema-manifest.js', () => ({
    IDENTITY_CONTROL_PLANE_SCHEMA_MANIFEST: { schema: 'public', tables: {} },
    AUTH_SECURITY_SCHEMA_MANIFEST: { schema: 'public', tables: {} },
    TENANT_ROUTE_SCHEMA_MANIFEST: { schema: 'public', tables: {} },
    normalizeCheckDefinition: (s: string) => s,
    normalizeDefault: (s: string | null) => s,
    normalizeIndexDefinition: (s: string) => s,
    verifySchema: async () => ({ passed: true, diffs: [], actualTableNames: [] }),
  }));
  unstableMockModule('../src/database/schema-manifest-f2.js', () => ({
    POST_F2_CONTROL_PLANE_SCHEMA_MANIFEST: {},
    POST_F2_CONTROL_PLANE_TABLES: [],
    ACADEMIC_CATALOG_SCHEMA_MANIFEST: {},
    CONTROL_PLANE_SUPPLEMENTAL_EXPECTATIONS: {},
    TENANT_FINAL_TABLES: [],
    TENANT_FINAL_SCHEMA_MANIFEST: {},
    TENANT_MIGRATION_MANIFESTS: {},
    TENANT_SUPPLEMENTAL_EXPECTATIONS: {},
  }));
  unstableMockModule('../src/database/schema-manifest-tenant.js', () => ({
    TENANT_FINAL_TABLES: [],
    TENANT_FINAL_SCHEMA_MANIFEST: {},
    TENANT_MIGRATION_MANIFESTS: {},
    TENANT_SUPPLEMENTAL_EXPECTATIONS: {},
  }));
  return import('../src/database/migration-runner.js');
}

describe('migration-runner W18: trigger verifier exhausts every dimension', () => {
  const cp0006Path = fileURLToPath(
    new URL('../migrations/control-plane/0006_mig001_users_identity.sql', import.meta.url),
  );
  const cp0006Bytes = readFileSync(cp0006Path);
  const baseConfig = {
    host: 'localhost',
    port: 5432,
    database: 'testdb',
    user: 'tester',
    password: 'secret',
    ssl: false as const,
  };

  const triggerExpectation = {
    table: 'lu_user',
    name: 'trg_lu_user_status_no_diverge',
    timing: 'BEFORE',
    events: ['INSERT', 'UPDATE'],
    updateColumns: ['status', 'account_status'],
    isConstraintTrigger: false,
    deferrable: false,
    initiallyDeferred: false,
    functionName: 'lu_user_status_no_diverge()',
  };

  const supplementalWithTrigger = (extra: Partial<typeof triggerExpectation> = {}) => ({
    stream: 'control-plane',
    relativePath: 'control-plane/0006_mig001_users_identity.sql',
    sequences: [],
    triggers: [{ ...triggerExpectation, ...extra }],
    tableAcls: [],
    sequenceAcls: [],
    functionAcls: [],
    schemaAcls: [],
    notes: [],
  });

  const matchingTrigger = {
    trigger_name: 'trg_lu_user_status_no_diverge',
    event_object_table: 'lu_user',
    action_timing: 'BEFORE',
    event_manipulation: 'INSERT,UPDATE',
    action_orientation: 'ROW',
    is_constraint: false,
    deferrable: false,
    initially_deferred: false,
    function_signature: 'lu_user_status_no_diverge()',
    update_columns: ['account_status', 'status'],
  };

  function buildTriggerRunner(opts: {
    triggerOverrides?: Record<string, unknown>;
    duplicateTrigger?: boolean;
    omitTrigger?: boolean;
    preflight?: Parameters<typeof migratorPreflightFilters>[0];
  }) {
    const log: string[] = [];
    const filters: QueryFilter[] = [];
    jestRuntime.resetModules();
    unstableMockModule('pg', () => ({
      Client: class {
        async connect(): Promise<void> {
          log.push('CONNECT');
        }
        async query(text: string, values?: unknown[]): Promise<MockQueryResult> {
          log.push(text);
          for (let i = filters.length - 1; i >= 0; i--) {
            const filter = filters[i] as QueryFilter;
            const result = filter(text, values);
            if (result !== undefined) return result;
          }
          return { rows: [] };
        }
        async end(): Promise<void> {
          log.push('END');
        }
      },
    }));
    unstableMockModule('../src/database/schema-manifest.js', () => ({
      IDENTITY_CONTROL_PLANE_SCHEMA_MANIFEST: { schema: 'public', tables: {} },
      AUTH_SECURITY_SCHEMA_MANIFEST: { schema: 'public', tables: {} },
      TENANT_ROUTE_SCHEMA_MANIFEST: { schema: 'public', tables: {} },
      normalizeCheckDefinition: (s: string) => s,
      normalizeDefault: (s: string | null) => s,
      normalizeIndexDefinition: (s: string) => s,
      verifySchema: async () => ({ passed: true, diffs: [], actualTableNames: [] }),
    }));
    unstableMockModule('../src/database/schema-manifest-f2.js', () => ({
      POST_F2_CONTROL_PLANE_SCHEMA_MANIFEST: {},
      POST_F2_CONTROL_PLANE_TABLES: [],
      ACADEMIC_CATALOG_SCHEMA_MANIFEST: {},
      CONTROL_PLANE_SUPPLEMENTAL_EXPECTATIONS: {
        'control-plane/0006_mig001_users_identity.sql': supplementalWithTrigger(),
      },
      TENANT_FINAL_TABLES: [],
      TENANT_FINAL_SCHEMA_MANIFEST: {},
      TENANT_MIGRATION_MANIFESTS: {},
      TENANT_SUPPLEMENTAL_EXPECTATIONS: {},
    }));
    unstableMockModule('../src/database/schema-manifest-tenant.js', () => ({
      TENANT_FINAL_TABLES: [],
      TENANT_FINAL_SCHEMA_MANIFEST: {},
      TENANT_MIGRATION_MANIFESTS: {},
      TENANT_SUPPLEMENTAL_EXPECTATIONS: {},
    }));
    return import('../src/database/migration-runner.js').then(
      async (
        runner,
      ): Promise<{
        runner: RunnerModuleW18;
        log: string[];
        filters: QueryFilter[];
        push: (f: QueryFilterW18) => void;
      }> => {
        filters.push(...migratorPreflightFilters(opts.preflight ?? {}));
        makeW18Filters((f) => filters.push(f));
        // History probe: CP 0006 creates lu_migration_history, so the
        // pre-body probe is FALSE and the post-body re-probe is TRUE.
        // Push this filter LAST so it wins over the makeW18Filters
        // generic SELECT EXISTS answer (filters iterate from the end).
        const probeAnswers: boolean[] = [false, true];
        let probeIdx = 0;
        filters.push((text) => {
          if (text.startsWith('SELECT EXISTS (')) {
            const ans = probeAnswers[probeIdx] ?? false;
            probeIdx += 1;
            return { rows: [{ exists: ans }] };
          }
          return undefined;
        });
        // History ledger backfill: CP 0006 stamps 1..6 exact pins.
        // The runner does INSERT ... ON CONFLICT DO NOTHING and then a
        // SELECT to re-read; we answer the SELECT with the registered
        // pin so the backfill passes.
        const ledger = new Map<string, Record<string, unknown>>();
        for (let i = 1; i <= 6; i += 1) {
          const e = CONTROL_PLANE_REGISTRY[i - 1];
          if (e !== undefined) {
            ledger.set(`control-plane:${i}`, {
              stream: e.stream,
              ordinal: e.ordinal,
              relative_path: e.relativePath,
              sha256: e.sha256,
              bytes: e.byteLength,
              work_id: e.workId,
            });
          }
        }
        filters.push((text, values) => {
          if (text.startsWith('INSERT INTO public.lu_migration_history')) {
            const v = (values ?? []) as unknown[];
            const key = `${String(v[0])}:${String(v[1])}`;
            if (!ledger.has(key)) {
              ledger.set(key, {
                stream: v[0],
                ordinal: v[1],
                relative_path: v[2],
                sha256: v[3],
                bytes: v[4],
                work_id: v[5],
              });
            }
            return { rows: [] };
          }
          if (
            text.includes(
              'FROM public.lu_migration_history WHERE stream = $1 AND ordinal = $2 LIMIT 1',
            )
          ) {
            const key = `${String(values?.[0])}:${String(values?.[1])}`;
            const row = ledger.get(key);
            return { rows: row === undefined ? [] : [row] };
          }
          return undefined;
        });
        if (!opts.omitTrigger) {
          const triggerRow = { ...matchingTrigger, ...(opts.triggerOverrides ?? {}) };
          filters.push((text) => {
            if (text.includes('pg_trigger')) {
              return { rows: opts.duplicateTrigger ? [triggerRow, triggerRow] : [triggerRow] };
            }
            return undefined;
          });
        }
        return { runner, log, filters, push: (f) => filters.push(f) };
      },
    );
  }

  it('all dimensions matching: pass-through (sanity check)', async () => {
    const { runner } = await buildTriggerRunner({});
    const outcome = await runner.applyMigration(baseConfig, cp0006Path, cp0006Bytes);
    expect(outcome.executed).toBe(true);
    expect(outcome.committed).toBe(true);
  });

  it('timing mismatch is rejected', async () => {
    const { runner } = await buildTriggerRunner({
      triggerOverrides: { action_timing: 'AFTER' },
    });
    await expect(runner.applyMigration(baseConfig, cp0006Path, cp0006Bytes)).rejects.toThrow(
      /timing is AFTER \(expected BEFORE\)/,
    );
  });

  it('event set mismatch (extra DELETE) is rejected', async () => {
    const { runner } = await buildTriggerRunner({
      triggerOverrides: { event_manipulation: 'DELETE,INSERT,UPDATE' },
    });
    await expect(runner.applyMigration(baseConfig, cp0006Path, cp0006Bytes)).rejects.toThrow(
      /events are DELETE,INSERT,UPDATE \(expected INSERT,UPDATE\)/,
    );
  });

  it('event set mismatch (missing UPDATE) is rejected', async () => {
    const { runner } = await buildTriggerRunner({
      triggerOverrides: { event_manipulation: 'INSERT' },
    });
    await expect(runner.applyMigration(baseConfig, cp0006Path, cp0006Bytes)).rejects.toThrow(
      /events are INSERT \(expected INSERT,UPDATE\)/,
    );
  });

  it('UPDATE OF columns mismatch is rejected', async () => {
    const { runner } = await buildTriggerRunner({
      triggerOverrides: { update_columns: ['email'] },
    });
    await expect(runner.applyMigration(baseConfig, cp0006Path, cp0006Bytes)).rejects.toThrow(
      /UPDATE OF columns are \[email\] \(expected \[account_status,status\]\)/,
    );
  });

  it('UPDATE OF columns missing (catalog returns empty) is rejected', async () => {
    const { runner } = await buildTriggerRunner({
      triggerOverrides: { update_columns: [] },
    });
    await expect(runner.applyMigration(baseConfig, cp0006Path, cp0006Bytes)).rejects.toThrow(
      /UPDATE OF columns are \[\] \(expected \[account_status,status\]\)/,
    );
  });

  it('function signature mismatch is rejected', async () => {
    const { runner } = await buildTriggerRunner({
      triggerOverrides: { function_signature: 'wrong_func()' },
    });
    await expect(runner.applyMigration(baseConfig, cp0006Path, cp0006Bytes)).rejects.toThrow(
      /function signature is wrong_func\(\) \(expected lu_user_status_no_diverge\(\)\)/,
    );
  });

  it('is_constraint flag mismatch (constraint vs non-constraint) is rejected', async () => {
    const { runner } = await buildTriggerRunner({
      triggerOverrides: { is_constraint: true },
    });
    await expect(runner.applyMigration(baseConfig, cp0006Path, cp0006Bytes)).rejects.toThrow(
      /is_constraint is true \(expected false\)/,
    );
  });

  it('deferrable flag mismatch is rejected', async () => {
    const { runner } = await buildTriggerRunner({
      triggerOverrides: { deferrable: true },
    });
    await expect(runner.applyMigration(baseConfig, cp0006Path, cp0006Bytes)).rejects.toThrow(
      /deferrable is true \(expected false\)/,
    );
  });

  it('initially_deferred flag mismatch is rejected', async () => {
    const { runner } = await buildTriggerRunner({
      triggerOverrides: { initially_deferred: true },
    });
    await expect(runner.applyMigration(baseConfig, cp0006Path, cp0006Bytes)).rejects.toThrow(
      /initially_deferred is true \(expected false\)/,
    );
  });

  it('multiple dimension mismatches are all reported', async () => {
    const { runner } = await buildTriggerRunner({
      triggerOverrides: {
        action_timing: 'AFTER',
        function_signature: 'wrong()',
        is_constraint: true,
      },
    });
    let caught: unknown = null;
    try {
      await runner.applyMigration(baseConfig, cp0006Path, cp0006Bytes);
    } catch (error) {
      caught = error;
    }
    expect(caught).toBeInstanceOf(Error);
    const message = (caught as Error).message;
    expect(message).toMatch(/timing is AFTER/);
    expect(message).toMatch(/function signature is wrong\(\)/);
    expect(message).toMatch(/is_constraint is true/);
  });

  it('duplicate pg_trigger rows (same schema/table/name) fail closed', async () => {
    const { runner } = await buildTriggerRunner({ duplicateTrigger: true });
    await expect(runner.applyMigration(baseConfig, cp0006Path, cp0006Bytes)).rejects.toThrow(
      /has 2 matching rows in pg_trigger \(expected exactly 1\)/,
    );
  });

  it('missing trigger row (catalog returns empty) fails closed', async () => {
    const { runner } = await buildTriggerRunner({ omitTrigger: true });
    await expect(runner.applyMigration(baseConfig, cp0006Path, cp0006Bytes)).rejects.toThrow(
      /missing trigger trg_lu_user_status_no_diverge ON lu_user/,
    );
  });

  // -------------------------------------------------------------------------
  // R8 trigger verifier regressions:
  //  * multi-event decode (INSERT+UPDATE works through independent bit
  //    decoding, not a CASE mask over `(tgtype & 28)`).
  //  * TRUNCATE mismatch — TRUNCATE is bit 32 of tgtype, previously masked
  //    out by the `(tgtype & 28)` CASE; the new verifier decodes bit 32
  //    independently and rejects a TRUNCATE-only row against an INSERT+UPDATE
  //    expectation.
  //  * is_constraint true (constraint trigger) — the previous verifier used
  //    `(tgtype & 32 = 32)` which conflates TRUNCATE with constraint; the
  //    new verifier reads `tgconstraint <> 0` directly. A constraint trigger
  //    row is rejected when the expectation is a non-constraint trigger.
  //  * orientation STATEMENT is rejected — the migration contract forbids
  //    statement-level triggers on these surfaces; the new verifier
  //    asserts `action_orientation = ROW` and rejects anything else.
  // -------------------------------------------------------------------------

  it('multi-event decode: INSERT+UPDATE passes (independent bit decoding)', async () => {
    // The mock already returns 'INSERT,UPDATE' for the canonical trigger
    // expectation; this regression explicitly pins the multi-event shape
    // so a future regression that drops one event is caught.
    const { runner } = await buildTriggerRunner({
      triggerOverrides: { event_manipulation: 'INSERT,UPDATE' },
    });
    const outcome = await runner.applyMigration(baseConfig, cp0006Path, cp0006Bytes);
    expect(outcome.executed).toBe(true);
    expect(outcome.committed).toBe(true);
  });

  it('multi-event decode: INSERT+DELETE+UPDATE passes (three independent bits)', async () => {
    const { runner } = await buildTriggerRunner({
      triggerOverrides: { event_manipulation: 'DELETE,INSERT,UPDATE' },
    });
    await expect(runner.applyMigration(baseConfig, cp0006Path, cp0006Bytes)).rejects.toThrow(
      /events are DELETE,INSERT,UPDATE \(expected INSERT,UPDATE\)/,
    );
  });

  it('TRUNCATE mismatch: catalog reports TRUNCATE-only against INSERT+UPDATE expectation is rejected', async () => {
    const { runner } = await buildTriggerRunner({
      triggerOverrides: { event_manipulation: 'TRUNCATE' },
    });
    await expect(runner.applyMigration(baseConfig, cp0006Path, cp0006Bytes)).rejects.toThrow(
      /events are TRUNCATE \(expected INSERT,UPDATE\)/,
    );
  });

  it('TRUNCATE+UPDATE mismatch: catalog reports TRUNCATE+UPDATE against INSERT+UPDATE expectation is rejected', async () => {
    const { runner } = await buildTriggerRunner({
      triggerOverrides: { event_manipulation: 'TRUNCATE,UPDATE' },
    });
    await expect(runner.applyMigration(baseConfig, cp0006Path, cp0006Bytes)).rejects.toThrow(
      /events are TRUNCATE,UPDATE \(expected INSERT,UPDATE\)/,
    );
  });

  it('is_constraint true: constraint trigger against non-constraint expectation is rejected (tgconstraint <> 0)', async () => {
    const { runner } = await buildTriggerRunner({
      triggerOverrides: { is_constraint: true },
    });
    await expect(runner.applyMigration(baseConfig, cp0006Path, cp0006Bytes)).rejects.toThrow(
      /is_constraint is true \(expected false\)/,
    );
  });

  it('orientation STATEMENT is rejected (migration contract requires ROW-level triggers)', async () => {
    const { runner } = await buildTriggerRunner({
      triggerOverrides: { action_orientation: 'STATEMENT' },
    });
    await expect(runner.applyMigration(baseConfig, cp0006Path, cp0006Bytes)).rejects.toThrow(
      /orientation is STATEMENT \(expected ROW\)/,
    );
  });
});

describe('migration-runner W18: tenant supplemental merge iterates 1..cap in ascending chronology', () => {
  it('later ordinal (3) wins over earlier ordinal (2) on the same dedup key', async () => {
    const runner = await importW18Runner();
    const ord2: LocalSupplemental = {
      stream: 'tenant',
      relativePath: 'tenant/0002_catalogs.sql',
      sequences: [],
      triggers: [],
      tableAcls: [
        {
          role: 'lu_auth_runtime',
          table: 'lu_country',
          grants: [{ privilege: 'SELECT', columns: null, granted: true }],
        },
      ],
      sequenceAcls: [],
      functionAcls: [],
      schemaAcls: [],
      notes: [],
    };
    const ord3: LocalSupplemental = {
      stream: 'tenant',
      relativePath: 'tenant/0003_academic_laboratories.sql',
      sequences: [],
      triggers: [],
      tableAcls: [
        {
          role: 'lu_auth_runtime',
          table: 'lu_country',
          grants: [{ privilege: 'SELECT', columns: null, granted: false }],
        },
      ],
      sequenceAcls: [],
      functionAcls: [],
      schemaAcls: [],
      notes: [],
    };
    const merged = runner.__testing.dedupTableAcls([
      asSupplemental(ord2) as never,
      asSupplemental(ord3) as never,
    ]);
    expect(merged).toHaveLength(1);
    const grants = merged[0]!.grants;
    expect(grants).toHaveLength(1);
    expect(grants[0]!.granted).toBe(false);
  });

  it('iteration is ascending: ord1 then ord2 then ord3 (no prepended own)', async () => {
    const runner = await importW18Runner();
    const ord1: LocalSupplemental = {
      stream: 'tenant',
      relativePath: 'tenant/0001_dashboard_foundation.sql',
      sequences: [],
      triggers: [],
      tableAcls: [
        {
          role: 'lu_auth_runtime',
          table: 'lu_management',
          grants: [{ privilege: 'SELECT', columns: null, granted: true }],
        },
      ],
      sequenceAcls: [],
      functionAcls: [],
      schemaAcls: [],
      notes: [],
    };
    const ord2: LocalSupplemental = {
      stream: 'tenant',
      relativePath: 'tenant/0002_catalogs.sql',
      sequences: [],
      triggers: [],
      tableAcls: [
        {
          role: 'lu_auth_runtime',
          table: 'lu_management',
          grants: [{ privilege: 'SELECT', columns: null, granted: true }],
        },
      ],
      sequenceAcls: [],
      functionAcls: [],
      schemaAcls: [],
      notes: [],
    };
    const ord3: LocalSupplemental = {
      stream: 'tenant',
      relativePath: 'tenant/0003_academic_laboratories.sql',
      sequences: [],
      triggers: [],
      tableAcls: [
        {
          role: 'lu_auth_runtime',
          table: 'lu_management',
          grants: [{ privilege: 'SELECT', columns: null, granted: false }],
        },
      ],
      sequenceAcls: [],
      functionAcls: [],
      schemaAcls: [],
      notes: [],
    };
    const merged = runner.__testing.dedupTableAcls([
      asSupplemental(ord1) as never,
      asSupplemental(ord2) as never,
      asSupplemental(ord3) as never,
    ]);
    expect(merged).toHaveLength(1);
    expect(merged[0]!.grants[0]!.granted).toBe(false);
  });
});

describe('migration-runner W18: migrator preflight covers every elevated-attribute flag', () => {
  const baseConfig = {
    host: 'localhost',
    port: 5432,
    database: 'testdb',
    user: 'tester',
    password: 'secret',
    ssl: false as const,
  };
  const MIGRATION_PATH = fileURLToPath(
    new URL(`../migrations/${IDENTITY_MIGRATION_FILE}`, import.meta.url),
  );
  const REAL_BYTES = readFileSync(MIGRATION_PATH);

  function buildSessionOverride(extra: Record<string, unknown>): Record<string, unknown> {
    return {
      rolname: 'lu_migrator_login',
      rolcanlogin: true,
      rolsuper: false,
      rolcreaterole: false,
      rolcreatedb: false,
      rolreplication: false,
      rolbypassrls: false,
      ...extra,
    };
  }

  function buildMigratorOverride(extra: Record<string, unknown>): Record<string, unknown> {
    return {
      rolname: 'lu_auth_migrator',
      rolcanlogin: false,
      ...extra,
    };
  }

  async function runWith(overrides: {
    sessionRole?: Record<string, unknown> | null;
    migratorRole?: Record<string, unknown> | null;
  }): Promise<void> {
    const r = await importW18RunnerWithFilters(overrides);
    await r.applyMigration(baseConfig, MIGRATION_PATH, REAL_BYTES);
  }

  it('rejects migrator role with rolsuper=true (NOSUPERUSER contract)', async () => {
    await expect(
      runWith({
        migratorRole: buildMigratorOverride({ rolsuper: true }),
      }),
    ).rejects.toThrow(/NOSUPERUSER|NOLOGIN/);
  });

  it('rejects migrator role with rolcreaterole=true (NOCREATEROLE contract)', async () => {
    await expect(
      runWith({
        migratorRole: buildMigratorOverride({ rolcreaterole: true }),
      }),
    ).rejects.toThrow(/NOCREATEROLE|NOLOGIN/);
  });

  it('rejects migrator role with rolcreatedb=true (NOCREATEDB contract)', async () => {
    await expect(
      runWith({
        migratorRole: buildMigratorOverride({ rolcreatedb: true }),
      }),
    ).rejects.toThrow(/NOCREATEDB|NOLOGIN/);
  });

  it('rejects migrator role with rolreplication=true (NOREPLICATION contract)', async () => {
    await expect(
      runWith({
        migratorRole: buildMigratorOverride({ rolreplication: true }),
      }),
    ).rejects.toThrow(/NOREPLICATION|NOLOGIN/);
  });

  it('rejects migrator role with rolbypassrls=true (NOBYPASSRLS contract)', async () => {
    await expect(
      runWith({
        migratorRole: buildMigratorOverride({ rolbypassrls: true }),
      }),
    ).rejects.toThrow(/NOBYPASSRLS|NOLOGIN/);
  });

  it('rejects session role with rolcreaterole=true (CREATEROLE contract)', async () => {
    await expect(
      runWith({
        sessionRole: buildSessionOverride({ rolcreaterole: true }),
      }),
    ).rejects.toThrow(/CREATEROLE/);
  });
});

// ---------------------------------------------------------------------------
// MIG-001-F2-W19 — R8 regressions:
//  * dedupSequenceAcls must NOT collapse mixed privilege grant states into
//    the first privilege's bool. Privileges with the SAME granted state
//    group together; privileges with DIFFERENT granted states emit one
//    expectation per privilege so the verifier never sees a positive SELECT
//    merged into a negative USAGE (or vice versa). Last ordinal still wins.
//  * The new APPLY preflight (Q1, Q2, Q4) covers the runtime role
//    properties (exists + exact name + NOLOGIN + NOSUPERUSER + NOCREATEROLE
//    + NOCREATEDB + NOREPLICATION + NOBYPASSRLS) BEFORE the existing
//    Q8/Q9 CREATE / ownership checks. Missing runtime role is rejected
//    before any lock or body.
// ---------------------------------------------------------------------------

describe('migration-runner W19: dedupSequenceAcls mixed-state / last-wins', () => {
  it('groups only same-granted privileges; mixed-state emits one expectation per privilege', async () => {
    const runner = await importW18Runner();
    // ord2 grants USAGE+SELECT both positive.
    const ord2: LocalSupplemental = {
      stream: 'control-plane',
      relativePath: 'control-plane/0002_create_auth_security_controls.sql',
      sequences: [],
      triggers: [],
      tableAcls: [],
      sequenceAcls: [
        {
          role: 'lu_auth_runtime',
          sequence: 'lu_foo_id_seq',
          privileges: ['USAGE', 'SELECT'],
          granted: true,
        },
      ],
      functionAcls: [],
      schemaAcls: [],
      notes: [],
    };
    // ord3 revokes USAGE only — SELECT keeps its positive state from ord2.
    const ord3: LocalSupplemental = {
      stream: 'control-plane',
      relativePath: 'control-plane/0003_create_tenant_route_catalog.sql',
      sequences: [],
      triggers: [],
      tableAcls: [],
      sequenceAcls: [
        {
          role: 'lu_auth_runtime',
          sequence: 'lu_foo_id_seq',
          privileges: ['USAGE'],
          granted: false,
        },
      ],
      functionAcls: [],
      schemaAcls: [],
      notes: [],
    };
    const merged = runner.__testing.dedupSequenceAcls([
      asSupplemental(ord2) as never,
      asSupplemental(ord3) as never,
    ]);
    // After merge:
    //   - USAGE granted=false (last ordinal wins from ord3)
    //   - SELECT granted=true (carried over from ord2)
    // Mixed-state: the merged set must contain TWO expectations — one per
    // privilege — not a single expectation with the first privilege's bool
    // propagated to the whole group.
    expect(merged).toHaveLength(2);
    const usage = merged.find((m) => m.privileges.length === 1 && m.privileges[0] === 'USAGE');
    const select = merged.find((m) => m.privileges.length === 1 && m.privileges[0] === 'SELECT');
    expect(usage).toBeDefined();
    expect(select).toBeDefined();
    expect(usage!.granted).toBe(false);
    expect(select!.granted).toBe(true);
    expect(usage!.role).toBe('lu_auth_runtime');
    expect(usage!.sequence).toBe('lu_foo_id_seq');
    expect(select!.role).toBe('lu_auth_runtime');
    expect(select!.sequence).toBe('lu_foo_id_seq');
  });

  it('groups same-granted privileges into one expectation (uniform granted state)', async () => {
    const runner = await importW18Runner();
    const ord2: LocalSupplemental = {
      stream: 'control-plane',
      relativePath: 'control-plane/0002_create_auth_security_controls.sql',
      sequences: [],
      triggers: [],
      tableAcls: [],
      sequenceAcls: [
        {
          role: 'lu_auth_runtime',
          sequence: 'lu_foo_id_seq',
          privileges: ['USAGE', 'SELECT'],
          granted: true,
        },
      ],
      functionAcls: [],
      schemaAcls: [],
      notes: [],
    };
    // ord3 reaffirms SELECT but does NOT add a new granted=false privilege.
    const ord3: LocalSupplemental = {
      stream: 'control-plane',
      relativePath: 'control-plane/0003_create_tenant_route_catalog.sql',
      sequences: [],
      triggers: [],
      tableAcls: [],
      sequenceAcls: [
        {
          role: 'lu_auth_runtime',
          sequence: 'lu_foo_id_seq',
          privileges: ['SELECT'],
          granted: true,
        },
      ],
      functionAcls: [],
      schemaAcls: [],
      notes: [],
    };
    const merged = runner.__testing.dedupSequenceAcls([
      asSupplemental(ord2) as never,
      asSupplemental(ord3) as never,
    ]);
    expect(merged).toHaveLength(1);
    expect(merged[0]!.privileges.slice().sort()).toEqual(['SELECT', 'USAGE']);
    expect(merged[0]!.granted).toBe(true);
  });

  it('last ordinal wins on the same (role, sequence, privilege) key', async () => {
    const runner = await importW18Runner();
    const ord2Positive: LocalSupplemental = {
      stream: 'control-plane',
      relativePath: 'control-plane/0002_create_auth_security_controls.sql',
      sequences: [],
      triggers: [],
      tableAcls: [],
      sequenceAcls: [
        {
          role: 'lu_auth_runtime',
          sequence: 'lu_bar_id_seq',
          privileges: ['USAGE'],
          granted: true,
        },
      ],
      functionAcls: [],
      schemaAcls: [],
      notes: [],
    };
    const ord3Negative: LocalSupplemental = {
      stream: 'control-plane',
      relativePath: 'control-plane/0003_create_tenant_route_catalog.sql',
      sequences: [],
      triggers: [],
      tableAcls: [],
      sequenceAcls: [
        {
          role: 'lu_auth_runtime',
          sequence: 'lu_bar_id_seq',
          privileges: ['USAGE'],
          granted: false,
        },
      ],
      functionAcls: [],
      schemaAcls: [],
      notes: [],
    };
    const ord4PositiveAgain: LocalSupplemental = {
      stream: 'control-plane',
      relativePath: 'control-plane/0004_academic_catalogs.sql',
      sequences: [],
      triggers: [],
      tableAcls: [],
      sequenceAcls: [
        {
          role: 'lu_auth_runtime',
          sequence: 'lu_bar_id_seq',
          privileges: ['USAGE'],
          granted: true,
        },
      ],
      functionAcls: [],
      schemaAcls: [],
      notes: [],
    };
    const merged = runner.__testing.dedupSequenceAcls([
      asSupplemental(ord2Positive) as never,
      asSupplemental(ord3Negative) as never,
      asSupplemental(ord4PositiveAgain) as never,
    ]);
    expect(merged).toHaveLength(1);
    expect(merged[0]!.privileges).toEqual(['USAGE']);
    expect(merged[0]!.granted).toBe(true);
  });

  it('multiple (role, sequence) pairs each keep their own group', async () => {
    const runner = await importW18Runner();
    const ord2: LocalSupplemental = {
      stream: 'control-plane',
      relativePath: 'control-plane/0002_create_auth_security_controls.sql',
      sequences: [],
      triggers: [],
      tableAcls: [],
      sequenceAcls: [
        {
          role: 'lu_auth_runtime',
          sequence: 'lu_seq_a',
          privileges: ['USAGE'],
          granted: true,
        },
        {
          role: 'lu_auth_runtime',
          sequence: 'lu_seq_b',
          privileges: ['SELECT'],
          granted: true,
        },
        {
          role: 'lu_user_runtime',
          sequence: 'lu_seq_a',
          privileges: ['USAGE'],
          granted: true,
        },
      ],
      functionAcls: [],
      schemaAcls: [],
      notes: [],
    };
    const merged = runner.__testing.dedupSequenceAcls([asSupplemental(ord2) as never]);
    expect(merged).toHaveLength(3);
    const a = merged.find((m) => m.role === 'lu_auth_runtime' && m.sequence === 'lu_seq_a');
    const b = merged.find((m) => m.role === 'lu_auth_runtime' && m.sequence === 'lu_seq_b');
    const c = merged.find((m) => m.role === 'lu_user_runtime' && m.sequence === 'lu_seq_a');
    expect(a).toBeDefined();
    expect(b).toBeDefined();
    expect(c).toBeDefined();
    expect(a!.privileges).toEqual(['USAGE']);
    expect(b!.privileges).toEqual(['SELECT']);
    expect(c!.privileges).toEqual(['USAGE']);
    expect(a!.granted).toBe(true);
    expect(b!.granted).toBe(true);
    expect(c!.granted).toBe(true);
  });
});

describe('migration-runner W19: APPLY preflight runtime role table-driven flags', () => {
  function buildRuntimeOverride(extra: Record<string, unknown>): Record<string, unknown> {
    return {
      rolname: 'lu_auth_runtime',
      rolcanlogin: false,
      rolsuper: false,
      rolcreaterole: false,
      rolcreatedb: false,
      rolreplication: false,
      rolbypassrls: false,
      ...extra,
    };
  }

  it('rejects missing runtime role (absent) without taking any lock or body', async () => {
    const filters: QueryFilter[] = [];
    filters.push(
      ...migratorPreflightFilters({
        runtimeRole: null,
      }),
    );
    makeW18Filters((f) => filters.push(f));
    await expect(
      importW18RunnerWithFilters({ runtimeRole: null }).then(async (runner) => {
        await runner.applyMigration(
          {
            host: 'localhost',
            port: 5432,
            database: 'testdb',
            user: 'tester',
            password: 'secret',
            ssl: false as const,
          },
          fileURLToPath(new URL(`../migrations/${IDENTITY_MIGRATION_FILE}`, import.meta.url)),
          REAL_BYTES,
        );
      }),
    ).rejects.toThrow(/role "lu_auth_runtime" is absent/);
  });

  it.each([
    { flag: 'rolcanlogin', message: 'NOLOGIN' },
    { flag: 'rolsuper', message: 'NOSUPERUSER' },
    { flag: 'rolcreaterole', message: 'NOCREATEROLE' },
    { flag: 'rolcreatedb', message: 'NOCREATEDB' },
    { flag: 'rolreplication', message: 'NOREPLICATION' },
    { flag: 'rolbypassrls', message: 'NOBYPASSRLS' },
  ])('rejects runtime role with $flag=true ($message contract)', async ({ flag, message }) => {
    await expect(
      importW18RunnerWithFilters({
        runtimeRole: buildRuntimeOverride({ [flag]: true }),
      }).then(async (runner) => {
        await runner.applyMigration(
          {
            host: 'localhost',
            port: 5432,
            database: 'testdb',
            user: 'tester',
            password: 'secret',
            ssl: false as const,
          },
          fileURLToPath(new URL(`../migrations/${IDENTITY_MIGRATION_FILE}`, import.meta.url)),
          REAL_BYTES,
        );
      }),
    ).rejects.toThrow(new RegExp(message));
  });

  it('rejects runtime role returned with an unexpected identifier', async () => {
    await expect(
      importW18RunnerWithFilters({
        runtimeRole: { ...buildRuntimeOverride({}), rolname: 'lu_other_role' },
      }).then(async (runner) => {
        await runner.applyMigration(
          {
            host: 'localhost',
            port: 5432,
            database: 'testdb',
            user: 'tester',
            password: 'secret',
            ssl: false as const,
          },
          fileURLToPath(new URL(`../migrations/${IDENTITY_MIGRATION_FILE}`, import.meta.url)),
          REAL_BYTES,
        );
      }),
    ).rejects.toThrow(/role "lu_auth_runtime" returned an unexpected identifier/);
  });

  it('session rolcanlogin=false is rejected (NOLOGIN session role)', async () => {
    await expect(
      importW18RunnerWithFilters({
        sessionRole: {
          rolname: 'lu_migrator_login',
          rolcanlogin: false,
          rolsuper: false,
          rolcreaterole: false,
          rolcreatedb: false,
          rolreplication: false,
          rolbypassrls: false,
        },
      }).then(async (runner) => {
        await runner.applyMigration(
          {
            host: 'localhost',
            port: 5432,
            database: 'testdb',
            user: 'tester',
            password: 'secret',
            ssl: false as const,
          },
          fileURLToPath(new URL(`../migrations/${IDENTITY_MIGRATION_FILE}`, import.meta.url)),
          REAL_BYTES,
        );
      }),
    ).rejects.toThrow(/session role "lu_migrator_login" is NOLOGIN/);
  });
});

describe('migration-runner: trigger-shape query does not emit ambiguous attnum (PG-18)', () => {
  // The supplemental trigger verifier projects `pg_trigger.tgattr` (an
  // int2[] of attnum values) back to column names via a `pg_attribute`
  // join. The legacy SQL used `FROM unnest(t.tgattr) AS attnum` and then
  // `att.attnum = attnum`, which PG-18 (and earlier versions) reject with
  // `column reference "attnum" is ambiguous` because both the qualified
  // `att.attnum` and the unnest alias name resolve to the same identifier
  // in scope. The fix names the unnest alias explicitly (`AS u(attnum)`)
  // and qualifies the right-hand side as `u.attnum`.
  //
  // These assertions read the runner source as text so a future edit that
  // re-introduces the unqualified pattern (or drops the alias qualifier)
  // fails fast at `pnpm test` time, before a live PG run surfaces the
  // ambiguity error again.
  const RUNNER_PATH = fileURLToPath(
    new URL('../src/database/migration-runner.ts', import.meta.url),
  );
  const src = readFileSync(RUNNER_PATH, 'utf8');

  it('does not contain the ambiguous pattern `att.attnum = attnum` (unqualified)', () => {
    // `\battnum\b` on the right side ensures we only flag an un-qualified
    // identifier; `att.attnum = u.attnum` (correct) is NOT matched because
    // the right-hand identifier is preceded by `.u`, not the literal
    // `attnum`. Whitespace around `=` is tolerated.
    const ambiguous = /att\.attnum\s*=\s*attnum\b/;
    expect(src).not.toMatch(ambiguous);
  });

  it('qualifies the unnest alias in the trigger-shape query (`AS u(attnum)`)', () => {
    // Portable PG form: `FROM <func>(...) AS alias(col[, col...])`. This
    // binds the column name to a fresh alias scope so the right-hand side
    // is unambiguous.
    expect(src).toMatch(/FROM\s+unnest\(\s*t\.tgattr\s*\)\s+AS\s+u\(\s*attnum\s*\)/);
  });

  it('qualifies the right-hand side of the join (`att.attnum = u.attnum`)', () => {
    expect(src).toMatch(/att\.attnum\s*=\s*u\.attnum\b/);
  });
});

describe('migration-runner: trigger-shape query GROUP BY covers all correlated outer refs (PG-18)', () => {
  // The supplemental trigger verifier runs an outer aggregate SELECT
  // over `pg_trigger` (with a `GROUP BY`) and a correlated scalar
  // subquery `(SELECT array_agg(...) FROM unnest(t.tgattr) ...)` that
  // references outer columns `t.tgattr` and `t.tgrelid`. PG rejects the
  // reference with `subquery uses ungrouped column "t.tgrelid" from
  // outer query` when `t.tgrelid` is missing from the GROUP BY (because
  // the outer aggregation collapses rows before the subquery evaluates,
  // so the correlated reference is undefined). The fix adds `t.tgrelid`
  // to the GROUP BY alongside the other relation-side column
  // `c.relname` it is functionally dependent on.
  //
  // The two assertions below read the runner source as text so a future
  // edit that removes the correlation or drops `t.tgrelid` from the
  // GROUP BY fails fast at `pnpm test` time, before a live PG run
  // surfaces the ungrouped-column error again.
  const RUNNER_PATH = fileURLToPath(
    new URL('../src/database/migration-runner.ts', import.meta.url),
  );
  const src = readFileSync(RUNNER_PATH, 'utf8');

  it('keeps the correlation `att.attrelid = t.tgrelid` in the trigger query', () => {
    // The correlated subquery joins pg_attribute back to the trigger's
    // owning table; without this predicate, the subquery is uncorrelated
    // and any change to its references is meaningless.
    expect(src).toMatch(/att\.attrelid\s*=\s*t\.tgrelid\b/);
  });

  it('includes `t.tgrelid` in the trigger query GROUP BY (covers the correlated reference)', () => {
    // The exact GROUP BY line of the trigger-shape verifier. We extract
    // it via a non-greedy match between the trigger-query anchor (the
    // `FROM pg_trigger t` block) and the closing backtick so the test
    // is scoped to the right query (the runner has several SELECTs with
    // GROUP BY clauses).
    const triggerQuery = /FROM\s+pg_trigger\s+t[\s\S]*?\)/;
    const match = src.match(triggerQuery);
    expect(match).not.toBeNull();
    const block = match![0];
    // Locate the trailing GROUP BY of the outer SELECT inside the block.
    const groupBy = /GROUP\s+BY\s+([^\n`]+)/.exec(block);
    expect(groupBy).not.toBeNull();
    expect(groupBy![1]).toMatch(/\bt\.tgrelid\b/);
  });
});

describe('migration-runner: trigger-shape query projects update_columns as text[]', () => {
  // The `update_columns` column on the trigger-shape verifier is consumed
  // by the runner as a JS array (`(actual.update_columns ?? []).slice()
  // .sort().join(',')`). The Node `pg` driver deserializes `text[]`
  // (OID 1009) as `string[]` consistently, but `name[]` (OID 1003) is
  // deserialized as the literal PG array string `"{}"` when empty,
  // which breaks `.sort()` downstream. The fix casts BOTH arms of the
  // COALESCE to `text[]` so the empty fallback and the populated array
  // agree on the JS-side shape.
  //
  // No global type parser is installed; the fix is local to the SELECT
  // and the runner's TypeScript types on the row interface already
  // declare `update_columns: readonly string[] | null` (line 1643),
  // matching `text[]`. The test below reads the runner source as text
  // and asserts the contract locally: both COALESCE arms cast to
  // `text[]`; `name[]` MUST NOT appear in the trigger-shape block.
  const RUNNER_PATH = fileURLToPath(
    new URL('../src/database/migration-runner.ts', import.meta.url),
  );
  const src = readFileSync(RUNNER_PATH, 'utf8');

  function extractUpdateColumnsBlock(): string {
    // Scopes the assertion to the COALESCE that aliases `update_columns`,
    // so other usages of `name[]` elsewhere in the runner are not in
    // scope. The block is the entire COALESCE expression until its
    // closing `) AS update_columns`.
    const re = /COALESCE\([\s\S]*?\)\s*AS\s+update_columns/;
    const match = src.match(re);
    expect(match).not.toBeNull();
    return match![0];
  }

  it('casts the populated COALESCE arm (array_agg) to text[] via att.attname::text', () => {
    const block = extractUpdateColumnsBlock();
    expect(block).toMatch(/array_agg\(\s*att\.attname::text\s+ORDER\s+BY\s+att\.attname\s*\)/);
  });

  it('casts the empty COALESCE arm to text[] (`ARRAY[]::text[]`, never `::name[]`)', () => {
    const block = extractUpdateColumnsBlock();
    expect(block).toMatch(/ARRAY\[\s*\]\s*::\s*text\[\s*\]/);
  });

  it('does not contain `name[]` in the COALESCE(update_columns) block', () => {
    // `name[]` is the exact substring to avoid — case-insensitive to
    // catch `NAME[]` variants too. The match must be inside the
    // COALESCE(update_columns) block only; `name[]` outside it (e.g.
    // in other catalog queries) is out of scope.
    const block = extractUpdateColumnsBlock();
    expect(block).not.toMatch(/name\s*\[\s*\]/i);
  });
});
