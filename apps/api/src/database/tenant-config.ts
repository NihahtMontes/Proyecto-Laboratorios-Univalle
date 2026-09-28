/**
 * Server-side tenant configuration resolution and migration lease.
 *
 * Two distinct surfaces, both pure and DB-free at the module boundary (every
 *  PostgreSQL interaction flows through an injected client factory):
 *
 *  1. `resolveTenantConfig(siteId, operation, ...)` — read-only metadata
 *     lookup used by the `status` and `verify` CLI commands. Runs the control
 *     plane query inside an explicit READ ONLY transaction with
 *     `SET LOCAL ROLE lu_auth_migrator`, COMMITS, ROLLBACKS on any error, and
 *     closes the client exactly once. NEVER touches the tenant DB.
 *
 *  2. `withTenantMigrationLease({...})` — write-side fence for the `up` CLI
 *     command. Acquires a GLOBAL advisory lock shared by every site
 *     (the contract forbids parallel migrations across sites), pins the
 *     target route row with `FOR UPDATE`, validates it, opens a callback
 *     window during which the runner commits the tenant DDL on the
 *     SEPARATE tenant DB, and only then advances the route row's
 *     `schema_version` to GREATEST(existing, ordinal) inside the SAME
 *     control-plane transaction. COMMIT only after the guarded UPDATE
 *     affected exactly one row; otherwise ROLLBACK.
 *
 * Safety invariants (MIG-001-F2-W15A):
 *  - `writer_label` allow-list: LEGACY_SQLSERVER / NEST_SQLSERVER / NEST_POSTGRES
 *  - `state = 'migrating'` is the ONLY entry point for the lease; the lease
 *    refuses to advance the route row from any other state.
 *  - migration_secret_reference must match the F1 regex; the env var name
 *    derived from the sanitized reference is the ONLY lookup path; the raw
 *    ref, the env var name, the DSN and the password are NEVER echoed in
 *    error messages or log output.
 *  - The lease holds the control-plane transaction AND client open for the
 *    full duration of the tenant-side callback. No distributed transaction
 *    is used: a tenant commit followed by a CP-update failure leaves the
 *    route in `migrating` and surfaces an explicit indeterminate/recovery-
 *    required error instead of attempting a rollback.
 *  - `writer_label`, `state` and `migration_secret_reference` of the route
 *    row are NEVER mutated by the lease. Only `schema_version`,
 *    `last_health_at` and `updated_at` are written.
 *  - `schema_version` is monotonic (GREATEST).
 *  - The client closes exactly once on every path (success or failure).
 *
 * Reference sanitization replaces every non-alphanumeric character with `_`
 * (matches the existing `TENANT_CONNECTION__TENANT_LOCAL_RUNTIME` example in
 * .env.example). The DSN value, the password, the reference itself, and the
 * env var name are NEVER logged or returned to callers; the returned object
 * holds only the parsed `PostgresConnectionConfig` plus a derived host/db
 * summary that is safe for logs.
 *
 * Operation-mode state gate (W9 contract):
 *  - `up`        requires `state = 'migrating'`; `active`, `degraded` and
 *                `disabled` are refused.
 *  - `status`    accepts `active` and `migrating`; `degraded` and
 *                `disabled` are refused.
 *  - `verify`    accepts `active` and `migrating`; `degraded` and
 *                `disabled` are refused.
 */
import {
  CONNECTION_STRING_ENV_VAR,
  loadConnectionConfigFromEnv,
  type PostgresConnectionConfig,
} from './connection-config.js';
import { TENANT_REGISTRY } from './migration-registry.js';

export type TenantOperationMode = 'up' | 'status' | 'verify';

export interface TenantSafeTarget {
  host: string;
  port: number;
  database: string;
}

export interface TenantConfigResolution {
  readonly config: PostgresConnectionConfig;
  /**
   * Secret-free, log-safe summary of the tenant target. Never includes the
   * reference, the env var name, the password or the DSN.
   */
  readonly safeTarget: TenantSafeTarget;
}

export class TenantConfigError extends Error {
  constructor(message: string) {
    super(message);
    this.name = 'TenantConfigError';
  }
}

/**
 * Raised when the tenant callback reports success but the control-plane
 * route row UPDATE did not affect exactly one row, or when any other
 * post-callback, post-validation inconsistency is detected. The lease
 * explicitly does NOT attempt a distributed rollback: the tenant DB is
 * independently committed; the route row remains in `migrating` until an
 * operator advances or resets it.
 */
export class TenantMigrationLeaseIndeterminateError extends TenantConfigError {
  constructor(message: string) {
    super(message);
    this.name = 'TenantMigrationLeaseIndeterminateError';
  }
}

/**
 * Raised when the lease transitions out of `up` mode because the route was
 * not in `migrating`, because the row was missing, or because one of the
 * validation gates failed before the callback had a chance to run. The
 * tenant callback was NEVER invoked; the control-plane transaction was
 * ROLLBACKed.
 */
