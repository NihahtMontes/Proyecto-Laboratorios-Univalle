import {
  useCallback,
  useEffect,
  useId,
  useRef,
  useState,
  type FormEvent,
  type ReactElement,
  type ReactNode,
} from 'react';
import type { ProfilePhotoUpload } from '@lu/api-client';
import {
  PASSWORD_POLICY,
  PROFILE_PHOTO,
  passwordPolicyViolations,
  SiteRole,
  type AccountStatus,
  type AddMembershipInput,
  type AdminResetPasswordInput,
  type AuthSessionResponse,
  type ChangeMembershipRoleInput,
  type ChangeMembershipStatusInput,
  type ChangeMembershipValidityInput,
  type ChangePasswordRequest,
  type CreateManagedUserInput,
  type ManagedUserPage,
  type ManagedUserQuery,
  type ManagedUserRecord,
  type MembershipProjection,
  type MembershipRole,
  type PersonStatus,
  type ProfilePictureRef,
  type ProfileRecord,
  type RestoreAccountInput,
  type RestoreMembershipInput,
  type SetAccountStatusInput,
  type SiteId,
  type UpdateProfileInput,
  type UpdateUserGlobalFieldsInput,
  type UpdateWorkProfileInput,
} from '@lu/contracts';
import { describeApiError, isApiError, passwordViolationText } from './apiErrors';
import { PeoplePanel, type PeopleApi } from './PeoplePanel';
import {
  globalFieldChanges,
  type IdentityForm,
  type UsersRoute,
  type UsersViewer,
} from './usersModel';

export type { UsersRoute, UsersViewer } from './usersModel';
import { photoFileProblem, photoUpload, usePhotoUrl } from './photo';
import { ProfilePhoto } from './ProfilePhoto';
import { FilterSelect, LegacyPagination, SmartIndexZone, StatusBadge } from './legacyUi';

/**
 * MIG-001 F5 Users module on the canonical F4 client. Identity is global;
 * role/status/validity/work profile belong to a site membership. SuperAdmin is
 * never granted or revoked here (CLI-only). The server stays authoritative:
 * the UI only hides actions the contract makes impossible.
 */

export interface UsersApi {
  listUsers(query?: ManagedUserQuery): Promise<ManagedUserPage>;
  userDetails(userId: string): Promise<ManagedUserRecord>;
  createManagedUser(input: CreateManagedUserInput): Promise<ManagedUserRecord>;
  updateUserGlobalFields(
    userId: string,
    input: UpdateUserGlobalFieldsInput,
  ): Promise<ManagedUserRecord>;
  adminResetPassword(userId: string, input: AdminResetPasswordInput): Promise<void>;
  setAccountStatus(userId: string, input: SetAccountStatusInput): Promise<void>;
  restoreAccount(userId: string, input?: RestoreAccountInput): Promise<void>;
  deleteAccount(userId: string): Promise<void>;
  addMembership(userId: string, input: AddMembershipInput): Promise<void>;
  changeMembershipRole(
    userId: string,
    siteId: string,
    input: ChangeMembershipRoleInput,
  ): Promise<void>;
  changeMembershipStatus(
    userId: string,
    siteId: string,
    input: ChangeMembershipStatusInput,
  ): Promise<void>;
  changeMembershipValidity(
    userId: string,
    siteId: string,
    input: ChangeMembershipValidityInput,
  ): Promise<void>;
  updateMembershipWorkProfile(
    userId: string,
    siteId: string,
    input: UpdateWorkProfileInput,
  ): Promise<void>;
  revokeMembership(userId: string, siteId: string): Promise<void>;
  restoreMembership(userId: string, siteId: string, input?: RestoreMembershipInput): Promise<void>;
  userPhoto(userId: string): Promise<Blob>;
  uploadUserPhoto(userId: string, upload: ProfilePhotoUpload): Promise<ProfilePictureRef>;
  deleteUserPhoto(userId: string): Promise<void>;
}

export interface ProfileApi {
  ownProfile(): Promise<ProfileRecord>;
  updateOwnProfile(input: UpdateProfileInput): Promise<ProfileRecord>;
  ownPhoto(): Promise<Blob>;
  uploadOwnPhoto(upload: ProfilePhotoUpload): Promise<ProfilePictureRef>;
  deleteOwnPhoto(): Promise<void>;
  changePassword(input: ChangePasswordRequest): Promise<AuthSessionResponse>;
  currentSession(): Promise<AuthSessionResponse>;
}

const ACCOUNT_STATUS: Record<AccountStatus, string> = {
  active: 'Activo',
  inactive: 'Inactivo',
  deleted: 'Eliminado',
};
const MEMBERSHIP_STATUS: Record<MembershipProjection['status'], string> = {
  active: 'Activa',
  suspended: 'Suspendida',
  revoked: 'Revocada',
};
const ACCOUNT_TO_PERSON: Record<AccountStatus, PersonStatus> = {
  active: 0,
  inactive: 1,
  deleted: 2,
};

function formatDate(value: string | null | undefined, withTime = false): string {
  if (!value) return 'Sin fecha registrada';
  const date = new Date(value);
  if (Number.isNaN(date.getTime())) return value;
  const pad = (n: number): string => String(n).padStart(2, '0');
  const day = `${pad(date.getDate())}/${pad(date.getMonth() + 1)}/${date.getFullYear()}`;
  return withTime ? `${day} ${pad(date.getHours())}:${pad(date.getMinutes())}` : day;
}

/**
 * Calendar dates (hire date, validity) are rendered from their YYYY-MM-DD part,
 * as the date inputs edit them; parsing them as instants shifts the day west of UTC.
 */
function formatDay(value: string): string {
  const match = /^(\d{4})-(\d{2})-(\d{2})/.exec(value);
  return match ? `${match[3]}/${match[2]}/${match[1]}` : formatDate(value);
}

function orFallback(value: string | null | undefined, fallback = 'Sin dato'): string {
  return value && value.trim() !== '' ? value : fallback;
}

function viewerMembership(
  user: Pick<ManagedUserRecord, 'memberships'>,
  viewer: UsersViewer,
): MembershipProjection | undefined {
  return (
    user.memberships.find((m) => m.siteId === viewer.activeSiteId) ??
    (user.memberships.length === 1 ? user.memberships[0] : undefined)
  );
}

/** F1 §11 effective ASP label, preferring the active-site membership projection. */
function effectiveStatus(user: ManagedUserRecord, viewer: UsersViewer): string {
  if (user.accountStatus !== 'active') return ACCOUNT_STATUS[user.accountStatus];
  return viewerMembership(user, viewer)?.effectiveStatus ?? 'Activo';
}

/** A site Administrador may see but never administer a SuperAdmin (F1 §8). */
function canAdminister(user: ManagedUserRecord, viewer: UsersViewer): boolean {
  return viewer.isSuperAdmin || !user.isSuperAdmin;
}

/** F1-D009: no role/status/revoke on the membership of one's own active site. */
function isOwnActiveMembership(
  user: ManagedUserRecord,
  membership: MembershipProjection,
  viewer: UsersViewer,
): boolean {
  return user.id === viewer.userId && membership.siteId === viewer.activeSiteId;
}

function Alerts({
  feedback,
  warning,
  error,
}: {
  readonly feedback?: string | null;
  readonly warning?: string | null;
  readonly error?: string | null;
}): ReactElement {
  return (
    <>
      {feedback ? (
        <div className="alert alert-success shadow-sm border-0 small" role="status">
          <i className="fas fa-check-circle mr-1" aria-hidden="true" /> {feedback}
        </div>
      ) : null}
      {warning ? (
        <div className="alert alert-warning shadow-sm border-0 small" role="status">
          <i className="fas fa-exclamation-triangle mr-1" aria-hidden="true" /> {warning}
        </div>
      ) : null}
      {error ? (
        <div className="alert alert-danger shadow-sm border-0 small" role="alert">
          {error}
        </div>
      ) : null}
    </>
  );
}

function PasswordHint({
  password,
  quiet = false,
}: {
  readonly password: string;
  /** Optional password: show the policy only once something was typed. */
  readonly quiet?: boolean;
}): ReactElement | null {
  if (quiet && password === '') return null;
  const violations = password === '' ? [] : passwordPolicyViolations(password);
  if (password !== '' && violations.length > 0) {
    return (
      <small className="text-danger small font-weight-bold d-block mt-1">
        La contraseña {passwordViolationText(violations)}.
      </small>
    );
  }
  return (
    <small className="text-muted italic d-block mt-1" style={{ fontSize: '0.75rem' }}>
      {password !== '' ? 'La contraseña cumple la política. ' : ''}
      Mínimo {PASSWORD_POLICY.minCodePoints} caracteres, con mayúsculas, minúsculas, números y
      símbolos.
    </small>
  );
}

/** Legacy `form-group` + `control-label font-weight-bold small uppercase text-muted`. */
function FormField({
  label,
  required = false,
  labelClass = 'text-muted',
  className = 'form-group mb-3',
  help,
  children,
}: {
  readonly label: string;
  readonly required?: boolean;
  readonly labelClass?: string;
  readonly className?: string;
  readonly help?: ReactNode;
  readonly children: (id: string) => ReactNode;
}): ReactElement {
  const id = useId();
  return (
    <div className={className}>
      <label
        htmlFor={id}
        className={`control-label font-weight-bold small uppercase ${labelClass}${required ? ' required' : ''}`}
      >
        {label}
      </label>
      {children(id)}
      {help}
    </div>
  );
}

function SectionTitle({
  icon,
  children,
  className = 'font-weight-bold text-info mb-3',
}: {
  readonly icon: string;
  readonly children: ReactNode;
  readonly className?: string;
}): ReactElement {
  return (
    <h5 className={className}>
      <i className={`${icon} mr-2`} aria-hidden="true" /> {children}
    </h5>
  );
}

/** Details `user-detail-label` / `user-detail-value` pair. */
function DetailItem({
  label,
  value,
  className = 'col-md-4 mb-3',
  children,
}: {
  readonly label: string;
  readonly value: ReactNode;
  readonly className?: string;
  readonly children?: ReactNode;
}): ReactElement {
  return (
    <div className={className}>
      <span className="user-detail-label">{label}</span>
      <div className="user-detail-value">{value}</div>
      {children}
    </div>
  );
}

/** Legacy badge rule: success for "Activo", danger otherwise. */
function activeTone(label: string): 'success' | 'danger' {
  return label === 'Activo' ? 'success' : 'danger';
}

const MEMBERSHIP_TONE: Record<MembershipProjection['status'], 'success' | 'warning' | 'danger'> = {
  active: 'success',
  suspended: 'warning',
  revoked: 'danger',
};

function nameInitials(fullName: string): string {
  return fullName
    .trim()
    .split(/\s+/)
    .slice(0, 2)
    .map((part) => part.charAt(0).toUpperCase())
    .join('');
}

/** Index row avatar: 36px photo or `btn btn-circle btn-info` initials. */
function UserRowAvatar({
  api,
  user,
}: {
  readonly api: Pick<UsersApi, 'userPhoto'>;
  readonly user: ManagedUserRecord;
}): ReactElement {
  const url = usePhotoUrl(user.profilePicture, () => api.userPhoto(user.id));
  return url ? (
    <img src={url} alt={user.fullName} className="rounded-circle shadow-sm user-row-avatar" />
  ) : (
    <span
      className="btn btn-circle btn-info font-weight-bold text-white shadow-sm"
      aria-hidden="true"
    >
      {user.initials || 'US'}
    </span>
  );
}

const PASSWORD_CHARSETS = [
  'ABCDEFGHJKLMNPQRSTUVWXYZ',
  'abcdefghijkmnopqrstuvwxyz',
  '23456789',
  '!@#$%&*?-_+=',
] as const;

