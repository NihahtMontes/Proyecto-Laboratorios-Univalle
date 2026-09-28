/**
 * MIG-F3-AUTH-PGQA-012 — E2E del contrato Auth sobre PostgreSQL real (opt-in).
 *
 * Se salta por completo salvo `AUTH_PG_INTEGRATION=1`. Al activarla:
 *  - Lee admin/migrador SOLO de `ConnectionStrings__DefaultConnection` con el
 *    parser existente; el raw nunca se imprime (mensajes saneados con
 *    `redactSecrets`).
 *  - Requiere `lu_auth_login` y `lu_auth_runtime` aprovisionados. Activa el
 *    login con un secreto aleatorio solo durante esta suite y prueba el
 *    AuthPgPool real, que asume el rol runtime en cada transacción.
 *  - Datos 100 % sintéticos (UUIDs y `example.invalid`): 2 sites active, 1 user
 *    canonical active bcrypt cost 12, 2 memberships active. afterAll limpia por
 *    IDs estrictos en orden lu_login_identifier -> lu_identity_audit_event ->
 *    sessions -> lu_security_event -> lu_auth_rate_limit -> memberships ->
 *    user -> sites, dentro de una transacción con
 *    `SET LOCAL session_replication_role = replica` para saltar los triggers
 *    de hard-delete (trg_lu_user_no_hard_delete /
 *    trg_lu_site_membership_no_hard_delete /
 *    trg_lu_identity_audit_event_no_delete) sin deshabilitarlos globalmente.
 *  - La pasarela de destino acepta `GastroExample` (compatibilidad con el
 *    sandbox histórico) o una base disposable con el patrón
 *    `^f7_[0-9a-f]{8}_control$` creada por `tests/e2e/stack/run-f7.mjs`.
 *
 * Ejecución autorizada (sandbox migrado, p. ej. GastroExample o f7_…_control):
 *   AUTH_PG_INTEGRATION=1 ConnectionStrings__DefaultConnection='<admin>' \
 *     pnpm --filter @lu/api test -- auth.postgres.integration
 */
import 'reflect-metadata';
import { randomBytes, randomUUID } from 'node:crypto';
import { Client } from 'pg';
import bcrypt from 'bcryptjs';

import { AuthConfig } from '../src/auth/auth.config.js';
import { hashAuthIdentifier, hashSessionToken } from '../src/auth/auth.crypto.js';
import {
  AuthForbiddenException,
  AuthRateLimitException,
  AuthUnauthorizedException,
} from '../src/auth/auth.exceptions.js';
import { AuthPgPool } from '../src/auth/auth.pg-pool.js';
import { AuthRateLimitService } from '../src/auth/auth.rate-limit.js';
import { AuthRepository } from '../src/auth/auth.repository.js';
import { AuthService } from '../src/auth/auth.service.js';
import type { ActiveSiteSession, AuthSessionResponse } from '../src/auth/auth.types.js';
import type {
  IdentityAuditWriter,
  LegacyPasswordWindow,
  PasswordHasher,
  PasswordPolicy,
  PasswordVerificationService,
  SessionInvalidator,
} from '../src/identity/identity.contracts.js';
import { AspNetIdentityPasswordVerifier } from '../src/identity/password/aspnet-identity.verifier.js';
import { BcryptPasswordHasher } from '../src/identity/password/bcrypt.hasher.js';
import { BcryptPasswordVerifier } from '../src/identity/password/bcrypt.verifier.js';
import { Utf8PasswordPolicy } from '../src/identity/password/password-policy.js';
import { DefaultPasswordVerificationService } from '../src/identity/password/password-verification.service.js';
import { PgIdentityAuditWriter } from '../src/identity/identity-audit.writer.js';
import { PgLegacyPasswordWindow } from '../src/identity/legacy-password-window.js';
import { PgSessionInvalidator } from '../src/identity/session-invalidator.js';
import {
  CONNECTION_STRING_ENV_VAR,
  loadConnectionConfigFromEnv,
  redactSecrets,
} from '../src/database/connection-config.js';

const PG_ENABLED = process.env['AUTH_PG_INTEGRATION'] === '1';
const describePg = PG_ENABLED ? describe : describe.skip;

/** Rol runtime del contrato; constante fija validada, nunca viene del entorno. */
const RUNTIME_ROLE = 'lu_auth_runtime';
const LOGIN_ROLE = 'lu_auth_login';
const TEST_TIMEOUT = 60_000;

const SQLSTATE_DENIED = '42501'; // insufficient_privilege
const SQLSTATE_LOCK_TIMEOUT = '55P03'; // lock_not_available

/** IP de documentación (TEST-NET-1, RFC 5737): sintética como todo lo demás. */
const CLIENT_IP = '203.0.113.7';
const RATE_LIMIT_CLIENT_IP = '203.0.113.8';

/** Sandbox histórico (compatibilidad) o disposable f7_…_control de run-f7.mjs. */
const ACCEPTED_TARGET_DBS: readonly RegExp[] = [/^GastroExample$/, /^f7_[0-9a-f]{8}_control$/];

function isAcceptedTargetDatabase(database: string): boolean {
  return ACCEPTED_TARGET_DBS.some((re) => re.test(database));
}

const TEST_AUTH_ENV: Record<string, string | undefined> = {
  AUTH_SESSION_IDLE_TTL_SECONDS: '1800',
  AUTH_SESSION_ABSOLUTE_TTL_SECONDS: '43200',
  AUTH_LOGIN_FLOOR_MS: '0',
  AUTH_BCRYPT_COST: '12',
  // La rate limit no es el objeto de esta suite; se eleva el techo para que
  // las pruebas que no la cubren nunca choquen con el limitador.
  AUTH_RATE_LIMIT_MAX_ATTEMPTS: '50',
  AUTH_RATE_LIMIT_IP_MAX_ATTEMPTS: '100',
  AUTH_RATE_LIMIT_WINDOW_SECONDS: '60',
  AUTH_AUDIT_HMAC_KEY: 'pgqa-only-auth-hmac-key-32-characters-long',
};

type Seed = {
  readonly siteAId: string;
  readonly siteBId: string;
  readonly userId: string;
  readonly username: string;
  readonly email: string;
  readonly password: string;
  readonly passwordHash: string;
  readonly securityVersion: string;
};

type SessionRow = {
  readonly id: string;
  readonly token_hash: string;
  readonly user_id: string;
  readonly active_site_id: string | null;
  readonly revoked_at: Date | null;
  readonly revocation_reason: string | null;
  readonly last_seen_at: Date;
  readonly idle_expires_at: Date;
};

type IdentityRow = {
  readonly current_user: string;
  readonly current_role: string;
  readonly session_user: string;
  readonly search_path: string;
};

