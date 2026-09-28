/**
 * Unicode-aware password policy (F1 §7 "New and changed passwords").
 *
 * - Minimum 12 Unicode code points (NOT UTF-16 units; astral codepoints
 *   like emoji count as one each).
 * - Maximum 72 UTF-8 bytes (bcrypt's documented limit).
 * - At least one uppercase letter (`\p{Lu}`), one lowercase letter
 *   (`\p{Ll}`), one ASCII digit (`\p{Nd}`), and one character that is
 *   neither a letter nor a number.
 * - At least 4 distinct code points.
 * - NO trim, NO Unicode normalization (NFKC/NFC), NO case conversion —
 *   the policy is byte-/codepoint-faithful.
 * - Returns ALL detected violations; callers render the union. The
 *   plaintext is NEVER logged or echoed back.
 *
 * The no-arg constructor lets the bootstrap CLI instantiate the policy
 * directly without depending on the auth config provider.
 */
import type { PasswordPolicy, PasswordPolicyViolation } from '../identity.contracts.js';

const MIN_CODE_POINTS = 12;
const MIN_DISTINCT_CODE_POINTS = 4;
const MAX_UTF8_BYTES = 72;

export class Utf8PasswordPolicy implements PasswordPolicy {
  validate(plaintext: string): readonly PasswordPolicyViolation[] {
    if (typeof plaintext !== 'string') {
      return ['too_short'];
    }
    const violations: PasswordPolicyViolation[] = [];
    if (Buffer.byteLength(plaintext, 'utf8') > MAX_UTF8_BYTES) {
      violations.push('too_long');
    }
    const codePoints = Array.from(plaintext);
    if (codePoints.length < MIN_CODE_POINTS) {
      violations.push('too_short');
    }
    const distinct = new Set(codePoints);
    if (distinct.size < MIN_DISTINCT_CODE_POINTS) {
      violations.push('insufficient_distinct');
    }
    let hasUpper = false;
    let hasLower = false;
    let hasDigit = false;
    let hasSymbol = false;
    for (const ch of codePoints) {
      if (!hasUpper && /\p{Lu}/u.test(ch)) hasUpper = true;
      if (!hasLower && /\p{Ll}/u.test(ch)) hasLower = true;
      if (!hasDigit && /\p{Nd}/u.test(ch)) hasDigit = true;
      if (!hasSymbol && !/\p{L}/u.test(ch) && !/\p{N}/u.test(ch)) hasSymbol = true;
      if (hasUpper && hasLower && hasDigit && hasSymbol) break;
    }
    if (!hasUpper) violations.push('missing_uppercase');
    if (!hasLower) violations.push('missing_lowercase');
    if (!hasDigit) violations.push('missing_digit');
    if (!hasSymbol) violations.push('missing_symbol');
    return violations;
  }
}
