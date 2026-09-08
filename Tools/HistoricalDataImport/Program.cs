using System.Globalization;
using System.Text;
using OfficeOpenXml;

ExcelPackage.LicenseContext = LicenseContext.NonCommercial;
return HistoricalImportApplication.Run(args);

internal sealed class HistoricalDataAnalyzer
{
    private const string OfficialGastronomyFaculty = "Facultad de Gastronomia y Turismo - Carrera de Gastronom\u00eda";
    private readonly string _workbookPath;
    private readonly AnalysisResult _result = new();
    private readonly Dictionary<string, EquipmentRecord> _equipmentByKey = new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<string, UnitRecord> _unitsByInventory = new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<string, PersonRecord> _peopleByName = new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<string, ManagementRecord> _managementByCode = new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<string, RequestRecord> _requestsByKey = new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<string, MaintenanceRecord> _maintenanceByKey = new(StringComparer.OrdinalIgnoreCase);

    private ExcelWorksheet _faculties = null!;
    private ExcelWorksheet _careers = null!;
    private ExcelWorksheet _catalogs = null!;
    private ExcelWorksheet _laboratories = null!;
    private ExcelWorksheet? _inventory;
    private ExcelWorksheet _verifications = null!;
    private ExcelWorksheet _requests = null!;
    private ExcelWorksheet _kardex = null!;
    private ExcelWorksheet _costs = null!;
    private ExcelWorksheet _departures = null!;
    private ExcelWorksheet _plans = null!;
    private ExcelWorksheet _technicians = null!;

    public HistoricalDataAnalyzer(string workbookPath)
    {
        _workbookPath = workbookPath;
    }

    public AnalysisResult Analyze()
    {
        using var package = new ExcelPackage(new FileInfo(_workbookPath));
        BindWorksheets(package.Workbook);

        ReadFaculties();
        ReadCareers();
        ReadLaboratories();
        ReadPeople();
        ReadManagements();
        ReadCatalogsAndUnits();
        ReadInventoryOverrides();
        RecordPendingLocations();
        ReadVerifications();
        ReadRequests();
        ReadKardex();
        ReadCosts();
        ReadDepartures();
        ReadPlans();
        AddKnownFindings();

        _result.Equipments.AddRange(_equipmentByKey.Values.OrderBy(e => e.Key));
        _result.Units.AddRange(_unitsByInventory.Values.OrderBy(u => NaturalSortKey(u.InventoryNumber)));
        _result.People.AddRange(_peopleByName.Values.OrderBy(p => p.Name));
        _result.Managements.AddRange(_managementByCode.Values.OrderBy(m => m.Year).ThenBy(m => m.Semester));
        _result.Requests.AddRange(_requestsByKey.Values.OrderBy(r => r.Key));
        _result.Maintenances.AddRange(_maintenanceByKey.Values.OrderBy(m => m.Key));
        return _result;
    }

    private void BindWorksheets(ExcelWorkbook workbook)
    {
        _faculties = workbook.Worksheets["0 \u00b7 Facultades"] ?? workbook.Worksheets[0];
        _careers = workbook.Worksheets["0b \u00b7 Carreras"] ?? workbook.Worksheets[2];
        _catalogs = workbook.Worksheets["1 \u00b7 Cat\u00e1logos"] ?? workbook.Worksheets[3];
        _laboratories = workbook.Worksheets["0c \u00b7 Laboratorios"] ?? workbook.Worksheets[4];
        // Optional sheet: the real template keeps physical units only in "Catalogos".
        _inventory = workbook.Worksheets["2 \u00b7 IInventario"]
            ?? workbook.Worksheets["2 \u00b7 Inventario"]
            ?? workbook.Worksheets["Inventario"]
            ?? workbook.Worksheets.FirstOrDefault(w => w.Name.Contains("Inventario", StringComparison.OrdinalIgnoreCase));
        _verifications = workbook.Worksheets["3 \u00b7 Verificaciones"] ?? workbook.Worksheets[7];
        _requests = workbook.Worksheets["4 \u00b7 Solicitudes"] ?? workbook.Worksheets[8];
        _kardex = workbook.Worksheets["5 \u00b7 Kardex Mantenimiento"] ?? workbook.Worksheets[9];
        _costs = workbook.Worksheets["6 \u00b7 Detalles de Costo"] ?? workbook.Worksheets[10];
        _departures = workbook.Worksheets["7 \u00b7 Salidas (L-3)"] ?? workbook.Worksheets[11];
        _plans = workbook.Worksheets["8 \u00b7 Plan de Mantenimiento"] ?? workbook.Worksheets[12];
        _technicians = workbook.Worksheets["9 \u00b7 T\u00e9cnicos y Proveedores"] ?? workbook.Worksheets[13];

        foreach (var ws in workbook.Worksheets)
        {
            _result.SheetSummaries.Add(new SheetSummary(ws.Name, CountDataRows(ws), ws.Dimension?.End.Row ?? 0, ws.Dimension?.End.Column ?? 0));
        }
    }

    private void ReadFaculties()
    {
        foreach (var row in DataRows(_faculties, 3))
        {
            var name = NormalizeFaculty(Text(row, 1));
            if (string.IsNullOrWhiteSpace(name))
            {
                Reject(_faculties, row, "Faculty name is empty.");
                continue;
            }

            AddFaculty(name, Text(row, 2), Text(row, 3), _faculties.Name, row);
        }

        AddFaculty(OfficialGastronomyFaculty, "GASTRO", "Facultad oficial normalizada para carga historica.", "system", 0);
    }

    private void ReadCareers()
    {
        foreach (var row in DataRows(_careers, 3))
        {
            var name = Text(row, 1);
            if (string.IsNullOrWhiteSpace(name))
            {
                Reject(_careers, row, "Career name is empty.");
                continue;
            }

            var faculty = NormalizeFaculty(Text(row, 2));
            if (string.IsNullOrWhiteSpace(faculty))
            {
                Issue("WARN", _careers, row, "B", "Career without faculty reference. It will be inserted without FacultadId.");
            }
            else
            {
                AddFaculty(faculty, null, null, _careers.Name, row);
            }

            _result.Careers.Add(new CareerRecord(name, faculty, MapGeneralStatus(Text(row, 3)), _careers.Name, row));
        }
    }

    private void ReadLaboratories()
    {
        foreach (var row in DataRows(_laboratories, 6))
        {
            var code = Text(row, 1);
            var name = Text(row, 2);
            if (string.IsNullOrWhiteSpace(code) || string.IsNullOrWhiteSpace(name))
            {
                Reject(_laboratories, row, "Laboratory code/name is empty.");
                continue;
            }

            var faculty = NormalizeFaculty(Text(row, 3));
            if (string.IsNullOrWhiteSpace(faculty))
            {
                faculty = OfficialGastronomyFaculty;
                Issue("WARN", _laboratories, row, "C", "Laboratory without faculty. Official gastronomy faculty will be used.");
            }

            AddFaculty(faculty, null, null, _laboratories.Name, row);
            _result.Laboratories.Add(new LaboratoryRecord(code, name, faculty, Text(row, 5), JoinText(Text(row, 4), Text(row, 6)), _laboratories.Name, row));
        }
    }

    private void ReadPeople()
    {
        foreach (var row in DataRows(_technicians, 6))
        {
            var name = Text(row, 1);
            if (string.IsNullOrWhiteSpace(name))
            {
                Reject(_technicians, row, "Person/provider name is empty.");
                continue;
            }

            var type = Text(row, 2);
            var isCompany = IsYes(Text(row, 3));
            var category = type.Equals("Interno", StringComparison.OrdinalIgnoreCase) ? 1 : 5;
            AddPerson(name, type, isCompany, category, Text(row, 4), Text(row, 5), Text(row, 6), _technicians.Name, row, warnIfAutoCreated: false);
        }
    }

    private void ReadManagements()
    {
        foreach (var row in DataRows(_plans, 9))
        {
            if (!TryResolvePlanManagement(row, out var year, out var semester, out var code))
            {
                Issue("ERROR", _plans, row, "A", $"Cannot resolve management (Gestion='{Text(row, 1)}', Fecha='{Text(row, 8)}').");
                continue;
            }

            AddManagement(year, semester, code, row);
        }

        if (_managementByCode.Count == 0)
        {
            AddManagement(DateTime.Now.Year, 1, $"{DateTime.Now.Year}-1", 0);
        }
    }

    private void ReadCatalogsAndUnits()
    {
        var seenInventory = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);

        foreach (var row in DataRows(_catalogs, 11))
        {
            var name = Text(row, 1);
            var inventory = NormalizeInventory(Text(row, 2));
            if (string.IsNullOrWhiteSpace(name) || string.IsNullOrWhiteSpace(inventory))
            {
                Reject(_catalogs, row, "Catalog row lacks asset name or inventory number.");
                continue;
            }

            if (seenInventory.TryGetValue(inventory, out var firstRow))
            {
                Issue("ERROR", _catalogs, row, "B", $"Duplicate inventory '{inventory}'. First occurrence is row {firstRow}.");
                Reject(_catalogs, row, $"Duplicate inventory '{inventory}'.");
                continue;
            }

            seenInventory[inventory] = row;

            var brand = CleanPlaceholder(Text(row, 7));
            var model = CleanPlaceholder(Text(row, 8));
            var equipment = AddEquipment(name, brand, model, Text(row, 11), MapEquipmentCategory(Text(row, 5)), MapClassification(Text(row, 6)), ParseInt(Text(row, 10)), _catalogs.Name, row);
            var unit = new UnitRecord(
                inventory,
                equipment.Key,
                Text(row, 3),
                null,
                null,
                null,
                null,
                ParseDecimal(Text(row, 4)),
                0,
                null,
                null,
                _catalogs.Name,
                row);

            _unitsByInventory[inventory] = unit;
        }

