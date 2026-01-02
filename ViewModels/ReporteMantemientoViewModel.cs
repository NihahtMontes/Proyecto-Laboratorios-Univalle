namespace Proyecto_Laboratorios_Univalle.ViewModels
{
    // En una carpeta /ViewModels (NO en Models)
    public class ReporteMantenimientoViewModel
    {
        public int IdMantenimiento { get; set; }
        public string NombreEquipo { get; set; }
        public string TecnicoNombre { get; set; }
        public DateTime FechaServicio { get; set; }
        public decimal CostoTotal { get; set; }
        public string Descripcion { get; set; }
        // ... todo lo que necesites para el PDF
    }
}
