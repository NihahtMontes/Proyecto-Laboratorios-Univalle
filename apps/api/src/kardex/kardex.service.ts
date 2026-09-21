import { Injectable, NotFoundException } from '@nestjs/common';
import type {
  KardexInput,
  KardexPage,
  KardexQuery,
  KardexRecord,
  SiteRequestContext,
} from '@lu/contracts';
import { SiteRole } from '@lu/contracts';
import { AuthForbiddenException, AuthValidationException } from '../auth/auth.exceptions.js';
import { TenantPgPoolRegistry } from '../core/tenant-pg-pool.js';
import { TenantRouter } from '../core/tenant-routing.js';
import { CatalogConflictError, CatalogMissingError } from '../catalogs/catalog.repository.js';
import { KardexRepository } from './kardex.repository.js';
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
export class KardexService {
  constructor(
    private readonly router: TenantRouter,
    private readonly pools: TenantPgPoolRegistry,
    private readonly repository: KardexRepository,
  ) {}
  private async tenant(context: SiteRequestContext) {
    administrator(context);
    return this.pools.get(await this.router.resolve(context));
  }
  async list(context: SiteRequestContext, query: KardexQuery): Promise<KardexPage> {
    try {
      return await this.repository.list(await this.tenant(context), context.siteId, query);
    } catch (error) {
      return translate(error);
    }
  }
  async find(context: SiteRequestContext, id: number): Promise<KardexRecord> {
    try {
      const result = await this.repository.find(await this.tenant(context), context.siteId, id);
      if (result === null) throw new NotFoundException();
      return result;
    } catch (error) {
      return translate(error);
    }
  }
  async draft(context: SiteRequestContext, input: KardexInput): Promise<KardexRecord> {
    try {
      return await this.repository.draft(
        await this.tenant(context),
        context.siteId,
        context.userId,
        input,
      );
    } catch (error) {
      return translate(error);
    }
  }
  async complete(context: SiteRequestContext, input: KardexInput): Promise<KardexRecord> {
    try {
      return await this.repository.complete(
        await this.tenant(context),
        context.siteId,
        context.userId,
        input,
      );
    } catch (error) {
      return translate(error);
    }
  }
}
