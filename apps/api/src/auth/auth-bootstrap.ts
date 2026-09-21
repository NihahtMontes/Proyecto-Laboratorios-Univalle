import bcrypt from 'bcryptjs';
import { randomUUID } from 'node:crypto';
import { hashAuthIdentifier } from './auth.crypto.js';

export interface BootstrapClient {
  query<T = Record<string, unknown>>(
    sql: string,
    params?: unknown[],
  ): Promise<{ rows: T[]; rowCount: number | null }>;
}

export interface InitialAdminBootstrapInput {
  readonly expectedDatabase: string;
  readonly siteCode: string;
  readonly siteName: string;
  readonly adminEmail: string;
  readonly adminFullName: string;
  readonly adminPassword: string;
  readonly bcryptCost: number;
  readonly auditHmacKey: string;
  readonly now?: Date;
}

export interface InitialAdminBootstrapResult {
  readonly created: boolean;
  readonly siteId: string;
  readonly userId: string;
}

interface BootstrapStateRow {
  readonly database_name: string;
  readonly site_count: number;
  readonly user_count: number;
  readonly membership_count: number;
  readonly site_id: string | null;
  readonly site_name: string | null;
  readonly site_status: string | null;
  readonly user_id: string | null;
  readonly user_full_name: string | null;
  readonly user_status: string | null;
  readonly is_super_admin: boolean | null;
  readonly membership_role: string | null;
  readonly membership_status: string | null;
}

export class AuthBootstrapError extends Error {
  constructor(message: string) {
    super(message);
    this.name = 'AuthBootstrapError';
  }
}

function requireText(value: string, label: string, maxLength: number): string {
  const normalized = value.trim();
  const hasControlCharacter = [...normalized].some((character) => {
    const code = character.charCodeAt(0);
    return code <= 31 || code === 127;
  });
  if (normalized.length === 0 || normalized.length > maxLength || hasControlCharacter) {
    throw new AuthBootstrapError(`${label} is invalid.`);
  }
  return normalized;
}

function validateInput(input: InitialAdminBootstrapInput): {
  expectedDatabase: string;
  siteCode: string;
  siteName: string;
  adminEmail: string;
  adminFullName: string;
} {
  const expectedDatabase = requireText(input.expectedDatabase, 'Expected database', 63);
  const siteCode = requireText(input.siteCode, 'Site code', 32).toUpperCase();
  const siteName = requireText(input.siteName, 'Site name', 200);
  const adminEmail = requireText(input.adminEmail, 'Administrator email', 254).toLowerCase();
  const adminFullName = requireText(input.adminFullName, 'Administrator full name', 200);

  if (!/^[A-Z0-9][A-Z0-9._-]{1,31}$/.test(siteCode)) {
    throw new AuthBootstrapError('Site code is invalid.');
  }
  if (!/^[^\s@]+@[^\s@]+\.[^\s@]+$/.test(adminEmail)) {
    throw new AuthBootstrapError('Administrator email is invalid.');
  }
  if (
    input.adminPassword.length < 16 ||
    input.adminPassword.length > 128 ||
    !/[a-z]/.test(input.adminPassword) ||
    !/[A-Z]/.test(input.adminPassword) ||
    !/[0-9]/.test(input.adminPassword) ||
    !/[^A-Za-z0-9]/.test(input.adminPassword)
  ) {
    throw new AuthBootstrapError(
      'Administrator password must contain 16-128 characters with upper, lower, digit and symbol.',
    );
  }
  if (!Number.isSafeInteger(input.bcryptCost) || input.bcryptCost < 10 || input.bcryptCost > 15) {
    throw new AuthBootstrapError('Bcrypt cost must be an integer between 10 and 15.');
  }
  if (input.auditHmacKey.trim().length < 32) {
    throw new AuthBootstrapError('Audit HMAC key must contain at least 32 characters.');
  }

  return { expectedDatabase, siteCode, siteName, adminEmail, adminFullName };
}

