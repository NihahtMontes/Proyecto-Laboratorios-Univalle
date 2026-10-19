"""Requerimientos tecnicos L-7: Create / Edit / Delete / Details / Index. Incluye la via correctiva (CORR-2027-2 Activa junto a la preventiva 30, reglas T0-09/D-7 y T0-10). Solo contra la copia QA."""
import os
import re

import http_cases_procesos_common as P
from http_cases_procesos_common import check, expect, sql, scalar, plan, unidad, post_form, post_raw, fase


def rq(rid):
    r = sql("select Status,Type,Priority,Description,Observations,EstimatedRepairTime,EquipmentUnitId,ManagementId,ApprovedById,RejectionReason,ModifiedById,InvestmentCode from Requests where Id=?", rid)
    keys = ["status", "type", "prio", "desc", "obs", "eta", "unit", "mgmt", "appr", "rej", "modby", "inv"]
    return dict(zip(keys, r[0])) if r else None


def n_req(unit, tipo=1):
    return scalar("select count(*) from Requests where EquipmentUnitId=? and Type=?", unit, tipo)


def checklist(n):
    base = ["1. Desconexion del cable", "2. Limpieza interna", "3. Limpieza externa", "4. Presion del refrigerante", "5. Fugas", "6. Hielo",
            "7. Temperatura", "8. Puertas y sellos", "9. Drenajes", "10. Ventiladores", "11. Electrico", "12. Lubricacion", "13. Personal externo"]
    return "\r\n".join(base[:n])


