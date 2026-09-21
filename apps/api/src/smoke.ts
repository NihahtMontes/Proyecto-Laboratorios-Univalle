import 'reflect-metadata';
import { createApp } from './main.js';

async function smoke(): Promise<void> {
  const app = await createApp();
  try {
    const response = await app.inject({
      method: 'GET',
      url: '/api/v1/healthz',
    });
    if (response.statusCode !== 200) {
      throw new Error(`Unexpected status: ${response.statusCode}`);
    }
    const body = JSON.parse(response.payload) as { status: string };
    if (body.status !== 'ok') {
      throw new Error(`Unexpected body status: ${body.status}`);
    }
    console.log('Smoke passed');
  } finally {
    await app.close();
  }
}

smoke().catch((err: unknown) => {
  console.error(err);
  process.exit(1);
});
