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
  AcademicQuery,
  CatalogPage,
  CatalogStatus,
  Career,
  CreateCareerInput,
  CreateFacultyInput,
  CreateLaboratoryInput,
  Faculty,
  Laboratory,
  SiteCareer,
  UpdateCareerInput,
  UpdateFacultyInput,
  UpdateLaboratoryInput,
} from '@lu/contracts';
import { AuthSecurityGuard, SkipCsrf } from '../auth/auth.guard.js';
import { AuthHeadersInterceptor } from '../auth/auth.interceptor.js';
import { AuthValidationException } from '../auth/auth.exceptions.js';
import type { FastifyRequest } from '../auth/auth.fastify.js';
import { CoreExceptionFilter } from '../core/core.filter.js';
import { ApiCorrelationGuard, SiteContextGuard } from '../core/core.guard.js';
import { AcademicService } from './academic.service.js';

function context(request: FastifyRequest) {
  if (request.siteContext === undefined) throw new Error('Site context was not resolved.');
  return request.siteContext;
}
function integer(
  value: string | undefined,
  field: string,
  min = 1,
  max = 2_147_483_647,
): number | undefined {
  if (value === undefined || value.trim() === '') return undefined;
  if (!/^\d+$/.test(value))
    throw new AuthValidationException('Academic query is invalid.', {
      [field]: ['must be a positive integer'],
    });
  const parsed = Number(value);
  if (!Number.isSafeInteger(parsed) || parsed < min || parsed > max)
    throw new AuthValidationException('Academic query is invalid.', {
      [field]: [`must be between ${min} and ${max}`],
    });
  return parsed;
}
function status(value: unknown, field = 'status'): CatalogStatus {
  if (value !== 0 && value !== 1 && value !== 2)
    throw new AuthValidationException('Academic input is invalid.', {
      [field]: ['must be 0, 1 or 2'],
    });
  return value;
}
function body(value: unknown): Record<string, unknown> {
  if (typeof value !== 'object' || value === null || Array.isArray(value))
    throw new AuthValidationException('Academic input is invalid.');
  return value as Record<string, unknown>;
}
function text(value: unknown, field: string, required = false): string | null | undefined {
  if (value === undefined || value === null) {
    if (required)
      throw new AuthValidationException('Academic input is invalid.', { [field]: ['is required'] });
    return value as null | undefined;
  }
  if (typeof value !== 'string' || (required && value.trim() === ''))
    throw new AuthValidationException('Academic input is invalid.', {
      [field]: ['must be a non-empty string'],
    });
  return value;
}
function id(value: unknown, field: string): number {
  if (typeof value !== 'number' || !Number.isSafeInteger(value) || value < 1)
    throw new AuthValidationException('Academic input is invalid.', {
      [field]: ['must be a positive integer'],
    });
  return value;
}
function query(value: Record<string, string | undefined>): AcademicQuery {
  const currentPage = integer(value.currentPage, 'currentPage');
  const statusFilter = integer(value.statusFilter, 'statusFilter', 0, 2);
  return {
    currentPage,
    statusFilter: statusFilter === undefined ? undefined : status(statusFilter, 'statusFilter'),
    searchTerm: value.searchTerm?.trim().slice(0, 200) || undefined,
  };
}

