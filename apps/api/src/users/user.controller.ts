/**
 * MIG-001 F3 /users control-plane HTTP layer.
 *
 * Splits the previous single-PUT mixed-scope update into dedicated global
 * and membership routes. Uses backend-local DTOs (apps/api/src/users/) — the
 * packages/contracts DTOs are intentionally not imported here.
 *
 * Every mutating route is CSRF-protected by AuthSecurityGuard. The global
 * session guard (GlobalSessionGuard) populates `request.identity` and is
 * overridden in tests because it is implemented by the concurrent auth worker.
 */
import {
  Body,
  Controller,
  Delete,
  Get,
  HttpCode,
  Param,
  Post,
  Put,
  Query,
  Req,
  Res,
  UseFilters,
  UseGuards,
  UseInterceptors,
} from '@nestjs/common';
import { randomUUID } from 'node:crypto';
import { AuthSecurityGuard, SkipCsrf } from '../auth/auth.guard.js';
import { AuthHeadersInterceptor } from '../auth/auth.interceptor.js';
import { CoreExceptionFilter } from '../core/core.filter.js';
import type { FastifyReply, FastifyRequest } from '../auth/auth.fastify.js';
import type { RequestIdentity } from '../identity/identity.contracts.js';
import { ApiCorrelationGuard, GlobalSessionGuard } from '../core/core.guard.js';
import {
  AuthAccessDeniedException,
  AuthConflictException,
  AuthNotFoundException,
  AuthUnauthorizedException,
  AuthValidationException,
} from '../auth/auth.exceptions.js';
import {
  AddMembershipInput,
  AdminResetPasswordInput,
  ChangeRoleInput,
  ChangeStatusInput,
  ChangeValidityInput,
  CreateUserGlobalInput,
  CreateUserSiteAdminInput,
  FORBIDDEN_PAYLOAD_KEYS,
  GlobalDeleteInput,
  ManagedAccountStatus,
  ManagedUserPage,
  ManagedUserQuery,
  ManagedUserRecord,
  ProfileRecord,
  RestoreAccountInput,
  RestoreMembershipInput,
  RevokeActiveSiteInput,
  SetAccountStatusInput,
  UpdateGlobalFieldsInput,
  UpdateProfileInput,
  UpdateWorkProfileInput,
} from './user.types.js';
import {
  UserAuthorizationException,
  UserCommandContext,
  UserConflictException,
  UserNotFoundException,
  UserService,
  UserValidationException,
} from './user.service.js';

interface RequestWithIdentity extends FastifyRequest {
  identity?: RequestIdentity;
}

function asRequest(req: FastifyRequest): RequestWithIdentity {
  return req as RequestWithIdentity;
}

function getIdentity(req: FastifyRequest): RequestIdentity {
  const identity = asRequest(req).identity;
  if (identity === undefined) {
    // Guards always resolve an identity; its absence is an unauthenticated call.
    throw new AuthUnauthorizedException();
  }
  return identity;
}

function getCorrelation(req: FastifyRequest): string {
  const correlationId = req.correlationId;
  if (typeof correlationId === 'string' && correlationId !== '') return correlationId;
  return randomUUID();
}

/**
 * Payloads are closed: privileged keys (is_super_admin, security/row versions,
 * password hash, full name, ...) and any key outside the endpoint allowlist
 * are rejected with 400 before reaching the service.
 */
function ensureAllowedKeys(payload: Record<string, unknown>, allowed: readonly string[]): void {
  for (const key of Object.keys(payload)) {
    if (FORBIDDEN_PAYLOAD_KEYS.includes(key) || !allowed.includes(key)) {
      throw new UserValidationException(`Field "${key}" is not accepted by this endpoint.`, {
        [key]: ['is not accepted by this endpoint'],
      });
    }
  }
}

function parseObject(body: unknown): Record<string, unknown> {
  if (typeof body !== 'object' || body === null || Array.isArray(body)) {
    throw new UserValidationException('Request body must be a JSON object.');
  }
  return body as Record<string, unknown>;
}

