/**
 * MIG-001-F2-W8 — exact, executable PostgreSQL schema manifests for the
 * control-plane stream (0004 academic catalogs + cumulative 0001..0006) and
 * the narrowly-typed supplemental expectation model for surfaces the
 * `SchemaManifest` model cannot express (sequences, ACLs, triggers).
 *
 * Everything here is transcribed 1:1 from the current on-disk SQL:
 *   - control-plane/0004_academic_catalogs.sql (immutable historical)
 *   - control-plane/0005_user_management.sql (immutable historical, ACL-only)
 *   - control-plane/0006_mig001_users_identity.sql (CURRENT W6 additive contract)
 * and composed with the immutable baseline/auth/route manifests exported by
 * schema-manifest.ts (0001..0003), which are reused verbatim for the tables
 * 0006 did not alter.
 *
 * `verifySchema` (schema-manifest.ts) is exact and bidirectional: every
 * column, default, PK, UNIQUE, FK, CHECK and index on each listed table must
 * match the live catalog, and any extra object fails. Consequently no
 * TableManifest here is a placeholder: every spec is complete and every
 * string is produced with the same `normalizeCheckDefinition` /
 * `normalizeDefault` normalizers the verifier applies to
 * pg_get_constraintdef / information_schema output.
 *
 * Deliberately NOT modeled inside `SchemaManifest` (the model cannot express
 * them; no fake table specs are invented):
 *   - identity/owned sequences  -> SupplementalExpectation.sequences
 *   - table/column privileges   -> SupplementalExpectation.tableAcls
 *   - sequence privileges       -> SupplementalExpectation.sequenceAcls
 *   - function EXECUTE rights   -> SupplementalExpectation.functionAcls
 *   - triggers                  -> SupplementalExpectation.triggers
 * 0005 is ACL-only: it deliberately has NO SchemaManifest. Runner W9 verifies
 * 0005 against CONTROL_PLANE_0005_SUPPLEMENTAL instead.
 *
 * Catalog validation against a real PostgreSQL instance is the runner's job;
 * the two fail-closed CHECK strings marked "DEPARSE-PINNED" below are written
 * to the exact pg_get_constraintdef shape and must be re-confirmed on the
 * first real PG run (see the handoff limitations).
 */
import type {
  CheckSpec,
  ColumnSpec,
  ForeignKeySpec,
  IndexSpec,
  SchemaManifest,
  TableManifest,
} from './schema-manifest.js';
import {
  AUTH_SECURITY_SCHEMA_MANIFEST,
  IDENTITY_CONTROL_PLANE_SCHEMA_MANIFEST,
  TENANT_ROUTE_SCHEMA_MANIFEST,
  normalizeCheckDefinition,
  normalizeDefault,
} from './schema-manifest.js';

// Tenant-stream manifests live in schema-manifest-tenant.ts; the runner and
// tests import them from this module, so they are re-exported here.
export {
  TENANT_FINAL_SCHEMA_MANIFEST,
  TENANT_FINAL_TABLES,
  TENANT_MIGRATION_MANIFESTS,
  TENANT_SUPPLEMENTAL_EXPECTATIONS,
} from './schema-manifest-tenant.js';
export type {
  TenantFinalTable,
  TenantMigrationManifest,
  TenantOrdinal,
} from './schema-manifest-tenant.js';

// ---------------------------------------------------------------------------
// Column factories (same conventions as schema-manifest.ts expected values)
// ---------------------------------------------------------------------------

const timestampDefault = normalizeDefault('CURRENT_TIMESTAMP');

const uuidNotNull = (name: string): ColumnSpec => ({
  name,
  dataType: 'uuid',
  udtName: 'uuid',
  charMaxLength: null,
  isNullable: false,
  columnDefault: null,
});

const uuidNullable = (name: string): ColumnSpec => ({
  name,
  dataType: 'uuid',
  udtName: 'uuid',
  charMaxLength: null,
  isNullable: true,
  columnDefault: null,
});

const textColumn = (name: string, isNullable: boolean, dflt: string | null): ColumnSpec => ({
  name,
  dataType: 'text',
  udtName: 'text',
  charMaxLength: null,
  isNullable,
  columnDefault: dflt,
});

const varchar = (
  name: string,
  length: number,
  isNullable: boolean,
  dflt: string | null,
): ColumnSpec => ({
  name,
  dataType: 'character varying',
  udtName: 'varchar',
  charMaxLength: length,
  isNullable,
  columnDefault: dflt,
});

const bool = (name: string, isNullable: boolean, dflt: string | null): ColumnSpec => ({
  name,
  dataType: 'boolean',
  udtName: 'bool',
  charMaxLength: null,
  isNullable,
  columnDefault: dflt,
});

const timestamptz = (name: string, isNullable: boolean, dflt: string | null): ColumnSpec => ({
  name,
  dataType: 'timestamp with time zone',
  udtName: 'timestamptz',
  charMaxLength: null,
  isNullable,
  columnDefault: dflt,
});

const bigint = (name: string, isNullable: boolean, dflt: string | null): ColumnSpec => ({
  name,
  dataType: 'bigint',
  udtName: 'int8',
  charMaxLength: null,
  isNullable,
  columnDefault: dflt,
});

const int4 = (name: string, isNullable: boolean, dflt: string | null): ColumnSpec => ({
  name,
  dataType: 'integer',
  udtName: 'int4',
  charMaxLength: null,
  isNullable,
  columnDefault: dflt,
});

const smallint = (name: string, isNullable: boolean, dflt: string | null): ColumnSpec => ({
  name,
  dataType: 'smallint',
  udtName: 'int2',
  charMaxLength: null,
  isNullable,
  columnDefault: dflt,
});

const char = (
  name: string,
  length: number,
  isNullable: boolean,
  dflt: string | null,
): ColumnSpec => ({
  name,
  dataType: 'character',
  udtName: 'bpchar',
  charMaxLength: length,
  isNullable,
  columnDefault: dflt,
});

/**
 * `id bigint GENERATED BY DEFAULT AS IDENTITY PRIMARY KEY` column. PG-18
 * omits the nextval default from `information_schema.columns.column_default`
 * for IDENTITY columns (it lives on `pg_attribute.attidentity='d'`), so the
 * verifier accepts `columnDefault = null` when `identity.generated='BY DEFAULT'`
 * is set. The owning sequence is also recorded on the supplemental
 * expectation so `verifySupplementalCatalog` accepts `pg_depend.deptype='i'`
 * for the identity link (in addition to `'a'` for legacy serial-style auto
 * ownership) and additionally asserts `attidentity='d'` on the column.
 */
const identityBigint = (seqName: string): ColumnSpec => ({
  name: 'id',
  dataType: 'bigint',
  udtName: 'int8',
  charMaxLength: null,
  isNullable: false,
  columnDefault: null,
  identity: { generated: 'BY DEFAULT', sequence: seqName },
});

function checks(...definitions: readonly string[]): readonly CheckSpec[] {
  return definitions.map((definition) => ({ definition: normalizeCheckDefinition(definition) }));
}

function pkIndex(table: string, columns: readonly string[] = ['id']): IndexSpec {
  return { name: `${table}_pkey`, table, columns, unique: true, where: null };
}

// ---------------------------------------------------------------------------
// 0004 — academic catalogs (lu_faculty, lu_career, lu_site_career)
// ---------------------------------------------------------------------------

export type AcademicCatalogTable = 'lu_faculty' | 'lu_career' | 'lu_site_career';

export const ACADEMIC_CATALOG_SCHEMA_TABLES: readonly AcademicCatalogTable[] = [
  'lu_faculty',
  'lu_career',
  'lu_site_career',
];

