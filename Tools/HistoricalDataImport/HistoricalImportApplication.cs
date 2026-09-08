using System.Text;
using OfficeOpenXml;

internal static class HistoricalImportApplication
{
    public static int Run(string[] args)
    {
        ImportOptions options;
        try
        {
            options = ImportOptions.Parse(args);
        }
        catch (ArgumentException exception)
        {
            Console.Error.WriteLine($"Error de argumentos: {exception.Message}");
            PrintHelp();
            return 2;
        }

        if (options.ShowHelp)
        {
            PrintHelp();
            return 0;
        }

        if (options.SelfCheck)
        {
            try
            {
                CanonicalSelfCheck.Run();
                return 0;
            }
            catch (Exception exception)
            {
                Console.Error.WriteLine(exception.Message);
                return 1;
            }
        }

        if (!File.Exists(options.WorkbookPath))
        {
            Console.Error.WriteLine($"No existe el Excel: {options.WorkbookPath}");
            return 2;
        }

        ImportContract contract;
        try
        {
            contract = ResolveContract(options);
        }
        catch (InvalidDataException exception)
        {
            Console.Error.WriteLine(exception.Message);
            return 2;
        }

        var mode = options.RequestedMode ?? (contract == ImportContract.V2 ? ImportMode.Validate : ImportMode.Sql);
        if (options.SeedMode && contract != ImportContract.Legacy)
        {
            Console.Error.WriteLine("--seed pertenece únicamente al contrato legacy. V2 nunca inventa cadenas preventivas/correctivas.");
            return 2;
        }

        Directory.CreateDirectory(options.OutputDirectory);
        return contract == ImportContract.V2
            ? RunV2(options, mode)
            : RunLegacy(options, mode);
    }

    private static int RunV2(ImportOptions options, ImportMode mode)
    {
        CanonicalImportPackage package;
        try
        {
            package = new V2WorkbookReader().Read(options.WorkbookPath);
        }
        catch (InvalidDataException exception)
        {
            Console.Error.WriteLine($"El archivo no puede leerse como V2: {exception.Message}");
            return 2;
        }

        var artifacts = new CanonicalArtifactWriter(options.OutputDirectory, package, options.SnapshotPath);
        var reconciliation = artifacts.WriteAll();
        var p0 = package.Issues.Count(issue => issue.Priority == DataPriority.P0);

        if (mode == ImportMode.Sql)
        {
            var reasons = new List<string>();
            if (string.IsNullOrWhiteSpace(options.SnapshotPath))
            {
                reasons.Add("Falta `--snapshot <archivo.csv>` con Entity, NaturalKey y Fingerprint de la base objetivo.");
            }
            else if (!File.Exists(options.SnapshotPath))
            {
                reasons.Add($"El snapshot indicado no existe: {options.SnapshotPath}");
            }

            if (!options.AllowSql)
            {
                reasons.Add("Falta la autorización explícita `--allow-sql`.");
            }

            if (p0 > 0)
            {
                reasons.Add($"Existen {p0} incidencias P0 bloqueantes.");
            }

            if (reasons.Count > 0)
            {
                artifacts.WriteSqlBlocked(reasons.ToArray());
                PrintResult(package, options, mode, sqlGenerated: false);
                return 3;
            }

            var sql = NormalizedSqlBuilder.Build(package, reconciliation);
            File.WriteAllText(Path.Combine(options.OutputDirectory, "stage-v2-delta.sql"), sql, Encoding.UTF8);
            PrintResult(package, options, mode, sqlGenerated: true);
            return 0;
        }

        PrintResult(package, options, mode, sqlGenerated: false);
        return p0 == 0 ? 0 : 1;
    }

