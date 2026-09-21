import { Module } from '@nestjs/common';
import { AuthModule } from '../auth/auth.module.js';
import { CoreModule } from '../core/core.module.js';
import { UserController } from './user.controller.js';
import { UserRepository } from './user.repository.js';
import { UserService } from './user.service.js';

@Module({
  imports: [AuthModule, CoreModule],
  controllers: [UserController],
  providers: [UserRepository, UserService],
})
export class UserModule {}
