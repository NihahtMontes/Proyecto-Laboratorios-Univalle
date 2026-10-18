using System.Globalization;
using System.Text;
using System.Text.Json;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Proyecto_Laboratorios_Univalle.Data;
using Proyecto_Laboratorios_Univalle.Helpers;
using Proyecto_Laboratorios_Univalle.Models.Enums;
using Proyecto_Laboratorios_Univalle.Services;

// Clasifica el catálogo de equipos por análisis de datos (nombre, serie física, pista del Excel y artículo
// de igual nombre). Por defecto SOLO SIMULA y escribe un CSV; con --apply registra cada decisión mediante
// IEquipmentClassificationDecisionService (historial en EquipmentClassificationDecisions).
const string KeyPrefix = "CLS-INFER-";
const string KeyVersion = "v2"; // v2 (2026-10-15): balanzas pasan de Medicion a Electronico
var arguments = ParseArguments(args);
if (arguments.ContainsKey("help"))
{
    PrintUsage();
    return 0;
}

var configuration = new ConfigurationBuilder()
    .SetBasePath(Directory.GetCurrentDirectory())
    .AddJsonFile("appsettings.json", optional: true)
    .AddUserSecrets<ClassifyMarker>(optional: true)
    .AddEnvironmentVariables()
    .Build();

var connectionString = configuration.GetConnectionString("DefaultConnection");
if (string.IsNullOrWhiteSpace(connectionString))
{
    Console.Error.WriteLine("No existe ConnectionStrings:DefaultConnection en User Secrets o variables de entorno.");
    return 2;
}

// Destino declarado obligatorio (también en simulación): el servidor y la base deben coincidir exactamente.
var connection = new SqlConnectionStringBuilder(connectionString);
arguments.TryGetValue("expected-server", out var expectedServer);
arguments.TryGetValue("expected-database", out var expectedDatabase);
if (string.IsNullOrWhiteSpace(expectedServer) || string.IsNullOrWhiteSpace(expectedDatabase) ||
    new[] { "master", "model", "msdb", "tempdb" }.Contains(expectedDatabase, StringComparer.OrdinalIgnoreCase) ||
    !string.Equals(connection.DataSource, expectedServer.Trim(), StringComparison.OrdinalIgnoreCase) ||
    !string.Equals(connection.InitialCatalog, expectedDatabase.Trim(), StringComparison.OrdinalIgnoreCase))
{
    Console.Error.WriteLine("SEGURIDAD: indique --expected-server y --expected-database; deben coincidir exactamente con DefaultConnection.");
    return 3;
}

var apply = arguments.ContainsKey("apply");
var outputDirectory = arguments.TryGetValue("out", out var outValue) && !string.IsNullOrWhiteSpace(outValue)
    ? outValue
    : Path.Combine(Directory.GetCurrentDirectory(), "outputs");
var userName = arguments.TryGetValue("user", out var userValue) && !string.IsNullOrWhiteSpace(userValue)
    ? userValue.Trim()
    : "nihaht";
var responsible = arguments.TryGetValue("responsible", out var responsibleValue) && !string.IsNullOrWhiteSpace(responsibleValue)
    ? responsibleValue.Trim()
    : "Nihaht Montes";

var services = new ServiceCollection();
services.AddLogging();
services.AddSingleton<IConfiguration>(configuration);
services.AddSingleton<ICurrentUserService, ClassifyCurrentUserService>();
services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(connectionString)
        .UseQueryTrackingBehavior(QueryTrackingBehavior.NoTracking));
services.AddScoped<IEquipmentClassificationDecisionService, EquipmentClassificationDecisionService>();

await using var provider = services.BuildServiceProvider();
await using var scope = provider.CreateAsyncScope();
var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
if ((await db.Database.GetPendingMigrationsAsync()).Any())
{
    Console.Error.WriteLine("La base tiene migraciones pendientes. No se clasificará nada.");
    return 4;
}

// ---- Lectura (solo lectura) ----
var equipments = await db.Equipments.IgnoreQueryFilters().AsNoTracking()
    .Where(e => e.CatalogCode != null)
    .OrderBy(e => e.Id)
    .Select(e => new
    {
        e.Id,
        CatalogCode = e.CatalogCode!,
        e.Name,
        e.Status,
        e.ClassificationReviewStatus,
        e.Category,
        e.TypeClassification,
        e.UtensilType,
        e.OtherClassificationDetail
    })
    .ToListAsync();

