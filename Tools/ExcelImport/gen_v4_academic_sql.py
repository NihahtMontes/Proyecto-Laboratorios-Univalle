"""
V4 · Bloque 1 · académico (MAPA_EXCEL_V4_A_BD.md §3.18–3.25).
Genera import_v4_academic.sql para DB_Laboratorios_Univalle_DEMO_QA ÚNICAMENTE.
- Materias (19) por código; las 6 homologadas LCG -> LGS llevan incidencia (decisión D3).
- Retiro lógico de datos académicos y de catálogo demo (sin borrar).
- Prácticas, recetas, existencias y programación (+ grupos derivados, D4): soportadas con encabezados validados.
  En la V4 esas hojas vienen vacías; el SQL correspondiente se genera solo si hay filas.
"""
import collections
import hashlib
import io
import os
import sys

import openpyxl

XLSX = r"C:\Users\monte\Downloads\BaseEstructuraFinal_V4.xlsx"
EXPECTED_SHA = "8ebb238c24a7ce94a71ff0506675fdc216db8d2831b7c24d27f7aab5ff1eb9d2"
OUT = os.path.join(os.environ.get("TEMP", "."), "mm", "import_v4_academic.sql")
HOMOLOGATED_LCG = {"BAS100", "COM100", "FUN100", "TSC300", "PAN300", "TAL300"}

sha = hashlib.sha256(open(XLSX, "rb").read()).hexdigest()
if sha != EXPECTED_SHA:
    sys.exit(f"Hash distinto: {sha}")
wb = openpyxl.load_workbook(XLSX, read_only=True, data_only=True)

HEADERS = {
    "Materias": ("MateriaCodigo", "Nombre", "CarreraCodigo", "Semestre", "Estado"),
    "Practicas": ("PracticaCodigo", "MateriaCodigo", "NumeroPractica", "Descripcion", "EstadoCobertura", "MetodoCobertura",
                  "FuenteReferencia", "Observaciones"),
    "ProgramacionPracticas": ("ProgramacionCodigo", "GestionCodigo", "MateriaCodigo", "PracticaCodigo", "Grupo", "DocenteActorCodigo",
                              "LaboratorioCodigo", "Semana", "FechaEjecucion", "HoraInicio", "HoraFin", "CantEstudiantes",
                              "EsDemostrativa", "FactorMateria", "Estado"),
    "RequerimientosPractica": ("RequerimientoCodigo", "PracticaCodigo", "ArticuloCodigo", "Cantidad", "Escalable", "EstadoValidacion",
                               "Observaciones"),
    "ExistenciasArticulos": ("LaboratorioCodigo", "ArticuloCodigo", "CantidadDisponible", "FechaActualizacion"),
}


def sheet(name):
    it = wb[name].iter_rows(values_only=True)
    header = tuple(next(it))
    if header[:len(HEADERS[name])] != HEADERS[name]:
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
    if isinstance(v, bool):
        return "1" if v else "0"
    if isinstance(v, (int, float)):
        return str(v)
    if hasattr(v, "strftime"):
        return "'" + v.strftime("%Y-%m-%d") + "'"
    return "N'" + str(v).strip().replace("'", "''") + "'"


def q_time(v):
    if v is None or v == "":
        return "NULL"
    if hasattr(v, "strftime"):
        return "'" + v.strftime("%H:%M") + "'"
    return "'" + str(v).strip()[:5] + "'"


def as_bool(v):
    s = str(v or "").strip().lower()
    if s in ("sí", "si", "true", "1", "x", "verdadero"):
        return "1"
    if s in ("no", "false", "0", "falso"):
        return "0"
    return "NULL"


SCHEDULE_STATUS = {"programada": 0, "ejecutada": 1, "reprogramada": 2, "cancelada": 3}

subjects = sheet("Materias")
practices = sheet("Practicas")
schedules = sheet("ProgramacionPracticas")
requirements = sheet("RequerimientosPractica")
stocks = sheet("ExistenciasArticulos")

