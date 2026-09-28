/**
 * Private object storage for profile pictures (F1 §14). PostgreSQL stores only
 * the opaque key; bytes live behind this port. Production binds a private
 * object-storage adapter; local development uses the filesystem adapter below
 * with a directory outside tracked files.
 */
import { mkdir, readFile, rm, writeFile } from 'node:fs/promises';
import { dirname, join, resolve, sep } from 'node:path';

export interface ProfilePhotoStorage {
  put(key: string, bytes: Buffer): Promise<void>;
  /** Returns null when the object does not exist. */
  get(key: string): Promise<Buffer | null>;
  /** Idempotent: deleting a missing object succeeds. */
  delete(key: string): Promise<void>;
}

export const PROFILE_PHOTO_STORAGE = Symbol('PROFILE_PHOTO_STORAGE');

/** `users/{user_uuid}/{random_uuid}.{jpg|jpeg|png|webp}` — server-generated only. */
export const PROFILE_PHOTO_KEY_PATTERN =
  /^users\/[0-9a-f]{8}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{12}\/[0-9a-f]{8}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{12}\.(jpg|jpeg|png|webp)$/;

export function assertProfilePhotoKey(key: string): void {
  if (!PROFILE_PHOTO_KEY_PATTERN.test(key)) {
    throw new Error('Invalid profile photo object key.');
  }
}

/** Development adapter: files under a root directory that is never tracked. */
export class FilesystemProfilePhotoStorage implements ProfilePhotoStorage {
  private readonly root: string;

  constructor(rootDirectory: string) {
    this.root = resolve(rootDirectory);
  }

  private pathFor(key: string): string {
    assertProfilePhotoKey(key);
    const full = resolve(join(this.root, ...key.split('/')));
    if (!full.startsWith(this.root + sep)) {
      throw new Error('Invalid profile photo object key.');
    }
    return full;
  }

  async put(key: string, bytes: Buffer): Promise<void> {
    const path = this.pathFor(key);
    await mkdir(dirname(path), { recursive: true });
    await writeFile(path, bytes, { flag: 'wx' });
  }

  async get(key: string): Promise<Buffer | null> {
    try {
      return await readFile(this.pathFor(key));
    } catch (error) {
      if ((error as NodeJS.ErrnoException).code === 'ENOENT') return null;
      throw error;
    }
  }

  async delete(key: string): Promise<void> {
    await rm(this.pathFor(key), { force: true });
  }
}

/**
 * Resolves the storage adapter from configuration. Production must configure
 * private object storage explicitly; without it the filesystem adapter is only
 * allowed outside production (fail closed).
 */
export function createProfilePhotoStorage(
  env: Record<string, string | undefined>,
): ProfilePhotoStorage {
  const configured = env['PROFILE_PHOTO_STORAGE_DIR']?.trim();
  if (configured !== undefined && configured !== '') {
    return new FilesystemProfilePhotoStorage(configured);
  }
  if (env['NODE_ENV'] === 'production') {
    return {
      put: () => Promise.reject(new Error('Profile photo storage is not configured.')),
      get: () => Promise.reject(new Error('Profile photo storage is not configured.')),
      delete: () => Promise.reject(new Error('Profile photo storage is not configured.')),
    };
  }
  return new FilesystemProfilePhotoStorage(join(process.cwd(), 'tmp', 'profile-photos'));
}
