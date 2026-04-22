using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Proyecto_Laboratorios_Univalle.Models;
using Proyecto_Laboratorios_Univalle.Models.Enums;
using Proyecto_Laboratorios_Univalle.Models.Interfaces;
using Proyecto_Laboratorios_Univalle.Services;

namespace Proyecto_Laboratorios_Univalle.Data
{
    public class ApplicationDbContext : IdentityDbContext<User, IdentityRole<int>, int>
    {
        private readonly ICurrentUserService _currentUserService;

        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options, ICurrentUserService currentUserService)
          : base(options)
        {
            _currentUserService = currentUserService;
        }

        public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            var userId = _currentUserService.UserId;
            var now = DateTime.UtcNow;

            foreach (var entry in ChangeTracker.Entries<IAuditable>())
            {
                if (entry.State == EntityState.Added)
                {
                    if (userId.HasValue && userId.Value > 0)
                    {
                        entry.Entity.CreatedById = userId;
                    }
                    else
                    {
                        entry.Entity.CreatedById = null;
                    }
                    entry.Entity.CreatedDate = now;
                }
                else if (entry.State == EntityState.Modified)
                {
                    entry.Property(x => x.CreatedDate).IsModified = false;
                    entry.Property(x => x.CreatedById).IsModified = false;

                    if (userId.HasValue && userId.Value > 0)
                    {
                        entry.Entity.ModifiedById = userId;
                    }
                    else
                    {
                        entry.Entity.ModifiedById = null;
                    }
                    entry.Entity.LastModifiedDate = now;
                }
            }