/** Generates a policy-compliant password (legacy "generar automática"). */
function generatePassword(length = 16): string {
  const random = new Uint32Array(length);
  crypto.getRandomValues(random);
  const all = PASSWORD_CHARSETS.join('');
  const chars = Array.from(random, (value, index) => {
    const set = index < PASSWORD_CHARSETS.length ? PASSWORD_CHARSETS[index]! : all;
    return set[value % set.length]!;
  });
  for (let i = chars.length - 1; i > 0; i -= 1) {
    const j = random[i]! % (i + 1);
    [chars[i], chars[j]] = [chars[j]!, chars[i]!];
  }
  return chars.join('');
}

// ---------------------------------------------------------------------------
// Module entry: one mounted component for every /Users/* and /Profile route so
// a flash message survives navigation (TempData equivalent).
// ---------------------------------------------------------------------------

export function UsersModule({
  api,
  peopleApi,
  viewer,
  route,
  navigate,
}: {
  readonly api: UsersApi;
  readonly peopleApi: PeopleApi | null;
  readonly viewer: UsersViewer;
  readonly route: UsersRoute;
  readonly navigate: (path: string) => void;
}): ReactElement | null {
  const [flash, setFlash] = useState<{ readonly text: string; readonly warning: boolean } | null>(
    null,
  );
  const go = useCallback(
    (path: string, text?: string, warning = false) => {
      setFlash(text ? { text, warning } : null);
      navigate(path);
    },
    [navigate],
  );
  const flashAlert = flash ? (
    <Alerts
      feedback={flash.warning ? null : flash.text}
      warning={flash.warning ? flash.text : null}
    />
  ) : null;

  switch (route.kind) {
    case 'index':
      return (
        <UsersIndex
          api={api}
          peopleApi={peopleApi}
          viewer={viewer}
          tab={route.tab}
          go={go}
          flash={flashAlert}
        />
      );
    case 'create':
      return <UserCreate api={api} viewer={viewer} go={go} />;
    case 'details':
      return (
        <UserDetails api={api} viewer={viewer} userId={route.userId} go={go} flash={flashAlert} />
      );
    case 'edit':
      return (
        <UserEdit
          key={route.userId}
          api={api}
          viewer={viewer}
          userId={route.userId}
          returnTo={route.returnTo}
          go={go}
        />
      );
    case 'delete':
      return <UserDelete api={api} viewer={viewer} userId={route.userId} go={go} />;
    default:
      return null;
  }
}

type Go = (path: string, text?: string, warning?: boolean) => void;

// ---------------------------------------------------------------------------
// Index: "Cuentas de Acceso" + "Directorio de Personal" tabs (F1-D016).
// ---------------------------------------------------------------------------

function UsersIndex({
  api,
  peopleApi,
  viewer,
  tab,
  go,
  flash,
}: {
  readonly api: UsersApi;
  readonly peopleApi: PeopleApi | null;
  readonly viewer: UsersViewer;
  readonly tab: 'usuarios' | 'personas';
  readonly go: Go;
  readonly flash: ReactElement | null;
}): ReactElement {
  const [searchInput, setSearchInput] = useState('');
  const [searchTerm, setSearchTerm] = useState('');
  const [statusFilter, setStatusFilter] = useState<AccountStatus | ''>('');
  const [pageIndex, setPageIndex] = useState(1);
  const [page, setPage] = useState<ManagedUserPage | null>(null);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);

  // Only the newest request may commit: an older, slower response must never
  // overwrite a newer search/filter result (same guard as PeoplePanel).
  const latestRequest = useRef(0);
  const load = useCallback(async () => {
    const request = ++latestRequest.current;
    setLoading(true);
    try {
      const next = await api.listUsers({
        currentPage: pageIndex,
        statusFilter: statusFilter === '' ? undefined : statusFilter,
        searchTerm: searchTerm === '' ? undefined : searchTerm,
      });
      if (request !== latestRequest.current) return;
      setPage(next);
      setError(null);
    } catch (caught) {
      if (request !== latestRequest.current) return;
      setError(describeApiError(caught, 'No fue posible cargar las cuentas de acceso.'));
    } finally {
      if (request === latestRequest.current) setLoading(false);
    }
  }, [api, pageIndex, searchTerm, statusFilter]);
  useEffect(() => {
    if (tab === 'usuarios') void load();
  }, [load, tab]);

  const hasFilters = searchTerm !== '' || statusFilter !== '';
  const peopleAllowed =
    peopleApi !== null &&
    viewer.activeSiteId !== null &&
    viewer.siteRole === SiteRole.Administrador;

  function clearFilters(): void {
    setSearchInput('');
    setSearchTerm('');
    setStatusFilter('');
    setPageIndex(1);
  }

  const filterZone = (extra: ReactNode, count: ReactNode): ReactElement => (
    <SmartIndexZone
      searchLabel={tab === 'usuarios' ? 'Buscar usuario' : 'Buscar persona'}
      placeholder={
        tab === 'usuarios'
          ? 'Buscar por nombre, usuario o C.I.'
          : 'Buscar por nombre, correo o código'
      }
      value={searchInput}
      active={searchTerm !== ''}
      onChange={setSearchInput}
      onSubmit={() => {
        setSearchTerm(searchInput.trim());
        setPageIndex(1);
      }}
      count={count}
    >
      <FilterSelect
        label="Filtrar por estado"
        value={statusFilter}
        onChange={(value) => {
          setStatusFilter(value as AccountStatus | '');
          setPageIndex(1);
        }}
      >
        <option value="">Estado</option>
        {Object.entries(ACCOUNT_STATUS).map(([key, value]) => (
          <option key={key} value={key}>
            {value}
          </option>
        ))}
      </FilterSelect>
      {extra}
      {hasFilters ? (
        <button
          type="button"
          className="btn btn-link btn-sm text-muted mb-2"
          title="Limpiar filtros"
          onClick={clearFilters}
        >
          <i className="fas fa-times mr-1" aria-hidden="true" /> Limpiar filtros
        </button>
      ) : null}
    </SmartIndexZone>
  );

  return (
    <section className="row users-module users-index" aria-label="Usuarios y roles">
      <div className="col-12">
        <div className="card shadow-sm border-0">
          <ul className="nav nav-tabs customtab px-4 pt-3 border-0" role="tablist">
            <li className="nav-item">
              <button
                type="button"
                role="tab"
                className={`nav-link font-weight-bold${tab === 'usuarios' ? ' active' : ''}`}
                aria-selected={tab === 'usuarios'}
                onClick={() => go('/Users/Index')}
              >
                <i className="fas fa-users-cog mr-2" aria-hidden="true" /> Cuentas de Acceso
              </button>
            </li>
            <li className="nav-item">
              <button
                type="button"
                role="tab"
                className={`nav-link font-weight-bold${tab === 'personas' ? ' active' : ''}`}
                aria-selected={tab === 'personas'}
                onClick={() => go('/Users/Index?tab=personas')}
              >
                <i className="fas fa-id-card mr-2" aria-hidden="true" /> Directorio de Personal
              </button>
            </li>
          </ul>

          <div className="tab-content border-top">
            <div className="tab-pane active" role="tabpanel">
              <div className="card-body">
                {flash}
                {tab === 'personas' ? (
                  peopleAllowed ? (
                    <PeoplePanel
                      api={peopleApi!}
                      sharedFilters={{
                        searchTerm: searchTerm === '' ? undefined : searchTerm,
                        statusFilter:
                          statusFilter === '' ? undefined : ACCOUNT_TO_PERSON[statusFilter],
                      }}
                      filterZone={filterZone}
                    />
                  ) : (
                    <div
                      className="alert alert-light border-0 small rounded p-3 mb-0 text-info italic shadow-none border-left border-info border-3"
                      role="status"
                    >
                      <i className="fas fa-info-circle mr-2" aria-hidden="true" />
                      El Directorio de Personal requiere una sede activa en la que tenga el rol
                      Administrador.
                    </div>
                  )
                ) : (
                  <>
                    <div className="d-md-flex align-items-center mb-4 pb-3 border-bottom">
                      <div>
                        <h3 className="card-title text-dark font-weight-bold mt-2 ml-2">
                          Control de Usuarios
                        </h3>
                        <h6 className="card-subtitle text-muted ml-2">
                          Administración de credenciales, roles y estatus de seguridad del sistema.
                        </h6>
                      </div>
                      <div className="ml-auto mt-3 mt-md-0">
                        <button
                          type="button"
                          className="btn bg-white border btn-rounded shadow-sm text-dark px-4 font-weight-bold"
                          onClick={() => go('/Users/Create')}
                        >
                          <i className="fas fa-plus-circle mr-1 text-info" aria-hidden="true" />{' '}
                          Vincular Usuario
                        </button>
                      </div>
                    </div>

                    {filterZone(
                      null,
                      <>
                        <strong className="text-dark">{page?.totalCount ?? 0}</strong> cuenta(s)
                      </>,
                    )}
                    <Alerts error={error} />

                    <div className="table-responsive">
                      <table className="table table-hover v-middle">
                        <thead className="bg-light">
                          <tr className="small uppercase font-weight-bold">
                            <th>Identidad / Usuario</th>
                            <th>Seguridad / Rol</th>
                            <th className="text-center">Estatus</th>
                            <th>Perfil Laboral</th>
                            <th>Registro</th>
                            <th className="text-center">Acciones</th>
                          </tr>
                        </thead>
                        <tbody>
                          {loading ? (
                            <tr>
                              <td colSpan={6} className="text-center py-5 text-muted">
                                Cargando cuentas…
                              </td>
                            </tr>
                          ) : page === null || page.items.length === 0 ? (
                            <tr>
                              <td colSpan={6} className="text-center py-5 text-muted italic">
                                <i
                                  className="fas fa-user-slash fa-3x mb-3 d-block opacity-2"
                                  aria-hidden="true"
                                />
                                No se encontraron cuentas bajo los criterios definidos.
                              </td>
                            </tr>
                          ) : (
                            page.items.map((item) => (
                              <UserRow
                                key={item.id}
                                api={api}
                                item={item}
                                viewer={viewer}
                                go={go}
                              />
                            ))
                          )}
                        </tbody>
                      </table>
                    </div>

                    {page ? (
                      <div className="mt-3">
                        <LegacyPagination
                          pageIndex={page.pageIndex}
                          totalPages={page.totalPages}
                          label="Paginación Usuarios"
                          onPage={setPageIndex}
                        />
                      </div>
                    ) : null}
                  </>
                )}
              </div>
            </div>
          </div>
        </div>
      </div>
    </section>
  );
}

function UserRow({
  api,
  item,
  viewer,
  go,
}: {
  readonly api: UsersApi;
  readonly item: ManagedUserRecord;
  readonly viewer: UsersViewer;
  readonly go: Go;
}): ReactElement {
  const membership = viewerMembership(item, viewer);
  const manageable = canAdminister(item, viewer);
  const status = effectiveStatus(item, viewer);
  const createdBy = item.createdBy
    ? nameInitials(item.createdBy.fullName)
    : item.createdBy === null
      ? 'Sist.'
      : '-';
  return (
    <tr>
      <td>
        <div className="d-flex align-items-center">
          <div className="m-r-10">
            <UserRowAvatar api={api} user={item} />
          </div>
          <div>
            <h6 className="m-b-0 font-weight-bold text-dark">{item.fullName}</h6>
            <span className="text-muted small">CI: {item.identityCard || '-'}</span>
          </div>
        </div>
      </td>
      <td>
        <div className="small">
          <div className="text-dark font-weight-bold">
            <i className="fas fa-shield-alt mr-1 text-info" aria-hidden="true" />{' '}
            {item.displayRole ?? 'Sin rol en la sede'}
          </div>
          <div className="text-muted italic">@{item.username}</div>
        </div>
      </td>
      <td className="text-center">
        <StatusBadge label={status} tone={activeTone(status)} />
      </td>
      <td>
        <div className="small">
          <div className="text-dark font-weight-bold">{membership?.position || '-'}</div>
          <div className="text-muted">{membership?.department || '-'}</div>
        </div>
      </td>
      <td>
        <div className="small">
          <div className="text-dark">
            <i className="far fa-calendar-alt text-muted mr-1" aria-hidden="true" />{' '}
            {formatDate(item.createdAt)}
          </div>
          <div className="text-muted">
            <i className="fas fa-user-check mr-1" aria-hidden="true" /> {createdBy}
          </div>
        </div>
      </td>
      <td className="text-center">
        <div className="btn-group">
          {manageable ? (
            <button
              type="button"
              className="btn btn-sm btn-outline-warning btn-rounded shadow-none"
              title="Editar"
              aria-label="Editar"
              onClick={() => go(`/Users/Edit/${item.id}`)}
            >
              <i className="fas fa-edit" aria-hidden="true" />
            </button>
          ) : null}
          <button
            type="button"
            className="btn btn-sm btn-outline-info btn-rounded mx-1 shadow-none"
            title="Detalles"
            aria-label="Detalles"
            onClick={() => go(`/Users/Details/${item.id}`)}
          >
            <i className="fas fa-search-plus" aria-hidden="true" />
          </button>
          {manageable && item.id !== viewer.userId ? (
            <button
              type="button"
              className="btn btn-sm btn-outline-danger btn-rounded shadow-none"
              title="Eliminar"
              aria-label="Eliminar"
              onClick={() => go(`/Users/Delete/${item.id}`)}
            >
              <i className="fas fa-trash" aria-hidden="true" />
            </button>
          ) : null}
        </div>
      </td>
    </tr>
  );
}

