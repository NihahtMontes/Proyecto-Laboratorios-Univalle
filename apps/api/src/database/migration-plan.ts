/**
 * Offline safety analyzer for SQL migrations (no DB access, no `process.env`, no `pg`).
 *
 * `analyzeMigrationSql` is the single keyword + envelope policy gate.
 * Per-entry behaviour is selected via `AnalyzeOptions`:
 *  - `legacyPolicy: true`        — for historically-pinned files whose bytes
 *                                   predate the strict envelope. The analyzer
 *                                   still rejects BOM / lone CR / unterminated
 *                                   literals and still enforces the envelope
 *                                   and CREATE-TABLE allowlist. The keyword
 *                                   allowlist is skipped.
 *  - `strictPolicy: true`        — for new additive migrations whose contents
 *                                   are pinned and reviewed. Rejects destructive
 *                                   DDL/DML (DROP TABLE/COLUMN, TRUNCATE, DELETE,
 *                                   COPY, role/principal mutation, dynamic EXECUTE
 *                                   even inside dollar-quoted bodies) and any
 *                                   CREATE ROLE/USER/GROUP/DATABASE/SCHEMA.
 *                                   Permits CREATE TABLE/INDEX/FUNCTION/TRIGGER,
 *                                   ALTER (additive), GRANT/REVOKE, COMMENT ON.
 *  - default (both flags false)   — for bootstrap entries that legitimately need
 *                                   the full keyword allowlist (0001-0003).
 *
 * The byte-level identity check (`sha256Hex`, `canonicalByteLength`,
 * `assertCanonicalBytes`) is CRLF-invariant: on-disk bytes on Windows
 * (CRLF due to Git's `text=auto` + `core.autocrlf=true`) are normalized
 * before any hash is computed. BOMs and lone CRs are rejected.
 */
import { createHash } from 'node:crypto';

export const MIGRATIONS_DIR_NAME = 'migrations';

export type MigrationStream = 'control-plane' | 'tenant';
export const DEFAULT_STREAM: MigrationStream = 'control-plane';

export const IDENTITY_MIGRATION_FILE = '0001_create_identity_control_plane.sql';

export const BASELINE_TARGET_TABLES = [
  'lu_site',
  'lu_user',
  'lu_site_membership',
  'lu_session',
] as const;

export const AUTH_SECURITY_TARGET_TABLES = ['lu_auth_rate_limit', 'lu_security_event'] as const;
export const TENANT_ROUTE_TARGET_TABLES = ['lu_tenant_route'] as const;
export const ACADEMIC_CATALOG_TABLES = ['lu_faculty', 'lu_career', 'lu_site_career'] as const;

export const IDENTITY_CONTRACT_TABLES = [
  'lu_login_identifier',
  'lu_legacy_user_xref',
  'lu_identity_audit_event',
  'lu_identity_migration_state',
  'lu_migration_history',
] as const;

export const TENANT_MIGRATION_HISTORY_TABLES = ['lu_migration_history'] as const;

/** Surfaces that 0006 touches (creates/alters/indexes on) on the control plane. */
export const IDENTITY_CONTRACT_TOUCHED_TABLES = [
  ...BASELINE_TARGET_TABLES,
  ...AUTH_SECURITY_TARGET_TABLES,
  ...TENANT_ROUTE_TARGET_TABLES,
  ...ACADEMIC_CATALOG_TABLES,
  ...IDENTITY_CONTRACT_TABLES,
] as const;

export const TARGET_TABLES = BASELINE_TARGET_TABLES;
/**
 * Pre-0006 control-plane table set. Matches the legacy
 * `IDENTITY_CONTROL_PLANE_SCHEMA_MANIFEST`, `AUTH_SECURITY_SCHEMA_MANIFEST`,
 * and `TENANT_ROUTE_SCHEMA_MANIFEST` types in schema-manifest.ts.
 */
export const CONTROL_PLANE_TABLES = [
  ...BASELINE_TARGET_TABLES,
  ...AUTH_SECURITY_TARGET_TABLES,
  ...TENANT_ROUTE_TARGET_TABLES,
] as const;
/** Post-0006 (F2-W2) cumulative control-plane table set. */
export const CONTROL_PLANE_TABLES_POST_F2 = IDENTITY_CONTRACT_TOUCHED_TABLES;
export const TENANT_TABLES = [...TENANT_MIGRATION_HISTORY_TABLES] as const;

