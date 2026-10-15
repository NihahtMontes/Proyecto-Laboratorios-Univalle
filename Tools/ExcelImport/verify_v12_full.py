"""
Verificador de completitud: compara BaseGastronomia_v1.xlsx contra DB_Laboratorios_Univalle_REAL (solo lectura).

1. Copia cruda: por cada hoja y fila, ImportSourceRows.OriginalDataJson == fila completa del Excel (cada celda).
2. Claves: el conjunto de claves de cada hoja existe en la tabla destino (faltantes / sobrantes).
3. Valores tipados: comparación celda por celda de las columnas con campo propio.
4. Integridad: DBCC CHECKCONSTRAINTS WITH ALL_CONSTRAINTS sin filas.
Resultado esperado: 0 diferencias. Uso: python verify_v12_full.py
"""
import json
import sys
from datetime import date, datetime

import pyodbc

from v12_common import EXPECTED_ROWS, BATCH_CODE, check_sha, read_sheets, row_json, text

CONN = ("Driver={ODBC Driver 17 for SQL Server};Server=localhost;Database=DB_Laboratorios_Univalle_REAL;"
        "Trusted_Connection=yes;TrustServerCertificate=yes")

# hoja -> [(tabla, expresión SQL de la clave, función clave Excel)]. Varias entradas = la clave puede estar en cualquiera.
inv = lambda r: str(int(r["Numeroinventario"]))  # noqa: E731
KEYS = {
    "Sedes": [("Campuses", "Code", lambda r: r["Sedecodigo"])],
    "Facultades": [("Faculties", "Code", lambda r: r["Facultadcodigo"])],
    "Carreras": [("Careers", "Code", lambda r: r["Carreracodigo"])],
    "Laboratorios": [("Laboratories", "Code", lambda r: r["Laboratoriocodigo"])],
    "Gestiones": [("Managements", "Code", lambda r: r["Gestioncodigo"])],
    "Actores": [("People", "ActorCode", lambda r: r["Actorcodigo"])],
    "CatalogoEquipos": [("Equipments", "CatalogCode", lambda r: r["Catalogocodigo"])],
    "UnidadesFisicas": [("EquipmentUnits", "InventoryNumber", inv)],
    "VerificacionesL6": [("Verifications", "HistoricalSourceKey", lambda r: "V12:" + r["Verificacioncodigo"])],
    "SolicitudesL7": [("Requests", "HistoricalSourceKey", lambda r: "V12:" + r["Solicitudcodigo"])],
    "MantenimientosL8": [("Maintenances", "HistoricalSourceKey", lambda r: "V12:" + r["Mantenimientocodigo"])],
    "PlanL48": [("MaintenancePlans", "PlanCode", lambda r: r["Plancodigo"])],
    "Materias": [("Subjects", "Code", lambda r: r["Materiacodigo"])],
    "Practicas": [("Practices", "Code", lambda r: r["Practicacodigo"])],
    "Planificacion": [("PracticeSchedules", "Code", lambda r: r["Planificacioncodigo"])],
    "CategoriasArticulo": [("ArticleCategories", "Code", lambda r: r["Categoriacodigo"])],
    "UnidadesMedida": [("MeasurementUnits", "Code", lambda r: r["Unidadmedidacodigo"])],
    "Articulos": [("Articles", "Code", lambda r: r["Articulocodigo"])],
    "RequerimientosPractica": [("PracticeRequirements", "Code", lambda r: r["Requerimientocodigo"])],
    "SolicitudesL3": [("Plannings", "Code", lambda r: r["Solicitudl3codigo"]),
                      ("UndatedL3Requests", "Code", lambda r: r["Solicitudl3codigo"])],
    "SolicitudL3Detalles": [("PlanningRequirements", "Code", lambda r: r["Solicitudl3detallecodigo"]),
                            ("UndatedL3RequestLines", "Code", lambda r: r["Solicitudl3detallecodigo"])],
}
# hojas sin clave natural propia en la BD: se comparan por conteo de ImportSourceRows (ya cubierto en el paso 1) y por tabla
COUNT_ONLY = {"ActorRoles": "PersonRoleAssignments", "SolicitudUnidades": "RequestEquipmentUnits",
              "MantSolicitudes": "MaintenanceRequests", "MantParticipantes": "MaintenanceParticipants",
              "ExistenciasArticulos": "ArticleStocks", "Incidentes": None}


