"""WS-4 · L-48 operativo, calendario y Excel L-48 (copia QA, escribe datos propios vía la app).

Gestiones: preventiva 30 (2027-2) y correctiva CORR-2027-2 (activas a la vez).
  CA-01 pestaña l48: planes mostrados = SQL (CurrentState >= AwaitingRequest), también con filtro de laboratorio
  CA-02 filtro «Retrasado» (StatusFilter=Delay) = SQL con la misma regla
  CA-03 calendario: eventos = planes con fecha (PlannedDate o fecha del L-8 no cancelado); fecha, clase y título correctos
  CA-04 sombreado: semestre 2 → 01-12 al 01-02 del año siguiente; correctiva sin sombreado
  CA-05 ToggleWeek: preventiva guarda la semana y su fecha; correctiva se rechaza sin cambios
  CA-06 pestaña sanos = planes VerifiedGood
  CA-07 Excel L-48: xlsx válido con las unidades del laboratorio; gestión inexistente 404; laboratorio sin planes sin 500
  CA-09 Schedules/Index (página sin enlace): preventiva por defecto, correctiva con TypeFilter
"""
import io
import json
import os
import re
import urllib.error

import openpyxl

import e2e_lib
import http_cases_correctivo_common as C
import http_cases_procesos_common as P
from http_cases_procesos_common import check, plan, scalar

MGMT = P.MGMT


def eventos(c, mid):
    st, html = c.get(f"/Managements/Details?id={mid}&ActiveTab=calendario")
    m = re.search(r"var allEvents = (\[.*?\]);", html or "", re.S)
    ev = json.loads(m.group(1)) if m else None
    shade = re.search(r"var shadingStart = '([^']*)';\s*var shadingEnd\s*=\s*'([^']*)'", html or "")
    return st, ev, (shade.group(1), shade.group(2)) if shade else None


def plan_ids(html, mid):
    return {int(x) for x in re.findall(rf"ManagementId={mid}&amp;SelectedLabId=\d*&amp;FocusPlanId=(\d+)", html or "")} | \
           {int(x) for x in re.findall(rf"ManagementId={mid}&SelectedLabId=\d*&FocusPlanId=(\d+)", html or "")}


def fetch(c, path):
    try:
        r = c.opener.open(e2e_lib.BASE + path, timeout=180)
        return r.status, r.read()
    except urllib.error.HTTPError as e:
        return e.code, e.read()


EVENT_SQL = """
SELECT p.Id,
       CONVERT(varchar(10), COALESCE(m.ScheduledDate, m.EndDate, p.PlannedDate), 23) AS fecha,
       CASE WHEN m.Status = 2 THEN 'ev-completed' WHEN g.Type = 1 THEN 'ev-corrective' ELSE 'ev-preventive' END AS clase,
       u.InventoryNumber
FROM ManagementPlans p
JOIN Managements g ON g.Id = p.ManagementId
JOIN EquipmentUnits u ON u.Id = p.EquipmentUnitId
LEFT JOIN Maintenances m ON m.Id = p.MaintenanceId AND m.Status <> 99
WHERE p.ManagementId = ? AND (p.PlannedDate IS NOT NULL OR m.ScheduledDate IS NOT NULL OR m.EndDate IS NOT NULL)
"""


