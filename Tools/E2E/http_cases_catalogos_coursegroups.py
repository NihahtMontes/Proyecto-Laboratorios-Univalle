"""
Catálogos / Grupos de materia (CourseGroups: Create heredado, Edit, Delete y errores de Horario por cohorte): reglas del servidor por HTTP.
Solo contra la copia QA. Grupos con nombres 'QB4', 'QC4'... sobre las materias 34 y 41 (Nutrición, semestre 4) en la gestión 2026-2 (Id 4).
"""
import json
import re
import urllib.parse

from http_forms import parse_messages
from http_cases_catalogos_common import (check, check_404, check_anon, expect, get_nofollow, post_404, run_module, scalar, sql, tag, tokenless)

B = "/Catalogs/CourseGroups"
LABEL = "CourseGroups"
MG = 4
MG_DONE = 1


def L(s):
    return f"{LABEL}/{s}"


def cg(gid):
    r = sql("select Id, Code, ManagementId, SubjectId, Name, TeacherPersonId, LaboratoryId, StudentCount, ScalingFactor, Status, Weekday from CourseGroups where Id=?", gid)
    return tuple(r[0]) if r else None


def cg_by(name, subject, mg=MG):
    return scalar("select top 1 Id from CourseGroups where Name=? and SubjectId=? and ManagementId=? order by Id desc", name, subject, mg)


def slots(gid):
    return [tuple(r) for r in sql("select s.Weekday, p.[Order] from CourseGroupScheduleSlots s join ClassPeriods p on p.Id=s.ClassPeriodId where s.CourseGroupId=? order by s.Weekday, p.[Order]", gid)]