            return await base.SaveChangesAsync(cancellationToken);
        }

        public new DbSet<User> Users { get; set; } = null!;
        public DbSet<Person> People { get; set; } = null!;
        public DbSet<Faculty> Faculties { get; set; } = null!;
        public DbSet<Laboratory> Laboratories { get; set; } = null!;
        public DbSet<Country> Countries { get; set; } = null!;
        public DbSet<City> Cities { get; set; } = null!;

        public DbSet<Equipment> Equipments { get; set; } = null!;
        public DbSet<EquipmentUnit> EquipmentUnits { get; set; } = null!;
        public DbSet<EquipmentStateHistory> EquipmentStateHistories { get; set; } = null!;
        public DbSet<Request> Requests { get; set; } = null!;
        public DbSet<Maintenance> Maintenances { get; set; } = null!;
        public DbSet<Notification> Notifications { get; set; }
        public DbSet<CostDetail> CostDetails { get; set; } = null!;
        public DbSet<MaintenancePlan> MaintenancePlans { get; set; } = null!;
        public DbSet<Verification> Verifications { get; set; } = null!;
        public DbSet<VerificationCheckItem> VerificationCheckItems { get; set; } = null!;
        public DbSet<VerificationCheckResult> VerificationCheckResults { get; set; } = null!;
        public DbSet<Departure> Departures { get; set; } = null!;
        public DbSet<Intern> Interns { get; set; } = null!;
        public DbSet<Extern> Externs { get; set; } = null!;
        public DbSet<Career> Careers { get; set; } = null!;
        public DbSet<Management> Managements { get; set; } = null!;
        public DbSet<ManagementPlan> ManagementPlans { get; set; } = null!;
        public DbSet<MaintenanceTask> MaintenanceTasks { get; set; } = null!;
        


        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<User>().ToTable("Users");
            modelBuilder.Entity<IdentityRole<int>>().ToTable("Roles");
            modelBuilder.Entity<IdentityUserRole<int>>().ToTable("UserRoles");
            modelBuilder.Entity<IdentityUserClaim<int>>().ToTable("UserClaims");
            modelBuilder.Entity<IdentityUserLogin<int>>().ToTable("UserLogins");
            modelBuilder.Entity<IdentityRoleClaim<int>>().ToTable("RoleClaims");
            modelBuilder.Entity<IdentityUserToken<int>>().ToTable("UserTokens");

            modelBuilder.Entity<EquipmentUnit>().Property(e => e.AcquisitionValue).HasColumnType("decimal(18,2)");
            modelBuilder.Entity<Maintenance>().Property(m => m.EstimatedCost).HasColumnType("decimal(18,2)");
            modelBuilder.Entity<Maintenance>().Property(m => m.ActualCost).HasColumnType("decimal(18,2)");
            modelBuilder.Entity<CostDetail>().Property(d => d.UnitPrice).HasColumnType("decimal(18,2)");
            modelBuilder.Entity<CostDetail>().Property(d => d.Quantity).HasColumnType("decimal(10,2)");
            modelBuilder.Entity<MaintenancePlan>().Property(p => p.EstimatedTime).HasColumnType("decimal(10,2)");
            modelBuilder.Entity<MaintenancePlan>().Property(p => p.ActualTime).HasColumnType("decimal(10,2)");

            modelBuilder.Entity<User>().HasIndex(u => u.IdentityCard).IsUnique().HasFilter("[Status] != 2");
            modelBuilder.Entity<EquipmentUnit>().HasIndex(e => e.InventoryNumber).IsUnique().HasFilter("[CurrentStatus] != 99");
            modelBuilder.Entity<Laboratory>().HasIndex(l => l.Code).IsUnique().HasFilter("[Status] != 2");

            modelBuilder.Entity<User>().Property(u => u.Status).HasDefaultValue(GeneralStatus.Activo);
            modelBuilder.Entity<Faculty>().Property(f => f.Status).HasDefaultValue(GeneralStatus.Activo);
            modelBuilder.Entity<Laboratory>().Property(l => l.Status).HasDefaultValue(GeneralStatus.Activo);
            modelBuilder.Entity<Country>().Property(c => c.Status).HasDefaultValue(GeneralStatus.Activo);
            modelBuilder.Entity<City>().Property(c => c.Status).HasDefaultValue(GeneralStatus.Activo);
            modelBuilder.Entity<EquipmentUnit>().Property(e => e.CurrentStatus).HasDefaultValue(EquipmentStatus.Operational);
            modelBuilder.Entity<Maintenance>().Property(m => m.Status).HasDefaultValue(MaintenanceStatus.Pending);
            modelBuilder.Entity<Request>().Property(s => s.Status).HasDefaultValue(RequestStatus.Pending);
            modelBuilder.Entity<Verification>().Property(v => v.Status).HasDefaultValue(VerificationStatus.Draft);
            modelBuilder.Entity<Career>().Property(c => c.Status).HasDefaultValue(GeneralStatus.Activo);

            modelBuilder.Entity<User>().HasQueryFilter(u => u.Status != GeneralStatus.Eliminado);
            modelBuilder.Entity<Faculty>().HasQueryFilter(f => f.Status != GeneralStatus.Eliminado);
            modelBuilder.Entity<Laboratory>().HasQueryFilter(l => l.Status != GeneralStatus.Eliminado);
            modelBuilder.Entity<Country>().HasQueryFilter(p => p.Status != GeneralStatus.Eliminado);
            modelBuilder.Entity<City>().HasQueryFilter(c => c.Status != GeneralStatus.Eliminado);
            modelBuilder.Entity<EquipmentUnit>().HasQueryFilter(e => e.CurrentStatus != EquipmentStatus.Deleted);
            modelBuilder.Entity<Maintenance>().HasQueryFilter(m => m.Status != MaintenanceStatus.Cancelled);
            modelBuilder.Entity<Request>().HasQueryFilter(s => s.Status != RequestStatus.Cancelled);
            modelBuilder.Entity<Verification>().HasQueryFilter(v => v.Status != VerificationStatus.Annulled);
            modelBuilder.Entity<VerificationCheckResult>().HasQueryFilter(r => r.Verification!.Status != VerificationStatus.Annulled);
            modelBuilder.Entity<Management>().HasQueryFilter(m => m.Status != ManagementStatus.Deleted);
            modelBuilder.Entity<ManagementPlan>().HasQueryFilter(p => p.Management!.Status != ManagementStatus.Deleted);

            modelBuilder.Entity<Departure>().HasQueryFilter(l => l.Status != LoanStatus.Cancelled);
            modelBuilder.Entity<Career>().HasQueryFilter(c => c.Status != GeneralStatus.Eliminado);
            modelBuilder.Entity<MaintenanceTask>().HasQueryFilter(t => t.Maintenance!.Status != MaintenanceStatus.Cancelled);

            // Management Relationships
            modelBuilder.Entity<Verification>().HasOne(v => v.Management).WithMany().HasForeignKey(v => v.ManagementId).OnDelete(DeleteBehavior.Restrict);
            modelBuilder.Entity<Request>().HasOne(r => r.Management).WithMany().HasForeignKey(r => r.ManagementId).OnDelete(DeleteBehavior.Restrict);
            modelBuilder.Entity<Maintenance>().HasOne(m => m.Management).WithMany().HasForeignKey(m => m.ManagementId).OnDelete(DeleteBehavior.Restrict);
            modelBuilder.Entity<EquipmentUnit>().HasOne(e => e.Management).WithMany().HasForeignKey(e => e.ManagementId).OnDelete(DeleteBehavior.Restrict);
            modelBuilder.Entity<Departure>().HasOne(d => d.Management).WithMany().HasForeignKey(d => d.ManagementId).OnDelete(DeleteBehavior.Restrict);

            // Relationships
            modelBuilder.Entity<City>().HasOne(c => c.Country).WithMany(p => p.Cities).OnDelete(DeleteBehavior.Restrict);
            modelBuilder.Entity<Laboratory>().HasOne(l => l.Faculty).WithMany(f => f.Laboratories).OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Equipment>().HasOne(e => e.Country).WithMany(c => c.Equipments).HasForeignKey(e => e.CountryId).OnDelete(DeleteBehavior.Restrict);
            modelBuilder.Entity<Equipment>().HasOne(e => e.City).WithMany(c => c.Equipments).HasForeignKey(e => e.CityId).OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<EquipmentUnit>().HasOne(u => u.Laboratory).WithMany(l => l.EquipmentUnits).HasForeignKey(u => u.LaboratoryId).OnDelete(DeleteBehavior.SetNull);
            modelBuilder.Entity<EquipmentStateHistory>().HasOne(ee => ee.EquipmentUnit).WithMany(e => e.StateHistory).HasForeignKey(ee => ee.EquipmentUnitId).OnDelete(DeleteBehavior.SetNull);
            modelBuilder.Entity<Maintenance>().HasOne(m => m.EquipmentUnit).WithMany(e => e.Maintenances).OnDelete(DeleteBehavior.Restrict);
            modelBuilder.Entity<Maintenance>().HasOne(m => m.Request).WithOne(s => s.Maintenance).HasForeignKey<Maintenance>(m => m.RequestId).OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<CostDetail>().HasOne(d => d.Maintenance).WithMany(m => m.CostDetails).HasForeignKey(d => d.MaintenanceId).OnDelete(DeleteBehavior.Cascade);
            modelBuilder.Entity<CostDetail>().HasOne(d => d.Request).WithMany(r => r.CostDetails).HasForeignKey(d => d.RequestId).OnDelete(DeleteBehavior.Cascade);
            modelBuilder.Entity<Request>().HasOne(s => s.Equipment).WithMany().OnDelete(DeleteBehavior.Restrict);
            modelBuilder.Entity<Verification>().HasOne(v => v.EquipmentUnit).WithMany(e => e.Verifications).OnDelete(DeleteBehavior.Restrict);
            modelBuilder.Entity<MaintenancePlan>().HasOne(p => p.EquipmentUnit).WithMany(e => e.MaintenancePlans).HasForeignKey(p => p.EquipmentUnitId).OnDelete(DeleteBehavior.SetNull);
            modelBuilder.Entity<MaintenanceTask>().HasOne(t => t.Maintenance).WithMany(m => m.Tasks).HasForeignKey(t => t.MaintenanceId).OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<Departure>().HasOne(l => l.EquipmentUnit).WithMany(u => u.Departures).HasForeignKey(l => l.EquipmentUnitId).OnDelete(DeleteBehavior.Restrict);
            modelBuilder.Entity<Departure>().HasOne(l => l.Borrower).WithMany(p => p.Departures).HasForeignKey(l => l.BorrowerId).OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Laboratory>().HasOne(l => l.CreatedBy).WithMany().HasForeignKey(l => l.CreatedById).OnDelete(DeleteBehavior.Restrict);
            modelBuilder.Entity<Equipment>().HasOne(e => e.CreatedBy).WithMany().HasForeignKey(e => e.CreatedById).OnDelete(DeleteBehavior.Restrict);
            modelBuilder.Entity<EquipmentUnit>().HasOne(u => u.CreatedBy).WithMany().HasForeignKey(u => u.CreatedById).OnDelete(DeleteBehavior.Restrict);
            modelBuilder.Entity<EquipmentUnit>().HasOne(u => u.Equipment).WithMany(e => e.Units).HasForeignKey(u => u.EquipmentId).OnDelete(DeleteBehavior.Restrict);
            modelBuilder.Entity<EquipmentUnit>().HasOne(u => u.Career).WithMany(c => c.EquipmentUnits).HasForeignKey(u => u.CareerId).OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Career>().HasOne(c => c.Facultad).WithMany().HasForeignKey(c => c.FacultadId).OnDelete(DeleteBehavior.Restrict);
            modelBuilder.Entity<Career>().HasOne(c => c.CreatedBy).WithMany().HasForeignKey(c => c.CreatedById).OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Person>().ToTable("People");
            modelBuilder.Entity<Intern>().ToTable("Interns");
            modelBuilder.Entity<Extern>().ToTable("Externs");
            
            modelBuilder.Entity<VerificationCheckResult>()
                .HasOne(r => r.Verification).WithMany(v => v.CheckResults)
                .HasForeignKey(r => r.VerificationId).OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<VerificationCheckResult>()
                .HasOne(r => r.CheckItem).WithMany(c => c.Results)
                .HasForeignKey(r => r.CheckItemId).OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<VerificationCheckItem>().HasData(
                new VerificationCheckItem { Id = 1,  Name = "Desconexión del cable de la alimentación eléctrica para mantenimiento preventivo/correctivo 12 horas antes.", Category = "Seguridad",      Order = 1,  IsActive = true },
                new VerificationCheckItem { Id = 2,  Name = "Limpieza y desinfección interna con productos no abrasivos.",                                 Category = "Higiene",        Order = 2,  IsActive = true },
                new VerificationCheckItem { Id = 3,  Name = "Limpieza externa de condensador, serpentín, evaporador y retiro de polvo y grasas adheridas.",          Category = "Higiene",        Order = 3,  IsActive = true },
                new VerificationCheckItem { Id = 4,  Name = "Verificación de presión del refrigerante.",                                                   Category = "Refrigeración",  Order = 4,  IsActive = true },
                new VerificationCheckItem { Id = 5,  Name = "Revisión de fugas y/o microfugas en serpentín.",                                             Category = "Refrigeración",  Order = 5,  IsActive = true },
                new VerificationCheckItem { Id = 6,  Name = "Revisión de formaciones de hielo y condensaciones superficiales no esporádicas.",             Category = "Refrigeración",  Order = 6,  IsActive = true },
                new VerificationCheckItem { Id = 7,  Name = "Control de temperatura y termostatos según norma.",                                          Category = "Control",        Order = 7,  IsActive = true },
                new VerificationCheckItem { Id = 8,  Name = "Revisión de puertas y sellos de goma (empaques).",                                           Category = "Mecánica",       Order = 8,  IsActive = true },
                new VerificationCheckItem { Id = 9,  Name = "Limpieza de drenajes de deshielo.",                                                          Category = "Higiene",        Order = 9,  IsActive = true },
                new VerificationCheckItem { Id = 10, Name = "Verificación del funcionamiento de ventiladores.",                                            Category = "Mecánica",       Order = 10, IsActive = true },
                new VerificationCheckItem { Id = 11, Name = "Mantenimiento eléctrico: inspección de cableado, terminales, protecciones eléctricas, etc.",       Category = "Eléctrico",      Order = 11, IsActive = true },
                new VerificationCheckItem { Id = 12, Name = "Lubricación de partes móviles.",                                                              Category = "Mecánica",       Order = 12, IsActive = true },
                new VerificationCheckItem { Id = 13, Name = "Mantenimiento con personal externo capacitado.",                                              Category = "Gestión",        Order = 13, IsActive = true }
            );
        }
    }
}
