"""
Checks E2E (solo lectura) del grupo PROCESOS contra la app local y la BD REAL.

Pantallas cubiertas: /Verifications (L-6), /Requests (L-7), /Maintenances (L-8),
/Managements (gestiones), /Acquisitions, /Departures (L-3), plan L-48
(MaintenancePlans), /Reports y /Notifications (solo salud).

Hallazgos documentados (no se inventan pruebas):
- /Verifications exige elegir laboratorio (FilterLaboratoryId); sin filtro muestra
  0 filas por diseño. El check itera los 24 laboratorios activos y suma.
  La pantalla agrupa por (laboratorio, fecha) = "sesiones"; la suma de los badges
  "N equipos" reconstruye el total de verificaciones visibles.
- /Maintenances no tiene pestañas Preventivo/Correctivo/PorConfirmar; su único
  filtro de agrupación es StatusFilter (estado). Suma por estado = total.
- /Managements exige ?Type=... (por defecto solo muestra Type=Preventive);
  las gestiones importadas son Type=PorConfirmar -> pestaña "Históricas".
- /Acquisitions no pagina (renderiza la lista completa Type=Purchasing).
- /Departures pagina con pageIndex (20/página) sobre la tabla Departures.
- MaintenancePlans (plan L-48, 387 filas en BD) NO tiene ninguna pantalla:
  grep de MaintenancePlan en Pages/** y Services/** no devuelve vistas;
  el tab "l48" de Managements/Details usa la tabla distinta ManagementPlans.
"""
import html
import re
import sys
from functools import partial

from e2e_lib import Result, Session, db_rows, db_scalar, run_all

# Badges "N equipos" de cada sesión en /Verifications
EQUIPOS_RE = re.compile(r">(\d+) equipos<")
# Enlaces "Ver Sesión": /Verifications/Details?labId=5&amp;date=2024-03-15
SESION_LINK_RE = re.compile(r"/Verifications/Details\?labId=(\d+)&amp;date=([0-9\-]{10})")


def squash(texto):
    """Normaliza texto visible vs BD: minúsculas, solo alfanumérico, entidades HTML resueltas."""
    return re.sub(r"[^a-z0-9]", "", html.unescape(texto or "").lower())


def check_verifications(s):
    """L-6: itera los laboratorios activos, suma sesiones y equipos visibles vs BD."""
    resultados = []
    labs = [fila[0] for fila in db_rows(
        "SELECT Id FROM Laboratories WHERE Status = 0 ORDER BY Id")]
    total_equipos, labs_ok, detalle_mal = 0, 0, []
    muestra_sesion = None

    for lab in labs:
        sesiones, equipos, pagina = set(), 0, 1
        while pagina <= 50:  # 15 sesiones/página; con 24 labs nunca llega al tope
            status, crudo = s.get(f"/Verifications?filterLaboratoryId={lab}&pageIndex={pagina}")
            if status != 200:
                break
            pares = {(int(a), b) for a, b in SESION_LINK_RE.findall(crudo)}
            nuevos = pares - sesiones
            if not pares or not nuevos:
                break  # evita doble conteo si alguna página repite filas
            sesiones |= nuevos
            equipos += sum(int(x) for x in EQUIPOS_RE.findall(crudo))
            if muestra_sesion is None:
                muestra_sesion = next(iter(nuevos))
            pagina += 1

        esperado_n, esperado_ses = db_rows(
            "SELECT COUNT(*), COUNT(DISTINCT CAST(v.[Date] AS date)) "
            "FROM Verifications v JOIN EquipmentUnits eu ON v.EquipmentUnitId = eu.Id "
            "WHERE v.Status <> 99 AND eu.LaboratoryId = ?", lab)[0]
        total_equipos += equipos
        if equipos == esperado_n and len(sesiones) == esperado_ses:
            labs_ok += 1
        else:
            detalle_mal.append(f"lab {lab}: equipos {equipos}/{esperado_n}, sesiones {len(sesiones)}/{esperado_ses}")

    resultados.append(Result(
        "Verifications: suma de equipos visibles (iterando labs activos) vs BD",
        db_scalar("SELECT COUNT(*) FROM Verifications WHERE Status <> 99"), total_equipos,
        "pantalla exige FilterLaboratoryId; sin filtro muestra 0 por diseño"))
    resultados.append(Result(
        "Verifications: sesiones y equipos por laboratorio vs BD (agrupado por lab de la unidad)",
        f"{len(labs)}/{len(labs)} laboratorios coinciden",
        f"{labs_ok}/{len(labs)} laboratorios coinciden",
        "; ".join(detalle_mal) if detalle_mal else "coincidencia exacta por laboratorio"))

    if muestra_sesion:
        lab0, fecha0 = muestra_sesion
        resultados.append(Result(
            "Verifications: Details de sesión (labId+date) abre sin error",
            True, s.healthy(f"/Verifications/Details?labId={lab0}&date={fecha0}"),
            "la pantalla enlaza Details por labId+date, no por id"))
    vid = db_scalar("SELECT MIN(Id) FROM Verifications WHERE Status <> 99")
    if vid:
        resultados.append(Result(
            "Verifications: Details por id abre sin error", True, s.healthy(f"/Verifications/Details?id={vid}")))
    return resultados


