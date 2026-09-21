import { useCallback, useEffect, useState, type FormEvent, type ReactElement } from 'react';
import { ApiClientError } from '@lu/api-client';
import type {
  CreatePersonInput,
  PersonCategory,
  PersonPage,
  PersonRecord,
  PersonStatus,
  PersonType,
  UpdatePersonInput,
} from '@lu/contracts';

export type PeopleApi = {
  people(query?: {
    readonly currentPage?: number;
    readonly type?: PersonType;
    readonly statusFilter?: PersonStatus;
    readonly category?: PersonCategory;
  }): Promise<PersonPage>;
  person(id: number): Promise<PersonRecord>;
  createPerson(input: CreatePersonInput): Promise<PersonRecord>;
  updatePerson(id: number, input: UpdatePersonInput): Promise<PersonRecord>;
  deletePerson(id: number): Promise<void>;
};

const TYPES: Record<PersonType, string> = { internal: 'Interno', external: 'Externo' };
const STATUS: Record<PersonStatus, string> = { 0: 'Activo', 1: 'Inactivo', 2: 'Baja' };
const CATEGORY: Record<PersonCategory, string> = {
  1: 'Técnico',
  2: 'Docente',
  3: 'Administrativo',
  4: 'Estudiante',
  5: 'Proveedor',
  99: 'Otro',
};

function blank(): CreatePersonInput {
  return {
    type: 'internal',
    name: '',
    email: null,
    phoneNumber: null,
    isEntity: false,
    address: null,
    category: 1,
    actorCode: null,
  };
}

