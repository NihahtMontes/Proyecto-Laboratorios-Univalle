/**
 * MIG-001 F4: the shared contract (@lu/contracts) is hand-written, so this spec
 * pins it to the F3 backend. Type assertions fail compilation (ts-jest and
 * `tsc --noEmit`) when a DTO drifts; runtime checks cover routes and the shared
 * password policy.
 */
import 'reflect-metadata';
import { RequestMethod } from '@nestjs/common';
import {
  API_PREFIX,
  AUTH_ROUTES,
  LOGIN_PASSWORD_MAX_BYTES as CONTRACT_LOGIN_PASSWORD_MAX_BYTES,
  PASSWORD_POLICY,
  PERSON_ROUTES,
  PROFILE_PHOTO,
  USER_ROUTES,
  passwordPolicyViolations,
  type AuthSessionResponse as ContractAuthSession,
  type AddMembershipInput,
  type AdminResetPasswordInput,
  type ChangeMembershipRoleInput,
  type ChangeMembershipStatusInput,
  type ChangeMembershipValidityInput,
  type ChangePasswordRequest,
  type CreateManagedUserGlobalInput,
  type CreateManagedUserSiteInput,
  type LoginRequest,
  type ManagedUserPage as ContractUserPage,
  type ManagedUserRecord as ContractUserRecord,
  type PasswordPolicyViolation as ContractViolation,
  type ProfilePhotoViolation as ContractPhotoViolation,
  type ProfileRecord as ContractProfile,
  type RestoreAccountInput,
  type RestoreMembershipInput,
  type SessionPurpose as ContractPurpose,
  type SetAccountStatusInput,
  type SiteId,
  type UpdateProfileInput as ContractUpdateProfile,
  type UpdateUserGlobalFieldsInput,
  type UpdateWorkProfileInput,
} from '@lu/contracts';
import { AuthController } from '../src/auth/auth.controller.js';
import { LOGIN_PASSWORD_MAX_BYTES } from '../src/auth/auth.constants.js';
import type {
  AuthSessionResponse as BackendAuthSession,
  LoginDto,
} from '../src/auth/auth.types.js';
import type {
  PasswordPolicyViolation as BackendViolation,
  SessionPurpose as BackendPurpose,
} from '../src/identity/identity.contracts.js';
import { Utf8PasswordPolicy } from '../src/identity/password/password-policy.js';
import { PersonController } from '../src/people/person.controller.js';
import { ProfilePhotoController } from '../src/users/profile-photo/profile-photo.controller.js';
import {
  PROFILE_PHOTO_MAX_BYTES,
  PROFILE_PHOTO_MIME,
  type ProfilePhotoViolation as BackendPhotoViolation,
} from '../src/users/profile-photo/profile-photo.validator.js';
import { ProfileController, UserController } from '../src/users/user.controller.js';
import type {
  AddMembershipInput as BackendAddMembership,
  AdminResetPasswordInput as BackendAdminReset,
  ChangeRoleInput,
  ChangeStatusInput,
  ChangeValidityInput,
  CreateUserGlobalInput,
  CreateUserSiteAdminInput,
  ManagedUserPage as BackendUserPage,
  ManagedUserRecord as BackendUserRecord,
  ProfileRecord as BackendProfile,
  RestoreAccountInput as BackendRestoreAccount,
  RestoreMembershipInput as BackendRestoreMembership,
  SetAccountStatusInput as BackendSetAccountStatus,
  UpdateGlobalFieldsInput,
  UpdateProfileInput as BackendUpdateProfile,
  UpdateWorkProfileInput as BackendWorkProfile,
} from '../src/users/user.types.js';

// ---------------------------------------------------------------------------
// Type-level conformance. `Wire<T>` erases the SiteId brand (JSON carries a
// plain string) so response shapes can be compared in both directions.
// ---------------------------------------------------------------------------

type Wire<T> = T extends SiteId
  ? string
  : T extends readonly (infer U)[]
    ? readonly Wire<U>[]
    : T extends object
      ? { readonly [K in keyof T]: Wire<T[K]> }
      : T;