function asString(value: unknown, field: string, required = false): string | undefined {
  if (value === undefined || value === null) {
    if (required) {
      throw new UserValidationException(`${field} is required.`, { [field]: ['is required'] });
    }
    return undefined;
  }
  if (typeof value !== 'string') {
    throw new UserValidationException(`${field} must be a string.`, {
      [field]: ['must be a string'],
    });
  }
  return value;
}

function asNullableString(value: unknown, field: string): string | null | undefined {
  if (value === undefined) return undefined;
  if (value === null) return null;
  if (typeof value !== 'string') {
    throw new UserValidationException(`${field} must be a string or null.`, {
      [field]: ['must be a string or null'],
    });
  }
  return value;
}

function asRequiredString(value: unknown, field: string): string {
  const result = asString(value, field, true);
  if (result === undefined) {
    throw new UserValidationException(`${field} is required.`, { [field]: ['is required'] });
  }
  return result;
}

function asUuid(value: string, field: string): string {
  if (!/^[0-9a-f]{8}-[0-9a-f]{4}-[1-5][0-9a-f]{3}-[89ab][0-9a-f]{3}-[0-9a-f]{12}$/i.test(value)) {
    throw new UserValidationException(`${field} must be a canonical UUID.`, {
      [field]: ['must be a canonical UUID'],
    });
  }
  return value.toLowerCase();
}

function asAccountStatus(value: unknown): 'active' | 'inactive' {
  if (value !== 'active' && value !== 'inactive') {
    throw new UserValidationException('accountStatus is invalid.', {
      accountStatus: ['must be active or inactive (deletion uses the delete route)'],
    });
  }
  return value;
}

function asMembershipStatus(value: unknown): 'active' | 'suspended' {
  if (value !== 'active' && value !== 'suspended') {
    throw new UserValidationException('status is invalid.', {
      status: ['must be active or suspended'],
    });
  }
  return value;
}

function asRole(value: unknown): 'Administrador' | 'Supervisor' {
  if (value !== 'Administrador' && value !== 'Supervisor') {
    throw new UserValidationException('role is invalid.', {
      role: ['must be Administrador or Supervisor'],
    });
  }
  return value;
}

function asSiteScope(value: string | undefined): string | null | undefined {
  if (value === undefined) return undefined;
  if (value === 'all') return null;
  if (value === '') return undefined;
  return asUuid(value, 'siteScope');
}

function buildCreateUserSiteAdminInput(body: Record<string, unknown>): CreateUserSiteAdminInput {
  ensureAllowedKeys(body, [
    'username',
    'email',
    'firstName',
    'lastName',
    'secondLastName',
    'identityCard',
    'phoneNumber',
    'password',
    'role',
  ]);
  return {
    username: asRequiredString(body.username, 'username'),
    email: asRequiredString(body.email, 'email'),
    firstName: asRequiredString(body.firstName, 'firstName'),
    lastName: asRequiredString(body.lastName, 'lastName'),
    secondLastName: asNullableString(body.secondLastName, 'secondLastName') ?? null,
    identityCard: asRequiredString(body.identityCard, 'identityCard'),
    phoneNumber: asRequiredString(body.phoneNumber, 'phoneNumber'),
    password: asRequiredString(body.password, 'password'),
    role: asRole(body.role),
  };
}

