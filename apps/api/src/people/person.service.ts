import { Injectable, NotFoundException } from '@nestjs/common';
import type {
  CreatePersonInput,
  PersonPage,
  PersonQuery,
  PersonRecord,
  SiteRequestContext,
  UpdatePersonInput,
} from '@lu/contracts';
import { SiteRole } from '@lu/contracts';
import { AuthForbiddenException, AuthValidationException } from '../auth/auth.exceptions.js';
import { TenantPgPoolRegistry } from '../core/tenant-pg-pool.js';
import { TenantRouter } from '../core/tenant-routing.js';
import { CatalogConflictError, CatalogMissingError } from '../catalogs/catalog.repository.js';
import { PersonRepository } from './person.repository.js';

function administrator(context: SiteRequestContext): void {
  if (context.siteRole !== SiteRole.Administrador)
    throw new AuthForbiddenException('SITE_ACCESS_DENIED');
}
function translate(error: unknown): never {
  if (error instanceof CatalogMissingError) throw new NotFoundException();
  if (error instanceof CatalogConflictError)
    throw new AuthValidationException(
      error.message,
      error.field ? { [error.field]: [error.message] } : undefined,
    );
  throw error;
}

@Injectable()
export class PersonService {
  constructor(
    private readonly router: TenantRouter,
    private readonly pools: TenantPgPoolRegistry,
    private readonly repository: PersonRepository,
  ) {}
  private async tenant(context: SiteRequestContext) {
    administrator(context);
    return this.pools.get(await this.router.resolve(context));
  }
  async list(context: SiteRequestContext, query: PersonQuery): Promise<PersonPage> {
    try {
      return await this.repository.list(await this.tenant(context), context.siteId, query);
    } catch (error) {
      return translate(error);
    }
  }
  async find(context: SiteRequestContext, id: number): Promise<PersonRecord> {
    try {
      const value = await this.repository.find(await this.tenant(context), context.siteId, id);
      if (value === null) throw new NotFoundException();
      return value;
    } catch (error) {
      return translate(error);
    }
  }
  async create(context: SiteRequestContext, input: CreatePersonInput): Promise<PersonRecord> {
    try {
      return await this.repository.create(
        await this.tenant(context),
        context.siteId,
        context.userId,
        input,
      );
    } catch (error) {
      return translate(error);
    }
  }
  async update(
    context: SiteRequestContext,
    id: number,
    input: UpdatePersonInput,
  ): Promise<PersonRecord> {
    try {
      return await this.repository.update(
        await this.tenant(context),
        context.siteId,
        context.userId,
        id,
        input,
      );
    } catch (error) {
      return translate(error);
    }
  }
  async delete(context: SiteRequestContext, id: number): Promise<void> {
    try {
      await this.repository.delete(await this.tenant(context), context.siteId, context.userId, id);
    } catch (error) {
      return translate(error);
    }
  }
}
