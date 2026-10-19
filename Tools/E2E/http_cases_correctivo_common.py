"""
Utilidades del flujo CORRECTIVO semestral (regla aprobada 2026-10-18): la gestion CORR-2027-2 convive Activa
con la preventiva 30 y su asistente arranca en el paso 2 (L-7). Reutiliza http_cases_procesos_common (P).

- asegurar_correctiva(): devuelve el Id de CORR-2027-2 Activa (la activa o crea si falta).
- checklist(): 13 lineas numeradas "1. ..." a "13. ..." separadas por \r\n (checklist tecnico de L-7).
- l7_corr(): L-7 por la via correctiva (/Requests/Create?isWizard=true&ManagementId=<corr> sin plan previo).
- plan_de(): ultimo ManagementPlan de una unidad dentro de la gestion correctiva.
- llevar_corr(): crea una unidad propia y la lleva por L-7 -> L-8 -> L-3 -> Kardex -> L-12 hasta la fase pedida
  ("L7", "L8", "L3", "KARDEX", "DONE"), reutilizando P.l8/P.l3/P.kardex/P.l12. Solo contra la copia QA.
"""
import http_cases_procesos_common as P
from http_cases_procesos_common import post_form, scalar

CODE = "CORR-2027-2"   # codigo de la correctiva semestral 2027-2 (Year=2027, Semester=2)

_CHECKLIST = ["1. Desconexion del cable", "2. Limpieza interna", "3. Limpieza externa", "4. Presion del refrigerante",
              "5. Fugas", "6. Hielo", "7. Temperatura", "8. Puertas y sellos", "9. Drenajes", "10. Ventiladores",
              "11. Electrico", "12. Lubricacion", "13. Mantenimiento con personal externo"]


def checklist(n=13):
    """Checklist tecnico de L-7: n lineas numeradas separadas por \r\n (13 por defecto)."""
    return "\r\n".join(_CHECKLIST[:n])


def asegurar_correctiva(c):
    """Id de la gestion correctiva activa CORR-2027-2; si no existe la crea por /Managements/Create?type=Corrective."""
    corr = scalar("select top 1 Id from Managements where Code=? and Type=1 and Status=0", CODE)
    if corr:
        return int(corr)
    res = None
    corr = scalar("select top 1 Id from Managements where Code=? and Type=1 and Status<>99", CODE)
    if corr:
        res = P.post_raw(c, f"/Managements/Index?handler=ActivateManagement&id={corr}", [],
                         "/Managements/Index?Type=Corrective", follow=True, name=f"activar {CODE}")
    else:
        res = post_form(c, "/Managements/Create?type=Corrective",
                        {"Input.Year": "2027", "Input.Semester": "2", "Input.Status": "0", "Input.Type": "Corrective",
                         "Input.StartDate": "2027-07-01", "Input.PlannedEndDate": "2027-12-31",
                         "Input.Description": "Correctiva QA", "Input.FacultyId": ""},
                        name=f"crear {CODE}", follow=True)
    corr = scalar("select top 1 Id from Managements where Code=? and Type=1 and Status=0", CODE)
    if not corr:
        raise RuntimeError(f"No se pudo asegurar la gestion correctiva activa {CODE}: {P.dump(res)}")
    return int(corr)


def l7_corr(c, corr, lab, unit, ov=None, draft=False, follow=True, name=None):
    """L-7 del asistente correctivo (sin ManagementPlanId): crea Request + ManagementPlan y salta al paso 3."""
    page = f"/Requests/Create?isWizard=true&ManagementId={corr}"
    base = {"Input.FacultyId": str(P.FAC), "Input.LaboratoryId": str(lab), "Input.EquipmentUnitId": str(unit),
            "Input.Description": "Falla correctiva QAP", "Input.Observations": checklist(13),
            "Input.TechnicalChecklistConfirmed": "true", "Input.Priority": "1"}
    base.update(ov or {})
    return post_form(c, page, base, handler="Draft" if draft else None,
                     name=name or f"L-7 corr unidad {unit}", follow=follow)


def plan_de(corr, unit):
    """Ultimo ManagementPlan de `unit` dentro de la gestion `corr` (None si no tiene ninguno)."""
    pid = scalar("select top 1 Id from ManagementPlans where ManagementId=? and EquipmentUnitId=? order by Id desc", corr, unit)
    return int(pid) if pid else None


FASES_CORR = ["L7", "L8", "L3", "KARDEX", "DONE"]


def llevar_corr(c, corr, lab, sufijo, hasta, unit=None):
    """Crea una unidad propia (salvo que se pase `unit`) y la lleva por el flujo correctivo hasta `hasta`:
    L7 (plan en 3/4) | L8 (4/6) | L3 (5/7, unidad OnLoan) | KARDEX (6/8, unidad Operativa) | DONE (6/9, PlanStatus 2).
    Reutiliza el plan ya existente de la unidad: cada paso solo se ejecuta si el plan aun no lo supero.
    Devuelve dict(unit, inv, plan, lab, corr)."""
    if hasta not in FASES_CORR:
        raise ValueError(f"hasta debe ser uno de {FASES_CORR}")
    if unit is None:
        uid, inv = P.nueva_unidad(c, lab, sufijo)
    else:
        uid = int(unit)
        inv = scalar("select top 1 InventoryNumber from EquipmentUnits where Id=?", uid)
    ctx = {"unit": uid, "inv": inv, "plan": plan_de(corr, uid), "lab": lab, "corr": corr}
    idx = FASES_CORR.index(hasta)

    p = P.plan(ctx["plan"]) if ctx["plan"] else None
    if p is None or (p["phase"] <= 2 and p["pstatus"] != 2):
        # sin L-7 registrado (o solo borrador): registrarlo desde el formulario (lo completa si era borrador)
        r = l7_corr(c, corr, lab, uid, name=f"{sufijo} L-7 corr")
        ctx["plan"] = plan_de(corr, uid)
        if not ctx["plan"]:
            raise RuntimeError(f"{sufijo}: el L-7 correctivo no creo el plan: {P.dump(r)}")
        p = P.plan(ctx["plan"])
    if idx >= 1 and p["phase"] <= 3 and p["pstatus"] != 2:
        P.l8(c, ctx["plan"], name=f"{sufijo} L-8 corr")
        p = P.plan(ctx["plan"])
    if idx >= 2 and p["phase"] <= 4 and p["pstatus"] != 2:
        P.l3(c, ctx["plan"], uid, lab, name=f"{sufijo} L-3 corr", mgmt=corr)
        p = P.plan(ctx["plan"])
    if idx >= 3 and p["phase"] <= 5 and p["pstatus"] != 2:
        P.kardex(c, ctx["plan"], name=f"{sufijo} Kardex corr")
        p = P.plan(ctx["plan"])
    if idx >= 4 and p["pstatus"] != 2:
        P.l12(c, ctx["plan"], name=f"{sufijo} L-12 corr")
        p = P.plan(ctx["plan"])
    if not p or p["phase"] < {0: 3, 1: 4, 2: 5, 3: 6, 4: 6}[idx]:
        raise RuntimeError(f"{sufijo}: el plan {ctx['plan']} no llego a {hasta}: {p}")
    return ctx
