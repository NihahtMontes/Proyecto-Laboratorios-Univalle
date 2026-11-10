using System.Globalization;
using System.Net;
using Laboratorios.Tests.Practica3.Soporte;
using Microsoft.EntityFrameworkCore;
using Proyecto_Laboratorios_Univalle.Models;
using Proyecto_Laboratorios_Univalle.Models.Enums;

namespace Laboratorios.Tests.Practica3.Integracion;

/// <summary>
/// Ejercicio 5 — Integración entre dos módulos funcionales.
/// Módulo 1: Circuito L-3 de la práctica (salida de material y devolución, Pages/LaboratoryOperations/L3 + L3CircuitService).
/// Módulo 2: Existencias del Economato (ArticleStock, Catálogos › Existencias), que usa la Disponibilidad y la planificación.
/// Relación: la salida L-3 descuenta la existencia del laboratorio de origen; la devolución la repone; una salida sin
/// existencia suficiente se rechaza completa. Los casos se ejecutan en orden sobre la práctica de hoy (PRA-P3-001).
/// </summary>
[Collection(AppCollection.Name)]
[TestCaseOrderer("Laboratorios.Tests.Practica3.Soporte.OrdenAlfabetico", "Laboratorios.Tests")]
public class Ej05_SalidaL3ExistenciasTests(AppFixture app)
{
    private const string Ejercicio = "Ej05_l3_existencias";

    private sealed record Estado(decimal Harina, decimal Huevo, decimal Cuchillo, decimal Mantequilla, int Salidas, string? EstadoSalida, decimal EntregadoPendiente);

    private async Task<Estado> EstadoAsync(string planCode) => await app.QueryAsync(async db =>
    {
        var econo = await db.Laboratories.Where(l => l.Code == EscenarioGastronomia.EconomatoCode).Select(l => l.Id).FirstAsync();
        var stock = await db.ArticleStocks.AsNoTracking().Where(s => s.LaboratoryId == econo).Include(s => s.Article).ToListAsync();
        decimal Q(string code) => stock.First(s => s.Article!.Code == code).AvailableQuantity;
        var planId = await db.Plannings.Where(p => p.Code == planCode).Select(p => p.Id).FirstAsync();
        var departures = await db.Departures.AsNoTracking().Include(d => d.Items).Where(d => d.PlanningId == planId).ToListAsync();
        var pendiente = departures.SelectMany(d => d.Items).Sum(i => (i.Quantity ?? 0) - (i.ReturnedQuantity ?? 0));
        return new Estado(Q(EscenarioGastronomia.HarinaCode), Q(EscenarioGastronomia.HuevoCode), Q(EscenarioGastronomia.CuchilloCode),
            Q(EscenarioGastronomia.MantequillaCode), departures.Count, departures.FirstOrDefault()?.Status.ToString(), pendiente);
    });

    private static string Txt(Estado e) =>
        $"harina {e.Harina:0.##}; huevo {e.Huevo:0.##}; cuchillo {e.Cuchillo:0.##}; salidas {e.Salidas}" + (e.EstadoSalida != null ? $" ({e.EstadoSalida}); pendiente {e.EntregadoPendiente:0.##}" : "");

    private Task<(int PlanId, List<(int DetailId, string ArticleCode, decimal Qty)> Lines)> PlanAsync(string code) => app.QueryAsync(async db =>
    {
        var plan = await db.Plannings.AsNoTracking().FirstAsync(p => p.Code == code);
        var lines = await db.PlanningRequirements.AsNoTracking().Include(r => r.Article).Where(r => r.PlanningId == plan.Id)
            .Select(r => new { r.Id, r.Article!.Code, r.RequestedQuantity }).ToListAsync();
        return (plan.Id, lines.Select(l => (l.Id, l.Code, l.RequestedQuantity ?? 0)).ToList());
    });

    private Task<int> EconomatoAsync() => app.QueryAsync(db => db.Laboratories.Where(l => l.Code == EscenarioGastronomia.EconomatoCode).Select(l => l.Id).FirstAsync());

