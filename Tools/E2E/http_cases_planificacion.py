"""Planificación por HTTP (solo copia QA): validaciones del paso 1, solapes, aprobación con faltantes, reapertura, calendario y doble envío.
Requiere los fixtures de Tools/E2E/sql/qa_conflict_fixtures_min.sql (planificaciones QA-CF-01..06 en la gestión 4) y la copia con la gestión #4 Inactiva/Activa (editable).
"""
import os
import re
import sys
import threading
import time

import e2e_lib
from http_forms import Client, expect

R = []
PW = os.environ["E2E_PW"]
c = Client(os.environ.get("E2E_USER", "qa_super"), PW)
sup = Client("qa_sup", PW)
MGMT = 4
PLAN_URL = f"/LaboratoryOperations/Planificacion?managementId={MGMT}"
# Maestros reales de la copia: materia CBC1G5 (Id 4), prácticas 28/29/30, laboratorios H-07 (7), H-01 (1), H-02 (2), docente 39
BASE = {"Input.SubjectId": "4", "Input.PracticeId": "30", "Input.LaboratoryId": "7", "Input.TeacherPersonId": "39",
        "Input.GroupName": "QAPA", "Input.StudentCount": "20", "Input.ScalingFactor": "", "Input.Date": "2026-11-17",
        "Input.StartPeriodId": "1", "Input.PeriodCount": "2", "Input.Observations": ""}


def post(over, **k):
    return c.submit(PLAN_URL, {**BASE, **over}, action_contains="Planificacion", follow=k.pop("follow", True), **k)


def plan_id(res):
    m = re.search(r"planId=(\d+)", res.location or "")
    return int(m.group(1)) if m else None


# ---------- paso 1: validaciones de servidor ----------
expect(R, "Planificación: domingo 15/11 rechazado", post({"Input.Date": "2026-11-15"}), status=200, error="domingos")
expect(R, "Planificación: fecha fuera del calendario (2027-03-02)", post({"Input.Date": "2027-03-02"}), status=200, error="no pertenece al calendario académico")
expect(R, "Planificación: día suspendido (20/10, reposición de QA)", post({"Input.Date": "2026-10-20"}), status=200, error="suspendido")
expect(R, "Planificación: bloque que no cabe (P12 con 2 periodos)", post({"Input.StartPeriodId": "12", "Input.PeriodCount": "2"}), status=200, error="no cabe en la jornada")
expect(R, "Planificación: estudiantes 501", post({"Input.StudentCount": "501"}), status=200, error="entre 0 y 500")
expect(R, "Planificación: factor 0", post({"Input.ScalingFactor": "0"}), status=200, error="entre 1 y 100")
expect(R, "Planificación: grupo con símbolos", post({"Input.GroupName": "A$B"}), status=200, error="letras, números y guion")
expect(R, "Planificación: sin materia, práctica, laboratorio, docente, grupo ni fecha",
       post({"Input.SubjectId": "0", "Input.PracticeId": "0", "Input.LaboratoryId": "0", "Input.TeacherPersonId": "0", "Input.GroupName": "", "Input.Date": ""}),
       status=200, error="obligatori")
expect(R, "Planificación: práctica de otra materia", post({"Input.SubjectId": "4", "Input.PracticeId": "1"}), status=200)