// ---------------------------------------------------------------------------
// Create: POST /users (shape chosen by caller) + explicit follow-up commands.
// ---------------------------------------------------------------------------

interface WorkForm {
  position: string;
  department: string;
  hireDate: string;
}

function WorkFields({
  form,
  onChange,
  disabled = false,
  departmentLabel = 'Departamento',
}: {
  readonly form: WorkForm;
  readonly onChange: (form: WorkForm) => void;
  readonly disabled?: boolean;
  readonly departmentLabel?: string;
}): ReactElement {
  return (
    <div className="row">
      <div className="col-md-6">
        <FormField label="Cargo">
          {(id) => (
            <input
              id={id}
              className="form-control shadow-none"
              maxLength={100}
              placeholder="Ej: Técnico II"
              disabled={disabled}
              value={form.position}
              onChange={(event) => onChange({ ...form, position: event.target.value })}
            />
          )}
        </FormField>
      </div>
      <div className="col-md-6">
        <FormField label={departmentLabel}>
          {(id) => (
            <input
              id={id}
              className="form-control shadow-none"
              maxLength={100}
              placeholder="Ej: Biomedicina"
              disabled={disabled}
              value={form.department}
              onChange={(event) => onChange({ ...form, department: event.target.value })}
            />
          )}
        </FormField>
      </div>
      <div className="col-md-6">
        <FormField label="Fecha de ingreso">
          {(id) => (
            <input
              id={id}
              className="form-control shadow-none"
              type="date"
              disabled={disabled}
              value={form.hireDate}
              onChange={(event) => onChange({ ...form, hireDate: event.target.value })}
            />
          )}
        </FormField>
      </div>
    </div>
  );
}

function workInput(form: WorkForm): UpdateWorkProfileInput {
  return {
    position: form.position.trim() || null,
    department: form.department.trim() || null,
    hireDate: form.hireDate || null,
  };
}

/** Create: input group with Ver/Ocultar and Generar Automática (legacy fa-eye / fa-magic). */
function PasswordField({
  label,
  value,
  onChange,
  required,
  tools = true,
  placeholder,
  help,
}: {
  readonly label: string;
  readonly value: string;
  readonly onChange: (value: string) => void;
  readonly required: boolean;
  readonly tools?: boolean;
  readonly placeholder?: string;
  readonly help?: ReactNode;
}): ReactElement {
  const [visible, setVisible] = useState(false);
  return (
    <FormField
      label={label}
      required={required}
      className={tools ? 'form-group mb-4' : 'form-group mb-0'}
      help={
        <>
          {help}
          <PasswordHint password={value} quiet={!required} />
        </>
      }
    >
      {(id) => (
        <div className={tools ? 'input-group drop-shadow' : undefined}>
          <input
            id={id}
            className="form-control shadow-none"
            type={visible ? 'text' : 'password'}
            autoComplete="new-password"
            required={required}
            placeholder={placeholder}
            value={value}
            onChange={(event) => onChange(event.target.value)}
          />
          {tools ? (
            <div className="input-group-append">
              <button
                type="button"
                className="btn btn-outline-secondary"
                title="Ver/Ocultar"
                aria-label={visible ? 'Ocultar contraseña' : 'Ver contraseña'}
                aria-pressed={visible}
                onClick={() => setVisible((v) => !v)}
              >
                <i className={`fas ${visible ? 'fa-eye-slash' : 'fa-eye'}`} aria-hidden="true" />
              </button>
              <button
                type="button"
                className="btn btn-dark"
                title="Generar Automática"
                aria-label="Generar contraseña"
                onClick={() => {
                  onChange(generatePassword());
                  setVisible(true);
                }}
              >
                <i className="fas fa-magic" aria-hidden="true" />
              </button>
            </div>
          ) : null}
        </div>
      )}
    </FormField>
  );
}

/** Legacy `custom-file` picker; the chosen file name replaces the placeholder label. */
function PhotoPicker({
  placeholder,
  fileName,
  onFile,
  preview,
}: {
  readonly placeholder: string;
  readonly fileName: string | null;
  readonly onFile: (file: File | null, input: HTMLInputElement) => void;
  readonly preview?: ReactNode;
}): ReactElement {
  return (
    <FormField
      label="Foto de Perfil"
      help={<small className="text-muted d-block mt-1">JPEG, PNG o WebP · máximo 5 MB</small>}
    >
      {(id) => (
        <>
          {preview}
          <div className="custom-file">
            <input
              id={id}
              type="file"
              className="custom-file-input"
              accept={PROFILE_PHOTO.mimeTypes.join(',')}
              onChange={(event) => onFile(event.target.files?.[0] ?? null, event.target)}
            />
            <label className="custom-file-label text-truncate" htmlFor={id}>
              {fileName ?? placeholder}
            </label>
          </div>
        </>
      )}
    </FormField>
  );
}

function RoleOptions(): ReactElement {
  return (
    <>
      <option value={SiteRole.Supervisor}>Supervisor</option>
      <option value={SiteRole.Administrador}>Administrador</option>
    </>
  );
}

const BLANK_IDENTITY: IdentityForm = {
  firstName: '',
  lastName: '',
  secondLastName: '',
  identityCard: '',
  email: '',
  phoneNumber: '',
};
const BLANK_WORK: WorkForm = { position: '', department: '', hireDate: '' };

function UserCreate({
  api,
  viewer,
  go,
}: {
  readonly api: UsersApi;
  readonly viewer: UsersViewer;
  readonly go: Go;
}): ReactElement {
  const [identity, setIdentity] = useState<IdentityForm>(BLANK_IDENTITY);
  const [work, setWork] = useState<WorkForm>(BLANK_WORK);
  const [username, setUsername] = useState('');
  const [password, setPassword] = useState('');
  const [role, setRole] = useState<MembershipRole>(SiteRole.Supervisor);
  const [siteId, setSiteId] = useState<string>(
    viewer.activeSiteId ?? viewer.eligibleSites[0]?.siteId ?? '',
  );
  const [photo, setPhoto] = useState<File | null>(null);
  const [busy, setBusy] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const superAdminWithoutSites = viewer.isSuperAdmin && viewer.eligibleSites.length === 0;

  async function submit(event: FormEvent<HTMLFormElement>): Promise<void> {
    event.preventDefault();
    if (busy) return;
    if (superAdminWithoutSites) return;
    setError(null);
    const violations = passwordPolicyViolations(password);
    if (violations.length > 0) {
      setError(`La contraseña ${passwordViolationText(violations)}.`);
      return;
    }
    const base = {
      username: username.trim(),
      email: identity.email.trim(),
      firstName: identity.firstName.trim(),
      lastName: identity.lastName.trim(),
      secondLastName: identity.secondLastName.trim() || null,
      identityCard: identity.identityCard.trim(),
      phoneNumber: identity.phoneNumber.trim(),
      password,
    };
    const input: CreateManagedUserInput = viewer.isSuperAdmin
      ? { ...base, memberships: siteId ? [{ siteId: siteId as SiteId, role }] : [] }
      : { ...base, role };
    setBusy(true);
    let created: ManagedUserRecord;
    try {
      created = await api.createManagedUser(input);
    } catch (caught) {
      setError(describeApiError(caught, 'No fue posible crear la cuenta.'));
      setBusy(false);
      return;
    }
    // Follow-up commands are separate F3 operations; report partial results truthfully.
    const pending: string[] = [];
    const membershipSite = viewer.isSuperAdmin ? siteId : viewer.activeSiteId;
    const wantsWork = work.position.trim() || work.department.trim() || work.hireDate;
    if (wantsWork && membershipSite) {
      try {
        await api.updateMembershipWorkProfile(created.id, membershipSite, workInput(work));
      } catch (caught) {
        pending.push(`perfil laboral (${describeApiError(caught, 'error')})`);
      }
    }
    if (photo) {
      try {
        await api.uploadUserPhoto(created.id, photoUpload(photo));
      } catch (caught) {
        pending.push(`foto (${describeApiError(caught, 'error')})`);
      }
    }
    setBusy(false);
    if (pending.length === 0) {
      go('/Users/Index', `Cuenta de usuario para '${created.fullName}' creada exitosamente.`);
    } else {
      go(
        `/Users/Details/${created.id}`,
        `Cuenta de '${created.fullName}' creada, pero no se aplicó: ${pending.join('; ')}.`,
        true,
      );
    }
  }

  const text = (
    key: keyof IdentityForm,
    placeholder: string,
    extra: { readonly required?: boolean; readonly type?: string; readonly maxLength: number },
  ) =>
    function Input(id: string): ReactElement {
      return (
        <input
          id={id}
          className="form-control"
          placeholder={placeholder}
          value={identity[key]}
          onChange={(event) => setIdentity({ ...identity, [key]: event.target.value })}
          {...extra}
        />
      );
    };

  return (
    <section className="row justify-content-center users-module" aria-label="Vincular usuario">
      <div className="col-lg-10 col-xl-9">
        <div className="card shadow-sm border-0">
          <div className="card-body p-4">
            <div className="d-flex align-items-center mb-4 pb-3 border-bottom">
              <div>
                <h4 className="card-title text-dark font-weight-bold">Alta de Cuenta de Usuario</h4>
                <h6 className="card-subtitle text-muted">
                  Defina credenciales seguras y privilegios de acceso para el personal técnico y
                  administrativo.
                </h6>
              </div>
              <div className="ml-auto mt-2 mt-md-0">
                <i className="fas fa-user-shield fa-2x text-info opacity-5" aria-hidden="true" />
              </div>
            </div>

            <form onSubmit={(event) => void submit(event)}>
              {superAdminWithoutSites ? (
                <div
                  className="alert alert-info shadow-sm border-0 small"
                  role="status"
                  data-testid="superadmin-no-sites-notice"
                >
                  <i className="fas fa-info-circle mr-1" aria-hidden="true" /> Para vincular un
                  usuario se requiere al menos una membresía de sede. Su cuenta SuperAdmin no tiene
                  sedes elegibles; solicite o asigne una membresía de sede antes de crear cuentas.
                </div>
              ) : null}
              <Alerts error={error} />
              <div className="row">
                <div className="col-md-6 border-right">
                  <SectionTitle icon="fas fa-address-card">Identidad del Usuario</SectionTitle>
                  <div className="row">
                    <div className="col-md-6">
                      <FormField label="Nombres" required>
                        {text('firstName', 'Ej: Juan Antonio', { required: true, maxLength: 100 })}
                      </FormField>
                    </div>
                    <div className="col-md-6">
                      <FormField label="Primer Apellido" required>
                        {text('lastName', 'Ej: Pérez', { required: true, maxLength: 100 })}
                      </FormField>
                    </div>
                  </div>
                  <div className="row">
                    <div className="col-md-6">
                      <FormField label="Segundo Apellido">
                        {text('secondLastName', 'Ej: García', { maxLength: 100 })}
                      </FormField>
                    </div>
                    <div className="col-md-6">
                      <FormField label="Cédula de Identidad" required>
                        {text('identityCard', '1234567', { required: true, maxLength: 20 })}
                      </FormField>
                    </div>
                  </div>
                  <FormField label="Correo Institucional" required>
                    {text('email', 'usuario@univalle.edu', {
                      required: true,
                      type: 'email',
                      maxLength: 256,
                    })}
                  </FormField>
                  <FormField label="Teléfono de Contacto" required>
                    {text('phoneNumber', '70000000', {
                      required: true,
                      type: 'tel',
                      maxLength: 30,
                    })}
                  </FormField>
                  <PhotoPicker
                    placeholder="Elegir archivo..."
                    fileName={photo?.name ?? null}
                    onFile={(file, input) => {
                      const problem = file ? photoFileProblem(file) : null;
                      setError(problem);
                      setPhoto(problem === null ? file : null);
                      if (problem !== null) input.value = '';
                    }}
                  />
                </div>

                <div className="col-md-6">
                  <SectionTitle icon="fas fa-lock">Seguridad y Privilegios</SectionTitle>
                  {viewer.isSuperAdmin ? (
                    <FormField label="Sede de la membresía" labelClass="text-info">
                      {(id) => (
                        <select
                          id={id}
                          className="form-control custom-select"
                          value={siteId}
                          onChange={(event) => setSiteId(event.target.value)}
                        >
                          {viewer.eligibleSites.length === 0 ? (
                            <option value="">Sin membresía de sede</option>
                          ) : null}
                          {viewer.eligibleSites.map((site) => (
                            <option key={site.siteId} value={site.siteId}>
                              {site.siteName}
                            </option>
                          ))}
                        </select>
                      )}
                    </FormField>
                  ) : null}
                  <FormField
                    label="Rol de Aplicación"
                    required
                    labelClass="text-info"
                    help={
                      viewer.isSuperAdmin ? null : (
                        <small className="text-muted d-block mt-1">
                          Membresía en {viewer.activeSiteName ?? 'la sede activa'}.
                        </small>
                      )
                    }
                  >
                    {(id) => (
                      <select
                        id={id}
                        className="form-control custom-select border-info"
                        value={role}
                        onChange={(event) => setRole(event.target.value as MembershipRole)}
                      >
                        <RoleOptions />
                      </select>
                    )}
                  </FormField>
                  <FormField
                    label="Nombre de Usuario (Login)"
                    required
                    labelClass="text-warning mb-2"
                    className="form-group mb-3 border-left border-warning pl-3 bg-light p-3 rounded shadow-none"
                  >
                    {(id) => (
                      <div className="input-group">
                        <div className="input-group-prepend">
                          <span className="input-group-text bg-white">
                            <i className="fas fa-user-tag text-muted" aria-hidden="true" />
                          </span>
                        </div>
                        <input
                          id={id}
                          className="form-control border-warning shadow-none"
                          placeholder="Ej: jperez"
                          required
                          maxLength={256}
                          autoComplete="off"
                          value={username}
                          onChange={(event) => setUsername(event.target.value)}
                        />
                      </div>
                    )}
                  </FormField>
                  <PasswordField
                    label="Contraseña de Acceso"
                    value={password}
                    onChange={setPassword}
                    required
                  />
                  <SectionTitle
                    icon="fas fa-briefcase"
                    className="font-weight-bold text-info mb-3 mt-4"
                  >
                    Perfil Laboral
                  </SectionTitle>
                  <WorkFields form={work} onChange={setWork} />
                </div>
              </div>

              <hr />

              <div className="form-actions text-right mt-4">
                <button
                  type="submit"
                  className="btn btn-info btn-rounded px-5 shadow-sm font-weight-bold py-2"
                  disabled={busy || superAdminWithoutSites}
                >
                  <i className="fas fa-save mr-1" aria-hidden="true" /> Crear Cuenta
                </button>
                <button
                  type="button"
                  className="btn btn-outline-secondary btn-rounded ml-2 py-2 px-4 font-weight-bold"
                  onClick={() => go('/Users/Index')}
                >
                  Cancelar
                </button>
              </div>
            </form>
          </div>
        </div>
      </div>
    </section>
  );
}

