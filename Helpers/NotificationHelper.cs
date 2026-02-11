namespace Proyecto_Laboratorios_Univalle.Helpers
{
    public static class NotificationHelper
    {
        public static class Users
        {
            // CRUD

            public static string Created(string fullName) =>
               $"El usuario '{fullName}' ha sido registrado exitosamente.";

            public static string Updated(string fullName) =>
                $"El usuario '{fullName}' ha sido actualizado correctamente.";

            public static string Deleted(string fullName) =>
                $"El usuario '{fullName}' ha sido eliminado.";

            public static string Edited(string fullName) =>
               $"El usuario '{fullName}' ha sido editado correctamente..";


            // Validations

            public const string UserCIDuplicate = "El CI ya se encuentra registrado por otro usuario activo.";
            public const string UserEmailDuplicate = "El correo electrónico ya está en uso por otro usuario activo.";
            public const string UserNameDuplicate = "El nombre de usuario ya está en uso por otro usuario activo.";
        }
        
        public static class Countries
        {
            // CRUD
            public static string Created(string name) =>
              $"El país '{name}' ha sido registrado exitosamente.";

            public static string Updated(string name) =>
                $"El país '{name}' ha sido actualizado correctamente.";

            public static string Deleted(string name) =>
                $"El país '{name}' ha sido eliminado.";

            // Validations
            public const string CountryNameDuplicate = "El país ya existe en el sistema.";
            public const string InvalidFormat = "El nombre contiene caracteres no permitidos. Solo se aceptan letras, espacios y guiones.";
        }

        public static class Cities 
        {
            // CRUD
            public static string Created(string name, string country) =>
                $"La ciudad '{name}' ({country}) ha sido registrada exitosamente.";

            public static string Updated(string name) =>
                $"La ciudad '{name}' ha sido actualizada correctamente.";

            public static string Deleted(string name) =>
                $"La ciudad '{name}' ha sido eliminada.";

            // Validations
            public const string CityNameDuplicate = "La ciudad ya existe en este país.";
        }

        public static class Faculties
        {
            // CRUD
            public static string Created(string name) =>
                $"La facultad '{name}' ha sido registrada exitosamente.";

            public static string Updated(string name) =>
                $"La facultad '{name}' ha sido actualizado correctamente.";

            public static string Deleted(string name) =>
                $"La facultad '{name}' ha sido eliminada.";

            // Validations
            public const string FacultyNameDuplicate = "Ya existe una facultad con ese nombre.";
        }

        public static class Laboratories
        {
            // CRUD
            public static string Created(string name) =>
                $"El laboratorio '{name}' ha sido registrado exitosamente.";

            public static string Updated(string name) =>
                $"El laboratorio '{name}' ha sido actualizado correctamente.";

            public static string Deleted(string name) =>
                $"El laboratorio '{name}' ha sido eliminado.";

            // Validations
            public const string LabNameDuplicate = "Ya existe un laboratorio con ese nombre en esta facultad.";
            public const string LabCodeDuplicate = "El código de laboratorio ya está en uso.";
        }

        public static class Maintenances
        {
            // CRUD Operations
            public static string Created(string? equipmentName) =>
                $"Mantenimiento programado exitosamente para '{equipmentName ?? "el equipo"}'.";

            public static string Updated(string? equipmentName) =>
                $"Registro de mantenimiento de '{equipmentName ?? "el equipo"}' actualizado correctamente.";

            public static string Deleted(string? equipmentName) =>
                $"Mantenimiento de '{equipmentName ?? "el equipo"}' eliminado del sistema.";

            // Business Validations
            public const string CompletedWithoutCosts =
                "Un mantenimiento completado debe tener detalles de costos registrados (repuestos o mano de obra).";

            public const string EndDateBeforeStart =
                "La fecha de finalización no puede ser anterior a la fecha de inicio.";

            public const string EquipmentRequired =
                "Debe seleccionar un equipo para asignar el mantenimiento.";

            public const string MaintenanceTypeRequired =
                "Debe seleccionar un tipo de mantenimiento.";

            public const string InvalidDateRange =
                "El rango de fechas no es válido. Verifique que las fechas sean coherentes.";

            // General
            public static string SaveError(string detail) =>
                $"Ocurrió un error al guardar el mantenimiento: {detail}";
        }

        public static class Requests
        {
            // CRUD
            public static string Created(string equipmentName) =>
                $"Solicitud de servicio generada exitosamente para '{equipmentName}'.";

            public static string Updated(int id) =>
                $"La solicitud #{id} ha sido actualizada correctamente.";

            public static string Deleted(int id) =>
                $"Solicitud #{id} eliminada del sistema.";

            public static string StatusChanged(int id, string status) =>
                $"El estado de la solicitud #{id} cambió a: {status}.";
            
            public static string SaveError(string detail) =>
                $"Error al guardar la solicitud: {detail}";

            // Validations
            public const string EquipmentRequired = "Debe seleccionar un equipo para generar la solicitud.";
            public const string DescriptionRequired = "La descripción del problema es obligatoria.";
            public const string DeleteRestricted = "No se puede eliminar una solicitud que ya tiene un mantenimiento en curso o finalizado.";
        }

        public static class Verifications
        {
            // CRUD
            public static string Created(string equipmentName) =>
                $"Lista de verificación registrada correctamente para el equipo '{equipmentName}'.";

            public static string Updated(int id) =>
                $"La verificación #{id} ha sido actualizada exitosamente.";

            public static string Deleted(int id) =>
                $"Verificación #{id} eliminada del sistema.";

            // Validations
            public const string EquipmentRequired = "Debe seleccionar un equipo para realizar la verificación.";
            public const string IncompleteChecklist = "La lista de verificación parece incompleta. Por favor revise los ítems marcados.";
            public const string CriticalFindingsWarning = "Al reportar hallazgos críticos, se recomienda cambiar el estado a 'Requiere Atención'.";
        }
    }
}