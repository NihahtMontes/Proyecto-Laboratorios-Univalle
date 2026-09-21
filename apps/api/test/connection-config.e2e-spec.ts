/**
 * Unit tests for the connection-string parser (no DB, no network, no sockets).
 *
 * MIG-F3-PG-TEST-003 (Kimi hardening adversarial pass):
 *  - `sslmode=disable` / `SSL Mode=Disable` is now REFUSED for remote hosts and only
 *    accepted on loopback (localhost, 127.0.0.1, ::1, case-insensitive).
 *  - The percent-encoded URI case is kept but pinned to `prefer` / `verify-full`,
 *    which must yield verified TLS.
 *  - Rejection messages must never echo the password (raw or percent-encoded form).
 *
 * Naming note: jest.config.js restricts testMatch to the `.e2e-spec.ts` suffix and is outside
 * allowed write scope of this task, so these pure-parser specs still carry that suffix.
 * They never open sockets and never mock database migrations.
 */
import {
  CONNECTION_STRING_ENV_VAR,
  ConnectionConfigError,
  describeTarget,
  loadConnectionConfigFromEnv,
  parsePostgresConnectionString,
  redactSecrets,
} from '../src/database/connection-config.js';

const BASE =
  'Host=localhost;Port=5432;Database=GastroExample;Username=postgres;Password=pg-pass-42';

const REMOTE = 'Host=db.example.com;Port=5432;Database=GastroExample;Username=postgres';

const SECRET = 'Sup3rS3cret-NoEcho!';

function captureError(raw: string): Error {
  try {
    parsePostgresConnectionString(raw);
  } catch (error) {
    expect(error).toBeInstanceOf(ConnectionConfigError);
    return error as Error;
  }
  throw new Error('Expected parsePostgresConnectionString to throw, but it resolved.');
}

function expectConfigError(raw: string, fragment: RegExp): Error {
  const error = captureError(raw);
  expect(error.message).toMatch(fragment);
  return error;
}

describe('parsePostgresConnectionString (Npgsql key=value format)', () => {
  it('parses the provided Npgsql format', () => {
    const config = parsePostgresConnectionString(BASE);
    expect(config).toEqual({
      host: 'localhost',
      port: 5432,
      database: 'GastroExample',
      user: 'postgres',
      password: 'pg-pass-42',
      ssl: false,
    });
  });

  it('tolerates whitespace around keys and values', () => {
    const config = parsePostgresConnectionString(
      '  Host = localhost ; Port= 5432 ; Database = GastroExample ;\n Username =postgres; Password = pg-pass-42 ',
    );
    expect(config.host).toBe('localhost');
    expect(config.port).toBe(5432);
    expect(config.database).toBe('GastroExample');
    expect(config.user).toBe('postgres');
    expect(config.password).toBe('pg-pass-42');
  });

  it('accepts "User ID" as alias for Username, case-insensitive', () => {
    const config = parsePostgresConnectionString(
      'host=LOCALHOST; user id  = Ana; database=db1; password=; port=1',
    );
    expect(config.user).toBe('Ana');
    expect(config.password).toBe('');
    // Empty Password is allowed explicitly and blocks the PGPASSWORD env fallback.
    expect(config.host).toBe('LOCALHOST');
    expect(config.port).toBe(1);
  });

  it('keeps ";" and quotes inside quoted values (doubled quote escapes the quote char)', () => {
    const config = parsePostgresConnectionString(
      "Host=localhost;Database=d;Username=u;Password='p;a;s''s';Port=5432",
    );
    expect(config.password).toBe("p;a;s's");
    const config2 = parsePostgresConnectionString(
      'Host=localhost;Database=d;Username=u;Password="he said ""hi""; ok";Port=5432',
    );
    expect(config2.password).toBe('he said "hi"; ok');
  });

  it('escapes a literal ";" via ";;" in unquoted values', () => {
    const config = parsePostgresConnectionString(
      'Host=localhost;Database=d;Username=u;Password=a;;b;Port=5432',
    );
    expect(config.password).toBe('a;b');
  });

  it('rejects unknown keys', () => {
    expectConfigError(`${BASE};Application Name=cli`, /Unknown connection string key/i);
  });

  it('rejects duplicate keys, including Username/User ID collisions', () => {
    expectConfigError(
      'Host=a;Host=b;Database=d;Username=u;Password=p',
      /Duplicate connection string key "Host"/,
    );
    expectConfigError(
      'Host=a;Username=u;User ID=v;Database=d;Password=p',
      /Duplicate connection string key "Username"/,
    );
  });

  it('rejects invalid ports', () => {
    for (const port of ['abc', '0', '65536', '99999', '54 32', '-1', '5432.5']) {
      expectConfigError(`Host=h;Port=${port};Database=d;Username=u;Password=p`, /Port must be/);
    }
  });

  it('rejects empty host, database or username', () => {
    expectConfigError(`Host=;Port=5432;Database=d;Username=u;Password=p`, /Host must be present/);
    expectConfigError(
      `Host=h;Port=5432;Database= ;Username=u;Password=p`,
      /Database must be present/,
    );
    expectConfigError(
      `Host=h;Port=5432;Database=d;Username=;Password=p`,
      /Username must be present/,
    );
  });

  it('accepts an omitted Password as explicit empty and never relies on PGPASSWORD', () => {
    const config = parsePostgresConnectionString('Host=localhost;Port=5432;Database=d;Username=u');
    expect(config.password).toBe('');
  });

  it('rejects malformed segments and unterminated quotes', () => {
    expectConfigError("Host='abc;Database=d;Username=u;Password=p", /Unterminated quoted value/);
    expectConfigError(
      'Host=h;Port=5432;Username=u;Password=p;garbage',
      /ended in the middle of a key=value segment/,
    );
    expectConfigError(
      'Host=h;Port=5432;Username=u;Password=p;junk=1',
      /Unknown connection string key/,
    );
  });

  it('rejects brace-wrapped Npgsql strings', () => {
    expectConfigError(`{${BASE}}`, /Brace-wrapped/);
  });

  it('rejects empty input', () => {
    expectConfigError('   ', /empty/);
  });

  it('never echoes the password in parse errors', () => {
    const error = expectConfigError(
      'Host=h;Password=Sup3rS3cret;Port=badport;Database=d;Username=u',
      /Port must be/,
    );
    // Explicit negative check for the secret in the thrown message.
    expect(error.message).not.toContain('Sup3rS3cret');
  });

  it('rejects a bare hostname that is not loopback only when Disable is requested', () => {
    expect(
      parsePostgresConnectionString('Host=sandbox-01;Database=d;Username=u;Password=p').ssl,
    ).toEqual({ rejectUnauthorized: true });
    expectConfigError(
      'Host=sandbox-01;Database=d;Username=u;Password=p;SSL Mode=Disable',
      /only permitted for loopback hosts/,
    );
  });
});

