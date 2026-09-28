import { PgIdentityAuditWriter } from '../src/identity/identity-audit.writer.js';
import { PgLegacyPasswordWindow } from '../src/identity/legacy-password-window.js';
import { PgSessionInvalidator } from '../src/identity/session-invalidator.js';
import type { IPgClient } from '../src/auth/auth.pg-pool.js';
import type { SecurityRotationReason } from '../src/identity/identity.contracts.js';

interface RecordedCall {
  readonly sql: string;
  readonly params: readonly unknown[];
}

class FakePgClient implements IPgClient {
  readonly calls: RecordedCall[] = [];
  private readonly responses: Array<{ rows: unknown[]; rowCount: number | null }>;

  constructor(responses: ReadonlyArray<{ rows: unknown[]; rowCount: number | null }>) {
    this.responses = [...responses];
  }

  async query<T = Record<string, unknown>>(
    sql: string,
    params?: unknown[],
  ): Promise<{ rows: T[]; rowCount: number | null }> {
    this.calls.push({ sql, params: params ?? [] });
    const next = this.responses.shift();
    if (next === undefined) {
      throw new Error('FakePgClient: no scripted response for next call.');
    }
    return next as unknown as { rows: T[]; rowCount: number | null };
  }
}

describe('PgSessionInvalidator', () => {
  it('issues a security_version increment and revokes sessions in order', async () => {
    const client = new FakePgClient([
      { rows: [{ security_version: '5' }], rowCount: 1 },
      { rows: [], rowCount: 3 },
    ]);
    const invalidator = new PgSessionInvalidator();
    const newVersion = await invalidator.rotateAndRevokeAll(
      client,
      '11111111-1111-4111-8111-111111111111',
      'email_change',
    );
    expect(newVersion).toBe('5');
    expect(client.calls).toHaveLength(2);
    expect(client.calls[0]?.sql).toMatch(/UPDATE public\.lu_user/i);
    expect(client.calls[0]?.sql).toMatch(/security_version\s*=\s*security_version\s*\+\s*1/);
    expect(client.calls[0]?.sql).toMatch(/RETURNING security_version::text/);
    expect(client.calls[0]?.params).toEqual(['11111111-1111-4111-8111-111111111111']);
    expect(client.calls[1]?.sql).toMatch(/UPDATE public\.lu_session/i);
    expect(client.calls[1]?.sql).toMatch(/revoked_at\s*=\s*now\(\)/);
    expect(client.calls[1]?.sql).toMatch(/revocation_reason\s*=\s*\$1/);
    expect(client.calls[1]?.sql).toMatch(/revoked_at IS NULL/);
    expect(client.calls[1]?.params).toEqual([
      'email_change',
      '11111111-1111-4111-8111-111111111111',
    ]);
  });

  it('throws when the UPDATE on lu_user returns zero rows', async () => {
    const client = new FakePgClient([{ rows: [], rowCount: 0 }]);
    const invalidator = new PgSessionInvalidator();
    await expect(invalidator.rotateAndRevokeAll(client, 'ghost', 'email_change')).rejects.toThrow(
      /expected exactly one lu_user row/i,
    );
  });

  it('throws when the UPDATE on lu_user returns more than one row', async () => {
    const client = new FakePgClient([
      { rows: [{ security_version: '1' }, { security_version: '2' }], rowCount: 2 },
    ]);
    const invalidator = new PgSessionInvalidator();
    await expect(
      invalidator.rotateAndRevokeAll(client, 'dup', 'membership_change'),
    ).rejects.toThrow(/expected exactly one lu_user row/i);
  });

  it('does not call the lu_session UPDATE when the user-version UPDATE is missing', async () => {
    const client = new FakePgClient([{ rows: [], rowCount: 0 }]);
    const invalidator = new PgSessionInvalidator();
    await expect(
      invalidator.rotateAndRevokeAll(client, 'no-user', 'global_role_change'),
    ).rejects.toThrow(/expected exactly one lu_user row/i);
    expect(client.calls).toHaveLength(1);
  });

  it.each<SecurityRotationReason>([
    'email_change',
    'username_repair',
    'self_password_change',
    'admin_password_reset',
    'credential_rehash',
    'global_role_change',
    'site_role_change',
    'membership_change',
    'account_status_change',
    'access_revoked',
    'access_restored',
    'legacy_retirement',
  ])('maps the typed reason "%s" to a non-blank revocation_reason string', async (reason) => {
    const client = new FakePgClient([
      { rows: [{ security_version: '7' }], rowCount: 1 },
      { rows: [], rowCount: 0 },
    ]);
    const invalidator = new PgSessionInvalidator();
    await invalidator.rotateAndRevokeAll(client, '22222222-2222-4222-8222-222222222222', reason);
    const revokeParams = client.calls[1]?.params;
    expect(typeof revokeParams?.[0]).toBe('string');
    expect((revokeParams?.[0] as string).length).toBeGreaterThan(0);
    // Map is identity-by-name; the test pins the wiring through the
    // session-revocation predicate (reason field is one of the non-blank
    // values allowed by `ck_lu_session`).
  });

  it('rejects an empty userId', async () => {
    const invalidator = new PgSessionInvalidator();
    const client = new FakePgClient([]);
    await expect(invalidator.rotateAndRevokeAll(client, '', 'email_change')).rejects.toThrow(
      /userId is required/,
    );
  });
});

