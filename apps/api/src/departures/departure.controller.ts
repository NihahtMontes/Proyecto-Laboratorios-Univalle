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
  CreateDepartureInput,
  DepartureDetail,
  DeparturePage,
  DepartureQuery,
  DepartureType,
  LoanStatus,
  MassDepartureInput,
  UpdateDepartureInput,
} from '@lu/contracts';
import { AuthSecurityGuard, SkipCsrf } from '../auth/auth.guard.js';
import { AuthHeadersInterceptor } from '../auth/auth.interceptor.js';
import { AuthValidationException } from '../auth/auth.exceptions.js';
import type { FastifyRequest } from '../auth/auth.fastify.js';
import { ApiCorrelationGuard, SiteContextGuard } from '../core/core.guard.js';
import { CoreExceptionFilter } from '../core/core.filter.js';
import { DepartureService } from './departure.service.js';

function context(request: FastifyRequest) {
  if (request.siteContext === undefined) throw new Error('Site context was not resolved.');
  return request.siteContext;
}
function object(value: unknown): Record<string, unknown> {
  if (typeof value !== 'object' || value === null || Array.isArray(value))
    throw new AuthValidationException('Departure input is invalid.');
  return value as Record<string, unknown>;
}
function positive(value: unknown, field: string): number | null {
  if (value === undefined || value === null || value === '') return null;
  if (typeof value !== 'number' || !Number.isSafeInteger(value) || value < 1)
    throw new AuthValidationException('Departure input is invalid', {
      [field]: ['must be positive'],
    });
  return value;
}
function text(value: unknown, field: string): string | null {
  if (value === undefined || value === null) return null;
  if (typeof value !== 'string')
    throw new AuthValidationException('Departure input is invalid', {
      [field]: ['must be a string'],
    });
  return value;
}
function date(value: unknown, field: string, required = false): string | null {
  const result = text(value, field);
  if (result === null || result === '') {
    if (required)
      throw new AuthValidationException('Departure input is invalid', { [field]: ['is required'] });
    return null;
  }
  if (!/^\d{4}-\d{2}-\d{2}$/.test(result))
    throw new AuthValidationException('Departure input is invalid', {
      [field]: ['must be an ISO date'],
    });
  return result;
}
function numberEnum<T extends number>(
  value: unknown,
  field: string,
  allowed: readonly T[],
  fallback: T,
): T {
  if (value === undefined || value === null || value === '') return fallback;
  if (typeof value !== 'number' || !allowed.includes(value as T))
    throw new AuthValidationException('Departure input is invalid', {
      [field]: ['unsupported value'],
    });
  return value as T;
}
function items(value: unknown) {
  if (value === undefined) return [];
  if (!Array.isArray(value))
    throw new AuthValidationException('Departure input is invalid', {
      items: ['must be an array'],
    });
  return value.map((raw) => {
    const row = object(raw);
    return {
      id: positive(row.id, 'itemId') ?? undefined,
      equipmentUnitId: positive(row.equipmentUnitId, 'equipmentUnitId'),
      productName: text(row.productName, 'productName'),
      quantity: typeof row.quantity === 'number' ? row.quantity : null,
      unitOfMeasure: text(row.unitOfMeasure, 'unitOfMeasure'),
      returnedQuantity: typeof row.returnedQuantity === 'number' ? row.returnedQuantity : null,
      observations: text(row.observations, 'observations'),
    };
  });
}
function input(
  value: Record<string, unknown>,
  update: boolean,
): CreateDepartureInput | UpdateDepartureInput {
  const base: CreateDepartureInput = {
    managementId: positive(value.managementId, 'managementId')!,
    equipmentUnitId: positive(value.equipmentUnitId, 'equipmentUnitId'),
    borrowerId: positive(value.borrowerId, 'borrowerId'),
    originLaboratoryId: positive(value.originLaboratoryId, 'originLaboratoryId'),
    destination: text(value.destination, 'destination'),
    type: numberEnum<DepartureType>(value.type, 'type', [1, 2, 3, 4, 5], 2),
    departureDate: date(value.departureDate, 'departureDate', true)!,
    estimatedReturnDate: date(value.estimatedReturnDate, 'estimatedReturnDate'),
    actualReturnDate: date(value.actualReturnDate, 'actualReturnDate'),
    departureObservations: text(value.departureObservations, 'departureObservations'),
    returnObservations: text(value.returnObservations, 'returnObservations'),
    status: numberEnum<LoanStatus>(value.status, 'status', [0, 1, 2, 99], 0),
    items: items(value.items),
    isDraft: value.isDraft === true,
  };
  return update
    ? { ...base, status: numberEnum<LoanStatus>(value.status, 'status', [0, 1, 2, 99], 0) }
    : base;
}
function query(value: Record<string, string | undefined>): DepartureQuery {
  const int = (key: string) => {
    const raw = value[key];
    if (raw === undefined || raw === '') return undefined;
    if (!/^\d+$/.test(raw)) throw new AuthValidationException('Departure query is invalid.');
    return Number(raw);
  };
  const status = int('statusFilter');
  const type = int('type');
  if (status !== undefined && !([0, 1, 2, 99] as number[]).includes(status))
    throw new AuthValidationException('Departure query is invalid.');
  if (type !== undefined && !([1, 2, 3, 4, 5] as number[]).includes(type))
    throw new AuthValidationException('Departure query is invalid.');
  return {
    currentPage: int('currentPage'),
    managementId: int('managementId'),
    laboratoryId: int('laboratoryId'),
    statusFilter: status as LoanStatus | undefined,
    type: type as DepartureType | undefined,
    searchTerm: value.searchTerm?.trim().slice(0, 200) || undefined,
  };
}
function mass(value: Record<string, unknown>): MassDepartureInput {
  const rows = value.rows;
  if (!Array.isArray(rows) || rows.length === 0)
    throw new AuthValidationException('Departure input is invalid', { rows: ['is required'] });
  return {
    managementId: positive(value.managementId, 'managementId')!,
    type: numberEnum<DepartureType>(value.type, 'type', [1, 2, 3, 4, 5], 2),
    destination: text(value.destination, 'destination'),
    departureDate: date(value.departureDate, 'departureDate', true)!,
    estimatedReturnDate: date(value.estimatedReturnDate, 'estimatedReturnDate'),
    isDraft: value.isDraft === true,
    rows: rows.map((raw) => {
      const row = object(raw);
      return {
        equipmentUnitId: positive(row.equipmentUnitId, 'equipmentUnitId')!,
        planId: positive(row.planId, 'planId') ?? undefined,
      };
    }),
  };
}