export class TenantMigrationLeaseError extends TenantConfigError {
  constructor(message: string) {
    super(message);
    this.name = 'TenantMigrationLeaseError';
  }
}

const REF_RE = /^[A-Za-z][A-Za-z0-9._/-]{2,127}$/;
const UUID_RE = /^[0-9a-f]{8}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{12}$/i;

/**
 * Frozen writer-label set. Mirrors the in-DB CHECK constraint
 * (ck_lu_tenant_route_writer_label) and the project's F1 contracts.
 */
const ALLOWED_LABELS = new Set<string>(['LEGACY_SQLSERVER', 'NEST_SQLSERVER', 'NEST_POSTGRES']);

export type TenantWriterLabel = 'LEGACY_SQLSERVER' | 'NEST_SQLSERVER' | 'NEST_POSTGRES';

const ALLOWED_STATE_UP = new Set<string>(['migrating']);
const ALLOWED_STATE_READONLY = new Set<string>(['active', 'migrating']);

/**
 * Global advisory-lock key shared by EVERY site. Used by `withTenantMigrationLease`
 * to serialize all tenant-side migrations across the entire fleet (the contract
 * forbids parallel migrations across sites). The key is hashed server-side via
 * PostgreSQL's `hashtext()` function (the same convention the runner uses for
 * per-entry and per-site advisory locks). Only this constant lives in the key:
 * no env var name, no DSN, no password, no site UUID, no reference.
 */
export const TENANT_MIGRATION_GLOBAL_LOCK_KEY =
  'lu:identity-control-plane:tenant-migration-lease:v1';

/** Sanitize a `lu_tenant_route.migration_secret_reference` to an env var name. */
export function sanitizeReference(ref: string): string {
  return ref.replace(/[^A-Za-z0-9]+/g, '_');
}

function envVarName(sanitized: string): string {
  return `TENANT_MIGRATION_CONNECTION__${sanitized}`;
}

function safeLabelEcho(label: string): string {
  return /^[A-Z][A-Z0-9_]{2,31}$/.test(label) ? label : '(redacted)';
}

function safeStateEcho(state: string): string {
  return /^[a-z][a-z0-9_-]{2,31}$/.test(state) ? state : '(redacted)';
}

// ---------------------------------------------------------------------------
// Migrator preflight (resolver + lease).
//
// Runs BEFORE SET LOCAL ROLE and BEFORE the route lookup so generic /
// redacted metadata is rejected without ever reaching the route query or
// the tenant callback. The runner has its own APPLY-time preflight on the
// tenant connection; this one is the read-only/lease-side defence.
//
//   Q1  session role    pg_roles WHERE rolname = current_user
//   Q2  migrator role   pg_roles WHERE rolname = 'lu_auth_migrator'
//   Q3  session member  pg_has_role(current_user, 'lu_auth_migrator', 'MEMBER')
//   Q4  runtime role    pg_roles WHERE rolname = 'lu_auth_runtime'
//
// Group (migrator) safe attrs: exists, NOLOGIN, NOSUPERUSER, NOCREATEDB,
// NOCREATEROLE, NOREPLICATION, NOBYPASSRLS.
// Session safe attrs: LOGIN, non-super, no create-role/db/replication/bypass,
// not runtime/migrator, member of group.
// Runtime safe attrs: exists, NOLOGIN, no elevated attrs (super / create
// role / db / replication / bypass RLS), no schema CREATE on public, no
// ownership of migrable public objects (tables / sequences / functions /
// views / matviews / partitioned tables).
// ---------------------------------------------------------------------------

interface ResolverSessionRow extends Record<string, unknown> {
  rolname: string | null;
  rolcanlogin: boolean | null;
  rolsuper: boolean | null;
  rolcreaterole: boolean | null;
  rolcreatedb: boolean | null;
  rolreplication: boolean | null;
  rolbypassrls: boolean | null;
}

interface ResolverMigratorRow extends Record<string, unknown> {
  rolname: string | null;
  rolcanlogin: boolean | null;
  rolsuper: boolean | null;
  rolcreaterole: boolean | null;
  rolcreatedb: boolean | null;
  rolreplication: boolean | null;
  rolbypassrls: boolean | null;
}

interface ResolverMembershipRow extends Record<string, unknown> {
  is_member: boolean | null;
}

interface ResolverRuntimeRow extends Record<string, unknown> {
  rolname: string | null;
  rolcanlogin: boolean | null;
  rolsuper: boolean | null;
  rolcreaterole: boolean | null;
  rolcreatedb: boolean | null;
  rolreplication: boolean | null;
  rolbypassrls: boolean | null;
}

interface ResolverRuntimeCanCreateRow extends Record<string, unknown> {
  runtime_can_create: boolean | null;
}