// ---------------------------------------------------------------------------
// Shared loader for one administrative record.
// ---------------------------------------------------------------------------

function useManagedUser(
  api: UsersApi,
  userId: string,
): {
  readonly user: ManagedUserRecord | null;
  readonly error: string | null;
  readonly reload: () => Promise<void>;
  readonly setUser: (user: ManagedUserRecord) => void;
} {
  const [user, setUser] = useState<ManagedUserRecord | null>(null);
  const [error, setError] = useState<string | null>(null);
  const reload = useCallback(async () => {
    try {
      setUser(await api.userDetails(userId));
      setError(null);
    } catch (caught) {
      setError(describeApiError(caught, 'No fue posible cargar la cuenta.'));
    }
  }, [api, userId]);
  useEffect(() => {
    void reload();
  }, [reload]);
  return { user, error, reload, setUser };
}

// ---------------------------------------------------------------------------
// Edit: global fields, active membership role/work profile and admin reset as
// separate commands, executed in order and reported step by step.
// ---------------------------------------------------------------------------

function identityOf(user: ManagedUserRecord): IdentityForm {
  return {
    firstName: user.firstName,
    lastName: user.lastName,
    secondLastName: user.secondLastName ?? '',
    identityCard: user.identityCard,
    email: user.email,
    phoneNumber: user.phoneNumber,
  };
}

function workOf(membership: MembershipProjection | undefined): WorkForm {
  return {
    position: membership?.position ?? '',
    department: membership?.department ?? '',
    hireDate: membership?.hireDate?.slice(0, 10) ?? '',
  };
}

