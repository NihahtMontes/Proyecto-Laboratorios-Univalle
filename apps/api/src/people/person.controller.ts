import {
  Body,
  Controller,
  Delete,
  Get,
  Param,
  ParseIntPipe,
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
  CreatePersonInput,
  PersonCategory,
  PersonPage,
  PersonQuery,
  PersonRecord,
  PersonStatus,
  PersonType,
  UpdatePersonInput,
} from '@lu/contracts';
import { AuthSecurityGuard, SkipCsrf } from '../auth/auth.guard.js';
import { AuthHeadersInterceptor } from '../auth/auth.interceptor.js';
import { AuthValidationException } from '../auth/auth.exceptions.js';
import type { FastifyRequest } from '../auth/auth.fastify.js';
import { ApiCorrelationGuard, SiteContextGuard } from '../core/core.guard.js';
import { CoreExceptionFilter } from '../core/core.filter.js';
import { PersonService } from './person.service.js';

function context(request: FastifyRequest) {
  if (request.siteContext === undefined) throw new Error('Site context was not resolved.');
  return request.siteContext;
}
function object(value: unknown): Record<string, unknown> {
  if (typeof value !== 'object' || value === null || Array.isArray(value))
    throw new AuthValidationException('Person input is invalid.');
  return value as Record<string, unknown>;
}
function text(value: unknown, field: string): string | null {
  if (value === undefined || value === null) return null;
  if (typeof value !== 'string')
    throw new AuthValidationException('Person input is invalid.', {
      [field]: ['must be a string'],
    });
  return value;
}
function enumValue<T extends string | number>(
  value: unknown,
  field: string,
  allowed: readonly T[],
  fallback: T,
): T {
  if (value === undefined || value === null || value === '') return fallback;
  if (!allowed.includes(value as T))
    throw new AuthValidationException('Person input is invalid.', {
      [field]: ['unsupported value'],
    });
  return value as T;
}
function input(
  value: Record<string, unknown>,
  update: boolean,
): CreatePersonInput | UpdatePersonInput {
  const base: CreatePersonInput = {
    type: enumValue<PersonType>(value.type, 'type', ['internal', 'external'], 'internal'),
    name: text(value.name, 'name') ?? '',
    email: text(value.email, 'email'),
    phoneNumber: text(value.phoneNumber, 'phoneNumber'),
    isEntity: value.isEntity === true,
    address: text(value.address, 'address'),
    category: enumValue<PersonCategory>(value.category, 'category', [1, 2, 3, 4, 5, 99], 99),
    actorCode: text(value.actorCode, 'actorCode'),
  };
  return update
    ? { ...base, status: enumValue<PersonStatus>(value.status, 'status', [0, 1, 2], 0) }
    : base;
}
function query(value: Record<string, string | undefined>): PersonQuery {
  const integer = (key: string) => {
    const raw = value[key];
    if (raw === undefined || raw === '') return undefined;
    if (!/^\d+$/.test(raw)) throw new AuthValidationException('Person query is invalid.');
    return Number(raw);
  };
  const status = integer('statusFilter');
  const category = integer('category');
  if (status !== undefined && ![0, 1, 2].includes(status))
    throw new AuthValidationException('Person query is invalid.');
  if (category !== undefined && ![1, 2, 3, 4, 5, 99].includes(category))
    throw new AuthValidationException('Person query is invalid.');
  const type = value.type;
  if (type !== undefined && type !== 'internal' && type !== 'external')
    throw new AuthValidationException('Person query is invalid.');
  return {
    currentPage: integer('currentPage'),
    statusFilter: status as PersonStatus | undefined,
    category: category as PersonCategory | undefined,
    type: type as PersonType | undefined,
    searchTerm: value.searchTerm?.trim().slice(0, 200) || undefined,
  };
}

@Controller()
@UseFilters(CoreExceptionFilter)
@UseGuards(ApiCorrelationGuard, AuthSecurityGuard, SiteContextGuard)
@UseInterceptors(AuthHeadersInterceptor)
export class PersonController {
  constructor(private readonly service: PersonService) {}
  @Get('people') @SkipCsrf() async list(
    @Req() request: FastifyRequest,
    @Query() value: Record<string, string | undefined>,
  ): Promise<ApiSuccess<PersonPage>> {
    return { success: true, data: await this.service.list(context(request), query(value)) };
  }
  @Get('people/:id') @SkipCsrf() async find(
    @Req() request: FastifyRequest,
    @Param('id', ParseIntPipe) id: number,
  ): Promise<ApiSuccess<PersonRecord>> {
    return { success: true, data: await this.service.find(context(request), id) };
  }
  @Post('people') async create(
    @Req() request: FastifyRequest,
    @Body() raw: unknown,
  ): Promise<ApiSuccess<PersonRecord>> {
    return {
      success: true,
      data: await this.service.create(context(request), input(object(raw), false)),
    };
  }
  @Put('people/:id') async update(
    @Req() request: FastifyRequest,
    @Param('id', ParseIntPipe) id: number,
    @Body() raw: unknown,
  ): Promise<ApiSuccess<PersonRecord>> {
    return {
      success: true,
      data: await this.service.update(
        context(request),
        id,
        input(object(raw), true) as UpdatePersonInput,
      ),
    };
  }
  @Delete('people/:id') async remove(
    @Req() request: FastifyRequest,
    @Param('id', ParseIntPipe) id: number,
  ): Promise<ApiSuccess<null>> {
    await this.service.delete(context(request), id);
    return { success: true, data: null };
  }
}