export function PeoplePanel({ api }: { readonly api: PeopleApi }): ReactElement {
  const [page, setPage] = useState<PersonPage | null>(null);
  const [form, setForm] = useState<CreatePersonInput>(blank());
  const [editing, setEditing] = useState<PersonRecord | null>(null);
  const [typeFilter, setTypeFilter] = useState<PersonType | ''>('');
  const [statusFilter, setStatusFilter] = useState<PersonStatus | ''>('');
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [feedback, setFeedback] = useState<string | null>(null);

  const load = useCallback(async () => {
    setLoading(true);
    try {
      setPage(
        await api.people({
          currentPage: 1,
          type: typeFilter === '' ? undefined : typeFilter,
          statusFilter: statusFilter === '' ? undefined : statusFilter,
        }),
      );
      setError(null);
    } catch {
      setError('No fue posible cargar el directorio de personas.');
    } finally {
      setLoading(false);
    }
  }, [api, statusFilter, typeFilter]);
  useEffect(() => {
    void load();
  }, [load]);

  function edit(item: PersonRecord): void {
    setEditing(item);
    setForm({
      type: item.type,
      name: item.name,
      email: item.email,
      phoneNumber: item.phoneNumber,
      isEntity: item.isEntity,
      address: item.address,
      category: item.category,
      actorCode: item.actorCode,
    });
    void api
      .person(item.id)
      .catch(() => setError('No fue posible cargar el detalle de la persona.'));
  }
  async function submit(event: FormEvent<HTMLFormElement>): Promise<void> {
    event.preventDefault();
    setError(null);
    setFeedback(null);
    if (form.type === 'external' && !(form.address ?? '').trim()) {
      setError('La dirección es obligatoria para externos.');
      return;
    }
    try {
      if (editing === null) await api.createPerson(form);
      else await api.updatePerson(editing.id, { ...form, status: editing.status });
      setFeedback(
        editing === null
          ? 'Persona registrada correctamente.'
          : 'Persona actualizada correctamente.',
      );
      setEditing(null);
      setForm(blank());
      await load();
    } catch (caught) {
      setError(
        caught instanceof ApiClientError
          ? caught.failure.error.message
          : 'No fue posible guardar la persona.',
      );
    }
  }
  async function remove(item: PersonRecord): Promise<void> {
    if (!window.confirm(`¿Confirmas la baja lógica de ${item.name}?`)) return;
    try {
      await api.deletePerson(item.id);
      setFeedback('Persona dada de baja correctamente.');
      await load();
    } catch (caught) {
      setError(
        caught instanceof ApiClientError
          ? caught.failure.error.message
          : 'No fue posible dar de baja la persona.',
      );
    }
  }

  return (
    <section className="catalog-panel people-panel" aria-label="Directorio de personas">
      <div className="catalog-heading">
        <div>
          <span className="welcome-kicker">Administración operacional</span>
          <h2>Personas</h2>
          <p>Técnicos, responsables y proveedores de la sede activa.</p>
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
          value={typeFilter}
          onChange={(event) => setTypeFilter(event.target.value as PersonType | '')}
        >
          <option value="">Todos los tipos</option>
          {Object.entries(TYPES).map(([key, value]) => (
            <option key={key} value={key}>
              {value}
            </option>
          ))}
        </select>
        <select
          className="form-control"
          value={statusFilter}
          onChange={(event) =>
            setStatusFilter(
              event.target.value === '' ? '' : (Number(event.target.value) as PersonStatus),
            )
          }
        >
          <option value="">Activos e inactivos</option>
          {Object.entries(STATUS).map(([key, value]) => (
            <option key={key} value={key}>
              {value}
            </option>
          ))}
        </select>
      </div>
      <div className="catalog-layout">
        <form className="catalog-form" onSubmit={(event) => void submit(event)}>
          <h3>{editing === null ? 'Nueva persona' : `Editar persona #${editing.id}`}</h3>
          <label>
            Tipo
            <select
              className="form-control"
              value={form.type}
              onChange={(event) => setForm({ ...form, type: event.target.value as PersonType })}
            >
              <option value="internal">Interno</option>
              <option value="external">Externo</option>
            </select>
          </label>
          <label>
            Nombre / razón social
            <input
              className="form-control"
              required
              maxLength={200}
              value={form.name}
              onChange={(event) => setForm({ ...form, name: event.target.value })}
            />
          </label>
          <label>
            Código de actor
            <input
              className="form-control"
              maxLength={30}
              value={form.actorCode ?? ''}
              onChange={(event) => setForm({ ...form, actorCode: event.target.value || null })}
            />
          </label>
          <label>
            Categoría
            <select
              className="form-control"
              value={form.category ?? 99}
              onChange={(event) =>
                setForm({ ...form, category: Number(event.target.value) as PersonCategory })
              }
            >
              {Object.entries(CATEGORY).map(([key, value]) => (
                <option key={key} value={key}>
                  {value}
                </option>
              ))}
            </select>
          </label>
          <label>
            Correo
            <input
              className="form-control"
              type="email"
              value={form.email ?? ''}
              onChange={(event) => setForm({ ...form, email: event.target.value || null })}
            />
          </label>
          <label>
            Teléfono
            <input
              className="form-control"
              value={form.phoneNumber ?? ''}
              onChange={(event) => setForm({ ...form, phoneNumber: event.target.value || null })}
            />
          </label>
          {form.type === 'external' ? (
            <>
              <label>
                <input
                  type="checkbox"
                  checked={form.isEntity === true}
                  onChange={(event) => setForm({ ...form, isEntity: event.target.checked })}
                />{' '}
                Es una entidad
              </label>
              <label>
                Dirección
                <textarea
                  className="form-control"
                  required
                  maxLength={500}
                  value={form.address ?? ''}
                  onChange={(event) => setForm({ ...form, address: event.target.value || null })}
                />
              </label>
            </>
          ) : null}
          {editing !== null ? (
            <label>
              Estado
              <select
                className="form-control"
                value={editing.status}
                onChange={(event) =>
                  setEditing({ ...editing, status: Number(event.target.value) as PersonStatus })
                }
              >
                {Object.entries(STATUS).map(([key, value]) => (
                  <option key={key} value={key}>
                    {value}
                  </option>
                ))}
              </select>
            </label>
          ) : null}
          <div className="catalog-actions">
            <button type="submit" className="btn btn-primary">
              Guardar
            </button>
            {editing !== null ? (
              <button
                type="button"
                className="btn btn-light"
                onClick={() => {
                  setEditing(null);
                  setForm(blank());
                }}
              >
                Cancelar
              </button>
            ) : null}
          </div>
        </form>
        <div className="catalog-table-wrap">
          {loading ? (
            <p className="text-muted">Cargando personas…</p>
          ) : page?.items.length === 0 ? (
            <div className="empty-state">
              <i className="mdi mdi-account-group-outline" />
              <h3>No hay personas</h3>
              <p>El directorio de la sede activa aparecerá aquí.</p>
            </div>
          ) : (
            <table className="catalog-table">
              <thead>
                <tr>
                  <th>Persona</th>
                  <th>Tipo</th>
                  <th>Contacto</th>
                  <th>Estado</th>
                  <th>Acciones</th>
                </tr>
              </thead>
              <tbody>
                {page?.items.map((item) => (
                  <tr key={item.id}>
                    <td>
                      <strong>{item.name}</strong>
                      <small>{item.actorCode ?? `#${item.id}`}</small>
                      <small>{CATEGORY[item.category]}</small>
                    </td>
                    <td>
                      {TYPES[item.type]}
                      {item.isEntity ? <small>Entidad</small> : null}
                    </td>
                    <td>
                      {item.email ?? 'Sin correo'}
                      <small>{item.phoneNumber ?? 'Sin teléfono'}</small>
                    </td>
                    <td>
                      <span className={`status-pill status-${item.status}`}>
                        {STATUS[item.status]}
                      </span>
                    </td>
                    <td className="catalog-actions">
                      <button type="button" onClick={() => edit(item)}>
                        Editar
                      </button>
                      {item.status !== 2 ? (
                        <button type="button" onClick={() => void remove(item)}>
                          Baja
                        </button>
                      ) : null}
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
