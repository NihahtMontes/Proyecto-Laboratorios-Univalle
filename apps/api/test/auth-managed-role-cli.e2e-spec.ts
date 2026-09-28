import { readFileSync } from 'node:fs';
import {
  loadManagedRoleConfig,
  roleFilePath,
  verifyRoleFile,
} from '../src/auth/auth-managed-role-cli.js';
import {
  assertCanonicalBytes,
  canonicalByteLength,
  sha256Hex,
} from '../src/database/migration-plan.js';

const ADMIN_DSN =
  'Host=managed.example.test;Port=5432;Database=neondb;Username=neondb_owner;Password=secret;SSL Mode=Prefer';

// Pinned LF digest for 003_provision_auth_runtime_managed.sql. Computed from
// the canonical LF bytes (CRLF → LF only; BOM and lone CR rejected).
const PINNED_ROLE_FILE_SHA256 = '7fd48d7130140af8e5468d432e9831ae8a2885d3cad62c6a6e046e43e368e85a';

const jestRuntime = (import.meta as ImportMeta & { jest: typeof jest }).jest;
const unstableMockModule = (
  jestRuntime as typeof jest & {
    unstable_mockModule(moduleName: string, factory: () => unknown): typeof jest;
  }
).unstable_mockModule.bind(jestRuntime);

interface PoolCall {
  readonly sql: string;
  readonly values?: readonly unknown[];
}

interface FakePool {
  queries: PoolCall[];
  responses: Map<string, readonly Record<string, unknown>[]>;
  /** `null` lets the call return default rows (configurable per pattern below). */
  throwOn?: RegExp;
  /** When true, `pool.query()` always rejects with `errorMessage`. */
  throwAlways?: { readonly pattern: RegExp; readonly error: Error };
}

function makeFakePool(): FakePool {
  return { queries: [], responses: new Map() };
}

function fakePoolRunner(fake: FakePool): (sql: string, values?: unknown[]) => Promise<unknown> {
  return async (sql: string, values?: unknown[]) => {
    fake.queries.push(values === undefined ? { sql } : { sql, values });
    if (fake.throwAlways !== undefined && fake.throwAlways.pattern.test(sql)) {
      throw fake.throwAlways.error;
    }
    if (fake.throwOn !== undefined && fake.throwOn.test(sql)) {
      throw new Error('fake pool: refused query');
    }
    for (const [pattern, rows] of fake.responses.entries()) {
      if (new RegExp(pattern).test(sql)) {
        return { rows: [...rows] };
      }
    }
    return { rows: [] };
  };
}

async function importManagedRoleCliWithPool(
  fake: FakePool,
): Promise<typeof import('../src/auth/auth-managed-role-cli.js')> {
  jestRuntime.resetModules();
  unstableMockModule('pg', () => ({
    Pool: class FakePool {
      readonly query = fakePoolRunner(fake);
      async connect(): Promise<unknown> {
        return {
          query: fakePoolRunner(fake),
          release(): void {
            /* no-op */
          },
        };
      }
      async end(): Promise<void> {
        /* no-op */
      }
    },
  }));
  return import('../src/auth/auth-managed-role-cli.js');
}