const luFacultyColumns: readonly ColumnSpec[] = [
  identityBigint('lu_faculty_id_seq'),
  varchar('name', 200, false, null),
  varchar('code', 50, true, null),
  varchar('description', 500, true, null),
  int4('status', false, normalizeDefault('0')),
  uuidNullable('created_by_id'),
  timestamptz('created_at', false, timestampDefault),
  uuidNullable('updated_by_id'),
  timestamptz('updated_at', true, null),
];

const luFacultyChecks: readonly CheckSpec[] = checks('status IN (0, 1, 2)');

const luFacultyIndexes: readonly IndexSpec[] = [
  pkIndex('lu_faculty'),
  {
    name: 'ux_lu_faculty_name_active',
    table: 'lu_faculty',
    columns: ['lower(name)'],
    unique: true,
    where: normalizeCheckDefinition('status <> 2'),
  },
  {
    name: 'ux_lu_faculty_code_active',
    table: 'lu_faculty',
    columns: ['lower(code)'],
    unique: true,
    where: normalizeCheckDefinition('code IS NOT NULL AND status <> 2'),
  },
];

const luCareerColumns: readonly ColumnSpec[] = [
  identityBigint('lu_career_id_seq'),
  varchar('code', 30, true, null),
  varchar('name', 200, false, null),
  bigint('faculty_id', true, null),
  int4('status', false, normalizeDefault('0')),
  uuidNullable('created_by_id'),
  timestamptz('created_at', false, timestampDefault),
  uuidNullable('updated_by_id'),
  timestamptz('updated_at', true, null),
];

const luCareerChecks: readonly CheckSpec[] = checks('status IN (0, 1, 2)');

const luCareerForeignKeys: readonly ForeignKeySpec[] = [
  {
    columns: ['faculty_id'],
    referencesTable: 'lu_faculty',
    referencesColumns: ['id'],
    deleteAction: 'RESTRICT',
  },
];

const luCareerIndexes: readonly IndexSpec[] = [
  pkIndex('lu_career'),
  {
    name: 'ux_lu_career_name_active',
    table: 'lu_career',
    columns: ['lower(name)'],
    unique: true,
    where: normalizeCheckDefinition('status <> 2'),
  },
  {
    name: 'ux_lu_career_code_active',
    table: 'lu_career',
    columns: ['lower(code)'],
    unique: true,
    where: normalizeCheckDefinition('code IS NOT NULL AND status <> 2'),
  },
];

const luSiteCareerColumns: readonly ColumnSpec[] = [
  uuidNotNull('site_id'),
  bigint('career_id', false, null),
  int4('status', false, normalizeDefault('0')),
  uuidNullable('created_by_id'),
  timestamptz('created_at', false, timestampDefault),
  uuidNullable('updated_by_id'),
  timestamptz('updated_at', true, null),
];

const luSiteCareerChecks: readonly CheckSpec[] = checks('status IN (0, 1, 2)');

const luSiteCareerForeignKeys: readonly ForeignKeySpec[] = [
  {
    columns: ['site_id'],
    referencesTable: 'lu_site',
    referencesColumns: ['id'],
    deleteAction: 'RESTRICT',
  },
  {
    columns: ['career_id'],
    referencesTable: 'lu_career',
    referencesColumns: ['id'],
    deleteAction: 'RESTRICT',
  },
];

/**
 * Exact postcondition manifest for control-plane 0004 (academic catalogs).
 * Includes the identity defaults/types (nextval(...)::regclass), status
 * CHECKs, PKs, FKs, the generated PK indexes and the four explicit partial
 * unique indexes. The owned sequences and the conditional runtime grants are
 * exposed via CONTROL_PLANE_0004_SUPPLEMENTAL (not representable here).
 */
export const ACADEMIC_CATALOG_SCHEMA_MANIFEST: SchemaManifest<AcademicCatalogTable> = {
  schema: 'public',
  tables: {
    lu_faculty: {
      columns: luFacultyColumns,
      primaryKey: { columns: ['id'] },
      uniques: [],
      foreignKeys: [],
      checks: luFacultyChecks,
      indexes: luFacultyIndexes,
    },
    lu_career: {
      columns: luCareerColumns,
      primaryKey: { columns: ['id'] },
      uniques: [],
      foreignKeys: luCareerForeignKeys,
      checks: luCareerChecks,
      indexes: luCareerIndexes,
    },
    lu_site_career: {
      columns: luSiteCareerColumns,
      primaryKey: { columns: ['site_id', 'career_id'] },
      uniques: [],
      foreignKeys: luSiteCareerForeignKeys,
      checks: luSiteCareerChecks,
      indexes: [pkIndex('lu_site_career', ['site_id', 'career_id'])],
    },
  },
};

// ---------------------------------------------------------------------------
// 0006-altered tables — complete final (post-0006) specs including every
// inherited baseline column/check/FK/index plus the 0006 additions.
// ---------------------------------------------------------------------------

/** lu_user after 0006: account_status authority, three-state status shadow,
 * canonical reconciliation fields, password state machine, actor FKs. */
