using System.Globalization;
using System.IO.Compression;
using System.Xml.Linq;

internal sealed class V2WorkbookReader
{
    internal static readonly string[] SentinelSheets =
    [
        "00_DICCIONARIO",
        "04_CATALOGO_EQUIPOS",
        "05_UNIDADES_FISICAS",
        "91_MAPA_ORIGEN",
        "92_PENDIENTES_CALIDAD"
    ];

    private static readonly SheetDefinition[] Definitions =
    [
        new("01_FACULTADES", "Faculty", "FacultadCodigo", ["FacultadCodigo", "Nombre", "Descripcion", "Estado"], ["FacultadCodigo", "Nombre"]),
        new("02_CARRERAS", "Career", "CarreraCodigo", ["CarreraCodigo", "Nombre", "FacultadCodigo", "Estado"], ["CarreraCodigo", "Nombre", "FacultadCodigo"]),
        new("03_LABORATORIOS", "Laboratory", "LaboratorioCodigo", ["LaboratorioCodigo", "Nombre", "FacultadCodigo", "Tipo", "Edificio", "Bloque", "Piso", "Ambiente", "Descripcion", "Estado"], ["LaboratorioCodigo", "Nombre", "FacultadCodigo"]),
        new("04_CATALOGO_EQUIPOS", "Equipment", "CatalogoCodigo", ["CatalogoCodigo", "NombreGenerico", "Categoria", "TipoUtensilio", "Clasificacion", "Marca", "Modelo", "PaisOrigen", "VidaUtilAnios", "Especificaciones", "Estado"], ["CatalogoCodigo", "NombreGenerico"]),
        new("05_UNIDADES_FISICAS", "EquipmentUnit", "NumeroInventario", ["NumeroInventario", "CatalogoCodigo", "NumeroSerie", "LaboratorioCodigo", "EstadoUbicacion", "CarreraResponsableCodigo", "UbicacionInterna", "FechaAdquisicion", "FechaFabricacion", "ValorAdquisicionBs", "EstadoOperativo", "CondicionFisica", "Observaciones"], ["NumeroInventario", "CatalogoCodigo", "EstadoUbicacion"]),
        new("06_ACTORES", "Actor", "ActorCodigo", ["ActorCodigo", "NombreRazonSocial", "TipoActor", "Vinculacion", "Email", "Telefono", "Direccion", "Estado"], ["ActorCodigo", "NombreRazonSocial", "TipoActor"]),
        new("07_ACTOR_ROLES", "ActorRole", "ActorRolCodigo", ["ActorRolCodigo", "ActorCodigo", "Rol", "Estado"], ["ActorRolCodigo", "ActorCodigo", "Rol"]),
        new("08_GESTIONES", "Management", "GestionCodigo", ["GestionCodigo", "TipoGestion", "Anio", "Semestre", "FechaInicio", "FechaFin", "FacultadCodigo", "Estado"], ["GestionCodigo", "TipoGestion", "Anio", "Semestre"]),
        new("09_ARTICULOS", "Article", "ArticuloCodigo", ["ArticuloCodigo", "Nombre", "Categoria", "UnidadMedida", "Estado"], ["ArticuloCodigo", "Nombre", "UnidadMedida"]),
        new("10_VERIFICACIONES_L6", "Verification", "VerificacionCodigo", ["VerificacionCodigo", "NumeroInventario", "GestionCodigo", "FechaVerificacion", "CondicionFisica", "EstadoEquipo", "ProblemasObservados", "Observaciones", "ResponsableActorCodigo"], ["VerificacionCodigo", "NumeroInventario", "GestionCodigo"]),
        new("11_SOLICITUDES_L7", "Request", "SolicitudCodigo", ["SolicitudCodigo", "GestionCodigo", "FechaSolicitud", "Prioridad", "ProblemaReportado", "Sugerencia", "TiempoEstimado", "SolicitanteActorCodigo", "TipoSolicitud", "Estado"], ["SolicitudCodigo", "GestionCodigo", "ProblemaReportado"]),
        new("12_SOLICITUD_UNIDADES", "RequestEquipmentUnit", "SolicitudUnidadCodigo", ["SolicitudUnidadCodigo", "SolicitudCodigo", "NumeroInventario", "Observacion"], ["SolicitudUnidadCodigo", "SolicitudCodigo", "NumeroInventario"]),
        new("13_MANTENIMIENTOS_L8", "Maintenance", "MantenimientoCodigo", ["MantenimientoCodigo", "NumeroInventario", "GestionCodigo", "FechaProgramada", "FechaInicio", "FechaFin", "TipoMantenimiento", "TipoServicio", "TrabajoRealizado", "Estado", "Satisfaccion", "Recomendaciones", "ProximoMantenimiento", "CostoTotalFuenteBs", "CostoDetalleCalculadoBs", "DiferenciaCostoBs", "EstadoCalidad"], ["MantenimientoCodigo", "NumeroInventario", "GestionCodigo", "TipoMantenimiento"]),
        new("14_MANT_SOLICITUDES", "MaintenanceRequest", "MantenimientoSolicitudCodigo", ["MantenimientoSolicitudCodigo", "MantenimientoCodigo", "SolicitudCodigo", "Observacion"], ["MantenimientoSolicitudCodigo", "MantenimientoCodigo", "SolicitudCodigo"]),
        new("15_MANT_PARTICIPANTES", "MaintenanceParticipant", "ParticipanteCodigo", ["ParticipanteCodigo", "MantenimientoCodigo", "ActorCodigo", "Rol", "EsPrincipal", "Estado"], ["ParticipanteCodigo", "MantenimientoCodigo", "ActorCodigo", "Rol"]),
        new("16_COSTOS", "CostDetail", "CostoCodigo", ["CostoCodigo", "SolicitudCodigo", "MantenimientoCodigo", "Fecha", "Concepto", "Categoria", "Cantidad", "UnidadMedida", "PrecioUnitarioBs", "SubtotalBs", "ProveedorActorCodigo", "NumeroFactura", "TipoReferenciaOrigen", "ReferenciaOrigen", "EstadoCalidad", "InventarioOrigen", "UnidadSolicitanteOrigen", "CentroCostoOrigen", "ResponsableActorCodigo"], ["CostoCodigo", "Concepto", "Cantidad", "PrecioUnitarioBs"]),
        new("17_SALIDAS_L3", "Departure", "SalidaCodigo", ["SalidaCodigo", "GestionCodigo", "Modalidad", "LaboratorioOrigenCodigo", "Destino", "ResponsableActorCodigo", "FechaSalida", "RetornoEstimado", "Estado", "Observaciones"], ["SalidaCodigo", "GestionCodigo", "Modalidad"]),
        new("18_SALIDA_DETALLES", "DepartureItem", "SalidaDetalleCodigo", ["SalidaDetalleCodigo", "SalidaCodigo", "NumeroInventario", "ArticuloCodigo", "DescripcionOriginal", "Cantidad", "UnidadMedida"], ["SalidaDetalleCodigo", "SalidaCodigo", "Cantidad"]),
        new("19_PLAN_L48", "MaintenancePlan", "PlanCodigo", ["PlanCodigo", "GestionCodigo", "NumeroInventario", "TipoMantenimiento", "TipoServicio", "ResponsableActorCodigo", "FechaPlanificada", "Estado", "EstadoCalidad"], ["PlanCodigo", "GestionCodigo", "NumeroInventario", "TipoMantenimiento"]),
        new("91_MAPA_ORIGEN", "ImportSourceRow", "MapaCodigo", ["MapaCodigo", "TipoEntidad", "CodigoDestino", "HojaOrigen", "FilaOrigen", "Resultado", "ReglaAplicada", "ValorClaveOrigen", "FilaOrigenCodigo"], ["MapaCodigo", "TipoEntidad", "HojaOrigen", "FilaOrigen", "Resultado"]),
        new("92_PENDIENTES_CALIDAD", "DataQualityIssue", "IncidenciaCodigo", ["IncidenciaCodigo", "Severidad", "HojaOrigen", "FilaOrigen", "TipoEntidad", "CodigoRegistro", "CodigoProblema", "ValorOriginal", "Candidatos", "EstadoResolucion", "NotaResolucion", "FilaOrigenCodigo"], ["IncidenciaCodigo", "Severidad", "CodigoProblema", "EstadoResolucion"])
    ];

