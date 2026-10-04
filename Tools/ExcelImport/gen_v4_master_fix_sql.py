"""
V4 · Bloque 1 · corrección de maestros (MAPA_EXCEL_V4_A_BD.md §3.4–3.9).
Genera import_v4_master.sql para DB_Laboratorios_Univalle_DEMO_QA ÚNICAMENTE (el SQL aborta en otra base).
Idempotente: claves naturales (Code, CatalogCode, InventoryNumber, ActorCode) e incidencias por lote + código + clave + campo.
"""
import hashlib
import io
import os
import sys

import openpyxl

XLSX = r"C:\Users\monte\Downloads\BaseEstructuraFinal_V4.xlsx"
EXPECTED_SHA = "8ebb238c24a7ce94a71ff0506675fdc216db8d2831b7c24d27f7aab5ff1eb9d2"
OUT = os.path.join(os.environ.get("TEMP", "."), "mm", "import_v4_master.sql")

sha = hashlib.sha256(open(XLSX, "rb").read()).hexdigest()
if sha != EXPECTED_SHA:
    sys.exit(f"Hash distinto: {sha}")

wb = openpyxl.load_workbook(XLSX, read_only=True, data_only=True)

EXPECTED_HEADERS = {
    "Laboratorios": ("LaboratorioCodigo", "Nombre", "SedeCodigo", "FacultadCodigo", "Tipo", "Edificio", "Bloque", "Piso", "Estado"),
    "Gestiones": ("GestionCodigo", "TipoGestion", "Anio", "Semestre", "FechaInicio", "FechaFin", "Estado"),
    "Actores": ("ActorCodigo", "NombreRazonSocial", "Titulo", "TipoActor", "Vinculacion", "Email", "Telefono", "Direccion", "Estado"),
    "CatalogoEquipos": ("CatalogoCodigo", "NombreGenerico", "Categoria", "TipoUtensilio", "Clasificacion", "Marca", "Modelo",
                        "PaisOrigen", "VidaUtilAnios", "Especificaciones", "Estado"),
    "UnidadesFisicas": ("NumeroInventario", "CatalogoCodigo", "NumeroSerie", "LaboratorioCodigo", "EstadoUbicacion", "UbicacionInterna",
                        "FechaAdquisicion", "FechaFabricacion", "ValorAdquisicionBs", "EstadoOperativo", "CondicionFisica", "Observaciones"),
}


def sheet(name):
    it = wb[name].iter_rows(values_only=True)
    header = tuple(next(it))
    if header[:len(EXPECTED_HEADERS[name])] != EXPECTED_HEADERS[name]:
        sys.exit(f"La hoja {name} cambió de encabezados: {header}")
    out = []
    for i, r in enumerate(it, start=2):
        if any(v is not None for v in r):
            d = dict(zip(header, r))
            d["_row"] = i
            out.append(d)
    return out


def q(v):
    if v is None or (isinstance(v, str) and v.strip() == ""):
        return "NULL"
    if isinstance(v, (int, float)) and not isinstance(v, bool):
        return str(v)
    return "N'" + str(v).strip().replace("'", "''") + "'"


def low(v):
    return str(v or "").strip().lower()


PENDING = {"por confirmar", "sin especificar"}
STATUS_MAP = {"operativo": 0, "de baja": 3, "por confirmar": 98}
COND_MAP = {"excelente": 5, "bueno": 4, "regular": 3, "malo": 2, "baja del equipo": 1, "por confirmar": 9}

labs = sheet("Laboratorios")
mgmts = sheet("Gestiones")
actors = sheet("Actores")
cats = sheet("CatalogoEquipos")
units = sheet("UnidadesFisicas")

