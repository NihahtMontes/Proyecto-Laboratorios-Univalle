/**
 * SuperAdmin grant/revoke CLI (MIG-001 F3). The ONLY path that changes
 * `lu_user.is_super_admin` after bootstrap; no HTTP endpoint can (F1 §8).
 *
 * Usage:
 *   node dist/users/superadmin-cli.js --grant  --user <uuid> --actor <uuid> --reason <text>
 *   node dist/users/superadmin-cli.js --revoke --user <uuid> --actor <uuid> --reason <text>
 *
 * Connection: `ConnectionStrings__ControlPlaneRuntime` with login user
 * `lu_auth_login`; every transaction runs `SET LOCAL ROLE lu_auth_runtime`,
 * like the rest of the runtime. The command itself is SuperAdminRoleService
 * (actor must be an active SuperAdmin, never self-revoke, last-active
 * invariant under the shared advisory lock, security_version rotation and
 * append-only audit), executed over this CLI transaction adapter.
 *
 * Never prints secrets; the DSN password is redacted from error messages.
 */
import { Client, type ClientConfig } from 'pg';
import { randomUUID } from 'node:crypto';
import { pathToFileURL } from 'node:url';
import type { IPgClient, IPgPool } from '../auth/auth.pg-pool.js';
import {
  CONNECTION_STRING_ENV_VAR,
  describeTarget,
  loadConnectionConfigFromEnv,
  parsePostgresConnectionString,
  redactSecrets,
} from '../database/connection-config.js';
import { PgIdentityAuditWriter } from '../identity/identity-audit.writer.js';
import { PgSessionInvalidator } from '../identity/session-invalidator.js';
import { SuperAdminRoleService } from './superadmin-role.service.js';
import { UserRepository } from './user.repository.js';

const EXPECTED_LOGIN_USER = 'lu_auth_login';
const EXPECTED_RUNTIME_ROLE = 'lu_auth_runtime';
const EXPECTED_SEARCH_PATH = 'pg_catalog, public';
const REASON_MIN_LENGTH = 8;
const REASON_MAX_LENGTH = 512;

export interface ParsedSuperAdminArgs {
  readonly command: 'grant' | 'revoke';
  readonly userId: string;
  readonly actorId: string;
  readonly reason: string;
  readonly correlationId: string;
}

export class CliArgError extends Error {
  constructor(message: string) {
    super(message);
    this.name = 'CliArgError';
  }
}

const UUID_REGEX = /^[0-9a-f]{8}-[0-9a-f]{4}-[1-5][0-9a-f]{3}-[89ab][0-9a-f]{3}-[0-9a-f]{12}$/i;
// eslint-disable-next-line no-control-regex
const CONTROL_CHARS = /[\u0000-\u001f\u007f]/;

export function parseSuperAdminArgs(argv: readonly string[]): ParsedSuperAdminArgs {
  let command: 'grant' | 'revoke' | null = null;
  let userId: string | null = null;
  let actorId: string | null = null;
  let reason: string | null = null;
  for (let i = 0; i < argv.length; i += 1) {
    const arg = argv[i];
    if (arg === '--grant' || arg === '--revoke') {
      if (command !== null) throw new CliArgError('Provide exactly one of --grant or --revoke.');
      command = arg === '--grant' ? 'grant' : 'revoke';
    } else if (arg === '--user') {
      userId = argv[++i] ?? null;
    } else if (arg === '--actor') {
      actorId = argv[++i] ?? null;
    } else if (arg === '--reason') {
      reason = argv[++i] ?? null;
    } else {
      throw new CliArgError(`Unknown argument: ${arg}`);
    }
  }
  if (command === null) {
    throw new CliArgError('Either --grant or --revoke must be provided.');
  }
  if (userId === null || !UUID_REGEX.test(userId)) {
    throw new CliArgError('--user must be a canonical UUID.');
  }
  if (actorId === null || !UUID_REGEX.test(actorId)) {
    throw new CliArgError('--actor must be a canonical UUID.');
  }
  const trimmed = reason?.trim() ?? '';
  if (
    trimmed.length < REASON_MIN_LENGTH ||
    trimmed.length > REASON_MAX_LENGTH ||
    CONTROL_CHARS.test(reason ?? '')
  ) {
    throw new CliArgError(
      `--reason must be a single line of ${REASON_MIN_LENGTH}-${REASON_MAX_LENGTH} characters.`,
    );
  }
  return {
    command,
    userId: userId.toLowerCase(),
    actorId: actorId.toLowerCase(),
    reason: trimmed,
    correlationId: randomUUID(),
  };
}

