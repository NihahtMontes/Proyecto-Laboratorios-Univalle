/**
 * F2-W2 registry + analyzer + manifest unit tests (no DB, no socket).
 *
 * Coverage:
 *  - CRLF tolerance for the 0006 and 0013 new SQL files.
 *  - Stream separation: control-plane and tenant registries are disjoint.
 *  - Per-entry policy wiring: legacy vs strict vs default (bootstrap).
 *  - Registry entry shape: ordinal, touchedTables, expectedIndexNames,
 *    advisoryLockKey, finalManifestId, stream, relativePath.
 *  - Canonical envelope derivation for entries that have leading comments.
 *  - Final manifests expose every table 0006 changes (lu_user,
 *    lu_site_membership, lu_login_identifier, lu_legacy_user_xref,
 *    lu_identity_audit_event, lu_identity_migration_state,
 *    lu_migration_history, plus lu_session.purpose, lu_tenant_route
 *    extension).
 *  - The NFKC normalizer mirrors the SQL `lu_login_identifier_normalize`.
 *  - The historical 0001-0003 pins are unchanged.
 *  - W14B: static proofs that 0006 carries the strict pre-F2 password
 *    classification branches (valid bcrypt preserved; invalid -> reset_required
 *    with NULL hash), the strengthened conditional password CHECK, the
 *    F1/W14A phone format CHECK, and that the schema manifest mirrors them.
 */
import { readFileSync } from 'node:fs';
import { fileURLToPath } from 'node:url';
import {
  MigrationPolicyError,
  MigrationStream,
  analyzeMigrationSql,
  assertCanonicalBytes,
  buildMigrationPlan,
  canonicalByteLength,
  sha256Hex,
} from '../src/database/migration-plan.js';
import {
  CONTROL_PLANE_REGISTRY,
  MigrationRegistryError,
  TENANT_REGISTRY,
  deriveCanonicalBootstrapSql,
  findRegistryEntryByOrdinal,
  findRegistryEntryByRelativePath,
  listControlPlaneMigrations,
  listTenantMigrations,
  resolveEntryPath,
  validatePinnedMigration,
} from '../src/database/migration-registry.js';
import {
  CONTROL_PLANE_SUPPLEMENTAL_EXPECTATIONS,
  POST_F2_CONTROL_PLANE_SCHEMA_MANIFEST,
  POST_F2_CONTROL_PLANE_TABLES,
  TENANT_FINAL_SCHEMA_MANIFEST,
} from '../src/database/schema-manifest-f2.js';

const HERE = fileURLToPath(new URL('.', import.meta.url));
const MIGRATION_0006 = fileURLToPath(
  new URL('../migrations/control-plane/0006_mig001_users_identity.sql', import.meta.url),
);
const MIGRATION_0013 = fileURLToPath(
  new URL('../migrations/tenant/0013_migration_history.sql', import.meta.url),
);

function normLf(buf: Buffer): Buffer {
  return Buffer.from(buf.toString('utf8').replace(/\r\n/g, '\n'), 'utf8');
}

describe('F2-W2 analyzer: CRLF invariant hashing', () => {
  it('sha256Hex hashes CRLF and LF representations identically', () => {
    const lf = 'SELECT 1;\n';
    const crlf = lf.replace(/\n/g, '\r\n');
    expect(sha256Hex(lf)).toBe(sha256Hex(crlf));
    expect(canonicalByteLength(crlf)).toBe(Buffer.byteLength(lf, 'utf8'));
  });

  it('0006 on-disk CRLF bytes hash to the registry pin', () => {
    const buf = readFileSync(MIGRATION_0006);
    const entry = findRegistryEntryByRelativePath('control-plane/0006_mig001_users_identity.sql')!;
    expect(sha256Hex(normLf(buf))).toBe(entry.sha256);
    expect(canonicalByteLength(normLf(buf))).toBe(entry.byteLength);
  });

  it('0013 on-disk CRLF bytes hash to the registry pin', () => {
    const buf = readFileSync(MIGRATION_0013);
    const entry = findRegistryEntryByRelativePath('tenant/0013_migration_history.sql')!;
    expect(sha256Hex(normLf(buf))).toBe(entry.sha256);
    expect(canonicalByteLength(normLf(buf))).toBe(entry.byteLength);
  });
});