var unitStats = (await db.EquipmentUnits.IgnoreQueryFilters().AsNoTracking()
        .GroupBy(u => u.EquipmentId)
        .Select(g => new
        {
            EquipmentId = g.Key,
            Units = g.Count(),
            WithSerial = g.Count(u => u.SerialNumber != null && u.SerialNumber != "")
        })
        .ToListAsync())
    .ToDictionary(x => x.EquipmentId);

var hintRows = await db.ImportSourceRows.AsNoTracking()
    .Where(r => r.SourceSheet == "CatalogoEquipos" && r.TargetEntityName == "Equipment")
    .OrderBy(r => r.Id)
    .Select(r => new { r.TargetEntityKey, r.OriginalDataJson })
    .ToListAsync();
var sourceHints = new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase);
foreach (var row in hintRows)
{
    if (string.IsNullOrWhiteSpace(row.TargetEntityKey) || string.IsNullOrWhiteSpace(row.OriginalDataJson))
    {
        continue;
    }

    using var document = JsonDocument.Parse(row.OriginalDataJson);
    string? raw = null;
    if (document.RootElement.ValueKind == JsonValueKind.Object
        && document.RootElement.TryGetProperty("Tipoutensilio", out var element)
        && element.ValueKind == JsonValueKind.String)
    {
        raw = element.GetString();
    }

    var normalized = Normalize(raw);
    // "No aplica" y "Por confirmar" no aportan pista de clasificación.
    sourceHints[row.TargetEntityKey] = normalized is "" or "no aplica" or "por confirmar" ? null : raw!.Trim();
}

var articleCategories = (await db.Articles.IgnoreQueryFilters().AsNoTracking()
        .Select(a => new { a.Name, Category = a.ArticleCategory != null ? a.ArticleCategory.Name : null })
        .ToListAsync())
    .Where(a => !string.IsNullOrWhiteSpace(a.Category))
    .GroupBy(a => Normalize(a.Name))
    .ToDictionary(g => g.Key, g => g.Select(a => a.Category!).Distinct(StringComparer.OrdinalIgnoreCase).ToList());

// ---- Inferencia ----
var results = new List<CatalogRow>();
foreach (var e in equipments)
{
    unitStats.TryGetValue(e.Id, out var stats);
    sourceHints.TryGetValue(e.CatalogCode, out var hint);
    string? linkedCategory = null;
    if (articleCategories.TryGetValue(Normalize(e.Name), out var categories) && categories.Count == 1)
    {
        linkedCategory = categories[0];
    }

    var proposal = CatalogClassificationInference.Infer(new CatalogClassificationInput(
        e.CatalogCode, e.Name, (stats?.WithSerial ?? 0) > 0, hint, linkedCategory));
    results.Add(new CatalogRow(e.Id, e.CatalogCode, e.Name, stats?.Units ?? 0, stats?.WithSerial ?? 0,
        e.ClassificationReviewStatus, e.Status, proposal,
        e.Category, e.TypeClassification, e.UtensilType, e.OtherClassificationDetail));
}

var invalid = results.Where(r => !EquipmentClassificationRules.IsConfirmedCombinationValid(
    r.Proposal.Category, r.Proposal.TypeClassification, r.Proposal.UtensilType, r.Proposal.OtherDetail)).ToList();
if (invalid.Count > 0)
{
    Console.Error.WriteLine($"ERROR: {invalid.Count} propuestas con jerarquía inválida: {string.Join(", ", invalid.Select(r => r.Code))}");
    return 5;
}

Directory.CreateDirectory(outputDirectory);
var csvAll = Path.Combine(outputDirectory, "clasificacion_catalogo.csv");
var csvReview = Path.Combine(outputDirectory, "clasificacion_catalogo_revision.csv");
WriteCsv(csvAll, results);
WriteCsv(csvReview, results.Where(r => r.Proposal.Confidence != InferenceConfidence.High));

Console.WriteLine($"Catálogos: {results.Count} | Equipo: {results.Count(r => r.Proposal.Category == EquipmentCategory.Equipment)}" +
                  $" | Utensilio: {results.Count(r => r.Proposal.Category == EquipmentCategory.Utensil)}" +
                  $" | Otro: {results.Count(r => r.Proposal.Category == EquipmentCategory.Other)}");
