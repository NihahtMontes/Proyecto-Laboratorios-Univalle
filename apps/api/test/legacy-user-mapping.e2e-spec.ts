/**
 * Legacy-user mapping unit tests (pure, no DB).
 *
 * Coverage:
 *  - Identity V2 / V3 / bcrypt / reset_required classification;
 *  - V2/V3 valid legacy and bcrypt pre-F2 yield mustChangePassword=false;
 *  - reset_required (malformed/blank/2FA) forces mustChangePassword=true;
 *  - role / status mapping per F1 (legacy enum must agree with managed
 *    Identity role; missing or unknown managed role blocks);
 *  - typed blockers for every required field (UUID, names, CI, phone,
 *    legacyUserId, dates, actors);
 *  - NFKC normalization + hyphen preservation for identity cards;
 *  - phone trimmed and bounded by PHONE_MAX_LENGTH;
 *  - F1/W14A strict STANDARD Base64 decode (length multiple of 4,
 *    canonical padding, canonical roundtrip, no URL-safe alphabet,
 *    noncanonical trailing bits rejected);
 *  - F1/W14A bcrypt shape restricted to $2a/$2b/$2y with cost 04..31
 *    and exactly 53 payload chars;
 *  - F1/W14A PG-aligned code-point bounds (256 for username/email,
 *    100 for names and work fields) verified with astral code points;
 *  - F1/W14A email-syntax validation (single @, no whitespace/control,
 *    dot-qualified domain, no reconciliation, normalized value
 *    preserved);
 *  - F1/W14A phone-syntax validator mirroring the PostgreSQL CHECK
 *    (optional leading +, charset, 7..15 digits, balanced
 *    parentheses);
 *  - caller-supplied UUID is RFC4122-validated and returned verbatim in
 *    canonical lowercase;
 *  - error messages never embed raw PII or the opaque hash;
 *  - reconciliation='canonical', reconcileReason=null for every valid row.
 */
import {
  ACCOUNT_STATUS_ACTIVE,
  ACCOUNT_STATUS_DELETED,
  ACCOUNT_STATUS_INACTIVE,
  LegacyMappingError,
  MARKER_IDENTITY_V2,
  MARKER_IDENTITY_V3,
  MEMBERSHIP_STATUS_ACTIVE,
  MEMBERSHIP_STATUS_REVOKED,
  MEMBERSHIP_STATUS_SUSPENDED,
  OPTIONAL_NAME_MAX_LENGTH,
  PASSWORD_SCHEME_BCRYPT,
  PASSWORD_SCHEME_LEGACY_V2,
  PASSWORD_SCHEME_LEGACY_V3,
  PASSWORD_SCHEME_RESET_REQUIRED,
  PHONE_MAX_LENGTH,
  ROLE_ADMINISTRADOR,
  ROLE_SUPERVISOR,
  SOURCE_SYSTEM_ASP_SQLSERVER,
  USERNAME_MAX_LENGTH,
  EMAIL_MAX_LENGTH,
  NAME_MAX_LENGTH,
  classifyBcryptHash,
  classifyLegacyPasswordHash,
  classifyPasswordScheme,
  codePointLength,
  dateToUtcDate,
  dateToUtcInstant,
  decodeLegacyIdentityBase64,
  mapLegacyRole,
  mapLegacyStatus,
  mapLegacyUser,
  normalizeIdentifier,
  normalizeIdentityCard,
  normalizePhone,
  normalizeSplitName,
  parseStrictDate,
  parseStrictInstant,
  validateActorId,
  validatePhoneSyntax,
  verifyRfc4122Uuid,
} from '../src/database/legacy-user-mapping.js';

const V3_PAYLOAD = (prf: number, iter: number, saltLen: number, subkeyLen: number): string => {
  const header = Buffer.alloc(13);
  header[0] = MARKER_IDENTITY_V3;
  header.writeUInt32BE(prf, 1);
  header.writeUInt32BE(iter, 5);
  header.writeUInt32BE(saltLen, 9);
  const salt = Buffer.alloc(saltLen, 0x33);
  const subkey = Buffer.alloc(subkeyLen, 0x44);
  return Buffer.concat([header, salt, subkey]).toString('base64');
};

const V2_PAYLOAD = (): string =>
  Buffer.concat([
    Buffer.from([MARKER_IDENTITY_V2]),
    Buffer.alloc(16, 0x11),
    Buffer.alloc(32, 0x22),
  ]).toString('base64');

const SAMPLE_UUID = 'aaaaaaaa-bbbb-4ccc-8ddd-eeeeeeeeeeee';
const SAMPLE_UUID_UPPER = 'AAAAAAAA-BBBB-4CCC-8DDD-EEEEEEEEEEEE';

const CANARY = {
  username: 'pii-canary-username-DO-NOT-LOG',
  email: 'pii-canary-email-DO-NOT-LOG@example.com',
  firstName: 'PiiCanaryFirstName',
  lastName: 'PiiCanaryLastName',
  identityCard: 'AB-1234-CD',
  phone: '+59170012345',
};

describe('classifyLegacyPasswordHash', () => {
  it('returns reset_required for a null/empty/invalid base64 hash', () => {
    expect(classifyLegacyPasswordHash(null).scheme).toBe(PASSWORD_SCHEME_RESET_REQUIRED);
    expect(classifyLegacyPasswordHash('').scheme).toBe(PASSWORD_SCHEME_RESET_REQUIRED);
    expect(classifyLegacyPasswordHash('@@@ not base64 @@@').scheme).toBe(
      PASSWORD_SCHEME_RESET_REQUIRED,
    );
  });

  it('returns reset_required when decoded bytes exceed the 141-byte limit', () => {
    const raw = Buffer.alloc(160, 0x00).toString('base64');
    expect(classifyLegacyPasswordHash(raw).scheme).toBe(PASSWORD_SCHEME_RESET_REQUIRED);
  });

  it('decodes V2 marker + 49-byte payload as legacy_identity_v2', () => {
    const raw = V2_PAYLOAD();
    const result = classifyLegacyPasswordHash(raw);
    expect(result.scheme).toBe(PASSWORD_SCHEME_LEGACY_V2);
  });

  it('decodes V2 with the wrong shape as reset_required', () => {
    const raw = Buffer.concat([
      Buffer.from([MARKER_IDENTITY_V2]),
      Buffer.alloc(16, 0x11),
      Buffer.alloc(20, 0x22),
    ]).toString('base64');
    expect(classifyLegacyPasswordHash(raw).scheme).toBe(PASSWORD_SCHEME_RESET_REQUIRED);
  });

  it('decodes V3 SHA512/100000/salt32/subkey32 as legacy_identity_v3 with metadata', () => {
    const raw = V3_PAYLOAD(2, 100_000, 32, 32);
    const result = classifyLegacyPasswordHash(raw);
    expect(result.scheme).toBe(PASSWORD_SCHEME_LEGACY_V3);
    expect(result.v3Metadata).toEqual({
      prf: 2,
      iterations: 100_000,
      saltLength: 32,
      subkeyLength: 32,
    });
  });

  it('decodes V3 with iterations out of bounds as reset_required', () => {
    const raw = V3_PAYLOAD(2, 999, 24, 24);
    expect(classifyLegacyPasswordHash(raw).scheme).toBe(PASSWORD_SCHEME_RESET_REQUIRED);
  });

  it('decodes V3 with an unsupported PRF as reset_required', () => {
    const raw = V3_PAYLOAD(7, 100_000, 24, 24);
    expect(classifyLegacyPasswordHash(raw).scheme).toBe(PASSWORD_SCHEME_RESET_REQUIRED);
  });
});

