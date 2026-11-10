using System.Globalization;
using System.Net;
using Laboratorios.Tests.Practica3.Soporte;
using Microsoft.EntityFrameworkCore;
using OfficeOpenXml;

namespace Laboratorios.Tests.Practica3.Integracion;

/// <summary>
/// Ejercicio 6 — Integración entre la aplicación y un servicio externo.
/// Componentes integrados:
///   A) Servicio de generación de documentos: ReportsController → IReportService (EPPlus + plantilla wwwroot/templates/L3.xlsx)
///      que produce el vale de salida L-3 en Excel.
///   B) Lógica de negocio ↔ base de datos (L3CircuitService → ApplicationDbContext), con falla de escritura y de conexión.
///   C) Mecanismo de autenticación: ASP.NET Core Identity (inicio de sesión, contraseña incorrecta y bloqueo).
/// Las fallas se simulan de forma controlada en el servidor de pruebas (LabAppHost.DocumentServiceDown, SaveTimeout,
/// DatabaseUnavailable): el código de la aplicación no se modifica.
/// </summary>
[Collection(AppCollection.Name)]
[TestCaseOrderer("Laboratorios.Tests.Practica3.Soporte.OrdenAlfabetico", "Laboratorios.Tests")]
public class Ej06_ServiciosExternosTests(AppFixture app)
{
    private const string Ejercicio = "Ej06_servicios_externos";

    private Task<int?> DepartureIdAsync(string planCode) => app.QueryAsync(db => db.Departures.AsNoTracking()
        .Where(d => d.Planning!.Code == planCode).Select(d => (int?)d.Id).FirstOrDefaultAsync());

    private async Task<int> AsegurarSalidaAsync()
    {
        // Precondición: PRA-P3-005 tiene salida L-3 (la registra el reintento de I6-01; si se ejecuta aislado, se registra aquí).
        if (await DepartureIdAsync(EscenarioGastronomia.PlanFallaBd) is int id) return id;
        using var s = await app.LoginAsAsync(EscenarioGastronomia.GestorUser);
        await RegistrarSalidaAsync(s, EscenarioGastronomia.PlanFallaBd);
        return (await DepartureIdAsync(EscenarioGastronomia.PlanFallaBd))!.Value;
    }

    private async Task<WebSession.Page> RegistrarSalidaAsync(WebSession s, string planCode)
    {
        var (planId, lines, econo) = await app.QueryAsync(async db =>
        {
            var plan = await db.Plannings.AsNoTracking().FirstAsync(p => p.Code == planCode);
            var ls = await db.PlanningRequirements.AsNoTracking().Where(r => r.PlanningId == plan.Id).Select(r => new { r.Id, r.RequestedQuantity }).ToListAsync();
            var e = await db.Laboratories.Where(l => l.Code == EscenarioGastronomia.EconomatoCode).Select(l => l.Id).FirstAsync();
            return (plan.Id, ls.Select(l => (l.Id, l.RequestedQuantity ?? 0)).ToList(), e);
        });
        var campos = new List<KeyValuePair<string, string>> { new("Departure.OriginLaboratoryId", econo.ToString()) };
        for (var i = 0; i < lines.Count; i++)
        {
            campos.Add(new($"Departure.Lines[{i}].DetailId", lines[i].Item1.ToString()));
            campos.Add(new($"Departure.Lines[{i}].Quantity", lines[i].Item2.ToString(CultureInfo.InvariantCulture)));
        }
        return await s.SubmitAsync($"/LaboratoryOperations/L3?id={planId}", campos, $"/LaboratoryOperations/L3?id={planId}&handler=Departure");
    }

    // ---------------- A) Servicio de generación de documentos ----------------

