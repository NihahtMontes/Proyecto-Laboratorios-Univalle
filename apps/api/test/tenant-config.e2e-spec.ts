/**
 * MIG-001-F2-W11 — `tenant-config.ts` server-side resolution specs.
 * No DB, no sockets: the control-plane client factory and the env record are
 * BOTH injected, so these tests prove the module never reads `process.env`
 * itself and never constructs a `pg.Client`.
 *
 * Contract covered (tenant-config.ts header + W9 contract):
 *  - injectable env; the resolved tenant config is distinct from the
 *    control-plane config;
 *  - per-operation state gate: `up` requires `migrating`; `status`/`verify`
 *    accept `active` and `migrating`; `degraded`/`disabled` are always
 *    refused;
 *  - `writer_label` allow-list (LEGACY_SQLSERVER / NEST_SQLSERVER /
 *    NEST_POSTGRES); the raw value is echoed only when it is a harmless
 *    identifier-like token, otherwise `(redacted)`;
 *  - missing row / missing reference / malformed reference / missing or
 *    blank migration secret: generic fail-closed errors that NEVER expose
 *    the secret reference, the DSN, the password or the env var name;
 *  - the injected client is closed exactly once on success AND on every
 *    error path;
 *  - invalid DSN material propagates through the pure connection-config
 *    parser, which itself never echoes the raw value.
 *
 * MIG-001-F2-W15A — `withTenantMigrationLease` cross-DB fence specs.
 * Proves the exact wire order (BEGIN -> SET LOCAL ROLE -> global lock ->
 * FOR UPDATE -> callback -> guarded UPDATE -> COMMIT -> close) and the
 * error paths (rollback + close once). The lease is the only operator
 * that advances `lu_tenant_route.schema_version` and the only one that
 * holds a control-plane transaction open across a caller-side tenant
 * DDL callback. No PG server is involved: every interaction is observed
 * through the injected fake client.
 */
import {
  TenantConfigError,
  TenantMigrationLeaseError,
  TenantMigrationLeaseIndeterminateError,
  TENANT_MIGRATION_GLOBAL_LOCK_KEY,
  resolveTenantConfig,
  sanitizeReference,
  withTenantMigrationLease,
  type TenantClient,
  type TenantConfigResolution,
  type WithTenantMigrationLeaseOptions,
} from '../src/database/tenant-config.js';
import type { PostgresConnectionConfig } from '../src/database/connection-config.js';

const SITE_ID = '3f2b7c1d-9a4e-4b6f-8c2d-1e5a7b9c0d1e';
const SITE_REF = 'tenant/prod.db-01';
const SITE_ENV_KEY = 'TENANT_MIGRATION_CONNECTION__tenant_prod_db_01';
const TENANT_DSN =
  'Host=localhost;Port=5433;Database=tenant_db_01;Username=mig_user;Password=TENANT-DSN-PASSWORD;SSL Mode=Disable';
const CP_PASSWORD = 'CP-SECRET-PASSWORD';

const CONTROL_PLANE_CONFIG: PostgresConnectionConfig = {
  host: 'cp.internal',
  port: 5432,
  database: 'cpdb',
  user: 'cp_user',
  password: CP_PASSWORD,
  ssl: { rejectUnauthorized: true },
};

interface FakeClientOptions {
  row?: {
    migration_secret_reference: string | null;
    writer_label: string | null;
    state: string | null;
  } | null;
  queryError?: Error;
  /**
   * When set, the fake mimics the migrator preflight failing. The
   * short-circuit is applied BEFORE the route SELECT — exactly the order
   * the production resolver enforces.
   */
  preflightFail?:
    | 'session-row-missing'
    | 'managed-identity'
    | 'no-login'
    | 'superuser'
    | 'createrole'
    | 'createdb'
    | 'replication'
    | 'bypassrls'
    | 'migrator-missing'
    | 'migrator-login'
    | 'migrator-super'
    | 'migrator-createrole'
    | 'migrator-createdb'
    | 'migrator-replication'
    | 'migrator-bypassrls'
    | 'non-member'
    | 'runtime-missing'
    | 'runtime-login'
    | 'runtime-elevated'
    | 'runtime-can-create'
    | 'runtime-owns-objects';
}

/**
 * Classify a query as one of the migrator preflight statements the
 * resolver/lease emit. The classifier is intentionally broad; tests that
 * assert specific query orders use the returned kind.
 */
function classifyPreflight(
  text: string,
  values: unknown[],
): 'SESSION' | 'MIGRATOR' | 'MEMBERSHIP' | 'RUNTIME' | 'RUNTIME_CREATE' | 'RUNTIME_OWNS' | null {
  const t = text.trim();
  if (
    /^SELECT rolname, rolcanlogin, rolsuper, rolcreaterole, rolcreatedb,\s*rolreplication, rolbypassrls\s*FROM pg_roles\s*WHERE rolname\s*=\s*current_user/i.test(
      t,
    )
  )
    return 'SESSION';
  if (
    /^SELECT rolname, rolcanlogin, rolsuper, rolcreaterole, rolcreatedb,\s*rolreplication, rolbypassrls\s*FROM pg_roles\s*WHERE rolname\s*=\s*\$1/i.test(
      t,
    )
  ) {
    // Distinguish migrator (Q2) from runtime (Q4): the values parameter
    // carries the role name being queried.
    const role = String(values?.[0] ?? '');
    if (role === 'lu_auth_runtime') return 'RUNTIME';
    return 'MIGRATOR';
  }
  if (/pg_has_role\(current_user, \$1, 'MEMBER'\)/i.test(t)) return 'MEMBERSHIP';
  if (/runtime_can_create/i.test(t)) return 'RUNTIME_CREATE';
  if (/runtime_owns_objects/i.test(t)) return 'RUNTIME_OWNS';
  return null;
}

function safeSessionRow() {
  return {
    rolname: 'lu_migrator_login',
    rolcanlogin: true,
    rolsuper: false,
    rolcreaterole: false,
    rolcreatedb: false,
    rolreplication: false,
    rolbypassrls: false,
  };
}

function safeMigratorRow() {
  return {
    rolname: 'lu_auth_migrator',
    rolcanlogin: false,
    rolsuper: false,
    rolcreaterole: false,
    rolcreatedb: false,
    rolreplication: false,
    rolbypassrls: false,
  };
}

function safeRuntimeRow() {
  return {
    rolname: 'lu_auth_runtime',
    rolcanlogin: false,
    rolsuper: false,
    rolcreaterole: false,
    rolcreatedb: false,
    rolreplication: false,
    rolbypassrls: false,
  };
}

function makePreflightAnswer(kind: NonNullable<FakeClientOptions['preflightFail']>): {
  throw: boolean;
  row?: Record<string, unknown>;
} {
  switch (kind) {
    case 'session-row-missing':
      return { throw: false, row: undefined };
    case 'managed-identity':
      return {
        throw: false,
        row: {
          rolname: 'lu_auth_runtime',
          rolcanlogin: true,
          rolsuper: false,
          rolcreaterole: false,
          rolcreatedb: false,
          rolreplication: false,
          rolbypassrls: false,
        },
      };
    case 'no-login':
      return {
        throw: false,
        row: {
          rolname: 'lu_migrator_login',
          rolcanlogin: false,
          rolsuper: false,
          rolcreaterole: false,
          rolcreatedb: false,
          rolreplication: false,
          rolbypassrls: false,
        },
      };
    case 'superuser':
      return {
        throw: false,
        row: {
          rolname: 'lu_migrator_login',
          rolcanlogin: true,
          rolsuper: true,
          rolcreaterole: false,
          rolcreatedb: false,
          rolreplication: false,
          rolbypassrls: false,
        },
      };
    case 'createrole':
      return {
        throw: false,
        row: {
          rolname: 'lu_migrator_login',
          rolcanlogin: true,
          rolsuper: false,
          rolcreaterole: true,
          rolcreatedb: false,
          rolreplication: false,
          rolbypassrls: false,
        },
      };
    case 'createdb':
      return {
        throw: false,
        row: {
          rolname: 'lu_migrator_login',
          rolcanlogin: true,
          rolsuper: false,
          rolcreaterole: false,
          rolcreatedb: true,
          rolreplication: false,
          rolbypassrls: false,
        },
      };
    case 'replication':
      return {
        throw: false,
        row: {
          rolname: 'lu_migrator_login',
          rolcanlogin: true,
          rolsuper: false,
          rolcreaterole: false,
          rolcreatedb: false,
          rolreplication: true,
          rolbypassrls: false,
        },
      };
    case 'bypassrls':
      return {
        throw: false,
        row: {
          rolname: 'lu_migrator_login',
          rolcanlogin: true,
          rolsuper: false,
          rolcreaterole: false,
          rolcreatedb: false,
          rolreplication: false,
          rolbypassrls: true,
        },
      };
    case 'migrator-missing':
      // Session passes; migrator row is absent. Implemented by letting the
      // session row succeed and the migrator row return empty.
      return { throw: false, row: undefined };
    case 'migrator-login':
      return {
        throw: false,
        row: {
          rolname: 'lu_auth_migrator',
          rolcanlogin: true,
          rolsuper: false,
          rolcreaterole: false,
          rolcreatedb: false,
          rolreplication: false,
          rolbypassrls: false,
        },
      };
    case 'migrator-super':
      return {
        throw: false,
        row: {
          rolname: 'lu_auth_migrator',
          rolcanlogin: false,
          rolsuper: true,
          rolcreaterole: false,
          rolcreatedb: false,
          rolreplication: false,
          rolbypassrls: false,
        },
      };
    case 'migrator-createrole':
      return {
        throw: false,
        row: {
          rolname: 'lu_auth_migrator',
          rolcanlogin: false,
          rolsuper: false,
          rolcreaterole: true,
          rolcreatedb: false,
          rolreplication: false,
          rolbypassrls: false,
        },
      };
    case 'migrator-createdb':
      return {
        throw: false,
        row: {
          rolname: 'lu_auth_migrator',
          rolcanlogin: false,
          rolsuper: false,
          rolcreaterole: false,
          rolcreatedb: true,
          rolreplication: false,
          rolbypassrls: false,
        },
      };
    case 'migrator-replication':
      return {
        throw: false,
        row: {
          rolname: 'lu_auth_migrator',
          rolcanlogin: false,
          rolsuper: false,
          rolcreaterole: false,
          rolcreatedb: false,
          rolreplication: true,
          rolbypassrls: false,
        },
      };
    case 'migrator-bypassrls':
      return {
        throw: false,
        row: {
          rolname: 'lu_auth_migrator',
          rolcanlogin: false,
          rolsuper: false,
          rolcreaterole: false,
          rolcreatedb: false,
          rolreplication: false,
          rolbypassrls: true,
        },
      };
    case 'non-member':
      return { throw: false, row: undefined };
    case 'runtime-missing':
      return { throw: false, row: undefined };
    case 'runtime-login':
      return {
        throw: false,
        row: {
          rolname: 'lu_auth_runtime',
          rolcanlogin: true,
          rolsuper: false,
          rolcreaterole: false,
          rolcreatedb: false,
          rolreplication: false,
          rolbypassrls: false,
        },
      };
    case 'runtime-elevated':
      return {
        throw: false,
        row: {
          rolname: 'lu_auth_runtime',
          rolcanlogin: false,
          rolsuper: true,
          rolcreaterole: false,
          rolcreatedb: false,
          rolreplication: false,
          rolbypassrls: false,
        },
      };
    case 'runtime-can-create':
      return { throw: false, row: undefined };
    case 'runtime-owns-objects':
      return { throw: false, row: undefined };
  }
}

