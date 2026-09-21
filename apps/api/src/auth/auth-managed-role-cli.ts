import { createHash } from 'node:crypto';
import { readFileSync } from 'node:fs';
import path from 'node:path';
import { pathToFileURL } from 'node:url';
import { Pool, type PoolConfig } from 'pg';
import {
  describeTarget,
  loadConnectionConfigFromEnv,
  redactSecrets,
  type PostgresConnectionConfig,
} from '../database/connection-config.js';

const ROLE_FILE = '003_provision_auth_runtime_managed.sql';
const ROLE_FILE_SHA256 = '7fd48d7130140af8e5468d432e9831ae8a2885d3cad62c6a6e046e43e368e85a';
const EXPECTED_LOGIN = 'lu_auth_login';
const EXPECTED_RUNTIME = 'lu_auth_runtime';

type ManagedRoleCommand = 'provision' | 'activate' | 'deactivate' | 'verify';

export interface ManagedRoleConfig {
  readonly database: PostgresConnectionConfig;
  readonly targetDatabase: string;
  readonly expectedAdminUser: string;
  readonly runtimePassword: string | null;
}

function required(env: Record<string, string | undefined>, key: string): string {
  const value = env[key]?.trim();
  if (value === undefined || value === '') {
    throw new Error(`Missing required environment variable "${key}".`);
  }
  return value;
}

export function loadManagedRoleConfig(
  env: Record<string, string | undefined>,
  requirePassword: boolean,
): ManagedRoleConfig {
  const database = loadConnectionConfigFromEnv(env);
  const targetDatabase = required(env, 'AUTH_MANAGED_TARGET_DATABASE');
  const expectedAdminUser = required(env, 'AUTH_MANAGED_ADMIN_USER');
  if (database.database !== targetDatabase || database.user !== expectedAdminUser) {
    throw new Error('Managed role target does not match the approved database and admin user.');
  }

  const rawPassword = env['AUTH_RUNTIME_PASSWORD'] ?? '';
  if (requirePassword && (rawPassword.length < 32 || rawPassword.length > 256)) {
    throw new Error('AUTH_RUNTIME_PASSWORD must contain between 32 and 256 characters.');
  }
  if (rawPassword.includes('\0') || rawPassword.includes('\r') || rawPassword.includes('\n')) {
    throw new Error('AUTH_RUNTIME_PASSWORD contains a forbidden control character.');
  }

  return {
    database,
    targetDatabase,
    expectedAdminUser,
    runtimePassword: requirePassword ? rawPassword : null,
  };
}

function poolConfig(config: PostgresConnectionConfig): PoolConfig {
  return {
    ...config,
    max: 1,
    connectionTimeoutMillis: 10_000,
    idleTimeoutMillis: 10_000,
  };
}

export function roleFilePath(cwd: string): string {
  return path.resolve(cwd, 'database', 'roles', ROLE_FILE);
}

export function verifyRoleFile(content: Buffer): string {
  const hash = createHash('sha256').update(content).digest('hex');
  if (hash !== ROLE_FILE_SHA256) {
    throw new Error(`Managed role SQL hash mismatch for ${ROLE_FILE}.`);
  }
  return hash;
}

async function verifyRoles(pool: Pool, expectedLogin: boolean): Promise<void> {
  const result = await pool.query<{
    rolname: string;
    rolcanlogin: boolean;
    rolinherit: boolean;
    rolsuper: boolean;
    rolcreaterole: boolean;
    rolcreatedb: boolean;
    rolreplication: boolean;
    rolbypassrls: boolean;
  }>(
    `SELECT rolname, rolcanlogin, rolinherit, rolsuper, rolcreaterole,
            rolcreatedb, rolreplication, rolbypassrls
       FROM pg_catalog.pg_roles
      WHERE rolname IN ($1, $2)
      ORDER BY rolname`,
    [EXPECTED_LOGIN, EXPECTED_RUNTIME],
  );
  if (result.rows.length !== 2) {
    throw new Error('Managed Auth roles are incomplete.');
  }
  for (const role of result.rows) {
    const canLogin = role.rolname === EXPECTED_LOGIN ? expectedLogin : false;
    if (
      role.rolcanlogin !== canLogin ||
      role.rolinherit ||
      role.rolsuper ||
      role.rolcreaterole ||
      role.rolcreatedb ||
      role.rolreplication ||
      role.rolbypassrls
    ) {
      throw new Error('Managed Auth role attributes differ from the approved baseline.');
    }
  }
}

