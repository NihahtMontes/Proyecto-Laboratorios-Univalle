/**
 * Pure/testable legacy-Identity mapping contract for MIG-001 F2-W7.
 *
 * Contract (frozen F1 DATA-MAPPING, no silent inference):
 *  - `mapLegacyUser` is all-or-blocked: a complete, valid legacy SQL Server
 *    row maps to `reconciliation: 'canonical'`. There is no
 *    `pending_reconciliation` disposition in this source mapper. The
 *    pre-F2 PostgreSQL `reconciliation_state='pending_reconciliation'`
 *    backfill on already-loaded target rows is an unrelated concern that
 *    lives in the SQL additive migration (control-plane 0006) and never
 *    appears in the source row contract.
 *  - `callerAllocatedUserId` is validated strictly as RFC4122 UUID-shaped
 *    and returned verbatim in canonical lowercase form (the F1 crosswalk
 *    records a UUID). The mapper never generates a UUID itself; the
 *    caller must pre-allocate it.
 *  - Required normalized fields (`username`, `email`, `firstName`,
 *    `lastName`, `identityCard`, `phoneNumber`) must all be non-empty and
 *    valid after canonical normalization. Any violation throws a typed,
 *    non-secret `LegacyMappingError` whose message references the field
 *    name only; no raw PII (username, email, name, CI, phone) and no
 *    opaque hash is ever placed in error messages. There are no
 *    placeholders such as `***` or `<value>`.
 *  - Username / email use `trim -> NFKC -> locale-independent
 *    lowercase` (TypeScript `toLowerCase`). Identity card uses
 *    `trim -> NFKC -> uppercase` with regex `[0-9A-Z-]{1,10}`; the
 *    internal hyphen is preserved. Names use `trim -> NFC -> collapse
 *    whitespace`. Phone is `trim` only, with a hard upper bound of 30
 *    characters enforced after trim.
 *  - Role: legacy enum and exactly one managed Identity role must agree:
 *      Supervisor(2) / Supervisor   -> no global + pilot Supervisor
 *      Administrador(1) / Administrator -> no global + pilot Administrador
 *      SuperAdmin(99) / SuperAdmin  -> global SuperAdmin + pilot Administrador
 *    Missing, unknown or contradictory managed roles block (typed
 *    `INVALID_ROLE` / `ROLE_MISMATCH`). Because the current input shape
 *    carries only one managed role string, the mapper cannot itself
 *    detect a multiplicity greater than one. Callers are responsible for
 *    pre-checking that exactly one managed role exists, or for extending
 *    the input to carry an explicit count/list field; this mapper does
 *    not expand that scope here. A missing managed role is therefore
 *    reported as a blocked mapping.
 *  - Status: legacy `0 / 1 / 2` map to
 *      active / active, inactive / suspended, deleted / revoked.
 *    Unknown status values throw `INVALID_STATUS`.
 *  - Password: valid bounded Identity V2 / V3 and target bcrypt classify
 *    explicitly and set `mustChangePassword=false`. Malformed, unknown,
 *    blank, leading / trailing / internal whitespace hashes classify as
 *    `reset_required` with `passwordClassification.scheme = 'reset_required'`,
 *    `mustChangePassword=true`, the opaque target hash is null and
 *    `passwordMigratedAt=null`. The hash is OPAQUE: `classifyPasswordScheme`
 *    and the V2/V3/bcrypt helpers NEVER trim or otherwise mutate the input
 *    before classification; a string that is exactly canonical (no
 *    surrounding whitespace, no internal whitespace) is the ONLY shape that
 *    survives V2/V3/bcrypt classification. Whitespace anywhere in the
 *    value (including a single blank space) is a `reset_required`
 *    classification. `TwoFactorEnabled=true` always forces
 *    `reset_required` + `mustChangePassword=true` regardless of the
 *    underlying valid scheme. The opaque hash itself is only kept on
 *    the caller-supplied input; it never appears in the mapping result,
 *    in error messages, in logs or in any future verification path
 *    performed by this mapper (it never verifies hashes).
 *  - `legacyUserId` must be a nonnegative integer. Date strings must be
 *    strict valid UTC instant / YYYY-MM-DD date shapes or be rejected
 *    (typed-blocked) rather than silently sliced. Actor IDs are nullable;
 *    non-null actor IDs must be nonnegative integers. Resolution through
 *    the legacy crosswalk remains the caller / data-run responsibility;
 *    an unresolved non-null actor blocks later in the import pipeline
 *    and is intentionally out of scope here.
 *  - `fullName` is derived from normalized split names. Initial site
 *    role / status / work fields (position, department, hire date) are
 *    carried through. Crosswalk provenance inputs (source system,
 *    legacy user id) and the caller-allocated UUID are preserved
 *    verbatim so the F2 crosswalk row can be written by the caller
 *    without re-derivation.
 *
 *  - F1/W14A frozen validation strengthens the F1 contract without
 *    altering mapping decisions:
 *      * Standard Base64 decode rejects lengths that are not multiples
 *        of 4, missing or excess `=` padding, internal whitespace,
 *        URL-safe `-`/`_` alphabet, and noncanonical trailing bits.
 *        The decoded length remains capped at 141 bytes and the exact
 *        V2/V3 binary shape validation is unchanged.
 *      * Bcrypt shape is restricted to `$2a/$2b/$2y`, cost `04..31`,
 *        and exactly 53 payload chars from `[A-Za-z0-9./]`. Malformed
 *        or unsupported hashes classify as `reset_required`.
 *      * After `NFKC/NFC/trim` the normalized identifier / name / work
 *        fields enforce PostgreSQL-aligned character (code-point) bounds
 *        using the same `length()` semantics as PG `text`/`varchar`
 *        columns: usernames and emails <=256 code points; required and
 *        optional names <=100 code points; optional
 *        position/department <=100 code points. Required fields remain
 *        nonblank.
 *      * Email syntax enforces exactly one `@`, no whitespace or control
 *        characters, non-empty local and domain, and a dot-qualified
 *        domain with a non-empty TLD. The normalized canonical value is
 *        preserved verbatim; this validator deliberately performs no
 *        reconciliation (splitting, aliasing, IDN handling) and never
 *        invents a substitution.
 *      * Phone syntax is mirrored from the F1/W14B PostgreSQL
 *        `ck_lu_user_phone_number_format` CHECK: at most 30 characters,
 *        one optional leading `+`, charset `[0-9 ().-]`, balanced
 *        parentheses, and 7..15 total ASCII digits. Controls, letters,
 *        extensions, double plus and unbalanced parentheses are
 *        rejected. The source formatting (separators, leading `+`,
 *        internal spaces) is preserved verbatim.
 *      * Every optional normalized field (secondLastName, position,
 *        department) is revalidated with its nonblank + bound check
 *        before the mapping is returned, and every typed error uses the
 *        existing `INVALID_IDENTIFIER` / `INVALID_NAME` /
 *        `INVALID_PHONE` / `INVALID_INPUT` codes. No PII or opaque hash
 *        is ever echoed in an error message.
 */