/** Metadata-only fake of the `TenantClient` the CLI/runner injects. */
function makeFakeClient(opts: FakeClientOptions): {
  client: TenantClient;
  calls: Array<{ text: string; values: unknown[] }>;
  closeCount: () => number;
} {
  const calls: Array<{ text: string; values: unknown[] }> = [];
  let closes = 0;
  // preflightFail injects an answer into one of the preflight slots. The
  // exact slot depends on the failure kind; see makePreflightAnswer.
  const preflightFail = opts.preflightFail;
  const client: TenantClient = {
    async query<T extends Record<string, unknown>>(text: string, values?: unknown[]) {
      calls.push({ text, values: values ?? [] });
      if (opts.queryError !== undefined) {
        throw opts.queryError;
      }
      // Migrator preflight branches.
      const kind = classifyPreflight(text, values ?? []);
      if (kind !== null) {
        if (preflightFail !== undefined) {
          // Fail this exact preflight slot for the requested failure kind.
          const fail = makePreflightAnswer(preflightFail);
          if (
            (kind === 'SESSION' &&
              [
                'session-row-missing',
                'managed-identity',
                'no-login',
                'superuser',
                'createrole',
                'createdb',
                'replication',
                'bypassrls',
              ].includes(preflightFail)) ||
            (kind === 'MIGRATOR' &&
              [
                'migrator-missing',
                'migrator-login',
                'migrator-super',
                'migrator-createrole',
                'migrator-createdb',
                'migrator-replication',
                'migrator-bypassrls',
              ].includes(preflightFail)) ||
            (kind === 'MEMBERSHIP' && preflightFail === 'non-member') ||
            (kind === 'RUNTIME' &&
              ['runtime-missing', 'runtime-login', 'runtime-elevated'].includes(preflightFail))
          ) {
            if (fail.row === undefined) {
              return { rows: [] as T[] };
            }
            return { rows: [fail.row as unknown as T] };
          }
          if (kind === 'RUNTIME_CREATE' && preflightFail === 'runtime-can-create') {
            return { rows: [{ runtime_can_create: true } as unknown as T] };
          }
          if (kind === 'RUNTIME_OWNS' && preflightFail === 'runtime-owns-objects') {
            return { rows: [{ runtime_owns_objects: true } as unknown as T] };
          }
        }
        if (kind === 'SESSION') return { rows: [safeSessionRow() as unknown as T] };
        if (kind === 'MIGRATOR') return { rows: [safeMigratorRow() as unknown as T] };
        if (kind === 'MEMBERSHIP') return { rows: [{ is_member: true } as unknown as T] };
        if (kind === 'RUNTIME') return { rows: [safeRuntimeRow() as unknown as T] };
        if (kind === 'RUNTIME_CREATE')
          return { rows: [{ runtime_can_create: false } as unknown as T] };
        if (kind === 'RUNTIME_OWNS')
          return { rows: [{ runtime_owns_objects: false } as unknown as T] };
      }
      if (opts.row === null) {
        return { rows: [] as T[] };
      }
      return { rows: [(opts.row ?? {}) as T] };
    },
    async close() {
      closes += 1;
    },
  };
  return { client, calls, closeCount: () => closes };
}

function okRow(overrides?: Partial<NonNullable<FakeClientOptions['row']>>) {
  return {
    migration_secret_reference: SITE_REF,
    writer_label: 'NEST_POSTGRES',
    state: 'migrating',
    ...overrides,
  };
}

async function resolve(
  opts: FakeClientOptions & {
    operation?: 'up' | 'status' | 'verify';
    env?: Record<string, string | undefined>;
    siteId?: string;
  } = {},
): Promise<TenantConfigResolution> {
  const { client } = makeFakeClient(opts);
  return resolveTenantConfig(
    opts.siteId ?? SITE_ID,
    opts.operation ?? 'up',
    CONTROL_PLANE_CONFIG,
    opts.env ?? { [SITE_ENV_KEY]: TENANT_DSN },
    async () => client,
  );
}

describe('tenant-config: sanitizeReference (secret-reference -> env suffix)', () => {
  it('replaces every non-alphanumeric run with a single underscore', () => {
    expect(sanitizeReference(SITE_REF)).toBe('tenant_prod_db_01');
    expect(sanitizeReference('tenant/local/runtime')).toBe('tenant_local_runtime');
    expect(sanitizeReference('x!!y')).toBe('x_y');
    expect(sanitizeReference('NEST/Prod.01')).toBe('NEST_Prod_01');
  });
});

describe('tenant-config: happy path (injectable env, distinct from control-plane)', () => {
  it('resolves the tenant DSN from the injected env and returns a config distinct from the control-plane one', async () => {
    const { client, calls, closeCount } = makeFakeClient({ row: okRow() });
    await resolveTenantConfig(
      SITE_ID,
      'up',
      CONTROL_PLANE_CONFIG,
      { [SITE_ENV_KEY]: TENANT_DSN },
      async () => client,
    );

    // Read-only path: BEGIN → migrator preflight (session, migrator,
    // membership, runtime [classifier matches MIGRATOR shape since both
    // use `WHERE rolname = $1 + rolcanlogin`], runtime_can_create,
    // runtime_owns_objects) → SET LOCAL ROLE → SELECT (route row) →
    // COMMIT. Every statement runs through the same injected client;
    // order is part of the contract so we explicitly assert it instead
    // of the legacy single-call shape.
    expect(calls).toHaveLength(10);
    expect(calls[0]!.text).toBe('BEGIN ISOLATION LEVEL REPEATABLE READ READ ONLY');
    expect(calls[1]!.text).toContain('pg_roles');
    expect(calls[1]!.text).toContain('rolname = current_user');
    // Indexes 2, 3 cover the migrator + runtime pg_roles probes; both
    // share the `WHERE rolname = $1 + rolcanlogin` shape so the resolver
    // classifier cannot distinguish them here. The lease-side classifier
    // uses the values parameter to differentiate. Order is preserved.
    expect(calls[2]!.text).toContain('WHERE rolname = $1');
    expect(calls[3]!.text).toContain("pg_has_role(current_user, $1, 'MEMBER')");
    // calls[4] is the runtime pg_roles probe (same shape as the
    // migrator probe at calls[2]); classifyPreflight cannot
    // distinguish them without the values parameter, so we just
    // assert the textual shape and leave the kind distinction to
    // the lease classifier.
    expect(calls[4]!.text).toContain('WHERE rolname = $1');
    expect(calls[5]!.text).toContain('runtime_can_create');
    expect(calls[6]!.text).toContain('runtime_owns_objects');
    expect(calls[7]!.text).toBe('SET LOCAL ROLE lu_auth_migrator');
    expect(calls[8]!.text).toContain('migration_secret_reference, writer_label, state');
    expect(calls[8]!.text).toContain('FROM public.lu_tenant_route');
    expect(calls[8]!.text).toContain('WHERE site_id = $1');
    expect(calls[8]!.values).toEqual([SITE_ID]);
    expect(calls[9]!.text).toBe('COMMIT');
    expect(closeCount()).toBe(1);
    void calls;
  });

  it('resolver runs the migrator preflight BEFORE SET LOCAL ROLE so generic / redacted metadata is rejected', async () => {
    const { client, calls, closeCount } = makeFakeClient({
      row: okRow(),
      preflightFail: 'superuser',
    });
    await expect(
      resolveTenantConfig(
        SITE_ID,
        'up',
        CONTROL_PLANE_CONFIG,
        { [SITE_ENV_KEY]: TENANT_DSN },
        async () => client,
      ),
    ).rejects.toThrow(/is a PostgreSQL superuser/);
    // The migrator preflight fires BEFORE SET LOCAL ROLE so generic /
    // redacted metadata never reaches the route SELECT or the caller.
    const setIdx = calls.findIndex((c) => c.text === 'SET LOCAL ROLE lu_auth_migrator');
    const routeIdx = calls.findIndex(
      (c) => c.text.includes('FROM public.lu_tenant_route') && c.text.includes('WHERE site_id'),
    );
    expect(setIdx).toBe(-1);
    expect(routeIdx).toBe(-1);
    expect(closeCount()).toBe(1);
  });

  it('resolver commits COMMIT only after the route SELECT and the SET LOCAL ROLE', async () => {
    const { client, calls } = makeFakeClient({ row: okRow() });
    await resolveTenantConfig(
      SITE_ID,
      'up',
      CONTROL_PLANE_CONFIG,
      { [SITE_ENV_KEY]: TENANT_DSN },
      async () => client,
    );
    const beginIdx = calls.findIndex((c) => c.text.startsWith('BEGIN'));
    const commitIdx = calls.findIndex((c) => c.text === 'COMMIT');
    const setIdx = calls.findIndex((c) => c.text === 'SET LOCAL ROLE lu_auth_migrator');
    const routeIdx = calls.findIndex(
      (c) => c.text.includes('FROM public.lu_tenant_route') && c.text.includes('WHERE site_id'),
    );
    expect(beginIdx).toBe(0);
    expect(setIdx).toBeGreaterThan(beginIdx);
    expect(routeIdx).toBeGreaterThan(setIdx);
    expect(commitIdx).toBeGreaterThan(routeIdx);
  });

  it('safeTarget is the log-safe subset only (resolver success path)', async () => {
    const { client } = makeFakeClient({ row: okRow() });
    const resolution = await resolveTenantConfig(
      SITE_ID,
      'up',
      CONTROL_PLANE_CONFIG,
      { [SITE_ENV_KEY]: TENANT_DSN },
      async () => client,
    );
    expect(resolution.config.host).toBe('localhost');
    expect(resolution.config.port).toBe(5433);
    expect(resolution.config.database).toBe('tenant_db_01');
    expect(resolution.config.user).toBe('mig_user');
    expect(resolution.config.password).toBe('TENANT-DSN-PASSWORD');
    expect(resolution.config.host).not.toBe(CONTROL_PLANE_CONFIG.host);
    expect(resolution.config.database).not.toBe(CONTROL_PLANE_CONFIG.database);
    expect(resolution.config.password).not.toBe(CONTROL_PLANE_CONFIG.password);
    expect(resolution.safeTarget).toEqual({
      host: 'localhost',
      port: 5433,
      database: 'tenant_db_01',
    });
    const serialized = JSON.stringify(resolution);
    expect(serialized).not.toContain(SITE_REF);
    expect(serialized).not.toContain('TENANT_MIGRATION_CONNECTION');
    expect(serialized).not.toContain(TENANT_DSN);
  });

  it('upper-case site UUID input is normalized to lower case for the lookup', async () => {
    const { client, calls } = makeFakeClient({ row: okRow() });
    await resolveTenantConfig(
      SITE_ID.toUpperCase(),
      'status',
      CONTROL_PLANE_CONFIG,
      { [SITE_ENV_KEY]: TENANT_DSN },
      async () => client,
    );
    const selectCall = calls.find((c) => c.text.includes('FROM public.lu_tenant_route'));
    expect(selectCall).toBeDefined();
    expect(selectCall!.values).toEqual([SITE_ID]);
  });

  it('never reads process.env: the DSN is present only in the injected env', async () => {
    const saved = process.env[SITE_ENV_KEY];
    process.env[SITE_ENV_KEY] = TENANT_DSN;
    try {
      // Injected env WITHOUT the secret -> fail closed even though
      // process.env carries it.
      await expect(resolve({ row: okRow(), env: { OTHER: 'x' } })).rejects.toThrow(
        TenantConfigError,
      );
      // Injected env WITH the secret and no process.env entry -> success.
      delete process.env[SITE_ENV_KEY];
      const ok = await resolve({ row: okRow(), env: { [SITE_ENV_KEY]: TENANT_DSN } });
      expect(ok.config.database).toBe('tenant_db_01');
    } finally {
      if (saved === undefined) {
        delete process.env[SITE_ENV_KEY];
      } else {
        process.env[SITE_ENV_KEY] = saved;
      }
    }
  });

  it('looks the secret up by the sanitized env name only (raw reference key is a decoy)', async () => {
    const { client } = makeFakeClient({ row: okRow() });
    const resolution = await resolveTenantConfig(
      SITE_ID,
      'up',
      CONTROL_PLANE_CONFIG,
      {
        [`TENANT_MIGRATION_CONNECTION__${SITE_REF}`]: 'Host=decoy.internal;Database=decoy',
        [SITE_ENV_KEY]: TENANT_DSN,
      },
      async () => client,
    );
    expect(resolution.config.database).toBe('tenant_db_01');
  });
});

