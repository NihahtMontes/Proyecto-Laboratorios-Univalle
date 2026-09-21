/**
 * Connection-string configuration for the migration tooling (fail-closed, no secrets in output).
 *
 * The only accepted source is the exact environment variable
 * `ConnectionStrings__DefaultConnection`. Missing/blank values abort: the tool never
 * guesses defaults and never echoes the raw value.
 *
 * Supported formats:
 *  1. URI: `postgresql://user:password@host:port/database?sslmode=...`
 *     (the legacy alias `postgres://` is also accepted; credentials use percent-encoding).
 *  2. Npgsql key=value: `Host=localhost;Port=5432;Database=GastroExample;Username=postgres;Password=...`
 *     - Keys are case-insensitive; whitespace around keys/values is tolerated.
 *     - Accepted keys: Host, Port, Database, Username | User ID, Password, SSL Mode.
 *       Unknown keys and duplicates are rejected.
 *     - Values may be single- or double-quoted. Inside quotes, a doubled quote is a
 *       literal quote and `;` is literal. In unquoted values, `;;` escapes one `;`.
 *     - Password may be omitted for a local trust-auth sandbox. It is normalized to an
 *       explicit empty string so node-postgres never falls back to `PGPASSWORD`.
 *
 * SSL semantics (documented, deliberately stricter than Npgsql/libpq):
 *  - `Disable`             -> cleartext (`ssl: false`). Explicit opt-out for local sandboxes only.
 *  - absent on loopback    -> cleartext (`ssl: false`) for the local sandbox only.
 *  - absent on remote host -> TLS mandatory with full verification against the OS trust store.
 *  - `Prefer`              -> TLS mandatory with full verification against the OS trust store
 *                             (`ssl: { rejectUnauthorized: true }`). Unlike libpq, there is NO
 *                             silent fallback to plaintext: connections to non-TLS servers fail
 *                             unless `Disable` is set explicitly.
 *  - `verify-ca`/`verify-full` -> treated as `Prefer` (system-store verification; no custom CA
 *                             pinning is supported by this tool).
 *  - `Require`             -> REFUSED. In Npgsql/libpq, `Require` means TLS *without*
 *                             certificate verification (MITM risk) and this tool cannot
 *                             guarantee a safe interpretation without a provisioned CA, so the
 *                             fail-closed choice is to reject it outright.
 *  - `allow` and any other value -> refused (unknown or downgrade-capable modes).
 *
 * This module is pure: it never touches `process.env` (an env adapter is provided that takes
 * the env object as a parameter) and never imports `pg`, so it stays testable without a DB.
 */
import type { ConnectionOptions } from 'node:tls';

/** Exact environment variable the tool reads. No alternates, no config files. */
export const CONNECTION_STRING_ENV_VAR = 'ConnectionStrings__DefaultConnection';

/** Default PostgreSQL port used when the connection string omits one. */
export const DEFAULT_POSTGRES_PORT = 5432;

/**
 * Validated, pg-ready connection parameters. `ssl === false` means cleartext (only via an
 * explicit `Disable`); an object means TLS with certificate verification.
 */
export interface PostgresConnectionConfig {
  host: string;
  port: number;
  database: string;
  user: string;
  password: string;
  ssl: false | ConnectionOptions;
}

/** Error type for configuration failures. Messages never contain secret values. */
export class ConnectionConfigError extends Error {
  constructor(message: string) {
    super(message);
    this.name = 'ConnectionConfigError';
  }
}

const KEY_ALIASES: Record<string, string> = {
  HOST: 'Host',
  PORT: 'Port',
  DATABASE: 'Database',
  USERNAME: 'Username',
  'USER ID': 'Username',
  PASSWORD: 'Password',
  'SSL MODE': 'SSL Mode',
};

const VERIFIED_TLS: ConnectionOptions = { rejectUnauthorized: true };

function isSpace(ch: string): boolean {
  return ch === ' ' || ch === '\t' || ch === '\n' || ch === '\r';
}