        _result.KnownFindings.Add($"Hoja 1: {CountDataRows(_catalogs)} filas utiles detectadas; {seenInventory.Count} inventarios no duplicados.");
    }

    private void ReadInventoryOverrides()
    {
        // The inventory sheet is optional. When it is absent (real template), skip silently.
        if (_inventory is not { } inventorySheet)
        {
            return;
        }

        foreach (var row in DataRows(inventorySheet, 11))
        {
            var inventory = NormalizeInventory(Text(row, 1));
            if (string.IsNullOrWhiteSpace(inventory))
            {
                Reject(inventorySheet, row, "Inventory row lacks inventory number.");
                continue;
            }

            if (!_unitsByInventory.TryGetValue(inventory, out var unit))
            {
                var equipmentName = Text(row, 2);
                if (string.IsNullOrWhiteSpace(equipmentName))
                {
                    Reject(inventorySheet, row, $"Inventory '{inventory}' does not exist in catalogs and has no equipment name.");
                    continue;
                }

                var equipment = AddEquipment(equipmentName, null, null, null, 0, 7, null, inventorySheet.Name, row);
                unit = new UnitRecord(inventory, equipment.Key, null, null, null, null, null, null, 0, null, null, inventorySheet.Name, row);
                _unitsByInventory[inventory] = unit;
                Issue("WARN", inventorySheet, row, "A", $"Inventory '{inventory}' was created from sheet 2 because it was not present in sheet 1.");
            }

            unit.SerialNumber = Coalesce(Text(row, 3), unit.SerialNumber);
            unit.LaboratoryCode = Coalesce(Text(row, 4), unit.LaboratoryCode);
            unit.CareerName = Coalesce(Text(row, 5), unit.CareerName);
            unit.AcquisitionDate = ParseDate(Text(row, 7)) ?? unit.AcquisitionDate;
            unit.ManufacturingDate = ParseDate(Text(row, 8)) ?? unit.ManufacturingDate;
            unit.AcquisitionValue = ParseDecimal(Text(row, 9)) ?? unit.AcquisitionValue;
            unit.Status = MapEquipmentStatus(Text(row, 10), unit.Status);
            unit.PhysicalCondition = MapPhysicalConditionNullable(Text(row, 11)) ?? unit.PhysicalCondition;

            if (!string.IsNullOrWhiteSpace(unit.LaboratoryCode) && !LabExists(unit.LaboratoryCode))
            {
                Issue("ERROR", inventorySheet, row, "D", $"Unknown laboratory code '{unit.LaboratoryCode}'.");
            }
        }
    }

    private void RecordPendingLocations()
    {
        var unitsWithoutLaboratory = _unitsByInventory.Values
            .Where(unit => string.IsNullOrWhiteSpace(unit.LaboratoryCode))
            .ToList();

        if (unitsWithoutLaboratory.Count == 0)
        {
            return;
        }

        _result.KnownFindings.Add(
            $"{unitsWithoutLaboratory.Count} unidades sin laboratorio expl\u00edcito se conservar\u00e1n con LaboratoryId NULL y LocationResolutionStatus=Pending.");
    }

    private void ReadVerifications()
    {
        foreach (var row in DataRows(_verifications, 7))
        {
            var inventory = NormalizeInventory(Text(row, 1));
            var equipmentName = Text(row, 5);
            if (string.IsNullOrWhiteSpace(inventory))
            {
                _result.UnresolvedVerifications.Add(new UnresolvedVerificationRecord(
                    $"UNRES_VER_R{row}", Text(row, 1), Text(row, 2), Text(row, 3), Text(row, 4), equipmentName,
                    Text(row, 6), Text(row, 7), "No inventory number (only equipment name).", _verifications.Name, row));
                Reject(_verifications, row, "Verification has no inventory number. It will not be inserted.");
                continue;
            }

            if (!_unitsByInventory.ContainsKey(inventory))
            {
                _result.UnresolvedVerifications.Add(new UnresolvedVerificationRecord(
                    $"UNRES_VER_R{row}", inventory, Text(row, 2), Text(row, 3), Text(row, 4), equipmentName,
                    Text(row, 6), Text(row, 7), "Inventory not found among known units.", _verifications.Name, row));
                Issue("ERROR", _verifications, row, "A", $"Verification references '{inventory}', which is not a known inventory number.");
                Reject(_verifications, row, $"Unknown inventory '{inventory}' in verification.");
                continue;
            }

            var verificationDate = ParseDate(Text(row, 2)) ?? DateTime.Today;
            var verificationGestion = ResolveGestion(verificationDate);
            AddManagement(verificationGestion.Year, verificationGestion.Semester, verificationGestion.Code, row);

            var verification = new VerificationRecord(
                $"VER_R{row}",
                inventory,
                verificationGestion.Code,
                verificationDate,
                MapPhysicalCondition(Text(row, 3)),
                1,
                JoinText(Text(row, 4), Text(row, 5), Text(row, 7)),
                _verifications.Name,
                row);
            _result.Verifications.Add(verification);

            if (!string.IsNullOrWhiteSpace(Text(row, 4)))
            {
                _result.VerificationFaults.Add(new VerificationFaultRecord(verification.Key, TruncateRequired(Text(row, 4), 255), _verifications.Name, row));
            }
        }
    }

    private void ReadRequests()
    {
        foreach (var row in DataRows(_requests, 8))
        {
            var rawInventory = Text(row, 1);
            var split = SplitInventoryTokens(rawInventory);
            if (split.InvalidTokens.Count > 0)
            {
                Issue("ERROR", _requests, row, "A", $"Invalid inventory token(s): {string.Join(", ", split.InvalidTokens)}.");
            }

            if (split.ValidTokens.Count > 1)
            {
                Issue("WARN", _requests, row, "A", "Multiple inventory numbers detected. One request will be generated per valid inventory.");
            }

            if (split.ValidTokens.Count == 0)
            {
                Reject(_requests, row, "Request has no valid inventory number.");
                continue;
            }

            if (string.IsNullOrWhiteSpace(Text(row, 3)))
            {
                Issue("WARN", _requests, row, "C", "Priority is empty. RequestPriority.Medium=1 will be used.");
            }

            if (!string.IsNullOrWhiteSpace(Text(row, 7)))
            {
                Issue("WARN", _requests, row, "G", "Excel unique identifier is ignored; importer generates an internal correlation key.");
            }

            foreach (var inventory in split.ValidTokens)
            {
                if (!_unitsByInventory.TryGetValue(inventory, out var unit))
                {
                    Issue("ERROR", _requests, row, "A", $"Request references unknown inventory '{inventory}'.");
                    Reject(_requests, row, $"Unknown inventory '{inventory}' in request.");
                    continue;
                }

                if (string.IsNullOrWhiteSpace(unit.LaboratoryCode))
                {
                    Issue("WARN", _requests, row, "A", $"Request inventory '{inventory}' has no laboratory; it will remain pending without inventing a location.");
                }

                var description = Text(row, 4);
                if (string.IsNullOrWhiteSpace(description))
                {
                    Reject(_requests, row, $"Request for inventory '{inventory}' has empty description.");
                    continue;
                }

                var requestDate = ParseDate(Text(row, 2)) ?? DateTime.Today;
                var requestGestion = ResolveGestion(requestDate);
                AddManagement(requestGestion.Year, requestGestion.Semester, requestGestion.Code, row);

                var key = $"REQ_R{row}_{SanitizeKey(inventory)}";
                _requestsByKey[key] = new RequestRecord(
                    key,
                    inventory,
                    unit.LaboratoryCode,
                    unit.EquipmentKey,
                    requestGestion.Code,
                    1,
                    0,
                    1,
                    requestDate,
                    TruncateRequired(description, 1000),
                    Truncate(JoinText(Text(row, 5), PrefixIfAny("Solicitado por", Text(row, 8))), 500),
                    Truncate(Text(row, 6), 100),
                    null,
                    null,
                    _requests.Name,
                    row);
            }
        }
    }

    private void ReadKardex()
    {
        foreach (var row in DataRows(_kardex, 16))
        {
            var inventory = NormalizeInventory(Text(row, 1));
            if (!_unitsByInventory.ContainsKey(inventory))
            {
                Reject(_kardex, row, $"Maintenance references unknown inventory '{inventory}'.");
                continue;
            }

            if (string.IsNullOrWhiteSpace(Text(row, 5)) || string.IsNullOrWhiteSpace(Text(row, 7)))
            {
                Issue("ERROR", _kardex, row, "E/G", "Maintenance row lacks maintenance type or technician/provider.");
                Reject(_kardex, row, "Maintenance row is incomplete.");
                continue;
            }

            var technicianName = Text(row, 7);
            if (!_peopleByName.ContainsKey(technicianName))
            {
                AddPerson(technicianName, "Externo", true, 5, null, null, "Sin dato", _kardex.Name, row, warnIfAutoCreated: true);
            }

            var maintenanceDate = ParseDate(Text(row, 2))
                ?? ParseDate(Text(row, 3))
                ?? ParseDate(Text(row, 4))
                ?? DateTime.Today;
            var maintenanceGestion = ResolveGestion(maintenanceDate);
            AddManagement(maintenanceGestion.Year, maintenanceGestion.Semester, maintenanceGestion.Code, row);

            var key = $"MNT_R{row}_{SanitizeKey(inventory)}";
            var scheduledDate = ParseDate(Text(row, 2));
            var startDate = ParseDate(Text(row, 3));
            var endDate = ParseDate(Text(row, 4));
            if (scheduledDate.HasValue && endDate.HasValue && scheduledDate > endDate)
            {
                Issue("WARN", _kardex, row, "B", "La fecha programada es posterior a la finalizaci\u00f3n; se importa como NULL sin reinterpretarla.");
                scheduledDate = null;
            }

            var maintenance = new MaintenanceRecord(
                key,
                inventory,
                maintenanceGestion.Code,
                MapMaintenanceType(Text(row, 5)),
                MapServiceType(Text(row, 6)),
                technicianName,
                null,
                scheduledDate,
                startDate,
                endDate,
                Truncate(Text(row, 8), 2000),
                string.IsNullOrWhiteSpace(Text(row, 4)) ? 1 : 2,
                ParseDecimal(Text(row, 9)),   // Column I "Costo Total Bs (opcional)"
                MapSatisfaction(Text(row, 10)),   // Column J "Nivel de Satisfaccion"
                Truncate(Text(row, 11), 1000),   // Column K "Recomendaciones Post-Servicio"
                ParseDate(Text(row, 12)),   // Column L "Fecha Proximo Mantenimiento"
                _kardex.Name,
                row);
            _maintenanceByKey[key] = maintenance;

        }
    }

    private void ReadCosts()
    {
        foreach (var row in DataRows(_costs, 14))
        {
            var inventory = NormalizeInventory(Text(row, 1));
            if (!_unitsByInventory.TryGetValue(inventory, out var unit))
            {
                Reject(_costs, row, $"Cost row references unknown inventory '{inventory}'.");
                continue;
            }

            var type = Text(row, 2);
            var reference = Text(row, 3);
            var targetKey = string.Empty;
            var targetKind = string.Empty;

            if (type.Equals("Mantenimiento", StringComparison.OrdinalIgnoreCase))
            {
                var maintenance = _maintenanceByKey.Values.FirstOrDefault(m => m.InventoryNumber.Equals(inventory, StringComparison.OrdinalIgnoreCase));
                if (maintenance == null)
                {
                    Issue("ERROR", _costs, row, "C", "Maintenance cost cannot be linked because no valid maintenance was generated for the inventory.");
                    Reject(_costs, row, $"Cost for inventory '{inventory}' has no generated maintenance.");
                    continue;
                }

                targetKind = "Maintenance";
                targetKey = maintenance.Key;
            }
            else if (type.Equals("Desembolso", StringComparison.OrdinalIgnoreCase))
            {
                var lab = Coalesce(Text(row, 4), unit.LaboratoryCode);
                if (!string.IsNullOrWhiteSpace(lab) && !LabExists(lab))
                {
                    Reject(_costs, row, $"Disbursement cost for inventory '{inventory}' references an unknown laboratory.");
                    continue;
                }

                targetKind = "Request";
                targetKey = $"PUR_{SanitizeKey(reference)}_{SanitizeKey(inventory)}";
                if (!_requestsByKey.ContainsKey(targetKey))
                {
                    _requestsByKey[targetKey] = new RequestRecord(
                        targetKey,
                        inventory,
                        lab,
                        unit.EquipmentKey,
                        GetDefaultManagementCode(),
                        1,
                        0,
                        2,
                        ParseDate(Text(row, 7)) ?? DateTime.Today,
                        TruncateRequired(Coalesce(Text(row, 8), $"Desembolso historico {reference}"), 1000),
                        Truncate(PrefixIfAny("Responsable", Text(row, 6)), 500),
                        null,
                        Truncate(reference, 50),
                        Truncate(Text(row, 5), 100),
                        _costs.Name,
                        row);
                }
            }
            else
            {
                Reject(_costs, row, $"Unknown cost record type '{type}'.");
                continue;
            }

            var concept = Text(row, 8);
            if (string.IsNullOrWhiteSpace(concept))
            {
                Reject(_costs, row, "Cost concept is empty.");
                continue;
            }

            _result.CostDetails.Add(new CostDetailRecord(
                targetKind,
                targetKey,
                TruncateRequired(concept, 200),
                null,
                ParseDecimal(Text(row, 10)) ?? 1,
                Truncate(Coalesce(Text(row, 11), "Unidad"), 50),
                ParseDecimal(Text(row, 12)) ?? 0,
                MapCostCategory(Text(row, 9)),
                Truncate(Text(row, 14), 200),
                null,
                _costs.Name,
                row));
        }
    }

    private void ReadDepartures()
    {
        foreach (var row in DataRows(_departures, 9))
        {
            var inventory = NormalizeInventory(Text(row, 1));
            if (!_unitsByInventory.TryGetValue(inventory, out var unit))
            {
                Reject(_departures, row, $"Departure references unknown inventory '{inventory}'.");
                continue;
            }

            var borrower = Text(row, 6);
            if (string.IsNullOrWhiteSpace(borrower))
            {
                Reject(_departures, row, "Departure has no borrower/responsible.");
                continue;
            }

            if (!_peopleByName.ContainsKey(borrower))
            {
                var category = borrower.Contains("DOCENTE", StringComparison.OrdinalIgnoreCase) ? 2 : 99;
                AddPerson(borrower, "Interno", false, category, null, null, null, _departures.Name, row, warnIfAutoCreated: true);
            }

            var departureDate = ParseDate(Text(row, 7)) ?? DateTime.Today;
            var estimatedReturn = ParseDate(Text(row, 8));
            if (estimatedReturn == null)
            {
                estimatedReturn = departureDate;
                Issue("WARN", _departures, row, "H", "Estimated return date is empty. DepartureDate will be used because the database requires a value.");
            }

            var key = $"DEP_R{row}_{SanitizeKey(inventory)}";
            _result.Departures.Add(new DepartureRecord(
                key,
                inventory,
                GetDefaultManagementCode(),
                borrower,
                MapDepartureType(Text(row, 2)),
                departureDate,
                estimatedReturn.Value,
                Truncate(Text(row, 9), 500),
                _departures.Name,
                row));

            _result.DepartureItems.Add(new DepartureItemRecord(
                key,
                inventory,
                TruncateRequired(GetEquipmentName(unit.EquipmentKey), 200),
                1,
                "UNIDAD",
                Truncate(Text(row, 9), 500),
                _departures.Name,
                row));
        }
    }

    private void ReadPlans()
    {
        foreach (var row in DataRows(_plans, 9))
        {
            var inventory = NormalizeInventory(Text(row, 2));
            if (string.IsNullOrWhiteSpace(inventory))
            {
                Reject(_plans, row, "Management plan row has no inventory number.");
                continue;
            }

            if (!_unitsByInventory.ContainsKey(inventory))
            {
                Reject(_plans, row, $"Management plan references unknown inventory '{inventory}'.");
                continue;
            }

            if (!TryResolvePlanManagement(row, out var year, out var semester, out var managementCode))
            {
                Reject(_plans, row, $"Invalid management (Gestion='{Text(row, 1)}', Fecha='{Text(row, 8)}').");
                continue;
            }

            AddManagement(year, semester, managementCode, row);
            var completed = Text(row, 9).Equals("Si", StringComparison.OrdinalIgnoreCase)
                || Text(row, 9).Equals("S\u00ed", StringComparison.OrdinalIgnoreCase);

            _result.ManagementPlans.Add(new ManagementPlanRecord(
                $"PLAN_R{row}_{SanitizeKey(inventory)}",
                managementCode,
                inventory,
                MapWizardPhaseByPlan(Text(row, 5), completed),
                completed ? 9 : 1,
                Text(row, 7),
                ParseDate(Text(row, 8)),
                completed ? 2 : 0,
                _plans.Name,
                row));
        }
    }

    private void AddKnownFindings()
    {
        _result.KnownFindings.Add("Hoja 1 se trata como catalogo + unidades por decision de cliente.");
        _result.KnownFindings.Add("Hoja 3 no genera inserts si columna A no contiene InventoryNumber real.");
        _result.KnownFindings.Add("Requests.Priority usa Medium=1 por defecto cuando la columna esta vacia.");
        _result.KnownFindings.Add($"Facultad oficial normalizada: {OfficialGastronomyFaculty}.");
    }

    private EquipmentRecord AddEquipment(string name, string? brand, string? model, string? description, int category, int classification, int? usefulLife, string sourceSheet, int sourceRow)
    {
        var key = EquipmentKey(name, brand, model);
        if (_equipmentByKey.TryGetValue(key, out var existing))
        {
            existing.Description = Coalesce(existing.Description, description);
            return existing;
        }

        var equipment = new EquipmentRecord(key, TruncateRequired(name, 200), Truncate(brand, 100), Truncate(model, 100), Truncate(description, 2000), category, classification, usefulLife, sourceSheet, sourceRow);
        _equipmentByKey[key] = equipment;
        return equipment;
    }

    private string GetEquipmentName(string equipmentKey)
    {
        return _equipmentByKey.TryGetValue(equipmentKey, out var equipment) ? equipment.Name : equipmentKey;
    }

    private void AddFaculty(string name, string? code, string? description, string sourceSheet, int sourceRow)
    {
        if (_result.Faculties.Any(f => f.Name.Equals(name, StringComparison.OrdinalIgnoreCase)))
        {
            return;
        }

        _result.Faculties.Add(new FacultyRecord(name, Truncate(code, 50), Truncate(description, 500), sourceSheet, sourceRow));
    }

    private void AddPerson(string name, string type, bool isCompany, int category, string? email, string? phone, string? address, string sourceSheet, int sourceRow, bool warnIfAutoCreated)
    {
        if (_peopleByName.ContainsKey(name))
        {
            return;
        }

        if (warnIfAutoCreated)
        {
            Issue("WARN", sourceSheet, sourceRow, "", $"Person/provider '{name}' was auto-created because it was referenced but not listed in sheet 9.");
        }

        _peopleByName[name] = new PersonRecord(name, type.Equals("Externo", StringComparison.OrdinalIgnoreCase), isCompany, category, Truncate(email, 100), Truncate(phone, 20), Truncate(address, 500), sourceSheet, sourceRow);
    }

    private void AddManagement(int year, int semester, string code, int sourceRow)
    {
        if (_managementByCode.ContainsKey(code))
        {
            return;
        }

        _managementByCode[code] = new ManagementRecord(code, year, semester, $"Gestion historica {code}", sourceRow);
    }

    // Resolves the management (year + semester) for a Plan (L-48) row.
    // The real template stores only a bare year in col 1 ("Gestion") and the actual date in col 8
    // ("Fecha Ejecucion"), so the execution date drives the semester when available.
    private bool TryResolvePlanManagement(SheetRow row, out int year, out int semester, out string code)
    {
        var executionDate = ParseDate(Text(row, 8));
        if (executionDate.HasValue)
        {
            var gestion = ResolveGestion(executionDate.Value);
            year = gestion.Year;
            semester = gestion.Semester;
            code = gestion.Code;
            return true;
        }

        var raw = Text(row, 1).Trim();
        if (TryParseManagement(raw, out year, out semester))
        {
            code = raw;
            return true;
        }

        // A bare year (e.g. "2025") is the documented format for this column; default to semester 1.
        if (int.TryParse(raw, NumberStyles.Integer, CultureInfo.InvariantCulture, out var bareYear)
            && bareYear >= 1900 && bareYear <= 2999)
        {
            year = bareYear;
            semester = 1;
            code = $"{bareYear}-1";
            return true;
        }

        // Last resort: a formatted/serial date in the Gestion column.
        if (TryResolveManagement(raw, out year, out semester, out code))
        {
            return true;
        }

        year = 0;
        semester = 0;
        code = string.Empty;
        return false;
    }

    private string GetDefaultManagementCode()
    {
        var latest = _managementByCode.Values
            .OrderByDescending(m => m.Year)
            .ThenByDescending(m => m.Semester)
            .FirstOrDefault();
        if (latest != null)
        {
            return latest.Code;
        }

        var code = $"{DateTime.Now.Year}-1";
        AddManagement(DateTime.Now.Year, 1, code, 0);
        return code;
    }

    private IEnumerable<SheetRow> DataRows(ExcelWorksheet ws, int maxCol)
    {
        if (ws.Dimension == null)
        {
            yield break;
        }

        for (var row = 5; row <= ws.Dimension.End.Row; row++)
        {
            var values = new List<string>();
            for (var col = 1; col <= maxCol; col++)
            {
                values.Add(Text(ws, row, col));
            }

            if (values.Any(v => !string.IsNullOrWhiteSpace(v)) && !IsExampleRow(values))
            {
                yield return new SheetRow(ws, row);
            }
        }
    }

    private int CountDataRows(ExcelWorksheet ws)
    {
        if (ws.Dimension == null)
        {
            return 0;
        }

        var count = 0;
        var maxCol = Math.Min(ws.Dimension.End.Column, 16);
        for (var row = 5; row <= ws.Dimension.End.Row; row++)
        {
            var values = new List<string>();
            for (var col = 1; col <= maxCol; col++)
            {
                values.Add(Text(ws, row, col));
            }

            if (values.Any(v => !string.IsNullOrWhiteSpace(v)) && !IsExampleRow(values))
            {
                count++;
            }
        }

        return count;
    }

    private static string Text(ExcelRangeBase cell)
    {
        return (cell.Text ?? string.Empty).Trim();
    }

    private static string Text(ExcelWorksheet ws, int row, int col)
    {
        return Text(ws.Cells[row, col]);
    }

    private static string Text(SheetRow row, int col)
    {
        return Text(row.Worksheet, row.Number, col);
    }

    private static bool IsExampleRow(IEnumerable<string> values)
    {
        var joined = string.Join(" | ", values).Trim();
        return joined.StartsWith("\u2193", StringComparison.Ordinal)
            || joined.Contains("EJEMPLOS", StringComparison.OrdinalIgnoreCase)
            || joined.Contains("borrar antes", StringComparison.OrdinalIgnoreCase);
    }

    private static string NormalizeFaculty(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return string.Empty;
        }

        var normalized = RemoveDiacritics(value).ToUpperInvariant();
        if (normalized.Contains("GASTRONOM") || normalized.Contains("ARQUITECTURA Y TURISMO"))
        {
            return OfficialGastronomyFaculty;
        }

        return value.Trim();
    }

    private static string NormalizeInventory(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return string.Empty;
        }

        // Strip surrounding whitespace and leading noise such as ':' so keys like ":34179" match their unit.
        return value.Trim().TrimStart(':', ' ', '\t').Trim();
    }

    private InventorySplit SplitInventoryTokens(string value)
    {
        var valid = new List<string>();
        var invalid = new List<string>();

        if (string.IsNullOrWhiteSpace(value))
        {
            return new InventorySplit(valid, invalid);
        }

        var tokens = value.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        if (tokens.Length == 0)
        {
            tokens = [value.Trim()];
        }

        foreach (var token in tokens)
        {
            var cleaned = token.Trim();
            if (cleaned == "000" || cleaned == "0" || cleaned.Contains('.') || cleaned.Contains(' '))
            {
                invalid.Add(cleaned);
                continue;
            }

            if (_unitsByInventory.ContainsKey(cleaned))
            {
                valid.Add(cleaned);
            }
            else
            {
                invalid.Add(cleaned);
            }
        }

        return new InventorySplit(valid.Distinct(StringComparer.OrdinalIgnoreCase).ToList(), invalid);
    }

    private bool LabExists(string code)
    {
        return _result.Laboratories.Any(l => l.Code.Equals(code, StringComparison.OrdinalIgnoreCase));
    }

    private static string EquipmentKey(string name, string? brand, string? model)
    {
        return $"{NormalizeKey(name)}|{NormalizeKey(brand)}|{NormalizeKey(model)}";
    }

    private static string NormalizeKey(string? value)
    {
        return RemoveDiacritics(value ?? string.Empty).Trim().ToUpperInvariant();
    }

    private static string SanitizeKey(string value)
    {
        var chars = value.Where(char.IsLetterOrDigit).ToArray();
        return chars.Length == 0 ? "NA" : new string(chars);
    }

    private static string NaturalSortKey(string value)
    {
        return value.PadLeft(20, '0');
    }

    private static string RemoveDiacritics(string value)
    {
        var normalized = value.Normalize(NormalizationForm.FormD);
        var builder = new StringBuilder();
        foreach (var ch in normalized)
        {
            if (CharUnicodeInfo.GetUnicodeCategory(ch) != UnicodeCategory.NonSpacingMark)
            {
                builder.Append(ch);
            }
        }

        return builder.ToString().Normalize(NormalizationForm.FormC);
    }

    private static string? CleanPlaceholder(string value)
    {
        if (value.Equals("DESCRIPCION DETALLADA DEL ACTIVO", StringComparison.OrdinalIgnoreCase))
        {
            return null;
        }

        return string.IsNullOrWhiteSpace(value) ? null : value;
    }

    private static string? Coalesce(params string?[] values)
    {
        return values.FirstOrDefault(v => !string.IsNullOrWhiteSpace(v));
    }

    private static string JoinText(params string?[] values)
    {
        return string.Join(" | ", values.Where(v => !string.IsNullOrWhiteSpace(v)).Select(v => v!.Trim()));
    }

    private static string PrefixIfAny(string prefix, string value)
    {
        return string.IsNullOrWhiteSpace(value) ? string.Empty : $"{prefix}: {value}";
    }

    private static string? Truncate(string? value, int maxLength)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        return value.Length <= maxLength ? value : value[..maxLength];
    }

    private static string TruncateRequired(string? value, int maxLength)
    {
        return Truncate(value, maxLength) ?? string.Empty;
    }

    private static int? ParseInt(string value)
    {
        return int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var parsed) ? parsed : null;
    }

    private static decimal? ParseDecimal(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        value = value.Replace("Bs", "", StringComparison.OrdinalIgnoreCase).Replace(" ", "").Replace(",", ".");
        return decimal.TryParse(value, NumberStyles.Number, CultureInfo.InvariantCulture, out var parsed) ? parsed : null;
    }

    private static DateTime? ParseDate(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        if (DateTime.TryParse(value, CultureInfo.InvariantCulture, DateTimeStyles.AssumeLocal, out var parsed)
            || DateTime.TryParse(value, new CultureInfo("es-BO"), DateTimeStyles.AssumeLocal, out parsed))
        {
            return parsed.Date;
        }

        return null;
    }

    private static bool TryParseManagement(string value, out int year, out int semester)
    {
        year = 0;
        semester = 0;
        var match = System.Text.RegularExpressions.Regex.Match(value.Trim(), @"^(?<year>\d{4})-(?<semester>[012])$");
        if (!match.Success)
        {
            return false;
        }

        year = int.Parse(match.Groups["year"].Value, CultureInfo.InvariantCulture);
        semester = int.Parse(match.Groups["semester"].Value, CultureInfo.InvariantCulture);
        return true;
    }

    // Derives the management (gestion) code from a date: semester 1 = Jan-Jun, semester 2 = Jul-Dec.
    private static (int Year, int Semester, string Code) ResolveGestion(DateTime fecha)
    {
        var year = fecha.Year;
        var semester = fecha.Month <= 6 ? 1 : 2;
        return (year, semester, $"{year}-{semester}");
    }

    // Resolves a "Gestion" cell that may hold either a literal YYYY-S code or an Excel serial/formatted date.
    private static bool TryResolveManagement(string value, out int year, out int semester, out string code)
    {
        if (TryParseManagement(value, out year, out semester))
        {
            code = value.Trim();
            return true;
        }

        var date = ParseManagementDate(value);
        if (date.HasValue)
        {
            var gestion = ResolveGestion(date.Value);
            year = gestion.Year;
            semester = gestion.Semester;
            code = gestion.Code;
            return true;
        }

        year = 0;
        semester = 0;
        code = string.Empty;
        return false;
    }

    private static DateTime? ParseManagementDate(string value)
    {
        var parsed = ParseDate(value);
        if (parsed.HasValue)
        {
            return parsed;
        }

        // Excel serial date (numeric cell): 60 ~ 1900-03-01, upper bound guards against unrelated large numbers.
        if (double.TryParse(value.Trim(), NumberStyles.Any, CultureInfo.InvariantCulture, out var serial)
            && serial > 59 && serial < 100000)
        {
            try
            {
                return DateTime.FromOADate(serial).Date;
            }
            catch (ArgumentException)
            {
                return null;
            }
        }

        return null;
    }

    private static bool IsYes(string value)
    {
        var normalized = RemoveDiacritics(value).Trim();
        return normalized.Equals("Si", StringComparison.OrdinalIgnoreCase)
            || normalized.Equals("Yes", StringComparison.OrdinalIgnoreCase)
            || normalized.Equals("True", StringComparison.OrdinalIgnoreCase);
    }

    private static int MapGeneralStatus(string value)
    {
        return RemoveDiacritics(value).Equals("Inactivo", StringComparison.OrdinalIgnoreCase) ? 1 : 0;
    }

    private static int MapEquipmentCategory(string value)
    {
        var normalized = RemoveDiacritics(value).ToUpperInvariant();
        if (normalized.Contains("UTENSILIO")) return 1;
        if (normalized.Contains("OTRO")) return 2;
        return 0;
    }

    private static int MapClassification(string value)
    {
        var normalized = RemoveDiacritics(value).ToUpperInvariant();
        if (normalized.Contains("ELECT")) return 0;
        if (normalized.Contains("MANUAL") || normalized.Contains("MECAN")) return 1;
        if (normalized.Contains("MOBILI")) return 2;
        if (normalized.Contains("MEDIC")) return 3;
        if (normalized.Contains("VIDRIO")) return 4;
        if (normalized.Contains("REACT")) return 5;
        if (normalized.Contains("INFORM")) return 6;
        if (normalized.Contains("CALOR")) return 8;
        if (normalized.Contains("FRIO")) return 9;
        return 7;
    }

    private static int MapEquipmentStatus(string value, int fallback)
    {
        var normalized = RemoveDiacritics(value).ToUpperInvariant();
        if (normalized.Contains("BAJA") || normalized.Contains("ELIMIN")) return 99;
        if (normalized.Contains("MANT")) return 1;
        if (normalized.Contains("REPAR")) return 5;
        if (normalized.Contains("AVERI")) return 6;
        if (normalized.Contains("PREST")) return 10;
        if (normalized.Contains("ALTA") || normalized.Contains("OPER")) return 0;
        return fallback;
    }

    private static int? MapPhysicalConditionNullable(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        return MapPhysicalCondition(value);
    }

    private static int MapPhysicalCondition(string value)
    {
        var normalized = RemoveDiacritics(value).ToUpperInvariant();
        if (normalized.Contains("EXCEL")) return 5;
        if (normalized.Contains("BUEN") || normalized.Contains("ALTA")) return 4;
        if (normalized.Contains("REG")) return 3;
        if (normalized.Contains("MAL")) return 2;
        if (normalized.Contains("BAJA")) return 1;
        return 4;
    }

    private static int MapMaintenanceType(string value)
    {
        var normalized = RemoveDiacritics(value).ToUpperInvariant();
        if (normalized.Contains("PREVENT")) return 1;
        if (normalized.Contains("CORRECT")) return 2;
        if (normalized.Contains("CALIB")) return 3;
        if (normalized.Contains("LIMPI")) return 4;
        if (normalized.Contains("ELECT")) return 5;
        return 99;
    }

    private static int MapServiceType(string value)
    {
        return RemoveDiacritics(value).Contains("Externo", StringComparison.OrdinalIgnoreCase) ? 1 : 0;
    }

    private static int MapSatisfaction(string value)
    {
        var normalized = RemoveDiacritics(value).ToUpperInvariant();
        if (normalized.Contains("MUY")) return 5;
        if (normalized.Contains("SATIS")) return 4;
        if (normalized.Contains("OK")) return 3;
        if (normalized.Contains("INSAT")) return 2;
        if (normalized.Contains("INACEP")) return 1;
        return 3;
    }

    private static int MapCostCategory(string value)
    {
        var normalized = RemoveDiacritics(value).ToUpperInvariant();
        if (normalized.Contains("MANO")) return 2;
        if (normalized.Contains("HERR")) return 3;
        if (normalized.Contains("TRANS")) return 4;
        if (normalized.Contains("SERVICIO")) return 5;
        if (normalized.Contains("CALIB")) return 6;
        if (normalized.Contains("INSUM") || normalized.Contains("CONSUM")) return 7;
        if (normalized.Contains("VIAT")) return 8;
        if (normalized.Contains("OTRO")) return 99;
        return 1;
    }

    private static int MapDepartureType(string value)
    {
        var normalized = RemoveDiacritics(value).ToUpperInvariant();
        if (normalized.Contains("MANTENIMIENTO EXTERNO")) return 3;
        if (normalized.Contains("MANTENIMIENTO INTERNO")) return 5;
        if (normalized.Contains("BAJA")) return 4;
        if (normalized.Contains("EXTERNO")) return 1;
        return 2;
    }

    private static int MapWizardPhaseByPlan(string maintenanceType, bool completed)
    {
        if (completed) return 6;
        return RemoveDiacritics(maintenanceType).Contains("Correct", StringComparison.OrdinalIgnoreCase) ? 2 : 1;
    }

    private void Issue(string severity, ExcelWorksheet ws, int row, string column, string message)
    {
        Issue(severity, ws.Name, row, column, message);
    }

    private void Issue(string severity, string sheet, int row, string column, string message)
    {
        _result.Issues.Add(new IssueRecord(severity, sheet, row, column, message));
    }

    private void Reject(ExcelWorksheet ws, int row, string reason)
    {
        _result.Rejects.Add(new RejectRecord(ws.Name, row, reason));
    }
}

internal sealed class OutputWriter
{
    private readonly string _outputDir;
    private readonly AnalysisResult _result;

    public OutputWriter(string outputDir, AnalysisResult result)
    {
        _outputDir = outputDir;
        _result = result;
    }

    public void WriteAll(bool includeSql = true)
    {
        WriteAudit();
        WriteIssues();
        WriteRejects();
        WritePreview();
        if (includeSql)
        {
            WriteSql();
        }
    }

