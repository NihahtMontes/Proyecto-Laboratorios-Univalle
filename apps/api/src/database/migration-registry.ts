/**
 * Pinned migration registry for the control-plane and tenant streams.
 *
 * The registry is **immutable at runtime**: entries are declared statically
 * and cannot be appended to, replaced, or removed via the public API. Each
 * entry is identified by its `relativePath` and `stream`; basename lookup is
 * only used as a fallback for the legacy 0001-0003 entry shape.
 *
 * Pin semantics:
 *  - SHA-256 over the canonical LF-normalized bytes is invariant to Git's
 *    `text=auto` + `core.autocrlf=true` checkout.
 *  - Lone CRs and BOMs are rejected by `assertCanonicalBytes` so any
 *    substantive tamper fails closed before the SHA check.
 *  - Each entry has an explicit `ordinal`, a `touchedTables` allowlist (for
 *    CREATE/INDEX/ALTER), an `advisoryLockKey`, and an optional
 *    `finalManifestId` that the runner uses for post-apply verification.
 *
 * Per-entry policy:
 *  - `legacyPolicy: true`   — historical files whose bytes predate the
 *                              strict keyword allowlist. The envelope and
 *                              table allowlist remain in force; the strict
 *                              keyword allowlist is skipped. Dynamic EXECUTE
 *                              inside dollar-quoted bodies is still rejected.
 *  - `strictPolicy: true`   — new additive migrations (0006, tenant 0013).
 *                              Forbids destructive DDL/DML, role/principal
 *                              mutation, dynamic SQL smuggling.
 *  - both flags false       — bootstrap entries (0001-0003). Full keyword
 *                              allowlist.
 */
import { join } from 'node:path';
import {
  ACADEMIC_CATALOG_TABLES,
  AUTH_SECURITY_TARGET_TABLES,
  BASELINE_TARGET_TABLES,
  IDENTITY_CONTRACT_TABLES,
  IDENTITY_CONTRACT_TOUCHED_TABLES,
  TENANT_MIGRATION_HISTORY_TABLES,
  TENANT_ROUTE_TARGET_TABLES,
  type AnalyzeOptions,
  type MigrationStream,
  DEFAULT_STREAM,
  assertCanonicalBytes,
  buildMigrationPlan,
  canonicalByteLength,
  sanitizeSql,
  sha256Hex,
} from './migration-plan.js';

export const CONTROL_PLANE_SCHEMA = 'public';
export const TENANT_SCHEMA = 'public';

/**
 * Common per-site advisory lock prefix shared by EVERY tenant ordinal.
 * The runner takes `pg_advisory_xact_lock(hashtext('<prefix>:<site-uuid>'))`
 * once per migration apply so concurrent runners targeting the same site
 * serialize regardless of ordinal. Only the site UUID appears in the key;
 * the env var name / DSN / password / reference are never used.
 */
export const TENANT_SITE_ADVISORY_LOCK_PREFIX = 'lu:tenant:site';

/**
 * Common control-plane advisory lock key. Identical for every entry (no per-
 * entry or per-site variation): the runner takes it ONCE per migration apply
 * before the per-entry advisory lock. Its only role is to serialize any two
 * concurrent CP applies that share the same database.
 */
export const CONTROL_PLANE_COMMON_ADVISORY_LOCK_KEY = 'lu:identity-control-plane:common';

/**
 * Common tenant advisory lock key prefix. The runner composes the lock key
 * from this prefix plus the REQUIRED site UUID; if the tenant stream apply
 * is invoked without a site context the runner fails closed (the prefix
 * itself is never sufficient). Only the site UUID appears in the key.
 */
export const TENANT_COMMON_ADVISORY_LOCK_PREFIX = 'lu:tenant:site:common';

export type { MigrationStream };

/** Per-entry policy selectors. */
export interface MigrationRegistryEntry {
  readonly stream: MigrationStream;
  readonly file: string;
  readonly relativePath: string;
  /** Sequential ordinal within the stream. 1-based. */
  readonly ordinal: number;
  readonly style: 'bootstrap' | 'additive';
  readonly workId: string;
  readonly sha256: string;
  readonly byteLength: number;
  readonly advisoryLockKey: string;
  /** Per-site advisory key for tenant stream (combined with site UUID). */
  readonly siteAdvisoryKeyPrefix?: string;
  /**
   * Common tenant site advisory lock prefix. The runner takes
   * `pg_advisory_xact_lock(hashtext('<siteAdvisoryKeyPrefix>:<site-uuid>'))`
   * for every tenant ordinal before applying it, so concurrent runners
   * targeting the same site serialize. The prefix is the SAME for every
   * tenant ordinal (a per-site cross-ordinal mutex), unlike
   * `siteAdvisoryKeyPrefix` which is per-ordinal and used only for the
   * legacy per-file advisory lock the migration SQL itself takes.
   */
  readonly tenantSiteAdvisoryLockPrefix?: string;
  /**
   * Stream-common advisory lock key. Constant for the control-plane stream;
   * for the tenant stream the runner composes it from
   * `tenantStreamCommonAdvisoryLockPrefix` and the required site UUID (a
   * tenant apply without a site UUID must fail closed before any lock is
   * taken). The runner acquires this lock FIRST, then the per-entry
   * `advisoryLockKey`.
   */
  readonly streamCommonAdvisoryLockKey?: string;
  /**
   * Tenant-stream common advisory lock prefix. The runner composes the key
   * as `<prefix>:<site-uuid>`; the prefix is identical for every tenant
   * entry, only the site UUID varies per apply. Mandatory for tenant entries;
   * the runner fails closed when a tenant apply is missing the site context.
   */
  readonly tenantStreamCommonAdvisoryLockPrefix?: string;
  readonly targetSchema: string;
  /** Tables the migration CREATEs. */
  readonly targetTables: readonly string[];
  /** Tables the migration touches (CREATE/ALTER/INDEX on). */
  readonly touchedTables: readonly string[];
  readonly expectedCreateTableCount: number;
  readonly expectedIndexNames: readonly string[];
  readonly legacyPolicy: boolean;
  readonly strictPolicy: boolean;
  /**
   * DROP CONSTRAINT IF EXISTS names the strict policy permits. Defaults to
   * `[]`; only 0006 currently declares any (the two lu_user CHECKs it drops
   * before re-creating the columns).
   */
  readonly allowedDropConstraints: readonly string[];
  /** Identifier for the final manifest to verify against; null for per-entry manifest. */
  readonly finalManifestId: 'final-control-plane' | 'final-tenant' | 'per-entry' | null;
}