function buildCreateUserGlobalInput(body: Record<string, unknown>): CreateUserGlobalInput {
  ensureAllowedKeys(body, [
    'username',
    'email',
    'firstName',
    'lastName',
    'secondLastName',
    'identityCard',
    'phoneNumber',
    'password',
    'memberships',
  ]);
  if (!Array.isArray(body.memberships)) {
    throw new UserValidationException('memberships is required.', {
      memberships: ['must be a non-empty array of memberships'],
    });
  }
  const memberships = body.memberships.map((entry, index) => {
    if (typeof entry !== 'object' || entry === null) {
      throw new UserValidationException(`memberships[${index}] must be an object.`);
    }
    const item = entry as Record<string, unknown>;
    return {
      siteId: asUuid(
        asRequiredString(item.siteId, `memberships[${index}].siteId`),
        `memberships[${index}].siteId`,
      ),
      role: asRole(item.role),
      validFrom: asNullableString(item.validFrom, `memberships[${index}].validFrom`) ?? null,
      validUntil: asNullableString(item.validUntil, `memberships[${index}].validUntil`) ?? null,
    };
  });
  return {
    username: asRequiredString(body.username, 'username'),
    email: asRequiredString(body.email, 'email'),
    firstName: asRequiredString(body.firstName, 'firstName'),
    lastName: asRequiredString(body.lastName, 'lastName'),
    secondLastName: asNullableString(body.secondLastName, 'secondLastName') ?? null,
    identityCard: asRequiredString(body.identityCard, 'identityCard'),
    phoneNumber: asRequiredString(body.phoneNumber, 'phoneNumber'),
    password: asRequiredString(body.password, 'password'),
    memberships,
  };
}

function buildUpdateGlobalFieldsInput(body: Record<string, unknown>): UpdateGlobalFieldsInput {
  ensureAllowedKeys(body, [
    'email',
    'firstName',
    'lastName',
    'secondLastName',
    'identityCard',
    'phoneNumber',
    'reason',
  ]);
  return {
    email: asString(body.email, 'email'),
    firstName: asString(body.firstName, 'firstName'),
    lastName: asString(body.lastName, 'lastName'),
    secondLastName: asNullableString(body.secondLastName, 'secondLastName'),
    identityCard: asString(body.identityCard, 'identityCard'),
    phoneNumber: asString(body.phoneNumber, 'phoneNumber'),
    reason: asString(body.reason, 'reason'),
  };
}

function buildAdminResetPasswordInput(body: Record<string, unknown>): AdminResetPasswordInput {
  ensureAllowedKeys(body, ['password', 'reason']);
  return {
    password: asRequiredString(body.password, 'password'),
    reason: asString(body.reason, 'reason'),
  };
}

function buildSetAccountStatusInput(body: Record<string, unknown>): SetAccountStatusInput {
  ensureAllowedKeys(body, ['accountStatus', 'reason']);
  return {
    accountStatus: asAccountStatus(body.accountStatus),
    reason: asString(body.reason, 'reason'),
  };
}

function buildRestoreAccountInput(body: Record<string, unknown>): RestoreAccountInput {
  ensureAllowedKeys(body, ['reason']);
  return { reason: asString(body.reason, 'reason') };
}

function buildGlobalDeleteInput(body: Record<string, unknown>): GlobalDeleteInput {
  ensureAllowedKeys(body, ['reason']);
  return { reason: asString(body.reason, 'reason') };
}

function buildAddMembershipInput(body: Record<string, unknown>): AddMembershipInput {
  ensureAllowedKeys(body, ['siteId', 'role', 'validFrom', 'validUntil']);
  return {
    siteId: asUuid(asRequiredString(body.siteId, 'siteId'), 'siteId'),
    role: asRole(body.role),
    validFrom: asNullableString(body.validFrom, 'validFrom') ?? null,
    validUntil: asNullableString(body.validUntil, 'validUntil') ?? null,
  };
}

function buildChangeRoleInput(body: Record<string, unknown>): ChangeRoleInput {
  ensureAllowedKeys(body, ['role', 'reason']);
  return {
    role: asRole(body.role),
    reason: asString(body.reason, 'reason'),
  };
}

function buildChangeStatusInput(body: Record<string, unknown>): ChangeStatusInput {
  ensureAllowedKeys(body, ['status', 'reason']);
  return {
    status: asMembershipStatus(body.status),
    reason: asString(body.reason, 'reason'),
  };
}

