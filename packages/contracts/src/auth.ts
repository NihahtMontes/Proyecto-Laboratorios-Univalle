import type { ActiveSiteSession, SiteId } from './site.js';
import { API_PREFIX } from './healthz.js';

export const AUTH_ROUTES = {
  csrf: `${API_PREFIX}/auth/csrf`,
  login: `${API_PREFIX}/auth/login`,
  session: `${API_PREFIX}/auth/session`,
  activeSite: `${API_PREFIX}/auth/session/active-site`,
  logout: `${API_PREFIX}/auth/logout`,
} as const;

export interface CsrfResponse {
  readonly csrfToken: string;
}

export interface LoginRequest {
  readonly email: string;
  readonly password: string;
  readonly rememberMe?: boolean;
  readonly activeSiteId?: SiteId | null;
}

export interface SetActiveSiteRequest {
  readonly activeSiteId: SiteId;
}

export type AuthSessionResponse = ActiveSiteSession;