sql = []
w = sql.append
w("SET NOCOUNT ON;")
w("SET XACT_ABORT ON;")
w("IF DB_NAME() <> N'DB_Laboratorios_Univalle_DEMO_QA' THROW 50000, 'Destino incorrecto: solo DEMO_QA', 1;")
w("DECLARE @Now datetime2 = SYSUTCDATETIME();")
w("DECLARE @n int;")
batch_code = "XLSX-V4-ACADEMIC-" + sha[:12].upper()
w(f"IF NOT EXISTS (SELECT 1 FROM dbo.ImportBatches WHERE Code = N'{batch_code}') "
  f"INSERT dbo.ImportBatches(Code, SourceType, SourceName, SourceSha256, ImportedAt, Status, Notes, ContractVersion) "
  f"VALUES(N'{batch_code}', N'Excel', N'BaseEstructuraFinal_V4.xlsx', N'{sha}', @Now, 1, "
  f"N'V4 bloque 1: materias, retiro de demo académico y soporte de programación/recetas/existencias. Solo DEMO_QA.', N'base-estructural-v4');")
w(f"DECLARE @Batch int = (SELECT Id FROM dbo.ImportBatches WHERE Code = N'{batch_code}');")
w("CREATE TABLE #Issue(IssueCode nvarchar(60), EntityName nvarchar(60), EntityKey nvarchar(100), FieldName nvarchar(100), "
  "OriginalValue nvarchar(400), NormalizedValue nvarchar(400), Description nvarchar(500), Severity int, SourceSheet nvarchar(60), SourceRow int);")


def issue(code, entity, key, field, original, normalized, desc, severity, sheet_name, row):
    w(f"INSERT #Issue VALUES({q(code)}, {q(entity)}, {q(key)}, {q(field)}, {q(original)}, {q(normalized)}, {q(desc)}, {severity}, {q(sheet_name)}, {row or 'NULL'});")


# ---------- 0. Dependencias de los datos demo (reporte previo) ----------
w("""-- 0. Dependencias de los datos demo antes de retirarlos
SELECT 'L3Requests sobre materias demo' k, COUNT(*) n FROM dbo.L3Requests r JOIN dbo.Subjects s ON s.Id = r.SubjectId WHERE s.Code LIKE N'GAS-%'
UNION ALL SELECT 'PracticeSchedules sobre materias demo', COUNT(*) FROM dbo.PracticeSchedules p JOIN dbo.Subjects s ON s.Id = p.SubjectId WHERE s.Code LIKE N'GAS-%'
UNION ALL SELECT 'Articulos demo usados en salidas', COUNT(*) FROM dbo.DepartureItems i JOIN dbo.Articles a ON a.Id = i.ArticleId WHERE a.Code LIKE N'ART-000%';""")

# ---------- 1. Materias ----------
w("-- 1. Materias del cliente")
w("BEGIN TRANSACTION;")
for s in subjects:
    code = s["MateriaCodigo"]
    career = q(s["CarreraCodigo"])
    status = 0 if str(s["Estado"]).strip().lower() == "activo" else 1
    w(f"IF NOT EXISTS (SELECT 1 FROM dbo.Subjects WHERE Code = {q(code)}) "
      f"INSERT dbo.Subjects(Name, Code, CareerId, Semester, Status) VALUES({q(s['Nombre'])}, {q(code)}, (SELECT Id FROM dbo.Careers WHERE Code = {career}), {q(s['Semestre'])}, {status});")
    w(f"UPDATE dbo.Subjects SET Name = {q(s['Nombre'])}, CareerId = (SELECT Id FROM dbo.Careers WHERE Code = {career}), Semester = {q(s['Semestre'])}, Status = {status} "
      f"WHERE Code = {q(code)} AND (Name <> {q(s['Nombre'])} OR ISNULL(Semester, -1) <> {q(s['Semestre'])} OR Status <> {status} "
      f"OR ISNULL(CareerId, -1) <> ISNULL((SELECT Id FROM dbo.Careers WHERE Code = {career}), -1));")
    if code in HOMOLOGATED_LCG:
        issue("SUBJECT_CAREER_HOMOLOGATED", "Subject", code, "CarreraCodigo", "LCG", s["CarreraCodigo"],
              "La planificación original indicaba LCG; la V4 la homologó a LGS. Confirmar con la cliente (D3).", 1, "Materias", s["_row"])
w("COMMIT TRANSACTION;")

