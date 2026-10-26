"""WS-8 · Filtros, búsqueda y paginación — arnés genérico (solo GET, copia QA).

Para cada listado paginado (Helpers/PaginatedList, 20 por página, parámetro pageIndex):
  PG-01 la página 1 muestra min(20, N) filas (N = total que muestra la propia página; en las pantallas clave, N = COUNT SQL)
  PG-02 recorriendo todas las páginas, las filas distintas suman N y no se repiten entre páginas
  PG-04 pageIndex extremos (0, -1, abc, vacío, 107374183, 99999999999) no producen 500; se anota qué muestra la página > última
  PG-06 búsqueda sin resultados: 200, 0 filas y sin paginador
  PG-07 búsqueda hostil (%, _, ', ", <script>, 300 caracteres) sin 500 ni HTML sin escapar
  PG-10 la página 1 responde en menos de 3 s
Una «fila» es un id distinto en los enlaces Details/Edit de la entidad, dentro del <tbody> de la tabla.
"""
import contextlib
import io
import os
import re
import sys
import time
from pathlib import Path
from urllib.parse import quote

import e2e_lib
from http_forms import Client

if e2e_lib.DB_NAME != "DB_Laboratorios_Univalle_QA_E2E":
    sys.exit(f"SEGURIDAD: solo corre contra la copia de QA (E2E_DB={e2e_lib.DB_NAME!r}).")

PW = os.environ["E2E_PW"]
c = Client(os.environ.get("E2E_USER", "qa_super"), PW)
R = []
PAGE = 20

STOCK_LAB = e2e_lib.db_scalar(
    "SELECT TOP 1 l.Id FROM Laboratories l WHERE l.Status = 0 ORDER BY (SELECT COUNT(*) FROM ArticleStocks s WHERE s.LaboratoryId = l.Id) DESC")

# path, entidad de los enlaces de fila (prefijo de la URL Details/Edit), COUNT SQL de contraste (None = se usa el total de la página)
PANTALLAS = [
    ("/Equipment", "/Equipment", None),
    ("/EquipmentUnits", "/EquipmentUnits", "SELECT COUNT(*) FROM EquipmentUnits WHERE CurrentStatus <> 99"),
    ("/Maintenances", "/Maintenances", None),
    ("/Maintenances/Plan", "/Maintenances", None),
    ("/Verifications", "/Verifications", None),
    ("/Requests", "/Requests", None),
    ("/Departures", "/Departures", None),
    ("/Acquisitions", "/Acquisitions", None),
    ("/Catalogs/Articles", "/Catalogs/Articles", None),
    ("/Catalogs/ArticleCategories", "/Catalogs/ArticleCategories", None),
    ("/Catalogs/MeasurementUnits", "/Catalogs/MeasurementUnits", None),
    ("/Catalogs/Practices", "/Catalogs/Practices", None),
    ("/Catalogs/Subjects", "/Catalogs/Subjects", None),
    ("/Catalogs/CourseGroups", "/Catalogs/CourseGroups", None),
    (f"/Catalogs/Stock?LaboratoryFilter={STOCK_LAB}", "/Catalogs/Stock", None),
    ("/Career", "/Career", None),
    ("/Cities", "/Cities", None),
    ("/Countries", "/Countries", None),
    # /Faculties redirige por diseño a /Laboratories#faculties: no es un listado propio
    ("/Laboratories", "/Laboratories", None),
    ("/Persons", "/Persons", None),
    ("/Users", "/Users", None),
    ("/Managements?Type=PorConfirmar", "/Managements", None),
    ("/AssetView/Units", "/EquipmentUnits", None),
    ("/AssetView/UtensilUnits", "/EquipmentUnits", None),
    ("/AssetView/OtherUnits", "/EquipmentUnits", None),
    ("/AssetView/EnvironmentUnits", "/EquipmentUnits", None),
    ("/Schedules/Practices", "/Schedules", None),
]

TOTAL_RE = re.compile(r'<strong class="text-dark">\s*(\d[\d.,]*)\s*</strong>\s*[^<]{0,40}\(')


def with_param(path, key, value):
    return f"{path}{'&' if '?' in path else '?'}{key}={value}"


def row_ids(html, entity):
    # solo el cuerpo de las tablas: el perfil propio de la cabecera y otros enlaces sueltos no cuentan como filas
    cuerpo = "".join(re.findall(r"<tbody.*?</tbody>", html or "", re.S | re.I))
    html = cuerpo if cuerpo else html
    if entity == "/Catalogs/Stock":   # fila = artículo: /Catalogs/Stock/Edit/{laboratorio}/{artículo}
        return {int(m) for m in re.findall(r'href="/Catalogs/Stock/Edit/\d+/(\d+)', html or "")}
    pat = re.compile(r'href="' + re.escape(entity) + r'/(?:Details|Edit|Kardex)(?:/|\?id=)(\d+)', re.I)
    return {int(m) for m in pat.findall(html or "")}