function UserEdit({
  api,
  viewer,
  userId,
  returnTo,
  go,
}: {
  readonly api: UsersApi;
  readonly viewer: UsersViewer;
  readonly userId: string;
  readonly returnTo?: string;
  readonly go: Go;
}): ReactElement {
  const { user, error: loadError, reload } = useManagedUser(api, userId);
  const [identity, setIdentity] = useState<IdentityForm>(BLANK_IDENTITY);
  const [work, setWork] = useState<WorkForm>(BLANK_WORK);
  const [role, setRole] = useState<MembershipRole>(SiteRole.Supervisor);
  const [password, setPassword] = useState('');
  const [photo, setPhoto] = useState<File | null>(null);
  const [busy, setBusy] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [warning, setWarning] = useState<string | null>(null);
  const existingPhoto = usePhotoUrl(user?.profilePicture ?? null, () => api.userPhoto(userId));

  const membership = user
    ? user.memberships.find(
        (m) => m.status !== 'revoked' && m.siteId === (viewer.activeSiteId ?? m.siteId),
      )
    : undefined;

  useEffect(() => {
    if (!user) return;
    setIdentity(identityOf(user));
    const current = user.memberships.find(
      (m) => m.status !== 'revoked' && m.siteId === (viewer.activeSiteId ?? m.siteId),
    );
    setWork(workOf(current));
    if (current) setRole(current.role);
  }, [user, viewer.activeSiteId]);

  const backPath = returnTo === 'Details' ? `/Users/Details/${userId}` : '/Users/Index';

  if (loadError) return <Alerts error={loadError} />;
  if (!user) return <p className="text-muted">Cargando cuenta…</p>;

  const readOnly = !canAdminister(user, viewer);
  const self = user.id === viewer.userId;
  const roleLocked = !membership || isOwnActiveMembership(user, membership, viewer);
  const accountLabel = ACCOUNT_STATUS[user.accountStatus];

  async function submit(event: FormEvent<HTMLFormElement>): Promise<void> {
    event.preventDefault();
    if (busy || !user) return;
    setError(null);
    setWarning(null);
    if (password !== '') {
      const violations = passwordPolicyViolations(password);
      if (violations.length > 0) {
        setError(`La nueva contraseña ${passwordViolationText(violations)}.`);
        return;
      }
    }
    const steps: Array<{ readonly label: string; readonly run: () => Promise<unknown> }> = [];
    const changes = globalFieldChanges(user, identity);
    if (Object.keys(changes).length > 0) {
      steps.push({
        label: 'datos de identidad',
        run: () => api.updateUserGlobalFields(user.id, changes),
      });
    }
    if (membership && !roleLocked && role !== membership.role) {
      steps.push({
        label: 'rol en la sede',
        run: () => api.changeMembershipRole(user.id, membership.siteId, { role }),
      });
    }
    if (membership) {
      const before = workOf(membership);
      if (
        before.position !== work.position ||
        before.department !== work.department ||
        before.hireDate !== work.hireDate
      ) {
        steps.push({
          label: 'perfil laboral',
          run: () => api.updateMembershipWorkProfile(user.id, membership.siteId, workInput(work)),
        });
      }
    }
    if (password !== '' && !self) {
      steps.push({
        label: 'contraseña',
        run: () => api.adminResetPassword(user.id, { password }),
      });
    }
    if (photo) {
      steps.push({
        label: 'foto',
        run: () => api.uploadUserPhoto(user.id, photoUpload(photo)),
      });
    }
    if (steps.length === 0) {
      setWarning('No hay cambios para guardar.');
      return;
    }
    setBusy(true);
    const done: string[] = [];
    for (const step of steps) {
      try {
        await step.run();
        done.push(step.label);
      } catch (caught) {
        setBusy(false);
        setError(
          `${done.length > 0 ? `Se aplicaron: ${done.join(', ')}. ` : ''}No se pudo actualizar ${step.label}: ${describeApiError(caught, 'error inesperado')}`,
        );
        setPassword('');
        await reload();
        return;
      }
    }
    setBusy(false);
    go(backPath, `Datos de la cuenta '${user.fullName}' actualizados correctamente.`);
  }

  const text = (key: keyof IdentityForm, extra: Record<string, unknown>) =>
    function Input(id: string): ReactElement {
      return (
        <input
          id={id}
          className="form-control"
          value={identity[key]}
          onChange={(event) => setIdentity({ ...identity, [key]: event.target.value })}
          {...extra}
        />
      );
    };

  return (
    <section className="row users-module" aria-label="Editar usuario">
      <div className="col-lg-8">
        <div className="card shadow-sm border-0">
          <div className="card-body p-4">
            <div className="d-flex align-items-center mb-4 pb-3 border-bottom">
              <div>
                <h4 className="card-title text-dark font-weight-bold">
                  Gestión de Perfil de Usuario
                </h4>
                <h6 className="card-subtitle text-muted">
                  Actualización de privilegios, credenciales y datos de contacto de la cuenta
                  institucional.
                </h6>
              </div>
              <div className="ml-auto mt-2 mt-md-0">
                <i className="fas fa-user-edit fa-2x text-warning opacity-5" aria-hidden="true" />
              </div>
            </div>

            <form onSubmit={(event) => void submit(event)}>
              <Alerts warning={warning} error={error} />
              {readOnly ? (
                <div className="alert alert-warning shadow-sm border-0 small" role="status">
                  Un Administrador de sede no puede modificar una cuenta SuperAdmin.
                </div>
              ) : null}
              <fieldset className="row" disabled={readOnly || busy}>
                <div className="col-md-6 border-right">
                  <SectionTitle icon="fas fa-id-badge">Identidad y Contacto</SectionTitle>
                  <div className="row">
                    <div className="col-md-6">
                      <FormField label="Nombres" required>
                        {text('firstName', { placeholder: 'Juan', required: true, maxLength: 100 })}
                      </FormField>
                    </div>
                    <div className="col-md-6">
                      <FormField label="A. Paterno" required>
                        {text('lastName', { placeholder: 'Pérez', required: true, maxLength: 100 })}
                      </FormField>
                    </div>
                  </div>
                  <FormField label="A. Materno">
                    {text('secondLastName', { maxLength: 100 })}
                  </FormField>
                  <FormField
                    label="Cédula de Identidad (C.I.)"
                    required
                    labelClass="text-info"
                    className="form-group mb-3 border-left border-info pl-3 bg-light p-3 rounded shadow-none"
                  >
                    {(id) => (
                      <input
                        id={id}
                        className="form-control border-info shadow-none font-weight-bold"
                        required
                        maxLength={20}
                        value={identity.identityCard}
                        onChange={(event) =>
                          setIdentity({ ...identity, identityCard: event.target.value })
                        }
                      />
                    )}
                  </FormField>
                  <FormField label="Correo Institucional" required>
                    {text('email', { type: 'email', required: true, maxLength: 256 })}
                  </FormField>
                  <FormField label="Teléfono de Contacto" required>
                    {text('phoneNumber', { type: 'tel', required: true, maxLength: 30 })}
                  </FormField>
                  <PhotoPicker
                    placeholder="Cambiar foto..."
                    fileName={photo?.name ?? null}
                    onFile={(file, input) => {
                      const problem = file ? photoFileProblem(file) : null;
                      setError(problem);
                      setPhoto(problem === null ? file : null);
                      if (problem !== null) input.value = '';
                    }}
                    preview={
                      <div className="mb-2">
                        {existingPhoto ? (
                          <img
                            src={existingPhoto}
                            alt="Foto actual"
                            className="img-thumbnail"
                            style={{ maxHeight: '100px' }}
                          />
                        ) : (
                          <div className="py-2 bg-light rounded border border-dashed text-muted text-center">
                            <i
                              className="fas fa-camera fa-lg mb-1 opacity-5 d-block"
                              aria-hidden="true"
                            />
                            <span className="small font-weight-bold uppercase">
                              Sin foto de perfil
                            </span>
                          </div>
                        )}
                      </div>
                    }
                  />
                </div>

                <div className="col-md-6">
                  <SectionTitle icon="fas fa-lock">Privilegios de Acceso</SectionTitle>
                  {membership ? (
                    <FormField
                      label="Rol de Aplicación"
                      required
                      labelClass="text-warning"
                      className="form-group mb-3 border-left border-warning pl-3 bg-light p-3 rounded shadow-none"
                      help={
                        <small className="text-muted d-block mt-1">
                          {roleLocked
                            ? 'No puede cambiar su propio rol en la sede activa.'
                            : `Membresía en ${membership.siteName}.`}
                        </small>
                      }
                    >
                      {(id) => (
                        <select
                          id={id}
                          className="form-control custom-select border-warning font-weight-bold"
                          value={role}
                          disabled={roleLocked}
                          onChange={(event) => setRole(event.target.value as MembershipRole)}
                        >
                          <RoleOptions />
                        </select>
                      )}
                    </FormField>
                  ) : (
                    <p className="text-muted small">
                      Sin membresía editable en la sede activa. Gestione las membresías desde
                      Detalles.
                    </p>
                  )}

                  <div className="bg-light p-3 rounded mb-4 border shadow-none">
                    <h6 className="font-weight-bold text-dark small text-uppercase mb-3 italic">
                      <i className="fas fa-key mr-2 text-muted" aria-hidden="true" /> Gestión de
                      Credenciales
                    </h6>
                    <FormField
                      label="Nombre de Usuario (Login)"
                      help={
                        <small className="text-muted italic" style={{ fontSize: '0.7rem' }}>
                          El identificador de acceso es único y permanente.
                        </small>
                      }
                    >
                      {(id) => (
                        <input
                          id={id}
                          className="form-control bg-white shadow-none"
                          readOnly
                          value={user.username}
                        />
                      )}
                    </FormField>
                    {self ? (
                      <small className="text-warning font-weight-bold d-block small">
                        <i className="fas fa-info-circle mr-1" aria-hidden="true" /> Para cambiar su
                        propia contraseña use Mi Perfil.
                      </small>
                    ) : (
                      <PasswordField
                        label="Nueva Contraseña"
                        value={password}
                        onChange={setPassword}
                        required={false}
                        tools={false}
                        placeholder="********"
                        help={
                          <small className="text-warning font-weight-bold d-block mt-2 small">
                            <i className="fas fa-info-circle mr-1" aria-hidden="true" /> Deje en
                            blanco para conservar la actual.
                          </small>
                        }
                      />
                    )}
                  </div>

                  {membership ? (
                    <WorkFields form={work} onChange={setWork} departmentLabel="Área/Dpto." />
                  ) : null}
                </div>
              </fieldset>

              <hr />

              <div className="form-actions text-right mt-4">
                <button
                  type="submit"
                  className="btn btn-warning btn-rounded px-5 shadow-sm text-white font-weight-bold py-2"
                  disabled={readOnly || busy}
                >
                  <i className="fas fa-save mr-1" aria-hidden="true" /> Guardar Cambios
                </button>
                <button
                  type="button"
                  className="btn btn-outline-secondary btn-rounded ml-2 py-2 px-4 font-weight-bold"
                  onClick={() => go(backPath)}
                >
                  Cancelar
                </button>
              </div>
            </form>
          </div>
        </div>
      </div>

      <div className="col-lg-4">
        <div className="card shadow-sm border-0 bg-dark text-white mb-4">
          <div className="card-body">
            <h5 className="card-title text-white font-weight-bold small uppercase mb-4">
              <i className="fas fa-shield-alt mr-2 text-info" aria-hidden="true" /> Estatus de
              Cuenta
            </h5>
            <hr className="border-secondary" />
            <div className="form-group">
              <span className="small text-muted uppercase font-weight-bold d-block mb-3">
                Estado Operativo
              </span>
              <div className="d-flex align-items-center mb-3">
                <div className="m-r-10">
                  <span
                    className={`btn btn-circle btn-lg btn-${user.accountStatus === 'active' ? 'success' : 'danger'} shadow-sm`}
                    aria-hidden="true"
                  >
                    <i className="fas fa-power-off" />
                  </span>
                </div>
                <div>
                  <h5 className="font-weight-bold text-white mb-0">Cuenta {accountLabel}</h5>
                  <span className="text-muted small">@{user.username}</span>
                </div>
              </div>
            </div>
            <div className="mt-4 py-2 border-top border-secondary small">
              <div className="d-flex justify-content-between mb-2">
                <span className="text-muted">Ingreso al Sistema:</span>
                <span className="font-weight-bold text-info">
                  {!membership
                    ? 'Sin membresía en la sede'
                    : user.accountStatus === 'active' && membership.effectiveStatus === 'Activo'
                      ? 'Habilitado'
                      : membership.effectiveStatus}
                </span>
              </div>
              <div className="d-flex justify-content-between">
                <span className="text-muted">Registro Auditado:</span>
                <span className="text-white">
                  {membership?.hireDate ? formatDay(membership.hireDate) : '-'}
                </span>
              </div>
            </div>
          </div>
        </div>

        <div className="alert alert-light border-0 p-3 shadow-none italic small text-info border-left border-info border-3">
          <i className="fas fa-info-circle mr-1" aria-hidden="true" /> <strong>Aviso:</strong> Al
          modificar el <strong>Rol de Aplicación</strong>, los cambios de permisos se aplicarán en
          la siguiente solicitud del usuario.
        </div>
      </div>
    </section>
  );
}

// ---------------------------------------------------------------------------
// Details: identity, contact/work, memberships, security, audit and actions.
// ---------------------------------------------------------------------------

function MembershipRow({
  api,
  user,
  membership,
  viewer,
  manageable,
  run,
}: {
  readonly api: UsersApi;
  readonly user: ManagedUserRecord;
  readonly membership: MembershipProjection;
  readonly viewer: UsersViewer;
  readonly manageable: boolean;
  readonly run: (label: string, action: () => Promise<unknown>, confirm?: string) => Promise<void>;
}): ReactElement {
  const [editing, setEditing] = useState(false);
  const [validFrom, setValidFrom] = useState(membership.validFrom?.slice(0, 10) ?? '');
  const [validUntil, setValidUntil] = useState(membership.validUntil?.slice(0, 10) ?? '');
  const [work, setWork] = useState<WorkForm>(workOf(membership));
  const locked = !manageable || isOwnActiveMembership(user, membership, viewer);
  const other: MembershipRole =
    membership.role === SiteRole.Administrador ? SiteRole.Supervisor : SiteRole.Administrador;
  const bounds = { validFrom: validFrom || null, validUntil: validUntil || null };
  const action = 'btn btn-sm btn-rounded shadow-none mr-1 mb-1';

  return (
    <>
      <tr>
        <td>{membership.siteName}</td>
        <td>
          <div className="small text-dark font-weight-bold">
            <i className="fas fa-shield-alt mr-1 text-info" aria-hidden="true" /> {membership.role}
          </div>
        </td>
        <td className="text-center">
          <StatusBadge
            label={MEMBERSHIP_STATUS[membership.status]}
            tone={MEMBERSHIP_TONE[membership.status]}
          />
          <div className="small text-muted mt-1">{membership.effectiveStatus}</div>
        </td>
        <td>
          <div className="small text-dark">
            <i className="far fa-calendar-alt text-muted mr-1" aria-hidden="true" />{' '}
            {membership.validFrom ? formatDay(membership.validFrom) : 'Sin inicio'} —{' '}
            {membership.validUntil ? formatDay(membership.validUntil) : 'Sin fin'}
          </div>
        </td>
        <td>
          <div className="small">
            <div className="text-dark font-weight-bold">{membership.position || '-'}</div>
            <div className="text-muted">{membership.department || '-'}</div>
            <div className="text-muted">
              Ingreso: {membership.hireDate ? formatDay(membership.hireDate) : 'Sin dato'}
            </div>
          </div>
        </td>
        <td className="text-center">
          {locked ? (
            <small className="text-muted italic">
              {manageable ? 'Membresía de su sede activa' : 'Solo lectura'}
            </small>
          ) : membership.status === 'revoked' ? (
            <button
              type="button"
              className={`${action} btn-outline-success`}
              onClick={() =>
                void run('Membresía restaurada.', () =>
                  api.restoreMembership(user.id, membership.siteId, {
                    ...(validFrom || validUntil ? bounds : {}),
                    restoreAccount: user.accountStatus === 'deleted' ? true : undefined,
                  }),
                )
              }
            >
              Restaurar
            </button>
          ) : (
            <>
              <button
                type="button"
                className={`${action} btn-outline-info`}
                onClick={() =>
                  void run(
                    `Rol cambiado a ${other}.`,
                    () => api.changeMembershipRole(user.id, membership.siteId, { role: other }),
                    `¿Cambiar el rol en ${membership.siteName} a ${other}?`,
                  )
                }
              >
                Cambiar a {other}
              </button>
              {membership.status === 'active' ? (
                <button
                  type="button"
                  className={`${action} btn-outline-warning`}
                  onClick={() =>
                    void run(
                      'Membresía suspendida.',
                      () =>
                        api.changeMembershipStatus(user.id, membership.siteId, {
                          status: 'suspended',
                        }),
                      `¿Suspender el acceso en ${membership.siteName}?`,
                    )
                  }
                >
                  Suspender
                </button>
              ) : (
                <button
                  type="button"
                  className={`${action} btn-outline-success`}
                  onClick={() =>
                    void run('Membresía reactivada.', () =>
                      api.changeMembershipStatus(user.id, membership.siteId, { status: 'active' }),
                    )
                  }
                >
                  Reactivar
                </button>
              )}
              <button
                type="button"
                className={`${action} btn-outline-danger`}
                onClick={() =>
                  void run(
                    'Acceso a la sede revocado.',
                    () => api.revokeMembership(user.id, membership.siteId),
                    `¿Revocar el acceso en ${membership.siteName}? Si no conserva otras sedes, la cuenta queda eliminada.`,
                  )
                }
              >
                Revocar
              </button>
            </>
          )}
          {!locked ? (
            <button
              type="button"
              className={`${action} btn-outline-secondary`}
              aria-expanded={editing}
              onClick={() => setEditing((value) => !value)}
            >
              {editing ? 'Cerrar' : 'Vigencia / perfil'}
            </button>
          ) : null}
        </td>
      </tr>
      {editing && !locked ? (
        <tr>
          <td colSpan={6} className="bg-light">
            <div className="p-2">
              <div className="row">
                <div className="col-md-4">
                  <FormField label="Vigente desde">
                    {(id) => (
                      <input
                        id={id}
                        type="date"
                        className="form-control shadow-none"
                        value={validFrom}
                        onChange={(event) => setValidFrom(event.target.value)}
                      />
                    )}
                  </FormField>
                </div>
                <div className="col-md-4">
                  <FormField label="Vigente hasta">
                    {(id) => (
                      <input
                        id={id}
                        type="date"
                        className="form-control shadow-none"
                        value={validUntil}
                        onChange={(event) => setValidUntil(event.target.value)}
                      />
                    )}
                  </FormField>
                </div>
                <div className="col-md-4 d-flex align-items-end">
                  {membership.status !== 'revoked' ? (
                    <button
                      type="button"
                      className="btn btn-outline-info btn-rounded mb-3"
                      onClick={() =>
                        void run('Vigencia actualizada.', () =>
                          api.changeMembershipValidity(user.id, membership.siteId, bounds),
                        )
                      }
                    >
                      Guardar vigencia
                    </button>
                  ) : (
                    <small className="text-muted italic mb-3">
                      Las fechas se usan como nueva vigencia al restaurar (obligatorias si expiró).
                    </small>
                  )}
                </div>
              </div>
              {membership.status !== 'revoked' ? (
                <>
                  <WorkFields form={work} onChange={setWork} />
                  <div className="text-right">
                    <button
                      type="button"
                      className="btn btn-outline-info btn-rounded"
                      onClick={() =>
                        void run('Perfil laboral actualizado.', () =>
                          api.updateMembershipWorkProfile(
                            user.id,
                            membership.siteId,
                            workInput(work),
                          ),
                        )
                      }
                    >
                      Guardar perfil laboral
                    </button>
                  </div>
                </>
              ) : null}
            </div>
          </td>
        </tr>
      ) : null}
    </>
  );
}