interface ResolverRuntimeOwnsRow extends Record<string, unknown> {
  runtime_owns_objects: boolean | null;
}

const RESOLVER_RUNTIME_ROLE = 'lu_auth_runtime';
const RESOLVER_MIGRATOR_ROLE = 'lu_auth_migrator';

async function assertResolverPreflight(client: TenantClient): Promise<void> {
  // Q1: session role attributes (LOGIN, non-super, no elevated attrs, not
  //     the migrator/runtime managed identity).
  const sessionRows = await client.query<ResolverSessionRow>(
    `SELECT rolname, rolcanlogin, rolsuper, rolcreaterole, rolcreatedb,
            rolreplication, rolbypassrls
       FROM pg_roles
      WHERE rolname = current_user`,
  );
  const session = sessionRows.rows[0];
  if (session === undefined) {
    throw new TenantConfigError(
      'migrator preflight: pg_roles returned no row for current_user; refusing to read tenant metadata.',
    );
  }
  const sessionName = session.rolname ?? '';
  if (sessionName === RESOLVER_RUNTIME_ROLE || sessionName === RESOLVER_MIGRATOR_ROLE) {
    throw new TenantConfigError(
      `migrator preflight: session role "${sessionName}" is a managed identity and cannot read tenant metadata.`,
    );
  }
  if (session.rolcanlogin !== true) {
    throw new TenantConfigError(
      `migrator preflight: session role "${sessionName}" must be LOGIN; the migrator contract requires an unprivileged login user.`,
    );
  }
  if (session.rolsuper === true) {
    throw new TenantConfigError(
      'migrator preflight: session role is a PostgreSQL superuser; the migrator contract forbids it.',
    );
  }
  if (session.rolcreaterole === true) {
    throw new TenantConfigError(
      'migrator preflight: session role has CREATEROLE; the migrator contract forbids it.',
    );
  }
  if (session.rolcreatedb === true) {
    throw new TenantConfigError(
      'migrator preflight: session role has CREATEDB; the migrator contract forbids it.',
    );
  }
  if (session.rolreplication === true) {
    throw new TenantConfigError(
      'migrator preflight: session role has REPLICATION; the migrator contract forbids it.',
    );
  }
  if (session.rolbypassrls === true) {
    throw new TenantConfigError(
      'migrator preflight: session role has BYPASSRLS; the migrator contract forbids it.',
    );
  }

  // Q2: migrator role presence + safe attrs (exists, NOLOGIN, no elevated
  //     attrs).
  const migratorRows = await client.query<ResolverMigratorRow>(
    `SELECT rolname, rolcanlogin, rolsuper, rolcreaterole, rolcreatedb,
            rolreplication, rolbypassrls
       FROM pg_roles
      WHERE rolname = $1`,
    [RESOLVER_MIGRATOR_ROLE],
  );
  const migrator = migratorRows.rows[0];
  if (migrator === undefined) {
    throw new TenantConfigError(
      `migrator preflight: role "${RESOLVER_MIGRATOR_ROLE}" is absent; refusing to read tenant metadata.`,
    );
  }
  if (migrator.rolcanlogin === true) {
    throw new TenantConfigError(
      `migrator preflight: role "${RESOLVER_MIGRATOR_ROLE}" must be NOLOGIN.`,
    );
  }
  if (migrator.rolsuper === true) {
    throw new TenantConfigError(
      `migrator preflight: role "${RESOLVER_MIGRATOR_ROLE}" must be NOSUPERUSER.`,
    );
  }
  if (migrator.rolcreaterole === true) {
    throw new TenantConfigError(
      `migrator preflight: role "${RESOLVER_MIGRATOR_ROLE}" must be NOCREATEROLE.`,
    );
  }
  if (migrator.rolcreatedb === true) {
    throw new TenantConfigError(
      `migrator preflight: role "${RESOLVER_MIGRATOR_ROLE}" must be NOCREATEDB.`,
    );
  }
  if (migrator.rolreplication === true) {
    throw new TenantConfigError(
      `migrator preflight: role "${RESOLVER_MIGRATOR_ROLE}" must be NOREPLICATION.`,
    );
  }
  if (migrator.rolbypassrls === true) {
    throw new TenantConfigError(
      `migrator preflight: role "${RESOLVER_MIGRATOR_ROLE}" must be NOBYPASSRLS.`,
    );
  }

  // Q3: session role must be a member of the migrator group.
  const membershipRows = await client.query<ResolverMembershipRow>(
    `SELECT pg_has_role(current_user, $1, 'MEMBER') AS is_member`,
    [RESOLVER_MIGRATOR_ROLE],
  );
  if (membershipRows.rows[0]?.is_member !== true) {
    throw new TenantConfigError(
      `migrator preflight: session role "${sessionName}" is not a member of role "${RESOLVER_MIGRATOR_ROLE}"; the migrator contract requires explicit membership.`,
    );
  }

  // Q4: runtime role presence + safe attrs (exists, NOLOGIN, no elevated
  //     attrs, no schema CREATE on public, no ownership of migrable
  //     public objects).
  const runtimeRows = await client.query<ResolverRuntimeRow>(
    `SELECT rolname, rolcanlogin, rolsuper, rolcreaterole, rolcreatedb,
            rolreplication, rolbypassrls
       FROM pg_roles
      WHERE rolname = $1`,
    [RESOLVER_RUNTIME_ROLE],
  );
  const runtime = runtimeRows.rows[0];
  if (runtime === undefined) {
    throw new TenantConfigError(
      `migrator preflight: role "${RESOLVER_RUNTIME_ROLE}" is absent; refusing to read tenant metadata.`,
    );
  }
  if (runtime.rolcanlogin === true) {
    throw new TenantConfigError(
      `migrator preflight: role "${RESOLVER_RUNTIME_ROLE}" must be NOLOGIN.`,
    );
  }
  if (
    runtime.rolsuper === true ||
    runtime.rolcreaterole === true ||
    runtime.rolcreatedb === true ||
    runtime.rolreplication === true ||
    runtime.rolbypassrls === true
  ) {
    throw new TenantConfigError(
      `migrator preflight: role "${RESOLVER_RUNTIME_ROLE}" must NOT have elevated attributes (super / create role / db / replication / bypass RLS).`,
    );
  }
  const runtimeCreateRows = await client.query<ResolverRuntimeCanCreateRow>(
    `SELECT has_schema_privilege($1::text, $2::text, 'CREATE') AS runtime_can_create`,
    [RESOLVER_RUNTIME_ROLE, 'public'],
  );
  if (runtimeCreateRows.rows[0]?.runtime_can_create === true) {
    throw new TenantConfigError(
      `migrator preflight: role "${RESOLVER_RUNTIME_ROLE}" must NOT have CREATE on schema "public".`,
    );
  }
  const runtimeOwnsRows = await client.query<ResolverRuntimeOwnsRow>(
    `SELECT EXISTS (
              SELECT 1 FROM pg_class c
                JOIN pg_namespace n ON c.relnamespace = n.oid
                JOIN pg_roles r ON c.relowner = r.oid
               WHERE n.nspname = 'public' AND r.rolname = $1
                 AND c.relkind IN ('r', 'p', 'S', 'v', 'm')
             ) OR EXISTS (
              SELECT 1 FROM pg_proc p
                JOIN pg_namespace n ON p.pronamespace = n.oid
                JOIN pg_roles r ON p.proowner = r.oid
               WHERE n.nspname = 'public' AND r.rolname = $1
             ) AS runtime_owns_objects`,
    [RESOLVER_RUNTIME_ROLE],
  );
  if (runtimeOwnsRows.rows[0]?.runtime_owns_objects === true) {
    throw new TenantConfigError(
      `migrator preflight: role "${RESOLVER_RUNTIME_ROLE}" must NOT own migrable public objects (tables / sequences / functions / views / matviews / partitioned tables).`,
    );
  }
}