export interface CliClient {
  query<T = Record<string, unknown>>(
    sql: string,
    params?: unknown[],
  ): Promise<{ rows: T[]; rowCount: number | null }>;
  connect(): Promise<void>;
  end(): Promise<void>;
}

/**
 * Adapts one pg client to IPgPool: every transaction assumes the runtime role
 * and search path before running the command, and rolls back on any error.
 */
export class CliTransactionPool implements IPgPool {
  constructor(private readonly client: CliClient) {}

  query<T = Record<string, unknown>>(sql: string, params?: unknown[]) {
    return this.client.query<T>(sql, params);
  }

  async transaction<T>(fn: (client: IPgClient) => Promise<T>): Promise<T> {
    await this.client.query('BEGIN');
    try {
      await this.client.query(`SET LOCAL ROLE ${EXPECTED_RUNTIME_ROLE}`);
      await this.client.query(`SET LOCAL search_path TO ${EXPECTED_SEARCH_PATH}`);
      const result = await fn(this.client);
      await this.client.query('COMMIT');
      return result;
    } catch (error) {
      await this.client.query('ROLLBACK').catch(() => undefined);
      throw error;
    }
  }
}

export function openSuperAdminClient(env: Record<string, string | undefined>): {
  client: CliClient;
  target: ReturnType<typeof describeTarget>;
} {
  const raw = env[CONNECTION_STRING_ENV_VAR];
  if (raw === undefined || raw.trim() === '') {
    throw new CliArgError(`Missing required environment variable "${CONNECTION_STRING_ENV_VAR}".`);
  }
  const parsed = parsePostgresConnectionString(raw);
  if (parsed.user !== EXPECTED_LOGIN_USER) {
    throw new CliArgError(`SuperAdmin CLI requires login user "${EXPECTED_LOGIN_USER}".`);
  }
  const config: ClientConfig = {
    ...parsed,
    application_name: 'lu-users-superadmin-cli',
    connectionTimeoutMillis: 15_000,
    query_timeout: 60_000,
    statement_timeout: 60_000,
  };
  return { client: new Client(config) as unknown as CliClient, target: describeTarget(parsed) };
}

export async function applySuperAdminRole(
  parsed: ParsedSuperAdminArgs,
  client: CliClient,
): Promise<{ status: 'granted' | 'revoked'; targetUserId: string; actorUserId: string }> {
  await client.connect();
  try {
    const pool = new CliTransactionPool(client);
    const service = new SuperAdminRoleService(
      pool,
      new PgSessionInvalidator(),
      new PgIdentityAuditWriter(),
      new UserRepository(pool),
    );
    const input = {
      actorUserId: parsed.actorId,
      targetUserId: parsed.userId,
      reason: parsed.reason,
      correlationId: parsed.correlationId,
    };
    return parsed.command === 'grant' ? await service.grant(input) : await service.revoke(input);
  } finally {
    await client.end().catch(() => undefined);
  }
}

export async function runSuperAdminCli(argv: readonly string[]): Promise<void> {
  const parsed = parseSuperAdminArgs(argv);
  const { client, target } = openSuperAdminClient(process.env);
  const result = await applySuperAdminRole(parsed, client);
  console.log(`SuperAdmin role CLI target: ${target}`);
  console.log(`result: ${result.status.toUpperCase()}`);
  console.log(`targetUserId: ${result.targetUserId}`);
  console.log(`actorUserId: ${result.actorUserId}`);
}

async function main(): Promise<void> {
  try {
    await runSuperAdminCli(process.argv.slice(2));
  } catch (error) {
    const secrets: string[] = [];
    try {
      secrets.push(loadConnectionConfigFromEnv(process.env).password);
    } catch {
      // Connection configuration may itself be the error.
    }
    const message = error instanceof Error ? error.message : String(error);
    console.error(`Error: ${redactSecrets(message, secrets)}`);
    process.exit(1);
  }
}

if (process.argv[1] && import.meta.url === pathToFileURL(process.argv[1]).href) {
  void main();
}
