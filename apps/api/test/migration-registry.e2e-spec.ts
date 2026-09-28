/**
 * MIG-F3-PG-TEST-003 — adversarial specs for the migration registry pin (no DB, no env).
 *
 * The registry is the PRIMARY integrity gate of the whole tooling: every command that can
 * reach a database validates the exact bytes against the pinned SHA-256/length before the
 * pg Client is ever constructed. These specs pin the pin:
 *  - the real 0001 file must match sha256 a7698e...bc118 and 4491 bytes;
 *  - a single flipped byte (same length, so ONLY the hash can catch it) must fail;
 *  - an unregistered file name must fail even when its bytes are the real ones;
 *  - the real plan must pass end-to-end through validatePinnedMigration.
 */
import { createHash } from 'node:crypto';
import { readFileSync } from 'node:fs';
import { fileURLToPath } from 'node:url';
import {
  AUTH_SECURITY_TARGET_TABLES,
  IDENTITY_MIGRATION_FILE,
  TARGET_TABLES,
  TENANT_ROUTE_TARGET_TABLES,
  buildMigrationPlan,
  canonicalByteLength,
  sha256Hex,
} from '../src/database/migration-plan.js';
import {
  CONTROL_PLANE_REGISTRY,
  MIGRATION_REGISTRY,
  MigrationRegistryError,
  TENANT_REGISTRY,
  deriveCanonicalBootstrapSql,
  findRegistryEntryByRelativePath,
  findRegistryEntry,
  listRegisteredMigrations,
  validatePinnedMigration,
} from '../src/database/migration-registry.js';
import { __testing as runnerTesting } from '../src/database/migration-runner.js';
import { CONTROL_PLANE_SUPPLEMENTAL_EXPECTATIONS } from '../src/database/schema-manifest-f2.js';
import {
  TENANT_MIGRATION_MANIFESTS,
  TENANT_SUPPLEMENTAL_EXPECTATIONS,
  type TenantOrdinal,
} from '../src/database/schema-manifest-tenant.js';

const MIGRATION_PATH = fileURLToPath(
  new URL(`../migrations/${IDENTITY_MIGRATION_FILE}`, import.meta.url),
);
const REAL_BYTES = readFileSync(MIGRATION_PATH);
const REAL_CANONICAL_LEN = canonicalByteLength(REAL_BYTES);
const AUTH_SECURITY_FILE = '0002_create_auth_security_controls.sql';
const AUTH_SECURITY_PATH = fileURLToPath(
  new URL(`../migrations/${AUTH_SECURITY_FILE}`, import.meta.url),
);
const AUTH_SECURITY_BYTES = readFileSync(AUTH_SECURITY_PATH);
const TENANT_ROUTE_FILE = '0003_create_tenant_route_catalog.sql';
const TENANT_ROUTE_PATH = fileURLToPath(
  new URL(`../migrations/${TENANT_ROUTE_FILE}`, import.meta.url),
);
const TENANT_ROUTE_BYTES = readFileSync(TENANT_ROUTE_PATH);

/** The approved digest, spelled out independently of the registry so the test fails if the
 *  registry itself is ever silently re-pinned to a different file. */
const APPROVED_SHA256 = 'a7698ea53a443d42e87bf905ae9e0f7060923a26199bc06ad23f17401e1bc118';
const APPROVED_BYTE_LENGTH = 4491;
const AUTH_SECURITY_SHA256 = '335bd7b1ed75b557e1618283d2272c4353bb029ca2fd28ee626566d3d93ff336';
const AUTH_SECURITY_BYTE_LENGTH = 2727;
const TENANT_ROUTE_SHA256 = 'dfc0dc9a4cea47c584d5cc1f8705d30a3df9cc589f8702b9afeb15b43dd851f1';
const TENANT_ROUTE_BYTE_LENGTH = 1692;

function expectRegistryError(fn: () => unknown, fragment: RegExp): Error {
  let caught: unknown;
  try {
    fn();
  } catch (error) {
    caught = error;
  }
  if (caught === undefined) {
    throw new Error('Expected validatePinnedMigration to throw, but it returned.');
  }
  expect(caught).toBeInstanceOf(MigrationRegistryError);
  const message = (caught as Error).message;
  expect(message).toMatch(fragment);
  return caught as Error;
}