/** Uppercase + trim + collapse internal whitespace so `user  id` matches `User ID`. */
function normalizeKey(rawKey: string): string {
  return rawKey.trim().toUpperCase().replace(/\s+/g, ' ');
}

/** Echo a key name in errors only when it is a harmless identifier-like token. */
function safeKeyEcho(normalizedKey: string): string {
  return /^[A-Z][A-Z0-9 ]{0,39}$/.test(normalizedKey) ? normalizedKey : '(redacted)';
}

function canonicalizeKey(rawKey: string): string {
  const normalized = normalizeKey(rawKey);
  const canonical = KEY_ALIASES[normalized];
  if (canonical === undefined) {
    throw new ConnectionConfigError(
      `Unknown connection string key "${safeKeyEcho(normalized)}". Accepted keys are ` +
        'Host, Port, Database, Username (or "User ID"), Password and SSL Mode.',
    );
  }
  return canonical;
}

function parsePort(rawPort: string): number {
  if (!/^\d{1,5}$/.test(rawPort)) {
    throw new ConnectionConfigError('Port must be a plain number between 1 and 65535.');
  }
  const port = Number(rawPort);
  if (!Number.isInteger(port) || port < 1 || port > 65535) {
    throw new ConnectionConfigError('Port must be a number between 1 and 65535.');
  }
  return port;
}

function resolveSsl(sslMode: string | undefined, host: string): false | ConnectionOptions {
  if (sslMode === undefined || sslMode.trim() === '') {
    const loopback = host.toLowerCase() === 'localhost' || host === '127.0.0.1' || host === '::1';
    return loopback ? false : VERIFIED_TLS;
  }
  const mode = sslMode.trim().toLowerCase();
  switch (mode) {
    case 'disable': {
      const loopback = host.toLowerCase() === 'localhost' || host === '127.0.0.1' || host === '::1';
      if (!loopback) {
        throw new ConnectionConfigError(
          'SSL Mode "disable" is only permitted for loopback hosts (localhost, 127.0.0.1 or ::1). ' +
            'Remote connections must use verified TLS.',
        );
      }
      return false;
    }
    case 'prefer':
    case 'verify-ca':
    case 'verify-full':
      return VERIFIED_TLS;
    case 'require':
    case 'allow':
      throw new ConnectionConfigError(
        `SSL Mode "${mode}" is refused by policy: "${mode === 'require' ? 'Require' : 'Allow'}" ` +
          'would permit TLS without certificate verification (or a plaintext fallback), and this ' +
          'tool cannot guarantee a secure interpretation without a provisioned CA. Use ' +
          '"Prefer" for verified TLS against the OS trust store, or "Disable" explicitly for a ' +
          'local sandbox on a trusted network.',
      );
    default: {
      const echo = /^[A-Za-z][A-Za-z0-9_-]{0,15}$/.test(mode) ? mode : '(redacted)';
      throw new ConnectionConfigError(
        `Unsupported SSL Mode value "${echo}". Accepted values are Disable, Prefer, ` +
          'verify-ca and verify-full.',
      );
    }
  }
}

function requireNonEmpty(value: string | undefined, label: string): string {
  if (value === undefined || value.trim() === '') {
    throw new ConnectionConfigError(
      `${label} must be present and non-empty in the connection string.`,
    );
  }
  return value.trim();
}

function decodeUriComponentSafe(value: string, label: string): string {
  try {
    return decodeURIComponent(value);
  } catch {
    throw new ConnectionConfigError(
      `The URI ${label} contains an invalid percent-escape sequence.`,
    );
  }
}

function assemble(
  host: string,
  port: number,
  database: string,
  user: string,
  password: string,
  ssl: false | ConnectionOptions,
): PostgresConnectionConfig {
  return {
    host: requireNonEmpty(host, 'Host'),
    port,
    database: requireNonEmpty(database, 'Database'),
    user: requireNonEmpty(user, 'Username'),
    password,
    ssl,
  };
}

