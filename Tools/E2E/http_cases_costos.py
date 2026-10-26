"""WS-5 · Costos y consistencia entre módulos (copia QA, escribe datos propios vía la app).

  CO-01 L-8 con 3 líneas: Σ(cantidad × precio) = Maintenances.ActualCost = total en Maintenances/Details = Kardex de la unidad
        = valor en el Excel L-8
  CO-02 límites: cantidad 0, precio negativo, cantidad 'abc', concepto de 201 caracteres → rechazo con mensaje y sin guardar
  CO-03 decimales (FlexibleDecimalModelBinder): '1234,56' y '1.234,56' se guardan como 1234.56
  CO-05 «Completado» con costo real 0 se acepta (decisión del usuario 2026-10-18, H-010: igual en Create y Edit)
  CO-06 categorías de costo en español en Maintenances/Details
  CO-09 lo mismo que CO-01 en un plan CORRECTIVO
"""
import io
import re
import urllib.error
from decimal import Decimal

import openpyxl

import e2e_lib
import http_cases_correctivo_common as C
import http_cases_procesos_common as P
from http_cases_procesos_common import check, plan, scalar

LINEAS = [("Repuesto QAP", "1", "2", "150.50"), ("Mano de obra QAP", "2", "1", "1200"), ("Consumibles QAP", "7", "3.5", "10.25")]
TOTAL = sum(Decimal(q) * Decimal(p) for _, _, q, p in LINEAS)   # 1537.875 → la BD guarda 2 decimales


def costos(lineas):
    ov = {}
    for i, (concepto, cat, q, p) in enumerate(lineas):
        ov.update({f"Input.CostDetails[{i}].Concept": concepto, f"Input.CostDetails[{i}].Category": cat,
                   f"Input.CostDetails[{i}].Quantity": q, f"Input.CostDetails[{i}].UnitPrice": p})
    return ov


def n2_variantes(valor):
    v = Decimal(valor).quantize(Decimal("0.01"))
    entero, dec = f"{v:.2f}".split(".")
    miles = f"{int(entero):,}"
    return {f"{miles}.{dec}", f"{miles.replace(',', '.')},{dec}", f"{entero}.{dec}", f"{entero},{dec}"}


def suma_bd(mid):
    return scalar("SELECT COALESCE(SUM(Quantity * UnitPrice), 0) FROM CostDetails WHERE MaintenanceId = ? AND IsVoided = 0", mid)


def fetch(c, path):
    try:
        r = c.opener.open(e2e_lib.BASE + path, timeout=180)
        return r.status, r.read()
    except urllib.error.HTTPError as e:
        return e.code, e.read()


def consistencia(R, c, etiqueta, pid, unit):
    mid = plan(pid)["mnt"]
    if not mid:
        check(R, f"{etiqueta} el L-8 se registró (plan con mantenimiento)", False, f"plan={plan(pid)}")
        return
    actual = scalar("SELECT ActualCost FROM Maintenances WHERE Id = ?", mid)
    s = suma_bd(mid)
    check(R, f"{etiqueta} BD: Σ(cantidad × precio) de las líneas = ActualCost",
          actual is not None and abs(Decimal(str(actual)) - Decimal(str(s))) <= Decimal("0.01"), f"suma={s} ActualCost={actual}")
    check(R, f"{etiqueta} BD: ActualCost = total esperado {TOTAL:.3f} (2 decimales)",
          actual is not None and abs(Decimal(str(actual)) - TOTAL) <= Decimal("0.01"), f"ActualCost={actual}")
    st, html = c.get(f"/Maintenances/Details?id={mid}")
    check(R, f"{etiqueta} Maintenances/Details muestra el total real (Bs N2)", st == 200 and any(v in html for v in n2_variantes(actual or 0)),
          f"status={st} buscado={sorted(n2_variantes(actual or 0))}")
    st, html = c.get(f"/EquipmentUnits/Kardex/{unit}")
    check(R, f"{etiqueta} Kardex de la unidad muestra el mismo total", st == 200 and any(v in html for v in n2_variantes(actual or 0)),
          f"status={st}")
    st, body = fetch(c, f"/api/reports/download/l8?managementPlanId={pid}")
    valores = []
    if st == 200:
        try:
            wb = openpyxl.load_workbook(io.BytesIO(body), data_only=True)
            valores = [v for ws in wb.worksheets for row in ws.iter_rows(values_only=True) for v in row if isinstance(v, (int, float))]
        except Exception as exc:
            valores = [f"no abre: {exc}"]
    check(R, f"{etiqueta} Excel L-8 contiene el total real", st == 200 and any(isinstance(v, (int, float)) and abs(Decimal(str(v)) - Decimal(str(actual or 0))) <= Decimal("0.01") for v in valores),
          f"status={st} numeros={valores[:12]}")
    # CO-06 categorías en español en Details
    st, html = c.get(f"/Maintenances/Details?id={mid}")
    check(R, f"{etiqueta} CO-06 categorías de costo en español (Repuesto/Material, Mano de Obra, Consumibles), sin nombres del enum",
          all(t in html for t in ("Repuesto/Material", "Mano de Obra", "Consumibles")) and not re.search(r">\s*(SparePart|Labor|Consumables)\s*<", html), "")