describe('PgIdentityAuditWriter', () => {
  it('inserts one row per call with all granted columns populated', async () => {
    const client = new FakePgClient([{ rows: [], rowCount: 1 }]);
    const writer = new PgIdentityAuditWriter();
    await writer.append(client, {
      action: 'sessions_revoked',
      subjectUserId: '11111111-1111-4111-8111-111111111111',
      actorUserId: '22222222-2222-4222-8222-222222222222',
      siteId: '33333333-3333-4333-8333-333333333333',
      correlationId: '44444444-4444-4444-8444-444444444444',
      reason: 'manual revoke',
      beforeStatus: 'active',
      afterStatus: 'inactive',
      metadata: { source: 'admin', force: true, count: 3 },
    });
    expect(client.calls).toHaveLength(1);
    const call = client.calls[0]!;
    expect(call.sql).toMatch(/INSERT INTO public\.lu_identity_audit_event/i);
    expect(call.sql).toMatch(/\$\d+::jsonb/);
    expect(call.params[0]).toEqual(expect.any(String)); // id (uuid)
    expect(call.params[1]).toBe('sessions_revoked');
    expect(call.params[2]).toBe('11111111-1111-4111-8111-111111111111');
    expect(call.params[3]).toBe('22222222-2222-4222-8222-222222222222');
    expect(call.params[4]).toBe('33333333-3333-4333-8333-333333333333');
    expect(call.params[5]).toBe('44444444-4444-4444-8444-444444444444');
    expect(call.params[6]).toBe('manual revoke');
    expect(call.params[7]).toBe('active');
    expect(call.params[8]).toBe('inactive');
    expect(call.params[9]).toBe(JSON.stringify({ source: 'admin', force: true, count: 3 }));
  });

  it('writes only granted columns when most optional fields are null', async () => {
    const client = new FakePgClient([{ rows: [], rowCount: 1 }]);
    const writer = new PgIdentityAuditWriter();
    await writer.append(client, {
      action: 'password_reset',
      subjectUserId: null,
      actorUserId: null,
    });
    const call = client.calls[0]!;
    expect(call.params[2]).toBeNull();
    expect(call.params[3]).toBeNull();
    expect(call.params[4]).toBeNull();
    expect(call.params[5]).toBeNull();
    expect(call.params[6]).toBeNull();
    expect(call.params[7]).toBeNull();
    expect(call.params[8]).toBeNull();
    expect(call.params[9]).toBe('{}');
  });

  it.each([
    'password',
    'passwordHash',
    'password_hash',
    'hashOfSecret',
    'sessionToken',
    'cookieSecret',
    'saltBytes',
    'publicKey',
    'privateKey',
  ])('rejects metadata keys that look like secrets: "%s"', async (key) => {
    const client = new FakePgClient([]);
    const writer = new PgIdentityAuditWriter();
    await expect(
      writer.append(client, {
        action: 'profile_updated',
        subjectUserId: null,
        actorUserId: null,
        metadata: { [key]: 'never-store-this' },
      }),
    ).rejects.toThrow(new RegExp(`metadata key "${key}" is forbidden`));
  });

  it('rejects non-scalar metadata values', async () => {
    const client = new FakePgClient([]);
    const writer = new PgIdentityAuditWriter();
    await expect(
      writer.append(client, {
        action: 'profile_updated',
        subjectUserId: null,
        actorUserId: null,
        // @ts-expect-error: intentionally wrong type
        metadata: { payload: { nested: 'object' } },
      }),
    ).rejects.toThrow(/metadata value for "payload" must be a scalar or null/);
  });

  it('rejects an undefined event', async () => {
    const client = new FakePgClient([]);
    const writer = new PgIdentityAuditWriter();
    // @ts-expect-error: intentionally wrong type
    await expect(writer.append(client, undefined)).rejects.toThrow(/event is required/);
  });

  it('rejects an event without action', async () => {
    const client = new FakePgClient([]);
    const writer = new PgIdentityAuditWriter();
    await expect(
      writer.append(client, {
        // @ts-expect-error: intentionally wrong type
        action: '',
        subjectUserId: null,
        actorUserId: null,
      }),
    ).rejects.toThrow(/action is required/);
  });
});

