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
  CreateEquipmentInput,
  EquipmentPage,
  EquipmentQuery,
  EquipmentRecord,
  UpdateEquipmentInput,
} from '@lu/contracts';
import { AuthSecurityGuard, SkipCsrf } from '../auth/auth.guard.js';
import { AuthHeadersInterceptor } from '../auth/auth.interceptor.js';
import { AuthValidationException } from '../auth/auth.exceptions.js';
import type { FastifyRequest } from '../auth/auth.fastify.js';
import { CoreExceptionFilter } from '../core/core.filter.js';
import { ApiCorrelationGuard, SiteContextGuard } from '../core/core.guard.js';
import { EquipmentService } from './equipment.service.js';

function context(request: FastifyRequest) {
  if (request.siteContext === undefined) throw new Error('Site context was not resolved.');
  return request.siteContext;
}
function object(value: unknown): Record<string, unknown> {
  if (typeof value !== 'object' || value === null || Array.isArray(value))
    throw new AuthValidationException('Equipment input is invalid.');
  return value as Record<string, unknown>;
}
function text(value: unknown, field: string, required = false): string | null | undefined {
  if (value === undefined || value === null) {
    if (required)
      throw new AuthValidationException('Equipment input is invalid.', {
        [field]: ['is required'],
      });
    return value as null | undefined;
  }
  if (typeof value !== 'string' || (required && value.trim() === ''))
    throw new AuthValidationException('Equipment input is invalid.', {
      [field]: ['must be a non-empty string'],
    });
  return value;
}
function id(value: unknown, field: string): number | null {
  if (value === undefined || value === null) return null;
  if (typeof value !== 'number' || !Number.isSafeInteger(value) || value < 1)
    throw new AuthValidationException('Equipment input is invalid.', {
      [field]: ['must be a positive integer or null'],
    });
  return value;
}
function numberValue(value: unknown, field: string, required = false): number | null {
  if (value === undefined || value === null) {
    if (required)
      throw new AuthValidationException('Equipment input is invalid.', {
        [field]: ['is required'],
      });
    return null;
  }
  if (typeof value !== 'number' || !Number.isSafeInteger(value))
    throw new AuthValidationException('Equipment input is invalid.', {
      [field]: ['must be an integer'],
    });
  return value;
}
function category(value: unknown): 0 | 1 | 2 {
  if (value !== 0 && value !== 1 && value !== 2)
    throw new AuthValidationException('Equipment input is invalid.', {
      category: ['must be 0, 1 or 2'],
    });
  return value;
}
function status(value: unknown): 0 | 1 | 2 {
  if (value !== 0 && value !== 1 && value !== 2)
    throw new AuthValidationException('Equipment input is invalid.', {
      status: ['must be 0, 1 or 2'],
    });
  return value;
}
function notes(value: unknown): readonly string[] {
  if (value === undefined) return [];
  if (!Array.isArray(value) || value.some((item) => typeof item !== 'string'))
    throw new AuthValidationException('Equipment input is invalid.', {
      notes: ['must be an array of strings'],
    });
  return value as string[];
}
function query(value: Record<string, string | undefined>): EquipmentQuery {
  const int = (key: string) => {
    const raw = value[key];
    if (raw === undefined || raw === '') return undefined;
    if (!/^\d+$/.test(raw))
      throw new AuthValidationException('Equipment query is invalid.', {
        [key]: ['must be an integer'],
      });
    return Number(raw);
  };
  const currentPage = int('currentPage');
  const categoryValue = int('category');
  const statusFilter = int('statusFilter');
  return {
    currentPage,
    searchTerm: value.searchTerm?.trim().slice(0, 200) || undefined,
    category: categoryValue === undefined ? undefined : category(categoryValue),
    typeClassification: int('typeClassification'),
    utensilType: int('utensilType'),
    reviewStatus: int('reviewStatus'),
    statusFilter: statusFilter === undefined ? undefined : status(statusFilter),
  };
}

@Controller()
@UseFilters(CoreExceptionFilter)
@UseGuards(ApiCorrelationGuard, AuthSecurityGuard, SiteContextGuard)
@UseInterceptors(AuthHeadersInterceptor)
export class EquipmentController {
  constructor(private readonly service: EquipmentService) {}
  @Get('equipment') @SkipCsrf() async list(
    @Req() request: FastifyRequest,
    @Query() value: Record<string, string | undefined>,
  ): Promise<ApiSuccess<EquipmentPage>> {
    return { success: true, data: await this.service.list(context(request), query(value)) };
  }
  @Get('equipment/:id') @SkipCsrf() async find(
    @Req() request: FastifyRequest,
    @Param('id', ParseIntPipe) idValue: number,
  ): Promise<ApiSuccess<EquipmentRecord>> {
    return { success: true, data: await this.service.find(context(request), idValue) };
  }
  @Post('equipment') async create(
    @Req() request: FastifyRequest,
    @Body() raw: unknown,
  ): Promise<ApiSuccess<EquipmentRecord>> {
    const value = object(raw);
    const input: CreateEquipmentInput = {
      category: category(value.category),
      utensilType: numberValue(value.utensilType, 'utensilType'),
      typeClassification: numberValue(value.typeClassification, 'typeClassification'),
      otherClassificationDetail: text(value.otherClassificationDetail, 'otherClassificationDetail'),
      catalogCode: text(value.catalogCode, 'catalogCode', true)!,
      countryId: id(value.countryId, 'countryId'),
      cityId: id(value.cityId, 'cityId'),
      name: text(value.name, 'name', true)!,
      brand: text(value.brand, 'brand'),
      model: text(value.model, 'model'),
      usefulLifeYears: numberValue(value.usefulLifeYears, 'usefulLifeYears'),
      description: text(value.description, 'description'),
      notes: notes(value.notes),
    };
    return { success: true, data: await this.service.create(context(request), input) };
  }
  @Put('equipment/:id') async update(
    @Req() request: FastifyRequest,
    @Param('id', ParseIntPipe) idValue: number,
    @Body() raw: unknown,
  ): Promise<ApiSuccess<EquipmentRecord>> {
    const value = object(raw);
    const input: UpdateEquipmentInput = {
      category: category(value.category),
      utensilType: numberValue(value.utensilType, 'utensilType'),
      typeClassification: numberValue(value.typeClassification, 'typeClassification'),
      otherClassificationDetail: text(value.otherClassificationDetail, 'otherClassificationDetail'),
      catalogCode: text(value.catalogCode, 'catalogCode', true)!,
      countryId: id(value.countryId, 'countryId'),
      cityId: id(value.cityId, 'cityId'),
      name: text(value.name, 'name', true)!,
      brand: text(value.brand, 'brand'),
      model: text(value.model, 'model'),
      usefulLifeYears: numberValue(value.usefulLifeYears, 'usefulLifeYears'),
      description: text(value.description, 'description'),
      notes: notes(value.notes),
      status: status(value.status),
    };
    return { success: true, data: await this.service.update(context(request), idValue, input) };
  }
  @Delete('equipment/:id') async remove(
    @Req() request: FastifyRequest,
    @Param('id', ParseIntPipe) idValue: number,
  ): Promise<ApiSuccess<null>> {
    await this.service.remove(context(request), idValue);
    return { success: true, data: null };
  }
}