export const luUserFinalTable: TableManifest = {
  columns: [
    uuidNotNull('id'),
    textColumn('email', false, null),
    textColumn('full_name', false, null),
    // 0006 drops the baseline non-blank hash CHECK and makes the column
    // nullable: only reset_required may carry a NULL hash.
    textColumn('password_hash', true, null),
    bool('is_super_admin', false, normalizeDefault('false')),
    textColumn('status', false, normalizeDefault("'active'::text")),
    textColumn('account_status', false, normalizeDefault("'active'::text")),
    bigint('security_version', false, normalizeDefault('0')),
    varchar('username', 256, true, null),
    varchar('first_name', 100, true, null),
    varchar('last_name', 100, true, null),
    varchar('second_last_name', 100, true, null),
    varchar('identity_card', 10, true, null),
    varchar('phone_number', 30, true, null),
    varchar('profile_picture_key', 512, true, null),
    textColumn('password_scheme', false, normalizeDefault("'bcrypt'::text")),
    bool('must_change_password', false, normalizeDefault('true')),
    timestamptz('password_migrated_at', true, null),
    bigint('row_version', false, normalizeDefault('0')),
    timestamptz('created_at', false, timestampDefault),
    timestamptz('updated_at', false, timestampDefault),
    uuidNullable('created_by_user_id'),
    uuidNullable('modified_by_user_id'),
    textColumn('reconciliation_state', false, normalizeDefault("'canonical'::text")),
  ],
  primaryKey: { columns: ['id'] },
  uniques: [],
  foreignKeys: [
    {
      columns: ['created_by_user_id'],
      referencesTable: 'lu_user',
      referencesColumns: ['id'],
      deleteAction: 'RESTRICT',
    },
    {
      columns: ['modified_by_user_id'],
      referencesTable: 'lu_user',
      referencesColumns: ['id'],
      deleteAction: 'RESTRICT',
    },
  ],
  checks: checks(
    // Inherited 0001 checks (0001's status and password_hash checks were
    // dropped by 0006 and are therefore absent).
    "btrim(email) <> ''",
    "btrim(full_name) <> ''",
    'security_version >= 0',
    'updated_at >= created_at',
    // 0006 step 4: password state machine (W16B conditional hash CHECK:
    // bcrypt rows must carry the exact $2a/$2b/$2y shape with cost 04..31 and
    // 53 payload chars from the FULL bcrypt alphabet [./A-Za-z0-9];
    // reset_required requires NULL; legacy Identity V2/V3 rows keep a nonblank
    // opaque Base64 hash because the runtime parser is the authority).
    "password_scheme IN ('legacy_identity_v2', 'legacy_identity_v3', 'bcrypt', 'reset_required')",
    "(password_scheme = 'bcrypt' AND password_hash ~ '^[$]2[aby][$](0[4-9]|[12][0-9]|3[01])[$][./A-Za-z0-9]{53}$') OR (password_scheme = 'reset_required' AND password_hash IS NULL) OR (password_scheme IN ('legacy_identity_v2', 'legacy_identity_v3') AND password_hash IS NOT NULL AND length(btrim(password_hash)) > 0)",
    "(password_scheme = 'reset_required' AND must_change_password = true) OR (password_scheme IN ('legacy_identity_v2', 'legacy_identity_v3') AND must_change_password = false) OR (password_scheme = 'bcrypt' AND must_change_password IN (true, false))",
    // 0006 step 5: length + format + canonical-required checks.
    'username IS NULL OR length(username) <= 256',
    'length(email) <= 256',
    'first_name IS NULL OR length(first_name) <= 100',
    'last_name IS NULL OR length(last_name) <= 100',
    'second_last_name IS NULL OR length(second_last_name) <= 100',
    "identity_card IS NULL OR identity_card ~ '^[0-9A-Z-]{1,10}$'",
    // 0006 step 5 (W14B): F1/W14A phone syntax replaces the length-only
    // check. NULL is allowed while pending; non-null values are max 30 chars
    // with one optional leading +, the stated charset and 7..15 total digits.
    "phone_number IS NULL OR (length(phone_number) <= 30 AND phone_number ~ '^[+]?[0-9 ().-]+$' AND length(regexp_replace(phone_number, '[^0-9]', '', 'g')) >= 7 AND length(regexp_replace(phone_number, '[^0-9]', '', 'g')) <= 15)",
    'profile_picture_key IS NULL OR length(profile_picture_key) <= 512',
    'row_version >= 0',
    "reconciliation_state IN ('pending_reconciliation', 'canonical')",
    "(reconciliation_state = 'pending_reconciliation') OR (reconciliation_state = 'canonical' AND username IS NOT NULL AND length(btrim(username)) > 0 AND email IS NOT NULL AND length(btrim(email)) > 0 AND first_name IS NOT NULL AND length(btrim(first_name)) > 0 AND last_name IS NOT NULL AND length(btrim(last_name)) > 0 AND identity_card IS NOT NULL AND length(btrim(identity_card)) > 0 AND phone_number IS NOT NULL AND length(btrim(phone_number)) > 0)",
    // 0006 step 7: F1 authority + shadow.
    "account_status IN ('active', 'inactive', 'deleted')",
    "status IN ('active', 'inactive', 'deleted')",
  ),
  indexes: [
    pkIndex('lu_user'),
    {
      name: 'ux_lu_user_email_lower',
      table: 'lu_user',
      columns: ['lower(email)'],
      unique: true,
      where: null,
    },
    {
      name: 'ux_lu_user_username_ci',
      table: 'lu_user',
      columns: ['lower(username)'],
      unique: true,
      where: normalizeCheckDefinition('username IS NOT NULL'),
    },
    {
      name: 'ux_lu_user_identity_card_ci',
      table: 'lu_user',
      columns: ['upper(identity_card)'],
      unique: true,
      where: normalizeCheckDefinition('identity_card IS NOT NULL'),
    },
    {
      name: 'ix_lu_user_account_status',
      table: 'lu_user',
      columns: ['account_status'],
      unique: false,
      where: null,
    },
    {
      name: 'ix_lu_user_security_version',
      table: 'lu_user',
      columns: ['security_version'],
      unique: false,
      where: null,
    },
    {
      name: 'ix_lu_user_reconciliation_state',
      table: 'lu_user',
      columns: ['reconciliation_state'],
      unique: false,
      where: null,
    },
    {
      name: 'ix_lu_user_modified_by',
      table: 'lu_user',
      columns: ['modified_by_user_id'],
      unique: false,
      where: normalizeCheckDefinition('modified_by_user_id IS NOT NULL'),
    },
    {
      name: 'ix_lu_user_created_by',
      table: 'lu_user',
      columns: ['created_by_user_id'],
      unique: false,
      where: normalizeCheckDefinition('created_by_user_id IS NOT NULL'),
    },
  ],
};

/** lu_site_membership after 0006: position/department/hire_date, row_version
 * and actor FKs on top of the immutable 0001 membership contract. */
export const luSiteMembershipFinalTable: TableManifest = {
  columns: [
    uuidNotNull('user_id'),
    uuidNotNull('site_id'),
    textColumn('role', false, null),
    textColumn('status', false, normalizeDefault("'active'::text")),
    timestamptz('valid_from', true, null),
    timestamptz('valid_until', true, null),
    timestamptz('created_at', false, timestampDefault),
    timestamptz('updated_at', false, timestampDefault),
    varchar('position', 100, true, null),
    varchar('department', 100, true, null),
    {
      name: 'hire_date',
      dataType: 'date',
      udtName: 'date',
      charMaxLength: null,
      isNullable: true,
      columnDefault: null,
    },
    bigint('row_version', false, normalizeDefault('0')),
    uuidNullable('created_by_user_id'),
    uuidNullable('modified_by_user_id'),
  ],
  primaryKey: { columns: ['user_id', 'site_id'] },
  uniques: [],
  foreignKeys: [
    {
      columns: ['user_id'],
      referencesTable: 'lu_user',
      referencesColumns: ['id'],
      deleteAction: 'RESTRICT',
    },
    {
      columns: ['site_id'],
      referencesTable: 'lu_site',
      referencesColumns: ['id'],
      deleteAction: 'RESTRICT',
    },
    {
      columns: ['created_by_user_id'],
      referencesTable: 'lu_user',
      referencesColumns: ['id'],
      deleteAction: 'RESTRICT',
    },
    {
      columns: ['modified_by_user_id'],
      referencesTable: 'lu_user',
      referencesColumns: ['id'],
      deleteAction: 'RESTRICT',
    },
  ],
  checks: checks(
    "role IN ('Administrador', 'Supervisor')",
    "status IN ('active', 'suspended', 'revoked')",
    'valid_until IS NULL OR valid_from IS NULL OR valid_until > valid_from',
    'updated_at >= created_at',
    'row_version >= 0',
    'position IS NULL OR length(position) <= 100',
    'department IS NULL OR length(department) <= 100',
  ),
  indexes: [
    pkIndex('lu_site_membership', ['user_id', 'site_id']),
    {
      name: 'ix_lu_site_membership_site_status',
      table: 'lu_site_membership',
      columns: ['site_id', 'status'],
      unique: false,
      where: null,
    },
    {
      name: 'ix_lu_site_membership_user_status',
      table: 'lu_site_membership',
      columns: ['user_id', 'status'],
      unique: false,
      where: null,
    },
  ],
};

/** lu_session after 0006: purpose column (default 'normal'), purpose CHECKs
 * and the purpose expiry index on top of the immutable 0001 session contract. */
