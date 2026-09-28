/**
 * MIG-001 F8 — cutover-preflight unit specs.
 *
 * Every check in the preflight is exercised with FAKE query functions; no
 * pg.Client or real database is opened. The wiring layers (migration-cli,
 * auth-managed-role-cli) own the actual client lifecycle; this module
 * must remain a pure helper so the harness can swap in an in-memory
 * recorder.
 */
import {
  assertProvisionAllowedBeforeControlPlaneFour,
  assertRuntimeGrantsBeforeControlPlaneFour,
  CutoverPreflightError,
  readRuntimeGrants,
  type PreflightQuery,
  type PreflightQueryRow,
} from '../src/database/cutover-preflight.js';

interface FakeQueryCall {
  readonly sql: string;
  readonly values?: readonly unknown[];
}

function makeFakeQuery(
  rowsByPattern: readonly {
    readonly pattern: RegExp;
    readonly rows: readonly PreflightQueryRow[];
  }[],
  capture: { calls: FakeQueryCall[] },
): PreflightQuery {
  return async <Row extends PreflightQueryRow>(
    sql: string,
    values?: readonly unknown[],
  ): Promise<readonly Row[]> => {
    capture.calls.push(values === undefined ? { sql } : { sql, values });
    for (const entry of rowsByPattern) {
      if (entry.pattern.test(sql)) {
        return entry.rows as readonly Row[];
      }
    }
    return [];
  };
}

function capture(): { calls: FakeQueryCall[] } {
  return { calls: [] };
}

