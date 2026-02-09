using Microsoft.AspNetCore.Identity;
using Proyecto_Laboratorios_Univalle.Models;
using Proyecto_Laboratorios_Univalle.Models.Enums;
using Proyecto_Laboratorios_Univalle.Helpers;
using Microsoft.EntityFrameworkCore;

namespace Proyecto_Laboratorios_Univalle.Data
{
    public static class DbInitializer
    {
        public static async Task SeedAsync(IServiceProvider serviceProvider)
        {
            var roleManager = serviceProvider.GetRequiredService<RoleManager<IdentityRole<int>>>();
            var userManager = serviceProvider.GetRequiredService<UserManager<User>>();
            var context = serviceProvider.GetRequiredService<ApplicationDbContext>();

            // 1. Asegurar que la base de datos esté migrada
            await context.Database.MigrateAsync();

            // 2. Crear Roles si no existen
            string[] roles = { AuthorizationHelper.RoleSupervisor, AuthorizationHelper.RoleAdministrator, AuthorizationHelper.RoleSuperAdmin };

            foreach (var roleName in roles)
            {
                if (!await roleManager.RoleExistsAsync(roleName))
                {
                    await roleManager.CreateAsync(new IdentityRole<int>(roleName));
                }
            }

            // 3. Crear Usuario Administrador por defecto
            var adminEmail = "admin@univalle.edu";
            var adminUserName = "admin";
            
            var adminUser = await userManager.FindByNameAsync(adminUserName);

            if (adminUser == null)
            {
                adminUser = new User
                {
                    UserName = adminUserName,
                    Email = adminEmail,
                    EmailConfirmed = true,
                    FirstName = "Administrador",
                    LastName = "Sistema",
                    IdentityCard = "0000000",
                    PhoneNumber = "00000000",
                    Role = UserRole.SuperAdmin,
                    Status = GeneralStatus.Activo,
                    CreatedDate = DateTime.Now
                };

                var result = await userManager.CreateAsync(adminUser, "admin123");
                if (result.Succeeded)
                {
                    await userManager.AddToRoleAsync(adminUser, AuthorizationHelper.RoleSuperAdmin);
                    Console.WriteLine(">>> SEMILLA: Usuario 'admin' creado exitosamente. <<<");
                }
                else
                {
                    var errors = string.Join(", ", result.Errors.Select(e => e.Description));
                    Console.WriteLine($">>> ERROR AL CREAR USUARIO ADMÍN: {errors} <<<");
                }
            }
            else
            {
                // Asegurar que tenga el rol y la contraseña correcta por si acaso
                if (!await userManager.IsInRoleAsync(adminUser, AuthorizationHelper.RoleSuperAdmin))
                {
                    await userManager.AddToRoleAsync(adminUser, AuthorizationHelper.RoleSuperAdmin);
                }
                
                // Opcional: Forzar contraseña por si el usuario la cambió o falló el seed previo
                var token = await userManager.GeneratePasswordResetTokenAsync(adminUser);
                await userManager.ResetPasswordAsync(adminUser, token, "admin123");
                
                Console.WriteLine(">>> SEMILLA: Usuario 'admin' verificado y actualizado. <<<");
            }
        }
    }
}