# ---------- 2. Retiro lógico de datos demo ----------
w("""-- 2. Retiro lógico de datos demo (nada se borra)
BEGIN TRANSACTION;
UPDATE r SET IsRemoved = 1, LastModifiedDate = @Now
FROM dbo.PracticeRequirements r JOIN dbo.Practices p ON p.Id = r.PracticeId WHERE p.Code LIKE N'PRA-GAS%' AND r.IsRemoved = 0;
UPDATE dbo.Practices SET Status = 2, LastModifiedDate = @Now WHERE Code LIKE N'PRA-GAS%' AND Status <> 2;
UPDATE dbo.CourseGroups SET Status = 2, LastModifiedDate = @Now WHERE Code LIKE N'GRP-2026-2-GAS%' AND Status <> 2;
UPDATE dbo.Subjects SET Status = 2 WHERE Code LIKE N'GAS-%' AND Status <> 2;
UPDATE a SET Status = 2, LastModifiedDate = @Now FROM dbo.Articles a
WHERE a.Code LIKE N'ART-000%' AND a.Status <> 2
  AND NOT EXISTS (SELECT 1 FROM dbo.DepartureItems i WHERE i.ArticleId = a.Id)
  AND NOT EXISTS (SELECT 1 FROM dbo.PracticeRequirements r WHERE r.ArticleId = a.Id AND r.IsRemoved = 0);
UPDATE c SET Status = 1 FROM dbo.ArticleCategories c
WHERE c.Code IN (N'UTEN', N'VAJI', N'CONS') AND c.Status = 0 AND NOT EXISTS (SELECT 1 FROM dbo.Articles a WHERE a.ArticleCategoryId = c.Id AND a.Status <> 2);
UPDATE u SET Status = 1 FROM dbo.MeasurementUnits u
WHERE u.Code IN (N'KG', N'LT', N'ROL') AND u.Status = 0 AND NOT EXISTS (SELECT 1 FROM dbo.Articles a WHERE a.MeasurementUnitId = u.Id AND a.Status <> 2);
COMMIT TRANSACTION;""")

# ---------- 3. Prácticas ----------
if practices:
    w("-- 3. Prácticas")
    w("CREATE TABLE #Prac(Code nvarchar(50) PRIMARY KEY, Subj nvarchar(50), Num nvarchar(20), Descr nvarchar(2000), CovS nvarchar(40), CovM nvarchar(60), Src nvarchar(500), Obs nvarchar(2000), SourceRow int);")
    for p in practices:
        w(f"INSERT #Prac VALUES({q(p['PracticaCodigo'])}, {q(p['MateriaCodigo'])}, {q(p['NumeroPractica'])}, {q(p['Descripcion'])}, {q(p['EstadoCobertura'])}, "
          f"{q(p['MetodoCobertura'])}, {q(p['FuenteReferencia'])}, {q(p['Observaciones'])}, {p['_row']});")
    w("""INSERT #Issue SELECT 'UNRESOLVED_REFERENCE', 'Practice', p.Code, 'MateriaCodigo', p.Subj, NULL, N'Materia inexistente; la práctica no se carga.', 2, 'Practicas', p.SourceRow
FROM #Prac p WHERE NOT EXISTS (SELECT 1 FROM dbo.Subjects s WHERE s.Code = p.Subj);
BEGIN TRANSACTION;
UPDATE x SET SubjectId = s.Id, Number = p.Num, Description = p.Descr, CoverageStatus = p.CovS, CoverageMethod = p.CovM, SourceReference = p.Src, Observations = p.Obs, LastModifiedDate = @Now
FROM dbo.Practices x JOIN #Prac p ON p.Code = x.Code JOIN dbo.Subjects s ON s.Code = p.Subj;
INSERT dbo.Practices(Code, SubjectId, Number, Description, CoverageStatus, CoverageMethod, SourceReference, Observations, Status, CreatedDate)
SELECT p.Code, s.Id, p.Num, p.Descr, p.CovS, p.CovM, p.Src, p.Obs, 0, @Now FROM #Prac p JOIN dbo.Subjects s ON s.Code = p.Subj
WHERE NOT EXISTS (SELECT 1 FROM dbo.Practices x WHERE x.Code = p.Code);
COMMIT TRANSACTION;""")