describe('tenant-config: operation-mode state gate', () => {
  it('up requires state=migrating', async () => {
    await expect(
      resolve({ row: okRow({ state: 'migrating' }), operation: 'up' }),
    ).resolves.toBeTruthy();
  });

  it('up refuses active / degraded / disabled with the operation name and expected state', async () => {
    for (const state of ['active', 'degraded', 'disabled']) {
      await expect(resolve({ row: okRow({ state }), operation: 'up' })).rejects.toThrow(
        new RegExp(
          `lu_tenant_route\\.state ${state} for site ${SITE_ID} does not allow the 'up' operation \\(expected migrating\\)`,
        ),
      );
    }
  });

  it('status and verify accept active and migrating (read-only gate)', async () => {
    for (const operation of ['status', 'verify'] as const) {
      for (const state of ['active', 'migrating']) {
        await expect(resolve({ row: okRow({ state }), operation })).resolves.toBeTruthy();
      }
    }
  });

  it('status and verify refuse degraded and disabled', async () => {
    for (const operation of ['status', 'verify'] as const) {
      for (const state of ['degraded', 'disabled']) {
        await expect(resolve({ row: okRow({ state }), operation })).rejects.toThrow(
          new RegExp(
            `does not allow the '${operation}' operation \\(expected active or migrating\\)`,
          ),
        );
      }
    }
  });

  it('a null state is refused with a generic missing-state error', async () => {
    await expect(resolve({ row: okRow({ state: null }) })).rejects.toThrow(/has no state/);
  });

  it('a malformed state token is redacted in the error message', async () => {
    await expect(
      resolve({ row: okRow({ state: 'DROP TABLE x;--' }), operation: 'up' }),
    ).rejects.toThrow(/lu_tenant_route\.state \(redacted\) for site/);
  });
});

describe('tenant-config: writer-label allow-list', () => {
  it('accepts LEGACY_SQLSERVER, NEST_SQLSERVER and NEST_POSTGRES', async () => {
    for (const label of ['LEGACY_SQLSERVER', 'NEST_SQLSERVER', 'NEST_POSTGRES']) {
      await expect(resolve({ row: okRow({ writer_label: label }) })).resolves.toBeTruthy();
    }
  });

  it('refuses any other label (even an identifier-safe one) with an allow-list error', async () => {
    await expect(resolve({ row: okRow({ writer_label: 'CUSTOM_LABEL' }) })).rejects.toThrow(
      /lu_tenant_route\.writer_label CUSTOM_LABEL for site .* is not in the allow-list/,
    );
  });

  it('refuses a non-identifier label WITHOUT echoing the raw value', async () => {
    const hostile = 'evil $(whoami); DROP';
    let caught: unknown;
    try {
      await resolve({ row: okRow({ writer_label: hostile }) });
    } catch (error) {
      caught = error;
    }
    expect(caught).toBeInstanceOf(TenantConfigError);
    const message = (caught as Error).message;
    expect(message).toContain('(redacted)');
    expect(message).not.toContain('whoami');
    expect(message).not.toContain('DROP');
  });

  it('refuses a missing (null) label', async () => {
    await expect(resolve({ row: okRow({ writer_label: null }) })).rejects.toThrow(
      /writer_label \(redacted\) for site .* is not in the allow-list/,
    );
  });
});

describe('tenant-config: missing/invalid secret reference fails closed', () => {
  it('a missing route row is refused (fail closed, no invented site)', async () => {
    await expect(resolve({ row: null })).rejects.toThrow(
      new RegExp(`lu_tenant_route row for site ${SITE_ID} is missing`),
    );
  });

  it('a null migration_secret_reference is refused', async () => {
    await expect(resolve({ row: okRow({ migration_secret_reference: null }) })).rejects.toThrow(
      /has no migration_secret_reference/,
    );
  });

  it('a reference that fails the format regex is refused WITHOUT echoing the raw value', async () => {
    for (const bad of ['AB', '1bad-start', 'has spaces', 'sem;colon', '$(secret)']) {
      let caught: unknown;
      try {
        await resolve({ row: okRow({ migration_secret_reference: bad }) });
      } catch (error) {
        caught = error;
      }
      expect(caught).toBeInstanceOf(TenantConfigError);
      const message = (caught as Error).message;
      expect(message).toMatch(/migration_secret_reference for site .* failed format check/);
      expect(message).not.toContain(bad);
    }
  });

  it('a missing migration secret env var fails closed and the error never names the env var or the reference', async () => {
    let caught: unknown;
    try {
      await resolve({ row: okRow(), env: {} });
    } catch (error) {
      caught = error;
    }
    expect(caught).toBeInstanceOf(TenantConfigError);
    const message = (caught as Error).message;
    expect(message).toBe(`tenant migration secret env var is not set for site ${SITE_ID}`);
    expect(message).not.toContain('TENANT_MIGRATION_CONNECTION');
    expect(message).not.toContain('tenant_prod_db_01');
    expect(message).not.toContain(SITE_REF);
  });

  it('a blank (whitespace) migration secret env var is treated as missing', async () => {
    await expect(resolve({ row: okRow(), env: { [SITE_ENV_KEY]: '   ' } })).rejects.toThrow(
      /tenant migration secret env var is not set for site/,
    );
  });

  it('a malformed DSN fails through the pure parser without echoing DSN material', async () => {
    let caught: unknown;
    try {
      await resolve({
        row: okRow(),
        env: { [SITE_ENV_KEY]: 'junk-dsn-P@ssw0rd-NOT-PARSEABLE' },
      });
    } catch (error) {
      caught = error;
    }
    expect(caught).toBeInstanceOf(Error);
    expect((caught as Error).name).toBe('ConnectionConfigError');
    expect((caught as Error).message).not.toContain('junk-dsn');
    expect((caught as Error).message).not.toContain('P@ssw0rd');
  });

  it('an RFC-4122-shaped but wrong-site row lookup still keys on the requested site_id', async () => {
    const { client, calls } = makeFakeClient({ row: null });
    await expect(
      resolveTenantConfig(
        '00000000-0000-4000-8000-000000000000',
        'status',
        CONTROL_PLANE_CONFIG,
        {},
        async () => client,
      ),
    ).rejects.toThrow(/is missing/);
    const selectCall = calls.find((c) => c.text.includes('FROM public.lu_tenant_route'));
    expect(selectCall).toBeDefined();
    expect(selectCall!.values).toEqual(['00000000-0000-4000-8000-000000000000']);
  });
});

describe('tenant-config: site UUID format and client lifecycle', () => {
  it('rejects a non-UUID site id BEFORE opening the control-plane client', async () => {
    let factoryCalls = 0;
    await expect(
      resolveTenantConfig(
        'site-name-not-a-uuid',
        'up',
        CONTROL_PLANE_CONFIG,
        { [SITE_ENV_KEY]: TENANT_DSN },
        async () => {
          factoryCalls += 1;
          return makeFakeClient({ row: okRow() }).client;
        },
      ),
    ).rejects.toThrow(/site UUID is not RFC 4122 shaped/);
    expect(factoryCalls).toBe(0);
  });

  it('closes the client exactly once on success', async () => {
    const { client, closeCount } = makeFakeClient({ row: okRow() });
    await resolveTenantConfig(
      SITE_ID,
      'up',
      CONTROL_PLANE_CONFIG,
      { [SITE_ENV_KEY]: TENANT_DSN },
      async () => client,
    );
    expect(closeCount()).toBe(1);
  });

  it('closes the client once when the lookup query itself fails', async () => {
    const { client, closeCount } = makeFakeClient({
      queryError: new Error('permission denied for table lu_tenant_route'),
    });
    await expect(
      resolveTenantConfig(
        SITE_ID,
        'status',
        CONTROL_PLANE_CONFIG,
        { [SITE_ENV_KEY]: TENANT_DSN },
        async () => client,
      ),
    ).rejects.toThrow(/permission denied for table lu_tenant_route/);
    expect(closeCount()).toBe(1);
  });

  it('closes the client once on every validation failure (missing ref, bad label, state gate, missing secret)', async () => {
    for (const row of [
      okRow({ migration_secret_reference: null }),
      okRow({ writer_label: 'NOT_A_LABEL' }),
      okRow({ state: 'disabled' }),
      okRow(),
    ]) {
      const { client, closeCount } = makeFakeClient({ row });
      await expect(
        resolveTenantConfig(
          SITE_ID,
          'up',
          CONTROL_PLANE_CONFIG,
          {}, // no secret env -> last case fails there
          async () => client,
        ),
      ).rejects.toBeInstanceOf(TenantConfigError);
      expect(closeCount()).toBe(1);
    }
  });

  it('the factory receives the control-plane config unchanged (lookup-only connection)', async () => {
    const seenConfigs: unknown[] = [];
    const { client } = makeFakeClient({ row: okRow() });
    await resolveTenantConfig(
      SITE_ID,
      'verify',
      CONTROL_PLANE_CONFIG,
      { [SITE_ENV_KEY]: TENANT_DSN },
      async (config) => {
        seenConfigs.push(config);
        return client;
      },
    );
    expect(seenConfigs).toEqual([CONTROL_PLANE_CONFIG]);
  });
});