/**
 * Parses the Npgsql key=value format with a small dedicated tokenizer (quoted values with
 * doubled-quote escapes, `;;` escapes in unquoted values, whitespace tolerance) and rejects
 * unknown/duplicate keys and malformed segments.
 */
function parseKeyValueFormat(raw: string): PostgresConnectionConfig {
  const entries = new Map<string, string>();
  let i = 0;
  const n = raw.length;

  for (;;) {
    while (i < n && isSpace(raw.charAt(i))) i++;
    if (i >= n) break; // trailing whitespace after the last segment

    // --- key: everything up to '=' ---
    let rawKey = '';
    for (;;) {
      if (i >= n) {
        throw new ConnectionConfigError(
          'Connection string ended in the middle of a key=value segment.',
        );
      }
      const ch = raw.charAt(i);
      if (ch === '=') break;
      if (ch === ';') {
        throw new ConnectionConfigError(
          'Unexpected ";" while reading a key; expected "Key=Value".',
        );
      }
      rawKey += ch;
      i++;
    }
    const canonical = canonicalizeKey(rawKey);
    if (entries.has(canonical)) {
      throw new ConnectionConfigError(`Duplicate connection string key "${canonical}".`);
    }
    i++; // consume '='

    // --- value: quoted or unquoted ---
    while (i < n && isSpace(raw.charAt(i))) i++;
    const quote = raw.charAt(i);
    let value: string;
    if (quote === "'" || quote === '"') {
      i++;
      let buf = '';
      for (;;) {
        if (i >= n) {
          throw new ConnectionConfigError(`Unterminated quoted value for key "${canonical}".`);
        }
        const ch = raw.charAt(i);
        if (ch === quote) {
          if (raw.charAt(i + 1) === quote) {
            buf += quote; // doubled quote -> literal quote
            i += 2;
            continue;
          }
          i++;
          break;
        }
        buf += ch;
        i++;
      }
      while (i < n && isSpace(raw.charAt(i))) i++;
      value = buf;
    } else {
      let buf = '';
      while (i < n) {
        const ch = raw.charAt(i);
        if (ch === ';') {
          if (raw.charAt(i + 1) === ';') {
            buf += ';'; // `;;` escapes one literal ';' inside unquoted values
            i += 2;
            continue;
          }
          break; // real separator
        }
        buf += ch;
        i++;
      }
      value = buf.trimEnd();
    }
    entries.set(canonical, value);

    if (i >= n) break;
    if (raw.charAt(i) === ';') {
      i++;
      continue;
    }
    throw new ConnectionConfigError(
      `Unexpected character after the value of key "${canonical}"; segments must be separated by ";".`,
    );
  }

  if (entries.size === 0) {
    throw new ConnectionConfigError(
      'The connection string does not contain any key=value segment.',
    );
  }
  const host = entries.get('Host') ?? '';
  const portRaw = entries.get('Port');
  const database = entries.get('Database') ?? '';
  const user = entries.get('Username') ?? '';
  const ssl = resolveSsl(entries.get('SSL Mode'), host);
  const port = portRaw === undefined || portRaw === '' ? DEFAULT_POSTGRES_PORT : parsePort(portRaw);
  return assemble(host, port, database, user, entries.get('Password') ?? '', ssl);
}

