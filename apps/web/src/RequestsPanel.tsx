import { useCallback, useEffect, useState, type FormEvent, type ReactElement } from 'react';
import { ApiClientError } from '@lu/api-client';
import type {
  CreateRequestInput,
  ManagementPage,
  ManagementPlanPage,
  RequestDetail,
  RequestPage,
  RequestPriority,
  RequestRecord,
  RequestStatus,
} from '@lu/contracts';

export type RequestsApi = {
  requests(query?: {
    readonly currentPage?: number;
    readonly managementId?: number;
    readonly statusFilter?: RequestStatus;
    readonly priorityFilter?: RequestPriority;
  }): Promise<RequestPage>;
  requestDetail(id: number): Promise<RequestDetail>;
  createRequest(input: CreateRequestInput): Promise<RequestDetail>;
  updateRequest(
    id: number,
    input: CreateRequestInput & { readonly status: RequestStatus },
  ): Promise<RequestDetail>;
  completeRequest(id: number): Promise<RequestDetail>;
  cancelRequest(id: number): Promise<void>;
  managements(query?: { readonly currentPage?: number }): Promise<ManagementPage>;
  managementPlans(
    id: number,
    query?: { readonly currentPage?: number },
  ): Promise<ManagementPlanPage>;
};

const PRIORITIES: Record<RequestPriority, string> = {
  0: 'Baja',
  1: 'Media',
  2: 'Alta',
  3: 'Crítica',
};
const STATUSES: Record<RequestStatus, string> = {
  0: 'Pendiente',
  1: 'Programada',
  2: 'En progreso',
  3: 'Aprobada',
  4: 'Rechazada',
  5: 'Completada',
  99: 'Cancelada',
};

function blank(): CreateRequestInput {
  return {
    managementId: 0,
    equipmentUnitId: 0,
    description: '',
    priority: 1,
    observations: null,
    suggestion: null,
    requestDate: new Date().toISOString().slice(0, 10),
    estimatedRepairTime: null,
    type: 1,
    investmentCode: null,
    costCenter: null,
    isDraft: false,
  };
}

