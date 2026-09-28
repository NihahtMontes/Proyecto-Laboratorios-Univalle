import {
  useCallback,
  useEffect,
  useId,
  useRef,
  useState,
  type FormEvent,
  type ReactElement,
  type ReactNode,
} from 'react';
import type {
  CreatePersonInput,
  PersonCategory,
  PersonPage,
  PersonRecord,
  PersonStatus,
  PersonType,
  UpdatePersonInput,
} from '@lu/contracts';
import { describeApiError } from './apiErrors';
import { FilterSelect, LegacyPagination, SmartIndexZone, StatusBadge } from './legacyUi';

export type PeopleApi = {
  people(query?: {
    readonly currentPage?: number;
    readonly type?: PersonType;
    readonly statusFilter?: PersonStatus;
    readonly category?: PersonCategory;
    readonly searchTerm?: string;
  }): Promise<PersonPage>;
  person(id: number): Promise<PersonRecord>;
  createPerson(input: CreatePersonInput): Promise<PersonRecord>;
  updatePerson(id: number, input: UpdatePersonInput): Promise<PersonRecord>;
  deletePerson(id: number): Promise<void>;
};

const TYPES: Record<PersonType, string> = { internal: 'Interno', external: 'Externo' };
/** Legacy `GeneralStatus` display names. */
const STATUS: Record<PersonStatus, string> = { 0: 'Activo', 1: 'Inactivo', 2: 'Eliminado' };
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

/** Filters owned by the Users Index when the directory is shown as its tab (legacy shared filters). */
export interface PeopleSharedFilters {
  readonly searchTerm?: string;
  readonly statusFilter?: PersonStatus;
}

function Field({
  label,
  required = false,
  children,
}: {
  readonly label: string;
  readonly required?: boolean;
  readonly children: (id: string) => ReactNode;
}): ReactElement {
  const id = useId();
  return (
    <div className="form-group mb-3">
      <label
        htmlFor={id}
        className={`control-label font-weight-bold small uppercase text-muted${required ? ' required' : ''}`}
      >
        {label}
      </label>
      {children(id)}
    </div>
  );
}