def run(R, c):
    pw = os.environ["E2E_PW"]
    lab = P.lab_propio(c, "requerimientos")
    N = lambda s: f"{s} {P.PREFIX}"

    # ---------------------------------------------------------------- fixtures: planes en L-7 (fase 2) y otros
    ctx = {}
    for k in ("a", "b", "c", "d", "e", "f", "g", "h", "i"):
        ctx[k] = P.llevar_a(c, lab, "r" + k, "P2")
    p1ok = P.llevar_a(c, lab, "rok", "P1OK")
    p1 = P.llevar_a(c, lab, "rp1", "P1")
    p3 = P.llevar_a(c, lab, "rp3", "P3")
    p4 = P.llevar_a(c, lab, "rp4", "P4")
    p7 = P.llevar_a(c, lab, "rp7", "P7")
    for k in ctx:
        fase(R, f"Fixture {k} en L-7", ctx[k]["plan"], 2, 3, 0)
    pend = {k: plan(ctx[k]["plan"])["req"] for k in ctx}

    def pg(k, **kw):
        return f"/Requests/Create?isWizard=true&ManagementPlanId={ctx[k]['plan']}&ManagementId={P.MGMT}"

    # ================================================================ Create (wizard)
    st, html = c.get(pg("a"))
    check(R, "Create: GET wizard 200 con el checklist de 13 puntos precargado", st == 200 and "13. Mantenimiento con personal externo" in html, f"status={st}")
    r = P.l7(c, ctx["a"]["plan"], {"Input.TechnicalChecklistConfirmed": "false"}, name="sin confirmar checklist", follow=True)
    expect(R, "Create: checklist sin confirmar", r, status=200, toast="Debe revisar y confirmar el checklist técnico obligatorio de 13 puntos.", toast_kind="error")
    r = P.l7(c, ctx["a"]["plan"], {"Input.Observations": checklist(12)}, name="checklist 12", follow=True)
    expect(R, "Create: checklist de 12 puntos (confirmado)", r, status=200, toast="Debe revisar y confirmar el checklist técnico obligatorio de 13 puntos.", toast_kind="error")
    r = P.l7(c, ctx["a"]["plan"], {"Input.Observations": ""}, name="checklist vacio", follow=True)
    expect(R, "Create: observaciones/checklist vacios", r, status=200, toast="checklist técnico obligatorio de 13 puntos", toast_kind="error")
    r = P.l7(c, ctx["a"]["plan"], {"Input.Observations": "\r\n".join(["Punto sin numero"] * 13)}, name="13 lineas sin numeracion", follow=True)
    check(R, "Create: 13 lineas sin numeracion no cuentan como checklist", r.has_toast("checklist técnico obligatorio", "error"), P.dump(r))
    r = P.l7(c, ctx["a"]["plan"], {"Input.Priority": "77", "Input.Observations": checklist(13)}, name="prioridad 77", follow=True)
    check(R, "Create: prioridad inexistente (77) rechazada con mensaje en espanol (el del plan 'Debe seleccionar una prioridad válida' no se alcanza: el binder responde antes)", r.status == 200 and r.has_toast("77", "error") and not P._EN.search(P.toast_text(r)), P.dump(r))
    r = P.l7(c, ctx["i"]["plan"], {"Input.Priority": "9", "Input.Observations": checklist(13)}, name="prioridad PorConfirmar", follow=True)
    check(R, "Create: prioridad 'Por confirmar' (9) no es una prioridad valida para registrar una L-7 (debe rechazarse)", r.status == 200 and bool(r.toasts) and plan(ctx["i"]["plan"])["state"] == 3, P.dump(r) + f" plan={plan(ctx['i']['plan'])['state']}")
    r = P.l7(c, ctx["a"]["plan"], {"Input.Description": "", "Input.Observations": checklist(13)}, name="sin descripcion", follow=True)
    expect(R, "Create: descripcion vacia", r, status=200, toast="La descripción del fallo es obligatoria", toast_kind="error")
    r = P.l7(c, ctx["a"]["plan"], {"Input.Description": "d" * 1001, "Input.Observations": checklist(13)}, name="descripcion 1001", follow=True)
    check(R, "Create: descripcion de 1001 caracteres -> mensaje de longitud especifico", r.status == 200 and r.has_toast("1000", "error"), P.dump(r))
    check(R, "Create: descripcion de 1001 caracteres no produce 500 ni avanza el plan", r.status < 500 and plan(ctx["a"]["plan"])["state"] == 3, P.dump(r))
    r = P.l7(c, ctx["a"]["plan"], {"Input.EstimatedRepairTime": "t" * 101, "Input.Observations": checklist(13)}, name="tiempo estimado 101", follow=True)
    check(R, "Create: tiempo estimado de 101 caracteres -> mensaje de longitud especifico", r.status == 200 and r.has_toast("100", "error"), P.dump(r))
    r = P.l7(c, 99999, name="plan inexistente", follow=True)
    check(R, "Create: plan inexistente -> 'No se pudo resolver el plan del wizard para L-7. ManagementPlanId recibido: 99999.'", r.status == 200 and r.has_toast("ManagementPlanId recibido: 99999", "error"), P.dump(r))
    check(R, "Create: ninguna validacion fallida modifico el plan", plan(ctx["a"]["plan"])["phase"] == 2 and plan(ctx["a"]["plan"])["state"] == 3 and plan(ctx["a"]["plan"])["req"] == pend["a"])

    # borrador (sin checklist ni descripcion)
    r = P.l7(c, ctx["b"]["plan"], {"Input.Description": "", "Input.TechnicalChecklistConfirmed": "false"}, draft=True, name="borrador", follow=True)
    check(R, "Create borrador: 'Borrador L-7 guardado correctamente.' sin exigir checklist ni descripcion", r.has_toast("Borrador L-7 guardado correctamente.", "success") and r.status == 302 and "Step=2" in (r.location or ""), P.dump(r))
    pb = plan(ctx["b"]["plan"])
    check(R, "Create borrador: plan sigue en L-7 (2/3) con IsDraft y DraftPhase=2", pb["phase"] == 2 and pb["state"] == 3 and pb["draft"] == 1 and pb["draftphase"] == 2, f"{pb}")
    check(R, "Create borrador: reutiliza la misma solicitud (no duplica) con descripcion marcador", pb["req"] == pend["b"] and n_req(ctx["b"]["unit"]) == 1 and rq(pb["req"])["desc"] == "Borrador L-7 pendiente de descripción técnica.", f"{rq(pb['req'])}")

    ob = rq(pb["req"])["obs"] or ""
    lineas_ob = [ln for ln in ob.split("\n") if ln.strip()[:1].isdigit()]
    check(R, "Create borrador: el checklist de 13 puntos se guarda completo (>500 caracteres, 13 lineas numeradas, conserva saltos de linea)",
          len(ob) > 500 and len(lineas_ob) == 13 and "\n" in ob and "13." in ob, f"len={len(ob)} lineas={len(lineas_ob)} termina={ob[-12:]!r}")
    r = P.l7(c, ctx["b"]["plan"], {"Input.Description": "Falla definitiva tras borrador QAP"}, name="borrador -> definitivo", follow=True)
    check(R, "Create: despues de guardar un borrador se puede registrar el L-7 definitivo desde el formulario recargado (302 al paso 3)",
          r.status == 302 and "Step=3" in (r.location or "") and plan(ctx["b"]["plan"])["state"] == 4, P.dump(r) + f" plan={plan(ctx['b']['plan'])}")

    # registro valido
    r = P.l7(c, ctx["a"]["plan"], {"Input.Description": "Motor no arranca QAP", "Input.EstimatedRepairTime": "3 dias", "Input.Priority": "2", "Input.Observations": checklist(13)}, name="valido", follow=True)
    check(R, "Create: 'Solicitud técnica L-7 registrada exitosamente.' y redirige al paso 3 del wizard", r.status == 302 and "Step=3" in (r.location or "") and r.has_toast("Solicitud técnica L-7 registrada exitosamente.", "success"), P.dump(r))
    pa = plan(ctx["a"]["plan"])
    check(R, "Create: plan a Maintenance/AwaitingMaintenance (3/4) sin borrador", pa["phase"] == 3 and pa["state"] == 4 and pa["draft"] == 0, f"{pa}")
    ra = rq(pa["req"])
    check(R, "Create: reutiliza la L-7 creada por L-6 (misma fila), con prioridad Alta, tiempo estimado y descripcion", pa["req"] == pend["a"] and n_req(ctx["a"]["unit"]) == 1 and ra["prio"] == 2 and ra["eta"] == "3 dias" and ra["desc"] == "Motor no arranca QAP", f"{ra}")
    check(R, "Create: la solicitud sigue Pendiente (0) tras registrarse L-7", ra["status"] == 0, f"{ra}")
    # reenvio del mismo L-7 (doble envio): el plan ya esta en L-8
    r = P.l7(c, ctx["a"]["plan"], {"Input.Description": "Motor no arranca QAP (reenvio)"}, name="doble envio", follow=True)
    check(R, "Create doble envio: no duplica la solicitud", n_req(ctx["a"]["unit"]) == 1, P.dump(r))
    check(R, "Create doble envio sobre un plan ya en L-8: el plan se queda en L-8 (3/4)", plan(ctx["a"]["plan"])["phase"] == 3 and plan(ctx["a"]["plan"])["state"] == 4, f"{plan(ctx['a']['plan'])}")
    # texto con HTML y observaciones largas (T0-03b: Observations admite 2000 caracteres, ya no se recorta a 500)
    r = P.l7(c, ctx["d"]["plan"], {"Input.Description": "Observaciones largas completas QAP", "Input.Observations": checklist(13) + "\r\n" + "o" * 1500}, name="obs 1500 completas", follow=True)
    rd = rq(plan(ctx["d"]["plan"])["req"])
    esperado1500 = checklist(13).replace("\r\n", "\n") + "\n" + "o" * 1500
    check(R, "Create: observaciones de ~1700 caracteres (checklist + 1500) se guardan completas (nuevo limite 2000)",
          r.status == 302 and (rd["obs"] or "") == esperado1500, f"len={len(rd['obs'] or '')} esperado={len(esperado1500)} {P.dump(r)}")
    r = P.l7(c, ctx["c"]["plan"], {"Input.Description": "Falla <script>alert(1)</script> & mas", "Input.Observations": checklist(13) + "\r\n" + "o" * 1800}, name="html y obs > 2000", follow=True)
    rc = rq(plan(ctx["c"]["plan"])["req"])
    check(R, "Create: observaciones de mas de 2000 caracteres se recortan a 2000 con '...' (no 500 de servidor)",
          r.status == 302 and len(rc["obs"] or "") == 2000 and rc["obs"].endswith("..."), f"len={len(rc['obs'] or '')} {P.dump(r)}")
    st, dp = c.get(f"/Requests/Details?id={plan(ctx['c']['plan'])['req']}")
    check(R, "Details: descripcion con HTML sale escapada", st == 200 and "<script>alert(1)</script>" not in dp and "&lt;script&gt;" in dp, f"status={st}")

    # ================================================================ L-7 correctiva (T0-09/T0-10): CORR-2027-2 Activa convive con la preventiva 30
    corr_id = scalar("select top 1 Id from Managements where Code='CORR-2027-2' and Type=1 and Status=0")
    if not corr_id:
        res = None
        corr_id = scalar("select top 1 Id from Managements where Code='CORR-2027-2' and Type=1 and Status<>99")
        if corr_id:
            res = P.post_raw(c, f"/Managements/Index?handler=ActivateManagement&id={corr_id}", [], "/Managements/Index?Type=Corrective", follow=True, name="activar CORR-2027-2")
        else:
            res = post_form(c, "/Managements/Create?type=Corrective", {"Input.Year": "2027", "Input.Semester": "2", "Input.Description": "Correctiva QA E2E",
                                                                       "Input.StartDate": "2027-07-01", "Input.PlannedEndDate": "2027-12-31", "Input.Status": "0",
                                                                       "Input.Type": "Corrective", "Input.FacultyId": ""}, name="crear CORR-2027-2", follow=True)
        corr_id = scalar("select top 1 Id from Managements where Code='CORR-2027-2' and Type=1 and Status=0")
        if not corr_id:
            raise RuntimeError(f"No se pudo asegurar la gestion correctiva activa CORR-2027-2: {P.dump(res)}")
    corr_id = int(corr_id)
    st_corr, st_prev = scalar("select Status from Managements where Id=?", corr_id), scalar("select Status from Managements where Id=30")
    check(R, "Gestiones: la correctiva CORR-2027-2 y la preventiva 30 estan Activas a la vez (una por tipo)", st_corr == 0 and st_prev == 0, f"corr={st_corr} preventiva30={st_prev}")

    # ---- regla 2 (T0-10): ya no hay L-7 sueltas; /Requests/Create sin plan ni gestion enruta al asistente correctivo
    n_antes = scalar("select count(*) from Requests")
    st, loc = c.probe("/Requests/Create")
    check(R, "Create: GET sin ManagementPlanId ni ManagementId -> 302 a /Requests/Index?handler=NewRequest", st == 302 and "handler=NewRequest" in (loc or ""), f"status={st} loc={loc}")
    r = P.post_raw(c, "/Requests/Create", [], f"/Requests/Create?ManagementId={corr_id}", follow=False, name="POST sin parametros")
    check(R, "Create: POST sin ManagementPlanId ni ManagementId -> 302 a NewRequest", r.status == 302 and "handler=NewRequest" in (r.location or ""), P.dump(r))
    check(R, "Create: ni el GET ni el POST sin parametros crearon solicitud alguna", scalar("select count(*) from Requests") == n_antes)
    cs = P.client("qa_sup", pw)
    st, loc = cs.probe("/Requests/Index?handler=NewRequest")
    check(R, "NewRequest (Supervisor): redirige al asistente correctivo de CORR-2027-2 (ShowWizard, Step=2)",
          st == 302 and "showwizard=true" in (loc or "").lower() and f"managementid={corr_id}" in (loc or "").lower() and "step=2" in (loc or "").lower(), f"status={st} loc={loc}")
    _, html = cs.get("/Requests/Index?handler=NewRequest")
    check(R, "NewRequest: aviso 'Iniciar mantenimiento correctivo: registre la falla en el L-7 del asistente.'", "Iniciar mantenimiento correctivo: registre la falla en el L-7 del asistente." in (html or ""), "")

    def campos_libre(mg, u):
        return [("Input.FacultyId", str(P.FAC)), ("Input.LaboratoryId", str(lab)), ("Input.EquipmentUnitId", str(u)),
                ("Input.Description", "Falla suelta QAP"), ("Input.Observations", checklist(13)),
                ("Input.TechnicalChecklistConfirmed", "true"), ("Input.Priority", "1"), ("ManagementId", str(mg))]

    n_antes = scalar("select count(*) from Requests")
    r = P.post_raw(c, f"/Requests/Create?ManagementId={P.MGMT}", campos_libre(P.MGMT, ctx["a"]["unit"]), f"/Requests/Create?ManagementId={corr_id}", follow=True, name="preventiva sin plan")
    check(R, "Create: POST con ManagementId=30 (preventiva) sin plan -> 'la L-7 nace de un L-6 con falla' y va al asistente",
          r.has_toast("En el mantenimiento preventivo la L-7 nace de un L-6 con falla", "warning") and "showwizard=true" in (r.location or "").lower(), P.dump(r))
    check(R, "Create: la L-7 preventiva suelta no creo ninguna solicitud", scalar("select count(*) from Requests") == n_antes)
    mgmt_inactiva = scalar("select top 1 Id from Managements where Status=1 and Type<>9 order by Id")
    if not mgmt_inactiva:
        # la copia recién restaurada no trae gestiones Inactivas: se crea una correctiva Inactiva propia (CORR-2027-1;
        # los códigos AAAA-S preventivos están ocupados por la importación)
        post_form(c, "/Managements/Create?type=Corrective", {"Input.Year": "2027", "Input.Semester": "1", "Input.Description": "Correctiva inactiva QA E2E",
                                                              "Input.StartDate": "2027-01-15", "Input.PlannedEndDate": "2027-06-30", "Input.Status": "1",
                                                              "Input.Type": "Corrective", "Input.FacultyId": ""}, name="crear CORR-2027-1 inactiva", follow=True)
        mgmt_inactiva = scalar("select top 1 Id from Managements where Status=1 and Type<>9 order by Id")
    n_antes = scalar("select count(*) from Requests")
    r = P.post_raw(c, f"/Requests/Create?ManagementId={mgmt_inactiva}", campos_libre(mgmt_inactiva, ctx["a"]["unit"]), f"/Requests/Create?ManagementId={corr_id}", follow=True, name="gestion inactiva")
    check(R, "Create: POST con ManagementId de una gestion Inactiva -> 'no esta activa' y vuelve a /Requests sin crear nada",
          r.has_toast("La gestión seleccionada no está activa: no admite nuevas solicitudes L-7.", "warning") and (r.location or "").lower().split("?")[0].rstrip("/") == "/requests" and scalar("select count(*) from Requests") == n_antes, P.dump(r) + f" mgmt={mgmt_inactiva}")

    # ---- D-7 (T0-09): un proceso correctivo abierto por unidad y gestion
    ud7, invd7 = P.nueva_unidad(c, lab, "rd7")
    fd7 = {"Input.FacultyId": str(P.FAC), "Input.LaboratoryId": str(lab), "Input.EquipmentUnitId": str(ud7), "Input.Description": "Primera falla correctiva QAP",
           "Input.Observations": checklist(13), "Input.TechnicalChecklistConfirmed": "true", "Input.Priority": "1"}
    r = post_form(c, f"/Requests/Create?isWizard=true&ManagementId={corr_id}", fd7, name="correctiva primera (D-7)", follow=True)
    check(R, "Create correctivo: la L-7 de la correctiva activa se registra y salta al paso 3 del asistente",
          r.status == 302 and "Step=3" in (r.location or "") and r.has_toast("Solicitud técnica L-7 registrada exitosamente.", "success"), P.dump(r))
    pd7 = scalar("select top 1 Id from ManagementPlans where ManagementId=? and EquipmentUnitId=? order by Id desc", corr_id, ud7)
    fase(R, "Create correctivo (D-7): sin plan previo se crea el ManagementPlan en fase Mantenimiento (3/4, En Progreso)", pd7, 3, 4, 1)
    n_reqs, n_plans = scalar("select count(*) from Requests"), scalar("select count(*) from ManagementPlans")
    r = post_form(c, f"/Requests/Create?isWizard=true&ManagementId={corr_id}", fd7, name="correctiva duplicada (D-7)", follow=True)
    check(R, "Create: unidad con L-7 pendiente -> D-7: 'ya tiene un proceso correctivo abierto en esta gestion' con la unidad y el paso",
          r.has_toast("ya tiene un proceso correctivo abierto en esta gestión", "warning") and f"La unidad {invd7}" in P.toast_text(r) and "asistente (paso 3)" in P.toast_text(r), P.dump(r))
    check(R, "Create D-7: no se creo ninguna solicitud ni plan adicional",
          scalar("select count(*) from Requests") == n_reqs and scalar("select count(*) from ManagementPlans") == n_plans,
          f"reqs={n_reqs}->{scalar('select count(*) from Requests')} plans={n_plans}->{scalar('select count(*) from ManagementPlans')}")

    # ---- L-7 por la via correctiva (T0-10): alimenta las pruebas de Edit (antes era una L-7 suelta sin plan)
    unw = P.nueva_unidad(c, lab, "rnw")
    nwf = {"Input.FacultyId": str(P.FAC), "Input.LaboratoryId": str(lab), "Input.EquipmentUnitId": str(unw[0]), "Input.Description": "Falla sin plan QAP",
           "Input.Observations": checklist(13), "Input.TechnicalChecklistConfirmed": "true", "Input.Priority": "1"}
    r = post_form(c, f"/Requests/Create?ManagementId={corr_id}", nwf, name="no wizard sin plan (correctiva)", follow=True)
    check(R, "Create no-wizard sin plan: registra la L-7 y vuelve al listado",
          r.status == 302 and r.has_toast("Solicitud técnica L-7 registrada exitosamente.", "success") and n_req(unw[0]) == 1, P.dump(r))
    rid_nw = scalar("select top 1 Id from Requests where EquipmentUnitId=? and Type=1 order by Id desc", unw[0])
    pid_nw = scalar("select top 1 Id from ManagementPlans where ManagementId=? and EquipmentUnitId=? order by Id desc", corr_id, unw[0])
    pnw = plan(pid_nw)
    check(R, "Create no-wizard sin plan (via correctiva): crea tambien el ManagementPlan (3/4) con la L-7 enlazada a CORR-2027-2",
          bool(pnw) and pnw["phase"] == 3 and pnw["state"] == 4 and pnw["req"] == rid_nw and pnw["mgmt"] == corr_id and pnw["unit"] == unw[0], f"{pnw}")
    r = post_form(c, f"/Requests/Create?ManagementId={corr_id}", nwf, name="no wizard repetida", follow=True)
    check(R, "Create no-wizard repetida (doble envio): rechazada por proceso correctivo abierto (D-7), sin duplicar",
          r.has_toast("ya tiene un proceso correctivo abierto", "warning") and n_req(unw[0]) == 1, P.dump(r))
    r = post_form(c, f"/Requests/Create?ManagementId={corr_id}", {**nwf, "Input.EquipmentUnitId": "99999"}, name="unidad inexistente", follow=True)
    check(R, "Create: unidad inexistente -> 'la unidad fisica no existe' sin 500", r.status < 500 and r.has_toast("la unidad física no existe", "error"), P.dump(r))
    r = post_form(c, f"/Requests/Create?ManagementId={corr_id}", {**nwf, "Input.EquipmentUnitId": ""}, name="sin unidad", follow=True)
    check(R, "Create: sin unidad -> rechazo con 'La unidad fisica es obligatoria' y sin 500",
          r.status < 500 and (r.has_toast("obligatorio", "error") or r.has_error("La unidad física es obligatoria")), P.dump(r))

    # fases del plan: L-7 sobre un plan que NO esta en L-7
    antes = plan(p1ok["plan"])
    r = P.l7(c, p1ok["plan"], {"Input.Observations": checklist(13)}, name="L-7 sobre plan sano (P1OK)", follow=True)
    desp = plan(p1ok["plan"])
    check(R, "Create: L-7 sobre un plan SANO (VerifiedGood, sin falla en L-6) debe rechazarse", desp["phase"] == antes["phase"] and desp["state"] == antes["state"], f"antes={antes['phase']}/{antes['state']} despues={desp['phase']}/{desp['state']} {P.dump(r)}")
    antes = plan(p1["plan"])
    r = P.l7(c, p1["plan"], {"Input.Observations": checklist(13)}, name="L-7 sobre plan sin L-6 (P1)", follow=True)
    desp = plan(p1["plan"])
    check(R, "Create: L-7 sobre un plan que aun no tiene L-6 debe rechazarse", desp["phase"] == antes["phase"] and desp["state"] == antes["state"], f"antes={antes['phase']}/{antes['state']} despues={desp['phase']}/{desp['state']} {P.dump(r)}")
    antes = plan(p4["plan"])
    r = P.l7(c, p4["plan"], {"Input.Observations": checklist(13)}, name="L-7 sobre plan en L-3 (P4)", follow=True)
    desp = plan(p4["plan"])
    check(R, "Create: L-7 sobre un plan que ya esta en L-3 (fase 4) no debe retroceder la fase", desp["phase"] == antes["phase"] and desp["state"] == antes["state"], f"antes={antes['phase']}/{antes['state']} despues={desp['phase']}/{desp['state']} {P.dump(r)}")
    antes = plan(p3["plan"])
    r = P.l7(c, p3["plan"], {"Input.Description": "", "Input.TechnicalChecklistConfirmed": "false"}, draft=True, name="borrador sobre plan en L-8 (P3)", follow=True)
    desp = plan(p3["plan"])
    check(R, "Create: guardar BORRADOR L-7 sobre un plan que ya esta en L-8 no debe retroceder la fase", desp["phase"] == antes["phase"] and desp["state"] == antes["state"], f"antes={antes['phase']}/{antes['state']} despues={desp['phase']}/{desp['state']} {P.dump(r)}")
    antes = plan(p7["plan"])
    r = P.l7(c, p7["plan"], {"Input.Observations": checklist(13)}, name="L-7 sobre plan completado (P7)", follow=True)
    desp = plan(p7["plan"])
    check(R, "Create: L-7 sobre un plan COMPLETADO no debe reabrirlo", desp["phase"] == antes["phase"] and desp["state"] == antes["state"] and desp["pstatus"] == antes["pstatus"], f"antes={antes['phase']}/{antes['state']}/{antes['pstatus']} despues={desp['phase']}/{desp['state']}/{desp['pstatus']} {P.dump(r)}")

    # ================================================================ Details / Index
    st, _ = c.get(f"/Requests/Details?id={pend['a']}")
    check(R, "Details: 200", st == 200, f"status={st}")
    for pth in ("/Requests/Details?id=99999", "/Requests/Details", "/Requests/Details?id=abc"):
        st, loc = c.probe(pth)
        check(R, f"Details {pth}: sin 500 (redirige a la pagina de error controlada)", st < 500, f"status={st} loc={loc}")
    for pth in ("/Requests", f"/Requests?SearchTerm={P.PREFIX}", "/Requests?SearchTerm=ZZZ_NO_EXISTE_QAP", "/Requests?pageIndex=999", "/Requests?pageIndex=-1", "/Requests?pageIndex=abc"):
        st, html = c.get(pth)
        check(R, f"Index {pth}: 200 sin pagina de error", st == 200, f"status={st}")
    st, html = c.get("/Requests?SearchTerm=ZZZ_NO_EXISTE_QAP")
    check(R, "Index: sin resultados -> 'No se encontraron requerimientos técnicos (L-7) registrados.'", "No se encontraron requerimientos técnicos (L-7) registrados." in html)

    # ================================================================ Edit
    ep = lambda i: f"/Requests/Edit/{i}"
    st, _ = c.get("/Requests/Edit/99999")
    check(R, "Edit: GET id inexistente -> 404", st == 404, f"status={st}")
    st, _ = c.get(ep(rid_nw))
    check(R, "Edit: GET 200 (L-7 correctiva con plan propio)", st == 200, f"status={st}")
    r = post_form(c, ep(rid_nw), {"Input.Description": ""}, name="edit sin descripcion")
    expect(R, "Edit: descripcion vacia", r, status=200, error="La descripción es obligatoria")
    r = post_form(c, ep(rid_nw), {"Input.Description": "d" * 1001}, name="edit descripcion 1001")
    expect(R, "Edit: descripcion de 1001 caracteres", r, status=200, error="La descripción no puede superar los 1000 caracteres")
    r = post_form(c, ep(rid_nw), {"Input.Observations": "o" * 2001}, name="edit observaciones 2001")
    expect(R, "Edit: observaciones de 2001 caracteres (limite 2000 desde D-2)", r, status=200, error="Las observaciones no pueden superar los 2000 caracteres")
    r = post_form(c, ep(rid_nw), {"Input.EstimatedRepairTime": "t" * 101}, name="edit tiempo 101")
    check(R, "Edit: tiempo estimado de 101 caracteres -> mensaje en espanol", r.status == 200 and bool(r.errors) and not P._EN.search(" ".join(r.errors)), P.dump(r))
    r = post_form(c, ep(rid_nw), {"Input.RejectionReason": "r" * 501}, name="edit motivo 501", follow=False)
    check(R, "Edit: motivo de rechazo de 501 caracteres no produce 500", r.status < 500, P.dump(r))
    r = post_form(c, ep(rid_nw), {"Input.Priority": "77"}, name="edit prioridad 77", follow=False)
    check(R, "Edit: prioridad inexistente (77) se rechaza", r.status == 200 and rq(rid_nw)["prio"] == 1, P.dump(r) + f" prio={rq(rid_nw)['prio']}")
    r = post_form(c, ep(rid_nw), {"Input.Status": "77"}, name="edit estado 77", follow=False)
    check(R, "Edit: estado inexistente (77) se rechaza", r.status == 200 and rq(rid_nw)["status"] == 0, P.dump(r) + f" estado={rq(rid_nw)['status']}")
    r = post_form(c, ep(rid_nw), {"Input.Id": "99999"}, name="edit Id inexistente", follow=False)
    check(R, "Edit: POST con Input.Id inexistente -> 404", r.status == 404, P.dump(r))
    check(R, "Edit: ningun rechazo modifico la solicitud", rq(rid_nw)["desc"] == "Falla sin plan QAP" and rq(rid_nw)["status"] == 0)
    # cambio valido + aprobacion
    r = post_form(c, ep(rid_nw), {"Input.Description": "Falla corregida QAP", "Input.Priority": "3", "Input.Status": "3", "Input.EstimatedRepairTime": "1 dia"}, name="edit valido", follow=True)
    check(R, "Edit: cambio valido -> 302 a Requests con 'Solicitud #id actualizada correctamente.'", r.status == 302 and r.has_toast(f"Solicitud #{rid_nw} actualizada correctamente.", "success"), P.dump(r))
    x = rq(rid_nw)
    check(R, "Edit: descripcion, prioridad Muy alta, estado Aprobada, tiempo estimado y auditoria guardados", x["desc"] == "Falla corregida QAP" and x["prio"] == 3 and x["status"] == 3 and x["eta"] == "1 dia" and x["appr"] is not None and x["modby"] is not None, f"{x}")
    r = post_form(c, ep(rid_nw), {"Input.Status": "4", "Input.RejectionReason": ""}, name="edit rechazada sin motivo", follow=False)
    check(R, "Edit: pasar a 'Rechazada' exige un motivo de rechazo", r.status == 200 and rq(rid_nw)["status"] == 3, P.dump(r) + f" estado={rq(rid_nw)['status']}")
    r = post_form(c, ep(rid_nw), {"Input.Status": "0"}, name="edit vuelve a pendiente", follow=False)
    check(R, "Edit: una L-7 ya aprobada no puede volver a Pendiente (reabre la regla 'una L-7 pendiente por unidad')", r.status == 200 and rq(rid_nw)["status"] == 3, P.dump(r) + f" estado={rq(rid_nw)['status']}")
    # L-7 que viene de L-6: descripcion/observaciones no se alteran
    rid_l6 = pend["e"]
    desc_l6 = rq(rid_l6)["desc"]
    r = post_form(c, ep(rid_l6), {"Input.Description": "Intento de cambio", "Input.Observations": "otro", "Input.Priority": "2"}, name="edit L-7 derivada de L-6", follow=True)
    y = rq(rid_l6)
    check(R, "Edit L-7 de L-6: descripcion y observaciones NO cambian, prioridad si", y["desc"] == desc_l6 and y["prio"] == 2, f"{y['desc'][:40]!r} prio={y['prio']} {P.dump(r)}")
    # Supervisor no cambia estado (cs: sesion qa_sup creada en el bloque del asistente correctivo)
    rid_sup = rid_l6
    r = post_form(cs, ep(rid_sup), {"Input.Status": "3", "Input.Priority": "1"}, name="edit como Supervisor", follow=False)
    check(R, "Rol Supervisor: puede editar la ficha pero el cambio de ESTADO se ignora", r.status == 302 and rq(rid_sup)["status"] == 0 and rq(rid_sup)["prio"] == 1, P.dump(r) + f" {rq(rid_sup)['status']}/{rq(rid_sup)['prio']}")
    # solicitud de compra (L-12) editada por Requests/Edit
    acq_id = plan(p7["plan"])["acq"]
    st, _ = c.get(ep(acq_id))
    check(R, "Edit: la solicitud de compra (L-12) se abre en Requests/Edit", st == 200, f"status={st}")
    r = post_form(c, ep(acq_id), {"Input.InvestmentCode": ""}, name="edit compra sin codigo")
    check(R, "Edit compra: codigo de inversion obligatorio -> 'El código de inversión es obligatorio.'", r.status == 200 and r.has_error("El código de inversión es obligatorio."), P.dump(r))
    r = post_form(c, ep(acq_id), {"Input.InvestmentCode": "INV-X"}, name="edit compra sin items", follow=False)
    check(R, "Edit compra: sin items -> 'Debe existir al menos un ítem.'", r.status == 200 and r.has_error("Debe existir al menos un ítem."), P.dump(r))
    r = post_form(c, ep(acq_id), {"Input.InvestmentCode": "INV-X", "Input.Items[0].Concept": "Pieza", "Input.Items[0].Quantity": "0", "Input.Items[0].UnitPrice": "10"}, name="edit compra cantidad 0", follow=False)
    check(R, "Edit compra: cantidad 0 rechazada con mensaje en espanol", r.status == 200 and bool(r.errors) and not P._EN.search(" ".join(r.errors)), P.dump(r))
    r = post_form(c, ep(acq_id), {"Input.InvestmentCode": "INV-X", "Input.Items[0].Concept": "", "Input.Items[0].Quantity": "1", "Input.Items[0].UnitPrice": "10"}, name="edit compra concepto vacio", follow=False)
    check(R, "Edit compra: concepto vacio rechazado con mensaje en espanol", r.status == 200 and bool(r.errors) and not P._EN.search(" ".join(r.errors)), P.dump(r))
    r = post_form(c, ep(acq_id), {"Input.InvestmentCode": "INV-X", "Input.Items[0].Concept": "Pieza", "Input.Items[0].Quantity": "2", "Input.Items[0].UnitPrice": "10"}, name="edit compra valida", follow=True)
    check(R, "Edit compra valida", r.status == 302 and r.has_toast("actualizada correctamente", "success"), P.dump(r))
    # doble envio
    a1 = post_form(c, ep(rid_nw), {"Input.Priority": "2"}, name="doble clic 1", follow=False)
    a2 = post_form(c, ep(rid_nw), {"Input.Priority": "2"}, name="doble clic 2", follow=False)
    check(R, "Edit: doble envio idempotente (ambos 302)", a1.status == 302 and a2.status == 302, f"{a1.status},{a2.status}")

    # ================================================================ Delete (anular)
    st, _ = c.get(f"/Requests/Delete/{pend['f']}")
    check(R, "Delete: GET 200", st == 200, f"status={st}")
    st, _ = c.get("/Requests/Delete/99999")
    check(R, "Delete: GET id inexistente -> 404", st == 404, f"status={st}")
    req_mnt = plan(p4["plan"])["req"]
    r = P.post_raw(c, f"/Requests/Delete/{req_mnt}", [], "/Requests", name="delete con mantenimiento", follow=True)
    check(R, "Delete: L-7 con mantenimiento asociado -> 'No se puede eliminar la solicitud porque ya tiene un registro de mantenimiento asociado.'", r.has_toast("No se puede eliminar la solicitud porque ya tiene un registro de mantenimiento asociado.", "error"), P.dump(r))
    check(R, "Delete bloqueado: la solicitud no cambio de estado", rq(req_mnt)["status"] != 99, f"{rq(req_mnt)}")
    r = P.post_raw(c, f"/Requests/Delete/{pend['f']}", [], "/Requests", name="delete", follow=True)
    expect(R, "Delete: anula la L-7 sin mantenimiento", r, status=302, toast=f"La solicitud #{pend['f']} ha sido anulada correctamente.", toast_kind="success")
    check(R, "Delete: queda Cancelled (99) y el plan vuelve a esperar una L-7 (2/3) sin apuntar a la anulada (2026-10-18)", rq(pend["f"])["status"] == 99 and plan(ctx["f"]["plan"])["req"] is None and plan(ctx["f"]["plan"])["state"] == 3, f"{rq(pend['f'])} {plan(ctx['f']['plan'])}")
    r = P.post_raw(c, f"/Requests/Delete/{pend['f']}", [], "/Requests", name="delete repetido", follow=False)
    check(R, "Delete repetido -> 404 (ya no existe)", r.status == 404, P.dump(r))
    st, _ = c.get(pg("f"))
    check(R, "Create: el formulario L-7 de un plan cuya solicitud fue anulada sigue abriendo (200)", st == 200, f"status={st}")
    r = P.l7(c, ctx["f"]["plan"], {"Input.Description": "Reemplazo tras anulacion"}, name="L-7 tras anulacion", follow=True)
    check(R, "Create tras anulacion: registra una L-7 nueva y el plan avanza a L-8", r.status == 302 and plan(ctx["f"]["plan"])["state"] == 4 and plan(ctx["f"]["plan"])["req"] != pend["f"], P.dump(r) + f" {plan(ctx['f']['plan'])}")
    r = P.post_raw(c, f"/Requests/Delete/{acq_id}", [], "/Requests", name="delete de compra L-12", follow=False)
    check(R, "Delete: Requests/Delete NO debe anular una solicitud de COMPRA (L-12) de un plan completado", rq(acq_id)["status"] != 99, P.dump(r) + f" estado={rq(acq_id)['status']}")
    r = P.post_raw(c, "/Requests/Delete/99999", [], "/Requests", name="delete inexistente", follow=False)
    check(R, "Delete: POST id inexistente -> 404", r.status == 404, P.dump(r))

    # ================================================================ acceso
    for pth in ("/Requests", pg("a"), ep(rid_nw), f"/Requests/Delete/{pend['g']}", f"/Requests/Details?id={pend['g']}"):
        P.check_anon(R, "Requests", pth)
    P.check_anon(R, "Requests", pg("a"), post=True)
    P.tokenless(R, c, "Requests/Create", pg("g"), [("Input.Description", "x"), ("Input.TechnicalChecklistConfirmed", "true")])
    P.tokenless(R, c, "Requests/Edit", ep(rid_nw), [("Input.Id", str(rid_nw)), ("Input.Description", "x"), ("Input.Priority", "1")])
    P.tokenless(R, c, "Requests/Delete", f"/Requests/Delete/{pend['g']}", [])
    check(R, "Sin token: la L-7 'g' sigue Pendiente", rq(pend["g"])["status"] == 0)
    for usuario, rol in (("qa_sup", "Supervisor"), ("qa_cierre", "PracticeClosure")):
        cl = P.client(usuario, pw)
        r = post_raw(cl, f"/Requests/Delete/{pend['g']}", [], "/", follow=False, name=f"delete como {rol}")
        check(R, f"Rol {rol}: POST Delete denegado (Delete es de Administrador)", r.status in (302, 403) and rq(pend["g"])["status"] != 99, P.dump(r))
    cc = P.client("qa_cierre", pw)
    st, loc = cc.probe("/Requests")
    check(R, "Rol PracticeClosure: listado /Requests denegado", st in (302, 403) and "/Login" not in (loc or ""), f"status={st} loc={loc}")
    r = post_form(cs, pg("h"), {"Input.Description": "Registro del Supervisor", "Input.TechnicalChecklistConfirmed": "true"}, name="create como Supervisor", follow=True)
    check(R, "Rol Supervisor: puede registrar L-7 (ManagementRoles) y avanza el plan", r.status == 302 and plan(ctx["h"]["plan"])["state"] == 4, P.dump(r) + f" {plan(ctx['h']['plan'])}")


if __name__ == "__main__":
    P.run_module("requests", run)
