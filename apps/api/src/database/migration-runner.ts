/**
 * Database-facing operations for the migration CLI (`status`, `up` and `verify`).
 *
 * Safety properties:
 *  - Only catalog metadata is ever read. No business rows are selected, printed or copied.
 *  - `up` executes the canonical derived body inside a single runner-controlled transaction:
 *    BEGIN ISOLATION LEVEL SERIALIZABLE; SET LOCAL search_path = public, pg_catalog;
 *    pg_advisory_xact_lock; privilege check; absence-of-target-tables check; body execution;
 *    exhaustive schema verification; COMMIT only on PASS, otherwise ROLLBACK.
 *  - No session-level advisory lock/unlock is used; the transaction-bound lock is sufficient
 *    and is reentrant with the lock statement left inside the canonical body.
 *  - The Client is always closed (`end()` in `finally`), even on failure paths.
 *  - Timeouts bound every phase: connect 15 s, server-side lock wait 30 s, per-statement and
 *    client-side query timeout 5 min.
 *
 * This module imports `pg` and must never be loaded by the offline `plan` command's tests.
 */
import { Client } from 'pg';
import type { PostgresConnectionConfig } from './connection-config.js';
import { CONTROL_PLANE_TABLES } from './migration-plan.js';
import {
  type MigrationRegistryEntry,
  deriveCanonicalBootstrapSql,
  validatePinnedMigration,
} from './migration-registry.js';
import {
  AUTH_SECURITY_SCHEMA_MANIFEST,
  IDENTITY_CONTROL_PLANE_SCHEMA_MANIFEST,
  TENANT_ROUTE_SCHEMA_MANIFEST,
  type SchemaManifest,
  type SchemaVerification,
  verifySchema,
} from './schema-manifest.js';

const TARGET_SCHEMA = 'public';

function manifestFor(entry: MigrationRegistryEntry): SchemaManifest {
  switch (entry.file) {
    case '0001_create_identity_control_plane.sql':
      return IDENTITY_CONTROL_PLANE_SCHEMA_MANIFEST;
    case '0002_create_auth_security_controls.sql':
      return AUTH_SECURITY_SCHEMA_MANIFEST;
    case '0003_create_tenant_route_catalog.sql':
      return TENANT_ROUTE_SCHEMA_MANIFEST;
    default: {
      const exhaustive: never = entry;
      throw new Error(`No schema manifest is registered for migration ${String(exhaustive)}.`);
    }
  }
}

const CLIENT_RUN_OPTIONS = {
  application_name: 'lu-migration-cli',
  connectionTimeoutMillis: 15_000,
  lock_timeout: 30_000,
  statement_timeout: 300_000,
  query_timeout: 300_000,
} as const;

export interface TablePresence {
  table: string;
  exists: boolean;
  columns: readonly string[];
}

export interface ServerMetadata {
  database: string;
  user: string;
  version: string;
}

export interface TargetInspection {
  server: ServerMetadata;
  tables: readonly TablePresence[];
}

export interface UpOutcome {
  /** Target tables found already present at preflight (migration NOT executed when non-empty). */
  preexisting: readonly string[];
  /** True when the SQL file was actually executed. */
  executed: boolean;
  /** True only when the migration transaction was committed. */
  committed: boolean;
  /** Verification result after execution (empty when not executed). */
  verification: SchemaVerification | null;
  /** Final metadata inspection after execution (or the preflight snapshot when aborted). */
  inspection: TargetInspection;
}

export interface VerifyOutcome {
  /** Schema verification result taken inside the read-only transaction. */
  verification: SchemaVerification;
  /** Target inspection snapshot taken inside the SAME read-only transaction. */
  inspection: TargetInspection;
}

interface QueryResultLike<T extends Record<string, unknown>> {
  rows: T[];
}

interface MetadataRow extends Record<string, unknown> {
  database: string | null;
  user: string | null;
  server_version: string | null;
}

