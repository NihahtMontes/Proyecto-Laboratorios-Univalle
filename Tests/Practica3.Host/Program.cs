using Laboratorios.Tests.Practica3.Soporte;

// Servidor de pruebas de aceptación: arranca la aplicación real con datos de prueba y espera hasta Ctrl+C.
var port = args.Length > 0 && int.TryParse(args[0], out var p) ? p : 5300;
await using var host = await LabAppHost.StartAsync(port);
await EscenarioGastronomia.SeedAsync(host.Services);
Console.WriteLine($"PRACTICA3_HOST_LISTO {host.BaseUrl}");

var stop = new TaskCompletionSource();
Console.CancelKeyPress += (_, e) => { e.Cancel = true; stop.TrySetResult(); };
AppDomain.CurrentDomain.ProcessExit += (_, _) => stop.TrySetResult();
await stop.Task;
