import { beforeEach, describe, expect, it, vi } from 'vitest';
import { render, screen, waitFor } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { ApiClientError } from '@lu/api-client';
import {
  SiteRole,
  SiteState,
  type ActiveSiteSession,
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

const SITE_CONTEXT: SiteRequestContext = {
  correlationId: '33333333-3333-4333-8333-333333333333',
  userId: ACTIVE_SESSION.userId,
  siteId: SITE_ID,
  siteName: 'Sede Central',
  siteRole: SiteRole.Administrador,
  globalRole: null,
};

const UNAUTHORIZED = new ApiClientError(401, {
  success: false,
  error: { code: 'INVALID_CREDENTIALS', message: 'Invalid credentials.' },
});

function makeApi(overrides: Partial<AuthApi> = {}): AuthApi {
  return {
    session: vi.fn().mockRejectedValue(UNAUTHORIZED),
    login: vi.fn().mockResolvedValue(ACTIVE_SESSION),
    setActiveSite: vi.fn().mockResolvedValue(ACTIVE_SESSION),
    siteContext: vi.fn().mockResolvedValue(SITE_CONTEXT),
    logout: vi.fn().mockResolvedValue(undefined),
    ...overrides,
  };
}

describe('Aplicación React autenticada', () => {
  beforeEach(() => {
    window.history.replaceState({}, '', '/');
  });

  it('reproduce los campos y textos del login Razor', async () => {
    render(<App api={makeApi()} />);

    expect(await screen.findByRole('heading', { name: 'Ingreso al Sistema' })).toBeInTheDocument();
    expect(screen.getByLabelText('Usuario o Correo')).toHaveAttribute('autocomplete', 'username');
    expect(screen.getByLabelText('Contraseña')).toHaveAttribute('autocomplete', 'current-password');
    expect(screen.getByLabelText('Recordarme en este equipo')).toBeInTheDocument();
    expect(
      screen.getByText('Portal de Gestión Administrativa de Laboratorios'),
    ).toBeInTheDocument();
  });

  it('completa login, consulta el contexto y presenta el shell institucional', async () => {
    const api = makeApi();
    const user = userEvent.setup();
    render(<App api={api} />);
    await screen.findByRole('heading', { name: 'Ingreso al Sistema' });

    await user.type(screen.getByLabelText('Usuario o Correo'), 'admin@univalle.edu');
    await user.type(screen.getByLabelText('Contraseña'), 'password-not-logged');
    await user.click(screen.getByLabelText('Recordarme en este equipo'));
    await user.click(screen.getByRole('button', { name: 'INICIAR SESIÓN' }));

    expect(
      await screen.findByRole('heading', { name: 'Dashboard de Gestión' }),
    ).toBeInTheDocument();
    expect(screen.getByRole('navigation', { name: 'Navegación principal' })).toBeInTheDocument();
    expect(screen.getByText('Bienvenido, Administradora')).toBeInTheDocument();
    expect(screen.getAllByText('Sede Central').length).toBeGreaterThan(0);
    expect(api.login).toHaveBeenCalledWith({
      email: 'admin@univalle.edu',
      password: 'password-not-logged',
      rememberMe: true,
    });
    expect(api.siteContext).toHaveBeenCalledOnce();
  });

  it('obliga a seleccionar una membresía antes de abrir el shell', async () => {
    const globalSession: ActiveSiteSession = {
      ...ACTIVE_SESSION,
      activeSiteId: null,
      activeSiteName: null,
      memberships: [
        ACTIVE_SESSION.memberships[0]!,
        {
          siteId: OTHER_SITE_ID,
          siteName: 'Sede Norte',
          role: SiteRole.Supervisor,
          state: SiteState.Active,
        },
      ],
    };
    const api = makeApi({ session: vi.fn().mockResolvedValue(globalSession) });
    const user = userEvent.setup();
    render(<App api={api} />);

    expect(await screen.findByRole('heading', { name: 'Selecciona una sede' })).toBeInTheDocument();
    expect(
      screen.queryByRole('navigation', { name: 'Navegación principal' }),
    ).not.toBeInTheDocument();
    await user.click(screen.getByRole('button', { name: /Sede Central\s*Administrador/ }));

    await waitFor(() => expect(api.setActiveSite).toHaveBeenCalledWith({ activeSiteId: SITE_ID }));
    expect(
      await screen.findByRole('navigation', { name: 'Navegación principal' }),
    ).toBeInTheDocument();
  });

  it('navega por rutas del sidebar y actualiza breadcrumb sin recargar', async () => {
    const user = userEvent.setup();
    render(<App api={makeApi({ session: vi.fn().mockResolvedValue(ACTIVE_SESSION) })} />);
    await screen.findByRole('heading', { name: 'Dashboard de Gestión' });

    await user.click(screen.getByRole('link', { name: 'Ver' }));

    expect(screen.getByRole('heading', { name: 'Ver' })).toBeInTheDocument();
    expect(screen.getByRole('navigation', { name: 'Migas de pan' })).toHaveTextContent('InicioVer');
    expect(window.location.pathname).toBe('/AssetView/Index');
  });

  it('oculta las rutas administrativas a un Supervisor', async () => {
    window.history.replaceState({}, '', '/Equipment/Index');
    const supervisorSession: ActiveSiteSession = {
      ...ACTIVE_SESSION,
      displayName: 'Supervisora Univalle',
      email: 'supervisor@univalle.edu',
      memberships: [{ ...ACTIVE_SESSION.memberships[0]!, role: SiteRole.Supervisor }],
    };
    const supervisorContext: SiteRequestContext = {
      ...SITE_CONTEXT,
      siteRole: SiteRole.Supervisor,
    };
    render(
      <App
        api={makeApi({
          session: vi.fn().mockResolvedValue(supervisorSession),
          siteContext: vi.fn().mockResolvedValue(supervisorContext),
        })}
      />,
    );

    await screen.findByRole('heading', { name: 'Dashboard de Gestión' });
    expect(screen.queryByText('Gestiones')).not.toBeInTheDocument();
    expect(screen.queryByText('Equipo')).not.toBeInTheDocument();
    expect(screen.getByText('Solicitudes de Servicio (L-7)')).toBeInTheDocument();
    expect(screen.getByText('Solicitudes de Adquisición')).toBeInTheDocument();
    await waitFor(() => expect(window.location.pathname).toBe('/'));
  });

  it('cierra la sesión desde el menú de perfil y vuelve al formulario', async () => {
    const api = makeApi({ session: vi.fn().mockResolvedValue(ACTIVE_SESSION) });
    const user = userEvent.setup();
    render(<App api={api} />);
    await screen.findByRole('heading', { name: 'Dashboard de Gestión' });

    await user.click(screen.getByRole('button', { name: /Administradora Univalle/ }));
    await user.click(screen.getByRole('button', { name: 'Cerrar Sesión' }));

    expect(await screen.findByRole('heading', { name: 'Ingreso al Sistema' })).toBeInTheDocument();
    expect(api.logout).toHaveBeenCalledOnce();
  });
});
