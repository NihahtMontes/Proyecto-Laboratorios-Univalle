import { createHash } from 'node:crypto';
import { readFileSync } from 'node:fs';
import { fileURLToPath } from 'node:url';

const ROLE_SCRIPT_PATH = fileURLToPath(
  new URL('../database/roles/002_grant_auth_security_controls.sql', import.meta.url),
);
const ROLE_SCRIPT_BYTES = readFileSync(ROLE_SCRIPT_PATH);
const ROLE_SCRIPT = ROLE_SCRIPT_BYTES.toString('utf8');
const ROLE_SCRIPT_CODE = ROLE_SCRIPT.replace(/--.*$/gm, '');

describe('auth security-control runtime ACL script', () => {
  it('pins the reviewed script bytes', () => {
    expect(ROLE_SCRIPT_BYTES).toHaveLength(6284);
    expect(createHash('sha256').update(ROLE_SCRIPT_BYTES).digest('hex')).toBe(
      '24934ed1ff17a1aec4e28f98f931286935065674f26763d87baf56d452e8b4a2',
    );
  });

  it('grants mutable buckets but append-only audit without audit reads', () => {
    expect(ROLE_SCRIPT).toContain(
      'GRANT SELECT, DELETE ON TABLE public.lu_auth_rate_limit TO lu_auth_runtime',
    );
    expect(ROLE_SCRIPT).toMatch(
      /GRANT INSERT\s+\(id, event_type, user_id, site_id, subject_hash, ip_hash, occurred_at, metadata\)\s+ON public\.lu_security_event TO lu_auth_runtime/,
    );
    expect(ROLE_SCRIPT).not.toMatch(
      /GRANT\s+(?:[^;]*\bSELECT\b[^;]*)\s+ON(?:\s+TABLE)?\s+public\.lu_security_event\s+TO\s+lu_auth_runtime/i,
    );
    expect(ROLE_SCRIPT).toContain('REVOKE UPDATE, DELETE, TRUNCATE, REFERENCES, TRIGGER, MAINTAIN');
  });

  it('contains no credential or principal mutation', () => {
    expect(ROLE_SCRIPT_CODE).not.toMatch(/\bPASSWORD\b/i);
    expect(ROLE_SCRIPT_CODE).not.toMatch(/\bALTER\s+ROLE\b/i);
    expect(ROLE_SCRIPT_CODE).not.toMatch(/\bCREATE\s+ROLE\b/i);
    expect(ROLE_SCRIPT_CODE).not.toMatch(/\bDROP\b/i);
  });
});
