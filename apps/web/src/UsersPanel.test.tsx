import { beforeEach, describe, expect, it, vi } from 'vitest';
import { render, screen, waitFor, within } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { ApiClientError } from '@lu/api-client';
import {
  SiteRole,
  SiteState,
  type ActiveSiteSession,
  type AuthSessionResponse,
  type ManagedUserPage,
  type ManagedUserRecord,
  type MembershipProjection,
  type PersonPage,
  type ProfileRecord,
  type SiteId,
} from '@lu/contracts';
import { useState } from 'react';
import {
  PasswordChangeForm,
  ProfilePanel,
  UsersModule,
  type ProfileApi,
  type UsersApi,
  type UsersRoute,
  type UsersViewer,
} from './UsersPanel';
import type { PeopleApi } from './PeoplePanel';
import { PeoplePanel } from './PeoplePanel';
import { photoFileProblem } from './photo';
import { globalFieldChanges, resolveUsersRoute } from './usersModel';

const SITE_ID = '22222222-2222-4222-8222-222222222222' as SiteId;
const OTHER_SITE = 'aaaaaaaa-aaaa-4aaa-8aaa-aaaaaaaaaaaa' as SiteId;
const ADMIN_ID = '11111111-1111-4111-8111-111111111111';
const TARGET_ID = '44444444-4444-4444-8444-444444444444';

const SITE_ADMIN: UsersViewer = {
  userId: ADMIN_ID,
  isSuperAdmin: false,
  activeSiteId: SITE_ID,
  activeSiteName: 'Sede Central',
  siteRole: SiteRole.Administrador,
  eligibleSites: [{ siteId: SITE_ID, siteName: 'Sede Central', role: 'Administrador' }],
};
const SUPER_ADMIN: UsersViewer = {
  ...SITE_ADMIN,
  isSuperAdmin: true,
  eligibleSites: [
    { siteId: SITE_ID, siteName: 'Sede Central', role: 'Administrador' },
    { siteId: OTHER_SITE, siteName: 'Sede Norte', role: 'Administrador' },
  ],
};

function membership(overrides: Partial<MembershipProjection> = {}): MembershipProjection {
  return {
    siteId: SITE_ID,
    siteName: 'Sede Central',
    role: 'Supervisor',
    status: 'active',
    effectiveStatus: 'Activo',
    validFrom: null,
    validUntil: null,
    position: 'Técnico',
    department: 'Gastronomía',
    hireDate: '2024-02-01',
    ...overrides,
  };
}

function record(overrides: Partial<ManagedUserRecord> = {}): ManagedUserRecord {
  return {
    id: TARGET_ID,
    username: 'jperez',
    email: 'jperez@univalle.edu',
    firstName: 'Juan',
    lastName: 'Pérez',
    secondLastName: null,
    fullName: 'Juan Pérez',
    initials: 'JP',
    identityCard: '1234567',
    phoneNumber: '70000001',
    accountStatus: 'active',
    isSuperAdmin: false,
    displayRole: 'Supervisor',
    profilePicture: null,
    mustChangePassword: false,
    memberships: [membership()],
    createdAt: '2025-01-10T12:00:00.000Z',
    updatedAt: '2025-02-10T12:00:00.000Z',
    createdBy: null,
    modifiedBy: { userId: ADMIN_ID, fullName: 'Administradora Univalle' },
    ...overrides,
  };
}

function page(items: readonly ManagedUserRecord[], totalPages = 1): ManagedUserPage {
  return { items, totalCount: items.length, pageIndex: 1, totalPages, pageSize: 20 };
}

function makeUsersApi(overrides: Partial<UsersApi> = {}): UsersApi {
  return {
    listUsers: vi.fn().mockResolvedValue(page([record()])),
    userDetails: vi.fn().mockResolvedValue(record()),
    createManagedUser: vi.fn().mockResolvedValue(record()),
    updateUserGlobalFields: vi.fn().mockResolvedValue(record()),
    adminResetPassword: vi.fn().mockResolvedValue(undefined),
    setAccountStatus: vi.fn().mockResolvedValue(undefined),
    restoreAccount: vi.fn().mockResolvedValue(undefined),
    deleteAccount: vi.fn().mockResolvedValue(undefined),
    addMembership: vi.fn().mockResolvedValue(undefined),
    changeMembershipRole: vi.fn().mockResolvedValue(undefined),
    changeMembershipStatus: vi.fn().mockResolvedValue(undefined),
    changeMembershipValidity: vi.fn().mockResolvedValue(undefined),
    updateMembershipWorkProfile: vi.fn().mockResolvedValue(undefined),
    revokeMembership: vi.fn().mockResolvedValue(undefined),
    restoreMembership: vi.fn().mockResolvedValue(undefined),
    userPhoto: vi.fn().mockResolvedValue(new Blob()),
    uploadUserPhoto: vi.fn().mockResolvedValue({ etag: 'v2' }),
    deleteUserPhoto: vi.fn().mockResolvedValue(undefined),
    ...overrides,
  };
}

const EMPTY_PEOPLE: PersonPage = {
  items: [],
  totalCount: 0,
  pageIndex: 1,
  totalPages: 0,
  pageSize: 20,
};

function makePeopleApi(): PeopleApi {
  return {
    people: vi.fn().mockResolvedValue(EMPTY_PEOPLE),
    person: vi.fn(),
    createPerson: vi.fn(),
    updatePerson: vi.fn(),
    deletePerson: vi.fn(),
  };
}

function apiError(status: number, code: string, message = 'rejected'): ApiClientError {
  return new ApiClientError(status, { success: false, error: { code, message } });
}

/** Mounts the module with a real in-memory router so navigation can be asserted. */
function Harness({
  api,
  viewer,
  start,
  peopleApi = null,
}: {
  readonly api: UsersApi;
  readonly viewer: UsersViewer;
  readonly start: string;
  readonly peopleApi?: PeopleApi | null;
}) {
  const [path, setPath] = useState(start);
  const resolved = resolveUsersRoute(path);
  const route: UsersRoute | null = resolved && 'route' in resolved ? resolved.route : null;
  return (
    <>
      <output data-testid="path">{path}</output>
      {route ? (
        <UsersModule
          api={api}
          peopleApi={peopleApi}
          viewer={viewer}
          route={route}
          navigate={setPath}
        />
      ) : null}
    </>
  );
}

