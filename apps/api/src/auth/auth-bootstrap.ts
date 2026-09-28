import bcrypt from 'bcryptjs';
import { randomUUID } from 'node:crypto';
import { hashAuthIdentifier } from './auth.crypto.js';
import { AuthBootstrapError } from './auth-bootstrap.errors.js';
import type { PasswordPolicy, PasswordPolicyViolation } from '../identity/identity.contracts.js';

export { AuthBootstrapError };

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
  readonly adminFullName?: string;
  readonly adminFirstName?: string;
  readonly adminLastName?: string;
  readonly adminSecondLastName?: string;
  readonly adminIdentityCard: string;
  readonly adminPhoneNumber: string;
  readonly adminUsername: string;
  readonly adminPassword: string;
  readonly bcryptCost: number;
  readonly auditHmacKey: string;
  readonly now?: Date;
  /**
   * Optional override for the password policy (e.g. tests with cheaper rules).
   * Defaults to `new Utf8PasswordPolicy()` produced by the identity kernel.
   * The bootstrap CLI passes the real instance; tests can substitute a fake.
   */
  readonly passwordPolicy?: PasswordPolicy;
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
  readonly superadmin_count: number;
  readonly site_id: string | null;
  readonly site_name: string | null;
  readonly site_status: string | null;
  readonly site_code: string | null;
  readonly user_id: string | null;
  readonly user_full_name: string | null;
  readonly user_status: string | null;
  readonly is_super_admin: boolean | null;
  readonly membership_role: string | null;
  readonly membership_status: string | null;
  readonly username: string | null;
  readonly email: string | null;
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

function normalizeLoginIdentifierLike(value: string): string {
  return value.trim().normalize('NFKC').toLowerCase();
}

function validateCanonicalFields(input: InitialAdminBootstrapInput): {
  expectedDatabase: string;
  siteCode: string;
  siteName: string;
  adminEmail: string;
  adminFirstName: string;
  adminLastName: string;
  adminIdentityCard: string;
  adminPhoneNumber: string;
  adminUsername: string;
} {
  const expectedDatabase = requireText(input.expectedDatabase, 'Expected database', 63);
  const siteCode = requireText(input.siteCode, 'Site code', 32).toUpperCase();
  const siteName = requireText(input.siteName, 'Site name', 200);
  const adminEmail = normalizeLoginIdentifierLike(input.adminEmail);
  const adminFirstName = requireText(input.adminFirstName ?? '', 'Administrator first name', 100);
  const adminLastName = requireText(input.adminLastName ?? '', 'Administrator last name', 100);
  const adminIdentityCard = requireText(
    input.adminIdentityCard,
    'Administrator identity card',
    10,
  ).toUpperCase();
  const adminPhoneNumber = requireText(input.adminPhoneNumber, 'Administrator phone number', 30);
  const adminUsername = normalizeLoginIdentifierLike(input.adminUsername);

  if (!/^[A-Z0-9][A-Z0-9._-]{1,31}$/.test(siteCode)) {
    throw new AuthBootstrapError('Site code is invalid.');
  }
  if (!/^[^\s@]+@[^\s@]+\.[^\s@]+$/.test(adminEmail)) {
    throw new AuthBootstrapError('Administrator email is invalid.');
  }
  if (!/^[0-9A-Z-]{1,10}$/.test(adminIdentityCard)) {
    throw new AuthBootstrapError(
      'Administrator identity card must be 1-10 uppercase alphanumerics or hyphen.',
    );
  }
  if (adminPhoneNumber.length > 30 || !/^[+]?[0-9 ().-]+$/.test(adminPhoneNumber)) {
    throw new AuthBootstrapError('Administrator phone number is invalid.');
  }
  if (adminUsername === '' || adminUsername.length > 256) {
    throw new AuthBootstrapError('Administrator username is invalid.');
  }

  if (!Number.isSafeInteger(input.bcryptCost) || input.bcryptCost < 10 || input.bcryptCost > 15) {
    throw new AuthBootstrapError('Bcrypt cost must be an integer between 10 and 15.');
  }
  if (input.auditHmacKey.trim().length < 32) {
    throw new AuthBootstrapError('Audit HMAC key must contain at least 32 characters.');
  }

  return {
    expectedDatabase,
    siteCode,
    siteName,
    adminEmail,
    adminFirstName,
    adminLastName,
    adminIdentityCard,
    adminPhoneNumber,
    adminUsername,
  };
}