def run(R, c):
    lab = P.lab_propio(c, "l48")
    corr = C.asegurar_correctiva(c)
    # datos propios: preventivos en distintas fases y un correctivo completado y otro en curso
    p3 = P.llevar_a(c, lab, "ca3", "P3")
    p4 = P.llevar_a(c, lab, "ca4", "P4")
    p7 = P.llevar_a(c, lab, "ca7", "P7")
    sano = P.llevar_a(c, lab, "cas", "P1OK")
    cdone = C.llevar_corr(c, corr, lab, "cac", "DONE")
    cl8 = C.llevar_corr(c, corr, lab, "cal", "L7")

    # ---------------- CA-01 pestaña l48 = SQL
    for mid, nombre in ((MGMT, "preventiva 30"), (corr, "correctiva")):
        st, html = c.get(f"/Managements/Details?id={mid}&ActiveTab=l48")
        esperado = {r[0] for r in e2e_lib.db_rows(
            "SELECT Id FROM ManagementPlans WHERE ManagementId=? AND EquipmentUnitId IS NOT NULL AND CurrentState >= 3", mid)}
        vistos = plan_ids(html, mid)
        check(R, f"CA-01 l48 {nombre}: planes mostrados = SQL (CurrentState >= AwaitingRequest)",
              st == 200 and vistos == esperado, f"status={st} vistos={len(vistos)} sql={len(esperado)} faltan={sorted(esperado - vistos)[:8]} sobran={sorted(vistos - esperado)[:8]}")
    st, html = c.get(f"/Managements/Details?id={MGMT}&ActiveTab=l48&LabFilterId={lab}")
    esperado = {r[0] for r in e2e_lib.db_rows(
        "SELECT p.Id FROM ManagementPlans p JOIN EquipmentUnits u ON u.Id=p.EquipmentUnitId WHERE p.ManagementId=? AND p.CurrentState >= 3 AND u.LaboratoryId=?", MGMT, lab)}
    check(R, "CA-01 l48 con filtro de laboratorio = SQL", st == 200 and plan_ids(html, MGMT) == esperado,
          f"vistos={sorted(plan_ids(html, MGMT))} sql={sorted(esperado)}")

    # ---------------- CA-02 Retrasado
    st, html = c.get(f"/Managements/Details?id={MGMT}&ActiveTab=l48&StatusFilter=Delay")
    esperado = {r[0] for r in e2e_lib.db_rows(
        "SELECT Id FROM ManagementPlans WHERE ManagementId=? AND EquipmentUnitId IS NOT NULL AND CurrentState >= 3 "
        "AND PlannedDate IS NOT NULL AND PlannedDate < SYSUTCDATETIME() AND CurrentState NOT IN (2, 9) AND PlanStatus <> 2", MGMT)}
    check(R, "CA-02 filtro Retrasado (Delay) = SQL con la misma regla", st == 200 and plan_ids(html, MGMT) == esperado,
          f"vistos={len(plan_ids(html, MGMT))} sql={len(esperado)}")

    # ---------------- CA-05 ToggleWeek (antes del calendario para que haya una fecha planificada)
    tok = f"/Managements/Details?id={MGMT}&ActiveTab=l48"
    r = c.raw_post(f"/Managements/Details?id={MGMT}&handler=ToggleWeek", [("planId", str(p3["plan"])), ("weekNumber", "3"), ("type", "planned")],
                   follow=False, token_from=tok)
    pw = e2e_lib.db_rows("SELECT PlannedWeek, CONVERT(varchar(10), PlannedDate, 23) FROM ManagementPlans WHERE Id=?", p3["plan"])[0]
    check(R, "CA-05 ToggleWeek preventiva: guarda la semana 3 y una fecha de diciembre de 2027 (semestre 2, semanas 1-4)",
          r.status == 200 and pw[0] == 3 and (pw[1] or "").startswith("2027-12"), f"status={r.status} body={(r.text or '')[:120]} semana/fecha={tuple(pw)}")
    antes = e2e_lib.db_rows("SELECT PlannedWeek, PlannedDate FROM ManagementPlans WHERE Id=?", cl8["plan"])[0]
    r = c.raw_post(f"/Managements/Details?id={corr}&handler=ToggleWeek", [("planId", str(cl8["plan"])), ("weekNumber", "2"), ("type", "planned")],
                   follow=False, token_from=f"/Managements/Details?id={corr}&ActiveTab=l48")
    despues = e2e_lib.db_rows("SELECT PlannedWeek, PlannedDate FROM ManagementPlans WHERE Id=?", cl8["plan"])[0]
    check(R, "CA-05 ToggleWeek correctiva: se rechaza con mensaje en español y no cambia nada",
          '"success":false' in (r.text or "").replace(" ", "") and "correctiva" in (r.text or "").lower() and tuple(antes) == tuple(despues),
          f"status={r.status} body={(r.text or '')[:160]} antes={tuple(antes)} despues={tuple(despues)}")

    # ---------------- CA-03 / CA-04 calendario
    for mid, nombre, sombra in ((MGMT, "preventiva 30", ("2027-12-01", "2028-02-01")), (corr, "correctiva", ("", ""))):
        st, ev, shade = eventos(c, mid)
        filas = e2e_lib.db_rows(EVENT_SQL, mid)
        esperado = {r[0]: (r[1], r[2], f"{r[3]} - ") for r in filas}
        obtenido = {e["PlanId"]: (e["Start"], e["ClassName"], e["Title"]) for e in (ev or [])}
        check(R, f"CA-03 calendario {nombre}: número de eventos = planes con fecha (SQL)",
              st == 200 and ev is not None and set(obtenido) == set(esperado), f"eventos={len(obtenido)} sql={len(esperado)} faltan={sorted(set(esperado) - set(obtenido))[:8]} sobran={sorted(set(obtenido) - set(esperado))[:8]}")
        malos = [pid for pid, (f, cl, t) in obtenido.items() if pid in esperado and (f != esperado[pid][0] or cl != esperado[pid][1] or not t.startswith(esperado[pid][2]))]
        check(R, f"CA-03 calendario {nombre}: fecha (L-8 programado ?? fin ?? planificada), clase y título «INV - Equipo» correctos",
              not malos, f"distintos={[(p, obtenido[p], esperado[p]) for p in malos[:4]]}")
        check(R, f"CA-04 sombreado {nombre} = {sombra}", shade == sombra, f"observado={shade}")

    # ---------------- CA-06 sanos
    st, html = c.get(f"/Managements/Details?id={MGMT}&ActiveTab=sanos&LabFilterId={lab}")
    esperado = {r[0] for r in e2e_lib.db_rows(
        "SELECT p.Id FROM ManagementPlans p JOIN EquipmentUnits u ON u.Id=p.EquipmentUnitId WHERE p.ManagementId=? AND p.CurrentState=2 AND u.LaboratoryId=?", MGMT, lab)}
    inv_sano = scalar("SELECT InventoryNumber FROM EquipmentUnits WHERE Id=?", sano["unit"])
    check(R, "CA-06 pestaña sanos (filtro laboratorio): incluye la unidad con L-6 sano y nada fuera de VerifiedGood",
          st == 200 and sano["plan"] in esperado and inv_sano in html and (P.unidad(p3["unit"]) and scalar("SELECT InventoryNumber FROM EquipmentUnits WHERE Id=?", p3["unit"]) not in html),
          f"status={st} sql={sorted(esperado)}")

    # ---------------- CA-07 Excel L-48
    for mid, nombre in ((MGMT, "preventiva 30"), (corr, "correctiva")):
        st, body = fetch(c, f"/api/reports/download/l48?managementId={mid}&labId={lab}")
        ok, texto = False, ""
        if st == 200:
            try:
                wb = openpyxl.load_workbook(io.BytesIO(body), data_only=True)
                texto = " ".join(str(v) for ws in wb.worksheets for row in ws.iter_rows(values_only=True) for v in row if v is not None)
                ok = True
            except Exception as exc:
                texto = f"no abre: {exc}"
        invs = [r[0] for r in e2e_lib.db_rows(
            "SELECT u.InventoryNumber FROM ManagementPlans p JOIN EquipmentUnits u ON u.Id=p.EquipmentUnitId WHERE p.ManagementId=? AND u.LaboratoryId=?", mid, lab)]
        faltan = [i for i in invs if i not in texto]
        check(R, f"CA-07 Excel L-48 {nombre}: xlsx válido con todas las unidades del laboratorio ({len(invs)})",
              ok and not faltan, f"status={st} faltan={faltan[:6]} {texto[:80] if not ok else ''}")
    st, body = fetch(c, f"/api/reports/download/l48?managementId=999999&labId={lab}")
    check(R, "CA-07 Excel L-48 gestión inexistente -> 404", st == 404, f"status={st}")
    lab_vacio = P.lab_propio(c, "l48 vacio")
    st, body = fetch(c, f"/api/reports/download/l48?managementId={MGMT}&labId={lab_vacio}")
    check(R, "CA-07 Excel L-48 laboratorio sin planes: sin 500", st < 500, f"status={st}")
    st, body = fetch(c, f"/api/reports/download/l48?managementId={MGMT}&labId=999999")
    check(R, "CA-07 Excel L-48 laboratorio inexistente: sin 500", st < 500, f"status={st}")

    # ---------------- CA-09 Schedules/Index
    inv_prev = scalar("SELECT InventoryNumber FROM EquipmentUnits WHERE Id=?", p4["unit"])
    inv_corr = scalar("SELECT InventoryNumber FROM EquipmentUnits WHERE Id=?", cdone["unit"])
    st, html = c.get(f"/Schedules/Index?LabFilterId={lab}")
    check(R, "CA-09 Schedules/Index por defecto muestra la preventiva (unidad preventiva sí, correctiva no)",
          st == 200 and inv_prev in html and inv_corr not in html, f"status={st} prev={inv_prev in (html or '')} corr={inv_corr in (html or '')}")
    st, html = c.get(f"/Schedules/Index?TypeFilter=Corrective&LabFilterId={lab}")
    check(R, "CA-09 Schedules/Index?TypeFilter=Corrective muestra la correctiva",
          st == 200 and inv_corr in html and inv_prev not in html, f"status={st} prev={inv_prev in (html or '')} corr={inv_corr in (html or '')}")
    _ = (p7, plan)


if __name__ == "__main__":
    P.run_module("l48_calendario", run)
