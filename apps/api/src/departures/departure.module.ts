import { Module } from '@nestjs/common';
import { AuthModule } from '../auth/auth.module.js';
import { CoreModule } from '../core/core.module.js';
import { DepartureController } from './departure.controller.js';
import { DepartureRepository } from './departure.repository.js';
import { DepartureService } from './departure.service.js';

@Module({
  imports: [AuthModule, CoreModule],
  controllers: [DepartureController],
  providers: [DepartureRepository, DepartureService],
})
export class DepartureModule {}
