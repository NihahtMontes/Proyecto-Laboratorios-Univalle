import { Module } from '@nestjs/common';
import { AuthModule } from './auth/auth.module.js';
import { HealthzController } from './healthz/healthz.controller.js';
import { CoreModule } from './core/core.module.js';
import { DashboardModule } from './dashboard/dashboard.module.js';
import { CatalogModule } from './catalogs/catalog.module.js';
import { AcademicModule } from './academic/academic.module.js';
import { EquipmentModule } from './equipment/equipment.module.js';
import { EquipmentUnitModule } from './equipment-units/equipment-unit.module.js';
import { ManagementModule } from './managements/management.module.js';
import { VerificationModule } from './verifications/verification.module.js';
import { RequestModule } from './requests/request.module.js';
import { MaintenanceModule } from './maintenances/maintenance.module.js';
import { DepartureModule } from './departures/departure.module.js';
import { KardexModule } from './kardex/kardex.module.js';
import { AcquisitionModule } from './acquisitions/acquisition.module.js';
import { PersonModule } from './people/person.module.js';
import { UserModule } from './users/user.module.js';
import { ReportModule } from './reports/report.module.js';

@Module({
  imports: [
    AuthModule,
    CoreModule,
    DashboardModule,
    CatalogModule,
    AcademicModule,
    EquipmentModule,
    EquipmentUnitModule,
    ManagementModule,
    VerificationModule,
    RequestModule,
    MaintenanceModule,
    DepartureModule,
    KardexModule,
    AcquisitionModule,
    PersonModule,
    UserModule,
    ReportModule,
  ],
  controllers: [HealthzController],
})
export class AppModule {}
