/**
 * MIG-001 F3 user administration service.
 *
 * Splits the previous unconditional mixed-scope update into separate global
 * and membership commands (F1 section 4, handoff F3 item 4). Each command:
 *  1. Validates its DTO before any DB work.
 *  2. Locks target user + relevant membership rows FOR UPDATE.
 *  3. Re-runs the UserAccessPolicy against the live counts (single-site
 *     custody and self-protection are never client-asserted).
 *  4. For changes that could reduce the count of active SuperAdmins,
 *     acquires pg_advisory_xact_lock(<fixed key>) and re-counts.
 *  5. Calls SESSION_INVALIDATOR.rotateAndRevokeAll for sensitive changes.
 *  6. Writes an IDENTITY_AUDIT_WRITER event with actor / subject / reason /
 *     before+after account_status; never secrets.
 */
import { Inject, Injectable } from '@nestjs/common';
import { randomUUID } from 'node:crypto';
import { AUTH_PG_POOL } from '../auth/auth.constants.js';
import type { IPgPool } from '../auth/auth.pg-pool.js';
import type { FastifyReply } from '../auth/auth.fastify.js';
import {
  IDENTITY_AUDIT_WRITER,
  PASSWORD_HASHER,
  PASSWORD_POLICY,
  SELF_SESSION_RENEWER,
  SESSION_INVALIDATOR,
  type AccountStatus,
  type IdentityAuditAction,
  type IdentityAuditWriter,
  type PasswordHasher,
  type PasswordPolicy,
  type RequestIdentity,
  type SelfSessionRenewer,
  type SessionInvalidator,
} from '../identity/identity.contracts.js';
import { MembershipRepository } from './membership.repository.js';
import {
  type CustodyProbe,
  type PolicyMembershipSummary,
  type PolicySubjectSummary,
  UserAccessPolicy,
} from './user.policy.js';
import {
  type AddMembershipInput,
  type AdminResetPasswordInput,
  type ChangeRoleInput,
  type ChangeStatusInput,
  type ChangeValidityInput,
  type CreateUserGlobalInput,
  type CreateUserSiteAdminInput,
  type GlobalDeleteInput,
  type ManagedAccountStatus,
  type ManagedUserPage,
  type ManagedUserQuery,
  type ManagedUserRecord,
  type ProfileRecord,
  type RestoreAccountInput,
  type RestoreMembershipInput,
  type RevokeActiveSiteInput,
  type SetAccountStatusInput,
  type UpdateGlobalFieldsInput,
  type UpdateProfileInput,
  type UpdateWorkProfileInput,
  SUPERADMIN_FIXED_ADVISORY_KEY,
} from './user.types.js';
import { UserRepository, type UserRow } from './user.repository.js';
import {
  activeRoleOf,
  initialsOf,
  iso,
  mapMemberships,
  profilePictureRef,
} from './user.projection.js';

export class UserValidationException extends Error {
  constructor(
    message: string,
    public readonly fieldErrors?: Readonly<Record<string, readonly string[]>>,
  ) {
    super(message);
    this.name = 'UserValidationException';
  }
}

export class UserAuthorizationException extends Error {
  constructor(
    message: string,
    public readonly code: string,
  ) {
    super(message);
    this.name = 'UserAuthorizationException';
  }
}

export class UserNotFoundException extends Error {
  constructor(message = 'User was not found.') {
    super(message);
    this.name = 'UserNotFoundException';
  }
}

export class UserConflictException extends Error {
  constructor(
    message: string,
    public readonly field: string,
  ) {
    super(message);
    this.name = 'UserConflictException';
  }
}

export interface UserCommandContext {
  readonly actor: RequestIdentity;
  readonly correlationId: string;
  readonly reply: FastifyReply | null;
}

export type UserListPage = ManagedUserPage;

const SUPERADMIN_LOCK_KEY = SUPERADMIN_FIXED_ADVISORY_KEY;

/** Audit reason that marks an account deleted by the F1 §11 site-revoke cascade. */
export const SITE_REVOKE_CASCADE_REASON = 'site_revoke_cascade';

function toStoredDate(value: Date | string | null | undefined): Date | null {
  if (value === null || value === undefined) return null;
  return value instanceof Date ? value : new Date(value);
}

function isUniqueViolation(error: unknown): boolean {
  return (
    typeof error === 'object' && error !== null && (error as { code?: unknown }).code === '23505'
  );
}

function toDateOrNull(value: string | null | undefined): Date | null {
  if (value === undefined || value === null) return null;
  if (value === '') return null;
  const parsed = new Date(value);
  if (Number.isNaN(parsed.getTime())) {
    throw new UserValidationException('Date value is invalid.');
  }
  return parsed;
}

function nfkcNormalize(value: string): string {
  return value.normalize('NFKC');
}

function nfcNormalize(value: string): string {
  return value.normalize('NFC');
}

function normalizeSplitName(value: string): string {
  if (value === '') return '';
  return nfcNormalize(value.trim()).replace(/\s+/g, ' ');
}

function normalizeEmail(value: string): string {
  return nfkcNormalize(value.trim()).toLowerCase();
}

function normalizeUsername(value: string): string {
  return nfkcNormalize(value.trim()).toLowerCase();
}

function normalizeIdentityCard(value: string): string {
  return nfkcNormalize(value.trim()).toUpperCase();
}

function validateName(value: string, field: string): string {
  const trimmed = normalizeSplitName(value);
  if (trimmed === '') {
    throw new UserValidationException(`${field} is required.`, { [field]: ['is required'] });
  }
  if (trimmed.length > 100) {
    throw new UserValidationException(`${field} is too long.`, {
      [field]: ['must be at most 100 characters'],
    });
  }
  return trimmed;
}

function validateOptionalName(value: string | null | undefined, field: string): string | null {
  if (value === undefined || value === null) return null;
  if (typeof value !== 'string') {
    throw new UserValidationException(`${field} must be a string.`, {
      [field]: ['must be a string'],
    });
  }
  const trimmed = normalizeSplitName(value);
  if (trimmed === '') return null;
  if (trimmed.length > 100) {
    throw new UserValidationException(`${field} is too long.`, {
      [field]: ['must be at most 100 characters'],
    });
  }
  return trimmed;
}

function validateEmailField(value: string): string {
  if (value === '') {
    throw new UserValidationException('email is required.', { email: ['is required'] });
  }
  const trimmed = normalizeEmail(value);
  if (trimmed.length > 256) {
    throw new UserValidationException('email is too long.', {
      email: ['must be at most 256 characters'],
    });
  }
  if (!/^[^\s@]+@[^\s@]+\.[^\s@]+$/.test(trimmed)) {
    throw new UserValidationException('email is invalid.', { email: ['must be a valid email'] });
  }
  return trimmed;
}

