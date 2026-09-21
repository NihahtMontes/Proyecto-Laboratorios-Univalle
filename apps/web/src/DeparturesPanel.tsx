import { useCallback, useEffect, useState, type FormEvent, type ReactElement } from 'react';
import { ApiClientError } from '@lu/api-client';
import type {
  CreateDepartureInput,
  DepartureDetail,
  DeparturePage,
  DepartureRecord,
  DepartureType,
  LoanStatus,
  ManagementPage,
  ManagementPlanPage,
  MassDepartureInput,
} from '@lu/contracts';

export type DepartureApi = {
  departures(query?: {
    readonly currentPage?: number;
    readonly managementId?: number;
    readonly statusFilter?: LoanStatus;
  }): Promise<DeparturePage>;
  departureDetail(id: number): Promise<DepartureDetail>;
  createDeparture(input: CreateDepartureInput): Promise<DepartureDetail>;
  createMassDepartures(input: MassDepartureInput): Promise<readonly DepartureDetail[]>;
  updateDeparture(
    id: number,
    input: CreateDepartureInput & { readonly status: LoanStatus },
  ): Promise<DepartureDetail>;
  returnDeparture(id: number, observations?: string | null): Promise<DepartureDetail>;
  cancelDeparture(id: number): Promise<void>;
  managements(query?: { readonly currentPage?: number }): Promise<ManagementPage>;
  managementPlans(
    id: number,
    query?: { readonly currentPage?: number },
  ): Promise<ManagementPlanPage>;
};

const TYPES: Record<DepartureType, string> = {
  1: 'Préstamo externo',
  2: 'Préstamo interno',
  3: 'Mantenimiento externo',
  4: 'Baja definitiva',
  5: 'Mantenimiento interno',
};
const STATUSES: Record<LoanStatus, string> = {
  0: 'Activo',
  1: 'Devuelto',
  2: 'Vencido',
  99: 'Cancelado',
};

