/**
 * MIG-F3-PG-TEST-003 — CLI offline / fail-closed specs (no DB, no sockets, no real connection).
 *
 * These tests prove that:
 *  - `plan` works entirely offline against the pinned registry entry,
 *  - `status`, `up` and `verify` fail closed when the required environment variable is missing,
 *  - no command in this suite opens a socket or mocks a successful database connection.
 */

import { existsSync, mkdirSync, readFileSync, readdirSync, rmSync } from 'node:fs';
import path from 'node:path';
import { parseCommandArgs, run, writeManifestArtifact } from '../src/database/migration-cli.js';
import { IDENTITY_MIGRATION_FILE } from '../src/database/migration-plan.js';

const jestRuntime = (import.meta as ImportMeta & { jest: typeof jest }).jest;
const unstableMockModule = (
  jestRuntime as typeof jest & {
    unstable_mockModule(moduleName: string, factory: () => unknown): typeof jest;
  }
).unstable_mockModule.bind(jestRuntime);

describe('migration-cli: plan command (offline, no DB)', () => {
  const logs: string[] = [];
  const errors: string[] = [];

  beforeEach(() => {
    logs.length = 0;
    errors.length = 0;
    jestRuntime.spyOn(console, 'log').mockImplementation((...args: unknown[]) => {
      logs.push(args.map(String).join(' '));
    });
    jestRuntime.spyOn(console, 'error').mockImplementation((...args: unknown[]) => {
      errors.push(args.map(String).join(' '));
    });
  });

  afterEach(() => {
    jestRuntime.restoreAllMocks();
  });

  it('plan without args validates the registered 0001 migration offline', async () => {
    const exitCode = await run(['plan']);

    expect(exitCode).toBe(0);
    expect(errors).toHaveLength(0);
    expect(logs.some((line) => line.includes('0001_create_identity_control_plane.sql'))).toBe(true);
    expect(
      logs.some((line) =>
        line.includes('a7698ea53a443d42e87bf905ae9e0f7060923a26199bc06ad23f17401e1bc118'),
      ),
    ).toBe(true);
    expect(logs.some((line) => line.includes('4491'))).toBe(true);
    expect(logs.some((line) => /safety:\s*PASS/.test(line))).toBe(true);
  });

  it('plan with an explicit registered file name validates offline', async () => {
    const exitCode = await run(['plan', '0001_create_identity_control_plane.sql']);

    expect(exitCode).toBe(0);
    expect(errors).toHaveLength(0);
    expect(logs.some((line) => line.includes('0001_create_identity_control_plane.sql'))).toBe(true);
  });

  it('plan rejects an unregistered file name offline without connecting', async () => {
    await expect(run(['plan', '0002_evil.sql'])).rejects.toThrow(
      /is not registered in the migration registry/,
    );
  });

  it('plan rejects an invalid file name shape offline', async () => {
    await expect(run(['plan', '../etc/passwd'])).rejects.toThrow(/Invalid migration file name/);
  });
});

describe('migration-cli: fail-closed when the connection env variable is missing', () => {
  const ORIGINAL_VALUE = process.env['ConnectionStrings__DefaultConnection'];

  beforeEach(() => {
    delete process.env['ConnectionStrings__DefaultConnection'];
  });

  afterEach(() => {
    if (ORIGINAL_VALUE !== undefined) {
      process.env['ConnectionStrings__DefaultConnection'] = ORIGINAL_VALUE;
    } else {
      delete process.env['ConnectionStrings__DefaultConnection'];
    }
  });

  it('status throws before opening any socket', async () => {
    await expect(run(['status'])).rejects.toThrow(/Missing required environment variable/);
  });

  it('up throws before reading or executing the migration', async () => {
    await expect(run(['up'])).rejects.toThrow(/Missing required environment variable/);
  });

  it('verify throws before opening any socket', async () => {
    await expect(run(['verify'])).rejects.toThrow(/Missing required environment variable/);
  });

  it('unknown command prints usage and exits 2 without connecting', async () => {
    const errors: string[] = [];
    jestRuntime.spyOn(console, 'error').mockImplementation((...args: unknown[]) => {
      errors.push(args.map(String).join(' '));
    });
    try {
      const exitCode = await run(['drop-database']);
      expect(exitCode).toBe(2);
      expect(errors.some((line) => /Usage/.test(line))).toBe(true);
    } finally {
      jestRuntime.restoreAllMocks();
    }
  });
});

describe('migration-cli: imported entry point does not auto-run in tests', () => {
  it('exposes the run function without executing main()', () => {
    expect(typeof run).toBe('function');
  });
});

describe('migration-cli: argument parsing', () => {
  it('parseCommandArgs extracts file and manifest output for up', () => {
    expect(parseCommandArgs('up', ['--manifest-out', 'out.json'])).toEqual({
      fileArg: undefined,
      manifestOut: 'out.json',
    });
    expect(parseCommandArgs('up', ['0001.sql', '--manifest-out', 'out.json'])).toEqual({
      fileArg: '0001.sql',
      manifestOut: 'out.json',
    });
    expect(parseCommandArgs('up', ['--manifest-out=out.json', '0001.sql'])).toEqual({
      fileArg: '0001.sql',
      manifestOut: 'out.json',
    });
  });

  it('parseCommandArgs extracts file and manifest output for verify', () => {
    expect(parseCommandArgs('verify', ['--manifest-out', 'out.json'])).toEqual({
      fileArg: undefined,
      manifestOut: 'out.json',
    });
    expect(parseCommandArgs('verify', ['0001.sql', '--manifest-out', 'out.json'])).toEqual({
      fileArg: '0001.sql',
      manifestOut: 'out.json',
    });
    expect(parseCommandArgs('verify', ['--manifest-out=out.json', '0001.sql'])).toEqual({
      fileArg: '0001.sql',
      manifestOut: 'out.json',
    });
  });

  it('parseCommandArgs rejects missing value and extra file arguments', () => {
    expect(() => parseCommandArgs('verify', ['--manifest-out'])).toThrow(
      '--manifest-out requires a file path.',
    );
    expect(() => parseCommandArgs('verify', ['a.sql', 'b.sql'])).toThrow(
      'verify takes at most one migration file name.',
    );
    expect(() => parseCommandArgs('up', ['a.sql', 'b.sql'])).toThrow(
      'up takes at most one migration file name.',
    );
  });
});

