/**
 * Direct fixture access to the run's disposable databases, authenticated as the
 * run's temporary migration executor (member of the object-owning group). Used
 * ONLY to arrange preconditions the product cannot create through its own UI
 * (legacy hashes, deleted accounts, expired sessions) and to observe persisted
 * effects. Every database is dropped by the runner at the end of the run.
 */
import { randomBytes, randomUUID } from 'node:crypto';
import pg from 'pg';
import { fixtureDsn, stack } from './env.js';
import { aspNetIdentityV2, aspNetIdentityV3, bcryptHash } from './hashes.js';

type Dsn = () => string;

function parseDsn(raw: string): pg.ClientConfig {
  const kv: Record<string, string> = {};
  for (const part of raw.split(';')) {
    const i = part.indexOf('=');
    if (i > 0) kv[part.slice(0, i).trim().toLowerCase()] = part.slice(i + 1);
  }
  return {
    host: kv['host'],
    port: Number(kv['port'] ?? 5432),
    database: kv['database'],
    user: kv['username'],
    password: kv['password'],
    ssl: false,
  };
}

export async function withDb<T>(dsn: Dsn, fn: (c: pg.Client) => Promise<T>): Promise<T> {
  const client = new pg.Client({ ...parseDsn(dsn()), application_name: 'lu-f7-fixture' });
  await client.connect();
  try {
    await client.query('BEGIN');
    await client.query('SET LOCAL ROLE lu_auth_migrator');
    const result = await fn(client);
    await client.query('COMMIT');
    return result;
  } catch (error) {
    await client.query('ROLLBACK').catch(() => undefined);
    throw error;
  } finally {
    await client.end().catch(() => undefined);
  }
}
export const controlDb = <T>(fn: (c: pg.Client) => Promise<T>): Promise<T> => withDb(fixtureDsn.control, fn);
export const tenantDb = <T>(site: 'a' | 'b', fn: (c: pg.Client) => Promise<T>): Promise<T> =>
  withDb(site === 'a' ? fixtureDsn.tenantA : fixtureDsn.tenantB, fn);

let counter = 0;
/** Unique, lowercase, short token for per-test identities. */
export function uniqueToken(): string {
  counter += 1;
  return `${randomBytes(3).toString('hex')}${counter.toString(36)}`;
}

/** Policy-compliant synthetic password (>= 12 chars, upper, lower, digit, symbol). */
export function syntheticPassword(label = 'Pw'): string {
  return `F7-${label}#${randomBytes(4).toString('hex')}Aa1`;
}

export type SiteKey = 'a' | 'b';
export type Role = 'Administrador' | 'Supervisor';
export type PasswordKind = 'bcrypt' | 'legacy_v2' | 'legacy_v3' | 'reset_required';

export interface MembershipSeed {
  readonly site: SiteKey;
  readonly role: Role;
  readonly status?: 'active' | 'suspended' | 'revoked';
}

export interface UserSeed {
  readonly label?: string;
  readonly password?: string;
  readonly passwordKind?: PasswordKind;
  readonly mustChangePassword?: boolean;
  readonly superAdmin?: boolean;
  readonly accountStatus?: 'active' | 'inactive' | 'deleted';
  readonly memberships?: readonly MembershipSeed[];
  readonly firstName?: string;
  readonly lastName?: string;
}

export interface SeededUser {
  readonly id: string;
  readonly username: string;
  readonly email: string;
  readonly password: string;
  readonly firstName: string;
  readonly lastName: string;
  readonly displayName: string;
  readonly identityCard: string;
}

export async function seedUser(seed: UserSeed = {}): Promise<SeededUser> {
  const token = uniqueToken();
  const label = (seed.label ?? 'user').toLowerCase().replace(/[^a-z0-9]/g, '');
  const username = `f7${label}${token}`.slice(0, 40);
  const email = `${username}@f7.example.invalid`;
  const password = seed.password ?? syntheticPassword(label.slice(0, 6) || 'Pw');
  const kind = seed.passwordKind ?? 'bcrypt';
  const scheme =
    kind === 'legacy_v2' ? 'legacy_identity_v2' : kind === 'legacy_v3' ? 'legacy_identity_v3' : kind;
  const hash =
    kind === 'bcrypt'
      ? bcryptHash(password)
      : kind === 'legacy_v2'
        ? aspNetIdentityV2(password)
        : kind === 'legacy_v3'
          ? aspNetIdentityV3(password)
          : null;
  const mustChange = kind === 'reset_required' ? true : kind === 'bcrypt' ? (seed.mustChangePassword ?? false) : false;
  const firstName = seed.firstName ?? 'Prueba';
  const lastName = seed.lastName ?? `F${token.toUpperCase().replace(/[^A-Z]/g, 'X')}`;
  const identityCard = `F7${randomBytes(4).toString('hex').toUpperCase()}`.slice(0, 10);
  const phone = `7${String(Date.now() % 1_000_000).padStart(6, '0')}${counter % 10}`;
  const id = randomUUID();
  const status = seed.accountStatus ?? 'active';
  const sites = stack().sites;
  await controlDb(async (c) => {
    await c.query(
      `INSERT INTO public.lu_user
         (id, email, username, first_name, last_name, identity_card, phone_number,
          password_hash, password_scheme, must_change_password, is_super_admin,
          account_status, status, reconciliation_state)
       VALUES ($1,$2,$3,$4,$5,$6,$7,$8,$9,$10,$11,$12,$12,'canonical')`,
      [id, email, username, firstName, lastName, identityCard, phone, hash, scheme, mustChange, seed.superAdmin === true, status],
    );
    for (const m of seed.memberships ?? []) {
      await c.query(
        `INSERT INTO public.lu_site_membership (user_id, site_id, role, status) VALUES ($1,$2,$3,$4)`,
        [id, sites[m.site].id, m.role, m.status ?? 'active'],
      );
    }
  });
  return { id, username, email, password, firstName, lastName, displayName: `${firstName} ${lastName}`, identityCard };
}