describe('password leak prevention across all rejection paths', () => {
  const SECRET = 'MyP@ssw0rd!2026';
  const ENCODED = encodeURIComponent(SECRET);

  function assertNoPasswordLeak(raw: string): void {
    try {
      parsePostgresConnectionString(raw);
      throw new Error(`Expected parsePostgresConnectionString to throw for: ${raw}`);
    } catch (error) {
      expect(error).toBeInstanceOf(ConnectionConfigError);
      const message = (error as Error).message;
      expect(message).not.toContain(SECRET);
      expect(message).not.toContain(ENCODED);
      // Single-character fragments that could reconstruct the secret are also absent.
      expect(message).not.toContain('MyP');
      expect(message).not.toContain('ssw0rd');
    }
  }

  it('hides the password in remote-disable refusal (URI and key/value)', () => {
    assertNoPasswordLeak(`postgresql://postgres:${ENCODED}@db.example.com/db?sslmode=disable`);
    assertNoPasswordLeak(
      `Host=db.example.com;Database=d;Username=postgres;Password=${SECRET};SSL Mode=Disable`,
    );
  });

  it('hides the password in Require/Allow SSL mode refusal', () => {
    assertNoPasswordLeak(
      `Host=db.example.com;Database=d;Username=postgres;Password=${SECRET};SSL Mode=Require`,
    );
    assertNoPasswordLeak(`postgresql://postgres:${ENCODED}@db.example.com/db?sslmode=allow`);
  });

  it('hides the password in malformed-port and duplicate-key errors', () => {
    assertNoPasswordLeak(
      `Host=db.example.com;Database=d;Username=postgres;Password=${SECRET};Port=bad`,
    );
    assertNoPasswordLeak(
      `Host=h;Database=d;Username=postgres;Password=${SECRET};Username=duplicate`,
    );
  });

  it('hides the password when unknown URI parameters or schemes are rejected', () => {
    assertNoPasswordLeak(`mysql://postgres:${ENCODED}@db.example.com/db`);
    assertNoPasswordLeak(
      `postgresql://postgres:${ENCODED}@db.example.com/db?application_name=${ENCODED}`,
    );
  });

  it('hides the password propagated through the env adapter', () => {
    try {
      loadConnectionConfigFromEnv({
        [CONNECTION_STRING_ENV_VAR]: `postgresql://postgres:${ENCODED}@db.example.com/db?sslmode=disable`,
      });
      throw new Error('Expected loadConnectionConfigFromEnv to throw');
    } catch (error) {
      expect(error).toBeInstanceOf(ConnectionConfigError);
      const message = (error as Error).message;
      expect(message).not.toContain(SECRET);
      expect(message).not.toContain(ENCODED);
    }
  });
});