Console.WriteLine($"Confianza alta: {results.Count(r => r.Proposal.Confidence == InferenceConfidence.High)}" +
                  $" | media: {results.Count(r => r.Proposal.Confidence == InferenceConfidence.Medium)}" +
                  $" | a revisar: {results.Count(r => r.Proposal.Confidence == InferenceConfidence.Review)}");
var willChange = results.Where(r => r.Proposal.Rule != "NAME_UNMATCHED"
    && (r.CurrentStatus != EquipmentClassificationReviewStatus.Confirmed
        || r.CurrentCategory != r.Proposal.Category
        || r.CurrentType != r.Proposal.TypeClassification
        || r.CurrentUtensil != r.Proposal.UtensilType
        || !string.Equals(r.CurrentDetail, r.Proposal.OtherDetail, StringComparison.Ordinal))).ToList();
Console.WriteLine($"Cambian respecto al estado actual de la base: {willChange.Count}" +
                  (willChange.Count is > 0 and <= 15 ? " (" + string.Join(", ", willChange.Select(r => r.Code)) + ")" : string.Empty));
Console.WriteLine($"CSV completo: {csvAll}");
Console.WriteLine($"CSV a revisar: {csvReview}");

if (!apply)
{
    Console.WriteLine("SIMULACIÓN: no se escribió nada en la base. Use --apply para registrar las decisiones.");
    return 0;
}

// ---- Aplicación (escritura en la base verificada por --expected-*) ----
var recordedBy = await db.Users.IgnoreQueryFilters().AsNoTracking()
    .Where(u => u.UserName == userName)
    .Select(u => (int?)u.Id)
    .FirstOrDefaultAsync();
if (recordedBy == null)
{
    Console.Error.WriteLine($"No existe el usuario '{userName}' para registrar las decisiones.");
    return 6;
}

var decisionService = scope.ServiceProvider.GetRequiredService<IEquipmentClassificationDecisionService>();
var now = DateTime.UtcNow;
var applied = 0;
var already = 0;
var skippedConfirmed = 0;
var skippedUnmatched = 0;
var unchanged = 0;
var failed = new List<string>();
var withOwnDecision = (await db.EquipmentClassificationDecisions.AsNoTracking()
        .Where(d => d.DecisionKey.StartsWith(KeyPrefix))
        .Select(d => d.EquipmentId)
        .Distinct()
        .ToListAsync())
    .ToHashSet();
foreach (var r in results)
{
    // Sin regla para el nombre: no se confirma nada, el catálogo sigue "Por confirmar".
    if (r.Proposal.Rule == "NAME_UNMATCHED")
    {
        skippedUnmatched++;
        continue;
    }

    var ownDecision = withOwnDecision.Contains(r.EquipmentId);
    if (r.CurrentStatus == EquipmentClassificationReviewStatus.Confirmed)
    {
        // Una clasificación confirmada por el cliente (sin decisión propia) no se sobrescribe.
        if (!ownDecision)
        {
            skippedConfirmed++;
            continue;
        }

        // Ya clasificado por esta herramienta con el mismo resultado: no se duplica la decisión.
        if (r.CurrentCategory == r.Proposal.Category
            && r.CurrentType == r.Proposal.TypeClassification
            && r.CurrentUtensil == r.Proposal.UtensilType
            && string.Equals(r.CurrentDetail, r.Proposal.OtherDetail, StringComparison.Ordinal))
        {
            unchanged++;
            continue;
        }
    }

    var evidence = $"Análisis de datos (nombre, serie física, pista del Excel y artículo de igual nombre); regla {r.Proposal.Rule}; " +
                   $"confianza {r.Proposal.Confidence}. Autorizado por el usuario el {now:yyyy-MM-dd}." +
                   (r.Proposal.Confidence == InferenceConfidence.High ? string.Empty : " Pendiente de revisión del cliente.");
    EquipmentClassificationDecisionResult result;
    try
    {
        result = await decisionService.ApplyAsync(new EquipmentClassificationDecisionInput(
        DecisionKey(r.Code),
        r.Code,
        EquipmentClassificationReviewStatus.Confirmed,
        r.Proposal.Category,
        r.Proposal.TypeClassification,
        r.Proposal.UtensilType,
        r.Status,
        r.Proposal.OtherDetail,
        evidence,
        null,
        responsible,
        now,
        now,
        null,
        null,
        recordedBy));
    }
    catch (Exception ex)
    {
        // Una fila con problema no detiene las demás; la herramienta es idempotente y se puede relanzar.
        failed.Add($"{r.Code}: {ex.Message}");
        continue;
    }

    if (result.Applied)
    {
        applied++;
    }
    else
    {
        already++;
    }
}

