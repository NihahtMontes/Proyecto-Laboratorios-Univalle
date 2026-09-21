import { useCallback, useEffect, useState, type FormEvent, type ReactElement } from 'react';
import { ApiClientError } from '@lu/api-client';
import type {
  AcquisitionDetail,
  AcquisitionPage,
  AcquisitionRecord,
  CreateAcquisitionInput,
  ManagementPage,
  ManagementPlanPage,
  MaintenanceCostInput,
  RequestPriority,
  RequestStatus,
} from '@lu/contracts';

export type AcquisitionsApi = {
  acquisitions(query?: {
    readonly currentPage?: number;
    readonly managementId?: number;
    readonly statusFilter?: RequestStatus;
  }): Promise<AcquisitionPage>;
  acquisitionDetail(id: number): Promise<AcquisitionDetail>;
  createAcquisition(input: CreateAcquisitionInput): Promise<AcquisitionDetail>;
  updateAcquisition(
    id: number,
    input: CreateAcquisitionInput & { readonly status: RequestStatus },
  ): Promise<AcquisitionDetail>;
  completeAcquisition(id: number): Promise<AcquisitionDetail>;
  cancelAcquisition(id: number): Promise<void>;
  managements(query?: { readonly currentPage?: number }): Promise<ManagementPage>;
  managementPlans(
    id: number,
    query?: { readonly currentPage?: number },
  ): Promise<ManagementPlanPage>;
};

const STATUS: Record<RequestStatus, string> = {
  0: 'Pendiente',
  1: 'Programada',
  2: 'En progreso',
  3: 'Aprobada',
  4: 'Rechazada',
  5: 'Completada',
  99: 'Cancelada',
};
const PRIORITY: Record<RequestPriority, string> = {
  0: 'Baja',
  1: 'Media',
  2: 'Alta',
  3: 'Crítica',
};

function blank(managementId = 0): CreateAcquisitionInput {
  return {
    managementId,
    equipmentUnitId: 0,
    description: '',
    observations: null,
    priority: 1,
    investmentCode: '',
    costCenter: '',
    costs: [],
    isDraft: false,
  };
}

function newCost(): MaintenanceCostInput {
  return { concept: '', quantity: 1, unitPrice: 0, category: 1, description: null };
}

