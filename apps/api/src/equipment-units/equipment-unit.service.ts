import { Inject, Injectable, NotFoundException } from '@nestjs/common';
import type {
  CreateEquipmentUnitInput,
  EquipmentUnitDetail,
  EquipmentUnitPage,
  EquipmentUnitQuery,
  EquipmentUnitStateHistory,
  SiteRequestContext,
  UpdateEquipmentUnitInput,
} from '@lu/contracts';
import { SiteRole } from '@lu/contracts';
import { AUTH_PG_POOL } from '../auth/auth.constants.js';
import type { IPgPool } from '../auth/auth.pg-pool.js';
import { AuthForbiddenException, AuthValidationException } from '../auth/auth.exceptions.js';
import { TenantPgPoolRegistry } from '../core/tenant-pg-pool.js';
import { TenantRouter } from '../core/tenant-routing.js';
import { CatalogConflictError, CatalogMissingError } from '../catalogs/catalog.repository.js';
import { EquipmentUnitRepository } from './equipment-unit.repository.js';

function writeRole(context: SiteRequestContext): void {
  if (context.siteRole !== SiteRole.Administrador && context.siteRole !== SiteRole.Supervisor)
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
export class EquipmentUnitService {
  constructor(
    private readonly router: TenantRouter,
    private readonly pools: TenantPgPoolRegistry,
    private readonly repository: EquipmentUnitRepository,
    @Inject(AUTH_PG_POOL) private readonly controlPool: IPgPool,
  ) {}

  private async tenant(context: SiteRequestContext, writing = false) {
    if (writing) writeRole(context);
    const route = await this.router.resolve(context);
    return this.pools.get(route);
  }

  private async assertCareer(
    context: SiteRequestContext,
    careerId: number | null | undefined,
  ): Promise<void> {
    if (careerId === null || careerId === undefined) return;
    const result = await this.controlPool.query(
      `SELECT 1
         FROM public.lu_site_career sc
         JOIN public.lu_career c ON c.id=sc.career_id
        WHERE sc.site_id=$1 AND sc.career_id=$2 AND sc.status=0 AND c.status=0
        LIMIT 1`,
      [context.siteId, careerId],
    );
    if (result.rows[0] === undefined)
      throw new CatalogConflictError(
        'The selected career is not active for this site.',
        'careerId',
      );
  }

  async list(context: SiteRequestContext, query: EquipmentUnitQuery): Promise<EquipmentUnitPage> {
    try {
      return await this.repository.list(await this.tenant(context), context.siteId, query);
    } catch (error) {
      return translate(error);
    }
  }

  async find(context: SiteRequestContext, id: number): Promise<EquipmentUnitDetail> {
    try {
      const result = await this.repository.find(await this.tenant(context), context.siteId, id);
      if (result === null) throw new NotFoundException();
      return result;
    } catch (error) {
      return translate(error);
    }
  }

  async history(
    context: SiteRequestContext,
    id: number,
  ): Promise<readonly EquipmentUnitStateHistory[]> {
    try {
      const pool = await this.tenant(context);
      const detail = await this.repository.find(pool, context.siteId, id);
      if (detail === null) throw new NotFoundException();
      return detail.stateHistory;
    } catch (error) {
      return translate(error);
    }
  }

  async create(
    context: SiteRequestContext,
    input: CreateEquipmentUnitInput,
  ): Promise<EquipmentUnitDetail> {
    try {
      await this.assertCareer(context, input.careerId);
      return await this.repository.create(
        await this.tenant(context, true),
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
    input: UpdateEquipmentUnitInput,
  ): Promise<EquipmentUnitDetail> {
    try {
      await this.assertCareer(context, input.careerId);
      return await this.repository.update(
        await this.tenant(context, true),
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
      await this.repository.remove(
        await this.tenant(context, true),
        context.siteId,
        context.userId,
        id,
      );
    } catch (error) {
      return translate(error);
    }
  }
}
