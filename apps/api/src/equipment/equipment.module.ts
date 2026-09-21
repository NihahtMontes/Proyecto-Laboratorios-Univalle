import { Module } from '@nestjs/common';
import { AuthModule } from '../auth/auth.module.js';
import { CoreModule } from '../core/core.module.js';
import { EquipmentController } from './equipment.controller.js';
import { EquipmentRepository } from './equipment.repository.js';
import { EquipmentService } from './equipment.service.js';
@Module({
  imports: [AuthModule, CoreModule],
  controllers: [EquipmentController],
  providers: [EquipmentRepository, EquipmentService],
})
export class EquipmentModule {}
