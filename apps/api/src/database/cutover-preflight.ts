/**
 * MIG-001 F8 — cutover preflight guards for the managed-role provisioning step.
 *
 * These checks make the ordering between `auth:managed-role provision`
 * (database/roles/003_provision_auth_runtime_managed.sql) and the control-plane
 * migrations 0001..0006 verifiable, instead of relying on prose. Two failure
 * modes are caught:
 *
 *   1. The migration CLI applies control-plane ordinal 4
 *      (`0004_academic_catalogs.sql`) on a database where roles/003 has not
 *      yet granted `lu_auth_runtime` its baseline table privileges. 0006
 *      fails closed when the runtime role is absent, but skipping 003
 *      entirely would leave the runtime WITHOUT grants (the runtime's
 *      read paths then fail with 42501).
 *
 *   2. `auth:managed-role provision` is invoked AFTER control-plane
 *      ordinal >= 4 has already landed. roles/003 issues a
 *      `REVOKE ALL PRIVILEGES ON TABLE public.lu_site, public.lu_user,
 *       public.lu_site_membership, public.lu_session, public.lu_auth_rate_limit,
 *       public.lu_security_event, public.lu_tenant_route` followed by
 *      `GRANT SELECT ...` only on the 0001-0003 tables. The column-level
 *      grants added by 0004+ on those same tables would be silently
 *      erased.
 *
 * The module is intentionally a pure helper: every check accepts a minimal
 * `query` function so it can be unit-tested with an in-memory fake. The
 * wiring layers (migration-cli, auth-managed-role-cli) own the actual
 * pg.Client lifecycle.
 */
export type PreflightQueryRow = Record<string, unknown>;

export type PreflightQuery = <Row extends PreflightQueryRow>(
  sql: string,
  values?: readonly unknown[],
) => Promise<readonly Row[]>;

export class CutoverPreflightError extends Error {
  constructor(message: string) {
    super(message);
    this.name = 'CutoverPreflightError';
  }
}

/**
 * Exact mirror of the runtime-table ACL postconditions in
 * `database/roles/003_provision_auth_runtime_managed.sql`. The check verifies:
 *   1. role `lu_auth_runtime` exists,
 *   2. SELECT on public.lu_site, public.lu_user, public.lu_site_membership,
 *      public.lu_session and public.lu_tenant_route,
 *   3. SELECT, DELETE on public.lu_auth_rate_limit,
 *   4. NO INSERT/UPDATE/DELETE/TRUNCATE/REFERENCES/TRIGGER/MAINTAIN on
 *      public.lu_tenant_route (negative),
 *   5. NO SELECT/UPDATE/DELETE/TRUNCATE/REFERENCES/TRIGGER/MAINTAIN on
 *      public.lu_security_event (negative).
 *
 * Every assertion matches the postconditions block verbatim; nothing
 * stricter is added.
 */
export interface RuntimeGrantsCheckResult {
  readonly runtimeRoleExists: boolean;
  readonly missingPrivileges: readonly string[];
  readonly extraPrivileges: readonly string[];
}

const RUNTIME_ROLE = 'lu_auth_runtime';

interface RuntimeGrantsRow extends PreflightQueryRow {
  lu_site_select: boolean | null;
  lu_user_select: boolean | null;
  lu_site_membership_select: boolean | null;
  lu_session_select: boolean | null;
  lu_auth_rate_limit_select_delete: boolean | null;
  lu_tenant_route_select: boolean | null;
  lu_tenant_route_disallowed: boolean | null;
  lu_security_event_disallowed: boolean | null;
}

interface RoleCountRow extends PreflightQueryRow {
  count: number | null;
}

const RUNTIME_GRANTS_SQL = `
  SELECT
    pg_catalog.has_table_privilege('${RUNTIME_ROLE}', 'public.lu_site', 'SELECT') AS lu_site_select,
    pg_catalog.has_table_privilege('${RUNTIME_ROLE}', 'public.lu_user', 'SELECT') AS lu_user_select,
    pg_catalog.has_table_privilege('${RUNTIME_ROLE}', 'public.lu_site_membership', 'SELECT') AS lu_site_membership_select,
    pg_catalog.has_table_privilege('${RUNTIME_ROLE}', 'public.lu_session', 'SELECT') AS lu_session_select,
    pg_catalog.has_table_privilege('${RUNTIME_ROLE}', 'public.lu_auth_rate_limit', 'SELECT,DELETE') AS lu_auth_rate_limit_select_delete,
    pg_catalog.has_table_privilege('${RUNTIME_ROLE}', 'public.lu_tenant_route', 'SELECT') AS lu_tenant_route_select,
    pg_catalog.has_table_privilege('${RUNTIME_ROLE}', 'public.lu_tenant_route', 'INSERT,UPDATE,DELETE,TRUNCATE,REFERENCES,TRIGGER,MAINTAIN') AS lu_tenant_route_disallowed,
    pg_catalog.has_table_privilege('${RUNTIME_ROLE}', 'public.lu_security_event', 'SELECT,UPDATE,DELETE,TRUNCATE,REFERENCES,TRIGGER,MAINTAIN') AS lu_security_event_disallowed
`;