# ---------- creación válida y choques de laboratorio ----------
ok = post({}, follow=False)
expect(R, "Planificación: válida (H-07, 17/11, P1-P2) → Requerimientos", ok, status=302)
pid = plan_id(ok)
R.append(e2e_lib.Result("Planificación: el redirect trae el planId", True, pid is not None, f"location={ok.location}"))
dup = post({})
expect(R, "Planificación: mismo grupo y práctica otra vez", dup, status=200, error="ya tiene planificada esta práctica")
solape = post({"Input.PracticeId": "28", "Input.GroupName": "QAPB", "Input.StartPeriodId": "2", "Input.PeriodCount": "1"})
expect(R, "Planificación: laboratorio solapado (P2 sobre P1-P2)", solape, status=200, error="ocupado")
contiguo = post({"Input.PracticeId": "28", "Input.GroupName": "QAPB", "Input.StartPeriodId": "3", "Input.PeriodCount": "1"})
expect(R, "Planificación: laboratorio contiguo (P3 pegado a P1-P2) exige un periodo libre", contiguo, status=200, error="periodo libre")
libre = post({"Input.PracticeId": "28", "Input.GroupName": "QAPB", "Input.StartPeriodId": "4", "Input.PeriodCount": "1"}, follow=False)
expect(R, "Planificación: P4 libre se acepta", libre, status=302)
doc = post({"Input.PracticeId": "29", "Input.GroupName": "QAPC", "Input.LaboratoryId": "1"}, follow=False)
expect(R, "Planificación: mismo docente solapado en OTRO laboratorio NO se valida (se guarda)", doc, status=302)

# ---------- edición ----------
if pid:
    r = c.submit(f"/LaboratoryOperations/Planificacion?id={pid}", {"Input.StudentCount": "25", "Input.PracticeId": "30"}, action_contains="Planificacion", follow=False)
    expect(R, "Planificación/Editar: cambia estudiantes sin tocar horario", r, status=302)

# ---------- aprobación con faltantes (QA-CF-01 Silpat en conflicto) ----------
cf1 = int(e2e_lib.db_scalar("SELECT Id FROM Plannings WHERE Code='QA-CF-01'"))
page = c.get(f"/LaboratoryOperations/Disponibilidad?planId={cf1}")[0]
R.append(e2e_lib.Result("Disponibilidad: carga la planificación QA-CF-01", 200, page))
sin = c.submit(f"/LaboratoryOperations/Disponibilidad?planId={cf1}", {"OriginLabId": "0"}, action_contains="Approve")
expect(R, "Aprobar con faltante/conflicto SIN confirmar → advertencia y sigue Planificada", sin, toast="Confirma que conoces")
st = e2e_lib.db_scalar("SELECT [Status] FROM Plannings WHERE Id=?", cf1)
R.append(e2e_lib.Result("Aprobar sin confirmar deja la planificación en Planificada", "Planificada", st))
con = c.submit(f"/LaboratoryOperations/Disponibilidad?planId={cf1}", {"OriginLabId": "0", "Acknowledged": "true"}, action_contains="Approve")
expect(R, "Aprobar CON confirmación → aprobada", con, toast="Planificación aprobada")
st = e2e_lib.db_scalar("SELECT [Status] FROM Plannings WHERE Id=?", cf1)
R.append(e2e_lib.Result("Aprobar con confirmación deja la planificación Aprobada", "Aprobada", st))

# ---------- reabrir (solo SuperAdmin) ----------
reopen_url = f"/LaboratoryOperations/Planificaciones?managementId={MGMT}&handler=Reopen&id={cf1}"
rs = sup.raw_post(reopen_url, [], follow=False, token_from=f"/LaboratoryOperations/Planificaciones?managementId={MGMT}") if False else None
ro = c.raw_post(reopen_url, [], token_from=f"/LaboratoryOperations/Planificaciones?managementId={MGMT}")
expect(R, "Reabrir (SuperAdmin) una Aprobada sin salida", ro, toast="se reabrió para editar")
st = e2e_lib.db_scalar("SELECT [Status] FROM Plannings WHERE Id=?", cf1)
R.append(e2e_lib.Result("Reabrir devuelve la planificación a Planificada", "Planificada", st))
sup_status, sup_loc = sup.probe(f"/LaboratoryOperations/Planificaciones?managementId={MGMT}")
R.append(e2e_lib.Result("Supervisor ve Planificaciones", 200, sup_status))
rr = sup.raw_post(reopen_url, [], follow=False, token_from=f"/LaboratoryOperations/Planificaciones?managementId={MGMT}")
R.append(e2e_lib.Result("Reabrir como Supervisor no está permitido (302 a /Error)", True, rr.status in (302, 403) and "/error" in (rr.location or "").lower() or rr.status == 403, f"status={rr.status} loc={rr.location}"))

