/**
 * Unit tests for the offline migration analyzer (no DB, no network).
 *
 * Covers: allowlist of the four CREATE TABLE targets, SHA-256 over the exact bytes, and
 * rejection of destructive/DDL-mutating/data-seeding SQL (DROP / TRUNCATE / DELETE /
 * UPDATE / ALTER / INSERT / GRANT / CREATE ROLE...), including the real 0001 file whose
 * `ON DELETE RESTRICT` referential clauses must NOT be flagged.
 *
 * Naming note: the `.e2e-spec.ts` suffix is required by the existing Jest config and is
 * out of scope here; these specs never touch a database.
 */
import { readFileSync } from 'node:fs';
import { fileURLToPath } from 'node:url';
import {
  IDENTITY_MIGRATION_FILE,
  MINIMUM_COLUMNS,
  MigrationPolicyError,
  TARGET_TABLES,
  analyzeMigrationSql,
  buildMigrationPlan,
  sanitizeSql,
  sha256Hex,
} from '../src/database/migration-plan.js';

const MIGRATION_PATH = fileURLToPath(
  new URL(`../migrations/${IDENTITY_MIGRATION_FILE}`, import.meta.url),
);
const REAL_SQL = readFileSync(MIGRATION_PATH, 'utf8');

function violationsFor(suffixSql: string): string[] {
  return [...analyzeMigrationSql(REAL_SQL + '\n' + suffixSql).violations];
}

describe('migration-plan: the real 0001 file passes the policy', () => {
  it('creates exactly the four allowlisted tables, in order', () => {
    const { tables, violations } = analyzeMigrationSql(REAL_SQL);
    expect(violations).toEqual([]);
    expect(tables).toEqual([...TARGET_TABLES]);
  });

  it('does not flag "ON DELETE RESTRICT" referential clauses as destructive', () => {
    expect(REAL_SQL).toContain('ON DELETE RESTRICT');
    const { violations } = analyzeMigrationSql(REAL_SQL);
    expect(violations.filter((v) => /DELETE/i.test(v))).toEqual([]);
  });

  it("does not flag the 'updated_at' column names as UPDATE statements", () => {
    expect(REAL_SQL).toContain('updated_at');
    expect(analyzeMigrationSql(REAL_SQL).violations.filter((v) => /UPDATE/i.test(v))).toEqual([]);
  });

  it('buildMigrationPlan returns a stable SHA-256 over the exact bytes', () => {
    const buffer = readFileSync(MIGRATION_PATH);
    const plan = buildMigrationPlan(MIGRATION_PATH, buffer);
    expect(plan.file).toBe(IDENTITY_MIGRATION_FILE);
    expect(plan.sha256).toMatch(/^[0-9a-f]{64}$/);
    expect(plan.sha256).toBe(sha256Hex(buffer));
    expect(plan.byteLength).toBe(buffer.length);
    expect(plan.tables).toEqual([...TARGET_TABLES]);
    // Same content twice -> same hash (reproducibility check).
    expect(buildMigrationPlan(MIGRATION_PATH, buffer.toString('utf8')).sha256).toBe(plan.sha256);
  });

  it('declares minimum columns only for the four target tables', () => {
    expect(Object.keys(MINIMUM_COLUMNS).sort()).toEqual([...TARGET_TABLES].sort());
    for (const table of TARGET_TABLES) {
      expect(MINIMUM_COLUMNS[table].length).toBeGreaterThan(3);
    }
  });
});