function validateUsernameField(value: string): string {
  if (value === '') {
    throw new UserValidationException('username is required.', { username: ['is required'] });
  }
  const trimmed = normalizeUsername(value);
  if (trimmed.length < 1 || trimmed.length > 256) {
    throw new UserValidationException('username is invalid.', {
      username: ['must be between 1 and 256 characters'],
    });
  }
  return trimmed;
}

function validateIdentityCardField(value: string): string {
  const trimmed = normalizeIdentityCard(value);
  if (trimmed.length > 10) {
    throw new UserValidationException('identityCard is too long.', {
      identityCard: ['must be at most 10 characters'],
    });
  }
  if (!/^[0-9A-Z-]+$/.test(trimmed)) {
    throw new UserValidationException('identityCard is invalid.', {
      identityCard: ['must match [0-9A-Z-]'],
    });
  }
  return trimmed;
}

function validatePhoneField(value: string): string {
  const trimmed = value.trim();
  if (trimmed.length === 0) {
    throw new UserValidationException('phoneNumber is required.', {
      phoneNumber: ['is required'],
    });
  }
  if (trimmed.length > 30) {
    throw new UserValidationException('phoneNumber is too long.', {
      phoneNumber: ['must be at most 30 characters'],
    });
  }
  if (!/^[+]?[0-9 ().-]+$/.test(trimmed)) {
    throw new UserValidationException('phoneNumber is invalid.', {
      phoneNumber: [
        'must contain only digits, spaces, dot, hyphen, parentheses and an optional leading +',
      ],
    });
  }
  const digits = trimmed.replace(/[^0-9]/g, '').length;
  if (digits < 7 || digits > 15) {
    throw new UserValidationException('phoneNumber is invalid.', {
      phoneNumber: ['must contain between 7 and 15 digits'],
    });
  }
  return trimmed;
}

function validateRole(value: unknown): 'Administrador' | 'Supervisor' {
  if (value !== 'Administrador' && value !== 'Supervisor') {
    throw new UserValidationException('role is invalid.', {
      role: ['must be Administrador or Supervisor'],
    });
  }
  return value;
}

function validatePasswordAgainstPolicy(policy: PasswordPolicy, plaintext: string): void {
  const violations = policy.validate(plaintext);
  if (violations.length > 0) {
    throw new UserValidationException('password does not satisfy the policy.', {
      password: violations.map(stringifyViolation),
    });
  }
}

function stringifyViolation(v: string): string {
  switch (v) {
    case 'too_short':
      return 'must be at least 12 characters';
    case 'too_long':
      return 'must be at most 72 UTF-8 bytes';
    case 'missing_uppercase':
      return 'must contain an uppercase letter';
    case 'missing_lowercase':
      return 'must contain a lowercase letter';
    case 'missing_digit':
      return 'must contain a digit';
    case 'missing_symbol':
      return 'must contain a non-alphanumeric character';
    case 'insufficient_distinct':
      return 'must contain at least 4 distinct code points';
    default:
      return 'is invalid';
  }
}

@Injectable()
export class UserService {
  readonly policy = new UserAccessPolicy();

  constructor(
    @Inject(AUTH_PG_POOL) private readonly pool: IPgPool,
    @Inject(PASSWORD_POLICY) private readonly passwordPolicy: PasswordPolicy,
    @Inject(PASSWORD_HASHER) private readonly passwordHasher: PasswordHasher,
    @Inject(SESSION_INVALIDATOR) private readonly sessionInvalidator: SessionInvalidator,
    @Inject(IDENTITY_AUDIT_WRITER) private readonly audit: IdentityAuditWriter,
    @Inject(SELF_SESSION_RENEWER) private readonly selfSessionRenewer: SelfSessionRenewer,
    private readonly userRepository: UserRepository,
    private readonly membershipRepository: MembershipRepository,
  ) {}

  /**
   * HTTP list. Site Admin sees only its active-site membership projection;
   * SuperAdmin without a site sees global users; SuperAdmin WITH an active
   * site still gets the global view. Explicit status filter can include
   * deleted accounts; default excludes them. Revoked memberships are always
   * excluded unless the caller asks for `includeRevoked` via status=deleted.
   */
  async list(ctx: UserCommandContext, query: ManagedUserQuery): Promise<ManagedUserPage> {
    const decision = this.policy.canListUsers(ctx.actor);
    if (!decision.allowed) {
      throw new UserAuthorizationException(decision.reason, decision.reason);
    }
    const filter = {
      currentPage: query.currentPage,
      statusFilter: query.statusFilter as AccountStatus | undefined,
      searchTerm: query.searchTerm?.trim().slice(0, 200) || undefined,
    };
    const now = new Date();

    if (!ctx.actor.isSuperAdmin) {
      // F1 §17: a site Administrator sees only the active-site membership
      // projection; filtering, paging and totals happen in SQL for that site.
      const siteId = ctx.actor.activeSiteId;
      if (siteId === null) {
        throw new UserAuthorizationException('SITE_ACCESS_DENIED', 'SITE_ACCESS_DENIED');
      }
      const page = await this.userRepository.listForSite(siteId, filter);
      return {
        items: page.items.map((row) =>
          this.toRecord(row, mapMemberships([row], now, row.account_status), row.role),
        ),
        totalCount: page.totalCount,
        pageIndex: page.pageIndex,
        totalPages: page.totalPages,
        pageSize: page.pageSize,
      };
    }

    // SuperAdmin: global account view (deleted accounts hidden by default).
    const page = await this.userRepository.listGlobal({
      ...filter,
      excludeDeleted: filter.statusFilter === undefined,
    });
    const items: ManagedUserRecord[] = [];
    for (const row of page.items) {
      const memberships = await this.userRepository.listMembershipsForUser(row.id);
      const activeRole =
        memberships.find(
          (m) => m.site_id === ctx.actor.activeSiteId && m.membership_status !== 'revoked',
        )?.role ?? null;
      items.push(
        this.toRecord(row, mapMemberships(memberships, now, row.account_status), activeRole),
      );
    }
    return {
      items,
      totalCount: page.totalCount,
      pageIndex: page.pageIndex,
      totalPages: page.totalPages,
      pageSize: page.pageSize,
    };
  }

