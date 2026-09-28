/**
 * migration-cli: ESM command-line entry point for the identity control-plane
 * and tenant migration streams.
 *
 * Commands:
 *   plan [--stream control-plane|tenant] [--site <uuid>]
 *                            Offline (NO connection): validates the registry pin and per-entry
 *                            safety policy for registered migrations, prints file, SHA-256,
 *                            canonical bytes, stream, tables and style.
 *   status [--stream control-plane|tenant] [--site <uuid>]
 *                            Connects using ConnectionStrings__DefaultConnection and prints ONLY
 *                            metadata (current_database/current_user/version + information_schema
 *                            presence of expected tables). Missing tables are reported,
 *                            not an error (exit 0).
 *   up [--manifest-out f] [file.sql] [--stream control-plane|tenant] [--site <uuid>]
 *                            Applies the canonical derived body of a registered migration inside
 *                            a single SERIALIZABLE transaction with full schema verification.
 *                            Tenant runs require an explicit --site <uuid>; the CLI opens a
 *                            tenant-specific cross-DB lease via `withTenantMigrationLease`,
 *                            runs the runner inside the callback window, and only commits the
 *                            control-plane route advancement after the tenant callback reports a
 *                            committed ordinal. Tenant reads / writes NEVER touch
 *                            ConnectionStrings__DefaultConnection at the runner level — the lease
 *                            resolves the DSN and the runner uses only the per-tenant config.
 *   verify [--manifest-out <file.json>] [file.sql] [--stream control-plane|tenant] [--site <uuid>]
 *                            Read-only verification of the registered schema manifest via the
 *                            read-only tenant resolver (`resolveTenantConfig`).
 *
 * Stream / --site flags are accepted anywhere in argv. Positional file arguments
 * are interpreted strictly: exactly one may follow.
 *
 * MIG-001-F2-W15A note: the `up` path on the tenant stream is wrapped in a
 * SERIALIZABLE-isolated cross-DB fence. The CLI is the sole caller of the
 * lease; the resolver path remains read-only. `plan` and `verify` never
 * touch the tenant DB except through the runner on a fully resolved DSN.
 */
import { readFileSync, renameSync, statSync, writeFileSync } from 'node:fs';
import { dirname, isAbsolute, join, resolve, sep } from 'node:path';
import { pathToFileURL } from 'node:url';
import { Client } from 'pg';
import {
  CONNECTION_STRING_ENV_VAR,
  describeTarget,
  loadConnectionConfigFromEnv,
  redactSecrets,
  type PostgresConnectionConfig,
} from './connection-config.js';
import {
  IDENTITY_MIGRATION_FILE,
  MIGRATIONS_DIR_NAME,
  type MigrationPlan,
  type MigrationStream,
} from './migration-plan.js';
import {
  applyMigration,
  fetchStatus,
  verifyMigration,
  type UpOutcome,
} from './migration-runner.js';
import {
  CONTROL_PLANE_REGISTRY,
  TENANT_REGISTRY,
  type MigrationRegistryEntry,
  listControlPlaneMigrations,
  listTenantMigrations,
  resolveEntryPath,
  validatePinnedMigration,
} from './migration-registry.js';
import {
  resolveTenantConfig,
  withTenantMigrationLease,
  TenantMigrationLeaseError,
  type TenantClient,
  type TenantSafeTarget,
} from './tenant-config.js';
import {
  assertRuntimeGrantsBeforeControlPlaneFour,
  CutoverPreflightError,
  isControlPlaneFourApplied,
  type PreflightQuery,
  type PreflightQueryRow,
} from './cutover-preflight.js';

const USAGE = `Usage (run from the apps/api package directory):
  node dist/database/migration-cli.js plan [--stream control-plane|tenant] [--site <uuid>]
  node dist/database/migration-cli.js status [--stream control-plane|tenant] [--site <uuid>]
  node dist/database/migration-cli.js up [--manifest-out <file.json>] [file.sql] [--stream control-plane|tenant] [--site <uuid>]
  node dist/database/migration-cli.js verify [--manifest-out <file.json>] [file.sql] [--stream control-plane|tenant] [--site <uuid>]

Required environment variable for status/up/verify:
  ${CONNECTION_STRING_ENV_VAR}   (never printed; fail-closed when missing)

Streams:
  control-plane  Central identity DB (default).
  tenant         Per-site tenant DB. The migration_secret_reference is
                 resolved server-side via lu_tenant_route and the
                 TENANT_MIGRATION_CONNECTION__<SANITIZED_REF> env var.
`;

