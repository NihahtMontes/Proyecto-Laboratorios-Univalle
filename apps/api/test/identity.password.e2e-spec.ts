import { pbkdf2Sync } from 'node:crypto';
import bcrypt from 'bcryptjs';
import { AspNetIdentityPasswordVerifier } from '../src/identity/password/aspnet-identity.verifier.js';
import { parseAspNetIdentityHash } from '../src/identity/password/aspnet-identity-hash.js';
import {
  BcryptPasswordVerifier,
  type BcryptVerifierOptions,
} from '../src/identity/password/bcrypt.verifier.js';
import { DefaultPasswordVerificationService } from '../src/identity/password/password-verification.service.js';
import { classifyLegacyPasswordHash } from '../src/database/legacy-user-mapping.js';
import { bcryptVerifierMinCost } from '../src/identity/identity.providers.js';
import { IDENTITY_VECTORS, MALFORMED_IDENTITY_VECTORS } from './identity-vectors.js';

const prfToDigest = {
  sha1: 'sha1',
  sha256: 'sha256',
  sha512: 'sha512',
} as const;

const DUMMY_HASH = '$2b$12$cwX8Zvuf1RsO.CYGKnnT5OiRQ/sGS6ptomphoUa2I1ReqXiiqGJ6i';

describe('parseAspNetIdentityHash', () => {
  it.each(IDENTITY_VECTORS.filter((v) => v.version === 2))('parses V2 fixture $name', (vector) => {
    const parsed = parseAspNetIdentityHash(vector.base64);
    expect(parsed).not.toBeNull();
    expect(parsed?.version).toBe(2);
    expect(parsed?.prf).toBe('sha1');
    expect(parsed?.iterations).toBe(1000);
    expect(parsed?.salt.length).toBe(16);
    expect(parsed?.subkey.length).toBe(32);
  });

  it.each(IDENTITY_VECTORS.filter((v) => v.version === 3))('parses V3 fixture $name', (vector) => {
    const parsed = parseAspNetIdentityHash(vector.base64);
    expect(parsed).not.toBeNull();
    expect(parsed?.version).toBe(3);
    expect(parsed?.prf).toBe(vector.prf);
    expect(parsed?.iterations).toBe(vector.iterations);
    expect(parsed?.salt.length).toBe(vector.saltLength);
    expect(parsed?.subkey.length).toBe(vector.subkeyLength);
  });

  it.each(MALFORMED_IDENTITY_VECTORS)('rejects malformed fixture $name', ({ base64 }) => {
    expect(parseAspNetIdentityHash(base64)).toBeNull();
  });

  it('rejects non-string input', () => {
    // @ts-expect-error: intentionally wrong type
    expect(parseAspNetIdentityHash(null)).toBeNull();
    // @ts-expect-error: intentionally wrong type
    expect(parseAspNetIdentityHash(undefined)).toBeNull();
    // @ts-expect-error: intentionally wrong type
    expect(parseAspNetIdentityHash(123)).toBeNull();
  });
});

describe('AspNetIdentityPasswordVerifier', () => {
  const v2 = new AspNetIdentityPasswordVerifier('legacy_identity_v2');
  const v3 = new AspNetIdentityPasswordVerifier('legacy_identity_v3');

  it.each(IDENTITY_VECTORS)('verifies fixture $name with the right plaintext', async (vector) => {
    const verifier = vector.version === 2 ? v2 : v3;
    const result = await verifier.verify(vector.password, vector.base64);
    expect(result.verified).toBe(true);
    expect(result.needsRehash).toBe(true);
  });

  it('rejects a wrong password (V2)', async () => {
    const vector = IDENTITY_VECTORS[0]!;
    const result = await v2.verify('wrong-password', vector.base64);
    expect(result.verified).toBe(false);
    expect(result.needsRehash).toBe(false);
  });

  it('rejects a wrong password (V3 sha512)', async () => {
    const vector = IDENTITY_VECTORS.find(
      (v) => v.prf === 'sha512' && v.iterations === 100_000 && v.version === 3,
    )!;
    const result = await v3.verify('wrong-password', vector.base64);
    expect(result.verified).toBe(false);
    expect(result.needsRehash).toBe(false);
  });

  it('rejects a V2 hash submitted to the V3 verifier', async () => {
    const v2Vec = IDENTITY_VECTORS.find((v) => v.version === 2)!;
    const result = await v3.verify(v2Vec.password, v2Vec.base64);
    expect(result.verified).toBe(false);
    expect(result.needsRehash).toBe(false);
  });

  it('rejects a V3 hash submitted to the V2 verifier', async () => {
    const v3Vec = IDENTITY_VECTORS.find((v) => v.version === 3)!;
    const result = await v2.verify(v3Vec.password, v3Vec.base64);
    expect(result.verified).toBe(false);
    expect(result.needsRehash).toBe(false);
  });

  it('rejects every malformed input without throwing', async () => {
    for (const { base64 } of MALFORMED_IDENTITY_VECTORS) {
      const r1 = await v2.verify('irrelevant', base64);
      expect(r1.verified).toBe(false);
      expect(r1.needsRehash).toBe(false);
      const r2 = await v3.verify('irrelevant', base64);
      expect(r2.verified).toBe(false);
      expect(r2.needsRehash).toBe(false);
    }
  });

  it('treats plaintext as opaque UTF-8 bytes (no trim, no normalization)', async () => {
    const vector = IDENTITY_VECTORS[0]!;
    const withSpace = `  ${vector.password}  `;
    const result = await v2.verify(withSpace, vector.base64);
    expect(result.verified).toBe(false);
    expect(result.needsRehash).toBe(false);
  });
});