    private void WriteAudit()
    {
        var path = Path.Combine(_outputDir, "audit.md");
        var builder = new StringBuilder();
        builder.AppendLine("# Auditoria De Carga Historica");
        builder.AppendLine();
        builder.AppendLine("## Resumen");
        builder.AppendLine($"- Faculties: {_result.Faculties.Count}");
        builder.AppendLine($"- Careers: {_result.Careers.Count}");
        builder.AppendLine($"- Laboratories: {_result.Laboratories.Count}");
        builder.AppendLine($"- People/Providers: {_result.People.Count}");
        builder.AppendLine($"- Managements: {_result.Managements.Count}");
        builder.AppendLine($"- Equipments: {_result.Equipments.Count}");
        builder.AppendLine($"- EquipmentUnits: {_result.Units.Count}");
        builder.AppendLine($"- Verifications insertables: {_result.Verifications.Count}");
        builder.AppendLine($"- Requests insertables: {_result.Requests.Count}");
        builder.AppendLine($"- Maintenances insertables: {_result.Maintenances.Count}");
        builder.AppendLine($"- CostDetails insertables: {_result.CostDetails.Count}");
        builder.AppendLine($"- Departures insertables: {_result.Departures.Count}");
        builder.AppendLine($"- DepartureItems insertables: {_result.DepartureItems.Count}");
        builder.AppendLine($"- ManagementPlans insertables: {_result.ManagementPlans.Count}");
        builder.AppendLine($"- Issues: {_result.Issues.Count} ({_result.Issues.Count(i => i.Severity == "ERROR")} errores)");
        builder.AppendLine($"- Rejects: {_result.Rejects.Count}");
        builder.AppendLine();
        builder.AppendLine("## Hojas");
        foreach (var sheet in _result.SheetSummaries)
        {
            builder.AppendLine($"- {sheet.Name}: dataRows={sheet.DataRows}, maxRow={sheet.MaxRow}, maxCol={sheet.MaxCol}");
        }

        builder.AppendLine();
        builder.AppendLine("## Hallazgos Incorporados");
        foreach (var finding in _result.KnownFindings)
        {
            builder.AppendLine($"- {finding}");
        }

        builder.AppendLine();
        builder.AppendLine("## Notas");
        builder.AppendLine("- El SQL se genera offline y no se ejecuta automaticamente.");
        builder.AppendLine("- Las verificaciones sin InventoryNumber real quedan rechazadas.");
        builder.AppendLine("- Las solicitudes con inventarios multiples generan una solicitud por inventario valido.");
        File.WriteAllText(path, builder.ToString(), Encoding.UTF8);
    }

    private void WriteIssues()
    {
        var rows = _result.Issues.Select(i => new[] { i.Severity, i.Sheet, i.Row.ToString(CultureInfo.InvariantCulture), i.Column, i.Message });
        WriteCsv(Path.Combine(_outputDir, "issues.csv"), ["Severity", "Sheet", "Row", "Column", "Message"], rows);
    }

    private void WriteRejects()
    {
        var rows = _result.Rejects.Select(r => new[] { r.Sheet, r.Row.ToString(CultureInfo.InvariantCulture), r.Reason });
        WriteCsv(Path.Combine(_outputDir, "rejects.csv"), ["Sheet", "Row", "Reason"], rows);
    }

    private void WritePreview()
    {
        var rows = new List<string[]>();
        rows.AddRange(_result.Faculties.Select(f => Preview("Faculty", f.Name, f.SourceSheet, f.SourceRow, $"Code={f.Code}; Description={f.Description}")));
        rows.AddRange(_result.Careers.Select(c => Preview("Career", c.Name, c.SourceSheet, c.SourceRow, $"Faculty={c.FacultyName}; Status={c.Status}")));
        rows.AddRange(_result.Laboratories.Select(l => Preview("Laboratory", l.Code, l.SourceSheet, l.SourceRow, $"Name={l.Name}; Faculty={l.FacultyName}")));
        rows.AddRange(_result.People.Select(p => Preview("Person", p.Name, p.SourceSheet, p.SourceRow, $"External={p.IsExternal}; Company={p.IsCompany}; Category={p.Category}")));
        rows.AddRange(_result.Managements.Select(m => Preview("Management", m.Code, "8 - Plan", m.SourceRow, $"Year={m.Year}; Semester={m.Semester}")));
        rows.AddRange(_result.Equipments.Select(e => Preview("Equipment", e.Key, e.SourceSheet, e.SourceRow, $"Name={e.Name}; Brand={e.Brand}; Model={e.Model}")));
        rows.AddRange(_result.Units.Select(u => Preview("EquipmentUnit", u.InventoryNumber, u.SourceSheet, u.SourceRow, $"EquipmentKey={u.EquipmentKey}; Lab={u.LaboratoryCode}; Career={u.CareerName}")));
        rows.AddRange(_result.Requests.Select(r => Preview("Request", r.Key, r.SourceSheet, r.SourceRow, $"Inventory={r.InventoryNumber}; Type={r.Type}; Description={r.Description}")));
        rows.AddRange(_result.Maintenances.Select(m => Preview("Maintenance", m.Key, m.SourceSheet, m.SourceRow, $"Inventory={m.InventoryNumber}; Type={m.MaintenanceType}; Technician={m.TechnicianName}")));
        rows.AddRange(_result.CostDetails.Select(c => Preview("CostDetail", c.TargetKey, c.SourceSheet, c.SourceRow, $"Target={c.TargetKind}; Concept={c.Concept}; Amount={c.Quantity * c.UnitPrice}")));
        rows.AddRange(_result.Departures.Select(d => Preview("Departure", d.Key, d.SourceSheet, d.SourceRow, $"Inventory={d.InventoryNumber}; Borrower={d.BorrowerName}")));
        rows.AddRange(_result.DepartureItems.Select(i => Preview("DepartureItem", i.DepartureKey, i.SourceSheet, i.SourceRow, $"Inventory={i.InventoryNumber}; Product={i.ProductName}; Quantity={i.Quantity}")));
        rows.AddRange(_result.ManagementPlans.Select(p => Preview("ManagementPlan", p.Key, p.SourceSheet, p.SourceRow, $"Management={p.ManagementCode}; Inventory={p.InventoryNumber}; Status={p.PlanStatus}")));
        WriteCsv(Path.Combine(_outputDir, "normalized-preview.csv"), ["Entity", "NaturalKey", "SourceSheet", "SourceRow", "Fields"], rows);
    }

    private void WriteSql()
    {
        var sql = new SqlScriptBuilder(_result).Build();
        File.WriteAllText(Path.Combine(_outputDir, "load.sql"), sql, Encoding.UTF8);
    }

    private static string[] Preview(string entity, string key, string sourceSheet, int sourceRow, string fields)
    {
        return [entity, key, sourceSheet, sourceRow.ToString(CultureInfo.InvariantCulture), fields];
    }

    private static void WriteCsv(string path, string[] headers, IEnumerable<string[]> rows)
    {
        var builder = new StringBuilder();
        builder.AppendLine(string.Join(",", headers.Select(Csv)));
        foreach (var row in rows)
        {
            builder.AppendLine(string.Join(",", row.Select(Csv)));
        }

        File.WriteAllText(path, builder.ToString(), Encoding.UTF8);
    }

    private static string Csv(string? value)
    {
        value ??= string.Empty;
        return $"\"{value.Replace("\"", "\"\"")}\"";
    }
}

internal sealed class SqlScriptBuilder
{
    private readonly AnalysisResult _result;
    private readonly StringBuilder _sql = new();

    public SqlScriptBuilder(AnalysisResult result)
    {
        _result = result;
    }

    public string Build()
    {
        Header();
        DeclareMaps();
        InsertFaculties();
        InsertCareers();
        InsertLaboratories();
        InsertPeople();
        InsertManagements();
        InsertEquipments();
        InsertUnits();
        InsertRequests();
        InsertMaintenances();
        InsertCostDetails();
        InsertDepartures();
        InsertDepartureItems();
        InsertManagementPlans();
        Footer();
        return _sql.ToString();
    }

    private void Header()
    {
        _sql.AppendLine("SET ANSI_NULLS ON;");
        _sql.AppendLine("SET ANSI_PADDING ON;");
        _sql.AppendLine("SET ANSI_WARNINGS ON;");
        _sql.AppendLine("SET ARITHABORT ON;");
        _sql.AppendLine("SET CONCAT_NULL_YIELDS_NULL ON;");
        _sql.AppendLine("SET QUOTED_IDENTIFIER ON;");
        _sql.AppendLine("SET NUMERIC_ROUNDABORT OFF;");
        _sql.AppendLine("SET NOCOUNT ON;");
        _sql.AppendLine("SET XACT_ABORT ON;");
        _sql.AppendLine("IF OBJECT_ID(N'dbo.__EFMigrationsHistory', N'U') IS NULL");
        _sql.AppendLine("    THROW 51000, 'La base destino no contiene el esquema EF Core esperado.', 1;");
        _sql.AppendLine("BEGIN TRY");
        _sql.AppendLine("BEGIN TRANSACTION;");
        _sql.AppendLine();
        _sql.AppendLine("DECLARE @CreatedById int = NULL;");
        _sql.AppendLine("DECLARE @Today datetime2 = SYSUTCDATETIME();");
        _sql.AppendLine();
    }

    private void DeclareMaps()
    {
        _sql.AppendLine("DECLARE @FacultyMap TABLE ([Key] nvarchar(300) PRIMARY KEY, Id int NOT NULL);");
        _sql.AppendLine("DECLARE @CareerMap TABLE ([Key] nvarchar(300) PRIMARY KEY, Id int NOT NULL);");
        _sql.AppendLine("DECLARE @LabMap TABLE ([Key] nvarchar(100) PRIMARY KEY, Id int NOT NULL);");
        _sql.AppendLine("DECLARE @PersonMap TABLE ([Key] nvarchar(300) PRIMARY KEY, Id int NOT NULL);");
        _sql.AppendLine("DECLARE @ManagementMap TABLE ([Key] nvarchar(50) PRIMARY KEY, Id int NOT NULL);");
        _sql.AppendLine("DECLARE @EquipmentMap TABLE ([Key] nvarchar(700) PRIMARY KEY, Id int NOT NULL);");
        _sql.AppendLine("DECLARE @UnitMap TABLE ([Key] nvarchar(50) PRIMARY KEY, Id int NOT NULL, EquipmentId int NOT NULL, LaboratoryId int NULL);");
        _sql.AppendLine("DECLARE @RequestMap TABLE ([Key] nvarchar(120) PRIMARY KEY, Id int NOT NULL);");
        _sql.AppendLine("DECLARE @MaintenanceMap TABLE ([Key] nvarchar(120) PRIMARY KEY, Id int NOT NULL);");
        _sql.AppendLine("DECLARE @DepartureMap TABLE ([Key] nvarchar(120) PRIMARY KEY, Id int NOT NULL);");
        _sql.AppendLine();
    }

    private void InsertFaculties()
    {
        foreach (var f in _result.Faculties)
        {
            _sql.AppendLine($"-- Faculty: {Comment(f.Name)}");
            _sql.AppendLine("DECLARE @FacultyId int;");
            _sql.AppendLine($"SELECT @FacultyId = Id FROM Faculties WHERE Name = {S(f.Name)};");
            _sql.AppendLine("IF @FacultyId IS NULL");
            _sql.AppendLine("BEGIN");
            _sql.AppendLine($"    INSERT INTO Faculties (Name, Code, Description, Status, CreatedDate, CreatedById) VALUES ({S(f.Name)}, {S(f.Code)}, {S(f.Description)}, 0, @Today, @CreatedById);");
            _sql.AppendLine("    SET @FacultyId = CONVERT(int, SCOPE_IDENTITY());");
            _sql.AppendLine("END");
            _sql.AppendLine($"INSERT INTO @FacultyMap ([Key], Id) VALUES ({S(f.Name)}, @FacultyId);");
            _sql.AppendLine();
        }
    }

    private void InsertCareers()
    {
        foreach (var c in _result.Careers)
        {
            _sql.AppendLine($"-- Career: {Comment(c.Name)}");
            _sql.AppendLine("DECLARE @CareerId int;");
            _sql.AppendLine("DECLARE @CareerFacultyId int = NULL;");
            if (!string.IsNullOrWhiteSpace(c.FacultyName))
            {
                _sql.AppendLine($"SELECT @CareerFacultyId = Id FROM @FacultyMap WHERE [Key] = {S(c.FacultyName)};");
            }
            _sql.AppendLine($"SELECT @CareerId = Id FROM Careers WHERE Name = {S(c.Name)};");
            _sql.AppendLine("IF @CareerId IS NULL");
            _sql.AppendLine("BEGIN");
            _sql.AppendLine($"    INSERT INTO Careers (Name, FacultadId, Status, CreatedDate, CreatedById) VALUES ({S(c.Name)}, @CareerFacultyId, {c.Status}, @Today, @CreatedById);");
            _sql.AppendLine("    SET @CareerId = CONVERT(int, SCOPE_IDENTITY());");
            _sql.AppendLine("END");
            _sql.AppendLine($"IF NOT EXISTS (SELECT 1 FROM @CareerMap WHERE [Key] = {S(c.Name)}) INSERT INTO @CareerMap ([Key], Id) VALUES ({S(c.Name)}, @CareerId);");
            _sql.AppendLine();
        }
    }

    private void InsertLaboratories()
    {
        foreach (var l in _result.Laboratories)
        {
            _sql.AppendLine($"-- Laboratory: {Comment(l.Code)}");
            _sql.AppendLine("DECLARE @LabId int;");
            _sql.AppendLine("DECLARE @LabFacultyId int;");
            _sql.AppendLine($"SELECT @LabFacultyId = Id FROM @FacultyMap WHERE [Key] = {S(l.FacultyName)};");
            _sql.AppendLine($"SELECT @LabId = Id FROM Laboratories WHERE Code = {S(l.Code)};");
            _sql.AppendLine("IF @LabId IS NULL");
            _sql.AppendLine("BEGIN");
            _sql.AppendLine($"    INSERT INTO Laboratories (FacultyId, Code, Name, Floor, Description, Status, CreatedDate, CreatedById) VALUES (@LabFacultyId, {S(l.Code)}, {S(l.Name)}, {S(l.Floor)}, {S(l.Description)}, 0, @Today, @CreatedById);");
            _sql.AppendLine("    SET @LabId = CONVERT(int, SCOPE_IDENTITY());");
            _sql.AppendLine("END");
            _sql.AppendLine($"INSERT INTO @LabMap ([Key], Id) VALUES ({S(l.Code)}, @LabId);");
            _sql.AppendLine();
        }
    }

    private void InsertPeople()
    {
        foreach (var p in _result.People)
        {
            _sql.AppendLine($"-- Person: {Comment(p.Name)}");
            _sql.AppendLine("DECLARE @PersonId int;");
            _sql.AppendLine(p.IsExternal
                ? $"SELECT @PersonId = p.Id FROM People p INNER JOIN Externs e ON e.Id = p.Id WHERE e.Name = {S(p.Name)};"
                : $"SELECT @PersonId = p.Id FROM People p INNER JOIN Interns i ON i.Id = p.Id WHERE i.Name = {S(p.Name)};");
            _sql.AppendLine("IF @PersonId IS NULL");
            _sql.AppendLine("BEGIN");
            _sql.AppendLine($"    INSERT INTO People (Status, Category, Email, PhoneNumber, CreatedDate, CreatedById) VALUES (0, {p.Category}, {S(p.Email)}, {S(p.Phone)}, @Today, @CreatedById);");
            _sql.AppendLine("    SET @PersonId = CONVERT(int, SCOPE_IDENTITY());");
            if (p.IsExternal)
            {
                _sql.AppendLine($"    INSERT INTO Externs (Id, IsEntity, Name, Address, ExternStatus) VALUES (@PersonId, {Bit(p.IsCompany)}, {S(p.Name)}, {S(p.Address ?? "Sin dato")}, 0);");
            }
            else
            {
                _sql.AppendLine($"    INSERT INTO Interns (Id, Name, InternStatus) VALUES (@PersonId, {S(p.Name)}, 0);");
            }
            _sql.AppendLine("END");
            _sql.AppendLine($"IF OBJECT_ID(N'PersonAliases', N'U') IS NOT NULL AND NOT EXISTS (SELECT 1 FROM PersonAliases WHERE PersonId = @PersonId AND IsPreferred = 1) INSERT INTO PersonAliases (PersonId, Alias, NormalizedAlias, IsPreferred, Source) VALUES (@PersonId, {S(p.Name)}, UPPER(LTRIM(RTRIM(REPLACE(REPLACE({S(p.Name)}, CHAR(13), N' '), CHAR(10), N' ')))), 1, N'Plantilla_Original.xlsx');");
            _sql.AppendLine($"IF OBJECT_ID(N'PersonRoleAssignments', N'U') IS NOT NULL AND NOT EXISTS (SELECT 1 FROM PersonRoleAssignments WHERE PersonId = @PersonId AND IsActive = 1) INSERT INTO PersonRoleAssignments (PersonId, Role, IsActive, ValidFrom) VALUES (@PersonId, {(p.Category == 1 ? 1 : p.Category == 5 ? 3 : 99)}, 1, @Today);");
            _sql.AppendLine($"INSERT INTO @PersonMap ([Key], Id) VALUES ({S(p.Name)}, @PersonId);");
            _sql.AppendLine();
        }
    }

    private void InsertManagements()
    {
        foreach (var m in _result.Managements)
        {
            _sql.AppendLine($"-- Management: {Comment(m.Code)}");
            _sql.AppendLine("DECLARE @ManagementId int;");
            _sql.AppendLine($"SELECT @ManagementId = Id FROM Managements WHERE Year = {m.Year} AND Semester = {m.Semester} AND Type = 0;");
            _sql.AppendLine("IF @ManagementId IS NULL");
            _sql.AppendLine("BEGIN");
            _sql.AppendLine($"    INSERT INTO Managements (Year, Semester, Code, Description, Status, Responsible, Type, CreatedDate, CreatedById) VALUES ({m.Year}, {m.Semester}, {S(m.Code)}, {S(m.Description)}, 2, N'Carga historica', 0, @Today, @CreatedById);");
            _sql.AppendLine("    SET @ManagementId = CONVERT(int, SCOPE_IDENTITY());");
            _sql.AppendLine("END");
            _sql.AppendLine($"INSERT INTO @ManagementMap ([Key], Id) VALUES ({S(m.Code)}, @ManagementId);");
            _sql.AppendLine();
        }
    }

    private void InsertEquipments()
    {
        foreach (var e in _result.Equipments)
        {
            _sql.AppendLine($"-- Equipment: {Comment(e.Name)}");
            _sql.AppendLine("DECLARE @EquipmentId int;");
            _sql.AppendLine($"SELECT TOP 1 @EquipmentId = Id FROM Equipments WHERE Name = {S(e.Name)} AND ISNULL(Brand, N'') = ISNULL({S(e.Brand)}, N'') AND ISNULL(Model, N'') = ISNULL({S(e.Model)}, N'');");
            _sql.AppendLine("IF @EquipmentId IS NULL");
            _sql.AppendLine("BEGIN");
            _sql.AppendLine($"    INSERT INTO Equipments (Category, UtensilType, TypeClassification, Status, Name, Brand, Model, UsefulLifeYears, Description, CreatedDate, CreatedById) VALUES ({e.Category}, 0, {e.TypeClassification}, 0, {S(e.Name)}, {S(e.Brand)}, {S(e.Model)}, {N(e.UsefulLifeYears)}, {S(e.Description)}, @Today, @CreatedById);");
            _sql.AppendLine("    SET @EquipmentId = CONVERT(int, SCOPE_IDENTITY());");
            _sql.AppendLine("END");
            _sql.AppendLine($"INSERT INTO @EquipmentMap ([Key], Id) VALUES ({S(e.Key)}, @EquipmentId);");
            _sql.AppendLine();
        }
    }

    private void InsertUnits()
    {
        foreach (var u in _result.Units)
        {
            _sql.AppendLine($"-- EquipmentUnit: {Comment(u.InventoryNumber)}");
            _sql.AppendLine("DECLARE @UnitId int;");
            _sql.AppendLine("DECLARE @UnitEquipmentId int;");
            _sql.AppendLine("DECLARE @UnitLabId int = NULL;");
            _sql.AppendLine("DECLARE @UnitCareerId int = NULL;");
            _sql.AppendLine("DECLARE @UnitManagementId int;");
            _sql.AppendLine($"SELECT @UnitEquipmentId = Id FROM @EquipmentMap WHERE [Key] = {S(u.EquipmentKey)};");
            if (!string.IsNullOrWhiteSpace(u.LaboratoryCode))
            {
                _sql.AppendLine($"SELECT @UnitLabId = Id FROM @LabMap WHERE [Key] = {S(u.LaboratoryCode)};");
            }
            if (!string.IsNullOrWhiteSpace(u.CareerName))
            {
                _sql.AppendLine($"SELECT @UnitCareerId = Id FROM @CareerMap WHERE [Key] = {S(u.CareerName)};");
            }
            _sql.AppendLine($"SELECT @UnitManagementId = Id FROM @ManagementMap WHERE [Key] = {S(_result.DefaultManagementCode)};");
            _sql.AppendLine($"SELECT @UnitId = Id FROM EquipmentUnits WHERE InventoryNumber = {S(u.InventoryNumber)};");
            _sql.AppendLine("IF @UnitId IS NULL");
            _sql.AppendLine("BEGIN");
            _sql.AppendLine($"    INSERT INTO EquipmentUnits (ManagementId, EquipmentId, LaboratoryId, LocationResolutionStatus, InventoryNumber, SerialNumber, CareerId, AcquisitionDate, ManufacturingDate, AcquisitionValue, CurrentStatus, PhysicalCondition, Notes, CreatedDate, CreatedById) VALUES (@UnitManagementId, @UnitEquipmentId, @UnitLabId, CASE WHEN @UnitLabId IS NULL THEN 0 ELSE 1 END, {S(u.InventoryNumber)}, {S(u.SerialNumber)}, @UnitCareerId, {D(u.AcquisitionDate)}, {D(u.ManufacturingDate)}, {N(u.AcquisitionValue)}, {u.Status}, {N(u.PhysicalCondition)}, {S(u.Notes)}, @Today, @CreatedById);");
            _sql.AppendLine("    SET @UnitId = CONVERT(int, SCOPE_IDENTITY());");
            _sql.AppendLine("END");
            _sql.AppendLine($"INSERT INTO @UnitMap ([Key], Id, EquipmentId, LaboratoryId) VALUES ({S(u.InventoryNumber)}, @UnitId, @UnitEquipmentId, @UnitLabId);");
            _sql.AppendLine();
        }
    }

