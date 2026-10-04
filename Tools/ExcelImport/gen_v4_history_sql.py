"""
V4 · Bloque 2 · histórico (MAPA_EXCEL_V4_A_BD.md §3.10–3.17 y §4).

Modo conciliación (por defecto): lee DEMO_QA (solo SELECT, vía sqlcmd) y la V4 y genera
conciliacion_v4.csv + un resumen de conteos. NO escribe en la base.
El modo carga se habilita solo tras la aprobación del usuario sobre el CSV.
"""
import csv
import datetime as dt
import hashlib
import io
import os
import subprocess
import sys
from collections import defaultdict

import openpyxl

XLSX = r"C:\Users\monte\Downloads\BaseEstructuraFinal_V4.xlsx"
EXPECTED_SHA = "8ebb238c24a7ce94a71ff0506675fdc216db8d2831b7c24d27f7aab5ff1eb9d2"
DB = "DB_Laboratorios_Univalle_DEMO_QA"
OUT_DIR = os.path.join(os.environ.get("TEMP", "."), "mm")
CSV_OUT = os.path.join(OUT_DIR, "conciliacion_v4.csv")

L6_WINDOW_DAYS = 60
L8_WINDOW_DAYS = 15

EXPECTED_HEADERS = {
    "VerificacionesL6": ("VerificacionCodigo", "NumeroInventario", "GestionCodigo", "FechaVerificacion", "CondicionFisica",
                         "EstadoEquipo", "ProblemasObservados", "Observaciones", "ResponsableActorCodigo"),
    "SolicitudesL7": ("SolicitudCodigo", "GestionCodigo", "FechaSolicitud", "Prioridad", "ProblemaReportado", "Sugerencia",
                      "TiempoEstimado", "SolicitanteActorCodigo", "TipoSolicitud", "Estado"),
    "SolicitudUnidades": ("SolicitudUnidadCodigo", "SolicitudCodigo", "NumeroInventario", "Observacion"),
    "MantenimientosL8": ("MantenimientoCodigo", "NumeroInventario", "GestionCodigo", "FechaProgramada", "FechaInicio", "FechaFin",
                         "TipoMantenimiento", "TipoServicio", "TrabajoRealizado", "Estado", "Satisfaccion", "Recomendaciones",
                         "ProximoMantenimiento", "CostoTotalFuenteBs", "CostoDetalleCalculadoBs", "DiferenciaCostoBs", "EstadoCalidad"),
    "MantSolicitudes": ("MantenimientoSolicitudCodigo", "MantenimientoCodigo", "SolicitudCodigo", "Observacion"),
    "MantParticipantes": ("ParticipanteCodigo", "MantenimientoCodigo", "ActorCodigo", "Rol", "EsPrincipal", "Estado"),
    "PlanL48": ("PlanCodigo", "GestionCodigo", "NumeroInventario", "TipoMantenimiento", "TipoServicio", "ResponsableActorCodigo",
                "FechaPlanificada", "Estado", "EstadoCalidad"),
}


def load_excel():
    sha = hashlib.sha256(open(XLSX, "rb").read()).hexdigest()
    if sha != EXPECTED_SHA:
        sys.exit(f"Hash distinto: {sha}")
    wb = openpyxl.load_workbook(XLSX, read_only=True, data_only=True)
    data = {}
    for name, expected in EXPECTED_HEADERS.items():
        it = wb[name].iter_rows(values_only=True)
        header = tuple(next(it))
        if header[:len(expected)] != expected:
            sys.exit(f"La hoja {name} cambió de encabezados: {header}")
        rows = []
        for i, r in enumerate(it, start=2):
            if any(v is not None for v in r):
                d = dict(zip(header, r))
                d["_row"] = i
                rows.append(d)
        data[name] = rows
    return sha, data


def query(sql):
    """SELECT de solo lectura contra DEMO_QA; devuelve lista de listas (separador |, sin encabezado)."""
    res = subprocess.run(
        ["sqlcmd", "-S", "localhost", "-E", "-d", DB, "-W", "-h", "-1", "-s", "|", "-f", "65001",
         "-Q", "SET NOCOUNT ON; " + sql],
        capture_output=True, check=True)
    out = res.stdout.decode("utf-8", errors="replace")
    return [line.split("|") for line in out.splitlines() if line.strip()]


def to_date(v):
    if v is None or v == "" or v == "NULL":
        return None
    if isinstance(v, dt.datetime):
        return v.date()
    if isinstance(v, dt.date):
        return v
    return dt.date.fromisoformat(str(v).strip()[:10])


def inv(v):
    if v is None:
        return None
    s = str(v).strip()
    return s[:-2] if s.endswith(".0") else s


def is_inferred(key):
    return (key or "").startswith("INF_")


def nearest_one_to_one(excel_rows, db_rows, window):
    """Empareja por inventario la fecha más cercana dentro de ±window días, uno a uno (menor distancia primero)."""
    db_by_inv = defaultdict(list)
    for d in db_rows:
        db_by_inv[d["inv"]].append(d)
    candidates = []
    for e in excel_rows:
        for d in db_by_inv.get(e["inv"], []):
            if e["date"] is None or d["date"] is None:
                continue
            gap = abs((e["date"] - d["date"]).days)
            if gap <= window:
                candidates.append((gap, e["key"], d["id"], e, d))
    candidates.sort(key=lambda c: (c[0], c[1], c[2]))
    used_e, used_d, pairs = set(), set(), []
    for gap, ek, did, e, d in candidates:
        if ek in used_e or did in used_d:
            continue
        used_e.add(ek)
        used_d.add(did)
        pairs.append((e, d, gap))
    return pairs, used_e, used_d


def processed_rows(batch_code):
    """Lo ya cargado por una ejecución anterior (ImportSourceRows del lote): claves Excel hechas e Ids de la BD consumidos."""
    done, consumed = defaultdict(set), defaultdict(set)
    for sheet, key, entity, target in query(
            "SELECT r.SourceSheet, REPLACE(r.SourceRowKey, '|', '#'), r.TargetEntityName, ISNULL(r.TargetEntityKey,'') FROM dbo.ImportSourceRows r "
            f"JOIN dbo.ImportBatches b ON b.Id = r.ImportBatchId WHERE b.Code = N'{batch_code}'"):
        if "." not in entity:
            done[sheet].add(key)
        if target.isdigit():
            consumed[entity.split(".")[0]].add(int(target))
    return done, consumed