describe('Usuarios — paridad funcional F5', () => {
  beforeEach(() => {
    vi.spyOn(window, 'confirm').mockReturnValue(true);
  });

  it('resuelve las rutas canónicas y las redirecciones de transición (F1 §16)', () => {
    expect(resolveUsersRoute('/Users/Details')).toEqual({ redirect: '/Profile' });
    expect(resolveUsersRoute(`/Users/Details?id=${TARGET_ID}`)).toEqual({
      redirect: `/Users/Details/${TARGET_ID}`,
    });
    expect(resolveUsersRoute('/Users/Index?tab=personas')).toEqual({
      route: { kind: 'index', tab: 'personas' },
    });
    expect(resolveUsersRoute(`/Users/Edit/${TARGET_ID}?returnUrl=Details`)).toEqual({
      route: { kind: 'edit', userId: TARGET_ID, returnTo: 'Details' },
    });
    expect(resolveUsersRoute('/Equipment/Index')).toBeNull();
  });

  it('lista cuentas con búsqueda, filtro de estado (incluye Eliminado) y paginación del servidor', async () => {
    const api = makeUsersApi({ listUsers: vi.fn().mockResolvedValue(page([record()], 3)) });
    const user = userEvent.setup();
    render(<Harness api={api} viewer={SITE_ADMIN} start="/Users/Index" />);

    const row = (await screen.findByText('Juan Pérez')).closest('tr')!;
    expect(within(row).getByText('@jperez')).toBeInTheDocument();
    expect(within(row).getByText('Activo')).toBeInTheDocument();
    expect(within(row).getByText('Técnico')).toBeInTheDocument();
    expect(api.listUsers).toHaveBeenLastCalledWith({
      currentPage: 1,
      statusFilter: undefined,
      searchTerm: undefined,
    });

    await user.type(screen.getByLabelText('Buscar usuario'), 'perez');
    await user.click(screen.getByRole('button', { name: 'Buscar' }));
    await user.selectOptions(screen.getByLabelText('Filtrar por estado'), 'deleted');
    await waitFor(() =>
      expect(api.listUsers).toHaveBeenLastCalledWith({
        currentPage: 1,
        statusFilter: 'deleted',
        searchTerm: 'perez',
      }),
    );

    await user.click(await screen.findByRole('button', { name: 'Siguiente' }));
    await waitFor(() =>
      expect(api.listUsers).toHaveBeenLastCalledWith(expect.objectContaining({ currentPage: 2 })),
    );
    await user.click(screen.getByRole('button', { name: 'Limpiar filtros' }));
    await waitFor(() =>
      expect(api.listUsers).toHaveBeenLastCalledWith({
        currentPage: 1,
        statusFilter: undefined,
        searchTerm: undefined,
      }),
    );
  });

  it('oculta Editar/Eliminar sobre un SuperAdmin y Eliminar sobre la propia cuenta', async () => {
    const api = makeUsersApi({
      listUsers: vi.fn().mockResolvedValue(
        page([
          record({
            id: 'sa',
            fullName: 'Super Admin',
            isSuperAdmin: true,
            displayRole: 'SuperAdmin',
          }),
          record({ id: ADMIN_ID, fullName: 'Yo Misma', username: 'yo' }),
        ]),
      ),
    });
    render(<Harness api={api} viewer={SITE_ADMIN} start="/Users/Index" />);

    const saRow = (await screen.findByText('Super Admin')).closest('tr')!;
    expect(within(saRow).queryByRole('button', { name: 'Editar' })).toBeNull();
    expect(within(saRow).queryByRole('button', { name: 'Eliminar' })).toBeNull();
    expect(within(saRow).getByRole('button', { name: 'Detalles' })).toBeInTheDocument();
    const selfRow = screen.getByText('Yo Misma').closest('tr')!;
    expect(within(selfRow).getByRole('button', { name: 'Editar' })).toBeInTheDocument();
    expect(within(selfRow).queryByRole('button', { name: 'Eliminar' })).toBeNull();
  });

  it('muestra el Directorio de Personal como tab, con filtros compartidos y requisito de Administrador', async () => {
    const peopleApi = makePeopleApi();
    const user = userEvent.setup();
    const { unmount } = render(
      <Harness
        api={makeUsersApi()}
        viewer={SITE_ADMIN}
        start="/Users/Index"
        peopleApi={peopleApi}
      />,
    );
    await screen.findByText('Juan Pérez');
    await user.selectOptions(screen.getByLabelText('Filtrar por estado'), 'deleted');
    await user.click(screen.getByRole('tab', { name: 'Directorio de Personal' }));
    expect(screen.getByTestId('path')).toHaveTextContent('/Users/Index?tab=personas');
    // Deleted accounts filter maps to People status 2 (the fixed deleted filter).
    await waitFor(() =>
      expect(peopleApi.people).toHaveBeenLastCalledWith(
        expect.objectContaining({ statusFilter: 2 }),
      ),
    );
    unmount();

    render(
      <Harness
        api={makeUsersApi()}
        viewer={{ ...SUPER_ADMIN, activeSiteId: null, siteRole: null }}
        start="/Users/Index?tab=personas"
        peopleApi={peopleApi}
      />,
    );
    expect(await screen.findByText(/requiere una sede activa/)).toBeInTheDocument();
  });

  it('crea como Administrador de sede con role (sin selector SuperAdmin) y aplica perfil laboral por comando separado', async () => {
    const api = makeUsersApi();
    const user = userEvent.setup();
    render(<Harness api={api} viewer={SITE_ADMIN} start="/Users/Create" />);

    await user.type(await screen.findByLabelText('Nombres'), 'Juan');
    await user.type(screen.getByLabelText('Primer Apellido'), 'Pérez');
    await user.type(screen.getByLabelText('Cédula de Identidad'), '1234567');
    await user.type(screen.getByLabelText('Correo Institucional'), 'jperez@univalle.edu');
    await user.type(screen.getByLabelText('Teléfono de Contacto'), '70000001');
    await user.type(screen.getByLabelText('Nombre de Usuario (Login)'), 'jperez');
    await user.type(screen.getByLabelText('Contraseña de Acceso'), 'Clave-Segura-2026');
    await user.type(screen.getByLabelText('Cargo'), 'Técnico');
    const roleSelect = screen.getByLabelText('Rol de Aplicación');
    expect(within(roleSelect).queryByRole('option', { name: 'SuperAdmin' })).toBeNull();
    await user.selectOptions(roleSelect, 'Administrador');
    await user.click(screen.getByRole('button', { name: 'Crear Cuenta' }));

    await waitFor(() => expect(api.createManagedUser).toHaveBeenCalledOnce());
    const input = vi.mocked(api.createManagedUser).mock.calls[0]![0];
    expect(input).toEqual({
      username: 'jperez',
      email: 'jperez@univalle.edu',
      firstName: 'Juan',
      lastName: 'Pérez',
      secondLastName: null,
      identityCard: '1234567',
      phoneNumber: '70000001',
      password: 'Clave-Segura-2026',
      role: 'Administrador',
    });
    expect(api.updateMembershipWorkProfile).toHaveBeenCalledWith(TARGET_ID, SITE_ID, {
      position: 'Técnico',
      department: null,
      hireDate: null,
    });
    expect(screen.getByTestId('path')).toHaveTextContent('/Users/Index');
    expect(await screen.findByText(/creada exitosamente/)).toBeInTheDocument();
  });

  it('crea como SuperAdmin con memberships explícitas y muestra el 409 del servidor', async () => {
    const api = makeUsersApi({
      createManagedUser: vi
        .fn()
        .mockRejectedValue(apiError(409, 'CONFLICT', 'Username already exists.')),
    });
    const user = userEvent.setup();
    render(<Harness api={api} viewer={SUPER_ADMIN} start="/Users/Create" />);

    await user.type(await screen.findByLabelText('Nombres'), 'Ana');
    await user.type(screen.getByLabelText('Primer Apellido'), 'Rojas');
    await user.type(screen.getByLabelText('Cédula de Identidad'), '7654321');
    await user.type(screen.getByLabelText('Correo Institucional'), 'arojas@univalle.edu');
    await user.type(screen.getByLabelText('Teléfono de Contacto'), '70000002');
    await user.type(screen.getByLabelText('Nombre de Usuario (Login)'), 'arojas');
    await user.type(screen.getByLabelText('Contraseña de Acceso'), 'Clave-Segura-2026');
    await user.selectOptions(screen.getByLabelText('Sede de la membresía'), OTHER_SITE);
    await user.click(screen.getByRole('button', { name: 'Crear Cuenta' }));

    expect(await screen.findByRole('alert')).toHaveTextContent(
      'Conflicto: Username already exists.',
    );
    const input = vi.mocked(api.createManagedUser).mock.calls[0]![0];
    expect(input).toMatchObject({ memberships: [{ siteId: OTHER_SITE, role: 'Supervisor' }] });
    expect(input).not.toHaveProperty('role');
    expect(input).not.toHaveProperty('isSuperAdmin');
  });

  it('edita con comandos separados, sin PUT /users/:id, y reporta fallos parciales con verdad', async () => {
    const api = makeUsersApi({
      changeMembershipRole: vi.fn().mockRejectedValue(apiError(403, 'ACCESS_DENIED', 'Forbidden.')),
    });
    const user = userEvent.setup();
    render(<Harness api={api} viewer={SITE_ADMIN} start={`/Users/Edit/${TARGET_ID}`} />);

    const phone = await screen.findByLabelText('Teléfono de Contacto');
    await waitFor(() => expect(phone).toHaveValue('70000001'));
    expect(screen.getByDisplayValue('jperez')).toHaveAttribute('readonly');
    await user.clear(phone);
    await user.type(phone, '79999999');
    await user.selectOptions(screen.getByLabelText('Rol de Aplicación'), 'Administrador');
    await user.click(screen.getByRole('button', { name: 'Guardar Cambios' }));

    await waitFor(() =>
      expect(api.updateUserGlobalFields).toHaveBeenCalledWith(TARGET_ID, {
        phoneNumber: '79999999',
      }),
    );
    expect(api.changeMembershipRole).toHaveBeenCalledWith(TARGET_ID, SITE_ID, {
      role: 'Administrador',
    });
    const alert = await screen.findByRole('alert');
    expect(alert).toHaveTextContent('Se aplicaron: datos de identidad');
    expect(alert).toHaveTextContent('No se pudo actualizar rol en la sede');
    expect(alert).toHaveTextContent('No tiene permisos');
    expect(api.updateMembershipWorkProfile).not.toHaveBeenCalled();
    expect(screen.getByTestId('path')).toHaveTextContent(`/Users/Edit/${TARGET_ID}`);
  });

  it('bloquea el propio rol en la sede activa y la edición de un SuperAdmin por un Administrador', async () => {
    const self = record({ id: ADMIN_ID, memberships: [membership({ role: 'Administrador' })] });
    const { unmount } = render(
      <Harness
        api={makeUsersApi({ userDetails: vi.fn().mockResolvedValue(self) })}
        viewer={SITE_ADMIN}
        start={`/Users/Edit/${ADMIN_ID}`}
      />,
    );
    expect(
      await screen.findByText('No puede cambiar su propio rol en la sede activa.'),
    ).toBeInTheDocument();
    expect(screen.getByLabelText('Rol de Aplicación')).toBeDisabled();
    unmount();

    render(
      <Harness
        api={makeUsersApi({
          userDetails: vi.fn().mockResolvedValue(record({ isSuperAdmin: true })),
        })}
        viewer={SITE_ADMIN}
        start={`/Users/Edit/${TARGET_ID}`}
      />,
    );
    expect(
      await screen.findByText('Un Administrador de sede no puede modificar una cuenta SuperAdmin.'),
    ).toBeInTheDocument();
    expect(screen.getByRole('button', { name: 'Guardar Cambios' })).toBeDisabled();
  });

  it('muestra la ficha administrativa con identidad, membresías y auditoría veraz', async () => {
    render(
      <Harness api={makeUsersApi()} viewer={SITE_ADMIN} start={`/Users/Details/${TARGET_ID}`} />,
    );

    expect(await screen.findByRole('heading', { name: 'Juan Pérez' })).toBeInTheDocument();
    expect(screen.getByText('@jperez')).toBeInTheDocument();
    expect(screen.getByText('1234567')).toBeInTheDocument();
    // createdBy null = labeled system action; modifiedBy comes from the backend.
    expect(screen.getByText('Sistema')).toBeInTheDocument();
    expect(screen.getByText('Administradora Univalle')).toBeInTheDocument();
    expect(screen.getByRole('cell', { name: 'Sede Central' })).toBeInTheDocument();
    // No HTTP control can grant or revoke SuperAdmin.
    expect(screen.queryByRole('button', { name: /SuperAdmin/ })).toBeNull();
  });

  it('opera estado de cuenta y membresía con comandos separados y muestra 409', async () => {
    const api = makeUsersApi({
      changeMembershipStatus: vi
        .fn()
        .mockRejectedValue(apiError(409, 'CONFLICT', 'Membership state changed.')),
    });
    const user = userEvent.setup();
    render(<Harness api={api} viewer={SITE_ADMIN} start={`/Users/Details/${TARGET_ID}`} />);

    await user.click(await screen.findByRole('button', { name: 'Inactivar cuenta' }));
    await waitFor(() =>
      expect(api.setAccountStatus).toHaveBeenCalledWith(TARGET_ID, { accountStatus: 'inactive' }),
    );
    await user.click(screen.getByRole('button', { name: 'Suspender' }));
    expect(await screen.findByRole('alert')).toHaveTextContent(
      'Conflicto: Membership state changed.',
    );
    expect(api.changeMembershipStatus).toHaveBeenCalledWith(TARGET_ID, SITE_ID, {
      status: 'suspended',
    });
  });

  it('restaura una membresía revocada junto con la cuenta eliminada por la cascada de sede', async () => {
    const deleted = record({
      accountStatus: 'deleted',
      memberships: [membership({ status: 'revoked', effectiveStatus: 'Eliminado' })],
    });
    const api = makeUsersApi({ userDetails: vi.fn().mockResolvedValue(deleted) });
    render(<Harness api={api} viewer={SITE_ADMIN} start={`/Users/Details/${TARGET_ID}`} />);

    await userEvent.setup().click(await screen.findByRole('button', { name: 'Restaurar' }));
    await waitFor(() =>
      expect(api.restoreMembership).toHaveBeenCalledWith(TARGET_ID, SITE_ID, {
        restoreAccount: true,
      }),
    );
    expect(screen.queryByRole('button', { name: 'Restaurar cuenta' })).toBeNull();
  });

  it('revoca el acceso de sede como Administrador y ofrece baja global solo al SuperAdmin', async () => {
    const api = makeUsersApi();
    const user = userEvent.setup();
    const { unmount } = render(
      <Harness api={api} viewer={SITE_ADMIN} start={`/Users/Delete/${TARGET_ID}`} />,
    );
    expect(
      await screen.findByRole('heading', { name: '¿Revocar Acceso al Sistema?' }),
    ).toBeInTheDocument();
    expect(screen.queryByRole('button', { name: /Eliminar cuenta global/ })).toBeNull();
    await user.click(screen.getByRole('button', { name: /Sí, Confirmar Baja en Sede Central/ }));
    await waitFor(() => expect(api.revokeMembership).toHaveBeenCalledWith(TARGET_ID, SITE_ID));
    expect(api.deleteAccount).not.toHaveBeenCalled();
    expect(screen.getByTestId('path')).toHaveTextContent('/Users/Index');
    unmount();

    render(<Harness api={api} viewer={SUPER_ADMIN} start={`/Users/Delete/${TARGET_ID}`} />);
    await user.click(await screen.findByRole('button', { name: /Eliminar cuenta global/ }));
    await waitFor(() => expect(api.deleteAccount).toHaveBeenCalledWith(TARGET_ID));
  });

  it('impide la baja propia y advierte si la cuenta ya estaba eliminada', async () => {
    const { unmount } = render(
      <Harness
        api={makeUsersApi({ userDetails: vi.fn().mockResolvedValue(record({ id: ADMIN_ID })) })}
        viewer={SITE_ADMIN}
        start={`/Users/Delete/${ADMIN_ID}`}
      />,
    );
    expect(await screen.findByText('No puede dar de baja su propia cuenta.')).toBeInTheDocument();
    expect(screen.queryByRole('button', { name: /Confirmar Baja/ })).toBeNull();
    unmount();

    render(
      <Harness
        api={makeUsersApi({
          userDetails: vi.fn().mockResolvedValue(record({ accountStatus: 'deleted' })),
        })}
        viewer={SUPER_ADMIN}
        start={`/Users/Delete/${TARGET_ID}`}
      />,
    );
    expect(await screen.findByText(/ya se encuentra dado de baja/)).toBeInTheDocument();
  });

  it('calcula solo los campos globales modificados', () => {
    const user = record();
    expect(
      globalFieldChanges(user, {
        firstName: 'Juan',
        lastName: 'Pérez',
        secondLastName: '',
        identityCard: '1234567',
        email: ' nuevo@univalle.edu ',
        phoneNumber: '70000001',
      }),
    ).toEqual({ email: 'nuevo@univalle.edu' });
  });
});

