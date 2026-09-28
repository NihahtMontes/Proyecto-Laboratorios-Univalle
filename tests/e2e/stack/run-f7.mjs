#!/usr/bin/env node
/**
 * MIG-001 F7 — disposable full-stack runner (opt-in, local only).
 *
 * Browser → React (Vite) → API client → NestJS → PostgreSQL, against uniquely
 * named disposable databases that are dropped at the end of every run.
 *
 * Required env:
 *   E2E_ADMIN_DSN            loopback admin DSN (key=value form). Used ONLY for
 *                            role snapshot/restore, CREATE/DROP of the run's
 *                            databases and the temporary executor login.
 * Optional env:
 *   E2E_ADMIN_WINDOW_MODULE  absolute path of a local module exporting
 *                            `withAdminWindow(probe, fn)`; every admin action runs
 *                            inside it (e.g. a temporary pg_hba window kept
 *                            outside the repository). Absent → admin DSN used as is.
 *   E2E_RECOVERY_FILE        path OUTSIDE the repository where the lu_auth_login
 *                            snapshot is kept while the run is in flight, so an
 *                            interrupted run can still be restored. Deleted after
 *                            a verified restore.
 *
 * Phases (`--phase`): `integration` (F3 opt-in PostgreSQL suites), `e2e`
 * (Playwright), `all` (both, integration first). Extra args after `--` go to
 * Playwright. Nothing secret is printed: DSNs/passwords only travel in child env.
 *
 * lu_auth_login lifecycle: its exact state (LOGIN flag, password verifier,
 * validity, connection limit) is captured before any change and restored
 * byte-exact at teardown. The API gets a random per-run password whose SCRAM
 * verifier is computed client-side, so the plaintext never reaches the server.
 */
import { spawn, spawnSync } from 'node:child_process';
import { createHash, createHmac, pbkdf2Sync, randomBytes, randomUUID } from 'node:crypto';
import { existsSync, mkdtempSync, readFileSync, rmSync, writeFileSync } from 'node:fs';
import { createConnection } from 'node:net';
import { tmpdir } from 'node:os';
import { dirname, join, resolve } from 'node:path';
import { fileURLToPath, pathToFileURL } from 'node:url';
import pg from 'pg';

const HERE = dirname(fileURLToPath(import.meta.url));
const E2E_ROOT = resolve(HERE, '..');
const REPO = resolve(E2E_ROOT, '..', '..');
const API_ROOT = join(REPO, 'apps', 'api');
const WEB_ROOT = join(REPO, 'apps', 'web');
const CLI_JS = join(API_ROOT, 'dist', 'database', 'migration-cli.js');
const API_MAIN = join(API_ROOT, 'dist', 'main.js');
const STATE_DIR = join(E2E_ROOT, '.state');

const RUNTIME_ROLE = 'lu_auth_runtime';
const GROUP_ROLE = 'lu_auth_migrator';
const LOGIN_ROLE = 'lu_auth_login';
const API_PORT = 3000;
const WEB_PORT = 5173;
const WEB_ORIGIN = `http://127.0.0.1:${WEB_PORT}`;

const RUN_ID = randomBytes(4).toString('hex');
const DB = {
  control: `f7_${RUN_ID}_control`,
  tenantA: `f7_${RUN_ID}_tenant_a`,
  tenantB: `f7_${RUN_ID}_tenant_b`,
};
const EXECUTOR = `lu_migration_exec_f7${RUN_ID}`;
const SITES = {
  a: { id: randomUUID(), code: `F7N${RUN_ID.slice(0, 4)}`, name: 'F7 Sede Norte', db: DB.tenantA, key: 'a' },
  b: { id: randomUUID(), code: `F7S${RUN_ID.slice(0, 4)}`, name: 'F7 Sede Sur', db: DB.tenantB, key: 'b' },
};

// ---------------------------------------------------------------------------
// args / env
// ---------------------------------------------------------------------------
const argv = process.argv.slice(2);
const dashdash = argv.indexOf('--');
const ownArgs = dashdash >= 0 ? argv.slice(0, dashdash) : argv;
const playwrightArgs = dashdash >= 0 ? argv.slice(dashdash + 1) : [];
const phaseArg = ownArgs.find((a) => a.startsWith('--phase='))?.slice('--phase='.length) ?? 'e2e';
if (!['e2e', 'integration', 'all'].includes(phaseArg)) throw new Error('--phase must be e2e|integration|all');

