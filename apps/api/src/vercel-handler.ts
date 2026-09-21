import type { IncomingMessage, Server, ServerResponse } from 'node:http';
import type { NestFastifyApplication } from '@nestjs/platform-fastify';
import { createApp } from './main.js';

export type VercelApplicationFactory = () => Promise<NestFastifyApplication>;
export type VercelRequestHandler = (
  request: IncomingMessage,
  response: ServerResponse,
) => Promise<void>;

const REWRITTEN_PATH_PARAMETER = '__lu_path';

function isSafeApiPath(value: string): boolean {
  const segments = value.split('/');
  return (
    segments[0] === 'v1' &&
    segments.every(
      (segment) =>
        segment !== '' && segment !== '.' && segment !== '..' && /^[A-Za-z0-9._~-]+$/.test(segment),
    )
  );
}

/** Restores the public API path after Vercel routes it to /api/gateway. */
export function restoreRewrittenApiUrl(request: IncomingMessage): void {
  const currentUrl = request.url;
  if (currentUrl === undefined) {
    return;
  }
  const parsed = new URL(currentUrl, 'http://vercel.internal');
  const rewrittenPath = parsed.searchParams.get(REWRITTEN_PATH_PARAMETER);
  if (rewrittenPath === null) {
    return;
  }

  parsed.searchParams.delete(REWRITTEN_PATH_PARAMETER);
  const safePath = isSafeApiPath(rewrittenPath) ? rewrittenPath : '__invalid';
  const query = parsed.searchParams.toString();
  request.url = `/api/${safePath}${query === '' ? '' : `?${query}`}`;
}

function requestServer(application: NestFastifyApplication): Server {
  return application.getHttpAdapter().getInstance().server as Server;
}

function dispatch(
  server: Server,
  request: IncomingMessage,
  response: ServerResponse,
): Promise<void> {
  return new Promise((resolve, reject) => {
    const cleanup = (): void => {
      response.off('finish', resolveRequest);
      response.off('close', resolveRequest);
      response.off('error', rejectRequest);
    };
    const resolveRequest = (): void => {
      cleanup();
      resolve();
    };
    const rejectRequest = (error: Error): void => {
      cleanup();
      reject(error);
    };

    response.once('finish', resolveRequest);
    response.once('close', resolveRequest);
    response.once('error', rejectRequest);

    try {
      if (!server.emit('request', request, response)) {
        rejectRequest(new Error('The application server has no request listener.'));
      }
    } catch (error) {
      rejectRequest(error instanceof Error ? error : new Error('Request dispatch failed.'));
    }
  });
}

function sendUnavailable(response: ServerResponse): void {
  if (response.writableEnded) {
    return;
  }
  if (response.headersSent) {
    response.end();
    return;
  }

  const payload = JSON.stringify({
    success: false,
    error: {
      code: 'SERVICE_UNAVAILABLE',
      message: 'Service temporarily unavailable.',
    },
  });
  response.writeHead(503, {
    'Cache-Control': 'no-store',
    'Content-Length': Buffer.byteLength(payload),
    'Content-Type': 'application/json; charset=utf-8',
    'X-Content-Type-Options': 'nosniff',
  });
  response.end(payload);
}

/**
 * Adapts the NestJS/Fastify server to Vercel's Node.js request/response
 * function signature. The initialized application is reused only inside the
 * same warm isolate; PostgreSQL and session state remain external.
 */
export function createVercelHandler(
  applicationFactory: VercelApplicationFactory = createApp,
): VercelRequestHandler {
  let applicationPromise: Promise<NestFastifyApplication> | null = null;

  const getApplication = (): Promise<NestFastifyApplication> => {
    if (applicationPromise === null) {
      const pending = applicationFactory();
      applicationPromise = pending;
      void pending.catch(() => {
        if (applicationPromise === pending) {
          applicationPromise = null;
        }
      });
    }
    return applicationPromise;
  };

  return async (request, response): Promise<void> => {
    try {
      restoreRewrittenApiUrl(request);
      const application = await getApplication();
      await dispatch(requestServer(application), request, response);
    } catch {
      console.error('Vercel API request failed.');
      sendUnavailable(response);
    }
  };
}

const handler = createVercelHandler();

export default handler;