describe('Foto de perfil — validación de cliente', () => {
  it('rechaza tipo, extensión y tamaño inválidos sin confiar solo en el nombre', () => {
    const png = new File([new Uint8Array(10)], 'foto.png', { type: 'image/png' });
    expect(photoFileProblem(png)).toBeNull();
    expect(
      photoFileProblem(new File([new Uint8Array(10)], 'foto.png', { type: 'image/gif' })),
    ).toMatch(/Formato no admitido/);
    expect(
      photoFileProblem(new File([new Uint8Array(10)], 'foto.exe', { type: 'image/png' })),
    ).toMatch(/extensión/);
    const big = new File([new Uint8Array(5 * 1024 * 1024 + 1)], 'big.jpg', { type: 'image/jpeg' });
    expect(photoFileProblem(big)).toMatch(/5 MB/);
  });

  it('sube la foto desde la ficha y muestra el error de validación del servidor', async () => {
    const api = makeUsersApi({
      uploadUserPhoto: vi.fn().mockRejectedValue(
        new ApiClientError(400, {
          success: false,
          error: {
            code: 'VALIDATION_ERROR',
            message: 'Invalid photo.',
            fieldErrors: { photo: ['signature_mismatch'] },
          },
        }),
      ),
    });
    const user = userEvent.setup();
    render(<Harness api={api} viewer={SITE_ADMIN} start={`/Users/Details/${TARGET_ID}`} />);

    const input = await screen.findByLabelText('Seleccionar foto de perfil');
    await user.upload(input, new File([new Uint8Array(16)], 'foto.jpg', { type: 'image/jpeg' }));
    await waitFor(() =>
      expect(api.uploadUserPhoto).toHaveBeenCalledWith(
        TARGET_ID,
        expect.objectContaining({ fileName: 'foto.jpg', contentType: 'image/jpeg' }),
      ),
    );
    expect(
      await screen.findByText('el contenido no corresponde a una imagen válida'),
    ).toBeInTheDocument();
  });
});

