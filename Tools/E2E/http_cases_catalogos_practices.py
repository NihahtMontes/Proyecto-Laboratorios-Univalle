"""
Catálogos / Prácticas (Create, Edit, Delete y Details: receta Agregar/Editar/Retirar): reglas del servidor por HTTP.
Solo contra la copia QA. Nombres con prefijo 'QAC'. Crea su propia materia QAC para colgar las prácticas.
"""
import re
import urllib.parse

from http_cases_catalogos_common import (check, check_404, check_anon, expect, post_404, run_module, scalar, sql, tag, tokenless, uniq)

B = "/Catalogs/Practices"
LABEL = "Practices"


def L(s):
    return f"{LABEL}/{s}"


def prac(pid):
    r = sql("select Id, Name, Code, SubjectId, Status, CoverageStatus, Description from Practices where Id=?", pid)
    return tuple(r[0]) if r else None


def pid_by_name(name):
    return scalar("select top 1 Id from Practices where Name=? order by Id desc", name)


def req_rows(pid, removed=False):
    return [tuple(r) for r in sql("select Id, ArticleId, Quantity, IsScalable, Observations, IsRemoved, ValidationStatus from PracticeRequirements "
                                  "where PracticeId=? and IsRemoved=? order by Id", pid, 1 if removed else 0)]


def en(errors):
    """True si algún mensaje de validación está en inglés (mensajes por defecto de ASP.NET)."""
    return any(re.search(r"(must be|The field|is invalid|is not valid|required)", e) for e in errors)


