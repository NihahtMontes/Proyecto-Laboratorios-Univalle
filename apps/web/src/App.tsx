import {
  useCallback,
  useEffect,
  useMemo,
  useState,
  type FormEvent,
  type MouseEvent,
  type ReactElement,
} from 'react';
import { ApiClient, ApiClientError } from '@lu/api-client';
import {
  GlobalRole,
  SiteRole,
  type ActiveSiteSession,
  type CatalogPage,
  type CatalogCollectionQuery,
  type City,
  type Country,
  type CreateCityInput,
  type CreateCountryInput,
  type DashboardNotification,
  type DashboardSummary,
  type LoginRequest,
  type SetActiveSiteRequest,
  type SiteRequestContext,
  type UpdateCityInput,
  type UpdateCountryInput,
  type AcademicQuery,
  type Career,
  type CreateLaboratoryInput,
  type Faculty,
  type Laboratory,
  type UpdateLaboratoryInput,
  type CreateEquipmentInput,
  type EquipmentPage,
  type EquipmentRecord,
  type EquipmentQuery,
  type UpdateEquipmentInput,
  type EquipmentUnitPage,
  type EquipmentUnitDetail,
  type EquipmentUnitQuery,
  type EquipmentUnitStateHistory,
  type CreateEquipmentUnitInput,
  type UpdateEquipmentUnitInput,
  type ManagementPage,
  type ManagementPlanPage,
  type ManagementRecord,
  type ManagementQuery,
  type CreateManagementInput,
  type UpdateManagementInput,
  type VerificationPage,
  type VerificationDetail,
  type VerificationCheckItem,
  type CreateVerificationInput,
  type RequestPage,
  type RequestDetail,
  type RequestQuery,
  type CreateRequestInput,
  type UpdateRequestInput,
  type MaintenancePage,
  type MaintenanceDetail,
  type MaintenanceQuery,
  type CreateMaintenanceInput,
  type UpdateMaintenanceInput,
  type DeparturePage,
  type DepartureDetail,
  type DepartureQuery,
  type CreateDepartureInput,
  type UpdateDepartureInput,
  type MassDepartureInput,
  type KardexPage,
  type KardexRecord,
  type KardexQuery,
  type KardexInput,
  type AcquisitionPage,
  type AcquisitionDetail,
  type AcquisitionQuery,
  type CreateAcquisitionInput,
  type UpdateAcquisitionInput,
  type PersonPage,
  type PersonRecord,
  type PersonQuery,
  type CreatePersonInput,
  type UpdatePersonInput,
  type ManagedUserPage,
  type ManagedUserRecord,
  type ManagedUserQuery,
  type CreateManagedUserInput,
  type UpdateManagedUserInput,
  type ProfileRecord,
  type UpdateProfileInput,
  type ReportKind,
  type ReportManifestItem,
} from '@lu/contracts';

import logoIcon from '../../../wwwroot/assets/images/logo-icon.png';
import logoLightIcon from '../../../wwwroot/assets/images/logo-light-icon.png';
import { EquipmentPanel, type EquipmentApi } from './EquipmentPanel';
import { EquipmentUnitsPanel, type EquipmentUnitsApi } from './EquipmentUnitsPanel';
import { ManagementPanel, type ManagementApi } from './ManagementPanel';
import { VerificationsPanel, type VerificationsApi } from './VerificationsPanel';
import { RequestsPanel, type RequestsApi } from './RequestsPanel';
import { MaintenancesPanel, type MaintenanceApi } from './MaintenancesPanel';
import { DeparturesPanel, type DepartureApi } from './DeparturesPanel';
import { KardexPanel, type KardexApi } from './KardexPanel';
import { AcquisitionsPanel, type AcquisitionsApi } from './AcquisitionsPanel';
import { PeoplePanel, type PeopleApi } from './PeoplePanel';
import { ProfilePanel, UsersPanel, type UsersApi } from './UsersPanel';
import { OperationsPanel, type OperationsApi } from './OperationsPanel';
import { WizardPanel, type WizardApi } from './WizardPanel';
import { ReportsPanel, type ReportsApi } from './ReportsPanel';

export interface AuthApi {
  session(): Promise<ActiveSiteSession>;
  login(input: LoginRequest): Promise<ActiveSiteSession>;
  setActiveSite(input: SetActiveSiteRequest): Promise<ActiveSiteSession>;
  siteContext(): Promise<SiteRequestContext>;
  logout(): Promise<void>;
  dashboard?(query?: { readonly currentPage?: number }): Promise<DashboardSummary>;
  notifications?(query?: {
    readonly unreadOnly?: boolean;
  }): Promise<readonly DashboardNotification[]>;
  markNotificationRead?(id: number): Promise<void>;
  markAllNotificationsRead?(): Promise<void>;
  countries?(query?: CatalogCollectionQuery): Promise<CatalogPage<Country>>;
  country?(id: number): Promise<Country>;
  createCountry?(input: CreateCountryInput): Promise<Country>;
  updateCountry?(id: number, input: UpdateCountryInput): Promise<Country>;
  deleteCountry?(id: number): Promise<void>;
  cities?(query?: CatalogCollectionQuery): Promise<CatalogPage<City>>;
  city?(id: number): Promise<City>;
  createCity?(input: CreateCityInput): Promise<City>;
  updateCity?(id: number, input: UpdateCityInput): Promise<City>;
  deleteCity?(id: number): Promise<void>;
  faculties?(query?: AcademicQuery): Promise<CatalogPage<Faculty>>;
  careers?(query?: AcademicQuery): Promise<CatalogPage<Career>>;
  laboratories?(query?: AcademicQuery): Promise<CatalogPage<Laboratory>>;
  laboratory?(id: number): Promise<Laboratory>;
  createLaboratory?(input: CreateLaboratoryInput): Promise<Laboratory>;
  updateLaboratory?(id: number, input: UpdateLaboratoryInput): Promise<Laboratory>;
  deleteLaboratory?(id: number): Promise<void>;
  equipment?(query?: EquipmentQuery): Promise<EquipmentPage>;
  equipmentDetail?(id: number): Promise<EquipmentRecord>;
  createEquipment?(input: CreateEquipmentInput): Promise<EquipmentRecord>;
  updateEquipment?(id: number, input: UpdateEquipmentInput): Promise<EquipmentRecord>;
  deleteEquipment?(id: number): Promise<void>;
  equipmentUnits?(query?: EquipmentUnitQuery): Promise<EquipmentUnitPage>;
  equipmentUnit?(id: number): Promise<EquipmentUnitDetail>;
  equipmentUnitHistory?(id: number): Promise<readonly EquipmentUnitStateHistory[]>;
  createEquipmentUnit?(input: CreateEquipmentUnitInput): Promise<EquipmentUnitDetail>;
  updateEquipmentUnit?(id: number, input: UpdateEquipmentUnitInput): Promise<EquipmentUnitDetail>;
  deleteEquipmentUnit?(id: number): Promise<void>;
  managements?(query?: ManagementQuery): Promise<ManagementPage>;
  management?(id: number): Promise<ManagementRecord>;
  createManagement?(input: CreateManagementInput): Promise<ManagementRecord>;
  updateManagement?(id: number, input: UpdateManagementInput): Promise<ManagementRecord>;
  activateManagement?(id: number): Promise<ManagementRecord>;
  closeManagement?(id: number): Promise<ManagementRecord>;
  deleteManagement?(id: number): Promise<void>;
  managementPlans?(
    id: number,
    query?: { readonly currentPage?: number },
  ): Promise<ManagementPlanPage>;
  syncManagementPlans?(
    id: number,
    input: { readonly addUnitIds: readonly number[]; readonly removeUnitIds: readonly number[] },
  ): Promise<void>;
  verifications?(query?: {
    readonly currentPage?: number;
    readonly managementId?: number;
  }): Promise<VerificationPage>;
  verification?(id: number): Promise<VerificationDetail>;
  verificationCheckItems?(): Promise<readonly VerificationCheckItem[]>;
  saveVerification?(input: CreateVerificationInput): Promise<VerificationDetail>;
  requests?(query?: RequestQuery): Promise<RequestPage>;
  requestDetail?(id: number): Promise<RequestDetail>;
  createRequest?(input: CreateRequestInput): Promise<RequestDetail>;
  updateRequest?(id: number, input: UpdateRequestInput): Promise<RequestDetail>;
  completeRequest?(id: number): Promise<RequestDetail>;
  cancelRequest?(id: number): Promise<void>;
  maintenances?(query?: MaintenanceQuery): Promise<MaintenancePage>;
  maintenanceDetail?(id: number): Promise<MaintenanceDetail>;
  createMaintenance?(input: CreateMaintenanceInput): Promise<MaintenanceDetail>;
  updateMaintenance?(id: number, input: UpdateMaintenanceInput): Promise<MaintenanceDetail>;
  completeMaintenance?(id: number): Promise<MaintenanceDetail>;
  cancelMaintenance?(id: number): Promise<void>;
  departures?(query?: DepartureQuery): Promise<DeparturePage>;
  departureDetail?(id: number): Promise<DepartureDetail>;
  createDeparture?(input: CreateDepartureInput): Promise<DepartureDetail>;
  createMassDepartures?(input: MassDepartureInput): Promise<readonly DepartureDetail[]>;
  updateDeparture?(id: number, input: UpdateDepartureInput): Promise<DepartureDetail>;
  returnDeparture?(id: number, observations?: string | null): Promise<DepartureDetail>;
  cancelDeparture?(id: number): Promise<void>;
  kardex?(query?: KardexQuery): Promise<KardexPage>;
  kardexDetail?(planId: number): Promise<KardexRecord>;
  saveKardexDraft?(input: KardexInput): Promise<KardexRecord>;
  completeKardex?(input: KardexInput): Promise<KardexRecord>;
  acquisitions?(query?: AcquisitionQuery): Promise<AcquisitionPage>;
  acquisitionDetail?(id: number): Promise<AcquisitionDetail>;
  createAcquisition?(input: CreateAcquisitionInput): Promise<AcquisitionDetail>;
  updateAcquisition?(id: number, input: UpdateAcquisitionInput): Promise<AcquisitionDetail>;
  completeAcquisition?(id: number): Promise<AcquisitionDetail>;
  cancelAcquisition?(id: number): Promise<void>;
  people?(query?: PersonQuery): Promise<PersonPage>;
  person?(id: number): Promise<PersonRecord>;
  createPerson?(input: CreatePersonInput): Promise<PersonRecord>;
  updatePerson?(id: number, input: UpdatePersonInput): Promise<PersonRecord>;
  deletePerson?(id: number): Promise<void>;
  users?(query?: ManagedUserQuery): Promise<ManagedUserPage>;
  user?(id: string): Promise<ManagedUserRecord>;
  createUser?(input: CreateManagedUserInput): Promise<ManagedUserRecord>;
  updateUser?(id: string, input: UpdateManagedUserInput): Promise<ManagedUserRecord>;
  deleteUser?(id: string): Promise<void>;
  profile?(): Promise<ProfileRecord>;
  updateProfile?(input: UpdateProfileInput): Promise<ProfileRecord>;
  reportManifest?(): Promise<readonly ReportManifestItem[]>;
  downloadReport?(kind: ReportKind, query?: Record<string, number | undefined>): Promise<Blob>;
}

