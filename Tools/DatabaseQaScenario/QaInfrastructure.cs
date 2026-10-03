using Microsoft.Data.SqlClient;
using Proyecto_Laboratorios_Univalle.Services;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace Proyecto_Laboratorios_Univalle.Tools.DatabaseQaScenario;

internal enum QaMode
{
    Help,
    Plan,
    Create,
    Seed,
    Verify,
    Benchmark,
    Backup,
    Credentials,
    All
}

internal enum QaProfile
{
    Smoke,
    Functional,
    Stress
}

internal sealed record QaCommand(QaMode Mode, QaProfile Profile, string? OutputDirectory)
{
    public static QaCommand Parse(string[] args)
    {
        if (args.Length == 0)
            return new QaCommand(QaMode.Help, QaProfile.Functional, null);

        if (!Enum.TryParse<QaMode>(args[0], true, out var mode))
            throw new ArgumentException($"Modo no reconocido: {args[0]}");

        var profile = QaProfile.Functional;
        string? output = null;
        for (var index = 1; index < args.Length; index++)
        {
            switch (args[index])
            {
                case "--profile" when index + 1 < args.Length:
                    if (!Enum.TryParse<QaProfile>(args[++index], true, out profile))
                        throw new ArgumentException("Perfil inválido. Use smoke, functional o stress.");
                    break;
                case "--output" when index + 1 < args.Length:
                    output = args[++index];
                    break;
                case "--help" or "-h":
                    mode = QaMode.Help;
                    break;
                default:
                    throw new ArgumentException($"Argumento no reconocido: {args[index]}");
            }
        }

        if (profile == QaProfile.Stress && mode is not QaMode.Plan and not QaMode.Help)
        {
            var enabled = string.Equals(
                Environment.GetEnvironmentVariable("QA_DATA_001_ALLOW_STRESS"),
                "YES",
                StringComparison.Ordinal);
            if (!enabled)
                throw new ArgumentException("El perfil stress exige QA_DATA_001_ALLOW_STRESS=YES y autorización específica.");
        }

        return new QaCommand(mode, profile, output);
    }

    public static void PrintHelp() => Console.WriteLine("""
        QA-DATA-001 — base reproducible para pruebas

        Uso:
          dotnet run --project Tools/DatabaseQaScenario -- plan [--profile functional] [--output ruta]
          dotnet run --project Tools/DatabaseQaScenario -- create [--profile functional|stress]
          dotnet run --project Tools/DatabaseQaScenario -- seed [--profile smoke|functional|stress]
          dotnet run --project Tools/DatabaseQaScenario -- verify [--profile functional|stress]
          dotnet run --project Tools/DatabaseQaScenario -- benchmark [--profile functional|stress]
          dotnet run --project Tools/DatabaseQaScenario -- backup [--profile functional]
          dotnet run --project Tools/DatabaseQaScenario -- credentials
          dotnet run --project Tools/DatabaseQaScenario -- all [--profile functional]

        Fuente fija de solo lectura: DB_Laboratorios_Univalle
        Destino: DB_Laboratorios_Univalle_SCENARIOS_QA (smoke/functional) o DB_Laboratorios_Univalle_STRESS_QA (stress, exige QA_DATA_001_ALLOW_STRESS=YES)
        El modo credentials copia la contraseña QA al portapapeles sin imprimirla.
        """);
}

internal sealed record QaPaths(string OutputDirectory, string ManifestPath, string BenchmarkPath)
{
    public static QaPaths Create(string? requested)
    {
        var root = string.IsNullOrWhiteSpace(requested)
            ? Path.Combine(Directory.GetCurrentDirectory(), "outputs", "qa-data-001")
            : Path.GetFullPath(requested);
        Directory.CreateDirectory(root);
        return new QaPaths(
            root,
            Path.Combine(root, "qa-data-001-manifest.json"),
            Path.Combine(root, "qa-data-001-benchmark.json"));
    }
}

internal static class QaConnections
{
    public static string Build(string database) => new SqlConnectionStringBuilder
    {
        DataSource = "localhost",
        InitialCatalog = database,
        IntegratedSecurity = true,
        Encrypt = true,
        TrustServerCertificate = true,
        MultipleActiveResultSets = true,
        ConnectTimeout = 15,
        MaxPoolSize = 10,
        ApplicationName = "Laboratorios-QA-DATA-001"
    }.ConnectionString;
}

internal sealed class QaCurrentUserService : ICurrentUserService
{
    public int? UserId => null;
}

internal static class QaCredentialStore
{
    private const string UserSecretsId = "e2ee0ec6-49ab-4006-8a26-76ab45a791b4";
    private const string Key = "QaScenario:SharedPassword";

    public static string GetOrCreatePassword()
    {
        var root = Load();
        if (root[Key]?.GetValue<string>() is { Length: > 0 } current)
            return current;

        var password = CreatePassword();
        root[Key] = password;
        Save(root);
        return password;
    }

    public static string GetPassword()
    {
        var password = Load()[Key]?.GetValue<string>();
        return !string.IsNullOrWhiteSpace(password)
            ? password
            : throw new InvalidOperationException("Todavía no existen credenciales QA. Ejecute create o all.");
    }

    public static async Task CopyToClipboardAsync(string password)
    {
        var start = new System.Diagnostics.ProcessStartInfo("clip.exe")
        {
            UseShellExecute = false,
            RedirectStandardInput = true,
            CreateNoWindow = true
        };
        using var process = System.Diagnostics.Process.Start(start)
            ?? throw new InvalidOperationException("No se pudo abrir el portapapeles de Windows.");
        await process.StandardInput.WriteAsync(password);
        process.StandardInput.Close();
        await process.WaitForExitAsync();
        if (process.ExitCode != 0)
            throw new InvalidOperationException("No se pudo copiar la contraseña QA.");
    }

    private static JsonObject Load()
    {
        var path = SecretsPath();
        if (!File.Exists(path)) return new JsonObject();
        return JsonNode.Parse(File.ReadAllText(path)) as JsonObject ?? new JsonObject();
    }

    private static void Save(JsonObject root)
    {
        var path = SecretsPath();
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        var temporary = path + ".qa-data-001.tmp";
        File.WriteAllText(temporary, root.ToJsonString(new JsonSerializerOptions { WriteIndented = true }));
        File.Move(temporary, path, true);
    }

    private static string SecretsPath() => Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
        "Microsoft", "UserSecrets", UserSecretsId, "secrets.json");

    private static string CreatePassword()
    {
        var random = Convert.ToBase64String(RandomNumberGenerator.GetBytes(24))
            .Replace('+', 'x').Replace('/', 'Y').TrimEnd('=');
        return $"Qa1!{random}";
    }
}

internal static class QaHash
{
    public static string Sha256(string value) => Convert.ToHexString(
        SHA256.HashData(Encoding.UTF8.GetBytes(value)));

    public static string FileSha256(string path) => Convert.ToHexString(
        SHA256.HashData(File.ReadAllBytes(path)));
}
