/**
 * MIG-001 F3 — profile pictures (F1 §14): validation, storage keys, the
 * replace/remove/read flows and the HTTP wiring behind the real guards.
 */
import { mkdtemp, rm } from 'node:fs/promises';
import { tmpdir } from 'node:os';
import { join } from 'node:path';
import type { NestFastifyApplication } from '@nestjs/platform-fastify';
import type { IPgClient, IPgPool } from '../src/auth/auth.pg-pool.js';
import type { IdentityAuditEvent, RequestIdentity } from '../src/identity/identity.contracts.js';
import { ProfilePhotoRepository } from '../src/users/profile-photo/profile-photo.repository.js';
import { ProfilePhotoService } from '../src/users/profile-photo/profile-photo.service.js';
import {
  FilesystemProfilePhotoStorage,
  PROFILE_PHOTO_KEY_PATTERN,
  type ProfilePhotoStorage,
} from '../src/users/profile-photo/profile-photo.storage.js';
import {
  PROFILE_PHOTO_MAX_BYTES,
  validateProfilePhoto,
} from '../src/users/profile-photo/profile-photo.validator.js';
import { UserRepository } from '../src/users/user.repository.js';
import { UserAuthorizationException, UserValidationException } from '../src/users/user.service.js';
import { createIdentity, createMembership, createSite, FakeAuthRepository } from './auth.fakes.js';
import { bcryptHash, createKernel, createTestApp, TRUSTED_ORIGIN } from './auth.harness.js';
import { AuthService } from '../src/auth/auth.service.js';

const PNG = Buffer.concat([
  Buffer.from([0x89, 0x50, 0x4e, 0x47, 0x0d, 0x0a, 0x1a, 0x0a]),
  Buffer.alloc(32, 1),
]);
const JPEG = Buffer.concat([Buffer.from([0xff, 0xd8, 0xff, 0xe0]), Buffer.alloc(32, 2)]);
const WEBP = Buffer.concat([
  Buffer.from('RIFF'),
  Buffer.alloc(4),
  Buffer.from('WEBP'),
  Buffer.alloc(16, 3),
]);
const HTML = Buffer.from('<html><script>alert(1)</script></html>');

const SITE_A = '11111111-1111-4111-8111-111111111111';
const SITE_B = '22222222-2222-4222-8222-222222222222';
const ACTOR = 'aaaaaaaa-aaaa-4aaa-8aaa-aaaaaaaaaaaa';
const TARGET = 'bbbbbbbb-bbbb-4bbb-8bbb-bbbbbbbbbbbb';

describe('validateProfilePhoto', () => {
  it.each([
    ['foto.png', 'image/png', PNG],
    ['foto.JPG', 'image/jpeg', JPEG],
    ['foto.jpeg', 'image/jpeg; charset=binary', JPEG],
    ['foto.webp', 'image/webp', WEBP],
  ])('accepts %s', (fileName, declaredMimeType, bytes) => {
    expect(validateProfilePhoto({ fileName, declaredMimeType, bytes }).ok).toBe(true);
  });

  it.each([
    ['empty', 'a.png', 'image/png', Buffer.alloc(0)],
    [
      'too_large',
      'a.png',
      'image/png',
      Buffer.concat([PNG, Buffer.alloc(PROFILE_PHOTO_MAX_BYTES)]),
    ],
    ['invalid_file_name', '../a.png', 'image/png', PNG],
    ['invalid_file_name', 'dir\\a.png', 'image/png', PNG],
    ['unsupported_extension', 'a.svg', 'image/png', PNG],
    ['unsupported_extension', 'a.gif', 'image/png', PNG],
    ['unsupported_media_type', 'a.png', 'image/svg+xml', PNG],
    ['signature_mismatch', 'a.png', 'image/png', HTML],
    ['type_mismatch', 'a.png', 'image/jpeg', PNG],
    ['type_mismatch', 'a.jpg', 'image/png', PNG],
  ] as const)('rejects %s', (violation, fileName, declaredMimeType, bytes) => {
    expect(validateProfilePhoto({ fileName, declaredMimeType, bytes })).toEqual({
      ok: false,
      violation,
    });
  });
});