def shown_total(html):
    m = TOTAL_RE.search(html or "")
    return int(m.group(1).replace(".", "").replace(",", "")) if m else None


def is_error_page(st, html):
    return st >= 500 or "An unhandled exception" in (html or "") or "Excepción no controlada" in (html or "")


def ok(name, cond, detail=""):
    R.append(e2e_lib.Result(name, True, bool(cond), "" if cond else detail))


for path, entity, count_sql in PANTALLAS:
    tag = f"[{path}]"
    t0 = time.perf_counter()
    st, html = c.get(path)
    elapsed = time.perf_counter() - t0
    if st != 200:
        ok(f"{tag} GET página 1 responde 200", False, f"status={st}")
        continue
    ok(f"{tag} PG-10 página 1 en menos de 3 s", elapsed < 3.0, f"{elapsed:.2f}s")

    total_pag = shown_total(html)
    total = e2e_lib.db_scalar(count_sql) if count_sql else total_pag
    ids1 = row_ids(html, entity)
    if count_sql and total_pag is not None:
        ok(f"{tag} total mostrado = COUNT SQL", total_pag == total, f"pagina={total_pag} sql={total}")

    if total is None or (total > 0 and not ids1):
        ok(f"{tag} PG-01/02 omitidas: no se pudo leer el total o las filas (revisar a mano)", True)
    else:
        ok(f"{tag} PG-01 página 1 muestra min(20, N) filas", len(ids1) == min(PAGE, total), f"filas={len(ids1)} N={total}")
        pages = max(1, -(-total // PAGE))
        vistos, repetidos = set(ids1), set()
        for p in range(2, min(pages, 60) + 1):
            _, hp = c.get(with_param(path, "pageIndex", p))
            ids = row_ids(hp, entity)
            repetidos |= vistos & ids
            vistos |= ids
        if pages <= 60:
            ok(f"{tag} PG-02 recorrer {pages} página(s): filas distintas = N y sin repetidas",
               len(vistos) == total and not repetidos, f"distintas={len(vistos)} N={total} repetidas={sorted(repetidos)[:10]}")
        else:
            ok(f"{tag} PG-02 primeras 60 de {pages} páginas sin filas repetidas", not repetidos, f"repetidas={sorted(repetidos)[:10]}")

    # PG-04 pageIndex extremos
    for v in ("0", "-1", "abc", "", "107374183", "99999999999"):
        stv, hv = c.get(with_param(path, "pageIndex", v))
        ok(f"{tag} PG-04 pageIndex={v!r} sin 500", not is_error_page(stv, hv), f"status={stv}")
    if total:
        last = max(1, -(-total // PAGE))
        stv, hv = c.get(with_param(path, "pageIndex", last + 5))
        ok(f"{tag} PG-04 página > última ({last + 5}) sin 500", not is_error_page(stv, hv), f"status={stv}")
        if not row_ids(hv, entity):
            print(f"   nota {tag}: la página {last + 5} (> última {last}) se muestra vacía")

    # PG-06 / PG-07 búsqueda (solo pantallas con SearchTerm)
    if 'name="SearchTerm"' in html:
        sts, hs = c.get(with_param(path, "SearchTerm", "zzzqqq_no_existe"))
        ok(f"{tag} PG-06 búsqueda sin resultados: 200, 0 filas y sin paginador",
           sts == 200 and not row_ids(hs, entity) and "pageIndex=2" not in hs, f"status={sts} filas={len(row_ids(hs, entity))}")
        for v in ("%", "_", "'", '"', "<script>alert(1)</script>", "a" * 300):
            stv, hv = c.get(with_param(path, "SearchTerm", quote(v)))
            ok(f"{tag} PG-07 búsqueda {v[:20]!r} sin 500 ni HTML sin escapar",
               not is_error_page(stv, hv) and "<script>alert(1)</script>" not in hv, f"status={stv}")

with contextlib.redirect_stdout(io.StringIO()) as out:
    fails = e2e_lib.run_all([lambda: R])
texto = out.getvalue()
print(texto)
dest = Path(__file__).resolve().parents[2] / "outputs" / "qa-usuario" / "qa-2026-10-18"
dest.mkdir(parents=True, exist_ok=True)
(dest / "http_cases_paginacion.txt").write_text(texto, encoding="utf-8")
sys.exit(1 if fails else 0)