@Controller()
@UseFilters(CoreExceptionFilter)
@UseGuards(ApiCorrelationGuard, AuthSecurityGuard, SiteContextGuard)
@UseInterceptors(AuthHeadersInterceptor)
export class DepartureController {
  constructor(private readonly service: DepartureService) {}
  @Get('departures') @SkipCsrf() async list(
    @Req() request: FastifyRequest,
    @Query() value: Record<string, string | undefined>,
  ): Promise<ApiSuccess<DeparturePage>> {
    return { success: true, data: await this.service.list(context(request), query(value)) };
  }
  @Get('departures/:id') @SkipCsrf() async find(
    @Req() request: FastifyRequest,
    @Param('id', ParseIntPipe) id: number,
  ): Promise<ApiSuccess<DepartureDetail>> {
    return { success: true, data: await this.service.find(context(request), id) };
  }
  @Post('departures') async create(
    @Req() request: FastifyRequest,
    @Body() raw: unknown,
  ): Promise<ApiSuccess<DepartureDetail>> {
    return {
      success: true,
      data: await this.service.save(context(request), input(object(raw), false)),
    };
  }
  @Post('departures/mass') async createMass(
    @Req() request: FastifyRequest,
    @Body() raw: unknown,
  ): Promise<ApiSuccess<readonly DepartureDetail[]>> {
    return { success: true, data: await this.service.mass(context(request), mass(object(raw))) };
  }
  @Put('departures/:id') async update(
    @Req() request: FastifyRequest,
    @Param('id', ParseIntPipe) id: number,
    @Body() raw: unknown,
  ): Promise<ApiSuccess<DepartureDetail>> {
    return {
      success: true,
      data: await this.service.update(
        context(request),
        id,
        input(object(raw), true) as UpdateDepartureInput,
      ),
    };
  }
  @Post('departures/:id/return') async return(
    @Req() request: FastifyRequest,
    @Param('id', ParseIntPipe) id: number,
    @Body() raw: unknown,
  ): Promise<ApiSuccess<DepartureDetail>> {
    const value = object(raw);
    return {
      success: true,
      data: await this.service.return(
        context(request),
        id,
        text(value.observations, 'returnObservations'),
      ),
    };
  }
  @Post('departures/:id/cancel') async cancel(
    @Req() request: FastifyRequest,
    @Param('id', ParseIntPipe) id: number,
  ): Promise<ApiSuccess<null>> {
    await this.service.cancel(context(request), id);
    return { success: true, data: null };
  }
  @Delete('departures/:id') async remove(
    @Req() request: FastifyRequest,
    @Param('id', ParseIntPipe) id: number,
  ): Promise<ApiSuccess<null>> {
    await this.service.cancel(context(request), id);
    return { success: true, data: null };
  }
}
