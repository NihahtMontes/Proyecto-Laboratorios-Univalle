/**
 * Offline safety analyzer for SQL migrations (no DB access, no `process.env`, no `pg`).
 *
 * `plan` uses this module to prove, before touching a database, that a migration file:
 *  - creates exactly the allowlisted identity-control-plane tables and nothing else,
 *  - contains no destructive statement (DROP / TRUNCATE / DELETE / UPDATE / ALTER) and no
 *    data seeding or privilege changes (INSERT / GRANT / REVOKE / COPY / CREATE ROLE|USER|...),
 *  - is hash-stable (SHA-256 over the exact bytes that would be executed).
 *
 * Comment and string-literal text is stripped before keyword scanning so decorative text
 * (e.g. `-- prevent DROP`) cannot mask real statements, and harmless prose cannot trip the
 * scanner. Referential clauses (`ON DELETE RESTRICT`, ...) are the only tolerated DELETE
 * occurrences. Dollar-quoted bodies and unterminated literals/comments are rejected outright:
 * the analyzer must understand 100% of the file it approves.
 */
import { createHash } from 'node:crypto';

/** Directory (relative to the package cwd) where migration files live. */
export const MIGRATIONS_DIR_NAME = 'migrations';

/** The migration this tool was built for; default target of `up`. */
export const IDENTITY_MIGRATION_FILE = '0001_create_identity_control_plane.sql';

/** Tables introduced by the immutable baseline migration 0001. */
export const BASELINE_TARGET_TABLES = [
  'lu_site',
  'lu_user',
  'lu_site_membership',
  'lu_session',
] as const;

/** Tables introduced by the additive authentication-security migration 0002. */
export const AUTH_SECURITY_TARGET_TABLES = ['lu_auth_rate_limit', 'lu_security_event'] as const;

/** Server-side tenant route catalog introduced by migration 0003. */
export const TENANT_ROUTE_TARGET_TABLES = ['lu_tenant_route'] as const;

/** Backward-compatible alias used by the immutable 0001 analyzer/tests. */
export const TARGET_TABLES = BASELINE_TARGET_TABLES;

/** Complete control-plane table set known by status/inspection. */
export const CONTROL_PLANE_TABLES = [
  ...BASELINE_TARGET_TABLES,
  ...AUTH_SECURITY_TARGET_TABLES,
  ...TENANT_ROUTE_TARGET_TABLES,
] as const;

/** Backward-compatible table type for the immutable 0001 baseline. */
export type TargetTable = (typeof BASELINE_TARGET_TABLES)[number];
export type ControlPlaneTable = (typeof CONTROL_PLANE_TABLES)[number];

/**
 * Minimum columns the post-apply verification expects per table (subset of the SQL file;
 * used by `status`/`up` to prove the schema actually landed).
 */
export const MINIMUM_COLUMNS: Readonly<Record<TargetTable, readonly string[]>> = {
  lu_site: ['id', 'code', 'name', 'status', 'created_at', 'updated_at'],
  lu_user: [
    'id',
    'email',
    'full_name',
    'password_hash',
    'is_super_admin',
    'status',
    'security_version',
    'created_at',
    'updated_at',
  ],
  lu_site_membership: ['user_id', 'site_id', 'role', 'status', 'created_at', 'updated_at'],
  lu_session: [
    'id',
    'token_hash',
    'user_id',
    'security_version',
    'last_seen_at',
    'idle_expires_at',
    'absolute_expires_at',
    'revoked_at',
  ],
};

/** Raised when a file violates the safety policy; carries every detected violation. */
export class MigrationPolicyError extends Error {
  readonly violations: readonly string[];

  constructor(message: string, violations: readonly string[]) {
    super(`${message} -> ${violations.join(' | ')}`);
    this.name = 'MigrationPolicyError';
    this.violations = violations;
  }
}

export interface MigrationPlan {
  /** File name as resolved under the migrations directory. */
  file: string;
  /** Absolute path used for the analysis. */
  path: string;
  /** SHA-256 hex of the exact bytes read from disk. */
  sha256: string;
  /** Byte length of the file content. */
  byteLength: number;
  /** Tables created by the file, in declaration order (validated against TARGET_TABLES). */
  tables: readonly string[];
}

export interface MigrationSqlAnalysis {
  tables: readonly string[];
  violations: readonly string[];
}