const SAFE_FILE_RE = /^[A-Za-z0-9][A-Za-z0-9._-]*\.sql$/;
const SAFE_STREAM_RE = /^(control-plane|tenant)$/;
const SAFE_SITE_ID_RE = /^[0-9a-f]{8}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{12}$/i;

class CliFailure extends Error {
  constructor(message: string) {
    super(message);
    this.name = 'CliFailure';
  }
}

function migrationsDir(): string {
  return resolve(process.cwd(), MIGRATIONS_DIR_NAME);
}

function controlPlaneDir(): string {
  return join(migrationsDir(), 'control-plane');
}

function tenantDir(): string {
  return join(migrationsDir(), 'tenant');
}

interface ResolvedFile {
  filePath: string;
  entry: MigrationRegistryEntry;
}

function resolveRegisteredFile(
  stream: MigrationStream,
  requested: string | undefined,
): ResolvedFile {
  const fileName = requested ?? IDENTITY_MIGRATION_FILE;
  if (!SAFE_FILE_RE.test(fileName) || fileName.includes('..')) {
    throw new CliFailure(
      `Invalid migration file name; expected a plain "*.sql" file name inside migrations/${stream}.`,
    );
  }
  const entry =
    (stream === 'control-plane'
      ? CONTROL_PLANE_REGISTRY.find((e) => e.file === fileName)
      : TENANT_REGISTRY.find((e) => e.file === fileName)) ??
    (stream === 'control-plane'
      ? CONTROL_PLANE_REGISTRY.find((e) => e.relativePath === `control-plane/${fileName}`)
      : TENANT_REGISTRY.find((e) => e.relativePath === `tenant/${fileName}`));
  if (entry === undefined) {
    throw new CliFailure(
      `Migration file "${fileName}" is not registered in the migration registry.`,
    );
  }
  const base =
    entry.relativePath.split('/').length > 1
      ? stream === 'control-plane'
        ? controlPlaneDir()
        : tenantDir()
      : migrationsDir();
  const localName = entry.relativePath.split('/').pop() ?? entry.file;
  return { filePath: join(base, localName), entry };
}

interface CommandArgs {
  fileArg: string | undefined;
  manifestOut: string | undefined;
  stream: MigrationStream | undefined;
  siteId: string | undefined;
}

interface ManifestTableObservation {
  table: string;
  exists: boolean;
  columnCount: number;
}

interface ManifestArtifact {
  workId: string;
  command: 'up' | 'verify';
  file: string;
  stream: MigrationStream;
  relativePath: string;
  sha256: string;
  bytes: number;
  style: string;
  policy: 'strict' | 'loose' | 'legacy' | 'legacy-strict';
  startedAt: string;
  finishedAt: string;
  server: {
    database: string;
    user: string;
    version: string;
  };
  target: string;
  readOnly: boolean;
  executed: boolean;
  committed: boolean;
  skipped: boolean;
  verification: {
    passed: boolean;
    diffs: readonly string[];
  };
  inspection: {
    tables: readonly ManifestTableObservation[];
  };
  preflight?: {
    preexisting: readonly string[];
  };
  history?: {
    stream: MigrationStream;
    relativePath: string;
  };
  site?: {
    siteId: string;
  };
}

function validateManifestOutPath(raw: string): string {
  const resolved = resolve(process.cwd(), raw);
  const cwd = resolve(process.cwd());
  if (!isAbsolute(raw) && !resolved.toLowerCase().startsWith(cwd.toLowerCase() + sep)) {
    throw new CliFailure(
      `Manifest output path "${raw}" escapes the current working directory. ` +
        'Use a path inside cwd or an absolute path.',
    );
  }
  const dir = dirname(resolved);
  try {
    const stat = statSync(dir);
    if (!stat.isDirectory()) {
      throw new CliFailure(`Manifest output directory "${dir}" is not a directory.`);
    }
  } catch {
    throw new CliFailure(`Manifest output directory "${dir}" does not exist.`);
  }
  return resolved;
}