export function DeparturesPanel({ api }: { readonly api: DepartureApi }): ReactElement {
  const [page, setPage] = useState<DeparturePage | null>(null);
  const [managements, setManagements] = useState<ManagementPage | null>(null);
  const [plans, setPlans] = useState<ManagementPlanPage | null>(null);
  const [managementId, setManagementId] = useState<number | ''>('');
  const [selectedUnitIds, setSelectedUnitIds] = useState<Set<number>>(new Set());
  const [departureDate, setDepartureDate] = useState(new Date().toISOString().slice(0, 10));
  const [estimatedReturnDate, setEstimatedReturnDate] = useState(
    new Date(Date.now() + 15 * 86400000).toISOString().slice(0, 10),
  );
  const [destination, setDestination] = useState('');
  const [type, setType] = useState<DepartureType>(5);
  const [statusFilter, setStatusFilter] = useState<LoanStatus | ''>('');
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [feedback, setFeedback] = useState<string | null>(null);
  const load = useCallback(async () => {
    setLoading(true);
    try {
      setPage(
        await api.departures({
          currentPage: 1,
          managementId: managementId === '' ? undefined : managementId,
          statusFilter: statusFilter === '' ? undefined : statusFilter,
        }),
      );
      setError(null);
    } catch {
      setError('No fue posible cargar las salidas L-3.');
    } finally {
      setLoading(false);
    }
  }, [api, managementId, statusFilter]);
  useEffect(() => {
    void load();
  }, [load]);
  useEffect(() => {
    void api
      .managements({ currentPage: 1 })
      .then((result) => {
        setManagements(result);
        if (managementId === '') {
          const active = result.items.find((item) => item.status === 0) ?? result.items[0];
          if (active !== undefined) {
            setManagementId(active.id);
            void api
              .managementPlans(active.id, { currentPage: 1 })
              .then(setPlans)
              .catch(() => setError('No fue posible cargar el plan de salidas.'));
          }
        }
      })
      .catch(() => setError('No fue posible cargar las gestiones para L-3.'));
  }, [api, managementId]);
  async function chooseManagement(value: number | ''): Promise<void> {
    setManagementId(value);
    setSelectedUnitIds(new Set());
    setPlans(null);
    if (value !== '')
      try {
        setPlans(await api.managementPlans(value, { currentPage: 1 }));
      } catch {
        setError('No fue posible cargar el plan de salidas.');
      }
  }
  async function submit(event: FormEvent<HTMLFormElement>, draft: boolean): Promise<void> {
    event.preventDefault();
    setError(null);
    setFeedback(null);
    if (managementId === '' || selectedUnitIds.size === 0) {
      setError('Selecciona una gestión y al menos una unidad en L-3.');
      return;
    }
    const rows = [...selectedUnitIds].map((equipmentUnitId) => ({ equipmentUnitId }));
    try {
      await api.createMassDepartures({
        managementId,
        type,
        destination: destination || null,
        departureDate,
        estimatedReturnDate,
        rows,
        isDraft: draft,
      });
      setFeedback(
        draft ? 'Borrador L-3 guardado correctamente.' : 'Salidas L-3 registradas correctamente.',
      );
      setSelectedUnitIds(new Set());
      await load();
    } catch (caught) {
      setError(
        caught instanceof ApiClientError
          ? caught.failure.error.message
          : 'No fue posible guardar las salidas.',
      );
    }
  }
  async function returnItem(item: DepartureRecord): Promise<void> {
    if (!window.confirm(`¿Confirmas la devolución de #${item.id}?`)) return;
    try {
      await api.returnDeparture(item.id);
      setFeedback(`Salida #${item.id} marcada como devuelta.`);
      await load();
    } catch (caught) {
      setError(
        caught instanceof ApiClientError
          ? caught.failure.error.message
          : 'No fue posible registrar la devolución.',
      );
    }
  }
  async function cancel(item: DepartureRecord): Promise<void> {
    if (!window.confirm(`¿Confirmas la baja de #${item.id}?`)) return;
    try {
      await api.cancelDeparture(item.id);
      setFeedback(`Salida #${item.id} cancelada.`);
      await load();
    } catch (caught) {
      setError(
        caught instanceof ApiClientError
          ? caught.failure.error.message
          : 'No fue posible cancelar la salida.',
      );
    }
  }
  return (
    <section className="catalog-panel departure-panel" aria-label="Salidas y devoluciones L-3">
      <div className="catalog-heading">
        <div>
          <span className="welcome-kicker">Wizard · L-3</span>
          <h2>Salidas y devoluciones</h2>
          <p>
            La salida masiva conserva borradores y mueve las unidades a Kardex solo al confirmar.
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
            void chooseManagement(event.target.value === '' ? '' : Number(event.target.value))
          }
        >
          <option value="">Seleccionar gestión</option>
          {managements?.items
            .filter((item) => item.status !== 99)
            .map((item) => (
              <option key={item.id} value={item.id}>
                {item.code}
              </option>
            ))}
        </select>
        <select
          className="form-control"
          value={statusFilter}
          onChange={(event) =>
            setStatusFilter(
              event.target.value === '' ? '' : (Number(event.target.value) as LoanStatus),
            )
          }
        >
          <option value="">Todos los estados</option>
          {Object.entries(STATUSES).map(([key, label]) => (
            <option key={key} value={key}>
              {label}
            </option>
          ))}
        </select>
      </div>
      <div className="catalog-layout">
        <form className="catalog-form" onSubmit={(event) => void submit(event, false)}>
          <h3>Registrar salida masiva</h3>
          <label>
            Tipo
            <select
              className="form-control"
              value={type}
              onChange={(event) => setType(Number(event.target.value) as DepartureType)}
            >
              {Object.entries(TYPES).map(([key, label]) => (
                <option key={key} value={key}>
                  {label}
                </option>
              ))}
            </select>
          </label>
          <label>
            Fecha de salida
            <input
              className="form-control"
              type="date"
              required
              value={departureDate}
              onChange={(event) => setDepartureDate(event.target.value)}
            />
          </label>
          <label>
            Retorno estimado
            <input
              className="form-control"
              type="date"
              value={estimatedReturnDate}
              onChange={(event) => setEstimatedReturnDate(event.target.value)}
            />
          </label>
          <label>
            Destino
            <input
              className="form-control"
              maxLength={200}
              value={destination}
              onChange={(event) => setDestination(event.target.value)}
            />
          </label>
          <div className="unit-selection">
            <strong>Unidades en L-3</strong>
            {plans?.items
              .filter((item) => item.equipmentUnitId !== null && item.currentPhase === 4)
              .map((item) => (
                <label key={item.equipmentUnitId}>
                  <input
                    type="checkbox"
                    checked={selectedUnitIds.has(item.equipmentUnitId!)}
                    onChange={(event) => {
                      const next = new Set(selectedUnitIds);
                      if (event.target.checked) next.add(item.equipmentUnitId!);
                      else next.delete(item.equipmentUnitId!);
                      setSelectedUnitIds(next);
                    }}
                  />{' '}
                  {item.inventoryNumber} · {item.equipmentName ?? 'Equipo'}
                </label>
              ))}
            {plans?.items.filter((item) => item.equipmentUnitId !== null && item.currentPhase === 4)
              .length === 0 ? (
              <p className="text-muted">No hay unidades pendientes de salida.</p>
            ) : null}
          </div>
          <div className="catalog-actions">
            <button className="btn btn-primary" type="submit">
              Registrar salidas
            </button>
            <button
              className="btn btn-light"
              type="button"
              onClick={(event) => void submit(event as unknown as FormEvent<HTMLFormElement>, true)}
            >
              Guardar borrador
            </button>
          </div>
        </form>
        <div className="catalog-table-wrap">
          {loading ? (
            <p className="text-muted">Cargando salidas…</p>
          ) : page?.items.length === 0 ? (
            <div className="empty-state">
              <i className="mdi mdi-clipboard-arrow-left-outline" />
              <h3>No hay salidas</h3>
              <p>Las salidas y devoluciones L-3 de la sede activa aparecerán aquí.</p>
            </div>
          ) : (
            <table className="catalog-table">
              <thead>
                <tr>
                  <th>Salida</th>
                  <th>Activo</th>
                  <th>Tipo</th>
                  <th>Estado</th>
                  <th>Acciones</th>
                </tr>
              </thead>
              <tbody>
                {page?.items.map((item) => (
                  <tr key={item.id}>
                    <td>
                      <strong>#{item.id}</strong>
                      <small>{item.managementCode ?? item.managementId}</small>
                      <small>{item.departureDate}</small>
                    </td>
                    <td>
                      {item.inventoryNumber ?? 'Sin unidad'}
                      <small>{item.equipmentName ?? 'Equipo'}</small>
                      <small>{item.destination ?? 'Sin destino'}</small>
                    </td>
                    <td>{TYPES[item.type]}</td>
                    <td>
                      <span className={`status-pill status-${item.status}`}>
                        {item.isDraft ? 'Borrador' : STATUSES[item.status]}
                      </span>
                    </td>
                    <td>
                      <div className="table-actions">
                        {item.status === 0 ? (
                          <button
                            type="button"
                            className="btn btn-sm btn-outline-success"
                            onClick={() => void returnItem(item)}
                          >
                            Devolver
                          </button>
                        ) : null}
                        {item.status !== 99 && item.status !== 1 ? (
                          <button
                            type="button"
                            className="btn btn-sm btn-outline-danger"
                            onClick={() => void cancel(item)}
                          >
                            Cancelar
                          </button>
                        ) : null}
                      </div>
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
