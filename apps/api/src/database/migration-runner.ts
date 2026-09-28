/**
 * Database-facing operations for the migration CLI (`status`, `up`, `verify`).
 *
 * Safety properties (W10/W11/W13A):
 *  - Only catalog metadata is ever read. No business rows are selected or
 *    printed. The exception is `lu_tenant_route.migration_secret_reference`
 *    which is referenced by name only and never echoed.
 *  - `up` executes the canonical LF-derived body inside a single SERIALIZABLE
 *    transaction:
 *      BEGIN, SET LOCAL search_path,
 *      migrator preflight (session role + lu_auth_migrator existence +
 *        membership + SET LOCAL ROLE lu_auth_migrator + CREATE on target
 *        schema + lu_auth_runtime CREATE/ownership on public),
 *      stream-common advisory lock,
 *      per-entry advisory lock,
 *      CREATE-privilege check (as lu_auth_migrator),
 *      history probe (metadata-only),
 *      ledger contiguous-pin check (when history exists),
 *      pin-at-ordinal replay check (when maxOrdinal >= entry.ordinal),
 *      preexistence gate (0001 / 0006 / 0013 only, gated on history existence),
 *      body,
 *      schema + supplemental verification,
 *      history re-probe + backfill (only when lu_migration_history exists;
 *        for CP 0006 / tenant 0013 with a pre-body probe of FALSE the
 *        runner re-probes metadata INSIDE the same transaction after
 *        verification and requires the newly-created table to exist; the
 *        same COMMIT then commits both the schema and the pin rows, never
 *        two separate commits).
 *      Inspection runs BEFORE the COMMIT (no catalog query after COMMIT).
 *  - History ledger: an `INSERT ... ON CONFLICT (stream, ordinal) DO NOTHING`
 *    stamps every pin in the same transaction. After each successful insert
 *    the runner SELECTs the row back and re-compares every field (stream,
 *    ordinal, relative_path, sha256, bytes, work_id) — a silent conflict
 *    that keeps a different pin is rejected as an immutable pin violation.
 *  - Ledger contiguity: when `lu_migration_history` exists the runner reads
 *    every row for the entry's stream, requires ordinals to be exactly
 *    `1..max` with no gap, requires `max <= current registry length`, and
 *    re-compares every documented field of every recorded pin against the
 *    corresponding registry entry. A missing old row with a later max
 *    rejects the apply before any body execution. The probe is metadata-only
 *    (information_schema.tables) so a missing history table never poisons
 *    the live transaction with a swallowed 42P01 error.
 *  - The apply path encodes two distinct entry-level booleans
 *    (W16A / proven B1 fix):
 *      * `requiresCleanState` (bootstrap 0001, CP 0006, tenant 0013):
 *        this entry may only land on a DB where (a) NO target table
 *        already exists OR (b) the exact contiguous pin row at
 *        `entry.ordinal` exists in `lu_migration_history`. Used to
 *        gate the partial-state preexisting branch.
 *      * `createsHistory` (CP 0006, tenant 0013 ONLY): this entry is
 *        the one that creates `lu_migration_history`. Used to gate
 *        the post-body metadata re-probe and ledger backfill inside
 *        the SERIALIZABLE transaction; early migrations (0001-0005
 *        CP, 0001-0012 tenant) and ACL-only entries never trigger
 *        the re-probe and never backfill.
 *    The intersection (`requiresCleanState && createsHistory`) names
 *    the entries that both gate preexisting tables AND create the
 *    ledger; the difference (`requiresCleanState && !createsHistory`)
 *    names bootstrap 0001 only.
 *  - Clean-state preexisting rule (split into two distinct cases):
 *      (a) bootstrap 0001 with preexisting tables AND history absent:
 *          the runner fails closed BEFORE any body / relation query.
 *          0001 is the FIRST entry in the CP stream and runs BEFORE
 *          `lu_migration_history` exists; a pre-history preexisting
 *          set means the DB is in a half-applied state that the
 *          bootstrap contract forbids silently masking by re-running
 *          the idempotent body.
 *      (b) CP 0006 / tenant 0013 with preexisting tables:
 *           * AND history absent: the body is allowed to run because
 *             0006 / 0013 is the entry that creates the ledger. The
 *             post-body metadata re-probe re-checks this contract:
 *             a body that silently fails to create the table forces
 *             a ROLLBACK before any INSERT.
 *           * AND history present AND exact pin row present: skip the
 *             body and run the verify path (handled by the replay
 *             shortcut above).
 *           * AND history present AND NO pin row: fail closed
 *             (the DB has the tables AND the ledger yet the registered
 *             pin row for entry.ordinal is missing — half-apply,
 *             refuse to silently re-run).
 *  - History re-probe for createsHistory entries (W16A): when the
 *    pre-body probe said the history table does NOT exist (fresh
 *    apply of the entry that creates the ledger), the runner re-probes
 *    the relation metadata INSIDE the same transaction AFTER the body
 *    and the schema / supplemental verifications. The newly-created
 *    `lu_migration_history` MUST be visible at this point; if the body
 *    silently failed to create the relation the runner ROLLBACKs and
 *    surfaces a fail-closed error before any COMMIT. Only when the
 *    re-probe confirms the table exists does the runner backfill
 *    `1..entry.ordinal` exact pins and re-read / re-compare every row.
 *    Early migrations (0001-0005 CP, 0001-0012 tenant) and ACL-only
 *    entries (CP 0005, tenant 0006) are unaffected: they never re-probe
 *    and never backfill the ledger. The replay path (matching contiguous
 *    history) bypasses this entire branch by taking the
 *    `pin-matches-history` shortcut before the body ever runs.
 *    Crucially, the re-probe uses the FRESH post-body metadata answer
 *    (a local `hasHistoryTableAfterBody`), NOT the stale pre-body
 *    `hasHistoryTable` variable, so the gating decision is made on the
 *    visible truth of the same transaction.
 *  - Replay / verify use the CURRENT cumulative recorded state (the max
 *    ordinal in lu_migration_history for the stream):
 *      * CP, max >= 6       -> POST_F2_CONTROL_PLANE_SCHEMA_MANIFEST.
 *      * CP, max = 5 / 4    -> route-final (0001..0003) + academic (0004).
 *      * CP, max = 3        -> TENANT_ROUTE_SCHEMA_MANIFEST (final).
 *      * CP, max = 2        -> AUTH_SECURITY_SCHEMA_MANIFEST (final).
 *      * CP, max = 1        -> IDENTITY_CONTROL_PLANE_SCHEMA_MANIFEST.
 *      * tenant, max >= 13  -> TENANT_FINAL_SCHEMA_MANIFEST.
 *      * tenant, max < 13   -> merged per-ordinal manifests (later overrides
 *                              earlier) + every applicable supplemental
 *                              through max.
 *    When maxOrdinal < entry.ordinal the runner verifies only through
 *    `entry.ordinal` (fresh-apply path); when maxOrdinal >= entry.ordinal
 *    the recorded pin at entry.ordinal MUST match the registered pin, and
 *    the cumulative manifest is taken through maxOrdinal.
 *  - Inspection covers the FINAL current state once history exists (POST_F2
 *    tables for the control-plane stream; TENANT_FINAL_TABLES for the tenant
 *    stream) so an old entry replaying on a final-state DB observes the same
 *    surface the cumulative manifest validates. On a fresh DB (no history)
 *    the entry's own touched-tables set is inspected.
 *  - Tenant stream requires explicit --site UUID. The DSN is resolved
 *    server-side through `tenant-config.ts`; the runtime_secret_reference
 *    role is never used for DDL. A tenant apply that arrives without a
 *    site context fails closed BEFORE the stream-common lock is taken.
 *  - ACL-only entries (CP 0005, tenant 0006) declare `schemaManifest: null`
 *    plus a non-null supplemental expectation. The runner must accept that
 *    pairing, run the supplemental verifier and commit on pass. Both-null
 *    is rejected.
 *  - Supplemental merge uses chronological entries through the recorded
 *    cap with LAST-ORDINAL-WINS for identical expectations: deduplication
 *    keys are (sequence name), (trigger table+name), (table ACL
 *    role+table+privilege+canonical columns), (sequence ACL
 *    role+sequence+privilege), (function ACL role+signature+privilege),
 *    (schema ACL role+schema+privilege). The CP0005 table-level
 *    INSERT/UPDATE true is overridden by the CP0006 table-level false; the
 *    CP0006 column-level grants remain true (different dedup key).
 *
 * This module imports `pg` and must never be loaded by the offline `plan`
 * command's tests.
 */
import { Client } from 'pg';
import type { PostgresConnectionConfig } from './connection-config.js';
import {
  CONTROL_PLANE_TABLES_POST_F2,
  DEFAULT_STREAM,
  TENANT_TABLES,
  type MigrationStream,
} from './migration-plan.js';
import {
  CONTROL_PLANE_REGISTRY,
  TENANT_REGISTRY,
  type MigrationRegistryEntry,
  deriveCanonicalBootstrapSql,
  resolveEntryPath,
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
import {
  ACADEMIC_CATALOG_SCHEMA_MANIFEST,
  POST_F2_CONTROL_PLANE_SCHEMA_MANIFEST,
  CONTROL_PLANE_SUPPLEMENTAL_EXPECTATIONS,
  type FunctionPrivilegeExpectation,
  type MigrationSupplementalExpectation,
  type SchemaPrivilegeExpectation,
  type SequenceExpectation,
  type SequencePrivilegeExpectation,
  type TablePrivilegeExpectation,
  type TriggerExpectation,
} from './schema-manifest-f2.js';
import {
  TENANT_FINAL_SCHEMA_MANIFEST,
  TENANT_FINAL_TABLES,
  TENANT_MIGRATION_MANIFESTS,
  TENANT_SUPPLEMENTAL_EXPECTATIONS,
} from './schema-manifest-tenant.js';

const TARGET_SCHEMA = 'public';
const MIGRATOR_ROLE = 'lu_auth_migrator';
const RUNTIME_ROLE = 'lu_auth_runtime';

export interface RunnerSiteContext {
  readonly siteId: string;
  readonly migrationSecretReference: string | null;
}

export interface RunnerContext {
  readonly stream?: MigrationStream;
  readonly site?: RunnerSiteContext;
  readonly appliedByUserId?: string | null;
}

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
  preexisting: readonly string[];
  executed: boolean;
  committed: boolean;
  verification: SchemaVerification | null;
  inspection: TargetInspection;
  historyKey?: { readonly stream: MigrationStream; readonly relativePath: string };
  skipped?: { readonly reason: 'pin-matches-history' };
}

