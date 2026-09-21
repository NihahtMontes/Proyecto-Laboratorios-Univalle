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
  CreateEquipmentUnitInput,
  EquipmentUnitDetail,
  EquipmentUnitPage,
  EquipmentUnitQuery,
  EquipmentUnitStateHistory,
  EquipmentUnitStatus,
  PhysicalCondition,
  UpdateEquipmentUnitInput,
} from '@lu/contracts';
import { AuthSecurityGuard, SkipCsrf } from '../auth/auth.guard.js';
import { AuthHeadersInterceptor } from '../auth/auth.interceptor.js';
import { AuthValidationException } from '../auth/auth.exceptions.js';
import type { FastifyRequest } from '../auth/auth.fastify.js';
import { CoreExceptionFilter } from '../core/core.filter.js';
import { ApiCorrelationGuard, SiteContextGuard } from '../core/core.guard.js';
import { EquipmentUnitService } from './equipment-unit.service.js';

function context(request: FastifyRequest) {
  if (request.siteContext === undefined) throw new Error('Site context was not resolved.');
  return request.siteContext;
}

function object(value: unknown): Record<string, unknown> {
  if (typeof value !== 'object' || value === null || Array.isArray(value))
    throw new AuthValidationException('Equipment unit input is invalid.');
  return value as Record<string, unknown>;
}

function positiveId(value: unknown, field: string, required = false): number | null {
  if (value === undefined || value === null || value === '') {
    if (required)
      throw new AuthValidationException('Equipment unit input is invalid.', {
        [field]: ['is required'],
      });
    return null;
  }
  if (typeof value !== 'number' || !Number.isSafeInteger(value) || value < 1)
    throw new AuthValidationException('Equipment unit input is invalid.', {
      [field]: ['must be a positive integer'],
    });
  return value;
}

function text(value: unknown, field: string, required = false): string | null {
  if (value === undefined || value === null) {
    if (required)
      throw new AuthValidationException('Equipment unit input is invalid.', {
        [field]: ['is required'],
      });
    return null;
  }
  if (typeof value !== 'string' || (required && value.trim() === ''))
    throw new AuthValidationException('Equipment unit input is invalid.', {
      [field]: ['must be a string'],
    });
  return value;
}

function date(value: unknown, field: string): string | null {
  const result = text(value, field);
  if (result === null || result.trim() === '') return null;
  if (!/^\d{4}-\d{2}-\d{2}$/.test(result) || Number.isNaN(Date.parse(`${result}T00:00:00Z`)))
    throw new AuthValidationException('Equipment unit input is invalid.', {
      [field]: ['must be an ISO date'],
    });
  return result;
}

function money(value: unknown, field: string): number | null {
  if (value === undefined || value === null || value === '') return null;
  if (typeof value !== 'number' || !Number.isFinite(value) || value < 0)
    throw new AuthValidationException('Equipment unit input is invalid.', {
      [field]: ['must be a non-negative number'],
    });
  return value;
}

const statuses: readonly EquipmentUnitStatus[] = [0, 1, 2, 3, 4, 5, 6, 10, 99];
const conditions: readonly PhysicalCondition[] = [1, 2, 3, 4, 5];

function status(value: unknown, required = false): EquipmentUnitStatus {
  if (value === undefined || value === null || value === '') {
    if (required)
      throw new AuthValidationException('Equipment unit input is invalid.', {
        status: ['is required'],
      });
    return 0;
  }
  if (
    typeof value !== 'number' ||
    !Number.isSafeInteger(value) ||
    !statuses.includes(value as EquipmentUnitStatus)
  )
    throw new AuthValidationException('Equipment unit input is invalid.', {
      status: ['has an unsupported value'],
    });
  return value as EquipmentUnitStatus;
}

function physicalCondition(value: unknown): PhysicalCondition | null {
  if (value === undefined || value === null || value === '') return null;
  if (
    typeof value !== 'number' ||
    !Number.isSafeInteger(value) ||
    !conditions.includes(value as PhysicalCondition)
  )
    throw new AuthValidationException('Equipment unit input is invalid.', {
      physicalCondition: ['has an unsupported value'],
    });
  return value as PhysicalCondition;
}