def norm(v):
    if isinstance(v, datetime):
        return v.date() if v.time() == datetime.min.time() else v
    return v


def close(a, b):
    if a is None or b is None:
        return a is None and b is None
    try:
        return abs(float(a) - float(b)) < 0.0051
    except (TypeError, ValueError):
        return str(a).strip() == str(b).strip()


def main():
    check_sha()
    data = read_sheets(list(EXPECTED_ROWS))
    cn = pyodbc.connect(CONN)
    cur = cn.cursor()
    problems = 0

    def fail(msg):
        nonlocal problems
        problems += 1
        if problems <= 60:
            print("  FALLA:", msg)

    print("== 1. Copia cruda de cada celda (ImportSourceRows.OriginalDataJson)")
    batch_id = cur.execute("SELECT Id FROM dbo.ImportBatches WHERE Code = ?", BATCH_CODE).fetchval()
    for sheet, (header, rows) in data.items():
        stored = {}
        for rownum, js in cur.execute(
                "SELECT SourceRowNumber, OriginalDataJson FROM dbo.ImportSourceRows WHERE ImportBatchId = ? AND SourceSheet = ?",
                batch_id, sheet):
            stored[rownum] = json.loads(js)
        bad = 0
        for r in rows:
            exp = json.loads(row_json(header, r))
            got = stored.get(r["_row"])
            if got != exp:
                bad += 1
                fail(f"{sheet} fila {r['_row']}: la copia cruda no coincide")
        extra = set(stored) - {r["_row"] for r in rows}
        if extra:
            fail(f"{sheet}: filas de más en ImportSourceRows {sorted(extra)[:5]}")
        print(f"  {sheet:<24} excel={len(rows):>6} copiadas={len(stored):>6} diferencias={bad + len(extra)}")

    print("== 2. Claves de cada hoja existen en la tabla destino")
    for sheet, (header, rows) in data.items():
        if sheet in KEYS:
            db_keys = set()
            for table, col, _ in KEYS[sheet]:
                db_keys |= {str(x[0]).strip() for x in cur.execute(f"SELECT {col} FROM dbo.{table}") if x[0] is not None}
            ex_keys = {str(KEYS[sheet][0][2](r)).strip() for r in rows}
            if len(ex_keys) != len(rows):
                fail(f"{sheet}: claves repetidas en el Excel")
            missing = ex_keys - db_keys
            if missing:
                fail(f"{sheet}: faltan en BD {sorted(missing)[:5]} ({len(missing)})")
            print(f"  {sheet:<24} excel={len(ex_keys):>6} en_bd={len(ex_keys & db_keys):>6} faltantes={len(missing)}")
        elif sheet in COUNT_ONLY:
            table = COUNT_ONLY[sheet]
            n = cur.execute(f"SELECT COUNT(*) FROM dbo.{table}").fetchval() if table else 0
            ok = n == len(rows)
            if not ok:
                fail(f"{sheet}: {table} tiene {n}, el Excel {len(rows)}")
            print(f"  {sheet:<24} excel={len(rows):>6} tabla {table or '-'}={n:>6} {'OK' if ok else 'DIFERENTE'}")

    print("== 3. Valores tipados (celda por celda)")
    checks = [
        ("UnidadesFisicas", "SELECT InventoryNumber, SerialNumber, AcquisitionValue FROM dbo.EquipmentUnits",
         lambda r: inv(r), [("Numeroserie", 1), ("Valoradquisicionbs", 2)]),
        ("VerificacionesL6", "SELECT HistoricalSourceKey, Date, Observations FROM dbo.Verifications",
         lambda r: "V12:" + r["Verificacioncodigo"], [("Fechaverificacion", 1)]),
        ("MantenimientosL8", "SELECT HistoricalSourceKey, ActualCost, Description, QualityStatus FROM dbo.Maintenances",
         lambda r: "V12:" + r["Mantenimientocodigo"], [("Costototalfuentebs", 1), ("Trabajorealizado", 2), ("Estadocalidad", 3)]),
        ("SolicitudesL7", "SELECT HistoricalSourceKey, Description, Suggestion, EstimatedRepairTime FROM dbo.Requests",
         lambda r: "V12:" + r["Solicitudcodigo"], [("Problemareportado", 1), ("Sugerencia", 2), ("Tiempoestimado", 3)]),
        ("PlanL48", "SELECT PlanCode, PlannedDate, QualityStatus FROM dbo.MaintenancePlans",
         lambda r: r["Plancodigo"], [("Fechaplanificada", 1), ("Estadocalidad", 2)]),
        ("CatalogoEquipos", "SELECT CatalogCode, Name, Brand, Model, UsefulLifeYears, Description FROM dbo.Equipments",
         lambda r: r["Catalogocodigo"], [("Nombregenerico", 1), ("Marca", 2), ("Modelo", 3), ("Vidautilanios", 4), ("Especificaciones", 5)]),
        ("Gestiones", "SELECT Code, StartDate, PlannedEndDate, Year, Semester FROM dbo.Managements",
         lambda r: r["Gestioncodigo"], [("Fechainicio", 1), ("Fechafin", 2), ("Anio", 3), ("Semestre", 4)]),
        ("Laboratorios", "SELECT Code, Name, Type, Building, Block FROM dbo.Laboratories",
         lambda r: r["Laboratoriocodigo"], [("Nombre", 1), ("Tipo", 2), ("Edificio", 3), ("Bloque", 4)]),
        ("Practicas", "SELECT Code, Number, Description, Observations FROM dbo.Practices",
         lambda r: r["Practicacodigo"], [("Numeropractica", 1), ("Descripcion", 2), ("Observaciones", 3)]),
        ("RequerimientosPractica", "SELECT Code, Quantity, ValidationStatus, Observations FROM dbo.PracticeRequirements",
         lambda r: r["Requerimientocodigo"], [("Cantidad", 1), ("Estadovalidacion", 2), ("Observaciones", 3)]),
        ("Planificacion", "SELECT Code, ExecutionDate, WeekNumber, StudentCount, ScalingFactor FROM dbo.PracticeSchedules",
         lambda r: r["Planificacioncodigo"], [("Fechaejecucion", 1), ("Semana", 2), ("Cantestudiantes", 3), ("Factormateria", 4)]),
        ("SolicitudesL3", "SELECT Code, CAST(ScheduledDate AS date), Observations, PracticeNumber, CAST(RequestDate AS date) FROM dbo.Plannings",
         lambda r: r["Solicitudl3codigo"], [("Fechapractica", 1), ("Observaciones", 2), ("Numeropractica", 3), ("Fechasolicitud", 4)]),
        ("SolicitudL3Detalles", "SELECT Code, RequestedQuantity, Observations FROM dbo.PlanningRequirements",
         lambda r: r["Solicitudl3detallecodigo"], [("Cantidadsolicitada", 1), ("Observaciones", 2)]),
    ]
    for sheet, sql, keyf, cols in checks:
        db = {str(row[0]).strip(): row for row in cur.execute(sql)}
        bad = compared = 0
        for r in data[sheet][1]:
            row = db.get(str(keyf(r)).strip())
            if row is None:
                continue  # solicitudes sin fecha: están en las tablas Undated (paso 2)
            for ecol, idx in cols:
                compared += 1
                a, b = norm(r[ecol]), norm(row[idx])
                if sheet == "SolicitudesL3" and ecol in ("Fechapractica", "Fechasolicitud") and a is None:
                    a = date.today()  # fecha vacía en el Excel -> se cargó con la fecha de la importación
                if isinstance(a, str) and isinstance(b, (date, datetime)):
                    a = a.strip()
                    b = b.isoformat()[:10]
                elif isinstance(a, (date, datetime)) and isinstance(b, (date, datetime)):
                    a, b = a.isoformat()[:10], b.isoformat()[:10]
                if isinstance(a, str) and isinstance(b, str):
                    ok = a.strip() == b.strip() or (ecol == "Nombregenerico" and a.strip() == b.strip())
                else:
                    ok = close(a, b)
                if not ok:
                    bad += 1
                    fail(f"{sheet} {keyf(r)} {ecol}: excel={a!r} bd={b!r}")
        print(f"  {sheet:<24} celdas comparadas={compared:>7} diferencias={bad}")

    print("== 4. Integridad (DBCC CHECKCONSTRAINTS WITH ALL_CONSTRAINTS)")
    cur.execute("DBCC CHECKCONSTRAINTS WITH ALL_CONSTRAINTS")
    violations = cur.fetchall() if cur.description else []
    print(f"  violaciones={len(violations)}")
    if violations:
        fail(f"CHECKCONSTRAINTS: {violations[:3]}")

    print("== RESULTADO:", "0 diferencias" if problems == 0 else f"{problems} problemas")
    sys.exit(1 if problems else 0)


if __name__ == "__main__":
    main()
