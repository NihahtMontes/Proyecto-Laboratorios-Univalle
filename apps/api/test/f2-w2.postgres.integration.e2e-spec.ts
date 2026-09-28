/**
 * MIG-F2-W2 / W15B — Opt-in real PostgreSQL integration harness.
 *
 * GATE (rule 1): activates ONLY when BOTH conditions hold:
 *   - `F2_INTEGRATION=1`, AND
 *   - an admin DSN configured via `F2_INTEGRATION_DSN` (preferred) or
 *     `ConnectionStrings__DefaultConnection` parses to a LOOPBACK host whose
 *     database is exactly `neondb`.
 * The admin DSN is used ONLY for external bootstrap (role validation/creation,
 * CREATE/DROP of temp DBs). It is NEVER handed to the migration CLI and never
 * used for data writes; every CLI invocation and every fixture write
 * authenticates as the unique temporary executor login and switches to the
 * migrator GROUP with SET (LOCAL) ROLE. Teardown catalog assertions also run
 * through the executor, so the admin connection only performs bootstrap and
 * drop/residue queries.
 *
 * BOOTSTRAP (rule 2), in beforeAll of the gated describe:
 *   - `lu_auth_runtime` must already exist and be safe (NOLOGIN, not
 *     superuser, no CREATEROLE/CREATEDB/REPLICATION/BYPASSRLS): validated,
 *     never created nor mutated.
 *   - `lu_auth_migrator` (the object-owning GROUP) must exist as
 *     NOLOGIN/NOSUPERUSER/NOCREATEDB/NOCREATEROLE/NOREPLICATION/NOBYPASSRLS.
 *     Created only when absent; an existing unsafe group aborts setup and is
 *     NEVER mutated. The group is required infrastructure: cleanup keeps it.
 *   - A unique temporary executor login `lu_migration_exec_<hex>` is created
 *     with a cryptographic temporary password (NOINHERIT, no elevated
 *     attributes) and granted group membership. The runner executes
 *     `SET LOCAL ROLE lu_auth_migrator`, so every migrated object is owned by
 *     the group. Passwords and DSNs are never logged (rules 2/7): every thrown
 *     message is scrubbed and every leak assertion is boolean-only.
 *
 * DATABASES (rule 3): four unique temp DBs (fresh CP, upgrade CP, router CP,
 * tenant) created `WITH OWNER lu_auth_migrator`, hardened per DB (PUBLIC and
 * the runtime role lose CREATE on schema public; the group keeps it). afterAll
 * drops every temp DB, revokes and drops the executor login, and ASSERTS zero
 * DB/login residue; the group stays. Cleanup is best-effort but any cleanup
 * failure aggregates into a thrown afterAll error — it is never swallowed.
 *
 * FRESH CP (rule 4): CLI applies 0001..0006 one at a time through the
 * executor DSN; old (0001) and latest (0006) pins are VERIFIED and RE-APPLIED
 * (replay must commit as `pin-matches-history` skip). Manifests exist, are
 * field-checked against imported registry entries and scanned for admin or
 * executor passwords, DSNs, the secret env name and the migration secret
 * reference. Constraints, claims, status mapping, full_name derivation, audit
 * append-only, xref uniqueness/immutability, ledger immutability and ACL/role
 * ownership are exercised with deterministic syntactically-valid 60-char
 * bcrypt fixtures, one SQL SAVEPOINT per expected negative, and the fixture
 * transaction is rolled back with baseline row-count residue assertions.
 *
 * UPGRADE CP (rule 5): apply0001 → seed one valid-shape bcrypt user, one
 * malformed hash user (status disabled) and one invalid-cost bcrypt user →
 * apply0002..0006. Valid hash survives byte-for-byte (bcrypt / must_change
 * false); invalid ones become reset_required with NULL hash and
 * must_change=true; disabled maps to inactive; rows stay
 * pending_reconciliation; original full_name survives; sessions default
 * purpose 'normal'. Verify + replay must not alter the seeded bytes. No
 * production or legacy source is ever loaded.
 *
 * ROUTER + TENANT (rule 6): CP 0001..0006 land on the router DB; a temp site
 * plus a route row in state 'migrating', an allowed writer label and a
 * `migration_secret_reference` that maps to an injected tenant env DSN are
 * inserted AS THE GROUP. Tenant 0001..0013 then apply strictly sequentially
 * through the CLI with `--stream tenant --site <uuid>` (never through direct
 * control-plane config). Each lease bumps schema_version monotonically and
 * stamps last_health_at while state/writer/reference stay unchanged. After 13:
 * the ledger is exactly contiguous 1..13 with the imported registry pins and
 * workIds, old+latest tenant pins verify and replay-skip, the final verify
 * manifest is field-exact, append-only is probed (positive bytes>0 INSERT and
 * SAVEPOINT negatives for UPDATE/DELETE/PK/bytes=0), a concurrent replay race
 * serializes cleanly, and the ACL proves the runtime role has no schema CREATE
 * and owns nothing while the group owns every public object.
 *
 * SECURITY (rules 7/8/9): an invalid fixture transaction is proven to roll
 * back to zero residue; all collected CLI stdout/stderr, manifest files and
 * env builders are leak-scanned with boolean-only checks; child processes run
 * cross-platform without a shell (process.execPath + argv + per-step
 * timeouts, no persistent server); the temp directory is removed; registry
 * pins/workIds are imported, never duplicated.
 *
 * RESOLVED BLOCKER NOTES (historical, for reference):
 *   B2) RESOLVED W16B: 0006 classifies bcrypt with '[./A-Za-z0-9]{53}', the
 *       same full alphabet that auth.service.ts/auth.config.ts use
 *       ('[./A-Za-z0-9]{53}'): real bcrypt digests (uppercase included)
 *       survive classification and satisfy
 *       ck_lu_user_password_hash_conditional on later writes. Fixtures
 *       therefore use a deterministic FULL-alphabet 53-char payload that
 *       satisfies the shared class.
 *
 * DO NOT ACTIVATE BY DEFAULT. Real run recipe (manual, opt-in):
 *   corepack pnpm run build          (from apps/api; the CLI needs dist/)
 *   F2_INTEGRATION=1 F2_INTEGRATION_DSN='Host=127.0.0.1;Port=5432;Database=neondb;Username=<admin>;Password=<admin>;SSL Mode=Disable' \
 *     node --experimental-vm-modules node_modules/jest/bin/jest.js --runInBand test/f2-w2.postgres.integration.e2e-spec.ts
 * Bootstrap assumes the admin role can CREATE DATABASE and terminate client
 * backends (superuser or CREATEDB + no member-of restrictions).
 */
import { spawn, spawnSync, type SpawnSyncReturns } from 'node:child_process';
import { randomBytes, randomUUID } from 'node:crypto';
import { existsSync, mkdtempSync, readFileSync, readdirSync, rmSync } from 'node:fs';
import { tmpdir } from 'node:os';
import { join } from 'node:path';
import { fileURLToPath } from 'node:url';
import { Client } from 'pg';
import {
  CONNECTION_STRING_ENV_VAR,
  loadConnectionConfigFromEnv,
  type PostgresConnectionConfig,
} from '../src/database/connection-config.js';
import {
  CONTROL_PLANE_REGISTRY,
  TENANT_REGISTRY,
  type MigrationRegistryEntry,
} from '../src/database/migration-registry.js';
import { sanitizeReference } from '../src/database/tenant-config.js';

const API_ROOT = fileURLToPath(new URL('../', import.meta.url));
const CLI_JS = join(API_ROOT, 'dist', 'database', 'migration-cli.js');

const INTEGRATION_FLAG = 'F2_INTEGRATION';
const ADMIN_DSN_ENV = 'F2_INTEGRATION_DSN';
const REQUIRED_TARGET_DB = 'neondb';

const GROUP_ROLE = 'lu_auth_migrator';
const RUNTIME_ROLE = 'lu_auth_runtime';

const RUN_ID = randomBytes(5).toString('hex');
const EXECUTOR = `lu_migration_exec_${RUN_ID}`;
const FRESH_DB = `f2w2w15b_fresh_${RUN_ID}`;
const UPGRADE_DB = `f2w2w15b_upgrade_${RUN_ID}`;
const ROUTER_DB = `f2w2w15b_router_${RUN_ID}`;
const TENANT_DB = `f2w2w15b_tenant_${RUN_ID}`;
const TEMP_DBS: readonly string[] = [FRESH_DB, UPGRADE_DB, ROUTER_DB, TENANT_DB];

/** migration_secret_reference shape: F1 regex + lowercase hex suffix. */
const MIGRATION_REF = `f2w2w15b-migration-${RUN_ID}`;
const TENANT_ENV_NAME = `TENANT_MIGRATION_CONNECTION__${sanitizeReference(MIGRATION_REF)}`;

/**
 * Deterministic 53-char bcrypt payload drawn from the FULL bcrypt alphabet
 * [./A-Za-z0-9] (uppercase included) so the fixture exercises exactly the
 * shared 0006 SQL class and the runtime class (W16B aligned both to
 * '[./A-Za-z0-9]{53}').
 */
