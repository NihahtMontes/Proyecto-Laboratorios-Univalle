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
  CreateVerificationInput,
  EquipmentUnitStatus,
  MassVerificationInput,
  PhysicalCondition,
  VerificationCheckItem,
  VerificationDetail,
  VerificationPage,
  VerificationQuery,
  VerificationStatus,
} from '@lu/contracts';
import { AuthSecurityGuard, SkipCsrf } from '../auth/auth.guard.js';
import { AuthHeadersInterceptor } from '../auth/auth.interceptor.js';
import { AuthValidationException } from '../auth/auth.exceptions.js';
import type { FastifyRequest } from '../auth/auth.fastify.js';
import { ApiCorrelationGuard, SiteContextGuard } from '../core/core.guard.js';
import { CoreExceptionFilter } from '../core/core.filter.js';
import { VerificationService } from './verification.service.js';

function context(request: FastifyRequest) {
  if (request.siteContext === undefined) throw new Error('Site context was not resolved.');
  return request.siteContext;
}
function object(value: unknown): Record<string, unknown> {
  if (typeof value !== 'object' || value === null || Array.isArray(value))
    throw new AuthValidationException('Verification input is invalid.');
  return value as Record<string, unknown>;
}
function positive(value: unknown, field: string, required = false): number | null {
  if (value === undefined || value === null || value === '') {
    if (required)
      throw new AuthValidationException('Verification input is invalid.', {
        [field]: ['is required'],
      });
    return null;
  }
  if (typeof value !== 'number' || !Number.isSafeInteger(value) || value < 1)
    throw new AuthValidationException('Verification input is invalid.', {
      [field]: ['must be positive'],
    });
  return value;
}
function text(value: unknown, field: string): string | null {
  if (value === undefined || value === null) return null;
  if (typeof value !== 'string')
    throw new AuthValidationException('Verification input is invalid.', {
      [field]: ['must be a string'],
    });
  return value;
}
function date(value: unknown): string {
  if (typeof value !== 'string' || !/^\d{4}-\d{2}-\d{2}$/.test(value))
    throw new AuthValidationException('Verification input is invalid.', {
      date: ['must be an ISO date'],
    });
  return value;
}
function physical(value: unknown): PhysicalCondition {
  if (typeof value !== 'number' || ![1, 2, 3, 4, 5].includes(value))
    throw new AuthValidationException('Verification input is invalid.', {
      physicalCondition: ['unsupported value'],
    });
  return value as PhysicalCondition;
}
function equipmentStatus(value: unknown): EquipmentUnitStatus | null {
  if (value === undefined || value === null || value === '') return null;
  if (typeof value !== 'number' || ![0, 1, 2, 3, 4, 5, 6, 10, 99].includes(value))
    throw new AuthValidationException('Verification input is invalid.', {
      observedEquipmentStatus: ['unsupported value'],
    });
  return value as EquipmentUnitStatus;
}
function verificationStatus(value: unknown): VerificationStatus | undefined {
  if (value === undefined || value === null || value === '') return undefined;
  if (value !== 0 && value !== 1 && value !== 2 && value !== 3 && value !== 99)
    throw new AuthValidationException('Verification input is invalid.', {
      status: ['unsupported value'],
    });
  return value as VerificationStatus;
}
function faults(value: unknown): readonly string[] {
  if (value === undefined) return [];
  if (!Array.isArray(value) || value.some((item) => typeof item !== 'string'))
    throw new AuthValidationException('Verification input is invalid.', {
      faults: ['must be an array of strings'],
    });
  return value as string[];
}
function checkResults(value: unknown): readonly { checkItemId: number; result: 0 | 1 }[] {
  if (value === undefined) return [];
  if (!Array.isArray(value))
    throw new AuthValidationException('Verification input is invalid.', {
      checkResults: ['must be an array'],
    });
  return value.map((item) => {
    const candidate = object(item);
    const checkItemId = positive(candidate.checkItemId, 'checkItemId', true)!;
    if (candidate.result !== 0 && candidate.result !== 1)
      throw new AuthValidationException('Verification input is invalid.', {
        result: ['must be 0 or 1'],
      });
    return { checkItemId, result: candidate.result as 0 | 1 };
  });
}
function input(value: Record<string, unknown>): CreateVerificationInput {
  return {
    managementId: positive(value.managementId, 'managementId', true)!,
    equipmentUnitId: positive(value.equipmentUnitId, 'equipmentUnitId', true)!,
    date: date(value.date),
    observations: text(value.observations, 'observations'),
    physicalCondition: physical(value.physicalCondition),
    observedEquipmentStatus: equipmentStatus(value.observedEquipmentStatus),
    status: verificationStatus(value.status),
    checkResults: checkResults(value.checkResults),
    faults: faults(value.faults),
  };
}
function massInput(value: Record<string, unknown>): MassVerificationInput {
  const rowsValue = value.rows;
  if (!Array.isArray(rowsValue) || rowsValue.length === 0)
    throw new AuthValidationException('Verification input is invalid.', { rows: ['is required'] });
  const rows = rowsValue.map((row) => {
    const candidate = object(row);
    return {
      managementPlanId: positive(candidate.managementPlanId, 'managementPlanId', true)!,
      equipmentUnitId: positive(candidate.equipmentUnitId, 'equipmentUnitId', true)!,
      physicalCondition: physical(candidate.physicalCondition),
      observations: text(candidate.observations, 'observations'),
      faults: faults(candidate.faults),
    };
  });
  return {
    managementId: positive(value.managementId, 'managementId', true)!,
    date: date(value.date),
    saveDraft: value.saveDraft === true,
    rows,
  };
}
function query(value: Record<string, string | undefined>): VerificationQuery {
  const int = (key: string) => {
    const raw = value[key];
    if (raw === undefined || raw === '') return undefined;
    if (!/^\d+$/.test(raw)) throw new AuthValidationException('Verification query is invalid.');
    return Number(raw);
  };
  const currentPage = int('currentPage');
  const managementId = int('managementId');
  const laboratoryId = int('laboratoryId');
  const status = int('statusFilter');
  if (status !== undefined && ![0, 1, 2, 3, 99].includes(status))
    throw new AuthValidationException('Verification query is invalid.');
  return {
    currentPage,
    managementId,
    laboratoryId,
    statusFilter: status as VerificationStatus | undefined,
    searchTerm: value.searchTerm?.trim().slice(0, 200) || undefined,
  };
}

