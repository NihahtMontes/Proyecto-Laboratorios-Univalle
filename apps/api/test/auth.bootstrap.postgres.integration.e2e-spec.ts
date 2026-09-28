/**
 * PostgreSQL opt-in for the one-time initial administrator bootstrap.
 *
 * Uses only synthetic example.invalid data and restores an empty identity state.
 *
 * Migración 0006 (control-plane) impone el contrato canónico para filas con
 * `reconciliation_state='canonical'`: username + email + first/last_name +
 * identity_card (regex `^[0-9A-Z-]{1,10}$`) + phone_number (7..15 dígitos).
 * `lu_user` y `lu_site_membership` tienen triggers de hard-delete prohibido
 * (step 18 de 0006); la limpieza del admin usa `SET LOCAL
 * session_replication_role = replica` dentro de la transacción para saltarlos
 * sin tocar el catálogo globalmente. `lu_identity_audit_event` es append-only
 * (step 15) y se borra en la misma ventana transaccional. Los claims
 * `lu_login_identifier` los mantiene un trigger desde `lu_user` y se limpian
 * también en el mismo scope.
 *
 * El destino acepta `GastroExample` (sandbox histórico) o la base disposable
 * `f7_<8 hex>_control` que crea `tests/e2e/stack/run-f7.mjs`; el bucle de
 * conexión y el chequeo `current_database()` post-connect comparten el mismo
 * set de aceptados.
 */
import bcrypt from 'bcryptjs';
import { randomBytes, randomUUID } from 'node:crypto';
import { Client } from 'pg';
import {
  AuthBootstrapError,
  bootstrapInitialAdmin,
  type InitialAdminBootstrapInput,
} from '../src/auth/auth-bootstrap.js';
import {
  CONNECTION_STRING_ENV_VAR,
  loadConnectionConfigFromEnv,
  redactSecrets,
} from '../src/database/connection-config.js';

const PG_ENABLED = process.env['AUTH_PG_INTEGRATION'] === '1';
const describePg = PG_ENABLED ? describe : describe.skip;
const TEST_TIMEOUT = 60_000;

/** Sandbox histórico o disposable f7_…_control creado por run-f7.mjs. */
const ACCEPTED_TARGET_DBS: readonly RegExp[] = [/^GastroExample$/, /^f7_[0-9a-f]{8}_control$/];

function isAcceptedTargetDatabase(database: string): boolean {
  return ACCEPTED_TARGET_DBS.some((re) => re.test(database));
}

type CountRow = { readonly c: number };