describe('managed Auth role CLI', () => {
  it('accepts only the explicitly approved database and admin identity', () => {
    const config = loadManagedRoleConfig(
      {
        ConnectionStrings__DefaultConnection: ADMIN_DSN,
        AUTH_MANAGED_TARGET_DATABASE: 'neondb',
        AUTH_MANAGED_ADMIN_USER: 'neondb_owner',
      },
      false,
    );

    expect(config.database.database).toBe('neondb');
    expect(config.database.user).toBe('neondb_owner');
    expect(config.runtimePassword).toBeNull();
  });

  it('rejects a target identity mismatch', () => {
    expect(() =>
      loadManagedRoleConfig(
        {
          ConnectionStrings__DefaultConnection: ADMIN_DSN,
          AUTH_MANAGED_TARGET_DATABASE: 'production',
          AUTH_MANAGED_ADMIN_USER: 'neondb_owner',
        },
        false,
      ),
    ).toThrow('does not match');
  });

  it.each(['short', 'x'.repeat(257), `x${'a'.repeat(31)}\n`])(
    'rejects an invalid runtime password without echoing it',
    (password) => {
      expect(() =>
        loadManagedRoleConfig(
          {
            ConnectionStrings__DefaultConnection: ADMIN_DSN,
            AUTH_MANAGED_TARGET_DATABASE: 'neondb',
            AUTH_MANAGED_ADMIN_USER: 'neondb_owner',
            AUTH_RUNTIME_PASSWORD: password,
          },
          true,
        ),
      ).toThrow(/AUTH_RUNTIME_PASSWORD/);
    },
  );

  it('pins the managed role SQL by canonical LF SHA-256', () => {
    const content = readFileSync(roleFilePath(process.cwd()));
    expect(verifyRoleFile(content)).toBe(PINNED_ROLE_FILE_SHA256);
    expect(() => verifyRoleFile(Buffer.concat([content, Buffer.from('\n')]))).toThrow(
      'hash mismatch',
    );
  });

  it('verifies LF, CRLF and CRLF→LF-converted bytes against the same canonical digest', () => {
    const content = readFileSync(roleFilePath(process.cwd()));

    // LF bytes
    const lfBytes = Buffer.from(content.toString('utf8').replace(/\r\n/g, '\n'), 'utf8');
    expect(sha256Hex(lfBytes)).toBe(PINNED_ROLE_FILE_SHA256);
    expect(verifyRoleFile(lfBytes)).toBe(PINNED_ROLE_FILE_SHA256);

    // CRLF bytes (the Windows checkout form): build from LF, never from a
    // string that already carries CRLF (avoids double-CR injection).
    const crlfBytes = Buffer.from(lfBytes.toString('utf8').replace(/\n/g, '\r\n'), 'utf8');
    expect(sha256Hex(crlfBytes)).toBe(PINNED_ROLE_FILE_SHA256);
    expect(verifyRoleFile(crlfBytes)).toBe(PINNED_ROLE_FILE_SHA256);

    // CRLF bytes converted to LF (canonical normalization roundtrip)
    const converted = Buffer.from(content.toString('utf8').replace(/\r\n/g, '\n'), 'utf8');
    expect(sha256Hex(converted)).toBe(PINNED_ROLE_FILE_SHA256);
    expect(canonicalByteLength(converted)).toBe(canonicalByteLength(lfBytes));
  });

  it('rejects a one-byte content change', () => {
    const content = readFileSync(roleFilePath(process.cwd()));
    const mutated = Buffer.concat([content, Buffer.from(' ', 'utf8')]);
    expect(sha256Hex(mutated)).not.toBe(PINNED_ROLE_FILE_SHA256);
    expect(() => verifyRoleFile(mutated)).toThrow('hash mismatch');
  });

  it('rejects bytes containing a lone CR (no following LF)', () => {
    const content = readFileSync(roleFilePath(process.cwd()));
    const loneCr = Buffer.concat([content, Buffer.from([0x0d, 0x0a]), Buffer.from([0x0d])]);
    expect(() => assertCanonicalBytes(loneCr, roleFilePath(process.cwd()))).toThrow(/lone CR/);
    expect(() => verifyRoleFile(loneCr)).toThrow(/lone CR/);
  });

  it('rejects bytes starting with a UTF-8 BOM', () => {
    const content = readFileSync(roleFilePath(process.cwd()));
    const bomBytes = Buffer.concat([Buffer.from([0xef, 0xbb, 0xbf]), content]);
    expect(() => assertCanonicalBytes(bomBytes, roleFilePath(process.cwd()))).toThrow(/BOM/);
    expect(() => verifyRoleFile(bomBytes)).toThrow(/BOM/);
  });
});