@Controller()
@UseFilters(CoreExceptionFilter)
@UseGuards(ApiCorrelationGuard, AuthSecurityGuard, SiteContextGuard)
@UseInterceptors(AuthHeadersInterceptor)
export class AcademicController {
  constructor(private readonly service: AcademicService) {}
  @Get('faculties') @SkipCsrf() async faculties(
    @Req() request: FastifyRequest,
    @Query() value: Record<string, string | undefined>,
  ): Promise<ApiSuccess<CatalogPage<Faculty>>> {
    return {
      success: true,
      data: await this.service.listFaculties(context(request), query(value)),
    };
  }
  @Get('faculties/:id') @SkipCsrf() async faculty(
    @Req() request: FastifyRequest,
    @Param('id', ParseIntPipe) idValue: number,
  ): Promise<ApiSuccess<Faculty>> {
    return { success: true, data: await this.service.findFaculty(context(request), idValue) };
  }
  @Post('faculties') async createFaculty(
    @Req() request: FastifyRequest,
    @Body() raw: unknown,
  ): Promise<ApiSuccess<Faculty>> {
    const value = body(raw);
    const input: CreateFacultyInput = {
      name: text(value.name, 'name', true)!,
      code: text(value.code, 'code'),
      description: text(value.description, 'description'),
    };
    return { success: true, data: await this.service.createFaculty(context(request), input) };
  }
  @Put('faculties/:id') async updateFaculty(
    @Req() request: FastifyRequest,
    @Param('id', ParseIntPipe) idValue: number,
    @Body() raw: unknown,
  ): Promise<ApiSuccess<Faculty>> {
    const value = body(raw);
    const input: UpdateFacultyInput = {
      name: text(value.name, 'name', true)!,
      code: text(value.code, 'code'),
      description: text(value.description, 'description'),
      status: status(value.status),
    };
    return {
      success: true,
      data: await this.service.updateFaculty(context(request), idValue, input),
    };
  }
  @Delete('faculties/:id') async deleteFaculty(
    @Req() request: FastifyRequest,
    @Param('id', ParseIntPipe) idValue: number,
  ): Promise<ApiSuccess<null>> {
    await this.service.deleteFaculty(context(request), idValue);
    return { success: true, data: null };
  }
  @Get('careers') @SkipCsrf() async careers(
    @Req() request: FastifyRequest,
    @Query() value: Record<string, string | undefined>,
  ): Promise<ApiSuccess<CatalogPage<Career>>> {
    return { success: true, data: await this.service.listCareers(context(request), query(value)) };
  }
  @Get('careers/:id') @SkipCsrf() async career(
    @Req() request: FastifyRequest,
    @Param('id', ParseIntPipe) idValue: number,
  ): Promise<ApiSuccess<Career>> {
    return { success: true, data: await this.service.findCareer(context(request), idValue) };
  }
  @Post('careers') async createCareer(
    @Req() request: FastifyRequest,
    @Body() raw: unknown,
  ): Promise<ApiSuccess<Career>> {
    const value = body(raw);
    const input: CreateCareerInput = {
      name: text(value.name, 'name', true)!,
      code: text(value.code, 'code'),
      facultyId:
        value.facultyId === null || value.facultyId === undefined
          ? null
          : id(value.facultyId, 'facultyId'),
      status: value.status === undefined ? undefined : status(value.status),
    };
    return { success: true, data: await this.service.createCareer(context(request), input) };
  }
  @Put('careers/:id') async updateCareer(
    @Req() request: FastifyRequest,
    @Param('id', ParseIntPipe) idValue: number,
    @Body() raw: unknown,
  ): Promise<ApiSuccess<Career>> {
    const value = body(raw);
    const input: UpdateCareerInput = {
      name: text(value.name, 'name', true)!,
      code: text(value.code, 'code'),
      facultyId:
        value.facultyId === null || value.facultyId === undefined
          ? null
          : id(value.facultyId, 'facultyId'),
      status: status(value.status),
    };
    return {
      success: true,
      data: await this.service.updateCareer(context(request), idValue, input),
    };
  }
  @Delete('careers/:id') async deleteCareer(
    @Req() request: FastifyRequest,
    @Param('id', ParseIntPipe) idValue: number,
  ): Promise<ApiSuccess<null>> {
    await this.service.deleteCareer(context(request), idValue);
    return { success: true, data: null };
  }
  @Get('site-careers') @SkipCsrf() async siteCareers(
    @Req() request: FastifyRequest,
  ): Promise<ApiSuccess<readonly SiteCareer[]>> {
    return { success: true, data: await this.service.listSiteCareers(context(request)) };
  }
  @Post('site-careers/:careerId') async assignSiteCareer(
    @Req() request: FastifyRequest,
    @Param('careerId', ParseIntPipe) careerId: number,
  ): Promise<ApiSuccess<null>> {
    await this.service.assignSiteCareer(context(request), careerId);
    return { success: true, data: null };
  }
  @Delete('site-careers/:careerId') async unassignSiteCareer(
    @Req() request: FastifyRequest,
    @Param('careerId', ParseIntPipe) careerId: number,
  ): Promise<ApiSuccess<null>> {
    await this.service.unassignSiteCareer(context(request), careerId);
    return { success: true, data: null };
  }
  @Get('laboratories') @SkipCsrf() async laboratories(
    @Req() request: FastifyRequest,
    @Query() value: Record<string, string | undefined>,
  ): Promise<ApiSuccess<CatalogPage<Laboratory>>> {
    return {
      success: true,
      data: await this.service.listLaboratories(context(request), query(value)),
    };
  }
  @Get('laboratories/:id') @SkipCsrf() async laboratory(
    @Req() request: FastifyRequest,
    @Param('id', ParseIntPipe) idValue: number,
  ): Promise<ApiSuccess<Laboratory>> {
    return { success: true, data: await this.service.findLaboratory(context(request), idValue) };
  }
  @Post('laboratories') async createLaboratory(
    @Req() request: FastifyRequest,
    @Body() raw: unknown,
  ): Promise<ApiSuccess<Laboratory>> {
    const value = body(raw);
    const input: CreateLaboratoryInput = {
      facultyId: id(value.facultyId, 'facultyId'),
      code: text(value.code, 'code', true)!,
      name: text(value.name, 'name', true)!,
      type: text(value.type, 'type'),
      building: text(value.building, 'building'),
      block: text(value.block, 'block'),
      floor: text(value.floor, 'floor'),
      room: text(value.room, 'room'),
      description: text(value.description, 'description'),
      cityId:
        value.cityId === null || value.cityId === undefined ? null : id(value.cityId, 'cityId'),
    };
    return { success: true, data: await this.service.createLaboratory(context(request), input) };
  }
  @Put('laboratories/:id') async updateLaboratory(
    @Req() request: FastifyRequest,
    @Param('id', ParseIntPipe) idValue: number,
    @Body() raw: unknown,
  ): Promise<ApiSuccess<Laboratory>> {
    const value = body(raw);
    const input: UpdateLaboratoryInput = {
      facultyId: id(value.facultyId, 'facultyId'),
      code: text(value.code, 'code', true)!,
      name: text(value.name, 'name', true)!,
      type: text(value.type, 'type'),
      building: text(value.building, 'building'),
      block: text(value.block, 'block'),
      floor: text(value.floor, 'floor'),
      room: text(value.room, 'room'),
      description: text(value.description, 'description'),
      cityId:
        value.cityId === null || value.cityId === undefined ? null : id(value.cityId, 'cityId'),
      status: status(value.status),
    };
    return {
      success: true,
      data: await this.service.updateLaboratory(context(request), idValue, input),
    };
  }
  @Delete('laboratories/:id') async deleteLaboratory(
    @Req() request: FastifyRequest,
    @Param('id', ParseIntPipe) idValue: number,
  ): Promise<ApiSuccess<null>> {
    await this.service.deleteLaboratory(context(request), idValue);
    return { success: true, data: null };
  }
}
