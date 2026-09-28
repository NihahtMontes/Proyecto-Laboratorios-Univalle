import { readFileSync } from 'node:fs';
import { fileURLToPath } from 'node:url';
import { canonicalByteLength, sha256Hex } from '../src/database/migration-plan.js';

const ROLE_SCRIPT_PATH = fileURLToPath(
  new URL('../database/roles/002_grant_auth_security_controls.sql', import.meta.url),
);
const ROLE_SCRIPT_BYTES = readFileSync(ROLE_SCRIPT_PATH);
const ROLE_SCRIPT = ROLE_SCRIPT_BYTES.toString('utf8');
const ROLE_SCRIPT_CODE = ROLE_SCRIPT.replace(/--.*$/gm, '');

// Pinned on the canonical LF bytes (CRLF → LF only; BOM and lone CR rejected).
// These constants match the LF bytes committed to HEAD; on Windows checkouts
// with core.autocrlf=true the on-disk bytes include the CR pre-byte (and are
// therefore larger), but the digest and length below are computed from the
// canonical form via sha256Hex / canonicalByteLength.
const PINNED_LF_SHA256 = '24934ed1ff17a1aec4e28f98f931286935065674f26763d87baf56d452e8b4a2';
const PINNED_LF_LENGTH = 6284;

describe('auth security-control runtime ACL script', () => {
  it('pins the canonical LF bytes by digest and length', () => {
    expect(sha256Hex(ROLE_SCRIPT_BYTES)).toBe(PINNED_LF_SHA256);
    expect(canonicalByteLength(ROLE_SCRIPT_BYTES)).toBe(PINNED_LF_LENGTH);
  });

  it('accepts LF bytes, CRLF bytes, and CRLF→LF converted bytes as identical', () => {
    // LF bytes
    const lfBytes = Buffer.from(ROLE_SCRIPT.replace(/\r\n/g, '\n'), 'utf8');
    expect(sha256Hex(lfBytes)).toBe(PINNED_LF_SHA256);
    expect(canonicalByteLength(lfBytes)).toBe(PINNED_LF_LENGTH);

    // CRLF bytes (the on-disk Windows checkout form): convert LF to CRLF.
    // Build from LF, never from a string that already carries CRLF.
    const crlfBytes = Buffer.from(lfBytes.toString('utf8').replace(/\n/g, '\r\n'), 'utf8');
    expect(sha256Hex(crlfBytes)).toBe(PINNED_LF_SHA256);
    expect(canonicalByteLength(crlfBytes)).toBe(PINNED_LF_LENGTH);

    // CRLF bytes converted to LF (canonical normalization roundtrip)
    const converted = Buffer.from(
      ROLE_SCRIPT_BYTES.toString('utf8').replace(/\r\n/g, '\n'),
      'utf8',
    );
    expect(sha256Hex(converted)).toBe(PINNED_LF_SHA256);
    expect(canonicalByteLength(converted)).toBe(PINNED_LF_LENGTH);
  });

  it('rejects a one-byte content change', () => {
    const mutated = Buffer.concat([ROLE_SCRIPT_BYTES, Buffer.from(' ', 'utf8')]);
    expect(sha256Hex(mutated)).not.toBe(PINNED_LF_SHA256);
    expect(canonicalByteLength(mutated)).not.toBe(PINNED_LF_LENGTH);
  });

  it('rejects bytes containing a lone CR (no following LF)', () => {
    const loneCr = Buffer.concat([
      ROLE_SCRIPT_BYTES,
      Buffer.from([0x0d, 0x0a]),
      Buffer.from([0x0d]),
    ]);
    expect(sha256Hex(loneCr)).not.toBe(PINNED_LF_SHA256);
  });

  it('rejects bytes starting with a UTF-8 BOM', () => {
    const bomBytes = Buffer.concat([Buffer.from([0xef, 0xbb, 0xbf]), ROLE_SCRIPT_BYTES]);
    expect(sha256Hex(bomBytes)).not.toBe(PINNED_LF_SHA256);
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