export const luSessionFinalTable: TableManifest = {
  columns: [
    uuidNotNull('id'),
    char('token_hash', 64, false, null),
    uuidNotNull('user_id'),
    uuidNullable('active_site_id'),
    bigint('security_version', false, null),
    timestamptz('created_at', false, timestampDefault),
    timestamptz('last_seen_at', false, timestampDefault),
    timestamptz('idle_expires_at', false, null),
    timestamptz('absolute_expires_at', false, null),
    timestamptz('revoked_at', true, null),
    textColumn('revocation_reason', true, null),
    textColumn('purpose', false, normalizeDefault("'normal'::text")),
  ],
  primaryKey: { columns: ['id'] },
  uniques: [{ columns: ['token_hash'] }],
  foreignKeys: [
    {
      columns: ['user_id'],
      referencesTable: 'lu_user',
      referencesColumns: ['id'],
      deleteAction: 'RESTRICT',
    },
    {
      columns: ['active_site_id'],
      referencesTable: 'lu_site',
      referencesColumns: ['id'],
      deleteAction: 'RESTRICT',
    },
  ],
  checks: checks(
    "token_hash ~ '^[0-9a-f]{64}$'",
    'security_version >= 0',
    'last_seen_at >= created_at',
    'idle_expires_at > created_at',
    'absolute_expires_at > created_at',
    'idle_expires_at <= absolute_expires_at',
    'revoked_at IS NULL OR revoked_at >= created_at',
    'revoked_at IS NULL OR revocation_reason IS NOT NULL',
    "revocation_reason IS NULL OR btrim(revocation_reason) <> ''",
    // 0006 step 19 (the IS NULL branch is vestigial after SET NOT NULL but
    // the constraint text is exactly what the SQL installs).
    "purpose IS NULL OR purpose IN ('normal', 'password_change', 'site_selection')",
    "purpose IS NULL OR purpose = 'normal' OR active_site_id IS NULL",
  ),
  indexes: [
    pkIndex('lu_session'),
    {
      name: 'lu_session_token_hash_key',
      table: 'lu_session',
      columns: ['token_hash'],
      unique: true,
      where: null,
    },
    {
      name: 'ix_lu_session_user',
      table: 'lu_session',
      columns: ['user_id'],
      unique: false,
      where: null,
    },
    {
      name: 'ix_lu_session_active_expiry',
      table: 'lu_session',
      columns: ['absolute_expires_at', 'idle_expires_at'],
      unique: false,
      where: normalizeCheckDefinition('revoked_at IS NULL'),
    },
    {
      name: 'ix_lu_session_purpose_expiry',
      table: 'lu_session',
      columns: ['purpose', 'absolute_expires_at'],
      unique: false,
      where: normalizeCheckDefinition('revoked_at IS NULL'),
    },
  ],
};

/** lu_tenant_route after 0006: the immutable 0003 route catalog plus the
 * migration_secret_reference column, its format CHECK and its partial unique
 * index. Composed from the real 0003 manifest table. */
const baseTenantRoute = TENANT_ROUTE_SCHEMA_MANIFEST.tables.lu_tenant_route;

export const luTenantRouteFinalTable: TableManifest = {
  columns: [...baseTenantRoute.columns, textColumn('migration_secret_reference', true, null)],
  primaryKey: baseTenantRoute.primaryKey,
  uniques: baseTenantRoute.uniques,
  foreignKeys: baseTenantRoute.foreignKeys,
  checks: [
    ...baseTenantRoute.checks,
    ...checks(
      "migration_secret_reference IS NULL OR (migration_secret_reference ~ '^[A-Za-z][A-Za-z0-9._/-]{2,127}$' AND migration_secret_reference !~ '\\.\\.')",
    ),
  ],
  indexes: [
    ...baseTenantRoute.indexes,
    {
      name: 'ux_lu_tenant_route_migration_secret_reference',
      table: 'lu_tenant_route',
      columns: ['migration_secret_reference'],
      unique: true,
      where: normalizeCheckDefinition('migration_secret_reference IS NOT NULL'),
    },
  ],
};

// ---------------------------------------------------------------------------
// Five new tables created by 0006
// ---------------------------------------------------------------------------

/**
 * lu_login_identifier (claim projection).
 *
 * The unique index on normalized_value is RAW (the column CHECK
 * `ck_lu_login_identifier_value_normalized` guarantees values are already
 * NFKC-lowercased by `lu_login_identifier_normalize`), NOT lower(...).
 * The current W6 SQL does NOT create `ix_lu_login_identifier_user`; only the
 * two unique indexes exist, so no non-unique index is declared here.
 */
export const luLoginIdentifierFinalTable: TableManifest = {
  columns: [
    uuidNotNull('id'),
    uuidNotNull('user_id'),
    textColumn('kind', false, null),
    varchar('normalized_value', 256, false, null),
    timestamptz('created_at', false, timestampDefault),
  ],
  primaryKey: { columns: ['id'] },
  uniques: [],
  foreignKeys: [
    {
      columns: ['user_id'],
      referencesTable: 'lu_user',
      referencesColumns: ['id'],
      deleteAction: 'RESTRICT',
    },
  ],
  checks: checks(
    "kind IN ('username', 'email')",
    'length(btrim(normalized_value)) > 0',
    'length(normalized_value) <= 256',
    // Structural normalization handles the function-call shape on both sides
    // (the verifier lowercases and strips ::text casts; the boolean parser
    // re-emits the call deterministically).
    'normalized_value = lu_login_identifier_normalize(normalized_value)',
  ),
  indexes: [
    pkIndex('lu_login_identifier'),
    {
      name: 'ux_lu_login_identifier_normalized_value',
      table: 'lu_login_identifier',
      columns: ['normalized_value'],
      unique: true,
      where: null,
    },
    {
      name: 'ux_lu_login_identifier_user_kind',
      table: 'lu_login_identifier',
      columns: ['user_id', 'kind'],
      unique: true,
      where: null,
    },
  ],
};

/** lu_legacy_user_xref (immutable migration crosswalk). */
export const luLegacyXrefFinalTable: TableManifest = {
  columns: [
    uuidNotNull('id'),
    textColumn('source_system', false, null),
    int4('legacy_user_id', false, null),
    uuidNotNull('user_id'),
    textColumn('migration_run_id', false, null),
    textColumn('source_fingerprint', false, null),
    timestamptz('migrated_at', false, timestampDefault),
  ],
  primaryKey: { columns: ['id'] },
  uniques: [],
  foreignKeys: [
    {
      columns: ['user_id'],
      referencesTable: 'lu_user',
      referencesColumns: ['id'],
      deleteAction: 'RESTRICT',
    },
  ],
  checks: checks(
    'length(btrim(source_system)) > 0 AND length(source_system) <= 64',
    'legacy_user_id >= 0',
    'length(btrim(migration_run_id)) > 0',
    'length(btrim(source_fingerprint)) > 0',
  ),
  indexes: [
    pkIndex('lu_legacy_user_xref'),
    {
      name: 'ux_lu_legacy_user_xref_source_legacy',
      table: 'lu_legacy_user_xref',
      columns: ['source_system', 'legacy_user_id'],
      unique: true,
      where: null,
    },
    {
      name: 'ux_lu_legacy_user_xref_source_user',
      table: 'lu_legacy_user_xref',
      columns: ['source_system', 'user_id'],
      unique: true,
      where: null,
    },
    {
      name: 'ix_lu_legacy_user_xref_user',
      table: 'lu_legacy_user_xref',
      columns: ['user_id'],
      unique: false,
      where: null,
    },
  ],
};

/**
 * lu_identity_audit_event (append-only audit). Per the current W6 SQL the
 * before/after status domains are the ACCOUNT statuses only:
 * ('active', 'inactive', 'deleted').
 */
