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
        // private readonly ICurrentUserService _currentUserService;

        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) //, ICurrentUserService currentUserService)
            : base(options)
        {
            // _currentUserService = currentUserService;
        }

        public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            // var userId = _currentUserService.UserId;
            int? userId = null; // DISABLED TEMPORARILY FOR DEBUGGING CRASH
            var now = DateTime.Now; 
            

            foreach (var entry in ChangeTracker.Entries<IAuditable>())
            {
                if (entry.State == EntityState.Added)
                {
                    // If no user is logged in (e.g., creating first user), CreatedBy can be null
                    entry.Entity.CreatedById = userId;
                    entry.Entity.CreatedDate = now;
                }
                else if (entry.State == EntityState.Modified)
                {
                    entry.Entity.ModifiedById = userId;
                    entry.Entity.LastModifiedDate = now;
                }
                // Implicit Soft Delete handling if we want to add it later could go here
            }

            return await base.SaveChangesAsync(cancellationToken);
        }

        // ============================================
        // DB SETS (Tables)
        // Initialized with null-forgiving operator to satisfy the nullable reference analysis.
        // EF will populate these at runtime.
        // ============================================
        public new DbSet<User> Users { get; set; } = null!;
        public DbSet<Person> People { get; set; } = null!; // New Table
        public DbSet<Faculty> Faculties { get; set; } = null!;
        public DbSet<Laboratory> Laboratories { get; set; } = null!;
        public DbSet<Country> Countries { get; set; } = null!;
        public DbSet<City> Cities { get; set; } = null!;
        public DbSet<EquipmentType> EquipmentTypes { get; set; } = null!;
        public DbSet<MaintenanceType> MaintenanceTypes { get; set; } = null!;
        public DbSet<Equipment> Equipments { get; set; } = null!;
        public DbSet<EquipmentStateHistory> EquipmentStateHistories { get; set; } = null!;
        public DbSet<Request> Requests { get; set; } = null!;
        public DbSet<Maintenance> Maintenances { get; set; } = null!;
        public DbSet<CostDetail> CostDetails { get; set; } = null!;
        public DbSet<MaintenancePlan> MaintenancePlans { get; set; } = null!;
        public DbSet<Verification> Verifications { get; set; } = null!;

        // ============================================
        // CONFIGURATION (OnModelCreating)
        // ============================================
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder); // IMPORTANT: Configures Identity tables

            // Customize Identity Table Names
            modelBuilder.Entity<User>().ToTable("Users");
            modelBuilder.Entity<IdentityRole<int>>().ToTable("Roles");
            modelBuilder.Entity<IdentityUserRole<int>>().ToTable("UserRoles");
            modelBuilder.Entity<IdentityUserClaim<int>>().ToTable("UserClaims");
            modelBuilder.Entity<IdentityUserLogin<int>>().ToTable("UserLogins");
            modelBuilder.Entity<IdentityRoleClaim<int>>().ToTable("RoleClaims");
            modelBuilder.Entity<IdentityUserToken<int>>().ToTable("UserTokens");

            // ============================================
            // DECIMAL PRECISION
            // ============================================
            modelBuilder.Entity<Equipment>().Property(e => e.AcquisitionValue).HasColumnType("decimal(18,2)");

            modelBuilder.Entity<Maintenance>().Property(m => m.EstimatedCost).HasColumnType("decimal(18,2)");
            modelBuilder.Entity<Maintenance>().Property(m => m.ActualCost).HasColumnType("decimal(18,2)");

            modelBuilder.Entity<CostDetail>().Property(d => d.UnitPrice).HasColumnType("decimal(18,2)");
            modelBuilder.Entity<CostDetail>().Property(d => d.Quantity).HasColumnType("decimal(10,2)");

            modelBuilder.Entity<MaintenancePlan>().Property(p => p.EstimatedTime).HasColumnType("decimal(10,2)");
            modelBuilder.Entity<MaintenancePlan>().Property(p => p.ActualTime).HasColumnType("decimal(10,2)");

            // ============================================
            // UNIQUE INDICES (With Soft Delete Filters)
            // ============================================
            // Allow duplicate IdentityCards if previous user is Deleted (Status = 2)
            modelBuilder.Entity<User>()
                .HasIndex(u => u.IdentityCard)
                .IsUnique()
                .HasFilter("[Status] != 2"); // GeneralStatus.Eliminado

            // Allow duplicate InventoryNumbers if previous equipment is Deleted (CurrentStatus = 99)
            modelBuilder.Entity<Equipment>()
                .HasIndex(e => e.InventoryNumber)
                .IsUnique()
                .HasFilter("[CurrentStatus] != 99"); // EquipmentStatus.Deleted

            // Allow duplicate Codes if previous lab is Deleted (Status = 2)
            modelBuilder.Entity<Laboratory>()
                .HasIndex(l => l.Code)
                .IsUnique()
                .HasFilter("[Status] != 2"); // GeneralStatus.Eliminado

            // ============================================
            // DEFAULT VALUES
            // ============================================
            modelBuilder.Entity<User>().Property(u => u.Status).HasDefaultValue(GeneralStatus.Activo);
            modelBuilder.Entity<Faculty>().Property(f => f.Status).HasDefaultValue(GeneralStatus.Activo);
            modelBuilder.Entity<Laboratory>().Property(l => l.Status).HasDefaultValue(GeneralStatus.Activo);
            modelBuilder.Entity<Country>().Property(c => c.Status).HasDefaultValue(GeneralStatus.Activo);
            modelBuilder.Entity<City>().Property(c => c.Status).HasDefaultValue(GeneralStatus.Activo);

            modelBuilder.Entity<Equipment>().Property(e => e.CurrentStatus).HasDefaultValue(EquipmentStatus.Operational);
            modelBuilder.Entity<EquipmentStateHistory>().Property(ee => ee.Status).HasDefaultValue(EquipmentStatus.Operational);

            modelBuilder.Entity<Maintenance>().Property(m => m.Status).HasDefaultValue(MaintenanceStatus.Pending);
            modelBuilder.Entity<Request>().Property(s => s.Status).HasDefaultValue(RequestStatus.Pending);
            modelBuilder.Entity<Verification>().Property(v => v.Status).HasDefaultValue(VerificationStatus.Draft);

            // ============================================
            // GLOBAL FILTERS (Query Filters)
            // ============================================
            modelBuilder.Entity<User>().HasQueryFilter(u => u.Status != GeneralStatus.Eliminado);
            modelBuilder.Entity<Faculty>().HasQueryFilter(f => f.Status != GeneralStatus.Eliminado);
            modelBuilder.Entity<Laboratory>().HasQueryFilter(l => l.Status != GeneralStatus.Eliminado);
            modelBuilder.Entity<Country>().HasQueryFilter(p => p.Status != GeneralStatus.Eliminado);
            modelBuilder.Entity<City>().HasQueryFilter(c => c.Status != GeneralStatus.Eliminado);
            modelBuilder.Entity<Equipment>().HasQueryFilter(e => e.CurrentStatus != EquipmentStatus.Deleted);
            modelBuilder.Entity<Maintenance>().HasQueryFilter(m => m.Status != MaintenanceStatus.Cancelled);
            modelBuilder.Entity<Request>().HasQueryFilter(s => s.Status != RequestStatus.Cancelled);
            modelBuilder.Entity<Verification>().HasQueryFilter(v => v.Status != VerificationStatus.Annulled);

            // ============================================
            // RELATIONSHIPS (DeleteBehavior.Restrict)
            // ============================================

            // City -> Country
            modelBuilder.Entity<City>()
                .HasOne(c => c.Country)
                .WithMany(p => p.Cities)
                .OnDelete(DeleteBehavior.Restrict);

            // Laboratory -> Faculty
            modelBuilder.Entity<Laboratory>()
                .HasOne(l => l.Faculty)
                .WithMany(f => f.Laboratories)
                .OnDelete(DeleteBehavior.Restrict);

            // Equipment -> Type, Lab, Country, City
            modelBuilder.Entity<Equipment>()
                .HasOne(e => e.EquipmentType)
                .WithMany(t => t.Equipments)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Equipment>()
                .HasOne(e => e.Laboratory)
                .WithMany(l => l.Equipments)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Equipment>()
                .HasOne(e => e.Country)
                .WithMany(p => p.Equipments)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Equipment>()
                .HasOne(e => e.City)
                .WithMany(c => c.Equipments)
                .OnDelete(DeleteBehavior.Restrict);

            // History -> Equipment
            modelBuilder.Entity<EquipmentStateHistory>()
                .HasOne(ee => ee.Equipment)
                .WithMany(e => e.StateHistory)
                .OnDelete(DeleteBehavior.Restrict);

            // Maintenance -> Equipment, Type, Request
            modelBuilder.Entity<Maintenance>()
                .HasOne(m => m.Equipment)
                .WithMany(e => e.Maintenances)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Maintenance>()
                .HasOne(m => m.MaintenanceType)
                .WithMany(t => t.Maintenances)
                .OnDelete(DeleteBehavior.Restrict);

            // 1:1 Request <-> Maintenance
            modelBuilder.Entity<Maintenance>()
                .HasOne(m => m.Request)
                .WithOne(s => s.Maintenance)
                .HasForeignKey<Maintenance>(m => m.RequestId)
                .OnDelete(DeleteBehavior.Restrict);

            // CostDetail -> Maintenance (CASCADE)
            modelBuilder.Entity<CostDetail>()
                .HasOne(d => d.Maintenance)
                .WithMany(m => m.CostDetails)
                .OnDelete(DeleteBehavior.Cascade);

            // Request -> Equipment
            modelBuilder.Entity<Request>()
                .HasOne(s => s.Equipment)
                .WithMany()
                .OnDelete(DeleteBehavior.Restrict);

            // Verification -> Equipment
            modelBuilder.Entity<Verification>()
                .HasOne(v => v.Equipment)
                .WithMany(e => e.Verifications)
                .OnDelete(DeleteBehavior.Restrict);

            // MaintenancePlan -> Equipment
            modelBuilder.Entity<MaintenancePlan>()
                .HasOne(p => p.Equipment)
                .WithMany(e => e.MaintenancePlans)
                .OnDelete(DeleteBehavior.Restrict);

            // ============================================
            // AUDIT & USER RELATIONSHIPS
            // ============================================

            // Laboratory -> User
            modelBuilder.Entity<Laboratory>()
                .HasOne(l => l.CreatedBy)
                .WithMany()
                .HasForeignKey(l => l.CreatedById)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Laboratory>()
                .HasOne(l => l.ModifiedBy)
                .WithMany()
                .HasForeignKey(l => l.ModifiedById)
                .OnDelete(DeleteBehavior.Restrict);

            // Equipment -> User
            modelBuilder.Entity<Equipment>()
                .HasOne(e => e.CreatedBy)
                .WithMany()
                .HasForeignKey(e => e.CreatedById)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Equipment>()
                .HasOne(e => e.ModifiedBy)
                .WithMany()
                .HasForeignKey(e => e.ModifiedById)
                .OnDelete(DeleteBehavior.Restrict);

            // History -> User
            modelBuilder.Entity<EquipmentStateHistory>()
                .HasOne(ee => ee.CreatedBy)
                .WithMany()
                .HasForeignKey(ee => ee.CreatedById)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<EquipmentStateHistory>()
                .HasOne(ee => ee.ModifiedBy)
                .WithMany()
                .HasForeignKey(ee => ee.ModifiedById)
                .OnDelete(DeleteBehavior.Restrict);

            // Maintenance -> User
            modelBuilder.Entity<Maintenance>()
                .HasOne(m => m.Technician)
                .WithMany() // Assuming not navigation property inversed for now to avoid clutter or specific 'MaintenancesPerformed'
                .HasForeignKey(m => m.TechnicianId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Maintenance>()
                .HasOne(m => m.CreatedBy)
                .WithMany()
                .HasForeignKey(m => m.CreatedById)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Maintenance>()
                .HasOne(m => m.ModifiedBy)
                .WithMany()
                .HasForeignKey(m => m.ModifiedById)
                .OnDelete(DeleteBehavior.Restrict);

            // CostDetail -> User
            modelBuilder.Entity<CostDetail>()
                .HasOne(d => d.CreatedBy)
                .WithMany()
                .HasForeignKey(d => d.CreatedById)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<CostDetail>()
                .HasOne(d => d.ModifiedBy)
                .WithMany()
                .HasForeignKey(d => d.ModifiedById)
                .OnDelete(DeleteBehavior.Restrict);

            // Request -> User
            modelBuilder.Entity<Request>()
                .HasOne(s => s.RequestedBy)
                .WithMany() // u.CreatedRequests
                .HasForeignKey(s => s.RequestedById)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Request>()
                .HasOne(s => s.ApprovedBy)
                .WithMany()
                .HasForeignKey(s => s.ApprovedById)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Request>()
                .HasOne(s => s.CreatedBy)
                .WithMany()
                .HasForeignKey(s => s.CreatedById)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Request>()
                .HasOne(s => s.ModifiedBy)
                .WithMany()
                .HasForeignKey(s => s.ModifiedById)
                .OnDelete(DeleteBehavior.Restrict);

            // Verification -> User
            modelBuilder.Entity<Verification>()
                .HasOne(v => v.CreatedBy)
                .WithMany()
                .HasForeignKey(v => v.CreatedById)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Verification>()
                .HasOne(v => v.ModifiedBy)
                .WithMany()
                .HasForeignKey(v => v.ModifiedById)
                .OnDelete(DeleteBehavior.Restrict);

            // MaintenancePlan -> User
            modelBuilder.Entity<MaintenancePlan>()
                .HasOne(p => p.Technician)
                .WithMany() // u.AssignedPlans
                .HasForeignKey(p => p.AssignedTechnicianId)
                .OnDelete(DeleteBehavior.Restrict);

            // User -> User (Audit)
            modelBuilder.Entity<User>()
                .HasOne(u => u.CreatedBy)
                .WithMany()
                .HasForeignKey(u => u.CreatedById)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<User>()
                .HasOne(u => u.ModifiedBy)
                .WithMany()
                .HasForeignKey(u => u.ModifiedById)
                .OnDelete(DeleteBehavior.Restrict);

            // Person -> User (Audit)
            modelBuilder.Entity<Person>()
                .HasOne(p => p.CreatedBy)
                .WithMany()
                .HasForeignKey(p => p.CreatedById)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Person>()
                .HasOne(p => p.ModifiedBy)
                .WithMany()
                .HasForeignKey(p => p.ModifiedById)
                .OnDelete(DeleteBehavior.Restrict);
        }
        public DbSet<Proyecto_Laboratorios_Univalle.Models.ScaffoldTest> ScaffoldTest { get; set; } = default!;
    }
}