/** Hex SHA-256 over the exact bytes of the content (utf8 for strings). */
export function sha256Hex(content: Buffer | string): string {
  const buf = Buffer.isBuffer(content) ? content : Buffer.from(content, 'utf8');
  return createHash('sha256').update(buf).digest('hex');
}

export interface SanitizedSql {
  /** Code text with comments removed, string literals replaced by '' and identifiers unwrapped. */
  code: string;
  /** Structural problems that make the file un-analyzable (treated as violations). */
  issues: readonly string[];
}

/**
 * Removes line comments, block comments and string-literal bodies, and unwraps double-quoted
 * identifiers. Any syntax the analyzer cannot fully understand (dollar-quoted bodies,
 * unterminated comments/strings/identifiers) is reported as an issue instead of guessed.
 */
export function sanitizeSql(sql: string): SanitizedSql {
  const issues: string[] = [];
  let out = '';
  let i = 0;
  const n = sql.length;

  while (i < n) {
    const ch = sql.charAt(i);
    const next = sql.charAt(i + 1);

    if (ch === '-' && next === '-') {
      const nl = sql.indexOf('\n', i + 2);
      if (nl === -1) {
        i = n;
      } else {
        out += '\n';
        i = nl + 1;
      }
      continue;
    }

    if (ch === '/' && next === '*') {
      const end = sql.indexOf('*/', i + 2);
      if (end === -1) {
        issues.push('unterminated block comment; refusing to analyze an unreadable file');
        return { code: out, issues };
      }
      out += ' ';
      i = end + 2;
      continue;
    }

    if (ch === "'") {
      let j = i + 1;
      let closed = false;
      while (j < n) {
        if (sql.charAt(j) === "'") {
          if (sql.charAt(j + 1) === "'") {
            j += 2; // doubled quote is an escaped quote inside the literal
            continue;
          }
          closed = true;
          break;
        }
        j++;
      }
      if (!closed) {
        issues.push('unterminated string literal; refusing to analyze an unreadable file');
        return { code: out, issues };
      }
      out += "''"; // literal body stripped: keywords inside strings are inert
      i = j + 1;
      continue;
    }

    if (ch === '"') {
      let j = i + 1;
      let inner = '';
      let closed = false;
      while (j < n) {
        if (sql.charAt(j) === '"') {
          if (sql.charAt(j + 1) === '"') {
            inner += '"';
            j += 2;
            continue;
          }
          closed = true;
          break;
        }
        inner += sql.charAt(j);
        j++;
      }
      if (!closed) {
        issues.push('unterminated quoted identifier; refusing to analyze an unreadable file');
        return { code: out, issues };
      }
      out += inner;
      i = j + 1;
      continue;
    }

    if (ch === '$') {
      const dollarTag = /^\$(?:[A-Za-z_][A-Za-z0-9_]*)?\$/.exec(sql.slice(i));
      if (dollarTag !== null) {
        issues.push(
          `dollar-quoted body (${dollarTag[0]}) is not supported by the safety analyzer; ` +
            'the migration must be fully inspectable',
        );
        return { code: out, issues };
      }
    }

    out += ch;
    i++;
  }

  return { code: out, issues };
}

/** Referential actions such as `ON DELETE RESTRICT` are DDL metadata, not destructive statements. */
const ON_DELETE_CLAUSE_RE =
  /\bON\s+DELETE\s+(?:NO\s+ACTION|RESTRICT|CASCADE|SET\s+NULL|SET\s+DEFAULT)\b/gi;

