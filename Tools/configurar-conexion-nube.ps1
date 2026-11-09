# Configura la conexión a la base de datos de la nube (MonsterASP) para desarrollo local, después de clonar.
# Guarda la cadena en User Secrets del proyecto (fuera del repositorio: %APPDATA%\Microsoft\UserSecrets\...),
# por eso la contraseña NUNCA queda en Git. Se ejecuta una sola vez por computadora:
#
#   powershell -ExecutionPolicy Bypass -File Tools\configurar-conexion-nube.ps1
#
# Pide la contraseña sin mostrarla (la entrega el responsable del proyecto por un canal privado).
param(
    [string]$Server = "db70630.public.databaseasp.net",
    [string]$Database = "db70630",
    [string]$User = "db70630"
)

$ErrorActionPreference = "Stop"
$project = Join-Path $PSScriptRoot "..\Proyecto Laboratorios Univalle.csproj"

$secure = Read-Host "Contraseña de la base $Database (no se muestra)" -AsSecureString
$password = [Runtime.InteropServices.Marshal]::PtrToStringAuto([Runtime.InteropServices.Marshal]::SecureStringToBSTR($secure))
if ([string]::IsNullOrWhiteSpace($password)) { throw "La contraseña está vacía." }

$connection = "Server=$Server;Database=$Database;User Id=$User;Password=$password;Encrypt=True;TrustServerCertificate=True;MultipleActiveResultSets=True"
dotnet user-secrets set "ConnectionStrings:DefaultConnection" $connection --project $project | Out-Null

Write-Host "Listo: conexión guardada en User Secrets (fuera de Git)." -ForegroundColor Green
Write-Host "Ahora ejecuta:  dotnet run --launch-profile Proyecto_Laboratorios_Univalle" -ForegroundColor Cyan
Write-Host "Ojo: esa base es la compartida en la nube; lo que hagas ahí lo ve todo el equipo." -ForegroundColor Yellow
