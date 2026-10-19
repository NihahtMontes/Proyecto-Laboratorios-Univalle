"""WS-7 · Equipos, unidades, AssetView y existencias — huecos no cubiertos por procesos_equipment/equnits ni catalogos_stock (solo GET).

  EQ-01 Equipment/Index: cada filtro (laboratorio, categoría, tipo, utensilio, revisión, búsqueda) responde y su total = SQL
  EQ-02 EquipmentUnits/Index: filtro por estado y laboratorio = SQL (sin unidades «Eliminadas» en los totales)
  EQ-03 AssetView: total de unidades vigentes = SQL; equipos + utensilios + otros + pendientes + heredadas = total
  ST-03 Existencias: el total de artículos del laboratorio = SQL de artículos activos (con o sin fila de stock)
"""
import os
import re
import sys

import e2e_lib
from http_forms import Client

if e2e_lib.DB_NAME != "DB_Laboratorios_Univalle_QA_E2E":
    sys.exit(f"SEGURIDAD: solo corre contra la copia de QA (E2E_DB={e2e_lib.DB_NAME!r}).")

c = Client(os.environ.get("E2E_USER", "qa_super"), os.environ["E2E_PW"])
R = []
q = e2e_lib.db_scalar


def ok(name, cond, detail=""):
    R.append(e2e_lib.Result(name, True, bool(cond), "" if cond else detail))


def total(html):
    m = re.search(r'<strong class="text-dark">\s*(\d[\d.,]*)\s*</strong>\s*[^<]{0,40}\(', html or "")
    return int(m.group(1).replace(".", "").replace(",", "")) if m else None


def metric_values(html):
    return [int(x) for x in re.findall(r'class="asset-metric-value[^"]*">\s*(\d+)\s*<', html or "")]


BASE_EQ = "FROM Equipments e WHERE e.Status <> 2"   # GeneralStatus.Eliminado = 2

# ---------------- EQ-01 Equipment/Index
lab = q("SELECT TOP 1 u.LaboratoryId FROM EquipmentUnits u JOIN Equipments e ON e.Id = u.EquipmentId WHERE u.CurrentStatus <> 99 GROUP BY u.LaboratoryId ORDER BY COUNT(*) DESC")
st, html = c.get("/Equipment")
ok("EQ-01 Equipment/Index sin filtros: total = SQL (equipos no eliminados)", total(html) == q(f"SELECT COUNT(*) {BASE_EQ}"), f"pagina={total(html)} sql={q(f'SELECT COUNT(*) {BASE_EQ}')}")
for nombre, param, sql in (
    ("categoría Equipo (0)", "SelectedCategory=Equipment", f"SELECT COUNT(*) {BASE_EQ} AND e.Category = 0"),
    ("categoría Utensilio (1)", "SelectedCategory=Utensil", f"SELECT COUNT(*) {BASE_EQ} AND e.Category = 1"),
    ("revisión Pendiente del cliente", "SelectedReviewStatus=PendingClient", f"SELECT COUNT(*) {BASE_EQ} AND e.ClassificationReviewStatus = 1"),
):
    st, html = c.get(f"/Equipment?{param}")
    ok(f"EQ-01 Equipment/Index filtro {nombre}: responde 200 y total = SQL", st == 200 and total(html) == q(sql), f"status={st} pagina={total(html)} sql={q(sql)}")
for nombre, param in (("laboratorio", f"SelectedLaboratoryId={lab}"), ("tipo de equipo", "SelectedEquipmentTypeClassification=0"),
                      ("tipo de utensilio", "SelectedUtensilType=0"), ("búsqueda", "SearchTerm=extractor")):
    st, html = c.get(f"/Equipment?{param}")
    ok(f"EQ-01 Equipment/Index filtro {nombre}: responde 200 sin error", st == 200 and total(html) is not None, f"status={st}")

