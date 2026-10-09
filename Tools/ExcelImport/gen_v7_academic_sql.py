"""
V7 · Bloque académico de BaseEstructuraFinal_V7.xlsx.
Genera import_v7_academic.sql SOLO para DB_Laboratorios_Univalle_DEMO_QA.
Ejecutar el SQL con: sqlcmd -S localhost -E -C -d DB_Laboratorios_Univalle_DEMO_QA -b -f 65001 -i <sql>
(-f 65001 es obligatorio: el archivo es UTF-8 y sin esa opción sqlcmd daña las tildes).
- Solo importa el bloque académico (Practicas, ProgramacionPracticas, RequerimientosPractica,
  ExistenciasArticulos y limpieza de Articulos). Todo lo demás (L-6, L-7, L-8, L-3, etc.) sigue
  saliendo de la V4, no se reimporta desde la V7.
- Normaliza encabezados (case-insensitive) y códigos (*Codigo y Grupo) a .strip().upper().
- Maneja 24 fusiones de artículos + 1 nota L-3 eliminada (ART-REQ-00348) y 4 artículos nuevos,
  todos antes de cargar las recetas y existencias (decisión D8/D9).
- 33 filas de la semana 6 se marcan como SCHEDULE_DATE_SUSPECT (regla cronológica, no fija).
- 2 fechas posteriores al fin de la gestión → SCHEDULE_OUT_OF_MANAGEMENT.
- Una incidencia VALUE_PENDING_CONFIRMATION por grupo cuando HoraInicio/HoraFin están vacías.
- Recetas con cantidad NULL → VALUE_MISSING (11 esperados).
- Práctica: Name = LEFT(Description, 200); si excede, registra VALUE_TRUNCATED_IN_NAME.
"""
import collections
import hashlib
import io
import os
import sys
from datetime import date, datetime

import openpyxl

XLSX = r"C:\Users\monte\Downloads\BaseEstructuraFinal_V7.xlsx"
EXPECTED_SHA = "c677c7cc81d63f5e74b00daab60c214be032c5089b1876c61b9ddb66dc4320e9"
OUT = os.path.join(os.environ.get("TEMP", "."), "mm", "import_v7_academic.sql")

# Encabezados canónicos en MAYÚSCULAS para matching case-insensitive.
HEADERS = {
    "Materias": ("MATERIACODIGO", "NOMBRE", "CARRERACODIGO", "SEMESTRE", "ESTADO"),
    "Practicas": ("PRACTICACODIGO", "MATERIACODIGO", "NUMEROPRACTICA", "DESCRIPCION", "ESTADO", "OBSERVACIONES"),
    "ProgramacionPracticas": ("PROGRAMACIONCODIGO", "GESTIONCODIGO", "MATERIACODIGO", "PRACTICACODIGO",
                              "GRUPO", "DOCENTEACTORCODIGO", "LABORATORIOCODIGO", "SEMANA",
                              "FECHAEJECUCION", "HORAINICIO", "HORAFIN", "CANTESTUDIANTES",
                              "ESDEMOSTRATIVA", "FACTORMATERIA", "ESTADO"),
    "RequerimientosPractica": ("REQUERIMIENTOCODIGO", "PRACTICACODIGO", "ARTICULOCODIGO", "CANTIDAD",
                               "ESCALABLE", "ESTADOVALIDACION", "OBSERVACIONES"),
    "ExistenciasArticulos": ("LABORATORIOCODIGO", "ARTICULOCODIGO", "CANTIDADDISPONIBLE", "FECHAACTUALIZACION"),
    "Articulos": ("ARTICULOCODIGO", "NOMBRE", "CATEGORIACODIGO", "UNIDADMEDIDACODIGO", "ESTADO"),
}

# 24 fusiones (conservado ← eliminado) y 1 nota L-3 (ART-REQ-00348).
# Origen: hoja _POBLADO_REQUERIMIENTOS §C (verificado en pre-vuelo).
ARTICLE_MERGES = [
    ("ART-REQ-00242", "ART-REQ-00248"),
    ("GA-05-04-017", "GA-05-04-125"),
    ("ART-REQ-00084", "ART-REQ-00513"),
    ("ART-REQ-00494", "ART-REQ-00517"),
    ("ART-REQ-00511", "ART-REQ-00556"),
    ("ART-REQ-00017", "ART-REQ-00485"),
    ("ART-REQ-00530", "ART-REQ-00550"),
    ("ART-REQ-00059", "ART-REQ-00482"),
    ("ART-REQ-00003", "ART-REQ-00464"),
    ("ART-REQ-00171", "ART-REQ-00189"),
    ("ART-LAB-0112", "ART-LAB-0127"),
    ("ART-REQ-00100", "ART-REQ-00543"),
    ("ART-REQ-00144", "ART-REQ-00498"),
    ("ART-REQ-00092", "ART-REQ-00320"),
    ("ART-REQ-00143", "ART-REQ-00542"),
    ("ART-REQ-00487", "ART-REQ-00515"),
    ("ART-REQ-00484", "ART-REQ-00553"),
    ("ART-LAB-0041", "ART-LAB-0101"),
    ("ART-REQ-00406", "ART-REQ-00531"),
    ("ART-REQ-00478", "ART-REQ-00480"),
    ("ART-REQ-00477", "ART-REQ-00523"),
    ("ART-REQ-00105", "ART-REQ-00476"),
    ("ART-REQ-00491", "ART-REQ-00541"),
    ("ART-REQ-00462", "ART-REQ-00490"),
]
# 4 artículos nuevos (D9). CatCod / UniCod / Nombre legados.
NEW_ARTICLES = [
    ("ART-REQ-00566", "CUCHARAS MEDIDORAS", "CAT-ART-09", "UNI-01", "PorConfirmar"),
    ("ART-REQ-00567", "OLLA EXPRESS", "CAT-ART-02", "UNI-03", "PorConfirmar"),
    ("ART-REQ-00568", "SOBRE MANTEL", "CAT-ART-01", "UNI-03", "PorConfirmar"),
    ("ART-REQ-00569", "JUEGO DE CUCHARAS MEDIDORAS", "CAT-ART-05", "UNI-01", "PorConfirmar"),
]

sha = hashlib.sha256(open(XLSX, "rb").read()).hexdigest()
if sha != EXPECTED_SHA:
    sys.exit(f"Hash distinto: {sha}")
wb = openpyxl.load_workbook(XLSX, read_only=True, data_only=True)


def _header_map(name):
    it = wb[name].iter_rows(values_only=True)
    header = next(it)
    canon = HEADERS[name]
    seen = {}
    for i, h in enumerate(header):
        key = (h or "").strip().casefold()
        if not key:
            continue
        if key in seen:
            sys.exit(f"Hoja {name}: encabezado duplicado '{h}'")
        seen[key] = i
    mapping = {}
    for c in canon:
        ci = c.casefold()
        if ci not in seen:
            sys.exit(f"Hoja {name}: falta encabezado '{c}' (leídos={list(header)})")
        mapping[c] = seen[ci]
    return header, mapping