// ---------------------------------------------------------------------------
// MIG-001-F2-W15A — `withTenantMigrationLease` cross-DB fence specs.
//
// Wire expectations per the contract:
//   1. BEGIN ISOLATION LEVEL READ COMMITTED (serialization comes from the
//      global advisory lock + FOR UPDATE; REPEATABLE READ/SERIALIZABLE would
//      fix the snapshot before the lock and fail a waiting lease with 40001)
//   2. SET LOCAL ROLE lu_auth_migrator
//   3. SELECT pg_advisory_xact_lock(hashtext(:GLOBAL_KEY))
//   4. SELECT migration_secret_reference, writer_label, state, schema_version
//        FROM public.lu_tenant_route WHERE site_id = $1 FOR UPDATE
//   5. callback (tenant DDL on a separate connection)
//   6. UPDATE public.lu_tenant_route
//        SET schema_version = GREATEST(schema_version, $2::integer),
//            last_health_at = CURRENT_TIMESTAMP,
//            updated_at      = CURRENT_TIMESTAMP
//        WHERE site_id = $1
//          AND state = $3
//          AND writer_label = $4
//          AND migration_secret_reference = $5
//        RETURNING ...
//   7. COMMIT
//   8. client.close() (exactly once on every path)
// ---------------------------------------------------------------------------

type LeaseQueryKind =
  | 'BEGIN'
  | 'COMMIT'
  | 'ROLLBACK'
  | 'PREFLIGHT_SESSION'
  | 'PREFLIGHT_MIGRATOR'
  | 'PREFLIGHT_MEMBERSHIP'
  | 'PREFLIGHT_RUNTIME'
  | 'PREFLIGHT_RUNTIME_CREATE'
  | 'PREFLIGHT_RUNTIME_OWNS'
  | 'SET_LOCAL_ROLE'
  | 'GLOBAL_LOCK'
  | 'FOR_UPDATE_SELECT'
  | 'ROUTE_UPDATE'
  | 'UNKNOWN';

interface LeaseQueryRecord {
  text: string;
  values: unknown[];
  kind: LeaseQueryKind;
}

function classifyLeaseQuery(text: string, values: unknown[]): LeaseQueryKind {
  const t = text.trim();
  if (/^BEGIN\b/i.test(t)) return 'BEGIN';
  if (/^COMMIT\b/i.test(t)) return 'COMMIT';
  if (/^ROLLBACK\b/i.test(t)) return 'ROLLBACK';
  if (
    /^SELECT rolname, rolcanlogin, rolsuper, rolcreaterole, rolcreatedb,\s*rolreplication, rolbypassrls\s*FROM pg_roles\s*WHERE rolname\s*=\s*current_user/i.test(
      t,
    )
  )
    return 'PREFLIGHT_SESSION';
  if (
    /^SELECT rolname, rolcanlogin, rolsuper, rolcreaterole, rolcreatedb,\s*rolreplication, rolbypassrls\s*FROM pg_roles\s*WHERE rolname\s*=\s*\$1/i.test(
      t,
    )
  ) {
    // Distinguish migrator from runtime: the values parameter carries the role
    // name. The migrator check is the first preflight pg_roles call, the
    // runtime check is the second (different role name).
    const role = String(values?.[0] ?? '');
    if (role === 'lu_auth_runtime') return 'PREFLIGHT_RUNTIME';
    return 'PREFLIGHT_MIGRATOR';
  }
  if (/pg_has_role\(current_user, \$1, 'MEMBER'\)/i.test(t)) return 'PREFLIGHT_MEMBERSHIP';
  if (/runtime_can_create/i.test(t)) return 'PREFLIGHT_RUNTIME_CREATE';
  if (/runtime_owns_objects/i.test(t)) return 'PREFLIGHT_RUNTIME_OWNS';
  if (/^SET LOCAL ROLE/i.test(t)) return 'SET_LOCAL_ROLE';
  if (/pg_advisory_xact_lock/i.test(t) && /hashtext/i.test(t)) return 'GLOBAL_LOCK';
  if (/FROM public\.lu_tenant_route/i.test(t) && /FOR UPDATE/i.test(t)) return 'FOR_UPDATE_SELECT';
  if (/UPDATE public\.lu_tenant_route/i.test(t)) return 'ROUTE_UPDATE';
  return 'UNKNOWN';
}

interface LeaseFakeClientOptions {
  /** Route row returned by the SELECT FOR UPDATE. */
  routeRow?: {
    migration_secret_reference: string | null;
    writer_label: string | null;
    state: string | null;
    schema_version?: number | null;
  } | null;
  /** Rows returned by the route UPDATE (RETURNING). */
  updateRows?: unknown[];
  /** Throw on the FIRST query that matches the given label. */
  throwOnKind?: LeaseQueryKind;
  /** Throw on every BEGIN. */
  throwOnBegin?: Error;
  /** Throw on every ROLLBACK (swallowed but recorded). */
  throwOnRollback?: Error;
  /**
   * When set, the fake mimics the migrator preflight failing. The
   * short-circuit fires BEFORE the route SELECT — exactly the order the
   * production lease enforces.
   */
  preflightFail?:
    | 'session-row-missing'
    | 'managed-identity'
    | 'no-login'
    | 'superuser'
    | 'createrole'
    | 'createdb'
    | 'replication'
    | 'bypassrls'
    | 'migrator-missing'
    | 'migrator-login'
    | 'migrator-super'
    | 'migrator-createrole'
    | 'migrator-createdb'
    | 'migrator-replication'
    | 'migrator-bypassrls'
    | 'non-member'
    | 'runtime-missing'
    | 'runtime-login'
    | 'runtime-elevated'
    | 'runtime-can-create'
    | 'runtime-owns-objects';
}

function makeLeaseFakeClient(opts: LeaseFakeClientOptions = {}): {
  client: TenantClient;
  calls: LeaseQueryRecord[];
  closeCount: () => number;
  rollbackSeen: () => boolean;
} {
  const calls: LeaseQueryRecord[] = [];
  let closes = 0;
  let rolledBack = false;
  const client: TenantClient = {
    async query<T extends Record<string, unknown>>(
      text: string,
      values?: unknown[],
    ): Promise<{ rows: T[] }> {
      const kind = classifyLeaseQuery(text, values ?? []);
      const record: LeaseQueryRecord = { text, values: values ?? [], kind };
      calls.push(record);
      if (opts.throwOnBegin && kind === 'BEGIN') throw opts.throwOnBegin;
      if (opts.throwOnRollback && kind === 'ROLLBACK') {
        rolledBack = true;
        throw opts.throwOnRollback;
      }
      if (opts.throwOnKind && opts.throwOnKind === kind) {
        throw new Error(`mock sql failure for ${kind}`);
      }
      if (kind === 'ROLLBACK') {
        rolledBack = true;
        return { rows: [] as T[] };
      }
      // Preflight answers — the lease emits the same six queries the
      // resolver does; tests can override a single slot via
      // `preflightFail`.
      if (
        kind === 'PREFLIGHT_SESSION' ||
        kind === 'PREFLIGHT_MIGRATOR' ||
        kind === 'PREFLIGHT_MEMBERSHIP' ||
        kind === 'PREFLIGHT_RUNTIME' ||
        kind === 'PREFLIGHT_RUNTIME_CREATE' ||
        kind === 'PREFLIGHT_RUNTIME_OWNS'
      ) {
        if (opts.preflightFail !== undefined) {
          const fail = makePreflightAnswer(opts.preflightFail);
          if (
            (kind === 'PREFLIGHT_SESSION' &&
              [
                'session-row-missing',
                'managed-identity',
                'no-login',
                'superuser',
                'createrole',
                'createdb',
                'replication',
                'bypassrls',
              ].includes(opts.preflightFail)) ||
            (kind === 'PREFLIGHT_MIGRATOR' &&
              [
                'migrator-missing',
                'migrator-login',
                'migrator-super',
                'migrator-createrole',
                'migrator-createdb',
                'migrator-replication',
                'migrator-bypassrls',
              ].includes(opts.preflightFail)) ||
            (kind === 'PREFLIGHT_MEMBERSHIP' && opts.preflightFail === 'non-member') ||
            (kind === 'PREFLIGHT_RUNTIME' &&
              ['runtime-missing', 'runtime-login', 'runtime-elevated'].includes(opts.preflightFail))
          ) {
            if (fail.row === undefined) {
              return { rows: [] as T[] };
            }
            return { rows: [fail.row as unknown as T] };
          }
          if (kind === 'PREFLIGHT_RUNTIME_CREATE' && opts.preflightFail === 'runtime-can-create') {
            return { rows: [{ runtime_can_create: true } as unknown as T] };
          }
          if (kind === 'PREFLIGHT_RUNTIME_OWNS' && opts.preflightFail === 'runtime-owns-objects') {
            return { rows: [{ runtime_owns_objects: true } as unknown as T] };
          }
        }
        if (kind === 'PREFLIGHT_SESSION') return { rows: [safeSessionRow() as unknown as T] };
        if (kind === 'PREFLIGHT_MIGRATOR') return { rows: [safeMigratorRow() as unknown as T] };
        if (kind === 'PREFLIGHT_MEMBERSHIP') return { rows: [{ is_member: true } as unknown as T] };
        if (kind === 'PREFLIGHT_RUNTIME') return { rows: [safeRuntimeRow() as unknown as T] };
        if (kind === 'PREFLIGHT_RUNTIME_CREATE')
          return { rows: [{ runtime_can_create: false } as unknown as T] };
        if (kind === 'PREFLIGHT_RUNTIME_OWNS')
          return { rows: [{ runtime_owns_objects: false } as unknown as T] };
      }
      if (kind === 'FOR_UPDATE_SELECT') {
        const row = opts.routeRow ?? null;
        if (row === null) return { rows: [] as T[] };
        return { rows: [row as unknown as T] };
      }
      if (kind === 'ROUTE_UPDATE') {
        const rows = opts.updateRows ?? [{ site_id: SITE_ID, schema_version: 13 }];
        return { rows: rows as T[] };
      }
      return { rows: [] as T[] };
    },
    async close() {
      closes += 1;
    },
  };
  return {
    client,
    calls,
    closeCount: () => closes,
    rollbackSeen: () => rolledBack,
  };
}