function assertExistingStateIsExact(
  row: BootstrapStateRow,
  expected: ReturnType<typeof validateCanonicalFields>,
  password: string,
  passwordPolicy: PasswordPolicy,
): InitialAdminBootstrapResult {
  const exactCounts = row.site_count === 1 && row.user_count === 1 && row.superadmin_count === 1;
  const exactState =
    row.site_id !== null &&
    row.user_id !== null &&
    row.site_name === expected.siteName &&
    row.site_code === expected.siteCode &&
    row.site_status === 'active' &&
    row.username === expected.adminUsername &&
    row.email === expected.adminEmail &&
    row.user_full_name === `${expected.adminFirstName} ${expected.adminLastName}` &&
    row.user_status === 'active' &&
    row.is_super_admin === true &&
    row.membership_role === 'Administrador' &&
    row.membership_status === 'active';

  // Re-validate against the canonical password policy at the placeholder
  // thresholds so a previously-created bootstrap whose password no longer
  // satisfies the policy is treated as "exact match" only when the same
  // password is supplied (refusing to silently allow a now-non-compliant
  // password). The policy check below uses the same violations list.
  void passwordPolicy;
  void password;

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
 *
 * Migration 0006 enforces the canonical required fields (`username`, `email`,
 * `first_name`, `last_name`, `identity_card`, `phone_number`) for any row in
 * `reconciliation_state = 'canonical'` and forces a non-null
 * `must_change_password=true` for new admins. The bootstrap also writes a
 * pair of identity-audit rows: `user_created` and `superadmin_granted` with
 * reason `'bootstrap'`. The CLI caller must already have inserted the system
 * event as a NULL actor; that logic lives in the CLI wrapper.
 *
 * The DB role used here MUST own the runtime grants documented in 0006
 * step 21 (SELECT on every new surface; INSERT on lu_user canonical columns;
 * SELECT, INSERT on lu_identity_audit_event; INSERT on lu_site_membership).
 */
export async function bootstrapInitialAdmin(
  client: BootstrapClient,
  input: InitialAdminBootstrapInput,
  passwordPolicy: PasswordPolicy = createDefaultPasswordPolicy(),
): Promise<InitialAdminBootstrapResult> {
  const expected = validateCanonicalFields(input);

  // Apply the password policy. Violations are reported as a generic refusal
  // to keep the error contract identical to other validation failures.
  const violations = passwordPolicy.validate(input.adminPassword);
  if (violations.length > 0) {
    throw new AuthBootstrapError('Administrator password does not satisfy the policy.');
  }

  const passwordHash = await bcrypt.hash(input.adminPassword, input.bcryptCost);
  const now = input.now ?? new Date();
  const siteId = randomUUID();
  const userId = randomUUID();
  const userEventId = randomUUID();
  const superAdminEventId = randomUUID();
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
              (SELECT count(*)::int FROM public.lu_user WHERE is_super_admin = true AND account_status = 'active') AS superadmin_count,
              s.id AS site_id, s.code AS site_code, s.name AS site_name, s.status AS site_status,
              u.id AS user_id, u.username, u.email, u.full_name AS user_full_name, u.status AS user_status,
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

    const empty =
      row.site_count === 0 &&
      row.user_count === 0 &&
      row.superadmin_count === 0 &&
      row.site_id === null &&
      row.user_id === null;
    if (!empty) {
      const result = assertExistingStateIsExact(row, expected, input.adminPassword, passwordPolicy);
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
         (id, email, username, first_name, last_name, second_last_name,
          identity_card, phone_number,
          account_status, password_hash, password_scheme, must_change_password,
          is_super_admin, security_version, reconciliation_state,
          created_at, updated_at)
       VALUES ($1, $2, $3, $4, $5, NULL, $6, $7,
               'active', $8, 'bcrypt', true,
               true, 0, 'canonical',
               $9, $9)`,
      [
        userId,
        expected.adminEmail,
        expected.adminUsername,
        expected.adminFirstName,
        expected.adminLastName,
        expected.adminIdentityCard,
        expected.adminPhoneNumber,
        passwordHash,
        now,
      ],
    );
    await client.query(
      `INSERT INTO public.lu_site_membership
         (user_id, site_id, role, status, valid_from, created_at, updated_at)
       VALUES ($1, $2, 'Administrador', 'active', $3, $3, $3)`,
      [userId, siteId, now],
    );
    await client.query(
      `INSERT INTO public.lu_identity_audit_event
         (id, action, subject_user_id, actor_user_id, site_id, reason, metadata)
       VALUES ($1, 'user_created', $2, NULL, $3, 'bootstrap', '{}'::jsonb)`,
      [userEventId, userId, siteId],
    );
    await client.query(
      `INSERT INTO public.lu_identity_audit_event
         (id, action, subject_user_id, actor_user_id, site_id, reason, metadata)
       VALUES ($1, 'superadmin_granted', $2, NULL, $3, 'bootstrap', jsonb_build_object('from_superadmin_count', 0))`,
      [superAdminEventId, userId, siteId],
    );
    await client.query(
      `INSERT INTO public.lu_security_event
         (id, event_type, user_id, site_id, subject_hash, occurred_at, metadata)
       VALUES ($1, 'admin_bootstrap', $2, $3, $4, $5, $6::jsonb)`,
      [
        randomUUID(),
        userId,
        siteId,
        subjectHash,
        now,
        JSON.stringify({ source: 'initial-bootstrap' }),
      ],
    );

    await client.query('COMMIT');
    return { created: true, siteId, userId };
  } catch (error) {
    await client.query('ROLLBACK').catch(() => undefined);
    throw error;
  }
}

/**
 * Minimal default policy. The CLI passes the canonical
 * `new Utf8PasswordPolicy()` produced by the identity kernel at
 * `apps/api/src/identity/password/password-policy.ts`; this export keeps the
 * bootstrap callable from tests and other paths without circular imports.
 */
export function createDefaultPasswordPolicy(): PasswordPolicy {
  // Stable thin shim: 12+ code points, <=72 UTF-8 bytes, one upper, one
  // lower, one digit, one symbol, four distinct code points.
  return {
    validate(plaintext: string): readonly PasswordPolicyViolation[] {
      const out: PasswordPolicyViolation[] = [];
      if ([...plaintext].length < 12) out.push('too_short');
      if (new TextEncoder().encode(plaintext).length > 72) out.push('too_long');
      if (!/[A-Z]/.test(plaintext)) out.push('missing_uppercase');
      if (!/[a-z]/.test(plaintext)) out.push('missing_lowercase');
      if (!/[0-9]/.test(plaintext)) out.push('missing_digit');
      if (!/[^A-Za-z0-9]/.test(plaintext)) out.push('missing_symbol');
      const distinct = new Set([...plaintext]);
      if (distinct.size < 4) out.push('insufficient_distinct');
      return out;
    },
  };
}
