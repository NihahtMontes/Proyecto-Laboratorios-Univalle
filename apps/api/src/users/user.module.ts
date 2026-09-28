import { Module, type OnModuleInit } from '@nestjs/common';
import { HttpAdapterHost } from '@nestjs/core';
import { AuthModule } from '../auth/auth.module.js';
import { CoreModule } from '../core/core.module.js';
import { MembershipRepository } from './membership.repository.js';
import {
  registerProfilePhotoBodyParser,
  type ContentTypeParserHost,
} from './profile-photo/profile-photo.body-parser.js';
import { ProfilePhotoController } from './profile-photo/profile-photo.controller.js';
import { ProfilePhotoRepository } from './profile-photo/profile-photo.repository.js';
import { ProfilePhotoService } from './profile-photo/profile-photo.service.js';
import {
  createProfilePhotoStorage,
  PROFILE_PHOTO_STORAGE,
} from './profile-photo/profile-photo.storage.js';
import { ProfileController, UserController } from './user.controller.js';
import { UserRepository } from './user.repository.js';
import { SuperAdminRoleService } from './superadmin-role.service.js';
import { UserService } from './user.service.js';

@Module({
  imports: [AuthModule, CoreModule],
  controllers: [UserController, ProfileController, ProfilePhotoController],
  providers: [
    UserRepository,
    MembershipRepository,
    UserService,
    SuperAdminRoleService,
    ProfilePhotoRepository,
    ProfilePhotoService,
    { provide: PROFILE_PHOTO_STORAGE, useFactory: () => createProfilePhotoStorage(process.env) },
  ],
  exports: [UserService, SuperAdminRoleService, UserRepository, MembershipRepository],
})
export class UserModule implements OnModuleInit {
  constructor(private readonly adapterHost: HttpAdapterHost) {}

  onModuleInit(): void {
    const instance = this.adapterHost.httpAdapter?.getInstance?.() as
      Partial<ContentTypeParserHost> | undefined;
    if (
      instance !== undefined &&
      typeof instance.addContentTypeParser === 'function' &&
      typeof instance.hasContentTypeParser === 'function'
    ) {
      registerProfilePhotoBodyParser(instance as ContentTypeParserHost);
    }
  }
}