def sheet(name):
    header, mapping = _header_map(name)
    out = []
    for i, r in enumerate(wb[name].iter_rows(min_row=2, values_only=True), start=2):
        if not any(v is not None for v in r):
            continue
        d = {}
        for canon, idx in mapping.items():
            d[canon] = r[idx] if idx < len(r) else None
        d["_row"] = i
        out.append(d)
    return out


def q(v):
    if v is None or (isinstance(v, str) and v.strip() == ""):
        return "NULL"
    if isinstance(v, bool):
        return "1" if v else "0"
    if isinstance(v, (int, float)):
        return str(v)
    if isinstance(v, (date, datetime)) and not isinstance(v, datetime):
        return "'" + v.strftime("%Y-%m-%d") + "'"
    if isinstance(v, datetime):
        return "'" + v.strftime("%Y-%m-%d %H:%M:%S") + "'"
    return "N'" + str(v).strip().replace("'", "''") + "'"


def q_date(v):
    if v is None:
        return "NULL"
    if isinstance(v, datetime):
        d = v.date()
    elif isinstance(v, date):
        d = v
    else:
        return "NULL"
    return "'" + d.strftime("%Y-%m-%d") + "'"


def q_time(v):
    if v is None or v == "":
        return "NULL"
    if hasattr(v, "strftime"):
        return "'" + v.strftime("%H:%M") + "'"
    s = str(v).strip()
    if len(s) >= 5 and s[2] == ":":
        return "'" + s[:5] + "'"
    return "NULL"


def as_bool(v):
    s = str(v or "").strip().lower()
    if s in ("sí", "si", "true", "1", "x", "verdadero"):
        return "1"
    if s in ("no", "false", "0", "falso"):
        return "0"
    return "NULL"


def norm_code(v):
    """MAYÚSCULAS sin espacios en los bordes (decisión D2)."""
    if v is None:
        return ""
    return str(v).strip().upper()


def as_date(v):
    if v is None:
        return None
    if isinstance(v, datetime):
        return v.date()
    return v


SCHEDULE_STATUS = {"programada": 0, "ejecutada": 1, "reprogramada": 2, "cancelada": 3}

# ---------- Lectura y normalización ----------
materias_raw = sheet("Materias")
practices_raw = sheet("Practicas")
schedules_raw = sheet("ProgramacionPracticas")
requirements_raw = sheet("RequerimientosPractica")
stocks_raw = sheet("ExistenciasArticulos")

materias_codes = sorted({norm_code(m["MATERIACODIGO"]) for m in materias_raw if m.get("MATERIACODIGO")})
practices = []
for p in practices_raw:
    p["PRACTICACODIGO"] = norm_code(p["PRACTICACODIGO"])
    p["MATERIACODIGO"] = norm_code(p["MATERIACODIGO"])
    p["NUMEROPRACTICA"] = (str(p["NUMEROPRACTICA"]).strip().upper() if p["NUMEROPRACTICA"] is not None else "")
    p["_desc"] = (str(p["DESCRIPCION"]).strip() if p["DESCRIPCION"] is not None else "")
    practices.append(p)

schedules = []
for s in schedules_raw:
    s["PROGRAMACIONCODIGO"] = norm_code(s["PROGRAMACIONCODIGO"])
    s["GESTIONCODIGO"] = norm_code(s["GESTIONCODIGO"])
    s["MATERIACODIGO"] = norm_code(s["MATERIACODIGO"])
    s["PRACTICACODIGO"] = norm_code(s["PRACTICACODIGO"])
    s["GRUPO"] = norm_code(s["GRUPO"])
    s["DOCENTEACTORCODIGO"] = norm_code(s["DOCENTEACTORCODIGO"])
    s["LABORATORIOCODIGO"] = norm_code(s["LABORATORIOCODIGO"])
    s["_exec_date"] = as_date(s["FECHAEJECUCION"])
    s["_t1"] = s["HORAINICIO"]
    s["_t2"] = s["HORAFIN"]
    s["_has_time"] = (s["HORAINICIO"] is not None and str(s["HORAINICIO"]).strip() != "") or \
                     (s["HORAFIN"] is not None and str(s["HORAFIN"]).strip() != "")
    schedules.append(s)

requirements = []
for r in requirements_raw:
    r["REQUERIMIENTOCODIGO"] = norm_code(r["REQUERIMIENTOCODIGO"])
    r["PRACTICACODIGO"] = norm_code(r["PRACTICACODIGO"])
    r["ARTICULOCODIGO"] = norm_code(r["ARTICULOCODIGO"])
    requirements.append(r)

stocks = []
for s in stocks_raw:
    s["LABORATORIOCODIGO"] = norm_code(s["LABORATORIOCODIGO"])
    s["ARTICULOCODIGO"] = norm_code(s["ARTICULOCODIGO"])
    # Mantener solo las que tienen laboratorio y cantidad (decisión D4).
    if s["LABORATORIOCODIGO"] and s["CANTIDADDISPONIBLE"] is not None:
        stocks.append(s)

# ---------- Pre-vuelo (verificaciones pedidas por T1 y T2) ----------
assert len(materias_raw) == 19, f"Materias={len(materias_raw)} ≠ 19"
assert len(practices) == 196, f"Practices={len(practices)} ≠ 196"
assert len(schedules) == 422, f"Schedules={len(schedules)} ≠ 422"
assert len(requirements) == 2111, f"Requirements={len(requirements)} ≠ 2111"
assert len(stocks_raw) == 1063, f"Stocks leídos={len(stocks_raw)} ≠ 1063"
assert len(stocks) == 220, f"Stocks con datos={len(stocks)} ≠ 220"
assert len({(s["GESTIONCODIGO"], s["MATERIACODIGO"], s["GRUPO"]) for s in schedules}) == 41, \
    f"Grupos={len({(s['GESTIONCODIGO'], s['MATERIACODIGO'], s['GRUPO']) for s in schedules})} ≠ 41"

# Códigos a retirar: 24 + 1 nota.
DROPPED_CODES = {drop for keep, drop in ARTICLE_MERGES} | {"ART-REQ-00348"}
assert len(DROPPED_CODES) == 25, f"Eliminados={len(DROPPED_CODES)} ≠ 25"
conflict_req = {r["ARTICULOCODIGO"] for r in requirements} & DROPPED_CODES
assert not conflict_req, f"Recetas usan códigos eliminados: {conflict_req}"
conflict_stk = {s["ARTICULOCODIGO"] for s in stocks} & DROPPED_CODES
assert not conflict_stk, f"Existencias usan códigos eliminados: {conflict_stk}"

# Regla cronológica semana 6 (D3) — 33 filas sospechosas.
by_week = collections.defaultdict(list)
for s in schedules:
    by_week[s["SEMANA"]].append(s["_exec_date"])
weeks_sorted = sorted(by_week)
week_ranges = {w: (min(by_week[w]), max(by_week[w])) for w in weeks_sorted}
suspect_weeks = set()
prev_max = None
for w in weeks_sorted:
    wmin, wmax = week_ranges[w]
    if prev_max is not None and wmin <= prev_max:
        suspect_weeks.add(w)
    if w not in suspect_weeks:
        prev_max = wmax
suspect_rows = sum(1 for s in schedules if s["SEMANA"] in suspect_weeks)
assert suspect_rows == 33, f"Filas sospechosas={suspect_rows} ≠ 33 (semanas {sorted(suspect_weeks)})"
assert suspect_weeks == {6}, f"Semanas sospechosas={suspect_weeks} ≠ {{6}}"