describe('parsePostgresConnectionString (URI format)', () => {
  it('REJECTS sslmode=disable on a remote URI (remote cleartext is refused post-hardening)', () => {
    const error = expectConfigError(
      'postgresql://postgres:p%40ssw0rd@db.example.com:6543/gastro?sslmode=disable',
      /SSL Mode "disable" is only permitted for loopback hosts/,
    );
    // No echo of the secret in either decoded or percent-encoded form.
    expect(error.message).not.toContain('p@ssw0rd');
    expect(error.message).not.toContain('p%40ssw0rd');
  });

  it('parses postgresql:// with percent-encoded credentials over TLS (sslmode=prefer)', () => {
    const config = parsePostgresConnectionString(
      'postgresql://postgres:p%40ssw0rd@db.example.com:6543/gastro?sslmode=prefer',
    );
    expect(config.user).toBe('postgres');
    expect(config.password).toBe('p@ssw0rd');
    expect(config.host).toBe('db.example.com');
    expect(config.port).toBe(6543);
    expect(config.database).toBe('gastro');
    // Unlike libpq, Prefer is upgraded to mandatory verified TLS (no plaintext fallback).
    expect(config.ssl).toEqual({ rejectUnauthorized: true });
  });

  it('parses percent-encoded credentials with sslmode=verify-full (OS trust store)', () => {
    const config = parsePostgresConnectionString(
      'postgresql://postgres:p%40ss%3Aw0rd@db.example.com:6543/gastro?sslmode=verify-full',
    );
    expect(config.password).toBe('p@ss:w0rd');
    expect(config.ssl).toEqual({ rejectUnauthorized: true });
  });

  it('defaults the port, uses empty password and verified TLS for a remote host', () => {
    const config = parsePostgresConnectionString('postgresql://u@h/db');
    expect(config.port).toBe(5432);
    expect(config.password).toBe('');
    expect(config.ssl).toEqual({ rejectUnauthorized: true });
  });

  it('accepts the postgres:// alias and strips IPv6 brackets', () => {
    const config = parsePostgresConnectionString(
      'postgres://u:p@[::1]:5433/db?sslmode=verify-full',
    );
    expect(config.host).toBe('::1');
    expect(config.port).toBe(5433);
  });

  it('rejects unsupported schemes, extra URI params, duplicated sslmode and nested paths', () => {
    expectConfigError('mysql://u:p@h/db', /Unsupported URI scheme/);
    expectConfigError(
      'postgresql://u:p@h/db?application_name=x',
      /Unsupported URI query parameter/,
    );
    expectConfigError(
      'postgresql://u:p@h/db?sslmode=prefer&sslmode=verify-full',
      /Duplicate "sslmode"/,
    );
    expectConfigError('postgresql://u:p@h/db/extra', /path segments beyond the database/);
  });

  it('rejects empty database or missing user in URIs', () => {
    expectConfigError('postgresql://u:p@h:5432/%20', /Database must be present/);
    expectConfigError('postgresql://h:5432/db', /Username must be present/);
  });
});

describe('SSL Mode=Disable host gating (hardening: cleartext is a LOOPBACK-only opt-out)', () => {
  it('key=value: accepts Disable for localhost, 127.0.0.1 and ::1', () => {
    for (const host of ['localhost', 'LOCALHOST', '127.0.0.1', '::1']) {
      const config = parsePostgresConnectionString(
        `Host=${host};Port=5432;Database=d;Username=u;Password=p;SSL Mode=Disable`,
      );
      expect(config.host).toBe(host);
      expect(config.ssl).toBe(false);
    }
  });

  it('key=value: tolerates mixed case and surrounding spaces in the mode token', () => {
    const config = parsePostgresConnectionString(`${BASE};ssl mode =  dIsAbLe `);
    expect(config.ssl).toBe(false);
  });

  it('key=value: REFUSES Disable for any non-loopback host, without echoing the password', () => {
    const error = expectConfigError(
      `${REMOTE};Password=${SECRET};SSL Mode=Disable`,
      /SSL Mode "disable" is only permitted for loopback hosts/,
    );
    expect(error.message).not.toContain(SECRET);
    for (const host of ['db.example.com', '10.0.0.8', 'pg.internal', 'localhost.evil']) {
      expectConfigError(
        `Host=${host};Database=d;Username=u;Password=p;SSL Mode=Disable`,
        /only permitted for loopback hosts/,
      );
    }
  });

  it('URI: accepts Disable only for loopback hosts (bracketed IPv6 included)', () => {
    expect(
      parsePostgresConnectionString('postgresql://u:p@localhost:5432/d?sslmode=disable').ssl,
    ).toBe(false);
    expect(parsePostgresConnectionString('postgresql://u:p@127.0.0.1/d?sslmode=disable').ssl).toBe(
      false,
    );
    expect(parsePostgresConnectionString('postgresql://u:p@[::1]:5433/d?sslmode=disable').ssl).toBe(
      false,
    );
    expect(
      parsePostgresConnectionString('postgresql://u:p@Localhost:5432/d?sslmode=Disable').ssl,
    ).toBe(false);
  });

  it('URI: REFUSES Disable for a remote host and for hostname lookalikes', () => {
    for (const uri of [
      'postgresql://u:p@db.example.com:6543/gastro?sslmode=disable',
      'postgresql://u:p@127.0.0.1.evil.example/d?sslmode=disable',
      'postgresql://u:p@localhost.evil.example/d?sslmode=disable',
    ]) {
      const error = expectConfigError(uri, /only permitted for loopback hosts/);
      expect(error.message).not.toContain('u:p@');
    }
  });

  it('absent mode stays cleartext on loopback and verified TLS on remote (unchanged)', () => {
    expect(parsePostgresConnectionString(BASE).ssl).toBe(false);
    expect(parsePostgresConnectionString(`${REMOTE};Password=${SECRET}`).ssl).toEqual({
      rejectUnauthorized: true,
    });
  });
});