  async find(ctx: UserCommandContext, id: string): Promise<ManagedUserRecord> {
    const row = await this.userRepository.findGlobal(id);
    if (row === null) throw new UserNotFoundException();
    const memberships = await this.userRepository.listMembershipsForUser(id);
    const decision = this.policy.canViewUser(
      ctx.actor,
      { id: row.id, accountStatus: row.account_status, isSuperAdmin: row.is_super_admin },
      memberships.map((m) => ({ siteId: m.site_id, role: m.role, status: m.membership_status })),
    );
    if (!decision.allowed) {
      // Indistinguishable from an unknown id for callers outside the scope.
      throw new UserNotFoundException();
    }
    // A site Administrator sees only the membership of its active site.
    const visible = ctx.actor.isSuperAdmin
      ? memberships
      : memberships.filter((m) => m.site_id === ctx.actor.activeSiteId);
    const activeRole =
      memberships.find(
        (m) => m.site_id === ctx.actor.activeSiteId && m.membership_status !== 'revoked',
      )?.role ?? null;
    const actors = await this.userRepository.findAuditActors(id);
    return {
      ...this.toRecord(row, mapMemberships(visible, new Date(), row.account_status), activeRole),
      createdBy: actors.createdBy,
      modifiedBy: actors.modifiedBy,
    };
  }

  // -------------------------------------------------------------------------
  // createUser (global): site Admin creates non-SuperAdmin + active-site
  // membership. SuperAdmin creates non-SuperAdmin globally + explicit
  // memberships. The HTTP layer always strips isSuperAdmin; this method is the
  // gatekeeper.
  // -------------------------------------------------------------------------

  async createSite(
    ctx: UserCommandContext,
    input: CreateUserSiteAdminInput,
  ): Promise<ManagedUserRecord> {
    if (ctx.actor.activeSiteId === null || ctx.actor.activeSiteRole !== 'Administrador') {
      throw new UserAuthorizationException('SITE_ACCESS_DENIED', 'SITE_ACCESS_DENIED');
    }
    const decision = this.policy.canCreateUser(ctx.actor, { isSuperAdminRequested: false });
    if (!decision.allowed) {
      throw new UserAuthorizationException(decision.reason, decision.reason);
    }
    return this.createWithMemberships(ctx, {
      username: input.username,
      email: input.email,
      firstName: input.firstName,
      lastName: input.lastName,
      secondLastName: input.secondLastName ?? null,
      identityCard: input.identityCard,
      phoneNumber: input.phoneNumber,
      password: input.password,
      memberships: [
        {
          siteId: ctx.actor.activeSiteId,
          role: input.role,
          validFrom: null,
          validUntil: null,
        },
      ],
    });
  }

  async createGlobal(
    ctx: UserCommandContext,
    input: CreateUserGlobalInput,
  ): Promise<ManagedUserRecord> {
    const decision = this.policy.canCreateUser(ctx.actor, { isSuperAdminRequested: false });
    if (!decision.allowed) {
      throw new UserAuthorizationException(decision.reason, decision.reason);
    }
    return this.createWithMemberships(ctx, input);
  }

  private async createWithMemberships(
    ctx: UserCommandContext,
    input: CreateUserGlobalInput,
  ): Promise<ManagedUserRecord> {
    const username = validateUsernameField(input.username);
    const email = validateEmailField(input.email);
    const firstName = validateName(input.firstName, 'firstName');
    const lastName = validateName(input.lastName, 'lastName');
    const secondLastName = validateOptionalName(input.secondLastName ?? null, 'secondLastName');
    const identityCard = validateIdentityCardField(input.identityCard);
    const phoneNumber = validatePhoneField(input.phoneNumber);
    validatePasswordAgainstPolicy(this.passwordPolicy, input.password);
    if (input.memberships.length === 0) {
      throw new UserValidationException('At least one membership is required.');
    }
    for (const m of input.memberships) {
      if (m.siteId === undefined || m.siteId === '') {
        throw new UserValidationException('membership siteId is required.');
      }
      validateRole(m.role);
    }

    const userId = randomUUID();
    const passwordHash = await this.passwordHasher.hash(input.password);

    return this.pool.transaction(async (client) => {
      let row: UserRow;
      try {
        row = await this.userRepository.insertUser(client, {
          id: userId,
          email,
          username,
          firstName,
          lastName,
          secondLastName,
          identityCard,
          phoneNumber,
          passwordHash,
          mustChangePassword: true,
          createdByUserId: ctx.actor.userId,
        });
      } catch (error) {
        if (isUniqueViolation(error)) {
          throw new UserConflictException('A conflicting record already exists.', 'identity');
        }
        throw error;
      }
      for (const membership of input.memberships) {
        try {
          await this.membershipRepository.insertMembership(client, {
            userId: row.id,
            siteId: membership.siteId,
            role: membership.role,
            status: 'active',
            validFrom: toDateOrNull(membership.validFrom ?? null),
            validUntil: toDateOrNull(membership.validUntil ?? null),
            createdByUserId: ctx.actor.userId,
          });
        } catch (error) {
          if (isUniqueViolation(error)) {
            throw new UserConflictException('A conflicting record already exists.', 'membership');
          }
          throw error;
        }
      }
      await this.audit.append(client, {
        action: 'user_created',
        subjectUserId: row.id,
        actorUserId: ctx.actor.userId,
        correlationId: ctx.correlationId,
        afterStatus: 'active',
        metadata: {
          membershipCount: input.memberships.length,
        },
      });
      // No session invalidation: a freshly created user has no sessions.
      const memberships = await this.userRepository.listMembershipsForUpdate(client, row.id);
      return this.toRecord(
        row,
        mapMemberships(memberships, new Date(), row.account_status),
        activeRoleOf(memberships, ctx.actor),
      );
    });
  }

  // -------------------------------------------------------------------------
  // updateGlobalFields: email, names, phone (always). identityCard is the
  // privileged path (re-checked inside). Username is immutable.
  // -------------------------------------------------------------------------