export type TargetTable = (typeof BASELINE_TARGET_TABLES)[number];
export type ControlPlaneTable = (typeof CONTROL_PLANE_TABLES)[number];
export type PostF2ControlPlaneTable = (typeof CONTROL_PLANE_TABLES_POST_F2)[number];
export type TenantTable = (typeof TENANT_TABLES)[number];

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

export class MigrationPolicyError extends Error {
  readonly violations: readonly string[];
  constructor(message: string, violations: readonly string[]) {
    super(`${message} -> ${violations.join(' | ')}`);
    this.name = 'MigrationPolicyError';
    this.violations = violations;
  }
}

export interface MigrationPlan {
  file: string;
  path: string;
  stream: MigrationStream;
  sha256: string;
  byteLength: number;
  rawByteLength: number;
  tables: readonly string[];
}

export interface MigrationSqlAnalysis {
  tables: readonly string[];
  violations: readonly string[];
}

export interface AnalyzeOptions {
  /** Legacy entries (0001-0005, tenant 0001-0012): skip the keyword allowlist. */
  readonly legacyPolicy?: boolean;
  /** Strict entries (0006, tenant 0013): forbid only destructive DDL/DML. */
  readonly strictPolicy?: boolean;
  /**
   * Names of DROP CONSTRAINT statements the strict policy permits. Defaults to
   * the two constraints 0006 actually drops (`lu_user_status_check`,
   * `lu_user_password_hash_check`); the runner / registry may override.
   */
  readonly allowedDropConstraints?: readonly string[];
}

/**
 * Hash the canonical (LF-normalized) UTF-8 bytes. This is invariant under
 * Git's CRLF checkout and under any tool that does the same canonicalization.
 */
export function sha256Hex(content: Buffer | string): string {
  const raw = Buffer.isBuffer(content) ? content : Buffer.from(content, 'utf8');
  const canonical = Buffer.from(raw.toString('utf8').replace(/\r\n/g, '\n'), 'utf8');
  return createHash('sha256').update(canonical).digest('hex');
}

/** Canonical (LF) byte length of the content. */
export function canonicalByteLength(content: Buffer | string): number {
  const raw = Buffer.isBuffer(content) ? content : Buffer.from(content, 'utf8');
  return Buffer.byteLength(raw.toString('utf8').replace(/\r\n/g, '\n'), 'utf8');
}

/**
 * Reject files that Git's CRLF checkout would silently encode differently
 * from the in-repo LF content. The byte-level identity guarantee relies on
 * the canonical LF form, so a BOM or a lone CR is an integrity violation.
 */
export function assertCanonicalBytes(raw: Buffer, label: string): void {
  if (raw.length >= 3 && raw[0] === 0xef && raw[1] === 0xbb && raw[2] === 0xbf) {
    throw new Error(`${label}: file starts with a UTF-8 BOM; canonical migration files must not.`);
  }
  for (let i = 0; i < raw.length; i++) {
    if (raw[i] === 0x0d) {
      if (raw[i + 1] !== 0x0a) {
        throw new Error(
          `${label}: lone CR at byte offset ${i} (no following LF). Git CRLF normalization must have been bypassed; this is rejected to keep registry pins invariant.`,
        );
      }
      i++;
    }
  }
}

export interface SanitizedSql {
  code: string;
  issues: readonly string[];
}

/**
 * Strip comments, string literals and double-quoted identifiers while
 * PRESERVING the textual content of every dollar-quoted body intact. The
 * output is suitable for scanning for keywords that must be inspected
 * regardless of whether the analyzer can follow their containing dollar
 * block (e.g. dynamic `EXECUTE 'foo'` smuggling inside `DO $do$ ... $do$`).
 *
 * Unterminated comments / literals / identifiers abort with an issue.
 */