function parseDsn(raw) {
  const kv = {};
  for (const part of raw.split(';')) {
    const i = part.indexOf('=');
    if (i > 0) kv[part.slice(0, i).trim().toLowerCase()] = part.slice(i + 1);
  }
  const host = kv.host ?? kv.server;
  if (!['127.0.0.1', 'localhost', '::1'].includes(String(host).toLowerCase())) {
    throw new Error('E2E_ADMIN_DSN must point to a loopback host.');
  }
  return {
    host,
    port: Number(kv.port ?? 5432),
    user: kv.username ?? kv['user id'] ?? kv.user,
    password: kv.password ?? '',
    database: kv.database ?? 'postgres',
    ssl: false,
  };
}
const adminRaw = process.env.E2E_ADMIN_DSN;
if (!adminRaw) throw new Error('E2E_ADMIN_DSN is required.');
const ADMIN = parseDsn(adminRaw);
const executorPassword = randomBytes(24).toString('base64url');
const runtimePassword = randomBytes(32).toString('hex');
const SECRETS = [adminRaw, ADMIN.password, executorPassword, runtimePassword].filter((s) => s && s.length >= 4);
const scrub = (text) => SECRETS.reduce((t, s) => t.split(s).join('<redacted>'), String(text));

const kvDsn = (c) =>
  `Host=${c.host};Port=${c.port};Database=${c.database};Username=${c.user};Password=${c.password};SSL Mode=Disable`;
const executorConn = (database) => ({ host: ADMIN.host, port: ADMIN.port, database, user: EXECUTOR, password: executorPassword, ssl: false });
const runtimeConn = (database) => ({ host: ADMIN.host, port: ADMIN.port, database, user: LOGIN_ROLE, password: runtimePassword, ssl: false });

/** SCRAM-SHA-256 verifier computed locally (RFC 5802 / PostgreSQL format). */
function scramVerifier(password) {
  const salt = randomBytes(16);
  const iterations = 4096;
  const salted = pbkdf2Sync(Buffer.from(password, 'utf8'), salt, iterations, 32, 'sha256');
  const clientKey = createHmac('sha256', salted).update('Client Key').digest();
  const storedKey = createHash('sha256').update(clientKey).digest();
  const serverKey = createHmac('sha256', salted).update('Server Key').digest();
  return `SCRAM-SHA-256$${iterations}:${salt.toString('base64')}$${storedKey.toString('base64')}:${serverKey.toString('base64')}`;
}

const log = (msg) => console.log(`[f7-stack] ${scrub(msg)}`);

// ---------------------------------------------------------------------------
// admin window
// ---------------------------------------------------------------------------
let windowModule = null;
if (process.env.E2E_ADMIN_WINDOW_MODULE) {
  windowModule = await import(pathToFileURL(process.env.E2E_ADMIN_WINDOW_MODULE).href);
}
async function passwordlessPostgresAccepted() {
  const c = new pg.Client({ host: ADMIN.host, port: ADMIN.port, user: ADMIN.user, database: 'postgres', password: '', connectionTimeoutMillis: 5000 });
  try {
    await c.connect();
    await c.end();
    return true;
  } catch {
    return false;
  }
}
async function adminWindow(fn) {
  if (windowModule === null) return fn();
  return windowModule.withAdminWindow(passwordlessPostgresAccepted, fn);
}
async function withClient(conn, fn) {
  const c = new pg.Client({ ...conn, connectionTimeoutMillis: 10000, application_name: 'lu-f7-stack' });
  await c.connect();
  try {
    return await fn(c);
  } finally {
    await c.end().catch(() => undefined);
  }
}
const withAdmin = (database, fn) => withClient({ ...ADMIN, database }, fn);

// ---------------------------------------------------------------------------
// lu_auth_login snapshot / restore
// ---------------------------------------------------------------------------
const ROLE_SNAPSHOT_SQL = `SELECT rolcanlogin, rolpassword, rolvaliduntil, rolconnlimit, rolsuper, rolinherit,
         rolcreaterole, rolcreatedb, rolreplication, rolbypassrls
    FROM pg_catalog.pg_authid WHERE rolname = $1`;
let loginSnapshot = null;
const recoveryFile = process.env.E2E_RECOVERY_FILE ?? null;
if (recoveryFile !== null && resolve(recoveryFile).toLowerCase().startsWith(REPO.toLowerCase())) {
  throw new Error('E2E_RECOVERY_FILE must be outside the repository.');
}

