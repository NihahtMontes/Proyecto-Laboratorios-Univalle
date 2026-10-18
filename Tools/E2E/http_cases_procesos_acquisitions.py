"""Adquisiciones L-12: Create / Edit / Delete / Details / Index. Solo contra la copia QA."""
import os

import http_cases_procesos_common as P
from http_cases_procesos_common import check, expect, sql, scalar, plan, unidad, post_form, post_raw, fase


def rq(rid):
    r = sql("select Status,Type,Priority,Description,Observations,InvestmentCode,CostCenter,EquipmentUnitId,ManagementId,RejectionReason from Requests where Id=?", rid)
    return dict(zip(["status", "type", "prio", "desc", "obs", "inv", "cc", "unit", "mgmt", "rej"], r[0])) if r else None


def n_acq(unit):
    return scalar("select count(*) from Requests where EquipmentUnitId=? and Type=2", unit)


def n_cost_req(rid):
    return scalar("select count(*) from CostDetails where RequestId=?", rid)


def n_notif(inv):
    return scalar("select count(*) from Notifications where Title=? and Message like ?", "Adquisición Solicitada", f"%{inv}%")


def run(R, c):
    pw = os.environ["E2E_PW"]
    lab = P.lab_propio(c, "adquisiciones")
    ctx = {}
    for k in ("a", "b", "c", "d", "e", "f", "g", "h"):
        ctx[k] = P.llevar_a(c, lab, "q" + k, "P6")
    p5 = P.llevar_a(c, lab, "q5", "P5")
    p2 = P.llevar_a(c, lab, "q2", "P2")
    p1ok = P.llevar_a(c, lab, "q1", "P1OK")
    p7 = P.llevar_a(c, lab, "q7", "P7")
    p7b = P.llevar_a(c, lab, "q7b", "P7")
    p7c = P.llevar_a(c, lab, "q7c", "P7")
    for k in ctx:
        fase(R, f"Fixture {k} en L-12", ctx[k]["plan"], 6, 8, 1)
    pid = lambda k: ctx[k]["plan"]
    uid = lambda k: ctx[k]["unit"]
    inv = lambda k: ctx[k]["inv"]
    pg = lambda k: f"/Acquisitions/Create?isWizard=true&managementPlanId={pid(k)}"

    # ================================================================ Create
    st, _ = c.get(pg("a"))
    check(R, "Create: GET wizard 200", st == 200, f"status={st}")
    for campo, msg in (("Input.Description", "La justificación es obligatoria"), ("Input.InvestmentCode", "El código de inversión es obligatorio"), ("Input.CostCenter", "El centro de costos es obligatorio")):
        r = P.l12(c, pid("a"), {campo: ""}, name=f"sin {campo}", follow=True)
        check(R, f"Create: sin {campo.split('.')[-1]} -> 'No se pudo guardar L-12: ... {msg}'", r.status == 200 and r.has_toast("No se pudo guardar L-12:", "error") and r.has_toast(msg, "error"), P.dump(r))
    r = P.l12(c, pid("a"), {"Input.Description": "", "Input.InvestmentCode": "", "Input.CostCenter": ""}, name="todo vacio", follow=True)
    check(R, "Create: los tres obligatorios vacios -> los tres mensajes", all(r.has_toast(m, "error") for m in ("La justificación es obligatoria", "El código de inversión es obligatorio", "El centro de costos es obligatorio")), P.dump(r))
    r = P.l12(c, pid("a"), {"Input.Description": "d" * 1001}, name="justificacion 1001", follow=True)
    check(R, "Create: justificacion de 1001 -> 'La justificación no puede superar los 1000 caracteres'", r.status == 200 and r.has_toast("La justificación no puede superar los 1000 caracteres", "error"), P.dump(r))
    r = P.l12(c, pid("a"), {"Input.Observations": "o" * 501}, name="observaciones 501", follow=True)
    check(R, "Create: observaciones de 501 -> 'Las observaciones no pueden superar los 500 caracteres'", r.status == 200 and r.has_toast("Las observaciones no pueden superar los 500 caracteres", "error"), P.dump(r))
    r = P.l12(c, pid("a"), {"Input.InvestmentCode": "i" * 51}, name="codigo 51", follow=True)
    check(R, "Create: codigo de inversion de 51 -> mensaje en espanol", r.status == 200 and bool(r.toasts) and not P._EN.search(P.toast_text(r)), P.dump(r))
    r = P.l12(c, pid("a"), {"Input.CostCenter": "c" * 101}, name="centro 101", follow=True)
    check(R, "Create: centro de costos de 101 -> mensaje en espanol", r.status == 200 and bool(r.toasts) and not P._EN.search(P.toast_text(r)), P.dump(r))
    r = P.l12(c, 99999, name="plan inexistente", follow=True)
    check(R, "Create: plan inexistente -> 'No se pudo resolver el plan de gestión para L-12. ManagementPlanId recibido: 99999.'", r.status == 200 and r.has_toast("ManagementPlanId recibido: 99999", "error"), P.dump(r))
    check(R, "Create: las validaciones fallidas no tocaron el plan ni crearon solicitudes", plan(pid("a"))["state"] == 8 and plan(pid("a"))["acq"] is None and n_acq(uid("a")) == 0)

    # borrador
    r = P.l12(c, pid("b"), {"Input.Description": "", "Input.InvestmentCode": "", "Input.CostCenter": ""}, draft=True, name="borrador", follow=True)
    pb = plan(pid("b"))
    check(R, "Create borrador: 'Borrador L-12 para ... guardado correctamente.' sin exigir campos, paso 6", r.status == 302 and "Step=6" in (r.location or "") and r.has_toast(f"Borrador L-12 para '{inv('b')}' guardado correctamente.", "success"), P.dump(r))
    check(R, "Create borrador: plan sigue en L-12 (6/8) en borrador; sin notificacion", pb["phase"] == 6 and pb["state"] == 8 and pb["draft"] == 1 and pb["draftphase"] == 6 and pb["acq"] and n_notif(inv("b")) == 0, f"{pb} notif={n_notif(inv('b'))}")
    check(R, "Create borrador: la solicitud queda con justificacion marcador", rq(pb["acq"])["desc"] == "Borrador L-12 pendiente de justificación.", f"{rq(pb['acq'])}")

    # valido
    nom = inv("a")
    r = P.l12(c, pid("a"), {"Input.Description": "Justificacion de compra QAP", "Input.Observations": "Obs compra QAP"}, name="valido", follow=True)
    pa = plan(pid("a"))
    check(R, "Create: 'Solicitud de Adquisición para ... registrada exitosamente. El mantenimiento quedó completado en el wizard.' y paso 7", r.status == 302 and "Step=7" in (r.location or "") and r.has_toast(f"Solicitud de Adquisición para '{nom}' registrada exitosamente. El mantenimiento quedó completado en el wizard.", "success"), P.dump(r))
    check(R, "Create: plan Completed (6/9), PlanStatus Completado (2), sin borrador", pa["phase"] == 6 and pa["state"] == 9 and pa["pstatus"] == 2 and pa["draft"] == 0 and pa["acq"], f"{pa}")
    x = rq(pa["acq"])
    check(R, "Create: solicitud de COMPRA (2) Pendiente, prioridad Media, con codigo, centro de costos, justificacion y observaciones", x["type"] == 2 and x["status"] == 0 and x["prio"] == 1 and x["inv"] == "INV-QAP" and x["cc"] == "CC-QAP" and x["desc"] == "Justificacion de compra QAP" and x["obs"] == "Obs compra QAP", f"{x}")
    check(R, "Create: se copian los costos del L-8 a la solicitud (mismo numero de lineas)", n_cost_req(pa["acq"]) == scalar("select count(*) from CostDetails where MaintenanceId=?", pa["mnt"]) and n_cost_req(pa["acq"]) > 0, f"req={n_cost_req(pa['acq'])} mnt={scalar('select count(*) from CostDetails where MaintenanceId=?', pa['mnt'])}")
    check(R, "Create: notificacion 'Adquisición Solicitada' creada para el usuario", n_notif(nom) == 1, f"notificaciones={n_notif(nom)}")
    # doble envio
    r = P.l12(c, pid("a"), {"Input.Description": "Justificacion de compra QAP"}, name="doble envio", follow=True)
    check(R, "Create doble envio: no duplica la solicitud de compra ni las lineas de costo", n_acq(uid("a")) == 1 and n_cost_req(pa["acq"]) == scalar("select count(*) from CostDetails where MaintenanceId=?", pa["mnt"]), P.dump(r))
    check(R, "Create doble envio: no duplica la notificacion 'Adquisición Solicitada'", n_notif(nom) == 1, f"notificaciones={n_notif(nom)}")
    # borrador sobre plan completado
    antes = plan(pid("a"))
    r = P.l12(c, pid("a"), draft=True, name="borrador sobre completado", follow=True)
    desp = plan(pid("a"))
    check(R, "Create: guardar BORRADOR sobre un plan ya Completado no debe reabrirlo", desp["state"] == antes["state"] and desp["pstatus"] == antes["pstatus"], f"antes={antes['phase']}/{antes['state']}/{antes['pstatus']} despues={desp['phase']}/{desp['state']}/{desp['pstatus']} {P.dump(r)}")

    # fases del plan
    for lbl, cx_ in (("plan que aun espera su Kardex (P5)", p5), ("plan que espera su L-8/L-7 (P2)", p2), ("plan sano de L-6 (P1OK)", p1ok)):
        antes = plan(cx_["plan"])
        r = P.l12(c, cx_["plan"], name=f"L-12 sobre {lbl}", follow=True)
        desp = plan(cx_["plan"])
        check(R, f"Create: L-12 sobre un {lbl} no debe completar el plan", desp["state"] == antes["state"] and desp["pstatus"] == antes["pstatus"], f"antes={antes['phase']}/{antes['state']}/{antes['pstatus']} despues={desp['phase']}/{desp['state']}/{desp['pstatus']} {P.dump(r)}")
    if plan(p5["plan"])["state"] == 9:
        check(R, "Create: el L-12 saltado no deja la unidad En Prestamo con el plan 'Completado'", unidad(p5["unit"])["status"] != 10, f"unidad={unidad(p5['unit'])['status']}")
    # no wizard sin plan
    un = P.nueva_unidad(c, lab, "qnw")
    nwf = {"Input.FacultyId": str(P.FAC), "Input.LaboratoryId": str(lab), "Input.EquipmentUnitId": str(un[0]), "Input.Description": "Compra sin plan QAP",
           "Input.InvestmentCode": "INV-NW", "Input.CostCenter": "CC-NW"}
    r = post_form(c, f"/Acquisitions/Create?ManagementId={P.MGMT}", nwf, name="no wizard sin plan", follow=True)
    check(R, "Create no-wizard sin plan: registra y vuelve al listado", r.status == 302 and "/Acquisitions" in (r.location or "") and n_acq(un[0]) == 1, P.dump(r))
    rid_nw = scalar("select top 1 Id from Requests where EquipmentUnitId=? and Type=2 order by Id desc", un[0])
    r = post_form(c, f"/Acquisitions/Create?ManagementId={P.MGMT}", {**nwf, "Input.EquipmentUnitId": "99999"}, name="unidad inexistente", follow=True)
    check(R, "Create: unidad inexistente -> error controlado sin 500", r.status < 500, P.dump(r))
    r = post_form(c, f"/Acquisitions/Create?ManagementId={P.MGMT}", {**nwf, "Input.EquipmentUnitId": ""}, name="sin unidad", follow=True)
    check(R, "Create: sin unidad -> rechazo sin 500", r.status < 500, P.dump(r))
    r = post_form(c, f"/Acquisitions/Create?ManagementId=99999", {**nwf, "Input.InvestmentCode": "INV-NW2"}, name="gestion inexistente", follow=True)
    check(R, "Create: gestion inexistente sin 500", r.status < 500, P.dump(r))
    r = post_form(c, f"/Acquisitions/Create?ManagementId={P.MGMT}", {**nwf, "Input.MaintenanceId": "99999", "Input.InvestmentCode": "INV-NW3"}, name="mantenimiento inexistente", follow=True)
    check(R, "Create: referencia de mantenimiento inexistente sin 500", r.status < 500, P.dump(r))

    # ================================================================ Details / Index
    acq_a = plan(pid("a"))["acq"]
    st, pgd = c.get(f"/Acquisitions/Details/{acq_a}")
    check(R, "Details: 200 con codigo de inversion", st == 200 and "INV-QAP" in pgd, f"status={st}")
    for pth in ("/Acquisitions/Details/99999", "/Acquisitions/Details/abc", "/Acquisitions/Details/0"):
        st, _ = c.get(pth)
        check(R, f"Details {pth}: 404", st == 404, f"status={st}")
    req_l7 = plan(ctx["c"]["plan"])["req"]
    st, _ = c.get(f"/Acquisitions/Details/{req_l7}")
    check(R, "Details: una solicitud TECNICA (L-7) no debe abrirse como adquisicion (404)", st == 404, f"status={st}")
    for pth in ("/Acquisitions", f"/Acquisitions?SearchTerm={P.PREFIX}", f"/Acquisitions?FilterLaboratoryId={lab}", "/Acquisitions?StatusFilter=0", "/Acquisitions?StatusFilter=99", "/Acquisitions?PriorityFilter=1", "/Acquisitions?pageIndex=999"):
        st, _ = c.get(pth)
        check(R, f"Index {pth}: 200", st == 200, f"status={st}")
    st, pgi = c.get("/Acquisitions?SearchTerm=ZZZ_NO_EXISTE_QAP")
    check(R, "Index: sin resultados -> 'No se encontraron solicitudes de adquisición registradas.'", "No se encontraron solicitudes de adquisición registradas." in pgi, f"status={st}")
    st, pgi = c.get("/Acquisitions")
    check(R, "Index: solo lista solicitudes de COMPRA (no la L-7 tecnica)", f"/Acquisitions/Details/{req_l7}" not in pgi, "")

    # ================================================================ Edit
    ep = lambda i: f"/Acquisitions/Edit/{i}"
    st, _ = c.get(ep(99999))
    check(R, "Edit: GET id inexistente -> 404", st == 404, f"status={st}")
    st, _ = c.get(ep(req_l7))
    check(R, "Edit: GET de una solicitud TECNICA (L-7) -> 404", st == 404, f"status={st}")
    st, _ = c.get(ep(rid_nw))
    check(R, "Edit: GET 200 (compra sin plan)", st == 200, f"status={st}")
    r = post_form(c, ep(rid_nw), {"Input.Description": ""}, name="edit sin descripcion")
    expect(R, "Edit: descripcion vacia", r, status=200, error="La descripción es obligatoria")
    r = post_form(c, ep(rid_nw), {"Input.InvestmentCode": ""}, name="edit sin codigo")
    expect(R, "Edit: codigo de inversion vacio", r, status=200, error="El código de inversión es obligatorio")
    r = post_form(c, ep(rid_nw), {"Input.Description": "d" * 1001}, name="edit descripcion 1001")
    check(R, "Edit: descripcion de 1001 -> mensaje en espanol", r.status == 200 and bool(r.errors) and not P._EN.search(" ".join(r.errors)), P.dump(r))
    r = post_form(c, ep(rid_nw), {"Input.Observations": "o" * 501}, name="edit observaciones 501")
    check(R, "Edit: observaciones de 501 -> mensaje en espanol", r.status == 200 and bool(r.errors) and not P._EN.search(" ".join(r.errors)), P.dump(r))
    r = post_form(c, ep(rid_nw), {"Input.CostCenter": "c" * 101}, name="edit centro 101")
    check(R, "Edit: centro de costos de 101 -> mensaje en espanol", r.status == 200 and bool(r.errors) and not P._EN.search(" ".join(r.errors)), P.dump(r))
    r = post_form(c, ep(rid_nw), {"Input.CostCenter": ""}, name="edit sin centro", follow=False)
    check(R, "Edit: el centro de costos vacio debe rechazarse como en Create (obligatorio)", r.status == 200, P.dump(r))
    post_form(c, ep(rid_nw), {"Input.CostCenter": "CC-NW"}, name="restaurar centro")
    r = post_form(c, ep(rid_nw), {"Input.Status": "77"}, name="edit estado 77", follow=False)
    check(R, "Edit: estado inexistente (77) se rechaza", r.status == 200 and rq(rid_nw)["status"] == 0, P.dump(r) + f" estado={rq(rid_nw)['status']}")
    r = post_form(c, ep(rid_nw), {"Input.Priority": "77"}, name="edit prioridad 77", follow=False)
    check(R, "Edit: prioridad inexistente (77) se rechaza", r.status == 200 and rq(rid_nw)["prio"] == 1, P.dump(r) + f" prio={rq(rid_nw)['prio']}")
    r = post_form(c, ep(rid_nw), {"Input.Items[0].Concept": "Item QAP", "Input.Items[0].Quantity": "-5", "Input.Items[0].UnitPrice": "-10"}, name="edit item negativo", follow=False)
    check(R, "Edit: item con cantidad y precio negativos se rechaza", r.status == 200 and n_cost_req(rid_nw) == 0, P.dump(r) + f" lineas={n_cost_req(rid_nw)}")
    r = post_form(c, ep(rid_nw), {"Input.Items[0].Concept": "", "Input.Items[0].Quantity": "1", "Input.Items[0].UnitPrice": "5"}, name="edit item sin concepto", follow=False)
    check(R, "Edit: item sin concepto se rechaza (no debe guardarse una linea vacia)", r.status == 200 and n_cost_req(rid_nw) == 0, P.dump(r) + f" lineas={n_cost_req(rid_nw)}")
    r = post_form(c, ep(rid_nw), {"Input.Id": "99999"}, name="edit Input.Id inexistente", follow=False)
    check(R, "Edit: POST con Input.Id inexistente -> 404", r.status == 404, P.dump(r))
    # sobre una L-7 tecnica
    d_antes = rq(req_l7)["desc"]
    r = post_form(c, ep(rid_nw), {"Input.Id": str(req_l7), "Input.Description": "Pisada por Acquisitions/Edit", "Input.InvestmentCode": "HACK", "Input.Status": "3"}, name="edit sobre solicitud tecnica", follow=False)
    check(R, "Edit: POST con el Id de una solicitud TECNICA (L-7) no debe modificarla (la GET responde 404)", rq(req_l7)["desc"] == d_antes and rq(req_l7)["inv"] != "HACK", P.dump(r) + f" desc={rq(req_l7)['desc'][:40]!r} inv={rq(req_l7)['inv']}")
    r = post_form(c, ep(rid_nw), {"Input.Description": "Compra editada QAP", "Input.Priority": "2", "Input.CostCenter": "CC-ED", "Input.InvestmentCode": "INV-ED"}, name="edit valido", follow=True)
    check(R, "Edit: cambio valido -> 302 con 'Solicitud actualizada con éxito.'", r.status == 302 and r.has_toast("Solicitud actualizada con éxito.", "success"), P.dump(r))
    x = rq(rid_nw)
    check(R, "Edit valido: descripcion, prioridad Alta, codigo y centro guardados; estado sin cambios", x["desc"] == "Compra editada QAP" and x["prio"] == 2 and x["inv"] == "INV-ED" and x["cc"] == "CC-ED" and x["status"] == 0, f"{x}")
    # edicion de una compra con costos copiados del L-8
    r = post_form(c, ep(acq_a), {"Input.Description": "Compra con costos editada QAP"}, name="edit compra con costos", follow=False)
    check(R, "Edit: editar una compra que ya tiene lineas de costo copiadas del L-8 funciona (no 500 por CK_CostDetails_ExactlyOneParent)", r.status == 302, P.dump(r))
    # Supervisor cambia estado
    cs = P.client("qa_sup", pw)
    r = post_form(cs, ep(rid_nw), {"Input.Status": "3"}, name="edit estado como Supervisor", follow=False)
    check(R, "Rol Supervisor: puede editar la ficha pero NO el estado (solo Administrador aprueba/rechaza)", rq(rid_nw)["status"] == 0, P.dump(r) + f" estado={rq(rid_nw)['status']}")
    a1 = post_form(c, ep(rid_nw), {"Input.Priority": "1"}, name="doble clic 1", follow=False)
    a2 = post_form(c, ep(rid_nw), {"Input.Priority": "1"}, name="doble clic 2", follow=False)
    check(R, "Edit: doble envio idempotente (ambos 302)", a1.status == 302 and a2.status == 302, f"{a1.status},{a2.status}")

    # ================================================================ Delete
    st, _ = c.get(f"/Acquisitions/Delete/{rid_nw}")
    check(R, "Delete: GET 200", st == 200, f"status={st}")
    st, _ = c.get("/Acquisitions/Delete/99999")
    check(R, "Delete: GET id inexistente -> 404", st == 404, f"status={st}")
    r = P.post_raw(c, f"/Acquisitions/Delete/{rid_nw}", [], f"/Acquisitions/Delete/{rid_nw}", name="delete")
    expect(R, "Delete: mensaje de exito", r, status=302, toast="Solicitud marcada como eliminada correctamente.", toast_kind="success")
    check(R, "Delete: la solicitud queda Cancelled (99) de verdad (el mensaje de exito debe corresponder a un cambio persistido)", rq(rid_nw)["status"] == 99, f"estado={rq(rid_nw)['status']}")
    r = P.post_raw(c, "/Acquisitions/Delete/99999", [], "/Acquisitions", name="delete inexistente")
    check(R, "Delete: id inexistente -> 404 o aviso (no silencioso)", r.status == 404 or bool(r.toasts), P.dump(r))
    r = P.post_raw(c, f"/Acquisitions/Delete/{req_l7}", [], "/Acquisitions", name="delete de solicitud tecnica", follow=True)
    check(R, "Delete: Acquisitions/Delete no debe anular una solicitud TECNICA (L-7)", rq(req_l7)["status"] != 99 and not r.has_toast("eliminada correctamente", "success"), P.dump(r) + f" estado L-7={rq(req_l7)['status']}")
    # delete de la compra de un plan completado
    acq_c = plan(p7["plan"])["acq"]
    r = P.post_raw(c, f"/Acquisitions/Delete/{acq_c}", [], "/Acquisitions", name="delete compra de plan completado", follow=True)
    p7n = plan(p7["plan"])
    check(R, "Delete: anular la compra de un plan 'Completado' debe reabrir/avisar sobre el plan (hoy el plan sigue Completado)", rq(acq_c)["status"] != 99 or p7n["state"] != 9, f"estado compra={rq(acq_c)['status']} plan={p7n['phase']}/{p7n['state']}")

    # ================================================================ acceso
    for pth in ("/Acquisitions", pg("e"), ep(acq_a), f"/Acquisitions/Delete/{acq_a}", f"/Acquisitions/Details/{acq_a}"):
        P.check_anon(R, "Acquisitions", pth)
    P.check_anon(R, "Acquisitions", pg("e"), post=True)
    P.tokenless(R, c, "Acquisitions/Create", pg("e"), [("Input.Description", "x"), ("Input.InvestmentCode", "x"), ("Input.CostCenter", "x")])
    P.tokenless(R, c, "Acquisitions/Edit", ep(acq_a), [("Input.Id", str(acq_a)), ("Input.Description", "x"), ("Input.InvestmentCode", "x")])
    P.tokenless(R, c, "Acquisitions/Delete", f"/Acquisitions/Delete/{acq_a}", [])
    check(R, "Sin token: el plan 'e' sigue en L-12 y la compra 'a' intacta", plan(pid("e"))["state"] == 8 and rq(acq_a)["status"] != 99)
    for usuario, rol in (("qa_sup", "Supervisor"), ("qa_cierre", "PracticeClosure")):
        cl = P.client(usuario, pw)
        st, loc = cl.probe(pg("e"))
        check(R, f"Rol {rol}: GET Acquisitions/Create denegado", st in (302, 403) and "/Login" not in (loc or ""), f"status={st} loc={loc}")
        r = post_raw(cl, pg("e"), [("Input.Description", "x"), ("Input.InvestmentCode", "x"), ("Input.CostCenter", "x")], "/", follow=False, name=f"create como {rol}")
        check(R, f"Rol {rol}: POST Create denegado y plan sin cambios", r.status in (302, 403) and plan(pid("e"))["state"] == 8, P.dump(r))
    st, loc = P.client("qa_cierre", pw).probe("/Acquisitions")
    check(R, "Rol PracticeClosure: listado /Acquisitions denegado", st in (302, 403) and "/Login" not in (loc or ""), f"status={st} loc={loc}")
    st, _ = cs.get("/Acquisitions")
    check(R, "Rol Supervisor: listado /Acquisitions accesible (ManagementRoles)", st == 200, f"status={st}")


if __name__ == "__main__":
    P.run_module("acquisitions", run)