const RUNTIME_COUNT_RE = /FROM pg_catalog\.pg_roles WHERE rolname = \$1/;
const RUNTIME_GRANTS_RE = /pg_catalog\.has_table_privilege\('lu_auth_runtime'/;
const HISTORY_PROBE_RE =
  /FROM information_schema\.tables\s+WHERE table_schema = \$1 AND table_name = 'lu_migration_history'/;
const HISTORY_MAX_ORDINAL_RE = /FROM public\.lu_migration_history\s+WHERE stream = \$1/;
const FACULTY_PROBE_RE = /to_regclass\('public\.lu_faculty'\)/;

const positiveRuntimeGrants: readonly PreflightQueryRow[] = [
  {
    lu_site_select: true,
    lu_user_select: true,
    lu_site_membership_select: true,
    lu_session_select: true,
    lu_auth_rate_limit_select_delete: true,
    lu_tenant_route_select: true,
    lu_tenant_route_disallowed: false,
    lu_security_event_disallowed: false,
  },
];

describe('cutover-preflight: readRuntimeGrants', () => {
  it('reports role missing when pg_roles has no row for lu_auth_runtime', async () => {
    const calls = capture();
    const query = makeFakeQuery([{ pattern: RUNTIME_COUNT_RE, rows: [{ count: 0 }] }], calls);
    const result = await readRuntimeGrants(query);
    expect(result.runtimeRoleExists).toBe(false);
    expect(result.missingPrivileges).toEqual([
      'role "lu_auth_runtime" is missing from pg_catalog.pg_roles',
    ]);
    expect(result.extraPrivileges).toEqual([]);
    expect(calls.calls).toHaveLength(1);
  });

  it('returns no missing or extra privileges when every postcondition passes', async () => {
    const calls = capture();
    const query = makeFakeQuery(
      [
        { pattern: RUNTIME_COUNT_RE, rows: [{ count: 1 }] },
        { pattern: RUNTIME_GRANTS_RE, rows: positiveRuntimeGrants },
      ],
      calls,
    );
    const result = await readRuntimeGrants(query);
    expect(result.runtimeRoleExists).toBe(true);
    expect(result.missingPrivileges).toEqual([]);
    expect(result.extraPrivileges).toEqual([]);
    expect(calls.calls).toHaveLength(2);
  });

  it.each([
    [
      'lu_site_select',
      { ...positiveRuntimeGrants[0], lu_site_select: false },
      'SELECT on public.lu_site',
    ],
    [
      'lu_user_select',
      { ...positiveRuntimeGrants[0], lu_user_select: false },
      'SELECT on public.lu_user',
    ],
    [
      'lu_site_membership_select',
      { ...positiveRuntimeGrants[0], lu_site_membership_select: false },
      'SELECT on public.lu_site_membership',
    ],
    [
      'lu_session_select',
      { ...positiveRuntimeGrants[0], lu_session_select: false },
      'SELECT on public.lu_session',
    ],
    [
      'lu_auth_rate_limit_select_delete',
      { ...positiveRuntimeGrants[0], lu_auth_rate_limit_select_delete: false },
      'SELECT,DELETE on public.lu_auth_rate_limit',
    ],
    [
      'lu_tenant_route_select',
      { ...positiveRuntimeGrants[0], lu_tenant_route_select: false },
      'SELECT on public.lu_tenant_route',
    ],
  ])('names the missing privilege when %s is false', async (_label, row, expectedFragment) => {
    const calls = capture();
    const query = makeFakeQuery(
      [
        { pattern: RUNTIME_COUNT_RE, rows: [{ count: 1 }] },
        { pattern: RUNTIME_GRANTS_RE, rows: [row] },
      ],
      calls,
    );
    const result = await readRuntimeGrants(query);
    expect(result.missingPrivileges).toEqual([expectedFragment]);
  });

  it('reports a disallowed privilege on lu_tenant_route as an extra grant', async () => {
    const calls = capture();
    const row = { ...positiveRuntimeGrants[0], lu_tenant_route_disallowed: true };
    const query = makeFakeQuery(
      [
        { pattern: RUNTIME_COUNT_RE, rows: [{ count: 1 }] },
        { pattern: RUNTIME_GRANTS_RE, rows: [row] },
      ],
      calls,
    );
    const result = await readRuntimeGrants(query);
    expect(result.extraPrivileges).toEqual([
      'INSERT,UPDATE,DELETE,TRUNCATE,REFERENCES,TRIGGER,MAINTAIN on public.lu_tenant_route',
    ]);
  });

  it('reports a disallowed privilege on lu_security_event as an extra grant', async () => {
    const calls = capture();
    const row = { ...positiveRuntimeGrants[0], lu_security_event_disallowed: true };
    const query = makeFakeQuery(
      [
        { pattern: RUNTIME_COUNT_RE, rows: [{ count: 1 }] },
        { pattern: RUNTIME_GRANTS_RE, rows: [row] },
      ],
      calls,
    );
    const result = await readRuntimeGrants(query);
    expect(result.extraPrivileges).toEqual([
      'SELECT,UPDATE,DELETE,TRUNCATE,REFERENCES,TRIGGER,MAINTAIN on public.lu_security_event',
    ]);
  });
});

describe('cutover-preflight: assertRuntimeGrantsBeforeControlPlaneFour', () => {
  it('allows when every postcondition is met', async () => {
    const calls = capture();
    const query = makeFakeQuery(
      [
        { pattern: RUNTIME_COUNT_RE, rows: [{ count: 1 }] },
        { pattern: RUNTIME_GRANTS_RE, rows: positiveRuntimeGrants },
      ],
      calls,
    );
    await expect(assertRuntimeGrantsBeforeControlPlaneFour(query)).resolves.toBeUndefined();
    expect(calls.calls).toHaveLength(2);
  });

  it('refuses when lu_auth_runtime does not exist and names the role', async () => {
    const calls = capture();
    const query = makeFakeQuery([{ pattern: RUNTIME_COUNT_RE, rows: [{ count: 0 }] }], calls);
    await expect(assertRuntimeGrantsBeforeControlPlaneFour(query)).rejects.toBeInstanceOf(
      CutoverPreflightError,
    );
    await expect(assertRuntimeGrantsBeforeControlPlaneFour(query)).rejects.toThrow(
      /role "lu_auth_runtime" is missing/,
    );
    await expect(assertRuntimeGrantsBeforeControlPlaneFour(query)).rejects.toThrow(
      /auth:managed-role provision/,
    );
  });

  it.each([
    ['lu_site', 'SELECT on public.lu_site'],
    ['lu_user', 'SELECT on public.lu_user'],
    ['lu_site_membership', 'SELECT on public.lu_site_membership'],
    ['lu_session', 'SELECT on public.lu_session'],
    ['lu_auth_rate_limit', 'SELECT,DELETE on public.lu_auth_rate_limit'],
    ['lu_tenant_route', 'SELECT on public.lu_tenant_route'],
  ])('refuses naming the missing privilege when %s lacks its grant', async (label, fragment) => {
    const calls = capture();
    const row = { ...positiveRuntimeGrants[0] };
    if (label === 'lu_site') row.lu_site_select = false;
    if (label === 'lu_user') row.lu_user_select = false;
    if (label === 'lu_site_membership') row.lu_site_membership_select = false;
    if (label === 'lu_session') row.lu_session_select = false;
    if (label === 'lu_auth_rate_limit') row.lu_auth_rate_limit_select_delete = false;
    if (label === 'lu_tenant_route') row.lu_tenant_route_select = false;
    const query = makeFakeQuery(
      [
        { pattern: RUNTIME_COUNT_RE, rows: [{ count: 1 }] },
        { pattern: RUNTIME_GRANTS_RE, rows: [row] },
      ],
      calls,
    );
    await expect(assertRuntimeGrantsBeforeControlPlaneFour(query)).rejects.toThrow(
      new RegExp(fragment.replace(/[.*+?^${}()|[\]\\]/g, '\\$&')),
    );
  });
});

describe('cutover-preflight: assertProvisionAllowedBeforeControlPlaneFour', () => {
  it('allows when neither the ledger nor any 0004 object exists (fresh control plane)', async () => {
    const calls = capture();
    const query = makeFakeQuery(
      [
        { pattern: HISTORY_PROBE_RE, rows: [{ exists: false }] },
        { pattern: FACULTY_PROBE_RE, rows: [{ exists: false }] },
      ],
      calls,
    );
    await expect(assertProvisionAllowedBeforeControlPlaneFour(query)).resolves.toBeUndefined();
    expect(calls.calls).toHaveLength(2);
  });

  // The ledger only appears with control-plane 0006: a database at 0004/0005
  // has no ledger, so its absence must not be read as "below 0004".
  it('refuses when the ledger is absent but control-plane 0004 objects exist', async () => {
    const query = makeFakeQuery(
      [
        { pattern: HISTORY_PROBE_RE, rows: [{ exists: false }] },
        { pattern: FACULTY_PROBE_RE, rows: [{ exists: true }] },
      ],
      capture(),
    );
    await expect(assertProvisionAllowedBeforeControlPlaneFour(query)).rejects.toThrow(
      /control-plane 0004 objects/,
    );
  });

  it('refuses (fail closed) when the ledger is absent and the 0004 probe fails', async () => {
    const query: PreflightQuery = async <Row extends PreflightQueryRow>(sql: string) => {
      if (HISTORY_PROBE_RE.test(sql)) return [{ exists: false }] as unknown as readonly Row[];
      throw new Error('permission denied');
    };
    await expect(assertProvisionAllowedBeforeControlPlaneFour(query)).rejects.toThrow(
      /control-plane 0004 objects/,
    );
  });

  it('allows when the ledger exists but no control-plane ordinal is recorded', async () => {
    const calls = capture();
    const query = makeFakeQuery(
      [
        { pattern: HISTORY_PROBE_RE, rows: [{ exists: true }] },
        { pattern: HISTORY_MAX_ORDINAL_RE, rows: [{ max_ordinal: 0 }] },
      ],
      calls,
    );
    await expect(assertProvisionAllowedBeforeControlPlaneFour(query)).resolves.toBeUndefined();
    expect(calls.calls).toHaveLength(2);
  });

  it('allows when the ledger exists and only ordinal 3 is recorded', async () => {
    const calls = capture();
    const query = makeFakeQuery(
      [
        { pattern: HISTORY_PROBE_RE, rows: [{ exists: true }] },
        { pattern: HISTORY_MAX_ORDINAL_RE, rows: [{ max_ordinal: 3 }] },
      ],
      calls,
    );
    await expect(assertProvisionAllowedBeforeControlPlaneFour(query)).resolves.toBeUndefined();
    expect(calls.calls).toHaveLength(2);
  });

  it('refuses when the ledger exists and ordinal 4 has been recorded', async () => {
    const calls = capture();
    const query = makeFakeQuery(
      [
        { pattern: HISTORY_PROBE_RE, rows: [{ exists: true }] },
        { pattern: HISTORY_MAX_ORDINAL_RE, rows: [{ max_ordinal: 4 }] },
      ],
      calls,
    );
    await expect(assertProvisionAllowedBeforeControlPlaneFour(query)).rejects.toBeInstanceOf(
      CutoverPreflightError,
    );
    await expect(assertProvisionAllowedBeforeControlPlaneFour(query)).rejects.toThrow(/ordinal 4/);
    await expect(assertProvisionAllowedBeforeControlPlaneFour(query)).rejects.toThrow(/REVOKE ALL/);
  });

  it('refuses when the ledger exists and a later ordinal (e.g. 6) has been recorded', async () => {
    const calls = capture();
    const query = makeFakeQuery(
      [
        { pattern: HISTORY_PROBE_RE, rows: [{ exists: true }] },
        { pattern: HISTORY_MAX_ORDINAL_RE, rows: [{ max_ordinal: 6 }] },
      ],
      calls,
    );
    await expect(assertProvisionAllowedBeforeControlPlaneFour(query)).rejects.toThrow(/ordinal 6/);
  });

  it('refuses (fail closed) when the information_schema probe throws', async () => {
    const calls = capture();
    const query = (async <Row extends PreflightQueryRow>(
      sql: string,
      values?: readonly unknown[],
    ): Promise<readonly Row[]> => {
      calls.calls.push(values === undefined ? { sql } : { sql, values });
      if (HISTORY_PROBE_RE.test(sql)) {
        throw new Error('permission denied');
      }
      return [];
    }) as PreflightQuery;
    await expect(assertProvisionAllowedBeforeControlPlaneFour(query)).rejects.toBeInstanceOf(
      CutoverPreflightError,
    );
    await expect(assertProvisionAllowedBeforeControlPlaneFour(query)).rejects.toThrow(
      /could not be probed/,
    );
  });

  it('refuses (fail closed) when the MAX(ordinal) query throws after the probe succeeded', async () => {
    const calls = capture();
    const query = (async <Row extends PreflightQueryRow>(
      sql: string,
      values?: readonly unknown[],
    ): Promise<readonly Row[]> => {
      calls.calls.push(values === undefined ? { sql } : { sql, values });
      if (HISTORY_PROBE_RE.test(sql)) {
        return [{ exists: true } as unknown as Row] as readonly Row[];
      }
      if (HISTORY_MAX_ORDINAL_RE.test(sql)) {
        throw new Error('relation "public.lu_migration_history" does not exist');
      }
      return [];
    }) as PreflightQuery;
    await expect(assertProvisionAllowedBeforeControlPlaneFour(query)).rejects.toBeInstanceOf(
      CutoverPreflightError,
    );
    await expect(assertProvisionAllowedBeforeControlPlaneFour(query)).rejects.toThrow(
      /was found but could not be read/,
    );
  });

  it('does not invoke the max-ordinal query when the ledger probe reports absent', async () => {
    const calls = capture();
    const query = makeFakeQuery(
      [
        { pattern: HISTORY_PROBE_RE, rows: [{ exists: false }] },
        { pattern: FACULTY_PROBE_RE, rows: [{ exists: false }] },
      ],
      calls,
    );
    await assertProvisionAllowedBeforeControlPlaneFour(query);
    expect(calls.calls.some((c) => HISTORY_MAX_ORDINAL_RE.test(c.sql))).toBe(false);
  });
});