export function stripCommentsAndLiteralsKeepDollarBodies(sql: string): SanitizedSql {
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
            j += 2;
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
      out += "''";
      i = j + 1;
      continue;
    }

    if (ch === '"') {
      let j = i + 1;
      let closed = false;
      while (j < n) {
        if (sql.charAt(j) === '"') {
          if (sql.charAt(j + 1) === '"') {
            j += 2;
            continue;
          }
          closed = true;
          break;
        }
        j++;
      }
      if (!closed) {
        issues.push('unterminated quoted identifier; refusing to analyze an unreadable file');
        return { code: out, issues };
      }
      // Preserve the identifier text so a dynamic EXECUTE that names an
      // identifier can still be detected.
      out += sql.slice(i + 1, j);
      i = j + 1;
      continue;
    }

    if (ch === '$') {
      const dollarTag = /^\$(?:[A-Za-z_][A-Za-z0-9_]*)?\$/.exec(sql.slice(i));
      if (dollarTag !== null) {
        const openTag = dollarTag[0];
        const closeIdx = sql.indexOf(openTag, i + openTag.length);
        if (closeIdx === -1) {
          issues.push(`unterminated dollar-quoted body ${openTag}; refusing to analyze`);
          return { code: out, issues };
        }
        // Preserve the dollar body content verbatim so the keyword scanner
        // can detect smuggling inside a trigger / DO block.
        out += sql.slice(i, closeIdx + openTag.length);
        i = closeIdx + openTag.length;
        continue;
      }
    }

    out += ch;
    i++;
  }

  return { code: out, issues };
}

/**
 * Deep view used by the strict policy to scan for static destructive /
 schema / principal operations inside dollar-quoted bodies (DO blocks,
 CREATE FUNCTION bodies, CREATE TRIGGER bodies, EXECUTE-chained bodies).
 *
 * Differences from `stripCommentsAndLiteralsKeepDollarBodies`:
 *   - Comments (`--` and `/* ... *\/`) are stripped EVERYWHERE, including
 *     INSIDE every dollar-quoted body. A comment that says `DROP TABLE`
 *     cannot smuggle the keyword past the keyword scanner.
 *   - String literals (`'...'`) are stripped EVERYWHERE, including inside
 *     dollar bodies. `RAISE EXCEPTION 'DROP TABLE'` collapses to
 *     `RAISE EXCEPTION ''` so the keyword scanner cannot be tricked by
 *     prose inside an error message.
 *   - Double-quoted identifiers are stripped EVERYWHERE (the inner text is
 *     preserved as bare code to match the `sanitizeSql` convention used by
 *     the rest of the analyzer).
 *   - Dollar body tags (`$$`, `$do$`, `$func$`, ...) are preserved verbatim
 *     so the resulting text is structurally a valid SQL fragment that
 *     still contains every statement top-level OR nested in a dollar
 *     block. Nested dollar bodies (rare in practice) are collapsed to
 *     whitespace by `sanitizeSql(body, true)` to keep the deep view flat.
 *
 * The dynamic EXECUTE scanner continues to use the verbatim-dollar-body
 * view (`stripCommentsAndLiteralsKeepDollarBodies`) because the dynamic
 * EXECUTE rule only needs to recognise the *shape* of `EXECUTE ...`,
 * regardless of whether the executed payload contains prose that looks
 * like destructive SQL — any dynamic EXECUTE is rejected on sight.
 *
 * Unterminated comments / literals / identifiers abort with an issue.
 */
export function stripCommentsAndLiteralsWithDeepDollarBodies(sql: string): SanitizedSql {
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
            j += 2;
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
      out += "''";
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
        const openTag = dollarTag[0];
        const closeIdx = sql.indexOf(openTag, i + openTag.length);
        if (closeIdx === -1) {
          issues.push(`unterminated dollar-quoted body ${openTag}; refusing to analyze`);
          return { code: out, issues };
        }
        const body = sql.slice(i + openTag.length, closeIdx);
        // Recursively sanitize the body content: comments / string literals /
        // double-quoted identifiers are stripped inside the body as well.
        // `sanitizeSql(body, true)` collapses any NESTED dollar bodies (rare
        // in practice) to whitespace, keeping the deep view flat.
        const sanitizedBody = sanitizeSql(body, true);
        if (sanitizedBody.issues.length > 0) {
          for (const issue of sanitizedBody.issues) issues.push(issue);
        }
        out += openTag + sanitizedBody.code + openTag;
        i = closeIdx + openTag.length;
        continue;
      }
    }

    out += ch;
    i++;
  }

  return { code: out, issues };
}

/**
 * Strip comments, string literals and double-quoted identifiers. Dollar-quoted
 * bodies are either skipped (when `allowDollarBodies` is true) or reported as
 * an issue. Unterminated comments / literals / identifiers abort the analyzer.
 */
