import { Module } from '@nestjs/common';
import { AuthConfig } from './auth.config.js';
import { AuthController } from './auth.controller.js';
import { AuthExceptionFilter } from './auth.filter.js';
import { AuthSecurityGuard } from './auth.guard.js';
import { AuthHeadersInterceptor } from './auth.interceptor.js';
import { AuthPgPool } from './auth.pg-pool.js';
import { AuthRateLimitService } from './auth.rate-limit.js';
import { AuthRepository } from './auth.repository.js';
import { AuthService } from './auth.service.js';
import { AUTH_CONFIG, AUTH_PG_POOL } from './auth.constants.js';

@Module({
  controllers: [AuthController],
  providers: [
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
    AuthSecurityGuard,
    AuthHeadersInterceptor,
    AuthExceptionFilter,
  ],
  exports: [AUTH_CONFIG, AUTH_PG_POOL, AuthService, AuthSecurityGuard, AuthHeadersInterceptor],
})
export class AuthModule {}
