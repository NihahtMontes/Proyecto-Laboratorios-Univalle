using System.Net;
using Laboratorios.Tests.Practica3.Soporte;
using Microsoft.EntityFrameworkCore;
using Proyecto_Laboratorios_Univalle.Models.Enums;

namespace Laboratorios.Tests.Practica3.Integracion;

/// <summary>
/// Ejercicio 4 — Integración entre formulario y base de datos.
/// Entidad principal: Laboratorio (ambiente de práctica).
/// Módulos: formulario Razor Pages/Laboratories/Create.cshtml → lógica de registro CreateModel.OnPostAsync
/// (validación del modelo, facultad activa, duplicado, normalización, código LAB-#####) → persistencia
/// ApplicationDbContext (tabla Laboratories, auditoría CreatedById).
/// Cada caso envía el formulario por HTTP al servidor real (con token antifalsificación y sesión iniciada) y
/// luego consulta la base para comprobar qué quedó guardado.
/// </summary>
[Collection(AppCollection.Name)]
public class Ej04_FormularioLaboratorioBaseDatosTests(AppFixture app)
{
    private const string Ejercicio = "Ej04_formulario_bd";
    private const string Form = "/Laboratories/Create";

    private Task<int> FacultyIdAsync() => app.QueryAsync(db => db.Faculties.Where(f => f.Name == EscenarioGastronomia.FacultyName).Select(f => f.Id).FirstAsync());
    private Task<int> CountLabsAsync() => app.QueryAsync(db => db.Laboratories.IgnoreQueryFilters().CountAsync());

    private static Dictionary<string, string> Campos(string facultad, string nombre, string? piso = null, string? descripcion = null) => new()
    {
        ["Input.FacultyId"] = facultad,
        ["Input.Name"] = nombre,
        ["Input.Floor"] = piso ?? string.Empty,
        ["Input.Description"] = descripcion ?? string.Empty
    };

    [Fact]
    public async Task I4_01_datos_validos_se_validan_guardan_y_se_listan()
    {
        using var gestor = await app.LoginAsAsync(EscenarioGastronomia.GestorUser);
        var facultad = await FacultyIdAsync();
        var antes = await CountLabsAsync();

        var respuesta = await gestor.SubmitAsync(Form, Campos(facultad.ToString(), "  Cocina   Internacional ", "Segundo piso", "Ambiente con 6 estaciones de cocción"), follow: false);
        var listado = await gestor.GetAsync(respuesta.Location!);

        var guardado = await app.QueryAsync(db => db.Laboratories.AsNoTracking().FirstOrDefaultAsync(l => l.Name == "Cocina Internacional"));
        var gestorId = await app.QueryAsync(db => db.Users.Where(u => u.UserName == EscenarioGastronomia.GestorUser).Select(u => u.Id).FirstAsync());
        var despues = await CountLabsAsync();

        var cumple = respuesta.Status == HttpStatusCode.Redirect && guardado != null && despues == antes + 1
            && guardado.Code.StartsWith("LAB-") && guardado.FacultyId == facultad && guardado.Status == GeneralStatus.Activo
            && guardado.CreatedById == gestorId && guardado.Floor == "Segundo piso" && listado.Contains("Cocina Internacional");
        Evidencia.Registrar(Ejercicio, "I4-01", "Registro con datos válidos (nombre con espacios extra)",
            "Facultad=FGT; Nombre=\"  Cocina   Internacional \"; Piso=Segundo piso",
            "Redirección al listado; 1 fila nueva con código LAB-#####, nombre normalizado, facultad, estado Activo y CreatedById del usuario",
            $"HTTP {(int)respuesta.Status} → {respuesta.Location}; filas {antes}→{despues}; Code={guardado?.Code}; Name=\"{guardado?.Name}\"; Status={guardado?.Status}; CreatedById={guardado?.CreatedById}; visible en listado={listado.Contains("Cocina Internacional")}",
            cumple);

        Assert.Equal(HttpStatusCode.Redirect, respuesta.Status);
        Assert.NotNull(guardado);
        Assert.Equal(antes + 1, despues);
        Assert.Matches("^LAB-\\d{5}$", guardado!.Code);
        Assert.Equal(gestorId, guardado.CreatedById);
        Assert.True(listado.Contains($"Laboratorio '{guardado.Code}' (Cocina Internacional) registrado exitosamente"), "Falta el mensaje de éxito");
    }