  async updateGlobalFields(
    ctx: UserCommandContext,
    id: string,
    input: UpdateGlobalFieldsInput,
  ): Promise<ManagedUserRecord> {
    const result = await this.pool.transaction(async (client) => {
      const target = await this.userRepository.findGlobalForUpdate(client, id);
      if (target === null) throw new UserNotFoundException();

      const subject: PolicySubjectSummary = {
        id: target.id,
        accountStatus: target.account_status,
        isSuperAdmin: target.is_super_admin,
      };
      const memberships = await this.userRepository.listMembershipsForUpdate(client, id);
      const nonRevokedMemberships = memberships.filter((m) => m.membership_status !== 'revoked');
      const custody: CustodyProbe = {
        subject,
        nonRevokedMemberships: nonRevokedMemberships.map<PolicyMembershipSummary>((m) => ({
          siteId: m.site_id,
          role: m.role,
          status: m.membership_status,
        })),
        now: new Date(),
      };

      if (input.identityCard !== undefined) {
        const decision = this.policy.canUpdateIdentityCard(ctx.actor, custody);
        if (!decision.allowed) {
          throw new UserAuthorizationException(decision.reason, decision.reason);
        }
      } else {
        const decision = this.policy.canUpdateGlobalFields(ctx.actor, custody);
        if (!decision.allowed) {
          throw new UserAuthorizationException(decision.reason, decision.reason);
        }
      }

      let emailChanged = false;
      const sets: Parameters<typeof this.userRepository.updateGlobalFields>[1] = {
        id: target.id,
        modifiedByUserId: ctx.actor.userId,
      };
      if (input.email !== undefined) {
        const next = validateEmailField(input.email);
        if (next !== target.email) emailChanged = true;
        sets.email = next;
      }
      if (input.firstName !== undefined) {
        sets.firstName = validateName(input.firstName, 'firstName');
      }
      if (input.lastName !== undefined) {
        sets.lastName = validateName(input.lastName, 'lastName');
      }
      if (input.secondLastName !== undefined) {
        sets.secondLastName = validateOptionalName(input.secondLastName, 'secondLastName');
      }
      if (input.identityCard !== undefined) {
        sets.identityCard = validateIdentityCardField(input.identityCard);
      }
      if (input.phoneNumber !== undefined) {
        sets.phoneNumber = validatePhoneField(input.phoneNumber);
      }

      await this.userRepository.updateGlobalFields(client, sets);

      const reason = input.reason?.trim() || null;
      if (emailChanged) {
        await this.sessionInvalidator.rotateAndRevokeAll(client, id, 'email_change');
        await this.audit.append(client, {
          action: 'email_changed',
          subjectUserId: id,
          actorUserId: ctx.actor.userId,
          correlationId: ctx.correlationId,
          reason,
          metadata: { changedField: 'email' },
        });
      } else {
        await this.audit.append(client, {
          action: 'profile_updated',
          subjectUserId: id,
          actorUserId: ctx.actor.userId,
          correlationId: ctx.correlationId,
          reason,
          metadata: { changedFields: this.changedFields(input).join(',') },
        });
      }

      const refreshed = await this.userRepository.findGlobalForUpdate(client, id);
      if (refreshed === null) throw new UserNotFoundException();
      const refreshedMemberships = await this.userRepository.listMembershipsForUpdate(client, id);
      const visible = ctx.actor.isSuperAdmin
        ? refreshedMemberships
        : refreshedMemberships.filter((m) => m.site_id === ctx.actor.activeSiteId);
      return {
        emailChanged,
        record: this.toRecord(
          refreshed,
          mapMemberships(visible, new Date(), refreshed.account_status),
          activeRoleOf(refreshedMemberships, ctx.actor),
        ),
      };
    });
    // F1 §12 "replace current if self": only after the rotation has committed.
    if (result.emailChanged && ctx.actor.userId === id) {
      await this.renewSelfSession(ctx);
    }
    return result.record;
  }

  private async renewSelfSession(ctx: UserCommandContext): Promise<void> {
    if (ctx.reply !== null) {
      await this.selfSessionRenewer.renew(ctx.reply, ctx.actor);
    }
  }

  private changedFields(input: UpdateGlobalFieldsInput): string[] {
    const fields: string[] = [];
    if (input.email !== undefined) fields.push('email');
    if (input.firstName !== undefined) fields.push('firstName');
    if (input.lastName !== undefined) fields.push('lastName');
    if (input.secondLastName !== undefined) fields.push('secondLastName');
    if (input.identityCard !== undefined) fields.push('identityCard');
    if (input.phoneNumber !== undefined) fields.push('phoneNumber');
    return fields;
  }

  // -------------------------------------------------------------------------
  // adminResetPassword: bcrypt at current cost, must_change_password=true,
  // password_migrated_at only if previous scheme was legacy/reset_required,
  // rotate admin_password_reset.
  // -------------------------------------------------------------------------

  async adminResetPassword(
    ctx: UserCommandContext,
    id: string,
    input: AdminResetPasswordInput,
  ): Promise<void> {
    validatePasswordAgainstPolicy(this.passwordPolicy, input.password);

    await this.pool.transaction(async (client) => {
      const target = await this.userRepository.findGlobalForUpdate(client, id);
      if (target === null) throw new UserNotFoundException();

      const memberships = await this.userRepository.listMembershipsForUpdate(client, id);
      const nonRevokedMemberships = memberships.filter((m) => m.membership_status !== 'revoked');
      const custody: CustodyProbe = {
        subject: {
          id: target.id,
          accountStatus: target.account_status,
          isSuperAdmin: target.is_super_admin,
        },
        nonRevokedMemberships: nonRevokedMemberships.map<PolicyMembershipSummary>((m) => ({
          siteId: m.site_id,
          role: m.role,
          status: m.membership_status,
        })),
        now: new Date(),
      };
      const decision = this.policy.canAdminResetPassword(ctx.actor, custody);
      if (!decision.allowed) {
        throw new UserAuthorizationException(decision.reason, decision.reason);
      }

      const hash = await this.passwordHasher.hash(input.password);
      const passwordMigratedAt =
        target.password_scheme === 'legacy_identity_v2' ||
        target.password_scheme === 'legacy_identity_v3' ||
        target.password_scheme === 'reset_required';

      await this.userRepository.adminResetPassword(client, {
        id,
        passwordHash: hash,
        mustChangePassword: true,
        passwordMigratedAt,
        modifiedByUserId: ctx.actor.userId,
      });

      await this.sessionInvalidator.rotateAndRevokeAll(client, id, 'admin_password_reset');
      await this.audit.append(client, {
        action: 'password_reset',
        subjectUserId: id,
        actorUserId: ctx.actor.userId,
        correlationId: ctx.correlationId,
        reason: input.reason?.trim() || null,
        metadata: { previousScheme: target.password_scheme },
      });
    });
  }

  // -------------------------------------------------------------------------
  // setAccountStatus: active/inactive (deleted goes through globalDelete).
  // Self-protection and last-active-SuperAdmin guard are enforced here.
  // -------------------------------------------------------------------------

