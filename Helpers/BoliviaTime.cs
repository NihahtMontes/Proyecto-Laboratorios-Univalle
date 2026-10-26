namespace Proyecto_Laboratorios_Univalle.Helpers
{
    /// <summary>
    /// Conversión entre la hora local de Bolivia (UTC-4, sin horario de verano) y UTC.
    /// El historial de estados se guarda en UTC; los formularios capturan hora local.
    /// </summary>
    public static class BoliviaTime
    {
        private static readonly TimeSpan Offset = TimeSpan.FromHours(-4);

        /// <summary>Fecha/hora local escrita por el usuario → UTC.</summary>
        public static DateTime ToUtc(DateTime local) =>
            local.Kind == DateTimeKind.Utc ? local : DateTime.SpecifyKind(local - Offset, DateTimeKind.Utc);

        /// <summary>Fecha/hora guardada en UTC → hora local de Bolivia para mostrar.</summary>
        public static DateTime ToLocal(DateTime utc) =>
            DateTime.SpecifyKind(utc + Offset, DateTimeKind.Unspecified);
    }
}
