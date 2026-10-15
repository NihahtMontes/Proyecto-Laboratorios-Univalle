"""
Chequeos E2E (solo lectura) del grupo INVENTARIO: Equipment, EquipmentUnits/AssetView,
Laboratories, Cities/Countries, Persons y Users.

Compara lo visible en cada pantalla (recorriendo la paginación) con la BD REAL.
Uso: python Tools/E2E/checks_inventario.py  (requiere la app en E2E_BASE/localhost:5000
y la variable de entorno E2E_PW; el código de salida es 0 si todo pasa).

Notas de alcance (hallazgos del análisis de pantallas):
- /Equipment no filtra Status en su consulta propia: el filtro Status<>2 lo aplica el
  query filter global del DbContext (Equipment.Status != Eliminado), por eso el listado
  completo visible debe ser igual al conteo de la BD.
- EquipmentUnits no tiene página Index: las unidades se listan en /AssetView (contador
  "Unidades vigentes"), en la cascada AssetView (Units/UtensilUnits/OtherUnits/
  EnvironmentUnits) y dentro de /Equipment/Details. El query filter global excluye
  CurrentStatus = 99 (Eliminado).
- HALLAZGO: la cascada de /AssetView?ActiveTab=Inventario solo alcanza unidades de
  equipos con clasificación Confirmada (Equipos/Utensilios/Otros por clasificación, o
  por ambiente con LaboratoryId asignado). Las unidades de equipos con clasificación
  Pendiente/LegacyInferred y las unidades sin ambiente no se listan ahí; el total
  completo solo se expone como contador en /AssetView y como suma de existencias
  ("Und.") en /Equipment. Por eso el total visible de unidades se verifica con esos
  dos recorridos más un muestreo de /Equipment/Details.
- Cities/Index lista además todos los países en la pestaña #paises sin paginar (IList).
"""
import functools
import os
import re
import sys
import urllib.parse

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
from e2e_lib import Result, Session, db_rows, db_scalar, run_all

MAX_PAGES = 50  # tope de seguridad para los recorridos de paginación (20 por página)


def _contador(texto, etiqueta):
    """Lee el contador visible '<N> etiqueta' del texto colapsado del body."""
    m = re.search(r"(\d+)\s*" + re.escape(etiqueta), texto)
    return int(m.group(1)) if m else -1


def _buscar_en_listado(s, path_base, termino, max_paginas=3):
    """True si el término aparece en las primeras páginas del listado filtrado."""
    filtro = urllib.parse.quote(termino)
    for pagina in range(1, max_paginas + 1):
        status, texto = s.text(f"{path_base}?SearchTerm={filtro}&pageIndex={pagina}")
        if status != 200:
            return False
        if termino.lower() in texto.lower():
            return True
    return False


def check_equipment_total(s):
    """Recorre todas las páginas de /Equipment: enlaces a Details, contador y existencias."""
    ids, existencias, pagina = set(), 0, 1
    while pagina <= MAX_PAGES:
        status, html = s.get(f"/Equipment?pageIndex={pagina}")
        if status != 200:
            break
        nuevos = set(re.findall(r"/Equipment/Details/(\d+)", html)) - ids
        if not nuevos:
            break
        ids |= nuevos
        existencias += sum(int(n) for n in re.findall(r"(\d+)\s*Und\.", html))
        pagina += 1
    esperado_eq = db_scalar("SELECT COUNT(*) FROM Equipments WHERE Status <> 2")
    # Unidades que la app puede mostrar: unidas a un equipo no eliminado (ambos query filters)
    esperado_un = db_scalar(
        "SELECT COUNT(*) FROM EquipmentUnits u JOIN Equipments e ON u.EquipmentId = e.Id "
        "WHERE u.CurrentStatus <> 99 AND e.Status <> 2")
    _, texto = s.text("/Equipment")
    return [
        Result("Equipment: total visible vs BD", esperado_eq, len(ids)),
        Result("Equipment: contador 'activo(s)' vs BD", esperado_eq, _contador(texto, "activo(s)")),
        Result("EquipmentUnits: suma de existencias visibles en /Equipment vs BD",
               esperado_un, existencias),
    ]


def check_equipment_busqueda(s):
    """Verifica por SearchTerm que 3 equipos del catálogo (CatalogCode de la BD) aparecen.
    La pantalla busca por Name/Brand/Model y números de unidad, no por CatalogCode."""
    filas = db_rows(
        "SELECT TOP 3 CatalogCode, Name FROM Equipments "
        "WHERE Status <> 2 AND CatalogCode IS NOT NULL ORDER BY Id")
    resultados = []
    for codigo, nombre in filas:
        observado = _buscar_en_listado(s, "/Equipment", nombre)
        resultados.append(Result(f"Equipment: búsqueda muestra '{codigo} {nombre}'", True, observado))
    return resultados


