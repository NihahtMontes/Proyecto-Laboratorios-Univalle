import { Controller, Get, Req, UseFilters, UseGuards, UseInterceptors } from '@nestjs/common';
import type { ApiSuccess, SiteRequestContext } from '@lu/contracts';
import { AuthSecurityGuard, SkipCsrf } from '../auth/auth.guard.js';
import type { FastifyRequest } from '../auth/auth.fastify.js';
import { AuthHeadersInterceptor } from '../auth/auth.interceptor.js';
import { CoreExceptionFilter } from './core.filter.js';
import { ApiCorrelationGuard, SiteContextGuard } from './core.guard.js';

@Controller('context')
@UseFilters(CoreExceptionFilter)
@UseGuards(ApiCorrelationGuard, AuthSecurityGuard, SiteContextGuard)
@UseInterceptors(AuthHeadersInterceptor)
export class CoreController {
  @Get()
  @SkipCsrf()
  getSiteContext(@Req() request: FastifyRequest): ApiSuccess<SiteRequestContext> {
    if (request.siteContext === undefined) {
      throw new Error('Site context was not resolved.');
    }
    return { success: true, data: request.siteContext };
  }
}
