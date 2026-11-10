using System.Diagnostics;
using System.Reflection;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using Proyecto_Laboratorios_Univalle.Data;
using Proyecto_Laboratorios_Univalle.Services;

namespace Laboratorios.Tests.Practica3.Soporte;

/// <summary>
/// Servidor de pruebas de la Práctica 3. Ejecuta el Program.cs REAL de la aplicación (middleware, Identity,
/// autorización, Razor Pages y servicios) sobre Kestrel, interceptando la construcción del host con el mismo
/// mecanismo que usa WebApplicationFactory (evento de diagnóstico "HostBuilding"). Solo se reemplazan dos piezas
/// ligadas a SQL Server: el proveedor de datos (base EF Core en memoria, desechable) y el bloqueo sp_getapplock.
/// La base oficial nunca se usa.
/// </summary>
public sealed class LabAppHost : IAsyncDisposable
{
    public const string WebProjectFolder = "Proyecto Laboratorios Univalle";

    private readonly Thread _thread;
    private readonly IHost _host;

    public string BaseUrl { get; }
    public IServiceProvider Services => _host.Services;
    public string DatabaseName { get; }

    /// <summary>Ej. 6: base de datos no disponible (el servidor SQL no acepta conexiones).</summary>
    public static readonly FaultSwitch DatabaseUnavailable = new();

    /// <summary>Ej. 6: la base acepta lecturas pero rechaza la escritura (tiempo de espera agotado al guardar).</summary>
    public static readonly FaultSwitch SaveTimeout = new();

    /// <summary>Ej. 6: el servicio de generación de documentos (IReportService) no responde.</summary>
    public static readonly FaultSwitch DocumentServiceDown = new();

    private LabAppHost(Thread thread, IHost host, string baseUrl, string databaseName)
    {
        _thread = thread;
        _host = host;
        BaseUrl = baseUrl;
        DatabaseName = databaseName;
    }

    public static async Task<LabAppHost> StartAsync(int port, string? databaseName = null, TimeSpan? timeout = null)
    {
        databaseName ??= "Practica3-" + Guid.NewGuid().ToString("N");
        var baseUrl = $"http://127.0.0.1:{port}";
        var root = new InMemoryDatabaseRoot();
        var contentRoot = FindWebProjectDirectory();

        // Program.cs exige una cadena de conexión antes de construir el host; el proveedor se reemplaza después.
        Environment.SetEnvironmentVariable("ConnectionStrings__DefaultConnection", "Server=no-usar;Database=no-usar;");
        Environment.SetEnvironmentVariable("ASPNETCORE_ENVIRONMENT", "Development");
        Environment.SetEnvironmentVariable("Database__AutoMigrate", "false");
        Environment.SetEnvironmentVariable("Database__RunSeed", "false");

        var built = new TaskCompletionSource<IHost>(TaskCreationOptions.RunContinuationsAsynchronously);
        var observer = new HostingObserver(services => ReplaceInfrastructure(services, databaseName, root), built);
        using var subscription = DiagnosticListener.AllListeners.Subscribe(observer);

        var entryPoint = typeof(ApplicationDbContext).Assembly.EntryPoint
            ?? throw new InvalidOperationException("No se encontró el punto de entrada de la aplicación web.");
        var args = new[]
        {
            $"--urls={baseUrl}",
            $"--contentRoot={contentRoot}",
            $"--applicationName={WebProjectFolder}",
            "--environment=Development"
        };

        Exception? startupError = null;
        var thread = new Thread(() =>
        {
            try { entryPoint.Invoke(null, entryPoint.GetParameters().Length == 0 ? null : new object[] { args }); }
            catch (Exception ex) { startupError = ex; built.TrySetException(ex); }
        }) { IsBackground = true, Name = "Practica3-AppHost" };
        thread.Start();

        var host = await built.Task.WaitAsync(timeout ?? TimeSpan.FromSeconds(60));
        var appHost = new LabAppHost(thread, host, baseUrl, databaseName);

        // Espera a que Kestrel responda la pantalla de inicio de sesión.
        using var http = new HttpClient { BaseAddress = new Uri(baseUrl) };
        var deadline = DateTime.UtcNow + (timeout ?? TimeSpan.FromSeconds(60));
        while (true)
        {
            if (startupError != null) throw new InvalidOperationException("La aplicación no arrancó.", startupError);
            try
            {
                var response = await http.GetAsync("/Login");
                if (response.IsSuccessStatusCode) break;
            }
            catch (HttpRequestException) { }
            if (DateTime.UtcNow > deadline) throw new TimeoutException("La aplicación no respondió a tiempo.");
            await Task.Delay(200);
        }
        return appHost;
    }

