import { CanActivate, ExecutionContext, Injectable } from '@nestjs/common';
import { randomUUID } from 'node:crypto';
import type { SiteRequestContext } from '@lu/contracts';
import { GlobalRole, SiteRole } from '@lu/contracts';
import { SESSION_COOKIE } from '../auth/auth.constants.js';
import { AuthForbiddenException, AuthUnauthorizedException } from '../auth/auth.exceptions.js';
import type { FastifyReply, FastifyRequest } from '../auth/auth.fastify.js';
import { AuthService } from '../auth/auth.service.js';
import { parseCookies } from '../auth/auth.types.js';
import type { ResolvedSession } from '../auth/auth.types.js';
import { buildRequestIdentity } from '../auth/session/index.js';
import type { MembershipRole } from '../identity/identity.contracts.js';

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

/**
 * Resolves the presented session and requires a normal purpose. Restricted
 * password_change / site_selection sessions never reach business routes.
 */
async function resolveNormalSession(
  authService: AuthService,
  request: FastifyRequest,
): Promise<ResolvedSession> {
  const token = parseCookies(request.headers.cookie)[SESSION_COOKIE];
  if (token === undefined) {
    throw new AuthUnauthorizedException();
  }
  const session = await authService.resolveSession(token);
  if (session.purpose !== 'normal' || request.correlationId === undefined) {
    throw new AuthForbiddenException('SITE_ACCESS_DENIED');
  }
  return session;
}

function toContractSiteRole(role: MembershipRole): SiteRole {
  return role === 'Administrador' ? SiteRole.Administrador : SiteRole.Supervisor;
}

/**
 * Tenant routes: normal session with an eligible active site (account, site,
 * membership status and validity revalidated on every request by
 * AuthService.resolveSession). Sets request.siteContext and request.identity.
 */
@Injectable()
export class SiteContextGuard implements CanActivate {
  constructor(private readonly authService: AuthService) {}

  async canActivate(context: ExecutionContext): Promise<true> {
    const request = context.switchToHttp().getRequest<FastifyRequest>();
    const session = await resolveNormalSession(this.authService, request);
    const membership =
      session.activeSiteId === null
        ? undefined
        : session.eligibleMemberships.find((m) => m.siteId === session.activeSiteId);
    if (membership === undefined || request.correlationId === undefined) {
      throw new AuthForbiddenException('SITE_ACCESS_DENIED');
    }
    request.identity = buildRequestIdentity({
      correlationId: request.correlationId,
      sessionId: session.sessionId,
      identity: session.identity,
      activeSiteId: membership.siteId,
      memberships: session.eligibleMemberships,
    });
    const siteContext: SiteRequestContext = {
      correlationId: request.correlationId,
      userId: session.identity.id,
      siteId: membership.siteId as SiteRequestContext['siteId'],
      siteName: membership.site.name,
      siteRole: toContractSiteRole(membership.role),
      globalRole: session.identity.isSuperAdmin ? GlobalRole.SuperAdmin : null,
    };
    request.siteContext = siteContext;
    return true;
  }
}

/**
 * Global routes (/profile, global Users administration): normal session with
 * an optional active site. A null site is allowed only for SuperAdmin
 * (F1 §17); a non-SuperAdmin must hold an eligible active site.
 */
@Injectable()
export class GlobalSessionGuard implements CanActivate {
  constructor(private readonly authService: AuthService) {}

  async canActivate(context: ExecutionContext): Promise<true> {
    const request = context.switchToHttp().getRequest<FastifyRequest>();
    const session = await resolveNormalSession(this.authService, request);
    if (session.activeSiteId === null && !session.identity.isSuperAdmin) {
      throw new AuthForbiddenException('SITE_ACCESS_DENIED');
    }
    request.identity = buildRequestIdentity({
      correlationId: request.correlationId ?? '',
      sessionId: session.sessionId,
      identity: session.identity,
      activeSiteId: session.activeSiteId,
      memberships: session.eligibleMemberships,
    });
    return true;
  }
}
