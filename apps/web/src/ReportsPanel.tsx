import { useEffect, useState, type ReactElement } from 'react';
import type {
  CatalogPage,
  Laboratory,
  ManagementPage,
  ReportKind,
  ReportManifestItem,
} from '@lu/contracts';

export type ReportsApi = {
  reportManifest(): Promise<readonly ReportManifestItem[]>;
  downloadReport(kind: ReportKind, query?: Record<string, number | undefined>): Promise<Blob>;
  managements(query?: { readonly currentPage?: number }): Promise<ManagementPage>;
  laboratories(query?: { readonly currentPage?: number }): Promise<CatalogPage<Laboratory>>;
};

function parseId(value: string): number | undefined {
  if (value.trim() === '' || !/^\d+$/.test(value)) return undefined;
  const result = Number(value);
  return Number.isSafeInteger(result) && result > 0 ? result : undefined;
}

export function ReportsPanel({ api }: { readonly api: ReportsApi }): ReactElement {
  const [manifest, setManifest] = useState<readonly ReportManifestItem[]>([]);
  const [managements, setManagements] = useState<ManagementPage | null>(null);
  const [laboratories, setLaboratories] = useState<readonly Laboratory[]>([]);
  const [kind, setKind] = useState<ReportKind>('l6');
  const [managementId, setManagementId] = useState<number | ''>('');
  const [laboratoryId, setLaboratoryId] = useState<number | ''>('');
  const [recordId, setRecordId] = useState('');
  const [busy, setBusy] = useState(false);
  const [feedback, setFeedback] = useState<string | null>(null);
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    let cancelled = false;
    void Promise.all([
      api.reportManifest(),
      api.managements({ currentPage: 1 }),
      api.laboratories({ currentPage: 1 }),
    ])
      .then(([available, managementPage, laboratoryPage]) => {
        if (cancelled) return;
        setManifest(available);
        setManagements(managementPage);
        setLaboratories(laboratoryPage.items);
        setManagementId(managementPage.items.find((item) => item.status === 0)?.id ?? '');
        setLaboratoryId(laboratoryPage.items[0]?.id ?? '');
      })
      .catch(() => {
        if (!cancelled) setError('No fue posible cargar el centro de reportes.');
      });
    return () => {
      cancelled = true;
    };
  }, [api]);

  async function download(): Promise<void> {
    setBusy(true);
    setFeedback(null);
    setError(null);
    const query: Record<string, number | undefined> = {};
    if (kind === 'l6' || kind === 'l48') {
      query.managementId = managementId === '' ? undefined : managementId;
      query.laboratoryId = laboratoryId === '' ? undefined : laboratoryId;
    } else if (kind === 'l8') {
      query.planId = parseId(recordId);
    } else if (kind === 'l3') {
      query.departureId = parseId(recordId);
    } else {
      query.requestId = parseId(recordId);
    }
    try {
      const blob = await api.downloadReport(kind, query);
      const url = URL.createObjectURL(blob);
      const anchor = document.createElement('a');
      anchor.href = url;
      anchor.download = `${kind}-laboratorios.xlsx`;
      document.body.appendChild(anchor);
      anchor.click();
      anchor.remove();
      window.setTimeout(() => URL.revokeObjectURL(url), 0);
      setFeedback('Reporte generado con los datos reales de la sede activa.');
    } catch {
      setError('No fue posible generar el reporte con los identificadores seleccionados.');
    } finally {
      setBusy(false);
    }
  }

  const selected = manifest.find((item) => item.kind === kind);
  return (
    <section className="catalog-panel reports-panel" aria-label="Centro de reportes oficiales">
      <div className="catalog-heading">
        <div>
          <span className="welcome-kicker">Reportes oficiales</span>
          <h2>Centro de reportes</h2>
          <p>
            Las descargas usan las plantillas institucionales y los datos PostgreSQL de la sede
            activa.
          </p>
        </div>
        <i
          className="mdi mdi-file-document-multiple-outline operations-heading-icon"
          aria-hidden="true"
        />
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
      <div className="catalog-form-grid reports-form">
        <label>
          Tipo de reporte
          <select
            className="form-control"
            value={kind}
            onChange={(event) => setKind(event.target.value as ReportKind)}
          >
            {manifest.map((item) => (
              <option key={item.kind} value={item.kind}>
                {item.label}
              </option>
            ))}
          </select>
        </label>
        {kind === 'l6' || kind === 'l48' ? (
          <>
            <label>
              Gestión
              <select
                className="form-control"
                value={managementId}
                onChange={(event) => setManagementId(parseId(event.target.value) ?? '')}
              >
                <option value="">Selecciona una gestión</option>
                {managements?.items.map((item) => (
                  <option key={item.id} value={item.id}>
                    {item.code}
                  </option>
                ))}
              </select>
            </label>
            <label>
              Laboratorio
              <select
                className="form-control"
                value={laboratoryId}
                onChange={(event) => setLaboratoryId(parseId(event.target.value) ?? '')}
              >
                <option value="">Selecciona un laboratorio</option>
                {laboratories.map((item) => (
                  <option key={item.id} value={item.id}>
                    {item.code} · {item.name}
                  </option>
                ))}
              </select>
            </label>
          </>
        ) : (
          <label>
            {kind === 'l8' ? 'ID del plan' : kind === 'l3' ? 'ID de salida' : 'ID de solicitud'}
            <input
              className="form-control"
              inputMode="numeric"
              value={recordId}
              onChange={(event) => setRecordId(event.target.value)}
            />
          </label>
        )}
      </div>
      <div className="catalog-detail">
        <strong>{selected?.label ?? 'Reporte'}</strong>
        <small>Formato Excel institucional (.xlsx). La generación se ejecuta server-side.</small>
        <button
          type="button"
          className="btn btn-primary"
          disabled={busy || selected?.available !== true}
          onClick={() => void download()}
        >
          {busy ? 'Generando…' : 'Descargar'}
        </button>
      </div>
    </section>
  );
}
