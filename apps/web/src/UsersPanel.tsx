import { useCallback, useEffect, useState, type FormEvent, type ReactElement } from 'react';
import { ApiClientError } from '@lu/api-client';
import {
  SiteRole,
  type CreateManagedUserInput,
  type ManagedUserPage,
  type ManagedUserRecord,
  type ManagedUserStatus,
  type ProfileRecord,
  type UpdateManagedUserInput,
  type UpdateProfileInput,
} from '@lu/contracts';

export type UsersApi = {
  users(query?: {
    readonly currentPage?: number;
    readonly statusFilter?: ManagedUserStatus;
  }): Promise<ManagedUserPage>;
  user(id: string): Promise<ManagedUserRecord>;
  createUser(input: CreateManagedUserInput): Promise<ManagedUserRecord>;
  updateUser(id: string, input: UpdateManagedUserInput): Promise<ManagedUserRecord>;
  deleteUser(id: string): Promise<void>;
  profile(): Promise<ProfileRecord>;
  updateProfile(input: UpdateProfileInput): Promise<ProfileRecord>;
};

const STATUS: Record<ManagedUserStatus, string> = { active: 'Activo', disabled: 'Deshabilitado' };
function blank(): CreateManagedUserInput {
  return { email: '', fullName: '', password: '', siteRole: SiteRole.Supervisor };
}

