"""Gestiones por HTTP (solo copia QA): crear, validar, editar, activar, eliminar y semanas. No toca la gestión 30 (preventiva 2027-2 en uso)."""
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
OVER = {"Input.Year": "2026", "Input.Semester": "0", "Input.Description": "Correctiva QA", "Input.StartDate": "2026-01-01",
        "Input.PlannedEndDate": "2026-12-31", "Input.Status": "1", "Input.Type": "Corrective", "Input.FacultyId": ""}


def make(over):
    return c.submit(CREATE, {**OVER, **over})


# ---------- validaciones de Create ----------
expect(R, "Gestión/Create: año 1999", make({"Input.Year": "1999"}), status=200)
expect(R, "Gestión/Create: año 2101", make({"Input.Year": "2101"}), status=200)
expect(R, "Gestión/Create: fin anterior al inicio", make({"Input.StartDate": "2026-12-31", "Input.PlannedEndDate": "2026-01-01"}), status=200, error="no puede ser anterior")
expect(R, "Gestión/Create: tipo 'Por confirmar' (históricas) no se crea", make({"Input.Type": "PorConfirmar"}), status=200)
expect(R, "Gestión/Create: estado Eliminada no se acepta", make({"Input.Status": "99"}), status=200)
pre = c.submit("/Managements/Create?type=Preventive", {**OVER, "Input.Type": "Preventive", "Input.Semester": "3"})
expect(R, "Gestión/Create: preventiva con semestre 3", pre, status=200, error="semestre 1 o 2")
exist = c.submit("/Managements/Create?type=Preventive", {**OVER, "Input.Type": "Preventive", "Input.Year": "2026", "Input.Semester": "1", "Input.Status": "1"})
expect(R, "Gestión/Create: preventiva 2026-1 (código ya ocupado por la importación) da un mensaje claro", exist, status=200, error="Ya existe una gestión con el código 2026-1")

# ---------- creación válida de una correctiva inactiva ----------
n = e2e_lib.db_scalar("SELECT COUNT(*) FROM Managements WHERE Code='CORR-2026-0'")
if n == 0:
    ok = make({})
    expect(R, "Gestión/Create: correctiva válida (Inactiva)", ok, toast="CORR-2026-0")
dup = make({})
expect(R, "Gestión/Create: correctiva duplicada", dup, status=200, error="Ya existe una gestion registrada")
cid = int(e2e_lib.db_scalar("SELECT TOP 1 Id FROM Managements WHERE Code='CORR-2026-0' AND [Status]<>99 ORDER BY Id DESC"))

# ---------- editar ----------
ed = c.submit(f"/Managements/Edit/{cid}", {"Input.Description": "Correctiva QA editada", "Input.Status": "1"})
expect(R, "Gestión/Edit: cambia la descripción", ed, toast="Gestión actualizada correctamente")
desc = e2e_lib.db_scalar("SELECT Description FROM Managements WHERE Id=?", cid)
R.append(e2e_lib.Result("Gestión/Edit: la descripción quedó guardada", "Correctiva QA editada", desc))
bad = c.submit(f"/Managements/Edit/{cid}", {"Input.Description": "x", "Input.StartDate": "2026-12-31", "Input.PlannedEndDate": "2026-01-01"})
expect(R, "Gestión/Edit: fin anterior al inicio", bad, status=200, error="no puede ser anterior")
imp = c.probe("/Managements/Edit/4")
R.append(e2e_lib.Result("Gestión/Edit de una gestión importada ('Por confirmar') no se abre para editar", True, imp[0] in (302, 404, 403), f"status={imp}"))
nf = c.probe("/Managements/Edit/99999")
R.append(e2e_lib.Result("Gestión/Edit id inexistente", True, nf[0] in (302, 404), f"status={nf}"))

# ---------- activar con otra Activa (índice único global) ----------
st0 = c.raw_post(f"/Managements/Index?handler=ActivateManagement&id={cid}", [], follow=True, token_from="/Managements/Index?Type=Corrective")
statuses = e2e_lib.db_scalar("SELECT [Status] FROM Managements WHERE Id=?", cid)
R.append(e2e_lib.Result("Gestión/Activar correctiva mientras la preventiva 30 sigue Activa: informa el motivo o la activa", True,
                        statuses in (0, 1), f"status BD={statuses} errores={st0.errors[:2]} toasts={st0.toasts[:2]}"))
# ---------- eliminar ----------
d_active = c.raw_post("/Managements/Index?handler=DeleteLogical&id=30", [], follow=True, token_from="/Managements/Index?Type=Preventive")
st30 = e2e_lib.db_scalar("SELECT [Status] FROM Managements WHERE Id=30")
R.append(e2e_lib.Result("Gestión/Eliminar la preventiva activa 30 está bloqueado", 0, st30, f"toasts={d_active.toasts[:2]}"))
d_imp = c.raw_post("/Managements/Index?handler=DeleteLogical&id=3", [], follow=True, token_from="/Managements/Index?Type=PorConfirmar")
st3 = e2e_lib.db_scalar("SELECT [Status] FROM Managements WHERE Id=3")
R.append(e2e_lib.Result("Gestión/Eliminar una histórica importada está bloqueado", 2, st3, f"toasts={d_imp.toasts[:2]} (2 = Completada)"))
d_ok = c.raw_post(f"/Managements/Index?handler=DeleteLogical&id={cid}", [], follow=True, token_from="/Managements/Index?Type=Corrective")
stc = e2e_lib.db_scalar("SELECT [Status] FROM Managements WHERE Id=?", cid)
R.append(e2e_lib.Result("Gestión/Eliminar (baja lógica) una correctiva inactiva", 99, stc, f"toasts={d_ok.toasts[:2]}"))

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