export async function readRuntimeGrants(query: PreflightQuery): Promise<RuntimeGrantsCheckResult> {
  const roleRows = await query<RoleCountRow>(
    `SELECT count(*)::int AS count FROM pg_catalog.pg_roles WHERE rolname = $1`,
    [RUNTIME_ROLE],
  );
  const runtimeRoleExists = (roleRows[0]?.count ?? 0) >= 1;
  if (!runtimeRoleExists) {
    return {
      runtimeRoleExists: false,
      missingPrivileges: [`role "${RUNTIME_ROLE}" is missing from pg_catalog.pg_roles`],
      extraPrivileges: [],
    };
  }

  const rows = await query<RuntimeGrantsRow>(RUNTIME_GRANTS_SQL);
  const row = rows[0];
  if (row === undefined) {
    throw new CutoverPreflightError('cutover preflight: runtime grants query returned no row.');
  }

  const missingPrivileges: string[] = [];
  if (row.lu_site_select !== true) {
    missingPrivileges.push('SELECT on public.lu_site');
  }
  if (row.lu_user_select !== true) {
    missingPrivileges.push('SELECT on public.lu_user');
  }
  if (row.lu_site_membership_select !== true) {
    missingPrivileges.push('SELECT on public.lu_site_membership');
  }
  if (row.lu_session_select !== true) {
    missingPrivileges.push('SELECT on public.lu_session');
  }
  if (row.lu_auth_rate_limit_select_delete !== true) {
    missingPrivileges.push('SELECT,DELETE on public.lu_auth_rate_limit');
  }
  if (row.lu_tenant_route_select !== true) {
    missingPrivileges.push('SELECT on public.lu_tenant_route');
  }

  const extraPrivileges: string[] = [];
  if (row.lu_tenant_route_disallowed === true) {
    extraPrivileges.push(
      'INSERT,UPDATE,DELETE,TRUNCATE,REFERENCES,TRIGGER,MAINTAIN on public.lu_tenant_route',
    );
  }
  if (row.lu_security_event_disallowed === true) {
    extraPrivileges.push(
      'SELECT,UPDATE,DELETE,TRUNCATE,REFERENCES,TRIGGER,MAINTAIN on public.lu_security_event',
    );
  }

  return {
    runtimeRoleExists: true,
    missingPrivileges,
    extraPrivileges,
  };
}

export interface ControlPlaneLedgerRow {
  readonly stream: string;
  readonly ordinal: number | null;
}

/**
 * Throws `CutoverPreflightError` when `lu_auth_runtime` lacks the
 * baseline grants from roles/003, refuses any DDL/ledger write by the
 * caller (migration-cli exits non-zero before reaching the runner).
 */
export async function assertRuntimeGrantsBeforeControlPlaneFour(
  query: PreflightQuery,
): Promise<void> {
  const result = await readRuntimeGrants(query);
  if (!result.runtimeRoleExists) {
    throw new CutoverPreflightError(
      `cutover preflight failed: ${result.missingPrivileges[0]}. ` +
        `Run \`auth:managed-role provision\` (database/roles/003_provision_auth_runtime_managed.sql) ` +
        'after control-plane migration 0003 and before 0004 so that lu_auth_runtime ' +
        'exists with its baseline grants.',
    );
  }
  if (result.missingPrivileges.length > 0 || result.extraPrivileges.length > 0) {
    const detail = [
      ...result.missingPrivileges.map((priv) => `missing ${priv}`),
      ...result.extraPrivileges.map((priv) => `unexpected ${priv}`),
    ];
    throw new CutoverPreflightError(
      `cutover preflight failed: roles/003 runtime postconditions are not met ` +
        `(${detail.join('; ')}). ` +
        `Run \`auth:managed-role provision\` (database/roles/003_provision_auth_runtime_managed.sql) ` +
        'after control-plane migration 0003 and before 0004 to reconcile the runtime grants.',
    );
  }
}

interface HistoryTableExistsRow extends PreflightQueryRow {
  exists: boolean | null;
}

/**
 * True when control-plane 0004 is already applied. The ledger
 * (`public.lu_migration_history`) is created by control-plane 0006, so on a
 * pre-0006 database it is absent and 0004 is detected by its first table
 * (`public.lu_faculty`). Query errors propagate (callers fail closed).
 */
export async function isControlPlaneFourApplied(query: PreflightQuery): Promise<boolean> {
  const ledger = await query<HistoryTableExistsRow>(
    `SELECT EXISTS (
       SELECT 1 FROM information_schema.tables
        WHERE table_schema = $1 AND table_name = 'lu_migration_history'
     ) AS exists`,
    ['public'],
  );
  if (ledger[0]?.exists === true) {
    const recorded = await query<HistoryTableExistsRow>(
      `SELECT EXISTS (
         SELECT 1 FROM public.lu_migration_history
          WHERE stream = $1 AND ordinal = 4
       ) AS exists`,
      ['control-plane'],
    );
    return recorded[0]?.exists === true;
  }
  const artifact = await query<HistoryTableExistsRow>(
    `SELECT pg_catalog.to_regclass('public.lu_faculty') IS NOT NULL AS exists`,
    [],
  );
  return artifact[0]?.exists === true;
}