export const SOURCE_SYSTEM_ASP_SQLSERVER = 'asp_sqlserver';

export const MARKER_IDENTITY_V2 = 0x00;
export const MARKER_IDENTITY_V3 = 0x01;
export const PRF_HMAC_SHA1 = 0;
export const PRF_HMAC_SHA256 = 1;
export const PRF_HMAC_SHA512 = 2;

export const PASSWORD_SCHEME_LEGACY_V2 = 'legacy_identity_v2';
export const PASSWORD_SCHEME_LEGACY_V3 = 'legacy_identity_v3';
export const PASSWORD_SCHEME_BCRYPT = 'bcrypt';
export const PASSWORD_SCHEME_RESET_REQUIRED = 'reset_required';

export type LegacyPasswordScheme =
  | typeof PASSWORD_SCHEME_LEGACY_V2
  | typeof PASSWORD_SCHEME_LEGACY_V3
  | typeof PASSWORD_SCHEME_BCRYPT
  | typeof PASSWORD_SCHEME_RESET_REQUIRED;

export interface LegacyPasswordClassification {
  readonly scheme: LegacyPasswordScheme;
  readonly reason: string;
  readonly v3Metadata?: {
    readonly prf: number;
    readonly iterations: number;
    readonly saltLength: number;
    readonly subkeyLength: number;
  };
}

/** Typed error raised when input is unparseable or contradicts itself. */
export class LegacyMappingError extends Error {
  readonly code:
    | 'INVALID_LEGACY_USER_ID'
    | 'INVALID_STATUS'
    | 'INVALID_ROLE'
    | 'ROLE_MISMATCH'
    | 'INVALID_IDENTIFIER'
    | 'INVALID_NAME'
    | 'INVALID_PHONE'
    | 'INVALID_DATE'
    | 'INVALID_ACTOR'
    | 'INVALID_INPUT';
  constructor(code: LegacyMappingError['code'], message: string) {
    super(`${code}: ${message}`);
    this.name = 'LegacyMappingError';
    this.code = code;
  }
}

// ---------------------------------------------------------------------------
// Standard Base64 (RFC 4648, alphabet + padding) — F1/W14A strict.
// ---------------------------------------------------------------------------

/** Standard alphabet `A-Z a-z 0-9 + /` (no URL-safe). */
const B64_STD_ALPHABET = 'ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789+/';

/** Maximum decoded bytes accepted from a legacy Identity base64 string. */
export const LEGACY_BASE64_MAX_DECODED_BYTES = 141;

/**
 * Look up the 6-bit value of a base64 character in the STANDARD alphabet
 * (returns `-1` for any non-canonical character, including URL-safe `-`
 * and `_`, internal whitespace, controls and any non-alphabet glyph).
 */
function b64StdCharValue(ch: string): number {
  if (ch.length !== 1) return -1;
  const idx = B64_STD_ALPHABET.indexOf(ch);
  return idx;
}

/**
 * Strictly canonical STANDARD base64 decoder for ASP.NET Identity
 * hashes. The hash is OPAQUE: the decoder NEVER trims or otherwise
 * mutates the input. A value that is not exactly the canonical shape
 * (no leading / trailing / internal whitespace, no URL-safe alphabet,
 * exact multiple-of-4 length, valid padding) returns `null` so the
 * pipeline classifies it as `reset_required`.
 *
 * Rejected as `null` (i.e. `reset_required` later in the pipeline):
 *  - non-string / null / undefined / empty string;
 *  - any leading / trailing / internal whitespace character
 *    (the regex anchors explicitly to the alphabet and trailing `=`
 *    padding; whitespace is not in the alphabet);
 *  - URL-safe alphabet characters (`-` / `_`) or any glyph outside the
 *    STANDARD base64 alphabet;
 *  - a length that is not an exact multiple of 4;
 *  - missing padding (e.g. `YB` — Node would silently pad and decode it,
 *    but we re-encode and reject via the canonical roundtrip);
 *  - excess padding (`====`, `===`, mid-string `=`);
 *  - noncanonical trailing bits (the last data character must encode
 *    zero-valued low bits to be canonical; e.g. `AB==` is rejected in
 *    favour of `AA==`).
 *
 * The decoded byte count remains capped at 141 bytes per the F1
 * contract; exact V2/V3 binary shape validation is unchanged.
 */