interface TableNameRow extends Record<string, unknown> {
  table_name: string | null;
}

interface ColumnNameRow extends Record<string, unknown> {
  table_name: string | null;
  column_name: string | null;
}

interface PrivilegeRow extends Record<string, unknown> {
  can_create: boolean | null;
}

function newClient(config: PostgresConnectionConfig): Client {
  return new Client({ ...config, ...CLIENT_RUN_OPTIONS });
}

async function queryRows<T extends Record<string, unknown>>(
  client: Client,
  text: string,
  values?: unknown[],
): Promise<T[]> {
  const result = values
    ? await client.query<T>(text, values as never[])
    : await client.query<T>(text);
  return (result as QueryResultLike<T>).rows ?? [];
}

/** Reads server metadata and target-table presence/columns. Metadata only; no business data. */
export async function inspectTarget(client: Client): Promise<TargetInspection> {
  const metaRows = await queryRows<MetadataRow>(
    client,
    'SELECT current_database() AS database, current_user AS "user", version() AS server_version',
  );
  const meta = metaRows[0];
  if (meta === undefined) {
    throw new Error(
      'The database returned no metadata row for current_database()/current_user/version().',
    );
  }

  const tableRows = await queryRows<TableNameRow>(
    client,
    'SELECT table_name FROM information_schema.tables WHERE table_schema = $1 AND table_name = ANY($2::text[])',
    [TARGET_SCHEMA, [...CONTROL_PLANE_TABLES]],
  );
  const columnRows = await queryRows<ColumnNameRow>(
    client,
    'SELECT table_name, column_name FROM information_schema.columns WHERE table_schema = $1 AND table_name = ANY($2::text[]) ORDER BY table_name, ordinal_position',
    [TARGET_SCHEMA, [...CONTROL_PLANE_TABLES]],
  );

  const present = new Set(tableRows.map((row) => String(row.table_name ?? '')));
  const columnsByTable = new Map<string, string[]>();
  for (const row of columnRows) {
    const table = String(row.table_name ?? '');
    const column = String(row.column_name ?? '');
    const list = columnsByTable.get(table);
    if (list === undefined) {
      columnsByTable.set(table, [column]);
    } else {
      list.push(column);
    }
  }

  const tables: TablePresence[] = CONTROL_PLANE_TABLES.map((table) => ({
    table,
    exists: present.has(table),
    columns: columnsByTable.get(table) ?? [],
  }));

  return {
    server: {
      database: String(meta.database ?? ''),
      user: String(meta.user ?? ''),
      version: String(meta.server_version ?? ''),
    },
    tables,
  };
}

/** `status`: connect, read metadata-only state, always disconnect. */
export async function fetchStatus(config: PostgresConnectionConfig): Promise<TargetInspection> {
  const client = newClient(config);
  let connected = false;
  try {
    await client.connect();
    connected = true;
    return await inspectTarget(client);
  } finally {
    if (connected) {
      await client.end().catch(() => undefined);
    }
  }
}

/**
 * `verify`: read-only catalog verification of the pinned migration's schema manifest.
 * Validates the file pin before opening a connection, then captures both the schema
 * verification and the target inspection inside a single REPEATABLE READ READ ONLY
 * transaction with search_path = public. Never executes DDL.
 */
export async function verifyMigration(
  config: PostgresConnectionConfig,
  filePath: string,
  content: Buffer,
): Promise<VerifyOutcome> {
  // Validate pin offline (no DB access).
  const { entry } = validatePinnedMigration(filePath, content);

  const client = newClient(config);
  let connected = false;
  try {
    await client.connect();
    connected = true;
    await client.query('BEGIN ISOLATION LEVEL REPEATABLE READ READ ONLY');
    await client.query('SET LOCAL search_path = public, pg_catalog');
    const verification = await verifySchema(client, manifestFor(entry));
    // Inspection is intentionally captured inside the same read-only transaction so the
    // manifest artifact references a single, consistent catalog snapshot.
    const inspection = await inspectTarget(client);
    await client.query('COMMIT');
    return { verification, inspection };
  } catch (error) {
    if (connected) {
      await client.query('ROLLBACK').catch(() => undefined);
    }
    throw error;
  } finally {
    if (connected) {
      await client.end().catch(() => undefined);
    }
  }
}

