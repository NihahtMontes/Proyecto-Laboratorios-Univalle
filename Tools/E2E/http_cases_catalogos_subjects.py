"""
Catálogos / Materias (Create, Edit, Delete, Kit y validación de errores de Horario): reglas del servidor por HTTP.
Solo contra la copia QA. Nombres con prefijo 'QAC'.
"""
import json
import re
import urllib.parse

from http_forms import parse_messages
from http_cases_catalogos_common import (check, check_404, check_anon, expect, post_404, run_module, scalar, sql, tag, tokenless, uniq)

B = "/Catalogs/Subjects"
LABEL = "Subjects"


def L(s):
    return f"{LABEL}/{s}"


def subj(sid):
    r = sql("select Id, Name, Code, CareerId, Semester, Status from Subjects where Id=?", sid)
    return tuple(r[0]) if r else None


def sid_by_name(name):
    return scalar("select top 1 Id from Subjects where Name=? order by Id desc", name)


def kit_rows(sid, removed=False):
    return [tuple(r) for r in sql("select Id, ArticleId, Quantity, IsScalable, Observations, IsRemoved from SubjectKitItems where SubjectId=? and IsRemoved=? order by Id",
                                  sid, 1 if removed else 0)]


def run(R, c):
    # ---------------------------------------------------------------- acceso sin sesión
    for p in (B, f"{B}/Create", f"{B}/Edit/14", f"{B}/Delete/14", f"{B}/Kit/14", f"{B}/Horario?id=14"):
        check_anon(R, LABEL, p)
    check_anon(R, LABEL, f"{B}/Create", post=True)
    check_anon(R, LABEL, f"{B}/Kit/14?handler=AddBatch", post=True)

    # ---------------------------------------------------------------- Create: validaciones
    nm = uniq("QAC Mat")
    base = {"Input.Name": nm, "Input.CareerId": "1", "Input.Semester": "2"}
    ov = lambda **k: {**base, **k}  # noqa: E731

    expect(R, L("Create: nombre vacío"), c.submit(f"{B}/Create", ov(**{"Input.Name": ""})), status=200, error="El nombre de la materia es obligatorio")
    expect(R, L("Create: nombre solo espacios"), c.submit(f"{B}/Create", ov(**{"Input.Name": "   "})), status=200, error="El nombre de la materia es obligatorio")
    expect(R, L("Create: 256 caracteres"), c.submit(f"{B}/Create", ov(**{"Input.Name": "x" * 256})), status=200, error="no puede superar los 255")
    expect(R, L("Create: carrera vacía"), c.submit(f"{B}/Create", ov(**{"Input.CareerId": ""})), status=200,
           error="Debe seleccionar la carrera a la que pertenece la materia")
    expect(R, L("Create: carrera=0"), c.submit(f"{B}/Create", ov(**{"Input.CareerId": "0"})), status=200, error="Debe seleccionar una carrera válida")
    expect(R, L("Create: carrera inexistente (99999)"), c.submit(f"{B}/Create", ov(**{"Input.CareerId": "99999"})), status=200,
           error="La carrera seleccionada no existe o no está activa.")
    for sem, msg in (("0", "El semestre debe estar entre 1 y 12"), ("13", "El semestre debe estar entre 1 y 12"), ("-1", "El semestre debe estar entre 1 y 12")):
        expect(R, L(f"Create: semestre {sem}"), c.submit(f"{B}/Create", ov(**{"Input.Semester": sem})), status=200, error=msg)
    r = c.submit(f"{B}/Create", ov(**{"Input.Semester": "abc"}))
    check(R, L("Create: semestre 'abc' se rechaza sin 500"), r.status == 200 and bool(r.errors), f"status={r.status} errores={r.errors[:2]}")
    r = c.submit(f"{B}/Create", ov(**{"Input.Semester": "1.5"}))
    check(R, L("Create: semestre 1.5 se rechaza sin 500"), r.status == 200 and bool(r.errors), f"status={r.status} errores={r.errors[:2]}")
    check(R, L("Create: ninguna validación fallida creó filas"), scalar("select count(*) from Subjects where Name=?", nm) == 0)

    # ---------------------------------------------------------------- Create: válidos y bordes
    r = c.submit(f"{B}/Create", ov())
    expect(R, L("Create: válido (carrera 1, semestre 2)"), r, status=302, redirect=B, toast_kind="success", toast="registrada exitosamente.")
    sid = sid_by_name(nm)
    row = subj(sid) if sid else None
    check(R, L("Create: fila en BD (nombre, código, carrera, semestre, Activo)"), row and row[1] == nm and row[2] and row[3] == 1 and row[4] == 2 and row[5] == 0, str(row))
    check(R, L("Create: el toast muestra «Materia 'CÓDIGO' (Nombre) registrada exitosamente.»"),
          bool(r.toasts) and r.toasts[0][1] == f"Materia '{row[2]}' ({nm}) registrada exitosamente." if row else False, str(r.toasts[:1]))

    expect(R, L("Create: duplicado en la misma carrera (otra capitalización y espacios)"),
           c.submit(f"{B}/Create", ov(**{"Input.Name": f"  {nm.upper()}  "})), status=200, error="Ya existe una materia con este nombre en la carrera seleccionada.")
    expect(R, L("Create: duplicado con espacios internos dobles"), c.submit(f"{B}/Create", ov(**{"Input.Name": nm.replace(' ', '   ')})),
           status=200, error="Ya existe una materia con este nombre en la carrera seleccionada.")
    expect(R, L("Create: mismo nombre en OTRA carrera (permitido)"), c.submit(f"{B}/Create", ov(**{"Input.CareerId": "2"})), status=302, redirect=B)
    sid_c2 = scalar("select top 1 Id from Subjects where Name=? and CareerId=2 order by Id desc", nm)
    check(R, L("Create: existen 2 filas con el mismo nombre en carreras distintas"), scalar("select count(*) from Subjects where Name=?", nm) == 2)

    nm_sem = uniq("QAC Mat sem")
    created = {"sem1": None, "sem12": None, "nosem": None}
    expect(R, L("Create: semestre 1 (límite inferior)"), c.submit(f"{B}/Create", {"Input.Name": nm_sem + " a", "Input.CareerId": "1", "Input.Semester": "1"}), status=302, redirect=B)
    expect(R, L("Create: semestre 12 (límite superior)"), c.submit(f"{B}/Create", {"Input.Name": nm_sem + " b", "Input.CareerId": "1", "Input.Semester": "12"}), status=302, redirect=B)
    expect(R, L("Create: sin semestre (opcional)"), c.submit(f"{B}/Create", {"Input.Name": nm_sem + " c", "Input.CareerId": "1", "Input.Semester": ""}), status=302, redirect=B)
    created["sem1"], created["sem12"], created["nosem"] = (sid_by_name(nm_sem + s) for s in (" a", " b", " c"))
    check(R, L("Create: sin semestre se guarda con Semester NULL"), created["nosem"] and subj(created["nosem"])[4] is None, str(subj(created["nosem"]) if created["nosem"] else None))
    exact = ("QAC Mat " + tag() + " " + "z" * 255)[:255]
    expect(R, L("Create: 255 caracteres exactos (válido)"), c.submit(f"{B}/Create", ov(**{"Input.Name": exact})), status=302, redirect=B)
    exact_id = sid_by_name(exact)

    html_name = f"<b>QAC Mat</b> {tag()}"
    expect(R, L("Create: nombre con HTML se acepta sin excepción"), c.submit(f"{B}/Create", ov(**{"Input.Name": html_name})), status=302, redirect=B)
    html_id = sid_by_name(html_name)
    if html_id:
        _, page = c.get(f"{B}?{urllib.parse.urlencode({'SearchTerm': html_name.split('</b> ')[1]})}")
        check(R, L("Index: el HTML hostil se muestra escapado"), html_name not in page and "&lt;b&gt;" in page)
    acc = f"{tag()} Cocción ñoño áéíóú «QAC» \"comillas\" 'simples'"
    expect(R, L("Create: tildes, ñ, comillas y símbolos"), c.submit(f"{B}/Create", ov(**{"Input.Name": acc})), status=302, redirect=B)
    acc_id = sid_by_name(acc)
    check(R, L("Create: el nombre con tildes/ñ/comillas se guarda idéntico"), acc_id is not None, repr(acc))

    # ---------------------------------------------------------------- listado
    _, page = c.get(f"{B}?{urllib.parse.urlencode({'SearchTerm': nm})}")
    check(R, L("Index: buscar por nombre encuentra la materia"), f"{B}/Edit/{sid}" in page)
    _, page = c.get(f"{B}?{urllib.parse.urlencode({'SearchTerm': nm, 'CareerFilter': 2})}")
    check(R, L("Index: filtro por carrera 2 muestra la copia de otra carrera y oculta la de la carrera 1"),
          f"{B}/Edit/{sid_c2}" in page and f"{B}/Edit/{sid}" not in page)
    _, page = c.get(f"{B}?{urllib.parse.urlencode({'SearchTerm': nm, 'SemesterFilter': 2})}")
    check(R, L("Index: filtro por semestre 2"), f"{B}/Edit/{sid}" in page)
    _, page = c.get(f"{B}?{urllib.parse.urlencode({'SearchTerm': nm, 'SemesterFilter': 7})}")
    check(R, L("Index: filtro por semestre 7 no muestra la materia de semestre 2"), f"{B}/Edit/{sid}" not in page)
    for q in ({"CareerFilter": "abc"}, {"SemesterFilter": "99"}, {"StatusFilter": "9"}, {"pageIndex": "99999"}, {"pageIndex": "-2"}, {"SearchTerm": "%'<>"}):
        st, _ = c.get(f"{B}?{urllib.parse.urlencode(q)}")
        check(R, L(f"Index: {q} responde 200"), st == 200, f"status={st}")

    # ---------------------------------------------------------------- Edit
    action, fields = c.form(f"{B}/Edit/{sid}")
    print(f"[{LABEL}] campos Edit:", [k for k, _ in fields if k != "__RequestVerificationToken"])
    nm2 = uniq("QAC Mat ed")
    r = c.submit(f"{B}/Edit/{sid}", {"Input.Name": nm2})
    expect(R, L("Edit: renombrar (válido)"), r, status=302, redirect=B, toast_kind="success", toast="actualizada correctamente.")
    check(R, L("Edit: nombre nuevo en BD y código intacto"), subj(sid)[1] == nm2 and subj(sid)[2] == row[2], str(subj(sid)))
    expect(R, L("Edit: guardar sin cambios (no choca consigo mismo)"), c.submit(f"{B}/Edit/{sid}", {"Input.Name": nm2}), status=302, redirect=B)
    expect(R, L("Edit: solo cambia capitalización de su propio nombre"), c.submit(f"{B}/Edit/{sid}", {"Input.Name": nm2.upper()}), status=302, redirect=B)
    expect(R, L("Edit: nombre vacío"), c.submit(f"{B}/Edit/{sid}", {"Input.Name": ""}), status=200, error="El nombre de la materia es obligatorio")
    expect(R, L("Edit: 256 caracteres"), c.submit(f"{B}/Edit/{sid}", {"Input.Name": "x" * 256}), status=200, error="no puede superar los 255")
    expect(R, L("Edit: carrera vacía"), c.submit(f"{B}/Edit/{sid}", {"Input.Name": nm2, "Input.CareerId": ""}), status=200,
           error="Debe seleccionar la carrera a la que pertenece la materia")
    expect(R, L("Edit: carrera inexistente"), c.submit(f"{B}/Edit/{sid}", {"Input.Name": nm2, "Input.CareerId": "99999"}), status=200,
           error="La carrera seleccionada no existe o no está activa.")
    expect(R, L("Edit: semestre 13"), c.submit(f"{B}/Edit/{sid}", {"Input.Name": nm2, "Input.Semester": "13"}), status=200, error="El semestre debe estar entre 1 y 12")
    expect(R, L("Edit: semestre 0"), c.submit(f"{B}/Edit/{sid}", {"Input.Name": nm2, "Input.Semester": "0"}), status=200, error="El semestre debe estar entre 1 y 12")
    dup_target = subj(created["sem1"])[1]
    expect(R, L("Edit: nombre de otra materia de la misma carrera (otra capitalización/espacios)"),
           c.submit(f"{B}/Edit/{sid}", {"Input.Name": f" {dup_target.upper()} "}), status=200, error="Ya existe otra materia con este nombre en la carrera seleccionada.")
    expect(R, L("Edit: cambiar a la carrera 2 con un nombre ya usado allí"),
           c.submit(f"{B}/Edit/{sid}", {"Input.Name": nm, "Input.CareerId": "2"}), status=200, error="Ya existe otra materia con este nombre")
    expect(R, L("Edit: mover a la carrera 3 semestre 12 (válido)"), c.submit(f"{B}/Edit/{sid}", {"Input.Name": nm2, "Input.CareerId": "3", "Input.Semester": "12"}), status=302, redirect=B)
    check(R, L("Edit: carrera y semestre nuevos en BD"), subj(sid)[3] == 3 and subj(sid)[4] == 12, str(subj(sid)))
    expect(R, L("Edit: Inactivo"), c.submit(f"{B}/Edit/{sid}", {"Input.Name": nm2, "Input.Status": "1"}), status=302, redirect=B)
    check(R, L("Edit: Inactivo en BD"), subj(sid)[5] == 1)
    expect(R, L("Edit: volver a Activo"), c.submit(f"{B}/Edit/{sid}", {"Input.Name": nm2, "Input.Status": "0"}), status=302, redirect=B)
    r = c.submit(f"{B}/Edit/{sid}", {"Input.Name": nm2, "Input.Status": "7"})
    check(R, L("Edit: Estado=7 (fuera del enum) no cambia la BD ni da 500"), r.status == 200 and subj(sid)[5] == 0, f"status={r.status} Status BD={subj(sid)[5]}")
    check(R, L("Edit: Estado=7 muestra un mensaje de error visible (Edit.cshtml no tiene span de validación de Status)"), bool(r.errors), f"errores visibles={r.errors[:2]}")
    r = c.submit(f"{B}/Edit/{sid}", {"Input.Name": nm2, "Input.Status": "2"})
    check(R, L("Edit: Estado=Eliminado forzado por POST se rechaza (la baja solo por Dar de Baja) [hallazgo extra]"),
          r.status == 200 and subj(sid)[5] == 0, f"status={r.status} Status en BD={subj(sid)[5]} (Categorías/Unidades sí lo rechazan)")
    if subj(sid)[5] == 2:   # el servidor aceptó el estado: se restituye para seguir probando
        c.submit(f"{B}/Edit/{sid}", {"Input.Name": nm2, "Input.Status": "0"})
    for bad in ("99999", "0", "-1", "abc"):
        check_404(R, c, LABEL, f"{B}/Edit/{bad}")
        check_404(R, c, LABEL, f"{B}/Delete/{bad}")
        check_404(R, c, LABEL, f"{B}/Kit/{bad}")
    post_404(R, c, L("Edit POST id inexistente"), f"{B}/Edit/99999", [("Input.Id", "99999"), ("Input.Name", "QAC fantasma"), ("Input.CareerId", "1")], token_from=f"{B}/Create")
    post_404(R, c, L("Delete POST id inexistente"), f"{B}/Delete/99999", [], token_from=f"{B}/Create")
    tokenless(R, c, L("Create"), f"{B}/Create", [("Input.Name", "QAC sintoken"), ("Input.CareerId", "1")])
    tokenless(R, c, L("Edit"), f"{B}/Edit/{sid}", [("Input.Id", sid), ("Input.Name", "QAC sintoken"), ("Input.CareerId", "1")])
    tokenless(R, c, L("Delete"), f"{B}/Delete/{sid}", [])
    check(R, L("POST sin token no modificó nada"), subj(sid)[1] == nm2 and subj(sid)[5] == 0)

    # ---------------------------------------------------------------- Kit
    st, text = c.text(f"{B}/Kit/{sid}")
    check(R, L("Kit: materia nueva muestra 'Esta materia aún no tiene kit.'"), st == 200 and "Esta materia aún no tiene kit." in text, text[:120])
    arts = [r[0] for r in sql("select top 4 Id from Articles where Status=0 and EquipmentId is null order by Id")]
    a1, a2, a3, a4 = arts
    kit_url = f"{B}/Kit/{sid}"

    def kpost(handler, fields, name):
        return c.raw_post(f"{kit_url}?handler={handler}", fields, token_from=kit_url, name=name)

    def line(i, art, qty, kind="Articles"):
        return [(f"Batch.{kind}[{i}].Id", art), (f"Batch.{kind}[{i}].Quantity", qty)]

    r = kpost("AddBatch", [], "kit vacío")
    expect(R, L("Kit/AddBatch: sin ninguna línea"), r, status=302, toast="Selecciona al menos un artículo o equipo.", toast_kind="warning")
    for qty, desc in (("0", "cantidad 0"), ("-1", "cantidad -1"), ("0.001", "cantidad 0.001 (3 decimales)"), ("1.234", "cantidad 1.234 (3 decimales)"),
                      ("999999.01", "cantidad 999999.01"), ("1000000", "cantidad 1000000"), ("abc", "cantidad 'abc'"), ("", "cantidad vacía")):
        r = kpost("AddBatch", line(0, a1, qty), desc)
        expect(R, L(f"Kit/AddBatch: {desc}"), r, status=302, toast="Las cantidades deben ser mayores a 0 y admiten hasta 2 decimales.", toast_kind="error")
    check(R, L("Kit/AddBatch: ninguna cantidad inválida creó líneas"), len(kit_rows(sid)) == 0)
    r = kpost("AddBatch", line(0, 99999999, "1"), "artículo inexistente")
    expect(R, L("Kit/AddBatch: artículo inexistente"), r, status=302, toast="Algún artículo seleccionado ya no está activo. Vuelve a buscarlo.", toast_kind="error")
    r = kpost("AddBatch", line(0, 999999, "1", "Equipments"), "equipo inexistente")
    expect(R, L("Kit/AddBatch: equipo inexistente"), r, status=302, toast="Algún equipo seleccionado ya no está activo. Vuelve a buscarlo.", toast_kind="error")
    r = kpost("AddBatch", line(0, a1, "2"), "agregar")
    expect(R, L("Kit/AddBatch: agregar un artículo (cantidad 2)"), r, status=302, toast="Kit actualizado: 1 artículo(s) agregado(s).", toast_kind="success")
    check(R, L("Kit: línea en BD con cantidad 2"), [x[1:3] for x in kit_rows(sid)] == [(a1, 2)], str(kit_rows(sid)))
    r = kpost("AddBatch", line(0, a1, "3"), "repetido")
    expect(R, L("Kit/AddBatch: repetir el artículo suma la cantidad"), r, status=302, toast="Kit actualizado: 1 cantidad(es) sumada(s).", toast_kind="success")
    check(R, L("Kit: cantidad sumada 2+3=5 en una sola línea"), [x[1:3] for x in kit_rows(sid)] == [(a1, 5)], str(kit_rows(sid)))
    r = kpost("AddBatch", line(0, a2, "1,5") + line(1, a2, "0.25") + line(2, a1, "1"), "lote mixto, coma decimal y repetido en el lote")
    expect(R, L("Kit/AddBatch: lote mixto ('1,5' + '0.25' del mismo artículo + uno existente)"), r, status=302,
           toast="Kit actualizado: 1 artículo(s) agregado(s) y 1 cantidad(es) sumada(s).", toast_kind="success")
    rows_now = {x[1]: float(x[2]) for x in kit_rows(sid)}
    check(R, L("Kit: coma decimal '1,5' se interpreta como 1.5 (1.5+0.25=1.75) y a1=6"), rows_now.get(a2) == 1.75 and rows_now.get(a1) == 6.0, str(rows_now))
    r = kpost("AddBatch", line(0, a3, "999999"), "máximo")
    expect(R, L("Kit/AddBatch: cantidad 999999 (máximo válido)"), r, status=302, toast_kind="success")
    r = kpost("AddBatch", line(0, a4, "0.01"), "mínimo")
    expect(R, L("Kit/AddBatch: cantidad 0.01 (mínimo válido)"), r, status=302, toast_kind="success")
    r = kpost("AddBatch", line(0, a1, "999999"), "suma que excede el máximo")
    qa1 = [float(x[2]) for x in kit_rows(sid) if x[1] == a1]
    check(R, L("Kit/AddBatch: la suma de líneas repetidas no puede superar el máximo 999999 [hallazgo extra]"), bool(qa1) and qa1[0] <= 999999,
          f"cantidad en BD={qa1} toasts={r.toasts[:1]}")
    tokenless(R, c, L("Kit AddBatch"), f"{kit_url}?handler=AddBatch", line(0, a1, "1"))

    items = kit_rows(sid)
    it1 = [x for x in items if x[1] == a2][0][0]
    r = kpost("Update", [("itemId", it1), ("quantity", "4.5"), ("isScalable", "true"), ("observations", "  nota   con   espacios  ")], "actualizar")
    expect(R, L("Kit/Update: editar cantidad, escalable y observaciones"), r, status=302, toast="Línea del kit actualizada.", toast_kind="success")
    cur = [x for x in kit_rows(sid) if x[0] == it1][0]
    check(R, L("Kit/Update: BD cantidad 4.5, escalable, observación limpia"), float(cur[2]) == 4.5 and cur[3] in (1, True) and cur[4] == "nota con espacios", str(cur))
    for qty, desc in (("0", "0"), ("-2", "-2"), ("0.001", "0.001"), ("1000000", "1000000"), ("abc", "'abc'"), ("", "vacía")):
        r = kpost("Update", [("itemId", it1), ("quantity", qty), ("isScalable", "false"), ("observations", "")], f"update {desc}")
        expect(R, L(f"Kit/Update: cantidad {desc}"), r, status=302, toast="La cantidad debe ser mayor a 0 y admite hasta 2 decimales.", toast_kind="error")
    r = kpost("Update", [("itemId", it1), ("quantity", "1,25"), ("isScalable", "false"), ("observations", "")], "update coma")
    expect(R, L("Kit/Update: '1,25' con coma decimal"), r, status=302, toast="Línea del kit actualizada.", toast_kind="success")
    r = kpost("Update", [("itemId", it1), ("quantity", "2"), ("isScalable", "false"), ("observations", "o" * 600)], "update obs larga")
    cur = [x for x in kit_rows(sid) if x[0] == it1][0]
    check(R, L("Kit/Update: observación de 600 caracteres se trunca a 500 sin error"), r.status == 302 and cur[4] and len(cur[4]) == 500, f"len={len(cur[4] or '')} toasts={r.toasts[:1]}")
    r = kpost("Update", [("itemId", 99999999), ("quantity", "2"), ("isScalable", "false"), ("observations", "")], "update línea inexistente")
    expect(R, L("Kit/Update: línea inexistente"), r, status=302, toast="La línea no pertenece al kit de esta materia.", toast_kind="error")
    other_item = scalar("select top 1 Id from SubjectKitItems where SubjectId<>? and IsRemoved=0 order by Id", sid)
    if other_item:
        before_other = scalar("select Quantity from SubjectKitItems where Id=?", other_item)
        r = kpost("Update", [("itemId", other_item), ("quantity", "9"), ("isScalable", "false"), ("observations", "")], "update línea ajena")
        expect(R, L("Kit/Update: línea de OTRA materia (IDOR)"), r, status=302, toast="La línea no pertenece al kit de esta materia.", toast_kind="error")
        check(R, L("Kit/Update: la línea ajena no cambió"), scalar("select Quantity from SubjectKitItems where Id=?", other_item) == before_other)
    tokenless(R, c, L("Kit Update"), f"{kit_url}?handler=Update", [("itemId", it1), ("quantity", "7")])

    r = kpost("Remove", [("itemId", it1)], "retirar")
    expect(R, L("Kit/Remove: retirar una línea"), r, status=302, toast="Artículo retirado del kit.", toast_kind="success")
    check(R, L("Kit/Remove: IsRemoved=1 en BD y desaparece de las líneas activas"), it1 in [x[0] for x in kit_rows(sid, removed=True)] and it1 not in [x[0] for x in kit_rows(sid)])
    r = kpost("Remove", [("itemId", it1)], "retirar otra vez")
    check(R, L("Kit/Remove: retirar de nuevo una línea ya retirada no falla (sin 500)"), r.status == 302, f"status={r.status} toasts={r.toasts[:1]}")
    if other_item:
        r = kpost("Remove", [("itemId", other_item)], "retirar ajena")
        check(R, L("Kit/Remove: línea de OTRA materia no se retira (IDOR)"), scalar("select IsRemoved from SubjectKitItems where Id=?", other_item) in (0, False), f"toasts={r.toasts[:1]}")
    r = kpost("AddBatch", line(0, a2, "3"), "re-agregar el retirado")
    expect(R, L("Kit/AddBatch: volver a agregar un artículo retirado crea una línea nueva"), r, status=302, toast="Kit actualizado: 1 artículo(s) agregado(s).", toast_kind="success")
    tokenless(R, c, L("Kit Remove"), f"{kit_url}?handler=Remove", [("itemId", it1)])
    post_404(R, c, L("Kit AddBatch materia inexistente"), f"{B}/Kit/99999?handler=AddBatch", line(0, a1, "1"), token_from=kit_url)

    # ---------------------------------------------------------------- Horario: solo errores
    res = c.raw_post(f"{B}/Horario", [("SlotsJson", "{}")], follow=True, name="horario sin id", token_from=f"{B}/Horario?id={sid}&managementId=4")
    expect(R, L("Horario POST sin id de materia"), res, status=302, redirect=B, toast="Debe abrir el horario desde una materia del catálogo.", toast_kind="error")
    st, page = c.get(f"{B}/Horario")
    check(R, L("Horario GET sin id: carga el listado con el error 'Debe abrir el horario desde una materia del catálogo.'"),
          st == 200 and any("Debe abrir el horario desde una materia del catálogo." in t for _, t in parse_messages(page)[1]), f"status={st} toasts={parse_messages(page)[1][:2]}")
    st, page = c.get(f"{B}/Horario?id=99999")
    check(R, L("Horario GET materia inexistente: 'La materia solicitada no existe.'"), st == 200 and "La materia solicitada no existe." in page, f"status={st}")
    st, page = c.get(f"{B}/Horario?id={sid}&managementId=99999")
    check(R, L("Horario GET gestión inexistente: 'La gestión solicitada no existe.'"),
          st == 200 and any("La gestión solicitada no existe." in t for _, t in parse_messages(page)[1]), f"status={st} toasts={parse_messages(page)[1][:2]}")
    st, page = c.get(f"{B}/Horario?id={sid}&managementId=1")
    check(R, L("Horario GET gestión completada (2025-1): modo solo lectura"), st == 200 and "solo lectura" in page.lower(), f"status={st}")
    res = c.raw_post(f"{B}/Horario?id={sid}&managementId=1", [("SlotsJson", "{}")], token_from=f"{B}/Horario?id={sid}&managementId=4", name="horario gestión completada")
    expect(R, L("Horario POST en gestión completada"), res, status=302, toast="está completada: el horario es de solo lectura.", toast_kind="error")
    hurl = f"{B}/Horario?id={sid}&managementId=4"
    res = c.raw_post(hurl, [("SlotsJson", "{{no es json")], token_from=hurl, name="horario json malo")
    expect(R, L("Horario POST con JSON inválido"), res, status=302, toast="No se pudieron leer los datos del horario. Intente nuevamente.", toast_kind="error")
    big = json.dumps({str(i): [] for i in range(1, 202)})
    res = c.raw_post(hurl, [("SlotsJson", big)], token_from=hurl, name="horario 201 grupos")
    expect(R, L("Horario POST con más de 200 grupos"), res, status=302, toast="No se pudieron leer los datos del horario. Intente nuevamente.", toast_kind="error")
    many = json.dumps({"1": [{"weekday": 1, "periodOrder": 1}] * 73})
    res = c.raw_post(hurl, [("SlotsJson", many)], token_from=hurl, name="horario 73 celdas")
    expect(R, L("Horario POST con más de 72 celdas en un grupo"), res, status=302, toast="No se pudieron leer los datos del horario. Intente nuevamente.", toast_kind="error")
    res = c.raw_post(hurl, [("SlotsJson", json.dumps({"999999": [{"weekday": 1, "periodOrder": 1}]}))], token_from=hurl, name="horario grupo ajeno")
    check(R, L("Horario POST con un grupo que no es de la materia: error controlado sin 500"), res.status == 302 and bool(res.toasts), f"status={res.status} toasts={res.toasts[:2]}")
    res = c.raw_post(hurl, [("SlotsJson", "{}")], token_from=hurl, name="horario vacío")
    expect(R, L("Horario POST '{}' (sin cambios) guarda sin error"), res, status=302, toast=f"Horario de {subj(sid)[1]} guardado.", toast_kind="success")
    tokenless(R, c, L("Horario"), hurl, [("SlotsJson", "{}")])

    # ---------------------------------------------------------------- Delete
    # una práctica activa cuelga de la materia: la baja no la comprueba (hallazgo extra)
    pn = uniq("QAC Prac dep")
    rp = c.raw_post("/Catalogs/Practices/Create", [("Input.Name", pn), ("Input.SubjectId", sid)], token_from="/Catalogs/Practices/Create", name="práctica dependiente")
    prac_id = scalar("select top 1 Id from Practices where Name=? order by Id desc", pn)
    # 2026-10-18: con prácticas activas la baja se bloquea; se da de baja primero la práctica
    r = c.submit(f"{B}/Delete/{sid}", {})
    check(R, L("Delete: con prácticas activas se bloquea con aviso y la materia sigue Activa"), r.has_toast("práctica(s) activa(s)", "warning") and subj(sid) and subj(sid)[5] == 0, f"toasts={r.toasts[:1]}")
    if prac_id:
        c.submit(f"/Catalogs/Practices/Delete/{prac_id}", {})
    r = c.submit(f"{B}/Delete/{sid}", {})
    expect(R, L("Delete: baja lógica"), r, status=302, redirect=B, toast_kind="success", toast=f"La materia '{nm2}' ha sido dada de baja del catálogo académico.")
    check(R, L("Delete: Status=Eliminado en BD (fila conservada)"), subj(sid) and subj(sid)[5] == 2, str(subj(sid)))
    _, page = c.get(f"{B}?{urllib.parse.urlencode({'SearchTerm': nm2})}")
    check(R, L("Index: la materia dada de baja desaparece del listado"), f"{B}/Edit/{sid}" not in page)
    st, _ = c.get(f"{B}/Kit/{sid}")
    check(R, L("Kit de una materia dada de baja -> 404"), st == 404, f"status={st}")
    has_prac = scalar("select Status from Practices where Id=?", prac_id) if prac_id else None
    check(R, L("Delete: tras la baja no queda ninguna práctica activa de la materia (integridad)"),
          has_prac != 0, f"práctica {prac_id} Status={has_prac} (0=Activo)")
    r = c.submit(f"{B}/Delete/{sid}", {})
    check(R, L("Delete: segunda baja de la misma materia no falla (sin 500)"), r.status == 302, f"status={r.status} toasts={r.toasts[:1]}")
    # recrear una materia dada de baja
    r = c.submit(f"{B}/Create", {"Input.Name": nm2, "Input.CareerId": "3", "Input.Semester": "12"})
    expect(R, L("Create: recrear una materia dada de baja (sospecha de índice único sin filtro de estado)"), r, status=200,
           error="Ya existe una materia con ese nombre en la carrera seleccionada.")
    # editar una materia dada de baja
    st, _ = c.get(f"{B}/Edit/{sid}")
    check(R, L("Edit: una materia dada de baja no se abre por URL [hallazgo extra]"), st == 404, f"GET Edit status={st} (la categoría tampoco lo impide)")

    # ---------------------------------------------------------------- limpieza (bajas lógicas)
    if prac_id and scalar("select Status from Practices where Id=?", prac_id) == 0:
        c.submit(f"/Catalogs/Practices/Delete/{prac_id}", {})
    for rid in (sid_c2, created["sem1"], created["sem12"], created["nosem"], exact_id, html_id, acc_id):
        if rid:
            c.submit(f"{B}/Delete/{rid}", {})
    left = scalar("select count(*) from Subjects where Name like 'QAC Mat%' and Status=0")
    check(R, L("Limpieza: no quedan materias QAC activas"), left == 0, f"quedan={left}")


if __name__ == "__main__":
    run_module("subjects", run)
