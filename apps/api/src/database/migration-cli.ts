/**
 * migration-cli: ESM command-line entry point for the identity control-plane migration.
 *
 * Commands:
 *   plan [file.sql]          Offline (NO connection): validates the registry pin and safety
 *                            policy for registered migrations, prints file, SHA-256, bytes,
 *                            tables and style. `plan` without args processes only registered
 *                            migrations; stray .sql files are ignored.
 *   status                   Connects using ConnectionStrings__DefaultConnection and prints ONLY
 *                            metadata (current_database/current_user/version + information_schema
 *                            presence of all known target tables). Missing tables are reported,
 *                            not an error (exit 0).
 *   up [--manifest-out f] [file.sql]
 *                            Applies the canonical derived body of a registered migration inside
 *                            a single SERIALIZABLE transaction with full schema verification.
 *   verify [--manifest-out <file.json>] [file.sql]
 *                            Read-only verification of the registered schema manifest.
 *
 * This file is the only place that touches `process.env` (the exact variable
 * `ConnectionStrings__DefaultConnection`) and `process.exit`/exit codes. SQL files are
 * resolved from the package cwd (`migrations/`), which the pnpm scripts guarantee.
 */
import { readFileSync, renameSync, statSync, writeFileSync } from 'node:fs';
import path from 'node:path';
import { pathToFileURL } from 'node:url';
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
} from './migration-plan.js';
import {
  applyMigration,
  fetchStatus,
  verifyMigration,
  type TargetInspection,
} from './migration-runner.js';
import {
  findRegistryEntry,
  listRegisteredMigrations,
  validatePinnedMigration,
  type MigrationRegistryEntry,
} from './migration-registry.js';

const USAGE = `Usage (run from the apps/api package directory):
  node dist/database/migration-cli.js plan [file.sql]
  node dist/database/migration-cli.js status
  node dist/database/migration-cli.js up [--manifest-out <file.json>] [file.sql]
  node dist/database/migration-cli.js verify [--manifest-out <file.json>] [file.sql]

Required environment variable for status/up/verify:
  ${CONNECTION_STRING_ENV_VAR}   (never printed; fail-closed when missing)
`;

const SAFE_FILE_RE = /^[A-Za-z0-9][A-Za-z0-9._-]*\.sql$/;

/** Raised for operational failures that should map to a non-zero exit code. */
class CliFailure extends Error {
  constructor(message: string) {
    super(message);
    this.name = 'CliFailure';
  }
}

function migrationsDir(): string {
  return path.resolve(process.cwd(), MIGRATIONS_DIR_NAME);
}

function resolveRegisteredFile(
  dir: string,
  requested: string | undefined,
): { filePath: string; entry: MigrationRegistryEntry } {
  const fileName = requested ?? IDENTITY_MIGRATION_FILE;
  if (!SAFE_FILE_RE.test(fileName) || fileName.includes('..')) {
    throw new CliFailure(
      `Invalid migration file name; expected a plain "*.sql" file name inside ${dir}.`,
    );
  }
  const entry = findRegistryEntry(fileName);
  if (entry === undefined) {
    throw new CliFailure(
      `Migration file "${fileName}" is not registered in the migration registry.`,
    );
  }
  return { filePath: path.join(dir, fileName), entry };
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
  console.log(`  file:   ${plan.path}`);
  console.log(`  sha256: ${plan.sha256}`);
  console.log(`  bytes:  ${plan.byteLength}`);
  console.log(`  style:  ${entry.style}`);
  console.log(`  workId: ${entry.workId}`);
  console.log(`  tables: ${plan.tables.join(', ')}`);
  console.log(
    '  safety: PASS (registry-pinned, allowlisted CREATE TABLE set, no destructive statements)',
  );
}

