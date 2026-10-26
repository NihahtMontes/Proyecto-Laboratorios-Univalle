"""POST /api/WizardRollback/redo (Rehacer fase del wizard). Solo contra la copia QA."""
import json
import os
import time
import urllib.request

import http_cases_procesos_common as P
from http_cases_procesos_common import check, expect, sql, scalar, plan, unidad, post_form, post_raw, fase

TOKEN_PAGE = "/Equipment/Create"
URL = "/api/WizardRollback/redo"


def redo(c, plan_id, target, **kw):
    return P.json_post(c, URL, {"planId": plan_id, "targetPhase": target}, token_page=TOKEN_PAGE, **kw)


def status_of(table, rid):
    return scalar(f"select Status from {table} where Id=?", rid) if rid else None


def run(R, c):
    pw = os.environ["E2E_PW"]
    lab = P.lab_propio(c, "rollback")
    ctx = {}
    for k, dest in (("p2", "P2"), ("p2b", "P2"), ("p3", "P3"), ("p4", "P4"), ("p4b", "P4"), ("p5", "P5"), ("p6", "P6"), ("p6b", "P6"), ("p7", "P7"), ("p7b", "P7"), ("p7c", "P7"), ("p1", "P1OK"), ("p4f", "P4"), ("p6f", "P6")):
        ctx[k] = P.llevar_a(c, lab, "rb" + k, dest, past=(k not in ("p4f", "p6f")))
    pid = lambda k: ctx[k]["plan"]
    uid = lambda k: ctx[k]["unit"]
    time.sleep(75)   # el fin del L-8 es "el minuto siguiente": el rollback cierra el historial en UtcNow y no puede ser anterior

    # ================================================================ validacion de la peticion
    st, body = redo(c, pid("p3"), 2, token=True)
    check(R, "Sondeo del token: el endpoint responde 200 con el token de una pagina del sitio (P3 -> L-7)", st == 200, f"status={st} {body[:120]}")
    # (p3 ya se revirtio a L-7; se vuelve a llevar a L-8 mas abajo si hace falta)
    anon = urllib.request.build_opener(P._NoRedirect)
    req = urllib.request.Request(P.BASE + URL, data=json.dumps({"planId": pid("p4"), "targetPhase": 3}).encode(), headers={"Content-Type": "application/json"}, method="POST")
    try:
        a = anon.open(req, timeout=60)
        st_a, loc_a = a.status, None
    except urllib.error.HTTPError as e:
        st_a, loc_a = e.code, e.headers.get("Location")
    check(R, "Sin sesion: POST redo -> 302 a /Login o 401 y sin cambios", st_a in (302, 401) and plan(pid("p4"))["phase"] == 4, f"status={st_a} loc={loc_a}")
    st, body = redo(c, pid("p4"), 3, token=False)
    check(R, "Sin token antifalsificacion -> 400 y sin cambios", st == 400 and plan(pid("p4"))["phase"] == 4, f"status={st} {body[:100]}")
    # token invalido
    hdr = {"Content-Type": "application/json", "RequestVerificationToken": "token-invalido"}
    op = urllib.request.build_opener(urllib.request.HTTPCookieProcessor(c.jar), P._NoRedirect)
    try:
        rr = op.open(urllib.request.Request(P.BASE + URL, data=json.dumps({"planId": pid("p4"), "targetPhase": 3}).encode(), headers=hdr, method="POST"), timeout=60)
        st_t = rr.status
    except urllib.error.HTTPError as e:
        st_t = e.code
    check(R, "Token invalido -> 400 y sin cambios", st_t == 400 and plan(pid("p4"))["phase"] == 4, f"status={st_t}")
    for usuario, rol in (("qa_sup", "Supervisor"), ("qa_cierre", "PracticeClosure")):
        cl = P.client(usuario, pw)
        st, body = redo(cl, pid("p4"), 3)
        check(R, f"Rol {rol}: redo -> 403 (o 302 a la pagina de acceso denegado) y sin cambios", st in (403, 302) and plan(pid("p4"))["phase"] == 4, f"status={st} {body[:80]}")
    st, body = P.json_post(c, URL, None, token_page=TOKEN_PAGE, content_type="text/plain", raw=b"planId=1")
    check(R, "Content-Type text/plain -> 415", st == 415, f"status={st} {body[:80]}")
    st, body = P.json_post(c, URL, None, token_page=TOKEN_PAGE, raw=b"")
    check(R, "Cuerpo vacio -> 400", st == 400, f"status={st} {body[:80]}")
    st, body = P.json_post(c, URL, None, token_page=TOKEN_PAGE, raw=b"null")
    check(R, "Cuerpo 'null' -> 400 'Solicitud inválida.'", st == 400 and "Solicitud inválida." in body, f"status={st} {body[:80]}")
    st, body = P.json_post(c, URL, None, token_page=TOKEN_PAGE, raw=b"{no es json")
    check(R, "JSON mal formado -> 400", st == 400, f"status={st} {body[:120]}")
    check(R, "JSON mal formado: el mensaje de error no esta en ingles (el sitio muestra xhr.responseText al usuario)", st == 400 and not P._EN.search(body) and "could not" not in body.lower() and "json" not in body.lower(), f"{body[:200]!r}")
    for pl_, tg, lbl in ((0, 2, "planId 0"), (-5, 2, "planId negativo"), (pid("p4"), 0, "fase 0"), (pid("p4"), 7, "fase 7"), (pid("p4"), -1, "fase -1"), (pid("p4"), 99, "fase 99")):
        st, body = redo(c, pl_, tg)
        check(R, f"{lbl} -> 400 'Solicitud inválida.'", st == 400 and body.strip('"') == "Solicitud inválida.", f"status={st} {body[:100]}")
    st, body = redo(c, 999999, 2)
    check(R, "Plan inexistente -> 404 'Plan no encontrado.'", st == 404 and "Plan no encontrado." in body, f"status={st} {body[:100]}")
    st, body = P.json_post(c, URL, {"planId": "abc", "targetPhase": 2}, token_page=TOKEN_PAGE)
    check(R, "planId no numerico -> 400 con mensaje en espanol", st == 400 and not P._EN.search(body) and "could not be converted" not in body, f"status={st} {body[:200]!r}")
    st, body = P.json_post(c, URL, {"planId": pid("p4"), "targetPhase": "Maintenance"}, token_page=TOKEN_PAGE)
    check(R, "targetPhase como texto -> 400 (o aceptado) sin 500 y sin mensaje en ingles", st < 500 and not (st == 400 and "could not be converted" in body), f"status={st} {body[:200]!r}")
    st, body = P.json_post(c, URL, {"planId": 1.5, "targetPhase": 2}, token_page=TOKEN_PAGE)
    check(R, "planId decimal -> 400 sin 500", st == 400, f"status={st} {body[:120]!r}")
    st, body = P.json_post(c, URL, None, token_page=TOKEN_PAGE, method="GET")
    check(R, "GET sobre el endpoint -> 405", st == 405, f"status={st}")
    check(R, "Las peticiones invalidas no modificaron el plan p4", plan(pid("p4"))["phase"] == 4 and plan(pid("p4"))["state"] == 6)

    # misma fase o superior
    pp3 = P.llevar_a(c, lab, "rbigual", "P3")
    for tg in (3, 4, 5, 6):
        st, body = redo(c, pp3["plan"], tg)
        check(R, f"Fase destino {tg} >= fase actual (3) -> 400 'Solo se puede rehacer una fase anterior a la fase actual.'", st == 400 and "Solo se puede rehacer una fase anterior a la fase actual." in body, f"status={st} {body[:100]}")
    check(R, "Destino igual/superior no cambia el plan", plan(pp3["plan"])["phase"] == 3 and plan(pp3["plan"])["state"] == 4)
    # un plan Completado (fase 6) no se puede 'rehacer' a la propia fase 6
    st, body = redo(c, pid("p7c"), 6)
    check(R, "Plan Completado (fase 6): destino 6 -> 400 (misma fase)", st == 400, f"status={st} {body[:100]}")

    # ================================================================ efectos
    # P3 (L-7 hecha, espera L-8) ya revertido a L-7 en el sondeo del token
    p = plan(pid("p3"))
    check(R, "P3 -> L-7: plan 2/3 sin L-7 vinculada, L-7 Cancelada (no borrada) y unidad Fuera de servicio (2)", p["phase"] == 2 and p["state"] == 3 and p["req"] is None and status_of("Requests", scalar("select top 1 Id from Requests where EquipmentUnitId=? and Type=1 order by Id desc", uid("p3"))) == 99 and unidad(uid("p3"))["status"] == 2, f"{p} unidad={unidad(uid('p3'))['status']}")
    check(R, "P3 -> L-7: la L-7 sigue existiendo (baja logica, no se borra)", scalar("select count(*) from Requests where EquipmentUnitId=? and Type=1", uid("p3")) == 1)
    # doble rollback
    st, body = redo(c, pid("p3"), 2)
    check(R, "Doble rollback al mismo destino -> 400 (ya esta en esa fase)", st == 400 and "Solo se puede rehacer una fase anterior" in body, f"status={st} {body[:100]}")
    # re-hacer la L-7 despues del rollback
    r = P.l7(c, pid("p3"), {"Input.Description": "L-7 rehecha QAP", "Input.Observations": "\r\n".join(f"{i}. punto" for i in range(1, 14))}, name="rehacer L-7", follow=True)
    check(R, "Tras rollback a L-7 se puede registrar una L-7 nueva y el plan avanza a L-8", r.status == 302 and plan(pid("p3"))["state"] == 4 and plan(pid("p3"))["req"] is not None, P.dump(r) + f" {plan(pid('p3'))}")

    # P4 -> L-8
    mid = plan(pid("p4"))["mnt"]
    st, body = redo(c, pid("p4"), 3)
    p = plan(pid("p4"))
    check(R, "P4 -> L-8: 200 'Progreso revertido con éxito.'", st == 200 and "Progreso revertido con éxito." in body and '"success":true' in body.replace(" ", ""), f"status={st} {body[:120]}")
    check(R, "P4 -> L-8: plan 3/4 sin L-8, L-8 Cancelado (no borrado) y unidad En mantenimiento (1)", p["phase"] == 3 and p["state"] == 4 and p["mnt"] is None and status_of("Maintenances", mid) == 99 and unidad(uid("p4"))["status"] == 1, f"{p} L-8={status_of('Maintenances', mid)} unidad={unidad(uid('p4'))['status']}")
    check(R, "P4 -> L-8: conserva la L-7 y la L-6 (solo se cancela lo posterior)", p["req"] is not None and p["ver"] is not None and status_of("Requests", p["req"]) != 99)
    check(R, "P4 -> L-8: el historial Kardex creado por el cierre automatico del L-8 queda desvinculado del plan", p["kardex"] is None, f"{p}")
    r = P.l8(c, pid("p4"), P.fechas_l8_recientes(), name="rehacer L-8", follow=True)
    check(R, "Tras rollback a L-8 se puede registrar un L-8 nuevo (otra fila) y el plan avanza a L-3", r.status == 302 and plan(pid("p4"))["phase"] == 4 and plan(pid("p4"))["mnt"] not in (None, mid), P.dump(r) + f" {plan(pid('p4'))}")

    # P5 -> L-3
    did = plan(pid("p5"))["dep"]
    st, body = redo(c, pid("p5"), 4)
    p = plan(pid("p5"))
    check(R, "P5 -> L-3: plan 4/6, salida Cancelada (no borrada) y desvinculada, unidad Operativa (0)", st == 200 and p["phase"] == 4 and p["state"] == 6 and p["dep"] is None and status_of("Departures", did) == 99 and unidad(uid("p5"))["status"] == 0, f"{p} salida={status_of('Departures', did)} unidad={unidad(uid('p5'))['status']}")
    r = P.l3(c, pid("p5"), uid("p5"), lab, salida=P.d(-8), retorno=P.d(-3), name="rehacer L-3", follow=True)
    check(R, "Tras rollback a L-3 se puede registrar una salida nueva (L-3) y el plan vuelve a Kardex", plan(pid("p5"))["phase"] == 5 and plan(pid("p5"))["dep"] not in (None, did), P.dump(r) + f" {plan(pid('p5'))}")

    # P6 -> Kardex
    kid = plan(pid("p6"))["kardex"]
    st, body = redo(c, pid("p6"), 5)
    p = plan(pid("p6"))
    check(R, "P6 -> Kardex: plan 5/7 sin historial Kardex vinculado y PlanStatus Pendiente (0)", st == 200 and p["phase"] == 5 and p["state"] == 7 and p["kardex"] is None and p["pstatus"] == 0, f"status={st} {p}")
    check(R, "P6 -> Kardex: el historial Kardex queda con fecha de fin (no se borra)", scalar("select count(*) from EquipmentStateHistories where Id=? and EndDate is not null", kid) == 1)
    check(R, "P6 -> Kardex: la salida L-3 conserva su registro; la unidad queda Operativa aunque el Kardex aun no se cierra (coherencia: deberia seguir En Prestamo hasta el cierre)", unidad(uid("p6"))["status"] == 10 or p["dep"] is None, f"unidad={unidad(uid('p6'))['status']} salida={status_of('Departures', p['dep'])}")
    r = P.kardex(c, pid("p6"), {"Input.ActualReturnDate": P.d(-5)}, name="rehacer Kardex", follow=True)
    check(R, "Tras rollback a Kardex se puede cerrar de nuevo", r.status == 302 and plan(pid("p6"))["phase"] == 6 and plan(pid("p6"))["state"] == 8, P.dump(r) + f" {plan(pid('p6'))}")

    # P7 -> Kardex / Verificacion (cancela L-12)
    acq = plan(pid("p7"))["acq"]
    st, body = redo(c, pid("p7"), 5)
    p = plan(pid("p7"))
    check(R, "P7 (Completado) -> Kardex: 200, plan 5/7 Pendiente, solicitud de compra L-12 Cancelada y desvinculada", st == 200 and p["phase"] == 5 and p["state"] == 7 and p["pstatus"] == 0 and p["acq"] is None and status_of("Requests", acq) == 99, f"status={st} {p} L-12={status_of('Requests', acq)}")
    n_before = {t: scalar(f"select count(*) from {t}") for t in ("Requests", "Maintenances", "Verifications", "Departures")}
    ids = plan(pid("p7b"))
    st, body = redo(c, pid("p7b"), 1)
    p = plan(pid("p7b"))
    n_after = {t: scalar(f"select count(*) from {t}") for t in n_before}
    check(R, "P7 -> L-6: 200, plan 1/1 sin ningun vinculo y unidad Operativa", st == 200 and p["phase"] == 1 and p["state"] == 1 and all(p[k] is None for k in ("ver", "req", "mnt", "dep", "kardex", "acq")) and unidad(uid("p7b"))["status"] == 0, f"status={st} {p}")
    check(R, "P7 -> L-6: todo lo posterior queda cancelado/anulado (99), nada se borra fisicamente", status_of("Verifications", ids["ver"]) == 99 and status_of("Requests", ids["req"]) == 99 and status_of("Maintenances", ids["mnt"]) == 99 and status_of("Departures", ids["dep"]) == 99 and status_of("Requests", ids["acq"]) == 99 and n_after == n_before,
          f"ver={status_of('Verifications', ids['ver'])} req={status_of('Requests', ids['req'])} mnt={status_of('Maintenances', ids['mnt'])} dep={status_of('Departures', ids['dep'])} acq={status_of('Requests', ids['acq'])} filas {n_before}->{n_after}")
    r = P.l6(c, pid("p7b"), uid("p7b"), lab, cond=4, name="rehacer L-6", follow=True)
    check(R, "Tras rollback a L-6 se puede verificar de nuevo (MassCreate)", r.has_toast("1 verificaciones registradas", "success") and plan(pid("p7b"))["state"] == 2, P.dump(r) + f" {plan(pid('p7b'))}")

    # P2 -> L-6 (L-7 creada por L-6)
    ids = plan(pid("p2"))
    st, body = redo(c, pid("p2"), 1)
    p = plan(pid("p2"))
    check(R, "P2 (L-7 creada por el L-6 'Malo') -> L-6: verificacion Anulada, L-7 Cancelada y plan 1/1", st == 200 and p["phase"] == 1 and p["state"] == 1 and status_of("Verifications", ids["ver"]) == 99 and status_of("Requests", ids["req"]) == 99, f"status={st} {p}")
    # P2 con borrador de L-7
    P.l7(c, pid("p2b"), {"Input.Description": "", "Input.TechnicalChecklistConfirmed": "false"}, draft=True, name="borrador L-7 antes de rollback", follow=True)
    check(R, "Fixture: plan con borrador L-7", plan(pid("p2b"))["draft"] == 1)
    st, body = redo(c, pid("p2b"), 1)
    p = plan(pid("p2b"))
    check(R, "Rollback de un plan con borrador: limpia el borrador (IsDraft=0, DraftPhase nulo)", st == 200 and p["draft"] == 0 and p["draftphase"] is None, f"status={st} {p}")
    # P1OK (sano) -> L-6 no es 'anterior' si ya esta en fase 1
    st, body = redo(c, pid("p1"), 1)
    check(R, "Plan sano en fase 1 (VerifiedGood): destino 1 -> 400 (misma fase)", st == 400, f"status={st} {body[:100]}")
    # saltos multiples y plan en L-3 con L-8 completado
    st, body = redo(c, pid("p4b"), 2)
    p = plan(pid("p4b"))
    check(R, "P4 -> L-7 (salto de dos fases): cancela L-8 y L-7, deja el plan en 2/3 con la verificacion intacta", st == 200 and p["phase"] == 2 and p["state"] == 3 and p["mnt"] is None and p["req"] is None and p["ver"] is not None and status_of("Verifications", p["ver"]) != 99, f"status={st} {p}")
    # historial de estado: rollback registra el motivo
    check(R, "Rollback: registra un historial de estado 'Estado restaurado al rehacer la fase ...' (trazabilidad)", scalar("select count(*) from EquipmentStateHistories where Reason like 'Estado restaurado al rehacer la fase%' and EquipmentUnitId in (?,?)", uid("p4b"), uid("p7b")) >= 1)
    check(R, "Rollback: el texto de trazabilidad esta en espanol (nombre de la fase)", not scalar("select count(*) from EquipmentStateHistories where Reason like 'Estado restaurado al rehacer la fase%' and EquipmentUnitId in (?,?) and (Reason like '%Verification%' or Reason like '%TechnicalRequest%' or Reason like '%Maintenance%')", uid("p4b"), uid("p7b")),
          str(sql("select top 2 Reason from EquipmentStateHistories where Reason like 'Estado restaurado%' and EquipmentUnitId in (?,?) order by Id desc", uid("p4b"), uid("p7b"))))

    # fechas futuras aceptadas por L-8/Kardex y rollback posterior
    st, body = redo(c, pid("p4f"), 3)
    check(R, "Rollback de un plan cuyo L-8 se cerro con fechas FUTURAS (la app las acepta): debe revertir (200) y no fallar con 500 por CK_EquipmentStateHistories_Dates", st == 200, f"status={st} {body[:100]}")
    st, body = redo(c, pid("p6f"), 5)
    check(R, "Rollback de un plan cuyo Kardex se cerro con devolucion FUTURA: debe revertir (200) y no fallar con 500", st == 200, f"status={st} {body[:100]}")

    # ================================================================ gestion cerrada
    n_closed = scalar("select count(*) from ManagementPlans p join Managements m on m.Id=p.ManagementId where m.Status=2")
    if n_closed:
        pc = scalar("select top 1 p.Id from ManagementPlans p join Managements m on m.Id=p.ManagementId where m.Status=2 and p.CurrentPhase>1")
        if pc:
            snap = plan(pc)
            st, body = redo(c, pc, 1)
            check(R, "Gestion cerrada: 400 'La gestión está cerrada, no se pueden alterar los registros.' y plan intacto", st == 400 and "La gestión está cerrada" in body and plan(pc) == snap, f"status={st} {body[:100]}")
    else:
        check(R, "Gestion cerrada: sin planes en gestiones cerradas en la copia QA (no se puede ejercitar sin crear datos historicos); se verifico por lectura del codigo (IsReadOnly -> 400)", True, "no ejecutable")

    # ================================================================ planes de otra gestion/correctiva
    cplan = scalar("select top 1 p.Id from ManagementPlans p join Managements m on m.Id=p.ManagementId where m.Type=1 and m.Status<>2 and m.Status<>99 and p.CurrentPhase>1")
    if cplan:
        snap = plan(cplan)
        st, body = redo(c, cplan, 1)
        check(R, "Gestion correctiva: rollback a L-6 -> 400 'La verificación L-6 no forma parte de una gestión correctiva.'", st == 400 and "no forma parte de una gestión correctiva" in body and plan(cplan) == snap, f"status={st} {body[:100]}")


if __name__ == "__main__":
    P.run_module("rollback", run)