describe('BcryptPasswordVerifier', () => {
  const makeOptions = (currentCost = 12): BcryptVerifierOptions => ({
    currentCost,
    minCost: 4,
    maxCost: 15,
  });

  it('verifies a hash at the current cost and signals no rehash', async () => {
    const hash = await bcrypt.hash('Aa1!Aa1!Aa1!Aa1!', 8);
    const verifier = new BcryptPasswordVerifier(makeOptions(8));
    const result = await verifier.verify('Aa1!Aa1!Aa1!Aa1!', hash);
    expect(result.verified).toBe(true);
    expect(result.needsRehash).toBe(false);
  });

  it('signals needsRehash when the stored cost is below the current cost', async () => {
    const hash = await bcrypt.hash('Aa1!Aa1!Aa1!Aa1!', 4);
    const verifier = new BcryptPasswordVerifier(makeOptions(8));
    const result = await verifier.verify('Aa1!Aa1!Aa1!Aa1!', hash);
    expect(result.verified).toBe(true);
    expect(result.needsRehash).toBe(true);
  });

  it('rejects a stored cost above the allowed ceiling', async () => {
    // Build a hash shape that LOOKS valid (regex) but cost outside range.
    const verifier = new BcryptPasswordVerifier({ currentCost: 12, minCost: 4, maxCost: 10 });
    const hash = await bcrypt.hash('Aa1!Aa1!Aa1!Aa1!', 11);
    const result = await verifier.verify('Aa1!Aa1!Aa1!Aa1!', hash);
    expect(result.verified).toBe(false);
    expect(result.needsRehash).toBe(false);
  });

  it('rejects a stored cost below the allowed floor', async () => {
    const verifier = new BcryptPasswordVerifier({ currentCost: 12, minCost: 8, maxCost: 15 });
    const hash = await bcrypt.hash('Aa1!Aa1!Aa1!Aa1!', 4);
    const result = await verifier.verify('Aa1!Aa1!Aa1!Aa1!', hash);
    expect(result.verified).toBe(false);
    expect(result.needsRehash).toBe(false);
  });

  it('rejects a plaintext > 72 UTF-8 bytes without calling compare', async () => {
    const hash = await bcrypt.hash('short', 8);
    const verifier = new BcryptPasswordVerifier(makeOptions(8));
    const oversized = 'a'.repeat(73);
    const result = await verifier.verify(oversized, hash);
    expect(result.verified).toBe(false);
    expect(result.needsRehash).toBe(false);
  });

  it('accepts a 72-byte plaintext (boundary)', async () => {
    const plaintext = 'A'.repeat(72);
    const hash = await bcrypt.hash(plaintext, 8);
    const verifier = new BcryptPasswordVerifier(makeOptions(8));
    const result = await verifier.verify(plaintext, hash);
    expect(result.verified).toBe(true);
  });

  it('accepts a 72-UTF8-byte multibyte plaintext (boundary)', async () => {
    // Cyrillic Ж is U+0416, encoded as 2 UTF-8 bytes (D0 96). 34 chars × 2
    // bytes + 4 ASCII bytes = 72 UTF-8 bytes exactly.
    const plaintext = 'Ж'.repeat(34) + 'Aa1!';
    const utf8Bytes = Buffer.byteLength(plaintext, 'utf8');
    expect(utf8Bytes).toBe(72);
    const hash = await bcrypt.hash(plaintext, 8);
    const verifier = new BcryptPasswordVerifier(makeOptions(8));
    const result = await verifier.verify(plaintext, hash);
    expect(result.verified).toBe(true);
  });

  it('rejects a 73-byte multibyte plaintext without calling compare', async () => {
    // 35 Cyrillic chars × 2 = 70 + 4 ASCII = 74 UTF-8 bytes — exceeds the
    // 72-byte bound. (35 × 2 alone would also exceed.)
    const plaintext = 'Ж'.repeat(35) + 'Aa1!';
    expect(Buffer.byteLength(plaintext, 'utf8')).toBe(74);
    const hash = await bcrypt.hash('Aa1!Aa1!Aa1!Aa1!', 8);
    const verifier = new BcryptPasswordVerifier(makeOptions(8));
    const result = await verifier.verify(plaintext, hash);
    expect(result.verified).toBe(false);
  });

  it('rejects a malformed hash shape without throwing', async () => {
    const verifier = new BcryptPasswordVerifier(makeOptions(8));
    expect((await verifier.verify('Aa1!Aa1!Aa1!Aa1!', 'not-a-bcrypt-hash')).verified).toBe(false);
    expect(
      (
        await verifier.verify(
          'Aa1!Aa1!Aa1!Aa1!',
          '$2c$10$abcdefghijklmnopqrstuvwxyz0123456789ABCDEFGHIJKL',
        )
      ).verified,
    ).toBe(false);
    expect((await verifier.verify('Aa1!Aa1!Aa1!Aa1!', '$2b$03$' + 'A'.repeat(53))).verified).toBe(
      false,
    );
    // All-'A' 53-char payload is not a real bcrypt hash (no salt); bcrypt.compare
    // returns false, but the SHAPE passes the regex + cost range. The verifier
    // must call bcrypt.compare and let it decide. expected: false.
    expect((await verifier.verify('Aa1!Aa1!Aa1!Aa1!', '$2b$10$' + 'A'.repeat(53))).verified).toBe(
      false,
    );
  });
});

