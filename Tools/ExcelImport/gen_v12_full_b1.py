"""
Importador único V12, parte B1: VerificacionesL6, SolicitudesL7, SolicitudUnidades, MantenimientosL8,
MantSolicitudes, MantParticipantes y PlanL48 -> DB_Laboratorios_Univalle_REAL.
Requiere haber cargado antes la parte A (gestiones, actores, unidades).

Textos tal cual del Excel. Columnas del Excel sin campo propio quedan en ImportSourceRows.OriginalDataJson.
Uso:  python gen_v12_full_b1.py [--dry-run]   ->  %TEMP%\\v12full\\parte_b1.sql
"""
import sys
from collections import defaultdict
from datetime import date, datetime

from v12_common import SqlBuilder, check_len, check_sha, q, read_sheets, text

SHEETS = ["UnidadesFisicas", "VerificacionesL6", "SolicitudesL7", "SolicitudUnidades", "MantenimientosL8",
          "MantSolicitudes", "MantParticipantes", "PlanL48"]

PHYSICAL = {"excelente": 5, "bueno": 4, "regular": 3, "malo": 2, "baja del equipo": 1, "por confirmar": 9}
OBSERVED = {"operativo": 0, "no operativo": 2, "por confirmar": 98}
MAINT_TYPE = {"preventivo": 1, "correctivo": 2, "por confirmar": 9}
SERVICE_TYPE = {"interno": 0, "externo": 1, "por confirmar": 9}


def low(v):
    return (text(v) or "").casefold()


def pending(v):
    return low(v) in ("por confirmar", "sin especificar")


def as_date(v):
    """Fecha de Excel (datetime) o texto AAAA-MM-DD -> date; None si vacío."""
    if v is None or str(v).strip() == "":
        return None
    if isinstance(v, datetime):
        return v.date()
    if isinstance(v, date):
        return v
    return datetime.strptime(str(v).strip(), "%Y-%m-%d").date()


def person(code):
    return f"(SELECT Id FROM dbo.People WHERE ActorCode = {q(code)})" if text(code) else "NULL"


def unit(inv):
    return f"(SELECT Id FROM dbo.EquipmentUnits WHERE InventoryNumber = {q(str(int(inv)))})"


def mgmt(code):
    return f"(SELECT Id FROM dbo.Managements WHERE Code = {q(code)})"


