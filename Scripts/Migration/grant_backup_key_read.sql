SET NOCOUNT ON;

DECLARE @WasAdvanced int =
    (SELECT CAST(value_in_use AS int) FROM sys.configurations WHERE name = N'show advanced options');
DECLARE @WasXp int =
    (SELECT CAST(value_in_use AS int) FROM sys.configurations WHERE name = N'xp_cmdshell');

IF @WasAdvanced = 0
BEGIN
    EXEC sp_configure N'show advanced options', 1;
    RECONFIGURE;
END;

IF @WasXp = 0
BEGIN
    EXEC sp_configure N'xp_cmdshell', 1;
    RECONFIGURE;
END;

EXEC master.dbo.xp_cmdshell
    N'icacls "C:\Users\monte\Documents\SQLServerBackups\DB_Laboratorios_Univalle\Keys\LaboratoriosBackupCertificate.cer" /grant "NIHAHT\monte:(R)" "NIHAHT\CodexSandboxUsers:(R)"',
    NO_OUTPUT;
EXEC master.dbo.xp_cmdshell
    N'icacls "C:\Users\monte\Documents\SQLServerBackups\DB_Laboratorios_Univalle\Keys\LaboratoriosBackupCertificate.pvk" /grant "NIHAHT\monte:(R)" "NIHAHT\CodexSandboxUsers:(R)"',
    NO_OUTPUT;

IF @WasXp = 0
BEGIN
    EXEC sp_configure N'xp_cmdshell', 0;
    RECONFIGURE;
END;

IF @WasAdvanced = 0
BEGIN
    EXEC sp_configure N'show advanced options', 0;
    RECONFIGURE;
END;

SELECT name, value_in_use
FROM sys.configurations
WHERE name IN (N'show advanced options', N'xp_cmdshell');
