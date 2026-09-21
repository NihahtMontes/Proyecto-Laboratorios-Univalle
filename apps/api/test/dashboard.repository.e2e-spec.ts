import { DashboardRepository } from '../src/dashboard/dashboard.repository.js';
import type { IPgPool } from '../src/auth/auth.pg-pool.js';

class EmptyTenantPool implements IPgPool {
  calls = 0;

  async query<T = Record<string, unknown>>(): Promise<{ rows: T[]; rowCount: number | null }> {
    this.calls += 1;
    return { rows: [], rowCount: 0 };
  }

  async transaction<T>(): Promise<T> {
    throw new Error('transaction is not used by the dashboard read path');
  }
}

describe('DashboardRepository', () => {
  it('returns a real empty state when the tenant has no active management', async () => {
    const pool = new EmptyTenantPool();
    const result = await new DashboardRepository().loadSummary(
      pool,
      '11111111-1111-4111-8111-111111111111',
      {},
    );

    expect(result.activeManagement).toBeNull();
    expect(result.metrics.totalAssets).toBe(0);
    expect(result.plans).toEqual([]);
    expect(pool.calls).toBe(1);
  });
});