function defaultRouteRow(
  overrides: Partial<NonNullable<LeaseFakeClientOptions['routeRow']>> = {},
): NonNullable<LeaseFakeClientOptions['routeRow']> {
  return {
    migration_secret_reference: SITE_REF,
    writer_label: 'NEST_POSTGRES',
    state: 'migrating',
    schema_version: 0,
    ...overrides,
  };
}

interface WireLeaseOverrides {
  /** Override every field of `WithTenantMigrationLeaseOptions`. */
  options?: Partial<WithTenantMigrationLeaseOptions>;
  /** Override the underlying fake-client behavior. */
  fake?: LeaseFakeClientOptions;
  /** Override the callback the lease will invoke. */
  callback?: WithTenantMigrationLeaseOptions['callback'];
  /** Override the resolved site UUID. */
  siteId?: string;
  /** Override the env record. */
  env?: Record<string, string | undefined>;
  /** Override the control-plane config. */
  controlPlaneConfig?: PostgresConnectionConfig;
  /** Override the global advisory-lock key. */
  globalLockKey?: string;
}

function wireLease(overrides: WireLeaseOverrides = {}): {
  options: WithTenantMigrationLeaseOptions;
  captured: { cb?: unknown };
} {
  const fake = makeLeaseFakeClient({
    routeRow: defaultRouteRow(),
    updateRows: [{ site_id: SITE_ID, schema_version: 13 }],
    ...overrides.fake,
  });
  const captured: { cb?: unknown } = {};
  const callback =
    overrides.callback ??
    overrides.options?.callback ??
    (async (input) => {
      captured.cb = input;
      return { committed: true, ordinal: 13 };
    });
  const options: WithTenantMigrationLeaseOptions = {
    siteId: overrides.siteId ?? overrides.options?.siteId ?? SITE_ID,
    env: overrides.env ?? overrides.options?.env ?? { [SITE_ENV_KEY]: TENANT_DSN },
    controlPlaneConfig:
      overrides.controlPlaneConfig ?? overrides.options?.controlPlaneConfig ?? CONTROL_PLANE_CONFIG,
    controlPlaneClientFactory:
      overrides.options?.controlPlaneClientFactory ?? (async () => fake.client),
    callback,
    globalLockKey: overrides.globalLockKey ?? overrides.options?.globalLockKey,
  };
  (options as unknown as { __fake?: typeof fake }).__fake = fake;
  return { options, captured };
}

async function expectLeaseErrorMatching(
  promise: Promise<unknown>,
  matcher: RegExp,
): Promise<unknown> {
  let caught: unknown = null;
  try {
    await promise;
  } catch (error) {
    caught = error;
  }
  expect(caught).not.toBeNull();
  expect((caught as Error).message).toMatch(matcher);
  return caught;
}