export function parseCommandArgs(command: string, args: readonly string[]): CommandArgs {
  const result: CommandArgs = {
    fileArg: undefined,
    manifestOut: undefined,
    stream: undefined,
    siteId: undefined,
  };
  let positionalUsed = false;
  let i = 0;
  while (i < args.length) {
    const arg = args[i];
    if (arg === undefined) break;
    switch (arg) {
      case '--manifest-out': {
        const value = args[i + 1];
        if (value === undefined) throw new CliFailure('--manifest-out requires a file path.');
        result.manifestOut = value;
        i += 2;
        continue;
      }
      case '--stream': {
        const value = args[i + 1];
        if (value === undefined || !SAFE_STREAM_RE.test(value)) {
          throw new CliFailure(
            `--stream must be one of control-plane or tenant (got "${value ?? ''}").`,
          );
        }
        result.stream = value as MigrationStream;
        i += 2;
        continue;
      }
      case '--site': {
        const value = args[i + 1];
        if (value === undefined || !SAFE_SITE_ID_RE.test(value)) {
          throw new CliFailure(`--site must be a RFC 4122 UUID (got "${value ?? ''}").`);
        }
        result.siteId = value.toLowerCase();
        i += 2;
        continue;
      }
    }
    if (arg.startsWith('--manifest-out=')) {
      result.manifestOut = arg.slice('--manifest-out='.length);
      i += 1;
      continue;
    }
    if (arg.startsWith('--stream=')) {
      const value = arg.slice('--stream='.length);
      if (!SAFE_STREAM_RE.test(value)) {
        throw new CliFailure(`--stream must be control-plane or tenant (got "${value}").`);
      }
      result.stream = value as MigrationStream;
      i += 1;
      continue;
    }
    if (arg.startsWith('--site=')) {
      const value = arg.slice('--site='.length);
      if (!SAFE_SITE_ID_RE.test(value)) {
        throw new CliFailure(`--site must be a RFC 4122 UUID (got "${value}").`);
      }
      result.siteId = value.toLowerCase();
      i += 1;
      continue;
    }
    if (arg.startsWith('-')) {
      throw new CliFailure(`unknown flag "${arg}".`);
    }
    if (positionalUsed) {
      throw new CliFailure(`${command} takes at most one migration file name.`);
    }
    positionalUsed = true;
    result.fileArg = arg;
    i += 1;
  }
  return result;
}

export function writeManifestArtifact(resolvedPath: string, artifact: ManifestArtifact): void {
  const tmp = `${resolvedPath}.tmp-${process.pid}-${Date.now()}`;
  writeFileSync(tmp, `${JSON.stringify(artifact, null, 2)}\n`);
  renameSync(tmp, resolvedPath);
}

function policyLabel(
  entry: MigrationRegistryEntry,
): 'strict' | 'loose' | 'legacy' | 'legacy-strict' {
  if (entry.legacyPolicy) return 'legacy';
  if (entry.strictPolicy) return 'strict';
  return 'legacy-strict';
}

function readRegisteredPlan(filePath: string): { plan: MigrationPlan; buffer: Buffer } {
  let buffer: Buffer;
  try {
    buffer = readFileSync(filePath);
  } catch {
    throw new CliFailure(`Cannot read migration file at ${filePath}.`);
  }
  const { plan } = validatePinnedMigration(filePath, buffer);
  return { plan, buffer };
}

function printPlan(plan: MigrationPlan, entry: MigrationRegistryEntry): void {
  console.log(`  file:       ${plan.path}`);
  console.log(`  stream:     ${plan.stream}`);
  console.log(`  ordinal:    ${entry.ordinal}`);
  console.log(`  sha256:     ${plan.sha256}`);
  console.log(`  bytes:      ${plan.byteLength} (canonical LF; raw=${plan.rawByteLength})`);
  console.log(`  style:      ${entry.style}`);
  console.log(`  workId:     ${entry.workId}`);
  console.log(`  policy:     ${policyLabel(entry)}`);
  console.log(
    `  tables:     ${plan.tables.join(', ') || '(none; DDL is index-only/extension only)'}`,
  );
  console.log('  safety:     PASS (registry-pinned, allowlisted CREATE TABLE set, envelope OK)');
}