# ---------- 4. Recetas ----------
if requirements:
    w("-- 4. Recetas (RequerimientosPractica)")
    w("CREATE TABLE #Req(Code nvarchar(50) PRIMARY KEY, Prac nvarchar(50), Art nvarchar(30), Qty decimal(12,2) NULL, Scal bit NULL, Val nvarchar(40), Obs nvarchar(1000), SourceRow int);")
    for r in requirements:
        if r["Cantidad"] is None:
            issue("VALUE_MISSING", "PracticeRequirement", r["RequerimientoCodigo"], "Cantidad", None, None,
                  "La fuente no trae cantidad; se carga sin cantidad (no se inventa).", 1, "RequerimientosPractica", r["_row"])
        w(f"INSERT #Req VALUES({q(r['RequerimientoCodigo'])}, {q(r['PracticaCodigo'])}, {q(r['ArticuloCodigo'])}, {q(r['Cantidad'])}, "
          f"{as_bool(r['Escalable'])}, {q(r['EstadoValidacion'])}, {q(r['Observaciones'])}, {r['_row']});")
    w("""INSERT #Issue SELECT 'UNRESOLVED_REFERENCE', 'PracticeRequirement', r.Code, 'PracticaCodigo/ArticuloCodigo', CONCAT(r.Prac, N' / ', r.Art), NULL,
       N'Práctica o artículo inexistente; la línea no se carga.', 2, 'RequerimientosPractica', r.SourceRow
FROM #Req r WHERE NOT EXISTS (SELECT 1 FROM dbo.Practices p WHERE p.Code = r.Prac) OR NOT EXISTS (SELECT 1 FROM dbo.Articles a WHERE a.Code = r.Art);
SET @n = 1;
WHILE @n > 0
BEGIN
    BEGIN TRANSACTION;
    INSERT dbo.PracticeRequirements(Code, PracticeId, ArticleId, Quantity, IsScalable, ValidationStatus, Observations, IsRemoved, CreatedDate)
    SELECT TOP (100) r.Code, p.Id, a.Id, r.Qty, ISNULL(r.Scal, 0), r.Val, r.Obs, 0, @Now
    FROM #Req r JOIN dbo.Practices p ON p.Code = r.Prac JOIN dbo.Articles a ON a.Code = r.Art
    WHERE NOT EXISTS (SELECT 1 FROM dbo.PracticeRequirements x WHERE x.Code = r.Code)
      AND NOT EXISTS (SELECT 1 FROM dbo.PracticeRequirements x WHERE x.PracticeId = p.Id AND x.ArticleId = a.Id AND x.IsRemoved = 0);
    SET @n = @@ROWCOUNT;
    COMMIT TRANSACTION;
END;""")

# ---------- 5. Existencias ----------
if stocks:
    w("-- 5. Existencias por laboratorio")
    w("CREATE TABLE #Stk(Lab nvarchar(50), Art nvarchar(30), Qty decimal(12,2) NULL, Upd date NULL, SourceRow int);")
    for s in stocks:
        w(f"INSERT #Stk VALUES({q(s['LaboratorioCodigo'])}, {q(s['ArticuloCodigo'])}, {q(s['CantidadDisponible'])}, {q(s['FechaActualizacion'])}, {s['_row']});")
    w("""INSERT #Issue SELECT 'UNRESOLVED_REFERENCE', 'ArticleStock', CONCAT(s.Lab, N'/', s.Art), 'LaboratorioCodigo/ArticuloCodigo', NULL, NULL,
       N'Laboratorio o artículo inexistente; la existencia no se carga.', 2, 'ExistenciasArticulos', s.SourceRow
FROM #Stk s WHERE NOT EXISTS (SELECT 1 FROM dbo.Laboratories l WHERE l.Code = s.Lab) OR NOT EXISTS (SELECT 1 FROM dbo.Articles a WHERE a.Code = s.Art);
BEGIN TRANSACTION;
UPDATE x SET AvailableQuantity = ISNULL(s.Qty, 0), UpdatedOn = s.Upd, LastModifiedDate = @Now
FROM dbo.ArticleStocks x JOIN dbo.Laboratories l ON l.Id = x.LaboratoryId JOIN dbo.Articles a ON a.Id = x.ArticleId
JOIN #Stk s ON s.Lab = l.Code AND s.Art = a.Code WHERE s.Qty IS NOT NULL;
INSERT dbo.ArticleStocks(LaboratoryId, ArticleId, AvailableQuantity, UpdatedOn, CreatedDate)
SELECT l.Id, a.Id, s.Qty, s.Upd, @Now FROM #Stk s JOIN dbo.Laboratories l ON l.Code = s.Lab JOIN dbo.Articles a ON a.Code = s.Art
WHERE s.Qty IS NOT NULL AND NOT EXISTS (SELECT 1 FROM dbo.ArticleStocks x WHERE x.LaboratoryId = l.Id AND x.ArticleId = a.Id);
COMMIT TRANSACTION;""")