describe('migration-cli: atomic manifest writer', () => {
  const tmpDir = `tmp-manifest-test-${process.pid}`;

  beforeAll(() => {
    mkdirSync(tmpDir, { recursive: true });
  });

  afterAll(() => {
    rmSync(tmpDir, { recursive: true, force: true });
  });

  it('writeManifestArtifact writes valid JSON atomically and leaves no tmp file', () => {
    const artifactPath = path.join(process.cwd(), tmpDir, 'artifact.json');
    const artifact = {
      workId: 'MIG-F3-PG-IDENTITY-001',
      command: 'verify' as const,
      file: IDENTITY_MIGRATION_FILE,
      stream: 'control-plane' as const,
      relativePath: '0001_create_identity_control_plane.sql',
      policy: 'legacy-strict' as const,
      sha256: 'a'.repeat(64),
      bytes: 4491,
      style: 'bootstrap' as const,
      startedAt: new Date().toISOString(),
      finishedAt: new Date().toISOString(),
      server: { database: 'testdb', user: 'tester', version: '16.0' },
      target: 'localhost:5432/testdb [ssl=disable (cleartext)]',
      readOnly: true,
      executed: false,
      committed: false,
      skipped: false,
      verification: { passed: true, diffs: [] },
      inspection: { tables: [{ table: 'lu_site', exists: true, columnCount: 6 }] },
    };

    writeManifestArtifact(artifactPath, artifact);

    expect(existsSync(artifactPath)).toBe(true);
    const parsed = JSON.parse(readFileSync(artifactPath, 'utf8'));
    expect(parsed.workId).toBe(artifact.workId);
    expect(parsed.command).toBe('verify');
    expect(parsed.readOnly).toBe(true);
    expect(parsed.executed).toBe(false);
    expect(parsed.committed).toBe(false);
    expect(parsed.target).not.toContain('secret');
    const dirEntries = readdirSync(tmpDir);
    expect(dirEntries.filter((name) => name.startsWith('artifact.json.tmp-'))).toHaveLength(0);
    expect(dirEntries).toContain('artifact.json');
  });
});

describe('migration-cli: verify command with mocked runner (no DB)', () => {
  const logs: string[] = [];
  const errors: string[] = [];
  const tmpDir = `tmp-verify-manifest-test-${process.pid}`;

  type VerifyOutcome = {
    verification: { passed: boolean; diffs: string[]; actualTableNames: string[] };
    inspection: {
      server: { database: string; user: string; version: string };
      tables: { table: string; exists: boolean; columns: string[] }[];
    };
  };

  function defaultVerifyOutcome(overrides?: Partial<VerifyOutcome>): VerifyOutcome {
    return {
      verification: { passed: true, diffs: [], actualTableNames: [] },
      inspection: {
        server: { database: 'testdb', user: 'tester', version: '16.0' },
        tables: [
          { table: 'lu_site', exists: true, columns: ['id', 'code', 'name'] },
          { table: 'lu_user', exists: true, columns: ['id', 'email'] },
          { table: 'lu_site_membership', exists: false, columns: [] },
          { table: 'lu_session', exists: false, columns: [] },
        ],
      },
      ...overrides,
    };
  }

  async function runWithVerifyOutcome(
    outcome: VerifyOutcome | (() => Promise<VerifyOutcome>),
  ): Promise<typeof import('../src/database/migration-cli.js')> {
    jestRuntime.resetModules();
    unstableMockModule('pg', () => ({
      Client: class {
        async connect(): Promise<void> {}
        async query(): Promise<{ rows: Record<string, unknown>[] }> {
          return { rows: [] };
        }
        async end(): Promise<void> {}
      },
    }));
    unstableMockModule('../src/database/migration-runner.js', () => ({
      fetchStatus: async () => ({
        server: { database: 'testdb', user: 'tester', version: '16.0' },
        tables: [],
      }),
      applyMigration: async () => ({
        preexisting: [],
        executed: true,
        committed: true,
        verification: { passed: true, diffs: [], actualTableNames: [] },
        inspection: {
          server: { database: 'testdb', user: 'tester', version: '16.0' },
          tables: [],
        },
      }),
      verifyMigration: jestRuntime.fn(async () =>
        typeof outcome === 'function' ? await outcome() : outcome,
      ),
    }));
    return import('../src/database/migration-cli.js');
  }

  beforeAll(() => {
    mkdirSync(tmpDir, { recursive: true });
  });

  beforeEach(() => {
    logs.length = 0;
    errors.length = 0;
    jestRuntime.spyOn(console, 'log').mockImplementation((...args: unknown[]) => {
      logs.push(args.map(String).join(' '));
    });
    jestRuntime.spyOn(console, 'error').mockImplementation((...args: unknown[]) => {
      errors.push(args.map(String).join(' '));
    });
  });

  afterEach(() => {
    jestRuntime.restoreAllMocks();
    for (const entry of readdirSync(tmpDir)) {
      rmSync(path.join(tmpDir, entry), { force: true });
    }
  });

  afterAll(() => {
    jestRuntime.resetModules();
    rmSync(tmpDir, { recursive: true, force: true });
  });

  it('verify PASS writes a secret-free manifest artifact and exits 0', async () => {
    process.env['ConnectionStrings__DefaultConnection'] =
      'Host=localhost;Port=5432;Database=testdb;Username=tester;Password=super-secret;SSL Mode=Disable';
    const artifactPath = path.join(tmpDir, 'verify-pass.json');
    const { run: mockedRun } = await runWithVerifyOutcome(defaultVerifyOutcome());

    const exitCode = await mockedRun(['verify', '--manifest-out', artifactPath]);

    expect(exitCode).toBe(0);
    expect(logs.some((line) => line.includes('verify: manifest artifact written'))).toBe(true);
    expect(errors).toHaveLength(0);
    expect(existsSync(artifactPath)).toBe(true);
    const artifact = JSON.parse(readFileSync(artifactPath, 'utf8'));
    expect(artifact.command).toBe('verify');
    expect(artifact.readOnly).toBe(true);
    expect(artifact.executed).toBe(false);
    expect(artifact.committed).toBe(false);
    expect(artifact.file).toBe(IDENTITY_MIGRATION_FILE);
    expect(artifact.sha256).toBe(
      'a7698ea53a443d42e87bf905ae9e0f7060923a26199bc06ad23f17401e1bc118',
    );
    expect(artifact.server).toEqual({ database: 'testdb', user: 'tester', version: '16.0' });
    expect(artifact.target).toBe('localhost:5432/testdb [ssl=disable (cleartext)]');
    expect(JSON.stringify(artifact)).not.toContain('super-secret');
    expect(artifact.inspection.tables).toHaveLength(4);
    expect(artifact.inspection.tables[0]).toEqual({
      table: 'lu_site',
      exists: true,
      columnCount: 3,
    });
  });

  it('verify mismatch still writes the manifest artifact and exits 1', async () => {
    process.env['ConnectionStrings__DefaultConnection'] =
      'Host=localhost;Port=5432;Database=testdb;Username=tester;Password=super-secret;SSL Mode=Disable';
    const artifactPath = path.join(tmpDir, 'verify-fail.json');
    const { run: mockedRun } = await runWithVerifyOutcome(
      defaultVerifyOutcome({
        verification: { passed: false, diffs: ['missing table lu_user'], actualTableNames: [] },
        inspection: {
          server: { database: 'testdb', user: 'tester', version: '16.0' },
          tables: [{ table: 'lu_site', exists: true, columns: ['id'] }],
        },
      }),
    );

    const exitCode = await mockedRun(['verify', '--manifest-out', artifactPath]);

    expect(exitCode).toBe(1);
    expect(errors.some((line) => line.includes('schema manifest mismatch'))).toBe(true);
    expect(existsSync(artifactPath)).toBe(true);
    const artifact = JSON.parse(readFileSync(artifactPath, 'utf8'));
    expect(artifact.command).toBe('verify');
    expect(artifact.readOnly).toBe(true);
    expect(artifact.executed).toBe(false);
    expect(artifact.committed).toBe(false);
    expect(artifact.verification.passed).toBe(false);
    expect(artifact.verification.diffs).toEqual(['missing table lu_user']);
  });

  it('verify does not write an artifact when the runner throws', async () => {
    process.env['ConnectionStrings__DefaultConnection'] =
      'Host=localhost;Port=5432;Database=testdb;Username=tester;Password=super-secret;SSL Mode=Disable';
    const artifactPath = path.join(tmpDir, 'verify-conn-fail.json');
    const { run: mockedRun } = await runWithVerifyOutcome(async () => {
      throw new Error('connection refused');
    });

    await expect(mockedRun(['verify', '--manifest-out', artifactPath])).rejects.toThrow(
      /connection refused/,
    );

    expect(existsSync(artifactPath)).toBe(false);
  });
});