export function UsersPanel({ api }: { readonly api: UsersApi }): ReactElement {
  const [page, setPage] = useState<ManagedUserPage | null>(null);
  const [form, setForm] = useState<CreateManagedUserInput>(blank());
  const [editing, setEditing] = useState<ManagedUserRecord | null>(null);
  const [statusFilter, setStatusFilter] = useState<ManagedUserStatus | ''>('');
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [feedback, setFeedback] = useState<string | null>(null);
  const load = useCallback(async () => {
    setLoading(true);
    try {
      setPage(
        await api.users({
          currentPage: 1,
          statusFilter: statusFilter === '' ? undefined : statusFilter,
        }),
      );
      setError(null);
    } catch {
      setError('No fue posible cargar las cuentas de acceso.');
    } finally {
      setLoading(false);
    }
  }, [api, statusFilter]);
  useEffect(() => {
    void load();
  }, [load]);
  function edit(item: ManagedUserRecord): void {
    setEditing(item);
    setForm({ email: item.email, fullName: item.fullName, password: '', siteRole: item.siteRole });
  }
  async function submit(event: FormEvent<HTMLFormElement>): Promise<void> {
    event.preventDefault();
    setError(null);
    setFeedback(null);
    try {
      if (editing === null) await api.createUser(form);
      else
        await api.updateUser(editing.id, {
          email: form.email,
          fullName: form.fullName,
          password: form.password || null,
          siteRole: form.siteRole,
          status: editing.status,
        });
      setFeedback(
        editing === null ? 'Cuenta creada correctamente.' : 'Cuenta actualizada correctamente.',
      );
      setEditing(null);
      setForm(blank());
      await load();
    } catch (caught) {
      setError(
        caught instanceof ApiClientError
          ? caught.failure.error.message
          : 'No fue posible guardar la cuenta.',
      );
    }
  }
  async function disable(item: ManagedUserRecord): Promise<void> {
    if (!window.confirm(`¿Confirmas deshabilitar a ${item.fullName}?`)) return;
    try {
      await api.deleteUser(item.id);
      setFeedback('Cuenta deshabilitada correctamente.');
      await load();
    } catch (caught) {
      setError(
        caught instanceof ApiClientError
          ? caught.failure.error.message
          : 'No fue posible deshabilitar la cuenta.',
      );
    }
  }
  return (
    <section className="catalog-panel users-panel" aria-label="Usuarios y roles">
      <div className="catalog-heading">
        <div>
          <span className="welcome-kicker">Control plane</span>
          <h2>Usuarios y roles</h2>
          <p>Las cuentas y membresías se administran desde el control plane.</p>
        </div>
        <span className="catalog-count">{page?.totalCount ?? 0} cuentas</span>
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
          value={statusFilter}
          onChange={(event) => setStatusFilter(event.target.value as ManagedUserStatus | '')}
        >
          <option value="">Todas las cuentas</option>
          {Object.entries(STATUS).map(([key, value]) => (
            <option key={key} value={key}>
              {value}
            </option>
          ))}
        </select>
      </div>
      <div className="catalog-layout">
        <form className="catalog-form" onSubmit={(event) => void submit(event)}>
          <h3>{editing === null ? 'Nueva cuenta' : `Editar cuenta de ${editing.fullName}`}</h3>
          <label>
            Nombre completo
            <input
              className="form-control"
              required
              maxLength={200}
              value={form.fullName}
              onChange={(event) => setForm({ ...form, fullName: event.target.value })}
            />
          </label>
          <label>
            Correo
            <input
              className="form-control"
              type="email"
              required
              value={form.email}
              onChange={(event) => setForm({ ...form, email: event.target.value })}
            />
          </label>
          <label>
            Rol en la sede
            <select
              className="form-control"
              value={form.siteRole}
              onChange={(event) => setForm({ ...form, siteRole: event.target.value as SiteRole })}
            >
              <option value={SiteRole.Supervisor}>Supervisor</option>
              <option value={SiteRole.Administrador}>Administrador</option>
            </select>
          </label>
          <label>
            {editing === null ? 'Contraseña inicial' : 'Nueva contraseña (opcional)'}
            <input
              className="form-control"
              type="password"
              required={editing === null}
              minLength={8}
              maxLength={72}
              value={form.password}
              onChange={(event) => setForm({ ...form, password: event.target.value })}
            />
          </label>
          <div className="catalog-actions">
            <button type="submit" className="btn btn-primary">
              Guardar cuenta
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
            <p className="text-muted">Cargando cuentas…</p>
          ) : page?.items.length === 0 ? (
            <div className="empty-state">
              <i className="mdi mdi-account-key-outline" />
              <h3>No hay cuentas</h3>
              <p>Las cuentas de la sede activa aparecerán aquí.</p>
            </div>
          ) : (
            <table className="catalog-table">
              <thead>
                <tr>
                  <th>Cuenta</th>
                  <th>Rol</th>
                  <th>Estado</th>
                  <th>Acciones</th>
                </tr>
              </thead>
              <tbody>
                {page?.items.map((item) => (
                  <tr key={item.id}>
                    <td>
                      <strong>{item.fullName}</strong>
                      <small>{item.email}</small>
                    </td>
                    <td>{item.siteRole}</td>
                    <td>
                      <span className={`status-pill status-${item.status}`}>
                        {STATUS[item.status]}
                      </span>
                    </td>
                    <td className="catalog-actions">
                      <button type="button" onClick={() => edit(item)}>
                        Editar
                      </button>
                      {item.status === 'active' ? (
                        <button type="button" onClick={() => void disable(item)}>
                          Deshabilitar
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

export function ProfilePanel({ api }: { readonly api: UsersApi }): ReactElement {
  const [profile, setProfile] = useState<ProfileRecord | null>(null);
  const [form, setForm] = useState<UpdateProfileInput>({ email: '', fullName: '' });
  const [error, setError] = useState<string | null>(null);
  const [feedback, setFeedback] = useState<string | null>(null);
  useEffect(() => {
    void api
      .profile()
      .then((value) => {
        setProfile(value);
        setForm({ email: value.email, fullName: value.fullName });
      })
      .catch(() => setError('No fue posible cargar tu perfil.'));
  }, [api]);
  async function submit(event: FormEvent<HTMLFormElement>): Promise<void> {
    event.preventDefault();
    setError(null);
    setFeedback(null);
    try {
      const value = await api.updateProfile(form);
      setProfile(value);
      setForm({ email: value.email, fullName: value.fullName });
      setFeedback('Perfil actualizado correctamente.');
    } catch (caught) {
      setError(
        caught instanceof ApiClientError
          ? caught.failure.error.message
          : 'No fue posible actualizar el perfil.',
      );
    }
  }
  return (
    <section className="catalog-panel profile-panel" aria-label="Mi perfil">
      <div className="catalog-heading">
        <div>
          <span className="welcome-kicker">Cuenta personal</span>
          <h2>Mi perfil</h2>
          <p>Actualiza tu nombre y correo de contacto.</p>
        </div>
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
      <form className="catalog-form" onSubmit={(event) => void submit(event)}>
        <label>
          Nombre completo
          <input
            className="form-control"
            required
            value={form.fullName}
            onChange={(event) => setForm({ ...form, fullName: event.target.value })}
          />
        </label>
        <label>
          Correo
          <input
            className="form-control"
            type="email"
            required
            value={form.email}
            onChange={(event) => setForm({ ...form, email: event.target.value })}
          />
        </label>
        <label>
          Rol en la sede
          <input className="form-control" readOnly value={profile?.siteRole ?? 'Cargando…'} />
        </label>
        <button className="btn btn-primary" type="submit">
          Guardar perfil
        </button>
      </form>
    </section>
  );
}
