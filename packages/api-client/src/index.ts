import {
  AUTH_ROUTES,
  CATALOG_ROUTES,
  ACADEMIC_ROUTES,
  EQUIPMENT_ROUTES,
  CORE_ROUTES,
  DASHBOARD_ROUTES,
  type ActiveSiteSession,
  type ApiFailure,
  type ApiResult,
  type CsrfResponse,
  type CatalogCollectionQuery,
  type CatalogPage,
  type City,
  type Country,
  type CreateCityInput,
  type CreateCountryInput,
  type DashboardNotificationsQuery,
  type DashboardNotification,
  type DashboardQuery,
  type DashboardSummary,
  type LoginRequest,
  type SetActiveSiteRequest,
  type SiteRequestContext,
  type UpdateCityInput,
  type UpdateCountryInput,
  type AcademicQuery,
  type Career,
  type Faculty,
  type Laboratory,
  type SiteCareer,
  type CreateCareerInput,
  type CreateFacultyInput,
  type CreateLaboratoryInput,
  type UpdateCareerInput,
  type UpdateFacultyInput,
  type UpdateLaboratoryInput,
  type CreateEquipmentInput,
  type EquipmentPage,
  type EquipmentQuery,
  type EquipmentRecord,
  type UpdateEquipmentInput,
  EQUIPMENT_UNIT_ROUTES,
  type CreateEquipmentUnitInput,
  type EquipmentUnitDetail,
  type EquipmentUnitPage,
  type EquipmentUnitQuery,
  type UpdateEquipmentUnitInput,
  type EquipmentUnitStateHistory,
  MANAGEMENT_ROUTES,
  type CreateManagementInput,
  type ManagementPage,
  type ManagementPlanPage,
  type ManagementPlanQuery,
  type ManagementRecord,
  type ManagementQuery,
  type SyncManagementPlanInput,
  type UpdateManagementInput,
  VERIFICATION_ROUTES,
  type CreateVerificationInput,
  type MassVerificationInput,
  type VerificationCheckItem,
  type VerificationDetail,
  type VerificationPage,
  type VerificationQuery,
  REQUEST_ROUTES,
  type CreateRequestInput,
  type RequestDetail,
  type RequestPage,
  type RequestQuery,
  type UpdateRequestInput,
  MAINTENANCE_ROUTES,
  type CreateMaintenanceInput,
  type MaintenanceDetail,
  type MaintenancePage,
  type MaintenanceQuery,
  type UpdateMaintenanceInput,
  DEPARTURE_ROUTES,
  type CreateDepartureInput,
  type DepartureDetail,
  type DeparturePage,
  type DepartureQuery,
  type MassDepartureInput,
  type UpdateDepartureInput,
  KARDEX_ROUTES,
  type KardexInput,
  type KardexPage,
  type KardexQuery,
  type KardexRecord,
  ACQUISITION_ROUTES,
  type AcquisitionDetail,
  type AcquisitionPage,
  type AcquisitionQuery,
  type CreateAcquisitionInput,
  type UpdateAcquisitionInput,
  PERSON_ROUTES,
  type CreatePersonInput,
  type PersonPage,
  type PersonQuery,
  type PersonRecord,
  type UpdatePersonInput,
  USER_ROUTES,
  type CreateManagedUserInput,
  type ManagedUserPage,
  type ManagedUserQuery,
  type ManagedUserRecord,
  type ProfileRecord,
  type UpdateManagedUserInput,
  type UpdateProfileInput,
  REPORT_ROUTES,
  type ReportKind,
  type ReportManifestItem,
} from '@lu/contracts';

export interface ApiClientOptions {
  /** Empty string uses the current browser origin and is the recommended BFF deployment. */
  readonly baseUrl?: string;
  readonly fetch?: typeof globalThis.fetch;
}

export class ApiClientError extends Error {
  constructor(
    public readonly status: number,
    public readonly failure: ApiFailure,
  ) {
    super(failure.error.message);
    this.name = 'ApiClientError';
  }
}