const defaultApi = new ApiClient({ baseUrl: import.meta.env.VITE_API_BASE_URL });

type View = 'loading' | 'login' | 'site-picker' | 'workspace' | 'unavailable';
type Permission = 'authenticated' | 'admin';

interface NavigationLink {
  readonly label: string;
  readonly path: string;
  readonly icon: string;
  readonly permission: Permission;
}

interface NavigationGroup {
  readonly id: string;
  readonly label: string;
  readonly icon: string;
  readonly links: readonly NavigationLink[];
}

interface NavigationSection {
  readonly caption: string;
  readonly entries: readonly (NavigationLink | NavigationGroup)[];
}

const NAVIGATION: readonly NavigationSection[] = [
  {
    caption: 'Inicio',
    entries: [
      {
        label: 'Ver',
        path: '/AssetView/Index',
        icon: 'mdi mdi-eye-outline',
        permission: 'authenticated',
      },
      {
        id: 'laboratory-operations',
        label: 'Operación de laboratorios',
        icon: 'mdi mdi-flask-outline',
        links: [
          {
            label: 'Resumen',
            path: '/LaboratoryOperations/Index',
            icon: 'mdi mdi-view-dashboard',
            permission: 'authenticated',
          },
          {
            label: 'Paso 1 · Planificación',
            path: '/LaboratoryOperations/Planificacion',
            icon: 'mdi mdi-clipboard-text-outline',
            permission: 'authenticated',
          },
          {
            label: 'Paso 2 · Requerimientos',
            path: '/LaboratoryOperations/Requerimientos',
            icon: 'mdi mdi-format-list-checks',
            permission: 'authenticated',
          },
          {
            label: 'Paso 3 · Disponibilidad',
            path: '/LaboratoryOperations/Disponibilidad',
            icon: 'mdi mdi-check-circle-outline',
            permission: 'authenticated',
          },
          {
            label: 'Paso 4 · L-3 Devolución',
            path: '/LaboratoryOperations/L3',
            icon: 'mdi mdi-clipboard-arrow-left-outline',
            permission: 'authenticated',
          },
          {
            label: 'Paso 5 · Incidentes L-5',
            path: '/LaboratoryOperations/L5',
            icon: 'mdi mdi-alert-outline',
            permission: 'authenticated',
          },
          {
            label: 'Demanda',
            path: '/LaboratoryOperations/Demanda',
            icon: 'mdi mdi-chart-bar',
            permission: 'authenticated',
          },
          {
            label: 'Calendarios',
            path: '/LaboratoryOperations/Calendario',
            icon: 'mdi mdi-calendar-range',
            permission: 'authenticated',
          },
        ],
      },
    ],
  },
  {
    caption: 'Elementos',
    entries: [
      {
        id: 'management-processes',
        label: 'Procesos',
        icon: 'mdi mdi-cube-outline',
        links: [
          {
            label: 'Gestiones',
            path: '/Managements/Index?type=Preventive',
            icon: 'fas fa-boxes text-info',
            permission: 'admin',
          },
          {
            label: 'Man. Preventivos',
            path: '/',
            icon: 'mdi mdi-view-dashboard',
            permission: 'authenticated',
          },
          {
            label: 'Man. Correctivos',
            path: '/Managements/Index?handler=CurrentCorrective',
            icon: 'mdi mdi-view-dashboard',
            permission: 'admin',
          },
        ],
      },
    ],
  },
  {
    caption: 'Equipos y Actividades',
    entries: [
      {
        label: 'Equipo',
        path: '/Equipment/Index',
        icon: 'mdi mdi-cube-outline',
        permission: 'admin',
      },
      {
        id: 'activity-processes',
        label: 'Procesos',
        icon: 'mdi mdi-wrench',
        links: [
          {
            label: 'Verificaciones (L6)',
            path: '/Verifications/Index',
            icon: 'mdi mdi-check-circle-outline',
            permission: 'admin',
          },
          {
            label: 'Solicitudes de Servicio (L-7)',
            path: '/Requests/Index',
            icon: 'mdi mdi-message-text-outline',
            permission: 'authenticated',
          },
          {
            label: 'Solicitudes de Adquisición',
            path: '/Acquisitions/Index',
            icon: 'mdi mdi-cart-outline',
            permission: 'authenticated',
          },
          {
            label: 'Mantenimiento (L-8)',
            path: '/Maintenances/Index',
            icon: 'mdi mdi-view-quilt',
            permission: 'admin',
          },
          {
            label: 'Salidas (L-3)',
            path: '/Departures/Index',
            icon: 'fas fa-sign-out-alt',
            permission: 'admin',
          },
        ],
      },
      {
        label: 'Reportes oficiales',
        path: '/Reports/Index',
        icon: 'mdi mdi-file-document-multiple-outline',
        permission: 'authenticated',
      },
      {
        id: 'catalogs',
        label: 'Catálogos',
        icon: 'mdi mdi-book-open-variant',
        links: [
          {
            label: 'Laboratorios',
            path: '/Laboratories/Index',
            icon: 'mdi mdi-home-modern',
            permission: 'admin',
          },
          { label: 'Regiones', path: '/Cities/Index', icon: 'mdi mdi-earth', permission: 'admin' },
        ],
      },
    ],
  },
  {
    caption: 'Personas',
    entries: [
      {
        id: 'users',
        label: 'Usuarios',
        icon: 'mdi mdi-account-key',
        links: [
          {
            label: 'Usuarios',
            path: '/Users/Index',
            icon: 'mdi mdi-account-multiple',
            permission: 'admin',
          },
          {
            label: 'Personas',
            path: '/Persons/Index',
            icon: 'mdi mdi-account-group-outline',
            permission: 'admin',
          },
        ],
      },
    ],
  },
] as const;

function isNavigationGroup(entry: NavigationLink | NavigationGroup): entry is NavigationGroup {
  return 'links' in entry;
}

function currentLocation(): string {
  return `${window.location.pathname}${window.location.search}`;
}