describe('classifyBcryptHash', () => {
  it('accepts canonical $2a$12$... hashes', () => {
    const hash = '$2a$12$' + 'A'.repeat(53);
    expect(classifyBcryptHash(hash).scheme).toBe(PASSWORD_SCHEME_BCRYPT);
  });

  it('accepts $2b$10$... hashes (lowercase payload)', () => {
    const hash = '$2b$10$' + 'x'.repeat(53);
    expect(classifyBcryptHash(hash).scheme).toBe(PASSWORD_SCHEME_BCRYPT);
  });

  it('accepts $2y$04$... hashes (cost 04 boundary)', () => {
    const hash = '$2y$04$' + 'A'.repeat(53);
    expect(classifyBcryptHash(hash).scheme).toBe(PASSWORD_SCHEME_BCRYPT);
  });

  it('rejects malformed bcrypt strings as reset_required', () => {
    expect(classifyBcryptHash('not-a-bcrypt-hash').scheme).toBe(PASSWORD_SCHEME_RESET_REQUIRED);
    expect(classifyBcryptHash('$2x$12$short').scheme).toBe(PASSWORD_SCHEME_RESET_REQUIRED);
    expect(classifyBcryptHash('$2a$03$' + 'x'.repeat(53)).scheme).toBe(
      PASSWORD_SCHEME_RESET_REQUIRED,
    ); // cost 03 below 04
    expect(classifyBcryptHash('$2a$32$' + 'x'.repeat(53)).scheme).toBe(
      PASSWORD_SCHEME_RESET_REQUIRED,
    ); // cost 32 above 31
    expect(classifyBcryptHash('$2a$10$' + 'x'.repeat(52)).scheme).toBe(
      PASSWORD_SCHEME_RESET_REQUIRED,
    ); // 52 payload chars
    expect(classifyBcryptHash('$2a$10$' + 'x'.repeat(54)).scheme).toBe(
      PASSWORD_SCHEME_RESET_REQUIRED,
    ); // 54 payload chars
    expect(classifyBcryptHash('$2a$10$' + '!'.repeat(53)).scheme).toBe(
      PASSWORD_SCHEME_RESET_REQUIRED,
    ); // '!' is not in the bcrypt alphabet
    expect(classifyBcryptHash('').scheme).toBe(PASSWORD_SCHEME_RESET_REQUIRED);
  });
});

describe('classifyPasswordScheme dispatcher', () => {
  it('routes bcrypt-like hashes to bcrypt', () => {
    expect(classifyPasswordScheme('$2b$10$' + 'x'.repeat(53)).scheme).toBe(PASSWORD_SCHEME_BCRYPT);
  });

  it('routes Identity V2/V3 to the right scheme', () => {
    const v2Raw = V2_PAYLOAD();
    const v3Raw = V3_PAYLOAD(2, 100_000, 24, 24);
    expect(classifyPasswordScheme(v2Raw).scheme).toBe(PASSWORD_SCHEME_LEGACY_V2);
    expect(classifyPasswordScheme(v3Raw).scheme).toBe(PASSWORD_SCHEME_LEGACY_V3);
  });

  it('routes empty strings to reset_required', () => {
    expect(classifyPasswordScheme(null).scheme).toBe(PASSWORD_SCHEME_RESET_REQUIRED);
    expect(classifyPasswordScheme('').scheme).toBe(PASSWORD_SCHEME_RESET_REQUIRED);
  });

  it('routes whitespace-padded canonical bcrypt to reset_required (no trim, opaque hash)', () => {
    const canonical = '$2b$10$' + 'x'.repeat(53);
    expect(classifyPasswordScheme(` ${canonical}`).scheme).toBe(PASSWORD_SCHEME_RESET_REQUIRED);
    expect(classifyPasswordScheme(`${canonical} `).scheme).toBe(PASSWORD_SCHEME_RESET_REQUIRED);
    expect(classifyPasswordScheme(`\n${canonical}\n`).scheme).toBe(PASSWORD_SCHEME_RESET_REQUIRED);
    expect(classifyPasswordScheme(`${canonical.slice(0, 4)} ${canonical.slice(4)}`).scheme).toBe(
      PASSWORD_SCHEME_RESET_REQUIRED,
    );
  });

  it('routes whitespace-padded canonical V2/V3 to reset_required (no trim, opaque hash)', () => {
    const v2Raw = V2_PAYLOAD();
    const v3Raw = V3_PAYLOAD(2, 100_000, 24, 24);
    expect(classifyPasswordScheme(` ${v2Raw}`).scheme).toBe(PASSWORD_SCHEME_RESET_REQUIRED);
    expect(classifyPasswordScheme(`${v2Raw} `).scheme).toBe(PASSWORD_SCHEME_RESET_REQUIRED);
    expect(classifyPasswordScheme(` ${v3Raw}`).scheme).toBe(PASSWORD_SCHEME_RESET_REQUIRED);
    expect(classifyPasswordScheme(`${v3Raw}\n`).scheme).toBe(PASSWORD_SCHEME_RESET_REQUIRED);
  });

  it('routes whitespace-only / blank input to reset_required', () => {
    expect(classifyPasswordScheme(' ').scheme).toBe(PASSWORD_SCHEME_RESET_REQUIRED);
    expect(classifyPasswordScheme('   ').scheme).toBe(PASSWORD_SCHEME_RESET_REQUIRED);
    expect(classifyPasswordScheme('\n\t').scheme).toBe(PASSWORD_SCHEME_RESET_REQUIRED);
  });

  it('classification reason never embeds the opaque hash', () => {
    const canonical = '$2b$10$' + 'x'.repeat(53);
    const reasons = [
      classifyPasswordScheme(` ${canonical}`).reason,
      classifyPasswordScheme(`${canonical.slice(0, 4)} ${canonical.slice(4)}`).reason,
      classifyPasswordScheme(`${canonical}\n`).reason,
    ];
    for (const reason of reasons) {
      expect(reason).not.toContain(canonical);
      expect(reason).not.toContain('xx');
    }
  });
});

describe('classifyBcryptHash opaque-hash contract', () => {
  const canonical = '$2b$10$' + 'x'.repeat(53);
  it('rejects leading whitespace without echoing the hash', () => {
    const r = classifyBcryptHash(` ${canonical}`);
    expect(r.scheme).toBe(PASSWORD_SCHEME_RESET_REQUIRED);
    expect(r.reason).not.toContain(canonical);
  });
  it('rejects trailing whitespace without echoing the hash', () => {
    const r = classifyBcryptHash(`${canonical} `);
    expect(r.scheme).toBe(PASSWORD_SCHEME_RESET_REQUIRED);
    expect(r.reason).not.toContain(canonical);
  });
  it('rejects internal whitespace without echoing the hash', () => {
    const r = classifyBcryptHash(`${canonical.slice(0, 6)} ${canonical.slice(6)}`);
    expect(r.scheme).toBe(PASSWORD_SCHEME_RESET_REQUIRED);
    expect(r.reason).not.toContain(canonical);
  });
  it('rejects blank-whitespace input without echoing the hash', () => {
    expect(classifyBcryptHash(' ').scheme).toBe(PASSWORD_SCHEME_RESET_REQUIRED);
    expect(classifyBcryptHash('\t\n').scheme).toBe(PASSWORD_SCHEME_RESET_REQUIRED);
  });
  it('accepts canonical exact input unchanged (no whitespace)', () => {
    expect(classifyBcryptHash(canonical).scheme).toBe(PASSWORD_SCHEME_BCRYPT);
  });
});

describe('identifier normalization', () => {
  it('lowercases, trims and NFKC-normalizes identifiers', () => {
    expect(normalizeIdentifier('  Foo@Bar.COM ')).toBe('foo@bar.com');
  });

  it('applies NFKC composition before lowercasing (e.g. full-width @)', () => {
    // NFKC of full-width U+FF20 (`＠`) is plain `@`; NFKC of full-width
    // U+FF46 (`ｆ`) is ASCII `f`, etc. The whole string must lowercase
    // after composition.
    expect(normalizeIdentifier('\uff20\uff46\uff4f\uff4f\uff20\uff42\uff41\uff52')).toBe(
      '@foo@bar',
    );
  });

  it('uppercases identity cards, restricts to [0-9A-Z-] and preserves internal hyphens', () => {
    expect(normalizeIdentityCard('  ab-1234-cd ')).toBe('AB-1234-CD');
    expect(normalizeIdentityCard('AB-1234567-CD')).toBeNull(); // length > 10
    expect(normalizeIdentityCard('!@#bad')).toBeNull();
    expect(normalizeIdentityCard(null)).toBeNull();
  });

  it('preserves case and NFC-normalizes split-name whitespace', () => {
    expect(normalizeSplitName('  Juan   Pérez  García ')).toBe('Juan Pérez García');
    expect(normalizeSplitName('')).toBe('');
  });

  it('keeps a leading + on phone numbers and trims only', () => {
    expect(normalizePhone('  +591 700-12345 ')).toBe('+591 700-12345');
  });
});

