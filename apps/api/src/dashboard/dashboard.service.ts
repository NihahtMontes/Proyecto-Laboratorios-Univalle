import { Injectable } from '@nestjs/common';
import type {
  DashboardNotificationsQuery,
  DashboardQuery,
  DashboardSummary,
  SiteRequestContext,
} from '@lu/contracts';
import { TenantPgPoolRegistry } from '../core/tenant-pg-pool.js';
import { TenantRouter } from '../core/tenant-routing.js';
import { DashboardRepository } from './dashboard.repository.js';

@Injectable()
export class DashboardService {
  constructor(
    private readonly tenantRouter: TenantRouter,
    private readonly tenantPools: TenantPgPoolRegistry,
    private readonly repository: DashboardRepository,
  ) {}

  async summary(context: SiteRequestContext, query: DashboardQuery): Promise<DashboardSummary> {
    const route = await this.tenantRouter.resolve(context);
    return this.repository.loadSummary(this.tenantPools.get(route), context.siteId, query);
  }

  async notifications(context: SiteRequestContext, query: DashboardNotificationsQuery) {
    const route = await this.tenantRouter.resolve(context);
    return this.repository.listNotifications(this.tenantPools.get(route), context.siteId, query);
  }

  async markNotificationRead(context: SiteRequestContext, id: number): Promise<void> {
    const route = await this.tenantRouter.resolve(context);
    await this.repository.markNotificationRead(this.tenantPools.get(route), context.siteId, id);
  }

  async markAllNotificationsRead(context: SiteRequestContext): Promise<void> {
    const route = await this.tenantRouter.resolve(context);
    await this.repository.markAllNotificationsRead(this.tenantPools.get(route), context.siteId);
  }
}
