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
  CreateRequestInput,
  RequestDetail,
  RequestPage,
  RequestPriority,
  RequestQuery,
  RequestStatus,
  RequestType,
  UpdateRequestInput,
} from '@lu/contracts';
import { AuthSecurityGuard, SkipCsrf } from '../auth/auth.guard.js';
import { AuthHeadersInterceptor } from '../auth/auth.interceptor.js';
import { AuthValidationException } from '../auth/auth.exceptions.js';
import type { FastifyRequest } from '../auth/auth.fastify.js';
import { ApiCorrelationGuard, SiteContextGuard } from '../core/core.guard.js';
import { CoreExceptionFilter } from '../core/core.filter.js';
import { RequestService } from './request.service.js';

function context(request: FastifyRequest) {
  if (request.siteContext === undefined) throw new Error('Site context was not resolved.');
  return request.siteContext;
}
function object(value: unknown): Record<string, unknown> {
  if (typeof value !== 'object' || value === null || Array.isArray(value))
    throw new AuthValidationException('Request input is invalid.');
  return value as Record<string, unknown>;
}
function positive(value: unknown, field: string, required = false): number | null {
  if (value === undefined || value === null || value === '') {
    if (required)
      throw new AuthValidationException('Request input is invalid.', { [field]: ['is required'] });
    return null;
  }
  if (typeof value !== 'number' || !Number.isSafeInteger(value) || value < 1)
    throw new AuthValidationException('Request input is invalid.', {
      [field]: ['must be a positive integer'],
    });
  return value;
}
function text(value: unknown, field: string): string | null {
  if (value === undefined || value === null) return null;
  if (typeof value !== 'string')
    throw new AuthValidationException('Request input is invalid.', {
      [field]: ['must be a string'],
    });
  return value;
}
function date(value: unknown, field: string): string | null {
  const result = text(value, field);
  if (result === null || result === '') return null;
  if (!/^\d{4}-\d{2}-\d{2}$/.test(result))
    throw new AuthValidationException('Request input is invalid.', {
      [field]: ['must be an ISO date'],
    });
  return result;
}
function enumValue<T extends number>(
  value: unknown,
  field: string,
  allowed: readonly T[],
  fallback?: T,
): T {
  if (value === undefined || value === null || value === '') {
    if (fallback !== undefined) return fallback;
    throw new AuthValidationException('Request input is invalid.', { [field]: ['is required'] });
  }
  if (typeof value !== 'number' || !allowed.includes(value as T))
    throw new AuthValidationException('Request input is invalid.', {
      [field]: ['unsupported value'],
    });
  return value as T;
}
function input(
  value: Record<string, unknown>,
  update: boolean,
): CreateRequestInput | UpdateRequestInput {
  const base: CreateRequestInput = {
    managementId: positive(value.managementId, 'managementId', true)!,
    equipmentUnitId: positive(value.equipmentUnitId, 'equipmentUnitId', true)!,
    description: text(value.description, 'description') ?? '',
    priority: enumValue<RequestPriority>(value.priority, 'priority', [0, 1, 2, 3], 1),
    observations: text(value.observations, 'observations'),
    suggestion: text(value.suggestion, 'suggestion'),
    requestDate: date(value.requestDate, 'requestDate'),
    estimatedRepairTime: text(value.estimatedRepairTime, 'estimatedRepairTime'),
    type: enumValue<RequestType>(value.type, 'type', [1, 2, 3], 1),
    investmentCode: text(value.investmentCode, 'investmentCode'),
    costCenter: text(value.costCenter, 'costCenter'),
    isDraft: value.isDraft === true,
  };
  if (!update) return base;
  return {
    ...base,
    status: enumValue<RequestStatus>(value.status, 'status', [0, 1, 2, 3, 4, 5, 99], 0),
    rejectionReason: text(value.rejectionReason, 'rejectionReason'),
  };
}
function query(value: Record<string, string | undefined>): RequestQuery {
  const int = (key: string) => {
    const raw = value[key];
    if (raw === undefined || raw === '') return undefined;
    if (!/^\d+$/.test(raw)) throw new AuthValidationException('Request query is invalid.');
    return Number(raw);
  };
  const status = int('statusFilter');
  const priority = int('priorityFilter');
  const type = int('type');
  if (status !== undefined && ![0, 1, 2, 3, 4, 5, 99].includes(status))
    throw new AuthValidationException('Request query is invalid.');
  if (priority !== undefined && ![0, 1, 2, 3].includes(priority))
    throw new AuthValidationException('Request query is invalid.');
  if (type !== undefined && ![1, 2, 3].includes(type))
    throw new AuthValidationException('Request query is invalid.');
  return {
    currentPage: int('currentPage'),
    managementId: int('managementId'),
    laboratoryId: int('laboratoryId'),
    statusFilter: status as RequestStatus | undefined,
    priorityFilter: priority as RequestPriority | undefined,
    type: type as RequestType | undefined,
    searchTerm: value.searchTerm?.trim().slice(0, 200) || undefined,
  };
}

@Controller()
@UseFilters(CoreExceptionFilter)
@UseGuards(ApiCorrelationGuard, AuthSecurityGuard, SiteContextGuard)
@UseInterceptors(AuthHeadersInterceptor)
export class RequestController {
  constructor(private readonly service: RequestService) {}

  @Get('requests')
  @SkipCsrf()
  async list(
    @Req() request: FastifyRequest,
    @Query() value: Record<string, string | undefined>,
  ): Promise<ApiSuccess<RequestPage>> {
    return { success: true, data: await this.service.list(context(request), query(value)) };
  }

  @Get('requests/:id')
  @SkipCsrf()
  async find(
    @Req() request: FastifyRequest,
    @Param('id', ParseIntPipe) id: number,
  ): Promise<ApiSuccess<RequestDetail>> {
    return { success: true, data: await this.service.find(context(request), id) };
  }

  @Post('requests')
  async create(
    @Req() request: FastifyRequest,
    @Body() raw: unknown,
  ): Promise<ApiSuccess<RequestDetail>> {
    return {
      success: true,
      data: await this.service.save(context(request), input(object(raw), false)),
    };
  }

  @Put('requests/:id')
  async update(
    @Req() request: FastifyRequest,
    @Param('id', ParseIntPipe) id: number,
    @Body() raw: unknown,
  ): Promise<ApiSuccess<RequestDetail>> {
    return {
      success: true,
      data: await this.service.update(
        context(request),
        id,
        input(object(raw), true) as UpdateRequestInput,
      ),
    };
  }

  @Post('requests/:id/complete')
  async complete(
    @Req() request: FastifyRequest,
    @Param('id', ParseIntPipe) id: number,
  ): Promise<ApiSuccess<RequestDetail>> {
    return { success: true, data: await this.service.complete(context(request), id) };
  }

  @Post('requests/:id/cancel')
  async cancel(
    @Req() request: FastifyRequest,
    @Param('id', ParseIntPipe) id: number,
  ): Promise<ApiSuccess<null>> {
    await this.service.cancel(context(request), id);
    return { success: true, data: null };
  }

  @Delete('requests/:id')
  async remove(
    @Req() request: FastifyRequest,
    @Param('id', ParseIntPipe) id: number,
  ): Promise<ApiSuccess<null>> {
    await this.service.cancel(context(request), id);
    return { success: true, data: null };
  }
}