    private async Task<WebSession.Page> RegistrarSalidaAsync(WebSession s, string planCode)
    {
        var (planId, lines) = await PlanAsync(planCode);
        var campos = new List<KeyValuePair<string, string>> { new("Departure.OriginLaboratoryId", (await EconomatoAsync()).ToString()) };
        for (var i = 0; i < lines.Count; i++)
        {
            campos.Add(new($"Departure.Lines[{i}].DetailId", lines[i].DetailId.ToString()));
            campos.Add(new($"Departure.Lines[{i}].Quantity", lines[i].Qty.ToString(CultureInfo.InvariantCulture)));
        }
        return await s.SubmitAsync($"/LaboratoryOperations/L3?id={planId}", campos, $"/LaboratoryOperations/L3?id={planId}&handler=Departure");
    }

    private async Task<WebSession.Page> RegistrarDevolucionAsync(WebSession s, string planCode, params (string ArticleCode, decimal Qty)[] devoluciones)
    {
        var (planId, _) = await PlanAsync(planCode);
        var items = await app.QueryAsync(db => db.DepartureItems.AsNoTracking().Where(i => i.Departure!.PlanningId == planId)
            .Select(i => new { i.Id, i.Article!.Code }).ToListAsync());
        var encargado = await app.QueryAsync(db => db.Interns.Where(i => i.Name == "Tec. Ruth Arnez").Select(i => i.Id).FirstAsync());
        var campos = new List<KeyValuePair<string, string>>
        {
            new("Return.ResponsiblePersonId", encargado.ToString()),
            new("Return.ReturnDate", DateTime.Today.ToString("yyyy-MM-dd")),
            new("Return.Observations", "Devolución de prueba (Práctica 3)")
        };
        var i = 0;
        foreach (var (code, qty) in devoluciones)
        {
            campos.Add(new($"Return.Lines[{i}].DepartureItemId", items.First(x => x.Code == code).Id.ToString()));
            campos.Add(new($"Return.Lines[{i}].Quantity", qty.ToString(CultureInfo.InvariantCulture)));
            i++;
        }
        return await s.SubmitAsync($"/LaboratoryOperations/L3?id={planId}", campos, $"/LaboratoryOperations/L3?id={planId}&handler=Return");
    }

    [Fact]
    public async Task I5_01_salida_L3_descuenta_la_existencia_del_economato()
    {
        using var s = await app.LoginAsAsync(EscenarioGastronomia.GestorUser);
        var antes = await EstadoAsync(EscenarioGastronomia.PlanHoy);

        var pagina = await RegistrarSalidaAsync(s, EscenarioGastronomia.PlanHoy);
        var despues = await EstadoAsync(EscenarioGastronomia.PlanHoy);
        var existencias = await s.GetAsync("/Catalogs/Stock?laboratoryFilter=" + await EconomatoAsync());

        var cumple = pagina.Contains("registrada") && despues.Harina == antes.Harina - 5 && despues.Huevo == antes.Huevo - 24
            && despues.Cuchillo == antes.Cuchillo - 6 && despues.Salidas == 1 && despues.EstadoSalida == nameof(LoanStatus.Active);
        Evidencia.Registrar(Ejercicio, "I5-01", "Salida L-3 de la práctica de hoy (módulo 1) → existencias (módulo 2)",
            "PRA-P3-001: harina 5; huevo 24; cuchillo 6; origen ECONO", "Mensaje «Salida SAL-##### registrada.»; harina −5, huevo −24 y cuchillo −6 respecto del valor previo; 1 salida activa",
            $"Antes: {Txt(antes)} → Después: {Txt(despues)}", cumple);

        Assert.True(pagina.Contains("registrada"), pagina.Text[..Math.Min(600, pagina.Text.Length)]);
        Assert.Equal(antes.Harina - 5, despues.Harina);
        Assert.Equal(antes.Huevo - 24, despues.Huevo);
        Assert.Equal(antes.Cuchillo - 6, despues.Cuchillo);
        Assert.Equal(HttpStatusCode.OK, existencias.Status);
    }