def check_equipment_details_unidades(s):
    """En /Equipment/Details de un equipo con unidades, las unidades visibles = BD."""
    id_eq = db_scalar(
        "SELECT TOP 1 e.Id FROM Equipments e WHERE e.Status <> 2 AND EXISTS ("
        "SELECT 1 FROM EquipmentUnits u WHERE u.EquipmentId = e.Id AND u.CurrentStatus <> 99) "
        "ORDER BY e.Id")
    if id_eq is None:
        return Result("Equipment/Details: equipo con unidades", "hay datos", "sin datos en BD")
    esperado = db_scalar(
        "SELECT COUNT(*) FROM EquipmentUnits WHERE EquipmentId = ? AND CurrentStatus <> 99", id_eq)
    status, html = s.get(f"/Equipment/Details/{id_eq}")
    visibles = len(set(re.findall(r"EquipmentUnits/Details\?id=(\d+)", html))) if status == 200 else -1
    return Result(f"Equipment/Details/{id_eq}: unidades visibles vs BD", esperado, visibles)


def check_assetview_total(s):
    """/AssetView debe mostrar 'Unidades vigentes: N' igual al conteo de la BD."""
    esperado = db_scalar(
        "SELECT COUNT(*) FROM EquipmentUnits u JOIN Equipments e ON u.EquipmentId = e.Id "
        "WHERE u.CurrentStatus <> 99 AND e.Status <> 2")
    status, texto = s.text("/AssetView")
    m = re.search(r"Unidades vigentes:\s*(\d+)", texto)
    return Result("AssetView: texto 'Unidades vigentes' vs BD", esperado,
                  int(m.group(1)) if m else -1)


def check_unidades_details_kardex(s):
    """/EquipmentUnits/Details?id=N y /EquipmentUnits/Kardex/N sanos y con el inv. visible."""
    filas = db_rows(
        "SELECT TOP 3 Id, InventoryNumber FROM EquipmentUnits "
        "WHERE CurrentStatus <> 99 AND InventoryNumber IS NOT NULL ORDER BY Id")
    resultados = []
    for uid, inv in filas:
        ok_det = s.healthy(f"/EquipmentUnits/Details?id={uid}")
        _, texto_det = s.text(f"/EquipmentUnits/Details?id={uid}")
        resultados.append(Result(
            f"EquipmentUnits/Details?id={uid}: 200 sin excepción y con inventario",
            True, ok_det and inv in texto_det))
        ok_kdx = s.healthy(f"/EquipmentUnits/Kardex/{uid}")
        _, texto_kdx = s.text(f"/EquipmentUnits/Kardex/{uid}")
        resultados.append(Result(
            f"EquipmentUnits/Kardex/{uid}: 200 sin excepción y con inventario",
            True, ok_kdx and f"Inv: {inv}" in texto_kdx))
    return resultados


def check_laboratories(s):
    """/Laboratories combina pestañas: #laboratories, #faculties y #careers en la misma
    paginación (?pageIndex=N compartida); cada tab-pane se renderiza en todas las páginas."""
    labs = s.collect("/Laboratories", r"/Laboratories/Details/(\d+)")
    facs = s.collect("/Laboratories", r"/Faculties/Details/(\d+)")
    cars = s.collect("/Laboratories", r"/Career/Details\?id=(\d+)")
    _, texto = s.text("/Laboratories")
    return [
        Result("Laboratories: total visible vs BD",
               db_scalar("SELECT COUNT(*) FROM Laboratories WHERE Status <> 2"), len(labs)),
        Result("Laboratories: contador 'ambiente(s)' vs BD",
               db_scalar("SELECT COUNT(*) FROM Laboratories WHERE Status <> 2"),
               _contador(texto, "ambiente(s)")),
        Result("Laboratories (#faculties): facultades visibles vs BD",
               db_scalar("SELECT COUNT(*) FROM Faculties WHERE Status <> 2"), len(facs)),
        Result("Laboratories (#careers): carreras visibles vs BD",
               db_scalar("SELECT COUNT(*) FROM Careers WHERE Status <> 2"), len(cars)),
    ]


