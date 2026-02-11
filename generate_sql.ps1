# Script para Generar SQL de Migración
# Proyecto: Laboratorios Univalle
# Fecha: 2026-02-09

Write-Host "============================================" -ForegroundColor Cyan
Write-Host "  GENERACIÓN DE SCRIPT SQL - MIGRACIONES   " -ForegroundColor Cyan
Write-Host "============================================" -ForegroundColor Cyan
Write-Host ""

$ProjectPath = $PSScriptRoot
$OutputFile = "deploy_to_production.sql"

Write-Host "📁 Ruta del proyecto: $ProjectPath" -ForegroundColor Yellow
Write-Host "📄 Archivo de salida: $OutputFile" -ForegroundColor Yellow
Write-Host ""

# Verificar si Entity Framework CLI está instalado
Write-Host "🔍 Verificando Entity Framework CLI..." -ForegroundColor Green

try {
    $efVersion = dotnet ef --version 2>&1
    if ($LASTEXITCODE -ne 0) {
        Write-Host "   ❌ Entity Framework CLI no está instalado" -ForegroundColor Red
        Write-Host ""
        Write-Host "   Instalando Entity Framework CLI..." -ForegroundColor Yellow
        dotnet tool install --global dotnet-ef
        
        if ($LASTEXITCODE -ne 0) {
            Write-Host "   ❌ Error al instalar EF CLI" -ForegroundColor Red
            exit 1
        }
    }
    Write-Host "   ✓ Entity Framework CLI está disponible" -ForegroundColor Gray
}
catch {
    Write-Host "   ❌ Error al verificar EF CLI: $_" -ForegroundColor Red
}
Write-Host ""

# Generar el script SQL
Write-Host "📝 Generando script SQL de migraciones..." -ForegroundColor Green

try {
    # Eliminar archivo anterior si existe
    if (Test-Path $OutputFile) {
        Remove-Item $OutputFile -Force
        Write-Host "   ℹ️ Archivo anterior eliminado" -ForegroundColor Gray
    }
    
    # Generar el script
    dotnet ef migrations script --output $OutputFile --idempotent
    
    if ($LASTEXITCODE -ne 0) {
        Write-Host "   ❌ Error al generar el script SQL" -ForegroundColor Red
        exit 1
    }
    
    Write-Host "   ✓ Script SQL generado exitosamente" -ForegroundColor Gray
}
catch {
    Write-Host "   ❌ Error: $_" -ForegroundColor Red
    exit 1
}
Write-Host ""

# Verificar el archivo generado
if (Test-Path $OutputFile) {
    $fileSize = (Get-Item $OutputFile).Length
    $fileSizeKB = [math]::Round($fileSize / 1KB, 2)
    
    Write-Host "✅ Archivo generado:" -ForegroundColor Green
    Write-Host "   📄 Nombre: $OutputFile" -ForegroundColor Gray
    Write-Host "   📊 Tamaño: $fileSizeKB KB" -ForegroundColor Gray
    Write-Host ""
    
    # Contar líneas
    $lineCount = (Get-Content $OutputFile).Count
    Write-Host "   📏 Líneas: $lineCount" -ForegroundColor Gray
}
else {
    Write-Host "   ❌ El archivo no se generó correctamente" -ForegroundColor Red
    exit 1
}
Write-Host ""

# Instrucciones
Write-Host "============================================" -ForegroundColor Cyan
Write-Host "         ✅ SCRIPT SQL GENERADO             " -ForegroundColor Green
Write-Host "============================================" -ForegroundColor Cyan
Write-Host ""
Write-Host "PRÓXIMOS PASOS:" -ForegroundColor Yellow
Write-Host ""
Write-Host "1. Abre SQL Server Management Studio (SSMS)" -ForegroundColor White
Write-Host ""
Write-Host "2. Conéctate al servidor remoto:" -ForegroundColor White
Write-Host "   - Server: db40037.public.databaseasp.net" -ForegroundColor Gray
Write-Host "   - Authentication: SQL Server Authentication" -ForegroundColor Gray
Write-Host "   - Login: db40037" -ForegroundColor Gray
Write-Host "   - Password: Nf5!%3Yan?2S" -ForegroundColor Gray
Write-Host ""
Write-Host "3. Abre una nueva consulta (CTRL+N)" -ForegroundColor White
Write-Host ""
Write-Host "4. Carga el archivo: $OutputFile" -ForegroundColor White
Write-Host ""
Write-Host "5. Ejecuta el script (F5)" -ForegroundColor White
Write-Host ""
Write-Host "============================================" -ForegroundColor Cyan
Write-Host ""
Write-Host "ℹ️  Presiona ENTER para abrir el archivo SQL..." -ForegroundColor Gray
Read-Host

# Abrir el archivo SQL con el editor predeterminado
Invoke-Item $OutputFile