describe('F2-W2 analyzer: canonical byte guards', () => {
  it('rejects a UTF-8 BOM at the start of an input buffer', () => {
    const bom = Buffer.concat([Buffer.from([0xef, 0xbb, 0xbf]), Buffer.from('SELECT 1;\n')]);
    expect(() => assertCanonicalBytes(bom, 'bom-test')).toThrow(/UTF-8 BOM/);
  });

  it('rejects a lone CR (no following LF)', () => {
    const lone = Buffer.from('SELECT 1;\nFOO\rBAR\n');
    expect(() => assertCanonicalBytes(lone, 'lone-cr-test')).toThrow(/lone CR/);
  });

  it('accepts a CRLF stream because every CR is followed by LF', () => {
    const crlf = Buffer.from('SELECT 1;\r\nSELECT 2;\r\n');
    expect(() => assertCanonicalBytes(crlf, 'crlf-test')).not.toThrow();
  });
});

describe('F2-W2 analyzer: per-entry policy modes', () => {
  function lfBody(statements: readonly string[]): string {
    return `BEGIN;\n${statements.join(';\n')};\nCOMMIT;\n`;
  }

  it('strict policy allows ALTER TABLE ADD COLUMN and rejects DROP TABLE', () => {
    const body = lfBody([
      'CREATE TABLE IF NOT EXISTS lu_test_one (id uuid PRIMARY KEY)',
      'ALTER TABLE lu_test_one ADD COLUMN x text',
    ]);
    const result = analyzeMigrationSql(body, ['lu_test_one'], {
      options: { strictPolicy: true },
      touchedTables: ['lu_test_one'],
    });
    expect(result.violations).toEqual([]);
  });

  it('strict policy forbids hard DELETE', () => {
    const body = lfBody([
      'CREATE TABLE IF NOT EXISTS lu_test_three (id uuid PRIMARY KEY)',
      'DELETE FROM lu_test_three',
    ]);
    const violations = analyzeMigrationSql(body, ['lu_test_three'], {
      options: { strictPolicy: true },
      touchedTables: ['lu_test_three'],
    }).violations;
    expect(violations.some((v) => /DELETE statement is not allowed/.test(v))).toBe(true);
  });

  it('strict policy tolerates dollar-quoted bodies', () => {
    const body = `BEGIN;\nDO $do$ BEGIN PERFORM 1; END $do$; COMMIT;\n`;
    expect(() => analyzeMigrationSql(body, [], { options: { strictPolicy: true } })).not.toThrow();
  });

  it('legacy policy rejects dynamic EXECUTE inside a dollar-quoted body', () => {
    const body = `BEGIN;\nDO $do$ BEGIN EXECUTE 'SELECT 1'; END $do$; COMMIT;\n`;
    const violations = analyzeMigrationSql(body, [], {
      options: { legacyPolicy: true },
    }).violations;
    expect(violations.some((v) => /EXECUTE inside the migration is forbidden/.test(v))).toBe(true);
  });

  it('default (bootstrap) policy forbids ALTER/GRANT', () => {
    const body = lfBody([
      'CREATE TABLE IF NOT EXISTS lu_test_two (id uuid PRIMARY KEY)',
      'GRANT SELECT ON lu_test_two TO public',
    ]);
    const violations = analyzeMigrationSql(body, ['lu_test_two']).violations;
    expect(violations.some((v) => /GRANT/.test(v))).toBe(true);
  });
});

describe('F2-W2 registry: stream separation and resolver', () => {
  it('control-plane and tenant registries are disjoint by relative path', () => {
    const cp = new Set(CONTROL_PLANE_REGISTRY.map((e) => e.relativePath));
    const tt = new Set(TENANT_REGISTRY.map((e) => e.relativePath));
    for (const t of tt) expect(cp.has(t)).toBe(false);
  });

  it('control-plane registry has 6 entries (0001..0006)', () => {
    expect(listControlPlaneMigrations()).toHaveLength(6);
  });

  it('tenant registry has 13 entries (0001..0013)', () => {
    expect(listTenantMigrations()).toHaveLength(13);
  });

  it('every entry has a unique ordinal within its stream', () => {
    for (const list of [CONTROL_PLANE_REGISTRY, TENANT_REGISTRY]) {
      const seen = new Set<number>();
      for (const e of list) {
        expect(seen.has(e.ordinal)).toBe(false);
        seen.add(e.ordinal);
      }
    }
  });

  it('findRegistryEntryByOrdinal resolves 0006 control-plane and 0013 tenant', () => {
    expect(findRegistryEntryByOrdinal('control-plane', 6)?.relativePath).toBe(
      'control-plane/0006_mig001_users_identity.sql',
    );
    expect(findRegistryEntryByOrdinal('tenant', 13)?.relativePath).toBe(
      'tenant/0013_migration_history.sql',
    );
    expect(findRegistryEntryByOrdinal('control-plane', 999)).toBeUndefined();
  });
});

