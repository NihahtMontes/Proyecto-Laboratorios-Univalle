import 'reflect-metadata';
import type { NestFastifyApplication } from '@nestjs/platform-fastify';
import { createApp } from '../src/main.js';

describe('HealthzController (e2e)', () => {
  let app: NestFastifyApplication;

  beforeAll(async () => {
    app = await createApp();
  });

  afterAll(async () => {
    await app.close();
  });

  it('GET /api/v1/healthz returns 200', async () => {
    const response = await app.inject({
      method: 'GET',
      url: '/api/v1/healthz',
    });

    expect(response.statusCode).toBe(200);

    const body = JSON.parse(response.payload) as { status: string };
    expect(body.status).toBe('ok');
  });
});
