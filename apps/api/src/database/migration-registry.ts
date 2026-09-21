/**
 * Immutable migration registry for the identity control-plane migration.
 *
 * 0001 is pinned by exact SHA-256 and byte length. `plan`, `status`, `up` and `verify`
 * must reject any migration file that is not registered or whose hash/bytes diverge.
 *
 * The canonical execution body for the bootstrap-style 0001 migration is derived
 * deterministically from the pinned file: the initial BEGIN and final COMMIT are removed,
 * `CREATE TABLE IF NOT EXISTS` becomes `CREATE TABLE`, and index `IF NOT EXISTS` clauses
 * are stripped. The hash printed to operators remains the source-file hash.
 */
import { createHash } from 'node:crypto';
import {
  type MigrationPlan,
  type ControlPlaneTable,
  AUTH_SECURITY_TARGET_TABLES,
  BASELINE_TARGET_TABLES,
  TENANT_ROUTE_TARGET_TABLES,
  buildMigrationPlan,
  sanitizeSql,
} from './migration-plan.js';

export const MIGRATION_REGISTRY = [
  {
    workId: 'MIG-F3-PG-IDENTITY-001',
    file: '0001_create_identity_control_plane.sql',
    style: 'bootstrap' as const,
    sha256: 'a7698ea53a443d42e87bf905ae9e0f7060923a26199bc06ad23f17401e1bc118',
    byteLength: 4491,
    advisoryLockKey: 'lu:identity-control-plane:0001',
    targetSchema: 'public',
    targetTables: [...BASELINE_TARGET_TABLES] as readonly ControlPlaneTable[],
    expectedCreateTableCount: 4,
    expectedIndexNames: [
      'ux_lu_site_code_lower',
      'ux_lu_user_email_lower',
      'ix_lu_site_membership_site_status',
      'ix_lu_session_user',
      'ix_lu_session_active_expiry',
    ] as const,
  },
  {
    workId: 'MIG-F3-AUTH-SECURITY-014',
    file: '0002_create_auth_security_controls.sql',
    style: 'additive' as const,
    sha256: '335bd7b1ed75b557e1618283d2272c4353bb029ca2fd28ee626566d3d93ff336',
    byteLength: 2727,
    advisoryLockKey: 'lu:identity-control-plane:0002',
    targetSchema: 'public',
    targetTables: [...AUTH_SECURITY_TARGET_TABLES] as readonly ControlPlaneTable[],
    expectedCreateTableCount: 2,
    expectedIndexNames: [
      'ix_lu_auth_rate_limit_reset',
      'ix_lu_security_event_type_time',
      'ix_lu_security_event_user_time',
    ] as const,
  },
  {
    workId: 'MIG-F4-ROUTE-CATALOG-008',
    file: '0003_create_tenant_route_catalog.sql',
    style: 'additive' as const,
    sha256: 'dfc0dc9a4cea47c584d5cc1f8705d30a3df9cc589f8702b9afeb15b43dd851f1',
    byteLength: 1692,
    advisoryLockKey: 'lu:identity-control-plane:0003',
    targetSchema: 'public',
    targetTables: [...TENANT_ROUTE_TARGET_TABLES] as readonly ControlPlaneTable[],
    expectedCreateTableCount: 1,
    expectedIndexNames: [
      'ux_lu_tenant_route_secret_reference',
      'ix_lu_tenant_route_state',
    ] as const,
  },
] as const;

export type MigrationRegistryEntry = (typeof MIGRATION_REGISTRY)[number];

export class MigrationRegistryError extends Error {
  constructor(message: string) {
    super(message);
    this.name = 'MigrationRegistryError';
  }
}

export function listRegisteredMigrations(): readonly MigrationRegistryEntry[] {
  return MIGRATION_REGISTRY;
}

export function findRegistryEntry(fileName: string): MigrationRegistryEntry | undefined {
  return MIGRATION_REGISTRY.find((entry) => entry.file === fileName);
}

export function sha256Hex(content: Buffer | string): string {
  const buf = Buffer.isBuffer(content) ? content : Buffer.from(content, 'utf8');
  return createHash('sha256').update(buf).digest('hex');
}

export interface ValidatedMigration {
  readonly entry: MigrationRegistryEntry;
  readonly plan: MigrationPlan;
}

/**
 * Validates that a migration file is registered and that its bytes match the pinned
 * digest exactly. Returns the registry entry and a generic policy plan (which is also
 * re-checked for destructive-statement violations).
 */
export function validatePinnedMigration(
  filePath: string,
  content: Buffer | string,
): ValidatedMigration {
  const buf = Buffer.isBuffer(content) ? content : Buffer.from(content, 'utf8');
  const fileName = filePath.split(/[\\/]/).pop() ?? filePath;
  const entry = findRegistryEntry(fileName);
  if (entry === undefined) {
    throw new MigrationRegistryError(
      `Migration file "${fileName}" is not registered in the migration registry. ` +
        'Only registry-pinned migrations may be analyzed or executed.',
    );
  }
  const hash = sha256Hex(buf);
  if (hash !== entry.sha256) {
    throw new MigrationRegistryError(
      `SHA-256 mismatch for registered migration "${fileName}". ` +
        `Expected ${entry.sha256}, got ${hash}. The file has been modified, corrupted, or is not the approved bootstrap migration.`,
    );
  }
  if (buf.length !== entry.byteLength) {
    throw new MigrationRegistryError(
      `Byte length mismatch for registered migration "${fileName}". ` +
        `Expected ${entry.byteLength}, got ${buf.length}.`,
    );
  }
  const plan = buildMigrationPlan(filePath, buf, entry.targetTables);
  return { entry, plan };
}

