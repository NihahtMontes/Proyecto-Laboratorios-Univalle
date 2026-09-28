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
 *  - `wrapInDoBlock` wraps a payload in `DO $do$ ... $do$;` so a static
 *                    statement can hide inside a dollar body, exercising the
 *                    deep view (W13B).
 *  - `wrapInFunction` wraps a payload in `CREATE OR REPLACE FUNCTION ... AS $body$ ... $body$ LANGUAGE sql;`
 *                    so a static statement can hide inside a function body.
 */
import { readFileSync } from 'node:fs';
import { fileURLToPath } from 'node:url';
import {
  IDENTITY_MIGRATION_FILE,
  analyzeMigrationSql,
  buildMigrationPlan,
} from '../src/database/migration-plan.js';
import { findRegistryEntryByRelativePath } from '../src/database/migration-registry.js';

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

/** Wrap a payload in a `DO $do$ ... $do$;` PL/pgSQL block. The body content
 *  is preserved verbatim by the deep view so a forbidden STATIC keyword
 *  (DROP TABLE, TRUNCATE TABLE, DELETE FROM, ...) is rejected by the deep
 *  scan, even though `sanitizeSql` would normally collapse dollar bodies. */
function wrapInDoBlock(payload: string): string {
  return `DO $do$ BEGIN ${payload} END $do$;`;
}

/** Wrap a payload inside a CREATE OR REPLACE FUNCTION ... AS $body$ ... $body$
 *  LANGUAGE plpgsql; block so it can hide behind a function body. */
function wrapInFunction(payload: string): string {
  return `CREATE OR REPLACE FUNCTION public.lu_smuggle_fn() RETURNS void LANGUAGE plpgsql AS $body$ BEGIN ${payload} END $body$;`;
}

/** Single-shape variants used when the strict policy rejects the top-level
 *  form (so we cannot use it as a control). We still need a positive
 *  control showing the deep view ignores RAISE / comment / string prose. */