export function decodeLegacyIdentityBase64(
  raw: string | null | undefined,
): { readonly bytes: Uint8Array; readonly bytesLength: number } | null {
  if (typeof raw !== 'string') return null;
  // Opaque hash: no trim. A pure-whitespace string (or any string with
  // a leading / trailing / internal whitespace character) is rejected
  // up-front because the rest of the contract reads the value as-is.
  if (raw.length === 0) return null;
  if (/\s/.test(raw)) return null;
  // Length must be an exact multiple of 4 (canonical base64 chars are
  // grouped in fours). This rejects both missing and excess padding in
  // the common cases without relying on the lenient Node decoder.
  if (raw.length % 4 !== 0) return null;
  // STANDARD alphabet only: no URL-safe (`-` / `_`), no whitespace,
  // no controls, and `=` only allowed as trailing padding (1 or 2).
  if (!/^[A-Za-z0-9+/]+={1,2}$/.test(raw) && !/^[A-Za-z0-9+/]+$/.test(raw)) {
    return null;
  }
  const padCount = raw.endsWith('==') ? 2 : raw.endsWith('=') ? 1 : 0;
  // Noncanonical trailing bits: the last data character must have its
  // trailing `padCount * 2` bits clear (canonical padding).
  if (padCount > 0) {
    const lastDataChar = raw.charAt(raw.length - 1 - padCount);
    const val = b64StdCharValue(lastDataChar);
    if (val < 0) return null;
    const unusedBits = padCount === 2 ? 4 : 2;
    const mask = (1 << unusedBits) - 1;
    if ((val & mask) !== 0) return null;
  }
  const buffer = Buffer.from(raw, 'base64');
  if (buffer.length === 0) return null;
  if (buffer.length > LEGACY_BASE64_MAX_DECODED_BYTES) return null;
  // Canonical roundtrip: re-encoding the decoded bytes through Node's
  // canonical standard encoder must produce the same opaque string.
  // This catches any combination of lenient-decoding that survives the
  // structural checks above (e.g. `YB` re-encoded as `YB==`, or a
  // noncanonical trailing-bit tweak).
  const reencoded = buffer.toString('base64');
  if (reencoded !== raw) return null;
  return { bytes: new Uint8Array(buffer), bytesLength: buffer.length };
}

export function classifyLegacyPasswordHash(
  raw: string | null | undefined,
): LegacyPasswordClassification {
  const decoded = decodeLegacyIdentityBase64(raw);
  if (decoded === null) {
    return {
      scheme: PASSWORD_SCHEME_RESET_REQUIRED,
      reason: 'base64 decode failed or not canonical',
    };
  }
  const marker = decoded.bytes[0];
  if (marker === MARKER_IDENTITY_V2) {
    if (decoded.bytesLength !== 49) {
      return { scheme: PASSWORD_SCHEME_RESET_REQUIRED, reason: 'V2 must be 49 bytes' };
    }
    return {
      scheme: PASSWORD_SCHEME_LEGACY_V2,
      reason: 'V2 marker 0x00 + 16-byte salt + 32-byte subkey',
    };
  }
  if (marker === MARKER_IDENTITY_V3) {
    if (decoded.bytesLength < 1 + 12 + 16 + 16) {
      return { scheme: PASSWORD_SCHEME_RESET_REQUIRED, reason: 'V3 header too short' };
    }
    const view = new DataView(
      decoded.bytes.buffer,
      decoded.bytes.byteOffset,
      decoded.bytes.byteLength,
    );
    const prf = view.getUint32(1, false);
    const iterations = view.getUint32(5, false);
    const saltLength = view.getUint32(9, false);
    if (prf !== PRF_HMAC_SHA1 && prf !== PRF_HMAC_SHA256 && prf !== PRF_HMAC_SHA512) {
      return { scheme: PASSWORD_SCHEME_RESET_REQUIRED, reason: `unsupported PRF ${prf}` };
    }
    if (iterations < 1_000 || iterations > 1_000_000) {
      return {
        scheme: PASSWORD_SCHEME_RESET_REQUIRED,
        reason: `iterations ${iterations} out of bounds`,
      };
    }
    if (saltLength < 16 || saltLength > 64) {
      return {
        scheme: PASSWORD_SCHEME_RESET_REQUIRED,
        reason: `salt length ${saltLength} out of bounds`,
      };
    }
    const subkeyLength = decoded.bytesLength - (1 + 12 + saltLength);
    if (subkeyLength < 16 || subkeyLength > 64) {
      return {
        scheme: PASSWORD_SCHEME_RESET_REQUIRED,
        reason: `subkey length ${subkeyLength} out of bounds`,
      };
    }
    return {
      scheme: PASSWORD_SCHEME_LEGACY_V3,
      reason: 'V3 marker 0x01 + valid PRF/iterations/salt/subkey lengths',
      v3Metadata: { prf, iterations, saltLength, subkeyLength },
    };
  }
  return {
    scheme: PASSWORD_SCHEME_RESET_REQUIRED,
    reason: `unrecognized marker byte 0x${marker?.toString(16) ?? '??'}`,
  };
}

/**
 * Bcrypt shape contract mirrored from `ck_lu_user_password_hash_format`
 * in 0006_mig001_users_identity.sql:
 *
 *   ^[$]2[aby][$](0[4-9]|[12][0-9]|3[01])[$][./A-Za-z0-9]{53}$
 *
 * The TypeScript mapper is the runtime authority and accepts the full
 * canonical bcrypt alphabet `[A-Za-z0-9./]` (uppercase included); the
 * SQL CHECK constrains its own copy to lowercase + `.` + `/` only.
 * Both sides reject cost < 04 and > 31, unknown version prefixes
 * (`$2x$`, `$2c$`, etc.), and a payload that is not exactly 53 chars.
 *
 * The hash is OPAQUE: the classifier NEVER trims. Leading, trailing or
 * internal whitespace (including a single blank space) is a
 * `reset_required` classification.
 */
const BCRYPT_SHAPE_RE = /^\$2[aby]\$(0[4-9]|[12][0-9]|3[01])\$[A-Za-z0-9./]{53}$/;

export function classifyBcryptHash(raw: string | null | undefined): LegacyPasswordClassification {
  if (typeof raw !== 'string') {
    return { scheme: PASSWORD_SCHEME_RESET_REQUIRED, reason: 'no bcrypt hash' };
  }
  if (raw.length === 0) {
    return { scheme: PASSWORD_SCHEME_RESET_REQUIRED, reason: 'bcrypt hash is blank' };
  }
  if (/\s/.test(raw)) {
    return { scheme: PASSWORD_SCHEME_RESET_REQUIRED, reason: 'bcrypt hash carries whitespace' };
  }
  if (!/^\$2[aby]\$/.test(raw)) {
    return { scheme: PASSWORD_SCHEME_RESET_REQUIRED, reason: 'bcrypt version prefix unsupported' };
  }
  if (!BCRYPT_SHAPE_RE.test(raw)) {
    return {
      scheme: PASSWORD_SCHEME_RESET_REQUIRED,
      reason: 'bcrypt shape invalid (cost 04..31, 53 payload chars)',
    };
  }
  return {
    scheme: PASSWORD_SCHEME_BCRYPT,
    reason: 'bcrypt $2a/$2b/$2y with cost 04..31 and 53 payload chars',
  };
}