/**
 * Minimal control-plane client interface shared by the read-only resolver
 * (`resolveTenantConfig`) and the lease (`withTenantMigrationLease`).
 * Transaction control (BEGIN / SET LOCAL ROLE / COMMIT / ROLLBACK) is
 * expressed through the same `query` channel; implementations must run
 * each call sequentially against the same connection.
 */
export interface TenantClient {
  query<T extends Record<string, unknown>>(
    text: string,
    values?: unknown[],
  ): Promise<{ rows: T[] }>;
  close(): Promise<void>;
}

/** Factory that opens a control-plane client for the given DSN. */
export type TenantClientFactory = (config: PostgresConnectionConfig) => Promise<TenantClient>;

/**
 * Read-only metadata view of a single route row. The read-only path only
 * ever SELECTs these columns; the lease `SELECT ... FOR UPDATE` widens the
 * selection to include `schema_version` so it can advance it guarded.
 */
interface RouteLookupRow extends Record<string, unknown> {
  migration_secret_reference: string | null;
  writer_label: string | null;
  state: string | null;
}

/**
 * Resolve a tenant migration connection for the given site (read-only path).
 * The `controlPlaneConfig` is the connection whose DSN was supplied via
 * `ConnectionStrings__DefaultConnection`; the runner uses it to look up the
 * migration_secret_reference.
 *
 * `env` is injected: the only production caller is the CLI entry point
 * (`migration-cli.ts`), which passes `process.env`; the runner and tests
 * pass an explicit env record so this module never reads `process.env`
 * directly.
 *
 * The metadata SELECT runs inside a READ ONLY transaction with
 * `SET LOCAL ROLE lu_auth_migrator`. The transaction is COMMITted on
 * success and ROLLBACKed on any error; the client closes exactly once on
 * every path.
 */