def check_requests(s):
    """L-7: enlaces Details únicos en todas las páginas vs BD; 2 Details muestran su descripción."""
    resultados = []
    ids = s.collect("/Requests", r"/Requests/Details/(\d+)")
    esperado = db_scalar("SELECT COUNT(*) FROM Requests WHERE Status <> 99")
    resultados.append(Result(
        "Requests: enlaces Details únicos en todas las páginas vs BD", esperado, len(ids),
        "sin filtro por defecto; EF oculta Status=99; 20/página vía pageIndex"))

    for rid in sorted(ids, key=int)[:2]:
        desc = db_scalar("SELECT [Description] FROM Requests WHERE Id = ?", int(rid))
        status, texto = s.text(f"/Requests/Details/{rid}")
        visible = status == 200 and bool(desc) and squash(desc) in squash(texto)
        resultados.append(Result(
            f"Requests: Details {rid} muestra su descripción", True, visible))
    return resultados


def check_maintenances(s):
    """L-8: enlaces Details únicos vs BD; suma por filtro de estado; 2 Details con costo y técnico."""
    resultados = []
    ids = s.collect("/Maintenances", r"/Maintenances/Details/(\d+)")
    esperado = db_scalar("SELECT COUNT(*) FROM Maintenances WHERE Status <> 99")
    resultados.append(Result(
        "Maintenances: enlaces Details únicos en todas las páginas vs BD", esperado, len(ids),
        "sin filtro por defecto; EF oculta Status=99; 20/página vía pageIndex"))

    # No existen pestañas Preventivo/Correctivo: el agrupador disponible es StatusFilter.
    grupos_bd = {estado: n for estado, n in db_rows(
        "SELECT Status, COUNT(*) FROM Maintenances WHERE Status <> 99 GROUP BY Status")}
    suma, detalle = 0, []
    for estado in (0, 1, 2, 3):  # Pending, InProgress, Completed, Scheduled
        _, texto = s.text(f"/Maintenances?statusFilter={estado}")
        m = re.search(r"(\d+) mantenimiento\(s\)", texto)
        n = int(m.group(1)) if m else -1
        suma += n
        detalle.append(f"estado {estado}: pantalla={n} bd={grupos_bd.get(estado, 0)}")
    resultados.append(Result(
        "Maintenances: suma del contador por filtro de estado vs BD",
        sum(grupos_bd.values()), suma, " ".join(detalle)))

    for mid in sorted(ids, key=int)[:2]:
        mid = int(mid)
        costo_bd = db_scalar("SELECT ActualCost FROM Maintenances WHERE Id = ?", mid)
        status, texto = s.text(f"/Maintenances/Details/{mid}")
        bajo = texto.lower()
        costo_ok = "real declarado" in bajo and (costo_bd is None or "bs " in bajo)
        tecnico_ok = ("técnico responsable" in bajo) or ("participante" in bajo)
        resultados.append(Result(
            f"Maintenances: Details {mid} muestra costo (ActualCost) y técnico/participante",
            True, status == 200 and costo_ok and tecnico_ok,
            f"ActualCost en BD = {costo_bd}"))
    return resultados


