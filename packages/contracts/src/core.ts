import type { GlobalRole, SiteId, SiteRole } from './site.js';
import { API_PREFIX } from './healthz.js';

export const CORE_ROUTES = {
  siteContext: `${API_PREFIX}/context`,
} as const;

/**
 * Safe request context returned to the browser. It intentionally contains no
 * host, DSN, secret reference, writer label or connection string.
 */
export interface SiteRequestContext {
  readonly correlationId: string;
  readonly userId: string;
  readonly siteId: SiteId;
  readonly siteName: string;
  readonly siteRole: SiteRole;
  readonly globalRole: GlobalRole | null;
}