describe('role and status mapping', () => {
  it('maps Supervisor to (no global, pilot Supervisor)', () => {
    expect(mapLegacyRole(2, 'Supervisor')).toEqual({
      globalSuperAdmin: false,
      siteRole: ROLE_SUPERVISOR,
    });
  });

  it('maps Administrador/Administrator to (no global, pilot Administrador)', () => {
    expect(mapLegacyRole(1, 'Administrator')).toEqual({
      globalSuperAdmin: false,
      siteRole: ROLE_ADMINISTRADOR,
    });
  });

  it('maps SuperAdmin to (global flag, pilot Administrador)', () => {
    expect(mapLegacyRole(99, 'SuperAdmin')).toEqual({
      globalSuperAdmin: true,
      siteRole: ROLE_ADMINISTRADOR,
    });
  });

  it('throws ROLE_MISMATCH when the Identity role name disagrees with the enum', () => {
    expect(() => mapLegacyRole(1, 'Supervisor')).toThrow(LegacyMappingError);
  });

  it('throws INVALID_ROLE when the managed Identity role is missing entirely', () => {
    expect(() => mapLegacyRole(1, null)).toThrow(LegacyMappingError);
    expect(() => mapLegacyRole(1, '')).toThrow(LegacyMappingError);
    expect(() => mapLegacyRole(1, '   ')).toThrow(LegacyMappingError);
  });

  it('throws INVALID_ROLE for an unrecognized role', () => {
    expect(() => mapLegacyRole(42, 'Unknown')).toThrow(LegacyMappingError);
  });

  it('throws INVALID_ROLE for an unrecognized managed Identity role', () => {
    expect(() => mapLegacyRole(1, 'GhostRole')).toThrow(LegacyMappingError);
  });

  it('maps legacy status 0/1/2 to canonical account/membership pairs', () => {
    expect(mapLegacyStatus(0)).toEqual({
      accountStatus: ACCOUNT_STATUS_ACTIVE,
      membershipStatus: MEMBERSHIP_STATUS_ACTIVE,
    });
    expect(mapLegacyStatus(1)).toEqual({
      accountStatus: ACCOUNT_STATUS_INACTIVE,
      membershipStatus: MEMBERSHIP_STATUS_SUSPENDED,
    });
    expect(mapLegacyStatus(2)).toEqual({
      accountStatus: ACCOUNT_STATUS_DELETED,
      membershipStatus: MEMBERSHIP_STATUS_REVOKED,
    });
  });

  it('throws INVALID_STATUS for an unknown status value', () => {
    expect(() => mapLegacyStatus(99)).toThrow(LegacyMappingError);
  });
});

describe('verifyRfc4122Uuid', () => {
  it('returns the canonical lowercase form for an upper-case UUID', () => {
    expect(verifyRfc4122Uuid(SAMPLE_UUID_UPPER)).toBe(SAMPLE_UUID);
  });

  it('returns the same lowercase string when given a lowercase canonical UUID', () => {
    expect(verifyRfc4122Uuid(SAMPLE_UUID)).toBe(SAMPLE_UUID);
  });

  it('throws INVALID_INPUT for non-string or empty input', () => {
    expect(() => verifyRfc4122Uuid(null)).toThrow(LegacyMappingError);
    expect(() => verifyRfc4122Uuid(undefined)).toThrow(LegacyMappingError);
    expect(() => verifyRfc4122Uuid('')).toThrow(LegacyMappingError);
    expect(() => verifyRfc4122Uuid('   ')).toThrow(LegacyMappingError);
  });

  it('throws INVALID_INPUT for malformed UUIDs', () => {
    expect(() => verifyRfc4122Uuid('not-a-uuid')).toThrow(LegacyMappingError);
    expect(() => verifyRfc4122Uuid('11111111-1111-1111-1111-11111111111')).toThrow(
      LegacyMappingError,
    ); // 11 chars
    expect(() => verifyRfc4122Uuid('11111111-1111-1111-1111-1111111111111')).toThrow(
      LegacyMappingError,
    ); // 13 chars
    expect(() => verifyRfc4122Uuid('11111111-1111-0111-8111-111111111111')).toThrow(
      LegacyMappingError,
    ); // bad version
    expect(() => verifyRfc4122Uuid('11111111-1111-1111-0111-111111111111')).toThrow(
      LegacyMappingError,
    ); // bad variant
  });
});

describe('parseStrictDate / parseStrictInstant', () => {
  it('parses strict YYYY-MM-DD dates', () => {
    expect(parseStrictDate('2023-04-01', 'hireDate')).toBe('2023-04-01');
  });

  it('rejects dates with the wrong shape', () => {
    expect(() => parseStrictDate('2023/04/01', 'hireDate')).toThrow(LegacyMappingError);
    expect(() => parseStrictDate('01-04-2023', 'hireDate')).toThrow(LegacyMappingError);
    expect(() => parseStrictDate('2023-4-1', 'hireDate')).toThrow(LegacyMappingError);
  });

  it('rejects calendar-impossible dates (e.g. 2023-02-30)', () => {
    expect(() => parseStrictDate('2023-02-30', 'hireDate')).toThrow(LegacyMappingError);
  });

  it('returns null for null / undefined / empty / whitespace input', () => {
    expect(parseStrictDate(null, 'hireDate')).toBeNull();
    expect(parseStrictDate(undefined, 'hireDate')).toBeNull();
    expect(parseStrictDate('', 'hireDate')).toBeNull();
    expect(parseStrictDate('   ', 'hireDate')).toBeNull();
  });

  it('parses strict UTC instants (trailing Z)', () => {
    expect(parseStrictInstant('2023-01-01T00:00:00Z', 'createdDate')).toBe('2023-01-01T00:00:00Z');
    expect(parseStrictInstant('2023-01-01T12:34:56.789Z', 'createdDate')).toBe(
      '2023-01-01T12:34:56.789Z',
    );
  });

  it('rejects instants that carry a local-time offset', () => {
    expect(() => parseStrictInstant('2023-01-01T00:00:00+00:00', 'createdDate')).toThrow(
      LegacyMappingError,
    );
    expect(() => parseStrictInstant('2023-01-01T00:00:00-05:00', 'createdDate')).toThrow(
      LegacyMappingError,
    );
  });

  it('rejects malformed instants', () => {
    expect(() => parseStrictInstant('2023-01-01', 'createdDate')).toThrow(LegacyMappingError);
    expect(() => parseStrictInstant('2023-01-01T00:00:00', 'createdDate')).toThrow(
      LegacyMappingError,
    );
    expect(() => parseStrictInstant('not-a-date', 'createdDate')).toThrow(LegacyMappingError);
  });

  it('round-trips Date instances through the UTC helpers', () => {
    expect(dateToUtcDate(new Date('2023-04-01T00:00:00Z'), 'hireDate')).toBe('2023-04-01');
    expect(dateToUtcInstant(new Date('2023-01-01T00:00:00Z'), 'createdDate')).toBe(
      '2023-01-01T00:00:00.000Z',
    );
  });
});

describe('validateActorId', () => {
  it('returns null for null / undefined input', () => {
    expect(validateActorId(null, 'createdByLegacyId')).toBeNull();
    expect(validateActorId(undefined, 'createdByLegacyId')).toBeNull();
  });

  it('returns the same integer for valid nonnegative integers', () => {
    expect(validateActorId(0, 'createdByLegacyId')).toBe(0);
    expect(validateActorId(7, 'createdByLegacyId')).toBe(7);
  });

  it('throws INVALID_ACTOR for negative or non-integer actor ids', () => {
    expect(() => validateActorId(-1, 'createdByLegacyId')).toThrow(LegacyMappingError);
    expect(() => validateActorId(1.5, 'createdByLegacyId')).toThrow(LegacyMappingError);
  });
});

