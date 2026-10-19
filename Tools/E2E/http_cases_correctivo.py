"""
Flujo de gestion CORRECTIVA semestral (regla aprobada 2026-10-18): CORR-2027-2 convive Activa con la preventiva 30
(2027-2); su asistente arranca en el paso 2 (L-7) y sigue L-8 -> L-3 -> Kardex -> L-12 hasta Completado (6/9, PlanStatus 2).
Cubre: CR-01 convivencia/visibilidad, CR-02 asistente paso 2, CR-03 L-7 con y sin borrador, CR-04 regla D-7,
CR-05 validaciones L-8, CR-06 L-3/Kardex/L-12, CR-07 historial de estados. Solo contra la copia QA.
Salida: outputs/qa-usuario/http_cases_procesos_correctivo.txt (la escribe P.finish).
"""
import http_cases_procesos_common as P
import http_cases_correctivo_common as C
from http_cases_procesos_common import check, plan, scalar, sql, unidad

ON_LOAN = 10  # EquipmentStatus.OnLoan "En Prestamo" (Models/Enums/EquipmentStatus.cs)


def obs_req(rid):
    """Observaciones (checklist) guardadas en la solicitud L-7."""
    r = sql("select Observations from Requests where Id=?", rid)
    return r[0][0] if r else None