# Fechas fuera de la gestión 2027-1 (D6) — 2 filas (sem 20, jul-01 y jul-02).
MGMT_END = date(2027, 6, 30)
late_rows = [s for s in schedules if s["_exec_date"] and s["_exec_date"] > MGMT_END]
assert len(late_rows) == 2, f"Fechas fuera de gestión={len(late_rows)} ≠ 2"

# 11 recetas sin cantidad.
qty_null = sum(1 for r in requirements if r["CANTIDAD"] is None)
assert qty_null == 11, f"Cantidad NULL={qty_null} ≠ 11"

# 4 artículos nuevos no existen aún en la BD (lo confirma el worker, aquí solo el set).
NEW_CODES = {code for code, *_ in NEW_ARTICLES}

# Conteo de prácticas sin receta y de artículos únicos en recetas.
practices_in_req = {r["PRACTICACODIGO"] for r in requirements}
articles_in_req = {r["ARTICULOCODIGO"] for r in requirements}
assert len(practices_in_req) == 180, len(practices_in_req)
assert len(articles_in_req) == 493, len(articles_in_req)

print(f"[pre] materias={len(materias_raw)} (codes={len(materias_codes)}) | "
      f"practicas={len(practices)} | programaciones={len(schedules)} | "
      f"recetas={len(requirements)} (qty_null={qty_null}) | "
      f"existencias leidas={len(stocks_raw)} con_datos={len(stocks)} | "
      f"grupos=41 | eliminados={len(DROPPED_CODES)} | "
      f"nuevos={len(NEW_ARTICLES)} | "
      f"suspect_sem6={suspect_rows} | fuera_gestion={len(late_rows)}", flush=True)

# ---------- Construcción del SQL ----------
sql = []
w = sql.append
w("SET NOCOUNT ON;")
w("SET QUOTED_IDENTIFIER ON;  -- requerido por los índices filtrados (sqlcmd lo trae OFF)")
w("SET ANSI_NULLS ON;")
w("SET ANSI_PADDING ON;")
w("SET ANSI_WARNINGS ON;")
w("SET CONCAT_NULL_YIELDS_NULL ON;")
w("SET ARITHABORT ON;")
w("SET XACT_ABORT ON;")
w("IF DB_NAME() <> N'DB_Laboratorios_Univalle_DEMO_QA' THROW 50000, 'Destino incorrecto: solo DEMO_QA', 1;")
w("DECLARE @Now datetime2 = SYSUTCDATETIME();")
w("DECLARE @n int;")
batch_code = "XLSX-V7-ACADEMIC-" + sha[:12].upper()
source_name = "BaseEstructuraFinal_V7.xlsx"
w(f"IF NOT EXISTS (SELECT 1 FROM dbo.ImportBatches WHERE Code = N'{batch_code}') "
  f"INSERT dbo.ImportBatches(Code, SourceType, SourceName, SourceSha256, ImportedAt, Status, Notes, ContractVersion) "
  f"VALUES(N'{batch_code}', N'Excel', N'{source_name}', N'{sha}', @Now, 1, "
  f"N'V7 bloque académico: prácticas, programación 2027-1, recetas, existencias y limpieza de artículos. Solo DEMO_QA.', "
  f"N'base-estructural-v7');")
w(f"DECLARE @Batch int = (SELECT Id FROM dbo.ImportBatches WHERE Code = N'{batch_code}');")
w("CREATE TABLE #Issue(IssueCode nvarchar(60), EntityName nvarchar(60), EntityKey nvarchar(100), FieldName nvarchar(100), "
  "OriginalValue nvarchar(400), NormalizedValue nvarchar(400), Description nvarchar(500), Severity int, SourceSheet nvarchar(60), SourceRow int);")


def issue(code, entity, key, field, original, normalized, desc, severity, sheet_name, row):
    w(f"INSERT #Issue VALUES({q(code)}, {q(entity)}, {q(key)}, {q(field)}, {q(original)}, {q(normalized)}, "
      f"{q(desc)}, {severity}, {q(sheet_name)}, {row if row is not None else 'NULL'});")


# ---------- 0. Migración esperada ----------
w("""-- 0. Migración esperada: la última debe ser AddSubjectKit (T0.3)
IF NOT EXISTS (SELECT 1 FROM dbo.__EFMigrationsHistory WHERE MigrationId = N'20261004214711_AddSubjectKit')
   THROW 50001, 'Falta la migración 20261004214711_AddSubjectKit; abortar T3.', 1;""")

# ---------- 1. Verificación de las 19 materias ----------
w("-- 1. Verificación de las 19 materias (Status = 0)")
w("CREATE TABLE #SubjIn(Code nvarchar(50) PRIMARY KEY);")
for code in materias_codes:
    w(f"INSERT #SubjIn VALUES({q(code)});")
w("""INSERT #Issue SELECT 'SUBJECT_MISSING', 'Subject', s.Code, 'MateriaCodigo', NULL, NULL,
   N'La materia de la V7 no existe en dbo.Subjects; revisar importación previa de la V4.', 2, 'Materias', NULL
FROM #SubjIn s WHERE NOT EXISTS (SELECT 1 FROM dbo.Subjects x WHERE x.Code = s.Code AND x.Status = 0);
IF EXISTS (SELECT 1 FROM #Issue WHERE IssueCode = N'SUBJECT_MISSING')
   THROW 50002, 'Alguna materia de la V7 no existe; abortar T3.', 1;""")

# ---------- 2. Limpieza de artículos (D8 + D9) ----------
w("-- 2. Limpieza de artículos: 24 fusiones + 1 nota L-3 → baja lógica; 4 nuevos → alta")
w("BEGIN TRANSACTION;")
# 2a. Mover referencias de PracticeRequirements / ArticleStocks / SubjectKitItems / DepartureItems / PlanningRequirements
#     del código eliminado al conservado, evitando choque con índices únicos y registrando duplicados.
w("CREATE TABLE #Merge(KEEP_CODE nvarchar(30), DROP_CODE nvarchar(30) PRIMARY KEY);")
for keep, drop in ARTICLE_MERGES:
    w(f"INSERT #Merge VALUES({q(keep)}, {q(drop)});")