const FORBIDDEN_PATTERNS: ReadonlyArray<{ label: string; pattern: RegExp }> = [
  { label: 'DROP statement is not allowed', pattern: /\bDROP\b/i },
  { label: 'TRUNCATE is not allowed', pattern: /\bTRUNCATE\b/i },
  {
    label:
      'destructive DELETE statement is not allowed (only "ON DELETE <action>" clauses are tolerated)',
    pattern: /\bDELETE\b/i,
  },
  { label: 'UPDATE statement is not allowed', pattern: /\bUPDATE\b/i },
  {
    label: 'ALTER is not allowed (the tool never mutates existing objects)',
    pattern: /\bALTER\b/i,
  },
  { label: 'INSERT is not allowed (no seeding)', pattern: /\bINSERT\b/i },
  { label: 'GRANT is not allowed (no privilege changes)', pattern: /\bGRANT\b/i },
  { label: 'REVOKE is not allowed (no privilege changes)', pattern: /\bREVOKE\b/i },
  { label: 'COPY is not allowed (no bulk data movement)', pattern: /\bCOPY\b/i },
  { label: 'VACUUM is not allowed', pattern: /\bVACUUM\b/i },
  {
    label: 'creating roles/users/databases/schemas is not allowed (no principal changes)',
    pattern: /\bCREATE\s+(?:OR\s+REPLACE\s+)?(?:ROLE|USER|GROUP|DATABASE|SCHEMA|TABLESPACE)\b/i,
  },
  {
    label: 'CREATE TEMP/UNLOGGED table is not allowed',
    pattern: /\bCREATE\s+(?:GLOBAL\s+|LOCAL\s+)?(?:TEMP(?:ORARY)?|UNLOGGED)\s+TABLE\b/i,
  },
  {
    label: 'CREATE VIEW / MATERIALIZED VIEW is not allowed',
    pattern: /\bCREATE\s+(?:OR\s+REPLACE\s+)?(?:MATERIALIZED\s+)?VIEW\b/i,
  },
  {
    label: 'CREATE SEQUENCE is not allowed',
    pattern: /\bCREATE\s+(?:TEMPORARY\s+|UNLOGGED\s+)?SEQUENCE\b/i,
  },
  {
    label: 'CREATE FUNCTION / PROCEDURE is not allowed',
    pattern: /\bCREATE\s+(?:OR\s+REPLACE\s+)?(?:FUNCTION|PROCEDURE)\b/i,
  },
  { label: 'CREATE TYPE is not allowed', pattern: /\bCREATE\s+TYPE\b/i },
  {
    label: 'CREATE TRIGGER is not allowed',
    pattern: /\bCREATE\s+(?:OR\s+REPLACE\s+)?TRIGGER\b/i,
  },
  { label: 'CREATE RULE is not allowed', pattern: /\bCREATE\s+RULE\b/i },
  {
    label: 'SET ROLE / SESSION AUTHORIZATION is not allowed',
    pattern: /\bSET\s+(?:ROLE|SESSION\s+AUTHORIZATION)\b/i,
  },
  { label: 'COMMENT is not allowed', pattern: /\bCOMMENT\s+ON\b/i },
];

const CREATE_TABLE_RE =
  /\bCREATE\b\s+(?:(?:GLOBAL|LOCAL)\s+)?(?:TEMP(?:ORARY)?|UNLOGGED|LOGGED)?\s*TABLE\s+(?:IF\s+NOT\s+EXISTS\s+)?([A-Za-z_][A-Za-z0-9_$]*)(?:\s*\.\s*([A-Za-z_][A-Za-z0-9_$]*))?/gi;

/** Extracts created table names (lowercased bare names) from sanitized code. */
export function extractCreatedTables(code: string): string[] {
  const tables: string[] = [];
  for (const match of code.matchAll(CREATE_TABLE_RE)) {
    const first = match[1];
    const second = match[2];
    if (first === undefined) continue;
    tables.push((second ?? first).toLowerCase());
  }
  return tables;
}

/** True when the sanitized code contains a CREATE TABLE the regex could not parse (e.g. quoted/case-sensitive names). */
function hasUnparsedCreateTable(code: string): boolean {
  const loose = /\bCREATE\b[\s\S]{0,40}?\bTABLE\b/gi;
  const looseCount = [...code.matchAll(loose)].length;
  const strictCount = [...code.matchAll(CREATE_TABLE_RE)].length;
  return looseCount > strictCount;
}

/**
 * Full static analysis of one migration file content: structure issues, destructive
 * keywords, and allowlist conformance of CREATE TABLE statements.
 */
