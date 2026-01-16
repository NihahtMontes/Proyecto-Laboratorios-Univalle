using Proyecto_Laboratorios_Univalle.Models.Enums;

namespace Proyecto_Laboratorios_Univalle.Helpers
{
    /// <summary>
    /// Helper class for authorization attributes based on user roles
    /// </summary>
    public static class AuthorizationHelper
    {
        // Role names as strings for use in [Authorize] attributes
        public const string RoleTechnician = "Technician";
        public const string RoleSupervisor = "Supervisor";
        public const string RoleDirector = "Director";
        public const string RoleEngineer = "Engineer";
        public const string RoleAdministrator = "Administrator";
        public const string RoleSuperAdmin = "SuperAdmin";

        // Combined role strings for common scenarios
        public const string AdminRoles = "Administrator,SuperAdmin";
        public const string ManagementRoles = "Director,Administrator,SuperAdmin";
        public const string TechnicalRoles = "Technician,Engineer,Supervisor";
        public const string AllRoles = "Technician,Supervisor,Director,Engineer,Administrator,SuperAdmin";

        /// <summary>
        /// Checks if a user role has administrative privileges
        /// </summary>
        public static bool IsAdmin(UserRole role)
        {
            return role == UserRole.Administrador || role == UserRole.SuperAdmin;
        }

        /// <summary>
        /// Checks if a user role has management privileges
        /// </summary>
        public static bool IsManagement(UserRole role)
        {
            return role == UserRole.Director || IsAdmin(role);
        }

        /// <summary>
        /// Checks if a user role has technical privileges
        /// </summary>
        public static bool IsTechnical(UserRole role)
        {
            return role == UserRole.Tecnico ||
                   role == UserRole.Ingeniero ||
                   role == UserRole.Supervisor;
        }
    }
}
