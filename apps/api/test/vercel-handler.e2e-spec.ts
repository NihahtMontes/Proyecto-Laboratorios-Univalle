import 'reflect-metadata';
import { createServer, type IncomingMessage, type Server } from 'node:http';
import type { AddressInfo } from 'node:net';
import type { NestFastifyApplication } from '@nestjs/platform-fastify';
import { createApp } from '../src/main.js';
import {
  createVercelHandler,
  restoreRewrittenApiUrl,
  type VercelApplicationFactory,
  type VercelRequestHandler,
} from '../src/vercel-handler.js';

function listen(handler: VercelRequestHandler): Promise<{ server: Server; origin: string }> {
  const server = createServer((request, response) => {
    void handler(request, response);
  });
  return new Promise((resolve, reject) => {
    server.once('error', reject);
    server.listen(0, '127.0.0.1', () => {
      server.off('error', reject);
      const address = server.address() as AddressInfo;
      resolve({ server, origin: `http://127.0.0.1:${address.port}` });
    });
  });
}

function close(server: Server): Promise<void> {
  return new Promise((resolve, reject) => {
    server.close((error) => {
      if (error) {
        reject(error);
      } else {
        resolve();
      }
    });
  });
}

function fakeApplication(server: Server): NestFastifyApplication {
  return {
    getHttpAdapter: () => ({
      getInstance: () => ({ server }),
    }),
  } as unknown as NestFastifyApplication;
}

describe('Vercel NestJS adapter', () => {
  it('restores the public API path and preserves non-internal query parameters', () => {
    const request = {
      url: '/api/gateway?__lu_path=v1%2Fauth%2Fcsrf&locale=es-BO',
    } as IncomingMessage;

    restoreRewrittenApiUrl(request);

    expect(request.url).toBe('/api/v1/auth/csrf?locale=es-BO');
  });

  it('maps unsafe rewritten paths to an unhandled internal route', () => {
    const request = {
      url: '/api/gateway?__lu_path=v1%2F..%2Fadmin',
    } as IncomingMessage;

    restoreRewrittenApiUrl(request);

    expect(request.url).toBe('/api/__invalid');
  });

  it('serves the real health endpoint through the Node request handler', async () => {
    let application: NestFastifyApplication | null = null;
    const handler = createVercelHandler(async () => {
      application = await createApp();
      return application;
    });
    const { server, origin } = await listen(handler);

    try {
      const response = await fetch(`${origin}/api/v1/healthz`);
      expect(response.status).toBe(200);
      await expect(response.json()).resolves.toEqual(
        expect.objectContaining({ status: 'ok', timestamp: expect.any(String) }),
      );
    } finally {
      await close(server);
      if (application !== null) {
        await (application as NestFastifyApplication).close();
      }
    }
  });

  it('reuses one initialized application inside a warm isolate', async () => {
    const backend = createServer((request, response) => {
      response.writeHead(200, { 'Content-Type': 'application/json' });
      response.end(JSON.stringify({ method: request.method, url: request.url }));
    });
    let factoryCalls = 0;
    const factory: VercelApplicationFactory = async () => {
      factoryCalls += 1;
      return fakeApplication(backend);
    };
    const { server, origin } = await listen(createVercelHandler(factory));

    try {
      const first = await fetch(`${origin}/api/v1/healthz`);
      const second = await fetch(`${origin}/api/v1/context`);
      expect(first.status).toBe(200);
      expect(second.status).toBe(200);
      expect(factoryCalls).toBe(1);
      await expect(second.json()).resolves.toEqual({
        method: 'GET',
        url: '/api/v1/context',
      });
    } finally {
      await close(server);
      backend.close();
    }
  });

  it('returns a sanitized 503 and retries after bootstrap failure', async () => {
    const backend = createServer((_request, response) => {
      response.writeHead(200, { 'Content-Type': 'application/json' });
      response.end(JSON.stringify({ status: 'recovered' }));
    });
    let factoryCalls = 0;
    const factory: VercelApplicationFactory = async () => {
      factoryCalls += 1;
      if (factoryCalls === 1) {
        throw new Error('postgres://user:secret@example.test/control');
      }
      return fakeApplication(backend);
    };
    const originalConsoleError = console.error;
    const consoleErrors: unknown[][] = [];
    console.error = (...args: unknown[]): void => {
      consoleErrors.push(args);
    };
    const { server, origin } = await listen(createVercelHandler(factory));

    try {
      const failed = await fetch(`${origin}/api/v1/healthz`);
      expect(failed.status).toBe(503);
      expect(await failed.text()).toBe(
        JSON.stringify({
          success: false,
          error: {
            code: 'SERVICE_UNAVAILABLE',
            message: 'Service temporarily unavailable.',
          },
        }),
      );
      expect(consoleErrors).toEqual([['Vercel API request failed.']]);
      expect(consoleErrors.flat().join(' ')).not.toContain('secret');

      const recovered = await fetch(`${origin}/api/v1/healthz`);
      expect(recovered.status).toBe(200);
      await expect(recovered.json()).resolves.toEqual({ status: 'recovered' });
      expect(factoryCalls).toBe(2);
    } finally {
      console.error = originalConsoleError;
      await close(server);
      backend.close();
    }
  });
});
