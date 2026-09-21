import { useCallback, useEffect, useState, type FormEvent, type ReactElement } from 'react';
import { ApiClientError } from '@lu/api-client';
import type {
  CreateMaintenanceInput,
  ManagementPage,
  ManagementPlanPage,
  MaintenanceDetail,
  MaintenancePage,
  MaintenanceRecord,
  MaintenanceStatus,
  MaintenanceTaskInput,
} from '@lu/contracts';

export type MaintenanceApi = {
  maintenances(query?: {
    readonly currentPage?: number;
    readonly managementId?: number;
    readonly statusFilter?: MaintenanceStatus;
  }): Promise<MaintenancePage>;
  maintenanceDetail(id: number): Promise<MaintenanceDetail>;
  createMaintenance(input: CreateMaintenanceInput): Promise<MaintenanceDetail>;
  updateMaintenance(
    id: number,
    input: CreateMaintenanceInput & { readonly status: MaintenanceStatus },
  ): Promise<MaintenanceDetail>;
  completeMaintenance(id: number): Promise<MaintenanceDetail>;
  cancelMaintenance(id: number): Promise<void>;
  managements(query?: { readonly currentPage?: number }): Promise<ManagementPage>;
  managementPlans(
    id: number,
    query?: { readonly currentPage?: number },
  ): Promise<ManagementPlanPage>;
};

const STATUSES: Record<MaintenanceStatus, string> = {
  0: 'Pendiente',
  1: 'En progreso',
  2: 'Completado',
  3: 'Programado',
  99: 'Cancelado',
};
function defaults(): MaintenanceTaskInput[] {
  return [
    { description: 'Limpieza y desinfección de componentes', isCompleted: false },
    { description: 'Calibración y ajuste del sistema', isCompleted: false },
    { description: 'Pruebas de esfuerzo y carga operativa', isCompleted: false },
    { description: 'Revisión final de seguridad y cierre', isCompleted: false },
  ];
}
function blank(): CreateMaintenanceInput {
  return {
    managementId: 0,
    equipmentUnitId: 0,
    maintenanceType: 1,
    serviceType: 0,
    scheduledDate: new Date().toISOString().slice(0, 10),
    status: 1,
    description: null,
    observations: null,
    recommendations: null,
    satisfactionLevel: null,
    actualCost: null,
    estimatedCost: null,
    tasks: defaults(),
    costs: [],
    isDraft: false,
  };
}