async function commandPlan(args: readonly string[]): Promise<number> {
  const dir = migrationsDir();
  const entries =
    args.length > 0
      ? args.map((arg) => resolveRegisteredFile(dir, arg).entry)
      : listRegisteredMigrations();

  if (entries.length === 0) {
    throw new CliFailure('No registered migrations to plan.');
  }

  console.log('Migration plan (offline analysis; no database connection made):');
  let failures = 0;
  for (const entry of entries) {
    const filePath = path.join(dir, entry.file);
    try {
      const { plan } = readRegisteredPlan(filePath);
      printPlan(plan, entry);
    } catch (error) {
      failures += 1;
      console.error(`  ${entry.file}: REJECTED`);
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

async function commandStatus(): Promise<number> {
  const config = loadConfigOrFail();
  console.log(`Migration status (metadata only) for target ${describeTarget(config)}:`);
  const inspection = await fetchStatus(config);
  console.log(`  server database: ${inspection.server.database}`);
  console.log(`  server user:     ${inspection.server.user}`);
  console.log(`  server version:  ${inspection.server.version}`);
  for (const entry of inspection.tables) {
    if (entry.exists) {
      console.log(`  ${entry.table.padEnd(20)} EXISTS   (${entry.columns.length} columns)`);
    } else {
      console.log(`  ${entry.table.padEnd(20)} MISSING`);
    }
  }
  const missing = inspection.tables.filter((entry) => !entry.exists).map((entry) => entry.table);
  if (missing.length === inspection.tables.length) {
    console.log('result: no target tables exist yet; "up" is applicable.');
  } else if (missing.length > 0) {
    console.log(
      `result: partially present; missing: ${missing.join(', ')}. Applicability is migration-specific; run plan and explicit up for the registered migration.`,
    );
  } else {
    console.log('result: all target tables exist.');
  }
  return 0;
}

interface CommandArgs {
  fileArg: string | undefined;
  manifestOut: string | undefined;
}

export function parseCommandArgs(command: string, args: readonly string[]): CommandArgs {
  let fileArg: string | undefined;
  let manifestOut: string | undefined;
  let i = 0;
  while (i < args.length) {
    const arg = args[i];
    if (arg === undefined) break;
    if (arg === '--manifest-out') {
      const value = args[i + 1];
      if (value === undefined) {
        throw new CliFailure('--manifest-out requires a file path.');
      }
      manifestOut = value;
      i += 2;
      continue;
    }
    if (arg.startsWith('--manifest-out=')) {
      manifestOut = arg.slice('--manifest-out='.length);
      i += 1;
      continue;
    }
    if (fileArg !== undefined) {
      throw new CliFailure(`${command} takes at most one migration file name.`);
    }
    fileArg = arg;
    i += 1;
  }
  return { fileArg, manifestOut };
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
  sha256: string;
  bytes: number;
  style: string;
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
}

function validateManifestOutPath(raw: string): string {
  const resolved = path.resolve(process.cwd(), raw);
  const cwd = path.resolve(process.cwd());
  // Accept absolute paths or paths that stay within cwd.
  if (!path.isAbsolute(raw) && !resolved.toLowerCase().startsWith(cwd.toLowerCase() + path.sep)) {
    throw new CliFailure(
      `Manifest output path "${raw}" escapes the current working directory. ` +
        'Use a path inside cwd or an absolute path.',
    );
  }
  const dir = path.dirname(resolved);
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

export function writeManifestArtifact(resolvedPath: string, artifact: ManifestArtifact): void {
  const tmp = `${resolvedPath}.tmp-${process.pid}-${Date.now()}`;
  writeFileSync(tmp, `${JSON.stringify(artifact, null, 2)}\n`);
  renameSync(tmp, resolvedPath);
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
    verification: { passed: boolean; diffs: readonly string[] };
    inspection: TargetInspection;
    preexisting?: readonly string[];
  },
): ManifestArtifact {
  return {
    workId: entry.workId,
    command: outcome.readOnly ? 'verify' : 'up',
    file: plan.file,
    sha256: plan.sha256,
    bytes: plan.byteLength,
    style: entry.style,
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
  };
}

async function commandUp(args: readonly string[]): Promise<number> {
  const startedAt = new Date().toISOString();
  const { fileArg, manifestOut } = parseCommandArgs('up', args);
  const dir = migrationsDir();
  const { filePath } = resolveRegisteredFile(dir, fileArg);

  // Read the bytes exactly ONCE: the same buffer feeds the registry pin check, the SHA-256
  // that is printed, and the canonical body derivation. No TOCTOU between them.
  let buffer: Buffer;
  try {
    buffer = readFileSync(filePath);
  } catch {
    throw new CliFailure(`Cannot read migration file at ${filePath}.`);
  }
  const { plan, entry } = validatePinnedMigration(filePath, buffer);
  const config = loadConfigOrFail();

  console.log(`Applying migration to ${describeTarget(config)}`);
  console.log(`  file:    ${plan.path}`);
  console.log(`  sha256:  ${plan.sha256}  (source file hash)`);
  console.log(`  bytes:   ${plan.byteLength}`);
  console.log(`  workId:  ${entry.workId}`);
  console.log(`  style:   ${entry.style}`);
  console.log(
    `  note:    executing the canonical derived body (BEGIN/COMMIT removed, IF NOT EXISTS stripped)`,
  );

  const outcome = await applyMigration(config, filePath, buffer);
  const finishedAt = new Date().toISOString();

  for (const entryTable of outcome.inspection.tables) {
    console.log(
      `  ${entryTable.table.padEnd(20)} ${entryTable.exists ? 'EXISTS ' : 'MISSING'} (${entryTable.columns.length} columns)`,
    );
  }

  if (!outcome.executed) {
    console.error(
      `ABORTED: existing target table(s) detected: ${outcome.preexisting.join(', ')}. ` +
        'This tool refuses to run against a database where any target table already exists.',
    );
    return 1;
  }

  const verification = outcome.verification;
  if (verification === null || !verification.passed) {
    console.error(`FAILED POST-VERIFICATION: ${verification?.diffs.join('; ') ?? 'unknown'}`);
    return 1;
  }

  console.log('up: migration applied, committed and verified (schema manifest PASS).');

  if (manifestOut !== undefined) {
    const resolved = validateManifestOutPath(manifestOut);
    const artifact = buildManifestArtifact(entry, plan, config, startedAt, finishedAt, {
      readOnly: false,
      executed: outcome.executed,
      committed: outcome.committed,
      verification: { passed: verification.passed, diffs: verification.diffs },
      inspection: outcome.inspection,
      preexisting: outcome.preexisting,
    });
    writeManifestArtifact(resolved, artifact);
    console.log(`up: manifest artifact written to ${resolved}`);
  }

  return 0;
}

async function commandVerify(args: readonly string[]): Promise<number> {
  const startedAt = new Date().toISOString();
  const { fileArg, manifestOut } = parseCommandArgs('verify', args);
  const dir = migrationsDir();
  const { filePath } = resolveRegisteredFile(dir, fileArg);
  let buffer: Buffer;
  try {
    buffer = readFileSync(filePath);
  } catch {
    throw new CliFailure(`Cannot read migration file at ${filePath}.`);
  }
  const { plan, entry } = validatePinnedMigration(filePath, buffer);
  const config = loadConfigOrFail();
  console.log(`Verifying schema manifest for target ${describeTarget(config)}:`);

  const outcome = await verifyMigration(config, filePath, buffer);
  const finishedAt = new Date().toISOString();
  const { verification, inspection } = outcome;

  for (const entryTable of inspection.tables) {
    console.log(
      `  ${entryTable.table.padEnd(20)} ${entryTable.exists ? 'EXISTS ' : 'MISSING'} (${entryTable.columns.length} columns)`,
    );
  }

  if (manifestOut !== undefined) {
    const resolved = validateManifestOutPath(manifestOut);
    const artifact = buildManifestArtifact(entry, plan, config, startedAt, finishedAt, {
      readOnly: true,
      executed: false,
      committed: false,
      verification: { passed: verification.passed, diffs: verification.diffs },
      inspection,
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

  console.log('verify: schema manifest PASS.');
  return 0;
}

export async function run(argv: readonly string[]): Promise<number> {
  const [command, ...args] = argv;
  switch (command) {
    case 'plan':
      return commandPlan(args);
    case 'status':
      if (args.length > 0) {
        console.error('status takes no arguments.');
        console.error(USAGE);
        return 2;
      }
      return commandStatus();
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
    // Scrub the configured password (when it was already parseable) from any error message.
    const secrets: string[] = [];
    try {
      const raw = process.env[CONNECTION_STRING_ENV_VAR];
      if (raw !== undefined && raw.trim() !== '') {
        secrets.push(loadConnectionConfigFromEnv(process.env).password);
      }
    } catch {
      // Config itself failed; parser errors never echo values, so there is nothing to redact.
    }
    const message = redactSecrets((error as Error).message ?? String(error), secrets);
    console.error(`Error: ${message}`);
    process.exitCode = 1;
  }
}

// ESM equivalent of `require.main === module`: execute only as an entry point.
if (process.argv[1] && import.meta.url === pathToFileURL(process.argv[1]).href) {
  void main();
}
