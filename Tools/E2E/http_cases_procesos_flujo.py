"""Flujo completo L-6 -> L-7 -> L-8 -> L-3 -> Kardex -> L-12 en la gestion preventiva activa 30 (2027-2),
descubriendo los ManagementPlanId en los enlaces del dashboard /?ShowWizard=true&Step=N&ManagementId=30&SelectedLabId=N.
Solo contra la copia QA."""
import re

import http_cases_procesos_common as P
from http_cases_procesos_common import check, expect, sql, scalar, plan, unidad, post_form, post_raw, fase

CHK = (chr(13) + chr(10)).join(f"{i}. punto de control" for i in range(1, 14))
PLAN_RE = re.compile(r"[mM]anagementPlanId=(\d+)")


def dash(c, step, lab):
    st, html = c.get(f"/?ShowWizard=true&Step={step}&ManagementId={P.MGMT}&SelectedLabId={lab}")
    return st, html, {int(x) for x in PLAN_RE.findall(html)}


def run(R, c):
    lab = P.lab_propio(c, "flujo")
    lab_otro = P.lab_propio(c, "flujo B")
    ua = P.nueva_unidad(c, lab, "fa")          # falla -> recorre todo el flujo
    ub = P.nueva_unidad(c, lab, "fb")          # sano
    uc = P.nueva_unidad(c, lab, "fc")          # falla -> usa borradores en cada fase
    ud = P.nueva_unidad(c, lab_otro, "fd")     # otro laboratorio: no debe verse en el flujo de 'lab'
    planes = P.planificar(c, [ua[0], ub[0], uc[0], ud[0]])
    pa, pb, pc, pd = (planes[u[0]] for u in (ua, ub, uc, ud))
    check(R, "PlanAssets: las 4 unidades entran al plan de la gestion 30 como L-6 pendiente (fase 1 / estado 1)", all(plan(p)["phase"] == 1 and plan(p)["state"] == 1 for p in (pa, pb, pc, pd)), f"{[plan(p) for p in (pa, pb, pc, pd)]}")

    # ---------------------------------------------------------------- L-6 (paso 1): los planes se listan en MassCreate (handler que usa la interfaz)
    st, html, ids = dash(c, 1, lab)
    check(R, "Dashboard paso 1 (L-6): 200 y enlaza el MassCreate del laboratorio", st == 200 and f"labId={lab}" in html.replace("&amp;", "&"), f"status={st}")
    st, js = c.get(f"/Verifications/MassCreate?handler=EquipmentByLab&labId={lab}&managementId={P.MGMT}")
    check(R, "L-6: el listado de MassCreate contiene los planes del laboratorio (A, B, C) y no el del otro laboratorio", all(f'"planId":{p}' in js for p in (pa, pb, pc)) and f'"planId":{pd}' not in js, js[:200])
    r = P.l6(c, 0, 0, lab, filas=[(pa, ua[0], 2, "Motor no enciende QAP"), (pb, ub[0], 4, ""), (pc, uc[0], 2, "Fuga de gas QAP")], name="flujo L-6", follow=True)
    check(R, "L-6: '3 verificaciones registradas (2 equipos con fallas).' y redirige al paso 2", r.has_toast("3 verificaciones registradas (2 equipos con fallas).", "success") and "Step=2" in (r.location or ""), P.dump(r))
    fase(R, "L-6 A (Malo)", pa, 2, 3, 0)
    fase(R, "L-6 B (Bueno)", pb, 1, 2, 0)
    fase(R, "L-6 C (Malo)", pc, 2, 3, 0)

    # ---------------------------------------------------------------- L-7 (paso 2): el dashboard enlaza los planes en falla
    st, html, ids = dash(c, 2, lab)
    check(R, "Dashboard paso 2 (L-7): enlaces 'Requests/Create?...managementPlanId=' para A y C (no B ni D)", st == 200 and pa in ids and pc in ids and pb not in ids and pd not in ids, f"status={st} ids={sorted(ids)}")
    r = P.l7(c, pa, {"Input.Description": "Motor no enciende (L-7 flujo QAP)", "Input.Priority": "2"}, name="flujo L-7 A", follow=True)
    check(R, "L-7 A: 'Solicitud técnica L-7 registrada exitosamente.' -> paso 3", r.has_toast("Solicitud técnica L-7 registrada exitosamente.", "success") and "Step=3" in (r.location or ""), P.dump(r))
    fase(R, "L-7 A", pa, 3, 4, 0)
    r = P.l7(c, pc, {"Input.Description": "", "Input.TechnicalChecklistConfirmed": "false"}, draft=True, name="flujo L-7 C borrador", follow=True)
    fase(R, "L-7 C borrador", pc, 2, 3, None, True)
    r = P.l7(c, pc, {"Input.Description": "Fuga de gas (L-7 flujo QAP)"}, name="flujo L-7 C definitivo desde el formulario recargado", follow=True)
    check(R, "L-7 C: borrador -> registro definitivo desde el formulario recargado (el checklist guardado en el borrador esta recortado a 500 caracteres)", r.status == 302, P.dump(r) + f" {plan(pc)}")
    if plan(pc)["state"] != 4:   # se reintenta con el checklist completo para poder seguir recorriendo el flujo
        P.l7(c, pc, {"Input.Description": "Fuga de gas (L-7 flujo QAP)", "Input.Observations": CHK}, name="flujo L-7 C definitivo (checklist completo)", follow=True)
    fase(R, "L-7 C", pc, 3, 4, None, False)

    # ---------------------------------------------------------------- L-8 (paso 3)
    st, html, ids = dash(c, 3, lab)
    check(R, "Dashboard paso 3 (L-8): enlaces 'Maintenances/Create?...managementPlanId=' para A y C", st == 200 and pa in ids and pc in ids and pb not in ids, f"status={st} ids={sorted(ids)}")
    r = P.l8(c, pa, name="flujo L-8 A", follow=True)
    check(R, "L-8 A: guardado -> paso 4", r.status == 302 and "Step=4" in (r.location or ""), P.dump(r))
    fase(R, "L-8 A", pa, 4, 6, 0)
    r = P.l8(c, pc, {"Input.Status": "1"}, draft=True, name="flujo L-8 C borrador", follow=True)
    check(R, "L-8 C: borrador de mantenimiento (plan sigue en paso 3)", r.status == 302 and "Step=3" in (r.location or "") and plan(pc)["phase"] == 3 and plan(pc)["draft"] == 1, P.dump(r) + f" {plan(pc)}")
    r = P.l8(c, pc, tech=P.TECH_EXT, name="flujo L-8 C definitivo (tecnico externo)", follow=True)
    fase(R, "L-8 C", pc, 4, 6, None, False)

    # ---------------------------------------------------------------- L-3 (paso 4)
    st, html, ids = dash(c, 4, lab)
    st2, js = c.get(f"/Departures/MassCreate?handler=EquipmentByLab&labId={lab}&managementId={P.MGMT}")
    check(R, "Dashboard paso 4 (L-3): 200 y el listado de MassCreate L-3 contiene A y C con tecnico", st == 200 and f'"planId":{pa}' in js and f'"planId":{pc}' in js and f'"planId":{pb}' not in js, js[:240])
    check(R, "Dashboard paso 4: los enlaces del paso (si los hay) son de planes de este flujo", ids <= {pa, pb, pc, pd}, f"ids={sorted(ids)}")
    r = P.l3(c, 0, 0, lab, filas=[(pa, ua[0], "true"), (pc, uc[0], "false")], salida=P.d(0), retorno=P.d(7), name="flujo L-3 A", follow=True)
    check(R, "L-3: '1 salida(s) registrada(s) correctamente.' (C queda fuera por Include=false) -> paso 5", r.has_toast("1 salida(s) registrada(s) correctamente.", "success") and "Step=5" in (r.location or ""), P.dump(r))
    fase(R, "L-3 A", pa, 5, 7, 0)
    check(R, "L-3 A: la unidad pasa a En Prestamo (10)", unidad(ua[0])["status"] == 10)
    fase(R, "L-3 C (no incluida)", pc, 4, 6, None)
    r = P.l3(c, 0, 0, lab, filas=[(pc, uc[0], "true")], salida=P.d(0), retorno=P.d(5), name="flujo L-3 C", follow=True)
    fase(R, "L-3 C", pc, 5, 7, None)

    # ---------------------------------------------------------------- Kardex (paso 5)
    st, html, ids = dash(c, 5, lab)
    check(R, "Dashboard paso 5 (Kardex): enlaces 'Kardex/Create?...managementPlanId=' para A y C", st == 200 and pa in ids and pc in ids, f"status={st} ids={sorted(ids)}")
    r = P.kardex(c, pc, {"Input.Description": "", "Input.Recommendations": ""}, draft=True, name="flujo Kardex C borrador", follow=True)
    fase(R, "Kardex C borrador", pc, 5, 7, None, True)
    r = P.kardex(c, pa, name="flujo Kardex A", follow=True)
    check(R, "Kardex A: cierre -> paso 6", r.status == 302 and "Step=6" in (r.location or ""), P.dump(r))
    fase(R, "Kardex A", pa, 6, 8, 1, False)
    check(R, "Kardex A: la unidad vuelve a Operativa (0)", unidad(ua[0])["status"] == 0)
    r = P.kardex(c, pc, name="flujo Kardex C", follow=True)
    fase(R, "Kardex C", pc, 6, 8, 1, False)

    # ---------------------------------------------------------------- L-12 (paso 6)
    st, html, ids = dash(c, 6, lab)
    check(R, "Dashboard paso 6 (L-12): enlaces 'Acquisitions/Create?...managementPlanId=' para A y C", st == 200 and pa in ids and pc in ids, f"status={st} ids={sorted(ids)}")
    r = P.l12(c, pa, {"Input.Description": "Compra por falla (flujo QAP)"}, name="flujo L-12 A", follow=True)
    check(R, "L-12 A: registro -> paso 7", r.status == 302 and "Step=7" in (r.location or ""), P.dump(r))
    fase(R, "L-12 A", pa, 6, 9, 2)
    r = P.l12(c, pc, {"Input.Description": "", "Input.InvestmentCode": ""}, draft=True, name="flujo L-12 C borrador", follow=True)
    fase(R, "L-12 C borrador", pc, 6, 8, 1, True)
    r = P.l12(c, pc, {"Input.Description": "Compra C (flujo QAP)"}, name="flujo L-12 C", follow=True)
    fase(R, "L-12 C", pc, 6, 9, 2, False)

    # ---------------------------------------------------------------- completados (paso 7) y consistencia global
    st, html, ids = dash(c, 7, lab)
    check(R, "Dashboard paso 7 (Completados): 200 y aparecen las unidades A y C; no el sano B ni el de otro laboratorio", st == 200 and ua[1] in html and uc[1] in html and ub[1] not in html and ud[1] not in html, f"status={st}")
    for step in range(0, 8):
        st, html, ids = dash(c, step, lab)
        check(R, f"Dashboard paso {step}: 200 sin pagina de error", st == 200 and "Codigo tecnico de seguimiento" not in html, f"status={st}")
    st, html = c.get(f"/Managements/Details?id={P.MGMT}")
    check(R, "Managements/Details de la gestion 30: 200 tras el flujo", st == 200, f"status={st}")
    check(R, "Consistencia: B (sano) sigue en L-6 VerifiedGood y D (otro laboratorio) intacto en L-6 pendiente", plan(pb)["phase"] == 1 and plan(pb)["state"] == 2 and plan(pd)["phase"] == 1 and plan(pd)["state"] == 1)
    for nombre, p_, u_ in (("A", pa, ua[0]), ("C", pc, uc[0])):
        q = plan(p_)
        check(R, f"Consistencia {nombre}: plan Completed con L-6, L-7, L-8, L-3, Kardex y L-12 vinculados y la unidad Operativa", all(q[k] is not None for k in ("ver", "req", "mnt", "dep", "kardex", "acq")) and unidad(u_)["status"] == 0, f"{q}")
        check(R, f"Consistencia {nombre}: salida Devuelta, L-8 Completado, solicitud de compra Pendiente (tipo 2)",
              scalar("select Status from Departures where Id=?", q["dep"]) == 1 and scalar("select Status from Maintenances where Id=?", q["mnt"]) == 2 and scalar("select Type from Requests where Id=?", q["acq"]) == 2, "")
    check(R, "Consistencia: la solicitud tecnica L-7 de A quedo vinculada al L-8 (Maintenances.RequestId)", scalar("select RequestId from Maintenances where Id=?", plan(pa)["mnt"]) == plan(pa)["req"])
    check(R, "Consistencia: el L-8 de C uso tecnico externo y el borrador previo no duplico mantenimientos", scalar("select TechnicianId from Maintenances where Id=?", plan(pc)["mnt"]) == P.TECH_EXT and scalar("select count(*) from Maintenances where EquipmentUnitId=?", uc[0]) == 1)
    st, html = c.get(f"/Acquisitions/Details/{plan(pa)['acq']}")
    check(R, "Acquisitions/Details de la compra de A: 200", st == 200, f"status={st}")
    st, html = c.get(f"/Maintenances/Details?id={plan(pa)['mnt']}")
    check(R, "Maintenances/Details del L-8 de A: 200", st == 200, f"status={st}")
    st, html = c.get(f"/Departures/Details?id={plan(pa)['dep']}")
    check(R, "Departures/Details de la salida de A: 200", st == 200, f"status={st}")
    st, html = c.get(f"/Requests/Details?id={plan(pa)['req']}")
    check(R, "Requests/Details de la L-7 de A: 200", st == 200, f"status={st}")
    st, html = c.get(f"/Verifications/Details?id={plan(pa)['ver']}")
    check(R, "Verifications/Details de la L-6 de A: 200", st == 200, f"status={st}")
    st, html = c.get(f"/EquipmentUnits/Kardex/{ua[0]}")
    check(R, "EquipmentUnits/Kardex de la unidad A: 200", st == 200, f"status={st}")


if __name__ == "__main__":
    P.run_module("flujo", run)