w(f"INSERT #Merge VALUES(NULL, {q('ART-REQ-00348')});")  # nota L-3: se da de baja sin artículo conservado
w("""-- 2a. PrácticaRequerimientos: cambiar ArticleId al conservado si no genera duplicado (PracticeId, ArticleId)
INSERT #Issue SELECT 'CONTENT_CONFLICT', 'PracticeRequirement', x.Code, 'ArticleId', m.DROP_CODE, m.KEEP_CODE,
   N'La receta apuntaba a un artículo que se elimina; ya existe otra con el mismo par práctica+artículo. No se reasigna.', 1, 'RequerimientosPractica', NULL
FROM dbo.PracticeRequirements x JOIN dbo.Articles d ON d.Id = x.ArticleId
JOIN #Merge m ON m.DROP_CODE = d.Code
WHERE x.IsRemoved = 0
  AND EXISTS (SELECT 1 FROM dbo.PracticeRequirements y JOIN dbo.Articles k ON k.Id = y.ArticleId
              WHERE y.PracticeId = x.PracticeId AND y.ArticleId <> x.ArticleId AND k.Code = m.KEEP_CODE AND y.IsRemoved = 0);

UPDATE x SET ArticleId = k.Id, LastModifiedDate = @Now
FROM dbo.PracticeRequirements x JOIN dbo.Articles d ON d.Id = x.ArticleId
JOIN #Merge m ON m.DROP_CODE = d.Code
JOIN dbo.Articles k ON k.Code = m.KEEP_CODE
WHERE x.IsRemoved = 0
  AND NOT EXISTS (
      SELECT 1 FROM dbo.PracticeRequirements y JOIN dbo.Articles k2 ON k2.Id = y.ArticleId
      WHERE y.PracticeId = x.PracticeId AND y.ArticleId = k.Id AND y.IsRemoved = 0 AND y.Id <> x.Id);

-- 2b. ArticleStocks: análogo (LabId, ArticleId) sin duplicado
INSERT #Issue SELECT 'CONTENT_CONFLICT', 'ArticleStock', CONCAT(d.Code, N'/', (SELECT TOP 1 Code FROM dbo.Laboratories WHERE Id = x.LaboratoryId)), 'ArticleId', m.DROP_CODE, m.KEEP_CODE,
   N'El stock apuntaba a un artículo que se elimina; ya existe otro stock con ese par lab+artículo. No se reasigna.', 1, 'ExistenciasArticulos', NULL
FROM dbo.ArticleStocks x JOIN dbo.Articles d ON d.Id = x.ArticleId
JOIN #Merge m ON m.DROP_CODE = d.Code
WHERE EXISTS (SELECT 1 FROM dbo.ArticleStocks y JOIN dbo.Articles k ON k.Id = y.ArticleId
              WHERE y.LaboratoryId = x.LaboratoryId AND y.ArticleId <> x.ArticleId AND k.Code = m.KEEP_CODE);

UPDATE x SET ArticleId = k.Id, LastModifiedDate = @Now
FROM dbo.ArticleStocks x JOIN dbo.Articles d ON d.Id = x.ArticleId
JOIN #Merge m ON m.DROP_CODE = d.Code
JOIN dbo.Articles k ON k.Code = m.KEEP_CODE
WHERE NOT EXISTS (
      SELECT 1 FROM dbo.ArticleStocks y JOIN dbo.Articles k2 ON k2.Id = y.ArticleId
      WHERE y.LaboratoryId = x.LaboratoryId AND y.ArticleId = k.Id AND y.Id <> x.Id);

-- 2c. SubjectKitItems (IsRemoved = 0)
UPDATE x SET ArticleId = k.Id, LastModifiedDate = @Now
FROM dbo.SubjectKitItems x JOIN dbo.Articles d ON d.Id = x.ArticleId
JOIN #Merge m ON m.DROP_CODE = d.Code
JOIN dbo.Articles k ON k.Code = m.KEEP_CODE
WHERE x.IsRemoved = 0
  AND NOT EXISTS (
      SELECT 1 FROM dbo.SubjectKitItems y
      WHERE y.SubjectId = x.SubjectId AND y.ArticleId = k.Id AND y.IsRemoved = 0 AND y.Id <> x.Id);

INSERT #Issue SELECT 'CONTENT_CONFLICT', 'SubjectKitItem', CONCAT((SELECT TOP 1 Code FROM dbo.Subjects WHERE Id = x.SubjectId), N'/', m.DROP_CODE), 'ArticleId', m.DROP_CODE, m.KEEP_CODE,
   N'El kit de materia apuntaba a un artículo que se elimina; ya existe otro kit con ese par materia+artículo. No se reasigna.', 1, 'RequerimientosPractica', NULL
FROM dbo.SubjectKitItems x JOIN dbo.Articles d ON d.Id = x.ArticleId
JOIN #Merge m ON m.DROP_CODE = d.Code
WHERE x.IsRemoved = 0
  AND EXISTS (SELECT 1 FROM dbo.SubjectKitItems y JOIN dbo.Articles k ON k.Id = y.ArticleId
              WHERE y.SubjectId = x.SubjectId AND y.ArticleId <> x.ArticleId AND k.Code = m.KEEP_CODE AND y.IsRemoved = 0);

-- 2d. DepartureItems (solo reasignamos cuando ArticleId IS NOT NULL)
UPDATE x SET ArticleId = k.Id, LastModifiedDate = @Now
FROM dbo.DepartureItems x JOIN dbo.Articles d ON d.Id = x.ArticleId
JOIN #Merge m ON m.DROP_CODE = d.Code
JOIN dbo.Articles k ON k.Code = m.KEEP_CODE
WHERE x.IsRemoved = 0
  AND NOT EXISTS (
      SELECT 1 FROM dbo.DepartureItems y JOIN dbo.Articles k2 ON k2.Id = y.ArticleId
      WHERE y.DepartureId = x.DepartureId AND y.ArticleId = k.Id AND y.IsRemoved = 0 AND y.Id <> x.Id);

INSERT #Issue SELECT 'CONTENT_CONFLICT', 'DepartureItem', d.Code, 'ArticleId', m.DROP_CODE, m.KEEP_CODE,
   N'Una salida apuntaba a un artículo que se elimina; ya existe otra línea con el mismo par salida+artículo. No se reasigna.', 1, 'RequerimientosPractica', NULL
FROM dbo.DepartureItems x JOIN dbo.Articles d ON d.Id = x.ArticleId
JOIN #Merge m ON m.DROP_CODE = d.Code
WHERE x.IsRemoved = 0
  AND EXISTS (SELECT 1 FROM dbo.DepartureItems y JOIN dbo.Articles k ON k.Id = y.ArticleId
              WHERE y.DepartureId = x.DepartureId AND y.ArticleId <> x.ArticleId AND k.Code = m.KEEP_CODE AND y.IsRemoved = 0);

-- 2e. PlanningRequirements
UPDATE x SET ArticleId = k.Id
FROM dbo.PlanningRequirements x JOIN dbo.Articles d ON d.Id = x.ArticleId
JOIN #Merge m ON m.DROP_CODE = d.Code
JOIN dbo.Articles k ON k.Code = m.KEEP_CODE
WHERE NOT EXISTS (
      SELECT 1 FROM dbo.PlanningRequirements y JOIN dbo.Articles k2 ON k2.Id = y.ArticleId
      WHERE y.PlanningId = x.PlanningId AND y.ArticleId = k.Id AND y.Id <> x.Id);

INSERT #Issue SELECT 'CONTENT_CONFLICT', 'PlanningRequirement', d.Code, 'ArticleId', m.DROP_CODE, m.KEEP_CODE,
   N'Una planificación apuntaba a un artículo que se elimina; ya existe otra línea con el mismo par planificación+artículo. No se reasigna.', 1, 'RequerimientosPractica', NULL
FROM dbo.PlanningRequirements x JOIN dbo.Articles d ON d.Id = x.ArticleId
JOIN #Merge m ON m.DROP_CODE = d.Code
WHERE EXISTS (SELECT 1 FROM dbo.PlanningRequirements y JOIN dbo.Articles k ON k.Id = y.ArticleId
              WHERE y.PlanningId = x.PlanningId AND y.ArticleId <> x.ArticleId AND k.Code = m.KEEP_CODE);""")