describe('SSL Mode semantics (documented, fail-closed)', () => {
  it('Require is refused (TLS without verification cannot be made safe without a CA)', () => {
    expectConfigError(`${BASE};SSL Mode=Require`, /SSL Mode "require" is refused/i);
    expectConfigError('postgresql://u:p@h/db?sslmode=require', /is refused/i);
  });

  it('verify-ca and verify-full map to verified TLS; unknown modes and "allow" are refused', () => {
    expect(parsePostgresConnectionString(`${BASE};SSL Mode=verify-full`).ssl).toEqual({
      rejectUnauthorized: true,
    });
    expect(parsePostgresConnectionString(`${BASE};SSL Mode=verify-ca`).ssl).toEqual({
      rejectUnauthorized: true,
    });
    expectConfigError(`${BASE};SSL Mode=allow`, /is refused/i);
    expectConfigError(`${BASE};SSL Mode=tunnel-vision`, /Unsupported SSL Mode value/);
  });
});

describe('loadConnectionConfigFromEnv (fail-closed adapter)', () => {
  it('throws without reading anything when the exact variable is missing', () => {
    expect(() => loadConnectionConfigFromEnv({})).toThrow(ConnectionConfigError);
    try {
      loadConnectionConfigFromEnv({});
    } catch (error) {
      expect((error as Error).message).toContain(CONNECTION_STRING_ENV_VAR);
    }
  });

  it('throws when the variable is whitespace-only', () => {
    expect(() => loadConnectionConfigFromEnv({ [CONNECTION_STRING_ENV_VAR]: '   ' })).toThrow(
      ConnectionConfigError,
    );
  });

  it('parses the value of the exact variable', () => {
    const config = loadConnectionConfigFromEnv({ [CONNECTION_STRING_ENV_VAR]: BASE });
    expect(config.database).toBe('GastroExample');
  });

  it('propagates the remote-disable refusal through the env adapter', () => {
    expect(() =>
      loadConnectionConfigFromEnv({
        [CONNECTION_STRING_ENV_VAR]: `postgresql://u:SecretInUri@remote.internal:5432/db?sslmode=disable`,
      }),
    ).toThrow(/only permitted for loopback hosts/);
  });

  it('ignores look-alike variables (exact name only)', () => {
    expect(() =>
      loadConnectionConfigFromEnv({
        DefaultConnection: BASE,
        CONNECTIONSTRINGS_DEFAULTCONNECTION: BASE,
      }),
    ).toThrow(/Missing required environment variable/);
  });
});

describe('output safety helpers', () => {
  it('describeTarget never contains the password', () => {
    const config = parsePostgresConnectionString(BASE);
    const line = describeTarget(config);
    expect(line).not.toContain('pg-pass-42');
    expect(line).toContain('GastroExample');
    expect(line).toContain('disable (cleartext)');
  });

  it('describeTarget of a remote TLS target names neither user nor password', () => {
    const config = parsePostgresConnectionString(`${REMOTE};Password=${SECRET};SSL Mode=Prefer`);
    const line = describeTarget(config);
    expect(line).not.toContain(SECRET);
    expect(line).not.toContain('postgres');
    expect(line).toContain('verified TLS');
  });

  it('redactSecrets scrubs known secrets from arbitrary messages', () => {
    expect(redactSecrets('auth failed using pg-pass-42 for user', ['pg-pass-42'])).toBe(
      'auth failed using *** for user',
    );
    expect(redactSecrets('nothing to hide', [undefined, ''])).toBe('nothing to hide');
  });
});
