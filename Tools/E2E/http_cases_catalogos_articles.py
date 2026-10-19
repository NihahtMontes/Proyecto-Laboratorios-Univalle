"""
Catálogos / Artículos (Create, Edit, Delete): reglas del servidor por HTTP.
Solo contra la copia QA. Nombres con prefijo 'QAC'. Crea su propia categoría/unidad/materia/práctica QAC cuando las necesita.
"""
import re
import urllib.parse

from http_cases_catalogos_common import (check, check_404, check_anon, expect, post_404, run_module, scalar, sql, tag, tokenless, uniq)

B = "/Catalogs/Articles"
LABEL = "Articles"


def L(s):
    return f"{LABEL}/{s}"


def art(aid):
    r = sql("select Id, Name, Code, Status, ReviewStatus, ArticleCategoryId, MeasurementUnitId, EquipmentId, Category, UnitOfMeasure from Articles where Id=?", aid)
    return tuple(r[0]) if r else None


def aid_by_name(name, status=None):
    if status is None:
        return scalar("select top 1 Id from Articles where Name=? and Status<>2 order by Id desc", name)
    return scalar("select top 1 Id from Articles where Name=? and Status=? order by Id desc", name, status)


def run(R, c):
    for p in (B, f"{B}/Create", f"{B}/Edit/1", f"{B}/Delete/1"):
        check_anon(R, LABEL, p)
    check_anon(R, LABEL, f"{B}/Create", post=True)
    check_anon(R, LABEL, f"{B}/Delete/1", post=True)

    cat = scalar("select top 1 Id from ArticleCategories where Status=0 order by Id")
    cat_name = scalar("select Name from ArticleCategories where Id=?", cat)
    unit = scalar("select top 1 Id from MeasurementUnits where Status=0 order by Id")
    unit_code = scalar("select Code from MeasurementUnits where Id=?", unit)
    cat2 = scalar("select top 1 Id from ArticleCategories where Status=0 and Id<>? order by Id", cat)
    unit2 = scalar("select top 1 Id from MeasurementUnits where Status=0 and Id<>? order by Id", unit)
    equip = scalar("select top 1 Id from Equipments where Status=0 order by Id")

    # categoría y unidad QAC inactivas (para probar 'no disponible')
    cn = uniq("QAC Cat art")
    c.submit("/Catalogs/ArticleCategories/Create", {"Input.Name": cn})
    cat_off = scalar("select top 1 Id from ArticleCategories where Name=? order by Id desc", cn)
    c.submit(f"/Catalogs/ArticleCategories/Edit/{cat_off}", {"Input.Status": "1"})
    un = uniq("QAC Uni art")
    c.submit("/Catalogs/MeasurementUnits/Create", {"Input.Name": un})
    unit_off = scalar("select top 1 Id from MeasurementUnits where Name=? order by Id desc", un)
    c.submit(f"/Catalogs/MeasurementUnits/Edit/{unit_off}", {"Input.Status": "1"})
    check(R, L("Preparación: categoría y unidad QAC inactivas"), scalar("select Status from ArticleCategories where Id=?", cat_off) == 1 and scalar("select Status from MeasurementUnits where Id=?", unit_off) == 1)

    nm = uniq("QAC Art")
    base = {"Input.Name": nm, "Input.ArticleCategoryId": str(cat), "Input.MeasurementUnitId": str(unit)}

    def ov(**k):
        return {**base, **k}

    # ---------------------------------------------------------------- Create: validaciones
    expect(R, L("Create: nombre vacío"), c.submit(f"{B}/Create", ov(**{"Input.Name": ""})), status=200, error="El nombre del artículo es obligatorio")
    expect(R, L("Create: nombre solo espacios"), c.submit(f"{B}/Create", ov(**{"Input.Name": "   "})), status=200, error="El nombre del artículo es obligatorio")
    expect(R, L("Create: 201 caracteres"), c.submit(f"{B}/Create", ov(**{"Input.Name": "x" * 201})), status=200, error="no puede superar los 200")
    expect(R, L("Create: categoría vacía"), c.submit(f"{B}/Create", ov(**{"Input.ArticleCategoryId": ""})), status=200, error="Seleccione la categoría")
    expect(R, L("Create: unidad vacía"), c.submit(f"{B}/Create", ov(**{"Input.MeasurementUnitId": ""})), status=200, error="Seleccione la unidad de medida")
    expect(R, L("Create: categoría inexistente"), c.submit(f"{B}/Create", ov(**{"Input.ArticleCategoryId": "99999"})), status=200, error="La categoría seleccionada no está disponible.")
    expect(R, L("Create: categoría=0"), c.submit(f"{B}/Create", ov(**{"Input.ArticleCategoryId": "0"})), status=200, error="La categoría seleccionada no está disponible.")
    expect(R, L("Create: categoría inactiva"), c.submit(f"{B}/Create", ov(**{"Input.ArticleCategoryId": str(cat_off)})), status=200, error="La categoría seleccionada no está disponible.")
    expect(R, L("Create: unidad inexistente"), c.submit(f"{B}/Create", ov(**{"Input.MeasurementUnitId": "99999"})), status=200, error="La unidad de medida seleccionada no está disponible.")
    expect(R, L("Create: unidad inactiva"), c.submit(f"{B}/Create", ov(**{"Input.MeasurementUnitId": str(unit_off)})), status=200, error="La unidad de medida seleccionada no está disponible.")
    expect(R, L("Create: equipo inexistente"), c.submit(f"{B}/Create", ov(**{"Input.EquipmentId": "99999"})), status=200, error="El catálogo de equipo no existe.")
    r = c.submit(f"{B}/Create", ov(**{"Input.ArticleCategoryId": "abc"}))
    check(R, L("Create: categoría 'abc' se rechaza sin 500"), r.status == 200 and bool(r.errors), f"status={r.status} errores={r.errors[:2]}")
    check(R, L("Create: ninguna validación fallida creó filas"), scalar("select count(*) from Articles where Name=?", nm) == 0)

    # ---------------------------------------------------------------- Create: válidos
    r = c.submit(f"{B}/Create", ov())
    expect(R, L("Create: válido"), r, status=302, redirect=B, toast_kind="success", toast="registrado exitosamente.")
    aid = aid_by_name(nm)
    row = art(aid) if aid else None
    check(R, L("Create: fila en BD (código, Activo, Confirmado, FKs, columnas legadas sincronizadas)"),
          row and row[2] and row[3] == 0 and row[4] == 0 and row[5] == cat and row[6] == unit and row[8] == cat_name and row[9] == unit_code, str(row))
    check(R, L("Create: el toast cita el código «Artículo 'CÓDIGO' registrado exitosamente.»"), bool(r.toasts) and row and r.toasts[0][1] == f"Artículo '{row[2]}' registrado exitosamente.", str(r.toasts[:1]))
    expect(R, L("Create: duplicado (otra capitalización y espacios)"), c.submit(f"{B}/Create", ov(**{"Input.Name": f"  {nm.upper()}  "})), status=200,
           error="Ya existe un artículo con este nombre en el sistema.")
    expect(R, L("Create: duplicado con espacios internos dobles"), c.submit(f"{B}/Create", ov(**{"Input.Name": nm.replace(' ', '   ')})), status=200, error="ya existe")
    check(R, L("Create: tras los duplicados queda una sola fila"), scalar("select count(*) from Articles where Name=? and Status<>2", nm) == 1)
    expect(R, L("Create: mismo nombre con otra categoría/unidad sigue siendo duplicado"),
           c.submit(f"{B}/Create", ov(**{"Input.ArticleCategoryId": str(cat2), "Input.MeasurementUnitId": str(unit2)})), status=200, error="Ya existe un artículo con este nombre")
    exact = ("QAC Art " + tag() + " " + "z" * 200)[:200]
    expect(R, L("Create: 200 caracteres exactos (válido)"), c.submit(f"{B}/Create", ov(**{"Input.Name": exact})), status=302, redirect=B)
    exact_id = aid_by_name(exact)
    r = c.submit(f"{B}/Create", ov(**{"Input.Name": nm + " eq", "Input.EquipmentId": str(equip)}))
    expect(R, L("Create: vinculado a un equipo del catálogo"), r, status=302, redirect=B)
    eq_id = aid_by_name(nm + " eq")
    check(R, L("Create: EquipmentId guardado"), eq_id and art(eq_id)[7] == equip, str(art(eq_id) if eq_id else None))
    messy = f"  QAC   Art {tag()}   x  "
    expect(R, L("Create: espacios sobrantes"), c.submit(f"{B}/Create", ov(**{"Input.Name": messy})), status=302, redirect=B)
    messy_id = aid_by_name(" ".join(messy.split()))
    check(R, L("Create: nombre sin espacios sobrantes en BD"), messy_id is not None, repr(" ".join(messy.split())))
    html_name = f"<b>QAC Art</b> {tag()}"
    expect(R, L("Create: nombre con HTML se acepta sin excepción"), c.submit(f"{B}/Create", ov(**{"Input.Name": html_name})), status=302, redirect=B)
    html_id = aid_by_name(html_name)
    if html_id:
        _, page = c.get(f"{B}?{urllib.parse.urlencode({'SearchTerm': html_name.split('</b> ')[1]})}")
        check(R, L("Index: el HTML hostil se muestra escapado"), html_name not in page and "&lt;b&gt;" in page)
    acc = f"{tag()} Pañuelo ñandú áéíóú «QAC» \"comillas\" 'simples'"
    expect(R, L("Create: tildes, ñ, comillas"), c.submit(f"{B}/Create", ov(**{"Input.Name": acc})), status=302, redirect=B)
    acc_id = aid_by_name(acc)
    check(R, L("Create: nombre con tildes/ñ/comillas idéntico en BD"), acc_id is not None, repr(acc))

    # ---------------------------------------------------------------- Index
    for qs, desc, expect_id in (({"SearchTerm": nm}, "nombre", aid), ({"SearchTerm": nm.upper()}, "nombre en mayúsculas", aid),
                                ({"SearchTerm": row[2]}, "código", aid), ({"SearchTerm": nm, "ArticleCategoryId": cat}, "categoría correcta", aid),
                                ({"SearchTerm": nm, "ReviewStatusFilter": 0}, "revisión Confirmado", aid), ({"SearchTerm": nm, "StatusFilter": 0}, "estado Activo", aid)):
        _, page = c.get(f"{B}?{urllib.parse.urlencode(qs)}")
        check(R, L(f"Index: filtro {desc} muestra el artículo"), f"{B}/Edit/{expect_id}" in page)
    for qs, desc in (({"SearchTerm": nm, "ArticleCategoryId": cat2}, "otra categoría"), ({"SearchTerm": nm, "ReviewStatusFilter": 9}, "revisión Por confirmar"),
                     ({"SearchTerm": nm, "StatusFilter": 1}, "estado Inactivo")):
        _, page = c.get(f"{B}?{urllib.parse.urlencode(qs)}")
        check(R, L(f"Index: filtro {desc} NO muestra el artículo"), f"{B}/Edit/{aid}" not in page)
    for q in ({"ArticleCategoryId": "abc"}, {"ReviewStatusFilter": "5"}, {"StatusFilter": "9"}, {"pageIndex": "99999"}, {"pageIndex": "-1"}, {"SearchTerm": "%'<>"}):
        st, _ = c.get(f"{B}?{urllib.parse.urlencode(q)}")
        check(R, L(f"Index: {q} responde 200"), st == 200, f"status={st}")

    # ---------------------------------------------------------------- Edit
    action, fields = c.form(f"{B}/Edit/{aid}")
    print(f"[{LABEL}] campos Edit:", [(k, v[:12]) for k, v in fields if k != "__RequestVerificationToken"])
    nm2 = uniq("QAC Art ed")
    r = c.submit(f"{B}/Edit/{aid}", {"Input.Name": nm2})
    expect(R, L("Edit: renombrar (válido)"), r, status=302, redirect=B, toast_kind="success", toast=f"Artículo '{nm2}' actualizado correctamente.")
    check(R, L("Edit: BD nombre nuevo, código intacto"), art(aid)[1] == nm2 and art(aid)[2] == row[2], str(art(aid)))
    expect(R, L("Edit: guardar sin cambios"), c.submit(f"{B}/Edit/{aid}", {"Input.Name": nm2}), status=302, redirect=B)
    expect(R, L("Edit: solo cambia capitalización de su propio nombre"), c.submit(f"{B}/Edit/{aid}", {"Input.Name": nm2.upper()}), status=302, redirect=B)
    expect(R, L("Edit: nombre vacío"), c.submit(f"{B}/Edit/{aid}", {"Input.Name": ""}), status=200, error="El nombre del artículo es obligatorio")
    expect(R, L("Edit: 201 caracteres"), c.submit(f"{B}/Edit/{aid}", {"Input.Name": "x" * 201}), status=200, error="no puede superar los 200")
    expect(R, L("Edit: categoría vacía"), c.submit(f"{B}/Edit/{aid}", {"Input.Name": nm2, "Input.ArticleCategoryId": ""}), status=200, error="Seleccione la categoría")
    expect(R, L("Edit: unidad vacía"), c.submit(f"{B}/Edit/{aid}", {"Input.Name": nm2, "Input.MeasurementUnitId": ""}), status=200, error="Seleccione la unidad de medida")
    expect(R, L("Edit: categoría inactiva"), c.submit(f"{B}/Edit/{aid}", {"Input.Name": nm2, "Input.ArticleCategoryId": str(cat_off)}), status=200, error="La categoría seleccionada no está disponible.")
    expect(R, L("Edit: unidad inactiva"), c.submit(f"{B}/Edit/{aid}", {"Input.Name": nm2, "Input.MeasurementUnitId": str(unit_off)}), status=200, error="La unidad de medida seleccionada no está disponible.")
    expect(R, L("Edit: equipo inexistente"), c.submit(f"{B}/Edit/{aid}", {"Input.Name": nm2, "Input.EquipmentId": "99999"}), status=200, error="El catálogo de equipo no existe.")
    messy_name = art(messy_id)[1]
    expect(R, L("Edit: nombre de otro artículo (otra capitalización y espacios)"),
           c.submit(f"{B}/Edit/{aid}", {"Input.Name": f" {messy_name.upper()} "}), status=200, error="Ya existe otro artículo con este nombre en el sistema.")
    expect(R, L("Edit: cambiar categoría y unidad (válido)"), c.submit(f"{B}/Edit/{aid}", {"Input.Name": nm2, "Input.ArticleCategoryId": str(cat2), "Input.MeasurementUnitId": str(unit2)}),
           status=302, redirect=B)
    cn2 = scalar("select Name from ArticleCategories where Id=?", cat2)
    uc2 = scalar("select Code from MeasurementUnits where Id=?", unit2)
    check(R, L("Edit: FKs y columnas legadas (Category, UnitOfMeasure) se sincronizan"), art(aid)[5] == cat2 and art(aid)[6] == unit2 and art(aid)[8] == cn2 and art(aid)[9] == uc2, str(art(aid)))
    expect(R, L("Edit: vincular a equipo"), c.submit(f"{B}/Edit/{aid}", {"Input.Name": nm2, "Input.EquipmentId": str(equip)}), status=302, redirect=B)
    expect(R, L("Edit: desvincular el equipo (campo vacío)"), c.submit(f"{B}/Edit/{aid}", {"Input.Name": nm2, "Input.EquipmentId": ""}), status=302, redirect=B)
    check(R, L("Edit: EquipmentId vuelve a NULL"), art(aid)[7] is None, str(art(aid)))
    # revisión
    r = c.submit(f"{B}/Edit/{aid}", {"Input.Name": nm2, "Input.ReviewStatus": "9"})
    expect(R, L("Edit: Confirmado -> 'Por confirmar' no permitido"), r, status=200, error="El estado de revisión enviado no es válido para el estado actual del artículo.")
    check(R, L("Edit: la revisión sigue Confirmada"), art(aid)[4] == 0)
    for bad in ("5", "abc", ""):
        r = c.submit(f"{B}/Edit/{aid}", {"Input.Name": nm2, "Input.ReviewStatus": bad})
        check(R, L(f"Edit: ReviewStatus={bad!r} se rechaza o se ignora sin 500"), r.status in (200, 302) and art(aid)[4] == 0, f"status={r.status} errores={r.errors[:2]} revisión BD={art(aid)[4]}")
    expect(R, L("Edit: Inactivo"), c.submit(f"{B}/Edit/{aid}", {"Input.Name": nm2, "Input.Status": "1"}), status=302, redirect=B)
    check(R, L("Edit: Inactivo en BD"), art(aid)[3] == 1)
    r = c.submit(f"{B}/Edit/{aid}", {"Input.Name": nm2, "Input.Status": "0"})
    expect(R, L("Edit: volver a Activo"), r, status=302, redirect=B)
    r = c.submit(f"{B}/Edit/{aid}", {"Input.Name": nm2, "Input.Status": "7"})
    check(R, L("Edit: Estado=7 (fuera del enum) no cambia la BD ni da 500"), r.status == 200 and art(aid)[3] == 0, f"status={r.status}")
    check(R, L("Edit: Estado=7 muestra un mensaje de error visible"), bool(r.errors), f"errores={r.errors[:2]}")
    sacrificial = aid_by_name(nm + " eq")
    r = c.submit(f"{B}/Edit/{sacrificial}", {"Input.Name": nm + " eq", "Input.Status": "2"})
    check(R, L("Edit: Estado=Eliminado forzado por POST se rechaza [hallazgo extra]"), r.status == 200 and art(sacrificial) is not None and art(sacrificial)[3] != 2,
          f"status={r.status} Status BD={art(sacrificial)[3] if art(sacrificial) else None} (Categorías/Unidades sí lo rechazan)")
    for bad in ("99999", "0", "-1", "abc"):
        check_404(R, c, LABEL, f"{B}/Edit/{bad}")
        check_404(R, c, LABEL, f"{B}/Delete/{bad}")
    post_404(R, c, L("Edit POST id inexistente"), f"{B}/Edit/99999", [("Input.Id", "99999"), ("Input.Name", "QAC fantasma"), ("Input.ArticleCategoryId", cat), ("Input.MeasurementUnitId", unit), ("Input.Status", "0"), ("Input.ReviewStatus", "0")], token_from=f"{B}/Create")
    post_404(R, c, L("Delete POST id inexistente"), f"{B}/Delete/99999", [], token_from=f"{B}/Create")
    tokenless(R, c, L("Create"), f"{B}/Create", [("Input.Name", "QAC sintoken"), ("Input.ArticleCategoryId", cat), ("Input.MeasurementUnitId", unit)])
    tokenless(R, c, L("Edit"), f"{B}/Edit/{aid}", [("Input.Id", aid), ("Input.Name", "QAC sintoken"), ("Input.ArticleCategoryId", cat), ("Input.MeasurementUnitId", unit), ("Input.Status", "0"), ("Input.ReviewStatus", "0")])
    tokenless(R, c, L("Delete"), f"{B}/Delete/{aid}", [])
    check(R, L("POST sin token no modificó nada"), art(aid)[1] == nm2 and art(aid)[3] == 0)

    # ---------------------------------------------------------------- Delete: no valida stock, kits ni recetas (plan); soft-delete
    dep = uniq("QAC Art dep")
    c.submit(f"{B}/Create", ov(**{"Input.Name": dep}))
    dep_id = aid_by_name(dep)
    r = c.submit(f"/Catalogs/Stock/Edit/1/{dep_id}", {"Input.AvailableQuantity": "5"})
    check(R, L("Preparación: existencia en el laboratorio 1 para el artículo"), r.status == 302 and scalar("select count(*) from ArticleStocks where ArticleId=?", dep_id) == 1, f"status={r.status} errores={r.errors[:2]}")
    sj = uniq("QAC Mat art")
    c.submit("/Catalogs/Subjects/Create", {"Input.Name": sj, "Input.CareerId": "1", "Input.Semester": "1"})
    sj_id = scalar("select top 1 Id from Subjects where Name=? order by Id desc", sj)
    c.raw_post(f"/Catalogs/Subjects/Kit/{sj_id}?handler=AddBatch", [("Batch.Articles[0].Id", dep_id), ("Batch.Articles[0].Quantity", "2")], token_from=f"/Catalogs/Subjects/Kit/{sj_id}")
    pn = uniq("QAC Prac art")
    c.raw_post("/Catalogs/Practices/Create", [("Input.Name", pn), ("Input.SubjectId", sj_id)], token_from="/Catalogs/Practices/Create")
    pr_id = scalar("select top 1 Id from Practices where Name=? order by Id desc", pn)
    c.raw_post(f"/Catalogs/Practices/Details/{pr_id}?handler=AddRequirement", [("RequirementInput.ArticleId", dep_id), ("RequirementInput.Quantity", "1"), ("RequirementUpdate.Quantity", "1")],
               token_from=f"/Catalogs/Practices/Details/{pr_id}")
    check(R, L("Preparación: el artículo está en un kit y en una receta"),
          scalar("select count(*) from SubjectKitItems where ArticleId=? and IsRemoved=0", dep_id) == 1 and scalar("select count(*) from PracticeRequirements where ArticleId=? and IsRemoved=0", dep_id) == 1)

    r = c.submit(f"{B}/Delete/{dep_id}", {})
    expect(R, L("Delete: baja de un artículo con stock, kit y receta (el plan dice que no se valida)"), r, status=302, redirect=B, toast_kind="success",
           toast=f"El artículo '{dep}' ha sido dado de baja del catálogo.")
    check(R, L("Delete: Status=Eliminado en BD y las filas dependientes se conservan"),
          art(dep_id)[3] == 2 and scalar("select count(*) from ArticleStocks where ArticleId=?", dep_id) == 1, str(art(dep_id)))
    _, page = c.get(f"{B}?{urllib.parse.urlencode({'SearchTerm': dep})}")
    check(R, L("Index: el artículo dado de baja desaparece del listado"), f"{B}/Edit/{dep_id}" not in page)
    check_404(R, c, L("artículo dado de baja"), f"{B}/Edit/{dep_id}")
    check_404(R, c, L("artículo dado de baja"), f"{B}/Delete/{dep_id}")
    r = c.raw_post(f"{B}/Delete/{dep_id}", [], token_from=f"{B}/Create", name="segunda baja")
    check(R, L("Delete: segunda baja por POST -> 404 (el filtro global oculta el eliminado)"), r.status == 404, f"status={r.status}")
    # el kit y la receta que usan un artículo dado de baja: la pantalla de kit no debe romperse
    st, _ = c.get(f"/Catalogs/Subjects/Kit/{sj_id}")
    check(R, L("Kit con un artículo dado de baja responde 200 (sin 500)"), st == 200, f"status={st}")
    st, _ = c.get(f"/Catalogs/Practices/Details/{pr_id}")
    check(R, L("Receta con un artículo dado de baja responde 200 (sin 500)"), st == 200, f"status={st}")
    # recrear un nombre dado de baja
    r = c.submit(f"{B}/Create", ov(**{"Input.Name": dep}))
    expect(R, L("Create: recrear un nombre dado de baja (índice único filtrado [Status]<>2: debe permitirse)"), r, status=302, redirect=B, toast_kind="success")
    dep2 = aid_by_name(dep)
    check(R, L("Create: el artículo recreado es una fila nueva con otro código"), dep2 and dep2 != dep_id and art(dep2)[2] != art(dep_id)[2], f"{dep_id}->{dep2}")
    # baja con estado Inactivo antes de eliminar y re-baja idempotente ya cubiertos arriba; baja simple del resto
    r = c.submit(f"{B}/Delete/{aid}", {})
    expect(R, L("Delete: baja lógica de un artículo simple"), r, status=302, redirect=B, toast_kind="success")

    # ---------------------------------------------------------------- limpieza
    for rid in (exact_id, eq_id, messy_id, html_id, acc_id, dep2, sacrificial):
        if rid and art(rid) and art(rid)[3] != 2:
            c.submit(f"{B}/Delete/{rid}", {})
    c.raw_post(f"/Catalogs/Practices/Delete/{pr_id}", [], token_from=f"{B}/Create")
    c.submit(f"/Catalogs/Subjects/Delete/{sj_id}", {})
    c.submit(f"/Catalogs/ArticleCategories/Delete/{cat_off}", {})
    c.submit(f"/Catalogs/MeasurementUnits/Delete/{unit_off}", {})
    left = scalar("select count(*) from Articles where Name like 'QAC%' and Status=0")
    check(R, L("Limpieza: no quedan artículos QAC activos"), left == 0, f"quedan={left}")


if __name__ == "__main__":
    run_module("articles", run)
