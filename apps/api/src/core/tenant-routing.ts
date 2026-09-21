import { Inject, Injectable } from '@nestjs/common';
import type { SiteId, SiteRequestContext } from '@lu/contracts';
import { AUTH_PG_POOL } from '../auth/auth.constants.js';
import type { IPgPool } from '../auth/auth.pg-pool.js';
import { WriterLabel } from '../governance/writer-label.js';

export const TENANT_ROUTE_CATALOG = Symbol('TENANT_ROUTE_CATALOG');

export type TenantRouteState = 'active' | 'migrating' | 'degraded' | 'disabled';

/** Internal control-plane record. Never serialize this type into an HTTP response. */
export interface TenantRouteRecord {
  readonly siteId: SiteId;
  readonly state: TenantRouteState;
  readonly runtimeSecretReference: string;
  readonly writerLabel: WriterLabel;
}

export interface TenantRouteCatalog {
  findBySiteId(siteId: SiteId): Promise<TenantRouteRecord | null>;
}

export interface ResolvedTenantRoute {
  readonly siteId: SiteId;
  readonly runtimeSecretReference: string;
  readonly writerLabel: WriterLabel;
}

export class TenantRoutingError extends Error {
  constructor() {
    super('Tenant route is unavailable.');
    this.name = 'TenantRoutingError';
  }
}

/** Default until a control-plane route catalog is provisioned for a capability. */
@Injectable()
export class UnconfiguredTenantRouteCatalog implements TenantRouteCatalog {
  findBySiteId(): Promise<null> {
    return Promise.resolve(null);
  }
}

interface DbTenantRouteRow {
  readonly site_id: string;
  readonly runtime_secret_reference: string;
  readonly writer_label: string;
  readonly state: string;
}

/**
 * Reads routing metadata from the control plane using the same restricted
 * PostgreSQL transport as Auth. It never accepts a DSN or tenant identifier
 * from the browser and fails closed when a row is malformed or absent.
 */
@Injectable()
export class PostgresTenantRouteCatalog implements TenantRouteCatalog {
  constructor(@Inject(AUTH_PG_POOL) private readonly pool: IPgPool) {}

  async findBySiteId(siteId: SiteId): Promise<TenantRouteRecord | null> {
    const result = await this.pool.query<DbTenantRouteRow>(
      `SELECT site_id, runtime_secret_reference, writer_label, state
         FROM public.lu_tenant_route
        WHERE site_id = $1
        LIMIT 1`,
      [siteId],
    );
    const row = result.rows[0];
    if (row === undefined) return null;

    const state = row.state as TenantRouteState;
    const writerLabel = row.writer_label as WriterLabel;
    if (
      row.site_id !== siteId ||
      !['active', 'migrating', 'degraded', 'disabled'].includes(state) ||
      !Object.values(WriterLabel).includes(writerLabel)
    ) {
      return null;
    }

    return {
      siteId,
      state,
      runtimeSecretReference: row.runtime_secret_reference,
      writerLabel,
    };
  }
}

function isValidSecretReference(value: string): boolean {
  return /^[A-Za-z][A-Za-z0-9._/-]{2,127}$/.test(value) && !value.includes('..');
}

@Injectable()
export class TenantRouter {
  constructor(@Inject(TENANT_ROUTE_CATALOG) private readonly catalog: TenantRouteCatalog) {}

  /**
   * The only routing input is the server-authenticated site context. Browser
   * headers, query strings, DTO fields and connection values are not accepted.
   */
  async resolve(context: SiteRequestContext): Promise<ResolvedTenantRoute> {
    const route = await this.catalog.findBySiteId(context.siteId);
    if (
      route === null ||
      route.siteId !== context.siteId ||
      route.state !== 'active' ||
      !isValidSecretReference(route.runtimeSecretReference)
    ) {
      throw new TenantRoutingError();
    }
    return {
      siteId: route.siteId,
      runtimeSecretReference: route.runtimeSecretReference,
      writerLabel: route.writerLabel,
    };
  }
}