describe('end-to-end mapper', () => {
  const input = {
    legacyUserId: 42,
    username: CANARY.username,
    email: `  ${CANARY.email}`,
    firstName: CANARY.firstName,
    lastName: CANARY.lastName,
    secondLastName: '  ',
    identityCard: CANARY.identityCard,
    phoneNumber: CANARY.phone,
    passwordHash: '$2a$12$' + 'x'.repeat(53),
    roleValue: ROLE_ADMINISTRADOR,
    identityRoleName: 'Administrator',
    statusValue: 0,
    position: '  Tech Lead ',
    department: 'IT',
    hireDate: '2023-04-01',
    twoFactorEnabled: false,
    createdDate: new Date('2023-01-01T00:00:00Z'),
    createdByLegacyId: 7,
  };

  it('maps a bcrypt-protected active Administrador to a canonical row with mustChangePassword=false', () => {
    const out = mapLegacyUser(input, SAMPLE_UUID);
    expect(out.callerAllocatedUserId).toBe(SAMPLE_UUID);
    expect(out.sourceSystem).toBe(SOURCE_SYSTEM_ASP_SQLSERVER);
    expect(out.username).toBe(CANARY.username.toLowerCase());
    expect(out.normalizedEmail).toBe(CANARY.email.toLowerCase());
    expect(out.firstName).toBe(CANARY.firstName);
    expect(out.lastName).toBe(CANARY.lastName);
    expect(out.secondLastName).toBeNull();
    expect(out.fullName).toBe(`${CANARY.firstName} ${CANARY.lastName}`);
    expect(out.identityCard).toBe('AB-1234-CD');
    expect(out.phoneNumber).toBe(CANARY.phone);
    expect(out.passwordClassification.scheme).toBe(PASSWORD_SCHEME_BCRYPT);
    expect(out.mustChangePassword).toBe(false);
    expect(out.passwordMigratedAt).toBeNull();
    expect(out.accountStatus).toBe(ACCOUNT_STATUS_ACTIVE);
    expect(out.membershipStatus).toBe(MEMBERSHIP_STATUS_ACTIVE);
    expect(out.isSuperAdmin).toBe(false);
    expect(out.siteRole).toBe(ROLE_ADMINISTRADOR);
    expect(out.position).toBe('Tech Lead');
    expect(out.department).toBe('IT');
    expect(out.hireDate).toBe('2023-04-01');
    expect(out.createdDate).toBe('2023-01-01T00:00:00.000Z');
    expect(out.createdByLegacyId).toBe(7);
    expect(out.reconciliation).toBe('canonical');
    expect(out.reconcileReason).toBeNull();
  });

  it('preserves a valid Identity V2 hash and yields mustChangePassword=false', () => {
    const out = mapLegacyUser({ ...input, passwordHash: V2_PAYLOAD() }, SAMPLE_UUID);
    expect(out.passwordClassification.scheme).toBe(PASSWORD_SCHEME_LEGACY_V2);
    expect(out.mustChangePassword).toBe(false);
    expect(out.reconciliation).toBe('canonical');
  });

  it('preserves a valid Identity V3 hash and yields mustChangePassword=false', () => {
    const out = mapLegacyUser(
      { ...input, passwordHash: V3_PAYLOAD(2, 100_000, 24, 24) },
      SAMPLE_UUID,
    );
    expect(out.passwordClassification.scheme).toBe(PASSWORD_SCHEME_LEGACY_V3);
    expect(out.mustChangePassword).toBe(false);
    expect(out.reconciliation).toBe('canonical');
  });

  it('forces reset_required + mustChangePassword=true on a malformed hash', () => {
    const out = mapLegacyUser({ ...input, passwordHash: '@@@ not a valid hash @@@' }, SAMPLE_UUID);
    expect(out.passwordClassification.scheme).toBe(PASSWORD_SCHEME_RESET_REQUIRED);
    expect(out.mustChangePassword).toBe(true);
    expect(out.passwordMigratedAt).toBeNull();
    expect(out.reconciliation).toBe('canonical');
  });

  it('forces reset_required + mustChangePassword=true on a blank hash', () => {
    const out = mapLegacyUser({ ...input, passwordHash: '' }, SAMPLE_UUID);
    expect(out.passwordClassification.scheme).toBe(PASSWORD_SCHEME_RESET_REQUIRED);
    expect(out.mustChangePassword).toBe(true);
    expect(out.reconciliation).toBe('canonical');
  });

  it('forces reset_required when the canonical hash is whitespace-padded (opaque hash, no echo)', () => {
    const canonical = input.passwordHash;
    const out = mapLegacyUser({ ...input, passwordHash: ` ${canonical}` }, SAMPLE_UUID);
    expect(out.passwordClassification.scheme).toBe(PASSWORD_SCHEME_RESET_REQUIRED);
    expect(out.mustChangePassword).toBe(true);
    expect(out.reconciliation).toBe('canonical');
    const serialized = JSON.stringify(out);
    expect(serialized).not.toContain(canonical);
    expect(serialized).not.toContain(canonical.slice(0, 12));
  });

  it('forces reset_required when the hash carries internal whitespace', () => {
    const canonical = input.passwordHash;
    const broken = `${canonical.slice(0, 6)} ${canonical.slice(6)}`;
    const out = mapLegacyUser({ ...input, passwordHash: broken }, SAMPLE_UUID);
    expect(out.passwordClassification.scheme).toBe(PASSWORD_SCHEME_RESET_REQUIRED);
    expect(out.mustChangePassword).toBe(true);
    expect(out.reconciliation).toBe('canonical');
    const serialized = JSON.stringify(out);
    expect(serialized).not.toContain(broken);
  });

  it('forces reset_required when TwoFactorEnabled=true even if a valid bcrypt is present', () => {
    const out = mapLegacyUser({ ...input, twoFactorEnabled: true }, SAMPLE_UUID);
    expect(out.passwordClassification.scheme).toBe(PASSWORD_SCHEME_RESET_REQUIRED);
    expect(out.twoFactorResetRequired).toBe(true);
    expect(out.mustChangePassword).toBe(true);
    expect(out.reconciliation).toBe('canonical');
  });

  it('returns the canonical lowercase UUID even when the caller passes upper-case', () => {
    const out = mapLegacyUser(input, SAMPLE_UUID_UPPER);
    expect(out.callerAllocatedUserId).toBe(SAMPLE_UUID);
    expect(out.reconciliation).toBe('canonical');
  });
});