def run(R, c):
    lab = P.lab_propio(c, "costos")

    # ---------------- CO-01 preventivo
    a = P.llevar_a(c, lab, "co1", "P3")
    r = P.l8(c, a["plan"], costos(LINEAS), name="CO-01 L-8 con 3 líneas", follow=True)
    check(R, "CO-01 L-8 con 3 líneas se guarda y avanza", plan(a["plan"])["phase"] == 4, P.dump(r))
    consistencia(R, c, "CO-01", a["plan"], a["unit"])

    # ---------------- CO-02 límites (cada caso sobre su propio plan en L-8)
    casos = [
        ("cantidad 0", [("Repuesto QAP", "1", "0", "100")]),
        ("precio negativo", [("Repuesto QAP", "1", "1", "-50")]),
        ("cantidad 'abc'", [("Repuesto QAP", "1", "abc", "100")]),
        ("concepto de 201 caracteres", [("c" * 201, "1", "1", "100")]),
    ]
    for i, (nombre, lineas) in enumerate(casos):
        b = P.llevar_a(c, lab, f"co2{i}", "P3")
        antes = scalar("SELECT COUNT(*) FROM Maintenances WHERE EquipmentUnitId = ?", b["unit"])
        r = P.l8(c, b["plan"], costos(lineas), name=f"CO-02 {nombre}", follow=True)
        despues = scalar("SELECT COUNT(*) FROM Maintenances WHERE EquipmentUnitId = ?", b["unit"])
        mensajes = " ".join(r.errors) + " " + P.toast_text(r)
        guardado = scalar("SELECT TOP 1 CONCAT(Quantity, ' x ', UnitPrice) FROM CostDetails cd JOIN Maintenances m ON m.Id = cd.MaintenanceId WHERE m.EquipmentUnitId = ? ORDER BY cd.Id DESC", b["unit"])
        check(R, f"CO-02 {nombre}: se rechaza con mensaje en español y no se guarda (el servidor no debe corregir el valor en silencio)",
              r.status < 500 and despues == antes and bool(mensajes.strip()) and "The field" not in mensajes,
              f"status={r.status} mantenimientos={antes}->{despues} guardado={guardado} mensajes={mensajes[:160]}")

    # ---------------- CO-03 decimales
    for i, precio in enumerate(("1234,56", "1.234,56")):
        d = P.llevar_a(c, lab, f"co3{i}", "P3")
        r = P.l8(c, d["plan"], costos([("Repuesto QAP", "1", "1", precio)]), name=f"CO-03 precio {precio}", follow=True)
        mid = plan(d["plan"])["mnt"]
        precio_bd = scalar("SELECT TOP 1 UnitPrice FROM CostDetails WHERE MaintenanceId = ?", mid) if mid else None
        check(R, f"CO-03 precio '{precio}' se guarda como 1234.56", precio_bd is not None and Decimal(str(precio_bd)) == Decimal("1234.56"),
              f"guardado={precio_bd} {P.dump(r)[:160]}")

    # ---------------- CO-05 Completado con costo 0
    e = P.llevar_a(c, lab, "co5", "P3")
    r = P.l8(c, e["plan"], costos([("Revisión sin costo QAP", "0", "1", "0")]), name="CO-05 Completado con costo 0", follow=True)
    mid = plan(e["plan"])["mnt"]
    check(R, "CO-05 L-8 «Completado» con costo real 0 se acepta y guarda ActualCost = 0 (H-010)",
          bool(mid) and scalar("SELECT Status FROM Maintenances WHERE Id = ?", mid) == 2 and Decimal(str(scalar("SELECT ActualCost FROM Maintenances WHERE Id = ?", mid) or 0)) == 0,
          f"mantenimiento={mid} estado={scalar('SELECT Status FROM Maintenances WHERE Id = ?', mid) if mid else None} ActualCost={scalar('SELECT ActualCost FROM Maintenances WHERE Id = ?', mid) if mid else None}")

    # ---------------- CO-09 correctivo
    corr = C.asegurar_correctiva(c)
    f = C.llevar_corr(c, corr, lab, "co9", "L7")
    r = P.l8(c, f["plan"], costos(LINEAS), name="CO-09 L-8 correctivo con 3 líneas", follow=True)
    check(R, "CO-09 L-8 correctivo con 3 líneas se guarda y avanza", plan(f["plan"])["phase"] == 4, P.dump(r))
    consistencia(R, c, "CO-09", f["plan"], f["unit"])


if __name__ == "__main__":
    P.run_module("costos", run)