describe('migration-plan: destructive and out-of-scope SQL is rejected', () => {
  it('flags DROP', () => {
    expect(violationsFor('DROP TABLE lu_session;')).toContain('DROP statement is not allowed');
  });

  it('flags TRUNCATE', () => {
    expect(violationsFor('TRUNCATE TABLE lu_user;')).toContain('TRUNCATE is not allowed');
  });

  it('flags DELETE FROM (beyond referential clauses)', () => {
    expect(
      violationsFor('DELETE FROM lu_site_membership WHERE status = $1;').find((v) =>
        /DELETE statement is not allowed/.test(v),
      ),
    ).toBeDefined();
  });

  it('flags UPDATE statements', () => {
    expect(violationsFor('UPDATE lu_site SET status = $1;')).toContain(
      'UPDATE statement is not allowed',
    );
  });

  it('flags ALTER', () => {
    expect(violationsFor('ALTER TABLE lu_user ADD COLUMN phone text;')).toContain(
      'ALTER is not allowed (the tool never mutates existing objects)',
    );
  });

  it('flags INSERT/GRANT/REVOKE/COPY/CREATE ROLE (no seeding, no principals)', () => {
    expect(violationsFor("INSERT INTO lu_site VALUES ('x');")).toContain(
      'INSERT is not allowed (no seeding)',
    );
    expect(violationsFor('GRANT ALL ON lu_user TO public;')).toContain(
      'GRANT is not allowed (no privilege changes)',
    );
    expect(violationsFor('REVOKE ALL ON lu_user FROM public;')).toContain(
      'REVOKE is not allowed (no privilege changes)',
    );
    expect(violationsFor("COPY lu_site FROM '/tmp/x.csv';")).toContain(
      'COPY is not allowed (no bulk data movement)',
    );
    expect(violationsFor('CREATE ROLE app_login LOGIN;')).toContain(
      'creating roles/users/databases/schemas is not allowed (no principal changes)',
    );
  });

  it('flags tables outside the allowlist and missing allowlisted tables', () => {
    const extra = analyzeMigrationSql(
      REAL_SQL + '\nCREATE TABLE IF NOT EXISTS lu_audit_log (id uuid PRIMARY KEY);\n',
    ).violations;
    expect(extra).toContain('creates table outside the allowlist: lu_audit_log');

    const partial = analyzeMigrationSql(
      'CREATE TABLE IF NOT EXISTS lu_site (id uuid PRIMARY KEY);',
    ).violations;
    for (const missing of ['lu_user', 'lu_site_membership', 'lu_session']) {
      expect(partial).toContain(`allowlisted table is not created: ${missing}`);
    }
  });

  it('flags duplicate CREATE TABLE declarations and non-public schemas', () => {
    const dup = analyzeMigrationSql(
      REAL_SQL + '\nCREATE TABLE IF NOT EXISTS lu_site (id uuid);\n',
    ).violations;
    expect(dup).toContain('table lu_site is declared more than once');

    const schema = analyzeMigrationSql(
      'CREATE TABLE IF NOT EXISTS other.lu_site (id uuid);' +
        'CREATE TABLE IF NOT EXISTS lu_user (id uuid);' +
        'CREATE TABLE IF NOT EXISTS lu_site_membership (user_id uuid, site_id uuid);' +
        'CREATE TABLE IF NOT EXISTS lu_session (id uuid);',
    ).violations;
    expect(schema.some((v) => /non-public schema/.test(v))).toBe(true);
  });

  it('rejects syntax the analyzer cannot fully inspect (dollar-quoted bodies)', () => {
    const violations = violationsFor('DO $$ BEGIN NULL; END $$;');
    expect(violations.some((v) => /dollar-quoted body/.test(v))).toBe(true);
  });

  it('comment/string decoys do not hide real statements nor create false positives', () => {
    // Harmless prose mentioning dangerous keywords must not flag...
    const decoy =
      "CREATE TABLE IF NOT EXISTS lu_site (id uuid, code text DEFAULT 'DROP TABLE x');\n" +
      '-- DELETE FROM lu_user; /* UPDATE lu_site SET x = 1; ALTER TABLE lu_session DROP; */\n' +
      'CREATE TABLE IF NOT EXISTS lu_user (id uuid, email text, full_name text, password_hash text);\n' +
      'CREATE TABLE IF NOT EXISTS lu_site_membership (user_id uuid, site_id uuid);\n' +
      'CREATE TABLE IF NOT EXISTS lu_session (id uuid);\n';
    expect(analyzeMigrationSql(decoy).violations).toEqual([]);
    // ...but a real statement after the decoy comment still flags.
    expect(violationsFor('-- fine comment\nDROP INDEX ux_lu_site_code_lower;')).toContain(
      'DROP statement is not allowed',
    );
  });

  it('buildMigrationPlan throws MigrationPolicyError listing every violation', () => {
    let caught: unknown;
    try {
      buildMigrationPlan(
        '/tmp/0002_evil.sql',
        Buffer.from(REAL_SQL + '\nDROP TABLE lu_user;\n', 'utf8'),
      );
    } catch (error) {
      caught = error;
    }
    expect(caught).toBeInstanceOf(MigrationPolicyError);
    const policyError = caught as MigrationPolicyError;
    expect(policyError.violations).toContain('DROP statement is not allowed');
    expect(policyError.message).toContain('0002_evil.sql');
  });
});

describe('migration-plan: sanitizer basics', () => {
  it('strips comments, string bodies and unwraps quoted identifiers', () => {
    const { code, issues } = sanitizeSql(
      'SELECT \'DELETE FROM x\' , "Lu Site" -- UPDATE\n/* TRUNCATE */ FROM t;',
    );
    expect(issues).toEqual([]);
    expect(code).not.toContain('DELETE FROM x');
    expect(code).not.toContain('UPDATE');
    expect(code).not.toContain('TRUNCATE');
    expect(code).toContain('Lu Site');
  });

  it('reports unterminated literals and comments as issues', () => {
    expect(
      sanitizeSql("SELECT 'oops").issues.some((v) => /unterminated string literal/i.test(v)),
    ).toBe(true);
    expect(
      sanitizeSql('/* forever').issues.some((v) => /unterminated block comment/i.test(v)),
    ).toBe(true);
  });

  it('sha256Hex matches a known digest', () => {
    expect(sha256Hex('abc')).toBe(
      'ba7816bf8f01cfea414140de5dae2223b00361a396177a9cb410ff61f20015ad',
    );
  });
});