function normalizeBaseUrl(value: string | undefined): string {
  const raw = value?.trim() ?? '';
  if (raw === '') return '';
  const parsed = new URL(raw);
  if (
    parsed.username !== '' ||
    parsed.password !== '' ||
    parsed.search !== '' ||
    parsed.hash !== ''
  ) {
    throw new TypeError('API base URL must not contain credentials, query or fragment.');
  }
  const isLoopback = ['localhost', '127.0.0.1', '::1'].includes(parsed.hostname.toLowerCase());
  if (parsed.protocol !== 'https:' && !(parsed.protocol === 'http:' && isLoopback)) {
    throw new TypeError('API base URL must use HTTPS except on loopback.');
  }
  return parsed.href.replace(/\/$/, '');
}

function isApiFailure(value: unknown): value is ApiFailure {
  if (typeof value !== 'object' || value === null) return false;
  const candidate = value as Partial<ApiFailure>;
  return (
    candidate.success === false &&
    typeof candidate.error === 'object' &&
    candidate.error !== null &&
    typeof candidate.error.code === 'string' &&
    typeof candidate.error.message === 'string'
  );
}

export class ApiClient {
  private readonly baseUrl: string;
  private readonly fetcher: typeof globalThis.fetch;
  private csrfToken: string | null = null;

  constructor(options: ApiClientOptions = {}) {
    this.baseUrl = normalizeBaseUrl(options.baseUrl);
    this.fetcher = options.fetch ?? globalThis.fetch.bind(globalThis);
  }

  async csrf(): Promise<CsrfResponse> {
    const response = await this.fetcher(`${this.baseUrl}${AUTH_ROUTES.csrf}`, {
      method: 'GET',
      credentials: 'include',
      headers: { Accept: 'application/json' },
    });
    const body = (await response.json()) as unknown;
    if (!response.ok || typeof body !== 'object' || body === null || !('csrfToken' in body)) {
      throw this.toError(response.status, body);
    }
    const token = (body as { csrfToken?: unknown }).csrfToken;
    if (typeof token !== 'string' || token.length === 0) {
      throw this.toError(response.status, body);
    }
    this.csrfToken = token;
    return { csrfToken: token };
  }

  async login(input: LoginRequest): Promise<ActiveSiteSession> {
    return this.mutate<ActiveSiteSession>(AUTH_ROUTES.login, 'POST', input);
  }

  async session(): Promise<ActiveSiteSession> {
    return this.request<ActiveSiteSession>(AUTH_ROUTES.session, { method: 'GET' });
  }

  async setActiveSite(input: SetActiveSiteRequest): Promise<ActiveSiteSession> {
    return this.mutate<ActiveSiteSession>(AUTH_ROUTES.activeSite, 'PUT', input);
  }

  async logout(): Promise<void> {
    const token = await this.ensureCsrf();
    const response = await this.fetcher(`${this.baseUrl}${AUTH_ROUTES.logout}`, {
      method: 'POST',
      credentials: 'include',
      headers: { Accept: 'application/json', 'X-CSRF-Token': token },
    });
    if (!response.ok) {
      const body = await this.readOptionalJson(response);
      throw this.toError(response.status, body);
    }
    this.csrfToken = null;
  }

  async siteContext(): Promise<SiteRequestContext> {
    return this.request<SiteRequestContext>(CORE_ROUTES.siteContext, { method: 'GET' });
  }

  async dashboard(query: DashboardQuery = {}): Promise<DashboardSummary> {
    return this.request<DashboardSummary>(this.withQuery(DASHBOARD_ROUTES.summary, query), {
      method: 'GET',
    });
  }

  async notifications(
    query: DashboardNotificationsQuery = {},
  ): Promise<readonly DashboardNotification[]> {
    return this.request<readonly DashboardNotification[]>(
      this.withQuery(DASHBOARD_ROUTES.notifications, query),
      { method: 'GET' },
    );
  }

  async markNotificationRead(id: number): Promise<void> {
    await this.mutate<null>(DASHBOARD_ROUTES.markNotificationRead(id), 'POST', {});
  }

  async markAllNotificationsRead(): Promise<void> {
    await this.mutate<null>(DASHBOARD_ROUTES.markAllNotificationsRead, 'POST', {});
  }

  async countries(query: CatalogCollectionQuery = {}): Promise<CatalogPage<Country>> {
    return this.request<CatalogPage<Country>>(this.withQuery(CATALOG_ROUTES.countries, query), {
      method: 'GET',
    });
  }

