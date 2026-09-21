import 'reflect-metadata';
import { pathToFileURL } from 'node:url';
import { NestFactory } from '@nestjs/core';
import { FastifyAdapter, NestFastifyApplication } from '@nestjs/platform-fastify';
import { API_PREFIX } from '@lu/contracts';
import { AppModule } from './app.module.js';

export async function createApp(): Promise<NestFastifyApplication> {
  const app = await NestFactory.create<NestFastifyApplication>(AppModule, new FastifyAdapter(), {
    logger: false,
  });
  app.enableShutdownHooks();
  app.setGlobalPrefix(API_PREFIX.replace(/^\//, ''));
  await app.init();
  await app.getHttpAdapter().getInstance().ready();
  return app;
}

export async function bootstrap(): Promise<void> {
  const app = await createApp();
  const port = process.env.PORT ? Number(process.env.PORT) : 3000;
  const host = process.env.HOST ?? '127.0.0.1';
  await app.listen(port, host);
}

// Equivalente ESM de `require.main === module`: solo ejecuta bootstrap cuando
// este archivo es el entry point, no cuando lo importan tests o smoke.ts.
if (process.argv[1] && import.meta.url === pathToFileURL(process.argv[1]).href) {
  bootstrap().catch((err: unknown) => {
    console.error(err);
    process.exit(1);
  });
}