    public CanonicalImportPackage Read(string workbookPath)
    {
        var package = new CanonicalImportPackage
        {
            Contract = ImportContract.V2,
            WorkbookPath = workbookPath,
            WorkbookSha256 = CanonicalImportPackage.HashFile(workbookPath),
            Strict = true
        };

        var excel = RawXlsxWorkbook.Load(workbookPath);
        package.SheetNames.AddRange(excel.SheetNames);
        var controlledLists = ReadControlledLists(excel, package);

        foreach (var sentinel in SentinelSheets)
        {
            if (!excel.TryGetSheet(sentinel, out _))
            {
                package.Issues.Add(new CanonicalIssue(DataPriority.P0, "V2_SENTINEL_MISSING",
                    $"Falta la hoja identificadora obligatoria '{sentinel}'.", sentinel,
                    SuggestedAction: "Usar la plantilla V2 oficial sin renombrar hojas."));
            }
        }

        foreach (var definition in Definitions)
        {
            if (!excel.TryGetSheet(definition.Sheet, out var worksheet))
            {
                package.Issues.Add(new CanonicalIssue(DataPriority.P0, "V2_SHEET_MISSING",
                    $"Falta la hoja contractual '{definition.Sheet}'.", definition.Sheet,
                    SuggestedAction: "Restaurar la hoja desde la plantilla V2 oficial."));
                continue;
            }

            ReadSheet(package, worksheet, definition);
        }

        CanonicalPackageValidator.Validate(package, controlledLists);
        return package;
    }

    public static bool HasV2Sentinels(string workbookPath, out IReadOnlyList<string> present)
    {
        var excel = RawXlsxWorkbook.Load(workbookPath);
        present = SentinelSheets.Where(name => excel.TryGetSheet(name, out _)).ToArray();
        return present.Count == SentinelSheets.Length;
    }

    private static void ReadSheet(CanonicalImportPackage package, RawXlsxSheet worksheet, SheetDefinition definition)
    {
        if (!worksheet.Rows.TryGetValue(1, out var headerRow))
        {
            package.Issues.Add(new CanonicalIssue(DataPriority.P0, "V2_EMPTY_SHEET",
                $"La hoja contractual '{worksheet.Name}' está vacía.", worksheet.Name, 1));
            return;
        }

        var headerByName = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        foreach (var cell in headerRow.OrderBy(pair => pair.Key))
        {
            var header = cell.Value?.Trim();
            if (!string.IsNullOrWhiteSpace(header) && !headerByName.TryAdd(header, cell.Key))
            {
                package.Issues.Add(new CanonicalIssue(DataPriority.P0, "V2_DUPLICATE_COLUMN",
                    $"La columna '{header}' está repetida.", worksheet.Name, 1, SuggestedAction: "Restaurar los encabezados oficiales."));
            }
        }

        foreach (var expected in definition.Columns)
        {
            if (!headerByName.ContainsKey(expected))
            {
                package.Issues.Add(new CanonicalIssue(DataPriority.P0, "V2_COLUMN_MISSING",
                    $"Falta la columna contractual '{expected}'.", worksheet.Name, 1,
                    SuggestedAction: "No renombrar ni eliminar columnas de la plantilla V2."));
            }
        }

        if (!headerByName.TryGetValue(definition.KeyColumn, out var keyColumn))
        {
            return;
        }

        foreach (var rowData in worksheet.Rows.Where(pair => pair.Key >= 2).OrderBy(pair => pair.Key))
        {
            var row = rowData.Key;
            var fields = new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase);
            var hasData = false;
            foreach (var columnName in definition.Columns)
            {
                var value = headerByName.TryGetValue(columnName, out var column)
                    ? worksheet.Get(row, column)
                    : null;
                fields[columnName] = value;
                hasData |= !string.IsNullOrWhiteSpace(value);
            }

            if (!hasData)
            {
                continue;
            }

            var key = worksheet.Get(row, keyColumn);
            if (string.IsNullOrWhiteSpace(key))
            {
                key = $"SIN-CODIGO:{worksheet.Name}:{row}";
                package.Issues.Add(new CanonicalIssue(DataPriority.P0, "V2_KEY_EMPTY",
                    $"La fila no tiene '{definition.KeyColumn}'.", worksheet.Name, row, key,
                    "Asignar un código estable; el importador no lo inventará."));
            }

            foreach (var required in definition.RequiredValues)
            {
                if (!fields.TryGetValue(required, out var value) || string.IsNullOrWhiteSpace(value))
                {
                    package.Issues.Add(new CanonicalIssue(DataPriority.P0, "V2_REQUIRED_EMPTY",
                        $"El campo obligatorio '{required}' está vacío.", worksheet.Name, row, key,
                        "Completar el dato con evidencia o registrar formalmente la fila como pendiente."));
                }
            }

            package.Add(new CanonicalRow(definition.Entity, key, fields,
                new SourceReference(worksheet.Name, row, $"{worksheet.Name}:{row}")));
        }
    }

    private static IReadOnlyDictionary<string, IReadOnlySet<string>>? ReadControlledLists(
        RawXlsxWorkbook workbook,
        CanonicalImportPackage package)
    {
        const string sheetName = "90_LISTAS_CONTROLADAS";
        if (!workbook.TryGetSheet(sheetName, out var worksheet))
        {
            package.Issues.Add(new CanonicalIssue(DataPriority.P0, "V2_CONTROL_LIST_SHEET_MISSING",
                $"Falta la hoja contractual '{sheetName}'; no es seguro interpretar estados, tipos ni otros dominios.",
                sheetName, SuggestedAction: "Restaurar la hoja desde la plantilla V2 oficial antes de preparar staging SQL."));
            return null;
        }

        if (!worksheet.Rows.TryGetValue(1, out var headerRow))
        {
            package.Issues.Add(new CanonicalIssue(DataPriority.P0, "V2_CONTROL_LIST_EMPTY",
                $"La hoja '{sheetName}' no contiene encabezados de dominios.", sheetName, 1,
                SuggestedAction: "Restaurar las listas controladas oficiales."));
            return null;
        }

        var headerColumns = new Dictionary<int, string>();
        var names = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var cell in headerRow.OrderBy(pair => pair.Key))
        {
            var name = cell.Value?.Trim();
            if (string.IsNullOrWhiteSpace(name))
            {
                continue;
            }

            if (!names.Add(name))
            {
                package.Issues.Add(new CanonicalIssue(DataPriority.P0, "V2_CONTROL_LIST_DUPLICATE_HEADER",
                    $"El dominio controlado '{name}' aparece más de una vez.", sheetName, 1,
                    SuggestedAction: "Conservar una sola columna por dominio y actualizar las validaciones de la plantilla."));
                continue;
            }

            headerColumns[cell.Key] = name;
        }

        var mutable = headerColumns.Values.ToDictionary(
            name => name,
            _ => new HashSet<string>(StringComparer.OrdinalIgnoreCase),
            StringComparer.OrdinalIgnoreCase);

        foreach (var row in worksheet.Rows.Keys.Where(row => row >= 2).OrderBy(row => row))
        {
            var firstValue = worksheet.Get(row, 1);
            if (firstValue?.StartsWith("HOJA CONTROLADA", StringComparison.OrdinalIgnoreCase) == true)
            {
                break;
            }

            foreach (var column in headerColumns)
            {
                var value = worksheet.Get(row, column.Key);
                if (!string.IsNullOrWhiteSpace(value))
                {
                    mutable[column.Value].Add(value);
                }
            }
        }

        return mutable.ToDictionary(
            pair => pair.Key,
            pair => (IReadOnlySet<string>)pair.Value,
            StringComparer.OrdinalIgnoreCase);
    }

    private sealed record SheetDefinition(
        string Sheet,
        string Entity,
        string KeyColumn,
        string[] Columns,
        string[] RequiredValues);
}

