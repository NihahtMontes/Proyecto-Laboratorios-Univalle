<#
.SYNOPSIS
  Sube una carpeta ya publicada (dotnet publish) a un sitio de MonsterASP con Web Deploy.

.DESCRIPTION
  - La contraseña NO pasa por este script: msdeploy la lee directamente del perfil
    .publishSettings descargado del panel (Deploy > Visual Studio access).
  - Guarde ese perfil FUERA del repositorio (p. ej. C:\Users\Public\BackupsUnivalle\).
  - DoNotDeleteRule: no borra en el servidor archivos que no estén en el paquete
    (DataProtectionKeys, wwwroot\uploads nuevos, logs).
  - AppOffline: pone app_offline.htm durante la copia para no chocar con DLL en uso.
  - Publique siempre desde un worktree limpio de un commit subido (ver
    docs/deployment/PROMPT_PUBLICACION_QA_DESKTOP.md, sección 1-B).

.EXAMPLE
  .\Tools\Deploy\Publicar-MonsterAsp.ps1 -PublishSettings C:\Users\Public\BackupsUnivalle\QA\site.PublishSettings -SourcePath C:\Temp\univalle-pub-20261103-1\publish -WhatIf
#>
[CmdletBinding()]
param(
    [Parameter(Mandatory = $true)] [string] $PublishSettings,
    [Parameter(Mandatory = $true)] [string] $SourcePath,
    [switch] $WhatIf
)

$ErrorActionPreference = 'Stop'

$msdeploy = 'C:\Program Files\IIS\Microsoft Web Deploy V3\msdeploy.exe'
if (-not (Test-Path -LiteralPath $msdeploy)) {
    $msdeploy = 'C:\Program Files (x86)\IIS\Microsoft Web Deploy V3\msdeploy.exe'
}
if (-not (Test-Path -LiteralPath $msdeploy)) { throw 'No se encontró msdeploy.exe (Web Deploy V3).' }

$PublishSettings = (Resolve-Path -LiteralPath $PublishSettings).Path
$SourcePath = (Resolve-Path -LiteralPath $SourcePath).Path

# msdeploy separa sus parámetros por comas y PowerShell 5 maneja mal las comillas internas:
# se exigen rutas sin espacios ni comas para no depender del entrecomillado.
foreach ($path in @($PublishSettings, $SourcePath)) {
    if ($path -match '[\s,]') { throw "Use una ruta sin espacios ni comas: $path" }
}

# El perfil con credenciales nunca debe estar dentro de un repositorio Git.
$dir = Split-Path -Parent $PublishSettings
while ($dir) {
    if (Test-Path -LiteralPath (Join-Path $dir '.git')) { throw "SEGURIDAD: el perfil está dentro de un repositorio Git ($dir). Muévalo fuera." }
    $parent = Split-Path -Parent $dir
    if ($parent -eq $dir) { break }
    $dir = $parent
}

foreach ($required in @('web.config', 'Proyecto Laboratorios Univalle.dll', 'wwwroot')) {
    if (-not (Test-Path -LiteralPath (Join-Path $SourcePath $required))) { throw "El paquete no contiene $required. ¿Es la salida de dotnet publish?" }
}
foreach ($forbidden in @('opencode.json', 'appsettings.Development.json')) {
    if (Test-Path -LiteralPath (Join-Path $SourcePath $forbidden)) { throw "El paquete contiene $forbidden; no se publica." }
}

# Solo se leen datos no secretos del perfil (sitio y destino) para mostrarlos.
[xml] $xml = Get-Content -LiteralPath $PublishSettings -Raw
$msProfile = $xml.publishData.publishProfile | Where-Object { $_.publishMethod -eq 'MSDeploy' } | Select-Object -First 1
if (-not $msProfile) { throw 'El perfil no tiene una entrada MSDeploy.' }
$site = $msProfile.msdeploySite
if ([string]::IsNullOrWhiteSpace($site)) { throw 'El perfil no indica msdeploySite.' }

Write-Host "Sitio destino : $site"
Write-Host "Servidor      : $($msProfile.publishUrl)"
Write-Host "Paquete       : $SourcePath"
Write-Host "Modo          : $(if ($WhatIf) { 'SIMULACIÓN (-whatif), no cambia nada' } else { 'PUBLICACIÓN REAL' })"

$arguments = @(
    '-verb:sync',
    "-source:contentPath=$SourcePath",
    "-dest:contentPath=$site,publishSettings=$PublishSettings,includeAcls=False",
    '-allowUntrusted',
    '-disableLink:AppPoolExtension',
    '-disableLink:ContentExtension',
    '-disableLink:CertificateExtension',
    '-enableRule:AppOffline',
    '-enableRule:DoNotDeleteRule',
    '-retryAttempts:3'
)
if ($WhatIf) { $arguments += '-whatif' }

& $msdeploy @arguments
$code = $LASTEXITCODE
if ($code -ne 0) { throw "msdeploy terminó con código $code." }
Write-Host 'Listo.'
