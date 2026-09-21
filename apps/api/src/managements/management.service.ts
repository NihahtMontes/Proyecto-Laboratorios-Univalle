import { Inject, Injectable, NotFoundException } from '@nestjs/common';
import type {
  CreateManagementInput,
  ManagementPage,
  ManagementPlanPage,
  ManagementPlanQuery,
  ManagementRecord,
  ManagementQuery,
  SiteRequestContext,
  SyncManagementPlanInput,
  UpdateManagementInput,
} from '@lu/contracts';
import { SiteRole } from '@lu/contracts';
import { AUTH_PG_POOL } from '../auth/auth.constants.js';
import type { IPgPool } from '../auth/auth.pg-pool.js';
import { AuthForbiddenException, AuthValidationException } from '../auth/auth.exceptions.js';
import { TenantPgPoolRegistry } from '../core/tenant-pg-pool.js';
import { TenantRouter } from '../core/tenant-routing.js';
import { CatalogConflictError, CatalogMissingError } from '../catalogs/catalog.repository.js';
import { ManagementRepository } from './management.repository.js';

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
export class ManagementService {
  constructor(
    private readonly router: TenantRouter,
    private readonly pools: TenantPgPoolRegistry,
    private readonly repository: ManagementRepository,
    @Inject(AUTH_PG_POOL) private readonly controlPool: IPgPool,
  ) {}
  private async tenant(context: SiteRequestContext) {
    administrator(context);
    return this.pools.get(await this.router.resolve(context));
  }
  private async assertFaculty(
    context: SiteRequestContext,
    facultyId: number | null | undefined,
  ): Promise<void> {
    if (facultyId === null || facultyId === undefined) return;
    const result = await this.controlPool.query(
      `SELECT id FROM public.lu_faculty WHERE id=$1 AND status=0`,
      [facultyId],
    );
    if (result.rows[0] === undefined)
      throw new CatalogConflictError('The selected faculty is not active.', 'facultyId');
  }
  async list(context: SiteRequestContext, query: ManagementQuery): Promise<ManagementPage> {
    try {
      return await this.repository.list(await this.tenant(context), context.siteId, query);
    } catch (error) {
      return translate(error);
    }
  }
  async find(context: SiteRequestContext, id: number): Promise<ManagementRecord> {
    try {
      const result = await this.repository.find(await this.tenant(context), context.siteId, id);
      if (result === null) throw new NotFoundException();
      return result;
    } catch (error) {
      return translate(error);
    }
  }
  async plans(
    context: SiteRequestContext,
    id: number,
    query: ManagementPlanQuery,
  ): Promise<ManagementPlanPage> {
    try {
      return await this.repository.plans(await this.tenant(context), context.siteId, id, query);
    } catch (error) {
      return translate(error);
    }
  }
  async create(
    context: SiteRequestContext,
    input: CreateManagementInput,
  ): Promise<ManagementRecord> {
    try {
      await this.assertFaculty(context, input.facultyId);
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
    input: UpdateManagementInput,
  ): Promise<ManagementRecord> {
    try {
      await this.assertFaculty(context, input.facultyId);
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
  async activate(context: SiteRequestContext, id: number): Promise<ManagementRecord> {
    try {
      return await this.repository.activate(
        await this.tenant(context),
        context.siteId,
        context.userId,
        id,
      );
    } catch (error) {
      return translate(error);
    }
  }
  async close(context: SiteRequestContext, id: number): Promise<ManagementRecord> {
    try {
      return await this.repository.close(
        await this.tenant(context),
        context.siteId,
        context.userId,
        id,
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
  async syncPlans(
    context: SiteRequestContext,
    id: number,
    input: SyncManagementPlanInput,
  ): Promise<void> {
    try {
      await this.repository.syncPlans(
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
}