export function PeoplePanel({
  api,
  sharedFilters,
  filterZone,
}: {
  readonly api: PeopleApi;
  readonly sharedFilters?: PeopleSharedFilters;
  /** Users Index renders the shared Smart Index Zone; the directory adds its type filter. */
  readonly filterZone?: (extra: ReactNode, count: ReactNode) => ReactElement;
}): ReactElement {
  const [page, setPage] = useState<PersonPage | null>(null);
  const [form, setForm] = useState<CreatePersonInput>(blank());
  const [editing, setEditing] = useState<PersonRecord | null>(null);
  const [formOpen, setFormOpen] = useState(false);
  const [typeFilter, setTypeFilter] = useState<PersonType | ''>('');
  const [ownStatusFilter, setStatusFilter] = useState<PersonStatus | ''>('');
  const [searchInput, setSearchInput] = useState('');
  const [ownSearchTerm, setSearchTerm] = useState('');
  const [pageIndex, setPageIndex] = useState(1);
  const shared = sharedFilters !== undefined;
  const statusFilter = shared ? (sharedFilters.statusFilter ?? '') : ownStatusFilter;
  const searchTerm = shared ? (sharedFilters.searchTerm ?? '') : ownSearchTerm;
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [feedback, setFeedback] = useState<string | null>(null);

  // Only the newest request may commit: an older, slower response (e.g. the
  // unfiltered first load) must never overwrite a newer search result.
  const latestRequest = useRef(0);
  const load = useCallback(async () => {
    const request = ++latestRequest.current;
    setLoading(true);
    try {
      const next = await api.people({
        currentPage: pageIndex,
        type: typeFilter === '' ? undefined : typeFilter,
        statusFilter: statusFilter === '' ? undefined : statusFilter,
        searchTerm: searchTerm === '' ? undefined : searchTerm,
      });
      if (request !== latestRequest.current) return;
      setPage(next);
      setError(null);
    } catch (caught) {
      if (request !== latestRequest.current) return;
      setError(describeApiError(caught, 'No fue posible cargar el directorio de personas.'));
    } finally {
      if (request === latestRequest.current) setLoading(false);
    }
  }, [api, pageIndex, searchTerm, statusFilter, typeFilter]);
  useEffect(() => {
    setPageIndex(1);
  }, [searchTerm, statusFilter, typeFilter]);
  useEffect(() => {
    void load();
  }, [load]);

  function openCreate(): void {
    setEditing(null);
    setForm(blank());
    setFormOpen(true);
  }
  function closeForm(): void {
    setEditing(null);
    setForm(blank());
    setFormOpen(false);
  }
  function edit(item: PersonRecord): void {
    setEditing(item);
    setFormOpen(true);
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
      .catch((caught: unknown) =>
        setError(describeApiError(caught, 'No fue posible cargar el detalle de la persona.')),
      );
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
      closeForm();
      await load();
    } catch (caught) {
      setError(describeApiError(caught, 'No fue posible guardar la persona.'));
    }
  }
  async function remove(item: PersonRecord): Promise<void> {
    if (!window.confirm(`¿Confirmas la baja lógica de ${item.name}?`)) return;
    try {
      await api.deletePerson(item.id);
      setFeedback('Persona dada de baja correctamente.');
      await load();
    } catch (caught) {
      setError(describeApiError(caught, 'No fue posible dar de baja la persona.'));
    }
  }

  const typeSelect = (
    <FilterSelect
      label="Filtrar por tipo"
      value={typeFilter}
      onChange={(value) => setTypeFilter(value as PersonType | '')}
    >
      <option value="">Tipo</option>
      {Object.entries(TYPES).map(([key, value]) => (
        <option key={key} value={key}>
          {value}
        </option>
      ))}
    </FilterSelect>
  );
  const count = (
    <>
      <strong className="text-dark">{page?.totalCount ?? 0}</strong> persona(s)
    </>
  );
  const items = page?.items ?? [];

  const body = (
    <>
      <div className="d-md-flex align-items-center mb-4 pb-3 border-bottom">
        <div>
          <h3 className="card-title text-dark font-weight-bold mt-2 ml-2">
            Directorio Base de Identidades
          </h3>
          <h6 className="card-subtitle text-muted ml-2">
            Personal registrado en la institución para servicios técnicos y administrativos.
          </h6>
        </div>
        <div className="ml-auto mt-3 mt-md-0">
          <button
            type="button"
            className="btn bg-white border btn-rounded shadow-sm text-dark px-4 font-weight-bold"
            aria-expanded={formOpen && editing === null}
            onClick={openCreate}
          >
            <i className="fas fa-plus-circle mr-1 text-info" aria-hidden="true" /> Nueva Identidad
          </button>
        </div>
      </div>

      {filterZone ? (
        filterZone(typeSelect, count)
      ) : (
        <SmartIndexZone
          searchLabel="Buscar persona"
          placeholder="Buscar por nombre, correo o código"
          value={searchInput}
          active={searchTerm !== ''}
          onChange={setSearchInput}
          onSubmit={() => setSearchTerm(searchInput.trim())}
          count={count}
        >
          {typeSelect}
          <FilterSelect
            label="Filtrar personas por estado"
            value={String(statusFilter)}
            onChange={(value) =>
              setStatusFilter(value === '' ? '' : (Number(value) as PersonStatus))
            }
          >
            <option value="">Estado</option>
            {Object.entries(STATUS).map(([key, value]) => (
              <option key={key} value={key}>
                {value}
              </option>
            ))}
          </FilterSelect>
        </SmartIndexZone>
      )}

      {feedback ? (
        <div className="alert alert-success shadow-sm border-0 small" role="status">
          <i className="fas fa-check-circle mr-1" aria-hidden="true" /> {feedback}
        </div>
      ) : null}
      {error ? (
        <div className="alert alert-danger shadow-sm border-0 small" role="alert">
          {error}
        </div>
      ) : null}

      {formOpen ? (
        <form
          className="bg-light p-3 mb-4 rounded border-left border-info"
          aria-label={editing === null ? 'Nueva identidad' : 'Editar identidad'}
          onSubmit={(event) => void submit(event)}
        >
          <h5 className="font-weight-bold text-info mb-3">
            <i className="fas fa-id-card mr-2" aria-hidden="true" />{' '}
            {editing === null ? 'Nueva Identidad' : `Editar Identidad #${editing.id}`}
          </h5>
          <div className="row">
            <div className="col-md-4">
              <Field label="Tipo" required>
                {(id) => (
                  <select
                    id={id}
                    className="form-control custom-select"
                    value={form.type}
                    onChange={(event) =>
                      setForm({ ...form, type: event.target.value as PersonType })
                    }
                  >
                    <option value="internal">Interno</option>
                    <option value="external">Externo</option>
                  </select>
                )}
              </Field>
            </div>
            <div className="col-md-8">
              <Field label="Nombre / razón social" required>
                {(id) => (
                  <input
                    id={id}
                    className="form-control"
                    required
                    maxLength={200}
                    value={form.name}
                    onChange={(event) => setForm({ ...form, name: event.target.value })}
                  />
                )}
              </Field>
            </div>
            <div className="col-md-4">
              <Field label="Código de actor">
                {(id) => (
                  <input
                    id={id}
                    className="form-control"
                    maxLength={30}
                    value={form.actorCode ?? ''}
                    onChange={(event) =>
                      setForm({ ...form, actorCode: event.target.value || null })
                    }
                  />
                )}
              </Field>
            </div>
            <div className="col-md-4">
              <Field label="Categoría">
                {(id) => (
                  <select
                    id={id}
                    className="form-control custom-select"
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
                )}
              </Field>
            </div>
            {editing !== null ? (
              <div className="col-md-4">
                <Field label="Estado">
                  {(id) => (
                    <select
                      id={id}
                      className="form-control custom-select"
                      value={editing.status}
                      onChange={(event) =>
                        setEditing({
                          ...editing,
                          status: Number(event.target.value) as PersonStatus,
                        })
                      }
                    >
                      {Object.entries(STATUS).map(([key, value]) => (
                        <option key={key} value={key}>
                          {value}
                        </option>
                      ))}
                    </select>
                  )}
                </Field>
              </div>
            ) : null}
            <div className="col-md-6">
              <Field label="Correo">
                {(id) => (
                  <input
                    id={id}
                    className="form-control"
                    type="email"
                    value={form.email ?? ''}
                    onChange={(event) => setForm({ ...form, email: event.target.value || null })}
                  />
                )}
              </Field>
            </div>
            <div className="col-md-6">
              <Field label="Teléfono">
                {(id) => (
                  <input
                    id={id}
                    className="form-control"
                    value={form.phoneNumber ?? ''}
                    onChange={(event) =>
                      setForm({ ...form, phoneNumber: event.target.value || null })
                    }
                  />
                )}
              </Field>
            </div>
            {form.type === 'external' ? (
              <>
                <div className="col-12 mb-3">
                  <div className="custom-control custom-checkbox">
                    <input
                      id="person-is-entity"
                      type="checkbox"
                      className="custom-control-input"
                      checked={form.isEntity === true}
                      onChange={(event) => setForm({ ...form, isEntity: event.target.checked })}
                    />
                    <label className="custom-control-label" htmlFor="person-is-entity">
                      Es una entidad
                    </label>
                  </div>
                </div>
                <div className="col-12">
                  <Field label="Dirección" required>
                    {(id) => (
                      <textarea
                        id={id}
                        className="form-control"
                        required
                        maxLength={500}
                        value={form.address ?? ''}
                        onChange={(event) =>
                          setForm({ ...form, address: event.target.value || null })
                        }
                      />
                    )}
                  </Field>
                </div>
              </>
            ) : null}
          </div>
          <div className="text-right">
            <button
              type="submit"
              className="btn btn-info btn-rounded px-4 shadow-sm font-weight-bold py-2"
            >
              <i className="fas fa-save mr-1" aria-hidden="true" /> Guardar
            </button>
            <button
              type="button"
              className="btn btn-outline-secondary btn-rounded ml-2 py-2 px-4 font-weight-bold"
              onClick={closeForm}
            >
              Cancelar
            </button>
          </div>
        </form>
      ) : null}

      {loading ? (
        <p className="text-muted text-center py-5 mb-0">Cargando personas…</p>
      ) : items.length === 0 ? (
        <div className="text-center py-5 rounded border shadow-none border-dashed" role="status">
          <i className="fas fa-id-badge fa-4x text-muted opacity-2 mb-3" aria-hidden="true" />
          <h5 className="text-muted font-weight-bold">
            No se han registrado identidades en la base de datos base.
          </h5>
          <button type="button" className="btn btn-link font-weight-bold" onClick={openCreate}>
            Registrar la primera persona
          </button>
        </div>
      ) : (
        <>
          <div className="alert alert-light border-0 small rounded p-3 mb-4 text-info italic shadow-none border-left border-info border-3">
            <i className="fas fa-info-circle mr-2" aria-hidden="true" /> Las personas listadas en
            este directorio pueden ser promovidas a <strong>Usuarios del Sistema</strong> para
            gestionar inventarios y servicios.
          </div>
          <div className="table-responsive">
            <table className="table table-hover v-middle">
              <thead className="bg-light">
                <tr className="small uppercase font-weight-bold">
                  <th>Nombre Completo</th>
                  <th>Tipo</th>
                  <th>Contacto</th>
                  <th className="text-center">Estado</th>
                  <th className="text-center">Acciones</th>
                </tr>
              </thead>
              <tbody>
                {items.map((item) => (
                  <tr key={item.id}>
                    <td>
                      <div className="d-flex align-items-center">
                        <div className="m-r-10">
                          <span
                            className="btn btn-circle btn-info font-weight-bold text-white shadow-sm"
                            aria-hidden="true"
                          >
                            {item.name.charAt(0).toUpperCase() || 'P'}
                          </span>
                        </div>
                        <div>
                          <h6 className="m-b-0 font-weight-bold text-dark">{item.name}</h6>
                          <span className="text-muted small">
                            ID: {item.id}
                            {item.actorCode ? ` · ${item.actorCode}` : ''} ·{' '}
                            {CATEGORY[item.category]}
                          </span>
                        </div>
                      </div>
                    </td>
                    <td>
                      <span
                        className={`badge badge-pill ${item.type === 'internal' ? 'badge-info' : 'badge-warning'} px-2 py-1 font-weight-bold small text-white`}
                      >
                        {TYPES[item.type]}
                      </span>
                      {item.isEntity ? <div className="text-muted small mt-1">Entidad</div> : null}
                    </td>
                    <td>
                      <div className="text-dark font-weight-bold small">
                        {item.phoneNumber ?? '-'}
                      </div>
                      <div className="text-muted small">{item.email ?? '-'}</div>
                    </td>
                    <td className="text-center">
                      <StatusBadge
                        label={STATUS[item.status]}
                        tone={item.status === 0 ? 'success' : 'danger'}
                      />
                    </td>
                    <td className="text-center">
                      <div className="btn-group">
                        <button
                          type="button"
                          className="btn btn-sm btn-outline-warning btn-rounded shadow-none"
                          title="Editar"
                          aria-label="Editar"
                          onClick={() => edit(item)}
                        >
                          <i className="fas fa-edit" aria-hidden="true" />
                        </button>
                        {item.status !== 2 ? (
                          <button
                            type="button"
                            className="btn btn-sm btn-outline-danger btn-rounded ml-1 shadow-none"
                            title="Eliminar"
                            aria-label="Eliminar"
                            onClick={() => void remove(item)}
                          >
                            <i className="fas fa-trash" aria-hidden="true" />
                          </button>
                        ) : null}
                      </div>
                    </td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>
        </>
      )}

      {page ? (
        <div className="mt-3">
          <LegacyPagination
            pageIndex={page.pageIndex}
            totalPages={page.totalPages}
            label="Paginación Personas"
            onPage={setPageIndex}
          />
        </div>
      ) : null}
    </>
  );

  if (shared) {
    return <section aria-label="Directorio de personas">{body}</section>;
  }
  return (
    <section className="row users-module" aria-label="Directorio de personas">
      <div className="col-12">
        <div className="card shadow-sm border-0">
          <div className="card-body">{body}</div>
        </div>
      </div>
    </section>
  );
}
