import type {
  EligibleSite,
  ManagedUserRecord,
  SiteId,
  SiteRole,
  UpdateUserGlobalFieldsInput,
} from '@lu/contracts';

/** MIG-001 F5 Users module model: routing and pure helpers (no components). */

/** Who is looking: derived from the canonical session and site context. */
export interface UsersViewer {
  readonly userId: string;
  readonly isSuperAdmin: boolean;
  readonly activeSiteId: SiteId | null;
  readonly activeSiteName: string | null;
  readonly siteRole: SiteRole | null;
  readonly eligibleSites: readonly EligibleSite[];
}

export type UsersRoute =
  | { readonly kind: 'index'; readonly tab: 'usuarios' | 'personas' }
  | { readonly kind: 'create' }
  | {
      readonly kind: 'details' | 'edit' | 'delete';
      readonly userId: string;
      readonly returnTo?: string;
    }
  | { readonly kind: 'profile' };

export type UsersRouteResolution = { readonly route: UsersRoute } | { readonly redirect: string };

/**
 * F1 §16 routes: `/Users/Details/:userId` (admin detail), `/Profile` (self).
 * `/Users/Details?id=` redirects to the admin path; `/Users/Details` to `/Profile`.
 */
export function resolveUsersRoute(route: string): UsersRouteResolution | null {
  const [path = '/', search = ''] = route.split('?');
  const params = new URLSearchParams(search);
  if (path === '/Profile') return { route: { kind: 'profile' } };
  if (path === '/Users/Index') {
    return {
      route: { kind: 'index', tab: params.get('tab') === 'personas' ? 'personas' : 'usuarios' },
    };
  }
  if (path === '/Users/Create') return { route: { kind: 'create' } };
  if (path === '/Users/Details') {
    const id = params.get('id');
    return { redirect: id ? `/Users/Details/${encodeURIComponent(id)}` : '/Profile' };
  }
  const match = /^\/Users\/(Details|Edit|Delete)\/([^/]+)$/.exec(path);
  if (match) {
    const kind = match[1] === 'Details' ? 'details' : match[1] === 'Edit' ? 'edit' : 'delete';
    const returnTo = params.get('returnUrl') ?? undefined;
    return { route: { kind, userId: decodeURIComponent(match[2]!), returnTo } };
  }
  return null;
}

export interface IdentityForm {
  firstName: string;
  lastName: string;
  secondLastName: string;
  identityCard: string;
  email: string;
  phoneNumber: string;
}

export function globalFieldChanges(
  user: ManagedUserRecord,
  form: IdentityForm,
): UpdateUserGlobalFieldsInput {
  const changes: {
    -readonly [K in keyof UpdateUserGlobalFieldsInput]: UpdateUserGlobalFieldsInput[K];
  } = {};
  if (form.email.trim() !== user.email) changes.email = form.email.trim();
  if (form.firstName.trim() !== user.firstName) changes.firstName = form.firstName.trim();
  if (form.lastName.trim() !== user.lastName) changes.lastName = form.lastName.trim();
  if ((form.secondLastName.trim() || null) !== user.secondLastName) {
    changes.secondLastName = form.secondLastName.trim() || null;
  }
  if (form.identityCard.trim() !== user.identityCard)
    changes.identityCard = form.identityCard.trim();
  if (form.phoneNumber.trim() !== user.phoneNumber) changes.phoneNumber = form.phoneNumber.trim();
  return changes;
}