describe('migration-registry: the approved pin (MIG-F3-PG-IDENTITY-001)', () => {
  it('registers the immutable baseline and both additive migrations', () => {
    const entries = listRegisteredMigrations();
    expect(entries).toHaveLength(3);
    const entry = entries[0];
    expect(entry?.file).toBe(IDENTITY_MIGRATION_FILE);
    expect(entry?.style).toBe('bootstrap');
    expect(entry?.workId).toBe('MIG-F3-PG-IDENTITY-001');
    expect(findRegistryEntry(IDENTITY_MIGRATION_FILE)).toBe(MIGRATION_REGISTRY[0]);
    expect(findRegistryEntry(AUTH_SECURITY_FILE)).toBe(MIGRATION_REGISTRY[1]);
    expect(findRegistryEntry(TENANT_ROUTE_FILE)).toBe(MIGRATION_REGISTRY[2]);
    expect(findRegistryEntry('0004_something.sql')).toBeUndefined();
  });

  it('0001 on disk MATCHES the pinned hash a7698e...bc118 and 4491 canonical bytes', () => {
    const entry = MIGRATION_REGISTRY[0]!;
    expect(entry.sha256).toBe(APPROVED_SHA256);
    expect(entry.byteLength).toBe(APPROVED_BYTE_LENGTH);
    // The registry pins the canonical (LF) bytes; with Git CRLF on Windows
    // the on-disk bytes may differ in length due to the LF<->CRLF mapping
    // applied at checkout. The verifier must compare canonical lengths.
    expect(REAL_CANONICAL_LEN).toBe(APPROVED_BYTE_LENGTH);
    expect(sha256Hex(REAL_BYTES)).toBe(APPROVED_SHA256);
  });

  it('the real plan passes: validatePinnedMigration returns entry + policy plan', () => {
    const { entry, plan } = validatePinnedMigration(MIGRATION_PATH, REAL_BYTES);
    expect(entry.file).toBe(IDENTITY_MIGRATION_FILE);
    expect(plan.sha256).toBe(APPROVED_SHA256);
    expect(plan.byteLength).toBe(APPROVED_BYTE_LENGTH);
    expect(plan.tables).toEqual([...TARGET_TABLES]);
    // The plan returned by the registry must be byte-identical to the standalone analyzer.
    expect(plan).toEqual(buildMigrationPlan(MIGRATION_PATH, REAL_BYTES));
    // Concurrency metadata: transaction-scoped advisory lock key.
    expect(entry.advisoryLockKey).toBe('lu:identity-control-plane:0001');
    expect(entry.targetSchema).toBe('public');
    expect([...entry.targetTables]).toEqual([...TARGET_TABLES]);
  });
});

describe('migration-registry: tenant route catalog pin (MIG-F4-ROUTE-CATALOG-008)', () => {
  it('pins and validates the exact 0003 bytes with a route-only allowlist', () => {
    const entry = MIGRATION_REGISTRY[2]!;
    expect(entry.file).toBe(TENANT_ROUTE_FILE);
    expect(entry.style).toBe('additive');
    expect(entry.workId).toBe('MIG-F4-ROUTE-CATALOG-008');
    expect(entry.sha256).toBe(TENANT_ROUTE_SHA256);
    expect(entry.byteLength).toBe(TENANT_ROUTE_BYTE_LENGTH);
    // CRLF-tolerant: compare against the canonical LF length of the on-disk buffer.
    expect(canonicalByteLength(TENANT_ROUTE_BYTES)).toBe(TENANT_ROUTE_BYTE_LENGTH);
    expect(sha256Hex(TENANT_ROUTE_BYTES)).toBe(TENANT_ROUTE_SHA256);

    const validated = validatePinnedMigration(TENANT_ROUTE_PATH, TENANT_ROUTE_BYTES);
    expect(validated.entry).toBe(entry);
    expect(validated.plan.tables).toEqual([...TENANT_ROUTE_TARGET_TABLES]);
    expect(validated.plan).toEqual(
      buildMigrationPlan(TENANT_ROUTE_PATH, TENANT_ROUTE_BYTES, TENANT_ROUTE_TARGET_TABLES),
    );
  });
});