function buildChangeValidityInput(body: Record<string, unknown>): ChangeValidityInput {
  ensureAllowedKeys(body, ['validFrom', 'validUntil', 'reason']);
  return {
    validFrom: asNullableString(body.validFrom, 'validFrom') ?? null,
    validUntil: asNullableString(body.validUntil, 'validUntil') ?? null,
    reason: asString(body.reason, 'reason'),
  };
}

function buildUpdateWorkProfileInput(body: Record<string, unknown>): UpdateWorkProfileInput {
  ensureAllowedKeys(body, ['position', 'department', 'hireDate']);
  return {
    position: asNullableString(body.position, 'position'),
    department: asNullableString(body.department, 'department'),
    hireDate: asNullableString(body.hireDate, 'hireDate'),
  };
}

function buildRevokeActiveSiteInput(body: Record<string, unknown>): RevokeActiveSiteInput {
  ensureAllowedKeys(body, ['reason']);
  return { reason: asString(body.reason, 'reason') };
}

function buildRestoreMembershipInput(body: Record<string, unknown>): RestoreMembershipInput {
  ensureAllowedKeys(body, ['validFrom', 'validUntil', 'restoreAccount', 'reason']);
  return {
    validFrom: asNullableString(body.validFrom, 'validFrom'),
    validUntil: asNullableString(body.validUntil, 'validUntil'),
    restoreAccount: body.restoreAccount === true,
    reason: asString(body.reason, 'reason'),
  };
}

function buildUpdateProfileInput(body: Record<string, unknown>): UpdateProfileInput {
  ensureAllowedKeys(body, ['email', 'firstName', 'lastName', 'secondLastName', 'phoneNumber']);
  return {
    email: asString(body.email, 'email'),
    firstName: asString(body.firstName, 'firstName'),
    lastName: asString(body.lastName, 'lastName'),
    secondLastName: asNullableString(body.secondLastName, 'secondLastName'),
    phoneNumber: asString(body.phoneNumber, 'phoneNumber'),
  };
}

function parseQuery(value: Record<string, string | undefined>): ManagedUserQuery {
  const currentPageRaw = value.currentPage;
  let currentPage: number | undefined;
  if (currentPageRaw !== undefined && currentPageRaw !== '') {
    const parsed = Number(currentPageRaw);
    if (!Number.isSafeInteger(parsed) || parsed < 1) {
      throw new UserValidationException('currentPage is invalid.');
    }
    currentPage = parsed;
  }
  const statusFilterRaw = value.statusFilter;
  let statusFilter: ManagedAccountStatus | undefined;
  if (statusFilterRaw !== undefined && statusFilterRaw !== '') {
    if (
      statusFilterRaw !== 'active' &&
      statusFilterRaw !== 'inactive' &&
      statusFilterRaw !== 'deleted'
    ) {
      throw new UserValidationException('statusFilter is invalid.');
    }
    statusFilter = statusFilterRaw as ManagedAccountStatus;
  }
  const searchTerm = value.searchTerm?.trim().slice(0, 200) || undefined;
  const siteScope = asSiteScope(value.siteScope);
  const query: ManagedUserQuery = {
    currentPage,
    statusFilter,
    searchTerm,
  };
  if (siteScope !== undefined) {
    return { ...query, siteScope };
  }
  return query;
}

const DENIAL_MESSAGES: Readonly<Record<string, string>> = {
  SELF_PROTECTION: 'This operation cannot be applied to your own account or active membership.',
  LAST_SUPERADMIN: 'At least one active SuperAdmin must remain.',
  SUPERADMIN_PROTECTED: 'SuperAdmin accounts can only be administered by a SuperAdmin.',
  SUPERADMIN_REQUIRED: 'This operation requires a SuperAdmin.',
  LINK_REQUIRES_SUPERADMIN: 'Linking an existing user to a site requires a SuperAdmin.',
  CUSTODY_NOT_SINGLE_SITE:
    'Global fields can only be changed by the custodian of a single-site account.',
};