sql = []
w = sql.append
w("SET NOCOUNT ON;")
w("SET XACT_ABORT ON;")
w("IF DB_NAME() <> N'DB_Laboratorios_Univalle_DEMO_QA' THROW 50000, 'Destino incorrecto: solo DEMO_QA', 1;")
w("DECLARE @Now datetime2 = SYSUTCDATETIME();")
w("DECLARE @n int;")
batch_code = "XLSX-V4-MASTER-" + sha[:12].upper()
w(f"IF NOT EXISTS (SELECT 1 FROM dbo.ImportBatches WHERE Code = N'{batch_code}') "
  f"INSERT dbo.ImportBatches(Code, SourceType, SourceName, SourceSha256, ImportedAt, Status, Notes, ContractVersion) "
  f"VALUES(N'{batch_code}', N'Excel', N'BaseEstructuraFinal_V4.xlsx', N'{sha}', @Now, 1, "
  f"N'V4 bloque 1: laboratorios, gestiones, títulos, catálogo y unidades. Solo DEMO_QA.', N'base-estructural-v4');")
w(f"DECLARE @Batch int = (SELECT Id FROM dbo.ImportBatches WHERE Code = N'{batch_code}');")
w("CREATE TABLE #Issue(IssueCode nvarchar(60), EntityName nvarchar(60), EntityKey nvarchar(100), FieldName nvarchar(100), "
  "OriginalValue nvarchar(400), NormalizedValue nvarchar(400), Description nvarchar(500), Severity int, SourceSheet nvarchar(60), SourceRow int);")


def issue(code, entity, key, field, original, normalized, desc, severity, sheet_name, row):
    w(f"INSERT #Issue VALUES({q(code)}, {q(entity)}, {q(key)}, {q(field)}, {q(original)}, {q(normalized)}, {q(desc)}, {severity}, {q(sheet_name)}, {row if row else 'NULL'});")


# ---------- 1. Laboratorios (§3.4) ----------
w("-- 1. Laboratorios: tipo, edificio, bloque y piso según el Excel")
w("BEGIN TRANSACTION;")
for lab in labs:
    floor = lab["Piso"]
    if low(floor) in PENDING:
        issue("VALUE_PENDING_CONFIRMATION", "Laboratory", lab["LaboratorioCodigo"], "Piso", floor, None,
              "Piso 'Por confirmar' en la fuente: se deja vacío.", 1, "Laboratorios", lab["_row"])
        floor = None
    w(f"UPDATE dbo.Laboratories SET Type = {q(lab['Tipo'])}, Building = {q(lab['Edificio'])}, Block = {q(lab['Bloque'])}, Floor = {q(floor)}, LastModifiedDate = @Now "
      f"WHERE Code = {q(lab['LaboratorioCodigo'])} AND (ISNULL(Type,N'') <> ISNULL({q(lab['Tipo'])},N'') OR ISNULL(Building,N'') <> ISNULL({q(lab['Edificio'])},N'') "
      f"OR ISNULL(Block,N'') <> ISNULL({q(lab['Bloque'])},N'') OR ISNULL(Floor,N'') <> ISNULL({q(floor)},N''));")
for code in ("OFICINAS 1ER PISO", "OFICINAS PLANTA BAJA"):
    issue("LEGACY_ONLY_IN_DB", "Laboratory", code, None, None, None,
          "Laboratorio que no existe en la base del cliente; se conserva activo hasta que confirme la ubicación de sus unidades (D5).", 1, None, None)
w("COMMIT TRANSACTION;")