    [Fact]
    public async Task I6_03_documento_valido_vale_L3_en_Excel()
    {
        var departureId = await AsegurarSalidaAsync();
        using var s = await app.LoginAsAsync(EscenarioGastronomia.GestorUser);

        var response = await s.RawGetAsync($"/api/reports/download/l3?departureId={departureId}");
        var bytes = await response.Content.ReadAsByteArrayAsync();
        var code = await app.QueryAsync(db => db.Departures.Where(d => d.Id == departureId).Select(d => d.Code).FirstAsync());

        string contenido;
        using (var package = new ExcelPackage(new MemoryStream(bytes)))
        {
            var ws = package.Workbook.Worksheets[0];
            contenido = string.Join(" ", ws.Cells[ws.Dimension.Address].Select(c => c.Text).Where(t => !string.IsNullOrWhiteSpace(t))).ToUpperInvariant();
        }
        var cumple = response.StatusCode == HttpStatusCode.OK
            && response.Content.Headers.ContentType?.MediaType == "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet"
            && bytes.Length > 1000 && bytes[0] == (byte)'P' && bytes[1] == (byte)'K'
            && contenido.Contains("HARINA DE TRIGO") && contenido.Contains(code!.ToUpperInvariant());
        Evidencia.Registrar(Ejercicio, "I6-03", "Operación válida: generar el vale de salida L-3", $"GET /api/reports/download/l3?departureId={departureId} ({code})",
            "HTTP 200; archivo .xlsx válido con el código de la salida y el artículo entregado",
            $"HTTP {(int)response.StatusCode}; {response.Content.Headers.ContentType?.MediaType}; {bytes.Length:N0} bytes; firma ZIP={(bytes.Length > 1 && bytes[0] == 'P' && bytes[1] == 'K')}; contiene código y artículo={contenido.Contains(code!.ToUpperInvariant()) && contenido.Contains("HARINA DE TRIGO")}",
            cumple);
        Assert.True(cumple, contenido);
        File.WriteAllBytes(Path.Combine(Evidencia.Folder, $"Salida_L3_{code}.xlsx"), bytes);
    }