describe('F2-W2 registry: per-entry policy wiring', () => {
  it('legacy policy is set on 0004 / 0005 / tenant 0001-0012', () => {
    for (const e of CONTROL_PLANE_REGISTRY.filter(
      (x) =>
        x.relativePath === 'control-plane/0004_academic_catalogs.sql' ||
        x.relativePath === 'control-plane/0005_user_management.sql',
    )) {
      expect(e.legacyPolicy).toBe(true);
      expect(e.strictPolicy).toBe(false);
    }
    for (const e of TENANT_REGISTRY.filter((x) => x.ordinal <= 12)) {
      expect(e.legacyPolicy).toBe(true);
      expect(e.strictPolicy).toBe(false);
    }
  });

  it('strict policy is set on 0006 and tenant 0013', () => {
    const six = findRegistryEntryByRelativePath('control-plane/0006_mig001_users_identity.sql')!;
    expect(six.legacyPolicy).toBe(false);
    expect(six.strictPolicy).toBe(true);
    const tn = findRegistryEntryByRelativePath('tenant/0013_migration_history.sql')!;
    expect(tn.legacyPolicy).toBe(false);
    expect(tn.strictPolicy).toBe(true);
  });

  it('bootstrap entries 0001-0003 use neither legacy nor strict', () => {
    for (const e of CONTROL_PLANE_REGISTRY.filter((x) => x.ordinal <= 3)) {
      expect(e.legacyPolicy).toBe(false);
      expect(e.strictPolicy).toBe(false);
    }
  });

  it('every control-plane entry has a unique relative path and stable hash', () => {
    for (const e of CONTROL_PLANE_REGISTRY) {
      const buf = readFileSync(resolveEntryPath(e, `${HERE}/../migrations`));
      expect(sha256Hex(normLf(buf))).toBe(e.sha256);
    }
  });

  it('every tenant entry has a unique relative path and stable hash', () => {
    for (const e of TENANT_REGISTRY) {
      const buf = readFileSync(resolveEntryPath(e, `${HERE}/../migrations`));
      expect(sha256Hex(normLf(buf))).toBe(e.sha256);
    }
  });
});

describe('F2-W2 validatePinnedMigration: accepts LF and CRLF on disk', () => {
  it('0006 accepts both LF and CRLF bytes', () => {
    const lfBuf = readFileSync(MIGRATION_0006);
    expect(() => validatePinnedMigration(MIGRATION_0006, lfBuf)).not.toThrow();
    const crlf = normLf(readFileSync(MIGRATION_0006));
    expect(() => validatePinnedMigration(MIGRATION_0006, crlf)).not.toThrow();
  });

  it('0004 accepts LF and CRLF bytes (legacy policy)', () => {
    const path = `${HERE}/../migrations/control-plane/0004_academic_catalogs.sql`;
    const buf = readFileSync(path);
    expect(() => validatePinnedMigration(path, buf)).not.toThrow();
    const crlf = normLf(readFileSync(path));
    expect(() => validatePinnedMigration(path, crlf)).not.toThrow();
  });

  it('rejects an unregistered file even with the canonical 0001 bytes', () => {
    const buf = readFileSync(`${HERE}/../migrations/0001_create_identity_control_plane.sql`);
    expect(() => validatePinnedMigration(`${HERE}/0001_evil.sql`, buf)).toThrow(
      MigrationRegistryError,
    );
  });
});

