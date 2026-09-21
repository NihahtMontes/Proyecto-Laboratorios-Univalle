import { CanActivate, ExecutionContext, Injectable } from '@nestjs/common';
import { randomUUID } from 'node:crypto';
import type { SiteRequestContext } from '@lu/contracts';
import { SESSION_COOKIE } from '../auth/auth.constants.js';
import { AuthForbiddenException } from '../auth/auth.exceptions.js';
import type { FastifyReply, FastifyRequest } from '../auth/auth.fastify.js';
import { AuthService } from '../auth/auth.service.js';
import { parseCookies } from '../auth/auth.types.js';

@Injectable()
export class ApiCorrelationGuard implements CanActivate {
  canActivate(context: ExecutionContext): true {
    const request = context.switchToHttp().getRequest<FastifyRequest>();
    const reply = context.switchToHttp().getResponse<FastifyReply>();
    const correlationId = randomUUID();
    request.correlationId = correlationId;
    reply.header('X-Correlation-Id', correlationId);
    return true;
  }
}

@Injectable()
export class SiteContextGuard implements CanActivate {
  constructor(private readonly authService: AuthService) {}

  async canActivate(context: ExecutionContext): Promise<true> {
    const request = context.switchToHttp().getRequest<FastifyRequest>();
    const token = parseCookies(request.headers.cookie)[SESSION_COOKIE];
    const session = await this.authService.getSession(token);
    if (session.activeSiteId === null || request.correlationId === undefined) {
      throw new AuthForbiddenException('SITE_ACCESS_DENIED');
    }

    const membership = session.memberships.find(
      (candidate) => candidate.siteId === session.activeSiteId && candidate.state === 'active',
    );
    if (membership === undefined) {
      throw new AuthForbiddenException('SITE_ACCESS_DENIED');
    }

    const siteContext: SiteRequestContext = {
      correlationId: request.correlationId,
      userId: session.userId,
      siteId: session.activeSiteId,
      siteName: membership.siteName,
      siteRole: membership.role,
      globalRole: session.globalRole,
    };
    request.siteContext = siteContext;
    return true;
  }
}
