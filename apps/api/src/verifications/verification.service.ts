import { Injectable, NotFoundException } from '@nestjs/common';
import type {
  CreateVerificationInput,
  MassVerificationInput,
  SiteRequestContext,
  VerificationCheckItem,
  VerificationDetail,
  VerificationPage,
  VerificationQuery,
} from '@lu/contracts';
import { SiteRole } from '@lu/contracts';
import { AuthForbiddenException, AuthValidationException } from '../auth/auth.exceptions.js';
import { TenantPgPoolRegistry } from '../core/tenant-pg-pool.js';
import { TenantRouter } from '../core/tenant-routing.js';
import { CatalogConflictError, CatalogMissingError } from '../catalogs/catalog.repository.js';
import { VerificationRepository } from './verification.repository.js';

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
export class VerificationService {
  constructor(
    private readonly router: TenantRouter,
    private readonly pools: TenantPgPoolRegistry,
    private readonly repository: VerificationRepository,
  ) {}
  private async tenant(context: SiteRequestContext) {
    administrator(context);
    return this.pools.get(await this.router.resolve(context));
  }
  async list(context: SiteRequestContext, query: VerificationQuery): Promise<VerificationPage> {
    try {
      return await this.repository.list(await this.tenant(context), context.siteId, query);
    } catch (error) {
      return translate(error);
    }
  }
  async find(context: SiteRequestContext, id: number): Promise<VerificationDetail> {
    try {
      const result = await this.repository.find(await this.tenant(context), context.siteId, id);
      if (result === null) throw new NotFoundException();
      return result;
    } catch (error) {
      return translate(error);
    }
  }
  async checkItems(context: SiteRequestContext): Promise<readonly VerificationCheckItem[]> {
    try {
      return await this.repository.checkItems(await this.tenant(context));
    } catch (error) {
      return translate(error);
    }
  }
  async save(
    context: SiteRequestContext,
    input: CreateVerificationInput,
  ): Promise<VerificationDetail> {
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
  async mass(
    context: SiteRequestContext,
    input: MassVerificationInput,
  ): Promise<readonly VerificationDetail[]> {
    try {
      return await this.repository.mass(
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