const BCRYPT_PAYLOAD_53 = 'AbCdEfGhIjKlMnOpQrStUvWxYz0123456789AbCdEf./0123456789GhIj'.slice(0, 53);
const VALID_BCRYPT_HASH = `$2b$12$${BCRYPT_PAYLOAD_53}`;
const INVALID_COST_BCRYPT_SHAPE = `$2b$03$${BCRYPT_PAYLOAD_53}`;
/** Mirror of the exact 0006 SQL class: the full bcrypt alphabet [./A-Za-z0-9]. */
const BCRYPT_SQL_SHAPE = /^\$2[aby]\$(0[4-9]|[12][0-9]|3[01])\$[./A-Za-z0-9]{53}$/;

interface ResolvedIntegration {
  readonly enabled: boolean;
  readonly admin: PostgresConnectionConfig | null;
  readonly reason: string;
}

/** Pure gate evaluation — used offline by the gate spec and never connects. */
export function resolveIntegration(env: NodeJS.ProcessEnv): ResolvedIntegration {
  if (env[INTEGRATION_FLAG] !== '1') {
    return { enabled: false, admin: null, reason: `${INTEGRATION_FLAG} is not set to 1` };
  }
  const candidate = env[ADMIN_DSN_ENV] ?? env[CONNECTION_STRING_ENV_VAR];
  if (candidate === undefined || candidate.trim() === '') {
    return { enabled: false, admin: null, reason: 'no admin DSN configured' };
  }
  let admin: PostgresConnectionConfig;
  try {
    admin = loadConnectionConfigFromEnv({ [CONNECTION_STRING_ENV_VAR]: candidate });
  } catch (error) {
    return {
      enabled: false,
      admin: null,
      // ConnectionConfigError never embeds values; cap the echo defensively.
      reason: `admin DSN rejected: ${(error as Error).message.slice(0, 160)}`,
    };
  }
  const host = admin.host.toLowerCase();
  const db = admin.database.toLowerCase();
  const isLoopback = host === '127.0.0.1' || host === 'localhost' || host === '::1';
  if (!isLoopback) {
    return {
      enabled: false,
      admin: null,
      reason: `target host "${host}" is not loopback; refusing to run destructive steps`,
    };
  }
  if (db !== REQUIRED_TARGET_DB) {
    return {
      enabled: false,
      admin: null,
      reason: `target database "${db}" is not the canonical control plane "${REQUIRED_TARGET_DB}"`,
    };
  }
  return { enabled: true, admin, reason: 'integration enabled' };
}