describe('Usuarios — paridad visual F6', () => {
  beforeEach(() => {
    vi.spyOn(window, 'confirm').mockReturnValue(true);
  });

  it('lista con badge-pill legacy, acciones con icono etiquetadas, contador vivo y paginación numerada', async () => {
    const api = makeUsersApi({
      listUsers: vi
        .fn()
        .mockResolvedValue(
          page(
            [record(), record({ id: 'x', fullName: 'Ana Inactiva', accountStatus: 'inactive' })],
            3,
          ),
        ),
    });
    const user = userEvent.setup();
    render(<Harness api={api} viewer={SITE_ADMIN} start="/Users/Index" />);

    const row = (await screen.findByText('Juan Pérez')).closest('tr')!;
    expect(within(row).getByText('Activo')).toHaveClass('badge', 'badge-pill', 'badge-success');
    const inactive = screen.getByText('Ana Inactiva').closest('tr')!;
    expect(within(inactive).getByText('Inactivo')).toHaveClass('badge-danger');
    expect(within(row).getByRole('button', { name: 'Editar' })).toHaveAttribute('title', 'Editar');
    expect(within(row).getByText('Sist.')).toBeInTheDocument();
    expect(screen.getByText('cuenta(s)', { exact: false })).toHaveAttribute('aria-live', 'polite');
    expect(screen.queryByRole('button', { name: 'Anterior' })).toBeNull();
    expect(screen.getByRole('button', { name: '1' })).toHaveAttribute('aria-current', 'page');
    await user.click(screen.getByRole('button', { name: '3' }));
    await waitFor(() =>
      expect(api.listUsers).toHaveBeenLastCalledWith(expect.objectContaining({ currentPage: 3 })),
    );
  });

  it('sube la foto elegida en Editar como un comando más, después de los datos de identidad', async () => {
    const api = makeUsersApi();
    const user = userEvent.setup();
    render(<Harness api={api} viewer={SITE_ADMIN} start={`/Users/Edit/${TARGET_ID}`} />);

    const phone = await screen.findByLabelText('Teléfono de Contacto');
    await waitFor(() => expect(phone).toHaveValue('70000001'));
    expect(screen.getByText('Sin foto de perfil')).toBeInTheDocument();
    await user.upload(
      screen.getByLabelText(/Foto de Perfil/),
      new File([new Uint8Array(16)], 'nueva.png', { type: 'image/png' }),
    );
    expect(screen.getByText('nueva.png')).toBeInTheDocument();
    await user.click(screen.getByRole('button', { name: 'Guardar Cambios' }));

    await waitFor(() =>
      expect(api.uploadUserPhoto).toHaveBeenCalledWith(
        TARGET_ID,
        expect.objectContaining({ fileName: 'nueva.png', contentType: 'image/png' }),
      ),
    );
    expect(api.updateUserGlobalFields).not.toHaveBeenCalled();
    expect(screen.getByTestId('path')).toHaveTextContent('/Users/Index');
  });

  it('muestra la ficha con la maqueta legacy y fechas de calendario sin desfase horario', async () => {
    const previousTz = process.env.TZ;
    process.env.TZ = 'America/La_Paz';
    try {
      render(
        <Harness api={makeUsersApi()} viewer={SITE_ADMIN} start={`/Users/Details/${TARGET_ID}`} />,
      );
      expect(await screen.findByText('Ficha de usuario')).toBeInTheDocument();
      // hireDate 2024-02-01 is a calendar date: never rendered as 31/01/2024 west of UTC.
      expect(screen.getByText('01/02/2024')).toBeInTheDocument();
      expect(screen.getByText('Ingreso: 01/02/2024')).toBeInTheDocument();
      expect(screen.getByText('Seguridad de acceso')).toBeInTheDocument();
      expect(screen.getByRole('button', { name: 'Revocar Acceso' })).toHaveClass('btn-link');
      expect(screen.getByRole('img', { name: 'Iniciales de Juan Pérez' })).toHaveClass(
        'user-detail-avatar',
        'user-detail-initials',
      );
    } finally {
      process.env.TZ = previousTz;
    }
  });

  it('abre el alta de identidad con Nueva Identidad y añade el filtro de tipo a la zona compartida', async () => {
    const peopleApi = makePeopleApi();
    const user = userEvent.setup();
    render(
      <Harness
        api={makeUsersApi()}
        viewer={SITE_ADMIN}
        start="/Users/Index?tab=personas"
        peopleApi={peopleApi}
      />,
    );

    expect(
      await screen.findByText('No se han registrado identidades en la base de datos base.'),
    ).toBeInTheDocument();
    expect(screen.queryByRole('form', { name: 'Nueva identidad' })).toBeNull();
    await user.selectOptions(screen.getByLabelText('Filtrar por tipo'), 'external');
    await waitFor(() =>
      expect(peopleApi.people).toHaveBeenLastCalledWith(
        expect.objectContaining({ type: 'external' }),
      ),
    );
    await user.click(screen.getByRole('button', { name: 'Nueva Identidad' }));
    expect(screen.getByRole('form', { name: 'Nueva identidad' })).toBeInTheDocument();
  });
});

