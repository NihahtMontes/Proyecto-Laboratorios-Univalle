"""Genera fase_a_demo.sql a partir de BaseEstructuraFinal.xlsx (solo para DB_Laboratorios_Univalle_DEMO_QA)."""
import io
import os
import openpyxl

XLSX = r"C:\Users\monte\Downloads\BaseEstructuraFinal.xlsx"
OUT = os.path.join(os.environ.get("TEMP", "."), "mm", "fase_a_demo.sql")

wb = openpyxl.load_workbook(XLSX, read_only=True, data_only=True)


def rows(sheet):
    it = wb[sheet].iter_rows(values_only=True)
    header = next(it)
    for r in it:
        if any(v is not None for v in r):
            yield dict(zip(header, r))


def q(v):
    if v is None or (isinstance(v, str) and v.strip() == ""):
        return "NULL"
    return "N'" + str(v).strip().replace("'", "''") + "'"


sedes = list(rows("Sedes"))
labs = list(rows("Laboratorios"))
units = [(str(u["NumeroInventario"]).strip(), u["LaboratorioCodigo"]) for u in rows("UnidadesFisicas")]

# Códigos actuales de la BD que se renombran al código oficial del Excel (conservan su Id).
LAB_RENAMES = {"H-1": "H-01", "H-2": "H-02", "H-3": "H-03", "H-4": "H-04", "H-5": "H-05",
               "H-6": "H-06", "H-7": "H-07", "K-1": "K-01", "CIRCULACION": "CIRC-GEN"}

sql = []
w = sql.append
w("SET NOCOUNT ON;")
w("SET XACT_ABORT ON;")
w("IF DB_NAME() <> N'DB_Laboratorios_Univalle_DEMO_QA' THROW 50000, 'Destino incorrecto: solo DEMO_QA', 1;")
w("BEGIN TRANSACTION;")
w("DECLARE @Now datetime2 = SYSUTCDATETIME();")
w("DECLARE @Bolivia int = (SELECT TOP 1 Id FROM dbo.Countries WHERE Name = N'Bolivia');")
w("")
w("-- 1. Ciudades y sedes")
for s in sedes:
    w(f"IF NOT EXISTS (SELECT 1 FROM dbo.Cities WHERE Name = {q(s['Ciudad'])}) INSERT dbo.Cities(CountryId, Name, Status, CreatedDate) VALUES(@Bolivia, {q(s['Ciudad'])}, 0, @Now);")
    w(f"IF NOT EXISTS (SELECT 1 FROM dbo.Campuses WHERE Code = {q(s['SedeCodigo'])}) INSERT dbo.Campuses(Code, Name, CityId, Address, Status, CreatedDate) "
      f"VALUES({q(s['SedeCodigo'])}, {q(s['NombreSede'])}, (SELECT TOP 1 Id FROM dbo.Cities WHERE Name = {q(s['Ciudad'])}), {q(s['Direccion'])}, 0, @Now);")
w("DECLARE @CBBA int = (SELECT Id FROM dbo.Campuses WHERE Code = N'CBBA');")
w("")
w("-- 2. Facultades y carreras a los códigos oficiales del Excel (se conservan los Id)")
w("UPDATE dbo.Faculties SET Code = N'FGYT', Name = N'Facultad de Gastronomía y Turismo', LastModifiedDate = @Now WHERE Code = N'GASTRO';")
w("UPDATE dbo.Faculties SET Code = N'FCSN', Name = N'Facultad de Ciencias de la Salud - Carrera de Nutrición', LastModifiedDate = @Now WHERE Code = N'NUTRI';")
w("DECLARE @FGYT int = (SELECT Id FROM dbo.Faculties WHERE Code = N'FGYT');")
for old, new in [("CAR-0001", "LGS"), ("CAR-0002", "LTH"), ("CAR-0003", "LND"), ("CAR-0004", "LCG")]:
    w(f"UPDATE dbo.Careers SET Code = N'{new}' WHERE Code = N'{old}';")
w("-- LND (Nutrición) queda en FGYT según el Excel (advertencia documentada en el informe).")
w("UPDATE dbo.Careers SET FacultadId = @FGYT WHERE Code = N'LND';")
w("")
w("-- 3. Laboratorios: renombrar códigos, crear faltantes y completar datos del Excel")
for old, new in LAB_RENAMES.items():
    w(f"UPDATE dbo.Laboratories SET Code = N'{new}', LastModifiedDate = @Now WHERE Code = N'{old}' AND NOT EXISTS (SELECT 1 FROM dbo.Laboratories x WHERE x.Code = N'{new}');")
