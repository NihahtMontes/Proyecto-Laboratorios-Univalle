"""Verificaciones L-6: MassCreate / Create / Edit / Delete / Details / Index. Solo contra la copia QA."""
import os

import http_cases_procesos_common as P
from http_cases_procesos_common import check, expect, sql, scalar, plan, unidad, post_form, post_raw, fase


def ver(vid):
    r = sql("select Status,PhysicalCondition,Observations,EquipmentUnitId,ManagementId from Verifications where Id=?", vid)
    return dict(zip(["status", "cond", "obs", "unit", "mgmt"], r[0])) if r else None


def req(rid):
    r = sql("select Status,Type,Priority,Description,EquipmentUnitId from Requests where Id=?", rid)
    return dict(zip(["status", "type", "prio", "desc", "unit"], r[0])) if r else None


def n_ver(unit):
    return scalar("select count(*) from Verifications where EquipmentUnitId=?", unit)


def run(R, c):
    pw = os.environ["E2E_PW"]
    lab = P.lab_propio(c, "verificaciones")
    lab2 = P.lab_propio(c, "verificaciones B")

    # ---------------------------------------------------------------- fixtures
    us = {}
    for k in ("g", "x", "r", "m", "b", "d", "o", "w", "z"):
        us[k] = P.nueva_unidad(c, lab, "v" + k)
    u_otro_lab, _ = P.nueva_unidad(c, lab2, "vl")
    planes = P.planificar(c, [u for u, _ in us.values()] + [u_otro_lab])
    pl = {k: planes[us[k][0]] for k in us}
    pl_lab2 = planes[u_otro_lab]
    for k in pl:
        fase(R, f"Fixture plan {k}", pl[k], 1, 1, 0)

    # ================================================================ MassCreate
    q = f"/Verifications/MassCreate?isWizard=true&labId={lab}&managementId={P.MGMT}"
    st, pg = c.get(q)
    check(R, "MassCreate: GET wizard 200", st == 200 and "Verificaci" in pg, f"status={st}")
    st, js = c.get(f"/Verifications/MassCreate?handler=EquipmentByLab&labId={lab}&managementId={P.MGMT}")
    check(R, "MassCreate: handler EquipmentByLab lista los planes pendientes del laboratorio", st == 200 and all(f'"planId":{p}' in js for p in pl.values()), f"status={st} {js[:200]}")
    st, js = c.get(f"/Verifications/MassCreate?handler=EquipmentByLab&labId={lab}&managementId=27")
    check(R, "MassCreate: EquipmentByLab con gestion historica no devuelve planes propios", st == 200 and f'"planId":{pl["g"]}' not in js, f"status={st} {js[:150]}")

    r = post_raw(c, P.with_handler(q, None), [("IsWizard", "true"), ("LabId", lab), ("ManagementId", P.MGMT)], q, follow=True, name="sin filas")
    check(R, "MassCreate: sin filas -> 'No hay equipos para procesar.'", r.has_toast("No hay equipos para procesar.", "error"), P.dump(r))
    r = P.l6(c, pl["g"], us["g"][0], lab, mgmt=27, name="gestion cerrada", follow=True)
    check(R, "MassCreate: gestion historica/cerrada (27) -> 'Seleccione una gestión preventiva activa para registrar L-6.'", r.has_toast("Seleccione una gestión preventiva activa para registrar L-6.", "error"), P.dump(r))
    r = P.l6(c, pl["g"], us["g"][0], lab, mgmt=99999, name="gestion inexistente", follow=True)
    check(R, "MassCreate: gestion inexistente -> mismo mensaje y sin 500", r.status < 500 and r.has_toast("gestión preventiva activa", "error"), P.dump(r))
    r = P.l6(c, 0, 0, lab, filas=[(pl["g"], us["g"][0], 4, ""), (pl["g"], us["g"][0], 5, "")], name="plan duplicado", follow=True)
    check(R, "MassCreate: el mismo plan dos veces -> 'envíe cada plan L-6 una sola vez'", r.has_toast("envíe cada plan L-6 una sola vez", "error"), P.dump(r))
    r = P.l6(c, 0, 0, lab, filas=[(0, us["g"][0], 4, "")], name="plan 0", follow=True)
    check(R, "MassCreate: PlanId 0 -> 'envíe cada plan L-6 una sola vez'", r.has_toast("envíe cada plan L-6 una sola vez", "error"), P.dump(r))
    r = P.l6(c, pl["b"], us["b"][0], lab, cond=2, obs="", name="malo sin obs", follow=True)
    check(R, "MassCreate: 'Malo' sin observacion -> 'Describa las fallas de cada equipo marcado en estado Malo antes de guardar L-6.'", r.has_toast("Describa las fallas de cada equipo marcado en estado Malo antes de guardar L-6.", "error"), P.dump(r))
    r = P.l6(c, pl["b"], us["b"][0], lab, cond=2, obs="    ", name="malo obs en blanco", follow=True)
    check(R, "MassCreate: 'Malo' con observacion solo espacios -> mismo error", r.has_toast("Describa las fallas", "error"), P.dump(r))
    check(R, "MassCreate: ninguna validacion fallida escribio verificaciones", n_ver(us["b"][0]) == 0 and n_ver(us["g"][0]) == 0)
    r = P.l6(c, pl["g"], us["x"][0], lab, name="unidad no coincide", follow=True)
    check(R, "MassCreate: unidad que no corresponde al plan -> 'Los equipos o la fase de L-6 cambiaron...'", r.has_toast("Los equipos o la fase de L-6 cambiaron. Recargue el formulario antes de guardar.", "error"), P.dump(r))
    r = P.l6(c, pl["g"], us["g"][0], lab, cond=77, name="condicion 77", follow=True)
    check(R, "MassCreate: condicion fisica inexistente (77) -> 'cambiaron'", r.has_toast("Los equipos o la fase de L-6 cambiaron", "error"), P.dump(r))
    r = P.l6(c, pl["g"], us["g"][0], lab2, name="laboratorio equivocado", follow=True)
    check(R, "MassCreate: plan de otro laboratorio -> 'cambiaron'", r.has_toast("Los equipos o la fase de L-6 cambiaron", "error"), P.dump(r))
    r = P.l6(c, pl_lab2, u_otro_lab, lab, name="plan de otro lab", follow=True)
    check(R, "MassCreate: plan ajeno al laboratorio enviado -> 'cambiaron'", r.has_toast("Los equipos o la fase de L-6 cambiaron", "error"), P.dump(r))
    r = P.l6(c, 999999, us["g"][0], lab, name="plan inexistente", follow=True)
    check(R, "MassCreate: plan inexistente -> 'cambiaron' y sin 500", r.status < 500 and r.has_toast("cambiaron", "error"), P.dump(r))
    r = P.l6(c, pl["g"], us["g"][0], 0, name="sin laboratorio", follow=True)
    check(R, "MassCreate: laboratorio 0 -> sin 500 y con mensaje de error", r.status < 500 and bool(r.toasts), P.dump(r))
    # plan ajeno (otra fase >1 de la gestion activa o, si no hay, el de otro laboratorio): solo lectura, sin escribir
    ajeno = scalar("select top 1 Id from ManagementPlans where ManagementId=? and CurrentPhase>1 order by Id", P.MGMT)
    if not ajeno:
        ajeno = pl_lab2
    u_aj = scalar("select EquipmentUnitId from ManagementPlans where Id=?", ajeno)
    p_antes, n_antes = plan(ajeno), n_ver(u_aj)
    r = P.l6(c, ajeno, u_aj, lab, name=f"plan ajeno (id {ajeno})", follow=True)
    check(R, f"MassCreate: plan de otro laboratorio/fase (plan #{ajeno}, solo lectura) rechazado sin escribir",
          r.status < 500 and (r.has_toast("cambiaron", "error") or any(k == "error" for k, _ in r.toasts))
          and plan(ajeno) == p_antes and n_ver(u_aj) == n_antes,
          P.dump(r) + f" antes={p_antes} despues={plan(ajeno)} verificaciones={n_antes}->{n_ver(u_aj)}")

    # borrador
    r = P.l6(c, pl["d"], us["d"][0], lab, cond=3, draft=True, name="borrador", follow=True)
    check(R, "MassCreate: borrador -> 'Borrador L-6 guardado para 1 equipo(s).'", r.has_toast("Borrador L-6 guardado para 1 equipo(s).", "success"), P.dump(r))
    p = plan(pl["d"])
    check(R, "MassCreate borrador: plan sigue en L-6 (fase 1, PendingVerification), IsDraft=1, DraftPhase=1, PlanStatus=InProgress", p["phase"] == 1 and p["state"] == 1 and p["draft"] == 1 and p["draftphase"] == 1 and p["pstatus"] == 1, f"{p}")
    check(R, "MassCreate borrador: verificacion en estado Draft (0)", p["ver"] and ver(p["ver"])["status"] == 0, f"{p['ver'] and ver(p['ver'])}")
    r = P.l6(c, pl["d"], us["d"][0], lab, cond=2, obs="", draft=True, name="borrador malo sin obs", follow=True)
    check(R, "MassCreate borrador: 'Malo' sin observacion tambien se rechaza en borrador", r.has_toast("Describa las fallas", "error"), P.dump(r))
    r = P.l6(c, pl["d"], us["d"][0], lab, cond=3, draft=True, name="borrador repetido", follow=True)
    check(R, "MassCreate borrador: reguardar el borrador reutiliza la misma verificacion (no duplica)", r.has_toast("Borrador L-6", "success") and n_ver(us["d"][0]) == 1, P.dump(r) + f" n={n_ver(us['d'][0])}")

    # guardado definitivo masivo: bueno, excelente(=x), regular, malo, 'Baja' y borrador previo
    obs_larga = "Falla " + "x" * 600
    filas = [(pl["g"], us["g"][0], 4, "ignorada en sano"), (pl["x"], us["x"][0], 5, ""), (pl["r"], us["r"][0], 3, ""),
             (pl["m"], us["m"][0], 2, obs_larga), (pl["d"], us["d"][0], 4, ""), (pl["o"], us["o"][0], 1, "")]
    r = P.l6(c, 0, 0, lab, filas=filas, name="guardado masivo", follow=True)
    check(R, "MassCreate: guardado definitivo -> '6 verificaciones registradas (1 equipos con fallas).'", r.has_toast("6 verificaciones registradas (1 equipos con fallas).", "success"), P.dump(r))
    check(R, "MassCreate: redirige al paso 2 del wizard (hay fallas)", r.status == 302 and "Step=2" in (r.location or ""), P.dump(r))
    fase(R, "MassCreate bueno (4)", pl["g"], 1, 2, 0, False)
    fase(R, "MassCreate excelente (5)", pl["x"], 1, 2, 0, False)
    fase(R, "MassCreate regular (3): cuenta como sano", pl["r"], 1, 2, 0, False)
    fase(R, "MassCreate borrador luego definitivo", pl["d"], 1, 2, None, False)
    fase(R, "MassCreate 'Baja del equipo' (1): queda como sano (VerifiedGood); la unidad no cambia de estado", pl["o"], 1, 2, 0, False)
    check(R, "MassCreate: la unidad 'Baja' no cambia de estado/condicion en el inventario", unidad(us["o"][0])["status"] == 0 and unidad(us["o"][0])["cond"] == 5, f"{unidad(us['o'][0])}")
    pm = plan(pl["m"])
    check(R, "MassCreate 'Malo': plan en TechnicalRequest/AwaitingRequest (fase 2 / estado 3) con L-7 creada", pm["phase"] == 2 and pm["state"] == 3 and pm["req"] is not None, f"{pm}")
    rq = req(pm["req"]) if pm["req"] else {}
    check(R, "MassCreate 'Malo': L-7 tecnica Pendiente, prioridad Media, de la unidad, descripcion recortada a 500 con '...'",
          rq.get("status") == 0 and rq.get("type") == 1 and rq.get("prio") == 1 and rq.get("unit") == us["m"][0] and len(rq.get("desc", "")) == 500 and rq["desc"].endswith("..."), f"{ {k: (v if k != 'desc' else len(v)) for k, v in rq.items()} }")
    vm = ver(pm["ver"])
    check(R, "MassCreate 'Malo': verificacion WithObservations(2), condicion 2, observaciones completas", vm["status"] == 2 and vm["cond"] == 2 and vm["obs"] == obs_larga, f"{vm['status']} {vm['cond']} len={len(vm['obs'] or '')}")
    vg = ver(plan(pl["g"])["ver"])
    check(R, "MassCreate sano: verificacion Completed(1), condicion 4 y observacion descartada", vg["status"] == 1 and vg["cond"] == 4 and vg["obs"] is None, f"{vg}")
    check(R, "MassCreate: ninguna L-7 para los equipos sanos", all(plan(pl[k])["req"] is None for k in ("g", "x", "r", "d", "o")))

    # doble envio / reenvio
    r = P.l6(c, pl["m"], us["m"][0], lab, cond=2, obs="otra falla", name="doble envio malo", follow=True)
    check(R, "MassCreate doble envio (plan ya en L-7): 'Los equipos o la fase de L-6 cambiaron...'", r.has_toast("Los equipos o la fase de L-6 cambiaron. Recargue el formulario antes de guardar.", "error"), P.dump(r))
    r = P.l6(c, pl["g"], us["g"][0], lab, cond=4, name="doble envio sano", follow=True)
    check(R, "MassCreate doble envio (plan sano ya verificado): 'Una verificación L-6 ya fue finalizada...'", r.has_toast("Una verificación L-6 ya fue finalizada o no pertenece a este plan. Recargue el formulario.", "error"), P.dump(r))
    check(R, "MassCreate: el doble envio no duplico verificaciones ni solicitudes", n_ver(us["m"][0]) == 1 and n_ver(us["g"][0]) == 1 and scalar("select count(*) from Requests where EquipmentUnitId=? and Type=1", us["m"][0]) == 1)
    # lote mixto con una fila invalida: todo o nada
    r = P.l6(c, 0, 0, lab, filas=[(pl["w"], us["w"][0], 4, ""), (pl["z"], us["x"][0], 4, "")], name="lote mixto", follow=True)
    check(R, "MassCreate: lote con una fila invalida -> no guarda ninguna (todo o nada)", r.has_toast("cambiaron", "error") and n_ver(us["w"][0]) == 0 and plan(pl["w"])["state"] == 1, P.dump(r) + f" {plan(pl['w'])}")
    # no wizard
    r = P.l6(c, pl["w"], us["w"][0], lab, cond=5, name="no wizard", follow=True, wizard=False)
    check(R, "MassCreate fuera del wizard: guarda y vuelve a Verifications (Index)", r.status == 302 and "/Verifications" in (r.location or "") and r.has_toast("1 verificaciones registradas (0 equipos con fallas).", "success"), P.dump(r))

    # ================================================================ Index / Details
    st, pg = c.get("/Verifications")
    check(R, "Index: sin laboratorio -> 'Seleccione un laboratorio'", st == 200 and "Seleccione un laboratorio" in pg, f"status={st}")
    st, pg = c.get(f"/Verifications?FilterLaboratoryId={lab}")
    check(R, "Index: con laboratorio muestra sesiones", st == 200 and "Sin sesiones registradas" not in pg, f"status={st}")
    st, pg = c.get(f"/Verifications?FilterLaboratoryId={P.lab_propio(c, 'verificaciones vacio')}")
    check(R, "Index: laboratorio sin verificaciones -> 'Sin sesiones registradas'", st == 200 and "Sin sesiones registradas" in pg, f"status={st}")
    vid_g = plan(pl["g"])["ver"]
    st, pg = c.get(f"/Verifications/Details?id={vid_g}")
    check(R, "Details: verificacion individual 200", st == 200 and us["g"][1] in pg, f"status={st}")
    import datetime
    hoy = datetime.date.today().isoformat()
    st, pg = c.get(f"/Verifications/Details?LabId={lab}&Date={hoy}")
    check(R, "Details: vista de sesion (LabId+Date) 200 con 'Sesión de Verificación L-6'", st == 200 and "Sesi" in pg and "Verificaci" in pg, f"status={st}")
    st, pg = c.get(f"/Verifications/Details?LabId={lab}&Date=1999-01-01")
    check(R, "Details: sesion sin datos -> 404", st == 404, f"status={st}")
    st, _ = c.get("/Verifications/Details?id=99999")
    check(R, "Details: id inexistente -> 404", st == 404, f"status={st}")
    st, _ = c.get("/Verifications/Details?id=abc")
    check(R, "Details: id no numerico -> 404", st == 404, f"status={st}")

    # ================================================================ Create (individual, solo URL)
    uc1 = P.nueva_unidad(c, lab, "c1")
    uc2 = P.nueva_unidad(c, lab, "c2")
    uc3 = P.nueva_unidad(c, lab, "c3")
    pc = P.planificar(c, [uc1[0], uc2[0], uc3[0]])
    def cv(u, **kw):
        d = {"Input.FacultyId": str(P.FAC), "Input.LaboratoryId": str(lab), "Input.EquipmentUnitId": str(u)}
        d.update(kw)
        return d
    cp = f"/Verifications/Create?ManagementPlanId={pc[uc1[0]]}&ManagementId={P.MGMT}"
    st, _ = c.get(cp)
    check(R, "Create: GET con plan 200", st == 200, f"status={st}")
    r = post_form(c, "/Verifications/Create", {"Input.FacultyId": None, "Input.LaboratoryId": None, "Input.EquipmentUnitId": None}, name="create vacio", follow=False)
    check(R, "Create: vacio (sin facultad/laboratorio/unidad) no produce 500", r.status < 500, P.dump(r))
    check(R, "Create: vacio -> rechazo con 'La unidad física es obligatoria' visible", r.status == 200 and r.has_error("La unidad física es obligatoria"), P.dump(r))
    r = post_form(c, cp, cv(99999), name="create unidad inexistente", follow=False)
    check(R, "Create: unidad inexistente no produce 500", r.status < 500, P.dump(r))
    r = post_form(c, cp, cv(uc1[0], **{"Input.Results[99999]": "1"}), name="create check inexistente", follow=False)
    check(R, "Create: resultado de un punto de control inexistente no produce 500", r.status < 500, P.dump(r))
    check(R, "Create: el fallo al guardar los resultados no deja una verificacion huerfana (guardado no transaccional)", n_ver(uc1[0]) == 0, f"verificaciones de la unidad={n_ver(uc1[0])}")
    r = post_form(c, cp, cv(uc1[0]), name="create falla larga", follow=False, extra=[("Input.FaultDescriptions", "f" * 300)])
    check(R, "Create: descripcion de falla de 300 caracteres (maximo 255) no produce 500", r.status < 500, P.dump(r))
    check(R, "Create: las validaciones fallidas no tocaron el plan", plan(pc[uc1[0]])["state"] == 1 and plan(pc[uc1[0]])["ver"] is None, f"{plan(pc[uc1[0]])}")
    r = post_form(c, cp, cv(uc1[0]), name="create sano", follow=True)
    check(R, "Create: sin fallas -> 302 a Verifications y 'Verificación registrada correctamente.'", r.status == 302 and r.has_toast("Verificación registrada correctamente.", "success"), P.dump(r))
    fase(R, "Create sano", pc[uc1[0]], 1, 2, 0)
    n1 = n_ver(uc1[0])
    r = post_form(c, cp, cv(uc1[0]), name="create sano otra vez", follow=True)
    check(R, "Create: segundo envio para el mismo plan reutiliza la verificacion (no suma otra)", n_ver(uc1[0]) == n1, f"antes={n1} despues={n_ver(uc1[0])} {P.dump(r)}")
    cp2 = f"/Verifications/Create?ManagementPlanId={pc[uc2[0]]}&ManagementId={P.MGMT}"
    r = post_form(c, cp2, cv(uc2[0]), name="create con fallas", follow=True, extra=[("Input.FaultDescriptions", "Ruido en motor"), ("Input.FaultDescriptions", "  "), ("Input.FaultDescriptions", "Fuga")])
    p2 = plan(pc[uc2[0]])
    check(R, "Create con fallas: plan a TechnicalRequest/AwaitingRequest (2/3) y verificacion WithObservations", p2["phase"] == 2 and p2["state"] == 3 and ver(p2["ver"])["status"] == 2 and ver(p2["ver"])["obs"] == "Ruido en motor | Fuga", f"{p2} {ver(p2['ver']) if p2['ver'] else None}")
    check(R, "Create con fallas: NO crea la L-7 automaticamente (a diferencia de MassCreate): plan sin RequestId", p2["req"] is None, f"{p2}")
    cp3 = f"/Verifications/Create?ManagementPlanId={pc[uc3[0]]}&ManagementId={P.MGMT}"
    r = post_form(c, cp3, cv(uc3[0]), handler="Draft", name="create borrador", follow=True)
    p3 = plan(pc[uc3[0]])
    check(R, "Create borrador: plan queda en borrador L-6 (fase 1 / estado 1 / IsDraft)", p3["phase"] == 1 and p3["state"] == 1 and p3["draft"] == 1 and r.has_toast("Borrador", "success"), f"{p3} {P.dump(r)}")

    # regresion de fase: Create sobre un plan que ya esta mas adelante
    adv = P.llevar_a(c, lab, "va", "P4")
    adv_cp = f"/Verifications/Create?ManagementPlanId={adv['plan']}&ManagementId={P.MGMT}"
    antes = plan(adv["plan"])
    r = post_form(c, adv_cp, cv(adv["unit"]), name="create sobre plan avanzado (P4)", follow=False, extra=[("Input.FaultDescriptions", "Falla nueva")])
    desp = plan(adv["plan"])
    check(R, "Create L-6 sobre un plan que ya esta en L-3 (fase 4): NO debe retroceder la fase", desp["phase"] == antes["phase"] and desp["state"] == antes["state"], f"antes={antes['phase']}/{antes['state']} despues={desp['phase']}/{desp['state']} {P.dump(r)}")

    # ================================================================ Edit
    adv2 = P.llevar_a(c, lab, "vb", "P4")
    adv3 = P.llevar_a(c, lab, "vc", "P4")
    st, _ = c.get("/Verifications/Edit")
    check(R, "Edit: GET sin id -> 404", st == 404, f"status={st}")
    st, _ = c.get("/Verifications/Edit?id=99999")
    check(R, "Edit: GET id inexistente -> 404", st == 404, f"status={st}")
    vm_id = plan(pl["m"])["ver"]
    ep = f"/Verifications/Edit?id={vm_id}&ManagementId={P.MGMT}"
    st, _ = c.get(ep)
    check(R, "Edit: GET 200", st == 200, f"status={st}")
    r = post_form(c, ep, {"Input.Date": ""}, name="edit sin fecha", follow=False)
    check(R, "Edit: fecha vacia rechazada con mensaje", r.status == 200 and bool(r.errors) and not P._EN.search(" ".join(r.errors)), P.dump(r))
    r = post_form(c, ep, {"Input.EquipmentUnitId": "99999"}, name="edit unidad inexistente", follow=False)
    check(R, "Edit: unidad inexistente no produce 500", r.status < 500, P.dump(r))
    r = post_form(c, ep, {"Input.PhysicalCondition": "77"}, name="edit condicion 77", follow=False)
    check(R, "Edit: condicion fisica inexistente (77) se rechaza", r.status == 200 and ver(vm_id)["cond"] == 2, P.dump(r) + f" cond={ver(vm_id)['cond']}")
    r = post_form(c, ep, {"Input.Id": "99999"}, name="edit Id inexistente", follow=False)
    check(R, "Edit: POST con Input.Id inexistente -> 404", r.status == 404, P.dump(r))
    # Malo -> Regular: desde 2026-10-18 «Regular» es sano en Edit igual que en MassCreate (solo «Malo» es falla)
    rid_m = plan(pl["m"])["req"]
    r = post_form(c, ep, {"Input.PhysicalCondition": "3"}, name="edit regular", follow=True)
    pmn = plan(pl["m"])
    check(R, "Edit Regular: verificacion Completed (1) -> 'Regular' es sano como en MassCreate", ver(vm_id)["status"] == 1, f"{ver(vm_id)}")
    check(R, "Edit Regular vs MassCreate Regular: mismo criterio (en MassCreate Regular=sano) -> inconsistencia", ver(plan(pl['r'])['ver'])["status"] == ver(vm_id)["status"], f"MassCreate Regular={ver(plan(pl['r'])['ver'])['status']} Edit Regular={ver(vm_id)['status']}")
    # Regular -> Bueno: sigue sano; la L-7 pendiente del estado Malo quedó cancelada
    r = post_form(c, ep, {"Input.PhysicalCondition": "4"}, name="edit bueno", follow=True)
    check(R, "Edit Bueno: 302 con 'Verificacion #id actualizada. El activo fue movido a activos sanos.'", r.status == 302 and r.has_toast(f"Verificacion #{vm_id} actualizada. El activo fue movido a activos sanos.", "success"), P.dump(r))
    pmn = plan(pl["m"])
    check(R, "Edit Bueno: verificacion Completed(1), plan VerifiedGood (1/2), L-7 pendiente Cancelada y desvinculada", ver(vm_id)["status"] == 1 and pmn["phase"] == 1 and pmn["state"] == 2 and pmn["req"] is None and scalar("select Status from Requests where Id=?", rid_m) == 99, f"{ver(vm_id)} {pmn} req={scalar('select Status from Requests where Id=?', rid_m)}")
    # Bueno -> Malo sin observaciones (MassCreate lo exige)
    r = post_form(c, ep, {"Input.PhysicalCondition": "2", "Input.Observations": ""}, name="edit malo sin obs", follow=True)
    check(R, "Edit Malo sin observaciones: se rechaza como en MassCreate ('Describa las fallas...')", r.status == 200 or not (ver(vm_id)["status"] == 2 and not ver(vm_id)["obs"]), f"{ver(vm_id)} {P.dump(r)}")
    r = post_form(c, ep, {"Input.PhysicalCondition": "2", "Input.Observations": "Falla de compresor QAP"}, name="edit malo con obs", follow=True)
    pmn = plan(pl["m"])
    check(R, "Edit Malo (con observaciones): plan a TechnicalRequest/AwaitingRequest sin L-7 vinculada (queda a la espera de L-7 manual)", pmn["phase"] == 2 and pmn["state"] == 3 and pmn["req"] is None, f"{pmn}")
    # Edit sobre plan avanzado
    adv_v = plan(adv2["plan"])["ver"]
    av = f"/Verifications/Edit?id={adv_v}&ManagementId={P.MGMT}"
    antes = plan(adv2["plan"])
    r = post_form(c, av, {"Input.PhysicalCondition": "4"}, name="edit sano sobre plan avanzado", follow=False)
    desp = plan(adv2["plan"])
    check(R, "Edit L-6 'Bueno' sobre un plan ya en L-3: NO debe retroceder a VerifiedGood (dejaria L-7/L-8 huerfanos)", desp["phase"] == antes["phase"] and desp["state"] == antes["state"], f"antes={antes['phase']}/{antes['state']} despues={desp['phase']}/{desp['state']} mnt={desp['mnt']}")

    # ================================================================ Delete
    st, _ = c.get(f"/Verifications/Delete/{vid_g}")
    check(R, "Delete: GET 200", st == 200, f"status={st}")
    st, _ = c.get("/Verifications/Delete/99999")
    check(R, "Delete: GET id inexistente -> 404", st == 404, f"status={st}")
    r = P.post_raw(c, f"/Verifications/Delete/{vid_g}", [], f"/Verifications/Delete/{vid_g}", name="delete")
    expect(R, "Delete: anula la verificacion", r, status=302, toast=f"Verificación #{vid_g} eliminada del sistema.", toast_kind="success")
    check(R, "Delete: queda Annulled (99) y el plan vuelve a Pendiente de verificación (1/1) sin apuntar a la anulada (2026-10-18)", ver(vid_g)["status"] == 99 and plan(pl["g"])["phase"] == 1 and plan(pl["g"])["state"] == 1 and plan(pl["g"])["ver"] is None, f"{ver(vid_g)} {plan(pl['g'])}")
    anulada = ver(vid_g)["status"] == 99
    if anulada:   # las comprobaciones dependientes solo tienen sentido si la baja logica se aplico
        st, _ = c.get(f"/Verifications/Details?id={vid_g}")
        check(R, "Delete: Details de una verificacion anulada -> 404", st == 404, f"status={st}")
        r = P.post_raw(c, f"/Verifications/Delete/{vid_g}", [], "/Verifications", name="delete repetido")
        check(R, "Delete repetido sobre una verificacion ya anulada: no repite el mensaje de exito", not r.has_toast("eliminada del sistema", "success"), P.dump(r))
    r = P.post_raw(c, "/Verifications/Delete/99999", [], "/Verifications", name="delete inexistente")
    check(R, "Delete: id inexistente -> 404 o aviso (no silencioso, no 500)", r.status < 500 and (r.status == 404 or bool(r.toasts)), P.dump(r))
    if anulada:
        r = post_form(c, f"/Verifications/Edit?id={vid_g}", {"Input.Id": str(vid_g)}, name="edit de anulada", follow=False)
        check(R, "Edit: no se puede editar una verificacion anulada (debe dar 404 y seguir anulada)", r.status == 404 and ver(vid_g)["status"] == 99, P.dump(r) + f" estado={ver(vid_g)['status']}")
    # borrado de una verificacion de un plan avanzado
    adv_v = plan(adv3["plan"])["ver"]
    r = P.post_raw(c, f"/Verifications/Delete/{adv_v}", [], "/Verifications", name="delete verificacion de plan avanzado")
    check(R, "Delete: anular la L-6 de un plan que ya esta en L-3 -> debe bloquearse o avisar (no confirmar un exito falso)", not r.has_toast("eliminada del sistema", "success"), P.dump(r) + f" ver={ver(adv_v)['status']}")

    # ================================================================ acceso
    for pth in ("/Verifications", q, cp, f"/Verifications/Edit?id={vm_id}", f"/Verifications/Delete/{vm_id}", f"/Verifications/Details?id={vm_id}"):
        P.check_anon(R, "Verifications", pth)
    P.check_anon(R, "Verifications", q, post=True)
    P.tokenless(R, c, "Verifications/MassCreate", q, [("IsWizard", "true"), ("LabId", str(lab)), ("ManagementId", str(P.MGMT)), ("Rows[0].PlanId", pl["w"]), ("Rows[0].EquipmentUnitId", us["w"][0]), ("Rows[0].Condition", "4")])
    P.tokenless(R, c, "Verifications/Delete", f"/Verifications/Delete/{vm_id}", [])
    P.tokenless(R, c, "Verifications/Edit", ep, [("Input.Id", str(vm_id)), ("Input.PhysicalCondition", "2")])
    for usuario, rol in (("qa_sup", "Supervisor"), ("qa_cierre", "PracticeClosure")):
        cl = P.client(usuario, pw)
        st, loc = cl.probe(q)
        check(R, f"Rol {rol}: GET MassCreate denegado", st in (302, 403) and "/Login" not in (loc or ""), f"status={st} loc={loc}")
        r = post_raw(cl, q, [("IsWizard", "true"), ("LabId", lab), ("ManagementId", P.MGMT), ("Rows[0].PlanId", pl["w"]), ("Rows[0].EquipmentUnitId", us["w"][0]), ("Rows[0].Condition", "4")], "/", follow=False, name=f"masscreate como {rol}")
        check(R, f"Rol {rol}: POST MassCreate denegado y sin escritura", r.status in (302, 403) and plan(pl["w"])["state"] == 2 and n_ver(us["w"][0]) == 1, P.dump(r))  # w ya quedo VerifiedGood por la prueba no-wizard
        r = post_raw(cl, f"/Verifications/Delete/{vm_id}", [], "/", follow=False, name=f"delete como {rol}")
        check(R, f"Rol {rol}: POST Delete denegado", r.status in (302, 403) and ver(vm_id)["status"] != 99, P.dump(r))


if __name__ == "__main__":
    P.run_module("verifications", run)