def conciliate():
    sha, x = load_excel()
    batch_code = "XLSX-V4-HIST-" + sha[:12].upper()
    done, consumed = processed_rows(batch_code)
    rows = []

    def add(sheet, result, action, e=None, d=None, gap=None, units=None, note=""):
        rows.append({
            "Hoja": sheet,
            "Resultado": result,
            "Accion": action,
            "ClaveExcel": e["key"] if e else "",
            "Inventario": units if units is not None else ((e or d or {}).get("inv") or ""),
            "GestionExcel": e["mgmt"] if e else "",
            "FechaExcel": e["date"].isoformat() if e and e.get("date") else "",
            "IdBD": d["id"] if d else "",
            "ClaveBD": d["key"] if d else "",
            "GestionBD": d["mgmt"] if d else "",
            "FechaBD": d["date"].isoformat() if d and d.get("date") else "",
            "DiasDiferencia": "" if gap is None else gap,
            "Inferido": "Si" if d and is_inferred(d["key"]) else ("No" if d else ""),
            "Nota": note,
        })

    # ---------------- L-6 ----------------
    xv = [{"key": r["VerificacionCodigo"], "inv": inv(r["NumeroInventario"]), "mgmt": r["GestionCodigo"],
           "date": to_date(r["FechaVerificacion"])} for r in x["VerificacionesL6"]]
    dv = [{"id": int(a), "key": b, "date": to_date(c), "inv": e, "mgmt": f}
          for a, b, c, e, f in query(
              "SELECT v.Id, ISNULL(v.HistoricalSourceKey,''), CONVERT(char(10), v.Date, 23), eu.InventoryNumber, m.Code "
              "FROM dbo.Verifications v JOIN dbo.EquipmentUnits eu ON eu.Id = v.EquipmentUnitId "
              "JOIN dbo.Managements m ON m.Id = v.ManagementId")]
    dv = [d for d in dv if d["id"] not in consumed["Verification"]]
    already = {d["key"] for d in dv} | {"V4:" + k for k in done["VerificacionesL6"]}
    pairs, used_e, used_d = nearest_one_to_one([e for e in xv if "V4:" + e["key"] not in already], dv, L6_WINDOW_DAYS)
    for e, d, gap in pairs:
        note = "Gestion distinta (MANAGEMENT_SHIFTED_BY_RULE)" if e["mgmt"] != d["mgmt"] else ""
        add("L-6", "PAR", "ANULAR_INFERIDO_Y_CREAR" if is_inferred(d["key"]) else "ACTUALIZAR_BD", e, d, gap, note=note)
    for e in xv:
        if "V4:" + e["key"] in already:
            add("L-6", "YA_CARGADO", "NADA", e)
        elif e["key"] not in used_e:
            add("L-6", "SOLO_EXCEL", "CREAR", e)
    for d in dv:
        if d["id"] not in used_d and not d["key"].startswith("V4:"):
            add("L-6", "SOLO_BD", "CONSERVAR_CON_INCIDENCIA", d=d,
                note="LEGACY_INFERRED_UNMATCHED" if is_inferred(d["key"]) else "LEGACY_ONLY_IN_DB")

    # ---------------- L-7 ----------------
    units_by_req = defaultdict(list)
    for r in x["SolicitudUnidades"]:
        units_by_req[r["SolicitudCodigo"]].append(inv(r["NumeroInventario"]))
    xr = [{"key": r["SolicitudCodigo"], "mgmt": r["GestionCodigo"], "date": to_date(r["FechaSolicitud"]),
           "set": frozenset(units_by_req.get(r["SolicitudCodigo"], []))} for r in x["SolicitudesL7"]]
    dr_rows = query(
        "SELECT r.Id, ISNULL(r.HistoricalSourceKey,''), ISNULL(m.Code,''), ISNULL(eu.InventoryNumber,''), r.Status "
        "FROM dbo.Requests r LEFT JOIN dbo.Managements m ON m.Id = r.ManagementId "
        "LEFT JOIN dbo.EquipmentUnits eu ON eu.Id = r.EquipmentUnitId")
    groups = defaultdict(list)  # REQ_R{fila}_{inv} -> grupo REQ_R{fila}
    singles = []
    for a, key, mg, iv, st in dr_rows:
        if int(a) in consumed["Request"]:
            continue
        d = {"id": int(a), "key": key, "mgmt": mg, "inv": iv, "date": None, "status": st}
        if key.startswith("REQ_R") and key.count("_") >= 2:
            groups[key.rsplit("_", 1)[0]].append(d)
        elif not key.startswith("V4:"):
            singles.append(d)
    xr_by_set = defaultdict(list)
    for e in xr:
        if e["set"]:
            xr_by_set[e["set"]].append(e)
    def year_after(code):
        # La V4 registra L-7 un año después de la gestión de la BD (2024-2 -> 2025-2, 2025-1 -> 2026-1).
        try:
            y, s = code.split("-")
            return f"{int(y) + 1}-{s}"
        except ValueError:
            return None

    group_keys = sorted(groups, key=lambda k: int(k[5:]) if k[5:].isdigit() else 0)
    picks = {}
    used_x = set(done["SolicitudesL7"])
    # Pasada 1: conjunto idéntico y gestión con el corrimiento de un año. Pasada 2: conjunto idéntico, cualquier gestión.
    for strict in (True, False):
        for gk in group_keys:
            if gk in picks:
                continue
            members = groups[gk]
            gset = frozenset(d["inv"] for d in members)
            target = year_after(min(members, key=lambda d: d["id"])["mgmt"])
            cands = sorted((e for e in xr_by_set.get(gset, []) if e["key"] not in used_x), key=lambda e: e["key"])
            if strict:
                cands = [e for e in cands if e["mgmt"] == target]
            if cands:
                picks[gk] = (cands[0], len(xr_by_set.get(gset, [])), strict)
                used_x.add(cands[0]["key"])
    for gk in group_keys:
        members = sorted(groups[gk], key=lambda d: d["id"])
        gset = frozenset(d["inv"] for d in members)
        units = ";".join(sorted(gset))
        if gk in picks:
            pick, n_same, strict = picks[gk]
            notes = []
            if n_same > 1:
                notes.append(f"{n_same} solicitudes del Excel con el mismo conjunto")
            if pick["mgmt"] != members[0]["mgmt"]:
                notes.append("Gestion distinta (MANAGEMENT_SHIFTED_BY_RULE)")
            if not strict:
                notes.append("Corrimiento distinto de un año: revisar")
            add("L-7", "PAR", "ACTUALIZAR_CANONICA", pick, members[0], units=units, note="; ".join(notes))
            for extra in members[1:]:
                add("L-7", "PAR_FILA_PARTIDA", "CANCELAR_LEGACY_SPLIT_MERGED", pick, extra, units=extra["inv"])
        else:
            for m_ in members:
                add("L-7", "GRUPO_SIN_PAR", "REVISAR_MANUAL", d=m_, units=units, note=f"Grupo {gk} ({len(members)} filas)")
    for d in singles:
        add("L-7", "SOLO_BD", "CONSERVAR_CON_INCIDENCIA", d=d,
            note="LEGACY_INFERRED_UNMATCHED" if is_inferred(d["key"]) else "LEGACY_ONLY_IN_DB")
    for e in xr:
        if e["key"] in done["SolicitudesL7"]:
            add("L-7", "YA_CARGADO", "NADA", e, units=";".join(sorted(e["set"])))
        elif e["key"] not in used_x:
            add("L-7", "SOLO_EXCEL", "CREAR" if not e["set"] else "CREAR_REVISAR", e,
                units=";".join(sorted(e["set"])), note="Sin unidades" if not e["set"] else "")

    # ---------------- L-8 ----------------
    xm = [{"key": r["MantenimientoCodigo"], "inv": inv(r["NumeroInventario"]), "mgmt": r["GestionCodigo"],
           "date": to_date(r["FechaFin"]), "cost": r["CostoTotalFuenteBs"]} for r in x["MantenimientosL8"]]
    dm = [{"id": int(a), "key": b, "date": to_date(c), "inv": e, "mgmt": f}
          for a, b, c, e, f in query(
              "SELECT mt.Id, ISNULL(mt.HistoricalSourceKey,''), CONVERT(char(10), mt.EndDate, 23), ISNULL(eu.InventoryNumber,''), "
              "ISNULL(m.Code,'') FROM dbo.Maintenances mt LEFT JOIN dbo.EquipmentUnits eu ON eu.Id = mt.EquipmentUnitId "
              "LEFT JOIN dbo.Managements m ON m.Id = mt.ManagementId")]
    dm = [d for d in dm if d["id"] not in consumed["Maintenance"]]
    already = {d["key"] for d in dm} | {"V4:" + k for k in done["MantenimientosL8"]}
    pairs, used_e, used_d = nearest_one_to_one([e for e in xm if "V4:" + e["key"] not in already], dm, L8_WINDOW_DAYS)
    for e, d, gap in pairs:
        add("L-8", "PAR", "ANULAR_INFERIDO_Y_CREAR" if is_inferred(d["key"]) else "ACTUALIZAR_BD", e, d, gap)
    for e in xm:
        if "V4:" + e["key"] in already:
            add("L-8", "YA_CARGADO", "NADA", e)
        elif e["key"] not in used_e:
            add("L-8", "SOLO_EXCEL", "CREAR", e)
    for d in dm:
        if d["id"] not in used_d and not d["key"].startswith("V4:"):
            add("L-8", "SOLO_BD", "CONSERVAR_CON_INCIDENCIA", d=d,
                note="LEGACY_INFERRED_UNMATCHED" if is_inferred(d["key"]) else "LEGACY_ONLY_IN_DB")

    # ---------------- L-48 ----------------
    existing_plans = {r[0] for r in query("SELECT ISNULL(PlanCode,'') FROM dbo.MaintenancePlans")}
    for r in x["PlanL48"]:
        e = {"key": r["PlanCodigo"], "inv": inv(r["NumeroInventario"]), "mgmt": r["GestionCodigo"],
             "date": to_date(r["FechaPlanificada"])}
        add("L-48", "YA_CARGADO" if e["key"] in existing_plans else "SOLO_EXCEL",
            "NADA" if e["key"] in existing_plans else "CREAR", e)

    os.makedirs(OUT_DIR, exist_ok=True)
    with io.open(CSV_OUT, "w", encoding="utf-8-sig", newline="") as f:
        wr = csv.DictWriter(f, fieldnames=list(rows[0].keys()), delimiter=";")
        wr.writeheader()
        wr.writerows(rows)

    summary = defaultdict(int)
    for r in rows:
        summary[(r["Hoja"], r["Resultado"], r["Inferido"])] += 1
    groups_total = len(groups)
    groups_matched = sum(1 for r in rows if r["Hoja"] == "L-7" and r["Resultado"] == "PAR")
    cost = sum(float(e["cost"] or 0) for e in xm)
    print(CSV_OUT)
    for k in sorted(summary):
        print(" | ".join(k), summary[k])
    print(f"L-7 grupos REQ_R en BD: {groups_total}, con par exacto: {groups_matched}")
    print(f"L-8 suma CostoTotalFuenteBs: {cost:,.2f}")
    print(f"Excel: L-6 {len(xv)}, L-7 {len(xr)} ({sum(len(v) for v in units_by_req.values())} vinculos), "
          f"L-8 {len(xm)}, L-48 {len(x['PlanL48'])}")
    return sha, batch_code, x, rows


