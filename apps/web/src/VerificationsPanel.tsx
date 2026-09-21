import { useCallback, useEffect, useState, type FormEvent, type ReactElement } from 'react';
import { ApiClientError } from '@lu/api-client';
import type {
  ManagementPage,
  ManagementPlanPage,
  PhysicalCondition,
  VerificationCheckItem,
  VerificationDetail,
  VerificationPage,
  VerificationStatus,
} from '@lu/contracts';

export type VerificationsApi = {
  verifications(query?: {
    readonly currentPage?: number;
    readonly managementId?: number;
  }): Promise<VerificationPage>;
  verification(id: number): Promise<VerificationDetail>;
  verificationCheckItems(): Promise<readonly VerificationCheckItem[]>;
  saveVerification(input: {
    readonly managementId: number;
    readonly equipmentUnitId: number;
    readonly date: string;
    readonly physicalCondition: PhysicalCondition;
    readonly observations?: string | null;
    readonly status?: VerificationStatus;
    readonly faults?: readonly string[];
    readonly checkResults?: readonly { readonly checkItemId: number; readonly result: 0 | 1 }[];
  }): Promise<VerificationDetail>;
  managements?(query?: { readonly currentPage?: number }): Promise<ManagementPage>;
  managementPlans?(
    id: number,
    query?: { readonly currentPage?: number },
  ): Promise<ManagementPlanPage>;
};

const STATUS_LABEL: Record<VerificationStatus, string> = {
  0: 'Borrador',
  1: 'Completada',
  2: 'Con observaciones',
  3: 'Revisada',
  99: 'Anulada',
};
const CONDITIONS: Record<PhysicalCondition, string> = {
  1: 'Baja del equipo',
  2: 'Malo',
  3: 'Regular',
  4: 'Bueno',
  5: 'Excelente',
};

