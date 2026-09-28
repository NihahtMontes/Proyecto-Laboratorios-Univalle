/**
 * Pure ASP.NET Identity V2/V3 hash parser adapter for the F3 runtime.
 *
 * Mirrors the F1 contract (`docs/migration/users/01-IDENTITY-CONTRACT.md`
 * §7 "Legacy compatibility boundary") and the F2 importer's accept set
 * (`classifyLegacyPasswordHash` in `apps/api/src/database/legacy-user-mapping.ts`).
 * No I/O, no DB access, no logging.
 *
 * Rules enforced on the stored opaque base64 string:
 *  - STRICT standard Base64 (RFC 4648 alphabet `+/` only): no whitespace,
 *    no URL-safe `-`/`_`, exact multiple-of-4 length, valid trailing
 *    padding (`=`/`==` or no padding) and a canonical roundtrip — the
 *    decoded bytes re-encoded with Node's standard `base64` encoder must
 *    equal the input string byte-for-byte.
 *  - Decoded length is capped at 141 bytes.
 *  - V2 is EXACTLY 49 bytes: marker `0x00` + 16-byte salt + 32-byte subkey,
 *    PBKDF2-HMAC-SHA1, 1000 iterations.
 *  - V3 is marker `0x01` + 12 header bytes (PRF/iterations/saltLength
 *    unsigned 32-bit big-endian) + salt + subkey; PRF `0/1/2` only;
 *    iterations `1000..1_000_000`; salt length `16..64`; subkey length
 *    `16..64`; remaining length must equal salt+subkey exactly.
 *
 * The parser NEVER mutates the input string (no trim); it fails closed
 * by returning `null` on every violation.
 *
 * The runtime accept set (this module) MUST equal the importer accept
 * set (`classifyLegacyPasswordHash`). A test in
 * `apps/api/test/identity.password.e2e-spec.ts` proves that agreement
 * over the shared corpus.
 */

export type AspNetPrf = 'sha1' | 'sha256' | 'sha512';

export interface ParsedAspNetIdentityHash {
  readonly version: 2 | 3;
  readonly prf: AspNetPrf;
  readonly iterations: number;
  readonly salt: Buffer;
  readonly subkey: Buffer;
}

/** Marker byte for ASP.NET Identity V2 (PBKDF2-HMAC-SHA1, 1000 iterations). */
export const ASPNET_IDENTITY_V2_MARKER = 0x00;
/** Marker byte for ASP.NET Identity V3 (PRF-iter-saltlen header). */
export const ASPNET_IDENTITY_V3_MARKER = 0x01;

/** Maximum decoded bytes accepted from a legacy Identity base64 string. */
export const LEGACY_HASH_MAX_DECODED_BYTES = 141;

/** V2 is marker + 16-byte salt + 32-byte subkey. */
export const ASPNET_IDENTITY_V2_LENGTH = 49;
/** V2 PBKDF2-HMAC-SHA1 iteration count. */
export const ASPNET_IDENTITY_V2_ITERATIONS = 1000;

/** V3 minimum accepted iteration count (also V2 floor; matches F1 §7). */
export const ASPNET_IDENTITY_ITERATIONS_MIN = 1000;
/** V3 maximum accepted iteration count (matches F1 §7). */
export const ASPNET_IDENTITY_ITERATIONS_MAX = 1_000_000;
/** Minimum salt / subkey length in bytes. */
export const ASPNET_IDENTITY_SALT_SUBKEY_MIN = 16;
/** Maximum salt / subkey length in bytes. */
export const ASPNET_IDENTITY_SALT_SUBKEY_MAX = 64;

/**
 * Standard base64 alphabet (`A-Z`, `a-z`, `0-9`, `+`, `/`) and trailing
 * padding (`=`/`==`). Mirrors the F2 importer's strict decoder so the
 * runtime accept set equals the importer accept set.
 */
const STRICT_STD_BASE64_RE = /^(?:[A-Za-z0-9+/]{4})*(?:[A-Za-z0-9+/]{2}==|[A-Za-z0-9+/]{3}=)?$/;

function isStdBase64Char(ch: string): boolean {
  if (ch.length !== 1) return false;
  const code = ch.charCodeAt(0);
  return (
    (code >= 0x41 && code <= 0x5a) ||
    (code >= 0x61 && code <= 0x7a) ||
    (code >= 0x30 && code <= 0x39) ||
    ch === '+' ||
    ch === '/'
  );
}

/**
 * Decode a standard-base64 string with strict canonical roundtrip. The
 * input is NEVER mutated: any whitespace, URL-safe characters, missing
 * or excess padding, or non-canonical trailing bits cause a `null`
 * return. The decoded length is capped at `LEGACY_HASH_MAX_DECODED_BYTES`
 * (141 bytes per F1 §7).
 */