describe('Mi Perfil', () => {
  const PROFILE: ProfileRecord = {
    id: ADMIN_ID,
    username: 'admin.qa',
    email: 'admin@univalle.edu',
    firstName: 'Administradora',
    lastName: 'Univalle',
    secondLastName: null,
    fullName: 'Administradora Univalle',
    initials: 'AU',
    phoneNumber: '70000000',
    identityCard: '123456',
    isSuperAdmin: false,
    accountStatus: 'active',
    profilePicture: null,
    memberships: [membership({ role: 'Administrador' })],
  };

  function makeProfileApi(overrides: Partial<ProfileApi> = {}): ProfileApi {
    return {
      ownProfile: vi.fn().mockResolvedValue(PROFILE),
      updateOwnProfile: vi.fn().mockResolvedValue({ ...PROFILE, email: 'nuevo@univalle.edu' }),
      ownPhoto: vi.fn(),
      uploadOwnPhoto: vi.fn(),
      deleteOwnPhoto: vi.fn(),
      changePassword: vi.fn(),
      currentSession: vi.fn(),
      ...overrides,
    };
  }

  it('actualiza solo los campos propios permitidos y notifica el cambio de identidad', async () => {
    const api = makeProfileApi();
    const onIdentityChanged = vi.fn();
    const user = userEvent.setup();
    render(
      <ProfilePanel api={api} onSessionChanged={vi.fn()} onIdentityChanged={onIdentityChanged} />,
    );

    const email = await screen.findByLabelText('Correo institucional');
    await waitFor(() => expect(email).toHaveValue('admin@univalle.edu'));
    expect(screen.getByLabelText('C.I.')).toHaveAttribute('readonly');
    await user.clear(email);
    await user.type(email, 'nuevo@univalle.edu');
    await user.click(screen.getByRole('button', { name: 'Guardar perfil' }));

    await waitFor(() =>
      expect(api.updateOwnProfile).toHaveBeenCalledWith({ email: 'nuevo@univalle.edu' }),
    );
    expect(onIdentityChanged).toHaveBeenCalledOnce();
    expect(await screen.findByText('Perfil actualizado correctamente.')).toBeInTheDocument();
  });
});