/** Maps users-domain errors onto the shared API error envelope (CoreExceptionFilter). */
function mapErrorToHttp(error: unknown): never {
  if (error instanceof UserValidationException) {
    throw new AuthValidationException(error.message, error.fieldErrors);
  }
  if (error instanceof UserNotFoundException) {
    throw new AuthNotFoundException(error.message);
  }
  if (error instanceof UserConflictException) {
    throw new AuthConflictException(error.message);
  }
  if (error instanceof UserAuthorizationException) {
    if (error.code === 'IDENTITY_REQUIRED') throw new AuthUnauthorizedException();
    throw new AuthAccessDeniedException(DENIAL_MESSAGES[error.code] ?? 'Operation not permitted.');
  }
  throw error;
}

@Controller('users')
@UseFilters(CoreExceptionFilter)
@UseGuards(ApiCorrelationGuard, AuthSecurityGuard, GlobalSessionGuard)
@UseInterceptors(AuthHeadersInterceptor)
export class UserController {
  constructor(private readonly service: UserService) {}

  @Get()
  @SkipCsrf()
  async list(
    @Req() req: FastifyRequest,
    @Query() value: Record<string, string | undefined>,
  ): Promise<{ success: true; data: ManagedUserPage }> {
    const ctx = this.commandContext(req);
    try {
      const data = await this.service.list(ctx, parseQuery(value));
      return { success: true, data };
    } catch (error) {
      return mapErrorToHttp(error);
    }
  }

  @Get(':id')
  @SkipCsrf()
  async find(
    @Req() req: FastifyRequest,
    @Param('id') id: string,
  ): Promise<{ success: true; data: ManagedUserRecord }> {
    const ctx = this.commandContext(req);
    try {
      const userId = asUuid(id, 'id');
      const data = await this.service.find(ctx, userId);
      return { success: true, data };
    } catch (error) {
      return mapErrorToHttp(error);
    }
  }

  @Post()
  @HttpCode(201)
  async create(
    @Req() req: FastifyRequest,
    @Body() raw: unknown,
  ): Promise<{ success: true; data: ManagedUserRecord }> {
    const ctx = this.commandContext(req);
    try {
      const body = parseObject(raw);
      if (ctx.actor.isSuperAdmin) {
        const data = await this.service.createGlobal(ctx, buildCreateUserGlobalInput(body));
        return { success: true, data };
      }
      const data = await this.service.createSite(ctx, buildCreateUserSiteAdminInput(body));
      return { success: true, data };
    } catch (error) {
      return mapErrorToHttp(error);
    }
  }

  @Put(':id/global-fields')
  async updateGlobalFields(
    @Req() req: FastifyRequest,
    @Res({ passthrough: true }) reply: FastifyReply,
    @Param('id') id: string,
    @Body() raw: unknown,
  ): Promise<{ success: true; data: ManagedUserRecord }> {
    // The reply carries the replacement session cookie after a self email change.
    const ctx = this.commandContext(req, reply);
    try {
      const userId = asUuid(id, 'id');
      const data = await this.service.updateGlobalFields(
        ctx,
        userId,
        buildUpdateGlobalFieldsInput(parseObject(raw)),
      );
      return { success: true, data };
    } catch (error) {
      return mapErrorToHttp(error);
    }
  }

  @Post(':id/admin-reset-password')
  @HttpCode(204)
  async adminResetPassword(
    @Req() req: FastifyRequest,
    @Param('id') id: string,
    @Body() raw: unknown,
  ): Promise<{ success: true; data: null }> {
    const ctx = this.commandContext(req);
    try {
      const userId = asUuid(id, 'id');
      await this.service.adminResetPassword(
        ctx,
        userId,
        buildAdminResetPasswordInput(parseObject(raw)),
      );
      return { success: true, data: null };
    } catch (error) {
      return mapErrorToHttp(error);
    }
  }

