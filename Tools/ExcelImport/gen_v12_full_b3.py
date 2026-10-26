"""
Importador único V12, parte B3: circuito L-3 (SolicitudesL3 / SolicitudL3Detalles) -> DB_Laboratorios_Univalle_REAL.
Requiere las partes A y B2 (laboratorios, actores, materias, prácticas y artículos).

- Solicitudes con fecha y laboratorio -> Plannings (+ PlanningRequirements); 'PROG-<SOL3>' (PracticeSchedule) solo si la
  práctica existe en el catálogo y no choca con otra programación (gestión + práctica + grupo).
- Todas las solicitudes entran como Plannings, también las 9 sin fecha (y 4 sin laboratorio): el dato vacío no se ignora;
  la fecha obligatoria que falta se completa con la fecha de la importación (instrucción de la clienta/equipo).
- Calendario de 2026-1 y 2026-2: fechas de clases = primera y última práctica de la fuente; semanas lunes-sábado.
- RequestDate (obligatorio) = fecha de la importación mientras la fuente no traiga Fechasolicitud (incidencia de lote).
Uso:  python gen_v12_full_b3.py [--dry-run]   ->  %TEMP%\\v12full\\parte_b3.sql
"""
import math
import sys
from collections import Counter, defaultdict
from datetime import date, datetime, timedelta

from v12_common import SqlBuilder, check_len, check_sha, q, read_sheets, text

SHEETS = ["Materias", "Practicas", "SolicitudesL3", "SolicitudL3Detalles"]
PERIODS = [(1, "07:35", "08:25"), (2, "08:35", "09:25"), (3, "09:25", "10:15"), (4, "10:25", "11:15"),
           (5, "11:15", "12:05"), (6, "12:15", "13:05"), (7, "13:05", "13:55"), (8, "14:05", "14:55"),
           (9, "14:55", "15:45"), (10, "15:55", "16:45"), (11, "16:45", "17:35"), (12, "17:45", "18:35")]
# estado de la fuente -> (Planning.Status, PracticeScheduleStatus)
STATUS = {"aprobada": ("Aprobada", 0), "terminada": ("Finalizada", 1), "parcial": ("Finalizada", 1),
          "cancelada": ("Cancelada", 3)}


def low(v):
    return (text(v) or "").casefold()


def hm(t):
    return None if t is None else t.strftime("%H:%M")


def week_slots(start, end):
    monday = start - timedelta(days=start.weekday())
    last = end - timedelta(days=end.weekday())
    n = 1
    while monday <= last:
        yield n, monday
        n += 1
        monday += timedelta(days=7)