describe('PgLegacyPasswordWindow', () => {
  it('is open when now < legacy_password_deadline', async () => {
    const futureDeadline = new Date('2099-01-01T00:00:00Z');
    const client = new FakePgClient([
      { rows: [{ legacy_password_deadline: futureDeadline }], rowCount: 1 },
    ]);
    const window = new PgLegacyPasswordWindow();
    const result = await window.isOpen(client, new Date('2026-01-01T00:00:00Z'));
    expect(result).toBe(true);
    expect(client.calls[0]?.sql).toMatch(/SELECT legacy_password_deadline/i);
    expect(client.calls[0]?.sql).toMatch(/FROM public\.lu_identity_migration_state/i);
    expect(client.calls[0]?.sql).toMatch(/WHERE id = 1/);
  });

  it('is closed when now == deadline (exclusive)', async () => {
    const exact = new Date('2026-06-01T00:00:00Z');
    const client = new FakePgClient([{ rows: [{ legacy_password_deadline: exact }], rowCount: 1 }]);
    const window = new PgLegacyPasswordWindow();
    const result = await window.isOpen(client, exact);
    expect(result).toBe(false);
  });

  it('is closed when now > deadline', async () => {
    const pastDeadline = new Date('2024-01-01T00:00:00Z');
    const client = new FakePgClient([
      { rows: [{ legacy_password_deadline: pastDeadline }], rowCount: 1 },
    ]);
    const window = new PgLegacyPasswordWindow();
    const result = await window.isOpen(client, new Date('2026-01-01T00:00:00Z'));
    expect(result).toBe(false);
  });

  it('applies no deadline refusal when the migration_state row is missing', async () => {
    const client = new FakePgClient([{ rows: [], rowCount: 0 }]);
    const window = new PgLegacyPasswordWindow();
    const result = await window.isOpen(client, new Date('2026-01-01T00:00:00Z'));
    expect(result).toBe(true);
  });

  it('applies no deadline refusal when legacy_password_deadline is null (cutover not recorded)', async () => {
    const client = new FakePgClient([{ rows: [{ legacy_password_deadline: null }], rowCount: 1 }]);
    const window = new PgLegacyPasswordWindow();
    const result = await window.isOpen(client, new Date('2026-01-01T00:00:00Z'));
    expect(result).toBe(true);
  });

  it('rejects an invalid now argument', async () => {
    const client = new FakePgClient([]);
    const window = new PgLegacyPasswordWindow();
    await expect(window.isOpen(client, new Date('invalid'))).rejects.toThrow(/valid Date/);
  });
});