function keyValueDsn(p: {
  host: string;
  port: number;
  database: string;
  user: string;
  password: string;
  ssl: false | object;
}): string {
  const ssl = p.ssl === false ? 'Disable' : 'Prefer';
  const quoted = /['\s;]/.test(p.password) ? `'${p.password.replace(/'/g, "''")}'` : p.password;
  return (
    `Host=${p.host};Port=${p.port};Database=${p.database};` +
    `Username=${p.user};Password=${quoted};SSL Mode=${ssl}`
  );
}

// ---------------------------------------------------------------------------
// Module state (meaningful only when the gate is enabled).
// ---------------------------------------------------------------------------

interface HarnessState {
  adminDsnRaw: string;
  admin: PostgresConnectionConfig;
  adminClient: Client | null;
  executorPassword: string;
  createdDbs: string[];
  executorCreated: boolean;
  tmpDir: string;
  cliOutputs: string[];
  sentinels: string[];
}

const state: HarnessState = {
  adminDsnRaw: '',
  admin: null as unknown as PostgresConnectionConfig,
  adminClient: null,
  executorPassword: '',
  createdDbs: [],
  executorCreated: false,
  tmpDir: '',
  cliOutputs: [],
  sentinels: [],
};

function containsSentinel(text: string): boolean {
  return state.sentinels.some((s) => s.length >= 4 && text.includes(s));
}

function scrub(text: string): string {
  let out = text;
  for (const s of state.sentinels) {
    if (s.length >= 4) out = out.split(s).join('***');
  }
  return out;
}

function executorConfig(database: string): PostgresConnectionConfig {
  return {
    host: state.admin.host,
    port: state.admin.port,
    database,
    user: EXECUTOR,
    password: state.executorPassword,
    ssl: state.admin.ssl,
  };
}

/**
 * Rule 1: CLI children NEVER receive the admin DSN. The control-plane env is
 * always the executor login against the requested CP DB; the tenant DSN is
 * only ever injected through the derived env var (rule 6).
 */
function cliEnv(database: string, withTenantDsn: boolean): NodeJS.ProcessEnv {
  const env: NodeJS.ProcessEnv = { ...process.env };
  delete env[ADMIN_DSN_ENV];
  delete env[INTEGRATION_FLAG];
  env[CONNECTION_STRING_ENV_VAR] = keyValueDsn(executorConfig(database));
  if (withTenantDsn) {
    env[TENANT_ENV_NAME] = keyValueDsn(executorConfig(TENANT_DB));
  }
  return env;
}

interface CliResult {
  code: number;
  stdout: string;
  stderr: string;
}

function runCliSync(args: readonly string[], env: NodeJS.ProcessEnv, timeoutMs: number): CliResult {
  const res: SpawnSyncReturns<string> = spawnSync(process.execPath, [CLI_JS, ...args], {
    cwd: API_ROOT,
    env,
    encoding: 'utf8',
    timeout: timeoutMs,
    maxBuffer: 16 * 1024 * 1024,
    windowsHide: true,
  });
  const stdout = res.stdout ?? '';
  const stderr = res.stderr ?? '';
  state.cliOutputs.push(stdout, stderr);
  if (res.error !== undefined) {
    throw new Error(`migration CLI spawn failed for "${args.join(' ')}": ${res.error.name}`);
  }
  return { code: res.status ?? -1, stdout, stderr };
}

function expectCliOk(stepLabel: string, result: CliResult): void {
  if (result.code !== 0) {
    throw new Error(
      `migration CLI failed for ${stepLabel} (exit ${result.code}):\n` +
        scrub(`${result.stdout}\n${result.stderr}`).slice(-1500),
    );
  }
}

function runCliAsync(
  args: readonly string[],
  env: NodeJS.ProcessEnv,
  timeoutMs: number,
): Promise<CliResult> {
  return new Promise((resolve, reject) => {
    const child = spawn(process.execPath, [CLI_JS, ...args], {
      cwd: API_ROOT,
      env,
      windowsHide: true,
    });
    let stdout = '';
    let stderr = '';
    let settled = false;
    const timer = setTimeout(() => {
      if (!settled) {
        settled = true;
        child.kill('SIGKILL');
        reject(new Error(`migration CLI timed out after ${timeoutMs}ms`));
      }
    }, timeoutMs);
    timer.unref();
    child.stdout?.setEncoding('utf8');
    child.stderr?.setEncoding('utf8');
    child.stdout?.on('data', (chunk: string) => {
      stdout += chunk;
    });
    child.stderr?.on('data', (chunk: string) => {
      stderr += chunk;
    });
    child.on('error', (error) => {
      if (!settled) {
        settled = true;
        clearTimeout(timer);
        reject(new Error(`migration CLI spawn failed: ${error.name}`));
      }
    });
    child.on('close', (code) => {
      if (!settled) {
        settled = true;
        clearTimeout(timer);
        state.cliOutputs.push(stdout, stderr);
        resolve({ code: code ?? -1, stdout, stderr });
      }
    });
  });
}

function readManifest(path: string): Record<string, unknown> {
  expect(existsSync(path)).toBe(true);
  const raw = readFileSync(path, 'utf8');
  // Rule 4/7: manifest artifacts must not embed secrets — boolean only.
  expect(containsSentinel(raw)).toBe(false);
  return JSON.parse(raw) as Record<string, unknown>;
}

function expectManifestHeader(
  artifact: Record<string, unknown>,
  entry: MigrationRegistryEntry,
  command: 'up' | 'verify',
): void {
  expect(artifact.command).toBe(command);
  expect(artifact.file).toBe(entry.file);
  expect(artifact.relativePath).toBe(entry.relativePath);
  expect(artifact.stream).toBe(entry.stream);
  expect(artifact.workId).toBe(entry.workId);
  expect(artifact.sha256).toBe(entry.sha256);
  expect(artifact.bytes).toBe(entry.byteLength);
  expect(artifact.style).toBe(entry.style);
  expect(artifact.policy).toBe(
    entry.legacyPolicy ? 'legacy' : entry.strictPolicy ? 'strict' : 'legacy-strict',
  );
  expect(artifact.readOnly).toBe(command === 'verify');
}

async function withClient<T>(
  config: PostgresConnectionConfig,
  applicationName: string,
  fn: (c: Client) => Promise<T>,
): Promise<T> {
  const c = new Client({
    ...config,
    application_name: applicationName,
    connectionTimeoutMillis: 15_000,
    statement_timeout: 60_000,
    query_timeout: 60_000,
  });
  await c.connect();
  try {
    return await fn(c);
  } finally {
    await c.end().catch(() => undefined);
  }
}

/**
 * Opens an executor connection and immediately switches to the migrator group
 * (the executor is a NOINHERIT member). Every data read/write in this spec
 * runs as the group owner — never as admin, never as a privilege-less
 * executor, and never through the admin DSN.
 */
async function withGroupClient<T>(
  database: string,
  applicationName: string,
  fn: (c: Client) => Promise<T>,
): Promise<T> {
  return withClient(executorConfig(database), applicationName, async (c) => {
    await c.query(`SET ROLE "${GROUP_ROLE}"`);
    try {
      return await fn(c);
    } finally {
      await c.query('RESET ROLE').catch(() => undefined);
    }
  });
}

let savepointCounter = 0;
async function expectViolation(c: Client, sql: string, params?: unknown[]): Promise<void> {
  const sp = `f2w2_sp_${savepointCounter++}`;
  await c.query(`SAVEPOINT ${sp}`);
  let threw = false;
  try {
    await c.query(sql, params);
  } catch {
    threw = true;
  }
  // ROLLBACK TO keeps the fixture transaction alive for later assertions.
  await c.query(`ROLLBACK TO SAVEPOINT ${sp}`);
  expect(threw).toBe(true);
}

async function expectAllowed(c: Client, sql: string, params?: unknown[]): Promise<void> {
  const sp = `f2w2_ok_${savepointCounter++}`;
  await c.query(`SAVEPOINT ${sp}`);
  let threw = false;
  try {
    await c.query(sql, params);
  } catch {
    threw = true;
  }
  if (threw) {
    await c.query(`ROLLBACK TO SAVEPOINT ${sp}`);
  } else {
    // KEEP the positive row: later negative probes reference these ids.
    // Everything is discarded by the fixture transaction's final ROLLBACK.
    await c.query(`RELEASE SAVEPOINT ${sp}`);
  }
  expect(threw).toBe(false);
}

const CP_IDENTITY_TABLES = [
  'lu_site',
  'lu_user',
  'lu_site_membership',
  'lu_session',
  'lu_login_identifier',
  'lu_legacy_user_xref',
  'lu_identity_audit_event',
] as const;

async function snapshotCounts(c: Client): Promise<Record<string, string>> {
  const rows: Record<string, string> = {};
  for (const table of CP_IDENTITY_TABLES) {
    const r = await c.query<{ c: string }>(`SELECT count(*)::text AS c FROM public.${table}`);
    rows[table] = r.rows[0]?.c ?? '0';
  }
  return rows;
}

interface LedgerRow {
  ordinal: number;
  relative_path: string;
  sha256: string;
  bytes: number;
  work_id: string | null;
}

async function fetchLedger(
  database: string,
  stream: 'control-plane' | 'tenant',
): Promise<LedgerRow[]> {
  return withGroupClient(database, 'lu-f2w2w15b-ledger-read', async (c) => {
    const r = await c.query<LedgerRow>(
      `SELECT ordinal, relative_path, sha256, bytes, work_id
         FROM public.lu_migration_history
        WHERE stream = $1 ORDER BY ordinal ASC`,
      [stream],
    );
    return r.rows;
  });
}

/**
 * Rule 9: pins/workIds are asserted against the IMPORTED registry — never
 * against duplicated literals.
 */
function assertLedgerMatchesRegistry(
  rows: readonly LedgerRow[],
  registry: readonly MigrationRegistryEntry[],
): void {
  expect(rows.length).toBe(registry.length);
  rows.forEach((row, index) => {
    const entry = registry[index];
    if (entry === undefined) throw new Error('registry walk error');
    expect(row.ordinal).toBe(entry.ordinal);
    expect(row.relative_path).toBe(entry.relativePath);
    expect(row.sha256.trim()).toBe(entry.sha256);
    expect(row.bytes).toBe(entry.byteLength);
    expect(row.work_id).toBe(entry.workId);
  });
}

/** Rule 6 ACL: group owns everything; runtime owns nothing and cannot CREATE. */
async function assertOwnershipAndAcl(database: string): Promise<void> {
  await withGroupClient(database, 'lu-f2w2w15b-acl', async (c) => {
    const groupCreate = await c.query<{ v: boolean }>(
      `SELECT has_schema_privilege($1, 'public', 'CREATE') AS v`,
      [GROUP_ROLE],
    );
    expect(groupCreate.rows[0]?.v).toBe(true);
    const runtimeCreate = await c.query<{ v: boolean }>(
      `SELECT has_schema_privilege($1, 'public', 'CREATE') AS v`,
      [RUNTIME_ROLE],
    );
    expect(runtimeCreate.rows[0]?.v).toBe(false);
    const runtimeOwned = await c.query<{ c: string }>(
      `SELECT (
         (SELECT count(*) FROM pg_class cls
            JOIN pg_namespace n ON n.oid = cls.relnamespace
            JOIN pg_roles r ON r.oid = cls.relowner
           WHERE n.nspname = 'public' AND r.rolname = $1
             AND cls.relkind IN ('r','p','S','v','m')) +
         (SELECT count(*) FROM pg_proc p
            JOIN pg_namespace n ON n.oid = p.pronamespace
            JOIN pg_roles r ON r.oid = p.proowner
           WHERE n.nspname = 'public' AND r.rolname = $1)
       )::text AS c`,
      [RUNTIME_ROLE],
    );
    expect(runtimeOwned.rows[0]?.c).toBe('0');
    const nonGroupOwned = await c.query<{ c: string }>(
      `SELECT (
         (SELECT count(*) FROM pg_class cls
            JOIN pg_namespace n ON n.oid = cls.relnamespace
            JOIN pg_roles r ON r.oid = cls.relowner
           WHERE n.nspname = 'public' AND r.rolname <> $1
             AND cls.relkind IN ('r','p','S','v','m')) +
         (SELECT count(*) FROM pg_proc p
            JOIN pg_namespace n ON n.oid = p.pronamespace
            JOIN pg_roles r ON r.oid = p.proowner
           WHERE n.nspname = 'public' AND r.rolname <> $1)
       )::text AS c`,
      [GROUP_ROLE],
    );
    expect(nonGroupOwned.rows[0]?.c).toBe('0');
    const routeTable = await c.query<{ c: string }>(
      `SELECT count(*)::text AS c FROM information_schema.tables
        WHERE table_schema='public' AND table_name='lu_tenant_route'`,
    );
    if (routeTable.rows[0]?.c === '1') {
      // 0006 excludes migration_secret_reference from the runtime column list.
      const leakedRefColumn = await c.query<{ c: string }>(
        `SELECT count(*)::text AS c FROM information_schema.role_column_grants
          WHERE table_schema='public' AND table_name='lu_tenant_route'
            AND column_name='migration_secret_reference' AND grantee=$1`,
        [RUNTIME_ROLE],
      );
      expect(leakedRefColumn.rows[0]?.c).toBe('0');
    }
    const historyDml = await c.query<{ c: string }>(
      `SELECT count(*)::text AS c FROM information_schema.role_table_grants
        WHERE table_schema='public' AND table_name='lu_migration_history'
          AND grantee=$1 AND privilege_type <> 'SELECT'`,
      [RUNTIME_ROLE],
    );
    expect(historyDml.rows[0]?.c).toBe('0');
  });
}

// ---------------------------------------------------------------------------
// Gate evaluation at module load (never connects).
// ---------------------------------------------------------------------------

const gateway = resolveIntegration(process.env);

describe('F2-W2/W15B opt-in PostgreSQL integration harness', () => {
  // -------------------------------------------------------------------------
  // Offline specs — always run, zero connections.
  // -------------------------------------------------------------------------
  it('gate contract: refuses wrong flag, missing/malformed DSN, non-loopback and non-neondb targets without echoing credentials', () => {
    const fakePassword = 'FAKEGATESENTINEL9f3a';
    const cases: NodeJS.ProcessEnv[] = [
      {},
      { [INTEGRATION_FLAG]: '0' },
      { [INTEGRATION_FLAG]: '1' },
      {
        [INTEGRATION_FLAG]: '1',
        [ADMIN_DSN_ENV]: `postgresql://gate:fake@db.example.com:5432/${REQUIRED_TARGET_DB}`,
      },
      {
        [INTEGRATION_FLAG]: '1',
        [ADMIN_DSN_ENV]: `Host=127.0.0.1;Port=55432;Database=postgres;Username=gate;Password=${fakePassword};SSL Mode=Disable`,
      },
      {
        [INTEGRATION_FLAG]: '1',
        [ADMIN_DSN_ENV]: `ftp://gate:${fakePassword}@127.0.0.1/db`,
      },
    ];
    for (const env of cases) {
      const resolved = resolveIntegration(env);
      expect(resolved.enabled).toBe(false);
      expect(resolved.admin).toBeNull();
      const rawDsn = env[ADMIN_DSN_ENV];
      expect(
        resolved.reason.includes(fakePassword) ||
          (rawDsn !== undefined && resolved.reason.includes(rawDsn)),
      ).toBe(false);
    }
    // A fully valid loopback/neondb env enables the gate (still no connect).
    const good = resolveIntegration({
      [INTEGRATION_FLAG]: '1',
      [ADMIN_DSN_ENV]: `Host=127.0.0.1;Port=5432;Database=neondb;Username=postgres;Password=${fakePassword};SSL Mode=Disable`,
    });
    expect(good.enabled).toBe(true);
    expect(good.admin?.database).toBe('neondb');
    // The ConnectionStrings fallback is honored too.
    const fallback = resolveIntegration({
      [INTEGRATION_FLAG]: '1',
      [CONNECTION_STRING_ENV_VAR]: `Host=localhost;Port=5432;Database=neondb;Username=postgres;Password=x${fakePassword.slice(1)};SSL Mode=Disable`,
    });
    expect(fallback.enabled).toBe(true);
  });

  it('bcrypt fixtures are deterministic 60-char full-alphabet shapes satisfying the 0006 SQL class and its negatives', () => {
    expect(VALID_BCRYPT_HASH.length).toBe(60);
    expect(INVALID_COST_BCRYPT_SHAPE.length).toBe(60);
    expect(BCRYPT_PAYLOAD_53.length).toBe(53);
    // The payload exercises the FULL bcrypt alphabet, uppercase included.
    expect(BCRYPT_PAYLOAD_53).toMatch(/^[./A-Za-z0-9]{53}$/);
    expect(BCRYPT_PAYLOAD_53).toMatch(/[A-Z]/);
    expect(BCRYPT_SQL_SHAPE.test(VALID_BCRYPT_HASH)).toBe(true);
    expect(BCRYPT_SQL_SHAPE.test(INVALID_COST_BCRYPT_SHAPE)).toBe(false);
    expect(BCRYPT_SQL_SHAPE.test('bcrypt-hash')).toBe(false);
  });

  it('is opt-in and reports its status', () => {
    if (!gateway.enabled) {
      console.log(`[skip] F2-W2 integration disabled: ${gateway.reason}`);
      expect(gateway.enabled).toBe(false);
      expect(gateway.admin).toBeNull();
      return;
    }
    expect(gateway.enabled).toBe(true);
    expect(gateway.admin).not.toBeNull();
    expect(existsSync(CLI_JS)).toBe(true);
  });

  if (gateway.enabled && gateway.admin !== null) {
    // -----------------------------------------------------------------------
    // External bootstrap + teardown (rules 2/3).
    // -----------------------------------------------------------------------
    beforeAll(async () => {
      const admin = gateway.admin!;
      state.admin = admin;
      state.adminDsnRaw =
        process.env[ADMIN_DSN_ENV] ?? process.env[CONNECTION_STRING_ENV_VAR] ?? '';
      state.tmpDir = mkdtempSync(join(tmpdir(), `f2w2w15b-${RUN_ID}-`));
      state.executorPassword = randomBytes(18).toString('base64url');
      state.sentinels = [state.adminDsnRaw, admin.password, state.executorPassword];
      const client = new Client({
        ...admin,
        application_name: 'lu-f2w2w15b-admin-bootstrap',
        connectionTimeoutMillis: 15_000,
        statement_timeout: 120_000,
      });
      try {
        await client.connect();
        state.adminClient = client;

        // (2a) runtime role: must pre-exist and be safe; never created/mutated.
        const runtime = await client.query<{
          rolcanlogin: boolean;
          rolsuper: boolean;
          rolcreaterole: boolean;
          rolcreatedb: boolean;
          rolreplication: boolean;
          rolbypassrls: boolean;
        }>(
          `SELECT rolcanlogin, rolsuper, rolcreaterole, rolcreatedb, rolreplication, rolbypassrls
             FROM pg_roles WHERE rolname = $1`,
          [RUNTIME_ROLE],
        );
        if (runtime.rows.length !== 1) {
          throw new Error(`role "${RUNTIME_ROLE}" is absent; bootstrap requires it to pre-exist`);
        }
        if (!isSafeManagedRole(runtime.rows[0]!)) {
          throw new Error(`role "${RUNTIME_ROLE}" exists but is unsafe; refusing to proceed`);
        }

        // (2b) migrator group: create only when absent; never mutate.
        const group = await client.query<{
          rolcanlogin: boolean;
          rolsuper: boolean;
          rolcreaterole: boolean;
          rolcreatedb: boolean;
          rolreplication: boolean;
          rolbypassrls: boolean;
        }>(
          `SELECT rolcanlogin, rolsuper, rolcreaterole, rolcreatedb, rolreplication, rolbypassrls
             FROM pg_roles WHERE rolname = $1`,
          [GROUP_ROLE],
        );
        if (group.rows.length === 1) {
          if (!isSafeManagedRole(group.rows[0]!)) {
            throw new Error(`role "${GROUP_ROLE}" exists but is unsafe; refusing to mutate it`);
          }
        } else {
          await client.query(
            `CREATE ROLE "${GROUP_ROLE}" WITH NOLOGIN NOSUPERUSER NOCREATEDB ` +
              `NOCREATEROLE NOREPLICATION NOBYPASSRLS`,
          );
        }

        // (2c) unique temporary executor login (NOINHERIT, no elevation).
        await client.query(
          `CREATE ROLE "${EXECUTOR}" WITH LOGIN PASSWORD '${state.executorPassword}' ` +
            `NOINHERIT NOSUPERUSER NOCREATEDB NOCREATEROLE NOREPLICATION NOBYPASSRLS`,
        );
        state.executorCreated = true;
        await client.query(`GRANT "${GROUP_ROLE}" TO "${EXECUTOR}"`);

        // (3) four temp DBs owned by the group + per-DB public-schema hardening.
        for (const db of TEMP_DBS) {
          await client.query(`CREATE DATABASE "${db}" WITH OWNER "${GROUP_ROLE}"`);
          state.createdDbs.push(db);
          await withClient({ ...admin, database: db }, 'lu-f2w2w15b-admin-harden', async (c) => {
            await c.query('REVOKE CREATE ON SCHEMA public FROM PUBLIC');
            await c.query(`REVOKE CREATE ON SCHEMA public FROM "${RUNTIME_ROLE}"`);
            await c.query(`GRANT CREATE ON SCHEMA public TO "${GROUP_ROLE}"`);
          });
        }
        state.sentinels.push(
          keyValueDsn(executorConfig(FRESH_DB)),
          keyValueDsn(executorConfig(TENANT_DB)),
          TENANT_ENV_NAME,
          MIGRATION_REF,
        );
      } catch (error) {
        throw new Error(
          `F2-W2 integration bootstrap failed: ${scrub(error instanceof Error ? error.message : String(error))}`,
          { cause: error },
        );
      }
    }, 180_000);

    afterAll(async () => {
      const failures: string[] = [];
      const admin = state.adminClient;
      try {
        if (admin !== null) {
          try {
            await admin.query(
              `SELECT pg_terminate_backend(pid) FROM pg_stat_activity
                WHERE pid <> pg_backend_pid() AND (usename = $1 OR datname = ANY($2::text[]))`,
              [EXECUTOR, [...TEMP_DBS]],
            );
          } catch {
            failures.push('terminate-backends');
          }
          for (const db of state.createdDbs) {
            try {
              await admin.query(`DROP DATABASE IF EXISTS "${db}" WITH (FORCE)`);
            } catch {
              try {
                await admin.query(`DROP DATABASE IF EXISTS "${db}"`);
              } catch {
                failures.push(`drop-db:${db}`);
              }
            }
          }
          if (state.executorCreated) {
            try {
              await admin.query(`REVOKE "${GROUP_ROLE}" FROM "${EXECUTOR}"`);
            } catch {
              failures.push('revoke-executor-membership');
            }
            try {
              await admin.query(`DROP ROLE IF EXISTS "${EXECUTOR}"`);
            } catch {
              failures.push('drop-executor');
            }
          }
          try {
            const dbResidue = await admin.query<{ c: string }>(
              `SELECT count(*)::text AS c FROM pg_database WHERE datname = ANY($1::text[])`,
              [[...TEMP_DBS]],
            );
            if (dbResidue.rows[0]?.c !== '0') failures.push('temp-db-residue');
            const loginResidue = await admin.query<{ c: string }>(
              `SELECT count(*)::text AS c FROM pg_roles WHERE rolname = $1`,
              [EXECUTOR],
            );
            if (loginResidue.rows[0]?.c !== '0') failures.push('executor-login-residue');
            const groupKept = await admin.query<{ c: string }>(
              `SELECT count(*)::text AS c FROM pg_roles WHERE rolname = $1`,
              [GROUP_ROLE],
            );
            if (groupKept.rows[0]?.c !== '1') failures.push('migrator-group-missing');
          } catch {
            failures.push('residue-assertion');
          }
          await admin.end().catch(() => failures.push('admin-close'));
        }
      } finally {
        state.adminClient = null;
        try {
          if (state.tmpDir !== '') rmSync(state.tmpDir, { recursive: true, force: true });
        } catch {
          failures.push('tmpdir-remove');
        }
      }
      if (failures.length > 0) {
        // Cleanup failure must fail the evidence, never be swallowed (rule 3).
        throw new Error(`F2-W2 integration cleanup incomplete: ${failures.join(', ')}`);
      }
    }, 180_000);

    // -----------------------------------------------------------------------
    // (4) Fresh control plane.
    // -----------------------------------------------------------------------
    it('fresh CP: CLI applies 0001..0006, ledger stamps every pin, old+latest pins verify and replay-skip, invariants exercise under SAVEPOINTs and the rolled-back fixture transaction leaves baseline counts', async () => {
      const cpEntries = [...CONTROL_PLANE_REGISTRY];
      for (const entry of cpEntries) {
        const manifestPath = join(state.tmpDir, `fresh-${entry.ordinal}.up.json`);
        const result = runCliSync(
          ['up', entry.file, '--stream', 'control-plane', '--manifest-out', manifestPath],
          cliEnv(FRESH_DB, false),
          120_000,
        );
        expectCliOk(`fresh up ${entry.relativePath}`, result);
        const artifact = readManifest(manifestPath);
        expectManifestHeader(artifact, entry, 'up');
        expect(artifact.executed).toBe(true);
        expect(artifact.committed).toBe(true);
        expect(artifact.skipped).toBe(false);
        expect((artifact.verification as { passed: boolean }).passed).toBe(true);
        expect((artifact.server as { database: string }).database).toBe(FRESH_DB);
      }

      // Ledger: contiguous 1..6 with exactly the imported registry pins.
      assertLedgerMatchesRegistry(await fetchLedger(FRESH_DB, 'control-plane'), cpEntries);

      // Old + latest pin: verify, then replay (skip via pin-matches-history).
      for (const ordinal of [1, 6]) {
        const entry = cpEntries[ordinal - 1]!;
        const verifyManifest = join(state.tmpDir, `fresh-${ordinal}.verify.json`);
        expectCliOk(
          `fresh verify ${entry.relativePath}`,
          runCliSync(
            ['verify', entry.file, '--stream', 'control-plane', '--manifest-out', verifyManifest],
            cliEnv(FRESH_DB, false),
            120_000,
          ),
        );
        const va = readManifest(verifyManifest);
        expectManifestHeader(va, entry, 'verify');
        expect((va.verification as { passed: boolean }).passed).toBe(true);

        const replayManifest = join(state.tmpDir, `fresh-${ordinal}.replay.json`);
        expectCliOk(
          `fresh replay ${entry.relativePath}`,
          runCliSync(
            ['up', entry.file, '--stream', 'control-plane', '--manifest-out', replayManifest],
            cliEnv(FRESH_DB, false),
            120_000,
          ),
        );
        const ra = readManifest(replayManifest);
        expect(ra.skipped).toBe(true);
        expect(ra.committed).toBe(true);
        expect(ra.executed).toBe(false);
      }
      assertLedgerMatchesRegistry(await fetchLedger(FRESH_DB, 'control-plane'), cpEntries);

      await assertOwnershipAndAcl(FRESH_DB);
      await withGroupClient(FRESH_DB, 'lu-f2w2w15b-acl-extra', async (c) => {
        const userIns = await c.query<{ c: string }>(
          `SELECT count(*)::text AS c FROM information_schema.role_table_grants
              WHERE table_schema='public' AND table_name='lu_user' AND grantee=$1
                AND privilege_type='INSERT'`,
          [RUNTIME_ROLE],
        );
        expect(userIns.rows[0]?.c).toBe('0');
        const userSel = await c.query<{ c: string }>(
          `SELECT count(*)::text AS c FROM information_schema.role_table_grants
              WHERE table_schema='public' AND table_name='lu_user' AND grantee=$1
                AND privilege_type='SELECT'`,
          [RUNTIME_ROLE],
        );
        expect(userSel.rows[0]?.c).toBe('1');
        const claimsDml = await c.query<{ c: string }>(
          `SELECT count(*)::text AS c FROM information_schema.role_table_grants
              WHERE table_schema='public' AND table_name='lu_login_identifier'
                AND grantee=$1 AND privilege_type <> 'SELECT'`,
          [RUNTIME_ROLE],
        );
        expect(claimsDml.rows[0]?.c).toBe('0');
      });

      // Fixture transaction: full invariant exercise, rolled back at the end.
      const baseline = await withGroupClient(FRESH_DB, 'lu-f2w2w15b-baseline', (c) =>
        snapshotCounts(c),
      );
      expect(Object.values(baseline).every((v) => v === '0')).toBe(true);

      await withGroupClient(FRESH_DB, 'lu-f2w2w15b-fixture-fresh', async (c) => {
        await c.query('BEGIN');
        await c.query(`SET LOCAL ROLE "${GROUP_ROLE}"`);
        try {
          // pending user with a bcrypt-scheme hash -------------------------
          const pendingId = randomUUID();
          await expectAllowed(
            c,
            `INSERT INTO public.lu_user
                 (id, email, full_name, password_hash, password_scheme,
                  must_change_password, row_version, account_status, status,
                  reconciliation_state)
               VALUES ($1,$2,$3,$4,'bcrypt',false,DEFAULT,'active','active',
                       'pending_reconciliation')`,
            [
              pendingId,
              'pending.f2w2@example.invalid',
              'Pending   Original Name',
              VALID_BCRYPT_HASH,
            ],
          );
          const pendingClaims = await c.query<{ kind: string }>(
            `SELECT kind FROM public.lu_login_identifier WHERE user_id=$1 ORDER BY kind`,
            [pendingId],
          );
          expect(pendingClaims.rows.map((r) => r.kind)).toEqual(['email']);

          // canonical user: full_name derivation + normalized claims -------
          const canonId = randomUUID();
          await expectAllowed(
            c,
            `INSERT INTO public.lu_user
                 (id, email, full_name, username, first_name, last_name,
                  second_last_name, identity_card, phone_number,
                  password_hash, password_scheme, must_change_password,
                  row_version, account_status, status, reconciliation_state)
               VALUES ($1,$2,$3,$4,$5,$6,$7,$8,$9,$10,'bcrypt',false,DEFAULT,
                       'active','active','canonical')`,
            [
              canonId,
              '  Canonical.User@Example.Invalid ',
              'ignored',
              'canonical.user',
              'Cano ',
              ' Nical',
              'Reales',
              'z-9876',
              '+591 700-12345',
              VALID_BCRYPT_HASH,
            ],
          );
          const canon = await c.query<{
            full_name: string;
            email: string;
            identity_card: string;
          }>(`SELECT full_name, email, identity_card FROM public.lu_user WHERE id=$1`, [canonId]);
          expect(canon.rows[0]?.full_name).toBe('Cano Nical Reales');
          expect(canon.rows[0]?.email).toBe('canonical.user@example.invalid');
          expect(canon.rows[0]?.identity_card).toBe('Z-9876');
          const canonClaims = await c.query<{ kind: string; v: string }>(
            `SELECT kind, normalized_value AS v FROM public.lu_login_identifier
                WHERE user_id=$1 ORDER BY kind`,
            [canonId],
          );
          expect(canonClaims.rows.map((r) => r.kind)).toEqual(['email', 'username']);
          expect(canonClaims.rows[0]?.v).toBe('canonical.user@example.invalid');
          expect(canonClaims.rows[1]?.v).toBe('canonical.user');

          // status mapping disabled -> inactive through the shadow rule ----
          const mappedId = randomUUID();
          await expectAllowed(
            c,
            `INSERT INTO public.lu_user
                 (id, email, full_name, password_hash, password_scheme,
                  must_change_password, status, reconciliation_state)
               VALUES ($1,$2,$3,$4,'bcrypt',false,'disabled','pending_reconciliation')`,
            [mappedId, 'mapped.f2w2@example.invalid', 'Mapped Disabled', VALID_BCRYPT_HASH],
          );
          const mapped = await c.query<{ account_status: string; status: string }>(
            `SELECT account_status, status FROM public.lu_user WHERE id=$1`,
            [mappedId],
          );
          expect(mapped.rows[0]?.account_status).toBe('inactive');
          expect(mapped.rows[0]?.status).toBe('inactive');

          // negatives, each isolated with a SAVEPOINT -----------------------
          await expectViolation(
            c,
            `INSERT INTO public.lu_user
                 (id, email, full_name, password_hash, password_scheme,
                  must_change_password, account_status, status, reconciliation_state)
               VALUES ($1,$2,$3,$4,'bcrypt',false,'deleted','disabled','pending_reconciliation')`,
            [randomUUID(), 'conflict.f2w2@example.invalid', 'Conflict', VALID_BCRYPT_HASH],
          ); // status/account_status conflict on insert
          await expectViolation(
            c,
            `INSERT INTO public.lu_user
                 (id, email, full_name, password_hash, password_scheme,
                  must_change_password, account_status, status, reconciliation_state)
               VALUES ($1,$2,$3,$4,'bcrypt',false,'active','active','pending_reconciliation')`,
            [randomUUID(), 'pending.f2w2@example.invalid', 'Dup Email', VALID_BCRYPT_HASH],
          ); // lower(email) uniqueness
          await expectViolation(
            c,
            `INSERT INTO public.lu_user
                 (id, email, full_name, password_hash, password_scheme,
                  must_change_password, account_status, status, identity_card,
                  reconciliation_state)
               VALUES ($1,$2,$3,$4,'bcrypt',false,'active','active','Z-9876','pending_reconciliation')`,
            [randomUUID(), 'card2.f2w2@example.invalid', 'Dup Card', VALID_BCRYPT_HASH],
          ); // identity_card CI uniqueness
          await expectViolation(
            c,
            `INSERT INTO public.lu_user
                 (id, email, full_name, password_hash, password_scheme,
                  must_change_password, account_status, status, row_version,
                  reconciliation_state)
               VALUES ($1,$2,$3,$4,'bcrypt',false,'active','active',5,'pending_reconciliation')`,
            [randomUUID(), 'rowver.f2w2@example.invalid', 'Bad RowVer', VALID_BCRYPT_HASH],
          ); // row_version must start at zero
          await expectViolation(
            c,
            `INSERT INTO public.lu_user
                 (id, email, full_name, password_hash, password_scheme,
                  must_change_password, account_status, status, reconciliation_state)
               VALUES ($1,$2,$3,$4,'bcrypt',false,'active','active','pending_reconciliation')`,
            [randomUUID(), 'hash.f2w2@example.invalid', 'Bad Scheme Hash', 'bcrypt-hash'],
          ); // bcrypt scheme with malformed hash (the old fake literal)
          await expectViolation(
            c,
            `INSERT INTO public.lu_user
                 (id, email, full_name, password_hash, password_scheme,
                  must_change_password, account_status, status, phone_number,
                  reconciliation_state)
               VALUES ($1,$2,$3,$4,'bcrypt',false,'active','active','+591abc','pending_reconciliation')`,
            [randomUUID(), 'phone.f2w2@example.invalid', 'Bad Phone', VALID_BCRYPT_HASH],
          ); // phone charset
          await expectViolation(
            c,
            `INSERT INTO public.lu_user
                 (id, email, full_name, password_hash, password_scheme,
                  must_change_password, account_status, status, identity_card,
                  reconciliation_state)
               VALUES ($1,$2,$3,$4,'bcrypt',false,'active','active','bad card!','pending_reconciliation')`,
            [randomUUID(), 'card3.f2w2@example.invalid', 'Bad Card', VALID_BCRYPT_HASH],
          ); // identity card format
          await expectViolation(
            c,
            `INSERT INTO public.lu_user
                 (id, email, full_name, password_hash, password_scheme,
                  must_change_password, account_status, status,
                  reconciliation_state)
               VALUES ($1,$2,$3,'legacy_identity_v2',false,'active','active','canonical')`,
            [randomUUID(), 'legacy.f2w2@example.invalid', 'Legacy Canonical'],
          ); // canonical without required fields
          await expectViolation(
            c,
            `INSERT INTO public.lu_login_identifier
                 (id, user_id, kind, normalized_value)
               VALUES ($1,$2,'username','Upper.Case@Example.Invalid')`,
            [randomUUID(), pendingId],
          ); // claim value must equal its own normalization
          await expectViolation(
            c,
            `UPDATE public.lu_user SET reconciliation_state='pending_reconciliation' WHERE id=$1`,
            [canonId],
          ); // canonical -> pending forbidden
          await expectViolation(c, `UPDATE public.lu_user SET username='renamed' WHERE id=$1`, [
            canonId,
          ]); // canonical username immutable
          await expectViolation(c, `UPDATE public.lu_user SET row_version=9 WHERE id=$1`, [
            canonId,
          ]); // row_version server-managed
          await expectViolation(c, `DELETE FROM public.lu_user WHERE id=$1`, [pendingId]); // identity hard delete forbidden
          await expectViolation(
            c,
            `UPDATE public.lu_user SET password_scheme='reset_required', password_hash=$2 WHERE id=$1`,
            [pendingId, VALID_BCRYPT_HASH],
          ); // reset_required must carry a NULL hash
          await expectViolation(
            c,
            `UPDATE public.lu_user SET password_scheme='legacy_identity_v2', must_change_password=true WHERE id=$1`,
            [pendingId],
          ); // legacy schemes force must_change=false

          // audit append-only ----------------------------------------------
          await expectAllowed(
            c,
            `INSERT INTO public.lu_identity_audit_event
                 (id, action, subject_user_id, actor_user_id, before_status, after_status, metadata)
               VALUES ($1,'role_change',$2,$3,'active','inactive','{}'::jsonb)`,
            [randomUUID(), pendingId, pendingId],
          );
          await expectViolation(
            c,
            `UPDATE public.lu_identity_audit_event SET reason='tamper' WHERE subject_user_id=$1`,
            [pendingId],
          );
          await expectViolation(
            c,
            `DELETE FROM public.lu_identity_audit_event WHERE subject_user_id=$1`,
            [pendingId],
          );
          await expectViolation(
            c,
            `INSERT INTO public.lu_identity_audit_event
                 (id, action, subject_user_id, before_status)
               VALUES ($1,'x',$2,'weird-status')`,
            [randomUUID(), pendingId],
          );

          // xref uniqueness + immutability ----------------------------------
          await expectAllowed(
            c,
            `INSERT INTO public.lu_legacy_user_xref
                 (id, source_system, legacy_user_id, user_id, migration_run_id, source_fingerprint)
               VALUES ($1,'asp_sqlserver',4242,$2,'run-f2w2','fp-f2w2')`,
            [randomUUID(), pendingId],
          );
          await expectViolation(
            c,
            `INSERT INTO public.lu_legacy_user_xref
                 (id, source_system, legacy_user_id, user_id, migration_run_id, source_fingerprint)
               VALUES ($1,'asp_sqlserver',4242,$2,'run-f2w2','fp-f2w2')`,
            [randomUUID(), canonId],
          ); // (source_system, legacy_user_id) unique
          await expectViolation(
            c,
            `INSERT INTO public.lu_legacy_user_xref
                 (id, source_system, legacy_user_id, user_id, migration_run_id, source_fingerprint)
               VALUES ($1,'asp_sqlserver',99,$2,'run-f2w2','fp-f2w2')`,
            [randomUUID(), pendingId],
          ); // (source_system, user_id) unique
          await expectViolation(
            c,
            `UPDATE public.lu_legacy_user_xref SET legacy_user_id=1 WHERE user_id=$1`,
            [pendingId],
          );
          await expectViolation(c, `DELETE FROM public.lu_legacy_user_xref WHERE user_id=$1`, [
            pendingId,
          ]);

          // session purpose rules -------------------------------------------
          await expectAllowed(
            c,
            `INSERT INTO public.lu_session
                 (id, token_hash, user_id, security_version, purpose,
                  idle_expires_at, absolute_expires_at)
               VALUES ($1,$2,$3,0,'password_change',
                       CURRENT_TIMESTAMP + INTERVAL '30 minutes',
                       CURRENT_TIMESTAMP + INTERVAL '24 hours')`,
            [randomUUID(), 'a'.repeat(64), pendingId],
          );
          await expectViolation(
            c,
            `INSERT INTO public.lu_session
                 (id, token_hash, user_id, security_version, purpose,
                  idle_expires_at, absolute_expires_at)
               VALUES ($1,$2,$3,0,'evil-purpose',
                       CURRENT_TIMESTAMP + INTERVAL '30 minutes',
                       CURRENT_TIMESTAMP + INTERVAL '24 hours')`,
            [randomUUID(), 'b'.repeat(64), pendingId],
          );

          // ledger immutability ---------------------------------------------
          await expectViolation(
            c,
            `INSERT INTO public.lu_migration_history
                 (stream, ordinal, relative_path, sha256, bytes, work_id)
               VALUES ('control-plane', 1, 'dup.sql', $1, 10, 'x')`,
            ['b'.repeat(64)],
          ); // PK (stream, ordinal)
          await expectViolation(
            c,
            `UPDATE public.lu_migration_history SET work_id='x' WHERE stream='control-plane' AND ordinal=1`,
          );
          await expectViolation(
            c,
            `DELETE FROM public.lu_migration_history WHERE stream='control-plane' AND ordinal=1`,
          );
        } finally {
          await c.query('ROLLBACK');
        }
        // Rule 4: rolled-back fixture transaction leaves baseline counts.
        expect(await snapshotCounts(c)).toEqual(baseline);
      });
    }, 420_000);

    // -----------------------------------------------------------------------
    // (5) Upgrade control plane.
    // -----------------------------------------------------------------------
    it('upgrade CP: apply0001, seed valid-bcrypt + malformed/invalid-cost pre-F2 rows, apply0002..0006; classification, disabled->inactive, pending reconciliation, full_name and session purpose hold; verify+replay preserve every seeded byte', async () => {
      const cpEntries = [...CONTROL_PLANE_REGISTRY];
      expectCliOk(
        'upgrade up 0001',
        runCliSync(
          [
            'up',
            cpEntries[0]!.file,
            '--stream',
            'control-plane',
            '--manifest-out',
            join(state.tmpDir, 'upgrade-1.up.json'),
          ],
          cliEnv(UPGRADE_DB, false),
          120_000,
        ),
      );

      // Seed pre-F2 rows AS THE GROUP (the admin DSN never writes data).
      const validId = randomUUID();
      const malformedId = randomUUID();
      const badcostId = randomUUID();
      const legacyFullName = 'Legacy   User\tOriginal ';
      await withGroupClient(UPGRADE_DB, 'lu-f2w2w15b-seed', async (c) => {
        await c.query('BEGIN');
        await c.query(`SET LOCAL ROLE "${GROUP_ROLE}"`);
        try {
          const siteId = randomUUID();
          await c.query(
            `INSERT INTO public.lu_site (id, code, name, status) VALUES ($1,'f2w2-legacy','Legacy Site','active')`,
            [siteId],
          );
          await c.query(
            `INSERT INTO public.lu_user (id, email, full_name, password_hash, is_super_admin, status, security_version)
               VALUES ($1,'legacy.valid@example.invalid','Valid Legacy',$2,false,'active',0)`,
            [validId, VALID_BCRYPT_HASH],
          );
          await c.query(
            `INSERT INTO public.lu_user (id, email, full_name, password_hash, is_super_admin, status, security_version)
               VALUES ($1,'legacy.malformed@example.invalid',$2,'not-a-bcrypt-hash',false,'disabled',0)`,
            [malformedId, legacyFullName],
          );
          await c.query(
            `INSERT INTO public.lu_user (id, email, full_name, password_hash, is_super_admin, status, security_version)
               VALUES ($1,'legacy.badcost@example.invalid','Bad Cost Legacy',$2,false,'active',0)`,
            [badcostId, INVALID_COST_BCRYPT_SHAPE],
          );
          await c.query(
            `INSERT INTO public.lu_site_membership (user_id, site_id, role, status) VALUES ($1,$2,'Administrador','active')`,
            [malformedId, siteId],
          );
          await c.query(
            `INSERT INTO public.lu_session
                 (id, token_hash, user_id, security_version, last_seen_at, idle_expires_at, absolute_expires_at)
               VALUES ($1,$2,$3,0,CURRENT_TIMESTAMP,CURRENT_TIMESTAMP + INTERVAL '30 minutes',CURRENT_TIMESTAMP + INTERVAL '24 hours')`,
            [randomUUID(), 'c'.repeat(64), validId],
          );
          await c.query('COMMIT');
        } catch (error) {
          await c.query('ROLLBACK');
          throw error;
        }
      });

      for (const entry of cpEntries.slice(1)) {
        expectCliOk(
          `upgrade up ${entry.relativePath}`,
          runCliSync(
            [
              'up',
              entry.file,
              '--stream',
              'control-plane',
              '--manifest-out',
              join(state.tmpDir, `upgrade-${entry.ordinal}.up.json`),
            ],
            cliEnv(UPGRADE_DB, false),
            120_000,
          ),
        );
      }
      assertLedgerMatchesRegistry(await fetchLedger(UPGRADE_DB, 'control-plane'), cpEntries);

      await withGroupClient(UPGRADE_DB, 'lu-f2w2w15b-assert-upgrade', async (c) => {
        const rows = await c.query<{
          email: string;
          full_name: string;
          password_hash: string | null;
          password_scheme: string;
          must_change_password: boolean;
          account_status: string;
          status: string;
          reconciliation_state: string;
        }>(
          `SELECT lower(email) AS email, full_name, password_hash, password_scheme,
                    must_change_password, account_status, status, reconciliation_state
               FROM public.lu_user ORDER BY email`,
        );
        expect(rows.rows.length).toBe(3); // no production/legacy source loaded
        const byEmail = new Map(rows.rows.map((x) => [x.email, x]));
        const valid = byEmail.get('legacy.valid@example.invalid');
        const malformed = byEmail.get('legacy.malformed@example.invalid');
        const badcost = byEmail.get('legacy.badcost@example.invalid');
        expect(valid?.password_hash).toBe(VALID_BCRYPT_HASH); // byte-for-byte
        expect(valid?.password_scheme).toBe('bcrypt');
        expect(valid?.must_change_password).toBe(false);
        expect(valid?.account_status).toBe('active');
        expect(malformed?.password_hash).toBeNull();
        expect(malformed?.password_scheme).toBe('reset_required');
        expect(malformed?.must_change_password).toBe(true);
        expect(malformed?.account_status).toBe('inactive'); // disabled -> inactive
        expect(malformed?.status).toBe('inactive'); // shadow rewritten
        expect(badcost?.password_hash).toBeNull();
        expect(badcost?.password_scheme).toBe('reset_required');
        expect(badcost?.must_change_password).toBe(true);
        expect(badcost?.account_status).toBe('active');
        for (const row of [valid, malformed, badcost]) {
          expect(row?.reconciliation_state).toBe('pending_reconciliation');
        }
        expect(malformed?.full_name).toBe(legacyFullName); // original bytes kept
        const purpose = await c.query<{ purpose: string }>(
          `SELECT purpose FROM public.lu_session LIMIT 1`,
        );
        expect(purpose.rows[0]?.purpose).toBe('normal');
        const claims = await c.query<{ c: string }>(
          `SELECT count(*)::text AS c FROM public.lu_login_identifier`,
        );
        expect(claims.rows[0]?.c).toBe('3');
      });

      // Verify + replay old and latest pins: seeded data must be untouched.
      for (const ordinal of [1, 6]) {
        const entry = cpEntries[ordinal - 1]!;
        expectCliOk(
          `upgrade verify ${entry.relativePath}`,
          runCliSync(
            [
              'verify',
              entry.file,
              '--stream',
              'control-plane',
              '--manifest-out',
              join(state.tmpDir, `upgrade-${ordinal}.verify.json`),
            ],
            cliEnv(UPGRADE_DB, false),
            120_000,
          ),
        );
        expectCliOk(
          `upgrade replay ${entry.relativePath}`,
          runCliSync(
            [
              'up',
              entry.file,
              '--stream',
              'control-plane',
              '--manifest-out',
              join(state.tmpDir, `upgrade-${ordinal}.replay.json`),
            ],
            cliEnv(UPGRADE_DB, false),
            120_000,
          ),
        );
        expect(readManifest(join(state.tmpDir, `upgrade-${ordinal}.replay.json`)).skipped).toBe(
          true,
        );
      }
      await withGroupClient(UPGRADE_DB, 'lu-f2w2w15b-post-replay', async (c) => {
        const valid = await c.query<{ hash: string | null }>(
          `SELECT password_hash AS hash FROM public.lu_user WHERE lower(email)='legacy.valid@example.invalid'`,
        );
        expect(valid.rows[0]?.hash).toBe(VALID_BCRYPT_HASH);
        const malformed = await c.query<{ full_name: string }>(
          `SELECT full_name FROM public.lu_user WHERE lower(email)='legacy.malformed@example.invalid'`,
        );
        expect(malformed.rows[0]?.full_name).toBe(legacyFullName);
        const membership = await c.query<{ c: string }>(
          `SELECT count(*)::text AS c FROM public.lu_site_membership`,
        );
        expect(membership.rows[0]?.c).toBe('1');
      });
      await assertOwnershipAndAcl(UPGRADE_DB);
    }, 420_000);

    // -----------------------------------------------------------------------
    // (6) Router + tenant streams through the CLI lease.
    // -----------------------------------------------------------------------
    it('router+tenant: CP ladder on the router DB; tenant 0001..0013 sequential through `up --stream tenant --site`; monotonic schema_version per lease; after 13 the ledger is exactly contiguous 1..13 pinned; old+latest tenant pins verify and replay-skip; append-only SAVEPOINTs; concurrent replay race tolerated; ACL ownership proven', async () => {
      const cpEntries = [...CONTROL_PLANE_REGISTRY];
      for (const entry of cpEntries) {
        expectCliOk(
          `router up ${entry.relativePath}`,
          runCliSync(
            [
              'up',
              entry.file,
              '--stream',
              'control-plane',
              '--manifest-out',
              join(state.tmpDir, `router-${entry.ordinal}.up.json`),
            ],
            cliEnv(ROUTER_DB, false),
            120_000,
          ),
        );
      }

      const siteId = randomUUID();
      await withGroupClient(ROUTER_DB, 'lu-f2w2w15b-seed-route', async (c) => {
        await c.query('BEGIN');
        await c.query(`SET LOCAL ROLE "${GROUP_ROLE}"`);
        try {
          await c.query(
            `INSERT INTO public.lu_site (id, code, name, status) VALUES ($1,'f2w2-tenant','Tenant Site','active')`,
            [siteId],
          );
          await c.query(
            `INSERT INTO public.lu_tenant_route
                 (site_id, runtime_secret_reference, writer_label, state, schema_version, migration_secret_reference)
               VALUES ($1,$2,'NEST_SQLSERVER','migrating',0,$3)`,
            [siteId, `f2w2-runtime-${RUN_ID}`, MIGRATION_REF],
          );
          await c.query('COMMIT');
        } catch (error) {
          await c.query('ROLLBACK');
          throw error;
        }
      });

      const readRoute = (): Promise<{
        schema_version: number;
        last_health_at: string | null;
        state: string;
        writer_label: string;
        ref: string | null;
      }> =>
        withGroupClient(ROUTER_DB, 'lu-f2w2w15b-read-route', async (c) => {
          const r = await c.query<{
            schema_version: number;
            last_health_at: string | null;
            state: string;
            writer_label: string;
            ref: string | null;
          }>(
            `SELECT schema_version, last_health_at, state, writer_label,
                      migration_secret_reference AS ref
                 FROM public.lu_tenant_route WHERE site_id=$1`,
            [siteId],
          );
          return r.rows[0]!;
        });

      // Fail-closed: tenant up without the injected env var must not
      // advance anything and must not leak the reference/env name.
      const failPath = join(state.tmpDir, 'fail-noenv.json');
      const noEnv = runCliSync(
        [
          'up',
          '0001_dashboard_foundation.sql',
          '--stream',
          'tenant',
          '--site',
          siteId,
          '--manifest-out',
          failPath,
        ],
        cliEnv(ROUTER_DB, false),
        120_000,
      );
      expect(noEnv.code !== 0).toBe(true);
      expect(existsSync(failPath)).toBe(false);
      expect(containsSentinel(`${noEnv.stdout}\n${noEnv.stderr}`)).toBe(false);
      expect((await readRoute()).schema_version).toBe(0);

      // Stream mix-up must fail before any DB work.
      expect(
        runCliSync(
          ['up', '0001_dashboard_foundation.sql', '--stream', 'control-plane'],
          cliEnv(ROUTER_DB, false),
          120_000,
        ).code !== 0,
      ).toBe(true);

      // Sequential tenant 0001..0013 through the CLI lease.
      const tenantEntries = [...TENANT_REGISTRY];
      expect(tenantEntries.length).toBe(13);
      for (const entry of tenantEntries) {
        const manifestPath = join(state.tmpDir, `tenant-${entry.ordinal}.up.json`);
        expectCliOk(
          `tenant up ${entry.relativePath}`,
          runCliSync(
            [
              'up',
              entry.file,
              '--stream',
              'tenant',
              '--site',
              siteId,
              '--manifest-out',
              manifestPath,
            ],
            cliEnv(ROUTER_DB, true),
            120_000,
          ),
        );
        const artifact = readManifest(manifestPath);
        expectManifestHeader(artifact, entry, 'up');
        expect((artifact.verification as { passed: boolean }).passed).toBe(true);
        expect((artifact.site as { siteId: string }).siteId).toBe(siteId);
        expect(String(artifact.target).includes(TENANT_DB)).toBe(true);
        const route = await readRoute();
        expect(route.schema_version).toBe(entry.ordinal); // GREATEST → exact
        expect(route.last_health_at === null).toBe(false); // health stamped
        expect(route.state).toBe('migrating'); // lease never mutates these
        expect(route.writer_label).toBe('NEST_SQLSERVER');
        expect(route.ref).toBe(MIGRATION_REF);
      }

      // Ledger: contiguous 1..13 with the exact imported pins/workIds.
      assertLedgerMatchesRegistry(await fetchLedger(TENANT_DB, 'tenant'), tenantEntries);

      // Old + latest tenant pins: verify + replay-skip.
      for (const ordinal of [1, 13]) {
        const entry = tenantEntries[ordinal - 1]!;
        const verifyPath = join(state.tmpDir, `tenant-${ordinal}.verify.json`);
        expectCliOk(
          `tenant verify ${entry.relativePath}`,
          runCliSync(
            [
              'verify',
              entry.file,
              '--stream',
              'tenant',
              '--site',
              siteId,
              '--manifest-out',
              verifyPath,
            ],
            cliEnv(ROUTER_DB, true),
            120_000,
          ),
        );
        const va = readManifest(verifyPath);
        expectManifestHeader(va, entry, 'verify');
        expect((va.verification as { passed: boolean }).passed).toBe(true);

        const replayPath = join(state.tmpDir, `tenant-${ordinal}.replay.json`);
        expectCliOk(
          `tenant replay ${entry.relativePath}`,
          runCliSync(
            [
              'up',
              entry.file,
              '--stream',
              'tenant',
              '--site',
              siteId,
              '--manifest-out',
              replayPath,
            ],
            cliEnv(ROUTER_DB, true),
            120_000,
          ),
        );
        expect(readManifest(replayPath).skipped).toBe(true);
      }

      // Final manifest is field-exact against the 0013 registry pin.
      const lastEntry = tenantEntries[12]!;
      const finalManifest = readManifest(join(state.tmpDir, 'tenant-13.verify.json'));
      expect(finalManifest.relativePath).toBe(lastEntry.relativePath);
      expect(finalManifest.sha256).toBe(lastEntry.sha256);
      expect(finalManifest.bytes).toBe(lastEntry.byteLength);
      expect(finalManifest.workId).toBe(lastEntry.workId);
      expect(finalManifest.policy).toBe('strict');
      expect((finalManifest.server as { database: string }).database).toBe(TENANT_DB);
      expect((await readRoute()).schema_version).toBe(13);
      expect((await readRoute()).state).toBe('migrating');

      // Concurrent replay race of the same ordinal: serialized by the
      // global lease advisory lock; route stays 13; ledger untouched.
      const race = await Promise.all([
        runCliAsync(
          [
            'up',
            '0006_management_operations.sql',
            '--stream',
            'tenant',
            '--site',
            siteId,
            '--manifest-out',
            join(state.tmpDir, 'race-a.json'),
          ],
          cliEnv(ROUTER_DB, true),
          120_000,
        ),
        runCliAsync(
          [
            'up',
            '0006_management_operations.sql',
            '--stream',
            'tenant',
            '--site',
            siteId,
            '--manifest-out',
            join(state.tmpDir, 'race-b.json'),
          ],
          cliEnv(ROUTER_DB, true),
          120_000,
        ),
      ]);
      // Surface the scrubbed output of any losing racer before the code check.
      race.forEach((result, i) =>
        expectCliOk(`concurrent replay race ${i === 0 ? 'a' : 'b'}`, result),
      );
      expect(race.map((x) => x.code)).toEqual([0, 0]);
      expect((await readRoute()).schema_version).toBe(13);
      assertLedgerMatchesRegistry(await fetchLedger(TENANT_DB, 'tenant'), tenantEntries);

      // Append-only ledger probes on the tenant DB (rolled back).
      await withGroupClient(TENANT_DB, 'lu-f2w2w15b-ledger-probes', async (c) => {
        const before = await c.query<{ c: string }>(
          `SELECT count(*)::text AS c FROM public.lu_migration_history WHERE stream='tenant'`,
        );
        expect(before.rows[0]?.c).toBe('13');
        await c.query('BEGIN');
        await c.query(`SET LOCAL ROLE "${GROUP_ROLE}"`);
        try {
          // Positive: a future ordinal with bytes > 0 may be appended.
          await expectAllowed(
            c,
            `INSERT INTO public.lu_migration_history
                 (stream, ordinal, relative_path, sha256, bytes, work_id)
               VALUES ('tenant', 14, 'tenant/0014_placeholder.sql', $1, 1024, 'F2-W2-INTEGRATION')`,
            ['d'.repeat(64)],
          );
          await expectViolation(
            c,
            `UPDATE public.lu_migration_history SET work_id='tamper' WHERE stream='tenant' AND ordinal=13`,
          );
          await expectViolation(
            c,
            `DELETE FROM public.lu_migration_history WHERE stream='tenant' AND ordinal=13`,
          );
          await expectViolation(
            c,
            `INSERT INTO public.lu_migration_history
                 (stream, ordinal, relative_path, sha256, bytes, work_id)
               VALUES ('tenant', 13, 'dup.sql', $1, 10, 'x')`,
            ['e'.repeat(64)],
          ); // PK duplicate
          await expectViolation(
            c,
            `INSERT INTO public.lu_migration_history
                 (stream, ordinal, relative_path, sha256, bytes, work_id)
               VALUES ('tenant', 15, 'zero.sql', $1, 0, 'x')`,
            ['f'.repeat(64)],
          ); // bytes must be > 0
        } finally {
          await c.query('ROLLBACK');
        }
        const still = await c.query<{ c: string }>(
          `SELECT count(*)::text AS c FROM public.lu_migration_history WHERE stream='tenant'`,
        );
        expect(still.rows[0]?.c).toBe('13');
      });

      await assertOwnershipAndAcl(TENANT_DB);
    }, 600_000);

    // -----------------------------------------------------------------------
    // (7) Rollback-to-zero-residue proof + leak audit.
    // -----------------------------------------------------------------------
    it('rollback proof: a deliberate invalid fixture transaction leaves zero residue on the fresh CP DB (baseline counts unchanged)', async () => {
      await withGroupClient(FRESH_DB, 'lu-f2w2w15b-rollback-proof', async (c) => {
        const before = await snapshotCounts(c);
        await c.query('BEGIN');
        await c.query(`SET LOCAL ROLE "${GROUP_ROLE}"`);
        let failed = false;
        try {
          await c.query(
            `INSERT INTO public.lu_site (id, code, name) VALUES ($1,'residue-a','Residue Site A')`,
            [randomUUID()],
          );
          // Deliberate contract violation: canonical row without required
          // fields. No SAVEPOINT — the whole transaction must abort.
          await c.query(
            `INSERT INTO public.lu_user
                 (id, email, full_name, password_hash, password_scheme,
                  must_change_password, account_status, status, reconciliation_state)
               VALUES ($1,'residue.b@example.invalid','Residue B',$2,'bcrypt',false,
                       'active','active','canonical')`,
            [randomUUID(), VALID_BCRYPT_HASH],
          );
        } catch {
          failed = true;
        }
        expect(failed).toBe(true);
        await c.query('ROLLBACK');
        expect(await snapshotCounts(c)).toEqual(before);
      });
    }, 120_000);

    it('leak audit: no CLI stdout/stderr, manifest artifact or child env ever contains the admin/executor passwords, DSNs, the migration_secret_reference or the derived env var name', () => {
      expect(state.sentinels.length > 0).toBe(true);
      // Boolean-only assertions (rule 7): never embed a sentinel in a
      // Jest failure message.
      for (const output of state.cliOutputs) {
        expect(containsSentinel(output)).toBe(false);
      }
      for (const file of readdirSync(state.tmpDir)) {
        const raw = readFileSync(join(state.tmpDir, file), 'utf8');
        expect(containsSentinel(raw)).toBe(false);
        expect(raw.includes('postgresql://')).toBe(false);
        expect(raw.includes('postgres://')).toBe(false);
        expect(raw.includes('Password=')).toBe(false);
      }
      // The CLI env builder must always inject the executor, never admin.
      const sample = cliEnv(FRESH_DB, true)[CONNECTION_STRING_ENV_VAR] ?? '';
      expect(sample.includes(`Username=${EXECUTOR}`)).toBe(true);
      expect(sample.includes(`Username=${state.admin.user}`)).toBe(false);
      if (state.admin.password.length >= 4) {
        expect(sample.includes(state.admin.password)).toBe(false);
      }
      if (state.adminDsnRaw.length >= 4) {
        expect(sample.includes(state.adminDsnRaw)).toBe(false);
      }
    }, 60_000);
  }
});

/** Managed identities (runtime + migrator group) must carry no privileges. */
function isSafeManagedRole(row: {
  rolcanlogin: boolean;
  rolsuper: boolean;
  rolcreaterole: boolean;
  rolcreatedb: boolean;
  rolreplication: boolean;
  rolbypassrls: boolean;
}): boolean {
  return (
    row.rolcanlogin === false &&
    row.rolsuper === false &&
    row.rolcreaterole === false &&
    row.rolcreatedb === false &&
    row.rolreplication === false &&
    row.rolbypassrls === false
  );
}
