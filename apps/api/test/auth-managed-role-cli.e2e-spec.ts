import { createHash } from 'node:crypto';
import { readFileSync } from 'node:fs';
import {
  loadManagedRoleConfig,
  roleFilePath,
  verifyRoleFile,
} from '../src/auth/auth-managed-role-cli.js';

const ADMIN_DSN =
  'Host=managed.example.test;Port=5432;Database=neondb;Username=neondb_owner;Password=secret;SSL Mode=Prefer';

describe('managed Auth role CLI', () => {
  it('accepts only the explicitly approved database and admin identity', () => {
    const config = loadManagedRoleConfig(
      {
        ConnectionStrings__DefaultConnection: ADMIN_DSN,
        AUTH_MANAGED_TARGET_DATABASE: 'neondb',
        AUTH_MANAGED_ADMIN_USER: 'neondb_owner',
      },
      false,
    );

    expect(config.database.database).toBe('neondb');
    expect(config.database.user).toBe('neondb_owner');
    expect(config.runtimePassword).toBeNull();
  });

  it('rejects a target identity mismatch', () => {
    expect(() =>
      loadManagedRoleConfig(
        {
          ConnectionStrings__DefaultConnection: ADMIN_DSN,
          AUTH_MANAGED_TARGET_DATABASE: 'production',
          AUTH_MANAGED_ADMIN_USER: 'neondb_owner',
        },
        false,
      ),
    ).toThrow('does not match');
  });

  it.each(['short', 'x'.repeat(257), `x${'a'.repeat(31)}\n`])(
    'rejects an invalid runtime password without echoing it',
    (password) => {
      expect(() =>
        loadManagedRoleConfig(
          {
            ConnectionStrings__DefaultConnection: ADMIN_DSN,
            AUTH_MANAGED_TARGET_DATABASE: 'neondb',
            AUTH_MANAGED_ADMIN_USER: 'neondb_owner',
            AUTH_RUNTIME_PASSWORD: password,
          },
          true,
        ),
      ).toThrow(/AUTH_RUNTIME_PASSWORD/);
    },
  );

  it('pins the managed role SQL by SHA-256', () => {
    const content = readFileSync(roleFilePath(process.cwd()));
    const expected = createHash('sha256').update(content).digest('hex');

    expect(verifyRoleFile(content)).toBe(expected);
    expect(() => verifyRoleFile(Buffer.concat([content, Buffer.from('\n')]))).toThrow(
      'hash mismatch',
    );
  });
});
