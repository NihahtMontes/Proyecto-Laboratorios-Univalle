# ============================================================
# fix-encoding.ps1
# Convierte TODOS los archivos .cs, .cshtml, .json del proyecto
# de UTF-8 con BOM → UTF-8 sin BOM (sin BOM).
# Ejecutar UNA VEZ al clonar el proyecto.
# Uso: powershell -ExecutionPolicy Bypass -File fix-encoding.ps1
# ============================================================

$extensions = @("*.cs", "*.cshtml", "*.json", "*.md")
$root = $PSScriptRoot

$utf8NoBom = New-Object System.Text.UTF8Encoding $false
$count = 0

foreach ($ext in $extensions) {
    Get-ChildItem -Path $root -Filter $ext -Recurse | ForEach-Object {
        $file = $_.FullName

        # Skip binary-ish files > 5MB
        if ($_.Length -gt 5MB) { return }

        try {
            $bytes = [System.IO.File]::ReadAllBytes($file)

            # Check for UTF-8 BOM (EF BB BF)
            if ($bytes.Length -ge 3 -and $bytes[0] -eq 0xEF -and $bytes[1] -eq 0xBB -and $bytes[2] -eq 0xBF) {
                $content = [System.Text.Encoding]::UTF8.GetString($bytes, 3, $bytes.Length - 3)
                [System.IO.File]::WriteAllText($file, $content, $utf8NoBom)
                Write-Host "  [FIXED] $($_.Name)"
                $count++
            }
        }
        catch {
            Write-Warning "Skipped: $file - $_"
        }
    }
}

Write-Host ""
Write-Host "✅ Encoding fix completado. $count archivo(s) corregidos." -ForegroundColor Green
