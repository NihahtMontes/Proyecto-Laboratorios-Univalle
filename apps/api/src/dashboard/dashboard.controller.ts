import {
  Controller,
  Get,
  Param,
  ParseIntPipe,
  Post,
  Query,
  Req,
  UseFilters,
  UseGuards,
  UseInterceptors,
} from '@nestjs/common';
import type {
  ApiSuccess,
  DashboardNotificationsQuery,
  DashboardQuery,
  DashboardSummary,
} from '@lu/contracts';
import { AuthSecurityGuard, SkipCsrf } from '../auth/auth.guard.js';
import { AuthHeadersInterceptor } from '../auth/auth.interceptor.js';
import { AuthValidationException } from '../auth/auth.exceptions.js';
import type { FastifyRequest } from '../auth/auth.fastify.js';
import { CoreExceptionFilter } from '../core/core.filter.js';
import { ApiCorrelationGuard, SiteContextGuard } from '../core/core.guard.js';
import { DashboardService } from './dashboard.service.js';

function optionalInt(value: string | undefined, field: string, min = 1, max = 2_147_483_647) {
  if (value === undefined || value.trim() === '') return undefined;
  if (!/^\d+$/.test(value)) {
    throw new AuthValidationException('Dashboard query is invalid.', {
      [field]: ['must be a positive integer'],
    });
  }
  const parsed = Number(value);
  if (!Number.isSafeInteger(parsed) || parsed < min || parsed > max) {
    throw new AuthValidationException('Dashboard query is invalid.', {
      [field]: [`must be between ${min} and ${max}`],
    });
  }
  return parsed;
}

function optionalText(value: string | undefined, maxLength: number): string | undefined {
  const normalized = value?.trim() ?? '';
  return normalized === '' ? undefined : normalized.slice(0, maxLength);
}

function contextOf(request: FastifyRequest) {
  if (request.siteContext === undefined) throw new Error('Site context was not resolved.');
  return request.siteContext;
}

@Controller()
@UseFilters(CoreExceptionFilter)
@UseGuards(ApiCorrelationGuard, AuthSecurityGuard, SiteContextGuard)
@UseInterceptors(AuthHeadersInterceptor)
export class DashboardController {
  constructor(private readonly service: DashboardService) {}

  @Get('dashboard')
  @SkipCsrf()
  async summary(
    @Req() request: FastifyRequest,
    @Query() query: Record<string, string | undefined>,
  ): Promise<ApiSuccess<DashboardSummary>> {
    const parsed: DashboardQuery = {
      managementId: optionalInt(query.managementId, 'managementId'),
      step: optionalInt(query.step, 'step', 0, 7),
      currentPage: optionalInt(query.currentPage, 'currentPage'),
      laboratoryId: optionalInt(query.laboratoryId, 'laboratoryId'),
      searchTerm: optionalText(query.searchTerm, 200),
      serialNumber: optionalText(query.serialNumber, 100),
      inventoryNumber: optionalText(query.inventoryNumber, 50),
    };
    return { success: true, data: await this.service.summary(contextOf(request), parsed) };
  }

  @Get('notifications')
  @SkipCsrf()
  async notifications(
    @Req() request: FastifyRequest,
    @Query() query: Record<string, string | undefined>,
  ) {
    const unreadOnly = query.unreadOnly === undefined ? undefined : query.unreadOnly === 'true';
    if (
      query.unreadOnly !== undefined &&
      query.unreadOnly !== 'true' &&
      query.unreadOnly !== 'false'
    ) {
      throw new AuthValidationException('Notification query is invalid.', {
        unreadOnly: ['must be true or false'],
      });
    }
    const parsed: DashboardNotificationsQuery = {
      unreadOnly,
      managementId: optionalInt(query.managementId, 'managementId'),
      scope: optionalText(query.scope, 50),
    };
    return { success: true, data: await this.service.notifications(contextOf(request), parsed) };
  }

  @Post('notifications/:id/read')
  async markNotificationRead(
    @Req() request: FastifyRequest,
    @Param('id', ParseIntPipe) id: number,
  ): Promise<ApiSuccess<null>> {
    await this.service.markNotificationRead(contextOf(request), id);
    return { success: true, data: null };
  }

  @Post('notifications/read-all')
  async markAllNotificationsRead(@Req() request: FastifyRequest): Promise<ApiSuccess<null>> {
    await this.service.markAllNotificationsRead(contextOf(request));
    return { success: true, data: null };
  }
}
