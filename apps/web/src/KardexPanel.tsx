import { useCallback, useEffect, useState, type FormEvent, type ReactElement } from 'react';
import { ApiClientError } from '@lu/api-client';
import type { KardexInput, KardexPage, KardexRecord, KardexQuery } from '@lu/contracts';

export type KardexApi = {
  kardex(query?: KardexQuery): Promise<KardexPage>;
  kardexDetail(planId: number): Promise<KardexRecord>;
  saveKardexDraft(input: KardexInput): Promise<KardexRecord>;
  completeKardex(input: KardexInput): Promise<KardexRecord>;
};

function asInput(value: KardexRecord): KardexInput {
  return {
    planId: value.planId,
    technicianId: value.technicianId,
    scheduledDate: value.scheduledDate,
    startDate: value.startDate,
    endDate: value.endDate,
    actualReturnDate: value.actualReturnDate,
    description: value.description,
    actualCost: value.actualCost,
    suggestedNextMaintenanceDate: value.suggestedNextMaintenanceDate,
    satisfactionLevel: value.satisfactionLevel,
    recommendations: value.recommendations,
    observations: value.observations,
    tasks: value.tasks,
    costs: value.costs,
  };
}

export function KardexPanel({ api }: { readonly api: KardexApi }): ReactElement {
  const [page, setPage] = useState<KardexPage | null>(null);
  const [selected, setSelected] = useState<KardexRecord | null>(null);
  const [form, setForm] = useState<KardexInput | null>(null);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [feedback, setFeedback] = useState<string | null>(null);
  const load = useCallback(async () => {
    setLoading(true);
    try {
      setPage(await api.kardex({ currentPage: 1 }));
      setError(null);
    } catch {
      setError('No fue posible cargar Kardex/L-48.');
    } finally {
      setLoading(false);
    }
  }, [api]);
  useEffect(() => {
    void load();
  }, [load]);
  async function select(item: KardexRecord): Promise<void> {
    try {
      const detail = await api.kardexDetail(item.planId);
      setSelected(detail);
      setForm(asInput(detail));
    } catch {
      setError('No fue posible cargar el detalle del Kardex.');
    }
  }
  function toggleTask(index: number): void {
    if (form === null) return;
    const tasks = [...(form.tasks ?? [])];
    const current = tasks[index];
    if (current === undefined) return;
    tasks[index] = { ...current, isCompleted: !current.isCompleted };
    setForm({ ...form, tasks });
  }
  async function submit(event: FormEvent<HTMLFormElement>, complete: boolean): Promise<void> {
    event.preventDefault();
    if (form === null) return;
    setError(null);
    setFeedback(null);
    try {
      const result = complete
        ? await api.completeKardex(form)
        : await api.saveKardexDraft({ ...form, isDraft: true });
      setSelected(result);
      setForm(asInput(result));
      setFeedback(
        complete ? 'Kardex completado y enviado a adquisiciones.' : 'Borrador de Kardex guardado.',
      );
      await load();
    } catch (caught) {
      setError(
        caught instanceof ApiClientError
          ? caught.failure.error.message
          : 'No fue posible guardar Kardex.',
      );
    }
  }
  return (
    <section className="catalog-panel kardex-panel" aria-label="Kardex y L-48">
      <div className="catalog-heading">
        <div>
          <span className="welcome-kicker">Wizard · L-48 / Kardex</span>
          <h2>Kardex e historial</h2>
          <p>
            El cierre valida tareas, costos, devolución y estado operativo antes de pasar a
            adquisiciones.
          </p>
        </div>
        <span className="catalog-count">{page?.totalCount ?? 0} planes</span>
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
        <div className="catalog-table-wrap">
          {loading ? (
            <p className="text-muted">Cargando Kardex…</p>
          ) : page?.items.length === 0 ? (
            <div className="empty-state">
              <i className="mdi mdi-history" />
              <h3>No hay planes en Kardex</h3>
              <p>Las salidas confirmadas aparecerán aquí.</p>
            </div>
          ) : (
            <table className="catalog-table">
              <thead>
                <tr>
                  <th>Activo</th>
                  <th>Gestión</th>
                  <th>Avance</th>
                  <th>Estado</th>
                  <th />
                </tr>
              </thead>
              <tbody>
                {page?.items.map((item) => (
                  <tr key={item.planId}>
                    <td>
                      <strong>{item.inventoryNumber ?? 'Sin inventario'}</strong>
                      <small>{item.equipmentName ?? 'Equipo'}</small>
                    </td>
                    <td>{item.managementCode ?? item.managementId}</td>
                    <td>
                      {item.completionPercentage}%<small>{item.actualCost.toFixed(2)} costo</small>
                    </td>
                    <td>
                      {item.currentPhase === 6
                        ? 'Adquisición'
                        : item.isDraft
                          ? 'Borrador'
                          : 'Pendiente'}
                    </td>
                    <td>
                      <button
                        type="button"
                        className="btn btn-sm btn-outline-primary"
                        onClick={() => void select(item)}
                      >
                        Abrir
                      </button>
                    </td>
                  </tr>
                ))}
              </tbody>
            </table>
          )}
        </div>
        {selected !== null && form !== null ? (
          <form className="catalog-form" onSubmit={(event) => void submit(event, true)}>
            <h3>Kardex · {selected.inventoryNumber ?? selected.equipmentUnitId}</h3>
            <label>
              Técnico responsable
              <input
                className="form-control"
                type="number"
                min={1}
                value={form.technicianId ?? ''}
                onChange={(event) =>
                  setForm({
                    ...form,
                    technicianId: event.target.value === '' ? null : Number(event.target.value),
                  })
                }
              />
            </label>
            <label>
              Fecha programada
              <input
                className="form-control"
                type="date"
                value={form.scheduledDate ?? ''}
                onChange={(event) =>
                  setForm({ ...form, scheduledDate: event.target.value || null })
                }
              />
            </label>
            <label>
              Inicio real
              <input
                className="form-control"
                type="datetime-local"
                value={form.startDate ? form.startDate.slice(0, 16) : ''}
                onChange={(event) => setForm({ ...form, startDate: event.target.value || null })}
              />
            </label>
            <label>
              Finalización
              <input
                className="form-control"
                type="datetime-local"
                value={form.endDate ? form.endDate.slice(0, 16) : ''}
                onChange={(event) => setForm({ ...form, endDate: event.target.value || null })}
              />
            </label>
            <label>
              Devolución L-3
              <input
                className="form-control"
                type="date"
                value={form.actualReturnDate ?? ''}
                onChange={(event) =>
                  setForm({ ...form, actualReturnDate: event.target.value || null })
                }
              />
            </label>
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
              Descripción
              <textarea
                className="form-control"
                maxLength={2000}
                value={form.description ?? ''}
                onChange={(event) => setForm({ ...form, description: event.target.value || null })}
              />
            </label>
            <fieldset className="verification-checklist">
              <legend>Tareas L-48</legend>
              {(form.tasks ?? []).map((task, index) => (
                <label key={`${task.id}-${index}`}>
                  <span>{task.description}</span>
                  <input
                    type="checkbox"
                    checked={task.isCompleted}
                    onChange={() => toggleTask(index)}
                  />
                </label>
              ))}
            </fieldset>
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
            <label>
              Observaciones
              <textarea
                className="form-control"
                maxLength={1000}
                value={form.observations ?? ''}
                onChange={(event) => setForm({ ...form, observations: event.target.value || null })}
              />
            </label>
            <div className="catalog-actions">
              <button className="btn btn-primary" type="submit">
                Completar Kardex
              </button>
              <button
                className="btn btn-light"
                type="button"
                onClick={(event) =>
                  void submit(event as unknown as FormEvent<HTMLFormElement>, false)
                }
              >
                Guardar borrador
              </button>
            </div>
          </form>
        ) : null}
      </div>
    </section>
  );
}