export async function resolveTenantConfig(
  siteId: string,
  operation: TenantOperationMode,
  controlPlaneConfig: PostgresConnectionConfig,
  env: Record<string, string | undefined>,
  controlPlaneClientFactory: TenantClientFactory,
): Promise<TenantConfigResolution> {
  if (!UUID_RE.test(siteId)) {
    throw new TenantConfigError(`site UUID is not RFC 4122 shaped`);
  }
  const allowedStates = operation === 'up' ? ALLOWED_STATE_UP : ALLOWED_STATE_READONLY;
  const expectedState = operation === 'up' ? 'migrating' : 'active or migrating';

  const client = await controlPlaneClientFactory(controlPlaneConfig);
  let transactionOpened: boolean;
  let closed = false;
  const closeOnce = async (): Promise<void> => {
    if (closed) return;
    closed = true;
    await client.close().catch(() => undefined);
  };
  try {
    // Read-only metadata transaction. The BEGIN is part of the contract
    // for the control-plane connection in migrator mode: only the migrator
    // role reads the `migration_secret_reference` column. The preflight
    // runs BEFORE SET LOCAL ROLE so the role contract is enforced even when
    // the SET itself would silently mask an unsafe state.
    await client.query('BEGIN ISOLATION LEVEL REPEATABLE READ READ ONLY');
    transactionOpened = true;
    try {
      await assertResolverPreflight(client);
      await client.query('SET LOCAL ROLE lu_auth_migrator');
      const r = await client.query<RouteLookupRow>(
        `SELECT migration_secret_reference, writer_label, state
           FROM public.lu_tenant_route
          WHERE site_id = $1
          LIMIT 1`,
        [siteId.toLowerCase()],
      );
      const row = r.rows[0] ?? null;
      if (row === null) {
        throw new TenantConfigError(
          `lu_tenant_route row for site ${siteId} is missing (route state must be ${expectedState})`,
        );
      }
      const ref = row.migration_secret_reference ?? null;
      if (ref === null) {
        throw new TenantConfigError(
          `lu_tenant_route row for site ${siteId} has no migration_secret_reference`,
        );
      }
      if (!REF_RE.test(ref)) {
        throw new TenantConfigError(
          `lu_tenant_route.migration_secret_reference for site ${siteId} failed format check`,
        );
      }
      const writerLabel = row.writer_label ?? null;
      if (writerLabel === null || !ALLOWED_LABELS.has(writerLabel)) {
        throw new TenantConfigError(
          `lu_tenant_route.writer_label ${safeLabelEcho(writerLabel ?? '')} for site ${siteId} is not in the allow-list`,
        );
      }
      const state = row.state ?? null;
      if (state === null) {
        throw new TenantConfigError(`lu_tenant_route row for site ${siteId} has no state`);
      }
      if (!allowedStates.has(state)) {
        throw new TenantConfigError(
          `lu_tenant_route.state ${safeStateEcho(state)} for site ${siteId} does not allow the '${operation}' operation (expected ${expectedState})`,
        );
      }
      const envName = envVarName(sanitizeReference(ref));
      const dsn = env[envName];
      if (dsn === undefined || dsn.trim() === '') {
        throw new TenantConfigError(
          `tenant migration secret env var is not set for site ${siteId}`,
        );
      }
      const config = loadConnectionConfigFromEnv({
        [CONNECTION_STRING_ENV_VAR]: dsn,
      });
      // COMMIT the metadata read explicitly; we hold no locks and the
      // BEGIN is a no-op semantically, but the explicit commit makes the
      // contract auditable in the query log.
      await client.query('COMMIT');
      transactionOpened = false;
      await closeOnce();
      return {
        config,
        safeTarget: {
          host: config.host,
          port: config.port,
          database: config.database,
        },
      };
    } catch (error) {
      if (transactionOpened) {
        try {
          await client.query('ROLLBACK');
        } catch {
          // The connection might be unusable; the close path will mask it.
        }
        transactionOpened = false;
      }
      throw error;
    }
  } finally {
    await closeOnce();
  }
}

// ---------------------------------------------------------------------------
// withTenantMigrationLease (MIG-001-F2-W15A) — cross-DB write fence.
// ---------------------------------------------------------------------------

/**
 * Input the lease passes to the caller-supplied callback. The callback
 * receives a fully-parsed tenant migration connection (distinct from the
 * control-plane connection) and is responsible for issuing the actual
 * tenant DDL on that connection. The lease intentionally does NOT touch
 * the tenant DB; it only owns the control-plane transaction.
 *
 * The reference, the env var name, the DSN and the password are NEVER
 * logged by the lease itself. The callback may observe the DSN via
 * `config` because the callback is the only producer of tenant DDL, but
 * the lease does not propagate that material further.
 */