describePg('Initial administrator bootstrap PostgreSQL E2E', () => {
  let admin: Client | null = null;
  let input: InitialAdminBootstrapInput | null = null;
  let createdSiteId: string | null = null;
  let createdUserId: string | null = null;
  const secretSentinels: string[] = [];

  function needAdmin(): Client {
    if (admin === null) throw new Error('Bootstrap PostgreSQL client is not initialized.');
    return admin;
  }

  function needInput(): InitialAdminBootstrapInput {
    if (input === null) throw new Error('Bootstrap input is not initialized.');
    return input;
  }

  function isLoopback(host: string): boolean {
    const normalized = host.trim().toLowerCase();
    return normalized === 'localhost' || normalized === '127.0.0.1' || normalized === '::1';
  }

  beforeAll(async () => {
    try {
      const raw = process.env[CONNECTION_STRING_ENV_VAR];
      const connection = loadConnectionConfigFromEnv(process.env);
      secretSentinels.push(raw ?? '', connection.password);
      if (!isLoopback(connection.host) || !isAcceptedTargetDatabase(connection.database)) {
        throw new Error(
          'Bootstrap integration target must be loopback and one of GastroExample or f7_<8 hex>_control.',
        );
      }

      admin = new Client({
        ...connection,
        application_name: 'lu-bootstrap-pgqa',
        connectionTimeoutMillis: 5_000,
        query_timeout: 15_000,
        statement_timeout: 15_000,
      });
      await admin.connect();

      const target = await admin.query<{ database_name: string }>(
        'SELECT pg_catalog.current_database() AS database_name',
      );
      const connected = target.rows[0]?.database_name ?? '';
      if (!isAcceptedTargetDatabase(connected)) {
        throw new Error(
          'Bootstrap connected database is not an approved sandbox (GastroExample or f7_<8 hex>_control).',
        );
      }

      const counts = await admin.query<CountRow>(
        `SELECT (
           (SELECT count(*) FROM public.lu_site) +
           (SELECT count(*) FROM public.lu_user) +
           (SELECT count(*) FROM public.lu_site_membership)
         )::int AS c`,
      );
      if ((counts.rows[0]?.c ?? -1) !== 0) {
        throw new Error('Bootstrap PGQA requires an empty identity control plane.');
      }

      const suffix = randomUUID().replace(/-/g, '').slice(0, 12);
      // ^F1 §7 (Utf8PasswordPolicy): 12+ code points, mayúscula/minúscula/
      // dígito/símbolo, <=72 bytes UTF-8. El bootstrap exige el policy real.
      const password = `Bootstrap-${randomBytes(12).toString('base64url')}#7aA`;
      const auditHmacKey = randomBytes(32).toString('hex');
      secretSentinels.push(password, auditHmacKey);
      // ^Bootstrap canonical: identity_card `^[0-9A-Z-]{1,10}$`,
      // phone_number `^[+]?[0-9 ().-]+$` con 7..15 dígitos.
      input = {
        expectedDatabase: connected,
        siteCode: `BOOT.${suffix}`,
        siteName: `Bootstrap Site ${suffix}`,
        adminEmail: `bootstrap-${suffix}@example.invalid`,
        adminFirstName: 'Bootstrap',
        adminLastName: 'PGQA Administrator',
        adminIdentityCard: 'BS1234567',
        adminPhoneNumber: '+15555550199',
        adminUsername: `bootstrap-${suffix}`,
        adminPassword: password,
        bcryptCost: 12,
        auditHmacKey,
      };
    } catch (error) {
      const sanitized = redactSecrets(
        error instanceof Error ? error.message : String(error),
        secretSentinels,
      );
      if (error instanceof Error) {
        error.message = sanitized;
        error.stack = sanitized;
      }
      throw new Error(`Bootstrap PostgreSQL setup failed: ${sanitized}`, { cause: error });
    }
  }, TEST_TIMEOUT);

  afterAll(async () => {
    const failures: string[] = [];
    if (admin !== null) {
      try {
        if (createdUserId !== null && createdSiteId !== null) {
          await admin.query('BEGIN');
          try {
            // SET LOCAL session_replication_role = replica deshabilita los
            // triggers de BEFORE DELETE solo dentro de la transacción (requiere
            // superuser): trg_lu_user_no_hard_delete,
            // trg_lu_site_membership_no_hard_delete y
            // trg_lu_identity_audit_event_no_delete.
            await admin.query("SET LOCAL session_replication_role = 'replica'");
            await admin.query('DELETE FROM public.lu_login_identifier WHERE user_id = $1', [
              createdUserId,
            ]);
            await admin.query('DELETE FROM public.lu_security_event WHERE user_id = $1', [
              createdUserId,
            ]);
            await admin.query(
              'DELETE FROM public.lu_identity_audit_event WHERE subject_user_id = $1',
              [createdUserId],
            );
            await admin.query(
              'DELETE FROM public.lu_site_membership WHERE user_id = $1 AND site_id = $2',
              [createdUserId, createdSiteId],
            );
            await admin.query('DELETE FROM public.lu_user WHERE id = $1', [createdUserId]);
            await admin.query('DELETE FROM public.lu_site WHERE id = $1', [createdSiteId]);
            await admin.query('COMMIT');
          } catch (error) {
            await admin.query('ROLLBACK');
            throw error;
          }
          const residue = await admin.query<CountRow>(
            `SELECT (
               (SELECT count(*) FROM public.lu_login_identifier WHERE user_id = $1) +
               (SELECT count(*) FROM public.lu_security_event WHERE user_id = $1) +
               (SELECT count(*) FROM public.lu_identity_audit_event WHERE subject_user_id = $1) +
               (SELECT count(*) FROM public.lu_site_membership WHERE user_id = $1) +
               (SELECT count(*) FROM public.lu_user WHERE id = $1) +
               (SELECT count(*) FROM public.lu_site WHERE id = $2)
             )::int AS c`,
            [createdUserId, createdSiteId],
          );
          if ((residue.rows[0]?.c ?? -1) !== 0) failures.push('bootstrap-residue');
        }
      } catch {
        failures.push('bootstrap-cleanup');
      }
      await admin.end().catch(() => failures.push('bootstrap-client-close'));
    }
    if (failures.length > 0) {
      throw new Error(`Bootstrap PostgreSQL teardown incomplete: ${failures.join(', ')}`);
    }
  }, TEST_TIMEOUT);

  it('creates exactly once, records audit, rejects drift and cleans safely', async () => {
    const bootstrapInput = needInput();
    const first = await bootstrapInitialAdmin(needAdmin(), bootstrapInput);
    createdSiteId = first.siteId;
    createdUserId = first.userId;
    expect(first.created).toBe(true);

    const state = await needAdmin().query<{
      password_hash: string;
      is_super_admin: boolean;
      user_status: string;
      site_status: string;
      role: string;
      membership_status: string;
      event_type: string;
      subject_hash: string;
      metadata: Record<string, unknown>;
      reconciliation_state: string;
      password_scheme: string;
      must_change_password: boolean;
    }>(
      `SELECT u.password_hash, u.is_super_admin, u.status AS user_status,
              u.reconciliation_state, u.password_scheme, u.must_change_password,
              s.status AS site_status, m.role, m.status AS membership_status,
              e.event_type, e.subject_hash, e.metadata
       FROM public.lu_user u
       JOIN public.lu_site_membership m ON m.user_id = u.id
       JOIN public.lu_site s ON s.id = m.site_id
       JOIN public.lu_security_event e ON e.user_id = u.id AND e.site_id = s.id
       WHERE u.id = $1 AND s.id = $2`,
      [first.userId, first.siteId],
    );
    expect(state.rows).toHaveLength(1);
    expect(state.rows[0]).toMatchObject({
      is_super_admin: true,
      user_status: 'active',
      site_status: 'active',
      role: 'Administrador',
      membership_status: 'active',
      event_type: 'admin_bootstrap',
      metadata: { source: 'initial-bootstrap' },
    });
    expect(await bcrypt.compare(bootstrapInput.adminPassword, state.rows[0]!.password_hash)).toBe(
      true,
    );
    expect(state.rows[0]!.subject_hash).toMatch(/^[0-9a-f]{64}$/);
    expect(JSON.stringify(state.rows[0])).not.toContain(bootstrapInput.adminEmail);
    expect(JSON.stringify(state.rows[0])).not.toContain(bootstrapInput.adminPassword);

    // El contrato canónico de 0006 mantiene username + first/last + identity_card
    // + phone_number para filas en reconciliation_state='canonical'. El trigger
    // trg_lu_user_full_name_sync deriva full_name; no se debe insertar manualmente.
    const canonical = await needAdmin().query<{
      username: string;
      email: string;
      first_name: string;
      last_name: string;
      identity_card: string;
      phone_number: string;
      full_name: string;
      password_scheme: string;
      must_change_password: boolean;
      reconciliation_state: string;
    }>(
      `SELECT username, email, first_name, last_name, identity_card, phone_number,
              full_name, password_scheme, must_change_password, reconciliation_state
       FROM public.lu_user WHERE id = $1`,
      [first.userId],
    );
    expect(canonical.rows[0]).toMatchObject({
      username: bootstrapInput.adminUsername,
      email: bootstrapInput.adminEmail,
      first_name: bootstrapInput.adminFirstName,
      last_name: bootstrapInput.adminLastName,
      identity_card: bootstrapInput.adminIdentityCard,
      phone_number: bootstrapInput.adminPhoneNumber,
      full_name: `${bootstrapInput.adminFirstName} ${bootstrapInput.adminLastName}`,
      password_scheme: 'bcrypt',
      must_change_password: true, // el bootstrap obliga al primer cambio
      reconciliation_state: 'canonical',
    });

    await expect(bootstrapInitialAdmin(needAdmin(), bootstrapInput)).resolves.toEqual({
      created: false,
      siteId: first.siteId,
      userId: first.userId,
    });
    const auditCount = await needAdmin().query<CountRow>(
      `SELECT count(*)::int AS c FROM public.lu_security_event
       WHERE user_id = $1 AND event_type = 'admin_bootstrap'`,
      [first.userId],
    );
    expect(auditCount.rows[0]?.c).toBe(1);

    await expect(
      bootstrapInitialAdmin(needAdmin(), {
        ...bootstrapInput,
        siteName: 'Unexpected Changed Site',
      }),
    ).rejects.toBeInstanceOf(AuthBootstrapError);
  });
});
