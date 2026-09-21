import { Injectable, NotFoundException } from '@nestjs/common';
import type {
  CatalogCollectionQuery,
  CatalogPage,
  City,
  Country,
  CreateCityInput,
  CreateCountryInput,
  SiteRequestContext,
  UpdateCityInput,
  UpdateCountryInput,
} from '@lu/contracts';
import { SiteRole } from '@lu/contracts';
import { AuthForbiddenException, AuthValidationException } from '../auth/auth.exceptions.js';
import { TenantPgPoolRegistry } from '../core/tenant-pg-pool.js';
import { TenantRouter } from '../core/tenant-routing.js';
import {
  CatalogConflictError,
  CatalogMissingError,
  CatalogRepository,
} from './catalog.repository.js';

function assertAdministrator(context: SiteRequestContext): void {
  if (context.siteRole !== SiteRole.Administrador) {
    throw new AuthForbiddenException('SITE_ACCESS_DENIED');
  }
}

function translate(error: unknown): never {
  if (error instanceof CatalogMissingError) throw new NotFoundException();
  if (error instanceof CatalogConflictError) {
    throw new AuthValidationException(
      error.message,
      error.field ? { [error.field]: [error.message] } : undefined,
    );
  }
  throw error;
}

@Injectable()
export class CatalogService {
  constructor(
    private readonly tenantRouter: TenantRouter,
    private readonly tenantPools: TenantPgPoolRegistry,
    private readonly repository: CatalogRepository,
  ) {}

  private async pool(context: SiteRequestContext) {
    assertAdministrator(context);
    const route = await this.tenantRouter.resolve(context);
    return this.tenantPools.get(route);
  }

  async listCountries(
    context: SiteRequestContext,
    query: CatalogCollectionQuery,
  ): Promise<CatalogPage<Country>> {
    try {
      return await this.repository.listCountries(await this.pool(context), context.siteId, query);
    } catch (error) {
      return translate(error);
    }
  }

  async findCountry(context: SiteRequestContext, id: number): Promise<Country> {
    const country = await this.repository.findCountry(await this.pool(context), context.siteId, id);
    if (country === null) throw new NotFoundException();
    return country;
  }

  async createCountry(context: SiteRequestContext, input: CreateCountryInput): Promise<Country> {
    try {
      return await this.repository.createCountry(
        await this.pool(context),
        context.siteId,
        context.userId,
        input,
      );
    } catch (error) {
      return translate(error);
    }
  }

  async updateCountry(
    context: SiteRequestContext,
    id: number,
    input: UpdateCountryInput,
  ): Promise<Country> {
    try {
      return await this.repository.updateCountry(
        await this.pool(context),
        context.siteId,
        context.userId,
        id,
        input,
      );
    } catch (error) {
      return translate(error);
    }
  }

  async deleteCountry(context: SiteRequestContext, id: number): Promise<void> {
    try {
      await this.repository.deleteCountry(
        await this.pool(context),
        context.siteId,
        context.userId,
        id,
      );
    } catch (error) {
      return translate(error);
    }
  }

  async listCities(
    context: SiteRequestContext,
    query: CatalogCollectionQuery,
  ): Promise<CatalogPage<City>> {
    try {
      return await this.repository.listCities(await this.pool(context), context.siteId, query);
    } catch (error) {
      return translate(error);
    }
  }

  async findCity(context: SiteRequestContext, id: number): Promise<City> {
    const city = await this.repository.findCity(await this.pool(context), context.siteId, id);
    if (city === null) throw new NotFoundException();
    return city;
  }

  async createCity(context: SiteRequestContext, input: CreateCityInput): Promise<City> {
    try {
      return await this.repository.createCity(
        await this.pool(context),
        context.siteId,
        context.userId,
        input,
      );
    } catch (error) {
      return translate(error);
    }
  }

  async updateCity(context: SiteRequestContext, id: number, input: UpdateCityInput): Promise<City> {
    try {
      return await this.repository.updateCity(
        await this.pool(context),
        context.siteId,
        context.userId,
        id,
        input,
      );
    } catch (error) {
      return translate(error);
    }
  }

  async deleteCity(context: SiteRequestContext, id: number): Promise<void> {
    try {
      await this.repository.deleteCity(
        await this.pool(context),
        context.siteId,
        context.userId,
        id,
      );
    } catch (error) {
      return translate(error);
    }
  }
}
