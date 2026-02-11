IF OBJECT_ID(N'[__EFMigrationsHistory]') IS NULL
BEGIN
    CREATE TABLE [__EFMigrationsHistory] (
        [MigrationId] nvarchar(150) NOT NULL,
        [ProductVersion] nvarchar(32) NOT NULL,
        CONSTRAINT [PK___EFMigrationsHistory] PRIMARY KEY ([MigrationId])
    );
END;
GO

BEGIN TRANSACTION;
IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260114155048_InitialCreate'
)
BEGIN
    CREATE TABLE [Roles] (
        [Id] int NOT NULL IDENTITY,
        [Name] nvarchar(256) NULL,
        [NormalizedName] nvarchar(256) NULL,
        [ConcurrencyStamp] nvarchar(max) NULL,
        CONSTRAINT [PK_Roles] PRIMARY KEY ([Id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260114155048_InitialCreate'
)
BEGIN
    CREATE TABLE [ScaffoldTest] (
        [Id] int NOT NULL IDENTITY,
        [Name] nvarchar(max) NOT NULL,
        CONSTRAINT [PK_ScaffoldTest] PRIMARY KEY ([Id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260114155048_InitialCreate'
)
BEGIN
    CREATE TABLE [Users] (
        [Id] int NOT NULL IDENTITY,
        [UserName] nvarchar(256) NOT NULL,
        [Email] nvarchar(256) NOT NULL,
        [PhoneNumber] nvarchar(max) NOT NULL,
        [FirstName] nvarchar(100) NOT NULL,
        [LastName] nvarchar(100) NOT NULL,
        [SecondLastName] nvarchar(100) NULL,
        [IdentityCard] nvarchar(10) NOT NULL,
        [Role] int NOT NULL,
        [Status] int NOT NULL DEFAULT 0,
        [Position] nvarchar(100) NULL,
        [Department] nvarchar(100) NULL,
        [HireDate] datetime2 NULL,
        [CreatedById] int NULL,
        [CreatedDate] datetime2 NOT NULL,
        [ModifiedById] int NULL,
        [LastModifiedDate] datetime2 NULL,
        [NormalizedUserName] nvarchar(256) NULL,
        [NormalizedEmail] nvarchar(256) NULL,
        [EmailConfirmed] bit NOT NULL,
        [PasswordHash] nvarchar(max) NULL,
        [SecurityStamp] nvarchar(max) NULL,
        [ConcurrencyStamp] nvarchar(max) NULL,
        [PhoneNumberConfirmed] bit NOT NULL,
        [TwoFactorEnabled] bit NOT NULL,
        [LockoutEnd] datetimeoffset NULL,
        [LockoutEnabled] bit NOT NULL,
        [AccessFailedCount] int NOT NULL,
        CONSTRAINT [PK_Users] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_Users_Users_CreatedById] FOREIGN KEY ([CreatedById]) REFERENCES [Users] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_Users_Users_ModifiedById] FOREIGN KEY ([ModifiedById]) REFERENCES [Users] ([Id]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260114155048_InitialCreate'
)
BEGIN
    CREATE TABLE [RoleClaims] (
        [Id] int NOT NULL IDENTITY,
        [RoleId] int NOT NULL,
        [ClaimType] nvarchar(max) NULL,
        [ClaimValue] nvarchar(max) NULL,
        CONSTRAINT [PK_RoleClaims] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_RoleClaims_Roles_RoleId] FOREIGN KEY ([RoleId]) REFERENCES [Roles] ([Id]) ON DELETE CASCADE
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260114155048_InitialCreate'
)
BEGIN
    CREATE TABLE [Countries] (
        [Id] int NOT NULL IDENTITY,
        [Name] nvarchar(100) NOT NULL,
        [Status] int NOT NULL DEFAULT 0,
        [CreatedDate] datetime2 NOT NULL,
        [CreatedById] int NULL,
        [ModifiedById] int NULL,
        [LastModifiedDate] datetime2 NULL,
        CONSTRAINT [PK_Countries] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_Countries_Users_CreatedById] FOREIGN KEY ([CreatedById]) REFERENCES [Users] ([Id]),
        CONSTRAINT [FK_Countries_Users_ModifiedById] FOREIGN KEY ([ModifiedById]) REFERENCES [Users] ([Id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260114155048_InitialCreate'
)
BEGIN
    CREATE TABLE [EquipmentTypes] (
        [Id] int NOT NULL IDENTITY,
        [Name] nvarchar(100) NOT NULL,
        [Description] nvarchar(500) NULL,
        [RequiresCalibration] bit NOT NULL,
        [MaintenanceFrequencyMonths] int NULL,
        [CreatedById] int NULL,
        [CreatedDate] datetime2 NOT NULL,
        [ModifiedById] int NULL,
        [LastModifiedDate] datetime2 NULL,
        CONSTRAINT [PK_EquipmentTypes] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_EquipmentTypes_Users_CreatedById] FOREIGN KEY ([CreatedById]) REFERENCES [Users] ([Id]),
        CONSTRAINT [FK_EquipmentTypes_Users_ModifiedById] FOREIGN KEY ([ModifiedById]) REFERENCES [Users] ([Id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260114155048_InitialCreate'
)
BEGIN
    CREATE TABLE [Faculties] (
        [Id] int NOT NULL IDENTITY,
        [Name] nvarchar(200) NOT NULL,
        [Code] nvarchar(50) NULL,
        [Description] nvarchar(500) NULL,
        [Status] int NOT NULL DEFAULT 0,
        [CreatedDate] datetime2 NOT NULL,
        [CreatedById] int NULL,
        [ModifiedById] int NULL,
        [LastModifiedDate] datetime2 NULL,
        CONSTRAINT [PK_Faculties] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_Faculties_Users_CreatedById] FOREIGN KEY ([CreatedById]) REFERENCES [Users] ([Id]),
        CONSTRAINT [FK_Faculties_Users_ModifiedById] FOREIGN KEY ([ModifiedById]) REFERENCES [Users] ([Id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260114155048_InitialCreate'
)
BEGIN
    CREATE TABLE [MaintenanceTypes] (
        [Id] int NOT NULL IDENTITY,
        [Name] nvarchar(100) NOT NULL,
        [Description] nvarchar(500) NULL,
        [CreatedById] int NULL,
        [CreatedDate] datetime2 NOT NULL,
        [ModifiedById] int NULL,
        [LastModifiedDate] datetime2 NULL,
        CONSTRAINT [PK_MaintenanceTypes] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_MaintenanceTypes_Users_CreatedById] FOREIGN KEY ([CreatedById]) REFERENCES [Users] ([Id]),
        CONSTRAINT [FK_MaintenanceTypes_Users_ModifiedById] FOREIGN KEY ([ModifiedById]) REFERENCES [Users] ([Id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260114155048_InitialCreate'
)
BEGIN
    CREATE TABLE [People] (
        [Id] int NOT NULL IDENTITY,
        [FirstName] nvarchar(100) NOT NULL,
        [LastName] nvarchar(100) NOT NULL,
        [Type] int NOT NULL,
        [JobTitle] nvarchar(100) NULL,
        [Company] nvarchar(200) NULL,
        [Email] nvarchar(100) NULL,
        [PhoneNumber] nvarchar(20) NULL,
        [CreatedDate] datetime2 NOT NULL,
        [CreatedById] int NULL,
        [ModifiedById] int NULL,
        [LastModifiedDate] datetime2 NULL,
        CONSTRAINT [PK_People] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_People_Users_CreatedById] FOREIGN KEY ([CreatedById]) REFERENCES [Users] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_People_Users_ModifiedById] FOREIGN KEY ([ModifiedById]) REFERENCES [Users] ([Id]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260114155048_InitialCreate'
)
BEGIN
    CREATE TABLE [UserClaims] (
        [Id] int NOT NULL IDENTITY,
        [UserId] int NOT NULL,
        [ClaimType] nvarchar(max) NULL,
        [ClaimValue] nvarchar(max) NULL,
        CONSTRAINT [PK_UserClaims] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_UserClaims_Users_UserId] FOREIGN KEY ([UserId]) REFERENCES [Users] ([Id]) ON DELETE CASCADE
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260114155048_InitialCreate'
)
BEGIN
    CREATE TABLE [UserLogins] (
        [LoginProvider] nvarchar(450) NOT NULL,
        [ProviderKey] nvarchar(450) NOT NULL,
        [ProviderDisplayName] nvarchar(max) NULL,
        [UserId] int NOT NULL,
        CONSTRAINT [PK_UserLogins] PRIMARY KEY ([LoginProvider], [ProviderKey]),
        CONSTRAINT [FK_UserLogins_Users_UserId] FOREIGN KEY ([UserId]) REFERENCES [Users] ([Id]) ON DELETE CASCADE
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260114155048_InitialCreate'
)
BEGIN
    CREATE TABLE [UserRoles] (
        [UserId] int NOT NULL,
        [RoleId] int NOT NULL,
        CONSTRAINT [PK_UserRoles] PRIMARY KEY ([UserId], [RoleId]),
        CONSTRAINT [FK_UserRoles_Roles_RoleId] FOREIGN KEY ([RoleId]) REFERENCES [Roles] ([Id]) ON DELETE CASCADE,
        CONSTRAINT [FK_UserRoles_Users_UserId] FOREIGN KEY ([UserId]) REFERENCES [Users] ([Id]) ON DELETE CASCADE
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260114155048_InitialCreate'
)
BEGIN
    CREATE TABLE [UserTokens] (
        [UserId] int NOT NULL,
        [LoginProvider] nvarchar(450) NOT NULL,
        [Name] nvarchar(450) NOT NULL,
        [Value] nvarchar(max) NULL,
        CONSTRAINT [PK_UserTokens] PRIMARY KEY ([UserId], [LoginProvider], [Name]),
        CONSTRAINT [FK_UserTokens_Users_UserId] FOREIGN KEY ([UserId]) REFERENCES [Users] ([Id]) ON DELETE CASCADE
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260114155048_InitialCreate'
)
BEGIN
    CREATE TABLE [Cities] (
        [Id] int NOT NULL IDENTITY,
        [CountryId] int NOT NULL,
        [Name] nvarchar(100) NOT NULL,
        [Region] nvarchar(100) NULL,
        [Status] int NOT NULL DEFAULT 0,
        [CreatedDate] datetime2 NOT NULL,
        [CreatedById] int NULL,
        [ModifiedById] int NULL,
        [LastModifiedDate] datetime2 NULL,
        CONSTRAINT [PK_Cities] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_Cities_Countries_CountryId] FOREIGN KEY ([CountryId]) REFERENCES [Countries] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_Cities_Users_CreatedById] FOREIGN KEY ([CreatedById]) REFERENCES [Users] ([Id]),
        CONSTRAINT [FK_Cities_Users_ModifiedById] FOREIGN KEY ([ModifiedById]) REFERENCES [Users] ([Id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260114155048_InitialCreate'
)
BEGIN
    CREATE TABLE [Laboratories] (
        [Id] int NOT NULL IDENTITY,
        [FacultyId] int NOT NULL,
        [Code] nvarchar(20) NOT NULL,
        [Name] nvarchar(200) NOT NULL,
        [Type] nvarchar(100) NULL,
        [Building] nvarchar(100) NULL,
        [Floor] nvarchar(50) NULL,
        [Description] nvarchar(1000) NULL,
        [Status] int NOT NULL DEFAULT 0,
        [CreatedById] int NULL,
        [CreatedDate] datetime2 NOT NULL,
        [ModifiedById] int NULL,
        [LastModifiedDate] datetime2 NULL,
        CONSTRAINT [PK_Laboratories] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_Laboratories_Faculties_FacultyId] FOREIGN KEY ([FacultyId]) REFERENCES [Faculties] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_Laboratories_Users_CreatedById] FOREIGN KEY ([CreatedById]) REFERENCES [Users] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_Laboratories_Users_ModifiedById] FOREIGN KEY ([ModifiedById]) REFERENCES [Users] ([Id]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260114155048_InitialCreate'
)
BEGIN
    CREATE TABLE [Equipments] (
        [Id] int NOT NULL IDENTITY,
        [EquipmentTypeId] int NOT NULL,
        [LaboratoryId] int NOT NULL,
        [CityId] int NOT NULL,
        [CountryId] int NOT NULL,
        [Name] nvarchar(200) NOT NULL,
        [InventoryNumber] nvarchar(50) NOT NULL,
        [Brand] nvarchar(100) NULL,
        [Model] nvarchar(100) NULL,
        [SerialNumber] nvarchar(100) NULL,
        [UsefulLifeYears] int NULL,
        [AcquisitionDate] datetime2 NULL,
        [AcquisitionValue] decimal(18,2) NULL,
        [CurrentStatus] int NOT NULL DEFAULT 0,
        [Observations] nvarchar(2000) NULL,
        [CreatedById] int NULL,
        [CreatedDate] datetime2 NOT NULL,
        [ModifiedById] int NULL,
        [LastModifiedDate] datetime2 NULL,
        CONSTRAINT [PK_Equipments] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_Equipments_Cities_CityId] FOREIGN KEY ([CityId]) REFERENCES [Cities] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_Equipments_Countries_CountryId] FOREIGN KEY ([CountryId]) REFERENCES [Countries] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_Equipments_EquipmentTypes_EquipmentTypeId] FOREIGN KEY ([EquipmentTypeId]) REFERENCES [EquipmentTypes] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_Equipments_Laboratories_LaboratoryId] FOREIGN KEY ([LaboratoryId]) REFERENCES [Laboratories] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_Equipments_Users_CreatedById] FOREIGN KEY ([CreatedById]) REFERENCES [Users] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_Equipments_Users_ModifiedById] FOREIGN KEY ([ModifiedById]) REFERENCES [Users] ([Id]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260114155048_InitialCreate'
)
BEGIN
    CREATE TABLE [EquipmentStateHistories] (
        [Id] int NOT NULL IDENTITY,
        [EquipmentId] int NOT NULL,
        [Status] int NOT NULL DEFAULT 0,
        [StartDate] datetime2 NOT NULL,
        [EndDate] datetime2 NULL,
        [Reason] nvarchar(500) NULL,
        [CreatedById] int NULL,
        [CreatedDate] datetime2 NOT NULL,
        [ModifiedById] int NULL,
        [LastModifiedDate] datetime2 NULL,
        CONSTRAINT [PK_EquipmentStateHistories] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_EquipmentStateHistories_Equipments_EquipmentId] FOREIGN KEY ([EquipmentId]) REFERENCES [Equipments] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_EquipmentStateHistories_Users_CreatedById] FOREIGN KEY ([CreatedById]) REFERENCES [Users] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_EquipmentStateHistories_Users_ModifiedById] FOREIGN KEY ([ModifiedById]) REFERENCES [Users] ([Id]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260114155048_InitialCreate'
)
BEGIN
    CREATE TABLE [MaintenancePlans] (
        [Id] int NOT NULL IDENTITY,
        [LaboratorySnapshot] nvarchar(200) NOT NULL,
        [BlockSnapshot] nvarchar(200) NOT NULL,
        [EquipmentId] int NOT NULL,
        [Service] nvarchar(200) NOT NULL,
        [ServiceType] int NOT NULL,
        [CreatedById] int NULL,
        [ModifiedById] int NULL,
        [LastModifiedDate] datetime2 NULL,
        [EstimatedTime] decimal(10,2) NULL,
        [ActualTime] decimal(10,2) NULL,
        [AssignedTechnicianId] int NULL,
        [CreatedDate] datetime2 NOT NULL,
        [LaboratoryId] int NULL,
        CONSTRAINT [PK_MaintenancePlans] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_MaintenancePlans_Equipments_EquipmentId] FOREIGN KEY ([EquipmentId]) REFERENCES [Equipments] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_MaintenancePlans_Laboratories_LaboratoryId] FOREIGN KEY ([LaboratoryId]) REFERENCES [Laboratories] ([Id]),
        CONSTRAINT [FK_MaintenancePlans_Users_AssignedTechnicianId] FOREIGN KEY ([AssignedTechnicianId]) REFERENCES [Users] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_MaintenancePlans_Users_CreatedById] FOREIGN KEY ([CreatedById]) REFERENCES [Users] ([Id]),
        CONSTRAINT [FK_MaintenancePlans_Users_ModifiedById] FOREIGN KEY ([ModifiedById]) REFERENCES [Users] ([Id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260114155048_InitialCreate'
)
BEGIN
    CREATE TABLE [Requests] (
        [Id] int NOT NULL IDENTITY,
        [EquipmentId] int NOT NULL,
        [RequestedById] int NULL,
        [Description] nvarchar(1000) NOT NULL,
        [Priority] int NOT NULL,
        [Observations] nvarchar(500) NULL,
        [Status] int NOT NULL DEFAULT 0,
        [ApprovedById] int NULL,
        [ApprovalDate] datetime2 NULL,
        [RejectionReason] nvarchar(500) NULL,
        [CreatedById] int NULL,
        [CreatedDate] datetime2 NOT NULL,
        [ModifiedById] int NULL,
        [LastModifiedDate] datetime2 NULL,
        CONSTRAINT [PK_Requests] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_Requests_Equipments_EquipmentId] FOREIGN KEY ([EquipmentId]) REFERENCES [Equipments] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_Requests_Users_ApprovedById] FOREIGN KEY ([ApprovedById]) REFERENCES [Users] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_Requests_Users_CreatedById] FOREIGN KEY ([CreatedById]) REFERENCES [Users] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_Requests_Users_ModifiedById] FOREIGN KEY ([ModifiedById]) REFERENCES [Users] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_Requests_Users_RequestedById] FOREIGN KEY ([RequestedById]) REFERENCES [Users] ([Id]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260114155048_InitialCreate'
)
BEGIN
    CREATE TABLE [Verifications] (
        [Id] int NOT NULL IDENTITY,
        [EquipmentId] int NOT NULL,
        [CablingCheck] int NOT NULL,
        [GasHoseCheck] int NOT NULL,
        [WaterHoseCheck] int NOT NULL,
        [BurnerCheck] int NOT NULL,
        [HeatExchangerCheck] int NOT NULL,
        [FlameSensorCheck] int NOT NULL,
        [ElectrodeIgniterCheck] int NOT NULL,
        [FanCheck] int NOT NULL,
        [CombustionFlameCheck] int NOT NULL,
        [LubricationCheck] int NOT NULL,
        [OvenIgnitionCheck] int NOT NULL,
        [TemperatureControlCheck] int NOT NULL,
        [InternalCleaningCheck] int NOT NULL,
        [ExternalCleaningCheck] int NOT NULL,
        [LightsCheck] int NOT NULL,
        [HighTempSteamCheck] int NOT NULL,
        [LedDisplayCheck] int NOT NULL,
        [SolenoidValveCheck] int NOT NULL,
        [SoundAlarmCheck] int NOT NULL,
        [ThermocoupleCheck] int NOT NULL,
        [SteamOutletCheck] int NOT NULL,
        [Date] datetime2 NOT NULL,
        [Observations] nvarchar(2000) NULL,
        [CriticalFindings] nvarchar(1000) NULL,
        [Recommendations] nvarchar(1000) NULL,
        [Status] int NOT NULL DEFAULT 0,
        [CreatedById] int NULL,
        [CreatedDate] datetime2 NOT NULL,
        [ModifiedById] int NULL,
        [LastModifiedDate] datetime2 NULL,
        CONSTRAINT [PK_Verifications] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_Verifications_Equipments_EquipmentId] FOREIGN KEY ([EquipmentId]) REFERENCES [Equipments] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_Verifications_Users_CreatedById] FOREIGN KEY ([CreatedById]) REFERENCES [Users] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_Verifications_Users_ModifiedById] FOREIGN KEY ([ModifiedById]) REFERENCES [Users] ([Id]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260114155048_InitialCreate'
)
BEGIN
    CREATE TABLE [Maintenances] (
        [Id] int NOT NULL IDENTITY,
        [EquipmentId] int NOT NULL,
        [MaintenanceTypeId] int NOT NULL,
        [TechnicianId] int NULL,
        [RequestId] int NULL,
        [ScheduledDate] datetime2 NULL,
        [StartDate] datetime2 NULL,
        [EndDate] datetime2 NULL,
        [Description] nvarchar(2000) NULL,
        [Status] int NOT NULL DEFAULT 0,
        [EstimatedCost] decimal(18,2) NULL,
        [ActualCost] decimal(18,2) NULL,
        [Recommendations] nvarchar(1000) NULL,
        [SuggestedNextMaintenanceDate] datetime2 NULL,
        [Observations] nvarchar(1000) NULL,
        [CreatedById] int NULL,
        [CreatedDate] datetime2 NOT NULL,
        [ModifiedById] int NULL,
        [LastModifiedDate] datetime2 NULL,
        CONSTRAINT [PK_Maintenances] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_Maintenances_Equipments_EquipmentId] FOREIGN KEY ([EquipmentId]) REFERENCES [Equipments] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_Maintenances_MaintenanceTypes_MaintenanceTypeId] FOREIGN KEY ([MaintenanceTypeId]) REFERENCES [MaintenanceTypes] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_Maintenances_Requests_RequestId] FOREIGN KEY ([RequestId]) REFERENCES [Requests] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_Maintenances_Users_CreatedById] FOREIGN KEY ([CreatedById]) REFERENCES [Users] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_Maintenances_Users_ModifiedById] FOREIGN KEY ([ModifiedById]) REFERENCES [Users] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_Maintenances_Users_TechnicianId] FOREIGN KEY ([TechnicianId]) REFERENCES [Users] ([Id]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260114155048_InitialCreate'
)
BEGIN
    CREATE TABLE [CostDetails] (
        [Id] int NOT NULL IDENTITY,
        [MaintenanceId] int NOT NULL,
        [Concept] nvarchar(200) NOT NULL,
        [Description] nvarchar(500) NULL,
        [Quantity] decimal(10,2) NOT NULL,
        [UnitOfMeasure] nvarchar(50) NULL,
        [UnitPrice] decimal(18,2) NOT NULL,
        [Category] int NOT NULL,
        [Provider] nvarchar(200) NULL,
        [InvoiceNumber] nvarchar(100) NULL,
        [CreatedById] int NULL,
        [CreatedDate] datetime2 NOT NULL,
        [ModifiedById] int NULL,
        [LastModifiedDate] datetime2 NULL,
        CONSTRAINT [PK_CostDetails] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_CostDetails_Maintenances_MaintenanceId] FOREIGN KEY ([MaintenanceId]) REFERENCES [Maintenances] ([Id]) ON DELETE CASCADE,
        CONSTRAINT [FK_CostDetails_Users_CreatedById] FOREIGN KEY ([CreatedById]) REFERENCES [Users] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_CostDetails_Users_ModifiedById] FOREIGN KEY ([ModifiedById]) REFERENCES [Users] ([Id]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260114155048_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_Cities_CountryId] ON [Cities] ([CountryId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260114155048_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_Cities_CreatedById] ON [Cities] ([CreatedById]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260114155048_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_Cities_ModifiedById] ON [Cities] ([ModifiedById]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260114155048_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_CostDetails_CreatedById] ON [CostDetails] ([CreatedById]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260114155048_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_CostDetails_MaintenanceId] ON [CostDetails] ([MaintenanceId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260114155048_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_CostDetails_ModifiedById] ON [CostDetails] ([ModifiedById]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260114155048_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_Countries_CreatedById] ON [Countries] ([CreatedById]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260114155048_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_Countries_ModifiedById] ON [Countries] ([ModifiedById]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260114155048_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_Equipments_CityId] ON [Equipments] ([CityId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260114155048_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_Equipments_CountryId] ON [Equipments] ([CountryId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260114155048_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_Equipments_CreatedById] ON [Equipments] ([CreatedById]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260114155048_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_Equipments_EquipmentTypeId] ON [Equipments] ([EquipmentTypeId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260114155048_InitialCreate'
)
BEGIN
    CREATE UNIQUE INDEX [IX_Equipments_InventoryNumber] ON [Equipments] ([InventoryNumber]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260114155048_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_Equipments_LaboratoryId] ON [Equipments] ([LaboratoryId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260114155048_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_Equipments_ModifiedById] ON [Equipments] ([ModifiedById]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260114155048_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_EquipmentStateHistories_CreatedById] ON [EquipmentStateHistories] ([CreatedById]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260114155048_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_EquipmentStateHistories_EquipmentId] ON [EquipmentStateHistories] ([EquipmentId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260114155048_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_EquipmentStateHistories_ModifiedById] ON [EquipmentStateHistories] ([ModifiedById]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260114155048_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_EquipmentTypes_CreatedById] ON [EquipmentTypes] ([CreatedById]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260114155048_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_EquipmentTypes_ModifiedById] ON [EquipmentTypes] ([ModifiedById]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260114155048_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_Faculties_CreatedById] ON [Faculties] ([CreatedById]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260114155048_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_Faculties_ModifiedById] ON [Faculties] ([ModifiedById]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260114155048_InitialCreate'
)
BEGIN
    CREATE UNIQUE INDEX [IX_Laboratories_Code] ON [Laboratories] ([Code]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260114155048_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_Laboratories_CreatedById] ON [Laboratories] ([CreatedById]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260114155048_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_Laboratories_FacultyId] ON [Laboratories] ([FacultyId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260114155048_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_Laboratories_ModifiedById] ON [Laboratories] ([ModifiedById]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260114155048_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_MaintenancePlans_AssignedTechnicianId] ON [MaintenancePlans] ([AssignedTechnicianId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260114155048_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_MaintenancePlans_CreatedById] ON [MaintenancePlans] ([CreatedById]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260114155048_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_MaintenancePlans_EquipmentId] ON [MaintenancePlans] ([EquipmentId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260114155048_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_MaintenancePlans_LaboratoryId] ON [MaintenancePlans] ([LaboratoryId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260114155048_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_MaintenancePlans_ModifiedById] ON [MaintenancePlans] ([ModifiedById]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260114155048_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_Maintenances_CreatedById] ON [Maintenances] ([CreatedById]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260114155048_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_Maintenances_EquipmentId] ON [Maintenances] ([EquipmentId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260114155048_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_Maintenances_MaintenanceTypeId] ON [Maintenances] ([MaintenanceTypeId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260114155048_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_Maintenances_ModifiedById] ON [Maintenances] ([ModifiedById]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260114155048_InitialCreate'
)
BEGIN
    EXEC(N'CREATE UNIQUE INDEX [IX_Maintenances_RequestId] ON [Maintenances] ([RequestId]) WHERE [RequestId] IS NOT NULL');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260114155048_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_Maintenances_TechnicianId] ON [Maintenances] ([TechnicianId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260114155048_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_MaintenanceTypes_CreatedById] ON [MaintenanceTypes] ([CreatedById]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260114155048_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_MaintenanceTypes_ModifiedById] ON [MaintenanceTypes] ([ModifiedById]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260114155048_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_People_CreatedById] ON [People] ([CreatedById]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260114155048_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_People_ModifiedById] ON [People] ([ModifiedById]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260114155048_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_Requests_ApprovedById] ON [Requests] ([ApprovedById]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260114155048_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_Requests_CreatedById] ON [Requests] ([CreatedById]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260114155048_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_Requests_EquipmentId] ON [Requests] ([EquipmentId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260114155048_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_Requests_ModifiedById] ON [Requests] ([ModifiedById]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260114155048_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_Requests_RequestedById] ON [Requests] ([RequestedById]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260114155048_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_RoleClaims_RoleId] ON [RoleClaims] ([RoleId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260114155048_InitialCreate'
)
BEGIN
    EXEC(N'CREATE UNIQUE INDEX [RoleNameIndex] ON [Roles] ([NormalizedName]) WHERE [NormalizedName] IS NOT NULL');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260114155048_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_UserClaims_UserId] ON [UserClaims] ([UserId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260114155048_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_UserLogins_UserId] ON [UserLogins] ([UserId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260114155048_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_UserRoles_RoleId] ON [UserRoles] ([RoleId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260114155048_InitialCreate'
)
BEGIN
    CREATE INDEX [EmailIndex] ON [Users] ([NormalizedEmail]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260114155048_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_Users_CreatedById] ON [Users] ([CreatedById]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260114155048_InitialCreate'
)
BEGIN
    CREATE UNIQUE INDEX [IX_Users_IdentityCard] ON [Users] ([IdentityCard]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260114155048_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_Users_ModifiedById] ON [Users] ([ModifiedById]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260114155048_InitialCreate'
)
BEGIN
    EXEC(N'CREATE UNIQUE INDEX [UserNameIndex] ON [Users] ([NormalizedUserName]) WHERE [NormalizedUserName] IS NOT NULL');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260114155048_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_Verifications_CreatedById] ON [Verifications] ([CreatedById]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260114155048_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_Verifications_EquipmentId] ON [Verifications] ([EquipmentId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260114155048_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_Verifications_ModifiedById] ON [Verifications] ([ModifiedById]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260114155048_InitialCreate'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260114155048_InitialCreate', N'9.0.12');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260114161814_FilteredUniqueIndices'
)
BEGIN
    DROP INDEX [IX_Users_IdentityCard] ON [Users];
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260114161814_FilteredUniqueIndices'
)
BEGIN
    DROP INDEX [IX_Laboratories_Code] ON [Laboratories];
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260114161814_FilteredUniqueIndices'
)
BEGIN
    DROP INDEX [IX_Equipments_InventoryNumber] ON [Equipments];
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260114161814_FilteredUniqueIndices'
)
BEGIN
    EXEC(N'CREATE UNIQUE INDEX [IX_Users_IdentityCard] ON [Users] ([IdentityCard]) WHERE [Status] != 2');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260114161814_FilteredUniqueIndices'
)
BEGIN
    EXEC(N'CREATE UNIQUE INDEX [IX_Laboratories_Code] ON [Laboratories] ([Code]) WHERE [Status] != 2');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260114161814_FilteredUniqueIndices'
)
BEGIN
    EXEC(N'CREATE UNIQUE INDEX [IX_Equipments_InventoryNumber] ON [Equipments] ([InventoryNumber]) WHERE [CurrentStatus] != 99');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260114161814_FilteredUniqueIndices'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260114161814_FilteredUniqueIndices', N'9.0.12');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260115152033_Cookies'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260115152033_Cookies', N'9.0.12');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260121154032_AddIdentityCardToPerson'
)
BEGIN
    ALTER TABLE [People] ADD [IdentityCard] nvarchar(15) NOT NULL DEFAULT N'';
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260121154032_AddIdentityCardToPerson'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260121154032_AddIdentityCardToPerson', N'9.0.12');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260128190447_FixUsersPersons'
)
BEGIN
    EXEC sp_rename N'[People].[Type]', N'Category', 'COLUMN';
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260128190447_FixUsersPersons'
)
BEGIN
    ALTER TABLE [People] ADD [IsInternal] bit NOT NULL DEFAULT CAST(0 AS bit);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260128190447_FixUsersPersons'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260128190447_FixUsersPersons', N'9.0.12');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260128191247_MyBad'
)
BEGIN
    ALTER TABLE [Maintenances] DROP CONSTRAINT [FK_Maintenances_Users_TechnicianId];
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260128191247_MyBad'
)
BEGIN
    ALTER TABLE [Maintenances] ADD CONSTRAINT [FK_Maintenances_People_TechnicianId] FOREIGN KEY ([TechnicianId]) REFERENCES [People] ([Id]) ON DELETE NO ACTION;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260128191247_MyBad'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260128191247_MyBad', N'9.0.12');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260131130644_Okey'
)
BEGIN
    ALTER TABLE [Laboratories] ADD [CityId] int NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260131130644_Okey'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260131130644_Okey', N'9.0.12');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260205114134_LabUnivalle_VO'
)
BEGIN
    ALTER TABLE [People] ADD [Status] int NOT NULL DEFAULT 0;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260205114134_LabUnivalle_VO'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260205114134_LabUnivalle_VO', N'9.0.12');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260206155742_AddLaboratoryIdToRequests'
)
BEGIN
    ALTER TABLE [Requests] ADD [LaboratoryId] int NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260206155742_AddLaboratoryIdToRequests'
)
BEGIN

                    UPDATE r
                    SET r.LaboratoryId = e.LaboratoryId
                    FROM Requests r
                    INNER JOIN Equipments e ON r.EquipmentId = e.Id
                    WHERE r.LaboratoryId IS NULL
                
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260206155742_AddLaboratoryIdToRequests'
)
BEGIN
    DECLARE @var0 sysname;
    SELECT @var0 = [d].[name]
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[Requests]') AND [c].[name] = N'LaboratoryId');
    IF @var0 IS NOT NULL EXEC(N'ALTER TABLE [Requests] DROP CONSTRAINT [' + @var0 + '];');
    ALTER TABLE [Requests] ALTER COLUMN [LaboratoryId] int NOT NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260206155742_AddLaboratoryIdToRequests'
)
BEGIN
    CREATE INDEX [IX_Requests_LaboratoryId] ON [Requests] ([LaboratoryId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260206155742_AddLaboratoryIdToRequests'
)
BEGIN
    ALTER TABLE [Requests] ADD CONSTRAINT [FK_Requests_Laboratories_LaboratoryId] FOREIGN KEY ([LaboratoryId]) REFERENCES [Laboratories] ([Id]) ON DELETE CASCADE;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260206155742_AddLaboratoryIdToRequests'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260206155742_AddLaboratoryIdToRequests', N'9.0.12');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260206161117_AddEstimatedRepairTimeToRequests'
)
BEGIN
    ALTER TABLE [Requests] ADD [EstimatedRepairTime] nvarchar(100) NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260206161117_AddEstimatedRepairTimeToRequests'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260206161117_AddEstimatedRepairTimeToRequests', N'9.0.12');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260206162120_AddServiceStartEquipment'
)
BEGIN
    ALTER TABLE [Equipments] ADD [ServiceStartDate] datetime2 NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260206162120_AddServiceStartEquipment'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260206162120_AddServiceStartEquipment', N'9.0.12');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260206200958_AddCostDetailsAndRequestType'
)
BEGIN
    ALTER TABLE [Requests] ADD [InvestmentCode] nvarchar(50) NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260206200958_AddCostDetailsAndRequestType'
)
BEGIN
    ALTER TABLE [Requests] ADD [Type] int NOT NULL DEFAULT 0;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260206200958_AddCostDetailsAndRequestType'
)
BEGIN
    DECLARE @var1 sysname;
    SELECT @var1 = [d].[name]
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[CostDetails]') AND [c].[name] = N'MaintenanceId');
    IF @var1 IS NOT NULL EXEC(N'ALTER TABLE [CostDetails] DROP CONSTRAINT [' + @var1 + '];');
    ALTER TABLE [CostDetails] ALTER COLUMN [MaintenanceId] int NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260206200958_AddCostDetailsAndRequestType'
)
BEGIN
    ALTER TABLE [CostDetails] ADD [RequestId] int NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260206200958_AddCostDetailsAndRequestType'
)
BEGIN
    CREATE INDEX [IX_CostDetails_RequestId] ON [CostDetails] ([RequestId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260206200958_AddCostDetailsAndRequestType'
)
BEGIN
    ALTER TABLE [CostDetails] ADD CONSTRAINT [FK_CostDetails_Requests_RequestId] FOREIGN KEY ([RequestId]) REFERENCES [Requests] ([Id]) ON DELETE CASCADE;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260206200958_AddCostDetailsAndRequestType'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260206200958_AddCostDetailsAndRequestType', N'9.0.12');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260207151450_AddPhysicalConditionToEquipment'
)
BEGIN
    ALTER TABLE [Equipments] ADD [PhysicalCondition] int NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260207151450_AddPhysicalConditionToEquipment'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260207151450_AddPhysicalConditionToEquipment', N'9.0.12');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260207184257_AddPhysicUnits'
)
BEGIN
    ALTER TABLE [EquipmentStateHistories] DROP CONSTRAINT [FK_EquipmentStateHistories_Equipments_EquipmentId];
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260207184257_AddPhysicUnits'
)
BEGIN
    ALTER TABLE [MaintenancePlans] DROP CONSTRAINT [FK_MaintenancePlans_Equipments_EquipmentId];
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260207184257_AddPhysicUnits'
)
BEGIN
    ALTER TABLE [Maintenances] DROP CONSTRAINT [FK_Maintenances_Equipments_EquipmentId];
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260207184257_AddPhysicUnits'
)
BEGIN
    ALTER TABLE [Verifications] DROP CONSTRAINT [FK_Verifications_Equipments_EquipmentId];
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260207184257_AddPhysicUnits'
)
BEGIN
    DROP INDEX [IX_Equipments_InventoryNumber] ON [Equipments];
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260207184257_AddPhysicUnits'
)
BEGIN
    DECLARE @var2 sysname;
    SELECT @var2 = [d].[name]
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[Equipments]') AND [c].[name] = N'AcquisitionDate');
    IF @var2 IS NOT NULL EXEC(N'ALTER TABLE [Equipments] DROP CONSTRAINT [' + @var2 + '];');
    ALTER TABLE [Equipments] DROP COLUMN [AcquisitionDate];
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260207184257_AddPhysicUnits'
)
BEGIN
    DECLARE @var3 sysname;
    SELECT @var3 = [d].[name]
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[Equipments]') AND [c].[name] = N'AcquisitionValue');
    IF @var3 IS NOT NULL EXEC(N'ALTER TABLE [Equipments] DROP CONSTRAINT [' + @var3 + '];');
    ALTER TABLE [Equipments] DROP COLUMN [AcquisitionValue];
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260207184257_AddPhysicUnits'
)
BEGIN
    DECLARE @var4 sysname;
    SELECT @var4 = [d].[name]
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[Equipments]') AND [c].[name] = N'CurrentStatus');
    IF @var4 IS NOT NULL EXEC(N'ALTER TABLE [Equipments] DROP CONSTRAINT [' + @var4 + '];');
    ALTER TABLE [Equipments] DROP COLUMN [CurrentStatus];
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260207184257_AddPhysicUnits'
)
BEGIN
    DECLARE @var5 sysname;
    SELECT @var5 = [d].[name]
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[Equipments]') AND [c].[name] = N'InventoryNumber');
    IF @var5 IS NOT NULL EXEC(N'ALTER TABLE [Equipments] DROP CONSTRAINT [' + @var5 + '];');
    ALTER TABLE [Equipments] DROP COLUMN [InventoryNumber];
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260207184257_AddPhysicUnits'
)
BEGIN
    DECLARE @var6 sysname;
    SELECT @var6 = [d].[name]
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[Equipments]') AND [c].[name] = N'PhysicalCondition');
    IF @var6 IS NOT NULL EXEC(N'ALTER TABLE [Equipments] DROP CONSTRAINT [' + @var6 + '];');
    ALTER TABLE [Equipments] DROP COLUMN [PhysicalCondition];
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260207184257_AddPhysicUnits'
)
BEGIN
    DECLARE @var7 sysname;
    SELECT @var7 = [d].[name]
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[Equipments]') AND [c].[name] = N'SerialNumber');
    IF @var7 IS NOT NULL EXEC(N'ALTER TABLE [Equipments] DROP CONSTRAINT [' + @var7 + '];');
    ALTER TABLE [Equipments] DROP COLUMN [SerialNumber];
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260207184257_AddPhysicUnits'
)
BEGIN
    DECLARE @var8 sysname;
    SELECT @var8 = [d].[name]
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[Equipments]') AND [c].[name] = N'ServiceStartDate');
    IF @var8 IS NOT NULL EXEC(N'ALTER TABLE [Equipments] DROP CONSTRAINT [' + @var8 + '];');
    ALTER TABLE [Equipments] DROP COLUMN [ServiceStartDate];
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260207184257_AddPhysicUnits'
)
BEGIN
    EXEC sp_rename N'[Verifications].[EquipmentId]', N'EquipmentUnitId', 'COLUMN';
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260207184257_AddPhysicUnits'
)
BEGIN
    EXEC sp_rename N'[Verifications].[IX_Verifications_EquipmentId]', N'IX_Verifications_EquipmentUnitId', 'INDEX';
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260207184257_AddPhysicUnits'
)
BEGIN
    EXEC sp_rename N'[Maintenances].[EquipmentId]', N'EquipmentUnitId', 'COLUMN';
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260207184257_AddPhysicUnits'
)
BEGIN
    EXEC sp_rename N'[Maintenances].[IX_Maintenances_EquipmentId]', N'IX_Maintenances_EquipmentUnitId', 'INDEX';
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260207184257_AddPhysicUnits'
)
BEGIN
    EXEC sp_rename N'[MaintenancePlans].[EquipmentId]', N'EquipmentUnitId', 'COLUMN';
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260207184257_AddPhysicUnits'
)
BEGIN
    EXEC sp_rename N'[MaintenancePlans].[IX_MaintenancePlans_EquipmentId]', N'IX_MaintenancePlans_EquipmentUnitId', 'INDEX';
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260207184257_AddPhysicUnits'
)
BEGIN
    EXEC sp_rename N'[EquipmentStateHistories].[EquipmentId]', N'EquipmentUnitId', 'COLUMN';
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260207184257_AddPhysicUnits'
)
BEGIN
    EXEC sp_rename N'[EquipmentStateHistories].[IX_EquipmentStateHistories_EquipmentId]', N'IX_EquipmentStateHistories_EquipmentUnitId', 'INDEX';
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260207184257_AddPhysicUnits'
)
BEGIN
    EXEC sp_rename N'[Equipments].[Observations]', N'Description', 'COLUMN';
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260207184257_AddPhysicUnits'
)
BEGIN
    ALTER TABLE [Requests] ADD [EquipmentUnitId] int NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260207184257_AddPhysicUnits'
)
BEGIN
    DECLARE @var9 sysname;
    SELECT @var9 = [d].[name]
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[Equipments]') AND [c].[name] = N'CountryId');
    IF @var9 IS NOT NULL EXEC(N'ALTER TABLE [Equipments] DROP CONSTRAINT [' + @var9 + '];');
    ALTER TABLE [Equipments] ALTER COLUMN [CountryId] int NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260207184257_AddPhysicUnits'
)
BEGIN
    DECLARE @var10 sysname;
    SELECT @var10 = [d].[name]
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[Equipments]') AND [c].[name] = N'CityId');
    IF @var10 IS NOT NULL EXEC(N'ALTER TABLE [Equipments] DROP CONSTRAINT [' + @var10 + '];');
    ALTER TABLE [Equipments] ALTER COLUMN [CityId] int NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260207184257_AddPhysicUnits'
)
BEGIN
    CREATE TABLE [EquipmentUnits] (
        [Id] int NOT NULL IDENTITY,
        [EquipmentId] int NOT NULL,
        [InventoryNumber] nvarchar(50) NOT NULL,
        [SerialNumber] nvarchar(100) NULL,
        [AcquisitionDate] datetime2 NULL,
        [ServiceStartDate] datetime2 NULL,
        [AcquisitionValue] decimal(18,2) NULL,
        [CurrentStatus] int NOT NULL DEFAULT 0,
        [PhysicalCondition] int NULL,
        [Notes] nvarchar(2000) NULL,
        [CreatedById] int NULL,
        [CreatedDate] datetime2 NOT NULL,
        [ModifiedById] int NULL,
        [LastModifiedDate] datetime2 NULL,
        CONSTRAINT [PK_EquipmentUnits] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_EquipmentUnits_Equipments_EquipmentId] FOREIGN KEY ([EquipmentId]) REFERENCES [Equipments] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_EquipmentUnits_Users_CreatedById] FOREIGN KEY ([CreatedById]) REFERENCES [Users] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_EquipmentUnits_Users_ModifiedById] FOREIGN KEY ([ModifiedById]) REFERENCES [Users] ([Id]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260207184257_AddPhysicUnits'
)
BEGIN
    CREATE INDEX [IX_Requests_EquipmentUnitId] ON [Requests] ([EquipmentUnitId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260207184257_AddPhysicUnits'
)
BEGIN
    CREATE INDEX [IX_EquipmentUnits_CreatedById] ON [EquipmentUnits] ([CreatedById]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260207184257_AddPhysicUnits'
)
BEGIN
    CREATE INDEX [IX_EquipmentUnits_EquipmentId] ON [EquipmentUnits] ([EquipmentId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260207184257_AddPhysicUnits'
)
BEGIN
    EXEC(N'CREATE UNIQUE INDEX [IX_EquipmentUnits_InventoryNumber] ON [EquipmentUnits] ([InventoryNumber]) WHERE [CurrentStatus] != 99');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260207184257_AddPhysicUnits'
)
BEGIN
    CREATE INDEX [IX_EquipmentUnits_ModifiedById] ON [EquipmentUnits] ([ModifiedById]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260207184257_AddPhysicUnits'
)
BEGIN
    ALTER TABLE [EquipmentStateHistories] ADD CONSTRAINT [FK_EquipmentStateHistories_EquipmentUnits_EquipmentUnitId] FOREIGN KEY ([EquipmentUnitId]) REFERENCES [EquipmentUnits] ([Id]) ON DELETE NO ACTION;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260207184257_AddPhysicUnits'
)
BEGIN
    ALTER TABLE [MaintenancePlans] ADD CONSTRAINT [FK_MaintenancePlans_EquipmentUnits_EquipmentUnitId] FOREIGN KEY ([EquipmentUnitId]) REFERENCES [EquipmentUnits] ([Id]) ON DELETE NO ACTION;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260207184257_AddPhysicUnits'
)
BEGIN
    ALTER TABLE [Maintenances] ADD CONSTRAINT [FK_Maintenances_EquipmentUnits_EquipmentUnitId] FOREIGN KEY ([EquipmentUnitId]) REFERENCES [EquipmentUnits] ([Id]) ON DELETE NO ACTION;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260207184257_AddPhysicUnits'
)
BEGIN
    ALTER TABLE [Requests] ADD CONSTRAINT [FK_Requests_EquipmentUnits_EquipmentUnitId] FOREIGN KEY ([EquipmentUnitId]) REFERENCES [EquipmentUnits] ([Id]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260207184257_AddPhysicUnits'
)
BEGIN
    ALTER TABLE [Verifications] ADD CONSTRAINT [FK_Verifications_EquipmentUnits_EquipmentUnitId] FOREIGN KEY ([EquipmentUnitId]) REFERENCES [EquipmentUnits] ([Id]) ON DELETE NO ACTION;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260207184257_AddPhysicUnits'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260207184257_AddPhysicUnits', N'9.0.12');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260207185012_SyncNullableFields'
)
BEGIN
    ALTER TABLE [Equipments] DROP CONSTRAINT [FK_Equipments_Laboratories_LaboratoryId];
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260207185012_SyncNullableFields'
)
BEGIN
    ALTER TABLE [EquipmentStateHistories] DROP CONSTRAINT [FK_EquipmentStateHistories_EquipmentUnits_EquipmentUnitId];
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260207185012_SyncNullableFields'
)
BEGIN
    ALTER TABLE [MaintenancePlans] DROP CONSTRAINT [FK_MaintenancePlans_EquipmentUnits_EquipmentUnitId];
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260207185012_SyncNullableFields'
)
BEGIN
    DECLARE @var11 sysname;
    SELECT @var11 = [d].[name]
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[MaintenancePlans]') AND [c].[name] = N'EquipmentUnitId');
    IF @var11 IS NOT NULL EXEC(N'ALTER TABLE [MaintenancePlans] DROP CONSTRAINT [' + @var11 + '];');
    ALTER TABLE [MaintenancePlans] ALTER COLUMN [EquipmentUnitId] int NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260207185012_SyncNullableFields'
)
BEGIN
    DECLARE @var12 sysname;
    SELECT @var12 = [d].[name]
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[EquipmentStateHistories]') AND [c].[name] = N'EquipmentUnitId');
    IF @var12 IS NOT NULL EXEC(N'ALTER TABLE [EquipmentStateHistories] DROP CONSTRAINT [' + @var12 + '];');
    ALTER TABLE [EquipmentStateHistories] ALTER COLUMN [EquipmentUnitId] int NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260207185012_SyncNullableFields'
)
BEGIN
    DECLARE @var13 sysname;
    SELECT @var13 = [d].[name]
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[Equipments]') AND [c].[name] = N'LaboratoryId');
    IF @var13 IS NOT NULL EXEC(N'ALTER TABLE [Equipments] DROP CONSTRAINT [' + @var13 + '];');
    ALTER TABLE [Equipments] ALTER COLUMN [LaboratoryId] int NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260207185012_SyncNullableFields'
)
BEGIN
    ALTER TABLE [Equipments] ADD CONSTRAINT [FK_Equipments_Laboratories_LaboratoryId] FOREIGN KEY ([LaboratoryId]) REFERENCES [Laboratories] ([Id]) ON DELETE SET NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260207185012_SyncNullableFields'
)
BEGIN
    ALTER TABLE [EquipmentStateHistories] ADD CONSTRAINT [FK_EquipmentStateHistories_EquipmentUnits_EquipmentUnitId] FOREIGN KEY ([EquipmentUnitId]) REFERENCES [EquipmentUnits] ([Id]) ON DELETE SET NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260207185012_SyncNullableFields'
)
BEGIN
    ALTER TABLE [MaintenancePlans] ADD CONSTRAINT [FK_MaintenancePlans_EquipmentUnits_EquipmentUnitId] FOREIGN KEY ([EquipmentUnitId]) REFERENCES [EquipmentUnits] ([Id]) ON DELETE SET NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260207185012_SyncNullableFields'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260207185012_SyncNullableFields', N'9.0.12');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260207191037_FixMaintenences'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260207191037_FixMaintenences', N'9.0.12');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260208012854_CompleteMoveLaboratoryToUnit'
)
BEGIN
    ALTER TABLE [Equipments] DROP CONSTRAINT [FK_Equipments_Laboratories_LaboratoryId];
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260208012854_CompleteMoveLaboratoryToUnit'
)
BEGIN
    DROP INDEX [IX_Equipments_LaboratoryId] ON [Equipments];
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260208012854_CompleteMoveLaboratoryToUnit'
)
BEGIN
    DECLARE @var14 sysname;
    SELECT @var14 = [d].[name]
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[Equipments]') AND [c].[name] = N'LaboratoryId');
    IF @var14 IS NOT NULL EXEC(N'ALTER TABLE [Equipments] DROP CONSTRAINT [' + @var14 + '];');
    ALTER TABLE [Equipments] DROP COLUMN [LaboratoryId];
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260208012854_CompleteMoveLaboratoryToUnit'
)
BEGIN
    ALTER TABLE [EquipmentUnits] ADD [LaboratoryId] int NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260208012854_CompleteMoveLaboratoryToUnit'
)
BEGIN
    CREATE INDEX [IX_EquipmentUnits_LaboratoryId] ON [EquipmentUnits] ([LaboratoryId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260208012854_CompleteMoveLaboratoryToUnit'
)
BEGIN
    ALTER TABLE [EquipmentUnits] ADD CONSTRAINT [FK_EquipmentUnits_Laboratories_LaboratoryId] FOREIGN KEY ([LaboratoryId]) REFERENCES [Laboratories] ([Id]) ON DELETE SET NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260208012854_CompleteMoveLaboratoryToUnit'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260208012854_CompleteMoveLaboratoryToUnit', N'9.0.12');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260208020358_CorrectLocationToPhysic'
)
BEGIN
    ALTER TABLE [CostDetails] DROP CONSTRAINT [FK_CostDetails_Users_CreatedById];
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260208020358_CorrectLocationToPhysic'
)
BEGIN
    ALTER TABLE [CostDetails] DROP CONSTRAINT [FK_CostDetails_Users_ModifiedById];
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260208020358_CorrectLocationToPhysic'
)
BEGIN
    ALTER TABLE [Equipments] DROP CONSTRAINT [FK_Equipments_Cities_CityId];
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260208020358_CorrectLocationToPhysic'
)
BEGIN
    ALTER TABLE [Equipments] DROP CONSTRAINT [FK_Equipments_Countries_CountryId];
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260208020358_CorrectLocationToPhysic'
)
BEGIN
    ALTER TABLE [Equipments] DROP CONSTRAINT [FK_Equipments_Users_ModifiedById];
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260208020358_CorrectLocationToPhysic'
)
BEGIN
    ALTER TABLE [EquipmentStateHistories] DROP CONSTRAINT [FK_EquipmentStateHistories_Users_CreatedById];
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260208020358_CorrectLocationToPhysic'
)
BEGIN
    ALTER TABLE [EquipmentStateHistories] DROP CONSTRAINT [FK_EquipmentStateHistories_Users_ModifiedById];
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260208020358_CorrectLocationToPhysic'
)
BEGIN
    ALTER TABLE [EquipmentUnits] DROP CONSTRAINT [FK_EquipmentUnits_Users_ModifiedById];
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260208020358_CorrectLocationToPhysic'
)
BEGIN
    ALTER TABLE [Laboratories] DROP CONSTRAINT [FK_Laboratories_Users_ModifiedById];
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260208020358_CorrectLocationToPhysic'
)
BEGIN
    ALTER TABLE [MaintenancePlans] DROP CONSTRAINT [FK_MaintenancePlans_Users_AssignedTechnicianId];
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260208020358_CorrectLocationToPhysic'
)
BEGIN
    ALTER TABLE [Maintenances] DROP CONSTRAINT [FK_Maintenances_People_TechnicianId];
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260208020358_CorrectLocationToPhysic'
)
BEGIN
    ALTER TABLE [Maintenances] DROP CONSTRAINT [FK_Maintenances_Users_CreatedById];
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260208020358_CorrectLocationToPhysic'
)
BEGIN
    ALTER TABLE [Maintenances] DROP CONSTRAINT [FK_Maintenances_Users_ModifiedById];
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260208020358_CorrectLocationToPhysic'
)
BEGIN
    ALTER TABLE [People] DROP CONSTRAINT [FK_People_Users_CreatedById];
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260208020358_CorrectLocationToPhysic'
)
BEGIN
    ALTER TABLE [People] DROP CONSTRAINT [FK_People_Users_ModifiedById];
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260208020358_CorrectLocationToPhysic'
)
BEGIN
    ALTER TABLE [Requests] DROP CONSTRAINT [FK_Requests_Users_ApprovedById];
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260208020358_CorrectLocationToPhysic'
)
BEGIN
    ALTER TABLE [Requests] DROP CONSTRAINT [FK_Requests_Users_CreatedById];
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260208020358_CorrectLocationToPhysic'
)
BEGIN
    ALTER TABLE [Requests] DROP CONSTRAINT [FK_Requests_Users_ModifiedById];
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260208020358_CorrectLocationToPhysic'
)
BEGIN
    ALTER TABLE [Requests] DROP CONSTRAINT [FK_Requests_Users_RequestedById];
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260208020358_CorrectLocationToPhysic'
)
BEGIN
    ALTER TABLE [Users] DROP CONSTRAINT [FK_Users_Users_CreatedById];
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260208020358_CorrectLocationToPhysic'
)
BEGIN
    ALTER TABLE [Users] DROP CONSTRAINT [FK_Users_Users_ModifiedById];
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260208020358_CorrectLocationToPhysic'
)
BEGIN
    ALTER TABLE [Verifications] DROP CONSTRAINT [FK_Verifications_Users_CreatedById];
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260208020358_CorrectLocationToPhysic'
)
BEGIN
    ALTER TABLE [Verifications] DROP CONSTRAINT [FK_Verifications_Users_ModifiedById];
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260208020358_CorrectLocationToPhysic'
)
BEGIN
    ALTER TABLE [EquipmentUnits] ADD [CityId] int NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260208020358_CorrectLocationToPhysic'
)
BEGIN
    ALTER TABLE [EquipmentUnits] ADD [CountryId] int NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260208020358_CorrectLocationToPhysic'
)
BEGIN
    DECLARE @var15 sysname;
    SELECT @var15 = [d].[name]
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[EquipmentStateHistories]') AND [c].[name] = N'Status');
    IF @var15 IS NOT NULL EXEC(N'ALTER TABLE [EquipmentStateHistories] DROP CONSTRAINT [' + @var15 + '];');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260208020358_CorrectLocationToPhysic'
)
BEGIN
    CREATE INDEX [IX_EquipmentUnits_CityId] ON [EquipmentUnits] ([CityId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260208020358_CorrectLocationToPhysic'
)
BEGIN
    CREATE INDEX [IX_EquipmentUnits_CountryId] ON [EquipmentUnits] ([CountryId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260208020358_CorrectLocationToPhysic'
)
BEGIN
    ALTER TABLE [CostDetails] ADD CONSTRAINT [FK_CostDetails_Users_CreatedById] FOREIGN KEY ([CreatedById]) REFERENCES [Users] ([Id]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260208020358_CorrectLocationToPhysic'
)
BEGIN
    ALTER TABLE [CostDetails] ADD CONSTRAINT [FK_CostDetails_Users_ModifiedById] FOREIGN KEY ([ModifiedById]) REFERENCES [Users] ([Id]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260208020358_CorrectLocationToPhysic'
)
BEGIN
    ALTER TABLE [Equipments] ADD CONSTRAINT [FK_Equipments_Cities_CityId] FOREIGN KEY ([CityId]) REFERENCES [Cities] ([Id]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260208020358_CorrectLocationToPhysic'
)
BEGIN
    ALTER TABLE [Equipments] ADD CONSTRAINT [FK_Equipments_Countries_CountryId] FOREIGN KEY ([CountryId]) REFERENCES [Countries] ([Id]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260208020358_CorrectLocationToPhysic'
)
BEGIN
    ALTER TABLE [Equipments] ADD CONSTRAINT [FK_Equipments_Users_ModifiedById] FOREIGN KEY ([ModifiedById]) REFERENCES [Users] ([Id]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260208020358_CorrectLocationToPhysic'
)
BEGIN
    ALTER TABLE [EquipmentStateHistories] ADD CONSTRAINT [FK_EquipmentStateHistories_Users_CreatedById] FOREIGN KEY ([CreatedById]) REFERENCES [Users] ([Id]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260208020358_CorrectLocationToPhysic'
)
BEGIN
    ALTER TABLE [EquipmentStateHistories] ADD CONSTRAINT [FK_EquipmentStateHistories_Users_ModifiedById] FOREIGN KEY ([ModifiedById]) REFERENCES [Users] ([Id]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260208020358_CorrectLocationToPhysic'
)
BEGIN
    ALTER TABLE [EquipmentUnits] ADD CONSTRAINT [FK_EquipmentUnits_Cities_CityId] FOREIGN KEY ([CityId]) REFERENCES [Cities] ([Id]) ON DELETE NO ACTION;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260208020358_CorrectLocationToPhysic'
)
BEGIN
    ALTER TABLE [EquipmentUnits] ADD CONSTRAINT [FK_EquipmentUnits_Countries_CountryId] FOREIGN KEY ([CountryId]) REFERENCES [Countries] ([Id]) ON DELETE NO ACTION;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260208020358_CorrectLocationToPhysic'
)
BEGIN
    ALTER TABLE [EquipmentUnits] ADD CONSTRAINT [FK_EquipmentUnits_Users_ModifiedById] FOREIGN KEY ([ModifiedById]) REFERENCES [Users] ([Id]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260208020358_CorrectLocationToPhysic'
)
BEGIN
    ALTER TABLE [Laboratories] ADD CONSTRAINT [FK_Laboratories_Users_ModifiedById] FOREIGN KEY ([ModifiedById]) REFERENCES [Users] ([Id]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260208020358_CorrectLocationToPhysic'
)
BEGIN
    ALTER TABLE [MaintenancePlans] ADD CONSTRAINT [FK_MaintenancePlans_Users_AssignedTechnicianId] FOREIGN KEY ([AssignedTechnicianId]) REFERENCES [Users] ([Id]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260208020358_CorrectLocationToPhysic'
)
BEGIN
    ALTER TABLE [Maintenances] ADD CONSTRAINT [FK_Maintenances_People_TechnicianId] FOREIGN KEY ([TechnicianId]) REFERENCES [People] ([Id]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260208020358_CorrectLocationToPhysic'
)
BEGIN
    ALTER TABLE [Maintenances] ADD CONSTRAINT [FK_Maintenances_Users_CreatedById] FOREIGN KEY ([CreatedById]) REFERENCES [Users] ([Id]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260208020358_CorrectLocationToPhysic'
)
BEGIN
    ALTER TABLE [Maintenances] ADD CONSTRAINT [FK_Maintenances_Users_ModifiedById] FOREIGN KEY ([ModifiedById]) REFERENCES [Users] ([Id]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260208020358_CorrectLocationToPhysic'
)
BEGIN
    ALTER TABLE [People] ADD CONSTRAINT [FK_People_Users_CreatedById] FOREIGN KEY ([CreatedById]) REFERENCES [Users] ([Id]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260208020358_CorrectLocationToPhysic'
)
BEGIN
    ALTER TABLE [People] ADD CONSTRAINT [FK_People_Users_ModifiedById] FOREIGN KEY ([ModifiedById]) REFERENCES [Users] ([Id]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260208020358_CorrectLocationToPhysic'
)
BEGIN
    ALTER TABLE [Requests] ADD CONSTRAINT [FK_Requests_Users_ApprovedById] FOREIGN KEY ([ApprovedById]) REFERENCES [Users] ([Id]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260208020358_CorrectLocationToPhysic'
)
BEGIN
    ALTER TABLE [Requests] ADD CONSTRAINT [FK_Requests_Users_CreatedById] FOREIGN KEY ([CreatedById]) REFERENCES [Users] ([Id]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260208020358_CorrectLocationToPhysic'
)
BEGIN
    ALTER TABLE [Requests] ADD CONSTRAINT [FK_Requests_Users_ModifiedById] FOREIGN KEY ([ModifiedById]) REFERENCES [Users] ([Id]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260208020358_CorrectLocationToPhysic'
)
BEGIN
    ALTER TABLE [Requests] ADD CONSTRAINT [FK_Requests_Users_RequestedById] FOREIGN KEY ([RequestedById]) REFERENCES [Users] ([Id]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260208020358_CorrectLocationToPhysic'
)
BEGIN
    ALTER TABLE [Users] ADD CONSTRAINT [FK_Users_Users_CreatedById] FOREIGN KEY ([CreatedById]) REFERENCES [Users] ([Id]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260208020358_CorrectLocationToPhysic'
)
BEGIN
    ALTER TABLE [Users] ADD CONSTRAINT [FK_Users_Users_ModifiedById] FOREIGN KEY ([ModifiedById]) REFERENCES [Users] ([Id]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260208020358_CorrectLocationToPhysic'
)
BEGIN
    ALTER TABLE [Verifications] ADD CONSTRAINT [FK_Verifications_Users_CreatedById] FOREIGN KEY ([CreatedById]) REFERENCES [Users] ([Id]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260208020358_CorrectLocationToPhysic'
)
BEGIN
    ALTER TABLE [Verifications] ADD CONSTRAINT [FK_Verifications_Users_ModifiedById] FOREIGN KEY ([ModifiedById]) REFERENCES [Users] ([Id]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260208020358_CorrectLocationToPhysic'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260208020358_CorrectLocationToPhysic', N'9.0.12');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260208022638_MoveLocationBackToEquipmentV3'
)
BEGIN
    ALTER TABLE [Equipments] DROP CONSTRAINT [FK_Equipments_Cities_CityId];
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260208022638_MoveLocationBackToEquipmentV3'
)
BEGIN
    ALTER TABLE [Equipments] DROP CONSTRAINT [FK_Equipments_Countries_CountryId];
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260208022638_MoveLocationBackToEquipmentV3'
)
BEGIN
    ALTER TABLE [EquipmentUnits] DROP CONSTRAINT [FK_EquipmentUnits_Cities_CityId];
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260208022638_MoveLocationBackToEquipmentV3'
)
BEGIN
    ALTER TABLE [EquipmentUnits] DROP CONSTRAINT [FK_EquipmentUnits_Countries_CountryId];
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260208022638_MoveLocationBackToEquipmentV3'
)
BEGIN
    DROP INDEX [IX_EquipmentUnits_CityId] ON [EquipmentUnits];
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260208022638_MoveLocationBackToEquipmentV3'
)
BEGIN
    DROP INDEX [IX_EquipmentUnits_CountryId] ON [EquipmentUnits];
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260208022638_MoveLocationBackToEquipmentV3'
)
BEGIN
    DECLARE @var16 sysname;
    SELECT @var16 = [d].[name]
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[EquipmentUnits]') AND [c].[name] = N'CityId');
    IF @var16 IS NOT NULL EXEC(N'ALTER TABLE [EquipmentUnits] DROP CONSTRAINT [' + @var16 + '];');
    ALTER TABLE [EquipmentUnits] DROP COLUMN [CityId];
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260208022638_MoveLocationBackToEquipmentV3'
)
BEGIN
    DECLARE @var17 sysname;
    SELECT @var17 = [d].[name]
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[EquipmentUnits]') AND [c].[name] = N'CountryId');
    IF @var17 IS NOT NULL EXEC(N'ALTER TABLE [EquipmentUnits] DROP CONSTRAINT [' + @var17 + '];');
    ALTER TABLE [EquipmentUnits] DROP COLUMN [CountryId];
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260208022638_MoveLocationBackToEquipmentV3'
)
BEGIN
    ALTER TABLE [Equipments] ADD CONSTRAINT [FK_Equipments_Cities_CityId] FOREIGN KEY ([CityId]) REFERENCES [Cities] ([Id]) ON DELETE NO ACTION;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260208022638_MoveLocationBackToEquipmentV3'
)
BEGIN
    ALTER TABLE [Equipments] ADD CONSTRAINT [FK_Equipments_Countries_CountryId] FOREIGN KEY ([CountryId]) REFERENCES [Countries] ([Id]) ON DELETE NO ACTION;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260208022638_MoveLocationBackToEquipmentV3'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260208022638_MoveLocationBackToEquipmentV3', N'9.0.12');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260209110411_UpdateDatabase1'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260209110411_UpdateDatabase1', N'9.0.12');
END;

COMMIT;
GO

