"""
QA de invariantes de integridad (SOLO LECTURA: SELECT + DBCC CHECKCONSTRAINTS)
contra la copia QA E2E. Se niega a correr si e2e_lib.DB_NAME <> DB_Laboratorios_Univalle_QA_E2E.

Salida por consola y además en outputs/qa-usuario/qa-2026-10-18/sql_invariantes.txt
(crea la carpeta si no existe). Cada invariante es una Result con su ID (IV-xx)
y, si falla, en detail los primeros 10 ids infractores.

Enums verificados en Models/Enums/*.cs:
  ManagementType: Preventive=0 Corrective=1 PorConfirmar=9
  ManagementStatus: Active=0 Inactive=1 Completed=2 Deleted=99
  ManagementPlanStatus: Pending=0 InProgress=1 Completed=2 Overdue=3
  WizardPhase: Verification=1 TechnicalRequest=2 Maintenance=3 Exit=4 Kardex=5 Disbursement=6
  EquipmentStatus: Operational=0 ... OnLoan=10 ... Deleted=99
  LoanStatus: Active=0 Returned=1 Overdue=2 Cancelled=99
  RequestType: Technical=1 Purchasing=2 Calibration=3
"""
import contextlib
import io
import os
import sys

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))

import pyodbc

import e2e_lib
from e2e_lib import Result, db_rows, db_scalar, run_all

QA_DB = "DB_Laboratorios_Univalle_QA_E2E"
OUT_DIR = os.path.join("outputs", "qa-usuario", "qa-2026-10-18")
OUT_FILE = os.path.join(OUT_DIR, "sql_invariantes.txt")
REPO_ROOT = os.path.abspath(os.path.join(os.path.dirname(os.path.abspath(__file__)), "..", ".."))

# Valores de enums (Models/Enums)
CORRECTIVE, PREVENTIVE = 1, 0
M_ACTIVE, M_DELETED = 0, 99
PLAN_COMPLETED = 2
OPERATIONAL, ON_LOAN = 0, 10
LOAN_ACTIVE, LOAN_OVERDUE = 0, 2
REQ_TECHNICAL = 1
CORTE_HISTORICO = "2026-10-18"  # fecha de corte del histórico importado


def _preview(rows):
    """Primeros 10 ids (primera columna) para el detail de una Result fallida."""
    return "ids: " + ",".join(str(r[0]) for r in rows[:10]) if rows else ""


def _check(name, sql):
    """Ejecuta un SELECT de infractores y arma la Result (esperado=0)."""
    rows = db_rows(sql)
    return Result(name, 0, len(rows), _preview(rows))


# ---------------------------------------------------------------- IV-01
def iv01_fase_vs_vinculos():
    base = "FROM ManagementPlans WHERE EquipmentUnitId IS NOT NULL"
    return [
        _check("IV-01.1 fase>=3 tiene RequestId", f"SELECT Id {base} AND CurrentPhase>=3 AND RequestId IS NULL"),
        _check("IV-01.2 fase>=4 tiene MaintenanceId", f"SELECT Id {base} AND CurrentPhase>=4 AND MaintenanceId IS NULL"),
        _check("IV-01.3 fase>=5 tiene DepartureId", f"SELECT Id {base} AND CurrentPhase>=5 AND DepartureId IS NULL"),
        _check("IV-01.4 plan correctivo con VerificationId", f"SELECT p.Id FROM ManagementPlans p "
                f"JOIN Managements m ON m.Id=p.ManagementId AND m.Type={CORRECTIVE} "
                f"WHERE p.EquipmentUnitId IS NOT NULL AND p.VerificationId IS NOT NULL"),
    ]


# ---------------------------------------------------------------- IV-02
def iv02_unidad_vs_salida():
    return [
        _check("IV-02.1 unidad OnLoan con salida Active/Overdue", f"SELECT Id FROM EquipmentUnits "
                f"WHERE CurrentStatus={ON_LOAN} AND NOT EXISTS (SELECT 1 FROM Departures d "
                f"WHERE d.EquipmentUnitId=EquipmentUnits.Id AND d.Status IN ({LOAN_ACTIVE},{LOAN_OVERDUE}))"),
        _check("IV-02.2 unidad Operational sin salida Active/Overdue de plan abierto", f"SELECT DISTINCT u.Id "
                f"FROM EquipmentUnits u JOIN Departures d ON d.EquipmentUnitId=u.Id AND d.Status IN ({LOAN_ACTIVE},{LOAN_OVERDUE}) "
                f"JOIN ManagementPlans p ON p.DepartureId=d.Id AND p.PlanStatus<>{PLAN_COMPLETED} "
                f"WHERE u.CurrentStatus={OPERATIONAL}"),
    ]