    private void InsertRequests()
    {
        foreach (var r in _result.Requests)
        {
            _sql.AppendLine($"-- Request: {Comment(r.Key)}");
            _sql.AppendLine("DECLARE @RequestId int;");
            _sql.AppendLine("DECLARE @RequestUnitId int;");
            _sql.AppendLine("DECLARE @RequestEquipmentId int;");
            _sql.AppendLine("DECLARE @RequestLabId int;");
            _sql.AppendLine("DECLARE @RequestManagementId int;");
            _sql.AppendLine($"SELECT @RequestUnitId = Id, @RequestEquipmentId = EquipmentId, @RequestLabId = LaboratoryId FROM @UnitMap WHERE [Key] = {S(r.InventoryNumber)};");
            _sql.AppendLine($"SELECT @RequestManagementId = Id FROM @ManagementMap WHERE [Key] = {S(r.ManagementCode)};");
            _sql.AppendLine($"INSERT INTO Requests (LaboratoryId, LocationResolutionStatus, EquipmentId, EquipmentUnitId, ManagementId, Description, Priority, Observations, EstimatedRepairTime, Status, Type, InvestmentCode, CostCenter, CreatedDate, CreatedById) VALUES (@RequestLabId, CASE WHEN @RequestLabId IS NULL THEN 0 ELSE 1 END, @RequestEquipmentId, @RequestUnitId, @RequestManagementId, {S(r.Description)}, {r.Priority}, {S(r.Observations)}, {S(r.EstimatedRepairTime)}, {r.Status}, {r.Type}, {S(r.InvestmentCode)}, {S(r.CostCenter)}, {D(r.CreatedDate)}, @CreatedById);");
            _sql.AppendLine("SET @RequestId = CONVERT(int, SCOPE_IDENTITY());");
            _sql.AppendLine($"INSERT INTO @RequestMap ([Key], Id) VALUES ({S(r.Key)}, @RequestId);");
            _sql.AppendLine();
        }
    }

    private void InsertMaintenances()
    {
        foreach (var m in _result.Maintenances)
        {
            _sql.AppendLine($"-- Maintenance: {Comment(m.Key)}");
            _sql.AppendLine("DECLARE @MaintenanceId int;");
            _sql.AppendLine("DECLARE @MaintenanceUnitId int;");
            _sql.AppendLine("DECLARE @MaintenanceManagementId int;");
            _sql.AppendLine("DECLARE @TechnicianId int = NULL;");
            _sql.AppendLine($"SELECT @MaintenanceUnitId = Id FROM @UnitMap WHERE [Key] = {S(m.InventoryNumber)};");
            _sql.AppendLine($"SELECT @MaintenanceManagementId = Id FROM @ManagementMap WHERE [Key] = {S(m.ManagementCode)};");
            if (!string.IsNullOrWhiteSpace(m.TechnicianName))
            {
                _sql.AppendLine($"SELECT @TechnicianId = Id FROM @PersonMap WHERE [Key] = {S(m.TechnicianName)};");
            }
            _sql.AppendLine($"INSERT INTO Maintenances (EquipmentUnitId, MaintenanceType, ManagementId, ServiceType, TechnicianId, ScheduledDate, StartDate, EndDate, Description, Status, CompletionPercentage, Step1_Cleaning, Step2_Calibration, Step3_Testing, Step4_FinalReview, ActualCost, SatisfactionLevel, Recommendations, SuggestedNextMaintenanceDate, CreatedDate, CreatedById) VALUES (@MaintenanceUnitId, {m.MaintenanceType}, @MaintenanceManagementId, {m.ServiceType}, @TechnicianId, {D(m.ScheduledDate)}, {D(m.StartDate)}, {D(m.EndDate)}, {S(m.Description)}, {m.Status}, {(m.Status == 2 ? 100 : 0)}, 0, 0, 0, 0, {N(m.ActualCost)}, {N(m.SatisfactionLevel)}, {S(m.Recommendations)}, {D(m.NextMaintenanceDate)}, @Today, @CreatedById);");
            _sql.AppendLine("SET @MaintenanceId = CONVERT(int, SCOPE_IDENTITY());");
            _sql.AppendLine("IF @TechnicianId IS NOT NULL INSERT INTO MaintenanceParticipants (MaintenanceId, PersonId, Role, IsPrimary, IsActive, AssignedAt) VALUES (@MaintenanceId, @TechnicianId, 1, 1, 1, @Today);");
            _sql.AppendLine($"INSERT INTO @MaintenanceMap ([Key], Id) VALUES ({S(m.Key)}, @MaintenanceId);");
            _sql.AppendLine();
        }
    }

    private void InsertCostDetails()
    {
        foreach (var c in _result.CostDetails)
        {
            _sql.AppendLine("DECLARE @CostRequestId int = NULL;");
            _sql.AppendLine("DECLARE @CostMaintenanceId int = NULL;");
            if (c.TargetKind == "Request")
            {
                _sql.AppendLine($"SELECT @CostRequestId = Id FROM @RequestMap WHERE [Key] = {S(c.TargetKey)};");
            }
            else
            {
                _sql.AppendLine($"SELECT @CostMaintenanceId = Id FROM @MaintenanceMap WHERE [Key] = {S(c.TargetKey)};");
            }
            _sql.AppendLine($"IF @CostRequestId IS NOT NULL OR @CostMaintenanceId IS NOT NULL INSERT INTO CostDetails (RequestId, MaintenanceId, Concept, Description, Quantity, UnitOfMeasure, UnitPrice, Category, Provider, InvoiceNumber, CreatedDate, CreatedById) VALUES (@CostRequestId, @CostMaintenanceId, {S(c.Concept)}, {S(c.Description)}, {N(c.Quantity)}, {S(c.UnitOfMeasure)}, {N(c.UnitPrice)}, {c.Category}, {S(c.Provider)}, {S(c.InvoiceNumber)}, @Today, @CreatedById);");
            _sql.AppendLine();
        }
    }

    private void InsertDepartures()
    {
        foreach (var d in _result.Departures)
        {
            _sql.AppendLine($"-- Departure: {Comment(d.Key)}");
            _sql.AppendLine("DECLARE @DepartureId int;");
            _sql.AppendLine("DECLARE @DepartureUnitId int;");
            _sql.AppendLine("DECLARE @DepartureManagementId int;");
            _sql.AppendLine("DECLARE @BorrowerId int;");
            _sql.AppendLine($"SELECT @DepartureUnitId = Id FROM @UnitMap WHERE [Key] = {S(d.InventoryNumber)};");
            _sql.AppendLine($"SELECT @DepartureManagementId = Id FROM @ManagementMap WHERE [Key] = {S(d.ManagementCode)};");
            _sql.AppendLine($"SELECT @BorrowerId = Id FROM @PersonMap WHERE [Key] = {S(d.BorrowerName)};");
            _sql.AppendLine($"INSERT INTO Departures (ManagementId, EquipmentUnitId, BorrowerId, Type, DepartureDate, EstimatedReturnDate, DepartureObservations, Status, CreatedDate, CreatedById) VALUES (@DepartureManagementId, @DepartureUnitId, @BorrowerId, {d.Type}, {D(d.DepartureDate)}, {D(d.EstimatedReturnDate)}, {S(d.Observations)}, 0, @Today, @CreatedById);");
            _sql.AppendLine("SET @DepartureId = CONVERT(int, SCOPE_IDENTITY());");
            _sql.AppendLine($"INSERT INTO @DepartureMap ([Key], Id) VALUES ({S(d.Key)}, @DepartureId);");
            _sql.AppendLine();
        }
    }

    private void InsertDepartureItems()
    {
        foreach (var i in _result.DepartureItems)
        {
            _sql.AppendLine($"-- DepartureItem: {Comment(i.DepartureKey)} / {Comment(i.InventoryNumber)}");
            _sql.AppendLine("DECLARE @DepartureItemDepartureId int;");
            _sql.AppendLine("DECLARE @DepartureItemUnitId int = NULL;");
            _sql.AppendLine($"SELECT @DepartureItemDepartureId = Id FROM @DepartureMap WHERE [Key] = {S(i.DepartureKey)};");
            _sql.AppendLine($"SELECT @DepartureItemUnitId = Id FROM @UnitMap WHERE [Key] = {S(i.InventoryNumber)};");
            _sql.AppendLine($"IF @DepartureItemDepartureId IS NOT NULL INSERT INTO DepartureItems (DepartureId, EquipmentUnitId, ProductName, Quantity, UnitOfMeasure, Observations, IsRemoved, CreatedDate, CreatedById) VALUES (@DepartureItemDepartureId, @DepartureItemUnitId, {S(i.ProductName)}, {i.Quantity}, {S(i.UnitOfMeasure)}, {S(i.Observations)}, 0, @Today, @CreatedById);");
            _sql.AppendLine();
        }
    }

    private void InsertManagementPlans()
    {
        foreach (var p in _result.ManagementPlans)
        {
            _sql.AppendLine($"-- ManagementPlan: {Comment(p.Key)}");
            _sql.AppendLine("DECLARE @PlanManagementId int;");
            _sql.AppendLine("DECLARE @PlanUnitId int;");
            _sql.AppendLine($"SELECT @PlanManagementId = Id FROM @ManagementMap WHERE [Key] = {S(p.ManagementCode)};");
            _sql.AppendLine($"SELECT @PlanUnitId = Id FROM @UnitMap WHERE [Key] = {S(p.InventoryNumber)};");
            _sql.AppendLine($"INSERT INTO ManagementPlans (ManagementId, EquipmentUnitId, CurrentPhase, CurrentState, Responsible, PlannedDate, PlanStatus, IsDraft, CreatedDate, CreatedById) VALUES (@PlanManagementId, @PlanUnitId, {p.CurrentPhase}, {p.CurrentState}, {S(p.Responsible)}, {D(p.PlannedDate)}, {p.PlanStatus}, 0, @Today, @CreatedById);");
            _sql.AppendLine();
        }
    }

    private void Footer()
    {
        _sql.AppendLine("COMMIT TRANSACTION;");
        _sql.AppendLine("END TRY");
        _sql.AppendLine("BEGIN CATCH");
        _sql.AppendLine("    IF @@TRANCOUNT > 0 ROLLBACK TRANSACTION;");
        _sql.AppendLine("    THROW;");
        _sql.AppendLine("END CATCH;");
    }

    private static string S(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return "NULL";
        }

        return "N'" + value.Replace("'", "''") + "'";
    }

    private static string N(int? value) => value.HasValue ? value.Value.ToString(CultureInfo.InvariantCulture) : "NULL";
    private static string N(decimal? value) => value.HasValue ? value.Value.ToString(CultureInfo.InvariantCulture) : "NULL";
    private static string D(DateTime? value) => value.HasValue ? $"CONVERT(datetime2, '{value.Value:yyyy-MM-dd}', 23)" : "NULL";
    private static int Bit(bool value) => value ? 1 : 0;
    private static string Comment(string value) => value.Replace("\r", " ").Replace("\n", " ").Replace("--", "-");
}

internal sealed class SeedDataBuilder
{
    private readonly AnalysisResult _source;
    private readonly SeedResult _seed = new();
    private readonly Dictionary<string, SeedChainRecord> _chains = new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<string, UnitRecord> _units;
    private readonly Dictionary<string, PersonRecord> _people;

    public SeedDataBuilder(AnalysisResult source)
    {
        _source = source;
        _units = source.Units.ToDictionary(u => u.InventoryNumber, StringComparer.OrdinalIgnoreCase);
        _people = source.People.ToDictionary(p => p.Name, StringComparer.OrdinalIgnoreCase);
    }

    public SeedResult Build()
    {
        CopyMasterData();
        CopyTransactionalData();
        RegisterChains();
        CompleteChains();
        AddSeedFindings();
        return _seed;
    }

    private void CopyMasterData()
    {
        _seed.Faculties.AddRange(_source.Faculties);
        _seed.Careers.AddRange(_source.Careers);
        _seed.Laboratories.AddRange(_source.Laboratories);
        _seed.People.AddRange(_source.People);
        _seed.Managements.AddRange(_source.Managements);
        _seed.Equipments.AddRange(_source.Equipments);
        _seed.Units.AddRange(_source.Units);

        foreach (var f in _source.Faculties) Origin("Faculty", f.Name, "Excel", f.SourceSheet, f.SourceRow, "Maestro tomado del Excel.");
        foreach (var c in _source.Careers) Origin("Career", c.Name, "Excel", c.SourceSheet, c.SourceRow, "Maestro tomado del Excel.");
        foreach (var l in _source.Laboratories)
        {
            var origin = l.SourceSheet.Equals("Inferido", StringComparison.OrdinalIgnoreCase) ? "Inferido" : "Excel";
            var notes = origin == "Inferido"
                ? "Ubicacion tecnica creada para conservar FK obligatorias sin inventar una ubicacion fisica."
                : "Maestro tomado del Excel.";
            Origin("Laboratory", l.Code, origin, l.SourceSheet, l.SourceRow, notes);
        }
        foreach (var p in _source.People) Origin("Person", p.Name, "Excel", p.SourceSheet, p.SourceRow, "Persona/proveedor tomado del Excel o referencia real normalizada.");
        foreach (var m in _source.Managements) Origin("Management", m.Code, "Excel", "8 - Plan", m.SourceRow, "Gestion tomada del Excel.");
        foreach (var e in _source.Equipments) Origin("Equipment", e.Key, "Excel", e.SourceSheet, e.SourceRow, "Equipo derivado del catalogo real.");
        foreach (var u in _source.Units)
        {
            Origin(
                "EquipmentUnit",
                u.InventoryNumber,
                "Excel",
                u.SourceSheet,
                u.SourceRow,
                string.IsNullOrWhiteSpace(u.LaboratoryCode)
                    ? "Unidad derivada del inventario real; ubicacion fisica pendiente por ausencia de hoja 2 en la plantilla."
                    : "Unidad derivada del inventario real.");
        }
    }

    private void CopyTransactionalData()
    {
        _seed.Verifications.AddRange(_source.Verifications);
        _seed.VerificationFaults.AddRange(_source.VerificationFaults);
        _seed.Requests.AddRange(_source.Requests);
        _seed.Maintenances.AddRange(_source.Maintenances);
        _seed.CostDetails.AddRange(_source.CostDetails);
        _seed.Departures.AddRange(_source.Departures);
        _seed.DepartureItems.AddRange(_source.DepartureItems);
        _seed.UnresolvedVerifications.AddRange(_source.UnresolvedVerifications);

        foreach (var v in _source.Verifications) Origin("Verification", v.Key, "Excel", v.SourceSheet, v.SourceRow, "Verificacion con inventario real.");
        foreach (var f in _source.VerificationFaults) Origin("VerificationFault", f.VerificationKey, "Excel", f.SourceSheet, f.SourceRow, "Falla tomada del Excel.");
        foreach (var r in _source.Requests) Origin("Request", r.Key, "Excel", r.SourceSheet, r.SourceRow, r.Type == 2 ? "Solicitud de adquisicion tomada del Excel." : "Solicitud tecnica tomada del Excel.");
        foreach (var m in _source.Maintenances) Origin("Maintenance", m.Key, "Excel", m.SourceSheet, m.SourceRow, "Mantenimiento/Kardex tomado del Excel.");
        foreach (var c in _source.CostDetails) Origin("CostDetail", c.TargetKey, "Excel", c.SourceSheet, c.SourceRow, "Costo tomado del Excel.");
        foreach (var d in _source.Departures) Origin("Departure", d.Key, "Excel", d.SourceSheet, d.SourceRow, "Salida L-3 tomada del Excel.");
        foreach (var i in _source.DepartureItems) Origin("DepartureItem", i.DepartureKey, "Excel", i.SourceSheet, i.SourceRow, "Item L-3 tomado del Excel.");
    }

    private void RegisterChains()
    {
        foreach (var request in _seed.Requests)
        {
            AddChain(request.InventoryNumber, request.ManagementCode).Requests.Add(request);
        }

        foreach (var maintenance in _seed.Maintenances)
        {
            AddChain(maintenance.InventoryNumber, maintenance.ManagementCode).Maintenances.Add(maintenance);
        }

        // Departures (Salidas / L-3) are intentionally excluded from the seed lifecycle:
        // they are not fed into chains, so ManagementPlan.DepartureId stays null and no
        // Departure inserts are emitted (departure SQL is only produced per linked chain).
        // The reader still runs for the audit mode; only seed chain wiring skips them.

        foreach (var plan in _source.ManagementPlans)
        {
            AddChain(plan.InventoryNumber, plan.ManagementCode).OriginalPlans.Add(plan);
            Origin("ManagementPlan", plan.Key, "Excel", plan.SourceSheet, plan.SourceRow, "Plan L-48 tomado del Excel.");
        }
    }

    private SeedChainRecord AddChain(string inventoryNumber, string managementCode)
    {
        var key = $"{managementCode}|{inventoryNumber}";
        if (_chains.TryGetValue(key, out var existing))
        {
            return existing;
        }

        var chain = new SeedChainRecord(key, inventoryNumber, managementCode);
        _chains[key] = chain;
        return chain;
    }

    private void CompleteChains()
    {
        foreach (var chain in _chains.Values.OrderBy(c => c.ManagementCode).ThenBy(c => NaturalKey(c.InventoryNumber)))
        {
            if (!_units.TryGetValue(chain.InventoryNumber, out var unit))
            {
                Reject("Chain", chain.InventoryNumber, $"No se genero cadena porque el inventario '{chain.InventoryNumber}' no existe en catalogo/unidades.");
                continue;
            }

            chain.IsCorrective = DetectCorrective(chain);
            chain.TechnicalRequestKey = ResolveTechnicalRequest(chain, unit);
            chain.VerificationKey = ResolveVerification(chain);
            chain.MaintenanceKey = ResolveMaintenance(chain, unit);
            chain.LinkedDepartureKey = ResolveLinkedDeparture(chain);
            chain.KardexHistoryKey = ResolveKardexHistory(chain);
            chain.AcquisitionRequestKey = ResolveAcquisition(chain);
            EnsureMaintenanceCost(chain);
            ResolvePlanState(chain);
            _seed.Chains.Add(chain);
        }
    }

    private string? ResolveVerification(SeedChainRecord chain)
    {
        if (chain.IsCorrective)
        {
            return null;
        }

        var existing = _seed.Verifications.FirstOrDefault(v =>
            v.InventoryNumber.Equals(chain.InventoryNumber, StringComparison.OrdinalIgnoreCase)
            && v.ManagementCode.Equals(chain.ManagementCode, StringComparison.OrdinalIgnoreCase));
        if (existing != null)
        {
            return existing.Key;
        }

        if (chain.TechnicalRequestKey == null && !chain.Maintenances.Any() && !chain.Departures.Any() && !chain.OriginalPlans.Any())
        {
            return null;
        }

        var date = BestDate(chain).AddDays(-7);
        var key = $"INF_VER_{SanitizeKey(chain.ManagementCode)}_{SanitizeKey(chain.InventoryNumber)}";
        var verification = new VerificationRecord(
            key,
            chain.InventoryNumber,
            chain.ManagementCode,
            date,
            3,
            2,
            $"Verificacion preventiva inferida para completar la cadena historica del inventario {chain.InventoryNumber}.",
            "Inferido",
            0);
        _seed.Verifications.Add(verification);
        _seed.VerificationFaults.Add(new VerificationFaultRecord(key, "Observacion tecnica inferida desde solicitud/mantenimiento historico.", "Inferido", 0));
        Origin("Verification", key, "Inferido", "chain", 0, "L6 inferido para mantener continuidad preventiva antes de L7.");
        Origin("VerificationFault", key, "Inferido", "chain", 0, "Falla inferida para justificar continuidad hacia L7.");
        return key;
    }

    private string? ResolveTechnicalRequest(SeedChainRecord chain, UnitRecord unit)
    {
        var existing = chain.Requests
            .Where(r => r.Type == 1)
            .OrderBy(r => r.CreatedDate)
            .ThenBy(r => r.Key)
            .FirstOrDefault();
        if (existing != null)
        {
            return existing.Key;
        }

        if (!chain.Maintenances.Any() && !chain.Departures.Any(d => IsMaintenanceDeparture(d)) && !chain.OriginalPlans.Any() && !chain.Requests.Any(r => r.Type == 2))
        {
            return null;
        }

        var key = $"INF_REQ_{SanitizeKey(chain.ManagementCode)}_{SanitizeKey(chain.InventoryNumber)}";
        var description = chain.IsCorrective
            ? $"Solicitud correctiva inferida para atender el mantenimiento historico del inventario {chain.InventoryNumber}."
            : $"Solicitud tecnica preventiva inferida para completar la cadena historica del inventario {chain.InventoryNumber}.";
        var request = new RequestRecord(
            key,
            chain.InventoryNumber,
            unit.LaboratoryCode,
            unit.EquipmentKey,
            chain.ManagementCode,
            1,
            chain.Maintenances.Any() ? 3 : 0,
            1,
            BestDate(chain).AddDays(-5),
            description,
            "Registro inferido desde cadena historica del Excel.",
            null,
            null,
            null,
            "Inferido",
            0);
        _seed.Requests.Add(request);
        chain.Requests.Add(request);
        Origin("Request", key, "Inferido", "chain", 0, chain.IsCorrective ? "L7 correctivo inferido; correctivo no recibe L6." : "L7 inferido para completar cadena preventiva.");
        return key;
    }

    private string? ResolveMaintenance(SeedChainRecord chain, UnitRecord unit)
    {
        var existing = chain.Maintenances
            .OrderByDescending(m => m.Status)
            .ThenBy(m => m.Key)
            .FirstOrDefault();
        if (existing != null)
        {
            return existing.Key;
        }

        var needsMaintenance = chain.Requests.Any(r => r.Type == 2)
            || chain.Departures.Any(IsMaintenanceDeparture)
            || chain.OriginalPlans.Any(p => p.PlanStatus == 2);
        if (!needsMaintenance)
        {
            return null;
        }

        var key = $"INF_MNT_{SanitizeKey(chain.ManagementCode)}_{SanitizeKey(chain.InventoryNumber)}";
        var baseDate = BestDate(chain);
        var technician = ResolveTechnician(chain);
        var completed = chain.Requests.Any(r => r.Type == 2) || chain.Departures.Any(IsMaintenanceDeparture) || chain.OriginalPlans.Any(p => p.PlanStatus == 2);
        var maintenance = new MaintenanceRecord(
            key,
            chain.InventoryNumber,
            chain.ManagementCode,
            chain.IsCorrective ? 2 : 1,
            chain.Departures.Any(d => d.Type == 3) ? 1 : 0,
            technician,
            chain.TechnicalRequestKey,
            baseDate.AddDays(-3),
            completed ? baseDate.AddDays(-2) : null,
            completed ? baseDate.AddDays(-1) : null,
            completed
                ? $"Mantenimiento historico inferido para completar Kardex/L-48 del inventario {chain.InventoryNumber}."
                : $"Mantenimiento programado inferido para el inventario {chain.InventoryNumber}.",
            completed ? 2 : 0,
            null,
            completed ? 4 : null,
            completed ? "Equipo revisado y habilitado para continuidad del flujo historico." : null,
            completed ? baseDate.AddMonths(6) : null,
            "Inferido",
            0);
        _seed.Maintenances.Add(maintenance);
        chain.Maintenances.Add(maintenance);
        Origin("Maintenance", key, "Inferido", "chain", 0, "L8 inferido para enlazar solicitud, salida, Kardex o desembolso.");
        return key;
    }