describe('FilesystemProfilePhotoStorage', () => {
  let root: string;
  beforeEach(async () => {
    root = await mkdtemp(join(tmpdir(), 'lu-photos-'));
  });
  afterEach(async () => {
    await rm(root, { recursive: true, force: true });
  });

  it('stores, reads and idempotently deletes server-generated keys', async () => {
    const storage = new FilesystemProfilePhotoStorage(root);
    const key = `users/${TARGET}/cccccccc-cccc-4ccc-8ccc-cccccccccccc.png`;
    await storage.put(key, PNG);
    expect(await storage.get(key)).toEqual(PNG);
    await expect(storage.put(key, JPEG)).rejects.toThrow();
    await storage.delete(key);
    await storage.delete(key);
    expect(await storage.get(key)).toBeNull();
  });

  it.each([
    'users/../../etc/passwd',
    `users/${TARGET}/x.png`,
    `users/${TARGET}/../a.png`,
    'C:\\evil.png',
  ])('rejects the non-canonical key %s', async (key) => {
    expect(PROFILE_PHOTO_KEY_PATTERN.test(key)).toBe(false);
    await expect(new FilesystemProfilePhotoStorage(root).put(key, PNG)).rejects.toThrow(
      'Invalid profile photo object key.',
    );
  });
});

/** Scripted control-plane client: lu_user rows (key + custody) and audit rows. */
class PhotoDb implements IPgPool {
  readonly users = new Map<string, { key: string | null; superAdmin: boolean; sites: string[] }>();
  failNextKeyUpdate = false;

  query<T = Record<string, unknown>>(
    sql: string,
    params: unknown[] = [],
  ): Promise<{ rows: T[]; rowCount: number }> {
    const text = sql.replace(/\s+/g, ' ').trim();
    const id = params[0] as string;
    const user = this.users.get(id);
    const out = (rows: unknown[], rowCount = rows.length) =>
      Promise.resolve({ rows: rows as T[], rowCount });
    if (text.startsWith('SELECT profile_picture_key FROM public.lu_user')) {
      return out(user === undefined ? [] : [{ profile_picture_key: user.key }]);
    }
    if (text.includes('FROM public.lu_user') && text.startsWith('SELECT id, username')) {
      return out(
        user === undefined
          ? []
          : [
              {
                id,
                username: 'u',
                email: 'e@x.test',
                first_name: 'A',
                last_name: 'B',
                second_last_name: null,
                full_name: 'A B',
                identity_card: 'X1',
                phone_number: '+1 555 0100',
                account_status: 'active',
                status: 'active',
                is_super_admin: user.superAdmin,
                password_scheme: 'bcrypt',
                must_change_password: false,
                security_version: '1',
                created_at: new Date(),
                updated_at: new Date(),
              },
            ],
      );
    }
    if (text.includes('FROM public.lu_site_membership m')) {
      return out(
        (user?.sites ?? []).map((site) => ({
          site_id: site,
          site_name: 'S',
          role: 'Supervisor',
          membership_status: 'active',
          valid_from: null,
          valid_until: null,
          position: null,
          department: null,
          hire_date: null,
        })),
      );
    }
    if (text.startsWith('UPDATE public.lu_user SET profile_picture_key')) {
      if (this.failNextKeyUpdate) {
        this.failNextKeyUpdate = false;
        return Promise.reject(new Error('simulated failure'));
      }
      user!.key = params[1] as string | null;
      return out([], 1);
    }
    return Promise.reject(new Error('PhotoDb: unexpected SQL ' + text.slice(0, 60)));
  }

  async transaction<T>(fn: (client: IPgClient) => Promise<T>): Promise<T> {
    const snapshot = new Map([...this.users].map(([k, v]) => [k, { ...v }]));
    try {
      return await fn(this);
    } catch (error) {
      this.users.clear();
      snapshot.forEach((v, k) => this.users.set(k, v));
      throw error;
    }
  }
}

class MemoryStorage implements ProfilePhotoStorage {
  readonly objects = new Map<string, Buffer>();
  failDeletes = 0;
  async put(key: string, bytes: Buffer): Promise<void> {
    this.objects.set(key, bytes);
  }
  async get(key: string): Promise<Buffer | null> {
    return this.objects.get(key) ?? null;
  }
  async delete(key: string): Promise<void> {
    if (this.failDeletes > 0) {
      this.failDeletes -= 1;
      throw new Error('storage unavailable');
    }
    this.objects.delete(key);
  }
}