def check_managements(s):
    """Gestiones: cuenta por pestaña Type vs BD agrupado por Type; abre 2 Details."""
    resultados = []
    tipos = {"Preventive": 0, "Corrective": 1, "PorConfirmar": 9}
    esperado = {tipo: n for tipo, n in db_rows(
        "SELECT [Type], COUNT(*) FROM Managements WHERE Status <> 99 GROUP BY [Type]")}
    for nombre, valor in tipos.items():
        ids = s.collect(f"/Managements?Type={nombre}", r"/Managements/Details\?id=(\d+)")
        resultados.append(Result(
            f"Managements: pestaña Type={nombre} registros visibles vs BD",
            esperado.get(valor, 0), len(ids),
            "por defecto la pantalla solo muestra Preventive; importadas están en PorConfirmar (Históricas)"))

    for mid, codigo in db_rows(
            "SELECT TOP 2 Id, Code FROM Managements WHERE Status <> 99 ORDER BY Id"):
        status, texto = s.text(f"/Managements/Details?id={mid}&ActiveTab=dashboard")
        resultados.append(Result(
            f"Managements: Details {mid} ({codigo}) abre mostrando su código",
            True, status == 200 and squash(codigo) in squash(texto)))
    return resultados


def check_acquisitions(s):
    """Solicitudes de adquisición: tabla Requests con Type=Purchasing; sin paginación."""
    esperado = db_scalar("SELECT COUNT(*) FROM Requests WHERE Type = 2 AND Status <> 99")
    _, texto = s.text("/Acquisitions")
    m = re.search(r"(\d+) solicitud\(es\)", texto)
    vacio_ok = esperado == 0 and "no se encontraron solicitudes" in texto.lower()
    return [
        Result("Acquisitions: contador visible vs BD (Requests Type=Purchasing)",
               esperado, int(m.group(1)) if m else (0 if vacio_ok else -1),
               "la pantalla no pagina: renderiza la lista completa"),
        Result("Acquisitions: pantalla abre sin error y con estado vacío correcto",
               True, s.healthy("/Acquisitions") and vacio_ok),
    ]


def check_departures(s):
    """Salidas L-3: tabla Departures; pagina con pageIndex (20/página)."""
    esperado = db_scalar("SELECT COUNT(*) FROM Departures WHERE Status <> 99")
    _, texto = s.text("/Departures")
    m = re.search(r"(\d+) salida\(es\)", texto)
    vacio_ok = esperado == 0 and "no hay salidas registradas" in texto.lower()
    return [
        Result("Departures: contador visible vs BD", esperado, int(m.group(1)) if m else (0 if vacio_ok else -1),
               "paginación pageIndex 20/página; con 0 filas no hay páginas que recorrer"),
        Result("Departures: pantalla abre sin error y con estado vacío correcto",
               True, s.healthy("/Departures") and vacio_ok),
    ]


def check_plan_l48(s):
    """Plan de mantenimiento L-48: MaintenancePlans no tiene pantalla; se reporta como hallazgo."""
    total_bd = db_scalar("SELECT COUNT(*) FROM MaintenancePlans")
    return [
        Result("L-48 MaintenancePlans: total en BD", 387, total_bd),
        # La pantalla /Maintenances/Plan (solo lectura) se añadió tras este hallazgo; su conteo se valida en checks_nuevas.py.
        Result("L-48 MaintenancePlans: pantalla /Maintenances/Plan responde",
               True, s.healthy("/Maintenances/Plan")),
    ]


def check_salud(s):
    """Solo salud: 200 sin excepción."""
    return [
        Result("Reports: salud (200 sin excepción)", True, s.healthy("/Reports")),
        Result("Notifications: salud (200 sin excepción)", True, s.healthy("/Notifications")),
    ]


CHECKS = [
    check_verifications,
    check_requests,
    check_maintenances,
    check_managements,
    check_acquisitions,
    check_departures,
    check_plan_l48,
    check_salud,
]


if __name__ == "__main__":
    sesion = Session()  # una sola sesión (login compartido) para todos los checks
    sys.exit(1 if run_all([partial(fn, sesion) for fn in CHECKS]) else 0)