type Same<A, B> = [A] extends [B] ? ([B] extends [A] ? true : false) : false;
type Assignable<From, To> = [From] extends [To] ? true : false;
const assertTrue = <T extends true>(): T | undefined => undefined;

// Responses: identical wire shapes.
assertTrue<Same<Wire<ContractAuthSession>, Wire<BackendAuthSession>>>();
assertTrue<Same<ContractPurpose, BackendPurpose>>();
assertTrue<Same<Wire<ContractUserRecord>, Wire<BackendUserRecord>>>();
assertTrue<Same<Wire<ContractUserPage>, Wire<BackendUserPage>>>();
assertTrue<Same<Wire<ContractProfile>, Wire<BackendProfile>>>();
assertTrue<Same<ContractViolation, BackendViolation>>();
assertTrue<Same<ContractPhotoViolation, BackendPhotoViolation>>();

// Requests: every contract payload is accepted by the backend input model.
assertTrue<
  Assignable<
    Omit<LoginRequest, 'rememberMe' | 'activeSiteId'>,
    Pick<LoginDto, 'loginIdentifier' | 'password'>
  >
>();
assertTrue<Assignable<CreateManagedUserGlobalInput, CreateUserGlobalInput>>();
assertTrue<Assignable<CreateManagedUserSiteInput, CreateUserSiteAdminInput>>();
assertTrue<Assignable<UpdateUserGlobalFieldsInput, UpdateGlobalFieldsInput>>();
assertTrue<Assignable<AdminResetPasswordInput, BackendAdminReset>>();
assertTrue<Assignable<SetAccountStatusInput, BackendSetAccountStatus>>();
assertTrue<Assignable<RestoreAccountInput, BackendRestoreAccount>>();
assertTrue<Assignable<AddMembershipInput, BackendAddMembership>>();
assertTrue<Assignable<ChangeMembershipRoleInput, ChangeRoleInput>>();
assertTrue<Assignable<ChangeMembershipStatusInput, ChangeStatusInput>>();
assertTrue<Assignable<ChangeMembershipValidityInput, ChangeValidityInput>>();
assertTrue<Assignable<UpdateWorkProfileInput, BackendWorkProfile>>();
assertTrue<Assignable<RestoreMembershipInput, BackendRestoreMembership>>();
assertTrue<Assignable<ContractUpdateProfile, BackendUpdateProfile>>();
// The identity card is not self-editable over HTTP (controller allowlist).
assertTrue<Same<'identityCard' extends keyof ContractUpdateProfile ? true : false, false>>();
assertTrue<Same<keyof ChangePasswordRequest, 'currentPassword' | 'newPassword'>>();

// ---------------------------------------------------------------------------
// Route conformance against the Nest controller metadata.
// ---------------------------------------------------------------------------

type Controller = abstract new (...args: never[]) => object;

function routesOf(...controllers: Controller[]): Set<string> {
  const routes = new Set<string>();
  for (const controller of controllers) {
    const base = String(Reflect.getMetadata('path', controller) ?? '/');
    const proto = controller.prototype as Record<string, unknown>;
    for (const name of Object.getOwnPropertyNames(proto)) {
      const handler = proto[name];
      if (typeof handler !== 'function' || name === 'constructor') continue;
      const method = Reflect.getMetadata('method', handler) as RequestMethod | undefined;
      const path = Reflect.getMetadata('path', handler) as string | undefined;
      if (method === undefined || path === undefined) continue;
      const full = `/${[base, path]
        .map((part) => part.replace(/^\/|\/$/g, ''))
        .filter(Boolean)
        .join('/')}`;
      routes.add(`${RequestMethod[method]} ${full}`);
    }
  }
  return routes;
}

const U = ':id';
const S = ':siteId';

function strip(path: string): string {
  expect(path.startsWith(API_PREFIX)).toBe(true);
  return path.slice(API_PREFIX.length);
}

