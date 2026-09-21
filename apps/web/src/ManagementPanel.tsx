import { useCallback, useEffect, useState, type FormEvent, type ReactElement } from 'react';
import { ApiClientError } from '@lu/api-client';
import type {
  CreateManagementInput,
  EquipmentUnitPage,
  ManagementPage,
  ManagementPlanPage,
  ManagementRecord,
  ManagementStatus,
  ManagementType,
  UpdateManagementInput,
} from '@lu/contracts';

export type ManagementApi = {
  managements(query?: {
    readonly currentPage?: number;
    readonly type?: ManagementType;
  }): Promise<ManagementPage>;
  createManagement(input: CreateManagementInput): Promise<ManagementRecord>;
  updateManagement(id: number, input: UpdateManagementInput): Promise<ManagementRecord>;
  activateManagement(id: number): Promise<ManagementRecord>;
  closeManagement(id: number): Promise<ManagementRecord>;
  deleteManagement(id: number): Promise<void>;
  managementPlans(
    id: number,
    query?: { readonly currentPage?: number },
  ): Promise<ManagementPlanPage>;
  syncManagementPlans(
    id: number,
    input: { readonly addUnitIds: readonly number[]; readonly removeUnitIds: readonly number[] },
  ): Promise<void>;
  equipmentUnits?(query?: { readonly currentPage?: number }): Promise<EquipmentUnitPage>;
};

const TYPES: Record<ManagementType, string> = { 0: 'Preventiva', 1: 'Correctiva' };
const STATUSES: Record<ManagementStatus, string> = {
  0: 'Activa',
  1: 'Inactiva',
  2: 'Completada',
  99: 'Eliminada',
};

function blank(): CreateManagementInput {
  return {
    year: new Date().getUTCFullYear(),
    semester: 1,
    description: null,
    startDate: null,
    plannedEndDate: null,
    status: 0,
    type: 0,
    facultyId: null,
  };
}

