import { pbkdf2Sync, randomBytes } from 'node:crypto';
import bcrypt from 'bcryptjs';

/** Same cost as the API default, so a seeded bcrypt hash never triggers a cost rehash. */
export const BCRYPT_COST = 12;

export function bcryptHash(password: string): string {
  return bcrypt.hashSync(password, BCRYPT_COST);
}

/** ASP.NET Identity V2: 0x00 + 16-byte salt + 32-byte PBKDF2-HMAC-SHA1(1000) subkey. */
export function aspNetIdentityV2(password: string): string {
  const salt = randomBytes(16);
  const subkey = pbkdf2Sync(Buffer.from(password, 'utf8'), salt, 1000, 32, 'sha1');
  return Buffer.concat([Buffer.from([0x00]), salt, subkey]).toString('base64');
}

/** ASP.NET Identity V3: 0x01 + PRF(1=SHA256) + iterations + salt length (uint32 BE) + salt + subkey. */
export function aspNetIdentityV3(password: string, iterations = 10_000): string {
  const salt = randomBytes(16);
  const subkey = pbkdf2Sync(Buffer.from(password, 'utf8'), salt, iterations, 32, 'sha256');
  const header = Buffer.alloc(13);
  header.writeUInt8(0x01, 0);
  header.writeUInt32BE(1, 1);
  header.writeUInt32BE(iterations, 5);
  header.writeUInt32BE(salt.length, 9);
  return Buffer.concat([header, salt, subkey]).toString('base64');
}