internal sealed class RawXlsxWorkbook
{
    private static readonly XNamespace Spreadsheet = "http://schemas.openxmlformats.org/spreadsheetml/2006/main";
    private static readonly XNamespace OfficeRelationships = "http://schemas.openxmlformats.org/officeDocument/2006/relationships";
    private static readonly XNamespace PackageRelationships = "http://schemas.openxmlformats.org/package/2006/relationships";
    private readonly Dictionary<string, RawXlsxSheet> _sheets = new(StringComparer.OrdinalIgnoreCase);

    public IReadOnlyList<string> SheetNames => _sheets.Keys.ToArray();

    public bool TryGetSheet(string name, out RawXlsxSheet sheet) => _sheets.TryGetValue(name, out sheet!);

    public static RawXlsxWorkbook Load(string path)
    {
        try
        {
            using var archive = ZipFile.OpenRead(path);
            var workbookDocument = ReadXml(archive, "xl/workbook.xml");
            var relationsDocument = ReadXml(archive, "xl/_rels/workbook.xml.rels");
            var relationships = relationsDocument.Root?
                .Elements(PackageRelationships + "Relationship")
                .Where(element => element.Attribute("Id") is not null && element.Attribute("Target") is not null)
                .ToDictionary(element => (string)element.Attribute("Id")!, element => NormalizeTarget((string)element.Attribute("Target")!), StringComparer.Ordinal)
                ?? new Dictionary<string, string>(StringComparer.Ordinal);
            var sharedStrings = ReadSharedStrings(archive);
            var dateStyles = ReadDateStyles(archive);
            var workbook = new RawXlsxWorkbook();

            foreach (var sheetElement in workbookDocument.Descendants(Spreadsheet + "sheet"))
            {
                var name = (string?)sheetElement.Attribute("name");
                var relationshipId = (string?)sheetElement.Attribute(OfficeRelationships + "id");
                if (string.IsNullOrWhiteSpace(name) || string.IsNullOrWhiteSpace(relationshipId)
                    || !relationships.TryGetValue(relationshipId, out var sheetPath))
                {
                    continue;
                }

                workbook._sheets[name] = ReadSheet(archive, sheetPath, name, sharedStrings, dateStyles);
            }

            return workbook;
        }
        catch (InvalidDataException)
        {
            throw;
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException or System.Xml.XmlException)
        {
            throw new InvalidDataException($"El archivo XLSX no es legible: {exception.Message}", exception);
        }
    }

    private static RawXlsxSheet ReadSheet(
        ZipArchive archive,
        string path,
        string name,
        IReadOnlyList<string> sharedStrings,
        IReadOnlySet<int> dateStyles)
    {
        var document = ReadXml(archive, path);
        var sheet = new RawXlsxSheet(name);
        foreach (var rowElement in document.Descendants(Spreadsheet + "row"))
        {
            var declaredRow = ParseInteger((string?)rowElement.Attribute("r"));
            var inferredRow = declaredRow > 0 ? declaredRow : sheet.Rows.Count + 1;
            foreach (var cell in rowElement.Elements(Spreadsheet + "c"))
            {
                var reference = (string?)cell.Attribute("r");
                var column = ColumnNumber(reference);
                if (column <= 0)
                {
                    continue;
                }

                var value = ReadCell(cell, sharedStrings, dateStyles);
                if (value is not null)
                {
                    sheet.Set(inferredRow, column, value);
                }
            }
        }

        return sheet;
    }

    private static string? ReadCell(XElement cell, IReadOnlyList<string> sharedStrings, IReadOnlySet<int> dateStyles)
    {
        var type = (string?)cell.Attribute("t");
        if (type == "inlineStr")
        {
            return string.Concat(cell.Descendants(Spreadsheet + "t").Select(text => text.Value)).Trim();
        }

        var raw = cell.Element(Spreadsheet + "v")?.Value;
        if (raw is null)
        {
            return null;
        }

        if (type == "s" && int.TryParse(raw, NumberStyles.Integer, CultureInfo.InvariantCulture, out var sharedIndex))
        {
            return sharedIndex >= 0 && sharedIndex < sharedStrings.Count ? sharedStrings[sharedIndex].Trim() : raw;
        }

        if (type == "b")
        {
            return raw == "1" ? "true" : "false";
        }

        if (type == "d" && DateTime.TryParse(raw, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind, out var isoDate))
        {
            return isoDate.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
        }

        var styleIndex = ParseInteger((string?)cell.Attribute("s"));
        if (dateStyles.Contains(styleIndex)
            && double.TryParse(raw, NumberStyles.Float, CultureInfo.InvariantCulture, out var serial))
        {
            try
            {
                return DateTime.FromOADate(serial).ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
            }
            catch (ArgumentException)
            {
                return raw;
            }
        }

        return raw.Trim();
    }

    private static List<string> ReadSharedStrings(ZipArchive archive)
    {
        var entry = archive.GetEntry("xl/sharedStrings.xml");
        if (entry is null)
        {
            return [];
        }

        using var stream = entry.Open();
        var document = XDocument.Load(stream, LoadOptions.None);
        return document.Descendants(Spreadsheet + "si")
            .Select(item => string.Concat(item.Descendants(Spreadsheet + "t").Select(text => text.Value)))
            .ToList();
    }

    private static HashSet<int> ReadDateStyles(ZipArchive archive)
    {
        var result = new HashSet<int>();
        var entry = archive.GetEntry("xl/styles.xml");
        if (entry is null)
        {
            return result;
        }

        using var stream = entry.Open();
        var document = XDocument.Load(stream, LoadOptions.None);
        var customDateFormats = document.Descendants(Spreadsheet + "numFmt")
            .Where(element => LooksLikeDate((string?)element.Attribute("formatCode")))
            .Select(element => ParseInteger((string?)element.Attribute("numFmtId")))
            .ToHashSet();
        var cellFormats = document.Root?.Element(Spreadsheet + "cellXfs")?.Elements(Spreadsheet + "xf").ToArray() ?? [];
        for (var index = 0; index < cellFormats.Length; index++)
        {
            var formatId = ParseInteger((string?)cellFormats[index].Attribute("numFmtId"));
            if ((formatId >= 14 && formatId <= 22) || (formatId >= 45 && formatId <= 47) || customDateFormats.Contains(formatId))
            {
                result.Add(index);
            }
        }

        return result;
    }