function routePath(route: string): string {
  return route.split('?')[0] || '/';
}

function isUnauthorized(error: unknown): boolean {
  return error instanceof ApiClientError && error.status === 401;
}

function isAdmin(session: ActiveSiteSession, context: SiteRequestContext | null): boolean {
  return (
    session.globalRole === GlobalRole.SuperAdmin || context?.siteRole === SiteRole.Administrador
  );
}

function initials(name: string): string {
  return (
    name
      .trim()
      .split(/\s+/)
      .slice(0, 2)
      .map((part) => part.charAt(0).toUpperCase())
      .join('') || 'US'
  );
}

function findRouteTitle(route: string): string {
  const path = routePath(route);
  if (route.includes('ShowWizard=true')) return 'Wizard de gestión';
  if (route === '/') return 'Dashboard de Gestión';
  if (route === '/Users/Details') return 'Mi Perfil';
  for (const section of NAVIGATION) {
    for (const entry of section.entries) {
      if (isNavigationGroup(entry)) {
        const match = entry.links.find(
          (link) => link.path === route || routePath(link.path) === path,
        );
        if (match) return match.label;
      } else if (entry.path === route || routePath(entry.path) === path) return entry.label;
    }
  }
  return 'Dashboard de Gestión';
}

function isAllowedRoute(route: string, admin: boolean): boolean {
  const path = routePath(route);
  if (route === '/' || route === '/Users/Details' || route.includes('ShowWizard=true')) return true;
  for (const section of NAVIGATION) {
    for (const entry of section.entries) {
      const links = isNavigationGroup(entry) ? entry.links : [entry];
      const match = links.find((link) => link.path === route || routePath(link.path) === path);
      if (match) return match.permission === 'authenticated' || admin;
    }
  }
  return false;
}

function DashboardPanel({
  summary,
  loading,
  error,
}: {
  readonly summary: DashboardSummary | null;
  readonly loading: boolean;
  readonly error: string | null;
}): ReactElement {
  if (loading) {
    return (
      <section className="dashboard-empty-card" aria-live="polite">
        <i className="mdi mdi-loading mdi-spin" aria-hidden="true" />
        <span>Cargando datos de la gestión…</span>
      </section>
    );
  }
  if (error !== null) {
    return (
      <section className="dashboard-empty-card dashboard-error-card" role="alert">
        <i className="mdi mdi-alert-circle-outline" aria-hidden="true" />
        <span>{error}</span>
      </section>
    );
  }
  if (summary === null || summary.activeManagement === null) {
    return (
      <section className="dashboard-empty-card">
        <i className="mdi mdi-view-dashboard-outline" aria-hidden="true" />
        <div>
          <strong>No hay una gestión activa para esta sede</strong>
          <span>Los indicadores aparecerán cuando exista una gestión operativa.</span>
        </div>
      </section>
    );
  }
  const metrics = [
    ['Activos', summary.metrics.totalAssets, 'mdi mdi-package-variant-closed text-info'],
    ['Completados', summary.metrics.completedAssets, 'mdi mdi-check-circle-outline text-success'],
    ['Vencidos', summary.metrics.overdueAssets, 'mdi mdi-clock-alert-outline text-danger'],
    ['Progreso', `${summary.metrics.globalProgress}%`, 'mdi mdi-chart-line text-primary'],
  ] as const;
  return (
    <section className="dashboard-panel" aria-label="Dashboard operativo">
      <div className="dashboard-heading">
        <div>
          <span className="welcome-kicker">Gestión activa</span>
          <h2>{summary.activeManagement.code}</h2>
          <p>{summary.activeManagement.type === 'Corrective' ? 'Correctiva' : 'Preventiva'}</p>
        </div>
        <span className="dashboard-status">{summary.activeManagement.status}</span>
      </div>
      <div className="dashboard-metrics-grid">
        {metrics.map(([label, value, icon]) => (
          <article className="dashboard-metric-card" key={label}>
            <i className={icon} aria-hidden="true" />
            <span>{label}</span>
            <strong>{value}</strong>
          </article>
        ))}
      </div>
      <div className="dashboard-step-grid" aria-label="Avance por paso">
        {[
          ['L-6 Verificaciones', summary.metrics.l6],
          ['L-7 Solicitudes', summary.metrics.l7],
          ['L-8 Mantenimiento', summary.metrics.l8],
          ['L-3 Salidas', summary.metrics.departures],
          ['L-12 Desembolsos', summary.metrics.disbursements],
        ].map(([label, value]) => (
          <div className="dashboard-step" key={label}>
            <span>{label}</span>
            <strong>{value}</strong>
          </div>
        ))}
      </div>
      <div className="dashboard-table-card">
        <div className="dashboard-table-heading">
          <strong>Próximas actividades</strong>
          <span>{summary.plans.length} registros</span>
        </div>
        {summary.plans.length === 0 ? (
          <p className="dashboard-table-empty">No hay actividades planificadas para mostrar.</p>
        ) : (
          <div className="dashboard-plan-list">
            {summary.plans.map((plan) => (
              <div className="dashboard-plan-row" key={plan.id}>
                <span>
                  <strong>{plan.equipmentName ?? 'Equipo sin nombre'}</strong>
                  <small>
                    {plan.inventoryNumber ?? 'Sin inventario'} ·{' '}
                    {plan.laboratoryName ?? 'Sin laboratorio'}
                  </small>
                </span>
                <span className="dashboard-plan-date">
                  {plan.plannedDate === null
                    ? 'Sin fecha'
                    : new Date(plan.plannedDate).toLocaleDateString('es-BO')}
                </span>
              </div>
            ))}
          </div>
        )}
      </div>
    </section>
  );
}