# ---------- calendario ----------
cal = f"/LaboratoryOperations/CalendarioAcademico?managementId={MGMT}"
day = int(e2e_lib.db_scalar("SELECT TOP 1 Id FROM AcademicDays WHERE AcademicWeekId IN (SELECT Id FROM AcademicWeeks WHERE ManagementId=?) AND CAST([Date] AS date)='2026-12-12'", MGMT))
r = c.submit(cal, {"DayId": str(day), "Status": "1", "Reason": ""}, action_contains="SetDay")
expect(R, "Calendario: suspender sin motivo", r, toast="motivo de la suspensión")
r = c.submit(cal, {"DayId": str(day), "Status": "1", "Reason": "x" * 201}, action_contains="SetDay")
expect(R, "Calendario: motivo de 201 caracteres", r, toast="200 caracteres")
r = c.submit(cal, {"DayId": str(day), "Status": "1", "Reason": "Paro departamental (QA)"}, action_contains="SetDay")
expect(R, "Calendario: suspender un día sin planificaciones", r, toast="suspendido")
r = c.submit(cal, {"DayId": str(day), "Status": "0", "Reason": ""}, action_contains="SetDay")
expect(R, "Calendario: habilitar el día", r, toast="habilitado")
busy = int(e2e_lib.db_scalar("SELECT TOP 1 d.Id FROM AcademicDays d JOIN AcademicWeeks w ON w.Id=d.AcademicWeekId WHERE w.ManagementId=? AND CAST(d.[Date] AS date)='2026-10-27'", MGMT))
r = c.submit(cal, {"DayId": str(busy), "Status": "1", "Reason": "Prueba"}, action_contains="SetDay")
expect(R, "Calendario: suspender un día CON planificaciones debe usar la reposición", r, toast="use la reposición de día")
r = c.submit(cal, {"ClassesStartDate": "2026-12-01", "ClassesEndDate": "2026-11-01"}, action_contains="Generate")
expect(R, "Calendario: generar con fin anterior al inicio", r, toast="no puede ser anterior")
r = c.submit(cal, {"ClassesStartDate": "2025-01-01", "ClassesEndDate": "2025-02-01"}, action_contains="Generate")
expect(R, "Calendario: fechas de clases fuera de la gestión", r, toast="dentro de las fechas de la gestión")
r = sup.raw_post(f"{cal}&handler=SetDay", [("ManagementId", str(MGMT)), ("DayId", str(day)), ("Status", "1"), ("Reason", "x")], follow=False, token_from=cal)
R.append(e2e_lib.Result("Calendario: Supervisor no puede suspender días", True, r.status in (302, 403), f"status={r.status} loc={r.location}"))

# ---------- doble envío de la salida L-3 (sospecha S20: salida duplicada) ----------
tgt = int(e2e_lib.db_scalar("SELECT Id FROM Plannings WHERE Code='QA-CF-04'"))   # Aprobada, Laja 6
results = []
def send():
    cl = Client(os.environ.get("E2E_USER", "qa_super"), PW)
    a, fields = cl.form(f"/LaboratoryOperations/L3?id={tgt}", action_contains="Departure")
    barrier.wait()
    results.append(cl._post(a, fields, False, "salida"))
barrier = threading.Barrier(2)
ts = [threading.Thread(target=send) for _ in range(2)]
[t.start() for t in ts]; [t.join() for t in ts]
deps = int(e2e_lib.db_scalar("SELECT COUNT(*) FROM Departures WHERE PlanningId=?", tgt))
R.append(e2e_lib.Result("L-3: dos envíos simultáneos de «Registrar salida» crean UNA sola salida", 1, deps, f"respuestas={[r.status for r in results]}"))

sys.exit(1 if e2e_lib.run_all([lambda: R]) else 0)
