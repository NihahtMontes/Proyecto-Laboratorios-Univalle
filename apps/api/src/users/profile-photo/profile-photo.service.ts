/**
 * Profile picture use cases (F1 §10, §12, §14).
 *
 * - Who: the owner (self-service), a SuperAdmin, or a site Administrator who is
 *   the transactionally verified single-site custodian of a non-SuperAdmin
 *   target — the same rule as the other cosmetic global fields.
 * - Replace: validate, store the new object under a server-generated key,
 *   commit the key, then delete the previous object. If the transaction fails
 *   the new object is deleted. Cleanup failures are retried and audited.
 * - A photo is cosmetic: it never rotates security_version.
 */
import { Inject, Injectable } from '@nestjs/common';
import { randomUUID } from 'node:crypto';
import { AUTH_PG_POOL } from '../../auth/auth.constants.js';
import type { IPgPool } from '../../auth/auth.pg-pool.js';
import {
  IDENTITY_AUDIT_WRITER,
  type IdentityAuditWriter,
  type RequestIdentity,
} from '../../identity/identity.contracts.js';
import { UserAccessPolicy, type CustodyProbe } from '../user.policy.js';
import { profilePictureRef } from '../user.projection.js';
import { UserRepository } from '../user.repository.js';
import {
  UserAuthorizationException,
  UserNotFoundException,
  UserValidationException,
} from '../user.service.js';
import type { ProfilePictureRef } from '../user.types.js';
import { ProfilePhotoRepository } from './profile-photo.repository.js';
import { PROFILE_PHOTO_STORAGE, type ProfilePhotoStorage } from './profile-photo.storage.js';
import {
  PROFILE_PHOTO_MIME,
  detectProfilePhotoFormat,
  validateProfilePhoto,
  type ProfilePhotoUpload,
} from './profile-photo.validator.js';

const CLEANUP_ATTEMPTS = 3;

export interface ProfilePhotoContent {
  readonly bytes: Buffer;
  readonly contentType: string;
  readonly etag: string;
}

@Injectable()
export class ProfilePhotoService {
  private readonly policy = new UserAccessPolicy();

  constructor(
    @Inject(AUTH_PG_POOL) private readonly pool: IPgPool,
    @Inject(PROFILE_PHOTO_STORAGE) private readonly storage: ProfilePhotoStorage,
    @Inject(IDENTITY_AUDIT_WRITER) private readonly audit: IdentityAuditWriter,
    private readonly photos: ProfilePhotoRepository,
    private readonly users: UserRepository,
  ) {}

  async replace(
    actor: RequestIdentity,
    correlationId: string,
    targetUserId: string,
    upload: ProfilePhotoUpload,
  ): Promise<ProfilePictureRef> {
    const validation = validateProfilePhoto(upload);
    if (!validation.ok) {
      throw new UserValidationException('The profile picture is not acceptable.', {
        photo: [validation.violation],
      });
    }
    // Pre-check so unauthorized callers never cause an object write; the locked
    // check inside the transaction below remains authoritative.
    await this.authorizeChange(actor, targetUserId, this.pool);
    const newKey = `users/${targetUserId}/${randomUUID()}.${validation.extension}`;
    await this.storage.put(newKey, upload.bytes);

    let previousKey: string | null;
    try {
      previousKey = await this.pool.transaction(async (client) => {
        await this.authorizeChange(actor, targetUserId, client);
        const current = await client.query<{ profile_picture_key: string | null }>(
          'SELECT profile_picture_key FROM public.lu_user WHERE id = $1 FOR UPDATE',
          [targetUserId],
        );
        const row = current.rows[0];
        if (row === undefined) throw new UserNotFoundException();
        await this.photos.setKey(client, targetUserId, newKey, actor.userId);
        await this.audit.append(client, {
          action: 'profile_updated',
          subjectUserId: targetUserId,
          actorUserId: actor.userId,
          correlationId,
          metadata: { field: 'profile_picture', operation: 'replace' },
        });
        return row.profile_picture_key;
      });
    } catch (error) {
      await this.cleanup(newKey, targetUserId, actor, correlationId);
      throw error;
    }
    if (previousKey !== null && previousKey !== newKey) {
      await this.cleanup(previousKey, targetUserId, actor, correlationId);
    }
    return profilePictureRef(newKey)!;
  }

