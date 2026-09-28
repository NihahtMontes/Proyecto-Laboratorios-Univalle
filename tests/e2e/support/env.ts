/**
 * Runtime values handed over by `stack/run-f7.mjs` through the environment.
 * The suite never starts or provisions anything by itself.
 */
export interface SiteFixture {
  readonly id: string;
  readonly code: string;
  readonly name: string;
}

export interface StackFixtures {
  readonly runId: string;
  readonly sites: { readonly a: SiteFixture; readonly b: SiteFixture };
}

function required(name: string): string {
  const value = process.env[name];
  if (value === undefined || value.trim() === '') {
    throw new Error(`${name} is not set. Run the suite through tests/e2e/stack/run-f7.mjs.`);
  }
  return value;
}

export const BASE_URL = process.env['E2E_BASE_URL'] ?? 'http://127.0.0.1:5173';

let cached: StackFixtures | null = null;
export function stack(): StackFixtures {
  cached ??= JSON.parse(required('E2E_FIXTURES')) as StackFixtures;
  return cached;
}

export const fixtureDsn = {
  control: (): string => required('E2E_FIXTURE_CONTROL_DSN'),
  tenantA: (): string => required('E2E_FIXTURE_TENANT_A_DSN'),
  tenantB: (): string => required('E2E_FIXTURE_TENANT_B_DSN'),
};