def main(dry_run):
    sha = check_sha()
    data = read_sheets(SHEETS)
    for name, (_, rows) in data.items():
        print(f"[hoja] {name}: {len(rows)} filas")
    if dry_run:
        return

    lab_of_unit = {str(int(r["Numeroinventario"])): text(r["Laboratoriocodigo"]) for r in data["UnidadesFisicas"][1]}
    s = SqlBuilder()
    s.prologue(sha, "Importación completa V12 (parte B1: L-6, L-7, L-8 y L-48).")
    sw = s.stmt

    # ---- L-6 Verificaciones ----
    hdr, rows = data["VerificacionesL6"]
    s.w("-- L-6 Verificaciones")
    s.begin_group()
    for r in rows:
        c = r["Verificacioncodigo"]
        cond = PHYSICAL.get(low(r["Condicionfisica"]))
        obs_status = OBSERVED.get(low(r["Estadoequipo"]))
        if cond is None or obs_status is None:
            sys.exit(f"{c}: condición/estado no mapeado")
        observations = "\n".join(x for x in (text(r["Problemasobservados"]), text(r["Observaciones"])) if x) or None
        sw(f"IF NOT EXISTS (SELECT 1 FROM dbo.Verifications WHERE HistoricalSourceKey = {q('V12:' + c)}) "
           f"INSERT dbo.Verifications(HistoricalSourceKey, EquipmentUnitId, ManagementId, Date, Observations, PhysicalCondition, "
           f"ObservedEquipmentStatus, ResponsiblePersonId, Status, CreatedDate, ImportBatchId) "
           f"VALUES({q('V12:' + c)}, {unit(r['Numeroinventario'])}, {mgmt(r['Gestioncodigo'])}, {q(as_date(r['Fechaverificacion']))}, "
           f"{q(observations)}, {cond}, {obs_status}, {person(r['Responsableactorcodigo'])}, 1, @Now, @Batch);")
        s.source_row("VerificacionesL6", hdr, r, c, "Verification", "V12:" + c)
        for col in ("Condicionfisica", "Estadoequipo"):
            if pending(r[col]):
                s.issue("VALUE_PENDING_CONFIRMATION", "Verification", c, col, text(r[col]), "PorConfirmar",
                        f"{col} 'Por confirmar' en la fuente.", 1, "VerificacionesL6", r)
    s.end_group()

    # ---- L-7 Solicitudes ----
    hdr_req, reqs = data["SolicitudesL7"]
    hdr_su, req_units = data["SolicitudUnidades"]
    units_by_req = defaultdict(list)
    for r in req_units:
        units_by_req[r["Solicitudcodigo"]].append(str(int(r["Numeroinventario"])))
    s.w("-- L-7 Solicitudes de mantenimiento")
    s.begin_group()
    for r in reqs:
        c = r["Solicitudcodigo"]
        desc = text(r["Problemareportado"])
        if not desc:
            sys.exit(f"{c}: Problemareportado vacío (columna obligatoria)")
        check_len("SolicitudesL7", r, "Problemareportado", desc, 1000)
        check_len("SolicitudesL7", r, "Sugerencia", r["Sugerencia"], 2000)
        us = units_by_req.get(c, [])
        labs = {lab_of_unit[u] for u in us}
        lab = next(iter(labs)) if len(labs) == 1 else None
        unit_expr = unit(us[0]) if len(us) == 1 else "NULL"
        equip_expr = (f"(SELECT EquipmentId FROM dbo.EquipmentUnits WHERE InventoryNumber = {q(us[0])})" if len(us) == 1 else "NULL")
        lab_expr = f"(SELECT Id FROM dbo.Laboratories WHERE Code = {q(lab)})" if lab else "NULL"
        sw(f"IF NOT EXISTS (SELECT 1 FROM dbo.Requests WHERE HistoricalSourceKey = {q('V12:' + c)}) "
           f"INSERT dbo.Requests(HistoricalSourceKey, ManagementId, RequestDate, Priority, Description, Suggestion, EstimatedRepairTime, "
           f"RequestedByPersonId, Type, Status, LaboratoryId, EquipmentId, EquipmentUnitId, LocationResolutionStatus, CreatedDate, ImportBatchId) "
           f"VALUES({q('V12:' + c)}, {mgmt(r['Gestioncodigo'])}, {q(as_date(r['Fechasolicitud']))}, 9, {q(desc)}, {q(r['Sugerencia'])}, "
           f"{q(r['Tiempoestimado'])}, {person(r['Solicitanteactorcodigo'])}, 1, 98, {lab_expr}, {equip_expr}, {unit_expr}, "
           f"{1 if lab else 0}, @Now, @Batch);")
        s.source_row("SolicitudesL7", hdr_req, r, c, "Request", "V12:" + c)
        for col in ("Prioridad", "Estado"):
            if pending(r[col]):
                s.issue("VALUE_PENDING_CONFIRMATION", "Request", c, col, text(r[col]), "PorConfirmar",
                        f"{col} sin especificar en la fuente.", 0, "SolicitudesL7", r)
        if not us:
            s.issue("REQUEST_WITHOUT_UNITS", "Request", c, "SolicitudUnidades", None, None,
                    "La solicitud no tiene unidades en SolicitudUnidades: LocationResolutionStatus = Pending.", 1, "SolicitudesL7", r)
    s.end_group()

    s.w("-- L-7 Unidades por solicitud")
    s.begin_group()
    seen_first = set()
    for r in req_units:
        c = r["Solicitudunidadcodigo"]
        rc = r["Solicitudcodigo"]
        primary = 1 if rc not in seen_first else 0
        seen_first.add(rc)
        req_id = f"(SELECT Id FROM dbo.Requests WHERE HistoricalSourceKey = {q('V12:' + rc)})"
        sw(f"IF NOT EXISTS (SELECT 1 FROM dbo.RequestEquipmentUnits WHERE RequestId = {req_id} AND EquipmentUnitId = {unit(r['Numeroinventario'])}) "
           f"INSERT dbo.RequestEquipmentUnits(RequestId, EquipmentUnitId, IsLegacyPrimary, IsActive, CreatedDate) "
           f"VALUES({req_id}, {unit(r['Numeroinventario'])}, {primary}, 1, @Now);")
        s.source_row("SolicitudUnidades", hdr_su, r, c, "RequestEquipmentUnit", f"{rc}|{int(r['Numeroinventario'])}")
    s.end_group()

    # ---- L-8 Mantenimientos ----
    hdr_m, mants = data["MantenimientosL8"]
    hdr_ms, links = data["MantSolicitudes"]
    hdr_p, parts = data["MantParticipantes"]
    # RequestId de Maintenances: primer vínculo de cada mantenimiento si la solicitud no está ya tomada (índice único).
    first_link = {}
    for r in links:
        first_link.setdefault(r["Mantenimientocodigo"], r["Solicitudcodigo"])
    taken, primary_req = set(), {}
    for m in mants:
        mc = m["Mantenimientocodigo"]
        rc = first_link.get(mc)
        if rc and rc not in taken:
            taken.add(rc)
            primary_req[mc] = rc
    principal = {}
    for r in parts:
        if low(r["Esprincipal"]) in ("sí", "si") and low(r["Estado"]) == "activo":
            if r["Mantenimientocodigo"] in principal:
                sys.exit(f"Dos participantes principales en {r['Mantenimientocodigo']}")
            principal[r["Mantenimientocodigo"]] = r["Actorcodigo"]
    s.w("-- L-8 Mantenimientos")
    s.begin_group()
    for r in mants:
        c = r["Mantenimientocodigo"]
        mtype, stype = MAINT_TYPE.get(low(r["Tipomantenimiento"])), SERVICE_TYPE.get(low(r["Tiposervicio"]))
        if mtype is None or stype is None or low(r["Estado"]) != "completado":
            sys.exit(f"{c}: tipo/servicio/estado no mapeado")
        check_len("MantenimientosL8", r, "Trabajorealizado", r["Trabajorealizado"], 2000)
        check_len("MantenimientosL8", r, "Recomendaciones", r["Recomendaciones"], 1000)
        rc = primary_req.get(c)
        req_expr = f"(SELECT Id FROM dbo.Requests WHERE HistoricalSourceKey = {q('V12:' + rc)})" if rc else "NULL"
        tech = person(principal.get(c))
        cost = r["Costototalfuentebs"]
        sw(f"IF NOT EXISTS (SELECT 1 FROM dbo.Maintenances WHERE HistoricalSourceKey = {q('V12:' + c)}) "
           f"INSERT dbo.Maintenances(HistoricalSourceKey, EquipmentUnitId, MaintenanceType, ManagementId, ServiceType, TechnicianId, RequestId, "
           f"ScheduledDate, StartDate, EndDate, Description, Status, CompletionPercentage, Step1_Cleaning, Step2_Calibration, Step3_Testing, "
           f"Step4_FinalReview, ActualCost, Recommendations, SuggestedNextMaintenanceDate, SatisfactionLevel, QualityStatus, CreatedDate, ImportBatchId) "
           f"VALUES({q('V12:' + c)}, {unit(r['Numeroinventario'])}, {mtype}, {mgmt(r['Gestioncodigo'])}, {stype}, {tech}, {req_expr}, "
           f"{q(as_date(r['Fechaprogramada']))}, {q(as_date(r['Fechainicio']))}, {q(as_date(r['Fechafin']))}, {q(r['Trabajorealizado'])}, "
           f"2, 100, 0, 0, 0, 0, {q(cost)}, {q(r['Recomendaciones'])}, {q(as_date(r['Proximomantenimiento']))}, {q(r['Satisfaccion'])}, "
           f"{q(r['Estadocalidad'])}, @Now, @Batch);")
        s.source_row("MantenimientosL8", hdr_m, r, c, "Maintenance", "V12:" + c)
        for col in ("Tipomantenimiento", "Tiposervicio"):
            if pending(r[col]):
                s.issue("VALUE_PENDING_CONFIRMATION", "Maintenance", c, col, text(r[col]), "PorConfirmar",
                        f"{col} 'Por confirmar' en la fuente.", 0, "MantenimientosL8", r)
        if low(r["Estadocalidad"]) != "válida":
            s.issue("CONTENT_REVIEW_REQUIRED", "Maintenance", c, "Estadocalidad", text(r["Estadocalidad"]), None,
                    "La fuente marca el registro como 'Requiere revisión' (guardado en Maintenance.QualityStatus).", 0, "MantenimientosL8", r)
    s.end_group()

    s.w("-- L-8 Vínculos mantenimiento-solicitud")
    s.begin_group()
    for r in links:
        c = r["Mantenimientosolicitudcodigo"]
        mc, rc = r["Mantenimientocodigo"], r["Solicitudcodigo"]
        legacy = 1 if primary_req.get(mc) == rc else 0
        mid = f"(SELECT Id FROM dbo.Maintenances WHERE HistoricalSourceKey = {q('V12:' + mc)})"
        rid = f"(SELECT Id FROM dbo.Requests WHERE HistoricalSourceKey = {q('V12:' + rc)})"
        sw(f"IF NOT EXISTS (SELECT 1 FROM dbo.MaintenanceRequests WHERE MaintenanceId = {mid} AND RequestId = {rid}) "
           f"INSERT dbo.MaintenanceRequests(MaintenanceId, RequestId, IsLegacyPrimary, IsActive, CreatedDate) "
           f"VALUES({mid}, {rid}, {legacy}, 1, @Now);")
        s.source_row("MantSolicitudes", hdr_ms, r, c, "MaintenanceRequest", f"{mc}|{rc}")
    s.end_group()

    s.w("-- L-8 Participantes")
    s.begin_group()
    for r in parts:
        c = r["Participantecodigo"]
        mc = r["Mantenimientocodigo"]
        is_primary = 1 if low(r["Esprincipal"]) in ("sí", "si") else 0
        mid = f"(SELECT Id FROM dbo.Maintenances WHERE HistoricalSourceKey = {q('V12:' + mc)})"
        pid = person(r["Actorcodigo"])
        active = 1 if low(r["Estado"]) == "activo" else 0
        sw(f"IF NOT EXISTS (SELECT 1 FROM dbo.MaintenanceParticipants WHERE MaintenanceId = {mid} AND PersonId = {pid} AND Role = 1) "
           f"INSERT dbo.MaintenanceParticipants(MaintenanceId, PersonId, Role, IsPrimary, IsActive, AssignedAt) "
           f"VALUES({mid}, {pid}, 1, {is_primary}, {active}, @Now);")
        s.source_row("MantParticipantes", hdr_p, r, c, "MaintenanceParticipant", f"{mc}|{r['Actorcodigo']}")
    s.end_group()

    # ---- L-48 Plan de mantenimiento ----
    hdr, rows = data["PlanL48"]
    s.w("-- L-48 Plan de mantenimiento")
    s.begin_group()
    for r in rows:
        c = r["Plancodigo"]
        mtype = MAINT_TYPE.get(low(r["Tipomantenimiento"]))
        stype = SERVICE_TYPE.get(low(r["Tiposervicio"]))
        if mtype is None or stype is None:
            sys.exit(f"{c}: tipo/servicio no mapeado")
        stype_sql = "NULL" if stype == 9 else str(stype)  # el check de MaintenancePlans solo admite 0/1 o NULL
        lab = lab_of_unit[str(int(r["Numeroinventario"]))]
        sw(f"IF NOT EXISTS (SELECT 1 FROM dbo.MaintenancePlans WHERE HistoricalSourceKey = {q('V12:' + c)}) "
           f"INSERT dbo.MaintenancePlans(HistoricalSourceKey, PlanCode, ManagementId, EquipmentUnitId, LaboratoryId, MaintenanceType, "
           f"ServiceType, ResponsiblePersonId, PlannedDate, Status, QualityStatus, CreatedDate, ImportBatchId) "
           f"VALUES({q('V12:' + c)}, {q(c)}, {mgmt(r['Gestioncodigo'])}, {unit(r['Numeroinventario'])}, "
           f"(SELECT Id FROM dbo.Laboratories WHERE Code = {q(lab)}), {mtype}, {stype_sql}, {person(r['Responsableactorcodigo'])}, "
           f"{q(as_date(r['Fechaplanificada']))}, NULL, {q(r['Estadocalidad'])}, @Now, @Batch);")
        s.source_row("PlanL48", hdr, r, c, "MaintenancePlan", "V12:" + c)
        for col in ("Tipomantenimiento", "Tiposervicio", "Estado"):
            if pending(r[col]):
                s.issue("VALUE_PENDING_CONFIRMATION", "MaintenancePlan", c, col, text(r[col]), None,
                        f"{col} 'Por confirmar' en la fuente.", 0, "PlanL48", r)
        if low(r["Estadocalidad"]) != "válida":
            s.issue("CONTENT_REVIEW_REQUIRED", "MaintenancePlan", c, "Estadocalidad", text(r["Estadocalidad"]), None,
                    "La fuente marca el registro como 'Requiere revisión' (guardado en MaintenancePlan.QualityStatus).", 0, "PlanL48", r)
    s.end_group()

    s.w("""SELECT 'verifications' k, COUNT(*) n FROM dbo.Verifications UNION ALL SELECT 'requests', COUNT(*) FROM dbo.Requests
UNION ALL SELECT 'request_units', COUNT(*) FROM dbo.RequestEquipmentUnits UNION ALL SELECT 'maintenances', COUNT(*) FROM dbo.Maintenances
UNION ALL SELECT 'maint_requests', COUNT(*) FROM dbo.MaintenanceRequests UNION ALL SELECT 'participants', COUNT(*) FROM dbo.MaintenanceParticipants
UNION ALL SELECT 'plans', COUNT(*) FROM dbo.MaintenancePlans
UNION ALL SELECT 'actual_cost_x100', CAST(ROUND(SUM(ActualCost) * 100, 0) AS bigint) FROM dbo.Maintenances
UNION ALL SELECT 'source_rows', COUNT(*) FROM dbo.ImportSourceRows WHERE ImportBatchId = @Batch
UNION ALL SELECT 'issues', COUNT(*) FROM dbo.DataQualityIssues WHERE ImportBatchId = @Batch;""")
    path = s.write("parte_b1.sql")
    print(f"[ok] {path}: {len(s.lines)} líneas, source_rows={s.source_row_count}, issues={s.issue_count}")


if __name__ == "__main__":
    main("--dry-run" in sys.argv)
