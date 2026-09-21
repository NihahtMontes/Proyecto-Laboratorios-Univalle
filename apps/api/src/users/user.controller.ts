import {
  Body,
  Controller,
  Delete,
  Get,
  Param,
  Post,
  Put,
  Query,
  Req,
  UseFilters,
  UseGuards,
  UseInterceptors,
} from '@nestjs/common';
import type {
  ApiSuccess,
  CreateManagedUserInput,
  ManagedUserPage,
  ManagedUserQuery,
  ManagedUserRecord,
  SiteRole,
  UpdateManagedUserInput,
  UpdateProfileInput,
  ProfileRecord,
  ManagedUserStatus,
} from '@lu/contracts';
import { AuthSecurityGuard, SkipCsrf } from '../auth/auth.guard.js';
import { AuthHeadersInterceptor } from '../auth/auth.interceptor.js';
import { AuthValidationException } from '../auth/auth.exceptions.js';
import type { FastifyRequest } from '../auth/auth.fastify.js';
import { ApiCorrelationGuard, SiteContextGuard } from '../core/core.guard.js';
import { CoreExceptionFilter } from '../core/core.filter.js';
import { UserService } from './user.service.js';

function context(request: FastifyRequest) {
  if (request.siteContext === undefined) throw new Error('Site context was not resolved.');
  return request.siteContext;
}
function object(value: unknown): Record<string, unknown> {
  if (typeof value !== 'object' || value === null || Array.isArray(value))
    throw new AuthValidationException('User input is invalid.');
  return value as Record<string, unknown>;
}
function text(value: unknown, field: string, required = false): string | null {
  if (value === undefined || value === null) {
    if (required)
      throw new AuthValidationException('User input is invalid.', { [field]: ['is required'] });
    return null;
  }
  if (typeof value !== 'string')
    throw new AuthValidationException('User input is invalid.', { [field]: ['must be a string'] });
  const result = value.trim();
  if (required && result === '')
    throw new AuthValidationException('User input is invalid.', { [field]: ['is required'] });
  return result;
}
function email(value: unknown): string {
  const result = text(value, 'email', true)!;
  if (!/^[^\s@]+@[^\s@]+\.[^\s@]+$/.test(result) || result.length > 254)
    throw new AuthValidationException('User input is invalid.', {
      email: ['must be a valid email'],
    });
  return result.toLowerCase();
}
function uuid(value: string): string {
  if (!/^[0-9a-f]{8}-[0-9a-f]{4}-[1-5][0-9a-f]{3}-[89ab][0-9a-f]{3}-[0-9a-f]{12}$/i.test(value))
    throw new AuthValidationException('User id is invalid.');
  return value;
}
function role(value: unknown): SiteRole {
  if (value !== 'Administrador' && value !== 'Supervisor')
    throw new AuthValidationException('User input is invalid.', {
      siteRole: ['unsupported value'],
    });
  return value as SiteRole;
}
function password(value: unknown, required: boolean): string | null {
  const result = text(value, 'password', required);
  if (result === null) return null;
  if (result.length < 8 || result.length > 72)
    throw new AuthValidationException('User input is invalid.', {
      password: ['must contain 8 to 72 characters'],
    });
  return result;
}
function createInput(value: Record<string, unknown>): CreateManagedUserInput {
  return {
    email: email(value.email),
    fullName: text(value.fullName, 'fullName', true)!,
    password: password(value.password, true)!,
    siteRole: role(value.siteRole),
  };
}
function updateInput(value: Record<string, unknown>): UpdateManagedUserInput {
  const status = value.status;
  if (status !== 'active' && status !== 'disabled')
    throw new AuthValidationException('User input is invalid.', { status: ['unsupported value'] });
  return {
    email: email(value.email),
    fullName: text(value.fullName, 'fullName', true)!,
    password: password(value.password, false),
    status,
    siteRole: role(value.siteRole),
  };
}
function profileInput(value: Record<string, unknown>): UpdateProfileInput {
  return { email: email(value.email), fullName: text(value.fullName, 'fullName', true)! };
}
function query(value: Record<string, string | undefined>): ManagedUserQuery {
  const raw = value.statusFilter;
  if (raw !== undefined && raw !== 'active' && raw !== 'disabled')
    throw new AuthValidationException('User query is invalid.');
  const currentPage = value.currentPage === undefined ? undefined : Number(value.currentPage);
  if (currentPage !== undefined && (!Number.isSafeInteger(currentPage) || currentPage < 1))
    throw new AuthValidationException('User query is invalid.');
  return {
    currentPage,
    statusFilter: raw as ManagedUserStatus | undefined,
    searchTerm: value.searchTerm?.trim().slice(0, 200) || undefined,
  };
}

@Controller()
@UseFilters(CoreExceptionFilter)
@UseGuards(ApiCorrelationGuard, AuthSecurityGuard, SiteContextGuard)
@UseInterceptors(AuthHeadersInterceptor)
export class UserController {
  constructor(private readonly service: UserService) {}
  @Get('users') @SkipCsrf() async list(
    @Req() request: FastifyRequest,
    @Query() value: Record<string, string | undefined>,
  ): Promise<ApiSuccess<ManagedUserPage>> {
    return { success: true, data: await this.service.list(context(request), query(value)) };
  }
  @Get('users/:id') @SkipCsrf() async find(
    @Req() request: FastifyRequest,
    @Param('id') id: string,
  ): Promise<ApiSuccess<ManagedUserRecord>> {
    return { success: true, data: await this.service.find(context(request), uuid(id)) };
  }
  @Post('users') async create(
    @Req() request: FastifyRequest,
    @Body() raw: unknown,
  ): Promise<ApiSuccess<ManagedUserRecord>> {
    return {
      success: true,
      data: await this.service.create(context(request), createInput(object(raw))),
    };
  }
  @Put('users/:id') async update(
    @Req() request: FastifyRequest,
    @Param('id') id: string,
    @Body() raw: unknown,
  ): Promise<ApiSuccess<ManagedUserRecord>> {
    return {
      success: true,
      data: await this.service.update(context(request), uuid(id), updateInput(object(raw))),
    };
  }
  @Delete('users/:id') async remove(
    @Req() request: FastifyRequest,
    @Param('id') id: string,
  ): Promise<ApiSuccess<null>> {
    await this.service.disable(context(request), uuid(id));
    return { success: true, data: null };
  }
  @Get('profile') @SkipCsrf() async profile(
    @Req() request: FastifyRequest,
  ): Promise<ApiSuccess<ProfileRecord>> {
    return { success: true, data: await this.service.profile(context(request)) };
  }
  @Put('profile') async updateProfile(
    @Req() request: FastifyRequest,
    @Body() raw: unknown,
  ): Promise<ApiSuccess<ProfileRecord>> {
    return {
      success: true,
      data: await this.service.updateProfile(context(request), profileInput(object(raw))),
    };
  }
}