function assertExistingStateIsExact(
  row: BootstrapStateRow,
  expected: ReturnType<typeof validateInput>,
): InitialAdminBootstrapResult {
  const exactCounts = row.site_count === 1 && row.user_count === 1 && row.membership_count === 1;
  const exactState =
    row.site_id !== null &&
    row.user_id !== null &&
    row.site_name === expected.siteName &&
    row.site_status === 'active' &&
    row.user_full_name === expected.adminFullName &&
    row.user_status === 'active' &&
    row.is_super_admin === true &&
    row.membership_role === 'Administrador' &&
    row.membership_status === 'active';

  if (!exactCounts || !exactState) {
    throw new AuthBootstrapError(
      'Bootstrap refused because the control plane is not empty and does not exactly match the requested initial administrator.',
    );
  }

  return { created: false, siteId: row.site_id!, userId: row.user_id! };
}

/**
 * Creates the first active site and SuperAdmin membership exactly once.
 * Existing non-empty or partially matching identity state is never modified.
 */
export async function bootstrapInitialAdmin(
  client: BootstrapClient,
  input: InitialAdminBootstrapInput,
): Promise<InitialAdminBootstrapResult> {
  const expected = validateInput(input);
  const passwordHash = await bcrypt.hash(input.adminPassword, input.bcryptCost);
  const now = input.now ?? new Date();
  const siteId = randomUUID();
  const userId = randomUUID();
  const eventId = randomUUID();
  const subjectHash = hashAuthIdentifier(input.auditHmacKey.trim(), 'subject', expected.adminEmail);

  await client.query('BEGIN ISOLATION LEVEL SERIALIZABLE');
  try {
    await client.query('SET LOCAL search_path = public, pg_catalog');
    await client.query('SELECT pg_advisory_xact_lock(hashtext($1))', [
      'lu:identity-control-plane:initial-admin-bootstrap',
    ]);

    const state = await client.query<BootstrapStateRow>(
      `SELECT pg_catalog.current_database() AS database_name,
              (SELECT count(*)::int FROM public.lu_site) AS site_count,
              (SELECT count(*)::int FROM public.lu_user) AS user_count,
              (SELECT count(*)::int FROM public.lu_site_membership) AS membership_count,
              s.id AS site_id, s.name AS site_name, s.status AS site_status,
              u.id AS user_id, u.full_name AS user_full_name, u.status AS user_status,
              u.is_super_admin,
              m.role AS membership_role, m.status AS membership_status
       FROM (SELECT 1) singleton
       LEFT JOIN public.lu_site s ON lower(s.code) = lower($1)
       LEFT JOIN public.lu_user u ON lower(u.email) = lower($2)
       LEFT JOIN public.lu_site_membership m ON m.site_id = s.id AND m.user_id = u.id`,
      [expected.siteCode, expected.adminEmail],
    );
    const row = state.rows[0];
    if (row === undefined || row.database_name !== expected.expectedDatabase) {
      throw new AuthBootstrapError(
        'Bootstrap database confirmation does not match the connected database.',
      );
    }

    const empty = row.site_count === 0 && row.user_count === 0 && row.membership_count === 0;
    if (!empty) {
      const result = assertExistingStateIsExact(row, expected);
      await client.query('COMMIT');
      return result;
    }

    await client.query(
      `INSERT INTO public.lu_site (id, code, name, status, created_at, updated_at)
       VALUES ($1, $2, $3, 'active', $4, $4)`,
      [siteId, expected.siteCode, expected.siteName, now],
    );
    await client.query(
      `INSERT INTO public.lu_user
         (id, email, full_name, password_hash, is_super_admin, status,
          security_version, created_at, updated_at)
       VALUES ($1, $2, $3, $4, true, 'active', 0, $5, $5)`,
      [userId, expected.adminEmail, expected.adminFullName, passwordHash, now],
    );
    await client.query(
      `INSERT INTO public.lu_site_membership
         (user_id, site_id, role, status, valid_from, created_at, updated_at)
       VALUES ($1, $2, 'Administrador', 'active', $3, $3, $3)`,
      [userId, siteId, now],
    );
    await client.query(
      `INSERT INTO public.lu_security_event
         (id, event_type, user_id, site_id, subject_hash, occurred_at, metadata)
       VALUES ($1, 'admin_bootstrap', $2, $3, $4, $5, $6::jsonb)`,
      [eventId, userId, siteId, subjectHash, now, JSON.stringify({ source: 'initial-bootstrap' })],
    );

    await client.query('COMMIT');
    return { created: true, siteId, userId };
  } catch (error) {
    await client.query('ROLLBACK').catch(() => undefined);
    throw error;
  }
}