export const luIdentityAuditFinalTable: TableManifest = {
  columns: [
    uuidNotNull('id'),
    timestamptz('occurred_at', false, timestampDefault),
    textColumn('action', false, null),
    uuidNullable('subject_user_id'),
    uuidNullable('actor_user_id'),
    uuidNullable('site_id'),
    uuidNullable('correlation_id'),
    textColumn('reason', true, null),
    textColumn('before_status', true, null),
    textColumn('after_status', true, null),
    {
      name: 'metadata',
      dataType: 'jsonb',
      udtName: 'jsonb',
      charMaxLength: null,
      isNullable: false,
      columnDefault: normalizeDefault("'{}'::jsonb"),
    },
  ],
  primaryKey: { columns: ['id'] },
  uniques: [],
  foreignKeys: [
    {
      columns: ['subject_user_id'],
      referencesTable: 'lu_user',
      referencesColumns: ['id'],
      deleteAction: 'RESTRICT',
    },
    {
      columns: ['actor_user_id'],
      referencesTable: 'lu_user',
      referencesColumns: ['id'],
      deleteAction: 'RESTRICT',
    },
    {
      columns: ['site_id'],
      referencesTable: 'lu_site',
      referencesColumns: ['id'],
      deleteAction: 'RESTRICT',
    },
  ],
  checks: checks(
    'length(btrim(action)) > 0 AND length(action) <= 128',
    "before_status IS NULL OR before_status IN ('active', 'inactive', 'deleted')",
    "after_status IS NULL OR after_status IN ('active', 'inactive', 'deleted')",
    "jsonb_typeof(metadata) = 'object'",
  ),
  indexes: [
    pkIndex('lu_identity_audit_event'),
    {
      name: 'ix_lu_identity_audit_event_subject_time',
      table: 'lu_identity_audit_event',
      columns: ['subject_user_id', 'occurred_at'],
      unique: false,
      where: normalizeCheckDefinition('subject_user_id IS NOT NULL'),
    },
    {
      name: 'ix_lu_identity_audit_event_actor_time',
      table: 'lu_identity_audit_event',
      columns: ['actor_user_id', 'occurred_at'],
      unique: false,
      where: normalizeCheckDefinition('actor_user_id IS NOT NULL'),
    },
    {
      name: 'ix_lu_identity_audit_event_site_time',
      table: 'lu_identity_audit_event',
      columns: ['site_id', 'occurred_at'],
      unique: false,
      where: normalizeCheckDefinition('site_id IS NOT NULL'),
    },
    {
      name: 'ix_lu_identity_audit_event_action_time',
      table: 'lu_identity_audit_event',
      columns: ['action', 'occurred_at'],
      unique: false,
      where: null,
    },
  ],
};

/**
 * lu_identity_migration_state (cutover singleton).
 *
 * DEPARSE-PINNED CHECK: the deadline constraint contains `+ INTERVAL '90 days'`.
 * The boolean normalizer fails closed on '+' (it refuses to structurally
 * re-emit what it cannot parse), so the expected string below is written in
 * the EXACT pg_get_constraintdef shape (parenthesized atoms/AND-groups, the
 * addition wrapped in parentheses, and the un-stripped `::interval` cast).
 * Do not "clean up" this string: any whitespace/paren deviation breaks the
 * bidirectional comparison. Re-confirm on the first real PG run.
 */
export const luIdentityMigrationStateFinalTable: TableManifest = {
  columns: [
    smallint('id', false, normalizeDefault('1')),
    textColumn('writer_label', true, null),
    timestamptz('cutover_at', true, null),
    timestamptz('legacy_password_deadline', true, null),
    timestamptz('created_at', false, timestampDefault),
    timestamptz('updated_at', false, timestampDefault),
  ],
  primaryKey: { columns: ['id'] },
  uniques: [],
  foreignKeys: [],
  checks: checks(
    'id = 1',
    "writer_label IS NULL OR writer_label IN ('LEGACY_SQLSERVER', 'NEST_SQLSERVER', 'NEST_POSTGRES')",
    "((cutover_at IS NULL) AND (legacy_password_deadline IS NULL)) OR ((cutover_at IS NOT NULL) AND (legacy_password_deadline IS NOT NULL) AND (legacy_password_deadline = (cutover_at + '90 days'::interval)))",
    'updated_at >= created_at',
  ),
  indexes: [pkIndex('lu_identity_migration_state')],
};

/** lu_migration_history (control-plane stream ledger). Constraint-backed
 * UNIQUE (stream, relative_path) plus the applied_at index. */
export const luMigrationHistoryFinalTable: TableManifest = {
  columns: [
    textColumn('stream', false, null),
    int4('ordinal', false, null),
    textColumn('relative_path', false, null),
    char('sha256', 64, false, null),
    int4('bytes', false, null),
    textColumn('work_id', true, null),
    timestamptz('applied_at', false, timestampDefault),
  ],
  primaryKey: { columns: ['stream', 'ordinal'] },
  uniques: [{ columns: ['stream', 'relative_path'] }],
  foreignKeys: [],
  checks: checks(
    "stream IN ('control-plane', 'tenant')",
    'ordinal >= 1',
    "sha256 ~ '^[0-9a-f]{64}$'",
    'bytes > 0',
    'length(relative_path) > 0',
  ),
  indexes: [
    pkIndex('lu_migration_history', ['stream', 'ordinal']),
    {
      name: 'lu_migration_history_stream_relative_path_key',
      table: 'lu_migration_history',
      columns: ['stream', 'relative_path'],
      unique: true,
      where: null,
    },
    {
      name: 'ix_lu_migration_history_applied_at',
      table: 'lu_migration_history',
      columns: ['applied_at'],
      unique: false,
      where: null,
    },
  ],
};

// ---------------------------------------------------------------------------
// Cumulative post-0006 control-plane manifest (0001..0006)
// ---------------------------------------------------------------------------

export type PostF2ControlPlaneTable =
  | 'lu_site'
  | 'lu_user'
  | 'lu_site_membership'
  | 'lu_session'
  | 'lu_auth_rate_limit'
  | 'lu_security_event'
  | 'lu_tenant_route'
  | 'lu_faculty'
  | 'lu_career'
  | 'lu_site_career'
  | 'lu_login_identifier'
  | 'lu_legacy_user_xref'
  | 'lu_identity_audit_event'
  | 'lu_identity_migration_state'
  | 'lu_migration_history';

/**
 * Exact final control-plane table-name list (4 baseline + 2 auth + 1 route +
 * 3 academic + 5 identity contract = 15). Mirrors
 * CONTROL_PLANE_TABLES_POST_F2 in migration-plan.ts and is the inspection
 * target for the post-0006 runner status path.
 */
export const POST_F2_CONTROL_PLANE_TABLES: readonly PostF2ControlPlaneTable[] = [
  'lu_site',
  'lu_user',
  'lu_site_membership',
  'lu_session',
  'lu_auth_rate_limit',
  'lu_security_event',
  'lu_tenant_route',
  'lu_faculty',
  'lu_career',
  'lu_site_career',
  'lu_login_identifier',
  'lu_legacy_user_xref',
  'lu_identity_audit_event',
  'lu_identity_migration_state',
  'lu_migration_history',
];

/**
 * Final control-plane schema expected after registered migrations 0001-0006.
 * Unchanged baseline/auth tables are the REAL exported manifest tables from
 * schema-manifest.ts; academic tables come from
 * ACADEMIC_CATALOG_SCHEMA_MANIFEST; every 0006-altered table and all five new
 * tables are spelled out completely above. No empty or cast-only specs.
 */
export const POST_F2_CONTROL_PLANE_SCHEMA_MANIFEST: SchemaManifest<PostF2ControlPlaneTable> = {
  schema: 'public',
  tables: {
    ...IDENTITY_CONTROL_PLANE_SCHEMA_MANIFEST.tables,
    ...AUTH_SECURITY_SCHEMA_MANIFEST.tables,
    ...ACADEMIC_CATALOG_SCHEMA_MANIFEST.tables,
    lu_user: luUserFinalTable,
    lu_site_membership: luSiteMembershipFinalTable,
    lu_session: luSessionFinalTable,
    lu_tenant_route: luTenantRouteFinalTable,
    lu_login_identifier: luLoginIdentifierFinalTable,
    lu_legacy_user_xref: luLegacyXrefFinalTable,
    lu_identity_audit_event: luIdentityAuditFinalTable,
    lu_identity_migration_state: luIdentityMigrationStateFinalTable,
    lu_migration_history: luMigrationHistoryFinalTable,
  },
};