    public static IEnumerable<object[]> CasosRechazados() =>
    [
        ["I4-02", "Falta el nombre (obligatorio)", "{fac}", "", "El nombre del laboratorio es obligatorio"],
        ["I4-03", "Falta la facultad (obligatoria)", "", "Cocina Molecular", "Este campo es obligatorio."],
        ["I4-04", "Identificador duplicado: mismo nombre en la misma facultad (otra capitalización)", "{fac}", "cocina  FRÍA", "Ya existe un laboratorio con este nombre en la facultad seleccionada."],
        ["I4-05", "Facultad inexistente (FK inválida)", "999", "Cocina Andina", "La facultad seleccionada no está disponible."],
        ["I4-06", "Nombre de más de 100 caracteres", "{fac}", new string('X', 101), "El nombre no puede superar los 100 caracteres"],
    ];

    [Theory]
    [MemberData(nameof(CasosRechazados))]
    public async Task Datos_incompletos_o_duplicados_no_se_guardan(string caso, string descripcion, string facultad, string nombre, string mensaje)
    {
        using var gestor = await app.LoginAsAsync(EscenarioGastronomia.GestorUser);
        var fac = (await FacultyIdAsync()).ToString();
        var antes = await CountLabsAsync();

        var respuesta = await gestor.SubmitAsync(Form, Campos(facultad.Replace("{fac}", fac), nombre));
        var despues = await CountLabsAsync();
        var mensajes = WebSession.ValidationMessages(respuesta.Html).Concat(respuesta.Alerts).Distinct().ToList();

        var cumple = respuesta.Status == HttpStatusCode.OK && despues == antes && respuesta.Contains(mensaje);
        Evidencia.Registrar(Ejercicio, caso, descripcion,
            $"Facultad={(facultad == "{fac}" ? "FGT" : facultad == "" ? "(vacío)" : facultad)}; Nombre=\"{(nombre.Length > 30 ? nombre[..10] + "…(101 caracteres)" : nombre)}\"",
            $"Formulario devuelto con «{mensaje}»; sin filas nuevas",
            $"HTTP {(int)respuesta.Status}; mensajes: {string.Join(" | ", mensajes)}; filas {antes}→{despues}", cumple);

        Assert.Equal(HttpStatusCode.OK, respuesta.Status);
        Assert.Equal(antes, despues);
        Assert.True(respuesta.Contains(mensaje), $"No se mostró «{mensaje}». Mensajes: {string.Join(" | ", mensajes)}");
    }

    [Fact]
    public async Task I4_07_envio_sin_token_antifalsificacion_se_rechaza()
    {
        using var gestor = await app.LoginAsAsync(EscenarioGastronomia.GestorUser);
        var fac = (await FacultyIdAsync()).ToString();
        var antes = await CountLabsAsync();

        using var http = new HttpClient(new HttpClientHandler { CookieContainer = gestor.Cookies, AllowAutoRedirect = false }) { BaseAddress = new Uri(app.Host.BaseUrl) };
        var response = await http.PostAsync(Form, new FormUrlEncodedContent(Campos(fac, "Cocina Sin Token")));
        var despues = await CountLabsAsync();

        var cumple = response.StatusCode == HttpStatusCode.BadRequest && despues == antes;
        Evidencia.Registrar(Ejercicio, "I4-07", "Envío del formulario sin token antifalsificación (seguridad)", "POST directo sin __RequestVerificationToken",
            "HTTP 400; sin filas nuevas", $"HTTP {(int)response.StatusCode}; filas {antes}→{despues}", cumple);
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal(antes, despues);
    }
}
