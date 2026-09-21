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
  AcquisitionDetail,
  AcquisitionPage,
  AcquisitionQuery,
  ApiSuccess,
  CreateAcquisitionInput,
  MaintenanceCostInput,
  RequestPriority,
  RequestStatus,
  UpdateAcquisitionInput,
} from '@lu/contracts';
import { AuthSecurityGuard, SkipCsrf } from '../auth/auth.guard.js';
import { AuthHeadersInterceptor } from '../auth/auth.interceptor.js';
import { AuthValidationException } from '../auth/auth.exceptions.js';
import type { FastifyRequest } from '../auth/auth.fastify.js';
import { ApiCorrelationGuard, SiteContextGuard } from '../core/core.guard.js';
import { CoreExceptionFilter } from '../core/core.filter.js';
import { AcquisitionService } from './acquisition.service.js';

function context(request: FastifyRequest) {
  if (request.siteContext === undefined) throw new Error('Site context was not resolved.');
  return request.siteContext;
}

function object(value: unknown): Record<string, unknown> {
  if (typeof value !== 'object' || value === null || Array.isArray(value))
    throw new AuthValidationException('Acquisition input is invalid.');
  return value as Record<string, unknown>;
}

function positive(value: unknown, field: string, required = false): number | null {
  if (value === undefined || value === null || value === '') {
    if (required)
      throw new AuthValidationException('Acquisition input is invalid.', {
        [field]: ['is required'],
      });
    return null;
  }
  if (typeof value !== 'number' || !Number.isSafeInteger(value) || value < 1)
    throw new AuthValidationException('Acquisition input is invalid.', {
      [field]: ['must be a positive integer'],
    });
  return value;
}

function decimal(value: unknown, field: string, fallback = 0): number {
  if (value === undefined || value === null || value === '') return fallback;
  if (typeof value !== 'number' || !Number.isFinite(value))
    throw new AuthValidationException('Acquisition input is invalid.', {
      [field]: ['must be numeric'],
    });
  return value;
}

function text(value: unknown, field: string, required = false): string | null {
  if (value === undefined || value === null) {
    if (required)
      throw new AuthValidationException('Acquisition input is invalid.', {
        [field]: ['is required'],
      });
    return null;
  }
  if (typeof value !== 'string')
    throw new AuthValidationException('Acquisition input is invalid.', {
      [field]: ['must be a string'],
    });
  if (required && value.trim() === '')
    throw new AuthValidationException('Acquisition input is invalid.', {
      [field]: ['is required'],
    });
  return value;
}

function date(value: unknown, field: string): string | null {
  const result = text(value, field);
  if (result === null || result === '') return null;
  if (!/^\d{4}-\d{2}-\d{2}$/.test(result))
    throw new AuthValidationException('Acquisition input is invalid.', {
      [field]: ['must be an ISO date'],
    });
  return result;
}

function enumValue<T extends number>(
  value: unknown,
  field: string,
  allowed: readonly T[],
  fallback: T,
): T {
  if (value === undefined || value === null || value === '') return fallback;
  if (typeof value !== 'number' || !allowed.includes(value as T))
    throw new AuthValidationException('Acquisition input is invalid.', {
      [field]: ['unsupported value'],
    });
  return value as T;
}

function costs(value: unknown): readonly MaintenanceCostInput[] {
  if (value === undefined) return [];
  if (!Array.isArray(value))
    throw new AuthValidationException('Acquisition input is invalid.', {
      costs: ['must be an array'],
    });
  if (value.length > 100)
    throw new AuthValidationException('Acquisition input is invalid.', {
      costs: ['must contain at most 100 rows'],
    });
  return value.map((raw) => {
    const row = object(raw);
    return {
      id: positive(row.id, 'costId') ?? undefined,
      concept: text(row.concept, 'concept', true)!,
      description: text(row.description, 'description'),
      quantity: decimal(row.quantity, 'quantity', 1),
      unitOfMeasure: text(row.unitOfMeasure, 'unitOfMeasure'),
      unitPrice: decimal(row.unitPrice, 'unitPrice'),
      category: enumValue<number>(row.category, 'category', [0, 1, 2, 3, 4, 5], 1),
      provider: text(row.provider, 'provider'),
      costDate: date(row.costDate, 'costDate'),
      invoiceNumber: text(row.invoiceNumber, 'invoiceNumber'),
    };
  });
}

