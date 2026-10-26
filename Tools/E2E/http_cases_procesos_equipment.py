"""Equipment (definiciones): Create / Edit / Delete / Details (incl. DeleteUnit, notas, imagen invalida, bajas logicas). Solo contra la copia QA."""
import os
import random

import e2e_lib
import http_cases_procesos_common as P
from http_cases_procesos_common import check, expect, sql, scalar, plan, unidad, post_form, post_raw

EQUIP = P.EQUIP
PNG_OK = b"\x89PNG\r\n\x1a\n" + b"\x00" * 32


def codigo(c=None):
    for _ in range(50):
        cod = "CAT-9" + "".join(random.choices("0123456789", k=5))
        if not scalar("select count(*) from Equipments where CatalogCode=?", cod):
            return cod
    raise RuntimeError("sin codigo libre")


def base(nombre, cod, **extra):
    ov = {"Input.Category": "0", "Input.TypeClassification": "0", "Input.CatalogCode": cod, "Input.Name": nombre, "Input.EvidenceReference": "Evidencia QAP"}
    ov.update(extra)
    return ov


def rechazo(R, label, r, nombre=None):
    """Rechazo del servidor: la pagina se vuelve a mostrar (200, sin redirect) y no se creo nada con ese nombre."""
    ok = r.status == 200 and not r.location
    if nombre is not None:
        ok = ok and scalar("select count(*) from Equipments where Name=?", nombre) == 0
    check(R, f"{label}: rechazado por el servidor (200, sin redirect, sin alta)", ok, P.dump(r))


def eq_id(cod):
    v = scalar("select top 1 Id from Equipments where CatalogCode=? order by Id desc", cod)
    return int(v) if v else None


