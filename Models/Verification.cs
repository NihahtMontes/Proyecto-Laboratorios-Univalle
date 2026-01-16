using Proyecto_Laboratorios_Univalle.Models.Enums;
using Proyecto_Laboratorios_Univalle.Models.Interfaces;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Proyecto_Laboratorios_Univalle.Models
{
    /// <summary>
    /// Technical verification checklist for laboratory equipment
    /// </summary>
    public class Verification : IAuditable
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [Display(Name = "Equipamiento")]
        public int EquipmentId { get; set; }

        // ========================================
        // CHECKLIST ITEMS (VerificationResult: Good, Bad, NA)
        // ========================================

        [Display(Name = "Verificación del cableado y conexiones de cables")]
        public VerificationResult CablingCheck { get; set; } = VerificationResult.NotChecked;

        [Display(Name = "Verificación de estado de la manguera del gas (GN / GLP) y conexiones")]
        public VerificationResult GasHoseCheck { get; set; } = VerificationResult.NotChecked;

        [Display(Name = "Verificación de manguera de agua y conexiones")]
        public VerificationResult WaterHoseCheck { get; set; } = VerificationResult.NotChecked;

        [Display(Name = "Verificación del quemador y compartimiento del quemador")]
        public VerificationResult BurnerCheck { get; set; } = VerificationResult.NotChecked;

        [Display(Name = "Inspección visual del intercambiador de calor")]
        public VerificationResult HeatExchangerCheck { get; set; } = VerificationResult.NotChecked;

        [Display(Name = "Verificación del sensor de llama")]
        public VerificationResult FlameSensorCheck { get; set; } = VerificationResult.NotChecked;

        [Display(Name = "Revisión del encendedor electrodo de chispa piloto")]
        public VerificationResult ElectrodeIgniterCheck { get; set; } = VerificationResult.NotChecked;

        [Display(Name = "Verificación del ventilador en ambos sentidos")]
        public VerificationResult FanCheck { get; set; } = VerificationResult.NotChecked;

        [Display(Name = "Verificación de llama de combustión en quemador")]
        public VerificationResult CombustionFlameCheck { get; set; } = VerificationResult.NotChecked;

        [Display(Name = "Verificación de lubricación de partes móviles")]
        public VerificationResult LubricationCheck { get; set; } = VerificationResult.NotChecked;

        [Display(Name = "Inspección del encendido del horno, control del encendido y llama")]
        public VerificationResult OvenIgnitionCheck { get; set; } = VerificationResult.NotChecked;

        [Display(Name = "Doble verificación del aumento de temperatura y control")]
        public VerificationResult TemperatureControlCheck { get; set; } = VerificationResult.NotChecked;

        [Display(Name = "Verificación de limpieza interna del horno")]
        public VerificationResult InternalCleaningCheck { get; set; } = VerificationResult.NotChecked;

        [Display(Name = "Verificación de limpieza externa del horno")]
        public VerificationResult ExternalCleaningCheck { get; set; } = VerificationResult.NotChecked;

        [Display(Name = "Verificación de luminarias")]
        public VerificationResult LightsCheck { get; set; } = VerificationResult.NotChecked;

        [Display(Name = "Verificación del vapor en alta temperatura")]
        public VerificationResult HighTempSteamCheck { get; set; } = VerificationResult.NotChecked;

        [Display(Name = "Verificación de pantalla LED Controlador")]
        public VerificationResult LedDisplayCheck { get; set; } = VerificationResult.NotChecked;

        [Display(Name = "Verificación del ajuste a Electroválvula")]
        public VerificationResult SolenoidValveCheck { get; set; } = VerificationResult.NotChecked;

        [Display(Name = "Verificación de alarmas sonoras, buzzer")]
        public VerificationResult SoundAlarmCheck { get; set; } = VerificationResult.NotChecked;

        [Display(Name = "Verificación de posicionamiento de Termocupla")]
        public VerificationResult ThermocoupleCheck { get; set; } = VerificationResult.NotChecked;

        [Display(Name = "Verificación de salida de vapor")]
        public VerificationResult SteamOutletCheck { get; set; } = VerificationResult.NotChecked;

        // ========================================
        // METADATA
        // ========================================

        [Required]
        [Display(Name = "Fecha de Verificación")]
        [DataType(DataType.Date)]
        public DateTime Date { get; set; } = DateTime.Now;

        [StringLength(2000)]
        [Display(Name = "Observaciones Generales")]
        public string? Observations { get; set; }

        [StringLength(1000)]
        [Display(Name = "Hallazgos Críticos")]
        public string? CriticalFindings { get; set; }

        [StringLength(1000)]
        [Display(Name = "Recomendaciones")]
        public string? Recommendations { get; set; }

        [Required]
        [Display(Name = "Estado de la Verificación")]
        public VerificationStatus Status { get; set; } = VerificationStatus.Draft;

        // ========================================
        // AUDIT
        // ========================================
        [Display(Name = "Creado Por")]
        public int? CreatedById { get; set; }

        [Display(Name = "Fecha de Creación")]
        public DateTime CreatedDate { get; set; } = DateTime.Now;

        [Display(Name = "Modificado Por")]
        public int? ModifiedById { get; set; }

        [Display(Name = "Última Modificación")]
        public DateTime? LastModifiedDate { get; set; }

        // ========================================
        // NAVIGATION
        // ========================================
        [ForeignKey("EquipmentId")]
        public virtual Equipment? Equipment { get; set; }

        [ForeignKey("CreatedById")]
        public virtual User? CreatedBy { get; set; }

        [ForeignKey("ModifiedById")]
        public virtual User? ModifiedBy { get; set; }

        // ========================================
        // CALCULATED PROPERTIES
        // ========================================

        [NotMapped]
        [Display(Name = "Porcentaje Completado")]
        public int CompletionPercentage
        {
            get
            {
                var properties = this.GetType().GetProperties()
                    .Where(p => p.PropertyType == typeof(VerificationResult) && p.Name.EndsWith("Check"))
                    .ToList();

                int totalItems = properties.Count;
                if (totalItems == 0) return 0;

                int completed = properties.Count(p => (VerificationResult)p.GetValue(this)! != VerificationResult.NotChecked);

                return (int)((completed / (double)totalItems) * 100);
            }
        }
    }
}