    private string? ResolveLinkedDeparture(SeedChainRecord chain)
    {
        if (chain.MaintenanceKey == null)
        {
            return null;
        }

        return chain.Departures
            .Where(IsMaintenanceDeparture)
            .OrderBy(d => d.DepartureDate)
            .Select(d => d.Key)
            .FirstOrDefault();
    }

    private string? ResolveKardexHistory(SeedChainRecord chain)
    {
        var maintenance = _seed.Maintenances.FirstOrDefault(m => m.Key.Equals(chain.MaintenanceKey, StringComparison.OrdinalIgnoreCase));
        if (maintenance == null || maintenance.Status != 2)
        {
            return null;
        }

        var key = $"INF_KDX_{SanitizeKey(chain.ManagementCode)}_{SanitizeKey(chain.InventoryNumber)}";
        var date = maintenance.EndDate ?? BestDate(chain);
        _seed.KardexHistories.Add(new KardexHistoryRecord(key, chain.InventoryNumber, 0, date, $"Kardex historico inferido desde mantenimiento {maintenance.Key}.", "Inferido", 0));
        Origin("EquipmentStateHistory", key, "Inferido", "chain", 0, "Historial Kardex inferido al cerrar mantenimiento completado.");
        return key;
    }

    private string? ResolveAcquisition(SeedChainRecord chain)
    {
        return chain.Requests
            .Where(r => r.Type == 2)
            .OrderBy(r => r.CreatedDate)
            .ThenBy(r => r.Key)
            .Select(r => r.Key)
            .FirstOrDefault();
    }

    private void EnsureMaintenanceCost(SeedChainRecord chain)
    {
        if (chain.MaintenanceKey == null)
        {
            return;
        }

        var hasMaintenanceCost = _seed.CostDetails.Any(c =>
            c.TargetKind.Equals("Maintenance", StringComparison.OrdinalIgnoreCase)
            && c.TargetKey.Equals(chain.MaintenanceKey, StringComparison.OrdinalIgnoreCase));
        if (hasMaintenanceCost)
        {
            return;
        }

        var purchaseCosts = _seed.CostDetails
            .Where(c => c.TargetKind.Equals("Request", StringComparison.OrdinalIgnoreCase)
                && chain.AcquisitionRequestKey != null
                && c.TargetKey.Equals(chain.AcquisitionRequestKey, StringComparison.OrdinalIgnoreCase))
            .ToList();

        if (purchaseCosts.Count > 0)
        {
            foreach (var cost in purchaseCosts)
            {
                _seed.CostDetails.Add(new CostDetailRecord(
                    "Maintenance",
                    chain.MaintenanceKey,
                    TruncateRequired($"{cost.Concept} (Kardex)", 200),
                    "Copia inferida para reflejar el costo en Kardex/L-8.",
                    cost.Quantity,
                    cost.UnitOfMeasure,
                    cost.UnitPrice,
                    cost.Category,
                    cost.Provider,
                    cost.InvoiceNumber,
                    "Inferido",
                    0));
                Origin("CostDetail", chain.MaintenanceKey, "Inferido", "chain", 0, "Costo copiado para que Kardex y L-12 reflejen el mismo desembolso historico.");
            }
            return;
        }

        var unitPrice = chain.IsCorrective ? 250m : 180m;
        _seed.CostDetails.Add(new CostDetailRecord(
            "Maintenance",
            chain.MaintenanceKey,
            chain.IsCorrective ? "Servicio correctivo historico inferido" : "Servicio preventivo historico inferido",
            "Costo tecnico inferido para completar cierre de Kardex.",
            1,
            "servicio",
            unitPrice,
            5,
            null,
            null,
            "Inferido",
            0));
        Origin("CostDetail", chain.MaintenanceKey, "Inferido", "chain", 0, "Costo minimo inferido para permitir cierre de mantenimiento en pruebas.");
    }

    private void ResolvePlanState(SeedChainRecord chain)
    {
        if (chain.AcquisitionRequestKey != null)
        {
            chain.CurrentPhase = 6;
            chain.CurrentState = 9;
            chain.PlanStatus = 2;
        }
        else if (chain.KardexHistoryKey != null)
        {
            chain.CurrentPhase = 6;
            chain.CurrentState = 8;
            chain.PlanStatus = 1;
        }
        else if (chain.LinkedDepartureKey != null)
        {
            chain.CurrentPhase = 5;
            chain.CurrentState = 7;
            chain.PlanStatus = 1;
        }
        else if (chain.MaintenanceKey != null)
        {
            var maintenance = _seed.Maintenances.FirstOrDefault(m => m.Key.Equals(chain.MaintenanceKey, StringComparison.OrdinalIgnoreCase));
            chain.CurrentPhase = maintenance?.Status == 2 ? 4 : 3;
            chain.CurrentState = maintenance?.Status == 2 ? 6 : 5;
            chain.PlanStatus = maintenance?.Status == 2 ? 2 : 1;
        }
        else if (chain.TechnicalRequestKey != null)
        {
            chain.CurrentPhase = 3;
            chain.CurrentState = 4;
            chain.PlanStatus = 1;
        }
        else if (chain.VerificationKey != null)
        {
            chain.CurrentPhase = 2;
            chain.CurrentState = 3;
            chain.PlanStatus = 1;
        }
        else
        {
            chain.CurrentPhase = 1;
            chain.CurrentState = 1;
            chain.PlanStatus = 0;
        }

        var plan = chain.OriginalPlans.OrderByDescending(p => p.PlanStatus).FirstOrDefault();
        chain.PlanKey = plan?.Key ?? $"INF_PLAN_{SanitizeKey(chain.ManagementCode)}_{SanitizeKey(chain.InventoryNumber)}";
        chain.Responsible = Coalesce(plan?.Responsible, ResolveTechnician(chain));
        chain.PlannedDate = plan?.PlannedDate ?? BestDate(chain);
        Origin("ManagementPlan", chain.PlanKey, plan == null ? "Inferido" : "Excel+Inferido", plan?.SourceSheet ?? "chain", plan?.SourceRow ?? 0, "Plan L-48 enlazado con las FK disponibles del wizard.");
    }

    private bool DetectCorrective(SeedChainRecord chain)
    {
        if (chain.Maintenances.Any(m => m.MaintenanceType == 2))
        {
            return true;
        }

        return chain.Requests.Any(r =>
            RemoveDiacritics(r.Description).Contains("CORRECT", StringComparison.OrdinalIgnoreCase)
            || RemoveDiacritics(r.Observations ?? string.Empty).Contains("CORRECT", StringComparison.OrdinalIgnoreCase));
    }

    private string ResolveTechnician(SeedChainRecord chain)
    {
        var explicitName = chain.Maintenances.Select(m => m.TechnicianName).FirstOrDefault(n => !string.IsNullOrWhiteSpace(n))
            ?? chain.OriginalPlans.Select(p => p.Responsible).FirstOrDefault(n => !string.IsNullOrWhiteSpace(n))
            ?? chain.Departures.Select(d => d.BorrowerName).FirstOrDefault(n => !string.IsNullOrWhiteSpace(n));

        if (!string.IsNullOrWhiteSpace(explicitName))
        {
            EnsurePerson(explicitName);
            return explicitName;
        }

        return _people.Keys.FirstOrDefault(k => k.Contains("TALLER", StringComparison.OrdinalIgnoreCase))
            ?? _people.Keys.FirstOrDefault()
            ?? EnsurePerson("TECNICO HISTORICO");
    }

    private string EnsurePerson(string name)
    {
        if (_people.ContainsKey(name))
        {
            return name;
        }

        var person = new PersonRecord(name, false, false, 99, null, null, null, "Inferido", 0);
        _people[name] = person;
        _seed.People.Add(person);
        Origin("Person", name, "Inferido", "chain", 0, "Persona inferida por referencia historica en una cadena.");
        return name;
    }

    private DateTime BestDate(SeedChainRecord chain)
    {
        var dates = new List<DateTime>();
        dates.AddRange(chain.Requests.Select(r => r.CreatedDate));
        dates.AddRange(chain.Maintenances.SelectMany(m => new[] { m.ScheduledDate, m.StartDate, m.EndDate }).Where(d => d.HasValue).Select(d => d!.Value));
        dates.AddRange(chain.Departures.Select(d => d.DepartureDate));
        dates.AddRange(chain.OriginalPlans.Select(p => p.PlannedDate).Where(d => d.HasValue).Select(d => d!.Value));
        return dates.Count > 0 ? dates.Min() : ManagementStart(chain.ManagementCode);
    }

    private DateTime ManagementStart(string code)
    {
        var management = _source.Managements.FirstOrDefault(m => m.Code.Equals(code, StringComparison.OrdinalIgnoreCase));
        if (management == null)
        {
            return DateTime.Today;
        }

        return new DateTime(management.Year, management.Semester == 2 ? 7 : 1, 15);
    }

    private static bool IsMaintenanceDeparture(DepartureRecord departure) => departure.Type is 3 or 5;

    private void AddSeedFindings()
    {
        _seed.Findings.Add("Modo seed: genera MERGE idempotente offline; no ejecuta SQL.");
        _seed.Findings.Add("Inferencia limitada a inventarios con registros transaccionales reales en hojas 4-8.");
        _seed.Findings.Add("Correctivos empiezan en L7; no se infiere L6 para correctivos.");
        _seed.Findings.Add("Las verificaciones sin inventario real permanecen fuera del seed.");
    }

    private void Origin(string entity, string key, string origin, string sourceSheet, int sourceRow, string notes)
    {
        _seed.Origins.Add(new SeedOriginRecord(entity, key, origin, sourceSheet, sourceRow, notes));
    }

    private void Reject(string entity, string key, string reason)
    {
        _seed.Rejects.Add(new SeedRejectRecord(entity, key, reason));
    }

    private static string Coalesce(params string?[] values)
    {
        foreach (var value in values)
        {
            if (!string.IsNullOrWhiteSpace(value))
            {
                return value;
            }
        }

        return string.Empty;
    }

    private static string NaturalKey(string value) => value.PadLeft(20, '0');

    private static string SanitizeKey(string value)
    {
        var builder = new StringBuilder();
        foreach (var ch in value)
        {
            if (char.IsLetterOrDigit(ch))
            {
                builder.Append(char.ToUpperInvariant(ch));
            }
        }

        return builder.Length == 0 ? "NA" : builder.ToString();
    }

    private static string TruncateRequired(string value, int max)
    {
        value = value.Trim();
        return value.Length <= max ? value : value[..max];
    }

    private static string RemoveDiacritics(string value)
    {
        var normalized = value.Normalize(NormalizationForm.FormD);
        var builder = new StringBuilder();
        foreach (var c in normalized)
        {
            if (CharUnicodeInfo.GetUnicodeCategory(c) != UnicodeCategory.NonSpacingMark)
            {
                builder.Append(c);
            }
        }

        return builder.ToString().Normalize(NormalizationForm.FormC);
    }
}

internal sealed class SeedOutputWriter
{
    private readonly string _outputDir;
    private readonly SeedResult _seed;

    public SeedOutputWriter(string outputDir, SeedResult seed)
    {
        _outputDir = outputDir;
        _seed = seed;
    }

    public void WriteAll()
    {
        var script = new SeedSqlScriptBuilder(_seed).Build();
        File.WriteAllText(Path.Combine(_outputDir, "load-seed.sql"), script.FullScript, Encoding.UTF8);
        File.WriteAllText(Path.Combine(_outputDir, "00-master-data.sql"), script.MasterData, Encoding.UTF8);
        File.WriteAllText(Path.Combine(_outputDir, "01-equipment.sql"), script.Equipment, Encoding.UTF8);
        File.WriteAllText(Path.Combine(_outputDir, "02-preventive-chains.sql"), script.PreventiveChains, Encoding.UTF8);
        File.WriteAllText(Path.Combine(_outputDir, "03-corrective-chains.sql"), script.CorrectiveChains, Encoding.UTF8);
        File.WriteAllText(Path.Combine(_outputDir, "04-acquisitions-costs.sql"), script.AcquisitionsAndCosts, Encoding.UTF8);
        File.WriteAllText(Path.Combine(_outputDir, "05-verification-quarantine.sql"), script.VerificationQuarantine, Encoding.UTF8);
        WriteSummary();
        WriteOrigin();
        WriteRejects();
        WriteClassificationReview();
    }

    private void WriteSummary()
    {
        var builder = new StringBuilder();
        builder.AppendLine("# Seed Data Historico");
        builder.AppendLine();
        builder.AppendLine("## Resumen");
        builder.AppendLine($"- Faculties: {_seed.Faculties.Count}");
        builder.AppendLine($"- Careers: {_seed.Careers.Count}");
        builder.AppendLine($"- Laboratories: {_seed.Laboratories.Count}");
        builder.AppendLine($"- People/Providers: {_seed.People.Count}");
        builder.AppendLine($"- Managements: {_seed.Managements.Count}");
        builder.AppendLine($"- Equipments: {_seed.Equipments.Count}");
        builder.AppendLine($"- EquipmentUnits: {_seed.Units.Count}");
        builder.AppendLine($"- Chains: {_seed.Chains.Count}");
        builder.AppendLine($"- Preventive chains: {_seed.Chains.Count(c => !c.IsCorrective)}");
        builder.AppendLine($"- Corrective chains: {_seed.Chains.Count(c => c.IsCorrective)}");
        builder.AppendLine($"- Verifications: {_seed.Verifications.Count}");
        builder.AppendLine($"- Requests: {_seed.Requests.Count}");
        builder.AppendLine($"- Maintenances: {_seed.Maintenances.Count}");
        builder.AppendLine($"- Departures: {_seed.Departures.Count}");
        builder.AppendLine($"- Kardex histories: {_seed.KardexHistories.Count}");
        builder.AppendLine($"- CostDetails: {_seed.CostDetails.Count}");
        builder.AppendLine($"- Origins Excel: {_seed.Origins.Count(o => o.Origin == "Excel" || o.Origin == "Excel+Inferido")}");
        builder.AppendLine($"- Origins Inferido: {_seed.Origins.Count(o => o.Origin == "Inferido")}");
        builder.AppendLine($"- Seed rejects: {_seed.Rejects.Count}");
        builder.AppendLine();
        builder.AppendLine("## Hallazgos");
        foreach (var finding in _seed.Findings)
        {
            builder.AppendLine($"- {finding}");
        }

        builder.AppendLine();
        builder.AppendLine("## Verificaciones sin inventario (no enlazadas)");
        builder.AppendLine($"- Total unresolved verifications: {_seed.UnresolvedVerifications.Count}");
        builder.AppendLine("- These L-6 rows could NOT be linked to an EquipmentUnit (no inventory COD or unknown COD). They are preserved in HistoricalVerificationQuarantines, NOT inserted into the operational Verifications table, and NOT fabricated. Name-based linkage is a deferred decision.");
        foreach (var uv in _seed.UnresolvedVerifications)
        {
            builder.AppendLine($"  - Row {uv.SourceRow} [{uv.SourceSheet}] COD='{uv.InventoryRaw}' Name='{uv.EquipmentName}' ({uv.Reason})");
        }

        builder.AppendLine();
        builder.AppendLine("## Cadenas");
        foreach (var chain in _seed.Chains)
        {
            builder.AppendLine($"- {chain.ManagementCode} / {chain.InventoryNumber}: Correctivo={chain.IsCorrective}; L6={chain.VerificationKey ?? "N/A"}; L7={chain.TechnicalRequestKey ?? "N/A"}; L8={chain.MaintenanceKey ?? "N/A"}; L3={chain.LinkedDepartureKey ?? "N/A"}; Kardex={chain.KardexHistoryKey ?? "N/A"}; L12={chain.AcquisitionRequestKey ?? "N/A"}; State={chain.CurrentPhase}/{chain.CurrentState}");
        }

        File.WriteAllText(Path.Combine(_outputDir, "seed-summary.md"), builder.ToString(), Encoding.UTF8);
    }

    private void WriteOrigin()
    {
        var rows = _seed.Origins.Select(o => new[]
        {
            o.Entity,
            o.Key,
            o.Origin,
            o.SourceSheet,
            o.SourceRow.ToString(CultureInfo.InvariantCulture),
            o.Notes
        });
        WriteCsv(Path.Combine(_outputDir, "seed-origin.csv"), ["Entity", "Key", "Origin", "SourceSheet", "SourceRow", "Notes"], rows);
    }

    private void WriteRejects()
    {
        var rows = _seed.Rejects.Select(r => new[] { r.Entity, r.Key, r.Reason });
        WriteCsv(Path.Combine(_outputDir, "seed-rejects.csv"), ["Entity", "Key", "Reason"], rows);
    }

    private void WriteClassificationReview()
    {
        var review = ClassificationReviewBuilder.Build(_seed);
        WriteCsv(Path.Combine(_outputDir, "classification-review.csv"), review.Headers, review.Rows);
        File.WriteAllText(Path.Combine(_outputDir, "classification-correction.sql"), review.Sql, Encoding.UTF8);
    }

    private static void WriteCsv(string path, string[] headers, IEnumerable<string[]> rows)
    {
        var builder = new StringBuilder();
        builder.AppendLine(string.Join(",", headers.Select(Csv)));
        foreach (var row in rows)
        {
            builder.AppendLine(string.Join(",", row.Select(Csv)));
        }

        File.WriteAllText(path, builder.ToString(), Encoding.UTF8);
    }

    private static string Csv(string? value)
    {
        value ??= string.Empty;
        return $"\"{value.Replace("\"", "\"\"")}\"";
    }
}

internal sealed class ClassificationReview
{
    public string[] Headers { get; init; } = [];
    public List<string[]> Rows { get; init; } = [];
    public string Sql { get; init; } = string.Empty;
}