function quoteLiteral(v) {
  return `'${String(v).replace(/'/g, "''")}'`;
}
async function applyLoginState(admin, snap) {
  // The run only ever changes LOGIN/NOLOGIN and the password; VALID UNTIL and the
  // other attributes are never touched and are verified unchanged by sameState().
  const pw = snap.rolpassword === null ? 'PASSWORD NULL' : `PASSWORD ${quoteLiteral(snap.rolpassword)}`;
  await admin.query(`ALTER ROLE ${LOGIN_ROLE} ${snap.rolcanlogin ? 'LOGIN' : 'NOLOGIN'} ${pw} CONNECTION LIMIT ${Number(snap.rolconnlimit)}`);
}
async function readLoginState(admin) {
  const r = await admin.query(ROLE_SNAPSHOT_SQL, [LOGIN_ROLE]);
  if (r.rows.length !== 1) throw new Error(`${LOGIN_ROLE} is absent`);
  return r.rows[0];
}
const sameState = (a, b) =>
  a.rolcanlogin === b.rolcanlogin &&
  a.rolpassword === b.rolpassword &&
  String(a.rolvaliduntil) === String(b.rolvaliduntil) &&
  Number(a.rolconnlimit) === Number(b.rolconnlimit) &&
  a.rolsuper === b.rolsuper &&
  a.rolinherit === b.rolinherit &&
  a.rolcreaterole === b.rolcreaterole &&
  a.rolcreatedb === b.rolcreatedb &&
  a.rolreplication === b.rolreplication &&
  a.rolbypassrls === b.rolbypassrls;

// ---------------------------------------------------------------------------
// provisioning
// ---------------------------------------------------------------------------
const created = { executor: false, dbs: [] };
const report = { runId: RUN_ID, phases: {}, teardown: {} };

async function provision() {
  await adminWindow(() =>
    withAdmin(ADMIN.database, async (admin) => {
      const roles = await admin.query(
        `SELECT rolname, rolcanlogin, rolsuper, rolcreaterole, rolcreatedb, rolreplication, rolbypassrls
           FROM pg_catalog.pg_roles WHERE rolname = ANY($1)`,
        [[RUNTIME_ROLE, GROUP_ROLE, LOGIN_ROLE]],
      );
      const byName = Object.fromEntries(roles.rows.map((r) => [r.rolname, r]));
      for (const name of [RUNTIME_ROLE, GROUP_ROLE, LOGIN_ROLE]) {
        const r = byName[name];
        if (!r) throw new Error(`role ${name} must pre-exist`);
        if (r.rolsuper || r.rolcreaterole || r.rolcreatedb || r.rolreplication || r.rolbypassrls) {
          throw new Error(`role ${name} is unsafe; refusing to proceed`);
        }
      }
      if (byName[RUNTIME_ROLE].rolcanlogin || byName[GROUP_ROLE].rolcanlogin) {
        throw new Error('runtime/migrator roles must be NOLOGIN');
      }
      const member = await admin.query(
        `SELECT 1 FROM pg_catalog.pg_auth_members m
           JOIN pg_catalog.pg_roles g ON g.oid = m.roleid
           JOIN pg_catalog.pg_roles u ON u.oid = m.member
          WHERE g.rolname = $1 AND u.rolname = $2`,
        [RUNTIME_ROLE, LOGIN_ROLE],
      );
      if (member.rows.length !== 1) throw new Error(`${LOGIN_ROLE} must be a member of ${RUNTIME_ROLE}`);

      loginSnapshot = await readLoginState(admin);
      if (recoveryFile !== null) {
        writeFileSync(recoveryFile, JSON.stringify({ role: LOGIN_ROLE, snapshot: loginSnapshot, runId: RUN_ID }), { mode: 0o600 });
      }
      log(`${LOGIN_ROLE} snapshot captured (login=${loginSnapshot.rolcanlogin}, verifier=${loginSnapshot.rolpassword === null ? 'null' : 'present'})`);

      await admin.query(
        `CREATE ROLE "${EXECUTOR}" WITH LOGIN PASSWORD ${quoteLiteral(scramVerifier(executorPassword))} ` +
          'NOINHERIT NOSUPERUSER NOCREATEDB NOCREATEROLE NOREPLICATION NOBYPASSRLS',
      );
      created.executor = true;
      await admin.query(`GRANT "${GROUP_ROLE}" TO "${EXECUTOR}"`);
      for (const db of Object.values(DB)) {
        await admin.query(`CREATE DATABASE "${db}" WITH OWNER "${GROUP_ROLE}"`);
        created.dbs.push(db);
        await withAdmin(db, async (c) => {
          await c.query('REVOKE CREATE ON SCHEMA public FROM PUBLIC');
          await c.query(`REVOKE CREATE ON SCHEMA public FROM "${RUNTIME_ROLE}"`);
          await c.query(`GRANT CREATE ON SCHEMA public TO "${GROUP_ROLE}"`);
        });
      }
      log(`created ${created.dbs.length} disposable databases and executor login`);
    }),
  );
}