describe('MIG-001 F4 contract conformance with the F3 backend', () => {
  const backend = routesOf(
    AuthController,
    UserController,
    ProfileController,
    ProfilePhotoController,
    PersonController,
  );

  it('every contract route used by the API client exists in the backend', () => {
    const expected: Array<[string, string]> = [
      ['GET', AUTH_ROUTES.csrf],
      ['POST', AUTH_ROUTES.login],
      ['GET', AUTH_ROUTES.session],
      ['PUT', AUTH_ROUTES.activeSite],
      ['POST', AUTH_ROUTES.password],
      ['POST', AUTH_ROUTES.logout],
      ['GET', USER_ROUTES.users],
      ['POST', USER_ROUTES.users],
      ['GET', USER_ROUTES.user(U)],
      ['DELETE', USER_ROUTES.user(U)],
      ['PUT', USER_ROUTES.globalFields(U)],
      ['POST', USER_ROUTES.adminResetPassword(U)],
      ['PUT', USER_ROUTES.accountStatus(U)],
      ['POST', USER_ROUTES.restore(U)],
      ['POST', USER_ROUTES.memberships(U)],
      ['DELETE', USER_ROUTES.membership(U, S)],
      ['PUT', USER_ROUTES.membershipRole(U, S)],
      ['PUT', USER_ROUTES.membershipStatus(U, S)],
      ['PUT', USER_ROUTES.membershipValidity(U, S)],
      ['PUT', USER_ROUTES.membershipWorkProfile(U, S)],
      ['POST', USER_ROUTES.membershipRestore(U, S)],
      ['GET', USER_ROUTES.userPhoto(U)],
      ['PUT', USER_ROUTES.userPhoto(U)],
      ['DELETE', USER_ROUTES.userPhoto(U)],
      ['GET', USER_ROUTES.profile],
      ['PUT', USER_ROUTES.profile],
      ['GET', USER_ROUTES.profilePhoto],
      ['PUT', USER_ROUTES.profilePhoto],
      ['DELETE', USER_ROUTES.profilePhoto],
      ['GET', PERSON_ROUTES.people],
      ['POST', PERSON_ROUTES.people],
      ['GET', PERSON_ROUTES.person(':id' as unknown as number)],
      ['PUT', PERSON_ROUTES.person(':id' as unknown as number)],
      ['DELETE', PERSON_ROUTES.person(':id' as unknown as number)],
    ];
    const missing = expected
      .map(([method, path]) => `${method} ${decodeURIComponent(strip(path))}`)
      .filter((route) => !backend.has(route));
    expect(missing).toEqual([]);
  });

  it('the shared password policy returns exactly the backend violations', () => {
    const policy = new Utf8PasswordPolicy();
    const vectors = [
      '',
      'Valid-password-1',
      'Aa1!Aa1!Aa1',
      'aaaaaaaaaaaa',
      'lowercase-only-1',
      'UPPERCASE-ONLY-1',
      'No-digits-here!',
      'NoSymbolsHere12',
      ' Abcdefghij1 ',
      'Ñandú-Árbol-9x',
      'Aa1!' + '\u{1F600}'.repeat(8),
      'Aa1!' + '\u{1F600}'.repeat(17),
      'Aa1!' + '\u{1F600}'.repeat(18),
      'Aa1!' + 'é'.repeat(34),
      'Aa1!' + 'é'.repeat(35),
      'Aa1!bcdefgh\uD800',
      '١٢٣Abc!defghi',
      'x'.repeat(80),
    ];
    for (const value of vectors) {
      expect({ value, violations: [...passwordPolicyViolations(value)] }).toEqual({
        value,
        violations: [...policy.validate(value)],
      });
    }
    expect(PASSWORD_POLICY.maxUtf8Bytes).toBe(72);
  });

  it('login transport bound and profile photo limits match the backend', () => {
    expect(CONTRACT_LOGIN_PASSWORD_MAX_BYTES).toBe(LOGIN_PASSWORD_MAX_BYTES);
    expect(PROFILE_PHOTO.maxBytes).toBe(PROFILE_PHOTO_MAX_BYTES);
    expect([...PROFILE_PHOTO.mimeTypes].sort()).toEqual(Object.values(PROFILE_PHOTO_MIME).sort());
  });
});
