import { Injectable, NotFoundException } from '@nestjs/common';
import type {
  AcademicQuery,
  Career,
  CatalogPage,
  CreateCareerInput,
  CreateFacultyInput,
  CreateLaboratoryInput,
  Faculty,
  Laboratory,
  SiteCareer,
  SiteRequestContext,
  UpdateCareerInput,
  UpdateFacultyInput,
  UpdateLaboratoryInput,
} from '@lu/contracts';
import { SiteRole } from '@lu/contracts';
import { AuthForbiddenException, AuthValidationException } from '../auth/auth.exceptions.js';
import { TenantPgPoolRegistry } from '../core/tenant-pg-pool.js';
import { TenantRouter } from '../core/tenant-routing.js';
import { CatalogConflictError, CatalogMissingError } from '../catalogs/catalog.repository.js';
import { AcademicRepository } from './academic.repository.js';

function admin(context: SiteRequestContext): void {
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
export class AcademicService {
  constructor(
    private readonly repository: AcademicRepository,
    private readonly tenantRouter: TenantRouter,
    private readonly tenantPools: TenantPgPoolRegistry,
  ) {}
  private async tenant(context: SiteRequestContext) {
    admin(context);
    const route = await this.tenantRouter.resolve(context);
    return this.tenantPools.get(route);
  }
  async listFaculties(
    context: SiteRequestContext,
    query: AcademicQuery,
  ): Promise<CatalogPage<Faculty>> {
    admin(context);
    try {
      return await this.repository.listFaculties(query);
    } catch (error) {
      return translate(error);
    }
  }
  async findFaculty(context: SiteRequestContext, id: number): Promise<Faculty> {
    admin(context);
    const result = await this.repository.findFaculty(id);
    if (result === null) throw new NotFoundException();
    return result;
  }
  async createFaculty(context: SiteRequestContext, input: CreateFacultyInput): Promise<Faculty> {
    admin(context);
    try {
      return await this.repository.createFaculty(context.userId, input);
    } catch (error) {
      return translate(error);
    }
  }
  async updateFaculty(
    context: SiteRequestContext,
    id: number,
    input: UpdateFacultyInput,
  ): Promise<Faculty> {
    admin(context);
    try {
      return await this.repository.updateFaculty(context.userId, id, input);
    } catch (error) {
      return translate(error);
    }
  }
  async deleteFaculty(context: SiteRequestContext, id: number): Promise<void> {
    admin(context);
    try {
      await this.repository.deleteFaculty(context.userId, id);
    } catch (error) {
      return translate(error);
    }
  }
  async listCareers(
    context: SiteRequestContext,
    query: AcademicQuery,
  ): Promise<CatalogPage<Career>> {
    admin(context);
    try {
      return await this.repository.listCareers(context.siteId, query);
    } catch (error) {
      return translate(error);
    }
  }
  async findCareer(context: SiteRequestContext, id: number): Promise<Career> {
    admin(context);
    const result = await this.repository.findCareer(context.siteId, id);
    if (result === null) throw new NotFoundException();
    return result;
  }
  async createCareer(context: SiteRequestContext, input: CreateCareerInput): Promise<Career> {
    admin(context);
    try {
      return await this.repository.createCareer(context.userId, context.siteId, input);
    } catch (error) {
      return translate(error);
    }
  }
  async updateCareer(
    context: SiteRequestContext,
    id: number,
    input: UpdateCareerInput,
  ): Promise<Career> {
    admin(context);
    try {
      return await this.repository.updateCareer(context.userId, context.siteId, id, input);
    } catch (error) {
      return translate(error);
    }
  }
  async deleteCareer(context: SiteRequestContext, id: number): Promise<void> {
    admin(context);
    try {
      await this.repository.deleteCareer(context.userId, context.siteId, id);
    } catch (error) {
      return translate(error);
    }
  }
  async listSiteCareers(context: SiteRequestContext): Promise<readonly SiteCareer[]> {
    admin(context);
    return this.repository.listSiteCareers(context.siteId);
  }
  async assignSiteCareer(context: SiteRequestContext, careerId: number): Promise<void> {
    admin(context);
    try {
      await this.repository.assignSiteCareer(context.userId, context.siteId, careerId);
    } catch (error) {
      return translate(error);
    }
  }
  async unassignSiteCareer(context: SiteRequestContext, careerId: number): Promise<void> {
    admin(context);
    try {
      await this.repository.unassignSiteCareer(context.userId, context.siteId, careerId);
    } catch (error) {
      return translate(error);
    }
  }
  async listLaboratories(
    context: SiteRequestContext,
    query: AcademicQuery,
  ): Promise<CatalogPage<Laboratory>> {
    try {
      return await this.repository.listLaboratories(
        await this.tenant(context),
        context.siteId,
        query,
      );
    } catch (error) {
      return translate(error);
    }
  }
  async findLaboratory(context: SiteRequestContext, id: number): Promise<Laboratory> {
    try {
      const result = await this.repository.findLaboratory(
        await this.tenant(context),
        context.siteId,
        id,
      );
      if (result === null) throw new NotFoundException();
      return result;
    } catch (error) {
      return translate(error);
    }
  }
  async createLaboratory(
    context: SiteRequestContext,
    input: CreateLaboratoryInput,
  ): Promise<Laboratory> {
    try {
      return await this.repository.createLaboratory(
        await this.tenant(context),
        context.siteId,
        context.userId,
        input,
      );
    } catch (error) {
      return translate(error);
    }
  }
  async updateLaboratory(
    context: SiteRequestContext,
    id: number,
    input: UpdateLaboratoryInput,
  ): Promise<Laboratory> {
    try {
      return await this.repository.updateLaboratory(
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
  async deleteLaboratory(context: SiteRequestContext, id: number): Promise<void> {
    try {
      await this.repository.deleteLaboratory(
        await this.tenant(context),
        context.siteId,
        context.userId,
        id,
      );
    } catch (error) {
      return translate(error);
    }
  }
}