// ---------------------------------------------------------------------------
// Supplemental expectation model for runner W9: sequences, ACLs and triggers
// that the SchemaManifest model cannot express. `SchemaManifest` stays exact
// for everything it CAN express; these structures carry the rest without
// inventing fake table specs.
// ---------------------------------------------------------------------------

export interface SequenceExpectation {
  readonly name: string;
  readonly ownedByTable: string;
  readonly ownedByColumn: string;
  /**
   * Identity mode: declared iff the sequence is owned by a column declared
   * `GENERATED ... AS IDENTITY`. When set, the verifier accepts
   * `pg_depend.deptype IN ('a', 'i')` for the ownership link (serial
   * `'a'` or identity `'i'`) AND additionally asserts
   * `pg_attribute.attidentity='d'` on the owning column. Only `'BY DEFAULT'`
   * is permitted (every migration in the contract uses BY DEFAULT AS
   * IDENTITY; ALWAYS is not modeled).
   */
  readonly identity?: 'BY DEFAULT';
}

export interface TriggerExpectation {
  readonly table: string;
  readonly name: string;
  readonly timing: 'BEFORE' | 'AFTER';
  readonly events: readonly ('INSERT' | 'UPDATE' | 'DELETE')[];
  /** Column list when created with `UPDATE OF <cols>`; null otherwise. */
  readonly updateColumns: readonly string[] | null;
  readonly isConstraintTrigger: boolean;
  readonly deferrable: boolean;
  readonly initiallyDeferred: boolean;
  /** Trigger function with signature, e.g. `lu_user_status_no_diverge()`. */
  readonly functionName: string;
}

export interface TablePrivilegeExpectation {
  readonly role: string;
  readonly table: string;
  readonly grants: readonly {
    readonly privilege: 'SELECT' | 'INSERT' | 'UPDATE' | 'DELETE' | 'TRUNCATE';
    /** null = table-level grant; otherwise the granted column list. */
    readonly columns: readonly string[] | null;
    readonly granted: boolean;
  }[];
}

export interface SequencePrivilegeExpectation {
  readonly role: string;
  readonly sequence: string;
  readonly privileges: readonly ('USAGE' | 'SELECT' | 'UPDATE')[];
  readonly granted: boolean;
}

export interface FunctionPrivilegeExpectation {
  readonly role: string;
  readonly functionName: string;
  readonly privilege: 'EXECUTE';
  readonly granted: boolean;
}

export interface SchemaPrivilegeExpectation {
  readonly role: string;
  readonly schema: string;
  readonly privilege: 'USAGE';
  readonly granted: boolean;
}

export interface MigrationSupplementalExpectation {
  readonly stream: 'control-plane' | 'tenant';
  readonly relativePath: string;
  readonly sequences: readonly SequenceExpectation[];
  readonly triggers: readonly TriggerExpectation[];
  readonly tableAcls: readonly TablePrivilegeExpectation[];
  readonly sequenceAcls: readonly SequencePrivilegeExpectation[];
  readonly functionAcls: readonly FunctionPrivilegeExpectation[];
  readonly schemaAcls: readonly SchemaPrivilegeExpectation[];
  readonly notes: readonly string[];
}

// --- 0004 (academic catalogs) ----------------------------------------------

export const CONTROL_PLANE_0004_SUPPLEMENTAL: MigrationSupplementalExpectation = {
  stream: 'control-plane',
  relativePath: 'control-plane/0004_academic_catalogs.sql',
  sequences: [
    {
      name: 'lu_faculty_id_seq',
      ownedByTable: 'lu_faculty',
      ownedByColumn: 'id',
      identity: 'BY DEFAULT',
    },
    {
      name: 'lu_career_id_seq',
      ownedByTable: 'lu_career',
      ownedByColumn: 'id',
      identity: 'BY DEFAULT',
    },
  ],
  triggers: [],
  tableAcls: [
    {
      role: 'lu_auth_runtime',
      table: 'lu_faculty',
      grants: [
        { privilege: 'SELECT', columns: null, granted: true },
        { privilege: 'INSERT', columns: null, granted: true },
        { privilege: 'UPDATE', columns: null, granted: true },
      ],
    },
    {
      role: 'lu_auth_runtime',
      table: 'lu_career',
      grants: [
        { privilege: 'SELECT', columns: null, granted: true },
        { privilege: 'INSERT', columns: null, granted: true },
        { privilege: 'UPDATE', columns: null, granted: true },
      ],
    },
    {
      role: 'lu_auth_runtime',
      table: 'lu_site_career',
      grants: [
        { privilege: 'SELECT', columns: null, granted: true },
        { privilege: 'INSERT', columns: null, granted: true },
        { privilege: 'UPDATE', columns: null, granted: true },
      ],
    },
  ],
  sequenceAcls: [
    {
      role: 'lu_auth_runtime',
      sequence: 'lu_faculty_id_seq',
      privileges: ['USAGE', 'SELECT', 'UPDATE'],
      granted: true,
    },
    {
      role: 'lu_auth_runtime',
      sequence: 'lu_career_id_seq',
      privileges: ['USAGE', 'SELECT', 'UPDATE'],
      granted: true,
    },
  ],
  functionAcls: [],
  schemaAcls: [],
  notes: [
    'lu_site_career has a composite PK and therefore owns NO identity sequence.',
    'All 0004 grants run inside a role-guarded DO block: they apply only when lu_auth_runtime existed at apply time.',
  ],
};

// --- 0005 (ACL-only user management) ----------------------------------------
// 0005 mutates NO schema surface; it only grants table privileges. It is
// therefore NOT represented by any SchemaManifest — the precise expectation is
// this ACL list.

export const CONTROL_PLANE_0005_SUPPLEMENTAL: MigrationSupplementalExpectation = {
  stream: 'control-plane',
  relativePath: 'control-plane/0005_user_management.sql',
  sequences: [],
  triggers: [],
  tableAcls: [
    {
      role: 'lu_auth_runtime',
      table: 'lu_user',
      grants: [
        { privilege: 'SELECT', columns: null, granted: true },
        { privilege: 'INSERT', columns: null, granted: true },
        { privilege: 'UPDATE', columns: null, granted: true },
      ],
    },
    {
      role: 'lu_auth_runtime',
      table: 'lu_site_membership',
      grants: [
        { privilege: 'SELECT', columns: null, granted: true },
        { privilege: 'INSERT', columns: null, granted: true },
        { privilege: 'UPDATE', columns: null, granted: true },
      ],
    },
  ],
  sequenceAcls: [],
  functionAcls: [],
  schemaAcls: [],
  notes: [
    '0005 is ACL-only: it creates no tables, indexes, checks, sequences or triggers.',
    'Grants run inside a role-guarded DO block; 0006 later narrows lu_user/lu_site_membership to column-level INSERT/UPDATE.',
  ],
};

// --- 0006 (identity contract) ------------------------------------------------

const luUser0006InsertColumns: readonly string[] = [
  'id',
  'email',
  'password_hash',
  'password_scheme',
  'must_change_password',
  'is_super_admin',
  'account_status',
  'status',
  'username',
  'first_name',
  'last_name',
  'second_last_name',
  'identity_card',
  'phone_number',
  'profile_picture_key',
  'password_migrated_at',
  'created_by_user_id',
  'modified_by_user_id',
];