# 2f. Baja lógica de los 25 códigos (los 24 fusionados + la nota L-3) — preservar LastModifiedDate.
w("""UPDATE a SET Status = 2, LastModifiedDate = @Now
FROM dbo.Articles a JOIN #Merge m ON a.Code = m.DROP_CODE
WHERE a.Status <> 2;

SELECT 'articulos_dados_baja_v7' k, @@ROWCOUNT n;""")

# 2g. Alta de los 4 artículos nuevos. Insert en lotes de 100 con respeto de UX_Articles_Name_Active.
w("-- 2g. Alta de los 4 artículos nuevos (D9)")
w("CREATE TABLE #NewArt(Code nvarchar(30) PRIMARY KEY, Name nvarchar(200), Cat nvarchar(30), Unit nvarchar(30), Review int);")
for code, name, cat, unit, _rev in NEW_ARTICLES:
    w(f"INSERT #NewArt VALUES({q(code)}, {q(name)}, {q(cat)}, {q(unit)}, 9);")
w("""SET @n = 1;
WHILE @n > 0
BEGIN
    BEGIN TRANSACTION;
    INSERT dbo.Articles(Code, Name, Category, UnitOfMeasure, Status, ArticleCategoryId, MeasurementUnitId, ImportBatchId, CreatedDate)
    SELECT a.Code, a.Name, (SELECT TOP 1 Name FROM dbo.ArticleCategories WHERE Code = a.Cat), a.Unit, 0,
           (SELECT TOP 1 Id FROM dbo.ArticleCategories WHERE Code = a.Cat),
           (SELECT TOP 1 Id FROM dbo.MeasurementUnits WHERE Code = a.Unit),
           @Batch, @Now
    FROM #NewArt a
    WHERE NOT EXISTS (SELECT 1 FROM dbo.Articles x WHERE x.Code = a.Code);
    SET @n = @@ROWCOUNT;
    COMMIT TRANSACTION;
END;

UPDATE a SET ReviewStatus = 9, LastModifiedDate = @Now
FROM dbo.Articles a JOIN #NewArt n ON n.Code = a.Code
WHERE a.ReviewStatus <> 9;

SELECT 'articulos_nuevos_v7' k, @@ROWCOUNT n;""")
w("COMMIT TRANSACTION;")
# Registrar incidencias informativas (no críticas) por la limpieza y la creación
for code in sorted(DROPPED_CODES):
    issue("ARTICLE_RETIRED_V7", "Article", code, "Status", "Activo", "Eliminado",
          "Artículo retirado lógicamente por la limpieza de la V7 (fusión o nota L-3).", 0, "Articulos", None)
for code, name, cat, unit, _rev in NEW_ARTICLES:
    issue("ARTICLE_NEW_V7", "Article", code, "Code", None, code,
          f"Artículo nuevo creado desde la V7 ({name}; {cat}/{unit}; ReviewStatus=9).", 0, "Articulos", None)

# ---------- 3. Prácticas (D10/D11) ----------
w("-- 3. Prácticas (Name = LEFT(Description, 200); Coverage/Source NULL por D11)")
w("CREATE TABLE #Prac(Code nvarchar(50) PRIMARY KEY, Subj nvarchar(50), Num nvarchar(20), Descr nvarchar(2000), Obs nvarchar(2000), SourceRow int);")
for p in practices:
    w(f"INSERT #Prac VALUES({q(p['PRACTICACODIGO'])}, {q(p['MATERIACODIGO'])}, {q(p['NUMEROPRACTICA'])}, "
      f"{q(p['_desc'])}, {q(p['OBSERVACIONES'])}, {p['_row']});")
# VALUE_TRUNCATED_IN_NAME: description > 200 chars
for p in practices:
    if len(p["_desc"]) > 200:
        issue("VALUE_TRUNCATED_IN_NAME", "Practice", p["PRACTICACODIGO"], "Name",
              f"{len(p['_desc'])} chars", "200",
              "Description > 200 chars: Practice.Name se trunca y Description conserva el texto completo (D10).", 0, "Practicas", p["_row"])
w("""INSERT #Issue SELECT 'UNRESOLVED_REFERENCE', 'Practice', p.Code, 'MateriaCodigo', p.Subj, NULL,
   N'Materia inexistente; la práctica no se carga.', 2, 'Practicas', p.SourceRow
FROM #Prac p WHERE NOT EXISTS (SELECT 1 FROM dbo.Subjects s WHERE s.Code = p.Subj AND s.Status = 0);

BEGIN TRANSACTION;
-- UPDATE idempotente: solo si Description / Number / Observations cambiaron (Name no se actualiza por D2).
UPDATE x SET Number = p.Num, Description = p.Descr, Observations = p.Obs, LastModifiedDate = @Now
FROM dbo.Practices x JOIN #Prac p ON p.Code = x.Code JOIN dbo.Subjects s ON s.Code = p.Subj AND s.Status = 0
WHERE ISNULL(x.Number, N'') <> p.Num
   OR ISNULL(x.Description, N'') <> p.Descr
   OR ISNULL(x.Observations, N'') <> p.Obs;

INSERT dbo.Practices(Code, SubjectId, Name, Number, Description, CoverageStatus, CoverageMethod, SourceReference, Observations, Status, CreatedDate)
SELECT p.Code, s.Id, LEFT(p.Descr, 200), p.Num, p.Descr, NULL, NULL, NULL, p.Obs, 0, @Now
FROM #Prac p JOIN dbo.Subjects s ON s.Code = p.Subj AND s.Status = 0
WHERE NOT EXISTS (SELECT 1 FROM dbo.Practices x WHERE x.Code = p.Code);
SELECT 'practicas_insertadas_v7' k, @@ROWCOUNT n;
COMMIT TRANSACTION;""")

# ---------- 4. Recetas (D2: Req-Codigo y Art en MAYÚSCULAS; D4: Cantidad NULL → incidencia) ----------
w("-- 4. Recetas (RequerimientosPractica)")
w("CREATE TABLE #Req(Code nvarchar(50) PRIMARY KEY, Prac nvarchar(50), Art nvarchar(30), Qty decimal(12,2) NULL, "
  "Scal bit NULL, Val nvarchar(40), Obs nvarchar(1000), SourceRow int);")
for r in requirements:
    if r["CANTIDAD"] is None:
        issue("VALUE_MISSING", "PracticeRequirement", r["REQUERIMIENTOCODIGO"], "Cantidad", None, None,
              "La fuente no trae cantidad; se carga sin cantidad (no se inventa).", 1, "RequerimientosPractica", r["_row"])
    w(f"INSERT #Req VALUES({q(r['REQUERIMIENTOCODIGO'])}, {q(r['PRACTICACODIGO'])}, {q(r['ARTICULOCODIGO'])}, "
      f"{q(r['CANTIDAD'])}, {as_bool(r['ESCALABLE'])}, {q(r['ESTADOVALIDACION'])}, "
      f"{q(r['OBSERVACIONES'])}, {r['_row']});")
