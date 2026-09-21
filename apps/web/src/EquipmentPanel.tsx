import { useCallback, useEffect, useState, type FormEvent, type ReactElement } from 'react';
import { ApiClientError } from '@lu/api-client';
import type {
  Country,
  City,
  CreateEquipmentInput,
  EquipmentRecord,
  UpdateEquipmentInput,
} from '@lu/contracts';

export type EquipmentApi = {
  equipment(query?: {
    readonly currentPage?: number;
    readonly searchTerm?: string;
  }): Promise<{ readonly items: readonly EquipmentRecord[]; readonly totalCount: number }>;
  countries(): Promise<{ readonly items: readonly Country[] }>;
  cities(): Promise<{ readonly items: readonly City[] }>;
  createEquipment(input: CreateEquipmentInput): Promise<EquipmentRecord>;
  updateEquipment(id: number, input: UpdateEquipmentInput): Promise<EquipmentRecord>;
  deleteEquipment(id: number): Promise<void>;
};

export function EquipmentPanel({ api }: { readonly api: EquipmentApi }): ReactElement {
  const [items, setItems] = useState<readonly EquipmentRecord[]>([]);
  const [countries, setCountries] = useState<readonly Country[]>([]);
  const [cities, setCities] = useState<readonly City[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [feedback, setFeedback] = useState<string | null>(null);
  const [editingId, setEditingId] = useState<number | null>(null);
  const [category, setCategory] = useState<0 | 1 | 2>(0);
  const [catalogCode, setCatalogCode] = useState('');
  const [name, setName] = useState('');
  const [brand, setBrand] = useState('');
  const [model, setModel] = useState('');
  const [countryId, setCountryId] = useState<number | ''>('');
  const [cityId, setCityId] = useState<number | ''>('');
  const [usefulLifeYears, setUsefulLifeYears] = useState('');
  const [description, setDescription] = useState('');
  const [notes, setNotes] = useState('');

  const load = useCallback(async () => {
    setLoading(true);
    try {
      const [page, countryPage, cityPage] = await Promise.all([
        api.equipment({ currentPage: 1 }),
        api.countries(),
        api.cities(),
      ]);
      setItems(page.items);
      setCountries(countryPage.items);
      setCities(cityPage.items);
      setError(null);
    } catch {
      setError('No fue posible cargar los equipos de la sede.');
    } finally {
      setLoading(false);
    }
  }, [api]);
  useEffect(() => {
    void load();
  }, [load]);

  function reset(): void {
    setEditingId(null);
    setCategory(0);
    setCatalogCode('');
    setName('');
    setBrand('');
    setModel('');
    setCountryId('');
    setCityId('');
    setUsefulLifeYears('');
    setDescription('');
    setNotes('');
  }
  function edit(item: EquipmentRecord): void {
    setEditingId(item.id);
    setCategory(item.category);
    setCatalogCode(item.catalogCode ?? '');
    setName(item.name);
    setBrand(item.brand ?? '');
    setModel(item.model ?? '');
    setCountryId(item.countryId ?? '');
    setCityId(item.cityId ?? '');
    setUsefulLifeYears(item.usefulLifeYears === null ? '' : String(item.usefulLifeYears));
    setDescription(item.description ?? '');
    setNotes(item.notes.join('\n'));
  }
  async function submit(event: FormEvent<HTMLFormElement>): Promise<void> {
    event.preventDefault();
    setError(null);
    setFeedback(null);
    const input: CreateEquipmentInput = {
      category,
      catalogCode,
      name,
      brand: brand || null,
      model: model || null,
      countryId: countryId === '' ? null : countryId,
      cityId: cityId === '' ? null : cityId,
      usefulLifeYears: usefulLifeYears === '' ? null : Number(usefulLifeYears),
      description: description || null,
      notes: notes
        .split('\n')
        .map((value) => value.trim())
        .filter(Boolean),
    };
    try {
      if (editingId === null) {
        await api.createEquipment(input);
        setFeedback('Equipo registrado correctamente.');
      } else {
        await api.updateEquipment(editingId, { ...input, status: 0 });
        setFeedback('Equipo actualizado correctamente.');
      }
      reset();
      await load();
    } catch (caught) {
      setError(
        caught instanceof ApiClientError
          ? caught.failure.error.message
          : 'No fue posible guardar el equipo.',
      );
    }
  }
  async function remove(id: number): Promise<void> {
    if (!window.confirm('¿Confirmas la baja lógica de este equipo?')) return;
    try {
      await api.deleteEquipment(id);
      setFeedback('Equipo dado de baja correctamente.');
      if (editingId === id) reset();
      await load();
    } catch {
      setError('No fue posible dar de baja el equipo.');
    }
  }

  return (
    <section className="catalog-panel" aria-label="Catálogo de equipos">
      <div className="catalog-heading">
        <div>
          <span className="welcome-kicker">Definiciones de activos</span>
          <h2>Equipos</h2>
          <p>Las unidades físicas y Kardex se incorporan en la siguiente vertical.</p>
        </div>
        <span className="catalog-count">{items.length} visibles</span>
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
      <div className="catalog-layout">
        <form className="catalog-form" onSubmit={(event) => void submit(event)}>
          <h3>{editingId === null ? 'Nuevo equipo' : 'Editar equipo'}</h3>
          <label>
            Categoría
            <select
              className="form-control"
              value={category}
              onChange={(event) => setCategory(Number(event.target.value) as 0 | 1 | 2)}
            >
              <option value={0}>Equipo</option>
              <option value={1}>Utensilio</option>
              <option value={2}>Otro</option>
            </select>
          </label>
          <label>
            Código catálogo
            <input
              className="form-control"
              value={catalogCode}
              onChange={(event) => setCatalogCode(event.target.value)}
              maxLength={30}
              required
            />
          </label>
          <label>
            Nombre
            <input
              className="form-control"
              value={name}
              onChange={(event) => setName(event.target.value)}
              maxLength={200}
              required
            />
          </label>
          <label>
            Marca
            <input
              className="form-control"
              value={brand}
              onChange={(event) => setBrand(event.target.value)}
              maxLength={100}
            />
          </label>
          <label>
            Modelo
            <input
              className="form-control"
              value={model}
              onChange={(event) => setModel(event.target.value)}
              maxLength={100}
            />
          </label>
          <label>
            País
            <select
              className="form-control"
              value={countryId}
              onChange={(event) => {
                setCountryId(event.target.value === '' ? '' : Number(event.target.value));
                setCityId('');
              }}
            >
              <option value="">Sin país</option>
              {countries
                .filter((item) => item.status === 0)
                .map((item) => (
                  <option key={item.id} value={item.id}>
                    {item.name}
                  </option>
                ))}
            </select>
          </label>
          <label>
            Ciudad
            <select
              className="form-control"
              value={cityId}
              onChange={(event) =>
                setCityId(event.target.value === '' ? '' : Number(event.target.value))
              }
            >
              <option value="">Sin ciudad</option>
              {cities
                .filter(
                  (item) => item.status === 0 && (countryId === '' || item.countryId === countryId),
                )
                .map((item) => (
                  <option key={item.id} value={item.id}>
                    {item.name}
                  </option>
                ))}
            </select>
          </label>
          <label>
            Vida útil (años)
            <input
              className="form-control"
              type="number"
              min={0}
              max={100}
              value={usefulLifeYears}
              onChange={(event) => setUsefulLifeYears(event.target.value)}
            />
          </label>
          <label>
            Descripción
            <textarea
              className="form-control"
              value={description}
              onChange={(event) => setDescription(event.target.value)}
              maxLength={2000}
            />
          </label>
          <label>
            Notas (una por línea)
            <textarea
              className="form-control"
              value={notes}
              onChange={(event) => setNotes(event.target.value)}
              maxLength={5000}
            />
          </label>
          <div className="catalog-form-actions">
            <button className="btn btn-primary" type="submit">
              Guardar
            </button>
            {editingId !== null ? (
              <button className="btn btn-light" type="button" onClick={() => reset()}>
                Cancelar
              </button>
            ) : null}
          </div>
        </form>
        <div className="catalog-table-wrap">
          {loading ? (
            <p className="catalog-empty">Cargando equipos…</p>
          ) : items.length === 0 ? (
            <p className="catalog-empty">No hay equipos registrados en esta sede.</p>
          ) : (
            <table className="catalog-table">
              <thead>
                <tr>
                  <th>Código</th>
                  <th>Equipo</th>
                  <th>Categoría</th>
                  <th>Origen</th>
                  <th>Acciones</th>
                </tr>
              </thead>
              <tbody>
                {items.map((item) => (
                  <tr key={item.id}>
                    <td>{item.catalogCode ?? '—'}</td>
                    <td>
                      {item.name}
                      <small>{[item.brand, item.model].filter(Boolean).join(' · ')}</small>
                    </td>
                    <td>
                      {item.category === 0 ? 'Equipo' : item.category === 1 ? 'Utensilio' : 'Otro'}
                    </td>
                    <td>{item.cityName ?? item.countryName ?? '—'}</td>
                    <td className="catalog-actions">
                      <button type="button" onClick={() => edit(item)}>
                        Editar
                      </button>
                      <button type="button" onClick={() => void remove(item.id)}>
                        Baja
                      </button>
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
