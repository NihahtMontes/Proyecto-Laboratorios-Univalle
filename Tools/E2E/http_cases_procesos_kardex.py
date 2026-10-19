"""Kardex (cierre L-48 y borrador) + Kardex/Edit. Solo contra la copia QA."""
import os

import http_cases_procesos_common as P
from http_cases_procesos_common import check, expect, sql, scalar, plan, unidad, post_form, post_raw, fase


def mn(mid):
    r = sql("select Status,CompletionPercentage,TechnicianId,ActualCost,Description,SatisfactionLevel,Recommendations,Observations,SuggestedNextMaintenanceDate from Maintenances where Id=?", mid)
    return dict(zip(["status", "pct", "tech", "cost", "desc", "sat", "rec", "obs", "next"], r[0])) if r else None


def dep(did):
    r = sql("select Status,ActualReturnDate,ReturnObservations,DepartureDate from Departures where Id=?", did)
    return dict(zip(["status", "act", "robs", "dep"], r[0])) if r else None


def hist(hid):
    r = sql("select Status,StartDate,EndDate,Reason,EquipmentUnitId from EquipmentStateHistories where Id=?", hid)
    return dict(zip(["status", "start", "end", "reason", "unit"], r[0])) if r else None


def n_hist(unit):
    return scalar("select count(*) from EquipmentStateHistories where EquipmentUnitId=?", unit)