# ---------- 2. Gestiones (§3.5) ----------
w("-- 2. Gestiones históricas faltantes (Type = Por confirmar); 1900-1 no se carga")
w("BEGIN TRANSACTION;")
for g in mgmts:
    code = g["GestionCodigo"]
    try:
        year = int(g["Anio"])
    except (TypeError, ValueError):
        issue("INVALID_MANAGEMENT", "Management", code, "Anio", g["Anio"], None,
              "Año vacío o no numérico en la fuente; la gestión no se carga.", 2, "Gestiones", g["_row"])
        continue
    if year < 2000 or year > 2100:
        issue("INVALID_MANAGEMENT", "Management", code, "Anio", g["Anio"], None,
              "Gestión inválida en la fuente (año fuera de 2000-2100); no se carga y sus filas van a cuarentena.", 2, "Gestiones", g["_row"])
        continue
    closed = low(g["Estado"]) == "cerrada"
    status = 2 if closed else 1  # Una "Abierta" nueva queda Inactiva: la gestión activa se elige en la app.
    start = g["FechaInicio"].strftime("%Y-%m-%d") if g["FechaInicio"] else None
    end = g["FechaFin"].strftime("%Y-%m-%d") if g["FechaFin"] else None
    w(f"IF NOT EXISTS (SELECT 1 FROM dbo.Managements WHERE Code = {q(code)}) "
      f"INSERT dbo.Managements(Year, Semester, Code, Description, StartDate, PlannedEndDate, ActualClosedDate, Status, Type, CreatedDate) "
      f"VALUES({int(g['Anio'])}, {int(g['Semestre'])}, {q(code)}, N'Gestión histórica (base estructural V4)', {q(start)}, {q(end)}, "
      f"{q(end) if closed else 'NULL'}, {status}, 9, @Now);")
    if low(g["TipoGestion"]) in PENDING:
        issue("VALUE_PENDING_CONFIRMATION", "Management", code, "TipoGestion", g["TipoGestion"], "PorConfirmar",
              "Tipo de gestión sin confirmar en la fuente (las gestiones existentes conservan su tipo).", 0, "Gestiones", g["_row"])
w("COMMIT TRANSACTION;")

# ---------- 3. Actores: títulos y posibles duplicados (§3.6) ----------
w("-- 3. Títulos de actores y posibles duplicados (no se fusionan)")
w("BEGIN TRANSACTION;")
for a in actors:
    if a["Titulo"]:
        w(f"UPDATE dbo.People SET Title = {q(a['Titulo'])} WHERE ActorCode = {q(a['ActorCodigo'])} AND Title IS NULL;")
for group in (("ACT-0007", "ACT-0008", "ACT-0016"), ("ACT-0018", "ACT-0019", "ACT-0020"), ("ACT-0005", "ACT-0010", "ACT-0033")):
    issue("POSSIBLE_PERSON_DUPLICATE", "Person", group[0], None, None, ", ".join(group),
          "Posible mismo actor con varios códigos en la fuente; no se fusiona.", 1, "Actores", None)
w("COMMIT TRANSACTION;")

# ---------- 4. Catálogo de equipos (§3.8) ----------
w("-- 4. Catálogo: revisión pendiente del cliente, nombre/marca del Excel y 15 catálogos nuevos")
w("CREATE TABLE #Cat(Code nvarchar(50) PRIMARY KEY, Name nvarchar(200), Category int, UtensilType int NULL, Brand nvarchar(100), Model nvarchar(100), Descr nvarchar(2000), SourceRow int);")
for c in cats:
    category = 0 if low(c["Categoria"]) == "equipo" else None
    ut_raw = low(c["TipoUtensilio"])
    utensil = 0 if ut_raw == "no aplica" else None
    if ut_raw and ut_raw != "no aplica":
        issue("VALUE_PENDING_CONFIRMATION" if ut_raw in PENDING else "UNMAPPED_VALUE", "Equipment", c["CatalogoCodigo"], "TipoUtensilio",
              c["TipoUtensilio"], None, "Tipo de utensilio sin equivalente en el catálogo de la app; queda vacío.", 0, "CatalogoEquipos", c["_row"])
    if low(c["Estado"]) in PENDING or low(c["Categoria"]) in PENDING:
        issue("VALUE_PENDING_CONFIRMATION", "Equipment", c["CatalogoCodigo"], "Estado/Categoria", c["Estado"], "PendingClient",
              "Clasificación pendiente de confirmación del cliente.", 0, "CatalogoEquipos", c["_row"])
    w(f"INSERT #Cat VALUES({q(c['CatalogoCodigo'])}, {q(c['NombreGenerico'])}, {category if category is not None else 'NULL'}, "
      f"{utensil if utensil is not None else 'NULL'}, {q(c['Marca'])}, {q(c['Modelo'])}, {q(c['Especificaciones'])}, {c['_row']});")