function UserDetails({
  api,
  viewer,
  userId,
  go,
  flash,
}: {
  readonly api: UsersApi;
  readonly viewer: UsersViewer;
  readonly userId: string;
  readonly go: Go;
  readonly flash: ReactElement | null;
}): ReactElement {
  const { user, error: loadError, reload, setUser } = useManagedUser(api, userId);
  const [feedback, setFeedback] = useState<string | null>(null);
  const [error, setError] = useState<string | null>(null);
  const [busy, setBusy] = useState(false);
  const [resetPassword, setResetPassword] = useState('');
  const [newSite, setNewSite] = useState('');
  const [newRole, setNewRole] = useState<MembershipRole>(SiteRole.Supervisor);

  const run = useCallback(
    async (label: string, action: () => Promise<unknown>, confirm?: string) => {
      if (confirm && !window.confirm(confirm)) return;
      setBusy(true);
      setError(null);
      setFeedback(null);
      try {
        await action();
        setFeedback(label);
      } catch (caught) {
        setError(describeApiError(caught, 'No fue posible completar la operación.'));
      } finally {
        setBusy(false);
        await reload();
      }
    },
    [reload],
  );

  if (loadError) {
    return (
      <section className="row users-module" aria-label="Detalle de usuario">
        <div className="col-12">
          <div className="card shadow-sm border-0">
            <div className="card-body p-4 text-center">
              <Alerts error={loadError} />
              <button
                type="button"
                className="btn btn-outline-secondary btn-rounded py-2 px-4 font-weight-bold"
                onClick={() => go('/Users/Index')}
              >
                <i className="fas fa-list mr-1" aria-hidden="true" /> Volver al Listado
              </button>
            </div>
          </div>
        </div>
      </section>
    );
  }
  if (!user) return <p className="text-muted">Cargando cuenta…</p>;

  const manageable = canAdminister(user, viewer);
  const self = user.id === viewer.userId;
  const current = viewerMembership(user, viewer);
  // F1 §11: a site Administrador sees its revoked site membership only as
  // history to restore; global commands need live single-site custody (F1 §10).
  const siteHistory = !viewer.isSuperAdmin && current?.status === 'revoked';
  const globalManageable = manageable && !siteHistory;
  const linkableSites = viewer.eligibleSites.filter(
    (site) => !user.memberships.some((m) => m.siteId === site.siteId),
  );
  const createdBy =
    user.createdBy === undefined ? 'Sin dato' : (user.createdBy?.fullName ?? 'Sistema');
  const modifiedBy =
    user.modifiedBy === undefined
      ? 'Sin dato'
      : (user.modifiedBy?.fullName ?? 'Sin modificaciones');
  const accountLabel = ACCOUNT_STATUS[user.accountStatus];
  const accountTone = activeTone(accountLabel);
  const roleLabel = user.displayRole ?? 'Sin rol en la sede';

  return (
    <section className="row users-module" aria-label="Detalle de usuario">
      <div className="col-12">
        {flash}
        <Alerts feedback={feedback} error={error} />
      </div>
      <div className="col-lg-8">
        <div className="card shadow-sm border-0">
          <div className="card-body p-4">
            <div className="d-flex flex-wrap align-items-center border-bottom pb-4 mb-4">
              <div className="mr-4 mb-3 mb-md-0">
                <ProfilePhoto
                  picture={user.profilePicture}
                  initials={user.initials}
                  label={user.fullName}
                  load={() => api.userPhoto(user.id)}
                  upload={
                    globalManageable ? (upload) => api.uploadUserPhoto(user.id, upload) : undefined
                  }
                  remove={globalManageable ? () => api.deleteUserPhoto(user.id) : undefined}
                  onChanged={(picture) => setUser({ ...user, profilePicture: picture })}
                />
              </div>
              <div className="flex-grow-1">
                <small className="text-muted text-uppercase font-weight-bold">
                  Ficha de usuario
                </small>
                <h2 className="font-weight-bold text-dark mb-1">{user.fullName}</h2>
                <div className="text-muted mb-2">
                  {orFallback(current?.position, 'Cargo no registrado')}
                </div>
                <span className="badge badge-pill badge-info px-3 py-1 mr-1">{roleLabel}</span>
                {user.isSuperAdmin ? (
                  <span className="badge badge-pill badge-dark px-3 py-1 mr-1">
                    SuperAdmin global
                  </span>
                ) : null}
                <span className={`badge badge-pill badge-${accountTone} px-3 py-1`}>
                  {accountLabel}
                </span>
              </div>
            </div>

            <SectionTitle icon="fas fa-id-card-alt">Identidad</SectionTitle>
            <div className="row mb-4">
              <DetailItem label="Nombres" value={orFallback(user.firstName)} />
              <DetailItem label="Apellido paterno" value={orFallback(user.lastName)} />
              <DetailItem
                label="Apellido materno"
                value={orFallback(user.secondLastName, 'No registrado')}
              />
              <DetailItem label="C.I." value={orFallback(user.identityCard)} />
              <DetailItem label="Usuario / login" value={`@${orFallback(user.username)}`} />
              <DetailItem
                label="Fecha de alta"
                value={current?.hireDate ? formatDay(current.hireDate) : 'No registrada'}
              />
            </div>

            <SectionTitle icon="fas fa-address-book">Contacto y perfil laboral</SectionTitle>
            <div className="row">
              <DetailItem
                className="col-md-6 mb-3"
                label="Correo institucional"
                value={orFallback(user.email, 'Correo no registrado')}
              />
              <DetailItem
                className="col-md-6 mb-3"
                label="Telefono"
                value={orFallback(user.phoneNumber, 'Telefono no registrado')}
              />
              <DetailItem
                className="col-md-6 mb-3"
                label="Cargo"
                value={orFallback(current?.position, 'Cargo no registrado')}
              />
              <DetailItem
                className="col-md-6 mb-3"
                label="Departamento"
                value={orFallback(current?.department, 'Departamento no registrado')}
              />
            </div>
          </div>
        </div>

        <div className="card shadow-sm border-0">
          <div className="card-body p-4">
            <SectionTitle
              icon="fas fa-history text-warning"
              className="font-weight-bold text-dark mb-4"
            >
              Auditoria
            </SectionTitle>
            <div className="row">
              <DetailItem className="col-md-6 mb-3" label="Creado por" value={createdBy}>
                <small className="text-muted">{formatDate(user.createdAt, true)}</small>
              </DetailItem>
              <DetailItem className="col-md-6 mb-3" label="Ultima modificacion" value={modifiedBy}>
                <small className="text-muted">{formatDate(user.updatedAt, true)}</small>
              </DetailItem>
            </div>
          </div>
        </div>

        <div className="card shadow-sm border-0">
          <div className="card-body p-4">
            <SectionTitle icon="fas fa-sitemap">Membresías y acceso por sede</SectionTitle>
            {user.memberships.length === 0 ? (
              <p className="text-muted italic mb-0">Sin membresías visibles.</p>
            ) : (
              <div className="table-responsive">
                <table className="table table-hover v-middle">
                  <thead className="bg-light">
                    <tr className="small uppercase font-weight-bold">
                      <th>Sede</th>
                      <th>Rol</th>
                      <th className="text-center">Estado</th>
                      <th>Vigencia</th>
                      <th>Perfil laboral</th>
                      <th className="text-center">Acciones</th>
                    </tr>
                  </thead>
                  <tbody>
                    {user.memberships.map((membership) => (
                      <MembershipRow
                        key={membership.siteId}
                        api={api}
                        user={user}
                        membership={membership}
                        viewer={viewer}
                        manageable={manageable && !busy}
                        run={run}
                      />
                    ))}
                  </tbody>
                </table>
              </div>
            )}

            {viewer.isSuperAdmin && linkableSites.length > 0 ? (
              <form
                className="d-flex flex-wrap align-items-center bg-light p-3 mt-3 rounded border-left border-info"
                aria-label="Agregar membresía"
                onSubmit={(event) => {
                  event.preventDefault();
                  const site = newSite || linkableSites[0]!.siteId;
                  void run('Membresía agregada.', () =>
                    api.addMembership(user.id, { siteId: site as SiteId, role: newRole }),
                  );
                }}
              >
                <select
                  className="custom-select w-auto mw-100 mr-2 mb-2"
                  aria-label="Sede a vincular"
                  value={newSite || linkableSites[0]!.siteId}
                  onChange={(event) => setNewSite(event.target.value)}
                >
                  {linkableSites.map((site) => (
                    <option key={site.siteId} value={site.siteId}>
                      {site.siteName}
                    </option>
                  ))}
                </select>
                <select
                  className="custom-select w-auto mw-100 mr-2 mb-2"
                  aria-label="Rol de la nueva membresía"
                  value={newRole}
                  onChange={(event) => setNewRole(event.target.value as MembershipRole)}
                >
                  <RoleOptions />
                </select>
                <button
                  type="submit"
                  className="btn bg-white border btn-rounded shadow-sm text-dark px-4 font-weight-bold mb-2"
                  disabled={busy}
                >
                  <i className="fas fa-plus-circle mr-1 text-info" aria-hidden="true" /> Agregar
                  membresía
                </button>
              </form>
            ) : null}
          </div>
        </div>
      </div>

      <div className="col-lg-4">
        <div className="card shadow-sm border-0 mb-4 bg-dark text-white">
          <div className="card-body p-4">
            <h5 className="card-title text-white font-weight-bold small text-uppercase mb-4">
              <i className="fas fa-shield-alt mr-2 text-info" aria-hidden="true" /> Seguridad de
              acceso
            </h5>
            <hr className="border-secondary" />
            <div className="d-flex justify-content-between align-items-center mb-3">
              <span className="text-muted small text-uppercase font-weight-bold">Rol</span>
              <span className="font-weight-bold text-white">{roleLabel}</span>
            </div>
            <div className="d-flex justify-content-between align-items-center mb-3">
              <span className="text-muted small text-uppercase font-weight-bold">Estado</span>
              <span className={`badge badge-pill badge-${accountTone} px-3`}>{accountLabel}</span>
            </div>
            <div className="d-flex justify-content-between align-items-center mb-3">
              <span className="text-muted small text-uppercase font-weight-bold">Foto</span>
              <span className="font-weight-bold text-white">
                {user.profilePicture ? 'Registrada' : 'Sin foto de perfil'}
              </span>
            </div>
            <div className="d-flex justify-content-between align-items-center">
              <span className="text-muted small text-uppercase font-weight-bold">
                Cambio de contraseña pendiente
              </span>
              <span className="font-weight-bold text-white">
                {user.mustChangePassword ? 'Sí' : 'No'}
              </span>
            </div>
          </div>
        </div>

        <div className="card shadow-sm border-0">
          <div className="card-body p-4">
            <h5 className="card-title font-weight-bold text-dark mb-4">
              <i className="fas fa-cogs mr-2 text-warning" aria-hidden="true" /> Acciones
            </h5>
            {globalManageable ? (
              <button
                type="button"
                className="btn btn-warning btn-block btn-rounded text-white shadow-sm font-weight-bold py-2 mb-2"
                onClick={() => go(`/Users/Edit/${user.id}?returnUrl=Details`)}
              >
                <i className="fas fa-edit mr-1" aria-hidden="true" /> Modificar Perfil
              </button>
            ) : null}
            <button
              type="button"
              className="btn btn-outline-secondary btn-block btn-rounded py-2 font-weight-bold mb-3"
              onClick={() => go('/Users/Index')}
            >
              <i className="fas fa-list mr-1" aria-hidden="true" /> Volver al Listado
            </button>
            {globalManageable && !self && user.accountStatus === 'active' ? (
              <button
                type="button"
                className="btn btn-outline-danger btn-block btn-rounded py-2 font-weight-bold mb-2"
                disabled={busy}
                onClick={() =>
                  void run(
                    'Cuenta inactivada.',
                    () => api.setAccountStatus(user.id, { accountStatus: 'inactive' }),
                    `¿Inactivar globalmente la cuenta de ${user.fullName}?`,
                  )
                }
              >
                <i className="fas fa-power-off mr-1" aria-hidden="true" /> Inactivar cuenta
              </button>
            ) : null}
            {globalManageable && !self && user.accountStatus === 'inactive' ? (
              <button
                type="button"
                className="btn btn-outline-success btn-block btn-rounded py-2 font-weight-bold mb-2"
                disabled={busy}
                onClick={() =>
                  void run('Cuenta activada.', () =>
                    api.setAccountStatus(user.id, { accountStatus: 'active' }),
                  )
                }
              >
                <i className="fas fa-power-off mr-1" aria-hidden="true" /> Activar cuenta
              </button>
            ) : null}
            {viewer.isSuperAdmin && !self && user.accountStatus === 'deleted' ? (
              <button
                type="button"
                className="btn btn-outline-success btn-block btn-rounded py-2 font-weight-bold mb-2"
                disabled={busy}
                onClick={() =>
                  void run('Cuenta restaurada.', () => api.restoreAccount(user.id, {}))
                }
              >
                <i className="fas fa-undo mr-1" aria-hidden="true" /> Restaurar cuenta
              </button>
            ) : null}
            {globalManageable && !self && user.accountStatus !== 'deleted' ? (
              <button
                type="button"
                className="btn btn-link btn-block text-danger small font-weight-bold"
                onClick={() => go(`/Users/Delete/${user.id}`)}
              >
                <i className="fas fa-user-minus mr-1" aria-hidden="true" /> Revocar Acceso
              </button>
            ) : null}
          </div>
        </div>

        {globalManageable && !self && user.accountStatus !== 'deleted' ? (
          <div className="card shadow-sm border-0">
            <div className="card-body p-4">
              <form
                aria-label="Restablecer contraseña"
                onSubmit={(event) => {
                  event.preventDefault();
                  const violations = passwordPolicyViolations(resetPassword);
                  if (violations.length > 0) {
                    setError(`La nueva contraseña ${passwordViolationText(violations)}.`);
                    return;
                  }
                  void run(
                    'Contraseña restablecida. El usuario deberá iniciar sesión nuevamente.',
                    async () => {
                      await api.adminResetPassword(user.id, { password: resetPassword });
                      setResetPassword('');
                    },
                  );
                }}
              >
                <h6 className="font-weight-bold text-dark small text-uppercase mb-3 italic">
                  <i className="fas fa-key mr-2 text-muted" aria-hidden="true" /> Gestión de
                  Credenciales
                </h6>
                <PasswordField
                  label="Restablecer contraseña"
                  value={resetPassword}
                  onChange={setResetPassword}
                  required
                />
                <button
                  type="submit"
                  className="btn btn-outline-warning btn-block btn-rounded py-2 font-weight-bold"
                  disabled={busy}
                >
                  Restablecer contraseña
                </button>
              </form>
            </div>
          </div>
        ) : null}
      </div>
    </section>
  );
}