async function provision(pool: Pool, cwd: string): Promise<string> {
  const content = readFileSync(roleFilePath(cwd));
  const hash = verifyRoleFile(content);
  await pool.query(content.toString('utf8'));
  await verifyRoles(pool, false);
  return hash;
}

async function setLoginState(pool: Pool, password: string | null): Promise<void> {
  const client = await pool.connect();
  try {
    await client.query('BEGIN');
    if (password === null) {
      await client.query(`ALTER ROLE ${EXPECTED_LOGIN} WITH NOLOGIN PASSWORD NULL`);
    } else {
      await client.query(`SELECT pg_catalog.set_config('lu.runtime_password', $1, true)`, [
        password,
      ]);
      await client.query(`
        DO $activate$
        DECLARE
          runtime_password text := pg_catalog.current_setting('lu.runtime_password', true);
        BEGIN
          IF runtime_password IS NULL OR pg_catalog.length(runtime_password) < 32 THEN
            RAISE EXCEPTION 'runtime password is unavailable';
          END IF;
          EXECUTE pg_catalog.format(
            'ALTER ROLE lu_auth_login WITH LOGIN PASSWORD %L',
            runtime_password
          );
        END
        $activate$
      `);
    }
    await client.query('COMMIT');
  } catch (error) {
    await client.query('ROLLBACK').catch(() => undefined);
    throw error;
  } finally {
    client.release();
  }
  await verifyRoles(pool, password !== null);
}

function parseCommand(value: string | undefined): ManagedRoleCommand {
  if (
    value === 'provision' ||
    value === 'activate' ||
    value === 'deactivate' ||
    value === 'verify'
  ) {
    return value;
  }
  throw new Error('Usage: auth:managed-role <provision|activate|deactivate|verify>');
}

export async function runManagedRoleCli(
  commandValue: string | undefined,
  env: Record<string, string | undefined> = process.env,
  cwd: string = process.cwd(),
): Promise<void> {
  const command = parseCommand(commandValue);
  const config = loadManagedRoleConfig(env, command === 'activate');
  const pool = new Pool(poolConfig(config.database));
  try {
    console.log(`Managed Auth role operation "${command}" on ${describeTarget(config.database)}.`);
    if (command === 'provision') {
      const hash = await provision(pool, cwd);
      console.log(`provision: PASS (${ROLE_FILE}, sha256 ${hash}).`);
    } else if (command === 'activate') {
      await setLoginState(pool, config.runtimePassword!);
      console.log('activate: PASS (runtime login enabled; secret not printed).');
    } else if (command === 'deactivate') {
      await setLoginState(pool, null);
      console.log('deactivate: PASS (runtime login disabled and password removed).');
    } else {
      await verifyRoles(pool, env['AUTH_EXPECT_RUNTIME_LOGIN'] === 'enabled');
      console.log('verify: PASS (managed Auth role attributes).');
    }
  } finally {
    await pool.end();
  }
}

if (process.argv[1] && import.meta.url === pathToFileURL(process.argv[1]).href) {
  runManagedRoleCli(process.argv[2]).catch((error: unknown) => {
    const message = error instanceof Error ? error.message : 'Unknown operational failure.';
    console.error(
      `Managed Auth role operation failed: ${redactSecrets(message, [
        process.env['ConnectionStrings__DefaultConnection'],
        process.env['AUTH_RUNTIME_PASSWORD'],
      ])}`,
    );
    process.exit(1);
  });
}