/**
 * Applies the tracked managed role-provisioning script (the path this cluster was
 * provisioned with: `postgres` holds ADMIN on lu_auth_runtime) exactly as
 * committed, verified against the LF SHA-256 pinned in auth-managed-role-cli.ts.
 * The only in-memory change retargets its four `neondb` references (guard and
 * database ACL checks) to this run's control database; its preflights and
 * postconditions still run unchanged.
 */
const ROLE_SCRIPT = '003_provision_auth_runtime_managed.sql';
const ROLE_SCRIPT_SHA256 = '7fd48d7130140af8e5468d432e9831ae8a2885d3cad62c6a6e046e43e368e85a';
async function provisionRuntimeGrants() {
  const lf = readFileSync(join(API_ROOT, 'database', 'roles', ROLE_SCRIPT), 'utf8').replace(/\r\n/g, '\n');
  if (createHash('sha256').update(lf, 'utf8').digest('hex') !== ROLE_SCRIPT_SHA256) {
    throw new Error(`${ROLE_SCRIPT} does not match its recorded SHA-256 pin`);
  }
  const refs = lf.match(/\bneondb\b/g) ?? [];
  if (refs.length !== 4) throw new Error(`${ROLE_SCRIPT}: expected exactly 4 database references`);
  const sql = lf.replace(/\bneondb\b/g, DB.control);
  await adminWindow(() =>
    withAdmin(DB.control, async (c) => {
      await c.query(sql);
      log(`role provisioning ${ROLE_SCRIPT} applied to the control database (postconditions passed)`);
    }),
  );
}

function runCli(args, env) {
  const res = spawnSync(process.execPath, [CLI_JS, ...args], {
    cwd: API_ROOT,
    env,
    encoding: 'utf8',
    timeout: 180_000,
    maxBuffer: 16 * 1024 * 1024,
    windowsHide: true,
  });
  if (res.status !== 0) {
    throw new Error(`migration CLI failed (${args.slice(0, 3).join(' ')}): ${scrub(`${res.stdout}\n${res.stderr}`).slice(-2000)}`);
  }
}

function cliBaseEnv(database) {
  const env = { ...process.env };
  delete env.E2E_ADMIN_DSN;
  env.ConnectionStrings__DefaultConnection = kvDsn(executorConn(database));
  return env;
}

const manifestDir = mkdtempSync(join(tmpdir(), `f7-${RUN_ID}-`));
const registry = () => import(pathToFileURL(join(API_ROOT, 'dist', 'database', 'migration-registry.js')).href);

/** Control plane only: the integration phase needs it bare (no sites, no routes). */
async function migrateControlPlane() {
  const { CONTROL_PLANE_REGISTRY } = await registry();
  {
    for (const entry of CONTROL_PLANE_REGISTRY) {
      runCli(['up', entry.file, '--stream', 'control-plane', '--manifest-out', join(manifestDir, `cp-${entry.ordinal}.json`)], cliBaseEnv(DB.control));
      // The runtime grants of the 0001..0003 tables come from the separate tracked
      // role-provisioning step, which must run before 0004+ add their own grants
      // (its REVOKE ALL would otherwise wipe them).
      if (entry.ordinal === 3) await provisionRuntimeGrants();
    }
    log(`control-plane ${CONTROL_PLANE_REGISTRY.length} migrations applied`);
  }
}