type CountRow = { readonly c: number };
type StatusRow = { readonly status: string };
type PublicAclRow = {
  readonly schema_create: boolean;
  readonly database_temp: boolean;
  readonly table_access: boolean;
};
type SecurityEventRow = {
  readonly event_type: string;
  readonly subject_hash: string | null;
  readonly ip_hash: string | null;
  readonly metadata: Record<string, unknown>;
};

describePg('Auth PostgreSQL E2E (MIG-F3-AUTH-PGQA-012)', () => {
  let admin: Client | null = null;
  let runtimePool: AuthPgPool | null = null;
  let seed: Seed | null = null;
  let cleanupIds: Pick<Seed, 'siteAId' | 'siteBId' | 'userId' | 'email' | 'username'> | null = null;
  let originalRuntimeConnection: string | undefined;
  let loginActivated = false;
  /** Fragmentos secretos que ningún mensaje de esta suite debe contener. */
  const secretSentinels: string[] = [];

  function needAdmin(): Client {
    if (admin === null) throw new Error('Admin connection is not initialized.');
    return admin;
  }

  function needPool(): AuthPgPool {
    if (runtimePool === null) throw new Error('Runtime role pool is not initialized.');
    return runtimePool;
  }

  function needSeed(): Seed {
    if (seed === null) throw new Error('Synthetic seed is not initialized.');
    return seed;
  }

  function codeOf(error: unknown): string | undefined {
    if (typeof error === 'object' && error !== null) {
      const code = (error as { code?: unknown }).code;
      if (typeof code === 'string') return code;
    }
    return undefined;
  }

  function messageOf(error: unknown): string {
    return error instanceof Error ? error.message : String(error);
  }

  function assertNoSecrets(error: unknown): void {
    const message = messageOf(error);
    for (const secret of secretSentinels) {
      if (secret.length >= 2) {
        expect(message).not.toContain(secret);
      }
    }
  }

  async function capture(fn: () => Promise<unknown>): Promise<unknown> {
    try {
      await fn();
      return undefined;
    } catch (error) {
      return error;
    }
  }

  /**
   * Sentencia que el contrato NIEGA al runtime. Corre dentro de una transacción
   * wrapper que SIEMPRE revierte: si la sentencia fuera inesperadamente
   * permitida tampoco muta nada y el test denuncia la desviación.
   */
  async function expectDeniedByRuntime(sql: string, params?: unknown[]): Promise<void> {
    const guard = new Error('auth-pgqa-guard-rollback');
    try {
      await needPool().transaction(async (client) => {
        await client.query(sql, params);
        throw guard;
      });
    } catch (error) {
      if (error === guard) {
        throw new Error('SQL statement unexpectedly allowed for the runtime role.', {
          cause: error,
        });
      }
      assertNoSecrets(error);
      expect(codeOf(error)).toBe(SQLSTATE_DENIED);
      return;
    }
    throw new Error('SQL statement unexpectedly allowed for the runtime role.');
  }

  function buildKernel(config: AuthConfig): {
    verifier: PasswordVerificationService;
    hasher: PasswordHasher;
    policy: PasswordPolicy;
    invalidator: SessionInvalidator;
    audit: IdentityAuditWriter;
    window: LegacyPasswordWindow;
  } {
    const verifier = new DefaultPasswordVerificationService(
      [
        new BcryptPasswordVerifier({
          currentCost: config.bcryptCost,
          minCost: Math.min(10, config.bcryptCost),
          maxCost: 15,
        }),
        new AspNetIdentityPasswordVerifier('legacy_identity_v2'),
        new AspNetIdentityPasswordVerifier('legacy_identity_v3'),
      ],
      config.dummyHash,
    );
    return {
      verifier,
      hasher: new BcryptPasswordHasher(config.bcryptCost),
      policy: new Utf8PasswordPolicy(),
      invalidator: new PgSessionInvalidator(),
      audit: new PgIdentityAuditWriter(),
      window: new PgLegacyPasswordWindow(),
    };
  }

  function makeServices(): { repository: AuthRepository; auth: AuthService } {
    const config = new AuthConfig(TEST_AUTH_ENV);
    const repository = new AuthRepository(needPool(), config);
    const rateLimit = new AuthRateLimitService(config, repository);
    const kernel = buildKernel(config);
    // F3 AuthService ctor (apps/api/src/auth/auth.service.ts:97-109):
    //   config, verifier, hasher, policy, audit, invalidator, legacyWindow,
    //   repository, rateLimit. Los nuevos proveedores Pg* ejercitan SQL real
    //   bajo el rol runtime; el trigger path de rehash no se cubre aquí.
    const auth = new AuthService(
      config,
      kernel.verifier,
      kernel.hasher,
      kernel.policy,
      kernel.audit,
      kernel.invalidator,
      kernel.window,
      repository,
      rateLimit,
    );
    return { repository, auth };
  }

  function identifierHash(purpose: string, value: string): string {
    return hashAuthIdentifier(TEST_AUTH_ENV.AUTH_AUDIT_HMAC_KEY!, purpose, value);
  }

  /** Login sintético nuevo por prueba: evita dependencias de orden y de sesión. */
  async function loginByEmailFresh(
    auth: AuthService,
  ): Promise<{ token: string; response: AuthSessionResponse }> {
    const s = needSeed();
    const result = await auth.login({ loginIdentifier: s.email, password: s.password }, CLIENT_IP);
    return { token: result.token, response: result.response };
  }

  async function loginByUsernameFresh(
    auth: AuthService,
  ): Promise<{ token: string; response: AuthSessionResponse }> {
    const s = needSeed();
    const result = await auth.login(
      { loginIdentifier: s.username, password: s.password },
      CLIENT_IP,
    );
    return { token: result.token, response: result.response };
  }

  async function fetchSessionRow(token: string): Promise<SessionRow> {
    const result = await needAdmin().query<SessionRow>(
      `SELECT id, token_hash, user_id, active_site_id, revoked_at, revocation_reason,
              last_seen_at, idle_expires_at
       FROM lu_session
       WHERE token_hash = $1`,
      [hashSessionToken(token)],
    );
    const row = result.rows[0];
    if (row === undefined) throw new Error('Session row not found for an issued token.');
    return row;
  }

  function expectWireContract(session: ActiveSiteSession): void {
    expect(Object.keys(session).sort()).toEqual([
      'activeSiteId',
      'activeSiteName',
      'displayName',
      'email',
      'globalRole',
      'memberships',
      'userId',
    ]);
    for (const membership of session.memberships) {
      expect(Object.keys(membership).sort()).toEqual(['role', 'siteId', 'siteName', 'state']);
    }
  }

  function isLoopback(host: string): boolean {
    const normalized = host.trim().toLowerCase();
    return normalized === 'localhost' || normalized === '127.0.0.1' || normalized === '::1';
  }

  beforeAll(async () => {
    let setupFailure: string | null = null;
    try {
      // Parser existente. El raw solo alimenta los centinelas de redacción.
      const rawEnv = process.env[CONNECTION_STRING_ENV_VAR];
      const connection = loadConnectionConfigFromEnv(process.env);
      secretSentinels.push(rawEnv ?? '', connection.password);

      if (!isLoopback(connection.host) || !isAcceptedTargetDatabase(connection.database)) {
        throw new Error(
          'Integration target must be loopback and one of GastroExample or f7_<8 hex>_control.',
        );
      }

      admin = new Client({
        host: connection.host,
        port: connection.port,
        database: connection.database,
        user: connection.user,
        password: connection.password,
        ssl: connection.ssl,
        connectionTimeoutMillis: 5_000,
        query_timeout: 15_000,
        statement_timeout: 15_000,
      });
      await admin.connect();
      await admin.query('SET search_path TO pg_catalog, public');

      const target = await admin.query<{ database_name: string; server_addr: string | null }>(
        `SELECT pg_catalog.current_database() AS database_name,
                pg_catalog.inet_server_addr()::text AS server_addr`,
      );
      if (target.rows[0] === undefined || !isAcceptedTargetDatabase(target.rows[0].database_name)) {
        throw new Error(
          'Connected database is not an approved sandbox (GastroExample or f7_<8 hex>_control).',
        );
      }

      // Ambos roles deben estar deshabilitados y con la membresía SET-only.
      const roleRes = await admin.query<CountRow>(
        `SELECT count(*)::int AS c
         FROM pg_catalog.pg_authid
         WHERE rolname = ANY($1::name[])
           AND rolcanlogin = false AND rolsuper = false
           AND rolinherit = false AND rolpassword IS NULL`,
        [[RUNTIME_ROLE, LOGIN_ROLE]],
      );
      if ((roleRes.rows[0]?.c ?? 0) !== 2) {
        throw new Error('Required disabled auth roles are not provisioned.');
      }
      const membership = await admin.query<CountRow>(
        `SELECT count(*)::int AS c
         FROM pg_catalog.pg_auth_members m
         JOIN pg_catalog.pg_roles parent ON parent.oid = m.roleid
         JOIN pg_catalog.pg_roles member ON member.oid = m.member
         WHERE parent.rolname = $1 AND member.rolname = $2
           AND m.admin_option = false
           AND m.inherit_option = false
           AND m.set_option = true`,
        [RUNTIME_ROLE, LOGIN_ROLE],
      );
      if ((membership.rows[0]?.c ?? 0) !== 1) {
        throw new Error('Auth login membership is not SET-only.');
      }

      // Identificadores sintéticos: sufijos estables + UUIDs.
      const suffix = randomUUID().replace(/-/g, '').slice(0, 12);
      const siteAId = randomUUID();
      const siteBId = randomUUID();
      const userId = randomUUID();
      const username = `pgqa-${suffix}`;
      const email = `pgqa-${suffix}@example.invalid`;
      // ^F1 §7: 12+ code points, mayúscula/minúscula/dígito/símbolo, <=72 bytes.
      const password = `Pgqa-${suffix}-Rotate#7`;
      const passwordHash = await bcrypt.hash(password, 12);
      // ^[0-9A-Z-]{1,10}$ para identity_card; 11 dígitos para phone_number.
      const identityCard = `P${suffix.slice(0, 8).toUpperCase()}`;
      const phoneDigits = (suffix.match(/\d/g) ?? []).join('').padEnd(11, '0').slice(0, 11);
      const phoneNumber = `+1${phoneDigits}`;
      cleanupIds = { siteAId, siteBId, userId, email, username };

      await admin.query('BEGIN');
      try {
        await admin.query(
          `INSERT INTO public.lu_site (id, code, name, status)
       VALUES ($1, $2, $3, 'active'), ($4, $5, $6, 'active')`,
          [
            siteAId,
            `PGQA.${suffix}.A`,
            `PGQA Site A ${suffix}`,
            siteBId,
            `PGQA.${suffix}.B`,
            `PGQA Site B ${suffix}`,
          ],
        );
        // F2 canonical (control-plane 0006): reconciliation_state='canonical' con
        // username + first/last name + identity_card + phone_number NO nulos.
        // full_name lo deriva el trigger trg_lu_user_full_name_sync; omitirlo es
        // obligatorio para no chocar con la sobreescritura del trigger.
        await admin.query(
          `INSERT INTO public.lu_user
             (id, email, username, first_name, last_name,
              identity_card, phone_number,
              password_hash, password_scheme, must_change_password,
              is_super_admin, account_status, status, reconciliation_state,
              security_version)
         VALUES ($1, $2, $3, 'Pgqa', 'Integration',
                 $4, $5,
                 $6, 'bcrypt', false,
                 false, 'active', 'active', 'canonical',
                 0)`,
          [userId, email, username, identityCard, phoneNumber, passwordHash],
        );
        await admin.query(
          `INSERT INTO public.lu_site_membership (user_id, site_id, role, status)
       VALUES ($1, $2, 'Administrador', 'active'), ($1, $3, 'Supervisor', 'active')`,
          [userId, siteAId, siteBId],
        );
        await admin.query('COMMIT');
      } catch (error) {
        await admin.query('ROLLBACK');
        throw error;
      }

      seed = {
        siteAId,
        siteBId,
        userId,
        username,
        email,
        password,
        passwordHash,
        securityVersion: '0',
      };

      // Enable the login only for this isolated run. The generated secret is
      // process-local and never logged or written to disk.
      const runtimePassword = randomBytes(32).toString('hex');
      secretSentinels.push(runtimePassword);
      await admin.query(`ALTER ROLE ${LOGIN_ROLE} LOGIN PASSWORD '${runtimePassword}'`);
      loginActivated = true;

      // El pool runtime debe conectar a LA MISMA base que la admin DSN; el
      // hard-code histórico `Database=GastroExample` se reemplaza aquí.
      originalRuntimeConnection = process.env['ConnectionStrings__ControlPlaneRuntime'];
      process.env['ConnectionStrings__ControlPlaneRuntime'] =
        `Host=${connection.host};Port=${connection.port};Database=${connection.database};` +
        `Username=${LOGIN_ROLE};Password=${runtimePassword};SSL Mode=Disable`;
      runtimePool = new AuthPgPool(new AuthConfig(TEST_AUTH_ENV));
    } catch (error) {
      // Fail-closed sin filtrar el raw ni la password en el mensaje.
      setupFailure = redactSecrets(messageOf(error), secretSentinels);
    }
    if (setupFailure !== null) {
      throw new Error(`PostgreSQL integration setup failed: ${setupFailure}`);
    }
  }, TEST_TIMEOUT);

  afterAll(async () => {
    const failures: string[] = [];
    try {
      if (runtimePool !== null) {
        try {
          await runtimePool.close();
        } catch {
          failures.push('close:runtimePool');
        }
      }

      if (admin !== null) {
        try {
          if (cleanupIds !== null) {
            // Hashes derivados iguales a los del auth slice (apps/api/src/auth/
            // auth.rate-limit.ts:25-33) usando la misma HMAC key del seed.
            const normalizedEmail = cleanupIds.email.trim().toLowerCase();
            const normalizedUsername = cleanupIds.username.trim().toLowerCase();
            const subjectHash = identifierHash('subject', normalizedEmail);
            const ipHashes = [CLIENT_IP, RATE_LIMIT_CLIENT_IP].map((ip) =>
              identifierHash('ip', ip),
            );
            const rateKeys = [
              identifierHash('rate-identifier-ip', `${normalizedEmail}|${CLIENT_IP}`),
              identifierHash('rate-identifier-ip', `${normalizedEmail}|${RATE_LIMIT_CLIENT_IP}`),
              identifierHash('rate-ip', CLIENT_IP),
              identifierHash('rate-ip', RATE_LIMIT_CLIENT_IP),
            ];
            // SET LOCAL session_replication_role = replica deshabilita los triggers
            // de BEFORE DELETE solo durante esta transacción (requiere superuser);
            // así evitamos tocar ALTER TABLE … DISABLE TRIGGER de forma global.
            await admin.query('BEGIN');
            try {
              await admin.query("SET LOCAL session_replication_role = 'replica'");
              await admin.query('DELETE FROM public.lu_login_identifier WHERE user_id = $1', [
                cleanupIds.userId,
              ]);
              await admin.query(
                'DELETE FROM public.lu_identity_audit_event WHERE subject_user_id = $1',
                [cleanupIds.userId],
              );
              await admin.query('DELETE FROM public.lu_session WHERE user_id = $1', [
                cleanupIds.userId,
              ]);
              await admin.query(
                `DELETE FROM public.lu_security_event
                 WHERE user_id = $1 OR subject_hash = $2 OR ip_hash = ANY($3::bpchar[])`,
                [cleanupIds.userId, subjectHash, ipHashes],
              );
              await admin.query(
                'DELETE FROM public.lu_auth_rate_limit WHERE key_hash = ANY($1::bpchar[])',
                [rateKeys],
              );
              await admin.query('DELETE FROM public.lu_site_membership WHERE user_id = $1', [
                cleanupIds.userId,
              ]);
              await admin.query(
                'DELETE FROM public.lu_user WHERE id = $1 AND lower(email) = $2 AND username = $3',
                [cleanupIds.userId, normalizedEmail, cleanupIds.username],
              );
              await admin.query('DELETE FROM public.lu_site WHERE id = ANY($1::uuid[])', [
                [cleanupIds.siteAId, cleanupIds.siteBId],
              ]);
              await admin.query('COMMIT');
            } catch (error) {
              await admin.query('ROLLBACK');
              throw error;
            }
            // El modo replica solo vive dentro de la transacción: el conteo se
            // hace con triggers activos, lo que también valida que ningún dato
            // del usuario sembrado sobrevivió.
            const residue = await admin.query<CountRow>(
              `SELECT (
                 (SELECT count(*) FROM public.lu_login_identifier WHERE user_id = $1) +
                 (SELECT count(*) FROM public.lu_identity_audit_event WHERE subject_user_id = $1) +
                 (SELECT count(*) FROM public.lu_session WHERE user_id = $1) +
                 (SELECT count(*) FROM public.lu_security_event
                  WHERE user_id = $1 OR subject_hash = $3 OR ip_hash = ANY($4::bpchar[])) +
                 (SELECT count(*) FROM public.lu_auth_rate_limit
                  WHERE key_hash = ANY($5::bpchar[])) +
                 (SELECT count(*) FROM public.lu_site_membership WHERE user_id = $1) +
                 (SELECT count(*) FROM public.lu_user
                  WHERE id = $1 OR lower(email) = $2 OR username = $6) +
                 (SELECT count(*) FROM public.lu_site WHERE id = ANY($7::uuid[]))
               )::int AS c`,
              [
                cleanupIds.userId,
                normalizedEmail,
                subjectHash,
                ipHashes,
                rateKeys,
                cleanupIds.username,
                [cleanupIds.siteAId, cleanupIds.siteBId],
              ],
            );
            if ((residue.rows[0]?.c ?? -1) !== 0) failures.push('cleanup:residue');
            void normalizedUsername;
          }

          if (loginActivated) {
            await admin.query(`ALTER ROLE ${LOGIN_ROLE} NOLOGIN PASSWORD NULL`);
            const activity = await admin.query<CountRow>(
              'SELECT count(*)::int AS c FROM pg_catalog.pg_stat_activity WHERE usename = $1',
              [LOGIN_ROLE],
            );
            if ((activity.rows[0]?.c ?? -1) !== 0) failures.push('runtime-connections-remain');
            const disabled = await admin.query<CountRow>(
              `SELECT count(*)::int AS c FROM pg_catalog.pg_authid
               WHERE rolname = $1 AND rolcanlogin = false AND rolpassword IS NULL`,
              [LOGIN_ROLE],
            );
            if ((disabled.rows[0]?.c ?? 0) !== 1) failures.push('login-not-disabled');
          }

          await admin.end();
        } catch {
          failures.push('admin-teardown');
        }
      }
    } finally {
      if (originalRuntimeConnection === undefined) {
        delete process.env['ConnectionStrings__ControlPlaneRuntime'];
      } else {
        process.env['ConnectionStrings__ControlPlaneRuntime'] = originalRuntimeConnection;
      }
    }
    if (failures.length > 0) {
      throw new Error(`PostgreSQL integration teardown incomplete: ${failures.join(', ')}`);
    }
  }, 90_000);

  it('1) AuthPgPool uses login transport with runtime role and fixed search_path', async () => {
    const result = await needPool().query<IdentityRow>(
      `SELECT current_user, current_role, session_user,
              pg_catalog.current_setting('search_path') AS search_path`,
    );
    const row = result.rows[0];
    if (row === undefined) throw new Error('Identity probe returned no row.');
    expect(row.current_role).toBe(RUNTIME_ROLE);
    expect(row.current_user).toBe(RUNTIME_ROLE);
    expect(row.session_user).toBe(LOGIN_ROLE);
    expect(row.search_path).toBe('pg_catalog, public');

    const acl = await needAdmin().query<PublicAclRow>(
      `SELECT
         pg_catalog.has_schema_privilege('public', 'public', 'CREATE') AS schema_create,
         pg_catalog.has_database_privilege(
           'public', pg_catalog.current_database(), 'TEMPORARY'
         ) AS database_temp,
         pg_catalog.has_table_privilege(
           'public', 'public.lu_session',
           'SELECT,INSERT,UPDATE,DELETE,TRUNCATE,REFERENCES,TRIGGER,MAINTAIN'
         ) AS table_access`,
    );
    expect(acl.rows[0]).toEqual({
      schema_create: false,
      database_temp: false,
      table_access: false,
    });
  });

  it('2) login by email -> bcrypt -> session with sha256 hash -> wire -> setActiveSite -> logout 401', async () => {
    const s = needSeed();
    const { auth } = makeServices();

    const wrong = await capture(() =>
      auth.login({ loginIdentifier: s.email, password: 'wrong-password-x' }, CLIENT_IP),
    );
    expect(wrong).toBeInstanceOf(AuthUnauthorizedException);

    const { token, response } = await loginByEmailFresh(auth);
    const session = response.session;
    expectWireContract(session);
    expect(session.userId).toBe(s.userId);
    expect(session.globalRole).toBeNull();
    // Dos memberships elegibles sin activeSiteId -> sesión site_selection
    // (buildAuthSessionResponse en apps/api/src/auth/auth.types.ts: la wire
    // restringida NO expone memberships/active site; los sitios elegibles van
    // en response.eligibleSites).
    expect(response.purpose).toBe('site_selection');
    expect(session.activeSiteId).toBeNull();
    expect(session.activeSiteName).toBeNull();
    expect(session.memberships).toHaveLength(0);
    expect([...response.eligibleSites].map((m) => m.siteId).sort()).toEqual(
      [s.siteAId, s.siteBId].sort(),
    );
    expect(response.eligibleSites.find((m) => m.siteId === s.siteAId)?.role).toBe('Administrador');
    expect(response.eligibleSites.find((m) => m.siteId === s.siteBId)?.role).toBe('Supervisor');
    // El wire jamás expone el hash de clave.
    expect(JSON.stringify(response)).not.toContain(s.passwordHash);

    // El hash persistido es bcrypt cost 12 y valida la clave sintética.
    const hashRes = await needAdmin().query<{ password_hash: string }>(
      'SELECT password_hash FROM lu_user WHERE id = $1',
      [s.userId],
    );
    const storedHash = hashRes.rows[0]?.password_hash;
    if (typeof storedHash !== 'string') throw new Error('Seeded user hash missing.');
    expect(storedHash).toMatch(/^\$2[aby]\$\d{2}\$/);
    expect(bcrypt.getRounds(storedHash)).toBe(12);
    expect(await bcrypt.compare(s.password, storedHash)).toBe(true);

    // Sesión creada: token_hash es SHA-256 hex lowercase; el raw no está en DB.
    const row = await fetchSessionRow(token);
    expect(row.token_hash).toBe(hashSessionToken(token));
    expect(/^[0-9a-f]{64}$/.test(row.token_hash)).toBe(true);
    expect(row.token_hash).not.toBe(token);
    expect(row.active_site_id).toBeNull();
    expect(row.revoked_at).toBeNull();
    const rawScan = await needAdmin().query<CountRow>(
      `SELECT count(*)::int AS c FROM lu_session
       WHERE user_id = $1 AND (token_hash::text = $2 OR COALESCE(revocation_reason, '') = $2)`,
      [s.userId, token],
    );
    expect(rawScan.rows[0]?.c).toBe(0);

    // getSession real.
    const wire = await auth.getSession(token);
    expectWireContract(wire.session);
    expect(wire.session.userId).toBe(s.userId);
    expect(wire.session.activeSiteId).toBeNull();

    // setActiveSite a la segunda membership (commit real bajo el wrapper runtime).
    // Como el login inicial quedó en purpose='site_selection', el contrato
    // (auth.service.ts setActiveSite) revoca la sesión previa y emite una
    // sesión normal nueva: el token devuelto NO es null y la row del token
    // antiguo queda marcada con revocation_reason='site_selection_resolved'.
    const after = await auth.setActiveSite(token, { activeSiteId: s.siteBId });
    expect(after.response.purpose).toBe('normal');
    expect(after.response.session.activeSiteId).toBe(s.siteBId);
    const siteBName = after.response.session.memberships.find(
      (m) => m.siteId === s.siteBId,
    )?.siteName;
    expect(siteBName).toBeDefined();
    expect(after.response.session.activeSiteName).toBe(siteBName);
    expect(after.token).not.toBeNull();
    const activeToken = after.token as string;
    expect((await fetchSessionRow(activeToken)).active_site_id).toBe(s.siteBId);

    // logout = UPDATE (no DELETE); la sesión revocada deja de autorizar (401).
    await auth.logout(activeToken);
    const revoked = await fetchSessionRow(activeToken);
    expect(revoked.revoked_at).not.toBeNull();
    expect(revoked.revocation_reason).toBe('logout');
    await expect(auth.getSession(activeToken)).rejects.toBeInstanceOf(AuthUnauthorizedException);

    const audit = await needAdmin().query<SecurityEventRow>(
      `SELECT event_type, subject_hash, ip_hash, metadata
       FROM public.lu_security_event
       WHERE user_id = $1
       ORDER BY occurred_at, event_type`,
      [s.userId],
    );
    // lu_security_event solo lleva los eventos de autenticación. La transición
    // site_selection -> normal va a lu_identity_audit_event con
    // action='active_site_changed' (auth.service.ts setActiveSite); el código
    // anterior del repositorio emitía active_site_changed a lu_security_event,
    // pero el contrato vigente lo particiona por tabla.
    expect(audit.rows.map((row) => row.event_type).sort()).toEqual(
      ['login_failure', 'login_success', 'logout'].sort(),
    );
    const credentialEvents = audit.rows.filter((row) => row.event_type.startsWith('login_'));
    expect(credentialEvents).toHaveLength(2);
    for (const event of credentialEvents) {
      expect(event.subject_hash).toMatch(/^[0-9a-f]{64}$/);
      expect(event.ip_hash).toMatch(/^[0-9a-f]{64}$/);
      expect(JSON.stringify(event)).not.toContain(s.email);
      expect(JSON.stringify(event)).not.toContain(CLIENT_IP);
    }

    const identityAudit = await needAdmin().query<{ action: string }>(
      `SELECT action FROM public.lu_identity_audit_event
       WHERE subject_user_id = $1 AND action = 'active_site_changed'`,
      [s.userId],
    );
    expect(identityAudit.rows.length).toBeGreaterThanOrEqual(1);
  });

  it('2b) login by username succeeds with the same seeded identity', async () => {
    const s = needSeed();
    const { auth } = makeServices();

    const { token, response } = await loginByUsernameFresh(auth);
    const session = response.session;
    expectWireContract(session);
    expect(session.userId).toBe(s.userId);
    expect(session.email).toBe(s.email);
    // Restringida por buildAuthSessionResponse: memberships queda vacía y los
    // sitios elegibles se exponen vía response.eligibleSites.
    expect(response.purpose).toBe('site_selection');
    expect(session.memberships).toHaveLength(0);
    expect(response.eligibleSites.map((m) => m.siteId).sort()).toEqual(
      [s.siteAId, s.siteBId].sort(),
    );
    expect(session.activeSiteId).toBeNull();
    expect(token).toMatch(/^[A-Za-z0-9_-]{20,}$/);

    await auth.logout(token);
  });

  it('3) setActiveSite to a foreign/no-membership site rolls back and keeps the session', async () => {
    const { auth } = makeServices();
    const { token } = await loginByEmailFresh(auth);
    const before = await fetchSessionRow(token);

    // UUID canónico sin ninguna membership (ajena al usuario).
    const alienSiteId = randomUUID();
    const error = await capture(() => auth.setActiveSite(token, { activeSiteId: alienSiteId }));
    expect(error).toBeInstanceOf(AuthForbiddenException);
    expect((error as AuthForbiddenException).code).toBe('SITE_ACCESS_DENIED');

    const after = await fetchSessionRow(token);
    expect(after.active_site_id).toBe(before.active_site_id);
    // Prueba de ROLLBACK real: ni last_seen_at ni idle_expires_at avanzaron.
    expect(after.last_seen_at.getTime()).toBe(before.last_seen_at.getTime());
    expect(after.idle_expires_at.getTime()).toBe(before.idle_expires_at.getTime());

    await auth.logout(token);
  });

  it('4) admin bumps security_version -> existing session is invalid; new sessions work', async () => {
    const s = needSeed();
    const { auth } = makeServices();
    const { token } = await loginByEmailFresh(auth);
    await expect(auth.getSession(token)).resolves.toBeDefined();

    await needAdmin().query(
      'UPDATE lu_user SET security_version = security_version + 1 WHERE id = $1',
      [s.userId],
    );
    try {
      await expect(auth.getSession(token)).rejects.toBeInstanceOf(AuthUnauthorizedException);
      await expect(auth.setActiveSite(token, { activeSiteId: s.siteAId })).rejects.toBeInstanceOf(
        AuthUnauthorizedException,
      );
    } finally {
      // Se restaura el baseline para que nada dependa del orden de las pruebas.
      await needAdmin().query('UPDATE lu_user SET security_version = $1::bigint WHERE id = $2', [
        s.securityVersion,
        s.userId,
      ]);
    }

    // Con sesión nueva (helper por prueba) el contrato vuelve a aplicarse.
    const fresh = await loginByEmailFresh(auth);
    await expect(auth.getSession(fresh.token)).resolves.toBeDefined();
    await auth.logout(fresh.token);
  });

  it('5) runtime privileges: session ops allowed; DELETE/INSERT/UPDATE identity and DDL denied (42501)', async () => {
    const s = needSeed();
    const pool = needPool();

    // Permitido: SELECT sobre las cuatro tablas de control.
    const userSel = await pool.query<{ id: string; status: string }>(
      'SELECT id, status FROM lu_user WHERE id = $1',
      [s.userId],
    );
    expect(userSel.rows[0]?.status).toBe('active');
    expect(
      (await pool.query('SELECT 1 AS one FROM lu_site WHERE id = $1', [s.siteAId])).rowCount,
    ).toBe(1);
    expect(
      (await pool.query('SELECT 1 AS one FROM lu_site_membership WHERE user_id = $1', [s.userId]))
        .rowCount,
    ).toBe(2);
    expect(
      (await pool.query('SELECT 1 AS one FROM lu_session WHERE user_id = $1', [s.userId])).rowCount,
    ).toBeGreaterThanOrEqual(0);

    // Negado: DELETE de sesiones (la revocación institucional es UPDATE soft).
    await expectDeniedByRuntime('DELETE FROM lu_session WHERE user_id = $1', [s.userId]);

    // Negado: DELETE sobre las tablas de identidad con trigger de hard-delete
    // (control-plane 0006 step 18 / step 15). El REVOKE de DELETE basta para
    // disparar 42501 antes del trigger; el resultado es exactamente el mismo
    // SQLSTATE que el resto de denegaciones por columna/privilegio.
    await expectDeniedByRuntime('DELETE FROM lu_user WHERE id = $1', [s.userId]);
    await expectDeniedByRuntime(
      'DELETE FROM lu_site_membership WHERE user_id = $1 AND site_id = $2',
      [s.userId, s.siteAId],
    );

    // Negado: INSERT/UPDATE en columnas no concedidas del runtime grant.
    // reconciliation_state, username y row_version no están en la lista de
    // INSERT/UPDATE column-level del GRANT de 0006 (lines 1301-1302, 1307-1308).
    await expectDeniedByRuntime(
      `INSERT INTO lu_user
         (id, email, username, first_name, last_name, identity_card, phone_number,
          password_hash, password_scheme, must_change_password,
          is_super_admin, account_status, status, reconciliation_state, security_version)
       VALUES ($1, $2, $3, 'Pgqa', 'Integration', $4, $5,
               $6, 'bcrypt', false,
               false, 'active', 'active', 'canonical', 0)`,
      [
        randomUUID(),
        `evil-${randomUUID()}@example.invalid`,
        `evil-${randomUUID()}`,
        'PEVIL0001',
        '+15555550100',
        '$2b$12$cwX8Zvuf1RsO.CYGKnnT5OiRQ/sGS6ptomphoUa2I1ReqXiiqGJ6i',
      ],
    );
    await expectDeniedByRuntime('UPDATE lu_user SET reconciliation_state = $1 WHERE id = $2', [
      'pending_reconciliation',
      s.userId,
    ]);
    await expectDeniedByRuntime('UPDATE lu_user SET username = $1 WHERE id = $2', [
      `mutated-${randomUUID()}`,
      s.userId,
    ]);
    await expectDeniedByRuntime('UPDATE lu_user SET row_version = row_version + 1 WHERE id = $1', [
      s.userId,
    ]);
    await expectDeniedByRuntime(
      'UPDATE lu_site_membership SET created_at = now() WHERE user_id = $1 AND site_id = $2',
      [s.userId, s.siteAId],
    );
    await expectDeniedByRuntime(
      'UPDATE lu_site_membership SET user_id = user_id WHERE user_id = $1 AND site_id = $2',
      [s.userId, s.siteAId],
    );
    await expectDeniedByRuntime(
      "INSERT INTO lu_site_membership (user_id, site_id, role, status, created_at) VALUES ($1, $2, 'Supervisor', 'active', now())",
      [s.userId, s.siteAId],
    );
    await expectDeniedByRuntime("UPDATE lu_site SET status = 'disabled' WHERE id = $1", [
      s.siteAId,
    ]);

    // Negado: DDL. El REVOKE CREATE del schema y la denegación TEMPORARY de la
    // base dejan el comando CREATE TABLE / CREATE TEMP TABLE en 42501.
    await expectDeniedByRuntime(`CREATE TABLE lu_pgqa_probe_${suffixless()} (id int)`);
    await expectDeniedByRuntime(`CREATE TEMP TABLE lu_pgqa_temp_${suffixless()} (id int)`);
    const maintain = await pool.query<{ allowed: boolean }>(
      `SELECT pg_catalog.has_table_privilege(
         current_user, 'public.lu_session', 'MAINTAIN'
       ) AS allowed`,
    );
    expect(maintain.rows[0]?.allowed).toBe(false);

    // Negado: append-only. lu_security_event y lu_identity_audit_event son
    // append-only por contrato (0002 / 0006 step 15). El REVOKE previo al
    // trigger es lo que materializa el 42501.
    await expectDeniedByRuntime('SELECT * FROM public.lu_security_event');
    await expectDeniedByRuntime(
      `UPDATE public.lu_security_event SET metadata = '{}'::jsonb WHERE user_id = $1`,
      [s.userId],
    );
    await expectDeniedByRuntime('DELETE FROM public.lu_security_event WHERE user_id = $1', [
      s.userId,
    ]);
    await expectDeniedByRuntime(
      `UPDATE public.lu_identity_audit_event SET metadata = '{}'::jsonb
        WHERE subject_user_id = $1`,
      [s.userId],
    );
    await expectDeniedByRuntime(
      'DELETE FROM public.lu_identity_audit_event WHERE subject_user_id = $1',
      [s.userId],
    );

    // Permitido: las operaciones exactas de sesión del repositorio bajo el rol.
    const guard = new Error('auth-pgqa-session-rollback');
    let sessionOpsError: unknown;
    const probeId = randomUUID();
    const probeHash = hashSessionToken(`auth-pgqa-probe-${probeId}`);
    const now = new Date();
    const idle = new Date(now.getTime() + 60_000);
    const absolute = new Date(now.getTime() + 120_000);
    try {
      await pool.transaction(async (client) => {
        const inserted = await client.query(
          `INSERT INTO lu_session
             (id, token_hash, user_id, active_site_id, security_version,
              created_at, last_seen_at, idle_expires_at, absolute_expires_at)
           VALUES ($1, $2, $3, NULL, $4, $5, $5, $6, $7)`,
          [probeId, probeHash, s.userId, s.securityVersion, now, idle, absolute],
        );
        expect(inserted.rowCount).toBe(1);
        const touched = await client.query(
          `UPDATE lu_session
           SET revoked_at = GREATEST(last_seen_at, $2), revocation_reason = 'probe',
               idle_expires_at = GREATEST(idle_expires_at, LEAST($3, absolute_expires_at))
           WHERE id = $1 AND revoked_at IS NULL`,
          [probeId, now, idle],
        );
        expect(touched.rowCount).toBe(1);
        throw guard; // nada de esto debe persistir
      });
    } catch (error) {
      sessionOpsError = error;
    }
    expect(sessionOpsError).toBe(guard);
    const gone = await needAdmin().query<CountRow>(
      'SELECT count(*)::int AS c FROM lu_session WHERE id = $1',
      [probeId],
    );
    expect(gone.rows[0]?.c).toBe(0); // el wrapper revirtió la transacción runtime
  });

  it('6) locks: runtime FOR UPDATE blocks admin revoke (55P03); after release it proceeds and restores', async () => {
    const s = needSeed();
    const now = new Date();

    // UPDATE de admin sobre una membership con lock_timeout corto, siempre
    // revertido para que el estado quede intacto pase lo que pase.
    async function tryAdminRevoke(): Promise<{
      ok: boolean;
      code?: string;
      rowCount: number | null;
    }> {
      const client = needAdmin();
      await client.query('BEGIN');
      try {
        await client.query("SET LOCAL lock_timeout = '1s'");
        const r = await client.query(
          "UPDATE lu_site_membership SET status = 'revoked' WHERE user_id = $1 AND site_id = $2",
          [s.userId, s.siteAId],
        );
        return { ok: true, rowCount: r.rowCount };
      } catch (error) {
        assertNoSecrets(error);
        return { ok: false, code: codeOf(error), rowCount: null };
      } finally {
        await client.query('ROLLBACK');
      }
    }

    // Transacción runtime que adquiere exactamente los locks del repositorio:
    // user FOR UPDATE y memberships+sites FOR UPDATE OF m, s.
    const guard = new Error('auth-pgqa-lock-release');
    let transactionError: unknown;
    try {
      await needPool().transaction(async (client) => {
        await client.query(
          'SELECT id, status, security_version, is_super_admin FROM lu_user WHERE id = $1 FOR UPDATE',
          [s.userId],
        );
        const locked = await client.query(
          `SELECT m.user_id, m.site_id, m.role, m.status, m.valid_from, m.valid_until,
                  s.code AS site_code, s.name AS site_name, s.status AS site_status
           FROM lu_site_membership m
           JOIN lu_site s ON s.id = m.site_id
           WHERE m.user_id = $1
             AND m.status = 'active'
             AND s.status = 'active'
             AND (m.valid_from IS NULL OR m.valid_from <= $2)
             AND (m.valid_until IS NULL OR m.valid_until > $2)
           ORDER BY s.code
           FOR UPDATE OF m, s`,
          [s.userId, now],
        );
        expect(locked.rowCount).toBe(2);
        const blocked = await tryAdminRevoke();
        expect(blocked.ok).toBe(false);
        expect(blocked.code).toBe(SQLSTATE_LOCK_TIMEOUT);
        throw guard; // libera los locks vía ROLLBACK del wrapper
      });
    } catch (error) {
      transactionError = error;
    }
    expect(transactionError).toBe(guard);

    // Locks liberados: el UPDATE puede proceder y se restaura con ROLLBACK.
    const unblocked = await tryAdminRevoke();
    expect(unblocked.ok).toBe(true);
    expect(unblocked.rowCount).toBe(1);
    const status = await needAdmin().query<StatusRow>(
      'SELECT status FROM lu_site_membership WHERE user_id = $1 AND site_id = $2',
      [s.userId, s.siteAId],
    );
    expect(status.rows[0]?.status).toBe('active');

    // Rutas reales del repositorio bajo runtime: FOR UPDATE + GREATEST válidos.
    const { auth } = makeServices();
    const { token } = await loginByEmailFresh(auth);
    await expect(auth.getSession(token)).resolves.toBeDefined();
    const set = await auth.setActiveSite(token, { activeSiteId: s.siteBId });
    expect(set.response.session.activeSiteId).toBe(s.siteBId);
    await auth.logout(token);
    await expect(auth.getSession(token)).rejects.toBeInstanceOf(AuthUnauthorizedException);
  });

  it('7) rollback: ineligible site leaves the session row untouched', async () => {
    const s = needSeed();
    const { auth } = makeServices();
    const { token } = await loginByEmailFresh(auth);
    const before = await fetchSessionRow(token);

    try {
      // Admin deja siteB no elegible; la membership sigue 'active'.
      await needAdmin().query("UPDATE lu_site SET status = 'migrating' WHERE id = $1", [s.siteBId]);
      const error = await capture(() => auth.setActiveSite(token, { activeSiteId: s.siteBId }));
      expect(error).toBeInstanceOf(AuthForbiddenException);
      expect((error as AuthForbiddenException).code).toBe('SITE_ACCESS_DENIED');

      // Fila de sesión inmutada: el UPDATE del repositorio no llegó a cometerse.
      const after = await fetchSessionRow(token);
      expect(after.active_site_id).toBe(before.active_site_id);
      expect(after.last_seen_at.getTime()).toBe(before.last_seen_at.getTime());
      expect(after.idle_expires_at.getTime()).toBe(before.idle_expires_at.getTime());

      // La membership no mutó (el FOR UPDATE del repositorio solo bloquea).
      const membership = await needAdmin().query<StatusRow>(
        'SELECT status FROM lu_site_membership WHERE user_id = $1 AND site_id = $2',
        [s.userId, s.siteBId],
      );
      expect(membership.rows[0]?.status).toBe('active');
    } finally {
      await needAdmin().query("UPDATE lu_site SET status = 'active' WHERE id = $1", [s.siteBId]);
    }

    await auth.logout(token);
  });

  it('8) distributed rate limit persists shared buckets and append-only hashed audit', async () => {
    const s = needSeed();
    const config = new AuthConfig({
      ...TEST_AUTH_ENV,
      AUTH_RATE_LIMIT_MAX_ATTEMPTS: '2',
      AUTH_RATE_LIMIT_IP_MAX_ATTEMPTS: '3',
    });
    const repository = new AuthRepository(needPool(), config);
    const kernel = buildKernel(config);
    const auth = new AuthService(
      config,
      kernel.verifier,
      kernel.hasher,
      kernel.policy,
      kernel.audit,
      kernel.invalidator,
      kernel.window,
      repository,
      new AuthRateLimitService(config, repository),
    );

    await expect(
      auth.login(
        { loginIdentifier: s.email, password: 'wrong-rate-password' },
        RATE_LIMIT_CLIENT_IP,
      ),
    ).rejects.toBeInstanceOf(AuthUnauthorizedException);
    await expect(
      auth.login(
        { loginIdentifier: s.email, password: 'wrong-rate-password' },
        RATE_LIMIT_CLIENT_IP,
      ),
    ).rejects.toBeInstanceOf(AuthUnauthorizedException);
    await expect(
      auth.login(
        { loginIdentifier: s.email, password: 'wrong-rate-password' },
        RATE_LIMIT_CLIENT_IP,
      ),
    ).rejects.toBeInstanceOf(AuthRateLimitException);

    // Derivaciones idénticas a apps/api/src/auth/auth.rate-limit.ts:25-33 con la
    // HMAC key del seed. Los `scope` literales están fijados por la CHECK de
    // 0002 ('email_ip', 'ip'); los `key_hash` usan `rate-identifier-ip` y
    // `rate-ip` como en el slice actual.
    const normalizedEmail = s.email.trim().toLowerCase();
    const normalizedIp = RATE_LIMIT_CLIENT_IP;
    const identifierIpKey = identifierHash(
      'rate-identifier-ip',
      `${normalizedEmail}|${normalizedIp}`,
    );
    const ipKey = identifierHash('rate-ip', normalizedIp);
    const buckets = await needAdmin().query<{ scope: string; attempt_count: number }>(
      `SELECT scope, attempt_count
       FROM public.lu_auth_rate_limit
       WHERE key_hash = ANY($1::bpchar[])
       ORDER BY scope`,
      [[identifierIpKey, ipKey]],
    );
    expect(buckets.rows).toEqual([
      { scope: 'email_ip', attempt_count: 3 },
      { scope: 'ip', attempt_count: 3 },
    ]);

    // Derivaciones idénticas a apps/api/src/auth/auth.service.ts:132-133 y
    // auth.rate-limit.ts:25-26. El sujeto es el identifier normalizado y el IP
    // se trimea antes de hashear.
    const subjectHash = identifierHash('subject', normalizedEmail);
    const ipHash = identifierHash('ip', normalizedIp);
    const events = await needAdmin().query<SecurityEventRow>(
      `SELECT event_type, subject_hash, ip_hash, metadata
       FROM public.lu_security_event
       WHERE subject_hash = $1 AND ip_hash = $2
       ORDER BY occurred_at, event_type`,
      [subjectHash, ipHash],
    );
    expect(events.rows.map((row) => row.event_type).sort()).toEqual(
      ['login_failure', 'login_failure', 'login_rate_limited'].sort(),
    );
    expect(JSON.stringify(events.rows)).not.toContain(s.email);
    expect(JSON.stringify(events.rows)).not.toContain(RATE_LIMIT_CLIENT_IP);

    await expectDeniedByRuntime('SELECT * FROM public.lu_security_event');
    await expectDeniedByRuntime(
      `UPDATE public.lu_security_event SET metadata = '{}'::jsonb WHERE user_id = $1`,
      [s.userId],
    );
    await expectDeniedByRuntime('DELETE FROM public.lu_security_event WHERE user_id = $1', [
      s.userId,
    ]);
  });
});

/** Sufijo [0-9a-f] estable por carga de módulo para nombres de sonda DDL. */
function suffixless(): string {
  return randomUUID().replace(/-/g, '').slice(0, 12);
}