    private static bool LooksLikeDate(string? formatCode)
    {
        if (string.IsNullOrWhiteSpace(formatCode))
        {
            return false;
        }

        var normalized = formatCode.ToLowerInvariant().Replace("\\", string.Empty);
        return normalized.Contains('y') && normalized.Contains('d')
            || normalized.Contains("dd/") || normalized.Contains("mm/");
    }

    private static XDocument ReadXml(ZipArchive archive, string path)
    {
        var entry = archive.GetEntry(path) ?? throw new InvalidDataException($"El XLSX no contiene '{path}'.");
        using var stream = entry.Open();
        return XDocument.Load(stream, LoadOptions.None);
    }

    private static string NormalizeTarget(string target)
    {
        var normalized = target.Replace('\\', '/').TrimStart('/');
        if (normalized.StartsWith("xl/", StringComparison.OrdinalIgnoreCase))
        {
            return normalized;
        }

        while (normalized.StartsWith("../", StringComparison.Ordinal))
        {
            normalized = normalized[3..];
        }

        return $"xl/{normalized}";
    }

    private static int ColumnNumber(string? reference)
    {
        var result = 0;
        foreach (var character in reference ?? string.Empty)
        {
            if (!char.IsLetter(character))
            {
                break;
            }

            result = checked(result * 26 + char.ToUpperInvariant(character) - 'A' + 1);
        }

        return result;
    }

    private static int ParseInteger(string? value) =>
        int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var parsed) ? parsed : 0;
}

internal sealed class RawXlsxSheet
{
    public RawXlsxSheet(string name)
    {
        Name = name;
    }

    public string Name { get; }
    public SortedDictionary<int, Dictionary<int, string>> Rows { get; } = [];

    public string? Get(int row, int column) =>
        Rows.TryGetValue(row, out var cells) && cells.TryGetValue(column, out var value) && !string.IsNullOrWhiteSpace(value)
            ? value.Trim()
            : null;

    public void Set(int row, int column, string value)
    {
        if (!Rows.TryGetValue(row, out var cells))
        {
            cells = [];
            Rows[row] = cells;
        }

        cells[column] = value;
    }
}

internal static class CanonicalPackageValidator
{
    private static readonly string[] DateFields =
    [
        "FechaInicio", "FechaFin", "FechaAdquisicion", "FechaFabricacion", "FechaVerificacion",
        "FechaSolicitud", "FechaProgramada", "ProximoMantenimiento", "Fecha", "FechaSalida",
        "RetornoEstimado", "FechaPlanificada"
    ];

    private static readonly string[] RequiredControlledDomains =
    [
        "EstadoGeneral", "EstadoUbicacion", "EstadoOperativo", "CondicionFisica", "Prioridad",
        "TipoMantenimiento", "TipoServicio", "RolActor", "TipoActor", "Vinculacion",
        "UnidadMedida", "CategoriaCosto", "ModalidadSalida", "EstadoCalidad", "EstadoResolucion",
        "EstadoPlan", "TipoGestion", "EstadoGestion", "ResultadoVerificacion", "CategoriaEquipo",
        "TipoUtensilio", "ClasificacionEquipo"
    ];

    private static readonly DomainRule[] DomainRules =
    [
        new("Faculty", "Estado", "EstadoGeneral"),
        new("Career", "Estado", "EstadoGeneral"),
        new("Laboratory", "Estado", "EstadoGeneral"),
        new("Equipment", "Categoria", "CategoriaEquipo"),
        new("Equipment", "TipoUtensilio", "TipoUtensilio"),
        new("Equipment", "Clasificacion", "ClasificacionEquipo"),
        new("Equipment", "Estado", "EstadoGeneral"),
        new("EquipmentUnit", "EstadoUbicacion", "EstadoUbicacion"),
        new("EquipmentUnit", "EstadoOperativo", "EstadoOperativo"),
        new("EquipmentUnit", "CondicionFisica", "CondicionFisica"),
        new("Actor", "TipoActor", "TipoActor"),
        new("Actor", "Vinculacion", "Vinculacion"),
        new("Actor", "Estado", "EstadoGeneral"),
        new("ActorRole", "Rol", "RolActor"),
        new("ActorRole", "Estado", "EstadoGeneral"),
        new("Management", "TipoGestion", "TipoGestion"),
        new("Management", "Estado", "EstadoGestion"),
        new("Article", "UnidadMedida", "UnidadMedida"),
        new("Article", "Estado", "EstadoGeneral"),
        new("Verification", "CondicionFisica", "CondicionFisica"),
        new("Verification", "EstadoEquipo", "ResultadoVerificacion"),
        new("Request", "Prioridad", "Prioridad"),
        new("Request", "Estado", "EstadoGeneral"),
        new("Maintenance", "TipoMantenimiento", "TipoMantenimiento"),
        new("Maintenance", "TipoServicio", "TipoServicio"),
        new("Maintenance", "Estado", "EstadoGeneral"),
        new("Maintenance", "EstadoCalidad", "EstadoCalidad"),
        new("MaintenanceParticipant", "Rol", "RolActor"),
        new("MaintenanceParticipant", "Estado", "EstadoGeneral"),
        new("CostDetail", "Categoria", "CategoriaCosto"),
        new("CostDetail", "UnidadMedida", "UnidadMedida"),
        new("CostDetail", "EstadoCalidad", "EstadoCalidad"),
        new("Departure", "Modalidad", "ModalidadSalida"),
        new("Departure", "Estado", "EstadoGeneral"),
        new("DepartureItem", "UnidadMedida", "UnidadMedida"),
        new("MaintenancePlan", "TipoMantenimiento", "TipoMantenimiento"),
        new("MaintenancePlan", "TipoServicio", "TipoServicio"),
        new("MaintenancePlan", "Estado", "EstadoPlan"),
        new("MaintenancePlan", "EstadoCalidad", "EstadoCalidad"),
        new("DataQualityIssue", "EstadoResolucion", "EstadoResolucion")
    ];

    private static readonly LengthRule[] LengthRules =
    [
        new("Faculty", "FacultadCodigo", 50), new("Faculty", "Nombre", 200), new("Faculty", "Descripcion", 500),
        new("Career", "CarreraCodigo", 30), new("Career", "Nombre", 200),
        new("Laboratory", "LaboratorioCodigo", 20), new("Laboratory", "Nombre", 200),
        new("Laboratory", "Tipo", 100), new("Laboratory", "Edificio", 100), new("Laboratory", "Bloque", 50),
        new("Laboratory", "Piso", 50), new("Laboratory", "Ambiente", 100), new("Laboratory", "Descripcion", 1000),
        new("Equipment", "CatalogoCodigo", 30), new("Equipment", "NombreGenerico", 200),
        new("Equipment", "Marca", 100), new("Equipment", "Modelo", 100), new("Equipment", "PaisOrigen", 100),
        new("Equipment", "Especificaciones", 2000),
        new("EquipmentUnit", "NumeroInventario", 50), new("EquipmentUnit", "NumeroSerie", 100),
        new("EquipmentUnit", "UbicacionInterna", 200), new("EquipmentUnit", "Observaciones", 2000),
        new("Actor", "ActorCodigo", 30), new("Actor", "NombreRazonSocial", 200), new("Actor", "Email", 100),
        new("Actor", "Telefono", 20), new("Actor", "Direccion", 500),
        new("Management", "GestionCodigo", 50),
        new("Article", "ArticuloCodigo", 30), new("Article", "Nombre", 200),
        new("Article", "Categoria", 100), new("Article", "UnidadMedida", 50),
        new("Request", "SolicitudCodigo", 200), new("Request", "ProblemaReportado", 1000),
        new("Request", "Sugerencia", 2000), new("Request", "TiempoEstimado", 100),
        new("Maintenance", "MantenimientoCodigo", 200), new("Maintenance", "TrabajoRealizado", 2000),
        new("Maintenance", "Recomendaciones", 1000),
        new("CostDetail", "CostoCodigo", 200), new("CostDetail", "Concepto", 200),
        new("CostDetail", "UnidadMedida", 50), new("CostDetail", "NumeroFactura", 100),
        new("CostDetail", "TipoReferenciaOrigen", 100), new("CostDetail", "ReferenciaOrigen", 200),
        new("CostDetail", "InventarioOrigen", 50), new("CostDetail", "UnidadSolicitanteOrigen", 200),
        new("CostDetail", "CentroCostoOrigen", 100),
        new("Departure", "SalidaCodigo", 200), new("Departure", "Destino", 200),
        new("Departure", "Observaciones", 500),
        new("DepartureItem", "SalidaDetalleCodigo", 200), new("DepartureItem", "DescripcionOriginal", 200),
        new("DepartureItem", "UnidadMedida", 50),
        new("ImportSourceRow", "TipoEntidad", 100), new("ImportSourceRow", "CodigoDestino", 200),
        new("ImportSourceRow", "HojaOrigen", 128), new("ImportSourceRow", "ReglaAplicada", 2000),
        new("ImportSourceRow", "ValorClaveOrigen", 200), new("ImportSourceRow", "FilaOrigenCodigo", 200),
        new("DataQualityIssue", "HojaOrigen", 128), new("DataQualityIssue", "TipoEntidad", 100),
        new("DataQualityIssue", "CodigoRegistro", 200), new("DataQualityIssue", "CodigoProblema", 100),
        new("DataQualityIssue", "NotaResolucion", 2000)
    ];

