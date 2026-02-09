# Script de Publicación Automatizada
# Proyecto: Laboratorios Univalle
# Fecha: 2026-02-09

Write-Host "============================================" -ForegroundColor Cyan
Write-Host "   PUBLICACIÓN - LABORATORIOS UNIVALLE     " -ForegroundColor Cyan
Write-Host "============================================" -ForegroundColor Cyan
Write-Host ""

# Configuración
$ProjectPath = $PSScriptRoot
$ProjectFile = "Proyecto Laboratorios Univalle.csproj"
$PublishPath = "bin\Release\net9.0\publish"
$Configuration = "Release"

Write-Host "📁 Ruta del proyecto: $ProjectPath" -ForegroundColor Yellow
Write-Host ""

# Paso 1: Limpiar publicación anterior
Write-Host "🧹 PASO 1: Limpiando publicaciones anteriores..." -ForegroundColor Green
if (Test-Path $PublishPath) {
    Remove-Item -Path $PublishPath -Recurse -Force
    Write-Host "   ✓ Carpeta de publicación anterior eliminada" -ForegroundColor Gray
} else {
    Write-Host "   ℹ️ No hay publicaciones anteriores" -ForegroundColor Gray
}
Write-Host ""

# Paso 2: Compilar el proyecto
Write-Host "🔨 PASO 2: Compilando el proyecto..." -ForegroundColor Green
Write-Host "   Configuración: $Configuration" -ForegroundColor Gray

try {
    dotnet build "$ProjectFile" --configuration $Configuration --nologo
    
    if ($LASTEXITCODE -ne 0) {
        Write-Host "   ❌ Error en la compilación" -ForegroundColor Red
        exit 1
    }
    
    Write-Host "   ✓ Compilación exitosa" -ForegroundColor Gray
} catch {
    Write-Host "   ❌ Error al compilar: $_" -ForegroundColor Red
    exit 1
}
Write-Host ""

# Paso 3: Publicar el proyecto
Write-Host "📦 PASO 3: Publicando el proyecto..." -ForegroundColor Green

try {
    dotnet publish "$ProjectFile" `
        --configuration $Configuration `
        --output $PublishPath `
        --no-build `
        --nologo
    
    if ($LASTEXITCODE -ne 0) {
        Write-Host "   ❌ Error en la publicación" -ForegroundColor Red
        exit 1
    }
    
    Write-Host "   ✓ Publicación exitosa" -ForegroundColor Gray
} catch {
    Write-Host "   ❌ Error al publicar: $_" -ForegroundColor Red
    exit 1
}
Write-Host ""

# Paso 4: Verificar archivos publicados
Write-Host "✅ PASO 4: Verificando archivos publicados..." -ForegroundColor Green

$PublishFullPath = Join-Path $ProjectPath $PublishPath

if (Test-Path $PublishFullPath) {
    $fileCount = (Get-ChildItem -Path $PublishFullPath -Recurse -File).Count
    $folderCount = (Get-ChildItem -Path $PublishFullPath -Recurse -Directory).Count
    
    Write-Host "   📊 Archivos publicados: $fileCount" -ForegroundColor Gray
    Write-Host "   📂 Carpetas: $folderCount" -ForegroundColor Gray
    
    # Verificar archivos importantes
    $criticalFiles = @(
        "Proyecto Laboratorios Univalle.dll",
        "appsettings.json",
        "web.config",
        "wwwroot"
    )
    
    Write-Host ""
    Write-Host "   Verificando archivos críticos:" -ForegroundColor Gray
    
    foreach ($file in $criticalFiles) {
        $filePath = Join-Path $PublishFullPath $file
        if (Test-Path $filePath) {
            Write-Host "      ✓ $file" -ForegroundColor Green
        } else {
            Write-Host "      ❌ $file (NO ENCONTRADO)" -ForegroundColor Red
        }
    }
} else {
    Write-Host "   ❌ No se encontró la carpeta de publicación" -ForegroundColor Red
    exit 1
}
Write-Host ""

# Paso 5: Generar información de despliegue
Write-Host "📋 PASO 5: Generando información de despliegue..." -ForegroundColor Green

$deployInfo = @"
==============================================
  INFORMACIÓN DE DESPLIEGUE
==============================================
Fecha: $(Get-Date -Format "yyyy-MM-dd HH:mm:ss")
Configuración: $Configuration
Carpeta de publicación: $PublishPath

PRÓXIMOS PASOS:
---------------
1. Conéctate a FileZilla con estos datos:
   - Host: site53261.siteasp.net
   - Usuario: site53261
   - Contraseña: d%7TFp!4-6cD

2. Navega a la carpeta /wwwroot en el servidor

3. ELIMINA todo el contenido de /wwwroot

4. Sube TODOS los archivos de:
   $PublishFullPath
   
5. Verifica que se haya subido correctamente

6. Navega a tu sitio web para verificar

==============================================
"@

$deployInfoFile = Join-Path $PublishFullPath "DEPLOY_INFO.txt"
$deployInfo | Out-File -FilePath $deployInfoFile -Encoding UTF8

Write-Host "   ✓ Información de despliegue guardada en:" -ForegroundColor Gray
Write-Host "     $deployInfoFile" -ForegroundColor Gray
Write-Host ""

# Resumen final
Write-Host "============================================" -ForegroundColor Cyan
Write-Host "         ✅ PUBLICACIÓN COMPLETADA          " -ForegroundColor Green
Write-Host "============================================" -ForegroundColor Cyan
Write-Host ""
Write-Host "📍 Los archivos están listos en:" -ForegroundColor Yellow
Write-Host "   $PublishFullPath" -ForegroundColor White
Write-Host ""
Write-Host "🚀 Ahora puedes subir estos archivos vía FTP" -ForegroundColor Yellow
Write-Host ""
Write-Host "ℹ️  Presiona ENTER para abrir la carpeta de publicación..." -ForegroundColor Gray
Read-Host

# Abrir la carpeta de publicación
Invoke-Item $PublishFullPath
