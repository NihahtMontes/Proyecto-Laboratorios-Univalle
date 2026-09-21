/**
 * MIG-F3-PG-TEST-003 — adversarial bypass suite for the offline safety analyzer
 * (no DB, no env, no sockets).
 *
 * Every vector below attempts to smuggle an operation the policy forbids past
 * analyzeMigrationSql/buildMigrationPlan. Expected outcome for ALL of them: REJECTED.
 * Positive controls: the real 0001 file and its `ON DELETE RESTRICT` referential clauses
 * must keep passing.
 *
 * Injection points used:
 *  - `inject(...)`   adds the payload as the LAST statement before the closing COMMIT,
 *                    so a rejection can never be credited to a broken envelope.
 *  - `append(...)`   adds the payload after the whole file (classic stray-statement shape).
 */
import { readFileSync } from 'node:fs';
import { fileURLToPath } from 'node:url';
import {
  IDENTITY_MIGRATION_FILE,
  analyzeMigrationSql,
  buildMigrationPlan,
} from '../src/database/migration-plan.js';

const MIGRATION_PATH = fileURLToPath(
  new URL(`../migrations/${IDENTITY_MIGRATION_FILE}`, import.meta.url),
);
const REAL_BYTES = readFileSync(MIGRATION_PATH);
const REAL_SQL = REAL_BYTES.toString('utf8');

function inject(payload: string): string {
  return REAL_SQL.replace(/COMMIT;\s*$/, `${payload}\nCOMMIT;\n`);
}

function append(payload: string): string {
  return `${REAL_SQL}\n${payload}\n`;
}

/** Returns the violations; fails the test outright when the analyzer accepted the payload. */
function expectRejected(sql: string, label: RegExp): readonly string[] {
  const { violations } = analyzeMigrationSql(sql);
  expect(violations.length).toBeGreaterThan(0);
  expect(violations.some((v) => label.test(v))).toBe(true);
  return violations;
}

describe('positive controls (must NOT be flagged)', () => {
  it('the real 0001 file passes with zero violations', () => {
    const { tables, violations } = analyzeMigrationSql(REAL_SQL);
    expect(violations).toEqual([]);
    expect(tables).toEqual(['lu_site', 'lu_user', 'lu_site_membership', 'lu_session']);
    expect(() => buildMigrationPlan(MIGRATION_PATH, REAL_BYTES)).not.toThrow();
  });

  it('ON DELETE RESTRICT referential clauses are tolerated (four occurrences)', () => {
    const occurrences = (REAL_SQL.match(/\bON DELETE RESTRICT\b/gi) ?? []).length;
    expect(occurrences).toBe(4);
    expect(analyzeMigrationSql(REAL_SQL).violations.filter((v) => /DELETE/i.test(v))).toEqual([]);
  });

  it('commented prose mentioning dangerous keywords stays inert', () => {
    expect(analyzeMigrationSql(inject('-- DELETE FROM lu_user; DROP x;')).violations).toEqual([]);
  });
});