describe('migration-registry: auth security controls pin (MIG-F3-AUTH-SECURITY-014)', () => {
  it('pins and validates the exact 0002 bytes with its own table allowlist', () => {
    const entry = MIGRATION_REGISTRY[1]!;
    expect(entry.file).toBe(AUTH_SECURITY_FILE);
    expect(entry.style).toBe('additive');
    expect(entry.workId).toBe('MIG-F3-AUTH-SECURITY-014');
    expect(entry.sha256).toBe(AUTH_SECURITY_SHA256);
    expect(entry.byteLength).toBe(AUTH_SECURITY_BYTE_LENGTH);
    // Use the canonical LF length for the on-disk buffer; CRLF on Windows
    // would otherwise inflate the raw byte length by the LF<->CRLF swap.
    expect(canonicalByteLength(AUTH_SECURITY_BYTES)).toBe(AUTH_SECURITY_BYTE_LENGTH);
    expect(sha256Hex(AUTH_SECURITY_BYTES)).toBe(AUTH_SECURITY_SHA256);

    const validated = validatePinnedMigration(AUTH_SECURITY_PATH, AUTH_SECURITY_BYTES);
    expect(validated.entry).toBe(entry);
    expect(validated.plan.tables).toEqual([...AUTH_SECURITY_TARGET_TABLES]);
    expect(validated.plan).toEqual(
      buildMigrationPlan(AUTH_SECURITY_PATH, AUTH_SECURITY_BYTES, AUTH_SECURITY_TARGET_TABLES),
    );
  });

  it('rejects tampered 0002 bytes without changing the approved pin', () => {
    const tampered = Buffer.from(AUTH_SECURITY_BYTES);
    tampered[tampered.indexOf('lu_auth_rate_limit')] = 0x4c;
    expectRegistryError(
      () => validatePinnedMigration(AUTH_SECURITY_PATH, tampered),
      /SHA-256 mismatch/,
    );
  });
});

describe('migration-registry: tampering is rejected', () => {
  it('a single flipped byte fails the hash even when the byte length is unchanged', () => {
    const tampered = Buffer.from(REAL_BYTES);
    // Flip the case of one ASCII letter inside the first CREATE TABLE name (length preserved,
    // so only the SHA-256 comparison can catch it).
    const at = tampered.indexOf('lu_site');
    expect(at).toBeGreaterThan(0);
    const code = tampered[at];
    tampered[at] = code === 0x6c /* l */ ? 0x4c /* L */ : 0x6c;
    // Comparing to the on-disk length (REAL_BYTES.length) — invariant under
    // the byte-flip. Comparing to APPROVED_BYTE_LENGTH would couple the test
    // to LF canonicalisation and break on Windows CRLF checkouts; the
    // canonical-length check is already covered above by REAL_CANONICAL_LEN.
    expect(tampered.length).toBe(REAL_BYTES.length);
    const error = expectRegistryError(
      () => validatePinnedMigration(MIGRATION_PATH, tampered),
      /SHA-256 mismatch/,
    );
    // The message quotes both digests (public data, never secrets) for operator forensics.
    expect(error.message).toContain(APPROVED_SHA256);
    expect(error.message).toContain(sha256Hex(tampered));
  });

  it('appended whitespace fails the pin (no "trim then compare" shortcut)', () => {
    expectRegistryError(
      () => validatePinnedMigration(MIGRATION_PATH, Buffer.concat([REAL_BYTES, Buffer.from('\n')])),
      /SHA-256 mismatch/,
    );
  });

  it('registered file name with foreign content is rejected', () => {
    expectRegistryError(
      () => validatePinnedMigration(MIGRATION_PATH, Buffer.from('BEGIN;\nCOMMIT;\n', 'utf8')),
      /SHA-256 mismatch/,
    );
  });

  it('an unregistered file name is rejected even with the APPROVED bytes', () => {
    for (const name of [
      '0000_unregistered.sql',
      '0001_create_identity_control_plane.sql.bak',
      'evil.sql',
    ]) {
      const error = expectRegistryError(
        () => validatePinnedMigration(`/tmp/sql/${name}`, REAL_BYTES),
        /is not registered in the migration registry/,
      );
      expect(error.message).toContain(name);
    }
  });

  it('path traversal in the file name cannot spoof the registry lookup', () => {
    // The lookup uses the last path segment; a nested path still resolves to an unregistered
    // name, and a registered BASENAME smuggled in a different directory is checked against
    // its pinned digest of the BYTES, not the path.
    expectRegistryError(
      () => validatePinnedMigration('../../tmp/0042_x.sql', REAL_BYTES),
      /is not registered/,
    );
    expectRegistryError(
      () => validatePinnedMigration(`C:\\evil\\other\\0042_x.sql`, REAL_BYTES),
      /is not registered/,
    );
  });
});