function CatalogPanel({
  api,
  mode,
}: {
  readonly api: AuthApi;
  readonly mode: 'countries' | 'cities';
}): ReactElement {
  const [countries, setCountries] = useState<readonly Country[]>([]);
  const [cities, setCities] = useState<readonly City[]>([]);
  const [totalCount, setTotalCount] = useState(0);
  const [pageIndex, setPageIndex] = useState(1);
  const [totalPages, setTotalPages] = useState(0);
  const [searchTerm, setSearchTerm] = useState('');
  const [statusFilter, setStatusFilter] = useState<string>('');
  const [name, setName] = useState('');
  const [region, setRegion] = useState('');
  const [countryId, setCountryId] = useState<number | ''>('');
  const [status, setStatus] = useState<0 | 1 | 2>(0);
  const [editingId, setEditingId] = useState<number | null>(null);
  const [detail, setDetail] = useState<Country | City | null>(null);
  const [loading, setLoading] = useState(true);
  const [saving, setSaving] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [feedback, setFeedback] = useState<string | null>(null);

  const load = useCallback(async (): Promise<void> => {
    setLoading(true);
    setError(null);
    try {
      const query = {
        currentPage: pageIndex,
        searchTerm: searchTerm.trim() || undefined,
        statusFilter: statusFilter === '' ? undefined : (Number(statusFilter) as 0 | 1 | 2),
      };
      if (mode === 'countries') {
        if (api.countries === undefined) throw new Error('Catalog API unavailable.');
        const result = await api.countries(query);
        setCountries(result.items);
        setTotalCount(result.totalCount);
        setTotalPages(result.totalPages);
      } else {
        if (api.cities === undefined || api.countries === undefined) {
          throw new Error('Catalog API unavailable.');
        }
        const [cityPage, countryPage] = await Promise.all([api.cities(query), api.countries({})]);
        setCities(cityPage.items);
        setCountries(countryPage.items);
        setTotalCount(cityPage.totalCount);
        setTotalPages(cityPage.totalPages);
      }
    } catch {
      setError('No fue posible cargar el catálogo de esta sede.');
    } finally {
      setLoading(false);
    }
  }, [api, mode, pageIndex, searchTerm, statusFilter]);

  useEffect(() => {
    void load();
  }, [load]);

  function resetForm(): void {
    setEditingId(null);
    setName('');
    setRegion('');
    setCountryId('');
    setStatus(0);
    setDetail(null);
  }

  function beginCountryEdit(country: Country): void {
    setEditingId(country.id);
    setName(country.name);
    setStatus(country.status);
    setDetail(null);
  }

  function beginCityEdit(city: City): void {
    setEditingId(city.id);
    setName(city.name);
    setRegion(city.region ?? '');
    setCountryId(city.countryId);
    setStatus(city.status);
    setDetail(null);
  }

  async function submit(event: FormEvent<HTMLFormElement>): Promise<void> {
    event.preventDefault();
    if (saving) return;
    setSaving(true);
    setError(null);
    setFeedback(null);
    try {
      if (mode === 'countries') {
        if (editingId === null) {
          if (api.createCountry === undefined) throw new Error('Catalog API unavailable.');
          await api.createCountry({ name });
          setFeedback('País registrado correctamente.');
        } else {
          if (api.updateCountry === undefined) throw new Error('Catalog API unavailable.');
          await api.updateCountry(editingId, { name, status });
          setFeedback('País actualizado correctamente.');
        }
      } else {
        if (countryId === '') {
          setError('Selecciona un país para la ciudad.');
          return;
        }
        if (editingId === null) {
          if (api.createCity === undefined) throw new Error('Catalog API unavailable.');
          await api.createCity({ countryId, name, region: region || null });
          setFeedback('Ciudad registrada correctamente.');
        } else {
          if (api.updateCity === undefined) throw new Error('Catalog API unavailable.');
          await api.updateCity(editingId, { countryId, name, region: region || null, status });
          setFeedback('Ciudad actualizada correctamente.');
        }
      }
      resetForm();
      await load();
    } catch (caught) {
      setError(
        caught instanceof ApiClientError
          ? caught.failure.error.message
          : 'No fue posible guardar los cambios.',
      );
    } finally {
      setSaving(false);
    }
  }

  async function remove(id: number): Promise<void> {
    if (!window.confirm('¿Confirmas la baja lógica de este registro?')) return;
    setError(null);
    setFeedback(null);
    try {
      if (mode === 'countries') {
        if (api.deleteCountry === undefined) throw new Error('Catalog API unavailable.');
        await api.deleteCountry(id);
      } else {
        if (api.deleteCity === undefined) throw new Error('Catalog API unavailable.');
        await api.deleteCity(id);
      }
      setFeedback('Registro dado de baja correctamente.');
      if (editingId === id) resetForm();
      await load();
    } catch (caught) {
      setError(
        caught instanceof ApiClientError
          ? caught.failure.error.message
          : 'No fue posible dar de baja el registro.',
      );
    }
  }

  async function showDetail(id: number): Promise<void> {
    try {
      const value = mode === 'countries' ? await api.country?.(id) : await api.city?.(id);
      if (value !== undefined) setDetail(value);
    } catch {
      setError('No fue posible cargar el detalle.');
    }
  }

  const rows = mode === 'countries' ? countries : cities;
  const title = mode === 'countries' ? 'Países' : 'Ciudades';
  return (
    <section className="catalog-panel" aria-label={`Catálogo de ${title.toLocaleLowerCase('es')}`}>
      <div className="catalog-heading">
        <div>
          <span className="welcome-kicker">Catálogos operacionales</span>
          <h2>{title}</h2>
          <p>Datos reales de la sede activa, con historial y baja lógica.</p>
        </div>
        <span className="catalog-count">{totalCount} registros</span>
      </div>
      {feedback ? (
        <div className="alert alert-success" role="status">
          {feedback}
        </div>
      ) : null}
      {error ? (
        <div className="alert alert-danger" role="alert">
          {error}
        </div>
      ) : null}
      <div className="catalog-toolbar">
        <input
          className="form-control"
          aria-label="Buscar"
          placeholder={mode === 'countries' ? 'Buscar país…' : 'Buscar ciudad o país…'}
          value={searchTerm}
          onChange={(event) => {
            setPageIndex(1);
            setSearchTerm(event.target.value);
          }}
        />
        <select
          className="form-control"
          aria-label="Filtrar por estado"
          value={statusFilter}
          onChange={(event) => {
            setPageIndex(1);
            setStatusFilter(event.target.value);
          }}
        >
          <option value="">Todos los estados</option>
          <option value="0">Activo</option>
          <option value="1">Inactivo</option>
        </select>
        <button type="button" className="btn btn-outline-primary" onClick={() => resetForm()}>
          <i className="mdi mdi-plus" /> Nuevo
        </button>
      </div>
      <div className="catalog-layout">
        <form className="catalog-form" onSubmit={(event) => void submit(event)}>
          <h3>
            {editingId === null
              ? `Nuevo ${mode === 'countries' ? 'país' : 'ciudad'}`
              : 'Editar registro'}
          </h3>
          {mode === 'cities' ? (
            <label>
              País
              <select
                className="form-control"
                value={countryId}
                onChange={(event) =>
                  setCountryId(event.target.value === '' ? '' : Number(event.target.value))
                }
                required
              >
                <option value="">Selecciona un país</option>
                {countries
                  .filter((country) => country.status !== 2)
                  .map((country) => (
                    <option key={country.id} value={country.id}>
                      {country.name}
                    </option>
                  ))}
              </select>
            </label>
          ) : null}
          <label>
            {mode === 'countries' ? 'Nombre del país' : 'Nombre de la ciudad'}
            <input
              className="form-control"
              value={name}
              onChange={(event) => setName(event.target.value)}
              required
              maxLength={100}
            />
          </label>
          {mode === 'cities' ? (
            <label>
              Región/Departamento
              <input
                className="form-control"
                value={region}
                onChange={(event) => setRegion(event.target.value)}
                maxLength={100}
              />
            </label>
          ) : null}
          {editingId !== null ? (
            <label>
              Estado
              <select
                className="form-control"
                value={status}
                onChange={(event) => setStatus(Number(event.target.value) as 0 | 1 | 2)}
              >
                <option value={0}>Activo</option>
                <option value={1}>Inactivo</option>
                <option value={2}>Eliminado</option>
              </select>
            </label>
          ) : null}
          <div className="catalog-form-actions">
            <button type="submit" className="btn btn-primary" disabled={saving}>
              {saving ? 'Guardando…' : 'Guardar'}
            </button>
            {editingId !== null ? (
              <button type="button" className="btn btn-light" onClick={() => resetForm()}>
                Cancelar
              </button>
            ) : null}
          </div>
        </form>
        <div className="catalog-table-wrap">
          {loading ? (
            <p className="catalog-empty">Cargando catálogo…</p>
          ) : rows.length === 0 ? (
            <p className="catalog-empty">No hay registros para los filtros seleccionados.</p>
          ) : (
            <table className="catalog-table">
              <thead>
                <tr>
                  <th>{mode === 'countries' ? 'País' : 'Ciudad'}</th>
                  {mode === 'cities' ? <th>País</th> : <th>Ciudades</th>}
                  <th>Estado</th>
                  <th>Acciones</th>
                </tr>
              </thead>
              <tbody>
                {mode === 'countries'
                  ? countries.map((country) => (
                      <tr key={country.id}>
                        <td>{country.name}</td>
                        <td>{country.cityCount}</td>
                        <td>{country.status === 0 ? 'Activo' : 'Inactivo'}</td>
                        <td className="catalog-actions">
                          <button type="button" onClick={() => void showDetail(country.id)}>
                            Detalle
                          </button>
                          <button type="button" onClick={() => beginCountryEdit(country)}>
                            Editar
                          </button>
                          <button type="button" onClick={() => void remove(country.id)}>
                            Baja
                          </button>
                        </td>
                      </tr>
                    ))
                  : cities.map((city) => (
                      <tr key={city.id}>
                        <td>
                          {city.name}
                          {city.region ? <small>{city.region}</small> : null}
                        </td>
                        <td>{city.countryName}</td>
                        <td>{city.status === 0 ? 'Activo' : 'Inactivo'}</td>
                        <td className="catalog-actions">
                          <button type="button" onClick={() => void showDetail(city.id)}>
                            Detalle
                          </button>
                          <button type="button" onClick={() => beginCityEdit(city)}>
                            Editar
                          </button>
                          <button type="button" onClick={() => void remove(city.id)}>
                            Baja
                          </button>
                        </td>
                      </tr>
                    ))}
              </tbody>
            </table>
          )}
          {totalPages > 1 ? (
            <div className="catalog-pagination">
              <button
                type="button"
                disabled={pageIndex <= 1}
                onClick={() => setPageIndex((value) => value - 1)}
              >
                Anterior
              </button>
              <span>
                Página {pageIndex} de {totalPages}
              </span>
              <button
                type="button"
                disabled={pageIndex >= totalPages}
                onClick={() => setPageIndex((value) => value + 1)}
              >
                Siguiente
              </button>
            </div>
          ) : null}
        </div>
      </div>
      {detail ? (
        <div className="catalog-detail">
          <strong>Detalle</strong>
          <span>{detail.name}</span>
          <small>
            {'countryName' in detail
              ? `${detail.countryName}${detail.region ? ` · ${detail.region}` : ''}`
              : `${detail.cityCount} ciudades asociadas`}
          </small>
          <button type="button" onClick={() => setDetail(null)}>
            Cerrar
          </button>
        </div>
      ) : null}
    </section>
  );
}