async function commandPlan(args: readonly string[]): Promise<number> {
  const parsed = parseCommandArgs('plan', args);
  const requestedFile = parsed.fileArg;
  // parseCommandArgs already accepts --stream control-plane|tenant; when no
  // file argument is given we plan the whole requested stream (or both
  // streams when --stream is omitted).
  const streamFilter = parsed.stream;
  let entries: readonly MigrationRegistryEntry[];
  if (requestedFile !== undefined) {
    // resolveRegisteredFile throws for unregistered / unsafe names; the
    // thrown CliFailure propagates up through `run` so callers see the
    // exact failure (the original CLI behaviour, preserved here).
    entries = [resolveRegisteredFile(streamFilter ?? 'control-plane', requestedFile).entry];
  } else if (streamFilter !== undefined) {
    entries =
      streamFilter === 'control-plane'
        ? [...listControlPlaneMigrations()]
        : [...listTenantMigrations()];
  } else {
    entries = [...listControlPlaneMigrations(), ...listTenantMigrations()];
  }

  if (entries.length === 0) {
    throw new CliFailure('No registered migrations to plan.');
  }

  console.log('Migration plan (offline analysis; no database connection made):');
  let failures = 0;
  for (const entry of entries) {
    const filePath = resolveEntryPath(entry, migrationsDir());
    try {
      const { plan } = readRegisteredPlan(filePath);
      printPlan(plan, entry);
    } catch (error) {
      failures += 1;
      console.error(`  ${entry.relativePath}: REJECTED`);
      console.error(`    ${(error as Error).message}`);
    }
  }
  if (failures > 0) {
    console.error(`plan: ${failures} registered migration(s) failed the safety policy.`);
    return 1;
  }
  console.log(
    `plan: ${entries.length} registered migration(s) validated; safe to inspect with status.`,
  );
  return 0;
}

function loadConfigOrFail(): PostgresConnectionConfig {
  return loadConnectionConfigFromEnv(process.env);
}

function requireSiteForTenant(
  command: string,
  stream: MigrationStream,
  siteId: string | undefined,
): string {
  if (stream === 'tenant' && siteId === undefined) {
    throw new CliFailure(
      `${command} on stream tenant requires an explicit --site <uuid> argument; the runner never invents a site.`,
    );
  }
  return siteId ?? '';
}

/**
 * Open a control-plane pg.Client adapter suitable for the tenant resolver.
 * The control-plane DSN is the same one passed to `loadConfigOrFail`; the
 * adapter is closed by the resolver (`TenantClient.close`).
 */
async function openControlPlaneTenantClient(
  config: PostgresConnectionConfig,
): Promise<TenantClient> {
  const c = new Client({
    ...config,
    application_name: 'lu-migration-cli-cp-resolver',
  });
  await c.connect();
  return {
    async query<T extends Record<string, unknown>>(text: string, values?: unknown[]) {
      const r = values ? await c.query<T>(text, values as never[]) : await c.query<T>(text);
      return { rows: (r as { rows: T[] }).rows ?? [] };
    },
    async close() {
      await c.end().catch(() => undefined);
    },
  };
}

async function resolveTenantTarget(
  operation: 'status' | 'verify' | 'up',
  siteId: string,
): Promise<{ config: PostgresConnectionConfig; safeTarget: string }> {
  const cpConfig = loadConfigOrFail();
  const resolution = await resolveTenantConfig(
    siteId,
    operation,
    cpConfig,
    process.env,
    openControlPlaneTenantClient,
  );
  return {
    config: resolution.config,
    safeTarget: `${resolution.safeTarget.host}:${resolution.safeTarget.port}/${resolution.safeTarget.database}`,
  };
}

async function commandStatus(args: readonly string[]): Promise<number> {
  const parsed = parseCommandArgs('status', args);
  const stream: MigrationStream = parsed.stream ?? 'control-plane';
  const siteId = requireSiteForTenant('status', stream, parsed.siteId);

  let config: PostgresConnectionConfig;
  let safeTarget: string;
  if (stream === 'tenant') {
    const r = await resolveTenantTarget('status', siteId);
    config = r.config;
    safeTarget = r.safeTarget;
  } else {
    config = loadConfigOrFail();
    safeTarget = describeTarget(config);
  }
  console.log(
    `Migration status (metadata only) for target ${safeTarget} on stream ${stream} (site ${siteId || '(n/a)'})`,
  );
  const inspection = await fetchStatus(config, {
    stream,
    site: siteId ? { siteId, migrationSecretReference: null } : undefined,
  });
  console.log(`  server database: ${inspection.server.database}`);
  console.log(`  server user:     ${inspection.server.user}`);
  console.log(`  server version:  ${inspection.server.version}`);
  for (const entry of inspection.tables) {
    if (entry.exists) {
      console.log(`  ${entry.table.padEnd(36)} EXISTS   (${entry.columns.length} columns)`);
    } else {
      console.log(`  ${entry.table.padEnd(36)} MISSING`);
    }
  }
  const missing = inspection.tables.filter((entry) => !entry.exists).map((entry) => entry.table);
  if (missing.length === inspection.tables.length) {
    console.log('result: no target tables exist yet; "up" is applicable.');
  } else if (missing.length > 0) {
    console.log(`result: partially present; missing: ${missing.join(', ')}.`);
  } else {
    console.log('result: all target tables exist.');
  }
  return 0;
}

