using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Proyecto_Laboratorios_Univalle.Models
{
    /// <summary>Periodo académico de 50 minutos (catálogo fijo, igual de lunes a sábado).</summary>
    public class ClassPeriod
    {
        [Key]
        public int Id { get; set; }

        /// <summary>Orden del periodo dentro de la jornada (1 a 12).</summary>
        [Range(1, 12)]
        public int Order { get; set; }

        public TimeSpan StartTime { get; set; }

        public TimeSpan EndTime { get; set; }

        /// <summary>Etiqueta visible del periodo (ej. "07:35 – 08:25").</summary>
        [NotMapped]
        public string Label => $"{StartTime:hh\\:mm} – {EndTime:hh\\:mm}";
    }
}