describe('ProfilePhotoService', () => {
  let db: PhotoDb;
  let storage: MemoryStorage;
  let audits: IdentityAuditEvent[];
  let service: ProfilePhotoService;
  const actor = (overrides: Partial<RequestIdentity> = {}): RequestIdentity => ({
    correlationId: 'c0c0c0c0-c0c0-4c0c-8c0c-c0c0c0c0c0c0',
    sessionId: 's',
    userId: ACTOR,
    isSuperAdmin: false,
    activeSiteId: SITE_A,
    activeSiteRole: 'Administrador',
    ...overrides,
  });
  const upload = { fileName: 'foto.png', declaredMimeType: 'image/png', bytes: PNG };

  beforeEach(() => {
    db = new PhotoDb();
    storage = new MemoryStorage();
    audits = [];
    service = new ProfilePhotoService(
      db,
      storage,
      { append: async (_c, event) => void audits.push(event) },
      new ProfilePhotoRepository(db),
      new UserRepository(db),
    );
    db.users.set(ACTOR, { key: null, superAdmin: false, sites: [SITE_A] });
  });

  it('self replace stores a server-generated key, commits it and deletes the previous object', async () => {
    const oldKey = `users/${ACTOR}/dddddddd-dddd-4ddd-8ddd-dddddddddddd.jpg`;
    db.users.get(ACTOR)!.key = oldKey;
    storage.objects.set(oldKey, JPEG);
    const ref = await service.replace(actor(), 'corr', ACTOR, upload);
    const newKey = db.users.get(ACTOR)!.key!;
    expect(newKey).toMatch(PROFILE_PHOTO_KEY_PATTERN);
    expect(newKey.startsWith(`users/${ACTOR}/`)).toBe(true);
    expect([...storage.objects.keys()]).toEqual([newKey]);
    expect(ref.etag).toMatch(/^"[0-9a-f]{32}"$/);
    expect(audits).toEqual([
      expect.objectContaining({
        action: 'profile_updated',
        metadata: { field: 'profile_picture', operation: 'replace' },
      }),
    ]);
  });

  it('deletes the new object when the transaction fails', async () => {
    db.failNextKeyUpdate = true;
    await expect(service.replace(actor(), 'corr', ACTOR, upload)).rejects.toThrow(
      'simulated failure',
    );
    expect(storage.objects.size).toBe(0);
    expect(db.users.get(ACTOR)!.key).toBeNull();
  });

  it('retries cleanup and audits a persistent cleanup failure without failing the request', async () => {
    const oldKey = `users/${ACTOR}/dddddddd-dddd-4ddd-8ddd-dddddddddddd.jpg`;
    db.users.get(ACTOR)!.key = oldKey;
    storage.objects.set(oldKey, JPEG);
    storage.failDeletes = 3;
    await service.replace(actor(), 'corr', ACTOR, upload);
    expect(storage.objects.has(oldKey)).toBe(true);
    expect(audits.at(-1)).toMatchObject({
      metadata: { operation: 'cleanup_failed', object_ref: oldKey },
    });
  });

  it('rejects invalid content before touching storage or the database', async () => {
    await expect(
      service.replace(actor(), 'corr', ACTOR, {
        fileName: 'x.png',
        declaredMimeType: 'image/png',
        bytes: HTML,
      }),
    ).rejects.toBeInstanceOf(UserValidationException);
    expect(storage.objects.size).toBe(0);
  });

  it('lets the single-site custodian change a user photo but not a multi-site user or a SuperAdmin', async () => {
    db.users.set(TARGET, { key: null, superAdmin: false, sites: [SITE_A] });
    await service.replace(actor(), 'corr', TARGET, upload);
    expect(db.users.get(TARGET)!.key).not.toBeNull();

    db.users.set(TARGET, { key: null, superAdmin: false, sites: [SITE_A, SITE_B] });
    await expect(service.replace(actor(), 'corr', TARGET, upload)).rejects.toBeInstanceOf(
      UserAuthorizationException,
    );
    db.users.set(TARGET, { key: null, superAdmin: true, sites: [SITE_A] });
    await expect(service.replace(actor(), 'corr', TARGET, upload)).rejects.toBeInstanceOf(
      UserAuthorizationException,
    );
    expect(storage.objects.size).toBe(1);
  });

  it('remove clears the key and deletes the object; read serves only stored images', async () => {
    await service.replace(actor(), 'corr', ACTOR, upload);
    const content = await service.read(actor(), ACTOR);
    expect(content.contentType).toBe('image/png');
    expect(content.bytes).toEqual(PNG);
    await service.remove(actor(), 'corr', ACTOR);
    expect(db.users.get(ACTOR)!.key).toBeNull();
    expect(storage.objects.size).toBe(0);
    await expect(service.read(actor(), ACTOR)).rejects.toThrow('No profile picture.');
  });

  it('hides other users outside the viewer scope', async () => {
    db.users.set(TARGET, {
      key: `users/${TARGET}/eeeeeeee-eeee-4eee-8eee-eeeeeeeeeeee.png`,
      superAdmin: false,
      sites: [SITE_B],
    });
    await expect(service.read(actor(), TARGET)).rejects.toThrow('User was not found.');
  });
});

