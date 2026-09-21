import { useEffect, useMemo, useState, type ReactElement } from 'react';
import type {
  ManagementPage,
  ManagementPlanPage,
  ManagementPlanQuery,
  ManagementRecord,
} from '@lu/contracts';

export type WizardApi = {
  managements(query?: { readonly currentPage?: number }): Promise<ManagementPage>;
  managementPlans(id: number, query?: ManagementPlanQuery): Promise<ManagementPlanPage>;
};

type VisualStep = 1 | 2 | 3 | 4 | 5 | 6 | 7;

type StepDefinition = {
  readonly number: VisualStep;
  readonly label: string;
  readonly route: string | null;
};

const STEP_ROUTES: Record<Exclude<VisualStep, 7>, string> = {
  1: '/Verifications/Index',
  2: '/Requests/Index',
  3: '/Maintenances/Index',
  4: '/Departures/Index',
  5: '/Kardex/Index',
  6: '/Acquisitions/Index',
};

function readNumber(value: string | null): number | null {
  if (value === null || value.trim() === '') return null;
  const parsed = Number(value);
  return Number.isInteger(parsed) && parsed > 0 ? parsed : null;
}

function clampStep(value: number | null): VisualStep {
  if (value === null || value < 1) return 1;
  if (value > 7) return 7;
  return value as VisualStep;
}

function buildSteps(corrective: boolean): readonly StepDefinition[] {
  const steps: StepDefinition[] = corrective
    ? [
        { number: 2, label: 'L-7 Solicitud', route: STEP_ROUTES[2] },
        { number: 3, label: 'L-8 Mantenimiento', route: STEP_ROUTES[3] },
        { number: 4, label: 'L-3 Salida', route: STEP_ROUTES[4] },
        { number: 5, label: 'Kardex / L-48', route: STEP_ROUTES[5] },
        { number: 6, label: 'L-12 Desembolso', route: STEP_ROUTES[6] },
      ]
    : [
        { number: 1, label: 'L-6 Verificación', route: STEP_ROUTES[1] },
        { number: 2, label: 'L-7 Solicitud', route: STEP_ROUTES[2] },
        { number: 3, label: 'L-8 Mantenimiento', route: STEP_ROUTES[3] },
        { number: 4, label: 'L-3 Salida', route: STEP_ROUTES[4] },
        { number: 5, label: 'Kardex / L-48', route: STEP_ROUTES[5] },
        { number: 6, label: 'L-12 Desembolso', route: STEP_ROUTES[6] },
      ];
  return [...steps, { number: 7, label: 'Completados', route: null }];
}