def check_cities_countries(s):
    """/Cities (ciudades paginadas + pestaña #paises sin paginar) y /Countries."""
    ciudades = s.collect("/Cities", r"/Cities/Details/(\d+)")
    _, texto_cities = s.text("/Cities")
    _, html_cities = s.get("/Cities")
    paises_en_cities = set(re.findall(r"/Countries/Details/(\d+)", html_cities))
    paises = s.collect("/Countries", r"/Countries/Details/(\d+)")
    _, texto_countries = s.text("/Countries")
    return [
        Result("Cities: total visible vs BD",
               db_scalar("SELECT COUNT(*) FROM Cities WHERE Status <> 2"), len(ciudades)),
        Result("Cities: contador 'ciudad(es)' vs BD",
               db_scalar("SELECT COUNT(*) FROM Cities WHERE Status <> 2"),
               _contador(texto_cities, "ciudad(es)")),
        Result("Cities (#paises): países visibles vs BD",
               db_scalar("SELECT COUNT(*) FROM Countries WHERE Status <> 2"), len(paises_en_cities)),
        Result("Countries: total visible vs BD",
               db_scalar("SELECT COUNT(*) FROM Countries WHERE Status <> 2"), len(paises)),
        Result("Countries: contador 'país(es)' vs BD",
               db_scalar("SELECT COUNT(*) FROM Countries WHERE Status <> 2"),
               _contador(texto_countries, "país(es)")),
    ]


def check_persons(s):
    """/Persons: listado paginado de People; los Details van como query (?id=N) porque
    la página no declara ruta {id:int}."""
    personas = s.collect("/Persons", r"/Persons/Details\?id=(\d+)")
    _, texto = s.text("/Persons")
    esperado = db_scalar("SELECT COUNT(*) FROM People WHERE Status <> 2")
    return [
        Result("Persons: total visible vs BD", esperado, len(personas)),
        Result("Persons: contador 'persona(s)' vs BD", esperado, _contador(texto, "persona(s)")),
    ]


def check_users(s):
    """/Users: pestaña #usuarios con las cuentas de acceso (esperado 1)."""
    usuarios = s.collect("/Users", r"/Users/Details/(\d+)")
    _, texto = s.text("/Users")
    esperado = db_scalar("SELECT COUNT(*) FROM Users WHERE Status <> 2")
    return [
        Result("Users: total visible vs BD", esperado, len(usuarios)),
        Result("Users: contador 'cuenta(s)' vs BD", esperado, _contador(texto, "cuenta(s)")),
    ]


def check_salud(s):
    """Session.healthy para cada listado y el Details de 1 registro real de la BD."""
    casos = [
        ("/Equipment", "Equipment"),
        ("/AssetView", "AssetView"),
        ("/AssetView?ActiveTab=Inventario", "AssetView (pestaña Inventario)"),
        ("/Laboratories", "Laboratories"),
        ("/Cities", "Cities"),
        ("/Countries", "Countries"),
        ("/Persons", "Persons"),
        ("/Users", "Users"),
    ]
    resultados = [Result(f"Salud {nombre}: {path}", True, s.healthy(path)) for path, nombre in casos]
    detalles = [
        (f"/Equipment/Details/{db_scalar('SELECT TOP 1 Id FROM Equipments WHERE Status <> 2 ORDER BY Id')}", "Equipment"),
        (f"/EquipmentUnits/Details?id={db_scalar('SELECT TOP 1 Id FROM EquipmentUnits WHERE CurrentStatus <> 99 ORDER BY Id')}", "EquipmentUnits"),
        (f"/EquipmentUnits/Kardex/{db_scalar('SELECT TOP 1 Id FROM EquipmentUnits WHERE CurrentStatus <> 99 ORDER BY Id')}", "EquipmentUnits (Kardex)"),
        (f"/Laboratories/Details/{db_scalar('SELECT TOP 1 Id FROM Laboratories WHERE Status <> 2 ORDER BY Id')}", "Laboratories"),
        (f"/Cities/Details/{db_scalar('SELECT TOP 1 Id FROM Cities WHERE Status <> 2 ORDER BY Id')}", "Cities"),
        (f"/Countries/Details/{db_scalar('SELECT TOP 1 Id FROM Countries WHERE Status <> 2 ORDER BY Id')}", "Countries"),
        (f"/Persons/Details?id={db_scalar('SELECT TOP 1 Id FROM People WHERE Status <> 2 ORDER BY Id')}", "Persons"),
        (f"/Users/Details/{db_scalar('SELECT TOP 1 Id FROM Users WHERE Status <> 2 ORDER BY Id')}", "Users"),
    ]
    resultados += [Result(f"Salud {nombre} (1 registro): {path}", True, s.healthy(path))
                   for path, nombre in detalles]
    return resultados


CHECKS = [
    check_equipment_total,
    check_equipment_busqueda,
    check_equipment_details_unidades,
    check_assetview_total,
    check_unidades_details_kardex,
    check_laboratories,
    check_cities_countries,
    check_persons,
    check_users,
    check_salud,
]


if __name__ == "__main__":
    s = Session()
    sys.exit(1 if run_all([functools.partial(check, s) for check in CHECKS]) else 0)