// ---------------------------------------------------------------------------
// MIG-001-F2-W11 — independent canonical recomputation of the two NEW pins
// (0006 / tenant 0013). The hashes and byte lengths below are literal test
// constants computed with node:crypto directly over `utf8 -> CRLF collapsed
// to LF` bytes, WITHOUT importing the production sha256Hex/canonicalByteLength
// helpers. f2-w2 already re-hashes every entry with the production helpers;
// this block guards against a shared-helper bug re-pinning both sides to the
// same wrong value, and against metadata drift in the create/index surface.
// ---------------------------------------------------------------------------
const CP0006_PATH = fileURLToPath(
  new URL('../migrations/control-plane/0006_mig001_users_identity.sql', import.meta.url),
);
const TN0013_PATH = fileURLToPath(
  new URL('../migrations/tenant/0013_migration_history.sql', import.meta.url),
);

/** Literal W9 re-pins, spelled out independently of the registry module. */
const CP0006_APPROVED_SHA256 = '16d7b3e0e26c91947097b9bb2ac4df6181fffc1cf7460c8cfd48aecc071be3b4';
const CP0006_APPROVED_BYTES = 54878;
const TN0013_APPROVED_SHA256 = '5a2c7e89054b58d57e9b2958c60ac259c1a0f8cfdbca8cc10403d91728ea16d8';
const TN0013_APPROVED_BYTES = 2640;

function independentCanonicalBytes(buf: Buffer): Buffer {
  return Buffer.from(buf.toString('utf8').replace(/\r\n/g, '\n'), 'utf8');
}

function independentSha256(buf: Buffer): string {
  return createHash('sha256').update(independentCanonicalBytes(buf)).digest('hex');
}

function independentIndexSurface(text: string): {
  createTableCount: number;
  uniqueIndexCount: number;
  plainIndexCount: number;
  indexNames: string[];
} {
  const body = text.replace(/--[^\n]*/g, '');
  const createTableCount = (body.match(/CREATE\s+TABLE\s+IF\s+NOT\s+EXISTS/gi) ?? []).length;
  const uniqueIndexCount = (body.match(/CREATE\s+UNIQUE\s+INDEX\s+IF\s+NOT\s+EXISTS/gi) ?? [])
    .length;
  const plainIndexCount = (body.match(/CREATE\s+INDEX\s+IF\s+NOT\s+EXISTS/gi) ?? []).length;
  const indexNames = [
    ...body.matchAll(/CREATE\s+(?:UNIQUE\s+)?INDEX\s+(?:IF\s+NOT\s+EXISTS\s+)?([A-Za-z0-9_]+)/gi),
  ].map((m) => m[1]!.toLowerCase());
  return { createTableCount, uniqueIndexCount, plainIndexCount, indexNames };
}