# =====================================================================================
# Modo carga (aprobado por el usuario el 2026-10-04 sobre conciliacion_v4.csv)
# =====================================================================================
SQL_OUT = os.path.join(OUT_DIR, "import_v4_history.sql")
PENDING = {"por confirmar", "sin especificar"}
COND_MAP = {"excelente": 5, "bueno": 4, "regular": 3, "malo": 2, "baja del equipo": 1, "por confirmar": 9}
OBSERVED_MAP = {"operativo": 0, "no operativo": 2, "por confirmar": 98}
MAINT_TYPE_MAP = {"preventivo": 1, "correctivo": 2, "por confirmar": 9}
SERVICE_MAP = {"interno": 0, "externo": 1, "por confirmar": 9}
INFERRED_COST = "(cd.HistoricalSourceKey LIKE N'INF%' OR cd.Description LIKE N'%inferido%' OR cd.Concept LIKE N'%inferido%')"
TX_SIZE = 100


def q(v):
    if v is None or (isinstance(v, str) and v.strip() == ""):
        return "NULL"
    if isinstance(v, bool):
        return "1" if v else "0"
    if isinstance(v, (int, float)):
        return str(v)
    if isinstance(v, (dt.date, dt.datetime)):
        return "'" + v.strftime("%Y-%m-%d") + "'"
    return "N'" + str(v).strip().replace("'", "''") + "'"


def low(v):
    return str(v or "").strip().lower()


def jq(row):
    clean = {k: (v.isoformat() if isinstance(v, (dt.date, dt.datetime)) else v) for k, v in row.items() if not k.startswith("_")}
    return q(__import__("json").dumps(clean, ensure_ascii=False, default=str))


