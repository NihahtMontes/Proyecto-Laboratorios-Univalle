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

            public const string CountryNameDuplicate = "El país ya existe en el sistema. Ponte viooooooooooooooo";
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

      
        
    }
} 