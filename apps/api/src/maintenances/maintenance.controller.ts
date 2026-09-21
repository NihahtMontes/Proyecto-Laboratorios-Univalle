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
  CreateMaintenanceInput,
  MaintenanceDetail,
  MaintenancePage,
  MaintenanceQuery,
  MaintenanceStatus,
  MaintenanceType,
  MaintenanceServiceType,
  MaintenanceSatisfaction,
  MaintenanceTaskInput,
  MaintenanceCostInput,
  UpdateMaintenanceInput,
} from '@lu/contracts';
import { AuthSecurityGuard, SkipCsrf } from '../auth/auth.guard.js';
import { AuthHeadersInterceptor } from '../auth/auth.interceptor.js';
import { AuthValidationException } from '../auth/auth.exceptions.js';
import type { FastifyRequest } from '../auth/auth.fastify.js';
import { ApiCorrelationGuard, SiteContextGuard } from '../core/core.guard.js';
import { CoreExceptionFilter } from '../core/core.filter.js';
import { MaintenanceService } from './maintenance.service.js';

function context(request: FastifyRequest) {
  if (request.siteContext === undefined) throw new Error('Site context was not resolved.');
  return request.siteContext;
}
function object(value: unknown): Record<string, unknown> {
  if (typeof value !== 'object' || value === null || Array.isArray(value))
    throw new AuthValidationException('Maintenance input is invalid.');
  return value as Record<string, unknown>;
}
function positive(value: unknown, field: string, required = false): number | null {
  if (value === undefined || value === null || value === '') {
    if (required)
      throw new AuthValidationException('Maintenance input is invalid.', {
        [field]: ['is required'],
      });
    return null;
  }
  if (typeof value !== 'number' || !Number.isSafeInteger(value) || value < 1)
    throw new AuthValidationException('Maintenance input is invalid.', {
      [field]: ['must be positive'],
    });
  return value;
}
function text(value: unknown, field: string): string | null {
  if (value === undefined || value === null) return null;
  if (typeof value !== 'string')
    throw new AuthValidationException('Maintenance input is invalid.', {
      [field]: ['must be a string'],
    });
  return value;
}
function date(value: unknown, field: string): string | null {
  const result = text(value, field);
  if (result === null || result === '') return null;
  if (!/^\d{4}-\d{2}-\d{2}$/.test(result))
    throw new AuthValidationException('Maintenance input is invalid.', {
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
    throw new AuthValidationException('Maintenance input is invalid.', {
      [field]: ['unsupported value'],
    });
  return value as T;
}
function tasks(value: unknown): readonly MaintenanceTaskInput[] {
  if (value === undefined) return [];
  if (!Array.isArray(value))
    throw new AuthValidationException('Maintenance input is invalid.', {
      tasks: ['must be an array'],
    });
  return value.map((item) => {
    const row = object(item);
    return {
      id: positive(row.id, 'taskId') ?? undefined,
      description: text(row.description, 'taskDescription') ?? '',
      isCompleted: row.isCompleted === true,
    };
  });
}
function costs(value: unknown): readonly MaintenanceCostInput[] {
  if (value === undefined) return [];
  if (!Array.isArray(value))
    throw new AuthValidationException('Maintenance input is invalid.', {
      costs: ['must be an array'],
    });
  return value.map((item) => {
    const row = object(item);
    if (typeof row.quantity !== 'number' || typeof row.unitPrice !== 'number')
      throw new AuthValidationException('Maintenance input is invalid.', {
        costs: ['quantity and unitPrice must be numbers'],
      });
    return {
      id: positive(row.id, 'costId') ?? undefined,
      concept: text(row.concept, 'concept') ?? '',
      description: text(row.description, 'costDescription'),
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
function input(
  value: Record<string, unknown>,
  update: boolean,
): CreateMaintenanceInput | UpdateMaintenanceInput {
  const base: CreateMaintenanceInput = {
    managementId: positive(value.managementId, 'managementId', true)!,
    equipmentUnitId: positive(value.equipmentUnitId, 'equipmentUnitId', true)!,
    requestId: positive(value.requestId, 'requestId'),
    maintenanceType: enumValue<MaintenanceType>(
      value.maintenanceType,
      'maintenanceType',
      [1, 2, 3, 4, 5, 99],
      99,
    ),
    serviceType: enumValue<MaintenanceServiceType>(value.serviceType, 'serviceType', [0, 1], 0),
    institutionalCode: text(value.institutionalCode, 'institutionalCode'),
    technicianId: positive(value.technicianId, 'technicianId'),
    scheduledDate: date(value.scheduledDate, 'scheduledDate'),
    startDate: text(value.startDate, 'startDate'),
    endDate: text(value.endDate, 'endDate'),
    description: text(value.description, 'description'),
    status: enumValue<MaintenanceStatus>(value.status, 'status', [0, 1, 2, 3, 99], 0),
    estimatedCost: typeof value.estimatedCost === 'number' ? value.estimatedCost : null,
    actualCost: typeof value.actualCost === 'number' ? value.actualCost : null,
    recommendations: text(value.recommendations, 'recommendations'),
    suggestedNextMaintenanceDate: date(
      value.suggestedNextMaintenanceDate,
      'suggestedNextMaintenanceDate',
    ),
    satisfactionLevel:
      value.satisfactionLevel === null || value.satisfactionLevel === undefined
        ? null
        : enumValue<MaintenanceSatisfaction>(
            value.satisfactionLevel,
            'satisfactionLevel',
            [1, 2, 3, 4, 5],
            3,
          ),
    observations: text(value.observations, 'observations'),
    tasks: tasks(value.tasks),
    costs: costs(value.costs),
    isDraft: value.isDraft === true,
  };
  return update
    ? { ...base, status: enumValue<MaintenanceStatus>(value.status, 'status', [0, 1, 2, 3, 99], 0) }
    : base;
}
function query(value: Record<string, string | undefined>): MaintenanceQuery {
  const int = (key: string) => {
    const raw = value[key];
    if (raw === undefined || raw === '') return undefined;
    if (!/^\d+$/.test(raw)) throw new AuthValidationException('Maintenance query is invalid.');
    return Number(raw);
  };
  const status = int('statusFilter');
  if (status !== undefined && ![0, 1, 2, 3, 99].includes(status))
    throw new AuthValidationException('Maintenance query is invalid.');
  return {
    currentPage: int('currentPage'),
    managementId: int('managementId'),
    laboratoryId: int('laboratoryId'),
    statusFilter: status as MaintenanceStatus | undefined,
    searchTerm: value.searchTerm?.trim().slice(0, 200) || undefined,
  };
}

@Controller()
@UseFilters(CoreExceptionFilter)
@UseGuards(ApiCorrelationGuard, AuthSecurityGuard, SiteContextGuard)
@UseInterceptors(AuthHeadersInterceptor)
export class MaintenanceController {
  constructor(private readonly service: MaintenanceService) {}
  @Get('maintenances') @SkipCsrf() async list(
    @Req() request: FastifyRequest,
    @Query() value: Record<string, string | undefined>,
  ): Promise<ApiSuccess<MaintenancePage>> {
    return { success: true, data: await this.service.list(context(request), query(value)) };
  }
  @Get('maintenances/:id') @SkipCsrf() async find(
    @Req() request: FastifyRequest,
    @Param('id', ParseIntPipe) id: number,
  ): Promise<ApiSuccess<MaintenanceDetail>> {
    return { success: true, data: await this.service.find(context(request), id) };
  }
  @Post('maintenances') async create(
    @Req() request: FastifyRequest,
    @Body() raw: unknown,
  ): Promise<ApiSuccess<MaintenanceDetail>> {
    return {
      success: true,
      data: await this.service.save(context(request), input(object(raw), false)),
    };
  }
  @Put('maintenances/:id') async update(
    @Req() request: FastifyRequest,
    @Param('id', ParseIntPipe) id: number,
    @Body() raw: unknown,
  ): Promise<ApiSuccess<MaintenanceDetail>> {
    return {
      success: true,
      data: await this.service.update(
        context(request),
        id,
        input(object(raw), true) as UpdateMaintenanceInput,
      ),
    };
  }
  @Post('maintenances/:id/complete') async complete(
    @Req() request: FastifyRequest,
    @Param('id', ParseIntPipe) id: number,
  ): Promise<ApiSuccess<MaintenanceDetail>> {
    return { success: true, data: await this.service.complete(context(request), id) };
  }
  @Post('maintenances/:id/cancel') async cancel(
    @Req() request: FastifyRequest,
    @Param('id', ParseIntPipe) id: number,
  ): Promise<ApiSuccess<null>> {
    await this.service.cancel(context(request), id);
    return { success: true, data: null };
  }
  @Delete('maintenances/:id') async remove(
    @Req() request: FastifyRequest,
    @Param('id', ParseIntPipe) id: number,
  ): Promise<ApiSuccess<null>> {
    await this.service.cancel(context(request), id);
    return { success: true, data: null };
  }
}