def run(R, c):
    for p in (B, f"{B}/Create", f"{B}/Create?handler=Legacy", f"{B}/Edit/1", f"{B}/Delete/1", f"{B}/Horario"):
        check_anon(R, LABEL, p)
    check_anon(R, LABEL, f"{B}/Create", post=True)
    check_anon(R, LABEL, f"{B}/Delete/1", post=True)
    check_anon(R, LABEL, f"{B}/Horario", post=True)

    n_cg0 = scalar("select count(*) from CourseGroups")
    max_cg0 = scalar("select max(Id) from CourseGroups")
    n_sg0 = scalar("select count(*) from StudentGroups")
    pre = scalar("select count(*) from CourseGroups where Name like 'Q%' and ManagementId in (1,4)")
    check(R, L("Preparación: no hay grupos 'Q*' previos"), pre == 0, f"previos={pre}")
    st, page = c.get("/Catalogs/StudentGroups/Create?managementId=4")
    cfg = json.loads(re.search(r'<script type="application/json" id="sgConfig">(.*?)</script>', page, re.S).group(1))
    teachers = [t["id"] for t in cfg["teachers"]]
    T1, T2 = teachers[0], teachers[1]
    LAB = cfg["laboratories"][0]["id"]
    tech = scalar("select top 1 Id from People where Category=1 and Status=0 order by Id")
    SJ_A, SJ_B = 34, 41      # Nutrición, semestre 4
    SJ_DEAD_NAME = "QAC Mat cg"
    c.submit("/Catalogs/Subjects/Create", {"Input.Name": f"{SJ_DEAD_NAME} {tag()}", "Input.CareerId": "1", "Input.Semester": "1"})
    sj_dead = scalar("select top 1 Id from Subjects where Name like ? order by Id desc", SJ_DEAD_NAME + "%")
    c.submit(f"/Catalogs/Subjects/Delete/{sj_dead}", {})
    TOKEN = f"{B}/Create?handler=Legacy&managementId={MG}"

    def post(fields, name, url=None):
        return c.raw_post(url or f"{B}/Create", fields, token_from=TOKEN, name=name)

    def body(name="QB4", subject=SJ_A, teacher=None, mg=MG, lab=None, count="20", factor=None, slots_json="[]", extra=()):
        f = [("Input.ManagementId", mg), ("Input.SubjectId", subject), ("Input.Name", name), ("Input.TeacherPersonId", T1 if teacher is None else teacher)]
        if lab is not None:
            f.append(("Input.LaboratoryId", lab))
        if count is not None:
            f.append(("Input.StudentCount", count))
        if factor is not None:
            f.append(("Input.ScalingFactor", factor))
        if slots_json is not None:
            f.append(("Input.SlotsJson", slots_json))
        return f + list(extra)

    # ---------------------------------------------------------------- GET Create / Legacy
    st, loc = get_nofollow(c, f"{B}/Create?managementId={MG}&subjectId={SJ_B}")
    check(R, L("Create GET redirige a StudentGroups/Create con managementId y subjectId"),
          st == 302 and "/Catalogs/StudentGroups/Create" in (loc or "") and f"managementId={MG}" in loc and f"subjectId={SJ_B}" in loc, f"status={st} loc={loc}")
    st, loc = get_nofollow(c, f"{B}/Create")
    check(R, L("Create GET sin parámetros también redirige a StudentGroups/Create"), st == 302 and "/Catalogs/StudentGroups/Create" in (loc or ""), f"status={st} loc={loc}")
    st, _ = c.get(f"{B}/Create?handler=Legacy&managementId={MG}&subjectId={SJ_B}")
    check(R, L("Create ?handler=Legacy responde 200"), st == 200, f"status={st}")
    st, page = c.get(f"{B}/Create?handler=Legacy&managementId=99999")
    check(R, L("Create ?handler=Legacy con gestión inexistente: 'La gestión solicitada no existe.'"),
          st == 200 and any("La gestión solicitada no existe." in t for _, t in parse_messages(page)[1]), f"status={st}")

    # ---------------------------------------------------------------- Create heredado: validaciones
    expect(R, L("Create: nombre vacío"), post(body(name=""), "nombre vacío"), status=200, error="El nombre del grupo es obligatorio")
    expect(R, L("Create: nombre solo espacios"), post(body(name="   "), "nombre espacios"), status=200, error="El nombre del grupo es obligatorio")
    expect(R, L("Create: nombre de 21 caracteres"), post(body(name="Q" * 21), "21"), status=200, error="El nombre no puede superar los 20 caracteres")
    r = post([("Input.SubjectId", SJ_A), ("Input.Name", "QB4"), ("Input.TeacherPersonId", T1)], "sin gestión")
    expect(R, L("Create: gestión ausente"), r, status=200, error="Debe seleccionar la gestión")
    r = post(body(mg=""), "gestión vacía")
    expect(R, L("Create: gestión vacía"), r, status=200, error="Debe seleccionar la gestión")
    expect(R, L("Create: gestión=0"), post(body(mg="0"), "gestión 0"), status=200, error="Debe seleccionar una gestión válida")
    expect(R, L("Create: gestión inexistente"), post(body(mg="99999"), "gestión inexistente"), status=200, error="La gestión seleccionada no existe")
    expect(R, L("Create: materia vacía"), post(body(subject=""), "materia vacía"), status=200, error="Debe seleccionar la materia")
    expect(R, L("Create: materia inexistente"), post(body(subject="99999"), "materia inexistente"), status=200, error="La materia seleccionada no existe o no está activa.")
    expect(R, L("Create: materia dada de baja"), post(body(subject=str(sj_dead)), "materia baja"), status=200, error="La materia seleccionada no existe o no está activa.")
    r = post([("Input.ManagementId", MG), ("Input.SubjectId", SJ_A), ("Input.Name", "QB4")], "sin docente")
    expect(R, L("Create: docente ausente"), r, status=200, error="Debe seleccionar el docente")
    expect(R, L("Create: docente=0"), post(body(teacher="0"), "docente 0"), status=200, error="Debe seleccionar un docente válido")
    expect(R, L("Create: docente que es técnico"), post(body(teacher=str(tech)), "docente técnico"), status=200, error="El docente seleccionado no existe o no está activo.")
    expect(R, L("Create: docente inexistente"), post(body(teacher="99999"), "docente inexistente"), status=200, error="El docente seleccionado no existe o no está activo.")
    expect(R, L("Create: laboratorio inexistente"), post(body(lab="99999"), "lab inexistente"), status=200, error="El laboratorio seleccionado no existe o no está activo.")
    expect(R, L("Create: estudiantes 1001"), post(body(count="1001"), "1001"), status=200, error="El número de estudiantes debe estar entre 0 y 1000")
    expect(R, L("Create: estudiantes -1"), post(body(count="-1"), "-1"), status=200, error="El número de estudiantes debe estar entre 0 y 1000")
    expect(R, L("Create: factor 1001"), post(body(factor="1001"), "factor"), status=200, error="El factor debe estar entre 0 y 1000")
    r = post(body(count="abc"), "estudiantes abc")
    check(R, L("Create: estudiantes 'abc' se rechaza sin 500"), r.status == 200 and bool(r.errors), f"status={r.status} errores={r.errors[:2]}")
    expect(R, L("Create: horario con JSON inválido"), post(body(slots_json="{{no"), "json malo"), status=200, error="El horario enviado no es válido: vuelva a marcar los periodos.")
    check(R, L("Create: ninguna validación fallida creó grupos"), scalar("select count(*) from CourseGroups") == n_cg0)

    # ---------------------------------------------------------------- Create heredado: válido
    r = post(body(name="QB4", count="25", factor="5", lab=str(LAB)), "válido")
    expect(R, L("Create: válido (QB4, materia 34)"), r, status=302, redirect=B, toast_kind="success", toast="registrado exitosamente.")
    gA = cg_by("QB4", SJ_A)
    row = cg(gA) if gA else None
    check(R, L("Create: fila en BD (código, gestión, materia, docente, laboratorio, estudiantes 25, factor 5, Activo)"),
          row and row[1] and row[2] == MG and row[3] == SJ_A and row[5] == T1 and row[6] == LAB and row[7] == 25 and row[8] == 5 and row[9] == 0, str(row))
    check(R, L("Create: el toast cita «Grupo 'CÓDIGO' (QB4) registrado exitosamente.»"), bool(r.toasts) and row and r.toasts[0][1] == f"Grupo '{row[1]}' (QB4) registrado exitosamente.", str(r.toasts[:1]))
    for dup, desc in (("qb4", "minúsculas"), (" QB4 ", "espacios alrededor"), ("Q B4", "espacios internos")):
        expect(R, L(f"Create: duplicado {desc}"), post(body(name=dup), f"dup {desc}"), status=200, error="Ya existe un grupo con este nombre para la gestión y materia seleccionadas.")
    check(R, L("Create: tras los duplicados queda un solo QB4 de la materia 34"), scalar("select count(*) from CourseGroups where SubjectId=? and ManagementId=? and Name='QB4'", SJ_A, MG) == 1)
    expect(R, L("Create: mismo nombre en OTRA materia (permitido)"), post(body(name="QB4", subject=SJ_B, teacher=str(T2)), "otra materia"), status=302, redirect=B)
    gB = cg_by("QB4", SJ_B)
    r = post(body(name="Q" * 20), "20 caracteres")
    expect(R, L("Create: nombre de 20 caracteres exactos"), r, status=302, redirect=B)
    g20 = cg_by("Q" * 20, SJ_A)
    html = "<b>QC4</b>"
    r = post(body(name=html), "html")
    expect(R, L("Create: nombre con HTML se acepta sin excepción"), r, status=302, redirect=B)
    g_html = cg_by(html, SJ_A)
    if g_html:
        _, page = c.get(f"{B}?{urllib.parse.urlencode({'SearchTerm': 'QC4', 'ManagementFilter': MG})}")
        check(R, L("Index: el HTML hostil se muestra escapado"), html not in page and "&lt;b&gt;" in page)
    r = post(body(name="Ñandú \"x\"", subject=SJ_B, teacher=str(T2)), "tildes")
    expect(R, L("Create: nombre con ñ, tildes y comillas"), r, status=302, redirect=B)
    g_acc = cg_by("Ñandú \"x\"", SJ_B)
    check(R, L("Create: el nombre con ñ y comillas se guarda idéntico"), g_acc is not None)
    r = post(body(name="  QD   4  ", teacher=str(T2)), "espacios")
    gD = cg_by("QD 4", SJ_A)
    check(R, L("Create: espacios sobrantes se limpian (guardado como 'QD 4')"), r.status == 302 and gD is not None, f"status={r.status}")
    r = post(body(name="QE4", count="0", factor="0"), "ceros")
    gE = cg_by("QE4", SJ_A)
    check(R, L("Create: estudiantes 0 y factor 0 válidos"), r.status == 302 and gE and cg(gE)[7] == 0 and cg(gE)[8] == 0, f"status={r.status}")
    r = post(body(name="QF4", count=""), "vacío")
    gF = cg_by("QF4", SJ_A)
    check(R, L("Create: estudiantes vacío = NULL (opcional)"), r.status == 302 and gF and cg(gF)[7] is None, f"status={r.status}")

    # ---------------------------------------------------------------- Create heredado con horario
    sj_json = json.dumps([{"weekday": 1, "order": 1}, {"weekday": 1, "order": 2}, {"weekday": 7, "order": 1}, {"weekday": 2, "order": 1}, {"weekday": 2, "order": 1}])
    r = post(body(name="QG4", slots_json=sj_json), "con horario")
    gG = cg_by("QG4", SJ_A)
    check(R, L("Create con horario: celdas válidas guardadas, día 7 ignorado y duplicados unificados"), r.status == 302 and gG and slots(gG) == [(1, 1), (1, 2), (2, 1)], f"status={r.status} slots={slots(gG) if gG else None} errores={r.errors[:2]}")
    r = post(body(name="QG4", subject=SJ_B, teacher=str(T2), slots_json=json.dumps([{"weekday": 1, "order": 2}])), "choque de cohorte")
    check(R, L("Create con horario: celda que choca con otra materia del mismo cohorte (QG4) se rechaza con mensaje claro"),
          r.status == 200 and bool(r.errors) and cg_by("QG4", SJ_B) is None, f"status={r.status} errores={r.errors[:2]} fila={cg_by('QG4', SJ_B)}")
    r = post(body(name="QH4", slots_json=json.dumps([{"weekday": 1, "order": 99}])), "periodo 99")
    gH = cg_by("QH4", SJ_A)
    check(R, L("Create con horario: periodo inexistente (99) no da 500 (se ignora o se informa)"), r.status in (200, 302), f"status={r.status} errores={r.errors[:2]} slots={slots(gH) if gH else None}")
    r = post(body(name="QI4", slots_json=json.dumps([{"weekday": 3, "order": i} for i in range(1, 13)] * 10)), "muchas celdas")
    gI = cg_by("QI4", SJ_A)
    check(R, L("Create con horario: 120 celdas repetidas no dan 500"), r.status in (200, 302), f"status={r.status} errores={r.errors[:2]} slots={len(slots(gI)) if gI else None}")
    r = post(body(name="QJ4", slots_json="[]"), "horario vacío")
    check(R, L("Create con horario vacío ('[]')"), r.status == 302 and cg_by("QJ4", SJ_A) is not None, f"status={r.status}")
    r = post(body(name="QK4", slots_json=None), "sin campo de horario")
    check(R, L("Create sin el campo de horario"), r.status == 302 and cg_by("QK4", SJ_A) is not None, f"status={r.status}")
    tokenless(R, c, L("Create"), f"{B}/Create", body(name="QL4"))
    check(R, L("Create sin token no creó nada"), cg_by("QL4", SJ_A) is None)

    # ---------------------------------------------------------------- gestión completada (2025-1)
    r = post(body(name="QDONE", mg=str(MG_DONE)), "gestión completada")
    g_done = cg_by("QDONE", SJ_A, MG_DONE)
    check(R, L("Create: en una gestión COMPLETADA (solo lectura) se rechaza [hallazgo: el código solo excluye gestiones eliminadas]"),
          g_done is None, f"status={r.status} errores={r.errors[:2]} fila creada={g_done}")

    # ---------------------------------------------------------------- Index
    q = {"SearchTerm": "QB4", "ManagementFilter": MG}
    _, page = c.get(f"{B}?{urllib.parse.urlencode(q)}")
    check(R, L("Index: buscar por nombre muestra QB4 con enlaces Edit/Delete"), f"{B}/Edit/{gA}" in page and f"{B}/Delete/{gA}" in page)
    _, page = c.get(f"{B}?{urllib.parse.urlencode({**q, 'SubjectFilter': SJ_A})}")
    check(R, L("Index: filtro por materia 34 muestra el de la materia 34 y oculta el de la 41"), f"{B}/Edit/{gA}" in page and f"{B}/Edit/{gB}" not in page)
    _, page = c.get(f"{B}?{urllib.parse.urlencode({**q, 'TeacherFilter': T2})}")
    check(R, L("Index: filtro por docente T2 muestra el grupo de la materia 41"), f"{B}/Edit/{gB}" in page and f"{B}/Edit/{gA}" not in page)
    _, page = c.get(f"{B}?{urllib.parse.urlencode({**q, 'SemesterFilter': 4})}")
    check(R, L("Index: filtro por semestre 4"), f"{B}/Edit/{gA}" in page)
    _, page = c.get(f"{B}?{urllib.parse.urlencode({**q, 'SemesterFilter': 7})}")
    check(R, L("Index: filtro por semestre 7 no muestra QB4"), f"{B}/Edit/{gA}" not in page)
    _, page = c.get(f"{B}?{urllib.parse.urlencode({**q, 'StatusFilter': 1})}")
    check(R, L("Index: filtro Inactivo no muestra QB4 activo"), f"{B}/Edit/{gA}" not in page)
    _, page = c.get(f"{B}?{urllib.parse.urlencode({'SearchTerm': 'QB4', 'ManagementFilter': 3})}")
    check(R, L("Index: otra gestión (2026-1) no lo muestra"), f"{B}/Edit/{gA}" not in page)
    for qs in ({"ManagementFilter": "abc"}, {"SubjectFilter": "99999"}, {"TeacherFilter": "abc"}, {"SemesterFilter": "99"}, {"StatusFilter": "9"}, {"pageIndex": "99999"}, {"pageIndex": "-1"}, {"SearchTerm": "%'<>"}):
        st, _ = c.get(f"{B}?{urllib.parse.urlencode(qs)}")
        check(R, L(f"Index: {qs} responde 200"), st == 200, f"status={st}")

    # ---------------------------------------------------------------- Edit
    action, fields = c.form(f"{B}/Edit/{gA}")
    print(f"[{LABEL}] campos Edit:", [(k, v[:14]) for k, v in fields if k != "__RequestVerificationToken"])
    r = c.submit(f"{B}/Edit/{gA}", {"Input.Name": "QB5", "Input.StudentCount": "30"})
    expect(R, L("Edit: renombrar y cambiar estudiantes (válido)"), r, status=302, redirect=B, toast_kind="success", toast=f"Grupo '{row[1]}' (QB5) actualizado correctamente.")
    check(R, L("Edit: BD con nombre QB5, 30 estudiantes y código intacto"), cg(gA)[4] == "QB5" and cg(gA)[7] == 30 and cg(gA)[1] == row[1], str(cg(gA)))
    expect(R, L("Edit: guardar sin cambios"), c.submit(f"{B}/Edit/{gA}", {}), status=302, redirect=B)
    expect(R, L("Edit: nombre vacío"), c.submit(f"{B}/Edit/{gA}", {"Input.Name": ""}), status=200, error="El nombre del grupo es obligatorio")
    expect(R, L("Edit: nombre de 21 caracteres"), c.submit(f"{B}/Edit/{gA}", {"Input.Name": "Q" * 21}), status=200, error="El nombre no puede superar los 20 caracteres")
    expect(R, L("Edit: duplicado con otro grupo de la misma materia y gestión (otra capitalización/espacios alrededor)"),
           c.submit(f"{B}/Edit/{gA}", {"Input.Name": " qd 4 "}), status=200, error="Ya existe otro grupo con")
    r = c.submit(f"{B}/Edit/{gA}", {"Input.Name": "QD4"})
    check(R, L("Edit: 'QD4' choca con 'QD 4' (misma comparación que Create ignora espacios) [hallazgo extra]"), r.status == 200 and bool(r.errors) and cg(gA)[4] == "QB5",
          f"status={r.status} nombre en BD={cg(gA)[4]} (la comparación quita TODOS los espacios del dato nuevo pero solo recorta los de la BD)")
    if cg(gA)[4] != "QB5":
        c.submit(f"{B}/Edit/{gA}", {"Input.Name": "QB5"})
    expect(R, L("Edit: docente técnico"), c.submit(f"{B}/Edit/{gA}", {"Input.TeacherPersonId": str(tech)}), status=200, error="El docente seleccionado no existe o no está activo.")
    expect(R, L("Edit: docente vacío"), c.submit(f"{B}/Edit/{gA}", {"Input.TeacherPersonId": ""}), status=200, error="Debe seleccionar el docente")
    expect(R, L("Edit: materia dada de baja"), c.submit(f"{B}/Edit/{gA}", {"Input.SubjectId": str(sj_dead)}), status=200, error="La materia seleccionada no existe o no está activa.")
    expect(R, L("Edit: gestión inexistente"), c.submit(f"{B}/Edit/{gA}", {"Input.ManagementId": "99999"}), status=200, error="La gestión seleccionada no existe")
    expect(R, L("Edit: laboratorio inexistente"), c.submit(f"{B}/Edit/{gA}", {"Input.LaboratoryId": "99999"}), status=200, error="El laboratorio seleccionado no existe o no está activo.")
    expect(R, L("Edit: estudiantes 1001"), c.submit(f"{B}/Edit/{gA}", {"Input.StudentCount": "1001"}), status=200, error="El número de estudiantes debe estar entre 0 y 1000")
    expect(R, L("Edit: factor 1001"), c.submit(f"{B}/Edit/{gA}", {"Input.ScalingFactor": "1001"}), status=200, error="El factor debe estar entre 0 y 1000")
    expect(R, L("Edit: horario con JSON inválido"), c.submit(f"{B}/Edit/{gA}", {"Input.SlotsJson": "{{no"}), status=200, error="El horario enviado no es válido: vuelva a marcar los periodos.")
    expect(R, L("Edit: cambiar docente a T2 y laboratorio"), c.submit(f"{B}/Edit/{gA}", {"Input.TeacherPersonId": str(T2), "Input.LaboratoryId": str(LAB)}), status=302, redirect=B)
    check(R, L("Edit: docente y laboratorio nuevos en BD"), cg(gA)[5] == T2 and cg(gA)[6] == LAB, str(cg(gA)))
    # horario
    expect(R, L("Edit: guardar horario nuevo (lunes P3, martes P4)"), c.submit(f"{B}/Edit/{gA}", {"Input.SlotsJson": json.dumps([{"weekday": 1, "order": 3}, {"weekday": 2, "order": 4}])}), status=302, redirect=B)
    check(R, L("Edit: horario guardado en CourseGroupScheduleSlots"), slots(gA) == [(1, 3), (2, 4)], str(slots(gA)))
    expect(R, L("Edit: horario vacío borra las celdas"), c.submit(f"{B}/Edit/{gA}", {"Input.SlotsJson": "[]"}), status=302, redirect=B)
    check(R, L("Edit: sin celdas tras guardar []"), slots(gA) == [], str(slots(gA)))
    # estado
    expect(R, L("Edit: Inactivo"), c.submit(f"{B}/Edit/{gA}", {"Input.Status": "1"}), status=302, redirect=B)
    check(R, L("Edit: Inactivo en BD"), cg(gA)[9] == 1)
    r = c.submit(f"{B}/Edit/{gA}", {"Input.Status": "0"})
    expect(R, L("Edit: volver a Activo"), r, status=302, redirect=B)
    r = c.submit(f"{B}/Edit/{gA}", {"Input.Status": "7"})
    check(R, L("Edit: Estado=7 (fuera del enum) no cambia la BD ni da 500"), r.status == 200 and cg(gA)[9] == 0, f"status={r.status}")
    check(R, L("Edit: Estado=7 muestra un mensaje de error visible"), bool(r.errors), f"errores visibles={r.errors[:2]}")
    r = c.submit(f"{B}/Edit/{gE}", {"Input.Status": "2"})
    check(R, L("Edit: Estado=Eliminado forzado por POST se rechaza [hallazgo extra]"), r.status == 200 and cg(gE) is not None and cg(gE)[9] != 2,
          f"status={r.status} Status BD={cg(gE)[9] if cg(gE) else 'fila inexistente'}")
    for bad in ("99999", "0", "-1", "abc"):
        check_404(R, c, LABEL, f"{B}/Edit/{bad}")
        check_404(R, c, LABEL, f"{B}/Delete/{bad}")
    post_404(R, c, L("Edit POST id inexistente"), f"{B}/Edit/99999", [("Input.Id", "99999"), ("Input.ManagementId", MG), ("Input.SubjectId", SJ_A), ("Input.Name", "QZZ"), ("Input.TeacherPersonId", T1)], token_from=TOKEN)
    post_404(R, c, L("Delete POST id inexistente"), f"{B}/Delete/99999", [], token_from=TOKEN)
    tokenless(R, c, L("Edit"), f"{B}/Edit/{gA}", [("Input.Id", gA), ("Input.Name", "QSIN"), ("Input.ManagementId", MG), ("Input.SubjectId", SJ_A), ("Input.TeacherPersonId", T1)])
    tokenless(R, c, L("Delete"), f"{B}/Delete/{gA}", [])
    check(R, L("POST sin token no modificó nada"), cg(gA)[4] == "QB5" and cg(gA) is not None)
    # choque de horario entre materias del mismo cohorte al editar (QG4 de la materia 34 ocupa L P1-2 y M P1; se crea QG4 en la 41 y se intenta lo mismo)
    r = post(body(name="QG4", subject=SJ_B, teacher=str(T2)), "QG4 materia 41 sin horario")
    gG2 = cg_by("QG4", SJ_B)
    r = c.submit(f"{B}/Edit/{gG2}", {"Input.SlotsJson": json.dumps([{"weekday": 2, "order": 1}])}) if gG2 else None
    check(R, L("Edit: celda del cohorte ya ocupada por otra materia se rechaza con mensaje"), r is not None and r.status == 200 and bool(r.errors) and slots(gG2) == [],
          f"status={r.status if r else None} errores={r.errors[:2] if r else None} slots={slots(gG2) if gG2 else None}")
    check(R, L("Edit: el rechazo por choque revirtió el grupo (transacción)"), gG2 is not None and cg(gG2)[4] == "QG4")

    # ---------------------------------------------------------------- Edit de un grupo que pertenece a un StudentGroup -> redirige al grupo
    TG = f"/Catalogs/StudentGroups/Create?managementId={MG}"
    rs = c.raw_post("/Catalogs/StudentGroups/Create", [("Input.ManagementId", MG), ("Input.CareerId", 3), ("Input.Semester", 7), ("Input.Letter", "QCG"), ("Input.StudentCount", "10"),
                                                       ("Input.Subjects[0].SubjectId", 15), ("Input.Subjects[0].Selected", "true"), ("Input.Subjects[0].TeacherPersonId", T1)],
                    token_from=TG, name="grupo de estudiantes QCG7")
    sgid = scalar("select top 1 Id from StudentGroups where Name='QCG7' and ManagementId=? order by Id desc", MG)
    cg_member = scalar("select top 1 Id from CourseGroups where StudentGroupId=?", sgid) if sgid else None
    check(R, L("Preparación: grupo de estudiantes QCG7 con una materia"), sgid and cg_member, f"status={rs.status} errores={rs.errors[:2]}")
    if cg_member:
        st, loc = get_nofollow(c, f"{B}/Edit/{cg_member}")
        check(R, L("Edit GET de un grupo de materia que es parte de un StudentGroup redirige a StudentGroups/Edit"),
              st == 302 and f"/Catalogs/StudentGroups/Edit/{sgid}" in (loc or ""), f"status={st} loc={loc}")
        st, page = c.get(f"{B}/Edit/{cg_member}")
        check(R, L("  ...con el aviso Info 'La materia es parte del grupo QCG7...'"), any("La materia es parte del grupo QCG7" in t and k == "info" for k, t in parse_messages(page)[1]),
              str(parse_messages(page)[1][:2]))
        # el POST directo al Edit de un grupo vinculado (el JS no lo ofrece)
        r = c.raw_post(f"{B}/Edit/{cg_member}", [("Input.Id", cg_member), ("Input.ManagementId", MG), ("Input.SubjectId", 15), ("Input.Name", "QXX"), ("Input.TeacherPersonId", T1), ("Input.Status", "0"), ("Input.SlotsJson", "[]")],
                       token_from=TOKEN, name="POST a grupo vinculado")
        check(R, L("Edit POST directo a un grupo vinculado a un StudentGroup no lo desincroniza de su grupo [hallazgo extra]"),
              cg(cg_member)[4] == "QCG7", f"status={r.status} nombre en BD={cg(cg_member)[4]} (el grupo de estudiantes se llama QCG7)")
        # eliminar el grupo de materia vinculado deja el StudentGroup con una materia menos
        r = c.submit(f"{B}/Delete/{cg_member}", {})
        check(R, L("Delete de un grupo de materia vinculado a un StudentGroup funciona sin 500 [observación]"), r.status == 302, f"status={r.status} toasts={r.toasts[:1]}")
        c.raw_post(f"/Catalogs/StudentGroups?handler=Delete&id={sgid}", [], token_from=f"/Catalogs/StudentGroups?managementId={MG}")

    # ---------------------------------------------------------------- Delete
    st, page = c.get(f"{B}/Delete/{gD}")
    check(R, L("Delete GET muestra la confirmación (200)"), st == 200 and "QD 4" in page)
    r = c.submit(f"{B}/Delete/{gD}", {})
    expect(R, L("Delete: borrado del grupo"), r, status=302, redirect=B, toast_kind="success", toast="Grupo eliminado. Las planificaciones conservan la materia y el nombre del grupo.")
    check(R, L("Delete: borrado físico (la fila ya no existe) y su horario"), cg(gD) is None)
    _, page = c.get(f"{B}?{urllib.parse.urlencode({'SearchTerm': 'QD 4', 'ManagementFilter': MG})}")
    check(R, L("Index: el grupo borrado desaparece"), f"{B}/Edit/{gD}" not in page)
    r = c.raw_post(f"{B}/Delete/{gD}", [], token_from=TOKEN, name="borrar otra vez")
    check(R, L("Delete: borrar de nuevo -> 404"), r.status == 404, f"status={r.status}")
    g_sch = cg_by("QG4", SJ_A)
    n_slots_before = scalar("select count(*) from CourseGroupScheduleSlots where CourseGroupId=?", g_sch)
    r = c.submit(f"{B}/Delete/{g_sch}", {})
    check(R, L("Delete: borra también las celdas de horario del grupo"), r.status == 302 and scalar("select count(*) from CourseGroupScheduleSlots where CourseGroupId=?", g_sch) == 0 and n_slots_before > 0,
          f"celdas antes={n_slots_before}")

    # ---------------------------------------------------------------- Horario por cohorte: errores
    H = f"{B}/Horario"
    st, _ = c.get(H)
    check(R, L("Horario: sin parámetros responde 200"), st == 200, f"status={st}")
    st, page = c.get(f"{H}?managementId=99999")
    check(R, L("Horario: gestión inexistente -> 'La gestión solicitada no existe.'"), st == 200 and any("La gestión solicitada no existe." in t for _, t in parse_messages(page)[1]), f"status={st}")
    for coh in ("abc", "3", "0|A", "13|A", "4|", "4|" + "x" * 21, "a|b|c"):
        st, _ = c.get(f"{H}?managementId={MG}&careerId=3&cohort={urllib.parse.quote(coh)}")
        check(R, L(f"Horario: cohorte malformado {coh[:12]!r} se ignora (200)"), st == 200, f"status={st}")
    st, page = c.get(f"{H}?managementId={MG}&careerId=3&cohort={urllib.parse.quote('4|NOEXISTE')}")
    check(R, L("Horario: cohorte inexistente -> aviso 'El cohorte solicitado no existe en esta gestión.'"),
          st == 200 and any("El cohorte solicitado no existe en esta gestión." in t and k == "warning" for k, t in parse_messages(page)[1]), f"status={st} {parse_messages(page)[1][:2]}")
    st, _ = c.get(f"{H}?managementId={MG}&careerId=99999")
    check(R, L("Horario: carrera inexistente se ignora (200)"), st == 200, f"status={st}")
    st, _ = c.get(f"{H}?managementId={MG}&careerId=3&cohort={urllib.parse.quote('4|QB4')}")
    check(R, L("Horario: cohorte existente 4|QB4 responde 200"), st == 200, f"status={st}")
    HT = f"{H}?managementId={MG}&careerId=3&cohort={urllib.parse.quote('4|QB4')}"
    r = c.raw_post(H, [("SlotsJson", "{}")], token_from=HT, name="horario sin datos")
    expect(R, L("Horario POST sin gestión/carrera/cohorte"), r, status=302, toast="Faltan datos del cohorte: vuelva a seleccionarlo.", toast_kind="error")
    r = c.raw_post(f"{H}?managementId=99999&careerId=3&cohort={urllib.parse.quote('4|QB4')}", [("SlotsJson", "{}")], token_from=HT, name="horario gestión inexistente")
    expect(R, L("Horario POST gestión inexistente"), r, status=302, toast="La gestión solicitada no existe.", toast_kind="error")
    r = c.raw_post(f"{H}?managementId={MG_DONE}&careerId=3&cohort={urllib.parse.quote('4|QB4')}", [("SlotsJson", "{}")], token_from=HT, name="horario gestión completada")
    expect(R, L("Horario POST gestión completada"), r, status=302, toast="está completada: el horario es de solo lectura.", toast_kind="error")
    r = c.raw_post(HT, [("SlotsJson", "{{no")], token_from=HT, name="horario json malo")
    expect(R, L("Horario POST con JSON inválido"), r, status=302, toast="No se pudieron leer los datos del horario. Intente nuevamente.", toast_kind="error")
    r = c.raw_post(HT, [("SlotsJson", json.dumps({str(i): [] for i in range(1, 102)}))], token_from=HT, name="horario 101 grupos")
    expect(R, L("Horario POST con más de 100 grupos"), r, status=302, toast="No se pudieron leer los datos del horario. Intente nuevamente.", toast_kind="error")
    r = c.raw_post(HT, [("SlotsJson", json.dumps({"1": [{"weekday": 1, "periodOrder": 1}] * 73}))], token_from=HT, name="horario 73 celdas")
    expect(R, L("Horario POST con más de 72 celdas en un grupo"), r, status=302, toast="No se pudieron leer los datos del horario. Intente nuevamente.", toast_kind="error")
    r = c.raw_post(HT, [("SlotsJson", json.dumps({"999999": [{"weekday": 1, "periodOrder": 1}]}))], token_from=HT, name="horario grupo ajeno")
    check(R, L("Horario POST con un grupo ajeno al cohorte: error controlado sin 500"), r.status == 302 and bool(r.toasts), f"status={r.status} toasts={r.toasts[:2]}")
    gB5 = cg_by("QB5", SJ_A)
    r = c.raw_post(HT.replace("4%7CQB4", "4%7CQB5"), [("SlotsJson", json.dumps({str(gB5): [{"weekday": 4, "periodOrder": 5}]}))], token_from=HT, name="horario válido")
    expect(R, L("Horario POST válido: guarda el horario del cohorte 4|QB5"), r, status=302, toast_kind="success", toast="Horario del cohorte")
    check(R, L("Horario: celda jueves P5 guardada"), slots(gB5) == [(4, 5)], str(slots(gB5)))
    tokenless(R, c, L("Horario"), HT, [("SlotsJson", "{}")])

    # ---------------------------------------------------------------- limpieza
    ids = [x[0] for x in sql("select Id from CourseGroups where Id>? ", max_cg0)]
    for gid in ids:
        c.raw_post(f"{B}/Delete/{gid}", [], token_from=TOKEN, name="limpieza")
    left = scalar("select count(*) from CourseGroups where Id>?", max_cg0)
    after_cg = scalar("select count(*) from CourseGroups")
    after_sg = scalar("select count(*) from StudentGroups")
    check(R, L("Limpieza: CourseGroups y StudentGroups vuelven al recuento inicial"), left == 0 and after_cg == n_cg0 and after_sg == n_sg0,
          f"CourseGroups {n_cg0}->{after_cg}; StudentGroups {n_sg0}->{after_sg}; restantes (Id>{max_cg0})={left}")
    c.submit("/Catalogs/Subjects/Delete/%d" % sj_dead, {})


if __name__ == "__main__":
    run_module("coursegroups", run)