export function AcquisitionsPanel({ api }: { readonly api: AcquisitionsApi }): ReactElement {
  const [page, setPage] = useState<AcquisitionPage | null>(null);
  const [managements, setManagements] = useState<ManagementPage | null>(null);
  const [plans, setPlans] = useState<ManagementPlanPage | null>(null);
  const [managementId, setManagementId] = useState<number | ''>('');
  const [form, setForm] = useState<CreateAcquisitionInput>(blank());
  const [editing, setEditing] = useState<AcquisitionRecord | null>(null);
  const [statusFilter, setStatusFilter] = useState<RequestStatus | ''>('');
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [feedback, setFeedback] = useState<string | null>(null);

  const load = useCallback(async () => {
    setLoading(true);
    try {
      setPage(
        await api.acquisitions({
          currentPage: 1,
          managementId: managementId === '' ? undefined : managementId,
          statusFilter: statusFilter === '' ? undefined : statusFilter,
        }),
      );
      setError(null);
    } catch {
      setError('No fue posible cargar las adquisiciones L-12.');
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
          if (active !== undefined) void chooseManagement(active.id);
        }
      })
      .catch(() => setError('No fue posible cargar las gestiones para L-12.'));
    // The selected management is deliberately initialized only once from the active site.
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [api]);

  async function chooseManagement(value: number | ''): Promise<void> {
    setManagementId(value);
    setEditing(null);
    setForm(blank(value === '' ? 0 : value));
    setPlans(null);
    if (value !== '') {
      try {
        setPlans(await api.managementPlans(value, { currentPage: 1 }));
      } catch {
        setError('No fue posible cargar los activos disponibles para L-12.');
      }
    }
  }

  function edit(item: AcquisitionRecord): void {
    setEditing(item);
    setManagementId(item.managementId);
    void api
      .acquisitionDetail(item.id)
      .then((detail) =>
        setForm({
          managementId: detail.managementId,
          equipmentUnitId: detail.equipmentUnitId,
          description: detail.description,
          observations: detail.observations,
          priority: detail.priority,
          investmentCode: detail.investmentCode ?? '',
          costCenter: detail.costCenter ?? '',
          costs: detail.costs,
          isDraft: detail.isDraft,
        }),
      )
      .catch(() => setError('No fue posible cargar el detalle de la adquisición.'));
    void api
      .managementPlans(item.managementId, { currentPage: 1 })
      .then(setPlans)
      .catch(() => setError('No fue posible cargar el plan de activos.'));
  }

  function setCost(index: number, value: Partial<MaintenanceCostInput>): void {
    const costs = [...(form.costs ?? [])];
    const current = costs[index];
    if (current === undefined) return;
    costs[index] = { ...current, ...value };
    setForm({ ...form, costs });
  }

  async function submit(event: FormEvent<HTMLFormElement>, draft: boolean): Promise<void> {
    event.preventDefault();
    setError(null);
    setFeedback(null);
    if (form.managementId < 1 || form.equipmentUnitId < 1) {
      setError('Selecciona una gestión y una unidad del plan L-12.');
      return;
    }
    try {
      const input = { ...form, isDraft: draft };
      if (editing === null) await api.createAcquisition(input);
      else await api.updateAcquisition(editing.id, { ...input, status: editing.status });
      setFeedback(draft ? 'Borrador L-12 guardado correctamente.' : 'Adquisición L-12 guardada.');
      setEditing(null);
      setForm(blank(form.managementId));
      await load();
    } catch (caught) {
      setError(
        caught instanceof ApiClientError
          ? caught.failure.error.message
          : 'No fue posible guardar la adquisición L-12.',
      );
    }
  }

  async function command(action: 'complete' | 'cancel', item: AcquisitionRecord): Promise<void> {
    const verb = action === 'complete' ? 'confirmar' : 'cancelar';
    if (!window.confirm(`¿Confirmas ${verb} la adquisición #${item.id}?`)) return;
    try {
      if (action === 'complete') await api.completeAcquisition(item.id);
      else await api.cancelAcquisition(item.id);
      setFeedback(`Adquisición #${item.id} ${action === 'complete' ? 'confirmada' : 'cancelada'}.`);
      await load();
    } catch (caught) {
      setError(
        caught instanceof ApiClientError
          ? caught.failure.error.message
          : 'No fue posible ejecutar la acción L-12.',
      );
    }
  }

  return (
    <section className="catalog-panel acquisition-panel" aria-label="Adquisiciones L-12">
      <div className="catalog-heading">
        <div>
          <span className="welcome-kicker">Wizard · L-12</span>
          <h2>Adquisiciones</h2>
          <p>Costos, códigos de inversión y borradores se conservan en la sede activa.</p>
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
          <option value="">Todas las gestiones</option>
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
              event.target.value === '' ? '' : (Number(event.target.value) as RequestStatus),
            )
          }
        >
          <option value="">Todos los estados</option>
          {Object.entries(STATUS).map(([key, value]) => (
            <option key={key} value={key}>
              {value}
            </option>
          ))}
        </select>
      </div>
      <div className="catalog-layout">
        <form className="catalog-form" onSubmit={(event) => void submit(event, false)}>
          <h3>{editing === null ? 'Nueva adquisición' : `Editar adquisición #${editing.id}`}</h3>
          <label>
            Gestión
            <select
              className="form-control"
              required
              value={form.managementId || ''}
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
          </label>
          <label>
            Unidad del plan L-12
            <select
              className="form-control"
              required
              value={form.equipmentUnitId || ''}
              onChange={(event) =>
                setForm({ ...form, equipmentUnitId: Number(event.target.value) })
              }
            >
              <option value="">Seleccionar unidad</option>
              {plans?.items
                .filter((item) => item.equipmentUnitId !== null && item.currentPhase === 6)
                .map((item) => (
                  <option key={item.equipmentUnitId} value={item.equipmentUnitId!}>
                    {item.inventoryNumber} · {item.equipmentName ?? 'Equipo'}
                  </option>
                ))}
            </select>
          </label>
          <label>
            Descripción / justificación
            <textarea
              className="form-control"
              required
              maxLength={1000}
              value={form.description}
              onChange={(event) => setForm({ ...form, description: event.target.value })}
            />
          </label>
          <label>
            Código de inversión
            <input
              className="form-control"
              required
              maxLength={50}
              value={form.investmentCode}
              onChange={(event) => setForm({ ...form, investmentCode: event.target.value })}
            />
          </label>
          <label>
            Centro de costo
            <input
              className="form-control"
              required
              maxLength={100}
              value={form.costCenter}
              onChange={(event) => setForm({ ...form, costCenter: event.target.value })}
            />
          </label>
          <label>
            Prioridad
            <select
              className="form-control"
              value={form.priority ?? 1}
              onChange={(event) =>
                setForm({ ...form, priority: Number(event.target.value) as RequestPriority })
              }
            >
              {Object.entries(PRIORITY).map(([key, value]) => (
                <option key={key} value={key}>
                  {value}
                </option>
              ))}
            </select>
          </label>
          <label>
            Observaciones
            <textarea
              className="form-control"
              maxLength={500}
              value={form.observations ?? ''}
              onChange={(event) => setForm({ ...form, observations: event.target.value || null })}
            />
          </label>
          <fieldset className="verification-checklist">
            <legend>Costos</legend>
            {(form.costs ?? []).map((cost, index) => (
              <div className="cost-row" key={cost.id ?? index}>
                <input
                  className="form-control"
                  placeholder="Concepto"
                  value={cost.concept}
                  onChange={(event) => setCost(index, { concept: event.target.value })}
                />
                <input
                  className="form-control"
                  type="number"
                  min={0}
                  step="0.01"
                  placeholder="Valor unitario"
                  value={cost.unitPrice}
                  onChange={(event) => setCost(index, { unitPrice: Number(event.target.value) })}
                />
                <button
                  type="button"
                  className="btn btn-sm btn-outline-danger"
                  onClick={() =>
                    setForm({ ...form, costs: (form.costs ?? []).filter((_, i) => i !== index) })
                  }
                >
                  Quitar
                </button>
              </div>
            ))}
            <button
              type="button"
              className="btn btn-sm btn-light"
              onClick={() => setForm({ ...form, costs: [...(form.costs ?? []), newCost()] })}
            >
              Agregar costo
            </button>
          </fieldset>
          <div className="catalog-actions">
            <button className="btn btn-primary" type="submit">
              Guardar adquisición
            </button>
            <button
              className="btn btn-light"
              type="button"
              onClick={(event) => void submit(event as unknown as FormEvent<HTMLFormElement>, true)}
            >
              Guardar borrador
            </button>
            {editing !== null ? (
              <button
                type="button"
                className="btn btn-light"
                onClick={() => {
                  setEditing(null);
                  setForm(blank(managementId === '' ? 0 : managementId));
                }}
              >
                Cancelar
              </button>
            ) : null}
          </div>
        </form>
        <div className="catalog-table-wrap">
          {loading ? (
            <p className="text-muted">Cargando adquisiciones…</p>
          ) : page?.items.length === 0 ? (
            <div className="empty-state">
              <i className="mdi mdi-cart-outline" />
              <h3>No hay adquisiciones</h3>
              <p>Los registros L-12 de la sede activa aparecerán aquí.</p>
            </div>
          ) : (
            <table className="catalog-table">
              <thead>
                <tr>
                  <th>Adquisición</th>
                  <th>Activo</th>
                  <th>Inversión</th>
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
                      <small>{item.description}</small>
                    </td>
                    <td>
                      {item.inventoryNumber ?? 'Sin unidad'}
                      <small>{item.equipmentName ?? 'Equipo'}</small>
                    </td>
                    <td>
                      {item.investmentCode ?? 'Sin código'}
                      <small>{item.costCenter ?? 'Sin centro'}</small>
                    </td>
                    <td>
                      <span className={`status-pill status-${item.status}`}>
                        {item.isDraft ? 'Borrador' : STATUS[item.status]}
                      </span>
                    </td>
                    <td>
                      <div className="table-actions">
                        <button
                          type="button"
                          className="btn btn-sm btn-outline-secondary"
                          onClick={() => edit(item)}
                        >
                          Editar
                        </button>
                        {item.status !== 5 && item.status !== 99 ? (
                          <button
                            type="button"
                            className="btn btn-sm btn-outline-success"
                            onClick={() => void command('complete', item)}
                          >
                            Confirmar
                          </button>
                        ) : null}
                        {item.status !== 99 ? (
                          <button
                            type="button"
                            className="btn btn-sm btn-outline-danger"
                            onClick={() => void command('cancel', item)}
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
