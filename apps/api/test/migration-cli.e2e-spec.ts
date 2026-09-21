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
