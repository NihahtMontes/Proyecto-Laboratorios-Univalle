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
  CreateManagementInput,
  ManagementPage,
  ManagementPlanPage,
  ManagementPlanQuery,
  ManagementRecord,
  ManagementQuery,
  ManagementStatus,
  ManagementType,
  SyncManagementPlanInput,
  UpdateManagementInput,
} from '@lu/contracts';
import { AuthSecurityGuard, SkipCsrf } from '../auth/auth.guard.js';
import { AuthHeadersInterceptor } from '../auth/auth.interceptor.js';
import { AuthValidationException } from '../auth/auth.exceptions.js';
import type { FastifyRequest } from '../auth/auth.fastify.js';
import { ApiCorrelationGuard, SiteContextGuard } from '../core/core.guard.js';
import { CoreExceptionFilter } from '../core/core.filter.js';
import { ManagementService } from './management.service.js';

function context(request: FastifyRequest) {
  if (request.siteContext === undefined) throw new Error('Site context was not resolved.');
  return request.siteContext;
}
function object(value: unknown): Record<string, unknown> {
  if (typeof value !== 'object' || value === null || Array.isArray(value))
    throw new AuthValidationException('Management input is invalid.');
  return value as Record<string, unknown>;
}
function integer(value: unknown, field: string, required = false): number | null {
  if (value === undefined || value === null || value === '') {
    if (required)
      throw new AuthValidationException('Management input is invalid.', {
        [field]: ['is required'],
      });
    return null;
  }
  if (typeof value !== 'number' || !Number.isSafeInteger(value))
    throw new AuthValidationException('Management input is invalid.', {
      [field]: ['must be an integer'],
    });
  return value;
}
function text(value: unknown, field: string): string | null {
  if (value === undefined || value === null) return null;
  if (typeof value !== 'string')
    throw new AuthValidationException('Management input is invalid.', {
      [field]: ['must be a string'],
    });
  return value;
}
function date(value: unknown, field: string): string | null {
  const result = text(value, field);
  if (result === null || result === '') return null;
  if (!/^\d{4}-\d{2}-\d{2}$/.test(result))
    throw new AuthValidationException('Management input is invalid.', {
      [field]: ['must be an ISO date'],
    });
  return result;
}
function type(value: unknown): ManagementType {
  if (value !== 0 && value !== 1)
    throw new AuthValidationException('Management input is invalid.', { type: ['must be 0 or 1'] });
  return value;
}
function status(value: unknown, required = false): ManagementStatus {
  if (value === undefined || value === null || value === '') {
    if (required)
      throw new AuthValidationException('Management input is invalid.', {
        status: ['is required'],
      });
    return 0;
  }
  if (value !== 0 && value !== 1 && value !== 2)
    throw new AuthValidationException('Management input is invalid.', {
      status: ['unsupported value'],
    });
  return value;
}
function input(
  value: Record<string, unknown>,
  update: boolean,
): CreateManagementInput | UpdateManagementInput {
  const base: CreateManagementInput = {
    year: integer(value.year, 'year', true)!,
    semester: integer(value.semester, 'semester', true)!,
    description: text(value.description, 'description'),
    startDate: date(value.startDate, 'startDate'),
    plannedEndDate: date(value.plannedEndDate, 'plannedEndDate'),
    status: status(value.status),
    type: type(value.type),
    facultyId: integer(value.facultyId, 'facultyId'),
  };
  return update ? { ...base, status: status(value.status, true) } : base;
}
function query(value: Record<string, string | undefined>): ManagementQuery {
  const int = (key: string) => {
    const raw = value[key];
    if (raw === undefined || raw === '') return undefined;
    if (!/^\d+$/.test(raw)) throw new AuthValidationException('Management query is invalid.');
    return Number(raw);
  };
  const rawType = int('type');
  const rawStatus = int('statusFilter');
  if (rawType !== undefined && rawType !== 0 && rawType !== 1)
    throw new AuthValidationException('Management query is invalid.');
  if (rawStatus !== undefined && ![0, 1, 2, 99].includes(rawStatus))
    throw new AuthValidationException('Management query is invalid.');
  return {
    currentPage: int('currentPage'),
    type: rawType as ManagementType | undefined,
    statusFilter: rawStatus as ManagementStatus | undefined,
    searchTerm: value.searchTerm?.trim().slice(0, 200) || undefined,
  };
}
function planQuery(value: Record<string, string | undefined>): ManagementPlanQuery {
  const rawPage = value.currentPage;
  const rawLab = value.laboratoryId;
  const parse = (raw: string | undefined) =>
    raw === undefined || raw === ''
      ? undefined
      : /^\d+$/.test(raw)
        ? Number(raw)
        : (() => {
            throw new AuthValidationException('Plan query is invalid.');
          })();
  return {
    currentPage: parse(rawPage),
    laboratoryId: parse(rawLab),
    searchTerm: value.searchTerm?.trim().slice(0, 200) || undefined,
    onlyAvailable: value.onlyAvailable === 'true',
  };
}
function syncInput(value: Record<string, unknown>): SyncManagementPlanInput {
  const ids = (key: string): readonly number[] => {
    const raw = value[key];
    if (
      !Array.isArray(raw) ||
      raw.some((id) => typeof id !== 'number' || !Number.isSafeInteger(id) || id < 1)
    )
      throw new AuthValidationException('Plan selection is invalid.', {
        [key]: ['must contain positive integer identifiers'],
      });
    return raw as number[];
  };
  return { addUnitIds: ids('addUnitIds'), removeUnitIds: ids('removeUnitIds') };
}