/** Sites, tenant routes and the tenant stream for both disposable tenant databases. */
async function migrateTenants() {
  const { TENANT_REGISTRY } = await registry();
  {
    for (const site of Object.values(SITES)) {
      const migrationRef = `f7-${RUN_ID}-migration-${site.key}`;
      const runtimeRef = `f7/${RUN_ID}/tenant-${site.key}`;
      site.runtimeRef = runtimeRef;
      await withClient(executorConn(DB.control), async (c) => {
        await c.query('BEGIN');
        await c.query(`SET LOCAL ROLE "${GROUP_ROLE}"`);
        await c.query(`INSERT INTO public.lu_site (id, code, name, status) VALUES ($1, $2, $3, 'active')`, [site.id, site.code, site.name]);
        await c.query(
          `INSERT INTO public.lu_tenant_route
             (site_id, runtime_secret_reference, writer_label, state, schema_version, migration_secret_reference)
           VALUES ($1, $2, 'NEST_POSTGRES', 'migrating', 0, $3)`,
          [site.id, runtimeRef, migrationRef],
        );
        await c.query('COMMIT');
      });
      const env = cliBaseEnv(DB.control);
      env[`TENANT_MIGRATION_CONNECTION__${migrationRef.replace(/[^A-Za-z0-9]+/g, '_')}`] = kvDsn(executorConn(site.db));
      for (const entry of TENANT_REGISTRY) {
        runCli(
          ['up', entry.file, '--stream', 'tenant', '--site', site.id, '--manifest-out', join(manifestDir, `t-${site.key}-${entry.ordinal}.json`)],
          env,
        );
      }
      await withClient(executorConn(DB.control), async (c) => {
        await c.query('BEGIN');
        await c.query(`SET LOCAL ROLE "${GROUP_ROLE}"`);
        await c.query(`UPDATE public.lu_tenant_route SET state = 'active' WHERE site_id = $1`, [site.id]);
        await c.query('COMMIT');
      });
      log(`tenant ${site.key}: ${TENANT_REGISTRY.length} migrations applied, route active`);
    }
  }
}

// ---------------------------------------------------------------------------
// processes
// ---------------------------------------------------------------------------
const children = [];
function portOpen(port) {
  return new Promise((res) => {
    const s = createConnection({ host: '127.0.0.1', port });
    s.once('connect', () => {
      s.destroy();
      res(true);
    });
    s.once('error', () => res(false));
  });
}
async function waitFor(check, label, timeoutMs = 90_000) {
  const start = Date.now();
  while (Date.now() - start < timeoutMs) {
    if (await check()) return;
    await new Promise((r) => setTimeout(r, 250));
  }
  throw new Error(`timeout waiting for ${label}`);
}
function startChild(label, cmd, args, opts) {
  const child = spawn(cmd, args, { ...opts, windowsHide: true, stdio: ['ignore', 'pipe', 'pipe'] });
  const buf = [];
  const keep = (d) => {
    buf.push(scrub(d.toString()));
    if (buf.length > 200) buf.shift();
  };
  child.stdout.on('data', keep);
  child.stderr.on('data', keep);
  children.push({ label, child, buf });
  return child;
}
function stopChildren() {
  for (const { label, child } of children.reverse()) {
    if (child.exitCode === null && child.pid) {
      if (process.platform === 'win32') {
        spawnSync('taskkill', ['/PID', String(child.pid), '/T', '/F'], { windowsHide: true });
      } else {
        child.kill('SIGTERM');
      }
      log(`stopped ${label} (pid ${child.pid})`);
    }
  }
}

