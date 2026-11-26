using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Proyecto_Laboratorios_Univalle.Models
{
    /// <summary>
    /// Asignación del revisor al aprobar (V13: SolicitudL3Asignaciones): de qué laboratorio o almacén sale cada línea
    /// pedida y en qué cantidad. Una línea puede repartirse entre varios orígenes; la salida L-3 se arma con ellas.
    /// </summary>
    public class PlanningAssignment
    {
        public int Id { get; set; }

        /// <summary>Línea pedida (artículo y cantidad de la planificación).</summary>
        public int PlanningRequirementId { get; set; }
        public PlanningRequirement? PlanningRequirement { get; set; }

        /// <summary>Origen del material: laboratorio o almacén (Economato).</summary>
        public int LaboratoryId { get; set; }
        public Laboratory? Laboratory { get; set; }

        /// <summary>Cantidad que sale de este origen (mayor que 0).</summary>
        [Column(TypeName = "decimal(12,2)")]
        [Range(0.01, 999999)]
        public decimal Quantity { get; set; }
    }
}
