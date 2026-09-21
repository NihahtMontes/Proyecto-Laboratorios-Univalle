import { Module } from '@nestjs/common';
import { AuthModule } from '../auth/auth.module.js';
import { CoreModule } from '../core/core.module.js';
import { MaintenanceController } from './maintenance.controller.js';
import { MaintenanceRepository } from './maintenance.repository.js';
import { MaintenanceService } from './maintenance.service.js';

@Module({
  imports: [AuthModule, CoreModule],
  controllers: [MaintenanceController],
  providers: [MaintenanceRepository, MaintenanceService],
})
export class MaintenanceModule {}