export function classifyPasswordScheme(
  rawHash: string | null | undefined,
): LegacyPasswordClassification {
  // Opaque hash: no trim. The hash is examined exactly as supplied.
  if (typeof rawHash !== 'string' || rawHash.length === 0) {
    return { scheme: PASSWORD_SCHEME_RESET_REQUIRED, reason: 'no hash' };
  }
  if (/\s/.test(rawHash)) {
    return { scheme: PASSWORD_SCHEME_RESET_REQUIRED, reason: 'hash carries whitespace' };
  }
  if (rawHash.startsWith('$2')) {
    return classifyBcryptHash(rawHash);
  }
  return classifyLegacyPasswordHash(rawHash);
}

// ---------------------------------------------------------------------------
// Normalization (TypeScript mirror of the SQL `lu_login_identifier_normalize`).
// ---------------------------------------------------------------------------

export function normalizeIdentifier(input: string | null | undefined): string {
  if (typeof input !== 'string') return '';
  return input.trim().normalize('NFKC').toLowerCase();
}

export function normalizeIdentityCard(input: string | null | undefined): string | null {
  if (typeof input !== 'string') return null;
  const upper = input.trim().normalize('NFKC').toUpperCase();
  if (upper === '') return null;
  if (!/^[0-9A-Z-]{1,10}$/.test(upper)) return null;
  return upper;
}

export function normalizeSplitName(input: string | null | undefined): string {
  if (typeof input !== 'string') return '';
  return input.trim().normalize('NFC').replace(/\s+/g, ' ');
}

// ---------------------------------------------------------------------------
// Code-point length (PostgreSQL `length(text)` / `length(varchar)` semantics).
// ---------------------------------------------------------------------------

/**
 * Count Unicode code points (not UTF-16 units) in `s`. This mirrors the
 * semantics of PostgreSQL `length()` over `text` / `varchar` columns and
 * is the bound the F1/W14A character-limit checks use. Astral codepoints
 * (e.g. U+1F600) count as 1.
 */
export function codePointLength(s: string): number {
  let n = 0;
  for (const ch of s) {
    void ch;
    n++;
  }
  return n;
}

// ---------------------------------------------------------------------------
// Character bounds (PostgreSQL-aligned).
// ---------------------------------------------------------------------------

export const USERNAME_MAX_LENGTH = 256;
export const EMAIL_MAX_LENGTH = 256;
export const NAME_MAX_LENGTH = 100;
export const OPTIONAL_NAME_MAX_LENGTH = 100;

function enforceNonblankAndBound(
  value: string,
  fieldName: string,
  code: 'INVALID_IDENTIFIER' | 'INVALID_NAME' | 'INVALID_INPUT',
  max: number,
): void {
  if (value === '') {
    throw new LegacyMappingError(
      code,
      `${fieldName} field is required and must be non-empty after normalization`,
    );
  }
  if (codePointLength(value) > max) {
    throw new LegacyMappingError(
      code,
      `${fieldName} field exceeds the ${max}-character (code-point) bound`,
    );
  }
}

// ---------------------------------------------------------------------------
// Email syntax validator (sufficient rule, no reconciliation).
// ---------------------------------------------------------------------------

/**
 * Validate that `email` (already NFKC-normalized, lowercase, trimmed)
 * has a sufficient syntactic shape for downstream storage:
 *  - exactly one `@` separator;
 *  - no whitespace and no control characters anywhere;
 *  - non-empty local-part and non-empty domain;
 *  - dot-qualified domain with a non-empty final label (TLD).
 *
 * The normalized canonical value is preserved verbatim; this function
 * performs NO reconciliation (no splitting, aliasing, IDN normalization
 * or substitutions). Any violation throws `INVALID_IDENTIFIER` with a
 * field-name-only message; the raw email is never echoed.
 */
function assertEmailSyntax(email: string): void {
  // eslint-disable-next-line no-control-regex -- intentional control-character guard for the email syntax check
  if (/[\s\u0000-\u001f\u007f]/.test(email)) {
    throw new LegacyMappingError(
      'INVALID_IDENTIFIER',
      'email field contains whitespace or control characters',
    );
  }
  const firstAt = email.indexOf('@');
  if (firstAt < 0) {
    throw new LegacyMappingError(
      'INVALID_IDENTIFIER',
      'email field must contain a single @ separator',
    );
  }
  if (email.indexOf('@', firstAt + 1) >= 0) {
    throw new LegacyMappingError(
      'INVALID_IDENTIFIER',
      'email field must contain exactly one @ separator',
    );
  }
  const local = email.slice(0, firstAt);
  const domain = email.slice(firstAt + 1);
  if (local === '') {
    throw new LegacyMappingError('INVALID_IDENTIFIER', 'email field local-part must be non-empty');
  }
  if (domain === '') {
    throw new LegacyMappingError('INVALID_IDENTIFIER', 'email field domain must be non-empty');
  }
  const lastDot = domain.lastIndexOf('.');
  if (lastDot < 0) {
    throw new LegacyMappingError('INVALID_IDENTIFIER', 'email field domain must be dot-qualified');
  }
  if (lastDot === domain.length - 1) {
    throw new LegacyMappingError(
      'INVALID_IDENTIFIER',
      'email field domain must have a non-empty TLD label',
    );
  }
}

// ---------------------------------------------------------------------------
// Phone syntax validator (F1/W14B PostgreSQL CHECK mirror).
// ---------------------------------------------------------------------------

export const PHONE_MAX_LENGTH = 30;

export function normalizePhone(input: string | null | undefined): string {
  if (typeof input !== 'string') return '';
  return input.trim();
}

