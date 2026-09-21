/**
 * PostgreSQL opt-in for the one-time initial administrator bootstrap.
 * Uses only synthetic example.invalid data and restores an empty identity state.
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

  beforeAll(async () => {
    try {
      const raw = process.env[CONNECTION_STRING_ENV_VAR];
      const connection = loadConnectionConfigFromEnv(process.env);
      secretSentinels.push(raw ?? '', connection.password);
      const host = connection.host.trim().toLowerCase();
      if (
        !['localhost', '127.0.0.1', '::1'].includes(host) ||
        connection.database !== 'GastroExample'
      ) {
        throw new Error('Bootstrap integration target must be loopback/GastroExample.');
      }

      admin = new Client({
        ...connection,
        application_name: 'lu-bootstrap-pgqa',
        connectionTimeoutMillis: 5_000,
        query_timeout: 15_000,
        statement_timeout: 15_000,
      });
      await admin.connect();

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
      const password = `Bootstrap-${randomBytes(12).toString('base64url')}#7aA`;
      const auditHmacKey = randomBytes(32).toString('hex');
      secretSentinels.push(password, auditHmacKey);
      input = {
        expectedDatabase: 'GastroExample',
        siteCode: `BOOT.${suffix}`,
        siteName: `Bootstrap Site ${suffix}`,
        adminEmail: `bootstrap-${suffix}@example.invalid`,
        adminFullName: 'Bootstrap PGQA Administrator',
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
            await admin.query('DELETE FROM public.lu_security_event WHERE user_id = $1', [
              createdUserId,
            ]);
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
               (SELECT count(*) FROM public.lu_security_event WHERE user_id = $1) +
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
    }>(
      `SELECT u.password_hash, u.is_super_admin, u.status AS user_status,
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
