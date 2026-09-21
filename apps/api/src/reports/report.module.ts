import { Module } from '@nestjs/common';
import { AuthModule } from '../auth/auth.module.js';
import { CoreModule } from '../core/core.module.js';
import { ReportController } from './report.controller.js';
import { ReportService } from './report.service.js';

@Module({
  imports: [AuthModule, CoreModule],
  controllers: [ReportController],
  providers: [ReportService],
})
export class ReportModule {}