describe('migration-registry W11: independent canonical recomputation of the 0006 pin', () => {
  const raw = readFileSync(CP0006_PATH);

  it('canonical LF bytes + SHA-256 recomputed with node:crypto match the approved W9 re-pin', () => {
    const entry = findRegistryEntryByRelativePath('control-plane/0006_mig001_users_identity.sql')!;
    expect(CP0006_APPROVED_SHA256).toBe(entry.sha256);
    expect(CP0006_APPROVED_BYTES).toBe(entry.byteLength);
    expect(independentSha256(raw)).toBe(CP0006_APPROVED_SHA256);
    expect(independentCanonicalBytes(raw).length).toBe(CP0006_APPROVED_BYTES);
    // The production helpers must agree with the independent recomputation.
    expect(sha256Hex(raw)).toBe(independentSha256(raw));
    expect(canonicalByteLength(raw)).toBe(independentCanonicalBytes(raw).length);
  });

  it('real create/index metadata: 5 CREATE TABLE, 7 unique + 13 plain indexes, exact name set', () => {
    const entry = findRegistryEntryByRelativePath('control-plane/0006_mig001_users_identity.sql')!;
    const surface = independentIndexSurface(independentCanonicalBytes(raw).toString('utf8'));
    expect(surface.createTableCount).toBe(5);
    expect(surface.uniqueIndexCount).toBe(7);
    expect(surface.plainIndexCount).toBe(13);
    expect([...new Set(surface.indexNames)].sort()).toEqual([...entry.expectedIndexNames].sort());
    expect(entry.expectedIndexNames).toHaveLength(20);
    expect(entry.expectedCreateTableCount).toBe(5);
    expect(entry.targetTables).toHaveLength(5);
    // The canonical derivation agrees with the independent surface scan.
    const derived = deriveCanonicalBootstrapSql(
      entry,
      independentCanonicalBytes(raw).toString('utf8'),
    );
    expect(derived.createTableCount).toBe(surface.createTableCount);
    expect(derived.uniqueIndexCount).toBe(surface.uniqueIndexCount);
    expect(derived.plainIndexCount).toBe(surface.plainIndexCount);
  });

  it('validatePinnedMigration accepts the raw and canonical buffers and reports consistent lengths', () => {
    const entry = findRegistryEntryByRelativePath('control-plane/0006_mig001_users_identity.sql')!;
    const { plan } = validatePinnedMigration(CP0006_PATH, raw, { stream: 'control-plane' });
    expect(plan.sha256).toBe(entry.sha256);
    expect(plan.byteLength).toBe(entry.byteLength);
    expect(plan.rawByteLength).toBe(raw.length);
    expect(() =>
      validatePinnedMigration(CP0006_PATH, independentCanonicalBytes(raw), {
        stream: 'control-plane',
      }),
    ).not.toThrow();
  });
});

describe('migration-registry W11: independent canonical recomputation of the tenant 0013 pin', () => {
  const raw = readFileSync(TN0013_PATH);

  it('canonical LF bytes + SHA-256 recomputed with node:crypto match the approved W9 re-pin', () => {
    const entry = findRegistryEntryByRelativePath('tenant/0013_migration_history.sql')!;
    expect(TN0013_APPROVED_SHA256).toBe(entry.sha256);
    expect(TN0013_APPROVED_BYTES).toBe(entry.byteLength);
    expect(independentSha256(raw)).toBe(TN0013_APPROVED_SHA256);
    expect(independentCanonicalBytes(raw).length).toBe(TN0013_APPROVED_BYTES);
    expect(sha256Hex(raw)).toBe(independentSha256(raw));
    expect(canonicalByteLength(raw)).toBe(independentCanonicalBytes(raw).length);
  });

  it('real create/index metadata: 1 CREATE TABLE, 0 unique + 1 plain index, exact name set', () => {
    const entry = findRegistryEntryByRelativePath('tenant/0013_migration_history.sql')!;
    const surface = independentIndexSurface(independentCanonicalBytes(raw).toString('utf8'));
    expect(surface.createTableCount).toBe(1);
    expect(surface.uniqueIndexCount).toBe(0);
    expect(surface.plainIndexCount).toBe(1);
    expect(surface.indexNames).toEqual(['ix_lu_migration_history_applied_at']);
    expect(entry.expectedIndexNames).toEqual(['ix_lu_migration_history_applied_at']);
    expect(entry.targetTables).toEqual(['lu_migration_history']);
    const derived = deriveCanonicalBootstrapSql(
      entry,
      independentCanonicalBytes(raw).toString('utf8'),
    );
    expect(derived.createTableCount).toBe(1);
    expect(derived.uniqueIndexCount).toBe(0);
    expect(derived.plainIndexCount).toBe(1);
  });

  it('validatePinnedMigration accepts the raw and canonical buffers and reports consistent lengths', () => {
    const entry = findRegistryEntryByRelativePath('tenant/0013_migration_history.sql')!;
    const { plan } = validatePinnedMigration(TN0013_PATH, raw, { stream: 'tenant' });
    expect(plan.sha256).toBe(entry.sha256);
    expect(plan.byteLength).toBe(entry.byteLength);
    expect(plan.rawByteLength).toBe(raw.length);
    expect(() =>
      validatePinnedMigration(TN0013_PATH, independentCanonicalBytes(raw), { stream: 'tenant' }),
    ).not.toThrow();
  });
});

