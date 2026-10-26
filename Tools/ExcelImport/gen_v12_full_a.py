"""
Importador único V12, parte A: Sedes, Facultades, Carreras, Laboratorios, Gestiones, Actores, ActorRoles,
CatalogoEquipos y UnidadesFisicas -> DB_Laboratorios_Univalle_REAL (BD vacía).

Textos tal cual del Excel (solo se recortan los bordes). Cada fila deja su copia completa en ImportSourceRows.
Uso:  python gen_v12_full_a.py [--dry-run]   ->  %TEMP%\\v12full\\parte_a.sql
"""
import sys
from collections import Counter, defaultdict

from v12_common import (BATCH_CODE, SqlBuilder, check_len, check_sha, q, read_sheets, text)

SHEETS = ["Sedes", "Facultades", "Carreras", "Laboratorios", "Gestiones", "Actores", "ActorRoles",
          "CatalogoEquipos", "UnidadesFisicas"]

ROLE_MAP = {"técnico": 1, "proveedor": 3, "solicitante": 5, "responsable": 6, "verificador": 7, "docente": 8}
PHYSICAL = {"excelente": 5, "bueno": 4, "regular": 3, "malo": 2, "baja del equipo": 1, "por confirmar": 9}
OPERATIVE = {"operativo": 0, "de baja": 3, "por confirmar": 98}
CATEGORY_BY_ROLE = {8: 2, 1: 1}  # Docente -> 2, Técnico -> 1


def low(v):
    return (text(v) or "").casefold()


def general_status(v):
    return 0 if low(v) in ("activo", "") else 1


def pending(v):
    return low(v) in ("por confirmar", "sin especificar")


