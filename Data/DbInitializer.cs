using Microsoft.AspNetCore.Identity;
using Proyecto_Laboratorios_Univalle.Models;
using Proyecto_Laboratorios_Univalle.Models.Enums;
using Proyecto_Laboratorios_Univalle.Helpers;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace Proyecto_Laboratorios_Univalle.Data
{
    public static class DbInitializer
    {
        public static async Task SeedAsync(IServiceProvider serviceProvider)
        {
            var roleManager = serviceProvider.GetRequiredService<RoleManager<IdentityRole<int>>>();
            var userManager = serviceProvider.GetRequiredService<UserManager<User>>();
            var context = serviceProvider.GetRequiredService<ApplicationDbContext>();

            // 1. Crear Roles si no existen
            string[] roles = { AuthorizationHelper.RoleSupervisor, AuthorizationHelper.RoleAdministrator, AuthorizationHelper.RoleSuperAdmin };

            foreach (var roleName in roles)
            {
                if (!await roleManager.RoleExistsAsync( roleName))
                {
                    await roleManager.CreateAsync(new IdentityRole<int>(roleName));
                }
            }

            // 2. Crear Usuarios (Administrador y Técnicos/Jefes de Lab)
            var adminUser = await EnsureUserAsync(userManager, "admin", "admin@univalle.edu", "Administrador", "Sistema", AuthorizationHelper.RoleSuperAdmin);
            var labJefe1 = await EnsureUserAsync(userManager, "jlab.cocina", "cocina@univalle.edu", "Juan", "Perez", AuthorizationHelper.RoleAdministrator);
            var labJefe2 = await EnsureUserAsync(userManager, "jlab.quimica", "quimica@univalle.edu", "Maria", "Gomez", AuthorizationHelper.RoleAdministrator);

            // 3. Crear Técnicos (Personas) -> Internos y Externos
            if (!context.People.Any())
            {
                var tecInterno1 = new Intern { Name = "Carlos Técnico Interno", Category = PersonCategory.Tecnico, InternStatus = GeneralStatus.Activo, Email = "carlos@univalle.edu" };
                var tecInterno2 = new Intern { Name = "Luis Técnico Interno", Category = PersonCategory.Tecnico, InternStatus = GeneralStatus.Activo, Email = "luis@univalle.edu" };
                var tecExterno1 = new Extern { Name = "Soporte Especializado S.R.L.", Category = PersonCategory.Externo, ExternStatus = GeneralStatus.Activo, IsEntity = true, Address = "Calle Falsa 123" };
                var tecExterno2 = new Extern { Name = "Miguel Externo Independiente", Category = PersonCategory.Externo, ExternStatus = GeneralStatus.Activo, IsEntity = false, Address = "Av. Siempre Viva" };

                context.Interns.AddRange(tecInterno1, tecInterno2);
                context.Externs.AddRange(tecExterno1, tecExterno2);
                await context.SaveChangesAsync();
            }

            var tInt1 = await context.Interns.FirstOrDefaultAsync(x => x.Name.Contains("Carlos"));
            var tExt1 = await context.Externs.FirstOrDefaultAsync(x => x.Name.Contains("Soporte"));

            // 4. Jerarquía: Países, Ciudades, Facultades, Carreras, Laboratorios
            if (!context.Countries.Any())
            {
                var bolivia = new Country { Name = "Bolivia", Status = GeneralStatus.Activo };
                context.Countries.Add(bolivia);
                await context.SaveChangesAsync();

                var cbba = new City { Name = "Cochabamba", CountryId = bolivia.Id, Status = GeneralStatus.Activo };
                context.Cities.Add(cbba);
                await context.SaveChangesAsync();

                var facTec = new Faculty { Name = "Facultad de Tecnología", Code = "TEC", Status = GeneralStatus.Activo };
                var facSalud = new Faculty { Name = "Facultad de Ciencias de la Salud", Code = "SALUD", Status = GeneralStatus.Activo };
                context.Faculties.AddRange(facTec, facSalud);
                await context.SaveChangesAsync();

                var carGas = new Career { Name = "Gastronomía", FacultadId = facTec.Id, Status = GeneralStatus.Activo };
                var carBio = new Career { Name = "Bioquímica", FacultadId = facSalud.Id, Status = GeneralStatus.Activo };
                context.Careers.AddRange(carGas, carBio);
                await context.SaveChangesAsync();

                var labCocina = new Laboratory { Name = "Cocina Caliente", Code = "LAB-GAS-01", FacultyId = facTec.Id, CityId = cbba.Id, Status = GeneralStatus.Activo };
                var labQuimica = new Laboratory { Name = "Laboratorio Químico Analítico", Code = "LAB-BIO-01", FacultyId = facSalud.Id, CityId = cbba.Id, Status = GeneralStatus.Activo };
                context.Laboratories.AddRange(labCocina, labQuimica);
                await context.SaveChangesAsync();
            }

            var lCocina = await context.Laboratories.FirstOrDefaultAsync(x => x.Code == "LAB-GAS-01");
            var lQuimica = await context.Laboratories.FirstOrDefaultAsync(x => x.Code == "LAB-BIO-01");
            var boliviaEntity = await context.Countries.FirstOrDefaultAsync();
            var cbbaEntity = await context.Cities.FirstOrDefaultAsync();

            // 5. Equipos Catalogo
            if (!context.Equipments.Any())
            {
                var eqBatidora = new Equipment { Name = "Batidora Industrial", Category = EquipmentCategory.Equipment, TypeClassification = EquipmentTypeClassification.Manual, UtensilType = UtensilType.NoAplica, CountryId = boliviaEntity.Id, CityId = cbbaEntity.Id };
                var eqMicro = new Equipment { Name = "Microondas Industrial", Category = EquipmentCategory.Equipment, TypeClassification = EquipmentTypeClassification.Electronico, UtensilType = UtensilType.NoAplica, CountryId = boliviaEntity.Id, CityId = cbbaEntity.Id };
                var eqHorno = new Equipment { Name = "Horno Rational", Category = EquipmentCategory.Equipment, TypeClassification = EquipmentTypeClassification.Electronico, UtensilType = UtensilType.NoAplica, CountryId = boliviaEntity.Id, CityId = cbbaEntity.Id };
                
                var eqMicroscopio = new Equipment { Name = "Microscopio Binocular", Category = EquipmentCategory.Equipment, TypeClassification = EquipmentTypeClassification.Medicion, UtensilType = UtensilType.NoAplica, CountryId = boliviaEntity.Id, CityId = cbbaEntity.Id };
                var eqEspectro = new Equipment { Name = "Espectrofotómetro UV-Vis", Category = EquipmentCategory.Equipment, TypeClassification = EquipmentTypeClassification.Electronico, UtensilType = UtensilType.NoAplica, CountryId = boliviaEntity.Id, CityId = cbbaEntity.Id };

                context.Equipments.AddRange(eqBatidora, eqMicro, eqHorno, eqMicroscopio, eqEspectro);
                await context.SaveChangesAsync();
            }

            var eBat = await context.Equipments.FirstOrDefaultAsync(x => x.Name.Contains("Batidora"));
            var eMic = await context.Equipments.FirstOrDefaultAsync(x => x.Name.Contains("Microondas"));
            var eHorno = await context.Equipments.FirstOrDefaultAsync(x => x.Name.Contains("Horno Rational"));
            var eMicroscopio = await context.Equipments.FirstOrDefaultAsync(x => x.Name.Contains("Microscopio"));
            var eEsp = await context.Equipments.FirstOrDefaultAsync(x => x.Name.Contains("Espectro"));

            // 6. Gestión Principal 1-2026
            var management = await context.Managements.FirstOrDefaultAsync(m => m.Year == 2026 && m.Semester == 1);
            if (management == null)
            {
                management = new Management
                {
                    Year = 2026,
                    Semester = 1,
                    Code = "1-2026",
                    Description = "Gestión Académica I-2026",
                    Status = ManagementStatus.Active,
                    StartDate = new DateTime(2026, 6, 1),
                    PlannedEndDate = new DateTime(2026, 7, 31)
                };
                context.Managements.Add(management);
                await context.SaveChangesAsync();
            }

            // 6b. Gestión Correctiva 2026 (contenedor de fallas)
            var correctiveMgmt = await context.Managements.FirstOrDefaultAsync(m => m.Type == ManagementType.Corrective && m.Year == 2026);
            if (correctiveMgmt == null)
            {
                correctiveMgmt = new Management
                {
                    Year = 2026,
                    Semester = 0,
                    Code = "CORR-2026",
                    Description = "Contenedor automático de fallas correctivas.",
                    Status = ManagementStatus.Active,
                    Type = ManagementType.Corrective
                };
                context.Managements.Add(correctiveMgmt);
                await context.SaveChangesAsync();
            }

            // 7. EquipmentUnits y Escenarios del Wizard
            if (!context.EquipmentUnits.Any())
            {
                // A) No verificados (Solo existen)
                var eu1 = new EquipmentUnit { EquipmentId = eBat.Id, LaboratoryId = lCocina.Id, InventoryNumber = "INV-GAS-001", CurrentStatus = EquipmentStatus.Operational, ManagementId = management.Id };
                var eu2 = new EquipmentUnit { EquipmentId = eMicroscopio.Id, LaboratoryId = lQuimica.Id, InventoryNumber = "INV-BIO-001", CurrentStatus = EquipmentStatus.Operational, ManagementId = management.Id };
                
                // B) Activos Sanos (VerifiedGood)
                var eu3 = new EquipmentUnit { EquipmentId = eHorno.Id, LaboratoryId = lCocina.Id, InventoryNumber = "INV-GAS-002", CurrentStatus = EquipmentStatus.Operational, PhysicalCondition = PhysicalCondition.Good, ManagementId = management.Id };
                var eu4 = new EquipmentUnit { EquipmentId = eMicroscopio.Id, LaboratoryId = lQuimica.Id, InventoryNumber = "INV-BIO-002", CurrentStatus = EquipmentStatus.Operational, PhysicalCondition = PhysicalCondition.Good, ManagementId = management.Id };

                // C) Equipos en L-7 (AwaitingRequest / TechnicalRequest)
                var eu5 = new EquipmentUnit { EquipmentId = eMic.Id, LaboratoryId = lCocina.Id, InventoryNumber = "INV-GAS-003", CurrentStatus = EquipmentStatus.Broken, PhysicalCondition = PhysicalCondition.Bad, ManagementId = management.Id };

                // D) Equipos en L-8 Planeados
                var eu6 = new EquipmentUnit { EquipmentId = eBat.Id, LaboratoryId = lCocina.Id, InventoryNumber = "INV-GAS-004", CurrentStatus = EquipmentStatus.UnderMaintenance, PhysicalCondition = PhysicalCondition.Bad, ManagementId = management.Id };
                
                // E) Equipos en L-8 Ejecutados
                var eu7 = new EquipmentUnit { EquipmentId = eEsp.Id, LaboratoryId = lQuimica.Id, InventoryNumber = "INV-BIO-003", CurrentStatus = EquipmentStatus.Operational, PhysicalCondition = PhysicalCondition.Bad, ManagementId = management.Id };

                context.EquipmentUnits.AddRange(eu1, eu2, eu3, eu4, eu5, eu6, eu7);
                await context.SaveChangesAsync();

                // ---------- CREAR LAS HISTORIAS DEL WIZARD ----------

                // Historia B: Sanos (L-6 + ManagementPlan VerifiedGood)
                CreateSanoScenario(context, eu3, management);
                CreateSanoScenario(context, eu4, management);

                // Historia C: En L-7 (L-6 + L-7 + ManagementPlan TechnicalRequest)
                CreateL7Scenario(context, eu5, management, adminUser.Id, "Magnetrón no calienta adecuadamente.");

                // Historia D: En L-8 Planeado (L-6 + L-7 + L-8 Planeado + ManagementPlan MaintenanceExecution)
                CreateL8Scenario(context, eu6, management, adminUser.Id, "Cambio de aspas y engrase general.", tInt1.Id, 2, null, 0); // Semana 2 planeada, 0% avance

                // Historia E: En L-8 Ejecutado
                CreateL8Scenario(context, eu7, management, adminUser.Id, "Calibración de lente óptico UV.", tExt1.Id, 3, 3, 100); // Semana 3 planeada y ejecutada, 100% avance

                await context.SaveChangesAsync();
                
                Console.WriteLine(">>> SEMILLA: Escenarios de prueba (Activos Sanos, L-7, L-8) insertados correctamente. <<<");
            }
        }

        private static async Task<User> EnsureUserAsync(UserManager<User> userManager, string userName, string email, string firstName, string lastName, string role)
        {
            var user = await userManager.FindByNameAsync(userName);
            if (user == null)
            {
                user = new User
                {
                    UserName = userName,
                    Email = email,
                    EmailConfirmed = true,
                    FirstName = firstName,
                    LastName = lastName,
                    IdentityCard = "1234567" + new Random().Next(10, 99),
                    PhoneNumber = "70000000",
                    Status = GeneralStatus.Activo,
                    CreatedDate = DateTime.UtcNow
                };
                var result = await userManager.CreateAsync(user, "admin123");
                if (result.Succeeded)
                {
                    await userManager.AddToRoleAsync(user, role);
                }
            }
            else
            {
                if (!await userManager.IsInRoleAsync(user, role))
                {
                    await userManager.AddToRoleAsync(user, role);
                }
                var token = await userManager.GeneratePasswordResetTokenAsync(user);
                await userManager.ResetPasswordAsync(user, token, "admin123");
            }
            return user;
        }

        private static void CreateSanoScenario(ApplicationDbContext context, EquipmentUnit eu, Management mgmt)
        {
            var verif = new Verification { EquipmentUnitId = eu.Id, Date = DateTime.UtcNow, ManagementId = mgmt.Id, Status = VerificationStatus.Completed };
            context.Verifications.Add(verif);

            var plan = new ManagementPlan { 
                ManagementId = mgmt.Id, 
                EquipmentUnitId = eu.Id, 
                Verification = verif,
                CurrentPhase = WizardPhase.Verification,
                CurrentState = WizardEquipmentState.VerifiedGood,
                PlanStatus = ManagementPlanStatus.Completed,
                CreatedDate = DateTime.UtcNow
            };
            context.ManagementPlans.Add(plan);
        }

        private static void CreateL7Scenario(ApplicationDbContext context, EquipmentUnit eu, Management mgmt, int userId, string issue)
        {
            var verif = new Verification { EquipmentUnitId = eu.Id, Date = DateTime.UtcNow.AddDays(-10), ManagementId = mgmt.Id, Status = VerificationStatus.Completed };
            context.Verifications.Add(verif);
            
            var req = new Request { EquipmentId = eu.EquipmentId, EquipmentUnitId = eu.Id, LaboratoryId = eu.LaboratoryId ?? 1, RequestedById = userId, Description = issue, Priority = RequestPriority.High, Status = RequestStatus.Pending, ManagementId = mgmt.Id };
            context.Requests.Add(req);

            var plan = new ManagementPlan {
                ManagementId = mgmt.Id,
                EquipmentUnitId = eu.Id,
                Verification = verif,
                TechnicalRequest = req,
                CurrentPhase = WizardPhase.TechnicalRequest,
                CurrentState = WizardEquipmentState.AwaitingRequest,
                PlanStatus = ManagementPlanStatus.InProgress,
                CreatedDate = DateTime.UtcNow
            };
            context.ManagementPlans.Add(plan);
        }

        private static void CreateL8Scenario(ApplicationDbContext context, EquipmentUnit eu, Management mgmt, int userId, string issue, int techId, int plannedWeek, int? executedWeek, int completionPct)
        {
            var verif = new Verification { EquipmentUnitId = eu.Id, Date = DateTime.UtcNow.AddDays(-15), ManagementId = mgmt.Id, Status = VerificationStatus.Completed };
            context.Verifications.Add(verif);

            var req = new Request { EquipmentId = eu.EquipmentId, EquipmentUnitId = eu.Id, LaboratoryId = eu.LaboratoryId ?? 1, RequestedById = userId, Description = issue, Priority = RequestPriority.Medium, Status = RequestStatus.Approved, ManagementId = mgmt.Id };
            context.Requests.Add(req);

            var maint = new Maintenance { 
                EquipmentUnitId = eu.Id, 
                ManagementId = mgmt.Id, 
                Request = req, 
                TechnicianId = techId,
                CompletionPercentage = completionPct,
                Status = completionPct == 100 ? MaintenanceStatus.Completed : MaintenanceStatus.InProgress,
                ServiceType = ServiceType.Internal
            };
            context.Maintenances.Add(maint);

            var plan = new ManagementPlan {
                ManagementId = mgmt.Id,
                EquipmentUnitId = eu.Id,
                Verification = verif,
                TechnicalRequest = req,
                Maintenance = maint,
                CurrentPhase = WizardPhase.Maintenance,
                CurrentState = completionPct == 100 ? WizardEquipmentState.Completed : WizardEquipmentState.AwaitingMaintenance,
                PlanStatus = completionPct == 100 ? ManagementPlanStatus.Completed : ManagementPlanStatus.InProgress,
                PlannedWeek = plannedWeek,
                ExecutedWeek = executedWeek,
                CreatedDate = DateTime.UtcNow
            };
            context.ManagementPlans.Add(plan);
        }
    }
}
