import { Module } from '@nestjs/common';
import { AuthConfig } from './auth.config.js';
import { AuthController } from './auth.controller.js';
import { AuthExceptionFilter } from './auth.filter.js';
import { AuthSecurityGuard } from './auth.guard.js';
import { AuthHeadersInterceptor } from './auth.interceptor.js';
import { AuthPgPool } from './auth.pg-pool.js';
import { AuthRateLimitService } from './auth.rate-limit.js';
import { AuthRepository } from './auth.repository.js';
import { AuthService, SelfSessionRenewerImpl } from './auth.service.js';
import { AUTH_CONFIG, AUTH_PG_POOL, SELF_SESSION_RENEWER } from './auth.constants.js';
import { IDENTITY_EXPORTS, IDENTITY_PROVIDERS } from '../identity/identity.providers.js';

@Module({
  controllers: [AuthController],
  providers: [
    ...IDENTITY_PROVIDERS,
    {
      provide: AUTH_CONFIG,
      useFactory: () => new AuthConfig(process.env),
    },
    {
      provide: AUTH_PG_POOL,
      useClass: AuthPgPool,
    },
    AuthRepository,
    AuthRateLimitService,
    AuthService,
    SelfSessionRenewerImpl,
    {
      provide: SELF_SESSION_RENEWER,
      useExisting: SelfSessionRenewerImpl,
    },
    AuthSecurityGuard,
    AuthHeadersInterceptor,
    AuthExceptionFilter,
  ],
  exports: [
    AUTH_CONFIG,
    AUTH_PG_POOL,
    AuthService,
    AuthSecurityGuard,
    AuthHeadersInterceptor,
    ...IDENTITY_EXPORTS,
    SELF_SESSION_RENEWER,
  ],
})
export class AuthModule {}