describe('bypass vectors: extra objects are rejected (injection before COMMIT)', () => {
  it('CREATE TEMPORARY / TEMP / GLOBAL TEMPORARY / UNLOGGED TABLE', () => {
    for (const payload of [
      'CREATE TEMPORARY TABLE t_x (id uuid);',
      'CREATE TEMP TABLE t_x (id uuid);',
      'CREATE GLOBAL TEMPORARY TABLE t_x (id uuid);',
      'CREATE LOCAL TEMP TABLE t_x (id uuid);',
      'CREATE UNLOGGED TABLE t_x (id uuid);',
    ]) {
      expectRejected(inject(payload), /CREATE TEMP\/UNLOGGED table is not allowed/);
    }
  });

  it('CREATE VIEW and CREATE MATERIALIZED VIEW (plain and OR REPLACE)', () => {
    for (const payload of [
      'CREATE VIEW v_x AS SELECT 1;',
      'CREATE OR REPLACE VIEW v_x AS SELECT 1;',
      'CREATE MATERIALIZED VIEW mv_x AS SELECT 1;',
    ]) {
      expectRejected(inject(payload), /CREATE VIEW \/ MATERIALIZED VIEW is not allowed/);
    }
  });

  it('CREATE SEQUENCE (plain, TEMPORARY and UNLOGGED forms)', () => {
    for (const payload of [
      'CREATE SEQUENCE s_x;',
      'CREATE TEMPORARY SEQUENCE s_x;',
      'CREATE UNLOGGED SEQUENCE s_x;',
    ]) {
      expectRejected(inject(payload), /CREATE SEQUENCE is not allowed/);
    }
  });

  it('CREATE FUNCTION and CREATE OR REPLACE FUNCTION and PROCEDURE', () => {
    for (const payload of [
      "CREATE FUNCTION f_x() RETURNS int AS 'SELECT 1' LANGUAGE sql;",
      "CREATE OR REPLACE FUNCTION f_x() RETURNS int AS 'SELECT 1' LANGUAGE sql;",
      'CREATE PROCEDURE p_x() LANGUAGE sql AS %$ BEGIN END %$;',
    ]) {
      expectRejected(inject(payload), /CREATE FUNCTION \/ PROCEDURE is not allowed/);
    }
  });

  it('CREATE TYPE', () => {
    expectRejected(inject("CREATE TYPE ty_x AS ENUM ('a', 'b');"), /CREATE TYPE is not allowed/);
  });

  it('CREATE TRIGGER (plain and OR REPLACE)', () => {
    for (const payload of [
      'CREATE TRIGGER tg_x BEFORE INSERT ON lu_site EXECUTE PROCEDURE nop();',
      'CREATE OR REPLACE TRIGGER tg_x BEFORE INSERT ON lu_site EXECUTE PROCEDURE nop();',
    ]) {
      expectRejected(inject(payload), /CREATE TRIGGER is not allowed/);
    }
  });

  it('CREATE RULE', () => {
    expectRejected(
      inject('CREATE RULE r_x AS ON INSERT TO lu_site DO NOTHING;'),
      /CREATE RULE is not allowed/,
    );
  });

  it('COMMENT ON (table, column, constraint, index)', () => {
    for (const payload of [
      "COMMENT ON TABLE lu_site IS 'audit note';",
      "COMMENT ON COLUMN lu_user.email IS 'unique email';",
      "COMMENT ON CONSTRAINT fk_lu_session_user ON lu_session IS 'fk';",
      "COMMENT ON INDEX ux_lu_site_code_lower IS 'unique code';",
    ]) {
      expectRejected(inject(payload), /COMMENT is not allowed/);
    }
  });

  it('SET ROLE / SESSION AUTHORIZATION (privilege escalation)', () => {
    for (const payload of [
      'SET ROLE postgres;',
      'SET SESSION AUTHORIZATION postgres;',
      'SET ROLE NONE;',
    ]) {
      expectRejected(inject(payload), /SET ROLE \/ SESSION AUTHORIZATION is not allowed/);
    }
  });

  it('CREATE INDEX on a non-allowlisted table is rejected', () => {
    expectRejected(
      inject('CREATE INDEX IF NOT EXISTS ix_evil ON lu_audit_log (id);'),
      /CREATE INDEX on non-allowlisted table/,
    );
    expectRejected(
      inject('CREATE UNIQUE INDEX ux_evil ON other_schema.lu_site (id);'),
      /CREATE INDEX uses a non-public schema/,
    );
  });

  it('quoted-Unicode table name smuggled inside the envelope is rejected', () => {
    // After identifier unquoting the analyzer sees "CREATE TABLE IF NOT EXISTS ünïcode_tbl ...".
    // Its strict parser captures "if" as the table name, so the object is flagged as outside
    // the allowlist. The registry pin is the ultimate gate for any byte-level tampering.
    expectRejected(
      inject('CREATE TABLE IF NOT EXISTS "\\u00fcn\\u00efcode_tbl" (id uuid PRIMARY KEY);'),
      /creates table outside the allowlist: if/,
    );
  });
});

describe('bypass vectors: broken transaction envelopes are rejected', () => {
  it('missing BEGIN', () => {
    const noBegin = REAL_SQL.replace(/^\s*BEGIN;\s*/i, '');
    const { violations } = analyzeMigrationSql(noBegin);
    expect(violations.some((v) => /transaction envelope must start with BEGIN/.test(v))).toBe(true);
  });

  it('missing COMMIT', () => {
    const noCommit = REAL_SQL.replace(/COMMIT;\s*$/, '');
    const { violations } = analyzeMigrationSql(noCommit);
    expect(violations.some((v) => /transaction envelope must end with COMMIT/.test(v))).toBe(true);
  });

  it('extra COMMIT before the real one', () => {
    const doubleCommit = REAL_SQL.replace(/COMMIT;\s*$/, 'COMMIT;\nCOMMIT;\n');
    const { violations } = analyzeMigrationSql(doubleCommit);
    expect(
      violations.some((v) =>
        /transaction envelope must contain exactly one BEGIN and one COMMIT/.test(v),
      ),
    ).toBe(true);
  });

  it('ROLLBACK/SAVEPOINT/END inside the envelope', () => {
    for (const payload of ['ROLLBACK;', 'SAVEPOINT;', 'END;']) {
      const { violations } = analyzeMigrationSql(inject(payload));
      expect(
        violations.some((v) =>
          /ROLLBACK\/SAVEPOINT\/END are not allowed inside the migration envelope/.test(v),
        ),
      ).toBe(true);
    }
  });

  it('stray statements after COMMIT are rejected', () => {
    const { violations } = analyzeMigrationSql(append('DROP TABLE lu_site;'));
    expect(violations).toContain('DROP statement is not allowed');
  });
});