@Controller()
@UseFilters(CoreExceptionFilter)
@UseGuards(ApiCorrelationGuard, AuthSecurityGuard, SiteContextGuard)
@UseInterceptors(AuthHeadersInterceptor)
export class VerificationController {
  constructor(private readonly service: VerificationService) {}
  @Get('verifications') @SkipCsrf() async list(
    @Req() request: FastifyRequest,
    @Query() value: Record<string, string | undefined>,
  ): Promise<ApiSuccess<VerificationPage>> {
    return { success: true, data: await this.service.list(context(request), query(value)) };
  }
  @Get('verifications/check-items') @SkipCsrf() async checkItems(
    @Req() request: FastifyRequest,
  ): Promise<ApiSuccess<readonly VerificationCheckItem[]>> {
    return { success: true, data: await this.service.checkItems(context(request)) };
  }
  @Get('verifications/:id') @SkipCsrf() async find(
    @Req() request: FastifyRequest,
    @Param('id', ParseIntPipe) id: number,
  ): Promise<ApiSuccess<VerificationDetail>> {
    return { success: true, data: await this.service.find(context(request), id) };
  }
  @Post('verifications') async save(
    @Req() request: FastifyRequest,
    @Body() raw: unknown,
  ): Promise<ApiSuccess<VerificationDetail>> {
    return { success: true, data: await this.service.save(context(request), input(object(raw))) };
  }
  @Post('verifications/mass') async mass(
    @Req() request: FastifyRequest,
    @Body() raw: unknown,
  ): Promise<ApiSuccess<readonly VerificationDetail[]>> {
    return {
      success: true,
      data: await this.service.mass(context(request), massInput(object(raw))),
    };
  }
}
