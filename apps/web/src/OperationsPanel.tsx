import { useEffect, useState, type ReactElement } from 'react';
import type { DashboardSummary, ManagementPage, ManagementPlanPage } from '@lu/contracts';

export type OperationsApi = {
  dashboard(query?: { readonly currentPage?: number }): Promise<DashboardSummary>;
  managements(query?: { readonly currentPage?: number }): Promise<ManagementPage>;
  managementPlans(
    id: number,
    query?: { readonly currentPage?: number },
  ): Promise<ManagementPlanPage>;
};

const TITLES: Record<string, { kicker: string; title: string; description: string; icon: string }> =
  {
    '/LaboratoryOperations/Index': {
      kicker: 'Operación de laboratorios',
      title: 'Resumen operativo',
      description: 'Indicadores y gestiones de la sede activa, consultados desde PostgreSQL.',
      icon: 'mdi mdi-view-dashboard',
    },
    '/LaboratoryOperations/Planificacion': {
      kicker: 'Paso 1 · Planificación',
      title: 'Planificación de activos',
      description: 'Activos que forman parte de la gestión activa y su avance en el wizard.',
      icon: 'mdi mdi-clipboard-text-outline',
    },
    '/LaboratoryOperations/Requerimientos': {
      kicker: 'Paso 2 · Requerimientos',
      title: 'Requerimientos',
      description: 'La operación L-7 concentra las solicitudes asociadas a unidades planificadas.',
      icon: 'mdi mdi-format-list-checks',
    },
    '/LaboratoryOperations/Disponibilidad': {
      kicker: 'Paso 3 · Disponibilidad',
      title: 'Disponibilidad',
      description:
        'La disponibilidad se deriva del inventario físico y del estado actual de cada unidad.',
      icon: 'mdi mdi-check-circle-outline',
    },
    '/LaboratoryOperations/L3': {
      kicker: 'Paso 4 · L-3',
      title: 'Salidas y devoluciones',
      description: 'Las salidas se gestionan en el módulo L-3 y actualizan el estado del activo.',
      icon: 'mdi mdi-clipboard-arrow-left-outline',
    },
    '/LaboratoryOperations/L5': {
      kicker: 'Paso 5 · L-5',
      title: 'Incidentes y correctivos',
      description: 'Los incidentes se expresan como solicitudes correctivas de la gestión activa.',
      icon: 'mdi mdi-alert-outline',
    },
    '/LaboratoryOperations/Demanda': {
      kicker: 'Planificación futura',
      title: 'Demanda',
      description:
        'La demanda se calcula cuando existen salidas y movimientos operacionales en la sede.',
      icon: 'mdi mdi-chart-bar',
    },
    '/LaboratoryOperations/Calendario': {
      kicker: 'Disponibilidad',
      title: 'Calendario de laboratorios',
      description:
        'El calendario se alimentará de fechas planificadas reales; no se muestran eventos simulados.',
      icon: 'mdi mdi-calendar-range',
    },
  };

export function OperationsPanel({
  api,
  route,
}: {
  readonly api: OperationsApi;
  readonly route: string;
}): ReactElement {
  const [summary, setSummary] = useState<DashboardSummary | null>(null);
  const [managements, setManagements] = useState<ManagementPage | null>(null);
  const [plans, setPlans] = useState<ManagementPlanPage | null>(null);
  const [error, setError] = useState<string | null>(null);
  const info = TITLES[route] ?? TITLES['/LaboratoryOperations/Index']!;
  useEffect(() => {
    let cancelled = false;
    void Promise.all([api.dashboard({ currentPage: 1 }), api.managements({ currentPage: 1 })])
      .then(([dashboard, managementPage]) => {
        if (cancelled) return;
        setSummary(dashboard);
        setManagements(managementPage);
        const active =
          managementPage.items.find((item) => item.status === 0) ?? managementPage.items[0];
        if (active !== undefined)
          void api
            .managementPlans(active.id, { currentPage: 1 })
            .then((value) => {
              if (!cancelled) setPlans(value);
            })
            .catch(() => undefined);
      })
      .catch(() => {
        if (!cancelled) setError('No fue posible cargar la operación de la sede activa.');
      });
    return () => {
      cancelled = true;
    };
  }, [api]);
  const active = managements?.items.find((item) => item.status === 0) ?? managements?.items[0];
  return (
    <section className="catalog-panel operations-panel" aria-label={info.title}>
      <div className="catalog-heading">
        <div>
          <span className="welcome-kicker">{info.kicker}</span>
          <h2>{info.title}</h2>
          <p>{info.description}</p>
        </div>
        <i className={`${info.icon} operations-heading-icon`} aria-hidden="true" />
      </div>
      {error ? (
        <div className="alert alert-danger" role="alert">
          {error}
        </div>
      ) : null}
      <div className="dashboard-grid">
        <article className="dashboard-card">
          <span className="dashboard-card-icon">
            <i className="mdi mdi-cube-outline" />
          </span>
          <div>
            <small>Activos planificados</small>
            <strong>{summary?.metrics.totalAssets ?? plans?.totalCount ?? 0}</strong>
          </div>
        </article>
        <article className="dashboard-card">
          <span className="dashboard-card-icon">
            <i className="mdi mdi-progress-clock" />
          </span>
          <div>
            <small>En proceso</small>
            <strong>{summary?.metrics.l8 ?? 0}</strong>
          </div>
        </article>
        <article className="dashboard-card">
          <span className="dashboard-card-icon">
            <i className="mdi mdi-alert-circle-outline" />
          </span>
          <div>
            <small>Vencidos</small>
            <strong>{summary?.metrics.overdueAssets ?? 0}</strong>
          </div>
        </article>
      </div>
      <div className="catalog-table-wrap">
        <h3>{active ? `Gestión activa · ${active.code}` : 'Sin gestión activa'}</h3>
        {active && plans?.items.length ? (
          <table className="catalog-table">
            <thead>
              <tr>
                <th>Activo</th>
                <th>Laboratorio</th>
                <th>Fase</th>
                <th>Estado</th>
              </tr>
            </thead>
            <tbody>
              {plans.items.slice(0, 20).map((item) => (
                <tr key={item.id}>
                  <td>
                    <strong>{item.inventoryNumber ?? 'Sin inventario'}</strong>
                    <small>{item.equipmentName ?? 'Equipo'}</small>
                  </td>
                  <td>{item.laboratoryName ?? 'Sin laboratorio'}</td>
                  <td>L-{item.currentPhase}</td>
                  <td>
                    <span className={`status-pill status-${item.currentState}`}>
                      {item.isDraft ? 'Borrador' : `Estado ${item.currentState}`}
                    </span>
                  </td>
                </tr>
              ))}
            </tbody>
          </table>
        ) : (
          <div className="empty-state">
            <i className={info.icon} />
            <h3>{active ? 'No hay activos para mostrar' : 'No hay gestión activa'}</h3>
            <p>La sede activa no tiene registros operacionales disponibles para esta vista.</p>
          </div>
        )}
      </div>
    </section>
  );
}