describe('typed blockers', () => {
  const base = {
    legacyUserId: 1,
    username: CANARY.username,
    email: CANARY.email,
    firstName: CANARY.firstName,
    lastName: CANARY.lastName,
    secondLastName: null,
    identityCard: CANARY.identityCard,
    phoneNumber: CANARY.phone,
    passwordHash: '$2a$12$' + 'x'.repeat(53),
    roleValue: ROLE_ADMINISTRADOR,
    identityRoleName: 'Administrator',
    statusValue: 0,
  };

  it('throws INVALID_LEGACY_USER_ID for negative or non-integer ids', () => {
    expect(() => mapLegacyUser({ ...base, legacyUserId: -1 }, SAMPLE_UUID)).toThrow(
      LegacyMappingError,
    );
    expect(() => mapLegacyUser({ ...base, legacyUserId: 1.5 }, SAMPLE_UUID)).toThrow(
      LegacyMappingError,
    );
  });

  it('throws INVALID_INPUT for non-RFC4122 caller UUIDs', () => {
    expect(() => mapLegacyUser(base, '')).toThrow(LegacyMappingError);
    expect(() => mapLegacyUser(base, 'not-a-uuid')).toThrow(LegacyMappingError);
    expect(() => mapLegacyUser(base, '11111111-1111-1111-1111-11111111111')).toThrow(
      LegacyMappingError,
    );
  });

  it('throws INVALID_IDENTIFIER when the username is missing', () => {
    expect(() => mapLegacyUser({ ...base, username: null }, SAMPLE_UUID)).toThrow(
      LegacyMappingError,
    );
    expect(() => mapLegacyUser({ ...base, username: '   ' }, SAMPLE_UUID)).toThrow(
      LegacyMappingError,
    );
  });

  it('throws INVALID_IDENTIFIER when the email is missing', () => {
    expect(() => mapLegacyUser({ ...base, email: null }, SAMPLE_UUID)).toThrow(LegacyMappingError);
    expect(() => mapLegacyUser({ ...base, email: '   ' }, SAMPLE_UUID)).toThrow(LegacyMappingError);
  });

  it('throws INVALID_NAME when firstName or lastName is missing', () => {
    expect(() => mapLegacyUser({ ...base, firstName: null }, SAMPLE_UUID)).toThrow(
      LegacyMappingError,
    );
    expect(() => mapLegacyUser({ ...base, firstName: '   ' }, SAMPLE_UUID)).toThrow(
      LegacyMappingError,
    );
    expect(() => mapLegacyUser({ ...base, lastName: null }, SAMPLE_UUID)).toThrow(
      LegacyMappingError,
    );
    expect(() => mapLegacyUser({ ...base, lastName: '   ' }, SAMPLE_UUID)).toThrow(
      LegacyMappingError,
    );
  });

  it('throws INVALID_IDENTIFIER when the identity card is missing or invalid', () => {
    expect(() => mapLegacyUser({ ...base, identityCard: null }, SAMPLE_UUID)).toThrow(
      LegacyMappingError,
    );
    expect(() => mapLegacyUser({ ...base, identityCard: '!@#bad' }, SAMPLE_UUID)).toThrow(
      LegacyMappingError,
    );
    expect(() => mapLegacyUser({ ...base, identityCard: 'AB-12345678-CD' }, SAMPLE_UUID)).toThrow(
      LegacyMappingError,
    );
  });

  it('throws INVALID_PHONE when the phone is missing or exceeds the 30-char bound', () => {
    expect(() => mapLegacyUser({ ...base, phoneNumber: null }, SAMPLE_UUID)).toThrow(
      LegacyMappingError,
    );
    expect(() => mapLegacyUser({ ...base, phoneNumber: '   ' }, SAMPLE_UUID)).toThrow(
      LegacyMappingError,
    );
    expect(() =>
      mapLegacyUser({ ...base, phoneNumber: '+' + '1'.repeat(PHONE_MAX_LENGTH + 1) }, SAMPLE_UUID),
    ).toThrow(LegacyMappingError);
  });

  it('throws INVALID_ACTOR for negative actor ids', () => {
    expect(() => mapLegacyUser({ ...base, createdByLegacyId: -2 }, SAMPLE_UUID)).toThrow(
      LegacyMappingError,
    );
    expect(() => mapLegacyUser({ ...base, modifiedByLegacyId: -1 }, SAMPLE_UUID)).toThrow(
      LegacyMappingError,
    );
  });

  it('throws INVALID_ROLE when the managed Identity role is missing', () => {
    expect(() => mapLegacyUser({ ...base, identityRoleName: null }, SAMPLE_UUID)).toThrow(
      LegacyMappingError,
    );
    expect(() => mapLegacyUser({ ...base, identityRoleName: '' }, SAMPLE_UUID)).toThrow(
      LegacyMappingError,
    );
  });

  it('throws ROLE_MISMATCH when the managed Identity role disagrees with the enum', () => {
    expect(() =>
      mapLegacyUser({ ...base, roleValue: 1, identityRoleName: 'Supervisor' }, SAMPLE_UUID),
    ).toThrow(LegacyMappingError);
  });

  it('throws INVALID_STATUS for an unknown legacy status', () => {
    expect(() => mapLegacyUser({ ...base, statusValue: 99 }, SAMPLE_UUID)).toThrow(
      LegacyMappingError,
    );
  });

  it('throws INVALID_DATE on a sliced or non-UTC date string', () => {
    expect(() => mapLegacyUser({ ...base, hireDate: '2023/04/01' }, SAMPLE_UUID)).toThrow(
      LegacyMappingError,
    );
    expect(() =>
      mapLegacyUser({ ...base, createdDate: '2023-01-01T00:00:00+00:00' }, SAMPLE_UUID),
    ).toThrow(LegacyMappingError);
    // The previous W3 implementation silently sliced `.slice(0, 10)` here; the
    // current contract must reject instead.
    expect(() => mapLegacyUser({ ...base, hireDate: '2023-04-01T12:34:56Z' }, SAMPLE_UUID)).toThrow(
      LegacyMappingError,
    );
  });
});

describe('error messages do not leak raw PII or the opaque hash', () => {
  const base = {
    legacyUserId: 7,
    username: CANARY.username,
    email: CANARY.email,
    firstName: CANARY.firstName,
    lastName: CANARY.lastName,
    secondLastName: null,
    identityCard: CANARY.identityCard,
    phoneNumber: CANARY.phone,
    passwordHash: '$2a$12$' + 'X'.repeat(53),
    roleValue: ROLE_ADMINISTRADOR,
    identityRoleName: 'Administrator',
    statusValue: 0,
  };

  const forbidden = [
    CANARY.username,
    CANARY.email,
    CANARY.firstName,
    CANARY.lastName,
    CANARY.identityCard,
    CANARY.phone,
    '$2a$12$' + 'X'.repeat(53),
  ];

  function assertNoPiiOrHash(err: unknown): void {
    expect(err).toBeInstanceOf(LegacyMappingError);
    const message = (err as Error).message;
    for (const needle of forbidden) {
      expect(message).not.toContain(needle);
    }
    // Sanity: legacyUserId (a non-PII integer) is allowed to appear for traceability.
    expect(message).not.toMatch(/<.*?>/);
    expect(message).not.toContain('***');
  }

  it('does not leak PII or the hash on missing username', () => {
    try {
      mapLegacyUser({ ...base, username: null }, SAMPLE_UUID);
      throw new Error('expected throw');
    } catch (err) {
      assertNoPiiOrHash(err);
    }
  });

  it('does not leak PII or the hash on missing email', () => {
    try {
      mapLegacyUser({ ...base, email: null }, SAMPLE_UUID);
      throw new Error('expected throw');
    } catch (err) {
      assertNoPiiOrHash(err);
    }
  });

  it('does not leak PII or the hash on missing identity card', () => {
    try {
      mapLegacyUser({ ...base, identityCard: null }, SAMPLE_UUID);
      throw new Error('expected throw');
    } catch (err) {
      assertNoPiiOrHash(err);
    }
  });

  it('does not leak PII or the hash on missing first name', () => {
    try {
      mapLegacyUser({ ...base, firstName: null }, SAMPLE_UUID);
      throw new Error('expected throw');
    } catch (err) {
      assertNoPiiOrHash(err);
    }
  });

  it('does not leak PII or the hash on missing last name', () => {
    try {
      mapLegacyUser({ ...base, lastName: null }, SAMPLE_UUID);
      throw new Error('expected throw');
    } catch (err) {
      assertNoPiiOrHash(err);
    }
  });

  it('does not leak PII or the hash on missing phone', () => {
    try {
      mapLegacyUser({ ...base, phoneNumber: null }, SAMPLE_UUID);
      throw new Error('expected throw');
    } catch (err) {
      assertNoPiiOrHash(err);
    }
  });

  it('does not leak PII or the hash on a malformed RFC4122 UUID', () => {
    try {
      mapLegacyUser(base, 'not-a-uuid');
      throw new Error('expected throw');
    } catch (err) {
      assertNoPiiOrHash(err);
    }
  });

  it('does not leak PII or the hash on missing managed Identity role', () => {
    try {
      mapLegacyUser({ ...base, identityRoleName: null }, SAMPLE_UUID);
      throw new Error('expected throw');
    } catch (err) {
      assertNoPiiOrHash(err);
    }
  });

  it('does not leak PII or the hash on contradictory role', () => {
    try {
      mapLegacyUser({ ...base, roleValue: 1, identityRoleName: 'Supervisor' }, SAMPLE_UUID);
      throw new Error('expected throw');
    } catch (err) {
      assertNoPiiOrHash(err);
    }
  });

  it('does not leak PII or the hash on invalid status', () => {
    try {
      mapLegacyUser({ ...base, statusValue: 99 }, SAMPLE_UUID);
      throw new Error('expected throw');
    } catch (err) {
      assertNoPiiOrHash(err);
    }
  });
});

describe('decodeLegacyIdentityBase64', () => {
  it('rejects malformed base64', () => {
    expect(decodeLegacyIdentityBase64('@@@')).toBeNull();
    expect(decodeLegacyIdentityBase64(null)).toBeNull();
    expect(decodeLegacyIdentityBase64(undefined)).toBeNull();
  });

  it('accepts canonical ASP.NET Identity base64', () => {
    const raw = Buffer.from([MARKER_IDENTITY_V2]).toString('base64');
    expect(decodeLegacyIdentityBase64(raw)).not.toBeNull();
  });
});

// ---------------------------------------------------------------------------
// F1/W14A boundary + adversarial tests.
// ---------------------------------------------------------------------------

