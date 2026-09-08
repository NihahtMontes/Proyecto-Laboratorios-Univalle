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
            ChangeTracker.DetectChanges();
            SynchronizeLocationResolutionStatuses();
            await SynchronizePrimaryMaintenanceParticipantsAsync(cancellationToken);
            await SynchronizeNormalizedHistoricalRelationsAsync(cancellationToken);

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

        private void SynchronizeLocationResolutionStatuses()
        {
            foreach (var entry in ChangeTracker.Entries<EquipmentUnit>()
                .Where(entry => entry.State is EntityState.Added or EntityState.Modified))
            {
                entry.Entity.LocationResolutionStatus = entry.Entity.LaboratoryId.HasValue
                    ? LocationResolutionStatus.Confirmed
                    : LocationResolutionStatus.Pending;
            }

            foreach (var entry in ChangeTracker.Entries<Request>()
                .Where(entry => entry.State is EntityState.Added or EntityState.Modified))
            {
                entry.Entity.LocationResolutionStatus = entry.Entity.LaboratoryId.HasValue
                    ? LocationResolutionStatus.Confirmed
                    : LocationResolutionStatus.Pending;
            }
        }

        private async Task SynchronizePrimaryMaintenanceParticipantsAsync(CancellationToken cancellationToken)
        {
            var maintenanceEntries = ChangeTracker.Entries<Maintenance>()
                .Where(entry => entry.State == EntityState.Added
                    || (entry.State == EntityState.Modified
                        && entry.Property(m => m.TechnicianId).IsModified))
                .ToList();

            foreach (var entry in maintenanceEntries)
            {
                var maintenance = entry.Entity;
                if (entry.State == EntityState.Added)
                {
                    if (maintenance.TechnicianId.HasValue)
                    {
                        maintenance.Participants.Add(new MaintenanceParticipant
                        {
                            PersonId = maintenance.TechnicianId.Value,
                            Role = MaintenanceParticipantRole.Technician,
                            IsPrimary = true,
                            IsActive = true,
                            AssignedAt = DateTime.UtcNow
                        });
                    }

                    continue;
                }

                var activePrimaryParticipants = await MaintenanceParticipants
                    .IgnoreQueryFilters()
                    .AsTracking()
                    .Where(participant => participant.MaintenanceId == maintenance.Id
                        && participant.IsActive
                        && participant.IsPrimary)
                    .ToListAsync(cancellationToken);

                if (maintenance.TechnicianId.HasValue
                    && activePrimaryParticipants.Count == 1
                    && activePrimaryParticipants[0].PersonId == maintenance.TechnicianId.Value)
                {
                    continue;
                }

                var now = DateTime.UtcNow;
                foreach (var participant in activePrimaryParticipants)
                {
                    participant.IsPrimary = false;
                    participant.IsActive = false;
                    participant.UnassignedAt = now;
                }

                if (maintenance.TechnicianId.HasValue)
                {
                    MaintenanceParticipants.Add(new MaintenanceParticipant
                    {
                        MaintenanceId = maintenance.Id,
                        PersonId = maintenance.TechnicianId.Value,
                        Role = MaintenanceParticipantRole.Technician,
                        IsPrimary = true,
                        IsActive = true,
                        AssignedAt = now
                    });
                }
            }
        }

        private async Task SynchronizeNormalizedHistoricalRelationsAsync(CancellationToken cancellationToken)
        {
            var requestEntries = ChangeTracker.Entries<Request>()
                .Where(entry => entry.State == EntityState.Added
                    || (entry.State == EntityState.Modified
                        && entry.Property(request => request.EquipmentUnitId).IsModified))
                .ToList();

            foreach (var entry in requestEntries)
            {
                var request = entry.Entity;
                if (entry.State == EntityState.Added)
                {
                    if (request.EquipmentUnitId.HasValue
                        && !request.EquipmentUnitLinks.Any(link => link.EquipmentUnitId == request.EquipmentUnitId.Value && link.IsActive))
                    {
                        request.EquipmentUnitLinks.Add(new RequestEquipmentUnit
                        {
                            EquipmentUnitId = request.EquipmentUnitId.Value,
                            IsLegacyPrimary = true,
                            IsActive = true
                        });
                    }

                    continue;
                }

                var persistedLinks = await RequestEquipmentUnits
                    .IgnoreQueryFilters()
                    .AsTracking()
                    .Where(link => link.RequestId == request.Id)
                    .ToListAsync(cancellationToken);
                persistedLinks = persistedLinks
                    .Concat(ChangeTracker.Entries<RequestEquipmentUnit>()
                        .Where(linkEntry => linkEntry.State != EntityState.Deleted
                            && (linkEntry.Entity.RequestId == request.Id || linkEntry.Entity.Request == request))
                        .Select(linkEntry => linkEntry.Entity))
                    .Distinct()
                    .ToList();

                foreach (var legacyLink in persistedLinks.Where(link => link.IsLegacyPrimary))
                {
                    if (request.EquipmentUnitId.HasValue
                        && legacyLink.EquipmentUnitId == request.EquipmentUnitId.Value)
                    {
                        legacyLink.IsActive = true;
                        legacyLink.DeactivatedDate = null;
                        continue;
                    }

                    legacyLink.IsLegacyPrimary = false;
                    legacyLink.IsActive = false;
                    legacyLink.DeactivatedDate = DateTime.UtcNow;
                }

                if (request.EquipmentUnitId.HasValue)
                {
                    var currentLink = persistedLinks.FirstOrDefault(link => link.EquipmentUnitId == request.EquipmentUnitId.Value);
                    if (currentLink == null)
                    {
                        RequestEquipmentUnits.Add(new RequestEquipmentUnit
                        {
                            RequestId = request.Id,
                            EquipmentUnitId = request.EquipmentUnitId.Value,
                            IsLegacyPrimary = true,
                            IsActive = true
                        });
                    }
                    else
                    {
                        currentLink.IsLegacyPrimary = true;
                        currentLink.IsActive = true;
                        currentLink.DeactivatedDate = null;
                    }
                }
            }

            var maintenanceEntries = ChangeTracker.Entries<Maintenance>()
                .Where(entry => entry.State == EntityState.Added
                    || (entry.State == EntityState.Modified
                        && entry.Property(maintenance => maintenance.RequestId).IsModified))
                .ToList();

            foreach (var entry in maintenanceEntries)
            {
                var maintenance = entry.Entity;
                if (entry.State == EntityState.Added)
                {
                    if (maintenance.RequestId.HasValue
                        && !maintenance.RequestLinks.Any(link => link.RequestId == maintenance.RequestId.Value && link.IsActive))
                    {
                        maintenance.RequestLinks.Add(new MaintenanceRequest
                        {
                            RequestId = maintenance.RequestId.Value,
                            IsLegacyPrimary = true,
                            IsActive = true
                        });
                    }

                    continue;
                }

                var persistedLinks = await MaintenanceRequests
                    .IgnoreQueryFilters()
                    .AsTracking()
                    .Where(link => link.MaintenanceId == maintenance.Id)
                    .ToListAsync(cancellationToken);
                persistedLinks = persistedLinks
                    .Concat(ChangeTracker.Entries<MaintenanceRequest>()
                        .Where(linkEntry => linkEntry.State != EntityState.Deleted
                            && (linkEntry.Entity.MaintenanceId == maintenance.Id || linkEntry.Entity.Maintenance == maintenance))
                        .Select(linkEntry => linkEntry.Entity))
                    .Distinct()
                    .ToList();

                foreach (var legacyLink in persistedLinks.Where(link => link.IsLegacyPrimary))
                {
                    if (maintenance.RequestId.HasValue
                        && legacyLink.RequestId == maintenance.RequestId.Value)
                    {
                        legacyLink.IsActive = true;
                        legacyLink.DeactivatedDate = null;
                        continue;
                    }

                    legacyLink.IsLegacyPrimary = false;
                    legacyLink.IsActive = false;
                    legacyLink.DeactivatedDate = DateTime.UtcNow;
                }

                if (maintenance.RequestId.HasValue)
                {
                    var currentLink = persistedLinks.FirstOrDefault(link => link.RequestId == maintenance.RequestId.Value);
                    if (currentLink == null)
                    {
                        MaintenanceRequests.Add(new MaintenanceRequest
                        {
                            MaintenanceId = maintenance.Id,
                            RequestId = maintenance.RequestId.Value,
                            IsLegacyPrimary = true,
                            IsActive = true
                        });
                    }
                    else
                    {
                        currentLink.IsLegacyPrimary = true;
                        currentLink.IsActive = true;
                        currentLink.DeactivatedDate = null;
                    }
                }
            }
        }

        public new DbSet<User> Users { get; set; } = null!;
        public DbSet<VerificationFault> VerificationFaults { get; set; } = null!;
        public DbSet<Person> People { get; set; } = null!;
        public DbSet<Faculty> Faculties { get; set; } = null!;
        public DbSet<Laboratory> Laboratories { get; set; } = null!;
        public DbSet<Country> Countries { get; set; } = null!;
        public DbSet<City> Cities { get; set; } = null!;

        public DbSet<Equipment> Equipments { get; set; } = null!;
        public DbSet<EquipmentUnit> EquipmentUnits { get; set; } = null!;
        public DbSet<EquipmentStateHistory> EquipmentStateHistories { get; set; } = null!;
        public DbSet<EquipmentNote> EquipmentNotes { get; set; } = null!;
        public DbSet<Request> Requests { get; set; } = null!;
        public DbSet<Maintenance> Maintenances { get; set; } = null!;
        public DbSet<Notification> Notifications { get; set; }
        public DbSet<CostDetail> CostDetails { get; set; } = null!;
        public DbSet<MaintenancePlan> MaintenancePlans { get; set; } = null!;
        public DbSet<Verification> Verifications { get; set; } = null!;
        public DbSet<VerificationCheckItem> VerificationCheckItems { get; set; } = null!;
        public DbSet<VerificationCheckResult> VerificationCheckResults { get; set; } = null!;
        public DbSet<Departure> Departures { get; set; } = null!;
        public DbSet<DepartureItem> DepartureItems { get; set; } = null!;
        public DbSet<Intern> Interns { get; set; } = null!;
        public DbSet<Extern> Externs { get; set; } = null!;
        public DbSet<Career> Careers { get; set; } = null!;
        public DbSet<Management> Managements { get; set; } = null!;
        public DbSet<ManagementPlan> ManagementPlans { get; set; } = null!;
        public DbSet<MaintenanceTask> MaintenanceTasks { get; set; } = null!;
        public DbSet<HistoricalVerificationQuarantine> HistoricalVerificationQuarantines { get; set; } = null!;
        public DbSet<ImportBatch> ImportBatches { get; set; } = null!;
        public DbSet<DataQualityIssue> DataQualityIssues { get; set; } = null!;
        public DbSet<MaintenanceParticipant> MaintenanceParticipants { get; set; } = null!;
        public DbSet<PersonAlias> PersonAliases { get; set; } = null!;
        public DbSet<PersonRoleAssignment> PersonRoleAssignments { get; set; } = null!;
        public DbSet<Article> Articles { get; set; } = null!;
        public DbSet<RequestEquipmentUnit> RequestEquipmentUnits { get; set; } = null!;
        public DbSet<MaintenanceRequest> MaintenanceRequests { get; set; } = null!;
        public DbSet<ImportSourceRow> ImportSourceRows { get; set; } = null!;
        public DbSet<EquipmentClassificationDecision> EquipmentClassificationDecisions { get; set; } = null!;
        


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

            modelBuilder.Entity<EquipmentUnit>().Property(e => e.AcquisitionValue).HasPrecision(18, 2);
            modelBuilder.Entity<Maintenance>().Property(m => m.EstimatedCost).HasPrecision(18, 2);
            modelBuilder.Entity<Maintenance>().Property(m => m.ActualCost).HasPrecision(18, 2);
            modelBuilder.Entity<CostDetail>().Property(d => d.UnitPrice).HasPrecision(18, 2);
            modelBuilder.Entity<CostDetail>().Property(d => d.Quantity).HasPrecision(10, 2);
            modelBuilder.Entity<MaintenancePlan>().Property(p => p.EstimatedTime).HasPrecision(10, 2);
            modelBuilder.Entity<MaintenancePlan>().Property(p => p.ActualTime).HasPrecision(10, 2);

            modelBuilder.Entity<User>().HasIndex(u => u.IdentityCard).IsUnique().HasFilter("[Status] <> 2");
            modelBuilder.Entity<EquipmentUnit>().HasIndex(e => e.InventoryNumber).IsUnique().HasFilter("[CurrentStatus] <> 99");
            modelBuilder.Entity<Laboratory>().HasIndex(l => l.Code).IsUnique().HasFilter("[Status] <> 2");
            modelBuilder.Entity<ImportBatch>().HasIndex(batch => batch.Code).IsUnique();
            modelBuilder.Entity<Management>().HasIndex(m => m.Code).IsUnique().HasFilter("[Status] <> 99");
            modelBuilder.Entity<Management>().HasIndex(m => m.Status).IsUnique().HasFilter("[Status] = 0");
            modelBuilder.Entity<ManagementPlan>().HasIndex(p => new { p.ManagementId, p.EquipmentUnitId }).IsUnique().HasFilter("[EquipmentUnitId] IS NOT NULL");
            modelBuilder.Entity<VerificationCheckResult>().HasIndex(r => new { r.VerificationId, r.CheckItemId }).IsUnique();
            modelBuilder.Entity<MaintenanceParticipant>().HasIndex(p => new { p.MaintenanceId, p.PersonId, p.Role }).IsUnique().HasFilter("[IsActive] = 1");
            modelBuilder.Entity<MaintenanceParticipant>().HasIndex(p => p.MaintenanceId).IsUnique().HasFilter("[IsPrimary] = 1 AND [IsActive] = 1");
            modelBuilder.Entity<PersonAlias>().HasIndex(a => new { a.PersonId, a.NormalizedAlias }).IsUnique();
            modelBuilder.Entity<PersonAlias>().HasIndex(a => a.PersonId).IsUnique().HasFilter("[IsPreferred] = 1");
            modelBuilder.Entity<PersonRoleAssignment>().HasIndex(r => new { r.PersonId, r.Role }).IsUnique().HasFilter("[IsActive] = 1");
            modelBuilder.Entity<DataQualityIssue>().HasIndex(issue => new { issue.ImportBatchId, issue.IssueCode, issue.EntityName });
            modelBuilder.Entity<Equipment>().HasIndex(e => e.CatalogCode).IsUnique().HasFilter("[CatalogCode] IS NOT NULL");
            modelBuilder.Entity<Equipment>().HasIndex(e => new { e.ClassificationReviewStatus, e.Category });
            modelBuilder.Entity<EquipmentClassificationDecision>().HasIndex(d => d.DecisionKey).IsUnique();
            modelBuilder.Entity<EquipmentClassificationDecision>().HasIndex(d => d.EquipmentId).IsUnique().HasFilter("[EffectiveTo] IS NULL");
            modelBuilder.Entity<EquipmentClassificationDecision>().HasIndex(d => d.ImportBatchId);
            modelBuilder.Entity<EquipmentClassificationDecision>().HasIndex(d => d.ImportSourceRowId);
            modelBuilder.Entity<Career>().HasIndex(c => c.Code).IsUnique().HasFilter("[Code] IS NOT NULL");
            modelBuilder.Entity<Person>().HasIndex(p => p.ActorCode).IsUnique().HasFilter("[ActorCode] IS NOT NULL");
            modelBuilder.Entity<Article>().HasIndex(a => a.Code).IsUnique();
            modelBuilder.Entity<RequestEquipmentUnit>().HasIndex(link => new { link.RequestId, link.EquipmentUnitId }).IsUnique();
            modelBuilder.Entity<MaintenanceRequest>().HasIndex(link => new { link.MaintenanceId, link.RequestId }).IsUnique();
            modelBuilder.Entity<ImportSourceRow>().HasIndex(row => new { row.ImportBatchId, row.SourceRowKey, row.TargetEntityName }).IsUnique();
            modelBuilder.Entity<DataQualityIssue>().HasIndex(issue => new { issue.ImportBatchId, issue.SourceSheet, issue.SourceRowNumber });
            modelBuilder.Entity<MaintenancePlan>().HasIndex(plan => plan.PlanCode).IsUnique().HasFilter("[PlanCode] IS NOT NULL");
            modelBuilder.Entity<MaintenancePlan>().HasIndex(plan => plan.HistoricalSourceKey).IsUnique().HasFilter("[HistoricalSourceKey] IS NOT NULL");

            // Bloque 5A: Índices de Performance
            modelBuilder.Entity<EquipmentUnit>().HasIndex(e => e.CurrentStatus);
            modelBuilder.Entity<Maintenance>().HasIndex(m => m.Status);
            modelBuilder.Entity<ManagementPlan>().HasIndex(p => new { p.PlanStatus, p.ManagementId });
            modelBuilder.Entity<ManagementPlan>().HasIndex(p => new { p.CurrentPhase, p.ManagementId });
            modelBuilder.Entity<Management>().HasIndex(m => new { m.Type, m.Status, m.Year, m.Semester });
            modelBuilder.Entity<Notification>().HasIndex(n => new { n.ManagementId, n.ManagementType, n.Scope, n.IsRead });

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
            modelBuilder.Entity<Equipment>().Property(e => e.Status).HasDefaultValue(GeneralStatus.Activo);
            modelBuilder.Entity<Equipment>().Property(e => e.ClassificationReviewStatus)
                .HasDefaultValue(EquipmentClassificationReviewStatus.LegacyInferred);
            modelBuilder.Entity<Article>().Property(a => a.Status).HasDefaultValue(GeneralStatus.Activo);
            modelBuilder.Entity<ImportBatch>().Property(batch => batch.ContractVersion).HasDefaultValue("legacy-v1");
            modelBuilder.Entity<RequestEquipmentUnit>().Property(link => link.IsActive).HasDefaultValue(true);
            modelBuilder.Entity<MaintenanceRequest>().Property(link => link.IsActive).HasDefaultValue(true);

            modelBuilder.Entity<User>().HasQueryFilter(u => u.Status != GeneralStatus.Eliminado);
            modelBuilder.Entity<Faculty>().HasQueryFilter(f => f.Status != GeneralStatus.Eliminado);
            modelBuilder.Entity<Laboratory>().HasQueryFilter(l => l.Status != GeneralStatus.Eliminado);
            modelBuilder.Entity<Country>().HasQueryFilter(p => p.Status != GeneralStatus.Eliminado);
            modelBuilder.Entity<City>().HasQueryFilter(c => c.Status != GeneralStatus.Eliminado);
            modelBuilder.Entity<Equipment>().HasQueryFilter(e => e.Status != GeneralStatus.Eliminado);
            modelBuilder.Entity<Article>().HasQueryFilter(a => a.Status != GeneralStatus.Eliminado);
            modelBuilder.Entity<RequestEquipmentUnit>().HasQueryFilter(link => link.IsActive
                && link.Request!.Status != RequestStatus.Cancelled
                && link.EquipmentUnit!.CurrentStatus != EquipmentStatus.Deleted);
            modelBuilder.Entity<MaintenanceRequest>().HasQueryFilter(link => link.IsActive
                && link.Maintenance!.Status != MaintenanceStatus.Cancelled
                && link.Request!.Status != RequestStatus.Cancelled);
            modelBuilder.Entity<Person>().HasQueryFilter(p => p.Status != GeneralStatus.Eliminado);
            modelBuilder.Entity<EquipmentUnit>().HasQueryFilter(e => e.CurrentStatus != EquipmentStatus.Deleted);
            modelBuilder.Entity<Maintenance>().HasQueryFilter(m => m.Status != MaintenanceStatus.Cancelled);
            modelBuilder.Entity<Request>().HasQueryFilter(s => s.Status != RequestStatus.Cancelled);
            modelBuilder.Entity<Verification>().HasQueryFilter(v => v.Status != VerificationStatus.Annulled);
            modelBuilder.Entity<VerificationCheckResult>().HasQueryFilter(r => r.Verification!.Status != VerificationStatus.Annulled);
            modelBuilder.Entity<Management>().HasQueryFilter(m => m.Status != ManagementStatus.Deleted);
            modelBuilder.Entity<ManagementPlan>().HasQueryFilter(p => p.Management!.Status != ManagementStatus.Deleted);

            modelBuilder.Entity<Departure>().HasQueryFilter(l => l.Status != LoanStatus.Cancelled);
            modelBuilder.Entity<DepartureItem>().HasQueryFilter(i => !i.IsRemoved);
            modelBuilder.Entity<Career>().HasQueryFilter(c => c.Status != GeneralStatus.Eliminado);
            modelBuilder.Entity<MaintenanceTask>().HasQueryFilter(t => !t.IsDeleted && t.Maintenance!.Status != MaintenanceStatus.Cancelled);
            modelBuilder.Entity<EquipmentNote>().HasQueryFilter(n => n.Equipment!.Status != GeneralStatus.Eliminado);
            modelBuilder.Entity<MaintenanceParticipant>().HasQueryFilter(p => p.IsActive
                && p.Maintenance!.Status != MaintenanceStatus.Cancelled
                && p.Person!.Status != GeneralStatus.Eliminado);
            modelBuilder.Entity<PersonAlias>().HasQueryFilter(a => a.Person!.Status != GeneralStatus.Eliminado);
            modelBuilder.Entity<PersonRoleAssignment>().HasQueryFilter(r => r.IsActive && r.Person!.Status != GeneralStatus.Eliminado);

            // Management Relationships
            modelBuilder.Entity<Management>().HasOne(m => m.Faculty).WithMany().HasForeignKey(m => m.FacultyId).OnDelete(DeleteBehavior.SetNull);
            modelBuilder.Entity<Verification>().HasOne(v => v.Management).WithMany().HasForeignKey(v => v.ManagementId).OnDelete(DeleteBehavior.Restrict);
            modelBuilder.Entity<Request>().HasOne(r => r.Management).WithMany().HasForeignKey(r => r.ManagementId).OnDelete(DeleteBehavior.Restrict);
            modelBuilder.Entity<Maintenance>().HasOne(m => m.Management).WithMany().HasForeignKey(m => m.ManagementId).OnDelete(DeleteBehavior.Restrict);
            modelBuilder.Entity<EquipmentUnit>().HasOne(e => e.Management).WithMany().HasForeignKey(e => e.ManagementId).OnDelete(DeleteBehavior.Restrict);
            modelBuilder.Entity<Departure>().HasOne(d => d.Management).WithMany().HasForeignKey(d => d.ManagementId).OnDelete(DeleteBehavior.Restrict);

            // Cascade fixes
            modelBuilder.Entity<ManagementPlan>().HasOne(p => p.Management).WithMany(m => m.ManagementPlans).HasForeignKey(p => p.ManagementId).OnDelete(DeleteBehavior.Restrict);
            modelBuilder.Entity<Notification>().HasOne(n => n.Management).WithMany().HasForeignKey(n => n.ManagementId).OnDelete(DeleteBehavior.SetNull);
            modelBuilder.Entity<Request>().HasOne(r => r.Laboratory).WithMany().HasForeignKey(r => r.LaboratoryId).OnDelete(DeleteBehavior.Restrict);
            modelBuilder.Entity<Laboratory>().HasOne(l => l.City).WithMany().HasForeignKey(l => l.CityId).OnDelete(DeleteBehavior.SetNull);

            // Relationships
            modelBuilder.Entity<City>().HasOne(c => c.Country).WithMany(p => p.Cities).OnDelete(DeleteBehavior.Restrict);
            modelBuilder.Entity<Laboratory>().HasOne(l => l.Faculty).WithMany(f => f.Laboratories).OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Equipment>().HasOne(e => e.Country).WithMany(c => c.Equipments).HasForeignKey(e => e.CountryId).OnDelete(DeleteBehavior.Restrict);
            modelBuilder.Entity<Equipment>().HasOne(e => e.City).WithMany(c => c.Equipments).HasForeignKey(e => e.CityId).OnDelete(DeleteBehavior.Restrict);
            modelBuilder.Entity<EquipmentClassificationDecision>()
                .HasOne(d => d.Equipment)
                .WithMany(e => e.ClassificationDecisions)
                .HasForeignKey(d => d.EquipmentId)
                .OnDelete(DeleteBehavior.Restrict);
            modelBuilder.Entity<EquipmentClassificationDecision>()
                .HasOne(d => d.ResponsiblePerson)
                .WithMany()
                .HasForeignKey(d => d.ResponsiblePersonId)
                .OnDelete(DeleteBehavior.Restrict);
            modelBuilder.Entity<EquipmentClassificationDecision>()
                .HasOne(d => d.ImportBatch)
                .WithMany(batch => batch.EquipmentClassificationDecisions)
                .HasForeignKey(d => d.ImportBatchId)
                .OnDelete(DeleteBehavior.Restrict);
            modelBuilder.Entity<EquipmentClassificationDecision>()
                .HasOne(d => d.ImportSourceRow)
                .WithMany(row => row.EquipmentClassificationDecisions)
                .HasForeignKey(d => d.ImportSourceRowId)
                .OnDelete(DeleteBehavior.Restrict);
            modelBuilder.Entity<EquipmentClassificationDecision>()
                .HasOne(d => d.RecordedByUser)
                .WithMany()
                .HasForeignKey(d => d.RecordedByUserId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<EquipmentUnit>().HasOne(u => u.Laboratory).WithMany(l => l.EquipmentUnits).HasForeignKey(u => u.LaboratoryId).OnDelete(DeleteBehavior.SetNull);
            modelBuilder.Entity<EquipmentStateHistory>().HasOne(ee => ee.EquipmentUnit).WithMany(e => e.StateHistory).HasForeignKey(ee => ee.EquipmentUnitId).OnDelete(DeleteBehavior.SetNull);
            modelBuilder.Entity<Maintenance>().HasOne(m => m.EquipmentUnit).WithMany(e => e.Maintenances).OnDelete(DeleteBehavior.Restrict);
            modelBuilder.Entity<Maintenance>().HasOne(m => m.Request).WithOne(s => s.Maintenance).HasForeignKey<Maintenance>(m => m.RequestId).OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<CostDetail>().HasOne(d => d.Maintenance).WithMany(m => m.CostDetails).HasForeignKey(d => d.MaintenanceId).OnDelete(DeleteBehavior.Cascade);
            modelBuilder.Entity<CostDetail>().HasOne(d => d.Request).WithMany(r => r.CostDetails).HasForeignKey(d => d.RequestId).OnDelete(DeleteBehavior.Cascade);
            modelBuilder.Entity<Request>().HasOne(s => s.Equipment).WithMany().OnDelete(DeleteBehavior.Restrict);
            modelBuilder.Entity<Verification>().HasOne(v => v.EquipmentUnit).WithMany(e => e.Verifications).OnDelete(DeleteBehavior.Restrict);
            modelBuilder.Entity<MaintenancePlan>().HasOne(p => p.EquipmentUnit).WithMany(e => e.MaintenancePlans).HasForeignKey(p => p.EquipmentUnitId).OnDelete(DeleteBehavior.SetNull);
            modelBuilder.Entity<MaintenancePlan>().HasOne(p => p.Laboratory).WithMany(l => l.MaintenancePlans).HasForeignKey(p => p.LaboratoryId).OnDelete(DeleteBehavior.SetNull);
            modelBuilder.Entity<MaintenancePlan>().HasOne(p => p.Management).WithMany(m => m.HistoricalMaintenancePlans).HasForeignKey(p => p.ManagementId).OnDelete(DeleteBehavior.Restrict);
            modelBuilder.Entity<MaintenancePlan>().HasOne(p => p.ResponsiblePerson).WithMany(person => person.ResponsibleMaintenancePlans).HasForeignKey(p => p.ResponsiblePersonId).OnDelete(DeleteBehavior.Restrict);
            modelBuilder.Entity<MaintenancePlan>().HasOne(p => p.ImportBatch).WithMany(batch => batch.MaintenancePlans).HasForeignKey(p => p.ImportBatchId).OnDelete(DeleteBehavior.Restrict);
            modelBuilder.Entity<MaintenanceTask>().HasOne(t => t.Maintenance).WithMany(m => m.Tasks).HasForeignKey(t => t.MaintenanceId).OnDelete(DeleteBehavior.Cascade);
            modelBuilder.Entity<MaintenanceParticipant>().HasOne(p => p.Maintenance).WithMany(m => m.Participants).HasForeignKey(p => p.MaintenanceId).OnDelete(DeleteBehavior.Cascade);
            modelBuilder.Entity<MaintenanceParticipant>().HasOne(p => p.Person).WithMany(p => p.MaintenanceParticipations).HasForeignKey(p => p.PersonId).OnDelete(DeleteBehavior.Restrict);
            modelBuilder.Entity<PersonAlias>().HasOne(a => a.Person).WithMany(p => p.Aliases).HasForeignKey(a => a.PersonId).OnDelete(DeleteBehavior.Cascade);
            modelBuilder.Entity<PersonRoleAssignment>().HasOne(r => r.Person).WithMany(p => p.RoleAssignments).HasForeignKey(r => r.PersonId).OnDelete(DeleteBehavior.Cascade);
            modelBuilder.Entity<DataQualityIssue>().HasOne(i => i.ImportBatch).WithMany(b => b.DataQualityIssues).HasForeignKey(i => i.ImportBatchId).OnDelete(DeleteBehavior.Restrict);
            modelBuilder.Entity<DataQualityIssue>().HasOne(i => i.ResolvedByUser).WithMany().HasForeignKey(i => i.ResolvedByUserId).OnDelete(DeleteBehavior.Restrict);
            modelBuilder.Entity<Article>().HasOne(a => a.ImportBatch).WithMany().HasForeignKey(a => a.ImportBatchId).OnDelete(DeleteBehavior.Restrict);
            modelBuilder.Entity<RequestEquipmentUnit>().HasOne(link => link.Request).WithMany(r => r.EquipmentUnitLinks).HasForeignKey(link => link.RequestId).OnDelete(DeleteBehavior.Restrict);
            modelBuilder.Entity<RequestEquipmentUnit>().HasOne(link => link.EquipmentUnit).WithMany(u => u.RequestLinks).HasForeignKey(link => link.EquipmentUnitId).OnDelete(DeleteBehavior.Restrict);
            modelBuilder.Entity<MaintenanceRequest>().HasOne(link => link.Maintenance).WithMany(m => m.RequestLinks).HasForeignKey(link => link.MaintenanceId).OnDelete(DeleteBehavior.Restrict);
            modelBuilder.Entity<MaintenanceRequest>().HasOne(link => link.Request).WithMany(r => r.MaintenanceLinks).HasForeignKey(link => link.RequestId).OnDelete(DeleteBehavior.Restrict);
            modelBuilder.Entity<Request>().HasOne(r => r.RequestedByPerson).WithMany(p => p.RequestedRequests).HasForeignKey(r => r.RequestedByPersonId).OnDelete(DeleteBehavior.Restrict);
            modelBuilder.Entity<Verification>().HasOne(v => v.ResponsiblePerson).WithMany(p => p.ResponsibleVerifications).HasForeignKey(v => v.ResponsiblePersonId).OnDelete(DeleteBehavior.Restrict);
            modelBuilder.Entity<CostDetail>().HasOne(c => c.ProviderPerson).WithMany(p => p.ProvidedCostDetails).HasForeignKey(c => c.ProviderPersonId).OnDelete(DeleteBehavior.Restrict);
            modelBuilder.Entity<ManagementPlan>().HasOne(p => p.ResponsiblePerson).WithMany(person => person.ResponsibleManagementPlans).HasForeignKey(p => p.ResponsiblePersonId).OnDelete(DeleteBehavior.Restrict);
            modelBuilder.Entity<ImportSourceRow>().HasOne(row => row.ImportBatch).WithMany(batch => batch.SourceRows).HasForeignKey(row => row.ImportBatchId).OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Verification>()
                .HasIndex(v => v.HistoricalSourceKey)
                .IsUnique()
                .HasFilter("[HistoricalSourceKey] IS NOT NULL");
            modelBuilder.Entity<Request>()
                .HasIndex(r => r.HistoricalSourceKey)
                .IsUnique()
                .HasFilter("[HistoricalSourceKey] IS NOT NULL");
            modelBuilder.Entity<Maintenance>()
                .HasIndex(m => m.HistoricalSourceKey)
                .IsUnique()
                .HasFilter("[HistoricalSourceKey] IS NOT NULL");
            modelBuilder.Entity<CostDetail>()
                .HasIndex(c => c.HistoricalSourceKey)
                .IsUnique()
                .HasFilter("[HistoricalSourceKey] IS NOT NULL");
            modelBuilder.Entity<Departure>()
                .HasIndex(d => d.HistoricalSourceKey)
                .IsUnique()
                .HasFilter("[HistoricalSourceKey] IS NOT NULL");
            modelBuilder.Entity<HistoricalVerificationQuarantine>()
                .HasIndex(row => row.SourceKey)
                .IsUnique();

            modelBuilder.Entity<Departure>().HasOne(l => l.EquipmentUnit).WithMany(u => u.Departures).HasForeignKey(l => l.EquipmentUnitId).OnDelete(DeleteBehavior.Restrict);
            modelBuilder.Entity<Departure>().HasOne(l => l.Borrower).WithMany(p => p.Departures).HasForeignKey(l => l.BorrowerId).OnDelete(DeleteBehavior.Restrict);
            modelBuilder.Entity<Departure>().HasOne(d => d.OriginLaboratory).WithMany().HasForeignKey(d => d.OriginLaboratoryId).OnDelete(DeleteBehavior.Restrict);
            modelBuilder.Entity<Departure>().HasOne(d => d.ImportBatch).WithMany().HasForeignKey(d => d.ImportBatchId).OnDelete(DeleteBehavior.Restrict);
            modelBuilder.Entity<DepartureItem>().HasOne(item => item.Article).WithMany(article => article.DepartureItems).HasForeignKey(item => item.ArticleId).OnDelete(DeleteBehavior.Restrict);

            // Sprint 3B: Soft Delete para fallas
            modelBuilder.Entity<VerificationFault>().HasQueryFilter(e => !e.IsDeleted && e.Verification!.Status != VerificationStatus.Annulled);

            // ManagementPlan KardexHistory relationship
            modelBuilder.Entity<ManagementPlan>()
                .HasOne(p => p.KardexHistory)
                .WithMany()
                .HasForeignKey(p => p.KardexHistoryId)
                .OnDelete(DeleteBehavior.SetNull);

            // Departure Items relationship
            modelBuilder.Entity<Departure>()
                .HasMany(d => d.Items)
                .WithOne(i => i.Departure)
                .HasForeignKey(i => i.DepartureId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Laboratory>().HasOne(l => l.CreatedBy).WithMany().HasForeignKey(l => l.CreatedById).OnDelete(DeleteBehavior.Restrict);
            modelBuilder.Entity<Equipment>().HasOne(e => e.CreatedBy).WithMany().HasForeignKey(e => e.CreatedById).OnDelete(DeleteBehavior.Restrict);
            modelBuilder.Entity<Equipment>().HasMany(e => e.Notes).WithOne(n => n.Equipment).HasForeignKey(n => n.EquipmentId).OnDelete(DeleteBehavior.Cascade);
            modelBuilder.Entity<EquipmentUnit>().HasOne(u => u.CreatedBy).WithMany().HasForeignKey(u => u.CreatedById).OnDelete(DeleteBehavior.Restrict);
            modelBuilder.Entity<EquipmentUnit>().HasOne(u => u.Equipment).WithMany(e => e.Units).HasForeignKey(u => u.EquipmentId).OnDelete(DeleteBehavior.Restrict);
            modelBuilder.Entity<EquipmentUnit>().HasOne(u => u.Career).WithMany(c => c.EquipmentUnits).HasForeignKey(u => u.CareerId).OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Career>().HasOne(c => c.Facultad).WithMany().HasForeignKey(c => c.FacultadId).OnDelete(DeleteBehavior.Restrict);
            modelBuilder.Entity<Career>().HasOne(c => c.CreatedBy).WithMany().HasForeignKey(c => c.CreatedById).OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Person>().ToTable("People");
            modelBuilder.Entity<Intern>().ToTable("Interns");
            modelBuilder.Entity<Extern>().ToTable("Externs");

            modelBuilder.Entity<EquipmentUnit>().ToTable(table => table.HasCheckConstraint(
                "CK_EquipmentUnits_LocationResolution",
                "([LaboratoryId] IS NULL AND [LocationResolutionStatus] = 0) OR ([LaboratoryId] IS NOT NULL AND [LocationResolutionStatus] = 1)"));
            modelBuilder.Entity<EquipmentUnit>().ToTable(table =>
            {
                table.HasCheckConstraint("CK_EquipmentUnits_CurrentStatus", "[CurrentStatus] IN (0, 1, 2, 3, 4, 5, 6, 10, 99)");
                table.HasCheckConstraint("CK_EquipmentUnits_PhysicalCondition", "[PhysicalCondition] IS NULL OR [PhysicalCondition] BETWEEN 1 AND 5");
                table.HasCheckConstraint("CK_EquipmentUnits_AcquisitionValue", "[AcquisitionValue] IS NULL OR [AcquisitionValue] >= 0");
            });
            modelBuilder.Entity<Request>().ToTable(table => table.HasCheckConstraint(
                "CK_Requests_LocationResolution",
                "([LaboratoryId] IS NULL AND [LocationResolutionStatus] = 0) OR ([LaboratoryId] IS NOT NULL AND [LocationResolutionStatus] = 1)"));
            modelBuilder.Entity<Request>().ToTable(table =>
            {
                table.HasCheckConstraint("CK_Requests_Priority", "[Priority] BETWEEN 0 AND 3");
                table.HasCheckConstraint("CK_Requests_Status", "[Status] IN (0, 1, 2, 3, 4, 5, 99)");
                table.HasCheckConstraint("CK_Requests_Type", "[Type] IN (1, 2, 3)");
            });
            modelBuilder.Entity<CostDetail>().ToTable(table =>
            {
                table.HasCheckConstraint("CK_CostDetails_ExactlyOneParent", "CASE WHEN [RequestId] IS NULL THEN 0 ELSE 1 END + CASE WHEN [MaintenanceId] IS NULL THEN 0 ELSE 1 END = 1");
                table.HasCheckConstraint("CK_CostDetails_PositiveValues", "[Quantity] > 0 AND [UnitPrice] >= 0");
                table.HasCheckConstraint("CK_CostDetails_Category", "[Category] IN (0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 99)");
            });
            modelBuilder.Entity<Maintenance>().ToTable(table =>
            {
                table.HasCheckConstraint("CK_Maintenances_CompletionPercentage", "[CompletionPercentage] BETWEEN 0 AND 100");
                table.HasCheckConstraint("CK_Maintenances_NonNegativeCosts", "([EstimatedCost] IS NULL OR [EstimatedCost] >= 0) AND ([ActualCost] IS NULL OR [ActualCost] >= 0)");
                table.HasCheckConstraint("CK_Maintenances_ExecutionDates", "[StartDate] IS NULL OR [EndDate] IS NULL OR [EndDate] >= [StartDate]");
                table.HasCheckConstraint("CK_Maintenances_Type", "[MaintenanceType] IN (1, 2, 3, 4, 5, 99)");
                table.HasCheckConstraint("CK_Maintenances_ServiceType", "[ServiceType] IN (0, 1)");
                table.HasCheckConstraint("CK_Maintenances_Status", "[Status] IN (0, 1, 2, 3, 99)");
                table.HasCheckConstraint("CK_Maintenances_Satisfaction", "[SatisfactionLevel] IS NULL OR [SatisfactionLevel] BETWEEN 1 AND 5");
            });
            modelBuilder.Entity<Management>().ToTable(table =>
            {
                table.HasCheckConstraint("CK_Managements_Year", "[Year] BETWEEN 2000 AND 2100");
                table.HasCheckConstraint("CK_Managements_Semester", "[Semester] IN (0, 1, 2)");
                table.HasCheckConstraint("CK_Managements_Status", "[Status] IN (0, 1, 2, 99)");
                table.HasCheckConstraint("CK_Managements_Type", "[Type] IN (0, 1)");
            });
            modelBuilder.Entity<ManagementPlan>().ToTable(table =>
            {
                table.HasCheckConstraint("CK_ManagementPlans_Weeks", "([PlannedWeek] IS NULL OR [PlannedWeek] BETWEEN 1 AND 8) AND ([ExecutedWeek] IS NULL OR [ExecutedWeek] BETWEEN 1 AND 8)");
                table.HasCheckConstraint("CK_ManagementPlans_Phase", "[CurrentPhase] BETWEEN 1 AND 6");
                table.HasCheckConstraint("CK_ManagementPlans_State", "[CurrentState] BETWEEN 1 AND 9");
                table.HasCheckConstraint("CK_ManagementPlans_Status", "[PlanStatus] BETWEEN 0 AND 3");
            });
            modelBuilder.Entity<VerificationCheckResult>().ToTable(table => table.HasCheckConstraint(
                "CK_VerificationCheckResults_Result", "[Result] IN (0, 1)"));
            modelBuilder.Entity<Verification>().ToTable(table =>
            {
                table.HasCheckConstraint("CK_Verifications_PhysicalCondition", "[PhysicalCondition] BETWEEN 1 AND 5");
                table.HasCheckConstraint("CK_Verifications_Status", "[Status] IN (0, 1, 2, 3, 99)");
                table.HasCheckConstraint("CK_Verifications_ObservedEquipmentStatus", "[ObservedEquipmentStatus] IS NULL OR [ObservedEquipmentStatus] IN (0, 1, 2, 3, 4, 5, 6, 10, 99)");
            });
            modelBuilder.Entity<Equipment>().ToTable(table =>
            {
                table.HasCheckConstraint("CK_Equipments_Category", "[Category] BETWEEN 0 AND 2");
                table.HasCheckConstraint("CK_Equipments_UtensilType", "[UtensilType] IS NULL OR [UtensilType] BETWEEN 0 AND 11");
                table.HasCheckConstraint("CK_Equipments_TypeClassification", "[TypeClassification] IS NULL OR [TypeClassification] BETWEEN 0 AND 15");
                table.HasCheckConstraint("CK_Equipments_ClassificationReviewStatus", "[ClassificationReviewStatus] IN (0, 1, 2)");
                table.HasCheckConstraint(
                    "CK_Equipments_ConfirmedClassificationHierarchy",
                    "[ClassificationReviewStatus] <> 2 OR " +
                    "(([Category] = 0 AND [TypeClassification] IS NOT NULL AND [UtensilType] IS NULL " +
                    "AND ([TypeClassification] <> 7 OR LEN(LTRIM(RTRIM(COALESCE([OtherClassificationDetail], '')))) > 0)) " +
                    "OR ([Category] = 1 AND [TypeClassification] IS NULL AND [UtensilType] BETWEEN 1 AND 11 " +
                    "AND ([UtensilType] <> 11 OR LEN(LTRIM(RTRIM(COALESCE([OtherClassificationDetail], '')))) > 0)) " +
                    "OR ([Category] = 2 AND [TypeClassification] IS NULL AND [UtensilType] IS NULL " +
                    "AND LEN(LTRIM(RTRIM(COALESCE([OtherClassificationDetail], '')))) > 0))");
                table.HasCheckConstraint("CK_Equipments_Status", "[Status] BETWEEN 0 AND 2");
                table.HasCheckConstraint("CK_Equipments_UsefulLife", "[UsefulLifeYears] IS NULL OR [UsefulLifeYears] >= 0");
            });
            modelBuilder.Entity<EquipmentClassificationDecision>().ToTable(table =>
            {
                table.HasCheckConstraint("CK_EquipmentClassificationDecisions_Status", "[ReviewStatus] IN (0, 1, 2)");
                table.HasCheckConstraint(
                    "CK_EquipmentClassificationDecisions_Dates",
                    "[EffectiveTo] IS NULL OR [EffectiveFrom] IS NULL OR [EffectiveTo] > [EffectiveFrom]");
                table.HasCheckConstraint(
                    "CK_EquipmentClassificationDecisions_Confirmed",
                    "[ReviewStatus] <> 2 OR (" +
                    "[GeneralStatus] IS NOT NULL AND [DecisionDate] IS NOT NULL " +
                    "AND LEN(LTRIM(RTRIM(COALESCE([EvidenceReference], '')))) > 0 " +
                    "AND LEN(LTRIM(RTRIM(COALESCE([ResponsibleSnapshot], '')))) > 0 " +
                    "AND (([Category] = 0 AND [TypeClassification] IS NOT NULL AND [UtensilType] IS NULL " +
                    "AND ([TypeClassification] <> 7 OR LEN(LTRIM(RTRIM(COALESCE([OtherDetail], '')))) > 0)) " +
                    "OR ([Category] = 1 AND [TypeClassification] IS NULL AND [UtensilType] BETWEEN 1 AND 11 " +
                    "AND ([UtensilType] <> 11 OR LEN(LTRIM(RTRIM(COALESCE([OtherDetail], '')))) > 0)) " +
                    "OR ([Category] = 2 AND [TypeClassification] IS NULL AND [UtensilType] IS NULL " +
                    "AND LEN(LTRIM(RTRIM(COALESCE([OtherDetail], '')))) > 0)))");
            });
            modelBuilder.Entity<User>().ToTable("Users", table =>
            {
                table.HasCheckConstraint("CK_Users_Role", "[Role] IN (1, 2, 99)");
                table.HasCheckConstraint("CK_Users_Status", "[Status] BETWEEN 0 AND 2");
            });
            modelBuilder.Entity<Person>().ToTable("People", table =>
            {
                table.HasCheckConstraint("CK_People_Status", "[Status] BETWEEN 0 AND 2");
                table.HasCheckConstraint("CK_People_Category", "[Category] IN (1, 2, 3, 4, 5, 99)");
            });
            modelBuilder.Entity<Laboratory>().ToTable(table => table.HasCheckConstraint("CK_Laboratories_Status", "[Status] BETWEEN 0 AND 2"));
            modelBuilder.Entity<Faculty>().ToTable(table => table.HasCheckConstraint("CK_Faculties_Status", "[Status] BETWEEN 0 AND 2"));
            modelBuilder.Entity<Career>().ToTable(table => table.HasCheckConstraint("CK_Careers_Status", "[Status] BETWEEN 0 AND 2"));
            modelBuilder.Entity<Country>().ToTable(table => table.HasCheckConstraint("CK_Countries_Status", "[Status] BETWEEN 0 AND 2"));
            modelBuilder.Entity<City>().ToTable(table => table.HasCheckConstraint("CK_Cities_Status", "[Status] BETWEEN 0 AND 2"));
            modelBuilder.Entity<Intern>().ToTable("Interns", table => table.HasCheckConstraint("CK_Interns_Status", "[InternStatus] BETWEEN 0 AND 2"));
            modelBuilder.Entity<Extern>().ToTable("Externs", table => table.HasCheckConstraint("CK_Externs_Status", "[ExternStatus] BETWEEN 0 AND 2"));
            modelBuilder.Entity<Departure>().ToTable(table =>
            {
                table.HasCheckConstraint("CK_Departures_Type", "[Type] IN (1, 2, 3, 4, 5)");
                table.HasCheckConstraint("CK_Departures_Status", "[Status] IN (0, 1, 2, 99)");
            });
            modelBuilder.Entity<DepartureItem>().ToTable(table =>
            {
                table.HasCheckConstraint(
                    "CK_DepartureItems_ExactlyOneReference",
                    "CASE WHEN [EquipmentUnitId] IS NULL THEN 0 ELSE 1 END + CASE WHEN [ArticleId] IS NULL THEN 0 ELSE 1 END = 1");
                table.HasCheckConstraint("CK_DepartureItems_Quantity", "[Quantity] IS NULL OR [Quantity] > 0");
            });
            modelBuilder.Entity<Article>().ToTable(table => table.HasCheckConstraint(
                "CK_Articles_Status", "[Status] BETWEEN 0 AND 2"));
            modelBuilder.Entity<RequestEquipmentUnit>().ToTable(table => table.HasCheckConstraint(
                "CK_RequestEquipmentUnits_Activation",
                "(([IsActive] = 1 AND [DeactivatedDate] IS NULL) OR ([IsActive] = 0 AND [DeactivatedDate] IS NOT NULL)) AND ([IsLegacyPrimary] = 0 OR [IsActive] = 1)"));
            modelBuilder.Entity<MaintenanceRequest>().ToTable(table => table.HasCheckConstraint(
                "CK_MaintenanceRequests_Activation",
                "(([IsActive] = 1 AND [DeactivatedDate] IS NULL) OR ([IsActive] = 0 AND [DeactivatedDate] IS NOT NULL)) AND ([IsLegacyPrimary] = 0 OR [IsActive] = 1)"));
            modelBuilder.Entity<ImportSourceRow>().ToTable(table =>
            {
                table.HasCheckConstraint("CK_ImportSourceRows_SourceRow", "[SourceRowNumber] > 0");
                table.HasCheckConstraint("CK_ImportSourceRows_MigrationStatus", "[MigrationStatus] IN (0, 1, 2, 3, 99)");
                table.HasCheckConstraint("CK_ImportSourceRows_ReconciliationStatus", "[ReconciliationStatus] BETWEEN 0 AND 6");
            });
            modelBuilder.Entity<EquipmentStateHistory>().ToTable(table =>
            {
                table.HasCheckConstraint("CK_EquipmentStateHistories_Status", "[Status] IN (0, 1, 2, 3, 4, 5, 6, 10, 99)");
                table.HasCheckConstraint("CK_EquipmentStateHistories_Dates", "[EndDate] IS NULL OR [EndDate] >= [StartDate]");
            });
            modelBuilder.Entity<MaintenancePlan>().ToTable(table =>
            {
                table.HasCheckConstraint("CK_MaintenancePlans_ServiceType", "[ServiceType] IS NULL OR [ServiceType] IN (0, 1)");
                table.HasCheckConstraint("CK_MaintenancePlans_MaintenanceType", "[MaintenanceType] IS NULL OR [MaintenanceType] IN (1, 2, 3, 4, 5, 99)");
                table.HasCheckConstraint("CK_MaintenancePlans_Status", "[Status] IS NULL OR [Status] IN (0, 1, 2, 3, 99)");
                table.HasCheckConstraint("CK_MaintenancePlans_Times", "([EstimatedTime] IS NULL OR [EstimatedTime] >= 0) AND ([ActualTime] IS NULL OR [ActualTime] >= 0)");
            });
            modelBuilder.Entity<MaintenanceParticipant>().ToTable(table =>
            {
                table.HasCheckConstraint("CK_MaintenanceParticipants_Role", "[Role] IN (1, 2, 3, 4)");
                table.HasCheckConstraint("CK_MaintenanceParticipants_PrimaryRole", "[IsPrimary] = 0 OR [Role] = 1");
                table.HasCheckConstraint("CK_MaintenanceParticipants_Dates", "[UnassignedAt] IS NULL OR [UnassignedAt] >= [AssignedAt]");
            });
            modelBuilder.Entity<PersonRoleAssignment>().ToTable(table =>
            {
                table.HasCheckConstraint("CK_PersonRoleAssignments_Role", "[Role] IN (1, 2, 3, 4, 5, 6, 7, 99)");
                table.HasCheckConstraint("CK_PersonRoleAssignments_Dates", "[ValidTo] IS NULL OR [ValidTo] >= [ValidFrom]");
            });
            modelBuilder.Entity<ImportBatch>().ToTable(table => table.HasCheckConstraint(
                "CK_ImportBatches_Status", "[Status] IN (0, 1, 2, 99)"));
            modelBuilder.Entity<DataQualityIssue>().ToTable(table =>
            {
                table.HasCheckConstraint("CK_DataQualityIssues_Severity", "[Severity] BETWEEN 0 AND 3");
                table.HasCheckConstraint("CK_DataQualityIssues_Status", "[Status] BETWEEN 0 AND 3");
                table.HasCheckConstraint("CK_DataQualityIssues_SourceRow", "[SourceRowNumber] IS NULL OR [SourceRowNumber] > 0");
            });
            
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
