import { CanActivate, ExecutionContext, Inject, Injectable, SetMetadata } from '@nestjs/common';
import { Reflector as NestReflector } from '@nestjs/core';
import type { FastifyRequest } from './auth.fastify.js';
import { AUTH_CONFIG, CSRF_COOKIE, CSRF_HEADER, SKIP_CSRF_KEY } from './auth.constants.js';
import { AuthConfig } from './auth.config.js';
import { AuthForbiddenException } from './auth.exceptions.js';
import { constantTimeStringEquals } from './auth.crypto.js';
import { parseCookies } from './auth.types.js';
import { getSingleHeader } from './auth.types.js';

export const SkipCsrf = () => SetMetadata(SKIP_CSRF_KEY, true);

function isMutatingMethod(method: string): boolean {
  return ['POST', 'PUT', 'PATCH', 'DELETE'].includes(method.toUpperCase());
}

function normalizeOrigin(origin: string | string[] | undefined): string | undefined {
  if (origin === undefined) {
    return undefined;
  }
  const value = Array.isArray(origin) ? (origin[0] ?? '') : origin;
  if (value === '') {
    return undefined;
  }
  return value.trim();
}

@Injectable()
export class AuthSecurityGuard implements CanActivate {
  constructor(
    @Inject(AUTH_CONFIG) private readonly config: AuthConfig,
    private readonly reflector: NestReflector,
  ) {}

  canActivate(context: ExecutionContext): true {
    const request = context.switchToHttp().getRequest<FastifyRequest>();
    const handler = context.getHandler();
    const skipCsrf = this.reflector.getAllAndOverride<boolean>(SKIP_CSRF_KEY, [
      handler,
      context.getClass(),
    ]);

    const origin = normalizeOrigin(request.headers.origin);
    this.assertOriginAllowed(origin, request.method);

    if (origin) {
      request.authAllowedOrigin = origin;
    }

    if (!skipCsrf) {
      this.assertCsrfValid(request);
    }

    return true;
  }

  private assertOriginAllowed(origin: string | undefined, method: string): void {
    this.config.validate();
    const allowed = this.config.allowedOrigins;

    if (origin === 'null') {
      throw new AuthForbiddenException('ORIGIN_DENIED');
    }

    if (isMutatingMethod(method)) {
      if (origin === undefined || allowed === null || !allowed.includes(origin)) {
        throw new AuthForbiddenException('ORIGIN_DENIED');
      }
      return;
    }

    // GET/HEAD: if Origin is present it must be allowed.
    if (origin !== undefined && (allowed === null || !allowed.includes(origin))) {
      throw new AuthForbiddenException('ORIGIN_DENIED');
    }
  }

  private assertCsrfValid(request: FastifyRequest): void {
    const cookies = parseCookies(request.headers.cookie);
    const cookieToken = cookies[CSRF_COOKIE];
    const headerToken = getSingleHeader(request.headers, CSRF_HEADER);

    if (
      cookieToken === undefined ||
      headerToken === undefined ||
      !constantTimeStringEquals(cookieToken, headerToken)
    ) {
      throw new AuthForbiddenException('CSRF_INVALID');
    }
  }
}