describe('F1/W14A decodeLegacyIdentityBase64 (canonical STANDARD)', () => {
  it('rejects a length that is not a multiple of 4', () => {
    expect(decodeLegacyIdentityBase64('A')).toBeNull();
    expect(decodeLegacyIdentityBase64('AB')).toBeNull(); // missing padding
    expect(decodeLegacyIdentityBase64('ABC')).toBeNull();
    expect(decodeLegacyIdentityBase64('ABCDE')).toBeNull(); // length 5
  });

  it('rejects excess padding (`===`, `====`)', () => {
    expect(decodeLegacyIdentityBase64('YB===')).toBeNull();
    expect(decodeLegacyIdentityBase64('YB====')).toBeNull();
  });

  it('rejects URL-safe alphabet (`-`, `_`)', () => {
    expect(decodeLegacyIdentityBase64('Y-_=')).toBeNull(); // URL-safe
    expect(decodeLegacyIdentityBase64('Y-_=')).toBeNull();
  });

  it('rejects internal whitespace', () => {
    expect(decodeLegacyIdentityBase64('Y B=')).toBeNull(); // inner space
    expect(decodeLegacyIdentityBase64('YB =')).toBeNull(); // space inside trim
  });

  it('rejects leading whitespace (no trim, opaque hash)', () => {
    expect(decodeLegacyIdentityBase64(' AA==')).toBeNull();
    expect(decodeLegacyIdentityBase64('   AA==')).toBeNull();
    expect(decodeLegacyIdentityBase64('\tAA==')).toBeNull();
    expect(decodeLegacyIdentityBase64('\nAA==')).toBeNull();
  });

  it('rejects trailing whitespace (no trim, opaque hash)', () => {
    expect(decodeLegacyIdentityBase64('AA== ')).toBeNull();
    expect(decodeLegacyIdentityBase64('AA==\n')).toBeNull();
    expect(decodeLegacyIdentityBase64('AA==\t')).toBeNull();
    expect(decodeLegacyIdentityBase64('AA==   ')).toBeNull();
  });

  it('rejects whitespace-only input (blank payload after strip)', () => {
    expect(decodeLegacyIdentityBase64(' ')).toBeNull();
    expect(decodeLegacyIdentityBase64('   ')).toBeNull();
    expect(decodeLegacyIdentityBase64('\n')).toBeNull();
    expect(decodeLegacyIdentityBase64('\t\t')).toBeNull();
  });

  it('rejects multi-line whitespace inside the value', () => {
    expect(decodeLegacyIdentityBase64('AA\n==')).toBeNull();
    expect(decodeLegacyIdentityBase64('AA==\nAA==')).toBeNull();
    expect(decodeLegacyIdentityBase64('A\nA==')).toBeNull();
  });

  it('rejects noncanonical trailing bits in the last data character', () => {
    // AA== is canonical (encodes 0x00). AB== is noncanonical and must be
    // rejected via the canonical-roundtrip check (Buffer re-encodes to AA==).
    expect(decodeLegacyIdentityBase64('AB==')).toBeNull();
    // EX== with X=E is noncanonical (the last data char carries non-zero
    // padding bits); Buffer re-encodes to EA==.
    expect(decodeLegacyIdentityBase64('AE==')).toBeNull();
    // AE= (1-char pad, length 4 OK, last data char E=4, low 2 bits = 00
    // (4%4==0) so technically trailing-bits-canonical; but the roundtrip
    // forces AA== which doesn't match). Buffers agrees on canonical form.
  });

  it('rejects mid-string `=` padding', () => {
    expect(decodeLegacyIdentityBase64('A=AA')).toBeNull();
    expect(decodeLegacyIdentityBase64('AA=A')).toBeNull();
  });

  it('accepts canonical 4-char canonical with 1 and 2 padding chars', () => {
    expect(decodeLegacyIdentityBase64('AA==')).not.toBeNull(); // 1 byte
    expect(decodeLegacyIdentityBase64('AAA=')).not.toBeNull(); // 2 bytes
    expect(decodeLegacyIdentityBase64('AAAA')).not.toBeNull(); // 3 bytes (no pad)
    expect(decodeLegacyIdentityBase64('AABA')).not.toBeNull();
  });

  it('round-trips ASP.NET V2 and V3 payloads through the canonical decoder', () => {
    // V2_PAYLOAD and V3_PAYLOAD outputs are canonical by construction.
    const v2 = decodeLegacyIdentityBase64(V2_PAYLOAD());
    expect(v2).not.toBeNull();
    expect(v2?.bytesLength).toBe(49);

    const v3 = decodeLegacyIdentityBase64(V3_PAYLOAD(2, 100_000, 24, 24));
    expect(v3).not.toBeNull();
    expect(v3?.bytesLength).toBe(61);
  });
});

describe('F1/W14A codePointLength', () => {
  it('counts BMP chars as one each', () => {
    expect(codePointLength('')).toBe(0);
    expect(codePointLength('a')).toBe(1);
    expect(codePointLength('abc')).toBe(3);
    // é is U+00E9 (single BMP codepoint)
    expect(codePointLength('é')).toBe(1);
    expect(codePointLength('café')).toBe(4);
  });

  it('counts astral code points as one (not UTF-16 surrogate pairs)', () => {
    const smiley = '\u{1F600}'; // 😀
    expect(smiley.length).toBe(2); // UTF-16 units
    expect(codePointLength(smiley)).toBe(1); // code points
    expect(codePointLength(smiley.repeat(50))).toBe(50);
    expect(codePointLength(smiley.repeat(50)).valueOf()).toBe(50);
  });
});

describe('F1/W14A PG-aligned character bounds', () => {
  // Use a canary unrelated to the PII canary set above; these are exact
  // boundary strings only.
  const mk = (
    s: string,
    ns: { username?: string; email?: string; first?: string; last?: string; second?: string } = {},
  ) => ({
    legacyUserId: 1,
    username: ns.username ?? CANARY.username,
    email: ns.email ?? CANARY.email,
    firstName: ns.first ?? CANARY.firstName,
    lastName: ns.last ?? CANARY.lastName,
    secondLastName: ns.second ?? null,
    identityCard: CANARY.identityCard,
    phoneNumber: CANARY.phone,
    passwordHash: '$2a$12$' + 'x'.repeat(53),
    roleValue: ROLE_ADMINISTRADOR,
    identityRoleName: 'Administrator',
    statusValue: 0,
    position: 'Engineer',
    department: 'IT',
  });

  it('accepts a 256-code-point username and rejects a 257-code-point one', () => {
    const user256 = 'u'.repeat(USERNAME_MAX_LENGTH);
    const user257 = 'u'.repeat(USERNAME_MAX_LENGTH + 1);
    expect(() => mapLegacyUser(mk('valid', { username: user256 }), SAMPLE_UUID)).not.toThrow();
    expect(() => mapLegacyUser(mk('valid', { username: user257 }), SAMPLE_UUID)).toThrow(
      LegacyMappingError,
    );
  });

  it('accepts a 256-code-point astral username and rejects a 257-code-point one', () => {
    const smiley = '\u{1F600}';
    const user256 = smiley.repeat(USERNAME_MAX_LENGTH); // 256 code points
    const user257 = smiley.repeat(USERNAME_MAX_LENGTH + 1);
    expect(codePointLength(user256)).toBe(USERNAME_MAX_LENGTH);
    expect(codePointLength(user257)).toBe(USERNAME_MAX_LENGTH + 1);
    expect(() => mapLegacyUser(mk('valid', { username: user256 }), SAMPLE_UUID)).not.toThrow();
    expect(() => mapLegacyUser(mk('valid', { username: user257 }), SAMPLE_UUID)).toThrow(
      LegacyMappingError,
    );
  });

  it('accepts a 256-code-point email and rejects a 257-code-point one', () => {
    // For an email we need it to be syntactically valid AND within 256
    // code points. Domain `@x.io` adds 5 code points (`@`, `x`, `.`, `i`, `o`),
    // so the local part must be 251 chars (= 256 - 5) for the email itself
    // to be exactly 256 code points.
    const local256 = 'a'.repeat(EMAIL_MAX_LENGTH - 5);
    const email256 = `${local256}@x.io`;
    expect(codePointLength(email256)).toBe(EMAIL_MAX_LENGTH);
    const email257 = `a${email256}`;
    expect(codePointLength(email257)).toBe(EMAIL_MAX_LENGTH + 1);
    expect(() => mapLegacyUser(mk('valid', { email: email256 }), SAMPLE_UUID)).not.toThrow();
    expect(() => mapLegacyUser(mk('valid', { email: email257 }), SAMPLE_UUID)).toThrow(
      LegacyMappingError,
    );
  });

  it('accepts a 100-code-point first name and rejects a 101-code-point one', () => {
    const n100 = 'a'.repeat(NAME_MAX_LENGTH);
    const n101 = 'a'.repeat(NAME_MAX_LENGTH + 1);
    expect(() => mapLegacyUser(mk('valid', { first: n100 }), SAMPLE_UUID)).not.toThrow();
    expect(() => mapLegacyUser(mk('valid', { first: n101 }), SAMPLE_UUID)).toThrow(
      LegacyMappingError,
    );
  });

  it('accepts an astral first name of 100 code points and rejects 101', () => {
    const smiley = '\u{1F600}';
    const n100 = smiley.repeat(NAME_MAX_LENGTH);
    const n101 = smiley.repeat(NAME_MAX_LENGTH + 1);
    expect(codePointLength(n100)).toBe(NAME_MAX_LENGTH);
    expect(codePointLength(n101)).toBe(NAME_MAX_LENGTH + 1);
    expect(() => mapLegacyUser(mk('valid', { first: n100 }), SAMPLE_UUID)).not.toThrow();
    expect(() => mapLegacyUser(mk('valid', { first: n101 }), SAMPLE_UUID)).toThrow(
      LegacyMappingError,
    );
  });

  it('accepts a 100-code-point last name and rejects a 101-code-point one', () => {
    const n100 = 'a'.repeat(NAME_MAX_LENGTH);
    const n101 = 'a'.repeat(NAME_MAX_LENGTH + 1);
    expect(() => mapLegacyUser(mk('valid', { last: n100 }), SAMPLE_UUID)).not.toThrow();
    expect(() => mapLegacyUser(mk('valid', { last: n101 }), SAMPLE_UUID)).toThrow(
      LegacyMappingError,
    );
  });

  it('accepts a 100-code-point secondLastName and rejects a 101-code-point one', () => {
    const n100 = 'a'.repeat(NAME_MAX_LENGTH);
    const n101 = 'a'.repeat(NAME_MAX_LENGTH + 1);
    expect(() => mapLegacyUser(mk('valid', { second: n100 }), SAMPLE_UUID)).not.toThrow();
    expect(() => mapLegacyUser(mk('valid', { second: n101 }), SAMPLE_UUID)).toThrow(
      LegacyMappingError,
    );
  });
});

