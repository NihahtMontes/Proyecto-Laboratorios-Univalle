using Microsoft.AspNetCore.Mvc.Rendering;
using System.ComponentModel.DataAnnotations;
using System.Reflection;

namespace Proyecto_Laboratorios_Univalle.Helpers
{
    public static class EnumHelper
    {
        // El método mágico que convierte cualquier Enum en una lista para el Select
        public static List<SelectListItem> ToSelectList<TEnum>() where TEnum : struct, Enum
        {
            return Enum.GetValues<TEnum>().Select(e => new SelectListItem
            {
                Value = Convert.ToInt32(e).ToString(),
                Text = GetDisplayName(e)
            }).ToList();
        }

        // Nuevo método: Obtiene la lista pero FILTRA los estados "Eliminado" o "Deleted"
        public static List<SelectListItem> GetStatusSelectList<TEnum>() where TEnum : struct, Enum
        {
            return Enum.GetValues<TEnum>()
                .Where(e => !IsDeletedStatus(e)) // Filtramos los borrados
                .Select(e => new SelectListItem
                {
                    Value = Convert.ToInt32(e).ToString(),
                    Text = GetDisplayName(e)
                }).ToList();
        }

        /// <summary>
        /// Lista para formularios de alta/edición: excluye "Eliminado" y "Por confirmar".
        /// "Por confirmar" solo aparece si el registro editado ya lo tiene (para no cambiarlo sin que el usuario lo decida).
        /// Los filtros de listados deben usar GetStatusSelectList (que sí incluye "Por confirmar").
        /// </summary>
        public static List<SelectListItem> FormSelectList<TEnum>(TEnum? current = null) where TEnum : struct, Enum
        {
            return Enum.GetValues<TEnum>()
                .Where(e => !IsDeletedStatus(e) && (!IsPendingValue(e) || (current.HasValue && current.Value.Equals(e))))
                .Select(e => new SelectListItem
                {
                    Value = Convert.ToInt32(e).ToString(),
                    Text = GetDisplayName(e),
                    Selected = current.HasValue && current.Value.Equals(e)
                }).ToList();
        }

        /// <summary>Valor "Por confirmar" de datos históricos importados (convención de nombre PorConfirmar).</summary>
        public static bool IsPendingValue(Enum value) =>
            value.ToString().Equals("PorConfirmar", StringComparison.Ordinal);

        private static bool IsDeletedStatus(Enum value)
        {
            // Convención: GeneralStatus.Eliminado = 2, EquipmentStatus.Deleted = 99
            var intValue = Convert.ToInt32(value);
            var name = value.ToString();

            // Filtramos por convención de nombre o valores conocidos
            return name.Equals("Eliminado", StringComparison.OrdinalIgnoreCase) ||
                   name.Equals("Deleted", StringComparison.OrdinalIgnoreCase) ||
                   intValue == 99; // EquipmentStatus.Deleted
        }

        // Esta función busca si le pusiste un [Display(Name="...")] al Enum
        public static string GetDisplayName(Enum value)
        {
            var field = value.GetType().GetField(value.ToString());
            var attribute = field?.GetCustomAttribute<DisplayAttribute>();

            // Si tiene el atributo usa el nombre bonito, si no, usa el nombre de la variable
            return attribute?.Name ?? value.ToString();
        }
    }
}