describe('migration-registry W11: ACL-only pairing (manifest XOR supplemental, never both null)', () => {
  it('CP 0005 ships manifest=null with a non-null supplemental (ACL-only)', () => {
    const entry = CONTROL_PLANE_REGISTRY.find(
      (e) => e.relativePath === 'control-plane/0005_user_management.sql',
    )!;
    expect(runnerTesting.manifestForEntry(entry)).toBeNull();
    expect(CONTROL_PLANE_SUPPLEMENTAL_EXPECTATIONS[entry.relativePath]).toBeDefined();
    expect(entry.targetTables).toEqual([]);
    expect(entry.expectedCreateTableCount).toBe(0);
  });

  it('tenant 0006 ships schemaManifest=null with a non-null supplemental (ACL-only)', () => {
    const entry = TENANT_REGISTRY.find((e) => e.ordinal === 6)!;
    expect(TENANT_MIGRATION_MANIFESTS[6 as TenantOrdinal].schemaManifest).toBeNull();
    expect(TENANT_SUPPLEMENTAL_EXPECTATIONS[6 as TenantOrdinal]).toBeDefined();
    expect(runnerTesting.manifestForEntry(entry)).toBeNull();
    expect(entry.expectedIndexNames).toEqual([]);
  });

  it('no registered entry pairs manifest=null with supplemental=null', () => {
    const violations: string[] = [];
    for (const entry of CONTROL_PLANE_REGISTRY) {
      const manifest = runnerTesting.manifestForEntry(entry);
      const supplemental = CONTROL_PLANE_SUPPLEMENTAL_EXPECTATIONS[entry.relativePath];
      if (manifest === null && supplemental === undefined) {
        violations.push(entry.relativePath);
      }
    }
    for (const entry of TENANT_REGISTRY) {
      const ordinal = entry.ordinal as TenantOrdinal;
      const manifest = TENANT_MIGRATION_MANIFESTS[ordinal]?.schemaManifest ?? null;
      const supplemental = TENANT_SUPPLEMENTAL_EXPECTATIONS[ordinal];
      if (manifest === null && supplemental === undefined) {
        violations.push(entry.relativePath);
      }
    }
    expect(violations).toEqual([]);
  });
});