w("""BEGIN TRANSACTION;
INSERT #Issue SELECT 'CONTENT_CONFLICT', 'Equipment', e.CatalogCode, 'NombreGenerico/Marca', CONCAT(e.Name, N' | ', ISNULL(e.Brand, N'')),
       CONCAT(c.Name, N' | ', ISNULL(c.Brand, N'')), N'Nombre o marca distintos a la fuente: se toma el valor del cliente.', 1, 'CatalogoEquipos', c.SourceRow
FROM dbo.Equipments e JOIN #Cat c ON c.Code = e.CatalogCode
WHERE e.Name <> c.Name OR ISNULL(e.Brand, N'') <> ISNULL(c.Brand, N'');  -- intercalación CI: solo diferencias reales, no de mayúsculas
UPDATE e SET Name = c.Name, Brand = c.Brand, Model = COALESCE(c.Model, e.Model), Description = COALESCE(c.Descr, e.Description),
       Category = COALESCE(c.Category, e.Category), UtensilType = c.UtensilType, TypeClassification = NULL,
       ClassificationReviewStatus = 1, ImportBatchId = COALESCE(e.ImportBatchId, @Batch), LastModifiedDate = @Now
FROM dbo.Equipments e JOIN #Cat c ON c.Code = e.CatalogCode
WHERE e.ClassificationReviewStatus <> 1 OR e.Name <> c.Name COLLATE Latin1_General_CS_AS OR ISNULL(e.Brand, N'') <> ISNULL(c.Brand, N'') COLLATE Latin1_General_CS_AS
   OR ISNULL(e.UtensilType, -1) <> ISNULL(c.UtensilType, -1) OR e.TypeClassification IS NOT NULL;
INSERT dbo.Equipments(Category, UtensilType, TypeClassification, Status, Name, Brand, Model, Description, CreatedDate, ImportBatchId, CatalogCode, ClassificationReviewStatus)
SELECT COALESCE(c.Category, 0), c.UtensilType, NULL, 0, c.Name, c.Brand, c.Model, c.Descr, @Now, @Batch, c.Code, 1
FROM #Cat c WHERE NOT EXISTS (SELECT 1 FROM dbo.Equipments e WHERE e.CatalogCode = c.Code);
INSERT #Issue SELECT 'LEGACY_ONLY_IN_DB', 'Equipment', e.CatalogCode, NULL, NULL, NULL, N'Catálogo que no está en la base del cliente; se conserva.', 1, NULL, NULL
FROM dbo.Equipments e WHERE e.CatalogCode IS NOT NULL AND NOT EXISTS (SELECT 1 FROM #Cat c WHERE c.Code = e.CatalogCode);
COMMIT TRANSACTION;""")

# ---------- 5. Unidades físicas (§3.9) ----------
w("-- 5. Unidades: estado y condición reales, valor, serie, catálogo; 97 nuevas")
w("CREATE TABLE #Unit(Inv nvarchar(50) PRIMARY KEY, Cat nvarchar(50), Serial nvarchar(100), Lab nvarchar(50), Value decimal(18,2) NULL, St int, Cond int, SourceRow int);")
rows = []
for u in units:
    inv = str(u["NumeroInventario"]).strip()
    st_raw, cond_raw = low(u["EstadoOperativo"]), low(u["CondicionFisica"])
    if st_raw not in STATUS_MAP:
        sys.exit(f"EstadoOperativo no mapeado: {u['EstadoOperativo']}")
    if cond_raw not in COND_MAP:
        sys.exit(f"CondicionFisica no mapeada: {u['CondicionFisica']}")
    if st_raw in PENDING:
        issue("VALUE_PENDING_CONFIRMATION", "EquipmentUnit", inv, "EstadoOperativo", u["EstadoOperativo"], "PorConfirmar", "Estado operativo sin confirmar en la fuente.", 0, "UnidadesFisicas", u["_row"])
    if cond_raw in PENDING:
        issue("VALUE_PENDING_CONFIRMATION", "EquipmentUnit", inv, "CondicionFisica", u["CondicionFisica"], "PorConfirmar", "Condición física sin confirmar en la fuente.", 0, "UnidadesFisicas", u["_row"])
    if low(u["EstadoUbicacion"]) == "pendiente":
        issue("LOCATION_DECLARED_PENDING", "EquipmentUnit", inv, "EstadoUbicacion", u["EstadoUbicacion"], u["LaboratorioCodigo"],
              "La fuente marca la ubicación 'Pendiente' pero asigna laboratorio; se registra el laboratorio (la BD exige Confirmada).", 0, "UnidadesFisicas", u["_row"])
    rows.append(f"({q(inv)}, {q(u['CatalogoCodigo'])}, {q(u['NumeroSerie'])}, {q(u['LaboratorioCodigo'])}, "
                f"{u['ValorAdquisicionBs'] if u['ValorAdquisicionBs'] is not None else 'NULL'}, {STATUS_MAP[st_raw]}, {COND_MAP[cond_raw]}, {u['_row']})")