def run(R, c):
    N = lambda s: f"QAP {s} {P.RUN}"
    lab = P.lab_propio(c, "inventario")

    # ------------------------------------------------------------ Create: validaciones del servidor
    expect(R, "Equipment/Create GET 200", type("X", (), {"status": c.get("/Equipment/Create")[0], "location": None, "errors": [], "toasts": []})(), status=200)
    r = post_form(c, "/Equipment/Create", {"Input.Category": None, "Input.TypeClassification": None, "Input.CatalogCode": None, "Input.Name": None, "Input.EvidenceReference": None}, name="vacio")
    expect(R, "Create: todo vacio -> categoria obligatoria", r, status=200, error="La categoría es obligatoria")
    check(R, "Create: todo vacio -> codigo obligatorio", r.has_error("El código de catálogo es obligatorio"), P.dump(r))
    check(R, "Create: todo vacio -> nombre obligatorio", r.has_error("El nombre del equipo es obligatorio"))
    check(R, "Create: todo vacio -> evidencia obligatoria", r.has_error("La evidencia de clasificación es obligatoria"))

    for cod, lbl in (("CAT-1", "CAT-1"), ("cat-0001", "minusculas"), ("CAT-ABCD", "letras"), ("XYZ-0001", "prefijo distinto"), (" CAT-9100001", "espacio inicial"), ("CAT-0001 ", "espacio final")):
        r = post_form(c, "/Equipment/Create", base(N("cod"), cod), name=f"codigo {lbl}")
        expect(R, f"Create: codigo '{lbl}' invalido", r, status=200, error="Use el formato CAT-0001.")
    r = post_form(c, "/Equipment/Create", base(N("cod31"), "CAT-" + "1" * 27), name="codigo 31")
    check(R, "Create: codigo de 31 caracteres rechazado con mensaje en espanol", r.status == 200 and bool(r.errors) and not P._EN.search(" ".join(r.errors)), P.dump(r))

    r = post_form(c, "/Equipment/Create", base("x" * 101, codigo()), name="nombre 101")
    expect(R, "Create: nombre de 101 caracteres", r, status=200, error="El nombre no puede superar los 100 caracteres")
    for campo, n in (("Input.Brand", 101), ("Input.Model", 101), ("Input.Description", 2001), ("Input.OtherClassificationDetail", 1001), ("Input.EvidenceReference", 1001)):
        r = post_form(c, "/Equipment/Create", base(N("lon"), codigo(), **{campo: "y" * n}), name=f"{campo} {n}")
        rechazo(R, f"Create: {campo} de {n} caracteres", r)
        ok = r.status == 200 and bool(r.errors) and not P._EN.search(" ".join(r.errors))
        check(R, f"Create: {campo} de {n} caracteres muestra un mensaje en espanol", ok, P.dump(r))
    for v in ("-1", "51"):
        r = post_form(c, "/Equipment/Create", base(N("vida"), codigo(), **{"Input.UsefulLifeYears": v}), name=f"vida {v}")
        expect(R, f"Create: vida util {v}", r, status=200, error="La vida útil debe estar entre 0 y 50 años")

    # combinaciones de categoria
    combos = [
        ("equipo sin subtipo", {"Input.Category": "0", "Input.TypeClassification": None}),
        ("equipo con tipo de utensilio", {"Input.Category": "0", "Input.TypeClassification": "0", "Input.UtensilType": "1"}),
        ("equipo subtipo Otro sin detalle", {"Input.Category": "0", "Input.TypeClassification": "7"}),
        ("utensilio sin tipo", {"Input.Category": "1", "Input.TypeClassification": None}),
        ("utensilio con subtipo de equipo", {"Input.Category": "1", "Input.TypeClassification": "0", "Input.UtensilType": "1"}),
        ("utensilio NoAplica (0)", {"Input.Category": "1", "Input.TypeClassification": None, "Input.UtensilType": "0"}),
        ("utensilio Otros sin detalle", {"Input.Category": "1", "Input.TypeClassification": None, "Input.UtensilType": "11"}),
        ("Otro sin detalle", {"Input.Category": "2", "Input.TypeClassification": None}),
        ("Otro con subtipo", {"Input.Category": "2", "Input.TypeClassification": "0", "Input.OtherClassificationDetail": "detalle"}),
    ]
    for lbl, ov in combos:
        r = post_form(c, "/Equipment/Create", base(N("comb"), codigo(), **ov), name=lbl)
        expect(R, f"Create: combinacion invalida ({lbl})", r, status=200, error="La combinación de categoría, subtipo y detalle de Otro no es válida.")
    r = post_form(c, "/Equipment/Create", base(N("cat"), codigo(), **{"Input.Category": "7", "Input.TypeClassification": None}), name="categoria 7")
    check(R, "Create: categoria inexistente (7) rechazada", r.status == 200 and bool(r.errors), P.dump(r))

    # duplicados
    r = post_form(c, "/Equipment/Create", base("Extractor de aire", codigo()), name="nombre duplicado")
    expect(R, "Create: nombre duplicado (existente 'Extractor de aire')", r, status=200, error="Ya existe un equipo registrado con este nombre y modelo.")
    r = post_form(c, "/Equipment/Create", base(N("codup"), "CAT-0001"), name="codigo duplicado")
    expect(R, "Create: codigo de catalogo duplicado (CAT-0001)", r, status=200, error="El código de catálogo ya existe.")

    # notas
    r = post_form(c, "/Equipment/Create", base(N("n51"), codigo()), name="51 notas", extra=[("Input.Notes", f"nota {i}") for i in range(51)])
    rechazo(R, "Create: 51 notas", r, N("n51"))
    expect(R, "Create: 51 notas muestra el mensaje", r, status=200, error="No se permiten más de 50 notas por equipo.")
    r = post_form(c, "/Equipment/Create", base(N("n501"), codigo()), name="nota 501", extra=[("Input.Notes", "z" * 501)])
    rechazo(R, "Create: nota de 501 caracteres", r, N("n501"))
    expect(R, "Create: nota de 501 caracteres muestra el mensaje", r, status=200, error="Cada nota puede tener como máximo 500 caracteres.")

    # pais / ciudad
    r = post_form(c, "/Equipment/Create", base(N("pais"), codigo(), **{"Input.CountryId": "99999"}), name="pais inexistente")
    expect(R, "Create: pais inexistente", r, status=200, error="El país seleccionado no está disponible.")
    r = post_form(c, "/Equipment/Create", base(N("ciudad"), codigo()), name="ciudad sin pais", extra=[("Input.CityId", "1")])
    rechazo(R, "Create: ciudad sin pais", r, N("ciudad"))
    expect(R, "Create: ciudad sin pais muestra el mensaje", r, status=200, error="Seleccione el país correspondiente a la ciudad.")
    pais = scalar("select top 1 Id from Countries where Status=0 order by Id")
    r = post_form(c, "/Equipment/Create", base(N("ciudad2"), codigo(), **{"Input.CountryId": str(pais)}), name="ciudad de otro pais", extra=[("Input.CityId", "99999")])
    rechazo(R, "Create: ciudad de otro pais", r, N("ciudad2"))
    expect(R, "Create: ciudad que no pertenece al pais muestra el mensaje", r, status=200, error="La ciudad no pertenece al país seleccionado.")

    # imagen invalida (multipart; solo archivos que el servidor rechaza, no se escribe nada en wwwroot)
    campos = [(k.replace("Input.", "Input."), v) for k, v in base(N("img"), codigo()).items()]
    for lbl, fn, content, ctype, esperado in (
        ("extension .txt", "malo.txt", b"hola", "text/plain", "La imagen debe ser JPG, PNG o WEBP."),
        ("extension .gif", "malo.gif", b"GIF89a", "image/gif", "La imagen debe ser JPG, PNG o WEBP."),
        ("png con content-type distinto", "malo.png", PNG_OK, "image/jpeg", "El tipo de contenido del archivo no coincide con una imagen permitida."),
        ("png con firma falsa", "malo.png", b"esto no es un png de verdad", "image/png", "El contenido del archivo no corresponde a una imagen válida."),
        ("archivo vacio", "vacio.png", b"", "image/png", "El archivo de imagen está vacío."),
        ("mayor de 5 MB", "grande.png", PNG_OK + b"\x00" * (5 * 1024 * 1024), "image/png", "La imagen no puede superar los 5 MB."),
        ("ejecutable renombrado .exe", "virus.exe", b"MZ\x90\x00", "application/octet-stream", "La imagen debe ser JPG, PNG o WEBP."),
    ):
        r = P.multipart_post(c, "/Equipment/Create", campos, [("Input.ImageUpload", fn, content, ctype)], token_page="/Equipment/Create", name=f"imagen {lbl}")
        expect(R, f"Create: imagen invalida ({lbl})", r, status=200, error=esperado)
    check(R, "Create: ninguna creacion invalida dejo un equipo QAP", scalar("select count(*) from Equipments where Name like ?", f"{N('img')}%") == 0)

    # ------------------------------------------------------------ Create: validos
    cod1 = codigo()
    nom1 = N("valido")
    r = post_form(c, "/Equipment/Create", base(nom1, cod1, **{"Input.Brand": "Marca QAP", "Input.Model": "M-1", "Input.UsefulLifeYears": "10", "Input.Description": "Descripcion QAP"}),
                  name="valido", extra=[("Input.Notes", "nota uno"), ("Input.Notes", "  "), ("Input.Notes", "nota dos")])
    eid1 = eq_id(cod1)
    expect(R, "Create: equipo valido con 3 notas (1 en blanco)", r, status=302, redirect=f"/Equipment/Details/{eid1}", toast=f"Definición de '{nom1}' registrada correctamente.", toast_kind="success")
    row = sql("select ClassificationReviewStatus,Status,Category,TypeClassification,Brand,Model,UsefulLifeYears from Equipments where Id=?", eid1)
    check(R, "Create valido: queda Confirmed, Activo, categoria Equipo, subtipo 0", bool(row) and tuple(row[0][:4]) == (2, 0, 0, 0), f"{row}")
    check(R, "Create valido: 2 notas guardadas (la nota en blanco se descarta)", scalar("select count(*) from EquipmentNotes where EquipmentId=?", eid1) == 2)
    check(R, "Create valido: decision de clasificacion auditable (Confirmed) registrada", scalar("select count(*) from EquipmentClassificationDecisions where EquipmentId=? and ReviewStatus=2", eid1) >= 1)
    st, page = c.get(f"/Equipment/Details/{eid1}")
    check(R, "Details: muestra el equipo y sus notas", st == 200 and nom1 in page and "nota uno" in page and "nota dos" in page, f"status={st}")

    # doble envio del mismo alta
    r = post_form(c, "/Equipment/Create", base(nom1, cod1, **{"Input.Model": "M-1"}), name="doble envio")
    check(R, "Create: reenvio del mismo alta rechazado (no duplica)", r.status == 200 and bool(r.errors) and scalar("select count(*) from Equipments where CatalogCode=?", cod1) == 1, P.dump(r))

    # utensilio, otro, equipo-otro, HTML hostil
    cod2 = codigo()
    r = post_form(c, "/Equipment/Create", base(N("uten"), cod2, **{"Input.Category": "1", "Input.TypeClassification": None, "Input.UtensilType": "1"}), name="utensilio")
    eid2 = eq_id(cod2)
    expect(R, "Create: utensilio (Vidrio) valido", r, status=302, redirect=f"/Equipment/Details/{eid2}")
    row = sql("select Category,UtensilType,TypeClassification from Equipments where Id=?", eid2)
    check(R, "Create utensilio: guarda tipo de utensilio y deja subtipo de equipo nulo", bool(row) and row[0][0] == 1 and row[0][1] == 1 and row[0][2] is None, f"{row}")
    cod3 = codigo()
    r = post_form(c, "/Equipment/Create", base(N("otro"), cod3, **{"Input.Category": "2", "Input.TypeClassification": None, "Input.OtherClassificationDetail": "Detalle otro QAP"}), name="otro")
    eid3 = eq_id(cod3)
    expect(R, "Create: categoria Otro con detalle valido", r, status=302, redirect=f"/Equipment/Details/{eid3}")
    cod4 = codigo()
    r = post_form(c, "/Equipment/Create", base(N("equipo otro"), cod4, **{"Input.TypeClassification": "7", "Input.OtherClassificationDetail": "detalle"}), name="equipo otro")
    expect(R, "Create: equipo subtipo Otro con detalle valido", r, status=302, redirect=f"/Equipment/Details/{eq_id(cod4)}")
    cod5 = codigo()
    hostil = f"QAP <b>negrita</b> {P.RUN}"
    r = post_form(c, "/Equipment/Create", base(hostil, cod5), name="html hostil")
    eid5 = eq_id(cod5)
    if eid5:
        _, page = c.get(f"/Equipment/Details/{eid5}")
        check(R, "Create: nombre con HTML se guarda y se muestra escapado en Details", "<b>negrita</b>" not in page and "&lt;b&gt;negrita&lt;/b&gt;" in page)
    else:
        check(R, "Create: nombre con HTML se rechaza o se guarda (sin excepcion)", r.status in (200, 302), P.dump(r))
    r = post_form(c, "/Equipment/Create", base("  " + N("espacios") + "   ", codigo()), name="espacios")
    nm = scalar("select top 1 Name from Equipments where Name like ? order by Id desc", f"%{N('espacios')}%")
    check(R, "Create: el nombre se guarda sin espacios sobrantes", nm == N("espacios"), f"guardado={nm!r}")

    # ------------------------------------------------------------ Details
    st, _ = c.get("/Equipment/Details/99999")
    check(R, "Details: id inexistente -> 404", st == 404, f"status={st}")
    st, _ = c.get("/Equipment/Details/abc")
    check(R, "Details: id no numerico -> 404", st == 404, f"status={st}")
    st, page = c.get(f"/Equipment/Details/{eid1}?SearchTerm=ZZZNOEXISTE")
    check(R, "Details: busqueda de unidades sin resultados responde 200", st == 200, f"status={st}")

    # ------------------------------------------------------------ Edit
    st, _ = c.get("/Equipment/Edit/99999")
    check(R, "Edit: GET id inexistente -> 404", st == 404, f"status={st}")
    st, _ = c.get(f"/Equipment/Edit/{eid1}")
    check(R, "Edit: GET 200", st == 200)
    ep = f"/Equipment/Edit/{eid1}"
    r = post_form(c, ep, {"Input.Name": ""}, name="edit sin nombre")
    expect(R, "Edit: nombre vacio", r, status=200, error="El nombre del equipo es obligatorio")
    r = post_form(c, ep, {"Input.Name": "x" * 101}, name="edit nombre 101")
    expect(R, "Edit: nombre de 101 caracteres", r, status=200, error="El nombre no puede superar los 100 caracteres")
    r = post_form(c, ep, {"Input.Name": "Extractor de aire", "Input.Model": ""}, name="edit duplicado")
    expect(R, "Edit: nombre duplicado de otro equipo", r, status=200, error="Ya existe otro equipo registrado con este nombre y modelo.")
    r = post_form(c, ep, {"Input.UsefulLifeYears": "101"}, name="edit vida 101")
    rechazo(R, "Edit: vida util 101", r)
    check(R, "Edit: vida util 101 muestra un mensaje en espanol", r.status == 200 and bool(r.errors) and not P._EN.search(" ".join(r.errors)), P.dump(r))
    r = post_form(c, ep, {"Input.UsefulLifeYears": "60"}, name="edit vida 60", follow=False)
    check(R, "Edit: vida util 60 (Create la rechaza por encima de 50; Edit usa Range(0,100)) -> debe rechazarse igual", r.status == 200, P.dump(r))
    r = post_form(c, ep, {"Input.CountryId": "99999"}, name="edit pais")
    expect(R, "Edit: pais inexistente", r, status=200, error="El país seleccionado no está disponible.")
    r = post_form(c, ep, {}, name="edit 51 notas", extra=[("Input.Notes[%d]" % i, f"n{i}") for i in range(2, 53)])
    rechazo(R, "Edit: mas de 50 notas", r)
    check(R, "Edit: mas de 50 notas muestra el mensaje", r.has_error("No se permiten más de 50 notas por equipo."), P.dump(r))
    r = post_form(c, ep, {"Input.Description": "d" * 2001}, name="edit descripcion 2001")
    rechazo(R, "Edit: descripcion de 2001 caracteres", r)
    check(R, "Edit: descripcion de 2001 caracteres muestra un mensaje en espanol", r.status == 200 and bool(r.errors) and not P._EN.search(" ".join(r.errors)), P.dump(r))
    campos = [(k, v) for k, v in c.form(ep)[1] if k != "Input.Name"] + [("Input.Name", N("img edit"))]
    for lbl, fn, content, ctype, esperado in (("extension .txt", "x.txt", b"hola", "text/plain", "La imagen debe ser JPG, PNG o WEBP."),
                                               ("firma falsa", "x.png", b"nada", "image/png", "El contenido del archivo no corresponde a una imagen válida.")):
        r = P.multipart_post(c, ep, campos, [("Input.ImageUpload", fn, content, ctype)], name=f"edit imagen {lbl}")
        expect(R, f"Edit: imagen invalida ({lbl})", r, status=200, error=esperado)

    # la clasificacion no se cambia por POST
    nuevo = N("editado")
    r = post_form(c, ep, {"Input.Name": nuevo, "Input.Brand": "Marca2", "Input.Model": "M-2", "Input.Description": "desc 2", "Input.Category": "1", "Input.TypeClassification": "5", "Input.UtensilType": "3"},
                  name="edit valido", extra=[])
    expect(R, "Edit: cambio valido", r, status=302, redirect=f"/Equipment/Details/{eid1}", toast=f"Datos del equipo '{nuevo}' actualizados correctamente.", toast_kind="success")
    row = sql("select Name,Brand,Model,Category,TypeClassification,UtensilType,ModifiedById from Equipments where Id=?", eid1)
    check(R, "Edit: la clasificacion enviada por POST (cat 1, tipo 5, utensilio 3) NO altera la definicion", bool(row) and row[0][3] == 0 and row[0][4] == 0 and row[0][5] is None, f"{row}")
    check(R, "Edit: nombre/marca/modelo actualizados y ModifiedById registrado", bool(row) and row[0][:3] == (nuevo, "Marca2", "M-2") and row[0][6] is not None, f"{row}")
    # notas se reemplazan
    r = post_form(c, ep, {}, name="edit notas", extra=[])
    f = [(k, v) for k, v in c.form(ep)[1] if not k.startswith("Input.Notes")]
    r = c._post(c.form(ep)[0], f + [("Input.Notes[0]", "unica nota")], True, "edit notas reemplazo")
    check(R, "Edit: las notas se reemplazan por completo (quedaba 2, ahora 1)", scalar("select count(*) from EquipmentNotes where EquipmentId=?", eid1) == 1)
    r = post_form(c, ep, {"Input.Id": "99999"}, name="edit id inexistente")
    check(R, "Edit: POST con Input.Id inexistente -> 404", r.status == 404, P.dump(r))
    # reenvio rapido (doble clic)
    a1 = post_form(c, ep, {"Input.Name": N("doble")}, name="doble clic 1")
    a2 = post_form(c, ep, {"Input.Name": N("doble")}, name="doble clic 2")
    check(R, "Edit: doble envio del mismo cambio es idempotente (ambos 302)", a1.status == 302 and a2.status == 302, f"{a1.status},{a2.status}")

    # ------------------------------------------------------------ DeleteUnit y bajas logicas (equipo con unidades de todos los estados)
    cod6 = codigo()
    nom6 = N("baja")
    post_form(c, "/Equipment/Create", base(nom6, cod6), name="equipo para baja")
    eid6 = eq_id(cod6)
    u_op, inv_op = P.nueva_unidad(c, lab, "b1", equipo=eid6)
    # «En Préstamo» solo por el flujo real (salida L-3): crear una unidad ya prestada está bloqueado (2026-10-18)
    _loan = P.llevar_a(c, lab, "b2", "P5", equipo=eid6)
    u_loan, inv_loan = _loan["unit"], _loan["inv"]
    u_mnt, inv_mnt = P.nueva_unidad(c, lab, "b3", equipo=eid6, **{"Input.CurrentStatus": "1"})
    u_plan, inv_plan = P.nueva_unidad(c, lab, "b4", equipo=eid6)
    pid_plan = P.planificar(c, [u_plan])[u_plan]
    u_otro, _ = P.nueva_unidad(c, lab, "b5", equipo=EQUIP)   # unidad de OTRO equipo: no debe verse afectada
    check(R, "Fixture baja: 4 unidades del equipo y 1 de otro equipo creadas", all([u_op, u_loan, u_mnt, u_plan, u_otro]))

    # DeleteUnit de una unidad
    r = P.post_raw(c, f"/Equipment/Details/{u_op}?handler=DeleteUnit", [], f"/Equipment/Details/{eid6}", name="DeleteUnit")
    expect(R, "DeleteUnit: unidad operativa", r, status=302, redirect=f"/Equipment/Details/{eid6}", toast=f"Unidad {nom6} ({inv_op}) dada de baja correctamente.", toast_kind="success")
    check(R, "DeleteUnit: la unidad queda Deleted (99) y las demas intactas", unidad(u_op)["status"] == 99 and unidad(u_loan)["status"] == 10 and unidad(u_mnt)["status"] == 1 and unidad(u_otro)["status"] == 0)
    _, page = c.get(f"/Equipment/Details/{eid6}")
    check(R, "DeleteUnit: la unidad dada de baja ya no aparece en Details", inv_op not in page and inv_loan in page)
    r = P.post_raw(c, f"/Equipment/Details/{u_op}?handler=DeleteUnit", [], f"/Equipment/Details/{eid6}", name="DeleteUnit repetido")
    check(R, "DeleteUnit repetido sobre una unidad ya dada de baja: 302 sin mensaje de exito", r.status == 302 and not r.has_toast("dada de baja", "success"), P.dump(r))
    r = P.post_raw(c, "/Equipment/Details/99999?handler=DeleteUnit", [], f"/Equipment/Details/{eid6}", name="DeleteUnit inexistente", follow=False)
    check(R, "DeleteUnit: id inexistente no produce 500 (plan: sospecha de excepcion por id=null)", r.status < 500, P.dump(r))
    r = P.post_raw(c, f"/Equipment/Details/{u_loan}?handler=DeleteUnit", [], f"/Equipment/Details/{eid6}", name="DeleteUnit OnLoan")
    check(R, "DeleteUnit de una unidad en prestamo (OnLoan): permitido sin aviso (observacion del plan)", unidad(u_loan)["status"] == 99, f"estado={unidad(u_loan)['status']} {P.dump(r)}")
    # unidad dentro de un plan activo
    r = P.post_raw(c, f"/Equipment/Details/{u_plan}?handler=DeleteUnit", [], f"/Equipment/Details/{eid6}", name="DeleteUnit planificada")
    p = plan(pid_plan)
    check(R, "DeleteUnit de unidad planificada: el plan sigue existiendo (sin borrado en cascada)", p is not None and p["unit"] == u_plan, f"{p}")
    ok_dash = True
    det = []
    for step in range(0, 8):
        stc, pg = c.get(f"/?ShowWizard=true&Step={step}&ManagementId={P.MGMT}&SelectedLabId={lab}")
        if stc != 200 or "Codigo tecnico de seguimiento" in pg:
            ok_dash = False
            det.append(f"step {step}: {stc}")
    check(R, "Wizard: los 8 pasos siguen respondiendo 200 tras dar de baja unidades planificadas", ok_dash, ";".join(det))

    # Delete de la definicion (cascada a todas las unidades, incluso OnLoan)
    st, _ = c.get(f"/Equipment/Delete/{eid6}")
    check(R, "Delete: GET 200", st == 200)
    r = post_form(c, f"/Equipment/Delete/{eid6}", {}, name="delete definicion")
    expect(R, "Delete: baja de la definicion", r, status=302, redirect="/Equipment", toast=f"La definición de equipo '{nom6}' ha sido marcada como eliminada.", toast_kind="success")
    check(R, "Delete: equipo Eliminado (Status=2) y TODAS sus unidades Deleted", scalar("select Status from Equipments where Id=?", eid6) == 2 and scalar("select count(*) from EquipmentUnits where EquipmentId=? and CurrentStatus<>99", eid6) == 0)
    check(R, "Delete: la unidad de OTRO equipo no se ve afectada", unidad(u_otro)["status"] == 0)
    check(R, "Delete: el plan de la unidad borrada sigue existiendo (queda apuntando a una unidad Deleted)", plan(pid_plan) is not None)
    st, _ = c.get(f"/Equipment/Details/{eid6}")
    check(R, "Delete: Details del equipo eliminado -> 404", st == 404, f"status={st}")
    r = post_form(c, f"/Equipment/Delete/{eid6}", {}, name="delete repetido", follow=False) if False else P.post_raw(c, f"/Equipment/Delete/{eid6}", [], "/Equipment", follow=False, name="delete repetido")
    check(R, "Delete: segunda baja del mismo equipo -> 404", r.status == 404, P.dump(r))
    st, _ = c.get("/Equipment/Delete/99999")
    check(R, "Delete: GET id inexistente -> 404", st == 404)
    r = P.post_raw(c, "/Equipment/Delete/99999", [], "/Equipment", follow=False, name="delete inexistente")
    check(R, "Delete: POST id inexistente -> 404", r.status == 404, P.dump(r))
    st, _ = c.get(f"/Equipment/Edit/{eid6}")
    check(R, "Edit de un equipo YA eliminado: el servidor lo abre (IgnoreQueryFilters) - observacion", st in (200, 404), f"status={st} (200 = se puede editar una baja)")
    cod_dup = scalar("select CatalogCode from Equipments where Id=?", eid6)
    r = post_form(c, "/Equipment/Create", base(N("reuso"), cod_dup), name="reusar codigo de baja")
    expect(R, "Create: el codigo de un equipo dado de baja sigue reservado", r, status=200, error="El código de catálogo ya existe.")
    r = post_form(c, "/Equipment/Create", base(nom6, codigo()), name="reusar nombre de baja")
    check(R, "Create: el nombre de un equipo dado de baja puede reutilizarse", r.status == 302, P.dump(r))

    # ------------------------------------------------------------ Acceso: sin sesion, sin token, roles
    for p in ("/Equipment/Create", f"/Equipment/Edit/{eid1}", f"/Equipment/Details/{eid1}", f"/Equipment/Delete/{eid1}"):
        P.check_anon(R, "Equipment", p)
    P.check_anon(R, "Equipment", "/Equipment/Create", post=True)
    P.tokenless(R, c, "Equipment/Create", "/Equipment/Create", list(base(N("sintoken"), codigo()).items()))
    P.tokenless(R, c, "Equipment/Edit", ep, [("Input.Id", str(eid1)), ("Input.Name", "x"), ("Input.Category", "0")])
    P.tokenless(R, c, "Equipment/Delete", f"/Equipment/Delete/{eid1}", [])
    P.tokenless(R, c, "Equipment/DeleteUnit", f"/Equipment/Details/{u_otro}?handler=DeleteUnit", [])
    check(R, "Sin token: ninguna escritura ocurrio", unidad(u_otro)["status"] == 0 and scalar("select Status from Equipments where Id=?", eid1) == 0)
    pw = os.environ["E2E_PW"]
    for usuario, rol in (("qa_sup", "Supervisor"), ("qa_cierre", "PracticeClosure")):
        cl = P.client(usuario, pw)
        cod = codigo()
        r = post_raw(cl, "/Equipment/Create", list(base(N("rol " + rol), cod).items()), "/", follow=False, name=f"create como {rol}")
        check(R, f"Rol {rol}: POST Equipment/Create denegado (302/403) y sin alta", r.status in (302, 403) and not eq_id(cod), P.dump(r))
    cod_c = codigo()
    post_form(c, "/Equipment/Create", base(N("para cierre"), cod_c), name="equipo para PracticeClosure")
    eid_c = eq_id(cod_c)
    cl = P.client("qa_cierre", pw)
    r = post_raw(cl, f"/Equipment/Delete/{eid_c}", [], "/", follow=False, name="delete como PracticeClosure")
    check(R, "Rol PracticeClosure: POST Equipment/Delete denegado y la definicion sigue Activa", r.status in (302, 403) and scalar("select Status from Equipments where Id=?", eid_c) == 0,
          P.dump(r) + f" estado={scalar('select Status from Equipments where Id=?', eid_c)}")
    # el Supervisor SI puede ver y dar de baja (ManagementRoles) -> se documenta con una definicion propia
    cl = P.client("qa_sup", pw)
    cod_s = codigo()
    post_form(c, "/Equipment/Create", base(N("para sup"), cod_s), name="equipo para supervisor")
    eid_s = eq_id(cod_s)
    st, _ = cl.get(f"/Equipment/Details/{eid_s}")
    check(R, "Rol Supervisor: Details 200 (ManagementRoles)", st == 200, f"status={st}")
    r = post_raw(cl, f"/Equipment/Delete/{eid_s}", [], f"/Equipment/Details/{eid_s}", follow=False, name="delete como Supervisor")
    check(R, "Rol Supervisor: puede dar de baja una definicion (Delete es ManagementRoles; el plan lo marca como M)", r.status == 302 and scalar("select Status from Equipments where Id=?", eid_s) == 2, P.dump(r))

    # ------------------------------------------------------------ limpieza propia: dar de baja lo creado por este modulo para no ensuciar listados
    for e in (eid1, eid2, eid3):
        if e:
            post_form(c, f"/Equipment/Delete/{e}", {}, name="limpieza", follow=False)


if __name__ == "__main__":
    P.run_module("equipment", run)
