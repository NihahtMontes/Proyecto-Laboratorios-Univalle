"""Chequeos de las tres pantallas de solo lectura añadidas para que ningún dato importado quede sin UI."""
import re
import sys
from functools import partial

from e2e_lib import Result, Session, db_scalar, run_all


def check_unidades(s):
    esperado = db_scalar("SELECT COUNT(*) FROM EquipmentUnits WHERE CurrentStatus <> 99")
    ids = s.collect("/EquipmentUnits", r"/EquipmentUnits/Details\?id=(\d+)")
    _, t = s.text("/EquipmentUnits")
    m = re.search(r"(\d+) unidad\(es\)", t)
    return [Result("EquipmentUnits/Index: total visible vs BD", esperado, len(ids)),
            Result("EquipmentUnits/Index: contador 'unidad(es)'", esperado, int(m.group(1)) if m else -1),
            Result("EquipmentUnits/Index: salud", True, s.healthy("/EquipmentUnits"))]


def check_plan(s):
    esperado = db_scalar("SELECT COUNT(*) FROM MaintenancePlans")
    # filas de la tabla: cada plan muestra su código PLA-...
    codigos = s.collect("/Maintenances/Plan", r"(PLA-[A-Z0-9-]+)")
    _, t = s.text("/Maintenances/Plan")
    m = re.search(r"(\d+) plan\(es\)", t)
    return [Result("Maintenances/Plan (L-48): total visible vs BD", esperado, len(codigos)),
            Result("Maintenances/Plan (L-48): contador 'plan(es)'", esperado, int(m.group(1)) if m else -1),
            Result("Maintenances/Plan: salud", True, s.healthy("/Maintenances/Plan"))]


def check_programaciones(s):
    esperado = db_scalar("SELECT COUNT(*) FROM PracticeSchedules WHERE IsDeleted = 0")
    codigos = s.collect("/Schedules/Practices", r"((?:PROG|PRG)-[A-Z0-9-]+)")
    _, t = s.text("/Schedules/Practices")
    m = re.search(r"(\d+) programaci[oó]n\(es\)", t)
    return [Result("Schedules/Practices: total visible vs BD", esperado, len(codigos)),
            Result("Schedules/Practices: contador 'programación(es)'", esperado, int(m.group(1)) if m else -1),
            Result("Schedules/Practices: salud", True, s.healthy("/Schedules/Practices"))]


if __name__ == "__main__":
    s = Session()
    sys.exit(1 if run_all([partial(check_unidades, s), partial(check_plan, s), partial(check_programaciones, s)]) else 0)