describe('Profile photo HTTP wiring', () => {
  let app: NestFastifyApplication;
  let repository: FakeAuthRepository;
  let token: string;
  let csrf: string;

  beforeEach(async () => {
    repository = new FakeAuthRepository();
    app = await createTestApp(repository, createKernel(repository));
    const identity = createIdentity({
      username: 'ana',
      email: 'ana@x.test',
      passwordHash: bcryptHash('Correct-Horse-9!'),
    });
    repository.addIdentity(identity, [createMembership(createSite(), { userId: identity.id })]);
    token = (
      await app
        .get(AuthService)
        .login({ loginIdentifier: 'ana', password: 'Correct-Horse-9!' }, '127.0.0.1')
    ).token;
    csrf = JSON.parse(
      (await app.inject({ method: 'GET', url: '/api/v1/auth/csrf' })).payload,
    ).csrfToken;
  });

  afterEach(async () => {
    await app.close();
  });

  function put(contentType: string, body: Buffer, fileName = 'foto.png', withSession = true) {
    return app.inject({
      method: 'PUT',
      url: '/api/v1/profile/photo',
      headers: {
        origin: TRUSTED_ORIGIN,
        'x-csrf-token': csrf,
        'content-type': contentType,
        'x-photo-filename': encodeURIComponent(fileName),
        cookie:
          `__Host-lu_csrf=${encodeURIComponent(csrf)}` +
          (withSession ? `; __Host-lu_session=${token}` : ''),
      },
      payload: body,
    });
  }

  it('requires a session', async () => {
    expect((await put('image/png', PNG, 'foto.png', false)).statusCode).toBe(401);
  });

  it('accepts only the three image media types', async () => {
    expect((await put('image/svg+xml', Buffer.from('<svg/>'), 'a.svg')).statusCode).toBe(415);
    expect((await put('text/html', HTML, 'a.png')).statusCode).toBe(415);
  });

  it('enforces the 5 MiB limit at the transport', async () => {
    const response = await put(
      'image/png',
      Buffer.concat([PNG, Buffer.alloc(PROFILE_PHOTO_MAX_BYTES)]),
    );
    expect(response.statusCode).toBe(413);
  });

  it('rejects mismatched content with 400 and never stores it', async () => {
    const response = await put('image/png', HTML, 'foto.png');
    expect(response.statusCode).toBe(400);
    expect(JSON.parse(response.payload).error.fieldErrors).toEqual({
      photo: ['signature_mismatch'],
    });
  });

  it('keeps JSON bodies on the default parser', async () => {
    const response = await app.inject({
      method: 'PUT',
      url: '/api/v1/profile/photo',
      headers: {
        origin: TRUSTED_ORIGIN,
        'x-csrf-token': csrf,
        'content-type': 'application/json',
        cookie: `__Host-lu_csrf=${encodeURIComponent(csrf)}; __Host-lu_session=${token}`,
      },
      payload: { photo: 'not-bytes' },
    });
    expect(response.statusCode).toBe(400);
  });
});
