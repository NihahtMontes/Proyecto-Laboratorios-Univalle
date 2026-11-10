using System.Net;
using System.Text.RegularExpressions;
using Microsoft.Extensions.DependencyInjection;
using Proyecto_Laboratorios_Univalle.Data;

namespace Laboratorios.Tests.Practica3.Soporte;

/// <summary>
/// Sesión HTTP de un usuario del sistema: conserva cookies (autenticación, TempData), lee el token antifalsificación
/// de cada formulario y envía los datos tal como lo hace el navegador.
/// </summary>
public sealed class WebSession : IDisposable
{
    private readonly HttpClient _client;
    public CookieContainer Cookies { get; } = new();

    public WebSession(string baseUrl)
    {
        var handler = new HttpClientHandler { CookieContainer = Cookies, AllowAutoRedirect = false, UseCookies = true };
        _client = new HttpClient(handler) { BaseAddress = new Uri(baseUrl), Timeout = TimeSpan.FromSeconds(60) };
    }

    public sealed record Page(HttpStatusCode Status, string Url, string Html, string? Location)
    {
        public string Text => WebUtility.HtmlDecode(Regex.Replace(Regex.Replace(Html, "<script[\\s\\S]*?</script>", " "), "<[^>]+>", " "));

        /// <summary>Mensajes emergentes (SweetAlert) que el layout arma con TempData: éxito, error, advertencia e información.</summary>
        public IReadOnlyList<string> Alerts => Regex.Matches(Html, "(?:title: escapeHtml\\(|text: )(\"(?:[^\"\\\\]|\\\\.)*\")")
            .Select(m => System.Text.Json.JsonSerializer.Deserialize<string>(m.Groups[1].Value) ?? string.Empty)
            .Where(t => t.Length > 0).ToList();

        public bool Contains(string text) => Text.Contains(text, StringComparison.Ordinal) || Html.Contains(text, StringComparison.Ordinal)
            || Alerts.Any(a => a.Contains(text, StringComparison.Ordinal));
    }

    public async Task<Page> GetAsync(string url, bool follow = true)
    {
        var response = await _client.GetAsync(url);
        return await FollowAsync(response, url, follow);
    }

    /// <summary>Carga el formulario en <paramref name="formUrl"/>, toma su token y envía <paramref name="fields"/> a <paramref name="postUrl"/>.</summary>
    public async Task<Page> SubmitAsync(string formUrl, IEnumerable<KeyValuePair<string, string>> fields, string? postUrl = null, bool follow = true)
    {
        var form = await GetAsync(formUrl);
        var token = AntiforgeryToken(form.Html)
            ?? throw new InvalidOperationException($"La pantalla {formUrl} no tiene token antifalsificación (¿sesión sin permiso?). Estado {form.Status}, URL {form.Url}.");
        var data = fields.ToList();
        data.Add(new("__RequestVerificationToken", token));
        var target = postUrl ?? formUrl;
        var response = await _client.PostAsync(target, new FormUrlEncodedContent(data));
        return await FollowAsync(response, target, follow);
    }

    public async Task<bool> LoginAsync(string user, string password)
    {
        var page = await SubmitAsync("/Login", new Dictionary<string, string>
        {
            ["Input.UserName"] = user,
            ["Input.Password"] = password,
            ["Input.RememberMe"] = "false"
        }, follow: false);
        return page.Status == HttpStatusCode.Redirect && !(page.Location ?? "").Contains("Login", StringComparison.OrdinalIgnoreCase);
    }

    public async Task<HttpResponseMessage> RawGetAsync(string url) => await _client.GetAsync(url);

    private async Task<Page> FollowAsync(HttpResponseMessage response, string url, bool follow)
    {
        var hops = 0;
        while (follow && (int)response.StatusCode is >= 300 and < 400 && response.Headers.Location != null && hops++ < 10)
        {
            url = response.Headers.Location.IsAbsoluteUri ? response.Headers.Location.PathAndQuery : response.Headers.Location.OriginalString;
            response = await _client.GetAsync(url);
        }
        var html = await response.Content.ReadAsStringAsync();
        return new Page(response.StatusCode, url, html, response.Headers.Location?.OriginalString);
    }

    public static string? AntiforgeryToken(string html)
    {
        var match = Regex.Match(html, "name=\"__RequestVerificationToken\"[^>]*value=\"([^\"]+)\"");
        if (!match.Success) match = Regex.Match(html, "value=\"([^\"]+)\"[^>]*name=\"__RequestVerificationToken\"");
        return match.Success ? match.Groups[1].Value : null;
    }

    /// <summary>Mensajes de validación visibles junto a los campos y en el resumen del formulario.</summary>
    public static IReadOnlyList<string> ValidationMessages(string html) =>
        Regex.Matches(html, "<span[^>]*class=\"[^\"]*field-validation-error[^\"]*\"[^>]*>([\\s\\S]*?)</span>|<li>([^<]+)</li>")
            .Select(m => WebUtility.HtmlDecode((m.Groups[1].Success ? m.Groups[1].Value : m.Groups[2].Value).Trim()))
            .Where(t => t.Length > 0)
            .ToList();

    public void Dispose() => _client.Dispose();
}

/// <summary>Servidor compartido por las pruebas HTTP de la Práctica 3 (una sola instancia por ejecución).</summary>
public sealed class AppFixture : IAsyncLifetime
{
    public LabAppHost Host { get; private set; } = null!;

    public async Task InitializeAsync()
    {
        Host = await LabAppHost.StartAsync(port: 5291);
        await EscenarioGastronomia.SeedAsync(Host.Services);
    }

    public async Task DisposeAsync() => await Host.DisposeAsync();

    public WebSession NewSession() => new(Host.BaseUrl);

    public async Task<WebSession> LoginAsAsync(string user)
    {
        var session = NewSession();
        if (!await session.LoginAsync(user, EscenarioGastronomia.Password))
            throw new InvalidOperationException($"No se pudo iniciar sesión como {user}.");
        return session;
    }

    /// <summary>Consulta directa a la base del servidor (verificación de persistencia).</summary>
    public async Task<T> QueryAsync<T>(Func<ApplicationDbContext, Task<T>> query)
    {
        using var scope = Host.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        return await query(db);
    }
}

[CollectionDefinition(Name)]
public sealed class AppCollection : ICollectionFixture<AppFixture>
{
    public const string Name = "Practica3-Servidor";
}