  async country(id: number): Promise<Country> {
    return this.request<Country>(CATALOG_ROUTES.country(id), { method: 'GET' });
  }

  async createCountry(input: CreateCountryInput): Promise<Country> {
    return this.mutate<Country>(CATALOG_ROUTES.countries, 'POST', input);
  }

  async updateCountry(id: number, input: UpdateCountryInput): Promise<Country> {
    return this.mutate<Country>(CATALOG_ROUTES.country(id), 'PUT', input);
  }

  async deleteCountry(id: number): Promise<void> {
    await this.mutate<null>(CATALOG_ROUTES.country(id), 'DELETE', {});
  }

  async cities(query: CatalogCollectionQuery = {}): Promise<CatalogPage<City>> {
    return this.request<CatalogPage<City>>(this.withQuery(CATALOG_ROUTES.cities, query), {
      method: 'GET',
    });
  }

  async city(id: number): Promise<City> {
    return this.request<City>(CATALOG_ROUTES.city(id), { method: 'GET' });
  }

  async createCity(input: CreateCityInput): Promise<City> {
    return this.mutate<City>(CATALOG_ROUTES.cities, 'POST', input);
  }

  async updateCity(id: number, input: UpdateCityInput): Promise<City> {
    return this.mutate<City>(CATALOG_ROUTES.city(id), 'PUT', input);
  }

  async deleteCity(id: number): Promise<void> {
    await this.mutate<null>(CATALOG_ROUTES.city(id), 'DELETE', {});
  }

  async faculties(query: AcademicQuery = {}): Promise<CatalogPage<Faculty>> {
    return this.request<CatalogPage<Faculty>>(this.withQuery(ACADEMIC_ROUTES.faculties, query), {
      method: 'GET',
    });
  }
  async faculty(id: number): Promise<Faculty> {
    return this.request<Faculty>(ACADEMIC_ROUTES.faculty(id), { method: 'GET' });
  }
  async createFaculty(input: CreateFacultyInput): Promise<Faculty> {
    return this.mutate<Faculty>(ACADEMIC_ROUTES.faculties, 'POST', input);
  }
  async updateFaculty(id: number, input: UpdateFacultyInput): Promise<Faculty> {
    return this.mutate<Faculty>(ACADEMIC_ROUTES.faculty(id), 'PUT', input);
  }
  async deleteFaculty(id: number): Promise<void> {
    await this.mutate<null>(ACADEMIC_ROUTES.faculty(id), 'DELETE', {});
  }
  async careers(query: AcademicQuery = {}): Promise<CatalogPage<Career>> {
    return this.request<CatalogPage<Career>>(this.withQuery(ACADEMIC_ROUTES.careers, query), {
      method: 'GET',
    });
  }
  async career(id: number): Promise<Career> {
    return this.request<Career>(ACADEMIC_ROUTES.career(id), { method: 'GET' });
  }
  async createCareer(input: CreateCareerInput): Promise<Career> {
    return this.mutate<Career>(ACADEMIC_ROUTES.careers, 'POST', input);
  }
  async updateCareer(id: number, input: UpdateCareerInput): Promise<Career> {
    return this.mutate<Career>(ACADEMIC_ROUTES.career(id), 'PUT', input);
  }
  async deleteCareer(id: number): Promise<void> {
    await this.mutate<null>(ACADEMIC_ROUTES.career(id), 'DELETE', {});
  }
  async siteCareers(): Promise<readonly SiteCareer[]> {
    return this.request<readonly SiteCareer[]>(ACADEMIC_ROUTES.siteCareers, { method: 'GET' });
  }
  async assignSiteCareer(careerId: number): Promise<void> {
    await this.mutate<null>(ACADEMIC_ROUTES.siteCareer(careerId), 'POST', {});
  }
  async unassignSiteCareer(careerId: number): Promise<void> {
    await this.mutate<null>(ACADEMIC_ROUTES.siteCareer(careerId), 'DELETE', {});
  }
  async laboratories(query: AcademicQuery = {}): Promise<CatalogPage<Laboratory>> {
    return this.request<CatalogPage<Laboratory>>(
      this.withQuery(ACADEMIC_ROUTES.laboratories, query),
      { method: 'GET' },
    );
  }
  async laboratory(id: number): Promise<Laboratory> {
    return this.request<Laboratory>(ACADEMIC_ROUTES.laboratory(id), { method: 'GET' });
  }
  async createLaboratory(input: CreateLaboratoryInput): Promise<Laboratory> {
    return this.mutate<Laboratory>(ACADEMIC_ROUTES.laboratories, 'POST', input);
  }
  async updateLaboratory(id: number, input: UpdateLaboratoryInput): Promise<Laboratory> {
    return this.mutate<Laboratory>(ACADEMIC_ROUTES.laboratory(id), 'PUT', input);
  }
  async deleteLaboratory(id: number): Promise<void> {
    await this.mutate<null>(ACADEMIC_ROUTES.laboratory(id), 'DELETE', {});
  }

