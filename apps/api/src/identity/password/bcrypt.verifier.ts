/**
 * Bcrypt password verifier adapter.
 *
 * - Uses the `bcryptjs` pure-JS package (already a dependency in
 *   `apps/api/package.json`; no native binding required).
 * - The verifier is registered with the `'bcrypt'` scheme; on success it
 *   signals `needsRehash: true` if the stored cost differs from
 *   `currentCost` (F1 §7: bcrypt at an allowed non-current cost verifies
 *   and is rehashed after success).
 * - Plaintext longer than `BCRYPT_PASSWORD_MAX_BYTES` (72) UTF-8 bytes
 *   fails closed WITHOUT calling bcrypt (bcryptjs silently truncates the
 *   first 72 bytes, which would allow an attacker to choose a 73-byte
 *   payload whose first 72 bytes match the stored hash).
 * - A stored cost outside `[minCost, maxCost]` fails closed without
 *   calling bcrypt (defense in depth: bcryptjs accepts any 04..31 cost).
 * - A non-string stored hash fails closed.
 * - The plaintext is NEVER logged; error messages are generic and never
 *   echo plaintext or hash material.
 */
import bcrypt from 'bcryptjs';
import type { PasswordVerifier, PasswordVerificationResult } from '../identity.contracts.js';

const BCRYPT_HASH_REGEX = /^\$2[aby]\$(0[4-9]|[12][0-9]|3[01])\$[./A-Za-z0-9]{53}$/;

export interface BcryptVerifierOptions {
  readonly currentCost: number;
  readonly minCost: number;
  readonly maxCost: number;
}

export class BcryptPasswordVerifier implements PasswordVerifier {
  readonly scheme = 'bcrypt' as const;
  readonly currentCost: number;
  readonly minCost: number;
  readonly maxCost: number;

  constructor(options: BcryptVerifierOptions) {
    if (!Number.isInteger(options.currentCost)) {
      throw new Error('BcryptPasswordVerifier: currentCost must be an integer.');
    }
    if (!Number.isInteger(options.minCost) || !Number.isInteger(options.maxCost)) {
      throw new Error('BcryptPasswordVerifier: minCost/maxCost must be integers.');
    }
    if (options.minCost > options.maxCost) {
      throw new Error('BcryptPasswordVerifier: minCost must be <= maxCost.');
    }
    this.currentCost = options.currentCost;
    this.minCost = options.minCost;
    this.maxCost = options.maxCost;
  }

  async verify(plaintext: string, storedHash: string): Promise<PasswordVerificationResult> {
    if (typeof plaintext !== 'string' || typeof storedHash !== 'string') {
      return { verified: false, needsRehash: false };
    }
    if (Buffer.byteLength(plaintext, 'utf8') > 72) {
      return { verified: false, needsRehash: false };
    }
    if (!BCRYPT_HASH_REGEX.test(storedHash)) {
      return { verified: false, needsRehash: false };
    }
    let storedCost: number;
    try {
      storedCost = bcrypt.getRounds(storedHash);
    } catch {
      return { verified: false, needsRehash: false };
    }
    if (storedCost < this.minCost || storedCost > this.maxCost) {
      return { verified: false, needsRehash: false };
    }
    let ok: boolean;
    try {
      ok = await bcrypt.compare(plaintext, storedHash);
    } catch {
      return { verified: false, needsRehash: false };
    }
    if (!ok) {
      return { verified: false, needsRehash: false };
    }
    return { verified: true, needsRehash: storedCost !== this.currentCost };
  }
}