describe('Personas — filtros reales de la API', () => {
  it('envía statusFilter 2 para Baja y la búsqueda al servidor', async () => {
    const api = makePeopleApi();
    const user = userEvent.setup();
    render(<PeoplePanel api={api} />);

    await waitFor(() => expect(api.people).toHaveBeenCalled());
    expect(vi.mocked(api.people).mock.calls[0]![0]).toMatchObject({ statusFilter: undefined });
    await user.selectOptions(screen.getByLabelText('Filtrar personas por estado'), '2');
    await waitFor(() =>
      expect(api.people).toHaveBeenLastCalledWith(expect.objectContaining({ statusFilter: 2 })),
    );
    await user.type(screen.getByLabelText('Buscar persona'), 'proveedor{Enter}');
    await waitFor(() =>
      expect(api.people).toHaveBeenLastCalledWith(
        expect.objectContaining({ searchTerm: 'proveedor', statusFilter: 2, currentPage: 1 }),
      ),
    );
  });

  it('muestra un 403 de sede como mensaje comprensible', async () => {
    const api = makePeopleApi();
    vi.mocked(api.people).mockRejectedValue(apiError(403, 'SITE_ACCESS_DENIED'));
    render(<PeoplePanel api={api} />);
    expect(await screen.findByRole('alert')).toHaveTextContent('rol Administrador');
  });
});

// MIG-001 F7: the live E2E run caught an out-of-order response overwriting a newer
// search result (last response wins). Only the newest request may commit.
describe('Listados — respuestas fuera de orden (F7)', () => {
  function deferred<T>() {
    let resolve!: (value: T) => void;
    const promise = new Promise<T>((r) => {
      resolve = r;
    });
    return { promise, resolve };
  }

  it('Usuarios: a slow unfiltered response never overwrites a newer search result', async () => {
    const user = userEvent.setup();
    const first = deferred<ManagedUserPage>();
    const second = deferred<ManagedUserPage>();
    const listUsers = vi
      .fn()
      .mockReturnValueOnce(first.promise)
      .mockReturnValueOnce(second.promise);
    const api = makeUsersApi({ listUsers });
    render(<Harness api={api} viewer={SITE_ADMIN} start="/Users/Index" />);

    await waitFor(() => expect(listUsers).toHaveBeenCalledTimes(1));
    await user.type(screen.getByLabelText('Buscar usuario'), 'nuevo');
    await user.click(screen.getByRole('button', { name: 'Buscar' }));
    await waitFor(() => expect(listUsers).toHaveBeenCalledTimes(2));

    second.resolve(
      page([
        record({
          id: 'n1',
          username: 'nuevo',
          fullName: 'Nuevo Resultado',
          firstName: 'Nuevo',
          lastName: 'Resultado',
        }),
      ]),
    );
    expect(await screen.findByText('Nuevo Resultado')).toBeInTheDocument();
    first.resolve(
      page([
        record({
          id: 'v1',
          username: 'viejo',
          fullName: 'Viejo Listado',
          firstName: 'Viejo',
          lastName: 'Listado',
        }),
      ]),
    );
    await first.promise;
    await waitFor(() => expect(screen.getByText('Nuevo Resultado')).toBeInTheDocument());
    expect(screen.queryByText('Viejo Listado')).not.toBeInTheDocument();
  });

  it('Personas: a slow unfiltered response never overwrites a newer search result', async () => {
    const user = userEvent.setup();
    const person = (id: number, name: string) => ({
      id,
      actorCode: null,
      type: 'internal' as const,
      name,
      email: null,
      phoneNumber: null,
      isEntity: false,
      address: null,
      category: 99 as const,
      status: 0 as const,
      createdAt: '2026-01-01T00:00:00.000Z',
      updatedAt: null,
    });
    const first = deferred<PersonPage>();
    const second = deferred<PersonPage>();
    const api = makePeopleApi();
    vi.mocked(api.people).mockReturnValueOnce(first.promise).mockReturnValueOnce(second.promise);
    render(<PeoplePanel api={api} />);

    await waitFor(() => expect(api.people).toHaveBeenCalledTimes(1));
    await user.type(screen.getByLabelText('Buscar persona'), 'nueva');
    await user.click(screen.getByRole('button', { name: 'Buscar' }));
    await waitFor(() => expect(api.people).toHaveBeenCalledTimes(2));

    second.resolve({
      ...EMPTY_PEOPLE,
      items: [person(2, 'Persona Nueva')],
      totalCount: 1,
      totalPages: 1,
    });
    expect(await screen.findByText('Persona Nueva')).toBeInTheDocument();
    first.resolve({
      ...EMPTY_PEOPLE,
      items: [person(1, 'Persona Vieja')],
      totalCount: 1,
      totalPages: 1,
    });
    await first.promise;
    await waitFor(() => expect(screen.getByText('Persona Nueva')).toBeInTheDocument());
    expect(screen.queryByText('Persona Vieja')).not.toBeInTheDocument();
  });
});