/**
 * Validate the trimmed phone against the F1/W14B PostgreSQL
 * `ck_lu_user_phone_number_format` CHECK, plus F1/W14A extras:
 *  - length (code-point) <= `PHONE_MAX_LENGTH` (30);
 *  - charset `^[+]?[0-9 ().-]+$` (one optional leading `+`, ASCII
 *    digits, spaces, `.`, `-`, balanced parentheses only);
 *  - 7..15 total ASCII digits via a non-cryptographic digit count;
 *  - parentheses balance: at no point may the running close count
 *    exceed the open count, and the final open count must be zero;
 *  - controls, letters, extensions, double `+`, multi-octet Unicode
 *    digits are rejected by the ASCII charset.
 *
 * The original trimmed formatting (separators, leading `+`, internal
 * spaces) is preserved verbatim; this function only signals accept or
 * reject. Any violation throws `INVALID_PHONE` with a field-name-only
 * message; the raw phone value is never echoed.
 */
export function validatePhoneSyntax(phone: string): void {
  if (typeof phone !== 'string') {
    throw new LegacyMappingError(
      'INVALID_PHONE',
      'phoneNumber field is required and must be a string',
    );
  }
  if (codePointLength(phone) > PHONE_MAX_LENGTH) {
    throw new LegacyMappingError(
      'INVALID_PHONE',
      `phoneNumber field exceeds the ${PHONE_MAX_LENGTH}-character bound`,
    );
  }
  if (!/^[+]?[0-9 ().-]+$/.test(phone)) {
    throw new LegacyMappingError(
      'INVALID_PHONE',
      'phoneNumber field does not match the canonical phone syntax (one optional leading +, digits, spaces, dot, hyphen, balanced parentheses)',
    );
  }
  let digits = 0;
  let opens = 0;
  for (const ch of phone) {
    if (ch >= '0' && ch <= '9') {
      digits++;
    } else if (ch === '(') {
      opens++;
    } else if (ch === ')') {
      opens--;
      if (opens < 0) {
        throw new LegacyMappingError(
          'INVALID_PHONE',
          'phoneNumber field has unbalanced parentheses',
        );
      }
    }
  }
  if (opens !== 0) {
    throw new LegacyMappingError('INVALID_PHONE', 'phoneNumber field has unbalanced parentheses');
  }
  if (digits < 7 || digits > 15) {
    throw new LegacyMappingError(
      'INVALID_PHONE',
      'phoneNumber field must carry 7..15 total digits',
    );
  }
}

// ---------------------------------------------------------------------------
// Strict UUID validation (RFC4122 UUID-shaped).
// ---------------------------------------------------------------------------

/**
 * Strict RFC4122 UUID-shaped pattern:
 *  - 8-4-4-4-12 hex groups,
 *  - version digit 1..7 (covers all current standard / draft UUID versions),
 *  - variant digit 8/9/a/b (RFC4122 variant `10xx`).
 *
 * Accepts either upper- or lower-case hex; the canonical return value is
 * always lowercase.
 */
const RFC4122_UUID_RE =
  /^[0-9a-f]{8}-[0-9a-f]{4}-[1-7][0-9a-f]{3}-[89ab][0-9a-f]{3}-[0-9a-f]{12}$/i;

export function verifyRfc4122Uuid(raw: string | null | undefined): string {
  if (typeof raw !== 'string') {
    throw new LegacyMappingError('INVALID_INPUT', 'callerAllocatedUserId must be a string');
  }
  const trimmed = raw.trim();
  if (trimmed === '' || !RFC4122_UUID_RE.test(trimmed)) {
    throw new LegacyMappingError(
      'INVALID_INPUT',
      'callerAllocatedUserId must be an RFC4122 UUID-shaped string',
    );
  }
  return trimmed.toLowerCase();
}

// ---------------------------------------------------------------------------
// Role + status mapping
// ---------------------------------------------------------------------------

export const ROLE_SUPERVISOR = 'Supervisor';
export const ROLE_ADMINISTRADOR = 'Administrador';
export const ROLE_SUPERADMIN = 'SuperAdmin';

export interface RoleMappingResult {
  readonly globalSuperAdmin: boolean;
  readonly siteRole: typeof ROLE_SUPERVISOR | typeof ROLE_ADMINISTRADOR | null;
}

/**
 * Crosswalk the legacy role enum against the (single) managed Identity role
 * name. The F1 contract requires agreement between both representations;
 * missing, unknown or contradictory combinations block the mapping.
 *
 * NOTE: the current input shape carries only one managed role string, so
 * this mapper cannot itself detect multiplicity greater than one. Callers
 * are responsible for pre-checking that exactly one managed role exists
 * for a given source user, or for extending the input to carry an explicit
 * count/list. That is intentionally out of scope here.
 */
export function mapLegacyRole(
  legacyRoleValue: number | string | null | undefined,
  identityRoleName: string | null | undefined,
): RoleMappingResult {
  const idn = (identityRoleName ?? '').trim();
  if (idn === '') {
    throw new LegacyMappingError(
      'INVALID_ROLE',
      'managed Identity role is required and must agree with the legacy role enum',
    );
  }
  const idnLower = idn.toLowerCase();

  const roleNum = typeof legacyRoleValue === 'number' ? legacyRoleValue : null;
  const roleStr = typeof legacyRoleValue === 'string' ? legacyRoleValue.trim().toLowerCase() : '';

  let target: RoleMappingResult;
  if (roleNum === 99 || roleStr === 'superadmin' || roleStr === 'super-admin') {
    target = { globalSuperAdmin: true, siteRole: ROLE_ADMINISTRADOR };
  } else if (roleNum === 1 || roleStr === 'administrador') {
    target = { globalSuperAdmin: false, siteRole: ROLE_ADMINISTRADOR };
  } else if (roleNum === 2 || roleStr === 'supervisor') {
    target = { globalSuperAdmin: false, siteRole: ROLE_SUPERVISOR };
  } else {
    throw new LegacyMappingError('INVALID_ROLE', 'legacy role enum is missing or unrecognized');
  }

  if (idnLower === 'superadmin') {
    if (!target.globalSuperAdmin) {
      throw new LegacyMappingError(
        'ROLE_MISMATCH',
        'managed Identity role disagrees with legacy role enum',
      );
    }
  } else if (idnLower === 'administrator') {
    if (target.globalSuperAdmin || target.siteRole !== ROLE_ADMINISTRADOR) {
      throw new LegacyMappingError(
        'ROLE_MISMATCH',
        'managed Identity role disagrees with legacy role enum',
      );
    }
  } else if (idnLower === 'supervisor') {
    if (target.globalSuperAdmin || target.siteRole !== ROLE_SUPERVISOR) {
      throw new LegacyMappingError(
        'ROLE_MISMATCH',
        'managed Identity role disagrees with legacy role enum',
      );
    }
  } else {
    throw new LegacyMappingError('INVALID_ROLE', 'managed Identity role is unrecognized');
  }

  return target;
}

