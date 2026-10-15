"""
Importador único V12, parte B2: Materias, Practicas, CategoriasArticulo, UnidadesMedida, Articulos,
RequerimientosPractica, ExistenciasArticulos y Planificacion (programación 2027-1 + grupos derivados)
-> DB_Laboratorios_Univalle_REAL. Requiere la parte A (carreras, laboratorios, gestiones, actores).

Textos tal cual del Excel. Columnas sin campo propio quedan en ImportSourceRows.OriginalDataJson.
Uso:  python gen_v12_full_b2.py [--dry-run]   ->  %TEMP%\\v12full\\parte_b2.sql
"""
import sys
from collections import Counter, defaultdict

from v12_common import SqlBuilder, check_len, check_sha, q, read_sheets, text
from gen_v12_full_b1 import as_date  # noqa: E402  (misma conversión de fechas)

SHEETS = ["Materias", "Practicas", "CategoriasArticulo", "UnidadesMedida", "Articulos", "RequerimientosPractica",
          "ExistenciasArticulos", "Planificacion"]
SCHEDULE_STATUS = {"programada": 0, "ejecutada": 1, "reprogramada": 2, "cancelada": 3}


def low(v):
    return (text(v) or "").casefold()


def yes_no(v):
    s = low(v)
    if s in ("sí", "si", "true", "1"):
        return 1
    if s in ("no", "false", "0"):
        return 0
    return None


def tstr(v):
    return None if v is None else v.strftime("%H:%M")