export interface VerifyOutcome {
  verification: SchemaVerification;
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

interface MigrationHistoryRow extends Record<string, unknown> {
  stream: string | null;
  ordinal: number | null;
  relative_path: string | null;
  sha256: string | null;
  bytes: number | null;
  work_id: string | null;
}

interface MaxOrdinalRow extends Record<string, unknown> {
  max_ordinal: number | null;
}

interface BooleanRow extends Record<string, unknown> {
  exists: boolean | null;
}

void null;

const CLIENT_RUN_OPTIONS = {
  application_name: 'lu-migration-cli',
  connectionTimeoutMillis: 15_000,
  lock_timeout: 30_000,
  statement_timeout: 300_000,
  query_timeout: 300_000,
} as const;

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

/**
 * Resolve the per-ordinal schema manifest for an entry, with no knowledge of
 * any history on the target DB. This is the OFFLINE manifest used when the
 * runner has just executed the body and wants to verify the freshly-applied
 * surface (the cumulative state of the DB cannot exceed `entry.ordinal`).
 *
 * For tenant ordinal 6 (ACL-only) this returns `null`; that entry is
 * verified exclusively through its supplemental expectation. The runner
 * accepts `manifest=null + supplemental!=null` as the ACL-only contract; a
 * `(null, null)` pair is rejected.
 */
function manifestForEntry(entry: MigrationRegistryEntry): SchemaManifest | null {
  if (entry.stream === 'tenant') {
    const list =
      TENANT_MIGRATION_MANIFESTS[
        entry.ordinal as 1 | 2 | 3 | 4 | 5 | 6 | 7 | 8 | 9 | 10 | 11 | 12 | 13
      ];
    if (list === undefined) return null;
    return list.schemaManifest as SchemaManifest | null;
  }
  switch (entry.relativePath) {
    case '0001_create_identity_control_plane.sql':
      return IDENTITY_CONTROL_PLANE_SCHEMA_MANIFEST;
    case '0002_create_auth_security_controls.sql':
      return AUTH_SECURITY_SCHEMA_MANIFEST;
    case '0003_create_tenant_route_catalog.sql':
      return TENANT_ROUTE_SCHEMA_MANIFEST;
    case 'control-plane/0004_academic_catalogs.sql':
      return ACADEMIC_CATALOG_SCHEMA_MANIFEST as unknown as SchemaManifest;
    case 'control-plane/0006_mig001_users_identity.sql':
      return POST_F2_CONTROL_PLANE_SCHEMA_MANIFEST as unknown as SchemaManifest;
    default:
      return null;
  }
}

/**
 * Merge a list of per-ordinal tenant schema manifests into one cumulative
 * manifest. Later ordinals override earlier ones on a per-table basis. The
 * caller is responsible for ensuring `ordinals` is non-empty and in order.
 */
function mergeTenantManifests(ordinals: readonly number[]): SchemaManifest<string> {
  const tables: Record<string, unknown> = {};
  for (const ordinal of ordinals) {
    const manifest =
      TENANT_MIGRATION_MANIFESTS[ordinal as 1 | 2 | 3 | 4 | 5 | 6 | 7 | 8 | 9 | 10 | 11 | 12 | 13];
    if (manifest === undefined) continue;
    if (manifest.schemaManifest === null) continue;
    Object.assign(tables, manifest.schemaManifest.tables);
  }
  return {
    schema: 'public',
    tables: tables as SchemaManifest<string>['tables'],
  };
}

/**
 * Build the per-ordinal-or-final control-plane manifest used for max=4/5
 * replay / verify. The composition combines the route-final manifest (the
 * state after 0003) with the academic catalog tables (0004). 0005 is
 * ACL-only so it adds no tables: max=4 and max=5 produce the SAME merged
 * surface.
 */
function composeControlPlaneRouteFinalPlusAcademic(): SchemaManifest<string> {
  return {
    schema: 'public',
    tables: {
      ...TENANT_ROUTE_SCHEMA_MANIFEST.tables,
      ...ACADEMIC_CATALOG_SCHEMA_MANIFEST.tables,
    } as SchemaManifest<string>['tables'],
  };
}

/**
 * Resolve the CUMULATIVE manifest that must hold on the target DB given the
 * recorded history `maxOrdinal` for the entry's stream. The runner uses this
 * to validate any apply or verify path against the CURRENT state, never
 * against the entry's own ordinal-only manifest, so an old runner replaying
 * its entry against a final-state DB cannot pass on a synthetic PASS.
 *
 * Control-plane ladder (per MIG-001-F2-W13A):
 *  - max >= 6  -> POST_F2_CONTROL_PLANE_SCHEMA_MANIFEST
 *  - max 4 / 5 -> route-final (0001..0003) + academic tables (0004)
 *  - max = 3   -> TENANT_ROUTE_SCHEMA_MANIFEST (final state after 0003)
 *  - max = 2   -> AUTH_SECURITY_SCHEMA_MANIFEST (final state after 0002)
 *  - max = 1   -> IDENTITY_CONTROL_PLANE_SCHEMA_MANIFEST
 *
 * Tenant stream remains merged/final:
 *  - max >= 13 -> TENANT_FINAL_SCHEMA_MANIFEST
 *  - max < 13  -> merged per-ordinal manifests (later overrides earlier)
 *
 * Fresh apply passes `entry.ordinal` explicitly. Verify and replay pass the
 * recorded max ordinal, which must not be clamped to the requested entry:
 * an old entry can be replayed after later migrations have already landed.
 */
function cumulativeManifestFor(
  entry: MigrationRegistryEntry,
  maxOrdinal: number,
): SchemaManifest<string> | null {
  if (entry.stream === 'tenant') {
    if (maxOrdinal >= 13) return TENANT_FINAL_SCHEMA_MANIFEST as SchemaManifest<string>;
    if (maxOrdinal < 1) return manifestForEntry(entry);
    const ordinals: number[] = [];
    for (let o = 1; o <= maxOrdinal; o += 1) ordinals.push(o);
    // Tenant 0006 is ACL-only (null schema manifest); the merge ignores nulls
    // by design so the cumulative state for cap >= 6 still surfaces every
    // created table.
    return mergeTenantManifests(ordinals);
  }
  // control-plane ladder
  if (maxOrdinal >= 6) {
    return POST_F2_CONTROL_PLANE_SCHEMA_MANIFEST as unknown as SchemaManifest<string>;
  }
  if (maxOrdinal >= 4) {
    return composeControlPlaneRouteFinalPlusAcademic();
  }
  if (maxOrdinal === 3) {
    return TENANT_ROUTE_SCHEMA_MANIFEST as unknown as SchemaManifest<string>;
  }
  if (maxOrdinal === 2) {
    return AUTH_SECURITY_SCHEMA_MANIFEST as unknown as SchemaManifest<string>;
  }
  if (maxOrdinal === 1) {
    return IDENTITY_CONTROL_PLANE_SCHEMA_MANIFEST as unknown as SchemaManifest<string>;
  }
  return manifestForEntry(entry);
}

/**
 * Resolve the schema manifest the apply/verify path must validate against.
 * For the apply-after-body path the DB only knows the freshly-applied state,
 * so the cap is `entry.ordinal`. For the replay path and for the verify path
 * the cap is the recorded history max ordinal.
 */
function manifestFor(
  entry: MigrationRegistryEntry,
  maxOrdinal: number,
): SchemaManifest<string> | null {
  return maxOrdinal > 0 ? cumulativeManifestFor(entry, maxOrdinal) : manifestForEntry(entry);
}

function inspectionTablesFor(
  entry: MigrationRegistryEntry | null,
  context: RunnerContext,
  maxOrdinal: number = 0,
): readonly string[] {
  if (context.stream === 'tenant') {
    // Tenant status (entry=null) and every tenant ordinal inspect the post-
    // 0013 final table list (25 operational + lu_migration_history). A
    // per-ordinal entry on a DB recorded at any max likewise inspects the
    // final list because the runner trusts the cumulative recorded state.
    return [...TENANT_FINAL_TABLES];
  }
  // Control-plane stream: an old entry replaying on a final-state DB must
  // inspect the FINAL current state (POST_F2), not the entry's own touched
  // tables. The final list applies whenever the recorded max is at or
  // beyond 0006; below 0006 the entry's own touched-tables set is what the
  // cumulative manifest is keyed against.
  if (entry !== null && maxOrdinal < 6) {
    return [...entry.touchedTables];
  }
  void TENANT_TABLES;
  return [...CONTROL_PLANE_TABLES_POST_F2];
}

export async function inspectTarget(
  client: Client,
  inspectionTables: readonly string[] = [...CONTROL_PLANE_TABLES_POST_F2],
): Promise<TargetInspection> {
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
    [TARGET_SCHEMA, [...inspectionTables]],
  );
  const columnRows = await queryRows<ColumnNameRow>(
    client,
    'SELECT table_name, column_name FROM information_schema.columns WHERE table_schema = $1 AND table_name = ANY($2::text[]) ORDER BY table_name, ordinal_position',
    [TARGET_SCHEMA, [...inspectionTables]],
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

  const tables: TablePresence[] = inspectionTables.map((table) => ({
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

export async function fetchStatus(
  config: PostgresConnectionConfig,
  context: RunnerContext = {},
): Promise<TargetInspection> {
  const client = newClient(config);
  let connected = false;
  try {
    await client.connect();
    connected = true;
    return await inspectTarget(client, inspectionTablesFor(null, context, 0));
  } finally {
    if (connected) {
      await client.end().catch(() => undefined);
    }
  }
}

export async function verifyMigration(
  config: PostgresConnectionConfig,
  filePath: string,
  content: Buffer,
  context: RunnerContext = {},
): Promise<VerifyOutcome> {
  const { entry } = validatePinnedMigration(filePath, content, { stream: context.stream });
  const client = newClient(config);
  let connected = false;
  try {
    await client.connect();
    connected = true;
    await client.query('BEGIN ISOLATION LEVEL REPEATABLE READ READ ONLY');
    await client.query('SET LOCAL search_path = public, pg_catalog');
    // Verify MUST run the same preflight as up. The executor session is
    // the migrator login (NOINHERIT), so without `SET LOCAL ROLE
    // lu_auth_migrator` (step S5 of the preflight) the current_user
    // cannot see `lu_migration_history` (owned by the migrator role),
    // `historyTableExists` would falsely report false, and `maxOrdinal`
    // would default to 0 — which would make the runner pick the
    // per-entry 0001 manifest even when the recorded state is already
    // at ordinal 6, falsifying the cumulative replay check. The
    // preflight is itself read-only (SELECTs against pg_roles /
    // pg_class / pg_proc + `SET LOCAL ROLE` + `has_schema_privilege`),
    // so it is fully compatible with the `READ ONLY` transaction
    // mode that opens this path.
    await assertMigratorIdentityAndPrivileges(client, entry);
    // The verify path uses the CURRENT cumulative recorded state of the DB.
    // The history probe is metadata-only so a pre-0006 / pre-0013 DB never
    // poisons the live read-only transaction with a 42P01 error; the probe
    // simply returns false and the runner falls back to entry.ordinal.
    const hasHistory = await historyTableExists(client);
    let maxOrdinal = 0;
    if (hasHistory) {
      maxOrdinal = await assertLedgerContiguousAndPinned(client, entry.stream);
      // No-pin semantics: if the recorded max already covers the requested
      // ordinal but the requested pin row is absent, the contiguous read
      // already rejected (a gap cannot exist at a later ordinal without
      // also rejecting the contiguous invariant).
      if (maxOrdinal >= entry.ordinal) {
        await assertPinAtOrdinal(client, entry);
      }
    }
    const manifest = manifestFor(entry, maxOrdinal);
    const supplemental = await verifySupplementalCatalog(client, entry, maxOrdinal);
    let combined: SchemaVerification;
    if (manifest === null) {
      // ACL-only contract: schemaManifest is null but supplemental is not.
      // supplemental.passed is the only gate here.
      combined = supplemental.passed
        ? { passed: true, diffs: [], actualTableNames: [] }
        : { passed: false, diffs: [...supplemental.diffs], actualTableNames: [] };
    } else {
      const verification = await verifySchema(client, manifest as SchemaManifest<string>);
      combined =
        verification.passed && supplemental.passed
          ? verification
          : {
              passed: false,
              diffs: [...verification.diffs, ...supplemental.diffs],
              actualTableNames: verification.actualTableNames,
            };
    }
    // Inspection happens BEFORE COMMIT (no catalog query after COMMIT). The
    // inspection always covers the FINAL current state once the ledger is
    // present so a replay sees the same surface the cumulative manifest
    // validated.
    const inspection = await inspectTarget(client, inspectionTablesFor(entry, context, maxOrdinal));
    await client.query('COMMIT');
    return { verification: combined, inspection };
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
  // The migrator role is in effect (SET LOCAL ROLE lu_auth_migrator was
  // executed by the preflight). We verify CREATE on the target schema for
  // the migrator role explicitly so the contract is auditable from the
  // query log without trusting SET ROLE side-effects.
  const rows = await queryRows<PrivilegeRow>(
    client,
    'SELECT has_schema_privilege($1, $2, $3) AS can_create',
    [MIGRATOR_ROLE, entry.targetSchema, 'CREATE'],
  );
  const canCreate = rows[0]?.can_create === true;
  if (!canCreate) {
    throw new Error(`Current user lacks CREATE privilege on schema "${entry.targetSchema}".`);
  }
}

/**
 * Acquire the STREAM-COMMON advisory lock for an apply. The runner takes
 * this lock FIRST, then the per-entry advisory lock. The key composition
 * is:
 *
 *  - control-plane: a constant string registered on every CP entry
 *    (`entry.streamCommonAdvisoryLockKey`).
 *  - tenant:       `<prefix>:<site-uuid>` where the prefix is the
 *    `tenantStreamCommonAdvisoryLockPrefix` and the site UUID is taken
 *    from the runner site context. A tenant apply that arrives without a
 *    site context fails closed BEFORE this helper is called (the runner
 *    throws immediately after BEGIN).
 *
 * Only the site UUID appears in the lock key for the tenant stream. Env var
 * names / DSN / passwords / references are NEVER part of any lock key.
 */
async function acquireStreamCommonLock(
  client: Client,
  entry: MigrationRegistryEntry,
  siteId: string | null,
): Promise<void> {
  if (entry.stream === 'control-plane') {
    const commonKey = entry.streamCommonAdvisoryLockKey;
    if (commonKey === undefined) {
      throw new Error(
        `control-plane entry ${entry.relativePath} is missing the stream-common advisory lock key`,
      );
    }
    await client.query('SELECT pg_advisory_xact_lock(hashtext($1))', [commonKey]);
    return;
  }
  // tenant
  const prefix = entry.tenantStreamCommonAdvisoryLockPrefix;
  if (prefix === undefined) {
    throw new Error(
      `tenant entry ${entry.relativePath} is missing the stream-common advisory lock prefix`,
    );
  }
  if (siteId === null) {
    throw new Error(
      `tenant apply for ${entry.relativePath} requires an explicit --site UUID; ` +
        'the runner never invents a site and refuses to take the stream-common lock without one.',
    );
  }
  const key = `${prefix}:${siteId.toLowerCase()}`;
  await client.query('SELECT pg_advisory_xact_lock(hashtext($1))', [key]);
}

/**
 * True when `public.lu_migration_history` exists in the target schema. The
 * probe is metadata-only (information_schema.tables) so it never poisons
 * the live transaction with a 42P01 error: a missing relation returns zero
 * rows from the view, NOT a failure. This is the sole presence probe the
 * runner uses for the history ledger; downstream SELECTs / INSERTs assume
 * the table exists once this helper returns true.
 */
async function historyTableExists(client: Client): Promise<boolean> {
  const rows = await queryRows<BooleanRow>(
    client,
    `SELECT EXISTS (
       SELECT 1 FROM information_schema.tables
        WHERE table_schema = $1 AND table_name = 'lu_migration_history'
     ) AS exists`,
    [TARGET_SCHEMA],
  );
  return rows[0]?.exists === true;
}

/**
 * Read the max ordinal recorded for the stream in `public.lu_migration_history`.
 * Returns 0 when the history table is absent (pre-0006 / pre-0013 DBs) so
 * callers fall back to the per-entry manifest without an extra branch.
 *
 * PROBE-FIRST: callers MUST have already established `historyTableExists`
 * is true; this helper runs the aggregation directly without a try/catch
 * wrapper, so the live transaction is never poisoned by a swallowed SQL
 * failure.
 */
async function fetchMaxOrdinalForStream(client: Client, stream: MigrationStream): Promise<number> {
  const rows = await queryRows<MaxOrdinalRow>(
    client,
    'SELECT COALESCE(MAX(ordinal), 0) AS max_ordinal FROM public.lu_migration_history WHERE stream = $1',
    [stream],
  );
  return Number(rows[0]?.max_ordinal ?? 0);
}

/**
 * Verify that a recorded pin row matches the registered pin byte-for-byte on
 * every documented field (stream, ordinal, relative_path, sha256, bytes,
 * work_id). A null work_id on either side is treated as equal. Throws when
 * the row is absent, when any field disagrees, or when the row lookup fails
 * for any reason (caller is responsible for confirming the history table
 * exists first).
 */
function comparePinRow(row: MigrationHistoryRow, entry: MigrationRegistryEntry): void {
  const failures: string[] = [];
  if ((row.stream ?? '') !== entry.stream) {
    failures.push(`stream "${row.stream ?? ''}" ≠ "${entry.stream}"`);
  }
  if (Number(row.ordinal ?? -1) !== entry.ordinal) {
    failures.push(`ordinal "${row.ordinal ?? ''}" ≠ "${entry.ordinal}"`);
  }
  if ((row.relative_path ?? '') !== entry.relativePath) {
    failures.push(`relative_path "${row.relative_path ?? ''}" ≠ "${entry.relativePath}"`);
  }
  if ((row.sha256 ?? '') !== entry.sha256) {
    failures.push(`sha256 "${row.sha256 ?? ''}" ≠ "${entry.sha256}"`);
  }
  if (Number(row.bytes ?? -1) !== entry.byteLength) {
    failures.push(`bytes "${row.bytes ?? ''}" ≠ "${entry.byteLength}"`);
  }
  // work_id is nullable on the table; treat null on either side as equal.
  if ((row.work_id ?? null) !== (entry.workId ?? null)) {
    failures.push(`work_id "${row.work_id ?? '(null)'}" ≠ "${entry.workId ?? '(null)'}"`);
  }
  if (failures.length > 0) {
    throw new Error(
      `lu_migration_history row for ${entry.relativePath} is immutable and conflicts with the registered pin (${failures.join('; ')}).`,
    );
  }
}

/**
 * Read the full ledger for the entry's stream and assert the contiguous
 * 1..max invariant with every row pinned to its registered entry. Returns
 * the recorded max ordinal (>= 1) on success; throws on any violation. The
 * caller MUST have established `historyTableExists` is true before calling.
 *
 * Invariants enforced (metadata only):
 *  - row count is at least 1 and every ordinal is a positive integer;
 *  - ordinals are unique per stream (the PRIMARY KEY on lu_migration_history
 *    already enforces this; the assertion makes the contract explicit);
 *  - ordinals are exactly 1..max (no gaps; a missing old row with a later
 *    max rejects before any body execution);
 *  - max <= current registry length for the stream (a recorded ordinal
 *    beyond the registered pin set rejects — the registry is the source
 *    of truth);
 *  - every row's stream/ordinal/relative_path/sha256/bytes/work_id matches
 *    the corresponding registry entry.
 */
async function assertLedgerContiguousAndPinned(
  client: Client,
  stream: MigrationStream,
): Promise<number> {
  const allEntries = stream === 'control-plane' ? CONTROL_PLANE_REGISTRY : TENANT_REGISTRY;
  const rows = await queryRows<MigrationHistoryRow>(
    client,
    'SELECT stream, ordinal, relative_path, sha256, bytes, work_id FROM public.lu_migration_history WHERE stream = $1 ORDER BY ordinal ASC',
    [stream],
  );
  if (rows.length === 0) return 0;
  const byOrdinal = new Map<number, MigrationHistoryRow>();
  let maxOrdinal = 0;
  for (const row of rows) {
    const ord = Number(row.ordinal);
    if (!Number.isInteger(ord) || ord < 1) {
      throw new Error(
        `lu_migration_history row has invalid ordinal "${row.ordinal}" for stream "${stream}"`,
      );
    }
    if (byOrdinal.has(ord)) {
      throw new Error(`lu_migration_history has duplicate ordinal ${ord} for stream "${stream}"`);
    }
    if (ord > maxOrdinal) maxOrdinal = ord;
    byOrdinal.set(ord, row);
  }
  // Contiguous 1..max.
  for (let o = 1; o <= maxOrdinal; o += 1) {
    const row = byOrdinal.get(o);
    if (row === undefined) {
      throw new Error(
        `lu_migration_history stream "${stream}" has a gap at ordinal ${o} (recorded max=${maxOrdinal})`,
      );
    }
  }
  // max <= current registry length.
  if (maxOrdinal > allEntries.length) {
    throw new Error(
      `lu_migration_history stream "${stream}" records ordinal ${maxOrdinal} but the registry only knows up to ${allEntries.length}`,
    );
  }
  // Pin match for every contiguous ordinal.
  for (let o = 1; o <= maxOrdinal; o += 1) {
    const row = byOrdinal.get(o);
    if (row === undefined) continue;
    const entry = allEntries[o - 1];
    if (entry === undefined) continue;
    comparePinRow(row, entry);
  }
  return maxOrdinal;
}

/**
 * Read the history pin row at `(stream, ordinal)` for the entry and verify
 * every documented field matches the registered pin. Throws when the row
 * is absent or any field disagrees. Caller MUST have established the
 * history table exists AND that the recorded max covers the entry's
 * ordinal.
 */
async function assertPinAtOrdinal(client: Client, entry: MigrationRegistryEntry): Promise<void> {
  const rows = await queryRows<MigrationHistoryRow>(
    client,
    'SELECT stream, ordinal, relative_path, sha256, bytes, work_id FROM public.lu_migration_history WHERE stream = $1 AND ordinal = $2 LIMIT 1',
    [entry.stream, entry.ordinal],
  );
  if (rows.length === 0) {
    throw new Error(
      `lu_migration_history row for ${entry.relativePath} is missing despite the recorded max covering ordinal ${entry.ordinal}; refusing to re-execute an old body.`,
    );
  }
  comparePinRow(rows[0]!, entry);
}

/**
 * Backfill the history ledger for every registered entry up to and including
 * `entry.ordinal` for the entry's stream. Every pin is inserted with
 * `INSERT ... ON CONFLICT (stream, ordinal) DO NOTHING` so an existing pin
 * row is NEVER overwritten; the conflict-safe INSERT is followed by a
 * SELECT to re-read the row and re-compare every field. The same SELECT
 * path also detects a pin row that already exists with a DIFFERENT
 * (sha256, bytes, relative_path, work_id) and rejects it as an immutable
 * pin violation.
 *
 * MUST be called INSIDE the SERIALIZABLE transaction the runner uses for
 * `up`: there is intentionally no post-commit stamping (a separate client
 * could race against further up-applies and miss the ledger write). The
 * 0006 / 0013 entries create the history table themselves; ordinals 0001
 * through 0005 / 0012 therefore backfill through 0006 / 0013 in the same
 * transaction.
 */
async function backfillHistoryThroughOrdinal(
  client: Client,
  entry: MigrationRegistryEntry,
): Promise<void> {
  const allEntries = entry.stream === 'control-plane' ? CONTROL_PLANE_REGISTRY : TENANT_REGISTRY;
  for (const prior of allEntries) {
    if (prior.stream !== entry.stream) continue;
    if (prior.ordinal > entry.ordinal) continue;
    // Conflict-safe INSERT first; the WHERE clause ON CONFLICT DO NOTHING
    // intentionally lets an existing pin row keep its bytes. The
    // re-SELECT below catches silent divergence (the existing row must
    // match every field of the registered pin).
    try {
      await client.query(
        'INSERT INTO public.lu_migration_history (stream, ordinal, relative_path, sha256, bytes, work_id) VALUES ($1, $2, $3, $4, $5, $6) ON CONFLICT (stream, ordinal) DO NOTHING',
        [
          prior.stream,
          prior.ordinal,
          prior.relativePath,
          prior.sha256,
          prior.byteLength,
          prior.workId,
        ],
      );
    } catch (error) {
      throw new Error(
        `lu_migration_history backfill failed for ${prior.relativePath}: ${(error as Error).message}`,
        { cause: error },
      );
    }
    // Re-read and re-compare every field, regardless of who wrote the row.
    const reread = await queryRows<MigrationHistoryRow>(
      client,
      'SELECT stream, ordinal, relative_path, sha256, bytes, work_id FROM public.lu_migration_history WHERE stream = $1 AND ordinal = $2 LIMIT 1',
      [prior.stream, prior.ordinal],
    );
    if (reread.length === 0) {
      throw new Error(
        `lu_migration_history backfill INSERT for ${prior.relativePath} was a no-op (the row is absent after the INSERT).`,
      );
    }
    comparePinRow(reread[0]!, prior);
  }
}

export async function applyMigration(
  config: PostgresConnectionConfig,
  filePath: string,
  content: Buffer,
  context: RunnerContext = {},
): Promise<UpOutcome> {
  const { entry } = validatePinnedMigration(filePath, content, { stream: context.stream });
  const canonical = deriveCanonicalBootstrapSql(entry, content.toString('utf8'));

  // Tenant stream fail-closed gate: a tenant apply that arrives without a
  // site UUID never opens a connection and never takes any lock. The runner
  // surfaces the error through the same fail-closed path as every other
  // precondition.
  if (entry.stream === 'tenant' && (context.site === undefined || context.site.siteId === '')) {
    throw new Error(
      `tenant apply for ${entry.relativePath} requires an explicit --site UUID; the runner refuses to invent a site and refuses to take the stream-common lock without one.`,
    );
  }

  const client = newClient(config);
  let connected = false;
  try {
    await client.connect();
    connected = true;

    await client.query('BEGIN ISOLATION LEVEL SERIALIZABLE');
    try {
      await client.query('SET LOCAL search_path = public, pg_catalog');
      // 0) Migrator preflight — applies ONLY to the up path. Verifies the
      //    session role identity and privileges, rejects when the
      //    migrator/runtime contract is violated, then executes SET LOCAL
      //    ROLE lu_auth_migrator so the rest of the transaction runs as
      //    the no-login migrator role. NO locks / NO body when this rejects.
      await assertMigratorIdentityAndPrivileges(client, entry);
      // 1) Stream-common lock FIRST (CP common key OR tenant common
      //    prefix:<site-uuid>). The helper throws and rolls back the
      //    transaction when the tenant stream has no site context; the
      //    runner never reaches the per-entry lock in that case.
      await acquireStreamCommonLock(client, entry, context.site?.siteId ?? null);
      // 2) Per-entry advisory lock ("file lock"). Serializes concurrent
      //    applies of the SAME entry on the SAME database; the
      //    stream-common lock above serializes concurrent applies of
      //    DIFFERENT entries / different sites on the SAME database.
      await client.query('SELECT pg_advisory_xact_lock(hashtext($1))', [entry.advisoryLockKey]);

      // 3) CREATE-privilege check as the migrator role (the preflight has
      //    already executed SET LOCAL ROLE; this query verifies the
      //    privilege the migration actually needs).
      await assertCanCreateSchema(client, entry);

      // 4) History probe — metadata-only (information_schema.tables). When
      //    the table is absent the runner never issues a relation query
      //    against it later in the transaction, so a swallowed 42P01 error
      //    never poisons the live SERIALIZABLE transaction.
      let hasHistoryTable = await historyTableExists(client);

      // 5) Ledger contiguous-pin check. When history exists every recorded
      //    row for the entry's stream must be present, contiguous, and
      //    pinned to its registered bytes; a missing old row with a later
      //    max rejects before any body execution. When history is absent
      //    maxOrdinal is 0 and the runner enters the fresh-apply path.
      let maxOrdinal = 0;
      if (hasHistoryTable) {
        maxOrdinal = await assertLedgerContiguousAndPinned(client, entry.stream);
      }

      // 6) No-pin semantics:
      //    - maxOrdinal >= entry.ordinal -> the requested pin row MUST be
      //      present and MUST match the registered pin (the contiguous
      //      invariant guarantees 1..maxOrdinal; this assertion covers the
      //      specific ordinal the caller requested).
      //    - maxOrdinal < entry.ordinal (incl. 0 = no history) -> fresh
      //      apply; the manifest cap is entry.ordinal.
      if (maxOrdinal >= entry.ordinal) {
        await assertPinAtOrdinal(client, entry);
        // Cumulative manifest driven by the recorded max ordinal; an old
        // entry replaying on a final-state DB must validate the FINAL
        // schema (control-plane POST_F2 / CP intermediate / tenant
        // TENANT_FINAL_SCHEMA), never its own ordinal-only manifest.
        const manifest = manifestFor(entry, maxOrdinal);
        const supplemental = await verifySupplementalCatalog(client, entry, maxOrdinal);
        const manifestVerification =
          manifest !== null
            ? await verifySchema(client, manifest as SchemaManifest<string>)
            : { passed: true, diffs: [], actualTableNames: [] };
        if (!manifestVerification.passed) {
          await client.query('ROLLBACK');
          throw new Error(
            `Replay manifest verification failed after a matching history pin was found for ${entry.relativePath}: ${manifestVerification.diffs.join('; ')}`,
          );
        }
        if (!supplemental.passed) {
          await client.query('ROLLBACK');
          throw new Error(
            `Replay supplemental verification failed for ${entry.relativePath}: ${supplemental.diffs.join('; ')}`,
          );
        }
        // Capture inspection BEFORE COMMIT (no catalog query after COMMIT).
        const inspection = await inspectTarget(
          client,
          inspectionTablesFor(entry, context, maxOrdinal),
        );
        await client.query('COMMIT');
        return {
          preexisting: [],
          executed: false,
          committed: true,
          verification: manifestVerification,
          inspection,
          skipped: { reason: 'pin-matches-history' },
        };
      }

      // 7) Fresh-apply path. maxOrdinal < entry.ordinal (or no history).
      //    The partial-state preexisting gate is split into two distinct
      //    policies keyed off the entry-level booleans (W16A / proven
      //    B1 fix):
      //
      //    - `requiresCleanState` (bootstrap 0001, CP 0006, tenant
      //      0013): this entry demands either no preexisting tables
      //      OR an exact contiguous pin row at `entry.ordinal`.
      //    - `createsHistory` (CP 0006, tenant 0013 ONLY): this
      //      entry is what creates `lu_migration_history`.
      //
      //    Combined gates:
      //    (a) `requiresCleanState && preexisting > 0 && hasHistory`:
      //        the DB is in a post-history state with the target
      //        tables already present. The runner checks the exact
      //        pin row for entry.ordinal; a missing pin is a
      //        fail-closed mismatch (half-apply), a matching pin
      //        lets the verify path run.
      //    (b) `requiresCleanState && !createsHistory &&
      //        preexisting > 0 && !hasHistory` — bootstrap 0001
      //        only. The B1 fix: no matching history CAN exist
      //        (the ledger is absent because 0001 is the entry that
      //        introduces it on a clean DB); running the idempotent
      //        body would mask a corruption / out-of-band schema
      //        and skip the ledger backfill that MUST stamp
      //        ordinal 1. Fail closed: no body, no relation query,
      //        no COMMIT.
      //    (c) `requiresCleanState && createsHistory &&
      //        preexisting > 0 && !hasHistory` — CP 0006 / tenant
      //        0013 fresh apply with preexisting target tables.
      //        Falls through to the body; the post-body metadata
      //        re-probe below re-checks the contract (a body that
      //        silently fails to create the table forces a
      //        ROLLBACK before any INSERT).
      //
      //    The matching contiguous-history replay path (maxOrdinal >=
      //    entry.ordinal) is taken BEFORE this fresh-apply branch,
      //    so a clean pin-matches-history apply never reaches these
      //    gates.
      const preexisting = await findPreexistingTables(client, entry);
      const requiresCleanState =
        entry.relativePath === '0001_create_identity_control_plane.sql' ||
        entry.relativePath === 'control-plane/0006_mig001_users_identity.sql' ||
        entry.relativePath === 'tenant/0013_migration_history.sql';
      const createsHistory =
        entry.relativePath === 'control-plane/0006_mig001_users_identity.sql' ||
        entry.relativePath === 'tenant/0013_migration_history.sql';
      if (requiresCleanState && preexisting.length > 0 && hasHistoryTable) {
        // Shared partial-state gate: the ledger exists, so the DB
        // is post-history; if the registered pin row for this
        // entry is missing the apply must be rejected (a partial
        // prior apply with no pin would silently re-run the body
        // and re-stamp the ledger, masking the mismatch).
        const existingPin = await queryRows<MigrationHistoryRow>(
          client,
          'SELECT stream, ordinal, relative_path, sha256, bytes, work_id FROM public.lu_migration_history WHERE stream = $1 AND ordinal = $2 LIMIT 1',
          [entry.stream, entry.ordinal],
        );
        if (existingPin.length === 0) {
          await client.query('ROLLBACK');
          const inspection = await inspectTarget(
            client,
            inspectionTablesFor(entry, context, maxOrdinal),
          );
          return {
            preexisting,
            executed: false,
            committed: false,
            verification: null,
            inspection,
          };
        }
        // Has a pin but tables also pre-exist; allow the verify path.
      }
      if (requiresCleanState && !createsHistory && preexisting.length > 0 && !hasHistoryTable) {
        // Bootstrap 0001 B1 fix (clean-state-gated, no
        // createsHistory). Baseline tables preexist in a
        // pre-history DB. There is no possible matching pin (the
        // ledger is absent because 0001 is the entry that
        // introduces it). The bootstrap contract is "clean state
        // only"; running the idempotent body here would silently
        // treat a corrupted DB as a successful apply and skip the
        // ledger backfill that MUST stamp ordinal 1. Fail closed,
        // do NOT query the missing ledger, do NOT run the body.
        await client.query('ROLLBACK');
        const inspection = await inspectTarget(
          client,
          inspectionTablesFor(entry, context, maxOrdinal),
        );
        return {
          preexisting,
          executed: false,
          committed: false,
          verification: null,
          inspection,
        };
      }

      await client.query(canonical.body);

      // Per-entry (fresh-state) verification: the DB only knows the entry
      // we just applied, so the cumulative cap is `entry.ordinal` itself.
      const manifest = manifestFor(entry, entry.ordinal);
      const supplemental = await verifySupplementalCatalog(client, entry, entry.ordinal);
      if (manifest === null) {
        // ACL-only contract: manifest is null, supplemental is not. The
        // supplemental.passed gate is the only verification we can run;
        // both-null would have been rejected by the registry pin wiring
        // (no entry ships with a (null, null) expectation pair).
        if (!supplemental.passed) {
          await client.query('ROLLBACK');
          throw new Error(
            `Supplemental verification failed for ACL-only migration ${entry.relativePath}: ${supplemental.diffs.join('; ')}`,
          );
        }
        const inspection = await inspectTarget(
          client,
          inspectionTablesFor(entry, context, entry.ordinal),
        );
        await client.query('COMMIT');
        return {
          preexisting: [],
          executed: true,
          committed: true,
          verification: {
            passed: true,
            diffs: [],
            actualTableNames: [],
          },
          inspection,
          historyKey:
            hasHistoryTable && supplemental.expectedHistoryWrite
              ? { stream: entry.stream, relativePath: entry.relativePath }
              : undefined,
        };
      }
      const verification = await verifySchema(client, manifest as SchemaManifest<string>);
      if (!verification.passed) {
        await client.query('ROLLBACK');
        throw new Error(
          `Schema verification failed after executing migration: ${verification.diffs.join('; ')}`,
        );
      }
      if (!supplemental.passed) {
        await client.query('ROLLBACK');
        throw new Error(
          `Supplemental verification failed after executing migration ${entry.relativePath}: ${supplemental.diffs.join('; ')}`,
        );
      }

      //      Backfill the history ledger INSIDE the same transaction. Three
      //      distinct cases (W16A / proven B1 fix):
      //
      //      - `createsHistory` entry (CP 0006, tenant 0013) AND the pre-body
      //        probe was FALSE: the entry's body is what creates the
      //        `lu_migration_history` relation. The runner re-probes metadata
      //        INSIDE the same transaction after verification; if the body
      //        silently failed to create the table the runner ROLLBACKs
      //        BEFORE any INSERT (fail-closed: no ledger row written for a
      //        missing table). Only when the re-probe confirms the table
      //        exists does the runner backfill `1..entry.ordinal` exact pins
      //        and re-read / re-compare every row.
      //      - hasHistoryTable was true BEFORE the body (replay or fresh
      //        apply on a post-history DB): backfill normally. 0006 / 0013
      //        pins that already exist stay byte-identical (ON CONFLICT
      //        DO NOTHING + post-INSERT SELECT+compare).
      //      - Early ordinals before history commit (CP 0001..0005,
      //        tenant 0001..0012): no backfill — the history table does not
      //        exist on a pre-history DB and the runner never INSERTs into
      //        a missing relation.
      //
      //      ON CONFLICT (stream, ordinal) DO NOTHING plus the post-INSERT
      //      SELECT+compare guarantee the existing pin rows are NEVER
      //      overwritten and any silent divergence is rejected as an
      //      immutable pin violation.
      if (createsHistory && !hasHistoryTable) {
        // Pre-body probe was false; body is the entry that creates
        // the relation. Re-probe metadata and require the
        // newly-created table to exist before any INSERT. A missing
        // table at this stage means the body silently failed to
        // deliver the contract — ROLLBACK and surface the failure
        // closed. The post-body probe is captured in a FRESH local
        // (NOT the stale pre-body `hasHistoryTable` variable) so the
        // decision is made on the visible truth of the same
        // transaction.
        const hasHistoryTableAfterBody = await historyTableExists(client);
        if (!hasHistoryTableAfterBody) {
          await client.query('ROLLBACK');
          throw new Error(
            `Migration ${entry.relativePath} did not create lu_migration_history in its body; ` +
              'the runner refuses to backfill pins into a missing ledger and is rolling the transaction back. ' +
              'Inspect the canonical body, re-run, and re-apply only after the relation is guaranteed to exist.',
          );
        }
        await backfillHistoryThroughOrdinal(client, entry);
        hasHistoryTable = true;
      } else if (hasHistoryTable) {
        await backfillHistoryThroughOrdinal(client, entry);
      }

      // Capture inspection BEFORE COMMIT (no catalog query after COMMIT).
      const inspection = await inspectTarget(
        client,
        inspectionTablesFor(entry, context, entry.ordinal),
      );
      await client.query('COMMIT');

      // historyKey is written only when the history ledger actually records
      // the apply. The presence of the history table was probed BEFORE
      // COMMIT, so the same value is reused here; no second catalog query
      // runs after COMMIT.
      return {
        preexisting: [],
        executed: true,
        committed: true,
        verification,
        inspection,
        historyKey: hasHistoryTable
          ? { stream: entry.stream, relativePath: entry.relativePath }
          : undefined,
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

// ---------------------------------------------------------------------------
// Migrator preflight (apply path only).
//
// Steps (in order — see file-level contract):
//   Q1  session role properties  pg_roles WHERE rolname = current_user
//                                  (rolcanlogin MUST be true; managed identities,
//                                   SUPERUSER, CREATEROLE, CREATEDB, REPLICATION,
//                                   BYPASSRLS are all rejected)
//   Q2  migrator role presence   pg_roles WHERE rolname = 'lu_auth_migrator'
//                                  (must exist, exact name match, NOLOGIN +
//                                   NOSUPERUSER + NOCREATEROLE + NOCREATEDB +
//                                   NOREPLICATION + NOBYPASSRLS)
//   Q3  session membership       pg_has_role(current_user, 'lu_auth_migrator', 'MEMBER')
//   Q4  runtime role properties  pg_roles WHERE rolname = 'lu_auth_runtime'
//                                  (must exist, exact name match, NOLOGIN +
//                                   NOSUPERUSER + NOCREATEROLE + NOCREATEDB +
//                                   NOREPLICATION + NOBYPASSRLS — same contract
//                                   as the migrator role; runs BEFORE
//                                   SET LOCAL ROLE so the runtime role is
//                                   still queryable from the session login).
//   S5  SET LOCAL ROLE lu_auth_migrator
//   Q6  current_user             SELECT current_user AS current_actor
//   Q7  migrator CREATE          has_schema_privilege('lu_auth_migrator', targetSchema, 'CREATE')
//   Q8  runtime CREATE on public has_schema_privilege('lu_auth_runtime', 'public', 'CREATE')
//                                  (MUST be false — runtime must NOT install
//                                   backdoors the migrator would then trust).
//   Q9  runtime ownership        EXISTS(SELECT 1 FROM pg_class / pg_proc ...)
//                                  (runtime must NOT own migrable public
//                                   objects).
// ---------------------------------------------------------------------------

interface SessionRoleRow extends Record<string, unknown> {
  rolname: string | null;
  rolcanlogin: boolean | null;
  rolsuper: boolean | null;
  rolcreaterole: boolean | null;
  rolcreatedb: boolean | null;
  rolreplication: boolean | null;
  rolbypassrls: boolean | null;
}

interface MigratorRoleRow extends Record<string, unknown> {
  rolname: string | null;
  rolcanlogin: boolean | null;
  rolsuper: boolean | null;
  rolcreaterole: boolean | null;
  rolcreatedb: boolean | null;
  rolreplication: boolean | null;
  rolbypassrls: boolean | null;
}

interface MembershipRow extends Record<string, unknown> {
  is_member: boolean | null;
}

interface CurrentActorRow extends Record<string, unknown> {
  current_actor: string | null;
}

interface RuntimeCreateRow extends Record<string, unknown> {
  runtime_can_create: boolean | null;
}

interface RuntimeOwnsRow extends Record<string, unknown> {
  runtime_owns_tables: boolean | null;
  runtime_owns_sequences: boolean | null;
  runtime_owns_functions: boolean | null;
}

async function assertMigratorIdentityAndPrivileges(
  client: Client,
  entry: MigrationRegistryEntry,
): Promise<void> {
  // Q1: session role properties.
  const sessionRows = await queryRows<SessionRoleRow>(
    client,
    'SELECT rolname, rolcanlogin, rolsuper, rolcreaterole, rolcreatedb, rolreplication, rolbypassrls FROM pg_roles WHERE rolname = current_user',
  );
  const session = sessionRows[0];
  if (session === undefined) {
    throw new Error(
      'Migrator preflight: pg_roles returned no row for current_user; refusing to proceed.',
    );
  }
  const sessionName = session.rolname ?? '';
  // Reject if the session role is one of the managed identities (they are
  // not login users and must never run a migration).
  if (sessionName === RUNTIME_ROLE || sessionName === MIGRATOR_ROLE) {
    throw new Error(
      `Migrator preflight: session role "${sessionName}" is a managed identity and cannot execute migrations.`,
    );
  }
  // The session role MUST be a login-capable principal — managed identities
  // (NOLOGIN) can never be the outer session and a misconfigured login role
  // with rolcanlogin=false would silently bypass the membership check.
  if (session.rolcanlogin !== true) {
    throw new Error(
      `Migrator preflight: session role "${sessionName}" is NOLOGIN; the migrator role contract requires an unprivileged login user.`,
    );
  }
  // Reject elevated privileges on the session role.
  if (session.rolsuper === true) {
    throw new Error(
      'Migrator preflight: session role is a PostgreSQL superuser; the migrator role contract requires an unprivileged login user.',
    );
  }
  if (session.rolcreaterole === true) {
    throw new Error(
      'Migrator preflight: session role has CREATEROLE; the migrator role contract forbids it.',
    );
  }
  if (session.rolcreatedb === true) {
    throw new Error(
      'Migrator preflight: session role has CREATEDB; the migrator role contract forbids it.',
    );
  }
  if (session.rolreplication === true) {
    throw new Error(
      'Migrator preflight: session role has REPLICATION; the migrator role contract forbids it.',
    );
  }
  if (session.rolbypassrls === true) {
    throw new Error(
      'Migrator preflight: session role has BYPASSRLS; the migrator role contract forbids it.',
    );
  }

  // Q2: migrator role presence + safe attrs (exists, NOLOGIN,
  //    NOSUPERUSER, NOCREATEROLE, NOCREATEDB, NOREPLICATION, NOBYPASSRLS).
  //    The query is widened to carry every elevated attr so the contract
  //    is enforced as a single round-trip; the migration role is a
  //    managed identity and never carries any of these flags.
  const migratorRows = await queryRows<MigratorRoleRow>(
    client,
    `SELECT rolname, rolcanlogin, rolsuper, rolcreaterole, rolcreatedb,
            rolreplication, rolbypassrls
       FROM pg_roles WHERE rolname = $1`,
    [MIGRATOR_ROLE],
  );
  if (migratorRows.length === 0) {
    throw new Error(`Migrator preflight: role "${MIGRATOR_ROLE}" is absent; refusing to proceed.`);
  }
  const migrator = migratorRows[0]!;
  if (migrator.rolname !== MIGRATOR_ROLE) {
    throw new Error(
      `Migrator preflight: role "${MIGRATOR_ROLE}" returned an unexpected identifier.`,
    );
  }
  if (migrator.rolcanlogin === true) {
    throw new Error(
      `Migrator preflight: role "${MIGRATOR_ROLE}" must be NOLOGIN; the contract requires the migrator role to be a managed identity.`,
    );
  }
  if (migrator.rolsuper === true) {
    throw new Error(
      `Migrator preflight: role "${MIGRATOR_ROLE}" must be NOSUPERUSER; the contract requires the migrator role to be a managed identity.`,
    );
  }
  if (migrator.rolcreaterole === true) {
    throw new Error(
      `Migrator preflight: role "${MIGRATOR_ROLE}" must be NOCREATEROLE; the contract requires the migrator role to be a managed identity.`,
    );
  }
  if (migrator.rolcreatedb === true) {
    throw new Error(
      `Migrator preflight: role "${MIGRATOR_ROLE}" must be NOCREATEDB; the contract requires the migrator role to be a managed identity.`,
    );
  }
  if (migrator.rolreplication === true) {
    throw new Error(
      `Migrator preflight: role "${MIGRATOR_ROLE}" must be NOREPLICATION; the contract requires the migrator role to be a managed identity.`,
    );
  }
  if (migrator.rolbypassrls === true) {
    throw new Error(
      `Migrator preflight: role "${MIGRATOR_ROLE}" must be NOBYPASSRLS; the contract requires the migrator role to be a managed identity.`,
    );
  }

  // Q3: session role must be a member of the migrator role.
  const membershipRows = await queryRows<MembershipRow>(
    client,
    `SELECT pg_has_role(current_user, $1, 'MEMBER') AS is_member`,
    [MIGRATOR_ROLE],
  );
  const isMember = membershipRows[0]?.is_member === true;
  if (!isMember) {
    throw new Error(
      `Migrator preflight: session role "${sessionName}" is not a member of role "${MIGRATOR_ROLE}"; the migrator role contract requires explicit membership.`,
    );
  }

  // Q4: runtime role presence + safe attrs (exists, NOLOGIN, NOSUPERUSER,
  //    NOCREATEROLE, NOCREATEDB, NOREPLICATION, NOBYPASSRLS). The runtime
  //    role is supposed to be a managed identity — same contract as the
  //    migrator role. This runs BEFORE SET LOCAL ROLE so the runtime role
  //    is still queryable from the session login (SET LOCAL ROLE only
  //    affects current_user, not the visibility of pg_roles rows). After
  //    the check, Q8 (runtime CREATE on public) and Q9 (runtime ownership)
  //    continue to enforce the runtime principal's safe-by-default contract.
  const runtimeRows = await queryRows<MigratorRoleRow>(
    client,
    `SELECT rolname, rolcanlogin, rolsuper, rolcreaterole, rolcreatedb,
            rolreplication, rolbypassrls
       FROM pg_roles WHERE rolname = $1`,
    [RUNTIME_ROLE],
  );
  if (runtimeRows.length === 0) {
    throw new Error(`Migrator preflight: role "${RUNTIME_ROLE}" is absent; refusing to proceed.`);
  }
  const runtime = runtimeRows[0]!;
  if (runtime.rolname !== RUNTIME_ROLE) {
    throw new Error(
      `Migrator preflight: role "${RUNTIME_ROLE}" returned an unexpected identifier.`,
    );
  }
  if (runtime.rolcanlogin === true) {
    throw new Error(
      `Migrator preflight: role "${RUNTIME_ROLE}" must be NOLOGIN; the contract requires the runtime role to be a managed identity.`,
    );
  }
  if (runtime.rolsuper === true) {
    throw new Error(
      `Migrator preflight: role "${RUNTIME_ROLE}" must be NOSUPERUSER; the contract requires the runtime role to be a managed identity.`,
    );
  }
  if (runtime.rolcreaterole === true) {
    throw new Error(
      `Migrator preflight: role "${RUNTIME_ROLE}" must be NOCREATEROLE; the contract requires the runtime role to be a managed identity.`,
    );
  }
  if (runtime.rolcreatedb === true) {
    throw new Error(
      `Migrator preflight: role "${RUNTIME_ROLE}" must be NOCREATEDB; the contract requires the runtime role to be a managed identity.`,
    );
  }
  if (runtime.rolreplication === true) {
    throw new Error(
      `Migrator preflight: role "${RUNTIME_ROLE}" must be NOREPLICATION; the contract requires the runtime role to be a managed identity.`,
    );
  }
  if (runtime.rolbypassrls === true) {
    throw new Error(
      `Migrator preflight: role "${RUNTIME_ROLE}" must be NOBYPASSRLS; the contract requires the runtime role to be a managed identity.`,
    );
  }

  // S5: switch into the migrator role for the remainder of the transaction.
  await client.query(`SET LOCAL ROLE ${MIGRATOR_ROLE}`);

  // Q6: verify current_user is the migrator role (defensive — catches a
  // misconfigured superuser that ignored SET LOCAL ROLE).
  const actorRows = await queryRows<CurrentActorRow>(
    client,
    'SELECT current_user AS current_actor',
  );
  if (actorRows[0]?.current_actor !== MIGRATOR_ROLE) {
    throw new Error(
      `Migrator preflight: SET LOCAL ROLE did not take effect (current_user="${actorRows[0]?.current_actor ?? ''}"); refusing to proceed.`,
    );
  }

  // Q7: migrator role must have CREATE on the migration target schema.
  const createRows = await queryRows<PrivilegeRow>(
    client,
    `SELECT has_schema_privilege($1, $2, $3) AS can_create`,
    [MIGRATOR_ROLE, entry.targetSchema, 'CREATE'],
  );
  if (createRows[0]?.can_create !== true) {
    throw new Error(
      `Migrator preflight: role "${MIGRATOR_ROLE}" lacks CREATE on schema "${entry.targetSchema}".`,
    );
  }

  // Q8: the runtime role must NOT have CREATE on the public schema (the
  // runtime role is supposed to be a data-only principal; CREATE on public
  // would let it install backdoors the migrator would then trust).
  const runtimeCreateRows = await queryRows<RuntimeCreateRow>(
    client,
    `SELECT has_schema_privilege($1, $2, 'CREATE') AS runtime_can_create`,
    [RUNTIME_ROLE, TARGET_SCHEMA],
  );
  if (runtimeCreateRows[0]?.runtime_can_create === true) {
    throw new Error(
      `Migrator preflight: role "${RUNTIME_ROLE}" must NOT have CREATE on schema "${TARGET_SCHEMA}"; the runtime role contract forbids it.`,
    );
  }

  // Q9: the runtime role must NOT own any migrable public object (tables,
  // partitioned tables, sequences, views, materialized views, functions).
  const runtimeOwnsRows = await queryRows<RuntimeOwnsRow>(
    client,
    `SELECT
       EXISTS(
         SELECT 1 FROM pg_class c
           JOIN pg_namespace n ON c.relnamespace = n.oid
           JOIN pg_roles r ON c.relowner = r.oid
          WHERE n.nspname = $2 AND r.rolname = $1
            AND c.relkind IN ('r', 'p', 'S', 'v', 'm')
       ) AS runtime_owns_tables,
       EXISTS(
         SELECT 1 FROM pg_proc p
           JOIN pg_namespace n ON p.pronamespace = n.oid
           JOIN pg_roles r ON p.proowner = r.oid
          WHERE n.nspname = $2 AND r.rolname = $1
       ) AS runtime_owns_functions,
       EXISTS(
         SELECT 1 FROM pg_class c
           JOIN pg_namespace n ON c.relnamespace = n.oid
           JOIN pg_roles r ON c.relowner = r.oid
          WHERE n.nspname = $2 AND r.rolname = $1 AND c.relkind = 'S'
       ) AS runtime_owns_sequences`,
    [RUNTIME_ROLE, TARGET_SCHEMA],
  );
  const owns = runtimeOwnsRows[0];
  if (
    owns !== undefined &&
    (owns.runtime_owns_tables === true ||
      owns.runtime_owns_functions === true ||
      owns.runtime_owns_sequences === true)
  ) {
    throw new Error(
      `Migrator preflight: role "${RUNTIME_ROLE}" owns migrable public objects; the runtime role contract forbids it.`,
    );
  }
}

interface SequenceOwnerRow extends Record<string, unknown> {
  sequence_relname: string | null;
  owner_table: string | null;
  owner_column: string | null;
}
interface SequenceAclRow extends Record<string, unknown> {
  grantee: string | null;
  privilege: string | null;
  is_grantable: boolean | null;
}
interface AclRow extends Record<string, unknown> {
  grantee: string | null;
  privilege_type: string | null;
  /**
   * `aclitem.is_grantable` is a `bool`; the previous information_schema
   * shape surfaced it as `'YES'/'NO'`. With the pg_class/pg_attribute
   * aclexplode-based verifier the row carries the raw boolean so the
   * runner can compare directly with `=== true`.
   */
  is_grantable: boolean | null;
}
interface FunctionAclRow extends Record<string, unknown> {
  grantee: string | null;
  privilege: string | null;
  is_grantable: boolean | null;
  proc_signature: string | null;
}
interface SchemaAclRow extends Record<string, unknown> {
  grantee: string | null;
  privilege: string | null;
  is_grantable: boolean | null;
}
interface TriggerRow extends Record<string, unknown> {
  trigger_name: string | null;
  event_object_table: string | null;
  action_timing: string | null;
  event_manipulation: string | null;
  action_orientation: string | null;
  is_constraint: boolean | null;
  deferrable: boolean | null;
  initially_deferred: boolean | null;
  function_signature: string | null;
  update_columns: readonly string[] | null;
}

export interface SupplementalVerification {
  passed: boolean;
  diffs: readonly string[];
  /**
   * True when the merged supplemental expectation is expected to write a
   * history pin (the merge includes the entry that creates
   * `lu_migration_history`). The runner uses it to decide whether to
   * populate `historyKey` on the `UpOutcome`. False when the entry is an
   * early migration whose target DB does not yet have the history table,
   * or when the apply lands on a stream / ordinal where the history write
   * is a no-op.
   */
  expectedHistoryWrite: boolean;
}

// ---------------------------------------------------------------------------
// Supplemental merge with LAST-ORDINAL-WINS dedup.
//
// Deduplication keys (chronological order; later ordinals override earlier):
//   sequence        name
//   trigger         table + name
//   table ACL       role + table + privilege + canonical columns (null=table-level)
//   sequence ACL    role + sequence + privilege
//   function ACL    role + signature (functionName, with parens) + privilege
//   schema ACL      role + schema + privilege
//
// CP0005 / CP0006 contract preserved:
//   CP0005 table-level INSERT/UPDATE true  →  CP0006 table-level false
//     (same dedup key: (role, table, INSERT, columns=null))
//   CP0006 column-level INSERT/UPDATE true  →  preserved
//     (different dedup key: (role, table, INSERT, columns=[...]))
//   CP0005 table-level SELECT true  →  CP0006 SELECT true (idempotent)
// ---------------------------------------------------------------------------

function dedupSequences(
  parts: readonly (MigrationSupplementalExpectation | undefined)[],
): readonly SequenceExpectation[] {
  const byName = new Map<string, SequenceExpectation>();
  for (const part of parts) {
    if (part === undefined) continue;
    for (const seq of part.sequences) {
      byName.set(seq.name, seq);
    }
  }
  return [...byName.values()];
}

function dedupTriggers(
  parts: readonly (MigrationSupplementalExpectation | undefined)[],
): readonly TriggerExpectation[] {
  const byKey = new Map<string, TriggerExpectation>();
  for (const part of parts) {
    if (part === undefined) continue;
    for (const trig of part.triggers) {
      byKey.set(`${trig.table}|${trig.name}`, trig);
    }
  }
  return [...byKey.values()];
}

interface FlatGrant {
  readonly privilege: 'SELECT' | 'INSERT' | 'UPDATE' | 'DELETE' | 'TRUNCATE';
  readonly columns: readonly string[] | null;
  readonly granted: boolean;
}

function canonicalColumnsKey(columns: readonly string[] | null): string {
  return columns === null ? 'null' : [...columns].sort().join(',');
}

function dedupTableAcls(
  parts: readonly (MigrationSupplementalExpectation | undefined)[],
): readonly TablePrivilegeExpectation[] {
  // Group grants by (role, table). Within each group dedup grants by
  // (privilege, columns) so table-level INSERT/UPDATE true (CP0005) and
  // table-level INSERT/UPDATE false (CP0006) collapse on the same key and
  // the later ordinal wins. Column-level grants live on a different key
  // and survive.
  type Group = { role: string; table: string; grants: Map<string, FlatGrant> };
  const groups = new Map<string, Group>();
  for (const part of parts) {
    if (part === undefined) continue;
    for (const acl of part.tableAcls) {
      const groupKey = `${acl.role}|${acl.table}`;
      let group = groups.get(groupKey);
      if (group === undefined) {
        group = { role: acl.role, table: acl.table, grants: new Map() };
        groups.set(groupKey, group);
      }
      for (const grant of acl.grants) {
        const key = `${grant.privilege}|${canonicalColumnsKey(grant.columns)}`;
        group.grants.set(key, {
          privilege: grant.privilege,
          columns: grant.columns,
          granted: grant.granted,
        });
      }
    }
  }
  const result: TablePrivilegeExpectation[] = [];
  for (const group of groups.values()) {
    result.push({
      role: group.role,
      table: group.table,
      grants: [...group.grants.values()],
    });
  }
  return result;
}

function dedupSequenceAcls(
  parts: readonly (MigrationSupplementalExpectation | undefined)[],
): readonly SequencePrivilegeExpectation[] {
  // Per-(role, sequence, privilege) tracking of granted state. Later ordinals
  // overwrite earlier ones on the same key (LAST-ORDINAL-WINS). Privileges are
  // only grouped into a single expectation when their granted state is the
  // SAME; mixed granted states are emitted as one expectation per privilege
  // so the verifier never collapses a positive SELECT with a negative USAGE
  // into a single bool derived from whichever privilege happens to be first.
  type PrivKey = string; // `${role}|${sequence}|${privilege}`
  type Row = {
    role: string;
    sequence: string;
    privilege: 'USAGE' | 'SELECT' | 'UPDATE';
    granted: boolean;
  };
  const byPriv = new Map<PrivKey, Row>();
  for (const part of parts) {
    if (part === undefined) continue;
    for (const acl of part.sequenceAcls) {
      for (const privilege of acl.privileges) {
        const key: PrivKey = `${acl.role}|${acl.sequence}|${privilege}`;
        byPriv.set(key, {
          role: acl.role,
          sequence: acl.sequence,
          privilege,
          granted: acl.granted,
        });
      }
    }
  }
  // Group the rows back by (role, sequence). Within each group, split
  // privileges by their granted value so that mixed-state groups produce
  // multiple expectations, while uniform groups produce a single expectation
  // listing every privilege. The ordering is stable: privileges are sorted
  // alphabetically so the verifier's `sort().join(',')` is deterministic.
  type GroupRows = { role: string; sequence: string; privileges: string[]; granted: boolean };
  const groups = new Map<string, GroupRows>();
  for (const row of byPriv.values()) {
    const groupKey = `${row.role}|${row.sequence}|${row.granted ? '1' : '0'}`;
    let g = groups.get(groupKey);
    if (g === undefined) {
      g = { role: row.role, sequence: row.sequence, privileges: [], granted: row.granted };
      groups.set(groupKey, g);
    }
    g.privileges.push(row.privilege);
  }
  const result: SequencePrivilegeExpectation[] = [];
  for (const g of groups.values()) {
    g.privileges.sort();
    result.push({
      role: g.role,
      sequence: g.sequence,
      privileges: g.privileges as readonly ('USAGE' | 'SELECT' | 'UPDATE')[],
      granted: g.granted,
    });
  }
  return result;
}

function dedupFunctionAcls(
  parts: readonly (MigrationSupplementalExpectation | undefined)[],
): readonly FunctionPrivilegeExpectation[] {
  const byKey = new Map<string, FunctionPrivilegeExpectation>();
  for (const part of parts) {
    if (part === undefined) continue;
    for (const acl of part.functionAcls) {
      byKey.set(`${acl.role}|${acl.functionName}|${acl.privilege}`, acl);
    }
  }
  return [...byKey.values()];
}

function dedupSchemaAcls(
  parts: readonly (MigrationSupplementalExpectation | undefined)[],
): readonly SchemaPrivilegeExpectation[] {
  const byKey = new Map<string, SchemaPrivilegeExpectation>();
  for (const part of parts) {
    if (part === undefined) continue;
    for (const acl of part.schemaAcls) {
      byKey.set(`${acl.role}|${acl.schema}|${acl.privilege}`, acl);
    }
  }
  return [...byKey.values()];
}

function dedupNotes(
  parts: readonly (MigrationSupplementalExpectation | undefined)[],
): readonly string[] {
  const notes: string[] = [];
  const seen = new Set<string>();
  for (const part of parts) {
    if (part === undefined) continue;
    for (const note of part.notes) {
      if (!seen.has(note)) {
        seen.add(note);
        notes.push(note);
      }
    }
  }
  return notes;
}

/**
 * Resolve the cumulative supplemental expectation for an apply / verify path.
 * For control-plane entries the supplemental is keyed by relativePath; for
 * tenant entries the lookup is keyed by ordinal. Every supplemental entry
 * is a per-ordinal expectation; the helper merges every entry up to and
 * including `maxOrdinal` into one aggregate using LAST-ORDINAL-WINS
 * dedup so a replay on a final-state DB verifies every applicable ACL,
 * sequence and trigger across every prior ordinal (the schema manifest uses
 * the same cumulative approach).
 *
 * `expectedHistoryWrite` is true when the merged set contains the entry
 * that CREATES `lu_migration_history` (CP 0006 or tenant 0013). The runner
 * uses it to decide whether `historyKey` should be written: when the
 * history table is absent (early migrations before 0006 / 0013), the
 * merged supplemental set may still describe sequences / ACLs / triggers
 * but it does not include any "we just created the history table" pin, so
 * no history write is expected for this apply.
 */
function supplementalFor(
  entry: MigrationRegistryEntry,
  maxOrdinal: number,
): { expectation: MigrationSupplementalExpectation; expectedHistoryWrite: boolean } | null {
  const cap = maxOrdinal > 0 ? maxOrdinal : entry.ordinal;
  if (entry.stream === 'control-plane') {
    const applicable = CONTROL_PLANE_REGISTRY.filter((candidate) => candidate.ordinal <= cap)
      .map((candidate) => CONTROL_PLANE_SUPPLEMENTAL_EXPECTATIONS[candidate.relativePath])
      .filter(
        (expectation): expectation is MigrationSupplementalExpectation => expectation !== undefined,
      );
    const current = applicable[applicable.length - 1];
    if (current === undefined) return null;
    return {
      expectation: {
        stream: 'control-plane',
        relativePath: current.relativePath,
        sequences: dedupSequences(applicable),
        triggers: dedupTriggers(applicable),
        tableAcls: dedupTableAcls(applicable),
        sequenceAcls: dedupSequenceAcls(applicable),
        functionAcls: dedupFunctionAcls(applicable),
        schemaAcls: dedupSchemaAcls(applicable),
        notes: dedupNotes(applicable),
      },
      expectedHistoryWrite: cap >= 6,
    };
  }
  const own =
    TENANT_SUPPLEMENTAL_EXPECTATIONS[
      entry.ordinal as 1 | 2 | 3 | 4 | 5 | 6 | 7 | 8 | 9 | 10 | 11 | 12 | 13
    ];
  if (own === undefined) return null;
  // Iterate ordinal 1..cap EXACTLY once, in ascending chronology, so that
  // later ordinals win on every dedup key (semantic dedup keys stay
  // unchanged; the iteration order is the LAST-ORDINAL-WINS tie-breaker).
  // The entry's own ordinal appears in its natural position; the prior
  // prepending pattern is removed because it forced the entry's own
  // expectation to overwrite any prior ordinal sharing the same key, even
  // when the cumulative cap is larger.
  const all: (MigrationSupplementalExpectation | undefined)[] = [];
  for (let o = 1; o <= cap; o += 1) {
    all.push(
      TENANT_SUPPLEMENTAL_EXPECTATIONS[o as 1 | 2 | 3 | 4 | 5 | 6 | 7 | 8 | 9 | 10 | 11 | 12 | 13],
    );
  }
  return {
    expectation: {
      stream: 'tenant',
      relativePath: own.relativePath,
      sequences: dedupSequences(all),
      triggers: dedupTriggers(all),
      tableAcls: dedupTableAcls(all),
      sequenceAcls: dedupSequenceAcls(all),
      functionAcls: dedupFunctionAcls(all),
      schemaAcls: dedupSchemaAcls(all),
      notes: dedupNotes(all),
    },
    expectedHistoryWrite: cap >= 13,
  };
}

/**
 * Verify the supplemental (sequences, ACLs, triggers, function ACLs) of the
 * given entry against the live catalog. Catalog queries are metadata-only;
 * no business rows are selected. Every expectation is enforced as either
 * a presence or an absence (negative ACLs and function denials MUST NOT be
 * observable in the catalog). Returns the first failure as a one-line
 * diff so the caller can ROLLBACK and surface the message.
 *
 * Exactness enforced:
 *  - sequence existence AND ownership (table + column via pg_catalog)
 *  - trigger name + table + timing + event set + UPDATE OF columns +
 *    constraint flag + deferrable + initiallyDeferred + function signature
 *    (via pg_catalog.pg_trigger since information_schema.triggers lacks
 *    the structural fields)
 *  - positive ACL row must have `is_grantable='NO'` (no GRANT OPTION)
 *  - negative ACL row must be absent
 */
async function verifySupplementalCatalog(
  client: Client,
  entry: MigrationRegistryEntry,
  maxOrdinal: number,
): Promise<SupplementalVerification> {
  const resolved = supplementalFor(entry, maxOrdinal);
  if (resolved === null) {
    return { passed: true, diffs: [], expectedHistoryWrite: false };
  }
  const sup = resolved.expectation;

  const diffs: string[] = [];

  // 1) Sequences: every declared sequence must exist AND be owned by the
  //    declared table/column. The catalog is `pg_class` (sequences are a
  //    `relkind = 'S'` row); the column name on pg_class is `relname`,
  //    NEVER `sequence_name` (which is a misreading of the legacy PG
  //    `sequence_name` view). Ownership is resolved through the standard
  //    `pg_depend` link to `pg_class` + `pg_attribute`. The deptype differs
  //    by declaration style: legacy serial-style ownership uses
  //    `deptype = 'a'` (auto); `GENERATED ... AS IDENTITY` columns use
  //    `deptype = 'i'` (internal/identity link from sequence -> column).
  //    When the expectation declares `identity: 'BY DEFAULT'` the verifier
  //    accepts EITHER deptype (real Postgres emits 'i' for IDENTITY) and
  //    additionally asserts `pg_attribute.attidentity = 'd'` on the owning
  //    column. Non-identity sequences keep the strict `deptype = 'a'` gate
  //    so a fake or broken identity link never silently passes.
  for (const seq of sup.sequences) {
    const isIdentity = seq.identity === 'BY DEFAULT';
    const deptypePredicate = isIdentity ? "d.deptype IN ('a', 'i')" : "d.deptype = 'a'";
    const rows = await queryRows<SequenceOwnerRow>(
      client,
      `SELECT s.relname  AS sequence_relname,
              tbl.relname AS owner_table,
              att.attname AS owner_column
         FROM pg_class s
         JOIN pg_namespace n ON s.relnamespace = n.oid
         JOIN pg_depend d    ON d.objid = s.oid AND ${deptypePredicate}
         JOIN pg_class tbl   ON tbl.oid = d.refobjid
         JOIN pg_attribute att
              ON att.attrelid = tbl.oid AND att.attnum = d.refobjsubid
        WHERE n.nspname = $1 AND s.relkind = 'S' AND s.relname = $2
        LIMIT 1`,
      ['public', seq.name],
    );
    if (rows.length === 0) {
      diffs.push(
        `missing sequence ${seq.name} (expected owned by ${seq.ownedByTable}.${seq.ownedByColumn}${isIdentity ? ' via identity link' : ''})`,
      );
      continue;
    }
    const actual = rows[0]!;
    if (actual.owner_table !== seq.ownedByTable) {
      diffs.push(
        `sequence ${seq.name} owned by ${actual.owner_table ?? '<null>'} (expected ${seq.ownedByTable})`,
      );
    }
    if (actual.owner_column !== seq.ownedByColumn) {
      diffs.push(
        `sequence ${seq.name} owned by column ${actual.owner_column ?? '<null>'} (expected ${seq.ownedByColumn})`,
      );
    }
    if (isIdentity) {
      // Identity link present: the owning column MUST carry
      // attidentity='d' (BY DEFAULT). This guards against a sequence that
      // happens to be linked from a non-identity column (deptype='a'
      // ownership without the identity attribute).
      const attrRows = await queryRows<{ attidentity: string | null }>(
        client,
        `SELECT COALESCE(a.attidentity, '') AS attidentity
           FROM pg_class tbl
           JOIN pg_namespace n ON n.oid = tbl.relnamespace
           JOIN pg_attribute a ON a.attrelid = tbl.oid
          WHERE n.nspname = $1 AND tbl.relname = $2 AND a.attname = $3
            AND a.attnum > 0 AND NOT a.attisdropped
          LIMIT 1`,
        ['public', seq.ownedByTable, seq.ownedByColumn],
      );
      const attidentity = attrRows[0]?.attidentity ?? '';
      if (attidentity !== 'd') {
        diffs.push(
          `sequence ${seq.name} identity link expected attidentity='d' on ${seq.ownedByTable}.${seq.ownedByColumn}, got '${attidentity}'`,
        );
      }
    }
  }

  // 2) Table privileges: positive grants MUST be present with no GRANT
  //    OPTION (`is_grantable=false`); negative grants MUST be absent.
  //    Column-level expectations check the (grantee, table, column,
  //    privilege) tuple; table-level expectations check the (grantee,
  //    table, privilege) tuple.
  //
  //    The verifier reads the catalog ACLs directly from pg_class.relacl
  //    (table-level) and pg_attribute.attacl (column-level) using
  //    `aclexplode(COALESCE(...))`. PUBLIC is resolved through grantee OID
  //    0; named roles are resolved through pg_roles.oid. This intentionally
  //    bypasses `information_schema.role_table_grants` and
  //    `information_schema.role_column_grants` because those views omit
  //    PUBLIC grants (and lose exact GRANT OPTION visibility for column-
  //    level grants when the table itself has no table-level grant).
  for (const acl of sup.tableAcls) {
    for (const grant of acl.grants) {
      const targetTable = acl.table;
      if (grant.columns === null) {
        const rows = await queryRows<AclRow>(
          client,
          `WITH cls AS (
              SELECT c.oid, c.relacl, c.relowner
                FROM pg_class c
                JOIN pg_namespace n ON c.relnamespace = n.oid
               WHERE n.nspname = $1 AND c.relname = $2
                  AND c.relkind IN ('r', 'p')
           ),
           exploded AS (
              SELECT
                (aclexplode(COALESCE(cls.relacl, acldefault('r', cls.relowner)))).grantee
                  AS grantee_oid,
                (aclexplode(COALESCE(cls.relacl, acldefault('r', cls.relowner)))).privilege_type
                  AS privilege,
                (aclexplode(COALESCE(cls.relacl, acldefault('r', cls.relowner)))).is_grantable
                  AS is_grantable
                FROM cls
           )
           SELECT
             CASE e.grantee_oid
               WHEN 0 THEN 'PUBLIC'
               ELSE COALESCE(r.rolname, '<oid:' || e.grantee_oid::text || '>')
             END AS grantee,
             e.privilege AS privilege_type,
             e.is_grantable AS is_grantable
             FROM exploded e
             LEFT JOIN pg_roles r ON r.oid = e.grantee_oid
            WHERE e.privilege = $3
              AND CASE e.grantee_oid
                    WHEN 0 THEN 'PUBLIC'
                    ELSE COALESCE(r.rolname, '<oid:' || e.grantee_oid::text || '>')
                  END = $4`,
          ['public', targetTable, grant.privilege, acl.role],
        );
        const present = rows.some(
          (r) => r.grantee === acl.role && r.privilege_type === grant.privilege,
        );
        const grantOption = rows.some((r) => r.is_grantable === true);
        if (grant.granted && !present) {
          diffs.push(`missing grant ${grant.privilege} ON ${targetTable} TO ${acl.role}`);
        } else if (grant.granted && present && grantOption) {
          diffs.push(
            `grant ${grant.privilege} ON ${targetTable} TO ${acl.role} carries GRANT OPTION`,
          );
        } else if (!grant.granted && present) {
          diffs.push(
            `unexpected grant ${grant.privilege} ON ${targetTable} TO ${acl.role} (negative expectation failed)`,
          );
        }
      } else {
        for (const column of grant.columns) {
          const rows = await queryRows<AclRow>(
            client,
            `WITH attr AS (
              SELECT a.attacl
                FROM pg_attribute a
                JOIN pg_class c ON c.oid = a.attrelid
                JOIN pg_namespace n ON c.relnamespace = n.oid
               WHERE n.nspname = $1 AND c.relname = $2 AND a.attname = $3
                 AND a.attnum > 0 AND NOT a.attisdropped
           ),
           exploded AS (
              SELECT
                (aclexplode(COALESCE(attr.attacl, '{}'::aclitem[]))).grantee
                  AS grantee_oid,
                (aclexplode(COALESCE(attr.attacl, '{}'::aclitem[]))).privilege_type
                  AS privilege,
                (aclexplode(COALESCE(attr.attacl, '{}'::aclitem[]))).is_grantable
                  AS is_grantable
                FROM attr
           )
           SELECT
             CASE e.grantee_oid
               WHEN 0 THEN 'PUBLIC'
               ELSE COALESCE(r.rolname, '<oid:' || e.grantee_oid::text || '>')
             END AS grantee,
             e.privilege AS privilege_type,
             e.is_grantable AS is_grantable
             FROM exploded e
             LEFT JOIN pg_roles r ON r.oid = e.grantee_oid
            WHERE e.privilege = $4
              AND CASE e.grantee_oid
                    WHEN 0 THEN 'PUBLIC'
                    ELSE COALESCE(r.rolname, '<oid:' || e.grantee_oid::text || '>')
                  END = $5`,
            ['public', targetTable, column, grant.privilege, acl.role],
          );
          const present = rows.some(
            (r) => r.grantee === acl.role && r.privilege_type === grant.privilege,
          );
          const grantOption = rows.some((r) => r.is_grantable === true);
          if (grant.granted && !present) {
            diffs.push(
              `missing grant ${grant.privilege} (${column}) ON ${targetTable} TO ${acl.role}`,
            );
          } else if (grant.granted && present && grantOption) {
            diffs.push(
              `grant ${grant.privilege} (${column}) ON ${targetTable} TO ${acl.role} carries GRANT OPTION`,
            );
          } else if (!grant.granted && present) {
            diffs.push(
              `unexpected grant ${grant.privilege} (${column}) ON ${targetTable} TO ${acl.role} (negative expectation failed)`,
            );
          }
        }
      }
    }
  }

  // 3) Sequence privileges: positive grants MUST be present, negative MUST
  //    be absent. The ACL lives on `pg_class.relacl` as a text[] column;
  //    we explode it with `aclexplode(COALESCE(relacl, acldefault('S',
  //    relowner)))` so PUBLIC (grantee 0) and named roles (grantee = OID
  //    looked up in pg_roles) are visible. This intentionally bypasses
  //    `information_schema.role_usage_privileges` and
  //    `information_schema.role_column_grants` because those views omit
  //    PUBLIC grants and cannot distinguish SELECT / USAGE / UPDATE on a
  //    sequence reliably. The PG `acldefault('S', relowner)` covers the
  //    case where relacl is NULL (which means "default privileges only",
  //    and the default is owner-only).
  for (const sacl of sup.sequenceAcls) {
    for (const privilege of sacl.privileges) {
      const rows = await queryRows<SequenceAclRow>(
        client,
        `WITH seq AS (
            SELECT c.oid, c.relacl, c.relowner
              FROM pg_class c
              JOIN pg_namespace n ON c.relnamespace = n.oid
             WHERE n.nspname = $1 AND c.relkind = 'S' AND c.relname = $2
         ),
         exploded AS (
            SELECT
              (aclexplode(COALESCE(seq.relacl, acldefault('S', seq.relowner)))).grantee
                AS grantee_oid,
              (aclexplode(COALESCE(seq.relacl, acldefault('S', seq.relowner)))).privilege_type
                AS privilege,
              (aclexplode(COALESCE(seq.relacl, acldefault('S', seq.relowner)))).is_grantable
                AS is_grantable
              FROM seq
         )
         SELECT
           CASE e.grantee_oid
             WHEN 0 THEN 'PUBLIC'
             ELSE COALESCE(r.rolname, '<oid:' || e.grantee_oid::text || '>')
           END AS grantee,
           e.privilege AS privilege,
           e.is_grantable AS is_grantable
           FROM exploded e
           LEFT JOIN pg_roles r ON r.oid = e.grantee_oid
          WHERE e.privilege = $3
            AND CASE e.grantee_oid
                  WHEN 0 THEN 'PUBLIC'
                  ELSE COALESCE(r.rolname, '<oid:' || e.grantee_oid::text || '>')
                END = $4`,
        ['public', sacl.sequence, privilege, sacl.role],
      );
      const present = rows.some((r) => r.grantee === sacl.role && r.privilege === privilege);
      const grantOption = rows.some((r) => r.is_grantable === true);
      if (sacl.granted && !present) {
        diffs.push(`missing grant ${privilege} ON SEQUENCE ${sacl.sequence} TO ${sacl.role}`);
      } else if (sacl.granted && present && grantOption) {
        diffs.push(
          `grant ${privilege} ON SEQUENCE ${sacl.sequence} TO ${sacl.role} carries GRANT OPTION`,
        );
      } else if (!sacl.granted && present) {
        diffs.push(
          `unexpected grant ${privilege} ON SEQUENCE ${sacl.sequence} TO ${sacl.role} (negative expectation failed)`,
        );
      }
    }
  }

  // 4) Function EXECUTE privileges: positive grants MUST be present,
  //    negative MUST be absent. The `functionName` field carries the
  //    function *signature* (e.g. `lu_user_status_no_diverge()` or
  //    `lu_login_identifier_normalize(text)`); the canonical lookup uses
  //    `to_regprocedure(<signature>)` so the signature matches exactly. The
  //    ACL lives on `pg_proc.proacl` and is exploded the same way
  //    `pg_class.relacl` is so PUBLIC (grantee 0) and named roles are
  //    visible. `information_schema.role_routine_grants` is intentionally
  //    NOT consulted because it omits PUBLIC, loses the function signature
  //    (it only carries `routine_name`), and conflates overloaded routines
  //    across schemas.
  for (const facl of sup.functionAcls) {
    const procOid = await queryRows<{ oid: number | null }>(
      client,
      `SELECT to_regprocedure($1::text)::oid AS oid`,
      [`public.${facl.functionName}`],
    );
    const oid = procOid[0]?.oid ?? null;
    if (oid === null) {
      diffs.push(`function ${facl.functionName} does not exist in schema public`);
      continue;
    }
    const rows = await queryRows<FunctionAclRow>(
      client,
      `WITH proc AS (
            SELECT p.proacl, p.proowner
              FROM pg_proc p
             WHERE p.oid = $1::oid
         ),
         exploded AS (
            SELECT
              (aclexplode(COALESCE(proc.proacl, acldefault('f', proc.proowner)))).grantee
                AS grantee_oid,
              (aclexplode(COALESCE(proc.proacl, acldefault('f', proc.proowner)))).privilege_type
                AS privilege,
              (aclexplode(COALESCE(proc.proacl, acldefault('f', proc.proowner)))).is_grantable
                AS is_grantable
              FROM proc
         )
         SELECT
           CASE e.grantee_oid
             WHEN 0 THEN 'PUBLIC'
             ELSE COALESCE(r.rolname, '<oid:' || e.grantee_oid::text || '>')
           END AS grantee,
           e.privilege AS privilege,
           e.is_grantable AS is_grantable,
           $2::text AS proc_signature
           FROM exploded e
           LEFT JOIN pg_roles r ON r.oid = e.grantee_oid
          WHERE e.privilege = $3
            AND CASE e.grantee_oid
                  WHEN 0 THEN 'PUBLIC'
                  ELSE COALESCE(r.rolname, '<oid:' || e.grantee_oid::text || '>')
                END = $4`,
      [oid, facl.functionName, facl.privilege, facl.role],
    );
    const present = rows.length > 0;
    const grantOption = rows.some((r) => r.is_grantable === true);
    if (facl.granted && !present) {
      diffs.push(
        `missing grant ${facl.privilege} ON FUNCTION ${facl.functionName} TO ${facl.role}`,
      );
    } else if (facl.granted && present && grantOption) {
      diffs.push(
        `grant ${facl.privilege} ON FUNCTION ${facl.functionName} TO ${facl.role} carries GRANT OPTION`,
      );
    } else if (!facl.granted && present) {
      diffs.push(
        `unexpected grant ${facl.privilege} ON FUNCTION ${facl.functionName} TO ${facl.role} (negative expectation failed)`,
      );
    }
  }

  // 5) Schema USAGE privileges: positive MUST be present, negative MUST be
  //    absent. The ACL lives on `pg_namespace.nspacl`; we explode it the
  //    same way sequence/function ACLs are, so PUBLIC (grantee 0) and
  //    named roles are visible. The default privilege type for schemas
  //    is `n` (acldefault('n', nspowner)). `information_schema.role_usage
  //    _privileges` is NOT consulted because it omits PUBLIC and conflates
  //    USAGE on schemas with USAGE / SELECT on sequences / types.
  for (const sacl of sup.schemaAcls) {
    const rows = await queryRows<SchemaAclRow>(
      client,
      `WITH ns AS (
            SELECT n.nspacl, n.nspowner
              FROM pg_namespace n
             WHERE n.nspname = $1
         ),
         exploded AS (
            SELECT
              (aclexplode(COALESCE(ns.nspacl, acldefault('n', ns.nspowner)))).grantee
                AS grantee_oid,
              (aclexplode(COALESCE(ns.nspacl, acldefault('n', ns.nspowner)))).privilege_type
                AS privilege,
              (aclexplode(COALESCE(ns.nspacl, acldefault('n', ns.nspowner)))).is_grantable
                AS is_grantable
              FROM ns
         )
         SELECT
           CASE e.grantee_oid
             WHEN 0 THEN 'PUBLIC'
             ELSE COALESCE(r.rolname, '<oid:' || e.grantee_oid::text || '>')
           END AS grantee,
           e.privilege AS privilege,
           e.is_grantable AS is_grantable
           FROM exploded e
           LEFT JOIN pg_roles r ON r.oid = e.grantee_oid
          WHERE e.privilege = $2
            AND CASE e.grantee_oid
                  WHEN 0 THEN 'PUBLIC'
                  ELSE COALESCE(r.rolname, '<oid:' || e.grantee_oid::text || '>')
                END = $3`,
      [sacl.schema, sacl.privilege, sacl.role],
    );
    const present = rows.length > 0;
    const grantOption = rows.some((r) => r.is_grantable === true);
    if (sacl.granted && !present) {
      diffs.push(`missing grant ${sacl.privilege} ON SCHEMA ${sacl.schema} TO ${sacl.role}`);
    } else if (sacl.granted && present && grantOption) {
      diffs.push(
        `grant ${sacl.privilege} ON SCHEMA ${sacl.schema} TO ${sacl.role} carries GRANT OPTION`,
      );
    } else if (!sacl.granted && present) {
      diffs.push(
        `unexpected grant ${sacl.privilege} ON SCHEMA ${sacl.schema} TO ${sacl.role} (negative expectation failed)`,
      );
    }
  }

  // 6) Triggers: every declared trigger must exist on the declared table,
  //    with the EXACT timing, event set, UPDATE OF columns, function regprocedure
  //    signature, constraint / non-constraint flag, deferrable and
  //    initiallyDeferred flags, and ROW orientation. information_schema.triggers
  //    lacks UPDATE OF columns, the constraint flag, the deferrable flags, and
  //    the function signature; the verifier reads pg_catalog.pg_trigger
  //    directly and joins pg_proc + pg_namespace for the function signature.
  //    UPDATE OF columns live on `pg_trigger.tgattr` (an int2[] of attnum
  //    values) and are resolved to names through `pg_attribute`. Fail-closed:
  //    when more than one matching trigger row exists for (schema, table,
  //    name) the runner rejects (duplicate trigger definition; not expected
  //    at the per-entry or cumulative scope).
  //
  //    Event bits are decoded INDEPENDENTLY per event (INSERT=4, DELETE=8,
  //    UPDATE=16, TRUNCATE=32) — the previous CASE mask over `(tgtype & 28)`
  //    lost multi-event combinations because it only matched the first bit
  //    set. Each bit is checked independently with `(tgtype::int & bit) <> 0`
  //    so INSERT+UPDATE, INSERT+DELETE+UPDATE etc. produce the full ordered
  //    list of events.
  //
  //    `is_constraint` is `tgconstraint <> 0` (per pg_trigger column
  //    definition), NOT `tgtype::int & 32 = 32` — bit 32 of tgtype is the
  //    TRUNCATE event and conflating the two loses TRUNCATE event coverage
  //    and reports false-positive constraint triggers.
  //
  //    Orientation must be ROW for the current expectation set; any
  //    STATEMENT orientation is rejected (the migration contract forbids
  //    statement-level triggers on these surfaces).
  for (const trig of sup.triggers) {
    const rows = await queryRows<TriggerRow>(
      client,
      `SELECT t.tgname AS trigger_name,
              c.relname AS event_object_table,
              CASE (t.tgtype::int & 66)
                WHEN 2 THEN 'BEFORE'
                WHEN 64 THEN 'INSTEAD OF'
                ELSE 'AFTER'
              END AS action_timing,
              (
                SELECT string_agg(decode.event_name, ',' ORDER BY decode.event_order)
                  FROM (VALUES
                    (1, 'INSERT',   4),
                    (2, 'DELETE',   8),
                    (3, 'UPDATE',  16),
                    (4, 'TRUNCATE', 32)
                  ) AS decode(event_order, event_name, event_bit)
                 WHERE (t.tgtype::int & decode.event_bit) <> 0
              ) AS event_manipulation,
              CASE t.tgtype::int & 1 WHEN 1 THEN 'ROW' ELSE 'STATEMENT' END AS action_orientation,
              (t.tgconstraint <> 0) AS is_constraint,
              t.tgdeferrable AS deferrable,
              t.tginitdeferred AS initially_deferred,
              p.oid::regprocedure::text AS function_signature,
              COALESCE(
                (SELECT array_agg(att.attname::text ORDER BY att.attname)
                   FROM unnest(t.tgattr) AS u(attnum)
                   JOIN pg_attribute att
                     ON att.attrelid = t.tgrelid AND att.attnum = u.attnum),
                ARRAY[]::text[]
              ) AS update_columns
         FROM pg_trigger t
         JOIN pg_class c ON c.oid = t.tgrelid
         JOIN pg_namespace n ON n.oid = c.relnamespace
         LEFT JOIN pg_proc p ON p.oid = t.tgfoid
        WHERE n.nspname = $1 AND c.relname = $2 AND t.tgname = $3 AND NOT t.tgisinternal
         GROUP BY t.tgname, c.relname, t.tgrelid, t.tgtype, t.tgattr, p.oid, t.tgdeferrable, t.tginitdeferred, t.tgconstraint`,
      ['public', trig.table, trig.name],
    );
    if (rows.length === 0) {
      diffs.push(`missing trigger ${trig.name} ON ${trig.table}`);
      continue;
    }
    if (rows.length > 1) {
      diffs.push(
        `trigger ${trig.name} ON ${trig.table} has ${rows.length} matching rows in pg_trigger (expected exactly 1)`,
      );
      continue;
    }
    const actual = rows[0]!;
    const expectedEvents = [...trig.events].sort().join(',');
    const actualEvents = String(actual.event_manipulation ?? '')
      .split(',')
      .sort()
      .join(',');
    if (actual.action_timing !== trig.timing) {
      diffs.push(
        `trigger ${trig.name} ON ${trig.table} timing is ${actual.action_timing} (expected ${trig.timing})`,
      );
    }
    if (actualEvents !== expectedEvents) {
      diffs.push(
        `trigger ${trig.name} ON ${trig.table} events are ${actualEvents} (expected ${expectedEvents})`,
      );
    }
    const actualUpdateColumns = (actual.update_columns ?? []).slice().sort().join(',');
    const expectedUpdateColumns = (trig.updateColumns ?? []).slice().sort().join(',');
    if (actualUpdateColumns !== expectedUpdateColumns) {
      diffs.push(
        `trigger ${trig.name} ON ${trig.table} UPDATE OF columns are [${actualUpdateColumns}] (expected [${expectedUpdateColumns}])`,
      );
    }
    if ((actual.is_constraint ?? false) !== trig.isConstraintTrigger) {
      diffs.push(
        `trigger ${trig.name} ON ${trig.table} is_constraint is ${actual.is_constraint} (expected ${trig.isConstraintTrigger})`,
      );
    }
    if ((actual.deferrable ?? false) !== trig.deferrable) {
      diffs.push(
        `trigger ${trig.name} ON ${trig.table} deferrable is ${actual.deferrable} (expected ${trig.deferrable})`,
      );
    }
    if ((actual.initially_deferred ?? false) !== trig.initiallyDeferred) {
      diffs.push(
        `trigger ${trig.name} ON ${trig.table} initially_deferred is ${actual.initially_deferred} (expected ${trig.initiallyDeferred})`,
      );
    }
    if (actual.action_orientation !== 'ROW') {
      diffs.push(
        `trigger ${trig.name} ON ${trig.table} orientation is ${actual.action_orientation} (expected ROW)`,
      );
    }
    const actualSig = String(actual.function_signature ?? '').replace(/^public\./, '');
    const expectedSig = trig.functionName;
    if (actualSig !== expectedSig) {
      diffs.push(
        `trigger ${trig.name} ON ${trig.table} function signature is ${actualSig || '<null>'} (expected ${expectedSig})`,
      );
    }
  }

  return {
    passed: diffs.length === 0,
    diffs,
    expectedHistoryWrite: resolved.expectedHistoryWrite,
  };
}

/**
 * Resolve a tenant migration configuration. Exported for the CLI to call
 * before `applyMigration`. Returns the resolved control-plane connection
 * factory (so the runner can re-use it) plus the tenant config.
 *
 * Removed in W9: the previous `resolveAndApplyTenant` helper merged the
 * resolution and the apply into one call, which meant the runner could be
 * invoked with the WRONG (control-plane) config when a future bug passed
 * the site UUID down the wrong path. The CLI is now the single place that
 * resolves the tenant config via `resolveTenantConfig`, then calls
 * `applyMigration` with the tenant connection.
 */
export { resolveTenantConfig } from './tenant-config.js';

export { DEFAULT_STREAM, CONTROL_PLANE_TABLES_POST_F2 };
export type { MigrationRegistryEntry };
export { resolveEntryPath };
// Internal helpers exported for the unit test suite (which mocks pg and
// exercises the W10/W11/W13A invariants end-to-end).
export const __testing = {
  manifestForEntry,
  mergeTenantManifests,
  composeControlPlaneRouteFinalPlusAcademic,
  cumulativeManifestFor,
  manifestFor,
  inspectionTablesFor,
  inspectTarget,
  historyTableExists,
  fetchMaxOrdinalForStream,
  assertLedgerContiguousAndPinned,
  assertPinAtOrdinal,
  comparePinRow,
  backfillHistoryThroughOrdinal,
  assertMigratorIdentityAndPrivileges,
  acquireStreamCommonLock,
  verifySupplementalCatalog,
  supplementalFor,
  dedupSequences,
  dedupTriggers,
  dedupTableAcls,
  dedupSequenceAcls,
  dedupFunctionAcls,
  dedupSchemaAcls,
  dedupNotes,
};