function strictViolationsInDoBlock(payload: string): readonly string[] {
  const sql =
    `BEGIN;\n` +
    `CREATE TABLE IF NOT EXISTS lu_witness (id uuid PRIMARY KEY);\n` +
    `${wrapInDoBlock(payload)}\n` +
    `COMMIT;\n`;
  return analyzeMigrationSql(sql, ['lu_witness'], {
    options: { strictPolicy: true },
    touchedTables: ['lu_witness'],
  }).violations;
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
      /creates table outside the touched allowlist: if/,
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

describe('bypass vectors: dynamic EXECUTE shapes are rejected (W10 strict + legacy)', () => {
  // Every payload below is the LAST statement before the closing COMMIT.
  // Positive controls come first so a regression that flips the negative /
  // positive split is caught immediately.

  it('positive controls: CREATE TRIGGER EXECUTE FUNCTION and REVOKE EXECUTE ON FUNCTION pass the analyzer', () => {
    // CREATE TRIGGER body uses EXECUTE FUNCTION <name>(...) — this is the
    // non-dynamic shape the policy explicitly allows. Use the strict
    // policy because it permits CREATE FUNCTION / CREATE TRIGGER (bootstrap
    // does not); the W10 contract is the same regardless of policy tier.
    const trigger = `BEGIN;
      CREATE TABLE IF NOT EXISTS lu_witness (id uuid PRIMARY KEY);
      CREATE OR REPLACE FUNCTION lu_witness_nop() RETURNS trigger LANGUAGE plpgsql AS $body$ BEGIN RETURN NEW; END $body$;
      CREATE TRIGGER trg_lu_witness_noop BEFORE INSERT ON lu_witness FOR EACH ROW EXECUTE FUNCTION lu_witness_nop();
      COMMIT;`;
    expect(
      analyzeMigrationSql(trigger, ['lu_witness'], {
        options: { strictPolicy: true },
        touchedTables: ['lu_witness'],
      }).violations,
    ).toEqual([]);
    // REVOKE / GRANT EXECUTE ON FUNCTION ... is the privilege pattern; the
    // policy explicitly allows it.
    const grant = `BEGIN;
      CREATE TABLE IF NOT EXISTS lu_witness (id uuid PRIMARY KEY);
      CREATE OR REPLACE FUNCTION lu_witness_nop() RETURNS trigger LANGUAGE plpgsql AS $body$ BEGIN RETURN NEW; END $body$;
      REVOKE EXECUTE ON FUNCTION lu_witness_nop() FROM PUBLIC;
      GRANT EXECUTE ON FUNCTION lu_witness_nop() TO PUBLIC;
      COMMIT;`;
    expect(
      analyzeMigrationSql(grant, ['lu_witness'], {
        options: { strictPolicy: true },
        touchedTables: ['lu_witness'],
      }).violations,
    ).toEqual([]);
  });

  it('rejects EXECUTE followed by a string literal (top-level)', () => {
    const bad = `BEGIN;
      CREATE TABLE IF NOT EXISTS lu_witness (id uuid PRIMARY KEY);
      SELECT 1;
      DO $$ BEGIN EXECUTE 'DROP TABLE lu_witness'; END $$;
      COMMIT;`;
    const { violations } = analyzeMigrationSql(bad, ['lu_witness']);
    expect(violations.some((v) => /EXECUTE inside the migration is forbidden/.test(v))).toBe(true);
  });

  it('rejects EXECUTE format(...) (function call) even inside a dollar body', () => {
    const bad = `BEGIN;
      CREATE TABLE IF NOT EXISTS lu_witness (id uuid PRIMARY KEY);
      DO $do$ BEGIN EXECUTE format('DROP TABLE %I', 'lu_witness'); END $do$;
      COMMIT;`;
    const { violations } = analyzeMigrationSql(bad, ['lu_witness']);
    expect(violations.some((v) => /EXECUTE inside the migration is forbidden/.test(v))).toBe(true);
  });

  it('rejects EXECUTE followed by a bare identifier (variable smuggling)', () => {
    const bad = `BEGIN;
      CREATE TABLE IF NOT EXISTS lu_witness (id uuid PRIMARY KEY);
      DO $do$ DECLARE stmt text; BEGIN EXECUTE stmt; END $do$;
      COMMIT;`;
    const { violations } = analyzeMigrationSql(bad, ['lu_witness']);
    expect(violations.some((v) => /EXECUTE inside the migration is forbidden/.test(v))).toBe(true);
  });

  it('rejects EXECUTE followed by a parenthesised expression', () => {
    const bad = `BEGIN;
      CREATE TABLE IF NOT EXISTS lu_witness (id uuid PRIMARY KEY);
      SELECT 1;
      DO $$ BEGIN EXECUTE ('SELECT 1'); END $$;
      COMMIT;`;
    const { violations } = analyzeMigrationSql(bad, ['lu_witness']);
    expect(violations.some((v) => /EXECUTE inside the migration is forbidden/.test(v))).toBe(true);
  });

  it('prose in a comment does NOT trigger the dynamic-EXECUTE rule', () => {
    // The keyword "EXECUTE" inside a `--` comment must be ignored. The
    // sanitizer collapses comments before the keyword scan, so the
    // strict policy keeps passing on harmless documentation.
    const prose = `BEGIN;
      CREATE TABLE IF NOT EXISTS lu_witness (id uuid PRIMARY KEY);
      -- WARNING: never EXECUTE 'DROP TABLE' from this trigger body.
      SELECT 1;
      COMMIT;`;
    expect(analyzeMigrationSql(prose, ['lu_witness']).violations).toEqual([]);
  });

  it('string-literal prose does NOT trigger the dynamic-EXECUTE rule', () => {
    const prose = `BEGIN;
      CREATE TABLE IF NOT EXISTS lu_witness (id uuid PRIMARY KEY);
      SELECT 'EXECUTE format(''DROP TABLE %I'')' AS warning;
      COMMIT;`;
    expect(analyzeMigrationSql(prose, ['lu_witness']).violations).toEqual([]);
  });
});

// ---------------------------------------------------------------------------
// MIG-001-F2-W13B — deep view enforcement: the strict policy MUST scan
// dollar-quoted bodies (DO blocks, CREATE FUNCTION bodies, CREATE TRIGGER
// bodies) for STATIC destructive / principal / schema operations. Dynamic
// EXECUTE smuggling is already covered above; this block exercises the
// additional deep-view surface so a forbidden keyword that hides inside a
// dollar body cannot bypass the analyzer.
//
// Each entry below is wrapped in BOTH a DO block AND a CREATE OR REPLACE
// FUNCTION body so the same smuggling payload exercises both shapes. The
// strict policy uses the deep view (comments + string literals + identifiers
// stripped EVERYWHERE, including inside dollar bodies) for the static
// keyword scan.
//
// False positives that MUST remain inert:
//   - TG_OP = 'DELETE'              (string literal in a comparison)
//   - RAISE EXCEPTION '...DELETE'   (string literal in an error message)
//   - COMMENT '...DROP TABLE...'    (line comment)
//   - GRANT EXECUTE ON FUNCTION     (privilege pattern, explicitly allowed)
//   - CREATE TRIGGER ... EXECUTE FUNCTION   (trigger action, explicitly allowed)
//   - DROP CONSTRAINT in allowlist
//   - DROP CONSTRAINT in a comment / string literal
// ---------------------------------------------------------------------------
describe('strict analyzer deep view (W13B): STATIC smuggling inside dollar bodies is rejected', () => {
  // Each entry is one forbidden static operation that MUST be detected when
  // it appears inside a DO block or inside a CREATE OR REPLACE FUNCTION
  // body. The label fragment is what the strict analyzer emits.
  const forbiddenInDollarBody: ReadonlyArray<{
    label: string;
    pattern: RegExp;
    payload: string;
  }> = [
    // DROP family
    { label: 'DROP TABLE', pattern: /DROP TABLE is not allowed/, payload: 'DROP TABLE lu_user;' },
    {
      label: 'DROP COLUMN',
      pattern: /DROP COLUMN is not allowed/,
      payload: 'ALTER TABLE lu_user DROP COLUMN email;',
    },
    { label: 'DROP SCHEMA', pattern: /DROP SCHEMA is not allowed/, payload: 'DROP SCHEMA public;' },
    {
      label: 'DROP DATABASE',
      pattern: /DROP SCHEMA is not allowed/,
      payload: 'DROP DATABASE lu_main;',
    },
    {
      label: 'DROP ROLE',
      pattern: /DROP SCHEMA is not allowed/,
      payload: 'DROP ROLE lu_auth_runtime;',
    },
    {
      label: 'DROP USER',
      pattern: /DROP SCHEMA is not allowed/,
      payload: 'DROP USER lu_auth_migrator;',
    },
    { label: 'DROP GROUP', pattern: /DROP SCHEMA is not allowed/, payload: 'DROP GROUP lu_staff;' },
    {
      label: 'DROP TABLESPACE',
      pattern: /DROP SCHEMA is not allowed/,
      payload: 'DROP TABLESPACE pg_default;',
    },
    // TRUNCATE / DELETE / COPY / DML
    {
      label: 'TRUNCATE TABLE',
      pattern: /TRUNCATE TABLE is not allowed/,
      payload: 'TRUNCATE TABLE lu_user;',
    },
    {
      label: 'DELETE FROM',
      pattern: /destructive DELETE statement is not allowed/,
      payload: 'DELETE FROM lu_user;',
    },
    { label: 'COPY', pattern: /COPY is not allowed/, payload: "COPY lu_user FROM '/tmp/x.csv';" },
    { label: 'VACUUM', pattern: /VACUUM is not allowed/, payload: 'VACUUM FULL;' },
    // CREATE principal / schema / temp / view / rule
    {
      label: 'CREATE ROLE',
      pattern: /creating roles\/users\/databases\/schemas is not allowed/,
      payload: 'CREATE ROLE smuggle_role LOGIN;',
    },
    {
      label: 'CREATE USER',
      pattern: /creating roles\/users\/databases\/schemas is not allowed/,
      payload: 'CREATE USER smuggle_user LOGIN;',
    },
    {
      label: 'CREATE GROUP',
      pattern: /creating roles\/users\/databases\/schemas is not allowed/,
      payload: 'CREATE GROUP smuggle_group;',
    },
    {
      label: 'CREATE DATABASE',
      pattern: /creating roles\/users\/databases\/schemas is not allowed/,
      payload: 'CREATE DATABASE smuggle_db;',
    },
    {
      label: 'CREATE SCHEMA',
      pattern: /creating roles\/users\/databases\/schemas is not allowed/,
      payload: 'CREATE SCHEMA smuggle_schema;',
    },
    {
      label: 'CREATE TABLESPACE',
      pattern: /creating roles\/users\/databases\/schemas is not allowed/,
      payload: "CREATE TABLESPACE smuggle_ts OWNER lu_auth_migrator LOCATION '/var/pg';",
    },
    {
      label: 'CREATE TEMP TABLE',
      pattern: /CREATE TEMP\/UNLOGGED table is not allowed/,
      payload: 'CREATE TEMP TABLE smuggle_tmp (id int);',
    },
    {
      label: 'CREATE UNLOGGED',
      pattern: /CREATE TEMP\/UNLOGGED table is not allowed/,
      payload: 'CREATE UNLOGGED TABLE smuggle_unlogged (id int);',
    },
    {
      label: 'CREATE VIEW',
      pattern: /CREATE VIEW \/ MATERIALIZED VIEW is not allowed/,
      payload: 'CREATE VIEW smuggle_view AS SELECT 1;',
    },
    {
      label: 'CREATE RULE',
      pattern: /CREATE RULE is not allowed/,
      payload: 'CREATE RULE smuggle_rule AS ON INSERT TO lu_user DO NOTHING;',
    },
    // ALTER principal
    {
      label: 'ALTER ROLE',
      pattern: /ALTER ROLE\/USER\/GROUP is not allowed/,
      payload: 'ALTER ROLE lu_auth_runtime SUPERUSER;',
    },
    {
      label: 'ALTER USER',
      pattern: /ALTER ROLE\/USER\/GROUP is not allowed/,
      payload: 'ALTER USER lu_auth_migrator CREATEDB;',
    },
    {
      label: 'ALTER GROUP',
      pattern: /ALTER ROLE\/USER\/GROUP is not allowed/,
      payload: 'ALTER GROUP smuggle_group ADD USER lu_auth_runtime;',
    },
    // SET ROLE / SESSION AUTHORIZATION
    {
      label: 'SET ROLE',
      pattern: /SET ROLE \/ SESSION AUTHORIZATION is not allowed/,
      payload: 'SET ROLE postgres;',
    },
    {
      label: 'SET SESSION AUTHORIZATION',
      pattern: /SET ROLE \/ SESSION AUTHORIZATION is not allowed/,
      payload: 'SET SESSION AUTHORIZATION postgres;',
    },
  ];

  for (const { label, pattern, payload } of forbiddenInDollarBody) {
    it(`DO block: rejects ${label} smuggled inside a dollar body`, () => {
      const violations = strictViolationsInDoBlock(payload);
      expect(violations.some((v) => pattern.test(v))).toBe(true);
    });

    it(`function body: rejects ${label} smuggled inside a CREATE FUNCTION $body$`, () => {
      // Same payload inside a CREATE OR REPLACE FUNCTION body.
      const sql =
        `BEGIN;\n` +
        `CREATE TABLE IF NOT EXISTS lu_witness (id uuid PRIMARY KEY);\n` +
        `${wrapInFunction(payload)}\n` +
        `COMMIT;\n`;
      const violations = analyzeMigrationSql(sql, ['lu_witness'], {
        options: { strictPolicy: true },
        touchedTables: ['lu_witness'],
      }).violations;
      expect(violations.some((v) => pattern.test(v))).toBe(true);
    });
  }

  it('DO block: rejects DROP CONSTRAINT for a name NOT in the per-entry allowlist', () => {
    const sql =
      `BEGIN;\n` +
      `CREATE TABLE IF NOT EXISTS lu_witness (id uuid PRIMARY KEY);\n` +
      `DO $do$ BEGIN ALTER TABLE lu_witness DROP CONSTRAINT smuggled_evil_constraint; END $do$;\n` +
      `COMMIT;\n`;
    const { violations } = analyzeMigrationSql(sql, ['lu_witness'], {
      options: { strictPolicy: true },
      touchedTables: ['lu_witness'],
    });
    expect(
      violations.some((v) =>
        /DROP CONSTRAINT "smuggled_evil_constraint" is not allowed by the strict policy/.test(v),
      ),
    ).toBe(true);
  });

  it('DO block: permits DROP CONSTRAINT for an allow-listed name (positive control)', () => {
    const sql =
      `BEGIN;\n` +
      `CREATE TABLE IF NOT EXISTS lu_witness (id uuid PRIMARY KEY);\n` +
      `DO $do$ BEGIN ALTER TABLE lu_witness DROP CONSTRAINT IF EXISTS lu_user_status_check; END $do$;\n` +
      `COMMIT;\n`;
    const { violations } = analyzeMigrationSql(sql, ['lu_witness'], {
      options: {
        strictPolicy: true,
        allowedDropConstraints: ['lu_user_status_check'],
      },
      touchedTables: ['lu_witness'],
    });
    expect(violations).toEqual([]);
  });

  it('DO block: ignores a constraint name hidden in a comment (deep view strips comments)', () => {
    const sql =
      `BEGIN;\n` +
      `CREATE TABLE IF NOT EXISTS lu_witness (id uuid PRIMARY KEY);\n` +
      `DO $do$ BEGIN\n` +
      `  -- DROP CONSTRAINT smuggled_evil_constraint;\n` +
      `  NULL;\n` +
      `END $do$;\n` +
      `COMMIT;\n`;
    const { violations } = analyzeMigrationSql(sql, ['lu_witness'], {
      options: { strictPolicy: true },
      touchedTables: ['lu_witness'],
    });
    expect(violations).toEqual([]);
  });

  it('DO block: ignores a constraint name hidden in a string literal (deep view strips strings)', () => {
    const sql =
      `BEGIN;\n` +
      `CREATE TABLE IF NOT EXISTS lu_witness (id uuid PRIMARY KEY);\n` +
      `DO $do$ BEGIN\n` +
      `  RAISE EXCEPTION 'DROP CONSTRAINT smuggled_evil_constraint';\n` +
      `END $do$;\n` +
      `COMMIT;\n`;
    const { violations } = analyzeMigrationSql(sql, ['lu_witness'], {
      options: { strictPolicy: true },
      touchedTables: ['lu_witness'],
    });
    expect(violations).toEqual([]);
  });
});

// ---------------------------------------------------------------------------
// MIG-001-F2-W13B — positive controls for the deep view: legitimate PL/pgSQL
// control flow, trigger event lists, error-message prose and privilege
// statements must NOT be flagged by the strict analyzer. The body content is
// intentionally chosen so that, WITHOUT the deep-view sanitizer, a naive
// "scan the bytes" approach would produce a false positive.
// ---------------------------------------------------------------------------
describe('strict analyzer deep view (W13B): positive controls inside dollar bodies', () => {
  it("TG_OP = 'DELETE' comparison (string literal) is NOT flagged as destructive DELETE", () => {
    const sql =
      `BEGIN;\n` +
      `CREATE TABLE IF NOT EXISTS lu_witness (id uuid PRIMARY KEY);\n` +
      `DO $do$ BEGIN\n` +
      `  IF TG_OP = 'DELETE' THEN RETURN OLD; ELSE RETURN NEW; END IF;\n` +
      `END $do$;\n` +
      `COMMIT;\n`;
    const { violations } = analyzeMigrationSql(sql, ['lu_witness'], {
      options: { strictPolicy: true },
      touchedTables: ['lu_witness'],
    });
    expect(violations).toEqual([]);
  });

  it("RAISE EXCEPTION with 'DELETE forbidden' prose is NOT flagged", () => {
    const sql =
      `BEGIN;\n` +
      `CREATE TABLE IF NOT EXISTS lu_witness (id uuid PRIMARY KEY);\n` +
      `DO $do$ BEGIN\n` +
      `  RAISE EXCEPTION 'rows are immutable; UPDATE/DELETE forbidden';\n` +
      `END $do$;\n` +
      `COMMIT;\n`;
    const { violations } = analyzeMigrationSql(sql, ['lu_witness'], {
      options: { strictPolicy: true },
      touchedTables: ['lu_witness'],
    });
    expect(violations).toEqual([]);
  });

  it('comments mentioning forbidden keywords (DROP TABLE, TRUNCATE, DELETE) are inert', () => {
    const sql =
      `BEGIN;\n` +
      `CREATE TABLE IF NOT EXISTS lu_witness (id uuid PRIMARY KEY);\n` +
      `DO $do$ BEGIN\n` +
      `  -- never DROP TABLE, never TRUNCATE, never DELETE FROM this audit log\n` +
      `  NULL;\n` +
      `END $do$;\n` +
      `COMMIT;\n`;
    const { violations } = analyzeMigrationSql(sql, ['lu_witness'], {
      options: { strictPolicy: true },
      touchedTables: ['lu_witness'],
    });
    expect(violations).toEqual([]);
  });

  it('CREATE TRIGGER ... EXECUTE FUNCTION (trigger action) is NOT flagged as dynamic EXECUTE', () => {
    const sql =
      `BEGIN;\n` +
      `CREATE TABLE IF NOT EXISTS lu_witness (id uuid PRIMARY KEY);\n` +
      `CREATE OR REPLACE FUNCTION public.lu_witness_nop() RETURNS trigger LANGUAGE plpgsql AS $body$ BEGIN RETURN NEW; END $body$;\n` +
      `CREATE TRIGGER trg_lu_witness_noop BEFORE INSERT ON lu_witness FOR EACH ROW EXECUTE FUNCTION public.lu_witness_nop();\n` +
      `COMMIT;\n`;
    const { violations } = analyzeMigrationSql(sql, ['lu_witness'], {
      options: { strictPolicy: true },
      touchedTables: ['lu_witness'],
    });
    expect(violations).toEqual([]);
  });

  it('GRANT EXECUTE ON FUNCTION and REVOKE EXECUTE ON FUNCTION are NOT flagged as dynamic EXECUTE', () => {
    const sql =
      `BEGIN;\n` +
      `CREATE TABLE IF NOT EXISTS lu_witness (id uuid PRIMARY KEY);\n` +
      `CREATE OR REPLACE FUNCTION public.lu_witness_nop() RETURNS trigger LANGUAGE plpgsql AS $body$ BEGIN RETURN NEW; END $body$;\n` +
      `GRANT EXECUTE ON FUNCTION public.lu_witness_nop() TO lu_auth_runtime;\n` +
      `REVOKE EXECUTE ON FUNCTION public.lu_witness_nop() FROM PUBLIC;\n` +
      `COMMIT;\n`;
    const { violations } = analyzeMigrationSql(sql, ['lu_witness'], {
      options: { strictPolicy: true },
      touchedTables: ['lu_witness'],
    });
    expect(violations).toEqual([]);
  });

  it('BEFORE DELETE / UPDATE / INSERT trigger event lists are inert (no FROM clause)', () => {
    const sql =
      `BEGIN;\n` +
      `CREATE TABLE IF NOT EXISTS lu_witness (id uuid PRIMARY KEY);\n` +
      `DO $do$ BEGIN\n` +
      `  CREATE TRIGGER trg_lu_witness_audit BEFORE INSERT OR UPDATE OR DELETE ON lu_witness FOR EACH ROW EXECUTE FUNCTION public.lu_witness_nop();\n` +
      `END $do$;\n` +
      `COMMIT;\n`;
    const { violations } = analyzeMigrationSql(sql, ['lu_witness'], {
      options: { strictPolicy: true },
      touchedTables: ['lu_witness'],
    });
    expect(violations).toEqual([]);
  });

  it('PL/pgSQL control flow (IF/ELSIF/LOOP/EXIT/CONTINUE/RETURN) is inert inside dollar bodies', () => {
    const sql =
      `BEGIN;\n` +
      `CREATE TABLE IF NOT EXISTS lu_witness (id uuid PRIMARY KEY);\n` +
      `DO $do$ DECLARE v int; BEGIN v := 0; LOOP EXIT WHEN v = 1; v := v + 1; CONTINUE WHEN v = 0; END LOOP; RETURN; END $do$;\n` +
      `COMMIT;\n`;
    const { violations } = analyzeMigrationSql(sql, ['lu_witness'], {
      options: { strictPolicy: true },
      touchedTables: ['lu_witness'],
    });
    expect(violations).toEqual([]);
  });

  it('the real 0006 / 0013 files pass under the strict deep view', () => {
    // Sanity check that the deep-view enhancement does not regress the
    // existing strict pins. The 0006 file declares 20 indexes, 5 CREATE
    // TABLEs and 2 DROP CONSTRAINTs (the allow-listed ones), all of which
    // are explicitly permitted by the strict policy.
    const cp0006 = readFileSync(
      fileURLToPath(
        new URL('../migrations/control-plane/0006_mig001_users_identity.sql', import.meta.url),
      ),
      'utf8',
    );
    const cp0006Entry = findRegistryEntryByRelativePath(
      'control-plane/0006_mig001_users_identity.sql',
    )!;
    expect(
      analyzeMigrationSql(cp0006, [...cp0006Entry.targetTables], {
        options: {
          strictPolicy: true,
          allowedDropConstraints: [...cp0006Entry.allowedDropConstraints],
        },
        touchedTables: [...cp0006Entry.touchedTables],
      }).violations,
    ).toEqual([]);
    const tn0013 = readFileSync(
      fileURLToPath(new URL('../migrations/tenant/0013_migration_history.sql', import.meta.url)),
      'utf8',
    );
    const tn0013Entry = findRegistryEntryByRelativePath('tenant/0013_migration_history.sql')!;
    expect(
      analyzeMigrationSql(tn0013, [...tn0013Entry.targetTables], {
        options: {
          strictPolicy: true,
          allowedDropConstraints: [...tn0013Entry.allowedDropConstraints],
        },
        touchedTables: [...tn0013Entry.touchedTables],
      }).violations,
    ).toEqual([]);
  });
});