    private static void ReplaceInfrastructure(IServiceCollection services, string databaseName, InMemoryDatabaseRoot root)
    {
        // Quitar todo lo que Program.cs registró para SQL Server.
        var toRemove = services.Where(d =>
                d.ServiceType == typeof(DbContextOptions<ApplicationDbContext>) ||
                d.ServiceType == typeof(DbContextOptions) ||
                (d.ServiceType.IsGenericType && d.ServiceType.GetGenericTypeDefinition().Name.StartsWith("IDbContextOptionsConfiguration")))
            .ToList();
        foreach (var descriptor in toRemove) services.Remove(descriptor);

        services.AddDbContext<ApplicationDbContext>((_, options) =>
        {
            if (DatabaseUnavailable.IsOn)
            {
                // Base no disponible: se usa el proveedor REAL de SQL Server contra un servidor inexistente
                // (puerto cerrado). La primera consulta de la solicitud falla con SqlException, como en una caída real.
                options.UseSqlServer("Server=127.0.0.1,1;Database=Laboratorios;User Id=prueba;Password=prueba;Connect Timeout=3;Encrypt=False",
                        sql => sql.UseQuerySplittingBehavior(QuerySplittingBehavior.SplitQuery))
                    .UseQueryTrackingBehavior(QueryTrackingBehavior.NoTracking);
                return;
            }
            options.UseInMemoryDatabase(databaseName, root)
                .UseQueryTrackingBehavior(QueryTrackingBehavior.NoTracking)
                .ConfigureWarnings(w => w.Ignore(InMemoryEventId.TransactionIgnoredWarning))
                .AddInterceptors(new FaultInterceptor(SaveTimeout));
        });

        // Servicio de documentos con falla controlable (proxy transparente sobre el ReportService real).
        services.RemoveAll<IReportService>();
        services.AddScoped<ReportService>();
        services.AddScoped<IReportService>(sp => FaultyProxy<IReportService>.Wrap(sp.GetRequiredService<ReportService>(), DocumentServiceDown,
            "Simulación Práctica 3: el servicio de generación de documentos no respondió (tiempo de espera agotado)."));

        services.RemoveAll<IPlanningLock>();
        services.AddScoped<IPlanningLock, InProcessPlanningLock>();
    }

    private static string FindWebProjectDirectory()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir != null && !File.Exists(Path.Combine(dir.FullName, WebProjectFolder + ".csproj")))
            dir = dir.Parent;
        return dir?.FullName ?? throw new DirectoryNotFoundException("No se encontró la carpeta del proyecto web.");
    }

    public async ValueTask DisposeAsync()
    {
        var lifetime = _host.Services.GetRequiredService<IHostApplicationLifetime>();
        lifetime.StopApplication();
        await Task.Run(() => _thread.Join(TimeSpan.FromSeconds(15)));
    }

    private sealed class HostingObserver : IObserver<DiagnosticListener>, IObserver<KeyValuePair<string, object?>>
    {
        private readonly Action<IServiceCollection> _configure;
        private readonly TaskCompletionSource<IHost> _built;

        public HostingObserver(Action<IServiceCollection> configure, TaskCompletionSource<IHost> built)
        {
            _configure = configure;
            _built = built;
        }

        public void OnNext(DiagnosticListener listener)
        {
            if (listener.Name == "Microsoft.Extensions.Hosting") listener.Subscribe(this);
        }

        public void OnNext(KeyValuePair<string, object?> value)
        {
            if (value.Key == "HostBuilding" && value.Value is IHostBuilder builder)
                builder.ConfigureServices((_, services) => _configure(services));
            else if (value.Key == "HostBuilt" && value.Value is IHost host)
                _built.TrySetResult(host);
        }

        public void OnCompleted() { }
        public void OnError(Exception error) { }
    }
}

/// <summary>Sustituto en proceso de SqlServerPlanningLock (sp_getapplock no existe fuera de SQL Server).</summary>
public sealed class InProcessPlanningLock : IPlanningLock
{
    public Task<bool> TryAcquireAsync(IEnumerable<string> resources, CancellationToken ct = default) => Task.FromResult(true);
}

/// <summary>Interruptor de falla controlada para simular «servicio no disponible».</summary>
public sealed class FaultSwitch
{
    private volatile bool _on;
    public bool IsOn => _on;
    public IDisposable TurnOn()
    {
        _on = true;
        return new Off(this);
    }
    private sealed class Off(FaultSwitch owner) : IDisposable
    {
        public void Dispose() => owner._on = false;
    }
}

/// <summary>Con la falla activa, SaveChanges falla como lo haría un servidor que agota el tiempo de espera al escribir.</summary>
public sealed class FaultInterceptor : ISaveChangesInterceptor
{
    private readonly FaultSwitch _fault;
    public FaultInterceptor(FaultSwitch fault) => _fault = fault;

    public InterceptionResult<int> SavingChanges(DbContextEventData eventData, InterceptionResult<int> result)
    {
        ThrowIfOn();
        return result;
    }

    public ValueTask<InterceptionResult<int>> SavingChangesAsync(DbContextEventData eventData, InterceptionResult<int> result, CancellationToken cancellationToken = default)
    {
        ThrowIfOn();
        return ValueTask.FromResult(result);
    }

    private void ThrowIfOn()
    {
        if (_fault.IsOn)
            throw new TimeoutException("Simulación Práctica 3: el servidor de base de datos agotó el tiempo de espera al guardar.");
    }
}

/// <summary>Proxy que delega en la implementación real y, con la falla activa, lanza TimeoutException.</summary>
public class FaultyProxy<T> : DispatchProxy where T : class
{
    private T _inner = default!;
    private FaultSwitch _fault = default!;
    private string _message = string.Empty;

    public static T Wrap(T inner, FaultSwitch fault, string message)
    {
        var proxy = Create<T, FaultyProxy<T>>();
        var typed = (FaultyProxy<T>)(object)proxy;
        typed._inner = inner;
        typed._fault = fault;
        typed._message = message;
        return proxy;
    }

    protected override object? Invoke(MethodInfo? targetMethod, object?[]? args)
    {
        if (_fault.IsOn) throw new TimeoutException(_message);
        try { return targetMethod!.Invoke(_inner, args); }
        catch (TargetInvocationException ex) when (ex.InnerException != null) { throw ex.InnerException; }
    }
}