def run(R, c):
    lab = P.lab_propio(c, "correctivo")
    corr = C.asegurar_correctiva(c)

    # ================================================================ CR-01 convivencia y visibilidad
    st_corr, st_prev = scalar("select Status from Managements where Id=?", corr), scalar("select Status from Managements where Id=?", P.MGMT)
    check(R, "CR-01 la correctiva CORR-2027-2 y la preventiva 30 estan Activas a la vez (SQL: ambas Status=0)",
          st_corr == 0 and st_prev == 0, f"corr={st_corr} preventiva30={st_prev}")
    r = P.post_form(c, "/Managements/Create?type=Corrective",
                    {"Input.Year": "2027", "Input.Semester": "2", "Input.Status": "0", "Input.Type": "Corrective",
                     "Input.StartDate": "2027-07-01", "Input.PlannedEndDate": "2027-12-31",
                     "Input.Description": "Correctiva QA", "Input.FacultyId": ""},
                    name="CR-01 duplicado CORR-2027-2")
    check(R, "CR-01 crear un duplicado CORR-2027-2 se rechaza con 'Ya existe una gestion registrada para CORR-2027-2.'",
          r.status == 200 and (r.has_error("Ya existe una gestion registrada") or r.has_toast("Ya existe una gestion registrada")),
          P.dump(r))
    st, html = c.get("/LaboratoryOperations/Planificaciones")
    check(R, "CR-01 CORR-2027-2 no aparece en /LaboratoryOperations/Planificaciones",
          st == 200 and "CORR-2027-2" not in (html or ""), f"status={st}")
    st, html = c.get("/Catalogs/CourseGroups/Create")
    check(R, "CR-01 CORR-2027-2 no aparece en /Catalogs/CourseGroups/Create",
          st == 200 and "CORR-2027-2" not in (html or ""), f"status={st}")
    st, loc = c.probe("/Managements/Index?handler=CurrentCorrective")
    check(R, "CR-01 /Managements/Index?handler=CurrentCorrective redirige a los Details de la correctiva activa",
          st == 302 and f"/Managements/Details?id={corr}" in (loc or ""), f"status={st} loc={loc}")

    # ================================================================ CR-02 asistente correctivo: arranca en el paso 2 (L-7)
    st, html = c.get(f"/?ShowWizard=true&ManagementId={corr}")
    check(R, "CR-02 GET /?ShowWizard=true&ManagementId=<corr> sin Step muestra el paso 2 ('Paso 2: L7 Solicitud')",
          st == 200 and "Paso 2: L7 Solicitud" in (html or ""), f"status={st}")
    check(R, "CR-02 el paso 2 correctivo ofrece reportar falla ('Reportar Nueva Falla')",
          st == 200 and "Reportar Nueva Falla" in (html or ""), f"status={st}")
    r = P.post_form(c, f"/?ShowWizard=true&Step=2&ManagementId={corr}", {"Step": "2", "ManagementId": str(corr)},
                    handler="PreviousStep", action_contains="PreviousStep", name="CR-02 Atras en paso 2", follow=True)
    # En el correctivo el paso 1 (L-6) no existe: «Atrás» desde el paso 2 vuelve al resumen (Step=0) o se queda en 2, nunca al 1.
    loc_back = (r.location or "")
    check(R, "CR-02 POST 'Atras' (PreviousStep) desde el paso 2 no lleva al paso 1 (L-6): vuelve al resumen (Step=0) o se queda en 2",
          r.status < 500 and "Step=1&" not in loc_back + "&" and ("Step=0" in loc_back or "Step=2" in loc_back or "Paso 2: L7 Solicitud" in (r.text or "")), P.dump(r))

    # ================================================================ CR-03 L-7 correctivo: plan nuevo y borrador
    u3, inv3 = P.nueva_unidad(c, lab, "c3")
    r = C.l7_corr(c, corr, lab, u3, name="CR-03 L-7 correctiva", follow=True)
    check(R, "CR-03 L-7 sin plan previo: 'Solicitud tecnica L-7 registrada exitosamente.' y redirige al paso 3",
          r.status == 302 and "Step=3" in (r.location or "") and r.has_toast("Solicitud t\u00e9cnica L-7 registrada exitosamente.", "success"),
          P.dump(r))
    pid3 = C.plan_de(corr, u3)
    p3 = plan(pid3)
    check(R, "CR-03 crea el ManagementPlan en fase Mantenimiento (3/4), PlanStatus En Progreso (1), RequestId enlazado y gestion = correctiva",
          bool(p3) and p3["phase"] == 3 and p3["state"] == 4 and p3["pstatus"] == 1 and bool(p3["req"]) and p3["mgmt"] == corr and p3["unit"] == u3,
          f"plan={p3}")

    u3b, inv3b = P.nueva_unidad(c, lab, "c3b")
    r = C.l7_corr(c, corr, lab, u3b, draft=True, name="CR-03 borrador L-7 correctiva", follow=True)
    pid3b = C.plan_de(corr, u3b)
    p3b = plan(pid3b)
    check(R, "CR-03 borrador L-7 (handler Draft) deja el plan en L-7 (fase 2, estado 3) con IsDraft=1 y DraftPhase=2",
          bool(p3b) and p3b["phase"] == 2 and p3b["state"] == 3 and p3b["draft"] == 1 and p3b["draftphase"] == 2,
          f"plan={p3b} {P.dump(r)}")
    # Sin ManagementPlanId (formulario «Reportar nueva falla») la guarda D-7 remite al asistente: es lo correcto.
    r = C.l7_corr(c, corr, lab, u3b, name="CR-03 nueva falla con borrador abierto", follow=True)
    check(R, "CR-03 con un borrador abierto, «Reportar nueva falla» de la misma unidad remite al asistente (D-7) sin crear nada",
          r.has_toast("ya tiene un proceso correctivo abierto", "warning") and scalar("select count(*) from Requests where EquipmentUnitId=? and Type=1", u3b) == 1,
          P.dump(r))
    # El borrador se completa desde el asistente (paso 2), es decir con su ManagementPlanId.
    r = P.post_form(c, f"/Requests/Create?isWizard=true&ManagementPlanId={pid3b}&ManagementId={corr}",
                    {"Input.Description": "Falla correctiva QAP", "Input.TechnicalChecklistConfirmed": "true", "Input.Priority": "1"},
                    name="CR-03 completar borrador desde el asistente", follow=True)
    p3b = plan(pid3b)
    check(R, "CR-03 completar el L-7 desde el formulario recargado pasa a fase Mantenimiento (3/4) sin duplicar solicitud",
          r.status == 302 and "Step=3" in (r.location or "") and p3b["phase"] == 3 and p3b["state"] == 4 and p3b["draft"] == 0
          and scalar("select count(*) from Requests where EquipmentUnitId=? and Type=1", u3b) == 1,
          P.dump(r) + f" plan={p3b}")
    lineas = [x for x in (obs_req(p3b["req"]) or "").splitlines() if x.strip()]
    check(R, "CR-03 el checklist guardado conserva las 13 lineas numeradas ('1. ...' a '13. ...')",
          len(lineas) == 13 and lineas[0].startswith("1.") and lineas[-1].startswith("13."),
          f"lineas={len(lineas)} primera={lineas[:1]} ultima={lineas[-1:]}")

    # ================================================================ CR-04 regla D-7: un proceso correctivo abierto por unidad y gestion
    ctx4 = C.llevar_corr(c, corr, lab, "c4", hasta="L7")
    n_reqs, n_plans = scalar("select count(*) from Requests"), scalar("select count(*) from ManagementPlans")
    r = C.l7_corr(c, corr, lab, ctx4["unit"], name="CR-04 D-7 duplicado", follow=True)
    check(R, "CR-04 segunda L-7 con proceso abierto -> warning 'ya tiene un proceso correctivo abierto en esta gestion'",
          r.has_toast("ya tiene un proceso correctivo abierto en esta gesti\u00f3n", "warning"), P.dump(r))
    check(R, "CR-04 la L-7 duplicada no crea ninguna solicitud ni plan (COUNT sin cambio)",
          scalar("select count(*) from Requests") == n_reqs and scalar("select count(*) from ManagementPlans") == n_plans,
          f"reqs={n_reqs}->{scalar('select count(*) from Requests')} plans={n_plans}->{scalar('select count(*) from ManagementPlans')}")
    C.llevar_corr(c, corr, lab, "c4", hasta="DONE", unit=ctx4["unit"])
    p4 = plan(ctx4["plan"])
    check(R, "CR-04 el primer proceso de la unidad queda Completado (6/9, PlanStatus 2) tras llevar_corr hasta DONE",
          p4["phase"] == 6 and p4["state"] == 9 and p4["pstatus"] == 2, f"plan={p4}")
    r = C.l7_corr(c, corr, lab, ctx4["unit"], name="CR-04 L-7 tras completar", follow=True)
    pid4b = C.plan_de(corr, ctx4["unit"])
    planes = sql("select Id, CurrentPhase, PlanStatus from ManagementPlans where ManagementId=? and EquipmentUnitId=?", corr, ctx4["unit"])
    check(R, "CR-04 tras completar el primero, una L-7 nueva de la MISMA unidad crea un segundo plan abierto (2 planes: uno Completado y otro en 3/4)",
          r.status == 302 and "Step=3" in (r.location or "") and pid4b and pid4b != ctx4["plan"] and len(planes) == 2
          and sorted(int(x[2]) for x in planes) == [1, 2] and plan(pid4b)["phase"] == 3 and plan(pid4b)["state"] == 4,
          P.dump(r) + f" planes={planes}")

    # ================================================================ CR-05 L-8 correctivo: Completado exige 100 % y tecnico
    # el servidor recalcula el avance desde las tareas: se desmarcan 2 de 4 (como en el script del preventivo)
    r = P.l8(c, pid3, {"Input.CompletionPercentage": "50", **{f"Input.Tasks[{i}].IsCompleted": None for i in (2, 3)}}, name="CR-05 L-8 al 50 %", follow=True)
    check(R, "CR-05 L-8 Completado con 50 % de avance -> 'el avance debe estar al 100%' (error de campo o toast)",
          r.status == 200 and (r.has_error("el avance debe estar al 100%") or r.has_toast("el avance debe estar al 100%")),
          P.dump(r))
    p = plan(pid3)
    check(R, "CR-05 el plan no avanza con avance 50 % (sigue 3/4 esperando L-8)",
          p["phase"] == 3 and p["state"] == 4 and not p["mnt"], f"plan={p}")
    r = P.l8(c, pid3, {"Input.TechnicianId": ""}, name="CR-05 L-8 sin tecnico", follow=True)
    check(R, "CR-05 L-8 sin tecnico -> 'El tecnico responsable es obligatorio en el flujo de mantenimiento...'",
          r.status == 200 and r.has_toast("t\u00e9cnico responsable es obligatorio", "error"), P.dump(r))
    r = P.l8(c, pid3, name="CR-05 L-8 valido", follow=True)
    p = plan(pid3)
    check(R, "CR-05 L-8 valido (100 %, tecnico interno) avanza a fase 4 (Exit/AwaitingDeparture 4/6)",
          p["phase"] == 4 and p["state"] == 6 and bool(p["mnt"]), P.dump(r) + f" plan={p}")

    # ================================================================ CR-06 L-3 -> Kardex -> L-12 con plan correctivo
    r = P.l3(c, pid3, u3, lab, mgmt=corr, name="CR-06 L-3 correctivo", follow=True)
    p = plan(pid3)
    check(R, "CR-06 L-3 con plan correctivo: la unidad queda En Prestamo (OnLoan=10) y el plan en Kardex (5/7)",
          p["phase"] == 5 and p["state"] == 7 and unidad(u3)["status"] == ON_LOAN and bool(p["dep"]),
          P.dump(r) + f" plan={p} unidad={unidad(u3)}")
    r = P.kardex(c, pid3, name="CR-06 Kardex correctivo", follow=True)
    p = plan(pid3)
    check(R, "CR-06 Kardex cierra: unidad Operativa (0) y plan a Disbursement (6/8) con KardexHistory enlazado",
          p["phase"] == 6 and p["state"] == 8 and unidad(u3)["status"] == 0 and bool(p["kardex"]),
          P.dump(r) + f" plan={p} unidad={unidad(u3)}")
    r = P.l12(c, pid3, name="CR-06 L-12 correctivo", follow=True)
    p = plan(pid3)
    check(R, "CR-06 L-12 deja el plan Completado (6/9, PlanStatus 2)",
          p["phase"] == 6 and p["state"] == 9 and p["pstatus"] == 2 and bool(p["acq"]), P.dump(r) + f" plan={p}")

    # ================================================================ CR-07 historial de estados de las unidades recorridas
    for uid, inv in ((u3, inv3), (u3b, inv3b), (ctx4["unit"], ctx4["inv"])):
        abiertos = scalar("select count(*) from EquipmentStateHistories where EquipmentUnitId=? and EndDate is null", uid)
        invertidos = scalar("select count(*) from EquipmentStateHistories where EquipmentUnitId=? and EndDate is not null and EndDate < StartDate", uid)
        check(R, f"CR-07 unidad {inv}: como maximo un historial abierto (EndDate NULL) y ninguno con EndDate < StartDate",
              abiertos <= 1 and invertidos == 0, f"abiertos={abiertos} fin_anterior_al_inicio={invertidos}")


if __name__ == "__main__":
    P.run_module("correctivo", run)