export function sanitizeSql(sql: string, allowDollarBodies = false): SanitizedSql {
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
            j += 2;
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
      out += "''";
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
        if (allowDollarBodies) {
          const openTag = dollarTag[0];
          const closeIdx = sql.indexOf(openTag, i + openTag.length);
          if (closeIdx === -1) {
            issues.push(`unterminated dollar-quoted body ${openTag}; refusing to analyze`);
            return { code: out, issues };
          }
          const body = sql.slice(i + openTag.length, closeIdx);
          out += openTag + body.replace(/[^\n]/g, ' ') + openTag;
          i = closeIdx + openTag.length;
          continue;
        }
        issues.push(
          `dollar-quoted body (${dollarTag[0]}) is not supported by the safety analyzer; ` +
            'the migration must be fully inspectable or be declared with `legacyPolicy: true`',
        );
        return { code: out, issues };
      }
    }

    out += ch;
    i++;
  }

  return { code: out, issues };
}

const CREATE_TABLE_RE =
  /\bCREATE\b\s+(?:(?:GLOBAL|LOCAL)\s+)?(?:TEMP(?:ORARY)?|UNLOGGED|LOGGED)?\s*TABLE\s+(?:IF\s+NOT\s+EXISTS\s+)?([A-Za-z_][A-Za-z0-9_$]*)(?:\s*\.\s*([A-Za-z_][A-Za-z0-9_$]*))?/gi;

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

const ON_DELETE_CLAUSE_RE =
  /\bON\s+DELETE\s+(?:NO\s+ACTION|RESTRICT|CASCADE|SET\s+NULL|SET\s+DEFAULT)\b/gi;

const CREATE_INDEX_RE =
  /\bCREATE\s+(?:UNIQUE\s+)?INDEX\s+(?:IF\s+NOT\s+EXISTS\s+)?(\S+)\s+ON\s+(?:([A-Za-z_][A-Za-z0-9_$]*)\s*\.\s*)?([A-Za-z_][A-Za-z0-9_$]*)/gi;

const FORBIDDEN_PATTERNS_STRICT: ReadonlyArray<{ label: string; pattern: RegExp }> = [
  { label: 'DROP TABLE is not allowed (no destructive DDL)', pattern: /\bDROP\s+TABLE\b/i },
  { label: 'DROP COLUMN is not allowed (no destructive DDL)', pattern: /\bDROP\s+COLUMN\b/i },
  {
    label: 'DROP SCHEMA is not allowed (no destructive DDL)',
    pattern: /\bDROP\s+(?:SCHEMA|TABLESPACE|DATABASE|ROLE|USER|GROUP)\b/i,
  },
  // DROP CONSTRAINT is forbidden UNLESS the constraint name matches one of
  // the per-entry allow-listed names declared on the entry. The matcher is
  // applied first so a constraint name outside the allowlist triggers the
  // generic DROP CONSTRAINT label below.
  {
    label: 'DROP CONSTRAINT IF EXISTS is not allowed outside the allow-listed names',
    pattern: /\bDROP\s+CONSTRAINT\b/i,
  },
  // Trigger event lists (`BEFORE TRUNCATE ON`, `AFTER ... OR TRUNCATE ON`) are
  // not destructive statements; the strict policy only forbids TRUNCATE as a
  // stand-alone data-movement command, which requires the TABLE keyword.
  { label: 'TRUNCATE TABLE is not allowed (no destructive DDL)', pattern: /\bTRUNCATE\s+TABLE\b/i },
  {
    label:
      'destructive DELETE statement is not allowed (only "ON DELETE <action>" clauses and trigger event lists are tolerated)',
    pattern: /\bDELETE\s+FROM\b/i,
  },
  { label: 'COPY is not allowed (no bulk data movement)', pattern: /\bCOPY\b/i },
  { label: 'VACUUM is not allowed', pattern: /\bVACUUM\b/i },
  {
    label: 'creating roles/users/databases/schemas is not allowed (no principal changes)',
    pattern: /\bCREATE\s+(?:OR\s+REPLACE\s+)?(?:ROLE|USER|GROUP|DATABASE|SCHEMA|TABLESPACE)\b/i,
  },
  {
    label: 'ALTER ROLE/USER/GROUP is not allowed (no principal changes)',
    pattern: /\bALTER\s+(?:ROLE|USER|GROUP)\b/i,
  },
  {
    label: 'CREATE TEMP/UNLOGGED table is not allowed',
    pattern: /\bCREATE\s+(?:GLOBAL\s+|LOCAL\s+)?(?:TEMP(?:ORARY)?|UNLOGGED)\s+TABLE\b/i,
  },
  {
    label: 'CREATE VIEW / MATERIALIZED VIEW is not allowed',
    pattern: /\bCREATE\s+(?:OR\s+REPLACE\s+)?(?:MATERIALIZED\s+)?VIEW\b/i,
  },
  { label: 'CREATE RULE is not allowed', pattern: /\bCREATE\s+RULE\b/i },
  {
    label: 'SET ROLE / SESSION AUTHORIZATION is not allowed',
    pattern: /\bSET\s+(?:ROLE|SESSION\s+AUTHORIZATION)\b/i,
  },
];

