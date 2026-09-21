/**
 * MIG-F3-PG-TEST-003 — adversarial specs for canonical bootstrap-body derivation
 * (no DB, no env, no sockets).
 *
 * deriveCanonicalBootstrapSql must:
 *  - require an exact `BEGIN ... COMMIT` envelope (no foreign BEGIN/COMMIT, no ROLLBACK,
 *    no SAVEPOINT/END, no missing COMMIT, no double envelope),
 *  - strip the outer BEGIN/COMMIT and every `IF NOT EXISTS` from the executed body,
 *  - contain exactly the 4 pinned CREATE TABLE statements and the 5 pinned indexes,
 *  - fail if an occurrence is missing or extra, or an object is renamed,
 *  - never mutate the source string/buffer it derives from.
 */
import { readFileSync } from 'node:fs';
import { fileURLToPath } from 'node:url';
import { IDENTITY_MIGRATION_FILE, sha256Hex } from '../src/database/migration-plan.js';
import {
  MIGRATION_REGISTRY,
  MigrationRegistryError,
  deriveCanonicalBootstrapSql,
  validatePinnedMigration,
} from '../src/database/migration-registry.js';

const MIGRATION_PATH = fileURLToPath(
  new URL(`../migrations/${IDENTITY_MIGRATION_FILE}`, import.meta.url),
);
const REAL_BYTES = readFileSync(MIGRATION_PATH);
const REAL_SQL = REAL_BYTES.toString('utf8');
const entry = MIGRATION_REGISTRY[0];

function deriveOrThrow(sql: string) {
  return deriveCanonicalBootstrapSql(entry, sql);
}

function expectDeriveError(sql: string, fragment: RegExp): Error {
  let caught: unknown;
  try {
    deriveOrThrow(sql);
  } catch (error) {
    caught = error;
  }
  if (caught === undefined) {
    throw new Error('Expected deriveCanonicalBootstrapSql to throw, but it returned.');
  }
  expect(caught).toBeInstanceOf(MigrationRegistryError);
  expect((caught as Error).message).toMatch(fragment);
  return caught as Error;
}

/** Insert payload as the last statement before the envelope-closing COMMIT. */
function injectBeforeCommit(sql: string, payload: string): string {
  return sql.replace(/COMMIT;\s*$/, `${payload}\nCOMMIT;\n`);
}

const canonical = deriveOrThrow(REAL_SQL);

describe('canonical derivation of the real 0001', () => {
  it('strips the external BEGIN/COMMIT: body contains no transaction envelope words', () => {
    expect(/\bBEGIN\b/i.test(canonical.body)).toBe(false);
    expect(/\bCOMMIT\b/i.test(canonical.body)).toBe(false);
    // Still terminates as a sequence of DDL statements; the first statement is not BEGIN.
    expect(canonical.body.trimStart().toUpperCase().startsWith('BEGIN')).toBe(false);
    expect(canonical.body.trimEnd().toUpperCase().endsWith('COMMIT')).toBe(false);
  });

  it('removes every IF NOT EXISTS clause from the body', () => {
    expect(/IF\s+NOT\s+EXISTS/i.test(canonical.body)).toBe(false);
  });

  it('keeps the reentrant advisory xact lock statement inside the body', () => {
    expect(canonical.body).toContain(
      "SELECT pg_advisory_xact_lock(hashtext('lu:identity-control-plane:0001'));",
    );
    expect(entry?.advisoryLockKey).toBe('lu:identity-control-plane:0001');
  });

  it('contains exactly the 4 expected CREATE TABLE and 5 expected CREATE INDEX', () => {
    expect(canonical.createTableCount).toBe(4);
    expect(canonical.uniqueIndexCount).toBe(2);
    expect(canonical.plainIndexCount).toBe(3);
    const tableCount = [...canonical.body.matchAll(/\bCREATE TABLE\b/gi)].length;
    const indexCount = [...canonical.body.matchAll(/\bCREATE\s+(?:UNIQUE\s+)?INDEX\b/gi)].length;
    expect(tableCount).toBe(4);
    expect(indexCount).toBe(5);
    const names = [...canonical.body.matchAll(/\bCREATE\s+(?:UNIQUE\s+)?INDEX\s+(\S+)/gi)]
      .map((m) => m[1]?.toLowerCase())
      .sort();
    expect(names).toEqual(
      [...(entry?.expectedIndexNames ?? [])].map((n) => n.toLowerCase()).sort(),
    );
  });

  it('reports the SOURCE-file digest, not a derived-body digest', () => {
    expect(canonical.derivedFromSha256).toBe(entry?.sha256);
    expect(sha256Hex(canonical.body)).not.toBe(entry?.sha256);
  });

  it('is deterministic: deriving twice yields byte-identical bodies', () => {
    const again = deriveOrThrow(REAL_SQL);
    expect(again.body).toBe(canonical.body);
  });

  it('does not mutate the original source bytes or string', () => {
    const sqlCopy = REAL_SQL.slice();
    const bytesCopy = Buffer.from(REAL_BYTES);
    deriveOrThrow(sqlCopy);
    expect(sqlCopy).toBe(REAL_SQL);
    expect(bytesCopy.equals(REAL_BYTES)).toBe(true);
    expect(sha256Hex(REAL_BYTES)).toBe(entry?.sha256);
  });
});