async function startStack(photoDir) {
  for (const port of [API_PORT, WEB_PORT]) {
    if (await portOpen(port)) throw new Error(`port ${port} already in use; refusing to start (no unrelated process is touched)`);
  }
  const apiEnv = {
    PATH: process.env.PATH,
    SystemRoot: process.env.SystemRoot,
    NODE_ENV: 'development',
    HOST: '127.0.0.1',
    PORT: String(API_PORT),
    ConnectionStrings__ControlPlaneRuntime: kvDsn(runtimeConn(DB.control)),
    AUTH_AUDIT_HMAC_KEY: randomBytes(32).toString('hex'),
    AUTH_ALLOWED_ORIGINS: WEB_ORIGIN,
    AUTH_DB_POOL_MAX: '5',
    TENANT_DB_POOL_MAX: '5',
    TENANT_DB_RUNTIME_ROLE: RUNTIME_ROLE,
    // Every E2E request comes from 127.0.0.1: only the per-IP ceiling is raised.
    // The per-identifier limit keeps its production default and is asserted by the suite.
    AUTH_RATE_LIMIT_IP_MAX_ATTEMPTS: '1000',
    PROFILE_PHOTO_STORAGE_DIR: photoDir,
  };
  for (const site of Object.values(SITES)) {
    apiEnv[`TENANT_CONNECTION__${site.runtimeRef.replace(/[^A-Za-z0-9]/g, '_').toUpperCase()}`] = kvDsn(runtimeConn(site.db));
  }
  SECRETS.push(apiEnv.AUTH_AUDIT_HMAC_KEY);
  startChild('nestjs', process.execPath, [API_MAIN], { cwd: API_ROOT, env: apiEnv });
  await waitFor(async () => {
    try {
      const r = await fetch(`http://127.0.0.1:${API_PORT}/api/v1/healthz`);
      return r.status < 500;
    } catch {
      return false;
    }
  }, 'NestJS');
  log('NestJS ready on 127.0.0.1:3000');
  const viteBin = join(WEB_ROOT, 'node_modules', 'vite', 'bin', 'vite.js');
  startChild('vite', process.execPath, [viteBin, '--host', '127.0.0.1', '--port', String(WEB_PORT), '--strictPort'], {
    cwd: WEB_ROOT,
    env: { PATH: process.env.PATH, SystemRoot: process.env.SystemRoot, BROWSER: 'none' },
  });
  await waitFor(async () => {
    try {
      return (await fetch(`${WEB_ORIGIN}/`)).ok;
    } catch {
      return false;
    }
  }, 'Vite');
  log('Vite ready on 127.0.0.1:5173');
}

function runPlaywright() {
  const fixtures = {
    runId: RUN_ID,
    sites: Object.fromEntries(Object.values(SITES).map((s) => [s.key, { id: s.id, code: s.code, name: s.name }])),
  };
  const env = {
    ...process.env,
    E2E_BASE_URL: WEB_ORIGIN,
    E2E_API_URL: `http://127.0.0.1:${API_PORT}`,
    E2E_FIXTURES: JSON.stringify(fixtures),
    E2E_FIXTURE_CONTROL_DSN: kvDsn(executorConn(DB.control)),
    E2E_FIXTURE_TENANT_A_DSN: kvDsn(executorConn(DB.tenantA)),
    E2E_FIXTURE_TENANT_B_DSN: kvDsn(executorConn(DB.tenantB)),
  };
  delete env.E2E_ADMIN_DSN;
  delete env.E2E_ADMIN_WINDOW_MODULE;
  delete env.E2E_RECOVERY_FILE;
  const cli = join(E2E_ROOT, 'node_modules', '@playwright', 'test', 'cli.js');
  const res = spawnSync(process.execPath, [cli, 'test', ...playwrightArgs], { cwd: E2E_ROOT, env, stdio: 'inherit', windowsHide: true });
  return res.status ?? 1;
}

function runIntegration() {
  const env = { ...process.env };
  delete env.E2E_ADMIN_DSN;
  env.AUTH_PG_INTEGRATION = '1';
  // The opt-in suites read their admin connection from DefaultConnection by design.
  env.ConnectionStrings__DefaultConnection = kvDsn({ ...ADMIN, database: DB.control });
  const jest = join(API_ROOT, 'node_modules', 'jest', 'bin', 'jest.js');
  const res = spawnSync(
    process.execPath,
    ['--experimental-vm-modules', jest, '--runInBand', 'test/auth.postgres.integration.e2e-spec.ts', 'test/auth.bootstrap.postgres.integration.e2e-spec.ts'],
    { cwd: API_ROOT, env, encoding: 'utf8', windowsHide: true, maxBuffer: 64 * 1024 * 1024 },
  );
  const out = scrub(`${res.stdout}\n${res.stderr}`);
  mkdirState();
  writeFileSync(join(STATE_DIR, 'integration.log'), out);
  process.stdout.write(out.split('\n').filter((l) => /Tests:|Suites:|✓|✕|●|PASS|FAIL/.test(l)).join('\n') + '\n');
  return res.status ?? 1;
}
function mkdirState() {
  if (!existsSync(STATE_DIR)) spawnSync(process.execPath, ['-e', `require('fs').mkdirSync(${JSON.stringify(STATE_DIR)},{recursive:true})`]);
}

