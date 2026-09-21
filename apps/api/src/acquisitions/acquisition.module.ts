import { Module } from '@nestjs/common';
import { AuthModule } from '../auth/auth.module.js';
import { CoreModule } from '../core/core.module.js';
import { AcquisitionController } from './acquisition.controller.js';
import { AcquisitionRepository } from './acquisition.repository.js';
import { AcquisitionService } from './acquisition.service.js';

@Module({
  imports: [AuthModule, CoreModule],
  controllers: [AcquisitionController],
  providers: [AcquisitionRepository, AcquisitionService],
})
export class AcquisitionModule {}
