using System.Text.Json;
using Microsoft.AspNetCore.Http;
using Proyecto_Laboratorios_Univalle.Models;

namespace Proyecto_Laboratorios_Univalle.Services;

/// <summary>
/// Persistencia temporal por navegador para el MVP de planificación.
/// Se mantiene aislada de EF hasta que Datos apruebe el modelo académico.
/// </summary>
public sealed class LaboratoryPlanningDemoService
{
    private const string SessionKey = "LaboratoryOperations.DemoPlans";
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);
    private readonly IHttpContextAccessor _httpContextAccessor;

    public LaboratoryPlanningDemoService(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    public List<LaboratoryPlanningDemo> GetPlans()
    {
        var value = Session.GetString(SessionKey);
        if (string.IsNullOrWhiteSpace(value))
            return [];

        try
        {
            return JsonSerializer.Deserialize<List<LaboratoryPlanningDemo>>(value, JsonOptions) ?? [];
        }
        catch (JsonException)
        {
            Session.Remove(SessionKey);
            return [];
        }
    }

    public LaboratoryPlanningDemo? Find(string? id)
    {
        if (string.IsNullOrWhiteSpace(id))
            return null;

        return GetPlans().FirstOrDefault(plan => string.Equals(plan.Id, id, StringComparison.Ordinal));
    }

    public void Save(LaboratoryPlanningDemo plan)
    {
        var plans = GetPlans();
        var index = plans.FindIndex(item => string.Equals(item.Id, plan.Id, StringComparison.Ordinal));
        plan.UpdatedAt = DateTime.UtcNow;

        if (index < 0)
            plans.Add(plan);
        else
            plans[index] = plan;

        SaveAll(plans);
    }

    public void SaveAll(IEnumerable<LaboratoryPlanningDemo> plans)
    {
        var json = JsonSerializer.Serialize(plans.ToList(), JsonOptions);
        Session.SetString(SessionKey, json);
    }

    private ISession Session => _httpContextAccessor.HttpContext?.Session
        ?? throw new InvalidOperationException("La sesión HTTP no está disponible para el MVP de planificación.");
}