interface MaxOrdinalRow extends PreflightQueryRow {
  max_ordinal: number | null;
}

/**
 * Read-only check intended to be invoked from
 * `auth:managed-role-cli.provision`. Refuses when any control-plane
 * ordinal >= 4 has already been recorded in `lu_migration_history`,
 * because roles/003 issues a blanket `REVOKE ALL` on the 0001-0003
 * tables that would erase the column-level grants added by 0004+.
 *
 * The ledger read uses an information_schema probe first (so a fresh,
 * pre-0006 control DB does not produce a 42P01 error) and the max
 * ordinal is queried through `COALESCE(MAX(ordinal), 0)` so the empty
 * ledger case resolves to 0.
 *
 * Any error raised while reading the ledger is treated as "unreadable"
 * and the check fails closed: a fresh process should never take the
 * risk of running 003 without knowing what migrations are already on
 * the target DB.
 */
export async function assertProvisionAllowedBeforeControlPlaneFour(
  query: PreflightQuery,
): Promise<void> {
  let historyExists: boolean;
  try {
    const probeRows = await query<HistoryTableExistsRow>(
      `SELECT EXISTS (
         SELECT 1 FROM information_schema.tables
          WHERE table_schema = $1 AND table_name = 'lu_migration_history'
       ) AS exists`,
      ['public'],
    );
    historyExists = probeRows[0]?.exists === true;
  } catch {
    throw new CutoverPreflightError(
      'cutover preflight failed: the control-plane migration ledger ' +
        '(public.lu_migration_history) could not be probed. ' +
        'Refusing to run `auth:managed-role provision` (roles/003) because ' +
        'its REVOKE ALL on lu_site, lu_user, lu_site_membership, lu_session, ' +
        'lu_auth_rate_limit, lu_security_event and lu_tenant_route would ' +
        'silently erase any grants added by control-plane 0004+. ' +
        'Reconnect with a session that can read information_schema.tables ' +
        'and re-run.',
    );
  }

  if (!historyExists) {
    // The ledger is created by control-plane 0006, so its absence does not
    // prove the database is below 0004: detect 0004 by its own first table.
    let hasFourArtifacts: boolean;
    try {
      const rows = await query<HistoryTableExistsRow>(
        `SELECT pg_catalog.to_regclass('public.lu_faculty') IS NOT NULL AS exists`,
        [],
      );
      hasFourArtifacts = rows[0]?.exists !== false;
    } catch {
      hasFourArtifacts = true;
    }
    if (hasFourArtifacts) {
      throw new CutoverPreflightError(
        'cutover preflight failed: control-plane 0004 objects (public.lu_faculty) ' +
          'exist although no migration ledger is present (or they could not be probed). ' +
          'Refusing to run `auth:managed-role provision` (roles/003): its REVOKE ALL on ' +
          'the 0001-0003 tables would silently erase the grants added by 0004+.',
      );
    }
    return;
  }

  let maxOrdinal: number;
  try {
    const maxRows = await query<MaxOrdinalRow>(
      `SELECT COALESCE(MAX(ordinal), 0)::int AS max_ordinal
         FROM public.lu_migration_history
        WHERE stream = $1`,
      ['control-plane'],
    );
    maxOrdinal = Number(maxRows[0]?.max_ordinal ?? 0);
  } catch {
    throw new CutoverPreflightError(
      'cutover preflight failed: the control-plane migration ledger ' +
        '(public.lu_migration_history) was found but could not be read. ' +
        'Refusing to run `auth:managed-role provision` (roles/003) because ' +
        'its REVOKE ALL on the 0001-0003 tables would silently erase the ' +
        'grants added by control-plane 0004+. Reconnect with a session that ' +
        'can SELECT from public.lu_migration_history and re-run.',
    );
  }

  if (maxOrdinal >= 4) {
    throw new CutoverPreflightError(
      `cutover preflight failed: control-plane migration ordinal ${maxOrdinal} ` +
        'is already recorded in public.lu_migration_history. ' +
        'Running `auth:managed-role provision` (database/roles/003_provision_auth_runtime_managed.sql) ' +
        'now would REVOKE ALL on lu_site, lu_user, lu_site_membership, lu_session, ' +
        'lu_auth_rate_limit, lu_security_event and lu_tenant_route and silently ' +
        'erase the column-level grants added by control-plane 0004+. ' +
        'Refusing to provision: roles/003 is valid only after control-plane 0003 ' +
        'and before 0004. Use `auth:managed-role verify` to check an existing control plane.',
    );
  }
}
