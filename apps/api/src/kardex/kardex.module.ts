import { Module } from '@nestjs/common';
import { AuthModule } from '../auth/auth.module.js';
import { CoreModule } from '../core/core.module.js';
import { KardexController } from './kardex.controller.js';
import { KardexRepository } from './kardex.repository.js';
import { KardexService } from './kardex.service.js';
@Module({
  imports: [AuthModule, CoreModule],
  controllers: [KardexController],
  providers: [KardexRepository, KardexService],
})
export class KardexModule {}