export interface TenantMigrationCallbackInput {
  /** Resolved tenant migration connection (parsed DSN; never the CP one). */
  readonly config: PostgresConnectionConfig;
  /** Log-safe summary of the resolved tenant target. */
  readonly safeTarget: TenantSafeTarget;
  /** Route state at lock time; always 'migrating' under the lease. */
  readonly state: 'migrating';
  /** Allow-listed writer label observed under FOR UPDATE. */
  readonly writerLabel: TenantWriterLabel;
  /**
   * `lu_tenant_route.migration_secret_reference` observed under FOR UPDATE.
   * The lease intentionally does not echo this anywhere; it is provided
   * here only so the callback may log structured provenance if it chooses.
   */
  readonly migrationSecretReference: string;
}

/**
 * Outcome the lease expects from the callback. A `committed=false` return
 * value means the callback did not commit any DDL on the tenant DB; the
 * lease treats that as a callback failure and ROLLBACKs.
 */
export interface TenantMigrationCallbackResult {
  /** True iff the callback committed/replayed the tenant DDL atomically. */
  readonly committed: boolean;
  /**
   * Ordinal actually committed/replayed on the tenant DB. Required when
   * `committed=true`; ignored otherwise. Must be a positive integer
   * aligned with a registered tenant migration pin.
   */
  readonly ordinal: number;
}

export type TenantMigrationCallback = (
  input: TenantMigrationCallbackInput,
) => Promise<TenantMigrationCallbackResult>;

export interface WithTenantMigrationLeaseOptions {
  /** Site UUID (RFC 4122). Validated before any client is opened. */
  readonly siteId: string;
  /** Injected env record; the CLI passes `process.env`; tests pass an object. */
  readonly env: Record<string, string | undefined>;
  /** Control-plane DSN (parsed). The lease opens its own CP client. */
  readonly controlPlaneConfig: PostgresConnectionConfig;
  /**
   * Factory that opens a CP client. The same factory is used by
   * `resolveTenantConfig`; the lease holds the client open for the
   * duration of the callback.
   */
  readonly controlPlaneClientFactory: TenantClientFactory;
  /** Caller-supplied tenant DDL callback. */
  readonly callback: TenantMigrationCallback;
  /**
   * Override for the global advisory-lock key; exported for tests that
   * assert exact constants. Production callers MUST leave this undefined
   * to keep the fleet serialization invariant.
   */
  readonly globalLockKey?: string;
}

export interface WithTenantMigrationLeaseResult {
  /** Schema-version advancement the lease committed on the route row. */
  readonly ordinal: number;
  /** State observed at lock time; always 'migrating' for the up stream. */
  readonly state: 'migrating';
  /** Allow-listed writer label observed under FOR UPDATE. */
  readonly writerLabel: TenantWriterLabel;
}

/**
 * Acquire a lock-serialized, cross-DB fence for a tenant `up` migration.
 *
 * Isolation is READ COMMITTED on purpose. Serialization comes from the
 * global advisory xact lock plus the route row FOR UPDATE, both held until
 * COMMIT. Under REPEATABLE READ the transaction snapshot is fixed by the
 * first preflight SELECT, i.e. BEFORE the advisory lock is granted; a
 * concurrent lease that waited on the lock would then hit SQLSTATE 40001
 * ("could not serialize access due to concurrent update") on its FOR
 * UPDATE of the row the winner just updated. READ COMMITTED takes the
 * FOR UPDATE snapshot after the lock is held, so the loser observes the
 * winner's committed route row and proceeds normally.
 *
 * Wire (verbatim, no reordering):
 *
 *   BEGIN ISOLATION LEVEL READ COMMITTED
 *   SET LOCAL ROLE lu_auth_migrator
 *   SELECT pg_advisory_xact_lock(hashtext(:GLOBAL_KEY))
 *   SELECT migration_secret_reference, writer_label, state, schema_version
 *     FROM public.lu_tenant_route
 *    WHERE site_id = :SITE
 *      FOR UPDATE
 *   -- callback runs, performing tenant DDL on its own connection --
 *   UPDATE public.lu_tenant_route
 *      SET schema_version = GREATEST(schema_version, :ordinal),
 *          last_health_at = CURRENT_TIMESTAMP,
 *          updated_at   = CURRENT_TIMESTAMP
 *    WHERE site_id = :SITE
 *      AND state = 'migrating'
 *      AND writer_label = :WRITER
 *      AND migration_secret_reference = :REF
 *   COMMIT      -- only if the UPDATE affected exactly one row
 *
 * The CLIENT is held open for the WHOLE pipeline so the global advisory
 * lock and the row FOR UPDATE lock survive the callback. CLIENT closes
 * exactly once on every path (try/finally). ROLLBACK fires on any error
 * before COMMIT (callback exception, validation failure, malformed
 * callback result, UPDATE row-count mismatch, COMMIT failure).
 *
 * The lease deliberately does NOT attempt a distributed rollback if the
 * tenant callback committed but the CP UPDATE failed: in that pathological
 * case the tenant DB is independently committed, the route row remains in
 * `migrating`, and the lease raises an explicit `TenantMigrationLeaseIndeterminateError`
 * so an operator can recover manually.
 */