  async setAccountStatus(
    ctx: UserCommandContext,
    id: string,
    input: SetAccountStatusInput,
  ): Promise<void> {
    const next: ManagedAccountStatus = input.accountStatus;
    await this.pool.transaction(async (client) => {
      await client.query(`SELECT pg_advisory_xact_lock(hashtext($1))`, [SUPERADMIN_LOCK_KEY]);

      const target = await this.userRepository.findGlobalForUpdate(client, id);
      if (target === null) throw new UserNotFoundException();

      const memberships = await this.userRepository.listMembershipsForUpdate(client, id);
      const nonRevokedMemberships = memberships.filter((m) => m.membership_status !== 'revoked');
      const custody: CustodyProbe = {
        subject: {
          id: target.id,
          accountStatus: target.account_status,
          isSuperAdmin: target.is_super_admin,
        },
        nonRevokedMemberships: nonRevokedMemberships.map<PolicyMembershipSummary>((m) => ({
          siteId: m.site_id,
          role: m.role,
          status: m.membership_status,
        })),
        now: new Date(),
      };
      const decision = this.policy.canSetAccountStatus(
        ctx.actor,
        { id: target.id, isSuperAdmin: target.is_super_admin },
        custody,
      );
      if (!decision.allowed) {
        throw new UserAuthorizationException(decision.reason, decision.reason);
      }

      if (target.is_super_admin && target.account_status === 'active' && next === 'inactive') {
        const remaining = await this.userRepository.countActiveSuperAdminsForUpdate(client);
        const allow = this.policy.canReduceActiveSuperAdminCount(ctx.actor, {
          remainingActiveSuperAdmins: remaining - 1,
        });
        if (!allow.allowed) {
          throw new UserAuthorizationException(allow.reason, allow.reason);
        }
      }

      const beforeStatus: AccountStatus = target.account_status;
      await this.userRepository.setAccountStatus(client, {
        id,
        accountStatus: next,
        modifiedByUserId: ctx.actor.userId,
      });
      await this.sessionInvalidator.rotateAndRevokeAll(client, id, 'account_status_change');
      await this.audit.append(client, {
        action: 'account_status_changed',
        subjectUserId: id,
        actorUserId: ctx.actor.userId,
        correlationId: ctx.correlationId,
        reason: input.reason?.trim() || null,
        beforeStatus,
        afterStatus: next,
      });
    });
  }

  async globalDelete(ctx: UserCommandContext, id: string, input: GlobalDeleteInput): Promise<void> {
    await this.pool.transaction(async (client) => {
      await client.query(`SELECT pg_advisory_xact_lock(hashtext($1))`, [SUPERADMIN_LOCK_KEY]);

      const target = await this.userRepository.findGlobalForUpdate(client, id);
      if (target === null) throw new UserNotFoundException();
      const decision = this.policy.canDeleteAccount(ctx.actor, { id: target.id });
      if (!decision.allowed) {
        throw new UserAuthorizationException(decision.reason, decision.reason);
      }
      if (target.is_super_admin && target.account_status === 'active') {
        const remaining = await this.userRepository.countActiveSuperAdminsForUpdate(client);
        const allow = this.policy.canReduceActiveSuperAdminCount(ctx.actor, {
          remainingActiveSuperAdmins: remaining - 1,
        });
        if (!allow.allowed) {
          throw new UserAuthorizationException(allow.reason, allow.reason);
        }
      }

      const beforeStatus: AccountStatus = target.account_status;
      await this.userRepository.softDeleteAccount(client, {
        id,
        modifiedByUserId: ctx.actor.userId,
      });
      const memberships = await this.membershipRepository.listForUserForUpdate(client, id);
      for (const membership of memberships) {
        if (membership.status !== 'revoked') {
          await this.membershipRepository.revoke(client, {
            userId: id,
            siteId: membership.site_id,
            modifiedByUserId: ctx.actor.userId,
          });
        }
      }
      await this.sessionInvalidator.rotateAndRevokeAll(client, id, 'access_revoked');
      await this.audit.append(client, {
        action: 'account_deleted',
        subjectUserId: id,
        actorUserId: ctx.actor.userId,
        correlationId: ctx.correlationId,
        reason: input.reason?.trim() || null,
        beforeStatus,
        afterStatus: 'deleted',
      });
    });
  }

  async restoreAccount(
    ctx: UserCommandContext,
    id: string,
    input: RestoreAccountInput,
  ): Promise<void> {
    await this.pool.transaction(async (client) => {
      await client.query(`SELECT pg_advisory_xact_lock(hashtext($1))`, [SUPERADMIN_LOCK_KEY]);
      const target = await this.userRepository.findGlobalForUpdate(client, id);
      if (target === null) throw new UserNotFoundException();
      const memberships = await this.userRepository.listMembershipsForUpdate(client, id);
      const nonRevokedMemberships = memberships.filter((m) => m.membership_status !== 'revoked');
      const custody: CustodyProbe = {
        subject: {
          id: target.id,
          accountStatus: target.account_status,
          isSuperAdmin: target.is_super_admin,
        },
        nonRevokedMemberships: nonRevokedMemberships.map<PolicyMembershipSummary>((m) => ({
          siteId: m.site_id,
          role: m.role,
          status: m.membership_status,
        })),
        now: new Date(),
      };
      const decision = this.policy.canRestoreAccount(
        ctx.actor,
        { id: target.id, isSuperAdmin: target.is_super_admin },
        custody,
        target.account_status,
      );
      if (!decision.allowed) {
        throw new UserAuthorizationException(decision.reason, decision.reason);
      }

      const beforeStatus: AccountStatus = target.account_status;
      await this.userRepository.restoreAccount(client, {
        id,
        modifiedByUserId: ctx.actor.userId,
      });
      await this.sessionInvalidator.rotateAndRevokeAll(client, id, 'access_restored');
      await this.audit.append(client, {
        action: 'account_restored',
        subjectUserId: id,
        actorUserId: ctx.actor.userId,
        correlationId: ctx.correlationId,
        reason: input.reason?.trim() || null,
        beforeStatus,
        afterStatus: 'active',
      });
    });
  }

  // -------------------------------------------------------------------------
  // Membership commands. All require the actor to be either a SuperAdmin
  // operating globally or a site Administrator in the target site. Self-
  // protection covers changeRole/changeStatus/revoke of own active site.
  // -------------------------------------------------------------------------

  async addMembership(
    ctx: UserCommandContext,
    userId: string,
    input: AddMembershipInput,
  ): Promise<void> {
    const targetSiteId = input.siteId;
    validateRole(input.role);
    await this.pool.transaction(async (client) => {
      const target = await this.userRepository.findGlobalForUpdate(client, userId);
      if (target === null) throw new UserNotFoundException();
      const decision = this.policy.canManageMembership(
        ctx.actor,
        { id: target.id, isSuperAdmin: target.is_super_admin },
        targetSiteId,
        'add',
      );
      if (!decision.allowed) {
        throw new UserAuthorizationException(decision.reason, decision.reason);
      }
      const existing = await this.membershipRepository.findByUserAndSiteForUpdate(
        client,
        userId,
        targetSiteId,
      );
      if (existing !== null) {
        throw new UserConflictException('Membership already exists.', 'membership');
      }
      await this.membershipRepository.insertMembership(client, {
        userId,
        siteId: targetSiteId,
        role: input.role,
        status: 'active',
        validFrom: toDateOrNull(input.validFrom ?? null),
        validUntil: toDateOrNull(input.validUntil ?? null),
        createdByUserId: ctx.actor.userId,
      });
      await this.sessionInvalidator.rotateAndRevokeAll(client, userId, 'membership_change');
      await this.audit.append(client, {
        action: 'membership_created',
        subjectUserId: userId,
        actorUserId: ctx.actor.userId,
        siteId: targetSiteId,
        correlationId: ctx.correlationId,
        metadata: { role: input.role },
      });
    });
  }

