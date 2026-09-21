import { Injectable, NotFoundException } from '@nestjs/common';
import type {
  CreateEquipmentInput,
  EquipmentPage,
  EquipmentQuery,
  EquipmentRecord,
  SiteRequestContext,
  UpdateEquipmentInput,
} from '@lu/contracts';
import { SiteRole } from '@lu/contracts';
import { AuthForbiddenException, AuthValidationException } from '../auth/auth.exceptions.js';
import { TenantPgPoolRegistry } from '../core/tenant-pg-pool.js';
import { TenantRouter } from '../core/tenant-routing.js';
import { CatalogConflictError, CatalogMissingError } from '../catalogs/catalog.repository.js';
import { EquipmentRepository } from './equipment.repository.js';

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
export class EquipmentService {
  constructor(
    private readonly router: TenantRouter,
    private readonly pools: TenantPgPoolRegistry,
    private readonly repository: EquipmentRepository,
  ) {}
  private async tenant(context: SiteRequestContext) {
    administrator(context);
    const route = await this.router.resolve(context);
    return this.pools.get(route);
  }
  async list(context: SiteRequestContext, query: EquipmentQuery): Promise<EquipmentPage> {
    try {
      return await this.repository.list(await this.tenant(context), context.siteId, query);
    } catch (error) {
      return translate(error);
    }
  }
  async find(context: SiteRequestContext, id: number): Promise<EquipmentRecord> {
    try {
      const result = await this.repository.find(await this.tenant(context), context.siteId, id);
      if (result === null) throw new NotFoundException();
      return result;
    } catch (error) {
      return translate(error);
    }
  }
  async create(context: SiteRequestContext, input: CreateEquipmentInput): Promise<EquipmentRecord> {
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
    input: UpdateEquipmentInput,
  ): Promise<EquipmentRecord> {
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
  async remove(context: SiteRequestContext, id: number): Promise<void> {
    try {
      await this.repository.remove(await this.tenant(context), context.siteId, context.userId, id);
    } catch (error) {
      return translate(error);
    }
  }
}