    [Fact]
    public async Task I6_04_respuesta_invalida_salida_inexistente()
    {
        using var s = await app.LoginAsAsync(EscenarioGastronomia.GestorUser);
        var response = await s.RawGetAsync("/api/reports/download/l3?departureId=999999");
        var body = await response.Content.ReadAsStringAsync();

        var cumple = response.StatusCode == HttpStatusCode.NotFound && body.Contains("Salida L-3 no encontrada.");
        Evidencia.Registrar(Ejercicio, "I6-04", "Respuesta inválida: el documento pedido no existe", "GET …/l3?departureId=999999",
            "HTTP 404 «Salida L-3 no encontrada.»", $"HTTP {(int)response.StatusCode} «{body.Trim('"')}»", cumple);
        Assert.True(cumple, body);
    }

    [Fact]
    public async Task I6_05_servicio_de_documentos_no_disponible_se_informa_sin_bloquear()
    {
        var departureId = await AsegurarSalidaAsync();
        using var s = await app.LoginAsAsync(EscenarioGastronomia.GestorUser);

        HttpResponseMessage enFalla;
        string cuerpo;
        using (LabAppHost.DocumentServiceDown.TurnOn())
        {
            enFalla = await s.RawGetAsync($"/api/reports/download/l3?departureId={departureId}");
            cuerpo = await enFalla.Content.ReadAsStringAsync();
        }
        var otraPantalla = await s.GetAsync("/Laboratories");
        var recuperado = await s.RawGetAsync($"/api/reports/download/l3?departureId={departureId}");

        const string msg = "No se pudo generar el reporte L-3. Intente nuevamente o contacte al administrador.";
        var cumple = enFalla.StatusCode == HttpStatusCode.InternalServerError && cuerpo.Contains(msg)
            && otraPantalla.Status == HttpStatusCode.OK && recuperado.StatusCode == HttpStatusCode.OK;
        Evidencia.Registrar(Ejercicio, "I6-05", "Error controlado: el servicio de documentos no responde (tiempo de espera)",
            "Falla simulada en IReportService; luego otra pantalla; luego reintento con el servicio restablecido",
            $"HTTP 500 con «{msg}»; la aplicación sigue respondiendo; el reintento genera el archivo",
            $"Durante la falla: HTTP {(int)enFalla.StatusCode} «{cuerpo.Trim('"')}»; Laboratorios: HTTP {(int)otraPantalla.Status}; reintento: HTTP {(int)recuperado.StatusCode}",
            cumple);
        Assert.True(cumple, cuerpo);
    }

    // ---------------- B) Lógica de negocio ↔ base de datos ----------------

    [Fact]
    public async Task I6_01_falla_al_guardar_no_deja_informacion_inconsistente()
    {
        using var s = await app.LoginAsAsync(EscenarioGastronomia.GestorUser);
        decimal Harina() => app.QueryAsync(db => db.ArticleStocks.Where(a => a.Article!.Code == EscenarioGastronomia.HarinaCode && a.Laboratory!.Code == EscenarioGastronomia.EconomatoCode)
            .Select(a => a.AvailableQuantity).FirstAsync()).GetAwaiter().GetResult();
        var harinaAntes = Harina();

        WebSession.Page enFalla;
        using (LabAppHost.SaveTimeout.TurnOn())
            enFalla = await RegistrarSalidaAsync(s, EscenarioGastronomia.PlanFallaBd);
        var salidasTrasFalla = await app.QueryAsync(db => db.Departures.CountAsync(d => d.Planning!.Code == EscenarioGastronomia.PlanFallaBd));
        var harinaTrasFalla = Harina();

        var reintento = await RegistrarSalidaAsync(s, EscenarioGastronomia.PlanFallaBd);
        var salidasTrasReintento = await app.QueryAsync(db => db.Departures.CountAsync(d => d.Planning!.Code == EscenarioGastronomia.PlanFallaBd));
        var harinaFinal = Harina();

        const string msg = "No se pudo registrar la salida. Intente nuevamente.";
        var cumple = enFalla.Contains(msg) && salidasTrasFalla == 0 && harinaTrasFalla == harinaAntes
            && reintento.Contains("registrada") && salidasTrasReintento == 1 && harinaFinal == harinaAntes - 3;
        Evidencia.Registrar(Ejercicio, "I6-01", "Error controlado: la base agota el tiempo de espera al guardar una salida L-3",
            "PRA-P3-005 (harina 3 kg) con SaveChanges en falla; luego reintento normal",
            $"«{msg}»; 0 salidas y existencia sin cambios; el reintento registra 1 salida y descuenta 3 kg",
            $"Falla: mensaje={enFalla.Contains(msg)}; salidas {salidasTrasFalla}; harina {harinaAntes:0.##}→{harinaTrasFalla:0.##} · Reintento: salidas {salidasTrasReintento}; harina {harinaFinal:0.##}",
            cumple);
        Assert.True(cumple, enFalla.Text[..Math.Min(800, enFalla.Text.Length)]);
    }

    [Fact]
    public async Task I6_02_base_de_datos_no_disponible_muestra_error_comprensible_y_se_recupera()
    {
        using var s = await app.LoginAsAsync(EscenarioGastronomia.GestorUser);
        var labsAntes = await app.QueryAsync(db => db.Laboratories.CountAsync());

        WebSession.Page enFalla;
        using (LabAppHost.DatabaseUnavailable.TurnOn())
            enFalla = await s.GetAsync("/Laboratories");
        var recuperado = await s.GetAsync("/Laboratories");
        var labsDespues = await app.QueryAsync(db => db.Laboratories.CountAsync());

        const string msg = "No se pudo conectar con la base de datos.";
        var cumple = enFalla.Status == HttpStatusCode.InternalServerError && enFalla.Contains(msg) && !enFalla.Contains("SqlException")
            && recuperado.Status == HttpStatusCode.OK && recuperado.Contains("Cocina Fría") && labsAntes == labsDespues;
        Evidencia.Registrar(Ejercicio, "I6-02", "Servicio no disponible: no se puede abrir la conexión a la base",
            "GET /Laboratories con la base caída; luego con la base restablecida",
            $"Página de error de la aplicación (HTTP 500) con «{msg}…» y sin detalles técnicos; la siguiente solicitud funciona; datos intactos",
            $"Falla: HTTP {(int)enFalla.Status}; mensaje comprensible={enFalla.Contains(msg)}; sin traza técnica={!enFalla.Contains("SqlException")}; recuperación: HTTP {(int)recuperado.Status}; laboratorios {labsAntes}→{labsDespues}",
            cumple);
        Assert.True(cumple, enFalla.Text[..Math.Min(1200, enFalla.Text.Length)]);
    }

    // ---------------- C) Mecanismo de autenticación ----------------

    [Fact]
    public async Task I6_06_credenciales_validas_autentican()
    {
        using var s = app.NewSession();
        var ok = await s.LoginAsync(EscenarioGastronomia.SupervisorUser, EscenarioGastronomia.Password);
        var inicio = await s.GetAsync("/");
        var cookie = s.Cookies.GetCookies(new Uri(app.Host.BaseUrl)).Any(c => c.Name == ".ProyectoUnivalle.Auth.vUniversal");
        var cumple = ok && cookie && inicio.Status == HttpStatusCode.OK && !inicio.Url.Contains("Login");
        Evidencia.Registrar(Ejercicio, "I6-06", "Autenticación válida", $"usuario {EscenarioGastronomia.SupervisorUser} + contraseña correcta",
            "Redirección fuera de /Login y cookie de autenticación emitida", $"autenticado={ok}; cookie={cookie}; inicio={inicio.Url} HTTP {(int)inicio.Status}", cumple);
        Assert.True(cumple);
    }

    [Fact]
    public async Task I6_07_contrasena_incorrecta_se_rechaza_con_mensaje_generico()
    {
        using var s = app.NewSession();
        var page = await s.SubmitAsync("/Login", new Dictionary<string, string>
        {
            ["Input.UserName"] = EscenarioGastronomia.SupervisorUser, ["Input.Password"] = "Incorrecta#2026", ["Input.RememberMe"] = "false"
        });
        var protegido = await s.RawGetAsync("/Laboratories");
        var cumple = page.Contains("Intento de inicio de sesión no válido.") && protegido.StatusCode == HttpStatusCode.Redirect
            && (protegido.Headers.Location?.OriginalString ?? "").Contains("/Login");
        Evidencia.Registrar(Ejercicio, "I6-07", "Respuesta de rechazo del servicio de autenticación", "contraseña incorrecta",
            "«Intento de inicio de sesión no válido.»; las pantallas protegidas redirigen a /Login",
            $"mensaje={page.Contains("Intento de inicio de sesión no válido.")}; /Laboratories → HTTP {(int)protegido.StatusCode} {protegido.Headers.Location}", cumple);
        Assert.True(cumple);
    }

    [Fact]
    public async Task I6_08_cinco_intentos_fallidos_bloquean_la_cuenta()
    {
        using var s = app.NewSession();
        WebSession.Page last = null!;
        for (var i = 0; i < 5; i++)
            last = await s.SubmitAsync("/Login", new Dictionary<string, string>
            {
                ["Input.UserName"] = EscenarioGastronomia.BloqueoUser, ["Input.Password"] = $"Mala#{i}2026xyz", ["Input.RememberMe"] = "false"
            });
        var conCorrecta = await s.LoginAsync(EscenarioGastronomia.BloqueoUser, EscenarioGastronomia.Password);
        var bloqueada = await app.QueryAsync(db => db.Users.Where(u => u.UserName == EscenarioGastronomia.BloqueoUser).Select(u => u.LockoutEnd).FirstAsync());

        const string msg = "Cuenta bloqueada temporalmente por 15 minutos";
        var cumple = last.Contains(msg) && !conCorrecta && bloqueada > DateTimeOffset.UtcNow.AddMinutes(14);
        Evidencia.Registrar(Ejercicio, "I6-08", "Bloqueo por intentos fallidos (protección del servicio de autenticación)",
            "5 contraseñas incorrectas y luego la correcta", $"«{msg}…»; la contraseña correcta tampoco entra durante el bloqueo",
            $"mensaje={last.Contains(msg)}; ingreso con contraseña correcta={conCorrecta}; LockoutEnd≈{(bloqueada.HasValue ? (bloqueada.Value - DateTimeOffset.UtcNow).TotalMinutes.ToString("0") + " min" : "null")}", cumple);
        Assert.True(cumple, last.Text[..Math.Min(800, last.Text.Length)]);
    }
}