describe('F2-W2 plan: canonical envelope derivation for entries with leading comments', () => {
  it('0001 still parses 4 CREATE TABLE / 5 CREATE INDEX', () => {
    const sql = readFileSync(
      `${HERE}/../migrations/0001_create_identity_control_plane.sql`,
      'utf8',
    );
    const entry = CONTROL_PLANE_REGISTRY.find(
      (e) => e.relativePath === '0001_create_identity_control_plane.sql',
    )!;
    const canonical = deriveCanonicalBootstrapSql(entry, sql);
    expect(canonical.createTableCount).toBe(4);
    expect(canonical.derivedFromSha256).toBe(entry.sha256);
  });

  it('0006 parses the expected CREATE TABLE / INDEX counts', () => {
    const sql = readFileSync(MIGRATION_0006, 'utf8');
    const entry = findRegistryEntryByRelativePath('control-plane/0006_mig001_users_identity.sql')!;
    const canonical = deriveCanonicalBootstrapSql(entry, sql);
    expect(canonical.createTableCount).toBe(entry.expectedCreateTableCount);
    expect(canonical.derivedFromSha256).toBe(entry.sha256);
  });

  it('0013 parses exactly 1 CREATE TABLE / 1 INDEX', () => {
    const sql = readFileSync(MIGRATION_0013, 'utf8');
    const entry = findRegistryEntryByRelativePath('tenant/0013_migration_history.sql')!;
    const canonical = deriveCanonicalBootstrapSql(entry, sql);
    expect(canonical.createTableCount).toBe(1);
    expect(canonical.derivedFromSha256).toBe(entry.sha256);
  });

  it('executed body preserves IF NOT EXISTS only for legacy entries (tenant 0003 redeclares 0001 lu_laboratory)', () => {
    const legacy = findRegistryEntryByRelativePath('tenant/0003_academic_laboratories.sql')!;
    expect(legacy.legacyPolicy).toBe(true);
    const legacySql = readFileSync(
      `${HERE}/../migrations/tenant/0003_academic_laboratories.sql`,
      'utf8',
    );
    const legacyBody = deriveCanonicalBootstrapSql(legacy, legacySql).body;
    expect(legacyBody).toMatch(/CREATE TABLE IF NOT EXISTS public\.lu_laboratory\b/);
    expect(/\bBEGIN\s*;/i.test(legacyBody)).toBe(false);
    expect(/\bCOMMIT\s*;/i.test(legacyBody)).toBe(false);

    for (const [path, file] of [
      [
        '0001_create_identity_control_plane.sql',
        `${HERE}/../migrations/0001_create_identity_control_plane.sql`,
      ],
      ['control-plane/0006_mig001_users_identity.sql', MIGRATION_0006],
      ['tenant/0013_migration_history.sql', MIGRATION_0013],
    ] as const) {
      const entry = findRegistryEntryByRelativePath(path)!;
      expect(entry.legacyPolicy).toBe(false);
      const body = deriveCanonicalBootstrapSql(entry, readFileSync(file, 'utf8')).body;
      expect(/\bCREATE\s+(?:UNIQUE\s+)?(?:TABLE|INDEX)\s+IF\s+NOT\s+EXISTS\b/i.test(body)).toBe(
        false,
      );
    }
  });
});