Console.WriteLine($"APLICADO: {applied} decisiones nuevas | sin cambios: {unchanged} | ya existentes: {already} | confirmadas por el cliente y omitidas: {skippedConfirmed} | sin regla (siguen Por confirmar): {skippedUnmatched} | con error: {failed.Count}");
foreach (var failure in failed)
{
    Console.Error.WriteLine("ERROR " + failure);
}

return failed.Count == 0 ? 0 : 7;

static string DecisionKey(string catalogCode) => $"{KeyPrefix}{KeyVersion}-{catalogCode}";

static string Normalize(string? value)
{
    if (string.IsNullOrWhiteSpace(value))
    {
        return string.Empty;
    }

    var decomposed = value.Trim().ToLowerInvariant().Normalize(NormalizationForm.FormD);
    var sb = new StringBuilder();
    foreach (var ch in decomposed)
    {
        if (CharUnicodeInfo.GetUnicodeCategory(ch) != UnicodeCategory.NonSpacingMark)
        {
            sb.Append(ch);
        }
    }

    return string.Join(' ', sb.ToString().Split(' ', StringSplitOptions.RemoveEmptyEntries));
}

static void WriteCsv(string path, IEnumerable<CatalogRow> rows)
{
    static string Q(string? value) => "\"" + (value ?? string.Empty).Replace("\"", "\"\"") + "\"";
    var sb = new StringBuilder();
    sb.AppendLine("CatalogCode;Nombre;Unidades;ConSerie;Categoria;Clasificacion;Utensilio;Detalle;Confianza;Regla;Motivo;EstadoActual");
    foreach (var r in rows)
    {
        sb.AppendLine(string.Join(';',
            Q(r.Code), Q(r.Name), r.Units, r.WithSerial,
            Q(r.Proposal.Category.ToString()),
            Q(r.Proposal.TypeClassification?.ToString()),
            Q(r.Proposal.UtensilType?.ToString()),
            Q(r.Proposal.OtherDetail),
            Q(r.Proposal.Confidence.ToString()),
            Q(r.Proposal.Rule),
            Q(r.Proposal.Reason),
            Q(r.CurrentStatus.ToString())));
    }

    File.WriteAllText(path, sb.ToString(), new UTF8Encoding(encoderShouldEmitUTF8Identifier: true));
}

static Dictionary<string, string?> ParseArguments(string[] values)
{
    var result = new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase);
    for (var index = 0; index < values.Length; index++)
    {
        var value = values[index];
        if (!value.StartsWith("--", StringComparison.Ordinal))
        {
            continue;
        }

        var key = value[2..];
        result[key] = index + 1 < values.Length && !values[index + 1].StartsWith("--", StringComparison.Ordinal)
            ? values[++index]
            : null;
    }

    return result;
}

static void PrintUsage()
{
    Console.WriteLine("Simulación: dotnet run --project Tools/CatalogClassify -- --expected-server localhost --expected-database DB_Laboratorios_Univalle_REAL");
    Console.WriteLine("Aplicar:    añada --apply (registra decisiones Confirmadas con evidencia; use --user nihaht --responsible \"Nombre\").");
    Console.WriteLine("Salida CSV en ./outputs (cambie con --out RUTA). La simulación no escribe en la base.");
}

sealed record CatalogRow(int EquipmentId, string Code, string Name, int Units, int WithSerial,
    EquipmentClassificationReviewStatus CurrentStatus, GeneralStatus Status, CatalogClassificationProposal Proposal,
    EquipmentCategory CurrentCategory, EquipmentTypeClassification? CurrentType, UtensilType? CurrentUtensil, string? CurrentDetail);

sealed class ClassifyMarker;

sealed class ClassifyCurrentUserService : ICurrentUserService
{
    public int? UserId => null;
}