describe('DefaultPasswordVerificationService (dispatcher)', () => {
  it('builds a scheme-keyed map and throws on duplicate registration', () => {
    const bcryptV = new BcryptPasswordVerifier({ currentCost: 12, minCost: 4, maxCost: 15 });
    const v2V = new AspNetIdentityPasswordVerifier('legacy_identity_v2');
    expect(
      () =>
        new DefaultPasswordVerificationService(
          [bcryptV, new BcryptPasswordVerifier({ currentCost: 12, minCost: 4, maxCost: 15 })],
          DUMMY_HASH,
        ),
    ).toThrow(/duplicate verifier scheme/);
    // unique registration does not throw
    expect(() => new DefaultPasswordVerificationService([bcryptV, v2V], DUMMY_HASH)).not.toThrow();
  });

  it('dispatches bcrypt, legacy V2 and legacy V3 to their verifiers', async () => {
    const bcryptV = new BcryptPasswordVerifier({ currentCost: 12, minCost: 4, maxCost: 15 });
    const v2V = new AspNetIdentityPasswordVerifier('legacy_identity_v2');
    const v3V = new AspNetIdentityPasswordVerifier('legacy_identity_v3');
    const service = new DefaultPasswordVerificationService([bcryptV, v2V, v3V], DUMMY_HASH);
    const v2Vec = IDENTITY_VECTORS.find((v) => v.version === 2)!;
    const v3Vec = IDENTITY_VECTORS.find((v) => v.version === 3)!;
    const bcryptHash = await bcrypt.hash('Aa1!Aa1!Aa1!Aa1!', 8);

    expect(
      (await service.verify('legacy_identity_v2', v2Vec.base64, v2Vec.password)).verified,
    ).toBe(true);
    expect(
      (await service.verify('legacy_identity_v3', v3Vec.base64, v3Vec.password)).verified,
    ).toBe(true);
    expect((await service.verify('bcrypt', bcryptHash, 'Aa1!Aa1!Aa1!Aa1!')).verified).toBe(true);
    expect((await service.verify('bcrypt', bcryptHash, 'wrong')).verified).toBe(false);
  });

  it('runs dummyVerify and returns false for reset_required, null hash, and unknown scheme', async () => {
    const bcryptV = new BcryptPasswordVerifier({ currentCost: 12, minCost: 4, maxCost: 15 });
    const v2V = new AspNetIdentityPasswordVerifier('legacy_identity_v2');
    const service = new DefaultPasswordVerificationService([bcryptV, v2V], DUMMY_HASH);
    const r1 = await service.verify('reset_required', null, 'any');
    expect(r1.verified).toBe(false);
    expect(r1.needsRehash).toBe(false);
    const r2 = await service.verify('bcrypt', null, 'any');
    expect(r2.verified).toBe(false);
    // @ts-expect-error: unknown scheme is rejected at runtime
    const r3 = await service.verify('unknown_scheme', 'whatever', 'any');
    expect(r3.verified).toBe(false);
  });

  it('dummyVerify always resolves', async () => {
    const bcryptV = new BcryptPasswordVerifier({ currentCost: 12, minCost: 4, maxCost: 15 });
    const service = new DefaultPasswordVerificationService([bcryptV], DUMMY_HASH);
    await expect(service.dummyVerify('whatever')).resolves.toBeUndefined();
  });
});