def main(dry_run):
    sha = check_sha()
    data = read_sheets(SHEETS)
    for name, (_, rows) in data.items():
        print(f"[hoja] {name}: {len(rows)} filas")
    if dry_run:
        return

    hdr_r, reqs = data["SolicitudesL3"]
    hdr_l, lines = data["SolicitudL3Detalles"]
    subj_name = {r["Materiacodigo"]: text(r["Nombre"]) for r in data["Materias"][1]}
    subj_career = {r["Materiacodigo"]: r["Carreracodigo"] for r in data["Materias"][1]}
    practice_by = {}
    for r in data["Practicas"][1]:
        k = (r["Materiacodigo"], str(r["Numeropractica"]).strip())
        if k in practice_by:
            sys.exit(f"Práctica repetida en el catálogo: {k}")
        practice_by[k] = (r["Practicacodigo"], (text(r["Descripcion"]) or "")[:200])

    req_codes = {r["Solicitudl3codigo"] for r in reqs}
    if len(req_codes) != len(reqs) or any(l["Solicitudl3codigo"] not in req_codes for l in lines):
        sys.exit("Solicitudes repetidas o líneas huérfanas en el Excel")
    today = date.today()
    undated = [r for r in reqs if r["Fechapractica"] is None]  # sin fecha de práctica: se carga con la fecha de hoy
    undated_codes = {r["Solicitudl3codigo"] for r in undated}
    dated = [r for r in reqs if r["Solicitudl3codigo"] not in undated_codes]
    loaded = reqs  # todas entran como Plannings
    for r in reqs:
        if r["Fechapractica"] is not None and r["Fechapractica"].weekday() == 6:
            sys.exit(f"{r['Solicitudl3codigo']}: práctica en domingo")
        if r["Horainicio"] and r["Horafin"] and r["Horafin"] <= r["Horainicio"]:
            sys.exit(f"{r['Solicitudl3codigo']}: hora de fin no posterior al inicio")
        if low(r["Estado"]) not in STATUS:
            sys.exit(f"{r['Solicitudl3codigo']}: estado no mapeado {r['Estado']}")
        if r["Todosloslaboratorios"] not in (False, None):
            sys.exit(f"{r['Solicitudl3codigo']}: Todosloslaboratorios no soportado")
        check_len("SolicitudesL3", r, "Grupo", str(r["Grupo"]).strip(), 20)
        check_len("SolicitudesL3", r, "Observaciones", r["Observaciones"], 1000)
        r["_date"] = r["Fechapractica"].date() if r["Fechapractica"] is not None else today
        r["_real_date"] = r["Fechapractica"] is not None

    starts = {a: pid for pid, a, _ in PERIODS}
    ends = {b: pid for pid, _, b in PERIODS}
    for r in loaded:
        a, b = hm(r["Horainicio"]), hm(r["Horafin"])
        r["_p"] = (starts[a], ends[b] - starts[a] + 1) if a in starts and b in ends and ends[b] >= starts[a] else None

    class_range = {}
    for m in sorted({r["Gestioncodigo"] for r in dated}):
        ds = [r["Fechapractica"].date() for r in dated if r["Gestioncodigo"] == m]
        class_range[m] = (min(ds), max(ds))
    first_monday = {m: next(week_slots(a, b))[1] for m, (a, b) in class_range.items()}

    def week_number(m, d):
        return (d - timedelta(days=d.weekday()) - first_monday[m]).days // 7 + 1

    combos = defaultdict(list)
    for r in loaded:
        combos[(r["Gestioncodigo"], r["Materiacodigo"], str(r["Grupo"]).strip())].append(r)

    s = SqlBuilder()
    s.prologue(sha, "Importación completa V12 (parte B3: circuito L-3 de 2026-1 y 2026-2).")
    sw = s.stmt
    s.w("IF (SELECT COUNT(*) FROM dbo.ClassPeriods) <> 12 THROW 50003, 'Catálogo de periodos inesperado', 1;")
    for pid, a, b in PERIODS:
        s.w(f"IF NOT EXISTS (SELECT 1 FROM dbo.ClassPeriods WHERE Id = {pid} AND StartTime = '{a}' AND EndTime = '{b}') "
            f"THROW 50004, 'Periodo {pid} distinto al esperado', 1;")

    # ---- Calendario académico (fechas de clases derivadas) ----
    s.w("-- Calendario de 2026-1 y 2026-2")
    s.begin_group()
    for m, (a, b) in class_range.items():
        sw(f"UPDATE dbo.Managements SET ClassesStartDate = {q(a)}, ClassesEndDate = {q(b)}, LastModifiedDate = @Now "
           f"WHERE Code = {q(m)} AND ClassesStartDate IS NULL AND ClassesEndDate IS NULL;")
        s.issue("VALUE_DERIVED_FROM_SOURCE", "Management", m, "ClassesStartDate/ClassesEndDate", None, f"{a} / {b}",
                "La fuente no trae inicio ni fin de clases: se toman la primera y la última práctica L-3 de la gestión. Confirmar con la clienta.",
                1, "SolicitudesL3", None)
        mid = f"(SELECT Id FROM dbo.Managements WHERE Code = {q(m)})"
        for n, monday in week_slots(a, b):
            sw(f"IF NOT EXISTS (SELECT 1 FROM dbo.AcademicWeeks WHERE ManagementId = {mid} AND StartDate = {q(monday)}) "
               f"INSERT dbo.AcademicWeeks(Number, StartDate, EndDate, ManagementId) VALUES({n}, {q(monday)}, {q(monday + timedelta(days=5))}, {mid});")
            wid = f"(SELECT Id FROM dbo.AcademicWeeks WHERE ManagementId = {mid} AND StartDate = {q(monday)})"
            for i in range(6):
                d = monday + timedelta(days=i)
                if a <= d <= b:
                    sw(f"IF NOT EXISTS (SELECT 1 FROM dbo.AcademicDays WHERE AcademicWeekId = {wid} AND [Date] = {q(d)}) "
                       f"INSERT dbo.AcademicDays(AcademicWeekId, [Date], Status) VALUES({wid}, {q(d)}, 0);")
    s.end_group()

    # ---- Grupos de curso derivados ----
    def top(rs, field):
        vals = Counter(str(r[field]) for r in rs if text(r[field]))
        if not vals:
            return None, 0
        best = vals.most_common(1)[0][0]
        return next(r[field] for r in rs if text(r[field]) and str(r[field]) == best), len(vals)

    s.w("-- Grupos de curso derivados de L-3")
    s.begin_group()
    for (m, subj, grp), rs in combos.items():
        gcode = f"GRP-{m}-{subj}-{grp}"
        teacher, nt = top(rs, "Docenteactorcodigo")
        lab, nl = top(rs, "Laboratoriocodigo")
        for field, n in (("Docenteactorcodigo", nt), ("Laboratoriocodigo", nl)):
            if n > 1:
                s.issue("COURSE_GROUP_VALUES_VARY", "CourseGroup", gcode, field, None, None,
                        "Las solicitudes del grupo tienen valores distintos; el grupo toma el más frecuente y cada solicitud conserva el suyo.",
                        0, "SolicitudesL3", rs[0])
        sw(f"IF NOT EXISTS (SELECT 1 FROM dbo.CourseGroups WHERE Code = {q(gcode)}) "
           f"INSERT dbo.CourseGroups(Code, ManagementId, SubjectId, Name, TeacherPersonId, LaboratoryId, IsDemonstrative, Status, CreatedDate) "
           f"VALUES({q(gcode)}, (SELECT Id FROM dbo.Managements WHERE Code = {q(m)}), (SELECT Id FROM dbo.Subjects WHERE Code = {q(subj)}), "
           f"{q(grp)}, (SELECT Id FROM dbo.People WHERE ActorCode = {q(teacher)}), (SELECT Id FROM dbo.Laboratories WHERE Code = {q(lab)}), 0, 0, @Now);")
    s.end_group()

    # ---- Programaciones (PROG-<SOL3>) y plannings ----
    schedule_taken = set()
    schedule_of = {}
    s.w("-- Programaciones de práctica (PROG-<SOL3>) para solicitudes con práctica en el catálogo")
    s.begin_group()
    for r in sorted(dated, key=lambda x: x["Solicitudl3codigo"]):
        c = r["Solicitudl3codigo"]
        grp = str(r["Grupo"]).strip()
        num = str(r["Numeropractica"]).strip()
        prac = practice_by.get((r["Materiacodigo"], num))
        if not prac:
            s.issue("PRACTICE_NOT_IN_CATALOG", "Planning", c, "Numeropractica", f"{r['Materiacodigo']} / {num}", None,
                    "La práctica no existe en el catálogo de la materia: se carga sin programación y con nombre derivado.", 0, "SolicitudesL3", r)
            continue
        key = (r["Gestioncodigo"], prac[0], grp)
        if key in schedule_taken:
            s.issue("CONTENT_CONFLICT", "PracticeSchedule", f"PROG-{c}", "Gestion/Practica/Grupo", None, None,
                    "Ya existe otra programación con la misma gestión, práctica y grupo: la solicitud se carga sin programación.", 1, "SolicitudesL3", r)
            continue
        schedule_taken.add(key)
        schedule_of[c] = prac[0]
        p_status, s_status = STATUS[low(r["Estado"])]
        gcode = f"GRP-{r['Gestioncodigo']}-{r['Materiacodigo']}-{grp}"
        wn = week_number(r["Gestioncodigo"], r["Fechapractica"].date())
        sw(f"IF NOT EXISTS (SELECT 1 FROM dbo.PracticeSchedules WHERE Code = {q('PROG-' + c)}) "
           f"INSERT dbo.PracticeSchedules(Code, ManagementId, SubjectId, PracticeId, CourseGroupId, GroupName, TeacherPersonId, LaboratoryId, "
           f"WeekNumber, AcademicWeekId, ExecutionDate, StartTime, EndTime, Status, ImportBatchId, CreatedDate, IsDeleted) "
           f"VALUES({q('PROG-' + c)}, (SELECT Id FROM dbo.Managements WHERE Code = {q(r['Gestioncodigo'])}), "
           f"(SELECT Id FROM dbo.Subjects WHERE Code = {q(r['Materiacodigo'])}), (SELECT Id FROM dbo.Practices WHERE Code = {q(prac[0])}), "
           f"(SELECT Id FROM dbo.CourseGroups WHERE Code = {q(gcode)}), {q(grp)}, (SELECT Id FROM dbo.People WHERE ActorCode = {q(r['Docenteactorcodigo'])}), "
           f"(SELECT Id FROM dbo.Laboratories WHERE Code = {q(r['Laboratoriocodigo'])}), {wn}, "
           f"(SELECT w.Id FROM dbo.AcademicWeeks w WHERE w.ManagementId = (SELECT Id FROM dbo.Managements WHERE Code = {q(r['Gestioncodigo'])}) AND w.Number = {wn}), "
           f"{q(r['Fechapractica'].date())}, {q(hm(r['Horainicio']))}, {q(hm(r['Horafin']))}, {s_status}, @Batch, @Now, 0);")
    s.end_group()

    # Limpieza de la primera versión (solicitudes sin fecha guardadas aparte): ahora entran como Plannings.
    s.w("-- Limpieza: UndatedL3* queda vacío; las 9 solicitudes sin fecha entran como Plannings")
    s.w("BEGIN TRANSACTION;")
    s.w("DELETE FROM dbo.UndatedL3RequestLines; DELETE FROM dbo.UndatedL3Requests;")
    s.w("DELETE FROM dbo.ImportSourceRows WHERE ImportBatchId = @Batch AND TargetEntityName IN (N'UndatedL3Request', N'UndatedL3RequestLine');")
    s.w("DELETE FROM dbo.DataQualityIssues WHERE ImportBatchId = @Batch AND EntityName = N'UndatedL3Request';")
    s.w("COMMIT TRANSACTION;")

    s.w("-- Plannings (SolicitudesL3): todas las solicitudes, también las sin fecha (fecha de la importación)")
    s.begin_group()
    by_day = defaultdict(list)
    for r in loaded:
        c = r["Solicitudl3codigo"]
        grp = str(r["Grupo"]).strip()
        num = str(r["Numeropractica"]).strip()
        prac = practice_by.get((r["Materiacodigo"], num))
        pname = prac[1] if prac and prac[1] else f"Práctica {num} – {subj_name[r['Materiacodigo']]}"
        p_status, _ = STATUS[low(r["Estado"])]
        d = r["_date"]
        real = r["_real_date"]
        wn = week_number(r["Gestioncodigo"], d) if real else 0
        week_expr = (f"(SELECT w.Id FROM dbo.AcademicWeeks w WHERE w.ManagementId = (SELECT Id FROM dbo.Managements WHERE Code = {q(r['Gestioncodigo'])}) "
                     f"AND w.Number = {wn})") if real else "NULL"
        start_dt = datetime.combine(d, r["Horainicio"]) if r["Horainicio"] else datetime.combine(d, datetime.min.time())
        end_dt = datetime.combine(d, r["Horafin"]) if r["Horafin"] else None
        sp, pc = r["_p"] if r["_p"] else (None, None)
        mid = f"(SELECT Id FROM dbo.Managements WHERE Code = {q(r['Gestioncodigo'])})"
        req_date = r["Fechasolicitud"].date() if r["Fechasolicitud"] is not None else today
        lab = text(r["Laboratoriocodigo"])
        lab_expr = f"(SELECT Id FROM dbo.Laboratories WHERE Code = {q(lab)})" if lab else "NULL"
        sw(f"IF NOT EXISTS (SELECT 1 FROM dbo.Plannings WHERE Code = {q(c)}) "
           f"INSERT dbo.Plannings(Code, ManagementId, CampusId, LaboratoryId, AllLaboratories, TeacherPersonId, SubjectId, CourseGroupId, GroupName, "
           f"PracticeNumber, PracticeName, PracticeScheduleId, AcademicWeekId, AcademicWeek, ScheduledDate, ScheduledEndDate, StartTime, EndTime, "
           f"StartPeriodId, PeriodCount, Status, RequestDate, Observations, CreatedDate, IsDeleted) "
           f"VALUES({q(c)}, {mid}, (SELECT Id FROM dbo.Campuses WHERE Code = {q(r['Sedecodigo'])}), "
           f"{lab_expr}, {1 if r['Todosloslaboratorios'] else 0}, "
           f"(SELECT Id FROM dbo.People WHERE ActorCode = {q(r['Docenteactorcodigo'])}), (SELECT Id FROM dbo.Subjects WHERE Code = {q(r['Materiacodigo'])}), "
           f"(SELECT Id FROM dbo.CourseGroups WHERE Code = {q('GRP-' + r['Gestioncodigo'] + '-' + r['Materiacodigo'] + '-' + grp)}), {q(grp)}, "
           f"{q(num)}, {q(pname)}, (SELECT Id FROM dbo.PracticeSchedules WHERE Code = {q('PROG-' + c)}), "
           f"{week_expr}, {wn}, {q(start_dt)}, {q(end_dt)}, "
           f"{q(hm(r['Horainicio']))}, {q(hm(r['Horafin']))}, {q(sp)}, {q(pc)}, {q(p_status)}, {q(datetime.combine(req_date, datetime.min.time()))}, "
           f"{q(r['Observaciones'])}, @Now, 0);")
        if r["Fechasolicitud"] is None:
            # corrige cargas previas que usaron la fecha de la práctica como fecha de solicitud
            today_dt = q(datetime.combine(today, datetime.min.time()))
            sw(f"UPDATE dbo.Plannings SET RequestDate = {today_dt}, LastModifiedDate = @Now WHERE Code = {q(c)} AND RequestDate <> {today_dt};")
        s.source_row("SolicitudesL3", hdr_r, r, c, "Planning", c)
        if not real:
            s.issue("REQUIRED_VALUE_MISSING", "Planning", c, "Fechapractica", None, str(today),
                    "La solicitud no trae fecha de práctica: se carga con la fecha de la importación para que el dato entre (la fecha real "
                    "debe confirmarla la clienta).", 2, "SolicitudesL3", r)
        if not lab:
            s.issue("REQUIRED_VALUE_MISSING", "Planning", c, "Laboratoriocodigo", None, None,
                    "La solicitud no trae laboratorio: se carga con LaboratoryId vacío (la app lo muestra como 'Sin ambiente').", 2, "SolicitudesL3", r)
        if not r["_p"]:
            s.issue("TIME_NOT_ALIGNED_TO_PERIOD", "Planning", c, "Horainicio/Horafin", f"{hm(r['Horainicio']) or '-'} / {hm(r['Horafin']) or '-'}", None,
                    "El horario no coincide con los límites de los periodos: se carga sin periodo y con la hora de la fuente. No se redondea.", 1, "SolicitudesL3", r)
        if subj_career.get(r["Materiacodigo"]) != r["Carreracodigo"]:
            s.issue("CAREER_MISMATCH", "Planning", c, "Carreracodigo", r["Carreracodigo"], subj_career.get(r["Materiacodigo"]),
                    "La carrera del formulario no es la de la materia; Planning no guarda carrera (queda en OriginalDataJson).", 0, "SolicitudesL3", r)
        if real and r["Horainicio"] and r["Horafin"]:
            by_day[d].append(r)
    s.end_group()
    s.issue("VALUE_PENDING_CONFIRMATION", "Planning", "SolicitudesL3", "Fechasolicitud", None, f"RequestDate = {today}",
            "La fuente no trae Fechasolicitud y RequestDate es obligatorio: se usa la fecha de la importación.", 1, "SolicitudesL3", None)
    for day_rows in by_day.values():
        day_rows.sort(key=lambda x: x["Solicitudl3codigo"])
        for i, a in enumerate(day_rows):
            for b in day_rows[i + 1:]:
                if a["Horainicio"] < b["Horafin"] and b["Horainicio"] < a["Horafin"]:
                    for fld in ("Laboratoriocodigo", "Docenteactorcodigo"):
                        if a[fld] == b[fld]:
                            s.issue("SCHEDULE_OVERLAP_SOURCE", "Planning", f"{a['Solicitudl3codigo']}|{b['Solicitudl3codigo']}", fld, a[fld], None,
                                    "Dos solicitudes de la fuente se solapan el mismo día (mismo laboratorio o docente). Informativo: se cargan ambas.",
                                    0, "SolicitudesL3", a)

    # ---- Líneas de todas las solicitudes ----
    pair_count = Counter((l["Solicitudl3codigo"], l["Articulocodigo"]) for l in lines)
    s.w("-- Planning requirements (SolicitudL3Detalles)")
    s.begin_group()
    for l in lines:
        c = l["Solicitudl3detallecodigo"]
        rc = l["Solicitudl3codigo"]
        qty = l["Cantidadsolicitada"]
        qty_sql = q(qty) if qty is not None else "NULL"  # el 0 de la fuente se conserva (check >= 0)
        expected = int(math.ceil(qty)) if qty is not None else 0
        check_len("SolicitudL3Detalles", l, "Observaciones", l["Observaciones"], 1000)
        sw(f"IF NOT EXISTS (SELECT 1 FROM dbo.PlanningRequirements WHERE Code = {q(c)}) "
           f"INSERT dbo.PlanningRequirements(Code, PlanningId, ArticleId, EquipmentUnitId, RequestedQuantity, ExpectedQuantity, DeliveredQuantity, ReturnedQuantity, Observations) "
           f"VALUES({q(c)}, (SELECT Id FROM dbo.Plannings WHERE Code = {q(rc)}), (SELECT Id FROM dbo.Articles WHERE Code = {q(l['Articulocodigo'])}), "
           f"NULL, {qty_sql}, {expected}, 0, 0, {q(l['Observaciones'])});")
        if qty == 0:
            sw(f"UPDATE dbo.PlanningRequirements SET RequestedQuantity = 0 WHERE Code = {q(c)} AND RequestedQuantity IS NULL;")
        s.source_row("SolicitudL3Detalles", hdr_l, l, c, "PlanningRequirement", c)
        if qty is None:
            s.issue("VALUE_MISSING", "PlanningRequirement", c, "Cantidadsolicitada", None, None,
                    "La línea no trae cantidad: se carga con cantidad vacía.", 1, "SolicitudL3Detalles", l)
        elif qty == 0:
            s.issue("VALUE_ZERO_IN_SOURCE", "PlanningRequirement", c, "Cantidadsolicitada", "0", "0",
                    "Cantidad 0 en la fuente: se carga tal cual (0).", 1, "SolicitudL3Detalles", l)
        elif qty >= 100:
            s.issue("QUANTITY_UNIT_SUSPECT", "PlanningRequirement", c, "Cantidadsolicitada", str(qty), None,
                    "Cantidad ≥ 100 en un artículo por pieza: probablemente gramos. Se carga tal cual; confirmar unidad.", 0, "SolicitudL3Detalles", l)
        if pair_count[(rc, l["Articulocodigo"])] > 1:
            s.issue("DUPLICATE_LINE_IN_SOURCE", "PlanningRequirement", c, "Articulocodigo", l["Articulocodigo"], rc,
                    "El artículo se repite en la misma solicitud: se cargan ambas líneas, sin sumar.", 0, "SolicitudL3Detalles", l)
    s.end_group()

    s.w("""SELECT 'plannings' k, COUNT(*) n FROM dbo.Plannings UNION ALL SELECT 'planning_requirements', COUNT(*) FROM dbo.PlanningRequirements
UNION ALL SELECT 'undated_requests', COUNT(*) FROM dbo.UndatedL3Requests UNION ALL SELECT 'undated_lines', COUNT(*) FROM dbo.UndatedL3RequestLines
UNION ALL SELECT 'schedules_prog', COUNT(*) FROM dbo.PracticeSchedules WHERE Code LIKE N'PROG-SOL3-%'
UNION ALL SELECT 'course_groups_2026', COUNT(*) FROM dbo.CourseGroups WHERE Code LIKE N'GRP-2026-%'
UNION ALL SELECT 'weeks_2026', COUNT(*) FROM dbo.AcademicWeeks w JOIN dbo.Managements m ON m.Id = w.ManagementId WHERE m.Code IN (N'2026-1', N'2026-2')
UNION ALL SELECT 'source_rows', COUNT(*) FROM dbo.ImportSourceRows WHERE ImportBatchId = @Batch
UNION ALL SELECT 'issues', COUNT(*) FROM dbo.DataQualityIssues WHERE ImportBatchId = @Batch;""")
    path = s.write("parte_b3.sql")
    print(f"[ok] {path}: {len(s.lines)} líneas, solicitudes={len(loaded)}, sin_fecha={len(undated)}, "
          f"programaciones={len(schedule_of)}, grupos={len(combos)}, source_rows={s.source_row_count}, issues={s.issue_count}")


if __name__ == "__main__":
    main("--dry-run" in sys.argv)