  @Put(':id/account-status')
  async setAccountStatus(
    @Req() req: FastifyRequest,
    @Param('id') id: string,
    @Body() raw: unknown,
  ): Promise<{ success: true; data: null }> {
    const ctx = this.commandContext(req);
    try {
      const userId = asUuid(id, 'id');
      await this.service.setAccountStatus(
        ctx,
        userId,
        buildSetAccountStatusInput(parseObject(raw)),
      );
      return { success: true, data: null };
    } catch (error) {
      return mapErrorToHttp(error);
    }
  }

  @Post(':id/restore')
  @HttpCode(204)
  async restoreAccount(
    @Req() req: FastifyRequest,
    @Param('id') id: string,
    @Body() raw: unknown,
  ): Promise<{ success: true; data: null }> {
    const ctx = this.commandContext(req);
    try {
      const userId = asUuid(id, 'id');
      await this.service.restoreAccount(
        ctx,
        userId,
        buildRestoreAccountInput(parseObject(raw) ?? {}),
      );
      return { success: true, data: null };
    } catch (error) {
      return mapErrorToHttp(error);
    }
  }

  @Delete(':id')
  @HttpCode(204)
  async globalDelete(
    @Req() req: FastifyRequest,
    @Param('id') id: string,
  ): Promise<{ success: true; data: null }> {
    const ctx = this.commandContext(req);
    try {
      const userId = asUuid(id, 'id');
      await this.service.globalDelete(ctx, userId, buildGlobalDeleteInput({}));
      return { success: true, data: null };
    } catch (error) {
      return mapErrorToHttp(error);
    }
  }

  @Post(':id/memberships')
  @HttpCode(201)
  async addMembership(
    @Req() req: FastifyRequest,
    @Param('id') id: string,
    @Body() raw: unknown,
  ): Promise<{ success: true; data: null }> {
    const ctx = this.commandContext(req);
    try {
      const userId = asUuid(id, 'id');
      await this.service.addMembership(ctx, userId, buildAddMembershipInput(parseObject(raw)));
      return { success: true, data: null };
    } catch (error) {
      return mapErrorToHttp(error);
    }
  }

  @Put(':id/memberships/:siteId/role')
  async changeRole(
    @Req() req: FastifyRequest,
    @Param('id') id: string,
    @Param('siteId') siteId: string,
    @Body() raw: unknown,
  ): Promise<{ success: true; data: null }> {
    const ctx = this.commandContext(req);
    try {
      const userId = asUuid(id, 'id');
      const targetSiteId = asUuid(siteId, 'siteId');
      await this.service.changeRole(
        ctx,
        userId,
        targetSiteId,
        buildChangeRoleInput(parseObject(raw)),
      );
      return { success: true, data: null };
    } catch (error) {
      return mapErrorToHttp(error);
    }
  }

  @Put(':id/memberships/:siteId/status')
  async changeStatus(
    @Req() req: FastifyRequest,
    @Param('id') id: string,
    @Param('siteId') siteId: string,
    @Body() raw: unknown,
  ): Promise<{ success: true; data: null }> {
    const ctx = this.commandContext(req);
    try {
      const userId = asUuid(id, 'id');
      const targetSiteId = asUuid(siteId, 'siteId');
      await this.service.changeStatus(
        ctx,
        userId,
        targetSiteId,
        buildChangeStatusInput(parseObject(raw)),
      );
      return { success: true, data: null };
    } catch (error) {
      return mapErrorToHttp(error);
    }
  }

  @Put(':id/memberships/:siteId/validity')
  async changeValidity(
    @Req() req: FastifyRequest,
    @Param('id') id: string,
    @Param('siteId') siteId: string,
    @Body() raw: unknown,
  ): Promise<{ success: true; data: null }> {
    const ctx = this.commandContext(req);
    try {
      const userId = asUuid(id, 'id');
      const targetSiteId = asUuid(siteId, 'siteId');
      await this.service.changeValidity(
        ctx,
        userId,
        targetSiteId,
        buildChangeValidityInput(parseObject(raw)),
      );
      return { success: true, data: null };
    } catch (error) {
      return mapErrorToHttp(error);
    }
  }