  async equipment(query: EquipmentQuery = {}): Promise<EquipmentPage> {
    return this.request<EquipmentPage>(this.withQuery(EQUIPMENT_ROUTES.equipment, query), {
      method: 'GET',
    });
  }
  async equipmentDetail(id: number): Promise<EquipmentRecord> {
    return this.request<EquipmentRecord>(EQUIPMENT_ROUTES.detail(id), { method: 'GET' });
  }
  async createEquipment(input: CreateEquipmentInput): Promise<EquipmentRecord> {
    return this.mutate<EquipmentRecord>(EQUIPMENT_ROUTES.equipment, 'POST', input);
  }
  async updateEquipment(id: number, input: UpdateEquipmentInput): Promise<EquipmentRecord> {
    return this.mutate<EquipmentRecord>(EQUIPMENT_ROUTES.detail(id), 'PUT', input);
  }
  async deleteEquipment(id: number): Promise<void> {
    await this.mutate<null>(EQUIPMENT_ROUTES.detail(id), 'DELETE', {});
  }

  async equipmentUnits(query: EquipmentUnitQuery = {}): Promise<EquipmentUnitPage> {
    return this.request<EquipmentUnitPage>(this.withQuery(EQUIPMENT_UNIT_ROUTES.units, query), {
      method: 'GET',
    });
  }
  async equipmentUnit(id: number): Promise<EquipmentUnitDetail> {
    return this.request<EquipmentUnitDetail>(EQUIPMENT_UNIT_ROUTES.unit(id), { method: 'GET' });
  }
  async equipmentUnitHistory(id: number): Promise<readonly EquipmentUnitStateHistory[]> {
    return this.request<readonly EquipmentUnitStateHistory[]>(EQUIPMENT_UNIT_ROUTES.history(id), {
      method: 'GET',
    });
  }
  async createEquipmentUnit(input: CreateEquipmentUnitInput): Promise<EquipmentUnitDetail> {
    return this.mutate<EquipmentUnitDetail>(EQUIPMENT_UNIT_ROUTES.units, 'POST', input);
  }
  async updateEquipmentUnit(
    id: number,
    input: UpdateEquipmentUnitInput,
  ): Promise<EquipmentUnitDetail> {
    return this.mutate<EquipmentUnitDetail>(EQUIPMENT_UNIT_ROUTES.unit(id), 'PUT', input);
  }
  async deleteEquipmentUnit(id: number): Promise<void> {
    await this.mutate<null>(EQUIPMENT_UNIT_ROUTES.unit(id), 'DELETE', {});
  }

  async managements(query: ManagementQuery = {}): Promise<ManagementPage> {
    return this.request<ManagementPage>(this.withQuery(MANAGEMENT_ROUTES.managements, query), {
      method: 'GET',
    });
  }
  async management(id: number): Promise<ManagementRecord> {
    return this.request<ManagementRecord>(MANAGEMENT_ROUTES.management(id), { method: 'GET' });
  }
  async createManagement(input: CreateManagementInput): Promise<ManagementRecord> {
    return this.mutate<ManagementRecord>(MANAGEMENT_ROUTES.managements, 'POST', input);
  }
  async updateManagement(id: number, input: UpdateManagementInput): Promise<ManagementRecord> {
    return this.mutate<ManagementRecord>(MANAGEMENT_ROUTES.management(id), 'PUT', input);
  }
  async activateManagement(id: number): Promise<ManagementRecord> {
    return this.mutate<ManagementRecord>(MANAGEMENT_ROUTES.activate(id), 'POST', {});
  }
  async closeManagement(id: number): Promise<ManagementRecord> {
    return this.mutate<ManagementRecord>(MANAGEMENT_ROUTES.close(id), 'POST', {});
  }
  async deleteManagement(id: number): Promise<void> {
    await this.mutate<null>(MANAGEMENT_ROUTES.management(id), 'DELETE', {});
  }
  async managementPlans(id: number, query: ManagementPlanQuery = {}): Promise<ManagementPlanPage> {
    return this.request<ManagementPlanPage>(this.withQuery(MANAGEMENT_ROUTES.plans(id), query), {
      method: 'GET',
    });
  }
  async syncManagementPlans(id: number, input: SyncManagementPlanInput): Promise<void> {
    await this.mutate<null>(MANAGEMENT_ROUTES.syncPlans(id), 'POST', input);
  }