  async remove(actor: RequestIdentity, correlationId: string, targetUserId: string): Promise<void> {
    const previousKey = await this.pool.transaction(async (client) => {
      await this.authorizeChange(actor, targetUserId, client);
      const current = await client.query<{ profile_picture_key: string | null }>(
        'SELECT profile_picture_key FROM public.lu_user WHERE id = $1 FOR UPDATE',
        [targetUserId],
      );
      const row = current.rows[0];
      if (row === undefined) throw new UserNotFoundException();
      if (row.profile_picture_key === null) return null;
      await this.photos.setKey(client, targetUserId, null, actor.userId);
      await this.audit.append(client, {
        action: 'profile_updated',
        subjectUserId: targetUserId,
        actorUserId: actor.userId,
        correlationId,
        metadata: { field: 'profile_picture', operation: 'remove' },
      });
      return row.profile_picture_key;
    });
    if (previousKey !== null) {
      await this.cleanup(previousKey, targetUserId, actor, correlationId);
    }
  }

  /** Serves the picture to anyone allowed to view the user (F1 §14). */
  async read(actor: RequestIdentity, targetUserId: string): Promise<ProfilePhotoContent> {
    if (actor.userId !== targetUserId) {
      const target = await this.users.findGlobal(targetUserId);
      if (target === null) throw new UserNotFoundException();
      const memberships = await this.users.listMembershipsForUser(targetUserId);
      const decision = this.policy.canViewUser(
        actor,
        {
          id: target.id,
          accountStatus: target.account_status,
          isSuperAdmin: target.is_super_admin,
        },
        memberships.map((m) => ({ siteId: m.site_id, role: m.role, status: m.membership_status })),
      );
      if (!decision.allowed) throw new UserNotFoundException();
    }
    const stored = await this.photos.findKey(targetUserId);
    if (stored === null || stored.key === null)
      throw new UserNotFoundException('No profile picture.');
    const bytes = await this.storage.get(stored.key);
    const format = bytes === null ? null : detectProfilePhotoFormat(bytes);
    if (bytes === null || format === null) throw new UserNotFoundException('No profile picture.');
    return {
      bytes,
      contentType: PROFILE_PHOTO_MIME[format],
      etag: profilePictureRef(stored.key)!.etag,
    };
  }

  private async authorizeChange(
    actor: RequestIdentity,
    targetUserId: string,
    client: Parameters<Parameters<IPgPool['transaction']>[0]>[0],
  ): Promise<void> {
    if (actor.userId === targetUserId) return; // self-service subset (F1 §9)
    const target = await this.users.findGlobalForUpdate(client, targetUserId);
    if (target === null) throw new UserNotFoundException();
    const memberships = await this.users.listMembershipsForUpdate(client, targetUserId);
    const custody: CustodyProbe = {
      subject: {
        id: target.id,
        accountStatus: target.account_status,
        isSuperAdmin: target.is_super_admin,
      },
      nonRevokedMemberships: memberships
        .filter((m) => m.membership_status !== 'revoked')
        .map((m) => ({ siteId: m.site_id, role: m.role, status: m.membership_status })),
      now: new Date(),
    };
    const decision = this.policy.canUpdateGlobalFields(actor, custody);
    if (!decision.allowed) {
      throw new UserAuthorizationException(decision.reason, decision.reason);
    }
  }

  /** Deletes an object with retries; a persistent failure is audited, never thrown. */
  private async cleanup(
    key: string,
    subjectUserId: string,
    actor: RequestIdentity,
    correlationId: string,
  ): Promise<void> {
    for (let attempt = 1; attempt <= CLEANUP_ATTEMPTS; attempt += 1) {
      try {
        await this.storage.delete(key);
        return;
      } catch {
        // retry
      }
    }
    await this.audit
      .append(this.pool, {
        action: 'profile_updated',
        subjectUserId,
        actorUserId: actor.userId,
        correlationId,
        metadata: { field: 'profile_picture', operation: 'cleanup_failed', object_ref: key },
      })
      .catch(() => undefined);
  }
}
