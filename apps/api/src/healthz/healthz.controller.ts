import { Controller, Get } from '@nestjs/common';
import type { HealthzResponse } from '@lu/contracts';

@Controller('healthz')
export class HealthzController {
  @Get()
  getHealthz(): HealthzResponse {
    return {
      status: 'ok',
      timestamp: new Date().toISOString(),
    };
  }
}