    private static readonly NumericRule[] NumericRules =
    [
        new("Equipment", "VidaUtilAnios", 0m, 100m, Integer: true, MaxScale: 0, "V2_USEFUL_LIFE_RANGE"),
        new("EquipmentUnit", "ValorAdquisicionBs", 0m, 999999999m, Integer: false, MaxScale: 2, "V2_ACQUISITION_VALUE_RANGE"),
        new("Management", "Anio", 2000m, 2100m, Integer: true, MaxScale: 0, "V2_MANAGEMENT_YEAR_RANGE"),
        new("Management", "Semestre", 0m, 2m, Integer: true, MaxScale: 0, "V2_MANAGEMENT_SEMESTER_RANGE"),
        new("Maintenance", "Satisfaccion", 1m, 5m, Integer: true, MaxScale: 0, "V2_SATISFACTION_RANGE"),
        new("Maintenance", "CostoTotalFuenteBs", 0m, 999999999m, Integer: false, MaxScale: 2, "V2_MAINTENANCE_COST_RANGE"),
        new("Maintenance", "CostoDetalleCalculadoBs", 0m, 999999999m, Integer: false, MaxScale: 2, "V2_MAINTENANCE_COST_RANGE"),
        new("Maintenance", "DiferenciaCostoBs", -999999999m, 999999999m, Integer: false, MaxScale: 2, "V2_MAINTENANCE_COST_RANGE"),
        new("CostDetail", "Cantidad", 0.01m, 99999m, Integer: false, MaxScale: 2, "V2_COST_QUANTITY_RANGE"),
        new("CostDetail", "PrecioUnitarioBs", 0m, 999999999m, Integer: false, MaxScale: 2, "V2_COST_UNIT_PRICE_RANGE"),
        new("CostDetail", "SubtotalBs", 0m, 9999999999999999.99m, Integer: false, MaxScale: 2, "V2_COST_SUBTOTAL_RANGE"),
        new("DepartureItem", "Cantidad", 1m, 9999m, Integer: true, MaxScale: 0, "V2_DEPARTURE_QUANTITY_RANGE"),
        new("ImportSourceRow", "FilaOrigen", 1m, int.MaxValue, Integer: true, MaxScale: 0, "V2_SOURCE_ROW_RANGE"),
        new("DataQualityIssue", "FilaOrigen", 1m, int.MaxValue, Integer: true, MaxScale: 0, "V2_SOURCE_ROW_RANGE")
    ];

    public static void Validate(
        CanonicalImportPackage package,
        IReadOnlyDictionary<string, IReadOnlySet<string>>? controlledLists = null)
    {
        ValidateDuplicates(package);
        ValidateForeignKeys(package);
        ValidateExclusiveParents(package);
        ValidateTypes(package);
        ValidateControlledDomains(package, controlledLists);
        ValidateLengths(package);
        ValidateNumericRanges(package);
        ValidateBooleans(package);
        RegisterClientIssues(package);
    }

    private static void ValidateDuplicates(CanonicalImportPackage package)
    {
        foreach (var entity in package.Entities)
        {
            foreach (var duplicate in entity.Value.GroupBy(row => row.NaturalKey, StringComparer.OrdinalIgnoreCase).Where(group => group.Count() > 1))
            {
                var rows = string.Join(", ", duplicate.Select(row => $"{row.Source.Sheet}:{row.Source.Row}"));
                package.Issues.Add(new CanonicalIssue(DataPriority.P0, "V2_DUPLICATE_KEY",
                    $"La clave '{duplicate.Key}' está duplicada en {entity.Key}: {rows}.",
                    duplicate.First().Source.Sheet, duplicate.First().Source.Row, duplicate.Key,
                    "Resolver el duplicado sin borrar evidencia; conservar el mapa de procedencia."));
            }
        }
    }

    private static void ValidateForeignKeys(CanonicalImportPackage package)
    {
        ForeignKey(package, "Career", "FacultadCodigo", "Faculty", required: true);
        ForeignKey(package, "Laboratory", "FacultadCodigo", "Faculty", required: true);
        ForeignKey(package, "EquipmentUnit", "CatalogoCodigo", "Equipment", required: true);
        ForeignKey(package, "EquipmentUnit", "LaboratorioCodigo", "Laboratory", required: false);
        ForeignKey(package, "EquipmentUnit", "CarreraResponsableCodigo", "Career", required: false);
        ForeignKey(package, "ActorRole", "ActorCodigo", "Actor", required: true);
        ForeignKey(package, "Management", "FacultadCodigo", "Faculty", required: false);
        ForeignKey(package, "Verification", "NumeroInventario", "EquipmentUnit", required: true);
        ForeignKey(package, "Verification", "GestionCodigo", "Management", required: true);
        ForeignKey(package, "Verification", "ResponsableActorCodigo", "Actor", required: false);
        ForeignKey(package, "Request", "GestionCodigo", "Management", required: true);
        ForeignKey(package, "Request", "SolicitanteActorCodigo", "Actor", required: false);
        ForeignKey(package, "RequestEquipmentUnit", "SolicitudCodigo", "Request", required: true);
        ForeignKey(package, "RequestEquipmentUnit", "NumeroInventario", "EquipmentUnit", required: true);
        ForeignKey(package, "Maintenance", "NumeroInventario", "EquipmentUnit", required: true);
        ForeignKey(package, "Maintenance", "GestionCodigo", "Management", required: true);
        ForeignKey(package, "MaintenanceRequest", "MantenimientoCodigo", "Maintenance", required: true);
        ForeignKey(package, "MaintenanceRequest", "SolicitudCodigo", "Request", required: true);
        ForeignKey(package, "MaintenanceParticipant", "MantenimientoCodigo", "Maintenance", required: true);
        ForeignKey(package, "MaintenanceParticipant", "ActorCodigo", "Actor", required: true);
        ForeignKey(package, "CostDetail", "SolicitudCodigo", "Request", required: false);
        ForeignKey(package, "CostDetail", "MantenimientoCodigo", "Maintenance", required: false);
        ForeignKey(package, "CostDetail", "ProveedorActorCodigo", "Actor", required: false);
        ForeignKey(package, "CostDetail", "ResponsableActorCodigo", "Actor", required: false);
        ForeignKey(package, "Departure", "GestionCodigo", "Management", required: true);
        ForeignKey(package, "Departure", "LaboratorioOrigenCodigo", "Laboratory", required: false);
        ForeignKey(package, "Departure", "ResponsableActorCodigo", "Actor", required: false);
        ForeignKey(package, "DepartureItem", "SalidaCodigo", "Departure", required: true);
        ForeignKey(package, "DepartureItem", "NumeroInventario", "EquipmentUnit", required: false);
        ForeignKey(package, "DepartureItem", "ArticuloCodigo", "Article", required: false);
        ForeignKey(package, "MaintenancePlan", "GestionCodigo", "Management", required: true);
        ForeignKey(package, "MaintenancePlan", "NumeroInventario", "EquipmentUnit", required: true);
        ForeignKey(package, "MaintenancePlan", "ResponsableActorCodigo", "Actor", required: false);

        foreach (var unit in package.Get("EquipmentUnit"))
        {
            var lab = Value(unit, "LaboratorioCodigo");
            var locationStatus = Value(unit, "EstadoUbicacion");
            if (string.IsNullOrWhiteSpace(lab) && !locationStatus.Equals("Pendiente", StringComparison.OrdinalIgnoreCase))
            {
                package.Issues.Add(new CanonicalIssue(DataPriority.P0, "V2_LOCATION_INCONSISTENT",
                    "Una unidad sin laboratorio debe tener EstadoUbicacion=Pendiente.", unit.Source.Sheet, unit.Source.Row,
                    unit.NaturalKey, "No deducir el laboratorio; marcar la ubicación como pendiente."));
            }
        }
    }

