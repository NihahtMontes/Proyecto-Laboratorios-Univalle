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
        // Esta función busca si le pusiste un [Display(Name="...")] al Enum
        private static string GetDisplayName(Enum value)
        {
            var field = value.GetType().GetField(value.ToString());
            var attribute = field?.GetCustomAttribute<DisplayAttribute>();

            // Si tiene el atributo usa el nombre bonito, si no, usa el nombre de la variable
            return attribute?.Name ?? value.ToString();
        }
    }
}