describe('F1/W14A optional position / department bounds', () => {
  const base = {
    legacyUserId: 1,
    username: CANARY.username,
    email: CANARY.email,
    firstName: CANARY.firstName,
    lastName: CANARY.lastName,
    secondLastName: null,
    identityCard: CANARY.identityCard,
    phoneNumber: CANARY.phone,
    passwordHash: '$2a$12$' + 'x'.repeat(53),
    roleValue: ROLE_ADMINISTRADOR,
    identityRoleName: 'Administrator',
    statusValue: 0,
  };

  it('keeps position/department null when missing or whitespace-only', () => {
    const out = mapLegacyUser({ ...base, position: null, department: '   ' }, SAMPLE_UUID);
    expect(out.position).toBeNull();
    expect(out.department).toBeNull();
    expect(out.reconciliation).toBe('canonical');
  });

  it('accepts 100-code-point position/department', () => {
    const v100 = 'a'.repeat(OPTIONAL_NAME_MAX_LENGTH);
    const out = mapLegacyUser({ ...base, position: v100, department: v100 }, SAMPLE_UUID);
    expect(out.position?.length).toBe(100);
    expect(out.department?.length).toBe(100);
    expect(out.reconciliation).toBe('canonical');
  });

  it('accepts a 100-astral-code-point position', () => {
    const smiley = '\u{1F600}';
    const out = mapLegacyUser(
      { ...base, position: smiley.repeat(OPTIONAL_NAME_MAX_LENGTH) },
      SAMPLE_UUID,
    );
    expect(codePointLength(out.position ?? '')).toBe(OPTIONAL_NAME_MAX_LENGTH);
    expect(out.reconciliation).toBe('canonical');
  });

  it('rejects 101-code-point position/department', () => {
    const v101 = 'a'.repeat(OPTIONAL_NAME_MAX_LENGTH + 1);
    expect(() => mapLegacyUser({ ...base, position: v101 }, SAMPLE_UUID)).toThrow(
      LegacyMappingError,
    );
    expect(() => mapLegacyUser({ ...base, department: v101 }, SAMPLE_UUID)).toThrow(
      LegacyMappingError,
    );
  });
});

describe('F1/W14A email syntax validator', () => {
  const base = {
    legacyUserId: 1,
    username: CANARY.username,
    email: CANARY.email,
    firstName: CANARY.firstName,
    lastName: CANARY.lastName,
    secondLastName: null,
    identityCard: CANARY.identityCard,
    phoneNumber: CANARY.phone,
    passwordHash: '$2a$12$' + 'x'.repeat(53),
    roleValue: ROLE_ADMINISTRADOR,
    identityRoleName: 'Administrator',
    statusValue: 0,
  };

  it('rejects a missing @', () => {
    expect(() => mapLegacyUser({ ...base, email: 'foo.example.com' }, SAMPLE_UUID)).toThrow(
      LegacyMappingError,
    );
  });

  it('rejects a domain-less (no-dot) address', () => {
    expect(() => mapLegacyUser({ ...base, email: 'foo@bar' }, SAMPLE_UUID)).toThrow(
      LegacyMappingError,
    );
  });

  it('rejects multiple @ separators', () => {
    expect(() => mapLegacyUser({ ...base, email: 'foo@bar@baz.com' }, SAMPLE_UUID)).toThrow(
      LegacyMappingError,
    );
  });

  it('rejects an empty domain (trailing @)', () => {
    expect(() => mapLegacyUser({ ...base, email: 'foo@' }, SAMPLE_UUID)).toThrow(
      LegacyMappingError,
    );
  });

  it('rejects an empty local-part (leading @)', () => {
    expect(() => mapLegacyUser({ ...base, email: '@bar.com' }, SAMPLE_UUID)).toThrow(
      LegacyMappingError,
    );
  });

  it('rejects a whitespace character inside the address', () => {
    expect(() => mapLegacyUser({ ...base, email: 'foo bar@example.com' }, SAMPLE_UUID)).toThrow(
      LegacyMappingError,
    );
  });

  it('rejects a control character inside the address', () => {
    expect(() => mapLegacyUser({ ...base, email: 'foo\t@example.com' }, SAMPLE_UUID)).toThrow(
      LegacyMappingError,
    );
    expect(() => mapLegacyUser({ ...base, email: 'foo\x00bar@example.com' }, SAMPLE_UUID)).toThrow(
      LegacyMappingError,
    );
  });

  it('rejects a domain with an empty TLD (trailing `.`)', () => {
    expect(() => mapLegacyUser({ ...base, email: 'foo@example.' }, SAMPLE_UUID)).toThrow(
      LegacyMappingError,
    );
  });

  it('accepts a dot-qualified, single-@ address and preserves the normalized canonical value', () => {
    const out = mapLegacyUser({ ...base, email: '  Foo.Bar+tag@Example.COM  ' }, SAMPLE_UUID);
    expect(out.email).toBe('foo.bar+tag@example.com');
    expect(out.normalizedEmail).toBe('foo.bar+tag@example.com');
    expect(out.reconciliation).toBe('canonical');
  });
});