  async verifications(query: VerificationQuery = {}): Promise<VerificationPage> {
    return this.request<VerificationPage>(
      this.withQuery(VERIFICATION_ROUTES.verifications, query),
      { method: 'GET' },
    );
  }
  async verification(id: number): Promise<VerificationDetail> {
    return this.request<VerificationDetail>(VERIFICATION_ROUTES.verification(id), {
      method: 'GET',
    });
  }
  async verificationCheckItems(): Promise<readonly VerificationCheckItem[]> {
    return this.request<readonly VerificationCheckItem[]>(VERIFICATION_ROUTES.checkItems, {
      method: 'GET',
    });
  }
  async saveVerification(input: CreateVerificationInput): Promise<VerificationDetail> {
    return this.mutate<VerificationDetail>(VERIFICATION_ROUTES.verifications, 'POST', input);
  }
  async saveMassVerifications(
    input: MassVerificationInput,
  ): Promise<readonly VerificationDetail[]> {
    return this.mutate<readonly VerificationDetail[]>(VERIFICATION_ROUTES.mass, 'POST', input);
  }

  async requests(query: RequestQuery = {}): Promise<RequestPage> {
    return this.request<RequestPage>(this.withQuery(REQUEST_ROUTES.requests, query), {
      method: 'GET',
    });
  }
  async requestDetail(id: number): Promise<RequestDetail> {
    return this.request<RequestDetail>(REQUEST_ROUTES.request(id), { method: 'GET' });
  }
  async createRequest(input: CreateRequestInput): Promise<RequestDetail> {
    return this.mutate<RequestDetail>(REQUEST_ROUTES.requests, 'POST', input);
  }
  async updateRequest(id: number, input: UpdateRequestInput): Promise<RequestDetail> {
    return this.mutate<RequestDetail>(REQUEST_ROUTES.request(id), 'PUT', input);
  }
  async completeRequest(id: number): Promise<RequestDetail> {
    return this.mutate<RequestDetail>(REQUEST_ROUTES.complete(id), 'POST', {});
  }
  async cancelRequest(id: number): Promise<void> {
    await this.mutate<null>(REQUEST_ROUTES.cancel(id), 'POST', {});
  }

  async maintenances(query: MaintenanceQuery = {}): Promise<MaintenancePage> {
    return this.request<MaintenancePage>(this.withQuery(MAINTENANCE_ROUTES.maintenances, query), {
      method: 'GET',
    });
  }
  async maintenanceDetail(id: number): Promise<MaintenanceDetail> {
    return this.request<MaintenanceDetail>(MAINTENANCE_ROUTES.maintenance(id), { method: 'GET' });
  }
  async createMaintenance(input: CreateMaintenanceInput): Promise<MaintenanceDetail> {
    return this.mutate<MaintenanceDetail>(MAINTENANCE_ROUTES.maintenances, 'POST', input);
  }
  async updateMaintenance(id: number, input: UpdateMaintenanceInput): Promise<MaintenanceDetail> {
    return this.mutate<MaintenanceDetail>(MAINTENANCE_ROUTES.maintenance(id), 'PUT', input);
  }
  async completeMaintenance(id: number): Promise<MaintenanceDetail> {
    return this.mutate<MaintenanceDetail>(MAINTENANCE_ROUTES.complete(id), 'POST', {});
  }
  async cancelMaintenance(id: number): Promise<void> {
    await this.mutate<null>(MAINTENANCE_ROUTES.cancel(id), 'POST', {});
  }