function input(
  value: Record<string, unknown>,
  update: boolean,
): CreateAcquisitionInput | UpdateAcquisitionInput {
  const base: CreateAcquisitionInput = {
    managementId: positive(value.managementId, 'managementId', true)!,
    equipmentUnitId: positive(value.equipmentUnitId, 'equipmentUnitId', true)!,
    description: text(value.description, 'description', true)!,
    observations: text(value.observations, 'observations'),
    priority: enumValue<RequestPriority>(value.priority, 'priority', [0, 1, 2, 3], 1),
    investmentCode: text(value.investmentCode, 'investmentCode', true)!,
    costCenter: text(value.costCenter, 'costCenter', true)!,
    costs: costs(value.costs),
    isDraft: value.isDraft === true,
  };
  if (!update) return base;
  return {
    ...base,
    status: enumValue<RequestStatus>(value.status, 'status', [0, 1, 2, 3, 4, 5, 99], 0),
  };
}

function query(value: Record<string, string | undefined>): AcquisitionQuery {
  const integer = (key: string) => {
    const raw = value[key];
    if (raw === undefined || raw === '') return undefined;
    if (!/^\d+$/.test(raw)) throw new AuthValidationException('Acquisition query is invalid.');
    return Number(raw);
  };
  const status = integer('statusFilter');
  if (status !== undefined && ![0, 1, 2, 3, 4, 5, 99].includes(status))
    throw new AuthValidationException('Acquisition query is invalid.');
  return {
    currentPage: integer('currentPage'),
    managementId: integer('managementId'),
    statusFilter: status as RequestStatus | undefined,
    searchTerm: value.searchTerm?.trim().slice(0, 200) || undefined,
  };
}

@Controller()
@UseFilters(CoreExceptionFilter)
@UseGuards(ApiCorrelationGuard, AuthSecurityGuard, SiteContextGuard)
@UseInterceptors(AuthHeadersInterceptor)
export class AcquisitionController {
  constructor(private readonly service: AcquisitionService) {}

  @Get('acquisitions')
  @SkipCsrf()
  async list(
    @Req() request: FastifyRequest,
    @Query() value: Record<string, string | undefined>,
  ): Promise<ApiSuccess<AcquisitionPage>> {
    return { success: true, data: await this.service.list(context(request), query(value)) };
  }

  @Get('acquisitions/:id')
  @SkipCsrf()
  async find(
    @Req() request: FastifyRequest,
    @Param('id', ParseIntPipe) id: number,
  ): Promise<ApiSuccess<AcquisitionDetail>> {
    return { success: true, data: await this.service.find(context(request), id) };
  }

  @Post('acquisitions')
  async create(
    @Req() request: FastifyRequest,
    @Body() raw: unknown,
  ): Promise<ApiSuccess<AcquisitionDetail>> {
    return {
      success: true,
      data: await this.service.save(context(request), input(object(raw), false)),
    };
  }

  @Put('acquisitions/:id')
  async update(
    @Req() request: FastifyRequest,
    @Param('id', ParseIntPipe) id: number,
    @Body() raw: unknown,
  ): Promise<ApiSuccess<AcquisitionDetail>> {
    return {
      success: true,
      data: await this.service.update(
        context(request),
        id,
        input(object(raw), true) as UpdateAcquisitionInput,
      ),
    };
  }

  @Post('acquisitions/:id/complete')
  async complete(
    @Req() request: FastifyRequest,
    @Param('id', ParseIntPipe) id: number,
  ): Promise<ApiSuccess<AcquisitionDetail>> {
    return { success: true, data: await this.service.complete(context(request), id) };
  }

  @Post('acquisitions/:id/cancel')
  async cancel(
    @Req() request: FastifyRequest,
    @Param('id', ParseIntPipe) id: number,
  ): Promise<ApiSuccess<null>> {
    await this.service.cancel(context(request), id);
    return { success: true, data: null };
  }

  @Delete('acquisitions/:id')
  async remove(
    @Req() request: FastifyRequest,
    @Param('id', ParseIntPipe) id: number,
  ): Promise<ApiSuccess<null>> {
    await this.service.cancel(context(request), id);
    return { success: true, data: null };
  }
}