def main(dry_run):
    sha = check_sha()
    data = read_sheets(SHEETS)
    for name, (_, rows) in data.items():
        print(f"[hoja] {name}: {len(rows)} filas")
    if dry_run:
        return

    s = SqlBuilder()
    s.prologue(sha, "Importación completa V12 (parte B2: académico, artículos, recetas, existencias y programación 2027-1).")
    sw = s.stmt

    # ---- Materias ----
    hdr, rows = data["Materias"]
    s.w("-- Materias")
    s.begin_group()
    for r in rows:
        c = r["Materiacodigo"]
        check_len("Materias", r, "Nombre", r["Nombre"], 255)
        st = 0 if low(r["Estado"]) == "activo" else 1
        sw(f"IF NOT EXISTS (SELECT 1 FROM dbo.Subjects WHERE Code = {q(c)}) "
           f"INSERT dbo.Subjects(Code, Name, CareerId, Semester, Status) "
           f"VALUES({q(c)}, {q(r['Nombre'])}, (SELECT Id FROM dbo.Careers WHERE Code = {q(r['Carreracodigo'])}), {q(r['Semestre'])}, {st});")
        s.source_row("Materias", hdr, r, c, "Subject", c)
    s.end_group()

    # ---- Prácticas ----
    hdr, rows = data["Practicas"]
    s.w("-- Prácticas")
    s.begin_group()
    for r in rows:
        c = r["Practicacodigo"]
        desc = text(r["Descripcion"]) or ""
        check_len("Practicas", r, "Descripcion", desc, 2000)
        check_len("Practicas", r, "Observaciones", r["Observaciones"], 2000)
        if low(r["Estado"]) != "activo":
            sys.exit(f"Practica {c}: estado no mapeado {r['Estado']}")
        sw(f"IF NOT EXISTS (SELECT 1 FROM dbo.Practices WHERE Code = {q(c)}) "
           f"INSERT dbo.Practices(Code, SubjectId, Name, Number, Description, Observations, Status, CreatedDate) "
           f"VALUES({q(c)}, (SELECT Id FROM dbo.Subjects WHERE Code = {q(r['Materiacodigo'])}), {q(desc[:200]) if desc else 'N' + chr(39) * 2}, "
           f"{q(str(r['Numeropractica']))}, {q(desc)}, {q(r['Observaciones'])}, 0, @Now);")
        s.source_row("Practicas", hdr, r, c, "Practice", c)
        if len(desc) > 200:
            s.issue("VALUE_TRUNCATED_IN_NAME", "Practice", c, "Name", f"{len(desc)} caracteres", "200",
                    "Descripción > 200 caracteres: Name se trunca y Description conserva el texto completo.", 0, "Practicas", r)
    s.end_group()

    # ---- Categorías y unidades de medida ----
    for sheet, table, entity in (("CategoriasArticulo", "ArticleCategories", "ArticleCategory"),
                                 ("UnidadesMedida", "MeasurementUnits", "MeasurementUnit")):
        hdr, rows = data[sheet]
        key_col = "Categoriacodigo" if sheet == "CategoriasArticulo" else "Unidadmedidacodigo"
        s.w(f"-- {sheet}")
        s.begin_group()
        for r in rows:
            c = r[key_col]
            sw(f"IF NOT EXISTS (SELECT 1 FROM dbo.{table} WHERE Code = {q(c)}) "
               f"INSERT dbo.{table}(Code, Name, Status, CreatedDate) VALUES({q(c)}, {q(r['Nombre'])}, 0, @Now);")
            s.source_row(sheet, hdr, r, c, entity, c)
        s.end_group()

    # ---- Artículos ----
    hdr, rows = data["Articulos"]
    cat_names = {r["Categoriacodigo"]: text(r["Nombre"]) for r in data["CategoriasArticulo"][1]}
    unit_names = {r["Unidadmedidacodigo"]: text(r["Nombre"]) for r in data["UnidadesMedida"][1]}
    seen_names = {}
    s.w("-- Artículos")
    s.begin_group()
    for r in rows:
        c = r["Articulocodigo"]
        name = text(r["Nombre"])
        key = name.casefold().rstrip()
        stored = name
        if key in seen_names:
            stored = f"{name} ({c})"  # UX_Articles_Name_Active exige nombres únicos entre activos
            s.issue("DUPLICATE_NAME_IN_SOURCE", "Article", c, "Nombre", name, stored,
                    f"Mismo nombre que {seen_names[key]}; se agrega el código al nombre por el índice único de nombres.",
                    1, "Articulos", r)
        else:
            seen_names[key] = c
        check_len("Articulos", r, "Nombre", stored, 200)
        review = 0 if low(r["Estado"]) == "activo" else 9
        sw(f"IF NOT EXISTS (SELECT 1 FROM dbo.Articles WHERE Code = {q(c)}) "
           f"INSERT dbo.Articles(Code, Name, Category, UnitOfMeasure, Status, ArticleCategoryId, MeasurementUnitId, ReviewStatus, ImportBatchId, CreatedDate) "
           f"VALUES({q(c)}, {q(stored)}, {q(cat_names[r['Categoriacodigo']])}, {q(unit_names[r['Unidadmedidacodigo']])}, 0, "
           f"(SELECT Id FROM dbo.ArticleCategories WHERE Code = {q(r['Categoriacodigo'])}), "
           f"(SELECT Id FROM dbo.MeasurementUnits WHERE Code = {q(r['Unidadmedidacodigo'])}), {review}, @Batch, @Now);")
        s.source_row("Articulos", hdr, r, c, "Article", c)
        if review == 9:
            s.issue("ARTICLE_PENDING_CONFIRMATION", "Article", c, "Estado", text(r["Estado"]), "ReviewStatus=9",
                    "La fuente marca el artículo como 'Por confirmar': se importa activo con ReviewStatus = Por confirmar.",
                    1, "Articulos", r)
    s.end_group()

    # ---- Requerimientos de práctica (recetas) ----
    hdr, rows = data["RequerimientosPractica"]
    s.w("-- Requerimientos de práctica")
    s.begin_group()
    for r in rows:
        c = r["Requerimientocodigo"]
        check_len("RequerimientosPractica", r, "Estadovalidacion", r["Estadovalidacion"], 40)
        check_len("RequerimientosPractica", r, "Observaciones", r["Observaciones"], 1000)
        scal = yes_no(r["Escalable"])
        sw(f"IF NOT EXISTS (SELECT 1 FROM dbo.PracticeRequirements WHERE Code = {q(c)}) "
           f"INSERT dbo.PracticeRequirements(Code, PracticeId, ArticleId, Quantity, IsScalable, ValidationStatus, Observations, IsRemoved, CreatedDate) "
           f"VALUES({q(c)}, (SELECT Id FROM dbo.Practices WHERE Code = {q(r['Practicacodigo'])}), "
           f"(SELECT Id FROM dbo.Articles WHERE Code = {q(r['Articulocodigo'])}), {q(r['Cantidad'])}, {scal or 0}, "
           f"{q(r['Estadovalidacion'])}, {q(r['Observaciones'])}, 0, @Now);")
        s.source_row("RequerimientosPractica", hdr, r, c, "PracticeRequirement", c)
        if r["Cantidad"] is None:
            s.issue("VALUE_MISSING", "PracticeRequirement", c, "Cantidad", None, None,
                    "La fuente no trae cantidad: se carga sin cantidad.", 1, "RequerimientosPractica", r)
    s.end_group()

    # ---- Existencias ----
    hdr, rows = data["ExistenciasArticulos"]
    s.w("-- Existencias de artículos")
    s.begin_group()
    for r in rows:
        key = f"{r['Laboratoriocodigo']}/{r['Articulocodigo']}"
        sw(f"IF NOT EXISTS (SELECT 1 FROM dbo.ArticleStocks WHERE LaboratoryId = (SELECT Id FROM dbo.Laboratories WHERE Code = {q(r['Laboratoriocodigo'])}) "
           f"AND ArticleId = (SELECT Id FROM dbo.Articles WHERE Code = {q(r['Articulocodigo'])})) "
           f"INSERT dbo.ArticleStocks(LaboratoryId, ArticleId, AvailableQuantity, UpdatedOn, CreatedDate) "
           f"VALUES((SELECT Id FROM dbo.Laboratories WHERE Code = {q(r['Laboratoriocodigo'])}), "
           f"(SELECT Id FROM dbo.Articles WHERE Code = {q(r['Articulocodigo'])}), {q(r['Cantidaddisponible'])}, "
           f"{q(as_date(r['Fechaactualizacion']))}, @Now);")
        s.source_row("ExistenciasArticulos", hdr, r, key, "ArticleStock", key)
    s.end_group()

    # ---- Planificación 2027-1: grupos derivados y programaciones ----
    hdr, rows = data["Planificacion"]
    combos = defaultdict(list)
    for r in rows:
        combos[(r["Gestioncodigo"], r["Materiacodigo"], str(r["Grupo"]).strip())].append(r)
    dup = Counter((r["Gestioncodigo"], r["Practicacodigo"], str(r["Grupo"]).strip()) for r in rows)
    if any(v > 1 for v in dup.values()):
        sys.exit(f"Planificacion: repetidos (gestión, práctica, grupo): {[k for k, v in dup.items() if v > 1][:5]}")

    def top(rs, field):
        vals = Counter(str(r[field]) for r in rs if r[field] is not None)
        if not vals:
            return None, 0
        best = vals.most_common(1)[0][0]
        return next(r[field] for r in rs if r[field] is not None and str(r[field]) == best), len(vals)

    s.w("-- Grupos de curso derivados (2027-1)")
    s.begin_group()
    for (m, subj, grp), rs in combos.items():
        gcode = f"GRP-{m}-{subj}-{grp}"
        check_len("Planificacion", rs[0], "Grupo", grp, 20)
        teacher, nt = top(rs, "Docenteactorcodigo")
        lab, nl = top(rs, "Laboratoriocodigo")
        students, _ = top(rs, "Cantestudiantes")
        demo, _ = top(rs, "Esdemostrativa")
        factor, _ = top(rs, "Factormateria")
        for field, n in (("Docenteactorcodigo", nt), ("Laboratoriocodigo", nl)):
            if n > 1:
                s.issue("COURSE_GROUP_VALUES_VARY", "CourseGroup", gcode, field, None, None,
                        "Las programaciones del grupo tienen valores distintos; el grupo toma el más frecuente.", 0, "Planificacion", rs[0])
        sw(f"IF NOT EXISTS (SELECT 1 FROM dbo.CourseGroups WHERE Code = {q(gcode)}) "
           f"INSERT dbo.CourseGroups(Code, ManagementId, SubjectId, Name, TeacherPersonId, LaboratoryId, StudentCount, IsDemonstrative, "
           f"ScalingFactor, Status, CreatedDate) "
           f"VALUES({q(gcode)}, (SELECT Id FROM dbo.Managements WHERE Code = {q(m)}), (SELECT Id FROM dbo.Subjects WHERE Code = {q(subj)}), "
           f"{q(grp)}, (SELECT Id FROM dbo.People WHERE ActorCode = {q(teacher)}), (SELECT Id FROM dbo.Laboratories WHERE Code = {q(lab)}), "
           f"{q(students)}, {yes_no(demo) or 0}, {q(factor)}, 0, @Now);")
        s.issue("VALUE_PENDING_CONFIRMATION", "CourseGroup", gcode, "Horainicio/Horafin", None, None,
                "El Excel no trae hora de inicio ni de fin en la planificación.", 0, "Planificacion", rs[0])
    s.end_group()

    s.w("-- Programaciones de práctica (2027-1)")
    s.begin_group()
    for r in rows:
        c = r["Planificacioncodigo"]
        grp = str(r["Grupo"]).strip()
        gcode = f"GRP-{r['Gestioncodigo']}-{r['Materiacodigo']}-{grp}"
        st = SCHEDULE_STATUS.get(low(r["Estado"]))
        if st is None:
            sys.exit(f"{c}: estado no mapeado {r['Estado']}")
        sw(f"IF NOT EXISTS (SELECT 1 FROM dbo.PracticeSchedules WHERE Code = {q(c)}) "
           f"INSERT dbo.PracticeSchedules(Code, ManagementId, SubjectId, PracticeId, CourseGroupId, GroupName, TeacherPersonId, LaboratoryId, "
           f"WeekNumber, ExecutionDate, StartTime, EndTime, StudentCount, IsDemonstrative, ScalingFactor, Status, ImportBatchId, CreatedDate, IsDeleted) "
           f"VALUES({q(c)}, (SELECT Id FROM dbo.Managements WHERE Code = {q(r['Gestioncodigo'])}), "
           f"(SELECT Id FROM dbo.Subjects WHERE Code = {q(r['Materiacodigo'])}), (SELECT Id FROM dbo.Practices WHERE Code = {q(r['Practicacodigo'])}), "
           f"(SELECT Id FROM dbo.CourseGroups WHERE Code = {q(gcode)}), {q(grp)}, (SELECT Id FROM dbo.People WHERE ActorCode = {q(r['Docenteactorcodigo'])}), "
           f"(SELECT Id FROM dbo.Laboratories WHERE Code = {q(r['Laboratoriocodigo'])}), {q(r['Semana'])}, {q(as_date(r['Fechaejecucion']))}, "
           f"{q(tstr(r['Horainicio']))}, {q(tstr(r['Horafin']))}, {q(r['Cantestudiantes'])}, {yes_no(r['Esdemostrativa']) if yes_no(r['Esdemostrativa']) is not None else 'NULL'}, "
           f"{q(r['Factormateria'])}, {st}, @Batch, @Now, 0);")
        s.source_row("Planificacion", hdr, r, c, "PracticeSchedule", c)
    s.end_group()

    s.w("""SELECT 'subjects' k, COUNT(*) n FROM dbo.Subjects UNION ALL SELECT 'practices', COUNT(*) FROM dbo.Practices
UNION ALL SELECT 'categories', COUNT(*) FROM dbo.ArticleCategories UNION ALL SELECT 'units', COUNT(*) FROM dbo.MeasurementUnits
UNION ALL SELECT 'articles', COUNT(*) FROM dbo.Articles UNION ALL SELECT 'practice_requirements', COUNT(*) FROM dbo.PracticeRequirements
UNION ALL SELECT 'stocks', COUNT(*) FROM dbo.ArticleStocks UNION ALL SELECT 'course_groups', COUNT(*) FROM dbo.CourseGroups
UNION ALL SELECT 'schedules', COUNT(*) FROM dbo.PracticeSchedules
UNION ALL SELECT 'source_rows', COUNT(*) FROM dbo.ImportSourceRows WHERE ImportBatchId = @Batch
UNION ALL SELECT 'issues', COUNT(*) FROM dbo.DataQualityIssues WHERE ImportBatchId = @Batch;""")
    path = s.write("parte_b2.sql")
    print(f"[ok] {path}: {len(s.lines)} líneas, source_rows={s.source_row_count}, issues={s.issue_count}")


if __name__ == "__main__":
    main("--dry-run" in sys.argv)