  async departures(query: DepartureQuery = {}): Promise<DeparturePage> {
    return this.request<DeparturePage>(this.withQuery(DEPARTURE_ROUTES.departures, query), {
      method: 'GET',
    });
  }
  async departureDetail(id: number): Promise<DepartureDetail> {
    return this.request<DepartureDetail>(DEPARTURE_ROUTES.departure(id), { method: 'GET' });
  }
  async createDeparture(input: CreateDepartureInput): Promise<DepartureDetail> {
    return this.mutate<DepartureDetail>(DEPARTURE_ROUTES.departures, 'POST', input);
  }
  async createMassDepartures(input: MassDepartureInput): Promise<readonly DepartureDetail[]> {
    return this.mutate<readonly DepartureDetail[]>(DEPARTURE_ROUTES.mass, 'POST', input);
  }
  async updateDeparture(id: number, input: UpdateDepartureInput): Promise<DepartureDetail> {
    return this.mutate<DepartureDetail>(DEPARTURE_ROUTES.departure(id), 'PUT', input);
  }
  async returnDeparture(id: number, observations?: string | null): Promise<DepartureDetail> {
    return this.mutate<DepartureDetail>(DEPARTURE_ROUTES.return(id), 'POST', { observations });
  }
  async cancelDeparture(id: number): Promise<void> {
    await this.mutate<null>(DEPARTURE_ROUTES.cancel(id), 'POST', {});
  }

  async kardex(query: KardexQuery = {}): Promise<KardexPage> {
    return this.request<KardexPage>(this.withQuery(KARDEX_ROUTES.kardex, query), { method: 'GET' });
  }
  async kardexDetail(planId: number): Promise<KardexRecord> {
    return this.request<KardexRecord>(KARDEX_ROUTES.detail(planId), { method: 'GET' });
  }
  async saveKardexDraft(input: KardexInput): Promise<KardexRecord> {
    return this.mutate<KardexRecord>(KARDEX_ROUTES.draft(input.planId), 'POST', input);
  }
  async completeKardex(input: KardexInput): Promise<KardexRecord> {
    return this.mutate<KardexRecord>(KARDEX_ROUTES.complete(input.planId), 'POST', input);
  }

  async acquisitions(query: AcquisitionQuery = {}): Promise<AcquisitionPage> {
    return this.request<AcquisitionPage>(this.withQuery(ACQUISITION_ROUTES.acquisitions, query), {
      method: 'GET',
    });
  }
  async acquisitionDetail(id: number): Promise<AcquisitionDetail> {
    return this.request<AcquisitionDetail>(ACQUISITION_ROUTES.acquisition(id), { method: 'GET' });
  }
  async createAcquisition(input: CreateAcquisitionInput): Promise<AcquisitionDetail> {
    return this.mutate<AcquisitionDetail>(ACQUISITION_ROUTES.acquisitions, 'POST', input);
  }
  async updateAcquisition(id: number, input: UpdateAcquisitionInput): Promise<AcquisitionDetail> {
    return this.mutate<AcquisitionDetail>(ACQUISITION_ROUTES.acquisition(id), 'PUT', input);
  }
  async completeAcquisition(id: number): Promise<AcquisitionDetail> {
    return this.mutate<AcquisitionDetail>(ACQUISITION_ROUTES.complete(id), 'POST', {});
  }
  async cancelAcquisition(id: number): Promise<void> {
    await this.mutate<null>(ACQUISITION_ROUTES.cancel(id), 'POST', {});
  }

  async people(query: PersonQuery = {}): Promise<PersonPage> {
    return this.request<PersonPage>(this.withQuery(PERSON_ROUTES.people, query), { method: 'GET' });
  }
  async person(id: number): Promise<PersonRecord> {
    return this.request<PersonRecord>(PERSON_ROUTES.person(id), { method: 'GET' });
  }
  async createPerson(input: CreatePersonInput): Promise<PersonRecord> {
    return this.mutate<PersonRecord>(PERSON_ROUTES.people, 'POST', input);
  }
  async updatePerson(id: number, input: UpdatePersonInput): Promise<PersonRecord> {
    return this.mutate<PersonRecord>(PERSON_ROUTES.person(id), 'PUT', input);
  }
  async deletePerson(id: number): Promise<void> {
    await this.mutate<null>(PERSON_ROUTES.person(id), 'DELETE', {});
  }