const luUser0006UpdateColumns: readonly string[] = [
  'email',
  'first_name',
  'last_name',
  'second_last_name',
  'identity_card',
  'phone_number',
  'profile_picture_key',
  'password_hash',
  'password_scheme',
  'must_change_password',
  'password_migrated_at',
  'account_status',
  'status',
  'is_super_admin',
  'security_version',
  'modified_by_user_id',
];

const luSiteMembership0006InsertColumns: readonly string[] = [
  'user_id',
  'site_id',
  'role',
  'status',
  'valid_from',
  'valid_until',
  'position',
  'department',
  'hire_date',
  'created_by_user_id',
  'modified_by_user_id',
];

const luSiteMembership0006UpdateColumns: readonly string[] = [
  'role',
  'status',
  'valid_from',
  'valid_until',
  'position',
  'department',
  'hire_date',
  'modified_by_user_id',
];

const luSession0006UpdateColumns: readonly string[] = [
  'last_seen_at',
  'idle_expires_at',
  'absolute_expires_at',
  'revoked_at',
  'revocation_reason',
  'active_site_id',
];

const luTenantRoute0006SelectColumns: readonly string[] = [
  'site_id',
  'runtime_secret_reference',
  'writer_label',
  'state',
  'schema_version',
  'last_health_at',
  'created_at',
  'updated_at',
];

/** Names of the 17 SECURITY DEFINER trigger/normalizer functions 0006 creates
 * and revokes from PUBLIC (EXECUTE). */
const luUser0006FunctionsRevokedFromPublic: readonly string[] = [
  'lu_user_status_no_diverge()',
  'lu_user_full_name_sync()',
  'lu_user_row_version_inc()',
  'lu_user_immutable_created()',
  'lu_user_sync_claims()',
  'lu_login_identifier_normalize(text)',
  'lu_user_identity_card_normalize(text)',
  'lu_user_split_name_normalize(text)',
  'lu_user_phone_normalize(text)',
  'lu_login_identifier_two_claims_check()',
  'lu_legacy_user_xref_immutable()',
  'lu_identity_audit_event_append_only()',
  'lu_identity_migration_state_lock()',
  'lu_migration_history_immutable()',
  'lu_site_membership_row_version_inc()',
  'lu_site_membership_immutable_created()',
  'lu_identity_hard_delete_forbidden()',
];

