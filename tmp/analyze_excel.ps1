$excel = New-Object -ComObject Excel.Application
$excel.Visible = $false
$files = @(
    'c:\Users\monte\Desktop\GASTRONOMIA - PELIGRO\Inventario equipos.xlsb',
    'c:\Users\monte\Desktop\GASTRONOMIA - PELIGRO\inventario utensilios.xlsx',
    'c:\Users\monte\Desktop\GASTRONOMIA - PELIGRO\tipos de equipos.xlsb',
    'c:\Users\monte\Desktop\GASTRONOMIA - PELIGRO\tipos de utensilios v.xlsx'
)

foreach ($f in $files) {
    Write-Host "`n=== FILE: $f ==="
    if (Test-Path $f) {
        try {
            $wb = $excel.Workbooks.Open($f)
            $sh = $wb.Sheets.Item(1)
            for ($r=1; $r -le 15; $r++) {
                $line = "$($r): "
                for ($c=1; $c -le 15; $c++) {
                    $val = $sh.Cells.Item($r, $c).Text
                    $line += "[$val] | "
                }
                Write-Host $line
            }
            $wb.Close($false)
        } catch {
            Write-Host "Error processing $f : $($_.Exception.Message)"
        }
    } else {
        Write-Host "File not found: $f"
    }
}
$excel.Quit()
[System.Runtime.Interopservices.Marshal]::ReleaseComObject($excel) | Out-Null