export const ACCOUNT_STATUS_ACTIVE = 'active';
export const ACCOUNT_STATUS_INACTIVE = 'inactive';
export const ACCOUNT_STATUS_DELETED = 'deleted';
export const MEMBERSHIP_STATUS_ACTIVE = 'active';
export const MEMBERSHIP_STATUS_SUSPENDED = 'suspended';
export const MEMBERSHIP_STATUS_REVOKED = 'revoked';

export interface StatusMappingResult {
  readonly accountStatus:
    typeof ACCOUNT_STATUS_ACTIVE | typeof ACCOUNT_STATUS_INACTIVE | typeof ACCOUNT_STATUS_DELETED;
  readonly membershipStatus:
    | typeof MEMBERSHIP_STATUS_ACTIVE
    | typeof MEMBERSHIP_STATUS_SUSPENDED
    | typeof MEMBERSHIP_STATUS_REVOKED;
}

export function mapLegacyStatus(
  legacyStatusValue: number | string | null | undefined,
): StatusMappingResult {
  const num = typeof legacyStatusValue === 'number' ? legacyStatusValue : null;
  const str = typeof legacyStatusValue === 'string' ? legacyStatusValue.trim().toLowerCase() : '';
  if (num === 0 || str === 'activo' || str === 'active') {
    return { accountStatus: ACCOUNT_STATUS_ACTIVE, membershipStatus: MEMBERSHIP_STATUS_ACTIVE };
  }
  if (num === 1 || str === 'inactivo' || str === 'inactive' || str === 'disabled') {
    return {
      accountStatus: ACCOUNT_STATUS_INACTIVE,
      membershipStatus: MEMBERSHIP_STATUS_SUSPENDED,
    };
  }
  if (num === 2 || str === 'eliminado' || str === 'deleted') {
    return { accountStatus: ACCOUNT_STATUS_DELETED, membershipStatus: MEMBERSHIP_STATUS_REVOKED };
  }
  throw new LegacyMappingError('INVALID_STATUS', 'legacy status is missing or unrecognized');
}

// ---------------------------------------------------------------------------
// Strict date / instant parsing.
// ---------------------------------------------------------------------------

const STRICT_DATE_RE = /^\d{4}-\d{2}-\d{2}$/;
const STRICT_INSTANT_RE = /^\d{4}-\d{2}-\d{2}T\d{2}:\d{2}:\d{2}(\.\d{1,9})?Z$/;

/** Parse a strict YYYY-MM-DD date string or null input. Throws on bad input. */
export function parseStrictDate(
  input: string | null | undefined,
  fieldName: string,
): string | null {
  if (input === null || input === undefined) return null;
  if (typeof input !== 'string') {
    throw new LegacyMappingError(
      'INVALID_DATE',
      `${fieldName} must be a YYYY-MM-DD date string or null`,
    );
  }
  const trimmed = input.trim();
  if (trimmed === '') return null;
  if (!STRICT_DATE_RE.test(trimmed)) {
    throw new LegacyMappingError(
      'INVALID_DATE',
      `${fieldName} must be a strict YYYY-MM-DD date string`,
    );
  }
  const parsed = new Date(`${trimmed}T00:00:00Z`);
  if (Number.isNaN(parsed.getTime())) {
    throw new LegacyMappingError('INVALID_DATE', `${fieldName} is not a valid calendar date`);
  }
  const iso = parsed.toISOString().slice(0, 10);
  if (iso !== trimmed) {
    throw new LegacyMappingError('INVALID_DATE', `${fieldName} is not a valid calendar date`);
  }
  return iso;
}

/**
 * Parse a strict UTC instant string or null input. Throws on bad input.
 *
 * The strict shape regex enforces the trailing `Z`, so any local-time
 * offset (e.g. `+00:00`) is rejected up front. The function preserves
 * the caller's exact string representation (with or without fractional
 * seconds) once it has been validated as a real instant.
 */
export function parseStrictInstant(
  input: string | null | undefined,
  fieldName: string,
): string | null {
  if (input === null || input === undefined) return null;
  if (typeof input !== 'string') {
    throw new LegacyMappingError(
      'INVALID_DATE',
      `${fieldName} must be a UTC instant string or null`,
    );
  }
  const trimmed = input.trim();
  if (trimmed === '') return null;
  if (!STRICT_INSTANT_RE.test(trimmed)) {
    throw new LegacyMappingError(
      'INVALID_DATE',
      `${fieldName} must be a strict UTC instant (ISO-8601 with trailing Z)`,
    );
  }
  const parsed = new Date(trimmed);
  if (Number.isNaN(parsed.getTime())) {
    throw new LegacyMappingError('INVALID_DATE', `${fieldName} is not a valid UTC instant`);
  }
  return trimmed;
}

/**
 * Convert a `Date` object to a UTC ISO instant string, throwing on invalid
 * dates. Used by `mapLegacyUser` when callers supply `Date` instances.
 */
export function dateToUtcInstant(value: Date, fieldName: string): string {
  if (!(value instanceof Date) || Number.isNaN(value.getTime())) {
    throw new LegacyMappingError('INVALID_DATE', `${fieldName} is not a valid Date instance`);
  }
  return value.toISOString();
}

/**
 * Convert a `Date` object to a UTC YYYY-MM-DD date string, throwing on
 * invalid dates.
 */