    private static void ForeignKey(CanonicalImportPackage package, string entity, string field, string targetEntity, bool required)
    {
        var valid = package.Keys(targetEntity);
        foreach (var row in package.Get(entity))
        {
            var value = Value(row, field);
            if (string.IsNullOrWhiteSpace(value))
            {
                // Los campos obligatorios ya se registran una sola vez al leer la hoja.
                continue;
            }

            if (!valid.Contains(value))
            {
                package.Issues.Add(new CanonicalIssue(DataPriority.P0, "V2_FK_NOT_FOUND",
                    $"{entity}.{field}='{value}' no existe en {targetEntity}.", row.Source.Sheet, row.Source.Row,
                    row.NaturalKey, "Corregir la clave o registrar el caso como pendiente; no crear maestros por inferencia."));
            }
        }
    }

    private static void ValidateExclusiveParents(CanonicalImportPackage package)
    {
        foreach (var cost in package.Get("CostDetail"))
        {
            ExactlyOne(package, cost, "SolicitudCodigo", "MantenimientoCodigo", "V2_COST_PARENT_EXCLUSIVE");
        }

        foreach (var detail in package.Get("DepartureItem"))
        {
            ExactlyOne(package, detail, "NumeroInventario", "ArticuloCodigo", "V2_DEPARTURE_ITEM_EXCLUSIVE");
        }
    }

    private static void ExactlyOne(CanonicalImportPackage package, CanonicalRow row, string first, string second, string code)
    {
        var count = (string.IsNullOrWhiteSpace(Value(row, first)) ? 0 : 1)
            + (string.IsNullOrWhiteSpace(Value(row, second)) ? 0 : 1);
        if (count != 1)
        {
            package.Issues.Add(new CanonicalIssue(DataPriority.P0, code,
                $"La fila debe tener exactamente uno de {first} o {second}; actualmente tiene {count}.",
                row.Source.Sheet, row.Source.Row, row.NaturalKey,
                "Resolver la referencia con evidencia. El importador no elegirá ni creará relaciones."));
        }
    }

    private static void ValidateTypes(CanonicalImportPackage package)
    {
        foreach (var row in package.Rows)
        {
            foreach (var field in DateFields)
            {
                var value = Value(row, field);
                if (!string.IsNullOrWhiteSpace(value)
                    && !DateTime.TryParseExact(value, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out _))
                {
                    package.Issues.Add(new CanonicalIssue(DataPriority.P0, "V2_DATE_INVALID",
                        $"{field}='{value}' no es una fecha real yyyy-MM-dd.", row.Source.Sheet, row.Source.Row,
                        row.NaturalKey, "Corregir la fecha; no sustituirla por la fecha actual."));
                }
            }
        }
    }

    private static void ValidateControlledDomains(
        CanonicalImportPackage package,
        IReadOnlyDictionary<string, IReadOnlySet<string>>? controlledLists)
    {
        // Read() already records a single explicit blocker when the contractual sheet is absent.
        if (controlledLists is null)
        {
            return;
        }

        foreach (var domain in RequiredControlledDomains)
        {
            if (!controlledLists.TryGetValue(domain, out var values))
            {
                package.Issues.Add(new CanonicalIssue(DataPriority.P0, "V2_CONTROL_DOMAIN_MISSING",
                    $"La hoja 90_LISTAS_CONTROLADAS no contiene el dominio obligatorio '{domain}'.",
                    "90_LISTAS_CONTROLADAS", 1, SuggestedAction: "Restaurar la columna y sus valores oficiales; no codificar equivalencias en el importador."));
            }
            else if (values.Count == 0)
            {
                package.Issues.Add(new CanonicalIssue(DataPriority.P0, "V2_CONTROL_DOMAIN_EMPTY",
                    $"El dominio obligatorio '{domain}' no contiene valores permitidos.",
                    "90_LISTAS_CONTROLADAS", 1, SuggestedAction: "Completar la lista controlada antes de preparar staging SQL."));
            }
        }

        foreach (var rule in DomainRules)
        {
            if (!controlledLists.TryGetValue(rule.Domain, out var allowed) || allowed.Count == 0)
            {
                // A single contract issue was already emitted for the missing/empty domain.
                continue;
            }

            foreach (var row in package.Get(rule.Entity))
            {
                var value = Value(row, rule.Field);
                if (string.IsNullOrWhiteSpace(value))
                {
                    continue;
                }

                if (!allowed.Contains(value.Trim()))
                {
                    package.Issues.Add(new CanonicalIssue(DataPriority.P0, "V2_CONTROL_VALUE_INVALID",
                        $"{rule.Entity}.{rule.Field}='{value}' no pertenece al dominio controlado '{rule.Domain}'.",
                        row.Source.Sheet, row.Source.Row, row.NaturalKey,
                        $"Elegir un valor existente en 90_LISTAS_CONTROLADAS/{rule.Domain}; si el dominio institucional cambió, actualizar primero la plantilla y el diccionario."));
                }
            }
        }
    }