/**
 * Names of the DROP CONSTRAINT IF EXISTS statements 0006 is allowed to issue.
 * Every other DROP CONSTRAINT is rejected by the strict policy. Declared on
 * the entry via `allowedDropConstraints`; tested post-statement-scanning.
 */
const STRICT_DEFAULT_ALLOWED_DROP_CONSTRAINTS: readonly string[] = [
  'lu_user_status_check',
  'lu_user_password_hash_check',
];

/**
 * Even under legacy or strict policy, dynamic SQL smuggling through EXECUTE is
 * forbidden because the analyzer cannot inspect it. The shape we forbid is
 * a non-static EXECUTE: a string literal (`EXECUTE '...'`), a parenthesised
 * expression (`EXECUTE (...)`), a function call (`EXECUTE format(...)`), or a
 * bare identifier (`EXECUTE varname`).
 *
 * We deliberately ALLOW only two EXECUTE shapes:
 *  - `EXECUTE FUNCTION <name>(...)`        (CREATE TRIGGER body)
 *  - `EXECUTE ON FUNCTION <name>(...) ...` (privilege GRANT/REVOKE)
 *
 * Both allowed shapes start `EXECUTE FUNCTION` or `EXECUTE ON`; the negative
 * lookahead below excludes them while still rejecting every dynamic shape.
 * The check runs against the ORIGINAL sql (not the sanitized form) because
 * dollar-quoted bodies are collapsed to whitespace by sanitizeSql and a
 * dynamic-EXECUTE smuggling attempt inside a trigger function body must be
 * rejected before the bytes reach PostgreSQL.
 */
const DYNAMIC_EXECUTE_FORBIDDEN: ReadonlyArray<{ label: string; pattern: RegExp }> = [
  {
    label:
      'EXECUTE inside the migration is forbidden when it is dynamic (string literal, parenthesised expression, format() call or bare identifier); only EXECUTE FUNCTION (trigger action) and EXECUTE ON FUNCTION (privilege) are allowed',
    pattern: /\bEXECUTE\s+(?!(?:FUNCTION|ON)\b)(?:'[^']*'|\(|format\b|[A-Za-z_]\w*)/i,
  },
];

/**
 * Full keyword allowlist used only for bootstrap entries (0001-0003). It is
 * intentionally broader than the strict policy so that the historical
 * bootstrap DDL is accepted; for every other policy tier, the strict
 * keyword set above is the gate.
 */
const FORBIDDEN_PATTERNS_BOOTSTRAP: ReadonlyArray<{ label: string; pattern: RegExp }> = [
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
  { label: 'CREATE TRIGGER is not allowed', pattern: /\bCREATE\s+(?:OR\s+REPLACE\s+)?TRIGGER\b/i },
  { label: 'CREATE RULE is not allowed', pattern: /\bCREATE\s+RULE\b/i },
  {
    label: 'SET ROLE / SESSION AUTHORIZATION is not allowed',
    pattern: /\bSET\s+(?:ROLE|SESSION\s+AUTHORIZATION)\b/i,
  },
  { label: 'COMMENT is not allowed', pattern: /\bCOMMENT\s+ON\b/i },
];