// MIG-001 F8: a SuperAdmin without eligible site memberships cannot link a user
// (F1 §17). Truthful inline notice + disabled submit, no raw server 400.
describe('MIG-001 F8 — Alta: SuperAdmin sin sedes elegibles', () => {
  const SUPER_ADMIN_NO_SITES: UsersViewer = {
    ...SUPER_ADMIN,
    activeSiteId: null,
    activeSiteName: null,
    siteRole: null,
    eligibleSites: [],
  };

  it('SuperAdmin sin eligibleSites: muestra aviso, deshabilita Crear Cuenta y nunca llama a la API', async () => {
    const api = makeUsersApi();
    const user = userEvent.setup();
    render(<Harness api={api} viewer={SUPER_ADMIN_NO_SITES} start="/Users/Create" />);

    const notice = await screen.findByTestId('superadmin-no-sites-notice');
    expect(notice).toHaveTextContent(
      'Para vincular un usuario se requiere al menos una membresía de sede.',
    );
    expect(notice).toHaveTextContent(
      'Su cuenta SuperAdmin no tiene sedes elegibles; solicite o asigne una membresía de sede antes de crear cuentas.',
    );
    expect(notice.className).toContain('alert-info');

    const submit = screen.getByRole('button', { name: 'Crear Cuenta' });
    expect(submit).toBeDisabled();
    await user.click(submit);
    expect(api.createManagedUser).not.toHaveBeenCalled();
    expect(api.updateMembershipWorkProfile).not.toHaveBeenCalled();
    expect(api.uploadUserPhoto).not.toHaveBeenCalled();
  });

  it('SuperAdmin con eligibleSites: no muestra aviso y permite enviar el alta', async () => {
    const api = makeUsersApi();
    const user = userEvent.setup();
    render(<Harness api={api} viewer={SUPER_ADMIN} start="/Users/Create" />);

    await user.type(await screen.findByLabelText('Nombres'), 'Ana');
    await user.type(screen.getByLabelText('Primer Apellido'), 'Rojas');
    await user.type(screen.getByLabelText('Cédula de Identidad'), '7654321');
    await user.type(screen.getByLabelText('Correo Institucional'), 'arojas@univalle.edu');
    await user.type(screen.getByLabelText('Teléfono de Contacto'), '70000002');
    await user.type(screen.getByLabelText('Nombre de Usuario (Login)'), 'arojas');
    await user.type(screen.getByLabelText('Contraseña de Acceso'), 'Clave-Segura-2026');
    expect(screen.queryByTestId('superadmin-no-sites-notice')).toBeNull();
    expect(screen.getByRole('button', { name: 'Crear Cuenta' })).toBeEnabled();
    await user.click(screen.getByRole('button', { name: 'Crear Cuenta' }));

    await waitFor(() => expect(api.createManagedUser).toHaveBeenCalled());
  });
});

// MIG-001 F8: voluntary password change must show a success alert and clear it
// on the next submit. Errors keep the previous behavior.
describe('MIG-001 F8 — Cambio voluntario de contraseña (/Profile)', () => {
  const PASSWORD_SESSION: ActiveSiteSession = {
    userId: ADMIN_ID,
    displayName: 'Administradora Univalle',
    email: 'admin@univalle.edu',
    activeSiteId: SITE_ID,
    activeSiteName: 'Sede Central',
    globalRole: null,
    memberships: [
      {
        siteId: SITE_ID,
        siteName: 'Sede Central',
        role: SiteRole.Administrador,
        state: SiteState.Active,
      },
    ],
  };

  function buildSession(overrides: Partial<AuthSessionResponse> = {}): AuthSessionResponse {
    return {
      session: PASSWORD_SESSION,
      purpose: 'normal',
      mustChangePassword: false,
      eligibleSites: [{ siteId: SITE_ID, siteName: 'Sede Central', role: 'Administrador' }],
      ...overrides,
    };
  }

  function makePasswordApi(overrides: Partial<ProfileApi> = {}): ProfileApi {
    return {
      ownProfile: vi.fn(),
      updateOwnProfile: vi.fn(),
      ownPhoto: vi.fn(),
      uploadOwnPhoto: vi.fn(),
      deleteOwnPhoto: vi.fn(),
      changePassword: vi.fn(),
      currentSession: vi.fn(),
      ...overrides,
    };
  }

  async function fillAndSubmit(
    user: ReturnType<typeof userEvent.setup>,
    current: string,
    next: string,
  ): Promise<void> {
    await user.type(screen.getByLabelText('Contraseña actual'), current);
    await user.type(screen.getByLabelText('Nueva contraseña'), next);
    await user.type(screen.getByLabelText('Confirmar nueva contraseña'), next);
    await user.click(screen.getByRole('button', { name: 'Cambiar contraseña' }));
  }

  it('muestra el aviso de éxito tras un cambio resuelto y limpia los campos', async () => {
    const api = makePasswordApi({
      changePassword: vi.fn().mockResolvedValue(buildSession()),
    });
    const user = userEvent.setup();
    const onChanged = vi.fn();
    render(
      <PasswordChangeForm api={api} onChanged={onChanged} title="Establecer nueva contraseña" />,
    );

    await fillAndSubmit(user, 'Antigua-Clave-2024', 'Nueva-Clave-Segura-2026');

    const success = await screen.findByRole('status');
    expect(success).toHaveTextContent('Contraseña actualizada correctamente.');
    expect(onChanged).toHaveBeenCalledOnce();
    await waitFor(() => expect(screen.getByLabelText('Contraseña actual')).toHaveValue(''));
  });

  it('no muestra el aviso de éxito cuando la API rechaza el cambio', async () => {
    const api = makePasswordApi({
      changePassword: vi.fn().mockRejectedValue(apiError(401, 'AUTH_FAILED')),
      currentSession: vi.fn().mockResolvedValue(buildSession()),
    });
    const user = userEvent.setup();
    render(<PasswordChangeForm api={api} onChanged={vi.fn()} />);

    await fillAndSubmit(user, 'Antigua-Clave-2024', 'Nueva-Clave-Segura-2026');

    const error = await screen.findByRole('alert');
    expect(error).toHaveTextContent('La contraseña actual es incorrecta.');
    expect(screen.queryByText('Contraseña actualizada correctamente.')).toBeNull();
  });

  it('limpia el aviso de éxito al iniciar un nuevo envío', async () => {
    const api = makePasswordApi({
      changePassword: vi
        .fn()
        .mockResolvedValueOnce(buildSession())
        .mockRejectedValueOnce(apiError(401, 'AUTH_FAILED')),
      currentSession: vi.fn().mockResolvedValue(buildSession()),
    });
    const user = userEvent.setup();
    render(<PasswordChangeForm api={api} onChanged={vi.fn()} />);

    await fillAndSubmit(user, 'Antigua-Clave-2024', 'Nueva-Clave-Segura-2026');
    expect(await screen.findByText('Contraseña actualizada correctamente.')).toBeInTheDocument();

    await fillAndSubmit(user, 'Nueva-Clave-Segura-2026', 'Otra-Clave-Mas-2026');
    const error = await screen.findByRole('alert');
    expect(error).toBeInTheDocument();
    expect(screen.queryByText('Contraseña actualizada correctamente.')).toBeNull();
  });
});