@Controller()
@UseFilters(CoreExceptionFilter)
@UseGuards(ApiCorrelationGuard, AuthSecurityGuard, SiteContextGuard)
@UseInterceptors(AuthHeadersInterceptor)
export class ManagementController {
  constructor(private readonly service: ManagementService) {}
  @Get('managements') @SkipCsrf() async list(
    @Req() request: FastifyRequest,
    @Query() value: Record<string, string | undefined>,
  ): Promise<ApiSuccess<ManagementPage>> {
    return { success: true, data: await this.service.list(context(request), query(value)) };
  }
  @Get('managements/:id') @SkipCsrf() async find(
    @Req() request: FastifyRequest,
    @Param('id', ParseIntPipe) id: number,
  ): Promise<ApiSuccess<ManagementRecord>> {
    return { success: true, data: await this.service.find(context(request), id) };
  }
  @Get('managements/:id/plans') @SkipCsrf() async plans(
    @Req() request: FastifyRequest,
    @Param('id', ParseIntPipe) id: number,
    @Query() value: Record<string, string | undefined>,
  ): Promise<ApiSuccess<ManagementPlanPage>> {
    return {
      success: true,
      data: await this.service.plans(context(request), id, planQuery(value)),
    };
  }
  @Post('managements') async create(
    @Req() request: FastifyRequest,
    @Body() raw: unknown,
  ): Promise<ApiSuccess<ManagementRecord>> {
    return {
      success: true,
      data: await this.service.create(context(request), input(object(raw), false)),
    };
  }
  @Put('managements/:id') async update(
    @Req() request: FastifyRequest,
    @Param('id', ParseIntPipe) id: number,
    @Body() raw: unknown,
  ): Promise<ApiSuccess<ManagementRecord>> {
    return {
      success: true,
      data: await this.service.update(
        context(request),
        id,
        input(object(raw), true) as UpdateManagementInput,
      ),
    };
  }
  @Post('managements/:id/activate') async activate(
    @Req() request: FastifyRequest,
    @Param('id', ParseIntPipe) id: number,
  ): Promise<ApiSuccess<ManagementRecord>> {
    return { success: true, data: await this.service.activate(context(request), id) };
  }
  @Post('managements/:id/close') async close(
    @Req() request: FastifyRequest,
    @Param('id', ParseIntPipe) id: number,
  ): Promise<ApiSuccess<ManagementRecord>> {
    return { success: true, data: await this.service.close(context(request), id) };
  }
  @Delete('managements/:id') async remove(
    @Req() request: FastifyRequest,
    @Param('id', ParseIntPipe) id: number,
  ): Promise<ApiSuccess<null>> {
    await this.service.remove(context(request), id);
    return { success: true, data: null };
  }
  @Post('managements/:id/plans/sync') async syncPlans(
    @Req() request: FastifyRequest,
    @Param('id', ParseIntPipe) id: number,
    @Body() raw: unknown,
  ): Promise<ApiSuccess<null>> {
    await this.service.syncPlans(context(request), id, syncInput(object(raw)));
    return { success: true, data: null };
  }
}