describe('F1/W14A phone syntax validator (validatePhoneSyntax)', () => {
  it('accepts a canonical phone with one leading + and the ASCII charset', () => {
    expect(() => validatePhoneSyntax('+59170012345')).not.toThrow();
    expect(() => validatePhoneSyntax('+591 700-12345')).not.toThrow();
    expect(() => validatePhoneSyntax('+1 (415) 555-2671')).not.toThrow();
    expect(() => validatePhoneSyntax('+591.700.12345')).not.toThrow();
  });

  it('accepts the 15-digit upper boundary', () => {
    expect(() => validatePhoneSyntax('+123456789012345')).not.toThrow(); // 15 digits
  });

  it('rejects letters', () => {
    expect(() => validatePhoneSyntax('+591abc12345')).toThrow(LegacyMappingError);
  });

  it('rejects a second + (double plus)', () => {
    expect(() => validatePhoneSyntax('++59170012345')).toThrow(LegacyMappingError);
    expect(() => validatePhoneSyntax('+591+70012345')).toThrow(LegacyMappingError);
  });

  it('rejects a non-ASCII plus (full-width / superscript)', () => {
    expect(() => validatePhoneSyntax('\uff0b59170012345')).toThrow(LegacyMappingError);
  });

  it('rejects controls and non-ASCII digits', () => {
    expect(() => validatePhoneSyntax('+591\t70012345')).toThrow(LegacyMappingError);
    expect(() => validatePhoneSyntax('+591\x0070012345')).toThrow(LegacyMappingError);
    // Arabic-Indic 5 (\u0665) is not in ASCII 0-9.
    expect(() => validatePhoneSyntax('+591\u0665700123450')).toThrow(LegacyMappingError);
  });

  it('rejects non-ASCII separators', () => {
    // full-width space U+3000 is not ASCII whitespace
    expect(() => validatePhoneSyntax('+591\u3000700123450')).toThrow(LegacyMappingError);
  });

  it('rejects too few (6 or fewer) digits', () => {
    expect(() => validatePhoneSyntax('+123456')).toThrow(LegacyMappingError); // 6 digits
    expect(() => validatePhoneSyntax('123456')).toThrow(LegacyMappingError); // 6 digits, no +
    expect(() => validatePhoneSyntax('+12345')).toThrow(LegacyMappingError); // 5 digits
  });

  it('accepts exactly 7 digits', () => {
    expect(() => validatePhoneSyntax('1234567')).not.toThrow();
  });

  it('rejects 16+ digits', () => {
    expect(() => validatePhoneSyntax('+1234567890123456')).toThrow(LegacyMappingError);
    expect(() => validatePhoneSyntax('1234567890123456')).toThrow(LegacyMappingError);
  });

  it('rejects unbalanced parentheses', () => {
    expect(() => validatePhoneSyntax('(+591 70012345')).toThrow(LegacyMappingError);
    expect(() => validatePhoneSyntax('(591) 70012345)')).toThrow(LegacyMappingError);
    expect(() => validatePhoneSyntax(')591( 70012345')).toThrow(LegacyMappingError);
  });

  it('rejects empty / non-string input', () => {
    expect(() => validatePhoneSyntax('')).toThrow(LegacyMappingError);
    expect(() => validatePhoneSyntax(null as unknown as string)).toThrow(LegacyMappingError);
    expect(() => validatePhoneSyntax(undefined as unknown as string)).toThrow(LegacyMappingError);
  });

  it('preserves the trimmed formatting (separators + leading +) on the mapped value', () => {
    const base = {
      legacyUserId: 1,
      username: CANARY.username,
      email: CANARY.email,
      firstName: CANARY.firstName,
      lastName: CANARY.lastName,
      secondLastName: null,
      identityCard: CANARY.identityCard,
      passwordHash: '$2a$12$' + 'x'.repeat(53),
      roleValue: ROLE_ADMINISTRADOR,
      identityRoleName: 'Administrator',
      statusValue: 0,
    };
    const out = mapLegacyUser({ ...base, phoneNumber: '+591 700-12345' }, SAMPLE_UUID);
    expect(out.phoneNumber).toBe('+591 700-12345');
    expect(out.reconciliation).toBe('canonical');
  });
});

describe('F1/W14A bcrypt cost boundaries', () => {
  it('accepts cost 04 (lower boundary) for $2a/$2b/$2y', () => {
    expect(classifyBcryptHash('$2a$04$' + 'x'.repeat(53)).scheme).toBe(PASSWORD_SCHEME_BCRYPT);
    expect(classifyBcryptHash('$2b$04$' + 'x'.repeat(53)).scheme).toBe(PASSWORD_SCHEME_BCRYPT);
    expect(classifyBcryptHash('$2y$04$' + 'x'.repeat(53)).scheme).toBe(PASSWORD_SCHEME_BCRYPT);
  });

  it('accepts cost 31 (upper boundary) for $2a/$2b/$2y', () => {
    expect(classifyBcryptHash('$2a$31$' + 'x'.repeat(53)).scheme).toBe(PASSWORD_SCHEME_BCRYPT);
    expect(classifyBcryptHash('$2b$31$' + 'x'.repeat(53)).scheme).toBe(PASSWORD_SCHEME_BCRYPT);
    expect(classifyBcryptHash('$2y$31$' + 'x'.repeat(53)).scheme).toBe(PASSWORD_SCHEME_BCRYPT);
  });

  it('rejects cost 03 (below lower boundary)', () => {
    expect(classifyBcryptHash('$2a$03$' + 'x'.repeat(53)).scheme).toBe(
      PASSWORD_SCHEME_RESET_REQUIRED,
    );
  });

  it('rejects cost 32 (above upper boundary)', () => {
    expect(classifyBcryptHash('$2a$32$' + 'x'.repeat(53)).scheme).toBe(
      PASSWORD_SCHEME_RESET_REQUIRED,
    );
  });

  it('rejects unknown bcrypt versions ($2x, $2c, $2z, $3a)', () => {
    expect(classifyBcryptHash('$2x$10$' + 'x'.repeat(53)).scheme).toBe(
      PASSWORD_SCHEME_RESET_REQUIRED,
    );
    expect(classifyBcryptHash('$2c$10$' + 'x'.repeat(53)).scheme).toBe(
      PASSWORD_SCHEME_RESET_REQUIRED,
    );
    expect(classifyBcryptHash('$2z$10$' + 'x'.repeat(53)).scheme).toBe(
      PASSWORD_SCHEME_RESET_REQUIRED,
    );
    expect(classifyBcryptHash('$3a$10$' + 'x'.repeat(53)).scheme).toBe(
      PASSWORD_SCHEME_RESET_REQUIRED,
    );
  });
});

describe('F1/W14A error messages do not leak PII for new validators', () => {
  const base = {
    legacyUserId: 9,
    username: CANARY.username,
    email: CANARY.email,
    firstName: CANARY.firstName,
    lastName: CANARY.lastName,
    secondLastName: null,
    identityCard: CANARY.identityCard,
    phoneNumber: CANARY.phone,
    passwordHash: '$2a$12$' + 'X'.repeat(53),
    roleValue: ROLE_ADMINISTRADOR,
    identityRoleName: 'Administrator',
    statusValue: 0,
  };

  function assertNoPii(err: unknown, extraForbidden: readonly string[] = []): void {
    expect(err).toBeInstanceOf(LegacyMappingError);
    const message = (err as Error).message;
    const needles = [
      CANARY.username,
      CANARY.email,
      CANARY.firstName,
      CANARY.lastName,
      CANARY.identityCard,
      CANARY.phone,
      '$2a$12$' + 'X'.repeat(53),
      ...extraForbidden,
    ];
    for (const needle of needles) {
      expect(message).not.toContain(needle);
    }
    expect(message).not.toMatch(/<.*?>/);
    expect(message).not.toContain('***');
  }

  it('does not echo the bad email on INVALID_IDENTIFIER (missing @)', () => {
    try {
      mapLegacyUser({ ...base, email: 'plain.example.com' }, SAMPLE_UUID);
      throw new Error('expected throw');
    } catch (err) {
      assertNoPii(err, ['plain.example.com']);
    }
  });

  it('does not echo the oversized username on INVALID_IDENTIFIER', () => {
    try {
      mapLegacyUser({ ...base, username: 'u'.repeat(USERNAME_MAX_LENGTH + 1) }, SAMPLE_UUID);
      throw new Error('expected throw');
    } catch (err) {
      assertNoPii(err);
    }
  });

  it('does not echo the oversized name on INVALID_NAME', () => {
    try {
      mapLegacyUser({ ...base, firstName: 'a'.repeat(NAME_MAX_LENGTH + 1) }, SAMPLE_UUID);
      throw new Error('expected throw');
    } catch (err) {
      assertNoPii(err);
    }
  });

  it('does not echo the bad phone on INVALID_PHONE', () => {
    try {
      mapLegacyUser({ ...base, phoneNumber: '+abc' }, SAMPLE_UUID);
      throw new Error('expected throw');
    } catch (err) {
      assertNoPii(err, ['+abc']);
    }
  });

  it('does not echo the oversized position on INVALID_INPUT', () => {
    try {
      mapLegacyUser({ ...base, position: 'p'.repeat(OPTIONAL_NAME_MAX_LENGTH + 1) }, SAMPLE_UUID);
      throw new Error('expected throw');
    } catch (err) {
      assertNoPii(err);
    }
  });
});