w("""INSERT #Issue SELECT 'UNRESOLVED_REFERENCE', 'PracticeRequirement', r.Code, 'PracticaCodigo/ArticuloCodigo',
   CONCAT(r.Prac, N' / ', r.Art), NULL,
   N'Práctica o artículo inexistente; la línea no se carga.', 2, 'RequerimientosPractica', r.SourceRow
FROM #Req r WHERE NOT EXISTS (SELECT 1 FROM dbo.Practices p WHERE p.Code = r.Prac)
            OR NOT EXISTS (SELECT 1 FROM dbo.Articles a WHERE a.Code = r.Art AND a.Status <> 2);

SET @n = 1;
WHILE @n > 0
BEGIN
    BEGIN TRANSACTION;
    INSERT dbo.PracticeRequirements(Code, PracticeId, ArticleId, Quantity, IsScalable, ValidationStatus, Observations, IsRemoved, CreatedDate)
    SELECT TOP (100) r.Code, p.Id, a.Id, r.Qty, ISNULL(r.Scal, 0), r.Val, r.Obs, 0, @Now
    FROM #Req r JOIN dbo.Practices p ON p.Code = r.Prac
               JOIN dbo.Articles a ON a.Code = r.Art AND a.Status <> 2
    WHERE NOT EXISTS (SELECT 1 FROM dbo.PracticeRequirements x WHERE x.Code = r.Code)
      AND NOT EXISTS (SELECT 1 FROM dbo.PracticeRequirements x
                      WHERE x.PracticeId = p.Id AND x.ArticleId = a.Id AND x.IsRemoved = 0);
    SET @n = @@ROWCOUNT;
    COMMIT TRANSACTION;
END;
SELECT 'recetas_insertadas_v7' k, @@ROWCOUNT n;""")

# ---------- 5. Existencias (D4: solo filas con laboratorio y cantidad) ----------
w("-- 5. Existencias por laboratorio (solo las 220 con laboratorio + cantidad; D4)")
w("CREATE TABLE #Stk(Lab nvarchar(50), Art nvarchar(30), Qty decimal(12,2), Upd date NULL, SourceRow int);")
for s in stocks:
    w(f"INSERT #Stk VALUES({q(s['LABORATORIOCODIGO'])}, {q(s['ARTICULOCODIGO'])}, "
      f"{q(s['CANTIDADDISPONIBLE'])}, {q_date(s['FECHAACTUALIZACION'])}, {s['_row']});")
w("""INSERT #Issue SELECT 'UNRESOLVED_REFERENCE', 'ArticleStock', CONCAT(s.Lab, N'/', s.Art),
   'LaboratorioCodigo/ArticuloCodigo', NULL, NULL,
   N'Laboratorio o artículo inexistente o retirado; no se carga.', 2, 'ExistenciasArticulos', s.SourceRow
FROM #Stk s WHERE NOT EXISTS (SELECT 1 FROM dbo.Laboratories l WHERE l.Code = s.Lab)
            OR NOT EXISTS (SELECT 1 FROM dbo.Articles a WHERE a.Code = s.Art AND a.Status <> 2);

BEGIN TRANSACTION;
-- CONTENT_CONFLICT solo cuando la cantidad difiere (UpdatedOn siempre NULL por la fuente).
INSERT #Issue SELECT 'CONTENT_CONFLICT', 'ArticleStock', CONCAT(s.Lab, N'/', s.Art), 'CantidadDisponible',
   CAST(x.AvailableQuantity AS nvarchar(40)), CAST(s.Qty AS nvarchar(40)),
   N'La BD tiene otra cantidad para el mismo par lab+artículo; no se sobreescribe (revisar la fuente).', 1, 'ExistenciasArticulos', s.SourceRow
FROM #Stk s JOIN dbo.Laboratories l ON l.Code = s.Lab JOIN dbo.Articles a ON a.Code = s.Art AND a.Status <> 2
JOIN dbo.ArticleStocks x ON x.LaboratoryId = l.Id AND x.ArticleId = a.Id
WHERE x.AvailableQuantity <> s.Qty;

INSERT dbo.ArticleStocks(LaboratoryId, ArticleId, AvailableQuantity, UpdatedOn, CreatedDate)
SELECT l.Id, a.Id, s.Qty, NULL, @Now
FROM #Stk s JOIN dbo.Laboratories l ON l.Code = s.Lab JOIN dbo.Articles a ON a.Code = s.Art AND a.Status <> 2
WHERE NOT EXISTS (SELECT 1 FROM dbo.ArticleStocks x WHERE x.LaboratoryId = l.Id AND x.ArticleId = a.Id);
SELECT 'existencias_insertadas_v7' k, @@ROWCOUNT n;
COMMIT TRANSACTION;""")

# ---------- 6. Programación (D3/D4/D6/D12/CourseGroup) ----------
w("-- 6. Programación de prácticas 2027-1 + grupos derivados (GRP-2027-1-MATERIA-GRUPO)")
w("CREATE TABLE #Sch(Code nvarchar(50) PRIMARY KEY, Mgmt nvarchar(50), Subj nvarchar(50), Prac nvarchar(50), Grp nvarchar(20), "
  "Teacher nvarchar(50), Lab nvarchar(50), Week int NULL, ExecDate date NULL, T1 time NULL, T2 time NULL, "
  "Students int NULL, Demo bit NULL, Factor int NULL, St int, HasTime bit, SourceRow int);")
combos = collections.defaultdict(list)
for s in schedules:
    st_raw = str(s["ESTADO"] or "").strip().lower()
    st_id = SCHEDULE_STATUS.get(st_raw, 9)
    if st_id == 9:
        issue("VALUE_PENDING_CONFIRMATION", "PracticeSchedule", s["PROGRAMACIONCODIGO"], "Estado",
              s["ESTADO"], "PorConfirmar",
              "Estado de la programación no reconocido o sin confirmar.", 0, "ProgramacionPracticas", s["_row"])
    if not s["GRUPO"]:
        issue("VALUE_MISSING", "PracticeSchedule", s["PROGRAMACIONCODIGO"], "Grupo", None, None,
              "Programación sin grupo: no se carga (rompería la unicidad gestión + práctica + grupo).", 2, "ProgramacionPracticas", s["_row"])
        continue
    w(f"INSERT #Sch VALUES({q(s['PROGRAMACIONCODIGO'])}, {q(s['GESTIONCODIGO'])}, {q(s['MATERIACODIGO'])}, "
      f"{q(s['PRACTICACODIGO'])}, {q(s['GRUPO'])}, {q(s['DOCENTEACTORCODIGO'])}, {q(s['LABORATORIOCODIGO'])}, "
      f"{q(s['SEMANA'])}, {q_date(s['FECHAEJECUCION'])}, {q_time(s['HORAINICIO'])}, {q_time(s['HORAFIN'])}, "
      f"{q(s['CANTESTUDIANTES'])}, {as_bool(s['ESDEMOSTRATIVA'])}, {q(s['FACTORMATERIA'])}, {st_id}, "
      f"{1 if s['_has_time'] else 0}, {s['_row']});")
    combos[(s["GESTIONCODIGO"], s["MATERIACODIGO"], s["GRUPO"])].append(s)