export interface CanonicalBody {
  readonly body: string;
  readonly derivedFromSha256: string;
  readonly createTableCount: number;
  readonly uniqueIndexCount: number;
  readonly plainIndexCount: number;
}

function splitSanitizedStatements(code: string): string[] {
  return code
    .split(';')
    .map((s) => s.trim())
    .filter((s) => s !== '');
}

function assertEnvelope(
  entry: MigrationRegistryEntry,
  code: string,
  issues: readonly string[],
): void {
  if (issues.length > 0) {
    throw new MigrationRegistryError(
      `Migration "${entry.file}" has structural issues that prevent canonical derivation: ${issues.join('; ')}`,
    );
  }

  const statements = splitSanitizedStatements(code);
  if (statements.length === 0) {
    throw new MigrationRegistryError(
      `Migration "${entry.file}" contains no statements after comment removal.`,
    );
  }

  const first = statements[0]?.toUpperCase() ?? '';
  const last = statements[statements.length - 1]?.toUpperCase() ?? '';
  const txCommands = statements.filter((s) => /^(BEGIN|COMMIT|ROLLBACK|SAVEPOINT|END)$/i.test(s));

  const violations: string[] = [];
  if (first !== 'BEGIN') violations.push('the first statement must be BEGIN');
  if (last !== 'COMMIT') violations.push('the last statement must be COMMIT');
  if (txCommands.length !== 2) {
    violations.push('the envelope must contain exactly one BEGIN and one COMMIT');
  }
  const extraTx = statements.filter((s) => /^(ROLLBACK|SAVEPOINT|END)$/i.test(s));
  if (extraTx.length > 0) {
    violations.push('ROLLBACK/SAVEPOINT/END are not allowed inside the migration envelope');
  }

  if (violations.length > 0) {
    throw new MigrationRegistryError(
      `Migration "${entry.file}" has an invalid transaction envelope: ${violations.join('; ')}.`,
    );
  }
}

/**
 * Derives the canonical execution body for a bootstrap-style registered migration.
 *
 * Safety checks performed here:
 *  - exact BEGIN ... COMMIT envelope (one BEGIN at start, one COMMIT at end),
 *  - no other transaction-control statements,
 *  - expected number of `CREATE TABLE IF NOT EXISTS` statements,
 *  - expected set of index names after stripping `IF NOT EXISTS`.
 *
 * The returned `body` is what the runner executes inside its own controlled transaction.
 */
export function deriveCanonicalBootstrapSql(
  entry: MigrationRegistryEntry,
  sql: string,
): CanonicalBody {
  const { code, issues } = sanitizeSql(sql);
  assertEnvelope(entry, code, issues);

  let body = sql;

  // Strip the initial BEGIN statement. The envelope assertion guarantees it is the first.
  const beginMatch = /^\s*BEGIN\s*;/i.exec(body);
  if (beginMatch === null) {
    throw new MigrationRegistryError(
      `Migration "${entry.file}" does not start with a removable BEGIN statement.`,
    );
  }
  body = body.slice(beginMatch[0].length);

  // Strip the final COMMIT statement.
  const commitMatch = /COMMIT\s*;\s*$/i.exec(body);
  if (commitMatch === null) {
    throw new MigrationRegistryError(
      `Migration "${entry.file}" does not end with a removable COMMIT statement.`,
    );
  }
  body = body.slice(0, commitMatch.index);

  let createTableCount = 0;
  body = body.replace(/\bCREATE\s+TABLE\s+IF\s+NOT\s+EXISTS\b/gi, () => {
    createTableCount += 1;
    return 'CREATE TABLE';
  });

  let uniqueIndexCount = 0;
  body = body.replace(/\bCREATE\s+UNIQUE\s+INDEX\s+IF\s+NOT\s+EXISTS\b/gi, () => {
    uniqueIndexCount += 1;
    return 'CREATE UNIQUE INDEX';
  });

  let plainIndexCount = 0;
  body = body.replace(/\bCREATE\s+INDEX\s+IF\s+NOT\s+EXISTS\b/gi, () => {
    plainIndexCount += 1;
    return 'CREATE INDEX';
  });

  if (createTableCount !== entry.expectedCreateTableCount) {
    throw new MigrationRegistryError(
      `Migration "${entry.file}" declares ${createTableCount} CREATE TABLE IF NOT EXISTS ` +
        `statements; ${entry.expectedCreateTableCount} were expected.`,
    );
  }

  // Verify the index names match the expected allowlist after stripping IF NOT EXISTS.
  const indexNamePattern = /\bCREATE\s+(?:UNIQUE\s+)?INDEX\s+(\S+)/gi;
  const seenIndexNames = new Set<string>();
  for (const match of body.matchAll(indexNamePattern)) {
    const name = match[1];
    if (name !== undefined) seenIndexNames.add(name.toLowerCase());
  }
  const expectedNames = new Set(entry.expectedIndexNames.map((n) => n.toLowerCase()));
  for (const name of expectedNames) {
    if (!seenIndexNames.has(name)) {
      throw new MigrationRegistryError(
        `Migration "${entry.file}" is missing expected index "${name}" after canonical derivation.`,
      );
    }
  }
  for (const name of seenIndexNames) {
    if (!expectedNames.has(name)) {
      throw new MigrationRegistryError(
        `Migration "${entry.file}" contains unexpected index "${name}" after canonical derivation.`,
      );
    }
  }

  return {
    body,
    derivedFromSha256: entry.sha256,
    createTableCount,
    uniqueIndexCount,
    plainIndexCount,
  };
}