export function dateToUtcDate(value: Date, fieldName: string): string {
  if (!(value instanceof Date) || Number.isNaN(value.getTime())) {
    throw new LegacyMappingError('INVALID_DATE', `${fieldName} is not a valid Date instance`);
  }
  return value.toISOString().slice(0, 10);
}

// ---------------------------------------------------------------------------
// Actor-id validation.
// ---------------------------------------------------------------------------

/**
 * Returns `null` for null input and the same integer for valid input.
 * Negative or non-integer actor ids are rejected with a typed error.
 */
export function validateActorId(
  input: number | null | undefined,
  fieldName: string,
): number | null {
  if (input === null || input === undefined) return null;
  if (typeof input !== 'number' || !Number.isInteger(input) || input < 0) {
    throw new LegacyMappingError(
      'INVALID_ACTOR',
      `${fieldName} must be null or a nonnegative integer`,
    );
  }
  return input;
}

// ---------------------------------------------------------------------------
// Optional-field normalization helpers (F1/W14A).
// ---------------------------------------------------------------------------

/**
 * Normalize an optional string field (position, department) to either
 * `null` (when blank after trim) or its NFC-normalized, whitespace-
 * collapsed form. The caller must follow up with a bound check via
 * `enforceOptionalWorkField` before returning the mapping.
 */
function normalizeOptionalWorkField(input: unknown): string | null {
  if (typeof input !== 'string') return null;
  const trimmed = input.trim();
  if (trimmed === '') return null;
  return normalizeSplitName(trimmed);
}

/**
 * Enforce the F1/W14A ≤`OPTIONAL_NAME_MAX_LENGTH` (100) code-point bound
 * on an already-normalized optional work field. Empty / null is treated
 * as "not supplied" and silently kept null; non-empty oversized values
 * are rejected with a typed `INVALID_INPUT` error referencing the field
 * name only. The opaque value is never echoed.
 */
function enforceOptionalWorkField(value: string | null, fieldName: string): string | null {
  if (value === null) return null;
  if (codePointLength(value) > OPTIONAL_NAME_MAX_LENGTH) {
    throw new LegacyMappingError(
      'INVALID_INPUT',
      `${fieldName} field exceeds the ${OPTIONAL_NAME_MAX_LENGTH}-character (code-point) bound`,
    );
  }
  return value;
}

// ---------------------------------------------------------------------------
// Mapping entry
// ---------------------------------------------------------------------------

export interface LegacyUserInput {
  readonly legacyUserId: number;
  readonly username: string | null | undefined;
  readonly email: string | null | undefined;
  readonly firstName: string | null | undefined;
  readonly lastName: string | null | undefined;
  readonly secondLastName?: string | null | undefined;
  readonly identityCard: string | null | undefined;
  readonly phoneNumber: string | null | undefined;
  readonly passwordHash: string | null | undefined;
  readonly roleValue: number | string | null | undefined;
  readonly identityRoleName: string | null | undefined;
  readonly statusValue: number | string | null | undefined;
  readonly position?: string | null | undefined;
  readonly department?: string | null | undefined;
  readonly hireDate?: Date | string | null | undefined;
  readonly twoFactorEnabled?: boolean | null | undefined;
  readonly createdDate?: Date | string | null | undefined;
  readonly createdByLegacyId?: number | null | undefined;
  readonly lastModifiedDate?: Date | string | null | undefined;
  readonly modifiedByLegacyId?: number | null | undefined;
}

export interface LegacyUserMapping {
  readonly sourceSystem: typeof SOURCE_SYSTEM_ASP_SQLSERVER;
  readonly legacyUserId: number;
  /** Caller-allocated canonical UUID; preserved verbatim in lowercase form. */
  readonly callerAllocatedUserId: string;
  readonly username: string;
  readonly email: string;
  readonly normalizedEmail: string;
  readonly identityCard: string;
  readonly phoneNumber: string;
  readonly firstName: string;
  readonly lastName: string;
  readonly secondLastName: string | null;
  readonly fullName: string;
  readonly passwordClassification: LegacyPasswordClassification;
  readonly mustChangePassword: boolean;
  readonly passwordMigratedAt: null;
  readonly accountStatus:
    typeof ACCOUNT_STATUS_ACTIVE | typeof ACCOUNT_STATUS_INACTIVE | typeof ACCOUNT_STATUS_DELETED;
  readonly membershipStatus:
    | typeof MEMBERSHIP_STATUS_ACTIVE
    | typeof MEMBERSHIP_STATUS_SUSPENDED
    | typeof MEMBERSHIP_STATUS_REVOKED;
  readonly isSuperAdmin: boolean;
  readonly siteRole: typeof ROLE_SUPERVISOR | typeof ROLE_ADMINISTRADOR | null;
  readonly position: string | null;
  readonly department: string | null;
  readonly hireDate: string | null;
  readonly createdDate: string | null;
  readonly createdByLegacyId: number | null;
  readonly lastModifiedDate: string | null;
  readonly modifiedByLegacyId: number | null;
  readonly twoFactorResetRequired: boolean;
  readonly reconciliation: 'canonical';
  readonly reconcileReason: null;
}

/**
 * Map a legacy SQL Server row to a canonical lu_user payload. The mapping
 * is pure and deterministic. Either it returns a complete, validated
 * mapping (reconciliation = 'canonical') or it throws a typed
 * `LegacyMappingError`. There is no `pending_reconciliation` output
 * because the frozen F1 contract maps every complete, valid legacy row
 * all-or-blocked.
 *
 * The caller supplies `callerAllocatedUserId` (the crosswalk UUID). The
 * mapper validates it strictly as RFC4122 UUID-shaped and returns it
 * verbatim in canonical lowercase; the F2 crosswalk row can then be
 * written by the caller from `sourceSystem`, `legacyUserId` and
 * `callerAllocatedUserId`.
 */