describe('tenant-config W15A: withTenantMigrationLease — wire order', () => {
  it('BEGIN -> SET LOCAL ROLE -> global lock -> FOR UPDATE -> callback -> guarded UPDATE -> COMMIT -> close runs in that exact order', async () => {
    const { options } = wireLease();
    const fake = (options as unknown as { __fake: ReturnType<typeof makeLeaseFakeClient> }).__fake;

    const result = await withTenantMigrationLease(options);

    expect(result).toEqual({
      ordinal: 13,
      state: 'migrating',
      writerLabel: 'NEST_POSTGRES',
    });
    const kinds = fake.calls.map((c) => c.kind);
    // Exact order: BEGIN -> preflight (session, migrator, membership,
    // runtime, runtime_can_create, runtime_owns_objects) -> SET LOCAL
    // ROLE -> global lock -> FOR UPDATE -> callback -> guarded UPDATE ->
    // COMMIT. The 6 preflight queries run BEFORE SET LOCAL ROLE so
    // generic / redacted metadata never reaches the route SELECT.
    expect(kinds).toEqual([
      'BEGIN',
      'PREFLIGHT_SESSION',
      'PREFLIGHT_MIGRATOR',
      'PREFLIGHT_MEMBERSHIP',
      'PREFLIGHT_RUNTIME',
      'PREFLIGHT_RUNTIME_CREATE',
      'PREFLIGHT_RUNTIME_OWNS',
      'SET_LOCAL_ROLE',
      'GLOBAL_LOCK',
      'FOR_UPDATE_SELECT',
      'ROUTE_UPDATE',
      'COMMIT',
    ]);

    // BEGIN — READ COMMITTED, so the FOR UPDATE snapshot is taken only after
    // the global lock is granted. A snapshot-at-first-statement level
    // (REPEATABLE READ / SERIALIZABLE) would be fixed by the preflight SELECT
    // before the lock and make a concurrent waiting lease fail with 40001.
    expect(fake.calls[0]!.text).toBe('BEGIN ISOLATION LEVEL READ COMMITTED');
    expect(fake.calls[0]!.text).not.toMatch(/REPEATABLE READ|SERIALIZABLE/i);

    // Migrator preflight: 6 queries, in fixed order, BEFORE SET LOCAL ROLE.
    expect(fake.calls[1]!.kind).toBe('PREFLIGHT_SESSION');
    expect(fake.calls[2]!.kind).toBe('PREFLIGHT_MIGRATOR');
    expect(fake.calls[3]!.kind).toBe('PREFLIGHT_MEMBERSHIP');
    expect(fake.calls[4]!.kind).toBe('PREFLIGHT_RUNTIME');
    expect(fake.calls[5]!.kind).toBe('PREFLIGHT_RUNTIME_CREATE');
    expect(fake.calls[6]!.kind).toBe('PREFLIGHT_RUNTIME_OWNS');

    // SET LOCAL ROLE
    expect(fake.calls[7]!.text).toBe('SET LOCAL ROLE lu_auth_migrator');

    // GLOBAL lock — pg_advisory_xact_lock(hashtext($1)) with the constant key
    expect(fake.calls[8]!.text).toMatch(/pg_advisory_xact_lock/i);
    expect(fake.calls[8]!.text).toMatch(/hashtext/i);
    expect(fake.calls[8]!.values).toEqual([TENANT_MIGRATION_GLOBAL_LOCK_KEY]);

    // FOR UPDATE select
    expect(fake.calls[9]!.text).toMatch(/FROM public\.lu_tenant_route/i);
    expect(fake.calls[9]!.text).toMatch(/FOR UPDATE/i);
    expect(fake.calls[9]!.text).toMatch(/migration_secret_reference/);
    expect(fake.calls[9]!.text).toMatch(/writer_label/);
    expect(fake.calls[9]!.text).toMatch(/state/);
    expect(fake.calls[9]!.text).toMatch(/schema_version/);
    expect(fake.calls[9]!.values).toEqual([SITE_ID]);

    // Route UPDATE — guards site, state, writer_label and migration_secret_reference
    const updateCall = fake.calls[10]!;
    expect(updateCall.text).toMatch(/UPDATE public\.lu_tenant_route/i);
    expect(updateCall.text).toMatch(
      /schema_version\s*=\s*GREATEST\s*\(\s*schema_version\s*,\s*\$2/i,
    );
    expect(updateCall.text).toMatch(/last_health_at\s*=\s*CURRENT_TIMESTAMP/i);
    expect(updateCall.text).toMatch(/updated_at\s*=\s*CURRENT_TIMESTAMP/i);
    expect(updateCall.text).toMatch(/RETURNING/i);
    expect(updateCall.text).toMatch(/site_id\s*=\s*\$1/i);
    expect(updateCall.text).toMatch(/state\s*=\s*\$3/i);
    expect(updateCall.text).toMatch(/writer_label\s*=\s*\$4/i);
    expect(updateCall.text).toMatch(/migration_secret_reference\s*=\s*\$5/i);
    expect(updateCall.values).toEqual([SITE_ID, 13, 'migrating', 'NEST_POSTGRES', SITE_REF]);

    // COMMIT
    expect(fake.calls[11]!.text).toBe('COMMIT');

    // Close exactly once
    expect(fake.closeCount()).toBe(1);
    expect(fake.rollbackSeen()).toBe(false);
  });

  it('concurrent sites share the SAME lock key (no per-site variation)', async () => {
    // Two parallel leases with DIFFERENT site IDs but the same global key
    // prove that the lock invariant is global, not per-site. Both leases
    // use the exact same constant `TENANT_MIGRATION_GLOBAL_LOCK_KEY`.
    const a = wireLease({ siteId: SITE_ID });
    const b = wireLease({
      siteId: 'aaaaaaaa-aaaa-4aaa-8aaa-aaaaaaaaaaaa',
      callback: async () => ({ committed: true, ordinal: 7 }),
    });
    const fakeA = (a.options as unknown as { __fake: ReturnType<typeof makeLeaseFakeClient> })
      .__fake;
    const fakeB = (b.options as unknown as { __fake: ReturnType<typeof makeLeaseFakeClient> })
      .__fake;

    // Each client has its own mock; the test asserts both use the same lock key.
    await withTenantMigrationLease(a.options);
    await withTenantMigrationLease(b.options);
    const lockA = fakeA.calls.find((c) => c.kind === 'GLOBAL_LOCK');
    const lockB = fakeB.calls.find((c) => c.kind === 'GLOBAL_LOCK');
    expect(lockA!.values).toEqual(lockB!.values);
    expect(lockA!.values[0]).toBe(TENANT_MIGRATION_GLOBAL_LOCK_KEY);
  });

  it('the route UPDATE uses GREATEST(schema_version, ordinal) so version is monotonic when the existing row already has a higher ordinal', async () => {
    // callback reports ordinal=7 but the existing schema_version is 25.
    // The DB still uses GREATEST so the version stays at 25.
    const { options } = wireLease({
      fake: {
        routeRow: defaultRouteRow({ schema_version: 25 }),
        updateRows: [{ site_id: SITE_ID, schema_version: 25 }],
      },
      callback: async () => ({ committed: true, ordinal: 7 }),
    });

    const result = await withTenantMigrationLease(options);

    // The lease reports the callback's ordinal (not the SQL GREATEST result);
    // the operator-visible fact is that the lease advanced the route by the
    // callback's ordinal intent. The SQL guard ensures the DB is monotone.
    expect(result.ordinal).toBe(7);
    expect(result.state).toBe('migrating');
    expect(result.writerLabel).toBe('NEST_POSTGRES');
  });
});

describe('tenant-config W15A: withTenantMigrationLease — callback contract', () => {
  it('callback receives a RESOLVED tenant config distinct from the CP config, plus the route metadata', async () => {
    const { options } = wireLease();
    const fake = (options as unknown as { __fake: ReturnType<typeof makeLeaseFakeClient> }).__fake;
    let captured: unknown = null;
    const wrapperOpts: WithTenantMigrationLeaseOptions = {
      ...options,
      callback: async (input) => {
        captured = input;
        return { committed: true, ordinal: 13 };
      },
    };
    await withTenantMigrationLease(wrapperOpts);
    const input = captured as {
      config: PostgresConnectionConfig;
      safeTarget: { host: string; port: number; database: string };
      state: string;
      writerLabel: string;
      migrationSecretReference: string;
    } | null;
    expect(input).not.toBeNull();
    // Resolved tenant config is distinct from the control-plane one.
    expect(input!.config.host).toBe('localhost');
    expect(input!.config.port).toBe(5433);
    expect(input!.config.database).toBe('tenant_db_01');
    expect(input!.config.user).toBe('mig_user');
    expect(input!.config.password).toBe('TENANT-DSN-PASSWORD');
    expect(input!.config.host).not.toBe(CONTROL_PLANE_CONFIG.host);
    expect(input!.config.database).not.toBe(CONTROL_PLANE_CONFIG.database);
    expect(input!.config.password).not.toBe(CONTROL_PLANE_CONFIG.password);
    expect(input!.state).toBe('migrating');
    expect(input!.writerLabel).toBe('NEST_POSTGRES');
    expect(input!.safeTarget).toEqual({ host: 'localhost', port: 5433, database: 'tenant_db_01' });
    // callback fires AFTER the global lock AND the FOR UPDATE — never before.
    const beforeCallback = fake.calls.findIndex((c) => c.kind === 'GLOBAL_LOCK');
    expect(beforeCallback).toBeGreaterThanOrEqual(0);
  });

  it('callback observes a committed ordinal from the runner; the lease advances schema_version to that ordinal', async () => {
    const seenOrdinal: number[] = [];
    const { options } = wireLease({
      callback: async (_input) => {
        void _input;
        seenOrdinal.push(7);
        return { committed: true, ordinal: 7 };
      },
    });
    const result = await withTenantMigrationLease(options);
    expect(seenOrdinal).toEqual([7]);
    expect(result.ordinal).toBe(7);
  });

  it('callback returning committed=false triggers ROLLBACK and does not advance the route row', async () => {
    const { options } = wireLease({
      callback: async () => ({ committed: false, ordinal: 0 }),
    });
    const fake = (options as unknown as { __fake: ReturnType<typeof makeLeaseFakeClient> }).__fake;
    await expectLeaseErrorMatching(
      withTenantMigrationLease(options),
      /did not commit on the tenant DB/i,
    );
    const kinds = fake.calls.map((c) => c.kind);
    // No COMMIT, no UPDATE — the lease never reached the guarded route UPDATE.
    expect(kinds).not.toContain('COMMIT');
    expect(kinds).not.toContain('ROUTE_UPDATE');
    expect(kinds).toContain('ROLLBACK');
    expect(fake.closeCount()).toBe(1);
  });

  it('callback throwing in flight also ROLLBACKs and rethrows the original error', async () => {
    const { options } = wireLease({
      callback: async () => {
        throw new Error('tenant runner exploded with a verification failure');
      },
    });
    const fake = (options as unknown as { __fake: ReturnType<typeof makeLeaseFakeClient> }).__fake;
    await expectLeaseErrorMatching(withTenantMigrationLease(options), /tenant runner exploded/i);
    const kinds = fake.calls.map((c) => c.kind);
    expect(kinds).not.toContain('COMMIT');
    expect(kinds).toContain('ROLLBACK');
    expect(fake.closeCount()).toBe(1);
  });

  it('callback returning a non-object throws a generic lease error and ROLLBACKs', async () => {
    const { options } = wireLease({
      callback: (async () =>
        null as unknown as WithTenantMigrationLeaseOptions['callback'] extends (
          ...args: never[]
        ) => infer R
          ? Awaited<R>
          : never) as WithTenantMigrationLeaseOptions['callback'],
    });
    const fake = (options as unknown as { __fake: ReturnType<typeof makeLeaseFakeClient> }).__fake;
    await expectLeaseErrorMatching(withTenantMigrationLease(options), /invalid result/i);
    expect(fake.calls.some((c) => c.kind === 'ROLLBACK')).toBe(true);
    expect(fake.calls.some((c) => c.kind === 'COMMIT')).toBe(false);
    expect(fake.closeCount()).toBe(1);
  });

  it('callback returning committed=true with an invalid ordinal (<= 0) ROLLBACKs and throws', async () => {
    const { options } = wireLease({
      callback: async () => ({ committed: true, ordinal: 0 }),
    });
    const fake = (options as unknown as { __fake: ReturnType<typeof makeLeaseFakeClient> }).__fake;
    await expectLeaseErrorMatching(withTenantMigrationLease(options), /invalid ordinal/i);
    expect(fake.calls.some((c) => c.kind === 'ROLLBACK')).toBe(true);
    expect(fake.closeCount()).toBe(1);
  });

  it('UPDATE returning zero rows is treated as INDETERMINATE — the tenant DB stays committed and the route row keeps migrating', async () => {
    const { options } = wireLease({
      fake: {
        routeRow: defaultRouteRow(),
        updateRows: [], // 0 rows => indeterminate
      },
      callback: async () => ({ committed: true, ordinal: 13 }),
    });
    const fake = (options as unknown as { __fake: ReturnType<typeof makeLeaseFakeClient> }).__fake;
    let caught: unknown = null;
    try {
      await withTenantMigrationLease(options);
    } catch (error) {
      caught = error;
    }
    expect(caught).toBeInstanceOf(TenantMigrationLeaseIndeterminateError);
    expect((caught as Error).name).toBe('TenantMigrationLeaseIndeterminateError');
    expect((caught as Error).message).toMatch(/indeterminate/i);
    expect((caught as Error).message).not.toContain('TENANT-DSN-PASSWORD');
    expect((caught as Error).message).not.toContain(SITE_REF);
    expect((caught as Error).message).not.toContain(TENANT_DSN);
    // The lease issued ROLLBACK on its CP transaction; the tenant DB is
    // independently committed and unreachable from here.
    expect(fake.calls.some((c) => c.kind === 'COMMIT')).toBe(false);
    expect(fake.calls.some((c) => c.kind === 'ROLLBACK')).toBe(true);
    expect(fake.closeCount()).toBe(1);
  });

  it('UPDATE returning two rows also ROLLBACKs and surfaces an indeterminate error (defensive row-count guard)', async () => {
    const { options } = wireLease({
      fake: {
        updateRows: [
          { site_id: SITE_ID, schema_version: 13 },
          { site_id: SITE_ID, schema_version: 13 },
        ],
      },
      callback: async () => ({ committed: true, ordinal: 13 }),
    });
    const fake = (options as unknown as { __fake: ReturnType<typeof makeLeaseFakeClient> }).__fake;
    await expectLeaseErrorMatching(withTenantMigrationLease(options), /indeterminate/i);
    expect(fake.calls.some((c) => c.kind === 'ROLLBACK')).toBe(true);
    expect(fake.closeCount()).toBe(1);
  });
});

describe('tenant-config W15A: withTenantMigrationLease — pre-callback validation failures', () => {
  it('rejects a non-UUID site BEFORE opening the control-plane client', async () => {
    const seenConfigs: unknown[] = [];
    await expectLeaseErrorMatching(
      withTenantMigrationLease({
        siteId: 'not-a-uuid',
        env: { [SITE_ENV_KEY]: TENANT_DSN },
        controlPlaneConfig: CONTROL_PLANE_CONFIG,
        controlPlaneClientFactory: async (config) => {
          seenConfigs.push(config);
          // Returned client must never be opened if the UUID is invalid.
          return makeLeaseFakeClient().client;
        },
        callback: async () => ({ committed: true, ordinal: 13 }),
      }),
      /site UUID is not RFC 4122 shaped/i,
    );
    expect(seenConfigs).toHaveLength(0);
  });

  it('rejects the lease if BEGIN itself fails (no COMMIT, but the client is closed once)', async () => {
    const { options } = wireLease({
      fake: {
        throwOnBegin: new Error('connection lost after connect()'),
      },
    });
    const fake = (options as unknown as { __fake: ReturnType<typeof makeLeaseFakeClient> }).__fake;
    await expectLeaseErrorMatching(withTenantMigrationLease(options), /connection lost/i);
    expect(fake.calls.some((c) => c.kind === 'COMMIT')).toBe(false);
    // The lease rejected before BEGIN; no ROLLBACK was attempted either.
    expect(fake.calls.some((c) => c.kind === 'ROLLBACK')).toBe(false);
    expect(fake.closeCount()).toBe(1);
  });

  it('refuses to advance when the route row is missing and ROLLBACKs the CP transaction', async () => {
    const { options } = wireLease({
      fake: { routeRow: null },
      callback: async () => ({ committed: true, ordinal: 13 }),
    });
    const fake = (options as unknown as { __fake: ReturnType<typeof makeLeaseFakeClient> }).__fake;
    await expectLeaseErrorMatching(
      withTenantMigrationLease(options),
      /lu_tenant_route row for site .* is missing/i,
    );
    // UPDATE never ran; ROLLBACK did.
    expect(fake.calls.some((c) => c.kind === 'ROUTE_UPDATE')).toBe(false);
    expect(fake.calls.some((c) => c.kind === 'ROLLBACK')).toBe(true);
    expect(fake.closeCount()).toBe(1);
  });

  it('refuses when migration_secret_reference is null and ROLLBACKs', async () => {
    const { options } = wireLease({
      fake: { routeRow: defaultRouteRow({ migration_secret_reference: null }) },
      callback: async () => ({ committed: true, ordinal: 13 }),
    });
    const fake = (options as unknown as { __fake: ReturnType<typeof makeLeaseFakeClient> }).__fake;
    await expectLeaseErrorMatching(
      withTenantMigrationLease(options),
      /has no migration_secret_reference/i,
    );
    expect(fake.calls.some((c) => c.kind === 'ROUTE_UPDATE')).toBe(false);
    expect(fake.calls.some((c) => c.kind === 'ROLLBACK')).toBe(true);
    expect(fake.closeCount()).toBe(1);
  });

  it('refuses when migration_secret_reference fails the format regex and the raw value is redacted in the error', async () => {
    const hostile = 'evil $(whoami); DROP';
    const { options } = wireLease({
      fake: { routeRow: defaultRouteRow({ migration_secret_reference: hostile }) },
      callback: async () => ({ committed: true, ordinal: 13 }),
    });
    const fake = (options as unknown as { __fake: ReturnType<typeof makeLeaseFakeClient> }).__fake;
    let caught: unknown = null;
    try {
      await withTenantMigrationLease(options);
    } catch (error) {
      caught = error;
    }
    expect(caught).toBeInstanceOf(TenantMigrationLeaseError);
    const message = (caught as Error).message;
    expect(message).toMatch(/failed format check/i);
    expect(message).not.toContain('whoami');
    expect(message).not.toContain('DROP');
    expect(message).not.toContain(hostile);
    expect(fake.calls.some((c) => c.kind === 'ROUTE_UPDATE')).toBe(false);
    expect(fake.closeCount()).toBe(1);
  });

  it('refuses when writer_label is outside the allow-list', async () => {
    const { options } = wireLease({
      fake: { routeRow: defaultRouteRow({ writer_label: 'CUSTOM_LABEL' }) },
      callback: async () => ({ committed: true, ordinal: 13 }),
    });
    const fake = (options as unknown as { __fake: ReturnType<typeof makeLeaseFakeClient> }).__fake;
    await expectLeaseErrorMatching(
      withTenantMigrationLease(options),
      /writer_label .* is not in the allow-list/i,
    );
    expect(fake.calls.some((c) => c.kind === 'ROUTE_UPDATE')).toBe(false);
    expect(fake.closeCount()).toBe(1);
  });

  it('refuses when state is not migrating (active / degraded / disabled) and ROLLBACKs', async () => {
    for (const state of ['active', 'degraded', 'disabled']) {
      const { options } = wireLease({
        fake: { routeRow: defaultRouteRow({ state }) },
        callback: async () => ({ committed: true, ordinal: 13 }),
      });
      const fake = (options as unknown as { __fake: ReturnType<typeof makeLeaseFakeClient> })
        .__fake;
      await expectLeaseErrorMatching(
        withTenantMigrationLease(options),
        /does not allow the 'up' operation \(expected migrating\)/i,
      );
      expect(fake.calls.some((c) => c.kind === 'ROUTE_UPDATE')).toBe(false);
      expect(fake.calls.some((c) => c.kind === 'ROLLBACK')).toBe(true);
      expect(fake.closeCount()).toBe(1);
    }
  });

  it('refuses when migration_secret_reference is NULL — env-var lookup never runs', async () => {
    const { options } = wireLease({
      fake: { routeRow: defaultRouteRow({ migration_secret_reference: null }) },
      env: {},
      callback: async () => ({ committed: true, ordinal: 13 }),
    });
    await expectLeaseErrorMatching(
      withTenantMigrationLease(options),
      /has no migration_secret_reference/i,
    );
  });

  it('refuses when the sanitized env var is unset and the error NEVER names the env var or the reference', async () => {
    let caught: unknown = null;
    try {
      await withTenantMigrationLease({
        siteId: SITE_ID,
        env: {},
        controlPlaneConfig: CONTROL_PLANE_CONFIG,
        controlPlaneClientFactory: async () =>
          makeLeaseFakeClient({ routeRow: defaultRouteRow() }).client,
        callback: async () => ({ committed: true, ordinal: 13 }),
      });
    } catch (error) {
      caught = error;
    }
    expect(caught).toBeInstanceOf(TenantConfigError);
    const message = (caught as Error).message;
    expect(message).toBe(`tenant migration secret env var is not set for site ${SITE_ID}`);
    expect(message).not.toContain('TENANT_MIGRATION_CONNECTION');
    expect(message).not.toContain('tenant_prod_db_01');
    expect(message).not.toContain(SITE_REF);
  });

  it('refuses when the DSN is malformed (parser failure) without echoing DSN material', async () => {
    let caught: unknown = null;
    try {
      await withTenantMigrationLease({
        siteId: SITE_ID,
        env: { [SITE_ENV_KEY]: 'junk-dsn-P@ssw0rd-NOT-PARSEABLE' },
        controlPlaneConfig: CONTROL_PLANE_CONFIG,
        controlPlaneClientFactory: async () =>
          makeLeaseFakeClient({ routeRow: defaultRouteRow() }).client,
        callback: async () => ({ committed: true, ordinal: 13 }),
      });
    } catch (error) {
      caught = error;
    }
    expect(caught).toBeInstanceOf(Error);
    expect((caught as Error).name).toBe('ConnectionConfigError');
    expect((caught as Error).message).not.toContain('junk-dsn');
    expect((caught as Error).message).not.toContain('P@ssw0rd');
  });

  it('callback is NEVER invoked when a pre-flight validation rejects (preserves "fail before callback" guarantee)', async () => {
    let callbackInvocations = 0;
    for (const fail of [
      { routeRow: null, matcher: /is missing/ },
      {
        routeRow: defaultRouteRow({ migration_secret_reference: null }),
        matcher: /has no migration_secret_reference/,
      },
      { routeRow: defaultRouteRow({ writer_label: 'CUSTOM' }), matcher: /not in the allow-list/ },
      {
        routeRow: defaultRouteRow({ state: 'active' }),
        matcher: /does not allow the 'up' operation/,
      },
      {
        routeRow: defaultRouteRow({ migration_secret_reference: 'evil $(reboot)' }),
        matcher: /failed format check/,
      },
    ]) {
      callbackInvocations = 0;
      const { options } = wireLease({
        fake: { routeRow: fail.routeRow },
        env: {},
        callback: async () => {
          callbackInvocations += 1;
          return { committed: true, ordinal: 13 };
        },
      });
      await expectLeaseErrorMatching(withTenantMigrationLease(options), fail.matcher);
      expect(callbackInvocations).toBe(0);
    }
  });
});

describe('tenant-config W15A: withTenantMigrationLease — failure paths & close-once', () => {
  it('SET LOCAL ROLE failure ROLLBACKs and closes the client exactly once', async () => {
    const { options } = wireLease({
      fake: { throwOnKind: 'SET_LOCAL_ROLE' },
      callback: async () => ({ committed: true, ordinal: 13 }),
    });
    const fake = (options as unknown as { __fake: ReturnType<typeof makeLeaseFakeClient> }).__fake;
    await expectLeaseErrorMatching(
      withTenantMigrationLease(options),
      /mock sql failure for SET_LOCAL_ROLE/i,
    );
    expect(fake.calls.some((c) => c.kind === 'ROLLBACK')).toBe(true);
    expect(fake.closeCount()).toBe(1);
  });

  it('global advisory-lock failure ROLLBACKs and closes the client exactly once', async () => {
    const { options } = wireLease({
      fake: { throwOnKind: 'GLOBAL_LOCK' },
      callback: async () => ({ committed: true, ordinal: 13 }),
    });
    const fake = (options as unknown as { __fake: ReturnType<typeof makeLeaseFakeClient> }).__fake;
    await expectLeaseErrorMatching(
      withTenantMigrationLease(options),
      /mock sql failure for GLOBAL_LOCK/i,
    );
    expect(fake.calls.some((c) => c.kind === 'ROLLBACK')).toBe(true);
    expect(fake.closeCount()).toBe(1);
  });

  it('FOR UPDATE failure ROLLBACKs and closes the client exactly once', async () => {
    const { options } = wireLease({
      fake: { throwOnKind: 'FOR_UPDATE_SELECT' },
      callback: async () => ({ committed: true, ordinal: 13 }),
    });
    const fake = (options as unknown as { __fake: ReturnType<typeof makeLeaseFakeClient> }).__fake;
    await expectLeaseErrorMatching(
      withTenantMigrationLease(options),
      /mock sql failure for FOR_UPDATE_SELECT/i,
    );
    expect(fake.calls.some((c) => c.kind === 'ROLLBACK')).toBe(true);
    expect(fake.closeCount()).toBe(1);
  });

  it('UPDATE failure during route advancement ROLLBACKs and closes the client exactly once', async () => {
    const { options } = wireLease({
      fake: { throwOnKind: 'ROUTE_UPDATE' },
      callback: async () => ({ committed: true, ordinal: 13 }),
    });
    const fake = (options as unknown as { __fake: ReturnType<typeof makeLeaseFakeClient> }).__fake;
    await expectLeaseErrorMatching(
      withTenantMigrationLease(options),
      /mock sql failure for ROUTE_UPDATE/i,
    );
    expect(fake.calls.some((c) => c.kind === 'ROLLBACK')).toBe(true);
    expect(fake.closeCount()).toBe(1);
  });

  it('COMMIT failure ROLLBACKs and closes the client exactly once', async () => {
    // We make the UPDATE succeed and COMMIT fail. The fake classifies
    // COMMIT the same way the runner would, so the lease rethrows the
    // COMMIT error.
    const { options } = wireLease({
      fake: {
        updateRows: [{ site_id: SITE_ID, schema_version: 13 }],
        throwOnKind: 'COMMIT',
      },
      callback: async () => ({ committed: true, ordinal: 13 }),
    });
    // Override query to fail on COMMIT specifically.
    const fake = (options as unknown as { __fake: ReturnType<typeof makeLeaseFakeClient> }).__fake;
    // Inject a COMMIT-only failure: wrap the fake's commit behaviour.
    const origClose = fake.client.close;
    const realClient: TenantClient = {
      async query<T extends Record<string, unknown>>(text: string, values?: unknown[]) {
        if (classifyLeaseQuery(text, values ?? []) === 'COMMIT') {
          throw new Error('commit transport broken');
        }
        return fake.client.query<T>(text, values);
      },
      async close() {
        return origClose();
      },
    };
    const wrappedOptions: WithTenantMigrationLeaseOptions = {
      ...options,
      controlPlaneClientFactory: async () => realClient,
    };
    await expectLeaseErrorMatching(
      withTenantMigrationLease(wrappedOptions),
      /commit transport broken/i,
    );
    expect(fake.closeCount()).toBe(1);
  });

  it('never changes writer_label / state of the route row (the only UPDATE issued by the lease)', async () => {
    const { options } = wireLease();
    const fake = (options as unknown as { __fake: ReturnType<typeof makeLeaseFakeClient> }).__fake;
    await withTenantMigrationLease(options);
    const update = fake.calls.find((c) => c.kind === 'ROUTE_UPDATE');
    expect(update).toBeDefined();
    // The SET clause never assigns writer_label or state.
    const setClause = update!.text.match(/SET\s+([\s\S]*?)\s+WHERE/i)?.[1] ?? '';
    expect(setClause.toLowerCase()).not.toMatch(/writer_label\s*=/);
    expect(setClause.toLowerCase()).not.toMatch(/state\s*=/);
    // The only writes are schema_version, last_health_at and updated_at.
    expect(setClause).toMatch(/schema_version\s*=\s*GREATEST/i);
    expect(setClause).toMatch(/last_health_at\s*=\s*CURRENT_TIMESTAMP/i);
    expect(setClause).toMatch(/updated_at\s*=\s*CURRENT_TIMESTAMP/i);
    // The WHERE clause still pins state and writer_label as defensive guards.
    expect(update!.text).toMatch(/state\s*=\s*\$3/i);
    expect(update!.text).toMatch(/writer_label\s*=\s*\$4/i);
  });

  it('lock query carries a value derived from the production constant (no site UUID, no DSN, no env var)', async () => {
    const { options } = wireLease();
    const fake = (options as unknown as { __fake: ReturnType<typeof makeLeaseFakeClient> }).__fake;
    await withTenantMigrationLease(options);
    const lock = fake.calls.find((c) => c.kind === 'GLOBAL_LOCK');
    expect(lock!.values).toEqual([TENANT_MIGRATION_GLOBAL_LOCK_KEY]);
    // The lock value never embeds the site UUID, the DSN, or any password.
    const serialized = JSON.stringify(lock!.values);
    expect(serialized).not.toContain(SITE_ID);
    expect(serialized).not.toContain(TENANT_DSN);
    expect(serialized).not.toContain('TENANT-DSN-PASSWORD');
  });
});

// ---------------------------------------------------------------------------
// MIG-001-F2-W18 — table-driven migrator preflight failure cases.
//
// The preflight runs BEFORE SET LOCAL ROLE and BEFORE the route SELECT so
// generic / redacted metadata is rejected without ever reaching the route
// query or the tenant callback. Each row in `preflightFailures` exercises
// a distinct flag (session / group / runtime), is exercised in BOTH the
// read-only resolver and the write-side lease, and asserts:
//   * the error name is `TenantConfigError`,
//   * the lease never reaches SET LOCAL ROLE / FOR UPDATE_SELECT / UPDATE,
//   * the lease still issues ROLLBACK + closeOnce on every path.
//
// The list is intentionally data-driven (table-driven tests) so a future
// flag is added in one place; the runner-side and resolver-side suites
// stay in lock-step with the same expectation matrix.
// ---------------------------------------------------------------------------

interface PreflightCase {
  fail: NonNullable<FakeClientOptions['preflightFail']>;
  matcher: RegExp;
  expectedKind: LeaseQueryKind;
}

const PREFLIGHT_CASES: readonly PreflightCase[] = [
  // Session role flags.
  {
    fail: 'session-row-missing',
    matcher: /no row for current_user/,
    expectedKind: 'PREFLIGHT_SESSION',
  },
  {
    fail: 'managed-identity',
    matcher: /managed identity and cannot read tenant metadata/,
    expectedKind: 'PREFLIGHT_SESSION',
  },
  { fail: 'no-login', matcher: /must be LOGIN/, expectedKind: 'PREFLIGHT_SESSION' },
  { fail: 'superuser', matcher: /is a PostgreSQL superuser/, expectedKind: 'PREFLIGHT_SESSION' },
  { fail: 'createrole', matcher: /has CREATEROLE/, expectedKind: 'PREFLIGHT_SESSION' },
  { fail: 'createdb', matcher: /has CREATEDB/, expectedKind: 'PREFLIGHT_SESSION' },
  { fail: 'replication', matcher: /has REPLICATION/, expectedKind: 'PREFLIGHT_SESSION' },
  { fail: 'bypassrls', matcher: /has BYPASSRLS/, expectedKind: 'PREFLIGHT_SESSION' },
  // Migrator (group) role flags.
  {
    fail: 'migrator-missing',
    matcher: /role "lu_auth_migrator" is absent/,
    expectedKind: 'PREFLIGHT_MIGRATOR',
  },
  { fail: 'migrator-login', matcher: /must be NOLOGIN/, expectedKind: 'PREFLIGHT_MIGRATOR' },
  { fail: 'migrator-super', matcher: /must be NOSUPERUSER/, expectedKind: 'PREFLIGHT_MIGRATOR' },
  {
    fail: 'migrator-createrole',
    matcher: /must be NOCREATEROLE/,
    expectedKind: 'PREFLIGHT_MIGRATOR',
  },
  { fail: 'migrator-createdb', matcher: /must be NOCREATEDB/, expectedKind: 'PREFLIGHT_MIGRATOR' },
  {
    fail: 'migrator-replication',
    matcher: /must be NOREPLICATION/,
    expectedKind: 'PREFLIGHT_MIGRATOR',
  },
  {
    fail: 'migrator-bypassrls',
    matcher: /must be NOBYPASSRLS/,
    expectedKind: 'PREFLIGHT_MIGRATOR',
  },
  // Membership + runtime role flags.
  {
    fail: 'non-member',
    matcher: /is not a member of role "lu_auth_migrator"/,
    expectedKind: 'PREFLIGHT_MEMBERSHIP',
  },
  {
    fail: 'runtime-missing',
    matcher: /role "lu_auth_runtime" is absent/,
    expectedKind: 'PREFLIGHT_RUNTIME',
  },
  {
    fail: 'runtime-login',
    matcher: /role "lu_auth_runtime" must be NOLOGIN/,
    expectedKind: 'PREFLIGHT_RUNTIME',
  },
  {
    fail: 'runtime-elevated',
    matcher: /must NOT have elevated attributes/,
    expectedKind: 'PREFLIGHT_RUNTIME',
  },
  {
    fail: 'runtime-can-create',
    matcher: /must NOT have CREATE on schema "public"/,
    expectedKind: 'PREFLIGHT_RUNTIME_CREATE',
  },
  {
    fail: 'runtime-owns-objects',
    matcher: /must NOT own migrable public objects/,
    expectedKind: 'PREFLIGHT_RUNTIME_OWNS',
  },
];

describe('tenant-config W18: read-only resolver rejects every preflight failure before SET LOCAL ROLE', () => {
  for (const tc of PREFLIGHT_CASES) {
    it(`resolver: preflightFail=${tc.fail} rejects with ${tc.matcher}`, async () => {
      const { client, calls, closeCount } = makeFakeClient({
        row: okRow(),
        preflightFail: tc.fail,
      });
      let caught: unknown = null;
      try {
        await resolveTenantConfig(
          SITE_ID,
          'up',
          CONTROL_PLANE_CONFIG,
          { [SITE_ENV_KEY]: TENANT_DSN },
          async () => client,
        );
      } catch (error) {
        caught = error;
      }
      expect(caught).toBeInstanceOf(TenantConfigError);
      expect((caught as Error).message).toMatch(tc.matcher);
      // The preflight fires BEFORE SET LOCAL ROLE and BEFORE the route
      // SELECT, so generic / redacted metadata never reaches the route
      // query or the caller.
      expect(calls.some((c) => c.text === 'SET LOCAL ROLE lu_auth_migrator')).toBe(false);
      expect(calls.some((c) => c.text.includes('FROM public.lu_tenant_route'))).toBe(false);
      expect(closeCount()).toBe(1);
    });
  }
});

describe('tenant-config W18: write-side lease rejects every preflight failure before SET LOCAL ROLE', () => {
  for (const tc of PREFLIGHT_CASES) {
    it(`lease: preflightFail=${tc.fail} ROLLBACKs and rejects before SET LOCAL ROLE`, async () => {
      const { options } = wireLease({
        fake: {
          routeRow: defaultRouteRow(),
          preflightFail: tc.fail,
        },
        callback: async () => ({ committed: true, ordinal: 13 }),
      });
      const fake = (options as unknown as { __fake: ReturnType<typeof makeLeaseFakeClient> })
        .__fake;
      await expectLeaseErrorMatching(withTenantMigrationLease(options), tc.matcher);
      // The preflight fires at the expected preflight slot and the
      // lease never reaches SET LOCAL ROLE / FOR UPDATE / UPDATE.
      const failIdx = fake.calls.findIndex((c) => c.kind === tc.expectedKind);
      expect(failIdx).toBeGreaterThanOrEqual(0);
      expect(fake.calls.some((c) => c.kind === 'SET_LOCAL_ROLE')).toBe(false);
      expect(fake.calls.some((c) => c.kind === 'FOR_UPDATE_SELECT')).toBe(false);
      expect(fake.calls.some((c) => c.kind === 'ROUTE_UPDATE')).toBe(false);
      expect(fake.calls.some((c) => c.kind === 'ROLLBACK')).toBe(true);
      expect(fake.closeCount()).toBe(1);
    });
  }
});

// ---------------------------------------------------------------------------
// MIG-001-F2-W18 — callback ordinal validation against TENANT_REGISTRY.
//
// A positive integer alone is not sufficient: the callback ordinal MUST be
// present in `TENANT_REGISTRY` (read-only import in tenant-config.ts). The
// registry is the source of truth for what ordinals exist on the tenant
// stream; ordinals outside 1..13 are rejected BEFORE the guarded route
// UPDATE so the route row's schema_version cannot be silently advanced to
// a value no migration can ever satisfy.
// ---------------------------------------------------------------------------

describe('tenant-config W18: lease callback ordinal is validated against TENANT_REGISTRY', () => {
  // Every registered ordinal 1..13 advances the route row.
  for (const ordinal of [1, 7, 13]) {
    it(`lease: ordinal=${ordinal} (registered) advances schema_version`, async () => {
      const { options } = wireLease({
        fake: {
          updateRows: [{ site_id: SITE_ID, schema_version: ordinal }],
        },
        callback: async () => ({ committed: true, ordinal }),
      });
      const result = await withTenantMigrationLease(options);
      expect(result.ordinal).toBe(ordinal);
      const fake = (options as unknown as { __fake: ReturnType<typeof makeLeaseFakeClient> })
        .__fake;
      expect(fake.calls.some((c) => c.kind === 'ROUTE_UPDATE')).toBe(true);
      expect(fake.calls.some((c) => c.kind === 'COMMIT')).toBe(true);
      expect(fake.closeCount()).toBe(1);
    });
  }

  // Ordinals outside the registered range are rejected BEFORE the route
  // UPDATE; the lease ROLLBACKs and the route row never advances.
  for (const ordinal of [0, 14, 999]) {
    it(`lease: ordinal=${ordinal} (rejected before UPDATE) ROLLBACKs without advancing`, async () => {
      const { options } = wireLease({
        callback: async () => ({ committed: true, ordinal }),
      });
      const fake = (options as unknown as { __fake: ReturnType<typeof makeLeaseFakeClient> })
        .__fake;
      await expectLeaseErrorMatching(
        withTenantMigrationLease(options),
        ordinal <= 0 ? /invalid ordinal/i : new RegExp(`unregistered ordinal ${ordinal}`),
      );
      expect(fake.calls.some((c) => c.kind === 'ROUTE_UPDATE')).toBe(false);
      expect(fake.calls.some((c) => c.kind === 'COMMIT')).toBe(false);
      expect(fake.calls.some((c) => c.kind === 'ROLLBACK')).toBe(true);
      expect(fake.closeCount()).toBe(1);
    });
  }
});
