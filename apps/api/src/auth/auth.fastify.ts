/**
 * Minimal structural interfaces for Fastify request/response objects used by the
 * auth slice. Avoids a direct dependency on the `fastify` package types while
 * remaining compatible with the runtime objects returned by NestJS FastifyAdapter.
 */
import type { SiteRequestContext } from '@lu/contracts';
import type { RequestIdentity } from '../identity/identity.contracts.js';

export interface FastifyRequest {
  readonly method: string;
  readonly headers: Record<string, string | string[] | undefined>;
  readonly socket?: { readonly remoteAddress?: string };
  readonly body?: unknown;
  authAllowedOrigin?: string;
  correlationId?: string;
  siteContext?: SiteRequestContext;
  /**
   * Populated by GlobalSessionGuard / SiteContextGuard after the session and
   * user have been revalidated. Restricted sessions (password_change,
   * site_selection) intentionally leave this undefined: those sessions are
   * never allowed to reach tenant/global endpoints.
   */
  identity?: RequestIdentity;
}

export interface FastifyReply {
  header(name: string, value: string | number): this;
  status(code: number): this;
  send(payload?: unknown): void;
}