describe('managed Auth role CLI: cutover preflight for provision', () => {
  const ENV_BASE: Record<string, string | undefined> = {
    ConnectionStrings__DefaultConnection: ADMIN_DSN,
    AUTH_MANAGED_TARGET_DATABASE: 'neondb',
    AUTH_MANAGED_ADMIN_USER: 'neondb_owner',
  };

  beforeEach(() => {
    jestRuntime.resetModules();
  });

  afterEach(() => {
    jestRuntime.restoreAllMocks();
    jestRuntime.resetModules();
  });

  it('provision refuses (no roles/003 SQL executed) when the control-plane ledger already records ordinal >= 4', async () => {
    const fake = makeFakePool();
    fake.responses.set(
      "FROM information_schema\\.tables\\s+WHERE table_schema = \\$1 AND table_name = 'lu_migration_history'",
      [{ exists: true }],
    );
    fake.responses.set('FROM public\\.lu_migration_history\\s+WHERE stream = \\$1', [
      { max_ordinal: 6 },
    ]);
    const cli = await importManagedRoleCliWithPool(fake);

    await expect(cli.runManagedRoleCli('provision', ENV_BASE)).rejects.toThrow(/ordinal 6/);
    // The cutover preflight must short-circuit BEFORE roles/003 is sent to
    // the database: no REVOKE / GRANT / CREATE ROLE / ALTER ROLE from the
    // roles file may be observed by the fake pool.
    const provisionedSql = fake.queries
      .map((q) => q.sql)
      .find((sql) =>
        /REVOKE ALL|GRANT SELECT|GUARD_RUNTIME|CREATE ROLE|ALTER ROLE lu_auth/i.test(sql),
      );
    expect(provisionedSql).toBeUndefined();
    // Exactly two ledger reads are expected: information_schema probe +
    // MAX(ordinal) query. No other queries.
    expect(fake.queries).toHaveLength(2);
    expect(fake.queries[0]!.sql).toMatch(/information_schema\.tables/);
    expect(fake.queries[1]!.sql).toMatch(/lu_migration_history/);
  });

  it('provision allows when the control-plane ledger has only ordinal 3', async () => {
    const fake = makeFakePool();
    fake.responses.set(
      "FROM information_schema\\.tables\\s+WHERE table_schema = \\$1 AND table_name = 'lu_migration_history'",
      [{ exists: true }],
    );
    fake.responses.set('FROM public\\.lu_migration_history\\s+WHERE stream = \\$1', [
      { max_ordinal: 3 },
    ]);
    // The verifyRoles SELECT after the provision returns two rows.
    fake.responses.set('FROM pg_catalog\\.pg_roles\\s+WHERE rolname IN', [
      {
        rolname: 'lu_auth_login',
        rolcanlogin: false,
        rolinherit: false,
        rolsuper: false,
        rolcreaterole: false,
        rolcreatedb: false,
        rolreplication: false,
        rolbypassrls: false,
      },
      {
        rolname: 'lu_auth_runtime',
        rolcanlogin: false,
        rolinherit: false,
        rolsuper: false,
        rolcreaterole: false,
        rolcreatedb: false,
        rolreplication: false,
        rolbypassrls: false,
      },
    ]);
    const cli = await importManagedRoleCliWithPool(fake);

    await expect(cli.runManagedRoleCli('provision', ENV_BASE)).resolves.toBeUndefined();
    // The provision must have hit the database with the roles/003 SQL
    // body AFTER the preflight cleared the ledger check.
    const sawProvisionSql = fake.queries.some((q) => /REVOKE ALL PRIVILEGES/i.test(q.sql));
    expect(sawProvisionSql).toBe(true);
  });

  it('provision refuses (no roles/003 SQL executed) when the ledger probe throws', async () => {
    const fake = makeFakePool();
    fake.throwAlways = {
      pattern: /FROM information_schema\.tables/,
      error: new Error('permission denied for information_schema'),
    };
    const cli = await importManagedRoleCliWithPool(fake);

    await expect(cli.runManagedRoleCli('provision', ENV_BASE)).rejects.toThrow(
      /could not be probed/,
    );
    const provisionedSql = fake.queries
      .map((q) => q.sql)
      .find((sql) => /REVOKE ALL|GRANT SELECT|CREATE ROLE|ALTER ROLE lu_auth/i.test(sql));
    expect(provisionedSql).toBeUndefined();
  });

  it('provision refuses when the MAX(ordinal) read throws', async () => {
    const fake = makeFakePool();
    fake.responses.set(
      "FROM information_schema\\.tables\\s+WHERE table_schema = \\$1 AND table_name = 'lu_migration_history'",
      [{ exists: true }],
    );
    fake.throwAlways = {
      pattern: /FROM public\.lu_migration_history/,
      error: new Error('relation "public.lu_migration_history" does not exist'),
    };
    const cli = await importManagedRoleCliWithPool(fake);

    await expect(cli.runManagedRoleCli('provision', ENV_BASE)).rejects.toThrow(
      /was found but could not be read/,
    );
    const provisionedSql = fake.queries
      .map((q) => q.sql)
      .find((sql) => /REVOKE ALL|GRANT SELECT|CREATE ROLE|ALTER ROLE lu_auth/i.test(sql));
    expect(provisionedSql).toBeUndefined();
  });

  it('provision allows when neither the ledger nor any control-plane 0004 object exists', async () => {
    const fake = makeFakePool();
    fake.responses.set(
      "FROM information_schema\\.tables\\s+WHERE table_schema = \\$1 AND table_name = 'lu_migration_history'",
      [{ exists: false }],
    );
    fake.responses.set("to_regclass\\('public\\.lu_faculty'\\)", [{ exists: false }]);
    fake.responses.set('FROM pg_catalog\\.pg_roles\\s+WHERE rolname IN', [
      {
        rolname: 'lu_auth_login',
        rolcanlogin: false,
        rolinherit: false,
        rolsuper: false,
        rolcreaterole: false,
        rolcreatedb: false,
        rolreplication: false,
        rolbypassrls: false,
      },
      {
        rolname: 'lu_auth_runtime',
        rolcanlogin: false,
        rolinherit: false,
        rolsuper: false,
        rolcreaterole: false,
        rolcreatedb: false,
        rolreplication: false,
        rolbypassrls: false,
      },
    ]);
    const cli = await importManagedRoleCliWithPool(fake);

    await expect(cli.runManagedRoleCli('provision', ENV_BASE)).resolves.toBeUndefined();
    const ledgerRead = fake.queries.find((q) => /MAX\(ordinal\)/.test(q.sql));
    expect(ledgerRead).toBeUndefined();
    const sawProvisionSql = fake.queries.some((q) => /REVOKE ALL PRIVILEGES/i.test(q.sql));
    expect(sawProvisionSql).toBe(true);
  });

  it('verify / deactivate are unaffected by the cutover preflight (no ledger read)', async () => {
    const fake = makeFakePool();
    fake.responses.set('FROM pg_catalog\\.pg_roles\\s+WHERE rolname IN', [
      {
        rolname: 'lu_auth_login',
        rolcanlogin: false,
        rolinherit: false,
        rolsuper: false,
        rolcreaterole: false,
        rolcreatedb: false,
        rolreplication: false,
        rolbypassrls: false,
      },
      {
        rolname: 'lu_auth_runtime',
        rolcanlogin: false,
        rolinherit: false,
        rolsuper: false,
        rolcreaterole: false,
        rolcreatedb: false,
        rolreplication: false,
        rolbypassrls: false,
      },
    ]);
    const cli = await importManagedRoleCliWithPool(fake);
    await expect(cli.runManagedRoleCli('verify', ENV_BASE)).resolves.toBeUndefined();
    fake.queries.length = 0;
    await expect(cli.runManagedRoleCli('deactivate', ENV_BASE)).resolves.toBeUndefined();
    const sawLedgerRead = fake.queries.some((q) =>
      /information_schema\.tables|lu_migration_history/.test(q.sql),
    );
    expect(sawLedgerRead).toBe(false);
  });
});