// ---------------------------------------------------------------------------
// MIG-001-F2-W11 — tenant-stream CLI contract. The resolver and the runner
// are BOTH mocked: the only way a tenant command reaches a database is via
// the config returned by `resolveTenantConfig`, and the CLI must forward
// exactly that object (never the parsed control-plane DSN) into
// fetchStatus / applyMigration / verifyMigration. `plan` must stay fully
// offline even when `ConnectionStrings__DefaultConnection` is absent.
// ---------------------------------------------------------------------------
describe('migration-cli W11: tenant commands receive the RESOLVED tenant config (mocked resolver + runner, no DB)', () => {
  const SITE_ID = '3f2b7c1d-9a4e-4b6f-8c2d-1e5a7b9c0d1e';
  const CP_DSN =
    'Host=cp.internal;Port=5432;Database=cpdb;Username=cp_user;Password=CP-SECRET-PASSWORD';
  const TENANT_RESOLVED_CONFIG = {
    host: 'tenant.internal',
    port: 5433,
    database: 'tenant_db_01',
    user: 'mig_user',
    password: 'TENANT-DSN-PASSWORD',
    ssl: false as const,
  };

  interface ResolveCall {
    siteId: string;
    operation: string;
    cpConfig: unknown;
    env: unknown;
    factory: unknown;
  }
  interface RunnerCall {
    config: unknown;
    context: unknown;
  }

  let resolveCalls: ResolveCall[] = [];
  let fetchStatusCalls: RunnerCall[] = [];
  let applyCalls: RunnerCall[] = [];
  let verifyCalls: RunnerCall[] = [];
  let leaseCalls: Array<{
    siteId: string;
    env: unknown;
    cpConfig: unknown;
    factory: unknown;
    callbackInput?: unknown;
    callbackResult?: unknown;
  }> = [];
  let pgClientCtorCount = 0;
  let resolverError: Error | null = null;
  let leaseCallbackOverride: ((input: unknown) => Promise<unknown>) | null = null;

  const logs: string[] = [];
  const errors: string[] = [];
  const tmpDir = `tmp-tenant-cli-${process.pid}`;

  async function importCliMocked(): Promise<
    (typeof import('../src/database/migration-cli.js'))['run']
  > {
    jestRuntime.resetModules();
    unstableMockModule('pg', () => ({
      Client: class {
        constructor() {
          pgClientCtorCount += 1;
        }
        async connect(): Promise<void> {}
        async query(): Promise<{ rows: unknown[] }> {
          return { rows: [] };
        }
        async end(): Promise<void> {}
      },
    }));
    unstableMockModule('../src/database/tenant-config.js', () => ({
      resolveTenantConfig: async (
        siteId: string,
        operation: string,
        cpConfig: unknown,
        env: unknown,
        factory: unknown,
      ) => {
        resolveCalls.push({ siteId, operation, cpConfig, env, factory });
        if (resolverError !== null) {
          throw resolverError;
        }
        return {
          config: TENANT_RESOLVED_CONFIG,
          safeTarget: { host: 'tenant.internal', port: 5433, database: 'tenant_db_01' },
        };
      },
      withTenantMigrationLease: async (opts: {
        siteId: string;
        env: unknown;
        controlPlaneConfig: unknown;
        controlPlaneClientFactory: unknown;
        callback: (input: unknown) => Promise<unknown>;
      }) => {
        const callbackInput = {
          config: TENANT_RESOLVED_CONFIG,
          safeTarget: { host: 'tenant.internal', port: 5433, database: 'tenant_db_01' },
          state: 'migrating' as const,
          writerLabel: 'NEST_POSTGRES' as const,
          migrationSecretReference: 'tenant/prod.db-01',
        };
        leaseCalls.push({
          siteId: opts.siteId,
          env: opts.env,
          cpConfig: opts.controlPlaneConfig,
          factory: opts.controlPlaneClientFactory,
          callbackInput,
        });
        if (resolverError !== null) {
          throw resolverError;
        }
        const invoked = leaseCallbackOverride ?? opts.callback;
        const cbResult = await invoked(callbackInput);
        leaseCalls[leaseCalls.length - 1]!.callbackResult = cbResult;
        // If the callback threw, propagate; otherwise return a positive
        // ordinal so the CLI believes the route row advanced.
        const cb = cbResult as { committed?: boolean; ordinal?: number } | null | undefined;
        if (!cb || cb.committed !== true || typeof cb.ordinal !== 'number') {
          throw new Error('lease mock: callback did not return a positive committed ordinal');
        }
        return {
          ordinal: cb.ordinal,
          state: 'migrating' as const,
          writerLabel: 'NEST_POSTGRES' as const,
        };
      },
      TenantConfigError: class TenantConfigError extends Error {
        constructor(message: string) {
          super(message);
          this.name = 'TenantConfigError';
        }
      },
      TenantMigrationLeaseError: class TenantMigrationLeaseError extends Error {
        constructor(message: string) {
          super(message);
          this.name = 'TenantMigrationLeaseError';
        }
      },
      sanitizeReference: (ref: string) => ref.replace(/[^A-Za-z0-9]+/g, '_'),
    }));
    unstableMockModule('../src/database/migration-runner.js', () => ({
      fetchStatus: async (config: unknown, context: unknown) => {
        fetchStatusCalls.push({ config, context });
        return {
          server: { database: 'tenant_db_01', user: 'mig_user', version: '16.0' },
          tables: [{ table: 'lu_migration_history', exists: true, columns: ['stream', 'ordinal'] }],
        };
      },
      applyMigration: async (
        config: unknown,
        _filePath: string,
        _content: Buffer,
        context: unknown,
      ) => {
        applyCalls.push({ config, context });
        return {
          preexisting: [],
          executed: true,
          committed: true,
          verification: { passed: true, diffs: [] },
          inspection: {
            server: { database: 'tenant_db_01', user: 'mig_user', version: '16.0' },
            tables: [],
          },
          historyKey: { stream: 'tenant', relativePath: 'tenant/0013_migration_history.sql' },
        };
      },
      verifyMigration: async (
        config: unknown,
        _filePath: string,
        _content: Buffer,
        context: unknown,
      ) => {
        verifyCalls.push({ config, context });
        return {
          verification: { passed: true, diffs: [] },
          inspection: {
            server: { database: 'tenant_db_01', user: 'mig_user', version: '16.0' },
            tables: [],
          },
        };
      },
      resolveTenantConfig: () => {
        throw new Error('the CLI must not reach the runner for tenant resolution');
      },
    }));
    const cli = await import('../src/database/migration-cli.js');
    return cli.run;
  }

  beforeAll(() => {
    mkdirSync(tmpDir, { recursive: true });
  });

  beforeEach(() => {
    resolveCalls = [];
    fetchStatusCalls = [];
    applyCalls = [];
    verifyCalls = [];
    leaseCalls = [];
    pgClientCtorCount = 0;
    resolverError = null;
    leaseCallbackOverride = null;
    logs.length = 0;
    errors.length = 0;
    process.env['ConnectionStrings__DefaultConnection'] = CP_DSN;
    jestRuntime.spyOn(console, 'log').mockImplementation((...args: unknown[]) => {
      logs.push(args.map(String).join(' '));
    });
    jestRuntime.spyOn(console, 'error').mockImplementation((...args: unknown[]) => {
      errors.push(args.map(String).join(' '));
    });
  });

  afterEach(() => {
    jestRuntime.restoreAllMocks();
    delete process.env['ConnectionStrings__DefaultConnection'];
    for (const entry of readdirSync(tmpDir)) {
      rmSync(path.join(tmpDir, entry), { force: true });
    }
  });

  afterAll(() => {
    jestRuntime.resetModules();
    rmSync(tmpDir, { recursive: true, force: true });
  });

  it('status --stream tenant --site <uuid> fetches status from the RESOLVED config, never the control-plane one', async () => {
    const runMocked = await importCliMocked();
    const exit = await runMocked(['status', '--stream', 'tenant', '--site', SITE_ID.toUpperCase()]);

    expect(exit).toBe(0);
    expect(resolveCalls).toHaveLength(1);
    const call = resolveCalls[0]!;
    expect(call.siteId).toBe(SITE_ID); // parser lowercases the UUID
    expect(call.operation).toBe('status');
    // The CP DSN is only the control-plane LOOKUP connection.
    expect(call.cpConfig).toEqual(
      expect.objectContaining({ host: 'cp.internal', database: 'cpdb', user: 'cp_user' }),
    );
    expect(call.env).toBe(process.env);
    expect(typeof call.factory).toBe('function');

    expect(fetchStatusCalls).toHaveLength(1);
    // Identity: the CLI forwards the exact resolved object — not a copy of
    // the control-plane config.
    expect(fetchStatusCalls[0]!.config).toBe(TENANT_RESOLVED_CONFIG);
    expect(fetchStatusCalls[0]!.context).toEqual({
      stream: 'tenant',
      site: { siteId: SITE_ID, migrationSecretReference: null },
    });
    expect(
      logs.some((l) =>
        l.includes(
          'for target tenant.internal:5433/tenant_db_01 on stream tenant (site ' + SITE_ID + ')',
        ),
      ),
    ).toBe(true);
    expect(logs.some((l) => l.includes('cpdb'))).toBe(false);
    expect(
      [...logs, ...errors].some(
        (l) => l.includes('CP-SECRET-PASSWORD') || l.includes('TENANT-DSN-PASSWORD'),
      ),
    ).toBe(false);
  });

  it('up --stream tenant takes the migration lease around applyMigration with the resolved tenant config and the site context', async () => {
    const runMocked = await importCliMocked();
    const exit = await runMocked([
      'up',
      '0013_migration_history.sql',
      '--stream',
      'tenant',
      '--site',
      SITE_ID,
    ]);

    expect(exit).toBe(0);
    // The CLI never invokes the read-only `resolveTenantConfig` for `up`;
    // it routes through `withTenantMigrationLease`, which holds the CP
    // transaction open across the callback's tenant-side apply.
    expect(leaseCalls).toHaveLength(1);
    expect(resolveCalls).toHaveLength(0);
    const lease = leaseCalls[0]!;
    expect(lease.siteId).toBe(SITE_ID); // parser lowercases the UUID
    expect(lease.cpConfig).toEqual(
      expect.objectContaining({ host: 'cp.internal', database: 'cpdb', user: 'cp_user' }),
    );
    expect(lease.env).toBe(process.env);
    expect(typeof lease.factory).toBe('function');

    // The callback passed to the lease invoked applyMigration exactly once
    // on the RESOLVED tenant config (never the control-plane DSN).
    expect(applyCalls).toHaveLength(1);
    expect(applyCalls[0]!.config).toBe(TENANT_RESOLVED_CONFIG);
    expect(applyCalls[0]!.context).toEqual({
      stream: 'tenant',
      site: { siteId: SITE_ID, migrationSecretReference: null },
    });
    expect((lease.callbackInput as { config: unknown } | undefined)?.config).toBe(
      TENANT_RESOLVED_CONFIG,
    );
    expect(lease.callbackResult as { committed: boolean; ordinal: number } | undefined).toEqual({
      committed: true,
      ordinal: 13,
    });
    expect(
      logs.some((l) =>
        l.includes('Applying migration on stream tenant to tenant.internal:5433/tenant_db_01'),
      ),
    ).toBe(true);
    expect(logs.some((l) => l.includes('migration applied, committed and verified'))).toBe(true);
    expect(errors.filter((l) => /ABORTED|FAILED POST-VERIFICATION/.test(l))).toHaveLength(0);
  });

  it('verify --stream tenant verifies on the RESOLVED config with operation "verify" and the site context', async () => {
    const runMocked = await importCliMocked();
    const exit = await runMocked([
      'verify',
      '0013_migration_history.sql',
      '--stream',
      'tenant',
      '--site',
      SITE_ID,
    ]);

    expect(exit).toBe(0);
    expect(resolveCalls).toHaveLength(1);
    expect(resolveCalls[0]!.operation).toBe('verify');
    expect(verifyCalls).toHaveLength(1);
    expect(verifyCalls[0]!.config).toBe(TENANT_RESOLVED_CONFIG);
    expect(verifyCalls[0]!.context).toEqual({
      stream: 'tenant',
      site: { siteId: SITE_ID, migrationSecretReference: null },
    });
    expect(
      logs.some((l) =>
        l.includes(
          'Verifying schema manifest for stream tenant, target tenant.internal:5433/tenant_db_01',
        ),
      ),
    ).toBe(true);
  });

  it.each(['status', 'up', 'verify'] as const)(
    '%s --stream tenant fails closed WITHOUT --site before resolving, applying or connecting',
    async (command) => {
      const runMocked = await importCliMocked();
      const args =
        command === 'status'
          ? [command, '--stream', 'tenant']
          : [command, '0013_migration_history.sql', '--stream', 'tenant'];
      await expect(runMocked(args)).rejects.toThrow(/requires an explicit --site/i);
      expect(resolveCalls).toHaveLength(0);
      expect(fetchStatusCalls).toHaveLength(0);
      expect(applyCalls).toHaveLength(0);
      expect(verifyCalls).toHaveLength(0);
      expect(pgClientCtorCount).toBe(0);
    },
  );

  it('up --stream tenant aborts when the lease refuses the state gate and never applies on the control-plane config', async () => {
    resolverError = new Error(
      `lu_tenant_route.state active for site ${SITE_ID} does not allow the 'up' operation (expected migrating)`,
    );
    const runMocked = await importCliMocked();
    await expect(
      runMocked(['up', '0013_migration_history.sql', '--stream', 'tenant', '--site', SITE_ID]),
    ).rejects.toThrow(/does not allow the 'up' operation/);
    expect(leaseCalls).toHaveLength(1);
    expect(applyCalls).toHaveLength(0);
    expect(fetchStatusCalls).toHaveLength(0);
    expect(pgClientCtorCount).toBe(0);
    // The rejection surfaced without ever echoing the CP password.
    expect([...logs, ...errors].some((l) => l.includes('CP-SECRET-PASSWORD'))).toBe(false);
  });

  it('up --manifest-out on the tenant stream writes a secret-free artifact with the tenant target, workId and site id', async () => {
    const runMocked = await importCliMocked();
    const artifactPath = path.join(tmpDir, 'up-tenant.json');
    const exit = await runMocked([
      'up',
      '0013_migration_history.sql',
      '--manifest-out',
      artifactPath,
      '--stream',
      'tenant',
      '--site',
      SITE_ID,
    ]);
    expect(exit).toBe(0);
    expect(existsSync(artifactPath)).toBe(true);
    const raw = readFileSync(artifactPath, 'utf8');
    const artifact = JSON.parse(raw);
    expect(artifact.command).toBe('up');
    expect(artifact.readOnly).toBe(false);
    expect(artifact.executed).toBe(true);
    expect(artifact.committed).toBe(true);
    expect(artifact.skipped).toBe(false);
    expect(artifact.stream).toBe('tenant');
    expect(artifact.relativePath).toBe('tenant/0013_migration_history.sql');
    expect(artifact.workId).toBe('MIG-F2-TENANT-MIGRATION-HISTORY');
    expect(artifact.file).toBe('0013_migration_history.sql');
    expect(artifact.policy).toBe('strict');
    expect(artifact.target).toBe('tenant.internal:5433/tenant_db_01 [ssl=disable (cleartext)]');
    expect(artifact.site).toEqual({ siteId: SITE_ID });
    expect(artifact.history).toEqual({
      stream: 'tenant',
      relativePath: 'tenant/0013_migration_history.sql',
    });
    expect(artifact.sha256).toBe(
      '5a2c7e89054b58d57e9b2958c60ac259c1a0f8cfdbca8cc10403d91728ea16d8',
    );
    expect(raw).not.toContain('CP-SECRET-PASSWORD');
    expect(raw).not.toContain('TENANT-DSN-PASSWORD');
    expect(raw).not.toContain('cpdb');
    expect(raw).not.toContain('tenant_db_01;');
  });

  it('plan stays DB-free for tenant / control-plane / both streams even with NO connection env var', async () => {
    const saved = process.env['ConnectionStrings__DefaultConnection'];
    delete process.env['ConnectionStrings__DefaultConnection'];
    const runMocked = await importCliMocked();
    try {
      expect(await runMocked(['plan', '--stream', 'tenant'])).toBe(0);
      expect(await runMocked(['plan', '--stream', 'control-plane'])).toBe(0);
      expect(await runMocked(['plan'])).toBe(0);
      expect(logs.some((l) => l.includes('offline analysis; no database connection made'))).toBe(
        true,
      );
      expect(logs.some((l) => l.includes('plan: 13 registered migration(s) validated'))).toBe(true);
      expect(logs.some((l) => l.includes('plan: 6 registered migration(s) validated'))).toBe(true);
      expect(logs.some((l) => l.includes('plan: 19 registered migration(s) validated'))).toBe(true);
      expect(logs.some((l) => l.includes('0013_migration_history.sql'))).toBe(true);
      expect(resolveCalls).toHaveLength(0);
      expect(fetchStatusCalls).toHaveLength(0);
      expect(applyCalls).toHaveLength(0);
      expect(verifyCalls).toHaveLength(0);
      expect(pgClientCtorCount).toBe(0);
    } finally {
      if (saved !== undefined) {
        process.env['ConnectionStrings__DefaultConnection'] = saved;
      }
    }
  });

  it('status on the default control-plane stream uses the CP config directly and never invokes the tenant resolver', async () => {
    const runMocked = await importCliMocked();
    expect(await runMocked(['status'])).toBe(0);
    expect(resolveCalls).toHaveLength(0);
    expect(fetchStatusCalls).toHaveLength(1);
    expect(fetchStatusCalls[0]!.config).toEqual(
      expect.objectContaining({
        host: 'cp.internal',
        database: 'cpdb',
        user: 'cp_user',
        password: 'CP-SECRET-PASSWORD',
      }),
    );
    expect(fetchStatusCalls[0]!.config).not.toBe(TENANT_RESOLVED_CONFIG);
    expect(logs.some((l) => l.includes('cp.internal:5432/cpdb'))).toBe(true);
  });
});

