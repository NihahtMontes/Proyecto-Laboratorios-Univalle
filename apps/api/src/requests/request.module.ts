import { Module } from '@nestjs/common';
import { AuthModule } from '../auth/auth.module.js';
import { CoreModule } from '../core/core.module.js';
import { RequestController } from './request.controller.js';
import { RequestRepository } from './request.repository.js';
import { RequestService } from './request.service.js';

@Module({
  imports: [AuthModule, CoreModule],
  controllers: [RequestController],
  providers: [RequestRepository, RequestService],
})
export class RequestModule {}