describe('runtime accept set equals importer accept set', () => {
  // The F2 importer classifyLegacyPasswordHash is the source of truth for
  // "is this hash a verifiable legacy hash?"; the runtime parser must
  // accept the EXACT SAME set. This test iterates the frozen well-formed
  // fixtures (must classify as the corresponding legacy scheme) and the
  // malformed fixtures (must classify as reset_required). Any disagreement
  // is reported as a bug against the runtime parser, not against the
  // importer (the importer is read-only for this worker).
  it.each(IDENTITY_VECTORS.filter((v) => v.version === 2))(
    'agrees on V2 fixture $name',
    (vector) => {
      const classification = classifyLegacyPasswordHash(vector.base64);
      expect(classification.scheme).toBe('legacy_identity_v2');
      expect(parseAspNetIdentityHash(vector.base64)).not.toBeNull();
    },
  );

  it.each(IDENTITY_VECTORS.filter((v) => v.version === 3))(
    'agrees on V3 fixture $name',
    (vector) => {
      const classification = classifyLegacyPasswordHash(vector.base64);
      expect(classification.scheme).toBe('legacy_identity_v3');
      expect(parseAspNetIdentityHash(vector.base64)).not.toBeNull();
    },
  );

  it.each(MALFORMED_IDENTITY_VECTORS)(
    'agrees that malformed fixture $name is reset_required',
    ({ base64 }) => {
      const classification = classifyLegacyPasswordHash(base64);
      expect(classification.scheme).toBe('reset_required');
      expect(parseAspNetIdentityHash(base64)).toBeNull();
    },
  );
});

describe('literal vectors were generated by the canonical PBKDF2 algorithm', () => {
  // Re-derives each frozen fixture from the documented algorithm using
  // `node:crypto` directly; the test must NOT call `parseAspNetIdentityHash`
  // or any verifier under test. If this test ever fails, the fixture
  // file drifted from the canonical algorithm and the parse/verify tests
  // cannot be trusted.
  it.each(IDENTITY_VECTORS)('re-derives $name', (vector) => {
    const salt = Buffer.from(
      Buffer.from(vector.base64, 'base64').subarray(
        vector.version === 2 ? 1 : 1 + 12,
        vector.version === 2 ? 1 + 16 : 1 + 12 + vector.saltLength,
      ),
    );
    const passwordBytes = Buffer.from(vector.password, 'utf8');
    const derived = pbkdf2Sync(
      passwordBytes,
      salt,
      vector.iterations,
      vector.subkeyLength,
      prfToDigest[vector.prf],
    );
    const expectedHeaderLen = vector.version === 2 ? 1 + 16 : 1 + 12 + vector.saltLength;
    const rederived = Buffer.concat([
      Buffer.from(vector.base64, 'base64').subarray(0, expectedHeaderLen),
      derived,
    ]);
    expect(rederived.toString('base64')).toBe(vector.base64);
  });
});

describe('bcrypt verifier production cost floor (F1 §7: 10..15)', () => {
  const password = 'Valid-Passw0rd!';

  it('derives floor 10 for any production current cost and keeps lower floors only for test-seam costs', () => {
    expect(bcryptVerifierMinCost(12)).toBe(10);
    expect(bcryptVerifierMinCost(10)).toBe(10);
    expect(bcryptVerifierMinCost(4)).toBe(4);
  });

  it('rejects a valid cost-4 hash when the current cost is 12', async () => {
    const verifier = new BcryptPasswordVerifier({
      currentCost: 12,
      minCost: bcryptVerifierMinCost(12),
      maxCost: 15,
    });
    const weak = bcrypt.hashSync(password, 4);
    await expect(verifier.verify(password, weak)).resolves.toEqual({
      verified: false,
      needsRehash: false,
    });
  });

  it('verifies a cost-10 hash at current cost 12 and flags it for rehash', async () => {
    const verifier = new BcryptPasswordVerifier({
      currentCost: 12,
      minCost: bcryptVerifierMinCost(12),
      maxCost: 15,
    });
    const allowed = bcrypt.hashSync(password, 10);
    await expect(verifier.verify(password, allowed)).resolves.toEqual({
      verified: true,
      needsRehash: true,
    });
  });
});