// MIG-001 F8: a site Administrador viewing a user whose active-site membership
// is revoked must only see "Restaurar" on the membership row; global commands
// stay hidden. SuperAdmin still sees the full command surface.
describe('MIG-001 F8 — Detalles con membresía activa revocada', () => {
  function revokedMembershipUser(accountStatus: 'active' | 'deleted'): ManagedUserRecord {
    return record({
      accountStatus,
      memberships: [
        membership({
          status: 'revoked',
          effectiveStatus: accountStatus === 'deleted' ? 'Eliminado' : 'Activo',
        }),
      ],
    });
  }

  it('Sede Administrador · cuenta activa: solo Restaurar, sin Modificar Perfil ni Inactivar', async () => {
    const user = revokedMembershipUser('active');
    const api = makeUsersApi({ userDetails: vi.fn().mockResolvedValue(user) });
    const event = userEvent.setup();
    render(<Harness api={api} viewer={SITE_ADMIN} start={`/Users/Details/${TARGET_ID}`} />);

    await screen.findByRole('heading', { name: 'Juan Pérez' });
    const restore = await screen.findByRole('button', { name: 'Restaurar' });
    expect(restore).toBeInTheDocument();
    expect(screen.queryByRole('button', { name: 'Modificar Perfil' })).toBeNull();
    expect(screen.queryByRole('button', { name: 'Inactivar cuenta' })).toBeNull();
    expect(screen.queryByRole('button', { name: 'Activar cuenta' })).toBeNull();
    expect(screen.queryByRole('button', { name: 'Revocar Acceso' })).toBeNull();
    expect(screen.queryByRole('heading', { name: 'Gestión de Credenciales' })).toBeNull();
    // The photo block hides upload/remove when globalManageable is false.
    expect(screen.queryByLabelText('Cargar foto de perfil')).toBeNull();
    expect(screen.queryByLabelText('Quitar foto de perfil')).toBeNull();

    await event.click(restore);
    await waitFor(() => expect(api.restoreMembership).toHaveBeenCalledWith(TARGET_ID, SITE_ID, {}));
    expect(api.restoreAccount).not.toHaveBeenCalled();
  });

  it('Sede Administrador · cuenta eliminada: Restaurar adjunta restoreAccount:true', async () => {
    const user = revokedMembershipUser('deleted');
    const api = makeUsersApi({ userDetails: vi.fn().mockResolvedValue(user) });
    const event = userEvent.setup();
    render(<Harness api={api} viewer={SITE_ADMIN} start={`/Users/Details/${TARGET_ID}`} />);

    await screen.findByRole('heading', { name: 'Juan Pérez' });
    const restore = await screen.findByRole('button', { name: 'Restaurar' });
    expect(restore).toBeInTheDocument();
    expect(screen.queryByRole('button', { name: 'Modificar Perfil' })).toBeNull();
    expect(screen.queryByRole('button', { name: 'Inactivar cuenta' })).toBeNull();
    // Site Administrador never sees the global "Restaurar cuenta" button.
    expect(screen.queryByRole('button', { name: 'Restaurar cuenta' })).toBeNull();

    await event.click(restore);
    await waitFor(() =>
      expect(api.restoreMembership).toHaveBeenCalledWith(TARGET_ID, SITE_ID, {
        restoreAccount: true,
      }),
    );
  });

  it('SuperAdmin sigue viendo Modificar Perfil sobre la misma cuenta', async () => {
    const user = revokedMembershipUser('active');
    const api = makeUsersApi({ userDetails: vi.fn().mockResolvedValue(user) });
    render(<Harness api={api} viewer={SUPER_ADMIN} start={`/Users/Details/${TARGET_ID}`} />);

    await screen.findByRole('heading', { name: 'Juan Pérez' });
    expect(screen.getByRole('button', { name: 'Modificar Perfil' })).toBeInTheDocument();
    // SuperAdmin also still sees the membership-level Restaurar control.
    expect(screen.getByRole('button', { name: 'Restaurar' })).toBeInTheDocument();
  });
});