# Grupos derivados (D4/CourseGroup)
w("CREATE TABLE #Grp(Code nvarchar(50) PRIMARY KEY, Mgmt nvarchar(50), Subj nvarchar(50), Grp nvarchar(20), "
  "Teacher nvarchar(50), Lab nvarchar(50), T1 time NULL, T2 time NULL, Students int NULL, Demo bit NULL, Factor int NULL);")
for (mgmt, subj, grp), rows in combos.items():
    code = f"GRP-{mgmt}-{subj}-{grp}"
    picked = {}
    for field in ("DOCENTEACTORCODIGO", "LABORATORIOCODIGO", "HORAINICIO", "HORAFIN",
                  "CANTESTUDIANTES", "ESDEMOSTRATIVA", "FACTORMATERIA"):
        values = collections.Counter(str(r[field]) for r in rows if r[field] is not None)
        if values:
            top_value, _ = values.most_common(1)[0]
            picked[field] = next((r[field] for r in rows if r[field] is not None and str(r[field]) == top_value), None)
            if len(values) > 1:
                issue("COURSE_GROUP_VALUES_VARY", "CourseGroup", code, field,
                      None, ", ".join(values),
                      "Las programaciones del grupo tienen valores distintos; el grupo toma el más frecuente y cada programación conserva el suyo.",
                      0, "ProgramacionPracticas", None)
        else:
            picked[field] = None
    w(f"INSERT #Grp VALUES({q(code)}, {q(mgmt)}, {q(subj)}, {q(grp)}, "
      f"{q(picked['DOCENTEACTORCODIGO'])}, {q(picked['LABORATORIOCODIGO'])}, "
      f"{q_time(picked['HORAINICIO'])}, {q_time(picked['HORAFIN'])}, "
      f"{q(picked['CANTESTUDIANTES'])}, {as_bool(picked['ESDEMOSTRATIVA'])}, {q(picked['FACTORMATERIA'])});")

# Incidencias propias del bloque de programación
w("""-- Resolución de referencias (UNRESOLVED_REFERENCE)
INSERT #Issue SELECT 'UNRESOLVED_REFERENCE', 'PracticeSchedule', s.Code, 'Gestion/Materia/Practica/Docente',
   CONCAT(s.Mgmt, N' / ', s.Subj, N' / ', s.Prac, N' / ', s.Teacher), NULL,
   N'Referencia inexistente; la programación no se carga.', 2, 'ProgramacionPracticas', s.SourceRow
FROM #Sch s WHERE NOT EXISTS (SELECT 1 FROM dbo.Managements m WHERE m.Code = s.Mgmt)
            OR NOT EXISTS (SELECT 1 FROM dbo.Subjects x WHERE x.Code = s.Subj AND x.Status = 0)
            OR NOT EXISTS (SELECT 1 FROM dbo.Practices p WHERE p.Code = s.Prac)
            OR NOT EXISTS (SELECT 1 FROM dbo.People p WHERE p.ActorCode = s.Teacher);

-- Práctica de otra materia
INSERT #Issue SELECT 'CONTENT_CONFLICT', 'PracticeSchedule', s.Code, 'PracticaCodigo', s.Prac, s.Subj,
   N'La práctica pertenece a otra materia.', 1, 'ProgramacionPracticas', s.SourceRow
FROM #Sch s JOIN dbo.Practices p ON p.Code = s.Prac JOIN dbo.Subjects x ON x.Code = s.Subj
WHERE p.SubjectId <> x.Id;

-- Colisión con programación activa existente (gestión + práctica + grupo)
INSERT #Issue SELECT 'CONTENT_CONFLICT', 'PracticeSchedule', s.Code, 'Gestion/Practica/Grupo', z.Code, NULL,
   N'Ya existe otra programación con la misma gestión, práctica y grupo; esta fila no se carga.', 2, 'ProgramacionPracticas', s.SourceRow
FROM #Sch s JOIN dbo.Managements m ON m.Code = s.Mgmt JOIN dbo.Practices pr ON pr.Code = s.Prac
JOIN dbo.PracticeSchedules z ON z.ManagementId = m.Id AND z.PracticeId = pr.Id AND z.GroupName = s.Grp
                            AND z.IsDeleted = 0 AND z.Code <> s.Code;

-- Docente sin rol Docente activo (Role = 8)
INSERT #Issue SELECT 'TEACHER_WITHOUT_ROLE', 'PracticeSchedule', s.Code, 'DocenteActorCodigo', s.Teacher, NULL,
   N'El actor no tiene rol Docente activo.', 1, 'ProgramacionPracticas', s.SourceRow
FROM #Sch s JOIN dbo.People p ON p.ActorCode = s.Teacher
WHERE NOT EXISTS (SELECT 1 FROM dbo.PersonRoleAssignments r WHERE r.PersonId = p.Id AND r.Role = 8 AND r.IsActive = 1);

-- SCHEDULE_DATE_SUSPECT (D3) — semanas marcadas por la regla cronológica
INSERT #Issue SELECT 'SCHEDULE_DATE_SUSPECT', 'PracticeSchedule', s.Code, 'FechaEjecucion',
   CONVERT(nvarchar(10), s.ExecDate, 23), NULL,
   N'La fecha cae en una semana marcada como sospechosa por la regla cronológica (semana 6 en la V7).', 1, 'ProgramacionPracticas', s.SourceRow
FROM #Sch s WHERE s.Week IN (""" + ",".join(str(w_) for w_ in sorted(suspect_weeks)) + """);

-- SCHEDULE_OUT_OF_MANAGEMENT (D6) — fechas posteriores al fin de la gestión
INSERT #Issue SELECT 'SCHEDULE_OUT_OF_MANAGEMENT', 'PracticeSchedule', s.Code, 'FechaEjecucion',
   CONVERT(nvarchar(10), s.ExecDate, 23), '2027-06-30',
   N'La fecha está después del fin planificado de la gestión 2027-1.', 0, 'ProgramacionPracticas', s.SourceRow
FROM #Sch s
WHERE s.ExecDate IS NOT NULL
  AND EXISTS (SELECT 1 FROM dbo.Managements m WHERE m.Code = s.Mgmt AND m.PlannedEndDate IS NOT NULL AND s.ExecDate > m.PlannedEndDate);

-- VALUE_PENDING_CONFIRMATION por hora, una por grupo (no por fila): la fila con SourceRow mínimo
INSERT #Issue SELECT 'VALUE_PENDING_CONFIRMATION', 'CourseGroup', CONCAT(N'GRP-', s.Mgmt, N'-', s.Subj, N'-', s.Grp), 'HoraInicio/HoraFin', NULL, NULL,
   N'Las programaciones del grupo no declaran hora de inicio ni de fin.', 0, 'ProgramacionPracticas', MIN(s.SourceRow)
FROM #Sch s
WHERE s.HasTime = 0
GROUP BY s.Mgmt, s.Subj, s.Grp;

-- Grupos: alta
BEGIN TRANSACTION;
INSERT dbo.CourseGroups(Code, ManagementId, SubjectId, Name, TeacherPersonId, LaboratoryId, StartTime, EndTime,
    StudentCount, IsDemonstrative, ScalingFactor, Status, CreatedDate)
SELECT g.Code, m.Id, x.Id, g.Grp, p.Id, l.Id, g.T1, g.T2, g.Students, ISNULL(g.Demo, 0), g.Factor, 0, @Now
FROM #Grp g JOIN dbo.Managements m ON m.Code = g.Mgmt
           JOIN dbo.Subjects x ON x.Code = g.Subj AND x.Status = 0
           JOIN dbo.People p ON p.ActorCode = g.Teacher
LEFT JOIN dbo.Laboratories l ON l.Code = g.Lab
WHERE NOT EXISTS (SELECT 1 FROM dbo.CourseGroups c WHERE c.Code = g.Code);
SELECT 'grupos_insertados_v7' k, @@ROWCOUNT n;
COMMIT TRANSACTION;

-- Programaciones: alta idempotente, en lotes de 100
SET @n = 1;
WHILE @n > 0
BEGIN
    BEGIN TRANSACTION;
    INSERT dbo.PracticeSchedules(Code, ManagementId, SubjectId, PracticeId, CourseGroupId, GroupName, TeacherPersonId,
        LaboratoryId, WeekNumber, AcademicWeekId, ExecutionDate, StartTime, EndTime, StudentCount,
        IsDemonstrative, ScalingFactor, Status, ImportBatchId, CreatedDate, IsDeleted)
    SELECT TOP (100) s.Code, m.Id, x.Id, pr.Id, cg.Id, s.Grp, pe.Id, l.Id, s.Week,
        (SELECT TOP 1 w.Id FROM dbo.AcademicWeeks w
         WHERE w.ManagementId = m.Id AND w.Number = s.Week),
        s.ExecDate, s.T1, s.T2, s.Students, s.Demo, s.Factor, s.St, @Batch, @Now, 0
    FROM #Sch s JOIN dbo.Managements m ON m.Code = s.Mgmt
               JOIN dbo.Subjects x ON x.Code = s.Subj AND x.Status = 0
               JOIN dbo.Practices pr ON pr.Code = s.Prac AND pr.SubjectId = x.Id
               JOIN dbo.People pe ON pe.ActorCode = s.Teacher
    LEFT JOIN dbo.Laboratories l ON l.Code = s.Lab
    LEFT JOIN dbo.CourseGroups cg ON cg.Code = CONCAT(N'GRP-', s.Mgmt, N'-', s.Subj, N'-', s.Grp)
    WHERE NOT EXISTS (SELECT 1 FROM dbo.PracticeSchedules z WHERE z.Code = s.Code)
      AND NOT EXISTS (SELECT 1 FROM dbo.PracticeSchedules z
                      WHERE z.ManagementId = m.Id AND z.PracticeId = pr.Id AND z.GroupName = s.Grp AND z.IsDeleted = 0)
      AND s.Code = (SELECT MIN(d.Code) FROM #Sch d WHERE d.Mgmt = s.Mgmt AND d.Prac = s.Prac AND d.Grp = s.Grp);
    SET @n = @@ROWCOUNT;
    COMMIT TRANSACTION;
END;
SELECT 'programaciones_insertadas_v7' k, @@ROWCOUNT n;""")