export function MaintenancesPanel({ api }: { readonly api: MaintenanceApi }): ReactElement {
  const [page, setPage] = useState<MaintenancePage | null>(null);
  const [managements, setManagements] = useState<ManagementPage | null>(null);
  const [plans, setPlans] = useState<ManagementPlanPage | null>(null);
  const [managementId, setManagementId] = useState<number | ''>('');
  const [form, setForm] = useState<CreateMaintenanceInput>(blank());
  const [editing, setEditing] = useState<MaintenanceRecord | null>(null);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [feedback, setFeedback] = useState<string | null>(null);

  const load = useCallback(async () => {
    setLoading(true);
    try {
      setPage(
        await api.maintenances({
          currentPage: 1,
          managementId: managementId === '' ? undefined : managementId,
        }),
      );
      setError(null);
    } catch {
      setError('No fue posible cargar los mantenimientos L-8.');
    } finally {
      setLoading(false);
    }
  }, [api, managementId]);
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
            setForm((current) => ({ ...current, managementId: active.id }));
          }
        }
      })
      .catch(() => setError('No fue posible cargar las gestiones para L-8.'));
  }, [api, managementId]);
  async function chooseManagement(value: number | ''): Promise<void> {
    setManagementId(value);
    setEditing(null);
    setPlans(null);
    setForm({ ...blank(), managementId: value === '' ? 0 : value });
    if (value !== '')
      try {
        setPlans(await api.managementPlans(value, { currentPage: 1 }));
      } catch {
        setError('No fue posible cargar el plan de activos.');
      }
  }
  function edit(item: MaintenanceRecord): void {
    setEditing(item);
    setManagementId(item.managementId);
    void api
      .maintenanceDetail(item.id)
      .then((detail) =>
        setForm({
          managementId: detail.managementId,
          equipmentUnitId: detail.equipmentUnitId,
          requestId: detail.requestId,
          maintenanceType: detail.maintenanceType,
          serviceType: detail.serviceType,
          institutionalCode: detail.institutionalCode,
          technicianId: detail.technicianId,
          scheduledDate: detail.scheduledDate,
          startDate: detail.startDate,
          endDate: detail.endDate,
          description: detail.description,
          status: detail.status,
          estimatedCost: detail.estimatedCost,
          actualCost: detail.actualCost,
          recommendations: detail.recommendations,
          suggestedNextMaintenanceDate: detail.suggestedNextMaintenanceDate,
          satisfactionLevel: detail.satisfactionLevel,
          observations: detail.observations,
          tasks: detail.tasks,
          costs: detail.costs,
          isDraft: detail.isDraft,
        }),
      )
      .catch(() => setError('No fue posible cargar el detalle del mantenimiento.'));
    void api
      .managementPlans(item.managementId, { currentPage: 1 })
      .then(setPlans)
      .catch(() => setError('No fue posible cargar el plan de activos.'));
  }
  async function submit(event: FormEvent<HTMLFormElement>, draft: boolean): Promise<void> {
    event.preventDefault();
    setError(null);
    setFeedback(null);
    if (form.managementId < 1 || form.equipmentUnitId < 1) {
      setError('Selecciona una gestión y unidad física.');
      return;
    }
    try {
      const input = { ...form, status: draft ? 1 : (form.status ?? 1), isDraft: draft };
      if (editing === null) await api.createMaintenance(input);
      else
        await api.updateMaintenance(editing.id, {
          ...input,
          status: input.status as MaintenanceStatus,
        });
      setFeedback(
        draft ? 'Borrador L-8 guardado correctamente.' : 'Mantenimiento guardado correctamente.',
      );
      setEditing(null);
      setForm({ ...blank(), managementId: form.managementId });
      await load();
    } catch (caught) {
      setError(
        caught instanceof ApiClientError
          ? caught.failure.error.message
          : 'No fue posible guardar el mantenimiento.',
      );
    }
  }
  async function command(action: 'complete' | 'cancel', item: MaintenanceRecord): Promise<void> {
    if (
      !window.confirm(
        `¿Confirmas ${action === 'complete' ? 'completar' : 'cancelar'} el mantenimiento #${item.id}?`,
      )
    )
      return;
    try {
      if (action === 'complete') await api.completeMaintenance(item.id);
      else await api.cancelMaintenance(item.id);
      setFeedback(
        `Mantenimiento #${item.id} ${action === 'complete' ? 'completado' : 'cancelado'}.`,
      );
      await load();
    } catch (caught) {
      setError(
        caught instanceof ApiClientError
          ? caught.failure.error.message
          : 'No fue posible ejecutar la acción.',
      );
    }
  }
  function toggleTask(index: number): void {
    const tasks = [...(form.tasks ?? defaults())];
    const current = tasks[index];
    if (current === undefined) return;
    tasks[index] = { ...current, isCompleted: !current.isCompleted };
    setForm({ ...form, tasks });
  }
  return (
    <section className="catalog-panel maintenance-panel" aria-label="Mantenimientos L-8">
      <div className="catalog-heading">
        <div>
          <span className="welcome-kicker">Wizard · L-8 / L-48</span>
          <h2>Mantenimiento</h2>
          <p>El avance técnico, costos y tareas se conservan en la sede activa.</p>
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
      </div>
      <div className="catalog-layout">
        <form className="catalog-form" onSubmit={(event) => void submit(event, false)}>
          <h3>
            {editing === null ? 'Nuevo mantenimiento' : `Editar mantenimiento #${editing.id}`}
          </h3>
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
              <option value="">Seleccionar unidad del plan</option>
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
            Tipo
            <select
              className="form-control"
              value={form.maintenanceType ?? 1}
              onChange={(event) =>
                setForm({
                  ...form,
                  maintenanceType: Number(event.target.value) as 1 | 2 | 3 | 4 | 5 | 99,
                })
              }
            >
              <option value={1}>Preventivo</option>
              <option value={2}>Correctivo</option>
              <option value={3}>Calibración</option>
              <option value={4}>Limpieza</option>
              <option value={5}>Eléctrico</option>
              <option value={99}>Otros</option>
            </select>
          </label>
          <label>
            Fecha programada
            <input
              className="form-control"
              type="date"
              value={form.scheduledDate ?? ''}
              onChange={(event) => setForm({ ...form, scheduledDate: event.target.value || null })}
            />
          </label>
          <label>
            Descripción
            <textarea
              className="form-control"
              maxLength={2000}
              value={form.description ?? ''}
              onChange={(event) => setForm({ ...form, description: event.target.value || null })}
            />
          </label>
          <fieldset className="verification-checklist">
            <legend>Tareas y avance</legend>
            {(form.tasks ?? defaults()).map((task, index) => (
              <label key={`${task.id ?? 'new'}-${index}`}>
                <span>{task.description}</span>
                <input
                  type="checkbox"
                  checked={task.isCompleted === true}
                  onChange={() => toggleTask(index)}
                />
              </label>
            ))}
          </fieldset>
          <label>
            Costo real
            <input
              className="form-control"
              type="number"
              min={0}
              step="0.01"
              value={form.actualCost ?? ''}
              onChange={(event) =>
                setForm({
                  ...form,
                  actualCost: event.target.value === '' ? null : Number(event.target.value),
                })
              }
            />
          </label>
          <label>
            Satisfacción
            <select
              className="form-control"
              value={form.satisfactionLevel ?? ''}
              onChange={(event) =>
                setForm({
                  ...form,
                  satisfactionLevel:
                    event.target.value === ''
                      ? null
                      : (Number(event.target.value) as 1 | 2 | 3 | 4 | 5),
                })
              }
            >
              <option value="">Pendiente</option>
              <option value={5}>Muy satisfecho</option>
              <option value={4}>Satisfecho</option>
              <option value={3}>Ok</option>
              <option value={2}>Insatisfecho</option>
              <option value={1}>Inaceptable</option>
            </select>
          </label>
          <label>
            Observaciones
            <textarea
              className="form-control"
              maxLength={1000}
              value={form.observations ?? ''}
              onChange={(event) => setForm({ ...form, observations: event.target.value || null })}
            />
          </label>
          <label>
            Recomendaciones
            <textarea
              className="form-control"
              maxLength={1000}
              value={form.recommendations ?? ''}
              onChange={(event) =>
                setForm({ ...form, recommendations: event.target.value || null })
              }
            />
          </label>
          <div className="catalog-actions">
            <button className="btn btn-primary" type="submit">
              Guardar mantenimiento
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
            <p className="text-muted">Cargando mantenimientos…</p>
          ) : page?.items.length === 0 ? (
            <div className="empty-state">
              <i className="mdi mdi-wrench-outline" />
              <h3>No hay mantenimientos</h3>
              <p>Los registros L-8 de la sede activa aparecerán aquí.</p>
            </div>
          ) : (
            <table className="catalog-table">
              <thead>
                <tr>
                  <th>Activo</th>
                  <th>Gestión</th>
                  <th>Avance</th>
                  <th>Estado</th>
                  <th>Acciones</th>
                </tr>
              </thead>
              <tbody>
                {page?.items.map((item) => (
                  <tr key={item.id}>
                    <td>
                      <strong>{item.inventoryNumber ?? 'Sin inventario'}</strong>
                      <small>{item.equipmentName ?? 'Equipo'}</small>
                      <small>{item.laboratoryName ?? 'Sin laboratorio'}</small>
                    </td>
                    <td>{item.managementCode ?? item.managementId}</td>
                    <td>
                      <strong>{item.completionPercentage}%</strong>
                      <small>{item.calculatedTotal.toFixed(2)} de costo</small>
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
                        {item.status !== 2 && item.status !== 99 ? (
                          <button
                            type="button"
                            className="btn btn-sm btn-outline-success"
                            onClick={() => void command('complete', item)}
                          >
                            Completar
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
