import { useCallback, useEffect, useState, type FormEvent, type ReactElement } from 'react';
import { ApiClientError } from '@lu/api-client';
import type {
  Career,
  CreateEquipmentUnitInput,
  EquipmentRecord,
  EquipmentUnitDetail,
  EquipmentUnitPage,
  EquipmentUnitStatus,
  Laboratory,
  PhysicalCondition,
  UpdateEquipmentUnitInput,
} from '@lu/contracts';

export type EquipmentUnitsApi = {
  equipmentUnits(query?: {
    readonly currentPage?: number;
    readonly searchTerm?: string;
    readonly laboratoryId?: number;
    readonly statusFilter?: EquipmentUnitStatus;
    readonly includeUnresolved?: boolean;
  }): Promise<EquipmentUnitPage>;
  equipmentUnit(id: number): Promise<EquipmentUnitDetail>;
  createEquipmentUnit(input: CreateEquipmentUnitInput): Promise<EquipmentUnitDetail>;
  updateEquipmentUnit(id: number, input: UpdateEquipmentUnitInput): Promise<EquipmentUnitDetail>;
  deleteEquipmentUnit(id: number): Promise<void>;
  equipment?(query?: {
    readonly currentPage?: number;
  }): Promise<{ readonly items: readonly EquipmentRecord[] }>;
  laboratories?(query?: {
    readonly currentPage?: number;
  }): Promise<{ readonly items: readonly Laboratory[] }>;
  careers?(query?: {
    readonly currentPage?: number;
  }): Promise<{ readonly items: readonly Career[] }>;
};

const STATUS_LABELS: Record<EquipmentUnitStatus, string> = {
  0: 'Operativo',
  1: 'En mantenimiento',
  2: 'Fuera de servicio',
  3: 'De baja',
  4: 'Desmantelado',
  5: 'En reparación',
  6: 'Averiado',
  10: 'En préstamo',
  99: 'Eliminado',
};
const CONDITION_LABELS: Record<PhysicalCondition, string> = {
  1: 'Baja del equipo',
  2: 'Malo',
  3: 'Regular',
  4: 'Bueno',
  5: 'Excelente',
};

function emptyForm(): CreateEquipmentUnitInput {
  return {
    equipmentId: 0,
    laboratoryId: 0,
    careerId: null,
    inventoryNumber: '',
    serialNumber: null,
    internalLocation: null,
    acquisitionDate: null,
    manufacturingDate: null,
    acquisitionValue: null,
    currentStatus: 0,
    physicalCondition: 5,
    notes: null,
  };
}