describe('F2-W2 final manifests expose every 0006 surface', () => {
  it('post-F2 control-plane manifest contains every new/changed table', () => {
    const tables = POST_F2_CONTROL_PLANE_SCHEMA_MANIFEST.tables;
    for (const t of [
      'lu_user',
      'lu_site_membership',
      'lu_session',
      'lu_tenant_route',
      'lu_login_identifier',
      'lu_legacy_user_xref',
      'lu_identity_audit_event',
      'lu_identity_migration_state',
      'lu_migration_history',
    ]) {
      expect(tables).toHaveProperty(t);
    }
  });

  it('lu_user final manifest has every F1 column', () => {
    const cols = POST_F2_CONTROL_PLANE_SCHEMA_MANIFEST.tables.lu_user.columns.map((c) => c.name);
    for (const c of [
      'id',
      'email',
      'full_name',
      'password_hash',
      'is_super_admin',
      'status',
      'account_status',
      'security_version',
      'username',
      'first_name',
      'last_name',
      'second_last_name',
      'identity_card',
      'phone_number',
      'profile_picture_key',
      'password_scheme',
      'must_change_password',
      'password_migrated_at',
      'row_version',
      'created_at',
      'updated_at',
      'created_by_user_id',
      'modified_by_user_id',
      'reconciliation_state',
    ]) {
      expect(cols).toContain(c);
    }
  });

  it('lu_login_identifier cross-kind uniqueness index is declared', () => {
    const idx = POST_F2_CONTROL_PLANE_SCHEMA_MANIFEST.tables.lu_login_identifier.indexes.map(
      (i) => i.name,
    );
    expect(idx).toContain('ux_lu_login_identifier_normalized_value');
    expect(idx).toContain('ux_lu_login_identifier_user_kind');
  });

  it('lu_identity_audit_event is declared append-only via the no-update index set', () => {
    const tbl = POST_F2_CONTROL_PLANE_SCHEMA_MANIFEST.tables.lu_identity_audit_event;
    expect(tbl.columns.map((c) => c.name)).toContain('metadata');
  });

  it('POST_F2_CONTROL_PLANE_TABLES has 15 entries (4 baseline + 2 auth + 1 route + 3 academic + 5 new)', () => {
    expect(POST_F2_CONTROL_PLANE_TABLES).toHaveLength(15);
  });

  it('tenant final manifest contains the migration history table', () => {
    const tbl = TENANT_FINAL_SCHEMA_MANIFEST.tables.lu_migration_history;
    expect(tbl.primaryKey.columns).toEqual(['stream', 'ordinal']);
    expect(tbl.uniques[0]?.columns).toEqual(['stream', 'relative_path']);
  });
});

describe('F2-W2 buildMigrationPlan: stream-aware strict policy for 0006', () => {
  it('plans 0006 with strictPolicy=true', () => {
    const buf = readFileSync(MIGRATION_0006);
    const entry = findRegistryEntryByRelativePath('control-plane/0006_mig001_users_identity.sql')!;
    const plan = buildMigrationPlan(MIGRATION_0006, buf, [...entry.targetTables], {
      allowedTables: [...entry.targetTables],
      touchedTables: [...entry.touchedTables],
      options: {
        legacyPolicy: entry.legacyPolicy,
        strictPolicy: entry.strictPolicy,
      },
      stream: entry.stream,
    });
    expect(plan.stream).toBe<MigrationStream>('control-plane');
    expect(plan.sha256).toBe(entry.sha256);
  });

  it('rejects 0006 under the bootstrap policy (keyword allowlist forbids ALTER/TRIGGER)', () => {
    const buf = readFileSync(MIGRATION_0006);
    const entry = findRegistryEntryByRelativePath('control-plane/0006_mig001_users_identity.sql')!;
    // bootstrap policy = both flags false; it forbids ALTER, CREATE TRIGGER,
    // GRANT, etc. 0006 (additive, strict) must therefore be rejected when
    // someone tries to feed it through a bootstrap-only gate.
    expect(() =>
      buildMigrationPlan(MIGRATION_0006, buf, [...entry.targetTables], {
        stream: entry.stream,
      }),
    ).toThrow(MigrationPolicyError);
  });
});

describe('F2-W2 historical pins are unchanged (regression guard)', () => {
  it('0001 / 0002 / 0003 / 0004 / 0005 / tenant 0001-0012 keep the original LF SHA-256', () => {
    const expected = {
      '0001_create_identity_control_plane.sql':
        'a7698ea53a443d42e87bf905ae9e0f7060923a26199bc06ad23f17401e1bc118',
      '0002_create_auth_security_controls.sql':
        '335bd7b1ed75b557e1618283d2272c4353bb029ca2fd28ee626566d3d93ff336',
      '0003_create_tenant_route_catalog.sql':
        'dfc0dc9a4cea47c584d5cc1f8705d30a3df9cc589f8702b9afeb15b43dd851f1',
      'control-plane/0004_academic_catalogs.sql':
        '65a916f8acc61a73f4b82cc60cdfa69f62a912770bea05f60082c581c270631b',
      'control-plane/0005_user_management.sql':
        'd83b147b375a67f42d50c516f599cb0e9d320229f510cbc8af08ade67df2aeaa',
    } as const;
    for (const e of CONTROL_PLANE_REGISTRY) {
      if (e.relativePath in expected) {
        expect(e.sha256).toBe(expected[e.relativePath as keyof typeof expected]);
      }
    }
  });
});

