import { Module } from '@nestjs/common';
import { AuthModule } from '../auth/auth.module.js';
import { CoreModule } from '../core/core.module.js';
import { PersonController } from './person.controller.js';
import { PersonRepository } from './person.repository.js';
import { PersonService } from './person.service.js';

@Module({
  imports: [AuthModule, CoreModule],
  controllers: [PersonController],
  providers: [PersonRepository, PersonService],
})
export class PersonModule {}