internal static class ClassificationReviewBuilder
{
    public static ClassificationReview Build(SeedResult seed)
    {
        var unitCounts = seed.Units
            .GroupBy(u => u.EquipmentKey, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(g => g.Key, g => g.Count(), StringComparer.OrdinalIgnoreCase);

        var rows = new List<string[]>();
        var sql = new StringBuilder();
        sql.AppendLine("-- Classification correction review");
        sql.AppendLine("-- Generated offline from Excel-derived seed data. Review classification-review.csv before executing.");
        sql.AppendLine("SET XACT_ABORT ON;");
        sql.AppendLine("BEGIN TRANSACTION;");
        sql.AppendLine();

        var changes = 0;
        foreach (var equipment in seed.Equipments.OrderBy(e => e.Name).ThenBy(e => e.Brand).ThenBy(e => e.Model))
        {
            var suggestion = Classify(equipment);
            var unitCount = unitCounts.TryGetValue(equipment.Key, out var count) ? count : 0;
            var changesClassification = equipment.Category != suggestion.Category
                || equipment.TypeClassification != suggestion.TypeClassification
                || suggestion.UtensilType != 0;

            rows.Add([
                equipment.Key,
                equipment.Name,
                equipment.Brand ?? string.Empty,
                equipment.Model ?? string.Empty,
                unitCount.ToString(CultureInfo.InvariantCulture),
                equipment.Category.ToString(CultureInfo.InvariantCulture),
                "0",
                equipment.TypeClassification.ToString(CultureInfo.InvariantCulture),
                suggestion.Category.ToString(CultureInfo.InvariantCulture),
                suggestion.UtensilType.ToString(CultureInfo.InvariantCulture),
                suggestion.TypeClassification.ToString(CultureInfo.InvariantCulture),
                suggestion.Rule,
                changesClassification ? "UPDATE" : "NO_CHANGE"
            ]);

            if (!changesClassification)
            {
                continue;
            }

            changes++;
            sql.AppendLine($"-- {SqlComment(equipment.Name)} | Units: {unitCount} | Rule: {SqlComment(suggestion.Rule)}");
            sql.AppendLine("UPDATE Equipments");
            sql.AppendLine($"SET Category = {suggestion.Category}, UtensilType = {suggestion.UtensilType}, TypeClassification = {suggestion.TypeClassification}, LastModifiedDate = SYSUTCDATETIME()");
            sql.AppendLine($"WHERE Name = {SqlString(equipment.Name)}");
            sql.AppendLine($"  AND ISNULL(Brand, N'') = ISNULL({SqlString(equipment.Brand)}, N'')");
            sql.AppendLine($"  AND ISNULL(Model, N'') = ISNULL({SqlString(equipment.Model)}, N'');");
            sql.AppendLine();
        }

        sql.AppendLine($"-- Suggested equipment catalog updates: {changes}");
        sql.AppendLine("COMMIT TRANSACTION;");

        return new ClassificationReview
        {
            Headers =
            [
                "EquipmentKey",
                "Name",
                "Brand",
                "Model",
                "UnitCount",
                "CurrentCategory",
                "CurrentUtensilType",
                "CurrentTypeClassification",
                "SuggestedCategory",
                "SuggestedUtensilType",
                "SuggestedTypeClassification",
                "Rule",
                "Action"
            ],
            Rows = rows,
            Sql = sql.ToString()
        };
    }

    private static ClassificationSuggestion Classify(EquipmentRecord equipment)
    {
        var text = Normalize(string.Join(" ", equipment.Name, equipment.Brand, equipment.Model, equipment.Description));

        if (Any(text, "EXTINTOR", "BOTIQUIN", "CASCO", "GUANTE", "MASCARA", "SENAL"))
            return new(0, 0, 13, "Equipo: seguridad industrial por nombre/descripcion.");
        if (Any(text, "PROYECTOR", "TELEVISOR", "TV ", "PARLANTE", "MICROFONO", "CAMARA", "VIDEO"))
            return new(0, 0, 14, "Equipo: audiovisual por nombre/descripcion.");
        if (Any(text, "BALANZA", "TERMOMETRO", "MEDIDOR", "PCC", "MANOMETRO", "BASCULA"))
            return new(0, 0, 3, "Equipo: medicion/PCC por nombre/descripcion.");
        if (Any(text, "REFRIG", "FRIGORIF", "HELADERA", "ABATIDOR", "VITRINA FRIA", "ENFRIADOR"))
            return new(0, 0, 9, "Equipo: frio por nombre/descripcion.");
        if (Any(text, "CONGEL", "FREEZER"))
            return new(0, 0, 10, "Equipo: congelacion por nombre/descripcion.");
        if (Any(text, "HORNO", "COCINA", "PLANCHA", "FREIDORA", "MARMITA", "TERMO TANQUE", "TERMOTANQUE", "BANO MARIA", "CAFETERA IND"))
            return new(0, 0, 8, "Equipo: calor por nombre/descripcion.");
        if (Any(text, "BATIDORA", "LICUADORA", "AMASADORA", "MOLINO", "PROCESADOR", "EXTRACTOR", "CORTADORA", "SOBADORA"))
            return new(0, 0, 12, "Equipo: maquina rotativa por nombre/descripcion.");
        if (Any(text, "ALL IN ONE", "ACCES POINT", "ACCESS POINT", "COMPUTAD", "IMPRESORA", "ROUTER", "SWITCH", "TELEFONO", "TELEFONICO"))
            return new(0, 0, 0, "Equipo: electronico/informatico por nombre/descripcion.");
        if (Any(text, "ESTANTE", "MESA", "SILLA", "BANCA", "ESCRITORIO", "GABINETE", "VITRINA"))
            return new(2, 0, 7, "Otro: mobiliario/infraestructura sin mantenimiento tecnico directo.");

        if (Any(text, "MANTEL", "SERVILLETA", "CUBREMANTEL", "CAMINO DE MESA"))
            return new(1, 9, 7, "Utensilio: manteleria por nombre/descripcion.");
        if (Any(text, "PLATO", "TAZA", "VASO", "COPA", "BOWL", "FUENTE", "BANDEJA", "CUBIERTO", "CUCHARA", "CUCHILLO", "TENEDOR", "JARRA"))
            return new(1, 10, 7, "Utensilio: vajilla/servicio por nombre/descripcion.");
        if (Any(text, "COCTEL", "SHAKER", "BARISMO", "CAFETERA MOKA", "ESPUMADOR"))
            return new(1, 6, 7, "Utensilio: bartending/barismo por nombre/descripcion.");
        if (Any(text, "MOLDE", "RODILLO", "MANGA", "BOQUILLA", "PASTELER", "REPOSTER"))
            return new(1, 7, 7, "Utensilio: panaderia/reposteria por nombre/descripcion.");
        if (Any(text, "OLLA", "SARTEN", "ESPATULA", "PINZA", "COLADOR", "TABLA", "CUCHARON", "RALLADOR", "CUCHARETA"))
            return new(1, 5, 7, "Utensilio: menaje de cocina por nombre/descripcion.");

        if (Any(text, "EXTINTOR", "BOTIQUIN", "CASCO", "GUANTE", "MASCARA", "SENAL"))
            return new(0, 0, 13, "Equipo: seguridad industrial por nombre/descripcion.");
        if (Any(text, "PROYECTOR", "TELEVISOR", "TV ", "PARLANTE", "MICROFONO", "CAMARA", "VIDEO"))
            return new(0, 0, 14, "Equipo: audiovisual por nombre/descripcion.");
        if (Any(text, "BALANZA", "TERMOMETRO", "MEDIDOR", "PCC", "MANOMETRO", "BASCULA"))
            return new(0, 0, 3, "Equipo: medicion/PCC por nombre/descripcion.");
        if (Any(text, "REFRIG", "FRIGORIF", "HELADERA", "ABATIDOR", "VITRINA FRIA", "ENFRIADOR"))
            return new(0, 0, 9, "Equipo: frio por nombre/descripcion.");
        if (Any(text, "CONGEL", "FREEZER"))
            return new(0, 0, 10, "Equipo: congelacion por nombre/descripcion.");
        if (Any(text, "HORNO", "COCINA", "PLANCHA", "FREIDORA", "MARMITA", "TERMO TANQUE", "TERMOTANQUE", "BANO MARIA", "CAFETERA IND"))
            return new(0, 0, 8, "Equipo: calor por nombre/descripcion.");
        if (Any(text, "BATIDORA", "LICUADORA", "AMASADORA", "MOLINO", "PROCESADOR", "EXTRACTOR", "CORTADORA", "SOBADORA"))
            return new(0, 0, 12, "Equipo: maquina rotativa por nombre/descripcion.");
        if (Any(text, "ALL IN ONE", "ACCES POINT", "ACCESS POINT", "COMPUTAD", "IMPRESORA", "ROUTER", "SWITCH", "TELEFONO", "TELEFONICO"))
            return new(0, 0, 0, "Equipo: electronico/informatico por nombre/descripcion.");
        if (Any(text, "ESTANTE", "MESA", "SILLA", "BANCA", "ESCRITORIO", "GABINETE", "VITRINA"))
            return new(2, 0, 7, "Otro: mobiliario/infraestructura sin mantenimiento tecnico directo.");

        return new(0, 0, 7, "Sin regla especifica: revisar manualmente.");
    }

    private static bool Any(string text, params string[] terms)
    {
        return terms.Any(text.Contains);
    }

    private static string Normalize(string value)
    {
        var formD = value.ToUpperInvariant().Normalize(NormalizationForm.FormD);
        var builder = new StringBuilder(formD.Length);
        foreach (var ch in formD)
        {
            if (CharUnicodeInfo.GetUnicodeCategory(ch) != UnicodeCategory.NonSpacingMark)
            {
                builder.Append(ch);
            }
        }

        return builder.ToString().Normalize(NormalizationForm.FormC);
    }

    private static string SqlString(string? value)
    {
        return value == null ? "NULL" : $"N'{value.Replace("'", "''")}'";
    }

    private static string SqlComment(string value)
    {
        return value.Replace("\r", " ").Replace("\n", " ");
    }

    private sealed record ClassificationSuggestion(int Category, int UtensilType, int TypeClassification, string Rule);
}

internal sealed class SeedSqlScriptBuilder
{
    private readonly SeedResult _seed;
    private readonly StringBuilder _master = new();
    private readonly StringBuilder _equipment = new();
    private readonly StringBuilder _preventive = new();
    private readonly StringBuilder _corrective = new();
    private readonly StringBuilder _acquisitions = new();
    private readonly StringBuilder _plans = new();
    private readonly StringBuilder _quarantine = new();

    public SeedSqlScriptBuilder(SeedResult seed)
    {
        _seed = seed;
    }

    public SeedScript Build()
    {
        BuildMasterData();
        BuildVerificationQuarantine();
        BuildEquipment();
        BuildChains(_seed.Chains.Where(c => !c.IsCorrective), _preventive, "PREVENTIVO");
        BuildChains(_seed.Chains.Where(c => c.IsCorrective), _corrective, "CORRECTIVO");
        BuildUnlinkedHistory();
        BuildAcquisitionsAndCosts();
        BuildPlans();

        var full = new StringBuilder();
        full.Append(Header());
        full.Append(_master);
        full.Append(_quarantine);
        full.Append(_equipment);
        full.Append(_preventive);
        full.Append(_corrective);
        full.Append(_acquisitions);
        full.Append(_plans);
        full.AppendLine("COMMIT TRANSACTION;");
        full.AppendLine("END TRY");
        full.AppendLine("BEGIN CATCH");
        full.AppendLine("    IF @@TRANCOUNT > 0 ROLLBACK TRANSACTION;");
        full.AppendLine("    THROW;");
        full.AppendLine("END CATCH;");
        return new SeedScript(full.ToString(), _master.ToString(), _equipment.ToString(), _preventive.ToString(), _corrective.ToString(), _acquisitions.ToString(), BuildStandaloneQuarantineScript());
    }

    private string Header()
    {
        var builder = new StringBuilder();
        builder.AppendLine("SET ANSI_NULLS ON;");
        builder.AppendLine("SET ANSI_PADDING ON;");
        builder.AppendLine("SET ANSI_WARNINGS ON;");
        builder.AppendLine("SET ARITHABORT ON;");
        builder.AppendLine("SET CONCAT_NULL_YIELDS_NULL ON;");
        builder.AppendLine("SET QUOTED_IDENTIFIER ON;");
        builder.AppendLine("SET NUMERIC_ROUNDABORT OFF;");
        builder.AppendLine("SET NOCOUNT ON;");
        builder.AppendLine("SET XACT_ABORT ON;");
        builder.AppendLine("IF OBJECT_ID(N'dbo.__EFMigrationsHistory', N'U') IS NULL");
        builder.AppendLine("    THROW 51000, 'La base destino no contiene el esquema EF Core esperado.', 1;");
        builder.AppendLine("IF OBJECT_ID(N'dbo.EquipmentUnits', N'U') IS NULL OR OBJECT_ID(N'dbo.Requests', N'U') IS NULL");
        builder.AppendLine("    THROW 51001, 'La base destino no contiene las tablas funcionales esperadas.', 1;");
        builder.AppendLine("IF OBJECT_ID(N'dbo.HistoricalVerificationQuarantines', N'U') IS NULL");
        builder.AppendLine("    THROW 51002, 'La base destino no contiene la tabla de cuarentena historica esperada.', 1;");
        builder.AppendLine("BEGIN TRY");
        builder.AppendLine("BEGIN TRANSACTION;");
        builder.AppendLine();
        builder.AppendLine("DECLARE @CreatedById int = NULL;");
        builder.AppendLine("DECLARE @Today datetime2 = SYSUTCDATETIME();");
        builder.AppendLine("DECLARE @EntityId int = NULL;");
        builder.AppendLine("DECLARE @RelatedId int = NULL;");
        builder.AppendLine("DECLARE @RelatedId2 int = NULL;");
        builder.AppendLine("DECLARE @RelatedId3 int = NULL;");
        builder.AppendLine("DECLARE @RelatedId4 int = NULL;");
        builder.AppendLine("DECLARE @UnitId int = NULL;");
        builder.AppendLine("DECLARE @EquipmentId int = NULL;");
        builder.AppendLine("DECLARE @LabId int = NULL;");
        builder.AppendLine("DECLARE @ManagementId int = NULL;");
        builder.AppendLine("DECLARE @RequestId int = NULL;");
        builder.AppendLine("DECLARE @MaintenanceId int = NULL;");
        builder.AppendLine("DECLARE @DepartureId int = NULL;");
        builder.AppendLine("DECLARE @VerificationId int = NULL;");
        builder.AppendLine("DECLARE @HistoryId int = NULL;");
        builder.AppendLine();
        builder.AppendLine("DECLARE @FacultyMap TABLE ([Key] nvarchar(300) PRIMARY KEY, Id int NOT NULL);");
        builder.AppendLine("DECLARE @CareerMap TABLE ([Key] nvarchar(300) PRIMARY KEY, Id int NOT NULL);");
        builder.AppendLine("DECLARE @LabMap TABLE ([Key] nvarchar(100) PRIMARY KEY, Id int NOT NULL);");
        builder.AppendLine("DECLARE @PersonMap TABLE ([Key] nvarchar(300) PRIMARY KEY, Id int NOT NULL);");
        builder.AppendLine("DECLARE @ManagementMap TABLE ([Key] nvarchar(50) PRIMARY KEY, Id int NOT NULL);");
        builder.AppendLine("DECLARE @EquipmentMap TABLE ([Key] nvarchar(700) PRIMARY KEY, Id int NOT NULL);");
        builder.AppendLine("DECLARE @UnitMap TABLE ([Key] nvarchar(50) PRIMARY KEY, Id int NOT NULL, EquipmentId int NOT NULL, LaboratoryId int NULL);");
        builder.AppendLine("DECLARE @VerificationMap TABLE ([Key] nvarchar(160) PRIMARY KEY, Id int NOT NULL);");
        builder.AppendLine("DECLARE @RequestMap TABLE ([Key] nvarchar(160) PRIMARY KEY, Id int NOT NULL);");
        builder.AppendLine("DECLARE @MaintenanceMap TABLE ([Key] nvarchar(160) PRIMARY KEY, Id int NOT NULL);");
        builder.AppendLine("DECLARE @DepartureMap TABLE ([Key] nvarchar(160) PRIMARY KEY, Id int NOT NULL);");
        builder.AppendLine("DECLARE @HistoryMap TABLE ([Key] nvarchar(160) PRIMARY KEY, Id int NOT NULL);");
        builder.AppendLine();
        return builder.ToString();
    }

    private void BuildMasterData()
    {
        _master.AppendLine("-- ============================================================");
        _master.AppendLine("-- 00 MASTER DATA");
        _master.AppendLine("-- ============================================================");
        foreach (var f in _seed.Faculties)
        {
            _master.AppendLine($"-- Faculty: {Comment(f.Name)}");
            _master.AppendLine("SET @EntityId = NULL;");
            _master.AppendLine($"MERGE Faculties AS target USING (SELECT {S(f.Name)} AS Name, {S(f.Code)} AS Code, {S(f.Description)} AS Description) AS source ON target.Name = source.Name WHEN MATCHED THEN UPDATE SET Code = COALESCE(target.Code, source.Code), Description = COALESCE(target.Description, source.Description) WHEN NOT MATCHED THEN INSERT (Name, Code, Description, Status, CreatedDate, CreatedById) VALUES (source.Name, source.Code, source.Description, 0, @Today, @CreatedById);");
            _master.AppendLine($"SELECT @EntityId = Id FROM Faculties WHERE Name = {S(f.Name)};");
            _master.AppendLine($"IF NOT EXISTS (SELECT 1 FROM @FacultyMap WHERE [Key] = {S(f.Name)}) INSERT INTO @FacultyMap ([Key], Id) VALUES ({S(f.Name)}, @EntityId);");
            _master.AppendLine();
        }

        foreach (var c in _seed.Careers)
        {
            _master.AppendLine($"-- Career: {Comment(c.Name)}");
            _master.AppendLine("SET @EntityId = NULL; SET @RelatedId = NULL;");
            if (!string.IsNullOrWhiteSpace(c.FacultyName))
            {
                _master.AppendLine($"SELECT @RelatedId = Id FROM @FacultyMap WHERE [Key] = {S(c.FacultyName)};");
            }
            _master.AppendLine($"MERGE Careers AS target USING (SELECT {S(c.Name)} AS Name) AS source ON target.Name = source.Name WHEN MATCHED THEN UPDATE SET FacultadId = COALESCE(target.FacultadId, @RelatedId) WHEN NOT MATCHED THEN INSERT (Name, FacultadId, Status, CreatedDate, CreatedById) VALUES (source.Name, @RelatedId, {c.Status}, @Today, @CreatedById);");
            _master.AppendLine($"SELECT @EntityId = Id FROM Careers WHERE Name = {S(c.Name)};");
            _master.AppendLine($"IF NOT EXISTS (SELECT 1 FROM @CareerMap WHERE [Key] = {S(c.Name)}) INSERT INTO @CareerMap ([Key], Id) VALUES ({S(c.Name)}, @EntityId);");
            _master.AppendLine();
        }

        foreach (var l in _seed.Laboratories)
        {
            _master.AppendLine($"-- Laboratory: {Comment(l.Code)}");
            _master.AppendLine("SET @EntityId = NULL; SET @RelatedId = NULL;");
            _master.AppendLine($"SELECT @RelatedId = Id FROM @FacultyMap WHERE [Key] = {S(l.FacultyName)};");
            _master.AppendLine($"MERGE Laboratories AS target USING (SELECT {S(l.Code)} AS Code, {S(l.Name)} AS Name, {S(l.Floor)} AS Floor, {S(l.Description)} AS Description) AS source ON target.Code = source.Code WHEN MATCHED THEN UPDATE SET Name = source.Name, FacultyId = COALESCE(target.FacultyId, @RelatedId), Floor = COALESCE(target.Floor, source.Floor), Description = COALESCE(target.Description, source.Description) WHEN NOT MATCHED THEN INSERT (FacultyId, Code, Name, Floor, Description, Status, CreatedDate, CreatedById) VALUES (@RelatedId, source.Code, source.Name, source.Floor, source.Description, 0, @Today, @CreatedById);");
            _master.AppendLine($"SELECT @EntityId = Id FROM Laboratories WHERE Code = {S(l.Code)};");
            _master.AppendLine($"IF NOT EXISTS (SELECT 1 FROM @LabMap WHERE [Key] = {S(l.Code)}) INSERT INTO @LabMap ([Key], Id) VALUES ({S(l.Code)}, @EntityId);");
            _master.AppendLine();
        }

        foreach (var p in _seed.People)
        {
            _master.AppendLine($"-- Person: {Comment(p.Name)}");
            _master.AppendLine("SET @EntityId = NULL;");
            _master.AppendLine(p.IsExternal
                ? $"SELECT @EntityId = p.Id FROM People p INNER JOIN Externs e ON e.Id = p.Id WHERE e.Name = {S(p.Name)};"
                : $"SELECT @EntityId = p.Id FROM People p INNER JOIN Interns i ON i.Id = p.Id WHERE i.Name = {S(p.Name)};");
            _master.AppendLine("IF @EntityId IS NULL");
            _master.AppendLine("BEGIN");
            _master.AppendLine($"    MERGE People AS target USING (SELECT {p.Category} AS Category) AS source ON 1 = 0 WHEN NOT MATCHED THEN INSERT (Status, Category, Email, PhoneNumber, CreatedDate, CreatedById) VALUES (0, source.Category, {S(p.Email)}, {S(p.Phone)}, @Today, @CreatedById);");
            _master.AppendLine("    SET @EntityId = CONVERT(int, SCOPE_IDENTITY());");
            if (p.IsExternal)
            {
                _master.AppendLine($"    INSERT INTO Externs (Id, IsEntity, Name, Address, ExternStatus) VALUES (@EntityId, {Bit(p.IsCompany)}, {S(p.Name)}, {S(p.Address ?? "Sin dato")}, 0);");
            }
            else
            {
                _master.AppendLine($"    INSERT INTO Interns (Id, Name, InternStatus) VALUES (@EntityId, {S(p.Name)}, 0);");
            }
            _master.AppendLine("END");
            _master.AppendLine($"IF OBJECT_ID(N'PersonAliases', N'U') IS NOT NULL AND NOT EXISTS (SELECT 1 FROM PersonAliases WHERE PersonId = @EntityId AND IsPreferred = 1) INSERT INTO PersonAliases (PersonId, Alias, NormalizedAlias, IsPreferred, Source) VALUES (@EntityId, {S(p.Name)}, UPPER(LTRIM(RTRIM(REPLACE(REPLACE({S(p.Name)}, CHAR(13), N' '), CHAR(10), N' ')))), 1, N'Plantilla_Original.xlsx');");
            _master.AppendLine($"IF OBJECT_ID(N'PersonRoleAssignments', N'U') IS NOT NULL AND NOT EXISTS (SELECT 1 FROM PersonRoleAssignments WHERE PersonId = @EntityId AND IsActive = 1) INSERT INTO PersonRoleAssignments (PersonId, Role, IsActive, ValidFrom) VALUES (@EntityId, {(p.Category == 1 ? 1 : p.Category == 5 ? 3 : 99)}, 1, @Today);");
            _master.AppendLine($"IF NOT EXISTS (SELECT 1 FROM @PersonMap WHERE [Key] = {S(p.Name)}) INSERT INTO @PersonMap ([Key], Id) VALUES ({S(p.Name)}, @EntityId);");
            _master.AppendLine();
        }

        foreach (var m in _seed.Managements)
        {
            _master.AppendLine($"-- Management: {Comment(m.Code)}");
            _master.AppendLine("SET @EntityId = NULL;");
            _master.AppendLine($"MERGE Managements AS target USING (SELECT {m.Year} AS [Year], {m.Semester} AS Semester, {S(m.Code)} AS Code) AS source ON target.[Year] = source.[Year] AND target.Semester = source.Semester AND target.Type = 0 WHEN MATCHED THEN UPDATE SET Code = source.Code WHEN NOT MATCHED THEN INSERT ([Year], Semester, Code, Description, Status, Responsible, Type, CreatedDate, CreatedById) VALUES (source.[Year], source.Semester, source.Code, {S(m.Description)}, 2, N'Carga historica seed', 0, @Today, @CreatedById);");
            _master.AppendLine($"SELECT @EntityId = Id FROM Managements WHERE [Year] = {m.Year} AND Semester = {m.Semester} AND Type = 0;");
            _master.AppendLine($"IF NOT EXISTS (SELECT 1 FROM @ManagementMap WHERE [Key] = {S(m.Code)}) INSERT INTO @ManagementMap ([Key], Id) VALUES ({S(m.Code)}, @EntityId);");
            _master.AppendLine();
        }

        AppendOriginCount(_master, "00 MASTER DATA", ["Faculty", "Career", "Laboratory", "Person", "Management"]);
    }

    private void BuildVerificationQuarantine()
    {
        _quarantine.AppendLine("-- ============================================================");
        _quarantine.AppendLine("-- 00B UNRESOLVED L-6 VERIFICATION QUARANTINE");
        _quarantine.AppendLine("-- ============================================================");
        foreach (var row in _seed.UnresolvedVerifications.OrderBy(row => row.SourceRow))
        {
            _quarantine.AppendLine($"MERGE HistoricalVerificationQuarantines AS target USING (SELECT {S(row.Key)} AS SourceKey, {S(row.SourceSheet)} AS SourceSheet, {row.SourceRow} AS SourceRow, {S(row.InventoryRaw)} AS InventoryRaw, {S(row.DateRaw)} AS DateRaw, {S(row.PhysicalConditionRaw)} AS PhysicalConditionRaw, {S(row.FindingRaw)} AS FindingRaw, {S(row.EquipmentName)} AS EquipmentNameRaw, {S(row.Column6Raw)} AS Column6Raw, {S(row.Column7Raw)} AS Column7Raw, {S(row.Reason)} AS Reason) AS source ON target.SourceKey = source.SourceKey WHEN MATCHED THEN UPDATE SET SourceSheet = source.SourceSheet, SourceRow = source.SourceRow, InventoryRaw = source.InventoryRaw, DateRaw = source.DateRaw, PhysicalConditionRaw = source.PhysicalConditionRaw, FindingRaw = source.FindingRaw, EquipmentNameRaw = source.EquipmentNameRaw, Column6Raw = source.Column6Raw, Column7Raw = source.Column7Raw, Reason = source.Reason WHEN NOT MATCHED THEN INSERT (SourceKey, SourceSheet, SourceRow, InventoryRaw, DateRaw, PhysicalConditionRaw, FindingRaw, EquipmentNameRaw, Column6Raw, Column7Raw, Reason, ImportedAt) VALUES (source.SourceKey, source.SourceSheet, source.SourceRow, source.InventoryRaw, source.DateRaw, source.PhysicalConditionRaw, source.FindingRaw, source.EquipmentNameRaw, source.Column6Raw, source.Column7Raw, source.Reason, @Today);");
        }
        _quarantine.AppendLine($"-- Quarantined unresolved verifications: {_seed.UnresolvedVerifications.Count}");
        _quarantine.AppendLine();
    }

