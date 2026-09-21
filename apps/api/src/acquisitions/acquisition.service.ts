import { Injectable, NotFoundException } from '@nestjs/common';
import type {
  AcquisitionDetail,
  AcquisitionPage,
  AcquisitionQuery,
  CreateAcquisitionInput,
  SiteRequestContext,
  UpdateAcquisitionInput,
} from '@lu/contracts';
import { SiteRole } from '@lu/contracts';
import { AuthForbiddenException, AuthValidationException } from '../auth/auth.exceptions.js';
import { TenantPgPoolRegistry } from '../core/tenant-pg-pool.js';
import { TenantRouter } from '../core/tenant-routing.js';
import { CatalogConflictError, CatalogMissingError } from '../catalogs/catalog.repository.js';
import { AcquisitionRepository } from './acquisition.repository.js';

function managementAccess(context: SiteRequestContext): void {
  if (context.siteRole !== SiteRole.Supervisor && context.siteRole !== SiteRole.Administrador)
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
export class AcquisitionService {
  constructor(
    private readonly router: TenantRouter,
    private readonly pools: TenantPgPoolRegistry,
    private readonly repository: AcquisitionRepository,
  ) {}

  private async tenant(context: SiteRequestContext) {
    managementAccess(context);
    return this.pools.get(await this.router.resolve(context));
  }

  async list(context: SiteRequestContext, query: AcquisitionQuery): Promise<AcquisitionPage> {
    try {
      return await this.repository.list(await this.tenant(context), context.siteId, query);
    } catch (error) {
      return translate(error);
    }
  }

  async find(context: SiteRequestContext, id: number): Promise<AcquisitionDetail> {
    try {
      const value = await this.repository.find(await this.tenant(context), context.siteId, id);
      if (value === null) throw new NotFoundException();
      return value;
    } catch (error) {
      return translate(error);
    }
  }

  async save(
    context: SiteRequestContext,
    input: CreateAcquisitionInput,
  ): Promise<AcquisitionDetail> {
    try {
      return await this.repository.save(
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
    input: UpdateAcquisitionInput,
  ): Promise<AcquisitionDetail> {
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

  async complete(context: SiteRequestContext, id: number): Promise<AcquisitionDetail> {
    try {
      return await this.repository.complete(
        await this.tenant(context),
        context.siteId,
        context.userId,
        id,
      );
    } catch (error) {
      return translate(error);
    }
  }

  async cancel(context: SiteRequestContext, id: number): Promise<void> {
    try {
      await this.repository.cancel(await this.tenant(context), context.siteId, context.userId, id);
    } catch (error) {
      return translate(error);
    }
  }
}