export function WizardPanel({
  api,
  location,
  onNavigate,
}: {
  readonly api: WizardApi;
  readonly location: string;
  readonly onNavigate: (path: string) => void;
}): ReactElement {
  const query = useMemo(() => {
    const separator = location.indexOf('?');
    return new URLSearchParams(separator >= 0 ? location.slice(separator + 1) : '');
  }, [location]);
  const requestedManagementId = readNumber(query.get('ManagementId'));
  const requestedStep = clampStep(readNumber(query.get('Step')));
  const [managementPage, setManagementPage] = useState<ManagementPage | null>(null);
  const [managementId, setManagementId] = useState<number | null>(requestedManagementId);
  const [step, setStep] = useState<VisualStep>(requestedStep);
  const [plans, setPlans] = useState<ManagementPlanPage | null>(null);
  const [searchTerm, setSearchTerm] = useState(query.get('SearchTerm') ?? '');
  const [laboratoryId, setLaboratoryId] = useState<number | ''>(
    readNumber(query.get('SelectedLabId')) ?? '',
  );
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    let cancelled = false;
    setLoading(true);
    void api
      .managements({ currentPage: 1 })
      .then((value) => {
        if (cancelled) return;
        setManagementPage(value);
        const selected =
          value.items.find((item) => item.id === requestedManagementId) ??
          value.items.find((item) => item.status === 0) ??
          value.items[0];
        setManagementId(selected?.id ?? null);
        if (selected?.type === 1 && requestedStep === 1) setStep(2);
      })
      .catch(() => {
        if (!cancelled) setError('No fue posible cargar las gestiones del wizard.');
      })
      .finally(() => {
        if (!cancelled) setLoading(false);
      });
    return () => {
      cancelled = true;
    };
  }, [api, requestedManagementId, requestedStep]);

  useEffect(() => {
    if (managementId === null) {
      setPlans(null);
      return;
    }
    let cancelled = false;
    setError(null);
    const planQuery: ManagementPlanQuery = {
      currentPage: 1,
      searchTerm: searchTerm.trim() || undefined,
      laboratoryId: laboratoryId === '' ? undefined : laboratoryId,
    };
    void api
      .managementPlans(managementId, planQuery)
      .then((value) => {
        if (!cancelled) setPlans(value);
      })
      .catch(() => {
        if (!cancelled) setError('No fue posible cargar los activos del wizard.');
      });
    return () => {
      cancelled = true;
    };
  }, [api, laboratoryId, managementId, searchTerm]);

  const selected: ManagementRecord | undefined = managementPage?.items.find(
    (item) => item.id === managementId,
  );
  const corrective = selected?.type === 1;
  const steps = buildSteps(corrective);
  const effectiveStep: VisualStep = corrective && step === 1 ? 2 : step;
  const currentDefinition = steps.find((item) => item.number === effectiveStep) ?? steps[0]!;

  function openStep(next: VisualStep): void {
    if (managementId === null) return;
    if (next === 7) {
      onNavigate(`/?ShowWizard=true&Step=7&ManagementId=${managementId}`);
      return;
    }
    const target = STEP_ROUTES[next as Exclude<VisualStep, 7>];
    onNavigate(`${target}?ManagementId=${managementId}`);
  }

  return (
    <section className="catalog-panel wizard-panel" aria-label="Wizard de gestión">
      <div className="catalog-heading">
        <div>
          <span className="welcome-kicker">Wizard de gestión</span>
          <h2>{selected?.code ?? 'Sin gestión seleccionada'}</h2>
          <p>
            El identificador de la gestión se conserva en cada paso. Los pasos no migrados se
            mantienen inaccesibles, nunca simulados.
          </p>
        </div>
        <i className="mdi mdi-clipboard-flow-outline operations-heading-icon" aria-hidden="true" />
      </div>

      {error ? (
        <div className="alert alert-danger" role="alert">
          {error}
        </div>
      ) : null}

      <div className="wizard-toolbar">
        <label>
          Gestión
          <select
            className="form-control"
            value={managementId ?? ''}
            onChange={(event) => {
              const value = readNumber(event.target.value);
              setManagementId(value);
              setStep(
                value !== null &&
                  managementPage?.items.find((item) => item.id === value)?.type === 1
                  ? 2
                  : 1,
              );
            }}
          >
            <option value="">Selecciona una gestión</option>
            {managementPage?.items.map((item) => (
              <option key={item.id} value={item.id}>
                {item.code} · {item.type === 1 ? 'Correctiva' : 'Preventiva'}
              </option>
            ))}
          </select>
        </label>
        <label>
          Laboratorio (ID)
          <input
            className="form-control"
            inputMode="numeric"
            value={laboratoryId}
            onChange={(event) => setLaboratoryId(readNumber(event.target.value) ?? '')}
            placeholder="Todos"
          />
        </label>
        <label>
          Buscar activo
          <input
            className="form-control"
            value={searchTerm}
            onChange={(event) => setSearchTerm(event.target.value)}
            placeholder="Inventario, serie o equipo"
          />
        </label>
      </div>

      <div className="wizard-stepper" aria-label="Pasos de la gestión">
        {steps.map((item) => {
          const complete =
            item.number < effectiveStep || (item.number === 7 && effectiveStep === 7);
          const active = item.number === effectiveStep;
          return (
            <button
              key={item.number}
              type="button"
              className={`wizard-step${active ? ' active' : ''}${complete ? ' complete' : ''}`}
              onClick={() => openStep(item.number)}
              disabled={item.route === null && item.number !== 7}
              aria-current={active ? 'step' : undefined}
            >
              <span>{complete && item.number !== 7 ? '✓' : item.number}</span>
              <small>{item.label}</small>
            </button>
          );
        })}
      </div>

      {loading ? (
        <div className="empty-state" aria-live="polite">
          <i className="mdi mdi-loading mdi-spin" />
          <p>Cargando contexto del wizard…</p>
        </div>
      ) : effectiveStep === 7 ? (
        <div className="empty-state">
          <i className="mdi mdi-check-all" />
          <h3>Completados</h3>
          <p>
            {plans?.items.filter((item) => item.planStatus === 2).length ?? 0} activos completados
            en la gestión seleccionada.
          </p>
        </div>
      ) : (
        <div className="catalog-table-wrap">
          <div className="wizard-current-step">
            <div>
              <span className="welcome-kicker">Paso {effectiveStep}</span>
              <h3>{currentDefinition.label}</h3>
            </div>
            <div className="wizard-navigation">
              <button
                type="button"
                className="btn btn-light"
                disabled={effectiveStep <= (corrective ? 2 : 1)}
                onClick={() => openStep((effectiveStep - 1) as VisualStep)}
              >
                Anterior
              </button>
              <button
                type="button"
                className="btn btn-primary"
                disabled={effectiveStep >= 6}
                onClick={() => openStep((effectiveStep + 1) as VisualStep)}
              >
                Siguiente
              </button>
            </div>
          </div>
          {plans?.items.length ? (
            <table className="catalog-table">
              <thead>
                <tr>
                  <th>Activo</th>
                  <th>Laboratorio</th>
                  <th>Fase</th>
                  <th>Estado</th>
                  <th>Avance</th>
                </tr>
              </thead>
              <tbody>
                {plans.items.map((item) => (
                  <tr key={item.id}>
                    <td>
                      <strong>{item.inventoryNumber ?? 'Sin inventario'}</strong>
                      <small>{item.equipmentName ?? 'Equipo'}</small>
                    </td>
                    <td>{item.laboratoryName ?? 'Sin laboratorio'}</td>
                    <td>L-{item.currentPhase}</td>
                    <td>{item.isDraft ? 'Borrador recuperable' : `Estado ${item.currentState}`}</td>
                    <td>
                      <button
                        type="button"
                        className="btn btn-link btn-sm"
                        onClick={() => openStep(item.currentPhase)}
                      >
                        Atender paso
                      </button>
                    </td>
                  </tr>
                ))}
              </tbody>
            </table>
          ) : (
            <div className="empty-state">
              <i className="mdi mdi-clipboard-text-outline" />
              <h3>No hay activos para este paso</h3>
              <p>La gestión no tiene registros que coincidan con los filtros actuales.</p>
            </div>
          )}
        </div>
      )}
    </section>
  );
}
