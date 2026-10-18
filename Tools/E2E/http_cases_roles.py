"""Matriz de roles por HTTP (solo GET sin seguir redirects) contra la copia QA.
Usuarios (creados por /Users/Create con la clave de E2E_PW): qa_super (SuperAdmin), qa_adm (Administrador), qa_sup (Supervisor), qa_cierre (PracticeClosure).
Resultado observado: OK (200) | DENY (302 a /Error) | CIERRE (302 a /Cierre) | LOGIN (302 a /Login) | 404 | otro.
"""
import os
import sys

import e2e_lib
from http_forms import Client

PW = os.environ["E2E_PW"]
ROLES = {"SA": "qa_super", "ADM": "qa_adm", "SUP": "qa_sup", "PC": "qa_cierre"}

# Convenciones de acceso esperado (según AuthorizationHelper y los [Authorize] de cada página)
A = {"SA": "OK", "ADM": "OK", "SUP": "DENY", "PC": "CIERRE"}            # solo Administrador/SuperAdmin
M = {"SA": "OK", "ADM": "OK", "SUP": "OK", "PC": "CIERRE"}              # Supervisor + Administrador + SuperAdmin
SAONLY = {"SA": "OK", "ADM": "DENY", "SUP": "DENY", "PC": "CIERRE"}     # solo SuperAdmin
CLO = {"SA": "OK", "ADM": "OK", "SUP": "OK", "PC": "OK"}                # incluye PracticeClosure
AUTH = {"SA": "OK", "ADM": "OK", "SUP": "OK", "PC": "CIERRE"}           # cualquier autenticado (PC queda fuera por el middleware)
PCOK = {"SA": "OK", "ADM": "OK", "SUP": "OK", "PC": "OK"}               # páginas que PC sí puede ver (Notifications)

CASES = [
    ("/", {"SA": "OK", "ADM": "OK", "SUP": "OK", "PC": "CIERRE"}),
    ("/Notifications", PCOK),
    ("/Cierre", CLO),
    ("/LaboratoryOperations/L3?id=1038", CLO),
    ("/LaboratoryOperations/L5?planId=1038", CLO),
    ("/LaboratoryOperations", AUTH),
    ("/LaboratoryOperations/Planificaciones?managementId=4", M),
    ("/LaboratoryOperations/Grilla?managementId=4", M),
    ("/LaboratoryOperations/CalendarioAcademico?managementId=4", M),
    ("/LaboratoryOperations/Demanda?managementId=4", AUTH),
    ("/LaboratoryOperations/Reposiciones?managementId=4", M),
    ("/LaboratoryOperations/Conflictos?managementId=4", M),
    ("/LaboratoryOperations/Planificacion?managementId=4", AUTH),
    ("/LaboratoryOperations/Requerimientos?planId=1038", AUTH),
    ("/LaboratoryOperations/Disponibilidad?planId=1038", AUTH),
    ("/AssetView", M),
    ("/AssetView/Units", M),
    ("/Equipment", A),
    ("/Equipment/Create", A),
    ("/Equipment/Details/1", M),
    ("/EquipmentUnits", A),
    ("/EquipmentUnits/Details?id=1", M),
    ("/EquipmentUnits/Create", M),
    ("/Verifications", A),
    ("/Requests", M),
    ("/Requests/Details/158", M),
    ("/Maintenances", A),
    ("/Maintenances/Plan", A),
    ("/Departures", A),
    ("/Acquisitions", M),
    ("/Acquisitions/Create", A),
    ("/Schedules", AUTH),
    ("/Schedules/Practices", AUTH),
    ("/Reports", M),
    ("/Managements?type=Preventive", A),
    ("/Managements/Create?type=Preventive", A),
    ("/Laboratories", A),
    ("/Cities", A),
    ("/Countries", A),
    ("/Faculties", A),
    ("/Career", AUTH),
    ("/Career/Create", A),
    ("/Persons", A),
    ("/Users", A),
    ("/Users/Create", A),
    ("/Catalogs/Subjects", A),
    ("/Catalogs/Practices", A),
    ("/Catalogs/Articles", A),
    ("/Catalogs/ArticleCategories", A),
    ("/Catalogs/MeasurementUnits", A),
    ("/Catalogs/Stock", A),
    ("/Catalogs/Stock/Import", A),
    ("/Catalogs/CourseGroups", A),
    ("/Catalogs/StudentGroups", A),
    ("/Admin/TestConnection", SAONLY),
    ("/Migration/Import", SAONLY),
    ("/Api/Notifications?handler=Unread&scope=all&take=5", PCOK),
    ("/Error", {"SA": "OK", "ADM": "OK", "SUP": "OK", "PC": "OK"}),
]


def classify(status, location):
    loc = (location or "").lower()
    if status == 200:
        return "OK"
    if status in (301, 302, 303):
        if "/error" in loc:
            return "DENY"
        if "/cierre" in loc:
            return "CIERRE"
        if "/login" in loc:
            return "LOGIN"
        return f"302->{location}"
    return str(status)


R = []
clients = {role: Client(user, PW) for role, user in ROLES.items()}
for path, expected in CASES:
    for role, c in clients.items():
        st, loc = c.probe(path)
        got = classify(st, loc)
        # /Error redirige 302 a sí mismo? se acepta 200
        R.append(e2e_lib.Result(f"[{role}] GET {path}", expected[role], got, "" if got == expected[role] else f"(status={st}, loc={loc})"))

# Sin sesión: todo debe ir a /Login
anon = Client.__new__(Client)
import http.cookiejar, urllib.request
anon.jar = http.cookiejar.CookieJar()
anon.opener = urllib.request.build_opener(urllib.request.HTTPCookieProcessor(anon.jar))
for path in ["/", "/Users", "/LaboratoryOperations/Grilla?managementId=4", "/Catalogs/Stock", "/Api/Notifications?handler=Unread&scope=all"]:
    st, loc = anon.probe(path)
    R.append(e2e_lib.Result(f"[ANON] GET {path}", "LOGIN", classify(st, loc), f"(status={st}, loc={loc})"))

# POST sin token antifalsificación → 400 (con sesión SuperAdmin)
for path in ["/Countries/Create", "/Logout", "/LaboratoryOperations/L3?id=1038&handler=Finalize", "/Catalogs/Stock/Edit/21/601"]:
    st, loc = clients["SA"].probe(path, "POST")
    R.append(e2e_lib.Result(f"[SA] POST sin token {path}", 400, st, f"(loc={loc})"))

sys.exit(1 if e2e_lib.run_all([lambda: R]) else 0)