def run(R, c):
    for p in (B, f"{B}/Create", f"{B}/Edit/1", f"{B}/Delete/1", f"{B}/Details/1"):
        check_anon(R, LABEL, p)
    check_anon(R, LABEL, f"{B}/Create", post=True)
    check_anon(R, LABEL, f"{B}/Details/1?handler=AddRequirement", post=True)

    max_prac0 = scalar("select max(Id) from Practices")
    # materias de apoyo (QAC): una activa y otra que se da de baja
    sname = uniq("QAC Mat prac")
    c.submit("/Catalogs/Subjects/Create", {"Input.Name": sname, "Input.CareerId": "1", "Input.Semester": "1"})
    sid = scalar("select top 1 Id from Subjects where Name=? order by Id desc", sname)
    sname2 = uniq("QAC Mat prac2")
    c.submit("/Catalogs/Subjects/Create", {"Input.Name": sname2, "Input.CareerId": "1", "Input.Semester": "1"})
    sid2 = scalar("select top 1 Id from Subjects where Name=? order by Id desc", sname2)
    sname3 = uniq("QAC Mat prac3")
    c.submit("/Catalogs/Subjects/Create", {"Input.Name": sname3, "Input.CareerId": "1", "Input.Semester": "1"})
    sid_dead = scalar("select top 1 Id from Subjects where Name=? order by Id desc", sname3)
    c.submit(f"/Catalogs/Subjects/Delete/{sid_dead}", {})
    check(R, L("Preparación: 2 materias QAC activas y 1 dada de baja"), sid and sid2 and sid_dead and scalar("select Status from Subjects where Id=?", sid_dead) == 2)

    nm = uniq("QAC Prac")
    base = {"Input.Name": nm, "Input.SubjectId": str(sid)}

    def ov(**k):
        return {**base, **k}

    # ---------------------------------------------------------------- Create
    expect(R, L("Create: nombre vacío"), c.submit(f"{B}/Create", ov(**{"Input.Name": ""})), status=200, error="El nombre de la práctica es obligatorio")
    expect(R, L("Create: nombre solo espacios"), c.submit(f"{B}/Create", ov(**{"Input.Name": "   "})), status=200, error="El nombre de la práctica es obligatorio")
    expect(R, L("Create: 201 caracteres"), c.submit(f"{B}/Create", ov(**{"Input.Name": "x" * 201})), status=200, error="no puede superar los 200")
    r = c.submit(f"{B}/Create", ov(**{"Input.SubjectId": ""}))
    expect(R, L("Create: materia vacía ('')"), r, status=200, error="Debe seleccionar una materia")
    r = c.submit(f"{B}/Create", {"Input.Name": nm, "Input.SubjectId": None})
    expect(R, L("Create: materia ausente del POST (el Required no actúa sobre int no anulable; mensaje del código)"), r, status=200,
           error="La materia seleccionada no existe o no está activa.")
    expect(R, L("Create: materia inexistente (99999)"), c.submit(f"{B}/Create", ov(**{"Input.SubjectId": "99999"})), status=200,
           error="La materia seleccionada no existe o no está activa.")
    expect(R, L("Create: materia dada de baja"), c.submit(f"{B}/Create", ov(**{"Input.SubjectId": str(sid_dead)})), status=200,
           error="La materia seleccionada no existe o no está activa.")
    r = c.submit(f"{B}/Create", ov(**{"Input.SubjectId": "abc"}))
    check(R, L("Create: materia 'abc' se rechaza sin 500"), r.status == 200 and bool(r.errors), f"status={r.status} errores={r.errors[:2]}")
    for bad in ("Otra", "cubierta", "CUBIERTA", "Cubierta "):
        r = c.submit(f"{B}/Create", ov(**{"Input.CoverageStatus": bad}))
        expect(R, L(f"Create: cobertura inválida {bad!r}"), r, status=200, error="El estado de cobertura seleccionado no es válido.")
    for fld, n, desc in (("Input.Description", 2001, "descripción 2001"), ("Input.CoverageMethod", 61, "método de cobertura 61"),
                         ("Input.SourceReference", 501, "fuente de referencia 501"), ("Input.Observations", 2001, "observaciones 2001")):
        r = c.submit(f"{B}/Create", ov(**{fld: "x" * n}))
        check(R, L(f"Create: {desc} caracteres se rechaza en español"), r.status == 200 and bool(r.errors) and not en(r.errors),
              f"status={r.status} errores={r.errors[:2]}")
    check(R, L("Create: ninguna validación fallida creó filas"), scalar("select count(*) from Practices where Name=?", nm) == 0)

    r = c.submit(f"{B}/Create", ov())
    expect(R, L("Create: válido (sin cobertura)"), r, status=302, redirect=B, toast_kind="success", toast="registrada exitosamente.")
    pid = pid_by_name(nm)
    row = prac(pid) if pid else None
    check(R, L("Create: fila en BD (nombre, código, materia, Activa)"), row and row[1] == nm and row[2] and row[3] == sid and row[4] == 0, str(row))
    check(R, L("Create: el toast muestra «Práctica 'CÓDIGO' registrada exitosamente.»"), bool(r.toasts) and row and r.toasts[0][1] == f"Práctica '{row[2]}' registrada exitosamente.", str(r.toasts[:1]))
    r = c.submit(f"{B}/Create", ov())
    expect(R, L("Create: mismo nombre y materia (sin unicidad por diseño)"), r, status=302, redirect=B, toast_kind="success")
    check(R, L("Create: quedan 2 prácticas con el mismo nombre"), scalar("select count(*) from Practices where Name=?", nm) == 2)
    pid_dup = scalar("select max(Id) from Practices where Name=?", nm)
    for cov in ("Cubierta", "Parcial", "Pendiente"):
        r = c.submit(f"{B}/Create", ov(**{"Input.Name": f"{nm} {cov}", "Input.CoverageStatus": cov}))
        expect(R, L(f"Create: cobertura {cov}"), r, status=302, redirect=B)
    long_ok = {"Input.Name": ("QAC " + tag() + " " + "n" * 200)[:200], "Input.Description": "d" * 2000, "Input.CoverageMethod": "m" * 60,
               "Input.SourceReference": "s" * 500, "Input.Observations": "o" * 2000}
    r = c.submit(f"{B}/Create", ov(**long_ok))
    expect(R, L("Create: todos los campos en su longitud máxima exacta"), r, status=302, redirect=B)
    long_id = pid_by_name(long_ok["Input.Name"])
    html_name = f"<b>QAC Prac</b> {tag()}"
    expect(R, L("Create: nombre con HTML se acepta sin excepción"), c.submit(f"{B}/Create", ov(**{"Input.Name": html_name, "Input.Description": "<script>alert(1)</script>"})), status=302, redirect=B)
    html_id = pid_by_name(html_name)
    if html_id:
        _, page = c.get(f"{B}?{urllib.parse.urlencode({'SearchTerm': html_name.split('</b> ')[1]})}")
        check(R, L("Index: el HTML hostil se muestra escapado"), html_name not in page and "&lt;b&gt;" in page)
        _, page = c.get(f"{B}/Details/{html_id}")
        check(R, L("Details: la descripción con <script> se muestra escapada"), "<script>alert(1)</script>" not in page)
    acc = f"{tag()} Cocción de ñoquis «QAC» \"comillas\" 'simples'"
    expect(R, L("Create: tildes, ñ, comillas"), c.submit(f"{B}/Create", ov(**{"Input.Name": acc})), status=302, redirect=B)
    acc_id = pid_by_name(acc)
    check(R, L("Create: nombre con tildes/ñ/comillas idéntico en BD"), acc_id is not None, repr(acc))
    messy = f"  QAC   {tag()}   x  "
    expect(R, L("Create: espacios sobrantes"), c.submit(f"{B}/Create", ov(**{"Input.Name": messy, "Input.Description": "  a    b  "})), status=302, redirect=B)
    messy_id = pid_by_name(" ".join(messy.split()))
    check(R, L("Create: nombre y descripción se guardan sin espacios sobrantes"), messy_id and prac(messy_id)[6] == "a b", str(prac(messy_id) if messy_id else None))

    # ---------------------------------------------------------------- Index
    _, page = c.get(f"{B}?{urllib.parse.urlencode({'SearchTerm': nm})}")
    check(R, L("Index: buscar por nombre encuentra la práctica"), f"{B}/Details/{pid}" in page)
    _, page = c.get(f"{B}?{urllib.parse.urlencode({'SearchTerm': nm, 'SubjectId': sid})}")
    check(R, L("Index: filtro por materia"), f"{B}/Details/{pid}" in page)
    _, page = c.get(f"{B}?{urllib.parse.urlencode({'SearchTerm': nm, 'SubjectId': sid2})}")
    check(R, L("Index: otra materia no la muestra"), f"{B}/Details/{pid}" not in page)
    for q in ({"CareerId": "abc"}, {"SubjectId": "99999"}, {"StatusFilter": "9"}, {"pageIndex": "99999"}, {"pageIndex": "-1"}, {"SearchTerm": "%'<>"}):
        st, _ = c.get(f"{B}?{urllib.parse.urlencode(q)}")
        check(R, L(f"Index: {q} responde 200"), st == 200, f"status={st}")

    # ---------------------------------------------------------------- Edit
    action, fields = c.form(f"{B}/Edit/{pid}")
    print(f"[{LABEL}] campos Edit:", [k for k, _ in fields if k != "__RequestVerificationToken"])
    nm2 = uniq("QAC Prac ed")
    r = c.submit(f"{B}/Edit/{pid}", {"Input.Name": nm2, "Input.CoverageStatus": "Parcial"})
    expect(R, L("Edit: renombrar y cobertura Parcial (válido)"), r, status=302, redirect=B, toast_kind="success", toast=f"Datos de la Práctica '{row[2]}' actualizados correctamente.")
    check(R, L("Edit: BD con nombre nuevo y cobertura Parcial, código intacto"), prac(pid)[1] == nm2 and prac(pid)[5] == "Parcial" and prac(pid)[2] == row[2], str(prac(pid)))
    expect(R, L("Edit: guardar sin cambios"), c.submit(f"{B}/Edit/{pid}", {"Input.Name": nm2}), status=302, redirect=B)
    expect(R, L("Edit: nombre vacío"), c.submit(f"{B}/Edit/{pid}", {"Input.Name": ""}), status=200, error="El nombre de la práctica es obligatorio")
    expect(R, L("Edit: 201 caracteres"), c.submit(f"{B}/Edit/{pid}", {"Input.Name": "x" * 201}), status=200, error="no puede superar los 200")
    expect(R, L("Edit: materia vacía"), c.submit(f"{B}/Edit/{pid}", {"Input.Name": nm2, "Input.SubjectId": ""}), status=200, error="Debe seleccionar una materia")
    expect(R, L("Edit: materia dada de baja"), c.submit(f"{B}/Edit/{pid}", {"Input.Name": nm2, "Input.SubjectId": str(sid_dead)}), status=200,
           error="La materia seleccionada no existe o no está activa.")
    expect(R, L("Edit: cobertura inválida"), c.submit(f"{B}/Edit/{pid}", {"Input.Name": nm2, "Input.CoverageStatus": "Otra"}), status=200,
           error="El estado de cobertura seleccionado no es válido.")
    expect(R, L("Edit: mover a otra materia activa"), c.submit(f"{B}/Edit/{pid}", {"Input.Name": nm2, "Input.SubjectId": str(sid2)}), status=302, redirect=B)
    check(R, L("Edit: materia nueva en BD"), prac(pid)[3] == sid2)
    expect(R, L("Edit: Inactiva"), c.submit(f"{B}/Edit/{pid}", {"Input.Name": nm2, "Input.Status": "1"}), status=302, redirect=B)
    check(R, L("Edit: Inactiva en BD"), prac(pid)[4] == 1)
    expect(R, L("Edit: volver a Activa"), c.submit(f"{B}/Edit/{pid}", {"Input.Name": nm2, "Input.Status": "0"}), status=302, redirect=B)
    r = c.submit(f"{B}/Edit/{pid}", {"Input.Name": nm2, "Input.Status": "7"})
    check(R, L("Edit: Estado=7 (fuera del enum) no cambia la BD ni da 500"), r.status == 200 and prac(pid)[4] == 0, f"status={r.status}")
    check(R, L("Edit: Estado=7 muestra un mensaje de error visible"), bool(r.errors), f"errores={r.errors[:2]}")
    r = c.submit(f"{B}/Edit/{pid_dup}", {"Input.Name": nm, "Input.Status": "2"})
    check(R, L("Edit: Estado=Eliminado forzado por POST se rechaza [hallazgo extra]"), r.status == 200 and prac(pid_dup)[4] == 0,
          f"status={r.status} Status BD={prac(pid_dup)[4]} (Categorías/Unidades sí lo rechazan)")
    for bad in ("99999", "0", "-1", "abc"):
        check_404(R, c, LABEL, f"{B}/Edit/{bad}")
        check_404(R, c, LABEL, f"{B}/Delete/{bad}")
        check_404(R, c, LABEL, f"{B}/Details/{bad}")
    post_404(R, c, L("Edit POST id inexistente"), f"{B}/Edit/99999", [("Input.Id", "99999"), ("Input.Name", "QAC fantasma"), ("Input.SubjectId", sid), ("Input.Status", "0")], token_from=f"{B}/Create")
    post_404(R, c, L("Delete POST id inexistente"), f"{B}/Delete/99999", [], token_from=f"{B}/Create")
    post_404(R, c, L("Details AddRequirement práctica inexistente"), f"{B}/Details/99999?handler=AddRequirement",
             [("RequirementInput.ArticleId", 1), ("RequirementInput.Quantity", "1")], token_from=f"{B}/Create")
    tokenless(R, c, L("Create"), f"{B}/Create", [("Input.Name", "QAC sintoken"), ("Input.SubjectId", sid)])
    tokenless(R, c, L("Edit"), f"{B}/Edit/{pid}", [("Input.Id", pid), ("Input.Name", "QAC sintoken"), ("Input.SubjectId", sid), ("Input.Status", "0")])
    tokenless(R, c, L("Delete"), f"{B}/Delete/{pid}", [])
    tokenless(R, c, L("Details AddRequirement"), f"{B}/Details/{pid}?handler=AddRequirement", [("RequirementInput.ArticleId", 1), ("RequirementInput.Quantity", "1")])
    tokenless(R, c, L("Details UpdateRequirement"), f"{B}/Details/{pid}?handler=UpdateRequirement", [("RequirementUpdate.Id", 1), ("RequirementUpdate.Quantity", "1")])
    tokenless(R, c, L("Details RemoveRequirement"), f"{B}/Details/{pid}?handler=RemoveRequirement&requirementId=1", [])
    check(R, L("POST sin token no modificó nada"), prac(pid)[1] == nm2 and prac(pid)[4] == 0)

    # ---------------------------------------------------------------- Details: receta
    durl = f"{B}/Details/{pid}"
    arts = [r[0] for r in sql("select top 4 Id from Articles where Status=0 and EquipmentId is null order by Id desc")]
    a1, a2, a3, a4 = arts
    inactive_art = scalar("select top 1 Id from Articles where Status=2 order by Id desc")
    st, text = c.text(durl)
    check(R, L("Details: GET de la práctica sin receta responde 200"), st == 200, f"status={st}")

    def rp(handler, fields, name, extra=""):
        return c.raw_post(f"{durl}?handler={handler}{extra}", fields, token_from=durl, name=name)

    # NOTA: PracticeDetails tiene dos [BindProperty] (RequirementInput y RequirementUpdate) y valida los dos en cada POST:
    # RequirementUpdate.Quantity=0 (sin enviar) incumple Range(0.01) y el "Agregar" falla en silencio. Para poder probar el resto de las
    # reglas de "Agregar" se envía RequirementUpdate.Quantity=1 (WA); el caso fiel al navegador se prueba aparte.
    WA = ("RequirementUpdate.Quantity", "1")

    def add(art, qty, sc=None, obs=None, vs=None):
        f = [("RequirementInput.ArticleId", art), ("RequirementInput.Quantity", qty), WA]
        if sc is not None:
            f.append(("RequirementInput.IsScalable", sc))
        if obs is not None:
            f.append(("RequirementInput.Observations", obs))
        if vs is not None:
            f.append(("RequirementInput.ValidationStatus", vs))
        return rp("AddRequirement", f, f"add {art} {qty}")

    r = rp("AddRequirement", [("RequirementInput.ArticleId", a1), ("RequirementInput.Quantity", "2")], "fiel al navegador")
    check(R, L("Receta/Agregar: el POST tal como lo envía el formulario del navegador (solo RequirementInput.*) agrega la línea"),
          r.has_toast("agregado", "success") and len(req_rows(pid)) == 1,
          f"status={r.status} toasts={r.toasts[:1]} errores={r.errors[:2]} líneas={len(req_rows(pid))} (silencioso: RequirementUpdate.Quantity=0 invalida el ModelState)")
    for x in req_rows(pid):
        rp("RemoveRequirement", [], "limpieza", f"&requirementId={x[0]}")
    r = rp("AddRequirement", [("RequirementInput.ArticleId", ""), ("RequirementInput.Quantity", "1"), WA], "sin artículo ''")
    expect(R, L("Receta/Agregar: artículo vacío ('')"), r, status=200, error="Debe seleccionar un artículo")
    r = rp("AddRequirement", [("RequirementInput.Quantity", "1"), WA], "sin artículo ausente")
    expect(R, L("Receta/Agregar: artículo ausente del POST (el Required no actúa sobre int no anulable; mensaje del código)"), r, status=200,
           error="El artículo seleccionado no existe o no está activo.")
    expect(R, L("Receta/Agregar: artículo inexistente"), add(99999999, "1"), status=200, error="El artículo seleccionado no existe o no está activo.")
    if inactive_art:
        expect(R, L("Receta/Agregar: artículo dado de baja"), add(inactive_art, "1"), status=200, error="El artículo seleccionado no existe o no está activo.")
    for qty, desc in (("0", "0"), ("-1", "-1"), ("0.001", "0.001"), ("0.009", "0.009"), ("999999.01", "999999.01"), ("1000000", "1000000")):
        expect(R, L(f"Receta/Agregar: cantidad {desc}"), add(a1, qty), status=200, error="La cantidad debe ser mayor a 0")
    r = add(a1, "abc")
    check(R, L("Receta/Agregar: cantidad 'abc' se rechaza sin 500"), r.status == 200 and bool(r.errors), f"status={r.status} errores={r.errors[:2]}")
    check(R, L("Receta: ninguna cantidad inválida creó líneas"), len(req_rows(pid)) == 0)
    r = add(a1, "")
    check(R, L("Receta/Agregar: cantidad vacía no se acepta en silencio como 1 [sospecha]"), not (r.status == 200 and r.has_toast("agregado", "success")) and len(req_rows(pid)) == 0,
          f"toasts={r.toasts[:1]} errores={r.errors[:2]} filas={req_rows(pid)}")
    for x in req_rows(pid):   # limpieza si lo anterior creó una línea
        rp("RemoveRequirement", [], "limpieza", f"&requirementId={x[0]}")
    r = add(a1, "2", sc="true", obs="  una   nota  ", vs="Validado")
    expect(R, L("Receta/Agregar: válida (cantidad 2, escalable, observación)"), r, status=200, toast="Artículo agregado a la receta de la práctica.", toast_kind="success")
    rows = req_rows(pid)
    check(R, L("Receta: línea en BD (cantidad 2, escalable, nota limpia, estado de validación)"),
          len(rows) == 1 and rows[0][1] == a1 and float(rows[0][2]) == 2 and rows[0][3] in (1, True) and rows[0][4] == "una nota" and rows[0][6] == "Validado", str(rows))
    expect(R, L("Receta/Agregar: artículo repetido"), add(a1, "1"), status=200, error="El artículo ya está en la receta de esta práctica.")
    check(R, L("Receta: el repetido no creó otra línea"), len(req_rows(pid)) == 1)
    r = add(a2, "1,5")
    expect(R, L("Receta/Agregar: '1,5' con coma decimal"), r, status=200, toast="Artículo agregado a la receta de la práctica.", toast_kind="success")
    check(R, L("Receta: '1,5' se guardó como 1.5"), any(x[1] == a2 and float(x[2]) == 1.5 for x in req_rows(pid)), str(req_rows(pid)))
    r = add(a3, "1.234")
    check(R, L("Receta/Agregar: 3 decimales (1.234) se rechaza como en el Kit [sospecha de incoherencia]"), not (r.status == 200 and r.has_toast("agregado", "success")),
          f"toasts={r.toasts[:1]} errores={r.errors[:1]} BD={[x[2] for x in req_rows(pid) if x[1] == a3]}")
    r = add(a4, "999999")
    expect(R, L("Receta/Agregar: cantidad 999999 (máximo)"), r, status=200, toast_kind="success")
    r = rp("AddRequirement", [("RequirementInput.ArticleId", a3), ("RequirementInput.Quantity", "1"), ("RequirementInput.Observations", "o" * 1001), WA], "obs 1001")
    check(R, L("Receta/Agregar: observaciones 1001 se rechaza en español"), r.status == 200 and bool(r.errors) and not en(r.errors), f"errores={r.errors[:2]}")
    r = rp("AddRequirement", [("RequirementInput.ArticleId", a3), ("RequirementInput.Quantity", "1"), ("RequirementInput.ValidationStatus", "v" * 41), WA], "validación 41")
    check(R, L("Receta/Agregar: estado de validación 41 caracteres se rechaza en español"), r.status == 200 and bool(r.errors) and not en(r.errors), f"errores={r.errors[:2]}")
    r = rp("AddRequirement", [("RequirementInput.ArticleId", a3), ("RequirementInput.Quantity", "1"), ("RequirementInput.Observations", "<script>x</script>"), WA], "obs html")
    _, page = c.get(durl)
    check(R, L("Details: observaciones con <script> se muestran escapadas"), "<script>x</script>" not in page)

    rows = req_rows(pid)
    check(R, L("Receta: la línea del artículo 1 existe para seguir probando"), any(x[1] == a1 for x in rows), str(rows))
    line1 = ([x for x in rows if x[1] == a1] or [(0,)])[0][0]

    def upd(rid, qty, sc="false", obs=""):
        return rp("UpdateRequirement", [("RequirementUpdate.Id", rid), ("RequirementUpdate.Quantity", qty), ("RequirementUpdate.IsScalable", sc), ("RequirementUpdate.Observations", obs)], f"upd {rid} {qty}")

    r = upd(line1, "3.5", "true", "  nueva   obs ")
    expect(R, L("Receta/Editar: cantidad, escalable y observación"), r, status=200, toast="Línea de receta actualizada correctamente.", toast_kind="success")
    cur = [x for x in req_rows(pid) if x[0] == line1][0]
    check(R, L("Receta/Editar: BD 3.5, escalable, observación limpia"), float(cur[2]) == 3.5 and cur[3] in (1, True) and cur[4] == "nueva obs", str(cur))
    for qty in ("0", "-1", "0.001", "999999.01", "1000000"):
        expect(R, L(f"Receta/Editar: cantidad {qty}"), upd(line1, qty), status=200, error="La cantidad debe ser mayor a 0")
    r = upd(line1, "abc")
    check(R, L("Receta/Editar: cantidad 'abc' se rechaza sin 500"), r.status == 200 and bool(r.errors), f"status={r.status} errores={r.errors[:2]}")
    r = upd(line1, "", "false", "")
    cur = [x for x in req_rows(pid) if x[0] == line1][0]
    check(R, L("Receta/Editar: cantidad vacía no pone la cantidad en 0 ni rompe [sospecha]"), r.status in (200, 302) and float(cur[2]) > 0, f"status={r.status} cant={cur[2]} errores={r.errors[:1]}")
    r = upd(line1, "1.234")
    cur = [x for x in req_rows(pid) if x[0] == line1][0]
    check(R, L("Receta/Editar: 3 decimales (1.234) se rechaza como en el Kit [sospecha de incoherencia]"), float(cur[2]) in (1.23, 1.0, 3.5, 2.0) and not r.has_toast("actualizada", "success"),
          f"toasts={r.toasts[:1]} BD={cur[2]}")
    r = upd(line1, "2", "false", "o" * 1001)
    check(R, L("Receta/Editar: observaciones 1001 se rechaza en español"), r.status == 200 and bool(r.errors) and not en(r.errors), f"errores={r.errors[:2]}")
    r = upd(99999999, "2")
    expect(R, L("Receta/Editar: línea inexistente"), r, status=302, toast="La línea de receta seleccionada no pertenece a esta práctica o ya fue retirada.", toast_kind="error")
    foreign = scalar("select top 1 Id from PracticeRequirements where PracticeId<>? and IsRemoved=0 order by Id", pid)
    if foreign:
        before = scalar("select Quantity from PracticeRequirements where Id=?", foreign)
        r = upd(foreign, "9")
        expect(R, L("Receta/Editar: línea de OTRA práctica (IDOR)"), r, status=302, toast="La línea de receta seleccionada no pertenece a esta práctica o ya fue retirada.", toast_kind="error")
        check(R, L("Receta/Editar: la línea ajena no cambió"), scalar("select Quantity from PracticeRequirements where Id=?", foreign) == before)

    r = rp("RemoveRequirement", [], "retirar", f"&requirementId={line1}")
    expect(R, L("Receta/Retirar: línea propia"), r, status=302, toast="Línea de receta retirada correctamente.", toast_kind="success")
    check(R, L("Receta/Retirar: IsRemoved=1 en BD (baja lógica)"), line1 in [x[0] for x in req_rows(pid, removed=True)])
    r = rp("RemoveRequirement", [], "retirar otra vez", f"&requirementId={line1}")
    expect(R, L("Receta/Retirar: línea ya retirada"), r, status=302, toast="La línea de receta ya estaba retirada.", toast_kind="warning")
    r = upd(line1, "2")
    expect(R, L("Receta/Editar: línea retirada"), r, status=302, toast="La línea de receta seleccionada no pertenece a esta práctica o ya fue retirada.", toast_kind="error")
    if foreign:
        r = rp("RemoveRequirement", [], "retirar ajena", f"&requirementId={foreign}")
        expect(R, L("Receta/Retirar: línea de OTRA práctica (IDOR)"), r, status=302, toast="La línea de receta seleccionada no pertenece a esta práctica.", toast_kind="error")
        check(R, L("Receta/Retirar: la línea ajena sigue activa"), scalar("select IsRemoved from PracticeRequirements where Id=?", foreign) in (0, False))
    r = rp("RemoveRequirement", [], "retirar inexistente", "&requirementId=99999999")
    expect(R, L("Receta/Retirar: línea inexistente"), r, status=302, toast="La línea de receta seleccionada no pertenece a esta práctica.", toast_kind="error")
    r = add(a1, "1")
    expect(R, L("Receta/Agregar: volver a agregar un artículo retirado"), r, status=200, toast="Artículo agregado a la receta de la práctica.", toast_kind="success")

    # ---------------------------------------------------------------- Delete
    r = c.submit(f"{B}/Delete/{pid}", {})
    expect(R, L("Delete: baja lógica"), r, status=302, redirect=B, toast_kind="success", toast=f"La Práctica '{row[2]}' ha sido dada de baja del catálogo institucional.")
    check(R, L("Delete: Status=Eliminado en BD (fila y receta conservadas)"), prac(pid)[4] == 2 and len(req_rows(pid)) > 0, str(prac(pid)))
    _, page = c.get(f"{B}?{urllib.parse.urlencode({'SearchTerm': nm2})}")
    check(R, L("Index: la práctica dada de baja desaparece del listado"), f"{B}/Details/{pid}" not in page)
    if prac(pid)[4] != 2:   # la baja por Delete no persistió: se fuerza por Edit (Estado=2) para poder probar el resto de reglas de una práctica dada de baja
        c.submit(f"{B}/Edit/{pid}", {"Input.Name": nm2, "Input.Status": "2"})
        print(f"[{LABEL}] AVISO: Delete no persistió la baja; se forzó Status=2 por Edit para las comprobaciones siguientes")
    check_404(R, c, L("práctica dada de baja"), f"{B}/Details/{pid}")
    check_404(R, c, L("práctica dada de baja Edit"), f"{B}/Edit/{pid}")
    check_404(R, c, L("práctica dada de baja Delete"), f"{B}/Delete/{pid}")
    post_404(R, c, L("Details AddRequirement práctica dada de baja"), f"{B}/Details/{pid}?handler=AddRequirement",
             [("RequirementInput.ArticleId", a1), ("RequirementInput.Quantity", "1")], token_from=f"{B}/Create")
    r = c.raw_post(f"{B}/Delete/{pid}", [], token_from=f"{B}/Create", name="segunda baja")
    check(R, L("Delete: segunda baja por POST no da 500"), r.status in (302, 404), f"status={r.status} toasts={r.toasts[:1]}")

    # ---------------------------------------------------------------- limpieza
    for rid in (pid_dup, long_id, html_id, acc_id, messy_id, *[x[0] for x in sql("select Id from Practices where Name like ? and Status<>2", nm + "%")]):
        if rid:
            c.raw_post(f"{B}/Delete/{rid}", [], token_from=f"{B}/Create", name="limpieza")
    for x in sql("select Id, Name from Practices where Id>? and Status<>2", max_prac0):   # Delete no persiste: se da de baja por Edit (Estado=2)
        c.submit(f"{B}/Edit/{x[0]}", {"Input.Name": x[1], "Input.Status": "2", "Input.SubjectId": str(sid),
                                           "Input.Description": "", "Input.Observations": ""})
    for sj in (sid, sid2):
        c.submit(f"/Catalogs/Subjects/Delete/{sj}", {})
    left = scalar("select count(*) from Practices where Id>? and Status<>2", max_prac0)
    check(R, L("Limpieza: no quedan prácticas QAC sin dar de baja"), left == 0, f"quedan={left}")


if __name__ == "__main__":
    run_module("practices", run)