    private static int RunLegacy(ImportOptions options, ImportMode mode)
    {
        var result = new HistoricalDataAnalyzer(options.WorkbookPath).Analyze();
        var package = LegacyCanonicalAdapter.Convert(result, options.WorkbookPath);
        var artifacts = new CanonicalArtifactWriter(options.OutputDirectory, package, options.SnapshotPath);
        artifacts.WriteAll();

        if (options.SeedMode)
        {
            var seed = new SeedDataBuilder(result).Build();
            new SeedOutputWriter(options.OutputDirectory, seed).WriteAll();
            Console.WriteLine("Contrato legacy procesado en modo seed (compatibilidad conservada).");
            Console.WriteLine($"Salida: {options.OutputDirectory}");
            Console.WriteLine($"Resumen común: {Path.Combine(options.OutputDirectory, "resumen.md")}");
            return seed.Rejects.Count == 0 ? 0 : 1;
        }

        if (mode != ImportMode.Validate)
        {
            new OutputWriter(options.OutputDirectory, result).WriteAll(includeSql: mode == ImportMode.Sql);
        }

        PrintResult(package, options, mode, sqlGenerated: mode == ImportMode.Sql);
        return package.Issues.Any(issue => issue.Priority == DataPriority.P0) ? 1 : 0;
    }

    private static ImportContract ResolveContract(ImportOptions options)
    {
        if (options.Contract != ImportContract.Auto)
        {
            return options.Contract;
        }

        try
        {
            var completeV2 = V2WorkbookReader.HasV2Sentinels(options.WorkbookPath, out var present);
            if (completeV2)
            {
                return ImportContract.V2;
            }

            if (present.Count > 0)
            {
                Console.Error.WriteLine($"Advertencia: V2 incompleta; solo se hallaron {present.Count}/{V2WorkbookReader.SentinelSheets.Length} hojas identificadoras. Se validará como V2 para informar lo faltante.");
                return ImportContract.V2;
            }

            return ImportContract.Legacy;
        }
        catch (Exception exception) when (exception is InvalidDataException or IOException or UnauthorizedAccessException)
        {
            throw new InvalidDataException($"No se pudo detectar el contrato del Excel: {exception.Message}", exception);
        }
    }

    private static void PrintResult(CanonicalImportPackage package, ImportOptions options, ImportMode mode, bool sqlGenerated)
    {
        Console.WriteLine("Importación histórica analizada.");
        Console.WriteLine($"Contrato: {package.Contract}");
        Console.WriteLine($"Modo:     {mode}");
        Console.WriteLine($"Excel:    {options.WorkbookPath}");
        Console.WriteLine($"Salida:   {options.OutputDirectory}");
        Console.WriteLine($"Filas:    {package.Rows.Count()}");
        Console.WriteLine($"P0/P1/P2: {package.Issues.Count(issue => issue.Priority == DataPriority.P0)}/{package.Issues.Count(issue => issue.Priority == DataPriority.P1)}/{package.Issues.Count(issue => issue.Priority == DataPriority.P2)}");
        Console.WriteLine($"Resumen:  {Path.Combine(options.OutputDirectory, "resumen.md")}");
        if (sqlGenerated)
        {
            Console.WriteLine($"SQL:      {Path.Combine(options.OutputDirectory, package.Contract == ImportContract.V2 ? "stage-v2-delta.sql" : "load.sql")}");
        }
    }

    private static void PrintHelp()
    {
        Console.WriteLine(
            """
            Importador histórico compatible con Excel legacy y contrato V2 normalizado.

            Uso:
              dotnet run --project Tools/HistoricalDataImport -- "archivo.xlsx" [directorio-salida] [opciones]

            Opciones:
              --contract auto|legacy|v2   Autodetección por defecto; V2 usa sus cinco hojas identificadoras.
              --mode validate|audit|sql   validate nunca genera SQL; V2 usa validate por defecto.
              --validate-only             Alias de --mode validate.
              --snapshot archivo.csv      Snapshot con Entity,NaturalKey,Fingerprint,SourceKind(opcional).
              --allow-sql                 Segunda llave requerida para producir SQL V2 offline.
              --seed                      Flujo seed histórico, válido solo para legacy.
              --self-check                Ejecuta reglas críticas reproducibles sin leer ni escribir base de datos.
              --help                      Muestra esta ayuda.

            Seguridad V2:
              --mode sql exige snapshot válido, --allow-sql y cero incidencias P0. El script resultante
              registra un delta idempotente en ImportBatches/ImportSourceRows/DataQualityIssues; no se conecta
              a SQL Server, no borra registros y deja los datos de negocio Pending para el adaptador transaccional.
            """);
    }
}
