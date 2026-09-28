/**
 * ASP.NET Identity V2/V3 password verifier adapter.
 *
 * - `verify` runs PBKDF2 (HMAC-SHA1/SHA256/SHA512 per the parsed header)
 *   with the stored salt and iteration count; plaintext is treated as
 *   opaque UTF-8 bytes (no trim, no normalization).
 * - `crypto.pbkdf2` is the ASYNCHRONOUS Node primitive; the request path
 *   never calls the synchronous variant.
 * - `crypto.timingSafeEqual` is used on equal-length buffers; length
 *   mismatches fail closed without timing leakage.
 * - Any malformed input (parse failure, marker/scheme mismatch) returns
 *   `{ verified: false, needsRehash: false }` without throwing.
 * - A successful verify against a legacy scheme always sets
 *   `needsRehash: true` (F1 §7: the verified legacy credential becomes
 *   bcrypt after proof).
 *
 * The scheme passed to the constructor (`'legacy_identity_v2'` or
 * `'legacy_identity_v3'`) MUST match the marker in the stored hash.
 * A mismatch fails closed; this protects against a V2 hash being
 * submitted to the V3 verifier (or vice versa).
 */
import { pbkdf2 as pbkdf2Async, timingSafeEqual } from 'node:crypto';
import { promisify } from 'node:util';
import type { PasswordVerifier, PasswordVerificationResult } from '../identity.contracts.js';
import {
  parseAspNetIdentityHash,
  ASPNET_IDENTITY_V2_MARKER,
  type ParsedAspNetIdentityHash,
} from './aspnet-identity-hash.js';

const pbkdf2 = promisify(pbkdf2Async);

export type AspNetIdentityScheme = 'legacy_identity_v2' | 'legacy_identity_v3';

export class AspNetIdentityPasswordVerifier implements PasswordVerifier {
  readonly scheme: AspNetIdentityScheme;

  constructor(scheme: AspNetIdentityScheme) {
    this.scheme = scheme;
  }

  async verify(plaintext: string, storedHash: string): Promise<PasswordVerificationResult> {
    const parsed = safeParse(storedHash);
    if (parsed === null) {
      return { verified: false, needsRehash: false };
    }
    if (!schemeMatchesMarker(this.scheme, parsed)) {
      return { verified: false, needsRehash: false };
    }
    const passwordBytes = safePasswordBytes(plaintext);
    if (passwordBytes === null) {
      return { verified: false, needsRehash: false };
    }
    let derived: Buffer;
    try {
      derived = await pbkdf2(
        passwordBytes,
        parsed.salt,
        parsed.iterations,
        parsed.subkey.length,
        parsed.prf,
      );
    } catch {
      return { verified: false, needsRehash: false };
    }
    if (derived.length !== parsed.subkey.length) {
      return { verified: false, needsRehash: false };
    }
    let ok: boolean;
    try {
      ok = timingSafeEqual(derived, parsed.subkey);
    } catch {
      return { verified: false, needsRehash: false };
    }
    if (!ok) {
      return { verified: false, needsRehash: false };
    }
    return { verified: true, needsRehash: true };
  }
}

function safeParse(storedHash: string): ParsedAspNetIdentityHash | null {
  try {
    return parseAspNetIdentityHash(storedHash);
  } catch {
    return null;
  }
}

function safePasswordBytes(plaintext: string): Buffer | null {
  if (typeof plaintext !== 'string') return null;
  // Plaintext is treated as opaque UTF-8 bytes; no trim, no normalization.
  return Buffer.from(plaintext, 'utf8');
}

function schemeMatchesMarker(
  scheme: AspNetIdentityScheme,
  parsed: ParsedAspNetIdentityHash,
): boolean {
  const expectedMarker = scheme === 'legacy_identity_v2' ? ASPNET_IDENTITY_V2_MARKER : 0x01;
  // Marker byte 0x01 is also the V3 marker; the parse layer rejects any
  // non-V2/V3 byte. Cross-marker defense in depth: a V3 payload submitted
  // to the V2 verifier fails here (and vice versa) without performing any
  // cryptographic work.
  return (
    (expectedMarker === ASPNET_IDENTITY_V2_MARKER && parsed.version === 2) ||
    (expectedMarker === 0x01 && parsed.version === 3)
  );
}