export function EquipmentUnitsPanel({ api }: { readonly api: EquipmentUnitsApi }): ReactElement {
  const [page, setPage] = useState<EquipmentUnitPage | null>(null);
  const [equipment, setEquipment] = useState<readonly EquipmentRecord[]>([]);
  const [laboratories, setLaboratories] = useState<readonly Laboratory[]>([]);
  const [careers, setCareers] = useState<readonly Career[]>([]);
  const [searchTerm, setSearchTerm] = useState('');
  const [laboratoryId, setLaboratoryId] = useState<number | ''>('');
  const [includeUnresolved, setIncludeUnresolved] = useState(false);
  const [selected, setSelected] = useState<EquipmentUnitDetail | null>(null);
  const [editingId, setEditingId] = useState<number | null>(null);
  const [form, setForm] = useState<CreateEquipmentUnitInput>(emptyForm());
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [feedback, setFeedback] = useState<string | null>(null);

  const load = useCallback(async () => {
    setLoading(true);
    try {
      const [units, equipmentPage, laboratoryPage, careerPage] = await Promise.all([
        api.equipmentUnits({
          currentPage: 1,
          searchTerm: searchTerm || undefined,
          laboratoryId: laboratoryId === '' ? undefined : laboratoryId,
          includeUnresolved,
        }),
        api.equipment ? api.equipment({ currentPage: 1 }) : Promise.resolve({ items: [] }),
        api.laboratories ? api.laboratories({ currentPage: 1 }) : Promise.resolve({ items: [] }),
        api.careers ? api.careers({ currentPage: 1 }) : Promise.resolve({ items: [] }),
      ]);
      setPage(units);
      setEquipment(equipmentPage.items);
      setLaboratories(laboratoryPage.items);
      setCareers(careerPage.items);
      setError(null);
    } catch {
      setError('No fue posible cargar el inventario de unidades físicas.');
    } finally {
      setLoading(false);
    }
  }, [api, includeUnresolved, laboratoryId, searchTerm]);

  useEffect(() => {
    void load();
  }, [load]);

  function edit(item: EquipmentUnitDetail): void {
    setEditingId(item.id);
    setSelected(item);
    setForm({
      equipmentId: item.equipmentId,
      laboratoryId: item.laboratoryId ?? 0,
      careerId: item.careerId,
      inventoryNumber: item.inventoryNumber,
      serialNumber: item.serialNumber,
      internalLocation: item.internalLocation,
      acquisitionDate: item.acquisitionDate,
      manufacturingDate: item.manufacturingDate,
      acquisitionValue: item.acquisitionValue,
      currentStatus: item.currentStatus,
      physicalCondition: item.physicalCondition,
      notes: item.notes,
    });
  }

  function reset(): void {
    setEditingId(null);
    setSelected(null);
    setForm(emptyForm());
  }

  async function inspect(id: number): Promise<void> {
    try {
      setSelected(await api.equipmentUnit(id));
      setError(null);
    } catch {
      setError('No fue posible cargar el detalle de la unidad.');
    }
  }

  async function submit(event: FormEvent<HTMLFormElement>): Promise<void> {
    event.preventDefault();
    setError(null);
    setFeedback(null);
    const input: CreateEquipmentUnitInput = {
      ...form,
      equipmentId: Number(form.equipmentId),
      laboratoryId: Number(form.laboratoryId),
      careerId: form.careerId ? Number(form.careerId) : null,
      acquisitionValue:
        form.acquisitionValue === null || form.acquisitionValue === undefined
          ? null
          : Number(form.acquisitionValue),
    };
    try {
      if (editingId === null) {
        await api.createEquipmentUnit(input);
        setFeedback('Unidad física registrada correctamente.');
      } else {
        await api.updateEquipmentUnit(editingId, { ...input, status: form.currentStatus ?? 0 });
        setFeedback('Unidad física actualizada correctamente.');
      }
      reset();
      await load();
    } catch (caught) {
      setError(
        caught instanceof ApiClientError
          ? caught.failure.error.message
          : 'No fue posible guardar la unidad física.',
      );
    }
  }

  async function remove(id: number): Promise<void> {
    if (!window.confirm('¿Confirmas la baja lógica de esta unidad física?')) return;
    try {
      await api.deleteEquipmentUnit(id);
      setFeedback('Unidad física dada de baja correctamente.');
      if (editingId === id) reset();
      await load();
    } catch {
      setError('No fue posible dar de baja la unidad física.');
    }
  }

  const items = page?.items ?? [];
  return (
    <section className="catalog-panel inventory-panel" aria-label="Inventario de unidades físicas">
      <div className="catalog-heading">
        <div>
          <span className="welcome-kicker">Inventario visual</span>
          <h2>Unidades físicas</h2>
          <p>
            La sede de cada unidad se deriva del laboratorio asignado. Las unidades sin laboratorio
            solo aparecen en revisión.
          </p>
        </div>
        <span className="catalog-count">{page?.totalCount ?? 0} visibles</span>
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
          aria-label="Buscar unidad"
          placeholder="Inventario, serie, equipo o laboratorio"
          value={searchTerm}
          onChange={(event) => setSearchTerm(event.target.value)}
        />
        <select
          className="form-control"
          value={laboratoryId}
          onChange={(event) =>
            setLaboratoryId(event.target.value === '' ? '' : Number(event.target.value))
          }
        >
          <option value="">Todos los laboratorios</option>
          {laboratories
            .filter((item) => item.status === 0)
            .map((item) => (
              <option key={item.id} value={item.id}>
                {item.code} · {item.name}
              </option>
            ))}
        </select>
        <label className="catalog-check">
          <input
            type="checkbox"
            checked={includeUnresolved}
            onChange={(event) => setIncludeUnresolved(event.target.checked)}
          />{' '}
          Revisar sin laboratorio
        </label>
      </div>
      <div className="catalog-layout">
        <form className="catalog-form" onSubmit={(event) => void submit(event)}>
          <h3>{editingId === null ? 'Nueva unidad' : 'Editar unidad'}</h3>
          <label>
            Equipo catálogo
            <select
              className="form-control"
              required
              value={form.equipmentId || ''}
              onChange={(event) => setForm({ ...form, equipmentId: Number(event.target.value) })}
            >
              <option value="">Seleccionar equipo</option>
              {equipment
                .filter((item) => item.status === 0)
                .map((item) => (
                  <option key={item.id} value={item.id}>
                    {item.name} · {item.catalogCode}
                  </option>
                ))}
            </select>
          </label>
          <label>
            Laboratorio
            <select
              className="form-control"
              required
              value={form.laboratoryId || ''}
              onChange={(event) => setForm({ ...form, laboratoryId: Number(event.target.value) })}
            >
              <option value="">Seleccionar laboratorio</option>
              {laboratories
                .filter((item) => item.status === 0)
                .map((item) => (
                  <option key={item.id} value={item.id}>
                    {item.code} · {item.name}
                  </option>
                ))}
            </select>
          </label>
          <label>
            Carrera propietaria
            <select
              className="form-control"
              value={form.careerId ?? ''}
              onChange={(event) =>
                setForm({
                  ...form,
                  careerId: event.target.value === '' ? null : Number(event.target.value),
                })
              }
            >
              <option value="">Sin carrera</option>
              {careers
                .filter((item) => item.status === 0 && item.siteAssigned)
                .map((item) => (
                  <option key={item.id} value={item.id}>
                    {item.name}
                  </option>
                ))}
            </select>
          </label>
          <label>
            Número de inventario
            <input
              className="form-control"
              required
              minLength={3}
              maxLength={50}
              value={form.inventoryNumber}
              onChange={(event) => setForm({ ...form, inventoryNumber: event.target.value })}
            />
          </label>
          <label>
            Número de serie
            <input
              className="form-control"
              maxLength={100}
              value={form.serialNumber ?? ''}
              onChange={(event) => setForm({ ...form, serialNumber: event.target.value || null })}
            />
          </label>
          <label>
            Ubicación interna
            <input
              className="form-control"
              maxLength={200}
              value={form.internalLocation ?? ''}
              onChange={(event) =>
                setForm({ ...form, internalLocation: event.target.value || null })
              }
            />
          </label>
          <div className="catalog-form-grid">
            <label>
              Adquisición
              <input
                className="form-control"
                type="date"
                value={form.acquisitionDate ?? ''}
                onChange={(event) =>
                  setForm({ ...form, acquisitionDate: event.target.value || null })
                }
              />
            </label>
            <label>
              Fabricación
              <input
                className="form-control"
                type="date"
                value={form.manufacturingDate ?? ''}
                onChange={(event) =>
                  setForm({ ...form, manufacturingDate: event.target.value || null })
                }
              />
            </label>
          </div>
          <label>
            Valor de adquisición
            <input
              className="form-control"
              type="number"
              min={0}
              step="0.01"
              value={form.acquisitionValue ?? ''}
              onChange={(event) =>
                setForm({
                  ...form,
                  acquisitionValue: event.target.value === '' ? null : Number(event.target.value),
                })
              }
            />
          </label>
          <div className="catalog-form-grid">
            <label>
              Estado
              <select
                className="form-control"
                value={form.currentStatus ?? 0}
                onChange={(event) =>
                  setForm({
                    ...form,
                    currentStatus: Number(event.target.value) as EquipmentUnitStatus,
                  })
                }
              >
                {Object.entries(STATUS_LABELS)
                  .filter(([key]) => key !== '99')
                  .map(([key, label]) => (
                    <option key={key} value={key}>
                      {label}
                    </option>
                  ))}
              </select>
            </label>
            <label>
              Condición
              <select
                className="form-control"
                value={form.physicalCondition ?? ''}
                onChange={(event) =>
                  setForm({
                    ...form,
                    physicalCondition:
                      event.target.value === ''
                        ? null
                        : (Number(event.target.value) as PhysicalCondition),
                  })
                }
              >
                <option value="">Sin confirmar</option>
                {Object.entries(CONDITION_LABELS).map(([key, label]) => (
                  <option key={key} value={key}>
                    {label}
                  </option>
                ))}
              </select>
            </label>
          </div>
          <label>
            Observaciones
            <textarea
              className="form-control"
              maxLength={2000}
              value={form.notes ?? ''}
              onChange={(event) => setForm({ ...form, notes: event.target.value || null })}
            />
          </label>
          <div className="catalog-actions">
            <button className="btn btn-primary" type="submit">
              {editingId === null ? 'Registrar unidad' : 'Guardar cambios'}
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
            <p className="text-muted">Cargando inventario…</p>
          ) : items.length === 0 ? (
            <div className="empty-state">
              <i className="mdi mdi-package-variant-closed" />
              <h3>Sin unidades para este filtro</h3>
              <p>Registra una unidad física o revisa los criterios de búsqueda.</p>
            </div>
          ) : (
            <table className="catalog-table">
              <thead>
                <tr>
                  <th>Inventario</th>
                  <th>Equipo</th>
                  <th>Laboratorio</th>
                  <th>Estado</th>
                  <th>Acciones</th>
                </tr>
              </thead>
              <tbody>
                {items.map((item) => (
                  <tr key={item.id}>
                    <td>
                      <strong>{item.inventoryNumber}</strong>
                      <small>{item.serialNumber ?? 'Sin serie'}</small>
                    </td>
                    <td>{item.equipmentName ?? 'Equipo no disponible'}</td>
                    <td>
                      {item.laboratoryName ? (
                        `${item.laboratoryCode ?? ''} · ${item.laboratoryName}`
                      ) : (
                        <span className="text-warning">Sin laboratorio</span>
                      )}
                    </td>
                    <td>
                      <span className={`status-pill status-${item.currentStatus}`}>
                        {STATUS_LABELS[item.currentStatus]}
                      </span>
                    </td>
                    <td>
                      <div className="table-actions">
                        <button
                          className="btn btn-sm btn-outline-primary"
                          type="button"
                          onClick={() => void inspect(item.id)}
                        >
                          Detalle
                        </button>
                        <button
                          className="btn btn-sm btn-outline-secondary"
                          type="button"
                          onClick={() =>
                            void api
                              .equipmentUnit(item.id)
                              .then(edit)
                              .catch(() => setError('No fue posible abrir la unidad.'))
                          }
                        >
                          Editar
                        </button>
                        <button
                          className="btn btn-sm btn-outline-danger"
                          type="button"
                          onClick={() => void remove(item.id)}
                        >
                          Baja
                        </button>
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
        <aside className="inventory-detail" aria-label="Detalle de unidad">
          <div className="catalog-heading">
            <div>
              <span className="welcome-kicker">Ficha de unidad</span>
              <h3>{selected.inventoryNumber}</h3>
              <p>
                {selected.equipmentName ?? 'Equipo'} ·{' '}
                {selected.laboratoryName ?? 'Laboratorio pendiente'}
              </p>
            </div>
            <button className="btn btn-light" type="button" onClick={() => setSelected(null)}>
              Cerrar
            </button>
          </div>
          <div className="inventory-detail-grid">
            <span>
              Estado<strong>{STATUS_LABELS[selected.currentStatus]}</strong>
            </span>
            <span>
              Condición
              <strong>
                {selected.physicalCondition
                  ? CONDITION_LABELS[selected.physicalCondition]
                  : 'Sin confirmar'}
              </strong>
            </span>
            <span>
              Serie<strong>{selected.serialNumber ?? '—'}</strong>
            </span>
            <span>
              Ubicación<strong>{selected.internalLocation ?? '—'}</strong>
            </span>
            <span>
              Adquisición
              <strong>
                {selected.acquisitionValue === null
                  ? '—'
                  : selected.acquisitionValue.toLocaleString()}
              </strong>
            </span>
            <span>
              Años de operación
              <strong>
                {selected.yearsInOperation === null ? '—' : selected.yearsInOperation}
              </strong>
            </span>
          </div>
          <h4>Historial de estado</h4>
          {selected.stateHistory.length === 0 ? (
            <p className="text-muted">Sin historial de estados.</p>
          ) : (
            <ul className="inventory-history">
              {selected.stateHistory.map((entry) => (
                <li key={entry.id}>
                  <strong>{STATUS_LABELS[entry.status]}</strong>
                  <span>{new Date(entry.startDate).toLocaleString()}</span>
                  <small>{entry.reason ?? 'Sin observación'}</small>
                </li>
              ))}
            </ul>
          )}
        </aside>
      ) : null}
    </section>
  );
}
