namespace Proyecto_Laboratorios_Univalle.Models;

/// <summary>
/// Estado temporal del MVP de planificación académica.
/// No es una entidad EF: se usa únicamente para la demostración local mientras
/// se aprueba el contrato definitivo de datos académicos.
/// </summary>
public sealed class LaboratoryPlanningDemo
{
    public string Id { get; set; } = Guid.NewGuid().ToString("N");
    public string Code { get; set; } = string.Empty;
    public int FacultyId { get; set; }
    public int CareerId { get; set; }
    public int LaboratoryId { get; set; }
    public string ResponsibleName { get; set; } = string.Empty;
    public string Subject { get; set; } = string.Empty;
    public string Group { get; set; } = string.Empty;
    public DateTime PracticeDate { get; set; } = DateTime.Today;
    public TimeSpan StartTime { get; set; } = new(8, 0, 0);
    public TimeSpan EndTime { get; set; } = new(11, 0, 0);
    public string MaterialCategory { get; set; } = "Gastronomía (Utensilios)";
    public int PracticeNumber { get; set; } = 1;
    public string? Observations { get; set; }
    public LaboratoryPlanningDemoStatus Status { get; set; } = LaboratoryPlanningDemoStatus.Draft;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    public List<LaboratoryPlanningDemoRequirement> Requirements { get; set; } = [];
    public List<LaboratoryPlanningDemoReturn> Returns { get; set; } = [];
    public LaboratoryPlanningDemoIncident? Incident { get; set; }
}

public enum LaboratoryPlanningDemoStatus
{
    Draft = 0,
    PendingAvailability = 1,
    Approved = 2,
    InProgress = 3,
    Completed = 4,
    Incident = 5,
    Cancelled = 6
}

public sealed class LaboratoryPlanningDemoRequirement
{
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Unit { get; set; } = "Unid.";
    public string Category { get; set; } = string.Empty;
    public int RequestedQuantity { get; set; }
    public int AvailableQuantity { get; set; }
}

public sealed class LaboratoryPlanningDemoReturn
{
    public string RequirementCode { get; set; } = string.Empty;
    public int ReturnedQuantity { get; set; }
}

public sealed class LaboratoryPlanningDemoIncident
{
    public string Code { get; set; } = string.Empty;
    public string Type { get; set; } = "Daño";
    public string? AffectedItem { get; set; }
    public int Quantity { get; set; } = 1;
    public string Observations { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