// ---------------------------------------------------------------------------
// teardown
// ---------------------------------------------------------------------------
async function teardown() {
  stopChildren();
  for (const port of [API_PORT, WEB_PORT]) {
    report.teardown[`port${port}Free`] = !(await portOpen(port));
  }
  await adminWindow(() =>
    withAdmin(ADMIN.database, async (admin) => {
      if (loginSnapshot !== null) {
        await applyLoginState(admin, loginSnapshot);
        const now = await readLoginState(admin);
        report.teardown.loginRoleRestored = sameState(now, loginSnapshot);
      }
      for (const db of created.dbs) {
        await admin.query(`DROP DATABASE IF EXISTS "${db}" WITH (FORCE)`);
      }
      if (created.executor) {
        await admin.query(`REVOKE "${GROUP_ROLE}" FROM "${EXECUTOR}"`).catch(() => undefined);
        await admin.query(`DROP ROLE IF EXISTS "${EXECUTOR}"`);
      }
      const dbResidue = await admin.query(`SELECT count(*)::int AS n FROM pg_catalog.pg_database WHERE datname LIKE 'f7\\_%' ESCAPE '\\'`);
      const roleResidue = await admin.query(`SELECT count(*)::int AS n FROM pg_catalog.pg_roles WHERE rolname LIKE 'lu\\_migration\\_exec\\_f7%' ESCAPE '\\'`);
      report.teardown.tempDbResidue = dbResidue.rows[0].n;
      report.teardown.tempRoleResidue = roleResidue.rows[0].n;
    }),
  );
  if (windowModule?.evidence?.length) {
    const last = windowModule.evidence[windowModule.evidence.length - 1];
    report.teardown.pgHbaPinMatch = last.pinMatch;
    report.teardown.trustActive = last.trustActive;
  }
  if (report.teardown.loginRoleRestored && recoveryFile !== null && existsSync(recoveryFile)) rmSync(recoveryFile);
}

// ---------------------------------------------------------------------------
// main
// ---------------------------------------------------------------------------
let exitCode = 0;
const photoDir = mkdtempSync(join(tmpdir(), `f7-photos-${RUN_ID}-`));
for (const f of [CLI_JS, API_MAIN]) if (!existsSync(f)) throw new Error(`missing ${f}; build @lu/api first`);
try {
  log(`run ${RUN_ID} phase=${phaseArg}`);
  await provision();
  await migrateControlPlane();
  if (phaseArg === 'integration' || phaseArg === 'all') {
    await adminWindow(() =>
      withAdmin(ADMIN.database, (admin) => admin.query(`ALTER ROLE ${LOGIN_ROLE} NOLOGIN PASSWORD NULL`)),
    );
    log(`${LOGIN_ROLE} set to NOLOGIN PASSWORD NULL for the integration precondition`);
    const code = await adminWindow(async () => runIntegration());
    report.phases.integration = code === 0 ? 'PASS' : `FAIL(${code})`;
    if (code !== 0) exitCode = code;
  }
  if (phaseArg === 'e2e' || phaseArg === 'all') {
    await migrateTenants();
    await adminWindow(() =>
      withAdmin(ADMIN.database, (admin) =>
        admin.query(`ALTER ROLE ${LOGIN_ROLE} LOGIN PASSWORD ${quoteLiteral(scramVerifier(runtimePassword))}`),
      ),
    );
    log(`${LOGIN_ROLE} enabled with a random per-run password`);
    await startStack(photoDir);
    const code = runPlaywright();
    report.phases.e2e = code === 0 ? 'PASS' : `FAIL(${code})`;
    if (code !== 0) exitCode = code;
  }
} catch (error) {
  console.error(`[f7-stack] ERROR: ${scrub(error instanceof Error ? error.stack ?? error.message : error)}`);
  for (const { label, buf } of children) console.error(`--- ${label} (tail) ---\n${buf.slice(-40).join('')}`);
  exitCode = 1;
} finally {
  try {
    await teardown();
  } catch (error) {
    console.error(`[f7-stack] TEARDOWN ERROR: ${scrub(error instanceof Error ? error.message : error)}`);
    exitCode = 98;
  }
  rmSync(photoDir, { recursive: true, force: true });
  rmSync(manifestDir, { recursive: true, force: true });
  report.teardown.photoDirRemoved = !existsSync(photoDir);
  console.log(`[f7-stack] REPORT ${JSON.stringify(report)}`);
  const t = report.teardown;
  if (t.loginRoleRestored === false || t.tempDbResidue !== 0 || t.tempRoleResidue !== 0 || t.trustActive === true) exitCode = 97;
}
process.exit(exitCode);
