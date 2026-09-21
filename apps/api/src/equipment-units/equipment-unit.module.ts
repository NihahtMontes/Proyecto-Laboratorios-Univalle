import { Module } from '@nestjs/common';
import { AuthModule } from '../auth/auth.module.js';
import { CoreModule } from '../core/core.module.js';
import { EquipmentUnitController } from './equipment-unit.controller.js';
import { EquipmentUnitRepository } from './equipment-unit.repository.js';
import { EquipmentUnitService } from './equipment-unit.service.js';

@Module({
  imports: [AuthModule, CoreModule],
  controllers: [EquipmentUnitController],
  providers: [EquipmentUnitRepository, EquipmentUnitService],
})
export class EquipmentUnitModule {}