export const CONTROL_PLANE_0006_SUPPLEMENTAL: MigrationSupplementalExpectation = {
  stream: 'control-plane',
  relativePath: 'control-plane/0006_mig001_users_identity.sql',
  sequences: [],
  triggers: [
    {
      table: 'lu_user',
      name: 'trg_lu_user_status_no_diverge',
      timing: 'BEFORE',
      events: ['INSERT', 'UPDATE'],
      updateColumns: ['status', 'account_status'],
      isConstraintTrigger: false,
      deferrable: false,
      initiallyDeferred: false,
      functionName: 'lu_user_status_no_diverge()',
    },
    {
      table: 'lu_user',
      name: 'trg_lu_user_full_name_sync',
      timing: 'BEFORE',
      events: ['INSERT', 'UPDATE'],
      updateColumns: null,
      isConstraintTrigger: false,
      deferrable: false,
      initiallyDeferred: false,
      functionName: 'lu_user_full_name_sync()',
    },
    {
      table: 'lu_user',
      name: 'trg_lu_user_row_version_inc',
      timing: 'BEFORE',
      events: ['INSERT', 'UPDATE'],
      updateColumns: null,
      isConstraintTrigger: false,
      deferrable: false,
      initiallyDeferred: false,
      functionName: 'lu_user_row_version_inc()',
    },
    {
      table: 'lu_user',
      name: 'trg_lu_user_immutable_created',
      timing: 'BEFORE',
      events: ['UPDATE'],
      updateColumns: null,
      isConstraintTrigger: false,
      deferrable: false,
      initiallyDeferred: false,
      functionName: 'lu_user_immutable_created()',
    },
    {
      table: 'lu_user',
      name: 'trg_lu_user_sync_claims',
      timing: 'AFTER',
      events: ['INSERT', 'UPDATE'],
      updateColumns: ['email', 'username', 'reconciliation_state'],
      isConstraintTrigger: false,
      deferrable: false,
      initiallyDeferred: false,
      functionName: 'lu_user_sync_claims()',
    },
    {
      table: 'lu_login_identifier',
      name: 'trg_lu_login_identifier_two_claims',
      timing: 'AFTER',
      events: ['INSERT', 'UPDATE', 'DELETE'],
      updateColumns: null,
      isConstraintTrigger: true,
      deferrable: true,
      initiallyDeferred: true,
      functionName: 'lu_login_identifier_two_claims_check()',
    },
    {
      table: 'lu_user',
      name: 'trg_lu_user_two_claims_invariant',
      timing: 'AFTER',
      events: ['INSERT', 'UPDATE'],
      updateColumns: ['reconciliation_state', 'email', 'username'],
      isConstraintTrigger: true,
      deferrable: true,
      initiallyDeferred: true,
      functionName: 'lu_login_identifier_two_claims_check()',
    },
    {
      table: 'lu_legacy_user_xref',
      name: 'trg_lu_legacy_user_xref_no_update',
      timing: 'BEFORE',
      events: ['UPDATE'],
      updateColumns: null,
      isConstraintTrigger: false,
      deferrable: false,
      initiallyDeferred: false,
      functionName: 'lu_legacy_user_xref_immutable()',
    },
    {
      table: 'lu_legacy_user_xref',
      name: 'trg_lu_legacy_user_xref_no_delete',
      timing: 'BEFORE',
      events: ['DELETE'],
      updateColumns: null,
      isConstraintTrigger: false,
      deferrable: false,
      initiallyDeferred: false,
      functionName: 'lu_legacy_user_xref_immutable()',
    },
    {
      table: 'lu_identity_audit_event',
      name: 'trg_lu_identity_audit_event_no_update',
      timing: 'BEFORE',
      events: ['UPDATE'],
      updateColumns: null,
      isConstraintTrigger: false,
      deferrable: false,
      initiallyDeferred: false,
      functionName: 'lu_identity_audit_event_append_only()',
    },
    {
      table: 'lu_identity_audit_event',
      name: 'trg_lu_identity_audit_event_no_delete',
      timing: 'BEFORE',
      events: ['DELETE'],
      updateColumns: null,
      isConstraintTrigger: false,
      deferrable: false,
      initiallyDeferred: false,
      functionName: 'lu_identity_audit_event_append_only()',
    },
    {
      table: 'lu_identity_migration_state',
      name: 'trg_lu_identity_migration_state_lock',
      timing: 'BEFORE',
      events: ['UPDATE'],
      updateColumns: null,
      isConstraintTrigger: false,
      deferrable: false,
      initiallyDeferred: false,
      functionName: 'lu_identity_migration_state_lock()',
    },
    {
      table: 'lu_migration_history',
      name: 'trg_lu_migration_history_no_update',
      timing: 'BEFORE',
      events: ['UPDATE'],
      updateColumns: null,
      isConstraintTrigger: false,
      deferrable: false,
      initiallyDeferred: false,
      functionName: 'lu_migration_history_immutable()',
    },
    {
      table: 'lu_migration_history',
      name: 'trg_lu_migration_history_no_delete',
      timing: 'BEFORE',
      events: ['DELETE'],
      updateColumns: null,
      isConstraintTrigger: false,
      deferrable: false,
      initiallyDeferred: false,
      functionName: 'lu_migration_history_immutable()',
    },
    {
      table: 'lu_site_membership',
      name: 'trg_lu_site_membership_row_version_inc',
      timing: 'BEFORE',
      events: ['INSERT', 'UPDATE'],
      updateColumns: null,
      isConstraintTrigger: false,
      deferrable: false,
      initiallyDeferred: false,
      functionName: 'lu_site_membership_row_version_inc()',
    },
    {
      table: 'lu_site_membership',
      name: 'trg_lu_site_membership_immutable_created',
      timing: 'BEFORE',
      events: ['UPDATE'],
      updateColumns: null,
      isConstraintTrigger: false,
      deferrable: false,
      initiallyDeferred: false,
      functionName: 'lu_site_membership_immutable_created()',
    },
    {
      table: 'lu_user',
      name: 'trg_lu_user_no_hard_delete',
      timing: 'BEFORE',
      events: ['DELETE'],
      updateColumns: null,
      isConstraintTrigger: false,
      deferrable: false,
      initiallyDeferred: false,
      functionName: 'lu_identity_hard_delete_forbidden()',
    },
    {
      table: 'lu_site_membership',
      name: 'trg_lu_site_membership_no_hard_delete',
      timing: 'BEFORE',
      events: ['DELETE'],
      updateColumns: null,
      isConstraintTrigger: false,
      deferrable: false,
      initiallyDeferred: false,
      functionName: 'lu_identity_hard_delete_forbidden()',
    },
  ],
  tableAcls: [
    {
      role: 'lu_auth_runtime',
      table: 'lu_user',
      grants: [
        { privilege: 'INSERT', columns: null, granted: false },
        { privilege: 'UPDATE', columns: null, granted: false },
        { privilege: 'DELETE', columns: null, granted: false },
        { privilege: 'TRUNCATE', columns: null, granted: false },
        { privilege: 'SELECT', columns: null, granted: true },
        { privilege: 'INSERT', columns: luUser0006InsertColumns, granted: true },
        { privilege: 'UPDATE', columns: luUser0006UpdateColumns, granted: true },
      ],
    },
    {
      role: 'lu_auth_runtime',
      table: 'lu_site_membership',
      grants: [
        { privilege: 'INSERT', columns: null, granted: false },
        { privilege: 'UPDATE', columns: null, granted: false },
        { privilege: 'DELETE', columns: null, granted: false },
        { privilege: 'TRUNCATE', columns: null, granted: false },
        { privilege: 'SELECT', columns: null, granted: true },
        { privilege: 'INSERT', columns: luSiteMembership0006InsertColumns, granted: true },
        { privilege: 'UPDATE', columns: luSiteMembership0006UpdateColumns, granted: true },
      ],
    },
    {
      role: 'lu_auth_runtime',
      table: 'lu_session',
      grants: [
        { privilege: 'INSERT', columns: null, granted: false },
        { privilege: 'UPDATE', columns: null, granted: false },
        { privilege: 'DELETE', columns: null, granted: false },
        { privilege: 'TRUNCATE', columns: null, granted: false },
        { privilege: 'SELECT', columns: null, granted: true },
        { privilege: 'INSERT', columns: null, granted: true },
        { privilege: 'UPDATE', columns: luSession0006UpdateColumns, granted: true },
      ],
    },
    {
      role: 'lu_auth_runtime',
      table: 'lu_login_identifier',
      grants: [
        { privilege: 'INSERT', columns: null, granted: false },
        { privilege: 'UPDATE', columns: null, granted: false },
        { privilege: 'DELETE', columns: null, granted: false },
        { privilege: 'TRUNCATE', columns: null, granted: false },
        { privilege: 'SELECT', columns: null, granted: true },
      ],
    },
    {
      role: 'lu_auth_runtime',
      table: 'lu_legacy_user_xref',
      grants: [
        { privilege: 'INSERT', columns: null, granted: false },
        { privilege: 'UPDATE', columns: null, granted: false },
        { privilege: 'DELETE', columns: null, granted: false },
        { privilege: 'TRUNCATE', columns: null, granted: false },
        { privilege: 'SELECT', columns: null, granted: true },
      ],
    },
    {
      role: 'lu_auth_runtime',
      table: 'lu_identity_migration_state',
      grants: [
        { privilege: 'INSERT', columns: null, granted: false },
        { privilege: 'UPDATE', columns: null, granted: false },
        { privilege: 'DELETE', columns: null, granted: false },
        { privilege: 'TRUNCATE', columns: null, granted: false },
        { privilege: 'SELECT', columns: null, granted: true },
      ],
    },
    {
      role: 'lu_auth_runtime',
      table: 'lu_identity_audit_event',
      grants: [
        { privilege: 'UPDATE', columns: null, granted: false },
        { privilege: 'DELETE', columns: null, granted: false },
        { privilege: 'TRUNCATE', columns: null, granted: false },
        { privilege: 'SELECT', columns: null, granted: true },
        { privilege: 'INSERT', columns: null, granted: true },
      ],
    },
    {
      role: 'lu_auth_runtime',
      table: 'lu_migration_history',
      grants: [
        { privilege: 'INSERT', columns: null, granted: false },
        { privilege: 'UPDATE', columns: null, granted: false },
        { privilege: 'DELETE', columns: null, granted: false },
        { privilege: 'TRUNCATE', columns: null, granted: false },
        { privilege: 'SELECT', columns: null, granted: true },
      ],
    },
    {
      role: 'lu_auth_runtime',
      table: 'lu_tenant_route',
      grants: [
        { privilege: 'INSERT', columns: null, granted: false },
        { privilege: 'UPDATE', columns: null, granted: false },
        { privilege: 'DELETE', columns: null, granted: false },
        { privilege: 'TRUNCATE', columns: null, granted: false },
        { privilege: 'SELECT', columns: luTenantRoute0006SelectColumns, granted: true },
      ],
    },
  ],
  sequenceAcls: [],
  functionAcls: [
    {
      role: 'lu_auth_runtime',
      functionName: 'lu_login_identifier_normalize(text)',
      privilege: 'EXECUTE',
      granted: true,
    },
    ...luUser0006FunctionsRevokedFromPublic.map((functionName): FunctionPrivilegeExpectation => ({
      role: 'PUBLIC',
      functionName,
      privilege: 'EXECUTE',
      granted: false,
    })),
  ],
  schemaAcls: [],
  notes: [
    '0006 creates NO sequences (all new PKs are client-supplied UUIDs or composite).',
    'Fail-closed preflights: UTF8 server_encoding, PostgreSQL >= 13, pg_catalog "und-x-icu" collation, lu_auth_runtime role must exist.',
    '18 triggers installed across lu_user, lu_login_identifier, lu_legacy_user_xref, lu_identity_audit_event, lu_identity_migration_state, lu_migration_history, lu_site_membership.',
    'lu_tenant_route SELECT is column-limited and excludes migration_secret_reference (migration connection reads it server-side).',
  ],
};

/**
 * Lookup of the supplemental (non-SchemaManifest) expectations for the
 * historical/current control-plane migrations, keyed by relative path. 0004
 * and 0005 are immutable historical files; 0006 is the current W6 contract.
 */
export const CONTROL_PLANE_SUPPLEMENTAL_EXPECTATIONS: Readonly<
  Record<string, MigrationSupplementalExpectation>
> = Object.freeze({
  'control-plane/0004_academic_catalogs.sql': CONTROL_PLANE_0004_SUPPLEMENTAL,
  'control-plane/0005_user_management.sql': CONTROL_PLANE_0005_SUPPLEMENTAL,
  'control-plane/0006_mig001_users_identity.sql': CONTROL_PLANE_0006_SUPPLEMENTAL,
});