export function mapLegacyUser(
  input: LegacyUserInput,
  callerAllocatedUserId: string,
): LegacyUserMapping {
  // 1. legacyUserId must be a nonnegative integer.
  if (
    typeof input.legacyUserId !== 'number' ||
    !Number.isInteger(input.legacyUserId) ||
    input.legacyUserId < 0
  ) {
    throw new LegacyMappingError(
      'INVALID_LEGACY_USER_ID',
      'legacyUserId must be a nonnegative integer',
    );
  }

  // 2. callerAllocatedUserId must be a strict RFC4122 UUID-shaped string.
  const verifiedUuid = verifyRfc4122Uuid(callerAllocatedUserId);

  // 3. Required normalized identifiers, with PG-aligned bounds.
  const username = normalizeIdentifier(input.username);
  enforceNonblankAndBound(username, 'username', 'INVALID_IDENTIFIER', USERNAME_MAX_LENGTH);

  const emailNormalized = normalizeIdentifier(input.email);
  enforceNonblankAndBound(emailNormalized, 'email', 'INVALID_IDENTIFIER', EMAIL_MAX_LENGTH);
  assertEmailSyntax(emailNormalized);

  const first = normalizeSplitName(input.firstName);
  enforceNonblankAndBound(first, 'firstName', 'INVALID_NAME', NAME_MAX_LENGTH);

  const last = normalizeSplitName(input.lastName);
  enforceNonblankAndBound(last, 'lastName', 'INVALID_NAME', NAME_MAX_LENGTH);

  const secondRaw = normalizeSplitName(input.secondLastName);
  const second = secondRaw === '' ? null : secondRaw;
  // secondLastName is optional: when present it must still be non-empty
  // (it is the result of a normalization, not a fresh trim) AND fit the
  // 100-character (code-point) name bound per the F1/W14A contract.
  if (second !== null && codePointLength(second) > NAME_MAX_LENGTH) {
    throw new LegacyMappingError(
      'INVALID_NAME',
      `secondLastName field exceeds the ${NAME_MAX_LENGTH}-character (code-point) bound`,
    );
  }

  const identityCard = normalizeIdentityCard(input.identityCard);
  if (identityCard === null) {
    throw new LegacyMappingError(
      'INVALID_IDENTIFIER',
      'identityCard field is required and must match [0-9A-Z-]{1,10}',
    );
  }

  const phone = normalizePhone(input.phoneNumber);
  if (phone === '') {
    throw new LegacyMappingError(
      'INVALID_PHONE',
      'phoneNumber field is required and must be non-empty after trim',
    );
  }
  // F1/W14A strict phone syntax mirrors the PostgreSQL CHECK.
  validatePhoneSyntax(phone);

  // 4. Actor ids (nullable; non-null must be nonnegative integers).
  const createdByLegacyId = validateActorId(input.createdByLegacyId, 'createdByLegacyId');
  const modifiedByLegacyId = validateActorId(input.modifiedByLegacyId, 'modifiedByLegacyId');

  // 5. Role + status (will throw on missing/unknown/contradictory).
  const role = mapLegacyRole(input.roleValue, input.identityRoleName);
  const status = mapLegacyStatus(input.statusValue);

  // 6. Password classification. The opaque hash is never copied into the
  //    mapping result, only its scheme + bounded metadata. W14A strict
  //    Base64 + bcrypt rules gate the legacy_identity_v{2,3} / bcrypt
  //    schemes; everything else is `reset_required`.
  const password = classifyPasswordScheme(input.passwordHash);
  const twoFactorForces = input.twoFactorEnabled === true;
  const finalScheme: LegacyPasswordScheme = twoFactorForces
    ? PASSWORD_SCHEME_RESET_REQUIRED
    : password.scheme;
  const finalPassword: LegacyPasswordClassification = twoFactorForces
    ? {
        scheme: PASSWORD_SCHEME_RESET_REQUIRED,
        reason: 'TwoFactorEnabled=true forces reset_required',
      }
    : password;
  const mustChangePassword = finalScheme === PASSWORD_SCHEME_RESET_REQUIRED;

  // 7. Dates: strict UTC instant / YYYY-MM-DD. The mapper never silently
  //    slices raw input strings.
  const hireDate =
    input.hireDate instanceof Date
      ? dateToUtcDate(input.hireDate, 'hireDate')
      : parseStrictDate(typeof input.hireDate === 'string' ? input.hireDate : null, 'hireDate');
  const createdDate =
    input.createdDate instanceof Date
      ? dateToUtcInstant(input.createdDate, 'createdDate')
      : parseStrictInstant(
          typeof input.createdDate === 'string' ? input.createdDate : null,
          'createdDate',
        );
  const lastModifiedDate =
    input.lastModifiedDate instanceof Date
      ? dateToUtcInstant(input.lastModifiedDate, 'lastModifiedDate')
      : parseStrictInstant(
          typeof input.lastModifiedDate === 'string' ? input.lastModifiedDate : null,
          'lastModifiedDate',
        );

  // 8. Optional work fields: normalize, then bound-check before return.
  const position = enforceOptionalWorkField(normalizeOptionalWorkField(input.position), 'position');
  const department = enforceOptionalWorkField(
    normalizeOptionalWorkField(input.department),
    'department',
  );

  // 9. Derived fullName from normalized split names.
  const fullName = `${first} ${last}${second !== null ? ' ' + second : ''}`.trim();

  return {
    sourceSystem: SOURCE_SYSTEM_ASP_SQLSERVER,
    legacyUserId: input.legacyUserId,
    callerAllocatedUserId: verifiedUuid,
    username,
    email: emailNormalized,
    normalizedEmail: emailNormalized,
    identityCard,
    phoneNumber: phone,
    firstName: first,
    lastName: last,
    secondLastName: second,
    fullName,
    passwordClassification: finalPassword,
    mustChangePassword,
    passwordMigratedAt: null,
    accountStatus: status.accountStatus,
    membershipStatus: status.membershipStatus,
    isSuperAdmin: role.globalSuperAdmin,
    siteRole: role.siteRole,
    position,
    department,
    hireDate,
    createdDate,
    createdByLegacyId,
    lastModifiedDate,
    modifiedByLegacyId,
    twoFactorResetRequired: twoFactorForces,
    reconciliation: 'canonical',
    reconcileReason: null,
  };
}