function buildManifestArtifact(
  entry: MigrationRegistryEntry,
  plan: MigrationPlan,
  config: PostgresConnectionConfig,
  startedAt: string,
  finishedAt: string,
  outcome: {
    readOnly: boolean;
    executed: boolean;
    committed: boolean;
    skipped: boolean;
    verification: { passed: boolean; diffs: readonly string[] };
    inspection: {
      server: { database: string; user: string; version: string };
      tables: readonly { table: string; exists: boolean; columns: readonly string[] }[];
    };
    preexisting?: readonly string[];
    history?: { stream: MigrationStream; relativePath: string };
    siteId?: string;
  },
): ManifestArtifact {
  return {
    workId: entry.workId,
    command: outcome.readOnly ? 'verify' : 'up',
    file: plan.file,
    stream: entry.stream,
    relativePath: entry.relativePath,
    sha256: plan.sha256,
    bytes: plan.byteLength,
    style: entry.style,
    policy: policyLabel(entry),
    startedAt,
    finishedAt,
    server: {
      database: outcome.inspection.server.database,
      user: outcome.inspection.server.user,
      version: outcome.inspection.server.version,
    },
    target: describeTarget(config),
    readOnly: outcome.readOnly,
    executed: outcome.executed,
    committed: outcome.committed,
    skipped: outcome.skipped,
    verification: {
      passed: outcome.verification.passed,
      diffs: outcome.verification.diffs,
    },
    inspection: {
      tables: outcome.inspection.tables.map((t) => ({
        table: t.table,
        exists: t.exists,
        columnCount: t.columns.length,
      })),
    },
    preflight: outcome.preexisting === undefined ? undefined : { preexisting: outcome.preexisting },
    history: outcome.history,
    site: outcome.siteId === undefined ? undefined : { siteId: outcome.siteId },
  };
}

