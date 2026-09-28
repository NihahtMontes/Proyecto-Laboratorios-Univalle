/**
 * Bcrypt password hasher — the only writer of new credentials.
 *
 * - Hashes with the configured current cost via `bcryptjs`.
 * - Throws (WITHOUT echoing the plaintext) for any plaintext longer than
 *   `BCRYPT_PASSWORD_MAX_BYTES` (72) UTF-8 bytes. The error message names
 *   the field and the bound; no plaintext, hash material or derived key
 *   is ever placed in the message or any log.
 * - The plaintext is treated as opaque UTF-8 bytes; no trim, no
 *   normalization. Cost and salt randomness are configured by the
 *   injected `currentCost` and `bcryptjs`'s built-in secure RNG.
 */
import bcrypt from 'bcryptjs';
import type { PasswordHasher } from '../identity.contracts.js';

export const BCRYPT_PASSWORD_MAX_BYTES = 72;

export class BcryptPasswordHasher implements PasswordHasher {
  readonly currentCost: number;

  constructor(currentCost: number) {
    if (!Number.isInteger(currentCost)) {
      throw new Error('BcryptPasswordHasher: currentCost must be an integer.');
    }
    if (currentCost < 4 || currentCost > 31) {
      throw new Error('BcryptPasswordHasher: currentCost must be between 4 and 31.');
    }
    this.currentCost = currentCost;
  }

  async hash(plaintext: string): Promise<string> {
    if (typeof plaintext !== 'string') {
      throw new Error('BcryptPasswordHasher: plaintext is required.');
    }
    const bytes = Buffer.byteLength(plaintext, 'utf8');
    if (bytes > BCRYPT_PASSWORD_MAX_BYTES) {
      throw new Error(
        `BcryptPasswordHasher: plaintext exceeds the ${BCRYPT_PASSWORD_MAX_BYTES}-byte bound.`,
      );
    }
    return bcrypt.hash(plaintext, this.currentCost);
  }
}