/**
 * Helper that produces a deeply-frozen registry entry. Every exported entry and
 * its array fields are passed through this so callers cannot accidentally
 * mutate the registry at runtime. The registry is the primary integrity gate;
 * frozen entries also let the analyzer reason about array identity safely.
 */
function freezeEntry<T extends MigrationRegistryEntry>(entry: T): T {
  for (const key of Object.keys(entry)) {
    const value = (entry as unknown as Record<string, unknown>)[key];
    if (Array.isArray(value)) {
      Object.freeze(value);
    }
  }
  return Object.freeze(entry);
}

export const CONTROL_PLANE_REGISTRY: readonly MigrationRegistryEntry[] = Object.freeze([
  freezeEntry({
    stream: 'control-plane',
    file: '0001_create_identity_control_plane.sql',
    relativePath: '0001_create_identity_control_plane.sql',
    ordinal: 1,
    style: 'bootstrap',
    workId: 'MIG-F3-PG-IDENTITY-001',
    sha256: 'a7698ea53a443d42e87bf905ae9e0f7060923a26199bc06ad23f17401e1bc118',
    byteLength: 4491,
    advisoryLockKey: 'lu:identity-control-plane:0001',
    streamCommonAdvisoryLockKey: CONTROL_PLANE_COMMON_ADVISORY_LOCK_KEY,
    targetSchema: CONTROL_PLANE_SCHEMA,
    targetTables: [...BASELINE_TARGET_TABLES],
    touchedTables: [...BASELINE_TARGET_TABLES],
    expectedCreateTableCount: 4,
    expectedIndexNames: [
      'ux_lu_site_code_lower',
      'ux_lu_user_email_lower',
      'ix_lu_site_membership_site_status',
      'ix_lu_session_user',
      'ix_lu_session_active_expiry',
    ],
    allowedDropConstraints: [],
    legacyPolicy: false,
    strictPolicy: false,
    finalManifestId: 'per-entry',
  }),
  freezeEntry({
    stream: 'control-plane',
    file: '0002_create_auth_security_controls.sql',
    relativePath: '0002_create_auth_security_controls.sql',
    ordinal: 2,
    style: 'additive',
    workId: 'MIG-F3-AUTH-SECURITY-014',
    sha256: '335bd7b1ed75b557e1618283d2272c4353bb029ca2fd28ee626566d3d93ff336',
    byteLength: 2727,
    advisoryLockKey: 'lu:identity-control-plane:0002',
    streamCommonAdvisoryLockKey: CONTROL_PLANE_COMMON_ADVISORY_LOCK_KEY,
    targetSchema: CONTROL_PLANE_SCHEMA,
    targetTables: [...AUTH_SECURITY_TARGET_TABLES],
    touchedTables: [...AUTH_SECURITY_TARGET_TABLES],
    expectedCreateTableCount: 2,
    expectedIndexNames: [
      'ix_lu_auth_rate_limit_reset',
      'ix_lu_security_event_type_time',
      'ix_lu_security_event_user_time',
    ],
    allowedDropConstraints: [],
    legacyPolicy: false,
    strictPolicy: false,
    finalManifestId: 'per-entry',
  }),
  freezeEntry({
    stream: 'control-plane',
    file: '0003_create_tenant_route_catalog.sql',
    relativePath: '0003_create_tenant_route_catalog.sql',
    ordinal: 3,
    style: 'additive',
    workId: 'MIG-F4-ROUTE-CATALOG-008',
    sha256: 'dfc0dc9a4cea47c584d5cc1f8705d30a3df9cc589f8702b9afeb15b43dd851f1',
    byteLength: 1692,
    advisoryLockKey: 'lu:identity-control-plane:0003',
    streamCommonAdvisoryLockKey: CONTROL_PLANE_COMMON_ADVISORY_LOCK_KEY,
    targetSchema: CONTROL_PLANE_SCHEMA,
    targetTables: [...TENANT_ROUTE_TARGET_TABLES],
    touchedTables: [...TENANT_ROUTE_TARGET_TABLES],
    expectedCreateTableCount: 1,
    expectedIndexNames: ['ux_lu_tenant_route_secret_reference', 'ix_lu_tenant_route_state'],
    allowedDropConstraints: [],
    legacyPolicy: false,
    strictPolicy: false,
    finalManifestId: 'per-entry',
  }),
  freezeEntry({
    stream: 'control-plane',
    file: '0004_academic_catalogs.sql',
    relativePath: 'control-plane/0004_academic_catalogs.sql',
    ordinal: 4,
    style: 'additive',
    workId: 'MIG-F3-PG-ACADEMIC-001',
    sha256: '65a916f8acc61a73f4b82cc60cdfa69f62a912770bea05f60082c581c270631b',
    byteLength: 2749,
    advisoryLockKey: 'lu:identity-control-plane:0004-academic',
    streamCommonAdvisoryLockKey: CONTROL_PLANE_COMMON_ADVISORY_LOCK_KEY,
    targetSchema: CONTROL_PLANE_SCHEMA,
    // 0004 creates lu_faculty, lu_career and lu_site_career; lu_site is only
    // touched as an FK target (lu_site_career.site_id REFERENCES lu_site.id).
    targetTables: [...ACADEMIC_CATALOG_TABLES],
    touchedTables: [...ACADEMIC_CATALOG_TABLES, 'lu_site'],
    expectedCreateTableCount: 3,
    expectedIndexNames: [
      'ux_lu_faculty_name_active',
      'ux_lu_faculty_code_active',
      'ux_lu_career_name_active',
      'ux_lu_career_code_active',
    ],
    allowedDropConstraints: [],
    legacyPolicy: true,
    strictPolicy: false,
    finalManifestId: 'final-control-plane',
  }),
  freezeEntry({
    stream: 'control-plane',
    file: '0005_user_management.sql',
    relativePath: 'control-plane/0005_user_management.sql',
    ordinal: 5,
    style: 'additive',
    workId: 'MIG-F4-CP-USERS-001',
    sha256: 'd83b147b375a67f42d50c516f599cb0e9d320229f510cbc8af08ade67df2aeaa',
    byteLength: 616,
    advisoryLockKey: 'lu:identity-control-plane:0005-users',
    streamCommonAdvisoryLockKey: CONTROL_PLANE_COMMON_ADVISORY_LOCK_KEY,
    targetSchema: CONTROL_PLANE_SCHEMA,
    targetTables: [],
    touchedTables: ['lu_user', 'lu_site_membership'],
    expectedCreateTableCount: 0,
    expectedIndexNames: [],
    allowedDropConstraints: [],
    legacyPolicy: true,
    strictPolicy: false,
    finalManifestId: 'final-control-plane',
  }),
  freezeEntry({
    stream: 'control-plane',
    file: '0006_mig001_users_identity.sql',
    relativePath: 'control-plane/0006_mig001_users_identity.sql',
    ordinal: 6,
    style: 'additive',
    workId: 'MIG-F2-ID-CONTRACT-001',
    // Canonical-LF digest and length of the current on-disk file. This is the
    // only place where W9 re-pins 0006 (W16B revision: pre-F2 password
    // classification with the FULL bcrypt alphabet [./A-Za-z0-9] + phone
    // format CHECK); historical entries above stay byte-identical (regression
    // guard in the registry test suite).
    sha256: '16d7b3e0e26c91947097b9bb2ac4df6181fffc1cf7460c8cfd48aecc071be3b4',
    byteLength: 54878,
    advisoryLockKey: 'lu:identity-control-plane:0006',
    streamCommonAdvisoryLockKey: CONTROL_PLANE_COMMON_ADVISORY_LOCK_KEY,
    targetSchema: CONTROL_PLANE_SCHEMA,
    targetTables: [...IDENTITY_CONTRACT_TABLES],
    touchedTables: [...IDENTITY_CONTRACT_TOUCHED_TABLES],
    expectedCreateTableCount: 5,
    expectedIndexNames: [
      'ux_lu_user_username_ci',
      'ux_lu_user_identity_card_ci',
      'ix_lu_user_account_status',
      'ix_lu_user_security_version',
      'ix_lu_user_reconciliation_state',
      'ix_lu_user_modified_by',
      'ix_lu_user_created_by',
      'ux_lu_login_identifier_normalized_value',
      'ux_lu_login_identifier_user_kind',
      'ux_lu_legacy_user_xref_source_legacy',
      'ux_lu_legacy_user_xref_source_user',
      'ix_lu_legacy_user_xref_user',
      'ix_lu_identity_audit_event_subject_time',
      'ix_lu_identity_audit_event_actor_time',
      'ix_lu_identity_audit_event_site_time',
      'ix_lu_identity_audit_event_action_time',
      'ix_lu_migration_history_applied_at',
      'ix_lu_site_membership_user_status',
      'ix_lu_session_purpose_expiry',
      'ux_lu_tenant_route_migration_secret_reference',
    ],
    // Only these two DROP CONSTRAINT IF EXISTS statements are permitted by
    // the strict policy (re-created with updated definitions immediately
    // afterwards). Every other DROP CONSTRAINT is rejected.
    allowedDropConstraints: ['lu_user_status_check', 'lu_user_password_hash_check'],
    legacyPolicy: false,
    strictPolicy: true,
    finalManifestId: 'final-control-plane',
  }),
]);

