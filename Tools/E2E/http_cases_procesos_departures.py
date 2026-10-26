"""Salidas L-3: MassCreate / Create / Edit / Details / Index. Solo contra la copia QA."""
import os
import re

import http_cases_procesos_common as P
from http_cases_procesos_common import check, expect, sql, scalar, plan, unidad, post_form, post_raw, fase

ENUM_EN = re.compile(r"\b(Operational|OnLoan|Returned|Active|Cancelled)\b")


def dp(did):
    r = sql("select Status,Type,BorrowerId,DepartureDate,EstimatedReturnDate,ActualReturnDate,EquipmentUnitId,ManagementId from Departures where Id=?", did)
    return dict(zip(["status", "type", "borrower", "dep", "est", "act", "unit", "mgmt"], r[0])) if r else None


def n_dep(unit):
    return scalar("select count(*) from Departures where EquipmentUnitId=? and Status<>99", unit)


def n_dep_all(unit):
    return scalar("select count(*) from Departures where EquipmentUnitId=?", unit)


def run(R, c):
    pw = os.environ["E2E_PW"]
    lab = P.lab_propio(c, "salidas")
    lab2 = P.lab_propio(c, "salidas B")
    ctx = {}
    for k in ("a", "b", "c", "d", "e", "f", "g", "h", "i", "j"):
        ctx[k] = P.llevar_a(c, lab, "s" + k, "P4")
    ext = P.llevar_a(c, lab, "sx", "P4", tech=P.TECH_EXT)
    p3 = P.llevar_a(c, lab, "s3", "P3")
    p2 = P.llevar_a(c, lab, "s2", "P2")
    p1ok = P.llevar_a(c, lab, "s1", "P1OK")
    # Regla nueva: ya no se admite un L-8 sin tecnico fuera del asistente (antes fabricaba el plan 'en L-3 sin tecnico').
    nt = P.llevar_a(c, lab, "sn", "P3")
    nt_mn_antes = scalar("select count(*) from Maintenances where EquipmentUnitId=?", nt["unit"])
    r_sn = P.l8(c, nt["plan"], {"Input.TechnicianId": ""}, wizard=False, name="sn L-8 sin tecnico (ruta no wizard)", follow=True)
    other_lab = P.llevar_a(c, lab2, "sl", "P4")
    for k in ctx:
        fase(R, f"Fixture {k} en L-3", ctx[k]["plan"], 4, 6, 0)
    fase(R, "Fixture ext en L-3", ext["plan"], 4, 6, 0)
    pid = lambda k: ctx[k]["plan"]
    uid = lambda k: ctx[k]["unit"]
    pn = plan(nt["plan"])
    check(R, "L-8 sin tecnico fuera del asistente se rechaza (no crea mantenimiento y el plan no avanza)",
          r_sn.status == 200 and pn["phase"] == 3 and pn["mnt"] is None and scalar("select count(*) from Maintenances where EquipmentUnitId=?", nt["unit"]) == nt_mn_antes,
          f"{P.dump(r_sn)} mantenimientos={nt_mn_antes}->{scalar('select count(*) from Maintenances where EquipmentUnitId=?', nt['unit'])} plan={pn}")

    q = f"/Departures/MassCreate?isWizard=true&labId={lab}&managementId={P.MGMT}"
    # ================================================================ MassCreate
    st, pg = c.get(q)
    check(R, "MassCreate: GET 200", st == 200, f"status={st}")
    st, js = c.get(f"/Departures/MassCreate?handler=EquipmentByLab&labId={lab}&managementId={P.MGMT}")
    check(R, "MassCreate: EquipmentByLab lista los planes en L-3 de la gestion 30 con su tecnico (sin depender de un plan sin tecnico)",
          st == 200 and all(f'"planId":{ctx[k]["plan"]}' in js for k in ctx) and f'"technicianId":{P.TECH_INT}' in js and '"hasTechnician":true' in js and '"hasTechnician":false' not in js, f"status={st} {js[:160]}")
    st, js = c.get(f"/Departures/MassCreate?handler=EquipmentByLab&labId={lab}&managementId=27")
    check(R, "MassCreate: EquipmentByLab con gestion no activa no devuelve planes", st == 200 and js.strip() in ("[]", ""), f"status={st} {js[:100]}")

    r = P.l3(c, 0, 0, lab, filas=[(pid("a"), uid("a"), "true")], salida=P.d(0), retorno=P.d(-5), name="retorno anterior", follow=True)
    expect(R, "MassCreate: retorno anterior a la salida", r, status=200, toast="La fecha de retorno debe ser igual o posterior a la fecha de salida.", toast_kind="error")
    r = P.l3(c, 0, 0, lab, filas=[(pid("a"), uid("a"), "true")], salida=P.d(0), retorno=P.d(0), name="retorno igual a salida", follow=False)
    check(R, "MassCreate: retorno igual a la salida es valido ('igual o posterior')", r.status == 302 and plan(pid("a"))["phase"] == 5, P.dump(r))
    # a ya salio: reponer plan b para el resto
    r = P.l3(c, 0, 0, lab, filas=[(pid("b"), uid("b"), "false")], name="nada incluido", follow=True)
    check(R, "MassCreate: ninguna fila incluida -> 'Debe incluir al menos un equipo para registrar la salida.'", r.has_toast("Debe incluir al menos un equipo para registrar la salida.", "error"), P.dump(r))
    r = post_raw(c, P.with_handler(q, None), [("IsWizard", "true"), ("LabId", lab), ("ManagementId", P.MGMT), ("DepartureDate", P.d(0)), ("EstimatedReturnDate", P.d(5))], q, follow=True, name="sin filas")
    check(R, "MassCreate: sin filas -> 'Debe incluir al menos un equipo...' con 'sin filas recibidas'", r.has_toast("Debe incluir al menos un equipo para registrar la salida.", "error") and r.has_toast("sin filas recibidas"), P.dump(r))
    r = P.l3(c, 0, 0, lab, filas=[(pid("b"), uid("b"), "true")], mgmt=99999, name="gestion inexistente", follow=True)
    check(R, "MassCreate: gestion inexistente -> 'No hay gestión activa...' o similar, sin 500 ni cambios", r.status < 500 and plan(pid("b"))["phase"] == 4, P.dump(r))
    r = P.l3(c, 0, 0, lab, filas=[(pid("b"), uid("b"), "true")], mgmt=27, name="gestion cerrada", follow=True)
    check(R, "MassCreate: gestion historica 27 con plan de la 30 -> se omite ('No se registró ninguna salida')", r.has_toast("No se registró ninguna salida. Verifique que los equipos seleccionados tengan técnico asignado en el registro de mantenimiento.", "error") and plan(pid("b"))["phase"] == 4, P.dump(r))
    r = P.l3(c, 0, 0, lab, filas=[(nt["plan"], nt["unit"], "true")], name="sin tecnico", follow=True)
    expect(R, "MassCreate: unico equipo sin tecnico L-8", r, status=200, toast="No se registró ninguna salida. Verifique que los equipos seleccionados tengan técnico asignado en el registro de mantenimiento.", toast_kind="error")
    l3_sin_tec = scalar("select count(*) from ManagementPlans mp join Maintenances m on m.Id = mp.MaintenanceId where mp.ManagementId = ? and mp.CurrentPhase = 4 and m.TechnicianId is null", P.MGMT)
    check(R, "MassCreate sin tecnico: no existe ningun plan en L-3 sin tecnico en la gestion 30", l3_sin_tec == 0, f"planes en L-3 sin tecnico={l3_sin_tec}")
    r = P.l3(c, 0, 0, lab, filas=[(p3["plan"], p3["unit"], "true")], name="plan en L-8", follow=True)
    check(R, "MassCreate: plan que aun esta en L-8 -> omitido", r.has_toast("No se registró ninguna salida", "error") and plan(p3["plan"])["phase"] == 3, P.dump(r))
    r = P.l3(c, 0, 0, lab, filas=[(p2["plan"], p2["unit"], "true"), (p1ok["plan"], p1ok["unit"], "true")], name="planes sin L-8", follow=True)
    check(R, "MassCreate: planes en L-7 y L-6 -> omitidos", r.has_toast("No se registró ninguna salida", "error") and plan(p2["plan"])["phase"] == 2 and plan(p1ok["plan"])["phase"] == 1, P.dump(r))
    r = P.l3(c, 0, 0, lab2, filas=[(pid("b"), uid("b"), "true")], name="laboratorio equivocado", follow=True)
    check(R, "MassCreate: laboratorio que no corresponde al plan -> omitido", r.has_toast("No se registró ninguna salida", "error") and plan(pid("b"))["phase"] == 4, P.dump(r))
    r = P.l3(c, 0, 0, lab, filas=[(pid("b"), uid("c"), "true")], name="unidad que no corresponde", follow=True)
    check(R, "MassCreate: unidad que no coincide con el plan -> omitido", r.has_toast("No se registró ninguna salida", "error") and plan(pid("b"))["phase"] == 4, P.dump(r))
    r = P.l3(c, 0, 0, lab, filas=[(999999, uid("b"), "true")], name="plan inexistente", follow=True)
    check(R, "MassCreate: plan inexistente -> omitido y sin 500", r.status < 500 and r.has_toast("No se registró ninguna salida", "error"), P.dump(r))
    r = P.l3(c, 0, 0, lab, filas=[(other_lab["plan"], other_lab["unit"], "true")], name="plan de otro laboratorio", follow=True)
    check(R, "MassCreate: plan de otro laboratorio -> omitido", r.has_toast("No se registró ninguna salida", "error") and plan(other_lab["plan"])["phase"] == 4, P.dump(r))

    # borrador
    r = P.l3(c, 0, 0, lab, filas=[(pid("b"), uid("b"), "true")], draft=True, name="borrador", follow=True)
    pb = plan(pid("b"))
    check(R, "MassCreate borrador: 'Borrador de la salida de equipos guardado para 1 equipo(s).' y vuelve al paso 4", r.status == 302 and "Step=4" in (r.location or "") and r.has_toast("Borrador de la salida de equipos guardado para 1 equipo(s).", "success"), P.dump(r))
    check(R, "MassCreate borrador: plan sigue en L-3 (4/6) con IsDraft/DraftPhase=4, la unidad NO sale (Operativa) y la salida queda Cancelada (99)", pb["phase"] == 4 and pb["state"] == 6 and pb["draft"] == 1 and pb["draftphase"] == 4 and unidad(uid("b"))["status"] == 0 and pb["dep"] and dp(pb["dep"])["status"] == 99, f"{pb} {dp(pb['dep']) if pb['dep'] else None}")
    r = P.l3(c, 0, 0, lab, filas=[(pid("b"), uid("b"), "true")], draft=True, name="borrador repetido", follow=True)
    check(R, "MassCreate borrador repetido: reutiliza la misma salida (no duplica)", n_dep_all(uid("b")) == 1, f"salidas={n_dep_all(uid('b'))}")

    # guardado definitivo: borrador b + c + externo + (d no incluido). Regla nueva: ya no existe un plan en L-3 sin técnico.
    filas = [(pid("b"), uid("b"), "true"), (pid("c"), uid("c"), "true"), (ext["plan"], ext["unit"], "true"), (pid("d"), uid("d"), "false")]
    r = P.l3(c, 0, 0, lab, filas=filas, salida=P.d(0), retorno=P.d(10), name="guardado masivo", follow=True)
    check(R, "MassCreate: '3 salida(s) registrada(s) correctamente.'", r.status == 302 and "Step=5" in (r.location or "") and r.has_toast("3 salida(s) registrada(s) correctamente.", "success"), P.dump(r))
    for k in ("b", "c"):
        pk = plan(pid(k))
        check(R, f"MassCreate {k}: plan a Kardex/AwaitingKardex (5/7), sin borrador, unidad En Prestamo (10)", pk["phase"] == 5 and pk["state"] == 7 and pk["draft"] == 0 and unidad(uid(k))["status"] == 10, f"{pk} unidad={unidad(uid(k))['status']}")
    pc = plan(pid("c"))
    dc = dp(pc["dep"])
    check(R, "MassCreate: salida Activa (0), tipo Mantenimiento Interno (5), responsable = tecnico del L-8, fechas y gestion correctas",
          dc["status"] == 0 and dc["type"] == 5 and dc["borrower"] == P.TECH_INT and str(dc["dep"])[:10] == P.d(0) and str(dc["est"])[:10] == P.d(10) and dc["mgmt"] == P.MGMT, f"{dc}")
    de = dp(plan(ext["plan"])["dep"])
    check(R, "MassCreate: tecnico externo -> tipo Mantenimiento Externo (3)", de["type"] == 3 and de["borrower"] == P.TECH_EXT, f"{de}")
    pb = plan(pid("b"))
    check(R, "MassCreate: el borrador pasa a definitiva sobre la MISMA salida (Activa, 1 sola)", dp(pb["dep"])["status"] == 0 and n_dep_all(uid("b")) == 1, f"{dp(pb['dep'])} n={n_dep_all(uid('b'))}")
    check(R, "MassCreate: el equipo marcado Include=false no salio", plan(pid("d"))["phase"] == 4 and unidad(uid("d"))["status"] == 0)
    check(R, "MassCreate: ProductName derivado del servidor (no del HTML) = 'Equipo (Inventario)'",
          scalar("select ProductName from DepartureItems where DepartureId=?", pc["dep"]) == f"{scalar('select Name from Equipments where Id=?', P.EQUIP)} ({ctx['c']['inv']})", "")
    # doble envio
    r = P.l3(c, 0, 0, lab, filas=[(pid("c"), uid("c"), "true")], name="doble envio", follow=True)
    check(R, "MassCreate doble envio: 'No se registró ninguna salida...' y no duplica", r.has_toast("No se registró ninguna salida", "error") and n_dep_all(uid("c")) == 1, P.dump(r))
    # no wizard
    r = P.l3(c, 0, 0, lab, filas=[(pid("e"), uid("e"), "true")], wizard=False, name="no wizard", follow=True)
    check(R, "MassCreate fuera del wizard: guarda y vuelve a Departures", r.status == 302 and "/Departures" in (r.location or "") and r.has_toast("1 salida(s) registrada(s) correctamente.", "success"), P.dump(r))
    # fechas extremas
    r = P.l3(c, 0, 0, lab, filas=[(pid("f"), uid("f"), "true")], salida="2000-01-01", retorno="2999-12-31", name="fechas extremas", follow=True)
    check(R, "MassCreate: fechas extremas (2000 / 2999) sin 500", r.status < 500, P.dump(r))
    r = P.l3(c, 0, 0, lab, filas=[(pid("g"), uid("g"), "true")], salida="no-es-fecha", retorno=P.d(5), name="fecha invalida", follow=True)
    check(R, "MassCreate: fecha de salida invalida -> rechazo con mensaje en espanol y sin 500", r.status < 500 and not P._EN.search(P.toast_text(r) + " ".join(r.errors)), P.dump(r))

    # ================================================================ Create individual
    cu = {}
    for k in ("w", "x", "y", "z"):
        cu[k] = P.llevar_a(c, lab, "sc" + k, "P4")
    cw = f"/Departures/Create?isWizard=true&managementPlanId={cu['w']['plan']}"
    st, _ = c.get(cw)
    check(R, "Create wizard: GET 200", st == 200, f"status={st}")
    r = post_form(c, cw, {"Input.DepartureDate": P.d(0), "Input.EstimatedReturnDate": P.d(-3)}, name="wizard retorno anterior", follow=False)
    check(R, "Create wizard: retorno anterior a la salida debe rechazarse (MassCreate si lo rechaza)", r.status == 200 and plan(cu["w"]["plan"])["phase"] == 4, P.dump(r) + f" plan={plan(cu['w']['plan'])['phase']}")
    r = post_form(c, cw, {"Input.DepartureObservations": "o" * 501}, name="wizard observaciones 501", follow=True)
    check(R, "Create wizard: observaciones de 501 caracteres -> mensaje en espanol, plan sin cambios", r.status == 200 and plan(cu["w"]["plan"])["phase"] == 4 and not P._EN.search(P.toast_text(r) + " ".join(r.errors)), P.dump(r))
    r = post_form(c, cw, {"Input.DepartureObservations": "Salida QAP", "Input.DepartureDate": P.d(0), "Input.EstimatedReturnDate": P.d(7)}, name="wizard valido", follow=True)
    pw_ = plan(cu["w"]["plan"])
    check(R, "Create wizard: valido -> paso 5 del wizard, plan 5/7, unidad En Prestamo", r.status == 302 and "Step=5" in (r.location or "") and pw_["phase"] == 5 and pw_["state"] == 7 and unidad(cu["w"]["unit"])["status"] == 10, P.dump(r) + f" {pw_}")
    dw = dp(pw_["dep"]) if pw_["dep"] else {}
    check(R, "Create wizard: salida Activa, tipo Mantenimiento Interno, responsable = tecnico L-8", dw.get("status") == 0 and dw.get("type") == 5 and dw.get("borrower") == P.TECH_INT, f"{dw}")
    r = post_form(c, cw, {"Input.DepartureObservations": "Reenvio"}, name="wizard doble envio", follow=True)
    check(R, "Create wizard doble envio: no duplica la salida", n_dep_all(cu["w"]["unit"]) == 1, P.dump(r) + f" n={n_dep_all(cu['w']['unit'])}")
    # borrador
    cx = f"/Departures/Create?isWizard=true&managementPlanId={cu['x']['plan']}"
    r = post_form(c, cx, {}, handler="Draft", name="wizard borrador", follow=True)
    px = plan(cu["x"]["plan"])
    check(R, "Create wizard borrador: 'Borrador de la salida de equipos guardado correctamente.' plan 4/6 en borrador, unidad no sale", r.status == 302 and r.has_toast("Borrador de la salida de equipos guardado correctamente.", "success") and px["phase"] == 4 and px["draft"] == 1 and unidad(cu["x"]["unit"])["status"] == 0, P.dump(r) + f" {px}")

    # no wizard (formulario completo)
    un1 = P.nueva_unidad(c, lab, "scn1")
    un2 = P.nueva_unidad(c, lab, "scn2")
    nf = lambda u, **kw: {"Input.FacultyId": str(P.FAC), "Input.LaboratoryId": str(lab), "Input.EquipmentUnitId": str(u), "Input.BorrowerId": str(P.TECH_INT),
                          "Input.DepartureDate": P.d(0), "Input.EstimatedReturnDate": P.d(5), "Input.DepartureObservations": "Prestamo QAP",
                          "Input.Items[0].ProductName": "Equipo prestado QAP", "Input.Items[0].Quantity": "1", "Input.Items[0].UnitOfMeasure": "UNIDAD", **kw}
    r = post_form(c, "/Departures/Create", nf(un1[0], **{"Input.EstimatedReturnDate": P.d(-4)}), name="no wizard retorno anterior", follow=False)
    check(R, "Create: retorno anterior a la salida debe rechazarse (hoy no se valida; MassCreate si)", r.status == 200 and n_dep_all(un1[0]) == 0, P.dump(r) + f" salidas={n_dep_all(un1[0])}")
    r = post_form(c, "/Departures/Create", nf(un2[0], **{"Input.DepartureObservations": "o" * 501}), name="no wizard observaciones 501", follow=True)
    check(R, "Create: observaciones de 501 -> mensaje en espanol", r.status == 200 and not P._EN.search(P.toast_text(r) + " ".join(r.errors)) and n_dep_all(un2[0]) == 0, P.dump(r))
    r = post_form(c, "/Departures/Create", nf(un2[0], **{"Input.Items[0].Quantity": "0"}), name="no wizard cantidad 0", follow=True)
    check(R, "Create: cantidad de item 0 -> 'La cantidad debe ser mayor a 0' y sin salida", r.status == 200 and (r.has_error("La cantidad debe ser mayor a 0") or r.has_toast("La cantidad debe ser mayor a 0")) and n_dep_all(un2[0]) == 0, P.dump(r))
    r = post_form(c, "/Departures/Create", nf(un2[0], **{"Input.BorrowerId": "99999"}), name="no wizard responsable inexistente", follow=True)
    check(R, "Create: responsable inexistente -> error controlado sin 500", r.status < 500 and n_dep_all(un2[0]) == 0, P.dump(r))
    r = post_form(c, "/Departures/Create", nf(99999), name="no wizard unidad inexistente", follow=True)
    check(R, "Create: unidad inexistente -> error controlado sin 500", r.status < 500, P.dump(r))
    r = post_form(c, "/Departures/Create", {k: v for k, v in nf(un2[0]).items() if k != "Input.BorrowerId"}, name="no wizard sin responsable", follow=True)
    check(R, "Create: sin responsable -> rechazo (400/200) y sin salida", r.status < 500 and n_dep_all(un2[0]) == 0, P.dump(r))
    r = post_form(c, "/Departures/Create", nf(un1[0]), name="no wizard valido", follow=True)
    check(R, "Create: salida valida fuera del wizard -> 'Salida de equipo registrada exitosamente.' y unidad En Prestamo", r.status == 302 and r.has_toast("Salida de equipo registrada exitosamente.", "success") and unidad(un1[0])["status"] == 10, P.dump(r))
    d1 = scalar("select top 1 Id from Departures where EquipmentUnitId=? order by Id desc", un1[0])
    check(R, "Create no-wizard: tipo derivado del responsable (interno -> Prestamo Interno=2) y estado Activo", dp(d1)["type"] == 2 and dp(d1)["status"] == 0, f"{dp(d1)}")
    r = post_form(c, "/Departures/Create", nf(un1[0]), name="no wizard segunda salida misma unidad", follow=True)
    check(R, "Create: una unidad que YA esta En Prestamo no admite otra salida activa", n_dep(un1[0]) == 1, P.dump(r) + f" salidas activas={n_dep(un1[0])}")
    # sobre un plan que no esta en L-3 (ruta no wizard con plan)
    r = post_form(c, f"/Departures/Create?managementPlanId={p2['plan']}", nf(p2["unit"], **{"Input.LaboratoryId": str(lab)}), name="no wizard con plan en L-7", follow=True)
    check(R, "Create: registrar una salida sobre un plan que aun esta en L-7 no debe mover el plan a Kardex", plan(p2["plan"])["phase"] == 2, P.dump(r) + f" plan={plan(p2['plan'])['phase']}/{plan(p2['plan'])['state']}")
    r = post_form(c, f"/Departures/Create?managementPlanId={p1ok['plan']}", nf(p1ok["unit"]), name="no wizard con plan sano", follow=True)
    check(R, "Create: registrar una salida sobre un plan sano (L-6) no debe moverlo a Kardex", plan(p1ok["plan"])["phase"] == 1, P.dump(r) + f" plan={plan(p1ok['plan'])['phase']}/{plan(p1ok['plan'])['state']}")

    # ================================================================ Details / Index
    st, pg = c.get(f"/Departures/Details?id={d1}")
    check(R, "Details: 200", st == 200, f"status={st}")
    for pth in ("/Departures/Details?id=99999", "/Departures/Details", "/Departures/Details?id=abc"):
        st, _ = c.get(pth)
        check(R, f"Details {pth}: 404", st == 404, f"status={st}")
    for pth in ("/Departures", f"/Departures?SearchTerm={P.PREFIX}", f"/Departures?FilterLaboratoryId={lab}", "/Departures?FilterStatus=0", "/Departures?FilterStatus=99", "/Departures?FilterType=5", "/Departures?FilterType=77", "/Departures?pageIndex=999"):
        st, pg = c.get(pth)
        check(R, f"Index {pth}: 200 sin error", st == 200, f"status={st}")
    st, pg = c.get("/Departures?SearchTerm=ZZZ_NO_EXISTE_QAP")
    check(R, "Index: sin resultados -> 'No hay salidas registradas.' (o texto vacio equivalente)", st == 200 and ("No hay salidas registradas" in pg or "No se encontraron" in pg), f"status={st}")

    # ================================================================ Edit
    st, _ = c.get("/Departures/Edit")
    check(R, "Edit: GET sin id -> 404", st == 404, f"status={st}")
    st, _ = c.get("/Departures/Edit?id=99999")
    check(R, "Edit: GET id inexistente -> 404", st == 404, f"status={st}")
    ep = f"/Departures/Edit?id={d1}"
    st, _ = c.get(ep)
    check(R, "Edit: GET 200", st == 200, f"status={st}")
    s0 = dict(dp(d1))
    r = post_form(c, ep, {"Input.BorrowerId": ""}, name="edit sin responsable", follow=False)
    check(R, "Edit: sin responsable -> rechazado (200) y sin cambios", r.status == 200 and dp(d1) == s0, P.dump(r))
    check(R, "Edit: el mensaje 'El responsable es obligatorio' se muestra", r.has_error("El responsable es obligatorio"), P.dump(r))
    r = post_form(c, ep, {"Input.DepartureObservations": "o" * 501}, name="edit observaciones 501", follow=False)
    check(R, "Edit: observaciones de 501 -> rechazado con mensaje en espanol visible", r.status == 200 and bool(r.errors) and not P._EN.search(" ".join(r.errors)), P.dump(r))
    r = post_form(c, ep, {"Input.Items[0].Quantity": "0"}, name="edit cantidad 0", follow=False)
    check(R, "Edit: cantidad de item 0 -> rechazado con mensaje visible", r.status == 200 and bool(r.errors), P.dump(r))
    r = post_form(c, ep, {"Input.Status": "77"}, name="edit estado 77", follow=False)
    check(R, "Edit: estado inexistente (77) se rechaza", r.status == 200 and dp(d1)["status"] == 0, P.dump(r) + f" estado={dp(d1)['status']}")
    r = post_form(c, ep, {"Input.Type": "77"}, name="edit tipo 77", follow=False)
    check(R, "Edit: tipo de salida inexistente (77) se rechaza", r.status == 200 and dp(d1)["type"] == s0["type"], P.dump(r) + f" tipo={dp(d1)['type']}")
    r = post_form(c, ep, {"Input.BorrowerId": "99999"}, name="edit responsable inexistente", follow=False)
    check(R, "Edit: responsable inexistente no produce 500", r.status < 500, P.dump(r))
    r = post_form(c, ep, {"Input.Id": "99999"}, name="edit Input.Id inexistente", follow=False)
    check(R, "Edit: POST con Input.Id inexistente -> 404", r.status == 404, P.dump(r))
    r = post_form(c, ep, {"Input.EstimatedReturnDate": P.d(-9)}, name="edit retorno anterior", follow=False)
    check(R, "Edit: retorno estimado anterior a la salida debe rechazarse", r.status == 200 and dp(d1)["est"] == s0["est"], P.dump(r) + f" est={dp(d1)['est']}")
    def nueva_salida(suf):
        u = P.nueva_unidad(c, lab, suf)
        post_form(c, "/Departures/Create", nf(u[0]), name=f"salida {suf}", follow=True)
        did = scalar("select top 1 Id from Departures where EquipmentUnitId=? order by Id desc", u[0])
        return u[0], did
    uA, dA = nueva_salida("sea")
    uB, dB = nueva_salida("seb")
    uC, dC = nueva_salida("sec")
    check(R, "Fixture Edit: tres salidas activas con sus unidades En Prestamo", all(dp(x)["status"] == 0 for x in (dA, dB, dC)) and all(unidad(u)["status"] == 10 for u in (uA, uB, uC)), f"{[unidad(u)['status'] for u in (uA, uB, uC)]}")
    r = post_form(c, f"/Departures/Edit?id={dA}", {"Input.Status": "1", "Input.ActualReturnDate": P.d(-30) + "T08:00"}, name="edit devuelta antes de salir", follow=False)
    check(R, "Edit: devolucion real anterior a la salida debe rechazarse", r.status == 200 and dp(dA)["status"] == 0, P.dump(r) + f" estado={dp(dA)['status']}")
    r = post_form(c, f"/Departures/Edit?id={dB}", {"Input.Status": "1", "Input.ActualReturnDate": ""}, name="edit devuelta sin fecha", follow=False)
    check(R, "Edit: estado 'Devuelto' sin fecha real de devolucion debe rechazarse o devolver la unidad (hoy deja la salida Devuelta y la unidad En Prestamo)",
          r.status == 200 or unidad(uB)["status"] != 10, P.dump(r) + f" salida={dp(dB)['status']} unidad={unidad(uB)['status']}")
    r = post_form(c, f"/Departures/Edit?id={dC}", {"Input.Status": "1", "Input.ActualReturnDate": P.d(1) + "T08:00", "Input.EquipmentUnitId": str(cu["w"]["unit"])}, name="edit devolver otra unidad", follow=False)
    check(R, "Edit: al marcar Devuelto solo debe liberarse la unidad de ESTA salida (no la de otra salida enviada en el POST)", unidad(cu["w"]["unit"])["status"] == 10, P.dump(r) + f" unidad ajena={unidad(cu['w']['unit'])['status']} unidad propia={unidad(uC)['status']}")
    check(R, "Edit: Devuelto con fecha real libera la unidad de ESA salida (Operativa)", dp(dC)["status"] == 1 and unidad(uC)["status"] == 0, f"salida={dp(dC)['status']} unidad propia={unidad(uC)['status']}")
    uE, dE = nueva_salida("see")
    r = post_form(c, f"/Departures/Edit?id={dE}", {"Input.Status": "1", "Input.ActualReturnDate": P.d(1) + "T08:00"}, name="edit devolver ok", follow=True)
    check(R, "Edit: Devuelto + fecha real valida -> exito y unidad Operativa", r.status == 302 and unidad(uE)["status"] == 0 and dp(dE)["status"] == 1, P.dump(r) + f" unidad={unidad(uE)['status']}")
    st_dev, un_dev = dp(dE)["status"], unidad(uE)["status"]
    r = post_form(c, f"/Departures/Edit?id={dE}", {"Input.Status": "0", "Input.ActualReturnDate": ""}, name="edit reabrir devuelta", follow=True)
    check(R, "Edit: reabrir (Activa) una salida ya devuelta se bloquea (D-4: 302 a /Departures, warning 'ya fue devuelta y no se puede reabrir', sigue Devuelta y la unidad no cambia)",
          r.status == 302 and "/Departures" in (r.location or "") and r.has_toast("ya fue devuelta y no se puede reabrir", "warning")
          and dp(dE)["status"] == st_dev and unidad(uE)["status"] == un_dev,
          P.dump(r) + f" salida={dp(dE)['status']} (antes {st_dev}) unidad={unidad(uE)['status']} (antes {un_dev})")
    # edicion valida
    d2 = plan(cu["w"]["plan"])["dep"]
    r = post_form(c, f"/Departures/Edit?id={d2}", {"Input.DepartureObservations": "Observacion editada QAP", "Input.EstimatedReturnDate": P.d(20)}, name="edit valido", follow=True)
    check(R, "Edit: cambio valido -> 302 a Departures con 'Salida #id actualizada correctamente.'", r.status == 302 and r.has_toast(f"Salida #{d2} actualizada correctamente.", "success"), P.dump(r))
    a1 = post_form(c, f"/Departures/Edit?id={d2}", {"Input.DepartureObservations": "x"}, name="doble clic 1", follow=False)
    a2 = post_form(c, f"/Departures/Edit?id={d2}", {"Input.DepartureObservations": "x"}, name="doble clic 2", follow=False)
    check(R, "Edit: doble envio idempotente (ambos 302)", a1.status == 302 and a2.status == 302, f"{a1.status},{a2.status}")
    # cancelar por Edit
    r = post_form(c, f"/Departures/Edit?id={d2}", {"Input.Status": "99"}, name="edit cancelar", follow=False)
    check(R, "Edit: cancelar la salida (99) por Edit debe devolver la unidad a Operativa o impedirlo", r.status == 200 or unidad(cu["w"]["unit"])["status"] != 10, P.dump(r) + f" salida={dp(d2) and dp(d2)['status']} unidad={unidad(cu['w']['unit'])['status']}")

    # ================================================================ acceso
    for pth in ("/Departures", q, cw, ep, f"/Departures/Details?id={d1}"):
        P.check_anon(R, "Departures", pth)
    P.check_anon(R, "Departures", q, post=True)
    P.tokenless(R, c, "Departures/MassCreate", q, [("IsWizard", "true"), ("LabId", str(lab)), ("ManagementId", str(P.MGMT)), ("DepartureDate", P.d(0)), ("EstimatedReturnDate", P.d(3)),
                                                  ("Rows[0].PlanId", str(pid("h"))), ("Rows[0].EquipmentUnitId", str(uid("h"))), ("Rows[0].Include", "true")])
    P.tokenless(R, c, "Departures/Edit", ep, [("Input.Id", str(d1)), ("Input.Status", "1")])
    check(R, "Sin token: el plan 'h' sigue en L-3", plan(pid("h"))["phase"] == 4)
    for usuario, rol in (("qa_sup", "Supervisor"), ("qa_cierre", "PracticeClosure")):
        cl = P.client(usuario, pw)
        st, loc = cl.probe("/Departures")
        check(R, f"Rol {rol}: listado /Departures denegado", st in (302, 403) and "/Login" not in (loc or ""), f"status={st} loc={loc}")
        r = post_raw(cl, q, [("IsWizard", "true"), ("LabId", lab), ("ManagementId", P.MGMT), ("DepartureDate", P.d(0)), ("EstimatedReturnDate", P.d(3)), ("Rows[0].PlanId", pid("h")), ("Rows[0].EquipmentUnitId", uid("h")), ("Rows[0].Include", "true")], "/", follow=False, name=f"masscreate como {rol}")
        check(R, f"Rol {rol}: POST MassCreate denegado y plan sin cambios", r.status in (302, 403) and plan(pid("h"))["phase"] == 4, P.dump(r))


if __name__ == "__main__":
    P.run_module("departures", run)