    private string BuildStandaloneQuarantineScript()
    {
        var builder = new StringBuilder();
        builder.AppendLine("SET ANSI_NULLS ON;");
        builder.AppendLine("SET ANSI_PADDING ON;");
        builder.AppendLine("SET ANSI_WARNINGS ON;");
        builder.AppendLine("SET ARITHABORT ON;");
        builder.AppendLine("SET CONCAT_NULL_YIELDS_NULL ON;");
        builder.AppendLine("SET QUOTED_IDENTIFIER ON;");
        builder.AppendLine("SET NUMERIC_ROUNDABORT OFF;");
        builder.AppendLine("SET NOCOUNT ON;");
        builder.AppendLine("SET XACT_ABORT ON;");
        builder.AppendLine("IF OBJECT_ID(N'dbo.HistoricalVerificationQuarantines', N'U') IS NULL");
        builder.AppendLine("    THROW 51002, 'La base destino no contiene la tabla de cuarentena historica esperada.', 1;");
        builder.AppendLine("BEGIN TRY");
        builder.AppendLine("BEGIN TRANSACTION;");
        builder.AppendLine("DECLARE @Today datetime2 = SYSUTCDATETIME();");
        builder.Append(_quarantine);
        builder.AppendLine("COMMIT TRANSACTION;");
        builder.AppendLine("END TRY");
        builder.AppendLine("BEGIN CATCH");
        builder.AppendLine("    IF @@TRANCOUNT > 0 ROLLBACK TRANSACTION;");
        builder.AppendLine("    THROW;");
        builder.AppendLine("END CATCH;");
        return builder.ToString();
    }

    private void BuildEquipment()
    {
        _equipment.AppendLine("-- ============================================================");
        _equipment.AppendLine("-- 01 EQUIPMENT");
        _equipment.AppendLine("-- ============================================================");
        foreach (var e in _seed.Equipments)
        {
            _equipment.AppendLine($"-- Equipment: {Comment(e.Name)}");
            _equipment.AppendLine("SET @EntityId = NULL;");
            _equipment.AppendLine($"MERGE Equipments AS target USING (SELECT {S(e.Name)} AS Name, {S(e.Brand)} AS Brand, {S(e.Model)} AS Model) AS source ON target.Name = source.Name AND ISNULL(target.Brand, N'') = ISNULL(source.Brand, N'') AND ISNULL(target.Model, N'') = ISNULL(source.Model, N'') WHEN MATCHED THEN UPDATE SET Description = COALESCE(target.Description, {S(e.Description)}) WHEN NOT MATCHED THEN INSERT (Category, UtensilType, TypeClassification, Status, Name, Brand, Model, UsefulLifeYears, Description, CreatedDate, CreatedById) VALUES ({e.Category}, 0, {e.TypeClassification}, 0, source.Name, source.Brand, source.Model, {N(e.UsefulLifeYears)}, {S(e.Description)}, @Today, @CreatedById);");
            _equipment.AppendLine($"SELECT TOP 1 @EntityId = Id FROM Equipments WHERE Name = {S(e.Name)} AND ISNULL(Brand, N'') = ISNULL({S(e.Brand)}, N'') AND ISNULL(Model, N'') = ISNULL({S(e.Model)}, N'');");
            _equipment.AppendLine($"IF NOT EXISTS (SELECT 1 FROM @EquipmentMap WHERE [Key] = {S(e.Key)}) INSERT INTO @EquipmentMap ([Key], Id) VALUES ({S(e.Key)}, @EntityId);");
            _equipment.AppendLine();
        }

        foreach (var u in _seed.Units)
        {
            _equipment.AppendLine($"-- EquipmentUnit: {Comment(u.InventoryNumber)}");
            _equipment.AppendLine("SET @EntityId = NULL; SET @EquipmentId = NULL; SET @LabId = NULL; SET @RelatedId = NULL; SET @ManagementId = NULL;");
            _equipment.AppendLine($"SELECT @EquipmentId = Id FROM @EquipmentMap WHERE [Key] = {S(u.EquipmentKey)};");
            if (!string.IsNullOrWhiteSpace(u.LaboratoryCode))
            {
                _equipment.AppendLine($"SELECT @LabId = Id FROM @LabMap WHERE [Key] = {S(u.LaboratoryCode)};");
            }
            if (!string.IsNullOrWhiteSpace(u.CareerName))
            {
                _equipment.AppendLine($"SELECT @RelatedId = Id FROM @CareerMap WHERE [Key] = {S(u.CareerName)};");
            }
            _equipment.AppendLine($"SELECT TOP 1 @ManagementId = Id FROM @ManagementMap WHERE [Key] = {S(_seed.DefaultManagementCode)};");
            _equipment.AppendLine($"MERGE EquipmentUnits AS target USING (SELECT {S(u.InventoryNumber)} AS InventoryNumber) AS source ON target.InventoryNumber = source.InventoryNumber WHEN MATCHED THEN UPDATE SET EquipmentId = COALESCE(target.EquipmentId, @EquipmentId), LaboratoryId = COALESCE(target.LaboratoryId, @LabId), LocationResolutionStatus = CASE WHEN COALESCE(target.LaboratoryId, @LabId) IS NULL THEN 0 ELSE 1 END, CareerId = COALESCE(target.CareerId, @RelatedId) WHEN NOT MATCHED THEN INSERT (ManagementId, EquipmentId, LaboratoryId, LocationResolutionStatus, InventoryNumber, SerialNumber, CareerId, AcquisitionDate, ManufacturingDate, AcquisitionValue, CurrentStatus, PhysicalCondition, Notes, CreatedDate, CreatedById) VALUES (@ManagementId, @EquipmentId, @LabId, CASE WHEN @LabId IS NULL THEN 0 ELSE 1 END, source.InventoryNumber, {S(u.SerialNumber)}, @RelatedId, {D(u.AcquisitionDate)}, {D(u.ManufacturingDate)}, {N(u.AcquisitionValue)}, {u.Status}, {N(u.PhysicalCondition)}, {S(u.Notes)}, @Today, @CreatedById);");
            _equipment.AppendLine($"SELECT @EntityId = Id FROM EquipmentUnits WHERE InventoryNumber = {S(u.InventoryNumber)};");
            _equipment.AppendLine($"IF NOT EXISTS (SELECT 1 FROM @UnitMap WHERE [Key] = {S(u.InventoryNumber)}) INSERT INTO @UnitMap ([Key], Id, EquipmentId, LaboratoryId) VALUES ({S(u.InventoryNumber)}, @EntityId, @EquipmentId, @LabId);");
            _equipment.AppendLine();
        }

        AppendOriginCount(_equipment, "01 EQUIPMENT", ["Equipment", "EquipmentUnit"]);
    }

    private void BuildChains(IEnumerable<SeedChainRecord> chains, StringBuilder builder, string label)
    {
        builder.AppendLine("-- ============================================================");
        builder.AppendLine($"-- {label} CHAINS");
        builder.AppendLine("-- ============================================================");
        foreach (var chain in chains)
        {
            builder.AppendLine($"-- Chain: {Comment(chain.ManagementCode)} / {Comment(chain.InventoryNumber)}");
            if (!chain.IsCorrective && chain.VerificationKey != null)
            {
                var verification = _seed.Verifications.First(v => v.Key == chain.VerificationKey);
                AppendVerification(builder, verification);
            }

            if (chain.TechnicalRequestKey != null)
            {
                var request = _seed.Requests.First(r => r.Key == chain.TechnicalRequestKey);
                AppendRequest(builder, request);
            }

            if (chain.MaintenanceKey != null)
            {
                var maintenance = _seed.Maintenances.First(m => m.Key == chain.MaintenanceKey);
                AppendMaintenance(builder, maintenance, chain.TechnicalRequestKey);
            }

            if (chain.LinkedDepartureKey != null)
            {
                var departure = _seed.Departures.First(d => d.Key == chain.LinkedDepartureKey);
                AppendDeparture(builder, departure);
                foreach (var item in _seed.DepartureItems.Where(i => i.DepartureKey == chain.LinkedDepartureKey))
                {
                    AppendDepartureItem(builder, item);
                }
            }

            if (chain.KardexHistoryKey != null)
            {
                var history = _seed.KardexHistories.First(h => h.Key == chain.KardexHistoryKey);
                AppendKardexHistory(builder, history);
            }

            builder.AppendLine();
        }

        AppendOriginCount(builder, $"{label} CHAINS", label == "PREVENTIVO"
            ? ["Verification", "VerificationFault", "Request", "Maintenance", "Departure", "DepartureItem", "EquipmentStateHistory"]
            : ["Request", "Maintenance", "Departure", "DepartureItem", "EquipmentStateHistory"]);
    }

    private void BuildAcquisitionsAndCosts()
    {
        _acquisitions.AppendLine("-- ============================================================");
        _acquisitions.AppendLine("-- 04 ACQUISITIONS AND COSTS");
        _acquisitions.AppendLine("-- ============================================================");
        foreach (var request in _seed.Requests.Where(r => r.Type == 2).OrderBy(r => r.Key))
        {
            AppendRequest(_acquisitions, request);
        }

        foreach (var cost in _seed.CostDetails)
        {
            AppendCostDetail(_acquisitions, cost);
        }

        AppendOriginCount(_acquisitions, "04 ACQUISITIONS AND COSTS", ["Request", "CostDetail"]);
    }

    private void BuildUnlinkedHistory()
    {
        _corrective.AppendLine("-- ============================================================");
        _corrective.AppendLine("-- HISTORICAL RECORDS NOT SELECTED AS THE ACTIVE PLAN LINK");
        _corrective.AppendLine("-- ============================================================");

        var linkedVerificationKeys = _seed.Chains
            .Where(c => c.VerificationKey != null)
            .Select(c => c.VerificationKey!)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);
        foreach (var verification in _seed.Verifications.Where(v => !linkedVerificationKeys.Contains(v.Key)).OrderBy(v => v.Key))
        {
            AppendVerification(_corrective, verification);
        }

        var linkedRequestKeys = _seed.Chains
            .Where(c => c.TechnicalRequestKey != null)
            .Select(c => c.TechnicalRequestKey!)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);
        foreach (var request in _seed.Requests.Where(r => r.Type != 2 && !linkedRequestKeys.Contains(r.Key)).OrderBy(r => r.Key))
        {
            AppendRequest(_corrective, request);
        }

        var linkedMaintenanceKeys = _seed.Chains
            .Where(c => c.MaintenanceKey != null)
            .Select(c => c.MaintenanceKey!)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);
        var requestKeysAlreadyAssigned = _seed.Chains
            .Where(c => c.MaintenanceKey != null && c.TechnicalRequestKey != null)
            .Select(c => c.TechnicalRequestKey!)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);
        foreach (var maintenance in _seed.Maintenances.Where(m => !linkedMaintenanceKeys.Contains(m.Key)).OrderBy(m => m.Key))
        {
            var requestKey = !string.IsNullOrWhiteSpace(maintenance.RequestKey) && requestKeysAlreadyAssigned.Add(maintenance.RequestKey)
                ? maintenance.RequestKey
                : null;
            AppendMaintenance(_corrective, maintenance, requestKey);
        }

        var linkedDepartureKeys = _seed.Chains
            .Where(c => c.LinkedDepartureKey != null)
            .Select(c => c.LinkedDepartureKey!)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);
        foreach (var departure in _seed.Departures.Where(d => !linkedDepartureKeys.Contains(d.Key)).OrderBy(d => d.Key))
        {
            AppendDeparture(_corrective, departure);
            foreach (var item in _seed.DepartureItems.Where(i => i.DepartureKey == departure.Key))
            {
                AppendDepartureItem(_corrective, item);
            }
        }

        var linkedHistoryKeys = _seed.Chains
            .Where(c => c.KardexHistoryKey != null)
            .Select(c => c.KardexHistoryKey!)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);
        foreach (var history in _seed.KardexHistories.Where(h => !linkedHistoryKeys.Contains(h.Key)).OrderBy(h => h.Key))
        {
            AppendKardexHistory(_corrective, history);
        }

        AppendOriginCount(_corrective, "UNLINKED HISTORY", ["Verification", "VerificationFault", "Request", "Maintenance", "Departure", "DepartureItem", "EquipmentStateHistory"]);
    }

    private void BuildPlans()
    {
        _plans.AppendLine("-- ============================================================");
        _plans.AppendLine("-- MANAGEMENT PLANS / L-48 LINKS");
        _plans.AppendLine("-- ============================================================");
        foreach (var chain in _seed.Chains)
        {
            _plans.AppendLine($"-- ManagementPlan: {Comment(chain.PlanKey)}");
            _plans.AppendLine("SET @EntityId = NULL; SET @ManagementId = NULL; SET @UnitId = NULL; SET @VerificationId = NULL; SET @RequestId = NULL; SET @MaintenanceId = NULL; SET @DepartureId = NULL; SET @HistoryId = NULL; SET @RelatedId = NULL;");
            _plans.AppendLine($"SELECT @ManagementId = Id FROM @ManagementMap WHERE [Key] = {S(chain.ManagementCode)};");
            _plans.AppendLine($"SELECT @UnitId = Id FROM @UnitMap WHERE [Key] = {S(chain.InventoryNumber)};");
            if (chain.VerificationKey != null) _plans.AppendLine($"SELECT @VerificationId = Id FROM @VerificationMap WHERE [Key] = {S(chain.VerificationKey)};");
            if (chain.TechnicalRequestKey != null) _plans.AppendLine($"SELECT @RequestId = Id FROM @RequestMap WHERE [Key] = {S(chain.TechnicalRequestKey)};");
            if (chain.MaintenanceKey != null) _plans.AppendLine($"SELECT @MaintenanceId = Id FROM @MaintenanceMap WHERE [Key] = {S(chain.MaintenanceKey)};");
            if (chain.LinkedDepartureKey != null) _plans.AppendLine($"SELECT @DepartureId = Id FROM @DepartureMap WHERE [Key] = {S(chain.LinkedDepartureKey)};");
            if (chain.KardexHistoryKey != null) _plans.AppendLine($"SELECT @HistoryId = Id FROM @HistoryMap WHERE [Key] = {S(chain.KardexHistoryKey)};");
            if (chain.AcquisitionRequestKey != null) _plans.AppendLine($"SELECT @RelatedId = Id FROM @RequestMap WHERE [Key] = {S(chain.AcquisitionRequestKey)};");
            _plans.AppendLine($"MERGE ManagementPlans AS target USING (SELECT @ManagementId AS ManagementId, @UnitId AS EquipmentUnitId) AS source ON target.ManagementId = source.ManagementId AND target.EquipmentUnitId = source.EquipmentUnitId WHEN MATCHED THEN UPDATE SET VerificationId = COALESCE(target.VerificationId, @VerificationId), RequestId = COALESCE(target.RequestId, @RequestId), MaintenanceId = COALESCE(target.MaintenanceId, @MaintenanceId), DepartureId = COALESCE(target.DepartureId, @DepartureId), KardexHistoryId = COALESCE(target.KardexHistoryId, @HistoryId), AcquisitionRequestId = COALESCE(target.AcquisitionRequestId, @RelatedId), CurrentPhase = {chain.CurrentPhase}, CurrentState = {chain.CurrentState}, PlanStatus = {chain.PlanStatus}, Responsible = COALESCE(target.Responsible, {S(chain.Responsible)}) WHEN NOT MATCHED THEN INSERT (ManagementId, EquipmentUnitId, VerificationId, RequestId, MaintenanceId, DepartureId, KardexHistoryId, AcquisitionRequestId, CurrentPhase, CurrentState, Responsible, PlannedDate, PlanStatus, IsDraft, CreatedDate, CreatedById) VALUES (@ManagementId, @UnitId, @VerificationId, @RequestId, @MaintenanceId, @DepartureId, @HistoryId, @RelatedId, {chain.CurrentPhase}, {chain.CurrentState}, {S(chain.Responsible)}, {D(chain.PlannedDate)}, {chain.PlanStatus}, 0, @Today, @CreatedById);");
            _plans.AppendLine();
        }

        AppendOriginCount(_plans, "MANAGEMENT PLANS", ["ManagementPlan"]);
    }

    private void AppendVerification(StringBuilder builder, VerificationRecord v)
    {
        builder.AppendLine($"-- Verification: {Comment(v.Key)}");
        builder.AppendLine("SET @VerificationId = NULL; SET @UnitId = NULL; SET @ManagementId = NULL;");
        builder.AppendLine($"SELECT @UnitId = Id FROM @UnitMap WHERE [Key] = {S(v.InventoryNumber)};");
        builder.AppendLine($"SELECT @ManagementId = Id FROM @ManagementMap WHERE [Key] = {S(v.ManagementCode)};");
        builder.AppendLine($"MERGE Verifications AS target USING (SELECT @UnitId AS EquipmentUnitId, @ManagementId AS ManagementId, {D(v.Date)} AS [Date], {v.PhysicalCondition} AS PhysicalCondition, {S(v.Observations)} AS Observations, {S(v.Key)} AS HistoricalSourceKey) AS source ON target.HistoricalSourceKey = source.HistoricalSourceKey OR (target.HistoricalSourceKey IS NULL AND target.EquipmentUnitId = source.EquipmentUnitId AND target.ManagementId = source.ManagementId AND CONVERT(date, target.[Date]) = CONVERT(date, source.[Date]) AND target.PhysicalCondition = source.PhysicalCondition AND ISNULL(target.Observations, N'') = ISNULL(source.Observations, N'')) WHEN MATCHED THEN UPDATE SET HistoricalSourceKey = COALESCE(target.HistoricalSourceKey, source.HistoricalSourceKey), Status = CASE WHEN target.Status = 0 THEN {v.Status} ELSE target.Status END WHEN NOT MATCHED THEN INSERT (EquipmentUnitId, ManagementId, [Date], Observations, PhysicalCondition, Status, HistoricalSourceKey, CreatedDate, CreatedById) VALUES (@UnitId, @ManagementId, source.[Date], source.Observations, source.PhysicalCondition, {v.Status}, source.HistoricalSourceKey, @Today, @CreatedById);");
        builder.AppendLine($"SELECT @VerificationId = Id FROM Verifications WHERE HistoricalSourceKey = {S(v.Key)};");
        builder.AppendLine($"IF NOT EXISTS (SELECT 1 FROM @VerificationMap WHERE [Key] = {S(v.Key)}) INSERT INTO @VerificationMap ([Key], Id) VALUES ({S(v.Key)}, @VerificationId);");
        foreach (var fault in _seed.VerificationFaults.Where(f => f.VerificationKey == v.Key))
        {
            builder.AppendLine($"MERGE VerificationFaults AS target USING (SELECT @VerificationId AS VerificationId, {S(fault.Description)} AS Description) AS source ON target.VerificationId = source.VerificationId AND target.Description = source.Description WHEN NOT MATCHED THEN INSERT (VerificationId, Description, IsDeleted, CreatedDate, CreatedById) VALUES (source.VerificationId, source.Description, 0, @Today, @CreatedById);");
        }
        builder.AppendLine();
    }

    private void AppendRequest(StringBuilder builder, RequestRecord r)
    {
        builder.AppendLine($"-- Request: {Comment(r.Key)}");
        builder.AppendLine("SET @RequestId = NULL; SET @UnitId = NULL; SET @EquipmentId = NULL; SET @LabId = NULL; SET @ManagementId = NULL;");
        builder.AppendLine($"SELECT @UnitId = Id, @EquipmentId = EquipmentId, @LabId = LaboratoryId FROM @UnitMap WHERE [Key] = {S(r.InventoryNumber)};");
        builder.AppendLine($"SELECT @ManagementId = Id FROM @ManagementMap WHERE [Key] = {S(r.ManagementCode)};");
        builder.AppendLine($"MERGE Requests AS target USING (SELECT @LabId AS LaboratoryId, @EquipmentId AS EquipmentId, @UnitId AS EquipmentUnitId, @ManagementId AS ManagementId, {r.Type} AS [Type], {S(r.Description)} AS Description, {S(r.InvestmentCode)} AS InvestmentCode, {D(r.CreatedDate)} AS CreatedDate, {S(r.Key)} AS HistoricalSourceKey) AS source ON target.HistoricalSourceKey = source.HistoricalSourceKey OR (target.HistoricalSourceKey IS NULL AND target.ManagementId = source.ManagementId AND target.EquipmentUnitId = source.EquipmentUnitId AND target.[Type] = source.[Type] AND target.Description = source.Description AND ISNULL(target.InvestmentCode, N'') = ISNULL(source.InvestmentCode, N'') AND CONVERT(date, target.CreatedDate) = CONVERT(date, source.CreatedDate)) WHEN MATCHED THEN UPDATE SET HistoricalSourceKey = COALESCE(target.HistoricalSourceKey, source.HistoricalSourceKey), LaboratoryId = COALESCE(target.LaboratoryId, source.LaboratoryId), LocationResolutionStatus = CASE WHEN COALESCE(target.LaboratoryId, source.LaboratoryId) IS NULL THEN 0 ELSE 1 END, Status = CASE WHEN target.Status < {r.Status} THEN {r.Status} ELSE target.Status END, Priority = {r.Priority} WHEN NOT MATCHED THEN INSERT (LaboratoryId, LocationResolutionStatus, EquipmentId, EquipmentUnitId, ManagementId, Description, Priority, Observations, EstimatedRepairTime, Status, Type, InvestmentCode, CostCenter, HistoricalSourceKey, CreatedDate, CreatedById) VALUES (@LabId, CASE WHEN @LabId IS NULL THEN 0 ELSE 1 END, @EquipmentId, @UnitId, @ManagementId, source.Description, {r.Priority}, {S(r.Observations)}, {S(r.EstimatedRepairTime)}, {r.Status}, {r.Type}, source.InvestmentCode, {S(r.CostCenter)}, source.HistoricalSourceKey, source.CreatedDate, @CreatedById);");
        builder.AppendLine($"SELECT @RequestId = Id FROM Requests WHERE HistoricalSourceKey = {S(r.Key)};");
        builder.AppendLine($"IF NOT EXISTS (SELECT 1 FROM @RequestMap WHERE [Key] = {S(r.Key)}) INSERT INTO @RequestMap ([Key], Id) VALUES ({S(r.Key)}, @RequestId);");
        builder.AppendLine();
    }

    private void AppendMaintenance(StringBuilder builder, MaintenanceRecord m, string? requestKey)
    {
        builder.AppendLine($"-- Maintenance: {Comment(m.Key)}");
        builder.AppendLine("SET @MaintenanceId = NULL; SET @UnitId = NULL; SET @ManagementId = NULL; SET @RelatedId = NULL; SET @RequestId = NULL;");
        builder.AppendLine($"SELECT @UnitId = Id FROM @UnitMap WHERE [Key] = {S(m.InventoryNumber)};");
        builder.AppendLine($"SELECT @ManagementId = Id FROM @ManagementMap WHERE [Key] = {S(m.ManagementCode)};");
        if (!string.IsNullOrWhiteSpace(m.TechnicianName)) builder.AppendLine($"SELECT @RelatedId = Id FROM @PersonMap WHERE [Key] = {S(m.TechnicianName)};");
        if (!string.IsNullOrWhiteSpace(requestKey)) builder.AppendLine($"SELECT @RequestId = Id FROM @RequestMap WHERE [Key] = {S(requestKey)};");
        builder.AppendLine($"MERGE Maintenances AS target USING (SELECT @UnitId AS EquipmentUnitId, @ManagementId AS ManagementId, {m.MaintenanceType} AS MaintenanceType, {m.ServiceType} AS ServiceType, {D(m.ScheduledDate)} AS ScheduledDate, {D(m.StartDate)} AS StartDate, {D(m.EndDate)} AS EndDate, {S(m.Description)} AS Description, {S(m.Key)} AS HistoricalSourceKey) AS source ON target.HistoricalSourceKey = source.HistoricalSourceKey OR (target.HistoricalSourceKey IS NULL AND target.EquipmentUnitId = source.EquipmentUnitId AND target.ManagementId = source.ManagementId AND target.MaintenanceType = source.MaintenanceType AND target.ServiceType = source.ServiceType AND (CONVERT(date, target.ScheduledDate) = CONVERT(date, source.ScheduledDate) OR (target.ScheduledDate IS NULL AND source.ScheduledDate IS NULL)) AND (CONVERT(date, target.StartDate) = CONVERT(date, source.StartDate) OR (target.StartDate IS NULL AND source.StartDate IS NULL)) AND (CONVERT(date, target.EndDate) = CONVERT(date, source.EndDate) OR (target.EndDate IS NULL AND source.EndDate IS NULL)) AND ISNULL(target.Description, N'') = ISNULL(source.Description, N'')) WHEN MATCHED THEN UPDATE SET HistoricalSourceKey = COALESCE(target.HistoricalSourceKey, source.HistoricalSourceKey), RequestId = COALESCE(target.RequestId, @RequestId), TechnicianId = COALESCE(target.TechnicianId, @RelatedId), Status = CASE WHEN target.Status < {m.Status} THEN {m.Status} ELSE target.Status END, ActualCost = COALESCE(target.ActualCost, {N(m.ActualCost)}) WHEN NOT MATCHED THEN INSERT (EquipmentUnitId, MaintenanceType, ManagementId, ServiceType, TechnicianId, RequestId, ScheduledDate, StartDate, EndDate, Description, Status, CompletionPercentage, Step1_Cleaning, Step2_Calibration, Step3_Testing, Step4_FinalReview, ActualCost, SatisfactionLevel, Recommendations, SuggestedNextMaintenanceDate, HistoricalSourceKey, CreatedDate, CreatedById) VALUES (@UnitId, source.MaintenanceType, @ManagementId, source.ServiceType, @RelatedId, @RequestId, source.ScheduledDate, source.StartDate, source.EndDate, source.Description, {m.Status}, {(m.Status == 2 ? 100 : 0)}, {(m.Status == 2 ? 1 : 0)}, {(m.Status == 2 ? 1 : 0)}, {(m.Status == 2 ? 1 : 0)}, {(m.Status == 2 ? 1 : 0)}, {N(m.ActualCost)}, {N(m.SatisfactionLevel)}, {S(m.Recommendations)}, {D(m.NextMaintenanceDate)}, source.HistoricalSourceKey, @Today, @CreatedById);");
        builder.AppendLine($"SELECT @MaintenanceId = Id FROM Maintenances WHERE HistoricalSourceKey = {S(m.Key)};");
        builder.AppendLine("IF @RelatedId IS NOT NULL AND NOT EXISTS (SELECT 1 FROM MaintenanceParticipants WHERE MaintenanceId = @MaintenanceId AND IsPrimary = 1 AND IsActive = 1) INSERT INTO MaintenanceParticipants (MaintenanceId, PersonId, Role, IsPrimary, IsActive, AssignedAt) VALUES (@MaintenanceId, @RelatedId, 1, 1, 1, @Today);");
        builder.AppendLine($"IF NOT EXISTS (SELECT 1 FROM @MaintenanceMap WHERE [Key] = {S(m.Key)}) INSERT INTO @MaintenanceMap ([Key], Id) VALUES ({S(m.Key)}, @MaintenanceId);");
        builder.AppendLine();
    }

    private void AppendDeparture(StringBuilder builder, DepartureRecord d)
    {
        builder.AppendLine($"-- Departure: {Comment(d.Key)}");
        builder.AppendLine("SET @DepartureId = NULL; SET @UnitId = NULL; SET @ManagementId = NULL; SET @RelatedId = NULL;");
        builder.AppendLine($"SELECT @UnitId = Id FROM @UnitMap WHERE [Key] = {S(d.InventoryNumber)};");
        builder.AppendLine($"SELECT @ManagementId = Id FROM @ManagementMap WHERE [Key] = {S(d.ManagementCode)};");
        builder.AppendLine($"SELECT @RelatedId = Id FROM @PersonMap WHERE [Key] = {S(d.BorrowerName)};");
        builder.AppendLine($"MERGE Departures AS target USING (SELECT @ManagementId AS ManagementId, @UnitId AS EquipmentUnitId, {d.Type} AS [Type], {D(d.DepartureDate)} AS DepartureDate) AS source ON target.ManagementId = source.ManagementId AND target.EquipmentUnitId = source.EquipmentUnitId AND target.[Type] = source.[Type] AND CONVERT(date, target.DepartureDate) = CONVERT(date, source.DepartureDate) WHEN MATCHED THEN UPDATE SET BorrowerId = COALESCE(target.BorrowerId, @RelatedId), EstimatedReturnDate = {D(d.EstimatedReturnDate)} WHEN NOT MATCHED THEN INSERT (ManagementId, EquipmentUnitId, BorrowerId, Type, DepartureDate, EstimatedReturnDate, DepartureObservations, Status, CreatedDate, CreatedById) VALUES (@ManagementId, @UnitId, @RelatedId, {d.Type}, {D(d.DepartureDate)}, {D(d.EstimatedReturnDate)}, {S(d.Observations)}, 0, @Today, @CreatedById);");
        builder.AppendLine($"SELECT TOP 1 @DepartureId = Id FROM Departures WHERE ManagementId = @ManagementId AND EquipmentUnitId = @UnitId AND [Type] = {d.Type} AND CONVERT(date, DepartureDate) = CONVERT(date, {D(d.DepartureDate)});");
        builder.AppendLine($"IF NOT EXISTS (SELECT 1 FROM @DepartureMap WHERE [Key] = {S(d.Key)}) INSERT INTO @DepartureMap ([Key], Id) VALUES ({S(d.Key)}, @DepartureId);");
        builder.AppendLine();
    }

    private void AppendDepartureItem(StringBuilder builder, DepartureItemRecord i)
    {
        builder.AppendLine("SET @DepartureId = NULL; SET @UnitId = NULL;");
        builder.AppendLine($"SELECT @DepartureId = Id FROM @DepartureMap WHERE [Key] = {S(i.DepartureKey)};");
        builder.AppendLine($"SELECT @UnitId = Id FROM @UnitMap WHERE [Key] = {S(i.InventoryNumber)};");
        builder.AppendLine($"MERGE DepartureItems AS target USING (SELECT @DepartureId AS DepartureId, @UnitId AS EquipmentUnitId, {S(i.ProductName)} AS ProductName) AS source ON target.DepartureId = source.DepartureId AND ISNULL(target.EquipmentUnitId, 0) = ISNULL(source.EquipmentUnitId, 0) AND target.ProductName = source.ProductName WHEN MATCHED THEN UPDATE SET IsRemoved = 0 WHEN NOT MATCHED THEN INSERT (DepartureId, EquipmentUnitId, ProductName, Quantity, UnitOfMeasure, Observations, IsRemoved, CreatedDate, CreatedById) VALUES (@DepartureId, @UnitId, source.ProductName, {i.Quantity}, {S(i.UnitOfMeasure)}, {S(i.Observations)}, 0, @Today, @CreatedById);");
        builder.AppendLine();
    }

    private void AppendKardexHistory(StringBuilder builder, KardexHistoryRecord h)
    {
        builder.AppendLine($"-- Kardex history: {Comment(h.Key)}");
        builder.AppendLine("SET @HistoryId = NULL; SET @UnitId = NULL;");
        builder.AppendLine($"SELECT @UnitId = Id FROM @UnitMap WHERE [Key] = {S(h.InventoryNumber)};");
        builder.AppendLine($"MERGE EquipmentStateHistories AS target USING (SELECT @UnitId AS EquipmentUnitId, {h.Status} AS Status, {D(h.StartDate)} AS StartDate, {S(h.Reason)} AS Reason) AS source ON target.EquipmentUnitId = source.EquipmentUnitId AND target.Status = source.Status AND CONVERT(date, target.StartDate) = CONVERT(date, source.StartDate) AND ISNULL(target.Reason, N'') = ISNULL(source.Reason, N'') WHEN NOT MATCHED THEN INSERT (EquipmentUnitId, Status, StartDate, Reason, CreatedDate, CreatedById) VALUES (source.EquipmentUnitId, source.Status, source.StartDate, source.Reason, @Today, @CreatedById);");
        builder.AppendLine($"SELECT TOP 1 @HistoryId = Id FROM EquipmentStateHistories WHERE EquipmentUnitId = @UnitId AND Status = {h.Status} AND CONVERT(date, StartDate) = CONVERT(date, {D(h.StartDate)}) AND ISNULL(Reason, N'') = ISNULL({S(h.Reason)}, N'');");
        builder.AppendLine($"IF NOT EXISTS (SELECT 1 FROM @HistoryMap WHERE [Key] = {S(h.Key)}) INSERT INTO @HistoryMap ([Key], Id) VALUES ({S(h.Key)}, @HistoryId);");
        builder.AppendLine("UPDATE EquipmentUnits SET CurrentStatus = 0 WHERE Id = @UnitId AND CurrentStatus <> 99;");
        builder.AppendLine();
    }

    private void AppendCostDetail(StringBuilder builder, CostDetailRecord c)
    {
        builder.AppendLine($"-- CostDetail: {Comment(c.TargetKey)} / {Comment(c.Concept)}");
        builder.AppendLine("SET @RequestId = NULL; SET @MaintenanceId = NULL;");
        if (c.TargetKind.Equals("Request", StringComparison.OrdinalIgnoreCase))
        {
            builder.AppendLine($"SELECT @RequestId = Id FROM @RequestMap WHERE [Key] = {S(c.TargetKey)};");
        }
        else
        {
            builder.AppendLine($"SELECT @MaintenanceId = Id FROM @MaintenanceMap WHERE [Key] = {S(c.TargetKey)};");
        }
        builder.AppendLine($"MERGE CostDetails AS target USING (SELECT @RequestId AS RequestId, @MaintenanceId AS MaintenanceId, {S(c.Concept)} AS Concept, {N(c.Quantity)} AS Quantity, {N(c.UnitPrice)} AS UnitPrice) AS source ON ISNULL(target.RequestId, 0) = ISNULL(source.RequestId, 0) AND ISNULL(target.MaintenanceId, 0) = ISNULL(source.MaintenanceId, 0) AND target.Concept = source.Concept AND target.UnitPrice = source.UnitPrice WHEN MATCHED THEN UPDATE SET Quantity = source.Quantity, UnitOfMeasure = {S(c.UnitOfMeasure)}, Category = {c.Category} WHEN NOT MATCHED THEN INSERT (RequestId, MaintenanceId, Concept, Description, Quantity, UnitOfMeasure, UnitPrice, Category, Provider, InvoiceNumber, CreatedDate, CreatedById) VALUES (@RequestId, @MaintenanceId, source.Concept, {S(c.Description)}, source.Quantity, {S(c.UnitOfMeasure)}, source.UnitPrice, {c.Category}, {S(c.Provider)}, {S(c.InvoiceNumber)}, @Today, @CreatedById);");
        builder.AppendLine();
    }

    private void AppendOriginCount(StringBuilder builder, string label, string[] entities)
    {
        var excel = _seed.Origins.Count(o => entities.Contains(o.Entity) && (o.Origin == "Excel" || o.Origin == "Excel+Inferido"));
        var inferred = _seed.Origins.Count(o => entities.Contains(o.Entity) && o.Origin == "Inferido");
        builder.AppendLine($"-- {label}: Excel directo={excel}; Inferido={inferred}");
        builder.AppendLine();
    }

    private static string S(string? value) => string.IsNullOrWhiteSpace(value) ? "NULL" : "N'" + value.Replace("'", "''") + "'";
    private static string N(int? value) => value.HasValue ? value.Value.ToString(CultureInfo.InvariantCulture) : "NULL";
    private static string N(decimal? value) => value.HasValue ? value.Value.ToString(CultureInfo.InvariantCulture) : "NULL";
    private static string D(DateTime? value) => value.HasValue ? $"CONVERT(datetime2, '{value.Value:yyyy-MM-dd}', 23)" : "NULL";
    private static int Bit(bool value) => value ? 1 : 0;
    private static string Comment(string value) => value.Replace("\r", " ").Replace("\n", " ").Replace("--", "-");
}

