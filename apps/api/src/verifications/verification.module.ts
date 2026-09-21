import { Module } from '@nestjs/common';
import { AuthModule } from '../auth/auth.module.js';
import { CoreModule } from '../core/core.module.js';
import { VerificationController } from './verification.controller.js';
import { VerificationRepository } from './verification.repository.js';
import { VerificationService } from './verification.service.js';

@Module({
  imports: [AuthModule, CoreModule],
  controllers: [VerificationController],
  providers: [VerificationRepository, VerificationService],
})
export class VerificationModule {}