# ---------------------------------------------------------------- IV-03
def iv03_historial_estados():
    return [
        _check("IV-03.1 max un registro abierto (EndDate NULL) por unidad", "SELECT h.EquipmentUnitId "
                "FROM EquipmentStateHistories h WHERE h.EquipmentUnitId IS NOT NULL GROUP BY h.EquipmentUnitId "
                "HAVING SUM(CASE WHEN h.EndDate IS NULL THEN 1 ELSE 0 END) > 1"),
        _check("IV-03.2 ningun EndDate < StartDate", "SELECT Id FROM EquipmentStateHistories "
                "WHERE EndDate IS NOT NULL AND EndDate < StartDate"),
    ]


# ---------------------------------------------------------------- IV-04
def iv04_costos():
    return [
        _check("IV-04.1 CostDetail exactamente un padre (RequestId/MaintenanceId)",
               "SELECT Id FROM CostDetails WHERE (RequestId IS NULL AND MaintenanceId IS NULL) "
               "OR (RequestId IS NOT NULL AND MaintenanceId IS NOT NULL)"),
        _check("IV-04.2 Maintenance.ActualCost = SUM(Quantity*UnitPrice) no anulados",
               "SELECT m.Id FROM Maintenances m CROSS APPLY (SELECT SUM(cd.Quantity*cd.UnitPrice) AS s "
               "FROM CostDetails cd WHERE cd.MaintenanceId=m.Id AND cd.IsVoided=0) c "
               "WHERE c.s IS NOT NULL AND (m.ActualCost IS NULL OR ABS(m.ActualCost-c.s)>0.005)"),
    ]


# ---------------------------------------------------------------- IV-05
def iv05_plan_completado():
    base = f"FROM ManagementPlans WHERE PlanStatus={PLAN_COMPLETED} AND EquipmentUnitId IS NOT NULL"
    return [
        _check("IV-05.1 plan Completado con todos los enlaces",
               f"SELECT Id {base} AND (RequestId IS NULL OR MaintenanceId IS NULL OR DepartureId IS NULL "
               "OR KardexHistoryId IS NULL OR AcquisitionRequestId IS NULL)"),
        _check("IV-05.2 plan Completado preventivo con VerificationId",
               f"SELECT p.Id FROM ManagementPlans p JOIN Managements m ON m.Id=p.ManagementId AND m.Type={PREVENTIVE} "
               f"WHERE p.PlanStatus={PLAN_COMPLETED} AND p.EquipmentUnitId IS NOT NULL AND p.VerificationId IS NULL"),
    ]


# ---------------------------------------------------------------- IV-06
def iv06_gestiones():
    return [
        _check("IV-06.1 max una gestion Activa por tipo", f"SELECT Id FROM Managements WHERE Status={M_ACTIVE} "
                "AND Type IN (SELECT Type FROM Managements WHERE Status=0 GROUP BY Type HAVING COUNT(*)>1) ORDER BY Id"),
        _check("IV-06.2 codigos de gestion unicos entre no eliminadas", "SELECT Id FROM Managements WHERE Status<>99 "
                "AND Code IN (SELECT Code FROM Managements WHERE Status<>99 GROUP BY Code HAVING COUNT(*)>1) ORDER BY Id"),
        _check("IV-06.3 sin correctiva Semester=0 creada desde el corte",
               f"SELECT Id FROM Managements WHERE Type={CORRECTIVE} AND Semester=0 "
               f"AND CreatedDate>='{CORTE_HISTORICO}'"),
    ]


# ---------------------------------------------------------------- IV-07
def iv07_huerfanos():
    return [
        # Status 99 = Cancelada: una request cancelada no avanza a plan por diseño.
        _check("IV-07.1 request tecnica nueva (no cancelada) con ManagementPlan que la referencie",
               f"SELECT Id FROM Requests WHERE Type={REQ_TECHNICAL} AND Status<>99 "
               f"AND CreatedDate>='{CORTE_HISTORICO}' "
               "AND NOT EXISTS (SELECT 1 FROM ManagementPlans p WHERE p.RequestId=Requests.Id)"),
        _check("IV-07.2 ningun Maintenance sin EquipmentUnitId",
               "SELECT Id FROM Maintenances WHERE EquipmentUnitId IS NULL"),
    ]