export function VerificationsPanel({ api }: { readonly api: VerificationsApi }): ReactElement {
  const [managements, setManagements] = useState<ManagementPage | null>(null);
  const [managementId, setManagementId] = useState<number | ''>('');
  const [plans, setPlans] = useState<ManagementPlanPage | null>(null);
  const [items, setItems] = useState<readonly VerificationCheckItem[]>([]);
  const [page, setPage] = useState<VerificationPage | null>(null);
  const [unitId, setUnitId] = useState<number | ''>('');
  const [condition, setCondition] = useState<PhysicalCondition>(5);
  const [date, setDate] = useState(new Date().toISOString().slice(0, 10));
  const [observations, setObservations] = useState('');
  const [faults, setFaults] = useState('');
  const [checkState, setCheckState] = useState<Record<number, 0 | 1>>({});
  const [selected, setSelected] = useState<VerificationDetail | null>(null);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [feedback, setFeedback] = useState<string | null>(null);

  const load = useCallback(async () => {
    setLoading(true);
    try {
      const [managementPage, checkItems] = await Promise.all([
        api.managements ? api.managements({ currentPage: 1 }) : Promise.resolve(null),
        api.verificationCheckItems(),
      ]);
      setManagements(managementPage);
      setItems(checkItems);
      const active =
        managementPage?.items.find((item) => item.status === 0) ?? managementPage?.items[0];
      const nextId = managementId === '' ? (active?.id ?? '') : managementId;
      setManagementId(nextId);
      if (nextId !== '') {
        const [planPage, verificationPage] = await Promise.all([
          api.managementPlans
            ? api.managementPlans(nextId, { currentPage: 1 })
            : Promise.resolve(null),
          api.verifications({ currentPage: 1, managementId: nextId }),
        ]);
        setPlans(planPage);
        setPage(verificationPage);
      }
      setError(null);
    } catch {
      setError('No fue posible cargar las verificaciones L-6.');
    } finally {
      setLoading(false);
    }
  }, [api, managementId]);

  useEffect(() => {
    void load();
  }, [load]);

  async function changeManagement(value: number | ''): Promise<void> {
    setManagementId(value);
    setUnitId('');
    setSelected(null);
    setPlans(null);
    setPage(null);
    if (value === '' || !api.managementPlans) return;
    try {
      const [planPage, verificationPage] = await Promise.all([
        api.managementPlans(value, { currentPage: 1 }),
        api.verifications({ currentPage: 1, managementId: value }),
      ]);
      setPlans(planPage);
      setPage(verificationPage);
    } catch {
      setError('No fue posible cargar la gestión seleccionada.');
    }
  }

  async function submit(event: FormEvent<HTMLFormElement>, saveDraft: boolean): Promise<void> {
    event.preventDefault();
    setError(null);
    setFeedback(null);
    if (managementId === '' || unitId === '') {
      setError('Selecciona una gestión y una unidad física.');
      return;
    }
    const faultList = faults
      .split('\n')
      .map((value) => value.trim())
      .filter(Boolean);
    try {
      await api.saveVerification({
        managementId,
        equipmentUnitId: unitId,
        date,
        physicalCondition: condition,
        observations: observations || null,
        faults: faultList,
        status: saveDraft ? 0 : undefined,
        checkResults: items.map((item) => ({
          checkItemId: item.id,
          result: checkState[item.id] ?? 0,
        })),
      });
      setFeedback(
        saveDraft
          ? 'Borrador L-6 guardado correctamente.'
          : 'Verificación L-6 registrada correctamente.',
      );
      setObservations('');
      setFaults('');
      setUnitId('');
      await load();
    } catch (caught) {
      setError(
        caught instanceof ApiClientError
          ? caught.failure.error.message
          : 'No fue posible guardar la verificación.',
      );
    }
  }

  return (
    <section className="catalog-panel verification-panel" aria-label="Verificaciones L-6">
      <div className="catalog-heading">
        <div>
          <span className="welcome-kicker">Wizard · L-6</span>
          <h2>Verificaciones</h2>
          <p>
            Los borradores conservan la gestión y la unidad; una falla lleva el plan al paso L-7.
          </p>
        </div>
        <span className="catalog-count">{page?.totalCount ?? 0} registros</span>
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
        <select
          className="form-control"
          value={managementId}
          onChange={(event) =>
            void changeManagement(event.target.value === '' ? '' : Number(event.target.value))
          }
        >
          <option value="">Seleccionar gestión</option>
          {managements?.items
            .filter((item) => item.status !== 99)
            .map((item) => (
              <option key={item.id} value={item.id}>
                {item.code} · {item.type === 0 ? 'Preventiva' : 'Correctiva'}
              </option>
            ))}
        </select>
      </div>
      <div className="catalog-layout">
        <form className="catalog-form" onSubmit={(event) => void submit(event, false)}>
          <h3>Nueva verificación</h3>
          <label>
            Unidad física
            <select
              className="form-control"
              required
              value={unitId}
              onChange={(event) =>
                setUnitId(event.target.value === '' ? '' : Number(event.target.value))
              }
            >
              <option value="">Seleccionar unidad planificada</option>
              {plans?.items
                .filter((item) => item.equipmentUnitId !== null)
                .map((item) => (
                  <option key={item.equipmentUnitId} value={item.equipmentUnitId!}>
                    {item.inventoryNumber} · {item.equipmentName ?? 'Equipo'}
                  </option>
                ))}
            </select>
          </label>
          <label>
            Fecha
            <input
              className="form-control"
              type="date"
              required
              value={date}
              onChange={(event) => setDate(event.target.value)}
            />
          </label>
          <label>
            Condición física
            <select
              className="form-control"
              value={condition}
              onChange={(event) => setCondition(Number(event.target.value) as PhysicalCondition)}
            >
              {Object.entries(CONDITIONS).map(([key, label]) => (
                <option key={key} value={key}>
                  {label}
                </option>
              ))}
            </select>
          </label>
          {items.length > 0 ? (
            <fieldset className="verification-checklist">
              <legend>Checklist</legend>
              {items.map((item) => (
                <label key={item.id}>
                  <span>{item.name}</span>
                  <select
                    className="form-control"
                    value={checkState[item.id] ?? 0}
                    onChange={(event) =>
                      setCheckState({
                        ...checkState,
                        [item.id]: Number(event.target.value) as 0 | 1,
                      })
                    }
                  >
                    <option value={0}>Pendiente</option>
                    <option value={1}>Realizado</option>
                  </select>
                </label>
              ))}
            </fieldset>
          ) : (
            <p className="text-muted">
              No hay puntos de checklist activos configurados para la sede.
            </p>
          )}
          <label>
            Observaciones
            <textarea
              className="form-control"
              maxLength={2000}
              value={observations}
              onChange={(event) => setObservations(event.target.value)}
            />
          </label>
          <label>
            Fallas (una por línea)
            <textarea
              className="form-control"
              maxLength={5000}
              value={faults}
              onChange={(event) => setFaults(event.target.value)}
            />
          </label>
          <div className="catalog-actions">
            <button type="submit" className="btn btn-primary">
              Guardar verificación
            </button>
            <button
              type="button"
              className="btn btn-light"
              onClick={(event) => {
                const form = event.currentTarget.form;
                if (form)
                  void submit(
                    { preventDefault: () => undefined } as FormEvent<HTMLFormElement>,
                    true,
                  );
              }}
            >
              Guardar borrador
            </button>
          </div>
        </form>
        <div className="catalog-table-wrap">
          {loading ? (
            <p className="text-muted">Cargando verificaciones…</p>
          ) : page?.items.length === 0 ? (
            <div className="empty-state">
              <i className="mdi mdi-clipboard-check-outline" />
              <h3>Sin verificaciones</h3>
              <p>Selecciona una unidad planificada para comenzar L-6.</p>
            </div>
          ) : (
            <table className="catalog-table">
              <thead>
                <tr>
                  <th>Unidad</th>
                  <th>Equipo</th>
                  <th>Fecha</th>
                  <th>Estado</th>
                  <th>Fallas</th>
                </tr>
              </thead>
              <tbody>
                {page?.items.map((item) => (
                  <tr key={item.id}>
                    <td>
                      <strong>{item.inventoryNumber ?? '—'}</strong>
                      <small>{item.laboratoryName ?? 'Sin laboratorio'}</small>
                    </td>
                    <td>{item.equipmentName ?? '—'}</td>
                    <td>{item.date}</td>
                    <td>
                      <span className={`status-pill status-${item.status}`}>
                        {STATUS_LABEL[item.status]}
                      </span>
                    </td>
                    <td>{item.faultsCount}</td>
                  </tr>
                ))}
              </tbody>
            </table>
          )}
        </div>
      </div>
      {selected ? (
        <aside className="inventory-detail">
          <h3>{selected.inventoryNumber}</h3>
          <p>{selected.observations ?? 'Sin observaciones'}</p>
        </aside>
      ) : null}
    </section>
  );
}