    [Fact]
    public async Task I5_02_una_segunda_salida_de_la_misma_practica_se_rechaza()
    {
        using var s = await app.LoginAsAsync(EscenarioGastronomia.GestorUser);
        var antes = await EstadoAsync(EscenarioGastronomia.PlanHoy);

        var pagina = await RegistrarSalidaAsync(s, EscenarioGastronomia.PlanHoy);
        var despues = await EstadoAsync(EscenarioGastronomia.PlanHoy);

        const string msg = "La planificación ya tiene una salida registrada.";
        var cumple = pagina.Contains(msg) && despues == antes;
        Evidencia.Registrar(Ejercicio, "I5-02", "Intento de duplicar la salida (no debe completarse)", "Segundo envío de la salida de PRA-P3-001",
            $"«{msg}»; existencias sin cambios", $"Mensaje mostrado={pagina.Contains(msg)}; {Txt(despues)}", cumple);
        Assert.True(pagina.Contains(msg));
        Assert.Equal(antes, despues);
    }

    [Fact]
    public async Task I5_03_devolucion_parcial_repone_solo_lo_devuelto()
    {
        using var s = await app.LoginAsAsync(EscenarioGastronomia.GestorUser);
        var antes = await EstadoAsync(EscenarioGastronomia.PlanHoy);

        var pagina = await RegistrarDevolucionAsync(s, EscenarioGastronomia.PlanHoy, (EscenarioGastronomia.CuchilloCode, 4m));
        var despues = await EstadoAsync(EscenarioGastronomia.PlanHoy);

        var cumple = despues.Cuchillo == antes.Cuchillo + 4 && despues.Harina == antes.Harina && despues.EstadoSalida == nameof(LoanStatus.Active)
            && despues.EntregadoPendiente == antes.EntregadoPendiente - 4;
        Evidencia.Registrar(Ejercicio, "I5-03", "Devolución parcial (4 de 6 cuchillos)", "Devolver cuchillo 4",
            "Cuchillo +4; resto sin cambios; la salida sigue Active con pendiente −4",
            $"Antes: {Txt(antes)} → Después: {Txt(despues)}", cumple);
        Assert.True(cumple, pagina.Text[..Math.Min(600, pagina.Text.Length)]);
    }

    [Fact]
    public async Task I5_04_devolver_mas_de_lo_pendiente_se_rechaza_sin_tocar_existencias()
    {
        using var s = await app.LoginAsAsync(EscenarioGastronomia.GestorUser);
        var antes = await EstadoAsync(EscenarioGastronomia.PlanHoy);

        var pagina = await RegistrarDevolucionAsync(s, EscenarioGastronomia.PlanHoy, (EscenarioGastronomia.CuchilloCode, 3m));
        var despues = await EstadoAsync(EscenarioGastronomia.PlanHoy);

        var cumple = pagina.Contains("solo quedan 2 pendientes") && despues == antes;
        Evidencia.Registrar(Ejercicio, "I5-04", "Devolver más de lo pendiente (3 cuando quedan 2)", "Devolver cuchillo 3",
            "Rechazo «…solo quedan 2 pendientes»; existencias sin cambios", $"Mensaje={pagina.Contains("solo quedan 2 pendientes")}; {Txt(despues)}", cumple);
        Assert.True(cumple, pagina.Text[..Math.Min(800, pagina.Text.Length)]);
    }

