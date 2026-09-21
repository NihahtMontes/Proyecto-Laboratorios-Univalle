import { Module } from '@nestjs/common';
import { AuthModule } from '../auth/auth.module.js';
import { CoreModule } from '../core/core.module.js';
import { CatalogController } from './catalog.controller.js';
import { CatalogRepository } from './catalog.repository.js';
import { CatalogService } from './catalog.service.js';

@Module({
  imports: [AuthModule, CoreModule],
  controllers: [CatalogController],
  providers: [CatalogRepository, CatalogService],
})
export class CatalogModule {}
