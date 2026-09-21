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
  CatalogCollectionQuery,
  CatalogPage,
  CatalogStatus,
  City,
  Country,
  CreateCityInput,
  CreateCountryInput,
  UpdateCityInput,
  UpdateCountryInput,
} from '@lu/contracts';
import { AuthSecurityGuard, SkipCsrf } from '../auth/auth.guard.js';
import { AuthHeadersInterceptor } from '../auth/auth.interceptor.js';
import { AuthValidationException } from '../auth/auth.exceptions.js';
import type { FastifyRequest } from '../auth/auth.fastify.js';
import { CoreExceptionFilter } from '../core/core.filter.js';
import { ApiCorrelationGuard, SiteContextGuard } from '../core/core.guard.js';
import { CatalogService } from './catalog.service.js';

function contextOf(request: FastifyRequest) {
  if (request.siteContext === undefined) throw new Error('Site context was not resolved.');
  return request.siteContext;
}

function integer(value: string | undefined, field: string, min = 1, max = 2_147_483_647) {
  if (value === undefined || value.trim() === '') return undefined;
  if (!/^\d+$/.test(value)) {
    throw new AuthValidationException('Catalog query is invalid.', {
      [field]: ['must be a positive integer'],
    });
  }
  const parsed = Number(value);
  if (!Number.isSafeInteger(parsed) || parsed < min || parsed > max) {
    throw new AuthValidationException('Catalog query is invalid.', {
      [field]: [`must be between ${min} and ${max}`],
    });
  }
  return parsed;
}

function statusValue(value: unknown, field = 'status'): CatalogStatus {
  if (value !== 0 && value !== 1 && value !== 2) {
    throw new AuthValidationException('Catalog input is invalid.', {
      [field]: ['must be 0, 1 or 2'],
    });
  }
  return value;
}

function objectBody(value: unknown): Record<string, unknown> {
  if (typeof value !== 'object' || value === null || Array.isArray(value)) {
    throw new AuthValidationException('Catalog input is invalid.');
  }
  return value as Record<string, unknown>;
}

function requiredName(body: Record<string, unknown>): string {
  if (typeof body.name !== 'string' || body.name.trim() === '') {
    throw new AuthValidationException('Catalog input is invalid.', {
      name: ['is required'],
    });
  }
  return body.name;
}

function requiredId(body: Record<string, unknown>, field: string): number {
  if (typeof body[field] !== 'number' || !Number.isSafeInteger(body[field]) || body[field] < 1) {
    throw new AuthValidationException('Catalog input is invalid.', {
      [field]: ['must be a positive integer'],
    });
  }
  return body[field];
}

function optionalRegion(body: Record<string, unknown>): string | null | undefined {
  if (body.region === undefined || body.region === null) return body.region as null | undefined;
  if (typeof body.region !== 'string') {
    throw new AuthValidationException('Catalog input is invalid.', {
      region: ['must be a string or null'],
    });
  }
  return body.region;
}

function collectionQuery(query: Record<string, string | undefined>): CatalogCollectionQuery {
  const currentPage = integer(query.currentPage, 'currentPage');
  const parsedStatus = integer(query.statusFilter, 'statusFilter', 0, 2);
  return {
    currentPage,
    statusFilter:
      parsedStatus === undefined ? undefined : statusValue(parsedStatus, 'statusFilter'),
    searchTerm:
      query.searchTerm?.trim() === '' ? undefined : query.searchTerm?.trim().slice(0, 200),
  };
}

@Controller()
@UseFilters(CoreExceptionFilter)
@UseGuards(ApiCorrelationGuard, AuthSecurityGuard, SiteContextGuard)
@UseInterceptors(AuthHeadersInterceptor)
export class CatalogController {
  constructor(private readonly service: CatalogService) {}

  @Get('countries')
  @SkipCsrf()
  async countries(
    @Req() request: FastifyRequest,
    @Query() query: Record<string, string | undefined>,
  ): Promise<ApiSuccess<CatalogPage<Country>>> {
    return {
      success: true,
      data: await this.service.listCountries(contextOf(request), collectionQuery(query)),
    };
  }

  @Get('countries/:id')
  @SkipCsrf()
  async country(
    @Req() request: FastifyRequest,
    @Param('id', ParseIntPipe) id: number,
  ): Promise<ApiSuccess<Country>> {
    return { success: true, data: await this.service.findCountry(contextOf(request), id) };
  }

  @Post('countries')
  async createCountry(
    @Req() request: FastifyRequest,
    @Body() body: unknown,
  ): Promise<ApiSuccess<Country>> {
    const payload: CreateCountryInput = { name: requiredName(objectBody(body)) };
    return { success: true, data: await this.service.createCountry(contextOf(request), payload) };
  }

  @Put('countries/:id')
  async updateCountry(
    @Req() request: FastifyRequest,
    @Param('id', ParseIntPipe) id: number,
    @Body() body: unknown,
  ): Promise<ApiSuccess<Country>> {
    const value = objectBody(body);
    const payload: UpdateCountryInput = {
      name: requiredName(value),
      status: statusValue(value.status),
    };
    return {
      success: true,
      data: await this.service.updateCountry(contextOf(request), id, payload),
    };
  }

  @Delete('countries/:id')
  async deleteCountry(
    @Req() request: FastifyRequest,
    @Param('id', ParseIntPipe) id: number,
  ): Promise<ApiSuccess<null>> {
    await this.service.deleteCountry(contextOf(request), id);
    return { success: true, data: null };
  }

  @Get('cities')
  @SkipCsrf()
  async cities(
    @Req() request: FastifyRequest,
    @Query() query: Record<string, string | undefined>,
  ): Promise<ApiSuccess<CatalogPage<City>>> {
    return {
      success: true,
      data: await this.service.listCities(contextOf(request), collectionQuery(query)),
    };
  }

  @Get('cities/:id')
  @SkipCsrf()
  async city(
    @Req() request: FastifyRequest,
    @Param('id', ParseIntPipe) id: number,
  ): Promise<ApiSuccess<City>> {
    return { success: true, data: await this.service.findCity(contextOf(request), id) };
  }

  @Post('cities')
  async createCity(
    @Req() request: FastifyRequest,
    @Body() body: unknown,
  ): Promise<ApiSuccess<City>> {
    const value = objectBody(body);
    const payload: CreateCityInput = {
      countryId: requiredId(value, 'countryId'),
      name: requiredName(value),
      region: optionalRegion(value),
    };
    return { success: true, data: await this.service.createCity(contextOf(request), payload) };
  }

  @Put('cities/:id')
  async updateCity(
    @Req() request: FastifyRequest,
    @Param('id', ParseIntPipe) id: number,
    @Body() body: unknown,
  ): Promise<ApiSuccess<City>> {
    const value = objectBody(body);
    const payload: UpdateCityInput = {
      countryId: requiredId(value, 'countryId'),
      name: requiredName(value),
      region: optionalRegion(value),
      status: statusValue(value.status),
    };
    return { success: true, data: await this.service.updateCity(contextOf(request), id, payload) };
  }

  @Delete('cities/:id')
  async deleteCity(
    @Req() request: FastifyRequest,
    @Param('id', ParseIntPipe) id: number,
  ): Promise<ApiSuccess<null>> {
    await this.service.deleteCity(contextOf(request), id);
    return { success: true, data: null };
  }
}
