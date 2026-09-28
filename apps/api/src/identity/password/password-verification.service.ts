/**
 * Multi-scheme password verification dispatcher.
 *
 * - One `PasswordVerifier` per verifiable scheme (`legacy_identity_v2`,
 *   `legacy_identity_v3`, `bcrypt`) is wired by the providers module.
 *   `reset_required` has no verifier; for that scheme (or for a missing
 *   strategy, a null stored hash, or any unknown scheme) the service
 *   runs a DUMMY verification of comparable cost against `dummyBcryptHash`
 *   and returns `verified: false`. This equalizes timing when no user
 *   credential exists.
 * - Construction is fail-fast: duplicate scheme registration throws so a
 *   wiring mistake is detected at startup, not on the request path.
 * - The service NEVER throws on the request path; every error path
 *   returns `{ verified: false, needsRehash: false }` and `dummyVerify`
 *   swallows the comparison result.
 * - Plaintext, hash material and derived keys are NEVER logged; the
 *   dummy comparison result is intentionally discarded.
 */
import type {
  PasswordScheme,
  PasswordVerificationResult,
  PasswordVerificationService,
  PasswordVerifier,
} from '../identity.contracts.js';

export class DefaultPasswordVerificationService implements PasswordVerificationService {
  private readonly verifiers: ReadonlyMap<PasswordVerifier['scheme'], PasswordVerifier>;
  private readonly dummyBcryptHash: string;

  constructor(verifiers: readonly PasswordVerifier[], dummyBcryptHash: string) {
    if (typeof dummyBcryptHash !== 'string' || dummyBcryptHash.length === 0) {
      throw new Error('DefaultPasswordVerificationService: dummyBcryptHash is required.');
    }
    const map = new Map<PasswordVerifier['scheme'], PasswordVerifier>();
    for (const verifier of verifiers) {
      if (map.has(verifier.scheme)) {
        throw new Error(
          `DefaultPasswordVerificationService: duplicate verifier scheme "${verifier.scheme}".`,
        );
      }
      map.set(verifier.scheme, verifier);
    }
    this.verifiers = map;
    this.dummyBcryptHash = dummyBcryptHash;
  }

  async verify(
    scheme: PasswordScheme,
    storedHash: string | null,
    plaintext: string,
  ): Promise<PasswordVerificationResult> {
    if (scheme === 'reset_required' || storedHash === null) {
      await this.dummyVerify(plaintext);
      return { verified: false, needsRehash: false };
    }
    const verifier = this.verifiers.get(scheme);
    if (verifier === undefined) {
      await this.dummyVerify(plaintext);
      return { verified: false, needsRehash: false };
    }
    return verifier.verify(plaintext, storedHash);
  }

  async dummyVerify(plaintext: string): Promise<void> {
    try {
      const bcrypt = await import('bcryptjs');
      // Result is intentionally discarded; timing is what matters here.
      await bcrypt.compare(plaintext, this.dummyBcryptHash);
    } catch {
      // Swallow: dummyVerify must never throw on the request path.
    }
  }
}