async function commandUp(args: readonly string[]): Promise<number> {
  const parsed = parseCommandArgs('up', args);
  const stream: MigrationStream = parsed.stream ?? 'control-plane';
  const { fileArg, manifestOut } = parsed;
  const siteIdResolved = requireSiteForTenant('up', stream, parsed.siteId);
  const { filePath } = resolveRegisteredFile(stream, fileArg);

  let buffer: Buffer;
  try {
    buffer = readFileSync(filePath);
  } catch {
    throw new CliFailure(`Cannot read migration file at ${filePath}.`);
  }
  const startedAt = new Date().toISOString();
  const { plan, entry } = validatePinnedMigration(filePath, buffer, { stream });

  // Tenant `up` paths through `withTenantMigrationLease`: the lease opens
  // the control-plane transaction with BEGIN + SET LOCAL ROLE + global
  // advisory lock + SELECT FOR UPDATE, holds the CP transaction open
  // while the callback runs `applyMigration` against the per-tenant DSN,
  // then UPDATEs the route row inside the SAME CP transaction and
  // COMMITs. Any callback failure (preexisting / not committed / verification
  // fail) is treated as a callback failure and the lease ROLLBACKs without
  // advancing `schema_version`.
  if (stream === 'tenant') {
    const cpConfig = loadConfigOrFail();
    const captured: { outcome: UpOutcome | null; safeTarget: TenantSafeTarget | null } = {
      outcome: null,
      safeTarget: null,
    };
    try {
      await withTenantMigrationLease({
        siteId: siteIdResolved,
        env: process.env,
        controlPlaneConfig: cpConfig,
        controlPlaneClientFactory: openControlPlaneTenantClient,
        callback: async (input) => {
          captured.safeTarget = input.safeTarget;
          const outcome = await applyMigration(input.config, filePath, buffer, {
            stream: 'tenant',
            site: { siteId: siteIdResolved, migrationSecretReference: null },
          });
          captured.outcome = outcome;
          // The lease MUST NOT advance the route row when the runner did
          // not commit (preexisting target / verification fail / not
          // committed). A throw inside the callback is the explicit
          // "callback failure" signal the lease expects.
          if (!outcome.committed) {
            throw new TenantMigrationLeaseError(
              `applyMigration did not commit on the tenant DB for site ${siteIdResolved} (preexisting / not committed); refusing to advance route version`,
            );
          }
          return { committed: true, ordinal: entry.ordinal };
        },
      });
    } catch (error) {
      // Preexisting abort: the runner returned committed=false while
      // applyMigration itself did NOT throw (it returned a structured
      // outcome). The captured outcome lets us surface the operator-facing
      // ABORTED log the same way the pre-lease path did, without losing
      // the migration_secret_reference / env-name redaction guarantees.
      const runnerOutcome = captured.outcome;
      if (runnerOutcome !== null && !runnerOutcome.executed) {
        console.error(
          `ABORTED: existing target table(s) detected: ${runnerOutcome.preexisting.join(', ')}. ` +
            'This tool refuses to run against a database where any target table already exists without a matching history pin.',
        );
        return 1;
      }
      throw error;
    }
    const outcome = captured.outcome as UpOutcome;
    const resolvedSafeTarget = captured.safeTarget;
    const safeTargetStr =
      resolvedSafeTarget === null
        ? '(resolved under lease)'
        : `${resolvedSafeTarget.host}:${resolvedSafeTarget.port}/${resolvedSafeTarget.database}`;
    // Tenant manifest artifacts describe the resolved tenant target, never
    // the control-plane DSN: the lease's callback input is captured into
    // `tenantApplyConfig` as a backup, but `describeTarget` derives a
    // secret-free host:port/database summary that is safe to embed.
    const tenantApplyConfig: PostgresConnectionConfig = {
      host: resolvedSafeTarget?.host ?? '',
      port: resolvedSafeTarget?.port ?? 0,
      database: resolvedSafeTarget?.database ?? '',
      user: '',
      password: '',
      ssl: false,
    };
    return finalizeUpApply({
      entry,
      plan,
      filePath,
      outcome,
      safeTargetStr,
      appliedConfig: tenantApplyConfig,
      startedAt,
      manifestOut,
      siteIdResolved,
    });
  }

  // Control-plane path is unchanged: the runner drives the same
  // SERIALIZABLE transaction it always has, and the manifest artifact
  // uses the CP target.
  const config = loadConfigOrFail();
  const safeTarget = describeTarget(config);
  console.log(`Applying migration on stream ${entry.stream} to ${safeTarget}`);
  console.log(`  file:       ${plan.path}`);
  console.log(`  relative:   ${entry.relativePath}`);
  console.log(`  ordinal:    ${entry.ordinal}`);
  console.log(`  sha256:     ${plan.sha256}  (canonical LF source)`);
  console.log(`  bytes:      ${plan.byteLength}`);
  console.log(`  workId:     ${entry.workId}`);
  console.log(`  style:      ${entry.style}`);
  console.log(`  policy:     ${policyLabel(entry)}`);
  console.log(`  siteId:     ${siteIdResolved || '(n/a for control-plane)'}`);
  console.log(
    `  note:       executing the canonical derived body (BEGIN/COMMIT removed; IF NOT EXISTS preserved)`,
  );

  if (entry.stream === 'control-plane' && entry.ordinal === 4) {
    await runControlPlaneFourCutoverPreflight(config);
  }

  const outcome = await applyMigration(config, filePath, buffer, {
    stream: entry.stream,
    site:
      siteIdResolved === ''
        ? undefined
        : { siteId: siteIdResolved, migrationSecretReference: null },
  });
  return finalizeUpApply({
    entry,
    plan,
    filePath,
    outcome,
    safeTargetStr: safeTarget,
    appliedConfig: config,
    startedAt,
    manifestOut,
    siteIdResolved,
  });
}