def main(dry_run):
    sha = check_sha()
    data = read_sheets(SHEETS)
    for name, (_, rows) in data.items():
        print(f"[hoja] {name}: {len(rows)} filas")
    if dry_run:
        return

    s = SqlBuilder()
    s.prologue(sha, "Importación completa V12 (parte A: institucional, actores, catálogo y unidades).")
    sw = s.stmt

    # ---- Ciudades (tabla vacía): una por valor distinto de Sedes.Ciudad; requiere un país ----
    hdr, rows = data["Sedes"]
    s.w("-- Países y ciudades")
    s.begin_group()
    sw("IF NOT EXISTS (SELECT 1 FROM dbo.Countries WHERE Name = N'Bolivia') "
       "INSERT dbo.Countries(Name, Status, CreatedDate) VALUES(N'Bolivia', 0, @Now);")
    for city in sorted({text(r["Ciudad"]) for r in rows if text(r["Ciudad"])}):
        sw(f"IF NOT EXISTS (SELECT 1 FROM dbo.Cities WHERE Name = {q(city)}) "
           f"INSERT dbo.Cities(CountryId, Name, Status, CreatedDate) "
           f"VALUES((SELECT Id FROM dbo.Countries WHERE Name = N'Bolivia'), {q(city)}, 0, @Now);")
    s.end_group()
    s.issue("ASSUMED_VALUE", "Country", "Bolivia", "Pais", None, "Bolivia",
            "Cities.CountryId es obligatorio y el Excel no trae país; se creó 'Bolivia' (sedes en LPZ/CBBA/SCZ).", 1, "Sedes", None)

    # ---- Sedes ----
    s.w("-- Sedes")
    s.begin_group()
    for r in rows:
        c = r["Sedecodigo"]
        check_len("Sedes", r, "Sedecodigo", c, 20)
        sw(f"IF NOT EXISTS (SELECT 1 FROM dbo.Campuses WHERE Code = {q(c)}) "
           f"INSERT dbo.Campuses(Code, Name, CityId, Address, Status, CreatedDate) "
           f"VALUES({q(c)}, {q(r['Nombresede'])}, (SELECT Id FROM dbo.Cities WHERE Name = {q(r['Ciudad'])}), "
           f"{q(r['Direccion'])}, {general_status(r['Estado'])}, @Now);")
        s.source_row("Sedes", hdr, r, c, "Campus", c)
    s.end_group()

    # ---- Facultades ----
    hdr, rows = data["Facultades"]
    s.w("-- Facultades")
    s.begin_group()
    for r in rows:
        c = r["Facultadcodigo"]
        sw(f"IF NOT EXISTS (SELECT 1 FROM dbo.Faculties WHERE Code = {q(c)}) "
           f"INSERT dbo.Faculties(Code, Name, Description, Status, CreatedDate) "
           f"VALUES({q(c)}, {q(r['Nombre'])}, {q(r['Descripcion'])}, {general_status(r['Estado'])}, @Now);")
        s.source_row("Facultades", hdr, r, c, "Faculty", c)
    s.end_group()

    # ---- Carreras ----
    hdr, rows = data["Carreras"]
    s.w("-- Carreras")
    s.begin_group()
    for r in rows:
        c = r["Carreracodigo"]
        sw(f"IF NOT EXISTS (SELECT 1 FROM dbo.Careers WHERE Code = {q(c)}) "
           f"INSERT dbo.Careers(Code, Name, FacultadId, Status, CreatedDate) "
           f"VALUES({q(c)}, {q(r['Nombre'])}, (SELECT Id FROM dbo.Faculties WHERE Code = {q(r['Facultadcodigo'])}), "
           f"{general_status(r['Estado'])}, @Now);")
        s.source_row("Carreras", hdr, r, c, "Career", c)
    s.end_group()

    # ---- Laboratorios ----
    hdr, rows = data["Laboratorios"]
    s.w("-- Laboratorios")
    s.begin_group()
    for r in rows:
        c = r["Laboratoriocodigo"]
        check_len("Laboratorios", r, "Laboratoriocodigo", c, 20)
        floor = None if pending(r["Piso"]) else text(r["Piso"])
        sw(f"IF NOT EXISTS (SELECT 1 FROM dbo.Laboratories WHERE Code = {q(c)}) "
           f"INSERT dbo.Laboratories(Code, Name, FacultyId, CampusId, CityId, Type, Building, Block, Floor, Status, CreatedDate) "
           f"VALUES({q(c)}, {q(r['Nombre'])}, (SELECT Id FROM dbo.Faculties WHERE Code = {q(r['Facultadcodigo'])}), "
           f"(SELECT Id FROM dbo.Campuses WHERE Code = {q(r['Sedecodigo'])}), "
           f"(SELECT CityId FROM dbo.Campuses WHERE Code = {q(r['Sedecodigo'])}), "
           f"{q(r['Tipo'])}, {q(r['Edificio'])}, {q(r['Bloque'])}, {q(floor)}, {general_status(r['Estado'])}, @Now);")
        s.source_row("Laboratorios", hdr, r, c, "Laboratory", c)
        if pending(r["Piso"]):
            s.issue("VALUE_PENDING_CONFIRMATION", "Laboratory", c, "Piso", text(r["Piso"]), None,
                    "Piso 'Por confirmar' en la fuente: Floor queda NULL.", 1, "Laboratorios", r)
    s.end_group()

    # ---- Gestiones (incluye 1900-1) ----
    hdr, rows = data["Gestiones"]
    s.w("-- Gestiones")
    s.begin_group()
    open_rows = [r for r in rows if low(r["Estado"]) == "abierta"]
    if len(open_rows) > 1:
        sys.exit(f"Más de una gestión Abierta: {[r['Gestioncodigo'] for r in open_rows]}")
    for r in rows:
        c = r["Gestioncodigo"]
        status = 0 if low(r["Estado"]) == "abierta" else 2
        mtype = 9 if pending(r["Tipogestion"]) else None
        if mtype is None:
            sys.exit(f"Tipogestion no mapeado: {r['Tipogestion']}")
        sw(f"IF NOT EXISTS (SELECT 1 FROM dbo.Managements WHERE Code = {q(c)}) "
           f"INSERT dbo.Managements(Code, Year, Semester, Type, StartDate, PlannedEndDate, Status, CreatedDate) "
           f"VALUES({q(c)}, {r['Anio']}, {r['Semestre']}, {mtype}, {q(r['Fechainicio'])}, {q(r['Fechafin'])}, {status}, @Now);")
        s.source_row("Gestiones", hdr, r, c, "Management", c)
        if pending(r["Tipogestion"]):
            s.issue("VALUE_PENDING_CONFIRMATION", "Management", c, "Tipogestion", text(r["Tipogestion"]), "PorConfirmar",
                    "Tipo de gestión 'Por confirmar' en la fuente.", 0, "Gestiones", r)
        if int(r["Anio"]) < 2000:
            s.issue("INVALID_MANAGEMENT", "Management", c, "Anio", str(r["Anio"]), None,
                    "Año 1900 en la fuente (gestión usada por dos mantenimientos L-8): se carga tal cual como gestión histórica.",
                    1, "Gestiones", r)
    s.end_group()

    # ---- Actores (People + Interns/Externs + alias) y roles ----
    hdr_a, actors = data["Actores"]
    hdr_r, roles = data["ActorRoles"]
    roles_by_actor = defaultdict(set)
    for r in roles:
        role = ROLE_MAP.get(low(r["Rol"]))
        if role is None:
            sys.exit(f"Rol desconocido: {r['Rol']}")
        roles_by_actor[r["Actorcodigo"]].add(role)
    s.w("-- Actores")
    s.begin_group()
    for a in actors:
        c = a["Actorcodigo"]
        is_extern = low(a["Tipoactor"]) == "organización" or low(a["Vinculacion"]) == "externo"
        is_entity = low(a["Tipoactor"]) == "organización"
        rset = roles_by_actor.get(c, set())
        if is_extern:
            category = 5
        elif 8 in rset:
            category = 2
        elif 1 in rset:
            category = 1
        else:
            category = 99
        name = text(a["Nombrerazonsocial"])
        if not name:
            sys.exit(f"Actor {c} sin nombre")
        check_len("Actores", a, "Nombrerazonsocial", name, 200 if is_extern else 100)
        check_len("Actores", a, "Telefono", a["Telefono"], 20)
        check_len("Actores", a, "Email", a["Email"], 100)
        status = general_status(a["Estado"])
        pid = f"(SELECT Id FROM dbo.People WHERE ActorCode = {q(c)})"
        sw(f"IF NOT EXISTS (SELECT 1 FROM dbo.People WHERE ActorCode = {q(c)}) "
           f"INSERT dbo.People(ActorCode, Status, Category, Email, PhoneNumber, Title, CreatedDate, ImportBatchId) "
           f"VALUES({q(c)}, {status}, {category}, {q(a['Email'])}, {q(a['Telefono'])}, {q(a['Titulo'])}, @Now, @Batch);")
        if is_extern:
            sw(f"IF NOT EXISTS (SELECT 1 FROM dbo.Externs WHERE Id = {pid}) "
               f"INSERT dbo.Externs(Id, IsEntity, Name, Address, ExternStatus) "
               f"VALUES({pid}, {1 if is_entity else 0}, {q(name)}, {q(a['Direccion']) if text(a['Direccion']) else 'N' + chr(39) * 2}, {status});")
        else:
            sw(f"IF NOT EXISTS (SELECT 1 FROM dbo.Interns WHERE Id = {pid}) "
               f"INSERT dbo.Interns(Id, Name, InternStatus) VALUES({pid}, {q(name)}, {status});")
        normalized = " ".join(name.upper().split())
        sw(f"IF NOT EXISTS (SELECT 1 FROM dbo.PersonAliases WHERE PersonId = {pid} AND NormalizedAlias = {q(normalized)}) "
           f"INSERT dbo.PersonAliases(PersonId, Alias, NormalizedAlias, IsPreferred, Source) "
           f"VALUES({pid}, {q(name)}, {q(normalized)}, 1, N'BaseGastronomia_v1.xlsx');")
        s.source_row("Actores", hdr_a, a, c, "Person", c)
        if pending(a["Tipoactor"]):
            s.issue("VALUE_PENDING_CONFIRMATION", "Person", c, "Tipoactor", text(a["Tipoactor"]), "Intern",
                    "Tipo de actor 'Por confirmar': se carga como interno.", 1, "Actores", a)
        if pending(a["Vinculacion"]) and not is_extern:
            s.issue("ACTOR_LINK_PENDING", "Person", c, "Vinculacion", text(a["Vinculacion"]), "Intern",
                    "Vinculación 'Por confirmar': se carga como interno.", 1, "Actores", a)
        if text(a["Direccion"]) and not is_extern:
            s.issue("VALUE_STORED_RAW_ONLY", "Person", c, "Direccion", text(a["Direccion"]), None,
                    "Los internos no tienen columna de dirección: el valor queda en OriginalDataJson.", 0, "Actores", a)
    s.end_group()
    s.w("-- Roles de actores")
    s.begin_group()
    for r in roles:
        c = r["Actorrolcodigo"]
        role = ROLE_MAP[low(r["Rol"])]
        active = 1 if low(r["Estado"]) == "activo" else 0
        pid = f"(SELECT Id FROM dbo.People WHERE ActorCode = {q(r['Actorcodigo'])})"
        sw(f"IF NOT EXISTS (SELECT 1 FROM dbo.PersonRoleAssignments WHERE PersonId = {pid} AND Role = {role}) "
           f"INSERT dbo.PersonRoleAssignments(PersonId, Role, IsActive, ValidFrom) VALUES({pid}, {role}, {active}, @Now);")
        s.source_row("ActorRoles", hdr_r, r, c, "PersonRoleAssignment", f"{r['Actorcodigo']}|{role}")
    s.end_group()

    # ---- Catálogo de equipos ----
    hdr, rows = data["CatalogoEquipos"]
    s.w("-- Catálogo de equipos")
    s.begin_group()
    for r in rows:
        c = r["Catalogocodigo"]
        check_len("CatalogoEquipos", r, "Catalogocodigo", c, 30)
        check_len("CatalogoEquipos", r, "Nombregenerico", r["Nombregenerico"], 200)
        check_len("CatalogoEquipos", r, "Marca", r["Marca"], 100)
        check_len("CatalogoEquipos", r, "Modelo", r["Modelo"], 100)
        check_len("CatalogoEquipos", r, "Especificaciones", r["Especificaciones"], 2000)
        category = 0  # Equipo; 'Por confirmar' también queda en 0 (columna obligatoria) con revisión pendiente
        utensil = 0 if low(r["Tipoutensilio"]) == "no aplica" else None
        sw(f"IF NOT EXISTS (SELECT 1 FROM dbo.Equipments WHERE CatalogCode = {q(c)}) "
           f"INSERT dbo.Equipments(CatalogCode, Name, Category, UtensilType, TypeClassification, Status, Brand, Model, "
           f"UsefulLifeYears, Description, ClassificationReviewStatus, CreatedDate, ImportBatchId) "
           f"VALUES({q(c)}, {q(r['Nombregenerico'])}, {category}, {q(utensil)}, NULL, 0, {q(r['Marca'])}, {q(r['Modelo'])}, "
           f"{q(r['Vidautilanios'])}, {q(r['Especificaciones'])}, 1, @Now, @Batch);")
        s.source_row("CatalogoEquipos", hdr, r, c, "Equipment", c)
        for col, val in (("Categoria", r["Categoria"]), ("Clasificacion", r["Clasificacion"]), ("Estado", r["Estado"])):
            if pending(val):
                s.issue("VALUE_PENDING_CONFIRMATION", "Equipment", c, col, text(val), None,
                        f"{col} 'Por confirmar' en la fuente: se carga con revisión de clasificación pendiente.", 1,
                        "CatalogoEquipos", r)
        if utensil is None and text(r["Tipoutensilio"]):
            s.issue("VALUE_NOT_MAPPED", "Equipment", c, "Tipoutensilio", text(r["Tipoutensilio"]), None,
                    "Tipo de utensilio sin equivalente en el enum: UtensilType queda NULL; el texto está en OriginalDataJson.",
                    1, "CatalogoEquipos", r)
    s.end_group()

    # ---- Unidades físicas ----
    hdr, rows = data["UnidadesFisicas"]
    s.w("-- Unidades físicas")
    s.begin_group()
    for r in rows:
        inv = str(int(r["Numeroinventario"]))
        status = OPERATIVE.get(low(r["Estadooperativo"]))
        cond = PHYSICAL.get(low(r["Condicionfisica"]))
        if status is None or cond is None:
            sys.exit(f"Unidad {inv}: estado/condición no mapeado ({r['Estadooperativo']}, {r['Condicionfisica']})")
        lab = text(r["Laboratoriocodigo"])
        check_len("UnidadesFisicas", r, "Numeroserie", r["Numeroserie"], 100)
        location = 1 if lab else 0
        sw(f"IF NOT EXISTS (SELECT 1 FROM dbo.EquipmentUnits WHERE InventoryNumber = {q(inv)}) "
           f"INSERT dbo.EquipmentUnits(InventoryNumber, EquipmentId, LaboratoryId, SerialNumber, InternalLocation, "
           f"AcquisitionDate, ManufacturingDate, AcquisitionValue, CurrentStatus, PhysicalCondition, Notes, "
           f"LocationResolutionStatus, CreatedDate, ImportBatchId) "
           f"VALUES({q(inv)}, (SELECT Id FROM dbo.Equipments WHERE CatalogCode = {q(r['Catalogocodigo'])}), "
           f"(SELECT Id FROM dbo.Laboratories WHERE Code = {q(lab)}), {q(r['Numeroserie'])}, {q(r['Ubicacioninterna'])}, "
           f"{q(r['Fechaadquisicion'])}, {q(r['Fechafabricacion'])}, {q(r['Valoradquisicionbs'])}, {status}, {cond}, "
           f"{q(r['Observaciones'])}, {location}, @Now, @Batch);")
        s.source_row("UnidadesFisicas", hdr, r, inv, "EquipmentUnit", inv)
        for col, val in (("Estadooperativo", r["Estadooperativo"]), ("Condicionfisica", r["Condicionfisica"])):
            if pending(val):
                s.issue("VALUE_PENDING_CONFIRMATION", "EquipmentUnit", inv, col, text(val), "PorConfirmar",
                        f"{col} 'Por confirmar' en la fuente.", 1, "UnidadesFisicas", r)
    s.end_group()

    # ---- Resumen en la propia ejecución ----
    s.w("""SELECT 'campuses' k, COUNT(*) n FROM dbo.Campuses UNION ALL SELECT 'faculties', COUNT(*) FROM dbo.Faculties
UNION ALL SELECT 'careers', COUNT(*) FROM dbo.Careers UNION ALL SELECT 'laboratories', COUNT(*) FROM dbo.Laboratories
UNION ALL SELECT 'managements', COUNT(*) FROM dbo.Managements UNION ALL SELECT 'people', COUNT(*) FROM dbo.People
UNION ALL SELECT 'roles', COUNT(*) FROM dbo.PersonRoleAssignments UNION ALL SELECT 'equipments', COUNT(*) FROM dbo.Equipments
UNION ALL SELECT 'units', COUNT(*) FROM dbo.EquipmentUnits
UNION ALL SELECT 'source_rows', COUNT(*) FROM dbo.ImportSourceRows WHERE ImportBatchId = @Batch
UNION ALL SELECT 'issues', COUNT(*) FROM dbo.DataQualityIssues WHERE ImportBatchId = @Batch;""")
    path = s.write("parte_a.sql")
    print(f"[ok] {path}: {len(s.lines)} líneas, source_rows={s.source_row_count}, issues={s.issue_count}")
    cnt = Counter(l.split("(")[0][:40] for l in s.lines if l.startswith("IF NOT EXISTS"))
    print(f"[ok] sentencias IF NOT EXISTS: {sum(cnt.values())}")


if __name__ == "__main__":
    main("--dry-run" in sys.argv)
