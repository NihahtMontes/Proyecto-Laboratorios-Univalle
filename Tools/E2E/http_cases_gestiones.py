"""Gestiones por HTTP (solo copia QA): crear, validar, editar, activar, eliminar y semanas.
Correctiva semestral (regla 2026-10-18): CORR-2026-1 Inactiva para no chocar con CORR-2027-2 Activa; ambas pueden
convivir Activas con la preventiva 30 (una Activa por tipo). No toca la gestion 30; CORR-2027-2 solo se re-activa
al final si la prueba de coexistencia la cerro (activar otra correctiva cierra la Activa anterior del mismo tipo)."""
import json
import os
import re
import sys

import e2e_lib
from http_forms import Client, expect

R = []
PW = os.environ["E2E_PW"]
c = Client(os.environ.get("E2E_USER", "qa_super"), PW)
sup = Client("qa_sup", PW)
adm = Client("qa_adm", PW)
CREATE = "/Managements/Create?type=Corrective"
OVER = {"Input.Year": "2026", "Input.Semester": "1", "Input.Description": "Correctiva QA", "Input.StartDate": "2026-01-01",
        "Input.PlannedEndDate": "2026-06-30", "Input.Status": "1", "Input.Type": "Corrective", "Input.FacultyId": ""}
CODIGO = "CORR-2026-1"  # semestral (1 o 2), Inactiva: no compite con la correctiva activa CORR-2027-2


def make(over):
    return c.submit(CREATE, {**OVER, **over})


# ---------- validaciones de Create ----------
expect(R, "Gestión/Create: año 1999", make({"Input.Year": "1999"}), status=200)
expect(R, "Gestión/Create: año 2101", make({"Input.Year": "2101"}), status=200)
expect(R, "Gestión/Create: fin anterior al inicio", make({"Input.StartDate": "2026-06-30", "Input.PlannedEndDate": "2026-01-01"}), status=200, error="no puede ser anterior")
expect(R, "Gestión/Create: tipo 'Por confirmar' (históricas) no se crea", make({"Input.Type": "PorConfirmar"}), status=200)
expect(R, "Gestión/Create: estado Eliminada no se acepta", make({"Input.Status": "99"}), status=200)
pre = c.submit("/Managements/Create?type=Preventive", {**OVER, "Input.Type": "Preventive", "Input.Semester": "3"})
expect(R, "Gestión/Create: preventiva con semestre 3 -> 'La gestión debe usar el semestre 1 o 2.'", pre, status=200, error="La gestión debe usar el semestre 1 o 2")
expect(R, "Gestión/Create: correctiva con semestre 0 -> 'La gestión debe usar el semestre 1 o 2.' (ya no existe semestre 0)", make({"Input.Semester": "0"}), status=200, error="La gestión debe usar el semestre 1 o 2")
exist = c.submit("/Managements/Create?type=Preventive", {**OVER, "Input.Type": "Preventive", "Input.Year": "2026", "Input.Semester": "1", "Input.Status": "1"})
expect(R, "Gestión/Create: preventiva 2026-1 (código ya ocupado por la importación) da un mensaje claro", exist, status=200, error="Ya existe una gestión con el código 2026-1")

# ---------- creación válida de una correctiva inactiva (CORR-2026-1, semestral) ----------
n = e2e_lib.db_scalar("SELECT COUNT(*) FROM Managements WHERE Code=?", CODIGO)
if n == 0:
    ok = make({})
    expect(R, "Gestión/Create: correctiva válida 2026-1 (Inactiva)", ok, toast=CODIGO)
dup = make({})
expect(R, "Gestión/Create: correctiva duplicada", dup, status=200, error="Ya existe una gestión registrada")
cid = int(e2e_lib.db_scalar("SELECT TOP 1 Id FROM Managements WHERE Code=? AND [Status]<>99 ORDER BY Id DESC", CODIGO))

# ---------- editar ----------
ed = c.submit(f"/Managements/Edit/{cid}", {"Input.Description": "Correctiva QA editada", "Input.Status": "1"})
expect(R, "Gestión/Edit: cambia la descripción", ed, toast="Gestión actualizada correctamente")
desc = e2e_lib.db_scalar("SELECT Description FROM Managements WHERE Id=?", cid)
R.append(e2e_lib.Result("Gestión/Edit: la descripción quedó guardada", "Correctiva QA editada", desc))
bad = c.submit(f"/Managements/Edit/{cid}", {"Input.Description": "x", "Input.StartDate": "2026-06-30", "Input.PlannedEndDate": "2026-01-01"})
expect(R, "Gestión/Edit: fin anterior al inicio", bad, status=200, error="no puede ser anterior")
imp = c.probe("/Managements/Edit/4")
R.append(e2e_lib.Result("Gestión/Edit de una gestión importada ('Por confirmar') no se abre para editar", True, imp[0] in (302, 404, 403), f"status={imp}"))
nf = c.probe("/Managements/Edit/99999")
R.append(e2e_lib.Result("Gestión/Edit id inexistente", True, nf[0] in (302, 404), f"status={nf}"))