  async changeRole(
    ctx: UserCommandContext,
    userId: string,
    siteId: string,
    input: ChangeRoleInput,
  ): Promise<void> {
    const role = validateRole(input.role);
    await this.pool.transaction(async (client) => {
      const target = await this.userRepository.findGlobalForUpdate(client, userId);
      if (target === null) throw new UserNotFoundException();
      // Authorize first so a site Administrator cannot probe other sites.
      const decision = this.policy.canManageMembership(
        ctx.actor,
        { id: target.id, isSuperAdmin: target.is_super_admin },
        siteId,
        'changeRole',
      );
      if (!decision.allowed) {
        throw new UserAuthorizationException(decision.reason, decision.reason);
      }
      const existing = await this.membershipRepository.findByUserAndSiteForUpdate(
        client,
        userId,
        siteId,
      );
      if (existing === null) throw new UserNotFoundException('Membership was not found.');
      if (existing.status === 'revoked') {
        throw new UserConflictException(
          'The membership is revoked; restore it first.',
          'membership',
        );
      }
      await this.membershipRepository.updateRole(client, {
        userId,
        siteId,
        role,
        modifiedByUserId: ctx.actor.userId,
      });
      await this.sessionInvalidator.rotateAndRevokeAll(client, userId, 'site_role_change');
      await this.audit.append(client, {
        action: 'membership_role_changed',
        subjectUserId: userId,
        actorUserId: ctx.actor.userId,
        siteId,
        correlationId: ctx.correlationId,
        reason: input.reason?.trim() || null,
        metadata: { role },
      });
    });
  }

  async changeStatus(
    ctx: UserCommandContext,
    userId: string,
    siteId: string,
    input: ChangeStatusInput,
  ): Promise<void> {
    if (input.status !== 'active' && input.status !== 'suspended') {
      throw new UserValidationException('status is invalid.', {
        status: ['must be active or suspended'],
      });
    }
    await this.pool.transaction(async (client) => {
      const target = await this.userRepository.findGlobalForUpdate(client, userId);
      if (target === null) throw new UserNotFoundException();
      // Authorize first so a site Administrator cannot probe other sites.
      const decision = this.policy.canManageMembership(
        ctx.actor,
        { id: target.id, isSuperAdmin: target.is_super_admin },
        siteId,
        'changeStatus',
      );
      if (!decision.allowed) {
        throw new UserAuthorizationException(decision.reason, decision.reason);
      }
      const existing = await this.membershipRepository.findByUserAndSiteForUpdate(
        client,
        userId,
        siteId,
      );
      if (existing === null) throw new UserNotFoundException('Membership was not found.');
      if (existing.status === 'revoked') {
        throw new UserConflictException(
          'The membership is revoked; restore it first.',
          'membership',
        );
      }
      await this.membershipRepository.updateStatus(client, {
        userId,
        siteId,
        status: input.status,
        modifiedByUserId: ctx.actor.userId,
      });
      await this.sessionInvalidator.rotateAndRevokeAll(client, userId, 'membership_change');
      await this.audit.append(client, {
        action: 'membership_status_changed',
        subjectUserId: userId,
        actorUserId: ctx.actor.userId,
        siteId,
        correlationId: ctx.correlationId,
        reason: input.reason?.trim() || null,
        metadata: { status: input.status },
      });
    });
  }

  async changeValidity(
    ctx: UserCommandContext,
    userId: string,
    siteId: string,
    input: ChangeValidityInput,
  ): Promise<void> {
    const validFrom = toDateOrNull(input.validFrom);
    const validUntil = toDateOrNull(input.validUntil);
    if (validFrom !== null && validUntil !== null && validUntil <= validFrom) {
      throw new UserValidationException('validUntil must be greater than validFrom.', {
        validUntil: ['must be greater than validFrom'],
      });
    }
    await this.pool.transaction(async (client) => {
      const target = await this.userRepository.findGlobalForUpdate(client, userId);
      if (target === null) throw new UserNotFoundException();
      // Authorize first so a site Administrator cannot probe other sites.
      const decision = this.policy.canManageMembership(
        ctx.actor,
        { id: target.id, isSuperAdmin: target.is_super_admin },
        siteId,
        'changeValidity',
      );
      if (!decision.allowed) {
        throw new UserAuthorizationException(decision.reason, decision.reason);
      }
      const existing = await this.membershipRepository.findByUserAndSiteForUpdate(
        client,
        userId,
        siteId,
      );
      if (existing === null) throw new UserNotFoundException('Membership was not found.');
      if (existing.status === 'revoked') {
        throw new UserConflictException(
          'The membership is revoked; restore it first.',
          'membership',
        );
      }
      await this.membershipRepository.updateValidity(client, {
        userId,
        siteId,
        validFrom,
        validUntil,
        modifiedByUserId: ctx.actor.userId,
      });
      await this.sessionInvalidator.rotateAndRevokeAll(client, userId, 'membership_change');
      await this.audit.append(client, {
        action: 'membership_validity_changed',
        subjectUserId: userId,
        actorUserId: ctx.actor.userId,
        siteId,
        correlationId: ctx.correlationId,
        reason: input.reason?.trim() || null,
        metadata: {
          validFrom: validFrom === null ? null : validFrom.toISOString(),
          validUntil: validUntil === null ? null : validUntil.toISOString(),
        },
      });
    });
  }

