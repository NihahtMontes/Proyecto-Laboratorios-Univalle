/**
 * MIG-F3-AUTH-PGQA-012 — E2E del contrato Auth sobre PostgreSQL real (opt-in).
 *
 * Se salta por completo salvo `AUTH_PG_INTEGRATION=1`. Al activarla:
 *  - Lee admin/migrador SOLO de `ConnectionStrings__DefaultConnection` con el
 *    parser existente; el raw nunca se imprime (mensajes saneados con
 *    `redactSecrets`).
 *  - Requiere `lu_auth_login` y `lu_auth_runtime` aprovisionados por 012B.
 *    Activa el login con un secreto aleatorio solo durante esta suite y prueba
 *    el AuthPgPool real, que asume el rol runtime en cada transacción.
 *  - Datos 100 % sintéticos (UUIDs y `example.invalid`): 2 sites active, 1 user
 *    active bcrypt cost 12, 2 memberships active. afterAll limpia por IDs
 *    estrictos en orden sessions -> memberships -> user -> sites, incluso si
 *    una prueba falla, y cierra pools/clients.
 *
 * Ejecución autorizada (sandbox migrado, p. ej. GastroExample):
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
import type { ActiveSiteSession } from '../src/auth/auth.types.js';
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

const TEST_AUTH_ENV: Record<string, string | undefined> = {
  AUTH_SESSION_IDLE_TTL_SECONDS: '1800',
  AUTH_SESSION_ABSOLUTE_TTL_SECONDS: '43200',
  AUTH_LOGIN_FLOOR_MS: '0',
  AUTH_BCRYPT_COST: '12',
  // La rate limit no es el objeto de esta suite; se instancia nueva por prueba
  // y además se eleva el techo para que nunca interfiera.
  AUTH_RATE_LIMIT_MAX_ATTEMPTS: '50',
  AUTH_RATE_LIMIT_IP_MAX_ATTEMPTS: '100',
  AUTH_RATE_LIMIT_WINDOW_SECONDS: '60',
  AUTH_AUDIT_HMAC_KEY: 'pgqa-only-auth-hmac-key-32-characters-long',
};

type Seed = {
  readonly siteAId: string;
  readonly siteBId: string;
  readonly userId: string;
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
  let cleanupIds: Pick<Seed, 'siteAId' | 'siteBId' | 'userId' | 'email'> | null = null;
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

  function makeServices(): { repository: AuthRepository; auth: AuthService } {
    const config = new AuthConfig(TEST_AUTH_ENV);
    const repository = new AuthRepository(needPool(), config);
    const rateLimit = new AuthRateLimitService(config, repository);
    const auth = new AuthService(config, repository, rateLimit);
    return { repository, auth };
  }

  function identifierHash(purpose: string, value: string): string {
    return hashAuthIdentifier(TEST_AUTH_ENV.AUTH_AUDIT_HMAC_KEY!, purpose, value);
  }

  /** Login sintético nuevo por prueba: evita dependencias de orden y de sesión. */
  async function loginFresh(
    auth: AuthService,
  ): Promise<{ token: string; session: ActiveSiteSession }> {
    const s = needSeed();
    return auth.login({ email: s.email, password: s.password }, CLIENT_IP);
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

      if (!isLoopback(connection.host) || connection.database !== 'GastroExample') {
        throw new Error('Integration target must be loopback/GastroExample.');
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
      if (target.rows[0]?.database_name !== 'GastroExample') {
        throw new Error('Connected database is not the approved GastroExample sandbox.');
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

      // Identificadores quedan disponibles para cleanup antes de escribir.
      const suffix = randomUUID().replace(/-/g, '').slice(0, 12);
      const siteAId = randomUUID();
      const siteBId = randomUUID();
      const userId = randomUUID();
      const email = `lu-pgqa-${suffix}@example.invalid`;
      const password = `Pgqa-${suffix}-Rotate#7`;
      const passwordHash = await bcrypt.hash(password, 12);
      cleanupIds = { siteAId, siteBId, userId, email };

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
        await admin.query(
          `INSERT INTO public.lu_user
             (id, email, full_name, password_hash, is_super_admin, status)
         VALUES ($1, $2, $3, $4, false, 'active')`,
          [userId, email, 'PGQA Integration User', passwordHash],
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

      originalRuntimeConnection = process.env['ConnectionStrings__ControlPlaneRuntime'];
      process.env['ConnectionStrings__ControlPlaneRuntime'] =
        `Host=${connection.host};Port=${connection.port};Database=GastroExample;` +
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
            const subjectHash = identifierHash('subject', cleanupIds.email.toLowerCase());
            const ipHashes = [CLIENT_IP, RATE_LIMIT_CLIENT_IP].map((ip) =>
              identifierHash('ip', ip),
            );
            const rateKeys = [
              identifierHash('rate-email-ip', `${cleanupIds.email.toLowerCase()}|${CLIENT_IP}`),
              identifierHash(
                'rate-email-ip',
                `${cleanupIds.email.toLowerCase()}|${RATE_LIMIT_CLIENT_IP}`,
              ),
              identifierHash('rate-ip', CLIENT_IP),
              identifierHash('rate-ip', RATE_LIMIT_CLIENT_IP),
            ];
            await admin.query('BEGIN');
            try {
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
              await admin.query('DELETE FROM public.lu_user WHERE id = $1 AND email = $2', [
                cleanupIds.userId,
                cleanupIds.email,
              ]);
              await admin.query('DELETE FROM public.lu_site WHERE id = ANY($1::uuid[])', [
                [cleanupIds.siteAId, cleanupIds.siteBId],
              ]);
              await admin.query('COMMIT');
            } catch (error) {
              await admin.query('ROLLBACK');
              throw error;
            }
            const residue = await admin.query<CountRow>(
              `SELECT (
                   (SELECT count(*) FROM public.lu_session WHERE user_id = $1) +
                  (SELECT count(*) FROM public.lu_security_event
                   WHERE user_id = $1 OR subject_hash = $3 OR ip_hash = ANY($4::bpchar[])) +
                  (SELECT count(*) FROM public.lu_auth_rate_limit
                   WHERE key_hash = ANY($5::bpchar[])) +
                  (SELECT count(*) FROM public.lu_site_membership WHERE user_id = $1) +
                  (SELECT count(*) FROM public.lu_user WHERE id = $1) +
                  (SELECT count(*) FROM public.lu_site WHERE id = ANY($2::uuid[]))
                )::int AS c`,
              [
                cleanupIds.userId,
                [cleanupIds.siteAId, cleanupIds.siteBId],
                subjectHash,
                ipHashes,
                rateKeys,
              ],
            );
            if ((residue.rows[0]?.c ?? -1) !== 0) failures.push('cleanup:residue');
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

  it('2) real login -> bcrypt -> session with sha256 hash -> wire -> setActiveSite -> logout 401', async () => {
    const s = needSeed();
    const { auth } = makeServices();

    const wrong = await capture(() =>
      auth.login({ email: s.email, password: 'wrong-password-x' }, CLIENT_IP),
    );
    expect(wrong).toBeInstanceOf(AuthUnauthorizedException);

    const { token, session } = await loginFresh(auth);
    expectWireContract(session);
    expect(session.userId).toBe(s.userId);
    expect(session.globalRole).toBeNull();
    // Dos memberships elegibles sin activeSiteId -> sesión global hasta elegir sede.
    expect(session.activeSiteId).toBeNull();
    expect(session.activeSiteName).toBeNull();
    expect(session.memberships).toHaveLength(2);
    expect([...session.memberships].map((m) => m.siteId).sort()).toEqual(
      [s.siteAId, s.siteBId].sort(),
    );
    expect(session.memberships.find((m) => m.siteId === s.siteAId)?.role).toBe('Administrador');
    expect(session.memberships.find((m) => m.siteId === s.siteBId)?.role).toBe('Supervisor');
    // El wire jamás expone el hash de clave.
    expect(JSON.stringify(session)).not.toContain(s.passwordHash);

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
    expectWireContract(wire);
    expect(wire.userId).toBe(s.userId);
    expect(wire.activeSiteId).toBeNull();

    // setActiveSite a la segunda membership (commit real bajo el wrapper runtime).
    const after = await auth.setActiveSite(token, { activeSiteId: s.siteBId });
    expect(after.activeSiteId).toBe(s.siteBId);
    const siteBName = after.memberships.find((m) => m.siteId === s.siteBId)?.siteName;
    expect(siteBName).toBeDefined();
    expect(after.activeSiteName).toBe(siteBName);
    expect((await fetchSessionRow(token)).active_site_id).toBe(s.siteBId);

    // logout = UPDATE (no DELETE); la sesión revocada deja de autorizar (401).
    await auth.logout(token);
    const revoked = await fetchSessionRow(token);
    expect(revoked.revoked_at).not.toBeNull();
    expect(revoked.revocation_reason).toBe('logout');
    await expect(auth.getSession(token)).rejects.toBeInstanceOf(AuthUnauthorizedException);

    const audit = await needAdmin().query<SecurityEventRow>(
      `SELECT event_type, subject_hash, ip_hash, metadata
       FROM public.lu_security_event
       WHERE user_id = $1
       ORDER BY occurred_at, event_type`,
      [s.userId],
    );
    expect(audit.rows.map((row) => row.event_type).sort()).toEqual(
      ['active_site_changed', 'login_failure', 'login_success', 'logout'].sort(),
    );
    const credentialEvents = audit.rows.filter((row) => row.event_type.startsWith('login_'));
    expect(credentialEvents).toHaveLength(2);
    for (const event of credentialEvents) {
      expect(event.subject_hash).toMatch(/^[0-9a-f]{64}$/);
      expect(event.ip_hash).toMatch(/^[0-9a-f]{64}$/);
      expect(JSON.stringify(event)).not.toContain(s.email);
      expect(JSON.stringify(event)).not.toContain(CLIENT_IP);
    }
  });

  it('3) setActiveSite to a foreign/no-membership site rolls back and keeps the session', async () => {
    const { auth } = makeServices();
    const { token } = await loginFresh(auth);
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
    const { token } = await loginFresh(auth);
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
    const fresh = await loginFresh(auth);
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
    // Negado: INSERT/UPDATE sobre lu_user (incluido status) y memberships.
    await expectDeniedByRuntime(
      `INSERT INTO lu_user (id, email, full_name, password_hash, is_super_admin, status, security_version)
       VALUES ($1, $2, $3, $4, false, 'active', 0)`,
      [randomUUID(), `evil-${randomUUID()}@example.invalid`, 'Evil', '$2b$12$not-a-real-hash'],
    );
    await expectDeniedByRuntime("UPDATE lu_user SET status = 'disabled' WHERE id = $1", [s.userId]);
    await expectDeniedByRuntime(
      "UPDATE lu_site_membership SET status = 'revoked' WHERE user_id = $1 AND site_id = $2",
      [s.userId, s.siteAId],
    );
    await expectDeniedByRuntime(
      "INSERT INTO lu_site_membership (user_id, site_id, role, status) VALUES ($1, $2, 'Supervisor', 'revoked')",
      [s.userId, s.siteAId],
    );
    await expectDeniedByRuntime("UPDATE lu_site SET status = 'disabled' WHERE id = $1", [
      s.siteAId,
    ]);
    // Negado: DDL.
    await expectDeniedByRuntime(`CREATE TABLE lu_pgqa_probe_${suffixless()} (id int)`);
    await expectDeniedByRuntime(`CREATE TEMP TABLE lu_pgqa_temp_${suffixless()} (id int)`);
    const maintain = await pool.query<{ allowed: boolean }>(
      `SELECT pg_catalog.has_table_privilege(
         current_user, 'public.lu_session', 'MAINTAIN'
       ) AS allowed`,
    );
    expect(maintain.rows[0]?.allowed).toBe(false);

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
    const { token } = await loginFresh(auth);
    await expect(auth.getSession(token)).resolves.toBeDefined();
    const set = await auth.setActiveSite(token, { activeSiteId: s.siteBId });
    expect(set.activeSiteId).toBe(s.siteBId);
    await auth.logout(token);
    await expect(auth.getSession(token)).rejects.toBeInstanceOf(AuthUnauthorizedException);
  });

  it('7) rollback: ineligible site leaves the session row untouched', async () => {
    const s = needSeed();
    const { auth } = makeServices();
    const { token } = await loginFresh(auth);
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
    const auth = new AuthService(config, repository, new AuthRateLimitService(config, repository));

    await expect(
      auth.login({ email: s.email, password: 'wrong-rate-password' }, RATE_LIMIT_CLIENT_IP),
    ).rejects.toBeInstanceOf(AuthUnauthorizedException);
    await expect(
      auth.login({ email: s.email, password: 'wrong-rate-password' }, RATE_LIMIT_CLIENT_IP),
    ).rejects.toBeInstanceOf(AuthUnauthorizedException);
    await expect(
      auth.login({ email: s.email, password: 'wrong-rate-password' }, RATE_LIMIT_CLIENT_IP),
    ).rejects.toBeInstanceOf(AuthRateLimitException);

    const emailIpKey = identifierHash(
      'rate-email-ip',
      `${s.email.toLowerCase()}|${RATE_LIMIT_CLIENT_IP}`,
    );
    const ipKey = identifierHash('rate-ip', RATE_LIMIT_CLIENT_IP);
    const buckets = await needAdmin().query<{ scope: string; attempt_count: number }>(
      `SELECT scope, attempt_count
       FROM public.lu_auth_rate_limit
       WHERE key_hash = ANY($1::bpchar[])
       ORDER BY scope`,
      [[emailIpKey, ipKey]],
    );
    expect(buckets.rows).toEqual([
      { scope: 'email_ip', attempt_count: 3 },
      { scope: 'ip', attempt_count: 3 },
    ]);

    const subjectHash = identifierHash('subject', s.email.toLowerCase());
    const ipHash = identifierHash('ip', RATE_LIMIT_CLIENT_IP);
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