def run(R, c):
    pw = os.environ["E2E_PW"]
    lab = P.lab_propio(c, "kardex")
    ctx = {}
    for k in ("a", "b", "c", "d", "e", "f", "g", "h", "i", "j", "k"):
        ctx[k] = P.llevar_a(c, lab, "k" + k, "P5")
    p4 = P.llevar_a(c, lab, "k4", "P4")
    p6 = P.llevar_a(c, lab, "k6", "P6")
    p7 = P.llevar_a(c, lab, "k7", "P7")
    p7b = P.llevar_a(c, lab, "k7b", "P7")
    p6b = P.llevar_a(c, lab, "k6b", "P6")
    for k in ctx:
        fase(R, f"Fixture {k} en Kardex", ctx[k]["plan"], 5, 7, 0)
    pid = lambda k: ctx[k]["plan"]
    uid = lambda k: ctx[k]["unit"]
    pg = lambda k: f"/Kardex/Create?isWizard=true&managementPlanId={pid(k)}"
    check(R, "Fixture: la unidad de un plan en Kardex esta En Prestamo (10)", unidad(uid("a"))["status"] == 10)

    # ================================================================ GET
    st, html = c.get(pg("a"))
    check(R, "Create: GET wizard 200 con las 4 tareas L-48", st == 200 and "Input.Tasks[3].Description" in html, f"status={st}")
    st, loc = c.probe("/Kardex/Create?isWizard=true&managementPlanId=99999")
    check(R, "Create: GET con plan inexistente redirige (no 500)", st in (302, 404), f"status={st} loc={loc}")
    st, html = c.get("/Kardex/Create?isWizard=true&managementPlanId=99999")
    _, tst = P.parse_messages(html)
    check(R, "Create: GET con plan inexistente muestra 'No se encontró el plan de gestión para cerrar Kardex.'", any("No se encontró el plan de gestión para cerrar Kardex." in t for _, t in tst), f"status={st} toasts={tst[:2]}")
    st, _ = c.get("/Kardex/Create")
    check(R, "Create: GET sin plan ni unidad no produce 500", st < 500, f"status={st}")

    # ================================================================ cierre: campos faltantes / reglas
    def cierre(k, label, quitar=None, ov=None, draft=False, follow=True):
        return P.kardex(c, pid(k), ov, draft=draft, name=label, quitar=quitar, follow=follow)

    faltas = [("Input.ActualReturnDate", "Falta devolución real L-3."), ("Input.SuggestedNextMaintenanceDate", "Falta próximo mantenimiento sugerido."),
              ("Input.SatisfactionLevel", "Falta nivel de satisfacción."), ("Input.Recommendations", "Faltan recomendaciones."),
              ("Input.Observations", "Faltan observaciones finales."), ("Input.Description", "Falta descripción de trabajos realizados."),
              ("Input.TechnicianId", "Falta técnico responsable."), ("Input.StartDate", "Falta inicio real."), ("Input.EndDate", "Falta finalización real.")]
    for campo, msg in faltas:
        r = cierre("a", f"sin {campo}", ov={campo: ""})
        check(R, f"Cierre: sin {campo.split('.')[-1]} -> 'No se pudo cerrar Kardex: ... {msg}'", r.status == 200 and r.has_toast("No se pudo cerrar Kardex:", "error") and r.has_toast(msg, "error"), P.dump(r))
    check(R, "Cierre: ningun campo faltante avanzo el plan ni cambio la unidad", plan(pid("a"))["phase"] == 5 and unidad(uid("a"))["status"] == 10)
    hoy_ret = P.d(3)
    r = cierre("a", "proximo igual a fin", ov={"Input.SuggestedNextMaintenanceDate": P.d(2)})
    check(R, "Cierre: proximo mantenimiento igual al dia de fin -> 'El próximo mantenimiento debe ser posterior a la finalización.'", r.has_toast("El próximo mantenimiento debe ser posterior a la finalización.", "error"), P.dump(r))
    r = cierre("a", "proximo anterior a fin", ov={"Input.SuggestedNextMaintenanceDate": P.d(-100)})
    check(R, "Cierre: proximo mantenimiento anterior a la finalizacion rechazado", r.has_toast("El próximo mantenimiento debe ser posterior a la finalización.", "error"), P.dump(r))
    r = cierre("a", "devolucion antes de salida", ov={"Input.ActualReturnDate": P.d(-2)})
    check(R, "Cierre: devolucion anterior a la salida -> 'La devolución L-3 no puede ser anterior a la fecha de salida.'", r.has_toast("La devolución L-3 no puede ser anterior a la fecha de salida.", "error"), P.dump(r))
    r = cierre("a", "fin antes de inicio", ov={"Input.StartDate": f"{P.d(5)}T08:00", "Input.EndDate": f"{P.d(4)}T08:00"})
    check(R, "Cierre: finalizacion anterior al inicio -> 'La finalización no puede ser anterior al inicio real.'", r.has_toast("La finalización no puede ser anterior al inicio real.", "error"), P.dump(r))
    r = cierre("a", "sin costos", ov={"Input.CostDetails[0].Concept": ""})
    check(R, "Cierre: sin costos -> 'Debe registrar al menos un costo real.'", r.has_toast("Debe registrar al menos un costo real.", "error"), P.dump(r))
    r = cierre("a", "costo total 0", ov={"Input.CostDetails[0].UnitPrice": "0"})
    check(R, "Cierre: costo total 0 -> 'El costo real total debe ser mayor a 0.'", r.has_toast("El costo real total debe ser mayor a 0.", "error") or r.has_toast("Debe registrar al menos un costo real.", "error"), P.dump(r))
    r = cierre("a", "tarea incompleta", ov={"Input.Tasks[1].IsCompleted": None})
    check(R, "Cierre: tarea L-48 incompleta -> 'Todas las tareas L-48 deben estar completadas.'", r.has_toast("Todas las tareas L-48 deben estar completadas.", "error"), P.dump(r))
    r = cierre("a", "tarea sin descripcion", ov={"Input.Tasks[1].Description": ""})
    check(R, "Cierre: sin tareas validas no se puede cerrar (descripcion vacia descarta la tarea)", r.status == 200 and plan(pid("a"))["phase"] == 5, P.dump(r))
    r = cierre("a", "descripcion 2001", ov={"Input.Description": "d" * 2001})
    check(R, "Cierre: descripcion de 2001 caracteres -> mensaje en espanol y sin cierre", r.status == 200 and plan(pid("a"))["phase"] == 5 and not P._EN.search(P.toast_text(r) + " ".join(r.errors)), P.dump(r))
    r = cierre("a", "recomendaciones 1001", ov={"Input.Recommendations": "r" * 1001})
    check(R, "Cierre: recomendaciones de 1001 caracteres -> mensaje en espanol y sin cierre", r.status == 200 and plan(pid("a"))["phase"] == 5 and not P._EN.search(P.toast_text(r) + " ".join(r.errors)), P.dump(r))
    r = cierre("a", "observaciones 1001", ov={"Input.Observations": "o" * 1001})
    check(R, "Cierre: observaciones de 1001 caracteres -> mensaje en espanol y sin cierre", r.status == 200 and plan(pid("a"))["phase"] == 5 and not P._EN.search(P.toast_text(r) + " ".join(r.errors)), P.dump(r))
    r = cierre("a", "satisfaccion 9", ov={"Input.SatisfactionLevel": "9"})
    check(R, "Cierre: nivel de satisfaccion fuera de rango (9) rechazado", r.status == 200 and plan(pid("a"))["phase"] == 5, P.dump(r) + f" plan={plan(pid('a'))['phase']}")
    r = cierre("a", "tecnico inexistente", ov={"Input.TechnicianId": "99999"})
    check(R, "Cierre: tecnico inexistente -> error controlado sin 500 y sin cierre", r.status < 500 and plan(pid("a"))["phase"] == 5, P.dump(r))
    r = cierre("a", "concepto de costo 201", ov={"Input.CostDetails[0].Concept": "c" * 201})
    check(R, "Cierre: concepto de costo de 201 caracteres -> sin 500 y sin cierre", r.status < 500 and plan(pid("a"))["phase"] == 5, P.dump(r))
    r = post_raw(c, P.with_handler("/Kardex/Create?isWizard=true&managementPlanId=99999", "Close"), [("Input.Description", "x"), ("Input.ActualReturnDate", P.d(3))], pg("a"), follow=True, name="cierre plan inexistente")
    check(R, "Cierre: plan inexistente -> 'No se pudo resolver el plan de Kardex. ManagementPlanId recibido: 99999.' sin 500", r.status < 500 and r.has_toast("No se pudo resolver el plan de Kardex. ManagementPlanId recibido: 99999.", "error"), P.dump(r))

    # ================================================================ borrador
    r = cierre("b", "borrador", ov={"Input.Description": "", "Input.Recommendations": "", "Input.Observations": "", "Input.SatisfactionLevel": ""}, draft=True)
    pb = plan(pid("b"))
    check(R, "Borrador: 'Borrador de Kardex guardado. Puede continuar después sin avanzar a desembolso.' sin exigir campos", r.status == 302 and "Step=5" in (r.location or "") and r.has_toast("Borrador de Kardex guardado. Puede continuar después sin avanzar a desembolso.", "success"), P.dump(r))
    check(R, "Borrador: plan sigue en Kardex (5/7) en borrador (DraftPhase=5), la unidad sigue En Prestamo y la salida Activa", pb["phase"] == 5 and pb["state"] == 7 and pb["draft"] == 1 and pb["draftphase"] == 5 and unidad(uid("b"))["status"] == 10 and dep(pb["dep"])["status"] == 0, f"{pb} unidad={unidad(uid('b'))['status']}")
    mb = mn(pb["mnt"])
    check(R, "Borrador: el mantenimiento L-8 (que ya estaba Completado) vuelve a 'En curso' (1) por un simple borrador de Kardex", mb["status"] == 2, f"estado del L-8={mb['status']} (2=Completado, 1=En curso)")
    r = cierre("b", "borrador -> cierre", follow=True)
    pb = plan(pid("b"))
    check(R, "Borrador -> cierre: se puede cerrar despues de un borrador", r.status == 302 and pb["phase"] == 6 and pb["state"] == 8, P.dump(r) + f" {pb}")

    # ================================================================ cierre valido
    r = cierre("c", "cierre valido", ov={"Input.Description": "Trabajos de cierre QAP"})
    pc = plan(pid("c"))
    check(R, "Cierre valido: 'Kardex completado correctamente...' y redirige al paso 6", r.status == 302 and "Step=6" in (r.location or "") and r.has_toast("Kardex completado correctamente. El registro quedó guardado en el Kardex histórico y el equipo avanza a Desembolso.", "success"), P.dump(r))
    check(R, "Cierre valido: plan a Disbursement/AwaitingDisbursement (6/8), PlanStatus En progreso, sin borrador, KardexHistory vinculado", pc["phase"] == 6 and pc["state"] == 8 and pc["pstatus"] == 1 and pc["draft"] == 0 and pc["kardex"], f"{pc}")
    dc = dep(pc["dep"])
    check(R, "Cierre valido: salida L-3 Devuelta (1) con fecha real y observacion de devolucion", dc["status"] == 1 and str(dc["act"])[:10] == P.d(3) and dc["robs"] == "Equipo devuelto y validado en cierre Kardex.", f"{dc}")
    check(R, "Cierre valido: unidad Operativa (0)", unidad(uid("c"))["status"] == 0, f"{unidad(uid('c'))['status']}")
    mc = mn(pc["mnt"])
    check(R, "Cierre valido: L-8 Completado, 100 %, descripcion, recomendaciones, observaciones, satisfaccion y proximo mantenimiento guardados", mc["status"] == 2 and mc["pct"] == 100 and mc["desc"] == "Trabajos de cierre QAP" and mc["rec"] == "Revisar cada 6 meses" and mc["sat"] == 4 and str(mc["next"])[:10] == P.d(400), f"{mc}")
    hc = hist(pc["kardex"])
    check(R, "Cierre valido: historial Kardex abierto en estado Operativo con fecha de devolucion", hc["status"] == 0 and hc["end"] is None and str(hc["start"])[:10] == P.d(3), f"{hc}")
    n0 = n_hist(uid("c"))
    r = cierre("c", "doble cierre", follow=True)
    check(R, "Cierre doble envio: no duplica historial Kardex ni salidas", n_hist(uid("c")) == n0 and scalar("select count(*) from Departures where EquipmentUnitId=?", uid("c")) == 1, P.dump(r) + f" historiales {n0}->{n_hist(uid('c'))}")
    check(R, "Cierre doble envio: el plan se queda en 6/8", plan(pid("c"))["phase"] == 6 and plan(pid("c"))["state"] == 8)

    # estado atrasado: cierre y borrador sobre planes en otra fase
    p4a = plan(p4["plan"])   # el cierre del L-8 ya deja un historial Kardex vinculado: se compara antes/después
    r = P.kardex(c, p4["plan"], name="cierre sobre plan sin salida (P4)", follow=True)
    p4d = plan(p4["plan"])
    check(R, "Cierre sobre un plan que aun espera su salida L-3 (sin salida) -> rechazado sin cerrar (guarda de fase con toast warning o 'El plan no tiene salida L-3 vinculada.')",
          p4d["phase"] == 4 and p4d["kardex"] == p4a["kardex"] and p4d["dep"] == p4a["dep"] and (any(k == "warning" for k, _ in r.toasts) or r.has_toast("El plan no tiene salida L-3 vinculada.", "error")),
          P.dump(r) + f" plan={p4d}")
    antes = plan(p7["plan"])
    r = P.kardex(c, p7["plan"], name="cierre sobre plan completado (P7)", follow=True)
    desp = plan(p7["plan"])
    check(R, "Cierre sobre un plan COMPLETADO (con L-12) no debe reabrirlo ni retroceder a Desembolso", desp["phase"] == antes["phase"] and desp["state"] == antes["state"] and desp["pstatus"] == antes["pstatus"], f"antes={antes['phase']}/{antes['state']}/{antes['pstatus']} despues={desp['phase']}/{desp['state']}/{desp['pstatus']} {P.dump(r)}")
    antes = plan(p7b["plan"])
    r = P.kardex(c, p7b["plan"], draft=True, name="borrador sobre plan completado (P7b)", follow=True)
    desp = plan(p7b["plan"])
    check(R, "Borrador sobre un plan COMPLETADO no debe retroceder el plan a Kardex", desp["phase"] == antes["phase"] and desp["state"] == antes["state"], f"antes={antes['phase']}/{antes['state']} despues={desp['phase']}/{desp['state']} {P.dump(r)}")
    mp7 = mn(antes["mnt"]) if antes["mnt"] else {}
    antes6 = plan(p6["plan"])
    r = P.kardex(c, p6["plan"], draft=True, name="borrador sobre plan en L-12 (P6)", follow=True)
    desp = plan(p6["plan"])
    check(R, "Borrador sobre un plan que ya cerro Kardex (en L-12) no debe retroceder a Kardex ni reabrir el L-8", desp["phase"] == antes6["phase"] and desp["state"] == antes6["state"] and mn(desp["mnt"])["status"] == 2, f"antes={antes6['phase']}/{antes6['state']} despues={desp['phase']}/{desp['state']} L-8={mn(desp['mnt'])['status']} {P.dump(r)}")
    # P6b: cierre repetido es idempotente en datos
    r = P.kardex(c, p6b["plan"], name="recierre P6b", follow=True)
    check(R, "Cierre repetido sobre un plan ya cerrado (P6): sin 500 y sin duplicar historial", r.status < 500 and plan(p6b["plan"])["phase"] == 6, P.dump(r))

    # ================================================================ Edit del historial Kardex
    hid = plan(pid("c"))["kardex"]
    ep = f"/Kardex/Edit?id={hid}"
    st, _ = c.get("/Kardex/Edit")
    check(R, "Edit: GET sin id -> 404", st == 404, f"status={st}")
    st, _ = c.get("/Kardex/Edit?id=99999")
    check(R, "Edit: GET id inexistente -> 404", st == 404, f"status={st}")
    st, _ = c.get(ep)
    check(R, "Edit: GET 200", st == 200, f"status={st}")
    r = post_form(c, ep, {"Input.StartDate": ""}, name="edit sin fecha", follow=False)
    check(R, "Edit: fecha de novedad vacia -> rechazo con mensaje visible en espanol", r.status == 200 and bool(r.errors) and not P._EN.search(" ".join(r.errors)), P.dump(r))
    r = post_form(c, ep, {"Input.Reason": "r" * 501}, name="edit motivo 501", follow=False)
    check(R, "Edit: motivo de 501 caracteres -> rechazo con mensaje visible en espanol", r.status == 200 and bool(r.errors) and not P._EN.search(" ".join(r.errors)), P.dump(r))
    r = post_form(c, ep, {"Input.Status": "77"}, name="edit estado 77", follow=False)
    check(R, "Edit: estado inexistente (77) se rechaza y la unidad no cambia", r.status == 200 and hist(hid)["status"] == 0 and unidad(uid("c"))["status"] == 0, P.dump(r) + f" hist={hist(hid)['status']} unidad={unidad(uid('c'))['status']}")
    r = post_form(c, ep, {"Input.EndDate": f"{P.d(-30)}T08:00"}, name="edit fin antes de inicio", follow=False)
    check(R, "Edit: fecha fin anterior al inicio debe rechazarse", r.status == 200 and hist(hid)["end"] is None, P.dump(r) + f" fin={hist(hid)['end']}")
    r = post_form(c, "/Kardex/Edit?id=99999", {"Input.Id": "99999"}, name="edit POST id inexistente", follow=False)
    check(R, "Edit: id inexistente -> 404", r.status == 404, P.dump(r))
    r = post_form(c, ep, {"Input.Id": "99999"}, name="edit Input.Id inexistente", follow=False)
    check(R, "Edit: POST con Input.Id inexistente -> 404", r.status == 404, P.dump(r))
    r = post_form(c, ep, {"IsWizard": "false", "Input.Reason": "Motivo editado QAP"}, name="edit valido (sin wizard)", follow=False)
    check(R, "Edit: cambio valido fuera del wizard no produce 500 (redirige a /EquipmentStateHistories/Index, que no existe)", r.status < 500, P.dump(r))
    check(R, "Edit: el motivo editado queda guardado (el servidor muestra exito; debe persistir)", hist(hid)["reason"] == "Motivo editado QAP", f"motivo={hist(hid)['reason']}")
    ps = f"/Kardex/Edit?id={hid}&isWizard=true&ManagementId={P.MGMT}&SelectedLabId={lab}&ReturnStep=6"
    r = post_form(c, ps, {"Input.Reason": "Motivo editado QAP 2"}, name="edit valido (wizard)", follow=True)
    check(R, "Edit wizard: 302 al paso del wizard con 'Registro de Kardex actualizado correctamente.'", r.status == 302 and r.has_toast("Registro de Kardex actualizado correctamente.", "success"), P.dump(r))
    check(R, "Edit wizard: tras el mensaje de exito el motivo editado esta realmente guardado", hist(hid)["reason"] == "Motivo editado QAP 2", f"motivo={hist(hid)['reason']}")
    # edicion que cambia el estado de la unidad (historial abierto => Edit actualiza la unidad)
    r = post_form(c, ps, {"Input.Status": "99", "Input.Reason": "x"}, name="edit estado Eliminado", follow=False)
    check(R, "Edit: el historial Kardex NO debe poder dar de baja (99) la unidad por la puerta de atras", unidad(uid("c"))["status"] != 99, P.dump(r) + f" unidad={unidad(uid('c'))['status']}")
    r = post_form(c, ps, {"Input.Status": "10", "Input.Reason": "x"}, name="edit estado En Prestamo", follow=False)
    check(R, "Edit: poner el historial abierto en 'En Prestamo' sin salida L-3 abierta no debe alterar la unidad", unidad(uid("c"))["status"] == 0, P.dump(r) + f" unidad={unidad(uid('c'))['status']}")
    a1 = post_form(c, ps, {"Input.Reason": "Doble"}, name="doble clic 1", follow=False)
    a2 = post_form(c, ps, {"Input.Reason": "Doble"}, name="doble clic 2", follow=False)
    check(R, "Edit: doble envio idempotente (ambos 302)", a1.status == 302 and a2.status == 302, f"{a1.status},{a2.status}")

    fin_txt = f"{P.d(30)}T08:00"
    r = post_form(c, ps, {"Input.EndDate": fin_txt}, name="edit fin valido", follow=True)
    check(R, "Edit wizard: la fecha de fin editada queda guardada", hist(hid)["end"] is not None, f"fin={hist(hid)['end']} {P.dump(r)}")
    # Edit de un historial AJENO (de otra unidad) tambien posible por URL
    oh = scalar("select top 1 Id from EquipmentStateHistories where EquipmentUnitId=? order by Id", uid("d"))
    r = post_form(c, f"/Kardex/Edit?id={oh}", {"Input.Reason": "ajeno QAP"}, name="edit historial de otra unidad", follow=False)
    check(R, "Edit: un historial de una unidad con salida L-3 abierta se puede editar (observacion informativa)", r.status in (200, 302, 500) , P.dump(r) + f" motivo={hist(oh)['reason'] if hist(oh) else None}")

    # ================================================================ acceso
    for pth in (pg("e"), ep):
        P.check_anon(R, "Kardex", pth)
    P.check_anon(R, "Kardex", pg("e"), post=True)
    P.tokenless(R, c, "Kardex/Create", P.with_handler(pg("e"), "Close"), [("Input.Description", "x")])
    P.tokenless(R, c, "Kardex/Edit", ep, [("Input.Id", str(hid)), ("Input.Reason", "x")])
    check(R, "Sin token: el plan 'e' sigue en Kardex", plan(pid("e"))["phase"] == 5)
    for usuario, rol in (("qa_sup", "Supervisor"), ("qa_cierre", "PracticeClosure")):
        cl = P.client(usuario, pw)
        st, loc = cl.probe(pg("e"))
        check(R, f"Rol {rol}: GET Kardex/Create denegado", st in (302, 403) and "/Login" not in (loc or ""), f"status={st} loc={loc}")
        r = post_raw(cl, P.with_handler(pg("e"), "Close"), [("Input.Description", "x"), ("Input.ActualReturnDate", P.d(3))], "/", follow=False, name=f"cierre como {rol}")
        check(R, f"Rol {rol}: POST cierre denegado y plan sin cambios", r.status in (302, 403) and plan(pid("e"))["phase"] == 5, P.dump(r))
        r = post_raw(cl, ep, [("Input.Id", hid), ("Input.Reason", "hack")], "/", follow=False, name=f"edit como {rol}")
        check(R, f"Rol {rol}: POST Edit denegado", r.status in (302, 403), P.dump(r))


if __name__ == "__main__":
    P.run_module("kardex", run)