for lab in labs:
    code = q(lab["LaboratorioCodigo"])
    floor = None if str(lab["Piso"]).strip().lower() == "por confirmar" else lab["Piso"]
    w(f"IF NOT EXISTS (SELECT 1 FROM dbo.Laboratories WHERE Code = {code}) "
      f"INSERT dbo.Laboratories(FacultyId, Code, Name, Type, Building, Block, Floor, Status, CreatedDate, CampusId) "
      f"VALUES(@FGYT, {code}, {q(lab['Nombre'])}, {q(lab['Tipo'])}, {q(lab['Edificio'])}, {q(lab['Bloque'])}, {q(floor)}, 0, @Now, @CBBA);")
    w(f"UPDATE dbo.Laboratories SET Name = {q(lab['Nombre'])}, Type = {q(lab['Tipo'])}, Building = {q(lab['Edificio'])}, "
      f"Block = {q(lab['Bloque'])}, Floor = {q(floor)}, CampusId = @CBBA, FacultyId = @FGYT, Status = 0, LastModifiedDate = @Now WHERE Code = {code};")
w("")
w("-- 4. Gestión 2027-1 (necesaria para la planificación)")
w("IF NOT EXISTS (SELECT 1 FROM dbo.Managements WHERE Code = N'2027-1') "
  "INSERT dbo.Managements(Year, Semester, Code, Description, StartDate, PlannedEndDate, Status, Type, CreatedDate) "
  "VALUES(2027, 1, N'2027-1', N'Gestión 2027-1 (base estructural del cliente)', '2027-02-01', '2027-06-30', 1, 0, @Now);")
w("")
w("-- 5. Laboratorio real de cada unidad según el Excel (reemplaza la inferencia [INFERIDO-DEMO])")
w("CREATE TABLE #ExcelUnit(InventoryNumber nvarchar(50) NOT NULL PRIMARY KEY, LabCode nvarchar(50) NOT NULL);")
for i in range(0, len(units), 100):
    chunk = units[i:i + 100]
    w("INSERT #ExcelUnit(InventoryNumber, LabCode) VALUES " + ", ".join(f"({q(inv)}, {q(lab)})" for inv, lab in chunk) + ";")
w("DECLARE @n int = 1;")
w("WHILE @n > 0")
w("BEGIN")
w("    UPDATE TOP (100) u SET LaboratoryId = l.Id, CareerId = NULL, LocationResolutionStatus = 1,")
w("        Notes = N'[EXCEL] Laboratorio ' + e.LabCode + N' según BaseEstructuraFinal.xlsx (EstadoUbicacion en origen: Pendiente).',")
w("        LastModifiedDate = @Now")
w("    FROM dbo.EquipmentUnits u")
w("    JOIN #ExcelUnit e ON e.InventoryNumber = u.InventoryNumber")
w("    JOIN dbo.Laboratories l ON l.Code = e.LabCode")
w("    WHERE u.Notes IS NULL OR u.Notes NOT LIKE N'[[]EXCEL]%';")
w("    SET @n = @@ROWCOUNT;")
w("END;")
w("")
w("-- 6. Laboratorios que no existen en el Excel y quedaron sin unidades: inactivar (sin borrar)")
w("UPDATE dbo.Laboratories SET Status = 1, LastModifiedDate = @Now")
w("WHERE Status = 0 AND Code IN (N'OFICINAS 1ER PISO', N'OFICINAS PLANTA BAJA')")
w("  AND NOT EXISTS (SELECT 1 FROM dbo.EquipmentUnits u WHERE u.LaboratoryId = dbo.Laboratories.Id);")
w("")
w("COMMIT TRANSACTION;")
w("")
w("-- Verificación")
w("SELECT 'sedes' k, COUNT(*) n FROM dbo.Campuses UNION ALL SELECT 'labs activos', COUNT(*) FROM dbo.Laboratories WHERE Status = 0")
w("UNION ALL SELECT 'labs del Excel presentes', COUNT(*) FROM dbo.Laboratories l WHERE l.Status = 0 AND l.CampusId IS NOT NULL")
w("UNION ALL SELECT 'unidades con lab Excel', COUNT(*) FROM dbo.EquipmentUnits WHERE Notes LIKE N'[[]EXCEL]%'")
w("UNION ALL SELECT 'unidades aun inferidas', COUNT(*) FROM dbo.EquipmentUnits WHERE Notes LIKE N'[[]INFERIDO-DEMO]%'")
w("UNION ALL SELECT 'gestion 2027-1', COUNT(*) FROM dbo.Managements WHERE Code = N'2027-1';")
w("SELECT InventoryNumber AS SoloEnBD FROM dbo.EquipmentUnits WHERE Notes LIKE N'[[]INFERIDO-DEMO]%' ORDER BY InventoryNumber;")

io.open(OUT, "w", encoding="utf-8").write("\n".join(sql) + "\n")
print(OUT, "lineas:", len(sql), "unidades excel:", len(units), "labs:", len(labs))