  @Put(':id/memberships/:siteId/work-profile')
  async updateWorkProfile(
    @Req() req: FastifyRequest,
    @Param('id') id: string,
    @Param('siteId') siteId: string,
    @Body() raw: unknown,
  ): Promise<{ success: true; data: null }> {
    const ctx = this.commandContext(req);
    try {
      const userId = asUuid(id, 'id');
      const targetSiteId = asUuid(siteId, 'siteId');
      await this.service.updateWorkProfile(
        ctx,
        userId,
        targetSiteId,
        buildUpdateWorkProfileInput(parseObject(raw)),
      );
      return { success: true, data: null };
    } catch (error) {
      return mapErrorToHttp(error);
    }
  }

  @Delete(':id/memberships/:siteId')
  @HttpCode(204)
  async revokeActiveSite(
    @Req() req: FastifyRequest,
    @Param('id') id: string,
    @Param('siteId') siteId: string,
  ): Promise<{ success: true; data: null }> {
    const ctx = this.commandContext(req);
    try {
      const userId = asUuid(id, 'id');
      const targetSiteId = asUuid(siteId, 'siteId');
      await this.service.revokeActiveSite(
        ctx,
        userId,
        targetSiteId,
        buildRevokeActiveSiteInput({}),
      );
      return { success: true, data: null };
    } catch (error) {
      return mapErrorToHttp(error);
    }
  }

  @Post(':id/memberships/:siteId/restore')
  @HttpCode(204)
  async restoreMembership(
    @Req() req: FastifyRequest,
    @Param('id') id: string,
    @Param('siteId') siteId: string,
    @Body() raw: unknown,
  ): Promise<{ success: true; data: null }> {
    const ctx = this.commandContext(req);
    try {
      const userId = asUuid(id, 'id');
      const targetSiteId = asUuid(siteId, 'siteId');
      await this.service.restoreMembership(
        ctx,
        userId,
        targetSiteId,
        buildRestoreMembershipInput(parseObject(raw) ?? {}),
      );
      return { success: true, data: null };
    } catch (error) {
      return mapErrorToHttp(error);
    }
  }

  private commandContext(
    req: FastifyRequest,
    reply: FastifyReply | null = null,
  ): UserCommandContext {
    return { actor: getIdentity(req), correlationId: getCorrelation(req), reply };
  }
}

@Controller('profile')
@UseFilters(CoreExceptionFilter)
@UseGuards(ApiCorrelationGuard, AuthSecurityGuard, GlobalSessionGuard)
@UseInterceptors(AuthHeadersInterceptor)
export class ProfileController {
  constructor(private readonly service: UserService) {}

  @Get()
  @SkipCsrf()
  async profile(@Req() req: FastifyRequest): Promise<{ success: true; data: ProfileRecord }> {
    try {
      const ctx: UserCommandContext = {
        actor: getIdentity(req),
        correlationId: getCorrelation(req),
        reply: null,
      };
      const data = await this.service.profile(ctx);
      return { success: true, data };
    } catch (error) {
      return mapErrorToHttp(error);
    }
  }

  @Put()
  async updateProfile(
    @Req() req: FastifyRequest,
    @Res({ passthrough: true }) reply: FastifyReply,
    @Body() raw: unknown,
  ): Promise<{ success: true; data: ProfileRecord }> {
    try {
      // The reply carries the replacement session cookie after a self email change.
      const ctx: UserCommandContext = {
        actor: getIdentity(req),
        correlationId: getCorrelation(req),
        reply,
      };
      const data = await this.service.updateProfile(ctx, buildUpdateProfileInput(parseObject(raw)));
      return { success: true, data };
    } catch (error) {
      return mapErrorToHttp(error);
    }
  }
}

// Re-export to ensure the import is kept (TS will tree-shake the type-only side).
export {};