# ---------- activar: coexistencia preventiva Activa + correctiva Activa (una por tipo) ----------
# Activar otra correctiva CIERRA (Completada, sin vuelta atrás) la correctiva Activa anterior: solo se prueba si no hay
# otra correctiva Activa en uso por los demás scripts (p. ej. CORR-2027-2); si la hay, se omite.
otra = e2e_lib.db_scalar("SELECT COUNT(*) FROM Managements WHERE Type=1 AND [Status]=0 AND Id<>?", cid)
if otra == 0:
    st0 = c.raw_post(f"/Managements/Index?handler=ActivateManagement&id={cid}", [], follow=True, token_from="/Managements/Index?Type=Corrective")
    R.append(e2e_lib.Result("Gestión/Activar correctiva con la preventiva 30 Activa: se activa (coexistencia, una Activa por tipo)", 0,
                            e2e_lib.db_scalar("SELECT [Status] FROM Managements WHERE Id=?", cid), f"toasts={st0.toasts[:2]}"))
    R.append(e2e_lib.Result("Gestión/Activar una correctiva no cambia el Status (0) de la gestión preventiva 30", 0,
                            e2e_lib.db_scalar("SELECT [Status] FROM Managements WHERE Id=30"), ""))
    c.submit(f"/Managements/Edit/{cid}", {"Input.Description": "Correctiva QA editada", "Input.Status": "1"})
else:
    R.append(e2e_lib.Result("Gestión/Activar correctiva junto a la preventiva 30: omitida (ya hay otra correctiva Activa en uso)", True, True))

# ---------- eliminar ----------
d_active = c.raw_post("/Managements/Index?handler=DeleteLogical&id=30", [], follow=True, token_from="/Managements/Index?Type=Preventive")
st30 = e2e_lib.db_scalar("SELECT [Status] FROM Managements WHERE Id=30")
R.append(e2e_lib.Result("Gestión/Eliminar la preventiva activa 30 está bloqueado", 0, st30, f"toasts={d_active.toasts[:2]}"))
hist_id = e2e_lib.db_scalar("SELECT TOP 1 Id FROM Managements WHERE Type=9 AND [Status]<>99 ORDER BY Id")
st_hist = e2e_lib.db_scalar("SELECT [Status] FROM Managements WHERE Id=?", hist_id)
d_imp = c.raw_post(f"/Managements/Index?handler=DeleteLogical&id={hist_id}", [], follow=True, token_from="/Managements/Index?Type=PorConfirmar")
R.append(e2e_lib.Result("Gestión/Eliminar una histórica importada está bloqueado", st_hist,
                        e2e_lib.db_scalar("SELECT [Status] FROM Managements WHERE Id=?", hist_id), f"id={hist_id} toasts={d_imp.toasts[:2]}"))
d_ok = c.raw_post(f"/Managements/Index?handler=DeleteLogical&id={cid}", [], follow=True, token_from="/Managements/Index?Type=Corrective")
stc = e2e_lib.db_scalar("SELECT [Status] FROM Managements WHERE Id=?", cid)
R.append(e2e_lib.Result("Gestión/Eliminar (baja lógica) la correctiva creada por el script", 99, stc, f"toasts={d_ok.toasts[:2]}"))
st27fin = e2e_lib.db_scalar("SELECT [Status] FROM Managements WHERE Code='CORR-2027-2' AND Type=1")
R.append(e2e_lib.Result("Gestión/Final: la 30 intacta y CORR-2027-2 Activa (si existe), sin rastro Activo del script", True,
                        st27fin in (None, 0) and st30 == 0 and stc == 99, f"CORR-2027-2={st27fin} gestion30={st30} script={stc}"))

# ---------- roles ----------
for name, cl in [("Supervisor", sup), ("Administrator", adm)]:
    s, loc = cl.probe("/Managements/Index?Type=Preventive")
    R.append(e2e_lib.Result(f"Gestiones/Index como {name}", 302 if name == "Supervisor" else 200, s, f"loc={loc}"))

# ---------- semanas del dashboard (ToggleWeek) ----------
plan = 2
for label, fields, expected in [
    ("ejecutada sin planificar", [("planId", plan), ("weekNumber", 1), ("type", "executed")], (400, 200)),
    ("semana 9 fuera de rango", [("planId", plan), ("weekNumber", 9), ("type", "planned")], (400,)),
    ("semana 0 fuera de rango", [("planId", plan), ("weekNumber", 0), ("type", "planned")], (400,)),
]:
    r = c.raw_post("/Managements/Details?id=30&handler=ToggleWeek", [(k, str(v)) for k, v in fields], follow=False, token_from="/Managements/Details?id=30&ActiveTab=dashboard")
    R.append(e2e_lib.Result(f"Gestión/ToggleWeek: {label}", True, r.status in expected, f"status={r.status} body={(r.text or '')[:120]}"))

sys.exit(1 if e2e_lib.run_all([lambda: R]) else 0)