// ---------------------------------------------------------------------------
// MIG-001-F2-W15A — CLI routing contract for the lease.
//
// Proves the three CLI-side invariants:
//   1. `up --stream tenant` ALWAYS holds the migration lease, with the CP
//      config as `controlPlaneConfig`, the resolved tenant config fed into
//      `applyMigration` via the callback, and the ordinal committed.
//   2. `status --stream tenant` and `verify --stream tenant` continue to
//      use the read-only `resolveTenantConfig` and NEVER enter the lease —
//      no CP transaction is opened for these commands.
//   3. `plan` is fully DB-free; it never touches the resolver, the lease
//      or any `pg.Client` regardless of stream.
// ---------------------------------------------------------------------------

describe('migration-cli W15A: lease routing contract (no DB)', () => {
  const SITE_ID = '3f2b7c1d-9a4e-4b6f-8c2d-1e5a7b9c0d1e';
  const CP_DSN =
    'Host=cp.internal;Port=5432;Database=cpdb;Username=cp_user;Password=CP-SECRET-PASSWORD';
  const TENANT_RESOLVED_CONFIG = {
    host: 'tenant.internal',
    port: 5433,
    database: 'tenant_db_01',
    user: 'mig_user',
    password: 'TENANT-DSN-PASSWORD',
    ssl: false as const,
  };

  interface ResolveCall {
    siteId: string;
    operation: string;
    cpConfig: unknown;
    env: unknown;
    factory: unknown;
  }
  interface RunnerCall {
    config: unknown;
    context: unknown;
  }
  interface LeaseCall {
    siteId: string;
    env: unknown;
    cpConfig: unknown;
    factory: unknown;
    callbackInput?: unknown;
    callbackResult?: unknown;
  }

  let resolveCalls: ResolveCall[] = [];
  let fetchStatusCalls: RunnerCall[] = [];
  let applyCalls: RunnerCall[] = [];
  let verifyCalls: RunnerCall[] = [];
  let leaseCalls: LeaseCall[] = [];
  let pgClientCtorCount = 0;
  let resolverError: Error | null = null;
  let leaseCallbackOverride: ((input: unknown) => Promise<unknown>) | null = null;

  const logs: string[] = [];
  const errors: string[] = [];

  async function importLeaseCliMocked(): Promise<
    (typeof import('../src/database/migration-cli.js'))['run']
  > {
    jestRuntime.resetModules();
    unstableMockModule('pg', () => ({
      Client: class {
        constructor() {
          pgClientCtorCount += 1;
        }
        async connect(): Promise<void> {}
        async query(): Promise<{ rows: unknown[] }> {
          return { rows: [] };
        }
        async end(): Promise<void> {}
      },
    }));
    unstableMockModule('../src/database/tenant-config.js', () => ({
      resolveTenantConfig: async (
        siteId: string,
        operation: string,
        cpConfig: unknown,
        env: unknown,
        factory: unknown,
      ) => {
        resolveCalls.push({ siteId, operation, cpConfig, env, factory });
        if (resolverError !== null) throw resolverError;
        return {
          config: TENANT_RESOLVED_CONFIG,
          safeTarget: { host: 'tenant.internal', port: 5433, database: 'tenant_db_01' },
        };
      },
      withTenantMigrationLease: async (opts: {
        siteId: string;
        env: unknown;
        controlPlaneConfig: unknown;
        controlPlaneClientFactory: unknown;
        callback: (input: unknown) => Promise<unknown>;
      }) => {
        const callbackInput = {
          config: TENANT_RESOLVED_CONFIG,
          safeTarget: { host: 'tenant.internal', port: 5433, database: 'tenant_db_01' },
          state: 'migrating' as const,
          writerLabel: 'NEST_POSTGRES' as const,
          migrationSecretReference: 'tenant/prod.db-01',
        };
        leaseCalls.push({
          siteId: opts.siteId,
          env: opts.env,
          cpConfig: opts.controlPlaneConfig,
          factory: opts.controlPlaneClientFactory,
          callbackInput,
        });
        if (resolverError !== null) throw resolverError;
        const cb = leaseCallbackOverride ?? opts.callback;
        const result = await cb(callbackInput);
        leaseCalls[leaseCalls.length - 1]!.callbackResult = result;
        return {
          ordinal: 13,
          state: 'migrating' as const,
          writerLabel: 'NEST_POSTGRES' as const,
        };
      },
      TenantConfigError: class TenantConfigError extends Error {
        constructor(message: string) {
          super(message);
          this.name = 'TenantConfigError';
        }
      },
      TenantMigrationLeaseError: class TenantMigrationLeaseError extends Error {
        constructor(message: string) {
          super(message);
          this.name = 'TenantMigrationLeaseError';
        }
      },
      sanitizeReference: (ref: string) => ref.replace(/[^A-Za-z0-9]+/g, '_'),
    }));
    unstableMockModule('../src/database/migration-runner.js', () => ({
      fetchStatus: async (config: unknown, context: unknown) => {
        fetchStatusCalls.push({ config, context });
        return {
          server: { database: 'cpdb', user: 'cp_user', version: '16.0' },
          tables: [],
        };
      },
      applyMigration: async (
        config: unknown,
        _filePath: string,
        _content: Buffer,
        context: unknown,
      ) => {
        applyCalls.push({ config, context });
        return {
          preexisting: [],
          executed: true,
          committed: true,
          verification: { passed: true, diffs: [] },
          inspection: {
            server: { database: 'cpdb', user: 'cp_user', version: '16.0' },
            tables: [],
          },
          historyKey: { stream: 'tenant', relativePath: 'tenant/0013_migration_history.sql' },
        };
      },
      verifyMigration: async (
        config: unknown,
        _filePath: string,
        _content: Buffer,
        context: unknown,
      ) => {
        verifyCalls.push({ config, context });
        return {
          verification: { passed: true, diffs: [] },
          inspection: {
            server: { database: 'cpdb', user: 'cp_user', version: '16.0' },
            tables: [],
          },
        };
      },
    }));
    const cli = await import('../src/database/migration-cli.js');
    return cli.run;
  }

  beforeEach(() => {
    resolveCalls = [];
    fetchStatusCalls = [];
    applyCalls = [];
    verifyCalls = [];
    leaseCalls = [];
    pgClientCtorCount = 0;
    resolverError = null;
    leaseCallbackOverride = null;
    logs.length = 0;
    errors.length = 0;
    process.env['ConnectionStrings__DefaultConnection'] = CP_DSN;
    jestRuntime.spyOn(console, 'log').mockImplementation((...args: unknown[]) => {
      logs.push(args.map(String).join(' '));
    });
    jestRuntime.spyOn(console, 'error').mockImplementation((...args: unknown[]) => {
      errors.push(args.map(String).join(' '));
    });
  });

  afterEach(() => {
    jestRuntime.restoreAllMocks();
    delete process.env['ConnectionStrings__DefaultConnection'];
  });

  afterAll(() => {
    jestRuntime.resetModules();
  });

  it('up --stream tenant holds the lease with CP controlPlaneConfig and forwards the callback input.config to applyMigration', async () => {
    const runMocked = await importLeaseCliMocked();
    const exit = await runMocked([
      'up',
      '0013_migration_history.sql',
      '--stream',
      'tenant',
      '--site',
      SITE_ID,
    ]);
    expect(exit).toBe(0);
    expect(leaseCalls).toHaveLength(1);
    expect(resolveCalls).toHaveLength(0);
    const lease = leaseCalls[0]!;
    // The lease receives the parsed CP DSN as controlPlaneConfig — never
    // the resolved tenant config.
    expect(lease.cpConfig).toEqual(
      expect.objectContaining({ host: 'cp.internal', database: 'cpdb', user: 'cp_user' }),
    );
    expect(lease.cpConfig).not.toBe(TENANT_RESOLVED_CONFIG);
    // The callback input carries the RESOLVED tenant config (NOT the CP one).
    const ci = lease.callbackInput as { config: unknown; safeTarget: unknown } | undefined;
    expect(ci?.config).toBe(TENANT_RESOLVED_CONFIG);
    expect(ci?.config).not.toBe(lease.cpConfig);
    // applyMigration inside the callback observed the tenant config.
    expect(applyCalls).toHaveLength(1);
    expect(applyCalls[0]!.config).toBe(TENANT_RESOLVED_CONFIG);
    // No CP password or secret was echoed.
    expect([...logs, ...errors].some((l) => l.includes('CP-SECRET-PASSWORD'))).toBe(false);
    expect([...logs, ...errors].some((l) => l.includes('TENANT-DSN-PASSWORD'))).toBe(false);
  });

  it('status --stream tenant uses the read-only resolver and NEVER enters the lease', async () => {
    const runMocked = await importLeaseCliMocked();
    const exit = await runMocked(['status', '--stream', 'tenant', '--site', SITE_ID]);
    expect(exit).toBe(0);
    expect(leaseCalls).toHaveLength(0);
    expect(resolveCalls).toHaveLength(1);
    expect(resolveCalls[0]!.operation).toBe('status');
    expect(fetchStatusCalls).toHaveLength(1);
    expect(fetchStatusCalls[0]!.config).toBe(TENANT_RESOLVED_CONFIG);
    expect(applyCalls).toHaveLength(0);
    expect(verifyCalls).toHaveLength(0);
    expect(pgClientCtorCount).toBe(0);
  });

  it('verify --stream tenant uses the read-only resolver and NEVER enters the lease', async () => {
    const runMocked = await importLeaseCliMocked();
    const exit = await runMocked([
      'verify',
      '0013_migration_history.sql',
      '--stream',
      'tenant',
      '--site',
      SITE_ID,
    ]);
    expect(exit).toBe(0);
    expect(leaseCalls).toHaveLength(0);
    expect(resolveCalls).toHaveLength(1);
    expect(resolveCalls[0]!.operation).toBe('verify');
    expect(verifyCalls).toHaveLength(1);
    expect(verifyCalls[0]!.config).toBe(TENANT_RESOLVED_CONFIG);
    expect(applyCalls).toHaveLength(0);
    expect(pgClientCtorCount).toBe(0);
  });

  it('plan stays DB-free across tenant / control-plane / both streams and never touches the lease or the resolver', async () => {
    const runMocked = await importLeaseCliMocked();
    expect(await runMocked(['plan', '--stream', 'tenant'])).toBe(0);
    expect(await runMocked(['plan', '--stream', 'control-plane'])).toBe(0);
    expect(await runMocked(['plan'])).toBe(0);
    expect(leaseCalls).toHaveLength(0);
    expect(resolveCalls).toHaveLength(0);
    expect(applyCalls).toHaveLength(0);
    expect(fetchStatusCalls).toHaveLength(0);
    expect(verifyCalls).toHaveLength(0);
    expect(pgClientCtorCount).toBe(0);
  });

  it('up --stream tenant treats a callback throwing as ABORTED (the route row must not advance)', async () => {
    // Override the lease mock callback to throw AFTER invoking the runner
    // so we observe both: applyMigration is called with the resolved
    // tenant config, the callback throws, the lease rethrows, the CLI
    // propagates and the route version is never advanced.
    leaseCallbackOverride = async (input) => {
      const ci = input as { config: unknown };
      expect(ci.config).toBe(TENANT_RESOLVED_CONFIG);
      // Stub the runner OUTSIDE the mock: directly assert it was called.
      applyCalls.push({
        config: ci.config,
        context: { stream: 'tenant', site: { siteId: SITE_ID, migrationSecretReference: null } },
      });
      throw new Error('tenant runner verification failure');
    };
    const runMocked = await importLeaseCliMocked();
    await expect(
      runMocked(['up', '0013_migration_history.sql', '--stream', 'tenant', '--site', SITE_ID]),
    ).rejects.toThrow(/tenant runner verification failure/i);
    expect(leaseCalls).toHaveLength(1);
    expect(applyCalls).toHaveLength(1);
    expect(applyCalls[0]!.config).toBe(TENANT_RESOLVED_CONFIG);
    // CP password never echoed.
    expect([...logs, ...errors].some((l) => l.includes('CP-SECRET-PASSWORD'))).toBe(false);
  });
});