export const TENANT_REGISTRY: readonly MigrationRegistryEntry[] = Object.freeze([
  freezeEntry({
    stream: 'tenant',
    file: '0001_dashboard_foundation.sql',
    relativePath: 'tenant/0001_dashboard_foundation.sql',
    ordinal: 1,
    style: 'additive',
    workId: 'MIG-F2-TENANT-DASHBOARD',
    sha256: 'c0a0e0eba0fbccab14bf326d7c4630cd5ef07195a01fa3b74714de9727245215',
    byteLength: 7660,
    advisoryLockKey: 'lu:tenant:0001-dashboard',
    siteAdvisoryKeyPrefix: 'lu:tenant:0001-dashboard',
    tenantSiteAdvisoryLockPrefix: TENANT_SITE_ADVISORY_LOCK_PREFIX,
    tenantStreamCommonAdvisoryLockPrefix: TENANT_COMMON_ADVISORY_LOCK_PREFIX,
    targetSchema: TENANT_SCHEMA,
    targetTables: [
      'lu_management',
      'lu_laboratory',
      'lu_equipment',
      'lu_equipment_unit',
      'lu_management_plan',
      'lu_notification',
    ],
    touchedTables: [
      'lu_management',
      'lu_laboratory',
      'lu_equipment',
      'lu_equipment_unit',
      'lu_management_plan',
      'lu_notification',
    ],
    expectedCreateTableCount: 6,
    expectedIndexNames: [
      'ux_lu_management_site_code',
      'ux_lu_management_active_type',
      'ux_lu_laboratory_site_code',
      'ix_lu_equipment_site_name',
      'ux_lu_equipment_unit_site_inventory',
      'ix_lu_equipment_unit_site_lab',
      'ux_lu_management_plan_site_management_unit',
      'ix_lu_management_plan_site_management_phase',
      'ix_lu_notification_site_unread',
    ],
    allowedDropConstraints: [],
    legacyPolicy: true,
    strictPolicy: false,
    finalManifestId: 'final-tenant',
  }),
  freezeEntry({
    stream: 'tenant',
    file: '0002_catalogs.sql',
    relativePath: 'tenant/0002_catalogs.sql',
    ordinal: 2,
    style: 'additive',
    workId: 'MIG-F2-TENANT-CATALOGS',
    sha256: 'fc772962a5428dba345917b258342c9be5337d60978323ce11127210eed9f698',
    byteLength: 2199,
    advisoryLockKey: 'lu:tenant:0002-catalogs',
    siteAdvisoryKeyPrefix: 'lu:tenant:0002-catalogs',
    tenantSiteAdvisoryLockPrefix: TENANT_SITE_ADVISORY_LOCK_PREFIX,
    tenantStreamCommonAdvisoryLockPrefix: TENANT_COMMON_ADVISORY_LOCK_PREFIX,
    targetSchema: TENANT_SCHEMA,
    targetTables: ['lu_country', 'lu_city'],
    touchedTables: ['lu_country', 'lu_city'],
    expectedCreateTableCount: 2,
    expectedIndexNames: [
      'ux_lu_country_site_name_active',
      'ux_lu_city_site_country_name_active',
      'ix_lu_city_site_country',
    ],
    allowedDropConstraints: [],
    legacyPolicy: true,
    strictPolicy: false,
    finalManifestId: 'final-tenant',
  }),
  freezeEntry({
    stream: 'tenant',
    file: '0003_academic_laboratories.sql',
    relativePath: 'tenant/0003_academic_laboratories.sql',
    ordinal: 3,
    style: 'additive',
    workId: 'MIG-F2-TENANT-LABORATORIES',
    sha256: 'a35271ed8383c0fc738046e9f6524c6b2067c714256ecf6887d98d1f40541f0e',
    byteLength: 1697,
    advisoryLockKey: 'lu:tenant:0003-laboratories',
    siteAdvisoryKeyPrefix: 'lu:tenant:0003-laboratories',
    tenantSiteAdvisoryLockPrefix: TENANT_SITE_ADVISORY_LOCK_PREFIX,
    tenantStreamCommonAdvisoryLockPrefix: TENANT_COMMON_ADVISORY_LOCK_PREFIX,
    targetSchema: TENANT_SCHEMA,
    // 0003's CREATE TABLE IF NOT EXISTS public.lu_laboratory is a no-op after
    // 0001 (the table already exists). The migration keeps the statement for
    // idempotency and only adds three indexes to lu_laboratory. The validator
    // counts every CREATE TABLE IF NOT EXISTS statement regardless of whether
    // it materialises a new table, so the literal count (1) is the value
    // asserted here. `targetTables` stays empty because no NEW table is added.
    targetTables: [],
    touchedTables: ['lu_laboratory'],
    expectedCreateTableCount: 1,
    expectedIndexNames: [
      'ux_lu_laboratory_site_code_active',
      'ux_lu_laboratory_site_faculty_name_active',
      'ix_lu_laboratory_site_faculty',
    ],
    allowedDropConstraints: [],
    legacyPolicy: true,
    strictPolicy: false,
    finalManifestId: 'final-tenant',
  }),
  freezeEntry({
    stream: 'tenant',
    file: '0004_equipment_notes.sql',
    relativePath: 'tenant/0004_equipment_notes.sql',
    ordinal: 4,
    style: 'additive',
    workId: 'MIG-F2-TENANT-EQUIPMENT-NOTES',
    sha256: 'a998a8cd7cbec3477b6a9b76fd70573592cfac61cb733f091e0daf63c2747e6f',
    byteLength: 1571,
    advisoryLockKey: 'lu:tenant:0004-equipment-notes',
    siteAdvisoryKeyPrefix: 'lu:tenant:0004-equipment-notes',
    tenantSiteAdvisoryLockPrefix: TENANT_SITE_ADVISORY_LOCK_PREFIX,
    tenantStreamCommonAdvisoryLockPrefix: TENANT_COMMON_ADVISORY_LOCK_PREFIX,
    targetSchema: TENANT_SCHEMA,
    targetTables: ['lu_equipment_note'],
    touchedTables: ['lu_equipment', 'lu_equipment_note'],
    expectedCreateTableCount: 1,
    expectedIndexNames: [
      'ux_lu_equipment_site_catalog_code_active',
      'ux_lu_equipment_site_name_model_active',
      'ix_lu_equipment_note_site_equipment',
    ],
    allowedDropConstraints: [],
    legacyPolicy: true,
    strictPolicy: false,
    finalManifestId: 'final-tenant',
  }),
  freezeEntry({
    stream: 'tenant',
    file: '0005_equipment_units_history.sql',
    relativePath: 'tenant/0005_equipment_units_history.sql',
    ordinal: 5,
    style: 'additive',
    workId: 'MIG-F2-TENANT-EQUIPMENT-UNITS',
    sha256: 'fda88547e10167598b0974c54f2fc83992c865d81e4992bbe3c141200a6e3156',
    byteLength: 2719,
    advisoryLockKey: 'lu:tenant:0005-equipment-units',
    siteAdvisoryKeyPrefix: 'lu:tenant:0005-equipment-units',
    tenantSiteAdvisoryLockPrefix: TENANT_SITE_ADVISORY_LOCK_PREFIX,
    tenantStreamCommonAdvisoryLockPrefix: TENANT_COMMON_ADVISORY_LOCK_PREFIX,
    targetSchema: TENANT_SCHEMA,
    targetTables: ['lu_equipment_unit_state_history'],
    touchedTables: ['lu_equipment_unit', 'lu_equipment_unit_state_history'],
    expectedCreateTableCount: 1,
    expectedIndexNames: [
      'ix_lu_equipment_unit_history_site_unit_start',
      'ix_lu_equipment_unit_site_equipment',
    ],
    allowedDropConstraints: [],
    legacyPolicy: true,
    strictPolicy: false,
    finalManifestId: 'final-tenant',
  }),
  freezeEntry({
    stream: 'tenant',
    file: '0006_management_operations.sql',
    relativePath: 'tenant/0006_management_operations.sql',
    ordinal: 6,
    style: 'additive',
    workId: 'MIG-F2-TENANT-MANAGEMENT',
    sha256: '14329fe049fa2d5938500574ca7973ea248e4e597ca8b1996f2a361abe0b031c',
    byteLength: 663,
    advisoryLockKey: 'lu:tenant:0006-management',
    siteAdvisoryKeyPrefix: 'lu:tenant:0006-management',
    tenantSiteAdvisoryLockPrefix: TENANT_SITE_ADVISORY_LOCK_PREFIX,
    tenantStreamCommonAdvisoryLockPrefix: TENANT_COMMON_ADVISORY_LOCK_PREFIX,
    targetSchema: TENANT_SCHEMA,
    // 0006 is ACL-only: no new tables, no new indexes, only GRANTs.
    targetTables: [],
    touchedTables: ['lu_management', 'lu_management_plan'],
    expectedCreateTableCount: 0,
    expectedIndexNames: [],
    allowedDropConstraints: [],
    legacyPolicy: true,
    strictPolicy: false,
    finalManifestId: 'final-tenant',
  }),
  freezeEntry({
    stream: 'tenant',
    file: '0007_verifications.sql',
    relativePath: 'tenant/0007_verifications.sql',
    ordinal: 7,
    style: 'additive',
    workId: 'MIG-F2-TENANT-VERIFICATIONS',
    sha256: '7ebc6f721f8d35b24082a6260a9379b72b4e28e3ae7714da27d748332f58d15a',
    byteLength: 3462,
    advisoryLockKey: 'lu:tenant:0007-verifications',
    siteAdvisoryKeyPrefix: 'lu:tenant:0007-verifications',
    tenantSiteAdvisoryLockPrefix: TENANT_SITE_ADVISORY_LOCK_PREFIX,
    tenantStreamCommonAdvisoryLockPrefix: TENANT_COMMON_ADVISORY_LOCK_PREFIX,
    targetSchema: TENANT_SCHEMA,
    targetTables: [
      'lu_verification_check_item',
      'lu_verification',
      'lu_verification_check_result',
      'lu_verification_fault',
    ],
    touchedTables: [
      'lu_verification_check_item',
      'lu_verification',
      'lu_verification_check_result',
      'lu_verification_fault',
    ],
    expectedCreateTableCount: 4,
    expectedIndexNames: [
      'ux_lu_verification_site_management_unit',
      'ix_lu_verification_site_lab_date',
      'ix_lu_verification_fault_site_verification',
    ],
    allowedDropConstraints: [],
    legacyPolicy: true,
    strictPolicy: false,
    finalManifestId: 'final-tenant',
  }),
  freezeEntry({
    stream: 'tenant',
    file: '0008_requests.sql',
    relativePath: 'tenant/0008_requests.sql',
    ordinal: 8,
    style: 'additive',
    workId: 'MIG-F2-TENANT-REQUESTS',
    sha256: 'd75761853342882eb2cb6339addbe5da0cddc6337873c874499395a103556c20',
    byteLength: 3563,
    advisoryLockKey: 'lu:tenant:0008-requests',
    siteAdvisoryKeyPrefix: 'lu:tenant:0008-requests',
    tenantSiteAdvisoryLockPrefix: TENANT_SITE_ADVISORY_LOCK_PREFIX,
    tenantStreamCommonAdvisoryLockPrefix: TENANT_COMMON_ADVISORY_LOCK_PREFIX,
    targetSchema: TENANT_SCHEMA,
    targetTables: ['lu_request', 'lu_request_equipment_unit'],
    touchedTables: ['lu_request', 'lu_request_equipment_unit'],
    expectedCreateTableCount: 2,
    expectedIndexNames: [
      'ix_lu_request_site_management_status',
      'ix_lu_request_site_unit',
      'ix_lu_request_site_priority',
      'ux_lu_request_active_technical_management_unit',
      'ix_lu_request_unit_site_request',
    ],
    allowedDropConstraints: [],
    legacyPolicy: true,
    strictPolicy: false,
    finalManifestId: 'final-tenant',
  }),
  freezeEntry({
    stream: 'tenant',
    file: '0009_maintenances.sql',
    relativePath: 'tenant/0009_maintenances.sql',
    ordinal: 9,
    style: 'additive',
    workId: 'MIG-F2-TENANT-MAINTENANCES',
    sha256: 'f99e75f87d2676739d321114b3b7f81127c9270102465286745820e69ff04d8b',
    byteLength: 6151,
    advisoryLockKey: 'lu:tenant:0009-maintenances',
    siteAdvisoryKeyPrefix: 'lu:tenant:0009-maintenances',
    tenantSiteAdvisoryLockPrefix: TENANT_SITE_ADVISORY_LOCK_PREFIX,
    tenantStreamCommonAdvisoryLockPrefix: TENANT_COMMON_ADVISORY_LOCK_PREFIX,
    targetSchema: TENANT_SCHEMA,
    targetTables: [
      'lu_maintenance',
      'lu_maintenance_task',
      'lu_maintenance_participant',
      'lu_maintenance_request',
      'lu_maintenance_cost',
    ],
    touchedTables: [
      'lu_maintenance',
      'lu_maintenance_task',
      'lu_maintenance_participant',
      'lu_maintenance_request',
      'lu_maintenance_cost',
    ],
    expectedCreateTableCount: 5,
    expectedIndexNames: [
      'ix_lu_maintenance_site_management_status',
      'ix_lu_maintenance_site_unit',
      'ux_lu_maintenance_active_plan_unit',
      'ix_lu_maintenance_task_site_parent',
      'ix_lu_maintenance_participant_site_parent',
      'ix_lu_maintenance_cost_site_parent',
    ],
    allowedDropConstraints: [],
    legacyPolicy: true,
    strictPolicy: false,
    finalManifestId: 'final-tenant',
  }),
  freezeEntry({
    stream: 'tenant',
    file: '0010_departures.sql',
    relativePath: 'tenant/0010_departures.sql',
    ordinal: 10,
    style: 'additive',
    workId: 'MIG-F2-TENANT-DEPARTURES',
    sha256: 'b5fdb1ff07aa1e0021b2f60382e434ff2410576a2dec2f0f048e5c264ea75240',
    byteLength: 2857,
    advisoryLockKey: 'lu:tenant:0010-departures',
    siteAdvisoryKeyPrefix: 'lu:tenant:0010-departures',
    tenantSiteAdvisoryLockPrefix: TENANT_SITE_ADVISORY_LOCK_PREFIX,
    tenantStreamCommonAdvisoryLockPrefix: TENANT_COMMON_ADVISORY_LOCK_PREFIX,
    targetSchema: TENANT_SCHEMA,
    targetTables: ['lu_departure', 'lu_departure_item'],
    touchedTables: ['lu_departure', 'lu_departure_item'],
    expectedCreateTableCount: 2,
    expectedIndexNames: [
      'ix_lu_departure_site_management_status',
      'ix_lu_departure_site_unit',
      'ix_lu_departure_item_site_parent',
    ],
    allowedDropConstraints: [],
    legacyPolicy: true,
    strictPolicy: false,
    finalManifestId: 'final-tenant',
  }),
  freezeEntry({
    stream: 'tenant',
    file: '0011_acquisition_costs.sql',
    relativePath: 'tenant/0011_acquisition_costs.sql',
    ordinal: 11,
    style: 'additive',
    workId: 'MIG-F2-TENANT-ACQUISITIONS',
    sha256: '629c09ddc41151de92557986905c9a4dba88136db1f818cbdbcb1bd242cf3504',
    byteLength: 1653,
    advisoryLockKey: 'lu:tenant:0011-acquisitions',
    siteAdvisoryKeyPrefix: 'lu:tenant:0011-acquisitions',
    tenantSiteAdvisoryLockPrefix: TENANT_SITE_ADVISORY_LOCK_PREFIX,
    tenantStreamCommonAdvisoryLockPrefix: TENANT_COMMON_ADVISORY_LOCK_PREFIX,
    targetSchema: TENANT_SCHEMA,
    targetTables: ['lu_request_cost'],
    touchedTables: ['lu_request_cost'],
    expectedCreateTableCount: 1,
    expectedIndexNames: ['ix_lu_request_cost_site_parent'],
    allowedDropConstraints: [],
    legacyPolicy: true,
    strictPolicy: false,
    finalManifestId: 'final-tenant',
  }),
  freezeEntry({
    stream: 'tenant',
    file: '0012_people.sql',
    relativePath: 'tenant/0012_people.sql',
    ordinal: 12,
    style: 'additive',
    workId: 'MIG-F2-TENANT-PEOPLE',
    sha256: 'd431f371206aee7e365a45e65be639b5f5988955264f6a82c8b911299e1a93bf',
    byteLength: 2001,
    advisoryLockKey: 'lu:tenant:0012-people',
    siteAdvisoryKeyPrefix: 'lu:tenant:0012-people',
    tenantSiteAdvisoryLockPrefix: TENANT_SITE_ADVISORY_LOCK_PREFIX,
    tenantStreamCommonAdvisoryLockPrefix: TENANT_COMMON_ADVISORY_LOCK_PREFIX,
    targetSchema: TENANT_SCHEMA,
    targetTables: ['lu_person'],
    touchedTables: ['lu_person'],
    expectedCreateTableCount: 1,
    expectedIndexNames: [
      'ux_lu_person_site_actor_code',
      'ix_lu_person_site_status_type',
      'ix_lu_person_site_email',
    ],
    allowedDropConstraints: [],
    legacyPolicy: true,
    strictPolicy: false,
    finalManifestId: 'final-tenant',
  }),
  freezeEntry({
    stream: 'tenant',
    file: '0013_migration_history.sql',
    relativePath: 'tenant/0013_migration_history.sql',
    ordinal: 13,
    style: 'additive',
    workId: 'MIG-F2-TENANT-MIGRATION-HISTORY',
    // Canonical-LF digest and length of the current on-disk file. This is the
    // only place where W9 re-pins 0013; historical tenant entries above stay
    // byte-identical (regression guard in the registry test suite).
    sha256: '5a2c7e89054b58d57e9b2958c60ac259c1a0f8cfdbca8cc10403d91728ea16d8',
    byteLength: 2640,
    advisoryLockKey: 'lu:tenant:0013-migration-history',
    siteAdvisoryKeyPrefix: 'lu:tenant:0013-migration-history',
    tenantSiteAdvisoryLockPrefix: TENANT_SITE_ADVISORY_LOCK_PREFIX,
    tenantStreamCommonAdvisoryLockPrefix: TENANT_COMMON_ADVISORY_LOCK_PREFIX,
    targetSchema: TENANT_SCHEMA,
    targetTables: [...TENANT_MIGRATION_HISTORY_TABLES],
    touchedTables: [...TENANT_MIGRATION_HISTORY_TABLES],
    expectedCreateTableCount: 1,
    expectedIndexNames: ['ix_lu_migration_history_applied_at'],
    allowedDropConstraints: [],
    legacyPolicy: false,
    strictPolicy: true,
    finalManifestId: 'final-tenant',
  }),
]);