export interface AnalyzeContext {
  /** Per-entry policy switches. */
  readonly options?: AnalyzeOptions;
  /** Tables the migration may declare (CREATE). Defaults to TARGET_TABLES. */
  readonly allowedTables?: readonly string[];
  /** Tables the migration may touch (CREATE/ALTER/INDEX on). Defaults to allowedTables. */
  readonly touchedTables?: readonly string[];
  /** Allow dollar-quoted bodies (function/trigger bodies). Defaults to legacyPolicy||strictPolicy. */
  readonly allowDollarBodies?: boolean;
}

/**
 * Full static analysis. Returns every detected violation; an empty `violations`
 * array means the migration is safe to apply.
 */
export function analyzeMigrationSql(
  sql: string,
  allowedTables: readonly string[] = TARGET_TABLES,
  context: AnalyzeContext = {},
): MigrationSqlAnalysis {
  const { options = {}, touchedTables = allowedTables } = context;
  const legacy = options.legacyPolicy === true;
  const strict = options.strictPolicy === true;
  // Strict policy hides dollar bodies so the body of CREATE FUNCTION /
  // CREATE TRIGGER cannot smuggle destructive DDL past the analyzer.
  // Legacy policy ALSO hides dollar bodies (so un-analyzable $do$ / $func$
  // / $grant$ blocks are not flagged as "unsupported"); dynamic EXECUTE
  // smuggling is detected against the ORIGINAL sql (not the sanitized code)
  // because dollar bodies are replaced with whitespace by sanitizeSql.
  const allowDollarBodies = context.allowDollarBodies ?? (legacy || strict);

  const { code, issues } = sanitizeSql(sql, allowDollarBodies);
  const violations: string[] = [...issues];

  const statementCode = code.replace(ON_DELETE_CLAUSE_RE, ' ');
  // `commentAndLiteralsStripped` is the ORIGINAL sql with line/block
  // comments and string literals removed but every dollar-quoted body kept
  // verbatim. The keyword scanner uses this view for dynamic EXECUTE
  // smuggling: the analyzer cannot inspect a dollar body's contents so the
  // pattern must be matched against the actual bytes, with comments and
  // string literals stripped so the analyzer is not tricked by prose or
  // by content inside a quoted literal.
  const { code: commentAndLiteralsStripped, issues: deepIssues } =
    stripCommentsAndLiteralsKeepDollarBodies(sql);
  if (deepIssues.length > 0) {
    for (const issue of deepIssues) violations.push(issue);
  }
  // `deepView` is the deep view: comments, string literals and double-quoted
  // identifiers are stripped EVERYWHERE (including INSIDE every dollar-quoted
  // body), while dollar body tags themselves are preserved verbatim. The strict
  // policy uses this view to scan for STATIC destructive / principal / schema
  // operations inside DO / function / trigger bodies without being fooled by
  // prose inside RAISE strings or comments. DROP CONSTRAINT in particular must
  // use this view so the per-name allowlist covers dollar-body smuggling while
  // a constraint name hidden in a comment / string literal is correctly ignored.
  const { code: deepView, issues: deepViewIssues } =
    stripCommentsAndLiteralsWithDeepDollarBodies(sql);
  if (deepViewIssues.length > 0) {
    for (const issue of deepViewIssues) violations.push(issue);
  }

  if (strict) {
    // The DROP CONSTRAINT rule is omitted from the generic forbidden-patterns
    // scan because the per-name allowlist below handles DROP CONSTRAINT
    // precisely; otherwise an allow-listed DROP CONSTRAINT would generate
    // a violation from the generic pattern AND from the per-name check.
    // Every generic pattern below runs against the deep view so a forbidden
    // static keyword smuggled inside a DO / CREATE FUNCTION / CREATE TRIGGER
    // body is rejected at scan time, while harmless prose inside RAISE
    // strings, comments, or quoted identifiers is already stripped.
    for (const { label, pattern } of FORBIDDEN_PATTERNS_STRICT) {
      if (pattern.source.includes('DROP\\s+CONSTRAINT')) continue;
      if (pattern.test(deepView)) {
        violations.push(label);
      }
    }
    // Dynamic EXECUTE smuggling: scan the SQL in a view that keeps every
    // dollar-quoted body verbatim. This view lets the pattern match a
    // smuggling attempt hidden inside a `DO $do$ ... $do$` block or a
    // CREATE TRIGGER / CREATE FUNCTION body that the generic sanitizer
    // cannot inspect. Only `EXECUTE FUNCTION` (trigger body) and `EXECUTE
    // ON FUNCTION` (privilege) are allowed; every other EXECUTE shape
    // (string literal, parenthesised expression, format() call, bare
    // identifier) is rejected.
    for (const { label, pattern } of DYNAMIC_EXECUTE_FORBIDDEN) {
      if (pattern.test(commentAndLiteralsStripped)) {
        violations.push(label);
        break;
      }
    }
    // DROP CONSTRAINT allowlist: every DROP CONSTRAINT must name one of the
    // declared allow-listed constraints. The check runs on the deep view
    // (top-level statements + dollar body content with comments/strings
    // stripped) so the allowlist covers dollar-body smuggling while a
    // constraint name hidden in a comment / string literal is correctly
    // ignored. The raw-SQL scan previously used as "defense in depth" has
    // been removed: it would have flagged constraint names that appear
    // only in comments / string literals, which is not a real smuggling
    // vector and is rejected below by the deep view anyway.
    const allowedDropConstraints = new Set<string>(
      context.options?.allowedDropConstraints ?? STRICT_DEFAULT_ALLOWED_DROP_CONSTRAINTS,
    );
    const dropConstraintNameRe =
      /\bDROP\s+CONSTRAINT(?:\s+IF\s+EXISTS)?\s+([A-Za-z_][A-Za-z0-9_]*)/gi;
    for (const match of deepView.matchAll(dropConstraintNameRe)) {
      const name = match[1];
      if (name === undefined || !allowedDropConstraints.has(name)) {
        violations.push(
          `DROP CONSTRAINT "${name ?? '(unknown)'}" is not allowed by the strict policy`,
        );
      }
    }
    // UPDATE / INSERT INTO must target a table in the per-entry allowlist.
    // To avoid false positives on trigger event lists (`INSERT OR UPDATE
    // ON table`), trigger UPDATE OF clauses (`BEFORE UPDATE OF (cols) ON
    // table`), and trigger function bodies (already collapsed to whitespace
    // by sanitizeSql), the UPDATE rule requires `SET` to follow the target
    // and the INSERT rule requires the target to be followed by an opening
    // paren or a column list. This way only real DML statements are checked.
    // The deep view is used so a smuggling UPDATE / INSERT INTO inside a
    // dollar body is also caught.
    const updateDmlRe =
      /\bUPDATE\s+(?:ONLY\s+)?(?:([A-Za-z_][A-Za-z0-9_]*)\s*\.\s*)?([A-Za-z_][A-Za-z0-9_]*)\s+SET\b/gi;
    const insertDmlRe =
      /\bINSERT\s+INTO\s+(?:ONLY\s+)?(?:([A-Za-z_][A-Za-z0-9_]*)\s*\.\s*)?([A-Za-z_][A-Za-z0-9_]*)\s*(?:\(|[A-Za-z_])/gi;
    const allowedTableSet = new Set<string>(touchedTables.map((t) => t.toLowerCase()));
    for (const match of deepView.matchAll(updateDmlRe)) {
      const table = ((match[2] ?? '') as string).toLowerCase();
      if (table === '' || !allowedTableSet.has(table)) {
        violations.push(
          `UPDATE on table "${table || '(unknown)'}" is not allowed by the strict policy`,
        );
      }
    }
    for (const match of deepView.matchAll(insertDmlRe)) {
      const table = ((match[2] ?? '') as string).toLowerCase();
      if (table === '' || !allowedTableSet.has(table)) {
        violations.push(
          `INSERT INTO "${table || '(unknown)'}" is not allowed by the strict policy`,
        );
      }
    }
  } else if (legacy) {
    // Legacy entries have an empty touched allowlist by design; the pin
    // guarantees the bytes. We only enforce dynamic-EXECUTE smuggling
    // against the view that strips comments and literals but keeps
    // dollar-quoted bodies intact, so a smuggling attempt inside a
    // trigger function body is still caught.
    for (const { label, pattern } of DYNAMIC_EXECUTE_FORBIDDEN) {
      if (pattern.test(commentAndLiteralsStripped)) {
        violations.push(label);
        break;
      }
    }
  } else {
    for (const { label, pattern } of FORBIDDEN_PATTERNS_BOOTSTRAP) {
      if (pattern.test(statementCode)) {
        violations.push(label);
      }
    }
    // Bootstrap (0001-0003) entries never ship dynamic EXECUTE; reject
    // every dynamic shape with the same rationale as the strict / legacy
    // branches so the analyzer cannot be tricked into accepting a
    // smuggling payload just because the entry is a bootstrap pin.
    for (const { label, pattern } of DYNAMIC_EXECUTE_FORBIDDEN) {
      if (pattern.test(commentAndLiteralsStripped)) {
        violations.push(label);
        break;
      }
    }
  }

  // Table allowlist: any table the migration CREATEs must be in `touchedTables`.
  // Legacy entries skip this check (the pin is the gate); bootstrap and strict
  // entries enforce it.
  const allowed = new Set<string>(touchedTables);
  const seen = new Set<string>();
  const created = extractCreatedTables(code);
  const enforceAllowlist = !legacy;
  for (const table of created) {
    if (enforceAllowlist && !allowed.has(table)) {
      violations.push(`creates table outside the touched allowlist: ${table}`);
    } else if (seen.has(table)) {
      violations.push(`table ${table} is declared more than once`);
    }
    seen.add(table);
  }
  for (const match of code.matchAll(CREATE_TABLE_RE)) {
    const schema = match[1];
    const table = match[2];
    if (schema !== undefined && table !== undefined && schema.toLowerCase() !== 'public') {
      violations.push(`table ${schema}.${table} uses a non-public schema; only public is allowed`);
    }
  }
  // Bootstrap entries (no policy flag) require EVERY allowlisted table to
  // be created in this migration. Strict and legacy entries are allowed to
  // touch tables without creating them (they may be ADD COLUMN, indexes, etc.).
  if (!legacy && !strict) {
    for (const table of allowedTables) {
      if (!seen.has(table)) {
        violations.push(`allowlisted table is not created: ${table}`);
      }
    }
  }

  // CREATE INDEX on a non-touched table is a DDL bypass; legacy entries skip.
  for (const match of code.matchAll(CREATE_INDEX_RE)) {
    const schema = match[2]?.toLowerCase();
    const table = match[3]?.toLowerCase();
    if (schema !== undefined && schema !== 'public') {
      violations.push(`CREATE INDEX uses a non-public schema: ${schema}.${table ?? '(unknown)'}`);
    } else if (enforceAllowlist && table !== undefined && !allowed.has(table)) {
      violations.push(`CREATE INDEX on non-allowlisted table: ${table}`);
    }
  }
  const looseIndexCount = [...code.matchAll(/\bCREATE\s+(?:UNIQUE\s+)?INDEX\b/gi)].length;
  const strictIndexCount = [...code.matchAll(CREATE_INDEX_RE)].length;
  if (looseIndexCount > strictIndexCount) {
    violations.push('CREATE INDEX syntax not recognized by the analyzer; refusing to approve it');
  }

  // Transaction envelope: BEGIN at start, COMMIT at end, exactly one each.
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
    if (first !== 'BEGIN') violations.push('transaction envelope must start with BEGIN');
    if (last !== 'COMMIT') violations.push('transaction envelope must end with COMMIT');
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
 * Build the plan for one migration file. Throws `MigrationPolicyError` when
 * the content is not provably safe; the returned hash covers the canonical
 * LF-normalized bytes.
 */
export function buildMigrationPlan(
  filePath: string,
  content: Buffer | string,
  allowedTables: readonly string[] = TARGET_TABLES,
  context: AnalyzeContext & { readonly stream?: MigrationStream } = {},
): MigrationPlan {
  const buf = Buffer.isBuffer(content) ? content : Buffer.from(content, 'utf8');
  assertCanonicalBytes(buf, filePath);
  const sql = buf.toString('utf8');
  const { tables, violations } = analyzeMigrationSql(sql, allowedTables, context);
  const file = filePath.split(/[\\/]/).pop() ?? filePath;
  if (violations.length > 0) {
    throw new MigrationPolicyError(`migration "${file}" fails the safety policy`, violations);
  }
  return {
    file,
    path: filePath,
    stream: context.stream ?? DEFAULT_STREAM,
    sha256: sha256Hex(buf),
    byteLength: canonicalByteLength(buf),
    rawByteLength: buf.length,
    tables,
  };
}
