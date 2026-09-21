import { ArgumentsHost, Catch, ExceptionFilter, HttpException } from '@nestjs/common';
import type { FastifyReply, FastifyRequest } from './auth.fastify.js';
import { AuthErrorCode } from './auth.constants.js';
import { AuthException, AuthValidationException } from './auth.exceptions.js';
import { setCorsHeaders, setSecurityHeaders } from './auth.interceptor.js';

@Catch(AuthException, Error)
export class AuthExceptionFilter implements ExceptionFilter {
  catch(exception: unknown, host: ArgumentsHost): void {
    const ctx = host.switchToHttp();
    const request = ctx.getRequest<FastifyRequest>();
    const reply = ctx.getResponse<FastifyReply>();

    let status = 503;
    let code: AuthErrorCode = 'SERVICE_UNAVAILABLE';
    let message = 'Authentication service is temporarily unavailable.';
    let fieldErrors: Readonly<Record<string, readonly string[]>> | undefined;
    let retryAfter: number | undefined;

    if (exception instanceof AuthException) {
      status = exception.statusCode;
      code = exception.code;
      message = exception.message;
      if (exception instanceof AuthValidationException) {
        fieldErrors = exception.fieldErrors;
      }
      if ('retryAfterSeconds' in exception) {
        retryAfter = (exception as { retryAfterSeconds: number }).retryAfterSeconds;
      }
    } else if (exception instanceof HttpException) {
      status = exception.getStatus();
      const response = exception.getResponse();
      code = 'SERVICE_UNAVAILABLE';
      message = typeof response === 'string' ? response : 'Unexpected error.';
    }

    // Never expose password or token values in error responses.
    setSecurityHeaders(reply);
    const allowedOrigin = request.authAllowedOrigin;
    if (allowedOrigin) {
      setCorsHeaders(reply, allowedOrigin);
    }

    if (retryAfter !== undefined) {
      reply.header('Retry-After', String(retryAfter));
    }

    const body = {
      success: false as const,
      error: {
        code,
        message,
        ...(fieldErrors ? { fieldErrors } : {}),
      },
    };

    reply.status(status).send(body);
  }
}