  async users(query: ManagedUserQuery = {}): Promise<ManagedUserPage> {
    return this.request<ManagedUserPage>(this.withQuery(USER_ROUTES.users, query), {
      method: 'GET',
    });
  }
  async user(id: string): Promise<ManagedUserRecord> {
    return this.request<ManagedUserRecord>(USER_ROUTES.user(id), { method: 'GET' });
  }
  async createUser(input: CreateManagedUserInput): Promise<ManagedUserRecord> {
    return this.mutate<ManagedUserRecord>(USER_ROUTES.users, 'POST', input);
  }
  async updateUser(id: string, input: UpdateManagedUserInput): Promise<ManagedUserRecord> {
    return this.mutate<ManagedUserRecord>(USER_ROUTES.user(id), 'PUT', input);
  }
  async deleteUser(id: string): Promise<void> {
    await this.mutate<null>(USER_ROUTES.user(id), 'DELETE', {});
  }
  async profile(): Promise<ProfileRecord> {
    return this.request<ProfileRecord>(USER_ROUTES.profile, { method: 'GET' });
  }
  async updateProfile(input: UpdateProfileInput): Promise<ProfileRecord> {
    return this.mutate<ProfileRecord>(USER_ROUTES.profile, 'PUT', input);
  }

  async reportManifest(): Promise<readonly ReportManifestItem[]> {
    return this.request<readonly ReportManifestItem[]>(REPORT_ROUTES.manifest, { method: 'GET' });
  }

  async downloadReport(
    kind: ReportKind,
    query: Record<string, number | undefined> = {},
  ): Promise<Blob> {
    const response = await this.fetcher(
      `${this.baseUrl}${this.withQuery(`${REPORT_ROUTES.download}/${kind}`, query)}`,
      {
        method: 'GET',
        credentials: 'include',
        headers: { Accept: 'application/vnd.openxmlformats-officedocument.spreadsheetml.sheet' },
      },
    );
    if (!response.ok) {
      const body = await this.readOptionalJson(response);
      throw this.toError(response.status, body);
    }
    return response.blob();
  }

  private async mutate<T>(
    path: string,
    method: 'DELETE' | 'POST' | 'PUT',
    input: unknown,
  ): Promise<T> {
    const token = await this.ensureCsrf();
    return this.request<T>(path, {
      method,
      headers: { 'Content-Type': 'application/json', 'X-CSRF-Token': token },
      body: JSON.stringify(input),
    });
  }

  private async request<T>(path: string, init: RequestInit): Promise<T> {
    const response = await this.fetcher(`${this.baseUrl}${path}`, {
      ...init,
      credentials: 'include',
      headers: { Accept: 'application/json', ...init.headers },
    });
    const body = await this.readOptionalJson(response);
    if (!response.ok || typeof body !== 'object' || body === null) {
      throw this.toError(response.status, body);
    }
    const result = body as Partial<ApiResult<T>>;
    if (result.success !== true || !('data' in result)) {
      throw this.toError(response.status, body);
    }
    return result.data as T;
  }

  private async ensureCsrf(): Promise<string> {
    if (this.csrfToken === null) await this.csrf();
    return this.csrfToken!;
  }

  private withQuery(path: string, values: object): string {
    const params = new URLSearchParams();
    for (const [key, value] of Object.entries(values as Record<string, unknown>)) {
      if (value !== undefined && value !== null && value !== '') params.set(key, String(value));
    }
    const query = params.toString();
    return query === '' ? path : `${path}?${query}`;
  }

  private async readOptionalJson(response: Response): Promise<unknown> {
    const text = await response.text();
    if (text === '') return null;
    try {
      return JSON.parse(text) as unknown;
    } catch {
      return null;
    }
  }

  private toError(status: number, body: unknown): ApiClientError {
    const failure: ApiFailure = isApiFailure(body)
      ? body
      : {
          success: false,
          error: {
            code: 'INVALID_API_RESPONSE',
            message: 'The API returned an invalid response.',
          },
        };
    return new ApiClientError(status, failure);
  }
}
