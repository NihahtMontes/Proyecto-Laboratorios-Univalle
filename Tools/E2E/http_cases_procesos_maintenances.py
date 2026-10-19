"""Mantenimientos L-8: Create / Edit / Delete / Details / Index / Plan. Solo contra la copia QA."""
import os

import http_cases_procesos_common as P
from http_cases_procesos_common import check, expect, sql, scalar, plan, unidad, post_form, post_raw, fase


def mn(mid):
    r = sql("select Status,CompletionPercentage,TechnicianId,ActualCost,EquipmentUnitId,RequestId,Description,SatisfactionLevel,MaintenanceType from Maintenances where Id=?", mid)
    return dict(zip(["status", "pct", "tech", "cost", "unit", "req", "desc", "sat", "type"], r[0])) if r else None


def n_mn(unit):
    return scalar("select count(*) from Maintenances where EquipmentUnitId=?", unit)


def costs(mid):
    return sql("select Concept,Quantity,UnitPrice,MaintenanceId,RequestId from CostDetails where MaintenanceId=? and IsVoided=0 order by Id", mid)


def sin_tareas():
    return {f"Input.Tasks[{i}].IsCompleted": None for i in range(4)}


def run(R, c):
    pw = os.environ["E2E_PW"]
    lab = P.lab_propio(c, "mantenimientos")
    names = {}
    ctx = {}
    for k in ("a", "b", "c", "d", "e", "f", "g", "h", "i", "j", "k", "l"):
        ctx[k] = P.llevar_a(c, lab, "m" + k, "P3")
    p2 = P.llevar_a(c, lab, "m2", "P2")
    p1ok = P.llevar_a(c, lab, "m1ok", "P1OK")
    p7 = P.llevar_a(c, lab, "m7", "P7")
    p6 = P.llevar_a(c, lab, "m6", "P6")
    for k in ctx:
        fase(R, f"Fixture {k} en L-8", ctx[k]["plan"], 3, 4, 0)
    pid = lambda k: ctx[k]["plan"]
    uid = lambda k: ctx[k]["unit"]

    # ================================================================ Create
    st, html = c.get(f"/Maintenances/Create?isWizard=true&managementPlanId={pid('a')}")
    check(R, "Create: GET wizard 200", st == 200, f"status={st}")
    r = P.l8(c, pid("a"), {"Input.TechnicianId": ""}, name="sin tecnico", follow=True)
    check(R, "Create (wizard): sin tecnico -> 'El técnico responsable es obligatorio en el flujo de mantenimiento...'", r.status == 200 and r.has_toast("El técnico responsable es obligatorio en el flujo de mantenimiento. Asígnelo para que L-3 pueda derivar el responsable de la salida.", "error"), P.dump(r))
    r = P.l8(c, pid("a"), {"Input.CompletionPercentage": "50", **{f"Input.Tasks[{i}].IsCompleted": None for i in (2, 3)}}, name="Completado al 50%", follow=True)
    check(R, "Create: Completado con 50 % de avance -> 'el avance debe estar al 100%' como error de campo o toast",
          r.status == 200 and (r.has_error("el avance debe estar al 100%") or r.has_toast("el avance debe estar al 100%")), P.dump(r))
    r = P.l8(c, pid("a"), {"Input.CompletionPercentage": "100", **sin_tareas()}, name="Completado 100 manipulado sin tareas", follow=True)
    check(R, "Create: 'Completado' con avance=100 enviado pero 0 tareas marcadas debe rechazarse (el servidor recalcula el avance DESPUES de validarlo)", r.status == 200 and plan(pid("a"))["state"] == 4, P.dump(r) + f" plan={plan(pid('a'))['state']} mnt={plan(pid('a'))['mnt']}")
    if plan(pid("a"))["mnt"]:   # el defecto dejo un L-8 'Completado' al 0 %; se anota
        m = mn(plan(pid("a"))["mnt"])
        check(R, "Create: el L-8 guardado no puede quedar 'Completado' con avance real 0 %", not (m["status"] == 2 and m["pct"] == 0), f"{m}")
    # ya avanzo por el defecto -> usar otro plan para las demas validaciones
    r = P.l8(c, pid("b"), {"Input.StartDate": f"{P.d(3)}T08:00", "Input.EndDate": f"{P.d(2)}T08:00"}, name="fin antes de inicio", follow=True)
    expect(R, "Create: fecha fin anterior al inicio", r, status=200, toast="La fecha de finalización no puede ser anterior al inicio.", toast_kind="error")
    r = P.l8(c, pid("b"), {"Input.TechnicianId": "99999"}, name="tecnico inexistente", follow=True)
    check(R, "Create: tecnico inexistente -> error controlado (sin 500) y el plan no avanza", r.status < 500 and r.status == 200 and bool(r.toasts) and plan(pid("b"))["state"] == 4, P.dump(r))
    r = P.l8(c, pid("b"), {"Input.CostDetails[0].Concept": "c" * 201}, name="concepto de costo 201", follow=True)
    check(R, "Create: concepto de costo de 201 caracteres -> mensaje especifico (maximo 200), sin avanzar", r.status == 200 and plan(pid("b"))["state"] == 4 and r.has_toast("200", "error"), P.dump(r))
    r = P.l8(c, pid("b"), {"Input.Description": "d" * 2001}, name="descripcion 2001", follow=True)
    check(R, "Create: descripcion de 2001 caracteres -> mensaje especifico (maximo 2000), sin avanzar", r.status == 200 and plan(pid("b"))["state"] == 4 and r.has_toast("2000", "error"), P.dump(r))
    r = P.l8(c, pid("b"), {"Input.SatisfactionLevel": "9"}, name="satisfaccion 9", follow=True)
    check(R, "Create: nivel de satisfaccion fuera de rango (9) se rechaza", r.status == 200 and plan(pid("b"))["state"] == 4, P.dump(r) + f" plan={plan(pid('b'))['state']}")
    r = P.l8(c, 99999, name="plan inexistente", follow=True)
    check(R, "Create: plan inexistente sin 500", r.status < 500, P.dump(r))
    # unidad en prestamo: por flujo real (llevar_a hasta la salida L-3 activa); poner 'En Prestamo' a mano esta bloqueado
    loan = P.llevar_a(c, lab, "mloan", "P5")
    u_loan = loan["unit"]
    check(R, "Fixture: unidad en Prestamo por flujo real (salida L-3 activa; ya no se puede poner En Prestamo a mano)", unidad(u_loan)["status"] == 10, f"unidad={unidad(u_loan)['status']}")
    n_loan = n_mn(u_loan)
    r = post_form(c, "/Maintenances/Create", {"Input.FacultyId": str(P.FAC), "Input.LaboratoryId": str(lab), "Input.EquipmentUnitId": str(u_loan),
                                              "Input.MaintenanceType": "1", "Input.TechnicianId": str(P.TECH_INT), "Input.Status": "2",
                                              "Input.Description": "Intento sobre unidad en prestamo QAP", "Input.CompletionPercentage": "100",
                                              "Input.StartDate": f"{P.d(1)}T08:00", "Input.EndDate": f"{P.d(2)}T17:00", **P.TAREAS_OK},
                  name="L-8 sin plan sobre unidad OnLoan", follow=True)
    expect(R, "Create: unidad en prestamo (OnLoan)", r, status=200, toast="No se puede realizar mantenimiento a un equipo que actualmente está en préstamo.", toast_kind="error")
    check(R, "Create OnLoan: el plan no avanzo, no se creo mantenimiento y la unidad sigue OnLoan",
          plan(loan["plan"])["phase"] == 5 and unidad(u_loan)["status"] == 10 and n_mn(u_loan) == n_loan,
          f"plan={plan(loan['plan'])['phase']} unidad={unidad(u_loan)['status']} mnt={n_loan}->{n_mn(u_loan)}")
    # unidad sin laboratorio
    post_form(c, f"/EquipmentUnits/Edit?id={uid('d')}", {"EquipmentUnit.LaboratoryId": ""}, name="quitar laboratorio d")
    r = P.l8(c, pid("d"), name="unidad sin laboratorio", follow=True)
    check(R, "Create: unidad sin laboratorio -> 'La unidad física no tiene laboratorio asignado...'", r.status == 200 and r.has_toast("no tiene laboratorio asignado", "error"), P.dump(r))
    post_form(c, f"/EquipmentUnits/Edit?id={uid('d')}", {"EquipmentUnit.LaboratoryId": str(lab)}, name="restaurar laboratorio d")
    check(R, "Create: las validaciones fallidas no dejaron mantenimientos huerfanos en 'b', la unidad en prestamo ni 'd' ('c' ya no se pone OnLoan a mano)",
          n_mn(uid("b")) == 0 and n_mn(u_loan) == n_loan and n_mn(uid("d")) == 0,
          f"b={n_mn(uid('b'))} prestamo={n_loan}->{n_mn(u_loan)} d={n_mn(uid('d'))}")

    # borrador
    r = P.l8(c, pid("e"), {"Input.TechnicianId": "", "Input.Description": "", "Input.Status": "1"}, draft=True, name="borrador", follow=True)
    pe = plan(pid("e"))
    check(R, "Create borrador: 'Borrador de mantenimiento para ... guardado correctamente.' sin exigir tecnico", r.status == 302 and "Step=3" in (r.location or "") and r.has_toast("Borrador de mantenimiento para", "success"), P.dump(r))
    check(R, "Create borrador: plan sigue en L-8 con borrador (IsDraft, DraftPhase=3), mantenimiento InProgress, la unidad NO cambia", pe["phase"] == 3 and pe["draft"] == 1 and pe["draftphase"] == 3 and mn(pe["mnt"])["status"] == 1 and unidad(uid("e"))["status"] == 0, f"{pe} {mn(pe['mnt']) if pe['mnt'] else None}")
    r = P.l8(c, pid("e"), {"Input.Status": "1"}, name="borrador -> en curso", follow=True)
    pe = plan(pid("e"))
    check(R, "Create: borrador -> guardado definitivo En curso: reutiliza el mantenimiento (1 solo)", n_mn(uid("e")) == 1 and r.status == 302, P.dump(r) + f" n={n_mn(uid('e'))}")
    check(R, "Create estado En curso (no Completado): la unidad pasa a UnderMaintenance (1) y el plan NO deberia avanzar a L-3 todavia", unidad(uid("e"))["status"] == 1 and pe["phase"] == 3, f"unidad={unidad(uid('e'))['status']} plan={pe['phase']}/{pe['state']}")

    # registro valido Completado con costos (2 lineas + linea con cantidad/precio negativos)
    ov = {"Input.CostDetails[0].Quantity": "2", "Input.CostDetails[0].UnitPrice": "50",
          "Input.CostDetails[1].Id": "0", "Input.CostDetails[1].Concept": "Mano de obra QAP", "Input.CostDetails[1].Category": "2", "Input.CostDetails[1].Quantity": "1", "Input.CostDetails[1].UnitPrice": "150",
          "Input.CostDetails[2].Id": "0", "Input.CostDetails[2].Concept": "Linea negativa QAP", "Input.CostDetails[2].Category": "0", "Input.CostDetails[2].Quantity": "-3", "Input.CostDetails[2].UnitPrice": "-10",
          "Input.CostDetails[3].Id": "0", "Input.CostDetails[3].Concept": "   ", "Input.CostDetails[3].Quantity": "5", "Input.CostDetails[3].UnitPrice": "99"}
    nom_eq = scalar("select Name from Equipments where Id=?", P.EQUIP)
    # H-009 (aprobado 2026-10-18): la línea con cantidad/precio negativos ya NO se normaliza en silencio; se rechaza con mensaje.
    r = P.l8(c, pid("f"), ov, name="Completado con linea negativa", follow=True)
    check(R, "Create: línea de costo con cantidad/precio negativos se rechaza con mensaje en español y el plan no avanza (H-009)",
          r.status == 200 and "cantidad debe ser un número" in P.toast_text(r) and plan(pid("f"))["phase"] == 3, P.dump(r))
    ov.update({"Input.CostDetails[2].Concept": "Linea sin costo QAP", "Input.CostDetails[2].Quantity": "1", "Input.CostDetails[2].UnitPrice": "0"})
    r = P.l8(c, pid("f"), ov, name="valido Completado", follow=True)
    pf = plan(pid("f"))
    check(R, "Create: Completado 100 % -> 'Mantenimiento para ... guardado correctamente.' y paso 4", r.status == 302 and "Step=4" in (r.location or "") and r.has_toast(f"Mantenimiento para '{nom_eq}' guardado correctamente.", "success"), P.dump(r))
    check(R, "Create valido: plan a Exit/AwaitingDeparture (4/6) sin borrador", pf["phase"] == 4 and pf["state"] == 6 and pf["draft"] == 0 and pf["mnt"], f"{pf}")
    m = mn(pf["mnt"]) if pf["mnt"] else {}
    check(R, "Create valido: Completado, 100 %, tecnico interno, costo = 2x50 + 150 + 1x0 = 250", m.get("status") == 2 and m.get("pct") == 100 and m.get("tech") == P.TECH_INT and m.get("cost") == 250, f"{m}")
    cs = costs(pf["mnt"]) if pf["mnt"] else []
    check(R, "Create valido: la linea de concepto en blanco se descarta y la linea 1 x 0 se guarda tal cual", len(cs) == 3 and cs[2][1] == 1 and cs[2][2] == 0, f"{cs}")
    check(R, "Create valido: la unidad queda Operativa y vinculada a la L-7 del plan", unidad(uid("f"))["status"] == 0 and m.get("req") == plan(pid("f"))["req"], f"unidad={unidad(uid('f'))['status']} req={m.get('req')}")
    check(R, "Create valido: el L-8 completado crea el historial 'Cierre automatico L-8' abierto", scalar("select count(*) from EquipmentStateHistories where EquipmentUnitId=? and Reason like 'Cierre autom%' and EndDate is null", uid("f")) == 1)
    # doble envio sobre plan ya en L-3
    r = P.l8(c, pid("f"), ov, name="doble envio", follow=True)
    check(R, "Create doble envio: no duplica el mantenimiento y el plan se queda en L-3", n_mn(uid("f")) == 1 and plan(pid("f"))["phase"] == 4, P.dump(r) + f" n={n_mn(uid('f'))} {plan(pid('f'))['phase']}")
    sc = scalar("select count(*) from CostDetails where MaintenanceId=?", plan(pid("f"))["mnt"])
    check(R, "Create doble envio: no duplica las lineas de costo", sc == 3, f"lineas={sc}")
    # tipo de mantenimiento: gestion preventiva => Preventivo (1)
    check(R, "Create wizard: el tipo de mantenimiento lo fija el servidor segun la gestion (Preventivo=1)", mn(plan(pid("f"))["mnt"])["type"] == 1)

    # estado no completado => no deberia avanzar
    r = P.l8(c, pid("g"), {"Input.Status": "0"}, name="estado Pendiente", follow=True)
    pg_ = plan(pid("g"))
    check(R, "Create: estado 'Pendiente' (no Completado) NO debe avanzar el plan a L-3", pg_["phase"] == 3, f"plan={pg_['phase']}/{pg_['state']} {P.dump(r)}")
    # tecnico externo
    r = P.l8(c, pid("h"), name="tecnico externo", tech=P.TECH_EXT, follow=True)
    check(R, "Create: tecnico externo valido", r.status == 302 and mn(plan(pid("h"))["mnt"])["tech"] == P.TECH_EXT, P.dump(r))

    # fases del plan
    antes = plan(p2["plan"])
    r = P.l8(c, p2["plan"], name="L-8 sobre plan sin L-7 (P2)", follow=True)
    desp = plan(p2["plan"])
    check(R, "Create: L-8 sobre un plan que aun espera su L-7 debe rechazarse (saltaria L-7)", desp["phase"] == antes["phase"] and desp["state"] == antes["state"], f"antes={antes['phase']}/{antes['state']} despues={desp['phase']}/{desp['state']} {P.dump(r)}")
    antes = plan(p1ok["plan"])
    r = P.l8(c, p1ok["plan"], name="L-8 sobre plan sano (P1OK)", follow=True)
    desp = plan(p1ok["plan"])
    check(R, "Create: L-8 sobre un plan sano (L-6 sin fallas) debe rechazarse", desp["phase"] == antes["phase"] and desp["state"] == antes["state"], f"antes={antes['phase']}/{antes['state']} despues={desp['phase']}/{desp['state']} {P.dump(r)}")
    antes = plan(p6["plan"])
    r = P.l8(c, p6["plan"], name="L-8 sobre plan en L-12 (P6)", follow=True)
    desp = plan(p6["plan"])
    check(R, "Create: L-8 sobre un plan que ya esta en L-12 no debe retroceder la fase", desp["phase"] == antes["phase"] and desp["state"] == antes["state"], f"antes={antes['phase']}/{antes['state']} despues={desp['phase']}/{desp['state']} {P.dump(r)}")
    antes = plan(p7["plan"])
    r = P.l8(c, p7["plan"], name="L-8 sobre plan completado (P7)", follow=True)
    desp = plan(p7["plan"])
    check(R, "Create: L-8 sobre un plan COMPLETADO no debe reabrirlo", desp["phase"] == antes["phase"] and desp["state"] == antes["state"] and desp["pstatus"] == antes["pstatus"], f"antes={antes['phase']}/{antes['state']}/{antes['pstatus']} despues={desp['phase']}/{desp['state']}/{desp['pstatus']} {P.dump(r)}")
    # ruta no wizard (managementPlanId sin isWizard): el tecnico deja de ser obligatorio
    r = P.l8(c, pid("i"), {"Input.TechnicianId": ""}, name="sin tecnico fuera del wizard", wizard=False, follow=True)
    pi_ = plan(pid("i"))
    check(R, "Create: omitir isWizard en la URL no debe saltarse la obligacion de tecnico", r.status == 200 and pi_["phase"] == 3, f"plan={pi_['phase']}/{pi_['state']} {P.dump(r)}")

    # ================================================================ Details / Index / Plan
    mid_f = plan(pid("f"))["mnt"]
    st, pg = c.get(f"/Maintenances/Details?id={mid_f}")
    check(R, "Details: 200 con costo 'Bs 250.00'", st == 200 and "250" in pg, f"status={st}")
    for pth in ("/Maintenances/Details?id=99999", "/Maintenances/Details", "/Maintenances/Details?id=abc"):
        st, loc = c.probe(pth)
        check(R, f"Details {pth}: sin 500", st < 500, f"status={st} loc={loc}")
    for pth in ("/Maintenances", f"/Maintenances?SearchTerm={P.PREFIX}", f"/Maintenances?SelectedLaboratoryId={lab}", "/Maintenances?StatusFilter=2", "/Maintenances?StatusFilter=99", "/Maintenances?pageIndex=999",
                "/Maintenances/Plan", "/Maintenances/Plan?SearchTerm=ZZZ_NO_EXISTE", f"/Maintenances/Plan?ManagementId={P.MGMT}", "/Maintenances/Plan?MaintenanceType=1"):
        st, pg = c.get(pth)
        check(R, f"GET {pth}: 200 sin error", st == 200, f"status={st}")
    st, pg = c.get(f"/Maintenances?SearchTerm={P.PREFIX}")
    check(R, "Index: la busqueda por prefijo QAP encuentra los mantenimientos creados", ctx["f"]["inv"] in pg, "")

    # ================================================================ Edit
    st, _ = c.get("/Maintenances/Edit")
    check(R, "Edit: GET sin id -> 404", st == 404, f"status={st}")
    st, _ = c.get("/Maintenances/Edit?id=99999")
    check(R, "Edit: GET id inexistente -> 404", st == 404, f"status={st}")
    ep = f"/Maintenances/Edit?id={mid_f}"
    st, _ = c.get(ep)
    check(R, "Edit: GET 200", st == 200, f"status={st}")
    OKB = {"Input.Recommendations": "Recomendacion base QAP", "Input.Observations": "Observacion base QAP", "Input.SatisfactionLevel": "4"}   # un L-8 de wizard aun no tiene estos campos
    snap = lambda: (mn(mid_f)["status"], mn(mid_f)["pct"], mn(mid_f)["desc"], mn(mid_f)["cost"], mn(mid_f)["unit"], mn(mid_f)["tech"])
    s0 = snap()

    def rechaza(label, ov, fragment=None):
        r = post_form(c, ep, {**OKB, **ov}, name=label, follow=False)
        check(R, f"Edit: {label} -> rechazado por el servidor (200, sin cambios)", r.status == 200 and snap() == s0, P.dump(r) + f" snap={snap()}")
        return r

    r = rechaza("descripcion vacia", {"Input.Description": ""})
    check(R, "Edit: el mensaje 'La descripción es obligatoria' se muestra al usuario (la vista solo tiene resumen ModelOnly)", r.has_error("La descripción es obligatoria"), P.dump(r))
    r = rechaza("fin antes de inicio", {"Input.StartDate": f"{P.d(5)}T08:00", "Input.EndDate": f"{P.d(4)}T08:00"})
    check(R, "Edit: el mensaje 'La fecha de finalización no puede ser anterior al inicio.' se muestra al usuario", r.has_error("La fecha de finalización no puede ser anterior"), P.dump(r))
    # H-010 (2026-10-18): «Completado» con costo 0 ahora se ACEPTA (Create y Edit); ya no es un caso de rechazo. Cubierto en http_cases_costos CO-05.
    r = rechaza("Completado sin satisfaccion", {"Input.SatisfactionLevel": ""})
    check(R, "Edit: el mensaje de satisfaccion obligatoria se muestra al usuario", r.has_error("Para estado Completado, la Evaluación de Satisfacción es obligatoria."), P.dump(r))
    r = rechaza("Completado sin recomendaciones", {"Input.Recommendations": ""})
    check(R, "Edit: el mensaje de recomendaciones obligatorias se muestra al usuario", r.has_error("Para estado Completado, las Recomendaciones Técnicas son obligatorias."), P.dump(r))
    r = rechaza("Completado sin observaciones", {"Input.Observations": ""})
    check(R, "Edit: el mensaje de observaciones obligatorias se muestra al usuario", r.has_error("Para estado Completado, las Observaciones Internas son obligatorias."), P.dump(r))
    rechaza("estado inexistente (77)", {"Input.Status": "77"})
    rechaza("descripcion de 2001 caracteres", {"Input.Description": "d" * 2001})
    r = post_form(c, ep, {**OKB, "Input.EquipmentUnitId": "99999"}, name="edit unidad inexistente", follow=False)
    check(R, "Edit: unidad inexistente no produce 500 y no cambia nada", r.status < 500 and snap() == s0, P.dump(r))
    r = post_form(c, ep, {**OKB, "Input.TechnicianId": "99999"}, name="edit tecnico inexistente", follow=False)
    check(R, "Edit: tecnico inexistente no produce 500 y no cambia nada", r.status < 500 and snap() == s0, P.dump(r))
    r = post_form(c, ep, {**OKB, "Input.Id": "99999"}, name="edit Input.Id inexistente", follow=False)
    check(R, "Edit: POST con Input.Id inexistente -> 404", r.status == 404, P.dump(r))
    # Completado sin avance 100 % en Edit (solo lo valida JS)
    r = post_form(c, ep, {**OKB, "Input.Tasks[2].IsCompleted": None, "Input.Tasks[3].IsCompleted": None}, name="edit Completado al 50%", follow=True)
    check(R, "Edit: 'Completado' con tareas al 50 % debe rechazarse (como en Create: 'el avance debe estar al 100%')", r.status == 200 and mn(mid_f)["pct"] == 100, P.dump(r) + f" pct={mn(mid_f)['pct']}")
    post_form(c, ep, {**OKB, "Input.Tasks[2].IsCompleted": "true", "Input.Tasks[3].IsCompleted": "true"}, name="edit restaurar tareas", follow=True)
    # costos negativos en Edit
    n_cs = len(costs(mid_f))
    r = post_form(c, ep, {**OKB, "Input.CostDetails[2].UnitPrice": "-500", "Input.CostDetails[2].Quantity": "-2"}, name="edit costos negativos", follow=True)
    cs2 = costs(mid_f)
    check(R, "Edit: cantidades/precios negativos se rechazan o normalizan igual que en Create (no se guardan con signo negativo)", all(q > 0 and pr >= 0 for _, q, pr, _, _ in cs2), f"{cs2}")
    # quitar una linea de costo
    cs_antes = costs(mid_f)
    r = post_form(c, ep, {**OKB, "Input.CostDetails[1].Concept": ""}, name="edit quitar linea de costo", follow=True)
    cs_desp = costs(mid_f)
    check(R, "Edit: quitar una linea de costo funciona (302 y una linea menos); la desvinculacion no debe violar CK_CostDetails_ExactlyOneParent",
          r.status == 302 and len(cs_desp) == len(cs_antes) - 1, P.dump(r) + f" lineas {len(cs_antes)}->{len(cs_desp)}")
    # edicion valida
    r = post_form(c, ep, {"Input.Description": "Trabajo editado QAP", "Input.Recommendations": "Recomendacion QAP", "Input.Observations": "Obs QAP", "Input.SatisfactionLevel": "5"}, name="edit valido", follow=True)
    check(R, "Edit: cambio valido -> 302 con 'Registro de mantenimiento de ... actualizado correctamente.'", r.status == 302 and r.has_toast(f"Registro de mantenimiento de '{nom_eq}' actualizado correctamente.", "success"), P.dump(r))
    check(R, "Edit valido: descripcion y satisfaccion guardadas", mn(mid_f)["desc"] == "Trabajo editado QAP" and mn(mid_f)["sat"] == 5, f"{mn(mid_f)}")
    a1 = post_form(c, ep, {"Input.Description": "Trabajo editado QAP"}, name="doble clic 1", follow=False)
    a2 = post_form(c, ep, {"Input.Description": "Trabajo editado QAP"}, name="doble clic 2", follow=False)
    check(R, "Edit: doble envio idempotente (ambos 302)", a1.status == 302 and a2.status == 302, f"{a1.status},{a2.status}")

    # ================================================================ Delete
    mid_e = plan(pid("e"))["mnt"]    # En curso (unidad UnderMaintenance)
    st, _ = c.get(f"/Maintenances/Delete?id={mid_e}")
    check(R, "Delete: GET 200", st == 200, f"status={st}")
    st, _ = c.get("/Maintenances/Delete?id=99999")
    check(R, "Delete: GET id inexistente -> 404", st == 404, f"status={st}")
    check(R, "Fixture Delete: unidad 'e' UnderMaintenance", unidad(uid("e"))["status"] == 1)
    r = P.post_raw(c, f"/Maintenances/Delete?id={mid_e}", [], f"/Maintenances/Delete?id={mid_e}", name="delete")
    expect(R, "Delete: anula el mantenimiento", r, status=302, toast=f"Mantenimiento de '{nom_eq}' eliminado del sistema.", toast_kind="success")
    check(R, "Delete: queda Cancelled (99)", scalar("select Status from Maintenances where Id=?", mid_e) == 99)
    pe = plan(pid("e"))
    check(R, "Delete: la baja logica libera al plan y a la unidad (unidad ya no 'En mantenimiento'; el plan deja de apuntar a un L-8 anulado)", unidad(uid("e"))["status"] != 1 and pe["mnt"] is None, f"unidad={unidad(uid('e'))['status']} plan mnt={pe['mnt']} fase={pe['phase']}/{pe['state']}")
    r = P.post_raw(c, f"/Maintenances/Delete?id={mid_e}", [], "/Maintenances", name="delete repetido")
    check(R, "Delete repetido: no repite el mensaje de exito", not r.has_toast("eliminado del sistema", "success"), P.dump(r))
    r = P.post_raw(c, "/Maintenances/Delete?id=99999", [], "/Maintenances", name="delete inexistente")
    check(R, "Delete: id inexistente -> 404 o aviso (no silencioso)", r.status == 404 or bool(r.toasts), P.dump(r))
    # delete de un L-8 que ya salio a L-3 (plan avanzado)
    mid_h = plan(pid("h"))["mnt"]
    r = P.post_raw(c, f"/Maintenances/Delete?id={mid_h}", [], "/Maintenances", name="delete mantenimiento de plan en L-3")
    check(R, "Delete: anular el L-8 de un plan que ya espera su salida L-3 -> debe bloquearse o recolocar el plan", plan(pid("h"))["mnt"] is not None and scalar("select Status from Maintenances where Id=?", mid_h) != 99 or plan(pid("h"))["phase"] == 3,
          P.dump(r) + f" plan={plan(pid('h'))['phase']}/{plan(pid('h'))['state']} mnt={plan(pid('h'))['mnt']} estado={scalar('select Status from Maintenances where Id=?', mid_h)}")

    # ================================================================ acceso
    for pth in ("/Maintenances", "/Maintenances/Plan", f"/Maintenances/Create?isWizard=true&managementPlanId={pid('j')}", f"/Maintenances/Edit?id={mid_f}", f"/Maintenances/Details?id={mid_f}", f"/Maintenances/Delete?id={mid_f}"):
        P.check_anon(R, "Maintenances", pth)
    P.check_anon(R, "Maintenances", f"/Maintenances/Create?isWizard=true&managementPlanId={pid('j')}", post=True)
    P.tokenless(R, c, "Maintenances/Create", f"/Maintenances/Create?isWizard=true&managementPlanId={pid('j')}", [("Input.TechnicianId", "1"), ("Input.Status", "2")])
    P.tokenless(R, c, "Maintenances/Edit", ep, [("Input.Id", str(mid_f)), ("Input.Description", "x")])
    P.tokenless(R, c, "Maintenances/Delete", f"/Maintenances/Delete?id={mid_f}", [])
    check(R, "Sin token: el plan 'j' sigue en L-8 y el mantenimiento 'f' intacto", plan(pid("j"))["phase"] == 3 and scalar("select Status from Maintenances where Id=?", mid_f) == 2)
    for usuario, rol in (("qa_sup", "Supervisor"), ("qa_cierre", "PracticeClosure")):
        cl = P.client(usuario, pw)
        st, loc = cl.probe("/Maintenances")
        check(R, f"Rol {rol}: listado /Maintenances denegado", st in (302, 403) and "/Login" not in (loc or ""), f"status={st} loc={loc}")
        r = post_raw(cl, f"/Maintenances/Create?isWizard=true&managementPlanId={pid('j')}", [("Input.TechnicianId", "1"), ("Input.Status", "2")], "/", follow=False, name=f"create como {rol}")
        check(R, f"Rol {rol}: POST Create denegado y plan sin cambios", r.status in (302, 403) and plan(pid("j"))["phase"] == 3, P.dump(r))
        r = post_raw(cl, f"/Maintenances/Delete?id={mid_f}", [], "/", follow=False, name=f"delete como {rol}")
        check(R, f"Rol {rol}: POST Delete denegado", r.status in (302, 403) and scalar("select Status from Maintenances where Id=?", mid_f) != 99, P.dump(r))


if __name__ == "__main__":
    P.run_module("maintenances", run)
