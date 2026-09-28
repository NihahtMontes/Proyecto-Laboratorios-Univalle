import { beforeEach, describe, expect, it, vi } from 'vitest';
import { render, screen, waitFor } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { ApiClientError } from '@lu/api-client';
import {
  GlobalRole,
  SiteRole,
  SiteState,
  type ActiveSiteSession,
  type AuthSessionResponse,
  type EligibleSite,
  type ManagedUserPage,
  type SiteId,
  type SiteRequestContext,
} from '@lu/contracts';
import { App, type AuthApi } from './App';

const SITE_ID = '22222222-2222-4222-8222-222222222222' as SiteId;
const OTHER_SITE_ID = 'aaaaaaaa-aaaa-4aaa-8aaa-aaaaaaaaaaaa' as SiteId;

const ACTIVE_SESSION: ActiveSiteSession = {
  userId: '11111111-1111-4111-8111-111111111111',
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

const ELIGIBLE: readonly EligibleSite[] = [
  { siteId: SITE_ID, siteName: 'Sede Central', role: 'Administrador' },
  { siteId: OTHER_SITE_ID, siteName: 'Sede Norte', role: 'Supervisor' },
];

function authState(
  purpose: AuthSessionResponse['purpose'],
  overrides: Partial<AuthSessionResponse> = {},
): AuthSessionResponse {
  const restricted = purpose !== 'normal';
  return {
    purpose,
    mustChangePassword: false,
    eligibleSites: purpose === 'password_change' ? [] : [ELIGIBLE[0]!],
    session: restricted
      ? { ...ACTIVE_SESSION, activeSiteId: null, activeSiteName: null, memberships: [] }
      : ACTIVE_SESSION,
    ...overrides,
  };
}

const NORMAL = authState('normal');
const SITE_SELECTION = authState('site_selection', { eligibleSites: ELIGIBLE });

const SITE_CONTEXT: SiteRequestContext = {
  correlationId: '33333333-3333-4333-8333-333333333333',
  userId: ACTIVE_SESSION.userId,
  siteId: SITE_ID,
  siteName: 'Sede Central',
  siteRole: SiteRole.Administrador,
  globalRole: null,
};

function apiError(status: number, code: string, message = 'error'): ApiClientError {
  return new ApiClientError(status, { success: false, error: { code, message } });
}
const UNAUTHORIZED = apiError(401, 'INVALID_CREDENTIALS', 'Invalid credentials.');

function makeApi(overrides: Partial<AuthApi> = {}): AuthApi {
  return {
    currentSession: vi.fn().mockRejectedValue(UNAUTHORIZED),
    signIn: vi.fn().mockResolvedValue(NORMAL),
    selectActiveSite: vi.fn().mockResolvedValue(NORMAL),
    changePassword: vi.fn().mockResolvedValue(NORMAL),
    siteContext: vi.fn().mockResolvedValue(SITE_CONTEXT),
    logout: vi.fn().mockResolvedValue(undefined),
    ...overrides,
  };
}

const EMPTY_USERS: ManagedUserPage = {
  items: [],
  totalCount: 0,
  pageIndex: 1,
  totalPages: 0,
  pageSize: 20,
};

async function signInAs(identifier: string): Promise<void> {
  const user = userEvent.setup();
  await screen.findByRole('heading', { name: 'Ingreso al Sistema' });
  await user.type(screen.getByLabelText('Usuario o Correo'), identifier);
  await user.type(screen.getByLabelText('Contraseña'), 'password-not-logged');
  await user.click(screen.getByRole('button', { name: 'INICIAR SESIÓN' }));
}

describe('Aplicación React autenticada (MIG-001 F5)', () => {
  beforeEach(() => {
    window.history.replaceState({}, '', '/');
  });

  it('reproduce los campos y textos del login Razor', async () => {
    render(<App api={makeApi()} />);

    expect(await screen.findByRole('heading', { name: 'Ingreso al Sistema' })).toBeInTheDocument();
    expect(screen.getByLabelText('Usuario o Correo')).toHaveAttribute('autocomplete', 'username');
    expect(screen.getByLabelText('Usuario o Correo')).toHaveAttribute('name', 'loginIdentifier');
    expect(screen.getByLabelText('Contraseña')).toHaveAttribute('autocomplete', 'current-password');
    // Login transport bound is 512 bytes: legacy passwords > 72 bytes must still reach the server.
    expect(screen.getByLabelText('Contraseña')).toHaveAttribute('maxlength', '512');
    expect(screen.getByLabelText('Recordarme en este equipo')).toBeInTheDocument();
  });

  it('inicia sesión por username enviando loginIdentifier y abre el shell', async () => {
    const api = makeApi();
    const user = userEvent.setup();
    render(<App api={api} />);
    await screen.findByRole('heading', { name: 'Ingreso al Sistema' });

    await user.type(screen.getByLabelText('Usuario o Correo'), 'admin.qa');
    await user.type(screen.getByLabelText('Contraseña'), 'password-not-logged');
    await user.click(screen.getByLabelText('Recordarme en este equipo'));
    await user.click(screen.getByRole('button', { name: 'INICIAR SESIÓN' }));

    expect(
      await screen.findByRole('heading', { name: 'Dashboard de Gestión' }),
    ).toBeInTheDocument();
    expect(screen.getByText('Bienvenido, Administradora')).toBeInTheDocument();
    expect(api.signIn).toHaveBeenCalledWith({
      loginIdentifier: 'admin.qa',
      password: 'password-not-logged',
      rememberMe: true,
    });
    expect(api.siteContext).toHaveBeenCalledOnce();
  });

  it('inicia sesión por email con el mismo loginIdentifier (sin campo email legado)', async () => {
    const api = makeApi();
    render(<App api={api} />);
    await signInAs('admin@univalle.edu');

    await screen.findByRole('heading', { name: 'Dashboard de Gestión' });
    const body = vi.mocked(api.signIn).mock.calls[0]![0];
    expect(body.loginIdentifier).toBe('admin@univalle.edu');
    expect(body).not.toHaveProperty('email');
  });

  it('muestra credenciales inválidas y el tiempo de espera del rate limit', async () => {
    const limited = new ApiClientError(
      429,
      { success: false, error: { code: 'RATE_LIMITED', message: 'Too many.' } },
      30,
    );
    const signIn = vi.fn().mockRejectedValueOnce(UNAUTHORIZED).mockRejectedValueOnce(limited);
    render(<App api={makeApi({ signIn })} />);

    await signInAs('admin.qa');
    expect(await screen.findByRole('alert')).toHaveTextContent('Usuario o contraseña incorrectos.');
    await userEvent.setup().click(screen.getByRole('button', { name: 'INICIAR SESIÓN' }));
    expect(await screen.findByRole('alert')).toHaveTextContent('30 segundos');
  });

  it('enruta site_selection por purpose, usa eligibleSites y crea la sesión normal', async () => {
    const api = makeApi({ signIn: vi.fn().mockResolvedValue(SITE_SELECTION) });
    const user = userEvent.setup();
    render(<App api={api} />);
    await signInAs('admin.qa');

    expect(await screen.findByRole('heading', { name: 'Selecciona una sede' })).toBeInTheDocument();
    expect(screen.queryByRole('navigation', { name: 'Navegación principal' })).toBeNull();
    expect(screen.getByRole('button', { name: /Sede Norte\s*Supervisor/ })).toBeInTheDocument();
    expect(api.siteContext).not.toHaveBeenCalled();

    await user.click(screen.getByRole('button', { name: /Sede Central\s*Administrador/ }));
    await waitFor(() =>
      expect(api.selectActiveSite).toHaveBeenCalledWith({ activeSiteId: SITE_ID }),
    );
    expect(
      await screen.findByRole('navigation', { name: 'Navegación principal' }),
    ).toBeInTheDocument();
  });

  it('vuelve al login cuando expira la sesión restringida de selección de sede', async () => {
    const api = makeApi({
      currentSession: vi.fn().mockResolvedValue(SITE_SELECTION),
      selectActiveSite: vi.fn().mockRejectedValue(UNAUTHORIZED),
    });
    const user = userEvent.setup();
    render(<App api={api} />);

    await user.click(await screen.findByRole('button', { name: /Sede Norte/ }));
    expect(await screen.findByRole('heading', { name: 'Ingreso al Sistema' })).toBeInTheDocument();
    expect(screen.getByRole('alert')).toHaveTextContent('15 minutos');
  });

  it('enruta password_change por purpose aunque mustChangePassword sea false', async () => {
    // Legacy password > 72 bytes: purpose=password_change, mustChangePassword=false.
    const restricted = authState('password_change', { mustChangePassword: false });
    const api = makeApi({ signIn: vi.fn().mockResolvedValue(restricted) });
    const user = userEvent.setup();
    render(<App api={api} />);
    await signInAs('legacy.user');

    expect(
      await screen.findByRole('heading', { name: 'Cambio de contraseña requerido' }),
    ).toBeInTheDocument();
    expect(screen.getByText(/no cumple la política vigente/)).toBeInTheDocument();
    expect(screen.queryByRole('navigation', { name: 'Navegación principal' })).toBeNull();

    await user.type(screen.getByLabelText('Contraseña actual'), 'old-legacy-password');
    await user.type(screen.getByLabelText('Nueva contraseña'), 'Nueva-Clave-2026!');
    await user.type(screen.getByLabelText('Confirmar nueva contraseña'), 'Nueva-Clave-2026!');
    await user.click(screen.getByRole('button', { name: 'Cambiar contraseña' }));

    await waitFor(() =>
      expect(api.changePassword).toHaveBeenCalledWith({
        currentPassword: 'old-legacy-password',
        newPassword: 'Nueva-Clave-2026!',
      }),
    );
    expect(
      await screen.findByRole('heading', { name: 'Dashboard de Gestión' }),
    ).toBeInTheDocument();
  });

  it('valida la política localmente y distingue contraseña actual incorrecta de sesión expirada', async () => {
    const restricted = authState('password_change', { mustChangePassword: true });
    const api = makeApi({
      currentSession: vi.fn().mockResolvedValue(restricted),
      changePassword: vi.fn().mockRejectedValue(UNAUTHORIZED),
    });
    const user = userEvent.setup();
    render(<App api={api} />);

    await screen.findByText('Debe establecer una nueva contraseña antes de continuar.');
    await user.type(screen.getByLabelText('Contraseña actual'), 'wrong');
    await user.type(screen.getByLabelText('Nueva contraseña'), 'corta');
    await user.type(screen.getByLabelText('Confirmar nueva contraseña'), 'corta');
    await user.click(screen.getByRole('button', { name: 'Cambiar contraseña' }));
    expect(await screen.findByRole('alert')).toHaveTextContent('al menos 12 caracteres');
    expect(api.changePassword).not.toHaveBeenCalled();

    await user.clear(screen.getByLabelText('Nueva contraseña'));
    await user.clear(screen.getByLabelText('Confirmar nueva contraseña'));
    await user.type(screen.getByLabelText('Nueva contraseña'), 'Nueva-Clave-2026!');
    await user.type(screen.getByLabelText('Confirmar nueva contraseña'), 'Nueva-Clave-2026!');
    await user.click(screen.getByRole('button', { name: 'Cambiar contraseña' }));
    // Session still valid -> the 401 meant a wrong current password.
    expect(await screen.findByRole('alert')).toHaveTextContent(
      'La contraseña actual es incorrecta.',
    );

    vi.mocked(api.currentSession).mockRejectedValue(UNAUTHORIZED);
    await user.click(screen.getByRole('button', { name: 'Cambiar contraseña' }));
    expect(await screen.findByRole('heading', { name: 'Ingreso al Sistema' })).toBeInTheDocument();
  });

  it('bootstrap: sin sesión muestra login; sesión normal abre el shell sin pasar por login', async () => {
    const first = render(<App api={makeApi()} />);
    expect(await screen.findByRole('heading', { name: 'Ingreso al Sistema' })).toBeInTheDocument();
    first.unmount();

    let resolveContext: (value: SiteRequestContext) => void = () => undefined;
    const api = makeApi({
      currentSession: vi.fn().mockResolvedValue(NORMAL),
      siteContext: vi.fn(
        () =>
          new Promise<SiteRequestContext>((resolve) => {
            resolveContext = resolve;
          }),
      ),
    });
    render(<App api={api} />);
    await waitFor(() => expect(api.siteContext).toHaveBeenCalled());
    // Nothing privileged renders before the site context resolves.
    expect(screen.queryByRole('navigation', { name: 'Navegación principal' })).toBeNull();
    expect(screen.getByText('Validando sesión…')).toBeInTheDocument();
    resolveContext(SITE_CONTEXT);
    expect(
      await screen.findByRole('navigation', { name: 'Navegación principal' }),
    ).toBeInTheDocument();
    expect(api.signIn).not.toHaveBeenCalled();
  });

  it('bootstrap: una sesión password_change existente no abre el workspace', async () => {
    render(
      <App
        api={makeApi({ currentSession: vi.fn().mockResolvedValue(authState('password_change')) })}
      />,
    );
    expect(
      await screen.findByRole('heading', { name: 'Cambio de contraseña requerido' }),
    ).toBeInTheDocument();
  });

  it('SuperAdmin con sesión normal sin sede accede a Usuarios y Mi Perfil, no a datos de sede', async () => {
    window.history.replaceState({}, '', '/Equipment/Index');
    const globalAuth = authState('normal', {
      session: {
        ...ACTIVE_SESSION,
        displayName: 'Super Admin',
        activeSiteId: null,
        activeSiteName: null,
        globalRole: GlobalRole.SuperAdmin,
        memberships: [],
      },
      eligibleSites: [],
    });
    const listUsers = vi.fn().mockResolvedValue(EMPTY_USERS);
    const api = makeApi({ currentSession: vi.fn().mockResolvedValue(globalAuth), listUsers });
    render(<App api={api} />);

    expect(await screen.findByText('Sesión global sin sede activa')).toBeInTheDocument();
    expect(api.siteContext).not.toHaveBeenCalled();
    expect(window.location.pathname).toBe('/');
    const nav = screen.getByRole('navigation', { name: 'Navegación principal' });
    expect(nav).toHaveTextContent('Usuarios');
    expect(nav).not.toHaveTextContent('Equipo');

    await userEvent.setup().click(screen.getAllByRole('link', { name: 'Usuarios' }).at(-1)!);
    expect(await screen.findByRole('heading', { name: 'Control de Usuarios' })).toBeInTheDocument();
    expect(listUsers).toHaveBeenCalled();
  });

  it('un 401 de cualquier panel termina la sesión y vuelve al login sin bucles', async () => {
    window.history.replaceState({}, '', '/Users/Index');
    const listUsers = vi.fn().mockRejectedValue(UNAUTHORIZED);
    const api = makeApi({ currentSession: vi.fn().mockResolvedValue(NORMAL), listUsers });
    render(<App api={api} />);

    expect(await screen.findByRole('heading', { name: 'Ingreso al Sistema' })).toBeInTheDocument();
    expect(screen.getByRole('alert')).toHaveTextContent('Su sesión expiró o fue revocada');
    expect(api.currentSession).toHaveBeenCalledOnce();
  });

  it('redirige /Users/Details sin id a /Profile y con ?id= a la ficha administrativa', async () => {
    window.history.replaceState({}, '', '/Users/Details');
    const ownProfile = vi.fn().mockResolvedValue({
      id: ACTIVE_SESSION.userId,
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
      memberships: [],
    });
    render(
      <App
        api={makeApi({
          currentSession: vi.fn().mockResolvedValue(NORMAL),
          ownProfile,
          updateOwnProfile: vi.fn(),
          ownPhoto: vi.fn(),
          uploadOwnPhoto: vi.fn(),
          deleteOwnPhoto: vi.fn(),
        })}
      />,
    );
    expect(await screen.findByRole('heading', { name: 'Mi perfil' })).toBeInTheDocument();
    expect(window.location.pathname).toBe('/Profile');
  });

  it('navega por rutas del sidebar y no expone un ítem Personas separado', async () => {
    const user = userEvent.setup();
    render(<App api={makeApi({ currentSession: vi.fn().mockResolvedValue(NORMAL) })} />);
    await screen.findByRole('heading', { name: 'Dashboard de Gestión' });
    expect(screen.queryByRole('link', { name: 'Personas' })).toBeNull();

    await user.click(screen.getByRole('link', { name: 'Ver' }));

    expect(screen.getByRole('heading', { name: 'Ver' })).toBeInTheDocument();
    expect(window.location.pathname).toBe('/AssetView/Index');
  });

  it('oculta las rutas administrativas a un Supervisor', async () => {
    window.history.replaceState({}, '', '/Users/Index');
    render(
      <App
        api={makeApi({
          currentSession: vi.fn().mockResolvedValue(NORMAL),
          siteContext: vi
            .fn()
            .mockResolvedValue({ ...SITE_CONTEXT, siteRole: SiteRole.Supervisor }),
        })}
      />,
    );

    await screen.findByRole('heading', { name: 'Dashboard de Gestión' });
    expect(screen.queryByText('Gestiones')).not.toBeInTheDocument();
    expect(screen.queryByRole('link', { name: 'Usuarios' })).toBeNull();
    await waitFor(() => expect(window.location.pathname).toBe('/'));
  });

  it('cierra la sesión y limpia la identidad del estado React', async () => {
    const api = makeApi({ currentSession: vi.fn().mockResolvedValue(NORMAL) });
    const user = userEvent.setup();
    render(<App api={api} />);
    await screen.findByRole('heading', { name: 'Dashboard de Gestión' });

    await user.click(screen.getByRole('button', { name: /Administradora Univalle/ }));
    await user.click(screen.getByRole('button', { name: 'Cerrar Sesión' }));

    expect(await screen.findByRole('heading', { name: 'Ingreso al Sistema' })).toBeInTheDocument();
    expect(api.logout).toHaveBeenCalledOnce();
    expect(screen.queryByText('Administradora Univalle')).toBeNull();
    expect(screen.queryByText('Sede Central')).toBeNull();
    expect(screen.getByLabelText('Usuario o Correo')).toHaveValue('');
  });

  it('muestra la campana solo en las páginas del legacy (showBell) y no sugiere login solo por correo', async () => {
    const { unmount } = render(<App api={makeApi()} />);
    const identifier = await screen.findByLabelText('Usuario o Correo');
    expect(identifier).toHaveAttribute('placeholder', 'Ej: admin');
    unmount();

    const api = makeApi({
      currentSession: vi.fn().mockResolvedValue(NORMAL),
      listUsers: vi.fn().mockResolvedValue(EMPTY_USERS),
    });
    const view = render(<App api={api} />);
    await screen.findByRole('heading', { name: 'Dashboard de Gestión' });
    expect(screen.getByRole('button', { name: 'Notificaciones' })).toBeInTheDocument();
    view.unmount();

    window.history.replaceState({}, '', '/Users/Index');
    render(<App api={api} />);
    expect(await screen.findByRole('heading', { name: 'Control de Usuarios' })).toBeInTheDocument();
    expect(screen.queryByRole('button', { name: 'Notificaciones' })).toBeNull();
  });
});