export interface UserRow {
  readonly account_status: string;
  readonly password_scheme: string;
  readonly must_change_password: boolean;
  readonly password_migrated_at: Date | null;
  readonly security_version: string;
  readonly profile_picture_key: string | null;
  readonly first_name: string;
  readonly last_name: string;
  readonly email: string;
  readonly phone_number: string | null;
}
/** Observes persisted state. Never returns the hash itself. */
export async function readUser(id: string): Promise<UserRow & { bcrypt_prefix: string | null }> {
  return controlDb(async (c) => {
    const r = await c.query(
      `SELECT account_status, password_scheme, must_change_password, password_migrated_at,
              security_version::text, profile_picture_key, first_name, last_name, email, phone_number,
              CASE WHEN password_scheme = 'bcrypt' THEN substr(password_hash, 1, 7) END AS bcrypt_prefix
         FROM public.lu_user WHERE id = $1`,
      [id],
    );
    if (r.rows.length !== 1) throw new Error('user not found');
    return r.rows[0];
  });
}

export async function readMemberships(userId: string): Promise<Array<{ site_id: string; role: string; status: string }>> {
  return controlDb(async (c) =>
    (await c.query(`SELECT site_id, role, status FROM public.lu_site_membership WHERE user_id = $1 ORDER BY site_id`, [userId])).rows,
  );
}

export async function findUserIdByUsername(username: string): Promise<string | null> {
  return controlDb(async (c) => {
    const r = await c.query(`SELECT id FROM public.lu_user WHERE username = lower($1)`, [username]);
    return r.rows[0]?.id ?? null;
  });
}

/** Returns the stored password hash for the user (test-only; expose carefully). */
export async function readPasswordHash(userId: string): Promise<string | null> {
  return controlDb(async (c) => {
    const r = await c.query<{ password_hash: string | null }>(
      `SELECT password_hash FROM public.lu_user WHERE id = $1`,
      [userId],
    );
    return r.rows[0]?.password_hash ?? null;
  });
}

/** Counts live (not revoked, not expired) sessions of a user. */
export async function liveSessionCount(userId: string): Promise<number> {
  return controlDb(async (c) => {
    const r = await c.query(
      `SELECT count(*)::int AS n FROM public.lu_session
        WHERE user_id = $1 AND revoked_at IS NULL AND idle_expires_at > now() AND absolute_expires_at > now()`,
      [userId],
    );
    return r.rows[0].n as number;
  });
}

/** Expires every live session of the user server-side (idle deadline moved to the past). */
export async function expireSessions(userId: string): Promise<void> {
  await controlDb(async (c) => {
    await c.query(
      `UPDATE public.lu_session
          SET idle_expires_at = GREATEST(created_at + interval '1 millisecond', now() - interval '1 second'),
              last_seen_at = created_at
        WHERE user_id = $1 AND revoked_at IS NULL`,
      [userId],
    );
  });
}

/** Revokes every live session of the user server-side. */
export async function revokeSessions(userId: string): Promise<void> {
  await controlDb(async (c) => {
    await c.query(
      `UPDATE public.lu_session SET revoked_at = now(), revocation_reason = 'f7_fixture'
        WHERE user_id = $1 AND revoked_at IS NULL`,
      [userId],
    );
  });
}

export interface PersonSeed {
  readonly site: SiteKey;
  readonly name?: string;
  readonly email?: string;
  readonly personType?: 'internal' | 'external';
  readonly status?: 0 | 1 | 2;
  readonly actorCode?: string;
}
export async function seedPerson(seed: PersonSeed): Promise<{ id: string; name: string }> {
  const name = seed.name ?? `Persona F7 ${uniqueToken()}`;
  const siteId = stack().sites[seed.site].id;
  return tenantDb(seed.site, async (c) => {
    const r = await c.query(
      `INSERT INTO public.lu_person (site_id, actor_code, person_type, name, email, address, status)
       VALUES ($1,$2,$3,$4,$5,$6,$7) RETURNING id::text`,
      [
        siteId,
        seed.actorCode ?? null,
        seed.personType ?? 'internal',
        name,
        seed.email ?? null,
        seed.personType === 'external' ? 'Av. Sintetica 123' : null,
        seed.status ?? 0,
      ],
    );
    return { id: r.rows[0].id as string, name };
  });
}