export function analyzeMigrationSql(
  sql: string,
  allowedTables: readonly string[] = BASELINE_TARGET_TABLES,
): MigrationSqlAnalysis {
  const { code, issues } = sanitizeSql(sql);
  const violations: string[] = [...issues];

  const statementCode = code.replace(ON_DELETE_CLAUSE_RE, ' ');
  for (const { label, pattern } of FORBIDDEN_PATTERNS) {
    if (pattern.test(statementCode)) {
      violations.push(label);
    }
  }

  const created = extractCreatedTables(code);
  if (created.length === 0 && hasUnparsedCreateTable(code)) {
    violations.push('CREATE TABLE syntax not recognized by the analyzer; refusing to approve it');
  }

  const allowed = new Set<string>(allowedTables);
  const seen = new Set<string>();
  for (const table of created) {
    if (!allowed.has(table)) {
      violations.push(`creates table outside the allowlist: ${table}`);
    } else if (seen.has(table)) {
      violations.push(`table ${table} is declared more than once`);
    }
    seen.add(table);
  }
  for (const table of allowedTables) {
    if (!seen.has(table)) {
      violations.push(`allowlisted table is not created: ${table}`);
    }
  }

  // Schema qualification detection: "CREATE TABLE other.tbl" -> regex captures (other, tbl).
  for (const match of code.matchAll(CREATE_TABLE_RE)) {
    const schema = match[1];
    const table = match[2];
    if (schema !== undefined && table !== undefined && schema.toLowerCase() !== 'public') {
      violations.push(`table ${schema}.${table} uses a non-public schema; only public is allowed`);
    }
  }

  // CREATE INDEX on a non-allowlisted table is a DDL bypass.
  const CREATE_INDEX_RE =
    /\bCREATE\s+(?:UNIQUE\s+)?INDEX\s+(?:IF\s+NOT\s+EXISTS\s+)?(\S+)\s+ON\s+(?:([A-Za-z_][A-Za-z0-9_$]*)\s*\.\s*)?([A-Za-z_][A-Za-z0-9_$]*)/gi;
  for (const match of code.matchAll(CREATE_INDEX_RE)) {
    const schema = match[2]?.toLowerCase();
    const table = match[3]?.toLowerCase();
    if (schema !== undefined && schema !== 'public') {
      violations.push(`CREATE INDEX uses a non-public schema: ${schema}.${table ?? '(unknown)'}`);
    } else if (table !== undefined && !allowed.has(table)) {
      violations.push(`CREATE INDEX on non-allowlisted table: ${table}`);
    }
  }
  const looseIndexCount = [...code.matchAll(/\bCREATE\s+(?:UNIQUE\s+)?INDEX\b/gi)].length;
  const strictIndexCount = [...code.matchAll(CREATE_INDEX_RE)].length;
  if (looseIndexCount > strictIndexCount) {
    violations.push('CREATE INDEX syntax not recognized by the analyzer; refusing to approve it');
  }

  // Transaction envelope: fail-closed. A single BEGIN at the start and a single COMMIT at the
  // end is the only permitted envelope. Anything else (extra BEGIN/COMMIT, ROLLBACK, SAVEPOINT,
  // END, or BEGIN/COMMIT not at the boundaries) is a violation.
  const statements = code
    .split(';')
    .map((s) => s.trim())
    .filter((s) => s !== '');
  const txCommands = statements.filter((s) => /^(BEGIN|COMMIT|ROLLBACK|SAVEPOINT|END)$/i.test(s));
  if (txCommands.length > 0) {
    const first = statements[0]?.toUpperCase() ?? '';
    const last = statements[statements.length - 1]?.toUpperCase() ?? '';
    const beginCount = statements.filter((s) => s.toUpperCase() === 'BEGIN').length;
    const commitCount = statements.filter((s) => s.toUpperCase() === 'COMMIT').length;
    const extraTx = statements.filter((s) => /^(ROLLBACK|SAVEPOINT|END)$/i.test(s)).length;
    if (first !== 'BEGIN') {
      violations.push('transaction envelope must start with BEGIN');
    }
    if (last !== 'COMMIT') {
      violations.push('transaction envelope must end with COMMIT');
    }
    if (beginCount !== 1 || commitCount !== 1) {
      violations.push('transaction envelope must contain exactly one BEGIN and one COMMIT');
    }
    if (extraTx > 0) {
      violations.push('ROLLBACK/SAVEPOINT/END are not allowed inside the migration envelope');
    }
  }

  return { tables: created, violations };
}

/**
 * Builds the plan for one migration file. Throws MigrationPolicyError when the content is
 * not provably safe; the returned hash covers the exact bytes provided.
 */
export function buildMigrationPlan(
  filePath: string,
  content: Buffer | string,
  allowedTables: readonly string[] = BASELINE_TARGET_TABLES,
): MigrationPlan {
  const buf = Buffer.isBuffer(content) ? content : Buffer.from(content, 'utf8');
  const sql = buf.toString('utf8');
  const { tables, violations } = analyzeMigrationSql(sql, allowedTables);
  const file = filePath.split(/[\\/]/).pop() ?? filePath;
  if (violations.length > 0) {
    throw new MigrationPolicyError(`migration "${file}" fails the safety policy`, violations);
  }
  return {
    file,
    path: filePath,
    sha256: sha256Hex(buf),
    byteLength: buf.length,
    tables,
  };
}