# ---------- 7. Incidencias (idempotentes) ----------
w("""-- 7. Incidencias (idempotentes por ImportBatchId + IssueCode + EntityKey + FieldName)
SET @n = 1;
WHILE @n > 0
BEGIN
    BEGIN TRANSACTION;
    INSERT dbo.DataQualityIssues(ImportBatchId, IssueCode, EntityName, EntityKey, FieldName, OriginalValue, NormalizedValue,
        Description, Severity, Status, CreatedDate, SourceSheet, SourceRowNumber)
    SELECT TOP (100) @Batch, i.IssueCode, i.EntityName, i.EntityKey, i.FieldName, i.OriginalValue, i.NormalizedValue,
        i.Description, i.Severity, 0, @Now, i.SourceSheet, i.SourceRow
    FROM (SELECT DISTINCT * FROM #Issue) i
    WHERE NOT EXISTS (SELECT 1 FROM dbo.DataQualityIssues d WHERE d.ImportBatchId = @Batch AND d.IssueCode = i.IssueCode
                      AND ISNULL(d.EntityKey, N'') = ISNULL(i.EntityKey, N'')
                      AND ISNULL(d.FieldName, N'') = ISNULL(i.FieldName, N''));
    SET @n = @@ROWCOUNT;
    COMMIT TRANSACTION;
END;
SELECT 'incidencias_v7' k, @@ROWCOUNT n;""")

# ---------- Verificación ----------
w("""-- 8. Verificación
SELECT 'practicas_v7' k, COUNT(*) n FROM dbo.Practices WHERE Code LIKE N'PRA-%' AND Code NOT LIKE N'PRA-GAS%' AND Status = 0
UNION ALL SELECT 'programaciones_2027_1', COUNT(*) FROM dbo.PracticeSchedules ps JOIN dbo.Managements m ON m.Id = ps.ManagementId WHERE m.Code = N'2027-1'
UNION ALL SELECT 'grupos_2027_1', COUNT(*) FROM dbo.CourseGroups WHERE Code LIKE N'GRP-2027-1-%' AND Status <> 2
UNION ALL SELECT 'recetas_v7', COUNT(*) FROM dbo.PracticeRequirements WHERE Code LIKE N'REQ-%' AND IsRemoved = 0
UNION ALL SELECT 'incidencias_lote_v7', COUNT(*) FROM dbo.DataQualityIssues WHERE ImportBatchId = @Batch;""")

io.open(OUT, "w", encoding="utf-8").write("\n".join(sql) + "\n")

# Conteo de incidencias por código (esperadas y generadas por el script).
ISSUE_COUNT_HINTS = {
    "ARTICLE_RETIRED_V7": 25,
    "ARTICLE_NEW_V7": 4,
    "VALUE_TRUNCATED_IN_NAME": 2,
    "VALUE_MISSING (recetas Cantidad)": 11,
    "VALUE_MISSING (grupo vacío)": 0,
    "SCHEDULE_DATE_SUSPECT": 33,
    "SCHEDULE_OUT_OF_MANAGEMENT": 2,
    "VALUE_PENDING_CONFIRMATION (Estado)": 0,
    "VALUE_PENDING_CONFIRMATION (Hora)": None,  # depende del Excel
    "UNRES_REFERENCE": None,
    "CONTENT_CONFLICT (stock duplicado)": None,
    "CONTENT_CONFLICT (práctica de otra materia)": None,
    "CONTENT_CONFLICT (colisión programación)": None,
    "TEACHER_WITHOUT_ROLE": None,
    "COURSE_GROUP_VALUES_VARY": None,
    "SUBJECT_MISSING": 0,
}

print(OUT, "|",
      "materias=", len(materias_raw),
      "| practicas=", len(practices),
      "| programaciones=", len(schedules),
      "| grupos=", len(combos),
      "| recetas=", len(requirements),
      "| existencias_leidas=", len(stocks_raw),
      "| existencias_cargadas=", len(stocks),
      "| articulos_eliminados=", len(DROPPED_CODES),
      "| articulos_nuevos=", len(NEW_ARTICLES),
      "| suspect_sem6=", suspect_rows,
      "| fuera_gestion=", len(late_rows),
      "| truncadas_name=", sum(1 for p in practices if len(p["_desc"]) > 200),
      "| qty_null=", qty_null, flush=True)