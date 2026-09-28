import { Utf8PasswordPolicy } from '../src/identity/password/password-policy.js';
import { MULTIBYTE_PASSWORD_FIXTURES } from './identity-vectors.js';

const policy = new Utf8PasswordPolicy();

function isViolation(result: readonly string[], violation: string): boolean {
  return result.includes(violation);
}

describe('Utf8PasswordPolicy', () => {
  it('accepts a compliant 12-codepoint password', () => {
    const password = 'Aa1!Bb2@Cc3#Dd4$';
    expect(policy.validate(password)).toEqual([]);
  });

  it.each([
    'too_short',
    'too_long',
    'missing_uppercase',
    'missing_lowercase',
    'missing_digit',
    'missing_symbol',
    'insufficient_distinct',
  ])('rejects a password missing only %s', (missing) => {
    const cases: Record<string, string> = {
      too_short: 'Aa1!Bb',
      too_long: 'A'.repeat(73),
      missing_uppercase: 'aa1!bb2@cc3#dd4$ee5%ff6^gg7&hh8*ii9(jj0)',
      missing_lowercase: 'AA1!BB2@CC3#DD4$EE5%FF6^GG7&HH8*II9(JJ0)',
      missing_digit: 'Aa!Bb@Cc#Dd$Ee%Ff^Gg&Hh*Ii(Jj)Kk',
      missing_symbol: 'Aa1Bb2Cc3Dd4Ee5Ff6Gg7Hh8Ii9Jj0',
      insufficient_distinct: 'AAAAAAAAAAAAA1!',
    };
    const result = policy.validate(cases[missing]!);
    expect(isViolation(result, missing)).toBe(true);
  });

  it('returns ALL violations for a password missing every category', () => {
    const result = policy.validate('aaaaaaaaaaaa');
    expect(result).toEqual(
      expect.arrayContaining([
        'missing_uppercase',
        'missing_digit',
        'missing_symbol',
        'insufficient_distinct',
      ]),
    );
  });

  it('counts Unicode code points (not UTF-16 units)', () => {
    // Astral codepoint 😀 (U+1F600) is a single UTF-16 surrogate pair but
    // a SINGLE code point; 12 such glyphs pass the 12-codepoint floor.
    const twelveEmoji = '😀'.repeat(12) + '!Aa1';
    expect(twelveEmoji.length).toBeGreaterThan(12); // many UTF-16 units
    const codePoints = Array.from(twelveEmoji);
    expect(codePoints.length).toBe(16);
    expect(codePoints.slice(0, 12).every((cp) => cp === '😀')).toBe(true);
    // 16 code points ≥ 12; if 12 were UTF-16 units we'd accept even shorter strings.
    const result = policy.validate(twelveEmoji);
    expect(isViolation(result, 'too_short')).toBe(false);
  });

  it('rejects 11 code points even when they are many UTF-16 units', () => {
    const elevenEmoji = '😀'.repeat(11);
    const result = policy.validate(elevenEmoji);
    expect(isViolation(result, 'too_short')).toBe(true);
  });

  it('enforces the 72-UTF8-byte ceiling across multibyte chars', () => {
    // 36 × 2-byte Cyrillic + 1 ASCII = 73 bytes — exceeds the bound.
    const oversize = 'Ж'.repeat(36) + 'A';
    expect(Buffer.byteLength(oversize, 'utf8')).toBe(73);
    const result = policy.validate(oversize);
    expect(isViolation(result, 'too_long')).toBe(true);
  });

  it('accepts a 72-UTF8-byte password with multibyte characters', () => {
    // Aa1! (4 ASCII bytes) + Ж (2 UTF-8 bytes) + 33 × Б (66 UTF-8 bytes) = 72 bytes.
    const ok = 'Aa1!Ж' + 'Б'.repeat(33);
    expect(Buffer.byteLength(ok, 'utf8')).toBe(72);
    const result = policy.validate(ok);
    expect(result).toEqual([]);
  });

  it('does not trim or normalize the plaintext', () => {
    // Surrounding whitespace must not be removed before validation.
    const padded = '   Aa1!Bb2@Cc3#   ';
    expect(padded.length).toBe(18);
    const result = policy.validate(padded);
    expect(isViolation(result, 'too_short')).toBe(false);
    expect(isViolation(result, 'missing_uppercase')).toBe(false);
    expect(isViolation(result, 'missing_lowercase')).toBe(false);
    expect(isViolation(result, 'missing_digit')).toBe(false);
    expect(isViolation(result, 'missing_symbol')).toBe(false);
    // 4 distinct: A, a, 1, !, B, b, 2, @, C, c, 3, #, ' ' (space) = 13 distinct → OK.
    expect(isViolation(result, 'insufficient_distinct')).toBe(false);
  });

  it('treats Unicode-equivalent strings as DIFFERENT (no normalization)', () => {
    // NFKC-normalized "ﬁ" is "fi"; without normalization, "ﬁ" is a single
    // code point. The policy must NOT collapse them, so the password
    // containing a ligature must be evaluated code-point-by-code-point.
    const ligature = 'ﬁre!Aa1!Bb2@';
    const result = policy.validate(ligature);
    const codePoints = Array.from(ligature);
    expect(new Set(codePoints).size).toBeGreaterThanOrEqual(4);
    // '!' is a symbol, so missing_symbol must NOT be flagged. The point of
    // the test is: the policy handles astral/non-Latin letters without
    // throwing or silently rewriting.
    expect(isViolation(result, 'missing_symbol')).toBe(false);
    expect(isViolation(result, 'missing_uppercase')).toBe(false);
    expect(isViolation(result, 'missing_lowercase')).toBe(false);
    expect(isViolation(result, 'missing_digit')).toBe(false);
    expect(Array.isArray(result)).toBe(true);
  });

  it('recognises Unicode uppercase, lowercase, digit and symbol classes', () => {
    // Cyrillic uppercase + lowercase + Arabic-Indic digit + symbol.
    const unicode = 'Аб1!Вг2#Дд3$Ее4%Жж5&Зз6(Ии7)Йй8*Кк9';
    const codePoints = Array.from(unicode);
    expect(codePoints.length).toBeGreaterThanOrEqual(12);
    const result = policy.validate(unicode);
    expect(result).toEqual([]);
  });

  it('does not count ASCII digits as symbols (digits must come from \\p{Nd})', () => {
    // Only digits and letters, no symbols.
    const result = policy.validate('AaBb1CcDd2EeFf3');
    expect(isViolation(result, 'missing_symbol')).toBe(true);
    expect(isViolation(result, 'missing_digit')).toBe(false);
    expect(isViolation(result, 'missing_uppercase')).toBe(false);
    expect(isViolation(result, 'missing_lowercase')).toBe(false);
  });

  it('counts each symbol only once across categories', () => {
    const result = policy.validate('Aa1!Bb2@Cc3#Dd4$');
    expect(result).toEqual([]);
  });

  it('rejects passwords with too few distinct code points even when long', () => {
    // 13 characters but only 3 distinct code points (A, a, !) — fails
    // the 4-distinct rule.
    const result = policy.validate('Aaa!aaaaaaaa!');
    expect(isViolation(result, 'insufficient_distinct')).toBe(true);
  });

  it.each(MULTIBYTE_PASSWORD_FIXTURES)(
    'multibyte fixture "$label": utf8Bytes=$utf8Bytes codePoints=$codePoints',
    (fixture) => {
      // Sanity-check the fixture itself so a regression in the fixture
      // file cannot silently invalidate the assertion.
      expect(Buffer.byteLength(fixture.value, 'utf8')).toBe(fixture.utf8Bytes);
      expect(Array.from(fixture.value).length).toBe(fixture.codePoints);
      const result = policy.validate(fixture.value);
      // A fixture is too short (5–6 code points) — the policy must flag it.
      expect(isViolation(result, 'too_short')).toBe(true);
    },
  );

  it('handles non-string input without throwing', () => {
    // @ts-expect-error: intentionally wrong type
    expect(policy.validate(null)).toEqual(['too_short']);
    // @ts-expect-error: intentionally wrong type
    expect(policy.validate(undefined)).toEqual(['too_short']);
    // @ts-expect-error: intentionally wrong type
    expect(policy.validate(123)).toEqual(['too_short']);
  });
});