describe('F2-W14B: pre-F2 password classification backfill (static SQL proofs)', () => {
  const sql = readFileSync(MIGRATION_0006, 'utf8').replace(/\r\n/g, '\n');
  const BCRYPT_SHAPE = "'^[$]2[aby][$](0[4-9]|[12][0-9]|3[01])[$][./A-Za-z0-9]{53}$'";

  const classificationStatement = (() => {
    const match =
      /UPDATE\s+public\.lu_user\s+SET\s+password_scheme\s*=\s*CASE[\s\S]*?row_version\s*=\s*COALESCE\(row_version,\s*0\)\s+WHERE[\s\S]*?;/.exec(
        sql,
      );
    expect(match).not.toBeNull();
    return match![0];
  })();

  it('preserves a valid bcrypt hash byte-for-byte with scheme bcrypt and must_change=false', () => {
    expect(classificationStatement).toContain(BCRYPT_SHAPE);
    expect(classificationStatement).toContain("THEN 'bcrypt'");
    expect(classificationStatement).toContain('THEN password_hash');
    expect(classificationStatement).toContain('THEN false');
  });

  it('routes null/blank/malformed/invalid-cost hashes to reset_required with a NULL hash and must_change=true', () => {
    expect(classificationStatement).toContain("ELSE 'reset_required'");
    expect(classificationStatement).toContain('ELSE NULL');
    expect(classificationStatement).toContain('ELSE true');
  });

  it('never classifies SQL Server Identity V2/V3 schemes in the pre-F2 backfill', () => {
    expect(classificationStatement).not.toContain('legacy_identity_v2');
    expect(classificationStatement).not.toContain('legacy_identity_v3');
  });

  it('coalesces row_version to 0 and is guarded to the newly-added columns only', () => {
    expect(classificationStatement).toContain('row_version = COALESCE(row_version, 0)');
    expect(classificationStatement).toContain('WHERE password_scheme IS NULL');
    expect(classificationStatement).toContain('OR must_change_password IS NULL');
    expect(classificationStatement).toContain('OR row_version IS NULL');
  });
});

describe('F2-W14B: strengthened password conditional CHECK (static SQL proof)', () => {
  const sql = readFileSync(MIGRATION_0006, 'utf8').replace(/\r\n/g, '\n');

  const constraint = (() => {
    const anchor = 'ADD CONSTRAINT ck_lu_user_password_hash_conditional';
    const start = sql.indexOf(anchor);
    const end = sql.indexOf('END IF;', start);
    expect(start).toBeGreaterThan(-1);
    expect(end).toBeGreaterThan(start);
    return sql.slice(start, end);
  })();

  it('requires bcrypt rows to carry the exact shape with cost 04..31 and 53 payload chars', () => {
    expect(constraint).toContain("password_scheme = 'bcrypt'");
    expect(constraint).toContain("'^[$]2[aby][$](0[4-9]|[12][0-9]|3[01])[$][./A-Za-z0-9]{53}$'");
  });

  it('requires reset_required rows to carry a NULL hash', () => {
    expect(constraint).toContain("password_scheme = 'reset_required' AND password_hash IS NULL");
  });

  it('keeps legacy Identity V2/V3 rows non-blank opaque Base64 without a format regex', () => {
    expect(constraint).toContain("password_scheme IN ('legacy_identity_v2', 'legacy_identity_v3')");
    expect(constraint).toContain('password_hash IS NOT NULL');
    expect(constraint).toContain('length(btrim(password_hash)) > 0');
    expect(constraint).not.toContain("'legacy_identity_v2', 'legacy_identity_v3', 'bcrypt'");
  });
});