// ---------------------------------------------------------------------------
// Delete: site revoke (default) or SuperAdmin explicit global soft delete.
// ---------------------------------------------------------------------------

function UserDelete({
  api,
  viewer,
  userId,
  go,
}: {
  readonly api: UsersApi;
  readonly viewer: UsersViewer;
  readonly userId: string;
  readonly go: Go;
}): ReactElement {
  const { user, error: loadError } = useManagedUser(api, userId);
  const [busy, setBusy] = useState(false);
  const [error, setError] = useState<string | null>(null);

  if (loadError) return <Alerts error={loadError} />;
  if (!user) return <p className="text-muted">Cargando cuenta…</p>;

  const siteMembership = user.memberships.find(
    (m) => m.siteId === viewer.activeSiteId && m.status !== 'revoked',
  );
  const self = user.id === viewer.userId;
  const blocked = self
    ? 'No puede dar de baja su propia cuenta.'
    : !canAdminister(user, viewer)
      ? 'Un Administrador de sede no puede revocar el acceso de una cuenta SuperAdmin.'
      : null;
  const alreadyDeleted =
    user.accountStatus === 'deleted' || (!viewer.isSuperAdmin && siteMembership === undefined)
      ? `El usuario '${user.fullName}' ya se encuentra dado de baja.`
      : null;

  async function confirm(kind: 'site' | 'global'): Promise<void> {
    if (!user || busy) return;
    setBusy(true);
    setError(null);
    try {
      if (kind === 'site') await api.revokeMembership(user.id, siteMembership!.siteId);
      else await api.deleteAccount(user.id);
      go('/Users/Index', `El acceso para '${user.fullName}' ha sido revocado correctamente.`);
    } catch (caught) {
      setError(describeApiError(caught, 'No fue posible revocar el acceso. Intente nuevamente.'));
      setBusy(false);
    }
  }

  return (
    <section
      className="row justify-content-center align-items-center users-module"
      style={{ minHeight: '60vh' }}
      aria-label="Revocar acceso"
    >
      <div className="col-md-6 col-lg-5">
        <div className="card text-center shadow-sm border-0">
          <div className="card-body p-5">
            <div className="mb-4 text-danger">
              <i className="fas fa-user-times display-3" aria-hidden="true" />
            </div>
            <h3 className="card-title text-dark font-weight-bold">¿Revocar Acceso al Sistema?</h3>
            <p className="text-muted mb-4">
              Está a punto de dar de baja la cuenta de usuario de{' '}
              <strong className="text-dark">{user.fullName}</strong> (@{user.username}). El usuario
              ya no podrá iniciar sesión, pero su historial de acciones se preservará por integridad
              institucional.
            </p>

            <div className="bg-light p-3 rounded mb-4 text-left d-inline-block w-100 border-left border-danger border-3 shadow-none italic">
              <div className="d-flex justify-content-between mb-2">
                <span className="text-muted small uppercase font-weight-bold">Rol Actual:</span>
                <strong className="text-dark">{user.displayRole ?? 'Sin rol en la sede'}</strong>
              </div>
              <div className="d-flex justify-content-between mb-2">
                <span className="text-muted small uppercase font-weight-bold">Área/Dpto:</span>
                <span className="text-dark font-weight-bold small">
                  {orFallback(
                    siteMembership?.department ?? viewerMembership(user, viewer)?.department,
                    'No especificado',
                  )}
                </span>
              </div>
              <div className="d-flex justify-content-between pt-2 border-top">
                <span className="text-muted small uppercase font-weight-bold">Documento (CI):</span>
                <span className="text-dark small font-weight-bold">
                  {orFallback(user.identityCard)}
                </span>
              </div>
            </div>

            <div className="text-left">
              <Alerts error={blocked ?? error} warning={blocked ? null : alreadyDeleted} />
            </div>

            <div>
              <button
                type="button"
                className="btn btn-outline-secondary btn-rounded px-4 mr-2 py-2 font-weight-bold mb-2"
                onClick={() => go(`/Users/Details/${user.id}`)}
              >
                Ver Expediente
              </button>
              {!blocked && siteMembership ? (
                <button
                  type="button"
                  className="btn btn-danger btn-rounded px-4 shadow-sm py-2 font-weight-bold mb-2"
                  disabled={busy}
                  onClick={() => void confirm('site')}
                >
                  <i className="fas fa-trash-alt mr-1" aria-hidden="true" /> Sí, Confirmar Baja en{' '}
                  {siteMembership.siteName}
                </button>
              ) : null}
              {!blocked && viewer.isSuperAdmin && user.accountStatus !== 'deleted' ? (
                <button
                  type="button"
                  className="btn btn-outline-danger btn-rounded px-4 py-2 font-weight-bold mb-2 ml-md-2"
                  disabled={busy}
                  onClick={() => void confirm('global')}
                >
                  Eliminar cuenta global (revoca todas las sedes)
                </button>
              ) : null}
            </div>
            {!blocked && siteMembership ? (
              <small className="text-muted d-block mt-2">
                La baja en sede revoca la membresía de {siteMembership.siteName}; si no conserva
                otras sedes, la cuenta queda eliminada.
              </small>
            ) : null}

            <div className="mt-4">
              <button
                type="button"
                className="btn btn-link text-muted small font-weight-bold"
                onClick={() => go('/Users/Index')}
              >
                Volver al Control de Usuarios
              </button>
            </div>
          </div>
        </div>

        <div className="text-center mt-3 text-muted small italic">
          <i className="fas fa-info-circle mr-1" aria-hidden="true" />{' '}
          <strong>Sugerencia Técnica:</strong> Se recomienda <strong>Inactivar</strong> la cuenta
          desde la ficha del usuario si ha realizado registros críticos recientes.
        </div>
      </div>
    </section>
  );
}

// ---------------------------------------------------------------------------
// /Profile: global self-service (no active site required).
// ---------------------------------------------------------------------------