export async function withTenantMigrationLease(
  opts: WithTenantMigrationLeaseOptions,
): Promise<WithTenantMigrationLeaseResult> {
  if (!UUID_RE.test(opts.siteId)) {
    throw new TenantConfigError(`site UUID is not RFC 4122 shaped`);
  }
  const normalizedSite = opts.siteId.toLowerCase();
  const globalLockKey = opts.globalLockKey ?? TENANT_MIGRATION_GLOBAL_LOCK_KEY;
  const client = await opts.controlPlaneClientFactory(opts.controlPlaneConfig);
  let closed = false;
  const closeOnce = async (): Promise<void> => {
    if (closed) return;
    closed = true;
    await client.close().catch(() => undefined);
  };
  try {
    return await runLease(client, normalizedSite, globalLockKey, opts);
  } finally {
    await closeOnce();
  }
}

async function runLease(
  client: TenantClient,
  normalizedSite: string,
  globalLockKey: string,
  opts: WithTenantMigrationLeaseOptions,
): Promise<WithTenantMigrationLeaseResult> {
  let transactionOpened = false;
  let committed = false;
  try {
    // (1) BEGIN — READ COMMITTED so the FOR UPDATE scan snapshots AFTER the
    //     global lock is granted (see the function doc); lock_timeout is
    //     inherited from the client defaults set by the CLI/runner.
    await client.query('BEGIN ISOLATION LEVEL READ COMMITTED');
    transactionOpened = true;
    // (2) Migrator preflight (resolver/lease only). The runner has its own
    //     APPLY-time preflight on the tenant connection; this one runs
    //     against the control-plane connection BEFORE the route lookup so
    //     generic / redacted metadata is rejected without ever reaching
    //     the route query or the tenant callback.
    await assertResolverPreflight(client);
    // (3) SET LOCAL ROLE — the migrator role is the only principal allowed
    //     to read the migration_secret_reference column under the F2/F3
    //     contract. SET LOCAL is transaction-scoped so the role resets on
    //     COMMIT/ROLLBACK without leaking into a connection-pool reuse.
    await client.query('SET LOCAL ROLE lu_auth_migrator');
    // (4) GLOBAL advisory lock — hashtext() runs server-side; only the
    //     constant key string appears in the wire payload. Every site
    //     competing for the same DB serializes here.
    await client.query('SELECT pg_advisory_xact_lock(hashtext($1))', [globalLockKey]);
    // (5) Site row FOR UPDATE — row lock on the specific site_id, in
    //     addition to the global advisory lock. We select schema_version
    //     so we can compute GREATEST(schema_version, ordinal) server-side.
    const r = await client.query<RouteLookupRow & { schema_version: number | null }>(
      `SELECT migration_secret_reference, writer_label, state, schema_version
         FROM public.lu_tenant_route
        WHERE site_id = $1
        FOR UPDATE`,
      [normalizedSite],
    );
    const row = r.rows[0] ?? null;
    if (row === null) {
      throw new TenantMigrationLeaseError(`lu_tenant_route row for site ${opts.siteId} is missing`);
    }
    const ref = row.migration_secret_reference ?? null;
    if (ref === null) {
      throw new TenantMigrationLeaseError(
        `lu_tenant_route row for site ${opts.siteId} has no migration_secret_reference`,
      );
    }
    if (!REF_RE.test(ref)) {
      throw new TenantMigrationLeaseError(
        `lu_tenant_route.migration_secret_reference for site ${opts.siteId} failed format check`,
      );
    }
    const writerLabel = row.writer_label ?? null;
    if (writerLabel === null || !ALLOWED_LABELS.has(writerLabel)) {
      throw new TenantMigrationLeaseError(
        `lu_tenant_route.writer_label ${safeLabelEcho(writerLabel ?? '')} for site ${opts.siteId} is not in the allow-list`,
      );
    }
    const state = row.state ?? null;
    if (state !== 'migrating') {
      throw new TenantMigrationLeaseError(
        `lu_tenant_route.state ${safeStateEcho(state ?? '')} for site ${opts.siteId} does not allow the 'up' operation (expected migrating)`,
      );
    }
    const envName = envVarName(sanitizeReference(ref));
    const dsn = opts.env[envName];
    if (dsn === undefined || dsn.trim() === '') {
      throw new TenantMigrationLeaseError(
        `tenant migration secret env var is not set for site ${opts.siteId}`,
      );
    }
    const config = loadConnectionConfigFromEnv({ [CONNECTION_STRING_ENV_VAR]: dsn });
    const safeTarget: TenantSafeTarget = {
      host: config.host,
      port: config.port,
      database: config.database,
    };

    // (6) Open the callback window. The CP transaction + global lock + row
    //     FOR UPDATE remain held. The tenant callback runs entirely on a
    //     different connection (the parse-config one) and commits to the
    //     tenant DB.
    const callbackResult = await opts.callback({
      config,
      safeTarget,
      state: 'migrating',
      writerLabel: writerLabel as TenantWriterLabel,
      migrationSecretReference: ref,
    });
    if (
      callbackResult === null ||
      typeof callbackResult !== 'object' ||
      typeof (callbackResult as { committed?: unknown }).committed !== 'boolean'
    ) {
      throw new TenantMigrationLeaseError(
        `tenant migration callback returned an invalid result for site ${opts.siteId}`,
      );
    }
    const ordinal = (callbackResult as { ordinal?: unknown }).ordinal;
    if (!(callbackResult as { committed: boolean }).committed) {
      throw new TenantMigrationLeaseError(
        `tenant migration callback did not commit on the tenant DB for site ${opts.siteId} (preexisting / not committed / verification failed); refusing to advance route version`,
      );
    }
    if (typeof ordinal !== 'number' || !Number.isInteger(ordinal) || ordinal < 1) {
      throw new TenantMigrationLeaseError(
        `tenant migration callback reported committed=true with an invalid ordinal for site ${opts.siteId}`,
      );
    }
    const ordinalAsInt = ordinal as number;
    // (6a) Ordinal must be present in the registered tenant stream — a
    //      positive integer alone is not sufficient. The registry is the
    //      source of truth; ordinals outside the [1, TENANT_REGISTRY.length]
    //      range (e.g. 14, 999) are rejected BEFORE the guarded UPDATE so
    //      the route row's schema_version cannot be silently advanced to
    //      a value no migration can ever satisfy.
    if (
      ordinalAsInt < 1 ||
      ordinalAsInt > TENANT_REGISTRY.length ||
      TENANT_REGISTRY[ordinalAsInt - 1]?.stream !== 'tenant'
    ) {
      throw new TenantMigrationLeaseError(
        `tenant migration callback reported committed=true with an unregistered ordinal ${ordinalAsInt} for site ${opts.siteId} (registered tenant ordinals: 1..${TENANT_REGISTRY.length})`,
      );
    }

    // (7) Guarded route UPDATE. The WHERE clause pins site_id, state, the
    //     observed writer_label and the observed migration_secret_reference;
    //     FOR UPDATE already prevents anyone else from mutating the row,
    //     so the predicate is technically redundant — but the contract
    //     also requires the predicate for auditability.
    const u = await client.query<{ site_id: string; schema_version: number }>(
      `UPDATE public.lu_tenant_route
          SET schema_version = GREATEST(schema_version, $2::integer),
              last_health_at = CURRENT_TIMESTAMP,
              updated_at      = CURRENT_TIMESTAMP
        WHERE site_id = $1
          AND state = $3
          AND writer_label = $4
          AND migration_secret_reference = $5
        RETURNING site_id, schema_version`,
      [normalizedSite, ordinalAsInt, 'migrating', writerLabel, ref],
    );
    if (u.rows.length !== 1) {
      // Indeterminate: tenant committed (callback succeeded) but the route
      // row UPDATE affected an unexpected row count. The CP transaction is
      // explicitly NOT rolled back here at the route-update level — we
      // still ROLLBACK the lease transaction so the FOR UPDATE lock is
      // released, but the lease raises an indeterminate error so an
      // operator can resolve the inconsistency. The tenant DB stays
      // committed (cross-DB invariants forbid rolling it back from here).
      try {
        await client.query('ROLLBACK');
      } catch {
        // ignored
      }
      transactionOpened = false;
      throw new TenantMigrationLeaseIndeterminateError(
        `tenant migration for site ${opts.siteId} advanced the tenant DB but the control-plane route row update was indeterminate; the route row remains in migrating and requires operator recovery`,
      );
    }

    // (8) COMMIT — only after a single-row UPDATE.
    await client.query('COMMIT');
    transactionOpened = false;
    committed = true;
    return {
      ordinal: ordinalAsInt,
      state: 'migrating',
      writerLabel: writerLabel as TenantWriterLabel,
    };
  } catch (error) {
    if (transactionOpened && !committed) {
      try {
        await client.query('ROLLBACK');
      } catch {
        // The connection might already be unusable; the caller will see
        // the original error and the close path masks the rollback
        // failure.
      }
    }
    throw error;
  }
}

// Re-export the client interface under its primary name for the public
// API. `TenantClient` remains the historical alias used by tests and the
// CLI runner integration; both refer to the same shape.
export type { TenantClient as TenantControlPlaneClient };
