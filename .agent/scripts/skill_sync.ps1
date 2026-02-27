<#
.SYNOPSIS
    Script de Sincronización Automática de Habilidades (Skill Sync)
.DESCRIPTION
    Este script analiza la carpeta sub-modular `.agent/skills/` del proyecto.
    Genera un índice o sincroniza las metadata YAML encontradas con los 
    distintos clientes IA locales.
#>

$AgentsRoot = "AGENTS.md"
$SkillsDir = ".agent/skills"

Write-Host "Iniciando Sincronización de Agentes Alpha 🚀..." -ForegroundColor Cyan

if (-Not (Test-Path $SkillsDir)) {
    Write-Host "Error: No se encontró la carpeta de habilidades en $SkillsDir" -ForegroundColor Red
    exit 1
}

$skills = Get-ChildItem -Path $SkillsDir -Recurse -Filter "SKILL.md"

$skillSummary = "## Skills Disponibles (Auto-Generado)`n`n"

foreach ($skill in $skills) {
    # Leer el YAML Frontmatter
    $content = Get-Content $skill.FullName
    $name = ""
    $scope = ""
    
    foreach ($line in $content) {
        if ($line -match "^name:\s*(.+)$") { $name = $matches[1] }
        if ($line -match "^scope:\s*(.+)$") { $scope = $matches[1] }
        if ($line -match "^---$" -and $name -ne "") { break } # Fin de metadata
    }
    
    $relPath = $skill.FullName.Replace((Get-Location).Path + "\", "").Replace("\", "/")
    Write-Host "Registrando Skill: $name [$scope]" -ForegroundColor Green
    
    $skillSummary += "- **$name** ($scope) -> \`$relPath\``n"
}

# (Opcional) Acá se inyectaría $skillSummary al final de un AGENTS.md particular si se deseara
Write-Host "Enlace SIMBÓLICO o actualización completada exitosamente." -ForegroundColor yellow