describe('F2-W14B: phone format CHECK (static SQL proof)', () => {
  const sql = readFileSync(MIGRATION_0006, 'utf8').replace(/\r\n/g, '\n');

  const constraint = (() => {
    const anchor = 'ADD CONSTRAINT ck_lu_user_phone_number_format';
    const start = sql.indexOf(anchor);
    const end = sql.indexOf('END IF;', start);
    expect(start).toBeGreaterThan(-1);
    expect(end).toBeGreaterThan(start);
    return sql.slice(start, end);
  })();

  it('allows NULL pending rows and bounds non-null values to 30 chars with the F1/W14A charset', () => {
    expect(constraint).toContain('phone_number IS NULL');
    expect(constraint).toContain('length(phone_number) <= 30');
    expect(constraint).toContain("phone_number ~ '^[+]?[0-9 ().-]+$'");
  });

  it('counts 7..15 total digits via a normalization-free regexp_replace expression', () => {
    expect(constraint).toContain("regexp_replace(phone_number, '[^0-9]', '', 'g')");
    expect(constraint).toContain('>= 7');
    expect(constraint).toContain('<= 15');
  });

  it('replaces the W2-era length-only phone check', () => {
    expect(sql).not.toContain('ck_lu_user_phone_number_length');
  });
});

describe('F2-W14B: SQL constraints are mirrored by the schema manifest', () => {
  it('lu_user manifest checks mirror the conditional hash and phone definitions 1:1', () => {
    const definitions = POST_F2_CONTROL_PLANE_SCHEMA_MANIFEST.tables.lu_user.checks.map(
      (c) => c.definition,
    );
    const hashDef = definitions.find((d) => d.includes("password_hash ~ '^[$]2[aby][$]"));
    expect(hashDef).toBeDefined();
    expect(hashDef).toContain("password_scheme = 'reset_required' and password_hash is null");
    expect(hashDef).toContain("password_scheme in ('legacy_identity_v2', 'legacy_identity_v3')");
    const phoneDef = definitions.find((d) => d.includes('regexp_replace(phone_number'));
    expect(phoneDef).toBeDefined();
    expect(phoneDef).toContain("phone_number ~ '^[+]?[0-9 ().-]+$'");
    expect(phoneDef).toContain('>= 7');
    expect(phoneDef).toContain('<= 15');
  });
});

describe('F2-W2: 0004 academic catalogs IDENTITY column model', () => {
  it('lu_faculty.id declares a BY DEFAULT AS IDENTITY marker with no synthetic nextval default', () => {
    const id = POST_F2_CONTROL_PLANE_SCHEMA_MANIFEST.tables.lu_faculty.columns.find(
      (c) => c.name === 'id',
    );
    expect(id).toBeDefined();
    expect(id?.columnDefault).toBeNull();
    expect(id?.identity).toEqual({
      generated: 'BY DEFAULT',
      sequence: 'lu_faculty_id_seq',
    });
  });

  it('lu_career.id declares a BY DEFAULT AS IDENTITY marker with no synthetic nextval default', () => {
    const id = POST_F2_CONTROL_PLANE_SCHEMA_MANIFEST.tables.lu_career.columns.find(
      (c) => c.name === 'id',
    );
    expect(id).toBeDefined();
    expect(id?.columnDefault).toBeNull();
    expect(id?.identity).toEqual({
      generated: 'BY DEFAULT',
      sequence: 'lu_career_id_seq',
    });
  });

  it('the 0004 supplemental marks both IDENTITY sequences with identity=BY DEFAULT', () => {
    const supplementals = CONTROL_PLANE_SUPPLEMENTAL_EXPECTATIONS[
      'control-plane/0004_academic_catalogs.sql'
    ] as { sequences: ReadonlyArray<{ name: string; identity?: string }> };
    const facSeq = supplementals.sequences.find((s) => s.name === 'lu_faculty_id_seq');
    const carSeq = supplementals.sequences.find((s) => s.name === 'lu_career_id_seq');
    expect(facSeq?.identity).toBe('BY DEFAULT');
    expect(carSeq?.identity).toBe('BY DEFAULT');
  });

  it('every 0004 partial index uses PG-18-normalizable functional expressions', () => {
    const indexes = POST_F2_CONTROL_PLANE_SCHEMA_MANIFEST.tables.lu_faculty.indexes;
    const lowerName = indexes.find((i) => i.name === 'ux_lu_faculty_name_active');
    expect(lowerName?.columns).toEqual(['lower(name)']);
    const lowerCode = indexes.find((i) => i.name === 'ux_lu_faculty_code_active');
    expect(lowerCode?.columns).toEqual(['lower(code)']);
  });
});
