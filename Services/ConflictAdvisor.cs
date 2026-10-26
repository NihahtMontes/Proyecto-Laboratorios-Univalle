using System.Globalization;

namespace Proyecto_Laboratorios_Univalle.Services;

/// <summary>Recomendación de resolución para el conflicto de material de un artículo en un día.</summary>
public sealed record ConflictAdvice(
    ConflictKind Kind,
    string Title,
    string Summary,
    IReadOnlyList<string> Actions,
    IReadOnlyList<int> SuggestedMoveIds,
    decimal StockIncrease);

/// <summary>
/// Sugerencias en texto para resolver el conflicto de un artículo en un día (puro, sin acceso a datos).
/// </summary>
public static class ConflictAdvisor
{
    private static readonly CultureInfo Cultura = new("es-ES");

    /// <summary>Arma la recomendación según el tipo de conflicto de la demanda del día.</summary>
    public static ConflictAdvice Advise(ArticleDayDemand demand, IReadOnlyList<WeekPractice> practices)
    {
        var porId = practices.ToDictionary(p => p.Id);
        return demand.Kind switch
        {
            ConflictKind.NoStock => new(ConflictKind.NoStock, "Sin stock registrado",
                $"{demand.ArticleName} no tiene existencias registradas: no se puede evaluar la demanda de este día.",
                ["Registra la existencia del artículo."], [], demand.PeakRequested),
            ConflictKind.Rotation => Rotacion(demand, porId),
            ConflictKind.Shortage => Escasez(demand, porId),
            ConflictKind.Overlap => Solapamiento(demand, porId),
            _ => new(ConflictKind.None, "Sin conflicto",
                $"La demanda de {demand.ArticleName} cabe en el stock disponible.", [], [], 0m)
        };
    }

    /// <summary>Stock insuficiente: alguna práctica sola pide más que todo el stock disponible.</summary>
    private static ConflictAdvice Escasez(ArticleDayDemand demand, IReadOnlyDictionary<int, WeekPractice> porId)
    {
        var stock = demand.Stock.Available!.Value;
        var excedentes = demand.Practices
            .Where(p => p.Quantity > stock)
            .OrderByDescending(p => p.Quantity)
            .Select(p => (p.PracticeId, Cantidad: p.Quantity, Codigo: porId[p.PracticeId].Code))
            .ToList();
        var mayor = excedentes[0];

        var acciones = new List<string> { $"Aumenta el stock en al menos {Cantidad(mayor.Cantidad - stock)}." };
        acciones.AddRange(excedentes.Select(p => $"Reduce la cantidad pedida por {p.Codigo}."));
        // Si el pico del día supera lo que pide la mayor práctica sola, además se cruzan varias.
        if (demand.Shortage > mayor.Cantidad - stock)
            acciones.Add($"Además se cruza con otras prácticas: faltan {Cantidad(demand.Shortage)} en la franja {demand.PeakText}.");

        return new(demand.Kind, "Stock insuficiente",
            $"{mayor.Codigo} pide {Cantidad(mayor.Cantidad)} y solo hay {Cantidad(stock)}.",
            acciones, [], demand.Shortage);
    }

    /// <summary>Conflicto de horario: varias prácticas simultáneas superan juntas el stock (ninguna sola lo hace).</summary>
    private static ConflictAdvice Solapamiento(ArticleDayDemand demand, IReadOnlyDictionary<int, WeekPractice> porId)
    {
        var stock = demand.Stock.Available!.Value;
        var pico = demand.OverSegments.OrderByDescending(s => s.Load).First();
        var faltante = pico.Load - stock;

        // Prácticas activas en el tramo del pico, con la cantidad que pide cada una.
        var activas = pico.ActiveIds
            .Select(id => (Id: id, Codigo: porId[id].Code, Cantidad: demand.Practices.Single(p => p.PracticeId == id).Quantity))
            .ToList();

        // Mover la práctica de menor cantidad que cubra sola el faltante; si ninguna alcanza, las mayores hasta cubrirlo.
        var solas = activas.Where(a => a.Cantidad >= faltante).OrderBy(a => a.Cantidad).ThenBy(a => a.Id).ToList();
        var mover = new List<(int Id, string Codigo, decimal Cantidad)>();
        if (solas.Count > 0)
        {
            mover.Add(solas[0]);
        }
        else
        {
            var acumulado = 0m;
            foreach (var a in activas.OrderByDescending(a => a.Cantidad).ThenBy(a => a.Id))
            {
                mover.Add(a);
                acumulado += a.Cantidad;
                if (acumulado >= faltante) break;
            }
        }

        var acciones = new List<string> { $"Aumenta el stock en {Cantidad(demand.Shortage)}." };
        acciones.Add(mover.Count == 1
            ? $"Mueve {mover[0].Codigo} ({Cantidad(mover[0].Cantidad)}) fuera de esa franja y el conflicto se resuelve."
            : $"Mueve fuera de esa franja: {string.Join(", ", mover.Select(m => $"{m.Codigo} ({Cantidad(m.Cantidad)})"))}.");

        return new(demand.Kind, "Conflicto de horario",
            $"{Franja(pico)} se piden {Cantidad(pico.Load)} a la vez y hay {Cantidad(stock)} (faltan {Cantidad(faltante)}).",
            acciones, [.. mover.Select(m => m.Id)], demand.Shortage);
    }

    /// <summary>Rotación: el total del día supera el stock, pero las prácticas no se cruzan en el tiempo.</summary>
    private static ConflictAdvice Rotacion(ArticleDayDemand demand, IReadOnlyDictionary<int, WeekPractice> porId)
    {
        var orden = demand.Practices
            .Select(p => porId[p.PracticeId])
            .OrderBy(p => p.StartTime)
            .ThenBy(p => p.Id)
            .Select(p => $"{p.Code} {p.TimeText}")
            .ToList();

        return new(demand.Kind, "Requiere rotación",
            $"El total del día ({Cantidad(demand.TotalRequested)}) supera el stock ({Cantidad(demand.Stock.Available!.Value)}), " +
            "pero las prácticas no se cruzan: alcanza si el material pasa de una práctica a la siguiente.",
            [$"Orden de entrega: {string.Join(" → ", orden)}."], [], 0m);
    }

    /// <summary>Cantidad legible en es-ES: sin decimales si es entera (10 -> "10", 2.5 -> "2,5").</summary>
    private static string Cantidad(decimal valor) => valor.ToString("0.##", Cultura);

    /// <summary>Hora legible HH:mm (un tramo de día completo termina en "24:00").</summary>
    private static string Hora(TimeSpan hora) => hora >= TimeSpan.FromDays(1) ? "24:00" : $"{hora:hh\\:mm}";

    /// <summary>Franja del tramo en texto: "Entre 08:35 y 12:05" o "Durante todo el día" si cubre el día completo.</summary>
    private static string Franja(DemandSegment tramo) =>
        tramo.Start == TimeSpan.Zero && tramo.End >= TimeSpan.FromDays(1)
            ? "Durante todo el día"
            : $"Entre {Hora(tramo.Start)} y {Hora(tramo.End)}";
}
