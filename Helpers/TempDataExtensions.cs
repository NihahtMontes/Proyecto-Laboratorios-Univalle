using Microsoft.AspNetCore.Mvc.ViewFeatures;

namespace Proyecto_Laboratorios_Univalle.Helpers
{
    public static class TempDataExtensions
    {
        public static void Success(this ITempDataDictionary tempData, string message)
        {
            tempData["SuccessMessage"] = message;
        }

        public static void Error(this ITempDataDictionary tempData, string message)
        {
            tempData["ErrorMessage"] = message;
        }

        public static void Warning(this ITempDataDictionary tempData, string message)
        {
            tempData["WarningMessage"] = message;
        }
    }
}