function query(value: Record<string, string | undefined>): EquipmentUnitQuery {
  const integerQuery = (key: string): number | undefined => {
    const raw = value[key];
    if (raw === undefined || raw === '') return undefined;
    if (!/^\d+$/.test(raw))
      throw new AuthValidationException('Equipment unit query is invalid.', {
        [key]: ['must be an integer'],
      });
    return Number(raw);
  };
  const rawStatus = integerQuery('statusFilter');
  const rawResolution = integerQuery('locationResolutionStatus');
  if (rawStatus !== undefined && !statuses.includes(rawStatus as EquipmentUnitStatus))
    throw new AuthValidationException('Equipment unit query is invalid.');
  if (rawResolution !== undefined && rawResolution !== 0 && rawResolution !== 1)
    throw new AuthValidationException('Equipment unit query is invalid.');
  return {
    currentPage: integerQuery('currentPage'),
    searchTerm: value.searchTerm?.trim().slice(0, 200) || undefined,
    laboratoryId: integerQuery('laboratoryId'),
    equipmentId: integerQuery('equipmentId'),
    statusFilter: rawStatus as EquipmentUnitStatus | undefined,
    locationResolutionStatus: rawResolution as 0 | 1 | undefined,
    includeUnresolved: value.includeUnresolved === 'true',
  };
}

function input(
  value: Record<string, unknown>,
  update: boolean,
): CreateEquipmentUnitInput | UpdateEquipmentUnitInput {
  const base: CreateEquipmentUnitInput = {
    equipmentId: positiveId(value.equipmentId, 'equipmentId', true)!,
    laboratoryId: positiveId(value.laboratoryId, 'laboratoryId', true)!,
    careerId: positiveId(value.careerId, 'careerId'),
    inventoryNumber: text(value.inventoryNumber, 'inventoryNumber', true)!,
    serialNumber: text(value.serialNumber, 'serialNumber'),
    internalLocation: text(value.internalLocation, 'internalLocation'),
    acquisitionDate: date(value.acquisitionDate, 'acquisitionDate'),
    manufacturingDate: date(value.manufacturingDate, 'manufacturingDate'),
    acquisitionValue: money(value.acquisitionValue, 'acquisitionValue'),
    currentStatus: status(value.currentStatus),
    physicalCondition: physicalCondition(value.physicalCondition),
    notes: text(value.notes, 'notes'),
  };
  return update ? { ...base, status: status(value.status, true) } : base;
}

@Controller()
@UseFilters(CoreExceptionFilter)
@UseGuards(ApiCorrelationGuard, AuthSecurityGuard, SiteContextGuard)
@UseInterceptors(AuthHeadersInterceptor)
export class EquipmentUnitController {
  constructor(private readonly service: EquipmentUnitService) {}

  @Get('equipment-units')
  @SkipCsrf()
  async list(
    @Req() request: FastifyRequest,
    @Query() value: Record<string, string | undefined>,
  ): Promise<ApiSuccess<EquipmentUnitPage>> {
    return { success: true, data: await this.service.list(context(request), query(value)) };
  }

  @Get('equipment-units/:id')
  @SkipCsrf()
  async find(
    @Req() request: FastifyRequest,
    @Param('id', ParseIntPipe) idValue: number,
  ): Promise<ApiSuccess<EquipmentUnitDetail>> {
    return { success: true, data: await this.service.find(context(request), idValue) };
  }

  @Get('equipment-units/:id/history')
  @SkipCsrf()
  async history(
    @Req() request: FastifyRequest,
    @Param('id', ParseIntPipe) idValue: number,
  ): Promise<ApiSuccess<readonly EquipmentUnitStateHistory[]>> {
    return { success: true, data: await this.service.history(context(request), idValue) };
  }

  @Post('equipment-units')
  async create(
    @Req() request: FastifyRequest,
    @Body() raw: unknown,
  ): Promise<ApiSuccess<EquipmentUnitDetail>> {
    return {
      success: true,
      data: await this.service.create(context(request), input(object(raw), false)),
    };
  }

  @Put('equipment-units/:id')
  async update(
    @Req() request: FastifyRequest,
    @Param('id', ParseIntPipe) idValue: number,
    @Body() raw: unknown,
  ): Promise<ApiSuccess<EquipmentUnitDetail>> {
    return {
      success: true,
      data: await this.service.update(
        context(request),
        idValue,
        input(object(raw), true) as UpdateEquipmentUnitInput,
      ),
    };
  }

  @Delete('equipment-units/:id')
  async remove(
    @Req() request: FastifyRequest,
    @Param('id', ParseIntPipe) idValue: number,
  ): Promise<ApiSuccess<null>> {
    await this.service.remove(context(request), idValue);
    return { success: true, data: null };
  }
}
