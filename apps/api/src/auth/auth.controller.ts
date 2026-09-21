import {
  Body,
  Controller,
  Get,
  HttpCode,
  Inject,
  Options,
  Post,
  Put,
  Req,
  Res,
  UseFilters,
  UseGuards,
  UseInterceptors,
} from '@nestjs/common';
import type { FastifyReply, FastifyRequest } from './auth.fastify.js';
import { AUTH_CONFIG, CSRF_COOKIE, SESSION_COOKIE } from './auth.constants.js';
import { AuthConfig } from './auth.config.js';
import { AuthExceptionFilter } from './auth.filter.js';
import { AuthSecurityGuard, SkipCsrf } from './auth.guard.js';
import { AuthHeadersInterceptor } from './auth.interceptor.js';
import { AuthService } from './auth.service.js';
import { clearCookie, serializeCookie } from './auth.crypto.js';
import { AuthForbiddenException, AuthValidationException } from './auth.exceptions.js';
import { getSingleHeader, parseCookies } from './auth.types.js';
import type { ActiveSiteSession, CsrfResponse } from './auth.types.js';

function getRemoteAddress(request: FastifyRequest): string | undefined {
  return request.socket?.remoteAddress;
}

function readSessionCookie(request: FastifyRequest): string | undefined {
  const cookies = parseCookies(request.headers.cookie);
  return cookies[SESSION_COOKIE];
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

const PREFLIGHT_ALLOWED_METHODS = ['GET', 'POST', 'PUT', 'OPTIONS'] as const;
const PREFLIGHT_ALLOWED_HEADERS = new Set(['content-type', 'x-csrf-token']);

@Controller('auth')
@UseFilters(AuthExceptionFilter)
@UseGuards(AuthSecurityGuard)
@UseInterceptors(AuthHeadersInterceptor)
export class AuthController {
  constructor(
    private readonly authService: AuthService,
    @Inject(AUTH_CONFIG) private readonly config: AuthConfig,
  ) {}

  @Options('*')
  @SkipCsrf()
  @HttpCode(204)
  preflight(@Req() request: FastifyRequest, @Res({ passthrough: true }) reply: FastifyReply): void {
    const origin = normalizeOrigin(request.headers.origin);
    if (
      origin === undefined ||
      origin === 'null' ||
      !this.config.allowedOrigins?.includes(origin)
    ) {
      throw new AuthForbiddenException('ORIGIN_DENIED');
    }

    const requestedMethod = getSingleHeader(request.headers, 'access-control-request-method');
    if (
      requestedMethod === undefined ||
      !PREFLIGHT_ALLOWED_METHODS.includes(
        requestedMethod.toUpperCase() as (typeof PREFLIGHT_ALLOWED_METHODS)[number],
      )
    ) {
      throw new AuthValidationException('Requested method is not allowed.', {
        'access-control-request-method': ['must be one of GET, POST, PUT, OPTIONS'],
      });
    }

    const requestedHeaders = getSingleHeader(request.headers, 'access-control-request-headers');
    if (requestedHeaders !== undefined) {
      const requested = requestedHeaders.split(',').map((h) => h.trim().toLowerCase());
      for (const header of requested) {
        if (header === '') {
          continue;
        }
        if (!PREFLIGHT_ALLOWED_HEADERS.has(header)) {
          throw new AuthValidationException('Requested header is not allowed.', {
            'access-control-request-headers': [`${header} is not allowed`],
          });
        }
      }
    }

    reply.header('Access-Control-Allow-Origin', origin);
    reply.header('Access-Control-Allow-Credentials', 'true');
    reply.header('Vary', 'Origin');
    reply.header('Access-Control-Allow-Methods', 'GET, POST, PUT, OPTIONS');
    reply.header('Access-Control-Allow-Headers', 'Content-Type, X-CSRF-Token');
  }

  @Get('csrf')
  @SkipCsrf()
  csrf(
    @Req() request: FastifyRequest,
    @Res({ passthrough: true }) reply: FastifyReply,
  ): CsrfResponse {
    const response = this.authService.generateCsrf();
    reply.header(
      'Set-Cookie',
      serializeCookie(CSRF_COOKIE, response.csrfToken, {
        path: '/',
        httpOnly: true,
        secure: true,
        sameSite: 'Lax',
        maxAge: this.config.csrfMaxAgeSeconds,
      }),
    );
    return response;
  }

  @Post('login')
  @HttpCode(200)
  async login(
    @Body() body: unknown,
    @Req() request: FastifyRequest,
    @Res({ passthrough: true }) reply: FastifyReply,
  ): Promise<{ success: true; data: ActiveSiteSession }> {
    const ip = getRemoteAddress(request);
    const result = await this.authService.login(body, ip);

    reply.header(
      'Set-Cookie',
      serializeCookie(SESSION_COOKIE, result.token, {
        path: '/',
        httpOnly: true,
        secure: true,
        sameSite: 'Lax',
        ...(result.persistent ? { maxAge: this.config.absoluteTtlSeconds } : {}),
      }),
    );

    return { success: true, data: result.session };
  }

  @Get('session')
  @SkipCsrf()
  async session(
    @Req() request: FastifyRequest,
  ): Promise<{ success: true; data: ActiveSiteSession }> {
    const sessionValue = readSessionCookie(request);
    const data = await this.authService.getSession(sessionValue);
    return { success: true, data };
  }

  @Put('session/active-site')
  async setActiveSite(
    @Body() body: unknown,
    @Req() request: FastifyRequest,
  ): Promise<{ success: true; data: ActiveSiteSession }> {
    const sessionValue = readSessionCookie(request);
    const data = await this.authService.setActiveSite(sessionValue, body);
    return { success: true, data };
  }

  @Post('logout')
  @HttpCode(204)
  async logout(
    @Req() request: FastifyRequest,
    @Res({ passthrough: true }) reply: FastifyReply,
  ): Promise<void> {
    const sessionValue = readSessionCookie(request);
    await this.authService.logout(sessionValue);

    reply.header(
      'Set-Cookie',
      clearCookie(SESSION_COOKIE, {
        path: '/',
        httpOnly: true,
        secure: true,
        sameSite: 'Lax',
      }),
    );
  }
}