describe('migration-cli W11: --stream / --site parsing', () => {
  const SITE_ID = '3f2b7c1d-9a4e-4b6f-8c2d-1e5a7b9c0d1e';

  it('extracts stream and site in both flag forms, lowercasing the UUID', () => {
    expect(parseCommandArgs('status', ['--stream', 'tenant', '--site', SITE_ID])).toEqual({
      fileArg: undefined,
      manifestOut: undefined,
      stream: 'tenant',
      siteId: SITE_ID,
    });
    expect(
      parseCommandArgs('up', [
        '--stream=control-plane',
        `--site=${SITE_ID.toUpperCase()}`,
        '0013.sql',
      ]),
    ).toEqual({
      fileArg: '0013.sql',
      manifestOut: undefined,
      stream: 'control-plane',
      siteId: SITE_ID,
    });
    // Flags may appear before or after the positional file.
    expect(parseCommandArgs('verify', ['0013.sql', '--stream', 'tenant'])).toEqual({
      fileArg: '0013.sql',
      manifestOut: undefined,
      stream: 'tenant',
      siteId: undefined,
    });
  });

  it('rejects unknown streams, malformed or missing site UUIDs and unknown flags', () => {
    expect(() => parseCommandArgs('up', ['--stream', 'main'])).toThrow(/--stream must be/);
    expect(() => parseCommandArgs('up', ['--stream='])).toThrow(/--stream must be/);
    expect(() => parseCommandArgs('up', ['--site', 'not-a-uuid'])).toThrow(
      /--site must be a RFC 4122 UUID/,
    );
    expect(() => parseCommandArgs('up', ['--site'])).toThrow(/--site must be a RFC 4122 UUID/);
    expect(() => parseCommandArgs('up', ['--bogus'])).toThrow(/unknown flag/);
  });
});

