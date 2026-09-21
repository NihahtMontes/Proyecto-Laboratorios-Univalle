declare const __siteIdBrand: unique symbol;

export type SiteId = string & { readonly [__siteIdBrand]: 'SiteId' };

export enum SiteState {
  Provisioning = 'provisioning',
  Active = 'active',
  Migrating = 'migrating',
  Degraded = 'degraded',
  Disabled = 'disabled',
}

export enum SiteRole {
  Administrador = 'Administrador',
  Supervisor = 'Supervisor',
}

export enum GlobalRole {
  SuperAdmin = 'SuperAdmin',
}

export interface SiteMembership {
  readonly siteId: SiteId;
  readonly siteName: string;
  readonly role: SiteRole;
  readonly state: SiteState;
}

export interface ActiveSiteSession {
  readonly userId: string;
  readonly displayName: string;
  readonly email: string;
  readonly activeSiteId: SiteId | null;
  readonly activeSiteName: string | null;
  readonly globalRole: GlobalRole | null;
  readonly memberships: readonly SiteMembership[];
}