  async updateWorkProfile(
    ctx: UserCommandContext,
    userId: string,
    siteId: string,
    input: UpdateWorkProfileInput,
  ): Promise<void> {
    await this.pool.transaction(async (client) => {
      const target = await this.userRepository.findGlobalForUpdate(client, userId);
      if (target === null) throw new UserNotFoundException();
      // Authorize first so a site Administrator cannot probe other sites.
      const decision = this.policy.canManageMembership(
        ctx.actor,
        { id: target.id, isSuperAdmin: target.is_super_admin },
        siteId,
        'updateWorkProfile',
      );
      if (!decision.allowed) {
        throw new UserAuthorizationException(decision.reason, decision.reason);
      }
      const existing = await this.membershipRepository.findByUserAndSiteForUpdate(
        client,
        userId,
        siteId,
      );
      if (existing === null) throw new UserNotFoundException('Membership was not found.');
      if (existing.status === 'revoked') {
        throw new UserConflictException(
          'The membership is revoked; restore it first.',
          'membership',
        );
      }
      const position =
        input.position === undefined
          ? existing.position
          : input.position === null
            ? null
            : validateOptionalName(input.position, 'position');
      const department =
        input.department === undefined
          ? existing.department
          : input.department === null
            ? null
            : validateOptionalName(input.department, 'department');
      const hireDate =
        input.hireDate === undefined
          ? existing.hire_date === null || existing.hire_date === undefined
            ? null
            : existing.hire_date instanceof Date
              ? existing.hire_date
              : new Date(existing.hire_date)
          : toDateOrNull(input.hireDate);

      await this.membershipRepository.updateWorkProfile(client, {
        userId,
        siteId,
        position: position ?? null,
        department: department ?? null,
        hireDate,
        modifiedByUserId: ctx.actor.userId,
      });
      // No session rotation for work-profile-only changes (section 12).
      await this.audit.append(client, {
        action: 'profile_updated',
        subjectUserId: userId,
        actorUserId: ctx.actor.userId,
        siteId,
        correlationId: ctx.correlationId,
        metadata: {
          workProfile: true,
        },
      });
    });
  }

  /**
   * revokeActiveSite: target user is removed from the active site. If no other
   * non-revoked membership remains AND the user is not SuperAdmin, the account
   * also becomes 'deleted' (the single-site cascade from F1 section 11).
   */
  async revokeActiveSite(
    ctx: UserCommandContext,
    userId: string,
    siteId: string,
    input: RevokeActiveSiteInput,
  ): Promise<void> {
    await this.pool.transaction(async (client) => {
      await client.query(`SELECT pg_advisory_xact_lock(hashtext($1))`, [SUPERADMIN_LOCK_KEY]);

      const target = await this.userRepository.findGlobalForUpdate(client, userId);
      if (target === null) throw new UserNotFoundException();
      // Authorize first so a site Administrator cannot probe other sites.
      const decision = this.policy.canManageMembership(
        ctx.actor,
        { id: target.id, isSuperAdmin: target.is_super_admin },
        siteId,
        'revoke',
      );
      if (!decision.allowed) {
        throw new UserAuthorizationException(decision.reason, decision.reason);
      }
      const existing = await this.membershipRepository.findByUserAndSiteForUpdate(
        client,
        userId,
        siteId,
      );
      if (existing === null) throw new UserNotFoundException('Membership was not found.');
      if (existing.status === 'revoked') {
        throw new UserConflictException(
          'The membership is revoked; restore it first.',
          'membership',
        );
      }
      const beforeStatus: AccountStatus = target.account_status;
      await this.membershipRepository.revoke(client, {
        userId,
        siteId,
        modifiedByUserId: ctx.actor.userId,
      });
      await this.sessionInvalidator.rotateAndRevokeAll(client, userId, 'access_revoked');
      await this.audit.append(client, {
        action: 'membership_revoked',
        subjectUserId: userId,
        actorUserId: ctx.actor.userId,
        siteId,
        correlationId: ctx.correlationId,
        reason: input.reason?.trim() || null,
      });

      // Site-revoke cascade (F1 §11): no other non-revoked membership AND
      // the user is not SuperAdmin -> account becomes 'deleted'.
      const remaining = await this.userRepository.countNonRevokedMembershipsForUpdate(
        client,
        userId,
      );
      if (remaining.nonRevokedCount === 0 && !target.is_super_admin) {
        await this.userRepository.softDeleteAccount(client, {
          id: userId,
          modifiedByUserId: ctx.actor.userId,
        });
        await this.audit.append(client, {
          action: 'account_deleted',
          subjectUserId: userId,
          actorUserId: ctx.actor.userId,
          siteId,
          correlationId: ctx.correlationId,
          reason: SITE_REVOKE_CASCADE_REASON,
          beforeStatus,
          afterStatus: 'deleted',
        });
      }
    });
  }

  async restoreMembership(
    ctx: UserCommandContext,
    userId: string,
    siteId: string,
    input: RestoreMembershipInput,
  ): Promise<void> {
    const validFrom = toDateOrNull(input.validFrom ?? null);
    const validUntil = toDateOrNull(input.validUntil ?? null);
    if (validFrom !== null && validUntil !== null && validUntil <= validFrom) {
      throw new UserValidationException('validUntil must be greater than validFrom.', {
        validUntil: ['must be greater than validFrom'],
      });
    }
    await this.pool.transaction(async (client) => {
      await client.query(`SELECT pg_advisory_xact_lock(hashtext($1))`, [SUPERADMIN_LOCK_KEY]);

      const target = await this.userRepository.findGlobalForUpdate(client, userId);
      if (target === null) throw new UserNotFoundException();
      // Authorize first so a site Administrator cannot probe other sites.
      const decision = this.policy.canManageMembership(
        ctx.actor,
        { id: target.id, isSuperAdmin: target.is_super_admin },
        siteId,
        'restore',
      );
      if (!decision.allowed) {
        throw new UserAuthorizationException(decision.reason, decision.reason);
      }
      const existing = await this.membershipRepository.findByUserAndSiteForUpdate(
        client,
        userId,
        siteId,
      );
      if (existing === null) throw new UserNotFoundException('Membership was not found.');
      if (existing.status !== 'revoked') {
        throw new UserConflictException('Only a revoked membership can be restored.', 'membership');
      }

      // F1 §11: restoration retains the stored role and work profile; expired
      // validity is never silently extended, so new bounds must be supplied.
      const now = new Date();
      const storedFrom = toStoredDate(existing.valid_from);
      const storedUntil = toStoredDate(existing.valid_until);
      const nextFrom = input.validFrom !== undefined ? validFrom : storedFrom;
      const nextUntil = input.validUntil !== undefined ? validUntil : storedUntil;
      if (nextUntil !== null && nextUntil <= now) {
        throw new UserValidationException(
          'The membership validity has expired; new bounds are required.',
          {
            validUntil: ['must be in the future when restoring an expired membership'],
          },
        );
      }
      if (nextFrom !== null && nextUntil !== null && nextUntil <= nextFrom) {
        throw new UserValidationException('validUntil must be greater than validFrom.', {
          validUntil: ['must be greater than validFrom'],
        });
      }

      // A deleted account: SuperAdmin may restore it together with the
      // membership; a site Administrator only when the deletion was this site's
      // own site-revoke cascade and this is the account's only membership.
      const restoreAccount = target.account_status === 'deleted';
      if (restoreAccount) {
        if (input.restoreAccount !== true) {
          throw new UserConflictException(
            'The account is deleted; restore it together with the membership.',
            'account',
          );
        }
        if (!ctx.actor.isSuperAdmin) {
          const deletion = await this.userRepository.findLatestAccountDeletion(client, userId);
          const others = await this.userRepository.countNonRevokedMembershipsForUpdate(
            client,
            userId,
          );
          const byThisSiteCascade =
            deletion !== null &&
            deletion.siteId === siteId &&
            deletion.reason === SITE_REVOKE_CASCADE_REASON;
          if (!byThisSiteCascade || others.nonRevokedCount !== 0) {
            throw new UserAuthorizationException('SUPERADMIN_REQUIRED', 'SUPERADMIN_REQUIRED');
          }
        }
      }

      await this.membershipRepository.restore(client, {
        userId,
        siteId,
        role: existing.role,
        validFrom: nextFrom,
        validUntil: nextUntil,
        modifiedByUserId: ctx.actor.userId,
      });
      if (restoreAccount) {
        await this.userRepository.restoreAccount(client, {
          id: userId,
          modifiedByUserId: ctx.actor.userId,
        });
      }
      await this.sessionInvalidator.rotateAndRevokeAll(client, userId, 'access_restored');
      await this.audit.append(client, {
        action: 'membership_restored',
        subjectUserId: userId,
        actorUserId: ctx.actor.userId,
        siteId,
        correlationId: ctx.correlationId,
        reason: input.reason?.trim() || null,
      });
      if (restoreAccount) {
        await this.audit.append(client, {
          action: 'account_restored',
          subjectUserId: userId,
          actorUserId: ctx.actor.userId,
          siteId,
          correlationId: ctx.correlationId,
          reason: input.reason?.trim() || null,
          beforeStatus: 'deleted',
          afterStatus: 'active',
        });
      }
    });
  }