function decodeStrictStandardBase64(
  raw: string,
): { readonly bytes: Uint8Array; readonly bytesLength: number } | null {
  if (typeof raw !== 'string') return null;
  if (raw.length === 0) return null;
  // Opaque: no trim. Reject any whitespace anywhere up-front.
  if (/\s/.test(raw)) return null;
  // Length must be an exact multiple of 4.
  if (raw.length % 4 !== 0) return null;
  // Standard alphabet only; URL-safe `-`/`_` are rejected by the explicit
  // per-character check below before the structural regex fires.
  for (let i = 0; i < raw.length; i++) {
    if (!isStdBase64Char(raw.charAt(i)) && raw.charAt(i) !== '=') {
      return null;
    }
  }
  if (!STRICT_STD_BASE64_RE.test(raw)) return null;
  const buf = Buffer.from(raw, 'base64');
  if (buf.length === 0) return null;
  if (buf.length > LEGACY_HASH_MAX_DECODED_BYTES) return null;
  // Canonical roundtrip: re-encoding the decoded bytes through Node's
  // standard encoder must equal the input string byte-for-byte.
  if (buf.toString('base64') !== raw) return null;
  return { bytes: new Uint8Array(buf), bytesLength: buf.length };
}

function prfFor(byte: number): AspNetPrf | null {
  if (byte === 0) return 'sha1';
  if (byte === 1) return 'sha256';
  if (byte === 2) return 'sha512';
  return null;
}

function readUint32BigEndian(buf: Uint8Array, offset: number): number {
  return (
    (((buf[offset] ?? 0) << 24) |
      ((buf[offset + 1] ?? 0) << 16) |
      ((buf[offset + 2] ?? 0) << 8) |
      (buf[offset + 3] ?? 0)) >>>
    0
  );
}

/**
 * Parse a stored ASP.NET Identity V2/V3 hash into a structured payload
 * the runtime can verify. Returns `null` for every malformed, out-of-
 * bound, non-canonical, or unsupported input. The plaintext password
 * is NEVER logged or echoed. The input string is NEVER mutated (no
 * trim); the caller may treat the stored value as opaque.
 *
 * The accept set MUST equal `classifyLegacyPasswordHash` from
 * `apps/api/src/database/legacy-user-mapping.ts`; a regression test
 * in `apps/api/test/identity.password.e2e-spec.ts` asserts agreement.
 */
export function parseAspNetIdentityHash(stored: string): ParsedAspNetIdentityHash | null {
  if (typeof stored !== 'string') return null;
  const decoded = decodeStrictStandardBase64(stored);
  if (decoded === null) return null;
  const { bytes, bytesLength } = decoded;
  const marker = bytes[0];
  if (marker === ASPNET_IDENTITY_V2_MARKER) {
    if (bytesLength !== ASPNET_IDENTITY_V2_LENGTH) return null;
    const salt = Buffer.from(bytes.subarray(1, 1 + 16));
    const subkey = Buffer.from(bytes.subarray(1 + 16, 1 + 16 + 32));
    return {
      version: 2,
      prf: 'sha1',
      iterations: ASPNET_IDENTITY_V2_ITERATIONS,
      salt,
      subkey,
    };
  }
  if (marker === ASPNET_IDENTITY_V3_MARKER) {
    const HEADER_LEN = 1 + 12;
    if (bytesLength < HEADER_LEN + 16 + 16) return null;
    const prfByte = readUint32BigEndian(bytes, 1);
    const prf = prfFor(prfByte);
    if (prf === null) return null;
    const iterations = readUint32BigEndian(bytes, 5);
    if (
      iterations < ASPNET_IDENTITY_ITERATIONS_MIN ||
      iterations > ASPNET_IDENTITY_ITERATIONS_MAX
    ) {
      return null;
    }
    const saltLength = readUint32BigEndian(bytes, 9);
    if (
      saltLength < ASPNET_IDENTITY_SALT_SUBKEY_MIN ||
      saltLength > ASPNET_IDENTITY_SALT_SUBKEY_MAX
    ) {
      return null;
    }
    const subkeyLength = bytesLength - (HEADER_LEN + saltLength);
    if (
      subkeyLength < ASPNET_IDENTITY_SALT_SUBKEY_MIN ||
      subkeyLength > ASPNET_IDENTITY_SALT_SUBKEY_MAX
    ) {
      return null;
    }
    const salt = Buffer.from(bytes.subarray(HEADER_LEN, HEADER_LEN + saltLength));
    const subkey = Buffer.from(
      bytes.subarray(HEADER_LEN + saltLength, HEADER_LEN + saltLength + subkeyLength),
    );
    return { version: 3, prf, iterations, salt, subkey };
  }
  return null;
}