// ---------------------------------------------------------------------------
// MIG-001 F8 — control-plane 0004 cutover preflight.
//
// Proves that the migration-cli refuses to apply ordinal 4
// (`0004_academic_catalogs.sql`) when the roles/003 runtime grants are
// missing, WITHOUT invoking the runner (no DDL / no ledger write). Other
// ordinals and an already-applied 0004 are not checked. The harness never
// connects to a real database; the pg.Client is replaced with a fake that
// records every SQL the preflight issues.
// ---------------------------------------------------------------------------

describe('migration-cli F8: control-plane 0004 cutover preflight (mocked pg.Client + runner, no DB)', () => {
  const CP_DSN =
    'Host=cp.internal;Port=5432;Database=cpdb;Username=cp_user;Password=CP-SECRET-PASSWORD';

  interface ClientCall {
    readonly sql: string;
    readonly values?: readonly unknown[];
  }
  interface ClientResponses {
    [pattern: string]: readonly Record<string, unknown>[];
  }

  let clientQueries: ClientCall[] = [];
  let clientResponses: ClientResponses = {};
  let clientErrors: { pattern: RegExp; error: Error } | null = null;
  let applyCalls: number = 0;

  function fakeClientBehavior(): {
    connect(): Promise<void>;
    query<R extends Record<string, unknown>>(
      sql: string,
      values?: unknown[],
    ): Promise<{ rows: readonly R[] }>;
    end(): Promise<void>;
  } {
    return {
      async connect(): Promise<void> {
        /* no-op */
      },
      async query<R extends Record<string, unknown>>(
        sql: string,
        values?: unknown[],
      ): Promise<{ rows: readonly R[] }> {
        clientQueries.push(values === undefined ? { sql } : { sql, values });
        if (clientErrors !== null && clientErrors.pattern.test(sql)) {
          throw clientErrors.error;
        }
        for (const [pattern, rows] of Object.entries(clientResponses)) {
          if (new RegExp(pattern).test(sql)) {
            return { rows: rows as readonly R[] };
          }
        }
        return { rows: [] };
      },
      async end(): Promise<void> {
        /* no-op */
      },
    };
  }

  async function importCliMocked(): Promise<
    (typeof import('../src/database/migration-cli.js'))['run']
  > {
    jestRuntime.resetModules();
    unstableMockModule('pg', () => ({
      Client: class {
        constructor() {
          return fakeClientBehavior();
        }
        connect = fakeClientBehavior().connect;
        query = fakeClientBehavior().query;
        end = fakeClientBehavior().end;
      },
    }));
    unstableMockModule('../src/database/migration-runner.js', () => ({
      fetchStatus: async () => ({
        server: { database: 'cpdb', user: 'cp_user', version: '16.0' },
        tables: [],
      }),
      applyMigration: async () => {
        applyCalls += 1;
        return {
          preexisting: [],
          executed: true,
          committed: true,
          verification: { passed: true, diffs: [] },
          inspection: {
            server: { database: 'cpdb', user: 'cp_user', version: '16.0' },
            tables: [],
          },
          historyKey: {
            stream: 'control-plane',
            relativePath: 'control-plane/0004_academic_catalogs.sql',
          },
        };
      },
      verifyMigration: async () => ({
        verification: { passed: true, diffs: [] },
        inspection: { server: { database: 'cpdb', user: 'cp_user', version: '16.0' }, tables: [] },
      }),
    }));
    const cli = await import('../src/database/migration-cli.js');
    return cli.run;
  }

  beforeEach(() => {
    clientQueries = [];
    clientResponses = {};
    clientErrors = null;
    applyCalls = 0;
    process.env['ConnectionStrings__DefaultConnection'] = CP_DSN;
    jestRuntime.spyOn(console, 'log').mockImplementation(() => undefined);
    jestRuntime.spyOn(console, 'error').mockImplementation(() => undefined);
  });

  afterEach(() => {
    jestRuntime.restoreAllMocks();
    delete process.env['ConnectionStrings__DefaultConnection'];
  });

  afterAll(() => {
    jestRuntime.resetModules();
  });

  it('up control-plane 0004 refuses before any DDL when roles/003 runtime grants are missing', async () => {
    clientResponses = {
      // Fresh control plane: the ledger only appears with 0006 and 0004 is absent.
      "table_name = 'lu_migration_history'": [{ exists: false }],
      "to_regclass\\('public\\.lu_faculty'\\)": [{ exists: false }],
      // lu_auth_runtime role exists.
      'FROM pg_catalog\\.pg_roles WHERE rolname = \\$1': [{ count: 1 }],
      // Runtime grants are missing SELECT on lu_site.
      "pg_catalog\\.has_table_privilege\\('lu_auth_runtime'": [
        {
          lu_site_select: false,
          lu_user_select: true,
          lu_site_membership_select: true,
          lu_session_select: true,
          lu_auth_rate_limit_select_delete: true,
          lu_tenant_route_select: true,
          lu_tenant_route_disallowed: false,
          lu_security_event_disallowed: false,
        },
      ],
    };
    const runMocked = await importCliMocked();
    await expect(
      runMocked(['up', '0004_academic_catalogs.sql', '--stream', 'control-plane']),
    ).rejects.toThrow(/SELECT on public\.lu_site/);
    // The runner must NEVER be invoked on a failed preflight: no DDL,
    // no ledger write, no pg_ledger inserts.
    expect(applyCalls).toBe(0);
    const preflightQueries = clientQueries.map((q) => q.sql);
    expect(preflightQueries.some((sql) => /has_table_privilege/.test(sql))).toBe(true);
    // No `BEGIN ISOLATION LEVEL` / canonical body execution.
    expect(preflightQueries.some((sql) => /BEGIN ISOLATION LEVEL/.test(sql))).toBe(false);
    // No roles/003-style DDL from the runner.
    expect(preflightQueries.some((sql) => /CREATE TABLE/.test(sql))).toBe(false);
  });

  it('up control-plane 0004 proceeds when roles/003 runtime grants are present and 0004 is not yet applied', async () => {
    clientResponses = {
      // Fresh control plane: the ledger only appears with 0006 and 0004 is absent.
      "table_name = 'lu_migration_history'": [{ exists: false }],
      "to_regclass\\('public\\.lu_faculty'\\)": [{ exists: false }],
      'FROM pg_catalog\\.pg_roles WHERE rolname = \\$1': [{ count: 1 }],
      "pg_catalog\\.has_table_privilege\\('lu_auth_runtime'": [
        {
          lu_site_select: true,
          lu_user_select: true,
          lu_site_membership_select: true,
          lu_session_select: true,
          lu_auth_rate_limit_select_delete: true,
          lu_tenant_route_select: true,
          lu_tenant_route_disallowed: false,
          lu_security_event_disallowed: false,
        },
      ],
    };
    const runMocked = await importCliMocked();
    const exit = await runMocked(['up', '0004_academic_catalogs.sql', '--stream', 'control-plane']);
    expect(exit).toBe(0);
    expect(applyCalls).toBe(1);
    // The pre-0006 path never reads the (absent) ledger table itself.
    expect(clientQueries.some((q) => /FROM public\.lu_migration_history/.test(q.sql))).toBe(false);
    expect(clientQueries.some((q) => /to_regclass/.test(q.sql))).toBe(true);
  });

  it('up control-plane 0004 skips the preflight when 0004 is already recorded as applied', async () => {
    clientResponses = {
      "table_name = 'lu_migration_history'": [{ exists: true }],
      'FROM public\\.lu_migration_history\\s+WHERE stream = \\$1 AND ordinal = 4': [
        { exists: true },
      ],
    };
    const runMocked = await importCliMocked();
    const exit = await runMocked(['up', '0004_academic_catalogs.sql', '--stream', 'control-plane']);
    expect(exit).toBe(0);
    expect(applyCalls).toBe(1);
    // No runtime grant check happens once 0004 is already on the ledger.
    const preflightSawGrants = clientQueries.some((q) => /has_table_privilege/.test(q.sql));
    expect(preflightSawGrants).toBe(false);
  });

  it('up control-plane 0004 skips the preflight on a pre-ledger database where 0004 objects exist', async () => {
    clientResponses = {
      "table_name = 'lu_migration_history'": [{ exists: false }],
      "to_regclass\\('public\\.lu_faculty'\\)": [{ exists: true }],
    };
    const runMocked = await importCliMocked();
    const exit = await runMocked(['up', '0004_academic_catalogs.sql', '--stream', 'control-plane']);
    expect(exit).toBe(0);
    expect(applyCalls).toBe(1);
    expect(clientQueries.some((q) => /has_table_privilege/.test(q.sql))).toBe(false);
  });

  it('up of any other control-plane ordinal does not run the preflight', async () => {
    clientResponses = {};
    const runMocked = await importCliMocked();
    const exit = await runMocked([
      'up',
      '0001_create_identity_control_plane.sql',
      '--stream',
      'control-plane',
    ]);
    expect(exit).toBe(0);
    expect(applyCalls).toBe(1);
    // The preflight is keyed off ordinal === 4 only.
    const preflightSawLedger = clientQueries.some((q) =>
      /lu_migration_history.*ordinal = 4/.test(q.sql),
    );
    expect(preflightSawLedger).toBe(false);
    const preflightSawGrants = clientQueries.some((q) => /has_table_privilege/.test(q.sql));
    expect(preflightSawGrants).toBe(false);
  });

  it('up control-plane 0004 fails closed when the preflight connection itself errors', async () => {
    clientErrors = {
      pattern: /information_schema\.tables/,
      error: new Error('connection refused'),
    };
    const runMocked = await importCliMocked();
    await expect(
      runMocked(['up', '0004_academic_catalogs.sql', '--stream', 'control-plane']),
    ).rejects.toThrow(/unable to verify roles\/003 runtime postconditions/);
    expect(applyCalls).toBe(0);
  });
});