  // -------------------------------------------------------------------------
  // Profile (self-service). No active site required (F1 §16).
  // -------------------------------------------------------------------------

  async profile(ctx: UserCommandContext): Promise<ProfileRecord> {
    const row = await this.userRepository.findGlobal(ctx.actor.userId);
    if (row === null) throw new UserNotFoundException();
    const memberships = await this.userRepository.listMembershipsForUser(row.id);
    return {
      id: row.id,
      username: row.username ?? '',
      email: row.email,
      firstName: row.first_name,
      lastName: row.last_name,
      secondLastName: row.second_last_name,
      fullName: row.full_name,
      initials: initialsOf(row.first_name, row.last_name),
      phoneNumber: row.phone_number,
      identityCard: row.identity_card,
      isSuperAdmin: row.is_super_admin,
      accountStatus: row.account_status,
      profilePicture: profilePictureRef(row.profile_picture_key),
      memberships: mapMemberships(memberships, new Date(), row.account_status),
    };
  }

  async updateProfile(ctx: UserCommandContext, input: UpdateProfileInput): Promise<ProfileRecord> {
    if (input.identityCard !== undefined) {
      const decision = this.policy.canEditOwnProfileSubset('identityCard');
      if (!decision.allowed) {
        throw new UserAuthorizationException(decision.reason, decision.reason);
      }
    }
    const id = ctx.actor.userId;
    const result = await this.pool.transaction(async (client) => {
      const target = await this.userRepository.findGlobalForUpdate(client, id);
      if (target === null) throw new UserNotFoundException();

      const sets: Parameters<typeof this.userRepository.updateGlobalFields>[1] = {
        id,
        modifiedByUserId: id,
      };
      let emailChanged = false;
      if (input.email !== undefined) {
        const next = validateEmailField(input.email);
        if (next !== target.email) emailChanged = true;
        sets.email = next;
      }
      if (input.firstName !== undefined)
        sets.firstName = validateName(input.firstName, 'firstName');
      if (input.lastName !== undefined) sets.lastName = validateName(input.lastName, 'lastName');
      if (input.secondLastName !== undefined) {
        sets.secondLastName = validateOptionalName(input.secondLastName, 'secondLastName');
      }
      if (input.phoneNumber !== undefined) sets.phoneNumber = validatePhoneField(input.phoneNumber);
      await this.userRepository.updateGlobalFields(client, sets);
      if (emailChanged) {
        await this.sessionInvalidator.rotateAndRevokeAll(client, id, 'email_change');
        await this.audit.append(client, {
          action: 'email_changed',
          subjectUserId: id,
          actorUserId: id,
          correlationId: ctx.correlationId,
          metadata: { selfService: true },
        });
      } else {
        await this.audit.append(client, {
          action: 'profile_updated',
          subjectUserId: id,
          actorUserId: id,
          correlationId: ctx.correlationId,
          metadata: { changedFields: this.changedFields(input).join(','), selfService: true },
        });
      }
      const refreshed = await this.userRepository.findGlobalForUpdate(client, id);
      if (refreshed === null) throw new UserNotFoundException();
      const memberships = await this.userRepository.listMembershipsForUpdate(client, id);
      const record: ProfileRecord = {
        id: refreshed.id,
        username: refreshed.username ?? '',
        email: refreshed.email,
        firstName: refreshed.first_name,
        lastName: refreshed.last_name,
        secondLastName: refreshed.second_last_name,
        fullName: refreshed.full_name,
        initials: initialsOf(refreshed.first_name, refreshed.last_name),
        phoneNumber: refreshed.phone_number,
        identityCard: refreshed.identity_card,
        isSuperAdmin: refreshed.is_super_admin,
        accountStatus: refreshed.account_status,
        profilePicture: profilePictureRef(refreshed.profile_picture_key),
        memberships: mapMemberships(memberships, new Date(), refreshed.account_status),
      };
      return { emailChanged, record };
    });
    if (result.emailChanged) {
      await this.renewSelfSession(ctx);
    }
    return result.record;
  }

  private toRecord(
    row: UserRow,
    memberships: ManagedUserRecord['memberships'],
    activeSiteRole: 'Administrador' | 'Supervisor' | null,
  ): ManagedUserRecord {
    return {
      id: row.id,
      username: row.username ?? '',
      email: row.email,
      firstName: row.first_name,
      lastName: row.last_name,
      secondLastName: row.second_last_name,
      fullName: row.full_name,
      initials: initialsOf(row.first_name, row.last_name),
      identityCard: row.identity_card,
      phoneNumber: row.phone_number,
      accountStatus: row.account_status,
      isSuperAdmin: row.is_super_admin,
      displayRole: row.is_super_admin ? 'SuperAdmin' : activeSiteRole,
      profilePicture: profilePictureRef(row.profile_picture_key),
      mustChangePassword: row.must_change_password,
      memberships,
      createdAt: iso(row.created_at),
      updatedAt: iso(row.updated_at),
    };
  }
}

// Keep the type-only re-export silent.
export type { IdentityAuditAction };