/**
 * MIG-001 F8 cutover guard. Applies only to control-plane ordinal 4
 * (`0004_academic_catalogs.sql`) when that entry is NOT yet recorded in
 * `public.lu_migration_history`. The check opens a short-lived pg.Client
 * against the SAME target database the runner is about to use, runs the
 * `roles/003` runtime postconditions, and closes the connection BEFORE
 * any DDL/ledger write. A failure exits non-zero with a non-secret
 * message naming the missing privileges and the remedy
 * (`auth:managed-role provision` after control-plane 0003, before 0004).
 *
 * Other ordinals and an already-applied 0004 short-circuit without a
 * preflight check.
 */
async function runControlPlaneFourCutoverPreflight(
  config: PostgresConnectionConfig,
): Promise<void> {
  const client = new Client({
    ...config,
    application_name: 'lu-migration-cli-cutover-preflight',
  });
  await client.connect();
  try {
    const query: PreflightQuery = <Row extends PreflightQueryRow>(
      sql: string,
      values?: readonly unknown[],
    ): Promise<readonly Row[]> => {
      const exec = values ? client.query<Row>(sql, values as unknown[]) : client.query<Row>(sql);
      return exec.then((result) => (result as { rows: readonly Row[] }).rows);
    };

    if (await isControlPlaneFourApplied(query)) {
      return;
    }

    await assertRuntimeGrantsBeforeControlPlaneFour(query);
  } catch (error) {
    if (error instanceof CutoverPreflightError) {
      throw error;
    }
    const reason = error instanceof Error ? error.message : String(error);
    throw new CutoverPreflightError(
      `cutover preflight failed: unable to verify roles/003 runtime postconditions (${reason}). ` +
        `Run \`auth:managed-role provision\` (database/roles/003_provision_auth_runtime_managed.sql) ` +
        'after control-plane migration 0003 and before 0004.',
    );
  } finally {
    await client.end().catch(() => undefined);
  }
}

interface FinalizeUpApplyArgs {
  entry: MigrationRegistryEntry;
  plan: MigrationPlan;
  filePath: string;
  outcome: UpOutcome;
  safeTargetStr: string;
  appliedConfig: PostgresConnectionConfig;
  startedAt: string;
  manifestOut: string | undefined;
  siteIdResolved: string;
}

function finalizeUpApply(args: FinalizeUpApplyArgs): number {
  const finishedAt = new Date().toISOString();
  // Control-plane path already logged the apply banner; the tenant lease
  // path logs it after the lease resolves so the operator sees the
  // resolved target exactly once on stdout.
  if (args.entry.stream === 'tenant') {
    console.log(`Applying migration on stream ${args.entry.stream} to ${args.safeTargetStr}`);
    console.log(`  file:       ${args.plan.path}`);
    console.log(`  relative:   ${args.entry.relativePath}`);
    console.log(`  ordinal:    ${args.entry.ordinal}`);
    console.log(`  sha256:     ${args.plan.sha256}  (canonical LF source)`);
    console.log(`  bytes:      ${args.plan.byteLength}`);
    console.log(`  workId:     ${args.entry.workId}`);
    console.log(`  style:      ${args.entry.style}`);
    console.log(`  policy:     ${policyLabel(args.entry)}`);
    console.log(`  siteId:     ${args.siteIdResolved || '(n/a for control-plane)'}`);
    console.log(
      `  note:       executing the canonical derived body (BEGIN/COMMIT removed; IF NOT EXISTS preserved)`,
    );
  }

  for (const entryTable of args.outcome.inspection.tables) {
    console.log(
      `  ${entryTable.table.padEnd(36)} ${entryTable.exists ? 'EXISTS ' : 'MISSING'} (${entryTable.columns.length} columns)`,
    );
  }

  if (args.outcome.skipped?.reason === 'pin-matches-history') {
    console.log('up: migration skipped (pin matches recorded history).');
  } else if (!args.outcome.executed) {
    console.error(
      `ABORTED: existing target table(s) detected: ${args.outcome.preexisting.join(', ')}. ` +
        'This tool refuses to run against a database where any target table already exists without a matching history pin.',
    );
    return 1;
  } else {
    const verification = args.outcome.verification;
    if (verification === null || !verification.passed) {
      console.error(`FAILED POST-VERIFICATION: ${verification?.diffs.join('; ') ?? 'unknown'}`);
      return 1;
    }
    console.log(
      'up: migration applied, committed and verified (schema manifest + supplemental PASS).',
    );
  }

  if (args.manifestOut !== undefined) {
    const resolved = validateManifestOutPath(args.manifestOut);
    const artifact = buildManifestArtifact(
      args.entry,
      args.plan,
      args.appliedConfig,
      args.startedAt,
      finishedAt,
      {
        readOnly: false,
        executed: args.outcome.executed,
        committed: args.outcome.committed,
        skipped: args.outcome.skipped?.reason === 'pin-matches-history',
        verification: {
          passed: args.outcome.verification?.passed ?? false,
          diffs: args.outcome.verification?.diffs ?? [],
        },
        inspection: args.outcome.inspection,
        preexisting: args.outcome.preexisting,
        history: args.outcome.historyKey,
        siteId: args.siteIdResolved === '' ? undefined : args.siteIdResolved,
      },
    );
    writeManifestArtifact(resolved, artifact);
    console.log(`up: manifest artifact written to ${resolved}`);
  }

  return 0;
}