export function ManagementPanel({ api }: { readonly api: ManagementApi }): ReactElement {
  const [page, setPage] = useState<ManagementPage | null>(null);
  const [type, setType] = useState<ManagementType>(0);
  const [form, setForm] = useState<CreateManagementInput>(blank());
  const [editingId, setEditingId] = useState<number | null>(null);
  const [selected, setSelected] = useState<ManagementRecord | null>(null);
  const [plans, setPlans] = useState<ManagementPlanPage | null>(null);
  const [units, setUnits] = useState<EquipmentUnitPage | null>(null);
  const [selectedUnitIds, setSelectedUnitIds] = useState<Set<number>>(new Set());
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [feedback, setFeedback] = useState<string | null>(null);

  const load = useCallback(async () => {
    setLoading(true);
    try {
      const result = await api.managements({ currentPage: 1, type });
      setPage(result);
      if (selected?.id !== undefined) {
        const current = result.items.find((item) => item.id === selected.id) ?? null;
        setSelected(current);
      }
      setError(null);
    } catch {
      setError('No fue posible cargar las gestiones de la sede.');
    } finally {
      setLoading(false);
    }
  }, [api, selected?.id, type]);

  useEffect(() => {
    void load();
  }, [load]);

  const loadPlans = useCallback(
    async (management: ManagementRecord) => {
      setSelected(management);
      try {
        const planPage = await api.managementPlans(management.id, { currentPage: 1 });
        setPlans(planPage);
        setSelectedUnitIds(
          new Set(
            planPage.items.flatMap((plan) =>
              plan.equipmentUnitId === null ? [] : [plan.equipmentUnitId],
            ),
          ),
        );
        if (management.type === 0 && management.status === 0 && api.equipmentUnits)
          setUnits(await api.equipmentUnits({ currentPage: 1 }));
      } catch {
        setError('No fue posible cargar la planificación de la gestión.');
      }
    },
    [api],
  );

  function reset(): void {
    setEditingId(null);
    setForm(blank());
  }
  function edit(item: ManagementRecord): void {
    setEditingId(item.id);
    setForm({
      year: item.year,
      semester: item.semester,
      description: item.description,
      startDate: item.startDate,
      plannedEndDate: item.plannedEndDate,
      status: item.status === 99 ? 1 : item.status,
      type: item.type,
      facultyId: item.facultyId,
    });
  }
  async function submit(event: FormEvent<HTMLFormElement>): Promise<void> {
    event.preventDefault();
    setError(null);
    setFeedback(null);
    try {
      if (editingId === null) {
        await api.createManagement({ ...form, type, semester: type === 1 ? 0 : form.semester });
        setFeedback('Gestión creada correctamente.');
      } else {
        await api.updateManagement(editingId, {
          ...form,
          type,
          status: form.status ?? 0,
          semester: type === 1 ? 0 : form.semester,
        });
        setFeedback('Gestión actualizada correctamente.');
      }
      reset();
      await load();
    } catch (caught) {
      setError(
        caught instanceof ApiClientError
          ? caught.failure.error.message
          : 'No fue posible guardar la gestión.',
      );
    }
  }
  async function command(action: 'activate' | 'close' | 'delete', id: number): Promise<void> {
    const label = action === 'activate' ? 'activar' : action === 'close' ? 'cerrar' : 'dar de baja';
    if (!window.confirm(`¿Confirmas ${label} esta gestión?`)) return;
    try {
      if (action === 'activate') await api.activateManagement(id);
      else if (action === 'close') await api.closeManagement(id);
      else await api.deleteManagement(id);
      setFeedback(`Gestión ${label} correctamente.`);
      await load();
    } catch (caught) {
      setError(
        caught instanceof ApiClientError
          ? caught.failure.error.message
          : `No fue posible ${label} la gestión.`,
      );
    }
  }
  async function savePlan(): Promise<void> {
    if (selected === null || plans === null) return;
    const current = new Set(
      plans.items.flatMap((plan) => (plan.equipmentUnitId === null ? [] : [plan.equipmentUnitId])),
    );
    const addUnitIds = [...selectedUnitIds].filter((id) => !current.has(id));
    const removeUnitIds = [...current].filter((id) => !selectedUnitIds.has(id));
    try {
      await api.syncManagementPlans(selected.id, { addUnitIds, removeUnitIds });
      setFeedback('Planificación actualizada correctamente.');
      await loadPlans(selected);
      await load();
    } catch (caught) {
      setError(
        caught instanceof ApiClientError
          ? caught.failure.error.message
          : 'No fue posible actualizar la planificación.',
      );
    }
  }

  return (
    <section className="catalog-panel management-panel" aria-label="Gestiones de mantenimiento">
      <div className="catalog-heading">
        <div>
          <span className="welcome-kicker">Procesos operativos</span>
          <h2>Gestiones</h2>
          <p>
            Preventiva y correctiva permanecen separadas por tipo; solo una puede estar activa
            dentro de cada tipo.
          </p>
        </div>
        <span className="catalog-count">{page?.totalCount ?? 0} registradas</span>
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
        <button
          type="button"
          className={`btn ${type === 0 ? 'btn-primary' : 'btn-light'}`}
          onClick={() => setType(0)}
        >
          Preventivas
        </button>
        <button
          type="button"
          className={`btn ${type === 1 ? 'btn-primary' : 'btn-light'}`}
          onClick={() => setType(1)}
        >
          Correctivas
        </button>
      </div>
      <div className="catalog-layout">
        <form className="catalog-form" onSubmit={(event) => void submit(event)}>
          <h3>{editingId === null ? 'Nueva gestión' : 'Editar gestión'}</h3>
          <label>
            Año
            <input
              className="form-control"
              type="number"
              min={2000}
              max={2100}
              required
              value={form.year}
              onChange={(event) => setForm({ ...form, year: Number(event.target.value) })}
            />
          </label>
          <label>
            Semestre
            <select
              className="form-control"
              value={type === 1 ? 0 : form.semester}
              disabled={type === 1}
              onChange={(event) => setForm({ ...form, semester: Number(event.target.value) })}
            >
              <option value={0}>Correctiva</option>
              <option value={1}>I</option>
              <option value={2}>II</option>
            </select>
          </label>
          <label>
            Descripción
            <textarea
              className="form-control"
              maxLength={1000}
              value={form.description ?? ''}
              onChange={(event) => setForm({ ...form, description: event.target.value || null })}
            />
          </label>
          <label>
            Inicio
            <input
              className="form-control"
              type="date"
              value={form.startDate ?? ''}
              onChange={(event) => setForm({ ...form, startDate: event.target.value || null })}
            />
          </label>
          <label>
            Cierre planificado
            <input
              className="form-control"
              type="date"
              value={form.plannedEndDate ?? ''}
              onChange={(event) => setForm({ ...form, plannedEndDate: event.target.value || null })}
            />
          </label>
          <label>
            Estado
            <select
              className="form-control"
              value={form.status ?? 0}
              onChange={(event) =>
                setForm({ ...form, status: Number(event.target.value) as ManagementStatus })
              }
            >
              <option value={0}>Activa</option>
              <option value={1}>Inactiva</option>
              <option value={2}>Completada</option>
            </select>
          </label>
          <div className="catalog-actions">
            <button className="btn btn-primary" type="submit">
              {editingId === null ? 'Crear gestión' : 'Guardar cambios'}
            </button>
            {editingId !== null ? (
              <button className="btn btn-light" type="button" onClick={reset}>
                Cancelar
              </button>
            ) : null}
          </div>
        </form>
        <div className="catalog-table-wrap">
          {loading ? (
            <p className="text-muted">Cargando gestiones…</p>
          ) : page?.items.length === 0 ? (
            <div className="empty-state">
              <i className="mdi mdi-calendar-blank-outline" />
              <h3>No hay gestiones {TYPES[type].toLowerCase()}</h3>
              <p>Crea la primera gestión para iniciar el flujo.</p>
            </div>
          ) : (
            <table className="catalog-table">
              <thead>
                <tr>
                  <th>Código</th>
                  <th>Periodo</th>
                  <th>Estado</th>
                  <th>Activos</th>
                  <th>Acciones</th>
                </tr>
              </thead>
              <tbody>
                {page?.items.map((item) => (
                  <tr key={item.id}>
                    <td>
                      <strong>{item.code}</strong>
                      <small>{TYPES[item.type]}</small>
                    </td>
                    <td>
                      {item.startDate ?? 'Sin inicio'}
                      <small>
                        {item.plannedEndDate ? `hasta ${item.plannedEndDate}` : 'Sin fecha final'}
                      </small>
                    </td>
                    <td>
                      <span className={`status-pill status-${item.status}`}>
                        {STATUSES[item.status]}
                      </span>
                    </td>
                    <td>
                      {item.planCount}
                      <small>{item.completedPlanCount} completados</small>
                    </td>
                    <td>
                      <div className="table-actions">
                        <button
                          type="button"
                          className="btn btn-sm btn-outline-primary"
                          onClick={() => void loadPlans(item)}
                        >
                          Planificar
                        </button>
                        <button
                          type="button"
                          className="btn btn-sm btn-outline-secondary"
                          onClick={() => edit(item)}
                        >
                          Editar
                        </button>
                        {item.status !== 0 && item.status !== 2 ? (
                          <button
                            type="button"
                            className="btn btn-sm btn-outline-success"
                            onClick={() => void command('activate', item.id)}
                          >
                            Activar
                          </button>
                        ) : null}
                        {item.status === 0 ? (
                          <button
                            type="button"
                            className="btn btn-sm btn-outline-warning"
                            onClick={() => void command('close', item.id)}
                          >
                            Cerrar
                          </button>
                        ) : null}
                        {item.status !== 0 ? (
                          <button
                            type="button"
                            className="btn btn-sm btn-outline-danger"
                            onClick={() => void command('delete', item.id)}
                          >
                            Baja
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
      {selected ? (
        <aside className="inventory-detail management-plans" aria-label="Planificación de gestión">
          <div className="catalog-heading">
            <div>
              <span className="welcome-kicker">Plan de activos</span>
              <h3>{selected.code}</h3>
              <p>
                {TYPES[selected.type]} · {STATUSES[selected.status]}
              </p>
            </div>
            <button
              type="button"
              className="btn btn-light"
              onClick={() => {
                setSelected(null);
                setPlans(null);
              }}
            >
              Cerrar
            </button>
          </div>
          {selected.type !== 0 || selected.status !== 0 ? (
            <p className="text-muted">
              La planificación de activos solo está disponible para una gestión preventiva activa.
            </p>
          ) : units === null ? (
            <p className="text-muted">Cargando unidades disponibles…</p>
          ) : (
            <>
              <div className="catalog-actions">
                <button className="btn btn-primary" type="button" onClick={() => void savePlan()}>
                  Guardar selección
                </button>
              </div>
              <div className="unit-selection">
                {units.items.map((unit) => (
                  <label key={unit.id}>
                    <input
                      type="checkbox"
                      checked={selectedUnitIds.has(unit.id)}
                      onChange={(event) => {
                        const next = new Set(selectedUnitIds);
                        if (event.target.checked) next.add(unit.id);
                        else next.delete(unit.id);
                        setSelectedUnitIds(next);
                      }}
                    />{' '}
                    <strong>{unit.inventoryNumber}</strong> · {unit.equipmentName ?? 'Equipo'} ·{' '}
                    {unit.laboratoryName ?? 'Sin laboratorio'}
                  </label>
                ))}
              </div>
              <p className="text-muted">
                {selectedUnitIds.size} unidad(es) seleccionada(s) · {plans?.totalCount ?? 0}{' '}
                plan(es) persistidos
              </p>
            </>
          )}
        </aside>
      ) : null}
    </section>
  );
}