    private static void ValidateLengths(CanonicalImportPackage package)
    {
        foreach (var row in package.Rows)
        {
            if (row.NaturalKey.Length > 200)
            {
                package.Issues.Add(new CanonicalIssue(DataPriority.P0, "V2_KEY_MAX_LENGTH_EXCEEDED",
                    $"La clave natural tiene {row.NaturalKey.Length} caracteres y el staging/EF admite como máximo 200.",
                    row.Source.Sheet, row.Source.Row, row.NaturalKey,
                    "Asignar un código estable de hasta 200 caracteres sin truncarlo automáticamente."));
            }
        }

        foreach (var rule in LengthRules)
        {
            foreach (var row in package.Get(rule.Entity))
            {
                var value = Value(row, rule.Field);
                if (!string.IsNullOrEmpty(value) && value.Length > rule.Maximum)
                {
                    package.Issues.Add(new CanonicalIssue(DataPriority.P0, "V2_MAX_LENGTH_EXCEEDED",
                        $"{rule.Entity}.{rule.Field} tiene {value.Length} caracteres; el máximo EF es {rule.Maximum}.",
                        row.Source.Sheet, row.Source.Row, row.NaturalKey,
                        "Corregir el dato fuente o ampliar explícitamente el esquema; el importador no truncará información."));
                }
            }
        }

        foreach (var unit in package.Get("EquipmentUnit"))
        {
            var inventory = Value(unit, "NumeroInventario");
            if (!string.IsNullOrWhiteSpace(inventory) && inventory.Length < 3)
            {
                package.Issues.Add(new CanonicalIssue(DataPriority.P0, "V2_INVENTORY_MIN_LENGTH",
                    $"NumeroInventario='{inventory}' tiene menos de 3 caracteres y no cumple el contrato EF.",
                    unit.Source.Sheet, unit.Source.Row, unit.NaturalKey,
                    "Confirmar el identificador patrimonial completo; no rellenar con ceros sin evidencia."));
            }
        }
    }

    private static void ValidateNumericRanges(CanonicalImportPackage package)
    {
        foreach (var rule in NumericRules)
        {
            foreach (var row in package.Get(rule.Entity))
            {
                var raw = Value(row, rule.Field);
                if (string.IsNullOrWhiteSpace(raw))
                {
                    continue;
                }

                if (!decimal.TryParse(raw, NumberStyles.Float, CultureInfo.InvariantCulture, out var number))
                {
                    package.Issues.Add(new CanonicalIssue(DataPriority.P0, "V2_NUMBER_INVALID",
                        $"{rule.Entity}.{rule.Field}='{raw}' no es un número contractual válido.",
                        row.Source.Sheet, row.Source.Row, row.NaturalKey,
                        "Ingresar un valor numérico real; dejar vacío únicamente cuando el campo sea nullable y desconocido."));
                    continue;
                }

                if (rule.Integer && decimal.Truncate(number) != number)
                {
                    package.Issues.Add(new CanonicalIssue(DataPriority.P0, "V2_INTEGER_REQUIRED",
                        $"{rule.Entity}.{rule.Field}='{raw}' debe ser un número entero.",
                        row.Source.Sheet, row.Source.Row, row.NaturalKey,
                        "Corregir el valor sin redondeo automático."));
                    continue;
                }

                if (number < rule.Minimum || number > rule.Maximum)
                {
                    package.Issues.Add(new CanonicalIssue(DataPriority.P0, rule.RangeCode,
                        $"{rule.Entity}.{rule.Field}='{raw}' está fuera del rango EF [{rule.Minimum}, {rule.Maximum}].",
                        row.Source.Sheet, row.Source.Row, row.NaturalKey,
                        "Confirmar el dato con evidencia; el importador no recortará ni sustituirá valores."));
                    continue;
                }

                if (decimal.Round(number, rule.MaxScale, MidpointRounding.ToEven) != number)
                {
                    package.Issues.Add(new CanonicalIssue(DataPriority.P0, "V2_NUMBER_SCALE_EXCEEDED",
                        $"{rule.Entity}.{rule.Field}='{raw}' tiene más de {rule.MaxScale} decimales admitidos por EF.",
                        row.Source.Sheet, row.Source.Row, row.NaturalKey,
                        "Corregir la precisión en la fuente; no redondear automáticamente durante la importación."));
                }
            }
        }
    }

    private static void ValidateBooleans(CanonicalImportPackage package)
    {
        var accepted = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "Sí", "Si", "No", "true", "false", "1", "0"
        };
        foreach (var participant in package.Get("MaintenanceParticipant"))
        {
            var value = Value(participant, "EsPrincipal");
            if (!string.IsNullOrWhiteSpace(value) && !accepted.Contains(value.Trim()))
            {
                package.Issues.Add(new CanonicalIssue(DataPriority.P0, "V2_BOOLEAN_INVALID",
                    $"MaintenanceParticipant.EsPrincipal='{value}' no representa un booleano válido.",
                    participant.Source.Sheet, participant.Source.Row, participant.NaturalKey,
                    "Usar Sí o No; dejar vacío únicamente si todavía no existe evidencia."));
            }
        }
    }

    private static void RegisterClientIssues(CanonicalImportPackage package)
    {
        foreach (var row in package.Get("DataQualityIssue"))
        {
            var status = Value(row, "EstadoResolucion");
            if (status.Equals("Resuelta", StringComparison.OrdinalIgnoreCase)
                || status.Equals("Cerrada", StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            var rawSeverity = Value(row, "Severidad");
            var priority = rawSeverity.Contains("Crít", StringComparison.OrdinalIgnoreCase)
                || rawSeverity.Contains("Crit", StringComparison.OrdinalIgnoreCase)
                ? DataPriority.P0
                : rawSeverity.Contains("Alta", StringComparison.OrdinalIgnoreCase)
                    || rawSeverity.Contains("Alto", StringComparison.OrdinalIgnoreCase)
                    ? DataPriority.P1
                    : DataPriority.P2;
            package.Issues.Add(new CanonicalIssue(priority, Value(row, "CodigoProblema") is { Length: > 0 } code ? code : "CLIENT_PENDING",
                $"Incidencia pendiente de cliente: {Value(row, "ValorOriginal")}",
                Value(row, "HojaOrigen"), ParseNullableInt(Value(row, "FilaOrigen")), row.NaturalKey,
                "Resolver en la planilla de levantamiento y documentar la evidencia."));
        }
    }

    internal static string Value(CanonicalRow row, string field) =>
        row.Fields.TryGetValue(field, out var value) ? value ?? string.Empty : string.Empty;

    private static int? ParseNullableInt(string value) =>
        int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var parsed) ? parsed : null;

    private sealed record DomainRule(string Entity, string Field, string Domain);
    private sealed record LengthRule(string Entity, string Field, int Maximum);
    private sealed record NumericRule(
        string Entity,
        string Field,
        decimal Minimum,
        decimal Maximum,
        bool Integer,
        int MaxScale,
        string RangeCode);
}

