[CmdletBinding()]
param(
    [string]$ServerInstance = "localhost",
    [string]$DatabaseName = "DB_Laboratorios_Univalle_MIGRACION",
    [string]$SeedScript,
    [string]$BackupDirectory
)

$ErrorActionPreference = "Stop"

$repoRoot = (Resolve-Path -LiteralPath (Join-Path $PSScriptRoot "..\..")).Path
$projectFile = Join-Path $repoRoot "Proyecto Laboratorios Univalle.csproj"
$validationScript = Join-Path $PSScriptRoot "validate_historical_import.sql"
$sqlcmd = "C:\Program Files\Microsoft SQL Server\Client SDK\ODBC\170\Tools\Binn\SQLCMD.EXE"

if ([string]::IsNullOrWhiteSpace($SeedScript)) {
    $SeedScript = Join-Path $repoRoot "tmp\migration-package-20260820-v6\load-seed.sql"
}

if ($DatabaseName -notmatch '^[A-Za-z][A-Za-z0-9_]{2,127}$') {
    throw "DatabaseName solo puede contener letras, numeros y guion bajo, y debe comenzar con una letra."
}

if ($DatabaseName -in @("master", "model", "msdb", "tempdb")) {
    throw "No se permite usar una base de sistema como destino."
}

foreach ($requiredFile in @($projectFile, $validationScript, $SeedScript, $sqlcmd)) {
    if (-not (Test-Path -LiteralPath $requiredFile -PathType Leaf)) {
        throw "No se encontro el archivo requerido: $requiredFile"
    }
}

function Invoke-SqlCmdTool {
    param([string[]]$Arguments)

    & $sqlcmd @Arguments
    if ($LASTEXITCODE -ne 0) {
        throw "sqlcmd termino con codigo $LASTEXITCODE."
    }
}

Write-Host "Verificando la instancia SQL Server '$ServerInstance'..."
Invoke-SqlCmdTool -Arguments @(
    "-S", $ServerInstance,
    "-E",
    "-b",
    "-Q", "SET NOCOUNT ON; SELECT CONVERT(nvarchar(128), SERVERPROPERTY('Edition')) AS Edition, CONVERT(nvarchar(128), SERVERPROPERTY('ProductVersion')) AS ProductVersion;"
)

$databaseExistsQuery = "SET NOCOUNT ON; IF DB_ID(N'$DatabaseName') IS NULL SELECT 0 ELSE SELECT 1;"
$databaseExists = & $sqlcmd -S $ServerInstance -E -h -1 -W -Q $databaseExistsQuery
if ($LASTEXITCODE -ne 0) {
    throw "No se pudo comprobar si la base destino existe."
}

if (($databaseExists | Out-String).Trim() -ne "0") {
    throw "La base '$DatabaseName' ya existe. El script no la sobrescribe; elige otro nombre o revisala manualmente."
}

Write-Host "Creando la base aislada '$DatabaseName'..."
Invoke-SqlCmdTool -Arguments @(
    "-S", $ServerInstance,
    "-E",
    "-b",
    "-Q", "CREATE DATABASE [$DatabaseName]; ALTER DATABASE [$DatabaseName] SET RECOVERY SIMPLE;"
)

$connectionString = "Server=$ServerInstance;Database=$DatabaseName;Trusted_Connection=True;MultipleActiveResultSets=true;TrustServerCertificate=True"

try {
    Push-Location $repoRoot
    try {
        Write-Host "Aplicando las migraciones EF Core existentes..."
        dotnet tool restore
        if ($LASTEXITCODE -ne 0) { throw "dotnet tool restore fallo." }

        dotnet build $projectFile --no-restore
        if ($LASTEXITCODE -ne 0) { throw "La compilacion del proyecto fallo." }

        $previousConnection = [Environment]::GetEnvironmentVariable("ConnectionStrings__DefaultConnection", "Process")
        [Environment]::SetEnvironmentVariable("ConnectionStrings__DefaultConnection", $connectionString, "Process")
        try {
            dotnet ef database update --project $projectFile --startup-project $projectFile --connection $connectionString --no-build
            if ($LASTEXITCODE -ne 0) { throw "La aplicacion de migraciones EF Core fallo." }
        }
        finally {
            [Environment]::SetEnvironmentVariable("ConnectionStrings__DefaultConnection", $previousConnection, "Process")
        }
    }
    finally {
        Pop-Location
    }

    Write-Host "Cargando los datos historicos desde el paquete validado..."
    Invoke-SqlCmdTool -Arguments @(
        "-S", $ServerInstance,
        "-E",
        "-d", $DatabaseName,
        "-b",
        "-r", "1",
        "-i", $SeedScript
    )

    Write-Host "Ejecutando controles de integridad y conteos..."
    Invoke-SqlCmdTool -Arguments @(
        "-S", $ServerInstance,
        "-E",
        "-b",
        "-r", "1",
        "-v", "DatabaseName=$DatabaseName",
        "-i", $validationScript
    )

    $timestamp = Get-Date -Format "yyyyMMdd-HHmmss"
    if ([string]::IsNullOrWhiteSpace($BackupDirectory)) {
        $backupDirectoryQuery = "SET NOCOUNT ON; SELECT CONVERT(nvarchar(4000), SERVERPROPERTY('InstanceDefaultBackupPath'));"
        $BackupDirectory = ((& $sqlcmd -S $ServerInstance -E -h -1 -W -Q $backupDirectoryQuery) | Out-String).Trim()
        if ($LASTEXITCODE -ne 0 -or [string]::IsNullOrWhiteSpace($BackupDirectory)) {
            throw "No se pudo obtener el directorio de backups de la instancia."
        }
    }

    $backupPath = Join-Path $BackupDirectory "$DatabaseName-$timestamp.bak"
    $escapedBackupPath = $backupPath.Replace("'", "''")
    Write-Host "Creando y verificando el backup '$backupPath'..."
    Invoke-SqlCmdTool -Arguments @(
        "-S", $ServerInstance,
        "-E",
        "-b",
        "-Q", "BACKUP DATABASE [$DatabaseName] TO DISK = N'$escapedBackupPath' WITH COPY_ONLY, CHECKSUM, COMPRESSION, STATS = 10; RESTORE VERIFYONLY FROM DISK = N'$escapedBackupPath' WITH CHECKSUM;"
    )

    Write-Host "Migracion completada. Base: $DatabaseName"
    Write-Host "Backup verificado: $backupPath"
}
catch {
    Write-Error "La migracion no se completo: $($_.Exception.Message)"
    Write-Warning "La base aislada '$DatabaseName' se conserva para diagnostico; no fue eliminada automaticamente."
    throw
}