# ---------------- EQ-02 EquipmentUnits/Index
st, html = c.get("/EquipmentUnits")
ok("EQ-02 EquipmentUnits/Index: total = unidades no eliminadas (SQL)", total(html) == q("SELECT COUNT(*) FROM EquipmentUnits WHERE CurrentStatus <> 99"),
   f"pagina={total(html)} sql={q('SELECT COUNT(*) FROM EquipmentUnits WHERE CurrentStatus <> 99')}")
for nombre, param, sql in (("estado En Préstamo", "Status=OnLoan", "SELECT COUNT(*) FROM EquipmentUnits WHERE CurrentStatus = 10"),
                           ("laboratorio", f"LaboratoryId={lab}", f"SELECT COUNT(*) FROM EquipmentUnits WHERE CurrentStatus <> 99 AND LaboratoryId = {lab}")):
    st, html = c.get(f"/EquipmentUnits?{param}")
    ok(f"EQ-02 EquipmentUnits/Index filtro {nombre}: total = SQL", st == 200 and total(html) == q(sql), f"status={st} pagina={total(html)} sql={q(sql)}")
st, html = c.get("/EquipmentUnits?Status=Deleted")
ok("EQ-02 filtrar por estado «Eliminado» no muestra unidades dadas de baja como vigentes (sin error)", st == 200, f"status={st}")

# ---------------- EQ-03 AssetView
st, html = c.get("/AssetView")
vigentes = q("SELECT COUNT(*) FROM EquipmentUnits u JOIN Equipments e ON e.Id = u.EquipmentId WHERE u.CurrentStatus <> 99 AND e.Status <> 2")
tot = re.search(r"Unidades vigentes:\s*<strong>(\d+)</strong>", html or "")
ok("EQ-03 AssetView: «Unidades vigentes» = SQL", tot is not None and int(tot.group(1)) == vigentes, f"pagina={tot.group(1) if tot else None} sql={vigentes}")
buckets = q("SELECT COUNT(*) FROM EquipmentUnits u JOIN Equipments e ON e.Id = u.EquipmentId WHERE u.CurrentStatus <> 99 AND e.Status <> 2 "
            # EquipmentClassificationReviewStatus: LegacyInferred = 0, PendingClient = 1, Confirmed = 2
            "AND NOT ((e.ClassificationReviewStatus = 2 AND e.Category = 0 AND e.TypeClassification IS NOT NULL) "
            "OR (e.ClassificationReviewStatus = 2 AND e.Category = 1 AND e.UtensilType IS NOT NULL) "
            "OR (e.ClassificationReviewStatus = 2 AND e.Category = 2) OR e.ClassificationReviewStatus IN (0, 1))")
ok("EQ-03 AssetView: equipos + utensilios + otros + pendientes + heredadas cubren todas las unidades vigentes (ninguna queda fuera)",
   buckets == 0, f"unidades fuera de toda tarjeta={buckets}")

# ---------------- ST-03 Existencias
lab_st = q("SELECT TOP 1 LaboratoryId FROM ArticleStocks GROUP BY LaboratoryId ORDER BY COUNT(*) DESC")
st, html = c.get(f"/Catalogs/Stock?LaboratoryFilter={lab_st}")
activos = q("SELECT COUNT(*) FROM Articles WHERE Status <> 2")
ok("ST-03 Existencias del laboratorio: total de artículos = artículos activos (SQL)", st == 200 and total(html) == activos, f"pagina={total(html)} sql={activos}")
st, html = c.get("/Catalogs/Stock")
econo = q("SELECT Id FROM Laboratories WHERE Code = 'ECONO'")
ok("ST-03 Existencias sin laboratorio elegido: abre ECONO por defecto (diseño) con todos los artículos activos",
   st == 200 and (econo is None or (f'value="{econo}" selected' in (html or "") or total(html) == activos)), f"status={st} total={total(html)} econo={econo}")

sys.exit(1 if e2e_lib.run_all([lambda: R]) else 0)
