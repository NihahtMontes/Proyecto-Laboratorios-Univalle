import { Module } from '@nestjs/common';
import { AuthModule } from '../auth/auth.module.js';
import { CoreController } from './core.controller.js';
import { CoreExceptionFilter } from './core.filter.js';
import { ApiCorrelationGuard, SiteContextGuard } from './core.guard.js';
import {
  EnvironmentTenantConnectionResolver,
  TENANT_CONNECTION_RESOLVER,
  TenantPgPoolRegistry,
} from './tenant-pg-pool.js';
import {
  TENANT_ROUTE_CATALOG,
  TenantRouter,
  PostgresTenantRouteCatalog,
} from './tenant-routing.js';

@Module({
  imports: [AuthModule],
  controllers: [CoreController],
  providers: [
    ApiCorrelationGuard,
    SiteContextGuard,
    CoreExceptionFilter,
    {
      provide: TENANT_CONNECTION_RESOLVER,
      useClass: EnvironmentTenantConnectionResolver,
    },
    TenantPgPoolRegistry,
    {
      provide: TENANT_ROUTE_CATALOG,
      useClass: PostgresTenantRouteCatalog,
    },
    TenantRouter,
  ],
  exports: [TenantRouter, TenantPgPoolRegistry],
})
export class CoreModule {}