/**
 * Backward-compatible union of the **legacy 0001-0003** entries that the
 * original tooling shipped. Intentionally narrow to preserve the historical
 * invariant of `listRegisteredMigrations().length === 3`.
 */
export const MIGRATION_REGISTRY: readonly MigrationRegistryEntry[] = Object.freeze(
  CONTROL_PLANE_REGISTRY.slice(0, 3).map((e) => Object.freeze(e)),
);

export class MigrationRegistryError extends Error {
  constructor(message: string) {
    super(message);
    this.name = 'MigrationRegistryError';
  }
}

export interface ValidatedMigration {
  readonly entry: MigrationRegistryEntry;
  readonly plan: ReturnType<typeof buildMigrationPlan>;
}

/**
 * Validate that a migration file is registered and that its bytes match the
 * pinned digest exactly. The relativePath match is checked first (the
 * canonical identifier); the basename fallback exists only for the legacy
 * 0001-0003 entries that pre-date stream-aware naming.
 *
 * Stream gate (MIG-001-F2-W13B): when the caller supplies `options.stream`,
 * the requested stream MUST equal the resolved registry entry's stream.
 * The check runs BEFORE the SHA-256 / byte-length / analyzer stages and
 * BEFORE any DB-touching helper can open a `pg.Client`, so a control-plane
 * connection string can never be silently routed at a tenant migration or
 * vice versa. The error identifies the requested stream, the expected
 * stream and the file name (all of which are public, registry-derived
 * values); the canonical digest, byte length, env var names, DSN, password
 * and reference are NEVER included.
 */