function AcademicPanel({ api }: { readonly api: AuthApi }): ReactElement {
  const [labs, setLabs] = useState<readonly Laboratory[]>([]);
  const [faculties, setFaculties] = useState<readonly Faculty[]>([]);
  const [cities, setCities] = useState<readonly City[]>([]);
  const [loading, setLoading] = useState(true);
  const [saving, setSaving] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [feedback, setFeedback] = useState<string | null>(null);
  const [editingId, setEditingId] = useState<number | null>(null);
  const [facultyId, setFacultyId] = useState<number | ''>('');
  const [cityId, setCityId] = useState<number | ''>('');
  const [code, setCode] = useState('');
  const [name, setName] = useState('');
  const [floor, setFloor] = useState('');
  const [status, setStatus] = useState<0 | 1 | 2>(0);

  const load = useCallback(async (): Promise<void> => {
    if (api.laboratories === undefined || api.faculties === undefined || api.cities === undefined) {
      setError('La API académica no está disponible.');
      setLoading(false);
      return;
    }
    setLoading(true);
    try {
      const [labPage, facultyPage, cityPage] = await Promise.all([
        api.laboratories({ currentPage: 1 }),
        api.faculties({ currentPage: 1 }),
        api.cities({ currentPage: 1 }),
      ]);
      setLabs(labPage.items);
      setFaculties(facultyPage.items);
      setCities(cityPage.items);
      setError(null);
    } catch {
      setError('No fue posible cargar los laboratorios de la sede.');
    } finally {
      setLoading(false);
    }
  }, [api]);

  useEffect(() => {
    void load();
  }, [load]);

  function reset(): void {
    setEditingId(null);
    setFacultyId('');
    setCityId('');
    setCode('');
    setName('');
    setFloor('');
    setStatus(0);
  }

  function edit(lab: Laboratory): void {
    setEditingId(lab.id);
    setFacultyId(lab.facultyId);
    setCityId(lab.cityId ?? '');
    setCode(lab.code);
    setName(lab.name);
    setFloor(lab.floor ?? '');
    setStatus(lab.status);
  }

  async function submit(event: FormEvent<HTMLFormElement>): Promise<void> {
    event.preventDefault();
    if (saving || facultyId === '') return;
    if (api.createLaboratory === undefined || api.updateLaboratory === undefined) return;
    setSaving(true);
    setError(null);
    setFeedback(null);
    const base = {
      facultyId,
      code,
      name,
      floor: floor || null,
      cityId: cityId === '' ? null : cityId,
    };
    try {
      if (editingId === null) {
        await api.createLaboratory(base);
        setFeedback('Laboratorio registrado correctamente.');
      } else {
        await api.updateLaboratory(editingId, { ...base, status });
        setFeedback('Laboratorio actualizado correctamente.');
      }
      reset();
      await load();
    } catch (caught) {
      setError(
        caught instanceof ApiClientError
          ? caught.failure.error.message
          : 'No fue posible guardar el laboratorio.',
      );
    } finally {
      setSaving(false);
    }
  }

  async function remove(id: number): Promise<void> {
    if (
      !window.confirm('¿Confirmas la baja lógica del laboratorio?') ||
      api.deleteLaboratory === undefined
    )
      return;
    try {
      await api.deleteLaboratory(id);
      setFeedback('Laboratorio dado de baja correctamente.');
      if (editingId === id) reset();
      await load();
    } catch {
      setError('No fue posible dar de baja el laboratorio.');
    }
  }

  return (
    <section className="catalog-panel" aria-label="Catálogo de laboratorios">
      <div className="catalog-heading">
        <div>
          <span className="welcome-kicker">Facultades, carreras y sede activa</span>
          <h2>Laboratorios</h2>
          <p>
            La facultad se resuelve en el control plane y el laboratorio permanece en la base de la
            sede.
          </p>
        </div>
        <span className="catalog-count">{labs.length} visibles</span>
      </div>
      {feedback ? (
        <div className="alert alert-success" role="status">
          {feedback}
        </div>
      ) : null}
      {error ? (
        <div className="alert alert-danger" role="alert">
          {error}
        </div>
      ) : null}
      <div className="catalog-layout">
        <form className="catalog-form" onSubmit={(event) => void submit(event)}>
          <h3>{editingId === null ? 'Nuevo laboratorio' : 'Editar laboratorio'}</h3>
          <label>
            Facultad
            <select
              className="form-control"
              value={facultyId}
              onChange={(event) =>
                setFacultyId(event.target.value === '' ? '' : Number(event.target.value))
              }
              required
            >
              <option value="">Selecciona una facultad</option>
              {faculties
                .filter((item) => item.status !== 2)
                .map((item) => (
                  <option key={item.id} value={item.id}>
                    {item.name}
                  </option>
                ))}
            </select>
          </label>
          <label>
            Código
            <input
              className="form-control"
              value={code}
              onChange={(event) => setCode(event.target.value)}
              maxLength={20}
              required
            />
          </label>
          <label>
            Nombre
            <input
              className="form-control"
              value={name}
              onChange={(event) => setName(event.target.value)}
              maxLength={200}
              required
            />
          </label>
          <label>
            Ciudad
            <select
              className="form-control"
              value={cityId}
              onChange={(event) =>
                setCityId(event.target.value === '' ? '' : Number(event.target.value))
              }
            >
              <option value="">Sin ciudad</option>
              {cities
                .filter((item) => item.status !== 2)
                .map((item) => (
                  <option key={item.id} value={item.id}>
                    {item.name} · {item.countryName}
                  </option>
                ))}
            </select>
          </label>
          <label>
            Piso/Nivel
            <input
              className="form-control"
              value={floor}
              onChange={(event) => setFloor(event.target.value)}
              maxLength={50}
            />
          </label>
          {editingId !== null ? (
            <label>
              Estado
              <select
                className="form-control"
                value={status}
                onChange={(event) => setStatus(Number(event.target.value) as 0 | 1 | 2)}
              >
                <option value={0}>Activo</option>
                <option value={1}>Inactivo</option>
                <option value={2}>Eliminado</option>
              </select>
            </label>
          ) : null}
          <div className="catalog-form-actions">
            <button type="submit" className="btn btn-primary" disabled={saving}>
              {saving ? 'Guardando…' : 'Guardar'}
            </button>
            {editingId !== null ? (
              <button type="button" className="btn btn-light" onClick={() => reset()}>
                Cancelar
              </button>
            ) : null}
          </div>
        </form>
        <div className="catalog-table-wrap">
          {loading ? (
            <p className="catalog-empty">Cargando laboratorios…</p>
          ) : labs.length === 0 ? (
            <p className="catalog-empty">No hay laboratorios registrados en esta sede.</p>
          ) : (
            <table className="catalog-table">
              <thead>
                <tr>
                  <th>Código</th>
                  <th>Laboratorio</th>
                  <th>Facultad</th>
                  <th>Ciudad</th>
                  <th>Acciones</th>
                </tr>
              </thead>
              <tbody>
                {labs.map((lab) => (
                  <tr key={lab.id}>
                    <td>{lab.code}</td>
                    <td>
                      {lab.name}
                      {lab.floor ? <small>Piso {lab.floor}</small> : null}
                    </td>
                    <td>{lab.facultyName ?? `Facultad #${lab.facultyId}`}</td>
                    <td>{lab.cityName ?? 'Sin ciudad'}</td>
                    <td className="catalog-actions">
                      <button type="button" onClick={() => edit(lab)}>
                        Editar
                      </button>
                      <button type="button" onClick={() => void remove(lab.id)}>
                        Baja
                      </button>
                    </td>
                  </tr>
                ))}
              </tbody>
            </table>
          )}
        </div>
      </div>
    </section>
  );
}

