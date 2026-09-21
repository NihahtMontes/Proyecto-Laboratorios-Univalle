import { Client } from 'pg';
import { pathToFileURL } from 'node:url';
import {
  CONNECTION_STRING_ENV_VAR,
  describeTarget,
  loadConnectionConfigFromEnv,
  redactSecrets,
} from '../database/connection-config.js';
import { bootstrapInitialAdmin } from './auth-bootstrap.js';

const CONFIRMATION = 'CREATE_INITIAL_ADMIN';

function requiredEnv(name: string): string {
  const value = process.env[name];
  if (value === undefined || value.trim() === '') {
    throw new Error(`Missing required environment variable "${name}".`);
  }
  return value;
}

function parseBcryptCost(): number {
  const raw = process.env['AUTH_BCRYPT_COST']?.trim() || '12';
  if (!/^\d+$/.test(raw)) throw new Error('AUTH_BCRYPT_COST must be an integer.');
  return Number(raw);
}

export async function runBootstrapCli(): Promise<void> {
  if (requiredEnv('AUTH_BOOTSTRAP_CONFIRM') !== CONFIRMATION) {
    throw new Error(`AUTH_BOOTSTRAP_CONFIRM must equal ${CONFIRMATION}.`);
  }

  const connection = loadConnectionConfigFromEnv(process.env);
  const input = {
    expectedDatabase: requiredEnv('AUTH_BOOTSTRAP_CONFIRM_DATABASE').trim(),
    siteCode: requiredEnv('AUTH_BOOTSTRAP_SITE_CODE'),
    siteName: requiredEnv('AUTH_BOOTSTRAP_SITE_NAME'),
    adminEmail: requiredEnv('AUTH_BOOTSTRAP_ADMIN_EMAIL'),
    adminFullName: requiredEnv('AUTH_BOOTSTRAP_ADMIN_FULL_NAME'),
    adminPassword: requiredEnv('AUTH_BOOTSTRAP_ADMIN_PASSWORD'),
    bcryptCost: parseBcryptCost(),
    auditHmacKey: requiredEnv('AUTH_AUDIT_HMAC_KEY'),
  };

  const client = new Client({
    ...connection,
    application_name: 'lu-initial-admin-bootstrap',
    connectionTimeoutMillis: 15_000,
    query_timeout: 120_000,
    statement_timeout: 120_000,
  });

  let connected = false;
  try {
    await client.connect();
    connected = true;
    const result = await bootstrapInitialAdmin(client, input);
    console.log(`Initial administrator bootstrap target: ${describeTarget(connection)}`);
    console.log(`result: ${result.created ? 'CREATED' : 'ALREADY_PRESENT'}`);
    console.log(`siteId: ${result.siteId}`);
    console.log(`userId: ${result.userId}`);
  } finally {
    if (connected) await client.end().catch(() => undefined);
  }
}

async function main(): Promise<void> {
  try {
    await runBootstrapCli();
  } catch (error) {
    const secrets = [
      process.env['AUTH_BOOTSTRAP_ADMIN_PASSWORD'] ?? '',
      process.env['AUTH_AUDIT_HMAC_KEY'] ?? '',
    ];
    try {
      secrets.push(loadConnectionConfigFromEnv(process.env).password);
    } catch {
      // Configuration errors do not echo raw values.
    }
    console.error(`Error: ${redactSecrets((error as Error).message ?? String(error), secrets)}`);
    process.exitCode = 1;
  }
}

if (process.argv[1] && import.meta.url === pathToFileURL(process.argv[1]).href) {
  void main();
}

export { CONNECTION_STRING_ENV_VAR };
