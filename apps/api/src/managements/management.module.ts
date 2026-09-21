import { Module } from '@nestjs/common';
import { AuthModule } from '../auth/auth.module.js';
import { CoreModule } from '../core/core.module.js';
import { ManagementController } from './management.controller.js';
import { ManagementRepository } from './management.repository.js';
import { ManagementService } from './management.service.js';

@Module({
  imports: [AuthModule, CoreModule],
  controllers: [ManagementController],
  providers: [ManagementRepository, ManagementService],
})
export class ManagementModule {}