    [Fact]
    public async Task I5_05_devolucion_total_cierra_la_salida_y_restituye_la_existencia_inicial()
    {
        using var s = await app.LoginAsAsync(EscenarioGastronomia.GestorUser);
        var antes = await EstadoAsync(EscenarioGastronomia.PlanHoy);
        var pagina = await RegistrarDevolucionAsync(s, EscenarioGastronomia.PlanHoy,
            (EscenarioGastronomia.HarinaCode, 5m), (EscenarioGastronomia.HuevoCode, 24m), (EscenarioGastronomia.CuchilloCode, 2m));
        var despues = await EstadoAsync(EscenarioGastronomia.PlanHoy);

        var cumple = despues.Harina == antes.Harina + 5 && despues.Huevo == antes.Huevo + 24 && despues.Cuchillo == antes.Cuchillo + 2
            && despues.EstadoSalida == nameof(LoanStatus.Returned) && despues.EntregadoPendiente == 0;
        Evidencia.Registrar(Ejercicio, "I5-05", "Devolución del resto (harina 5; huevo 24; cuchillo 2)", "Devolver todo lo pendiente",
            "Harina +5; huevo +24; cuchillo +2 (vuelven a su valor anterior a la salida); salida Returned; pendiente 0",
            $"Antes: {Txt(antes)} → Después: {Txt(despues)}", cumple);
        Assert.True(cumple, Txt(despues) + " · " + pagina.Text[..Math.Min(600, pagina.Text.Length)]);
    }

    [Fact]
    public async Task I5_06_salida_sin_existencia_suficiente_no_se_completa_y_no_deja_rastros()
    {
        using var s = await app.LoginAsAsync(EscenarioGastronomia.GestorUser);
        var antes = await EstadoAsync(EscenarioGastronomia.PlanSinStock);

        var pagina = await RegistrarSalidaAsync(s, EscenarioGastronomia.PlanSinStock);
        var despues = await EstadoAsync(EscenarioGastronomia.PlanSinStock);

        const string msg = "Stock insuficiente de ART-00004 · Mantequilla sin sal en ECONO: disponible 2, solicitado 5.";
        var cumple = pagina.Contains(msg) && despues == antes && despues.Salidas == 0;
        Evidencia.Registrar(Ejercicio, "I5-06", "Salida de un recurso no disponible (mantequilla 5 kg; existencia 2 kg)", "PRA-P3-004: mantequilla 5",
            $"«{msg}»; sin salida; existencia 2", $"Mensaje={pagina.Contains(msg)}; mantequilla {despues.Mantequilla:0.##}; salidas {despues.Salidas}", cumple);
        Assert.True(cumple, pagina.Text[..Math.Min(800, pagina.Text.Length)]);
    }

    [Fact]
    public async Task I5_07_consistencia_entre_modulos_existencia_mas_pendiente_igual_a_inicial()
    {
        // Invariante: para cada artículo, existencia actual + entregado sin devolver = existencia inicial (no se crea ni pierde material).
        var resumen = await app.QueryAsync(async db =>
        {
            var econo = await db.Laboratories.Where(l => l.Code == EscenarioGastronomia.EconomatoCode).Select(l => l.Id).FirstAsync();
            var stock = await db.ArticleStocks.AsNoTracking().Include(a => a.Article).Where(a => a.LaboratoryId == econo).ToListAsync();
            var pendiente = await db.DepartureItems.AsNoTracking().Where(i => i.Departure!.OriginLaboratoryId == econo && i.ArticleId != null)
                .GroupBy(i => i.ArticleId!.Value).Select(g => new { g.Key, P = g.Sum(i => (i.Quantity ?? 0) - (i.ReturnedQuantity ?? 0)) }).ToListAsync();
            return stock.Select(sk => (sk.Article!.Code, sk.AvailableQuantity + (pendiente.FirstOrDefault(p => p.Key == sk.ArticleId)?.P ?? 0))).ToList();
        });
        var iniciales = new Dictionary<string, decimal>
        {
            [EscenarioGastronomia.HarinaCode] = 50, [EscenarioGastronomia.HuevoCode] = 120, [EscenarioGastronomia.CuchilloCode] = 10, [EscenarioGastronomia.MantequillaCode] = 2
        };
        var faltan = resumen.Where(r => iniciales.TryGetValue(r.Code, out var ini) && r.Item2 != ini).ToList();
        var cumple = faltan.Count == 0;
        Evidencia.Registrar(Ejercicio, "I5-07", "Consistencia: existencia + pendiente = existencia inicial", "Todos los artículos del Economato",
            "Igualdad para todos los artículos", string.Join("; ", resumen.Select(r => $"{r.Code}: {r.Item2:0.##}")), cumple);
        Assert.Empty(faltan);
    }
}