async function assertCanCreateSchema(client: Client, entry: MigrationRegistryEntry): Promise<void> {
  const rows = await queryRows<PrivilegeRow>(
    client,
    'SELECT has_schema_privilege($1, $2) AS can_create',
    [entry.targetSchema, 'CREATE'],
  );
  const canCreate = rows[0]?.can_create === true;
  if (!canCreate) {
    throw new Error(`Current user lacks CREATE privilege on schema "${entry.targetSchema}".`);
  }
}

async function findPreexistingTables(
  client: Client,
  entry: MigrationRegistryEntry,
): Promise<string[]> {
  const rows = await queryRows<TableNameRow>(
    client,
    'SELECT table_name FROM information_schema.tables WHERE table_schema = $1 AND table_name = ANY($2::text[])',
    [entry.targetSchema, [...entry.targetTables]],
  );
  return rows
    .map((row) => row.table_name)
    .filter((name): name is string => typeof name === 'string' && name !== '');
}

/**
 * `up`: registry-pinned, canonical-body execution with fail-closed transaction control.
 *
 * 1. Reads the bytes once.
 * 2. Validates the registry pin + generic policy.
 * 3. Derives the canonical body deterministically.
 * 4. Opens a single SERIALIZABLE transaction, sets search_path, acquires the advisory xact lock.
 * 5. Verifies CREATE privilege and that no target table exists in public.
 * 6. Executes the canonical body.
 * 7. Runs exhaustive schema verification.
 * 8. COMMIT on PASS, ROLLBACK on any failure.
 */
export async function applyMigration(
  config: PostgresConnectionConfig,
  filePath: string,
  content: Buffer,
): Promise<UpOutcome> {
  // Step 1-3: single read, pin check, policy re-check, canonical derivation.
  const { entry } = validatePinnedMigration(filePath, content);
  const canonical = deriveCanonicalBootstrapSql(entry, content.toString('utf8'));

  const client = newClient(config);
  let connected = false;
  try {
    await client.connect();
    connected = true;

    await client.query('BEGIN ISOLATION LEVEL SERIALIZABLE');
    try {
      await client.query('SET LOCAL search_path = public, pg_catalog');
      await client.query('SELECT pg_advisory_xact_lock(hashtext($1))', [entry.advisoryLockKey]);

      await assertCanCreateSchema(client, entry);
      const preexisting = await findPreexistingTables(client, entry);
      if (preexisting.length > 0) {
        await client.query('ROLLBACK');
        const inspection = await inspectTarget(client);
        return {
          preexisting,
          executed: false,
          committed: false,
          verification: null,
          inspection,
        };
      }

      await client.query(canonical.body);

      const verification = await verifySchema(client, manifestFor(entry));
      if (!verification.passed) {
        await client.query('ROLLBACK');
        throw new Error(
          `Schema verification failed after executing migration: ${verification.diffs.join('; ')}`,
        );
      }

      // Capture the final inspection inside the SAME transaction, before COMMIT. This
      // guarantees the returned snapshot is consistent with the verification and avoids
      // issuing queries after the transaction has committed.
      const inspection = await inspectTarget(client);

      await client.query('COMMIT');
      return {
        preexisting: [],
        executed: true,
        committed: true,
        verification,
        inspection,
      };
    } catch (error) {
      await client.query('ROLLBACK').catch(() => undefined);
      throw error;
    }
  } finally {
    if (connected) {
      await client.end().catch(() => undefined);
    }
  }
}
