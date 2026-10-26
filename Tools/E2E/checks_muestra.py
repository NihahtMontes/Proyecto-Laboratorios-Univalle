"""Muestra aleatoria (semilla fija): los campos importados aparecen en el detalle de cada registro."""
import random
import re
import sys
from functools import partial

from e2e_lib import Result, Session, db_rows, run_all

random.seed(20261015)


def money_in(n, v):
    """El valor aparece como 1.505,00 (formato es-BO) o sin separadores."""
    es = f"{float(v):,.2f}".replace(",", "X").replace(".", ",").replace("X", ".")
    return es in n or f"{float(v):.2f}" in n or str(int(v)) in n


def norm(t):
    return re.sub(r"\s+", " ", t or "").lower()


def check_unidades(s):
    rows = db_rows("SELECT Id, InventoryNumber, SerialNumber, AcquisitionValue, Notes FROM EquipmentUnits WHERE CurrentStatus <> 99")
    out = []
    for (id_, inv, serie, valor, _) in random.sample(rows, 25):
        _, t = s.text(f"/EquipmentUnits/Details?id={id_}")
        n = norm(t)
        faltan = []
        if str(inv) not in n:
            faltan.append("inventario")
        if serie and norm(serie) not in n:
            faltan.append("serie")
        if valor is not None and not money_in(n, valor):
            faltan.append("valor")
        out.append(Result(f"Unidad {inv}: inventario/serie/valor visibles", "completo", "completo" if not faltan else "faltan " + ",".join(faltan)))
    return out


def check_articulos(s):
    rows = db_rows("SELECT Code, Name, Category, UnitOfMeasure FROM Articles WHERE Status <> 2")
    out = []
    for (code, name, cat, unit) in random.sample(rows, 25):
        _, t = s.text(f"/Catalogs/Articles?SearchTerm={code}")
        n = norm(t)
        faltan = [x for x, v in (("nombre", name), ("categoria", cat), ("unidad", unit)) if v and norm(v) not in n]
        out.append(Result(f"Artículo {code}: nombre/categoría/unidad visibles", "completo", "completo" if not faltan else "faltan " + ",".join(faltan)))
    return out


def check_solicitudes(s):
    rows = db_rows("SELECT Id, Description, Suggestion FROM Requests")
    out = []
    for (id_, desc, sug) in random.sample(rows, 15):
        _, t = s.text(f"/Requests/Details/{id_}")
        n = norm(t)
        clave = norm(desc)[:60]
        faltan = []
        if clave and clave not in n:
            faltan.append("descripcion")
        if sug and norm(sug)[:40] not in n:
            faltan.append("sugerencia")
        out.append(Result(f"Solicitud L-7 {id_}: descripción/sugerencia visibles", "completo", "completo" if not faltan else "faltan " + ",".join(faltan)))
    return out


def check_mantenimientos(s):
    rows = db_rows("SELECT Id, Description, ActualCost FROM Maintenances")
    out = []
    for (id_, desc, costo) in random.sample(rows, 15):
        _, t = s.text(f"/Maintenances/Details/{id_}")
        n = norm(t)
        faltan = []
        if desc and norm(desc)[:50] not in n:
            faltan.append("trabajo")
        if costo is not None and costo > 0 and not money_in(n, costo):
            faltan.append("costo")
        out.append(Result(f"Mantenimiento L-8 {id_}: trabajo/costo visibles", "completo", "completo" if not faltan else "faltan " + ",".join(faltan)))
    return out


def check_equipos(s):
    rows = db_rows("SELECT Id, Name, Brand, Model, UsefulLifeYears FROM Equipments WHERE Status <> 2")
    out = []
    for (id_, name, brand, model, vida) in random.sample(rows, 20):
        _, t = s.text(f"/Equipment/Details/{id_}")
        n = norm(t)
        faltan = [x for x, v in (("nombre", name), ("marca", brand), ("modelo", model)) if v and norm(v) not in n]
        out.append(Result(f"Equipo {id_}: nombre/marca/modelo visibles", "completo", "completo" if not faltan else "faltan " + ",".join(faltan)))
    return out


def check_verificaciones(s):
    """Observaciones de L-6 visibles en la pantalla de verificación del laboratorio/fecha."""
    rows = db_rows("SELECT v.Id, eu.LaboratoryId, CONVERT(varchar(10), v.[Date], 23), v.Observations, eu.InventoryNumber "
                   "FROM Verifications v JOIN EquipmentUnits eu ON eu.Id = v.EquipmentUnitId WHERE v.Observations IS NOT NULL")
    out = []
    for (id_, lab, fecha, obs, inv) in random.sample(rows, 15):
        _, t = s.text(f"/Verifications/Details?labId={lab}&date={fecha}")
        n = norm(t)
        faltan = []
        if str(inv) not in n:
            faltan.append("inventario")
        if norm(obs)[:40] not in n:
            faltan.append("observacion")
        out.append(Result(f"Verificación L-6 {id_} (lab {lab}, {fecha}): inventario/observación visibles", "completo",
                          "completo" if not faltan else "faltan " + ",".join(faltan)))
    return out


def check_equipos_ficha(s):
    rows = db_rows("SELECT Id, UsefulLifeYears, Description FROM Equipments WHERE Status <> 2")
    out = []
    for (id_, vida, desc) in random.sample(rows, 20):
        _, t = s.text(f"/Equipment/Details/{id_}")
        n = norm(t)
        faltan = []
        if vida is not None and str(vida) not in n:
            faltan.append("vida util")
        if desc and norm(desc)[:40] not in n:
            faltan.append("especificaciones")
        out.append(Result(f"Equipo {id_}: vida útil/especificaciones visibles", "completo", "completo" if not faltan else "faltan " + ",".join(faltan)))
    return out


def check_personas(s):
    rows = db_rows("SELECT p.Id, p.Title, p.Email, p.PhoneNumber, COALESCE(i.Name, e.Name) FROM People p "
                   "LEFT JOIN Interns i ON i.Id = p.Id LEFT JOIN Externs e ON e.Id = p.Id")
    out = []
    for (id_, titulo, email, tel, nombre) in random.sample(rows, 20):
        _, t = s.text(f"/Persons/Details?id={id_}")
        n = norm(t)
        faltan = [x for x, v in (("nombre", nombre), ("titulo", titulo), ("email", email), ("telefono", tel)) if v and norm(v) not in n]
        out.append(Result(f"Persona {id_}: nombre/título/email/teléfono visibles", "completo", "completo" if not faltan else "faltan " + ",".join(faltan)))
    return out


def check_planificaciones(s):
    rows = db_rows("SELECT Id, Code, Observations FROM Plannings WHERE IsDeleted = 0 AND Observations IS NOT NULL")
    out = []
    for (id_, code, obs) in random.sample(rows, 15):
        _, t = s.text(f"/LaboratoryOperations/Planificacion?id={id_}")
        n = norm(t)
        out.append(Result(f"Planificación {code}: observaciones visibles", "visible", "visible" if norm(obs)[:30] in n else "no visible"))
    return out


if __name__ == "__main__":
    s = Session()
    sys.exit(1 if run_all([partial(f, s) for f in (check_unidades, check_articulos, check_solicitudes, check_mantenimientos, check_equipos, check_verificaciones, check_equipos_ficha, check_personas, check_planificaciones)]) else 0)
