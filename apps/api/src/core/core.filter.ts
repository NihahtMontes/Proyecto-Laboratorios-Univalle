import { ArgumentsHost, Catch, ExceptionFilter, HttpException } from '@nestjs/common';
import type { ApiFailure } from '@lu/contracts';
import { AuthException, AuthValidationException } from '../auth/auth.exceptions.js';
import type { FastifyReply, FastifyRequest } from '../auth/auth.fastify.js';
import { setCorsHeaders, setSecurityHeaders } from '../auth/auth.interceptor.js';

@Catch()
export class CoreExceptionFilter implements ExceptionFilter {
  catch(exception: unknown, host: ArgumentsHost): void {
    const context = host.switchToHttp();
    const request = context.getRequest<FastifyRequest>();
    const reply = context.getResponse<FastifyReply>();
    let status = 503;
    let code = 'SERVICE_UNAVAILABLE';
    let message = 'Service is temporarily unavailable.';
    let fieldErrors: Readonly<Record<string, readonly string[]>> | undefined;

    if (exception instanceof AuthException) {
      status = exception.statusCode;
      code = exception.code;
      message = exception.message;
      if (exception instanceof AuthValidationException) fieldErrors = exception.fieldErrors;
    } else if (exception instanceof HttpException) {
      status = exception.getStatus();
      code = status === 404 ? 'NOT_FOUND' : 'REQUEST_REJECTED';
      message = status === 404 ? 'Resource not found.' : 'Request rejected.';
    }

    setSecurityHeaders(reply);
    if (request.authAllowedOrigin) {
      setCorsHeaders(reply, request.authAllowedOrigin);
    }

    const body: ApiFailure = {
      success: false,
      error: {
        code,
        message,
        ...(request.correlationId ? { correlationId: request.correlationId } : {}),
        ...(fieldErrors ? { fieldErrors } : {}),
      },
    };
    reply.status(status).send(body);
  }
}
