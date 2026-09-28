/**
 * MIG-001 F3 — static conformance of runtime SQL with the lu_auth_runtime
 * column grants of control-plane 0006. Every INSERT/UPDATE issued by the F3
 * runtime (auth, identity kernel, users) against identity tables must only
 * name granted columns; a violation fails in PostgreSQL with "permission
 * denied" and is invisible to fake-based tests.
 */
import { readdirSync, readFileSync, statSync } from 'node:fs';
import { join } from 'node:path';

const ROOT = new URL('..', import.meta.url);
const MIGRATION = readFileSync(
  new URL('migrations/control-plane/0006_mig001_users_identity.sql', ROOT),
  'utf8',
);

/** Runtime-executed source trees (bootstrap is a privileged opt-in CLI, not runtime). */
const RUNTIME_DIRS = ['src/auth', 'src/identity', 'src/users'];
const EXCLUDED = new Set([
  'auth-bootstrap.ts',
  'auth-bootstrap-cli.ts',
  'auth-managed-role-cli.ts',
]);
const TABLES = ['lu_user', 'lu_site_membership', 'lu_session'] as const;

function granted(kind: 'INSERT' | 'UPDATE', table: string): Set<string> | 'ALL' | null {
  const columnGrant = new RegExp(
    `GRANT ${kind} \\(([^)]+)\\) ON TABLE public\\.${table} TO lu_auth_runtime`,
  ).exec(MIGRATION);
  if (columnGrant !== null) return new Set(columnGrant[1]!.split(',').map((c) => c.trim()));
  if (
    new RegExp(
      `GRANT [A-Z, ]*\\b${kind}\\b[A-Z, ]* ON TABLE public\\.${table} TO lu_auth_runtime`,
    ).test(MIGRATION)
  ) {
    return 'ALL';
  }
  return null;
}

function sourceFiles(dir: string): string[] {
  const out: string[] = [];
  for (const entry of readdirSync(new URL(dir + '/', ROOT))) {
    const relative = join(dir, entry);
    if (statSync(new URL(relative, ROOT)).isDirectory()) out.push(...sourceFiles(relative));
    else if (entry.endsWith('.ts') && !EXCLUDED.has(entry)) out.push(relative);
  }
  return out;
}

interface Statement {
  readonly file: string;
  readonly kind: 'INSERT' | 'UPDATE';
  readonly table: string;
  readonly columns: string[];
}

function statements(): Statement[] {
  const found: Statement[] = [];
  for (const file of RUNTIME_DIRS.flatMap(sourceFiles)) {
    const text = readFileSync(new URL(file, ROOT), 'utf8').replace(/\s+/g, ' ');
    for (const table of TABLES) {
      for (const m of text.matchAll(
        new RegExp(`INSERT INTO public\\.${table} \\(([^)]+)\\)`, 'g'),
      )) {
        found.push({ file, kind: 'INSERT', table, columns: m[1]!.split(',').map((c) => c.trim()) });
      }
      for (const m of text.matchAll(new RegExp(`UPDATE public\\.${table} SET (.+?) WHERE`, 'g'))) {
        const columns = [...m[1]!.matchAll(/(?:^|,)\s*([a-z_]+)\s*=/g)].map((c) => c[1]!);
        found.push({ file, kind: 'UPDATE', table, columns });
      }
    }
  }
  return found;
}

describe('F3 runtime SQL vs lu_auth_runtime grants (control-plane 0006)', () => {
  const all = statements();

  it('finds the runtime identity writes to check', () => {
    expect(
      all.filter((s) => s.table === 'lu_user' && s.kind === 'UPDATE').length,
    ).toBeGreaterThanOrEqual(8);
    expect(all.some((s) => s.table === 'lu_user' && s.kind === 'INSERT')).toBe(true);
    expect(all.some((s) => s.table === 'lu_site_membership' && s.kind === 'INSERT')).toBe(true);
    expect(all.some((s) => s.table === 'lu_session' && s.kind === 'UPDATE')).toBe(true);
  });

  it('only writes granted columns', () => {
    const violations = all.flatMap((s) => {
      const grant = granted(s.kind, s.table);
      if (grant === 'ALL') return [];
      if (grant === null) return [`${s.file}: ${s.kind} on ${s.table} is not granted`];
      return s.columns
        .filter((c) => !grant.has(c))
        .map((c) => `${s.file}: ${s.kind} ${s.table}.${c}`);
    });
    expect(violations).toEqual([]);
  });

  it('never writes trigger-managed or DB-maintained columns', () => {
    const managed = new Set([
      'full_name',
      'row_version',
      'created_at',
      'updated_at',
      'reconciliation_state',
    ]);
    const offenders = all
      .filter((s) => s.table !== 'lu_session')
      .flatMap((s) =>
        s.columns.filter((c) => managed.has(c)).map((c) => `${s.file}: ${s.table}.${c}`),
      );
    expect(offenders).toEqual([]);
  });
});