# ---------------------------------------------------------------- IV-08
def iv08_dbcc():
    """DBCC CHECKCONSTRAINTS WITH ALL_CONSTRAINTS no debe devolver filas.

    pyodbc puede lanzar ProgrammingError cuando el comando no produce result set
    (caso tipico: cero violaciones); se interpreta como 0 filas y se deja constancia.
    """
    name = "IV-08 DBCC CHECKCONSTRAINTS sin violaciones"
    try:
        rows = db_rows("DBCC CHECKCONSTRAINTS WITH ALL_CONSTRAINTS")
        return Result(name, 0, len(rows), "; ".join(str(tuple(r)) for r in rows[:10]))
    except pyodbc.ProgrammingError as exc:
        if "No results" in str(exc) or "not a query" in str(exc).lower() or "empty" in str(exc).lower():
            return Result(name, 0, 0, "driver no devolvio filas para DBCC (interpretado como 0 violaciones)")
        return Result(name, "sin excepcion", f"ProgrammingError: {exc}")


# ---------------------------------------------------------------- IV-09
def iv09_notificaciones():
    return [
        _check("IV-09 sin duplicados exactos (titulo/mensaje/usuario/gestion/mismo segundo)",
               "SELECT n.Id FROM Notifications n WHERE EXISTS (SELECT 1 FROM Notifications o WHERE o.Id<>n.Id "
               "AND ISNULL(o.UserId,-1)=ISNULL(n.UserId,-1) AND o.Title=n.Title AND o.Message=n.Message "
               "AND ISNULL(o.ManagementId,-1)=ISNULL(n.ManagementId,-1) AND o.CreatedAt=n.CreatedAt) ORDER BY n.Id"),
    ]


# ---------------------------------------------------------------- IV-10
def iv10_planes_por_unidad():
    return [
        _check("IV-10.1 max un plan ABIERTO por (ManagementId, EquipmentUnitId)",
               "SELECT p.Id FROM ManagementPlans p JOIN (SELECT ManagementId, EquipmentUnitId "
               "FROM ManagementPlans WHERE EquipmentUnitId IS NOT NULL AND PlanStatus<>2 "
               "GROUP BY ManagementId, EquipmentUnitId HAVING COUNT(*)>1) v "
               "ON p.ManagementId=v.ManagementId AND p.EquipmentUnitId=v.EquipmentUnitId "
               "WHERE p.PlanStatus<>2 ORDER BY p.Id"),
        _check("IV-10.2 preventiva: max UN plan (cualquier estado) por (ManagementId, EquipmentUnitId)",
               f"SELECT p.Id FROM ManagementPlans p JOIN Managements m ON m.Id=p.ManagementId AND m.Type={PREVENTIVE} "
               "JOIN (SELECT p2.ManagementId, p2.EquipmentUnitId FROM ManagementPlans p2 "
               "WHERE p2.EquipmentUnitId IS NOT NULL GROUP BY p2.ManagementId, p2.EquipmentUnitId HAVING COUNT(*)>1) v "
               "ON p.ManagementId=v.ManagementId AND p.EquipmentUnitId=v.EquipmentUnitId ORDER BY p.Id"),
    ]


CHECKS = [iv01_fase_vs_vinculos, iv02_unidad_vs_salida, iv03_historial_estados, iv04_costos,
          iv05_plan_completado, iv06_gestiones, iv07_huerfanos, iv08_dbcc, iv09_notificaciones,
          iv10_planes_por_unidad]


def main():
    if e2e_lib.DB_NAME != QA_DB:
        sys.exit(f"ABORTADO: E2E_DB={e2e_lib.DB_NAME!r}; este script solo corre contra {QA_DB}")
    actual = db_scalar("SELECT DB_NAME()")
    if actual != QA_DB:
        sys.exit(f"ABORTADO: la conexion apunta a {actual!r}; se requiere {QA_DB}")

    header = (f"sql_invariantes.py - BD: {actual} - invariantes de integridad (solo lectura)\n")
    buf = io.StringIO()
    with contextlib.redirect_stdout(buf):
        fails = run_all(CHECKS)
    report = header + buf.getvalue()

    out_dir = os.path.join(REPO_ROOT, OUT_DIR)
    os.makedirs(out_dir, exist_ok=True)
    with open(os.path.join(REPO_ROOT, OUT_FILE), "w", encoding="utf-8", newline="\n") as f:
        f.write(report)
    print(report, end="")
    return 1 if fails else 0


if __name__ == "__main__":
    sys.exit(main())