export function App({ api = defaultApi }: { readonly api?: AuthApi }): ReactElement {
  const [view, setView] = useState<View>('loading');
  const [session, setSession] = useState<ActiveSiteSession | null>(null);
  const [context, setContext] = useState<SiteRequestContext | null>(null);
  const [email, setEmail] = useState('');
  const [password, setPassword] = useState('');
  const [rememberMe, setRememberMe] = useState(false);
  const [busy, setBusy] = useState(false);
  const [message, setMessage] = useState<string | null>(null);
  const [route, setRoute] = useState(currentLocation);
  const [sidebarOpen, setSidebarOpen] = useState(false);
  const [miniSidebar, setMiniSidebar] = useState(false);
  const [profileOpen, setProfileOpen] = useState(false);
  const [siteOpen, setSiteOpen] = useState(false);
  const [searchOpen, setSearchOpen] = useState(false);
  const [notificationsOpen, setNotificationsOpen] = useState(false);
  const [notifications, setNotifications] = useState<readonly DashboardNotification[]>([]);
  const [dashboard, setDashboard] = useState<DashboardSummary | null>(null);
  const [dashboardLoading, setDashboardLoading] = useState(false);
  const [dashboardError, setDashboardError] = useState<string | null>(null);
  const [navigationSearch, setNavigationSearch] = useState('');
  const [openGroups, setOpenGroups] = useState<ReadonlySet<string>>(new Set());

  const enterSession = useCallback(
    async (current: ActiveSiteSession, cancelled = false): Promise<void> => {
      setSession(current);
      setMessage(null);
      if (current.activeSiteId === null) {
        if (!cancelled) setView('site-picker');
        return;
      }
      try {
        const siteContext = await api.siteContext();
        if (!cancelled) {
          setContext(siteContext);
          setView('workspace');
        }
      } catch (error) {
        if (!cancelled) setView(isUnauthorized(error) ? 'login' : 'unavailable');
      }
    },
    [api],
  );

  useEffect(() => {
    let cancelled = false;
    void api
      .session()
      .then(async (current) => {
        if (!cancelled) await enterSession(current, cancelled);
      })
      .catch((error: unknown) => {
        if (!cancelled) setView(isUnauthorized(error) ? 'login' : 'unavailable');
      });
    return () => {
      cancelled = true;
    };
  }, [api, enterSession]);

  useEffect(() => {
    if (view !== 'workspace' || route !== '/' || api.dashboard === undefined) return;
    let cancelled = false;
    setDashboardLoading(true);
    setDashboardError(null);
    void api
      .dashboard({ currentPage: 1 })
      .then((value) => {
        if (!cancelled) setDashboard(value);
      })
      .catch(() => {
        if (!cancelled) setDashboardError('No fue posible cargar el dashboard de esta sede.');
      })
      .finally(() => {
        if (!cancelled) setDashboardLoading(false);
      });
    return () => {
      cancelled = true;
    };
  }, [api, route, view]);

  useEffect(() => {
    if (view !== 'workspace' || api.notifications === undefined) return;
    let cancelled = false;
    void api
      .notifications({ unreadOnly: true })
      .then((value) => {
        if (!cancelled) setNotifications(value);
      })
      .catch(() => {
        if (!cancelled) setNotifications([]);
      });
    return () => {
      cancelled = true;
    };
  }, [api, view]);

  useEffect(() => {
    const onPopState = (): void => setRoute(currentLocation());
    window.addEventListener('popstate', onPopState);
    return () => window.removeEventListener('popstate', onPopState);
  }, []);

  const admin = session ? isAdmin(session, context) : false;
  const allowedSections = useMemo(() => {
    const query = navigationSearch.trim().toLocaleLowerCase('es');
    const allowed = (link: NavigationLink): boolean =>
      (link.permission === 'authenticated' || admin) &&
      (query === '' || link.label.toLocaleLowerCase('es').includes(query));
    return NAVIGATION.map((section) => ({
      ...section,
      entries: section.entries
        .map((entry): NavigationLink | NavigationGroup | null => {
          if (!isNavigationGroup(entry)) return allowed(entry) ? entry : null;
          const links = entry.links.filter(allowed);
          return links.length > 0 ? { ...entry, links } : null;
        })
        .filter((entry): entry is NavigationLink | NavigationGroup => entry !== null),
    })).filter((section) => section.entries.length > 0);
  }, [admin, navigationSearch]);

  useEffect(() => {
    if (view === 'workspace' && !isAllowedRoute(route, admin)) {
      window.history.replaceState({}, '', '/');
      setRoute('/');
    }
  }, [admin, route, view]);

  useEffect(() => {
    for (const section of allowedSections) {
      for (const entry of section.entries) {
        if (
          isNavigationGroup(entry) &&
          entry.links.some(
            (link) => link.path === route || routePath(link.path) === routePath(route),
          )
        ) {
          setOpenGroups((current) => new Set([...current, entry.id]));
          return;
        }
      }
    }
  }, [allowedSections, route]);

  async function submitLogin(event: FormEvent<HTMLFormElement>): Promise<void> {
    event.preventDefault();
    if (busy) return;
    setBusy(true);
    setMessage(null);
    try {
      const current = await api.login({ email, password, rememberMe });
      setPassword('');
      await enterSession(current);
    } catch (error) {
      setMessage(
        error instanceof ApiClientError && error.status === 429
          ? 'Demasiados intentos. Intente más tarde.'
          : isUnauthorized(error)
            ? 'Usuario o contraseña incorrectos.'
            : 'No fue posible conectar con el servicio. Intente nuevamente.',
      );
      setView('login');
    } finally {
      setBusy(false);
    }
  }

  async function chooseSite(activeSiteId: SetActiveSiteRequest['activeSiteId']): Promise<void> {
    if (busy) return;
    setBusy(true);
    setMessage(null);
    try {
      const current = await api.setActiveSite({ activeSiteId });
      setSiteOpen(false);
      await enterSession(current);
    } catch {
      setMessage('No fue posible activar la sede seleccionada.');
    } finally {
      setBusy(false);
    }
  }

  async function logout(): Promise<void> {
    if (busy) return;
    setBusy(true);
    try {
      await api.logout();
    } finally {
      setSession(null);
      setContext(null);
      setEmail('');
      setPassword('');
      setRememberMe(false);
      setMessage(null);
      setProfileOpen(false);
      setBusy(false);
      setView('login');
      window.history.replaceState({}, '', '/');
      setRoute('/');
    }
  }

  function navigateTo(path: string): void {
    if (path !== route) window.history.pushState({}, '', path);
    setRoute(path);
    setSidebarOpen(false);
    setProfileOpen(false);
  }

  function navigate(event: MouseEvent<HTMLAnchorElement>, path: string): void {
    event.preventDefault();
    navigateTo(path);
  }

  function toggleGroup(id: string): void {
    setOpenGroups((current) => {
      const next = new Set(current);
      if (next.has(id)) next.delete(id);
      else next.add(id);
      return next;
    });
  }

  if (view === 'loading') {
    return (
      <main className="loading-stage" aria-live="polite">
        <div className="lds-ripple" aria-hidden="true">
          <div />
          <div />
        </div>
        <span>Validando sesión…</span>
      </main>
    );
  }

  if (view === 'unavailable') {
    return (
      <main className="service-unavailable" role="alert">
        <i className="mdi mdi-alert-circle-outline" aria-hidden="true" />
        <h1>Servicio temporalmente no disponible</h1>
        <p>No fue posible conectar con la API local de Laboratorios Univalle.</p>
        <button type="button" className="btn btn-primary" onClick={() => window.location.reload()}>
          Reintentar
        </button>
      </main>
    );
  }

  if (view === 'login') {
    return (
      <main className="auth-wrapper" aria-label="Ingreso al sistema">
        <div className="auth-overlay" />
        <div className="auth-box">
          <section className="auth-left" aria-label="Laboratorios Univalle">
            <img src={logoIcon} alt="Logo Univalle" />
            <h1 className="brand-title">Univalle</h1>
            <p className="brand-subtitle">
              Gestión de
              <br />
              Gastronomía
            </p>
          </section>
          <section className="auth-right">
            <div className="login-heading">
              <h2 className="form-title" aria-label="Ingreso al Sistema">
                Ingreso
                <br />
                al Sistema
              </h2>
              <p className="form-subtitle">Portal de Gestión Administrativa de Laboratorios</p>
            </div>
            <form id="login-form" onSubmit={(event) => void submitLogin(event)}>
              {message ? (
                <div className="alert alert-danger" role="alert">
                  {message}
                </div>
              ) : null}
              <label className="form-label-custom" htmlFor="login-email">
                Usuario o Correo
              </label>
              <div className="input-group-custom">
                <span className="input-group-text" aria-hidden="true">
                  <i className="ti-user" />
                </span>
                <input
                  id="login-email"
                  className="form-control"
                  name="email"
                  type="text"
                  autoComplete="username"
                  value={email}
                  onChange={(event) => setEmail(event.target.value)}
                  required
                  maxLength={254}
                  autoFocus
                  placeholder="Ej: admin@univalle.edu"
                />
              </div>
              <label className="form-label-custom" htmlFor="login-password">
                Contraseña
              </label>
              <div className="input-group-custom">
                <span className="input-group-text" aria-hidden="true">
                  <i className="ti-lock" />
                </span>
                <input
                  id="login-password"
                  className="form-control"
                  name="password"
                  type="password"
                  autoComplete="current-password"
                  value={password}
                  onChange={(event) => setPassword(event.target.value)}
                  required
                  maxLength={72}
                  placeholder="••••••••"
                />
              </div>
              <div className="custom-checkbox-container">
                <input
                  id="remember-me"
                  type="checkbox"
                  checked={rememberMe}
                  onChange={(event) => setRememberMe(event.target.checked)}
                />
                <label htmlFor="remember-me">Recordarme en este equipo</label>
              </div>
              <button className="btn-login-glow" type="submit" disabled={busy}>
                {busy ? 'VALIDANDO…' : 'INICIAR SESIÓN'}
              </button>
              <p className="login-footer">
                © {new Date().getUTCFullYear()} Universidad del Valle
                <br /> Sistema de Gestión de Laboratorios
              </p>
            </form>
          </section>
        </div>
      </main>
    );
  }

  if (view === 'site-picker' && session) {
    return (
      <main className="site-picker-stage">
        <section className="site-picker-card">
          <img src={logoIcon} alt="Logo Univalle" />
          <span className="site-picker-eyebrow">Contexto de trabajo</span>
          <h1>Selecciona una sede</h1>
          <p>Solo se muestran las membresías activas de tu cuenta.</p>
          {message ? (
            <div className="alert alert-danger" role="alert">
              {message}
            </div>
          ) : null}
          <div className="site-grid">
            {session.memberships.map((membership) => (
              <button
                className="site-card"
                type="button"
                key={membership.siteId}
                disabled={busy}
                onClick={() => void chooseSite(membership.siteId)}
              >
                <span className="site-icon" aria-hidden="true">
                  <i className="mdi mdi-map-marker" />
                </span>
                <span>
                  <strong>{membership.siteName}</strong>
                  <small>{membership.role}</small>
                </span>
                <i className="mdi mdi-chevron-right" aria-hidden="true" />
              </button>
            ))}
          </div>
        </section>
      </main>
    );
  }

  if (!session || !context) return <main className="loading-stage">Validando contexto…</main>;

  const title = findRouteTitle(route);
  const displayName = session.displayName.trim() || session.email;

  return (
    <div
      id="main-wrapper"
      className={`app-shell${miniSidebar ? ' mini-sidebar' : ''}${sidebarOpen ? ' show-sidebar' : ''}`}
    >
      <header className="topbar">
        <nav
          className="navbar top-navbar navbar-expand-md navbar-dark"
          aria-label="Cabecera principal"
        >
          <div className="navbar-header">
            <button
              className="nav-toggler waves-effect waves-light d-md-none"
              type="button"
              aria-label="Abrir menú"
              aria-expanded={sidebarOpen}
              onClick={() => setSidebarOpen((open) => !open)}
            >
              <i className="ti-menu" />
            </button>
            <div className="navbar-brand">
              <a className="logo" href="/" onClick={(event) => navigate(event, '/')}>
                <span className="logo-icon">
                  <img src={logoLightIcon} alt="Logo Univalle" />
                </span>
                <span className="logo-text">LABORATORIOS</span>
              </a>
              <button
                className="sidebartoggler d-none d-md-inline-flex"
                type="button"
                aria-label={miniSidebar ? 'Expandir menú' : 'Contraer menú'}
                onClick={() => setMiniSidebar((mini) => !mini)}
              >
                <i className={`mdi mdi-toggle-switch${miniSidebar ? '-off' : ''}`} />
              </button>
            </div>
          </div>
          <div className="navbar-collapse">
            <div className={`search-box${searchOpen ? ' open' : ''}`}>
              <button
                className="nav-link search-toggle"
                type="button"
                aria-label="Buscar en el menú"
                aria-expanded={searchOpen}
                onClick={() => setSearchOpen((open) => !open)}
              >
                <i className="mdi mdi-magnify font-20" />
                <span>Buscar</span>
              </button>
              {searchOpen ? (
                <div className="app-search">
                  <label className="sr-only" htmlFor="navigation-search">
                    Buscar una opción del menú
                  </label>
                  <input
                    id="navigation-search"
                    type="search"
                    className="form-control"
                    placeholder="Buscar opción"
                    value={navigationSearch}
                    onChange={(event) => setNavigationSearch(event.target.value)}
                    autoFocus
                  />
                  <button
                    type="button"
                    className="srh-btn"
                    aria-label="Cerrar búsqueda"
                    onClick={() => {
                      setSearchOpen(false);
                      setNavigationSearch('');
                    }}
                  >
                    <i className="ti-close" />
                  </button>
                </div>
              ) : null}
            </div>
            <ul className="navbar-nav ml-auto align-items-center">
              <li className="nav-item site-switcher">
                <button
                  className="nav-link site-button"
                  type="button"
                  aria-expanded={siteOpen}
                  onClick={() => {
                    setSiteOpen((open) => !open);
                    setProfileOpen(false);
                  }}
                >
                  <i className="mdi mdi-map-marker" aria-hidden="true" />
                  <span className="d-none d-sm-inline">{context.siteName}</span>
                  <i className="mdi mdi-chevron-down" aria-hidden="true" />
                </button>
                {siteOpen ? (
                  <div className="dropdown-menu site-menu show">
                    <div className="dropdown-heading">Sede activa</div>
                    {session.memberships.map((membership) => (
                      <button
                        type="button"
                        className={`dropdown-item${membership.siteId === session.activeSiteId ? ' active' : ''}`}
                        key={membership.siteId}
                        disabled={busy || membership.siteId === session.activeSiteId}
                        onClick={() => void chooseSite(membership.siteId)}
                      >
                        <i className="mdi mdi-map-marker-outline" />
                        <span>
                          <strong>{membership.siteName}</strong>
                          <small>{membership.role}</small>
                        </span>
                      </button>
                    ))}
                  </div>
                ) : null}
              </li>
              <li className="nav-item">
                <button
                  className="nav-link notification-button"
                  type="button"
                  aria-label="Notificaciones"
                  aria-expanded={notificationsOpen}
                  onClick={() => {
                    setNotificationsOpen((open) => !open);
                    setSiteOpen(false);
                    setProfileOpen(false);
                  }}
                >
                  <i className="mdi mdi-bell font-24" />
                  {notifications.length > 0 ? (
                    <span className="notification-badge">{notifications.length}</span>
                  ) : null}
                </button>
                {notificationsOpen ? (
                  <div className="dropdown-menu notifications-menu show">
                    <div className="dropdown-heading">
                      <span>Notificaciones</span>
                      {notifications.length > 0 && api.markAllNotificationsRead ? (
                        <button
                          type="button"
                          className="notifications-clear"
                          onClick={() => {
                            void api.markAllNotificationsRead!().then(() => setNotifications([]));
                          }}
                        >
                          Marcar todas
                        </button>
                      ) : null}
                    </div>
                    {notifications.length === 0 ? (
                      <p className="notifications-empty">No tienes notificaciones pendientes.</p>
                    ) : (
                      notifications.map((notification) => (
                        <button
                          type="button"
                          className="notification-item"
                          key={notification.id}
                          onClick={() => {
                            if (api.markNotificationRead) {
                              void api
                                .markNotificationRead(notification.id)
                                .then(() =>
                                  setNotifications((current) =>
                                    current.filter((item) => item.id !== notification.id),
                                  ),
                                );
                            }
                          }}
                        >
                          <i className={notification.iconClass ?? 'mdi mdi-information-outline'} />
                          <span>
                            <strong>{notification.title}</strong>
                            <small>{notification.message}</small>
                          </span>
                        </button>
                      ))
                    )}
                  </div>
                ) : null}
              </li>
              <li className="nav-item profile-menu">
                <button
                  className="nav-link profile-button"
                  type="button"
                  aria-expanded={profileOpen}
                  onClick={() => {
                    setProfileOpen((open) => !open);
                    setSiteOpen(false);
                  }}
                >
                  <span className="profile-avatar">{initials(displayName)}</span>
                  <span className="profile-name d-none d-sm-inline">{displayName}</span>
                  <i className="mdi mdi-chevron-down" aria-hidden="true" />
                </button>
                {profileOpen ? (
                  <div className="dropdown-menu user-menu show">
                    <div className="user-summary">
                      <span className="profile-avatar profile-avatar-lg">
                        {initials(displayName)}
                      </span>
                      <span>
                        <strong>{displayName}</strong>
                        <small>{session.email}</small>
                        <small>{context.siteRole}</small>
                      </span>
                    </div>
                    {api.profile ? (
                      <a
                        className="dropdown-item"
                        href="/Users/Details"
                        onClick={(event) => navigate(event, '/Users/Details')}
                      >
                        <i className="ti-user" /> Mi Perfil
                      </a>
                    ) : null}
                    <button
                      type="button"
                      className="dropdown-item logout-button"
                      onClick={() => void logout()}
                      disabled={busy}
                    >
                      <i className="fa fa-power-off" /> Cerrar Sesión
                    </button>
                  </div>
                ) : null}
              </li>
            </ul>
          </div>
        </nav>
      </header>

      <aside className="left-sidebar" aria-label="Navegación principal">
        <div className="scroll-sidebar">
          <nav className="sidebar-nav" aria-label="Navegación principal">
            <ul id="sidebarnav">
              {allowedSections.map((section) => (
                <li className="sidebar-section" key={section.caption}>
                  <div className="nav-small-cap">
                    <i className="mdi mdi-dots-horizontal" />
                    <span className="hide-menu">{section.caption}</span>
                  </div>
                  <ul>
                    {section.entries.map((entry) => {
                      if (!isNavigationGroup(entry)) {
                        return (
                          <li className="sidebar-item" key={entry.path}>
                            <a
                              className={`sidebar-link${route === entry.path ? ' active' : ''}`}
                              href={entry.path}
                              onClick={(event) => navigate(event, entry.path)}
                              aria-current={route === entry.path ? 'page' : undefined}
                            >
                              <i className={entry.icon} />
                              <span className="hide-menu">{entry.label}</span>
                            </a>
                          </li>
                        );
                      }
                      const expanded = openGroups.has(entry.id) || navigationSearch.trim() !== '';
                      return (
                        <li className="sidebar-item" key={entry.id}>
                          <button
                            className={`sidebar-link has-arrow${expanded ? ' expanded' : ''}`}
                            type="button"
                            aria-expanded={expanded}
                            onClick={() => toggleGroup(entry.id)}
                          >
                            <i className={entry.icon} />
                            <span className="hide-menu">{entry.label}</span>
                          </button>
                          <ul className={`collapse first-level${expanded ? ' show' : ''}`}>
                            {entry.links.map((link) => (
                              <li className="sidebar-item" key={link.path}>
                                <a
                                  className={`sidebar-link${route === link.path ? ' active' : ''}`}
                                  href={link.path}
                                  onClick={(event) => navigate(event, link.path)}
                                  aria-current={route === link.path ? 'page' : undefined}
                                >
                                  <i className={link.icon} />
                                  <span className="hide-menu">{link.label}</span>
                                </a>
                              </li>
                            ))}
                          </ul>
                        </li>
                      );
                    })}
                  </ul>
                </li>
              ))}
            </ul>
          </nav>
        </div>
      </aside>

      <button
        type="button"
        className="sidebar-overlay"
        aria-label="Cerrar menú"
        onClick={() => setSidebarOpen(false)}
      />

      <div className="page-wrapper">
        <div className="page-breadcrumb">
          <div>
            <h1 className="page-title">{title}</h1>
            <nav aria-label="Migas de pan">
              <ol className="breadcrumb">
                <li className="breadcrumb-item">
                  <a href="/" onClick={(event) => navigate(event, '/')}>
                    Inicio
                  </a>
                </li>
                {route !== '/' ? (
                  <li className="breadcrumb-item active" aria-current="page">
                    {title}
                  </li>
                ) : null}
              </ol>
            </nav>
          </div>
          <div className="active-site-chip">
            <i className="mdi mdi-map-marker" aria-hidden="true" />
            <span>
              <small>Sede activa</small>
              <strong>{context.siteName}</strong>
            </span>
          </div>
        </div>
        <main className="container-fluid" id="main-content">
          {message ? (
            <div className="alert alert-danger" role="alert">
              {message}
            </div>
          ) : null}
          {route.includes('ShowWizard=true') && api.managements && api.managementPlans ? (
            <WizardPanel api={api as WizardApi} location={route} onNavigate={navigateTo} />
          ) : route === '/' ? (
            api.dashboard ? (
              <DashboardPanel
                summary={dashboard}
                loading={dashboardLoading}
                error={dashboardError}
              />
            ) : (
              <section className="session-welcome" aria-label="Sesión activa">
                <div className="welcome-icon" aria-hidden="true">
                  <i className="mdi mdi-flask-outline" />
                </div>
                <div>
                  <span className="welcome-kicker">Laboratorios Univalle</span>
                  <h2>Bienvenido, {displayName.split(' ')[0]}</h2>
                  <p>
                    Trabajas en <strong>{context.siteName}</strong> con el rol{' '}
                    <strong>{context.siteRole}</strong>.
                  </p>
                </div>
              </section>
            )
          ) : route === '/Cities/Index' && api.cities ? (
            <CatalogPanel api={api} mode="cities" />
          ) : route === '/Laboratories/Index' && api.laboratories ? (
            <AcademicPanel api={api} />
          ) : route === '/Equipment/Index' &&
            api.equipment &&
            api.countries &&
            api.cities &&
            api.createEquipment &&
            api.updateEquipment &&
            api.deleteEquipment ? (
            <EquipmentPanel api={api as EquipmentApi} />
          ) : route.startsWith('/AssetView/') &&
            api.equipmentUnits &&
            api.equipmentUnit &&
            api.createEquipmentUnit &&
            api.updateEquipmentUnit &&
            api.deleteEquipmentUnit ? (
            <EquipmentUnitsPanel api={api as EquipmentUnitsApi} />
          ) : route.startsWith('/Managements/') &&
            api.managements &&
            api.createManagement &&
            api.updateManagement &&
            api.activateManagement &&
            api.closeManagement &&
            api.deleteManagement &&
            api.managementPlans &&
            api.syncManagementPlans ? (
            <ManagementPanel api={api as ManagementApi} />
          ) : route.startsWith('/Verifications/') &&
            api.verifications &&
            api.verificationCheckItems &&
            api.saveVerification ? (
            <VerificationsPanel api={api as VerificationsApi} />
          ) : route.startsWith('/Requests/') &&
            api.requests &&
            api.requestDetail &&
            api.createRequest &&
            api.updateRequest &&
            api.completeRequest &&
            api.cancelRequest &&
            api.managements &&
            api.managementPlans ? (
            <RequestsPanel api={api as RequestsApi} />
          ) : route.startsWith('/Maintenances/') &&
            api.maintenances &&
            api.maintenanceDetail &&
            api.createMaintenance &&
            api.updateMaintenance &&
            api.completeMaintenance &&
            api.cancelMaintenance &&
            api.managements &&
            api.managementPlans ? (
            <MaintenancesPanel api={api as MaintenanceApi} />
          ) : route.startsWith('/Departures/') &&
            api.departures &&
            api.departureDetail &&
            api.createDeparture &&
            api.createMassDepartures &&
            api.updateDeparture &&
            api.returnDeparture &&
            api.cancelDeparture &&
            api.managements &&
            api.managementPlans ? (
            <DeparturesPanel api={api as DepartureApi} />
          ) : route.startsWith('/Kardex/') &&
            api.kardex &&
            api.kardexDetail &&
            api.saveKardexDraft &&
            api.completeKardex ? (
            <KardexPanel api={api as KardexApi} />
          ) : route.startsWith('/Acquisitions/') &&
            api.acquisitions &&
            api.acquisitionDetail &&
            api.createAcquisition &&
            api.updateAcquisition &&
            api.completeAcquisition &&
            api.cancelAcquisition &&
            api.managements &&
            api.managementPlans ? (
            <AcquisitionsPanel api={api as AcquisitionsApi} />
          ) : route.startsWith('/Persons/') &&
            api.people &&
            api.person &&
            api.createPerson &&
            api.updatePerson &&
            api.deletePerson ? (
            <PeoplePanel api={api as PeopleApi} />
          ) : route === '/Users/Index' &&
            api.users &&
            api.user &&
            api.createUser &&
            api.updateUser &&
            api.deleteUser &&
            api.profile &&
            api.updateProfile ? (
            <UsersPanel api={api as UsersApi} />
          ) : route === '/Users/Details' && api.profile && api.updateProfile ? (
            <ProfilePanel api={api as UsersApi} />
          ) : route === '/Reports/Index' &&
            api.reportManifest &&
            api.downloadReport &&
            api.managements &&
            api.laboratories ? (
            <ReportsPanel api={api as ReportsApi} />
          ) : route.startsWith('/LaboratoryOperations/') &&
            api.dashboard &&
            api.managements &&
            api.managementPlans ? (
            <OperationsPanel api={api as OperationsApi} route={route.split('?')[0]!} />
          ) : null}
        </main>
        <footer className="footer text-center">
          © {new Date().getUTCFullYear()} Universidad del Valle · Sistema de Gestión de Laboratorios
        </footer>
      </div>
    </div>
  );
}