export function validatePinnedMigration(
  filePath: string,
  content: Buffer | string,
  options?: { readonly stream?: MigrationStream },
): ValidatedMigration {
  const buf = Buffer.isBuffer(content) ? content : Buffer.from(content, 'utf8');
  assertCanonicalBytes(buf, filePath);

  const normalizedPath = filePath.split(/[\\/]/).join('/');
  const normalizedTail = normalizedPath.replace(/^.*?migrations\//, '');

  let entry: MigrationRegistryEntry | undefined =
    CONTROL_PLANE_REGISTRY.find((e) => e.relativePath === normalizedTail) ??
    TENANT_REGISTRY.find((e) => e.relativePath === normalizedTail);
  if (entry === undefined) {
    // basename fallback for legacy entries (0001-0003 live at migrations/ root)
    const fileName = filePath.split(/[\\/]/).pop() ?? filePath;
    entry =
      CONTROL_PLANE_REGISTRY.find((e) => e.file === fileName) ??
      TENANT_REGISTRY.find((e) => e.file === fileName);
  }
  if (entry === undefined) {
    throw new MigrationRegistryError(
      `Migration file "${filePath}" is not registered in the migration registry. ` +
        'Only registry-pinned migrations may be analyzed or executed.',
    );
  }

  // Stream gate (MIG-001-F2-W13B): when the caller declared a stream, it
  // must equal the resolved registry entry's stream. The check runs BEFORE
  // any hash / length / analyzer / DB work so a wrong stream never reaches
  // PostgreSQL. The error message identifies the requested stream, the
  // expected (registry) stream and the public file name only.
  const requestedStream = options?.stream;
  if (requestedStream !== undefined && requestedStream !== entry.stream) {
    const fileName = entry.file;
    throw new MigrationRegistryError(
      `Migration "${fileName}" is registered for stream "${entry.stream}", ` +
        `but the caller requested stream "${requestedStream}". ` +
        'Refusing to analyze or execute a migration against the wrong stream ' +
        'before any hash, analyzer or database work is performed.',
    );
  }

  const hash = sha256Hex(buf);
  if (hash !== entry.sha256) {
    throw new MigrationRegistryError(
      `SHA-256 mismatch for registered migration "${entry.relativePath}". ` +
        `Expected ${entry.sha256}, got ${hash}. The file has been modified, corrupted, or is not the approved migration.`,
    );
  }
  const canonicalBytes = canonicalByteLength(buf);
  if (canonicalBytes !== entry.byteLength) {
    throw new MigrationRegistryError(
      `Byte length mismatch for registered migration "${entry.relativePath}". ` +
        `Expected ${entry.byteLength}, got ${canonicalBytes}.`,
    );
  }

  const analyzeOptions: AnalyzeOptions = {
    legacyPolicy: entry.legacyPolicy,
    strictPolicy: entry.strictPolicy,
    allowedDropConstraints: [...entry.allowedDropConstraints],
  };
  const plan = buildMigrationPlan(filePath, buf, [...entry.targetTables], {
    allowedTables: [...entry.targetTables],
    touchedTables: [...entry.touchedTables],
    options: analyzeOptions,
    stream: entry.stream,
  });
  return { entry, plan };
}

/** Snapshot of every registered entry. */
export function listRegisteredMigrations(): readonly MigrationRegistryEntry[] {
  return [...MIGRATION_REGISTRY];
}

export function listControlPlaneMigrations(): readonly MigrationRegistryEntry[] {
  return [...CONTROL_PLANE_REGISTRY];
}

export function listTenantMigrations(): readonly MigrationRegistryEntry[] {
  return [...TENANT_REGISTRY];
}

export function findRegistryEntry(fileName: string): MigrationRegistryEntry | undefined {
  return MIGRATION_REGISTRY.find((e) => e.file === fileName);
}

export function findRegistryEntryByStream(
  stream: MigrationStream,
  file: string,
): MigrationRegistryEntry | undefined {
  const list = stream === 'control-plane' ? CONTROL_PLANE_REGISTRY : TENANT_REGISTRY;
  return list.find((e) => e.file === file);
}

export function findRegistryEntryByRelativePath(
  relativePath: string,
): MigrationRegistryEntry | undefined {
  return (
    CONTROL_PLANE_REGISTRY.find((e) => e.relativePath === relativePath) ??
    TENANT_REGISTRY.find((e) => e.relativePath === relativePath)
  );
}

export function findRegistryEntryByOrdinal(
  stream: MigrationStream,
  ordinal: number,
): MigrationRegistryEntry | undefined {
  const list = stream === 'control-plane' ? CONTROL_PLANE_REGISTRY : TENANT_REGISTRY;
  return list.find((e) => e.ordinal === ordinal);
}

/**
 * Resolve a name (basename, relative path, or qualified path) to a registered
 * entry. The unambiguous `(stream, relativePath)` lookup is tried first
 * (relativePath match within the requested stream); the basename fallback is
 * used only when a single stream matches. An ambiguous basename that collides
 * across streams is REJECTED so the analyzer cannot silently misroute a file.
 */
export function resolveMigrationSelector(name: string): MigrationRegistryEntry | undefined {
  if (name.includes('/') || name.includes('\\')) {
    const normalized = name.split(/[\\/]/).join('/');
    return findRegistryEntryByRelativePath(normalized);
  }
  const cpMatch = CONTROL_PLANE_REGISTRY.find((e) => e.file === name);
  const tnMatch = TENANT_REGISTRY.find((e) => e.file === name);
  if (cpMatch !== undefined && tnMatch !== undefined) {
    throw new MigrationRegistryError(
      `Migration basename "${name}" is ambiguous across control-plane and tenant streams; ` +
        'specify a relative path (e.g. "tenant/0013_migration_history.sql") instead.',
    );
  }
  return cpMatch ?? tnMatch;
}

export { sha256Hex };

export interface CanonicalBody {
  readonly body: string;
  readonly derivedFromSha256: string;
  readonly createTableCount: number;
  readonly uniqueIndexCount: number;
  readonly plainIndexCount: number;
}

/**
 * Derive the canonical execution body for a pinned registered migration.
 *
 *   - Strips leading comments and the outer BEGIN/COMMIT envelope.
 *   - Replaces `CREATE TABLE/INDEX IF NOT EXISTS` with the plain form to
 *     count and validate the pinned object surface. Bootstrap and strict
 *     entries execute that stripped body (a pre-existing object fails
 *     closed); legacy entries execute the envelope body with IF NOT EXISTS
 *     preserved, because their historical SQL relies on idempotent
 *     redeclaration across ordinals.
 *   - Returns create / unique-index / plain-index counts and the source
 *     digest for audit purposes.
 *
 * The body is fed verbatim to the runner inside its own controlled transaction.
 */
export function deriveCanonicalBootstrapSql(
  entry: MigrationRegistryEntry,
  sql: string,
): CanonicalBody {
  // Sanitize first so envelope + IF NOT EXISTS counts operate on
  // inspectable text only. We allow dollar bodies for legacy / strict
  // entries; the bootstrap (0001-0003) policy never sees them.
  const legacyOrStrict = entry.legacyPolicy || entry.strictPolicy;
  const { code, issues } = sanitizeSql(sql, legacyOrStrict);
  if (issues.length > 0) {
    throw new MigrationRegistryError(
      `Migration "${entry.relativePath}" has structural issues: ${issues.join('; ')}.`,
    );
  }
  // Statement-level envelope assertion: exactly one BEGIN and one COMMIT,
  // no ROLLBACK/SAVEPOINT/END anywhere.
  const statements = code
    .split(';')
    .map((s) => s.trim())
    .filter((s) => s !== '');
  if (statements.length === 0) {
    throw new MigrationRegistryError(`Migration "${entry.relativePath}" has no statements.`);
  }
  const first = statements[0]?.toUpperCase() ?? '';
  const last = statements[statements.length - 1]?.toUpperCase() ?? '';
  const beginCount = statements.filter((s) => s.toUpperCase() === 'BEGIN').length;
  const commitCount = statements.filter((s) => s.toUpperCase() === 'COMMIT').length;
  const extraTx = statements.filter((s) => /^(ROLLBACK|SAVEPOINT|END)$/i.test(s)).length;
  const violations: string[] = [];
  if (first !== 'BEGIN') violations.push('the first statement must be BEGIN');
  if (last !== 'COMMIT') violations.push('the last statement must be COMMIT');
  if (beginCount !== 1 || commitCount !== 1) {
    violations.push('exactly one BEGIN and one COMMIT');
  }
  if (extraTx > 0) {
    violations.push('ROLLBACK/SAVEPOINT/END are not allowed inside the migration envelope');
  }
  if (violations.length > 0) {
    throw new MigrationRegistryError(
      `Migration "${entry.relativePath}" has an invalid transaction envelope: ${violations.join('; ')}.`,
    );
  }

  // Drop the initial BEGIN and final COMMIT to derive the executable body.
  // Strip SQL line comments (`-- ...`) from the head so a leading comment
  // block before BEGIN does not prevent the BEGIN match. Block comments are
  // already collapsed to spaces by sanitizeSql; line comments are stripped
  // here because they are not present in the sanitized code.
  const strippedHead = sql.replace(/^(\s*--[^\n]*\n)+/g, (match) => match.replace(/[^\n]/g, ' '));
  const beginMatch = /^\s*BEGIN\s*;\s*/i.exec(strippedHead);
  if (beginMatch === null) {
    throw new MigrationRegistryError(
      `Migration "${entry.relativePath}" does not start with a removable BEGIN statement.`,
    );
  }
  let body = strippedHead.slice(beginMatch[0].length);
  const commitMatch = /COMMIT\s*;\s*$/i.exec(body);
  if (commitMatch === null) {
    throw new MigrationRegistryError(
      `Migration "${entry.relativePath}" does not end with a removable COMMIT statement.`,
    );
  }
  body = body.slice(0, commitMatch.index);
  // Legacy (historical) entries were authored as idempotent redeclarations
  // (e.g. tenant 0003 re-declares lu_laboratory from tenant 0001), so their
  // IF NOT EXISTS clauses are semantic and must survive into the executed
  // body. Counting and index-name validation below still run on the
  // stripped form for every policy.
  const envelopeBody = body;

  let createTableCount = 0;
  body = body.replace(/\bCREATE\s+TABLE\s+IF\s+NOT\s+EXISTS\b/gi, () => {
    createTableCount += 1;
    return 'CREATE TABLE';
  });
  let uniqueIndexCount = 0;
  body = body.replace(/\bCREATE\s+UNIQUE\s+INDEX\s+IF\s+NOT\s+EXISTS\b/gi, () => {
    uniqueIndexCount += 1;
    return 'CREATE UNIQUE INDEX';
  });
  let plainIndexCount = 0;
  body = body.replace(/\bCREATE\s+INDEX\s+IF\s+NOT\s+EXISTS\b/gi, () => {
    plainIndexCount += 1;
    return 'CREATE INDEX';
  });

  if (entry.expectedCreateTableCount >= 0 && createTableCount !== entry.expectedCreateTableCount) {
    throw new MigrationRegistryError(
      `Migration "${entry.relativePath}" declares ${createTableCount} CREATE TABLE IF NOT EXISTS ` +
        `statements; ${entry.expectedCreateTableCount} were expected.`,
    );
  }

  const indexNamePattern = /\bCREATE\s+(?:UNIQUE\s+)?INDEX\s+(\S+)/gi;
  const seenIndexNames = new Set<string>();
  for (const match of body.matchAll(indexNamePattern)) {
    const name = match[1];
    if (name !== undefined) seenIndexNames.add(name.toLowerCase());
  }
  const expectedNames = new Set(entry.expectedIndexNames.map((n) => n.toLowerCase()));
  for (const name of expectedNames) {
    if (!seenIndexNames.has(name)) {
      throw new MigrationRegistryError(
        `Migration "${entry.relativePath}" is missing expected index "${name}" after canonical derivation.`,
      );
    }
  }
  for (const name of seenIndexNames) {
    if (!expectedNames.has(name)) {
      throw new MigrationRegistryError(
        `Migration "${entry.relativePath}" contains unexpected index "${name}" after canonical derivation.`,
      );
    }
  }

  return {
    body: entry.legacyPolicy ? envelopeBody : body,
    derivedFromSha256: entry.sha256,
    createTableCount,
    uniqueIndexCount,
    plainIndexCount,
  };
}

/** Compose the file on-disk path for a registry entry. */
export function resolveEntryPath(entry: MigrationRegistryEntry, baseDir: string): string {
  const segments = entry.relativePath.split('/');
  const isLegacyRoot = segments.length === 1;
  const base = isLegacyRoot ? baseDir : join(baseDir, segments[0]!);
  return join(base, segments[segments.length - 1]!);
}

export const __DEFAULT_STREAM = DEFAULT_STREAM;