async function commandVerify(args: readonly string[]): Promise<number> {
  const parsed = parseCommandArgs('verify', args);
  const stream: MigrationStream = parsed.stream ?? 'control-plane';
  const { fileArg, manifestOut } = parsed;
  const siteIdResolved = requireSiteForTenant('verify', stream, parsed.siteId);
  const { filePath } = resolveRegisteredFile(stream, fileArg);
  let buffer: Buffer;
  try {
    buffer = readFileSync(filePath);
  } catch {
    throw new CliFailure(`Cannot read migration file at ${filePath}.`);
  }
  const startedAt = new Date().toISOString();
  const { plan, entry } = validatePinnedMigration(filePath, buffer, { stream });

  let config: PostgresConnectionConfig;
  let safeTarget: string;
  if (stream === 'tenant') {
    const r = await resolveTenantTarget('verify', siteIdResolved);
    config = r.config;
    safeTarget = r.safeTarget;
  } else {
    config = loadConfigOrFail();
    safeTarget = describeTarget(config);
  }
  console.log(`Verifying schema manifest for stream ${entry.stream}, target ${safeTarget}:`);

  const outcome = await verifyMigration(config, filePath, buffer, {
    stream: entry.stream,
    site:
      siteIdResolved === ''
        ? undefined
        : { siteId: siteIdResolved, migrationSecretReference: null },
  });
  const finishedAt = new Date().toISOString();
  const { verification, inspection } = outcome;

  for (const entryTable of inspection.tables) {
    console.log(
      `  ${entryTable.table.padEnd(36)} ${entryTable.exists ? 'EXISTS ' : 'MISSING'} (${entryTable.columns.length} columns)`,
    );
  }

  if (manifestOut !== undefined) {
    const resolved = validateManifestOutPath(manifestOut);
    const artifact = buildManifestArtifact(entry, plan, config, startedAt, finishedAt, {
      readOnly: true,
      executed: false,
      committed: false,
      skipped: false,
      verification: { passed: verification.passed, diffs: verification.diffs },
      inspection,
      siteId: siteIdResolved === '' ? undefined : siteIdResolved,
    });
    writeManifestArtifact(resolved, artifact);
    console.log(`verify: manifest artifact written to ${resolved}`);
  }

  if (!verification.passed) {
    console.error('verify: schema manifest mismatch.');
    for (const diff of verification.diffs) {
      console.error(`  - ${diff}`);
    }
    return 1;
  }

  console.log('verify: schema manifest + supplemental PASS.');
  return 0;
}

export async function run(argv: readonly string[]): Promise<number> {
  const [command, ...args] = argv;
  switch (command) {
    case 'plan':
      return commandPlan(args);
    case 'status':
      return commandStatus(args);
    case 'up':
      return commandUp(args);
    case 'verify':
      return commandVerify(args);
    default:
      console.error(USAGE);
      return 2;
  }
}

async function main(): Promise<void> {
  try {
    process.exitCode = await run(process.argv.slice(2));
  } catch (error) {
    const secrets: string[] = [];
    try {
      const raw = process.env[CONNECTION_STRING_ENV_VAR];
      if (raw !== undefined && raw.trim() !== '') {
        secrets.push(loadConnectionConfigFromEnv(process.env).password);
      }
    } catch {
      // Config itself failed; parser errors never echo values.
    }
    const message = redactSecrets((error as Error).message ?? String(error), secrets);
    console.error(`Error: ${message}`);
    process.exitCode = 1;
  }
}

if (process.argv[1] && import.meta.url === pathToFileURL(process.argv[1]).href) {
  void main();
}
