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
import { readFileSync } from 'node:fs';
import { fileURLToPath } from 'node:url';
import {
  AUTH_SECURITY_TARGET_TABLES,
  IDENTITY_MIGRATION_FILE,
  TARGET_TABLES,
  TENANT_ROUTE_TARGET_TABLES,
  buildMigrationPlan,
  sha256Hex,
} from '../src/database/migration-plan.js';
import {
  MIGRATION_REGISTRY,
  MigrationRegistryError,
  findRegistryEntry,
  listRegisteredMigrations,
  validatePinnedMigration,
} from '../src/database/migration-registry.js';

const MIGRATION_PATH = fileURLToPath(
  new URL(`../migrations/${IDENTITY_MIGRATION_FILE}`, import.meta.url),
);
const REAL_BYTES = readFileSync(MIGRATION_PATH);
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

  it('0001 on disk MATCHES the pinned hash a7698e...bc118 and 4491 bytes', () => {
    const entry = MIGRATION_REGISTRY[0];
    expect(entry.sha256).toBe(APPROVED_SHA256);
    expect(entry.byteLength).toBe(APPROVED_BYTE_LENGTH);
    expect(REAL_BYTES.length).toBe(APPROVED_BYTE_LENGTH);
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
    const entry = MIGRATION_REGISTRY[2];
    expect(entry.file).toBe(TENANT_ROUTE_FILE);
    expect(entry.style).toBe('additive');
    expect(entry.workId).toBe('MIG-F4-ROUTE-CATALOG-008');
    expect(entry.sha256).toBe(TENANT_ROUTE_SHA256);
    expect(entry.byteLength).toBe(TENANT_ROUTE_BYTE_LENGTH);
    expect(TENANT_ROUTE_BYTES).toHaveLength(TENANT_ROUTE_BYTE_LENGTH);
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
    const entry = MIGRATION_REGISTRY[1];
    expect(entry.file).toBe(AUTH_SECURITY_FILE);
    expect(entry.style).toBe('additive');
    expect(entry.workId).toBe('MIG-F3-AUTH-SECURITY-014');
    expect(entry.sha256).toBe(AUTH_SECURITY_SHA256);
    expect(entry.byteLength).toBe(AUTH_SECURITY_BYTE_LENGTH);
    expect(AUTH_SECURITY_BYTES).toHaveLength(AUTH_SECURITY_BYTE_LENGTH);
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
    expect(tampered.length).toBe(APPROVED_BYTE_LENGTH);
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