/** Parses `postgresql://` (and `postgres://`) URIs with percent-decoded credentials. */
function parseUriFormat(raw: string): PostgresConnectionConfig {
  let url: URL;
  try {
    url = new URL(raw);
  } catch {
    throw new ConnectionConfigError('The postgresql:// URI is malformed and could not be parsed.');
  }
  const scheme = url.protocol.replace(/:$/, '').toLowerCase();
  if (scheme !== 'postgresql' && scheme !== 'postgres') {
    throw new ConnectionConfigError(
      `Unsupported URI scheme "(redacted)"; only postgresql:// (and the postgres:// alias) are accepted.`,
    );
  }
  const user = decodeUriComponentSafe(url.username, 'username');
  const password = decodeUriComponentSafe(url.password, 'password');
  let host = decodeUriComponentSafe(url.hostname, 'hostname');
  if (host.startsWith('[') && host.endsWith(']')) {
    host = host.slice(1, -1); // URL keeps brackets around IPv6 literals; net/pg wants them stripped
  }
  const port = url.port === '' ? DEFAULT_POSTGRES_PORT : parsePort(url.port);
  if (!url.pathname.startsWith('/')) {
    throw new ConnectionConfigError('The postgresql:// URI is missing a /database path segment.');
  }
  const dbRaw = url.pathname.slice(1);
  if (dbRaw.includes('/')) {
    throw new ConnectionConfigError(
      'The postgresql:// URI must not contain path segments beyond the database name.',
    );
  }
  const database = decodeUriComponentSafe(dbRaw, 'database');

  let sslMode: string | undefined;
  for (const [key, value] of url.searchParams) {
    if (normalizeKey(key) !== 'SSLMODE') {
      throw new ConnectionConfigError(
        `Unsupported URI query parameter "${safeKeyEcho(normalizeKey(key))}"; only "sslmode" is allowed.`,
      );
    }
    if (sslMode !== undefined) {
      throw new ConnectionConfigError('Duplicate "sslmode" URI parameter.');
    }
    sslMode = value;
  }

  const ssl = resolveSsl(sslMode, host);
  return assemble(host, port, database, user, password, ssl);
}

/**
 * Entry point for parsing the configured connection string value. Detects URI vs key=value
 * format. Never throws with the raw string embedded in the message.
 */
export function parsePostgresConnectionString(raw: string): PostgresConnectionConfig {
  if (typeof raw !== 'string') {
    throw new ConnectionConfigError('The connection string must be a string.');
  }
  const trimmed = raw.trim();
  if (trimmed === '') {
    throw new ConnectionConfigError('The connection string is empty.');
  }
  if (trimmed.startsWith('{')) {
    throw new ConnectionConfigError(
      'Brace-wrapped Npgsql connection strings are not supported; use plain "Key=Value;..." form.',
    );
  }
  if (/^postgres(ql)?:\/\//i.test(trimmed)) {
    return parseUriFormat(trimmed);
  }
  if (/^[a-z][a-z0-9+.-]*:\/\//i.test(trimmed)) {
    // Any other URI-shaped string is a non-Postgres scheme; reject instead of misparsing it
    // as key=value (the raw value may embed credentials, so only a redacted token is shown).
    throw new ConnectionConfigError(
      'Unsupported URI scheme "(redacted)"; only postgresql:// (and the postgres:// alias) are accepted.',
    );
  }
  return parseKeyValueFormat(trimmed);
}

/**
 * Fail-closed environment adapter: reads the exact `ConnectionStrings__DefaultConnection`
 * variable from a provided env record. Pass `process.env` only from CLI entry points.
 */
export function loadConnectionConfigFromEnv(
  env: Record<string, string | undefined>,
): PostgresConnectionConfig {
  const raw = env[CONNECTION_STRING_ENV_VAR];
  if (raw === undefined || raw.trim() === '') {
    throw new ConnectionConfigError(
      `Missing required environment variable "${CONNECTION_STRING_ENV_VAR}". This tool is ` +
        'fail-closed and will not invent a default connection.',
    );
  }
  return parsePostgresConnectionString(raw);
}

/** Secret-free one-line target summary safe for logs/CLI output. */
export function describeTarget(config: PostgresConnectionConfig): string {
  const ssl =
    config.ssl === false ? 'disable (cleartext)' : 'prefer (verified TLS, OS trust store)';
  return `${config.host}:${config.port}/${config.database} [ssl=${ssl}]`;
}

/**
 * Defense in depth: scrub any known secret substrings from messages before printing.
 * Secrets shorter than 2 chars are skipped to avoid mangling unrelated text.
 */
export function redactSecrets(message: string, secrets: readonly (string | undefined)[]): string {
  let out = message;
  for (const secret of secrets) {
    if (typeof secret === 'string' && secret.length >= 2) {
      out = out.split(secret).join('***');
    }
  }
  return out;
}