internal static class CanonicalSelfCheck
{
    public static void Run()
    {
        var package = new CanonicalImportPackage
        {
            Contract = ImportContract.V2,
            WorkbookPath = "self-check.xlsx",
            WorkbookSha256 = new string('0', 64),
            Strict = true
        };

        package.Add(Row("Equipment", "CAT-0001", ("CatalogoCodigo", "CAT-0001"),
            ("Categoria", "Categoría inventada"), ("VidaUtilAnios", "101")));
        package.Add(Row("EquipmentUnit", "INV-1", ("NumeroInventario", "INV-1"), ("CatalogoCodigo", "CAT-INEXISTENTE"), ("EstadoUbicacion", "Confirmada")));
        package.Add(Row("Actor", new string('A', 31), ("ActorCodigo", new string('A', 31))));
        package.Add(Row("Maintenance", "MNT-1", ("Satisfaccion", "6")));
        package.Add(Row("MaintenanceParticipant", "PART-1", ("EsPrincipal", "Tal vez")));
        package.Add(Row("CostDetail", "COST-1", ("SolicitudCodigo", "REQ-1"), ("MantenimientoCodigo", "MNT-1"), ("Cantidad", "0")));
        package.Add(Row("DepartureItem", "DET-1", ("NumeroInventario", null), ("ArticuloCodigo", null), ("Cantidad", "1.5")));
        CanonicalPackageValidator.Validate(package, SelfCheckControlledLists());

        var expected = new[]
        {
            "V2_FK_NOT_FOUND", "V2_LOCATION_INCONSISTENT", "V2_COST_PARENT_EXCLUSIVE",
            "V2_DEPARTURE_ITEM_EXCLUSIVE", "V2_CONTROL_VALUE_INVALID", "V2_USEFUL_LIFE_RANGE",
            "V2_MAX_LENGTH_EXCEEDED", "V2_SATISFACTION_RANGE", "V2_BOOLEAN_INVALID",
            "V2_INTEGER_REQUIRED", "V2_COST_QUANTITY_RANGE"
        };
        var missing = expected.Where(code => package.Issues.All(issue => issue.Code != code)).ToArray();
        if (missing.Length > 0)
        {
            throw new InvalidOperationException($"Self-check falló. No se detectaron: {string.Join(", ", missing)}");
        }

        var nullable = new CanonicalImportPackage
        {
            Contract = ImportContract.V2,
            WorkbookPath = "self-check-nullable.xlsx",
            WorkbookSha256 = new string('B', 64),
            Strict = true
        };
        nullable.Add(Row("Maintenance", "MNT-NULLABLE", ("Satisfaccion", null)));
        CanonicalPackageValidator.Validate(nullable, SelfCheckControlledLists());
        if (nullable.Issues.Any(issue => issue.Message.Contains("Satisfaccion", StringComparison.OrdinalIgnoreCase)))
        {
            throw new InvalidOperationException("Self-check falló: Satisfaccion vacía y nullable fue clasificada como error.");
        }

        var normalizedSuggestion = new CanonicalImportPackage
        {
            Contract = ImportContract.V2,
            WorkbookPath = "self-check-suggestion.xlsx",
            WorkbookSha256 = new string('D', 64),
            Strict = true
        };
        normalizedSuggestion.Add(Row("Request", "REQ-SUGGESTION", ("Sugerencia", new string('S', 1000))));
        normalizedSuggestion.Add(Row("Request", "REQ-SUGGESTION-TOO-LONG", ("Sugerencia", new string('S', 2001))));
        CanonicalPackageValidator.Validate(normalizedSuggestion, SelfCheckControlledLists());
        if (normalizedSuggestion.Issues.Any(issue => issue.Code == "V2_MAX_LENGTH_EXCEEDED" && issue.NaturalKey == "REQ-SUGGESTION"))
        {
            throw new InvalidOperationException("Self-check falló: Request.Sugerencia válida para el campo normalizado de 2000 caracteres fue rechazada.");
        }
        if (normalizedSuggestion.Issues.All(issue => issue.Code != "V2_MAX_LENGTH_EXCEEDED" || issue.NaturalKey != "REQ-SUGGESTION-TOO-LONG"))
        {
            throw new InvalidOperationException("Self-check falló: Request.Sugerencia superior a 2000 caracteres no fue bloqueada.");
        }

        var incompleteLists = SelfCheckControlledLists()
            .Where(pair => !pair.Key.Equals("Prioridad", StringComparison.OrdinalIgnoreCase))
            .ToDictionary(pair => pair.Key, pair => pair.Value, StringComparer.OrdinalIgnoreCase);
        var missingDomain = new CanonicalImportPackage
        {
            Contract = ImportContract.V2,
            WorkbookPath = "self-check-domain.xlsx",
            WorkbookSha256 = new string('C', 64),
            Strict = true
        };
        missingDomain.Add(Row("Request", "REQ-DOMAIN", ("Prioridad", "Alta")));
        CanonicalPackageValidator.Validate(missingDomain, incompleteLists);
        if (missingDomain.Issues.All(issue => issue.Code != "V2_CONTROL_DOMAIN_MISSING"))
        {
            throw new InvalidOperationException("Self-check falló: no se bloqueó un dominio obligatorio ausente de 90_LISTAS_CONTROLADAS.");
        }

        var clean = new CanonicalImportPackage
        {
            Contract = ImportContract.V2,
            WorkbookPath = "self-check.xlsx",
            WorkbookSha256 = new string('A', 64),
            Strict = true
        };
        clean.Add(Row("Equipment", "CAT-0001", ("CatalogoCodigo", "CAT-0001"), ("NombreGenerico", "Equipo de prueba")));
        var cleanRow = clean.Rows.Single();
        var sql = NormalizedSqlBuilder.Build(clean,
        [
            new ReconciliationRow("Equipment", "CAT-0001", ReconciliationStatus.SoloExcel,
                cleanRow.Fingerprint(), null, "Self-check")
        ]);
        if (!sql.Contains("ImportSourceRows", StringComparison.Ordinal)
            || !sql.Contains("IF NOT EXISTS", StringComparison.Ordinal)
            || sql.Contains(" DELETE ", StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException("Self-check falló: el SQL V2 no conserva sus guardas idempotentes/no destructivas.");
        }

        Console.WriteLine($"Self-check OK: {package.Issues.Count} incidencias detectadas, {expected.Length} reglas críticas, sugerencia normalizada, dominio ausente, nullable legítimo y SQL idempotente/no destructivo cubiertos.");
    }

    private static IReadOnlyDictionary<string, IReadOnlySet<string>> SelfCheckControlledLists()
    {
        static IReadOnlySet<string> Values(params string[] values) =>
            new HashSet<string>(values, StringComparer.OrdinalIgnoreCase);

        return new Dictionary<string, IReadOnlySet<string>>(StringComparer.OrdinalIgnoreCase)
        {
            ["EstadoGeneral"] = Values("Activo", "Por confirmar"),
            ["EstadoUbicacion"] = Values("Confirmada", "Pendiente"),
            ["EstadoOperativo"] = Values("Operativo", "Por confirmar"),
            ["CondicionFisica"] = Values("Excelente", "Por confirmar"),
            ["Prioridad"] = Values("Alta", "Por confirmar"),
            ["TipoMantenimiento"] = Values("Preventivo", "Por confirmar"),
            ["TipoServicio"] = Values("Interno", "Por confirmar"),
            ["RolActor"] = Values("Técnico"),
            ["TipoActor"] = Values("Persona"),
            ["Vinculacion"] = Values("Interno", "Por confirmar"),
            ["UnidadMedida"] = Values("unidad"),
            ["CategoriaCosto"] = Values("Repuesto/Material"),
            ["ModalidadSalida"] = Values("Préstamo Interno"),
            ["EstadoCalidad"] = Values("Válida", "Por confirmar"),
            ["EstadoResolucion"] = Values("Abierta"),
            ["EstadoPlan"] = Values("Pendiente", "Por confirmar"),
            ["TipoGestion"] = Values("Preventiva", "Por confirmar"),
            ["EstadoGestion"] = Values("Abierta", "Por confirmar"),
            ["ResultadoVerificacion"] = Values("Operativo", "Por confirmar"),
            ["CategoriaEquipo"] = Values("Equipo", "Por confirmar"),
            ["TipoUtensilio"] = Values("No aplica", "Por confirmar"),
            ["ClasificacionEquipo"] = Values("Otro", "Por confirmar")
        };
    }

    private static CanonicalRow Row(string entity, string key, params (string Key, string? Value)[] fields) =>
        new(entity, key, fields.ToDictionary(item => item.Key, item => item.Value), new SourceReference("SELF_CHECK", 2, "SELF_CHECK:2"));
}
