import {
  Controller,
  Get,
  Param,
  Query,
  Req,
  StreamableFile,
  UseFilters,
  UseGuards,
  UseInterceptors,
} from '@nestjs/common';
import type { ApiSuccess, ReportKind, SiteRequestContext } from '@lu/contracts';
import { AuthSecurityGuard, SkipCsrf } from '../auth/auth.guard.js';
import { AuthHeadersInterceptor } from '../auth/auth.interceptor.js';
import type { FastifyRequest } from '../auth/auth.fastify.js';
import { ApiCorrelationGuard, SiteContextGuard } from '../core/core.guard.js';
import { CoreExceptionFilter } from '../core/core.filter.js';
import { ReportService } from './report.service.js';

function context(request: FastifyRequest): SiteRequestContext {
  if (request.siteContext === undefined) throw new Error('Site context was not resolved.');
  return request.siteContext;
}

function positive(value: string | undefined): number | undefined {
  if (value === undefined || value === '') return undefined;
  if (!/^\d+$/.test(value)) return undefined;
  const parsed = Number(value);
  return Number.isSafeInteger(parsed) && parsed > 0 ? parsed : undefined;
}

function kind(value: string): ReportKind {
  if (['l6', 'l7', 'l8', 'l3', 'l48', 'l12'].includes(value)) return value as ReportKind;
  throw new Error('Unsupported report kind.');
}

@Controller('reports')
@UseFilters(CoreExceptionFilter)
@UseGuards(ApiCorrelationGuard, AuthSecurityGuard, SiteContextGuard)
@UseInterceptors(AuthHeadersInterceptor)
export class ReportController {
  constructor(private readonly service: ReportService) {}

  @Get()
  @SkipCsrf()
  manifest(@Req() request: FastifyRequest): ApiSuccess<ReturnType<ReportService['manifest']>> {
    context(request);
    return { success: true, data: this.service.manifest() };
  }

  @Get('download/:kind')
  @SkipCsrf()
  async download(
    @Req() request: FastifyRequest,
    @Param('kind') rawKind: string,
    @Query() query: Record<string, string | undefined>,
  ): Promise<StreamableFile> {
    const report = await this.service.download(context(request), kind(rawKind), {
      managementId: positive(query.managementId),
      laboratoryId: positive(query.laboratoryId),
      planId: positive(query.planId),
      requestId: positive(query.requestId),
      departureId: positive(query.departureId),
    });
    return new StreamableFile(report.buffer, {
      type: report.contentType,
      disposition: `attachment; filename="${report.filename}"`,
      length: report.buffer.byteLength,
    });
  }
}