internal sealed record SeedScript(string FullScript, string MasterData, string Equipment, string PreventiveChains, string CorrectiveChains, string AcquisitionsAndCosts, string VerificationQuarantine);

internal sealed record SeedResult
{
    public List<string> Findings { get; } = [];
    public List<SeedOriginRecord> Origins { get; } = [];
    public List<SeedRejectRecord> Rejects { get; } = [];
    public List<SeedChainRecord> Chains { get; } = [];
    public List<FacultyRecord> Faculties { get; } = [];
    public List<CareerRecord> Careers { get; } = [];
    public List<LaboratoryRecord> Laboratories { get; } = [];
    public List<PersonRecord> People { get; } = [];
    public List<ManagementRecord> Managements { get; } = [];
    public List<EquipmentRecord> Equipments { get; } = [];
    public List<UnitRecord> Units { get; } = [];
    public List<VerificationRecord> Verifications { get; } = [];
    public List<VerificationFaultRecord> VerificationFaults { get; } = [];
    public List<RequestRecord> Requests { get; } = [];
    public List<MaintenanceRecord> Maintenances { get; } = [];
    public List<CostDetailRecord> CostDetails { get; } = [];
    public List<DepartureRecord> Departures { get; } = [];
    public List<DepartureItemRecord> DepartureItems { get; } = [];
    public List<KardexHistoryRecord> KardexHistories { get; } = [];
    public List<UnresolvedVerificationRecord> UnresolvedVerifications { get; } = [];
    public string DefaultManagementCode => Managements.OrderByDescending(m => m.Year).ThenByDescending(m => m.Semester).FirstOrDefault()?.Code ?? $"{DateTime.Now.Year}-1";
}

internal sealed class SeedChainRecord
{
    public SeedChainRecord(string key, string inventoryNumber, string managementCode)
    {
        Key = key;
        InventoryNumber = inventoryNumber;
        ManagementCode = managementCode;
    }

    public string Key { get; }
    public string InventoryNumber { get; }
    public string ManagementCode { get; }
    public bool IsCorrective { get; set; }
    public string PlanKey { get; set; } = string.Empty;
    public string? VerificationKey { get; set; }
    public string? TechnicalRequestKey { get; set; }
    public string? MaintenanceKey { get; set; }
    public string? LinkedDepartureKey { get; set; }
    public string? KardexHistoryKey { get; set; }
    public string? AcquisitionRequestKey { get; set; }
    public int CurrentPhase { get; set; }
    public int CurrentState { get; set; }
    public int PlanStatus { get; set; }
    public string? Responsible { get; set; }
    public DateTime? PlannedDate { get; set; }
    public List<RequestRecord> Requests { get; } = [];
    public List<MaintenanceRecord> Maintenances { get; } = [];
    public List<DepartureRecord> Departures { get; } = [];
    public List<ManagementPlanRecord> OriginalPlans { get; } = [];
}

internal sealed record SeedOriginRecord(string Entity, string Key, string Origin, string SourceSheet, int SourceRow, string Notes);
internal sealed record SeedRejectRecord(string Entity, string Key, string Reason);
internal sealed record UnresolvedVerificationRecord(
    string Key,
    string InventoryRaw,
    string DateRaw,
    string PhysicalConditionRaw,
    string FindingRaw,
    string EquipmentName,
    string Column6Raw,
    string Column7Raw,
    string Reason,
    string SourceSheet,
    int SourceRow);
internal sealed record KardexHistoryRecord(string Key, string InventoryNumber, int Status, DateTime StartDate, string Reason, string SourceSheet, int SourceRow);

internal sealed record AnalysisResult
{
    public List<SheetSummary> SheetSummaries { get; } = [];
    public List<string> KnownFindings { get; } = [];
    public List<IssueRecord> Issues { get; } = [];
    public List<RejectRecord> Rejects { get; } = [];
    public List<FacultyRecord> Faculties { get; } = [];
    public List<CareerRecord> Careers { get; } = [];
    public List<LaboratoryRecord> Laboratories { get; } = [];
    public List<PersonRecord> People { get; } = [];
    public List<ManagementRecord> Managements { get; } = [];
    public List<EquipmentRecord> Equipments { get; } = [];
    public List<UnitRecord> Units { get; } = [];
    public List<VerificationRecord> Verifications { get; } = [];
    public List<VerificationFaultRecord> VerificationFaults { get; } = [];
    public List<RequestRecord> Requests { get; } = [];
    public List<MaintenanceRecord> Maintenances { get; } = [];
    public List<CostDetailRecord> CostDetails { get; } = [];
    public List<DepartureRecord> Departures { get; } = [];
    public List<DepartureItemRecord> DepartureItems { get; } = [];
    public List<ManagementPlanRecord> ManagementPlans { get; } = [];
    public List<UnresolvedVerificationRecord> UnresolvedVerifications { get; } = [];
    public string DefaultManagementCode => Managements.OrderByDescending(m => m.Year).ThenByDescending(m => m.Semester).FirstOrDefault()?.Code ?? $"{DateTime.Now.Year}-1";
}

internal sealed record SheetSummary(string Name, int DataRows, int MaxRow, int MaxCol);
internal readonly record struct SheetRow(ExcelWorksheet Worksheet, int Number)
{
    public static implicit operator int(SheetRow row) => row.Number;
    public override string ToString() => Number.ToString(CultureInfo.InvariantCulture);
}

internal sealed record IssueRecord(string Severity, string Sheet, int Row, string Column, string Message);
internal sealed record RejectRecord(string Sheet, int Row, string Reason);
internal sealed record InventorySplit(List<string> ValidTokens, List<string> InvalidTokens);
internal sealed record FacultyRecord(string Name, string? Code, string? Description, string SourceSheet, int SourceRow);
internal sealed record CareerRecord(string Name, string FacultyName, int Status, string SourceSheet, int SourceRow);
internal sealed record LaboratoryRecord(string Code, string Name, string FacultyName, string? Floor, string? Description, string SourceSheet, int SourceRow);
internal sealed record PersonRecord(string Name, bool IsExternal, bool IsCompany, int Category, string? Email, string? Phone, string? Address, string SourceSheet, int SourceRow);
internal sealed record ManagementRecord(string Code, int Year, int Semester, string Description, int SourceRow);
internal sealed class EquipmentRecord
{
    public EquipmentRecord(string key, string name, string? brand, string? model, string? description, int category, int typeClassification, int? usefulLifeYears, string sourceSheet, int sourceRow)
    {
        Key = key;
        Name = name;
        Brand = brand;
        Model = model;
        Description = description;
        Category = category;
        TypeClassification = typeClassification;
        UsefulLifeYears = usefulLifeYears;
        SourceSheet = sourceSheet;
        SourceRow = sourceRow;
    }

    public string Key { get; }
    public string Name { get; }
    public string? Brand { get; }
    public string? Model { get; }
    public string? Description { get; set; }
    public int Category { get; }
    public int TypeClassification { get; }
    public int? UsefulLifeYears { get; }
    public string SourceSheet { get; }
    public int SourceRow { get; }
}

internal sealed class UnitRecord
{
    public UnitRecord(string inventoryNumber, string equipmentKey, string? serialNumber, string? laboratoryCode, string? careerName, DateTime? acquisitionDate, DateTime? manufacturingDate, decimal? acquisitionValue, int status, int? physicalCondition, string? notes, string sourceSheet, int sourceRow)
    {
        InventoryNumber = inventoryNumber;
        EquipmentKey = equipmentKey;
        SerialNumber = serialNumber;
        LaboratoryCode = laboratoryCode;
        CareerName = careerName;
        AcquisitionDate = acquisitionDate;
        ManufacturingDate = manufacturingDate;
        AcquisitionValue = acquisitionValue;
        Status = status;
        PhysicalCondition = physicalCondition;
        Notes = notes;
        SourceSheet = sourceSheet;
        SourceRow = sourceRow;
    }

    public string InventoryNumber { get; }
    public string EquipmentKey { get; }
    public string? SerialNumber { get; set; }
    public string? LaboratoryCode { get; set; }
    public string? CareerName { get; set; }
    public DateTime? AcquisitionDate { get; set; }
    public DateTime? ManufacturingDate { get; set; }
    public decimal? AcquisitionValue { get; set; }
    public int Status { get; set; }
    public int? PhysicalCondition { get; set; }
    public string? Notes { get; }
    public string SourceSheet { get; }
    public int SourceRow { get; }
}

internal sealed record VerificationRecord(string Key, string InventoryNumber, string ManagementCode, DateTime Date, int PhysicalCondition, int Status, string? Observations, string SourceSheet, int SourceRow);
internal sealed record VerificationFaultRecord(string VerificationKey, string Description, string SourceSheet, int SourceRow);
internal sealed record RequestRecord(string Key, string InventoryNumber, string? LaboratoryCode, string EquipmentKey, string ManagementCode, int Priority, int Status, int Type, DateTime CreatedDate, string Description, string? Observations, string? EstimatedRepairTime, string? InvestmentCode, string? CostCenter, string SourceSheet, int SourceRow);
internal sealed record MaintenanceRecord(string Key, string InventoryNumber, string ManagementCode, int MaintenanceType, int ServiceType, string? TechnicianName, string? RequestKey, DateTime? ScheduledDate, DateTime? StartDate, DateTime? EndDate, string? Description, int Status, decimal? ActualCost, int? SatisfactionLevel, string? Recommendations, DateTime? NextMaintenanceDate, string SourceSheet, int SourceRow);
internal sealed record CostDetailRecord(string TargetKind, string TargetKey, string Concept, string? Description, decimal Quantity, string? UnitOfMeasure, decimal UnitPrice, int Category, string? Provider, string? InvoiceNumber, string SourceSheet, int SourceRow);
internal sealed record DepartureRecord(string Key, string InventoryNumber, string ManagementCode, string BorrowerName, int Type, DateTime DepartureDate, DateTime EstimatedReturnDate, string? Observations, string SourceSheet, int SourceRow);
internal sealed record DepartureItemRecord(string DepartureKey, string InventoryNumber, string ProductName, int Quantity, string? UnitOfMeasure, string? Observations, string SourceSheet, int SourceRow);
internal sealed record ManagementPlanRecord(string Key, string ManagementCode, string InventoryNumber, int CurrentPhase, int CurrentState, string? Responsible, DateTime? PlannedDate, int PlanStatus, string SourceSheet, int SourceRow);
