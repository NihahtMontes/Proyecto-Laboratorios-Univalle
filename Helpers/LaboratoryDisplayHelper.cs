using Proyecto_Laboratorios_Univalle.Models;

namespace Proyecto_Laboratorios_Univalle.Helpers
{
    public static class LaboratoryDisplayHelper
    {
        public static string Format(Laboratory? laboratory)
        {
            if (laboratory == null)
            {
                return "N/A";
            }

            return string.IsNullOrWhiteSpace(laboratory.Code)
                ? laboratory.Name
                : $"{laboratory.Code} - {laboratory.Name}";
        }

        public static IEnumerable<object> ToSelectItems(IEnumerable<Laboratory> laboratories)
        {
            return laboratories.Select(l => new
            {
                l.Id,
                DisplayName = Format(l)
            });
        }
    }
}
