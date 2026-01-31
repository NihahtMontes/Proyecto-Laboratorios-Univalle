using Proyecto_Laboratorios_Univalle.Models.Enums;

namespace Proyecto_Laboratorios_Univalle.Helpers
{
    /// <summary>
    /// Helper class for authorization attributes based on user roles
    /// </summary>
    public static class AuthorizationHelper
    {
        // Role names as strings for use in [Authorize] attributes
        public const string RoleSupervisor = "Supervisor";
        public const string RoleAdministrator = "Administrator";
        public const string RoleSuperAdmin = "SuperAdmin";

        // Combined role strings for common scenarios
        public const string AdminRoles = "Administrator,SuperAdmin";
        public const string ManagementRoles = "Supervisor,Administrator,SuperAdmin";
        public const string AllRoles = "Supervisor,Administrator,SuperAdmin";

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
            return role == UserRole.Supervisor || IsAdmin(role);
        }
    }
}