export function ProfilePanel({
  api,
  onSessionChanged,
  onIdentityChanged,
}: {
  readonly api: ProfileApi;
  /** Password change rotates the session: the caller routes on the returned purpose. */
  readonly onSessionChanged: (session: AuthSessionResponse) => void;
  /** Email/name/photo changed: the caller refreshes the shell identity. */
  readonly onIdentityChanged: () => void;
}): ReactElement {
  const [profile, setProfile] = useState<ProfileRecord | null>(null);
  const [form, setForm] = useState<UpdateProfileInput>({});
  const [error, setError] = useState<string | null>(null);
  const [feedback, setFeedback] = useState<string | null>(null);
  const [busy, setBusy] = useState(false);

  const fill = useCallback((value: ProfileRecord) => {
    setProfile(value);
    setForm({
      email: value.email,
      firstName: value.firstName,
      lastName: value.lastName,
      secondLastName: value.secondLastName ?? '',
      phoneNumber: value.phoneNumber,
    });
  }, []);

  useEffect(() => {
    void api
      .ownProfile()
      .then(fill)
      .catch((caught: unknown) =>
        setError(describeApiError(caught, 'No fue posible cargar tu perfil.')),
      );
  }, [api, fill]);

  async function submit(event: FormEvent<HTMLFormElement>): Promise<void> {
    event.preventDefault();
    if (!profile || busy) return;
    setError(null);
    setFeedback(null);
    const changes: { -readonly [K in keyof UpdateProfileInput]: UpdateProfileInput[K] } = {};
    if (form.email?.trim() !== profile.email) changes.email = form.email?.trim();
    if (form.firstName?.trim() !== profile.firstName) changes.firstName = form.firstName?.trim();
    if (form.lastName?.trim() !== profile.lastName) changes.lastName = form.lastName?.trim();
    if ((form.secondLastName?.trim() || null) !== profile.secondLastName) {
      changes.secondLastName = form.secondLastName?.trim() || null;
    }
    if (form.phoneNumber?.trim() !== profile.phoneNumber) {
      changes.phoneNumber = form.phoneNumber?.trim();
    }
    if (Object.keys(changes).length === 0) {
      setFeedback('No hay cambios para guardar.');
      return;
    }
    setBusy(true);
    try {
      fill(await api.updateOwnProfile(changes));
      setFeedback('Perfil actualizado correctamente.');
      onIdentityChanged();
    } catch (caught) {
      setError(describeApiError(caught, 'No fue posible actualizar el perfil.'));
    } finally {
      setBusy(false);
    }
  }

  const input = (
    key: keyof UpdateProfileInput,
    extra: { readonly required?: boolean; readonly type?: string; readonly maxLength?: number },
  ) =>
    function Input(id: string): ReactElement {
      return (
        <input
          id={id}
          className="form-control"
          value={(form[key] as string | null | undefined) ?? ''}
          onChange={(event) => setForm({ ...form, [key]: event.target.value })}
          {...extra}
        />
      );
    };
  const readOnly = (value: string) =>
    function ReadOnlyInput(id: string): ReactElement {
      return <input id={id} className="form-control bg-light shadow-none" readOnly value={value} />;
    };

  const accountLabel = profile ? ACCOUNT_STATUS[profile.accountStatus] : '';
  const roleLabel = profile
    ? profile.isSuperAdmin
      ? 'SuperAdmin'
      : profile.memberships.map((m) => m.role).join(', ') || 'Sin rol en la sede'
    : '';
  const position = profile?.memberships.find((m) => m.position)?.position;

  return (
    <section className="row users-module" aria-label="Mi perfil">
      <div className="col-12">
        <Alerts feedback={feedback} error={error} />
      </div>
      <div className="col-lg-8">
        <div className="card shadow-sm border-0">
          <div className="card-body p-4">
            <div className="d-flex flex-wrap align-items-center border-bottom pb-4 mb-4">
              {profile ? (
                <div className="mr-4 mb-3 mb-md-0">
                  <ProfilePhoto
                    picture={profile.profilePicture}
                    initials={profile.initials}
                    label={profile.fullName}
                    load={() => api.ownPhoto()}
                    upload={(upload) => api.uploadOwnPhoto(upload)}
                    remove={() => api.deleteOwnPhoto()}
                    onChanged={(picture) => {
                      setProfile({ ...profile, profilePicture: picture });
                      onIdentityChanged();
                    }}
                  />
                </div>
              ) : null}
              <div className="flex-grow-1">
                <h6 className="text-muted text-uppercase font-weight-bold small mb-1">Mi perfil</h6>
                <h2 className="font-weight-bold text-dark mb-1">
                  {profile ? profile.fullName : 'Cargando…'}
                </h2>
                {profile ? (
                  <>
                    <div className="text-muted mb-2">
                      {orFallback(position, 'Cargo no registrado')}
                    </div>
                    <span className="badge badge-pill badge-info px-3 py-1 mr-1">{roleLabel}</span>
                    <span
                      className={`badge badge-pill badge-${activeTone(accountLabel)} px-3 py-1`}
                    >
                      {accountLabel}
                    </span>
                  </>
                ) : null}
              </div>
            </div>

            <form onSubmit={(event) => void submit(event)}>
              <SectionTitle icon="fas fa-id-card-alt">Identidad</SectionTitle>
              <div className="row">
                <div className="col-md-4">
                  <FormField label="Nombres" required>
                    {input('firstName', { required: true, maxLength: 100 })}
                  </FormField>
                </div>
                <div className="col-md-4">
                  <FormField label="Apellido paterno" required>
                    {input('lastName', { required: true, maxLength: 100 })}
                  </FormField>
                </div>
                <div className="col-md-4">
                  <FormField label="Apellido materno">
                    {input('secondLastName', { maxLength: 100 })}
                  </FormField>
                </div>
                <div className="col-md-4">
                  <FormField label="C.I.">{readOnly(profile?.identityCard ?? '')}</FormField>
                </div>
                <div className="col-md-4">
                  <FormField label="Usuario / login">{readOnly(profile?.username ?? '')}</FormField>
                </div>
              </div>

              <SectionTitle icon="fas fa-address-book">Contacto</SectionTitle>
              <div className="row">
                <div className="col-md-6">
                  <FormField label="Correo institucional" required>
                    {input('email', { required: true, type: 'email' })}
                  </FormField>
                </div>
                <div className="col-md-6">
                  <FormField label="Telefono" required>
                    {input('phoneNumber', { required: true, type: 'tel' })}
                  </FormField>
                </div>
              </div>

              <hr />
              <div className="form-actions text-right mt-4">
                <button
                  className="btn btn-info btn-rounded px-5 shadow-sm font-weight-bold py-2"
                  type="submit"
                  disabled={busy || !profile}
                >
                  <i className="fas fa-save mr-1" aria-hidden="true" /> Guardar perfil
                </button>
              </div>
            </form>
          </div>
        </div>
      </div>

      <div className="col-lg-4">
        {profile ? (
          <div className="card shadow-sm border-0 mb-4 bg-dark text-white">
            <div className="card-body p-4">
              <h5 className="card-title text-white font-weight-bold small text-uppercase mb-4">
                <i className="fas fa-shield-alt mr-2 text-info" aria-hidden="true" /> Seguridad de
                acceso
              </h5>
              <hr className="border-secondary" />
              <div className="d-flex justify-content-between align-items-center mb-3">
                <span className="text-muted small text-uppercase font-weight-bold">Rol</span>
                <span className="font-weight-bold text-white">{roleLabel}</span>
              </div>
              <div className="d-flex justify-content-between align-items-center mb-3">
                <span className="text-muted small text-uppercase font-weight-bold">Estado</span>
                <span className={`badge badge-pill badge-${activeTone(accountLabel)} px-3`}>
                  {accountLabel}
                </span>
              </div>
              <div className="d-flex justify-content-between align-items-center">
                <span className="text-muted small text-uppercase font-weight-bold">Foto</span>
                <span className="font-weight-bold text-white">
                  {profile.profilePicture ? 'Registrada' : 'Sin foto de perfil'}
                </span>
              </div>
            </div>
          </div>
        ) : null}

        {profile ? (
          <div className="card shadow-sm border-0">
            <div className="card-body p-4">
              <h5 className="card-title font-weight-bold text-dark mb-3">
                <i className="fas fa-sitemap mr-2 text-info" aria-hidden="true" /> Membresías
              </h5>
              {profile.isSuperAdmin ? (
                <p className="small text-muted mb-2">Rol global: SuperAdmin</p>
              ) : null}
              {profile.memberships.length === 0 ? (
                <p className="text-muted italic small mb-0">Sin membresías de sede.</p>
              ) : (
                <ul className="list-unstyled mb-0">
                  {profile.memberships.map((m) => (
                    <li key={m.siteId} className="py-2 border-bottom small">
                      <div className="d-flex justify-content-between align-items-center">
                        <strong className="text-dark">{m.siteName}</strong>
                        <StatusBadge
                          label={m.effectiveStatus}
                          tone={activeTone(m.effectiveStatus)}
                        />
                      </div>
                      <div className="text-muted">
                        <i className="fas fa-shield-alt mr-1 text-info" aria-hidden="true" />{' '}
                        {m.role}
                        {m.position ? ` · ${m.position}` : ''}
                        {m.department ? ` · ${m.department}` : ''}
                      </div>
                    </li>
                  ))}
                </ul>
              )}
            </div>
          </div>
        ) : null}

        <div className="card shadow-sm border-0">
          <div className="card-body p-4">
            <PasswordChangeForm api={api} onChanged={onSessionChanged} />
          </div>
        </div>
      </div>
    </section>
  );
}

/**
 * POST /auth/password. A wrong current password and an expired session are
 * both 401; the session is re-read to tell them apart without guessing.
 */
export function PasswordChangeForm({
  api,
  onChanged,
  onExpired,
  title = 'Cambiar contraseña',
}: {
  readonly api: Pick<ProfileApi, 'changePassword' | 'currentSession'>;
  readonly onChanged: (session: AuthSessionResponse) => void;
  readonly onExpired?: () => void;
  readonly title?: string;
}): ReactElement {
  const [currentPassword, setCurrentPassword] = useState('');
  const [newPassword, setNewPassword] = useState('');
  const [confirmation, setConfirmation] = useState('');
  const [busy, setBusy] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [feedback, setFeedback] = useState<string | null>(null);

  async function submit(event: FormEvent<HTMLFormElement>): Promise<void> {
    event.preventDefault();
    if (busy) return;
    setError(null);
    setFeedback(null);
    const violations = passwordPolicyViolations(newPassword);
    if (violations.length > 0) {
      setError(`La nueva contraseña ${passwordViolationText(violations)}.`);
      return;
    }
    if (newPassword !== confirmation) {
      setError('La confirmación no coincide con la nueva contraseña.');
      return;
    }
    setBusy(true);
    try {
      const session = await api.changePassword({ currentPassword, newPassword });
      setCurrentPassword('');
      setNewPassword('');
      setConfirmation('');
      setBusy(false);
      setFeedback('Contraseña actualizada correctamente.');
      onChanged(session);
    } catch (caught) {
      setBusy(false);
      if (isApiError(caught) && caught.kind === 'authentication') {
        const stillValid = await api.currentSession().then(
          () => true,
          () => false,
        );
        if (stillValid) {
          setError('La contraseña actual es incorrecta.');
          return;
        }
        onExpired?.();
        setError(describeApiError(caught, ''));
        return;
      }
      setError(describeApiError(caught, 'No fue posible cambiar la contraseña.'));
    }
  }

  const secret = (value: string, onChange: (value: string) => void, autoComplete: string) =>
    function SecretInput(id: string): ReactElement {
      return (
        <input
          id={id}
          className="form-control shadow-none"
          type="password"
          autoComplete={autoComplete}
          required
          value={value}
          onChange={(event) => onChange(event.target.value)}
        />
      );
    };

  return (
    <form className="users-module" aria-label={title} onSubmit={(event) => void submit(event)}>
      <h3 className="h6 font-weight-bold text-dark small text-uppercase mb-3 italic">
        <i className="fas fa-key mr-2 text-muted" aria-hidden="true" /> {title}
      </h3>
      <Alerts feedback={feedback} error={error} />
      <FormField label="Contraseña actual" required>
        {secret(currentPassword, setCurrentPassword, 'current-password')}
      </FormField>
      <FormField label="Nueva contraseña" required help={<PasswordHint password={newPassword} />}>
        {secret(newPassword, setNewPassword, 'new-password')}
      </FormField>
      <FormField label="Confirmar nueva contraseña" required>
        {secret(confirmation, setConfirmation, 'new-password')}
      </FormField>
      <button
        className="btn btn-info btn-block btn-rounded shadow-sm font-weight-bold py-2"
        type="submit"
        disabled={busy}
      >
        {busy ? 'Guardando…' : 'Cambiar contraseña'}
      </button>
    </form>
  );
}