def load():
    sha, batch_code, x, rows = conciliate()
    units = {r[0]: (int(r[1]), int(r[2]) if r[2] else None)
             for r in query("SELECT InventoryNumber, Id, ISNULL(CAST(LaboratoryId AS varchar(12)),'') FROM dbo.EquipmentUnits")}
    mgmts = {r[0]: int(r[1]) for r in query("SELECT Code, Id FROM dbo.Managements")}
    people = {r[0]: int(r[1]) for r in query("SELECT ActorCode, Id FROM dbo.People WHERE ActorCode IS NOT NULL")}
    entities = {int(r[0]) for r in query("SELECT Id FROM dbo.Externs WHERE IsEntity = 1")}

    sql = []
    w = sql.append
    issues = []

    def issue(code, entity, key, field, original, normalized, desc, severity, sheet, row):
        issues.append(f"INSERT #Issue VALUES({q(code)}, {q(entity)}, {q(key)}, {q(field)}, {q(original)}, {q(normalized)}, "
                      f"{q(desc)}, {severity}, {q(sheet)}, {row if row else 'NULL'});")

    def unit(inv_, entity, key, sheet, row):
        u = units.get(inv(inv_))
        if not u:
            issue("UNRESOLVED_REFERENCE", entity, key, "NumeroInventario", inv(inv_), None,
                  "Inventario que no existe en la BD; la fila no se carga.", 2, sheet, row)
        return u

    def mgmt(code, entity, key, sheet, row):
        m = mgmts.get(code)
        if not m:
            issue("INVALID_MANAGEMENT", entity, key, "GestionCodigo", code, None,
                  "Gestión inexistente o inválida en la fuente; la fila no se carga.", 2, sheet, row)
        return m

    def person(code, entity, key, field, sheet, row):
        if not code:
            return None
        p = people.get(code)
        if not p:
            issue("UNRESOLVED_REFERENCE", entity, key, field, code, None, "Actor inexistente en la BD; se deja vacío.", 1, sheet, row)
        return p

    def pending(value, entity, key, field, sheet, row, normalized):
        if low(value) in PENDING:
            issue("VALUE_PENDING_CONFIRMATION", entity, key, field, value, normalized,
                  f"'{value}' en la fuente: se registra como pendiente.", 0, sheet, row)

    def isr(sheet, row, key, ident, entity, target_sql, mig, rec, data_sql, notes):
        return (f"INSERT dbo.ImportSourceRows(ImportBatchId, SourceSheet, SourceRowNumber, SourceRowKey, OriginalIdentifier, TargetEntityName, "
                f"TargetEntityKey, MigrationStatus, ReconciliationStatus, OriginalDataJson, Notes, LastReconciledDate, CreatedDate) "
                f"VALUES(@Batch, N'{sheet}', {row}, {q(key)}, {q(ident)}, N'{entity}', CAST({target_sql} AS nvarchar(20)), {mig}, {rec}, "
                f"{data_sql}, {q(notes)}, @Now, @Now);")

    def not_done(sheet, key, entity):
        return (f"IF NOT EXISTS (SELECT 1 FROM dbo.ImportSourceRows WHERE ImportBatchId = @Batch AND SourceSheet = N'{sheet}' "
                f"AND SourceRowKey = {q(key)} AND TargetEntityName = N'{entity}')")

    def target(sheet, key, entity):
        return (f"(SELECT TOP 1 CAST(TargetEntityKey AS int) FROM dbo.ImportSourceRows WHERE ImportBatchId = @Batch "
                f"AND SourceSheet = N'{sheet}' AND SourceRowKey = {q(key)} AND TargetEntityName = N'{entity}')")

    blocks = defaultdict(list)  # sección -> bloques (cada bloque es atómico; se agrupan de a 100 por transacción)

    def by_key(sheet):
        return {r["ClaveExcel"]: r for r in rows if r["Hoja"] == sheet and r["Resultado"] in ("PAR", "SOLO_EXCEL")}

    # ---------------- L-6 ----------------
    dec = by_key("L-6")
    for r in x["VerificacionesL6"]:
        key, row = r["VerificacionCodigo"], r["_row"]
        d = dec.get(key)
        if not d:
            continue  # ya cargado
        u = unit(r["NumeroInventario"], "Verification", key, "VerificacionesL6", row)
        m = mgmt(r["GestionCodigo"], "Verification", key, "VerificacionesL6", row)
        if not u or not m:
            continue
        cond = COND_MAP.get(low(r["CondicionFisica"]))
        obs = OBSERVED_MAP.get(low(r["EstadoEquipo"]))
        if cond is None:
            cond = 9
            issue("UNMAPPED_VALUE", "Verification", key, "CondicionFisica", r["CondicionFisica"], "PorConfirmar",
                  "Valor no reconocido; se registra Por confirmar.", 1, "VerificacionesL6", row)
        pending(r["CondicionFisica"], "Verification", key, "CondicionFisica", "VerificacionesL6", row, "PorConfirmar")
        pending(r["EstadoEquipo"], "Verification", key, "EstadoEquipo", "VerificacionesL6", row, "PorConfirmar")
        text = "\n".join(t.strip() for t in (r["ProblemasObservados"], r["Observaciones"]) if t and str(t).strip()) or None
        resp = person(r["ResponsableActorCodigo"], "Verification", key, "ResponsableActorCodigo", "VerificacionesL6", row)
        values = (f"{u[0]}, {m}, {q(to_date(r['FechaVerificacion']))}, {q(text)}, {cond}, 1, @Now, {q('V4:' + key)}, @Batch, "
                  f"{q(resp)}, {q(obs)}")
        insert = ("INSERT dbo.Verifications(EquipmentUnitId, ManagementId, Date, Observations, PhysicalCondition, Status, CreatedDate, "
                  f"HistoricalSourceKey, ImportBatchId, ResponsiblePersonId, ObservedEquipmentStatus) VALUES({values}); SET @id = SCOPE_IDENTITY();")
        b = [not_done("VerificacionesL6", key, "Verification"), "BEGIN"]
        if d["Resultado"] == "SOLO_EXCEL":
            b += [insert, isr("VerificacionesL6", row, key, None, "Verification", "@id", 1, 2, jq(r), "Solo en la V4: creada.")]
        elif d["Inferido"] == "Si":
            b += [f"UPDATE dbo.Verifications SET Status = 99, LastModifiedDate = @Now WHERE Id = {d['IdBD']} AND Status <> 99;",
                  insert,
                  isr("VerificacionesL6", row, key, None, "Verification", "@id", 1, 5, jq(r),
                      f"Reemplaza a la verificación inferida {d['ClaveBD']} (Id {d['IdBD']}, anulada)."),
                  isr("VerificacionesL6", row, key, d["ClaveBD"], "Verification.Superseded", d["IdBD"], 1, 5, "NULL",
                      "Verificación inferida anulada: la V4 la reemplaza.")]
            issue("LEGACY_INFERRED_SUPERSEDED", "Verification", d["ClaveBD"], None, key, None,
                  f"Inferida anulada; la reemplaza {key} (diferencia {d['DiasDiferencia']} días).", 0, "VerificacionesL6", row)
        else:
            prev = ("(SELECT v.ManagementId, v.Date, v.PhysicalCondition, v.ObservedEquipmentStatus, v.Observations, "
                    f"v.ResponsiblePersonId, v.Status FROM dbo.Verifications v WHERE v.Id = {d['IdBD']} FOR JSON PATH, WITHOUT_ARRAY_WRAPPER)")
            b += [isr("VerificacionesL6", row, key, d["ClaveBD"], "Verification", d["IdBD"], 1, 1,
                      f"CONCAT(N'{{\"excel\":', {jq(r)}, N',\"bdPrevio\":', {prev}, N'}}')",
                      f"Par por inventario y fecha ({d['DiasDiferencia']} días); valores de la V4 aplicados, previos en OriginalDataJson."),
                  f"UPDATE dbo.Verifications SET ManagementId = {m}, Date = {q(to_date(r['FechaVerificacion']))}, PhysicalCondition = {cond}, "
                  f"ObservedEquipmentStatus = {q(obs)}, Observations = {q(text)}, ResponsiblePersonId = {q(resp)}, Status = 1, "
                  f"LastModifiedDate = @Now WHERE Id = {d['IdBD']};"]
        if d["Resultado"] == "PAR" and d["GestionExcel"] != d["GestionBD"]:
            issue("MANAGEMENT_SHIFTED_BY_RULE", "Verification", key, "GestionCodigo", d["GestionBD"], d["GestionExcel"],
                  "La gestión de la V4 difiere de la registrada; se aplica la de la V4 (regla de negocio).", 0, "VerificacionesL6", row)
        b.append("END")
        blocks["L-6"].append("\n".join(b))

    # ---------------- L-7 ----------------
    dec = by_key("L-7")
    extras = defaultdict(list)
    for r in rows:
        if r["Hoja"] == "L-7" and r["Resultado"] == "PAR_FILA_PARTIDA":
            extras[r["ClaveExcel"]].append(r)
    units_by_req = defaultdict(list)
    for r in x["SolicitudUnidades"]:
        units_by_req[r["SolicitudCodigo"]].append(r)
    for r in x["SolicitudesL7"]:
        key, row = r["SolicitudCodigo"], r["_row"]
        d = dec.get(key)
        if not d:
            continue
        m = mgmt(r["GestionCodigo"], "Request", key, "SolicitudesL7", row)
        if not m:
            continue
        req_units = []
        for su in units_by_req.get(key, []):
            u = unit(su["NumeroInventario"], "RequestEquipmentUnit", su["SolicitudUnidadCodigo"], "SolicitudUnidades", su["_row"])
            if u and u not in req_units:
                req_units.append(u)
        pending(r["Prioridad"], "Request", key, "Prioridad", "SolicitudesL7", row, "PorConfirmar")
        pending(r["Estado"], "Request", key, "Estado", "SolicitudesL7", row, "PorConfirmar")
        requester = person(r["SolicitanteActorCodigo"], "Request", key, "SolicitanteActorCodigo", "SolicitudesL7", row)
        desc, sugg = (r["ProblemaReportado"] or "").strip(), (r["Sugerencia"] or "").strip() or None
        date = to_date(r["FechaSolicitud"])
        b = [not_done("SolicitudesL7", key, "Request"), "BEGIN"]
        if d["Resultado"] == "SOLO_EXCEL":
            labs = {u[1] for u in req_units}
            lab = labs.pop() if len(labs) == 1 else None
            eu = req_units[0][0] if len(req_units) == 1 else None
            b += ["INSERT dbo.Requests(LaboratoryId, EquipmentUnitId, ManagementId, Description, Priority, Status, CreatedDate, Type, "
                  "HistoricalSourceKey, ImportBatchId, LocationResolutionStatus, RequestedByPersonId, Suggestion, RequestDate) "
                  f"VALUES({q(lab)}, {q(eu)}, {m}, {q(desc)}, 9, 98, @Now, 1, {q('V4:' + key)}, @Batch, {1 if lab else 0}, "
                  f"{q(requester)}, {q(sugg)}, {q(date)}); SET @rid = SCOPE_IDENTITY();",
                  isr("SolicitudesL7", row, key, None, "Request", "@rid", 1, 2, jq(r), "Solo en la V4: creada.")]
            if not req_units:
                issue("VALUE_MISSING", "Request", key, "SolicitudUnidades", None, None,
                      "Solicitud sin unidades en la fuente: se carga sin unidad ni laboratorio.", 0, "SolicitudesL7", row)
        else:
            canon = d["IdBD"]
            prev = ("(SELECT r.ManagementId, r.RequestDate, r.Priority, r.Description, r.Suggestion, r.RequestedByPersonId, r.Type, "
                    f"r.Status, r.EstimatedRepairTime FROM dbo.Requests r WHERE r.Id = {canon} FOR JSON PATH, WITHOUT_ARRAY_WRAPPER)")
            b += [f"SET @rid = {canon};",
                  isr("SolicitudesL7", row, key, d["ClaveBD"], "Request", canon, 1, 1,
                      f"CONCAT(N'{{\"excel\":', {jq(r)}, N',\"bdPrevio\":', {prev}, N'}}')",
                      "Par por conjunto de unidades; solicitud canónica con los valores de la V4 (previos en OriginalDataJson)."),
                  f"UPDATE dbo.Requests SET ManagementId = {m}, RequestDate = {q(date)}, Priority = 9, Description = {q(desc)}, "
                  f"Suggestion = {q(sugg)}, RequestedByPersonId = {q(requester)}, Type = 1, Status = 98, EstimatedRepairTime = NULL, "
                  f"LastModifiedDate = @Now WHERE Id = {canon};"]
            for ex in extras.get(key, []):
                b += [isr("SolicitudesL7", row, key + "|" + ex["ClaveBD"], ex["ClaveBD"], "Request.SplitMerged", ex["IdBD"], 1, 5, "NULL",
                          f"Fila partida de la solicitud canónica Id {canon}: cancelada, su unidad queda vinculada a la canónica."),
                      f"UPDATE dbo.Requests SET Status = 99, LastModifiedDate = @Now WHERE Id = {ex['IdBD']} AND Status <> 99;"]
                issue("LEGACY_SPLIT_MERGED", "Request", ex["ClaveBD"], None, key, None,
                      f"Fila partida cancelada; la solicitud canónica es {d['ClaveBD']} ({key}).", 0, "SolicitudesL7", row)
            if d["GestionExcel"] != d["GestionBD"]:
                issue("MANAGEMENT_SHIFTED_BY_RULE", "Request", key, "GestionCodigo", d["GestionBD"], d["GestionExcel"],
                      "La gestión de la V4 difiere de la registrada; se aplica la de la V4 (regla de negocio).", 0, "SolicitudesL7", row)
        for i, u in enumerate(req_units):
            primary = 1 if d["Resultado"] == "SOLO_EXCEL" and i == 0 else 0
            b.append(f"IF NOT EXISTS (SELECT 1 FROM dbo.RequestEquipmentUnits WHERE RequestId = @rid AND EquipmentUnitId = {u[0]}) "
                     f"INSERT dbo.RequestEquipmentUnits(RequestId, EquipmentUnitId, IsLegacyPrimary, IsActive, CreatedDate) "
                     f"VALUES(@rid, {u[0]}, {primary}, 1, @Now);")
        b.append("END")
        blocks["L-7"].append("\n".join(b))

    # ---------------- L-8 ----------------
    dec = by_key("L-8")
    loaded_cost = 0.0
    for r in x["MantenimientosL8"]:
        key, row = r["MantenimientoCodigo"], r["_row"]
        d = dec.get(key)
        if not d:
            continue
        u = unit(r["NumeroInventario"], "Maintenance", key, "MantenimientosL8", row)
        m = mgmt(r["GestionCodigo"], "Maintenance", key, "MantenimientosL8", row)
        if not u or not m:
            continue
        mtype = MAINT_TYPE_MAP.get(low(r["TipoMantenimiento"]), 9)
        stype = SERVICE_MAP.get(low(r["TipoServicio"]), 9)
        pending(r["TipoMantenimiento"], "Maintenance", key, "TipoMantenimiento", "MantenimientosL8", row, "PorConfirmar")
        pending(r["TipoServicio"], "Maintenance", key, "TipoServicio", "MantenimientosL8", row, "PorConfirmar")
        if low(r["EstadoCalidad"]) == "requiere revisión":
            issue("CONTENT_REVIEW_REQUIRED", "Maintenance", key, "EstadoCalidad", r["EstadoCalidad"], None,
                  "La fuente marca la fila para revisión.", 0, "MantenimientosL8", row)
        start, end, nxt = to_date(r["FechaInicio"]), to_date(r["FechaFin"]), to_date(r["ProximoMantenimiento"])
        cost = float(r["CostoTotalFuenteBs"] or 0)
        loaded_cost += cost
        desc = (r["TrabajoRealizado"] or "").strip() or None
        rec = (r["Recomendaciones"] or "").strip() or None if isinstance(r["Recomendaciones"], str) else None
        b = [not_done("MantenimientosL8", key, "Maintenance"), "BEGIN"]
        if d["Resultado"] == "SOLO_EXCEL":
            b += ["INSERT dbo.Maintenances(EquipmentUnitId, MaintenanceType, ManagementId, ServiceType, StartDate, EndDate, Description, "
                  "Status, CompletionPercentage, Step1_Cleaning, Step2_Calibration, Step3_Testing, Step4_FinalReview, ActualCost, "
                  "Recommendations, SuggestedNextMaintenanceDate, CreatedDate, HistoricalSourceKey, ImportBatchId) "
                  f"VALUES({u[0]}, {mtype}, {m}, {stype}, {q(start)}, {q(end)}, {q(desc)}, 2, 100, 0, 0, 0, 0, {cost}, {q(rec)}, "
                  f"{q(nxt)}, @Now, {q('V4:' + key)}, @Batch); SET @mid = SCOPE_IDENTITY();",
                  isr("MantenimientosL8", row, key, None, "Maintenance", "@mid", 1, 2, jq(r), "Solo en la V4: creado.")]
        else:
            mid = d["IdBD"]
            prev = ("(SELECT t.ManagementId, t.StartDate, t.EndDate, t.MaintenanceType, t.ServiceType, t.Description, t.Status, "
                    "t.CompletionPercentage, t.ActualCost, t.Recommendations, t.SuggestedNextMaintenanceDate, t.TechnicianId "
                    f"FROM dbo.Maintenances t WHERE t.Id = {mid} FOR JSON PATH, WITHOUT_ARRAY_WRAPPER)")
            b += [f"SET @mid = {mid};",
                  isr("MantenimientosL8", row, key, d["ClaveBD"], "Maintenance", mid, 1, 1,
                      f"CONCAT(N'{{\"excel\":', {jq(r)}, N',\"bdPrevio\":', {prev}, N'}}')",
                      f"Par por inventario y fecha de fin ({d['DiasDiferencia']} días); valores de la V4 aplicados, previos en OriginalDataJson."),
                  f"UPDATE dbo.Maintenances SET ManagementId = {m}, MaintenanceType = {mtype}, ServiceType = {stype}, StartDate = {q(start)}, "
                  f"EndDate = {q(end)}, Description = {q(desc)}, Status = 2, CompletionPercentage = 100, ActualCost = {cost}, "
                  f"Recommendations = COALESCE({q(rec)}, Recommendations), SuggestedNextMaintenanceDate = {q(nxt)}, "
                  f"LastModifiedDate = @Now WHERE Id = {mid};",
                  "INSERT #Issue SELECT 'LEGACY_INFERRED_SUPERSEDED', 'CostDetail', CAST(cd.Id AS nvarchar(20)), 'Subtotal', "
                  "CAST(cd.Quantity * cd.UnitPrice AS nvarchar(40)), NULL, "
                  f"N'Costo inferido anulado: el costo real es CostoTotalFuenteBs de {key}.', 0, N'MantenimientosL8', {row} "
                  f"FROM dbo.CostDetails cd WHERE cd.MaintenanceId = {mid} AND cd.IsVoided = 0 AND {INFERRED_COST};",
                  f"UPDATE cd SET IsVoided = 1, VoidedDate = @Now, VoidReason = N'Costo inferido reemplazado por CostoTotalFuenteBs de la V4 ({key}).' "
                  f"FROM dbo.CostDetails cd WHERE cd.MaintenanceId = {mid} AND cd.IsVoided = 0 AND {INFERRED_COST};"]
        b.append("END")
        blocks["L-8"].append("\n".join(b))

    # ---------------- Participantes L-8 ----------------
    for r in x["MantParticipantes"]:
        key, row = r["MantenimientoCodigo"], r["_row"]
        p = person(r["ActorCodigo"], "MaintenanceParticipant", r["ParticipanteCodigo"], "ActorCodigo", "MantParticipantes", row)
        if not p:
            continue
        primary = 1 if low(r["EsPrincipal"]) in ("sí", "si") else 0
        active = 0 if low(r["Estado"]) not in ("activo", "") else 1
        if low(r["Rol"]) != "técnico":
            issue("UNMAPPED_VALUE", "MaintenanceParticipant", r["ParticipanteCodigo"], "Rol", r["Rol"], None,
                  "Rol de participante no previsto; no se carga.", 1, "MantParticipantes", row)
            continue
        if p in entities:
            issue("ORGANIZATION_AS_TECHNICIAN", "MaintenanceParticipant", r["ParticipanteCodigo"], "ActorCodigo", r["ActorCodigo"], None,
                  "Organización registrada como técnico en la fuente; se carga fiel al Excel.", 0, "MantParticipantes", row)
        b = [f"SET @mid = {target('MantenimientosL8', key, 'Maintenance')};",
             f"IF @mid IS NOT NULL AND {active} = 1 AND NOT EXISTS (SELECT 1 FROM dbo.MaintenanceParticipants WHERE MaintenanceId = @mid "
             f"AND PersonId = {p} AND Role = 1 AND IsActive = 1 AND IsPrimary = {primary})",
             "BEGIN"]
        if primary:
            b += ["INSERT #Issue SELECT 'CONTENT_CONFLICT', 'Maintenance', " + q(key) + ", 'TecnicoPrincipal', CAST(mp.PersonId AS nvarchar(20)), "
                  f"N'{p}', N'El técnico principal de la BD difiere del de la V4: queda como técnico no principal.', 1, N'MantParticipantes', {row} "
                  f"FROM dbo.MaintenanceParticipants mp WHERE mp.MaintenanceId = @mid AND mp.IsPrimary = 1 AND mp.IsActive = 1 AND mp.PersonId <> {p};",
                  "UPDATE dbo.MaintenanceParticipants SET IsPrimary = 0 WHERE MaintenanceId = @mid AND IsPrimary = 1 AND IsActive = 1;"]
        b += [f"IF EXISTS (SELECT 1 FROM dbo.MaintenanceParticipants WHERE MaintenanceId = @mid AND PersonId = {p} AND Role = 1 AND IsActive = 1) "
              f"UPDATE dbo.MaintenanceParticipants SET IsPrimary = {primary} WHERE MaintenanceId = @mid AND PersonId = {p} AND Role = 1 AND IsActive = 1;",
              "ELSE INSERT dbo.MaintenanceParticipants(MaintenanceId, PersonId, Role, IsPrimary, IsActive, AssignedAt) "
              f"SELECT @mid, {p}, 1, {primary}, 1, ISNULL(StartDate, @Now) FROM dbo.Maintenances WHERE Id = @mid;",
              "END"]
        if primary:
            b.append(f"IF @mid IS NOT NULL UPDATE dbo.Maintenances SET TechnicianId = {p} WHERE Id = @mid AND ISNULL(TechnicianId, 0) <> {p};")
        blocks["Participantes"].append("\n".join(b))

    # ---------------- Mantenimiento <-> solicitud ----------------
    first_for_request = {}
    for r in sorted(x["MantSolicitudes"], key=lambda r: r["MantenimientoSolicitudCodigo"]):
        first_for_request.setdefault(r["SolicitudCodigo"], r["MantenimientoSolicitudCodigo"])
    for r in x["MantSolicitudes"]:
        code, row = r["MantenimientoSolicitudCodigo"], r["_row"]
        legacy = 1 if first_for_request[r["SolicitudCodigo"]] == code else 0
        b = [f"SET @mid = {target('MantenimientosL8', r['MantenimientoCodigo'], 'Maintenance')};",
             f"SET @rid = {target('SolicitudesL7', r['SolicitudCodigo'], 'Request')};",
             "IF @mid IS NULL OR @rid IS NULL "
             f"INSERT #Issue VALUES('UNRESOLVED_REFERENCE', 'MaintenanceRequest', {q(code)}, 'MantenimientoCodigo/SolicitudCodigo', "
             f"{q(r['MantenimientoCodigo'] + ' / ' + r['SolicitudCodigo'])}, NULL, N'Mantenimiento o solicitud no cargados; el vínculo no se crea.', 2, N'MantSolicitudes', {row});",
             "IF @mid IS NOT NULL AND @rid IS NOT NULL",
             "BEGIN",
             "INSERT #MS VALUES(@mid, @rid);",
             "IF NOT EXISTS (SELECT 1 FROM dbo.MaintenanceRequests WHERE MaintenanceId = @mid AND RequestId = @rid) "
             "INSERT dbo.MaintenanceRequests(MaintenanceId, RequestId, IsLegacyPrimary, IsActive, CreatedDate) "
             f"SELECT @mid, @rid, CASE WHEN {legacy} = 1 AND NOT EXISTS (SELECT 1 FROM dbo.MaintenanceRequests WHERE RequestId = @rid AND IsLegacyPrimary = 1) "
             "THEN 1 ELSE 0 END, 1, @Now;",
             "IF (SELECT RequestId FROM dbo.Maintenances WHERE Id = @mid) IS NULL AND NOT EXISTS (SELECT 1 FROM dbo.Maintenances WHERE RequestId = @rid) "
             "UPDATE dbo.Maintenances SET RequestId = @rid, LastModifiedDate = @Now WHERE Id = @mid;",
             "ELSE IF (SELECT ISNULL(RequestId, 0) FROM dbo.Maintenances WHERE Id = @mid) NOT IN (0, @rid) "
             f"INSERT #Issue VALUES('CONTENT_CONFLICT', 'Maintenance', {q(r['MantenimientoCodigo'])}, 'RequestId', NULL, {q(r['SolicitudCodigo'])}, "
             f"N'El mantenimiento ya tiene otra solicitud principal; la de la V4 queda vinculada en MaintenanceRequests.', 1, N'MantSolicitudes', {row});",
             "END"]
        blocks["MantSolicitudes"].append("\n".join(b))

    # ---------------- L-48 ----------------
    seen_plan = defaultdict(list)
    for r in x["PlanL48"]:
        key, row = r["PlanCodigo"], r["_row"]
        u = unit(r["NumeroInventario"], "MaintenancePlan", key, "PlanL48", row)
        m = mgmt(r["GestionCodigo"], "MaintenancePlan", key, "PlanL48", row)
        if not u or not m:
            continue
        mtype = MAINT_TYPE_MAP.get(low(r["TipoMantenimiento"]), 9)
        stype = SERVICE_MAP.get(low(r["TipoServicio"]))
        stype = None if stype == 9 else stype  # CK_MaintenancePlans_ServiceType no admite 9: queda vacío
        pending(r["TipoMantenimiento"], "MaintenancePlan", key, "TipoMantenimiento", "PlanL48", row, "PorConfirmar")
        pending(r["TipoServicio"], "MaintenancePlan", key, "TipoServicio", "PlanL48", row, None)
        pending(r["Estado"], "MaintenancePlan", key, "Estado", "PlanL48", row, None)
        if low(r["EstadoCalidad"]) == "requiere revisión":
            issue("CONTENT_REVIEW_REQUIRED", "MaintenancePlan", key, "EstadoCalidad", r["EstadoCalidad"], None,
                  "La fuente marca la fila para revisión.", 0, "PlanL48", row)
        resp = person(r["ResponsableActorCodigo"], "MaintenancePlan", key, "ResponsableActorCodigo", "PlanL48", row)
        dup = (r["GestionCodigo"], inv(r["NumeroInventario"]), low(r["TipoMantenimiento"]))
        seen_plan[dup].append(key)
        if len(seen_plan[dup]) > 1:
            issue("POSSIBLE_DUPLICATE", "MaintenancePlan", key, "Gestion/Inventario/Tipo", seen_plan[dup][0], None,
                  "Otro plan de la fuente tiene la misma gestión, inventario y tipo; se cargan ambos.", 1, "PlanL48", row)
        blocks["L-48"].append(
            f"IF NOT EXISTS (SELECT 1 FROM dbo.MaintenancePlans WHERE PlanCode = {q(key)})\nBEGIN\n"
            "INSERT dbo.MaintenancePlans(EquipmentUnitId, LaboratoryId, ManagementId, PlanCode, HistoricalSourceKey, MaintenanceType, ServiceType, "
            f"ResponsiblePersonId, PlannedDate, Status, ImportBatchId, CreatedDate) VALUES({u[0]}, {q(u[1])}, {m}, {q(key)}, {q('V4:' + key)}, "
            f"{mtype}, {q(stype)}, {q(resp)}, {q(to_date(r['FechaPlanificada']))}, NULL, @Batch, @Now); SET @id = SCOPE_IDENTITY();\n"
            + isr("PlanL48", row, key, None, "MaintenancePlan", "@id", 1, 2, jq(r), "Solo en la V4: creado.") + "\nEND")

    # ---------------- Lo que queda solo en la BD ----------------
    entity_of = {"L-6": "Verification", "L-7": "Request", "L-8": "Maintenance"}
    for r in rows:
        if r["Resultado"] in ("SOLO_BD", "GRUPO_SIN_PAR"):
            code = "LEGACY_INFERRED_UNMATCHED" if r["Inferido"] == "Si" else "LEGACY_ONLY_IN_DB"
            issue(code, entity_of[r["Hoja"]], r["ClaveBD"] or str(r["IdBD"]), None, str(r["IdBD"]), None,
                  "Registro de la BD sin par en la V4; se conserva." + (f" {r['Nota']}" if r["Nota"] and "Grupo" in r["Nota"] else ""),
                  1, None, None)

    # ---------------- Script ----------------
    w("SET NOCOUNT ON;")
    w("SET XACT_ABORT ON;")
    w("IF DB_NAME() <> N'DB_Laboratorios_Univalle_DEMO_QA' THROW 50000, 'Destino incorrecto: solo DEMO_QA', 1;")
    w("DECLARE @Now datetime2 = SYSUTCDATETIME();")
    w("DECLARE @n int, @id int, @mid int, @rid int;")
    w(f"IF NOT EXISTS (SELECT 1 FROM dbo.ImportBatches WHERE Code = N'{batch_code}') "
      "INSERT dbo.ImportBatches(Code, SourceType, SourceName, SourceSha256, ImportedAt, Status, Notes, ContractVersion) "
      f"VALUES(N'{batch_code}', N'Excel', N'BaseEstructuraFinal_V4.xlsx', N'{sha}', @Now, 1, "
      "N'V4 bloque 2: histórico L-6, L-7, L-8 y L-48 según conciliacion_v4.csv aprobada. Solo DEMO_QA.', N'base-estructural-v4');")
    w(f"DECLARE @Batch int = (SELECT Id FROM dbo.ImportBatches WHERE Code = N'{batch_code}');")
    w("CREATE TABLE #Issue(IssueCode nvarchar(60), EntityName nvarchar(60), EntityKey nvarchar(100), FieldName nvarchar(100), "
      "OriginalValue nvarchar(400), NormalizedValue nvarchar(400), Description nvarchar(500), Severity int, SourceSheet nvarchar(60), SourceRow int);")
    w("CREATE TABLE #MS(MaintenanceId int, RequestId int);")
    declare = ("DECLARE @Now datetime2 = SYSUTCDATETIME(); DECLARE @n int, @id int, @mid int, @rid int; "
               f"DECLARE @Batch int = (SELECT Id FROM dbo.ImportBatches WHERE Code = N'{batch_code}');")
    for section in ("L-6", "L-7", "L-8", "Participantes", "MantSolicitudes", "L-48"):
        bl = blocks[section]
        w(f"-- {section}: {len(bl)} filas")
        for i in range(0, len(bl), TX_SIZE):
            w("GO")
            w(declare)
            w("BEGIN TRANSACTION;")
            sql.extend(bl[i:i + TX_SIZE])
            w("COMMIT TRANSACTION;")
    w("GO")
    w(declare)
    w("""-- Vínculos de la BD contradichos por la V4 (mantenimiento y solicitud con versión V4): se desactivan, no se borran
BEGIN TRANSACTION;
DECLARE @M TABLE(id int PRIMARY KEY);
INSERT @M SELECT DISTINCT CAST(TargetEntityKey AS int) FROM dbo.ImportSourceRows WHERE ImportBatchId = @Batch AND TargetEntityName = N'Maintenance';
DECLARE @R TABLE(id int PRIMARY KEY);
INSERT @R SELECT DISTINCT CAST(TargetEntityKey AS int) FROM dbo.ImportSourceRows WHERE ImportBatchId = @Batch AND TargetEntityName IN (N'Request', N'Request.SplitMerged');
IF EXISTS (SELECT 1 FROM #MS)
BEGIN
    INSERT #Issue SELECT 'LEGACY_LINK_SUPERSEDED', 'MaintenanceRequest', CONCAT(mr.MaintenanceId, '-', mr.RequestId), NULL, NULL, NULL,
           N'Vínculo de la BD contradicho por MantSolicitudes de la V4: desactivado (no se borra).', 1, N'MantSolicitudes', NULL
    FROM dbo.MaintenanceRequests mr
    WHERE mr.IsActive = 1 AND mr.MaintenanceId IN (SELECT id FROM @M) AND mr.RequestId IN (SELECT id FROM @R)
      AND NOT EXISTS (SELECT 1 FROM #MS s WHERE s.MaintenanceId = mr.MaintenanceId AND s.RequestId = mr.RequestId);
    UPDATE mr SET IsActive = 0, IsLegacyPrimary = 0, DeactivatedDate = @Now, LastModifiedDate = @Now
    FROM dbo.MaintenanceRequests mr
    WHERE mr.IsActive = 1 AND mr.MaintenanceId IN (SELECT id FROM @M) AND mr.RequestId IN (SELECT id FROM @R)
      AND NOT EXISTS (SELECT 1 FROM #MS s WHERE s.MaintenanceId = mr.MaintenanceId AND s.RequestId = mr.RequestId);

    INSERT #Issue SELECT 'LEGACY_LINK_SUPERSEDED', 'Maintenance', CAST(t.Id AS nvarchar(20)), 'RequestId', CAST(t.RequestId AS nvarchar(20)), NULL,
           N'RequestId apuntaba a una solicitud que la V4 no vincula; se reasigna a la de la V4 si está libre.', 1, N'MantSolicitudes', NULL
    FROM dbo.Maintenances t
    WHERE t.Id IN (SELECT id FROM @M) AND t.RequestId IN (SELECT id FROM @R)
      AND NOT EXISTS (SELECT 1 FROM #MS s WHERE s.MaintenanceId = t.Id AND s.RequestId = t.RequestId);
    UPDATE t SET RequestId = NULL, LastModifiedDate = @Now
    FROM dbo.Maintenances t
    WHERE t.Id IN (SELECT id FROM @M) AND t.RequestId IN (SELECT id FROM @R)
      AND NOT EXISTS (SELECT 1 FROM #MS s WHERE s.MaintenanceId = t.Id AND s.RequestId = t.RequestId);

    ;WITH c AS (
        SELECT t.Id mid, s.RequestId rid,
               ROW_NUMBER() OVER (PARTITION BY s.RequestId ORDER BY CASE WHEN mr.IsLegacyPrimary = 1 THEN 0 ELSE 1 END, t.Id) rn
        FROM dbo.Maintenances t
        JOIN #MS s ON s.MaintenanceId = t.Id
        JOIN dbo.MaintenanceRequests mr ON mr.MaintenanceId = t.Id AND mr.RequestId = s.RequestId AND mr.IsActive = 1
        WHERE t.RequestId IS NULL AND t.Id IN (SELECT id FROM @M)
          AND NOT EXISTS (SELECT 1 FROM dbo.Maintenances o WHERE o.RequestId = s.RequestId))
    UPDATE t SET RequestId = c.rid, LastModifiedDate = @Now FROM dbo.Maintenances t JOIN c ON c.mid = t.Id WHERE c.rn = 1;

    -- Cada solicitud V4 con vínculos activos conserva un vínculo principal histórico
    UPDATE mr SET IsLegacyPrimary = 1, LastModifiedDate = @Now
    FROM dbo.MaintenanceRequests mr
    JOIN (SELECT a.RequestId, MIN(a.MaintenanceId) m FROM dbo.MaintenanceRequests a
          WHERE a.IsActive = 1 AND a.RequestId IN (SELECT id FROM @R)
          GROUP BY a.RequestId HAVING MAX(CAST(a.IsLegacyPrimary AS int)) = 0) z ON z.RequestId = mr.RequestId AND z.m = mr.MaintenanceId;
END;
COMMIT TRANSACTION;""")
    w("-- Vínculos mantenimiento-solicitud de la BD que la V4 no trae")
    w("INSERT #Issue SELECT 'LEGACY_ONLY_IN_DB', 'MaintenanceRequest', CONCAT(mr.MaintenanceId, '-', mr.RequestId), NULL, NULL, NULL, "
      "N'Vínculo mantenimiento-solicitud de la BD sin par en MantSolicitudes; se conserva.', 1, NULL, NULL "
      "FROM dbo.MaintenanceRequests mr WHERE mr.IsActive = 1 AND NOT EXISTS (SELECT 1 FROM #MS s WHERE s.MaintenanceId = mr.MaintenanceId AND s.RequestId = mr.RequestId) "
      "AND EXISTS (SELECT 1 FROM #MS);")
    sql.extend(issues)
    w("""SET @n = 1;
WHILE @n > 0
BEGIN
    BEGIN TRANSACTION;
    INSERT dbo.DataQualityIssues(ImportBatchId, IssueCode, EntityName, EntityKey, FieldName, OriginalValue, NormalizedValue, Description, Severity, Status, CreatedDate, SourceSheet, SourceRowNumber)
    SELECT TOP (100) @Batch, i.IssueCode, i.EntityName, i.EntityKey, i.FieldName, i.OriginalValue, i.NormalizedValue, i.Description, i.Severity, 0, @Now, i.SourceSheet, i.SourceRow
    FROM (SELECT IssueCode, EntityName, EntityKey, FieldName, MIN(OriginalValue) OriginalValue, MIN(NormalizedValue) NormalizedValue, MIN(Description) Description,
                 MAX(Severity) Severity, MIN(SourceSheet) SourceSheet, MIN(SourceRow) SourceRow
          FROM #Issue GROUP BY IssueCode, EntityName, EntityKey, FieldName) i
    WHERE NOT EXISTS (SELECT 1 FROM dbo.DataQualityIssues d WHERE d.ImportBatchId = @Batch AND d.IssueCode = i.IssueCode
                      AND ISNULL(d.EntityKey, N'') = ISNULL(i.EntityKey, N'') AND ISNULL(d.FieldName, N'') = ISNULL(i.FieldName, N''));
    SET @n = @@ROWCOUNT;
    COMMIT TRANSACTION;
END;""")
    w(f"""-- Verificación
SELECT 'ISR ' + TargetEntityName k, COUNT(*) n FROM dbo.ImportSourceRows WHERE ImportBatchId = @Batch GROUP BY TargetEntityName
UNION ALL SELECT 'verificaciones V4', COUNT(*) FROM dbo.Verifications WHERE HistoricalSourceKey LIKE N'V4:%'
UNION ALL SELECT 'verificaciones anuladas', COUNT(*) FROM dbo.Verifications WHERE Status = 99
UNION ALL SELECT 'solicitudes V4', COUNT(*) FROM dbo.Requests WHERE HistoricalSourceKey LIKE N'V4:%'
UNION ALL SELECT 'solicitudes canceladas', COUNT(*) FROM dbo.Requests WHERE Status = 99
UNION ALL SELECT 'mantenimientos V4', COUNT(*) FROM dbo.Maintenances WHERE HistoricalSourceKey LIKE N'V4:%'
UNION ALL SELECT 'mantenimientos total', COUNT(*) FROM dbo.Maintenances
UNION ALL SELECT 'costos anulados', COUNT(*) FROM dbo.CostDetails WHERE IsVoided = 1
UNION ALL SELECT 'planes L-48', COUNT(*) FROM dbo.MaintenancePlans WHERE PlanCode IS NOT NULL
UNION ALL SELECT 'mant-solicitud activos', COUNT(*) FROM dbo.MaintenanceRequests WHERE IsActive = 1
UNION ALL SELECT 'mant sin tecnico principal (V4)', COUNT(*) FROM dbo.Maintenances t WHERE t.Id IN
      (SELECT CAST(TargetEntityKey AS int) FROM dbo.ImportSourceRows WHERE ImportBatchId = @Batch AND TargetEntityName = N'Maintenance')
      AND NOT EXISTS (SELECT 1 FROM dbo.MaintenanceParticipants p WHERE p.MaintenanceId = t.Id AND p.IsPrimary = 1 AND p.IsActive = 1)
UNION ALL SELECT 'incidencias lote historico', COUNT(*) FROM dbo.DataQualityIssues WHERE ImportBatchId = @Batch;
SELECT 'ActualCost filas V4' k, SUM(t.ActualCost) total FROM dbo.Maintenances t WHERE t.Id IN
      (SELECT CAST(TargetEntityKey AS int) FROM dbo.ImportSourceRows WHERE ImportBatchId = @Batch AND TargetEntityName = N'Maintenance');
SELECT 'cuarentena con inventario hoy existente' k, COUNT(*) n FROM dbo.HistoricalVerificationQuarantines h
      WHERE EXISTS (SELECT 1 FROM dbo.EquipmentUnits u WHERE u.InventoryNumber = LTRIM(RTRIM(h.InventoryRaw)));
SELECT IssueCode, COUNT(*) n FROM dbo.DataQualityIssues WHERE ImportBatchId = @Batch GROUP BY IssueCode ORDER BY IssueCode;""")

    with io.open(SQL_OUT, "w", encoding="utf-8") as f:
        f.write("\n".join(sql) + "\n")
    print(f"{SQL_OUT} | bloques " + ", ".join(f"{k} {len(v)}" for k, v in blocks.items())
          + f" | incidencias {len(issues)} | costo esperado {loaded_cost:,.2f}")


if __name__ == "__main__":
    if len(sys.argv) > 1 and sys.argv[1] == "--cargar":
        load()
    else:
        conciliate()