describe('canonical derivation: occurrence and naming guards', () => {
  it('fails when one CREATE TABLE IF NOT EXISTS occurrence is missing (count 3)', () => {
    const tampered = REAL_SQL.replace('CREATE TABLE IF NOT EXISTS lu_user', 'CREATE TABLE lu_user');
    expectDeriveError(
      tampered,
      /declares 3 CREATE TABLE IF NOT EXISTS statements; 4 were expected/,
    );
  });

  it('fails when an extra CREATE TABLE IF NOT EXISTS is smuggled in the envelope (count 5)', () => {
    const tampered = injectBeforeCommit(
      REAL_SQL,
      'CREATE TABLE IF NOT EXISTS lu_backdoor (id uuid PRIMARY KEY);',
    );
    expectDeriveError(
      tampered,
      /declares 5 CREATE TABLE IF NOT EXISTS statements; 4 were expected/,
    );
  });

  it('fails when an expected index is renamed (missing index)', () => {
    expectDeriveError(
      REAL_SQL.replace('ux_lu_site_code_lower', 'ux_lu_site_code_UPPER'),
      /missing expected index "ux_lu_site_code_lower"/,
    );
  });

  it('fails when an unexpected index is added (extra index)', () => {
    const tampered = injectBeforeCommit(
      REAL_SQL,
      'CREATE INDEX IF NOT EXISTS ix_backdoor ON lu_session (user_id);',
    );
    expectDeriveError(tampered, /contains unexpected index "ix_backdoor"/);
  });
});

describe('canonical derivation: transaction envelope tampering', () => {
  it('fails when the closing COMMIT is missing', () => {
    const noCommit = REAL_SQL.replace(/COMMIT;\s*$/, '');
    expectDeriveError(noCommit, /invalid transaction envelope.*last statement must be COMMIT/s);
  });

  it('fails when the opening BEGIN is gone', () => {
    const noBegin = REAL_SQL.replace(/^\s*BEGIN;\s*/, '');
    expectDeriveError(noBegin, /invalid transaction envelope.*first statement must be BEGIN/s);
  });

  it('fails on a doubled BEGIN', () => {
    expectDeriveError(
      REAL_SQL.replace(/BEGIN;\s*/, 'BEGIN;\nBEGIN;\n'),
      /exactly one BEGIN and one COMMIT/,
    );
  });

  it('fails on a doubled COMMIT', () => {
    expectDeriveError(
      REAL_SQL.replace(/COMMIT;\s*$/, 'COMMIT;\nCOMMIT;\n'),
      /exactly one BEGIN and one COMMIT/,
    );
  });

  it('fails when a BEGIN is smuggled before the final COMMIT', () => {
    expectDeriveError(injectBeforeCommit(REAL_SQL, 'BEGIN;'), /exactly one BEGIN and one COMMIT/);
  });

  it('fails when a ROLLBACK or SAVEPOINT or END appears inside the envelope', () => {
    for (const payload of ['ROLLBACK;', 'SAVEPOINT;', 'END;']) {
      expectDeriveError(
        injectBeforeCommit(REAL_SQL, payload),
        /ROLLBACK\/SAVEPOINT\/END are not allowed inside the migration envelope/,
      );
    }
  });

  it('fails on structural garbage the sanitizer refuses outright (comments/quotes)', () => {
    // A truncated string literal makes the file un-analyzable; derivation must refuse rather
    // than guess. The dangling quote destroys the last statement before COMMIT.
    expectDeriveError(REAL_SQL.replace(/'active'/, "'activ"), /structural issues|envelope/s);
  });

  it('fails when the file contains no statements at all', () => {
    expectDeriveError('', /structural issues|no statements|envelope/s);
  });
});

describe('canonical derivation: quoted-Unicode hole is covered by defense in depth', () => {
  /**
   * CHARACTERIZATION (documented in the QA handoff, not fixed here because src is frozen):
   * a quoted `CREATE TABLE "unicode"` smuggled INSIDE the envelope is not counted by the
   * derivation (the counters only match the `IF NOT EXISTS` textual form) and the name-based
   * allowlist of the generic analyzer cannot parse unicode initial names. The derivation
   * alone therefore returns an "OK" body containing 5 tables. The tooling stays safe because
   * every executable path validates the byte-level registry pin FIRST: any such edit changes
   * the SHA-256, so `validatePinnedMigration` rejects the file before derivation is reached.
   */
  it('derivation-only counters miss a quoted unicode table (pin is the real gate)', () => {
    const tampered = injectBeforeCommit(REAL_SQL, 'CREATE TABLE "ünïcode_tbl" (id uuid);');
    const result = deriveOrThrow(tampered); // accepted by counters alone...
    expect(result.createTableCount).toBe(4);
    expect((result.body.match(/\bCREATE TABLE\b/gi) ?? []).length).toBe(5);
    // ...but the pinned-pipeline refuses the same bytes long before derivation.
    expect(() => validatePinnedMigration(MIGRATION_PATH, Buffer.from(tampered, 'utf8'))).toThrow(
      MigrationRegistryError,
    );
    expect(sha256Hex(Buffer.from(tampered, 'utf8'))).not.toBe(entry?.sha256);
  });

  it('the IF NOT EXISTS unicode variant IS caught by the derivation counters themselves', () => {
    expectDeriveError(
      injectBeforeCommit(REAL_SQL, 'CREATE TABLE IF NOT EXISTS "ünïcode2" (id uuid);'),
      /declares 5 CREATE TABLE IF NOT EXISTS statements/,
    );
  });
});
