import {
  Body,
  Controller,
  Get,
  Param,
  ParseIntPipe,
  Post,
  Query,
  Req,
  UseFilters,
  UseGuards,
  UseInterceptors,
} from '@nestjs/common';
import type {
  ApiSuccess,
  KardexInput,
  KardexPage,
  KardexQuery,
  KardexRecord,
  MaintenanceCostInput,
  MaintenanceSatisfaction,
  MaintenanceTaskInput,
} from '@lu/contracts';
import { AuthSecurityGuard, SkipCsrf } from '../auth/auth.guard.js';
import { AuthHeadersInterceptor } from '../auth/auth.interceptor.js';
import { AuthValidationException } from '../auth/auth.exceptions.js';
import type { FastifyRequest } from '../auth/auth.fastify.js';
import { ApiCorrelationGuard, SiteContextGuard } from '../core/core.guard.js';
import { CoreExceptionFilter } from '../core/core.filter.js';
import { KardexService } from './kardex.service.js';
function context(request: FastifyRequest) {
  if (request.siteContext === undefined) throw new Error('Site context was not resolved.');
  return request.siteContext;
}
function object(value: unknown): Record<string, unknown> {
  if (typeof value !== 'object' || value === null || Array.isArray(value))
    throw new AuthValidationException('Kardex input is invalid.');
  return value as Record<string, unknown>;
}
function positive(value: unknown, field: string): number | null {
  if (value === undefined || value === null || value === '') return null;
  if (typeof value !== 'number' || !Number.isSafeInteger(value) || value < 1)
    throw new AuthValidationException('Kardex input is invalid', { [field]: ['must be positive'] });
  return value;
}
function text(value: unknown, field: string): string | null {
  if (value === undefined || value === null) return null;
  if (typeof value !== 'string')
    throw new AuthValidationException('Kardex input is invalid', { [field]: ['must be a string'] });
  return value;
}
function date(value: unknown, field: string): string | null {
  const result = text(value, field);
  if (result === null || result === '') return null;
  if (!/^\d{4}-\d{2}-\d{2}$/.test(result))
    throw new AuthValidationException('Kardex input is invalid', {
      [field]: ['must be an ISO date'],
    });
  return result;
}
function tasks(value: unknown): readonly MaintenanceTaskInput[] {
  if (value === undefined) return [];
  if (!Array.isArray(value))
    throw new AuthValidationException('Kardex input is invalid', { tasks: ['must be an array'] });
  return value.map((raw) => {
    const row = object(raw);
    return {
      id: positive(row.id, 'taskId') ?? undefined,
      description: text(row.description, 'description') ?? '',
      isCompleted: row.isCompleted === true,
    };
  });
}
function costs(value: unknown): readonly MaintenanceCostInput[] {
  if (value === undefined) return [];
  if (!Array.isArray(value))
    throw new AuthValidationException('Kardex input is invalid', { costs: ['must be an array'] });
  return value.map((raw) => {
    const row = object(raw);
    if (typeof row.quantity !== 'number' || typeof row.unitPrice !== 'number')
      throw new AuthValidationException('Kardex input is invalid', {
        costs: ['quantity and unitPrice must be numbers'],
      });
    return {
      id: positive(row.id, 'costId') ?? undefined,
      concept: text(row.concept, 'concept') ?? '',
      description: text(row.description, 'description'),
      quantity: row.quantity,
      unitOfMeasure: text(row.unitOfMeasure, 'unitOfMeasure'),
      unitPrice: row.unitPrice,
      category: typeof row.category === 'number' ? row.category : 1,
      provider: text(row.provider, 'provider'),
      costDate: date(row.costDate, 'costDate'),
      invoiceNumber: text(row.invoiceNumber, 'invoiceNumber'),
    };
  });
}
function input(value: Record<string, unknown>, planId: number): KardexInput {
  return {
    planId,
    technicianId: positive(value.technicianId, 'technicianId'),
    scheduledDate: date(value.scheduledDate, 'scheduledDate'),
    startDate: text(value.startDate, 'startDate'),
    endDate: text(value.endDate, 'endDate'),
    actualReturnDate: date(value.actualReturnDate, 'actualReturnDate'),
    description: text(value.description, 'description'),
    actualCost: typeof value.actualCost === 'number' ? value.actualCost : null,
    suggestedNextMaintenanceDate: date(
      value.suggestedNextMaintenanceDate,
      'suggestedNextMaintenanceDate',
    ),
    satisfactionLevel:
      value.satisfactionLevel === null || value.satisfactionLevel === undefined
        ? null
        : [1, 2, 3, 4, 5].includes(value.satisfactionLevel as number)
          ? (value.satisfactionLevel as MaintenanceSatisfaction)
          : 3,
    recommendations: text(value.recommendations, 'recommendations'),
    observations: text(value.observations, 'observations'),
    tasks: tasks(value.tasks),
    costs: costs(value.costs),
    isDraft: value.isDraft === true,
  };
}
function query(value: Record<string, string | undefined>): KardexQuery {
  const int = (key: string) => {
    const raw = value[key];
    if (raw === undefined || raw === '') return undefined;
    if (!/^\d+$/.test(raw)) throw new AuthValidationException('Kardex query is invalid.');
    return Number(raw);
  };
  return {
    currentPage: int('currentPage'),
    managementId: int('managementId'),
    laboratoryId: int('laboratoryId'),
    searchTerm: value.searchTerm?.trim().slice(0, 200) || undefined,
  };
}
@Controller()
@UseFilters(CoreExceptionFilter)
@UseGuards(ApiCorrelationGuard, AuthSecurityGuard, SiteContextGuard)
@UseInterceptors(AuthHeadersInterceptor)
export class KardexController {
  constructor(private readonly service: KardexService) {}
  @Get('kardex') @SkipCsrf() async list(
    @Req() request: FastifyRequest,
    @Query() value: Record<string, string | undefined>,
  ): Promise<ApiSuccess<KardexPage>> {
    return { success: true, data: await this.service.list(context(request), query(value)) };
  }
  @Get('kardex/:planId') @SkipCsrf() async find(
    @Req() request: FastifyRequest,
    @Param('planId', ParseIntPipe) planId: number,
  ): Promise<ApiSuccess<KardexRecord>> {
    return { success: true, data: await this.service.find(context(request), planId) };
  }
  @Post('kardex/:planId/draft') async draft(
    @Req() request: FastifyRequest,
    @Param('planId', ParseIntPipe) planId: number,
    @Body() raw: unknown,
  ): Promise<ApiSuccess<KardexRecord>> {
    return {
      success: true,
      data: await this.service.draft(context(request), input(object(raw), planId)),
    };
  }
  @Post('kardex/:planId/complete') async complete(
    @Req() request: FastifyRequest,
    @Param('planId', ParseIntPipe) planId: number,
    @Body() raw: unknown,
  ): Promise<ApiSuccess<KardexRecord>> {
    return {
      success: true,
      data: await this.service.complete(context(request), input(object(raw), planId)),
    };
  }
}