for i in range(0, len(rows), 100):
    w("INSERT #Unit VALUES " + ", ".join(rows[i:i + 100]) + ";")
w("""
-- 5a. Reasignaciones de catálogo (incidencia con catálogo anterior y nuevo; la decisión de clasificación es por equipo, no por unidad)
INSERT #Issue SELECT 'CATALOG_REASSIGNED', 'EquipmentUnit', u.InventoryNumber, 'CatalogoCodigo', e.CatalogCode, x.Cat,
       N'La unidad cambia de catálogo según la fuente.', 1, 'UnidadesFisicas', x.SourceRow
FROM dbo.EquipmentUnits u JOIN dbo.Equipments e ON e.Id = u.EquipmentId JOIN #Unit x ON x.Inv = u.InventoryNumber
WHERE e.CatalogCode IS NULL OR e.CatalogCode <> x.Cat;

-- 5b. Actualizar las existentes (lotes de 100)
SET @n = 1;
WHILE @n > 0
BEGIN
    BEGIN TRANSACTION;
    UPDATE TOP (100) u SET CurrentStatus = x.St, PhysicalCondition = x.Cond, EquipmentId = ne.Id,
        AcquisitionValue = COALESCE(x.Value, u.AcquisitionValue), SerialNumber = COALESCE(x.Serial, u.SerialNumber),
        ImportBatchId = COALESCE(u.ImportBatchId, @Batch), LastModifiedDate = @Now
    FROM dbo.EquipmentUnits u JOIN #Unit x ON x.Inv = u.InventoryNumber JOIN dbo.Equipments ne ON ne.CatalogCode = x.Cat
    WHERE u.CurrentStatus <> x.St OR ISNULL(u.PhysicalCondition, -1) <> x.Cond OR u.EquipmentId <> ne.Id
       OR (x.Value IS NOT NULL AND ISNULL(u.AcquisitionValue, -1) <> x.Value) OR (x.Serial IS NOT NULL AND ISNULL(u.SerialNumber, N'') <> x.Serial);
    SET @n = @@ROWCOUNT;
    COMMIT TRANSACTION;
END;

-- 5c. Crear las nuevas (lotes de 100)
SET @n = 1;
WHILE @n > 0
BEGIN
    BEGIN TRANSACTION;
    INSERT dbo.EquipmentUnits(EquipmentId, LaboratoryId, InventoryNumber, SerialNumber, AcquisitionValue, CurrentStatus, PhysicalCondition,
                              Notes, CreatedDate, ImportBatchId, LocationResolutionStatus)
    SELECT TOP (100) ne.Id, l.Id, x.Inv, x.Serial, x.Value, x.St, x.Cond,
           N'[EXCEL] Laboratorio ' + x.Lab + N' según BaseEstructuraFinal_V4.xlsx (EstadoUbicacion en origen: Pendiente).', @Now, @Batch, 1
    FROM #Unit x JOIN dbo.Equipments ne ON ne.CatalogCode = x.Cat JOIN dbo.Laboratories l ON l.Code = x.Lab
    WHERE NOT EXISTS (SELECT 1 FROM dbo.EquipmentUnits u WHERE u.InventoryNumber = x.Inv);
    SET @n = @@ROWCOUNT;
    COMMIT TRANSACTION;
END;

-- 5d. Unidades solo en la BD
INSERT #Issue SELECT 'LEGACY_ONLY_IN_DB', 'EquipmentUnit', u.InventoryNumber, NULL, NULL, NULL, N'Unidad que no está en la base del cliente; se conserva.', 1, NULL, NULL
FROM dbo.EquipmentUnits u WHERE NOT EXISTS (SELECT 1 FROM #Unit x WHERE x.Inv = u.InventoryNumber);

-- 6. Incidencias (idempotentes por lote + código + clave + campo), en lotes de 100
SET @n = 1;
WHILE @n > 0
BEGIN
    BEGIN TRANSACTION;
    INSERT dbo.DataQualityIssues(ImportBatchId, IssueCode, EntityName, EntityKey, FieldName, OriginalValue, NormalizedValue, Description, Severity, Status, CreatedDate, SourceSheet, SourceRowNumber, CandidateKeys)
    SELECT TOP (100) @Batch, i.IssueCode, i.EntityName, i.EntityKey, i.FieldName, i.OriginalValue, i.NormalizedValue, i.Description, i.Severity, 0, @Now, i.SourceSheet, i.SourceRow,
           CASE WHEN i.IssueCode = 'POSSIBLE_PERSON_DUPLICATE' THEN i.NormalizedValue END
    FROM (SELECT DISTINCT * FROM #Issue) i
    WHERE NOT EXISTS (SELECT 1 FROM dbo.DataQualityIssues d WHERE d.ImportBatchId = @Batch AND d.IssueCode = i.IssueCode
                      AND ISNULL(d.EntityKey, N'') = ISNULL(i.EntityKey, N'') AND ISNULL(d.FieldName, N'') = ISNULL(i.FieldName, N''));
    SET @n = @@ROWCOUNT;
    COMMIT TRANSACTION;
END;

-- Verificación
SELECT 'gestiones' k, COUNT(*) n FROM dbo.Managements
UNION ALL SELECT 'catalogos con codigo', COUNT(*) FROM dbo.Equipments WHERE CatalogCode IS NOT NULL
UNION ALL SELECT 'catalogos PendingClient', COUNT(*) FROM dbo.Equipments WHERE ClassificationReviewStatus = 1
UNION ALL SELECT 'unidades', COUNT(*) FROM dbo.EquipmentUnits
UNION ALL SELECT 'unidades Por confirmar', COUNT(*) FROM dbo.EquipmentUnits WHERE CurrentStatus = 98
UNION ALL SELECT 'unidades Operativo', COUNT(*) FROM dbo.EquipmentUnits WHERE CurrentStatus = 0
UNION ALL SELECT 'unidades De baja', COUNT(*) FROM dbo.EquipmentUnits WHERE CurrentStatus = 3
UNION ALL SELECT 'Operativo pero Excel Por confirmar', COUNT(*) FROM dbo.EquipmentUnits u JOIN #Unit x ON x.Inv = u.InventoryNumber WHERE x.St = 98 AND u.CurrentStatus = 0
UNION ALL SELECT 'actores con titulo', COUNT(*) FROM dbo.People WHERE Title IS NOT NULL AND ActorCode LIKE N'ACT-%'
UNION ALL SELECT 'incidencias lote V4 maestros', COUNT(*) FROM dbo.DataQualityIssues WHERE ImportBatchId = @Batch;
SELECT IssueCode, COUNT(*) n FROM dbo.DataQualityIssues WHERE ImportBatchId = @Batch GROUP BY IssueCode ORDER BY IssueCode;
""")

io.open(OUT, "w", encoding="utf-8").write("\n".join(sql) + "\n")
print(OUT, "| labs", len(labs), "| gestiones", len(mgmts), "| catalogos", len(cats), "| unidades", len(units))
