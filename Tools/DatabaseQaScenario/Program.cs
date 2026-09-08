using Proyecto_Laboratorios_Univalle.Tools.DatabaseQaScenario;

const string sourceDatabase = "DB_Laboratorios_Univalle";
const string targetDatabase = "DB_Laboratorios_Univalle_SCENARIOS_QA";

QaCommand options;
try
{
    options = QaCommand.Parse(args);
}
catch (ArgumentException exception)
{
    Console.Error.WriteLine(exception.Message);
    QaCommand.PrintHelp();
    return 2;
}

if (options.Mode == QaMode.Help)
{
    QaCommand.PrintHelp();
    return 0;
}

try
{
    var memory = MemoryGuard.EnsureAvailable(options.Profile == QaProfile.Stress);
    Console.WriteLine($"RAM aprobada: {memory.AvailableGb:F2} GB libres, {memory.UsedPercent:F1}% en uso.");

    var paths = QaPaths.Create(options.OutputDirectory);
    var service = new QaScenarioService(sourceDatabase, targetDatabase, paths);

    switch (options.Mode)
    {
        case QaMode.Plan:
            await service.PlanAsync(options.Profile);
            break;
        case QaMode.Create:
            await service.CreateAsync(options.Profile);
            break;
        case QaMode.Seed:
            await service.SeedAsync(options.Profile);
            break;
        case QaMode.Verify:
            await service.VerifyAsync(options.Profile);
            break;
        case QaMode.Benchmark:
            await service.BenchmarkAsync(options.Profile);
            break;
        case QaMode.Backup:
            await service.BackupAsync(options.Profile);
            break;
        case QaMode.Credentials:
            await service.CopyCredentialsAsync();
            break;
        case QaMode.All:
            await service.RunAllAsync(options.Profile);
            break;
        default:
            throw new ArgumentOutOfRangeException();
    }

    return 0;
}
catch (Exception exception)
{
    Console.Error.WriteLine($"QA-DATA-001 FALLÓ: {exception.GetType().Name}: {exception.Message}");
    return 1;
}