export function RequestsPanel({ api }: { readonly api: RequestsApi }): ReactElement {
  const [page, setPage] = useState<RequestPage | null>(null);
  const [managements, setManagements] = useState<ManagementPage | null>(null);
  const [plans, setPlans] = useState<ManagementPlanPage | null>(null);
  const [managementId, setManagementId] = useState<number | ''>('');
  const [form, setForm] = useState<CreateRequestInput>(blank());
  const [editing, setEditing] = useState<RequestRecord | null>(null);
  const [statusFilter, setStatusFilter] = useState<RequestStatus | ''>('');
  const [priorityFilter, setPriorityFilter] = useState<RequestPriority | ''>('');
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [feedback, setFeedback] = useState<string | null>(null);

  const load = useCallback(async () => {
    setLoading(true);
    try {
      const result = await api.requests({
        currentPage: 1,
        managementId: managementId === '' ? undefined : managementId,
        statusFilter: statusFilter === '' ? undefined : statusFilter,
        priorityFilter: priorityFilter === '' ? undefined : priorityFilter,
      });
      setPage(result);
      setError(null);
    } catch {
      setError('No fue posible cargar las solicitudes L-7.');
    } finally {
      setLoading(false);
    }
  }, [api, managementId, priorityFilter, statusFilter]);

  useEffect(() => {
    void (async () => {
      try {
        const result = await api.managements({ currentPage: 1 });
        setManagements(result);
        const active = result.items.find((item) => item.status === 0) ?? result.items[0];
        if (active !== undefined && managementId === '') {
          setManagementId(active.id);
          setForm((current) => ({ ...current, managementId: active.id }));
        }
      } catch {
        setError('No fue posible cargar las gestiones para L-7.');
      }
    })();
  }, [api, managementId]);

  useEffect(() => {
    void load();
  }, [load]);

  async function chooseManagement(value: number | ''): Promise<void> {
    setManagementId(value);
    setEditing(null);
    setForm({ ...blank(), managementId: value === '' ? 0 : value });
    setPlans(null);
    if (value === '') return;
    try {
      setPlans(await api.managementPlans(value, { currentPage: 1 }));
    } catch {
      setError('No fue posible cargar los activos planificados.');
    }
  }

  function edit(item: RequestRecord): void {
    setEditing(item);
    setManagementId(item.managementId);
    setForm({
      managementId: item.managementId,
      equipmentUnitId: item.equipmentUnitId ?? 0,
      description: item.description,
      priority: item.priority,
      observations: item.observations,
      suggestion: item.suggestion,
      requestDate: item.requestDate,
      estimatedRepairTime: item.estimatedRepairTime,
      type: item.type,
      investmentCode: item.investmentCode,
      costCenter: item.costCenter,
      isDraft: item.isDraft,
    });
    void api
      .managementPlans(item.managementId, { currentPage: 1 })
      .then(setPlans)
      .catch(() => {
        setError('No fue posible cargar el plan de la solicitud.');
      });
  }

  async function submit(event: FormEvent<HTMLFormElement>, draft: boolean): Promise<void> {
    event.preventDefault();
    setError(null);
    setFeedback(null);
    if (form.managementId < 1 || form.equipmentUnitId < 1) {
      setError('Selecciona una gestión y una unidad física.');
      return;
    }
    try {
      const input = { ...form, isDraft: draft };
      if (editing === null) await api.createRequest(input);
      else await api.updateRequest(editing.id, { ...input, status: editing.status });
      setFeedback(
        draft ? 'Borrador L-7 guardado correctamente.' : 'Solicitud L-7 guardada correctamente.',
      );
      setEditing(null);
      setForm({ ...blank(), managementId: form.managementId });
      await load();
    } catch (caught) {
      setError(
        caught instanceof ApiClientError
          ? caught.failure.error.message
          : 'No fue posible guardar la solicitud L-7.',
      );
    }
  }

  async function command(action: 'complete' | 'cancel', item: RequestRecord): Promise<void> {
    const text = action === 'complete' ? 'completar' : 'cancelar';
    if (!window.confirm(`¿Confirmas ${text} la solicitud #${item.id}?`)) return;
    try {
      if (action === 'complete') await api.completeRequest(item.id);
      else await api.cancelRequest(item.id);
      setFeedback(`Solicitud #${item.id} ${text} correctamente.`);
      await load();
    } catch (caught) {
      setError(
        caught instanceof ApiClientError
          ? caught.failure.error.message
          : `No fue posible ${text} la solicitud.`,
      );
    }
  }

  return (
    <section className="catalog-panel request-panel" aria-label="Solicitudes L-7">
      <div className="catalog-heading">
        <div>
          <span className="welcome-kicker">Wizard · L-7</span>
          <h2>Solicitudes</h2>
          <p>Las solicitudes conservan el borrador y enlazan la unidad física con su gestión.</p>
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
      <div className="catalog-toolbar request-filters">
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
                {item.code} · {item.type === 0 ? 'Preventiva' : 'Correctiva'}
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
          {Object.entries(STATUSES).map(([key, label]) => (
            <option key={key} value={key}>
              {label}
            </option>
          ))}
        </select>
        <select
          className="form-control"
          value={priorityFilter}
          onChange={(event) =>
            setPriorityFilter(
              event.target.value === '' ? '' : (Number(event.target.value) as RequestPriority),
            )
          }
        >
          <option value="">Todas las prioridades</option>
          {Object.entries(PRIORITIES).map(([key, label]) => (
            <option key={key} value={key}>
              {label}
            </option>
          ))}
        </select>
      </div>
      <div className="catalog-layout">
        <form className="catalog-form" onSubmit={(event) => void submit(event, false)}>
          <h3>{editing === null ? 'Nueva solicitud' : `Editar solicitud #${editing.id}`}</h3>
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
            Unidad física
            <select
              className="form-control"
              required
              value={form.equipmentUnitId || ''}
              onChange={(event) =>
                setForm({ ...form, equipmentUnitId: Number(event.target.value) })
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
            Prioridad
            <select
              className="form-control"
              value={form.priority ?? 1}
              onChange={(event) =>
                setForm({ ...form, priority: Number(event.target.value) as RequestPriority })
              }
            >
              {Object.entries(PRIORITIES).map(([key, label]) => (
                <option key={key} value={key}>
                  {label}
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
          <label>
            Tiempo estimado de reparación
            <input
              className="form-control"
              maxLength={100}
              value={form.estimatedRepairTime ?? ''}
              onChange={(event) =>
                setForm({ ...form, estimatedRepairTime: event.target.value || null })
              }
            />
          </label>
          <div className="catalog-actions">
            <button className="btn btn-primary" type="submit">
              Guardar solicitud
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
                className="btn btn-light"
                type="button"
                onClick={() => {
                  setEditing(null);
                  setForm({ ...blank(), managementId: managementId === '' ? 0 : managementId });
                }}
              >
                Cancelar
              </button>
            ) : null}
          </div>
        </form>
        <div className="catalog-table-wrap">
          {loading ? (
            <p className="text-muted">Cargando solicitudes…</p>
          ) : page?.items.length === 0 ? (
            <div className="empty-state">
              <i className="mdi mdi-clipboard-alert-outline" />
              <h3>No hay solicitudes</h3>
              <p>Las solicitudes L-7 creadas en esta sede aparecerán aquí.</p>
            </div>
          ) : (
            <table className="catalog-table">
              <thead>
                <tr>
                  <th>Solicitud</th>
                  <th>Activo</th>
                  <th>Prioridad</th>
                  <th>Estado</th>
                  <th>Acciones</th>
                </tr>
              </thead>
              <tbody>
                {page?.items.map((item) => (
                  <tr key={item.id}>
                    <td>
                      <strong>#{item.id}</strong>
                      <small>{item.managementCode ?? `Gestión ${item.managementId}`}</small>
                      <small>{item.description}</small>
                    </td>
                    <td>
                      {item.inventoryNumber ?? 'Sin unidad'}
                      <small>{item.laboratoryName ?? 'Sin laboratorio'}</small>
                    </td>
                    <td>
                      <span className={`status-pill status-${item.priority}`}>
                        {PRIORITIES[item.priority]}
                      </span>
                    </td>
                    <td>
                      <span className={`status-pill status-${item.status}`}>
                        {item.isDraft ? 'Borrador' : STATUSES[item.status]}
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
                        {item.status !== 99 && item.status !== 5 ? (
                          <button
                            type="button"
                            className="btn btn-sm btn-outline-success"
                            onClick={() => void command('complete', item)}
                          >
                            Continuar L-8
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
