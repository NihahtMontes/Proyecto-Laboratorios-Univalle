import { useId, type FormEvent, type ReactElement, type ReactNode } from 'react';

/**
 * MIG-001 F6 visual primitives shared by Pages/Users/* and _PersonsTable:
 * markup and Bootstrap/NiceAdmin classes copied from the legacy Razor pages.
 */

/** `<ul class="pagination justify-content-center">` from Users/Index.cshtml. */
export function LegacyPagination({
  pageIndex,
  totalPages,
  label,
  onPage,
}: {
  readonly pageIndex: number;
  readonly totalPages: number;
  readonly label: string;
  readonly onPage: (page: number) => void;
}): ReactElement | null {
  if (totalPages <= 1) return null;
  const pages = Array.from({ length: totalPages }, (_, index) => index + 1);
  return (
    <nav aria-label={label}>
      <ul className="pagination justify-content-center">
        {pageIndex > 1 ? (
          <li className="page-item">
            <button type="button" className="page-link" onClick={() => onPage(pageIndex - 1)}>
              Anterior
            </button>
          </li>
        ) : null}
        {pages.map((page) => (
          <li className={`page-item${page === pageIndex ? ' active' : ''}`} key={page}>
            <button
              type="button"
              className="page-link"
              aria-current={page === pageIndex ? 'page' : undefined}
              onClick={() => onPage(page)}
            >
              {page}
            </button>
          </li>
        ))}
        {pageIndex < totalPages ? (
          <li className="page-item">
            <button type="button" className="page-link" onClick={() => onPage(pageIndex + 1)}>
              Siguiente
            </button>
          </li>
        ) : null}
      </ul>
    </nav>
  );
}

/** "Smart Index Zone": search input group + filter row + live count. */
export function SmartIndexZone({
  searchLabel,
  placeholder,
  value,
  active,
  onChange,
  onSubmit,
  count,
  children,
}: {
  readonly searchLabel: string;
  readonly placeholder: string;
  readonly value: string;
  readonly active: boolean;
  readonly onChange: (value: string) => void;
  readonly onSubmit: () => void;
  readonly count: ReactNode;
  readonly children?: ReactNode;
}): ReactElement {
  const id = useId();
  return (
    <div className="bg-light p-3 mb-4 rounded border-left border-info">
      <form
        role="search"
        onSubmit={(event: FormEvent<HTMLFormElement>) => {
          event.preventDefault();
          onSubmit();
        }}
      >
        <label htmlFor={id} className="sr-only">
          {searchLabel}
        </label>
        <div className="input-group mb-3">
          <div className="input-group-prepend">
            <button
              type="submit"
              className="input-group-text bg-white border-right-0"
              aria-label="Buscar"
              title="Buscar"
            >
              <i className="fas fa-search text-muted" aria-hidden="true" />
            </button>
          </div>
          <input
            id={id}
            type="search"
            className={`form-control border-left-0${active ? ' border-info' : ''}`}
            placeholder={placeholder}
            autoComplete="off"
            maxLength={200}
            value={value}
            onChange={(event) => onChange(event.target.value)}
          />
        </div>
        <div className="d-flex flex-wrap align-items-center border-top pt-3">
          {children}
          <span className="small text-muted ml-auto mb-2" aria-live="polite">
            {count}
          </span>
        </div>
      </form>
    </div>
  );
}

/** Filter `<select class="custom-select btn-rounded ...">`; highlighted while a value is set. */
export function FilterSelect({
  label,
  value,
  onChange,
  children,
}: {
  readonly label: string;
  readonly value: string;
  readonly onChange: (value: string) => void;
  readonly children: ReactNode;
}): ReactElement {
  return (
    <select
      className={`custom-select btn-rounded w-auto mw-100 mr-2 mb-2${value !== '' ? ' border-info text-info font-weight-bold' : ''}`}
      aria-label={label}
      value={value}
      onChange={(event) => onChange(event.target.value)}
    >
      {children}
    </select>
  );
}

/** `badge badge-pill ... px-3 py-1 font-weight-bold`: success only for the active label. */
export function StatusBadge({
  label,
  tone,
}: {
  readonly label: string;
  readonly tone: 'success' | 'danger' | 'warning' | 'info' | 'secondary';
}): ReactElement {
  return (
    <span className={`badge badge-pill badge-${tone} px-3 py-1 font-weight-bold`}>{label}</span>
  );
}