# ---------- 6. Programación + grupos derivados (D4) ----------
if schedules:
    w("-- 6. Programación de prácticas y grupos derivados")
    w("CREATE TABLE #Sch(Code nvarchar(50) PRIMARY KEY, Mgmt nvarchar(50), Subj nvarchar(50), Prac nvarchar(50), Grp nvarchar(20), Teacher nvarchar(50), Lab nvarchar(50), "
      "Week int NULL, ExecDate date NULL, T1 time NULL, T2 time NULL, Students int NULL, Demo bit NULL, Factor int NULL, St int, SourceRow int);")
    combos = collections.defaultdict(list)
    for s in schedules:
        st_raw = str(s["Estado"] or "").strip().lower()
        st = SCHEDULE_STATUS.get(st_raw, 9)
        if st == 9:
            issue("VALUE_PENDING_CONFIRMATION", "PracticeSchedule", s["ProgramacionCodigo"], "Estado", s["Estado"], "PorConfirmar",
                  "Estado de la programación no reconocido o sin confirmar.", 0, "ProgramacionPracticas", s["_row"])
        if not s["Grupo"]:
            issue("VALUE_MISSING", "PracticeSchedule", s["ProgramacionCodigo"], "Grupo", None, None,
                  "Programación sin grupo: no se carga (rompería la unicidad gestión + práctica + grupo).", 2, "ProgramacionPracticas", s["_row"])
            continue
        w(f"INSERT #Sch VALUES({q(s['ProgramacionCodigo'])}, {q(s['GestionCodigo'])}, {q(s['MateriaCodigo'])}, {q(s['PracticaCodigo'])}, {q(s['Grupo'])}, "
          f"{q(s['DocenteActorCodigo'])}, {q(s['LaboratorioCodigo'])}, {q(s['Semana'])}, {q(s['FechaEjecucion'])}, {q_time(s['HoraInicio'])}, {q_time(s['HoraFin'])}, "
          f"{q(s['CantEstudiantes'])}, {as_bool(s['EsDemostrativa'])}, {q(s['FactorMateria'])}, {st}, {s['_row']});")
        combos[(s["GestionCodigo"], s["MateriaCodigo"], s["Grupo"])].append(s)
    # Grupos derivados: valor más frecuente de cada campo habitual; incidencia si las filas difieren.
    w("CREATE TABLE #Grp(Code nvarchar(50) PRIMARY KEY, Mgmt nvarchar(50), Subj nvarchar(50), Grp nvarchar(20), Teacher nvarchar(50), Lab nvarchar(50), "
      "T1 time NULL, T2 time NULL, Students int NULL, Demo bit NULL, Factor int NULL);")
    for (mgmt, subj, grp), rows in combos.items():
        if not grp:
            continue
        code = f"GRP-{mgmt}-{subj}-{grp}"
        picked = {}
        for field in ("DocenteActorCodigo", "LaboratorioCodigo", "HoraInicio", "HoraFin", "CantEstudiantes", "EsDemostrativa", "FactorMateria"):
            values = collections.Counter(str(r[field]) for r in rows if r[field] is not None)
            picked[field] = next((r[field] for r in rows if r[field] is not None and str(r[field]) == values.most_common(1)[0][0]), None) if values else None
            if len(values) > 1:
                issue("COURSE_GROUP_VALUES_VARY", "CourseGroup", code, field, None, ", ".join(values),
                      "Las programaciones del grupo tienen valores distintos; el grupo toma el más frecuente y cada programación conserva el suyo.", 0, "ProgramacionPracticas", None)
        w(f"INSERT #Grp VALUES({q(code)}, {q(mgmt)}, {q(subj)}, {q(grp)}, {q(picked['DocenteActorCodigo'])}, {q(picked['LaboratorioCodigo'])}, "
          f"{q_time(picked['HoraInicio'])}, {q_time(picked['HoraFin'])}, {q(picked['CantEstudiantes'])}, {as_bool(picked['EsDemostrativa'])}, {q(picked['FactorMateria'])});")
    w("""INSERT #Issue SELECT 'UNRESOLVED_REFERENCE', 'PracticeSchedule', s.Code, 'Gestion/Materia/Practica/Docente', CONCAT(s.Mgmt, N' / ', s.Subj, N' / ', s.Prac, N' / ', s.Teacher), NULL,
       N'Referencia inexistente; la programación no se carga.', 2, 'ProgramacionPracticas', s.SourceRow
FROM #Sch s WHERE NOT EXISTS (SELECT 1 FROM dbo.Managements m WHERE m.Code = s.Mgmt) OR NOT EXISTS (SELECT 1 FROM dbo.Subjects x WHERE x.Code = s.Subj)
   OR NOT EXISTS (SELECT 1 FROM dbo.Practices p WHERE p.Code = s.Prac) OR NOT EXISTS (SELECT 1 FROM dbo.People p WHERE p.ActorCode = s.Teacher);
INSERT #Issue SELECT 'CONTENT_CONFLICT', 'PracticeSchedule', s.Code, 'PracticaCodigo', s.Prac, s.Subj, N'La práctica pertenece a otra materia.', 1, 'ProgramacionPracticas', s.SourceRow
FROM #Sch s JOIN dbo.Practices p ON p.Code = s.Prac JOIN dbo.Subjects x ON x.Code = s.Subj WHERE p.SubjectId <> x.Id;
INSERT #Issue SELECT 'CONTENT_CONFLICT', 'PracticeSchedule', s.Code, 'Gestion/Practica/Grupo', z.Code, NULL,
       N'Ya existe otra programación con la misma gestión, práctica y grupo; esta fila no se carga.', 2, 'ProgramacionPracticas', s.SourceRow
FROM #Sch s JOIN dbo.Managements m ON m.Code = s.Mgmt JOIN dbo.Practices pr ON pr.Code = s.Prac
JOIN dbo.PracticeSchedules z ON z.ManagementId = m.Id AND z.PracticeId = pr.Id AND z.GroupName = s.Grp AND z.IsDeleted = 0 AND z.Code <> s.Code;
INSERT #Issue SELECT 'TEACHER_WITHOUT_ROLE', 'PracticeSchedule', s.Code, 'DocenteActorCodigo', s.Teacher, NULL, N'El actor no tiene rol Docente activo.', 1, 'ProgramacionPracticas', s.SourceRow
FROM #Sch s JOIN dbo.People p ON p.ActorCode = s.Teacher
WHERE NOT EXISTS (SELECT 1 FROM dbo.PersonRoleAssignments r WHERE r.PersonId = p.Id AND r.Role = 8 AND r.IsActive = 1);
BEGIN TRANSACTION;
INSERT dbo.CourseGroups(Code, ManagementId, SubjectId, Name, TeacherPersonId, LaboratoryId, StartTime, EndTime, StudentCount, IsDemonstrative, ScalingFactor, Status, CreatedDate)
SELECT g.Code, m.Id, x.Id, g.Grp, p.Id, l.Id, g.T1, g.T2, g.Students, ISNULL(g.Demo, 0), g.Factor, 0, @Now
FROM #Grp g JOIN dbo.Managements m ON m.Code = g.Mgmt JOIN dbo.Subjects x ON x.Code = g.Subj JOIN dbo.People p ON p.ActorCode = g.Teacher
LEFT JOIN dbo.Laboratories l ON l.Code = g.Lab
WHERE NOT EXISTS (SELECT 1 FROM dbo.CourseGroups c WHERE c.Code = g.Code);
COMMIT TRANSACTION;
SET @n = 1;
WHILE @n > 0
BEGIN
    BEGIN TRANSACTION;
    INSERT dbo.PracticeSchedules(Code, ManagementId, SubjectId, PracticeId, CourseGroupId, GroupName, TeacherPersonId, LaboratoryId, WeekNumber, AcademicWeekId,
        ExecutionDate, StartTime, EndTime, StudentCount, IsDemonstrative, ScalingFactor, Status, ImportBatchId, CreatedDate)
    SELECT TOP (100) s.Code, m.Id, x.Id, pr.Id, cg.Id, s.Grp, pe.Id, l.Id, s.Week,
        (SELECT TOP 1 w.Id FROM dbo.AcademicWeeks w JOIN dbo.AcademicPlannings ap ON ap.Id = w.AcademicPlanningId
         WHERE ap.ManagementId = m.Id AND ap.CareerId = x.CareerId AND w.Number = s.Week),
        s.ExecDate, s.T1, s.T2, s.Students, s.Demo, s.Factor, s.St, @Batch, @Now
    FROM #Sch s JOIN dbo.Managements m ON m.Code = s.Mgmt JOIN dbo.Subjects x ON x.Code = s.Subj JOIN dbo.Practices pr ON pr.Code = s.Prac AND pr.SubjectId = x.Id
    JOIN dbo.People pe ON pe.ActorCode = s.Teacher LEFT JOIN dbo.Laboratories l ON l.Code = s.Lab
    LEFT JOIN dbo.CourseGroups cg ON cg.Code = CONCAT(N'GRP-', s.Mgmt, N'-', s.Subj, N'-', s.Grp)
    WHERE NOT EXISTS (SELECT 1 FROM dbo.PracticeSchedules z WHERE z.Code = s.Code)
      AND NOT EXISTS (SELECT 1 FROM dbo.PracticeSchedules z WHERE z.ManagementId = m.Id AND z.PracticeId = pr.Id AND z.GroupName = s.Grp AND z.IsDeleted = 0)
      AND s.Code = (SELECT MIN(d.Code) FROM #Sch d WHERE d.Mgmt = s.Mgmt AND d.Prac = s.Prac AND d.Grp = s.Grp);
    SET @n = @@ROWCOUNT;
    COMMIT TRANSACTION;
END;""")

