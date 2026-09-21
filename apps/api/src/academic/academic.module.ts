import { Module } from '@nestjs/common';
import { AuthModule } from '../auth/auth.module.js';
import { CoreModule } from '../core/core.module.js';
import { AcademicController } from './academic.controller.js';
import { AcademicRepository } from './academic.repository.js';
import { AcademicService } from './academic.service.js';

@Module({
  imports: [AuthModule, CoreModule],
  controllers: [AcademicController],
  providers: [AcademicRepository, AcademicService],
})
export class AcademicModule {}