// ---------------------------------------------------------------------------
// MIG-001-F2-W13B — stream gate. `validatePinnedMigration` must reject when
// the caller passes `options.stream` that differs from the resolved
// registry entry's stream, BEFORE the SHA-256 / byte-length / analyzer
// stages and BEFORE any DB-touching helper can open a `pg.Client`. The
// error must be generic and identify the requested / expected stream and
// the file name only — never the digest, byte length, env var names, DSN,
// password, or reference.
//
// Two lookup paths are exercised (each entry is hit twice):
//   - exact relative path:  "control-plane/0006_mig001_users_identity.sql"
//   - legacy basename:      "0006_mig001_users_identity.sql"
//
// The legacy 0001 / 0002 / 0003 entries live at the migrations/ root with a
// unique basename, so a basename lookup of one of those files is also a
// control-plane entry — the cross-stream basename-collision branch is
// reserved for the test below that uses a basename alias.
// ---------------------------------------------------------------------------
describe('migration-registry W13B: validatePinnedMigration stream gate', () => {
  // Every (entry, wrongStream) pair we want to verify. The error must fire
  // regardless of whether the caller used the exact relative path or the
  // legacy basename fallback.
  const cp0006_BYTES = readFileSync(CP0006_PATH);
  const tn0013_BYTES = readFileSync(TN0013_PATH);
  // 0001 (legacy basename) lives at migrations/ root; its basename is
  // unique across streams so the basename fallback also resolves to the
  // control-plane entry.
  const cp0001_BYTES = readFileSync(MIGRATION_PATH);

  const matrix: ReadonlyArray<{
    label: string;
    bytes: Buffer;
    exactPath: string;
    basenamePath: string;
    expectedStream: 'control-plane' | 'tenant';
    requestedStream: 'control-plane' | 'tenant';
  }> = [
    {
      label: 'control-plane 0006 by exact relative path, requested=tenant',
      bytes: cp0006_BYTES,
      exactPath: CP0006_PATH,
      basenamePath: fileURLToPath(
        new URL('../migrations/0006_mig001_users_identity.sql', import.meta.url),
      ),
      expectedStream: 'control-plane',
      requestedStream: 'tenant',
    },
    {
      label: 'tenant 0013 by exact relative path, requested=control-plane',
      bytes: tn0013_BYTES,
      exactPath: TN0013_PATH,
      basenamePath: fileURLToPath(
        new URL('../migrations/0013_migration_history.sql', import.meta.url),
      ),
      expectedStream: 'tenant',
      requestedStream: 'control-plane',
    },
    {
      label: 'control-plane legacy 0001 by basename, requested=tenant',
      bytes: cp0001_BYTES,
      exactPath: MIGRATION_PATH,
      basenamePath: MIGRATION_PATH,
      expectedStream: 'control-plane',
      requestedStream: 'tenant',
    },
  ];

  for (const { label, bytes, exactPath, basenamePath, expectedStream, requestedStream } of matrix) {
    it(`rejects ${label}`, () => {
      const error = expectRegistryError(
        () => validatePinnedMigration(exactPath, bytes, { stream: requestedStream }),
        /is registered for stream .* but the caller requested stream .*\. Refusing to analyze or execute/i,
      );
      // The error identifies the public file name, the expected (registry)
      // stream and the requested stream. No digests, env-var names, DSN,
      // password or reference are leaked.
      const fileName = label.includes('0006')
        ? '0006_mig001_users_identity.sql'
        : label.includes('0013')
          ? '0013_migration_history.sql'
          : IDENTITY_MIGRATION_FILE;
      expect(error.message).toContain(fileName);
      expect(error.message).toContain(`"${expectedStream}"`);
      expect(error.message).toContain(`"${requestedStream}"`);
      expect(error.message).not.toMatch(/[0-9a-f]{40,}/i);
      expect(error.message).not.toMatch(/CONNECTION|DSN|PASSWORD|SECRET/i);
    });

    it(`rejects the same entry via basename fallback: ${label}`, () => {
      // Same scenario but the caller passes the basename path; the legacy
      // basename fallback inside validatePinnedMigration must still resolve
      // the same registered entry, so the stream gate fires identically.
      const error = expectRegistryError(
        () => validatePinnedMigration(basenamePath, bytes, { stream: requestedStream }),
        /is registered for stream .* but the caller requested stream/i,
      );
      expect(error.message).toContain(`"${expectedStream}"`);
      expect(error.message).toContain(`"${requestedStream}"`);
      expect(error.message).not.toMatch(/[0-9a-f]{40,}/i);
    });
  }

  it('a stream request that matches the registry entry is accepted (control-plane)', () => {
    // Positive control: matching stream must NOT trip the gate.
    expect(() =>
      validatePinnedMigration(CP0006_PATH, cp0006_BYTES, { stream: 'control-plane' }),
    ).not.toThrow();
  });

  it('a stream request that matches the registry entry is accepted (tenant)', () => {
    expect(() =>
      validatePinnedMigration(TN0013_PATH, tn0013_BYTES, { stream: 'tenant' }),
    ).not.toThrow();
  });

  it('omitting the stream option skips the gate and proceeds with hash/analyzer (control-plane)', () => {
    // Backward compatibility: callers that do NOT pass `options.stream`
    // (e.g. the `plan` command) must continue to work.
    expect(() => validatePinnedMigration(CP0006_PATH, cp0006_BYTES)).not.toThrow();
  });

  it('stream gate runs BEFORE the hash check (SHA mismatch must not leak into the stream error)', () => {
    // Flip one byte of 0006 then call validatePinnedMigration with the
    // WRONG stream. The stream gate must fire first and the error must
    // NOT contain a SHA-256 digest (which would leak from the later hash
    // stage).
    const tampered = Buffer.from(cp0006_BYTES);
    const at = tampered.indexOf('lu_user');
    tampered[at] = tampered[at] === 0x6c ? 0x4c : 0x6c;
    const error = expectRegistryError(
      () => validatePinnedMigration(CP0006_PATH, tampered, { stream: 'tenant' }),
      /is registered for stream .* but the caller requested stream/i,
    );
    expect(error.message).not.toMatch(/[0-9a-f]{40,}/i);
    expect(error.message).not.toMatch(/SHA-256/);
  });

  it('stream gate runs BEFORE the byte-length check (length mismatch must not leak)', () => {
    const bloated = Buffer.concat([cp0006_BYTES, Buffer.from('\n')]);
    const error = expectRegistryError(
      () => validatePinnedMigration(CP0006_PATH, bloated, { stream: 'tenant' }),
      /is registered for stream .* but the caller requested stream/i,
    );
    expect(error.message).not.toMatch(/Byte length/);
    expect(error.message).not.toMatch(/[0-9a-f]{40,}/i);
  });
});