# ---------- 7. Incidencias ----------
w("""-- 7. Incidencias (idempotentes)
SET @n = 1;
WHILE @n > 0
BEGIN
    BEGIN TRANSACTION;
    INSERT dbo.DataQualityIssues(ImportBatchId, IssueCode, EntityName, EntityKey, FieldName, OriginalValue, NormalizedValue, Description, Severity, Status, CreatedDate, SourceSheet, SourceRowNumber)
    SELECT TOP (100) @Batch, i.IssueCode, i.EntityName, i.EntityKey, i.FieldName, i.OriginalValue, i.NormalizedValue, i.Description, i.Severity, 0, @Now, i.SourceSheet, i.SourceRow
    FROM (SELECT DISTINCT * FROM #Issue) i
    WHERE NOT EXISTS (SELECT 1 FROM dbo.DataQualityIssues d WHERE d.ImportBatchId = @Batch AND d.IssueCode = i.IssueCode
                      AND ISNULL(d.EntityKey, N'') = ISNULL(i.EntityKey, N'') AND ISNULL(d.FieldName, N'') = ISNULL(i.FieldName, N''));
    SET @n = @@ROWCOUNT;
    COMMIT TRANSACTION;
END;

-- Verificación
SELECT 'materias cliente activas' k, COUNT(*) n FROM dbo.Subjects WHERE Code NOT LIKE N'GAS-%' AND Status = 0
UNION ALL SELECT 'materias demo activas', COUNT(*) FROM dbo.Subjects WHERE Code LIKE N'GAS-%' AND Status = 0
UNION ALL SELECT 'practicas demo activas', COUNT(*) FROM dbo.Practices WHERE Code LIKE N'PRA-GAS%' AND Status = 0
UNION ALL SELECT 'grupos demo activos', COUNT(*) FROM dbo.CourseGroups WHERE Code LIKE N'GRP-2026-2-GAS%' AND Status <> 2
UNION ALL SELECT 'articulos demo activos', COUNT(*) FROM dbo.Articles WHERE Code LIKE N'ART-000%' AND Status <> 2
UNION ALL SELECT 'programaciones', COUNT(*) FROM dbo.PracticeSchedules
UNION ALL SELECT 'incidencias lote academico', COUNT(*) FROM dbo.DataQualityIssues WHERE ImportBatchId = @Batch;""")

io.open(OUT, "w", encoding="utf-8").write("\n".join(sql) + "\n")
print(OUT, "| materias", len(subjects), "| practicas", len(practices), "| programaciones", len(schedules),
      "| recetas", len(requirements), "| existencias", len(stocks))
